namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed class UnitMemory
	{
		public const ushort Origin = 0x81d4;
		public const int ParticipantStride = 0x600;
		public const int TroopStride = 0xc;
		public const int TroopTally = 128;

		public const int Status = 0;

		public const int Horizontal = 1;
		public const int Vertical = 2;

		public const int Kind = 3;

		public const int LeftoverSteps = 4;

		public const int UnusualSteps = 5;

		public const int TravelHorizontal = 6;

		public const int TravelVertical = 7;
		public const int TravelHeading = 8;

		public const int ShownToParticipants = 9;

		public const int FollowingInStack = 0xa;

		public const int BaseTown = 0xb;

		private readonly CentralProcessor processor;

		private byte[]? records;

		public UnitMemory(CentralProcessor processor)
		{
			this.processor = processor;
		}

		private byte[]? Records => this.records ??= this.processor.Storage.OverlayAt(CentralProcessor.AsLinearAddress(this.processor.SegmentData.Word, Origin));

		public static ushort Offset(int participantIndex, int troopIndex)
		{
			return (ushort)((ParticipantStride * participantIndex) + (TroopStride * troopIndex));
		}

		public static ushort Address(int participantIndex, int troopIndex)
		{
			return (ushort)(Origin + (ParticipantStride * participantIndex) + (TroopStride * troopIndex));
		}

		public sbyte PullSigned(int participantIndex, int troopIndex, int offset)
		{
			return (sbyte)Pull(participantIndex, troopIndex, offset);
		}

		public byte Pull(int participantIndex, int troopIndex, int offset)
		{
			return PullAt(Offset(participantIndex, troopIndex), offset);
		}

		public void Assign(int participantIndex, int troopIndex, int offset, byte amount)
		{
			AssignAt(Offset(participantIndex, troopIndex), offset, amount);
		}

		public byte PullAt(int recordOffset, int offset)
		{
			byte[]? records = Records;
			ushort place = (ushort)(recordOffset + offset);

			return records != null && place < records.Length ? records[place] : this.processor.PullByte(this.processor.SegmentData.Word, (ushort)(Origin + recordOffset + offset));
		}

		public void AssignAt(int recordOffset, int offset, byte amount)
		{
			byte[]? records = Records;
			ushort place = (ushort)(recordOffset + offset);

			if (records != null && place < records.Length)
			{
				records[place] = amount;
			}
			else
			{
				this.processor.EmitByte(this.processor.SegmentData.Word, (ushort)(Origin + recordOffset + offset), amount);
			}
		}

		public sbyte TroopKind(int participantIndex, int troopIndex)
		{
			return PullSigned(participantIndex, troopIndex, Kind);
		}

		public sbyte Following(int participantIndex, int troopIndex)
		{
			return PullSigned(participantIndex, troopIndex, FollowingInStack);
		}

		public void AssignFollowing(int participantIndex, int troopIndex, byte following)
		{
			Assign(participantIndex, troopIndex, FollowingInStack, following);
		}

		public ushort KindField(int participantIndex, int troopIndex, int fieldOffset)
		{
			return UnitKindTable.Field(this.processor, TroopKind(participantIndex, troopIndex), fieldOffset);
		}

		public bool Exists(int participantIndex, int troopIndex)
		{
			return PullSigned(participantIndex, troopIndex, Kind) != -1;
		}

		public bool StandsAt(int participantIndex, int troopIndex, int horizontalLocation, int verticalLocation)
		{
			return PullSigned(participantIndex, troopIndex, Horizontal) == horizontalLocation && PullSigned(participantIndex, troopIndex, Vertical) == verticalLocation;
		}
	}
}
