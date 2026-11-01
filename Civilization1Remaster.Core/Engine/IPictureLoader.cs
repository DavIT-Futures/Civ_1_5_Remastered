using System;

namespace Civilization1Remaster.Core.Engine
{
	public interface IPictureLoader
	{
		void LoadPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, GameText docname, ushort swatchAddr);

		void LoadPictureOrColors(short displayIdx, ushort horizLoc, ushort vertLoc, string fileName, ushort swatchAddr);

		ushort LoadIcon(string docname);

		ushort LoadIcon(ushort docnameAddr);
	}
}
