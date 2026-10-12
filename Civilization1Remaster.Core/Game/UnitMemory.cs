using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// The live units of all players: 128 records of 12 bytes for each of the 8 players, in the emulated data segment.
	/// GameState.Participants[].Troops is only the copy made when a game is loaded, the game code reads and writes these.
	/// </summary>
	public sealed class UnitMemory
	{
		public const ushort Origin = 0x81d4;
		public const int ParticipantStride = 0x600;
		public const int TroopStride = 0xc;
		public const int TroopTally = 128;

		// Offsets in a record
		public const int Status = 0;
		public const int Horiz = 1;
		public const int Vert = 2;
		/// <summary>Unit type, -1 when the record is empty</summary>
		public const int Kind = 3;
		/// <summary>Moves left in this turn, in thirds</summary>
		public const int LeftoverSteps = 4;
		/// <summary>Work turns of settlers, fuel (turns left in the air) of planes</summary>
		public const int UnusualSteps = 5;
		/// <summary>Target of a "go to" order, -1 when none</summary>
		public const int TravelHoriz = 6;
		public const int TravelVert = 7;
		public const int TravelHeading = 8;
		/// <summary>Bit mask of the players that see the unit</summary>
		public const int ShownToParticipants = 9;
		/// <summary>Next unit in the same stack, -1 when none</summary>
		public const int FollowingInStack = 0xa;
		public const int BaseTown = 0xb;

		private readonly CPU processor;

		public UnitMemory(CPU processor)
		{
			this.processor = processor;
		}

		public static ushort Address(int participantIdx, int troopIdx)
		{
			return (ushort)(Origin + (ParticipantStride * participantIdx) + (TroopStride * troopIdx));
		}

		public sbyte PullSigned(int participantIdx, int troopIdx, int offset)
		{
			return this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(Address(participantIdx, troopIdx) + offset));
		}

		public byte Pull(int participantIdx, int troopIdx, int offset)
		{
			return this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(Address(participantIdx, troopIdx) + offset));
		}

		public void Assign(int participantIdx, int troopIdx, int offset, byte amount)
		{
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(Address(participantIdx, troopIdx) + offset), amount);
		}

		public sbyte TroopKind(int participantIdx, int troopIdx)
		{
			return PullSigned(participantIdx, troopIdx, Kind);
		}

		/// <summary>Next unit in the same stack (the stack is a ring), -1 when the unit stands alone</summary>
		public sbyte Following(int participantIdx, int troopIdx)
		{
			return PullSigned(participantIdx, troopIdx, FollowingInStack);
		}

		public void AssignFollowing(int participantIdx, int troopIdx, byte following)
		{
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(Address(participantIdx, troopIdx) + FollowingInStack), following);
		}

		/// <summary>
		/// A field of the unit type of the unit, from the unit type table of the data segment (it is saved with the game)
		/// </summary>
		public ushort KindField(int participantIdx, int troopIdx, int fieldOffset)
		{
			return UnitKindTable.Field(this.processor, TroopKind(participantIdx, troopIdx), fieldOffset);
		}

		/// <summary>True when the record holds a unit</summary>
		public bool Exists(int participantIdx, int troopIdx)
		{
			return PullSigned(participantIdx, troopIdx, Kind) != -1;
		}

		public bool StandsAt(int participantIdx, int troopIdx, int horizLoc, int vertLoc)
		{
			return PullSigned(participantIdx, troopIdx, Horiz) == horizLoc && PullSigned(participantIdx, troopIdx, Vert) == vertLoc;
		}
	}

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
