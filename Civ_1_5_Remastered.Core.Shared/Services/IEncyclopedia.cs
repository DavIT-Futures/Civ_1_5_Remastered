namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IEncyclopedia
	{
		void ShowCivilopedia(ushort part);

		ushort ShowCivilopediaIndex(ushort firstEntry, ushort part);

		void ShowCivilopediaRecord(ushort scienceIndex, ushort part);

		ushort ObtainFirstPrerequisite(ushort scienceIndex);

		void DrawIcon(ushort left, ushort top, ushort breadth, ushort height, ushort centreHorizontal, ushort centreVertical);

		ushort ObtainSecondPrerequisite(ushort scienceIndex);

		ushort ObtainTechnologyCategory(ushort scienceIndex);
	}
}
