using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// A computer civilization with one or two cities catches up: settlers cost half the shields and the city
	/// that builds them does not shrink.
	/// </summary>
	public class SettlerCatchUpGameTests
	{
		[Fact]
		public async Task ASmallComputerCivilizationGetsSettlersForHalfAndKeepsItsCitizens()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			foreach (string save in GameFiles.Find("*.SVE"))
			{
				bool tried = await Task.Run(() => TrySave(Path.GetFileName(save))).WaitAsync(TimeSpan.FromMinutes(2));

				if (tried)
				{
					return;
				}
			}
		}

		/// <summary>Tries the rule on the first small computer civilization of a save; false when it has none.</summary>
		private static bool TrySave(string save)
		{
			using ComputerExpansionSimulation game = new ComputerExpansionSimulation(save);

			if (!game.Loaded || game.Engine.World.HardnessTier < 1)
			{
				return false;
			}

			var engine = game.Engine;

			for (short participant = 1; participant < 8; participant++)
			{
				Player player = engine.World.Participants[participant];

				if ((engine.World.LiveRealms & (1 << participant)) == 0 || player.TownTally == 0 || player.TownTally > 2)
				{
					continue;
				}

				City? town = engine.World.Towns.FirstOrDefault(city => city.ConditionMarker != 0xff && city.ParticipantIndex == participant && city.TrueExtent >= 2 &&
					(city.ConditionMarker & 1) == 0);

				if (town == null)
				{
					continue;
				}

				// Half of the settlers at the cheapest a computer city pays for them (the cost factor of the level):
				// with the catch-up the city completes them this turn, without it not before several turns.
				int price = (short)engine.Tables.UnitKindWord(0, UnitKindTable.Price);
				int full = price * (16 - (2 * engine.World.HardnessTier));
				int size = town.TrueExtent;
				int before = player.LiveTroops[0] + player.TownTally;

				town.PresentMakingIndex = 0;
				town.MaterialsTally = (short)((full + 1) / 2);

				game.PlayRound();

				Assert.True(player.LiveTroops[0] + player.TownTally > before || town.MaterialsTally < (full + 1) / 2,
					$"{save}: civilization {participant} built no settlers for half the shields");
				Assert.True(town.TrueExtent >= size, $"{save}: the city of civilization {participant} shrank from {size} to {town.TrueExtent}");

				return true;
			}

			return false;
		}
	}
}
