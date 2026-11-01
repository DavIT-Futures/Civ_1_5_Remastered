using System;
using System.IO;

namespace Civ_1_5_Remastered.GameModel.Rules
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
			for (int index = 0; index < ParticipantTally; index++)
			{
				for (int innerIndex = 0; innerIndex < TroopTally; innerIndex++)
				{
					Wipe(index, innerIndex);
				}
			}
		}

		public void Wipe(int participantIndex, int troopIndex)
		{
			if (!VerifyValid(participantIndex, troopIndex))
				return;

			this.troopKinds[participantIndex, troopIndex] = NoneTroop;
			this.missingStrikeMarks[participantIndex, troopIndex] = 0;
			this.ace[participantIndex, troopIndex] = false;
		}

		public int ObtainLostHitPoints(int participantIndex, int troopIndex, int troopKind)
		{
			return IsEquivalentTo(participantIndex, troopIndex, troopKind) ? this.missingStrikeMarks[participantIndex, troopIndex] : 0;
		}

		public bool VerifyElite(int participantIndex, int troopIndex, int troopKind)
		{
			return IsEquivalentTo(participantIndex, troopIndex, troopKind) && this.ace[participantIndex, troopIndex];
		}

		public int ObtainFullHitPoints(int participantIndex, int troopIndex, int troopKind, bool seasoned)
		{
			return CombatResolver.TopHitPoints(seasoned, VerifyElite(participantIndex, troopIndex, troopKind));
		}

		public int ObtainHitPoints(int participantIndex, int troopIndex, int troopKind, bool seasoned)
		{
			int complete = ObtainFullHitPoints(participantIndex, troopIndex, troopKind, seasoned);

			return Math.Max(1, complete - ObtainLostHitPoints(participantIndex, troopIndex, troopKind));
		}

		public void Assign(int participantIndex, int troopIndex, int troopKind, int missingStrikeMarks, bool ace)
		{
			if (!VerifyValid(participantIndex, troopIndex))
				return;

			this.troopKinds[participantIndex, troopIndex] = (byte)troopKind;
			this.missingStrikeMarks[participantIndex, troopIndex] = (byte)Math.Clamp(missingStrikeMarks, 0, CombatResolver.PeakStrikeMarks - 1);
			this.ace[participantIndex, troopIndex] = ace;
		}

		public int MendAmount(int participantIndex, int troopIndex, int troopKind, int amount)
		{
			if (amount <= 0 || !IsEquivalentTo(participantIndex, troopIndex, troopKind) || this.missingStrikeMarks[participantIndex, troopIndex] == 0)
				return 0;

			int healed = 0;
			for (int index = 0; index < amount && this.missingStrikeMarks[participantIndex, troopIndex] > 0; index++)
			{
				this.missingStrikeMarks[participantIndex, troopIndex]--;
				healed++;
			}

			return healed;
		}

		public void Store(Stream flow)
		{
			flow.Write(DocumentSignature, 0, DocumentSignature.Length);
			flow.WriteByte(DocumentEdition);

			for (int index = 0; index < ParticipantTally; index++)
			{
				for (int innerIndex = 0; innerIndex < TroopTally; innerIndex++)
				{
					flow.WriteByte(this.troopKinds[index, innerIndex]);
					flow.WriteByte(this.missingStrikeMarks[index, innerIndex]);
					flow.WriteByte((byte)(this.ace[index, innerIndex] ? 1 : 0));
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

			for (int index = 0; index < DocumentSignature.Length; index++)
			{
				if (content[index] != DocumentSignature[index])
					return false;
			}

			int place = DocumentSignature.Length + 1;

			for (int index = 0; index < ParticipantTally; index++)
			{
				for (int innerIndex = 0; innerIndex < TroopTally; innerIndex++)
				{
					this.troopKinds[index, innerIndex] = content[place];
					this.missingStrikeMarks[index, innerIndex] = (byte)Math.Min((int)content[place + 1], CombatResolver.PeakStrikeMarks - 1);
					this.ace[index, innerIndex] = content[place + 2] != 0;
					place += 3;
				}
			}

			return true;
		}

		private static bool VerifyValid(int participantIndex, int troopIndex)
		{
			return participantIndex >= 0 && participantIndex < ParticipantTally && troopIndex >= 0 && troopIndex < TroopTally;
		}

		private bool IsEquivalentTo(int participantIndex, int troopIndex, int troopKind)
		{
			return VerifyValid(participantIndex, troopIndex) && this.troopKinds[participantIndex, troopIndex] == (byte)troopKind;
		}
	}
}
