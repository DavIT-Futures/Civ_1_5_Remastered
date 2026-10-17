using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x25fb.
	/// </summary>
	public class AIPlayer
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public AIPlayer(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
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

			// The locals stay in the frame and get every value the original gives them: functions called later from the same
			// depth may read what is left there
			this.processor.EnterFrame(0x68);
			this.processor.PreserveIndexRegisters();

			CountUnits(participantIdx);
			CountCities(participantIdx);
			CountUsedBlocks(participantIdx);
			ChooseContinentApproaches(participantIdx);

			if (participantIdx != 0 && (this.owner.World.ParticipantMarkers & (1 << participantIdx)) == 0)
			{
				Player player = this.owner.World.Participants[participantIdx];

				if (player.ColonistTally == 0 && player.TownTally == 0 && participantIdx != this.owner.World.PersonParticipantIdx)
					this.owner.NewGameWizard.EliminateCivilization(participantIdx, 0);

				if ((((byte)participantIdx + (byte)this.owner.World.RoundTally) & 7) == 0)
					ChooseGovernment(participantIdx);

				DirectUnits(participantIdx);
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("OperateAITurn");
		}

		// Locals of OperateAITurn (distances below bp)
		private const int SurveyBase = 0x4;
		private const int SurveyLocations = 0x44;
		private const int SurveyDistance = 0x46;
		private const int SurveyMostTowns = 0x48;
		private const int SurveyBestDistance = 0x4a;
		private const int SurveyLoop2 = 0x4c;
		private const int SurveyStrength = 0x4e;
		private const int SurveyHoriz = 0x50;
		private const int SurveyVert = 0x52;
		private const int SurveyLoop = 0x54;
		private const int SurveyOuterLoop = 0x58;
		private const int SurveyContinent = 0x5a;
		private const int SurveyTowns = 0x5c;
		private const int SurveyThreats = 0x5e;
		private const int SurveyApproach = 0x60;
		private const int SurveyChoice = 0x62;
		private const int SurveyScratch = 0x64;
		private const int SurveyScratch2 = 0x66;
		private const int SurveyScratch3 = 0x68;
		private const int SurveyStronger = 0x2;

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

		/// <summary>The word of the table of the frame (bp - 0x44 + 2 * location): units sent to the location so far, plus the base</summary>
		private ushort LocationSentAddress(int locationIdx) => (ushort)(this.processor.RegFrame.Value16 + (locationIdx << 1) - SurveyLocations);

		/// <summary>
		/// Strength of the units of the player by continent, the units of the player (without the nuclear weapons), the
		/// settlers, the unit roles in the blocks; the mark 0x10 of a unit is set when it can't move (another player's unit
		/// next to it, or a land unit on the ocean)
		/// </summary>
		private void CountUnits(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];

			this.processor.EmitLocalWord(SurveyLoop, 0);

			for (ushort continentIdx = 0; continentIdx < 0x10; continentIdx++)
			{
				player.Landmasses[continentIdx].TownTally = 0;
				player.Landmasses[continentIdx].Guard = 0;
				player.Landmasses[continentIdx].Assault = 0;
				this.processor.EmitLocalWord(SurveyLoop, (ushort)(continentIdx + 1));
			}

			this.processor.EmitLocalWord(SurveyHoriz, 0);

			for (ushort blockHoriz = 0; blockHoriz < 0x14; blockHoriz++)
			{
				this.processor.EmitLocalWord(SurveyVert, 0);

				for (ushort blockVert = 0; blockVert < 0xd; blockVert++)
				{
					this.processor.EmitUInt8(BlockSegment, (ushort)(BlockOrigin + blockHoriz * 0xd + blockVert), 0);
					this.processor.EmitLocalWord(SurveyVert, (ushort)(blockVert + 1));
				}

				this.processor.EmitLocalWord(SurveyHoriz, (ushort)(blockHoriz + 1));
			}

			player.MartialMight = 0;
			player.ColonistTally = 0;
			player.TroopTally = 0;

			this.processor.EmitLocalWord(SurveyLoop, 0);

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);
				this.processor.EmitLocalByte(SurveyScratch, kind);

				if (kind != 0xff && kind != 0x19)
				{
					if (kind == 0)
						player.ColonistTally++;
					else
						player.TroopTally++;

					CountUnit(participantIdx, troopIdx);
				}

				this.processor.EmitLocalWord(SurveyLoop, (ushort)(troopIdx + 1));
			}
		}

		private void CountUnit(short participantIdx, short troopIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			player.MartialMight += (short)(UnitKindTable.Field(this.processor, kind, UnitKindTable.AssaultPower) +
				UnitKindTable.Field(this.processor, kind, UnitKindTable.GuardPower));

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
				this.processor.EmitLocalWord(SurveyContinent, continentIdx);

				kind = this.troops.TroopKind(participantIdx, troopIdx);
				this.processor.EmitLocalWord(SurveyScratch, (ushort)((participantIdx << 5) + (continentIdx << 1)));

				player.Landmasses[continentIdx].Assault += (short)UnitKindTable.Field(this.processor, kind, UnitKindTable.GuardPower);
				player.Landmasses[continentIdx].Guard += (short)UnitKindTable.Field(this.processor, kind, UnitKindTable.AssaultPower);
			}
			else if (UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				// A land unit at sea (on a ship)
				status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			}

			// The role of the unit (0 - 6) as a bit of its block
			byte role = (byte)UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup);
			byte roleBit = (byte)(2 << role);
			ushort block = Block(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
			this.processor.EmitUInt8(BlockSegment, block, (byte)(this.processor.PullUInt8(BlockSegment, block) | roleBit));
		}

		/// <summary>
		/// The cities of the player by continent, their total size and the units in production; the units known to be in
		/// the cities of the player; the cities, own and others', where a unit is sent (strategic locations)
		/// </summary>
		private void CountCities(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];

			// The base of the weight of a strategic location: an eighth of the units, 3 - 99
			this.owner.Helpers.VerifyValueSpan(player.TroopTally / 8, 3, 99);
			this.processor.EmitLocalWord(SurveyBase, this.processor.RegAcc.Value16);
			player.GrandTownExtent = 0;
			player.TownTally = 0;

			this.processor.EmitLocalWord(SurveyLoop, 0);

			for (int kindIdx = 0; kindIdx < 0x1c; kindIdx++)
			{
				player.TroopsInsideMaking[kindIdx] = 0;
				this.processor.EmitLocalWord(SurveyLoop, (ushort)(kindIdx + 1));
			}

			this.processor.EmitLocalWord(SurveyLoop, 0);

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
							(short)((town.StructureMarkers0 & 0x80) != 0 ? 3 : 5));
					}

					// A city without a unit and without known units: one unit to defend it (own) or to take it
					if (this.owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y) == 0xffff && (byte)town.Unfamiliar[0] == 0xff)
					{
						SendUnitToStrategicLocation(participantIdx, (short)town.Place.X, (short)town.Place.Y, 2,
							(short)(town.ParticipantIdx == participantIdx ? 4 : 2));
					}
				}

				this.processor.EmitLocalWord(SurveyLoop, (ushort)(townIdx + 1));
			}
		}

		private void CountOwnCity(short participantIdx, short townIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];
			City town = this.owner.World.Towns[townIdx];

			player.TownTally++;
			player.Landmasses[this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y)].TownTally++;
			player.GrandTownExtent += town.TrueExtent;

			if (town.PresentMakingIdx >= 0)
				player.TroopsInsideMaking[town.PresentMakingIdx]++;

			this.processor.EmitLocalWord(SurveyHoriz, (ushort)((ushort)(short)town.Place.X >> 2));
			this.processor.EmitLocalWord(SurveyVert, (ushort)((ushort)(short)town.Place.Y >> 2));
			ushort block = Block(town.Place.X, town.Place.Y);
			this.processor.EmitUInt8(BlockSegment, block, (byte)(this.processor.PullUInt8(BlockSegment, block) | 1));

			// The (up to two) units known to be in the city: their strength (a translation slip keeps only the last one
			// for the whole player and the attack of the continent; the defence adds up)
			this.processor.EmitLocalWord(SurveyLoop2, 0);

			for (ushort unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
			{
				if ((byte)town.Unfamiliar[unfamiliarIdx] != 0xff)
				{
					sbyte kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);
					this.processor.EmitLocalWord(SurveyScratch, (ushort)(kind * UnitKindTable.Stride));

					player.MartialMight = (short)(UnitKindTable.Field(this.processor, kind, UnitKindTable.AssaultPower) +
						UnitKindTable.Field(this.processor, kind, UnitKindTable.GuardPower));

					ushort continentIdx = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
					this.processor.EmitLocalWord(SurveyContinent, continentIdx);

					kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);
					this.processor.EmitLocalWord(SurveyScratch2, (ushort)(kind * UnitKindTable.Stride));
					this.processor.EmitLocalWord(SurveyScratch3, (ushort)((participantIdx << 5) + (continentIdx << 1)));

					player.Landmasses[continentIdx].Assault = (short)UnitKindTable.Field(this.processor, kind, UnitKindTable.GuardPower);
					player.Landmasses[continentIdx].Guard += (short)UnitKindTable.Field(this.processor, kind, UnitKindTable.AssaultPower);
				}

				this.processor.EmitLocalWord(SurveyLoop2, (ushort)(unfamiliarIdx + 1));
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

			this.processor.EmitLocalWord(SurveyHoriz, 0);

			for (ushort blockHoriz = 0; blockHoriz < 0x14; blockHoriz++)
			{
				this.processor.EmitLocalWord(SurveyVert, 0);

				for (ushort blockVert = 0; blockVert < 0xd; blockVert++)
				{
					if (this.processor.PullUInt8(BlockSegment, (ushort)(BlockOrigin + blockHoriz * 0xd + blockVert)) != 0)
						player.SoilTally++;

					this.processor.EmitLocalWord(SurveyVert, (ushort)(blockVert + 1));
				}

				this.processor.EmitLocalWord(SurveyHoriz, (ushort)(blockHoriz + 1));
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
			this.processor.EmitGlobalWord(0x2a6e, (ushort)(this.processor.PullGlobalWord(0x2a6e) & ~(1 << participantIdx)));

			this.processor.EmitGlobalWord(0x6ed4, 0);
			this.processor.EmitLocalWord(SurveyOuterLoop, 1);

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if ((player.Relations[otherIdx] & 3) == 1)
					this.processor.EmitGlobalWord(0x6ed4, 1);

				this.processor.EmitLocalWord(SurveyOuterLoop, (ushort)(otherIdx + 1));
			}

			this.processor.EmitLocalWord(SurveyMostTowns, 0xffff);
			this.processor.EmitLocalWord(SurveyOuterLoop, 1);

			for (ushort continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				ChooseContinentApproach(participantIdx, continentIdx);
				this.processor.EmitLocalWord(SurveyOuterLoop, (ushort)(continentIdx + 1));
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

			this.processor.EmitLocalWord(SurveyThreats, 0);
			this.processor.EmitLocalWord(SurveyStronger, 0);
			this.processor.EmitLocalWord(SurveyChoice, 0);
			this.processor.EmitLocalWord(SurveyTowns, 0);
			this.processor.EmitLocalWord(SurveyStrength, 0);
			this.processor.EmitLocalWord(SurveyApproach, (ushort)approach);
			this.processor.EmitLocalWord(SurveyLoop, 1);

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				PlayerContinent other = this.owner.World.Participants[otherIdx].Landmasses[continentIdx];

				// An enemy (war, or 0x100) whose units there are not much weaker than the player's defence, or who has cities there
				if (other.Assault != 0 &&
					((short)(own.Guard / 4) < other.Guard || other.TownTally != 0) &&
					((player.Relations[otherIdx] & 3) == 1 || (player.Relations[otherIdx] & 0x100) != 0))
				{
					this.processor.EmitLocalWord(SurveyScratch3, (ushort)((otherIdx << 5) + (continentIdx << 1)));

					if (other.Assault < own.Guard || other.Guard < own.Assault || own.TownTally == 0)
					{
						threats++;
						this.processor.EmitLocalWord(SurveyThreats, (ushort)threats);
					}
					else
					{
						stronger++;
						this.processor.EmitLocalWord(SurveyStronger, (ushort)stronger);
					}
				}

				// The human player at peace with the player, stronger there than half its defence and all its attack
				if (other.Assault != 0 && otherIdx == this.owner.World.PersonParticipantIdx && (player.Relations[otherIdx] & 2) != 0)
				{
					this.processor.EmitLocalWord(SurveyScratch3, (ushort)((participantIdx << 5) + (continentIdx << 1)));

					if ((short)(CPU.SignedQuotient(own.Guard, 2) + own.Assault) < other.Guard)
					{
						stronger++;
						this.processor.EmitLocalWord(SurveyStronger, (ushort)stronger);
					}
				}

				strength += other.Assault;
				this.processor.EmitLocalWord(SurveyStrength, (ushort)strength);
				towns += other.TownTally;
				this.processor.EmitLocalWord(SurveyTowns, (ushort)towns);

				if (other.Assault != 0)
				{
					// 1: someone at peace with the player, 2: someone at war
					presence |= (ushort)((player.Relations[otherIdx] & 2) != 0 ? 1 : 2);
					this.processor.EmitLocalWord(SurveyChoice, presence);
				}

				this.processor.EmitLocalWord(SurveyLoop, (ushort)(otherIdx + 1));
			}

			if (this.owner.World.Participants[0].Landmasses[continentIdx].TownTally != 0)
			{
				threats++;
				this.processor.EmitLocalWord(SurveyThreats, (ushort)threats);
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

			if ((short)this.processor.PullLocalWord(SurveyMostTowns) < own.TownTally)
				this.processor.EmitLocalWord(SurveyMostTowns, (ushort)own.TownTally);

			if (own.Approach != approach && (this.owner.World.ParticipantMarkers & (1 << participantIdx)) == 0)
				RefreshCityProduction(participantIdx, continentIdx);

			if (own.Assault != 0 && this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[continentIdx].TownTally > 1)
				this.processor.EmitGlobalWord(0x2a6e, (ushort)(this.processor.PullGlobalWord(0x2a6e) | (1 << participantIdx)));
		}

		/// <summary>Communism (4), Democracy (3, more than 10 cities), Monarchy (2), or Anarchy (1) when 0xe3c2 is set</summary>
		private void ChooseGovernment(short participantIdx)
		{
			if ((short)this.processor.PullGlobalWord(0xe3c2) > 0)
			{
				this.owner.Politics.SwitchGovernment(participantIdx, 1);
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2c) != 0 && (short)this.processor.PullGlobalWord(0xdb42) >= 0)
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
		private void DirectUnits(short participantIdx)
		{
			ushort sentBase = this.processor.PullLocalWord(SurveyBase);

			this.processor.EmitLocalWord(SurveyOuterLoop, 0);

			for (int locationIdx = 0; locationIdx < 0x20; locationIdx++)
			{
				this.processor.EmitUInt16(this.processor.SegStack.Value16, LocationSentAddress(locationIdx), sentBase);
				this.processor.EmitLocalWord(SurveyOuterLoop, (ushort)(locationIdx + 1));
			}

			this.processor.EmitLocalWord(SurveyLoop, 0);

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

				if (kind != 0xff && (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x10) == 0 && kind != 0x19)
				{
					if (!GiveUpObsoleteUnit(participantIdx, troopIdx))
						SendUnitToBestLocation(participantIdx, troopIdx);
				}

				this.processor.EmitLocalWord(SurveyLoop, (ushort)(troopIdx + 1));
			}
		}

		/// <summary>
		/// Once in 16 turns: a unit whose technology the player knows (or a militia under a government above despotism, or
		/// when the continent is attacked) is given up, unless it is next to another player's unit, alone in a city, on the
		/// ocean or on a continent where the enemy is stronger
		/// </summary>
		private bool GiveUpObsoleteUnit(short participantIdx, short troopIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];

			this.processor.EmitLocalWord(SurveyBestDistance, 0x270f);
			this.processor.EmitLocalWord(SurveyChoice, 0xffff);

			short horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			short vertLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			this.processor.EmitLocalWord(SurveyHoriz, (ushort)horizLoc);
			this.processor.EmitLocalWord(SurveyVert, (ushort)vertLoc);

			ushort continentIdx = this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
			this.processor.EmitLocalWord(SurveyContinent, continentIdx);

			if ((((byte)troopIdx + (byte)this.owner.World.RoundTally) & 0xf) != 0)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)UnitKindTable.Field(this.processor, kind, 0xc)) == 0)
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
		private void SendUnitToBestLocation(short participantIdx, short troopIdx)
		{
			ushort sentBase = this.processor.PullLocalWord(SurveyBase);
			short horizLoc = (short)this.processor.PullLocalWord(SurveyHoriz);
			short vertLoc = (short)this.processor.PullLocalWord(SurveyVert);
			ushort continentIdx = this.processor.PullLocalWord(SurveyContinent);
			short bestDistance = 0x270f;
			short best = -1;

			this.processor.EmitLocalWord(SurveyOuterLoop, 0);

			for (short locationIdx = 0; locationIdx < 0x20; locationIdx++)
			{
				if (LocationFits(participantIdx, troopIdx, locationIdx, continentIdx))
				{
					short? distance = WeighLocation(participantIdx, troopIdx, locationIdx, horizLoc, vertLoc);

					if (distance != null && MayTakeLocation(participantIdx, troopIdx, locationIdx, distance.Value, sentBase) &&
						distance.Value < bestDistance &&
						(short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx)) << 2) >=
							CPU.SignedQuotient(distance.Value, (short)sentBase))
					{
						bestDistance = distance.Value;
						best = locationIdx;
						this.processor.EmitLocalWord(SurveyBestDistance, (ushort)bestDistance);
						this.processor.EmitLocalWord(SurveyChoice, (ushort)best);
					}
				}

				this.processor.EmitLocalWord(SurveyOuterLoop, (ushort)(locationIdx + 1));
			}

			if (best == -1)
				return;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.processor.PullUInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, best)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.processor.PullUInt8(this.processor.SegData.Value16, LocationVert(participantIdx, best)));

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & 0xf0));

			ushort sent = LocationSentAddress(best);
			this.processor.EmitUInt16(this.processor.SegData.Value16, sent, (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, sent) + 1));
		}

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

			if (UnitKindTable.Field(this.processor, kind, UnitKindTable.TroopGroup) == (ushort)(sbyte)role &&
				this.owner.MapOps.ObtainContinentID(this.processor.PullInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					this.processor.PullInt8(this.processor.SegData.Value16, LocationVert(participantIdx, locationIdx))) == continentIdx)
			{
				return true;
			}

			byte kindByte = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);
			this.processor.EmitLocalByte(SurveyScratch3, kindByte);

			if (kindByte != 0xf)
				return false;

			sbyte bomberRole = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationRole(participantIdx, locationIdx));
			this.processor.EmitLocalByte(SurveyScratch2, (byte)bomberRole);

			return UnitKindTable.Field(this.processor, (sbyte)this.processor.PullLocalByte(SurveyScratch3), UnitKindTable.TroopGroup) == (ushort)bomberRole ||
				bomberRole == 3;
		}

		/// <summary>
		/// The weighed distance to the location (kept in 0x46): a bomber only goes to locations with priority bit 0 in every
		/// other turn (else null: the location is skipped), its distance is doubled within its range, else quadrupled; other
		/// units: the distance times the units already sent there; both divided by the priority + 1
		/// </summary>
		private short? WeighLocation(short participantIdx, short troopIdx, short locationIdx, short horizLoc, short vertLoc)
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
				this.processor.EmitLocalWord(SurveyDistance, (ushort)distance);

				short range = (short)UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally);
				weighed = (short)((range >= distance ? 2 : 4) * distance);
			}
			else
			{
				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc,
					this.processor.PullInt8(this.processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					vertLoc, this.processor.PullInt8(this.processor.SegData.Value16, LocationVert(participantIdx, locationIdx)));

				weighed = (short)(this.processor.PullUInt16(this.processor.SegStack.Value16, LocationSentAddress(locationIdx)) * distance);
			}

			sbyte priority = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx));
			short result = CPU.SignedQuotient(weighed, (short)(priority + 1));
			this.processor.EmitLocalWord(SurveyDistance, (ushort)result);

			return result;
		}

		/// <summary>
		/// A unit with orders (status bits 2, 3) only takes a location of priority 2 or more, whose weight (priority times
		/// the base) is not below the distance or no unit was sent there yet, and when it is not in a city
		/// </summary>
		private bool MayTakeLocation(short participantIdx, short troopIdx, short locationIdx, short distance, ushort sentBase)
		{
			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc) == 0)
				return true;

			sbyte priority = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, LocationPriority(participantIdx, locationIdx));
			this.processor.EmitLocalByte(SurveyScratch3, (byte)priority);

			if (priority < 2)
				return false;

			if ((short)(priority * (short)sentBase) < distance &&
				this.processor.PullUInt16(this.processor.SegStack.Value16, LocationSentAddress(locationIdx)) != sentBase)
			{
				return false;
			}

			return (this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) & 1) == 0;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <returns></returns>
		public ushort ShiftAIUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ShiftAIUnit({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x5e);
			this.processor.PreserveIndexRegisters();

			if (participantIdx == 0)
			{
				ManageBarbarianUnit(participantIdx, troopIdx);
				goto L2fd0;
			}

			// Planes out of fuel or moves, the fighters after a bomber of the human player or a city to land in
			ushort? airAnswer = PlanAirUnit(participantIdx, troopIdx);

			if (airAnswer != null)
			{
				this.processor.RegAcc.Value16 = airAnswer.Value;
				goto L2fd0;
			}

			SurveyAIUnit(participantIdx, troopIdx);

			if (this.processor.PullLocalWord(0x8) != 0x19)
				goto L12a3;

			this.processor.RegAcc.Value16 = AimNuclearWeapon(participantIdx, troopIdx);
			goto L2fd0;

		L12a3:
			if (this.processor.PullLocalWord(0x8) == 0x1a)
			{
				// A diplomat with a destination goes on, one near another player's city goes there, one in an own city is given up
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
					goto L12c0;

				this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x34) * 0x1c);

				if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx != participantIdx)
					goto L12d7;

				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			}

			goto L1307;

		L12c0:
			this.processor.RegAcc.Value16 = 0;
			goto L2fd0;

		L12d7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.RegSource.Value16 / 28].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			goto L12c0;

		L1307:
			ushort? explored = ExploreAround(participantIdx, troopIdx);

			if (explored != null)
			{
				this.processor.RegAcc.Value16 = explored.Value;
				goto L2fd0;
			}

			goto L14ac;

		L1417:
			this.processor.RegAcc.Value16 = 0x62;
			goto L2fd0;

		L14ac:
			switch (GuardCity(participantIdx, troopIdx))
			{
				case CityGuard.NotDefender:
					goto L168f;

				case CityGuard.Stays:
					goto L14e1;
			}

			goto L166c;

		L14e1:
			this.processor.RegAcc.Value16 = 0x66;
			goto L2fd0;

		L166c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L167b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L167b;
			goto L1f44;

		L167b:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			goto L1738;

		L168f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L166c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L166c;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				2, 4);

		L16b3:
			this.processor.RegAcc.Value16 = 0x20;
			goto L2fd0;

		L16b9:
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L16c4;
			goto L1752;

		L16c4:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x20));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L16f8;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1148));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x1e, this.processor.BitOrWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1732;
			goto L172f;

		L16f8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L1732;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x5;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x2c);
			this.processor.IncrementLocalWord(0x2c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1732;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x20));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xf7));

		L172f:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L1732:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x20));

		L1738:
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1752;
			goto L16b9;

		L1752:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L175d;
			this.processor.RegAcc.Value16 = 0x1;
			goto L1760;

		L175d:
			this.processor.RegAcc.Value16 = 0x3;

		L1760:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x54));
			if (this.processor.FlagSet.IsGreater) goto L176e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L176e;
			goto L198e;

		L176e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L1777;
			goto L198e;

		L1777:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L17d4;

		L1783:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.processor.PullLocalSignedWord(0x36),
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].Assault >= 16)
				goto L1800;

		L17d1:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L17d4:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
			if (this.processor.FlagSet.IsLess) goto L17de;
			goto L18d1;

		L17de:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L17d1;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L17d1;
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ConditionMarker, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1783;
			goto L17d1;

		L1800:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xc));

			this.owner.MapOps.ObtainUnitAtTile(
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1846;

			this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 5);

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1846;
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0x10));

		L1846:
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach == 5)
				goto L185f;

			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0x10));
			goto L1880;

		L185f:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Guard;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0x12, this.processor.SubtractWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));

		L1880:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L188b;
			goto L17d1;

		L188b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			goto L17d1;

		L18d1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L18dc;
			this.processor.RegAcc.Value16 = 0x4;
			goto L18df;

		L18dc:
			this.processor.RegAcc.Value16 = 0x2;

		L18df:
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x12));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L18f0;
			goto L1f44;

		L18f0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1922;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				0, this.processor.PullLocalSignedWord(0x12));

		L1922:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1955;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				2, this.processor.PullLocalSignedWord(0x12));

		L1955:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L195e;
			goto L1f44;

		L195e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)),
				1, this.processor.PullLocalSignedWord(0x12));

			goto L1f44;

		L198e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L1997;
			goto L1c37;

		L1997:
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L1a68;

		L199f:
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach != 1)
				goto L19ca;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				3, 5);

		L19ca:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = 0x75;
			goto L2fd0;

		L19ea:
			this.processor.TestBitsByte((byte)(this.owner.World.RoundTally & 0xff), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L1a06;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L1a06:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1a20;
			goto L1c37;

		L1a20:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual)
				goto L1c37;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1a62;
			goto L1c22;

		L1a62:
			goto L1c37;

		L1a65:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1a68:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L1a06;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1a65;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1a65;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1ad2;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1a65;

		L1ad2:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1b00;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)this.processor.RegAcc.Value16, 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L1b00;
			goto L1a65;

		L1b00:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				goto L1b3d;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.PersonParticipantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L1b3d;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0xf);
			if (this.processor.FlagSet.IsEqual) goto L1b3d;
			goto L1a65;

		L1b3d:
			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, 0x0);
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Assault >= 16)
				goto L1b6c;
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach != 5)
				goto L1b87;

		L1b6c:
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, this.processor.PullLocalByte(0x1e));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L1b8c;

		L1b87:
			this.processor.EmitLocalWord(0x3c, 0x1);

		L1b8c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1bc1;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4a));
			if (this.processor.FlagSet.IsNotEqual) goto L1bc1;
			this.processor.EmitLocalWord(0x3c, 0x1);

		L1bc1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1bca;
			goto L19ea;

		L1bca:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			if (this.owner.World.Landmasses[this.processor.PullLocalWord(0x4a)].ConstructionSpotTally < 5)
				goto L19ea;

			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L1be2;
			goto L19ea;

		L1be2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x30);
			if (this.processor.FlagSet.IsLess) goto L1beb;
			goto L19ea;

		L1beb:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L1c05;
			goto L199f;

		L1c05:
			MayAIEnterTile(participantIdx, 
				this.processor.PullLocalWord(0x30),
				this.processor.PullLocalWord(0x36));
			
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreater) goto L1c1f;
			goto L199f;

		L1c1f:
			goto L2fd0;

		L1c22:
			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e),
				3, 3);

		L1c37:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1c46;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L1c46;
			goto L1f44;

		L1c46:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1c60;
			goto L1d49;

		L1c60:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x1e), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1c6f;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x1e), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1c6f;
			goto L1d49;

		L1c6f:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L1c89;
			goto L1d49;

		L1c89:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RoundTally;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			goto L1d3f;

		L1c9a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ParticipantIdx;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ConditionMarker == 0xff)
				goto L1d3b;

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1d3b;

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			
			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x3a)] & 0x102) == 2)
				goto L1d3b;

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing < 7)
				goto L1ce8;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1d3b;

		L1ce8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.processor.PullLocalSignedWord(0x36),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1d3b;
			this.processor.CopyLocalWord(0x12, 0x14);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L1d3b:
			this.processor.EmitLocalWord(0x40, this.processor.SumWord(this.processor.PullLocalWord(0x40), 0x8));

		L1d3f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1d49;
			goto L1c9a;

		L1d49:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1d63;
			goto L1e78;

		L1d63:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L1d6d;
			goto L1e78;

		L1d6d:
			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0x10, 0xffff);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1d90;
			this.processor.CopyLocalWord(0x1a, 0x10);

		L1d90:
			this.processor.EmitLocalWord(0x40, 0x0);

		L1d95:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ParticipantIdx;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x40)].ConditionMarker == 0xff)
				goto L1e6b;

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1dba;
			goto L1e6b;

		L1dba:
			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x30),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.processor.PullLocalSignedWord(0x36),
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1ddf;
			goto L1e6b;

		L1ddf:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x40));

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1e6b;
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach == 5)
				goto L1e6b;

			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4a)].Approach;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, this.processor.PullLocalByte(0x1e));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L1e35;
			this.processor.EmitLocalWord(0x12, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x12), 0x1));

		L1e35:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e6b;
			this.processor.CopyLocalWord(0x12, 0x14);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x40));
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.X);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x40)].Place.Y);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L1e6b:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e78;
			goto L1d95;

		L1e78:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1e92;
			goto L1f44;

		L1e92:
			this.processor.EmitLocalWord(0x40, 0x2);
			goto L1e9c;

		L1e99:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L1e9c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x18);
			if (this.processor.FlagSet.IsLess) goto L1ea5;
			goto L1f44;

		L1ea5:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(9));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.owner.Routing.TunePosition(
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882)) *
					this.processor.PullLocalSignedWord(0x40)) +
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x40));
			this.processor.RegCount.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegCount.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1e99;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e99;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1e99;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));
			
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].TownTally == 0) goto L1f36;
			goto L1e99;

		L1f36:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);

		L1f44:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1fb2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1fb2;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x34));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L1fb2;
			
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 2) != 0)
				goto L1fb2;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L1fb2;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L1fb2;
			this.processor.RegAcc.Value16 = 0x50;
			goto L2fd0;

		L1fb2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1fbb;
			goto L2520;

		L1fbb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1fc4;
			goto L241e;

		L1fc4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x2);
			if (this.processor.FlagSet.IsLess) goto L1fdf;

			SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36),
				2, 2);

		L1fdf:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = 0x5;
			}
			else
			{
				this.processor.RegSource.Value16 = 0x4;
			}
		
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x42));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x44a)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2014;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L2014;
			goto L213b;

		L2014:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L201d;
			goto L213b;

		L201d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2026;
			goto L213b;

		L2026:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L203b;
			goto L213b;

		L203b:
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2060;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L2060;

			this.owner.MapOps.ContainsSpecialResource(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2060;
			goto L213b;

		L2060:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L206a;
			goto L213b;

		L206a:
			this.owner.TurnLogic.ObtainTileMoveCost(this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L20c0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L20db;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x6);
			if (this.processor.FlagSet.IsEqual) goto L20f6;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L20f6;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L20f6;

		L20a5:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L20ba:
			this.processor.RegAcc.Value16 = 0x72;
			goto L2fd0;

		L20c0:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = 0x69;
			goto L2fd0;

		L20db:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = 0x6d;
			goto L2fd0;

		L20f6:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x3a);
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, 0x18);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x18);
			if (this.processor.FlagSet.IsEqual) goto L213b;
			
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L213b;

			this.owner.CityOps.MayCityWorkTile(this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36), 1);

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x3a), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L2131;
			this.processor.RegAcc.Value16 = 0x1;
			goto L2134;

		L2131:
			this.processor.RegAcc.Value16 = 0x2;

		L2134:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreater) goto L213b;
			goto L20a5;

		L213b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L2153;
			this.processor.RegAcc.Value16 = 0x70;
			goto L2fd0;

		L2153:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L219c;
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1514));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Landmasses[this.processor.PullLocalWord(0x1a)].ConstructionSpotTally);
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegData.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x1a)].TownTally;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L219c;
			goto L12c0;

		L219c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L21a5;
			goto L230b;

		L21a5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L21ba;
			goto L230b;

		L21ba:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L21c4;
			goto L230b;

		L21c4:
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L22a6;

		L21cc:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.RegCount.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 5);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegBase.Value16 = 0x5;
			}
			else
			{
				this.processor.RegBase.Value16 = 0x4;
			}
		
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x44a)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L220a;
			goto L22a3;

		L220a:
			this.owner.TurnLogic.ObtainTileMoveCost(this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L221f;
			goto L22a3;

		L221f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2257;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L2257;

			this.owner.MapOps.ContainsSpecialResource(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L22a3;

		L2257:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));

			this.owner.Helpers.MergeCoordinates(
				(short)(this.processor.PullLocalSignedWord(0x26) -
					this.owner.World.Towns[this.processor.PullLocalWord(0x34)].Place.X),
				(short)(this.processor.PullLocalSignedWord(0x2e) -
					this.owner.World.Towns[this.processor.PullLocalWord(0x34)].Place.Y));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsGreater) goto L22a3;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2e);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);
			goto L12c0;

		L22a3:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L22a6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L230b;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.RoundTally);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x30));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L22e9;
			goto L21cc;

		L22e9:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L22a3;

			this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x20), 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2309;
			goto L21cc;

		L2309:
			goto L22a3;

		L230b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L2320;
			goto L241e;

		L2320:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L233c;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L233c;

			goto L241e;

		L233c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x34));
			if (this.processor.PullLocalWord(0x34) < 128)
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L2351;
			goto L241e;

		L2351:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L2369;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2360;
			goto L20ba;

		L2360:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2369;
			goto L20ba;

		L2369:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x40, 0x1);

		L2379:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + this.processor.PullLocalSignedWord(0x30),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L23cf;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x40);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x10, this.processor.BitOrWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x54);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) * 2 + this.processor.PullLocalSignedWord(0x30),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) * 2 + this.processor.PullLocalSignedWord(0x36));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L23cf;
			this.processor.EmitLocalWord(0x3a, 0x1);

		L23cf:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2379;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x10, this.processor.BitAndWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2400;
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L23f1;
			goto L20ba;

		L23f1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2400;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L2400;
			goto L20ba;

		L2400:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L241e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L241e;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x42));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L241e;
			goto L20ba;

		L241e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2438;
			goto L12c0;

		L2438:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(20));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x3a);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xd, this.processor.PullLocalWord(0x26));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x2e));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L2499;
			goto L2520;

		L2499:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1882));
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x18e4));
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, 0x1);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db), this.processor.RegAcc.LowByte);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L250b;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.RegCount.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x291)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L250b;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1a));
			if (this.processor.FlagSet.IsNotEqual) goto L250b;
			goto L12c0;

		L250b:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L2520:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2558;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L2540;
			goto L12c0;

		L2540:
			this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2558;
			goto L12c0;

		L2558:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2594;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2594;
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2585;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L2585;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L2585;
			goto L14e1;

		L2585:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2594;
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2594;
			goto L14e1;

		L2594:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25f2;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Landmasses[this.processor.PullLocalWord(0x1a)].ConstructionSpotTally;
			this.processor.DivideAccumulatorByPowerOfTwo(0x3);
			this.processor.CompareWord((ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x1a)].TownTally, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L25f2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2a), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25d6;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L25d6;
			goto L1417;

		L25d6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x34));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L25f2;
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x34)].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L25f2;
			goto L12d7;

		L25f2:
			this.processor.EmitLocalWord(0x28, 0xfc19);
			this.processor.EmitLocalWord(0x2c, 0x0);
			this.processor.EmitLocalWord(0x5c, 0x0);

			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullLocalSignedWord(0x30),
				this.processor.PullLocalSignedWord(0x36));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L262d;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), 0xff);
			goto L2632;

		L262d:
			this.processor.EmitLocalWord(0x5c, 0x1);

		L2632:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2643;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2643;
			this.processor.EmitLocalWord(0x56, 0x1);

		L2643:
			this.processor.EmitLocalWord(0x40, 0x1);
			goto L267b;

		L264a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2653;
			goto L2857;

		L2653:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L265e;
			goto L2857;

		L265e:
			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2678;
			goto L2857;

		L2678:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L267b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2684;
			goto L2f94;

		L2684:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2678;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x58, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x38, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L26ff;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L26ff;
			goto L2678;

		L26ff:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L277d;
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x24));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L277d;
			this.processor.CopyLocalWord(0x24, 0x20);

		L2733:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x20));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L2771;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2771;
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x20));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L2733;

		L2771:
			this.processor.CompareWord(this.processor.PullLocalWord(0x20), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L277d;
			this.processor.CopyLocalWord(0x20, 0x24);

		L277d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L27cd;
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x81d7);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L27cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L27cd;

			this.owner.UnitOps.MayOccupyTile(participantIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L27cd;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x1a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L27cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L27cd;
			goto L2678;

		L27cd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2803;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2803;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L2803;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2803;
			goto L2678;

		L2803:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L2825;
			goto L264a;

		L2825:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L282e;
			goto L264a;

		L282e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2837;
			goto L2678;

		L2837:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2842;
			goto L2678;

		L2842:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x16);
			if (this.processor.FlagSet.IsNotEqual) goto L284b;
			goto L2678;

		L284b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2854;
			goto L264a;

		L2854:
			goto L2678;

		L2857:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2886;
			this.processor.EmitLocalWord(0x10, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L286b;
			goto L29ed;

		L286b:
			this.owner.UnitOps.VerifyTileOccupation(participantIdx,
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2883;
			goto L29ed;

		L2883:
			goto L2678;

		L2886:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L28eb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L28eb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L28ce;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);

		L28c6:
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegCount.Value16);
			goto L29bf;

		L28ce:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			goto L28c6;

		L28eb:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(5));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2903;
			goto L29ab;

		L2903:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L290e;
			goto L29ab;

		L290e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2947;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 1);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2947:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2975;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 1);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x13, this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2975:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L29bf;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 1);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x24), 3);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			goto L29bc;

		L29ab:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x38));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);

		L29bc:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L29bf:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L29ed;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

		L29ed:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc));
			this.processor.EmitLocalByte(0x5e, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L2a30;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x40));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2a26;
			this.processor.RegAcc.Value16 = 0x8;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xa));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

		L2a26:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2a30:
			this.processor.EmitLocalWord(0x1c, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2a3e;
			goto L2d85;

		L2a3e:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2a49;
			goto L2d64;

		L2a49:
			this.processor.EmitLocalWord(0x1c, 0x1);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2a57;
			goto L2678;

		L2a57:
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.processor.RegAcc.Value16].Relations[this.processor.PullLocalWord(0x58)] & 2) != 0)
				goto L2c6a;

			this.owner.UnitOps.LocateBestGuardianInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24));

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x24));
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x58));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ac8;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xe);
			if (this.processor.FlagSet.IsEqual) goto L2ac8;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ac8;
			goto L2678;

		L2ac8:
			this.owner.Combat.SettleBattle(participantIdx, troopIdx,
				this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24),
				0);
			
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

			this.owner.UnitOps.TallyUnitsByDomainInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24), 0);

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x5a));
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2b3b;
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x5a));
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

		L2b3b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2b50;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2b50;
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x5a));
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);

		L2b50:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2b8f;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 3);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2b8f;
			this.processor.EmitLocalWord(0x5a, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x5a), 0x1));

		L2b8f:
			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2b9a;
			this.processor.RegAcc.Value16 = 0x6;
			goto L2b9d;

		L2b9a:
			this.processor.RegAcc.Value16 = 0xc;

		L2b9d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x5a));
			if (this.processor.FlagSet.IsGreater) goto L2baf;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			goto L2dbd;

		L2baf:
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), 0x3e7));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bbd;
			goto L2dbd;

		L2bbd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bc6;
			goto L2dbd;

		L2bc6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2be8;
			goto L2dbd;

		L2be8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x24));
			this.processor.ScaleIndexToSource(0x600, this.processor.PullLocalWord(0x58));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x28);
			if (this.processor.FlagSet.IsNotEqual) goto L2c02;
			goto L2dbd;

		L2c02:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x38));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28f)), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2c14;
			goto L2dbd;

		L2c14:
			this.owner.UnitOps.TallyUnitsByDomainInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L2c2e;
			goto L2dbd;

		L2c2e:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2c41;
			goto L2dbd;

		L2c41:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c56;
			goto L2dbd;

		L2c56:
			this.owner.Diplomats.BuyUnit(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24),
				participantIdx);
			
			goto L2dbd;

		L2c6a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c73;
			goto L2678;

		L2c73:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c7c;
			goto L2678;

		L2c7c:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2c9e;
			goto L2678;

		L2c9e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x24));
			this.processor.ScaleIndex(0x600, this.processor.PullLocalWord(0x58));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L2cb8;
			goto L2678;

		L2cb8:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 3) != 1)
				goto L2678;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x58)].Relations[this.owner.World.PersonParticipantIdx] & 3) == 1)
				goto L2678;

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Landmasses[this.processor.PullLocalWord(0x1a)].TownTally == 0)
				goto L2678;

			this.owner.UnitOps.TallyUnitsByDomainInStack(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24), 2);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLess) goto L2d15;
			goto L2678;

		L2d15:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(8));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2d3b;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x34));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L2678;

		L2d3b:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d50;
			goto L2678;

		L2d50:
			this.owner.Diplomats.BuyUnit(this.processor.PullLocalSignedWord(0x58),
				this.processor.PullLocalSignedWord(0x24),
				participantIdx);
			
			goto L2678;

		L2d64:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1140));
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));
			goto L2dbd;

		L2d85:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2da4;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2da4;
			this.processor.EmitLocalWord(0x10, 0x3e7);

		L2da4:
			this.owner.MapOps.ContainsHut(
				this.processor.PullLocalWord(0x38),
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2dbd;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x14));

		L2dbd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x5c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2dc6;
			goto L2f15;

		L2dc6:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x30));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x36));
			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.RegCount.Value16 = 0xd;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2e);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2438)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2e47;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2e47;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullLocalSignedWord(0x2e));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L2e47;
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x8));

		L2e47:
			this.processor.EmitLocalWord(0x44, 0x1);

		L2e4c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2e));
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L2e81;
			goto L2f09;

		L2e81:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x16)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			if (this.processor.FlagSet.IsNotEqual) goto L2ed1;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2ecd;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2ed1;

		L2ecd:
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), 0x2));

		L2ed1:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2ee6;
			this.processor.EmitLocalWord(0x10, this.processor.SubtractWord(this.processor.PullLocalWord(0x10), 0x2));

		L2ee6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f09;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x16));

			this.processor.RegCount.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x290));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L2f09:
			this.processor.EmitLocalWord(0x44, this.processor.IncrementWord(this.processor.PullLocalWord(0x44)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x44), 0x8);
			if (this.processor.FlagSet.IsGreater) goto L2f15;
			goto L2e4c;

		L2f15:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2f74;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2f74;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 3);

			this.owner.Helpers.VerifyValueSpan((short)this.processor.RegAcc.Value16, 1, 99);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.RegCount.Value16;

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 1);

			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x10));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

		L2f74:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L2f7f;
			goto L2678;

		L2f7f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x40, 0x2c);
			this.processor.CopyLocalWord(0x1c, 0x52);
			goto L2678;

		L2f94:
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2fb6;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2fb6;
			this.processor.EmitLocalWord(0x2c, 0x0);

		L2fb6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x2c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);

		L2fcd:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2c);

		L2fd0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftAIUnit");

			return this.processor.RegAcc.Value16;
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
			short horizLoc = (short)this.processor.PullLocalWord(0x30);
			short vertLoc = (short)this.processor.PullLocalWord(0x36);

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
				return CityGuard.GoesOn;

			if (this.processor.PullLocalWord(0x56) != 2)
				return CityGuard.NotDefender;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return CityGuard.Stays;

			// The leader of the stack without this unit (it counts as a diplomat while looking)
			this.processor.EmitLocalWord(0x3a, (ushort)this.troops.TroopKind(participantIdx, troopIdx));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0x1a);
			this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, this.processor.PullLocalByte(0x3a));

			if (this.processor.PullLocalWord(0x20) != 0xffff)
			{
				short defence = (short)UnitKindTable.Field(this.processor, (sbyte)this.processor.PullLocalByte(0x3a), UnitKindTable.GuardPower);
				short leader = (short)this.processor.PullLocalWord(0x20);

				if ((short)UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, leader), UnitKindTable.GuardPower) < defence)
					return CityGuard.Stays;
			}

			this.owner.UnitOps.VerifyTileOccupation(participantIdx, horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x6, (ushort)(this.processor.RegAcc.Value16 != 0 ? 3 : 4));

			Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x34)];

			if ((town.StructureMarkers0 & 1) != 0)
				this.processor.DecrementLocalWord(0x6);
			else if (this.processor.PullLocalWord(0x2) == 0)
				this.processor.IncrementLocalWord(0x6);

			this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, troopIdx, 2);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			short wanted = (short)(CPU.SignedQuotient(town.TrueExtent, (short)this.processor.PullLocalWord(0x6)) + 1);

			if (wanted > (short)this.processor.PullLocalWord(0x3a))
				SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 2, 2);

			if (wanted >= (short)this.processor.PullLocalWord(0x3a))
				return CityGuard.Stays;

			// The leader of the stack fortifies while the units there are too few
			ushort status = (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Status);
			this.processor.EmitLocalWord(0x3a, (ushort)(sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, status));
			this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) | 8));

			if (this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx) == (ushort)troopIdx)
			{
				short units = (short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 4);

				if ((short)(CPU.SignedQuotient(town.TrueExtent, (short)this.processor.PullLocalWord(0x6)) + 1) > units)
				{
					// A translation slip: the status is put back at SI, which the count of the stack has left at 0x600 * player
					this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.PullLocalByte(0x3a));
					return CityGuard.Stays;
				}
			}

			this.processor.EmitUInt8(this.processor.SegData.Value16, status, this.processor.PullLocalByte(0x3a));

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
			short horizLoc = (short)this.processor.PullLocalWord(0x30);
			short vertLoc = (short)this.processor.PullLocalWord(0x36);

			if (this.processor.PullLocalWord(0x56) == 0)
				DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 0);

			this.owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);
			this.processor.EmitLocalWord(0x28, this.processor.RegAcc.Value16);

			if (this.processor.PullLocalWord(0x28) != 0 && (short)this.processor.PullLocalWord(0x56) < 3)
			{
				this.processor.EmitLocalWord(0x2c, 0);

				for (this.processor.EmitLocalWord(0x40, 1); (short)this.processor.PullLocalWord(0x40) <= 8;
					this.processor.EmitLocalWord(0x40, (ushort)(this.processor.PullLocalWord(0x40) + 1)))
				{
					ushort direction = this.processor.PullLocalWord(0x40);

					this.owner.Routing.TunePosition(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((direction << 1) + 0x1882)) + horizLoc);
					this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
					this.processor.EmitLocalWord(0x2e, (ushort)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + this.processor.PullLocalWord(0x36)));

					short nextHoriz = (short)this.processor.PullLocalWord(0x26);
					short nextVert = (short)this.processor.PullLocalWord(0x2e);

					this.owner.VideoCard.ObtainDot(2, nextHoriz + 0x50, nextVert);
					this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

					if ((short)this.processor.PullLocalWord(0x10) > (short)this.processor.PullLocalWord(0x28))
					{
						this.processor.CopyLocalWord(0x10, 0x28);
						this.processor.CopyLocalWord(0x40, 0x2c);
					}

					this.owner.MapOps.ObtainTerrainType(nextHoriz, nextVert);
					this.owner.MapOps.ContainsHut(this.processor.RegAcc.Value16, nextHoriz, nextVert);

					if (this.processor.RegAcc.Value16 != 0 && this.processor.PullLocalWord(0x56) != 0)
						return this.processor.PullLocalWord(0x40);
				}

				if (this.owner.World.RoundTally == 0 && this.processor.PullGlobalWord(0xd76a) != 0)
				{
					this.processor.EmitLocalWord(0x28, 0xf);
					this.processor.EmitLocalWord(0x2c, 0);
				}

				if ((short)this.processor.PullLocalWord(0x28) >= 8 &&
					(short)(0xf - this.processor.PullLocalWord(0x2a)) <= (short)this.processor.PullLocalWord(0x28) &&
					this.processor.PullLocalWord(0x4c) == 0)
				{
					if (this.processor.PullLocalWord(0x56) == 0)
						return this.processor.PullLocalWord(0x2c) == 0 ? (ushort)0x62 : this.processor.PullLocalWord(0x2c);

					AttachStrategicLocation(participantIdx, this.processor.PullLocalWord(0x30), this.processor.PullLocalWord(0x36), 0, 2);
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
			if (this.processor.PullLocalWord(0x56) != 0 || this.owner.World.HardnessTier == 0)
				return false;

			ushort nearest = this.processor.PullLocalWord(0x34);
			this.processor.RegSource.Value16 = nearest < 128 ? (ushort)this.owner.World.Towns[nearest].ParticipantIdx : (ushort)0xffff;
			short person = this.owner.World.PersonParticipantIdx;

			if ((ushort)person != this.processor.RegSource.Value16 || this.processor.PullLocalWord(0x4c) != 0 ||
				(short)this.processor.PullLocalWord(0x2a) <= 1 || (ushort)participantIdx == this.processor.RegSource.Value16 ||
				this.owner.World.Participants[participantIdx].FoundScienceTally >= this.owner.World.Participants[person].FoundScienceTally)
			{
				return false;
			}

			this.owner.VideoCard.ObtainDot(2, (short)this.processor.PullLocalWord(0x30) + 0x50, (short)this.processor.PullLocalWord(0x36) + 0x32);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			return (short)this.processor.RegAcc.Value16 >= 9 &&
				(short)(0xe - this.processor.PullLocalWord(0x2a)) <= (short)this.processor.PullLocalWord(0x3a);
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

			this.processor.EmitLocalWord(0x28, 0xffff);

			for (this.processor.EmitLocalWord(0xc, 0); (short)this.processor.PullLocalWord(0xc) < 0x80;
				this.processor.EmitLocalWord(0xc, (ushort)(this.processor.PullLocalWord(0xc) + 1)))
			{
				Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0xc)];

				// The score starts as the owner: a dead city or one with bit 0 of the second word is compared with that
				this.processor.EmitLocalWord(0x3a, (ushort)town.ParticipantIdx);

				if (town.ConditionMarker != 0xff && (town.StructureMarkers1 & 1) == 0 && IsNuclearTarget(participantIdx, town))
					ScoreNuclearTarget(participantIdx, town);

				if ((short)this.processor.PullLocalWord(0x3a) > (short)this.processor.PullLocalWord(0x28) && HasCityNear(participantIdx, town))
				{
					this.processor.CopyLocalWord(0x3a, 0x28);
					this.processor.CopyLocalWord(0xc, 0x34);
				}
			}

			if ((short)this.processor.PullLocalWord(0x28) < 0xa || player.LiveTroops[25] <= 1)
				return 0x20;

			this.processor.CopyLocalWord(0x34, 0xc);
			Game.State.City target = this.owner.World.Towns[this.processor.PullLocalWord(0xc)];
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);

			for (this.processor.EmitLocalWord(0x40, 1); (short)this.processor.PullLocalWord(0x40) <= 8;
				this.processor.EmitLocalWord(0x40, (ushort)(this.processor.PullLocalWord(0x40) + 1)))
			{
				ushort direction = this.processor.PullLocalWord(0x40);
				this.processor.EmitLocalWord(0x26, (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) +
					this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882))));
				this.processor.EmitLocalWord(0x2e, (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) +
					this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4))));

				if (this.owner.MapOps.ObtainTileOwner((short)this.processor.PullLocalWord(0x26), (short)this.processor.PullLocalWord(0x2e)) != 0xffff)
					continue;

				this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, (short)this.processor.PullLocalWord(0x30), (short)this.processor.PullLocalWord(0x36));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, this.processor.PullLocalByte(0x26));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, this.processor.PullLocalByte(0x2e));
				this.owner.MapOps.SituateUnitOnTile(participantIdx, troopIdx, (short)this.processor.PullLocalWord(0x26), (short)this.processor.PullLocalWord(0x2e));

				if (this.owner.World.Towns[this.processor.PullLocalWord(0x34)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
					player.TouchParticipantTimeleft = -2;

				return (ushort)(this.processor.PullLocalWord(0x40) ^ 4);
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

			this.processor.EmitLocalWord(0x5e, (ushort)player.MartialMight);

			bool weighed = (short)(3 * (short)this.processor.PullLocalWord(0x5e)) < ownerMight || (player.Relations[owner] & 8) != 0;

			if (!weighed)
			{
				if (this.owner.World.Participants[this.processor.PullLocalWord(0x3a)].LiveTroops[25] != 0 ||
					(short)this.processor.PullLocalWord(0x5e) >= ownerMight)
				{
					return false;
				}
			}

			return (player.Relations[this.processor.PullLocalWord(0x3a)] & 0x82) == 0x80 && town.TrueExtent > 4;
		}

		/// <summary>The score of a target: the tiles around it (+1 an enemy's, -2 own, -99 another player's) and half its size</summary>
		private void ScoreNuclearTarget(short participantIdx, Game.State.City town)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];

			this.processor.EmitLocalWord(0x3a, 0);

			for (this.processor.EmitLocalWord(0x40, 0); (short)this.processor.PullLocalWord(0x40) <= 8;
				this.processor.EmitLocalWord(0x40, (ushort)(this.processor.PullLocalWord(0x40) + 1)))
			{
				ushort direction = this.processor.PullLocalWord(0x40);
				this.processor.EmitLocalWord(0x26, (ushort)(town.Place.X + this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882))));
				this.processor.EmitLocalWord(0x2e, (ushort)(town.Place.Y + this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4))));

				this.processor.EmitLocalWord(0x20, this.owner.MapOps.ObtainTileOwner((short)this.processor.PullLocalWord(0x26), (short)this.processor.PullLocalWord(0x2e)));
				ushort tileOwner = this.processor.PullLocalWord(0x20);

				if (tileOwner == 0xffff)
					continue;

				short delta = (player.Relations[tileOwner] & 0x82) == 0x80 ? (short)1 : tileOwner == (ushort)participantIdx ? (short)-2 : (short)-0x63;
				this.processor.EmitLocalWord(0x3a, (ushort)(this.processor.PullLocalWord(0x3a) + delta));
			}

			this.processor.EmitLocalWord(0x3a, (ushort)(this.processor.PullLocalWord(0x3a) + town.TrueExtent / 2));
		}

		/// <summary>An own city of the player within 16 tiles of the target (0x3e), searched with 0x22</summary>
		private bool HasCityNear(short participantIdx, Game.State.City target)
		{
			this.processor.EmitLocalWord(0x3e, 0);

			for (this.processor.EmitLocalWord(0x22, 0); (short)this.processor.PullLocalWord(0x22) < 0x80;
				this.processor.EmitLocalWord(0x22, (ushort)(this.processor.PullLocalWord(0x22) + 1)))
			{
				Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x22)];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				if ((short)this.owner.Helpers.ObtainDistance(target.Place.X, town.Place.X, target.Place.Y, town.Place.Y) > 0x10)
					continue;

				this.processor.EmitLocalWord(0x3e, 1);
				break;
			}

			return this.processor.PullLocalWord(0x3e) != 0;
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
			short range = (short)(CPU.SignedQuotient((short)UnitKindTable.Field(this.processor, kind, UnitKindTable.ShiftTally), 2) * 3);

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

			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0x4, 0xffff);
			this.processor.EmitLocalWord(0x40, 0);

			do
			{
				short bomberIdx = (short)this.processor.PullLocalWord(0x40);

				if (this.troops.Pull(person, bomberIdx, UnitMemory.Kind) == 0xf &&
					((1 << (byte)participantIdx) & this.troops.PullSigned(person, bomberIdx, UnitMemory.ShownToParticipants)) != 0)
				{
					short distance = (short)this.owner.Helpers.ObtainDistance(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(person, bomberIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), this.troops.PullSigned(person, bomberIdx, UnitMemory.Vert));
					this.processor.EmitLocalWord(0x12, (ushort)distance);

					if ((short)this.processor.PullLocalWord(0x12) < (short)this.processor.PullLocalWord(0x14))
					{
						this.processor.CopyLocalWord(0x12, 0x14);
						this.processor.CopyLocalWord(0x40, 0x4);
					}
				}

				this.processor.EmitLocalWord(0x40, (ushort)(this.processor.PullLocalWord(0x40) + 1));
			}
			while ((short)this.processor.PullLocalWord(0x40) < 0x80);

			// The last distance measured, as in the original (not the nearest one)
			if (this.processor.PullLocalWord(0x4) == 0xffff || (short)this.processor.PullLocalWord(0x12) <= 1)
				return null;

			short bomber = (short)this.processor.PullLocalWord(0x4);

			if ((short)UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) >
				(short)this.processor.PullLocalWord(0x14))
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.troops.Pull(person, bomber, UnitMemory.Horiz));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.troops.Pull(person, bomber, UnitMemory.Vert));
				return null;
			}

			this.processor.EmitLocalWord(0x14, 0x3e7);
			this.processor.EmitLocalWord(0x34, 0xffff);

			for (this.processor.EmitLocalWord(0x40, 0); (short)this.processor.PullLocalWord(0x40) < 0x80;
				this.processor.EmitLocalWord(0x40, (ushort)(this.processor.PullLocalWord(0x40) + 1)))
			{
				Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x40)];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				short reach = (short)this.owner.Helpers.ObtainDistance(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), town.Place.Y);

				if ((short)UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) < reach)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(
					this.troops.PullSigned(person, (short)this.processor.PullLocalWord(0x4), UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(person, (short)this.processor.PullLocalWord(0x4), UnitMemory.Vert), town.Place.Y);
				this.processor.EmitLocalWord(0x12, (ushort)distance);

				if ((short)this.processor.PullLocalWord(0x12) < (short)this.processor.PullLocalWord(0x14))
				{
					this.processor.CopyLocalWord(0x12, 0x14);
					this.processor.CopyLocalWord(0x40, 0x34);
				}
			}

			if (this.processor.PullLocalWord(0x34) == 0xffff)
				return 0x20;

			Game.State.City landing = this.owner.World.Towns[this.processor.PullLocalWord(0x34)];
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
			this.owner.UnitOps.MayOccupyTile(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x2c, 0);
			this.processor.EmitLocalWord(0x30, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz));
			this.processor.EmitLocalWord(0x36, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
			this.processor.EmitLocalWord(0x8, (ushort)this.troops.TroopKind(participantIdx, troopIdx));

			short horizLoc = (short)this.processor.PullLocalWord(0x30);
			short vertLoc = (short)this.processor.PullLocalWord(0x36);

			this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2a, this.processor.PullGlobalWord(0x6c9a));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.owner.Helpers.LocateNearestUnit(participantIdx, troopIdx, horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x32, this.processor.PullGlobalWord(0x6c9a));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, this.processor.PullLocalByte(0x8));

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x56, UnitKindTable.Field(this.processor, this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup));

			this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].Approach);
			this.processor.EmitLocalWord(0x50, (ushort)(1 << (byte)participantIdx));

			// The registers as the original leaves them for the code that follows
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
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

			if (participantIdx == this.processor.PullGlobalSignedWord(0x6b90) && participantIdx != this.owner.World.PersonParticipantIdx &&
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

			if (town.PresentMakingIdx >= 0)
			{
				this.owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]--;
			}

			this.owner.Advisors.PickCityProduction((short)participantIdx, townIdx);

			town.PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			if (town.PresentMakingIdx >= 0)
			{
				this.owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]++;
			}

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
			byte unitYByte = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d6));
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
				this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d5)),
				this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d6)));
			ushort unitX = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d5));
			ushort unitY = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d6));
			byte unitKind = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d7));
			ushort unitSpecOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)unitKind);

			if (this.processor.PullUInt16(dataSeg, (ushort)(unitSpecOffset + 0x1148)) == 0x5)
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
			if (this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81de)) == 0xff)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
				return 0x20;
			}

			ushort nearestCity = this.owner.Helpers.LocateNearestCity((short)unitX, (short)unitY);
			for (ushort dir = 1; (short)dir <= 8; dir++)
			{
				ushort dirOffset = (ushort)(dir << 1);
				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					this.processor.PullInt16(dataSeg, (ushort)(dirOffset + 0x1882)) + (short)unitX);
				ushort adjY = (ushort)(this.processor.PullUInt16(dataSeg, (ushort)(dirOffset + 0x18e4)) + unitY);

				if ((short)this.processor.PullGlobalWord(0x6c9a) <= 8 &&
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
							this.processor.EmitUInt8(
								dataSeg,
								(ushort)(unitAddr + 0x81dd),
								(byte)(this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81dd)) | humanBitByte));

							this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx, (short)adjX, (short)adjY);
							this.processor.EmitGlobalWord(0x2f9e, 0x3);
							this.owner.Libc.CopyText(0xba06, "Barbarian raiding party\nlands near ");
							nearestCity = this.owner.Helpers.LocateNearestCity((short)adjX, (short)adjY);
							this.owner.Conquest.ObtainCityLabel((short)nearestCity);
							this.owner.Libc.AppendText(0xba06, "!\nCitizens are alarmed.\n");
							this.owner.TurnHooks.DisplayDialog(0xba06, 100, 32);
						}
					}

					byte tileSight = (byte)this.owner.World.ChartSight[adjX, adjY];
					this.processor.EmitUInt8(
						dataSeg,
						(ushort)(unitAddr + 0x81dd),
						(byte)(this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81dd)) | tileSight));
					this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81d8), 0x0);
					return 0x75;
				}

				if ((short)this.owner.MapOps.ObtainTileOwner((short)adjX, (short)adjY) > 0)
				{
					return dir;
				}
			}

			if (this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81da)) == 0xff)
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

				this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81da), (byte)(sbyte)this.owner.World.Towns[bestCity].Place.X);
				this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81db), (byte)(sbyte)this.owner.World.Towns[bestCity].Place.Y);
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
			if (this.processor.PullGlobalWord(0x6c9a) == 0)
			{
				if (this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81de)) == 0xff)
				{
					return 0x66;
				}

				byte curKind = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d7));
				ushort curSpecOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)curKind);
				if (this.processor.PullUInt16(dataSeg, (ushort)(curSpecOffset + 0x1148)) == 0x2)
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
				if (unitContinent != cityContinent || (short)this.processor.PullGlobalWord(0x6c9a) > 8)
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
				this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81da), (byte)(sbyte)this.owner.World.Towns[nearestCity].Place.X);
				this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81db), (byte)(sbyte)this.owner.World.Towns[nearestCity].Place.Y);
				ushort gotoDir = this.owner.Routing.CarryOutGoto(participantIdx, troopIdx);
				this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81dc), (byte)(gotoDir & 0xff));
			}

			this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81da), 0xff);
			if (this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d7)) == 0x1a)
			{
				ushort currentTerrain = this.owner.MapOps.ObtainTerrainType(
					this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d5)),
					this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d6)));
				byte nextInStack = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81de));
				if (nextInStack != 0xff && currentTerrain != 0xa)
				{
					ushort nextUnitOffset = (ushort)((short)(sbyte)0xc * (short)(sbyte)nextInStack);
					ushort nextUnitAddr = (ushort)((ushort)participantIdx * 0x600 + nextUnitOffset);
					if (this.processor.PullUInt8(dataSeg, (ushort)(nextUnitAddr + 0x81d7)) != 0x1a)
					{
						return 0x20;
					}
				}

				short cityDist = (short)this.processor.PullGlobalWord(0x6c9a);
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
						this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d5)),
						this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d6)));
					if ((short)this.processor.PullGlobalWord(0x6c9a) <= 3)
					{
						ushort nearestUnitAddr = (ushort)((ushort)participantIdx * 0x600 + nearestUnit * 0xc);
						if (this.processor.PullUInt8(dataSeg, (ushort)(nearestUnitAddr + 0x81d7)) != 0x1a)
						{
							this.processor.EmitUInt8(
								dataSeg,
								(ushort)(unitAddr + 0x81da),
								this.processor.PullUInt8(dataSeg, (ushort)(nearestUnitAddr + 0x81d5)));
							this.processor.EmitUInt8(
								dataSeg,
								(ushort)(unitAddr + 0x81db),
								this.processor.PullUInt8(dataSeg, (ushort)(nearestUnitAddr + 0x81d6)));
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
					this.processor.PullInt8(dataSeg, (ushort)(unitAddr + 0x81d5)) +
					this.processor.PullInt16(dataSeg, (ushort)(dirOffset + 0x1882)));
				ushort adjY = (ushort)(this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d6)) +
					this.processor.PullUInt16(dataSeg, (ushort)(dirOffset + 0x18e4)));

				ushort adjTerrain = this.owner.MapOps.ObtainTerrainType((short)adjX, (short)adjY);
				ushort isOcean = adjTerrain == 0xa ? (ushort)1 : (ushort)0;

				byte curKind = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d7));
				ushort curSpecOff = (ushort)((short)(sbyte)0x22 * (short)(sbyte)curKind);
				ushort isSeaDomain = this.processor.PullUInt16(dataSeg, (ushort)(curSpecOff + 0x1138)) == 0x2 ? (ushort)1 : (ushort)0;
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

				if (this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d7)) == 0x1a)
				{
					if (unitAtAdj != 0xffff)
					{
						ushort unitOwnerAtAdj = this.processor.PullGlobalWord(0xd7f0);
						dirScore = (short)(dirScore + ((ushort)participantIdx == unitOwnerAtAdj ? (short)0x63 : unchecked((short)0xff9d)));
					}

					this.owner.Helpers.LocateNearestCity((short)adjX, (short)adjY);
					short randBonus = (short)this.owner.Libc.Dice.DrawInt(4);
					short defenseBonus = (short)(sbyte)this.processor.PullUInt8(dataSeg, (ushort)(adjTerrain * 0x13 + 0x28e));
					short distPenalty = (short)(this.processor.PullGlobalWord(0x6c9a) << 2);
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
						ushort adjUnitOwner = this.processor.PullGlobalWord(0xd7f0);
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
							if (this.processor.PullUInt8(dataSeg, (ushort)(adjUnitAddr + 0x81d7)) != 0x1a)
							{
								dirScore = (short)(dirScore + 0x63);
							}
						}
					}

					if (mayOccupyCurrent != 0)
					{
						byte kindNow = this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81d7));
						ushort specNow = (ushort)((short)(sbyte)0x22 * (short)(sbyte)kindNow);
						if (this.processor.PullUInt16(dataSeg, (ushort)(specNow + 0x1138)) == 0 &&
							unitAtAdj == 0xffff &&
							this.owner.UnitOps.MayOccupyTile(participantIdx, (short)adjX, (short)adjY) != 0)
						{
							continue;
						}
					}

					short prevDir = (short)(sbyte)this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81dc));
					if ((ushort)prevDir == dir)
					{
						dirScore = (short)(dirScore + 0x6);
					}

					if (this.processor.PullUInt16(dataSeg, (ushort)(dirOffset + 0x1882)) == targetDirX)
					{
						dirScore = (short)(dirScore + 0x2);
					}

					if (this.processor.PullUInt16(dataSeg, (ushort)(dirOffset + 0x18e4)) == targetDirY)
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
				this.processor.PullUInt8(dataSeg, (ushort)(unitAddr + 0x81de)) == 0xff &&
				bestDirScore < 0x63)
			{
				return 0x20;
			}

			this.processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81dc), (byte)(bestDir & 0xff));
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
