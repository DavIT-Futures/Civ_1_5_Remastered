namespace Civilization1Remaster.GameModel.Rules
{
	public static class SpaceshipPictureRules
	{
		public const ushort Pictures = 0x682a;

		public static ushort PictureAddress(int part) => (ushort)(Pictures + 2 * part);
	}
}
