using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.Core.Shared.Texts;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// Pikemen, the first extra unit kind, in a running game. Each test takes the first saved game that suits it: the
	/// autosaves change whenever the game is played.
	/// </summary>
	public class PikemenGameTests
	{
		private const short IronWorking = 0x12;
		private const short Gunpowder = 0x22;
		private const short BronzeWorking = 0x11;

		[Fact]
		public Task ComputerCivilizationsBuildPikemenAndCountThem() => OnFirstFittingSave(TimeSpan.FromMinutes(3), (game, save) =>
		{
			var engine = game.Engine;

			if (Enumerable.Range(1, 7).Sum(participant => engine.World.Participants[participant].TownTally) < 4)
			{
				return false;
			}

			for (short participant = 1; participant < 8; participant++)
			{
				Learn(engine.World.Participants[participant], BronzeWorking);
				Learn(engine.World.Participants[participant], IronWorking);
				Forget(engine.World.Participants[participant], Gunpowder);
			}

			int Pikemen() => Enumerable.Range(1, 7).Sum(participant =>
				engine.World.Participants[participant].LiveTroops[UnitKinds.Pikemen] + engine.World.Participants[participant].TroopsInsideMaking[UnitKinds.Pikemen]);

			for (int round = 0; round < 12 && Pikemen() == 0; round++)
			{
				game.PlayRound();
			}

			Assert.True(Pikemen() > 0, $"{save}: no computer civilization built or started Pikemen");

			// The tallies agree with the units on the map.
			UnitMemory troops = new UnitMemory(engine.Processor);

			for (short participant = 0; participant < 8; participant++)
			{
				int onMap = Enumerable.Range(0, UnitMemory.TroopTally).Count(troop => troops.TroopKind(participant, troop) == UnitKinds.Pikemen);
				Assert.Equal(onMap, engine.World.Participants[participant].LiveTroops[UnitKinds.Pikemen]);
			}

			return true;
		});

		[Fact]
		public Task AComputerCityFinishesPikemenWithTheirPrice() => OnFirstFittingSave(TimeSpan.FromMinutes(2), (game, save) =>
		{
			var engine = game.Engine;
			City? town = engine.World.Towns.FirstOrDefault(city => city.ConditionMarker != 0xff && city.ParticipantIndex > 0 &&
				city.ParticipantIndex != engine.World.PersonParticipantIndex && (engine.World.LiveRealms & (1 << city.ParticipantIndex)) != 0);

			if (town == null)
			{
				return false;
			}

			Player player = engine.World.Participants[town.ParticipantIndex];
			Learn(player, BronzeWorking);
			Learn(player, IronWorking);
			Forget(player, Gunpowder);

			// The most a computer city pays per ten shields of price is 16 (the easiest level).
			town.PresentMakingIndex = UnitKinds.Pikemen;
			town.MaterialsTally = (short)engine.Tables.UnitCost(UnitKinds.Pikemen, 16);
			int before = player.LiveTroops[UnitKinds.Pikemen];

			game.PlayRound();

			Assert.True(player.LiveTroops[UnitKinds.Pikemen] > before, $"{save}: the city did not finish its Pikemen");

			return true;
		});

		[Fact]
		public Task APhalanxInACityIsUpgradedToPikemenOrStraightToMusketeers() => OnFirstFittingSave(TimeSpan.FromMinutes(2), (game, save) =>
		{
			var engine = game.Engine;
			short person = engine.World.PersonParticipantIndex;
			Player player = engine.World.Participants[person];
			City? town = engine.World.Towns.FirstOrDefault(city => city.ConditionMarker != 0xff && city.ParticipantIndex == person);

			if (town == null)
			{
				return false;
			}

			Learn(player, BronzeWorking);
			Learn(player, IronWorking);
			Forget(player, Gunpowder);

			ushort created = engine.UnitOperations.CreateUnit(person, UnitKinds.Phalanx, town.Place.X, town.Place.Y);

			if (created == 0xffff)
			{
				return false;
			}

			short troop = (short)created;

			Assert.Equal(UnitKinds.Pikemen, engine.UnitOperations.UpgradeKind(person, troop));
			Assert.Equal(UnitUpgradeRules.Cost(20, 25), engine.UnitOperations.UpgradeCost(person, troop));

			Learn(player, Gunpowder);
			Assert.Equal(UnitKinds.Musketeers, engine.UnitOperations.UpgradeKind(person, troop));

			Forget(player, Gunpowder);
			player.Gold = 1000;
			int phalanxes = player.LiveTroops[UnitKinds.Phalanx];
			int pikemen = player.LiveTroops[UnitKinds.Pikemen];

			Assert.True(engine.UnitOperations.UpgradeUnit(person, troop));

			UnitMemory troops = new UnitMemory(engine.Processor);
			Assert.Equal(UnitKinds.Pikemen, troops.TroopKind(person, troop));
			Assert.Equal(phalanxes - 1, player.LiveTroops[UnitKinds.Phalanx]);
			Assert.Equal(pikemen + 1, player.LiveTroops[UnitKinds.Pikemen]);
			Assert.Equal(1000 - UnitUpgradeRules.Cost(20, 25), player.Gold);

			return true;
		});

		[Fact]
		public Task EveryCivilizationHasItsOwnPikemenPicture() => OnFirstFittingSave(TimeSpan.FromMinutes(2), (game, save) =>
		{
			var engine = game.Engine;
			HashSet<ushort> pictures = new HashSet<ushort>();

			// The game cuts the unit pictures on its start screens, which a loaded test game skips: cut them here.
			engine.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceshipPictureSecond, 0);

			for (int participant = 0; participant < 8; participant++)
			{
				engine.NewGameWizard.CutPlayerCitySprites(participant);
			}

			for (int participant = 0; participant < 8; participant++)
			{
				ushort pikemen = engine.Tables.SpritePicture(0x40 + (participant << 5) + UnitKinds.Pikemen);
				ushort phalanx = engine.Tables.SpritePicture(0x40 + (participant << 5) + UnitKinds.Phalanx);

				Assert.NotEqual(0, pikemen);
				Assert.NotEqual(phalanx, pikemen);
				Assert.True(engine.VideoCard.Pictures.ContainsKey(pikemen), $"civilization {participant} has no Pikemen picture");
				Assert.True(pictures.Add(pikemen), $"civilization {participant} shares its Pikemen picture");

				// The remaster's own picture: the figure as drawn, only the badge colours change with the civilization
				// (light green, green, and white or light grey for some civilizations).
				var drawn = engine.VideoCard.Pictures[pikemen];
				string[] picture = ExtraUnitKinds.MapPicture(UnitKinds.Pikemen);
				int[] badge = { 10, 2, 15, 7 };

				for (int vertical = 0; vertical < ExtraUnitPictures.MapBreadth; vertical++)
				{
					for (int horizontal = 0; horizontal < ExtraUnitPictures.MapBreadth; horizontal++)
					{
						byte dot = ExtraUnitPictures.Dot(picture, horizontal, vertical);

						if (!badge.Contains(dot))
						{
							Assert.Equal(dot, drawn.ObtainDot(horizontal, vertical));
						}
					}
				}
			}

			return true;
		});

		/// <summary>Runs <paramref name="attempt"/> on the saved games in turn until one suits it (it returns true).</summary>
		private static async Task OnFirstFittingSave(TimeSpan patience, Func<ComputerExpansionSimulation, string, bool> attempt)
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			foreach (string save in GameFiles.Find("*.SVE").Select(Path.GetFileName).OfType<string>())
			{
				bool fitted = await Task.Run(() =>
				{
					using ComputerExpansionSimulation game = new ComputerExpansionSimulation(save);

					return game.Loaded && attempt(game, save);
				}).WaitAsync(patience);

				if (fitted)
				{
					return;
				}
			}
		}

		private static void Learn(Player player, short science) =>
			player.FoundScienceMarkers[science >> 4] |= (ushort)(1 << (science & 0xf));

		private static void Forget(Player player, short science) =>
			player.FoundScienceMarkers[science >> 4] &= (ushort)~(1 << (science & 0xf));
	}
}
