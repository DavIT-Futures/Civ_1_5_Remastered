using System;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>The operations of PictureLoader that the other parts of the game use</summary>
	public interface IPictureLoader
	{
		/// <summary>
		/// Loads and .pic or .map Image from given filename with optional palette data
		/// </summary>
		/// <param name="screenID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="filenamePtr"></param>
		/// <param name="palettePtr"></param>
		/// <exception cref="Exception"></exception>
		void ImportPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, GameText docname, ushort swatchAddr);

		void ImportPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, string fileName, ushort swatchAddr);

		/// <summary>
		/// Loads an Bitmap (Icon) from given image Filename
		/// </summary>
		/// <param name="filenamePtr"></param>
		ushort ImportIcon(string docname);

		ushort ImportIcon(ushort docnameAddr);
	}
}
