namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IPictureLoader
	{
		void LoadPictureOrColors(short displayIndex, ushort horizontalLocation, ushort verticalLocation, GameText documentName, ushort swatchAddress);

		void LoadPictureOrColors(short displayIndex, ushort horizontalLocation, ushort verticalLocation, string fileName, ushort swatchAddress);

		ushort LoadIcon(string documentName);

		ushort LoadIcon(ushort documentNameAddress);
	}
}
