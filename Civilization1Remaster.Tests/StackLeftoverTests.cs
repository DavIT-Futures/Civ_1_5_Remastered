using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The leftovers of the stack (TurnEvents.ClassicStackLeftovers, CPU.ClearFrameLocals): off (the game) the local
	/// variables of a function start at 0, so what earlier functions left on the stack no longer matters; on (the tests)
	/// the translated functions read those leftovers as before.
	/// </summary>
	public class StackLeftoverTests
	{
		/// <summary>A new frame: its locals zeroed when ClearFrameLocals is set, else what lay on the stack</summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void NewFrameStartsEmpty(bool clear)
		{
			using HeadlessGame game = Load();
			CPU processor = game.Engine.Processor;
			processor.ClearFrameLocals = clear;

			ushort stack = processor.RegStack.Value16;

			for (int place = 1; place <= 0x20; place++)
				processor.Storage.EmitUInt8(processor.SegStack.Value16, (ushort)(stack - place), 0x5a);

			processor.EnterFrame(0x10);
			ushort[] locals = Enumerable.Range(1, 8).Select(idx => processor.PullLocalWord((ushort)(2 * idx))).ToArray();
			processor.LeaveFrame();

			Assert.All(locals, word => Assert.Equal(clear ? 0 : 0x5a5a, word));
			Assert.Equal(stack, processor.RegStack.Value16);
		}

		/// <summary>
		/// A unit of a player without cities or carriers flying home (LocateNearestFriendlyCity): the remaster keeps it
		/// where it is (direction 0); the classic code heads for the place left in its frame (here 0x14 / 0x16 = 0, 0)
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void UnitWithoutHomeStays(bool classic)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicStackLeftovers = classic;

			short human = engine.World.PersonParticipantIdx;
			short player = (short)Enumerable.Range(1, 7).First(idx => idx != human && engine.World.Participants[idx].TownTally > 0);
			Game.UnitMemory troops = new Game.UnitMemory(engine.Processor);
			short unit = (short)Enumerable.Range(0, 0x80).First(idx => troops.Pull(player, idx, Game.UnitMemory.Kind) != 0xff &&
				troops.Pull(player, idx, Game.UnitMemory.Horiz) > 2 && troops.Pull(player, idx, Game.UnitMemory.Vert) > 2);

			// No city and no carrier of the player
			foreach (var town in engine.World.Towns.Where(town => town.ParticipantIdx == player))
				town.ParticipantIdx = (sbyte)(player == 1 ? 2 : 1);

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (troops.Pull(player, idx, Game.UnitMemory.Kind) == Game.UnitKindTable.Carrier)
					troops.Assign(player, idx, Game.UnitMemory.Kind, 0xff);
			}

			// The words the frame will have at 0x14 / 0x16 (below the saved frame pointer): 0, the corner of the map
			ushort stack = engine.Processor.RegStack.Value16;
			for (int place = 1; place <= 0x40; place++)
				engine.Processor.Storage.EmitUInt8(engine.Processor.SegStack.Value16, (ushort)(stack - place), 0);

			ushort direction = engine.UnitOps.LocateNearestFriendlyCity(player, unit);

			if (classic)
				Assert.NotEqual(0, direction);
			else
				Assert.Equal(0, direction);
		}

		/// <summary>
		/// Without the leftovers the turns run: five turns of each autosave end without an exception (the locals read before
		/// they are written now give 0, never an index the original would not have got from its garbage)
		/// </summary>
		[Theory]
		[InlineData(0)]
		[InlineData(3)]
		[InlineData(5)]
		[InlineData(7)]
		[InlineData(9)]
		[InlineData(11)]
		[InlineData(13)]
		public void TurnsRunWithoutLeftovers(int autosave)
		{
			Assert.NotEmpty(PlayTurns(autosave, classic: false));
		}

		/// <summary>The switch matters: the zeroed locals change the game of some autosave against the leftovers</summary>
		[Fact]
		public void LeftoversChangeTheGame()
		{
			Assert.Contains(new[] { 9, 0, 3, 5, 7, 11, 13 }, autosave => PlayTurns(autosave, classic: true) != PlayTurns(autosave, classic: false));
		}

		private static string PlayTurns(int autosave, bool classic)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.Engine.TurnHooks.ClassicStackLeftovers = classic;
			game.LoadAutosave(autosave, GoldenMasterTests.RandomSeed);

			List<string> turns = [];

			for (int turn = 0; turn < 5; turn++)
			{
				game.PlayTurn();
				turns.Add(string.Join("\n", game.Capture().Values.Where(entry => GameSnapshot.IsBehaviour(entry.Key))
					.Select(entry => $"{entry.Key}={entry.Value}")));
			}

			return string.Join("\n---\n", turns);
		}

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
