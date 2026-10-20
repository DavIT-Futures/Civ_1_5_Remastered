using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 15.
	/// </summary>
	public class CivilizationSchism : GameModule
	{
		public CivilizationSchism(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// A civil war splits the player: in the first free slot (no units, no cities) a new civilization rises (none or
		/// ConstructCivilization fails: 0), with a new leader, half the treasury and might, the same research, government and
		/// known technologies count, at war with the player (relation 9; the technologies are copied onto the player himself
		/// and the second relation goes to the player's own row too, like in the original). It gets the continents beyond the
		/// capital's when they make between half and all of the rest, else the cities farthest from the capital until it has
		/// enough; the player's units follow their cities, units in the new civilization's cities or homed there change sides
		/// (with their stacks). Both get a capital (the human player's map moves home). Returns 1. The continent sizes are in
		/// the stack frame, like in the original.
		/// </summary>
		public ushort DivideCivilization(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DivideCivilization({participantIdx})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x48);
			this.processor.PreserveIndexRegisters();

			ushort result = SplitCivilization(participantIdx);

			this.processor.RegAcc.Value16 = result;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DivideCivilization");

			return result;
		}

		/// <summary>The continent's total city size in the frame (-0x44 + 2 * continent)</summary>
		private ushort ContinentShare(ushort continent)
		{
			return (ushort)((ushort)(continent << 1) + this.processor.RegFrame.Value16 - 0x44);
		}

		/// <summary>The civil war (see DivideCivilization)</summary>
		private ushort SplitCivilization(short participantIdx)
		{
			short rebelIdx = -1;
			ushort slotIdx;

			this.processor.EmitLocalWord(0x16, 0xffff);

			for (slotIdx = 1; slotIdx < 8; slotIdx++)
			{
				this.processor.EmitLocalWord(0x14, slotIdx);

				if (this.owner.World.Participants[slotIdx].TroopTally == 0 && this.owner.World.Participants[slotIdx].TownTally == 0)
				{
					rebelIdx = (short)slotIdx;
					this.processor.EmitLocalWord(0x16, slotIdx);
					break;
				}
			}

			if (rebelIdx == -1)
				return 0;

			for (ushort continent = 0; continent < 0x10; continent++)
			{
				this.processor.RegSource.Value16 = (ushort)(continent << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, ContinentShare(continent), 0);
			}

			this.processor.EmitLocalWord(0x24, 0xffff);

			ushort townIdx;

			for (townIdx = 0; townIdx < 0x80; townIdx++)
			{
				this.processor.EmitLocalWord(0x14, townIdx);
				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				ushort share = ContinentShare(this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y));
				this.processor.RegDest.Value16 = (ushort)(this.processor.RegAcc.Value16 << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, share, (ushort)(this.processor.PullUInt16(this.processor.SegStack.Value16, share) + town.TrueExtent));

				if (!town.Has(CityStructure.Palace))
					continue;

				if (this.processor.PullLocalWord(0x24) != 0xffff && (ushort)this.owner.World.Participants[participantIdx].HorizBegin != (ushort)(short)town.Place.X)
					continue;

				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

				this.processor.EmitLocalWord(0x24, this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y));
				this.processor.EmitLocalWord(0xe, (ushort)(short)town.Place.X);
				this.processor.EmitLocalWord(0x12, (ushort)(short)town.Place.Y);
				this.processor.EmitLocalWord(0x8, townIdx);
			}

			this.processor.EmitLocalWord(0x14, townIdx);

			if (this.processor.PullLocalWord(0x24) == 0xffff)
				return 0;

			if (!RaiseRebelCivilization(participantIdx, rebelIdx))
				return 0;

			DivideContinents(participantIdx, rebelIdx);

			this.owner.World.Participants[participantIdx].GrandTownExtent += this.owner.World.Participants[rebelIdx].GrandTownExtent;

			TransferRebelUnits(participantIdx, rebelIdx);

			ushort capitalIdx = ObtainDistanceToCapital(rebelIdx);
			this.processor.EmitLocalWord(0x10, capitalIdx);
			this.processor.RegAcc.Value16 = (ushort)(0x1c * capitalIdx);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.Towns[capitalIdx].StructureMarkers0 |= 1;
			this.owner.World.Participants[rebelIdx].HorizBegin = (short)this.owner.World.Towns[capitalIdx].Place.X;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, (short)this.processor.PullGlobalWord(0xd4cc), (short)this.processor.PullGlobalWord(0xd75e));
			}
			else
			{
				ushort oldCapital = this.processor.PullLocalWord(0x8);
				this.processor.RegAcc.Value16 = (ushort)(0x1c * oldCapital);
				this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.Towns[oldCapital].ParticipantIdx = -1;

				this.processor.StackWord(0); // stack management - push return segment, ignored
				this.processor.StackWord(0x084f); // stack management - push return offset
				this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

				ushort newCapital = ObtainDistanceToCapital(participantIdx);
				this.processor.EmitLocalWord(0x10, newCapital);
				this.processor.RegAcc.Value16 = (ushort)(0x1c * newCapital);
				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.Towns[newCapital].StructureMarkers0 |= 1;
				this.owner.World.Participants[participantIdx].HorizBegin = (short)this.owner.World.Towns[newCapital].Place.X;
				this.owner.World.Towns[oldCapital].ParticipantIdx = participantIdx;
			}

			return 1;
		}

		/// <summary>
		/// The new civilization in the slot (ConstructCivilization; false when it fails): its leader, names, the message, half
		/// the treasury and might, research and government, war with the player
		/// </summary>
		private bool RaiseRebelCivilization(short participantIdx, short rebelIdx)
		{
			bool barbarians = rebelIdx > (short)(this.owner.World.BotRivalTally + 1);
			this.processor.EmitLocalWord(0xc, barbarians ? (ushort)1 : (ushort)0);

			this.owner.World.LiveRealms &= (short)~(1 << (byte)rebelIdx);

			if (this.owner.NewGameWizard.ConstructCivilization(rebelIdx) == 0)
				return false;

			this.owner.World.LiveRealms |= (short)(1 << (byte)rebelIdx);

			Player rebel = this.owner.World.Participants[rebelIdx];
			Player player = this.owner.World.Participants[participantIdx];
			bool newLeader = barbarians;

			if (!barbarians)
			{
				ushort leader = (ushort)rebel.ChiefVisuals;
				this.processor.RegSource.Value16 = leader;

				newLeader = ((rebelIdx != 7 ? 1 : 0) & leader) != 0 || (short)leader > 0xf;
			}

			if (newLeader)
			{
				rebel.ChiefVisuals = (short)(this.owner.Libc.Dice.DrawInt(2) != 0 ? rebelIdx : rebelIdx + 8);

				if ((short)rebel.ChiefVisuals >= 8)
					this.owner.World.ParticipantIdentMarkers |= (short)(1 << (byte)rebelIdx);
			}
			else
			{
				rebel.ChiefVisuals ^= 8;
			}

			ushort names = (ushort)(rebelIdx << 1);
			this.processor.RegSource.Value16 = names;

			ushort leaderNames = (ushort)(0x3a * rebel.ChiefVisuals);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord((ushort)(names + 0x19a2)), (ushort)(leaderNames + 0x14e2));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord((ushort)(names + 0x1982)), (ushort)((ushort)(0x3a * rebel.ChiefVisuals) + 0x1502));

			this.owner.Libc.CopyText(0xba06, (ushort)((ushort)(0x3a * rebel.ChiefVisuals) + 0x14f2));

			if (this.processor.PullGlobalByte(0xba06) == 0)
			{
				Message(this.processor.PullGlobalWord((ushort)(names + 0x1982)))
					.Text("s");
			}

			this.processor.RegSource.Value16 = names;

			this.owner.Libc.CopyText(this.processor.PullGlobalWord((ushort)(names + 0x1992)), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.processor.RegDest.Value16 = (ushort)(participantIdx << 1);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), this.processor.PullGlobalWord((ushort)(((ushort)participantIdx << 1) + 0x1982)));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), this.processor.PullGlobalWord((ushort)(names + 0x1982)));

			this.owner.BlockText.DisplayTextDialog(participantIdx == this.owner.World.PersonParticipantIdx ? (ushort)0x4a18 : (ushort)0x4a20);

			this.processor.EmitGlobalWord(0x2f9e, 0x5);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			rebel.Gold = (short)(player.Gold / 2);
			player.Gold -= rebel.Gold;
			rebel.MartialMight = (short)(player.MartialMight / 2);
			rebel.InquiryHeadway = player.InquiryHeadway;
			rebel.FoundScienceTally = player.FoundScienceTally;
			rebel.RegimeKind = player.RegimeKind;

			// The technologies go back onto the player himself, like in the original (the new civilization gets none)
			ushort scienceIdx;

			for (scienceIdx = 0; scienceIdx < 5; scienceIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(scienceIdx << 1);
				player.FoundScienceMarkers[scienceIdx] = player.FoundScienceMarkers[scienceIdx];
			}

			this.processor.EmitLocalWord(0x14, scienceIdx);

			this.processor.RegSource.Value16 = names;

			rebel.TouchParticipantTimeleft = (short)(this.owner.World.RoundTally - 8);

			// War: both flags go into the player's relation to the new civilization, like in the original
			player.Relations[rebelIdx] |= 1;

			this.processor.RegDest.Value16 = (ushort)(rebelIdx << 4);

			player.Relations[rebelIdx] |= 9;

			return true;
		}

		/// <summary>
		/// The continents beyond the capital's go to the new civilization when they make between half and all of the rest,
		/// else the cities farthest from the capital until its share is reached (9 * size / (0xdc6a + 3) of the player's)
		/// </summary>
		private void DivideContinents(short participantIdx, short rebelIdx)
		{
			ushort capitalContinent = this.processor.PullLocalWord(0x24);
			short rest = (short)this.processor.PullUInt16(this.processor.SegStack.Value16, ContinentShare(capitalContinent));
			short given = 0;

			this.processor.EmitLocalWord(0x1e, (ushort)rest);
			this.processor.EmitLocalWord(0x22, 0);

			ushort continent;

			for (continent = 0; continent < 0x10; continent++)
			{
				this.processor.EmitLocalWord(0x14, continent);
				this.processor.RegSource.Value16 = (ushort)(continent << 1);

				ushort share = ContinentShare(continent);
				short size = (short)this.processor.PullUInt16(this.processor.SegStack.Value16, share);

				if (size == 0)
					continue;

				if (given <= rest && continent != capitalContinent)
				{
					this.processor.RegSource.Value16 = share;

					given += size;
					this.processor.EmitLocalWord(0x22, (ushort)given);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, share, 2);
					continue;
				}

				if (continent != capitalContinent)
				{
					rest += size;
					this.processor.EmitLocalWord(0x1e, (ushort)rest);
				}

				this.processor.RegSource.Value16 = (ushort)(continent << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, share, 1);
			}

			this.processor.EmitLocalWord(0x14, continent);

			if ((short)(given << 1) > rest && given <= rest)
			{
				ushort townIdx;

				for (townIdx = 0; townIdx < 0x80; townIdx++)
				{
					this.processor.EmitLocalWord(0x14, townIdx);
					this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

					City town = this.owner.World.Towns[townIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
						continue;

					ushort townContinent = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
					this.processor.EmitLocalWord(0x18, townContinent);
					this.processor.RegDest.Value16 = (ushort)(townContinent << 1);

					if (this.processor.PullUInt16(this.processor.SegStack.Value16, ContinentShare(townContinent)) == 2)
						AssignCityOwner((short)townIdx, rebelIdx);
				}

				this.processor.EmitLocalWord(0x14, townIdx);
				return;
			}

			given = 0;
			this.processor.EmitLocalWord(0x22, 0);

			while (CPU.SignedQuotient((short)(9 * given), (short)(this.processor.PullGlobalWord(0xdc6a) + 3)) < this.owner.World.Participants[participantIdx].GrandTownExtent)
			{
				short farthest = 1;
				this.processor.EmitLocalWord(0xa, (ushort)farthest);

				ushort townIdx;

				for (townIdx = 0; townIdx < 0x80; townIdx++)
				{
					this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

					City town = this.owner.World.Towns[townIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
						continue;

					short distance = (short)(ushort)this.owner.Helpers.ObtainDistance(town.Place.X, (short)this.processor.PullLocalWord(0xe), town.Place.Y, (short)this.processor.PullLocalWord(0x12));
					this.processor.EmitLocalWord(0x4, (ushort)distance);

					if (distance > farthest)
					{
						farthest = distance;
						this.processor.EmitLocalWord(0xa, (ushort)farthest);
						this.processor.EmitLocalWord(0x10, townIdx);
					}
				}

				this.processor.EmitLocalWord(0x14, townIdx);

				// Without a farther city the one of the last round is taken again, like in the original
				ushort farthestIdx = this.processor.PullLocalWord(0x10);
				this.processor.RegAcc.Value16 = (ushort)(0x1c * farthestIdx);
				this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

				given += this.owner.World.Towns[farthestIdx].TrueExtent;
				this.processor.EmitLocalWord(0x22, (ushort)given);

				AssignCityOwner((short)farthestIdx, rebelIdx);
			}
		}

		/// <summary>
		/// The player's units follow the cities: a unit in a city of another owner than its home city's owner goes to the
		/// city's owner (homed by the raw byte at 0x1c * city, like in the original); in a city of the new civilization
		/// homed there it changes sides; outside a city, a unit homed in a city of the new civilization changes sides with
		/// its stack, else the stack's units homed elsewhere get its home city
		/// </summary>
		private void TransferRebelUnits(short participantIdx, short rebelIdx)
		{
			ushort troopIdx;

			for (troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				this.processor.EmitLocalWord(0x14, troopIdx);

				ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
				this.processor.RegSource.Value16 = (ushort)(0x600 * participantIdx);

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.processor.RegSource.Value16 = troop;
				this.processor.EmitLocalWord(0x2, (ushort)(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)));

				this.owner.MapOps.ObtainTileImprovements((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				if ((this.processor.RegAcc.LowByte & 1) != 0)
					TransferUnitInCity(participantIdx, rebelIdx, troopIdx, troop);
				else
					TransferUnitOutside(participantIdx, rebelIdx, troopIdx, troop);
			}

			this.processor.EmitLocalWord(0x14, troopIdx);
		}

		/// <summary>A unit of the player in a city (see TransferRebelUnits)</summary>
		private void TransferUnitInCity(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop)
		{
			ushort townIdx = this.owner.Helpers.LocateCityAtPosition((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
			this.processor.EmitLocalWord(0x10, townIdx);
			this.processor.EmitLocalWord(0x2, (ushort)(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)));

			ushort townRecord = (ushort)(0x1c * townIdx);
			this.processor.RegDest.Value16 = townRecord;

			byte homeIdx = this.processor.PullGlobalByte((ushort)(troop + 0x81df));

			// AX holds the home city's offset like in the original (a unit without a home, 0xff, throws here)
			this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)homeIdx);

			if (this.owner.World.Towns[homeIdx].ParticipantIdx != this.owner.World.Towns[townIdx].ParticipantIdx)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, (short)troopIdx);

				this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				this.processor.EmitLocalWord(0x6, this.owner.UnitOps.ConstructUnit((sbyte)this.processor.PullGlobalByte(townRecord), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)),
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6))));
				return;
			}

			if ((ushort)this.owner.World.Towns[townIdx].ParticipantIdx != (ushort)rebelIdx)
				return;

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)troopIdx);

			this.processor.RegSource.Value16 = troop;

			this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			ushort newTroop = this.owner.UnitOps.ConstructUnit(rebelIdx, (short)this.processor.PullLocalWord(0x2),
				(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
			this.processor.EmitLocalWord(0x6, newTroop);

			ushort copy = (ushort)((ushort)(0xc * newTroop) + (ushort)(0x600 * rebelIdx));
			this.processor.RegDest.Value16 = (ushort)(0x600 * rebelIdx);

			this.processor.EmitGlobalByte((ushort)(copy + 0x81d4), (byte)(this.processor.PullGlobalByte((ushort)(troop + 0x81d4)) & 0xfe));
		}

		/// <summary>A unit of the player outside the cities, with its stack (see TransferRebelUnits)</summary>
		private void TransferUnitOutside(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop)
		{
			ushort playerUnits = (ushort)(0x600 * participantIdx);

			// AX holds the home city's offset like in the original (a unit without a home, 0xff, throws here)
			this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81df)));

			short homeOwner = this.owner.World.Towns[this.processor.PullGlobalByte((ushort)(troop + 0x81df))].ParticipantIdx;
			short next = (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81de));

			this.processor.EmitLocalWord(0x1c, (ushort)homeOwner);
			this.processor.EmitLocalWord(0x1a, (ushort)next);

			if ((ushort)homeOwner != (ushort)rebelIdx)
			{
				// The stack's units homed elsewhere get the unit's home city
				while (next != -1 && (ushort)next != troopIdx)
				{
					this.processor.RegSource.Value16 = playerUnits;

					ushort other = (ushort)((ushort)(0xc * next) + playerUnits);
					this.processor.RegDest.Value16 = other;

					short following = (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81de));
					this.processor.EmitLocalWord(0x20, (ushort)following);
					this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81df)));

					if (this.owner.World.Towns[this.processor.PullGlobalByte((ushort)(other + 0x81df))].ParticipantIdx != homeOwner)
					{
						this.processor.EmitGlobalByte((ushort)(other + 0x81df), this.processor.PullGlobalByte((ushort)((ushort)(0xc * troopIdx) + playerUnits + 0x81df)));
					}

					next = following;
					this.processor.EmitLocalWord(0x1a, (ushort)next);
				}

				return;
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)troopIdx);

			this.owner.MapOps.MapAssignCityOwner(rebelIdx, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			ushort newTroop = this.owner.UnitOps.ConstructUnit(rebelIdx, (short)this.processor.PullLocalWord(0x2),
				(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
			this.processor.EmitLocalWord(0x6, newTroop);

			ushort copy = (ushort)((ushort)(0x600 * rebelIdx) + (ushort)(0xc * newTroop));
			this.processor.RegDest.Value16 = copy;

			this.processor.EmitGlobalByte((ushort)(copy + 0x81d4), (byte)(this.processor.PullGlobalByte((ushort)(troop + 0x81d4)) & 0xfe));
			this.processor.EmitGlobalByte((ushort)(copy + 0x81df), this.processor.PullGlobalByte((ushort)(troop + 0x81df)));

			// The rest of the stack changes sides too, on the unit's tile, with its status and (homed elsewhere) home city
			while (next != -1 && (ushort)next != troopIdx)
			{
				this.processor.RegSource.Value16 = playerUnits;

				ushort other = (ushort)((ushort)(0xc * next) + playerUnits);
				this.processor.RegDest.Value16 = other;

				short following = (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81de));
				short kind = (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d7));

				this.processor.EmitLocalWord(0x20, (ushort)following);
				this.processor.EmitLocalWord(0x2, (ushort)kind);

				this.owner.UnitOps.EliminateUnit(participantIdx, next);

				this.processor.EmitLocalWord(0x46, troop);

				this.owner.MapOps.MapAssignCityOwner(rebelIdx, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				if (kind != -1)
				{
					this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

					ushort joined = this.owner.UnitOps.ConstructUnit(rebelIdx, kind,
						(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
					this.processor.EmitLocalWord(0x6, joined);

					ushort joinedCopy = (ushort)((ushort)(0x600 * rebelIdx) + (ushort)(0xc * joined));
					this.processor.EmitLocalWord(0x48, joinedCopy);

					this.processor.EmitGlobalByte((ushort)(joinedCopy + 0x81d4), (byte)(this.processor.PullGlobalByte((ushort)(troop + 0x81d4)) & 0xfe));
					this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81df)));

					if (this.owner.World.Towns[this.processor.PullGlobalByte((ushort)(other + 0x81df))].ParticipantIdx != homeOwner)
					{
						this.processor.EmitGlobalByte((ushort)(joinedCopy + 0x81df), this.processor.PullGlobalByte((ushort)(troop + 0x81df)));
					}
				}

				next = following;
				this.processor.EmitLocalWord(0x1a, (ushort)next);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="playerID"></param>
		public void AssignCityOwner(short townIdx, short participantIdx)
		{
			this.processor.Journal.OpenScope($"AssignCityOwner({townIdx}, {participantIdx})");

			// function body
			this.owner.World.Towns[townIdx].ParticipantIdx = participantIdx;

			this.owner.MapOps.MapAssignCityOwner(participantIdx, 
				this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.owner.World.Participants[participantIdx].GrandTownExtent += this.owner.World.Towns[townIdx].TrueExtent;

			// Far return
			this.processor.Journal.CloseScope("AssignCityOwner");
		}

		/// <summary>
		/// The most central city of the player: the one with the smallest sum of distances to all its cities (a signed word sum,
		/// the first one of equal sums; sums of 0x7fff and more never count)
		/// </summary>
		/// <returns>City index, 0xffff when none (also in AX)</returns>
		public ushort ObtainDistanceToCapital(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainDistanceToCapital({participantIdx})");

			short smallestSum = 0x7fff;
			ushort central = 0xffff;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ParticipantIdx != participantIdx || town.ConditionMarker == 0xff)
					continue;

				short sum = 0;

				for (int otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					City other = this.owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == participantIdx && other.ConditionMarker != 0xff)
					{
						sum = (short)(sum + this.owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y));
					}
				}

				if (sum < smallestSum)
				{
					smallestSum = sum;
					central = (ushort)townIdx;
				}
			}

			this.processor.RegAcc.Value16 = central;

			this.processor.Journal.CloseScope("ObtainDistanceToCapital");

			return central;
		}
	}
}
