using System;
using System.IO;

namespace Civilization1Remaster.Core.Engine
{
	public class PictureLoader : GameComponent, IPictureLoader
	{
		public PictureLoader(GameEngine owner)
			: base(owner)
		{
		}

		public void LoadPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, GameText docname, ushort swatchAddr)
		{
			string fileName = docname.Read(this.Processor);
			LoadPictureOrColors(displayIdx, horizLoc, vertLoc, fileName, swatchAddr);
		}

		public void LoadPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, string fileName, ushort swatchAddr)
		{
			string docname = this.Processor.StandardFolder +
				RuntimeLibrary.ObtainDOSFileLabel(fileName.ToUpper());
			using var _ = this.Processor.Journal.Scope($"ImportPictureOrColors(0x{displayIdx:x4}, 0x{horizLoc:x4}, 0x{vertLoc:x4}, " +
				$"'{docname}', 0x{swatchAddr:x4})");

			if (displayIdx >= 0 && (Path.GetExtension(docname).Equals(".pic", StringComparison.InvariantCultureIgnoreCase) ||
				Path.GetExtension(docname).Equals(".map", StringComparison.InvariantCultureIgnoreCase)))
			{
				if (this.Owner.VideoCard.Displays.ContainsKey(displayIdx))
				{
					byte[] swatch;
					ushort beginAddr;
					this.Owner.VideoCard.Displays[displayIdx].ImportPicture(docname, horizLoc, vertLoc, out swatch);

					if (swatch != null)
					{
						switch (swatchAddr)
						{
							case 0:
								beginAddr = (ushort)(TextBuffer.Address + 2);
								for (int idx = 0; idx < swatch.Length; idx++)
								{
									this.Processor.EmitUInt8(this.Processor.SegData.Value16, beginAddr++, swatch[idx]);
								}

								break;
							case 1:
								beginAddr = TextBuffer.Address;
								for (int idx = 0; idx < swatch.Length; idx++)
								{
									this.Processor.EmitUInt8(this.Processor.SegData.Value16, beginAddr++, swatch[idx]);
								}

								break;

							default:
								beginAddr = swatchAddr;
								for (int idx = 0; idx < swatch.Length; idx++)
								{
									this.Processor.EmitUInt8(this.Processor.SegData.Value16, beginAddr++, swatch[idx]);
								}

								break;
						}

						if (swatchAddr == 1 || swatchAddr == TextBuffer.Address)
						{
							this.Owner.VideoCard.AssignHuesFromHueStruct(swatch);
						}
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
								this.Processor.EmitUInt8(this.Processor.SegData.Value16, beginAddr++, swatch[idx]);
							}

							break;
						case 1:
							beginAddr = TextBuffer.Address;
							for (int idx = 0; idx < swatch.Length; idx++)
							{
								this.Processor.EmitUInt8(this.Processor.SegData.Value16, beginAddr++, swatch[idx]);
							}

							break;

						default:
							beginAddr = swatchAddr;
							for (int idx = 0; idx < swatch.Length; idx++)
							{
								this.Processor.EmitUInt8(this.Processor.SegData.Value16, beginAddr++, swatch[idx]);
							}

							break;
					}

					if (swatchAddr == 1 || swatchAddr == TextBuffer.Address)
					{
						this.Owner.VideoCard.AssignHuesFromHueStruct(swatch);
					}
				}
			}
		}

		public ushort LoadIcon(string docname)
		{
			return this.Owner.VideoCard.LoadIcon(RuntimeLibrary.ObtainDOSFileLabel(docname));
		}

		public ushort LoadIcon(ushort docnameAddr)
		{
			return LoadIcon(this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, docnameAddr)));
		}
	}
}
