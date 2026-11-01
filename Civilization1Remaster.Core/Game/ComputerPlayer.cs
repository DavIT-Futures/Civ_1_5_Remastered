namespace Civilization1Remaster.Core.Game
{
	public partial class ComputerPlayer : GameComponent, IComputerPlayer
	{
		private const ushort BlockSegment = 0x3772;

		private const ushort BlockOrigin = 0x2438;

		private readonly UnitMemory troops;

		private UnitLook look = new UnitLook();

		public ComputerPlayer(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

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

		private static ushort LocationRole(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationRoleTableAddress + (participantIdx << 5) + locationIdx);

		private static ushort LocationHoriz(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationHorizTableAddress + (participantIdx << 5) + locationIdx);

		private static ushort LocationVert(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationVertTableAddress + (participantIdx << 5) + locationIdx);

		private static ushort LocationPriority(int participantIdx, int locationIdx) => (ushort)(UnitPicker.StrategicLocationPriorityTableAddress + (participantIdx << 5) + locationIdx);

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

			player.MartialMight += (short)(Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) +
				Tables.UnitKindWord(kind, UnitKindTable.GuardPower));

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

				player.Landmasses[continentIdx].Assault += (short)Tables.UnitKindWord(kind, UnitKindTable.GuardPower);
				player.Landmasses[continentIdx].Guard += (short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower);
			}
			else if (Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status | 0x10));
			}

			byte role = (byte)Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup);
			byte roleBit = (byte)(2 << role);
			ushort block = Block(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
			this.Processor.EmitUInt8(BlockSegment, block, (byte)(this.Processor.PullUInt8(BlockSegment, block) | roleBit));
		}

		private ushort CountCities(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

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
						SendUnitToStrategicLocation(participantIdx, (short)town.Place.X, (short)town.Place.Y, 1,
							(short)(town.Has(CityStructure.CityWalls) ? 3 : 5));
					}

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

			for (ushort unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
			{
				if ((byte)town.Unfamiliar[unfamiliarIdx] != 0xff)
				{
					sbyte kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);

					player.MartialMight = (short)(Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) +
						Tables.UnitKindWord(kind, UnitKindTable.GuardPower));

					ushort continentIdx = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

					kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIdx] & 0x3f);

					player.Landmasses[continentIdx].Assault = (short)Tables.UnitKindWord(kind, UnitKindTable.GuardPower);
					player.Landmasses[continentIdx].Guard += (short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower);
				}
			}
		}

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

		private void ChooseContinentApproaches(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			player.Landmasses[0].Approach = 0;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)((participantIdx << 5) + 0xe6b6), 0);
			Turn.HumanContinentAttackers = (ushort)(Turn.HumanContinentAttackers & ~(1 << participantIdx));

			Turn.PlayerAtWar = 0;

			for (ushort otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if ((player.Relations[otherIdx] & 3) == 1)
					Turn.PlayerAtWar = 1;
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
					presence |= (ushort)((player.Relations[otherIdx] & 2) != 0 ? 1 : 2);
				}
			}

			if (this.Owner.World.Participants[0].Landmasses[continentIdx].TownTally != 0)
			{
				threats++;
			}

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
				Turn.HumanContinentAttackers = (ushort)(Turn.HumanContinentAttackers | (1 << participantIdx));
		}

		private void ChooseGovernment(short participantIdx)
		{
			if ((short)Turn.AnarchyPressure > 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIdx, 1);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2c) != 0 && (short)Turn.DemocracyPressure >= 0)
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

		private bool GiveUpObsoleteUnit(short participantIdx, short troopIdx, out short horizLoc, out short vertLoc, out ushort continentIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			vertLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			continentIdx = this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc);

			if ((((byte)troopIdx + (byte)this.Owner.World.RoundTally) & 0xf) != 0)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)Tables.UnitKindWord(kind, 0xc)) == 0)
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

		private bool LocationFits(short participantIdx, short troopIdx, short locationIdx, ushort continentIdx)
		{
			byte role = this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationRole(participantIdx, locationIdx));

			if (role == 0xff)
				return false;

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (Tables.UnitKindWord(kind, UnitKindTable.TroopGroup) == (ushort)(sbyte)role &&
				this.Owner.MapOps.ObtainContinentID(this.Processor.PullInt8(this.Processor.SegData.Value16, LocationHoriz(participantIdx, locationIdx)),
					this.Processor.PullInt8(this.Processor.SegData.Value16, LocationVert(participantIdx, locationIdx))) == continentIdx)
			{
				return true;
			}

			byte kindByte = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kindByte != 0xf)
				return false;

			sbyte bomberRole = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, LocationRole(participantIdx, locationIdx));

			return Tables.UnitKindWord((sbyte)kindByte, UnitKindTable.TroopGroup) == (ushort)bomberRole ||
				bomberRole == 3;
		}

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

				short range = (short)Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally);
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
