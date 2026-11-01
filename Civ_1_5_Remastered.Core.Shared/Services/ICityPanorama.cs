namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface ICityPanorama
	{
		void ShowCityScreen(short townIndex, short improvement);

		void DrawCityPicture(ushort column, ushort row, ushort pictureIndex);

		void DrawCityPictureRegion(ushort column, ushort row, ushort picture, ushort kind);

		void DrawCityResourceBar(ushort column, ushort row, ushort picture);

		void DrawCityScenery(short townIndex, ushort rowHorizontal, ushort rowVertical, ushort breadth);

		void DrawCityBuildings(ushort improvement, short participantIndex, short townIndex);

		ushort SeizeCityScenery(ushort picture);

		void DrawCitySceneryForPlayer(short participantIndex);

		void RiotAnimation();

		void DrawCitySceneryForHuman();
	}
}
