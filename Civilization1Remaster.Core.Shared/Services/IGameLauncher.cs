namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IGameLauncher
	{
		public const ushort VideoDriverOptionAddress = 0x1a22;

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
