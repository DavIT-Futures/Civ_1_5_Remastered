using System;

namespace Civ_1_5_Remastered.Core.Game.Units
{
	public partial class UnitRoster : GameComponent, IUnitRoster
	{
		private const sbyte KeptVeteran = 0x40;

		private const byte KeptUnitsCondition = 4;

		private const int PeaceRelation = 2;

		private const int CityImprovement = 1;

		private const int ScienceTally = 72;

		private const int StackListTally = 12;

		private readonly UnitMemory troops;

		public UnitRoster(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		private short FoundUnitParticipant => (short)Search.FoundPlayer;

		private bool RevealAll => MapView.Revealed != 0;

		public void CreateUnitsFromCityList(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ConstructUnitsFromCityList({townIndex})");

			City town = this.Owner.World.Towns[townIndex];

			if (town.ParticipantIndex == this.Owner.World.PersonParticipantIndex)
			{
				Turn.QuietUnitSetup = 0x1;

				for (int keptIndex = 0; keptIndex < 2; keptIndex++)
				{
					sbyte kept = town.Unfamiliar[keptIndex];

					if (kept == -1)
					{
						continue;
					}

					ushort troopIndex = CreateUnit(town.ParticipantIndex, (short)(kept & 0x3f), town.Place.X, town.Place.Y);

					if (troopIndex == 0xffff)
					{
						continue;
					}

					byte status = (byte)(this.troops.Pull(town.ParticipantIndex, troopIndex, UnitMemory.Status) | (int)UnitStatus.Fortified);

					if ((kept & KeptVeteran) != 0)
					{
						status |= (byte)UnitStatus.Veteran;
					}

					this.troops.Assign(town.ParticipantIndex, troopIndex, UnitMemory.Status, status);
					town.Unfamiliar[keptIndex] = -1;
				}

				Turn.QuietUnitSetup = 0x0;
				town.ConditionMarker |= KeptUnitsCondition;
			}
		}

		public void KeepUnitsInCityList(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"KeepUnitsInCityList({townIndex})");

			City town = this.Owner.World.Towns[townIndex];
			short participantIndex = town.ParticipantIndex;

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				ushort countLosses = Turn.CountLosses;
				Turn.CountLosses = 0;
				Turn.QuietUnitSetup = 0x1;

				bool firstSeen = false;

				for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (!IsFortifiedHomeUnitIn(town, townIndex, participantIndex, troopIndex))
					{
						continue;
					}

					if (!firstSeen)
					{
						firstSeen = true;
						continue;
					}

					int keptIndex = town.Unfamiliar[0] == -1 ? 0 : 1;

					if (keptIndex == 1 && town.Unfamiliar[1] != -1)
					{
						break;
					}

					town.Unfamiliar[keptIndex] = (sbyte)this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

					if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0)
					{
						town.Unfamiliar[keptIndex] |= KeptVeteran;
					}

					DestroyUnit(participantIndex, troopIndex);
				}

