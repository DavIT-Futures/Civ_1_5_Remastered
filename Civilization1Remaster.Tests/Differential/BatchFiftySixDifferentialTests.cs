using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Cutting the sprites of the game out of the sprite sheets (DisplayTribeSelection) against its original code
	/// </summary>
	public class BatchFiftySixDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayTribeSelectionMatchesOriginalCode()
		{
			Check(3, 12, (chance, pair) =>
			{
				// VGA only: the test games run with VGA colours, without the EGA screen 3, so the EGA sheet (0x3714) throws in
				// both versions (the original leaving its frame on the stack)
				ushort colours = 1;
				ushort sameUnits = (ushort)(chance.Next(3) == 0 ? 1 : 0);

				// The player colours: white (15) and grey (7) are changed first
				ushort[] playerColours = new ushort[16];

				for (int idx = 0; idx < playerColours.Length; idx++)
					playerColours[idx] = (ushort)(chance.Next(3) == 0 ? new[] { 7, 15 }[chance.Next(2)] : chance.Next(16));

				return new Scenario($"DisplayTribeSelection(), 0x1a3e {sameUnits}, colours {string.Join(" ", playerColours)}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd762, colours);
						game.Processor.EmitGlobalWord(0x1a3e, sameUnits);

						for (int idx = 0; idx < playerColours.Length; idx++)
							game.Processor.EmitGlobalWord((ushort)(0x1946 + (idx << 1)), playerColours[idx]);
					},
					game => { new StartGameMenuReference(game).DisplayTribeSelection(); HashPictures(game); },
					game => { game.NewGameWizard.DisplayTribeSelection(); HashPictures(game); },
					Register.Kept | Register.Acc,
					game => sameUnits != 0 ? "same units" : "units");
			}, output, ["units", "same units"]);
		}

		/// <summary>The picture tables filled by DisplayTribeSelection: address, number of pictures</summary>
		private static readonly (ushort Table, int Tally)[] PictureTables =
		[
			(0xdf62, 3), (0xb886, 0xa0), (0xd294, 0x20), (0xd2d4, 4), (0xb27a, 8), (0xb29a, 8), (0xb2ba, 1), (0x6e00, 0xf),
			(0x6e20, 0xf), (0xd21c, 0x14), (0xd244, 0x14), (0xd26c, 0x14), (0x7eec, 4), (0xd4ce, 7), (0xd4de, 8), (0x6e96, 9),
			(0xd4ee, 0x10), (0xd50e, 8), (0xb880, 1), (0xdf0c, 1), (0xd54e, 0x100),
		];

		/// <summary>
		/// The snapshot does not see the pictures, only their numbers: each picture is drawn at the corner of screen 0 and
		/// a hash of the corner goes into the data segment (0xbc00), also the same in both games
		/// </summary>
		private static void HashPictures(Civilization1Remaster.GameEngine game)
		{
			uint hash = 2166136261;
			ushort box = game.Processor.PullGlobalWord(0xaa);
			Civilization1Remaster.Video.VGABitmap screen = game.VideoCard.Displays.ObtainValueByKey(0);

			foreach ((ushort table, int tally) in PictureTables)
			{
				for (int idx = 0; idx < tally; idx++)
				{
					for (int vertLoc = 0; vertLoc < 0x3a; vertLoc++)
						for (int horizLoc = 0; horizLoc < 0x29; horizLoc++)
							screen.AssignDot(horizLoc, vertLoc, 0);

					game.VideoCard.RenderPictureToScreen(box, 0, 0, game.Processor.Storage.PullUInt16(0x3b01, (ushort)(table + (idx << 1))));

					for (int vertLoc = 0; vertLoc < 0x3a; vertLoc++)
						for (int horizLoc = 0; horizLoc < 0x29; horizLoc++)
							hash = (hash ^ screen.ObtainDot(horizLoc, vertLoc)) * 16777619;
				}
			}

			game.Processor.Storage.EmitUInt32(0x3b01, 0xbc00, hash);
		}
	}
}
