using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The replay (ShowReplay) against its original code: the mode is chosen in the dialog with the keys of the script
	/// (the first key of a script goes before the dialog; the dialog comes again after modes 1 and 2: Enter then leaves),
	/// or mode 4 without asking (0x680e). The pauses of mode 1 are not seen: the timer differs anyway after a long replay
	/// (another number of memory accesses).
	/// </summary>
	public class BatchSixtyThreeDifferentialTests(ITestOutputHelper output)
	{
		[Theory]
		[InlineData(2, 0, new[] { 0x0d, 0x5000, 0x0d, 0x0d }, "mode 0")]
		[InlineData(3, 0, new[] { 0x0d, 0x5000, 0x0d, 0x0d }, "mode 0")]
		[InlineData(3, 0, new[] { 0x0d, 0x5000, 0x5000, 0x0d, 0x0d, 0x0d, 0x1b, 0x0d }, "mode 0")]
		[InlineData(3, 0, new[] { 0x5000, 0x5000, 0x5000, 0x5000, 0x0d }, "mode 3")]
		[InlineData(3, 1, new[] { 0x0d }, "mode 4")]
		[InlineData(5, 1, new[] { 0x0d }, "mode 4")]
		[InlineData(5, 0, new[] { 0x0d }, "mode 0")]
		public void DisplayReplayMatchesOriginalCode(int bay, int atGameEnd, int[] script, string branch)
		{
			Check(bay, 1, (chance, pair) =>
				new Scenario($"DisplayReplay(), at the end {atGameEnd}",
					game => game.Processor.EmitGlobalWord(0x680e, (ushort)atGameEnd),
					game => { new ReplayViewerReference(game).ShowReplay(); ShowReplayFile(game); },
					game => { game.Replayer.ShowReplay(); ShowReplayFile(game); },
					Register.Kept,
					game => $"mode {(short)game.Processor.Storage.PullUInt16(0x3b01, 0x680a)}",
					[(0x3725, 0, 0x100)]),
				output, [branch], script);
		}

		/// <summary>
		/// The events are written over each other at the top of the screen, only the replay file (mode 3) keeps them all: its
		/// length and a hash go into the data segment (0xbc00)
		/// </summary>
		private static void ShowReplayFile(Civilization1Remaster.GameEngine game)
		{
			string route = System.IO.Path.Combine(game.Processor.StandardFolder, "replay.txt");
			byte[] content = System.IO.File.Exists(route) ? System.IO.File.ReadAllBytes(route) : [];
			uint hash = 2166136261;

			foreach (byte item in content)
				hash = (hash ^ item) * 16777619;

			game.Processor.Storage.EmitUInt16(0x3b01, 0xbc00, (ushort)content.Length);
			game.Processor.Storage.EmitUInt16(0x3b01, 0xbc02, (ushort)hash);
			game.Processor.Storage.EmitUInt16(0x3b01, 0xbc04, (ushort)(hash >> 16));
		}
	}
}
