using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The small rules of TurnEndRules and their switches in a loaded game: off (the game) the remaster draws two cities for
	/// disasters, writes the high byte of the population into the replay and names the enemy in the threat of an emissary;
	/// on (the tests) it does what the translated code did.
	/// </summary>
	public class TurnEndRulesTests
	{
		[Fact]
		public void ReplayKeepsTheHighByteOfThePopulation()
		{
			Assert.Equal(0x01, TurnEndRules.ReplayPopulationHigh(0x01c2, classic: false));
			Assert.Equal(0x00, TurnEndRules.ReplayPopulationHigh(0x01c2, classic: true));
		}

		[Fact]
		public void ThreatNamesTheEnemy()
		{
			Assert.Equal(6, TurnEndRules.EmissaryEnemyName(2, 3, classic: false));
			Assert.Equal(0x20, TurnEndRules.EmissaryEnemyName(2, 3, classic: true));
		}

		[Fact]
		public void FreshGameClearsTheNewCivilization()
		{
			Assert.Equal(3, TurnEndRules.RuinedUnitsCleared(3, classic: false));
			Assert.Equal(6, TurnEndRules.RuinedUnitsCleared(3, classic: true));
		}

		/// <summary>
		/// The disasters draw two cities a turn; the classic loop one, unless city 0 comes first. No city qualifies here (all
		/// small), so the draws of the loop are all the draws.
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void DisastersDrawTwoCities(bool classic)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicDisasterDraws = classic;

			foreach (var town in engine.World.Towns)
				town.TrueExtent = (sbyte)System.Math.Min((int)town.TrueExtent, 4);

			const uint seed = 1234;
			engine.Libc.SeedRandom(seed);
			Invoke(engine.TurnHooks, "StrikeRandomCities");

			// The draws made: the place of the next number of the game among the numbers of the same seed drawn again
			int next = engine.Libc.Dice.DrawInt(128);
			engine.Libc.SeedRandom(seed);
			int[] numbers = Enumerable.Range(0, 4).Select(_ => engine.Libc.Dice.DrawInt(128)).ToArray();

			int expected = classic ? (numbers[0] == 0 ? 2 : 1) : TurnEndRules.DisasterDraws;
			Assert.Equal(numbers[expected], next);
			Assert.NotEqual(numbers[classic ? 2 : 1], next);
		}

		/// <summary>The status of the human player in the replay every 50 turns: with big cities the population needs two bytes</summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ReplayWritesThePopulation(bool classic)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicReplayPopulation = classic;
			short human = engine.World.PersonParticipantIdx;

			foreach (var town in engine.World.Towns.Where(town => town.ParticipantIdx == human && town.ConditionMarker != 0xff))
				town.TrueExtent = 25;

			ushort population = engine.Helpers.ObtainPlayerPopulation(human);
			Assert.True(population > 0xff);

			int span = engine.World.RerunContentSpan;
			Invoke(engine.TurnHooks, "NoteStatusInReplay");

			// Entry 11: kind and turn, then the cities, the high and the low byte of the population
			byte[] entry = engine.World.RerunContent[span..(span + 5)];
			Assert.Equal(11, entry[0] >> 4);
			Assert.Equal(classic ? 0 : population >> 8, entry[3]);
			Assert.Equal((byte)population, entry[4]);
		}

		/// <summary>
		/// A computer player at war with an enemy of the human player (who asked for help) sends an emissary; stronger than
		/// the enemy but not twice as strong, it threatens: "$THEM" (CodeText2) is the enemy
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ThreatOfAnEmissaryNamesTheEnemy(bool classic)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicEmissaryThreat = classic;
			short human = engine.World.PersonParticipantIdx;

			short[] computers = Enumerable.Range(1, 7).Select(idx => (short)idx)
				.Where(idx => idx != human && engine.World.Participants[idx].TownTally > 0).ToArray();
			short player = computers[0];
			short enemy = computers[1];

			engine.World.Participants[player].Relations[enemy] |= 0x20;
			engine.World.Participants[player].Relations[human] &= unchecked((ushort)~0x20);
			engine.World.Participants[human].Relations[enemy] |= 2;
			engine.World.Participants[player].MartialMight = 15;
			engine.World.Participants[enemy].MartialMight = 20;

			// The audience of this turn already held and accepted (WelcomeEmissary gives 0 without asking)
			engine.Globals.EmissaryAudience.Value = 1;
			engine.Globals.KingAnswer.Value = 0;

			// "$THEM" when the king's text is shown (the audience clears it afterwards)
			string? them = null;
			Action<string>? earlier = engine.Journal.ScopeListener;
			engine.Journal.ScopeListener = scope =>
			{
				earlier?.Invoke(scope);

				if (scope.StartsWith("DisplayTextDialog("))
					them ??= Text(engine, engine.Globals.CodeText2.Value);
			};

			Invoke(engine.Politics, "SendWarEmissary", player, (ushort)enemy);

			string enemyName = Text(engine, engine.Processor.PullGlobalWord((ushort)(0x1992 + (enemy << 1))));
			string classicName = Text(engine, engine.Processor.PullGlobalWord((ushort)(0x1992 + (player << 4))));
			Assert.NotEqual(enemyName, classicName);
			Assert.Equal(classic ? classicName : enemyName, them);
		}

		private static string Text(GameEngine engine, ushort address) =>
			engine.Processor.PullText(Civilization1Remaster.Emulation.CPU.AsLinearAddress(engine.Processor.SegData.Value16, address));

		private static object? Invoke(object target, string method, params object[] arguments) =>
			target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(target, arguments);

		private static HeadlessGame Load()
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
			return game;
		}
	}
}
