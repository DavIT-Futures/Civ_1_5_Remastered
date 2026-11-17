using System;
using System.IO;

namespace Civ_1_5_Remastered.GameModel.State
{
	/// <summary>
	/// Remastered state that has no room in the original save format: city production queues, settler automation
	/// orders and the real kind of every unit of an extra kind (the original format holds it as its stand-in kind, see
	/// Rules.UnitKinds). It is saved next to the game as a separate file.
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

		/// <summary>Places for extra unit kinds: the pictures of a civilization have room for four more kinds.</summary>
		public const int ExtraKindPlaces = 4;

		/// <summary>Tallies per civilization and extra kind: units alive, units in production, units lost.</summary>
		public const int KindTallyLists = 3;

		public const int KeptTroopPlaces = 2;

		public const byte NoKind = 0xff;

		/// <summary>Bytes the fifth edition adds: the extra kinds of units, productions, kept units and tallies.</summary>
		public const int ExtraKindsBytes =
			(ParticipantTally * TroopTally) + (TownTally * (1 + KeptTroopPlaces)) + (ParticipantTally * KindTallyLists * ExtraKindPlaces * 2);

		private const byte DocumentEdition = 5;
		private const byte FourthEdition = 4;
		private const byte FirstEdition = 1;
		private const byte SecondEdition = 2;
		private const byte ThirdEdition = 3;
		private const byte NoneOwner = 0xff;

		private static readonly byte[] DocumentSignature = { (byte)'O', (byte)'C', (byte)'R', (byte)'X' };

