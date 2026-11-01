using System.Linq;

namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		public void ReimposeStrategicLocations(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ReimposeStrategicLocations({participantIndex})");

			for (int entryIndex = 0; entryIndex < 0x20; entryIndex++)
			{
				ushort entry = (ushort)((participantIndex << 5) + entryIndex);

				this.Processor.EmitGlobalByte((ushort)(entry + IUnitPicker.StrategicLocationRoleTableAddress), 0xff);
				this.Processor.EmitGlobalByte((ushort)(entry + IUnitPicker.StrategicLocationPriorityTableAddress), 0x0);
			}

			for (int siteIndex = 0; siteIndex < 0x10; siteIndex++)
			{
				StrategicLocation site = this.Owner.World.Participants[participantIndex].TacticalSites[siteIndex];

				if (site.Live != -1 && site.Live <= 2)
				{
					SendUnitToStrategicLocation(participantIndex, (short)site.Place.X, (short)site.Place.Y, site.Live, site.Strategy);
				}
			}
		}

		public void SendUnitToStrategicLocation(short participantIndex, short horizontalLocation, short verticalLocation, short live, short strategy)
		{
			using var _ = this.Processor.Journal.Scope($"SendUnitToStrategicLocation({participantIndex}, {horizontalLocation}, {verticalLocation}, {live}, {strategy})");

			for (int placeIndex = 0; placeIndex < 0x20; placeIndex++)
			{
				ushort place = (ushort)((participantIndex << 5) + placeIndex);

				if (this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationHorizontalTableAddress)) == horizontalLocation &&
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationVerticalTableAddress)) == verticalLocation &&
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationRoleTableAddress)) == live &&
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationPriorityTableAddress)) <= strategy)
				{
					return;
				}
			}

			if (participantIndex == (short)Turn.Player && participantIndex != this.Owner.World.PersonParticipantIndex &&
				(live == 3 || live == 4))
			{
				SendUnitOfRole(participantIndex, horizontalLocation, verticalLocation, live);
			}

			for (int placeIndex = 0; placeIndex < 0x20; placeIndex++)
			{
				ushort place = (ushort)((participantIndex << 5) + placeIndex);

				if (this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationRoleTableAddress)) != 0xff &&
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationPriorityTableAddress)) >= strategy)
				{
					continue;
				}

				FreeStrategicLocation(participantIndex, (ushort)placeIndex);

				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationHorizontalTableAddress), (byte)horizontalLocation);
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationVerticalTableAddress), (byte)verticalLocation);
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationRoleTableAddress), (byte)live);
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationPriorityTableAddress), (byte)strategy);
				break;
			}
		}

		public void FreeStrategicLocation(short participantIndex, ushort place)
		{
			using var _ = this.Processor.Journal.Scope($"FreeStrategicLocation({participantIndex}, {place})");

			for (int index = 30; index >= (short)place; index--)
			{
				int entry = (participantIndex << 5) + index;

				if (this.Processor.PullGlobalByte((ushort)(IUnitPicker.StrategicLocationPriorityTableAddress + entry)) == 0)
				{
					continue;
				}

				MoveListEntryOn(IUnitPicker.StrategicLocationHorizontalTableAddress, entry);
				MoveListEntryOn(IUnitPicker.StrategicLocationVerticalTableAddress, entry);
				MoveListEntryOn(IUnitPicker.StrategicLocationRoleTableAddress, entry);
				MoveListEntryOn(IUnitPicker.StrategicLocationPriorityTableAddress, entry);
			}
		}

		public void ClearStrategicLocations(short participantIndex)
		{
			for (int index = 0; index < 16; index++)
			{
				this.Owner.World.Participants[participantIndex].TacticalSites[index].Live = -1;
				this.Owner.World.Participants[participantIndex].TacticalSites[index].Strategy = 0;
			}
		}

		public void AttachStrategicLocation(short participantIndex, ushort horizontalLocation, ushort verticalLocation, ushort live, ushort strategy)
		{
			using var _ = this.Processor.Journal.Scope($"AttachStrategicLocation({participantIndex}, {horizontalLocation}, {verticalLocation}, {live}, {strategy})");

			StrategicLocation[] sites = this.Owner.World.Participants[participantIndex].TacticalSites;

			bool known = sites.Take(0x10).Any(site => (ushort)(sbyte)site.Place.X == horizontalLocation && (ushort)(sbyte)site.Place.Y == verticalLocation &&
				(ushort)site.Live == live && (sbyte)site.Strategy <= (short)strategy);

			if (!known)
			{
				for (ushort place = 0; place < 0x10; place++)
				{
					if (sites[place].Live == -1 || (sbyte)sites[place].Strategy < (short)strategy)
					{
						AffixStrategicLocation(participantIndex, place);

						sites[place].Place = new Point((sbyte)horizontalLocation, sites[place].Place.Y);
						sites[place].Place = new Point(sites[place].Place.X, (sbyte)verticalLocation);
						sites[place].Live = (sbyte)live;
						sites[place].Strategy = (byte)strategy;
						break;
					}
				}
			}
		}

		public void AffixStrategicLocation(short participantIndex, ushort place)
		{
			using var _ = this.Processor.Journal.Scope($"AffixStrategicLocation({participantIndex}, {place})");

			StrategicLocation[] sites = this.Owner.World.Participants[participantIndex].TacticalSites;

			for (int index = 14; index >= (short)place; index--)
			{
				if (sites[index].Strategy == 0)
				{
					continue;
				}

				sites[index + 1].Place = new Point(sites[index].Place.X, sites[index + 1].Place.Y);
				sites[index + 1].Place = new Point(sites[index + 1].Place.X, sites[index].Place.Y);
				sites[index + 1].Live = sites[index].Live;
				sites[index + 1].Strategy = sites[index].Strategy;
			}
		}

		public void DisableStrategicLocation(short participantIndex, ushort role, int horizontalLocation, int verticalLocation, ushort reach)
		{
			using var _ = this.Processor.Journal.Scope($"DisableStrategicLocation({participantIndex}, {role}, {horizontalLocation}, {verticalLocation}, {reach})");

			StrategicLocation[] sites = this.Owner.World.Participants[participantIndex].TacticalSites;

			for (int index = 0; index < 16; index++)
			{
				if ((ushort)sites[index].Live != role)
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizontalLocation, sites[index].Place.X, verticalLocation, sites[index].Place.Y);

				if (distance <= (short)reach)
				{
					sites[index].Live = -1;
				}
			}
		}

		public void RefreshCityProduction(short participantIndex, ushort continent)
		{
			using var _ = this.Processor.Journal.Scope($"RefreshCityProduction({participantIndex}, {continent})");

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if ((byte)town.ParticipantIndex != (ushort)participantIndex || town.ConditionMarker == 0xff)
				{
					continue;
				}

				if (continent != 0xffff && this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y) != continent)
				{
					continue;
				}

				PickNewCityProduction(townIndex);
			}
		}

		public void PickNewCityProduction(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"PickNewCityProduction({townIndex})");

			City town = this.Owner.World.Towns[townIndex];
			ushort participantIndex = (byte)town.ParticipantIndex;

			this.Owner.World.Participants[participantIndex].StopMaking(town.PresentMakingIndex);
			town.PresentMakingIndex = (sbyte)this.Owner.Advisors.PickCityProduction((short)participantIndex, townIndex);
			this.Owner.World.Participants[participantIndex].StartMaking(town.PresentMakingIndex);
		}

		public ushort MayComputerEnterTile(short participantIndex, ushort horizontalLocation, ushort verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"MayAIEnterTile({participantIndex}, {horizontalLocation}, {verticalLocation})");

			short bestHeading = -1;
			short bestShore = -1;
			IMapPlanes chart = this.Owner.MapOperations.Chart;

			for (short heading = 0; heading <= 8; heading++)
			{
				short oceanHorizontal = (short)(chart.DirectionHorizontal(heading) + horizontalLocation);
				short oceanVertical = (short)(chart.DirectionVertical(heading) + verticalLocation);

				if (this.Owner.MapOperations.ObtainTerrainType(oceanHorizontal, oceanVertical) != MapRules.OceanTerrain)
				{
					continue;
				}

				if (heading != 0 && this.Owner.MapOperations.ObtainTileOwner(oceanHorizontal, oceanVertical) != 0xffff)
				{
					continue;
				}

				short shore = 0;

				for (int around = 1; around <= 8; around++)
				{
					short landHorizontal = (short)(chart.DirectionHorizontal(around) + oceanHorizontal);
					short landVertical = (short)(chart.DirectionVertical(around) + oceanVertical);

					if (this.Owner.MapOperations.ObtainTerrainType(landHorizontal, landVertical) == MapRules.OceanTerrain ||
						this.Owner.MapOperations.ObtainTileOwner(landHorizontal, landVertical) != 0xffff)
					{
						continue;
					}

					shore++;

					if ((this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] & 2) != 0 &&
						this.Owner.VideoCard.ObtainDot(2, landHorizontal + 0x50, landVertical) == (ushort)this.Owner.World.PersonParticipantIndex)
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

			return (ushort)bestHeading;
		}

		private void SendUnitOfRole(short participantIndex, short horizontalLocation, short verticalLocation, short live)
		{
			for (int troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xff ||
					this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) == 0 ||
					this.troops.KindField(participantIndex, troopIndex, UnitKindTable.TroopGroup) != (ushort)live)
				{
					continue;
				}

				sbyte troopHorizontal = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
				sbyte troopVertical = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

				if (this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation) != this.Owner.MapOperations.ObtainContinentIdentifier(troopHorizontal, troopVertical))
				{
					continue;
				}

				int distance = this.Owner.Helpers.ObtainDistance(troopHorizontal, horizontalLocation, troopVertical, verticalLocation);

				if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) < (short)(distance << 1))
				{
					continue;
				}

				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)horizontalLocation);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)verticalLocation);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status,
					(byte)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 0x10) & 0xf0));
				return;
			}
		}

		private void MoveListEntryOn(ushort origin, int entry)
		{
			this.Processor.EmitGlobalByte((ushort)(origin + entry + 1), this.Processor.PullGlobalByte((ushort)(origin + entry)));
		}
	}
}
