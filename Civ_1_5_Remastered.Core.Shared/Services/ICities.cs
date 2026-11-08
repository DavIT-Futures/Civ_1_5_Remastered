namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface ICities
	{
		ushort ObtainCityTileIndex(short horizontalLocation, short verticalLocation);

		void DrawCityTile(short townIndex, ushort townTile, short marker);

		ushort MayCityWorkTile(int horizontalLocation, int verticalLocation, ushort resource);

		ushort PlayerEnjoysWonderEffect(short participantIndex, short miracleIdentifier);

		ushort ObtainWonderCityIdentifier(short miracleIdentifier);

		void AttachSmog(int horizontalLocation, int verticalLocation);

		void AssignCitizenType(ushort citizen, ushort specialist);

		ushort ObtainCitizenType(ushort citizen);

		ushort TallySpecialists(ushort specialist);

		void ReshuffleCitizens(short townIndex, ushort workerTally);

		ushort DrawCitizenIcons(short townIndex, short horizontalLocation, short verticalLocation, ushort specialists, ushort breadth);

		void DrawResourceIcon(short horizontalLocationOrigin, short horizontalLocationTo, short verticalLocationTo);

		void DrawFrame(int horizontalLocation, int verticalLocation, int firstHorizontalSpot, int firstVerticalSpot);

		void FillBoxWithMotif(int horizontalLocation, int verticalLocation, int breadth, int altitude);

		void DrawLabelBox(int horizontalLocation, int verticalLocation, int firstHorizontalSpot, int firstVerticalSpot, GameText label, ushort style);

		void DrawMiniMap();

		void DrawMiniMapDot(int horizontalLocation, int verticalLocation, ushort style);

		ushort HandleCity(short townIndex, short marker);

		/// <summary>Turns the least productive workers of a city into entertainers until it is no longer in disorder.</summary>
		/// <returns><c>true</c> when the city is calm.</returns>
		bool CalmCity(short townIndex);
	}
}
