namespace Civilization1Remaster.Game.Rules
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
		/// modules). classic: the translated code shifted by the kind itself (3 - 8), bits no dialog line has.
		/// </summary>
		public static int CompleteBit(int kind, int partGroup, bool classic) => classic ? kind : kind - (3 * partGroup);

		/// <summary>
		/// The address of the name of the part kind. classic: the translated code added 3 * group to the kind that already
		/// had it, the name of another part (or past the table).
		/// </summary>
		public static ushort PartName(int kind, int partGroup, bool classic) =>
			(ushort)((((classic ? 3 * partGroup : 0) + kind) * 0x1a) + PartRecords);

		/// <summary>
		/// The place of a new structure: the free place chosen (the side closer to the last built part), else the free place
		/// of the other side, else none. classic: the chosen one even when that side is full (0x63, past the table).
		/// </summary>
		public static ushort StructureSlot(ushort chosen, ushort other, bool classic)
		{
			if (classic || chosen != NoStructureSlot)
				return chosen;

			return other != NoStructureSlot ? other : NoSlot;
		}
	}
}
