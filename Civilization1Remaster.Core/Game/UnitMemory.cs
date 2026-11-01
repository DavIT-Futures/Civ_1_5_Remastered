namespace Civilization1Remaster.Core.Game
{
	public sealed class UnitMemory
	{
		public const ushort Origin = 0x81d4;
		public const int ParticipantStride = 0x600;
		public const int TroopStride = 0xc;
		public const int TroopTally = 128;

		public const int Status = 0;

		public const int Horiz = 1;
		public const int Vert = 2;

		public const int Kind = 3;

		public const int LeftoverSteps = 4;

		public const int UnusualSteps = 5;

		public const int TravelHoriz = 6;

		public const int TravelVert = 7;
		public const int TravelHeading = 8;

		public const int ShownToParticipants = 9;

		public const int FollowingInStack = 0xa;

		public const int BaseTown = 0xb;

		private readonly CPU processor;

		private byte[]? records;

		public UnitMemory(CPU processor)
		{
			this.processor = processor;
		}

		private byte[]? Records => this.records ??= this.processor.Storage.OverlayAt(CPU.AsLinearAddress(this.processor.SegData.Value16, Origin));

		public static ushort Offset(int participantIdx, int troopIdx)
		{
			return (ushort)((ParticipantStride * participantIdx) + (TroopStride * troopIdx));
		}

		public static ushort Address(int participantIdx, int troopIdx)
		{
			return (ushort)(Origin + (ParticipantStride * participantIdx) + (TroopStride * troopIdx));
		}

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

		public byte PullAt(int recordOffset, int offset)
		{
			byte[]? records = Records;
			ushort place = (ushort)(recordOffset + offset);

			return records != null && place < records.Length ? records[place] :
				this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(Origin + recordOffset + offset));
		}

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

		public sbyte Following(int participantIdx, int troopIdx)
		{
			return PullSigned(participantIdx, troopIdx, FollowingInStack);
		}

		public void AssignFollowing(int participantIdx, int troopIdx, byte following)
		{
			Assign(participantIdx, troopIdx, FollowingInStack, following);
		}

		public ushort KindField(int participantIdx, int troopIdx, int fieldOffset)
		{
			return UnitKindTable.Field(this.processor, TroopKind(participantIdx, troopIdx), fieldOffset);
		}

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
