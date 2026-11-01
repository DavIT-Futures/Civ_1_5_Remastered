namespace Civilization1Remaster.Core.Game
{
	public interface IWorldBuilder
	{
		void OpeningSequence();

		void BlendTerrainTile(short horizontalLocation, short verticalLocation);

		void ProduceElevation();

		void ProduceCloudLayer();

		void ProduceCloud(short horizontalLocation, short verticalLocation);

		void ProduceTerrain();

		void PlaceSpecialTerrain();

		void LabelLandmasses();

		void ReckonContinentSizes(ushort marker);

		ushort ObtainTerrainTypeFromScreen(short horizontalLocation, short verticalLocation);

		ushort ObtainContinentIdentifierFromScreen(short horizontalLocation, short verticalLocation);

		ushort ShowStoryIntro();

		void EnactGlobalWarming(short worldHeatingTally);
	}
}
