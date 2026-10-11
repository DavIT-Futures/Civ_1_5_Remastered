using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Pure C# domain logic and battle animation integration for unit combat.
	/// Translated from original CIV.EXE segment 0x29f3.
	/// </summary>
	public class UnitCombat
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public UnitCombat(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		private const int TroopsRoot = 0x81d4;
		private const int TroopExtent = 0xc;
		private const int ParticipantTroopsExtent = 0x600;

		private const int TroopGroupSky = 1;
		private const int TroopGroupWater = 2;

		private const int LandGuardLookup = 0x28f;
		private const int LandExtent = 0x13;

		private const int CastleMarker = 0x1;
		private const int TownRampartsMarker = 0x80;

		// The last battle, for the battle animation
		private CombatResolver.Battle? finalFight = null;
		private int finalAssailantCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;
		private int finalGuardianCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;

		// Pauses of the battle animation (in timer ticks)
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

		private int ObtainUnitAttack(int troopKind)
		{
			if (troopKind >= 0 && troopKind < this.owner.World.TroopSpecs.Length)
			{
				return this.owner.World.TroopSpecs[troopKind].AssaultPower;
			}
			return 0;
		}

		private int ObtainUnitProtection(int troopKind)
		{
			if (troopKind >= 0 && troopKind < this.owner.World.TroopSpecs.Length)
			{
				return this.owner.World.TroopSpecs[troopKind].GuardPower;
			}
			return 0;
		}

		private int ObtainUnitTerrainCategory(int troopKind)
		{
			if (troopKind >= 0 && troopKind < this.owner.World.TroopSpecs.Length)
			{
				return this.owner.World.TroopSpecs[troopKind].LandGroup;
			}
			return 0;
		}

		/// <summary>
		/// Resolves a battle between an attacking and defending unit or estimates combat odds.
		/// </summary>
		public ushort SettleBattle(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, ushort marker)
		{
			this.processor.Journal.OpenScope($"SettleBattle({participantIdx1}, {troopIdx1}, {participantIdx2}, {troopIdx2}, {marker})");

			var assailantUnit = this.owner.World.Participants[participantIdx1].Troops[troopIdx1];
			var guardianUnit = this.owner.World.Participants[participantIdx2].Troops[troopIdx2];

			int assailantKind = assailantUnit.KindIdx;
			int guardianKind = guardianUnit.KindIdx;
			int guardianHoriz = guardianUnit.Place.X;
			int guardianVert = guardianUnit.Place.Y;
			int guardianLand = this.owner.MapOps.ObtainTerrainType(guardianHoriz, guardianVert);
			int landGuard = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(LandGuardLookup + guardianLand * LandExtent));
			bool raidersAssaultTown = false;

			// Attack and defense strengths are scaled by 8, terrain defense factor of 2 is the normal (100%) value
			int assault = ObtainUnitAttack(assailantKind) * 8;
			bool stronghold = (this.owner.MapOps.ObtainTileImprovements(guardianHoriz, guardianVert) & 0x20) != 0;
			bool dugin = (guardianUnit.Condition & (int)UnitStatusEnum.Fortified) != 0;
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
			if ((assailantUnit.Condition & (int)UnitStatusEnum.Veteran) != 0)
			{
				assault += assault / 2;
			}

			// Terrain, fortified, fortress, City Walls and veteran bonuses
			int guard = DefenseStrength.Compute(ObtainUnitProtection(guardianKind), landGuard, stronghold, dugin,
				guardianGroup == TroopGroupWater, guardianGroup == TroopGroupSky, townRamparts,
				ObtainUnitAttack(assailantKind),
				(guardianUnit.Condition & (int)UnitStatusEnum.Veteran) != 0);

			// Attacker with less than one full move left attacks with reduced strength
			sbyte assailantSteps = (sbyte)assailantUnit.LeftoverSteps;
			assault = CombatRules.AdjustForRemainingSteps(assault, assailantSteps);

			if (marker == 0)
			{
				// Only estimate the odds (used by AI), don't fight
				var (chances, leftover) = CombatRules.EstimateOdds(assault, guard);
				this.processor.RegAcc.Value16 = (ushort)chances;
				this.processor.RegData.Value16 = (ushort)leftover;
				this.processor.Journal.CloseScope("ResolveBattle");
				return (ushort)chances;
			}

			assault = CombatRules.AdjustAttackerStrength(assault, participantIdx1, participantIdx2,
				this.owner.World.PersonParticipantIdx, this.owner.World.HardnessTier);

			if (participantIdx1 == this.owner.World.PersonParticipantIdx)
			{
				AffirmBreakTreaty(participantIdx2);
				if (this.processor.RegAcc.Value16 == 0xffff)
				{
					assailantUnit.TravelTowardPlace.X = -1;
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(TroopsRoot + participantIdx1 * ParticipantTroopsExtent + troopIdx1 * TroopExtent + 6), 0xff);
					this.processor.RegAcc.Value16 = 0xffff;
					this.processor.Journal.CloseScope("ResolveBattle");
					return 0xffff;
				}
			}

			// Resolve the battle using CombatResolver
			bool townAssaultedViaRaiders = raidersAssaultTown;
			Func<int, int> followingChance = peakAmount => this.owner.Libc.Dice.DrawInt(peakAmount);
			UnitHealth vitality = this.owner.World.Vitals;

			bool assailantSeasoned = (assailantUnit.Condition & (int)UnitStatusEnum.Veteran) != 0;
			bool guardianSeasoned = (guardianUnit.Condition & (int)UnitStatusEnum.Veteran) != 0;
			bool assailantAce = vitality.VerifyElite(participantIdx1, troopIdx1, assailantKind);
			bool guardianAce = vitality.VerifyElite(participantIdx2, troopIdx2, guardianKind);

			CombatResolver.Battle fight = CombatResolver.Clash((ushort)assault, (ushort)guard,
				vitality.ObtainHitPoints(participantIdx1, troopIdx1, assailantKind, assailantSeasoned),
				vitality.ObtainHitPoints(participantIdx2, troopIdx2, guardianKind, guardianSeasoned),
				townAssaultedViaRaiders ? 2 : 1, followingChance);

			this.finalFight = fight;
			this.finalAssailantCompleteStrikeMarks = CombatResolver.TopHitPoints(assailantSeasoned, assailantAce);
			this.finalGuardianCompleteStrikeMarks = CombatResolver.TopHitPoints(guardianSeasoned, guardianAce);

			if (fight.AssailantVictories)
			{
				bool ace = assailantAce || (assailantSeasoned && fight.Phases.Count > 0);
				vitality.Assign(participantIdx1, troopIdx1, assailantKind,
					CombatResolver.TopHitPoints(assailantSeasoned, ace) - fight.AssailantStrikeMarks, ace);
				vitality.Wipe(participantIdx2, troopIdx2);
			}
			else
			{
				bool ace = guardianAce || (guardianSeasoned && fight.Phases.Count > 0);
				vitality.Assign(participantIdx2, troopIdx2, guardianKind,
					CombatResolver.TopHitPoints(guardianSeasoned, ace) - fight.GuardianStrikeMarks, ace);
				vitality.Wipe(participantIdx1, troopIdx1);
			}

			// Battle visual rendering and sound
			short humanPlayer = this.owner.World.PersonParticipantIdx;
			bool shouldAnimate = (participantIdx1 == humanPlayer || participantIdx2 == humanPlayer);
			if (!shouldAnimate)
			{
				ushort sightMask = (ushort)(1 << humanPlayer);
				bool attackerVisible = (assailantUnit.ShownViaParticipant & sightMask) != 0;
				bool defenderVisible = (guardianUnit.ShownViaParticipant & sightMask) != 0;
				shouldAnimate = attackerVisible || defenderVisible || (this.processor.PullGlobalWord(0xd806) != 0);
			}

			if (shouldAnimate && this.processor.PullGlobalWord(0xb278) != 1)
			{
				int defX = guardianUnit.Place.X;
				int defY = guardianUnit.Place.Y;
				int attX = assailantUnit.Place.X;
				int attY = assailantUnit.Place.Y;

				this.owner.UnitOps.PanMapToPosition(humanPlayer, (sbyte)defX, (sbyte)defY);
				this.owner.Startup.ConcealMouseCursor();

				if (this.owner.MapOps.RenderMapTile((sbyte)attX, (sbyte)attY) != 0)
				{
					bool modernUnitSound = false;
					if (fight.AssailantVictories)
					{
						if (this.owner.World.TroopSpecs[assailantKind].AssaultPower > 4 || this.owner.World.TroopSpecs[assailantKind].GuardPower > 2)
							modernUnitSound = true;
					}
					else
					{
						if (this.owner.World.TroopSpecs[guardianKind].AssaultPower > 4 || this.owner.World.TroopSpecs[guardianKind].GuardPower > 2)
							modernUnitSound = true;
					}

					short sound;
					if (assailantKind == 0xf)
					{
						sound = 0x2b;
					}
					else if (fight.AssailantVictories)
					{
						if (participantIdx1 == humanPlayer)
							sound = modernUnitSound ? (short)0x28 : (short)0x26;
						else
							sound = modernUnitSound ? (short)0x29 : (short)0x27;
					}
					else
					{
						if (participantIdx2 == humanPlayer)
							sound = modernUnitSound ? (short)0x28 : (short)0x26;
						else
							sound = modernUnitSound ? (short)0x29 : (short)0x27;
					}
					this.owner.Services.PerformSound((ushort)sound, 0);

					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx2, troopIdx2);

					short followingTroop = assailantUnit.FollowingTroopIdx;
					if (followingTroop != -1)
					{
						this.owner.MapOps.RenderUnitOnWorldMap(participantIdx1, (sbyte)followingTroop);
					}

					ushort screen0 = this.processor.PullGlobalWord(0xaa);
					ushort screen1 = this.processor.PullGlobalWord(0x19d4);

					this.owner.VideoCard.RenderImage(screen0, 80, 0, 240, 200, screen1, 80, 0);

					int screenAttX = (this.owner.Routing.TunePosition(attX - this.processor.PullGlobalSignedWord(0xd4cc)) << 4) + 80;
					int screenAttY = ((attY - this.processor.PullGlobalWord(0xd75e)) << 4) + 8;

					int dirX = Math.Sign(defX - attX);
					int dirY = Math.Sign(defY - attY);

					if (defX == 0 && attX == 79) dirX = 1;
					if (defX == 79 && attX == 0) dirX = -1;

					for (int step = 0; step <= 10; step++)
					{
						int px = screenAttX + step * dirX;
						int py = screenAttY + step * dirY;

						ushort spriteOffset = (ushort)(((assailantKind + (participantIdx1 << 5) + 0x40) << 1) + 0xd4ce);
						ushort sprite = this.processor.PullUInt16(this.processor.SegData.Value16, spriteOffset);

						this.owner.Services.RenderPictureToScreen(screen0, (short)(px + 1), (short)(py + 1), sprite);
						this.owner.TextPainter.AwaitTime(2);
						this.owner.VideoCard.RenderImage(screen1, px, py, 16, 16, screen0, px, py);
					}

					this.owner.MapOps.RepaintMapTile((sbyte)attX, (sbyte)attY);
					this.owner.MapOps.RepaintMapTile((sbyte)defX, (sbyte)defY);
					this.owner.VideoCard.RenderImage(screen0, 0, 0, 256, 200, screen1, 0, 0);

					int guardianDisplayX = screenAttX + 16 * dirX;
					int guardianDisplayY = screenAttY + 16 * dirY;
					DisplayBattleRounds(screenAttX, screenAttY, guardianDisplayX, guardianDisplayY);

					int explodeX = screenAttX + (fight.AssailantVictories ? 16 : 0) * dirX;
					int explodeY = screenAttY + (fight.AssailantVictories ? 16 : 0) * dirY;

					for (int step = 0; step < 8; step++)
					{
						ushort explosionSprite = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(((step + 0x20) << 1) + 0xd4ce));
						this.owner.Services.RenderPictureToScreen(screen0, (short)(explodeX + 1), (short)(explodeY + 1), explosionSprite);
						this.owner.TextPainter.AwaitTime(4);
						this.owner.VideoCard.RenderImage(screen1, explodeX, explodeY, 16, 16, screen0, explodeX, explodeY);
					}

					int victorX = screenAttX + (fight.AssailantVictories ? 0 : 16 * dirX);
					int victorY = screenAttY + (fight.AssailantVictories ? 0 : 16 * dirY);
					this.owner.VideoCard.RenderImage(screen1, victorX, victorY, 16, 16, screen0, victorX, victorY);
				}

				this.owner.Startup.DisplayMouseCursor();
			}

			this.owner.World.Participants[participantIdx1].Relations[participantIdx2] |= 0x20;
			this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 0x20;

			if (participantIdx1 != 0 && participantIdx2 != 0)
			{
				this.owner.World.TruceRoundTally = 0;
			}

			// Sneak attack / treaty cancellation checks
			if ((participantIdx2 == humanPlayer && (this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0) ||
				(participantIdx1 != humanPlayer && (this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 0x200) != 0))
			{
				this.processor.EmitGlobalWord(0x2f9e, 0x3);
				ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1982));
				this.owner.Libc.CopyText(0xba06, "Sneak attack by\n");
				this.owner.Libc.AppendText(0xba06, civNamePtr);
				this.owner.Libc.AppendText(0xba06, " forces!\n");
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				this.owner.World.Participants[participantIdx1].Relations[participantIdx2] &= 0xfdff;
			}

			if ((this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0 ||
				(this.owner.World.Participants[participantIdx1].Relations[participantIdx2] & 2) != 0)
			{
				if (participantIdx1 == humanPlayer || participantIdx2 == humanPlayer)
				{
					this.processor.EmitGlobalWord(0x2f9e, 0x5);
					ushort p2NamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx2 << 1) + 0x1992));
					this.owner.Libc.CopyText(0xba06, p2NamePtr);
					this.owner.Libc.AppendText(0xba06, " cancel\npeace treaty\nwith ");
					ushort p1NamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1992));
					this.owner.Libc.AppendText(0xba06, p1NamePtr);
					this.owner.Libc.AppendText(0xba06, ".\n");
					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				}

				if (participantIdx1 == humanPlayer && (this.owner.World.Participants[participantIdx2].Relations[participantIdx1] & 2) != 0)
				{
					this.owner.World.Participants[participantIdx2].Relations[participantIdx1] |= 8;
				}

				this.owner.Politics.WipeDiplomacyFlags(participantIdx1, participantIdx2, 2);
			}

			if (participantIdx1 == humanPlayer)
			{
				this.owner.World.Participants[participantIdx2].Relations[participantIdx1] &= 0xfdff;
			}

			// Nuclear weapon unit
			if (assailantKind == 0x19)
			{
				ExplodeNuclearWeapon(participantIdx1, (short)guardianHoriz, (short)guardianVert);
				this.processor.RegAcc.Value16 = 1;
				this.processor.Journal.CloseScope("ResolveBattle");
				return 1;
			}

			if (fight.AssailantVictories)
			{
				// Attacker won:
				if (participantIdx2 == 0 && guardianKind == 0x1a)
				{
					this.owner.World.Participants[participantIdx1].Gold += 100;
					if (participantIdx1 == humanPlayer)
					{
						this.processor.EmitGlobalByte(0xba06, 0);
						this.owner.BlockText.DisplayTextDialog(0x2ae7);
						this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
					}
				}

				if (townIdx == -1)
				{
					if ((this.owner.MapOps.ObtainTileImprovements(guardianHoriz, guardianVert) & 0x20) == 0)
					{
						this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx2, troopIdx2, 2);
						int destroyedStackCount = (short)this.processor.RegAcc.Value16;
						this.owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2] += (short)destroyedStackCount;
						this.owner.UnitOps.VisitEachUnitInStack(participantIdx2, troopIdx2, 0xf10);

						if (destroyedStackCount > 1 && (participantIdx1 == humanPlayer || participantIdx2 == humanPlayer))
						{
							this.processor.EmitGlobalByte(0xba06, 0);
							this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)destroyedStackCount, 10));
							this.owner.Libc.AppendText(0xba06, " units destroyed.\n");
							this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
						}
					}
					else
					{
						this.owner.UnitOps.EliminateUnit(participantIdx2, troopIdx2);
						this.owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2]++;
					}
				}
				else
				{
					if (guardianUnit.FollowingTroopIdx == -1)
					{
						for (short d = 8; d >= 0; d--)
						{
							short tx = (short)this.owner.Routing.TunePosition(this.owner.TownDeltas[d].X + guardianHoriz);
							short ty = (short)(this.owner.TownDeltas[d].Y + guardianVert);
							this.owner.MapOps.ObtainUnitAtTile(tx, ty);
							short nearbyUnitIdx = (short)this.processor.RegAcc.Value16;
							if (nearbyUnitIdx != -1 && this.processor.PullGlobalWord(0xd7f0) != humanPlayer)
							{
								this.owner.UnitOps.VisitEachUnitInStack(
									(short)this.processor.PullGlobalSignedWord(0xd7f0), nearbyUnitIdx, 0x1593);
							}
						}

						if (this.owner.World.Towns[townIdx].ParticipantIdx == humanPlayer)
						{
							this.owner.Bot.PostCityDefenders((short)townIdx);
						}
					}

					this.owner.UnitOps.EliminateUnit(participantIdx2, troopIdx2);
					this.owner.World.Participants[participantIdx1].TroopsRuined[participantIdx2]++;
				}

				if (this.owner.Libc.Dice.DrawInt(2) != 0)
				{
					assailantUnit.Condition |= (short)UnitStatusEnum.Veteran;
				}

				this.processor.RegAcc.Value16 = 1;
				this.processor.Journal.CloseScope("ResolveBattle");
				return 1;
			}
			else
			{
				// Defender won:
				this.owner.World.Participants[participantIdx2].TroopsRuined[participantIdx1]++;
				this.owner.UnitOps.EliminateUnit(participantIdx1, troopIdx1);

				if (this.owner.Libc.Dice.DrawInt(2) != 0)
				{
					guardianUnit.Condition |= (short)UnitStatusEnum.Veteran;
				}

				this.processor.RegAcc.Value16 = 0;
				this.processor.Journal.CloseScope("ResolveBattle");
				return 0;
			}
		}

		/// <summary>
		/// Adds a unit to a stack headed by anchorTroopIdx.
		/// </summary>
		public void AttachUnitToStack(short participantIdx, short troopIdx, ushort anchorTroopIdx)
		{
			this.processor.Journal.OpenScope($"AttachUnitToStack({participantIdx}, {troopIdx}, {anchorTroopIdx})");

			UnitStack.AttachUnit(
				idx => (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81de + participantIdx * ParticipantTroopsExtent + idx * TroopExtent)),
				(idx, next) => this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(0x81de + participantIdx * ParticipantTroopsExtent + idx * TroopExtent), (byte)next),
				troopIdx,
				(short)anchorTroopIdx);

			this.processor.Journal.CloseScope("AttachUnitToStack");
		}

		/// <summary>
		/// Removes a unit from its stack.
		/// </summary>
		public void DiscardUnitFromStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DiscardUnitFromStack({participantIdx}, {troopIdx})");

			UnitStack.DiscardUnit(
				idx => (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81de + participantIdx * ParticipantTroopsExtent + idx * TroopExtent)),
				(idx, next) => this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(0x81de + participantIdx * ParticipantTroopsExtent + idx * TroopExtent), (byte)next),
				troopIdx);

			this.processor.Journal.CloseScope("DiscardUnitFromStack");
		}

		/// <summary>
		/// Confirms breaking a peace treaty before attacking.
		/// </summary>
		public ushort AffirmBreakTreaty(short participantIdx)
		{
			this.processor.Journal.OpenScope($"AffirmBreakTreaty({participantIdx})");

			short human = this.owner.World.PersonParticipantIdx;
			if ((this.owner.World.Participants[human].Relations[participantIdx] & 2) == 0)
			{
				this.processor.RegAcc.Value16 = 0;
				this.processor.Journal.CloseScope("AffirmBreakTreaty");
				return 0;
			}

			ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
			this.owner.Libc.CopyText(0xba06, "We have signed a\npeace treaty with\nthe ");
			this.owner.Libc.AppendText(0xba06, civNamePtr);
			this.owner.Libc.AppendText(0xba06, "!\n Cancel action.\n Break treaty.\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x5);
			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			if (this.processor.RegAcc.Value16 != 1)
			{
				this.processor.RegAcc.Value16 = 0xffff;
				this.processor.Journal.CloseScope("AffirmBreakTreaty");
				return 0xffff;
			}

			// If Republic or Democracy, Senate overrules!
			if (this.owner.World.Participants[human].RegimeKind >= 4)
			{
				this.owner.Libc.CopyText(0xba06, "Overruled by\nthe Senate.\nAction canceled.\n");
				this.processor.EmitGlobalWord(0x2f9e, 0x4);
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				this.processor.RegAcc.Value16 = 0xffff;
				this.processor.Journal.CloseScope("AffirmBreakTreaty");
				return 0xffff;
			}

			this.processor.RegAcc.Value16 = 1;
			this.processor.Journal.CloseScope("AffirmBreakTreaty");
			return 1;
		}

		/// <summary>
		/// Detonates a nuclear weapon at the specified coordinates.
		/// </summary>
		public void ExplodeNuclearWeapon(short participantIdx, short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ExplodeNuclearWeapon({participantIdx}, {horizLoc}, {vertLoc})");

			short human = this.owner.World.PersonParticipantIdx;
			if (participantIdx != human)
			{
				this.processor.EmitGlobalWord(0x2f9e, 0x3);
				ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
				this.owner.Libc.CopyText(0xba06, civNamePtr);
				this.owner.Libc.AppendText(0xba06, " use\nnuclear weapons!\n");
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.Diplomats.RenderExplosion(horizLoc, vertLoc);

			// Blast affects 3x3 area (deltas 0..8)
			for (short d = 8; d >= 0; d--)
			{
				short tx = (short)this.owner.Routing.TunePosition(this.owner.TownDeltas[d].X + horizLoc);
				short ty = (short)(this.owner.TownDeltas[d].Y + vertLoc);

				this.owner.MapOps.ObtainUnitAtTile(tx, ty);
				short unitIdx = (short)this.processor.RegAcc.Value16;

				// Check for city with SDI Defense
				if ((this.owner.MapOps.ObtainTileImprovements(tx, ty) & 1) != 0)
				{
					short townIdx = (short)this.owner.Helpers.LocateCityAtPosition(tx, ty);
					if (townIdx >= 0 && CombatRules.IsSDIProtected(this.owner.World.Towns[townIdx].StructureMarkers1))
					{
						this.owner.Libc.CopyText(0xba06, "SDI protects ");
						this.owner.Conquest.ObtainCityLabel(townIdx);
						this.owner.Libc.AppendText(0xba06, ".\n");
						this.owner.TurnHooks.DisplayDialog(0xba06, 80, 100);
						unitIdx = -1;
					}
				}

				if (unitIdx != -1)
				{
					short victimPlayer = (short)this.processor.PullGlobalSignedWord(0xd7f0);
					this.owner.UnitOps.TallyUnitsByDomainInStack(victimPlayer, unitIdx, 2);
					int destroyedCount = (short)this.processor.RegAcc.Value16;

					this.owner.World.Participants[participantIdx].TroopsRuined[victimPlayer] += (short)destroyedCount;
					this.owner.UnitOps.VisitEachUnitInStack(victimPlayer, unitIdx, 0xf10);

					if (victimPlayer != participantIdx)
					{
						this.owner.World.Participants[victimPlayer].Relations[participantIdx] |= 0x88;
					}
				}
			}

			EnactNuclearFallout(horizLoc, vertLoc);

			this.processor.Journal.CloseScope("ExplodeNuclearWeapon");
		}

		/// <summary>
		/// Spreads nuclear fallout and halves city population across the 3x3 blast area.
		/// </summary>
		public void EnactNuclearFallout(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"EnactNuclearFallout({horizLoc}, {vertLoc})");

			for (short d = 0; d <= 8; d++)
			{
				short tx = (short)this.owner.Routing.TunePosition(this.owner.TownDeltas[d].X + horizLoc);
				short ty = (short)(this.owner.TownDeltas[d].Y + vertLoc);

				if ((this.owner.MapOps.ObtainTileImprovements(tx, ty) & 1) != 0)
				{
					// Tile has city: halve population
					short townIdx = (short)this.owner.Helpers.LocateCityAtPosition(tx, ty);
					if (townIdx >= 0)
					{
						var town = this.owner.World.Towns[townIdx];
						town.TrueExtent = (sbyte)CombatRules.CalculateNuclearCityPopulation(town.TrueExtent);
					}
				}
				else
				{
					// Open tile: chance of smog/fallout (if not ocean)
					int diceVal = this.owner.Libc.Dice.DrawInt(3);
					int terrain = this.owner.MapOps.ObtainTerrainType(tx, ty);
					if (CombatRules.ShouldAttachFallout(terrain, diceVal))
					{
						this.owner.CityOps.AttachSmog(tx, ty);
					}
				}

				this.owner.MapOps.RepaintMapTile(tx, ty);
			}

			this.processor.Journal.CloseScope("EnactNuclearFallout");
		}
	}
}
