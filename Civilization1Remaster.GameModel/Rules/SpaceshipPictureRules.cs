namespace Civilization1Remaster.Game.Rules
{
	/// <summary>The pictures of the spaceship parts, cut out by the spaceship screen into the words 0x682a + 2 * part</summary>
	public static class SpaceshipPictureRules
	{
		/// <summary>The first word of the pictures of the parts</summary>
		public const ushort Pictures = 0x682a;

		/// <summary>
		/// The address of the word with the picture of the part. classic: as the translated code computed it, part &lt;&lt; 1 +
		/// 0x682a read as part &lt;&lt; 0x682b, a shift by 11 (the count is masked to 5 bits), so a word far from the pictures
		/// and usually no picture at all; kept for the tests (TurnEvents.ClassicSpaceshipPictures).
		/// </summary>
		public static ushort PictureAddress(int part, bool classic) =>
			classic ? (ushort)((ushort)part << 0xb) : (ushort)(Pictures + 2 * part);
	}
}
