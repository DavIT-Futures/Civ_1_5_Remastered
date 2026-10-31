using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The story told while the world is made</summary>
	public partial class WorldBuilder
	{
		/// <inheritdoc/>
		public ushort ShowStoryIntro()
		{
			using var _ = this.Processor.Journal.Scope("DisplayStoryIntro()");

			DrawingBox.DisplayIdx = 0x0;

			bool waiting = StoryStep();

			ushort result = (ushort)(waiting ? 1 : 0);

			return result;
		}

		/// <summary>
		/// The step of ShowStoryIntro, true while waiting for the music
		/// </summary>
		private bool StoryStep()
		{
			if ((short)Story.State >= 2 && this.Owner.Libc.KeyAvailable() == 0)
			{
				// The story is told: the "press a key" line
				if ((short)Story.State < 5)
				{
					Story.State = (ushort)(Story.State + 1);

					if (Story.State == 5)
					{
						this.Owner.Services.FloodBox(Screen.Box, 40, 160, 240, 8, Hues.Black);
					}
				}
				else
				{
					Story.MusicTime = (ushort)CPU.SignedQuotient((short)this.Owner.Timer.Beats, 0x3c);

					ushort colour = (ushort)(((byte)Story.MusicTime & 1) != 0 ? 0xf : 0x3);

					this.Owner.TextPainter.DrawCenteredTextToScreen0(GameTexts.TextBuildingNewWorld, 160, 160, colour);
				}

				MarkScreenChanged();

				return false;
			}

			if (Story.State == 0)
			{
				Story.File = (ushort)this.Owner.Libc.OpenStream("story.txt", "rt");
				Story.State = 0x1;
				Story.LineTime = 0;
				Story.MusicTime = 0;

				this.Owner.Words.Set("");

				DrawingBox.TypefaceIdx = 0x7;
				Story.LinesTold = 0x0;
			}

			if (Story.LinesTold != 0)
			{
				Story.MusicTime = this.Owner.Services.ReadSongPosition();
			}

			if ((short)Story.MusicTime < (short)Story.LineTime)
			{
				MarkScreenChanged();

				return true;
			}

			if (Story.State != 2 && (short)Story.LinesTold < 0x28)
			{
				this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 160, Hues.Cyan);

				this.Owner.TextPainter.WaitForTime(5);
			}

			short lineRead = this.Owner.Libc.ScanStream((short)Story.File, "%[^\n]\n", TextBuffer.Address);

			if (lineRead == -1 || (this.Owner.Libc.KeyAvailable() != 0 && Story.KeyAllowed != 0))
			{
				EndStory(lineRead == -1);
			}
			else
			{
				TellStoryLine();
			}

			this.Owner.Services.ShowScreen(0);

			MarkScreenChanged();

			return Story.State != 2;
		}

		/// <summary>
		/// The line just read from story.txt (TextBuffer.Address): shown under the picture, or the next picture for a short line; then the
		/// time of the next line (0x6806) from the music table 0x3710:0x83
		/// </summary>
		private void TellStoryLine()
		{
			if ((short)Icons.BirthPicture > 1)
			{
				// The line under the picture is wiped with the picture
				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0xa0, 0x140, 8,
					Screen.Box, 0, 0xa0);
			}

			this.Owner.TextPainter.WaitForTime(10);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 160, Hues.Cyan);

			this.Owner.TextPainter.WaitForTime(5);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 160, Hues.LightCyan);

			Story.LineTime = 0x0;

			if ((short)this.Owner.Words.Length > 3)
			{
				if (Story.LinesTold == 0)
				{
					// The first line starts the music
					this.Owner.Services.RestoreTimer();

					this.Owner.Services.PerformSound(1, 0);

					this.Owner.Services.PerformSound(4, 0);
				}

				Story.LineTime = (ushort)(Story.LineTime + 2);
				Story.LinesTold = (ushort)(Story.LinesTold + 1);
			}
			else
			{
				ShowStoryPicture();
			}

			if ((short)this.Owner.Words.Length > 0x13)
			{
				Story.LineTime = (ushort)(Story.LineTime + 1);
			}

			Story.LineTime = (ushort)this.Owner.Helpers.ClampValue(
				this.Processor.PullInt16(StorySegment, (ushort)((Story.LinesTold << 1) + StoryTimingTableOffset)),
				(45 * (short)Story.LineTime) + (short)Story.MusicTime,
				32767);
		}

		/// <summary>
		/// The next picture birthN.pic (N = 0x3b64), with VGA colours faded in with its palette birthN.pal and its colour cycles
		/// </summary>
		private void ShowStoryPicture()
		{
			byte pictureIdx = (byte)Icons.BirthPicture;
			Icons.BirthPicture = (ushort)(Icons.BirthPicture + 1);
			string pictureName = $"birth{pictureIdx}.pic";

			bool colours = Screen.VgaMode != 0;

			if (colours)
			{
				StopStoryColourCycles();
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(2, 0, 0, pictureName, 0);

			if (Screen.VgaMode != 0)
			{
				this.Owner.Services.FillColors(8, 0, 0, 0);
			}

			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			if (Screen.VgaMode == 0)
				return;

			string palName = $"birth{pictureIdx}.pal";

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, palName, 0xc5be);

			this.Owner.Services.LoadColorsFromBuffer(8, 0xc5be);

			// The colour cycles of the picture: first colour, last colour, speed (300 / speed beats a step), 0 at the end
			Story.CycleCount = 0x0;

			while (this.Processor.PullUInt8(StorySegment, Story.CycleRecord) != 0)
			{
				ushort first = NextStoryCycleByte();
				ushort last = NextStoryCycleByte();
				ushort speed = NextStoryCycleByte();

				if (speed == 0)
					throw new Exception("Division by zero");

				ushort delay = (ushort)(0x12c / speed);

				Story.CycleCount = (ushort)(Story.CycleCount + 1);

				this.Owner.Services.AssignHueCycleSpan(Story.CycleCount, delay, first, last);
			}

			Story.CycleRecord = (ushort)(Story.CycleRecord + 1);

			for (short cycleIdx = 1; cycleIdx <= (short)Story.CycleCount; cycleIdx++)
			{
				this.Owner.Services.ActivateHueCycle((ushort)cycleIdx);
			}
		}

		/// <summary>The next byte of the colour cycle records of the story pictures (0x3710:0x3b66)</summary>
		private ushort NextStoryCycleByte()
		{
			ushort spot = Story.CycleRecord;
			Story.CycleRecord = (ushort)(Story.CycleRecord + 1);

			return this.Processor.PullUInt8(StorySegment, spot);
		}

		/// <summary>
		/// The end of the story (the end of story.txt, then a pause of 180, or a key): closes the file, stops the music and the
		/// colour cycles, stage 2
		/// </summary>
		private void EndStory(bool endOfFile)
		{
			if (endOfFile)
			{
				this.Owner.TextPainter.WaitForTime(180);
			}

			this.Owner.Libc.CloseStream((short)Story.File);

			this.Owner.Services.PerformSound(1, 0);

			if (Screen.VgaMode != 0)
			{
				StopStoryColourCycles();
			}

			Story.State = 0x2;
		}

		/// <summary>Stops the colour cycles 1 - 0x3b68 of the story picture</summary>
		private void StopStoryColourCycles()
		{
			for (short cycleIdx = 1; cycleIdx <= (short)Story.CycleCount; cycleIdx++)
			{
				this.Owner.Services.DeactivateHueCycle((ushort)cycleIdx);
			}
		}
	}
}
