using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.IO.Compression;
using Civilization1Original.Emulation;
using Civilization1Original.Video;
using Civilization1Original.Containers;
using System.Runtime.InteropServices;
using Civilization1Original.Properties;

namespace Civilization1Original.Video
{
	public class VGADriver
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		private readonly IGameHost? primaryHost = null;
		public object VideoLatch = new object();
		private readonly BDictionary<int, VGABitmap> displays = new BDictionary<int, VGABitmap>();
		private int pictureFollowingIdx = 0xb000;
		private readonly BDictionary<int, VGABitmap> pictures = new BDictionary<int, VGABitmap>();
		private readonly Queue<int> tokens = new Queue<int>();
		private readonly CivFonts typefaces;

		private readonly ushort glob15c0 = 0;
		private readonly short glob15c2HorizLoc = 0;
		private readonly short glob15c4VertLoc = 0;
		private readonly ushort glob15caStagingHoriz = 0;
		private readonly ushort glob15ccStagingVert = 0;
		private readonly ushort glob15ceStagingBreadth = 0;
		private readonly ushort glob15d0StagingAltitude = 0;
		private ushort glob15d2StagingMarker = 0;
		private ushort glob15d4DisplayIdx = 0;

		private readonly byte[] glob15d6Staging = new byte[512];

		public VGADriver(GameEngine owner, IGameHost? host)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.primaryHost = host;

			this.displays.Add(0, new VGABitmap()); // our Main screen

			byte[]? typefaces = (byte[]?)Resources.ResourceManager.GetObject("Fonts_xml");

			if (typefaces == null)
			{
				this.primaryHost?.ShowMessage("Civilization1Original resource Fonts is missing.", "Resource missing");
				this.processor.Quit(-1);

				throw new Exception("Embedded resource missing"); // Fool compiler as Exit already throws an exception
			}

