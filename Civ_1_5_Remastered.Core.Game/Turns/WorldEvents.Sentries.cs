using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class WorldEvents
	{
		/// <summary>Wakes the player's sentried units that have an enemy next to them, see <see cref="SentryRules"/>.</summary>
		private void WakeThreatenedSentries(short person)
		{
			UnitMemory troops = new UnitMemory(this.Processor);
			HashSet<Point> hostile = new HashSet<Point>();

			for (int participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				if (participantIndex == person || (participantIndex != 0 && DiplomacyRules.AtPeace(this.Owner.World.Participants, person, participantIndex)))
				{
					continue;
				}

				for (int troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (troops.Exists(participantIndex, troopIndex))
					{
						hostile.Add(new Point(troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal), troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical)));
					}
				}
			}

			for (int troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				int status = troops.Exists(person, troopIndex) ? troops.Pull(person, troopIndex, UnitMemory.Status) : 0;

				if ((status & (int)UnitStatus.Sentry) == 0)
				{
					continue;
				}

				int horizontalLocation = troops.Pull(person, troopIndex, UnitMemory.Horizontal);
				int verticalLocation = troops.Pull(person, troopIndex, UnitMemory.Vertical);
				bool carried = troops.KindField(person, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
					this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain;

				if (SentryRules.ShouldWake(new Point(horizontalLocation, verticalLocation), carried, hostile))
				{
					troops.Assign(person, troopIndex, UnitMemory.Status, (byte)(status & ~(int)UnitStatus.Sentry));
				}
			}
		}
	}
}
