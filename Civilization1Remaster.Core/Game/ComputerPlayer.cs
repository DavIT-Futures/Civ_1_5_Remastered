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

		public void OperateComputerTurn(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"OperateAITurn({participantIndex})");

			CountUnits(participantIndex);
			ushort sentBase = CountCities(participantIndex);
			CountUsedBlocks(participantIndex);
			ChooseContinentApproaches(participantIndex);

			if (participantIndex != 0 && !this.Owner.World.IsHuman(participantIndex))
			{
				Player player = this.Owner.World.Participants[participantIndex];

				if (player.ColonistTally == 0 && player.TownTally == 0 && participantIndex != this.Owner.World.PersonParticipantIndex)
				{
					this.Owner.NewGameWizard.DestroyCivilization(participantIndex, 0);
				}

				if ((((byte)participantIndex + (byte)this.Owner.World.RoundTally) & 7) == 0)
				{
					ChooseGovernment(participantIndex);
				}

				DirectUnits(participantIndex, sentBase);
			}
		}

		private static ushort Block(int horizontalLocation, int verticalLocation)
		{
			return (ushort)(BlockOrigin + ((horizontalLocation >> 2) * 0xd) + (verticalLocation >> 2));
		}

		private static ushort LocationRole(int participantIndex, int locationIndex) => (ushort)(UnitPicker.StrategicLocationRoleTableAddress + (participantIndex << 5) + locationIndex);

		private static ushort LocationHorizontal(int participantIndex, int locationIndex) => (ushort)(UnitPicker.StrategicLocationHorizontalTableAddress + (participantIndex << 5) + locationIndex);

		private static ushort LocationVertical(int participantIndex, int locationIndex) => (ushort)(UnitPicker.StrategicLocationVerticalTableAddress + (participantIndex << 5) + locationIndex);

		private static ushort LocationPriority(int participantIndex, int locationIndex) => (ushort)(UnitPicker.StrategicLocationPriorityTableAddress + (participantIndex << 5) + locationIndex);

		private void CountUnits(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			for (ushort continentIndex = 0; continentIndex < 0x10; continentIndex++)
			{
				player.Landmasses[continentIndex].TownTally = 0;
				player.Landmasses[continentIndex].Guard = 0;
				player.Landmasses[continentIndex].Assault = 0;
			}

			for (ushort blockHorizontal = 0; blockHorizontal < 0x14; blockHorizontal++)
			{
				for (ushort blockVertical = 0; blockVertical < 0xd; blockVertical++)
				{
					this.Processor.EmitByte(BlockSegment, (ushort)(BlockOrigin + (blockHorizontal * 0xd) + blockVertical), 0);
				}
			}

			player.MartialMight = 0;
			player.ColonistTally = 0;
			player.TroopTally = 0;

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

				if (kind != 0xff && kind != 0x19)
				{
					if (kind == 0)
					{
						player.ColonistTally++;
					}
					else
					{
						player.TroopTally++;
					}

					CountUnit(participantIndex, troopIndex);
				}
			}
		}

		private void CountUnit(short participantIndex, short troopIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			player.MartialMight += (short)(Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) +
				Tables.UnitKindWord(kind, UnitKindTable.GuardPower));

			byte status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(status & 0xef));

			if (this.Owner.UnitOperations.CheckTileOccupation(participantIndex, this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) != 0)
			{
				status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(status | 0x10));
			}

			if (this.Owner.MapOperations.ObtainTerrainType(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) != MapRules.OceanTerrain)
			{
				ushort continentIndex = this.Owner.MapOperations.ObtainContinentIdentifier(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

				kind = this.troops.TroopKind(participantIndex, troopIndex);

				player.Landmasses[continentIndex].Assault += (short)Tables.UnitKindWord(kind, UnitKindTable.GuardPower);
				player.Landmasses[continentIndex].Guard += (short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower);
			}
			else if (Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(status | 0x10));
			}

			byte role = (byte)Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.TroopGroup);
			byte roleBit = (byte)(2 << role);
			ushort block = Block(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal), this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical));
			this.Processor.EmitByte(BlockSegment, block, (byte)(this.Processor.PullByte(BlockSegment, block) | roleBit));
		}

		private ushort CountCities(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			ushort sentBase = (ushort)this.Owner.Helpers.ClampValue(player.TroopTally / 8, 3, 99);
			player.GrandTownExtent = 0;
			player.TownTally = 0;

			for (int kindIndex = 0; kindIndex < 0x1c; kindIndex++)
			{
				player.TroopsInsideMaking[kindIndex] = 0;
			}

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker != 0xff)
				{
					if (town.ParticipantIndex == participantIndex)
					{
						CountOwnCity(participantIndex, townIndex);
					}
					else if (MayAimAtCity(participantIndex, townIndex))
					{
						SendUnitToStrategicLocation(participantIndex, (short)town.Place.X, (short)town.Place.Y, 1,
							(short)(town.Has(CityStructure.CityWalls) ? 3 : 5));
					}

					if (this.Owner.MapOperations.ObtainUnitAtTile(town.Place.X, town.Place.Y) == 0xffff && (byte)town.Unfamiliar[0] == 0xff)
					{
						SendUnitToStrategicLocation(participantIndex, (short)town.Place.X, (short)town.Place.Y, 2,
							(short)(town.ParticipantIndex == participantIndex ? 4 : 2));
					}
				}
			}

			return sentBase;
		}

		private void CountOwnCity(short participantIndex, short townIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			City town = this.Owner.World.Towns[townIndex];

			player.TownTally++;
			player.Landmasses[this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y)].TownTally++;
			player.GrandTownExtent += town.TrueExtent;

			player.StartMaking(town.PresentMakingIndex);

			ushort block = Block(town.Place.X, town.Place.Y);
			this.Processor.EmitByte(BlockSegment, block, (byte)(this.Processor.PullByte(BlockSegment, block) | 1));

			for (ushort unfamiliarIndex = 0; unfamiliarIndex < 2; unfamiliarIndex++)
			{
				if ((byte)town.Unfamiliar[unfamiliarIndex] != 0xff)
				{
					sbyte kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIndex] & 0x3f);

					player.MartialMight = (short)(Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) +
						Tables.UnitKindWord(kind, UnitKindTable.GuardPower));

					ushort continentIndex = this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y);

					kind = (sbyte)((byte)town.Unfamiliar[unfamiliarIndex] & 0x3f);

					player.Landmasses[continentIndex].Assault = (short)Tables.UnitKindWord(kind, UnitKindTable.GuardPower);
					player.Landmasses[continentIndex].Guard += (short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower);
				}
			}
		}

		private bool MayAimAtCity(short participantIndex, short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			if ((short)(this.Owner.World.RoundTally * this.Owner.World.HardnessTier) <= 200 &&
				((sbyte)this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << participantIndex)) == 0 &&
				this.Owner.World.PersonParticipantIndex == town.ParticipantIndex)
			{
				return false;
			}

			if ((this.Owner.World.Participants[participantIndex].Relations[town.ParticipantIndex] & 0x102) == 2)
			{
				return false;
			}

			return (((byte)townIndex + (byte)this.Owner.World.RoundTally) & 3) != 0;
		}

		private void CountUsedBlocks(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			player.SoilTally = 0;

			for (ushort blockHorizontal = 0; blockHorizontal < 0x14; blockHorizontal++)
			{
				for (ushort blockVertical = 0; blockVertical < 0xd; blockVertical++)
				{
					if (this.Processor.PullByte(BlockSegment, (ushort)(BlockOrigin + (blockHorizontal * 0xd) + blockVertical)) != 0)
					{
						player.SoilTally++;
					}
				}
			}
		}

		private void ChooseContinentApproaches(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			player.Landmasses[0].Approach = 0;
			this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)((participantIndex << 5) + 0xe6b6), 0);
			Turn.HumanContinentAttackers = (ushort)(Turn.HumanContinentAttackers & ~(1 << participantIndex));

			Turn.PlayerAtWar = 0;

			for (ushort otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if ((player.Relations[otherIndex] & 3) == 1)
				{
					Turn.PlayerAtWar = 1;
				}
			}

			for (ushort continentIndex = 1; continentIndex < 0xf; continentIndex++)
			{
				ChooseContinentApproach(participantIndex, continentIndex);
			}
		}

		private void ChooseContinentApproach(short participantIndex, ushort continentIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			PlayerContinent own = player.Landmasses[continentIndex];

			short threats = 0;
			short stronger = 0;
			ushort presence = 0;
			short towns = 0;
			short strength = 0;
			short approach = own.Approach;

			for (ushort otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				PlayerContinent other = this.Owner.World.Participants[otherIndex].Landmasses[continentIndex];

				if (other.Assault != 0 &&
					((short)(own.Guard / 4) < other.Guard || other.TownTally != 0) &&
					((player.Relations[otherIndex] & 3) == 1 || (player.Relations[otherIndex] & 0x100) != 0))
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

				if (other.Assault != 0 && otherIndex == this.Owner.World.PersonParticipantIndex && (player.Relations[otherIndex] & 2) != 0)
				{
					if ((short)(CentralProcessor.SignedQuotient(own.Guard, 2) + own.Assault) < other.Guard)
					{
						stronger++;
					}
				}

				strength += other.Assault;
				towns += other.TownTally;

				if (other.Assault != 0)
				{
					presence |= (ushort)((player.Relations[otherIndex] & 2) != 0 ? 1 : 2);
				}
			}

			if (this.Owner.World.Participants[0].Landmasses[continentIndex].TownTally != 0)
			{
				threats++;
			}

			bool crowded = (short)((own.Assault + strength) << 1) > (short)this.Processor.PullGlobalWord((ushort)((continentIndex << 1) + 0xdcfe)) ||
				(short)(((own.TownTally + towns) * 6) + 2) > this.Owner.World.Landmasses[continentIndex].ConstructionSpotTally;

			own.Approach = (short)(crowded && this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 7) != 0 ? 5 : 0);

			if (threats != 0)
			{
				own.Approach = 1;
			}

			if (stronger != 0)
			{
				own.Approach = 2;
			}

			if (own.Assault == 0 && own.TownTally == 0 && presence != 1)
			{
				own.Approach = 1;
			}

			if (own.Approach != approach && !this.Owner.World.IsHuman(participantIndex))
			{
				RefreshCityProduction(participantIndex, continentIndex);
			}

			if (own.Assault != 0 && this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Landmasses[continentIndex].TownTally > 1)
			{
				Turn.HumanContinentAttackers = (ushort)(Turn.HumanContinentAttackers | (1 << participantIndex));
			}
		}

		private void ChooseGovernment(short participantIndex)
		{
			if ((short)Turn.AnarchyPressure > 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIndex, 1);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x2c) != 0 && (short)Turn.DemocracyPressure >= 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIndex, 4);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x2b) != 0 && this.Owner.World.Participants[participantIndex].TownTally > 10)
			{
				this.Owner.Politics.SwitchGovernment(participantIndex, 3);
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 5) != 0)
			{
				this.Owner.Politics.SwitchGovernment(participantIndex, 2);
			}
		}

		private void DirectUnits(short participantIndex, ushort sentBase)
		{
			ushort[] locationSent = new ushort[0x20];

			for (int locationIndex = 0; locationIndex < 0x20; locationIndex++)
			{
				locationSent[locationIndex] = sentBase;
			}

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

				if (kind != 0xff && (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x10) == 0 && kind != 0x19)
				{
					if (!GiveUpObsoleteUnit(participantIndex, troopIndex, out short horizontalLocation, out short verticalLocation, out ushort continentIndex))
					{
						SendUnitToBestLocation(participantIndex, troopIndex, horizontalLocation, verticalLocation, continentIndex, sentBase, locationSent);
					}
				}
			}
		}

		private bool GiveUpObsoleteUnit(short participantIndex, short troopIndex, out short horizontalLocation, out short verticalLocation, out ushort continentIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			horizontalLocation = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
			verticalLocation = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);
			continentIndex = this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation);

			if ((((byte)troopIndex + (byte)this.Owner.World.RoundTally) & 0xf) != 0)
			{
				return false;
			}

			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, (short)Tables.UnitKindWord(kind, 0xc)) == 0)
			{
				if (player.RegimeKind <= 1 && player.Landmasses[continentIndex].Approach != 5)
				{
					return false;
				}

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 1)
				{
					return false;
				}
			}

			if (this.Owner.UnitOperations.CheckTileOccupation(participantIndex, horizontalLocation, verticalLocation) != 0)
			{
				return false;
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0 &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.FollowingInStack) == 0xff)
			{
				return false;
			}

			if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain ||
				player.Landmasses[continentIndex].Approach == 2)
			{
				return false;
			}

			this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);

			return true;
		}

		private void SendUnitToBestLocation(short participantIndex, short troopIndex, short horizontalLocation, short verticalLocation, ushort continentIndex, ushort sentBase, ushort[] locationSent)
		{
			short bestDistance = 0x270f;
			short best = -1;

			for (short locationIndex = 0; locationIndex < 0x20; locationIndex++)
			{
				if (LocationFits(participantIndex, troopIndex, locationIndex, continentIndex))
				{
					short? distance = WeighLocation(participantIndex, troopIndex, locationIndex, horizontalLocation, verticalLocation, locationSent);

					if (distance != null && MayTakeLocation(participantIndex, troopIndex, locationIndex, distance.Value, sentBase, locationSent) &&
						distance.Value < bestDistance &&
						(short)((sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, LocationPriority(participantIndex, locationIndex)) << 2) >=
							CentralProcessor.SignedQuotient(distance.Value, (short)sentBase))
					{
						bestDistance = distance.Value;
						best = locationIndex;
					}
				}
			}

			if (best == -1)
			{
				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, this.Processor.PullByte(this.Processor.SegmentData.Word, LocationHorizontal(participantIndex, best)));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, this.Processor.PullByte(this.Processor.SegmentData.Word, LocationVertical(participantIndex, best)));

			byte status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(status | 0x10));
			status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(status & 0xf0));

			locationSent[best]++;
		}

		private bool LocationFits(short participantIndex, short troopIndex, short locationIndex, ushort continentIndex)
		{
			byte role = this.Processor.PullByte(this.Processor.SegmentData.Word, LocationRole(participantIndex, locationIndex));

			if (role == 0xff)
			{
				return false;
			}

			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			if (Tables.UnitKindWord(kind, UnitKindTable.TroopGroup) == (ushort)(sbyte)role &&
				this.Owner.MapOperations.ObtainContinentIdentifier(this.Processor.PullSignedByte(this.Processor.SegmentData.Word, LocationHorizontal(participantIndex, locationIndex)),
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, LocationVertical(participantIndex, locationIndex))) == continentIndex)
			{
				return true;
			}

			byte kindByte = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

			if (kindByte != 0xf)
			{
				return false;
			}

			sbyte bomberRole = (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, LocationRole(participantIndex, locationIndex));

			return Tables.UnitKindWord((sbyte)kindByte, UnitKindTable.TroopGroup) == (ushort)bomberRole ||
				bomberRole == 3;
		}

		private short? WeighLocation(short participantIndex, short troopIndex, short locationIndex, short horizontalLocation, short verticalLocation, ushort[] locationSent)
		{
			short weighed;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xf)
			{
				if ((this.Processor.PullByte(this.Processor.SegmentData.Word, LocationPriority(participantIndex, locationIndex)) & 1) == 0 ||
					(((byte)locationIndex + (byte)(this.Owner.World.RoundTally >> 1)) & 1) == 0)
				{
					return null;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizontalLocation,
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, LocationHorizontal(participantIndex, locationIndex)),
					verticalLocation, this.Processor.PullSignedByte(this.Processor.SegmentData.Word, LocationVertical(participantIndex, locationIndex)));

				short range = (short)Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.ShiftTally);
				weighed = (short)((range >= distance ? 2 : 4) * distance);
			}
			else
			{
				short distance = (short)this.Owner.Helpers.ObtainDistance(horizontalLocation,
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, LocationHorizontal(participantIndex, locationIndex)),
					verticalLocation, this.Processor.PullSignedByte(this.Processor.SegmentData.Word, LocationVertical(participantIndex, locationIndex)));

				weighed = (short)(locationSent[locationIndex] * distance);
			}

			sbyte priority = (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, LocationPriority(participantIndex, locationIndex));
			return CentralProcessor.SignedQuotient(weighed, (short)(priority + 1));
		}

		private bool MayTakeLocation(short participantIndex, short troopIndex, short locationIndex, short distance, ushort sentBase, ushort[] locationSent)
		{
			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xc) == 0)
			{
				return true;
			}

			sbyte priority = (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, LocationPriority(participantIndex, locationIndex));

			if (priority < 2)
			{
				return false;
			}

			if ((short)(priority * (short)sentBase) < distance &&
				locationSent[locationIndex] != sentBase)
			{
				return false;
			}

			return (this.Owner.MapOperations.ObtainTileImprovements(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) & 1) == 0;
		}
	}
}
