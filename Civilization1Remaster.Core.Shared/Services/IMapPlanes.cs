namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IMapPlanes
	{
		public const ushort TerrainOfColour = 0x2ba6;

		int Terrain(int horizontalLocation, int verticalLocation);

		int Continent(int horizontalLocation, int verticalLocation);

		int Improvements(int horizontalLocation, int verticalLocation);

		int LowImprovements(int horizontalLocation, int verticalLocation);

		int HighImprovements(int horizontalLocation, int verticalLocation);

		int KnownImprovements(int horizontalLocation, int verticalLocation);

		void RevealImprovements(int horizontalLocation, int verticalLocation);

		void ClearImprovements(int improvements, int horizontalLocation, int verticalLocation);

		int DirectionHorizontal(int heading);

		int DirectionVertical(int heading);

		int OwnerBits(int horizontalLocation, int verticalLocation);

		void AssignOwnerBits(int horizontalLocation, int verticalLocation, int bits);
	}
}
