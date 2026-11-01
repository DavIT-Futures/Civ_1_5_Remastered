namespace Civilization1Remaster.Core.UI
{
	public interface IEncyclopedia
	{
		void ShowCivilopedia(ushort part);

		ushort ShowCivilopediaIndex(ushort firstEntry, ushort part);

		void ShowCivilopediaRecord(ushort scienceIdx, ushort part);

		ushort ObtainFirstPrerequisite(ushort scienceIdx);

		void DrawIcon(ushort left, ushort top, ushort breadth, ushort height, ushort centreHoriz, ushort centreVert);

		ushort ObtainSecondPrerequisite(ushort scienceIdx);

		ushort ObtainTechnologyCategory(ushort scienceIdx);
	}
}
