using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x25fb.
	/// </summary>
	public class AIPlayer : GameModule
	{
		private readonly UnitMemory troops;
		private readonly UnitLook look;

		public AIPlayer(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
			this.look = new UnitLook(owner.Processor);
		}

		/// <summary>
		/// The yearly survey of a player (every player, also the human one): counts the units and cities by continent, marks
		/// the 4 x 4 blocks of the map where they are (0x3772:0x2438), picks the approach of every continent (0 build up,
		/// 1 defend, 2 the enemy is stronger, 5 attack), and for a computer player also changes the government, gives up
		/// obsolete units and sends the units to the strategic locations (0xe2c2, a table of 32 per player)
		/// </summary>
		public void OperateAITurn(short participantIdx)
		{
			this.processor.Journal.OpenScope($"OperateAITurn({participantIdx})");

			this.processor.EnterFrame(0x68);
			this.processor.PreserveIndexRegisters();

			CountUnits(participantIdx);
			ushort sentBase = CountCities(participantIdx);
			CountUsedBlocks(participantIdx);
			ChooseContinentApproaches(participantIdx);

			if (participantIdx != 0 && !this.owner.World.IsHuman(participantIdx))
			{
				Player player = this.owner.World.Participants[participantIdx];

				if (player.ColonistTally == 0 && player.TownTally == 0 && participantIdx != this.owner.World.PersonParticipantIdx)
					this.owner.NewGameWizard.EliminateCivilization(participantIdx, 0);

				if ((((byte)participantIdx + (byte)this.owner.World.RoundTally) & 7) == 0)
					ChooseGovernment(participantIdx);

				DirectUnits(participantIdx, sentBase);
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("OperateAITurn");
		}

		/// <summary>The 20 x 13 blocks of 4 x 4 tiles of the map in the segment 0x3772: bit 0 a city, bits 1 - 7 the unit roles</summary>
		private const ushort BlockSegment = 0x3772;
		private const ushort BlockOrigin = 0x2438;

		private static ushort Block(int horizLoc, int vertLoc)
		{
			return (ushort)(BlockOrigin + (horizLoc >> 2) * 0xd + (vertLoc >> 2));
		}

		/// <summary>The strategic locations of a player (32): the role of the unit wanted there, the place and the priority</summary>
		private static ushort LocationRole(int participantIdx, int locationIdx) => (ushort)(0xe2c2 + (participantIdx << 5) + locationIdx);

		private static ushort LocationHoriz(int participantIdx, int locationIdx) => (ushort)(0xe598 + (participantIdx << 5) + locationIdx);

		private static ushort LocationVert(int participantIdx, int locationIdx) => (ushort)(0xe798 + (participantIdx << 5) + locationIdx);

		private static ushort LocationPriority(int participantIdx, int locationIdx) => (ushort)(0xb2be + (participantIdx << 5) + locationIdx);

		/// <summary>
		/// Strength of the units of the player by continent, the units of the player (without the nuclear weapons), the
		/// settlers, the unit roles in the blocks; the mark 0x10 of a unit is set when it can't move (another player's unit
		/// next to it, or a land unit on the ocean)
		/// </summary>
		private void CountUnits(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];

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
					this.processor.EmitUInt8(BlockSegment, (ushort)(BlockOrigin + blockHoriz * 0xd + blockVert), 0);
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
			Player player = this.owner.World.Participants[participantIdx];
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			player.MartialMight += (short)(UnitKindWord(kind, UnitKindTable.AssaultPower) +
				UnitKindWord(kind, UnitKindTable.GuardPower));

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & 0xef));

			if (this.owner.UnitOps.VerifyTileOccupation(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) != 0)
			{
				status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			}

			if (this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) != MapRules.OceanTerrain)
			{
				ushort continentIdx = this.owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
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
			this.processor.EmitUInt8(BlockSegment, block, (byte)(this.processor.PullUInt8(BlockSegment, block) | roleBit));
		}

		/// <summary>
		/// The cities of the player by continent, their total size and the units in production; the units known to be in
		/// the cities of the player; the cities, own and others', where a unit is sent (strategic locations)
		/// </summary>
		private ushort CountCities(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];

			// The base of the weight of a strategic location: an eighth of the units, 3 - 99
			ushort sentBase = (ushort)this.owner.Helpers.VerifyValueSpan(player.TroopTally / 8, 3, 99);
			player.GrandTownExtent = 0;
			player.TownTally = 0;

			for (int kindIdx = 0; kindIdx < 0x1c; kindIdx++)
			{
				player.TroopsInsideMaking[kindIdx] = 0;
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

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
					if (this.owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y) == 0xffff && (byte)town.Unfamiliar[0] == 0xff)
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
			Player player = this.owner.World.Participants[participantIdx];
			City town = this.owner.World.Towns[townIdx];

			player.TownTally++;
			player.Landmasses[this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y)].TownTally++;
			player.GrandTownExtent += town.TrueExtent;

			player.StartMaking(town.PresentMakingIdx);

			ushort block = Block(town.Place.X, town.Place.Y);
			this.processor.EmitUInt8(BlockSegment, block, (byte)(this.processor.PullUInt8(BlockSegment, block) | 1));

			// The (up to two) units known to be in the city: their strength (a translation slip keeps only the last one
			// for the whole player and the attack of the continent; the defence adds up)
			for (ushort unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
			{
				if ((byte)town.Unfamiliar[unfamiliarIdx] != 0xff)
				{
					sbyte kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);

					player.MartialMight = (short)(UnitKindWord(kind, UnitKindTable.AssaultPower) +
						UnitKindWord(kind, UnitKindTable.GuardPower));

					ushort continentIdx = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

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
			City town = this.owner.World.Towns[townIdx];

			if ((short)(this.owner.World.RoundTally * this.owner.World.HardnessTier) <= 200 &&
				((sbyte)this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << participantIdx)) == 0 &&
				this.owner.World.PersonParticipantIdx == town.ParticipantIdx)
			{
				return false;
			}

			if ((this.owner.World.Participants[participantIdx].Relations[town.ParticipantIdx] & 0x102) == 2)
				return false;

			return (((byte)townIdx + (byte)this.owner.World.RoundTally) & 3) != 0;
		}

		/// <summary>The blocks of the map with a city or a unit of the player</summary>
		private void CountUsedBlocks(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];
			player.SoilTally = 0;

			for (ushort blockHoriz = 0; blockHoriz < 0x14; blockHoriz++)
			{
				for (ushort blockVert = 0; blockVert < 0xd; blockVert++)
				{
					if (this.processor.PullUInt8(BlockSegment, (ushort)(BlockOrigin + blockHoriz * 0xd + blockVert)) != 0)
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
			Player player = this.owner.World.Participants[participantIdx];

			player.Landmasses[0].Approach = 0;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 5) + 0xe6b6), 0);
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
			Player player = this.owner.World.Participants[participantIdx];
			PlayerContinent own = player.Landmasses[continentIdx];

			short threats = 0;
			short stronger = 0;
			ushort presence = 0;
			short towns = 0;
			short strength = 0;
			short approach = own.Approach;

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				PlayerContinent other = this.owner.World.Participants[otherIdx].Landmasses[continentIdx];

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
				if (other.Assault != 0 && otherIdx == this.owner.World.PersonParticipantIdx && (player.Relations[otherIdx] & 2) != 0)
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

			if (this.owner.World.Participants[0].Landmasses[continentIdx].TownTally != 0)
			{
				threats++;
			}

			// Attack (5, with the technology 7) when the player and the others are strong there compared with the
			// continent (0xdcfe) or the cities don't fit on it, else build up (0)
			bool crowded = (short)((own.Assault + strength) << 1) > (short)this.processor.PullGlobalWord((ushort)((continentIdx << 1) + 0xdcfe)) ||
				(short)((own.TownTally + towns) * 6 + 2) > this.owner.World.Landmasses[continentIdx].ConstructionSpotTally;

			own.Approach = (short)(crowded && this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 7) != 0 ? 5 : 0);

			if (threats != 0)
				own.Approach = 1;

			if (stronger != 0)
				own.Approach = 2;

			if (own.Assault == 0 && own.TownTally == 0 && presence != 1)
				own.Approach = 1;

			if (own.Approach != approach && !this.owner.World.IsHuman(participantIdx))
				RefreshCityProduction(participantIdx, continentIdx);

			if (own.Assault != 0 && this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[continentIdx].TownTally > 1)
				HumanContinentAttackers = (ushort)(HumanContinentAttackers | (1 << participantIdx));
		}

		/// <summary>Communism (4), Democracy (3, more than 10 cities), Monarchy (2), or Anarchy (1) when 0xe3c2 is set</summary>
		private void ChooseGovernment(short participantIdx)
		{
			if ((short)AnarchyPressure > 0)
			{
				this.owner.Politics.SwitchGovernment(participantIdx, 1);
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2c) != 0 && (short)DemocracyPressure >= 0)
			{
				this.owner.Politics.SwitchGovernment(participantIdx, 4);
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2b) != 0 && this.owner.World.Participants[participantIdx].TownTally > 10)
			{
				this.owner.Politics.SwitchGovernment(participantIdx, 3);
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5) != 0)
			{
				this.owner.Politics.SwitchGovernment(participantIdx, 2);
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
				this.processor.EmitUInt16(this.processor.SegStack.Value16, LocationSentAddress(locationIdx), sentBase);
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
			Player player = this.owner.World.Participants[participantIdx];

			horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			vertLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			continentIdx = this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);

			if ((((byte)troopIdx + (byte)this.owner.World.RoundTally) & 0xf) != 0)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0xc)) == 0)
			{
				if (player.RegimeKind <= 1 && player.Landmasses[continentIdx].Approach != 5)
					return false;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 1)
					return false;
			}

			if (this.owner.UnitOps.VerifyTileOccupation(participantIdx, horizLoc, vertLoc) != 0)
				return false;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
			{
				return false;
			}

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain ||
				player.Landmasses[continentIdx].Approach == 2)
			{
				return false;
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

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
						(short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx)) << 2) >=
							CPU.SignedQuotient(distance.Value, (short)sentBase))
					{
						bestDistance = distance.Value;
						best = locationIdx;
					}
				}
			}

			if (best == -1)
				return;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.processor.PullUInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, best)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.processor.PullUInt8(this.processor.SegData.Value16, LocationVert(participantIdx, best)));

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & 0xf0));

			locationSent[best]++;
			ushort sent = LocationSentAddress(best);
			this.processor.EmitUInt16(this.processor.SegData.Value16, sent, (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, sent) + 1));
		}

		/// <summary>
		/// The word of a location in the copy of the counts of the units sent that stays in the frame of OperateAITurn
		/// (BP-0x44, one word per location). Nothing here reads it, but HandlePlayerTurn, called later at the same depth of
		/// the stack, reads its words as uninitialized locals (0x12 the row of the cursor, 0x24, 0x36, 0x3e).
		/// </summary>
		private ushort LocationSentAddress(int locationIdx) => (ushort)(this.processor.RegFrame.Value16 + (locationIdx << 1) - 0x44);

		/// <summary>
		/// The location is used and wants the unit's role on the unit's continent, or the unit is a bomber (0xf) and the
		/// location wants a bomber or the role 3
		/// </summary>
		private bool LocationFits(short participantIdx, short troopIdx, short locationIdx, ushort continentIdx)
		{
			byte role = this.processor.PullUInt8(this.processor.SegData.Value16, LocationRole(participantIdx, locationIdx));

			if (role == 0xff)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (UnitKindWord(kind, UnitKindTable.TroopGroup) == (ushort)(sbyte)role &&
				this.owner.MapOps.ObtainContinentID(this.processor.PullInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					this.processor.PullInt8(this.processor.SegData.Value16, LocationVert(participantIdx, locationIdx))) == continentIdx)
			{
				return true;
			}

			byte kindByte = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kindByte != 0xf)
				return false;

			sbyte bomberRole = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationRole(participantIdx, locationIdx));

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
				if ((this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx)) & 1) == 0 ||
					(((byte)locationIdx + (byte)(this.owner.World.RoundTally >> 1)) & 1) == 0)
				{
					return null;
				}

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc,
					this.processor.PullInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					vertLoc, this.processor.PullInt8(this.processor.SegData.Value16, LocationVert(participantIdx, locationIdx)));

				short range = (short)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally);
				weighed = (short)((range >= distance ? 2 : 4) * distance);
			}
			else
			{
				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc,
					this.processor.PullInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					vertLoc, this.processor.PullInt8(this.processor.SegData.Value16, LocationVert(participantIdx, locationIdx)));

				weighed = (short)(locationSent[locationIdx] * distance);
			}

			sbyte priority = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx));
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

			sbyte priority = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx));

			if (priority < 2)
				return false;

			if ((short)(priority * (short)sentBase) < distance &&
				locationSent[locationIdx] != sentBase)
			{
				return false;
			}

			return (this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) & 1) == 0;
		}

		/// <summary>
		/// The locals of ShiftAIUnit (its frame of 0x5e bytes), shared by the methods it calls as in the original: named words
		/// of the frame, read and written on the stack as the original does them. The frame stays: SurveyStack reads 0xc before
		/// anything writes it, and functions called later at the same depth read its words as uninitialized locals
		/// (LocateNearestFriendlyCity, HandlePlayerTurn; Tests/Dual/LeftoverWatcher.cs).
		/// </summary>
		private sealed class UnitLook(CPU processor)
		{
			/// <summary>0x2: the approach of the player on the continent of the unit</summary>
			public ushort Approach { get => processor.PullLocalWord(0x2); set => processor.EmitLocalWord(0x2, value); }
			/// <summary>0x4: the nearest unit (of the player, or the bomber of the human player for a fighter)</summary>
			public ushort NearestUnit { get => processor.PullLocalWord(0x4); set => processor.EmitLocalWord(0x4, value); }
			/// <summary>0x6: the city size per guard the city wants (3 or 4, one more or less by the approach)</summary>
			public ushort GuardShare { get => processor.PullLocalWord(0x6); set => processor.EmitLocalWord(0x6, value); }
			/// <summary>0x8: the kind of the unit</summary>
			public ushort Kind { get => processor.PullLocalWord(0x8); set => processor.EmitLocalWord(0x8, value); }
			/// <summary>0xa: how far the direction is from the heading of the unit (written only)</summary>
			public ushort Away { get => processor.PullLocalWord(0xa); set => processor.EmitLocalWord(0xa, value); }
			/// <summary>0xc: the nearest coastal city of the player for a transport; SurveyStack reads it before anything writes it</summary>
			public ushort Harbour { get => processor.PullLocalWord(0xc); set => processor.EmitLocalWord(0xc, value); }
			/// <summary>0xe: the column of the tile around the tile of the direction</summary>
			public ushort AroundHoriz { get => processor.PullLocalWord(0xe); set => processor.EmitLocalWord(0xe, value); }
			/// <summary>0x10: the score of the direction; the bits of the roads in the directions</summary>
			public ushort Score { get => processor.PullLocalWord(0x10); set => processor.EmitLocalWord(0x10, value); }
			/// <summary>0x12: the distance to the coastal city</summary>
			public ushort HarbourDistance { get => processor.PullLocalWord(0x12); set => processor.EmitLocalWord(0x12, value); }
			/// <summary>0x14: the best distance so far; the distance of the nearest bomber</summary>
			public ushort BestDistance { get => processor.PullLocalWord(0x14); set => processor.EmitLocalWord(0x14, value); }
			/// <summary>0x16: the row of the tile around the tile of the direction</summary>
			public ushort AroundVert { get => processor.PullLocalWord(0x16); set => processor.EmitLocalWord(0x16, value); }
			/// <summary>0x1a: the continent of the unit</summary>
			public ushort Continent { get => processor.PullLocalWord(0x1a); set => processor.EmitLocalWord(0x1a, value); }
			/// <summary>0x1c: a foreign unit at the tile of the direction (an enemy next to the unit)</summary>
			public ushort Foreign { get => processor.PullLocalWord(0x1c); set => processor.EmitLocalWord(0x1c, value); }
			/// <summary>0x1e: the bits of the roles of the other units of the stack</summary>
			public ushort StackRoles { get => processor.PullLocalWord(0x1e); set => processor.EmitLocalWord(0x1e, value); }
			/// <summary>0x20: the owner of a land tile around a transport</summary>
			public ushort TileOwner { get => processor.PullLocalWord(0x20); set => processor.EmitLocalWord(0x20, value); }
			/// <summary>0x22: the city looked at by HasCityNear</summary>
			public ushort CityLoop { get => processor.PullLocalWord(0x22); set => processor.EmitLocalWord(0x22, value); }
			/// <summary>0x24: the unit at the tile of the direction</summary>
			public ushort TileUnit { get => processor.PullLocalWord(0x24); set => processor.EmitLocalWord(0x24, value); }
			/// <summary>0x26: the column of the tile of the direction</summary>
			public ushort TileHoriz { get => processor.PullLocalWord(0x26); set => processor.EmitLocalWord(0x26, value); }
			/// <summary>0x28: the best score so far (-999 at the start)</summary>
			public ushort BestScore { get => processor.PullLocalWord(0x28); set => processor.EmitLocalWord(0x28, value); }
			/// <summary>0x2a: the distance to the nearest city</summary>
			public ushort CityDistance { get => processor.PullLocalWord(0x2a); set => processor.EmitLocalWord(0x2a, value); }
			/// <summary>0x2c: the best direction so far; a count</summary>
			public ushort BestDirection { get => processor.PullLocalWord(0x2c); set => processor.EmitLocalWord(0x2c, value); }
			/// <summary>0x2e: the row of the tile of the direction</summary>
			public ushort TileVert { get => processor.PullLocalWord(0x2e); set => processor.EmitLocalWord(0x2e, value); }
			/// <summary>0x30: the column of the unit</summary>
			public ushort Horiz { get => processor.PullLocalWord(0x30); set => processor.EmitLocalWord(0x30, value); }
			/// <summary>0x32: the distance to the nearest unit</summary>
			public ushort UnitDistance { get => processor.PullLocalWord(0x32); set => processor.EmitLocalWord(0x32, value); }
			/// <summary>0x34: the nearest city (the target of a nuclear weapon, the city to land in)</summary>
			public ushort NearestCity { get => processor.PullLocalWord(0x34); set => processor.EmitLocalWord(0x34, value); }
			/// <summary>0x36: the row of the unit</summary>
			public ushort Vert { get => processor.PullLocalWord(0x36); set => processor.EmitLocalWord(0x36, value); }
			/// <summary>0x38: the terrain of the tile of the direction</summary>
			public ushort TileTerrain { get => processor.PullLocalWord(0x38); set => processor.EmitLocalWord(0x38, value); }
			/// <summary>0x3a: the direction from the round; a road two tiles away; the approach at the coastal city</summary>
			public ushort Round { get => processor.PullLocalWord(0x3a); set => processor.EmitLocalWord(0x3a, value); }
			/// <summary>0x3c: the load of the transport is wanted on the continent</summary>
			public ushort LoadWanted { get => processor.PullLocalWord(0x3c); set => processor.EmitLocalWord(0x3c, value); }
			/// <summary>0x3e: HasCityNear found a city</summary>
			public ushort CityNear { get => processor.PullLocalWord(0x3e); set => processor.EmitLocalWord(0x3e, value); }
			/// <summary>0x40: the direction (the try)</summary>
			public ushort Direction { get => processor.PullLocalWord(0x40); set => processor.EmitLocalWord(0x40, value); }
			/// <summary>0x42: the terrain of the unit</summary>
			public ushort Terrain { get => processor.PullLocalWord(0x42); set => processor.EmitLocalWord(0x42, value); }
			/// <summary>0x44: the tile around the tile of the direction</summary>
			public ushort Around { get => processor.PullLocalWord(0x44); set => processor.EmitLocalWord(0x44, value); }
			/// <summary>0x4a: the continent of the tile of the direction</summary>
			public ushort TileContinent { get => processor.PullLocalWord(0x4a); set => processor.EmitLocalWord(0x4a, value); }
			/// <summary>0x4c: the unit may stay on its tile</summary>
			public ushort MayStay { get => processor.PullLocalWord(0x4c); set => processor.EmitLocalWord(0x4c, value); }
			/// <summary>0x50: the bit of the player</summary>
			public ushort PlayerBit { get => processor.PullLocalWord(0x50); set => processor.EmitLocalWord(0x50, value); }
			/// <summary>0x52: the best direction was next to an enemy</summary>
			public ushort BestNextToEnemy { get => processor.PullLocalWord(0x52); set => processor.EmitLocalWord(0x52, value); }
			/// <summary>0x54: the land units of the stack; the roads around</summary>
			public ushort LandUnits { get => processor.PullLocalWord(0x54); set => processor.EmitLocalWord(0x54, value); }
			/// <summary>0x56: the role of the unit</summary>
			public ushort Role { get => processor.PullLocalWord(0x56); set => processor.EmitLocalWord(0x56, value); }
			/// <summary>0x58: the player of the unit at the tile of the direction</summary>
			public ushort TileUnitOwner { get => processor.PullLocalWord(0x58); set => processor.EmitLocalWord(0x58, value); }
			/// <summary>0x5a: the weight of the battle with the unit at the tile</summary>
			public ushort BattleScore { get => processor.PullLocalWord(0x5a); set => processor.EmitLocalWord(0x5a, value); }
			/// <summary>0x5c: no enemy stands next to the unit</summary>
			public ushort NoEnemyNear { get => processor.PullLocalWord(0x5c); set => processor.EmitLocalWord(0x5c, value); }
			/// <summary>0x5e: the heading of the unit</summary>
			public ushort Heading { get => processor.PullLocalWord(0x5e); set => processor.EmitLocalWord(0x5e, value); }
			/// <summary>The low byte of the heading alone (mov byte [bp - 0x5e]), the high byte stays</summary>
			public byte HeadingLow { set => processor.EmitLocalByte(0x5e, value); }
		}

		public ushort ShiftAIUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ShiftAIUnit({participantIdx}, {troopIdx})");

			this.processor.EnterFrame(0x5e);
			this.processor.PreserveIndexRegisters();

			// (the barbarians leave the accumulator as their move leaves it)
			if (participantIdx == 0)
				ManageBarbarianUnit(participantIdx, troopIdx);
			else
				this.processor.RegAcc.Value16 = DecideAIUnitMove(participantIdx, troopIdx);

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftAIUnit");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The move of a unit of a computer player (not a barbarian), as ShiftAIUnit answers it: planes, a look at the
		/// surroundings, nuclear weapons, diplomats, exploring, the defence of a city, the strategic locations, the transports
		/// and attacking ships, pillaging, settlers, the decisions before a fight and at last the best of the 8 directions.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort DecideAIUnitMove(short participantIdx, short troopIdx)
		{
			// Planes out of fuel or moves, the fighters after a bomber of the human player or a city to land in
			ushort? airAnswer = PlanAirUnit(participantIdx, troopIdx);

			if (airAnswer != null)
				return airAnswer.Value;

			SurveyAIUnit(participantIdx, troopIdx);

			if (this.look.Kind == 0x19)
				return AimNuclearWeapon(participantIdx, troopIdx);

			if (this.look.Kind == UnitKindTable.Diplomat)
			{
				// A diplomat with a destination goes on, one near another player's city goes there, one in an own city is given up
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
					return 0;

				this.processor.RegSource.Value16 = (ushort)(this.look.NearestCity * 0x1c);
				Game.State.City town = this.owner.World.Towns[this.look.NearestCity];

				if (town.ParticipantIdx != participantIdx)
				{
					// (DI and DX as the original leaves them: the unit and the high word of its multiplications)
					this.processor.RegDest.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
					this.processor.RegData.Value16 = 0;
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);

					return 0;
				}

				// (the look goes on for the unit given up)
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			}

			ushort? explored = ExploreAround(participantIdx, troopIdx);

			if (explored != null)
				return explored.Value;

			switch (GuardCity(participantIdx, troopIdx))
			{
				case CityGuard.Stays:
					return 0x66;

				case CityGuard.NotDefender:
					// A unit (not settlers) with another unit somewhere near goes to a strategic location
					if ((short)this.look.UnitDistance > 0 && this.look.Role != 0)
					{
						SendUnitToStrategicLocation(participantIdx, (short)this.look.Horiz, (short)this.look.Vert, 2, 4);
						return 0x20;
					}

					break;
			}

			// The attacking ships (role 3) and the transports (role 5) after a look at their stack: a transport with too few
			// land units on board goes to a coastal city, one next to land where its units may land unloads them there, else a
			// destination is chosen
			if (this.look.Role == 5 || this.look.Role == 3)
			{
				SurveyStack(participantIdx, troopIdx);

				if (!ChooseStagingCity(participantIdx, troopIdx))
				{
					ushort? landing = PlanTransportLanding(participantIdx, troopIdx);

					if (landing != null)
						return landing.Value;

					ChooseTransportDestination(participantIdx, troopIdx);
				}
			}

			// A slow unit next to a human city, not at peace, on an irrigated or mined tile of it
			if (StandsOnHumanFields(participantIdx, troopIdx))
				return 0x50;

			if (this.look.Role == 0)
			{
				// Settlers improve the land around their city, build roads or look for a place for a city
				ushort? settlerAnswer = DirectSettlers(participantIdx, troopIdx);

				if (settlerAnswer != null)
					return settlerAnswer.Value;
			}

			// A unit with a destination goes there; defenders stay, settlers found or join cities
			ushort? stayAnswer = DecideBeforeFight(participantIdx, troopIdx);

			if (stayAnswer != null)
				return stayAnswer.Value;

			// Each of the 8 directions around: some are no target, the others are scored (the tile, the unit there and the
			// surroundings), the best direction so far is kept
			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				if (MayAimAtDirection(participantIdx, troopIdx) && ScoreTile(participantIdx, troopIdx) && WeighUnitAtTile(participantIdx, troopIdx))
					ScoreSurroundings(participantIdx, troopIdx);
			}

			// The heading: the best direction (none for an attacker short of moves next to an enemy)
			return ChooseHeading(participantIdx, troopIdx);
		}

		/// <summary>
		/// The tile of the direction 0x40 (0x26 / 0x2e, owner 0x58, terrain 0x38, unit 0x24: a diplomat stands back behind
		/// another unit of its stack) is no target (false) outside the map, at sea for a land unit, as a free tile it may
		/// occupy for a land unit that may stay (0x4c) on land, with a foreign unit for a land unit at sea, on land for a
		/// ship (but a foreign unit there for a ship at sea, not a submarine) and with an own stack of 2 or more units.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private bool MayAimAtDirection(short participantIdx, short troopIdx)
		{
			ushort direction = (ushort)(this.look.Direction << 1);

			this.look.TileHoriz = (ushort)this.owner.Routing.TunePosition((short)DirectionHorizAt(direction) +
				(short)this.look.Horiz);
			this.look.TileVert = (ushort)(DirectionVertAt(direction) + this.look.Vert);

			short horizLoc = (short)this.look.TileHoriz;
			short vertLoc = (short)this.look.TileVert;

			if (this.owner.MapOps.VerifyMapLimits(horizLoc, vertLoc) == 0)
				return false;

			this.look.TileUnitOwner = this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc);
			this.look.TileTerrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (this.look.TileTerrain == MapRules.OceanTerrain &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				return false;
			}

			this.look.TileUnit = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			if (this.look.TileUnit != 0xffff)
			{
				ushort owner = FoundPlayer;

				if (this.troops.Pull(owner, this.look.TileUnit, UnitMemory.Kind) == UnitKindTable.Diplomat)
				{
					// The next unit of the stack that is not a diplomat
					this.look.TileOwner = this.look.TileUnit;

					do
					{
						this.look.TileOwner = (ushort)this.troops.PullSigned(owner, this.look.TileOwner, UnitMemory.FollowingInStack);
					}
					while (this.look.TileOwner != 0xffff && this.look.TileOwner != this.look.TileUnit &&
						this.troops.Pull(owner, this.look.TileOwner, UnitMemory.Kind) == UnitKindTable.Diplomat);

					if (this.look.TileOwner != 0xffff)
						this.look.TileUnit = this.look.TileOwner;
				}
			}

			bool free = this.look.TileUnit == 0xffff;

			if (this.look.MayStay != 0 && this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				free && this.owner.UnitOps.MayOccupyTile(participantIdx, horizLoc, vertLoc) != 0 &&
				this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat && this.look.Terrain != MapRules.OceanTerrain)
			{
				return false;
			}

			if (this.look.Terrain == MapRules.OceanTerrain &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				!free && this.look.TileUnitOwner != (ushort)participantIdx)
			{
				return false;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
				this.look.TileTerrain != MapRules.OceanTerrain)
			{
				// A ship next to land: only a foreign unit, not for a submarine, and only from the sea
				if (free || this.look.TileUnitOwner == (ushort)participantIdx || this.look.Kind == UnitKindTable.Submarine ||
					this.look.Terrain != MapRules.OceanTerrain)
				{
					return false;
				}
			}

			return free || this.look.TileUnitOwner != (ushort)participantIdx ||
				(short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)this.look.TileUnit, 2) < 2;
		}

		/// <summary>
		/// The first score 0x10 of the tile of the direction 0x40 (0x26 / 0x2e, terrain 0x38, unit 0x24 of the player 0x58):
		/// settlers start from 0 (on a ship they skip a tile they may not occupy: false); other units unseen (byte 9) on land a
		/// die of 3 less the defence (twice it for a defender) of the terrain, else a die of 5 and with an own stack there its
		/// strength (StackStrengthRules: the tally + 1, or with ClassicStackStrength SI as the second tally leaves
		/// it), or 4 times the defence of the terrain;
		/// planes a die of 3. The farther the direction from the heading of the unit (0x5e), the less. The locals of
		/// ShiftAIUnit are used as in the original.
		/// </summary>
		private bool ScoreTile(short participantIdx, short troopIdx)
		{
			ushort role = this.look.Role;

			if (role == 0)
			{
				this.look.Score = 0;

				if (this.look.Terrain == MapRules.OceanTerrain &&
					this.owner.UnitOps.VerifyTileOccupation(participantIdx, (short)this.look.TileHoriz, (short)this.look.TileVert) != 0)
				{
					return false;
				}
			}
			else
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) == 0 && this.look.Terrain != MapRules.OceanTerrain)
				{
					short die = (short)this.owner.Libc.Dice.DrawInt(3);
					short defence = role == 1 ? (short)(TerrainByte(0x28e) << 1) : TerrainByte(0x28f);
					this.look.Score = (ushort)(die - defence);
				}
				else
				{
					this.look.Score = (ushort)this.owner.Libc.Dice.DrawInt(5);

					if (this.look.TileUnit != 0xffff && this.look.TileUnitOwner == (ushort)participantIdx)
					{
						short stack = (short)this.look.TileUnit;

						if (role == 1)
						{
							ushort tallyPlusOne = (ushort)(this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, stack, 2) + 1);
							this.processor.RegSource.Value16 = tallyPlusOne;
							ushort strength = (ushort)(this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, stack, 1) << 2);
							ushort share = DivideWords(strength,
								StackStrengthRules.Divisor(tallyPlusOne, this.processor.RegSource.Value16, this.owner.TurnHooks.ClassicStackStrength));
							this.look.Score = (ushort)(this.look.Score + share);
						}

						if (role == 2)
						{
							ushort tallyPlusOne = (ushort)(this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, stack, 1) + 1);
							this.processor.RegSource.Value16 = tallyPlusOne;
							ushort strength = (ushort)(this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, stack, 3) << 1);
							ushort share = DivideWords(strength,
								StackStrengthRules.Divisor(tallyPlusOne, this.processor.RegSource.Value16, this.owner.TurnHooks.ClassicStackStrength));
							this.look.Score = (ushort)(this.look.Score + share);
						}
					}
					else
					{
						this.look.Score = (ushort)(this.look.Score + (TerrainByte(0x28f) << 2));
					}
				}

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
					this.look.Score = (ushort)this.owner.Libc.Dice.DrawInt(3);
			}

			byte heading = this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHeading);
			this.look.HeadingLow = heading;

			if (heading != 0xff)
			{
				short away = this.owner.Libc.AbsoluteValue((short)((sbyte)heading - (short)this.look.Direction));

				if (away > 4)
					away = (short)(8 - away);

				this.look.Away = (ushort)away;
				this.look.Score = (ushort)(this.look.Score - ((away * away) << 1));
			}

			return true;
		}

		/// <summary>A signed byte of the terrain 0x38 in the table of terrains (0x13 bytes each)</summary>
		private short TerrainByte(ushort offset)
		{
			return (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.look.TileTerrain * 0x13 + offset));
		}

		/// <summary>
		/// The unit at the tile of the direction counts in the score 0x10 (0x1c marks a foreign one): a free tile +20 with a
		/// hut, 999 for a foreign city; an own unit less its defence. Settlers next to a foreign unit and units at peace with
		/// it skip the tile (false); at peace a militia under approach 1 may buy a lone veteran unit of a player at war with
		/// the human player (who has cities on the continent) outside a city. At war the best defender of the stack (a plane
		/// outside a city only for kind 0xe) is weighed by the battle (0x5a: the result times the units of the stack over the
		/// price, x3 in a city, x3 for a militia under approach 1, x2 for a defender with a weak stack): from 6 (12 for the
		/// barbarians) 4 times that, else -999 and a militia on land may buy a lone unit on a good defensive terrain.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private bool WeighUnitAtTile(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.TileHoriz;
			short vertLoc = (short)this.look.TileVert;
			this.look.Foreign = 0;

			if (this.look.TileUnit == 0xffff)
			{
				if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 && this.look.TileUnitOwner != (ushort)participantIdx)
					this.look.Score = 999;

				if (this.owner.MapOps.ContainsHut(this.look.TileTerrain, horizLoc, vertLoc) != 0)
					this.look.Score = (ushort)(this.look.Score + 20);

				return true;
			}

			if (this.look.TileUnitOwner == (ushort)participantIdx)
			{
				this.look.Score = (ushort)(this.look.Score - this.troops.KindField(participantIdx, troopIdx, UnitKindTable.GuardPower));
				return true;
			}

			this.look.Foreign = 1;
			ushort role = this.look.Role;

			if (role == 0)
				return false;

			if ((this.owner.World.Participants[participantIdx].Relations[this.look.TileUnitOwner] & 2) != 0)
			{
				BuyUnitAtPeace(participantIdx, troopIdx, horizLoc, vertLoc);
				return false;
			}

			short enemy = (short)this.look.TileUnitOwner;
			this.look.TileUnit = this.owner.UnitOps.LocateBestGuardianInStack(enemy, (short)this.look.TileUnit);

			bool airGuardian = this.troops.KindField(enemy, (short)this.look.TileUnit, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;

			if (airGuardian && this.troops.TroopKind(participantIdx, troopIdx) != 0xe &&
				(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
			{
				return false;
			}

			// (SI as the original leaves it for the battle: what a battle that throws leaves behind)
			this.processor.RegSource.Value16 = (ushort)((airGuardian ? participantIdx : enemy) * UnitMemory.ParticipantStride);
			this.look.BattleScore = this.owner.Combat.SettleBattle(participantIdx, troopIdx,
				(short)this.look.TileUnitOwner, (short)this.look.TileUnit, 0);

			ushort losses = MultiplyWords((ushort)(this.owner.UnitOps.TallyUnitsByDomainInStack((short)this.look.TileUnitOwner,
				(short)this.look.TileUnit, 0) + 1), this.look.BattleScore);
			this.look.BattleScore = losses;
			this.look.BattleScore = DivideWords(losses, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				this.look.BattleScore = (ushort)(this.look.BattleScore * 3);

			if (role == 1 && this.look.Approach == 1)
				this.look.BattleScore = (ushort)(this.look.BattleScore * 3);

			if (role == 1)
			{
				short attack = (short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 3);

				if ((short)(this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower) << 1) < attack)
					this.look.BattleScore = (ushort)(this.look.BattleScore << 1);
			}

			short enough = (short)(participantIdx == 0 ? 0xc : 6);

			if (enough <= (short)this.look.BattleScore)
			{
				this.look.Score = (ushort)(this.look.Score + (this.look.BattleScore << 2));
				return true;
			}

			this.look.Score = (ushort)(this.look.Score - 999);

			if (role == 1 && this.look.Approach == 1 &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				(this.troops.Pull(this.look.TileUnitOwner, this.look.TileUnit, UnitMemory.Status) & 0x28) != 0 &&
				TerrainByte(0x28f) >= 4 &&
				(short)this.owner.UnitOps.TallyUnitsByDomainInStack((short)this.look.TileUnitOwner, (short)this.look.TileUnit, 2) < 2 &&
				this.owner.Libc.Dice.DrawInt(4) == 0 &&
				(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
			{
				this.owner.Diplomats.BuyUnit((short)this.look.TileUnitOwner, (short)this.look.TileUnit, participantIdx);
			}

			return true;
		}

		/// <summary>
		/// At peace with the player of the unit at the tile: a militia under approach 1 (a land unit) may buy it when it is a
		/// veteran (status bit 3), alone, outside a city, its player at war with the human player while this one is at peace
		/// with him and he has cities on the continent (0x1a); seven times in eight only next to a city of the human player.
		/// </summary>
		private void BuyUnitAtPeace(short participantIdx, short troopIdx, short horizLoc, short vertLoc)
		{
			if (this.look.Role != 1 || this.look.Approach != 1)
				return;

			// (DI as the original leaves it: the player, then the player of the unit at the tile)
			this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			short other = (short)this.look.TileUnitOwner;
			short stack = (short)this.look.TileUnit;
			this.processor.RegDest.Value16 = (ushort)(other * UnitMemory.ParticipantStride);
			short person = this.owner.World.PersonParticipantIdx;

			if ((this.troops.Pull(other, stack, UnitMemory.Status) & 8) == 0 ||
				(this.owner.World.Participants[participantIdx].Relations[person] & 3) != 1 ||
				(this.owner.World.Participants[other].Relations[person] & 3) == 1 ||
				this.owner.World.Participants[person].Landmasses[this.look.Continent].TownTally == 0 ||
				(short)this.owner.UnitOps.TallyUnitsByDomainInStack(other, stack, 2) >= 2)
			{
				return;
			}

			if (this.owner.Libc.Dice.DrawInt(8) != 0 && this.owner.World.Towns[this.look.NearestCity].ParticipantIdx != person)
				return;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				return;

			this.owner.Diplomats.BuyUnit((short)this.look.TileUnitOwner, (short)this.look.TileUnit, participantIdx);
		}

		/// <summary>
		/// The score 0x10 of the direction 0x40 grows by the surroundings when no enemy stands next to the unit (0x5c): +8 for
		/// an unmarked land tile four steps away (the pattern 0x3772:0x2438), and for each tile around the tile of the direction
		/// (0x44, at 0xe / 0x16) +2 when the player has not seen it (land, or the sea for a ship), -2 with units there and the
		/// worth of its terrain (byte 0x290) for settlers. Next to an enemy (0x1c) with fewer than 3 moves left it is scaled by
		/// the share of the attackers in the stack. The best score (0x28) keeps the direction (0x2c) and the enemy mark (0x52).
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private void ScoreSurroundings(short participantIdx, short troopIdx)
		{
			if (this.look.NoEnemyNear != 0)
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				this.look.TileHoriz = (ushort)((DirectionHorizAt(direction) << 2) + this.look.Horiz);
				this.look.TileVert = (ushort)((DirectionVertAt(direction) << 2) + this.look.Vert);

				// (the divisions as the original makes them)
				this.processor.RegAcc.Value16 = this.look.TileHoriz;
				this.processor.DivideAccumulatorByPowerOfTwo(2);
				this.processor.RegCount.Value16 = 0xd;
				this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
				this.processor.RegAcc.Value16 = this.look.TileVert;
				this.processor.DivideAccumulatorByPowerOfTwo(2);
				this.processor.RegBase.Value16 = (ushort)(this.processor.RegBase.Value16 + this.processor.RegAcc.Value16);
				this.processor.SegExtra.Value16 = 0x3772;

				short farHoriz = (short)this.look.TileHoriz;
				short farVert = (short)this.look.TileVert;

				if (this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)) == 0 &&
					this.owner.MapOps.ObtainTerrainType(farHoriz, farVert) != MapRules.OceanTerrain)
				{
					if (this.owner.MapOps.VerifyMapLimits(farHoriz, farVert) != 0)
						this.look.Score = (ushort)(this.look.Score + 8);
				}

				for (this.look.Around = 1; (short)this.look.Around <= 8;
					this.look.Around = (ushort)(this.look.Around + 1))
				{
					ushort around = (ushort)(this.look.Around << 1);

					this.look.AroundHoriz = (ushort)this.owner.Routing.TunePosition((short)DirectionHorizAt(around) +
						(short)this.look.TileHoriz);
					this.look.AroundVert = (ushort)(DirectionVertAt(around) + this.look.TileVert);

					short horizLoc = (short)this.look.AroundHoriz;
					short vertLoc = (short)this.look.AroundVert;

					if (this.owner.MapOps.VerifyMapLimits(horizLoc, vertLoc) == 0)
						continue;

					// Not seen by the player: land, or the sea for a ship
					if ((this.owner.World.ChartSight[this.look.AroundHoriz, this.look.AroundVert] & (short)this.look.PlayerBit) == 0 &&
						(this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
							UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
					{
						this.look.Score = (ushort)(this.look.Score + 2);
					}

					if (this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc) != 0xffff)
						this.look.Score = (ushort)(this.look.Score - 2);

					if (this.look.Role == 0)
					{
						ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
						this.look.Score = (ushort)(this.look.Score +
							(sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(terrain * 0x13 + 0x290)));
					}
				}
			}

			if (this.look.Foreign != 0 && this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 3)
			{
				short stack = (short)this.owner.Helpers.VerifyValueSpan((short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 3), 1, 99);
				this.processor.RegCount.Value16 = (ushort)stack;
				this.processor.RegSource.Value16 = (ushort)stack;

				ushort weighed = MultiplyWords(this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 1), this.look.Score);
				// (the original divides by SI as the tally leaves it, not by the number of units kept there)
				this.look.Score = DivideWords(weighed, this.processor.RegSource.Value16);
			}

			if ((short)this.look.Score > (short)this.look.BestScore)
			{
				this.look.BestScore = this.look.Score;
				this.look.BestDirection = this.look.Direction;
				this.look.BestNextToEnemy = this.look.Foreign;
			}
		}

		/// <summary>
		/// The end of the look around: the best direction (0x2c) becomes the heading of the unit and the answer of ShiftAIUnit;
		/// none (0) when the best was next to an enemy (0x52) and fewer than 3 moves are left.
		/// </summary>
		private ushort ChooseHeading(short participantIdx, short troopIdx)
		{
			if (this.look.BestNextToEnemy != 0 && this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 3)
				this.look.BestDirection = 0;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, (byte)this.look.BestDirection);

			return this.look.BestDirection;
		}

		/// <summary>
		/// Before the look around for a fight: a unit with a destination goes on (0), an attacker only when no foreign unit
		/// stands next to it; a defender (role 2) on land fortifies (0x66) next to its own city or with an enemy around; settlers
		/// on a continent with enough cities (more than an eighth of the places) build a city in a small city (0x62) or go
		/// to their small city (0). Else the look starts (0x28 the best -999, 0x2c, 0x5c: no enemy around; the heading of the
		/// unit is forgotten when there is one) and a militia under approach 1 counts as a defender (0x56 = 1); null.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? DecideBeforeFight(short participantIdx, short troopIdx)
		{
			ushort role = this.look.Role;
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff &&
				(role != 3 || this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx, horizLoc, vertLoc) == 0))
			{
				return 0;
			}

			if (role == 2 && this.look.Terrain != MapRules.OceanTerrain)
			{
				if ((short)this.look.UnitDistance > 1 && (short)this.look.CityDistance <= 3 &&
					this.owner.World.Towns[this.look.NearestCity].ParticipantIdx == participantIdx)
				{
					return 0x66;
				}

				if (this.look.MayStay != 0 && this.look.UnitDistance != 0)
					return 0x66;
			}

			if (role == 0)
			{
				ushort continent = this.look.Continent;
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Landmasses[continent].ConstructionSpotTally;
				this.processor.DivideAccumulatorByPowerOfTwo(3);

				if (this.owner.World.Participants[participantIdx].Landmasses[continent].TownTally > (short)this.processor.RegAcc.Value16)
				{
					Game.State.City town = this.owner.World.Towns[this.look.NearestCity];

					if (this.look.CityDistance == 0 && (sbyte)town.TrueExtent < 10)
						return 0x62;

					if (town.ParticipantIdx == participantIdx && (sbyte)town.TrueExtent < 10)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);
						this.processor.RegDest.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

						return 0;
					}
				}
			}

			this.look.BestScore = 0xfc19;
			this.look.BestDirection = 0;
			this.look.NoEnemyNear = 0;

			if (this.owner.UnitOps.VerifyTileOccupation(participantIdx, horizLoc, vertLoc) != 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			else
				this.look.NoEnemyNear = 1;

			if (this.look.Kind == 1 && this.look.Approach == 1)
				this.look.Role = 1;

			return null;
		}

		/// <summary>
		/// A unit with fewer than 2 moves next to (0x2a under 4) a city of the human player (0x34), who is not at peace with
		/// the player, standing on an irrigated or mined tile: it pillages (0x50). The locals of ShiftAIUnit are used as in the
		/// original; DI as the original leaves it.
		/// </summary>
		private bool StandsOnHumanFields(short participantIdx, short troopIdx)
		{
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);
			this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if ((short)UnitKindWord(kind, UnitKindTable.ShiftTally) >= 2 || (short)this.look.CityDistance >= 4)
				return false;

			short person = this.owner.World.PersonParticipantIdx;

			if (this.owner.World.Towns[this.look.NearestCity].ParticipantIdx != person)
				return false;

			this.processor.RegDest.Value16 = (ushort)(participantIdx << 1);

			if ((this.owner.World.Participants[person].Relations[participantIdx] & 2) != 0 ||
				(this.owner.MapOps.ObtainTileImprovements((short)this.look.Horiz, (short)this.look.Vert) & 6) == 0)
			{
				return false;
			}

			return this.owner.World.Towns[this.look.NearestCity].ParticipantIdx != participantIdx;
		}

		/// <summary>
		/// Settlers (role 0) at the place 0x30 / 0x36 on the terrain 0x42: near their city (0x34, distance 0x2a) they irrigate
		/// (0x69 / 0x72 by the move cost) or mine (0x6d) a tile worth it, build a fortress (0x70) on a tile marked for it, go to
		/// a better free tile around a small city, build roads (0x72) on a terrain worth it, else wander to a free land
		/// tile on their continent in the pattern 0x2438 (0). Null: it goes on (L2520), DI and ES as the original leaves them.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? DirectSettlers(short participantIdx, short troopIdx)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];
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
				if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x40) != 0)
					return 0x70;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
				{
					// The leader's wish for cities (word 0x1514 by the leader) times the places on the continent, against 16 per city
					ushort continent = this.look.Continent;
					short wish = (short)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1514 + 0x3a * player.ChiefVisuals)) + 1);
					short wanted = (short)MultiplyWords((ushort)wish, (ushort)this.owner.World.Landmasses[continent].ConstructionSpotTally);
					this.processor.RegData.Value16 = (ushort)(player.Landmasses[continent].TownTally << 4);

					if (wanted > (short)this.processor.RegData.Value16)
						return 0;
				}

				if ((short)this.look.CityDistance <= 1 && this.owner.World.Towns[this.look.NearestCity].ParticipantIdx == participantIdx &&
					(this.owner.World.DiagMarkers & 2) != 0 && ChooseSettlersTile(participantIdx, troopIdx))
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

			this.look.Round = (ushort)(this.owner.Libc.Dice.DrawInt(20) + 1);
			ushort direction = (ushort)(this.look.Round << 1);
			this.processor.RegDest.Value16 = direction;
			this.look.TileHoriz = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) >> 2) +
				DirectionHorizAt(direction));
			this.look.TileVert = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) >> 2) +
				DirectionVertAt(direction));
			this.processor.SegExtra.Value16 = 0x3772;

			if ((this.processor.PullUInt8(this.processor.SegExtra.Value16,
				(ushort)(0xd * this.look.TileHoriz + this.look.TileVert + 0x2438)) & 3) != 0)
			{
				return null;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz,
				(byte)((this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(direction + 0x1882)) << 2) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert,
				(byte)((this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(direction + 0x18e4)) << 2) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert)));

			short travelHoriz = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz);
			short travelVert = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert);

			if (this.owner.MapOps.VerifyMapLimits(travelHoriz, travelVert) != 0)
			{
				ushort terrain = this.owner.MapOps.ObtainTerrainType(travelHoriz, travelVert);

				if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(terrain * 0x13 + 0x291)) != 0 &&
					this.owner.MapOps.ObtainContinentID(travelHoriz, travelVert) == this.look.Continent)
				{
					return 0;
				}
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			return null;
		}

		/// <summary>
		/// The column of the terrain table (word 0x44a + terrain * 12) for the improvements the player may make: 5 with the
		/// technology 5, else 4
		/// </summary>
		private ushort SettlersTerrainColumn(short participantIdx)
		{
			return (ushort)((this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5) != 0 ? 5 : 4) << 1);
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

			if (this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x44a + terrain * 0xc + SettlersTerrainColumn(participantIdx))) == 0 &&
				this.look.Approach != 5)
			{
				return null;
			}

			short distance = (short)this.look.CityDistance;
			Game.State.City town = this.owner.World.Towns[this.look.NearestCity];

			if (distance <= 0 || distance > 2 || town.ParticipantIdx != participantIdx)
				return null;

			if ((sbyte)town.TrueExtent < 3 && terrain == 4 && this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) == 0)
				return null;

			if ((this.owner.World.DiagMarkers & 2) == 0)
				return null;

			ushort cost = this.owner.TurnLogic.ObtainTileMoveCost(horizLoc, vertLoc);

			if (cost == 1 || cost == 2)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
				return (ushort)(cost == 1 ? 0x69 : 0x6d);
			}

			this.look.Round = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);
			byte improvements = (byte)this.look.Round;

			if ((improvements & 6) != 0 && (improvements & 8) == 0 && (short)terrain <= 2)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
				return 0x72;
			}

			if ((improvements & 0x18) == 0x18 || this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31) == 0)
				return null;

			short worked = (short)this.owner.CityOps.MayCityWorkTile(horizLoc, vertLoc, 1);

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
				this.look.Round = (ushort)(((this.look.Direction + this.owner.World.RoundTally) & 7) + 1);
				ushort direction = (ushort)(this.look.Round << 1);
				this.look.TileHoriz = (ushort)(DirectionHorizAt(direction) + this.look.Horiz);
				this.look.TileVert = (ushort)(DirectionVertAt(direction) + this.look.Vert);

				short horizLoc = (short)this.look.TileHoriz;
				short vertLoc = (short)this.look.TileVert;

				this.look.TileOwner = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

				// Units of another player, or own settlers there
				if (this.look.TileOwner != 0xffff &&
					((ushort)participantIdx != FoundPlayer ||
						this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, (short)this.look.TileOwner, 0) != 0))
				{
					continue;
				}

				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				if (this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x44a + terrain * 0xc + SettlersTerrainColumn(participantIdx))) == 0 ||
					this.owner.TurnLogic.ObtainTileMoveCost(horizLoc, vertLoc) == 0)
				{
					continue;
				}

				Game.State.City town = this.owner.World.Towns[this.look.NearestCity];

				if ((sbyte)town.TrueExtent < 3 && this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 4 &&
					this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) == 0)
				{
					continue;
				}

				if ((short)(ushort)this.owner.Helpers.MergeCoordinates((short)(horizLoc - town.Place.X), (short)(vertLoc - town.Place.Y)) > 2)
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

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 8) != 0)
				return null;

			if (this.look.Terrain == 0xb && this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13) == 0)
				return null;

			ushort city = this.look.NearestCity;

			if ((city < 128 ? (ushort)this.owner.World.Towns[city].ParticipantIdx : (ushort)0xffff) != (ushort)participantIdx)
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
				short stepHoriz = (short)DirectionHorizAt(direction);
				short stepVert = (short)DirectionVertAt(direction);

				if ((this.owner.MapOps.ObtainTileImprovements(stepHoriz + horizLoc, stepVert + vertLoc) & 8) == 0)
					continue;

				this.look.Score = (ushort)(this.look.Score | (ushort)(1 << (byte)((byte)this.look.Direction - 1)));
				this.look.LandUnits = this.processor.IncrementWord(this.look.LandUnits);

				if ((this.owner.MapOps.ObtainTileImprovements(stepHoriz * 2 + horizLoc, stepVert * 2 + vertLoc) & 8) != 0)
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
				TerrainField(this.look.Terrain, TerrainTable.MoveCost) == 1)
			{
				return 0x72;
			}

			return null;
		}

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

			Game.State.Player player = this.owner.World.Participants[participantIdx];
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			byte roles = (byte)this.look.StackRoles;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff && ((roles & 2) != 0 || (roles & 1) == 0) &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10)
			{
				// The nearest enemy city, every eighth one
				this.look.BestDistance = 0x3e7;

				for (this.look.Direction = (ushort)(this.owner.World.RoundTally & 7); (short)this.look.Direction < 0x80;
					this.look.Direction = (ushort)(this.look.Direction + 8))
				{
					Game.State.City town = this.owner.World.Towns[this.look.Direction];
					this.look.Round = (ushort)town.ParticipantIdx;

					if (town.ConditionMarker == 0xff || this.look.Round == (ushort)participantIdx ||
						(player.Relations[this.look.Round] & 0x102) == 2)
					{
						continue;
					}

					if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing >= 7 &&
						this.look.Round != (ushort)this.owner.World.PersonParticipantIdx)
					{
						continue;
					}

					this.look.HarbourDistance = (ushort)this.owner.Helpers.ObtainDistance((short)this.look.Horiz, town.Place.X, (short)this.look.Vert, town.Place.Y);

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);
					}
				}
			}

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10)
			{
				// The nearest own city at least 8 away on another continent, not under attack
				this.look.BestDistance = 0x3e7;
				this.look.Score = 0xffff;

				if (this.owner.MapOps.ObtainTerrainType((short)this.look.Horiz, (short)this.look.Vert) != MapRules.OceanTerrain)
					this.look.Score = this.look.Continent;

				for (this.look.Direction = 0; (short)this.look.Direction < 0x80;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					Game.State.City town = this.owner.World.Towns[this.look.Direction];
					this.look.Round = (ushort)town.ParticipantIdx;

					if (town.ConditionMarker == 0xff || this.look.Round != (ushort)participantIdx)
						continue;

					this.look.HarbourDistance = (ushort)this.owner.Helpers.ObtainDistance((short)this.look.Horiz, town.Place.X, (short)this.look.Vert, town.Place.Y);

					if ((short)this.look.HarbourDistance < 8)
						continue;

					this.look.TileContinent = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

					if (this.look.TileContinent == this.look.Score)
						continue;

					var continent = player.Landmasses[this.look.TileContinent];

					if (continent.Approach == 5)
						continue;

					if (((byte)(this.processor.ShiftLeftByte(1, (byte)continent.Approach) & (byte)this.look.StackRoles) & 7) == 0)
						this.look.HarbourDistance = (ushort)(this.look.HarbourDistance << 1);

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);
					}
				}
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
				return;

			// A random land tile without cities of the player, farther with every try
			for (this.look.Direction = 2; (short)this.look.Direction < 0x18;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				this.look.Round = (ushort)this.owner.Libc.Dice.DrawInt(9);
				this.processor.RegSource.Value16 = unit;
				this.processor.RegDest.Value16 = (ushort)(this.look.Round << 1);

				this.look.TileHoriz = (ushort)this.owner.Routing.TunePosition((short)DirectionHorizAt(this.processor.RegDest.Value16) *
					(short)this.look.Direction + this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz));

				// (the registers of the multiplication as in the original)
				ushort step = MultiplyWords(DirectionVertAt(this.processor.RegDest.Value16),
					this.look.Direction);
				this.processor.RegCount.Value16 = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
				this.look.TileVert = (ushort)(step + this.processor.RegCount.Value16);

				short horizLoc = (short)this.look.TileHoriz;
				short vertLoc = (short)this.look.TileVert;

				if (vertLoc <= 2 || vertLoc >= 0x2f || this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if (player.Landmasses[this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc)].TownTally == 0)
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

			Game.State.Player player = this.owner.World.Participants[participantIdx];
			short person = this.owner.World.PersonParticipantIdx;
			bool forgets = false;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;

				this.look.TileHoriz = (ushort)this.owner.Routing.TunePosition((short)DirectionHoriz(direction) +
					(short)this.look.Horiz);
				this.look.TileVert = (ushort)(DirectionVert(direction) + this.look.Vert);

				short horizLoc = (short)this.look.TileHoriz;
				short vertLoc = (short)this.look.TileVert;

				this.look.TileOwner = this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if (this.owner.MapOps.VerifyMapLimits(horizLoc, vertLoc) == 0)
					continue;

				ushort owner = this.look.TileOwner;

				if (owner != 0xffff && owner != (ushort)participantIdx)
					continue;

				if (owner == (ushort)participantIdx &&
					(short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc), 2) >= 2)
				{
					continue;
				}

				if ((player.Relations[person] & 2) != 0 && this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) == (ushort)person &&
					(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0xf) != 0)
				{
					continue;
				}

				this.look.TileContinent = this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
				this.look.LoadWanted = 0;

				var continent = player.Landmasses[this.look.TileContinent];

				if ((continent.Assault < 16 && continent.Approach != 5) ||
					((byte)(this.processor.ShiftLeftByte(1, (byte)continent.Approach) & (byte)this.look.StackRoles) & 6) != 0)
				{
					this.look.LoadWanted = 1;
				}

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff &&
					this.owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert)) == this.look.TileContinent)
				{
					this.look.LoadWanted = 1;
				}

				if (this.look.LoadWanted == 0 ||
					this.owner.World.Landmasses[this.look.TileContinent].ConstructionSpotTally < 5 ||
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
				this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

				return 0x75;
			}

			if (forgets && (this.owner.World.RoundTally & 3) == 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 3)
				return null;

			this.look.TileHoriz = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz)) >> 1);
			this.look.TileVert = (ushort)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) + this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert)) >> 1);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
				return null;

			if (this.owner.MapOps.ObtainTerrainType((short)this.look.TileHoriz, (short)this.look.TileVert) == MapRules.OceanTerrain)
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

			Game.State.Player player = this.owner.World.Participants[participantIdx];

			this.look.BestDistance = 0x3e7;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				Game.State.City town = this.owner.World.Towns[this.look.Harbour];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx || (town.ConditionMarker & 2) == 0)
					continue;

				this.look.HarbourDistance = (ushort)this.owner.Helpers.ObtainDistance((short)this.look.Horiz, town.Place.X, (short)this.look.Vert, town.Place.Y);
				this.look.TileContinent = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

				if (player.Landmasses[this.look.TileContinent].Assault < 16)
					continue;

				this.look.TileUnit = this.owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y);

				if (this.look.TileUnit != 0xffff)
				{
					short attackers = (short)this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, (short)this.look.TileUnit, 5);

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

			// The original leaves SI at the last city of the loop
			this.processor.RegSource.Value16 = 0x7f * 0x1c;

			this.look.HarbourDistance = (ushort)(this.look.Round == 5 ? 4 : 2);

			if ((short)(3 * (short)this.look.HarbourDistance) < (short)this.look.BestDistance)
				return true;

			ushort approach = this.look.Round;
			short horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz);
			short vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert);
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			if (approach != 0)
				SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 0, (short)this.look.HarbourDistance);

			if (this.look.Round != 2)
			{
				this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
				SendUnitToStrategicLocation(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHoriz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelVert), 2, (short)this.look.HarbourDistance);
			}

			if (this.look.Round != 1)
			{
				this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
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

				if ((this.processor.PullUInt8(this.processor.SegData.Value16, status) & 8) == 0)
				{
					sbyte kind = this.troops.TroopKind(participantIdx, member);
					byte role = (byte)UnitKindWord(kind, UnitKindTable.TroopGroup);
					this.look.StackRoles = (ushort)(this.look.StackRoles | (ushort)(1 << role));

					if (UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
						this.look.LandUnits = (ushort)(this.look.LandUnits + 1);
				}
				else if (this.look.Approach == 5)
				{
					short fifth = (short)(this.owner.World.Towns[this.look.Harbour].TrueExtent / 5);
					short counted = (short)this.look.BestDirection;
					this.look.BestDirection = this.processor.IncrementWord(this.look.BestDirection);

					if (fifth < counted)
					{
						this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) & 0xf7));
						this.look.LandUnits = (ushort)(this.look.LandUnits + 1);
					}
				}

				current = member;
			}

			// The registers as the original leaves them
			this.processor.RegBase.Value16 = (ushort)(current * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
		}

		private enum CityGuard
		{
			/// <summary>Not in a city, or a defender that goes on (L166c)</summary>
			GoesOn,

			/// <summary>In a city, but not a defender (L168f)</summary>
			NotDefender,

			/// <summary>The defender stays (0x66)</summary>
			Stays,
		}

		/// <summary>
		/// A defender (role 2) in a city: alone, or better than the leader of its stack, it stays; it calls for more defenders
		/// when the city (size / need + 1; need 4, 3 with a unit of another player next to it, one less with the palace, one more
		/// on a continent at peace) has fewer, and stays while they are too few; the leader of the stack fortifies (bit 3) while
		/// the units there are too few; every eighth turn the unit loses its orders (bits 0, 2, 3)
		/// </summary>
		private CityGuard GuardCity(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
				return CityGuard.GoesOn;

			if (this.look.Role != 2)
				return CityGuard.NotDefender;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return CityGuard.Stays;

			// The leader of the stack without this unit (it counts as a diplomat while looking)
			this.look.Round = (ushort)this.troops.TroopKind(participantIdx, troopIdx);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0x1a);
			this.look.TileOwner = this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)this.look.Round);

			if (this.look.TileOwner != 0xffff)
			{
				short defence = (short)UnitKindWord((sbyte)(byte)this.look.Round, UnitKindTable.GuardPower);
				short leader = (short)this.look.TileOwner;

				if ((short)UnitKindWord(this.troops.TroopKind(participantIdx, leader), UnitKindTable.GuardPower) < defence)
					return CityGuard.Stays;
			}

			this.look.GuardShare = (ushort)(this.owner.UnitOps.VerifyTileOccupation(participantIdx, horizLoc, vertLoc) != 0 ? 3 : 4);

			Game.State.City town = this.owner.World.Towns[this.look.NearestCity];

			if (town.Has(CityStructure.Palace))
				this.look.GuardShare = this.processor.DecrementWord(this.look.GuardShare);
			else if (this.look.Approach == 0)
				this.look.GuardShare = this.processor.IncrementWord(this.look.GuardShare);

			this.look.Round = this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, troopIdx, 2);

			short wanted = (short)(CPU.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1);

			if (wanted > (short)this.look.Round)
				SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 2, 2);

			if (wanted >= (short)this.look.Round)
				return CityGuard.Stays;

			// The leader of the stack fortifies while the units there are too few
			ushort status = (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Status);
			this.look.Round = (ushort)(sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, status);
			this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) | 8));

			if (this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx) == (ushort)troopIdx)
			{
				short units = (short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 4);

				if ((short)(CPU.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1) > units)
				{
					// A translation slip: the status is put back at SI, which the count of the stack has left at 0x600 * player
					this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, (byte)this.look.Round);
					return CityGuard.Stays;
				}
			}

			this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)this.look.Round);

			if ((((byte)troopIdx + (byte)this.owner.World.RoundTally) & 7) == 0)
				this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) & 0xf2));

			// The original leaves SI at the status of the unit
			this.processor.RegSource.Value16 = status;

			return CityGuard.GoesOn;
		}

		/// <summary>
		/// A unit that may explore (role below 3) next to unknown land (the visibility layer): the direction with the most
		/// unknown around (0x2c, count 0x28), or straight to a hut (settlers excepted: role 0); in the first turn with 0xd76a
		/// it wanders; with enough unknown it explores (0x62 or the direction), a military unit there marks a strategic location
		/// instead. A settler next to the human player's city, with the human player ahead in technology, goes there as an
		/// ambassador (0x62). Null: the unit goes on (L14ac). The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? ExploreAround(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if (this.look.Role == 0)
				DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 0);

			this.look.BestScore = this.owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);

			if (this.look.BestScore != 0 && (short)this.look.Role < 3)
			{
				this.look.BestDirection = 0;

				for (this.look.Direction = 1; (short)this.look.Direction <= 8;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					ushort direction = this.look.Direction;

					this.look.TileHoriz = (ushort)this.owner.Routing.TunePosition((short)DirectionHoriz(direction) + horizLoc);
					this.look.TileVert = (ushort)(DirectionVert(direction) + this.look.Vert);

					short nextHoriz = (short)this.look.TileHoriz;
					short nextVert = (short)this.look.TileVert;

					this.look.Score = this.owner.VideoCard.ObtainDot(2, nextHoriz + 0x50, nextVert);

					if ((short)this.look.Score > (short)this.look.BestScore)
					{
						this.processor.RegAcc.Value16 = this.look.Score;
						this.look.BestScore = this.processor.RegAcc.Value16;
						this.processor.RegAcc.Value16 = this.look.Direction;
						this.look.BestDirection = this.processor.RegAcc.Value16;
					}

					if (this.owner.MapOps.ContainsHut(this.owner.MapOps.ObtainTerrainType(nextHoriz, nextVert), nextHoriz, nextVert) != 0 && this.look.Role != 0)
						return this.look.Direction;
				}

				if (this.owner.World.RoundTally == 0 && EarthGame != 0)
				{
					this.look.BestScore = 0xf;
					this.look.BestDirection = 0;
				}

				if ((short)this.look.BestScore >= 8 &&
					(short)(0xf - this.look.CityDistance) <= (short)this.look.BestScore &&
					this.look.MayStay == 0)
				{
					if (this.look.Role == 0)
						return this.look.BestDirection == 0 ? (ushort)0x62 : this.look.BestDirection;

					AttachStrategicLocation(participantIdx, this.look.Horiz, this.look.Vert, 0, 2);
				}
			}

			return VisitAsAmbassador(participantIdx) ? (ushort)0x62 : null;
		}

		/// <summary>
		/// A settler (role 0) that may move on, near the nearest city of the human player (distance 0x2a above 1), with the
		/// human player ahead in technology and land known around it (the layer 0x32 below)
		/// </summary>
		private bool VisitAsAmbassador(short participantIdx)
		{
			if (this.look.Role != 0 || this.owner.World.HardnessTier == 0)
				return false;

			ushort nearest = this.look.NearestCity;
			this.processor.RegSource.Value16 = nearest < 128 ? (ushort)this.owner.World.Towns[nearest].ParticipantIdx : (ushort)0xffff;
			short person = this.owner.World.PersonParticipantIdx;

			if ((ushort)person != this.processor.RegSource.Value16 || this.look.MayStay != 0 ||
				(short)this.look.CityDistance <= 1 || (ushort)participantIdx == this.processor.RegSource.Value16 ||
				this.owner.World.Participants[participantIdx].FoundScienceTally >= this.owner.World.Participants[person].FoundScienceTally)
			{
				return false;
			}

			this.look.Round = this.owner.VideoCard.ObtainDot(2, (short)this.look.Horiz + 0x50, (short)this.look.Vert + 0x32);

			return (short)this.processor.RegAcc.Value16 >= 9 &&
				(short)(0xe - this.look.CityDistance) <= (short)this.look.Round;
		}

		/// <summary>
		/// A nuclear weapon of a computer player (kind 0x19): the target is the city (0x34, score 0x28) of a player the
		/// player is at war with (bit 0x80 without peace) or much stronger than, without its own nuclear weapons, bigger than 4,
		/// scored by the enemy tiles around it (+1, own -2, others -99) and half its size, with an own city within 16 tiles; at
		/// score 10 and with at least two nuclear weapons the weapon is placed on a free tile next to it (the answer: the
		/// direction ^ 4), else 0x20. The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort AimNuclearWeapon(short participantIdx, short troopIdx)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];

			this.look.BestScore = 0xffff;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				Game.State.City town = this.owner.World.Towns[this.look.Harbour];

				// The score starts as the owner: a dead city or one with bit 0 of the second word is compared with that
				this.look.Round = (ushort)town.ParticipantIdx;

				if (town.ConditionMarker != 0xff && !town.Has(CityStructure.SdiDefense) && IsNuclearTarget(participantIdx, town))
					ScoreNuclearTarget(participantIdx, town);

				if ((short)this.look.Round > (short)this.look.BestScore && HasCityNear(participantIdx, town))
				{
					this.processor.RegAcc.Value16 = this.look.Round;
					this.look.BestScore = this.processor.RegAcc.Value16;
					this.processor.RegAcc.Value16 = this.look.Harbour;
					this.look.NearestCity = this.processor.RegAcc.Value16;
				}
			}

			if ((short)this.look.BestScore < 0xa || player.LiveTroops[25] <= 1)
				return 0x20;

			this.processor.RegAcc.Value16 = this.look.NearestCity;
			this.look.Harbour = this.processor.RegAcc.Value16;
			Game.State.City target = this.owner.World.Towns[this.look.Harbour];
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHoriz = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) +
					DirectionHoriz(direction));
				this.look.TileVert = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) +
					DirectionVert(direction));

				if (this.owner.MapOps.ObtainTileOwner((short)this.look.TileHoriz, (short)this.look.TileVert) != 0xffff)
					continue;

				this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, (short)this.look.Horiz, (short)this.look.Vert);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)this.look.TileHoriz);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)this.look.TileVert);
				this.owner.MapOps.SituateUnitOnTile(participantIdx, troopIdx, (short)this.look.TileHoriz, (short)this.look.TileVert);

				if (this.owner.World.Towns[this.look.NearestCity].ParticipantIdx == this.owner.World.PersonParticipantIdx)
					player.TouchParticipantTimeleft = -2;

				return (ushort)(this.look.Direction ^ 4);
			}

			return 0x20;
		}

		/// <summary>
		/// The owner of the city is an enemy (bit 0x80 without peace) and the city is bigger than 4, when the player is not
		/// much stronger (3 * its strength below twice the owner's), the owner is warned (bit 8), or the owner has no nuclear
		/// weapons and its strength is more than half the player's
		/// </summary>
		private bool IsNuclearTarget(short participantIdx, Game.State.City town)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];
			ushort owner = (ushort)town.ParticipantIdx;
			short ownerMight = (short)(this.owner.World.Participants[owner].MartialMight << 1);

			this.look.Heading = (ushort)player.MartialMight;

			bool weighed = (short)(3 * (short)this.look.Heading) < ownerMight || (player.Relations[owner] & 8) != 0;

			if (!weighed)
			{
				if (this.owner.World.Participants[this.look.Round].LiveTroops[25] != 0 ||
					(short)this.look.Heading >= ownerMight)
				{
					return false;
				}
			}

			return (player.Relations[this.look.Round] & 0x82) == 0x80 && town.TrueExtent > 4;
		}

		/// <summary>The score of a target: the tiles around it (+1 an enemy's, -2 own, -99 another player's) and half its size</summary>
		private void ScoreNuclearTarget(short participantIdx, Game.State.City town)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];

			this.look.Round = 0;

			for (this.look.Direction = 0; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHoriz = (ushort)(town.Place.X + DirectionHoriz(direction));
				this.look.TileVert = (ushort)(town.Place.Y + DirectionVert(direction));

				this.look.TileOwner = this.owner.MapOps.ObtainTileOwner((short)this.look.TileHoriz, (short)this.look.TileVert);
				ushort tileOwner = this.look.TileOwner;

				if (tileOwner == 0xffff)
					continue;

				short delta = (player.Relations[tileOwner] & 0x82) == 0x80 ? (short)1 : tileOwner == (ushort)participantIdx ? (short)-2 : (short)-0x63;
				this.look.Round = (ushort)(this.look.Round + delta);
			}

			this.look.Round = (ushort)(this.look.Round + town.TrueExtent / 2);
		}

		/// <summary>An own city of the player within 16 tiles of the target (0x3e), searched with 0x22</summary>
		private bool HasCityNear(short participantIdx, Game.State.City target)
		{
			this.look.CityNear = 0;

			for (this.look.CityLoop = 0; (short)this.look.CityLoop < 0x80;
				this.look.CityLoop = (ushort)(this.look.CityLoop + 1))
			{
				Game.State.City town = this.owner.World.Towns[this.look.CityLoop];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				if ((short)this.owner.Helpers.ObtainDistance(target.Place.X, town.Place.X, target.Place.Y, town.Place.Y) > 0x10)
					continue;

				this.look.CityNear = 1;
				break;
			}

			return this.look.CityNear != 0;
		}

		/// <summary>
		/// A bomber without fuel (byte 5) or a fighter without a destination and with fewer moves left than its range waits
		/// (0x68); a fighter with enough moves looks for a target, or a city to land in (0x20 without one). Null: the unit goes on.
		/// The locals of ShiftAIUnit are used as in the original (its frame is open).
		/// </summary>
		private ushort? PlanAirUnit(short participantIdx, short troopIdx)
		{
			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf &&
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) <= 0)
			{
				return 0x68;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xe ||
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				return null;
			}

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);
			short range = (short)(CPU.SignedQuotient((short)UnitKindWord(kind, UnitKindTable.ShiftTally), 2) * 3);

			if (range >= this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps))
				return 0x68;

			return SendFighter(participantIdx, troopIdx);
		}

		/// <summary>
		/// The nearest bomber of the human player the fighter's player has seen (0x4, distance 0x14): within the fighter's
		/// moves it is the destination; else the own city within reach nearest to that bomber (0x34), or 0x20 without one
		/// </summary>
		private ushort? SendFighter(short participantIdx, short troopIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;

			this.look.BestDistance = 0x3e7;
			this.look.NearestUnit = 0xffff;
			this.look.Direction = 0;

			do
			{
				short bomberIdx = (short)this.look.Direction;

				if (this.troops.Pull(person, bomberIdx, UnitMemory.Kind) == 0xf &&
					((1 << (byte)participantIdx) & this.troops.PullSigned(person, bomberIdx, UnitMemory.ShownToParticipants)) != 0)
				{
					short distance = (short)this.owner.Helpers.ObtainDistance(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(person, bomberIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), this.troops.PullSigned(person, bomberIdx, UnitMemory.Vert));
					this.look.HarbourDistance = (ushort)distance;

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.processor.RegAcc.Value16 = this.look.HarbourDistance;
						this.look.BestDistance = this.processor.RegAcc.Value16;
						this.processor.RegAcc.Value16 = this.look.Direction;
						this.look.NearestUnit = this.processor.RegAcc.Value16;
					}
				}

				this.look.Direction = (ushort)(this.look.Direction + 1);
			}
			while ((short)this.look.Direction < 0x80);

			// The last distance measured, as in the original (not the nearest one)
			if (this.look.NearestUnit == 0xffff || (short)this.look.HarbourDistance <= 1)
				return null;

			short bomber = (short)this.look.NearestUnit;

			if ((short)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) >
				(short)this.look.BestDistance)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.troops.Pull(person, bomber, UnitMemory.Horiz));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.troops.Pull(person, bomber, UnitMemory.Vert));
				return null;
			}

			this.look.BestDistance = 0x3e7;
			this.look.NearestCity = 0xffff;

			for (this.look.Direction = 0; (short)this.look.Direction < 0x80;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				Game.State.City town = this.owner.World.Towns[this.look.Direction];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				short reach = (short)this.owner.Helpers.ObtainDistance(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), town.Place.Y);

				if ((short)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) < reach)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(
					this.troops.PullSigned(person, (short)this.look.NearestUnit, UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(person, (short)this.look.NearestUnit, UnitMemory.Vert), town.Place.Y);
				this.look.HarbourDistance = (ushort)distance;

				if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
				{
					this.processor.RegAcc.Value16 = this.look.HarbourDistance;
					this.look.BestDistance = this.processor.RegAcc.Value16;
					this.processor.RegAcc.Value16 = this.look.Direction;
					this.look.NearestCity = this.processor.RegAcc.Value16;
				}
			}

			if (this.look.NearestCity == 0xffff)
				return 0x20;

			Game.State.City landing = this.owner.World.Towns[this.look.NearestCity];
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)landing.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)landing.Place.Y);

			return null;
		}

		/// <summary>
		/// The surroundings of the unit in the locals of ShiftAIUnit: may it stay (0x4c), its place (0x30, 0x36), kind (0x8),
		/// the nearest city (0x34, distance 0x2a) and unit (0x4, distance 0x32; the unit is hidden while looking), the terrain
		/// (0x42), its role (0x56), continent (0x1a) and the approach there (0x2), the player's bit (0x50)
		/// </summary>
		private void SurveyAIUnit(short participantIdx, short troopIdx)
		{
			this.look.MayStay = this.owner.UnitOps.MayOccupyTile(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			this.look.BestDirection = 0;
			this.look.Horiz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.look.Vert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			this.look.Kind = (ushort)this.troops.TroopKind(participantIdx, troopIdx);

			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			this.look.NearestCity = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
			this.look.CityDistance = FoundDistance;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.look.NearestUnit = this.owner.Helpers.LocateNearestUnit(participantIdx, troopIdx, horizLoc, vertLoc);
			this.look.UnitDistance = FoundDistance;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)this.look.Kind);

			this.look.Terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.look.Role = UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup);

			this.look.Continent = this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
			this.look.Approach = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.look.Continent].Approach;
			this.look.PlayerBit = (ushort)(1 << (byte)participantIdx);

			// The registers as the original leaves them for the code that follows
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
			this.processor.RegAcc.Value16 = (ushort)(1 << (byte)participantIdx);
		}

		/// <summary>
		/// Sets up the strategic locations of the player again: its 32 entries at 0xe2c2 + 32 * player become empty (0xff, with
		/// 0 at 0xb2be + ...), then a unit is sent to each of its 16 locations that is in use with a state up to 2
		/// </summary>
		public void ReimposeStrategicLocations(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ReimposeStrategicLocations({participantIdx})");

			for (int entryIdx = 0; entryIdx < 0x20; entryIdx++)
			{
				ushort entry = (ushort)((participantIdx << 5) + entryIdx);

				this.processor.EmitGlobalByte((ushort)(entry + 0xe2c2), 0xff);
				this.processor.EmitGlobalByte((ushort)(entry + 0xb2be), 0x0);
			}

			for (int siteIdx = 0; siteIdx < 0x10; siteIdx++)
			{
				StrategicLocation site = this.owner.World.Participants[participantIdx].TacticalSites[siteIdx];

				if (site.Live != -1 && site.Live <= 2)
				{
					SendUnitToStrategicLocation(participantIdx, (short)site.Place.X, (short)site.Place.Y, site.Live, site.Strategy);
				}
			}

			this.processor.Journal.CloseScope("ReimposeStrategicLocations");
		}

		/// <summary>
		/// Puts the place into the list of strategic locations of the player (32 entries a player in the byte arrays at 0xe598
		/// horizontal, 0xe798 vertical, 0xe2c2 role, 0xb2be strategy), unless it is there already for the role with no higher
		/// strategy. On the turn of an AI player (0x6b90), for the roles 3 and 4 the first unit of the role on the same continent
		/// with enough moves left (twice the distance) goes there. The place takes the first free entry or the first one with a
		/// lower strategy (the rest move one on); with none, it is not listed.
		/// </summary>
		public void SendUnitToStrategicLocation(short participantIdx, short horizLoc, short vertLoc, short live, short strategy)
		{
			this.processor.Journal.OpenScope($"SendUnitToStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy})");

			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);

				if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xe598)) == horizLoc &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xe798)) == vertLoc &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xe2c2)) == live &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xb2be)) <= strategy)
				{
					this.processor.Journal.CloseScope("SendUnitToStrategicLocation");
					return;
				}
			}

			if (participantIdx == (short)TurnPlayer && participantIdx != this.owner.World.PersonParticipantIdx &&
				(live == 3 || live == 4))
			{
				SendUnitOfRole(participantIdx, horizLoc, vertLoc, live);
			}

			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);

				if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(place + 0xe2c2)) != 0xff &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xb2be)) >= strategy)
					continue;

				FreeStrategicLocation(participantIdx, (ushort)placeIdx);

				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + 0xe598), (byte)horizLoc);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + 0xe798), (byte)vertLoc);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + 0xe2c2), (byte)live);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + 0xb2be), (byte)strategy);
				break;
			}

			// Far return
			this.processor.Journal.CloseScope("SendUnitToStrategicLocation");
		}

		/// <summary>
		/// The first unit of the role with moves left, on the continent of the place and with at least twice the distance in
		/// moves, gets the place as its goal (status 0x10, no orders)
		/// </summary>
		private void SendUnitOfRole(short participantIdx, short horizLoc, short vertLoc, short live)
		{
			for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff ||
					this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0 ||
					this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) != (ushort)live)
					continue;

				sbyte troopHoriz = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
				sbyte troopVert = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

				if (this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc) != this.owner.MapOps.ObtainContinentID(troopHoriz, troopVert))
					continue;

				int distance = this.owner.Helpers.ObtainDistance(troopHoriz, horizLoc, troopVert, vertLoc);

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < (short)(distance << 1))
					continue;

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)horizLoc);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)vertLoc);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
					(byte)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0x10) & 0xf0));
				return;
			}
		}

		/// <summary>
		/// Makes room at the place of the list of the player (32 entries a player in the byte arrays at 0xb2be, 0xe2c2, 0xe598
		/// and 0xe798): the used entries from the place up to 30 move one place on
		/// </summary>
		public void FreeStrategicLocation(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"FreeStrategicLocation({participantIdx}, {arg2})");

			for (int idx = 30; idx >= (short)arg2; idx--)
			{
				int entry = (participantIdx << 5) + idx;

				if (this.processor.PullGlobalByte((ushort)(0xb2be + entry)) == 0)
					continue;

				MoveListEntryOn(0xe598, entry);
				MoveListEntryOn(0xe798, entry);
				MoveListEntryOn(0xe2c2, entry);
				MoveListEntryOn(0xb2be, entry);
			}

			this.processor.Journal.CloseScope("FreeStrategicLocation");
		}

		private void MoveListEntryOn(ushort origin, int entry)
		{
			this.processor.EmitGlobalByte((ushort)(origin + entry + 1), this.processor.PullGlobalByte((ushort)(origin + entry)));
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void WipeStrategicLocations(short participantIdx)
		{
			for (int idx = 0; idx < 16; idx++)
			{
				this.owner.World.Participants[participantIdx].TacticalSites[idx].Live = -1;
				this.owner.World.Participants[participantIdx].TacticalSites[idx].Strategy = 0;
			}
		}

		/// <summary>
		/// Notes a strategic location of the player (the 16 places are sorted by strategy, highest first): nothing when it is
		/// already there with the same state and at least the strategy; otherwise it goes in before the first free place or
		/// the first place with a lower strategy (AffixStrategicLocation moves the rest down); no room, no change
		/// </summary>
		public void AttachStrategicLocation(short participantIdx, ushort horizLoc, ushort vertLoc, ushort live, ushort strategy)
		{
			this.processor.Journal.OpenScope($"AttachStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			bool known = sites.Take(0x10).Any(site => (ushort)(sbyte)site.Place.X == horizLoc && (ushort)(sbyte)site.Place.Y == vertLoc &&
				(ushort)site.Live == live && (sbyte)site.Strategy <= (short)strategy);

			if (!known)
			{
				for (ushort place = 0; place < 0x10; place++)
				{
					if (sites[place].Live == -1 || (sbyte)sites[place].Strategy < (short)strategy)
					{
						AffixStrategicLocation(participantIdx, place);

						sites[place].Place.X = (sbyte)horizLoc;
						sites[place].Place.Y = (sbyte)vertLoc;
						sites[place].Live = (sbyte)live;
						sites[place].Strategy = (byte)strategy;
						break;
					}
				}
			}

			this.processor.Journal.CloseScope("AttachStrategicLocation");
		}

		/// <summary>
		/// Makes room at the place of the strategic locations of the player: the used ones from the place up to 14 move one place on
		/// </summary>
		public void AffixStrategicLocation(short participantIdx, ushort place)
		{
			this.processor.Journal.OpenScope($"AffixStrategicLocation({participantIdx}, {place})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 14; idx >= (short)place; idx--)
			{
				if (sites[idx].Strategy == 0)
					continue;

				sites[idx + 1].Place.X = sites[idx].Place.X;
				sites[idx + 1].Place.Y = sites[idx].Place.Y;
				sites[idx + 1].Live = sites[idx].Live;
				sites[idx + 1].Strategy = sites[idx].Strategy;
			}

			this.processor.Journal.CloseScope("AffixStrategicLocation");
		}

		/// <summary>
		/// The strategic locations of the player of the kind (Live = arg2) not farther than arg5 from the position are given up
		/// (Live = -1)
		/// </summary>
		public void DisableStrategicLocation(short participantIdx, ushort arg2, int horizLoc, int vertLoc, ushort arg5)
		{
			this.processor.Journal.OpenScope($"DisableStrategicLocation({participantIdx}, {arg2}, {horizLoc}, {vertLoc}, {arg5})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 0; idx < 16; idx++)
			{
				if ((ushort)sites[idx].Live != arg2)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, sites[idx].Place.X, vertLoc, sites[idx].Place.Y);

				if (distance <= (short)arg5)
				{
					sites[idx].Live = -1;
				}
			}

			this.processor.Journal.CloseScope("DisableStrategicLocation");
		}

		/// <summary>
		/// The cities of the player choose their production again (PickNewCityProduction): all of them, or with arg2 not 0xffff
		/// only the cities on that continent
		/// </summary>
		public void RefreshCityProduction(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"RefreshCityProduction({participantIdx}, {arg2})");

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if ((byte)town.ParticipantIdx != (ushort)participantIdx || town.ConditionMarker == 0xff)
					continue;

				if (arg2 != 0xffff && this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) != arg2)
					continue;

				PickNewCityProduction(townIdx);
			}

			this.processor.Journal.CloseScope("RefreshCityProduction");
		}

		/// <summary>
		/// The city chooses its production again (ScienceAndAdvisors.PickCityProduction); the count of the units of each type
		/// its player is making (TroopsInsideMaking) follows the change
		/// </summary>
		public void PickNewCityProduction(short townIdx)
		{
			this.processor.Journal.OpenScope($"PickNewCityProduction({townIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			City town = this.owner.World.Towns[townIdx];
			ushort participantIdx = (byte)town.ParticipantIdx;

			this.owner.World.Participants[participantIdx].StopMaking(town.PresentMakingIdx);
			town.PresentMakingIdx = (sbyte)this.owner.Advisors.PickCityProduction((short)participantIdx, townIdx);
			this.owner.World.Participants[participantIdx].StartMaking(town.PresentMakingIdx);

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;

			this.processor.Journal.CloseScope("PickNewCityProduction");
		}

		/// <summary>
		/// The direction (0 - 8, 0 is the tile itself) of the ocean tile at or around (horizLoc, vertLoc), without units
		/// unless it is the tile itself, with the most empty land tiles around it (with relations bit 2 towards the human
		/// player, land of the human player, map layer 2 at column + 80, does not count); -1 without such an ocean tile
		/// </summary>
		public ushort MayAIEnterTile(short participantIdx, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"MayAIEnterTile({participantIdx}, {horizLoc}, {vertLoc})");

			short bestHeading = -1;
			short bestShore = -1;
			MapLayers chart = this.owner.MapOps.Chart;

			for (short heading = 0; heading <= 8; heading++)
			{
				short oceanHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
				short oceanVert = (short)(chart.DirectionVert(heading) + vertLoc);

				if (this.owner.MapOps.ObtainTerrainType(oceanHoriz, oceanVert) != MapRules.OceanTerrain)
					continue;

				if (heading != 0 && this.owner.MapOps.ObtainTileOwner(oceanHoriz, oceanVert) != 0xffff)
					continue;

				short shore = 0;

				for (int around = 1; around <= 8; around++)
				{
					short landHoriz = (short)(chart.DirectionHoriz(around) + oceanHoriz);
					short landVert = (short)(chart.DirectionVert(around) + oceanVert);

					if (this.owner.MapOps.ObtainTerrainType(landHoriz, landVert) == MapRules.OceanTerrain ||
						this.owner.MapOps.ObtainTileOwner(landHoriz, landVert) != 0xffff)
						continue;

					shore++;

					if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0 &&
						this.owner.VideoCard.ObtainDot(2, landHoriz + 0x50, landVert) == (ushort)this.owner.World.PersonParticipantIdx)
					{
						shore--;
					}
				}

				if (shore > bestShore)
				{
					bestShore = shore;
					bestHeading = heading;
				}
			}

			this.processor.RegAcc.Value16 = (ushort)bestHeading;
			this.processor.Journal.CloseScope("MayAIEnterTile");

			return (ushort)bestHeading;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort ManageBarbarianUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ManageBarbarianUnit({participantIdx}, {troopIdx})");

			this.processor.EnterFrame(0x54);
			this.processor.PreserveIndexRegisters();

			ushort dataSeg = this.processor.SegData.Value16;
			ushort unitAddr = (ushort)((ushort)participantIdx * 0x600 + (ushort)troopIdx * 0xc);
			byte unitYByte = this.Troops.PullAt(unitAddr, UnitMemory.Vert);
			if (unitYByte < 0x2 || unitYByte >= 0x30)
			{
				this.processor.RegAcc.Value16 = (ushort)(((0x600 * (ushort)participantIdx) & 0xff00) | unitYByte);
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
				this.processor.RestoreIndexRegisters();
				this.processor.LeaveFrame();
				this.processor.Journal.CloseScope("ManageBarbarianUnit");
				return this.processor.RegAcc.Value16;
			}

			ushort mayOccupyCurrent = this.owner.UnitOps.MayOccupyTile(
				participantIdx,
				(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz),
				(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Vert));
			ushort unitX = this.Troops.PullAt(unitAddr, UnitMemory.Horiz);
			ushort unitY = this.Troops.PullAt(unitAddr, UnitMemory.Vert);
			byte unitKind = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
			ushort unitSpecOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)unitKind);

			if (UnitKindWordAt(unitSpecOffset, UnitKindTable.TroopGroup) == 0x5)
			{
				ushort seaResult = this.ManageBarbarianSeaTransport(participantIdx, troopIdx, unitAddr, unitX, unitY);
				this.processor.RegAcc.Value16 = seaResult;
				this.processor.RestoreIndexRegisters();
				this.processor.LeaveFrame();
				this.processor.Journal.CloseScope("ManageBarbarianUnit");
				return seaResult;
			}

			ushort landResult = this.ManageBarbarianFieldUnit(participantIdx, troopIdx, unitAddr, unitX, unitY, mayOccupyCurrent);
			this.processor.RegAcc.Value16 = landResult;
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("ManageBarbarianUnit");
			return landResult;
		}

		private ushort ManageBarbarianSeaTransport(short participantIdx, short troopIdx, ushort unitAddr, ushort unitX, ushort unitY)
		{
			ushort dataSeg = this.processor.SegData.Value16;
			if (this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack) == 0xff)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
				return 0x20;
			}

			ushort nearestCity = this.owner.Helpers.LocateNearestCity((short)unitX, (short)unitY);
			for (ushort dir = 1; (short)dir <= 8; dir++)
			{
				ushort dirOffset = (ushort)(dir << 1);
				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					(short)DirectionHorizAt(dirOffset) + (short)unitX);
				ushort adjY = (ushort)(DirectionVertAt(dirOffset) + unitY);

				if ((short)FoundDistance <= 8 &&
					this.owner.MapOps.ObtainTerrainType((short)adjX, (short)adjY) != 0xa &&
					((byte)this.owner.MapOps.ObtainTileImprovements((short)adjX, (short)adjY) & 0x1) == 0 &&
					(ushort)(this.owner.MapOps.ObtainTileOwner((short)adjX, (short)adjY) + 1) == 0 &&
					(short)adjY > 1 &&
					(short)adjY < 0x30)
				{
					short sightMask = (short)(sbyte)(byte)this.owner.World.ChartSight[adjX, adjY];
					ushort humanBit = (ushort)(1 << (byte)(this.owner.World.PersonParticipantIdx & 0xff));
					if ((sightMask & (short)humanBit) != 0 &&
						this.owner.World.Towns[nearestCity].ParticipantIdx == this.owner.World.PersonParticipantIdx)
					{
						ushort cityContinent = this.owner.MapOps.ObtainContinentID(
							this.owner.World.Towns[nearestCity].Place.X,
							this.owner.World.Towns[nearestCity].Place.Y);
						ushort adjContinent = this.owner.MapOps.ObtainContinentID((short)adjX, (short)adjY);
						if (adjContinent == cityContinent)
						{
							byte humanBitByte = (byte)(1 << (byte)(this.owner.World.PersonParticipantIdx & 0xff));
							this.Troops.AssignAt(unitAddr, UnitMemory.ShownToParticipants, (byte)(this.Troops.PullAt(unitAddr, UnitMemory.ShownToParticipants) | humanBitByte));

							this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx, (short)adjX, (short)adjY);
							DialogStyle = 0x3;
							this.owner.Words.Set("Barbarian raiding party\nlands near ");
							nearestCity = this.owner.Helpers.LocateNearestCity((short)adjX, (short)adjY);
							this.owner.Conquest.ObtainCityLabel((short)nearestCity);
							this.owner.Words.Append("!\nCitizens are alarmed.\n");
							this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 32);
						}
					}

					byte tileSight = (byte)this.owner.World.ChartSight[adjX, adjY];
					this.Troops.AssignAt(unitAddr, UnitMemory.ShownToParticipants, (byte)(this.Troops.PullAt(unitAddr, UnitMemory.ShownToParticipants) | tileSight));
					this.Troops.AssignAt(unitAddr, UnitMemory.LeftoverSteps, 0x0);
					return 0x75;
				}

				if ((short)this.owner.MapOps.ObtainTileOwner((short)adjX, (short)adjY) > 0)
				{
					return dir;
				}
			}

			if (this.Troops.PullAt(unitAddr, UnitMemory.TravelHoriz) == 0xff)
			{
				short bestScore = 0;
				ushort bestCity = 0;
				for (ushort cityIdx = 0; cityIdx < 0x80; cityIdx++)
				{
					if (this.owner.World.Towns[cityIdx].ConditionMarker == 0xff)
					{
						continue;
					}

					ushort dist = (ushort)(this.owner.Helpers.ObtainDistance(
						(short)unitX,
						this.owner.World.Towns[cityIdx].Place.X,
						(short)unitY,
						this.owner.World.Towns[cityIdx].Place.Y) + 1);
					ushort plunder = this.owner.Conquest.ComputeCityPlunder((short)cityIdx);
					short score = CPU.SignedQuotient((short)(plunder + 0x32), (short)dist);
					if (score > bestScore)
					{
						bestScore = score;
						bestCity = cityIdx;
					}
				}

				if (bestScore == 0)
				{
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					return 0x20;
				}

				this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, (byte)(sbyte)this.owner.World.Towns[bestCity].Place.X);
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelVert, (byte)(sbyte)this.owner.World.Towns[bestCity].Place.Y);
			}

			return 0;
		}

		private ushort ManageBarbarianFieldUnit(
			short participantIdx,
			short troopIdx,
			ushort unitAddr,
			ushort unitX,
			ushort unitY,
			ushort mayOccupyCurrent)
		{
			ushort dataSeg = this.processor.SegData.Value16;
			ushort nearestCity = this.owner.Helpers.LocateNearestCity((short)unitX, (short)unitY);
			if (FoundDistance == 0)
			{
				if (this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack) == 0xff)
				{
					return 0x66;
				}

				byte curKind = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
				ushort curSpecOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)curKind);
				if (UnitKindWordAt(curSpecOffset, UnitKindTable.TroopGroup) == 0x2)
				{
					return 0x66;
				}
			}

			ushort targetDirX = 0xfffe;
			ushort targetDirY = 0xfffe;
			if (((byte)((byte)(sbyte)troopIdx + (byte)(this.owner.World.RoundTally & 0xff)) & 0x3) == 0)
			{
				ushort cityContinent = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[nearestCity].Place.X,
					this.owner.World.Towns[nearestCity].Place.Y);
				ushort unitContinent = this.owner.MapOps.ObtainContinentID((short)unitX, (short)unitY);
				if (unitContinent != cityContinent || (short)FoundDistance > 8)
				{
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					return 0x20;
				}
			}

			if (nearestCity != 0xffff)
			{
				ushort cityContinent = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[nearestCity].Place.X,
					this.owner.World.Towns[nearestCity].Place.Y);
				ushort unitContinent = this.owner.MapOps.ObtainContinentID((short)unitX, (short)unitY);
				if (unitContinent == cityContinent)
				{
					ushort ownerMask = (ushort)(1 << (byte)this.owner.World.Towns[nearestCity].ParticipantIdx);
					if ((ownerMask & (ushort)this.owner.World.ParticipantMarkers) != 0)
					{
						targetDirX = (ushort)Math.Sign((short)((short)this.owner.World.Towns[nearestCity].Place.X - (short)unitX));
						targetDirY = (ushort)Math.Sign((short)((short)this.owner.World.Towns[nearestCity].Place.Y - (short)unitY));
					}
				}
			}

			ushort cityOwner = (ushort)this.owner.World.Towns[nearestCity].ParticipantIdx;
			if ((ushort)participantIdx == cityOwner ||
				(this.owner.World.Participants[cityOwner].TownTally < 2 && this.owner.World.Participants[cityOwner].Gold < 100))
			{
				targetDirX = 0xfffe;
				targetDirY = 0xfffe;
			}

			if (targetDirX != 0xfffe)
			{
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, (byte)(sbyte)this.owner.World.Towns[nearestCity].Place.X);
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelVert, (byte)(sbyte)this.owner.World.Towns[nearestCity].Place.Y);
				ushort gotoDir = this.owner.Routing.CarryOutGoto(participantIdx, troopIdx);
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelHeading, (byte)(gotoDir & 0xff));
			}

			this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, 0xff);
			if (this.Troops.PullAt(unitAddr, UnitMemory.Kind) == 0x1a)
			{
				ushort currentTerrain = this.owner.MapOps.ObtainTerrainType(
					(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz),
					(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Vert));
				byte nextInStack = this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack);
				if (nextInStack != 0xff && currentTerrain != 0xa)
				{
					ushort nextUnitOffset = (ushort)((short)(sbyte)0xc * (short)(sbyte)nextInStack);
					ushort nextUnitAddr = (ushort)((ushort)participantIdx * 0x600 + nextUnitOffset);
					if (this.Troops.PullAt(nextUnitAddr, UnitMemory.Kind) != 0x1a)
					{
						return 0x20;
					}
				}

				short cityDist = (short)FoundDistance;
				if (currentTerrain == 0xa)
				{
					if (cityDist < 3)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						return 0x20;
					}
				}
				else
				{
					ushort nearestUnit = this.owner.Helpers.LocateNearestUnit(
						participantIdx,
						troopIdx,
						(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz),
						(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Vert));
					if ((short)FoundDistance <= 3)
					{
						ushort nearestUnitAddr = (ushort)((ushort)participantIdx * 0x600 + nearestUnit * 0xc);
						if (this.Troops.PullAt(nearestUnitAddr, UnitMemory.Kind) != 0x1a)
						{
							this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, this.Troops.PullAt(nearestUnitAddr, UnitMemory.Horiz));
							this.Troops.AssignAt(unitAddr, UnitMemory.TravelVert, this.Troops.PullAt(nearestUnitAddr, UnitMemory.Vert));
							return 0;
						}
					}

					short surviveDist = (short)(((ushort)this.owner.World.RoundTally & 0x7) + 4);
					if (surviveDist <= cityDist)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						return 0x20;
					}
				}
			}

			ushort bestDir = 0;
			short bestDirScore = unchecked((short)0xfc19);
			for (ushort dir = 1; (short)dir < 9; dir++)
			{
				ushort dirOffset = (ushort)(dir << 1);
				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz) +
					(short)DirectionHorizAt(dirOffset));
				ushort adjY = (ushort)(this.Troops.PullAt(unitAddr, UnitMemory.Vert) +
					DirectionVertAt(dirOffset));

				ushort adjTerrain = this.owner.MapOps.ObtainTerrainType((short)adjX, (short)adjY);
				ushort isOcean = adjTerrain == 0xa ? (ushort)1 : (ushort)0;

				byte curKind = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
				ushort curSpecOff = (ushort)((short)(sbyte)0x22 * (short)(sbyte)curKind);
				ushort isSeaDomain = UnitKindWordAt(curSpecOff, UnitKindTable.LandGroup) == 0x2 ? (ushort)1 : (ushort)0;
				if (isSeaDomain != isOcean)
				{
					continue;
				}

				if (this.owner.MapOps.VerifyMapLimits((short)adjX, (short)adjY) == 0)
				{
					continue;
				}

				ushort unitAtAdj = this.owner.MapOps.ObtainUnitAtTile((short)adjX, (short)adjY);
				short dirScore = 0;

				if (this.Troops.PullAt(unitAddr, UnitMemory.Kind) == 0x1a)
				{
					if (unitAtAdj != 0xffff)
					{
						ushort unitOwnerAtAdj = FoundPlayer;
						dirScore = (short)(dirScore + ((ushort)participantIdx == unitOwnerAtAdj ? (short)0x63 : unchecked((short)0xff9d)));
					}

					this.owner.Helpers.LocateNearestCity((short)adjX, (short)adjY);
					short randBonus = (short)this.owner.Libc.Dice.DrawInt(4);
					short defenseBonus = (short)(sbyte)TerrainField(adjTerrain, TerrainTable.MoveCost);
					short distPenalty = (short)(FoundDistance << 2);
					dirScore = (short)(dirScore + defenseBonus + randBonus + distPenalty);
				}
				else
				{
					ushort adjImprovements = this.owner.MapOps.ObtainTileImprovements((short)adjX, (short)adjY);
					if (((byte)adjImprovements & 0x1) != 0)
					{
						ushort adjOwnerBits = this.owner.MapOps.ObtainTileOwnerBits((short)adjX, (short)adjY);
						if (adjOwnerBits != (ushort)participantIdx)
						{
							if (this.owner.MapOps.ObtainTerrainType((short)unitX, (short)unitY) != 0xa)
							{
								return dir;
							}

							continue;
						}
					}

					ushort randRoll = (ushort)this.owner.Libc.Dice.DrawInt(6);
					ushort impCheck = this.owner.MapOps.ObtainTileImprovements((short)adjX, (short)adjY);
					ushort impBonus = ((byte)impCheck & 0x6) != 0 ? (ushort)0x6 : (ushort)0;
					ushort ownerBits = this.owner.MapOps.ObtainTileOwnerBits((short)adjX, (short)adjY);
					ushort ownerBonus = ownerBits == (ushort)participantIdx ? (ushort)0 : (ushort)0x4;
					dirScore = (short)(ownerBonus + impBonus + randRoll);

					if (unitAtAdj != 0xffff)
					{
						ushort adjUnitOwner = FoundPlayer;
						if ((ushort)participantIdx == adjUnitOwner)
						{
							if (this.owner.MapOps.ObtainTerrainType((short)unitX, (short)unitY) != 0xa)
							{
								continue;
							}

							dirScore = (short)(dirScore - 0x14);
						}
						else
						{
							if (this.owner.MapOps.ObtainTerrainType((short)unitX, (short)unitY) == 0xa)
							{
								continue;
							}

							ushort adjUnitAddr = (ushort)(adjUnitOwner * 0x600 + unitAtAdj * 0xc);
							if (this.Troops.PullAt(adjUnitAddr, UnitMemory.Kind) != 0x1a)
							{
								dirScore = (short)(dirScore + 0x63);
							}
						}
					}

					if (mayOccupyCurrent != 0)
					{
						byte kindNow = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
						ushort specNow = (ushort)((short)(sbyte)0x22 * (short)(sbyte)kindNow);
						if (UnitKindWordAt(specNow, UnitKindTable.LandGroup) == 0 &&
							unitAtAdj == 0xffff &&
							this.owner.UnitOps.MayOccupyTile(participantIdx, (short)adjX, (short)adjY) != 0)
						{
							continue;
						}
					}

					short prevDir = (short)(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.TravelHeading);
					if ((ushort)prevDir == dir)
					{
						dirScore = (short)(dirScore + 0x6);
					}

					if (DirectionHorizAt(dirOffset) == targetDirX)
					{
						dirScore = (short)(dirScore + 0x2);
					}

					if (DirectionVertAt(dirOffset) == targetDirY)
					{
						dirScore = (short)(dirScore + 0x2);
					}
				}

				if (dirScore > bestDirScore)
				{
					bestDirScore = dirScore;
					bestDir = dir;
				}
			}

			ushort curImprovements = this.owner.MapOps.ObtainTileImprovements((short)unitX, (short)unitY);
			if (((byte)curImprovements & 0x1) != 0 &&
				this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack) == 0xff &&
				bestDirScore < 0x63)
			{
				return 0x20;
			}

			this.Troops.AssignAt(unitAddr, UnitMemory.TravelHeading, (byte)(bestDir & 0xff));
			return bestDir;
		}

		/// <summary>
		/// Sends to the city the loaded transports at sea of the players 1 - 7 (not the human player, and only those without
		/// relations bit 2 towards the human player) that are closer to it than 3 turns of moves
		/// </summary>
		public void PostCityDefenders(short townIdx)
		{
			this.processor.Journal.OpenScope($"PostCityDefenders({townIdx})");

			short townHoriz = (short)this.owner.World.Towns[townIdx].Place.X;
			short townVert = (short)this.owner.World.Towns[townIdx].Place.Y;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx ||
					(this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
					continue;

				for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx) ||
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ||
						this.troops.Following(participantIdx, troopIdx) == -1)
						continue;

					sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
					sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

					if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain)
						continue;

					int distance = this.owner.Helpers.ObtainDistance(horizLoc, townHoriz, vertLoc, townVert);

					if ((short)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)) > distance)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)townHoriz);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)townVert);
					}
				}
			}

			this.processor.Journal.CloseScope("PostCityDefenders");
		}
	}
}
