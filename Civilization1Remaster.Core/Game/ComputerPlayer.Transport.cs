using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The AI transports and the staging cities</summary>
	partial class ComputerPlayer
	{
		/// <summary>
		/// A unit without a destination gets one. With attackers on board (or no settlers), a transport that is not a trireme
		/// aims at the nearest enemy city (every eighth one from the round, not the cities of a partner under peace and, while the
		/// human player is strong, only the cities of the human player); else, with the ship not a trireme, at the nearest own
		/// city at least 8 away on another continent (not the one in 0x1a, unless at sea) and not under attack, a city where its
		/// load is not wanted counting twice as far; else at a random land tile without the cities of the player, 2 - 23 tiles
		/// away. Only for a land unit outside (0x4c zero) or a transport (role 5). The locals of ShiftAIUnit are used as in the
		/// original.
		/// </summary>
		private void ChooseTransportDestination(short participantIdx, short troopIdx)
		{
			if (this.look.MayStay != 0 && this.look.Role != 5)
				return;

			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			byte roles = (byte)this.look.StackRoles;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff && ((roles & 2) != 0 || (roles & 1) == 0) &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10)
			{
				// The nearest enemy city, every eighth one
				this.look.BestDistance = 0x3e7;

				for (this.look.Direction = (ushort)(this.Owner.World.RoundTally & 7); (short)this.look.Direction < 0x80;
					this.look.Direction = (ushort)(this.look.Direction + 8))
				{
					GameModel.State.City town = this.Owner.World.Towns[this.look.Direction];
					this.look.Round = (ushort)town.ParticipantIdx;

					if (town.ConditionMarker == 0xff || this.look.Round == (ushort)participantIdx ||
						(player.Relations[this.look.Round] & 0x102) == 2)
					{
						continue;
					}

					if (this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Standing >= 7 &&
						this.look.Round != (ushort)this.Owner.World.PersonParticipantIdx)
					{
						continue;
					}

					this.look.HarbourDistance = (ushort)this.Owner.Helpers.ObtainDistance((short)this.look.Horiz, town.Place.X, (short)this.look.Vert, town.Place.Y);

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);
					}
				}
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10)
			{
				// The nearest own city at least 8 away on another continent, not under attack
				this.look.BestDistance = 0x3e7;
				this.look.Score = 0xffff;

				if (this.Owner.MapOps.ObtainTerrainType((short)this.look.Horiz, (short)this.look.Vert) != MapRules.OceanTerrain)
					this.look.Score = this.look.Continent;

				for (this.look.Direction = 0; (short)this.look.Direction < 0x80;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					GameModel.State.City town = this.Owner.World.Towns[this.look.Direction];
					this.look.Round = (ushort)town.ParticipantIdx;

					if (town.ConditionMarker == 0xff || this.look.Round != (ushort)participantIdx)
						continue;

					this.look.HarbourDistance = (ushort)this.Owner.Helpers.ObtainDistance((short)this.look.Horiz, town.Place.X, (short)this.look.Vert, town.Place.Y);

					if ((short)this.look.HarbourDistance < 8)
						continue;

					this.look.TileContinent = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

					if (this.look.TileContinent == this.look.Score)
						continue;

					var continent = player.Landmasses[this.look.TileContinent];

					if (continent.Approach == 5)
						continue;

					if (((byte)(this.Processor.ShiftLeftByte(1, (byte)continent.Approach) & (byte)this.look.StackRoles) & 7) == 0)
						this.look.HarbourDistance = (ushort)(this.look.HarbourDistance << 1);

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);
					}
				}
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
				return;

			// A random land tile without cities of the player, farther with every try
			for (this.look.Direction = 2; (short)this.look.Direction < 0x18;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				this.look.Round = (ushort)this.Owner.Libc.Dice.DrawInt(9);
				ushort round = (ushort)(this.look.Round << 1);

				this.look.TileHoriz = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizAt(round) *
					(short)this.look.Direction + this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz));

				// (the registers of the multiplication as in the original)
				ushort step = MultiplyWords(Tables.DirectionVertAt(round),
					this.look.Direction);
				this.look.TileVert = (ushort)(step + this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));

				short horizLoc = (short)this.look.TileHoriz;
				short vertLoc = (short)this.look.TileVert;

				if (vertLoc <= 2 || vertLoc >= 0x2f || this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if (player.Landmasses[this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc)].TownTally == 0)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)this.look.TileHoriz);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)this.look.TileVert);
					return;
				}
			}
		}

		/// <summary>
		/// A transport (role 5) looks at the land tiles around (0x40 the direction, 0x26 / 0x2e the place, 0x20 the owner):
		/// free or own tiles with fewer than two own land units, not a human player's improved tile under peace, on a continent
		/// (0x4a) where its load is wanted (0x3c: a weak continent, the role of the load fits the approach, or the destination is
		/// there) with room for cities and away from the poles. With moves left it tries to land (the answer of MayAIEnterTile);
		/// else it unloads (0x75, a strategic location of role 3 on a defended continent). Without such a tile, every fourth turn it
		/// forgets its destination; without moves left, a point of the sea halfway to the destination gets a strategic location.
		/// Null: the unit goes on (L1c37). The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? PlanTransportLanding(short participantIdx, short troopIdx)
		{
			if (this.look.Role != 5)
				return null;

			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];
			short person = this.Owner.World.PersonParticipantIdx;
			bool forgets = false;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;

				this.look.TileHoriz = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHoriz(direction) +
					(short)this.look.Horiz);
				this.look.TileVert = (ushort)(Tables.DirectionVert(direction) + this.look.Vert);

				short horizLoc = (short)this.look.TileHoriz;
				short vertLoc = (short)this.look.TileVert;

				this.look.TileOwner = this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if (this.Owner.MapOps.CheckMapLimits(horizLoc, vertLoc) == 0)
					continue;

				ushort owner = this.look.TileOwner;

				if (owner != 0xffff && owner != (ushort)participantIdx)
					continue;

				if (owner == (ushort)participantIdx &&
					(short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, (short)this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc), 2) >= 2)
				{
					continue;
				}

				if ((player.Relations[person] & 2) != 0 && this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) == (ushort)person &&
					(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0xf) != 0)
				{
					continue;
				}

				this.look.TileContinent = this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
				this.look.LoadWanted = 0;

				var continent = player.Landmasses[this.look.TileContinent];

				if ((continent.Assault < 16 && continent.Approach != 5) ||
					((byte)(this.Processor.ShiftLeftByte(1, (byte)continent.Approach) & (byte)this.look.StackRoles) & 6) != 0)
				{
					this.look.LoadWanted = 1;
				}

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff &&
					this.Owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert)) == this.look.TileContinent)
				{
					this.look.LoadWanted = 1;
				}

				if (this.look.LoadWanted == 0 ||
					this.Owner.World.Landmasses[this.look.TileContinent].ConstructionSpotTally < 5 ||
					(short)this.look.TileVert <= 1 || (short)this.look.TileVert >= 0x30)
				{
					forgets = true;
					break;
				}

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 3)
				{
					this.look.Round = MayAIEnterTile(participantIdx, this.look.Horiz, this.look.Vert);

					if ((short)this.look.Round > 0)
						return this.look.Round;
				}

				// Unloading: a strategic location of role 3 on a defended continent, the transport stops and forgets its destination
				if (player.Landmasses[this.look.TileContinent].Approach == 1)
					SendUnitToStrategicLocation(participantIdx, (short)this.look.Horiz, (short)this.look.Vert, 3, 5);

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

				return 0x75;
			}

			if (forgets && (this.Owner.World.RoundTally & 3) == 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 3)
				return null;

			this.look.TileHoriz = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz)) >> 1);
			this.look.TileVert = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert)) >> 1);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
				return null;

			if (this.Owner.MapOps.ObtainTerrainType((short)this.look.TileHoriz, (short)this.look.TileVert) == MapRules.OceanTerrain)
				SendUnitToStrategicLocation(participantIdx, (short)this.look.TileHoriz, (short)this.look.TileVert, 3, 3);

			return null;
		}

		/// <summary>
		/// A transport (role 5) with fewer land units on board than it may carry (1 near a city, else 3) or with only settlers:
		/// the nearest coastal city of the player (0xc, distance 0x12, best 0x14) on a continent with attack strength 16 or more
		/// (an empty city +16 with attackers on board; an attacked continent -guard / 4, else +16) is its destination (approach in
		/// 0x3a); the strategic locations there are marked by the approach (0: role 0 / 2 / 1; 2: 0 / 1; 5: 0 / 2 / 1, priority
		/// 4, else 2). False: not such a transport (L198e). The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private bool ChooseStagingCity(short participantIdx, short troopIdx)
		{
			short capacity = (short)(this.look.CityDistance != 0 ? 1 : 3);

			if ((capacity <= (short)this.look.LandUnits && this.look.StackRoles != 1) || this.look.Role != 5)
				return false;

			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];

			this.look.BestDistance = 0x3e7;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.Harbour];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx || (town.ConditionMarker & 2) == 0)
					continue;

				this.look.HarbourDistance = (ushort)this.Owner.Helpers.ObtainDistance((short)this.look.Horiz, town.Place.X, (short)this.look.Vert, town.Place.Y);
				this.look.TileContinent = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

				if (player.Landmasses[this.look.TileContinent].Assault < 16)
					continue;

				this.look.TileUnit = this.Owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y);

				if (this.look.TileUnit != 0xffff)
				{
					short attackers = (short)this.Owner.UnitOps.CountUnitsWithFlagInStack(participantIdx, (short)this.look.TileUnit, 5);

					// (sbb / neg: 1 for the distance 0)
					if ((short)(this.look.HarbourDistance == 0 ? 1 : 0) < attackers)
						this.look.HarbourDistance = (ushort)(this.look.HarbourDistance + 0x10);
				}

				if (player.Landmasses[this.look.TileContinent].Approach == 5)
				{
					short guard = CPU.SignedQuotient(player.Landmasses[this.look.TileContinent].Guard, 4);
					this.look.HarbourDistance = (ushort)(this.look.HarbourDistance - guard);
				}
				else
				{
					this.look.HarbourDistance = (ushort)(this.look.HarbourDistance + 0x10);
				}

				if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
				{
					this.look.BestDistance = this.look.HarbourDistance;
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);
					this.look.Round = (ushort)player.Landmasses[this.look.TileContinent].Approach;
				}
			}

			this.look.HarbourDistance = (ushort)(this.look.Round == 5 ? 4 : 2);

			if ((short)(3 * (short)this.look.HarbourDistance) < (short)this.look.BestDistance)
				return true;

			ushort approach = this.look.Round;
			short horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz);
			short vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert);

			if (approach != 0)
				SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 0, (short)this.look.HarbourDistance);

			if (this.look.Round != 2)
			{
				SendUnitToStrategicLocation(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert), 2, (short)this.look.HarbourDistance);
			}

			if (this.look.Round != 1)
			{
				SendUnitToStrategicLocation(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert), 1, (short)this.look.HarbourDistance);
			}

			return true;
		}

		/// <summary>
		/// The other units of the stack (the ring of FollowingInStack): the roles of the ones that are not fortified (bits
		/// 0x1e) and the land units among them (0x54); on an attacked continent (approach 5) the fortified ones beyond a fifth of
		/// the size of the city 0xc leave their fortification and count too (0x2c counts them). The local 0xc is the one ShiftAIUnit
		/// may not have set (it reads what is left on the stack), as in the original.
		/// </summary>
		private void SurveyStack(short participantIdx, short troopIdx)
		{
			this.look.BestDirection = 0;
			this.look.StackRoles = 0;
			this.look.LandUnits = 0;

			short current = troopIdx;

			while (true)
			{
				this.look.TileOwner = (ushort)this.troops.Following(participantIdx, current);

				if (this.look.TileOwner == 0xffff || (short)this.look.TileOwner == troopIdx)
					break;

				short member = (short)this.look.TileOwner;
				ushort status = (ushort)(UnitMemory.Address(participantIdx, member) + UnitMemory.Status);

				if ((this.Processor.PullUInt8(this.Processor.SegData.Value16, status) & 8) == 0)
				{
					sbyte kind = this.troops.TroopKind(participantIdx, member);
					byte role = (byte)Tables.UnitKindWord(kind, UnitKindTable.TroopGroup);
					this.look.StackRoles = (ushort)(this.look.StackRoles | (ushort)(1 << role));

					if (Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
						this.look.LandUnits = (ushort)(this.look.LandUnits + 1);
				}
				else if (this.look.Approach == 5)
				{
					short fifth = (short)(this.Owner.World.Towns[this.look.Harbour].TrueExtent / 5);
					short counted = (short)this.look.BestDirection;
					this.look.BestDirection = this.Processor.IncrementWord(this.look.BestDirection);

					if (fifth < counted)
					{
						this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) & 0xf7));
						this.look.LandUnits = (ushort)(this.look.LandUnits + 1);
					}
				}

				current = member;
			}
		}
	}
}
