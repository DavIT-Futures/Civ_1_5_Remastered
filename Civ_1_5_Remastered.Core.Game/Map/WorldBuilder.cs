namespace Civ_1_5_Remastered.Core.Game.Map
{
	public partial class WorldBuilder : GameComponent, IWorldBuilder
	{
		private const ushort SeaBlocksAddress = 0x7f38;
		private const ushort SeaBlocksAlternateAddress = 0x802f;
		private const ushort StorySegment = 0x3710;
		private const ushort StoryTimingTableOffset = 0x83;

		private FrameWords terrainFrame = null!;

		public WorldBuilder(IGameEngine owner)
			: base(owner)
		{
		}

		public void OpeningSequence()
		{
			using var _ = this.Processor.Journal.Scope("OpeningSequence()");

			this.Owner.Startup.AskForDisk(7, 1);

			ushort screenZero = Screen.Box;
			this.Processor.EmitWord(this.Processor.SegmentData.Word, screenZero, 0x1);
			Story.KeyAllowed = 0x0;

			if (NewWorld.EarthGame != 0)
			{
				if (EuropeMap.Chosen(NewWorld.EarthGame))
				{
					this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, EuropeMap.Picture, 1);
				}
				else
				{
					this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileMapPicture, 1);
				}
			}
			else
			{
				this.ShowStoryIntro();

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				do
				{
					this.ProduceCloudLayer();
					this.ShowStoryIntro();
				}
				while ((short)(((NewWorld.LandMass << 3) * 0x28) + 0x280) > (short)Story.LightenedPixels);

				this.Owner.Dialogs.DrawBox(0, 0, 79, 49, Hues.Black);
				this.Owner.Dialogs.DrawBox(1, 1, 77, 47, Hues.Black);

				SmoothDiagonalCoastlines();
				AssignLatitudeAndElevationTerrain();
				ApplyMoisturePasses();
				AgeTerrain();

				this.ProduceElevation();
			}

			this.Owner.Services.FloodBox(Screen.Box, 160, 0, 160, 200, Hues.Black);

			this.ProduceTerrain();

			Story.KeyAllowed = 0x1;

			while (this.ShowStoryIntro() != 0)
			{
			}

			DrawingBox.TypefaceIndex = 0x1;

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 0, 0x140, 0xc8,
				Screen.PictureScreenBox,
				0, 0);
		}

		private void MarkScreenChanged()
		{
			DrawingBox.DisplayIndex = 0x1;
		}
	}
}
