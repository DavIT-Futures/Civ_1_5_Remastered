namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// The places of the parts of a spaceship (table 0x375a: column, row, kind of place, ended by 0xff): structures 0 - 2,
	/// components 3 - 4 (arg2 1), modules 6 - 8 (arg2 2). A kind without a free place is marked complete (0x687a + 2 * arg2,
	/// one bit per line of the dialog, so the line is disabled) and the player is told "No further ... required."
	/// </summary>
	public static class SpaceshipSlotRules
	{
		/// <summary>No free place</summary>
		public const ushort NoSlot = 0xffff;

		/// <summary>The "no free place" of a structure side in the translated code: a place past the table</summary>
		public const ushort NoStructureSlot = 0x63;

		/// <summary>The first record of the spaceship parts (0x1a bytes each, the name first)</summary>
		public const ushort PartRecords = 0x4bd0;

		/// <summary>
		/// The bit of the full kind in its complete mask: its line in the dialog (kind - 3 for components, kind - 6 for
		/// modules)
		/// </summary>
		public static int CompleteBit(int kind, int partGroup) => kind - (3 * partGroup);

		/// <summary>The address of the name of the part kind</summary>
		public static ushort PartName(int kind, int partGroup) => (ushort)((kind * 0x1a) + PartRecords);

		/// <summary>
		/// The place of a new structure: the free place chosen (the side closer to the last built part), else the free place
		/// of the other side, else none
		/// </summary>
		public static ushort StructureSlot(ushort chosen, ushort other)
		{
			if (chosen != NoStructureSlot)
				return chosen;

			return other != NoStructureSlot ? other : NoSlot;
		}
	}
}
