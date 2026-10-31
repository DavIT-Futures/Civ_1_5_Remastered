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
	/// The local variables start at 0 (CPU.EnterFrame), the garbage the translated code read from the stack is gone: a
	/// unit without a home stays
	/// </summary>
	public class StackLeftoverTests
	{
		/// <summary>
		/// A unit of a player without cities or carriers flying home (FindNearestFriendlyCity) stays where it is
		/// (direction 0); the translated code headed for the place left on the stack
		/// </summary>
		[Fact]
		public void UnitWithoutHomeStays()
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;

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

			ushort direction = engine.UnitOps.FindNearestFriendlyCity(player, unit);

			Assert.Equal(0, direction);
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
