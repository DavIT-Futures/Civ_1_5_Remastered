using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Civilization1Remaster.Imaging
{
	public class VGABitmap : IDisposable
	{
		protected bool Shown { get; set; } = true;

		private readonly int pitch;
		private readonly byte[] pictureStorage;
		private readonly Color[] palette = new Color[256];
		private readonly int[] swatchRgba = new int[256];

		private Rectangle box;
		private bool edited = true;

		public VGABitmap() : this(new Size(320, 200))
		{ }

		public VGABitmap(Size extent) : this(extent.Width, extent.Height)
		{ }

		public VGABitmap(int breadth, int altitude)
		{
			this.pitch = (int)(Math.Ceiling((double)breadth / 4.0) * 4.0);
			this.pictureStorage = new byte[this.pitch * altitude];
			this.box = new Rectangle(0, 0, breadth, altitude);

			for (int idx = 0; idx < StandardSwatch.Length && idx < this.palette.Length; idx++)
			{
				this.palette[idx] = StandardSwatch[idx];
				this.swatchRgba[idx] = ToFullArgb(StandardSwatch[idx]);
			}
		}

		~VGABitmap()
		{
			this.Dispose();
		}

		public static Color[] StandardSwatch { get; } = new Color[] {
			Color.FromArgb(0x000000),
			Color.FromArgb(0x00002a),
			Color.FromArgb(0x002a00),
			Color.FromArgb(0x002a2a),
			Color.FromArgb(0x2a0000),
			Color.FromArgb(0x2a002a),
			Color.FromArgb(0x2a1500),
			Color.FromArgb(0x2a2a2a),
			Color.FromArgb(0x151515),
			Color.FromArgb(0x15153f),
			Color.FromArgb(0x153f15),
			Color.FromArgb(0x153f3f),
			Color.FromArgb(0x3f1515),
			Color.FromArgb(0x3f153f),
			Color.FromArgb(0x3f3f15),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x000000),
			Color.FromArgb(0x0a0a0a),
			Color.FromArgb(0x151515),
			Color.FromArgb(0x1f1f1f),
			Color.FromArgb(0x2a2a2a),
			Color.FromArgb(0x353535),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x150000),
			Color.FromArgb(0x1f0a0a),
			Color.FromArgb(0x2a1515),
			Color.FromArgb(0x2a0000),
			Color.FromArgb(0x2a0a00),
			Color.FromArgb(0x350a0a),
			Color.FromArgb(0x35150a),
			Color.FromArgb(0x351f1f),
			Color.FromArgb(0x3f1515),
			Color.FromArgb(0x3f2a2a),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x150a00),
			Color.FromArgb(0x1f150a),
			Color.FromArgb(0x2a1f15),
			Color.FromArgb(0x2a1500),
			Color.FromArgb(0x351f0a),
			Color.FromArgb(0x352a1f),
			Color.FromArgb(0x3f2a15),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x151500),
			Color.FromArgb(0x1f1f0a),
			Color.FromArgb(0x151f00),
			Color.FromArgb(0x2a2a15),
			Color.FromArgb(0x35351f),
			Color.FromArgb(0x352a0a),
			Color.FromArgb(0x3f3f2a),
			Color.FromArgb(0x3f3f15),
			Color.FromArgb(0x001500),
			Color.FromArgb(0x151f15),
			Color.FromArgb(0x0a1f0a),
			Color.FromArgb(0x1f2a1f),
			Color.FromArgb(0x152a15),
			Color.FromArgb(0x1f2a0a),
			Color.FromArgb(0x002a00),
			Color.FromArgb(0x002a15),
			Color.FromArgb(0x1f351f),
			Color.FromArgb(0x1f350a),
			Color.FromArgb(0x0a350a),
			Color.FromArgb(0x0a351f),
			Color.FromArgb(0x2a3f2a),
			Color.FromArgb(0x2a3f15),
			Color.FromArgb(0x153f15),
			Color.FromArgb(0x153f2a),
			Color.FromArgb(0x001515),
			Color.FromArgb(0x0a1f1f),
			Color.FromArgb(0x152a2a),
			Color.FromArgb(0x002a2a),
			Color.FromArgb(0x1f3535),
			Color.FromArgb(0x0a3535),
			Color.FromArgb(0x2a3f3f),
			Color.FromArgb(0x153f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x000015),
			Color.FromArgb(0x0a0a1f),
			Color.FromArgb(0x15152a),
			Color.FromArgb(0x00152a),
			Color.FromArgb(0x00002a),
			Color.FromArgb(0x15002a),
			Color.FromArgb(0x0a0a35),
			Color.FromArgb(0x0a1f35),
			Color.FromArgb(0x1f0a35),
			Color.FromArgb(0x1f1f35),
			Color.FromArgb(0x15153f),
			Color.FromArgb(0x152a3f),
			Color.FromArgb(0x2a153f),
			Color.FromArgb(0x2a2a3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x150a15),
			Color.FromArgb(0x150015),
			Color.FromArgb(0x1f151f),
			Color.FromArgb(0x1f0a1f),
			Color.FromArgb(0x2a152a),
			Color.FromArgb(0x2a002a),
			Color.FromArgb(0x351f35),
			Color.FromArgb(0x350a35),
			Color.FromArgb(0x3f153f),
			Color.FromArgb(0x3f2a3f),
			Color.FromArgb(0x3f3f3f),
			Color.FromArgb(0x2a0a15),
			Color.FromArgb(0x2a0015),
			Color.FromArgb(0x35151f),
			Color.FromArgb(0x350a1f),
			Color.FromArgb(0x3f152a)
		};

		public static Color[] Swatch1 { get; } = new Color[] {
			Color.FromArgb(0x000000),
			Color.FromArgb(0x0000a7),
			Color.FromArgb(0x00a700),
			Color.FromArgb(0x00a7a7),
			Color.FromArgb(0xa70000),
			Color.FromArgb(0xa700a7),
			Color.FromArgb(0xa75300),
			Color.FromArgb(0xa7a7a7),
			Color.FromArgb(0x535353),
			Color.FromArgb(0x5353fb),
			Color.FromArgb(0x53fb53),
			Color.FromArgb(0x53fbfb),
			Color.FromArgb(0xfb5353),
			Color.FromArgb(0xfb53fb),
			Color.FromArgb(0xfbfb53),
			Color.FromArgb(0xfbfbfb)
		};

		public Color[] Palette
		{
			get { return this.palette; }
		}

		public int Pitch
		{
			get { return this.pitch; }
		}

		public Rectangle Box
		{
			get { return this.box; }
		}

		public Size Extent
		{
			get { return this.box.Size; }
		}

		public byte[] PictureStorage
		{
			get { return this.pictureStorage; }
		}

		public bool Edited
		{
			get { return this.edited; }
			set { this.edited = value; }
		}

		public bool Seen
		{
			get { return this.Shown; }
			set { this.Shown = value; }
		}

		public int[] SwatchRgba
		{
			get { return this.swatchRgba; }
		}

		public static Color Hue18ToHue(int crimson, int emerald, int azure)
		{
			return Color.FromArgb((255 * (crimson & 0x3f)) / 63, (255 * (emerald & 0x3f)) / 63, (255 * (azure & 0x3f)) / 63);
		}

		public static Color HueToHue18(Color hue)
		{
			return Color.FromArgb(Level18(hue.R), Level18(hue.G), Level18(hue.B));
		}

		private static int Level18(byte level)
		{
			return (63 * level + 127) / 255;
		}

		public static VGABitmap? ViaFile(string route, out byte[] swatch)
		{
			VGABitmap? picture = null;
			using FileStream flow = new FileStream(route, FileMode.Open, FileAccess.Read);
			List<KeyValuePair<int, Color>> colors = new List<KeyValuePair<int, Color>>();
			swatch = new byte[0];
			
			foreach ((ushort stamp, byte[] chunk) in ReadBlocks(flow))
			{
				MemoryStream chunkSource;
				ushort breadthInt;
				ushort elevation;
				bool sized;
				int peakDigits;

				switch (stamp)
				{
					case 0x3045:
						break;

					case 0x304d:
						swatch = ReadPaletteBlock(stamp, chunk, colors);
						break;

					case 0x3058:
						chunkSource = new MemoryStream(chunk);
						sized = chunkSource.TryPullUInt16(out breadthInt) & chunkSource.TryPullUInt16(out elevation);
						peakDigits = chunkSource.ReadByte();

						if (sized && peakDigits > 7)
						{
							MemoryStream lzEmitted = new MemoryStream();
							LZW.Unpack(lzEmitted, chunkSource, 9, peakDigits);
							lzEmitted.Position = 0;

							MemoryStream runlenEmitted = new MemoryStream();
							RLE.Unpack(runlenEmitted, lzEmitted);
							runlenEmitted.Position = 0;

							int pitchSpan = (int)(Math.Ceiling((double)breadthInt / 4.0) * 4.0);
							int sweepPlace = 0;

							picture = new VGABitmap(breadthInt, elevation);

							for (int idx = 0; idx < elevation; idx++)
							{
								for (int jdx = 0; jdx < breadthInt; jdx++)
								{
									int chrAlt = runlenEmitted.ReadByte();
									if (chrAlt < 0)
										break;

									picture.pictureStorage[sweepPlace] = (byte)chrAlt;
									sweepPlace++;
								}
								sweepPlace += pitchSpan - breadthInt;
							}

							for (int idx = 0; idx < colors.Count; idx++)
							{
								picture.palette[colors[idx].Key] = colors[idx].Value;
								picture.swatchRgba[colors[idx].Key] = colors[idx].Value.ToArgb();
							}
						}
						else
						{
							throw new Exception($"Image block type 0x{stamp:x4} malformed");
						}
						break;

					case 0x3158:
						chunkSource = new MemoryStream(chunk);
						sized = chunkSource.TryPullUInt16(out breadthInt) & chunkSource.TryPullUInt16(out elevation);
						peakDigits = chunkSource.ReadByte();

						if (sized && peakDigits > 7)
						{
							MemoryStream lzEmitted = new MemoryStream();
							LZW.Unpack(lzEmitted, chunkSource, 9, peakDigits);
							lzEmitted.Position = 0;

							MemoryStream runlenEmitted = new MemoryStream();
							RLE.Unpack(runlenEmitted, lzEmitted);
							runlenEmitted.Position = 0;

							int pitchSpan = (int)(Math.Ceiling((double)breadthInt / 2.0) * 4.0);
							int sweepPlace = 0;

							picture = new VGABitmap(breadthInt * 2, elevation);

							for (int idx = 0; idx < elevation; idx++)
							{
								for (int jdx = 0; jdx < breadthInt; jdx += 2)
								{
									int chrAlt = runlenEmitted.ReadByte();
									if (chrAlt < 0)
										break;

									picture.pictureStorage[sweepPlace] = (byte)(chrAlt & 0xf);
									sweepPlace++;
									picture.pictureStorage[sweepPlace] = (byte)((chrAlt & 0xf0) >> 4);
									sweepPlace++;
								}
								sweepPlace += pitchSpan - breadthInt;
							}

							for (int idx = 0; idx < colors.Count; idx++)
							{
								picture.palette[colors[idx].Key] = colors[idx].Value;
								picture.swatchRgba[colors[idx].Key] = ToFullArgb(colors[idx].Value);
							}
						}
						else
						{
							throw new Exception($"Image block type 0x{stamp:x4} malformed");
						}
						break;

					default:
						throw new Exception($"Undefined block type 0x{stamp:x4}");
				}
			}

			return picture;
		}

		public static List<KeyValuePair<int, Color>> ColorsFromFile(string route, out byte[] swatch)
		{
			using FileStream flow = new FileStream(route, FileMode.Open, FileAccess.Read);
			List<KeyValuePair<int, Color>> colors = new List<KeyValuePair<int, Color>>();
			swatch = new byte[0];

			foreach ((ushort stamp, byte[] chunk) in ReadBlocks(flow))
			{
				switch (stamp)
				{
					case 0x304d:
						swatch = ReadPaletteBlock(stamp, chunk, colors);
						break;

					case 0x3045:
					case 0x3058:
					case 0x3158:
						break;

					default:
						throw new Exception($"Undefined block type 0x{stamp:x4}");
				}
			}

			return colors;
		}

		private static IEnumerable<(ushort Stamp, byte[] Chunk)> ReadBlocks(Stream flow)
		{
			while (flow.TryPullUInt16(out ushort stamp) && flow.TryPullUInt16(out ushort chunkSpan))
			{
				byte[] chunk = new byte[chunkSpan];
				int chunkExtent = flow.Read(chunk, 0, chunkSpan);

				if (chunkSpan != chunkExtent)
				{
					throw new Exception(
						$"Block type 0x{stamp:x4} data missing, expected {chunkSpan} bytes, " +
						$"but read only {chunkExtent} bytes");
				}

				yield return (stamp, chunk);
			}
		}

		private static byte[] ReadPaletteBlock(ushort stamp, byte[] chunk, List<KeyValuePair<int, Color>> colors)
		{
			if (chunk.Length < 2)
				throw new Exception($"Palette block type 0x{stamp:x4} malformed");

			int slot = chunk[0];
			int hueTally = chunk[1] - slot + 1;

			for (int idx = 0; idx < hueTally; idx++)
			{
				int place = 2 + idx * 3;

				if (place + 3 > chunk.Length)
					throw new Exception($"Palette block type 0x{stamp:x4} malformed");

				colors.Add(new KeyValuePair<int, Color>(slot + idx, Hue18ToHue(chunk[place], chunk[place + 1], chunk[place + 2])));
			}

			byte[] swatch = new byte[4 + chunk.Length];
			swatch[0] = 0x4d;
			swatch[1] = 0x30;
			swatch[2] = (byte)chunk.Length;
			swatch[3] = (byte)(chunk.Length >> 8);
			chunk.CopyTo(swatch, 4);

			return swatch;
		}

		public void Dispose()
		{
		}

		public Color ObtainHue(int ordinal)
		{
			return Color.FromArgb(this.swatchRgba[ordinal]);
		}

		public void AssignHue(int ordinal, Color hue)
		{
			this.palette[ordinal] = hue;
			this.swatchRgba[ordinal] = ToFullArgb(hue);
			this.edited = true;
		}

		public void AssignHuesFromHueStruct(byte[] hueLayout)
		{
			int layoutLoc = 0;

			if (hueLayout.Length > 0 && hueLayout[layoutLoc] == 0x4d && hueLayout[layoutLoc + 1] == 0x30)
			{
				int since = hueLayout[layoutLoc + 4];
				int toward = hueLayout[layoutLoc + 5];
				int total = (toward - since) + 1;
				layoutLoc += 6;

				Color[] hues = new Color[total];

				for (int idx = 0; idx < total; idx++)
				{
					hues[idx] = VGABitmap.Hue18ToHue(hueLayout[layoutLoc + (idx * 3)],
						hueLayout[layoutLoc + (idx * 3) + 1],
						hueLayout[layoutLoc + (idx * 3) + 2]);
				}

				this.AssignHuesFromArray(hues, since, since, total);
			}
		}

		public void AssignHuesFromArray(Color[] shades, int originOrdinal, int targetOrdinal, int span)
		{
			for (int idx = 0; idx < span; idx++)
			{
				Color shade = shades[originOrdinal + idx];
				this.palette[targetOrdinal + idx] = shade;
				this.swatchRgba[targetOrdinal + idx] = ToFullArgb(shade);
			}

			this.edited = true;
		}

		public void DuplicateColors(VGABitmap picture)
		{
			for (int idx = 0; idx < picture.Palette.Length; idx++)
			{
				this.palette[idx] = picture.Palette[idx];
				this.swatchRgba[idx] = picture.SwatchRgba[idx];
			}
		}

		public void SubstituteHue(Rectangle box, byte priorHue, byte freshHue)
		{
			if (box.IntersectsWith(this.box))
			{
				Rectangle freshBox = new Rectangle(box.Location, box.Size);
				freshBox.Intersect(this.box);

				int pictureStorageAddr = (freshBox.Top * this.pitch) + freshBox.Left;

				for (int idx = 0; idx < freshBox.Height; idx++)
				{
					for (int jdx = 0; jdx < freshBox.Width; jdx++)
					{
						if (pictureStorage[pictureStorageAddr + jdx] == priorHue)
						{
							this.edited = true;
							pictureStorage[pictureStorageAddr + jdx] = freshHue;
						}
					}

					pictureStorageAddr += this.pitch;
				}
				this.edited = true;
			}
		}

		public byte ObtainDot(int px, int py)
		{
			if (this.box.Contains(px, py))
			{
				return pictureStorage[(py * this.pitch) + px];
			}

			return 0;
		}

		public void AssignDot(int px, int py, byte hue)
		{
			if (this.box.Contains(px, py))
			{
				pictureStorage[(py * this.pitch) + px] = hue;
				this.edited = true;
			}
		}

		public void AssignDot(int px, int py, byte hue, PixelWriteMode style)
		{
			if (this.box.Contains(px, py))
			{
				switch (style)
				{
					case PixelWriteMode.Normal:
						pictureStorage[(py * this.pitch) + px] = hue;
						break;

					case PixelWriteMode.And:
						pictureStorage[(py * this.pitch) + px] &= hue;
						break;

					case PixelWriteMode.Or:
						pictureStorage[(py * this.pitch) + px] |= hue;
						break;

					case PixelWriteMode.Xor:
						pictureStorage[(py * this.pitch) + px] ^= hue;
						break;
				}

				this.edited = true;
			}
		}

		public void RenderLine(int horiz1, int vert1, int horiz2, int vert2, byte hue, PixelWriteMode style)
		{
			int breadthInt;
			int breadthFolder = 1;
			int elevation;
			int altitudeFolder = 1;

			if (horiz1 > horiz2)
			{
				breadthInt = (horiz1 - horiz2) + 1;
				breadthFolder = -1;
			}
			else
			{
				breadthInt = (horiz2 - horiz1) + 1;
			}

			if (vert1 > vert2)
			{
				elevation = (vert1 - vert2) + 1;
				altitudeFolder = -1;
			}
			else
			{
				elevation = (vert2 - vert1) + 1;
			}

			if (breadthInt == 1 && elevation == 1)
			{
				this.AssignDot(horiz1, vert1, hue, style);
			}
			else if (breadthInt > 1 && elevation == 1)
			{
				for (int idx = 0; idx < breadthInt; idx++)
				{
					this.AssignDot(horiz1 + idx * breadthFolder, vert1, hue, style);
				}
			}
			else if (breadthInt == 1 && elevation > 1)
			{
				for (int idx = 0; idx < elevation; idx++)
				{
					this.AssignDot(horiz1, vert1 + idx * altitudeFolder, hue, style);
				}
			}
			else if (breadthInt > elevation)
			{
				double vertAdvance = (double)elevation / (double)breadthInt;

				for (int idx = 0; idx < breadthInt; idx++)
				{
					this.AssignDot(horiz1 + idx * breadthFolder, vert1 + (int)(vertAdvance * idx) * altitudeFolder, hue, style);
				}
			}
			else
			{
				double horizAdvance = (double)breadthInt / (double)elevation;

				for (int idx = 0; idx < elevation; idx++)
				{
					this.AssignDot(horiz1 + (int)(horizAdvance * idx) * breadthFolder, vert1 + idx * altitudeFolder, hue, style);
				}
			}
		}

		public void FloodBox(Rectangle box, byte hue, PixelWriteMode style)
		{
			if (box.IntersectsWith(this.box))
			{
				Rectangle freshBox = new Rectangle(box.Location, box.Size);
				freshBox.Intersect(this.box);

				int pictureStorageAddr = (freshBox.Top * this.pitch) + freshBox.Left;

				for (int idx = 0; idx < freshBox.Height; idx++)
				{
					for (int jdx = 0; jdx < freshBox.Width; jdx++)
					{
						switch (style)
						{
							case PixelWriteMode.Normal:
								pictureStorage[pictureStorageAddr + jdx] = hue;
								break;

							case PixelWriteMode.And:
								pictureStorage[pictureStorageAddr + jdx] &= hue;
								break;

							case PixelWriteMode.Or:
								pictureStorage[pictureStorageAddr + jdx] |= hue;
								break;

							case PixelWriteMode.Xor:
								pictureStorage[pictureStorageAddr + jdx] ^= hue;
								break;
						}
					}

					pictureStorageAddr += this.pitch;
				}
				this.edited = true;
			}
		}

		public void RenderImage(VGABitmap originDisplay)
		{
			this.RenderImage(Point.Empty, originDisplay, originDisplay.Box, false);
		}

		public void RenderImage(int px, int py, VGABitmap originPicture, bool clear)
		{
			RenderImage(new Point(px, py), originPicture, originPicture.Box, clear);
		}

		public void RenderImage(int px, int py, VGABitmap originPicture, Rectangle originBox, bool clear)
		{
			RenderImage(new Point(px, py), originPicture, originBox, clear);
		}

		public void RenderImage(Point targetMark, VGABitmap originPicture, Rectangle originBox, bool clear)
		{
			Rectangle originBox1 = new Rectangle(originBox.Location, originBox.Size);
			originBox1.Intersect(originPicture.Box);

			if (targetMark.X < 0)
			{
				originBox1.X -= targetMark.X;
				originBox1.Width += targetMark.X;
				targetMark.X = 0;
			}
			if (targetMark.Y < 0)
			{
				originBox1.Y -= targetMark.Y;
				originBox1.Height += targetMark.Y;
				targetMark.Y = 0;
			}

			Rectangle targetBox = new Rectangle(targetMark, originBox1.Size);
			targetBox.Intersect(this.box);

			if (targetBox.Width > 0 && targetBox.Height > 0)
			{
				originBox1.Size = targetBox.Size;

				int originPlace = (originPicture.Pitch * originBox1.Y) + originBox1.X;
				int targetPlace = (this.pitch * targetBox.Y) + targetBox.X;

				for (int idx = 0; idx < targetBox.Height; idx++)
				{
					for (int jdx = 0; jdx < targetBox.Width; jdx++)
					{
						if (originPicture.PictureStorage[originPlace + jdx] != 0 || !clear)
						{
							this.pictureStorage[targetPlace + jdx] = originPicture.PictureStorage[originPlace + jdx];
						}
					}

					originPlace += originPicture.Pitch;
					targetPlace += this.pitch;
				}
				this.edited = true;
			}
		}

		public void RenderText(string words, CivFont typeface, Rectangle box, byte foreHue, byte rearHue, PixelWriteMode emitStyle)
		{
			Rectangle box1 = new Rectangle(box.Location, box.Size);
			box1.Intersect(this.box);

			if (box1.Width > 0 && box1.Height > 0)
			{
				int picturePlace = (this.pitch * box1.Y) + box1.X;

				for (int idx = 0; idx < words.Length; idx++)
				{
					char chr = words[idx];
					foreHue = (byte)((chr > 0x7f) ? (chr - 0x80) : foreHue);
					CivFontCharacter typefaceChr;

					if (chr > 0x7f)
						continue;

					if (idx > 0)
						picturePlace += typeface.LetterBreadthGap;

					if (typeface.Letters.ContainsKey(chr))
					{
						typefaceChr = typeface.Letters[chr];
					}
					else
					{
						typefaceChr = typeface.Letters['?'];
					}

					int picturePlace1 = picturePlace;

					for (int jdx = 0; jdx < typefaceChr.Altitude; jdx++)
					{
						for (int kdx = 0; kdx < typefaceChr.Breadth; kdx++)
						{
							byte hue = (byte)((typefaceChr.Picture[jdx][kdx] != 0) ? foreHue : rearHue);

							if (typefaceChr.Picture[jdx][kdx] != 0 )
							{
								switch (emitStyle)
								{
									case PixelWriteMode.Normal:
										this.pictureStorage[picturePlace1 + kdx] = hue;
										break;

									case PixelWriteMode.And:
										this.pictureStorage[picturePlace1 + kdx] &= hue;
										break;

									case PixelWriteMode.Or:
										this.pictureStorage[picturePlace1 + kdx] |= hue;
										break;

									case PixelWriteMode.Xor:
										this.pictureStorage[picturePlace1 + kdx] ^= hue;
										break;
								}
							}
						}

						picturePlace1 += this.pitch;
					}

					picturePlace += typefaceChr.Breadth;
				}

				this.edited = true;
			}
		}

		public void Store(string route)
		{
			Store(route, true);
		}

		public void Store(string route, bool storeSwatch)
		{
			using FileStream sink = new FileStream(route, FileMode.Create);
			int spanInt;

			if (storeSwatch)
			{
				spanInt = 2 + 256 * 3;

				sink.WriteByte(0x4d);
				sink.WriteByte(0x30);

				sink.EmitUInt16((ushort)spanInt);

				sink.WriteByte(0);
				sink.WriteByte(255);

				for (int idx = 0; idx < 256; idx++)
				{
					Color hue = HueToHue18(this.palette[idx]);
					sink.WriteByte(hue.R);
					sink.WriteByte(hue.G);
					sink.WriteByte(hue.B);
				}
			}

			MemoryStream runlenIncoming = new MemoryStream();
			int pictureLocation = 0;

			for (int idx = 0; idx < this.box.Height; idx++)
			{
				for (int jdx = 0; jdx < this.box.Width; jdx++)
				{
					runlenIncoming.WriteByte(pictureStorage[pictureLocation + jdx]);
				}
				pictureLocation += this.pitch;
			}

			MemoryStream lzIncoming = new MemoryStream();
			runlenIncoming.Position = 0;
			RLE.Pack(lzIncoming, runlenIncoming, 4);

			MemoryStream lzEmitted = new MemoryStream();
			lzIncoming.Position = 0;
			LZW.Pack(lzEmitted, lzIncoming, 9, 11);

			sink.WriteByte(0x58);
			sink.WriteByte(0x30);

			byte[] staging = lzEmitted.ToArray();
			spanInt = staging.Length + 4;

			sink.EmitUInt16((ushort)spanInt);

			sink.EmitUInt16((ushort)this.box.Width);
			sink.EmitUInt16((ushort)this.box.Height);

			sink.Write(staging, 0, staging.Length);
		}

		public void ImportPicture(string docname, ushort horizLoc, ushort vertLoc, out byte[] swatch)
		{
			VGABitmap? picture = VGABitmap.ViaFile(docname, out swatch);

			if (picture != null)
			{
				for (int idx = 0; idx < picture.Box.Height; idx++)
				{
					for (int jdx = 0; jdx < picture.Box.Width; jdx++)
					{
						this.AssignDot(horizLoc + jdx, vertLoc + idx, picture.ObtainDot(jdx, idx));
					}
				}
			}
		}

		private static int ToFullArgb(Color c)
		{
			return (int)(0xff000000 | (uint)(c.ToArgb() & 0x00ffffff));
		}
	}
}
