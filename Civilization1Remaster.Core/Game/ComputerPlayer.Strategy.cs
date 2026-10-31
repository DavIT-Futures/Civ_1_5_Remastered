using System;
using System.Linq;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The strategic locations of the AI players, their city production and the tiles they may enter</summary>
	partial class ComputerPlayer
	{
		/// <inheritdoc/>
		public void ReimposeStrategicLocations(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ReimposeStrategicLocations({participantIdx})");

			for (int entryIdx = 0; entryIdx < 0x20; entryIdx++)
			{
				ushort entry = (ushort)((participantIdx << 5) + entryIdx);

				this.Processor.EmitGlobalByte((ushort)(entry + UnitPicker.StrategicLocationRoleTableAddress), 0xff);
				this.Processor.EmitGlobalByte((ushort)(entry + UnitPicker.StrategicLocationPriorityTableAddress), 0x0);
			}

			for (int siteIdx = 0; siteIdx < 0x10; siteIdx++)
			{
				StrategicLocation site = this.Owner.World.Participants[participantIdx].TacticalSites[siteIdx];

				if (site.Live != -1 && site.Live <= 2)
				{
					SendUnitToStrategicLocation(participantIdx, (short)site.Place.X, (short)site.Place.Y, site.Live, site.Strategy);
				}
			}
		}

		/// <inheritdoc/>
		public void SendUnitToStrategicLocation(short participantIdx, short horizLoc, short vertLoc, short live, short strategy)
		{
			using var _ = this.Processor.Journal.Scope($"SendUnitToStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy})");

			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);

				if (this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationHorizTableAddress)) == horizLoc &&
					this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationVertTableAddress)) == vertLoc &&
					this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationRoleTableAddress)) == live &&
					this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationPriorityTableAddress)) <= strategy)
				{
					return;
				}
			}

			if (participantIdx == (short)Turn.Player && participantIdx != this.Owner.World.PersonParticipantIdx &&
				(live == 3 || live == 4))
			{
				SendUnitOfRole(participantIdx, horizLoc, vertLoc, live);
			}

			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);

				if (this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationRoleTableAddress)) != 0xff &&
					this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationPriorityTableAddress)) >= strategy)
					continue;

				FreeStrategicLocation(participantIdx, (ushort)placeIdx);

				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationHorizTableAddress), (byte)horizLoc);
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationVertTableAddress), (byte)vertLoc);
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationRoleTableAddress), (byte)live);
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitPicker.StrategicLocationPriorityTableAddress), (byte)strategy);
				break;
			}
		}

		/// <inheritdoc/>
		public void FreeStrategicLocation(short participantIdx, ushort place)
		{
			using var _ = this.Processor.Journal.Scope($"FreeStrategicLocation({participantIdx}, {place})");

			for (int idx = 30; idx >= (short)place; idx--)
			{
				int entry = (participantIdx << 5) + idx;

				if (this.Processor.PullGlobalByte((ushort)(UnitPicker.StrategicLocationPriorityTableAddress + entry)) == 0)
					continue;

				MoveListEntryOn(UnitPicker.StrategicLocationHorizTableAddress, entry);
				MoveListEntryOn(UnitPicker.StrategicLocationVertTableAddress, entry);
				MoveListEntryOn(UnitPicker.StrategicLocationRoleTableAddress, entry);
				MoveListEntryOn(UnitPicker.StrategicLocationPriorityTableAddress, entry);
			}
		}

		/// <inheritdoc/>
		public void ClearStrategicLocations(short participantIdx)
		{
			for (int idx = 0; idx < 16; idx++)
			{
				this.Owner.World.Participants[participantIdx].TacticalSites[idx].Live = -1;
				this.Owner.World.Participants[participantIdx].TacticalSites[idx].Strategy = 0;
			}
		}

		/// <inheritdoc/>
		public void AttachStrategicLocation(short participantIdx, ushort horizLoc, ushort vertLoc, ushort live, ushort strategy)
		{
			using var _ = this.Processor.Journal.Scope($"AttachStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy})");

			StrategicLocation[] sites = this.Owner.World.Participants[participantIdx].TacticalSites;

			bool known = sites.Take(0x10).Any(site => (ushort)(sbyte)site.Place.X == horizLoc && (ushort)(sbyte)site.Place.Y == vertLoc &&
				(ushort)site.Live == live && (sbyte)site.Strategy <= (short)strategy);

			if (!known)
			{
				for (ushort place = 0; place < 0x10; place++)
				{
					if (sites[place].Live == -1 || (sbyte)sites[place].Strategy < (short)strategy)
					{
						AffixStrategicLocation(participantIdx, place);

						sites[place].Place = new Point((sbyte)horizLoc, sites[place].Place.Y);
						sites[place].Place = new Point(sites[place].Place.X, (sbyte)vertLoc);
						sites[place].Live = (sbyte)live;
						sites[place].Strategy = (byte)strategy;
						break;
					}
				}
			}
		}

		/// <inheritdoc/>
		public void AffixStrategicLocation(short participantIdx, ushort place)
		{
			using var _ = this.Processor.Journal.Scope($"AffixStrategicLocation({participantIdx}, {place})");

			StrategicLocation[] sites = this.Owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 14; idx >= (short)place; idx--)
			{
				if (sites[idx].Strategy == 0)
					continue;

				sites[idx + 1].Place = new Point(sites[idx].Place.X, sites[idx + 1].Place.Y);
				sites[idx + 1].Place = new Point(sites[idx + 1].Place.X, sites[idx].Place.Y);
				sites[idx + 1].Live = sites[idx].Live;
				sites[idx + 1].Strategy = sites[idx].Strategy;
			}
		}

		/// <inheritdoc/>
		public void DisableStrategicLocation(short participantIdx, ushort role, int horizLoc, int vertLoc, ushort reach)
		{
			using var _ = this.Processor.Journal.Scope($"DisableStrategicLocation({participantIdx}, {role}, {horizLoc}, {vertLoc}, {reach})");

			StrategicLocation[] sites = this.Owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 0; idx < 16; idx++)
			{
				if ((ushort)sites[idx].Live != role)
					continue;

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, sites[idx].Place.X, vertLoc, sites[idx].Place.Y);

				if (distance <= (short)reach)
				{
					sites[idx].Live = -1;
				}
			}
		}

		/// <inheritdoc/>
		public void RefreshCityProduction(short participantIdx, ushort continent)
		{
			using var _ = this.Processor.Journal.Scope($"RefreshCityProduction({participantIdx}, {continent})");

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if ((byte)town.ParticipantIdx != (ushort)participantIdx || town.ConditionMarker == 0xff)
					continue;

				if (continent != 0xffff && this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) != continent)
					continue;

				PickNewCityProduction(townIdx);
			}
		}

		/// <inheritdoc/>
		public void PickNewCityProduction(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"PickNewCityProduction({townIdx})");

			City town = this.Owner.World.Towns[townIdx];
			ushort participantIdx = (byte)town.ParticipantIdx;

			this.Owner.World.Participants[participantIdx].StopMaking(town.PresentMakingIdx);
			town.PresentMakingIdx = (sbyte)this.Owner.Advisors.PickCityProduction((short)participantIdx, townIdx);
			this.Owner.World.Participants[participantIdx].StartMaking(town.PresentMakingIdx);
		}

		/// <inheritdoc/>
		public ushort MayAIEnterTile(short participantIdx, ushort horizLoc, ushort vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"MayAIEnterTile({participantIdx}, {horizLoc}, {vertLoc})");

			short bestHeading = -1;
			short bestShore = -1;
			IMapPlanes chart = this.Owner.MapOps.Chart;

			for (short heading = 0; heading <= 8; heading++)
			{
				short oceanHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
				short oceanVert = (short)(chart.DirectionVert(heading) + vertLoc);

				if (this.Owner.MapOps.ObtainTerrainType(oceanHoriz, oceanVert) != MapRules.OceanTerrain)
					continue;

				if (heading != 0 && this.Owner.MapOps.ObtainTileOwner(oceanHoriz, oceanVert) != 0xffff)
					continue;

				short shore = 0;

				for (int around = 1; around <= 8; around++)
				{
					short landHoriz = (short)(chart.DirectionHoriz(around) + oceanHoriz);
					short landVert = (short)(chart.DirectionVert(around) + oceanVert);

					if (this.Owner.MapOps.ObtainTerrainType(landHoriz, landVert) == MapRules.OceanTerrain ||
						this.Owner.MapOps.ObtainTileOwner(landHoriz, landVert) != 0xffff)
						continue;

					shore++;

					if ((this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] & 2) != 0 &&
						this.Owner.VideoCard.ObtainDot(2, landHoriz + 0x50, landVert) == (ushort)this.Owner.World.PersonParticipantIdx)
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

				if (this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc) != this.Owner.MapOps.ObtainContinentID(troopHoriz, troopVert))
					continue;

				int distance = this.Owner.Helpers.ObtainDistance(troopHoriz, horizLoc, troopVert, vertLoc);

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < (short)(distance << 1))
					continue;

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)horizLoc);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)vertLoc);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
					(byte)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0x10) & 0xf0));
				return;
			}
		}

		private void MoveListEntryOn(ushort origin, int entry)
		{
			this.Processor.EmitGlobalByte((ushort)(origin + entry + 1), this.Processor.PullGlobalByte((ushort)(origin + entry)));
		}
	}
}
