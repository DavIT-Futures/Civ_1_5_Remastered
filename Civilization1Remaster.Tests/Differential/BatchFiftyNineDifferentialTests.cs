using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// A page of the Civilopedia index (ShowCivilopediaIndex) against its original code: every part, the first and later
	/// pages, with the keys (arrows, Enter, space, "m", Esc, "x") or the mouse (no clicks in the tests: a key is taken)
	/// </summary>
	public class BatchFiftyNineDifferentialTests(ITestOutputHelper output)
	{
		[Theory]
		[InlineData(new[] { 0x5000, 0x5000, 0x0d }, "entry")]
		[InlineData(new[] { 0x4d00, 0x5000, 0x20 }, "entry")]
		[InlineData(new[] { 0x4d00, 0x4d00, 0x4800, 0x4b00, 0x0d }, "entry")]
		[InlineData(new[] { 0x4800, 0x4800, 0x0d }, "nothing")]
		[InlineData(new[] { 0x6d }, "next page")]
		[InlineData(new[] { 0x1b }, "leave")]
		[InlineData(new[] { 0x78 }, "leave")]
		public void DisplayCivilopediaIndexMatchesOriginalCode(int[] script, string branch)
		{
			Check(3, 10, (chance, pair) =>
			{
				ushort[] parts = [0xffff, 0, 1, 2, 3, 4];
				ushort part = parts[chance.Next(parts.Length)];
				ushort firstEntry = (ushort)(chance.Next(3) == 0 ? 0x4e * chance.Next(1, 3) : 0);
				ushort mouse = (ushort)(chance.Next(4) == 0 ? 1 : 0);

				return new Scenario($"DisplayCivilopediaIndex({firstEntry}, {(short)part}), mouse {mouse}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x1a3c, mouse);

						lock (game.VideoCard.VideoLatch)
							game.VideoCard.Tokens.Clear();
					},
					game => new EncyclopediaReference(game).ShowCivilopediaIndex(firstEntry, part),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Encyclopedia.ShowCivilopediaIndex(firstEntry, part)),
					Register.Kept | Register.Acc,
					game => game.Processor.RegAcc.Value16 switch { 0xffff => "next page", 0xfffe => "leave", 0 => "nothing", _ => "entry" });
			}, output, [branch], script);
		}
	}
}
