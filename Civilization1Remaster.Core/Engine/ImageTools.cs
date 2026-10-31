using System;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Image loading functions - RLE and LZW compression was used
	/// </summary>
	public class ImageTools : GameModule
	{
		public ImageTools(GameEngine owner)
			: base(owner)
		{
		}

		#region New Image loading functions
		/// <summary>
		/// Loads and .pic or .map Image from given filename with optional palette data
		/// </summary>
		/// <param name="screenID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="filenamePtr"></param>
		/// <param name="palettePtr"></param>
		/// <exception cref="Exception"></exception>
		public void ImportPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, ushort docnameAddr, ushort swatchAddr)
		{
			string fileName = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr));
			ImportPictureOrColors(displayIdx, horizLoc, vertLoc, fileName, swatchAddr);
		}

		public void ImportPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, string fileName, ushort swatchAddr)
		{
			string docname = this.processor.StandardFolder +
				CRuntime.ObtainDOSFileLabel(fileName.ToUpper());
			this.processor.Journal.OpenScope($"ImportPictureOrColors(0x{displayIdx:x4}, 0x{horizLoc:x4}, 0x{vertLoc:x4}, " +
				$"'{docname}', 0x{swatchAddr:x4})");

			if (displayIdx >= 0 && (Path.GetExtension(docname).Equals(".pic", StringComparison.InvariantCultureIgnoreCase) ||
				Path.GetExtension(docname).Equals(".map", StringComparison.InvariantCultureIgnoreCase)))
			{
				if (this.owner.VideoCard.Displays.IncludesKey(displayIdx))
				{
					byte[] swatch;
					ushort beginAddr;
					this.owner.VideoCard.Displays.ObtainValueByKey(displayIdx).ImportPicture(docname, horizLoc, vertLoc, out swatch);

					if (swatch != null)
					{
						switch (swatchAddr)
						{
							case 0:
								beginAddr = (ushort)(TextBuffer.Address + 2);
								for (int idx = 0; idx < swatch.Length; idx++)
								{
									this.processor.EmitUInt8(this.processor.SegData.Value16, beginAddr++, swatch[idx]);
								}
								break;
							case 1:
								beginAddr = TextBuffer.Address;
								for (int idx = 0; idx < swatch.Length; idx++)
								{
									this.processor.EmitUInt8(this.processor.SegData.Value16, beginAddr++, swatch[idx]);
								}
								break;

							default:
								beginAddr = swatchAddr;
								for (int idx = 0; idx < swatch.Length; idx++)
								{
									this.processor.EmitUInt8(this.processor.SegData.Value16, beginAddr++, swatch[idx]);
								}
								break;
						}
						if (swatchAddr == 1 || swatchAddr == TextBuffer.Address)
							this.owner.VideoCard.AssignHuesFromHueStruct(swatch);
					}
				}
				else
				{
					throw new Exception($"The page {displayIdx} is not allocated");
				}
			}
			else
			{
				byte[] swatch;
				ushort beginAddr;

				VGABitmap.ColorsFromFile(docname, out swatch);
				if (swatch != null)
				{
					switch (swatchAddr)
					{
						case 0:
							beginAddr = (ushort)(TextBuffer.Address + 2);
							for (int idx = 0; idx < swatch.Length; idx++)
							{
								this.processor.EmitUInt8(this.processor.SegData.Value16, beginAddr++, swatch[idx]);
							}
							break;
						case 1:
							beginAddr = TextBuffer.Address;
							for (int idx = 0; idx < swatch.Length; idx++)
							{
								this.processor.EmitUInt8(this.processor.SegData.Value16, beginAddr++, swatch[idx]);
							}
							break;

						default:
							beginAddr = swatchAddr;
							for (int idx = 0; idx < swatch.Length; idx++)
							{
								this.processor.EmitUInt8(this.processor.SegData.Value16, beginAddr++, swatch[idx]);
							}
							break;
					}
					if (swatchAddr == 1 || swatchAddr == TextBuffer.Address)
						this.owner.VideoCard.AssignHuesFromHueStruct(swatch);
				}
			}

			this.processor.Journal.CloseScope("ImportPictureOrColors");
		}

		/// <summary>
		/// Loads an Bitmap (Icon) from given image Filename
		/// </summary>
		/// <param name="filenamePtr"></param>
		public ushort ImportIcon(string docname)
		{
			return this.owner.VideoCard.ImportIcon(CRuntime.ObtainDOSFileLabel(docname));
		}

		public ushort ImportIcon(ushort docnameAddr)
		{
			return ImportIcon(this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr)));
		}
		#endregion
	}
}
