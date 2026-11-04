namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IGameLauncher
	{
		public const ushort VideoDriverOptionAddress = 0x1a22;
		public const ushort SoundDriverOptionAddress = 0x1a30;

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
