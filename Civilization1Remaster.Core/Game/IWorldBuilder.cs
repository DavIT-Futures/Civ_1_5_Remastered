namespace Civilization1Remaster.Core.Game
{
	public interface IWorldBuilder
	{
		void OpeningSequence();

		void BlendTerrainTile(short horizLoc, short vertLoc);

		void ProduceElevation();

		void ProduceCloudLayer();

		void ProduceCloud(short horizLoc, short vertLoc);

		void ProduceTerrain();

		void PlaceSpecialTerrain();

		void LabelLandmasses();

		void ReckonContinentSizes(ushort marker);

		ushort ObtainTerrainTypeFromScreen(short horizLoc, short vertLoc);

		ushort ObtainContinentIDFromScreen(short horizLoc, short vertLoc);

		ushort ShowStoryIntro();

		void EnactGlobalWarming(short worldHeatingTally);
	}
}
