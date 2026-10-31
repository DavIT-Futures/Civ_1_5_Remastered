using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1866.
	/// </summary>
	public partial class UnitRoster : GameComponent, IUnitRoster
	{
		/// <summary>Bit of a kept unit type (City.Unfamiliar): the unit was a veteran</summary>
		private const sbyte KeptVeteran = 0x40;

		/// <summary>Bit of City.ConditionMarker changed when the kept units are put back or taken away</summary>
		private const byte KeptUnitsCondition = 4;

		/// <summary>Bit of Player.Relations: the two players have peace</summary>
		private const int PeaceRelation = 2;

		/// <summary>Bit of the tile improvements: a city</summary>
		private const int CityImprovement = 1;

		/// <summary>Number of technologies</summary>
		private const int ScienceTally = 72;

		/// <summary>Most units shown in the list of ShowBattleScreen</summary>
		private const int StackListTally = 12;

		private readonly UnitMemory troops;

		public UnitRoster(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>The player whose unit ObtainUnitAtTile found last (0xd7f0)</summary>
		private short FoundUnitParticipant => (short)Search.FoundPlayer;

		/// <summary>The AI players see all units (0xd806, a debug option of the original)</summary>
		private bool RevealAll => MapView.Revealed != 0;

		/// <inheritdoc/>
		public void CreateUnitsFromCityList(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ConstructUnitsFromCityList({townIdx})");

			City town = this.Owner.World.Towns[townIdx];

			if (town.ParticipantIdx == this.Owner.World.PersonParticipantIdx)
			{
				Turn.QuietUnitSetup = 0x1;

				for (int keptIdx = 0; keptIdx < 2; keptIdx++)
				{
					sbyte kept = town.Unfamiliar[keptIdx];

					if (kept == -1)
						continue;

					ushort troopIdx = CreateUnit(town.ParticipantIdx, (short)(kept & 0x3f), town.Place.X, town.Place.Y);

					if (troopIdx == 0xffff)
						continue;

					byte status = (byte)(this.troops.Pull(town.ParticipantIdx, troopIdx, UnitMemory.Status) | (int)UnitStatus.Fortified);

					if ((kept & KeptVeteran) != 0)
						status |= (byte)UnitStatus.Veteran;

					this.troops.Assign(town.ParticipantIdx, troopIdx, UnitMemory.Status, status);
					town.Unfamiliar[keptIdx] = -1;
				}

				Turn.QuietUnitSetup = 0x0;
				town.ConditionMarker |= KeptUnitsCondition;
			}
		}

		/// <inheritdoc/>
		public void KeepUnitsInCityList(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"KeepUnitsInCityList({townIdx})");

			City town = this.Owner.World.Towns[townIdx];
			short participantIdx = town.ParticipantIdx;

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				ushort countLosses = Turn.CountLosses;
				Turn.CountLosses = 0;
				Turn.QuietUnitSetup = 0x1;

				bool firstSeen = false;

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!IsFortifiedHomeUnitIn(town, townIdx, participantIdx, troopIdx))
						continue;

					if (!firstSeen)
					{
						firstSeen = true;
						continue;
					}

					int keptIdx = town.Unfamiliar[0] == -1 ? 0 : 1;

					if (keptIdx == 1 && town.Unfamiliar[1] != -1)
						break;

					town.Unfamiliar[keptIdx] = (sbyte)this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0)
						town.Unfamiliar[keptIdx] |= KeptVeteran;

					DestroyUnit(participantIdx, troopIdx);
				}

				Turn.QuietUnitSetup = 0x0;
				Turn.CountLosses = countLosses;
				town.ConditionMarker &= unchecked((byte)~KeptUnitsCondition);
			}
		}

		/// <inheritdoc/>
		public void ShiftUnit(int horizLoc, int vertLoc, short participantIdx, short troopIdx, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftUnit({horizLoc}, {vertLoc}, {participantIdx}, {troopIdx}, {marker})");

			int startTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				RevealSurroundings(participantIdx, troopIdx, horizLoc, vertLoc);
			}

			int startImprovements = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);
			bool repaint = marker != 0;

			for (int heading = 1; heading < 9; heading++)
			{
				MeetNearTile(participantIdx, troopIdx, horizLoc, vertLoc, heading, startTerrain, startImprovements, repaint);
			}

			for (int heading = 9; heading < 25; heading++)
			{
				MeetFarTile(participantIdx, troopIdx, horizLoc, vertLoc, heading, startImprovements, repaint);
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx && repaint)
			{
				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

				for (int heading = 9; heading < 49; heading++)
				{
					int ringHoriz = RingHoriz(heading, horizLoc);
					int ringVert = RingVert(heading, vertLoc);

					if (this.Owner.MapOps.CheckMapLimits(ringHoriz, ringVert) != 0 && Sees(participantIdx, ringHoriz, ringVert))
					{
						this.Owner.MapOps.RepaintMapTile(ringHoriz, ringVert);
					}
				}
			}

		}

		/// <inheritdoc/>
		public ushort CreateUnit(short participantIdx, short kind, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ConstructUnit({participantIdx}, {kind}, {horizLoc}, {vertLoc})");

			short troopIdx = -1;

			for (short idx = 0; idx < UnitMemory.TroopTally - 1; idx++)
			{
				if (this.troops.Pull(participantIdx, idx, UnitMemory.Kind) == 0xff)
				{
					troopIdx = idx;
					break;
				}
			}

			ushort result;

			if (troopIdx == -1)
			{
				if (participantIdx == this.Owner.World.PersonParticipantIdx && Turn.UnitLimitTold == 0)
				{
					Screen.DialogStyle = 0x3;
					this.Owner.Words.Clear();

					this.Owner.BlockText.ShowTextDialog(GameTexts.TopicUmax);

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 64);

					Turn.UnitLimitTold = 0x1;
				}

				result = 0xffff;
			}
			else
			{
				SetUpUnit(participantIdx, troopIdx, kind, horizLoc, vertLoc);

				result = (ushort)troopIdx;
			}

			return result;
		}

		/// <inheritdoc/>
		public void DestroyUnit(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateUnit({participantIdx}, {troopIdx})");

			if (Turn.CountLosses != 0)
			{
				// Losses are counted (the kind as an unsigned word: an empty record, -1, is not counted)
				ushort lostKind = (ushort)this.troops.TroopKind(participantIdx, troopIdx);

				if (lostKind < this.Owner.World.Participants[participantIdx].MissingTroops.Length)
				{
					this.Owner.World.Participants[participantIdx].MissingTroops[lostKind]++;
				}
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) != 0 &&
				this.troops.Following(participantIdx, troopIdx) != -1 &&
				IsOnOcean(participantIdx, troopIdx))
			{
				VisitEachUnitInStack(participantIdx, troopIdx, 0x1610);
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Carrier &&
				this.troops.Following(participantIdx, troopIdx) != -1 &&
				IsOnOcean(participantIdx, troopIdx))
			{
				VisitEachUnitInStack(participantIdx, troopIdx, 0x1676);
			}

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (kind >= 0 && kind < this.Owner.World.Participants[participantIdx].LiveTroops.Length)
				this.Owner.World.Participants[participantIdx].LiveTroops[kind]--;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			this.Owner.MapOps.RemoveUnitFromTile(participantIdx, troopIdx, horizLoc, vertLoc);

			if (Turn.QuietUnitSetup == 0 && King.Mood == 0xffff &&
				(MapView.Revealed != 0 || participantIdx == this.Owner.World.PersonParticipantIdx ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << this.Owner.World.PersonParticipantIdx)) != 0))
			{
				this.Owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}
		}

		/// <inheritdoc/>
		public void CheckUnitTerrainFlag(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyUnitTerrainFlag({participantIdx}, {troopIdx})");

			bool carried = IsOnOcean(participantIdx, troopIdx) &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;

			if (!carried)
			{
				ClearStatus(participantIdx, troopIdx, UnitStatus.Sentry);
			}
		}

		/// <inheritdoc/>
		public void ClearUnitOrders(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"WipeUnitOrders({participantIdx}, {troopIdx})");

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatus.Sentry) != 0)
			{
				ClearStatus(participantIdx, troopIdx, UnitStatus.Sentry);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, FullSteps(participantIdx, troopIdx));

				ushort roundsAway = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway);

				if (roundsAway != 0)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
				}
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
		}

		/// <inheritdoc/>
		public void RestoreUnitOrders(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RestoreUnitOrders({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
			{
				RestoreUnitStatus(participantIdx, troopIdx);
			}
		}

		/// <inheritdoc/>
		public void RestoreUnitStatus(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RestoreUnitStatus({participantIdx}, {troopIdx})");

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);

			if ((status & (int)UnitStatus.SettlerBuildMask) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
			}

			if ((status & (int)(UnitStatus.SettlerBuildMask | UnitStatus.Fortified | UnitStatus.Sentry)) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, FullSteps(participantIdx, troopIdx));
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)(UnitStatus.Veteran | UnitStatus.Unknown1)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, troopIdx);
			}
		}

		/// <inheritdoc/>
		public void DestroyLandUnit(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateLandUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				DestroyUnit(participantIdx, troopIdx);
			}
		}

		/// <inheritdoc/>
		public void DestroyNonLandUnit(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateNonLandUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				DestroyUnit(participantIdx, troopIdx);
			}
		}

		/// <inheritdoc/>
		public void DestroySeaUnit(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateSeaUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				DestroyUnit(participantIdx, troopIdx);
			}
		}

		/// <inheritdoc/>
		public void PanMapToPosition(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"PanMapToPosition({participantIdx}, {horizLoc}, {vertLoc})");

			int viewLeft = (short)MapView.Horiz;

			// The view may reach over the right edge of the world
			if (horizLoc < 16 && viewLeft > 64)
			{
				horizLoc += MapRules.Width;
			}

			int viewTop = (short)MapView.Vert;
			bool outside = horizLoc < viewLeft + 2 || horizLoc > viewLeft + 13 || vertLoc < viewTop + 2 || vertLoc > viewTop + 9;

			if (outside)
			{
				int left = (short)this.Owner.Routing.TunePosition(horizLoc - 8);

				this.Owner.MapOps.FocusMapView(participantIdx, left, vertLoc - 6);
			}
		}

		/// <inheritdoc/>
		public ushort MayOccupyTile(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"MayOccupyTile({participantIdx}, {horizLoc}, {vertLoc})");

			ushort blocked = (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 ? (ushort)0 :
				CheckTileOccupation(participantIdx, horizLoc, vertLoc);

			return blocked;
		}

		/// <inheritdoc/>
		public ushort CheckTileOccupation(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyTileOccupation({participantIdx}, {horizLoc}, {vertLoc})");

			bool onOcean = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
			ushort found = 0;

			for (int heading = 1; heading < 9; heading++)
			{
				int nearHoriz = this.Owner.Routing.TunePosition(this.Owner.MapOps.Chart.DirectionHoriz(heading) + horizLoc);
				int nearVert = (short)(this.Owner.MapOps.Chart.DirectionVert(heading) + vertLoc);

				bool nearOcean = this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain;

				if (nearOcean != onOcean)
					continue;

				ushort nearParticipantIdx = this.Owner.MapOps.ObtainTileOwner(nearHoriz, nearVert);

				if (nearParticipantIdx == 0xffff || nearParticipantIdx == (ushort)participantIdx)
					continue;

				if (participantIdx == this.Owner.World.PersonParticipantIdx)
				{
					RevealToHuman((short)nearParticipantIdx, nearHoriz, nearVert);
				}

				found = 1;
				break;
			}

			return found;
		}

		/// <inheritdoc/>
		public ushort ContainsAdjacentForeignUnit(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ContainsAdjacentForeignUnit({participantIdx}, {horizLoc}, {vertLoc})");

			// Check the 8 neighboring tiles for any foreign unit (Zone of Control)
			bool found = MovementRules.HasAdjacentForeignUnit(horizLoc, vertLoc, participantIdx, (x, y) =>
			{
				return (int)(short)this.Owner.MapOps.ObtainTileOwner(x, y);
			});

			ushort result = (ushort)(found ? 1 : 0);

			return result;
		}

		/// <inheritdoc/>
		public ushort FindNearestFriendlyCity(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"LocateNearestFriendlyCity({participantIdx}, {troopIdx})");

			// Without an own city or carrier the target is the place of the unit, so it stays (direction 0; the original took
			// what was left on the stack)
			int unitHoriz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			int unitVert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			int targetHoriz = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int targetVert = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
			int nearest = 999;

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				int distance = WrappedChebyshev(unitHoriz - town.Place.X, unitVert - town.Place.Y);

				if (distance < nearest)
				{
					nearest = distance;
					targetHoriz = town.Place.X;
					targetVert = town.Place.Y;
				}
			}

			for (short carrierIdx = 0; carrierIdx < UnitMemory.TroopTally; carrierIdx++)
			{
				if (this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Kind) != UnitKindTable.Carrier)
					continue;

				int carrierHoriz = this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Horiz);
				int carrierVert = this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Vert);
				int distance = WrappedChebyshev(unitHoriz - carrierHoriz, unitVert - carrierVert);

				if (distance < nearest)
				{
					nearest = distance;
					targetHoriz = carrierHoriz;
					targetVert = carrierVert;
				}
			}

			int best = this.Owner.Helpers.ComputeDistanceFromOffsets(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - targetHoriz,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - targetVert);
			ushort heading = 0;

			for (int direction = 1; direction <= 8; direction++)
			{
				// Like the original, the step doesn't wrap around the map
				int stepHoriz = (short)(unitHoriz + this.Owner.MapOps.Chart.DirectionHoriz(direction));
				int stepVert = (short)(unitVert + this.Owner.MapOps.Chart.DirectionVert(direction));

				ushort stepOwner = this.Owner.MapOps.ObtainTileOwner(stepHoriz, stepVert);

				if (stepOwner != 0xffff && stepOwner != (ushort)participantIdx)
					continue;

				if ((this.Owner.MapOps.ObtainTileImprovements(stepHoriz, stepVert) & 1) != 0 &&
					this.Owner.MapOps.ObtainTileOwnerBits(stepHoriz, stepVert) != (ushort)participantIdx)
					continue;

				int distance = this.Owner.Helpers.ComputeDistanceFromOffsets(stepHoriz - targetHoriz, stepVert - targetVert);

				if (distance < best)
				{
					best = distance;
					heading = (ushort)direction;
				}
			}

			return heading;
		}

		/// <summary>The larger of the two steps, the horizontal one wrapping over 40</summary>
		private static int WrappedChebyshev(int horizStep, int vertStep)
		{
			horizStep = Math.Abs(horizStep);

			if (horizStep > 40)
				horizStep = 80 - horizStep;

			return Math.Max(horizStep, Math.Abs(vertStep));
		}

		/// <summary>A fortified unit of the city standing in it</summary>
		private bool IsFortifiedHomeUnitIn(City town, short townIdx, short participantIdx, short troopIdx)
		{
			return this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) == (byte)(sbyte)town.Place.X &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) == (byte)(sbyte)town.Place.Y &&
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == townIdx &&
				(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatus.Fortified) != 0;
		}

		/// <summary>The unit forgets its "go to" order</summary>
		private void ClearTravel(short participantIdx, short troopIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
		}

		/// <summary>
		/// Fills the free record of the new unit, puts it on the map and lets it see around
		/// </summary>
		private void SetUpUnit(short participantIdx, short troopIdx, short kind, int horizLoc, int vertLoc)
		{
			byte participantBit = (byte)(1 << participantIdx);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.FollowingInStack, 0xff);

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);
			this.Owner.MapOps.PlaceUnitOnTile(participantIdx, troopIdx, horizLoc, vertLoc);

			this.Owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)participantBit;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)kind);

			// A new unit is fully healthy
			this.Owner.World.Vitals.Wipe(participantIdx, troopIdx);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants, participantBit);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps,
				(byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc));

			byte baseTown = this.troops.Pull(participantIdx, troopIdx, UnitMemory.BaseTown);
			ushort baseOwner = baseTown < 128 ? (ushort)this.Owner.World.Towns[baseTown].ParticipantIdx : (ushort)0xffff;

			if (baseOwner != (ushort)participantIdx)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, 0xff);
			}

			// Planes start with one turn of fuel used
			byte fuel = this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps);

			if (fuel != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(fuel - 1));
			}

			this.Owner.World.Participants[participantIdx].LiveTroops[kind]++;

			if (Turn.QuietUnitSetup == 0)
			{
				if (MapView.Revealed != 0 || participantIdx == this.Owner.World.PersonParticipantIdx ||
					(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << this.Owner.World.PersonParticipantIdx)) != 0)
				{
					this.Owner.MapOps.RepaintMapTile(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
				}

				ShiftUnit(horizLoc, vertLoc, participantIdx, troopIdx, 1);
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx && this.Owner.World.RoundTally != 0 &&
				Turn.QuietUnitSetup == 0)
			{
				if (this.Owner.World.Participants[participantIdx].TroopTally == 0)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstunit1);
				}

				if (this.Owner.World.Participants[participantIdx].TroopTally == 1)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstunit2);
				}
			}
		}

		/// <summary>Does the unit stand on an ocean tile</summary>
		private bool IsOnOcean(short participantIdx, short troopIdx)
		{
			return this.Owner.MapOps.ObtainTerrainType(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain;
		}

		/// <summary>Adds to a word of the data segment (with the 16 bit overflow of the original)</summary>
		private void AddToGlobal(ushort address, int amount)
		{
			this.Processor.EmitGlobalWord(address, (ushort)(this.Processor.PullGlobalWord(address) + amount));
		}

		private void ClearStatus(short participantIdx, short troopIdx, UnitStatus flags)
		{
			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & ~(int)flags));
		}

		/// <summary>The full moves of the unit type of the unit, in thirds</summary>
		private byte FullSteps(short participantIdx, short troopIdx)
		{
			return (byte)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));
		}

		/// <summary>Move cost of the terrain type</summary>
		private sbyte TerrainMoveCost(int terrain)
		{
			return (sbyte)TerrainField(terrain, TerrainTable.MoveCost);
		}
	}
}