		private readonly byte[] queueOwners = new byte[TownTally];
		private readonly byte[] queueLabels = new byte[TownTally];
		private readonly sbyte[,] queues = new sbyte[TownTally, QueueLength];
		private readonly byte[,] automationKinds = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] automationModes = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] automationTowns = new byte[ParticipantTally, TroopTally];
		private readonly byte[,] troopKinds = new byte[ParticipantTally, TroopTally];
		private readonly byte[] makingKinds = new byte[TownTally];
		private readonly byte[,] keptKinds = new byte[TownTally, KeptTroopPlaces];
		private readonly short[,,] kindTallies = new short[ParticipantTally, KindTallyLists, ExtraKindPlaces];

		public RemasterExtras()
		{
			WipeAll();
		}

		/// <summary>The fixed world the game is played on: 0 a random world or EARTH, 2 EUROPE (see EuropeMap).</summary>
		public byte World { get; set; }

		/// <summary>
		/// The game was saved when the player ended the turn (an autosave): the player's cities and units have done
		/// their part of the round, the civilizations after the player have not.
		/// </summary>
		public bool AtTurnEnd { get; set; }

		public void WipeAll()
		{
			this.World = 0;
			this.AtTurnEnd = false;

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

			ClearExtraKinds();
		}

		/// <summary>Forgets every unit, production and tally of an extra kind.</summary>
		public void ClearExtraKinds()
		{
			for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < TroopTally; troopIndex++)
				{
					this.troopKinds[participantIndex, troopIndex] = NoKind;
				}

				for (int list = 0; list < KindTallyLists; list++)
				{
					for (int place = 0; place < ExtraKindPlaces; place++)
					{
						this.kindTallies[participantIndex, list, place] = 0;
					}
				}
			}

			for (int townIndex = 0; townIndex < TownTally; townIndex++)
			{
				this.makingKinds[townIndex] = NoKind;

				for (int place = 0; place < KeptTroopPlaces; place++)
				{
					this.keptKinds[townIndex, place] = NoKind;
				}
			}
		}

		/// <summary>The extra kind of a unit the saved game holds as its stand-in, or <see cref="NoKind"/>.</summary>
		public byte TroopKind(int participantIndex, int troopIndex) =>
			ValidTroop(participantIndex, troopIndex) ? this.troopKinds[participantIndex, troopIndex] : NoKind;

		public void AssignTroopKind(int participantIndex, int troopIndex, byte kind)
		{
			if (ValidTroop(participantIndex, troopIndex))
			{
				this.troopKinds[participantIndex, troopIndex] = kind;
			}
		}

		/// <summary>The extra kind a city makes, held by the saved game as its stand-in, or <see cref="NoKind"/>.</summary>
		public byte MakingKind(int townIndex) => ValidTown(townIndex) ? this.makingKinds[townIndex] : NoKind;

		public void AssignMakingKind(int townIndex, byte kind)
		{
			if (ValidTown(townIndex))
			{
				this.makingKinds[townIndex] = kind;
			}
		}

		/// <summary>The extra kind of a unit a city keeps, held by the saved game as its stand-in, or <see cref="NoKind"/>.</summary>
		public byte KeptKind(int townIndex, int place) =>
			ValidTown(townIndex) && place >= 0 && place < KeptTroopPlaces ? this.keptKinds[townIndex, place] : NoKind;

		public void AssignKeptKind(int townIndex, int place, byte kind)
		{
			if (ValidTown(townIndex) && place >= 0 && place < KeptTroopPlaces)
			{
				this.keptKinds[townIndex, place] = kind;
			}
		}

		/// <summary>A tally of an extra kind (place 0 is the first extra kind) that the saved game adds to its stand-in.</summary>
		public short KindTally(int participantIndex, int list, int place) =>
			ValidTally(participantIndex, list, place) ? this.kindTallies[participantIndex, list, place] : (short)0;

		public void AssignKindTally(int participantIndex, int list, int place, short tally)
		{
			if (ValidTally(participantIndex, list, place))
			{
				this.kindTallies[participantIndex, list, place] = tally;
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

			flow.WriteByte(this.World);
			flow.WriteByte(this.AtTurnEnd ? (byte)1 : (byte)0);

			for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < TroopTally; troopIndex++)
				{
					flow.WriteByte(this.troopKinds[participantIndex, troopIndex]);
				}
			}

			for (int townIndex = 0; townIndex < TownTally; townIndex++)
			{
				flow.WriteByte(this.makingKinds[townIndex]);

				for (int place = 0; place < KeptTroopPlaces; place++)
				{
					flow.WriteByte(this.keptKinds[townIndex, place]);
				}
			}

			for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
			{
				for (int list = 0; list < KindTallyLists; list++)
				{
					for (int place = 0; place < ExtraKindPlaces; place++)
					{
						short tally = this.kindTallies[participantIndex, list, place];
						flow.WriteByte((byte)tally);
						flow.WriteByte((byte)(tally >> 8));
					}
				}
			}
		}

		public bool Import(Stream flow)
		{
			WipeAll();

			byte[] content = new byte[DocumentSignature.Length + 1 + (TownTally * (2 + QueueLength)) + (ParticipantTally * TroopTally * 3) + 2 + ExtraKindsBytes];
			int span = 0;
			int pull;

			while (span < content.Length && (pull = flow.Read(content, span, content.Length - span)) > 0)
			{
				span += pull;
			}

			int edition = span > DocumentSignature.Length ? content[DocumentSignature.Length] : 0;
			int unitBytes = edition == FirstEdition ? 2 : 3;
			int expected = DocumentSignature.Length + 1 + (TownTally * (2 + QueueLength)) + (ParticipantTally * TroopTally * unitBytes) +
				(edition >= ThirdEdition ? 1 : 0) + (edition >= FourthEdition ? 1 : 0) + (edition >= DocumentEdition ? ExtraKindsBytes : 0);

			if (edition < FirstEdition || edition > DocumentEdition || span != expected)
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

			this.World = edition >= ThirdEdition ? content[place++] : (byte)0;
			this.AtTurnEnd = edition >= FourthEdition && content[place] != 0;
			place += edition >= FourthEdition ? 1 : 0;

			if (edition >= DocumentEdition)
			{
				for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
				{
					for (int troopIndex = 0; troopIndex < TroopTally; troopIndex++)
					{
						this.troopKinds[participantIndex, troopIndex] = content[place++];
					}
				}

				for (int townIndex = 0; townIndex < TownTally; townIndex++)
				{
					this.makingKinds[townIndex] = content[place++];

					for (int keptPlace = 0; keptPlace < KeptTroopPlaces; keptPlace++)
					{
						this.keptKinds[townIndex, keptPlace] = content[place++];
					}
				}

				for (int participantIndex = 0; participantIndex < ParticipantTally; participantIndex++)
				{
					for (int list = 0; list < KindTallyLists; list++)
					{
						for (int tallyPlace = 0; tallyPlace < ExtraKindPlaces; tallyPlace++)
						{
							this.kindTallies[participantIndex, list, tallyPlace] = (short)(content[place] | (content[place + 1] << 8));
							place += 2;
						}
					}
				}
			}

			return true;
		}

		private static bool ValidTown(int townIndex) => townIndex >= 0 && townIndex < TownTally;

		private static bool ValidTally(int participantIndex, int list, int place) =>
			participantIndex >= 0 && participantIndex < ParticipantTally && list >= 0 && list < KindTallyLists && place >= 0 && place < ExtraKindPlaces;


		private static bool ValidTroop(int participantIndex, int troopIndex) =>
			participantIndex >= 0 && participantIndex < ParticipantTally && troopIndex >= 0 && troopIndex < TroopTally;

		private bool OwnsQueue(int townIndex, City town) =>
			ValidTown(townIndex) && town.ConditionMarker != 0xff &&
			this.queueOwners[townIndex] == (byte)town.ParticipantIndex && this.queueLabels[townIndex] == town.LabelIndex;
	}
}
