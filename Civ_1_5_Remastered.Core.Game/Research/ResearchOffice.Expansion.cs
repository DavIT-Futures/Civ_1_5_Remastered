namespace Civ_1_5_Remastered.Core.Game.Research
{
	public partial class ResearchOffice
	{
		/// <summary>
		/// Remastered expansion priority of the computer (Prince and up): a city with a defender and spare food builds
		/// settlers while free city sites remain and the civilization has fewer cities than its target; a harbour on
		/// a full landmass builds a colony ship when islands are free and no civilian ship is available.
		/// </summary>
		/// <returns>The unit to build, or <c>null</c> to keep the original choice.</returns>
		private ushort? ChooseExpansionProduction(short participantIndex, short townIndex)
		{
			if (!ComputerExpansion.Expands(this.Owner, participantIndex))
			{
				return null;
			}

			Player player = this.Owner.World.Participants[participantIndex];
			City town = this.Owner.World.Towns[townIndex];

			if (player.TownTally >= ComputerExpansion.CityTarget(this.Owner, participantIndex) || player.TownTally >= 0x7c ||
				!ComputerExpansion.HasDefender(this.Owner, this.troops, participantIndex, town.Place))
			{
				return null;
			}

			bool siteNearby = ComputerExpansion.SiteFrom(this.Owner, this.troops, participantIndex, -1, town.Place) != null;
			int shipKind = ComputerExpansion.ColonyShipKind(this.Owner, participantIndex);
			bool overseas = !siteNearby && (town.ConditionMarker & 2) != 0 && shipKind >= 0 &&
				ComputerExpansion.OverseasSiteFrom(this.Owner, this.troops, participantIndex, -1, town.Place,
					this.Owner.MapOperations.Chart.Continent(town.Place.X, town.Place.Y)) != null;

			if (!siteNearby && !overseas)
			{
				return null;
			}

			if (overseas && ComputerExpansion.CivilianShips(this.Owner, participantIndex) == 0)
			{
				return (ushort)shipKind;
			}

			int settlers = player.LiveTroops[ComputerExpansion.SettlersKind] + player.TroopsInsideMaking[ComputerExpansion.SettlersKind];

			if (town.PresentMakingIndex == ComputerExpansion.SettlersKind)
			{
				settlers--;
			}

			if (settlers >= ComputerExpansion.SettlerLimit(this.Owner, player.TownTally) || town.TrueExtent < 2 ||
				(short)FrameWord(HappyRoomLocal) < 1)
			{
				return null;
			}

			return ComputerExpansion.SettlersKind;
		}
	}
}
