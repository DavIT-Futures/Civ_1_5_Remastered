namespace Civ_1_5_Remastered.Core.Game.Diplomacy
{
	public class CivilWar : GameComponent, ICivilWar
	{
		public CivilWar(IGameEngine owner)
			: base(owner)
		{
		}

		public ushort DivideCivilization(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DivideCivilization({participantIndex})");

			ushort result = SplitCivilization(participantIndex);

			return result;
		}

		public void AssignCityOwner(short townIndex, short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"AssignCityOwner({townIndex}, {participantIndex})");

			this.Owner.World.Towns[townIndex].ParticipantIndex = participantIndex;

			this.Owner.MapOperations.MapAssignCityOwner(participantIndex,
				this.Owner.World.Towns[townIndex].Place.X, this.Owner.World.Towns[townIndex].Place.Y);

			this.Owner.World.Participants[participantIndex].GrandTownExtent += this.Owner.World.Towns[townIndex].TrueExtent;
		}

		public ushort ComputeDistanceToCapital(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainDistanceToCapital({participantIndex})");

			short smallestSum = 0x7fff;
			ushort central = 0xffff;

			for (int townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ParticipantIndex != participantIndex || town.ConditionMarker == 0xff)
				{
					continue;
				}

				short sum = 0;

				for (int otherIndex = 0; otherIndex < 0x80; otherIndex++)
				{
					City other = this.Owner.World.Towns[otherIndex];

					if (other.ParticipantIndex == participantIndex && other.ConditionMarker != 0xff)
					{
						sum = (short)(sum + this.Owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y));
					}
				}

				if (sum < smallestSum)
				{
					smallestSum = sum;
					central = (ushort)townIndex;
				}
			}

			return central;
		}

		private ushort SplitCivilization(short participantIndex)
		{
			short rebelIndex = -1;

			for (ushort slotIndex = 1; slotIndex < 8; slotIndex++)
			{
				if (this.Owner.World.Participants[slotIndex].TroopTally == 0 && this.Owner.World.Participants[slotIndex].TownTally == 0)
				{
					rebelIndex = (short)slotIndex;
					break;
				}
			}

			if (rebelIndex == -1)
			{
				return 0;
			}

			ushort[] continentShares = new ushort[0x10];
			ushort capitalContinent = 0xffff;
			short capitalHorizontal = 0;
			short capitalVertical = 0;
			ushort oldCapital = 0;

			for (ushort townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
				{
					continue;
				}

				ushort continent = this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y);
				continentShares[continent] = (ushort)(continentShares[continent] + town.TrueExtent);

				if (!town.Has(CityStructure.Palace))
				{
					continue;
				}

				if (capitalContinent != 0xffff && (ushort)this.Owner.World.Participants[participantIndex].HorizontalBegin != (ushort)(short)town.Place.X)
				{
					continue;
				}

				capitalContinent = this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y);
				capitalHorizontal = (short)town.Place.X;
				capitalVertical = (short)town.Place.Y;
				oldCapital = townIndex;
			}

			if (capitalContinent == 0xffff)
			{
				return 0;
			}

			if (!RaiseRebelCivilization(participantIndex, rebelIndex))
			{
				return 0;
			}

			DivideContinents(participantIndex, rebelIndex, continentShares, capitalContinent, capitalHorizontal, capitalVertical);

			this.Owner.World.Participants[participantIndex].GrandTownExtent += this.Owner.World.Participants[rebelIndex].GrandTownExtent;

			TransferRebelUnits(participantIndex, rebelIndex);

			ushort capitalIndex = ComputeDistanceToCapital(rebelIndex);

			if (capitalIndex != 0xffff)
			{
				this.Owner.World.Towns[capitalIndex].StructureMarkersLow |= 1;
				this.Owner.World.Participants[rebelIndex].HorizontalBegin = (short)this.Owner.World.Towns[capitalIndex].Place.X;
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.MapOperations.FocusMapView(this.Owner.World.PersonParticipantIndex, (short)MapView.Horizontal, (short)MapView.Vertical);
			}
			else
			{
				this.Owner.World.Towns[oldCapital].ParticipantIndex = -1;

				ushort newCapital = ComputeDistanceToCapital(participantIndex);

				if (newCapital == 0xffff)
				{
					newCapital = oldCapital;
				}

				this.Owner.World.Towns[newCapital].StructureMarkersLow |= 1;
				this.Owner.World.Participants[participantIndex].HorizontalBegin = (short)this.Owner.World.Towns[newCapital].Place.X;
				this.Owner.World.Towns[oldCapital].ParticipantIndex = participantIndex;
			}

			return 1;
		}

		private bool RaiseRebelCivilization(short participantIndex, short rebelIndex)
		{
			bool barbarians = rebelIndex > (short)(this.Owner.World.ComputerRivalTally + 1);

			this.Owner.World.LiveRealms &= (short)~(1 << (byte)rebelIndex);

			if (this.Owner.NewGameWizard.CreateCivilization(rebelIndex) == 0)
			{
				return false;
			}

			this.Owner.World.LiveRealms |= (short)(1 << (byte)rebelIndex);

			Player rebel = this.Owner.World.Participants[rebelIndex];
			Player player = this.Owner.World.Participants[participantIndex];
			bool newLeader = barbarians;

			if (!barbarians)
			{
				ushort leader = (ushort)rebel.ChiefVisuals;

				newLeader = ((rebelIndex != 7 ? 1 : 0) & leader) != 0 || (short)leader > 0xf;
			}

			if (newLeader)
			{
				rebel.ChiefVisuals = (short)(this.Owner.Runtime.Dice.DrawInteger(2) != 0 ? rebelIndex : rebelIndex + 8);

				if ((short)rebel.ChiefVisuals >= 8)
				{
					this.Owner.World.ParticipantIdentifierMarkers |= (short)(1 << (byte)rebelIndex);
				}
			}
			else
			{
				rebel.ChiefVisuals ^= 8;
			}

			ushort names = (ushort)(rebelIndex << 1);

			ushort leaderNames = (ushort)(0x3a * rebel.ChiefVisuals);

			this.Owner.Names.Set(Tables.LeaderNameAt(names), GameTables.LeaderTextAt(leaderNames, LeaderTable.Name));

			this.Owner.Names.Set(Tables.CivilizationNameAt(names), GameTables.LeaderText(rebel.ChiefVisuals, LeaderTable.Civilization));

			this.Owner.Words.Set(GameTables.LeaderText(rebel.ChiefVisuals, LeaderTable.CivilizationPlural));

			if (this.Owner.Words[0] == 0)
			{
				Message(Tables.CivilizationNameAt(names))
					.Text("s");
			}

			this.Owner.Names.Set(Tables.CivilizationPluralAt(names), this.Owner.Words.ToString());

			this.Owner.Words.Clear();

			this.Owner.Codes.Set(0, Tables.CivilizationName((ushort)participantIndex));

			this.Owner.Codes.Set(1, Tables.CivilizationNameAt(names));

			this.Owner.BlockText.ShowTextDialog(participantIndex == this.Owner.World.PersonParticipantIndex ? (ushort)0x4a18 : (ushort)0x4a20);

			Screen.DialogStyle = 0x5;

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

			rebel.Gold = (short)(player.Gold / 2);
			player.Gold -= rebel.Gold;
			rebel.MartialMight = (short)(player.MartialMight / 2);
			rebel.InquiryHeadway = player.InquiryHeadway;
			rebel.FoundScienceTally = player.FoundScienceTally;
			rebel.RegimeKind = player.RegimeKind;

			for (ushort scienceIndex = 0; scienceIndex < 5; scienceIndex++)
			{
				rebel.FoundScienceMarkers[scienceIndex] = player.FoundScienceMarkers[scienceIndex];
			}

			rebel.TouchParticipantTimeLeft = (short)(this.Owner.World.RoundTally - 8);

			player.Relations[rebelIndex] |= 1;
			rebel.Relations[participantIndex] |= 1;
			DiplomacyRules.BeginVendetta(this.Owner.World.Participants, rebelIndex, participantIndex, DiplomacyRules.CivilWarGrudge);

			return true;
		}

		private void DivideContinents(short participantIndex, short rebelIndex, ushort[] continentShares, ushort capitalContinent, short capitalHorizontal, short capitalVertical)
		{
			short rest = (short)continentShares[capitalContinent];
			short given = 0;

			for (ushort continent = 0; continent < 0x10; continent++)
			{
				short size = (short)continentShares[continent];

				if (size == 0)
				{
					continue;
				}

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
				for (ushort townIndex = 0; townIndex < 0x80; townIndex++)
				{
					City town = this.Owner.World.Towns[townIndex];

					if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
					{
						continue;
					}

					ushort townContinent = this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y);

					if (continentShares[townContinent] == 2)
					{
						AssignCityOwner((short)townIndex, rebelIndex);
					}
				}

				return;
			}

			given = 0;
			ushort farthestIndex = 0;

			while (CentralProcessor.SignedQuotient((short)(9 * given), (short)(Scoring.SchismShare + 3)) < this.Owner.World.Participants[participantIndex].GrandTownExtent)
			{
				short farthest = 1;

				for (ushort townIndex = 0; townIndex < 0x80; townIndex++)
				{
					City town = this.Owner.World.Towns[townIndex];

					if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
					{
						continue;
					}

					short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(town.Place.X, capitalHorizontal, town.Place.Y, capitalVertical);

					if (distance > farthest)
					{
						farthest = distance;
						farthestIndex = townIndex;
					}
				}

				if (farthest == 1)
				{
					break;
				}

				given += this.Owner.World.Towns[farthestIndex].TrueExtent;

				AssignCityOwner((short)farthestIndex, rebelIndex);
			}
		}

		private void TransferRebelUnits(short participantIndex, short rebelIndex)
		{
			for (ushort troopIndex = 0; troopIndex < 0x80; troopIndex++)
			{
				ushort troop = (ushort)((ushort)(0x600 * participantIndex) + (ushort)(0xc * troopIndex));

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
				{
					continue;
				}

				short unitKind = (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind);

				if ((this.Owner.MapOperations.ObtainTileImprovements((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical)) & 1) != 0)
				{
					TransferUnitInCity(participantIndex, rebelIndex, troopIndex, troop);
				}
				else
				{
					TransferUnitOutside(participantIndex, rebelIndex, troopIndex, troop, unitKind);
				}
			}
		}

		private void TransferUnitInCity(short participantIndex, short rebelIndex, ushort troopIndex, ushort troop)
		{
			ushort townIndex = this.Owner.Helpers.FindCityAtPosition((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));
			short unitKind = (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind);

			byte homeIndex = this.Troops.PullAt(troop, UnitMemory.BaseTown);

			if (HomeOwner(participantIndex, homeIndex) != this.Owner.World.Towns[townIndex].ParticipantIndex)
			{
				this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)troopIndex);

				this.Owner.MapOperations.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

				this.Owner.UnitOperations.CreateUnit(this.Owner.World.Towns[townIndex].ParticipantIndex, unitKind,
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));
				return;
			}

			if ((ushort)this.Owner.World.Towns[townIndex].ParticipantIndex != (ushort)rebelIndex)
			{
				return;
			}

			this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)troopIndex);

			this.Owner.MapOperations.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

			ushort newTroop = this.Owner.UnitOperations.CreateUnit(rebelIndex, unitKind,
				(sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

			ushort copy = (ushort)((ushort)(0xc * newTroop) + (ushort)(0x600 * rebelIndex));

			this.Troops.AssignAt(copy, UnitMemory.Status, (byte)(this.Troops.PullAt(troop, UnitMemory.Status) & 0xfe));
		}

		private void TransferUnitOutside(short participantIndex, short rebelIndex, ushort troopIndex, ushort troop, short unitKind)
		{
			ushort playerUnits = (ushort)(0x600 * participantIndex);

			short homeOwner = HomeOwner(participantIndex, this.Troops.PullAt(troop, UnitMemory.BaseTown));
			short next = (sbyte)this.Troops.PullAt(troop, UnitMemory.FollowingInStack);

			if ((ushort)homeOwner != (ushort)rebelIndex)
			{
				while (next != -1 && (ushort)next != troopIndex)
				{
					ushort other = (ushort)((ushort)(0xc * next) + playerUnits);

					short following = (sbyte)this.Troops.PullAt(other, UnitMemory.FollowingInStack);

					if (HomeOwner(participantIndex, this.Troops.PullAt(other, UnitMemory.BaseTown)) != homeOwner)
					{
						this.Troops.AssignAt(other, UnitMemory.BaseTown, this.Troops.PullAt((ushort)(0xc * troopIndex) + playerUnits, UnitMemory.BaseTown));
					}

					next = following;
				}

				return;
			}

			this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)troopIndex);

			this.Owner.MapOperations.MapAssignCityOwner(rebelIndex, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

			this.Owner.MapOperations.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

			ushort newTroop = this.Owner.UnitOperations.CreateUnit(rebelIndex, unitKind,
				(sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

			ushort copy = (ushort)((ushort)(0x600 * rebelIndex) + (ushort)(0xc * newTroop));

			this.Troops.AssignAt(copy, UnitMemory.Status, (byte)(this.Troops.PullAt(troop, UnitMemory.Status) & 0xfe));
			this.Troops.AssignAt(copy, UnitMemory.BaseTown, this.Troops.PullAt(troop, UnitMemory.BaseTown));

			while (next != -1 && (ushort)next != troopIndex)
			{
				ushort other = (ushort)((ushort)(0xc * next) + playerUnits);

				short following = (sbyte)this.Troops.PullAt(other, UnitMemory.FollowingInStack);
				short kind = (sbyte)this.Troops.PullAt(other, UnitMemory.Kind);

				this.Owner.UnitOperations.DestroyUnit(participantIndex, next);

				this.Owner.MapOperations.MapAssignCityOwner(rebelIndex, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

				if (kind != -1)
				{
					this.Owner.MapOperations.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

					ushort joined = this.Owner.UnitOperations.CreateUnit(rebelIndex, kind,
						(sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical));

					ushort joinedCopy = (ushort)((ushort)(0x600 * rebelIndex) + (ushort)(0xc * joined));

					this.Troops.AssignAt(joinedCopy, UnitMemory.Status, (byte)(this.Troops.PullAt(troop, UnitMemory.Status) & 0xfe));

					if (HomeOwner(participantIndex, this.Troops.PullAt(other, UnitMemory.BaseTown)) != homeOwner)
					{
						this.Troops.AssignAt(joinedCopy, UnitMemory.BaseTown, this.Troops.PullAt(troop, UnitMemory.BaseTown));
					}
				}

				next = following;
			}
		}

		private short HomeOwner(short participantIndex, byte homeIndex) =>
			HomeCityRules.HasHome(homeIndex) ? this.Owner.World.Towns[homeIndex].ParticipantIndex : participantIndex;
	}
}
