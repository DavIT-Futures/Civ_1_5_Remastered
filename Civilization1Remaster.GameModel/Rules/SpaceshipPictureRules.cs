namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>The pictures of the spaceship parts, cut out by the spaceship screen into the words 0x682a + 2 * part</summary>
	public static class SpaceshipPictureRules
	{
		/// <summary>The first word of the pictures of the parts</summary>
		public const ushort Pictures = 0x682a;

		/// <summary>The address of the word with the picture of the part</summary>
		public static ushort PictureAddress(int part) => (ushort)(Pictures + 2 * part);
	}
}
