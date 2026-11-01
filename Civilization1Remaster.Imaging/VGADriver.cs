using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Imaging
{
	public class VGADriver
	{
		private readonly CallJournal videoJournal;
		private readonly CPU processor;
		private readonly IGameHost primaryHost;
		private readonly Dictionary<int, VGABitmap> displays = new Dictionary<int, VGABitmap>();
		private readonly Dictionary<int, VGABitmap> pictures = new Dictionary<int, VGABitmap>();
		private readonly Queue<int> tokens = new Queue<int>();
		private readonly Dictionary<int, CivFont> typefaces;
		private readonly ushort glob15c0 = 0;
		private readonly short glob15c2HorizLoc = 0;
		private readonly short glob15c4VertLoc = 0;
		private readonly ushort glob15caStagingHoriz = 0;
		private readonly ushort glob15ccStagingVert = 0;
		private readonly ushort glob15ceStagingBreadth = 0;
		private readonly ushort glob15d0StagingAltitude = 0;
		private readonly byte[] glob15d6Staging = new byte[512];

		private int pictureFollowingIdx = 0xb000;
		private ushort glob15d2StagingMarker = 0;
		private ushort glob15d4DisplayIdx = 0;

		public VGADriver(CPU processor, CallJournal videoJournal, IGameHost host, Dictionary<int, CivFont> typefaces)
		{
			this.processor = processor;
			this.videoJournal = videoJournal;
			this.primaryHost = host;
			this.typefaces = typefaces;

			this.displays.Add(0, new VGABitmap());
		}

		public object VideoLatch { get; set; } = new object();

		public CPU Processor
		{
			get { return this.processor; }
		}

		public Dictionary<int, VGABitmap> Displays
		{
			get { return this.displays; }
		}

		public Dictionary<int, VGABitmap> Pictures
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
			this.processor.RegAcc.Value16 = (ushort)((this.processor.Storage.UnusedStorage.Extent >> 4) & 0xffff);

			return this.processor.RegAcc.Value16;
		}

		public ushort ReserveScreen(ushort displayIdx)
		{
			if (!this.displays.ContainsKey(displayIdx))
			{
				lock (this.VideoLatch)
				{
					VGABitmap picture = new VGABitmap();
					picture.Seen = false;
					this.displays.Add(displayIdx, picture);
				}

				if (this.primaryHost != null)
				{
					this.primaryHost.OnDisplayCountChanged();
				}
			}

			this.processor.RegAcc.Value16 = 0xa000;

			return this.processor.RegAcc.Value16;
		}

		public void DrawBufferToScreen()
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: DrawBufferToScreen()");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope("RenderBufferToScreen()");

			if (this.glob15d2StagingMarker != 0)
			{
				lock (this.VideoLatch)
				{
					int cacheSpot = 0;
					VGABitmap primaryDisplay = this.displays[0];

					for (int idx = 0; idx < this.glob15d0StagingAltitude; idx++)
					{
						for (int jdx = 0; jdx < this.glob15ceStagingBreadth; jdx++)
						{
							primaryDisplay.AssignDot(this.glob15caStagingHoriz + jdx, this.glob15ccStagingVert + idx, this.glob15d6Staging[cacheSpot]);
							cacheSpot++;
						}
					}
				}
			}

			this.glob15d2StagingMarker = 0x0;

			this.processor.Journal = scratchJournal;
		}

		public void DrawBufferAt(short horizLoc, short vertLoc, ushort stagingAddr)
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: DrawBufferAt({horizLoc}, {vertLoc}, 0x{stagingAddr:x4})");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope($"RenderBufferAt({horizLoc}, {vertLoc}, 0x{stagingAddr:x4})");

			this.processor.Journal = scratchJournal;
			return;
		}

		public void FillBuffer(ushort displayIdx)
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: FillBuffer({displayIdx})");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope($"FloodBuffer({displayIdx})");

			this.glob15d4DisplayIdx = displayIdx;

			if (this.glob15d2StagingMarker != 0)
			{
				lock (this.VideoLatch)
				{
					VGABitmap display = Display(displayIdx);
					int cacheSpot = 0;

					for (int idx = 0; idx < this.glob15d0StagingAltitude; idx++)
					{
						for (int jdx = 0; jdx < this.glob15ceStagingBreadth; jdx++)
						{
							this.glob15d6Staging[cacheSpot] = display.ObtainDot(this.glob15caStagingHoriz + jdx, this.glob15ccStagingVert + idx);
							cacheSpot++;
						}
					}
				}

				DrawPictureToDisplay(this.glob15d4DisplayIdx, this.glob15c2HorizLoc, this.glob15c4VertLoc, this.glob15c0);
			}

			this.processor.Journal = scratchJournal;
		}

		public void DuplicateScreenToMainScreen(ushort displayIdx)
		{
			CallJournal scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: CopyScreenToMainScreen(0x{displayIdx:x4})");
			this.processor.Journal = this.videoJournal;
			using var _ = this.processor.Journal.Scope($"DuplicateScreenToMainScreen(0x{displayIdx:x4})");

			if (displayIdx != 0)
			{
				FillBuffer(displayIdx);

				lock (this.VideoLatch)
				{
					VGABitmap primaryDisplay = Display(0);
					VGABitmap display = Display(displayIdx);

					primaryDisplay.RenderImage(Point.Empty, display, new Rectangle(0, 0, 320, 100), false);
				}
			}

			this.processor.Journal = scratchJournal;
		}

		public void AssignHuesByIndexArray(ushort ordinalListAddr)
		{
			lock (this.VideoLatch)
			{
				Color[] hues = new Color[16];

				for (int idx = 0; idx < 16; idx++)
				{
					hues[idx] = VGABitmap.Swatch1[this.processor.PullUInt8(this.processor.SegData.Value16, ordinalListAddr)];

					ordinalListAddr++;
				}

				foreach (VGABitmap screen in this.displays.Values)
				{
					screen.AssignHuesFromArray(hues, 0, 0, 16);
				}
			}
		}

		public void AssignHue(int ordinal, Color hue)
		{
			lock (this.VideoLatch)
			{
				foreach (VGABitmap screen in this.displays.Values)
				{
					screen.AssignHue(ordinal, hue);
				}
			}
		}

		public void AssignHuesFromHueStruct(ushort hueLayoutAddr)
		{
			if (this.processor.PullUInt16(this.processor.SegData.Value16, hueLayoutAddr) == 0x304d)
			{
				int since = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(hueLayoutAddr + 0x4));
				int toward = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(hueLayoutAddr + 0x5));
				int total = (toward - since) + 1;
				hueLayoutAddr += 6;

				lock (this.VideoLatch)
				{
					Color[] hues = new Color[total];

					for (int idx = 0; idx < total; idx++)
					{
						hues[idx] = VGABitmap.Hue18ToHue(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(hueLayoutAddr + (idx * 3))),
							this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(hueLayoutAddr + (idx * 3) + 1)),
							this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(hueLayoutAddr + (idx * 3) + 2)));

						this.processor.Journal.EmitLine($"Setting palette index {since + idx}, #{hues[idx].A:x2}{hues[idx].R:x2}{hues[idx].G:x2}{hues[idx].B:x2}");
					}

					foreach (VGABitmap screen in this.displays.Values)
					{
						screen.AssignHuesFromArray(hues, since, since, total);
					}
				}
			}
		}

		public void AssignHuesFromHueStruct(byte[] hueLayout)
		{
			int layoutLoc = 0;

			if (hueLayout[layoutLoc] == 0x4d && hueLayout[layoutLoc + 1] == 0x30)
			{
				int since = hueLayout[layoutLoc + 4];
				int toward = hueLayout[layoutLoc + 5];
				int total = (toward - since) + 1;
				layoutLoc += 6;

				lock (this.VideoLatch)
				{
					Color[] hues = new Color[total];

					for (int idx = 0; idx < total; idx++)
					{
						hues[idx] = VGABitmap.Hue18ToHue(hueLayout[layoutLoc + (idx * 3)],
							hueLayout[layoutLoc + (idx * 3) + 1],
							hueLayout[layoutLoc + (idx * 3) + 2]);
					}

					foreach (VGABitmap screen in this.displays.Values)
					{
						screen.AssignHuesFromArray(hues, since, since, total);
					}
				}
			}
		}

		public void DrawScreenToMainScreen(ushort displayIdx)
		{
			if (displayIdx != 0)
			{
				lock (this.VideoLatch)
				{
					VGABitmap display = Display(displayIdx);

					this.displays[0].RenderImage(display);
				}
			}
		}

		public void RenderImage(ushort originBoxAddr, int horizSince, int vertSince, int breadth, int altitude, ushort targetBoxAddr, int horizToward, int vertToward)
		{
			CivRectangle boxSince = BoxAt(originBoxAddr);
			CivRectangle boxToward = BoxAt(targetBoxAddr);
			VGABitmap originPicture = Display(boxSince.DisplayIdx);

			lock (this.VideoLatch)
			{
				Display(boxToward.DisplayIdx).RenderImage(boxToward.Horiz + horizToward, boxToward.Vert + vertToward, originPicture,
					new Rectangle(boxSince.Horiz + horizSince, boxSince.Vert + vertSince, breadth, altitude), false);
			}
		}

		public ushort ScreenToPicture(ushort displayIdx, ushort horizLoc, ushort vertLoc, ushort breadth, ushort altitude)
		{
			lock (this.VideoLatch)
			{
				VGABitmap display = Display(displayIdx);
				VGABitmap picture = new VGABitmap(breadth, altitude);
				display.DuplicateColors(picture);

				picture.RenderImage(Point.Empty, display, new Rectangle(horizLoc, vertLoc, breadth, altitude), false);

				this.pictures[this.pictureFollowingIdx] = picture;

				this.processor.RegAcc.Value16 = (ushort)this.pictureFollowingIdx;
				this.pictureFollowingIdx++;
			}

			return this.processor.RegAcc.Value16;
		}

		public void DrawPictureToDisplay(ushort displayIdx, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			VGABitmap display = Display(displayIdx);

			if (!this.pictures.TryGetValue(pictureAddr, out VGABitmap? picture))
			{
				this.processor.Journal.EmitLine($"The bitmap 0x{pictureAddr:x4} is not allocated");
				return;
			}

			lock (this.VideoLatch)
			{
				display.RenderImage(horizLoc, vertLoc, picture, true);
			}
		}

		public void DrawPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			CivRectangle box = BoxAt(boxAddr);
			DrawPictureToDisplay(box.DisplayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, pictureAddr);
		}

		public void DrawPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, int pictureAddr)
		{
			DrawPictureToScreen(boxAddr, horizLoc, vertLoc, (ushort)pictureAddr);
		}

		public ushort LoadIcon(string docname)
		{
			VGABitmap? picture = VGABitmap.ViaFile(this.processor.StandardFolder + docname.ToUpper(), out _);

			if (picture != null)
			{
				this.pictures[this.pictureFollowingIdx] = picture;

				this.processor.RegAcc.Value16 = (ushort)this.pictureFollowingIdx;
				this.pictureFollowingIdx++;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}

			return this.processor.RegAcc.Value16;
		}

		public ushort ObtainDot(ushort displayIdx, int horizLoc, int vertLoc)
		{
			lock (this.VideoLatch)
			{
				this.processor.RegAcc.Value16 = Display(displayIdx).ObtainDot(horizLoc, vertLoc);
			}

			return this.processor.RegAcc.Value16;
		}

		public void AssignDot(ushort displayIdx, int horizLoc, int vertLoc, ushort style)
		{
			lock (this.VideoLatch)
			{
				Display(displayIdx).AssignDot(horizLoc, vertLoc, Hue(style), WriteMode(style));
			}
		}

		public void AssignDotInBox(ushort boxAddr, ushort displayIdx, int horizLoc, int vertLoc, ushort style)
		{
			CivRectangle box = BoxAt(boxAddr);

			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				AssignDot(displayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, style);
			}
		}

		public void RenderLine(ushort boxAddr, int horiz1, int vert1, int horiz2, int vert2, ushort style)
		{
			CivRectangle box = BoxAt(boxAddr);
			VGABitmap display = Display(box.DisplayIdx);

			lock (this.VideoLatch)
			{
				display.RenderLine(box.Horiz + horiz1, box.Vert + vert1, box.Horiz + horiz2, box.Vert + vert2, Hue(style), WriteMode(style));
			}
		}

		public void FloodBox(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			CivRectangle box = BoxAt(boxAddr);

			if (breadth > 0 && altitude > 0)
			{
				lock (this.VideoLatch)
				{
					Display(box.DisplayIdx).FloodBox(new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, breadth, altitude), Hue(style), WriteMode(style));
				}
			}
		}

		public void SubstituteHue(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, byte priorHue, byte freshHue)
		{
			lock (this.VideoLatch)
			{
				CivRectangle box = BoxAt(boxAddr);

				Display(box.DisplayIdx).SubstituteHue(new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, breadth, altitude), priorHue, freshHue);
			}
		}

		public ushort ObtainCharWidth(ushort typefaceIdx, byte chr)
		{
			this.processor.RegAcc.Value16 = (ushort)ObtainRenderTextSize(typefaceIdx, new string((char)chr, 1)).Width;

			return this.processor.RegAcc.Value16;
		}

		public Size ObtainRenderTextSize(int typefaceIdx, string words)
		{
			int breadthInt = 0;
			int elevation = 0;

			if (!this.typefaces.ContainsKey(typefaceIdx))
			{
				typefaceIdx = 1;
			}

			CivFont typeface = this.typefaces[typefaceIdx];

			for (int idx = 0; idx < words.Length; idx++)
			{
				char chr = words[idx];
				CivFontCharacter typefaceChr;

				breadthInt += typeface.LetterBreadthGap;

				if (typeface.Letters.ContainsKey(chr))
				{
					typefaceChr = typeface.Letters[chr];
				}
				else
				{
					typefaceChr = typeface.Letters['?'];
				}

				breadthInt += typefaceChr.Breadth;
				elevation = Math.Max(elevation, typefaceChr.Altitude);
				elevation += typeface.StrokeGap;
			}

			return new Size(breadthInt, elevation);
		}

		public ushort ObtainTextHeight(ushort typefaceIdx)
		{
			this.processor.RegAcc.Value16 = (ushort)ObtainRenderTextSize(typefaceIdx, "?").Height;

			return this.processor.RegAcc.Value16;
		}

		public void RenderText(ushort boxAddr, int horizLoc, int vertLoc, ushort charsAddr) =>
			RenderText(boxAddr, horizLoc, vertLoc, this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr)));

		public void RenderText(ushort boxAddr, int horizLoc, int vertLoc, string words)
		{
			CivRectangle box = BoxAt(boxAddr);

			if (!this.typefaces.ContainsKey(box.TypefaceIdx))
			{
				box.TypefaceIdx = 1;
			}

			CivFont typeface = this.typefaces[box.TypefaceIdx];
			VGABitmap display = Display(box.DisplayIdx);

			lock (this.VideoLatch)
			{
				Rectangle place = new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, box.Breadth, box.Altitude);

				display.RenderText(words, typeface, place, box.ForeHue, box.RearHue, (PixelWriteMode)box.DotStyle);
			}
		}

		private VGABitmap Display(int displayIdx) =>
			this.displays.TryGetValue(displayIdx, out VGABitmap? display) ? display : throw new Exception($"The screen {displayIdx} is not allocated");

		private CivRectangle BoxAt(ushort boxAddr) =>
			new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));

		private static byte Hue(ushort style) => (byte)style;

		private static PixelWriteMode WriteMode(ushort style) => (PixelWriteMode)(style >> 8);
	}
}
