namespace Civilization1Remaster.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private ushort? DirectSettlers(short participantIndex, short troopIndex)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if (this.look.Terrain != MapRules.OceanTerrain)
			{
				if ((short)this.look.UnitDistance >= 2)
				{
					SendUnitToStrategicLocation(participantIndex, horizontalLocation, verticalLocation, 2, 2);
				}

				ushort? improvement = ImproveSettlersTile(participantIndex, troopIndex);

				if (improvement != null)
				{
					return improvement;
				}

				if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0x40) != 0)
				{
					return 0x70;
				}

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
				{
					ushort continent = this.look.Continent;
					short wish = (short)(this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(0x1514 + (0x3a * player.ChiefVisuals))) + 1);
					short wanted = (short)MultiplyWords((ushort)wish, (ushort)this.Owner.World.Landmasses[continent].ConstructionSpotTally);
					short maximumTownsScore = (short)(player.Landmasses[continent].TownTally << 4);

					if (wanted > maximumTownsScore)
					{
						return 0;
					}
				}

				if ((short)this.look.CityDistance <= 1 && this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex == participantIndex &&
					(this.Owner.World.DiagonalMarkers & 2) != 0 && ChooseSettlersTile(participantIndex, troopIndex))
				{
					return 0;
				}

				ushort? road = BuildSettlersRoad(participantIndex);

				if (road != null)
				{
					return road;
				}
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				return 0;
			}

			this.look.Round = (ushort)(this.Owner.Runtime.Dice.DrawInteger(20) + 1);
			ushort direction = (ushort)(this.look.Round << 1);
			this.look.TileHorizontal = (ushort)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal) >> 2) +
				Tables.DirectionHorizontalAt(direction));
			this.look.TileVertical = (ushort)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) >> 2) +
				Tables.DirectionVerticalAt(direction));
			this.Processor.SegmentExtra.Word = 0x3772;

			if ((this.Processor.PullByte(this.Processor.SegmentExtra.Word,
				(ushort)((0xd * this.look.TileHorizontal) + this.look.TileVertical + 0x2438)) & 3) != 0)
			{
				return null;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal,
				(byte)((this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(direction + 0x1882)) << 2) + this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal)));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical,
				(byte)((this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(direction + 0x18e4)) << 2) + this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical)));

			short travelHorizontal = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelHorizontal);
			short travelVertical = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.TravelVertical);

			if (this.Owner.MapOperations.CheckMapLimits(travelHorizontal, travelVertical) != 0)
			{
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(travelHorizontal, travelVertical);

				if (this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)((terrain * 0x13) + 0x291)) != 0 &&
					this.Owner.MapOperations.ObtainContinentIdentifier(travelHorizontal, travelVertical) == this.look.Continent)
				{
					return 0;
				}
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);

			return null;
		}

		private ushort SettlersTerrainColumn(short participantIndex)
		{
			return (ushort)((this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 5) != 0 ? 5 : 4) << 1);
		}

		private ushort? ImproveSettlersTile(short participantIndex, short troopIndex)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;
			ushort terrain = this.look.Terrain;

			if (Tables.TerrainImprovementWord(terrain, SettlersTerrainColumn(participantIndex)) == 0 &&
				this.look.Approach != 5)
			{
				return null;
			}

			short distance = (short)this.look.CityDistance;
			GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

			if (distance <= 0 || distance > 2 || town.ParticipantIndex != participantIndex)
			{
				return null;
			}

			if ((sbyte)town.TrueExtent < 3 && terrain == 4 && this.Owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) == 0)
			{
				return null;
			}

			if ((this.Owner.World.DiagonalMarkers & 2) == 0)
			{
				return null;
			}

			ushort cost = this.Owner.TurnLogic.ObtainTileMoveCost(horizontalLocation, verticalLocation);

			if (cost == 1 || cost == 2)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
				return (ushort)(cost == 1 ? 0x69 : 0x6d);
			}

			this.look.Round = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);
			byte improvements = (byte)this.look.Round;

			if ((improvements & 6) != 0 && (improvements & 8) == 0 && (short)terrain <= 2)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
				return 0x72;
			}

			if ((improvements & 0x18) == 0x18 || this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x31) == 0)
			{
				return null;
			}

			short worked = (short)this.Owner.CityOperations.MayCityWorkTile(horizontalLocation, verticalLocation, 1);

			if ((short)((improvements & 8) != 0 ? 1 : 2) > worked)
			{
				return null;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			return 0x72;
		}

		private bool ChooseSettlersTile(short participantIndex, short troopIndex)
		{
			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				this.look.Round = (ushort)(((this.look.Direction + this.Owner.World.RoundTally) & 7) + 1);
				ushort direction = (ushort)(this.look.Round << 1);
				this.look.TileHorizontal = (ushort)(Tables.DirectionHorizontalAt(direction) + this.look.Horizontal);
				this.look.TileVertical = (ushort)(Tables.DirectionVerticalAt(direction) + this.look.Vertical);

				short horizontalLocation = (short)this.look.TileHorizontal;
				short verticalLocation = (short)this.look.TileVertical;

				this.look.TileOwner = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

				if (this.look.TileOwner != 0xffff &&
					((ushort)participantIndex != Search.FoundPlayer ||
						this.Owner.UnitOperations.CountUnitsOfTypeInStack(participantIndex, (short)this.look.TileOwner, 0) != 0))
				{
					continue;
				}

				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

				if (Tables.TerrainImprovementWord(terrain, SettlersTerrainColumn(participantIndex)) == 0 ||
					this.Owner.TurnLogic.ObtainTileMoveCost(horizontalLocation, verticalLocation) == 0)
				{
					continue;
				}

				GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

				if ((sbyte)town.TrueExtent < 3 && this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == 4 &&
					this.Owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) == 0)
				{
					continue;
				}

				if ((short)(ushort)this.Owner.Helpers.MergeCoordinates((short)(horizontalLocation - town.Place.X), (short)(verticalLocation - town.Place.Y)) > 2)
				{
					continue;
				}

				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)this.look.TileHorizontal);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)this.look.TileVertical);

				return true;
			}

			return false;
		}

		private ushort? BuildSettlersRoad(short participantIndex)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 8) != 0)
			{
				return null;
			}

			if (this.look.Terrain == 0xb && this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x13) == 0)
			{
				return null;
			}

			ushort city = this.look.NearestCity;

			if ((city < 128 ? (ushort)this.Owner.World.Towns[city].ParticipantIndex : (ushort)0xffff) != (ushort)participantIndex)
			{
				return null;
			}

			if ((short)this.look.CityDistance <= 2 && (this.look.Terrain == 1 || this.look.Terrain == 2))
			{
				return 0x72;
			}

			this.look.Round = 0;
			this.look.LandUnits = 0;
			this.look.Score = 0;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				short stepHorizontal = (short)Tables.DirectionHorizontalAt(direction);
				short stepVertical = (short)Tables.DirectionVerticalAt(direction);

				if ((this.Owner.MapOperations.ObtainTileImprovements(stepHorizontal + horizontalLocation, stepVertical + verticalLocation) & 8) == 0)
				{
					continue;
				}

				this.look.Score = (ushort)(this.look.Score | (ushort)(1 << (byte)((byte)this.look.Direction - 1)));
				this.look.LandUnits = this.Processor.IncrementWord(this.look.LandUnits);

				if ((this.Owner.MapOperations.ObtainTileImprovements((stepHorizontal * 2) + horizontalLocation, (stepVertical * 2) + verticalLocation) & 8) != 0)
				{
					this.look.Round = 1;
				}
			}

			this.look.Score = (ushort)(this.look.Score & (ushort)((short)this.look.Score >> 4));

			if ((short)this.look.LandUnits < 4)
			{
				if (this.look.Score != 0)
				{
					return 0x72;
				}

				if (this.look.CityDistance == 1 && (short)this.look.Terrain <= 2)
				{
					return 0x72;
				}
			}

			if (this.look.LandUnits == 1 && this.look.Round != 0 &&
				Tables.TerrainField(this.look.Terrain, TerrainTable.MoveCost) == 1)
			{
				return 0x72;
			}

			return null;
		}
	}
}
