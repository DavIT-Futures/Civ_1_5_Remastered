namespace Civilization1Remaster.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private void ChooseTransportDestination(short participantIndex, short troopIndex)
		{
			if (this.look.MayStay != 0 && this.look.Role != 5)
			{
				return;
			}

			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];
			byte roles = (byte)this.look.StackRoles;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) == 0xff && ((roles & 2) != 0 || (roles & 1) == 0) &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0x10)
			{
				this.look.BestDistance = 0x3e7;

				for (this.look.Direction = (ushort)(this.Owner.World.RoundTally & 7); (short)this.look.Direction < 0x80;
					this.look.Direction = (ushort)(this.look.Direction + 8))
				{
					GameModel.State.City town = this.Owner.World.Towns[this.look.Direction];
					this.look.Round = (ushort)town.ParticipantIndex;

					if (town.ConditionMarker == 0xff || this.look.Round == (ushort)participantIndex ||
						(player.Relations[this.look.Round] & 0x102) == 2)
					{
						continue;
					}

					if (this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Standing >= 7 &&
						this.look.Round != (ushort)this.Owner.World.PersonParticipantIndex)
					{
						continue;
					}

					this.look.HarbourDistance = (ushort)this.Owner.Helpers.ObtainDistance((short)this.look.Horizontal, town.Place.X, (short)this.look.Vertical, town.Place.Y);

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)town.Place.Y);
					}
				}
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) == 0xff && this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0x10)
			{
				this.look.BestDistance = 0x3e7;
				this.look.Score = 0xffff;

				if (this.Owner.MapOperations.ObtainTerrainType((short)this.look.Horizontal, (short)this.look.Vertical) != MapRules.OceanTerrain)
				{
					this.look.Score = this.look.Continent;
				}

				for (this.look.Direction = 0; (short)this.look.Direction < 0x80;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					GameModel.State.City town = this.Owner.World.Towns[this.look.Direction];
					this.look.Round = (ushort)town.ParticipantIndex;

					if (town.ConditionMarker == 0xff || this.look.Round != (ushort)participantIndex)
					{
						continue;
					}

					this.look.HarbourDistance = (ushort)this.Owner.Helpers.ObtainDistance((short)this.look.Horizontal, town.Place.X, (short)this.look.Vertical, town.Place.Y);

					if ((short)this.look.HarbourDistance < 8)
					{
						continue;
					}

					this.look.TileContinent = this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y);

					if (this.look.TileContinent == this.look.Score)
					{
						continue;
					}

					var continent = player.Landmasses[this.look.TileContinent];

					if (continent.Approach == 5)
					{
						continue;
					}

					if (((byte)(this.Processor.ShiftLeftByte(1, (byte)continent.Approach) & (byte)this.look.StackRoles) & 7) == 0)
					{
						this.look.HarbourDistance = (ushort)(this.look.HarbourDistance << 1);
					}

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)town.Place.Y);
					}
				}
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				return;
			}

			for (this.look.Direction = 2; (short)this.look.Direction < 0x18;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				this.look.Round = (ushort)this.Owner.Runtime.Dice.DrawInteger(9);
				ushort round = (ushort)(this.look.Round << 1);

				this.look.TileHorizontal = (ushort)this.Owner.Routing.TunePosition(((short)Tables.DirectionHorizontalAt(round) *
					(short)this.look.Direction) + this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal));

				ushort step = MultiplyWords(Tables.DirectionVerticalAt(round),
					this.look.Direction);
				this.look.TileVertical = (ushort)(step + this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical));

				short horizontalLocation = (short)this.look.TileHorizontal;
				short verticalLocation = (short)this.look.TileVertical;

				if (verticalLocation <= 2 || verticalLocation >= 0x2f || this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
				{
					continue;
				}

				if (player.Landmasses[this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation)].TownTally == 0)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)this.look.TileHorizontal);
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)this.look.TileVertical);
					return;
				}
			}
		}

		private ushort? PlanTransportLanding(short participantIndex, short troopIndex)
		{
			if (this.look.Role != 5)
			{
				return null;
			}

			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];
			short person = this.Owner.World.PersonParticipantIndex;
			bool forgets = false;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;

				this.look.TileHorizontal = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(direction) +
					(short)this.look.Horizontal);
				this.look.TileVertical = (ushort)(Tables.DirectionVertical(direction) + this.look.Vertical);

				short horizontalLocation = (short)this.look.TileHorizontal;
				short verticalLocation = (short)this.look.TileVertical;

				this.look.TileOwner = this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation);

				if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
				{
					continue;
				}

				if (this.Owner.MapOperations.CheckMapLimits(horizontalLocation, verticalLocation) == 0)
				{
					continue;
				}

				ushort owner = this.look.TileOwner;

				if (owner != 0xffff && owner != (ushort)participantIndex)
				{
					continue;
				}

				if (owner == (ushort)participantIndex &&
					(short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, (short)this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation), 2) >= 2)
				{
					continue;
				}

				if ((player.Relations[person] & 2) != 0 && this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation) == (ushort)person &&
					(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0xf) != 0)
				{
					continue;
				}

				this.look.TileContinent = this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation);
				this.look.LoadWanted = 0;

				var continent = player.Landmasses[this.look.TileContinent];

				if ((continent.Assault < 16 && continent.Approach != 5) ||
					((byte)(this.Processor.ShiftLeftByte(1, (byte)continent.Approach) & (byte)this.look.StackRoles) & 6) != 0)
				{
					this.look.LoadWanted = 1;
				}

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff &&
					this.Owner.MapOperations.ObtainContinentIdentifier(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHorizontal),
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelVertical)) == this.look.TileContinent)
				{
					this.look.LoadWanted = 1;
				}

				if (this.look.LoadWanted == 0 ||
					this.Owner.World.Landmasses[this.look.TileContinent].ConstructionSpotTally < 5 ||
					(short)this.look.TileVertical <= 1 || (short)this.look.TileVertical >= 0x30)
				{
					forgets = true;
					break;
				}

				if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) > 3)
				{
					this.look.Round = MayComputerEnterTile(participantIndex, this.look.Horizontal, this.look.Vertical);

					if ((short)this.look.Round > 0)
					{
						return this.look.Round;
					}
				}

				if (player.Landmasses[this.look.TileContinent].Approach == 1)
				{
					SendUnitToStrategicLocation(participantIndex, (short)this.look.Horizontal, (short)this.look.Vertical, 3, 5);
				}

				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

				return 0x75;
			}

			if (forgets && (this.Owner.World.RoundTally & 3) == 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			}

			if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) > 3)
			{
				return null;
			}

			this.look.TileHorizontal = (ushort)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal) + this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal)) >> 1);
			this.look.TileVertical = (ushort)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) + this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelVertical)) >> 1);

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) == 0xff)
			{
				return null;
			}

			if (this.Owner.MapOperations.ObtainTerrainType((short)this.look.TileHorizontal, (short)this.look.TileVertical) == MapRules.OceanTerrain)
			{
				SendUnitToStrategicLocation(participantIndex, (short)this.look.TileHorizontal, (short)this.look.TileVertical, 3, 3);
			}

			return null;
		}

		private bool ChooseStagingCity(short participantIndex, short troopIndex)
		{
			short capacity = (short)(this.look.CityDistance != 0 ? 1 : 3);

			if ((capacity <= (short)this.look.LandUnits && this.look.StackRoles != 1) || this.look.Role != 5)
			{
				return false;
			}

			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];

			this.look.BestDistance = 0x3e7;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.Harbour];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex || (town.ConditionMarker & 2) == 0)
				{
					continue;
				}

				this.look.HarbourDistance = (ushort)this.Owner.Helpers.ObtainDistance((short)this.look.Horizontal, town.Place.X, (short)this.look.Vertical, town.Place.Y);
				this.look.TileContinent = this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y);

				if (player.Landmasses[this.look.TileContinent].Assault < 16)
				{
					continue;
				}

				this.look.TileUnit = this.Owner.MapOperations.ObtainUnitAtTile(town.Place.X, town.Place.Y);

				if (this.look.TileUnit != 0xffff)
				{
					short attackers = (short)this.Owner.UnitOperations.CountUnitsWithFlagInStack(participantIndex, (short)this.look.TileUnit, 5);

					if ((short)(this.look.HarbourDistance == 0 ? 1 : 0) < attackers)
					{
						this.look.HarbourDistance = (ushort)(this.look.HarbourDistance + 0x10);
					}
				}

				if (player.Landmasses[this.look.TileContinent].Approach == 5)
				{
					short guard = CentralProcessor.SignedQuotient(player.Landmasses[this.look.TileContinent].Guard, 4);
					this.look.HarbourDistance = (ushort)(this.look.HarbourDistance - guard);
				}
				else
				{
					this.look.HarbourDistance = (ushort)(this.look.HarbourDistance + 0x10);
				}

				if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
				{
					this.look.BestDistance = this.look.HarbourDistance;
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)town.Place.X);
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)town.Place.Y);
					this.look.Round = (ushort)player.Landmasses[this.look.TileContinent].Approach;
				}
			}

			this.look.HarbourDistance = (ushort)(this.look.Round == 5 ? 4 : 2);

			if ((short)(3 * (short)this.look.HarbourDistance) < (short)this.look.BestDistance)
			{
				return true;
			}

			ushort approach = this.look.Round;
			short horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHorizontal);
			short verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelVertical);

			if (approach != 0)
			{
				SendUnitToStrategicLocation(participantIndex, horizontalLocation, verticalLocation, 0, (short)this.look.HarbourDistance);
			}

			if (this.look.Round != 2)
			{
				SendUnitToStrategicLocation(participantIndex, this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHorizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelVertical), 2, (short)this.look.HarbourDistance);
			}

			if (this.look.Round != 1)
			{
				SendUnitToStrategicLocation(participantIndex, this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHorizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelVertical), 1, (short)this.look.HarbourDistance);
			}

			return true;
		}

		private void SurveyStack(short participantIndex, short troopIndex)
		{
			this.look.BestDirection = 0;
			this.look.StackRoles = 0;
			this.look.LandUnits = 0;

			short current = troopIndex;

			while (true)
			{
				this.look.TileOwner = (ushort)this.troops.Following(participantIndex, current);

				if (this.look.TileOwner == 0xffff || (short)this.look.TileOwner == troopIndex)
				{
					break;
				}

				short member = (short)this.look.TileOwner;
				ushort status = (ushort)(UnitMemory.Address(participantIndex, member) + UnitMemory.Status);

				if ((this.Processor.PullByte(this.Processor.SegmentData.Word, status) & 8) == 0)
				{
					sbyte kind = this.troops.TroopKind(participantIndex, member);
					byte role = (byte)Tables.UnitKindWord(kind, UnitKindTable.TroopGroup);
					this.look.StackRoles = (ushort)(this.look.StackRoles | (ushort)(1 << role));

					if (Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
					{
						this.look.LandUnits = (ushort)(this.look.LandUnits + 1);
					}
				}
				else if (this.look.Approach == 5)
				{
					short fifth = (short)(this.Owner.World.Towns[this.look.Harbour].TrueExtent / 5);
					short counted = (short)this.look.BestDirection;
					this.look.BestDirection = this.Processor.IncrementWord(this.look.BestDirection);

					if (fifth < counted)
					{
						this.Processor.EmitByte(this.Processor.SegmentData.Word, status, (byte)(this.Processor.PullByte(this.Processor.SegmentData.Word, status) & 0xf7));
						this.look.LandUnits = (ushort)(this.look.LandUnits + 1);
					}
				}

				current = member;
			}
		}
	}
}
