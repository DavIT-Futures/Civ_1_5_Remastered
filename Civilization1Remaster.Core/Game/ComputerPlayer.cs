using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x25fb.
	/// </summary>
	public partial class ComputerPlayer : GameComponent, IComputerPlayer
	{
		/// <summary>The 20 x 13 blocks of 4 x 4 tiles of the map in the segment 0x3772: bit 0 a city, bits 1 - 7 the unit roles</summary>
		private const ushort BlockSegment = 0x3772;

		private const ushort BlockOrigin = 0x2438;

		private readonly UnitMemory troops;

		private UnitLook look = new UnitLook();

		public ComputerPlayer(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <inheritdoc/>
		public void OperateAITurn(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"OperateAITurn({participantIdx})");

			CountUnits(participantIdx);
			ushort sentBase = CountCities(participantIdx);
			CountUsedBlocks(participantIdx);
			ChooseContinentApproaches(participantIdx);

			if (participantIdx != 0 && !this.Owner.World.IsHuman(participantIdx))
			{
				Player player = this.Owner.World.Participants[participantIdx];

				if (player.ColonistTally == 0 && player.TownTally == 0 && participantIdx != this.Owner.World.PersonParticipantIdx)
					this.Owner.NewGameWizard.DestroyCivilization(participantIdx, 0);

				if ((((byte)participantIdx + (byte)this.Owner.World.RoundTally) & 7) == 0)
					ChooseGovernment(participantIdx);

				DirectUnits(participantIdx, sentBase);
			}
		}

		private static ushort Block(int horizLoc, int vertLoc)
		{
			return (ushort)(BlockOrigin + (horizLoc >> 2) * 0xd + (vertLoc >> 2));
		}

		/// <summary>The strategic locations of a player (32): the role of the unit wanted there, the place and the priority</summary>
		private static ushort LocationRole(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationRoleTableAddress + (participantIdx << 5) + locationIdx);

		private static ushort LocationHoriz(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationHorizTableAddress + (participantIdx << 5) + locationIdx);

		private static ushort LocationVert(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationVertTableAddress + (participantIdx << 5) + locationIdx);

		private static ushort LocationPriority(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationPriorityTableAddress + (participantIdx << 5) + locationIdx);

		/// <summary>
		/// Strength of the units of the player by continent, the units of the player (without the nuclear weapons), the
		/// settlers, the unit roles in the blocks; the mark 0x10 of a unit is set when it can't move (another player's unit
		/// next to it, or a land unit on the ocean)
		/// </summary>
		private void CountUnits(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			for (ushort continentIdx = 0; continentIdx < 0x10; continentIdx++)
			{
				player.Landmasses[continentIdx].TownTally = 0;
				player.Landmasses[continentIdx].Guard = 0;
				player.Landmasses[continentIdx].Assault = 0;
			}

			for (ushort blockHoriz = 0; blockHoriz < 0x14; blockHoriz++)
			{
				for (ushort blockVert = 0; blockVert < 0xd; blockVert++)
				{
					this.Processor.EmitUInt8(BlockSegment, (ushort)(BlockOrigin + blockHoriz * 0xd + blockVert), 0);
				}
			}

			player.MartialMight = 0;
			player.ColonistTally = 0;
			player.TroopTally = 0;

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

				if (kind != 0xff && kind != 0x19)
				{
					if (kind == 0)
						player.ColonistTally++;
					else
						player.TroopTally++;

					CountUnit(participantIdx, troopIdx);
				}
			}
		}

		private void CountUnit(short participantIdx, short troopIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			player.MartialMight += (short)(UnitKindWord(kind, UnitKindTable.AssaultPower) +
				UnitKindWord(kind, UnitKindTable.GuardPower));

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & 0xef));

			if (this.Owner.UnitOps.CheckTileOccupation(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) != 0)
			{
				status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			}

			if (this.Owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) != MapRules.OceanTerrain)
			{
				ushort continentIdx = this.Owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				kind = this.troops.TroopKind(participantIdx, troopIdx);

				player.Landmasses[continentIdx].Assault += (short)UnitKindWord(kind, UnitKindTable.GuardPower);
				player.Landmasses[continentIdx].Guard += (short)UnitKindWord(kind, UnitKindTable.AssaultPower);
			}
			else if (UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				// A land unit at sea (on a ship)
				status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			}

			// The role of the unit (0 - 6) as a bit of its block
			byte role = (byte)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup);
			byte roleBit = (byte)(2 << role);
			ushort block = Block(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
			this.Processor.EmitUInt8(BlockSegment, block, (byte)(this.Processor.PullUInt8(BlockSegment, block) | roleBit));
		}

		/// <summary>
		/// The cities of the player by continent, their total size and the units in production; the units known to be in
		/// the cities of the player; the cities, own and others', where a unit is sent (strategic locations)
		/// </summary>
		private ushort CountCities(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			// The base of the weight of a strategic location: an eighth of the units, 3 - 99
			ushort sentBase = (ushort)this.Owner.Helpers.ClampValue(player.TroopTally / 8, 3, 99);
			player.GrandTownExtent = 0;
			player.TownTally = 0;

			for (int kindIdx = 0; kindIdx < 0x1c; kindIdx++)
			{
				player.TroopsInsideMaking[kindIdx] = 0;
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff)
				{
					if (town.ParticipantIdx == participantIdx)
					{
						CountOwnCity(participantIdx, townIdx);
					}
					else if (MayAimAtCity(participantIdx, townIdx))
					{
						// A city of another player: a target (3 with city walls, else 5)
						SendUnitToStrategicLocation(participantIdx, (short)town.Place.X, (short)town.Place.Y, 1,
							(short)(town.Has(CityStructure.CityWalls) ? 3 : 5));
					}

					// A city without a unit and without known units: one unit to defend it (own) or to take it
					if (this.Owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y) == 0xffff && (byte)town.Unfamiliar[0] == 0xff)
					{
						SendUnitToStrategicLocation(participantIdx, (short)town.Place.X, (short)town.Place.Y, 2,
							(short)(town.ParticipantIdx == participantIdx ? 4 : 2));
					}
				}
			}

			return sentBase;
		}

		private void CountOwnCity(short participantIdx, short townIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			City town = this.Owner.World.Towns[townIdx];

			player.TownTally++;
			player.Landmasses[this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y)].TownTally++;
			player.GrandTownExtent += town.TrueExtent;

			player.StartMaking(town.PresentMakingIdx);

			ushort block = Block(town.Place.X, town.Place.Y);
			this.Processor.EmitUInt8(BlockSegment, block, (byte)(this.Processor.PullUInt8(BlockSegment, block) | 1));

			// The (up to two) units known to be in the city: their strength (a translation slip keeps only the last one
			// for the whole player and the attack of the continent; the defence adds up)
			for (ushort unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
			{
				if ((byte)town.Unfamiliar[unfamiliarIdx] != 0xff)
				{
					sbyte kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);

					player.MartialMight = (short)(UnitKindWord(kind, UnitKindTable.AssaultPower) +
						UnitKindWord(kind, UnitKindTable.GuardPower));

					ushort continentIdx = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

					kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);

					player.Landmasses[continentIdx].Assault = (short)UnitKindWord(kind, UnitKindTable.GuardPower);
					player.Landmasses[continentIdx].Guard += (short)UnitKindWord(kind, UnitKindTable.AssaultPower);
				}
			}
		}

		/// <summary>
		/// A city of another player is a target when the game is far enough (turn * level > 200), the player has seen it or it
		/// belongs to someone else than the human player, there is no peace (without an alliance) with its owner, and in 3 turns of 4
		/// </summary>
		private bool MayAimAtCity(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if ((short)(this.Owner.World.RoundTally * this.Owner.World.HardnessTier) <= 200 &&
				((sbyte)this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << participantIdx)) == 0 &&
				this.Owner.World.PersonParticipantIdx == town.ParticipantIdx)
			{
				return false;
			}

			if ((this.Owner.World.Participants[participantIdx].Relations[town.ParticipantIdx] & 0x102) == 2)
				return false;

			return (((byte)townIdx + (byte)this.Owner.World.RoundTally) & 3) != 0;
		}

		/// <summary>The blocks of the map with a city or a unit of the player</summary>
		private void CountUsedBlocks(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			player.SoilTally = 0;

			for (ushort blockHoriz = 0; blockHoriz < 0x14; blockHoriz++)
			{
				for (ushort blockVert = 0; blockVert < 0xd; blockVert++)
				{
					if (this.Processor.PullUInt8(BlockSegment, (ushort)(BlockOrigin + blockHoriz * 0xd + blockVert)) != 0)
						player.SoilTally++;
				}
			}
		}

		/// <summary>
		/// The approach of the player on every continent 1 - 14 from the strength of the others there; the cities change
		/// their production when it changes; 0x2a6e: the players that attack a continent where the human player has more than
		/// one city; 0x6ed4: the player is at war with someone
		/// </summary>
		private void ChooseContinentApproaches(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			player.Landmasses[0].Approach = 0;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)((participantIdx << 5) + 0xe6b6), 0);
			HumanContinentAttackers = (ushort)(HumanContinentAttackers & ~(1 << participantIdx));

			PlayerAtWar = 0;

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if ((player.Relations[otherIdx] & 3) == 1)
					PlayerAtWar = 1;
			}

			for (ushort continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				ChooseContinentApproach(participantIdx, continentIdx);
			}
		}

		private void ChooseContinentApproach(short participantIdx, ushort continentIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			PlayerContinent own = player.Landmasses[continentIdx];

			short threats = 0;
			short stronger = 0;
			ushort presence = 0;
			short towns = 0;
			short strength = 0;
			short approach = own.Approach;

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				PlayerContinent other = this.Owner.World.Participants[otherIdx].Landmasses[continentIdx];

				// An enemy (war, or 0x100) whose units there are not much weaker than the player's defence, or who has cities there
				if (other.Assault != 0 &&
					((short)(own.Guard / 4) < other.Guard || other.TownTally != 0) &&
					((player.Relations[otherIdx] & 3) == 1 || (player.Relations[otherIdx] & 0x100) != 0))
				{
					if (other.Assault < own.Guard || other.Guard < own.Assault || own.TownTally == 0)
					{
						threats++;
					}
					else
					{
						stronger++;
					}
				}

				// The human player at peace with the player, stronger there than half its defence and all its attack
				if (other.Assault != 0 && otherIdx == this.Owner.World.PersonParticipantIdx && (player.Relations[otherIdx] & 2) != 0)
				{
					if ((short)(CPU.SignedQuotient(own.Guard, 2) + own.Assault) < other.Guard)
					{
						stronger++;
					}
				}

				strength += other.Assault;
				towns += other.TownTally;

				if (other.Assault != 0)
				{
					// 1: someone at peace with the player, 2: someone at war
					presence |= (ushort)((player.Relations[otherIdx] & 2) != 0 ? 1 : 2);
				}
			}

			if (this.Owner.World.Participants[0].Landmasses[continentIdx].TownTally != 0)
			{
				threats++;
			}

			// Attack (5, with the technology 7) when the player and the others are strong there compared with the
			// continent (0xdcfe) or the cities don't fit on it, else build up (0)
			bool crowded = (short)((own.Assault + strength) << 1) > (short)this.Processor.PullGlobalWord((ushort)((continentIdx << 1) + 0xdcfe)) ||
				(short)((own.TownTally + towns) * 6 + 2) > this.Owner.World.Landmasses[continentIdx].ConstructionSpotTally;

			own.Approach = (short)(crowded && this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 7) != 0 ? 5 : 0);

			if (threats != 0)
				own.Approach = 1;

			if (stronger != 0)
				own.Approach = 2;

			if (own.Assault == 0 && own.TownTally == 0 && presence != 1)
				own.Approach = 1;

			if (own.Approach != approach && !this.Owner.World.IsHuman(participantIdx))
				RefreshCityProduction(participantIdx, continentIdx);

			if (own.Assault != 0 && this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Landmasses[continentIdx].TownTally > 1)
				HumanContinentAttackers = (ushort)(HumanContinentAttackers | (1 << participantIdx));
		}

		/// <summary>Communism (4), Democracy (3, more than 10 cities), Monarchy (2), or Anarchy (1) when 0xe3c2 is set</summary>
		private void ChooseGovernment(short participantIdx)
		{
			if ((short)AnarchyPressure > 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIdx, 1);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2c) != 0 && (short)DemocracyPressure >= 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIdx, 4);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2b) != 0 && this.Owner.World.Participants[participantIdx].TownTally > 10)
			{
				this.Owner.Politics.SwitchGovernment(participantIdx, 3);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 5) != 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIdx, 2);
			}
		}

		/// <summary>
		/// Every unit that can move (not a nuclear weapon) is given up when it is obsolete (and alone in a city or outside),
		/// or sent to the best of the strategic locations of its role
		/// </summary>
		private void DirectUnits(short participantIdx, ushort sentBase)
		{
			ushort[] locationSent = new ushort[0x20];

			for (int locationIdx = 0; locationIdx < 0x20; locationIdx++)
			{
				locationSent[locationIdx] = sentBase;
			}

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

				if (kind != 0xff && (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x10) == 0 && kind != 0x19)
				{
					if (!GiveUpObsoleteUnit(participantIdx, troopIdx, out short horizLoc, out short vertLoc, out ushort continentIdx))
						SendUnitToBestLocation(participantIdx, troopIdx, horizLoc, vertLoc, continentIdx, sentBase, locationSent);
				}
			}
		}

		/// <summary>
		/// Once in 16 turns: a unit whose technology the player knows (or a militia under a government above despotism, or
		/// when the continent is attacked) is given up, unless it is next to another player's unit, alone in a city, on the
		/// ocean or on a continent where the enemy is stronger
		/// </summary>
		private bool GiveUpObsoleteUnit(short participantIdx, short troopIdx, out short horizLoc, out short vertLoc, out ushort continentIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			vertLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			continentIdx = this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc);

			if ((((byte)troopIdx + (byte)this.Owner.World.RoundTally) & 0xf) != 0)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0xc)) == 0)
			{
				if (player.RegimeKind <= 1 && player.Landmasses[continentIdx].Approach != 5)
					return false;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 1)
					return false;
			}

			if (this.Owner.UnitOps.CheckTileOccupation(participantIdx, horizLoc, vertLoc) != 0)
				return false;

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
			{
				return false;
			}

			if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain ||
				player.Landmasses[continentIdx].Approach == 2)
			{
				return false;
			}

			this.Owner.UnitOps.DestroyUnit(participantIdx, troopIdx);

			return true;
		}

		/// <summary>
		/// The nearest strategic location of the unit's role on its continent (bombers: anywhere, also the locations of role 3)
		/// weighed by its priority and the units already sent there; the unit gets it as its destination
		/// </summary>
		private void SendUnitToBestLocation(short participantIdx, short troopIdx, short horizLoc, short vertLoc, ushort continentIdx, ushort sentBase, ushort[] locationSent)
		{
			short bestDistance = 0x270f;
			short best = -1;

			for (short locationIdx = 0; locationIdx < 0x20; locationIdx++)
			{
				if (LocationFits(participantIdx, troopIdx, locationIdx, continentIdx))
				{
					short? distance = WeighLocation(participantIdx, troopIdx, locationIdx, horizLoc, vertLoc, locationSent);

					if (distance != null && MayTakeLocation(participantIdx, troopIdx, locationIdx, distance.Value, sentBase, locationSent) &&
						distance.Value < bestDistance &&
						(short)((sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationPriority(participantIdx, locationIdx)) << 2) >=
							CPU.SignedQuotient(distance.Value, (short)sentBase))
					{
						bestDistance = distance.Value;
						best = locationIdx;
					}
				}
			}

			if (best == -1)
				return;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationHoriz(participantIdx, best)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationVert(participantIdx, best)));

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & 0xf0));

			locationSent[best]++;
		}

		/// <summary>
		/// The location is used and wants the unit's role on the unit's continent, or the unit is a bomber (0xf) and the
		/// location wants a bomber or the role 3
		/// </summary>
		private bool LocationFits(short participantIdx, short troopIdx, short locationIdx, ushort continentIdx)
		{
			byte role = this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationRole(participantIdx, locationIdx));

			if (role == 0xff)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (UnitKindWord(kind, UnitKindTable.TroopGroup) == (ushort)(sbyte)role &&
				this.Owner.MapOps.ObtainContinentID(this.Processor.PullInt8(this.Processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					this.Processor.PullInt8(this.Processor.SegData.Value16, LocationVert(participantIdx, locationIdx))) == continentIdx)
			{
				return true;
			}

			byte kindByte = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kindByte != 0xf)
				return false;

			sbyte bomberRole = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationRole(participantIdx, locationIdx));

			return UnitKindWord((sbyte)kindByte, UnitKindTable.TroopGroup) == (ushort)bomberRole ||
				bomberRole == 3;
		}

		/// <summary>
		/// The weighed distance to the location: a bomber only goes to locations with priority bit 0 in every
		/// other turn (else null: the location is skipped), its distance is doubled within its range, else quadrupled; other
		/// units: the distance times the units already sent there; both divided by the priority + 1
		/// </summary>
		private short? WeighLocation(short participantIdx, short troopIdx, short locationIdx, short horizLoc, short vertLoc, ushort[] locationSent)
		{
			short weighed;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf)
			{
				if ((this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationPriority(participantIdx, locationIdx)) & 1) == 0 ||
					(((byte)locationIdx + (byte)(this.Owner.World.RoundTally >> 1)) & 1) == 0)
				{
					return null;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc,
					this.Processor.PullInt8(this.Processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					vertLoc, this.Processor.PullInt8(this.Processor.SegData.Value16, LocationVert(participantIdx, locationIdx)));

				short range = (short)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally);
				weighed = (short)((range >= distance ? 2 : 4) * distance);
			}
			else
			{
				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc,
					this.Processor.PullInt8(this.Processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					vertLoc, this.Processor.PullInt8(this.Processor.SegData.Value16, LocationVert(participantIdx, locationIdx)));

				weighed = (short)(locationSent[locationIdx] * distance);
			}

			sbyte priority = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationPriority(participantIdx, locationIdx));
			return CPU.SignedQuotient(weighed, (short)(priority + 1));
		}

		/// <summary>
		/// A unit with orders (status bits 2, 3) only takes a location of priority 2 or more, whose weight (priority times
		/// the base) is not below the distance or no unit was sent there yet, and when it is not in a city
		/// </summary>
		private bool MayTakeLocation(short participantIdx, short troopIdx, short locationIdx, short distance, ushort sentBase, ushort[] locationSent)
		{
			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc) == 0)
				return true;

			sbyte priority = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationPriority(participantIdx, locationIdx));

			if (priority < 2)
				return false;

			if ((short)(priority * (short)sentBase) < distance &&
				locationSent[locationIdx] != sentBase)
			{
				return false;
			}

			return (this.Owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) & 1) == 0;
		}
	}
}
