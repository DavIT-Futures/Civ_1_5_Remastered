namespace Civilization1Remaster.Core.Engine
{
	public interface IGameLauncher
	{
		void RunGame();

		void LoadGameDuringPlay(ushort arrowTier);

		void ReadMouseState();

		void ShowMouseCursor();

		void HideMouseCursor();

		void ShowMouseCursorFully();

		void HideMouseCursorToSavedLevel();

		ushort AskForDisk(ushort diskIndex, ushort marker);

		void ShowTitleAndMainMenu();

		void ChooseNewWorldOptions();
	}
}
