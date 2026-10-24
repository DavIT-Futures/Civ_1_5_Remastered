using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The schism of a computer player (DivideCivilization) in a loaded game, with ClassicSchism off (the game) and on (the
	/// tests, the translated code): the technologies and the war of the new civilization, and a player left with only its
	/// capital (no city to give).
	/// </summary>
	public class SchismRepairTests
	{
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void NewCivilizationKnowsTechnologiesAndIsAtWar(bool classic)
		{
			using HeadlessGame game = LoadGame(classic);
			GameEngine engine = game.Engine;
			(short player, short rebel) = PrepareSchism(engine);

			ushort[] technologies = engine.World.Participants[player].FoundScienceMarkers.ToArray();

			Assert.Equal(1, engine.Secession.DivideCivilization(player));

			Player newCivilization = engine.World.Participants[rebel];
			Assert.Equal(!classic, technologies.SequenceEqual(newCivilization.FoundScienceMarkers.Take(5)));
			Assert.Equal(!classic, (newCivilization.Relations[player] & 9) == 9);
			Assert.Equal(technologies, engine.World.Participants[player].FoundScienceMarkers.Take(5));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void PlayerWithOnlyItsCapital(bool classic)
		{
			using HeadlessGame game = LoadGame(classic);
			GameEngine engine = game.Engine;
			(short player, short rebel) = PrepareSchism(engine);

			// The other cities of the player go to the human player: nothing farther than the capital is left to give
			short human = engine.World.PersonParticipantIdx;
			int capital = Enumerable.Range(0, 0x80).First(idx => OwnCity(engine, idx, player) && engine.World.Towns[idx].Has(CityStructure.Palace));

			List<(int X, int Y)> given = [];

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (idx != capital && OwnCity(engine, idx, player))
				{
					engine.World.Towns[idx].ParticipantIdx = (sbyte)human;
					given.Add((engine.World.Towns[idx].Place.X, engine.World.Towns[idx].Place.Y));
				}
			}

			// One unit of the player in a given city is homed in the capital
			int homed = Enumerable.Range(0, 0x80).First(unit => UnitByte(engine, player, unit, 3) != 0xff &&
				given.Contains((UnitByte(engine, player, unit, 1), UnitByte(engine, player, unit, 2))));
			engine.Processor.Storage.EmitUInt8(engine.Processor.SegData.Value16, (ushort)(0x81d4 + 0x600 * player + 0xc * homed + 0xb), (byte)capital);

			// A unit of the player in a city whose owner is not the owner of its home city (none: the player) goes to the
			// city's owner with its kind; count those that go to the human player
			int[] movedKinds = new int[0x1c];

			for (int unit = 0; unit < 0x80; unit++)
			{
				int kind = UnitByte(engine, player, unit, 3);
				int home = UnitByte(engine, player, unit, 0xb);
				int city = Enumerable.Range(0, 0x80).FirstOrDefault(idx => engine.World.Towns[idx].ConditionMarker != 0xff &&
					engine.World.Towns[idx].Place.X == UnitByte(engine, player, unit, 1) && engine.World.Towns[idx].Place.Y == UnitByte(engine, player, unit, 2), -1);

				if (kind == 0xff || city == -1)
					continue;

				short homeOwner = home == 0xff ? player : engine.World.Towns[home].ParticipantIdx;

				if (homeOwner != engine.World.Towns[city].ParticipantIdx && engine.World.Towns[city].ParticipantIdx == human)
					movedKinds[kind]++;
			}

			Assert.True(movedKinds.Sum() > 0);
			short[] humanKinds = engine.World.Participants[human].LiveTroops.ToArray();

			// The translated code takes city 0 then (forever when it is empty)
			Assert.True(engine.World.Towns[0].TrueExtent > 0);
			short firstOwner = engine.World.Towns[0].ParticipantIdx;

			if (classic)
			{
				Assert.Throws<IndexOutOfRangeException>(() => engine.Secession.DivideCivilization(player));
				return;
			}

			Assert.Equal(1, engine.Secession.DivideCivilization(player));

			Assert.Equal(player, engine.World.Towns[capital].ParticipantIdx);
			Assert.True(engine.World.Towns[capital].Has(CityStructure.Palace));
			Assert.Equal(firstOwner, engine.World.Towns[0].ParticipantIdx);
			Assert.DoesNotContain(Enumerable.Range(0, 0x80), idx => OwnCity(engine, idx, rebel));

			for (int kind = 0; kind < 0x1c; kind++)
				Assert.Equal(humanKinds[kind] + movedKinds[kind], engine.World.Participants[human].LiveTroops[kind]);
		}

		/// <summary>The computer player with the most cities splits, into the first slot without cities and units</summary>
		private static (short Player, short Rebel) PrepareSchism(GameEngine engine)
		{
			short human = engine.World.PersonParticipantIdx;
			short player = (short)Enumerable.Range(1, 7).Where(idx => idx != human)
				.OrderByDescending(idx => engine.World.Participants[idx].TownTally).First();
			Assert.True(engine.World.Participants[player].TownTally >= 2);

			short freed = (short)Enumerable.Range(1, 7).First(idx => idx != human && idx != player);
			engine.World.Participants[freed].TroopTally = 0;
			engine.World.Participants[freed].TownTally = 0;

			short rebel = (short)Enumerable.Range(1, 7).First(idx =>
				engine.World.Participants[idx].TroopTally == 0 && engine.World.Participants[idx].TownTally == 0);

			return (player, rebel);
		}

		private static byte UnitByte(GameEngine engine, int player, int unit, int field) =>
			engine.Processor.Storage.PullUInt8(engine.Processor.SegData.Value16, (ushort)(0x81d4 + 0x600 * player + 0xc * unit + field));

		private static bool OwnCity(GameEngine engine, int townIdx, short player) =>
			engine.World.Towns[townIdx].ConditionMarker != 0xff && engine.World.Towns[townIdx].ParticipantIdx == player;

		private static HeadlessGame LoadGame(bool classic)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
			game.Engine.TurnHooks.ClassicSchism = classic;

			return game;
		}
	}
}
