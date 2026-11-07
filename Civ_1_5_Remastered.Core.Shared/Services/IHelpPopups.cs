namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IHelpPopups
	{
		void ShowHelpTopic(ushort charactersAddress);

		void ShowTileInformation(short participantIndex, ushort troopIndex);

		void ShowHelpPopup(GameText topic, bool always = false);

		void ShowHelpPopupForHuman(GameText topic);

		/// <summary>Shows a note in the style of the help popups with text given directly (lines separated by newlines).</summary>
		void ShowNotePopup(string header, string text);
	}
}
