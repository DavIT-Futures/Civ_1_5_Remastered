using System;
using System.IO;

namespace Civilization1Remaster.GameModel.Rules
{
	public class UnitHealth
	{
		public const int ParticipantTally = 8;
		public const int TroopTally = 128;

		private const byte DocumentEdition = 1;
		private const byte NoneTroop = 0xff;

		private static readonly byte[] DocumentSignature = { (byte)'O', (byte)'C', (byte)'H', (byte)'P' };

		private readonly byte[,] troopKinds = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] missingStrikeMarks = new byte[ParticipantTally, TroopTally];
		private readonly bool[,] ace = new bool[ParticipantTally, TroopTally];

		public UnitHealth()
		{
			WipeAll();
		}

		public void WipeAll()
		{
			for (int idx = 0; idx < ParticipantTally; idx++)
			{
				for (int jdx = 0; jdx < TroopTally; jdx++)
				{
					Wipe(idx, jdx);
				}
			}
		}

		public void Wipe(int participantIdx, int troopIdx)
		{
			if (!VerifyValid(participantIdx, troopIdx))
				return;

			this.troopKinds[participantIdx, troopIdx] = NoneTroop;
			this.missingStrikeMarks[participantIdx, troopIdx] = 0;
			this.ace[participantIdx, troopIdx] = false;
		}

		public int ObtainLostHitPoints(int participantIdx, int troopIdx, int troopKind)
		{
			return IsEquivalentTo(participantIdx, troopIdx, troopKind) ? this.missingStrikeMarks[participantIdx, troopIdx] : 0;
		}

		public bool VerifyElite(int participantIdx, int troopIdx, int troopKind)
		{
			return IsEquivalentTo(participantIdx, troopIdx, troopKind) && this.ace[participantIdx, troopIdx];
		}

		public int ObtainFullHitPoints(int participantIdx, int troopIdx, int troopKind, bool seasoned)
		{
			return CombatResolver.TopHitPoints(seasoned, VerifyElite(participantIdx, troopIdx, troopKind));
		}

		public int ObtainHitPoints(int participantIdx, int troopIdx, int troopKind, bool seasoned)
		{
			int complete = ObtainFullHitPoints(participantIdx, troopIdx, troopKind, seasoned);

			return Math.Max(1, complete - ObtainLostHitPoints(participantIdx, troopIdx, troopKind));
		}

		public void Assign(int participantIdx, int troopIdx, int troopKind, int missingStrikeMarks, bool ace)
		{
			if (!VerifyValid(participantIdx, troopIdx))
				return;

			this.troopKinds[participantIdx, troopIdx] = (byte)troopKind;
			this.missingStrikeMarks[participantIdx, troopIdx] = (byte)Math.Clamp(missingStrikeMarks, 0, CombatResolver.PeakStrikeMarks - 1);
			this.ace[participantIdx, troopIdx] = ace;
		}

		public int MendAmount(int participantIdx, int troopIdx, int troopKind, int amount)
		{
			if (amount <= 0 || !IsEquivalentTo(participantIdx, troopIdx, troopKind) || this.missingStrikeMarks[participantIdx, troopIdx] == 0)
				return 0;

			int healed = 0;
			for (int i = 0; i < amount && this.missingStrikeMarks[participantIdx, troopIdx] > 0; i++)
			{
				this.missingStrikeMarks[participantIdx, troopIdx]--;
				healed++;
			}

			return healed;
		}

		public void Store(Stream flow)
		{
			flow.Write(DocumentSignature, 0, DocumentSignature.Length);
			flow.WriteByte(DocumentEdition);

			for (int idx = 0; idx < ParticipantTally; idx++)
			{
				for (int jdx = 0; jdx < TroopTally; jdx++)
				{
					flow.WriteByte(this.troopKinds[idx, jdx]);
					flow.WriteByte(this.missingStrikeMarks[idx, jdx]);
					flow.WriteByte((byte)(this.ace[idx, jdx] ? 1 : 0));
				}
			}
		}

		public bool Import(Stream flow)
		{
			WipeAll();

			byte[] content = new byte[DocumentSignature.Length + 1 + ParticipantTally * TroopTally * 3];
			int span = 0;
			int pull;

			while (span < content.Length && (pull = flow.Read(content, span, content.Length - span)) > 0)
				span += pull;

			if (span != content.Length || content[4] != DocumentEdition)
				return false;

			for (int idx = 0; idx < DocumentSignature.Length; idx++)
			{
				if (content[idx] != DocumentSignature[idx])
					return false;
			}

			int place = DocumentSignature.Length + 1;

			for (int idx = 0; idx < ParticipantTally; idx++)
			{
				for (int jdx = 0; jdx < TroopTally; jdx++)
				{
					this.troopKinds[idx, jdx] = content[place];
					this.missingStrikeMarks[idx, jdx] = (byte)Math.Min((int)content[place + 1], CombatResolver.PeakStrikeMarks - 1);
					this.ace[idx, jdx] = content[place + 2] != 0;
					place += 3;
				}
			}

			return true;
		}

		private static bool VerifyValid(int participantIdx, int troopIdx)
		{
			return participantIdx >= 0 && participantIdx < ParticipantTally && troopIdx >= 0 && troopIdx < TroopTally;
		}

		private bool IsEquivalentTo(int participantIdx, int troopIdx, int troopKind)
		{
			return VerifyValid(participantIdx, troopIdx) && this.troopKinds[participantIdx, troopIdx] == (byte)troopKind;
		}
	}
}
