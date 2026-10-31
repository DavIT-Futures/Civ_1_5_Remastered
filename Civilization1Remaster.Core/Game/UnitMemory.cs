using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// The live units of all players: 128 records of 12 bytes for each of the 8 players at 0x81d4 of the data segment.
	/// GameState.Participants[].Troops is only the copy made when a game is loaded, the game code reads and writes these.
	/// B6.2: the records live in a C# array (GameEngine.PrepareMachine attaches it to the memory), read and written here
	/// directly; the code that still uses the addresses reaches the same array through the memory.
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

		/// <summary>The place of the unit in the table of units, from its start (Origin)</summary>
		public static ushort Offset(int participantIdx, int troopIdx)
		{
			return (ushort)((ParticipantStride * participantIdx) + (TroopStride * troopIdx));
		}

		public static ushort Address(int participantIdx, int troopIdx)
		{
			return (ushort)(Origin + (ParticipantStride * participantIdx) + (TroopStride * troopIdx));
		}

		private byte[]? records;

		/// <summary>The records in C#, found once (null before the machine is prepared: then through the memory)</summary>
		private byte[]? Records => this.records ??= this.processor.Storage.OverlayAt(CPU.AsLinearAddress(this.processor.SegData.Value16, Origin));

		public sbyte PullSigned(int participantIdx, int troopIdx, int offset)
		{
			return (sbyte)Pull(participantIdx, troopIdx, offset);
		}

		public byte Pull(int participantIdx, int troopIdx, int offset)
		{
			return PullAt(Offset(participantIdx, troopIdx), offset);
		}

		public void Assign(int participantIdx, int troopIdx, int offset, byte amount)
		{
			AssignAt(Offset(participantIdx, troopIdx), offset, amount);
		}

		/// <summary>
		/// A byte of a unit through the place of its record computed before (0x600 * player + 0xc * unit, the raw address
		/// of the original less Origin). A place past the table (the original reads the memory after it) goes through the
		/// memory.
		/// </summary>
		public byte PullAt(int recordOffset, int offset)
		{
			byte[]? records = Records;
			ushort place = (ushort)(recordOffset + offset);

			return records != null && place < records.Length ? records[place] :
				this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(Origin + recordOffset + offset));
		}

		/// <summary>Writes a byte of a unit through the place of its record computed before (see PullAt)</summary>
		public void AssignAt(int recordOffset, int offset, byte amount)
		{
			byte[]? records = Records;
			ushort place = (ushort)(recordOffset + offset);

			if (records != null && place < records.Length)
				records[place] = amount;
			else
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(Origin + recordOffset + offset), amount);
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
			Assign(participantIdx, troopIdx, FollowingInStack, following);
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
}