			Stream typefaces1 = new GZipStream(new MemoryStream(typefaces), CompressionMode.Decompress);
			this.typefaces = CivFonts.Unpack(typefaces1);
			typefaces1.Close();
		}

		public CPU Processor
		{
			get { return this.processor; }
		}

		public BDictionary<int, VGABitmap> Displays
		{
			get { return this.displays; }
		}

		public BDictionary<int, VGABitmap> Pictures
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
					return this.primaryHost.PointerSite;
				else
					return Point.Empty;
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
					return 0;
			}
		}

		#region Memory and Initialization functions
		public ushort ObtainFreeStorage()
		{
			// function body
			this.processor.RegAcc.Value16 = (ushort)((this.processor.Storage.UnusedStorage.Extent >> 4) & 0xffff);

			return this.processor.RegAcc.Value16;
		}

		public ushort ReserveScreen(ushort displayIdx)
		{
			// function body
			if (!this.displays.IncludesKey(displayIdx))
			{
				lock (this.VideoLatch)
				{
					VGABitmap picture = new VGABitmap();
					picture.Seen = false; // additional screens are not shown by default
					this.displays.Add(displayIdx, picture);
				}
				if (this.primaryHost != null)
					this.primaryHost.OnDisplayCountChanged();
			}

			this.processor.RegAcc.Value16 = 0xa000; // return something to make underlying code happy

			return this.processor.RegAcc.Value16;
		}
		#endregion

		public void RenderBufferToScreen()
		{
			LogWrapper scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: DrawBufferToScreen()");
			this.processor.Journal = this.owner.VideoJournal;
			this.processor.Journal.OpenScope("RenderBufferToScreen()");

			// function body
			if (this.glob15d2StagingMarker != 0)
			{
				lock (this.VideoLatch)
				{
					int cacheSpot = 0;
					VGABitmap primaryDisplay = this.displays.ObtainValueByKey(0);

					for (int idx = 0; idx < this.glob15d0StagingAltitude; idx++) // height
					{
						for (int jdx = 0; jdx < this.glob15ceStagingBreadth; jdx++) // width
						{
							primaryDisplay.AssignDot(this.glob15caStagingHoriz + jdx, this.glob15ccStagingVert + idx, this.glob15d6Staging[cacheSpot]);
							cacheSpot++;
						}
					}
				}
			}
			this.glob15d2StagingMarker = 0x0;

			// Far return
			this.processor.Journal.CloseScope("RenderBufferToScreen");
			this.processor.Journal = scratchJournal;
		}

		public void RenderBufferAt(short horizLoc, short vertLoc, ushort stagingAddr)
		{
			LogWrapper scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: DrawBufferAt({horizLoc}, {vertLoc}, 0x{stagingAddr:x4})");
			this.processor.Journal = this.owner.VideoJournal;
			this.processor.Journal.OpenScope($"RenderBufferAt({horizLoc}, {vertLoc}, 0x{stagingAddr:x4})");

			// function body
			// Far return
			this.processor.Journal.CloseScope("RenderBufferAt");
			this.processor.Journal = scratchJournal;
			return;
		}

		public void FloodBuffer(ushort displayIdx)
		{
			LogWrapper scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: FillBuffer({displayIdx})");
			this.processor.Journal = this.owner.VideoJournal;
			this.processor.Journal.OpenScope($"FloodBuffer({displayIdx})");

			// function body
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.StackWord(this.processor.SegExtra.Value16);

			this.glob15d4DisplayIdx = displayIdx;

			if (this.glob15d2StagingMarker != 0)
			{
				if (this.displays.IncludesKey(displayIdx))
				{
					lock (this.VideoLatch)
					{
						VGABitmap display = this.displays.ObtainValueByKey(displayIdx);

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

					this.processor.StackWord(199);
					this.processor.StackWord(319);
					this.processor.StackWord(0);
					this.processor.StackWord(0);
					this.processor.StackWord(this.glob15d4DisplayIdx);
					ushort boxAddr = this.processor.RegStack.Value16;

					RenderPictureToScreen(boxAddr, this.glob15c2HorizLoc, this.glob15c4VertLoc, this.glob15c0);

					// free Rect structure
					this.processor.RegStack.Value16 = this.processor.SumWord(this.processor.RegStack.Value16, 0xa);
				}
				else
				{
					throw new Exception($"The screen {displayIdx} is not allocated");
				}
			}
			this.processor.SegExtra.Value16 = this.processor.UnstackWord();
			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();

			// Far return
			this.processor.Journal.CloseScope("FloodBuffer");
			this.processor.Journal = scratchJournal;
		}

		public void DuplicateScreenToMainScreen(ushort displayIdx)
		{
			LogWrapper scratchJournal = this.processor.Journal;
			this.processor.Journal.EmitLine($"// Calling: CopyScreenToMainScreen(0x{displayIdx:x4})");
			this.processor.Journal = this.owner.VideoJournal;
			this.processor.Journal.OpenScope($"DuplicateScreenToMainScreen(0x{displayIdx:x4})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.SegData.Value16);

			if (displayIdx != 0)
			{
				this.processor.StackWord(0); // stack management - push return segment, ignored
				this.processor.StackWord(0x0650); // stack management - push return offset
				FloodBuffer(displayIdx);
				this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

				if (this.displays.IncludesKey(0))
				{
					if (this.displays.IncludesKey(displayIdx))
					{
						lock (this.VideoLatch)
						{
							VGABitmap primaryDisplay = this.displays.ObtainValueByKey(0);
							VGABitmap display = this.displays.ObtainValueByKey(displayIdx);

							primaryDisplay.RenderImage(Point.Empty, display, new Rectangle(0, 0, 320, 100), false);
						}
					}
					else
					{
						throw new Exception($"The screen {displayIdx} is not allocated");
					}
				}
				else
				{
					throw new Exception($"The screen 0 is not allocated");
				}
			}

			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("'CopyScreenToMainScreen'");
			this.processor.Journal = scratchJournal;
		}

		#region Palette functions
		public void AssignHuesByIndexArray(ushort ordinalListAddr)
		{
			// function body
			lock (this.VideoLatch)
			{
				Color[] hues = new Color[16];

				for (int idx = 0; idx < 16; idx++)
				{
					hues[idx] = VGABitmap.Swatch1[this.processor.PullUInt8(this.processor.SegData.Value16, ordinalListAddr)];

					ordinalListAddr++;
				}

				// set colors to all planes, as this is what original code does
				for (int idx = 0; idx < this.displays.Count; idx++)
				{
					this.displays[idx].Datum.AssignHuesFromArray(hues, 0, 0, 16);
				}
			}
		}

		public void AssignHue(int ordinal, Color hue)
		{
			lock (this.VideoLatch)
			{
				// set colors to all planes, as this is what original code does
				for (int idx = 0; idx < this.displays.Count; idx++)
				{
					this.displays[idx].Datum.AssignHue(ordinal, hue);
				}
			}
		}

		public void AssignHuesFromHueStruct(ushort hueLayoutAddr)
		{
			// function body
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

					// set colors to all planes, as this is what original code does
					for (int idx = 0; idx < this.displays.Count; idx++)
					{
						this.displays[idx].Datum.AssignHuesFromArray(hues, since, since, total);
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

					// set colors to all screens, as this is what original code does
					for (int idx = 0; idx < this.displays.Count; idx++)
					{
						this.displays[idx].Datum.AssignHuesFromArray(hues, since, since, total);
					}
				}
			}
		}
		#endregion

		#region Drawing functions
		public void RenderScreenToMainScreen(ushort displayIdx, ushort arg1)
		{
			// function body
			// this is either random fade in image, or a scrolling from right to left
			if (displayIdx != 0)
			{
				if (this.displays.IncludesKey(displayIdx))
				{
					lock (this.VideoLatch)
					{
						VGABitmap display0 = this.displays.ObtainValueByKey(0);

						display0.RenderImage(this.displays.ObtainValueByKey(displayIdx));
					}
				}
				else
				{
					throw new Exception($"The screen {displayIdx} is not allocated");
				}
			}
		}

		public void RenderImage(ushort originBoxAddr, int horizSince, int vertSince, int breadth, int altitude, ushort targetBoxAddr, int horizToward, int vertToward)
		{
			CivRectangle boxSince = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, originBoxAddr));
			int horizDisplacementSince = boxSince.Horiz + horizSince;
			int vertDisplacementSince = boxSince.Vert + vertSince;

			CivRectangle boxToward = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, targetBoxAddr));
			int horizDisplacementToward = boxToward.Horiz + horizToward;
			int vertDisplacementToward = boxToward.Vert + vertToward;

			// function body
			if (this.displays.IncludesKey(boxSince.DisplayIdx))
			{
				VGABitmap originPicture = this.displays.ObtainValueByKey(boxSince.DisplayIdx);

				if (this.displays.IncludesKey(boxToward.DisplayIdx))
				{
					lock (this.VideoLatch)
					{
						VGABitmap targetPicture = this.displays.ObtainValueByKey(boxToward.DisplayIdx);

						targetPicture.RenderImage(horizDisplacementToward, vertDisplacementToward, originPicture, new Rectangle(horizDisplacementSince, vertDisplacementSince, breadth, altitude), false);
					}
				}
				else
				{
					throw new Exception($"The screen {boxToward.DisplayIdx} is not allocated");
				}
			}
			else
			{
				throw new Exception($"The screen {boxSince.DisplayIdx} is not allocated");
			}
		}

		public ushort ScreenToPicture(ushort displayIdx, ushort horizLoc, ushort vertLoc, ushort breadth, ushort altitude)
		{
			// function body
			if (this.displays.IncludesKey(displayIdx))
			{
				lock (this.VideoLatch)
				{
					VGABitmap display = this.displays.ObtainValueByKey(displayIdx);
					VGABitmap picture = new VGABitmap(breadth, altitude);
					Rectangle box = new Rectangle(horizLoc, vertLoc, breadth, altitude);
					display.DuplicateColors(picture);

					picture.RenderImage(Point.Empty, display, box, false);

					this.pictures.Add(this.pictureFollowingIdx, picture);

					this.processor.RegAcc.Value16 = (ushort)this.pictureFollowingIdx;
					this.pictureFollowingIdx++;
				}
			}
			else
			{
				throw new Exception($"The screen {displayIdx} is not allocated");
			}

			return this.processor.RegAcc.Value16;
		}

		public void RenderPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			// function body
			CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));

			if (this.displays.IncludesKey(box.DisplayIdx))
			{
				if (this.pictures.IncludesKey(pictureAddr))
				{
					lock (this.VideoLatch)
					{
						VGABitmap display = this.displays.ObtainValueByKey(box.DisplayIdx);
						VGABitmap picture = this.pictures.ObtainValueByKey(pictureAddr);

						display.RenderImage(box.Horiz + horizLoc, box.Vert + vertLoc, picture, true);
					}
				}
				else
				{
					this.processor.Journal.EmitLine($"The bitmap 0x{pictureAddr:x4} is not allocated");
				}
			}
			else
			{
				throw new Exception($"The screen {box.DisplayIdx} is not allocated");
			}
		}

		public void RenderPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, int pictureAddr)
		{
			// function body
			CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));

			if (this.displays.IncludesKey(box.DisplayIdx))
			{
				if (this.pictures.IncludesKey(pictureAddr))
				{
					lock (this.VideoLatch)
					{
						VGABitmap display = this.displays.ObtainValueByKey(box.DisplayIdx);
						VGABitmap picture = this.pictures.ObtainValueByKey(pictureAddr);

						display.RenderImage(box.Horiz + horizLoc, box.Vert + vertLoc, picture, true);
					}
				}
				else
				{
					this.processor.Journal.EmitLine($"The bitmap 0x{pictureAddr:x4} is not allocated");
				}
			}
			else
			{
				throw new Exception($"The screen {box.DisplayIdx} is not allocated");
			}
		}

		public ushort ImportIcon(string docname)
		{
			byte[] scratchList;
			VGABitmap? picture = VGABitmap.ViaFile(this.processor.StandardFolder + docname.ToUpper(), out scratchList);

			if (picture != null)
			{
				this.pictures.Add(this.pictureFollowingIdx, picture);

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
			// function body
			if (this.displays.IncludesKey(displayIdx))
			{
				lock (this.VideoLatch)
				{
					this.processor.RegAcc.Value16 = this.displays.ObtainValueByKey(displayIdx).ObtainDot(horizLoc, vertLoc);
				}
			}
			else
			{
				throw new Exception($"The screen {displayIdx} is not allocated");
			}

			return this.processor.RegAcc.Value16;
		}

		public void AssignDot(ushort displayIdx, int horizLoc, int vertLoc, byte foreHue, byte dotStyle)
		{
			// function body
			if (this.displays.IncludesKey(displayIdx))
			{
				lock (this.VideoLatch)
				{
					VGABitmap display = this.displays.ObtainValueByKey(displayIdx);

					display.AssignDot(horizLoc, vertLoc, foreHue, (PixelWriteModeEnum)dotStyle);
				}
			}
			else
			{
				throw new Exception($"The screen {displayIdx} is not allocated");
			}
		}

		public void RenderLine(ushort boxAddr, int horiz1, int vert1, int horiz2, int vert2, ushort style)
		{
			// function body
			CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));

			if (this.displays.IncludesKey(box.DisplayIdx))
			{
				lock (this.VideoLatch)
				{
					VGABitmap display = this.displays.ObtainValueByKey(box.DisplayIdx);

					horiz1 += box.Horiz;
					vert1 += box.Vert;
					horiz2 += box.Horiz;
					vert2 += box.Vert;

					display.RenderLine(horiz1, vert1, horiz2, vert2, (byte)(style & 0xff), (PixelWriteModeEnum)((style & 0xff00) >> 8));
				}
			}
			else
			{
				throw new Exception($"The screen {box.DisplayIdx} is not allocated");
			}
		}

		public void FloodBox(int displayIdx, Rectangle box, byte foreHue, byte dotStyle)
		{
			// function body
			if (this.displays.IncludesKey(displayIdx))
			{
				lock (this.VideoLatch)
				{
					this.displays.ObtainValueByKey(displayIdx).FloodBox(box, foreHue, (PixelWriteModeEnum)dotStyle);
				}
			}
			else
			{
				throw new Exception($"The screen {displayIdx} is not allocated");
			}
		}

		public void SubstituteHue(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, byte priorHue, byte freshHue)
		{
			// function body
			lock (this.VideoLatch)
			{
				CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));
				int headInt = box.Horiz + horizLoc;
				int remainingInt = box.Vert + vertLoc;

				if (this.displays.IncludesKey(box.DisplayIdx))
				{
					this.displays.ObtainValueByKey(box.DisplayIdx).SubstituteHue(new Rectangle(headInt, remainingInt, breadth, altitude), priorHue, freshHue);
				}
				else
				{
					throw new Exception($"The screen {box.DisplayIdx} is not allocated");
				}
			}
		}
		#endregion

		#region Fonts
		public ushort ObtainCharWidth(ushort typefaceIdx, byte chr)
		{
			// function body
			this.processor.RegAcc.Value16 = (ushort)ObtainRenderTextSize(typefaceIdx, new string((char)chr, 1)).Width;

			return this.processor.RegAcc.Value16;
		}

		public Size ObtainRenderTextSize(int typefaceIdx, string words)
		{
			int breadthInt = 0;
			int elevation = 0;

			if (!this.typefaces.IncludesKey(typefaceIdx))
			{
				typefaceIdx = 1; // default font
			}

			CivFont typeface = this.typefaces.ObtainValueByKey(typefaceIdx);

			for (int idx = 0; idx < words.Length; idx++)
			{
				char chr = words[idx];
				CivFontCharacter typefaceChr;

				breadthInt += typeface.LetterBreadthGap;

				if (typeface.Letters.IncludesKey(chr))
				{
					typefaceChr = typeface.Letters.ObtainValueByKey(chr);
				}
				else
				{
					// unknown char, use '?'
					typefaceChr = typeface.Letters.ObtainValueByKey('?');
				}
				breadthInt += typefaceChr.Breadth;
				elevation = Math.Max(elevation, typefaceChr.Altitude);
				elevation += typeface.StrokeGap;
			}

			return new Size(breadthInt, elevation);
		}

		public ushort ObtainTextHeight(ushort typefaceIdx)
		{
			// function body
			this.processor.RegAcc.Value16 = (ushort)ObtainRenderTextSize(typefaceIdx, "?").Height;

			return this.processor.RegAcc.Value16;
		}

		public void RenderText(ushort boxAddr, int horizLoc, int vertLoc, ushort charsAddr)
		{
			CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));
			string words = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr));

			if (!this.typefaces.IncludesKey(box.TypefaceIdx))
			{
				box.TypefaceIdx = 1; // default font
			}

			// function body
			CivFont typeface = this.typefaces.ObtainValueByKey(box.TypefaceIdx);

			if (this.displays.IncludesKey(box.DisplayIdx))
			{
				lock (this.VideoLatch)
				{
					VGABitmap display = this.displays.ObtainValueByKey(box.DisplayIdx);
					Rectangle box1 = new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, box.Breadth, box.Altitude);

					display.RenderText(words, typeface, box1, box.ForeHue, box.RearHue, (PixelWriteModeEnum)box.DotStyle);
				}
			}
			else
			{
				throw new Exception($"The screen {box.DisplayIdx} is not allocated");
			}
		}
		#endregion
	}
}
