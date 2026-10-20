using System;
using System.IO;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Hit points that units lost in battles, and which units are elite.
	/// The original save file has no place for them, so they are saved in a separate file next to the saved game
	/// (for example CIVIL0.HP next to CIVIL0.SVE), and the original game can still load the saved game.
	/// Each entry remembers the unit type, so an entry is ignored when its unit slot is used by another unit.
	/// </summary>
	public class UnitHealth
	{
		public const int ParticipantTally = 8;
		public const int TroopTally = 128;

		private static readonly byte[] DocumentSignature = { (byte)'O', (byte)'C', (byte)'H', (byte)'P' };
		private const byte DocumentEdition = 1;
		private const byte NoneTroop = 0xff;

		private readonly byte[,] troopKinds = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] missingStrikeMarks = new byte[ParticipantTally, TroopTally];
		private readonly bool[,] ace = new bool[ParticipantTally, TroopTally];

		public UnitHealth()
		{
			WipeAll();
		}

		private static bool VerifyValid(int participantIdx, int troopIdx)
		{
			return participantIdx >= 0 && participantIdx < ParticipantTally && troopIdx >= 0 && troopIdx < TroopTally;
		}

		private bool IsEquivalentTo(int participantIdx, int troopIdx, int troopKind)
		{
			return VerifyValid(participantIdx, troopIdx) && this.troopKinds[participantIdx, troopIdx] == (byte)troopKind;
		}

		/// <summary>
		/// Forgets all units, for a new game or a saved game without the hit points file
		/// </summary>
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

		/// <summary>
		/// Forgets one unit: it is new, destroyed or its slot is reused
		/// </summary>
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

		/// <summary>
		/// Hit points of a fully healthy unit
		/// </summary>
		public int ObtainFullHitPoints(int participantIdx, int troopIdx, int troopKind, bool seasoned)
		{
			return CombatResolver.TopHitPoints(seasoned, VerifyElite(participantIdx, troopIdx, troopKind));
		}

		/// <summary>
		/// Current hit points of a unit, at least 1
		/// </summary>
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

		/// <summary>
		/// Heals one hit point. Returns true if the unit was wounded.
		/// </summary>
		public bool MendOne(int participantIdx, int troopIdx, int troopKind)
		{
			if (!IsEquivalentTo(participantIdx, troopIdx, troopKind) || this.missingStrikeMarks[participantIdx, troopIdx] == 0)
				return false;

			this.missingStrikeMarks[participantIdx, troopIdx]--;

			return true;
		}

		/// <summary>
		/// Heals up to <paramref name="amount"/> hit points. Returns the number of hit points actually healed.
		/// </summary>
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

		/// <summary>
		/// Loads the hit points, returns false (and forgets all units) if the data is not valid
		/// </summary>
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
	}
}
