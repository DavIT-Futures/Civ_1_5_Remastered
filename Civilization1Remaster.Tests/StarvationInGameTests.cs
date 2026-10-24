using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// A starving city of a computer player without a settler to lose (FeedCity), in a loaded game: with ClassicStarvation
	/// off (the game) it loses a citizen and the food storage, and itself only at size 1; on (the tests) it is lost whatever
	/// its size, as the translated code did it.
	/// </summary>
	public class StarvationInGameTests
	{
		[Theory]
		[InlineData(false, 3)]
		[InlineData(true, 3)]
		[InlineData(false, 1)]
		[InlineData(true, 1)]
		public void StarvingCityWithoutSettler(bool classic, sbyte size)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);

			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicStarvation = classic;

			// A city of a computer player with another city (losing it does not end the civilization)
			short human = engine.World.PersonParticipantIdx;
			short townIdx = (short)Enumerable.Range(0, 0x80).First(idx =>
			{
				City candidate = engine.World.Towns[idx];
				return candidate.ConditionMarker != 0xff && candidate.ParticipantIdx != human && candidate.ParticipantIdx > 0 &&
					engine.World.Participants[candidate.ParticipantIdx].TownTally >= 2;
			});

			City town = engine.World.Towns[townIdx];
			short owner = town.ParticipantIdx;
			town.TrueExtent = size;
			town.GrainTally = -500;

			// No settler of the city to starve instead
			for (int unit = 0; unit < 0x80; unit++)
			{
				ushort address = (ushort)(0x81d4 + 0x600 * owner + 0xc * unit);

				if (UnitByte(engine, address + 3) == 0 && UnitByte(engine, address + 0xb) == townIdx)
					engine.Processor.Storage.EmitUInt8(engine.Processor.SegData.Value16, (ushort)(address + 0xb), (byte)0xff);
			}

			engine.CityOps.HandleCity(townIdx, 0);

			bool lost = classic || size == 1;
			Assert.Equal(lost, town.ParticipantIdx != owner || town.ConditionMarker == 0xff);

			if (!lost)
			{
				Assert.Equal(size - 1, town.TrueExtent);
				Assert.True(town.GrainTally >= 0);
			}
		}

		private static byte UnitByte(GameEngine engine, int address) =>
			engine.Processor.Storage.PullUInt8(engine.Processor.SegData.Value16, (ushort)address);
	}
}
