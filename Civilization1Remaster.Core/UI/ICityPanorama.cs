namespace Civilization1Remaster.Core.UI
{
	public interface ICityPanorama
	{
		void ShowCityScreen(short townIdx, short improvement);

		void DrawCityPicture(ushort column, ushort row, ushort pictureIdx);

		void DrawCityPictureRegion(ushort column, ushort row, ushort picture, ushort kind);

		void DrawCityResourceBar(ushort column, ushort row, ushort picture);

		void DrawCityScenery(short townIdx, ushort rowHoriz, ushort rowVert, ushort breadth);

		void DrawCityBuildings(ushort improvement, short participantIdx, short townIdx);

		ushort SeizeCityScenery(ushort picture);

		void DrawCitySceneryForPlayer(short participantIdx);

		void RiotAnimation();

		void DrawCitySceneryForHuman();
	}
}
