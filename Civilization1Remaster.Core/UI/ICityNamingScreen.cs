namespace Civilization1Remaster.Core.UI
{
	public interface ICityNamingScreen
	{
		ushort ShowCityLabelPrompt(short townIdx);

		void ShowFoundCityMessage();

		void ShowCityFoundedMessage();

		void ShowNewCityLocation();

		ushort ShowLabelInputBox(ushort charsAddr);

		ushort PullTextInput(short horizLoc, short vertLoc, ushort breadth);

		void DrawTextInputField(short horizLoc, short vertLoc, ushort breadth);
	}
}
