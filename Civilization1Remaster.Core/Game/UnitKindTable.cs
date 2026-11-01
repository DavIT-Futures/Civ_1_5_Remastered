namespace Civilization1Remaster.Core.Game
{
	public static class UnitKindTable
	{
		public const ushort Origin = 0x112a;
		public const int Stride = 0x22;

		public const int LandGroup = 0xe;

		public const int ShiftTally = 0x10;

		public const int RoundsAway = 0x12;

		public const int AssaultPower = 0x14;
		public const int GuardPower = 0x16;
		public const int Price = 0x18;

		public const int Sight = 0x1a;

		public const int FerryRoom = 0x1c;
		public const int TroopGroup = 0x1e;

		public const int Technology = 0x20;

		public const int LandUnits = 0;

		public const int AirUnits = 1;

		public const int SeaUnits = 2;

		public const int Submarine = 0x16;

		public const int Carrier = 0x17;

		public const int Diplomat = 0x1a;

		public const int DefenceTroops = 2;

		public const int TransportTroops = 5;

		public static ushort Field(CPU processor, sbyte kind, int fieldOffset)
		{
			return processor.PullGlobalWord((ushort)(Origin + fieldOffset + (Stride * kind)));
		}
	}
}
