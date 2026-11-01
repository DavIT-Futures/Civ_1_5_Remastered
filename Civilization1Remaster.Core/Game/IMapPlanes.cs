namespace Civilization1Remaster.Core.Game
{
	public interface IMapPlanes
	{
		int Terrain(int horizLoc, int vertLoc);

		int Continent(int horizLoc, int vertLoc);

		int Improvements(int horizLoc, int vertLoc);

		int LowImprovements(int horizLoc, int vertLoc);

		int HighImprovements(int horizLoc, int vertLoc);

		int KnownImprovements(int horizLoc, int vertLoc);

		void RevealImprovements(int horizLoc, int vertLoc);

		void ClearImprovements(int improvements, int horizLoc, int vertLoc);

		int DirectionHoriz(int heading);

		int DirectionVert(int heading);

		int OwnerBits(int horizLoc, int vertLoc);

		void AssignOwnerBits(int horizLoc, int vertLoc, int bits);
	}
}
