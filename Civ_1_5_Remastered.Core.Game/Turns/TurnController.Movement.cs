using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController
	{
		private (int Round, Dictionary<(short, short), int> Steps) freeRides = (-1, new Dictionary<(short, short), int>());

		public bool RideForFree(short participantIndex, short troopIndex, short horizontalLocation, short verticalLocation)
		{
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RailroadRules.RailroadTechnology) != 0;

			if (!RailroadRules.HasRailroad(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation), knowsRailroad))
			{
				return false;
			}

			bool byHand = participantIndex == this.Owner.World.PersonParticipantIndex &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) == 0xff;

			if (this.freeRides.Round != this.Owner.World.RoundTally)
			{
				this.freeRides = (this.Owner.World.RoundTally, new Dictionary<(short, short), int>());
			}

			int ridden = this.freeRides.Steps.GetValueOrDefault((participantIndex, troopIndex));

			if (!RailroadRules.IsFreeStep(byHand, ridden))
			{
				return false;
			}

			if (!byHand)
			{
				this.freeRides.Steps[(participantIndex, troopIndex)] = ridden + 1;
			}

			return true;
		}

		private static ushort KeyDirection(ushort key)
		{
			return (short)key switch
			{
				>= 1 and <= 8 => key,
				0x4700 => 8,
				0x4800 => 1,
				0x4900 => 2,
				0x4d00 => 3,
				0x5100 => 4,
				0x5000 => 5,
				0x4f00 => 6,
				0x4b00 => 7,
				_ => 0,
			};
		}

		private void CenterView(short participantIndex)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				this.Owner.MapOperations.FocusMapView(participantIndex,
					(short)this.turn.CursorHorizontal - Viewport.MiddleColumn,
					(short)this.turn.CursorVertical - Viewport.MiddleRow);
			}
			else
			{
				this.Owner.MapOperations.FocusMapView(participantIndex,
					(short)this.turn.UnitHorizontal - Viewport.MiddleColumn,
					(short)this.turn.UnitVertical - Viewport.MiddleRow);
			}
		}

		private void MoveMarkedTile(short participantIndex)
		{
			if (((sbyte)this.Owner.World.ChartSight[this.turn.CursorHorizontal, this.turn.CursorVertical] & (1 << (byte)participantIndex)) != 0)
			{
				this.Owner.MapOperations.RepaintMapTile(
					(short)this.turn.CursorHorizontal,
					(short)this.turn.CursorVertical);
			}

			this.turn.CursorHorizontal = (ushort)this.Owner.Routing.TunePosition(
				(short)Tables.DirectionHorizontalAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.CursorHorizontal);

			this.turn.CursorVertical = (ushort)this.Owner.Helpers.ClampValue(
				(short)Tables.DirectionVerticalAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.CursorVertical,
				0, 49);

			if (CheckPositionOnScreen((short)this.turn.CursorHorizontal, (short)this.turn.CursorVertical) == 0)
			{
				this.Owner.MapOperations.FocusMapView(participantIndex,
					(short)this.turn.CursorHorizontal - Viewport.MiddleColumn - 1,
					(short)this.turn.CursorVertical - Viewport.MiddleRow - 1);
			}
		}

		private bool BarbarianPillages(short participantIndex)
		{
			if (participantIndex != 0)
			{
				return false;
			}

			short troopIndex = (short)this.turn.Troop;
			byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

			if (kind == UnitKindTable.Diplomat)
			{
				return false;
			}

			short horizontalLocation = (short)this.turn.UnitHorizontal;
			short verticalLocation = (short)this.turn.UnitVertical;
			this.turn.TargetImprovements = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

			if (((byte)this.turn.TargetImprovements & 1) != 0 || ((byte)this.turn.TargetImprovements & 6) == 0 || kind == 3 || kind == 7)
			{
				return false;
			}

			if (this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation)].ParticipantIndex != this.Owner.World.PersonParticipantIndex)
			{
				return false;
			}

			this.Owner.MapOperations.RemoveTileImprovements(6, horizontalLocation, verticalLocation);

			if (((sbyte)this.Owner.World.ChartSight[this.turn.UnitHorizontal, this.turn.UnitVertical] & (1 << (this.Owner.World.PersonParticipantIndex & 0xff))) != 0)
			{
				this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);
				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		private bool FindMoveTarget(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			this.turn.TargetHorizontal = (ushort)this.Owner.Routing.TunePosition(
				(short)Tables.DirectionHorizontalAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.UnitHorizontal);

			this.turn.TargetVertical = (ushort)this.Owner.Helpers.ClampValue(
				(short)Tables.DirectionVerticalAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.UnitVertical,
				0, 49);

			this.turn.StartTerrain = this.Owner.MapOperations.ObtainTerrainType((short)this.turn.UnitHorizontal, (short)this.turn.UnitVertical);
			this.turn.TargetTerrain = this.Owner.MapOperations.ObtainTerrainType((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);
			this.turn.TargetImprovements = this.Owner.MapOperations.ObtainTileImprovements((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);
			this.turn.OtherUnit = this.Owner.MapOperations.ObtainUnitAtTile((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);

			this.indexDestination = (ushort)(participantIndex * UnitMemory.ParticipantStride);

			return this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;
		}

		/// <summary>
		/// On Europe the map does not wrap: no unit steps from the last column to the first or back. A human
		/// player's unit just stays; a computer unit ends its moves, so it does not try the same step again.
		/// </summary>
		private bool StoppedAtTheEdge(short participantIndex)
		{
			if (!EuropeMap.Played(this.Owner.World.Extras.World) ||
				!EuropeMap.CrossesEdge((short)this.turn.UnitHorizontal, (short)this.turn.TargetHorizontal))
			{
				return false;
			}

			short troopIndex = (short)this.turn.Troop;

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			}
			else
			{
				this.Owner.Services.PerformSound(0x25, 0);
			}

			return true;
		}

		private bool StoppedByZoneOfControl(short participantIndex)
		{
			if (this.turn.OtherUnit != 0xffff ||
				this.Owner.UnitOperations.MayOccupyTile(participantIndex, (short)this.turn.UnitHorizontal, (short)this.turn.UnitVertical) == 0 ||
				this.Owner.UnitOperations.MayOccupyTile(participantIndex, (short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical) == 0)
			{
				return false;
			}

			short troopIndex = (short)this.turn.Troop;

			if (this.troops.TroopKind(participantIndex, troopIndex) >= UnitKindTable.Diplomat || this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				return false;
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Services.PerformSound(0x25, 0);
			}

			if ((this.Owner.World.SessionPreferenceMarkers & 1) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicZoneOfControl);
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

			return true;
		}

		private MoveStep EnterCityAsDiplomatOrCaravan(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.troops.TroopKind(participantIndex, troopIndex) < UnitKindTable.Diplomat)
			{
				return MoveStep.GoesOn;
			}

			this.turn.City = this.Owner.Helpers.FindCityAtPosition((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);

			if (this.turn.City == 0xffff)
			{
				return MoveStep.NoCity;
			}

			GameModel.State.City town = this.Owner.World.Towns[this.turn.City];

			if (town.ParticipantIndex != participantIndex && this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				return MoveStep.Done;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				this.indexDestination = (ushort)town.ParticipantIndex;

				if (participantIndex != town.ParticipantIndex)
				{
					short person = this.Owner.World.PersonParticipantIndex;

					if (person == town.ParticipantIndex)
					{
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));

						this.Owner.UnitOperations.PanMapToPosition(person,
							this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
							this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

						this.Owner.TextPainter.WaitForTime(30);

						this.Owner.UnitOperations.ChoreographUnitCombat(participantIndex,
							(short)this.turn.Troop,
							this.turn.Direction);
					}

					this.Owner.Diplomats.DiplomatArrivesAtCity((short)this.turn.City,
						participantIndex,
						(short)this.turn.Troop);

					return MoveStep.Done;
				}
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0x1b)
			{
				return MoveStep.GoesOn;
			}

			sbyte home = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.BaseTown);
			this.indexDestination = (ushort)(home * 0x1c);
			byte homeIndex = this.troops.Pull(participantIndex, troopIndex, UnitMemory.BaseTown);

			this.turn.Result = (ushort)this.Owner.Helpers.ObtainDistance(
				(short)this.turn.TargetHorizontal,
				this.Owner.World.Towns[homeIndex].Place.X,
				(short)this.turn.TargetVertical,
				this.Owner.World.Towns[homeIndex].Place.Y);

			if (town.ParticipantIndex == participantIndex && ((sbyte)town.PresentMakingIndex >= -24 || (sbyte)town.PresentMakingIndex < -48) && (short)this.turn.Result < 10)
			{
				ushort homeContinent = this.Owner.MapOperations.ObtainContinentIdentifier(
					this.Owner.World.Towns[homeIndex].Place.X,
					this.Owner.World.Towns[homeIndex].Place.Y);
				this.indexDestination = homeContinent;

				if (this.Owner.MapOperations.ObtainContinentIdentifier((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical) == homeContinent)
				{
					return MoveStep.GoesOn;
				}
			}

			this.Owner.Words.Set("Will you?\n Keep moving\n Establish trade route\n");

			if (town.ParticipantIndex == participantIndex && (sbyte)town.PresentMakingIndex < -24 && (sbyte)town.PresentMakingIndex >= -48)
			{
				this.Owner.Words.Append(" Help build WONDER.\n");

				if ((short)this.turn.Result < 10)
				{
					Menu.DisabledLines = 0x2;
				}
			}

			if (town.ParticipantIndex == participantIndex)
			{
				this.turn.Result = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
			else
			{
				this.turn.Result = 0x1;
			}

			if (this.turn.Result == 1 &&
				this.Owner.Conquest.FoundTradeRoute(participantIndex, (short)this.turn.Troop, (short)this.turn.City) != 0)
			{
				return MoveStep.Done;
			}

			if (this.turn.Result != 2)
			{
				return MoveStep.GoesOn;
			}

			short materials = (short)(10 * this.troops.KindField(participantIndex, troopIndex, UnitKindTable.Price));
			this.Owner.World.Towns[this.turn.City].MaterialsTally += materials;

			this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);

			return MoveStep.Done;
		}

		private void BribeUnitOnTheWay(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIndex, troopIndex);

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != UnitKindTable.Diplomat || this.turn.OtherUnit == 0xffff ||
				participantIndex == (short)MapView.UnitAtTilePlayer)
			{
				return;
			}

			this.Owner.Diplomats.BuyUnit((short)MapView.UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				participantIndex);

			this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHorizontal), 0xff);
		}

		private MoveCheck CheckMoveTile(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIndex, troopIndex);
			short owner = (short)MapView.UnitAtTilePlayer;

			if (this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					if (participantIndex != (short)MapView.UnitAtTilePlayer || this.turn.OtherUnit == 0xffff ||
						(short)this.Owner.UnitOperations.CountSpecialUnitsInStack(participantIndex, (short)this.turn.OtherUnit) <= 0)
					{
						return MoveCheck.Done;
					}

					ushort status = (ushort)(unit + UnitMemory.Origin + UnitMemory.Status);
					this.Processor.EmitByte(this.Processor.SegmentData.Word, status, (byte)(this.Processor.PullByte(this.Processor.SegmentData.Word, status) | 1));
					this.Processor.EmitByte(this.Processor.SegmentData.Word, status, (byte)(this.Processor.PullByte(this.Processor.SegmentData.Word, status) & 0xf3));
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps), 3);

					return MoveCheck.Moves;
				}
			}

			if (this.turn.OtherUnit == 0xffff || participantIndex == (short)MapView.UnitAtTilePlayer)
			{
				return MoveCheck.Moves;
			}

			if (this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					ShowHelpForHuman(GameTexts.TopicAmphibious);
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHorizontal), 0xff);

					return MoveCheck.Done;
				}
			}

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == UnitKindTable.Submarine)
			{
				return MoveCheck.Done;
			}

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.AssaultPower) == 0)
				{
					return MoveCheck.Done;
				}
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				return MoveCheck.Done;
			}

			short guardian = (short)this.turn.OtherUnit;
			this.indexDestination = (ushort)(guardian * UnitMemory.TroopStride);

			if (this.troops.KindField(owner, guardian, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0xe && ((byte)this.turn.TargetImprovements & 1) == 0)
				{
					ShowHelpForHuman(GameTexts.TopicFighter);
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHorizontal), 0xff);

					return MoveCheck.Done;
				}
			}

			return MoveCheck.Attacks;
		}

		private bool ConfirmAttack(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) >= 3)
			{
				return true;
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Words.Set("Attack at\n");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((sbyte)this.Troops.PullAt(UnitMemory.Offset(participantIndex, troopIndex), UnitMemory.LeftoverSteps), 10));
				this.Owner.Words.Append("/3 strength?\n Cancel\n Attack\n");
				if (this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 16) == 1)
				{
					return true;
				}

				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(UnitMemory.Offset(participantIndex, troopIndex) + UnitMemory.Origin + UnitMemory.TravelHorizontal), 0xff);
				return false;
			}

			return this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) >= 2;
		}

		private bool FightAt(short participantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			short troopIndex = (short)this.turn.Troop;
			short horizontalLocation = (short)this.turn.TargetHorizontal;
			short verticalLocation = (short)this.turn.TargetVertical;

			if (MapView.UnitAtTilePlayer == (ushort)person)
			{
				this.Owner.UnitOperations.PanMapToPosition(person, horizontalLocation, verticalLocation);
			}

			this.turn.OtherUnit = this.Owner.UnitOperations.FindBestGuardianInStack((short)MapView.UnitAtTilePlayer,
				(short)this.turn.OtherUnit);

			if (participantIndex == person || MapView.UnitAtTilePlayer == (ushort)person)
			{
				Turn.CountLosses = 0x1;
			}

			this.turn.Result = this.Owner.Combat.SettleBattle(participantIndex,
				(short)this.turn.Troop,
				(short)MapView.UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				1);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			Turn.CountLosses = 0x0;

			if (this.turn.Result == 0xffff)
			{
				return false;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xff)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
				return true;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) - 3));

			if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) < 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xf)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			}

			if (((byte)this.turn.TargetImprovements & 1) == 0)
			{
				if (((byte)this.turn.TargetImprovements & 0x20) == 0)
				{
				}

				return false;
			}

			this.turn.City = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);
			GameModel.State.City town = this.Owner.World.Towns[this.turn.City];

			if (town.ParticipantIndex != person)
			{
				town.ConditionMarker |= 0x10;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0xff && !town.Has(CityStructure.CityWalls) &&
				this.turn.StartTerrain != MapRules.OceanTerrain &&
				(this.Owner.World.HardnessTier != 0 || MapView.UnitAtTilePlayer != (ushort)person))
			{
				town.TrueExtent--;
			}

			if (town.TrueExtent == 0)
			{
				this.Owner.Advisors.FinalizeProduction(
					(short)this.turn.City,
					this.turn.TargetHorizontal,
					this.turn.TargetVertical);

				this.Owner.NewGameWizard.DestroyCivilization((short)MapView.UnitAtTilePlayer, participantIndex);

				if (participantIndex == person)
				{
					this.turn.OtherUnit = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

					if (this.turn.OtherUnit != 0xffff)
					{
						short owner = (short)MapView.UnitAtTilePlayer;
						short left = (short)this.turn.OtherUnit;
						this.troops.Assign(owner, left, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(owner, left, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));
					}
				}
			}

			if (participantIndex == person || MapView.UnitAtTilePlayer == (ushort)person || MapView.Revealed != 0)
			{
				town.ShownExtent = (sbyte)town.TrueExtent;

				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}

			if (this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation) == 0xffff && MapView.UnitAtTilePlayer != (ushort)person)
			{
				this.Owner.Computer.RefreshCityProduction((short)MapView.UnitAtTilePlayer, this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation));
			}

			return false;
		}

		private bool PayForMove(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIndex, troopIndex);

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && ((byte)this.turn.TargetImprovements & 1) == 0)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
					return false;
				}
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.LandUnits && ((byte)this.turn.TargetImprovements & 1) != 0 &&
				this.Owner.MapOperations.ObtainTileOwnerBits((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical) != (ushort)participantIndex)
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0x19)
				{
					ShowHelpForHuman(GameTexts.TopicOccupy);
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHorizontal), 0xff);
					return false;
				}

				this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);

				this.Owner.Combat.ExplodeNuclearWeapon(participantIndex,
					(short)this.turn.TargetHorizontal,
					(short)this.turn.TargetVertical);

				return false;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) == 0)
			{
				return false;
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				short horizontalLocation = (short)this.turn.UnitHorizontal;
				short verticalLocation = (short)this.turn.UnitVertical;

				if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & (byte)this.turn.TargetImprovements & 8) != 0 &&
					this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) != 0)
				{
					byte cost = 1;

					if (RideForFree(participantIndex, troopIndex, horizontalLocation, verticalLocation))
					{
						cost = 0;
					}

					this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) - cost));
				}
				else
				{
					short terrainCost = (short)(3 * (sbyte)Tables.TerrainField(this.turn.TargetTerrain, TerrainTable.MoveCost));

					if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) != 3)
					{
						int roll = this.Owner.Runtime.Dice.DrawInteger(terrainCost);

						if (roll > this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps))
						{
							this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
							return false;
						}
					}

					this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) - (byte)terrainCost));
				}
			}
			else
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) - 3));
			}

			if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) < 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			}

			return true;
		}

		private bool ComputerMayMove(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short person = this.Owner.World.PersonParticipantIndex;

			if (participantIndex != person)
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHeading) != 0xff &&
					(ushort)(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHeading) ^ 4) == this.turn.Direction &&
					(short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, (short)this.turn.Troop, 2) <= 2)
				{
					ushort targetBase = (ushort)(UnitMemory.Origin + (UnitMemory.ParticipantStride * participantIndex));
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(targetBase + UnitMemory.TravelHeading), 0xff);
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(targetBase + UnitMemory.TravelHorizontal), 0xff);
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(targetBase + UnitMemory.LeftoverSteps), 0);

					return false;
				}

				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, (byte)this.turn.Direction);
			}

			if (this.turn.OtherUnit == 0xffff || participantIndex != (short)MapView.UnitAtTilePlayer || participantIndex == person ||
				((byte)this.turn.TargetImprovements & 1) != 0)
			{
				return true;
			}

			ushort group = this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup);
			this.indexDestination = group;

			if (group != UnitKindTable.LandUnits &&
				(group != UnitKindTable.AirUnits || this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) == 0))
			{
				return true;
			}

			this.turn.Result = this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, (short)this.turn.OtherUnit, 2);
			short threshold = (short)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) != 0 ? 4 : 2);

			return threshold > (short)this.turn.Result;
		}

		private bool EnterTerritory(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIndex, troopIndex);
			short person = this.Owner.World.PersonParticipantIndex;
			short horizontalLocation = (short)this.turn.TargetHorizontal;
			short verticalLocation = (short)this.turn.TargetVertical;

			this.turn.Dot = this.Owner.VideoCard.ObtainDot(2, horizontalLocation + 80, verticalLocation);

			if (this.turn.Dot == 0 || (short)this.turn.Dot >= 8)
			{
				return true;
			}

			short owner = (short)this.turn.Dot;

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.turn.StartTerrain != MapRules.OceanTerrain &&
				MayTreatyStop(participantIndex, troopIndex, owner, person))
			{
				if (participantIndex == person)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

					if (this.Owner.Combat.AffirmBreakTreaty((short)this.turn.Dot) == 0xffff)
					{
						return false;
					}

					this.Owner.Politics.ClearDiplomacyFlags(participantIndex, (short)this.turn.Dot, 2);
				}
				else if (this.turn.Dot == (ushort)person)
				{
					if ((this.Owner.World.Participants[this.turn.Dot].Relations[participantIndex] & 2) != 0)
					{
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

						return false;
					}
				}
			}

			if (participantIndex != (short)this.turn.Dot && (this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0xe) != 0)
			{
				byte mask = (byte)(1 << ((byte)this.turn.Dot & 0x1f));
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | mask));
			}

			return true;
		}

		private bool MayTreatyStop(short participantIndex, short troopIndex, short owner, short person)
		{
			if (this.troops.TroopKind(participantIndex, troopIndex) >= UnitKindTable.Diplomat || ((byte)this.turn.TargetImprovements & 0xf) == 0 ||
				owner == participantIndex || (participantIndex != person && owner != person))
			{
				return false;
			}

			return this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.Owner.MapOperations.ObtainTileOwner((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical) != (ushort)participantIndex;
		}

		private void ShowMove(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short person = this.Owner.World.PersonParticipantIndex;
			int personBit = 1 << (person & 0xff);

			this.turn.TileOwner = this.Owner.MapOperations.ObtainTileOwnerBits((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);

			if (participantIndex == 0 && this.turn.TargetTerrain != MapRules.OceanTerrain)
			{
				if (((sbyte)this.Owner.World.ChartSight[this.turn.UnitHorizontal, this.turn.UnitVertical] & personBit) != 0)
				{
					this.indexDestination = (ushort)((troopIndex * UnitMemory.TroopStride) + UnitMemory.Origin + UnitMemory.ShownToParticipants);

					if ((this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & personBit) == 0 &&
						this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity((short)this.turn.UnitHorizontal, (short)this.turn.UnitVertical)].ParticipantIndex == person)
					{
						byte sight = (byte)this.Owner.World.ChartSight[this.turn.UnitHorizontal, this.turn.UnitVertical];
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | sight));
					}
				}
			}

			if (((byte)this.turn.TargetImprovements & 1) != 0 && this.turn.TileOwner != (ushort)participantIndex &&
				((sbyte)this.Owner.World.ChartSight[this.turn.TargetHorizontal, this.turn.TargetVertical] & personBit) != 0)
			{
				bool embassy = (this.Owner.World.Participants[person].Relations[participantIndex] & 0x40) != 0;

				if (!embassy)
				{
					embassy = (this.Owner.World.Participants[person].Relations[this.turn.TileOwner] & 0x40) != 0;
				}

				if (embassy)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
				(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | personBit));
				}
			}

			if (this.turn.TargetTerrain == MapRules.OceanTerrain &&
				(this.Owner.MapOperations.ObtainTileImprovements((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical) & 2) != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
				(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | (byte)(1 << ((byte)this.turn.TileOwner & 0x1f))));
			}

			if ((this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & personBit) == 0 && participantIndex != person &&
				(MapView.Revealed == 0 || participantIndex == 0))
			{
				return;
			}

			if ((this.Owner.World.SessionPreferenceMarkers & 0x20) != 0 || participantIndex == person)
			{
				this.Owner.UnitOperations.PanMapToPosition(person,
					(short)this.turn.TargetHorizontal,
					(short)this.turn.TargetVertical);

				this.Owner.UnitOperations.ChoreographUnitCombat(participantIndex,
					(short)this.turn.Troop,
					this.turn.Direction);
			}
			else
			{
				this.Owner.MapOperations.DrawMapTile((short)this.turn.UnitHorizontal, (short)this.turn.UnitVertical);
			}
		}

		private void MoveUnit(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIndex, troopIndex);
			short person = this.Owner.World.PersonParticipantIndex;

			this.turn.Cargo = (ushort)this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.FollowingInStack);

			this.Owner.MapOperations.RemoveUnitFromTile(participantIndex,
				(short)this.turn.Troop,
				(short)this.turn.UnitHorizontal,
				(short)this.turn.UnitVertical);

			this.turn.CarrierHorizontal = this.turn.UnitHorizontal;
			this.turn.CarrierVertical = this.turn.UnitVertical;

			this.Owner.MapOperations.PlaceUnitOnTile(participantIndex,
				(short)this.turn.Troop,
				(short)this.turn.TargetHorizontal,
				(short)this.turn.TargetVertical);

			this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.Horizontal), (byte)this.turn.TargetHorizontal);
			this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.Vertical), (byte)this.turn.TargetVertical);
			this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(unit + UnitMemory.Origin + UnitMemory.ShownToParticipants), 0);

			if (participantIndex != person)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x30));
				}
			}

			if (this.turn.Dot != 0 && (short)this.turn.Dot < 8 && this.turn.Dot != (ushort)participantIndex)
			{
				this.Owner.Services.AssignDot(2, (short)this.turn.TargetHorizontal + 80,
					(short)this.turn.TargetVertical,
					0);
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) == this.turn.TargetHorizontal &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelVertical) == this.turn.TargetVertical)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

				if (participantIndex != person)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
				}
			}
		}

		private void TakeCargoAlong(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort player = (ushort)(participantIndex * UnitMemory.ParticipantStride);

			if (this.turn.StartTerrain == MapRules.OceanTerrain && this.turn.TargetTerrain != MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
				}
			}

			this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind));

			if ((Tables.UnitKindWord((sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.CarrierKind != UnitKindTable.Carrier) || this.turn.Cargo == 0xffff)
			{
				return;
			}

			this.turn.CargoTally = 0;
			this.turn.Count = 0;

			while (true)
			{
				short member = (short)this.turn.Cargo;
				this.indexDestination = (ushort)((member * UnitMemory.TroopStride) + (participantIndex * UnitMemory.ParticipantStride));
				this.turn.NextCargo = (ushort)this.troops.PullSigned(participantIndex, member, UnitMemory.FollowingInStack);

				ushort cargoGroup = (ushort)(this.troops.KindField(participantIndex, troopIndex, UnitKindTable.FerryRoom) == 0 ? 1 : 0);

				if (cargoGroup == this.troops.KindField(participantIndex, member, UnitKindTable.LandGroup))
				{
					byte status = this.troops.Pull(participantIndex, member, UnitMemory.Status);
					bool taken = (status & 1) != 0 || this.turn.StartTerrain == MapRules.OceanTerrain;

					if (!taken && participantIndex != this.Owner.World.PersonParticipantIndex && (status & 0xc) == 0)
					{
						taken = this.troops.Pull(participantIndex, member, UnitMemory.FollowingInStack) != 0xff;
					}

					if (taken)
					{
						CarryUnit(participantIndex, troopIndex, member);
					}
				}

				this.turn.Cargo = this.turn.NextCargo;
				this.turn.CargoTally = (ushort)(this.turn.CargoTally + 1);

				if ((short)this.turn.CargoTally >= 0x14 || this.turn.Cargo == 0xffff)
				{
					return;
				}

				this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind));

				if ((short)Tables.UnitKindWord((sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) <= (short)this.turn.Count &&
					(byte)this.turn.CarrierKind != UnitKindTable.Carrier && participantIndex != 0)
				{
					return;
				}
			}
		}

		private void CarryUnit(short participantIndex, short troopIndex, short member)
		{
			this.Owner.MapOperations.RemoveUnitFromTile(participantIndex,
				(short)this.turn.Cargo,
				(short)this.turn.CarrierHorizontal,
				(short)this.turn.CarrierVertical);

			this.Owner.MapOperations.PlaceUnitOnTile(participantIndex,
				(short)this.turn.Cargo,
				(short)this.turn.TargetHorizontal,
				(short)this.turn.TargetVertical);

			this.troops.Assign(participantIndex, member, UnitMemory.Horizontal, (byte)this.turn.TargetHorizontal);
			this.troops.Assign(participantIndex, member, UnitMemory.Vertical, (byte)this.turn.TargetVertical);
			this.troops.Assign(participantIndex, member, UnitMemory.ShownToParticipants, 0);
			this.troops.Assign(participantIndex, member, UnitMemory.TravelHeading, 0xff);

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.FerryRoom) != 0)
			{
				this.troops.Assign(participantIndex, member, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, member, UnitMemory.Status) | 1));
			}

			ushort roundsAway = this.troops.KindField(participantIndex, member, UnitKindTable.RoundsAway);
			this.indexDestination = roundsAway;

			if (roundsAway != 0)
			{
				this.troops.Assign(participantIndex, member, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
			}

			this.turn.Count = (ushort)(this.turn.Count + 1);
		}

		private void ArriveAtTile(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short person = this.Owner.World.PersonParticipantIndex;
			int personBit = 1 << (person & 0xff);

			if (((byte)this.turn.TargetImprovements & 1) != 0)
			{
				this.turn.City = this.Owner.Helpers.FindCityAtPosition((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);

				if (this.turn.City != 0xffff && this.turn.City < this.Owner.World.Towns.Length &&
					this.Owner.World.Towns[this.turn.City].ParticipantIndex != participantIndex)
				{
					byte mask = (byte)(1 << ((byte)this.turn.TileOwner & 0x1f));
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
						(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | mask));

					this.Owner.Conquest.SeizeCity(participantIndex, (short)this.turn.City, 0);

					this.Owner.MapOperations.TagTileOwned((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);
				}
			}

			this.Owner.MapOperations.MapAssignCityOwner(participantIndex, (short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);

			this.Owner.UnitOperations.ShiftUnit(
				(short)this.turn.TargetHorizontal,
				(short)this.turn.TargetVertical,
				participantIndex,
				(short)this.turn.Troop,
				1);

			if (this.Owner.MapOperations.ContainsHut(this.turn.TargetTerrain, (short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical) != 0)
			{
				this.Owner.UnitOperations.VisitHut(participantIndex, (short)this.turn.Troop);

				this.Owner.World.ChartSight[this.turn.TargetHorizontal, this.turn.TargetVertical] |= 1;
			}

			if (this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
				{
					this.Owner.UnitOperations.VisitEachUnitInStack(participantIndex, (short)this.turn.Troop, 0x1560);
				}
			}

			if (participantIndex != 0 || this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				return;
			}

			if (((sbyte)this.Owner.World.ChartSight[this.turn.TargetHorizontal, this.turn.TargetVertical] & personBit) == 0)
			{
				return;
			}

			this.indexDestination = (ushort)((troopIndex * UnitMemory.TroopStride) + UnitMemory.Origin + UnitMemory.ShownToParticipants);

			if ((this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & personBit) == 0 &&
				this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity((short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical)].ParticipantIndex == person)
			{
				byte sight = (byte)this.Owner.World.ChartSight[this.turn.TargetHorizontal, this.turn.TargetVertical];
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | sight));
			}
		}

		private void FinishMove(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short person = this.Owner.World.PersonParticipantIndex;

			if ((this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & (1 << (person & 0xff))) != 0 || participantIndex == person ||
				(MapView.Revealed != 0 && participantIndex != 0))
			{
				if (participantIndex == person || (this.Owner.World.SessionPreferenceMarkers & 0x20) != 0)
				{
					this.Owner.UnitOperations.PanMapToPosition(person, (short)this.turn.TargetHorizontal, (short)this.turn.TargetVertical);
					this.Owner.MapOperations.DrawUnitOnWorldMap(participantIndex, (short)this.turn.Troop);

					if (participantIndex != person && MapView.WorldMapView != 1)
					{
						this.Owner.TextPainter.WaitForTime(30);
					}
				}
				else
				{
					if (this.Owner.MapOperations.DrawUnitOnWorldMap(participantIndex, (short)this.turn.Troop) != 0)
					{
						this.Owner.TextPainter.WaitForTime(10);
					}
				}
			}

			this.turn.ComputerTries = 0;

			if (((byte)this.turn.TargetImprovements & 1) != 0)
			{
				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway) != 0)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xfd));
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
				}
			}

			this.indexDestination = (ushort)(this.troops.TroopKind(participantIndex, troopIndex) * UnitKindTable.Stride);

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway) == 0 ||
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				return;
			}

			if (this.Owner.UnitOperations.CountUnitsOfTypeInStack(participantIndex, (short)this.turn.Troop, 23) == 0)
			{
				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway));
		}
	}
}
