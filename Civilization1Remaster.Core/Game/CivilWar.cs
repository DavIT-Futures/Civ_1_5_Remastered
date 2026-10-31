using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 15.
	/// </summary>
	public class CivilWar : GameComponent, ICivilWar
	{
		public CivilWar(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public ushort DivideCivilization(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"DivideCivilization({participantIdx})");

			ushort result = SplitCivilization(participantIdx);

			this.Processor.Journal.CloseScope("DivideCivilization");

			return result;
		}

		/// <inheritdoc/>
		public void AssignCityOwner(short townIdx, short participantIdx)
		{
			this.Processor.Journal.OpenScope($"AssignCityOwner({townIdx}, {participantIdx})");

			this.Owner.World.Towns[townIdx].ParticipantIdx = participantIdx;

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, 
				this.Owner.World.Towns[townIdx].Place.X, this.Owner.World.Towns[townIdx].Place.Y);

			this.Owner.World.Participants[participantIdx].GrandTownExtent += this.Owner.World.Towns[townIdx].TrueExtent;

			this.Processor.Journal.CloseScope("AssignCityOwner");
		}

		/// <inheritdoc/>
		public ushort ComputeDistanceToCapital(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"ObtainDistanceToCapital({participantIdx})");

			short smallestSum = 0x7fff;
			ushort central = 0xffff;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ParticipantIdx != participantIdx || town.ConditionMarker == 0xff)
					continue;

				short sum = 0;

				for (int otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					City other = this.Owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == participantIdx && other.ConditionMarker != 0xff)
					{
						sum = (short)(sum + this.Owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y));
					}
				}

				if (sum < smallestSum)
				{
					smallestSum = sum;
					central = (ushort)townIdx;
				}
			}


			this.Processor.Journal.CloseScope("ObtainDistanceToCapital");

			return central;
		}

		/// <summary>The civil war (see DivideCivilization)</summary>
		private ushort SplitCivilization(short participantIdx)
		{
			short rebelIdx = -1;

			for (ushort slotIdx = 1; slotIdx < 8; slotIdx++)
			{
				if (this.Owner.World.Participants[slotIdx].TroopTally == 0 && this.Owner.World.Participants[slotIdx].TownTally == 0)
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
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				ushort continent = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
				continentShares[continent] = (ushort)(continentShares[continent] + town.TrueExtent);

				if (!town.Has(CityStructure.Palace))
					continue;

				if (capitalContinent != 0xffff && (ushort)this.Owner.World.Participants[participantIdx].HorizBegin != (ushort)(short)town.Place.X)
					continue;

				capitalContinent = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);
				capitalX = (short)town.Place.X;
				capitalY = (short)town.Place.Y;
				oldCapital = townIdx;
			}

			if (capitalContinent == 0xffff)
				return 0;

			if (!RaiseRebelCivilization(participantIdx, rebelIdx))
				return 0;

			DivideContinents(participantIdx, rebelIdx, continentShares, capitalContinent, capitalX, capitalY);

			this.Owner.World.Participants[participantIdx].GrandTownExtent += this.Owner.World.Participants[rebelIdx].GrandTownExtent;

			TransferRebelUnits(participantIdx, rebelIdx);

			bool classic = this.Owner.TurnHooks.ClassicSchism;

			ushort capitalIdx = ComputeDistanceToCapital(rebelIdx);

			// The new civilization without a city gets no capital (ClassicSchism: Towns[0xffff] throws)
			if (classic || capitalIdx != 0xffff)
			{
				this.Owner.World.Towns[capitalIdx].StructureMarkers0 |= 1;
				this.Owner.World.Participants[rebelIdx].HorizBegin = (short)this.Owner.World.Towns[capitalIdx].Place.X;
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx, (short)MapViewHoriz, (short)MapViewVert);
			}
			else
			{
				this.Owner.World.Towns[oldCapital].ParticipantIdx = -1;

				ushort newCapital = ComputeDistanceToCapital(participantIdx);

				// Without another city the old capital stays (ClassicSchism: Towns[0xffff] throws)
				if (!classic && newCapital == 0xffff)
					newCapital = oldCapital;


				this.Owner.World.Towns[newCapital].StructureMarkers0 |= 1;
				this.Owner.World.Participants[participantIdx].HorizBegin = (short)this.Owner.World.Towns[newCapital].Place.X;
				this.Owner.World.Towns[oldCapital].ParticipantIdx = participantIdx;
			}

			return 1;
		}

		/// <summary>
		/// The new civilization in the slot (CreateCivilization; false when it fails): its leader, names, the message, half
		/// the treasury and might, research and government, war with the player
		/// </summary>
		private bool RaiseRebelCivilization(short participantIdx, short rebelIdx)
		{
			bool barbarians = rebelIdx > (short)(this.Owner.World.BotRivalTally + 1);

			this.Owner.World.LiveRealms &= (short)~(1 << (byte)rebelIdx);

			if (this.Owner.NewGameWizard.CreateCivilization(rebelIdx) == 0)
				return false;

			this.Owner.World.LiveRealms |= (short)(1 << (byte)rebelIdx);

			Player rebel = this.Owner.World.Participants[rebelIdx];
			Player player = this.Owner.World.Participants[participantIdx];
			bool newLeader = barbarians;

			if (!barbarians)
			{
				ushort leader = (ushort)rebel.ChiefVisuals;

				newLeader = ((rebelIdx != 7 ? 1 : 0) & leader) != 0 || (short)leader > 0xf;
			}

			if (newLeader)
			{
				rebel.ChiefVisuals = (short)(this.Owner.Libc.Dice.DrawInt(2) != 0 ? rebelIdx : rebelIdx + 8);

				if ((short)rebel.ChiefVisuals >= 8)
					this.Owner.World.ParticipantIdentMarkers |= (short)(1 << (byte)rebelIdx);
			}
			else
			{
				rebel.ChiefVisuals ^= 8;
			}

			ushort names = (ushort)(rebelIdx << 1);

			ushort leaderNames = (ushort)(0x3a * rebel.ChiefVisuals);

			this.Owner.Names.Set(LeaderNameAt(names), LeaderTextAt(leaderNames, LeaderTable.Name));

			this.Owner.Names.Set(CivilizationNameAt(names), LeaderText(rebel.ChiefVisuals, LeaderTable.Civilization));

			this.Owner.Words.Set(LeaderText(rebel.ChiefVisuals, LeaderTable.CivilizationPlural));

			if (this.Owner.Words[0] == 0)
			{
				Message(CivilizationNameAt(names))
					.Text("s");
			}

			this.Owner.Names.Set(CivilizationPluralAt(names), this.Owner.Words.ToString());

			this.Owner.Words.Clear();

			this.Owner.Codes.Set(0, CivilizationName((ushort)participantIdx));

			this.Owner.Codes.Set(1, CivilizationNameAt(names));

			this.Owner.BlockText.ShowTextDialog(participantIdx == this.Owner.World.PersonParticipantIdx ? (ushort)0x4a18 : (ushort)0x4a20);

			DialogStyle = 0x5;

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

			rebel.Gold = (short)(player.Gold / 2);
			player.Gold -= rebel.Gold;
			rebel.MartialMight = (short)(player.MartialMight / 2);
			rebel.InquiryHeadway = player.InquiryHeadway;
			rebel.FoundScienceTally = player.FoundScienceTally;
			rebel.RegimeKind = player.RegimeKind;

			// The new civilization knows the player's technologies (ClassicSchism: they go back onto the player himself, like
			// in the translated code, and the new civilization gets none)
			bool classic = this.Owner.TurnHooks.ClassicSchism;
			Player learner = classic ? player : rebel;

			for (ushort scienceIdx = 0; scienceIdx < 5; scienceIdx++)
			{
				learner.FoundScienceMarkers[scienceIdx] = player.FoundScienceMarkers[scienceIdx];
			}

			rebel.TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally - 8);

			// War: the player's relation to the new civilization and the new civilization's to the player (ClassicSchism: both
			// flags go into the player's relation, like in the translated code)
			player.Relations[rebelIdx] |= 1;

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

				continentShares[continent] = 1;
			}

			if ((short)(given << 1) > rest && given <= rest)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					City town = this.Owner.World.Towns[townIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
						continue;

					ushort townContinent = this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

					if (continentShares[townContinent] == 2)
						AssignCityOwner((short)townIdx, rebelIdx);
				}

				return;
			}

			given = 0;
			ushort farthestIdx = 0;

			while (CPU.SignedQuotient((short)(9 * given), (short)(SchismShareDc6a + 3)) < this.Owner.World.Participants[participantIdx].GrandTownExtent)
			{
				short farthest = 1;

				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					City town = this.Owner.World.Towns[townIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
						continue;

					short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(town.Place.X, capitalX, town.Place.Y, capitalY);

					if (distance > farthest)
					{
						farthest = distance;
						farthestIdx = townIdx;
					}
				}

				// Without a farther city no city is left to give (ClassicSchism: the one of the last round is taken again, or
				// city 0 of anybody, like in the translated code)
				if (!this.Owner.TurnHooks.ClassicSchism && farthest == 1)
					break;


				given += this.Owner.World.Towns[farthestIdx].TrueExtent;

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

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				short unitKind = (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind);

				if ((this.Owner.MapOps.ObtainTileImprovements((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert)) & 1) != 0)
					TransferUnitInCity(participantIdx, rebelIdx, troopIdx, troop);
				else
					TransferUnitOutside(participantIdx, rebelIdx, troopIdx, troop, unitKind);
			}
		}

		/// <summary>A unit of the player in a city (see TransferRebelUnits)</summary>
		private void TransferUnitInCity(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop)
		{
			ushort townIdx = this.Owner.Helpers.FindCityAtPosition((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));
			short unitKind = (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind);

			ushort townRecord = (ushort)(0x1c * townIdx);

			byte homeIdx = this.Troops.PullAt(troop, UnitMemory.BaseTown);

			// AX holds the home city's offset like in the original (for a unit without a home, 0xff, see HomeOwner)

			if (HomeOwner(participantIdx, homeIdx) != this.Owner.World.Towns[townIdx].ParticipantIdx)
			{
				this.Owner.UnitOps.DestroyUnit(participantIdx, (short)troopIdx);

				this.Owner.MapOps.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

				// The unit goes to the city's owner (ClassicSchism: to the player in the raw byte at 0x1c * city of the data
				// segment, like in the translated code, which lost the address of the city table), with its kind (ClassicSchism:
				// the kind read after DestroyUnit, -1, like in the original: the unit vanishes, C# throws)
				bool classic = this.Owner.TurnHooks.ClassicSchism;
				short cityOwner = classic ? (sbyte)this.Processor.PullGlobalByte(townRecord) : this.Owner.World.Towns[townIdx].ParticipantIdx;
				short kind = classic ? (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind) : unitKind;

				this.Owner.UnitOps.CreateUnit(cityOwner, kind,
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));
				return;
			}

			if ((ushort)this.Owner.World.Towns[townIdx].ParticipantIdx != (ushort)rebelIdx)
				return;

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)troopIdx);

			this.Owner.MapOps.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

			ushort newTroop = this.Owner.UnitOps.CreateUnit(rebelIdx, unitKind,
				(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

			ushort copy = (ushort)((ushort)(0xc * newTroop) + (ushort)(0x600 * rebelIdx));

			this.Troops.AssignAt(copy, UnitMemory.Status, (byte)(this.Troops.PullAt(troop, UnitMemory.Status) & 0xfe));
		}

		/// <summary>A unit of the player outside the cities, with its stack (see TransferRebelUnits)</summary>
		private void TransferUnitOutside(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop, short unitKind)
		{
			ushort playerUnits = (ushort)(0x600 * participantIdx);

			// AX holds the home city's offset like in the original (for a unit without a home, 0xff, see HomeOwner)

			short homeOwner = HomeOwner(participantIdx, this.Troops.PullAt(troop, UnitMemory.BaseTown));
			short next = (sbyte)this.Troops.PullAt(troop, UnitMemory.FollowingInStack);

			if ((ushort)homeOwner != (ushort)rebelIdx)
			{
				// The stack's units homed elsewhere get the unit's home city
				while (next != -1 && (ushort)next != troopIdx)
				{
					ushort other = (ushort)((ushort)(0xc * next) + playerUnits);

					short following = (sbyte)this.Troops.PullAt(other, UnitMemory.FollowingInStack);

					if (HomeOwner(participantIdx, this.Troops.PullAt(other, UnitMemory.BaseTown)) != homeOwner)
					{
						this.Troops.AssignAt(other, UnitMemory.BaseTown, this.Troops.PullAt(((ushort)(0xc * troopIdx) + playerUnits), UnitMemory.BaseTown));
					}

					next = following;
				}

				return;
			}

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)troopIdx);

			this.Owner.MapOps.MapAssignCityOwner(rebelIdx, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

			this.Owner.MapOps.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

			ushort newTroop = this.Owner.UnitOps.CreateUnit(rebelIdx, unitKind,
				(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

			ushort copy = (ushort)((ushort)(0x600 * rebelIdx) + (ushort)(0xc * newTroop));

			this.Troops.AssignAt(copy, UnitMemory.Status, (byte)(this.Troops.PullAt(troop, UnitMemory.Status) & 0xfe));
			this.Troops.AssignAt(copy, UnitMemory.BaseTown, this.Troops.PullAt(troop, UnitMemory.BaseTown));

			// The rest of the stack changes sides too, on the unit's tile, with its status and (homed elsewhere) home city
			while (next != -1 && (ushort)next != troopIdx)
			{
				ushort other = (ushort)((ushort)(0xc * next) + playerUnits);

				short following = (sbyte)this.Troops.PullAt(other, UnitMemory.FollowingInStack);
				short kind = (sbyte)this.Troops.PullAt(other, UnitMemory.Kind);

				this.Owner.UnitOps.DestroyUnit(participantIdx, next);

				this.Owner.MapOps.MapAssignCityOwner(rebelIdx, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

				if (kind != -1)
				{
					this.Owner.MapOps.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

					ushort joined = this.Owner.UnitOps.CreateUnit(rebelIdx, kind,
						(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

					ushort joinedCopy = (ushort)((ushort)(0x600 * rebelIdx) + (ushort)(0xc * joined));

					this.Troops.AssignAt(joinedCopy, UnitMemory.Status, (byte)(this.Troops.PullAt(troop, UnitMemory.Status) & 0xfe));

					if (HomeOwner(participantIdx, this.Troops.PullAt(other, UnitMemory.BaseTown)) != homeOwner)
					{
						this.Troops.AssignAt(joinedCopy, UnitMemory.BaseTown, this.Troops.PullAt(troop, UnitMemory.BaseTown));
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
			HomeCityRules.HasHome(homeIdx, this.Owner.TurnHooks.ClassicSchismHomes) ?
				this.Owner.World.Towns[homeIdx].ParticipantIdx : participantIdx;
	}
}
