using System;
using System.IO;

namespace Civ_1_5_Remastered.Core.Engine.Painting
{
	public class PictureLoader : GameComponent, IPictureLoader
	{
		public PictureLoader(IGameEngine owner)
			: base(owner)
		{
		}

		public void LoadPictureOrColors(short displayIndex, ushort horizontalLocation, ushort verticalLocation, GameText documentName, ushort swatchAddress)
		{
			string fileName = documentName.Read(this.Processor);
			LoadPictureOrColors(displayIndex, horizontalLocation, verticalLocation, fileName, swatchAddress);
		}

		public void LoadPictureOrColors(short displayIndex, ushort horizontalLocation, ushort verticalLocation, string fileName, ushort swatchAddress)
		{
			string documentName = this.Processor.StandardFolder +
				RuntimeLibrary.ObtainOperatingSystemFileLabel(fileName.ToUpper());
			documentName = WithBundledPicture(documentName);
			using var _ = this.Processor.Journal.Scope($"ImportPictureOrColors(0x{displayIndex:x4}, 0x{horizontalLocation:x4}, 0x{verticalLocation:x4}, " +
				$"'{documentName}', 0x{swatchAddress:x4})");

			if (displayIndex >= 0 && (Path.GetExtension(documentName).Equals(".pic", StringComparison.InvariantCultureIgnoreCase) ||
				Path.GetExtension(documentName).Equals(".map", StringComparison.InvariantCultureIgnoreCase)))
			{
				if (this.Owner.VideoCard.Displays.ContainsKey(displayIndex))
				{
					byte[] swatch;
					ushort beginAddress;

					// On the enlarged main screens a 320 x 200 picture lands where the centred screen is read from.
					System.Drawing.Point origin = this.Owner.VideoCard.PrepareClassicPicture(displayIndex);
					this.Owner.VideoCard.Displays[displayIndex].ImportPicture(documentName, (ushort)(horizontalLocation + origin.X), (ushort)(verticalLocation + origin.Y), out swatch);

					if (swatch != null)
					{
						switch (swatchAddress)
						{
							case 0:
								beginAddress = (ushort)(TextBuffer.Address + 2);
								for (int index = 0; index < swatch.Length; index++)
								{
									this.Processor.EmitByte(this.Processor.SegmentData.Word, beginAddress++, swatch[index]);
								}

								break;
							case 1:
								beginAddress = TextBuffer.Address;
								for (int index = 0; index < swatch.Length; index++)
								{
									this.Processor.EmitByte(this.Processor.SegmentData.Word, beginAddress++, swatch[index]);
								}

								break;

							default:
								beginAddress = swatchAddress;
								for (int index = 0; index < swatch.Length; index++)
								{
									this.Processor.EmitByte(this.Processor.SegmentData.Word, beginAddress++, swatch[index]);
								}

								break;
						}

						if (swatchAddress == 1 || swatchAddress == TextBuffer.Address)
						{
							this.Owner.VideoCard.AssignHuesFromHueStructure(swatch);
						}
					}
				}
				else
				{
					throw new Exception($"The page {displayIndex} is not allocated");
				}
			}
			else
			{
				byte[] swatch;
				ushort beginAddress;

				VideoGraphicsBitmap.ColorsFromFile(documentName, out swatch);
				if (swatch != null)
				{
					switch (swatchAddress)
					{
						case 0:
							beginAddress = (ushort)(TextBuffer.Address + 2);
							for (int index = 0; index < swatch.Length; index++)
							{
								this.Processor.EmitByte(this.Processor.SegmentData.Word, beginAddress++, swatch[index]);
							}

							break;
						case 1:
							beginAddress = TextBuffer.Address;
							for (int index = 0; index < swatch.Length; index++)
							{
								this.Processor.EmitByte(this.Processor.SegmentData.Word, beginAddress++, swatch[index]);
							}

							break;

						default:
							beginAddress = swatchAddress;
							for (int index = 0; index < swatch.Length; index++)
							{
								this.Processor.EmitByte(this.Processor.SegmentData.Word, beginAddress++, swatch[index]);
							}

							break;
					}

					if (swatchAddress == 1 || swatchAddress == TextBuffer.Address)
					{
						this.Owner.VideoCard.AssignHuesFromHueStructure(swatch);
					}
				}
			}
		}

		public ushort LoadIcon(string documentName)
		{
			return this.Owner.VideoCard.LoadIcon(RuntimeLibrary.ObtainOperatingSystemFileLabel(documentName));
		}

		public ushort LoadIcon(ushort documentNameAddress)
		{
			return LoadIcon(this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress)));
		}

		/// <summary>
		/// A picture the game folder lacks may ship with the application (the added leader portraits KING14 and
		/// KING15 in Assets/Pictures); the game folder always wins.
		/// </summary>
		private static string WithBundledPicture(string documentName)
		{
			if (File.Exists(documentName))
			{
				return documentName;
			}

			string bundled = Path.Combine(AppContext.BaseDirectory, "Assets", "Pictures", Path.GetFileName(documentName));

			return File.Exists(bundled) ? bundled : documentName;
		}
	}
}
