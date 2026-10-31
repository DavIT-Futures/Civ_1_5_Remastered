using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The AI settlers: cities, terrain improvements, roads</summary>
	partial class ComputerPlayer
	{
		/// <summary>
		/// Settlers (role 0) at the place 0x30 / 0x36 on the terrain 0x42: near their city (0x34, distance 0x2a) they irrigate
		/// (0x69 / 0x72 by the move cost) or mine (0x6d) a tile worth it, build a fortress (0x70) on a tile marked for it, go to
		/// a better free tile around a small city, build roads (0x72) on a terrain worth it, else wander to a free land
		/// tile on their continent in the pattern 0x2438 (0). Null: it goes on (L2520), DI and ES as the original leaves them.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? DirectSettlers(short participantIdx, short troopIdx)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if (this.look.Terrain != MapRules.OceanTerrain)
			{
				if ((short)this.look.UnitDistance >= 2)
					SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 2, 2);

				ushort? improvement = ImproveSettlersTile(participantIdx, troopIdx);

				if (improvement != null)
					return improvement;

				// A tile marked for a fortress
				if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x40) != 0)
					return 0x70;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
				{
					// The leader's wish for cities (word 0x1514 by the leader) times the places on the continent, against 16 per city
					ushort continent = this.look.Continent;
					short wish = (short)(this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(0x1514 + 0x3a * player.ChiefVisuals)) + 1);
					short wanted = (short)MultiplyWords((ushort)wish, (ushort)this.Owner.World.Landmasses[continent].ConstructionSpotTally);
					short maxTownsScore = (short)(player.Landmasses[continent].TownTally << 4);

					if (wanted > maxTownsScore)
						return 0;
				}

				if ((short)this.look.CityDistance <= 1 && this.Owner.World.Towns[this.look.NearestCity].ParticipantIdx == participantIdx &&
					(this.Owner.World.DiagMarkers & 2) != 0 && ChooseSettlersTile(participantIdx, troopIdx))
				{
					return 0;
				}

				ushort? road = BuildSettlersRoad(participantIdx);

				if (road != null)
					return road;
			}

			// Wandering: a free land tile on the continent, about four tiles away
			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
				return 0;

			this.look.Round = (ushort)(this.Owner.Libc.Dice.DrawInt(20) + 1);
			ushort direction = (ushort)(this.look.Round << 1);
			this.look.TileHoriz = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) >> 2) +
				Tables.DirectionHorizAt(direction));
			this.look.TileVert = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) >> 2) +
				Tables.DirectionVertAt(direction));
			this.Processor.SegExtra.Value16 = 0x3772;

			if ((this.Processor.PullUInt8(this.Processor.SegExtra.Value16,
				(ushort)(0xd * this.look.TileHoriz + this.look.TileVert + 0x2438)) & 3) != 0)
			{
				return null;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz,
				(byte)((this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(direction + 0x1882)) << 2) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert,
				(byte)((this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(direction + 0x18e4)) << 2) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert)));

			short travelHoriz = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz);
			short travelVert = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert);

			if (this.Owner.MapOps.CheckMapLimits(travelHoriz, travelVert) != 0)
			{
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(travelHoriz, travelVert);

				if (this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(terrain * 0x13 + 0x291)) != 0 &&
					this.Owner.MapOps.ObtainContinentID(travelHoriz, travelVert) == this.look.Continent)
				{
					return 0;
				}
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			return null;
		}

		/// <summary>
		/// The column of the terrain improvement table for the improvements the player may make: 5 with the
		/// technology 5, else 4
		/// </summary>
		private ushort SettlersTerrainColumn(short participantIdx)
		{
			return (ushort)((this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 5) != 0 ? 5 : 4) << 1);
		}

		/// <summary>
		/// The tile of settlers next to (0x2a 1 - 2) their own city: when the terrain is worth improving (or the continent is
		/// attacked) and the city is big or the tile is not a plain grassland, with the AI improving (the marker bit 2): by the
		/// move cost 1 irrigation (0x69), 2 a mine (0x6d), else irrigation or roads (0x72) on a tile without them, a farmland
		/// worth it with refrigeration (technology 0x31). Null: nothing to do (L213b).
		/// </summary>
		private ushort? ImproveSettlersTile(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;
			ushort terrain = this.look.Terrain;

			if (Tables.TerrainImprovementWord(terrain, SettlersTerrainColumn(participantIdx)) == 0 &&
				this.look.Approach != 5)
			{
				return null;
			}

			short distance = (short)this.look.CityDistance;
			GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

			if (distance <= 0 || distance > 2 || town.ParticipantIdx != participantIdx)
				return null;

			if ((sbyte)town.TrueExtent < 3 && terrain == 4 && this.Owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) == 0)
				return null;

			if ((this.Owner.World.DiagMarkers & 2) == 0)
				return null;

			ushort cost = this.Owner.TurnLogic.ObtainTileMoveCost(horizLoc, vertLoc);

			if (cost == 1 || cost == 2)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
				return (ushort)(cost == 1 ? 0x69 : 0x6d);
			}

			this.look.Round = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);
			byte improvements = (byte)this.look.Round;

			if ((improvements & 6) != 0 && (improvements & 8) == 0 && (short)terrain <= 2)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
				return 0x72;
			}

			if ((improvements & 0x18) == 0x18 || this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31) == 0)
				return null;

			short worked = (short)this.Owner.CityOps.MayCityWorkTile(horizLoc, vertLoc, 1);

			if ((short)((improvements & 8) != 0 ? 1 : 2) > worked)
				return null;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			return 0x72;
		}

		/// <summary>
		/// Settlers in their city (or next to it): the first free tile around (0x40 the try, 0x3a the direction from the round)
		/// that is worth improving, may be entered, is not a plain grassland of a small city and lies within 2 tiles of the city
		/// becomes their destination. False: none (L230b).
		/// </summary>
		private bool ChooseSettlersTile(short participantIdx, short troopIdx)
		{
			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				this.look.Round = (ushort)(((this.look.Direction + this.Owner.World.RoundTally) & 7) + 1);
				ushort direction = (ushort)(this.look.Round << 1);
				this.look.TileHoriz = (ushort)(Tables.DirectionHorizAt(direction) + this.look.Horiz);
				this.look.TileVert = (ushort)(Tables.DirectionVertAt(direction) + this.look.Vert);

				short horizLoc = (short)this.look.TileHoriz;
				short vertLoc = (short)this.look.TileVert;

				this.look.TileOwner = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

				// Units of another player, or own settlers there
				if (this.look.TileOwner != 0xffff &&
					((ushort)participantIdx != Search.FoundPlayer ||
						this.Owner.UnitOps.CountUnitsOfTypeInStack(participantIdx, (short)this.look.TileOwner, 0) != 0))
				{
					continue;
				}

				ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				if (Tables.TerrainImprovementWord(terrain, SettlersTerrainColumn(participantIdx)) == 0 ||
					this.Owner.TurnLogic.ObtainTileMoveCost(horizLoc, vertLoc) == 0)
				{
					continue;
				}

				GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

				if ((sbyte)town.TrueExtent < 3 && this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 4 &&
					this.Owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) == 0)
				{
					continue;
				}

				if ((short)(ushort)this.Owner.Helpers.MergeCoordinates((short)(horizLoc - town.Place.X), (short)(vertLoc - town.Place.Y)) > 2)
					continue;

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)this.look.TileHoriz);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)this.look.TileVert);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Settlers on a tile without a road (not a jungle before the technology 0x13) near their own city: roads (0x72) on
		/// irrigable terrain close to the city, or where they join roads around (0x10 the bits of the roads in the directions,
		/// 0x54 their number, 0x3a a road two tiles away). Null: nothing to build (L241e).
		/// </summary>
		private ushort? BuildSettlersRoad(short participantIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 8) != 0)
				return null;

			if (this.look.Terrain == 0xb && this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13) == 0)
				return null;

			ushort city = this.look.NearestCity;

			if ((city < 128 ? (ushort)this.Owner.World.Towns[city].ParticipantIdx : (ushort)0xffff) != (ushort)participantIdx)
				return null;

			if ((short)this.look.CityDistance <= 2 && (this.look.Terrain == 1 || this.look.Terrain == 2))
				return 0x72;

			this.look.Round = 0;
			this.look.LandUnits = 0;
			this.look.Score = 0;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				short stepHoriz = (short)Tables.DirectionHorizAt(direction);
				short stepVert = (short)Tables.DirectionVertAt(direction);

				if ((this.Owner.MapOps.ObtainTileImprovements(stepHoriz + horizLoc, stepVert + vertLoc) & 8) == 0)
					continue;

				this.look.Score = (ushort)(this.look.Score | (ushort)(1 << (byte)((byte)this.look.Direction - 1)));
				this.look.LandUnits = this.Processor.IncrementWord(this.look.LandUnits);

				if ((this.Owner.MapOps.ObtainTileImprovements(stepHoriz * 2 + horizLoc, stepVert * 2 + vertLoc) & 8) != 0)
					this.look.Round = 1;
			}

			// The roads in opposite directions
			this.look.Score = (ushort)(this.look.Score & (ushort)((short)this.look.Score >> 4));

			if ((short)this.look.LandUnits < 4)
			{
				if (this.look.Score != 0)
					return 0x72;

				if (this.look.CityDistance == 1 && (short)this.look.Terrain <= 2)
					return 0x72;
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
