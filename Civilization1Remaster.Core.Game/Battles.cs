using System;

namespace Civilization1Remaster.Core.Game
{
	public class Battles : GameComponent, IBattles
	{
		private const int TroopConditionDisplacement = 0;

		private const int TroopHorizontalDisplacement = 1;
		private const int TroopVerticalDisplacement = 2;
		private const int TroopKindDisplacement = 3;
		private const int TroopStepsDisplacement = 4;

		private const int TroopSpecificationsRoot = 0x112a;

		private const int TroopSpecificationExtent = 0x22;
		private const int TroopLandGroupDisplacement = 14;
		private const int TroopAssaultDisplacement = 20;
		private const int TroopGuardDisplacement = 22;
		private const int TroopGroupSky = 1;
		private const int TroopGroupWater = 2;

		private const int LandGuardLookup = 0x28f;

		private const int LandExtent = 0x13;
		private const int CastleMarker = 0x1;
		private const int TownRampartsMarker = 0x80;

		private const int FightBeginWait = 20;

		private const int FightPhaseWait = 18;

		private readonly UnitMemory troops;

		private CombatResolver.Battle? finalFight;

		private int finalAssailantCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;
		private int finalGuardianCompleteStrikeMarks = CombatResolver.RegularStrikeMarks;

		public Battles(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public ushort SettleBattle(short firstParticipantIndex, short firstTroopIndex, short secondParticipantIndex, short secondTroopIndex, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"SettleBattle({firstParticipantIndex}, {firstTroopIndex}, {secondParticipantIndex}, {secondTroopIndex}, {marker})");

			int assailantKind = PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopKindDisplacement);
			int guardianKind = PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopKindDisplacement);
			int guardianHorizontal = PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement);
			int guardianVertical = PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement);
			int guardianLand = this.Owner.MapOperations.ObtainTerrainType(guardianHorizontal, guardianVertical);
			int landGuard = (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(LandGuardLookup + (guardianLand * LandExtent)));
			bool raidersAssaultTown = false;

			int assault = ObtainUnitAttack(assailantKind) * 8;
			bool stronghold = (this.Owner.MapOperations.ObtainTileImprovements(guardianHorizontal, guardianVertical) & 0x20) != 0;
			bool dugIn = (PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopConditionDisplacement) & (int)UnitStatus.Fortified) != 0;
			bool townRamparts = false;

			if (firstParticipantIndex == 0)
			{
				if (secondParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					assault = assault * (this.Owner.World.HardnessTier + 1) / 4;
				}
				else
				{
					assault /= 2;
				}
			}

			int guardianGroup = ObtainUnitTerrainCategory(guardianKind);
			int townIndex = -1;

			if ((this.Owner.MapOperations.ObtainTileImprovements(guardianHorizontal, guardianVertical) & 0x1) != 0)
			{
				townIndex = (short)this.Owner.Helpers.FindCityAtPosition(guardianHorizontal, guardianVertical);
				ushort townStructures = this.Owner.World.Towns[townIndex].StructureMarkersLow;

				townRamparts = (townStructures & TownRampartsMarker) != 0;

				if (firstParticipantIndex == 0)
				{
					if (this.Owner.World.Participants[secondParticipantIndex].TownTally == 1)
					{
						assault = 0;
					}

					if ((townStructures & CastleMarker) != 0)
					{
						assault /= 2;
					}

					raidersAssaultTown = true;
				}
			}

			if ((PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopConditionDisplacement) & (int)UnitStatus.Veteran) != 0)
			{
				assault += assault / 2;
			}

			int guard = DefenseStrength.Compute(ObtainUnitProtection(guardianKind), landGuard, stronghold, dugIn,
				guardianGroup == TroopGroupWater, guardianGroup == TroopGroupSky, townRamparts,
				ObtainUnitAttack(assailantKind),
				(PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopConditionDisplacement) & (int)UnitStatus.Veteran) != 0);

			sbyte assailantSteps = (sbyte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopStepsDisplacement);
			if (assailantSteps < 3)
			{
				assault = assailantSteps * assault / 3;
			}

			if (marker == 0)
			{
				int chances = (short)(assault * 8) / (short)(guard + 1);
				return (ushort)chances;
			}

			if (firstParticipantIndex != 0)
			{
				if (this.Owner.World.HardnessTier <= 1 && secondParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					assault /= 2;
				}

				if (this.Owner.World.HardnessTier == 0 && firstParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					assault *= 2;
				}
			}

			if (firstParticipantIndex == this.Owner.World.PersonParticipantIndex)
			{
				if (AffirmBreakTreaty(secondParticipantIndex) == 0xffff)
				{
					this.troops.Assign(firstParticipantIndex, firstTroopIndex, UnitMemory.TravelHorizontal, 0xff);
					return 0xffff;
				}
			}

			bool assailantWins = ExecuteBattleClash(
				firstParticipantIndex, firstTroopIndex,
				secondParticipantIndex, secondTroopIndex,
				(ushort)assault, (ushort)guard,
				raidersAssaultTown);

			if (ShouldShowBattle(firstParticipantIndex, firstTroopIndex, secondParticipantIndex, secondTroopIndex))
			{
				AnimateBattle(firstParticipantIndex, firstTroopIndex, secondParticipantIndex, secondTroopIndex, assailantKind, guardianKind, assailantWins);
			}

			UpdateBattleDiplomacy(firstParticipantIndex, secondParticipantIndex);

			if (assailantKind == 0x19)
			{
				ExplodeNuclearWeapon(
					firstParticipantIndex,
					(short)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement),
					(short)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement));
				return 1;
			}

			if (assailantWins)
			{
				ResolveAttackerVictory(firstParticipantIndex, firstTroopIndex, secondParticipantIndex, secondTroopIndex, (short)townIndex);
				return 1;
			}

			this.Owner.World.Participants[secondParticipantIndex].TroopsRuined[firstParticipantIndex]++;
			this.Owner.UnitOperations.DestroyUnit(firstParticipantIndex, firstTroopIndex);

			if (this.Owner.Runtime.Dice.DrawInteger(2) != 0)
			{
				PromoteUnitToVeteran(secondParticipantIndex, secondTroopIndex);
			}

			return 0;
		}

		public void AttachUnitToStack(short participantIndex, short troopIndex, ushort stackTroopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"AttachUnitToStack({participantIndex}, {troopIndex}, {stackTroopIndex})");

			if (this.troops.Following(participantIndex, stackTroopIndex) == -1)
			{
				this.troops.AssignFollowing(participantIndex, stackTroopIndex, (byte)troopIndex);
				this.troops.AssignFollowing(participantIndex, troopIndex, (byte)stackTroopIndex);
			}
			else
			{
				this.troops.AssignFollowing(participantIndex, troopIndex, (byte)this.troops.Following(participantIndex, stackTroopIndex));
				this.troops.AssignFollowing(participantIndex, stackTroopIndex, (byte)troopIndex);
			}
		}

		public void RemoveUnitFromStack(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DiscardUnitFromStack({participantIndex}, {troopIndex})");

			int following = this.troops.Following(participantIndex, troopIndex);

			if (following != -1)
			{
				if (this.troops.Following(participantIndex, following) == troopIndex)
				{
					this.troops.AssignFollowing(participantIndex, following, 0xff);
				}
				else
				{
					int previous = following;
					int steps = 0;

					do
					{
						previous = this.troops.Following(participantIndex, previous);
						steps++;
					}
					while (steps < 32 && previous != -1 && this.troops.Following(participantIndex, previous) != troopIndex);

					if (steps < 32 && previous != -1)
					{
						this.troops.AssignFollowing(participantIndex, previous, (byte)this.troops.Following(participantIndex, troopIndex));
					}
				}

				this.troops.AssignFollowing(participantIndex, troopIndex, 0xff);
			}
		}

		public ushort AffirmBreakTreaty(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"AffirmBreakTreaty({participantIndex})");

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];
			ushort affirmed = 0;

			if ((person.Relations[participantIndex] & 2) != 0)
			{
				ushort choice = Message("We have signed a\npeace treaty with\nthe ")
					.Text(Tables.CivilizationPlural(participantIndex))
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

		public void ExplodeNuclearWeapon(short participantIndex, short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ExplodeNuclearWeapon({participantIndex}, {horizontalLocation}, {verticalLocation})");

			IMapPlanes chart = this.Owner.MapOperations.Chart;

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				Screen.DialogStyle = 0x3;

				Message(Tables.CivilizationPlural(participantIndex))
					.Text(" use\nnuclear weapons!\n")
					.Dialog(100, 80);
			}

			this.Owner.Diplomats.DrawExplosion(horizontalLocation, verticalLocation);

			for (int heading = 8; heading >= 0; heading--)
			{
				short tileHorizontal = (short)(chart.DirectionHorizontal(heading) + horizontalLocation);
				short tileVertical = (short)(chart.DirectionVertical(heading) + verticalLocation);

				ushort troopIndex = this.Owner.MapOperations.ObtainUnitAtTile(tileHorizontal, tileVertical);

				if ((this.Owner.MapOperations.ObtainTileImprovements(tileHorizontal, tileVertical) & 0x1) != 0)
				{
					short townIndex = (short)this.Owner.Helpers.FindCityAtPosition(tileHorizontal, tileVertical);

					if (this.Owner.World.Towns[(ushort)townIndex].Has(CityStructure.StrategicDefense))
					{
						Message("SDI protects ")
							.City(townIndex)
							.Text(".\n")
							.Dialog(80, 100);

						troopIndex = 0xffff;
					}
				}

				if (troopIndex == 0xffff)
				{
					continue;
				}

				ushort ruined = this.Owner.UnitOperations.CountUnitsByDomainInStack((short)Search.FoundPlayer, (short)troopIndex, 2);

				this.Owner.World.Participants[participantIndex].TroopsRuined[Search.FoundPlayer] += (short)ruined;

				this.Owner.UnitOperations.VisitEachUnitInStack((short)Search.FoundPlayer, (short)troopIndex, 0xf10);

				ushort victimIndex = Search.FoundPlayer;

				if ((ushort)participantIndex != victimIndex)
				{
					this.Owner.World.Participants[victimIndex].Relations[participantIndex] |= 0x88;
				}
			}

			EnactNuclearFallout(horizontalLocation, verticalLocation);
		}

		public void EnactNuclearFallout(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"EnactNuclearFallout({horizontalLocation}, {verticalLocation})");

			IMapPlanes chart = this.Owner.MapOperations.Chart;

			for (int heading = 0; heading <= 8; heading++)
			{
				int tileHorizontal = this.Owner.Routing.TunePosition(chart.DirectionHorizontal(heading) + horizontalLocation);
				int tileVertical = (short)(chart.DirectionVertical(heading) + verticalLocation);

				if ((this.Owner.MapOperations.ObtainTileImprovements(tileHorizontal, tileVertical) & 0x1) != 0)
				{
					City town = this.Owner.World.Towns[this.Owner.Helpers.FindCityAtPosition(tileHorizontal, tileVertical)];

					town.TrueExtent -= (sbyte)(town.TrueExtent / 2);
				}
				else if (this.Owner.Runtime.Dice.DrawInteger(3) != 0 && this.Owner.MapOperations.ObtainTerrainType(tileHorizontal, tileVertical) != 0xa)
				{
					this.Owner.CityOperations.AttachSmog(tileHorizontal, tileVertical);
				}

				this.Owner.MapOperations.RepaintMapTile(tileHorizontal, tileVertical);
			}
		}

		private void ShowBattleRounds(int assailantHorizontal, int assailantVertical, int guardianHorizontal, int guardianVertical)
		{
			CombatResolver.Battle? fight = this.finalFight;

			if (fight == null)
			{
				return;
			}

			int assailantStrikeMarks = fight.AssailantBeginStrikeMarks;
			int guardianStrikeMarks = fight.GuardianBeginStrikeMarks;

			this.Owner.MapOperations.DrawHealthBar(assailantHorizontal, assailantVertical, this.finalAssailantCompleteStrikeMarks, assailantStrikeMarks);
			this.Owner.MapOperations.DrawHealthBar(guardianHorizontal, guardianVertical, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);
			this.Owner.TextPainter.WaitForTime(FightBeginWait);

			for (int index = 0; index < fight.Phases.Count; index++)
			{
				if (fight.Phases[index])
				{
					guardianStrikeMarks--;
					this.Owner.MapOperations.DrawHealthBar(guardianHorizontal, guardianVertical, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);

					if (guardianStrikeMarks == 0 && index + 1 < fight.Phases.Count)
					{
						this.Owner.TextPainter.WaitForTime(FightBeginWait);
						guardianStrikeMarks = fight.GuardianBeginStrikeMarks;
						this.Owner.MapOperations.DrawHealthBar(guardianHorizontal, guardianVertical, this.finalGuardianCompleteStrikeMarks, guardianStrikeMarks);
					}
				}
				else
				{
					assailantStrikeMarks--;
					this.Owner.MapOperations.DrawHealthBar(assailantHorizontal, assailantVertical, this.finalAssailantCompleteStrikeMarks, assailantStrikeMarks);
				}

				this.Owner.TextPainter.WaitForTime(FightPhaseWait);
			}
		}

		private int PullUnitByte(int participantIndex, int troopIndex, int displacement)
		{
			return this.troops.PullSigned(participantIndex, troopIndex, displacement);
		}

		private int PullUnitDefinitionWord(int troopKind, int displacement)
		{
			return (short)this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(TroopSpecificationsRoot + (troopKind * TroopSpecificationExtent) + displacement));
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
			short firstParticipantIndex, short firstTroopIndex,
			short secondParticipantIndex, short secondTroopIndex,
			ushort assaultAmount, ushort guardAmount,
			bool townAssaultedViaRaiders)
		{
			Func<int, int> followingChance = peakAmount => this.Owner.Runtime.Dice.DrawInteger(peakAmount);
			UnitHealth vitality = this.Owner.World.Vitals;

			int assailantTroopKind = PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopKindDisplacement);
			int guardianTroopKind = PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopKindDisplacement);
			bool assailantSeasoned = (PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopConditionDisplacement) & (int)UnitStatus.Veteran) != 0;
			bool guardianSeasoned = (PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopConditionDisplacement) & (int)UnitStatus.Veteran) != 0;
			bool assailantAce = vitality.VerifyElite(firstParticipantIndex, firstTroopIndex, assailantTroopKind);
			bool guardianAce = vitality.VerifyElite(secondParticipantIndex, secondTroopIndex, guardianTroopKind);

			CombatResolver.Battle fight = CombatResolver.Clash(
				assaultAmount, guardAmount,
				vitality.ObtainHitPoints(firstParticipantIndex, firstTroopIndex, assailantTroopKind, assailantSeasoned),
				vitality.ObtainHitPoints(secondParticipantIndex, secondTroopIndex, guardianTroopKind, guardianSeasoned),
				townAssaultedViaRaiders ? 2 : 1,
				followingChance);

			this.finalFight = fight;
			this.finalAssailantCompleteStrikeMarks = CombatResolver.TopHitPoints(assailantSeasoned, assailantAce);
			this.finalGuardianCompleteStrikeMarks = CombatResolver.TopHitPoints(guardianSeasoned, guardianAce);

			if (fight.AssailantVictories)
			{
				bool ace = assailantAce || (assailantSeasoned && fight.Phases.Count > 0);
				vitality.Assign(
					firstParticipantIndex, firstTroopIndex, assailantTroopKind,
					CombatResolver.TopHitPoints(assailantSeasoned, ace) - fight.AssailantStrikeMarks, ace);
				vitality.Wipe(secondParticipantIndex, secondTroopIndex);
			}
			else
			{
				bool ace = guardianAce || (guardianSeasoned && fight.Phases.Count > 0);
				vitality.Assign(
					secondParticipantIndex, secondTroopIndex, guardianTroopKind,
					CombatResolver.TopHitPoints(guardianSeasoned, ace) - fight.GuardianStrikeMarks, ace);
				vitality.Wipe(firstParticipantIndex, firstTroopIndex);
			}

			return fight.AssailantVictories;
		}

		private bool ShouldShowBattle(short firstParticipantIndex, short firstTroopIndex, short secondParticipantIndex, short secondTroopIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;
			ushort personBit = (ushort)(1 << (personIndex & 0xff));

			bool visibleToPerson =
				firstParticipantIndex == personIndex ||
				secondParticipantIndex == personIndex ||
				((ushort)PullUnitByte(firstParticipantIndex, firstTroopIndex, 9) & personBit) != 0 ||
				((ushort)PullUnitByte(secondParticipantIndex, secondTroopIndex, 9) & personBit) != 0 ||
				MapView.Revealed != 0;

			return visibleToPerson && MapView.WorldMapView != 1;
		}

		private void AnimateBattle(
			short firstParticipantIndex, short firstTroopIndex,
			short secondParticipantIndex, short secondTroopIndex,
			int assailantKind, int guardianKind,
			bool assailantWins)
		{
			this.Owner.UnitOperations.PanMapToPosition(
				this.Owner.World.PersonParticipantIndex,
				PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement),
				PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement));

			this.Owner.Startup.HideMouseCursor();

			ushort tileVisible = this.Owner.MapOperations.DrawMapTile(
				PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopHorizontalDisplacement),
				PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopVerticalDisplacement));

			if (tileVisible != 0)
			{
				int victorKind = assailantWins ? assailantKind : guardianKind;
				ushort modernWeapon = (ObtainUnitAttack(victorKind) > 4 || ObtainUnitProtection(victorKind) > 2) ? (ushort)1 : (ushort)0;

				ushort soundIdentifier;
				if (assailantWins && assailantKind == 0xf)
				{
					soundIdentifier = 0x2b;
				}
				else if ((assailantWins && firstParticipantIndex == this.Owner.World.PersonParticipantIndex) ||
					(!assailantWins && secondParticipantIndex == this.Owner.World.PersonParticipantIndex))
				{
					soundIdentifier = modernWeapon != 0 ? (ushort)0x28 : (ushort)0x26;
				}
				else
				{
					soundIdentifier = modernWeapon != 0 ? (ushort)0x29 : (ushort)0x27;
				}

				this.Owner.Services.PerformSound(soundIdentifier, 0);

				this.Owner.MapOperations.DrawUnitOnWorldMap(secondParticipantIndex, secondTroopIndex);

				byte nextInStack = (byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, 0xa);
				if (nextInStack != 0xff)
				{
					this.Owner.MapOperations.DrawUnitOnWorldMap(firstParticipantIndex, (short)PullUnitByte(firstParticipantIndex, firstTroopIndex, 0xa));
				}

				this.Owner.VideoCard.RenderImage(
					Screen.Box,
					0x50, 0, 0xf0, 0xc8,
					Screen.BackScreenBox,
					0x50, 0);

				short assailantScreenHorizontal = (short)((this.Owner.Routing.TunePosition(
					PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopHorizontalDisplacement) -
					(short)MapView.Horizontal) << 4) + 0x50);

				short assailantScreenVertical = (short)((((byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopVerticalDisplacement) -
					MapView.Vertical) << 4) + 0x8);

				byte defenderHorizontal = (byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement);
				byte attackerHorizontal = (byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopHorizontalDisplacement);
				short stepHorizontal = (short)Math.Sign((short)(defenderHorizontal - attackerHorizontal));

				byte defenderVertical = (byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement);
				byte attackerVertical = (byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopVerticalDisplacement);
				short stepVertical = (short)Math.Sign((short)(defenderVertical - attackerVertical));

				if ((byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement) == 0 &&
					(byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopHorizontalDisplacement) == 0x4f)
				{
					stepHorizontal = (short)-stepHorizontal;
				}
				else if ((byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement) == 0x4f &&
					(byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopHorizontalDisplacement) == 0)
				{
					stepHorizontal = (short)-stepHorizontal;
				}

				for (short step = 0; step <= 0xa; step++)
				{
					short drawHorizontal = (short)(assailantScreenHorizontal + (step * stepHorizontal));
					short drawVertical = (short)(assailantScreenVertical + (step * stepVertical));

					ushort unitSprite = this.Processor.PullWord(
						this.Processor.SegmentData.Word,
						(ushort)((((byte)PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopKindDisplacement) +
							(firstParticipantIndex << 5) + 0x40) << 1) + 0xd4ce));

					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						drawHorizontal + 1,
						drawVertical + 1,
						unitSprite);

					this.Owner.TextPainter.WaitForTime(2);

					this.Owner.VideoCard.RenderImage(
						Screen.BackScreenBox,
						(ushort)drawHorizontal, (ushort)drawVertical, 16, 16,
						Screen.Box,
						(ushort)drawHorizontal, (ushort)drawVertical);
				}

				this.Owner.MapOperations.RepaintMapTile(
					PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopHorizontalDisplacement),
					PullUnitByte(firstParticipantIndex, firstTroopIndex, TroopVerticalDisplacement));

				this.Owner.MapOperations.RepaintMapTile(
					PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement),
					PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement));

				this.Owner.VideoCard.RenderImage(
					Screen.Box,
					0, 0, 0x100, 0xc8,
					Screen.BackScreenBox,
					0, 0);

				int guardianDisplayHorizontal = assailantScreenHorizontal + (16 * stepHorizontal);
				int guardianDisplayVertical = assailantScreenVertical + (16 * stepVertical);
				ShowBattleRounds(assailantScreenHorizontal, assailantScreenVertical, guardianDisplayHorizontal, guardianDisplayVertical);

				short loserStep = (short)(assailantWins ? 0x10 : 0);
				for (short frame = 0; frame < 8; frame++)
				{
					short loserHorizontal = (short)(assailantScreenHorizontal + (loserStep * stepHorizontal));
					short loserVertical = (short)(assailantScreenVertical + (loserStep * stepVertical));

					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						loserHorizontal + 1,
						loserVertical + 1,
						Tables.SpritePicture(frame + 0x20));

					this.Owner.TextPainter.WaitForTime(4);

					this.Owner.VideoCard.RenderImage(
						Screen.BackScreenBox,
						(ushort)loserHorizontal, (ushort)loserVertical, 16, 16,
						Screen.Box,
						(ushort)loserHorizontal, (ushort)loserVertical);
				}

				int victorHorizontal = assailantScreenHorizontal + (assailantWins ? 0 : 16 * stepHorizontal);
				int victorVertical = assailantScreenVertical + (assailantWins ? 0 : 16 * stepVertical);

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					victorHorizontal, victorVertical, 16, 16,
					Screen.Box,
					victorHorizontal, victorVertical);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		private void UpdateBattleDiplomacy(short firstParticipantIndex, short secondParticipantIndex)
		{
			this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] |= 0x20;
			this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] |= 0x20;

			if (firstParticipantIndex != 0 && secondParticipantIndex != 0)
			{
				this.Owner.World.TruceRoundTally = 0;
			}

			short personIndex = this.Owner.World.PersonParticipantIndex;
			if ((secondParticipantIndex == personIndex && (this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] & 2) != 0) ||
				(this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] & 0x200) != 0)
			{
				Screen.DialogStyle = 0x3;

				Message("Sneak attack by\n")
					.Text(Tables.CivilizationName(firstParticipantIndex))
					.Text(" forces!\n")
					.Dialog(100, 80);

				this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] &= 0xfdff;
			}

			if ((this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] & 2) != 0 ||
				(this.Owner.World.Participants[firstParticipantIndex].Relations[secondParticipantIndex] & 2) != 0)
			{
				if (firstParticipantIndex == personIndex || secondParticipantIndex == personIndex)
				{
					Screen.DialogStyle = 0x5;

					Message(Tables.CivilizationPlural(secondParticipantIndex))
						.Text(" cancel\npeace treaty\nwith ")
						.Text(Tables.CivilizationPlural(firstParticipantIndex))
						.Text(".\n")
						.Dialog(100, 80);
				}

				if (firstParticipantIndex == personIndex && (this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] & 2) != 0)
				{
					this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] |= 8;
				}

				this.Owner.Politics.ClearDiplomacyFlags(firstParticipantIndex, secondParticipantIndex, 2);
			}

			if (firstParticipantIndex == personIndex)
			{
				this.Owner.World.Participants[secondParticipantIndex].Relations[firstParticipantIndex] &= 0xfdff;
			}
		}

		private void ResolveAttackerVictory(short firstParticipantIndex, short firstTroopIndex, short secondParticipantIndex, short secondTroopIndex, short townIndex)
		{
			if (secondParticipantIndex == 0 && (byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopKindDisplacement) == 0x1a)
			{
				this.Owner.World.Participants[firstParticipantIndex].Gold += 100;

				if (firstParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					this.Owner.Words.Clear();
					this.Owner.BlockText.ShowTextDialog(GameTexts.TopicLeader);
					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);
				}
			}

			if (townIndex == -1 &&
				(this.Owner.MapOperations.ObtainTileImprovements(
					PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement),
					PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement)) & 0x20) == 0)
			{
				ushort destroyedCount = this.Owner.UnitOperations.CountUnitsByDomainInStack(secondParticipantIndex, secondTroopIndex, 2);

				this.Owner.World.Participants[firstParticipantIndex].TroopsRuined[secondParticipantIndex] += (short)destroyedCount;
				this.Owner.UnitOperations.VisitEachUnitInStack(secondParticipantIndex, secondTroopIndex, 0xf10);

				if ((short)destroyedCount > 1 &&
					(firstParticipantIndex == this.Owner.World.PersonParticipantIndex || secondParticipantIndex == this.Owner.World.PersonParticipantIndex))
				{
					EmptyMessage()
						.Number((short)destroyedCount)
						.Text(" units destroyed.\n")
						.Dialog(80, 80);
				}
			}
			else
			{
				if (townIndex != -1 && (byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, 0xa) == 0xff)
				{
					for (short direction = 8; direction >= 0; direction--)
					{
						short checkHorizontal = (short)((byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopHorizontalDisplacement) +
							(short)Tables.DirectionHorizontal(direction));

						short checkVertical = (short)((byte)PullUnitByte(secondParticipantIndex, secondTroopIndex, TroopVerticalDisplacement) +
							(short)Tables.DirectionVertical(direction));

						ushort unitAtTile = this.Owner.MapOperations.ObtainUnitAtTile(checkHorizontal, checkVertical);

						if (unitAtTile != 0xffff && (short)Search.FoundPlayer != this.Owner.World.PersonParticipantIndex)
						{
							this.Owner.UnitOperations.VisitEachUnitInStack((short)Search.FoundPlayer, (short)unitAtTile, 0x1593);
						}
					}

					if (this.Owner.World.Towns[townIndex].ParticipantIndex == this.Owner.World.PersonParticipantIndex)
					{
						this.Owner.Computer.PostCityDefenders(townIndex);
					}
				}

				this.Owner.UnitOperations.DestroyUnit(secondParticipantIndex, secondTroopIndex);
				this.Owner.World.Participants[firstParticipantIndex].TroopsRuined[secondParticipantIndex]++;
			}

			if (this.Owner.Runtime.Dice.DrawInteger(2) != 0)
			{
				PromoteUnitToVeteran(firstParticipantIndex, firstTroopIndex);
			}
		}

		private void PromoteUnitToVeteran(short participantIndex, short troopIndex)
		{
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | (byte)UnitStatus.Veteran));
		}
	}
}
