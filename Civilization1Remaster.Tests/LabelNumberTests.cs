using System.IO;
using System.Reflection;

using Civilization1Remaster.Engine;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The numbers of the continents and seas of a new map (GameInitAndIntro.RankLandOrSea) for a label past the 63 ranked
	/// ones: off (the game) it gets 15 like the other unranked labels; on (ClassicLabelNumbers, the tests) it takes the word
	/// past the table of numbers, as the translated code did it (here a word of the frame of ProduceTerrain, 0)
	/// </summary>
	public class LabelNumberTests
	{
		[Theory]
		[InlineData(false, 0x45, 15)]
		[InlineData(true, 0x45, 0)]
		[InlineData(false, -0x10, 15)]
		[InlineData(false, 0x20, 15)]
		[InlineData(true, 0x20, 15)]
		public void LabelPastTheRankedOnes(bool classic, int label, int number)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicLabelNumbers = classic;

			// Every tile unlabelled but one, every size 0: no label is ranked, the ranked ones get 15
			for (int horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 0x32; vertLoc++)
					engine.World.ChartSight[horizLoc, vertLoc] = 0;
			}

			for (int idx = 0; idx < 0x80; idx++)
				engine.Processor.EmitGlobalWord((ushort)(0xdcfe + (idx << 1)), 0);

			engine.World.ChartSight[10, 20] = (sbyte)label;
			ushort displayIdx = engine.Processor.PullGlobalWord(engine.Processor.PullGlobalWord(0xaa));
			engine.VideoCard.AssignDot(displayIdx, 10, 20 + 0x32, 0, 0);

			// The frame of ProduceTerrain, as RankLandOrSea runs in it
			engine.Processor.EnterFrame(0xd4);
			SetField(engine.WorldSetup, "terrainFrame", new FrameWords(engine.Processor, 0xd4));

			try
			{
				engine.WorldSetup.GetType().GetMethod("RankLandOrSea", BindingFlags.Instance | BindingFlags.NonPublic)!
					.Invoke(engine.WorldSetup, [(ushort)0]);
			}
			finally
			{
				engine.Processor.LeaveFrame();
			}

			Assert.Equal(number, engine.VideoCard.ObtainDot(displayIdx, 10, 20 + 0x32));
		}

		private static void SetField(object target, string field, object value) =>
			target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

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
