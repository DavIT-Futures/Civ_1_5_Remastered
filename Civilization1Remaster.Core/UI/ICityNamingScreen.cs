namespace Civilization1Remaster.Core.UI
{
	public interface ICityNamingScreen
	{
		ushort ShowCityLabelPrompt(short townIndex);

		void ShowFoundCityMessage();

		void ShowCityFoundedMessage();

		void ShowNewCityLocation();

		ushort ShowLabelInputBox(ushort charactersAddress);

		ushort PullTextInput(short horizontalLocation, short verticalLocation, ushort breadth);

		void DrawTextInputField(short horizontalLocation, short verticalLocation, ushort breadth);
	}
}
