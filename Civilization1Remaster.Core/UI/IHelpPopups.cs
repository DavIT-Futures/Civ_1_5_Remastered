namespace Civilization1Remaster.Core.UI
{
	public interface IHelpPopups
	{
		void ShowHelpTopic(ushort charsAddr);

		void ShowTileInfo(short participantIdx, ushort troopIdx);

		void ShowHelpPopup(GameText topic, bool always = false);

		void ShowHelpPopupForHuman(GameText topic);
	}
}
