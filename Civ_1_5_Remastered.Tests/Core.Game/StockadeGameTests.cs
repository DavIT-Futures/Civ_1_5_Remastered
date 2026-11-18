using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>The Stockade, the first extra city improvement, in a running game.</summary>
	public class StockadeGameTests
	{
		private const short IronWorking = 0x12;
		private const short BronzeWorking = 0x11;

		[Fact]
		public Task TheStockadeCostsHalfAsMuchAsCityWalls() => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			GameTables tables = game.Engine.Tables;

			Assert.Equal(tables.BuildingWord(BuildingKinds.CityWalls, BuildingTable.Price), 2 * tables.BuildingWord(BuildingKinds.Stockade, BuildingTable.Price));
			Assert.Equal(tables.BuildingWord(BuildingKinds.CityWalls, BuildingTable.Upkeep), 2 * tables.BuildingWord(BuildingKinds.Stockade, BuildingTable.Upkeep));

			return true;
		});

		[Fact]
		public Task AComputerCityFinishesAStockade() => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(2), (game, save) =>
		{
			var engine = game.Engine;
			City? town = engine.World.Towns.FirstOrDefault(city => city.ConditionMarker != 0xff && city.ParticipantIndex > 0 &&
				city.ParticipantIndex != engine.World.PersonParticipantIndex && (engine.World.LiveRealms & (1 << city.ParticipantIndex)) != 0 &&
				!city.Has(CityStructure.CityWalls));

			if (town == null)
			{
				return false;
			}

			Player player = engine.World.Participants[town.ParticipantIndex];
			Learn(player, BronzeWorking);
			Learn(player, IronWorking);

			// The most a computer city pays per ten shields of price is 16 (the easiest level).
			town.PresentMakingIndex = -BuildingKinds.Stockade;
			town.MaterialsTally = (short)(engine.Tables.BuildingWord(BuildingKinds.Stockade, BuildingTable.Price) * 16);

			game.PlayRound();

			Assert.True(town.Has(CityStructure.Stockade), $"{save}: the city did not finish its Stockade");
			Assert.False(town.Has(CityStructure.StrategicDefense), $"{save}: the Stockade went to another city bit");

			return true;
		});

		private static void Learn(Player player, short science) =>
			player.FoundScienceMarkers[science >> 4] |= (ushort)(1 << (science & 0xf));
	}
}
