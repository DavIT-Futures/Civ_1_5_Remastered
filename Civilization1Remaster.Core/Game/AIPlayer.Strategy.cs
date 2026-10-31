using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.UI;

namespace Civilization1Remaster.Game
{
	/// <summary>The strategic locations of the AI players, their city production and the tiles they may enter</summary>
	partial class AIPlayer
	{
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

				this.processor.EmitGlobalByte((ushort)(entry + ArmySelection.StrategicLocationRoleTableAddress), 0xff);
				this.processor.EmitGlobalByte((ushort)(entry + ArmySelection.StrategicLocationPriorityTableAddress), 0x0);
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

				if (this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationHorizTableAddress)) == horizLoc &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationVertTableAddress)) == vertLoc &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationRoleTableAddress)) == live &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationPriorityTableAddress)) <= strategy)
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

				if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationRoleTableAddress)) != 0xff &&
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationPriorityTableAddress)) >= strategy)
					continue;

				FreeStrategicLocation(participantIdx, (ushort)placeIdx);

				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationHorizTableAddress), (byte)horizLoc);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationVertTableAddress), (byte)vertLoc);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationRoleTableAddress), (byte)live);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(place + ArmySelection.StrategicLocationPriorityTableAddress), (byte)strategy);
				break;
			}

			this.processor.Journal.CloseScope("SendUnitToStrategicLocation");
		}

		/// <summary>
		/// Makes room at the place of the list of the player (32 entries a player in the byte arrays at 0xb2be, 0xe2c2, 0xe598
		/// and 0xe798): the used entries from the place up to 30 move one place on
		/// </summary>
		public void FreeStrategicLocation(short participantIdx, ushort place)
		{
			this.processor.Journal.OpenScope($"FreeStrategicLocation({participantIdx}, {place})");

			for (int idx = 30; idx >= (short)place; idx--)
			{
				int entry = (participantIdx << 5) + idx;

				if (this.processor.PullGlobalByte((ushort)(ArmySelection.StrategicLocationPriorityTableAddress + entry)) == 0)
					continue;

				MoveListEntryOn(ArmySelection.StrategicLocationHorizTableAddress, entry);
				MoveListEntryOn(ArmySelection.StrategicLocationVertTableAddress, entry);
				MoveListEntryOn(ArmySelection.StrategicLocationRoleTableAddress, entry);
				MoveListEntryOn(ArmySelection.StrategicLocationPriorityTableAddress, entry);
			}

			this.processor.Journal.CloseScope("FreeStrategicLocation");
		}

		/// <summary>Clears the 16 tactical sites of the player (none, no strategy)</summary>
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

						sites[place].Place = new Point((sbyte)horizLoc, sites[place].Place.Y);
						sites[place].Place = new Point(sites[place].Place.X, (sbyte)vertLoc);
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

				sites[idx + 1].Place = new Point(sites[idx].Place.X, sites[idx + 1].Place.Y);
				sites[idx + 1].Place = new Point(sites[idx + 1].Place.X, sites[idx].Place.Y);
				sites[idx + 1].Live = sites[idx].Live;
				sites[idx + 1].Strategy = sites[idx].Strategy;
			}

			this.processor.Journal.CloseScope("AffixStrategicLocation");
		}

		/// <summary>
		/// The strategic locations of the player of the kind (Live = role) not farther than reach from the position are given up
		/// (Live = -1)
		/// </summary>
		public void DisableStrategicLocation(short participantIdx, ushort role, int horizLoc, int vertLoc, ushort reach)
		{
			this.processor.Journal.OpenScope($"DisableStrategicLocation({participantIdx}, {role}, {horizLoc}, {vertLoc}, {reach})");

			StrategicLocation[] sites = this.owner.World.Participants[participantIdx].TacticalSites;

			for (int idx = 0; idx < 16; idx++)
			{
				if ((ushort)sites[idx].Live != role)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, sites[idx].Place.X, vertLoc, sites[idx].Place.Y);

				if (distance <= (short)reach)
				{
					sites[idx].Live = -1;
				}
			}

			this.processor.Journal.CloseScope("DisableStrategicLocation");
		}

		/// <summary>
		/// The cities of the player choose their production again (PickNewCityProduction): all of them, or with continent not 0xffff
		/// only the cities on that continent
		/// </summary>
		public void RefreshCityProduction(short participantIdx, ushort continent)
		{
			this.processor.Journal.OpenScope($"RefreshCityProduction({participantIdx}, {continent})");

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if ((byte)town.ParticipantIdx != (ushort)participantIdx || town.ConditionMarker == 0xff)
					continue;

				if (continent != 0xffff && this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) != continent)
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

			City town = this.owner.World.Towns[townIdx];
			ushort participantIdx = (byte)town.ParticipantIdx;

			this.owner.World.Participants[participantIdx].StopMaking(town.PresentMakingIdx);
			town.PresentMakingIdx = (sbyte)this.owner.Advisors.PickCityProduction((short)participantIdx, townIdx);
			this.owner.World.Participants[participantIdx].StartMaking(town.PresentMakingIdx);

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

			this.processor.Journal.CloseScope("MayAIEnterTile");

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

		private void MoveListEntryOn(ushort origin, int entry)
		{
			this.processor.EmitGlobalByte((ushort)(origin + entry + 1), this.processor.PullGlobalByte((ushort)(origin + entry)));
		}
	}
}
