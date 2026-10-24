using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
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
		/// (with their stacks). Both get a capital (the human player's map moves home). Returns 1.
		/// </summary>
		public ushort DivideCivilization(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DivideCivilization({participantIdx})");

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

		/// <summary>The civil war (see DivideCivilization)</summary>
		private ushort SplitCivilization(short participantIdx)
		{
			short rebelIdx = -1;

			for (ushort slotIdx = 1; slotIdx < 8; slotIdx++)
			{
				if (this.owner.World.Participants[slotIdx].TroopTally == 0 && this.owner.World.Participants[slotIdx].TownTally == 0)
				{
					rebelIdx = (short)slotIdx;
					break;
				}
			}

			if (rebelIdx == -1)
				return 0;

			ushort[] continentShares = new ushort[0x10];
			ushort capitalContinent = 0xffff;
			short capitalX = 0;
			short capitalY = 0;
			ushort oldCapital = 0;

			for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				ushort continent = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
				this.processor.RegDest.Value16 = (ushort)(continent << 1);
				continentShares[continent] = (ushort)(continentShares[continent] + town.TrueExtent);

				if (!town.Has(CityStructure.Palace))
					continue;

				if (capitalContinent != 0xffff && (ushort)this.owner.World.Participants[participantIdx].HorizBegin != (ushort)(short)town.Place.X)
					continue;

				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

				capitalContinent = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
				capitalX = (short)town.Place.X;
				capitalY = (short)town.Place.Y;
				oldCapital = townIdx;
			}

			if (capitalContinent == 0xffff)
				return 0;

			if (!RaiseRebelCivilization(participantIdx, rebelIdx))
				return 0;

			DivideContinents(participantIdx, rebelIdx, continentShares, capitalContinent, capitalX, capitalY);

			this.owner.World.Participants[participantIdx].GrandTownExtent += this.owner.World.Participants[rebelIdx].GrandTownExtent;

			TransferRebelUnits(participantIdx, rebelIdx);

			bool classic = this.owner.TurnHooks.ClassicSchism;

			ushort capitalIdx = ObtainDistanceToCapital(rebelIdx);
			this.processor.RegAcc.Value16 = (ushort)(0x1c * capitalIdx);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			// The new civilization without a city gets no capital (ClassicSchism: Towns[0xffff] throws)
			if (classic || capitalIdx != 0xffff)
			{
				this.owner.World.Towns[capitalIdx].StructureMarkers0 |= 1;
				this.owner.World.Participants[rebelIdx].HorizBegin = (short)this.owner.World.Towns[capitalIdx].Place.X;
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, (short)MapViewHoriz, (short)MapViewVert);
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)(0x1c * oldCapital);
				this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.Towns[oldCapital].ParticipantIdx = -1;

				this.processor.StackWord(0); // stack management - push return segment, ignored
				this.processor.StackWord(0x084f); // stack management - push return offset
				this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

				ushort newCapital = ObtainDistanceToCapital(participantIdx);

				// Without another city the old capital stays (ClassicSchism: Towns[0xffff] throws)
				if (!classic && newCapital == 0xffff)
					newCapital = oldCapital;

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

			this.owner.Libc.CopyText(CodeText1, CivilizationName((ushort)participantIdx));

			this.owner.Libc.CopyText(CodeText2, this.processor.PullGlobalWord((ushort)(names + 0x1982)));

			this.owner.BlockText.DisplayTextDialog(participantIdx == this.owner.World.PersonParticipantIdx ? (ushort)0x4a18 : (ushort)0x4a20);

			DialogStyle = 0x5;

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			rebel.Gold = (short)(player.Gold / 2);
			player.Gold -= rebel.Gold;
			rebel.MartialMight = (short)(player.MartialMight / 2);
			rebel.InquiryHeadway = player.InquiryHeadway;
			rebel.FoundScienceTally = player.FoundScienceTally;
			rebel.RegimeKind = player.RegimeKind;

			// The new civilization knows the player's technologies (ClassicSchism: they go back onto the player himself, like
			// in the translated code, and the new civilization gets none)
			bool classic = this.owner.TurnHooks.ClassicSchism;
			Player learner = classic ? player : rebel;

			for (ushort scienceIdx = 0; scienceIdx < 5; scienceIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(scienceIdx << 1);
				learner.FoundScienceMarkers[scienceIdx] = player.FoundScienceMarkers[scienceIdx];
			}

			this.processor.RegSource.Value16 = names;

			rebel.TouchParticipantTimeleft = (short)(this.owner.World.RoundTally - 8);

			// War: the player's relation to the new civilization and the new civilization's to the player (ClassicSchism: both
			// flags go into the player's relation, like in the translated code)
			player.Relations[rebelIdx] |= 1;

			this.processor.RegDest.Value16 = (ushort)(rebelIdx << 4);

			if (classic)
				player.Relations[rebelIdx] |= 9;
			else
				rebel.Relations[participantIdx] |= 9;

			return true;
		}

		/// <summary>
		/// The continents beyond the capital's go to the new civilization when they make between half and all of the rest,
		/// else the cities farthest from the capital until its share is reached (9 * size / (0xdc6a + 3) of the player's)
		/// </summary>
		private void DivideContinents(short participantIdx, short rebelIdx, ushort[] continentShares, ushort capitalContinent, short capitalX, short capitalY)
		{
			short rest = (short)continentShares[capitalContinent];
			short given = 0;

			for (ushort continent = 0; continent < 0x10; continent++)
			{
				this.processor.RegSource.Value16 = (ushort)(continent << 1);

				short size = (short)continentShares[continent];

				if (size == 0)
					continue;

				if (given <= rest && continent != capitalContinent)
				{
					given += size;
					continentShares[continent] = 2;
					continue;
				}

				if (continent != capitalContinent)
				{
					rest += size;
				}

				this.processor.RegSource.Value16 = (ushort)(continent << 1);
				continentShares[continent] = 1;
			}

			if ((short)(given << 1) > rest && given <= rest)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

					City town = this.owner.World.Towns[townIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
						continue;

					ushort townContinent = this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
					this.processor.RegDest.Value16 = (ushort)(townContinent << 1);

					if (continentShares[townContinent] == 2)
						AssignCityOwner((short)townIdx, rebelIdx);
				}

				return;
			}

			given = 0;
			ushort farthestIdx = 0;

			while (CPU.SignedQuotient((short)(9 * given), (short)(this.processor.PullGlobalWord(0xdc6a) + 3)) < this.owner.World.Participants[participantIdx].GrandTownExtent)
			{
				short farthest = 1;

				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

					City town = this.owner.World.Towns[townIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
						continue;

					short distance = (short)(ushort)this.owner.Helpers.ObtainDistance(town.Place.X, capitalX, town.Place.Y, capitalY);

					if (distance > farthest)
					{
						farthest = distance;
						farthestIdx = townIdx;
					}
				}

				// Without a farther city no city is left to give (ClassicSchism: the one of the last round is taken again, or
				// city 0 of anybody, like in the translated code)
				if (!this.owner.TurnHooks.ClassicSchism && farthest == 1)
					break;

				this.processor.RegAcc.Value16 = (ushort)(0x1c * farthestIdx);
				this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

				given += this.owner.World.Towns[farthestIdx].TrueExtent;

				AssignCityOwner((short)farthestIdx, rebelIdx);
			}
		}

		/// <summary>
		/// The player's units follow the cities: a unit in a city of another owner than its home city's owner goes to the
		/// city's owner (ClassicSchism: to the player in the raw byte at 0x1c * city, as a unit of kind -1); in a city of the new civilization
		/// homed there it changes sides; outside a city, a unit homed in a city of the new civilization changes sides with
		/// its stack, else the stack's units homed elsewhere get its home city
		/// </summary>
		private void TransferRebelUnits(short participantIdx, short rebelIdx)
		{
			for (ushort troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)((ushort)(0x600 * participantIdx) + (ushort)(0xc * troopIdx));
				this.processor.RegSource.Value16 = (ushort)(0x600 * participantIdx);

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.processor.RegSource.Value16 = troop;
				short unitKind = (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7));

				if ((this.owner.MapOps.ObtainTileImprovements((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6))) & 1) != 0)
					TransferUnitInCity(participantIdx, rebelIdx, troopIdx, troop);
				else
					TransferUnitOutside(participantIdx, rebelIdx, troopIdx, troop, unitKind);
			}
		}

		/// <summary>A unit of the player in a city (see TransferRebelUnits)</summary>
		private void TransferUnitInCity(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop)
		{
			ushort townIdx = this.owner.Helpers.LocateCityAtPosition((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
			short unitKind = (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7));

			ushort townRecord = (ushort)(0x1c * townIdx);
			this.processor.RegDest.Value16 = townRecord;

			byte homeIdx = this.processor.PullGlobalByte((ushort)(troop + 0x81df));

			// AX holds the home city's offset like in the original (for a unit without a home, 0xff, see HomeOwner)
			this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)homeIdx);

			if (HomeOwner(participantIdx, homeIdx) != this.owner.World.Towns[townIdx].ParticipantIdx)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, (short)troopIdx);

				this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				// The unit goes to the city's owner (ClassicSchism: to the player in the raw byte at 0x1c * city of the data
				// segment, like in the translated code, which lost the address of the city table), with its kind (ClassicSchism:
				// the kind read after EliminateUnit, -1, like in the original: the unit vanishes, C# throws)
				bool classic = this.owner.TurnHooks.ClassicSchism;
				short cityOwner = classic ? (sbyte)this.processor.PullGlobalByte(townRecord) : this.owner.World.Towns[townIdx].ParticipantIdx;
				short kind = classic ? (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) : unitKind;

				this.owner.UnitOps.ConstructUnit(cityOwner, kind,
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
				return;
			}

			if ((ushort)this.owner.World.Towns[townIdx].ParticipantIdx != (ushort)rebelIdx)
				return;

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)troopIdx);

			this.processor.RegSource.Value16 = troop;

			this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			ushort newTroop = this.owner.UnitOps.ConstructUnit(rebelIdx, unitKind,
				(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			ushort copy = (ushort)((ushort)(0xc * newTroop) + (ushort)(0x600 * rebelIdx));
			this.processor.RegDest.Value16 = (ushort)(0x600 * rebelIdx);

			this.processor.EmitGlobalByte((ushort)(copy + 0x81d4), (byte)(this.processor.PullGlobalByte((ushort)(troop + 0x81d4)) & 0xfe));
		}

		/// <summary>A unit of the player outside the cities, with its stack (see TransferRebelUnits)</summary>
		private void TransferUnitOutside(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop, short unitKind)
		{
			ushort playerUnits = (ushort)(0x600 * participantIdx);

			// AX holds the home city's offset like in the original (for a unit without a home, 0xff, see HomeOwner)
			this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81df)));

			short homeOwner = HomeOwner(participantIdx, this.processor.PullGlobalByte((ushort)(troop + 0x81df)));
			short next = (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81de));

			if ((ushort)homeOwner != (ushort)rebelIdx)
			{
				// The stack's units homed elsewhere get the unit's home city
				while (next != -1 && (ushort)next != troopIdx)
				{
					this.processor.RegSource.Value16 = playerUnits;

					ushort other = (ushort)((ushort)(0xc * next) + playerUnits);
					this.processor.RegDest.Value16 = other;

					short following = (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81de));
					this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81df)));

					if (HomeOwner(participantIdx, this.processor.PullGlobalByte((ushort)(other + 0x81df))) != homeOwner)
					{
						this.processor.EmitGlobalByte((ushort)(other + 0x81df), this.processor.PullGlobalByte((ushort)((ushort)(0xc * troopIdx) + playerUnits + 0x81df)));
					}

					next = following;
				}

				return;
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)troopIdx);

			this.owner.MapOps.MapAssignCityOwner(rebelIdx, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

			ushort newTroop = this.owner.UnitOps.ConstructUnit(rebelIdx, unitKind,
				(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

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

				this.owner.UnitOps.EliminateUnit(participantIdx, next);

				this.owner.MapOps.MapAssignCityOwner(rebelIdx, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				if (kind != -1)
				{
					this.owner.MapOps.TagTileOwned((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

					ushort joined = this.owner.UnitOps.ConstructUnit(rebelIdx, kind,
						(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

					ushort joinedCopy = (ushort)((ushort)(0x600 * rebelIdx) + (ushort)(0xc * joined));

					this.processor.EmitGlobalByte((ushort)(joinedCopy + 0x81d4), (byte)(this.processor.PullGlobalByte((ushort)(troop + 0x81d4)) & 0xfe));
					this.processor.RegAcc.Value16 = (ushort)(0x1c * (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81df)));

					if (HomeOwner(participantIdx, this.processor.PullGlobalByte((ushort)(other + 0x81df))) != homeOwner)
					{
						this.processor.EmitGlobalByte((ushort)(joinedCopy + 0x81df), this.processor.PullGlobalByte((ushort)(troop + 0x81df)));
					}
				}

				next = following;
			}
		}

		/// <summary>
		/// The owner of the home city of a unit of the player; a unit without a home city counts as homed with the player
		/// (HomeCityRules.HasHome)
		/// </summary>
		private short HomeOwner(short participantIdx, byte homeIdx) =>
			HomeCityRules.HasHome(homeIdx, this.owner.TurnHooks.ClassicSchismHomes) ?
				this.owner.World.Towns[homeIdx].ParticipantIdx : participantIdx;

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
