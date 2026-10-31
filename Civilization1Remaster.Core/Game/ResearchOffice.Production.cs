
namespace Civilization1Remaster.Core.Game
{
	/// <summary>The production the computer players choose for their cities: the choices, the needs of the city, the scores and the advice</summary>
	public partial class ResearchOffice
	{
		/// <inheritdoc/>
		public ushort PickCityProduction(short participantIdx, short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"PickCityProduction({participantIdx}, {townIdx})");

			// The words of the frame are in C# (B3, properties below; some are read before they are written: 0)
			FrameWords outerFrame = this.pickFrame;
			this.pickFrame = new FrameWords(this.Processor, 0x14a);
			PageState = 0;
			AdvisorsShown = 0;

			this.Owner.CityOps.HandleCity(townIdx, -1);

			ushort choice;

			while (true)
			{
				if (ListProductionChoices(participantIdx, townIdx))
					continue;

				ushort? early = AssessCityNeeds(participantIdx, townIdx);
				if (early != null)
				{
					choice = early.Value;
					break;
				}

				AssessCityDefence(participantIdx, townIdx);
				ScoreProductionChoices(participantIdx, townIdx);

				ushort? decided = DecideProduction(participantIdx, townIdx);
				if (decided != null)
				{
					choice = decided.Value;
					break;
				}
			}

			this.pickFrame = outerFrame;

			return choice;
		}

