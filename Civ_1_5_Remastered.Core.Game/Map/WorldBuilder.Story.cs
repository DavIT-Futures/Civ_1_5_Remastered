using System;

namespace Civ_1_5_Remastered.Core.Game.Map
{
	public partial class WorldBuilder
	{
		/// <summary>How long (milliseconds) a story line waits for a song that does not move.</summary>
		private const int StorySilenceLimit = 1500;

		private readonly System.Diagnostics.Stopwatch storySilence = System.Diagnostics.Stopwatch.StartNew();

		private ushort storySongPosition = 0xffff;

		public ushort ShowStoryIntro()
		{
			using var _ = this.Processor.Journal.Scope("DisplayStoryIntro()");

			DrawingBox.DisplayIndex = 0x0;

			bool waiting = StoryStep();

			ushort result = (ushort)(waiting ? 1 : 0);

			return result;
		}

		private bool StoryStep()
		{
			if ((short)Story.State >= 2 && this.Owner.Runtime.KeyAvailable() == 0)
			{
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
					Story.MusicTime = (ushort)CentralProcessor.SignedQuotient((short)this.Owner.Timer.Beats, 0x3c);

					ushort colour = (ushort)(((byte)Story.MusicTime & 1) != 0 ? 0xf : 0x3);

					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(GameTexts.TextBuildingNewWorld, 160, 160, colour);
				}

				MarkScreenChanged();

				return false;
			}

			if (Story.State == 0)
			{
				Story.File = (ushort)this.Owner.Runtime.OpenStream("story.txt", "rt");
				Story.State = 0x1;
				Story.LineTime = 0;
				Story.MusicTime = 0;

				this.Owner.Words.Set(string.Empty);

				DrawingBox.TypefaceIndex = 0x7;
				Story.LinesTold = 0x0;
			}

			if (Story.LinesTold != 0)
			{
				Story.MusicTime = this.Owner.Services.ReadSongPosition();
			}

			if ((short)Story.MusicTime < (short)Story.LineTime)
			{
				// The lines follow the music. When the song does not move (no sound device can play it), the
				// story would wait for ever: after a while without music, the next line comes anyway.
				if (Story.MusicTime != this.storySongPosition)
				{
					this.storySongPosition = Story.MusicTime;
					this.storySilence.Restart();
				}
				else if (this.storySilence.ElapsedMilliseconds > StorySilenceLimit)
				{
					this.storySilence.Restart();
					Story.MusicTime = Story.LineTime;
				}

				if ((short)Story.MusicTime < (short)Story.LineTime)
				{
					MarkScreenChanged();

					return true;
				}
			}

			if (Story.State != 2 && (short)Story.LinesTold < 0x28)
			{
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 160, Hues.Cyan);

				this.Owner.TextPainter.WaitForTime(5);
			}

			short lineRead = this.Owner.Runtime.ScanStream((short)Story.File, "%[^\n]\n", TextBuffer.Address);

			if (lineRead == -1 || (this.Owner.Runtime.KeyAvailable() != 0 && Story.KeyAllowed != 0))
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

		private void TellStoryLine()
		{
			if ((short)Icons.BirthPicture > 1)
			{
				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0xa0, 0x140, 8,
					Screen.Box, 0, 0xa0);
			}

			this.Owner.TextPainter.WaitForTime(10);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 160, Hues.Cyan);

			this.Owner.TextPainter.WaitForTime(5);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 160, Hues.LightCyan);

			Story.LineTime = 0x0;

			if ((short)this.Owner.Words.Length > 3)
			{
				if (Story.LinesTold == 0)
				{
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
				this.Processor.PullSignedWord(StorySegment, (ushort)((Story.LinesTold << 1) + StoryTimingTableOffset)),
				(45 * (short)Story.LineTime) + (short)Story.MusicTime,
				32767);
		}

		private void ShowStoryPicture()
		{
			byte pictureIndex = (byte)Icons.BirthPicture;
			Icons.BirthPicture = (ushort)(Icons.BirthPicture + 1);
			string pictureName = $"birth{pictureIndex}.pic";

			bool colours = Screen.VideoGraphicsMode != 0;

			if (colours)
			{
				StopStoryColourCycles();
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(2, 0, 0, pictureName, 0);

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.Services.FillColors(8, 0, 0, 0);
			}

			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			if (Screen.VideoGraphicsMode == 0)
			{
				return;
			}

			string paletteName = $"birth{pictureIndex}.pal";

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, paletteName, 0xc5be);

			this.Owner.Services.LoadColorsFromBuffer(8, 0xc5be);

			Story.CycleCount = 0x0;

			while (this.Processor.PullByte(StorySegment, Story.CycleRecord) != 0)
			{
				ushort first = NextStoryCycleByte();
				ushort last = NextStoryCycleByte();
				ushort speed = NextStoryCycleByte();

				if (speed == 0)
				{
					throw new Exception("Division by zero");
				}

				ushort delay = (ushort)(0x12c / speed);

				Story.CycleCount = (ushort)(Story.CycleCount + 1);

				this.Owner.Services.AssignHueCycleSpan(Story.CycleCount, delay, first, last);
			}

			Story.CycleRecord = (ushort)(Story.CycleRecord + 1);

			for (short cycleIndex = 1; cycleIndex <= (short)Story.CycleCount; cycleIndex++)
			{
				this.Owner.Services.ActivateHueCycle((ushort)cycleIndex);
			}
		}

		private ushort NextStoryCycleByte()
		{
			ushort spot = Story.CycleRecord;
			Story.CycleRecord = (ushort)(Story.CycleRecord + 1);

			return this.Processor.PullByte(StorySegment, spot);
		}

		private void EndStory(bool endOfFile)
		{
			if (endOfFile)
			{
				this.Owner.TextPainter.WaitForTime(180);
			}

			this.Owner.Runtime.CloseStream((short)Story.File);

			this.Owner.Services.PerformSound(1, 0);

			if (Screen.VideoGraphicsMode != 0)
			{
				StopStoryColourCycles();
			}

			Story.State = 0x2;
		}

		private void StopStoryColourCycles()
		{
			for (short cycleIndex = 1; cycleIndex <= (short)Story.CycleCount; cycleIndex++)
			{
				this.Owner.Services.DeactivateHueCycle((ushort)cycleIndex);
			}
		}
	}
}