				Turn.QuietUnitSetup = 0x0;
				Turn.CountLosses = countLosses;
				town.ConditionMarker &= unchecked((byte)~KeptUnitsCondition);
			}
		}

		public void ShiftUnit(int horizontalLocation, int verticalLocation, short participantIndex, short troopIndex, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftUnit({horizontalLocation}, {verticalLocation}, {participantIndex}, {troopIndex}, {marker})");

			int startTerrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				RevealSurroundings(participantIndex, troopIndex, horizontalLocation, verticalLocation);
			}

			int startImprovements = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);
			bool repaint = marker != 0;

			for (int heading = 1; heading < 9; heading++)
			{
				MeetNearTile(participantIndex, troopIndex, horizontalLocation, verticalLocation, heading, startTerrain, startImprovements, repaint);
			}

			for (int heading = 9; heading < 25; heading++)
			{
				MeetFarTile(participantIndex, troopIndex, horizontalLocation, verticalLocation, heading, startImprovements, repaint);
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex && repaint)
			{
				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);

				for (int heading = 9; heading < 49; heading++)
				{
					int ringHorizontal = RingHorizontal(heading, horizontalLocation);
					int ringVertical = RingVertical(heading, verticalLocation);

					if (this.Owner.MapOperations.CheckMapLimits(ringHorizontal, ringVertical) != 0 && Sees(participantIndex, ringHorizontal, ringVertical))
					{
						this.Owner.MapOperations.RepaintMapTile(ringHorizontal, ringVertical);
					}
				}
			}
		}

		public ushort CreateUnit(short participantIndex, short kind, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ConstructUnit({participantIndex}, {kind}, {horizontalLocation}, {verticalLocation})");

			short troopIndex = -1;

			for (short index = 0; index < UnitMemory.TroopTally - 1; index++)
			{
				if (this.troops.Pull(participantIndex, index, UnitMemory.Kind) == 0xff)
				{
					troopIndex = index;
					break;
				}
			}

			ushort result;

			if (troopIndex == -1)
			{
				if (participantIndex == this.Owner.World.PersonParticipantIndex && Turn.UnitLimitTold == 0)
				{
					Screen.DialogStyle = 0x3;
					this.Owner.Words.Clear();

					this.Owner.BlockText.ShowTextDialog(GameTexts.TopicUnitMaximum);

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 64);

					Turn.UnitLimitTold = 0x1;
				}

				result = 0xffff;
			}
			else
			{
				SetUpUnit(participantIndex, troopIndex, kind, horizontalLocation, verticalLocation);

				result = (ushort)troopIndex;
			}

			return result;
		}

		public void DestroyUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateUnit({participantIndex}, {troopIndex})");

			if (Turn.CountLosses != 0)
			{
				ushort lostKind = (ushort)this.troops.TroopKind(participantIndex, troopIndex);

				if (lostKind < this.Owner.World.Participants[participantIndex].MissingTroops.Length)
				{
					this.Owner.World.Participants[participantIndex].MissingTroops[lostKind]++;
				}
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.FerryRoom) != 0 &&
				this.troops.Following(participantIndex, troopIndex) != -1 &&
				IsOnOcean(participantIndex, troopIndex))
			{
				VisitEachUnitInStack(participantIndex, troopIndex, 0x1610);
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == UnitKindTable.Carrier &&
				this.troops.Following(participantIndex, troopIndex) != -1 &&
				IsOnOcean(participantIndex, troopIndex))
			{
				VisitEachUnitInStack(participantIndex, troopIndex, 0x1676);
			}

			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			if (kind >= 0 && kind < this.Owner.World.Participants[participantIndex].LiveTroops.Length)
			{
				this.Owner.World.Participants[participantIndex].LiveTroops[kind]--;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			int horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			int verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

			this.Owner.MapOperations.RemoveUnitFromTile(participantIndex, troopIndex, horizontalLocation, verticalLocation);

			if (Turn.QuietUnitSetup == 0 && King.Mood == 0xffff &&
				(MapView.Revealed != 0 || participantIndex == this.Owner.World.PersonParticipantIndex ||
				(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & (ushort)(1 << this.Owner.World.PersonParticipantIndex)) != 0))
			{
				this.Owner.MapOperations.RepaintMapTile(
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));
			}
		}

		public void CheckUnitTerrainFlag(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyUnitTerrainFlag({participantIndex}, {troopIndex})");

			bool carried = IsOnOcean(participantIndex, troopIndex) &&
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;

			if (!carried)
			{
				ClearStatus(participantIndex, troopIndex, UnitStatus.Sentry);
			}
		}

		public void ClearUnitOrders(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"WipeUnitOrders({participantIndex}, {troopIndex})");

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & (int)UnitStatus.Sentry) != 0)
			{
				ClearStatus(participantIndex, troopIndex, UnitStatus.Sentry);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, FullSteps(participantIndex, troopIndex));

				ushort roundsAway = this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway);

				if (roundsAway != 0)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
				}
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
		}

		public void RestoreUnitOrders(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RestoreUnitOrders({participantIndex}, {troopIndex})");

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
			{
				RestoreUnitStatus(participantIndex, troopIndex);
			}
		}

		public void RestoreUnitStatus(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RestoreUnitStatus({participantIndex}, {troopIndex})");

			byte status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);

			if ((status & (int)UnitStatus.SettlerBuildMask) != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
			}

			if ((status & (int)(UnitStatus.SettlerBuildMask | UnitStatus.Fortified | UnitStatus.Sentry)) != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, FullSteps(participantIndex, troopIndex));
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & (int)(UnitStatus.Veteran | UnitStatus.FirstUnknown)));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.MapOperations.DrawUnitOnWorldMap(participantIndex, troopIndex);
			}
		}

		public void DestroyLandUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateLandUnit({participantIndex}, {troopIndex})");

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				DestroyUnit(participantIndex, troopIndex);
			}
		}

		public void DestroyNonLandUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateNonLandUnit({participantIndex}, {troopIndex})");

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				DestroyUnit(participantIndex, troopIndex);
			}
		}

		public void DestroySeaUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateSeaUnit({participantIndex}, {troopIndex})");

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				DestroyUnit(participantIndex, troopIndex);
			}
		}

		public void PanMapToPosition(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"PanMapToPosition({participantIndex}, {horizontalLocation}, {verticalLocation})");

			int viewLeft = (short)MapView.Horizontal;

			if (horizontalLocation < 16 && viewLeft > 64)
			{
				horizontalLocation += MapRules.Width;
			}

			int viewTop = (short)MapView.Vertical;
			bool outside = horizontalLocation < viewLeft + 2 || horizontalLocation > viewLeft + Viewport.Columns - 2 ||
				verticalLocation < viewTop + 2 || verticalLocation > viewTop + Viewport.Rows - 3;

			if (outside)
			{
				int left = (short)this.Owner.Routing.TunePosition(horizontalLocation - Viewport.MiddleColumn - 1);

				this.Owner.MapOperations.FocusMapView(participantIndex, left, verticalLocation - Viewport.MiddleRow);
			}
		}

		public ushort MayOccupyTile(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"MayOccupyTile({participantIndex}, {horizontalLocation}, {verticalLocation})");

			ushort blocked = (this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0 ? (ushort)0 : CheckTileOccupation(participantIndex, horizontalLocation, verticalLocation);

			return blocked;
		}

		public ushort CheckTileOccupation(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyTileOccupation({participantIndex}, {horizontalLocation}, {verticalLocation})");

			bool onOcean = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain;
			ushort found = 0;

			for (int heading = 1; heading < 9; heading++)
			{
				int nearHorizontal = this.Owner.Routing.TunePosition(this.Owner.MapOperations.Chart.DirectionHorizontal(heading) + horizontalLocation);
				int nearVertical = (short)(this.Owner.MapOperations.Chart.DirectionVertical(heading) + verticalLocation);

				bool nearOcean = this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical) == MapRules.OceanTerrain;

				if (nearOcean != onOcean)
				{
					continue;
				}

				ushort nearParticipantIndex = this.Owner.MapOperations.ObtainTileOwner(nearHorizontal, nearVertical);

				if (nearParticipantIndex == 0xffff || nearParticipantIndex == (ushort)participantIndex)
				{
					continue;
				}

				if (participantIndex == this.Owner.World.PersonParticipantIndex)
				{
					RevealToHuman((short)nearParticipantIndex, nearHorizontal, nearVertical);
				}

				found = 1;
				break;
			}

			return found;
		}

		public ushort ContainsAdjacentForeignUnit(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ContainsAdjacentForeignUnit({participantIndex}, {horizontalLocation}, {verticalLocation})");

			bool found = MovementRules.HasAdjacentForeignUnit(horizontalLocation, verticalLocation, participantIndex, (horizontal, vertical) =>
			{
				return (int)(short)this.Owner.MapOperations.ObtainTileOwner(horizontal, vertical);
			});

			ushort result = (ushort)(found ? 1 : 0);

			return result;
		}

		public ushort FindNearestFriendlyCity(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestFriendlyCity({participantIndex}, {troopIndex})");

			int unitHorizontal = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
			int unitVertical = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);
			int targetHorizontal = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			int targetVertical = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);
			int nearest = 999;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
				{
					continue;
				}

				int distance = WrappedChebyshev(unitHorizontal - town.Place.X, unitVertical - town.Place.Y);

				if (distance < nearest)
				{
					nearest = distance;
					targetHorizontal = town.Place.X;
					targetVertical = town.Place.Y;
				}
			}

			for (short carrierIndex = 0; carrierIndex < UnitMemory.TroopTally; carrierIndex++)
			{
				if (this.troops.Pull(participantIndex, carrierIndex, UnitMemory.Kind) != UnitKindTable.Carrier)
				{
					continue;
				}

				int carrierHorizontal = this.troops.Pull(participantIndex, carrierIndex, UnitMemory.Horizontal);
				int carrierVertical = this.troops.Pull(participantIndex, carrierIndex, UnitMemory.Vertical);
				int distance = WrappedChebyshev(unitHorizontal - carrierHorizontal, unitVertical - carrierVertical);

				if (distance < nearest)
				{
					nearest = distance;
					targetHorizontal = carrierHorizontal;
					targetVertical = carrierVertical;
				}
			}

			int best = this.Owner.Helpers.ComputeDistanceFromOffsets(
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal) - targetHorizontal,
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical) - targetVertical);
			ushort heading = 0;

			for (int direction = 1; direction <= 8; direction++)
			{
				int stepHorizontal = (short)(unitHorizontal + this.Owner.MapOperations.Chart.DirectionHorizontal(direction));
				int stepVertical = (short)(unitVertical + this.Owner.MapOperations.Chart.DirectionVertical(direction));

				ushort stepOwner = this.Owner.MapOperations.ObtainTileOwner(stepHorizontal, stepVertical);

				if (stepOwner != 0xffff && stepOwner != (ushort)participantIndex)
				{
					continue;
				}

				if ((this.Owner.MapOperations.ObtainTileImprovements(stepHorizontal, stepVertical) & 1) != 0 &&
					this.Owner.MapOperations.ObtainTileOwnerBits(stepHorizontal, stepVertical) != (ushort)participantIndex)
				{
					continue;
				}

				int distance = this.Owner.Helpers.ComputeDistanceFromOffsets(stepHorizontal - targetHorizontal, stepVertical - targetVertical);

				if (distance < best)
				{
					best = distance;
					heading = (ushort)direction;
				}
			}

			return heading;
		}

		private static int WrappedChebyshev(int horizontalStep, int verticalStep)
		{
			horizontalStep = Math.Abs(horizontalStep);

			if (horizontalStep > 40)
			{
				horizontalStep = 80 - horizontalStep;
			}

			return Math.Max(horizontalStep, Math.Abs(verticalStep));
		}

		private bool IsFortifiedHomeUnitIn(City town, short townIndex, short participantIndex, short troopIndex)
		{
			return this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0xff &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal) == (byte)(sbyte)town.Place.X &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) == (byte)(sbyte)town.Place.Y &&
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.BaseTown) == townIndex &&
				(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & (int)UnitStatus.Fortified) != 0;
		}

		private void ClearTravel(short participantIndex, short troopIndex)
		{
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
		}

		private void SetUpUnit(short participantIndex, short troopIndex, short kind, int horizontalLocation, int verticalLocation)
		{
			byte participantBit = (byte)(1 << participantIndex);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Horizontal, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.FollowingInStack, 0xff);

			this.Owner.MapOperations.MapAssignCityOwner(participantIndex, horizontalLocation, verticalLocation);
			this.Owner.MapOperations.PlaceUnitOnTile(participantIndex, troopIndex, horizontalLocation, verticalLocation);

			this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= (sbyte)participantBit;

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, 0);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Horizontal, (byte)horizontalLocation);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Vertical, (byte)verticalLocation);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, (byte)kind);

			this.Owner.World.Vitals.Wipe(participantIndex, troopIndex);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants, participantBit);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps,
				(byte)this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway));

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.BaseTown, (byte)this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation));

			byte baseTown = this.troops.Pull(participantIndex, troopIndex, UnitMemory.BaseTown);
			ushort baseOwner = baseTown < 128 ? (ushort)this.Owner.World.Towns[baseTown].ParticipantIndex : (ushort)0xffff;

			if (baseOwner != (ushort)participantIndex)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.BaseTown, 0xff);
			}

			byte fuel = this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps);

			if (fuel != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(fuel - 1));
			}

			this.Owner.World.Participants[participantIndex].LiveTroops[kind]++;

			if (Turn.QuietUnitSetup == 0)
			{
				if (MapView.Revealed != 0 || participantIndex == this.Owner.World.PersonParticipantIndex ||
					(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & (ushort)(1 << this.Owner.World.PersonParticipantIndex)) != 0)
				{
					this.Owner.MapOperations.RepaintMapTile(
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));
				}

				ShiftUnit(horizontalLocation, verticalLocation, participantIndex, troopIndex, 1);
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex && this.Owner.World.RoundTally != 0 &&
				Turn.QuietUnitSetup == 0)
			{
				if (this.Owner.World.Participants[participantIndex].TroopTally == 0)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstUnitOne);
				}

				if (this.Owner.World.Participants[participantIndex].TroopTally == 1)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstUnitTwo);
				}
			}
		}

		private bool IsOnOcean(short participantIndex, short troopIndex)
		{
			return this.Owner.MapOperations.ObtainTerrainType(
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) == MapRules.OceanTerrain;
		}

		private void AddToGlobal(ushort address, int amount)
		{
			this.Processor.EmitGlobalWord(address, (ushort)(this.Processor.PullGlobalWord(address) + amount));
		}

		private void ClearStatus(short participantIndex, short troopIndex, UnitStatus flags)
		{
			byte status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(status & ~(int)flags));
		}

		private byte FullSteps(short participantIndex, short troopIndex)
		{
			return (byte)(3 * this.troops.KindField(participantIndex, troopIndex, UnitKindTable.ShiftTally));
		}

		private sbyte TerrainMoveCost(int terrain)
		{
			return (sbyte)Tables.TerrainField(terrain, TerrainTable.MoveCost);
		}
	}
}
