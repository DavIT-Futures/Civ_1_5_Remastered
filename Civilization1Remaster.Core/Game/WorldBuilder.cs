namespace Civilization1Remaster.Core.Game
{
	public partial class WorldBuilder : GameComponent, IWorldBuilder
	{
		private const ushort SeaBlocksAddress = 0x7f38;
		private const ushort SeaBlocksAlternateAddress = 0x802f;
		private const ushort StorySegment = 0x3710;
		private const ushort StoryTimingTableOffset = 0x83;

		/// <summary>The words of the frame of ProduceTerrain, shared by its helpers (B3: in C#, new for every call)</summary>
		private FrameWords terrainFrame = null!;

		/// <summary>The words of the frame of ReckonContinentSizes, shared by its helpers (B3: in C#, new for every call)</summary>

		public WorldBuilder(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void OpeningSequence()
		{
			using var _ = this.Processor.Journal.Scope("OpeningSequence()");

			this.Owner.Startup.AskForDisk(7, 1);

			ushort screenZero = Screen.Box;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screenZero, 0x1);
			Story.KeyAllowed = 0x0;

			if (NewWorld.EarthGame != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileMapPic, 1);
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

			// The story again while it waits (ShowStoryIntro gives back 1)
			while (this.ShowStoryIntro() != 0)
			{
			}

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 0, 0x140, 0xc8,
				Screen.PictureScreenBox,
				0, 0);
		}

		/// <summary>The drawing box back on screen 1 (its display index: [[0xaa]] = 1)</summary>
		private void MarkScreenChanged()
		{
			DrawingBox.DisplayIdx = 0x1;
		}
	}
}
