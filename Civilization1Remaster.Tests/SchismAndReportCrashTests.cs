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
	/// Two crashes of the translated code, in a loaded game: the schism of a civilization with units without a home city
	/// (Towns[255], ClassicSchismHomes) and a row without contact chosen in the military report (garbage of the stack frame,
	/// ClassicMilitaryReportRows). Off (the game) they work, on (the tests) they throw IndexOutOfRangeException.
	/// </summary>
	public class SchismAndReportCrashTests
	{
		[Fact]
		public void HomeCityRule()
		{
			Assert.True(HomeCityRules.HasHome(0, classic: false));
			Assert.True(HomeCityRules.HasHome(127, classic: false));
			Assert.False(HomeCityRules.HasHome(HomeCityRules.NoHome, classic: false));
			Assert.True(HomeCityRules.HasHome(HomeCityRules.NoHome, classic: true));
		}

		[Theory]
		[InlineData(false, true)]
		[InlineData(true, true)]
		[InlineData(false, false)]
		[InlineData(true, false)]
		public void SchismWithUnitWithoutHome(bool classic, bool inCity)
		{
			using HeadlessGame game = LoadGame();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicSchismHomes = classic;

			// The computer player with the most cities splits; a slot without cities and units for the new civilization
			short human = engine.World.PersonParticipantIdx;
			short player = (short)Enumerable.Range(1, 7).Where(idx => idx != human)
				.OrderByDescending(idx => engine.World.Participants[idx].TownTally).First();
			Assert.True(engine.World.Participants[player].TownTally >= 2);

			short freed = (short)Enumerable.Range(1, 7).First(idx => idx != human && idx != player);
			engine.World.Participants[freed].TroopTally = 0;
			engine.World.Participants[freed].TownTally = 0;

			// A unit of the player in a city (or outside) loses its home city
			int unit = Enumerable.Range(0, 0x80).First(idx => UnitByte(engine, player, idx, 3) != 0xff &&
				((engine.MapOps.ObtainTileImprovements((sbyte)UnitByte(engine, player, idx, 1), (sbyte)UnitByte(engine, player, idx, 2)) & 1) != 0) == inCity);
			engine.Processor.Storage.EmitUInt8(engine.Processor.SegData.Value16, UnitAddress(player, unit, 0xb), (byte)HomeCityRules.NoHome);

			if (classic)
			{
				Assert.Throws<IndexOutOfRangeException>(() => engine.Secession.DivideCivilization(player));
			}
			else
			{
				Assert.Equal(1, engine.Secession.DivideCivilization(player));
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void MilitaryReportRowWithoutContact(bool classic)
		{
			// The row of a player without contact (the autosaves have none yet)
			int row;

			using (HeadlessGame probe = LoadGame())
			{
				GameEngine world = probe.Engine;
				short human = world.World.PersonParticipantIdx;
				short[] live = Enumerable.Range(1, 7).Where(idx => (world.World.LiveRealms & (1 << idx)) != 0).Select(idx => (short)idx).ToArray();
				short stranger = live.First(idx => idx != human && (world.World.Participants[human].Relations[idx] & 0x40) == 0);
				row = Array.IndexOf(live, stranger);
			}

			// The key of that row, then any key to leave
			using HeadlessGame game = LoadGame([0x31 + row, 0x20]);
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicMilitaryReportRows = classic;

			// The words below the stack pointer, where the report's frame will be: a player number past the table
			ushort stackTop = engine.Processor.RegStack.Value16;

			for (int offset = 2; offset <= 0x400; offset += 2)
				engine.Processor.Storage.EmitUInt16(engine.Processor.SegStack.Value16, (ushort)(stackTop - offset), 0x99);

			if (classic)
				Assert.Throws<IndexOutOfRangeException>(engine.Briefings.DisplayMilitaryReport);
			else
				engine.Briefings.DisplayMilitaryReport();
		}

		private static HeadlessGame LoadGame(int[]? script = null)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"), script);
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);

			return game;
		}

		private static ushort UnitAddress(int player, int unit, int field) => (ushort)(0x81d4 + 0x600 * player + 0xc * unit + field);

		private static byte UnitByte(GameEngine engine, int player, int unit, int field) =>
			engine.Processor.Storage.PullUInt8(engine.Processor.SegData.Value16, UnitAddress(player, unit, field));
	}
}
