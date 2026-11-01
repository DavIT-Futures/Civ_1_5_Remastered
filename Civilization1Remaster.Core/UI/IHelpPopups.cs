namespace Civilization1Remaster.Core.UI
{
	public interface IHelpPopups
	{
		void ShowHelpTopic(ushort charactersAddress);

		void ShowTileInformation(short participantIndex, ushort troopIndex);

		void ShowHelpPopup(GameText topic, bool always = false);

		void ShowHelpPopupForHuman(GameText topic);
	}
}
