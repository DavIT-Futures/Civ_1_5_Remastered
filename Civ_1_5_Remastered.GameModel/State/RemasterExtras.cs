using System;
using System.IO;

namespace Civ_1_5_Remastered.GameModel.State
{
	/// <summary>
	/// Remastered state that has no room in the original save format: city production queues
	/// and settler automation orders. It is saved next to the game as a separate file.
	/// </summary>
	/// <remarks>
	/// Every entry remembers what it belongs to (the owner and name of a city, the kind of a unit),
	/// so an entry left behind by a destroyed city or unit is ignored instead of leaking into a new one.
	/// </remarks>
	public class RemasterExtras
	{
		public const int TownTally = 128;
		public const int ParticipantTally = 8;
		public const int TroopTally = 128;
		public const int QueueLength = 5;
		public const sbyte EmptySlot = sbyte.MaxValue;

		public const int Anywhere = -1;

		private const byte DocumentEdition = 2;
		private const byte FirstEdition = 1;
		private const byte NoneOwner = 0xff;

		private static readonly byte[] DocumentSignature = { (byte)'O', (byte)'C', (byte)'R', (byte)'X' };

		private readonly byte[] queueOwners = new byte[TownTally];
		private readonly byte[] queueLabels = new byte[TownTally];
		private readonly sbyte[,] queues = new sbyte[TownTally, QueueLength];
		private readonly byte[,] automationKinds = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] automationModes = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] automationTowns = new byte[ParticipantTally, TroopTally];

		public RemasterExtras()
		{
			WipeAll();
		}

		public void WipeAll()
		{
			for (int townIndex = 0; townIndex < TownTally; townIndex++)
			{
				ClearQueue(townIndex);
			}

			for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < TroopTally; troopIndex++)
				{
					ClearAutomation(participantIndex, troopIndex);
				}
			}
		}

		public int QueueCount(int townIndex, City town)
		{
			if (!OwnsQueue(townIndex, town))
			{
				return 0;
			}

			int count = 0;

			while (count < QueueLength && this.queues[townIndex, count] != EmptySlot)
			{
				count++;
			}

			return count;
		}

		public sbyte QueueItem(int townIndex, City town, int position) =>
			position >= 0 && position < QueueCount(townIndex, town) ? this.queues[townIndex, position] : EmptySlot;

		public bool Enqueue(int townIndex, City town, sbyte making)
		{
			if (!ValidTown(townIndex) || making == EmptySlot)
			{
				return false;
			}

			if (!OwnsQueue(townIndex, town))
			{
				ClearQueue(townIndex);
				this.queueOwners[townIndex] = (byte)town.ParticipantIndex;
				this.queueLabels[townIndex] = town.LabelIndex;
			}

			int count = QueueCount(townIndex, town);

			if (count >= QueueLength)
			{
				return false;
			}

			this.queues[townIndex, count] = making;
			return true;
		}

		public sbyte Dequeue(int townIndex, City town)
		{
			sbyte first = QueueItem(townIndex, town, 0);

			if (first != EmptySlot)
			{
				RemoveAt(townIndex, town, 0);
			}

			return first;
		}

		public void RemoveAt(int townIndex, City town, int position)
		{
			int count = QueueCount(townIndex, town);

			if (position < 0 || position >= count)
			{
				return;
			}

			for (int index = position; index < QueueLength - 1; index++)
			{
				this.queues[townIndex, index] = this.queues[townIndex, index + 1];
			}

			this.queues[townIndex, QueueLength - 1] = EmptySlot;
		}

		public void ClearQueue(int townIndex)
		{
			if (!ValidTown(townIndex))
			{
				return;
			}

			this.queueOwners[townIndex] = NoneOwner;
			this.queueLabels[townIndex] = 0;

			for (int index = 0; index < QueueLength; index++)
			{
				this.queues[townIndex, index] = EmptySlot;
			}
		}

		public SettlerAutomation AutomationMode(int participantIndex, int troopIndex, int troopKind)
		{
			if (!ValidTroop(participantIndex, troopIndex) || this.automationKinds[participantIndex, troopIndex] != (byte)troopKind)
			{
				return SettlerAutomation.None;
			}

			return (SettlerAutomation)this.automationModes[participantIndex, troopIndex];
		}

		/// <summary>The only city around which the settler works, or <see cref="Anywhere"/>.</summary>
		public int AutomationTown(int participantIndex, int troopIndex, int troopKind)
		{
			if (AutomationMode(participantIndex, troopIndex, troopKind) == SettlerAutomation.None)
			{
				return Anywhere;
			}

			byte town = this.automationTowns[participantIndex, troopIndex];

			return town < TownTally ? town : Anywhere;
		}

		public void AssignAutomation(int participantIndex, int troopIndex, int troopKind, SettlerAutomation mode, int townIndex = Anywhere)
		{
			if (!ValidTroop(participantIndex, troopIndex))
			{
				return;
			}

			this.automationKinds[participantIndex, troopIndex] = mode == SettlerAutomation.None ? NoneOwner : (byte)troopKind;
			this.automationModes[participantIndex, troopIndex] = (byte)mode;
			this.automationTowns[participantIndex, troopIndex] = mode != SettlerAutomation.None && ValidTown(townIndex) ? (byte)townIndex : NoneOwner;
		}

		public void ClearAutomation(int participantIndex, int troopIndex) =>
			AssignAutomation(participantIndex, troopIndex, NoneOwner, SettlerAutomation.None);

		public void Store(Stream flow)
		{
			flow.Write(DocumentSignature, 0, DocumentSignature.Length);
			flow.WriteByte(DocumentEdition);

			for (int townIndex = 0; townIndex < TownTally; townIndex++)
			{
				flow.WriteByte(this.queueOwners[townIndex]);
				flow.WriteByte(this.queueLabels[townIndex]);

				for (int index = 0; index < QueueLength; index++)
				{
					flow.WriteByte((byte)this.queues[townIndex, index]);
				}
			}

			for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < TroopTally; troopIndex++)
				{
					flow.WriteByte(this.automationKinds[participantIndex, troopIndex]);
					flow.WriteByte(this.automationModes[participantIndex, troopIndex]);
					flow.WriteByte(this.automationTowns[participantIndex, troopIndex]);
				}
			}
		}

		public bool Import(Stream flow)
		{
			WipeAll();

			byte[] content = new byte[DocumentSignature.Length + 1 + (TownTally * (2 + QueueLength)) + (ParticipantTally * TroopTally * 3)];
			int span = 0;
			int pull;

			while (span < content.Length && (pull = flow.Read(content, span, content.Length - span)) > 0)
			{
				span += pull;
			}

			int edition = span > DocumentSignature.Length ? content[DocumentSignature.Length] : 0;
			int unitBytes = edition == FirstEdition ? 2 : 3;
			int expected = DocumentSignature.Length + 1 + (TownTally * (2 + QueueLength)) + (ParticipantTally * TroopTally * unitBytes);

			if ((edition != DocumentEdition && edition != FirstEdition) || span != expected)
			{
				return false;
			}

			for (int index = 0; index < DocumentSignature.Length; index++)
			{
				if (content[index] != DocumentSignature[index])
				{
					return false;
				}
			}

			int place = DocumentSignature.Length + 1;

			for (int townIndex = 0; townIndex < TownTally; townIndex++)
			{
				this.queueOwners[townIndex] = content[place++];
				this.queueLabels[townIndex] = content[place++];

				for (int index = 0; index < QueueLength; index++)
				{
					this.queues[townIndex, index] = (sbyte)content[place++];
				}
			}

			for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < TroopTally; troopIndex++)
				{
					this.automationKinds[participantIndex, troopIndex] = content[place++];
					byte mode = content[place++];
					this.automationModes[participantIndex, troopIndex] = Enum.IsDefined(typeof(SettlerAutomation), (int)mode) ? mode : (byte)0;
					this.automationTowns[participantIndex, troopIndex] = unitBytes == 3 ? content[place++] : NoneOwner;
				}
			}

			return true;
		}

		private static bool ValidTown(int townIndex) => townIndex >= 0 && townIndex < TownTally;

		private static bool ValidTroop(int participantIndex, int troopIndex) =>
			participantIndex >= 0 && participantIndex < ParticipantTally && troopIndex >= 0 && troopIndex < TroopTally;

		private bool OwnsQueue(int townIndex, City town) =>
			ValidTown(townIndex) && town.ConditionMarker != 0xff &&
			this.queueOwners[townIndex] == (byte)town.ParticipantIndex && this.queueLabels[townIndex] == town.LabelIndex;
	}
}
