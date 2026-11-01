using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Civilization1Remaster.Imaging
{
	public class VideoGraphicsBitmap : IDisposable
	{
		private readonly int pitch;

		private readonly byte[] pictureStorage;
		private readonly Color[] palette = new Color[256];
		private readonly int[] swatchColor = new int[256];
		private Rectangle box;

		private bool edited = true;

		public VideoGraphicsBitmap()
			: this(new Size(320, 200))
		{
		}

		public VideoGraphicsBitmap(Size extent)
			: this(extent.Width, extent.Height)
		{
		}

		public VideoGraphicsBitmap(int breadth, int altitude)
		{
			this.pitch = (int)(Math.Ceiling((double)breadth / 4.0) * 4.0);
			this.pictureStorage = new byte[this.pitch * altitude];
			this.box = new Rectangle(0, 0, breadth, altitude);

			for (int index = 0; index < StandardSwatch.Length && index < this.palette.Length; index++)
			{
				this.palette[index] = StandardSwatch[index];
				this.swatchColor[index] = ToFullColor(StandardSwatch[index]);
			}
		}

		~VideoGraphicsBitmap()
		{
			this.Dispose();
		}

		public static Color[] StandardSwatch { get; } = new Color[]
		{
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

		public static Color[] FirstSwatch { get; } = new Color[]
		{
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

		public int[] SwatchColor
		{
			get { return this.swatchColor; }
		}

		protected bool Shown { get; set; } = true;

		public static Color HueFromDigitalLevels(int crimson, int emerald, int azure)
		{
			return Color.FromArgb((255 * (crimson & 0x3f)) / 63, (255 * (emerald & 0x3f)) / 63, (255 * (azure & 0x3f)) / 63);
		}

		public static Color HueToDigitalLevels(Color hue)
		{
			return Color.FromArgb(DigitalLevel(hue.R), DigitalLevel(hue.G), DigitalLevel(hue.B));
		}

		public static VideoGraphicsBitmap? ViaFile(string route, out byte[] swatch)
		{
			VideoGraphicsBitmap? picture = null;
			using FileStream flow = new FileStream(route, FileMode.Open, FileAccess.Read);
			List<KeyValuePair<int, Color>> colors = new List<KeyValuePair<int, Color>>();
			swatch = new byte[0];

			foreach ((ushort stamp, byte[] chunk) in ReadBlocks(flow))
			{
				MemoryStream chunkSource;
				ushort breadthValue;
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
						sized = chunkSource.TryPullWord(out breadthValue) & chunkSource.TryPullWord(out elevation);
						peakDigits = chunkSource.ReadByte();

						if (sized && peakDigits > 7)
						{
							MemoryStream dictionaryEmitted = new MemoryStream();
							DictionaryCompression.Unpack(dictionaryEmitted, chunkSource, 9, peakDigits);
							dictionaryEmitted.Position = 0;

							MemoryStream runLengthEmitted = new MemoryStream();
							RunLength.Unpack(runLengthEmitted, dictionaryEmitted);
							runLengthEmitted.Position = 0;

							int pitchSpan = (int)(Math.Ceiling((double)breadthValue / 4.0) * 4.0);
							int sweepPlace = 0;

							picture = new VideoGraphicsBitmap(breadthValue, elevation);

							for (int index = 0; index < elevation; index++)
							{
								for (int innerIndex = 0; innerIndex < breadthValue; innerIndex++)
								{
									int characterAlternate = runLengthEmitted.ReadByte();
									if (characterAlternate < 0)
									{
										break;
									}

									picture.pictureStorage[sweepPlace] = (byte)characterAlternate;
									sweepPlace++;
								}

								sweepPlace += pitchSpan - breadthValue;
							}

							for (int index = 0; index < colors.Count; index++)
							{
								picture.palette[colors[index].Key] = colors[index].Value;
								picture.swatchColor[colors[index].Key] = colors[index].Value.ToArgb();
							}
						}
						else
						{
							throw new Exception($"Image block type 0x{stamp:x4} malformed");
						}

						break;

					case 0x3158:
						chunkSource = new MemoryStream(chunk);
						sized = chunkSource.TryPullWord(out breadthValue) & chunkSource.TryPullWord(out elevation);
						peakDigits = chunkSource.ReadByte();

						if (sized && peakDigits > 7)
						{
							MemoryStream dictionaryEmitted = new MemoryStream();
							DictionaryCompression.Unpack(dictionaryEmitted, chunkSource, 9, peakDigits);
							dictionaryEmitted.Position = 0;

							MemoryStream runLengthEmitted = new MemoryStream();
							RunLength.Unpack(runLengthEmitted, dictionaryEmitted);
							runLengthEmitted.Position = 0;

							int pitchSpan = (int)(Math.Ceiling((double)breadthValue / 2.0) * 4.0);
							int sweepPlace = 0;

							picture = new VideoGraphicsBitmap(breadthValue * 2, elevation);

							for (int index = 0; index < elevation; index++)
							{
								for (int innerIndex = 0; innerIndex < breadthValue; innerIndex += 2)
								{
									int characterAlternate = runLengthEmitted.ReadByte();
									if (characterAlternate < 0)
									{
										break;
									}

									picture.pictureStorage[sweepPlace] = (byte)(characterAlternate & 0xf);
									sweepPlace++;
									picture.pictureStorage[sweepPlace] = (byte)((characterAlternate & 0xf0) >> 4);
									sweepPlace++;
								}

								sweepPlace += pitchSpan - breadthValue;
							}

							for (int index = 0; index < colors.Count; index++)
							{
								picture.palette[colors[index].Key] = colors[index].Value;
								picture.swatchColor[colors[index].Key] = ToFullColor(colors[index].Value);
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

		public void Dispose()
		{
		}

		public Color ObtainHue(int ordinal)
		{
			return Color.FromArgb(this.swatchColor[ordinal]);
		}

		public void AssignHue(int ordinal, Color hue)
		{
			this.palette[ordinal] = hue;
			this.swatchColor[ordinal] = ToFullColor(hue);
			this.edited = true;
		}

		public void AssignHuesFromHueStructure(byte[] hueLayout)
		{
			int layoutLocation = 0;

			if (hueLayout.Length > 0 && hueLayout[layoutLocation] == 0x4d && hueLayout[layoutLocation + 1] == 0x30)
			{
				int since = hueLayout[layoutLocation + 4];
				int toward = hueLayout[layoutLocation + 5];
				int total = (toward - since) + 1;
				layoutLocation += 6;

				Color[] hues = new Color[total];

				for (int index = 0; index < total; index++)
				{
					hues[index] = VideoGraphicsBitmap.HueFromDigitalLevels(hueLayout[layoutLocation + (index * 3)],
						hueLayout[layoutLocation + (index * 3) + 1],
						hueLayout[layoutLocation + (index * 3) + 2]);
				}

				this.AssignHuesFromArray(hues, since, since, total);
			}
		}

		public void AssignHuesFromArray(Color[] shades, int originOrdinal, int targetOrdinal, int span)
		{
			for (int index = 0; index < span; index++)
			{
				Color shade = shades[originOrdinal + index];
				this.palette[targetOrdinal + index] = shade;
				this.swatchColor[targetOrdinal + index] = ToFullColor(shade);
			}

			this.edited = true;
		}

		public void DuplicateColors(VideoGraphicsBitmap picture)
		{
			for (int index = 0; index < picture.Palette.Length; index++)
			{
				this.palette[index] = picture.Palette[index];
				this.swatchColor[index] = picture.SwatchColor[index];
			}
		}

		public void SubstituteHue(Rectangle box, byte priorHue, byte freshHue)
		{
			if (box.IntersectsWith(this.box))
			{
				Rectangle freshBox = new Rectangle(box.Location, box.Size);
				freshBox.Intersect(this.box);

				int pictureStorageAddress = (freshBox.Top * this.pitch) + freshBox.Left;

				for (int index = 0; index < freshBox.Height; index++)
				{
					for (int innerIndex = 0; innerIndex < freshBox.Width; innerIndex++)
					{
						if (pictureStorage[pictureStorageAddress + innerIndex] == priorHue)
						{
							this.edited = true;
							pictureStorage[pictureStorageAddress + innerIndex] = freshHue;
						}
					}

					pictureStorageAddress += this.pitch;
				}

				this.edited = true;
			}
		}

		public byte ObtainDot(int pixelHorizontal, int pixelVertical)
		{
			if (this.box.Contains(pixelHorizontal, pixelVertical))
			{
				return pictureStorage[(pixelVertical * this.pitch) + pixelHorizontal];
			}

			return 0;
		}

		public void AssignDot(int pixelHorizontal, int pixelVertical, byte hue)
		{
			if (this.box.Contains(pixelHorizontal, pixelVertical))
			{
				pictureStorage[(pixelVertical * this.pitch) + pixelHorizontal] = hue;
				this.edited = true;
			}
		}

		public void AssignDot(int pixelHorizontal, int pixelVertical, byte hue, PixelWriteMode style)
		{
			if (this.box.Contains(pixelHorizontal, pixelVertical))
			{
				switch (style)
				{
					case PixelWriteMode.Normal:
						pictureStorage[(pixelVertical * this.pitch) + pixelHorizontal] = hue;
						break;

					case PixelWriteMode.And:
						pictureStorage[(pixelVertical * this.pitch) + pixelHorizontal] &= hue;
						break;

					case PixelWriteMode.Or:
						pictureStorage[(pixelVertical * this.pitch) + pixelHorizontal] |= hue;
						break;

					case PixelWriteMode.Xor:
						pictureStorage[(pixelVertical * this.pitch) + pixelHorizontal] ^= hue;
						break;
				}

				this.edited = true;
			}
		}

		public void RenderLine(int firstHorizontal, int firstVertical, int secondHorizontal, int secondVertical, byte hue, PixelWriteMode style)
		{
			int breadthValue;
			int breadthFolder = 1;
			int elevation;
			int altitudeFolder = 1;

			if (firstHorizontal > secondHorizontal)
			{
				breadthValue = (firstHorizontal - secondHorizontal) + 1;
				breadthFolder = -1;
			}
			else
			{
				breadthValue = (secondHorizontal - firstHorizontal) + 1;
			}

			if (firstVertical > secondVertical)
			{
				elevation = (firstVertical - secondVertical) + 1;
				altitudeFolder = -1;
			}
			else
			{
				elevation = (secondVertical - firstVertical) + 1;
			}

			if (breadthValue == 1 && elevation == 1)
			{
				this.AssignDot(firstHorizontal, firstVertical, hue, style);
			}
			else if (breadthValue > 1 && elevation == 1)
			{
				for (int index = 0; index < breadthValue; index++)
				{
					this.AssignDot(firstHorizontal + (index * breadthFolder), firstVertical, hue, style);
				}
			}
			else if (breadthValue == 1 && elevation > 1)
			{
				for (int index = 0; index < elevation; index++)
				{
					this.AssignDot(firstHorizontal, firstVertical + (index * altitudeFolder), hue, style);
				}
			}
			else if (breadthValue > elevation)
			{
				double verticalAdvance = (double)elevation / (double)breadthValue;

				for (int index = 0; index < breadthValue; index++)
				{
					this.AssignDot(firstHorizontal + (index * breadthFolder), firstVertical + ((int)(verticalAdvance * index) * altitudeFolder), hue, style);
				}
			}
			else
			{
				double horizontalAdvance = (double)breadthValue / (double)elevation;

				for (int index = 0; index < elevation; index++)
				{
					this.AssignDot(firstHorizontal + ((int)(horizontalAdvance * index) * breadthFolder), firstVertical + (index * altitudeFolder), hue, style);
				}
			}
		}

		public void FloodBox(Rectangle box, byte hue, PixelWriteMode style)
		{
			if (box.IntersectsWith(this.box))
			{
				Rectangle freshBox = new Rectangle(box.Location, box.Size);
				freshBox.Intersect(this.box);

				int pictureStorageAddress = (freshBox.Top * this.pitch) + freshBox.Left;

				for (int index = 0; index < freshBox.Height; index++)
				{
					for (int innerIndex = 0; innerIndex < freshBox.Width; innerIndex++)
					{
						switch (style)
						{
							case PixelWriteMode.Normal:
								pictureStorage[pictureStorageAddress + innerIndex] = hue;
								break;

							case PixelWriteMode.And:
								pictureStorage[pictureStorageAddress + innerIndex] &= hue;
								break;

							case PixelWriteMode.Or:
								pictureStorage[pictureStorageAddress + innerIndex] |= hue;
								break;

							case PixelWriteMode.Xor:
								pictureStorage[pictureStorageAddress + innerIndex] ^= hue;
								break;
						}
					}

					pictureStorageAddress += this.pitch;
				}

				this.edited = true;
			}
		}

		public void RenderImage(VideoGraphicsBitmap originDisplay)
		{
			this.RenderImage(Point.Empty, originDisplay, originDisplay.Box, false);
		}

		public void RenderImage(int pixelHorizontal, int pixelVertical, VideoGraphicsBitmap originPicture, bool clear)
		{
			RenderImage(new Point(pixelHorizontal, pixelVertical), originPicture, originPicture.Box, clear);
		}

		public void RenderImage(int pixelHorizontal, int pixelVertical, VideoGraphicsBitmap originPicture, Rectangle originBox, bool clear)
		{
			RenderImage(new Point(pixelHorizontal, pixelVertical), originPicture, originBox, clear);
		}

		public void RenderImage(Point targetMark, VideoGraphicsBitmap originPicture, Rectangle originBox, bool clear)
		{
			Rectangle firstOriginBox = new Rectangle(originBox.Location, originBox.Size);
			firstOriginBox.Intersect(originPicture.Box);

			if (targetMark.X < 0)
			{
				firstOriginBox.X -= targetMark.X;
				firstOriginBox.Width += targetMark.X;
				targetMark.X = 0;
			}

			if (targetMark.Y < 0)
			{
				firstOriginBox.Y -= targetMark.Y;
				firstOriginBox.Height += targetMark.Y;
				targetMark.Y = 0;
			}

			Rectangle targetBox = new Rectangle(targetMark, firstOriginBox.Size);
			targetBox.Intersect(this.box);

			if (targetBox.Width > 0 && targetBox.Height > 0)
			{
				firstOriginBox.Size = targetBox.Size;

				int originPlace = (originPicture.Pitch * firstOriginBox.Y) + firstOriginBox.X;
				int targetPlace = (this.pitch * targetBox.Y) + targetBox.X;

				for (int index = 0; index < targetBox.Height; index++)
				{
					for (int innerIndex = 0; innerIndex < targetBox.Width; innerIndex++)
					{
						if (originPicture.PictureStorage[originPlace + innerIndex] != 0 || !clear)
						{
							this.pictureStorage[targetPlace + innerIndex] = originPicture.PictureStorage[originPlace + innerIndex];
						}
					}

					originPlace += originPicture.Pitch;
					targetPlace += this.pitch;
				}

				this.edited = true;
			}
		}

		public void RenderText(string words, CivilizationFont typeface, Rectangle box, byte foregroundHue, byte rearHue, PixelWriteMode emitStyle)
		{
			Rectangle firstBox = new Rectangle(box.Location, box.Size);
			firstBox.Intersect(this.box);

			if (firstBox.Width > 0 && firstBox.Height > 0)
			{
				int picturePlace = (this.pitch * firstBox.Y) + firstBox.X;

				for (int index = 0; index < words.Length; index++)
				{
					char character = words[index];
					foregroundHue = (byte)((character > 0x7f) ? (character - 0x80) : foregroundHue);
					CivilizationFontCharacter typefaceCharacter;

					if (character > 0x7f)
					{
						continue;
					}

					if (index > 0)
					{
						picturePlace += typeface.LetterBreadthGap;
					}

					if (typeface.Letters.ContainsKey(character))
					{
						typefaceCharacter = typeface.Letters[character];
					}
					else
					{
						typefaceCharacter = typeface.Letters['?'];
					}

					int firstPicturePlace = picturePlace;

					for (int innerIndex = 0; innerIndex < typefaceCharacter.Altitude; innerIndex++)
					{
						for (int thirdIndex = 0; thirdIndex < typefaceCharacter.Breadth; thirdIndex++)
						{
							byte hue = (byte)((typefaceCharacter.Picture[innerIndex][thirdIndex] != 0) ? foregroundHue : rearHue);

							if (typefaceCharacter.Picture[innerIndex][thirdIndex] != 0)
							{
								switch (emitStyle)
								{
									case PixelWriteMode.Normal:
										this.pictureStorage[firstPicturePlace + thirdIndex] = hue;
										break;

									case PixelWriteMode.And:
										this.pictureStorage[firstPicturePlace + thirdIndex] &= hue;
										break;

									case PixelWriteMode.Or:
										this.pictureStorage[firstPicturePlace + thirdIndex] |= hue;
										break;

									case PixelWriteMode.Xor:
										this.pictureStorage[firstPicturePlace + thirdIndex] ^= hue;
										break;
								}
							}
						}

						firstPicturePlace += this.pitch;
					}

					picturePlace += typefaceCharacter.Breadth;
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
			int spanValue;

			if (storeSwatch)
			{
				spanValue = 2 + (256 * 3);

				sink.WriteByte(0x4d);
				sink.WriteByte(0x30);

				sink.EmitWord((ushort)spanValue);

				sink.WriteByte(0);
				sink.WriteByte(255);

				for (int index = 0; index < 256; index++)
				{
					Color hue = HueToDigitalLevels(this.palette[index]);
					sink.WriteByte(hue.R);
					sink.WriteByte(hue.G);
					sink.WriteByte(hue.B);
				}
			}

			MemoryStream runLengthIncoming = new MemoryStream();
			int pictureLocation = 0;

			for (int index = 0; index < this.box.Height; index++)
			{
				for (int innerIndex = 0; innerIndex < this.box.Width; innerIndex++)
				{
					runLengthIncoming.WriteByte(pictureStorage[pictureLocation + innerIndex]);
				}

				pictureLocation += this.pitch;
			}

			MemoryStream dictionaryIncoming = new MemoryStream();
			runLengthIncoming.Position = 0;
			RunLength.Pack(dictionaryIncoming, runLengthIncoming, 4);

			MemoryStream dictionaryEmitted = new MemoryStream();
			dictionaryIncoming.Position = 0;
			DictionaryCompression.Pack(dictionaryEmitted, dictionaryIncoming, 9, 11);

			sink.WriteByte(0x58);
			sink.WriteByte(0x30);

			byte[] staging = dictionaryEmitted.ToArray();
			spanValue = staging.Length + 4;

			sink.EmitWord((ushort)spanValue);

			sink.EmitWord((ushort)this.box.Width);
			sink.EmitWord((ushort)this.box.Height);

			sink.Write(staging, 0, staging.Length);
		}

		public void ImportPicture(string documentName, ushort horizontalLocation, ushort verticalLocation, out byte[] swatch)
		{
			VideoGraphicsBitmap? picture = VideoGraphicsBitmap.ViaFile(documentName, out swatch);

			if (picture != null)
			{
				for (int index = 0; index < picture.Box.Height; index++)
				{
					for (int innerIndex = 0; innerIndex < picture.Box.Width; innerIndex++)
					{
						this.AssignDot(horizontalLocation + innerIndex, verticalLocation + index, picture.ObtainDot(innerIndex, index));
					}
				}
			}
		}

		private static int DigitalLevel(byte level)
		{
			return ((63 * level) + 127) / 255;
		}

		private static IEnumerable<(ushort Stamp, byte[] Chunk)> ReadBlocks(Stream flow)
		{
			while (flow.TryPullWord(out ushort stamp) && flow.TryPullWord(out ushort chunkSpan))
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
			{
				throw new Exception($"Palette block type 0x{stamp:x4} malformed");
			}

			int slot = chunk[0];
			int hueTally = chunk[1] - slot + 1;

			for (int index = 0; index < hueTally; index++)
			{
				int place = 2 + (index * 3);

				if (place + 3 > chunk.Length)
				{
					throw new Exception($"Palette block type 0x{stamp:x4} malformed");
				}

				colors.Add(new KeyValuePair<int, Color>(slot + index, HueFromDigitalLevels(chunk[place], chunk[place + 1], chunk[place + 2])));
			}

			byte[] swatch = new byte[4 + chunk.Length];
			swatch[0] = 0x4d;
			swatch[1] = 0x30;
			swatch[2] = (byte)chunk.Length;
			swatch[3] = (byte)(chunk.Length >> 8);
			chunk.CopyTo(swatch, 4);

			return swatch;
		}

		private static int ToFullColor(Color color)
		{
			return (int)(0xff000000 | (uint)(color.ToArgb() & 0x00ffffff));
		}
	}
}
