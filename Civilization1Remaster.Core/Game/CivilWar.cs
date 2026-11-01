namespace Civilization1Remaster.Core.Game
{
	public class CivilWar : GameComponent, ICivilWar
	{
		public CivilWar(GameEngine owner)
			: base(owner)
		{
		}

		public ushort DivideCivilization(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DivideCivilization({participantIdx})");

			ushort result = SplitCivilization(participantIdx);

			return result;
		}

		public void AssignCityOwner(short townIdx, short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"AssignCityOwner({townIdx}, {participantIdx})");

			this.Owner.World.Towns[townIdx].ParticipantIdx = participantIdx;

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, 
				this.Owner.World.Towns[townIdx].Place.X, this.Owner.World.Towns[townIdx].Place.Y);

			this.Owner.World.Participants[participantIdx].GrandTownExtent += this.Owner.World.Towns[townIdx].TrueExtent;
		}

		public ushort ComputeDistanceToCapital(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainDistanceToCapital({participantIdx})");

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

			return central;
		}

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

			ushort capitalIdx = ComputeDistanceToCapital(rebelIdx);

			if (capitalIdx != 0xffff)
			{
				this.Owner.World.Towns[capitalIdx].StructureMarkers0 |= 1;
				this.Owner.World.Participants[rebelIdx].HorizBegin = (short)this.Owner.World.Towns[capitalIdx].Place.X;
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx, (short)MapView.Horiz, (short)MapView.Vert);
			}
			else
			{
				this.Owner.World.Towns[oldCapital].ParticipantIdx = -1;

				ushort newCapital = ComputeDistanceToCapital(participantIdx);

				if (newCapital == 0xffff)
					newCapital = oldCapital;

				this.Owner.World.Towns[newCapital].StructureMarkers0 |= 1;
				this.Owner.World.Participants[participantIdx].HorizBegin = (short)this.Owner.World.Towns[newCapital].Place.X;
				this.Owner.World.Towns[oldCapital].ParticipantIdx = participantIdx;
			}

			return 1;
		}

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

			this.Owner.Codes.Set(0, Tables.CivilizationName((ushort)participantIdx));

			this.Owner.Codes.Set(1, Tables.CivilizationNameAt(names));

			this.Owner.BlockText.ShowTextDialog(participantIdx == this.Owner.World.PersonParticipantIdx ? (ushort)0x4a18 : (ushort)0x4a20);

			Screen.DialogStyle = 0x5;

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

			rebel.Gold = (short)(player.Gold / 2);
			player.Gold -= rebel.Gold;
			rebel.MartialMight = (short)(player.MartialMight / 2);
			rebel.InquiryHeadway = player.InquiryHeadway;
			rebel.FoundScienceTally = player.FoundScienceTally;
			rebel.RegimeKind = player.RegimeKind;

			for (ushort scienceIdx = 0; scienceIdx < 5; scienceIdx++)
			{
				rebel.FoundScienceMarkers[scienceIdx] = player.FoundScienceMarkers[scienceIdx];
			}

			rebel.TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally - 8);

			player.Relations[rebelIdx] |= 1;
			rebel.Relations[participantIdx] |= 9;

			return true;
		}

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

			while (CPU.SignedQuotient((short)(9 * given), (short)(Scoring.SchismShareDc6a + 3)) < this.Owner.World.Participants[participantIdx].GrandTownExtent)
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

				if (farthest == 1)
					break;

				given += this.Owner.World.Towns[farthestIdx].TrueExtent;

				AssignCityOwner((short)farthestIdx, rebelIdx);
			}
		}

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

		private void TransferUnitInCity(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop)
		{
			ushort townIdx = this.Owner.Helpers.FindCityAtPosition((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));
			short unitKind = (sbyte)this.Troops.PullAt(troop, UnitMemory.Kind);

			byte homeIdx = this.Troops.PullAt(troop, UnitMemory.BaseTown);

			if (HomeOwner(participantIdx, homeIdx) != this.Owner.World.Towns[townIdx].ParticipantIdx)
			{
				this.Owner.UnitOps.DestroyUnit(participantIdx, (short)troopIdx);

				this.Owner.MapOps.TagTileOwned((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

				this.Owner.UnitOps.CreateUnit(this.Owner.World.Towns[townIdx].ParticipantIdx, unitKind,
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

		private void TransferUnitOutside(short participantIdx, short rebelIdx, ushort troopIdx, ushort troop, short unitKind)
		{
			ushort playerUnits = (ushort)(0x600 * participantIdx);

			short homeOwner = HomeOwner(participantIdx, this.Troops.PullAt(troop, UnitMemory.BaseTown));
			short next = (sbyte)this.Troops.PullAt(troop, UnitMemory.FollowingInStack);

			if ((ushort)homeOwner != (ushort)rebelIdx)
			{
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

		private short HomeOwner(short participantIdx, byte homeIdx) =>
			HomeCityRules.HasHome(homeIdx) ?
				this.Owner.World.Towns[homeIdx].ParticipantIdx : participantIdx;
	}
}