		/// <inheritdoc/>
		public void ComposeProductionOption(short townIdx, ushort price)
		{
			using var _ = this.Processor.Journal.Scope($"ComposeProductionOption({townIdx}, {price})");

			int perTurn = this.Owner.Helpers.ClampValue(
				(short)CityTurn.Shields - (short)CityTurn.UnitUpkeep, 1, 99);

			ushort turns = (ushort)this.Owner.Helpers.ClampValue(
				(((10 * price) - this.Owner.World.Towns[townIdx].MaterialsTally - 1) / (short)perTurn) + 1, 1, 999);

			this.Owner.Words.Append(" (");

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)turns, 10));

			this.Owner.Words.Append((ushort)(turns != 1 ? 0x22b2 : 0x22b9));
		}

		private ushort ChoiceEntry(int entryIdx) => FrameWord(TableWord(ChoiceTable, entryIdx));

		private void ChoiceEntry(int entryIdx, ushort choice) => FrameWord(TableWord(ChoiceTable, entryIdx), choice);

		/// <summary>Appends a choice to the list (and to the menu text, done by the caller)</summary>
		private void AddChoice(ushort choice)
		{
			short entryIdx = ChoiceCount;
			ChoiceCount = (short)(entryIdx + 1);
			ChoiceEntry(entryIdx, choice);
		}

		/// <summary>
		/// The menu text and the list of choices. True when the list is too long for one page the first time: it starts again,
		/// cut after the units
		/// </summary>
		private bool ListProductionChoices(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			ChoiceCount = 1;
			FrameWord(ChoiceTable, (ushort)(short)town.PresentMakingIdx);

			Message("What shall we build in ")
				.City(townIdx)
				.Text("?\n ");

			if (PageState != 2)
				ListUnitChoices(participantIdx, townIdx);

			FrameWord(PageCutCountLocal, (ushort)ChoiceCount);
			FrameWord(PageCutLengthLocal, this.Owner.Words.Length);

			// The Great Wall on the continent of the city (no walls and the like then)
			FrameWord(SharedWallLocal, 0);

			if (this.Owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 15) != 0)
			{
				City wallTown = this.Owner.World.Towns[this.Owner.World.MarvelTownIdx[15]];
				FrameWord(WallContinentLocal, this.Owner.MapOps.ObtainContinentID(wallTown.Place.X, wallTown.Place.Y));

				if (this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) == FrameWord(WallContinentLocal))
					FrameWord(SharedWallLocal, 1);
			}

			if ((this.Owner.World.DiagMarkers & 4) != 0)
				ListImprovementChoices(participantIdx, townIdx);

			ListWonderChoices(participantIdx, townIdx);

			if (participantIdx == this.Owner.World.PersonParticipantIdx && (town.ConditionMarker & 0x10) == 0 && ChoiceCount > 0x1e)
			{
				if (PageState == 0)
				{
					PageState = 1;
					return true;
				}

				this.Owner.Libc.CopyText((ushort)(FrameWord(PageCutLengthLocal) + TextBuffer.Address), "more...\n");
				ChoiceEntry(FrameWord(PageCutCountLocal), 0x63);
			}

			if (PageState == 2)
			{
				this.Owner.Words.Append("more...\n");
				AddChoice(0x62);
			}

			return false;
		}

		/// <summary>
		/// The units the player knows and that aren't obsolete (ships only in a coastal city; nuclear weapons with the
		/// Manhattan Project and the technology 0x1b), with their turns, attack, defence and moves
		/// </summary>
		private void ListUnitChoices(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (FrameWord(ChoiceLoopLocal, 0); (short)FrameWord(ChoiceLoopLocal) < 0x1c; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				sbyte kind = (sbyte)FrameWord(ChoiceLoopLocal);

				if (kind == 0x19 && (this.Owner.World.MarvelTownIdx[17] == -1 || PlayerKnowsTechnology(participantIdx, 0x1b) == 0))
					continue;

				if (PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0x20)) == 0 ||
					PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0xc)) != 0)
				{
					continue;
				}

				if ((town.ConditionMarker & 2) == 0 && UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
					continue;

				this.Owner.Words.Append((ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride));
				ComposeProductionOption(townIdx, UnitKindWord(kind, UnitKindTable.Price));
				this.Owner.Words.Append(", ADM:");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.AssaultPower), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.GuardPower), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.ShiftTally), 10));
				this.Owner.Words.Append(")\n ");

				AddChoice((ushort)kind);
			}
		}

		/// <summary>
		/// The improvements 1 - 24 the player knows and the city hasn't built, without the ones that don't fit: a second
		/// building of bit 0 (7), 15 with bit 15, 10 / 12 when replaced by a known technology, 20 only in a city with bit 3 of
		/// its state, 19 with bit 14, 19 - 21 not with the Great Wall on the continent or with bits 2 - 4 of the second word;
		/// the spaceship parts (22 - 24) after the Apollo Program, before the launch
		/// </summary>
		private void ListImprovementChoices(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (FrameWord(ChoiceLoopLocal, 1); (short)FrameWord(ChoiceLoopLocal) <= 0x18; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				short improvementIdx = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIdx, this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x1c))) == 0)
					continue;

				FrameWord(BuiltLowLocal, town.StructureMarkers0);
				FrameWord(BuiltHighLocal, town.StructureMarkers1);
				uint built = (uint)(FrameWord(BuiltHighLocal) << 16 | FrameWord(BuiltLowLocal));

				if ((built & (1u << (byte)(improvementIdx - 1))) != 0)
					continue;

				if (improvementIdx == 7 && (this.pickFrame.FrameByte(BuiltLowLocal) & 1) != 0)
					continue;

				if (improvementIdx == 0xf && town.Has(CityStructure.ManufacturingPlant))
					continue;

				if (improvementIdx == 0xa && !town.Has(CityStructure.Marketplace) &&
					PlayerKnowsTechnology(participantIdx, (short)BuildingWord(5, BuildingTable.Technology)) != 0)
				{
					continue;
				}

				if (improvementIdx == 0xc && !town.Has(CityStructure.Library) &&
					PlayerKnowsTechnology(participantIdx, (short)BuildingWord(6, BuildingTable.Technology)) != 0)
				{
					continue;
				}

				if (improvementIdx == 0x14 && (town.ConditionMarker & 8) == 0)
					continue;

				if (improvementIdx == 0x13 && !town.Has(CityStructure.Factory))
					continue;

				if (improvementIdx is 0x13 or 0x14 or 0x15 && (FrameWord(SharedWallLocal) != 0 || (town.StructureMarkers1 & 0x1c) != 0))
					continue;

				if (improvementIdx > 0x15 &&
					(this.Owner.World.MarvelTownIdx[19] == -1 || (this.Owner.World.StarshipMarkers & (1 << participantIdx)) != 0))
				{
					continue;
				}

				this.Owner.Words.Append(ImprovementRecord(improvementIdx));
				ComposeProductionOption(townIdx, this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x18)));
				this.Owner.Words.Append(")\n ");

				AddChoice((ushort)-improvementIdx);
			}
		}

		/// <summary>The wonders (25 - 45) the player knows that nobody has built; "*" before one that is obsolete for someone</summary>
		private void ListWonderChoices(short participantIdx, short townIdx)
		{
			FrameWord(ChoiceLoopLocal, 0x19);

			do
			{
				short improvementIdx = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIdx, this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x1c))) != 0 &&
					this.Owner.World.MarvelTownIdx[improvementIdx - 0x18] == -1)
				{
					ushort obsolete = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((improvementIdx << 1) + 0x10ce));

					if (obsolete != 0x7f && ((byte)this.Owner.World.ScienceLeadingFoundVia[this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((improvementIdx << 1) + 0x10ce))] & 7) != 0)
						this.Owner.Words.Append("*");

					this.Owner.Words.Append(ImprovementRecord(improvementIdx));
					ComposeProductionOption(townIdx, this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x18)));
					this.Owner.Words.Append(")\n ");

					AddChoice((ushort)-improvementIdx);
				}

				FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) <= 0x2d);
		}

		/// <summary>
		/// The situation of the city: its continent and the player's approach there (defensive: 1, 2, or 5 with 0x8078), the
		/// room for unhappiness, the units being built in the player's other cities there (+1) and the units of each kind
		/// on the continent (also the ones known in cities). A computer player without units under despotism or anarchy builds
		/// militia (1); the barbarians build legions (3) or, with bit 0 - 2 of 0x3772:0x520, knights (7). Null: go on.
		/// The player's units are counted again (TroopTally); it is put back later, but not after these early answers.
		/// </summary>
		private ushort? AssessCityNeeds(short participantIdx, short townIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			City town = this.Owner.World.Towns[townIdx];

			FrameWord(ContinentLocal, this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y));
			FrameWord(ApproachLocal, (ushort)player.Landmasses[FrameWord(ContinentLocal)].Approach);
			FrameWord(DefensiveLocal, (ushort)((short)FrameWord(ApproachLocal) is 1 or 2 ? 1 : 0));

			if (Turn.LateGameNudge != 0 && (short)FrameWord(ApproachLocal) == 5)
				FrameWord(DefensiveLocal, 1);

			short unhappy = (short)((player.RegimeKind <= 1 ? 1 : 2) * (short)CityTurn.Settlers);
			FrameWord(HappyRoomLocal, (ushort)((short)CityTurn.Food - unhappy - (town.TrueExtent << 1)));

			for (FrameWord(ChoiceLoopLocal, 0); (short)FrameWord(ChoiceLoopLocal) < 0x1c; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				int kind = (short)FrameWord(ChoiceLoopLocal);
				FrameWord(TableWord(MakingElsewhereTable, kind), 1);
				FrameWord(TableWord(UnitsOnContinentTable, kind), 0);
			}

			FrameWord(UnitTallyLocal, (ushort)player.TroopTally);
			player.TroopTally = 0;

			FrameWord(TownLoopLocal, 0);

			do
			{
				City other = this.Owner.World.Towns[FrameWord(TownLoopLocal)];

				if (other.ConditionMarker != 0xff && other.ParticipantIdx == participantIdx &&
					this.Owner.MapOps.ObtainContinentID(other.Place.X, other.Place.Y) == FrameWord(ContinentLocal))
				{
					if (other.PresentMakingIdx >= 0 && (short)FrameWord(TownLoopLocal) != townIdx)
					{
						int making = TableWord(MakingElsewhereTable, other.PresentMakingIdx);
						FrameWord(making, (ushort)(FrameWord(making) + 1));
					}

					for (FrameWord(UnfamiliarLoopLocal, 0); (short)FrameWord(UnfamiliarLoopLocal) < 2; FrameWord(UnfamiliarLoopLocal, (ushort)(FrameWord(UnfamiliarLoopLocal) + 1)))
					{
						byte unfamiliar = (byte)other.Unfamiliar[FrameWord(UnfamiliarLoopLocal)];
						this.pickFrame.SetFrameByte(BuiltLowLocal, unfamiliar);

						if (unfamiliar != 0xff)
						{
							player.TroopTally++;
							int units = TableWord(UnitsOnContinentTable, (sbyte)unfamiliar & 0x3f);
							FrameWord(units, (ushort)(FrameWord(units) + 1));
						}
					}
				}

				FrameWord(TownLoopLocal, (ushort)(FrameWord(TownLoopLocal) + 1));
			}
			while ((short)FrameWord(TownLoopLocal) < 0x80);

			FrameWord(ChoiceLoopLocal, 0);

			do
			{
				short troopIdx = (short)FrameWord(ChoiceLoopLocal);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff)
				{
					player.TroopTally++;

					if (this.Owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == FrameWord(ContinentLocal))
					{
						int units = TableWord(UnitsOnContinentTable, this.troops.TroopKind(participantIdx, troopIdx));
						FrameWord(units, (ushort)(FrameWord(units) + 1));
					}
				}

				FrameWord(ChoiceLoopLocal, (ushort)(troopIdx + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) < 0x80);

			if (player.TroopTally == 0 && participantIdx != this.Owner.World.PersonParticipantIdx && player.RegimeKind <= 1)
				return 1;

			if (participantIdx == 0)
				return (ushort)((this.Processor.PullUInt8(0x3772, 0x520) & 7) != 0 ? 7 : 3);

			return null;
		}

		/// <summary>
		/// The defenders the city needs (0x8): 2 without units in it, 1 when it has fewer defenders than a quarter of its
		/// size plus the defensive approach
		/// </summary>
		private void AssessCityDefence(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			FrameWord(DefenceNeedLocal, 0);
			FrameWord(ChoiceLoopLocal, this.Owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y));

			if (FrameWord(ChoiceLoopLocal) == 0xffff)
			{
				FrameWord(DefenceNeedLocal, 2);
				return;
			}

			short defenders = (short)this.Owner.UnitOps.CountUnitsWithFlagInStack(participantIdx, (short)FrameWord(ChoiceLoopLocal), 2);

			if ((short)(town.TrueExtent / 4 + FrameWord(DefensiveLocal)) > defenders)
				FrameWord(DefenceNeedLocal, 1);
		}

		/// <summary>
		/// The score of every choice (lower is better), and the best one overall (0x144), the best unit (0xc) and the best
		/// improvement or wonder (0x48)
		/// </summary>
		private void ScoreProductionChoices(short participantIdx, short townIdx)
		{
			FrameWord(BestUnitScoreLocal, 0x3e7);
			FrameWord(BestImprovementScoreLocal, 0x3e7);
			FrameWord(BestScoreLocal, 0x3e7);
			FrameWord(BestUnitLocal, 0xffff);
			FrameWord(BestImprovementLocal, 0xffff);

			for (FrameWord(ChoiceLoopLocal, 1); (short)FrameWord(ChoiceLoopLocal) < ChoiceCount; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				FrameWord(ChoiceLocal, ChoiceEntry((short)FrameWord(ChoiceLoopLocal)));
				short choice = (short)FrameWord(ChoiceLocal);

				if (choice >= 0 && choice < 0x1a)
				{
					// Carriers are never chosen
					if (choice == 0x17 || !ScoreUnitChoice(participantIdx, townIdx, choice))
						continue;
				}
				else
				{
					ScoreImprovementChoice(participantIdx, townIdx, choice);
				}

				if (Score < (short)FrameWord(BestScoreLocal))
				{
					FrameWord(BestChoiceLocal, FrameWord(ChoiceLoopLocal));
					FrameWord(BestScoreLocal, FrameWord(ScoreLocal));
				}

				if (choice < 0 || choice >= 0x1a)
				{
					if (Score < (short)FrameWord(BestImprovementScoreLocal))
					{
						FrameWord(BestImprovementScoreLocal, (ushort)Score);
						FrameWord(BestImprovementLocal, FrameWord(ChoiceLoopLocal));
					}
				}
				else if (Score < (short)FrameWord(BestUnitScoreLocal))
				{
					FrameWord(BestUnitScoreLocal, (ushort)Score);
					FrameWord(BestUnitLocal, FrameWord(ChoiceLoopLocal));
				}
			}
		}

		/// <summary>
		/// The score of an improvement, a wonder, a diplomat (26) or a caravan (27): its need in the city (most are 999, not
		/// needed), weighed by the defensive approach and the leader's character, half more with bit 1 of the buildings
		/// </summary>
		private void ScoreImprovementChoice(short participantIdx, short townIdx, short choice)
		{
			City town = this.Owner.World.Towns[townIdx];
			short food = (short)CityTurn.Shields;
			short trade = (short)CityTurn.Trade;

			Score = 0x3e7;
			FrameWord(DefensiveImprovementLocal, 0);

			switch (27 - choice)
			{
				case 0:
					// A caravan: with a free trade route, more wanted with less trade, at least 3; for the human player only
					// when the city builds by itself
					if ((byte)town.CommerceTownIndices[2] == 0xff)
					{
						Score = (short)-(CPU.SignedQuotient(trade, 2) - 10);

						if ((byte)town.CommerceTownIndices[0] != 0xff)
							Score += 2;

						if ((byte)town.CommerceTownIndices[1] != 0xff)
							Score += 2;

						if (Score < 3)
							Score = 3;
					}

					if (participantIdx == this.Owner.World.PersonParticipantIdx && (town.ConditionMarker & 0x10) != 0)
						Score = 0x3e7;

					break;

				case 1:
					// A diplomat: against the human player (no peace, no caravan in production, a player of 0x2a6e), when the
					// human player knows a technology of a random word of the technologies the player doesn't
					ScoreDiplomatChoice(participantIdx);
					break;

				case 29:
					Score = 5;
					FrameWord(DefensiveImprovementLocal, 1);
					break;

				case 30:
					Score = (short)(food < 3 ? 8 : 4);
					break;

				case 31:
					if ((town.ConditionMarker & 1) != 0)
						Score = -4;
					break;

				case 32:
					Score = (short)-(CPU.SignedQuotient(trade, 2) - 10);

					if ((town.ConditionMarker & 1) != 0)
						Score = -3;
					break;

				case 33:
				case 37:
					Score = (short)-(CPU.SignedQuotient(trade, 3) - 10);
					break;

				case 34:
					Score = (short)-(((short)CityTurn.Corruption << 1) - 0xc);
					break;

				case 35:
					Score = (short)-(town.TrueExtent / 2 - 10);

					if (town.Has(CityStructure.Palace))
						Score -= 4;

					FrameWord(DefensiveImprovementLocal, 1);
					break;

				case 36:
				{
					short room = CPU.SignedQuotient((short)FrameWord(HappyRoomLocal), 2);
					short size = town.TrueExtent;

					if ((short)(10 - room) <= size)
						Score = (short)-(size + room - 0xe);
					break;
				}

				case 38:
					if ((town.ConditionMarker & 1) != 0)
						Score = -2;
					break;

				case 39:
					Score = (short)-(CPU.SignedQuotient(trade, 4) - 10);
					break;

				case 41:
					if ((town.ConditionMarker & 1) != 0)
						Score = -1;
					break;

				case 42:
				case 47:
					Score = (short)-(CPU.SignedQuotient(food, 2) - 0xd);
					break;

				case 43:
				case 45:
				case 48:
					Score = (short)-(CPU.SignedQuotient(food, 8) - 0xc);
					break;

				case 46:
					Score = (short)-(CPU.SignedQuotient(food, 5) - 0xc);
					break;

				case 49:
				{
					// A spaceship part: not when the player or the human player has launched; 2 when the human player has
					// built a structure (bit 8 + human player), else 4
					byte person = (byte)this.Owner.World.PersonParticipantIdx;

					if ((((1 << participantIdx) + (1 << person)) & this.Owner.World.StarshipMarkers) == 0)
						Score = (short)((this.Owner.World.StarshipMarkers & (1 << (byte)(person + 8))) != 0 ? 2 : 4);
					break;
				}
			}

			if (Score >= 0x190)
			{
				Score = 0x3fff;
			}
			else
			{
				short weight = (short)(FrameWord(DefensiveLocal) == FrameWord(DefensiveImprovementLocal) ? 10 : 20);
				Score = (short)(weight * Score);

				short character = (short)(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Leaning) + 3);
				Score = CPU.SignedQuotient((short)(3 * Score), character);
			}

			if (town.Has(CityStructure.Barracks))
				Score += CPU.SignedQuotient(Score, 4);
		}

		private void ScoreDiplomatChoice(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			if (person <= 0 || (this.Owner.World.Participants[participantIdx].Relations[person] & 2) != 0 ||
				this.Owner.World.Participants[participantIdx].TroopsInsideMaking[27] > 0)
			{
				return;
			}

			FrameWord(TownLoopLocal, (ushort)this.Owner.Libc.Dice.DrawInt(5));

			if (((1 << participantIdx) & Turn.HumanContinentAttackers) == 0)
				return;

			ushort own = this.Owner.World.Participants[participantIdx].FoundScienceMarkers[FrameWord(TownLoopLocal)];

			if ((ushort)(this.Owner.World.Participants[person].FoundScienceMarkers[FrameWord(TownLoopLocal)] | own) == own)
				return;

			Score = (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[participantIdx].FoundScienceTally -
				this.Owner.World.Participants[person].FoundScienceTally + 10, 5, 10);
		}

		/// <summary>
		/// The score of a unit (false: not wanted): settlers only with room for unhappiness or no settlers yet; no militia
		/// above despotism; at most 3 nuclear weapons; fighters (0xe) only against a human player without peace who has more
		/// than twice as many; the units of the kind there and in production, weighed by the defence the city needs, the role
		/// and the approach, the cost, the strength and the moves
		/// </summary>
		private bool ScoreUnitChoice(short participantIdx, short townIdx, short choice)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			City town = this.Owner.World.Towns[townIdx];
			sbyte kind = (sbyte)choice;

			if (choice == 0 && (player.RegimeKind <= 1 ? 1 : 2) >= (short)FrameWord(HappyRoomLocal) &&
				(player.LiveTroops[0] != 0 || (short)FrameWord(MakingElsewhereTable) > 1))
			{
				return false;
			}

			if (choice == 1 && player.RegimeKind > 1)
				return false;

			if (choice == 0x19 && (player.LiveTroops[25] >= 4 ||
				(short)this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(participantIdx * 0x38 + 0xd33a)) >= 2))
			{
				return false;
			}

			if (choice == 0xe)
			{
				short human = CPU.SignedQuotient((short)this.Processor.PullUInt16(this.Processor.SegData.Value16,
					(ushort)(this.Owner.World.PersonParticipantIdx * 0x38 + 0xb3dc)), 2);

				if ((short)(player.LiveTroops[choice] + player.TroopsInsideMaking[choice]) >= human ||
					(player.Relations[this.Owner.World.PersonParticipantIdx] & 2) != 0)
				{
					return false;
				}
			}

			if (UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				Score = (short)((FrameWord(TableWord(MakingElsewhereTable, choice)) << 1) +
					FrameWord(TableWord(UnitsOnContinentTable, choice)));
			}
			else
			{
				short assault = player.Landmasses[FrameWord(ContinentLocal)].Assault;

				if (assault < 8)
					return false;

				short units = (short)((player.TroopsInsideMaking[choice] << 1) + player.LiveTroops[choice]);
				Score = CPU.SignedQuotient((short)(player.Landmasses[FrameWord(ContinentLocal)].Assault * units), (short)(player.TroopTally + 1));
			}

			if (FrameWord(DefenceNeedLocal) != 0)
			{
				if (UnitKindWord(kind, UnitKindTable.TroopGroup) != UnitKindTable.DefenceTroops)
					return false;

				Score = CPU.SignedQuotient(2, (short)FrameWord(DefenceNeedLocal));
			}

			ushort role = UnitKindWord(kind, UnitKindTable.TroopGroup);

			if (role != UnitKindTable.DefenceTroops && role != 0)
			{
				Score <<= 1;

				if (FrameWord(ApproachLocal) != role)
					Score <<= 1;
			}

			if (UnitKindWord(kind, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				if (FrameWord(ApproachLocal) != 5)
					return false;

				Score = CPU.SignedQuotient((short)(3 * Score), 2);
			}

			short weight = choice == 0
				? (short)((town.Has(CityStructure.Granary) ? 4 : 6) -
					LeaderWord(player.ChiefVisuals, LeaderTable.Leaning))
				: (short)0x10;

			short price = (short)(weight * (short)(UnitKindWord(kind, UnitKindTable.Price) + 2) * Score);
			short strength = (short)((short)(UnitKindWord(kind, UnitKindTable.AssaultPower) +
				UnitKindWord(kind, UnitKindTable.GuardPower)) *
				(short)(UnitKindWord(kind, UnitKindTable.ShiftTally) + 1));
			Score = CPU.SignedQuotient(price, strength);

			if (UnitKindWord(kind, UnitKindTable.TroopGroup) == 3)
				Score = (short)((FrameWord(DefensiveLocal) != 0 ? 4 : 2) * Score);

			if (UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				Score <<= 1;

			if (player.TroopTally > 120)
				Score <<= 2;

			// Doubled unless the player is at war (0x6ed4) and the approach is defensive or there is more food than 0xdeb8
			if ((FrameWord(DefensiveLocal) == 0 && (short)CityTurn.Shields <= (short)CityTurn.SpecialSlots) ||
				Turn.PlayerAtWar == 0)
			{
				Score <<= 1;
			}

			return true;
		}

		/// <summary>
		/// The answer, or null to show the menu again: a computer player takes the best choice (0xffff when the city builds
		/// nothing); a city of the human player that builds by itself takes the best improvement (score up to 500) or 0x63;
		/// else the advisors (once, with bit 0 of the preferences) and the menu
		/// </summary>
		private ushort? DecideProduction(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.World.Participants[participantIdx].TroopTally = (short)FrameWord(UnitTallyLocal);

			if (!this.Owner.World.IsHuman(participantIdx))
			{
				if ((byte)town.PresentMakingIdx == 0xff)
					return 0xffff;

				return ChoiceEntry((short)FrameWord(BestChoiceLocal));
			}

			if ((town.ConditionMarker & 0x10) != 0)
			{
				if (FrameWord(BestImprovementLocal) != 0xffff && (short)FrameWord(BestImprovementScoreLocal) <= 0x1f4)
					return ChoiceEntry((short)FrameWord(BestImprovementLocal));

				return 0x63;
			}

			if ((this.Owner.World.SessionPrefMarkers & 1) != 0 && AdvisorsShown == 0)
			{
				ushort screen = Screen.Box;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), 2);

				if (FrameWord(BestUnitLocal) != 0xffff)
					ShowProductionAdvice((short)FrameWord(BestUnitLocal), "Military Advisor:\n", 0x2284, 8);

				if (FrameWord(BestImprovementLocal) != 0xffff)
					ShowProductionAdvice((short)FrameWord(BestImprovementLocal), "Domestic Advisor:\n", 0x22a3, 164);

				AdvisorsShown = 1;
				screen = Screen.Box;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), 1);

				return null;
			}

			if (ChoiceCount > 0x14 || PageState != 0)
			{
				ushort screen = Screen.Box;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), 2);
			}

			Menu.Footer = 1;
			ushort picked = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 80, 8, 1);
			Score = (short)ChoiceEntry(picked + 1);

			if ((ushort)Score == 0x63)
			{
				PageState = 2;
				return null;
			}

			if ((ushort)Score == 0x62)
			{
				PageState = 1;
				return null;
			}

			ushort display = Screen.Box;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(display + 0x10), 1);

			if (Menu.HelpTopic == 0)
				return (ushort)Score;

			// The Civilopedia of the choice, then the menu again
			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			Turn.CivilopediaWithoutPictures = 1;

			if (Score >= 0)
				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)Score, 2);
			else
				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)(-Score), 1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			MapView.RedrawRequest = 1;

			return null;
		}

		/// <summary>An advisor names a choice: the advice for "*name" from the text file, in a menu at the bottom</summary>
		private void ShowProductionAdvice(short entryIdx, string advisor, ushort documentAddr, int horizLoc)
		{
			Score = (short)ChoiceEntry(entryIdx);
			ushort name = Score >= 0 ? (ushort)(0x112a + 0x22 * Score) : ImprovementRecord(-Score);

			string choiceName = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, name));
			this.Owner.Words.Set(advisor);
			this.Owner.BlockText.LoadTextFromFile(documentAddr, "*" + choiceName);
			this.Owner.Codes.Set(3, choiceName);
			this.Owner.BlockText.ReflowText();
			this.Owner.BlockText.ArrangeTextRow(0x32);
			Screen.DialogNeedsRedraw = 1;
			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, horizLoc, 160, 1);
		}
	}
}
