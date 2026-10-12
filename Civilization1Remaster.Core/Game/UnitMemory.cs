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
		/// <summary>Next unit in the same stack, -1 when none</summary>
		public const int FollowingInStack = 0xa;

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
