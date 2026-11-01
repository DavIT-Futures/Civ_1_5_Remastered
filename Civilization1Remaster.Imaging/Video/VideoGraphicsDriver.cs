using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Imaging.Video
{
	public class VideoGraphicsDriver
	{
		private readonly CallJournal videoJournal;
		private readonly CentralProcessor processor;
		private readonly IGameHost primaryHost;
		private readonly Dictionary<int, VideoGraphicsBitmap> displays = new Dictionary<int, VideoGraphicsBitmap>();
		private readonly Dictionary<int, VideoGraphicsBitmap> pictures = new Dictionary<int, VideoGraphicsBitmap>();
		private readonly Queue<int> tokens = new Queue<int>();
		private readonly Dictionary<int, CivilizationFont> typefaces;
		private readonly ushort pictureStyle = 0;
		private readonly short pictureHorizontalLocation = 0;
		private readonly short pictureVerticalLocation = 0;
		private readonly ushort stagingHorizontal = 0;
		private readonly ushort stagingVertical = 0;
		private readonly ushort stagingBreadth = 0;
		private readonly ushort stagingAltitude = 0;
		private readonly byte[] stagingPixels = new byte[512];

		private int pictureFollowingIndex = 0xb000;
		private ushort stagingMarker = 0;
		private ushort pictureDisplayIndex = 0;

		public VideoGraphicsDriver(CentralProcessor processor, CallJournal videoJournal, IGameHost host, Dictionary<int, CivilizationFont> typefaces)
		{
			this.processor = processor;
			this.videoJournal = videoJournal;
			this.primaryHost = host;
			this.typefaces = typefaces;

			this.displays.Add(0, new VideoGraphicsBitmap());
		}

		public object VideoLatch { get; set; } = new object();

		public CentralProcessor Processor
		{
			get { return this.processor; }
		}

		public Dictionary<int, VideoGraphicsBitmap> Displays
		{
			get { return this.displays; }
		}

		public Dictionary<int, VideoGraphicsBitmap> Pictures
		{
			get { return this.pictures; }
		}

		public Queue<int> Tokens
		{
			get { return this.tokens; }
		}

		public Point DisplayPointerSite
		{
			get
			{
				if (this.primaryHost != null)
				{
					return this.primaryHost.PointerSite;
				}
				else
				{
					return Point.Empty;
				}
			}
		}

		public ushort DisplayPointerClicks
		{
			get
			{
				if (this.primaryHost != null)
				{
					ushort interim = 0;
					interim |= (ushort)(((this.primaryHost.PointerClicks & PointerButtons.Left) == PointerButtons.Left) ? 1 : 0);
					interim |= (ushort)(((this.primaryHost.PointerClicks & PointerButtons.Right) == PointerButtons.Right) ? 2 : 0);
					return interim;
				}
				else
				{
					return 0;
				}
			}
		}

		public ushort ObtainFreeStorage()
		{
			this.processor.RegisterAccumulator.Word = (ushort)((this.processor.Storage.UnusedStorage.Extent >> 4) & 0xffff);

			return this.processor.RegisterAccumulator.Word;
		}

		public ushort ReserveScreen(ushort displayIndex)
		{
			if (!this.displays.ContainsKey(displayIndex))
			{
				lock (this.VideoLatch)
				{
					VideoGraphicsBitmap picture = new VideoGraphicsBitmap();
					picture.Seen = false;
					this.displays.Add(displayIndex, picture);
				}

				if (this.primaryHost != null)
				{
					this.primaryHost.OnDisplayCountChanged();
				}
			}

			this.processor.RegisterAccumulator.Word = 0xa000;

			return this.processor.RegisterAccumulator.Word;
		}

		public void DrawBufferToScreen()
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: DrawBufferToScreen()");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope("RenderBufferToScreen()");

			if (this.stagingMarker != 0)
			{
				lock (this.VideoLatch)
				{
					int cacheSpot = 0;
					VideoGraphicsBitmap primaryDisplay = this.displays[0];

					for (int index = 0; index < this.stagingAltitude; index++)
					{
						for (int innerIndex = 0; innerIndex < this.stagingBreadth; innerIndex++)
						{
							primaryDisplay.AssignDot(this.stagingHorizontal + innerIndex, this.stagingVertical + index, this.stagingPixels[cacheSpot]);
							cacheSpot++;
						}
					}
				}
			}

			this.stagingMarker = 0x0;

			this.processor.Journal = scratchJournal;
		}

		public void DrawBufferAt(short horizontalLocation, short verticalLocation, ushort stagingAddress)
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: DrawBufferAt({horizontalLocation}, {verticalLocation}, 0x{stagingAddress:x4})");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope($"RenderBufferAt({horizontalLocation}, {verticalLocation}, 0x{stagingAddress:x4})");

			this.processor.Journal = scratchJournal;
			return;
		}

		public void FillBuffer(ushort displayIndex)
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: FillBuffer({displayIndex})");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope($"FloodBuffer({displayIndex})");

			this.pictureDisplayIndex = displayIndex;

			if (this.stagingMarker != 0)
			{
				lock (this.VideoLatch)
				{
					VideoGraphicsBitmap display = Display(displayIndex);
					int cacheSpot = 0;

					for (int index = 0; index < this.stagingAltitude; index++)
					{
						for (int innerIndex = 0; innerIndex < this.stagingBreadth; innerIndex++)
						{
							this.stagingPixels[cacheSpot] = display.ObtainDot(this.stagingHorizontal + innerIndex, this.stagingVertical + index);
							cacheSpot++;
						}
					}
				}

				DrawPictureToDisplay(this.pictureDisplayIndex, this.pictureHorizontalLocation, this.pictureVerticalLocation, this.pictureStyle);
			}

			this.processor.Journal = scratchJournal;
		}

		public void DuplicateScreenToMainScreen(ushort displayIndex)
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: CopyScreenToMainScreen(0x{displayIndex:x4})");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope($"DuplicateScreenToMainScreen(0x{displayIndex:x4})");

			if (displayIndex != 0)
			{
				FillBuffer(displayIndex);

				lock (this.VideoLatch)
				{
					VideoGraphicsBitmap primaryDisplay = Display(0);
					VideoGraphicsBitmap display = Display(displayIndex);

					primaryDisplay.RenderImage(Point.Empty, display, new Rectangle(0, 0, 320, 100), false);
				}
			}

			this.processor.Journal = scratchJournal;
		}

		public void AssignHuesByIndexArray(ushort ordinalListAddress)
		{
			lock (this.VideoLatch)
			{
				Color[] hues = new Color[16];

				for (int index = 0; index < 16; index++)
				{
					hues[index] = VideoGraphicsBitmap.FirstSwatch[this.processor.PullByte(this.processor.SegmentData.Word, ordinalListAddress)];

					ordinalListAddress++;
				}

				foreach (VideoGraphicsBitmap screen in this.displays.Values)
				{
					screen.AssignHuesFromArray(hues, 0, 0, 16);
				}
			}
		}

		public void AssignHue(int ordinal, Color hue)
		{
			lock (this.VideoLatch)
			{
				foreach (VideoGraphicsBitmap screen in this.displays.Values)
				{
					screen.AssignHue(ordinal, hue);
				}
			}
		}

		public void AssignHuesFromHueStructure(ushort hueLayoutAddress)
		{
			if (this.processor.PullWord(this.processor.SegmentData.Word, hueLayoutAddress) == 0x304d)
			{
				int since = this.processor.PullByte(this.processor.SegmentData.Word, (ushort)(hueLayoutAddress + 0x4));
				int toward = this.processor.PullByte(this.processor.SegmentData.Word, (ushort)(hueLayoutAddress + 0x5));
				int total = (toward - since) + 1;
				hueLayoutAddress += 6;

				lock (this.VideoLatch)
				{
					Color[] hues = new Color[total];

					for (int index = 0; index < total; index++)
					{
						hues[index] = VideoGraphicsBitmap.HueFromDigitalLevels(this.processor.PullByte(this.processor.SegmentData.Word, (ushort)(hueLayoutAddress + (index * 3))),
							this.processor.PullByte(this.processor.SegmentData.Word, (ushort)(hueLayoutAddress + (index * 3) + 1)),
							this.processor.PullByte(this.processor.SegmentData.Word, (ushort)(hueLayoutAddress + (index * 3) + 2)));

						this.processor.Journal.EmitLine($"Setting palette index {since + index}, #{hues[index].A:x2}{hues[index].R:x2}{hues[index].G:x2}{hues[index].B:x2}");
					}

					foreach (VideoGraphicsBitmap screen in this.displays.Values)
					{
						screen.AssignHuesFromArray(hues, since, since, total);
					}
				}
			}
		}

		public void AssignHuesFromHueStructure(byte[] hueLayout)
		{
			int layoutLocation = 0;

			if (hueLayout[layoutLocation] == 0x4d && hueLayout[layoutLocation + 1] == 0x30)
			{
				int since = hueLayout[layoutLocation + 4];
				int toward = hueLayout[layoutLocation + 5];
				int total = (toward - since) + 1;
				layoutLocation += 6;

				lock (this.VideoLatch)
				{
					Color[] hues = new Color[total];

					for (int index = 0; index < total; index++)
					{
						hues[index] = VideoGraphicsBitmap.HueFromDigitalLevels(hueLayout[layoutLocation + (index * 3)],
							hueLayout[layoutLocation + (index * 3) + 1],
							hueLayout[layoutLocation + (index * 3) + 2]);
					}

					foreach (VideoGraphicsBitmap screen in this.displays.Values)
					{
						screen.AssignHuesFromArray(hues, since, since, total);
					}
				}
			}
		}

		public void DrawScreenToMainScreen(ushort displayIndex)
		{
			if (displayIndex != 0)
			{
				lock (this.VideoLatch)
				{
					VideoGraphicsBitmap display = Display(displayIndex);

					this.displays[0].RenderImage(display);
				}
			}
		}

		public void RenderImage(ushort originBoxAddress, int horizontalSince, int verticalSince, int breadth, int altitude, ushort targetBoxAddress, int horizontalToward, int verticalToward)
		{
			CivilizationRectangle boxSince = BoxAt(originBoxAddress);
			CivilizationRectangle boxToward = BoxAt(targetBoxAddress);
			VideoGraphicsBitmap originPicture = Display(boxSince.DisplayIndex);

			lock (this.VideoLatch)
			{
				Display(boxToward.DisplayIndex).RenderImage(boxToward.Horizontal + horizontalToward, boxToward.Vertical + verticalToward, originPicture,
					new Rectangle(boxSince.Horizontal + horizontalSince, boxSince.Vertical + verticalSince, breadth, altitude), false);
			}
		}

		public ushort ScreenToPicture(ushort displayIndex, ushort horizontalLocation, ushort verticalLocation, ushort breadth, ushort altitude)
		{
			lock (this.VideoLatch)
			{
				VideoGraphicsBitmap display = Display(displayIndex);
				VideoGraphicsBitmap picture = new VideoGraphicsBitmap(breadth, altitude);
				display.DuplicateColors(picture);

				picture.RenderImage(Point.Empty, display, new Rectangle(horizontalLocation, verticalLocation, breadth, altitude), false);

				this.pictures[this.pictureFollowingIndex] = picture;

				this.processor.RegisterAccumulator.Word = (ushort)this.pictureFollowingIndex;
				this.pictureFollowingIndex++;
			}

			return this.processor.RegisterAccumulator.Word;
		}

		public void DrawPictureToDisplay(ushort displayIndex, int horizontalLocation, int verticalLocation, ushort pictureAddress)
		{
			VideoGraphicsBitmap display = Display(displayIndex);

			if (!this.pictures.TryGetValue(pictureAddress, out VideoGraphicsBitmap? picture))
			{
				this.processor.Journal.EmitLine($"The bitmap 0x{pictureAddress:x4} is not allocated");
				return;
			}

			lock (this.VideoLatch)
			{
				display.RenderImage(horizontalLocation, verticalLocation, picture, true);
			}
		}

		public void DrawPictureToScreen(ushort boxAddress, int horizontalLocation, int verticalLocation, ushort pictureAddress)
		{
			CivilizationRectangle box = BoxAt(boxAddress);
			DrawPictureToDisplay(box.DisplayIndex, box.Horizontal + horizontalLocation, box.Vertical + verticalLocation, pictureAddress);
		}

		public void DrawPictureToScreen(ushort boxAddress, int horizontalLocation, int verticalLocation, int pictureAddress)
		{
			DrawPictureToScreen(boxAddress, horizontalLocation, verticalLocation, (ushort)pictureAddress);
		}

		public ushort LoadIcon(string documentName)
		{
			VideoGraphicsBitmap? picture = VideoGraphicsBitmap.ViaFile(this.processor.StandardFolder + documentName.ToUpper(), out _);

			if (picture != null)
			{
				this.pictures[this.pictureFollowingIndex] = picture;

				this.processor.RegisterAccumulator.Word = (ushort)this.pictureFollowingIndex;
				this.pictureFollowingIndex++;
			}
			else
			{
				this.processor.RegisterAccumulator.Word = 0xffff;
			}

			return this.processor.RegisterAccumulator.Word;
		}

		public ushort ObtainDot(ushort displayIndex, int horizontalLocation, int verticalLocation)
		{
			lock (this.VideoLatch)
			{
				this.processor.RegisterAccumulator.Word = Display(displayIndex).ObtainDot(horizontalLocation, verticalLocation);
			}

			return this.processor.RegisterAccumulator.Word;
		}

		public void AssignDot(ushort displayIndex, int horizontalLocation, int verticalLocation, ushort style)
		{
			lock (this.VideoLatch)
			{
				Display(displayIndex).AssignDot(horizontalLocation, verticalLocation, Hue(style), WriteMode(style));
			}
		}

		public void AssignDotInBox(ushort boxAddress, ushort displayIndex, int horizontalLocation, int verticalLocation, ushort style)
		{
			CivilizationRectangle box = BoxAt(boxAddress);

			if (horizontalLocation >= 0 && horizontalLocation <= box.Breadth && verticalLocation >= 0 && verticalLocation <= box.Altitude)
			{
				AssignDot(displayIndex, box.Horizontal + horizontalLocation, box.Vertical + verticalLocation, style);
			}
		}

		public void RenderLine(ushort boxAddress, int firstHorizontal, int firstVertical, int secondHorizontal, int secondVertical, ushort style)
		{
			CivilizationRectangle box = BoxAt(boxAddress);
			VideoGraphicsBitmap display = Display(box.DisplayIndex);

			lock (this.VideoLatch)
			{
				display.RenderLine(box.Horizontal + firstHorizontal, box.Vertical + firstVertical, box.Horizontal + secondHorizontal, box.Vertical + secondVertical, Hue(style), WriteMode(style));
			}
		}

		public void FloodBox(ushort boxAddress, int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style)
		{
			CivilizationRectangle box = BoxAt(boxAddress);

			if (breadth > 0 && altitude > 0)
			{
				lock (this.VideoLatch)
				{
					Display(box.DisplayIndex).FloodBox(new Rectangle(box.Horizontal + horizontalLocation, box.Vertical + verticalLocation, breadth, altitude), Hue(style), WriteMode(style));
				}
			}
		}

		public void SubstituteHue(ushort boxAddress, int horizontalLocation, int verticalLocation, int breadth, int altitude, byte priorHue, byte freshHue)
		{
			lock (this.VideoLatch)
			{
				CivilizationRectangle box = BoxAt(boxAddress);

				Display(box.DisplayIndex).SubstituteHue(new Rectangle(box.Horizontal + horizontalLocation, box.Vertical + verticalLocation, breadth, altitude), priorHue, freshHue);
			}
		}

		public ushort ObtainCharacterWidth(ushort typefaceIndex, byte character)
		{
			this.processor.RegisterAccumulator.Word = (ushort)ObtainRenderTextSize(typefaceIndex, new string((char)character, 1)).Width;

			return this.processor.RegisterAccumulator.Word;
		}

		public Size ObtainRenderTextSize(int typefaceIndex, string words)
		{
			int breadthValue = 0;
			int elevation = 0;

			if (!this.typefaces.ContainsKey(typefaceIndex))
			{
				typefaceIndex = 1;
			}

			CivilizationFont typeface = this.typefaces[typefaceIndex];

			for (int index = 0; index < words.Length; index++)
			{
				char character = words[index];
				CivilizationFontCharacter typefaceCharacter;

				breadthValue += typeface.LetterBreadthGap;

				if (typeface.Letters.ContainsKey(character))
				{
					typefaceCharacter = typeface.Letters[character];
				}
				else
				{
					typefaceCharacter = typeface.Letters['?'];
				}

				breadthValue += typefaceCharacter.Breadth;
				elevation = Math.Max(elevation, typefaceCharacter.Altitude);
				elevation += typeface.StrokeGap;
			}

			return new Size(breadthValue, elevation);
		}

		public ushort ObtainTextHeight(ushort typefaceIndex)
		{
			this.processor.RegisterAccumulator.Word = (ushort)ObtainRenderTextSize(typefaceIndex, "?").Height;

			return this.processor.RegisterAccumulator.Word;
		}

		public void RenderText(ushort boxAddress, int horizontalLocation, int verticalLocation, ushort charactersAddress) =>
			RenderText(boxAddress, horizontalLocation, verticalLocation, this.processor.PullText(CentralProcessor.AsLinearAddress(this.processor.SegmentData.Word, charactersAddress)));

		public void RenderText(ushort boxAddress, int horizontalLocation, int verticalLocation, string words)
		{
			CivilizationRectangle box = BoxAt(boxAddress);

			if (!this.typefaces.ContainsKey(box.TypefaceIndex))
			{
				box.TypefaceIndex = 1;
			}

			CivilizationFont typeface = this.typefaces[box.TypefaceIndex];
			VideoGraphicsBitmap display = Display(box.DisplayIndex);

			lock (this.VideoLatch)
			{
				Rectangle place = new Rectangle(box.Horizontal + horizontalLocation, box.Vertical + verticalLocation, box.Breadth, box.Altitude);

				display.RenderText(words, typeface, place, box.ForegroundHue, box.RearHue, (PixelWriteMode)box.DotStyle);
			}
		}

		private static byte Hue(ushort style) => (byte)style;

		private static PixelWriteMode WriteMode(ushort style) => (PixelWriteMode)(style >> 8);

		private VideoGraphicsBitmap Display(int displayIndex) =>
			this.displays.TryGetValue(displayIndex, out VideoGraphicsBitmap? display) ? display : throw new Exception($"The screen {displayIndex} is not allocated");

		private CivilizationRectangle BoxAt(ushort boxAddress) =>
			new CivilizationRectangle(this.processor, CentralProcessor.AsLinearAddress(this.processor.SegmentData.Word, boxAddress));
	}
}
