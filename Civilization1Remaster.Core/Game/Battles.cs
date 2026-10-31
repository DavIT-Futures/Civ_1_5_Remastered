using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x29f3.
	/// </summary>
	public class Battles : GameComponent, IBattles
	{
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

		// Pauses of the battle animation (in timer ticks): before the first round and after each round
		private const int FightBeginWait = 20;

		private const int FightPhaseWait = 18;

		private readonly UnitMemory troops;

		// The last battle, for the battle animation
		private CombatResolver.Battle? finalFight;

		private int finalAssailantCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;
		private int finalGuardianCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;

		public Battles(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <inheritdoc/>
		public ushort SettleBattle(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"SettleBattle({participantIdx1}, {troopIdx1}, {participantIdx2}, {troopIdx2}, {marker})");


			int assailantKind = PullUnitByte(participantIdx1, troopIdx1, TroopKindDisplacement);
			int guardianKind = PullUnitByte(participantIdx2, troopIdx2, TroopKindDisplacement);
			int guardianHoriz = PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement);
			int guardianVert = PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement);
			int guardianLand = this.Owner.MapOps.ObtainTerrainType(guardianHoriz, guardianVert);
			int landGuard = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(LandGuardLookup + guardianLand * LandExtent));
			bool raidersAssaultTown = false;

			// Attack and defense strengths are scaled by 8, terrain defense factor of 2 is the normal (100%) value
			int assault = ObtainUnitAttack(assailantKind) * 8;
			bool stronghold = (this.Owner.MapOps.ObtainTileImprovements(guardianHoriz, guardianVert) & 0x20) != 0;
			bool dugin = (PullUnitByte(participantIdx2, troopIdx2, TroopConditionDisplacement) & (int)UnitStatusEnum.Fortified) != 0;
			bool townRamparts = false;

			if (participantIdx1 == 0)
			{
				// Barbarians attack
				if (participantIdx2 == this.Owner.World.PersonParticipantIdx)
				{
					assault = assault * (this.Owner.World.HardnessTier + 1) / 4;
				}
				else
				{
					assault /= 2;
				}
			}

			int guardianGroup = ObtainUnitTerrainCategory(guardianKind);
			int townIdx = -1;

			if ((this.Owner.MapOps.ObtainTileImprovements(guardianHoriz, guardianVert) & 0x1) != 0)
			{
				// Defender is in a city
				townIdx = (short)this.Owner.Helpers.FindCityAtPosition(guardianHoriz, guardianVert);
				ushort townStructures = this.Owner.World.Towns[townIdx].StructureMarkers0;

				townRamparts = (townStructures & TownRampartsMarker) != 0;

				if (participantIdx1 == 0)
				{
					// Barbarians can't capture the last city of a player
					if (this.Owner.World.Participants[participantIdx2].TownTally == 1)
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
				return (ushort)chances;
			}

			if (participantIdx1 != 0)
			{
				// Difficulty adjustments: Chieftain and Warlord halve AI attack against the human player
				if (this.Owner.World.HardnessTier <= 1 && participantIdx2 == this.Owner.World.PersonParticipantIdx)
				{
					assault /= 2;
				}

				// Chieftain doubles human player attack
				if (this.Owner.World.HardnessTier == 0 && participantIdx1 == this.Owner.World.PersonParticipantIdx)
				{
					assault *= 2;
				}
			}


			if (participantIdx1 == this.Owner.World.PersonParticipantIdx)
			{
				if (AffirmBreakTreaty(participantIdx2) == 0xffff)
				{
					this.troops.Assign(participantIdx1, troopIdx1, UnitMemory.TravelHoriz, 0xff);
					return 0xffff;
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
				return 1;
			}

			if (assailantWins)
			{
				ResolveAttackerVictory(participantIdx1, troopIdx1, participantIdx2, troopIdx2, (short)townIdx);
				return 1;
			}

			this.Owner.World.Participants[participantIdx2].TroopsRuined[participantIdx1]++;
			this.Owner.UnitOps.DestroyUnit(participantIdx1, troopIdx1);

			if (this.Owner.Libc.Dice.DrawInt(2) != 0)
			{
				PromoteUnitToVeteran(participantIdx2, troopIdx2);
			}

			return 0;
		}

		/// <inheritdoc/>
		public void AttachUnitToStack(short participantIdx, short troopIdx, ushort stackTroopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"AttachUnitToStack({participantIdx}, {troopIdx}, {stackTroopIdx})");

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
		}

		/// <inheritdoc/>
		public void RemoveUnitFromStack(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DiscardUnitFromStack({participantIdx}, {troopIdx})");

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
		}

		/// <inheritdoc/>
		public ushort AffirmBreakTreaty(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"AffirmBreakTreaty({participantIdx})");

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];
			ushort affirmed = 0;

			if ((person.Relations[participantIdx] & 2) != 0)
			{
				ushort choice = Message("We have signed a\npeace treaty with\nthe ")
					.Text(CivilizationPlural(participantIdx))
					.Text("!\n Cancel action.\n Break treaty.\n")
					.Style(0x5)
					.Dialog(80, 80);

				affirmed = 0xffff;

				if (choice == 1)
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

			return affirmed;
		}

		/// <inheritdoc/>
		public void ExplodeNuclearWeapon(short participantIdx, short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ExplodeNuclearWeapon({participantIdx}, {horizLoc}, {vertLoc})");


			IMapPlanes chart = this.Owner.MapOps.Chart;

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				DialogStyle = 0x3;

				Message(CivilizationPlural(participantIdx))
					.Text(" use\nnuclear weapons!\n")
					.Dialog(100, 80);
			}

			this.Owner.Diplomats.DrawExplosion(horizLoc, vertLoc);

			for (int heading = 8; heading >= 0; heading--)
			{
				short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
				short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

				ushort troopIdx = this.Owner.MapOps.ObtainUnitAtTile(tileHoriz, tileVert);

				if ((this.Owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert) & 0x1) != 0)
				{
					short townIdx = (short)this.Owner.Helpers.FindCityAtPosition(tileHoriz, tileVert);

					if (this.Owner.World.Towns[(ushort)townIdx].Has(CityStructure.SdiDefense))
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

				ushort ruined = this.Owner.UnitOps.CountUnitsByDomainInStack((short)FoundPlayer, (short)troopIdx, 2);

				this.Owner.World.Participants[participantIdx].TroopsRuined[FoundPlayer] += (short)ruined;

				this.Owner.UnitOps.VisitEachUnitInStack((short)FoundPlayer, (short)troopIdx, 0xf10);

				ushort victimIdx = FoundPlayer;

				if ((ushort)participantIdx != victimIdx)
				{
					this.Owner.World.Participants[victimIdx].Relations[participantIdx] |= 0x88;
				}
			}

			EnactNuclearFallout(horizLoc, vertLoc);
		}

		/// <inheritdoc/>
		public void EnactNuclearFallout(short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"EnactNuclearFallout({horizLoc}, {vertLoc})");

			IMapPlanes chart = this.Owner.MapOps.Chart;

			for (int heading = 0; heading <= 8; heading++)
			{
				int tileHoriz = this.Owner.Routing.TunePosition(chart.DirectionHoriz(heading) + horizLoc);
				int tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

				if ((this.Owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert) & 0x1) != 0)
				{
					City town = this.Owner.World.Towns[this.Owner.Helpers.FindCityAtPosition(tileHoriz, tileVert)];

					town.TrueExtent -= (sbyte)(town.TrueExtent / 2);
				}
				else if (this.Owner.Libc.Dice.DrawInt(3) != 0 && this.Owner.MapOps.ObtainTerrainType(tileHoriz, tileVert) != 0xa)
				{
					this.Owner.CityOps.AttachSmog(tileHoriz, tileVert);
				}

				this.Owner.MapOps.RepaintMapTile(tileHoriz, tileVert);
			}
		}

		/// <summary>
		/// Shows the rounds of the last battle: green squares (hit points) over both units,
		/// a square of the unit that lost the round becomes empty.
		/// </summary>
		private void ShowBattleRounds(int assailantHoriz, int assailantVert, int guardianHoriz, int guardianVert)
		{
			CombatResolver.Battle? fight = this.finalFight;

			if (fight == null)
				return;

			int assailantStrikeMarks = fight.AssailantBeginStrikeMarks;
			int guardianStrikeMarks = fight.GuardianBeginStrikeMarks;

			this.Owner.MapOps.DrawHealthBar(assailantHoriz, assailantVert, this.finalAssailantCompleteStrikeMarks, assailantStrikeMarks);
			this.Owner.MapOps.DrawHealthBar(guardianHoriz, guardianVert, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);
			this.Owner.TextPainter.WaitForTime(FightBeginWait);

			for (int idx = 0; idx < fight.Phases.Count; idx++)
			{
				if (fight.Phases[idx])
				{
					guardianStrikeMarks--;
					this.Owner.MapOps.DrawHealthBar(guardianHoriz, guardianVert, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);

					if (guardianStrikeMarks == 0 && idx + 1 < fight.Phases.Count)
					{
						// Barbarians attacking a city: the city defends again
						this.Owner.TextPainter.WaitForTime(FightBeginWait);
						guardianStrikeMarks = fight.GuardianBeginStrikeMarks;
						this.Owner.MapOps.DrawHealthBar(guardianHoriz, guardianVert, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);
					}
				}
				else
				{
					assailantStrikeMarks--;
					this.Owner.MapOps.DrawHealthBar(assailantHoriz, assailantVert, this.finalAssailantCompleteStrikeMarks, assailantStrikeMarks);
				}

				this.Owner.TextPainter.WaitForTime(FightPhaseWait);
			}
		}

		private int PullUnitByte(int participantIdx, int troopIdx, int displacement)
		{
			return this.troops.PullSigned(participantIdx, troopIdx, displacement);
		}

		private int PullUnitDefinitionWord(int troopKind, int displacement)
		{
			return (short)this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(TroopSpecsRoot + troopKind * TroopSpecExtent + displacement));
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

		private bool ExecuteBattleClash(
			short participantIdx1, short troopIdx1,
			short participantIdx2, short troopIdx2,
			ushort assaultAmount, ushort guardAmount,
			bool townAssaultedViaRaiders)
		{
			Func<int, int> followingChance = peakAmount => this.Owner.Libc.Dice.DrawInt(peakAmount);
			UnitHealth vitality = this.Owner.World.Vitals;

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
			short personIdx = this.Owner.World.PersonParticipantIdx;
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
			this.Owner.UnitOps.PanMapToPosition(
				this.Owner.World.PersonParticipantIdx,
				PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
				PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement));

			this.Owner.Startup.HideMouseCursor();

			ushort tileVisible = this.Owner.MapOps.DrawMapTile(
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
				else if ((assailantWins && participantIdx1 == this.Owner.World.PersonParticipantIdx) ||
					(!assailantWins && participantIdx2 == this.Owner.World.PersonParticipantIdx))
				{
					soundId = modernWeapon != 0 ? (ushort)0x28 : (ushort)0x26;
				}
				else
				{
					soundId = modernWeapon != 0 ? (ushort)0x29 : (ushort)0x27;
				}

				this.Owner.Services.PerformSound(soundId, 0);

				this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx2, troopIdx2);

				byte nextInStack = (byte)PullUnitByte(participantIdx1, troopIdx1, 0xa);
				if (nextInStack != 0xff)
				{
					this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx1, (short)PullUnitByte(participantIdx1, troopIdx1, 0xa));
				}

				this.Owner.VideoCard.RenderImage(
					ScreenBox,
					0x50, 0, 0xf0, 0xc8,
					BackScreenBox,
					0x50, 0);

				short assailantScreenX = (short)((this.Owner.Routing.TunePosition(
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

					ushort unitSprite = this.Processor.PullUInt16(
						this.Processor.SegData.Value16,
						(ushort)((((byte)PullUnitByte(participantIdx1, troopIdx1, TroopKindDisplacement) +
							(participantIdx1 << 5) + 0x40) << 1) + 0xd4ce));

					this.Owner.Services.DrawPictureToScreen(
						ScreenBox,
						drawX + 1,
						drawY + 1,
						unitSprite);

					this.Owner.TextPainter.WaitForTime(2);

					this.Owner.VideoCard.RenderImage(
						BackScreenBox,
						(ushort)drawX, (ushort)drawY, 16, 16,
						ScreenBox,
						(ushort)drawX, (ushort)drawY);
				}


				this.Owner.MapOps.RepaintMapTile(
					PullUnitByte(participantIdx1, troopIdx1, TroopHorizDisplacement),
					PullUnitByte(participantIdx1, troopIdx1, TroopVertDisplacement));

				this.Owner.MapOps.RepaintMapTile(
					PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
					PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement));

				this.Owner.VideoCard.RenderImage(
					ScreenBox,
					0, 0, 0x100, 0xc8,
					BackScreenBox,
					0, 0);

				int guardianDisplayHoriz = assailantScreenX + 16 * stepX;
				int guardianDisplayVert = assailantScreenY + 16 * stepY;
				ShowBattleRounds(assailantScreenX, assailantScreenY, guardianDisplayHoriz, guardianDisplayVert);

				short loserStep = (short)(assailantWins ? 0x10 : 0);
				for (short frame = 0; frame < 8; frame++)
				{
					short loserX = (short)(assailantScreenX + loserStep * stepX);
					short loserY = (short)(assailantScreenY + loserStep * stepY);

					this.Owner.Services.DrawPictureToScreen(
						ScreenBox,
						loserX + 1,
						loserY + 1,
						SpritePicture((frame + 0x20)));

					this.Owner.TextPainter.WaitForTime(4);

					this.Owner.VideoCard.RenderImage(
						BackScreenBox,
						(ushort)loserX, (ushort)loserY, 16, 16,
						ScreenBox,
						(ushort)loserX, (ushort)loserY);
				}


				int victorHoriz = assailantScreenX + (assailantWins ? 0 : 16 * stepX);
				int victorVert = assailantScreenY + (assailantWins ? 0 : 16 * stepY);

				this.Owner.VideoCard.RenderImage(
					BackScreenBox,
					victorHoriz, victorVert, 16, 16,
					ScreenBox,
					victorHoriz, victorVert);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		private void UpdateBattleDiplomacy(short participantIdx1, short participantIdx2)
		{
			this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] |= 0x20;
			this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 0x20;

			if (participantIdx1 != 0 && participantIdx2 != 0)
			{
				this.Owner.World.TruceRoundTally = 0;
			}

			short personIdx = this.Owner.World.PersonParticipantIdx;
			if ((participantIdx2 == personIdx && (this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0) ||
				(this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] & 0x200) != 0)
			{
				DialogStyle = 0x3;

				Message("Sneak attack by\n")
					.Text(CivilizationName(participantIdx1))
					.Text(" forces!\n")
					.Dialog(100, 80);

				this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xfdff;
			}

			if ((this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0 ||
				(this.Owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
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

				if (participantIdx1 == personIdx && (this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0)
				{
					this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 8;
				}

				this.Owner.Politics.ClearDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}

			if (participantIdx1 == personIdx)
			{
				this.Owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xfdff;
			}
		}

		private void ResolveAttackerVictory(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, short townIdx)
		{
			if (participantIdx2 == 0 && (byte)PullUnitByte(participantIdx2, troopIdx2, TroopKindDisplacement) == 0x1a)
			{
				this.Owner.World.Participants[participantIdx1].Gold += 100;

				if (participantIdx1 == this.Owner.World.PersonParticipantIdx)
				{
					this.Owner.Words.Clear();
					this.Owner.BlockText.ShowTextDialog(GameTexts.TopicLeader);
					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);
				}
			}

			if (townIdx == -1 &&
				(this.Owner.MapOps.ObtainTileImprovements(
					PullUnitByte(participantIdx2, troopIdx2, TroopHorizDisplacement),
					PullUnitByte(participantIdx2, troopIdx2, TroopVertDisplacement)) & 0x20) == 0)
			{
				ushort destroyedCount = this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx2, troopIdx2, 2);

				this.Owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2] += (short)destroyedCount;
				this.Owner.UnitOps.VisitEachUnitInStack(participantIdx2, troopIdx2, 0xf10);

				if ((short)destroyedCount > 1 &&
					(participantIdx1 == this.Owner.World.PersonParticipantIdx || participantIdx2 == this.Owner.World.PersonParticipantIdx))
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

						ushort unitAtTile = this.Owner.MapOps.ObtainUnitAtTile(checkX, checkY);

						if (unitAtTile != 0xffff && (short)FoundPlayer != this.Owner.World.PersonParticipantIdx)
						{
							this.Owner.UnitOps.VisitEachUnitInStack((short)FoundPlayer, (short)unitAtTile, 0x1593);
						}
					}


					if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
					{
						this.Owner.Bot.PostCityDefenders(townIdx);
					}
				}

				this.Owner.UnitOps.DestroyUnit(participantIdx2, troopIdx2);
				this.Owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2]++;
			}

			if (this.Owner.Libc.Dice.DrawInt(2) != 0)
			{
				PromoteUnitToVeteran(participantIdx1, troopIdx1);
			}
		}

		private void PromoteUnitToVeteran(short participantIdx, short troopIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | (byte)UnitStatusEnum.Veteran));
		}
	}
}
