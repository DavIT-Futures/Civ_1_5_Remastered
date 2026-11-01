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

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				Player participant = this.Owner.World.Participants[participantIdx];
				ushort power = (ushort)(DivideByPowerOfTwo(participant.Gold, 5) + (participant.GrandTownExtent << 3) + participant.FoundScienceTally);

				for (int kind = 0; kind < 0x1c; kind++)
				{
					power += (ushort)(Tables.UnitKindWord((sbyte)kind, UnitKindTable.Price) * participant.LiveTroops[kind]);
				}

				if (((1 << participantIdx) & this.Owner.World.LiveRealms) == 0)
				{
					power = 0;
				}

				powers[participantIdx] = power;

				if (participantIdx != this.Owner.World.PersonParticipantIdx && strongest < (short)power)
				{
					strongest = (short)power;
				}

				if (this.Owner.World.RoundTally < 600)
				{
					this.Processor.SegExtra.Value16 = ReplayHistoryScoresSegment;
					this.Processor.EmitUInt8(ReplayHistoryScoresSegment, GraphPlace(participantIdx), (byte)this.Owner.Helpers.ClampValue((short)power / 8, 0, 255));
				}

				treaties = 0;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if ((participant.Relations[otherIdx] & 3) == 1)
					{
						treaties |= (ushort)(1 << otherIdx);
					}
				}

				if (this.Owner.World.RoundTally < 600)
				{
					this.Owner.World.TrucePlotContent[GraphPlace(participantIdx)] = (byte)treaties;
				}
			}

			ushort best = 0;

			for (short rank = 1; rank < 8; rank++)
			{
				strongest = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short power = (short)powers[participantIdx];

					if (strongest < power)
					{
						best = (ushort)participantIdx;
						strongest = power;
					}
				}

				this.Owner.World.Participants[best].Standing = (short)(8 - rank);
				powers[best] = 0xffff;
			}

			Turn.LateGameNudge = 0x0;

			Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

			if (human.Standing == 7 && human.TownTally > 4 && human.LiveTroops[25] == 0 && this.Owner.World.RoundTally > 200)
			{
				Turn.LateGameNudge = 0x1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					if (participantIdx == this.Owner.World.PersonParticipantIdx || this.Owner.World.Participants[participantIdx].TownTally <= 1)
						continue;

					if ((this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] & 2) == 0)
					{
						this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 1;
					}
					else
					{
						this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 0x100;
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

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				short sciences = this.Owner.World.Participants[participantIdx].FoundScienceTally;

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

			ushort population = this.Owner.Helpers.ComputePlayerPopulation(this.Owner.World.PersonParticipantIdx);

			this.Owner.UnitOps.AttachReplayContent(11, (byte)this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].TownTally,
				TurnEndRules.ReplayPopulationHigh(population), (byte)population);

			byte[] ranking = new byte[4];

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				short standing = this.Owner.World.Participants[participantIdx].Standing;
				int pairIdx = standing / 2;
				if (pairIdx >= 0 && pairIdx < 4)
				{
					byte nibble = (byte)((standing & 1) == 0 ? participantIdx : (participantIdx << 4));
					ranking[pairIdx] |= nibble;
				}
			}

			this.Owner.UnitOps.AttachReplayContent(12, ranking[3], ranking[2], ranking[1], ranking[0]);
		}

		private ushort GraphPlace(short participantIdx)
		{
			return (ushort)(((DivideByPowerOfTwo(this.Owner.World.RoundTally, 2) % 150) << 3) + participantIdx);
		}
	}
}
