using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x29f3.
	/// </summary>
	public class UnitCombat : GameModule
	{
		private readonly UnitMemory troops;

		public UnitCombat(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		// Unit data in memory: 12 bytes per unit, 0x600 bytes (128 units) per player
		private const int TroopConditionDisplacement = 0;
		private const int TroopHorizDisplacement = 1;
		private const int TroopVertDisplacement = 2;
		private const int TroopKindDisplacement = 3;
		private const int TroopStepsDisplacement = 4;

		// Unit definitions in memory: 34 bytes per unit type (see UnitDefinition class)
		private const int TroopSpecsRoot = 0x112a;
		private const int TroopSpecExtent = 0x22;
		private const int TroopLandGroupDisplacement = 14;
		private const int TroopAssaultDisplacement = 20;
		private const int TroopGuardDisplacement = 22;

		private const int TroopGroupSky = 1;
		private const int TroopGroupWater = 2;

		// Terrain definitions in memory: 19 bytes per terrain type, this points to the defense factor
		private const int LandGuardLookup = 0x28f;
		private const int LandExtent = 0x13;

		private const int CastleMarker = 0x1;
		private const int TownRampartsMarker = 0x80;

		// The last battle, for the battle animation
		private CombatResolver.Battle? finalFight = null;
		private int finalAssailantCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;
		private int finalGuardianCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;

		// Pauses of the battle animation (in timer ticks): before the first round and after each round
		private const int FightBeginWait = 20;
		private const int FightPhaseWait = 18;

		/// <summary>
		/// Shows the rounds of the last battle: green squares (hit points) over both units,
		/// a square of the unit that lost the round becomes empty.
		/// </summary>
		private void DisplayBattleRounds(int assailantHoriz, int assailantVert, int guardianHoriz, int guardianVert)
		{
			CombatResolver.Battle? fight = this.finalFight;

			if (fight == null)
				return;

			int assailantStrikeMarks = fight.AssailantBeginStrikeMarks;
			int guardianStrikeMarks = fight.GuardianBeginStrikeMarks;

			this.owner.MapOps.RenderHealthBar(assailantHoriz, assailantVert, this.finalAssailantCompleteStrikeMarks, assailantStrikeMarks);
			this.owner.MapOps.RenderHealthBar(guardianHoriz, guardianVert, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);
			this.owner.TextPainter.AwaitTime(FightBeginWait);

			for (int idx = 0; idx < fight.Phases.Count; idx++)
			{
				if (fight.Phases[idx])
				{
					guardianStrikeMarks--;
					this.owner.MapOps.RenderHealthBar(guardianHoriz, guardianVert, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);

					if (guardianStrikeMarks == 0 && idx + 1 < fight.Phases.Count)
					{
						// Barbarians attacking a city: the city defends again
						this.owner.TextPainter.AwaitTime(FightBeginWait);
						guardianStrikeMarks = fight.GuardianBeginStrikeMarks;
						this.owner.MapOps.RenderHealthBar(guardianHoriz, guardianVert, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);
					}
				}
				else
				{
					assailantStrikeMarks--;
					this.owner.MapOps.RenderHealthBar(assailantHoriz, assailantVert, this.finalAssailantCompleteStrikeMarks, assailantStrikeMarks);
				}

				this.owner.TextPainter.AwaitTime(FightPhaseWait);
			}
		}

		private int PullUnitByte(int participantIdx, int troopIdx, int displacement)
		{
			return this.troops.PullSigned(participantIdx, troopIdx, displacement);
		}

		private int PullUnitDefinitionWord(int troopKind, int displacement)
		{
			return (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(TroopSpecsRoot + troopKind * TroopSpecExtent + displacement));
		}

		private int ObtainUnitAttack(int troopKind)
		{
			return PullUnitDefinitionWord(troopKind, TroopAssaultDisplacement);
		}

		private int ObtainUnitProtection(int troopKind)
		{
			return PullUnitDefinitionWord(troopKind, TroopGuardDisplacement);
		}

		private int ObtainUnitTerrainCategory(int troopKind)
		{
			return PullUnitDefinitionWord(troopKind, TroopLandGroupDisplacement);
		}

		/// <summary>
		/// Resolves or estimates (`marker == 0`) combat between attacker (`participantIdx1`, `troopIdx1`) and defender
		/// (`participantIdx2`, `troopIdx2`), including treaty confirmation, hit-point combat rounds, map/explosion animation,
		/// diplomacy updates, nuclear detonation, stack/city aftermath, and veteran promotion.
		/// </summary>
		public ushort SettleBattle(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, ushort marker)
		{
			this.processor.Journal.OpenScope($"SettleBattle({participantIdx1}, {troopIdx1}, {participantIdx2}, {troopIdx2}, {marker})");

			this.processor.EnterFrame(0x26);
			this.processor.PreserveIndexRegisters();

			int assailantKind = PullUnitByte(participantIdx1, troopIdx1, TroopKindDisplacement);
			int guardianKind = PullUnitByte(participantIdx2, troopIdx2, TroopKindDisplacement);
			int guardianHoriz = PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement);
			int guardianVert = PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement);
			int guardianLand = this.owner.MapOps.ObtainTerrainType(guardianHoriz, guardianVert);
			int landGuard = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(LandGuardLookup + guardianLand * LandExtent));
			bool raidersAssaultTown = false;

			// Attack and defense strengths are scaled by 8, terrain defense factor of 2 is the normal (100%) value
			int assault = ObtainUnitAttack(assailantKind) * 8;
			bool stronghold = (this.owner.MapOps.ObtainTileImprovements(guardianHoriz, guardianVert) & 0x20) != 0;
			bool dugin = (PullUnitByte(participantIdx2, troopIdx2, TroopConditionDisplacement) & (int)UnitStatusEnum.Fortified) != 0;
			bool townRamparts = false;

			if (participantIdx1 == 0)
			{
				// Barbarians attack
				if (participantIdx2 == this.owner.World.PersonParticipantIdx)
				{
					assault = assault * (this.owner.World.HardnessTier + 1) / 4;
				}
				else
				{
					assault /= 2;
				}
			}

			int guardianGroup = ObtainUnitTerrainCategory(guardianKind);
			int townIdx = -1;

			if ((this.owner.MapOps.ObtainTileImprovements(guardianHoriz, guardianVert) & 0x1) != 0)
			{
				// Defender is in a city
				townIdx = (short)this.owner.Helpers.LocateCityAtPosition(guardianHoriz, guardianVert);
				ushort townStructures = this.owner.World.Towns[townIdx].StructureMarkers0;

				townRamparts = (townStructures & TownRampartsMarker) != 0;

				if (participantIdx1 == 0)
				{
					// Barbarians can't capture the last city of a player
					if (this.owner.World.Participants[participantIdx2].TownTally == 1)
					{
						assault = 0;
					}

					// Palace halves barbarian attack
					if ((townStructures & CastleMarker) != 0)
					{
						assault /= 2;
					}

					raidersAssaultTown = true;
				}
			}

			// Veteran: +50%
			if ((PullUnitByte(participantIdx1, troopIdx1, TroopConditionDisplacement) & (int)UnitStatusEnum.Veteran) != 0)
			{
				assault += assault / 2;
			}

			// Terrain, fortified, fortress, City Walls and veteran bonuses, see DefenseStrength
			int guard = DefenseStrength.Compute(ObtainUnitProtection(guardianKind), landGuard, stronghold, dugin,
				guardianGroup == TroopGroupWater, guardianGroup == TroopGroupSky, townRamparts,
				ObtainUnitAttack(assailantKind),
				(PullUnitByte(participantIdx2, troopIdx2, TroopConditionDisplacement) & (int)UnitStatusEnum.Veteran) != 0);

			// Attacker with less than one full move left attacks with reduced strength (moves are stored * 3)
			sbyte assailantSteps = (sbyte)PullUnitByte(participantIdx1, troopIdx1, TroopStepsDisplacement);
			if (assailantSteps < 3)
			{
				assault = assailantSteps * assault / 3;
			}


			if (marker == 0)
			{
				// Only estimate the odds (used by AI), don't fight
				int chances = (short)(assault * 8) / (short)(guard + 1);
				int leftover = (short)(assault * 8) % (short)(guard + 1);
				this.processor.RegAcc.Value16 = (ushort)chances;
				this.processor.RegData.Value16 = (ushort)leftover;
				return FinishSettleBattle((ushort)chances);
			}

			if (participantIdx1 != 0)
			{
				// Difficulty adjustments: Chieftain and Warlord halve AI attack against the human player
				if (this.owner.World.HardnessTier <= 1 && participantIdx2 == this.owner.World.PersonParticipantIdx)
				{
					assault /= 2;
				}

				// Chieftain doubles human player attack
				if (this.owner.World.HardnessTier == 0 && participantIdx1 == this.owner.World.PersonParticipantIdx)
				{
					assault *= 2;
				}
			}


			if (participantIdx1 == this.owner.World.PersonParticipantIdx)
			{
				if (AffirmBreakTreaty(participantIdx2) == 0xffff)
				{
					this.troops.Assign(participantIdx1, troopIdx1, UnitMemory.TravelHoriz, 0xff);
					return FinishSettleBattle(0xffff);
				}
			}

			bool assailantWins = ExecuteBattleClash(
				participantIdx1, troopIdx1,
				participantIdx2, troopIdx2,
				(ushort)assault, (ushort)guard,
				raidersAssaultTown);


			if (ShouldShowBattle(participantIdx1, troopIdx1, participantIdx2, troopIdx2))
			{
				AnimateBattle(participantIdx1, troopIdx1, participantIdx2, troopIdx2, assailantKind, guardianKind, assailantWins);
			}

			UpdateBattleDiplomacy(participantIdx1, participantIdx2);

			if (assailantKind == 0x19)
			{
				ExplodeNuclearWeapon(
					participantIdx1,
					(short)PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
					(short)PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement));
				return FinishSettleBattle(1);
			}

			if (assailantWins)
			{
				ResolveAttackerVictory(participantIdx1, troopIdx1, participantIdx2, troopIdx2, (short)townIdx);
				return FinishSettleBattle(1);
			}

			this.owner.World.Participants[participantIdx2].TroopsRuined[participantIdx1]++;
			this.owner.UnitOps.EliminateUnit(participantIdx1, troopIdx1);

			if (this.owner.Libc.Dice.DrawInt(2) != 0)
			{
				PromoteUnitToVeteran(participantIdx2, troopIdx2);
			}

			return FinishSettleBattle(0);
		}

		private ushort FinishSettleBattle(ushort outcome)
		{
			this.processor.RegAcc.Value16 = outcome;
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("SettleBattle");
			return outcome;
		}

		private bool ExecuteBattleClash(
			short participantIdx1, short troopIdx1,
			short participantIdx2, short troopIdx2,
			ushort assaultAmount, ushort guardAmount,
			bool townAssaultedViaRaiders)
		{
			Func<int, int> followingChance = peakAmount => this.owner.Libc.Dice.DrawInt(peakAmount);
			UnitHealth vitality = this.owner.World.Vitals;

			int assailantTroopKind = PullUnitByte(participantIdx1, troopIdx1, TroopKindDisplacement);
			int guardianTroopKind = PullUnitByte(participantIdx2, troopIdx2, TroopKindDisplacement);
			bool assailantSeasoned = (PullUnitByte(participantIdx1, troopIdx1, TroopConditionDisplacement) & (int)UnitStatusEnum.Veteran) != 0;
			bool guardianSeasoned = (PullUnitByte(participantIdx2, troopIdx2, TroopConditionDisplacement) & (int)UnitStatusEnum.Veteran) != 0;
			bool assailantAce = vitality.VerifyElite(participantIdx1, troopIdx1, assailantTroopKind);
			bool guardianAce = vitality.VerifyElite(participantIdx2, troopIdx2, guardianTroopKind);

			CombatResolver.Battle fight = CombatResolver.Clash(
				assaultAmount, guardAmount,
				vitality.ObtainHitPoints(participantIdx1, troopIdx1, assailantTroopKind, assailantSeasoned),
				vitality.ObtainHitPoints(participantIdx2, troopIdx2, guardianTroopKind, guardianSeasoned),
				townAssaultedViaRaiders ? 2 : 1,
				followingChance);

			this.finalFight = fight;
			this.finalAssailantCompleteStrikeMarks = CombatResolver.TopHitPoints(assailantSeasoned, assailantAce);
			this.finalGuardianCompleteStrikeMarks = CombatResolver.TopHitPoints(guardianSeasoned, guardianAce);

			if (fight.AssailantVictories)
			{
				bool ace = assailantAce || (assailantSeasoned && fight.Phases.Count > 0);
				vitality.Assign(
					participantIdx1, troopIdx1, assailantTroopKind,
					CombatResolver.TopHitPoints(assailantSeasoned, ace) - fight.AssailantStrikeMarks, ace);
				vitality.Wipe(participantIdx2, troopIdx2);
			}
			else
			{
				bool ace = guardianAce || (guardianSeasoned && fight.Phases.Count > 0);
				vitality.Assign(
					participantIdx2, troopIdx2, guardianTroopKind,
					CombatResolver.TopHitPoints(guardianSeasoned, ace) - fight.GuardianStrikeMarks, ace);
				vitality.Wipe(participantIdx1, troopIdx1);
			}

			return fight.AssailantVictories;
		}

		private bool ShouldShowBattle(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2)
		{
			short personIdx = this.owner.World.PersonParticipantIdx;
			ushort personBit = (ushort)(1 << (personIdx & 0xff));

			bool visibleToPerson =
				participantIdx1 == personIdx ||
				participantIdx2 == personIdx ||
				((ushort)PullUnitByte(participantIdx1, troopIdx1, 9) & personBit) != 0 ||
				((ushort)PullUnitByte(participantIdx2, troopIdx2, 9) & personBit) != 0 ||
				MapRevealed != 0;

			return visibleToPerson && WorldMapView != 1;
		}

		private void AnimateBattle(
			short participantIdx1, short troopIdx1,
			short participantIdx2, short troopIdx2,
			int assailantKind, int guardianKind,
			bool assailantWins)
		{
			this.owner.UnitOps.PanMapToPosition(
				this.owner.World.PersonParticipantIdx,
				PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
				PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement));

			this.owner.Startup.ConcealMouseCursor();

			ushort tileVisible = this.owner.MapOps.RenderMapTile(
				PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement),
				PullUnitByte(participantIdx1, troopIdx1, TroopVertDisplacement));

			if (tileVisible != 0)
			{
				int victorKind = assailantWins ? assailantKind : guardianKind;
				ushort modernWeapon = (ObtainUnitAttack(victorKind) > 4 || ObtainUnitProtection(victorKind) > 2) ? (ushort)1 : (ushort)0;

				ushort soundId;
				if (assailantWins && assailantKind == 0xf)
				{
					soundId = 0x2b;
				}
				else if ((assailantWins && participantIdx1 == this.owner.World.PersonParticipantIdx) ||
					(!assailantWins && participantIdx2 == this.owner.World.PersonParticipantIdx))
				{
					soundId = modernWeapon != 0 ? (ushort)0x28 : (ushort)0x26;
				}
				else
				{
					soundId = modernWeapon != 0 ? (ushort)0x29 : (ushort)0x27;
				}

				this.owner.Services.PerformSound(soundId, 0);

				this.owner.MapOps.RenderUnitOnWorldMap(participantIdx2, troopIdx2);

				byte nextInStack = (byte)PullUnitByte(participantIdx1, troopIdx1, 0xa);
				if (nextInStack != 0xff)
				{
					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx1, (short)PullUnitByte(participantIdx1, troopIdx1, 0xa));
				}

				this.owner.VideoCard.RenderImage(
					ScreenBox,
					0x50, 0, 0xf0, 0xc8,
					BackScreenBox,
					0x50, 0);

				short assailantScreenX = (short)((this.owner.Routing.TunePosition(
					PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement) -
					(short)MapViewHoriz) << 4) + 0x50);

				short assailantScreenY = (short)((((byte)PullUnitByte(participantIdx1, troopIdx1, TroopVertDisplacement) -
					MapViewVert) << 4) + 0x8);

				byte defX = (byte)PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement);
				byte attX = (byte)PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement);
				short stepX = (short)Math.Sign((short)(defX - attX));

				byte defY = (byte)PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement);
				byte attY = (byte)PullUnitByte(participantIdx1, troopIdx1, TroopVertDisplacement);
				short stepY = (short)Math.Sign((short)(defY - attY));

				if ((byte)PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement) == 0 &&
					(byte)PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement) == 0x4f)
				{
					stepX = (short)-stepX;
				}
				else if ((byte)PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement) == 0x4f &&
					(byte)PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement) == 0)
				{
					stepX = (short)-stepX;
				}

				for (short step = 0; step <= 0xa; step++)
				{
					short drawX = (short)(assailantScreenX + step * stepX);
					short drawY = (short)(assailantScreenY + step * stepY);

					ushort unitSprite = this.processor.PullUInt16(
						this.processor.SegData.Value16,
						(ushort)((((byte)PullUnitByte(participantIdx1, troopIdx1, TroopKindDisplacement) +
							(participantIdx1 << 5) + 0x40) << 1) + 0xd4ce));

					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						drawX + 1,
						drawY + 1,
						unitSprite);

					this.owner.TextPainter.AwaitTime(2);

					this.owner.VideoCard.RenderImage(
						BackScreenBox,
						(ushort)drawX, (ushort)drawY, 16, 16,
						ScreenBox,
						(ushort)drawX, (ushort)drawY);
				}


				this.owner.MapOps.RepaintMapTile(
					PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement),
					PullUnitByte(participantIdx1, troopIdx1, TroopVertDisplacement));

				this.owner.MapOps.RepaintMapTile(
					PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
					PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement));

				this.owner.VideoCard.RenderImage(
					ScreenBox,
					0, 0, 0x100, 0xc8,
					BackScreenBox,
					0, 0);

				int guardianDisplayHoriz = assailantScreenX + 16 * stepX;
				int guardianDisplayVert = assailantScreenY + 16 * stepY;
				DisplayBattleRounds(assailantScreenX, assailantScreenY, guardianDisplayHoriz, guardianDisplayVert);

				short loserStep = (short)(assailantWins ? 0x10 : 0);
				for (short frame = 0; frame < 8; frame++)
				{
					short loserX = (short)(assailantScreenX + loserStep * stepX);
					short loserY = (short)(assailantScreenY + loserStep * stepY);

					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						loserX + 1,
						loserY + 1,
						SpritePicture((frame + 0x20)));

					this.owner.TextPainter.AwaitTime(4);

					this.owner.VideoCard.RenderImage(
						BackScreenBox,
						(ushort)loserX, (ushort)loserY, 16, 16,
						ScreenBox,
						(ushort)loserX, (ushort)loserY);
				}


				int victorHoriz = assailantScreenX + (assailantWins ? 0 : 16 * stepX);
				int victorVert = assailantScreenY + (assailantWins ? 0 : 16 * stepY);

				this.owner.VideoCard.RenderImage(
					BackScreenBox,
					victorHoriz, victorVert, 16, 16,
					ScreenBox,
					victorHoriz, victorVert);
			}

			this.owner.Startup.DisplayMouseCursor();
		}

		private void UpdateBattleDiplomacy(short participantIdx1, short participantIdx2)
		{
			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] |= 0x20;
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 0x20;

			if (participantIdx1 != 0 && participantIdx2 != 0)
			{
				this.owner.World.TruceRoundTally = 0;
			}

			short personIdx = this.owner.World.PersonParticipantIdx;
			if ((participantIdx2 == personIdx && (this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0) ||
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 0x200) != 0)
			{
				DialogStyle = 0x3;

				Message("Sneak attack by\n")
					.Text(CivilizationName(participantIdx1))
					.Text(" forces!\n")
					.Dialog(100, 80);

				this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xfdff;
			}

			if ((this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0 ||
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
			{
				if (participantIdx1 == personIdx || participantIdx2 == personIdx)
				{
					DialogStyle = 0x5;

					Message(CivilizationPlural(participantIdx2))
						.Text(" cancel\npeace treaty\nwith ")
						.Text(CivilizationPlural(participantIdx1))
						.Text(".\n")
						.Dialog(100, 80);
				}

				if (participantIdx1 == personIdx && (this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0)
				{
					this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 8;
				}

				this.owner.Politics.WipeDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}

			if (participantIdx1 == personIdx)
			{
				this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xfdff;
			}
		}

		private void ResolveAttackerVictory(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, short townIdx)
		{
			if (participantIdx2 == 0 && (byte)PullUnitByte(participantIdx2, troopIdx2, TroopKindDisplacement) == 0x1a)
			{
				this.owner.World.Participants[participantIdx1].Gold += 100;

				if (participantIdx1 == this.owner.World.PersonParticipantIdx)
				{
					this.owner.Words.Clear();
					this.owner.BlockText.DisplayTextDialog(0x2ae7);
					this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);
				}
			}

			if (townIdx == -1 &&
				(this.owner.MapOps.ObtainTileImprovements(
					PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
					PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement)) & 0x20) == 0)
			{
				ushort destroyedCount = this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx2, troopIdx2, 2);

				this.owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2] += (short)destroyedCount;
				this.owner.UnitOps.VisitEachUnitInStack(participantIdx2, troopIdx2, 0xf10);

				if ((short)destroyedCount > 1 &&
					(participantIdx1 == this.owner.World.PersonParticipantIdx || participantIdx2 == this.owner.World.PersonParticipantIdx))
				{
					EmptyMessage()
						.Number((short)destroyedCount)
						.Text(" units destroyed.\n")
						.Dialog(80, 80);
				}
			}
			else
			{
				if (townIdx != -1 && (byte)PullUnitByte(participantIdx2, troopIdx2, 0xa) == 0xff)
				{
					for (short dir = 8; dir >= 0; dir--)
					{

						short checkX = (short)((byte)PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement) +
							(short)DirectionHoriz(dir));

						short checkY = (short)((byte)PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement) +
							(short)DirectionVert(dir));

						ushort unitAtTile = this.owner.MapOps.ObtainUnitAtTile(checkX, checkY);

						if (unitAtTile != 0xffff && (short)FoundPlayer != this.owner.World.PersonParticipantIdx)
						{
							this.owner.UnitOps.VisitEachUnitInStack((short)FoundPlayer, (short)unitAtTile, 0x1593);
						}
					}


					if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
					{
						this.owner.Bot.PostCityDefenders(townIdx);
					}
				}

				this.owner.UnitOps.EliminateUnit(participantIdx2, troopIdx2);
				this.owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2]++;
			}

			if (this.owner.Libc.Dice.DrawInt(2) != 0)
			{
				PromoteUnitToVeteran(participantIdx1, troopIdx1);
			}
		}

		private void PromoteUnitToVeteran(short participantIdx, short troopIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | (byte)UnitStatusEnum.Veteran));
		}

		/// <summary>
		/// Puts the unit into the stack of another unit of the same player, right after it. The units of a stack form a ring
		/// through the FollowingInStack field; a unit alone has -1 there.
		/// </summary>
		/// <param name="stackTroopIdx">A unit of the stack</param>
		public void AttachUnitToStack(short participantIdx, short troopIdx, ushort stackTroopIdx)
		{
			this.processor.Journal.OpenScope($"AttachUnitToStack({participantIdx}, {troopIdx}, {stackTroopIdx})");

			if (this.troops.Following(participantIdx, stackTroopIdx) == -1)
			{
				// Two units: each follows the other
				this.troops.AssignFollowing(participantIdx, stackTroopIdx, (byte)troopIdx);
				this.troops.AssignFollowing(participantIdx, troopIdx, (byte)stackTroopIdx);
			}
			else
			{
				this.troops.AssignFollowing(participantIdx, troopIdx, (byte)this.troops.Following(participantIdx, stackTroopIdx));
				this.troops.AssignFollowing(participantIdx, stackTroopIdx, (byte)troopIdx);
			}

			this.processor.Journal.CloseScope("AttachUnitToStack");
		}

		/// <summary>
		/// Takes the unit out of its stack (the ring of FollowingInStack), the unit stands alone afterwards.
		/// Like the original, the unit before it is searched in at most 32 steps.
		/// </summary>
		public void DiscardUnitFromStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DiscardUnitFromStack({participantIdx}, {troopIdx})");

			int following = this.troops.Following(participantIdx, troopIdx);

			if (following != -1)
			{
				if (this.troops.Following(participantIdx, following) == troopIdx)
				{
					// A stack of two, the other unit stays alone
					this.troops.AssignFollowing(participantIdx, following, 0xff);
				}
				else
				{
					int previous = following;
					int steps = 0;

					do
					{
						previous = this.troops.Following(participantIdx, previous);
						steps++;
					}
					while (steps < 32 && previous != -1 && this.troops.Following(participantIdx, previous) != troopIdx);

					if (steps < 32 && previous != -1)
					{
						this.troops.AssignFollowing(participantIdx, previous, (byte)this.troops.Following(participantIdx, troopIdx));
					}
				}

				this.troops.AssignFollowing(participantIdx, troopIdx, 0xff);
			}

			this.processor.Journal.CloseScope("DiscardUnitFromStack");
		}

		/// <summary>
		/// Before the human player attacks a player with whom it has a peace treaty (Relations bit 1): asks whether to break the
		/// treaty; the Senate overrules it under governments 4 and up
		/// </summary>
		/// <returns>0 without a treaty, 1 when the treaty is broken, 0xffff when the action is cancelled (also in AX)</returns>
		public ushort AffirmBreakTreaty(short participantIdx)
		{
			this.processor.Journal.OpenScope($"AffirmBreakTreaty({participantIdx})");

			Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];
			ushort affirmed = 0;

			if ((person.Relations[participantIdx] & 2) != 0)
			{
				Message("We have signed a\npeace treaty with\nthe ")
					.Text(CivilizationPlural(participantIdx))
					.Text("!\n Cancel action.\n Break treaty.\n")
					.Style(0x5)
					.Dialog(80, 80);

				affirmed = 0xffff;

				if (this.processor.RegAcc.Value16 == 1)
				{
					if (person.RegimeKind < 4)
					{
						affirmed = 1;
					}
					else
					{
						Message("Overruled by\nthe Senate.\nAction canceled.\n")
							.Style(0x4)
							.Dialog(100, 80);
					}
				}
			}

			this.processor.RegAcc.Value16 = affirmed;

			this.processor.Journal.CloseScope("AffirmBreakTreaty");

			return affirmed;
		}

		/// <summary>
		/// A nuclear attack by the player on the tile: a message "... use nuclear weapons!" (unless the human attacks), the
		/// explosion, then on the tile and the 8 around it (headings 8 down to 0) every unit stack is destroyed (counted in
		/// TroopsRuined of the attacker, the victim 0xd7f0 turns against it) unless the tile is a city with SDI (structure bit
		/// 0 of the second word); last the fallout. The stack frame and the saved SI stay, and SI holds 2 * heading in the
		/// loop, like in the original: a called function copies SI into DI, which the caller gets back.
		/// </summary>
		public void ExplodeNuclearWeapon(short participantIdx, short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ExplodeNuclearWeapon({participantIdx}, {horizLoc}, {vertLoc})");

			this.processor.EnterFrame(0xc);
			this.processor.PreserveSource();

			MapLayers chart = this.owner.MapOps.Chart;

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				DialogStyle = 0x3;

				Message(CivilizationPlural(participantIdx))
					.Text(" use\nnuclear weapons!\n")
					.Dialog(100, 80);
			}

			this.owner.Diplomats.RenderExplosion(horizLoc, vertLoc);

			for (int heading = 8; heading >= 0; heading--)
			{
				this.processor.RegSource.Value16 = (ushort)(heading << 1);

				short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
				short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

				ushort troopIdx = this.owner.MapOps.ObtainUnitAtTile(tileHoriz, tileVert);

				if ((this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert) & 0x1) != 0)
				{
					short townIdx = (short)this.owner.Helpers.LocateCityAtPosition(tileHoriz, tileVert);

					if (this.owner.World.Towns[(ushort)townIdx].Has(CityStructure.SdiDefense))
					{
						Message("SDI protects ")
							.City(townIdx)
							.Text(".\n")
							.Dialog(80, 100);

						troopIdx = 0xffff;
					}
				}

				if (troopIdx == 0xffff)
					continue;

				ushort ruined = this.owner.UnitOps.TallyUnitsByDomainInStack((short)FoundPlayer, (short)troopIdx, 2);

				this.owner.World.Participants[participantIdx].TroopsRuined[FoundPlayer] += (short)ruined;

				this.owner.UnitOps.VisitEachUnitInStack((short)FoundPlayer, (short)troopIdx, 0xf10);

				ushort victimIdx = FoundPlayer;

				if ((ushort)participantIdx != victimIdx)
				{
					this.owner.World.Participants[victimIdx].Relations[participantIdx] |= 0x88;
				}
			}

			EnactNuclearFallout(horizLoc, vertLoc);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ExplodeNuclearWeapon");
		}

		/// <summary>
		/// The fallout of a nuclear attack on the tile and the 8 tiles around it (direction tables 0x1882 and 0x18e4): a city
		/// there loses half of its size, any other tile gets pollution with chance 2 in 3 (unless its terrain is 10); every tile
		/// is drawn again
		/// </summary>
		public void EnactNuclearFallout(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"EnactNuclearFallout({horizLoc}, {vertLoc})");

			MapLayers chart = this.owner.MapOps.Chart;

			for (int heading = 0; heading <= 8; heading++)
			{
				int tileHoriz = this.owner.Routing.TunePosition(chart.DirectionHoriz(heading) + horizLoc);
				int tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

				if ((this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert) & 0x1) != 0)
				{
					City town = this.owner.World.Towns[this.owner.Helpers.LocateCityAtPosition(tileHoriz, tileVert)];

					town.TrueExtent -= (sbyte)(town.TrueExtent / 2);
				}
				else if (this.owner.Libc.Dice.DrawInt(3) != 0 && this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert) != 0xa)
				{
					this.owner.CityOps.AttachSmog(tileHoriz, tileVert);
				}

				this.owner.MapOps.RepaintMapTile(tileHoriz, tileVert);
			}

			this.processor.Journal.CloseScope("EnactNuclearFallout");
		}
	}
}
