using System;

namespace Civilization1Remaster.Core.Game
{
	public partial class WorldEvents
	{
		public void RefreshPeaceGraphContent()
		{
			using var _ = this.Processor.Journal.Scope("RefreshPeaceGraphContent()");

			ushort[] powers = new ushort[8];
			short strongest = 0;
			ushort treaties = 0;

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				Player participant = this.Owner.World.Participants[participantIndex];
				ushort power = (ushort)(DivideByPowerOfTwo(participant.Gold, 5) + (participant.GrandTownExtent << 3) + participant.FoundScienceTally);

				for (int kind = 0; kind < 0x1c; kind++)
				{
					power += (ushort)(Tables.UnitKindWord((sbyte)kind, UnitKindTable.Price) * participant.LiveTroops[kind]);
				}

				if (((1 << participantIndex) & this.Owner.World.LiveRealms) == 0)
				{
					power = 0;
				}

				powers[participantIndex] = power;

				if (participantIndex != this.Owner.World.PersonParticipantIndex && strongest < (short)power)
				{
					strongest = (short)power;
				}

				if (this.Owner.World.RoundTally < 600)
				{
					this.Processor.SegmentExtra.Word = ReplayHistoryScoresSegment;
					this.Processor.EmitByte(ReplayHistoryScoresSegment, GraphPlace(participantIndex), (byte)this.Owner.Helpers.ClampValue((short)power / 8, 0, 255));
				}

				treaties = 0;

				for (int otherIndex = 0; otherIndex < 8; otherIndex++)
				{
					if ((participant.Relations[otherIndex] & 3) == 1)
					{
						treaties |= (ushort)(1 << otherIndex);
					}
				}

				if (this.Owner.World.RoundTally < 600)
				{
					this.Owner.World.TrucePlotContent[GraphPlace(participantIndex)] = (byte)treaties;
				}
			}

			ushort best = 0;

			for (short rank = 1; rank < 8; rank++)
			{
				strongest = -1;

				for (short participantIndex = 1; participantIndex < 8; participantIndex++)
				{
					short power = (short)powers[participantIndex];

					if (strongest < power)
					{
						best = (ushort)participantIndex;
						strongest = power;
					}
				}

				this.Owner.World.Participants[best].Standing = (short)(8 - rank);
				powers[best] = 0xffff;
			}

			Turn.LateGameNudge = 0x0;

			Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];

			if (human.Standing == 7 && human.TownTally > 4 && human.LiveTroops[25] == 0 && this.Owner.World.RoundTally > 200)
			{
				Turn.LateGameNudge = 0x1;

				for (short participantIndex = 1; participantIndex < 8; participantIndex++)
				{
					if (participantIndex == this.Owner.World.PersonParticipantIndex || this.Owner.World.Participants[participantIndex].TownTally <= 1)
					{
						continue;
					}

					if ((this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] & 2) == 0)
					{
						this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] |= 1;
					}
					else
					{
						this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] |= 0x100;
					}
				}
			}
		}

		private static short DivideByPowerOfTwo(short amount, int shift)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> shift);

			return amount < 0 ? (short)-quotient : quotient;
		}

		private void NoteStatusInReplay()
		{
			this.Owner.World.HighestScienceTally = 0;

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				short sciences = this.Owner.World.Participants[participantIndex].FoundScienceTally;

				if (this.Owner.World.HighestScienceTally < sciences)
				{
					this.Owner.World.HighestScienceTally = sciences;
				}
			}

			Turn.TechnologyLevel = 0x0;

			if (this.Owner.World.Season >= 0)
			{
				Turn.TechnologyLevel = (ushort)this.Owner.Helpers.ClampValue(
					this.Owner.World.HighestScienceTally - (this.Owner.World.RoundTally / 9), 0, 6);
			}

			ushort population = this.Owner.Helpers.ComputePlayerPopulation(this.Owner.World.PersonParticipantIndex);

			this.Owner.UnitOperations.AttachReplayContent(11, (byte)this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].TownTally,
				TurnEndRules.ReplayPopulationHigh(population), (byte)population);

			byte[] ranking = new byte[4];

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if ((this.Owner.World.LiveRealms & (1 << participantIndex)) == 0)
				{
					continue;
				}

				short standing = this.Owner.World.Participants[participantIndex].Standing;
				int pairIndex = standing / 2;
				if (pairIndex >= 0 && pairIndex < 4)
				{
					byte nibble = (byte)((standing & 1) == 0 ? participantIndex : (participantIndex << 4));
					ranking[pairIndex] |= nibble;
				}
			}

			this.Owner.UnitOperations.AttachReplayContent(12, ranking[3], ranking[2], ranking[1], ranking[0]);
		}

		private ushort GraphPlace(short participantIndex)
		{
			return (ushort)(((DivideByPowerOfTwo(this.Owner.World.RoundTally, 2) % 150) << 3) + participantIndex);
		}
	}
}
