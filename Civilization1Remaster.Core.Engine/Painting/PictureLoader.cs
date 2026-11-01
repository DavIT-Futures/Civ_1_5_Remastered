using System;
using System.IO;

namespace Civilization1Remaster.Core.Engine.Painting
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
			using var _ = this.Processor.Journal.Scope($"ImportPictureOrColors(0x{displayIndex:x4}, 0x{horizontalLocation:x4}, 0x{verticalLocation:x4}, " +
				$"'{documentName}', 0x{swatchAddress:x4})");

			if (displayIndex >= 0 && (Path.GetExtension(documentName).Equals(".pic", StringComparison.InvariantCultureIgnoreCase) ||
				Path.GetExtension(documentName).Equals(".map", StringComparison.InvariantCultureIgnoreCase)))
			{
				if (this.Owner.VideoCard.Displays.ContainsKey(displayIndex))
				{
					byte[] swatch;
					ushort beginAddress;
					this.Owner.VideoCard.Displays[displayIndex].ImportPicture(documentName, horizontalLocation, verticalLocation, out swatch);

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
	}
}
