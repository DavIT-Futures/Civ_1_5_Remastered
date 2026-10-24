using System;
using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// A wonder nobody has built (-1) or whose city was destroyed (128), in a loaded game: with ClassicWonderCities off
	/// (the game) it belongs to nobody, on (the tests) the translated code reads the city past the city table and throws
	/// IndexOutOfRangeException.
	/// </summary>
	public class WonderCityInGameTests
	{
		[Theory]
		[InlineData(false, WonderCityRules.Destroyed)]
		[InlineData(true, WonderCityRules.Destroyed)]
		public void EffectOfWonderWithoutCity(bool classic, short townIdx)
		{
			using HeadlessGame game = LoadGame(classic);
			GameEngine engine = game.Engine;

			// A wonder that is not obsolete (PlayerEnjoysWonderEffect only checks -1)
			short wonder = (short)Enumerable.Range(1, 0x15).First(idx =>
			{
				engine.World.MarvelTownIdx[idx] = 0;
				return engine.CityOps.ObtainWonderCityID((short)idx) != 0xffff;
			});
			engine.World.MarvelTownIdx[wonder] = townIdx;

			AssertClassicThrows(classic, () =>
				Assert.Equal(0, engine.CityOps.PlayerEnjoysWonderEffect(engine.World.PersonParticipantIdx, wonder)));
		}

		[Theory]
		[InlineData(false, WonderCityRules.NotBuilt)]
		[InlineData(true, WonderCityRules.NotBuilt)]
		[InlineData(false, WonderCityRules.Destroyed)]
		[InlineData(true, WonderCityRules.Destroyed)]
		public void FirstDiscoveryOfTechnologyThatMakesWonderObsolete(bool classic, short townIdx)
		{
			using HeadlessGame game = LoadGame(classic);
			GameEngine engine = game.Engine;

			// The technology that makes the wonder 2 obsolete (table 0x10fe), discovered first now
			short technology = ObsoletingTechnology(engine, 2);
			Assert.True(technology >= 0);

			for (int wonder = 1; wonder <= 0x15; wonder++)
			{
				if (ObsoletingTechnology(engine, wonder) == technology)
					engine.World.MarvelTownIdx[wonder] = townIdx;
			}

			engine.World.ScienceLeadingFoundVia[technology] = 0;
			engine.World.MarvelTownIdx[5] = WonderCityRules.NotBuilt;
			Forget(engine, technology);

			AssertClassicThrows(classic, () => engine.Advisors.EarnTechnology(engine.World.PersonParticipantIdx, technology, 0));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void GreatLibraryInDestroyedCity(bool classic)
		{
			using HeadlessGame game = LoadGame(classic);
			GameEngine engine = game.Engine;

			// A technology somebody discovered before (no news of obsolete wonders), the Great Library in a destroyed city
			short technology = ObsoletingTechnology(engine, 2);
			engine.World.ScienceLeadingFoundVia[technology] = 1;
			engine.World.MarvelTownIdx[5] = WonderCityRules.Destroyed;
			Forget(engine, technology);

			AssertClassicThrows(classic, () => engine.Advisors.EarnTechnology(engine.World.PersonParticipantIdx, technology, 0));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void WondersOfTheWorldWithDestroyedCity(bool classic)
		{
			using HeadlessGame game = LoadGame(classic);
			GameEngine engine = game.Engine;

			engine.World.MarvelTownIdx[3] = WonderCityRules.Destroyed;

			AssertClassicThrows(classic, engine.Atlas.DisplayWondersOfTheWorld);
		}

		private static HeadlessGame LoadGame(bool classic)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
			game.Engine.TurnHooks.ClassicWonderCities = classic;

			return game;
		}

		/// <summary>EarnTechnology does nothing for a technology the player knows</summary>
		private static void Forget(GameEngine engine, short technology) =>
			engine.World.Participants[engine.World.PersonParticipantIdx].FoundScienceMarkers[technology >> 4] &=
				(ushort)~(1 << (technology & 0xf));

		private static short ObsoletingTechnology(GameEngine engine, int wonder) =>
			engine.Processor.Storage.PullInt16(engine.Processor.SegData.Value16, (ushort)(0x10fe + 2 * wonder));

		private static void AssertClassicThrows(bool classic, Action action)
		{
			if (classic)
				Assert.Throws<IndexOutOfRangeException>(action);
			else
				action();
		}
	}
}
