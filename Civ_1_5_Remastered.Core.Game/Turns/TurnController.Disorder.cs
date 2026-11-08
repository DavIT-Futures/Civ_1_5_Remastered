using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController
	{
		private const int ListedRiots = 5;

		/// <summary>
		/// Before the player's turn ends, lists the cities that would fall into civil disorder and offers to assign
		/// entertainers automatically.
		/// </summary>
		/// <returns><c>true</c> when the player wants to go back to the map instead of ending the turn.</returns>
		private bool WarnOfDisorder(short participantIndex)
		{
			List<short> riots = RiotingCities(participantIndex);

			if (riots.Count == 0)
			{
				return false;
			}

			GameMessage warning = Message("CIVIL DISORDER next turn in:\n");

			for (int index = 0; index < riots.Count && index < ListedRiots; index++)
			{
				warning.City(riots[index]).Text(index + 1 < riots.Count && index + 1 < ListedRiots ? ", " : string.Empty);
			}

			if (riots.Count > ListedRiots)
			{
				warning.Text(" and ").Number((short)(riots.Count - ListedRiots)).Text(" more");
			}

			ushort choice = warning
				.Text(".\n Assign entertainers\n End turn anyway\n Back to the map\n")
				.Style(0x4)
				.Dialog(60, 60);

			if (choice == 2)
			{
				return BackToTheMap();
			}

			if (choice != 0)
			{
				return false;
			}

			List<short> restless = new List<short>();

			foreach (short townIndex in riots)
			{
				if (!this.Owner.CityOperations.CalmCity(townIndex))
				{
					restless.Add(townIndex);
				}
			}

			if (restless.Count == 0)
			{
				return false;
			}

			GameMessage failure = Message("Entertainers cannot\ncalm ");

			for (int index = 0; index < restless.Count && index < ListedRiots; index++)
			{
				failure.City(restless[index]).Text(index + 1 < restless.Count && index + 1 < ListedRiots ? ", " : string.Empty);
			}

			choice = failure
				.Text(".\n End turn anyway\n Back to the map\n")
				.Style(0x4)
				.Dialog(60, 60);

			return choice == 1 && BackToTheMap();
		}

		/// <summary>Resumes the turn; it ends again only when the player confirms the End of Turn.</summary>
		private bool BackToTheMap()
		{
			Screen.RepaintRequest = 1;
			this.turn.HumanTookKey = 0;

			return true;
		}

		private List<short> RiotingCities(short participantIndex)
		{
			List<short> riots = new List<short>();

			for (short townIndex = 0; townIndex < this.Owner.World.Towns.Length; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker != 0xff && town.ParticipantIndex == participantIndex &&
					(short)this.Owner.CityOperations.HandleCity(townIndex, -1) < 0)
				{
					riots.Add(townIndex);
				}
			}

			return riots;
		}
	}
}
