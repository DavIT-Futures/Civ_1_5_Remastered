using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// The unit types (records of 0x22 bytes, the layout of GameState.UnitDefinition) in the data segment
	/// </summary>
	public static class UnitKindTable
	{
		public const ushort Origin = 0x112a;
		public const int Stride = 0x22;

		// Word fields of a record, after the 12 bytes of the name
		public const int LandGroup = 0xe;
		/// <summary>Moves per turn</summary>
		public const int ShiftTally = 0x10;
		/// <summary>Turns a plane can stay in the air</summary>
		public const int RoundsAway = 0x12;
		public const int AssaultPower = 0x14;
		public const int GuardPower = 0x16;
		public const int Price = 0x18;
		/// <summary>How far the unit sees: 1 the tiles next to it, 2 also the tiles 2 tiles far</summary>
		public const int Sight = 0x1a;
		public const int FerryRoom = 0x1c;
		public const int TroopGroup = 0x1e;
		/// <summary>The technology that allows the unit</summary>
		public const int Technology = 0x20;

		/// <summary>LandGroup of land units</summary>
		public const int LandUnits = 0;

		/// <summary>LandGroup of planes</summary>
		public const int AirUnits = 1;

		/// <summary>LandGroup of ships</summary>
		public const int SeaUnits = 2;

		/// <summary>Unit type of the Submarine</summary>
		public const int Submarine = 0x16;

		/// <summary>Unit type of the Carrier</summary>
		public const int Carrier = 0x17;

		/// <summary>Unit type of the Diplomat</summary>
		public const int Diplomat = 0x1a;

		/// <summary>TroopGroup of defensive units</summary>
		public const int DefenceTroops = 2;

		/// <summary>TroopGroup of transports (ships carrying units)</summary>
		public const int TransportTroops = 5;

		/// <summary>
		/// A word field of a unit type. Like the original (a signed byte multiplication), kind -1 reads the record before the table.
		/// </summary>
		public static ushort Field(CPU processor, sbyte kind, int fieldOffset)
		{
			return processor.PullGlobalWord((ushort)(Origin + fieldOffset + (Stride * kind)));
		}
	}
}
