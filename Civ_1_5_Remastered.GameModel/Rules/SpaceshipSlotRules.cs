namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class SpaceshipSlotRules
	{
		public const ushort NoSlot = 0xffff;

		public const ushort NoStructureSlot = 0x63;

		public const ushort PartRecords = 0x4bd0;

		public static int CompleteBit(int kind, int partGroup) => kind - (3 * partGroup);

		public static ushort PartName(int kind) => (ushort)((kind * 0x1a) + PartRecords);

		public static ushort StructureSlot(ushort chosen, ushort other)
		{
			if (chosen != NoStructureSlot)
				return chosen;

			return other != NoStructureSlot ? other : NoSlot;
		}
	}
}
