namespace Civ_1_5_Remastered.Core.Game.Research
{
	public partial class ResearchOffice
	{
		public ushort PickCityProduction(short participantIndex, short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"PickCityProduction({participantIndex}, {townIndex})");

			FrameWords outerFrame = this.pickFrame;
			this.pickFrame = new FrameWords(this.Processor, 0x14a);
			PageState = 0;
			AdvisorsShown = 0;

			this.Owner.CityOperations.HandleCity(townIndex, -1);

			ushort choice;

			while (true)
			{
				if (ListProductionChoices(participantIndex, townIndex))
				{
					continue;
				}

				ushort? early = AssessCityNeeds(participantIndex, townIndex);
				if (early != null)
				{
					choice = early.Value;
					break;
				}

				AssessCityDefence(participantIndex, townIndex);
				ScoreProductionChoices(participantIndex, townIndex);

				ushort? decided = DecideProduction(participantIndex, townIndex);
				if (decided != null)
				{
					choice = decided.Value;
					break;
				}
			}

			this.pickFrame = outerFrame;

			return choice;
		}

		public void ComposeProductionOption(short townIndex, ushort price)
		{
			using var _ = this.Processor.Journal.Scope($"ComposeProductionOption({townIndex}, {price})");

			int perTurn = this.Owner.Helpers.ClampValue(
				(short)CityTurn.Shields - (short)CityTurn.UnitUpkeep, 1, 99);

			ushort turns = (ushort)this.Owner.Helpers.ClampValue(
				(((10 * price) - this.Owner.World.Towns[townIndex].MaterialsTally - 1) / (short)perTurn) + 1, 1, 999);

			this.Owner.Words.Append(" (");

			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)turns, 10));

			this.Owner.Words.Append((ushort)(turns != 1 ? 0x22b2 : 0x22b9));
		}

		private ushort ChoiceEntry(int entryIndex) => FrameWord(TableWord(ChoiceTable, entryIndex));

		private void ChoiceEntry(int entryIndex, ushort choice) => FrameWord(TableWord(ChoiceTable, entryIndex), choice);

		private void AddChoice(ushort choice)
		{
			short entryIndex = ChoiceCount;
			ChoiceCount = (short)(entryIndex + 1);
			ChoiceEntry(entryIndex, choice);
		}

		private bool ListProductionChoices(short participantIndex, short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			ChoiceCount = 1;
			FrameWord(ChoiceTable, (ushort)(short)town.PresentMakingIndex);

			Message("What shall we build in ")
				.City(townIndex)
				.Text("?\n ");

			if (PageState != 2)
			{
				ListUnitChoices(participantIndex, townIndex);
			}

			FrameWord(PageCutCountLocal, (ushort)ChoiceCount);
			FrameWord(PageCutLengthLocal, this.Owner.Words.Length);

			FrameWord(SharedWallLocal, 0);

			if (this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 15) != 0)
			{
				City wallTown = this.Owner.World.Towns[this.Owner.World.MarvelTownIndex[15]];
				FrameWord(WallContinentLocal, this.Owner.MapOperations.ObtainContinentIdentifier(wallTown.Place.X, wallTown.Place.Y));

				if (this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y) == FrameWord(WallContinentLocal))
				{
					FrameWord(SharedWallLocal, 1);
				}
			}

			if ((this.Owner.World.DiagonalMarkers & 4) != 0)
			{
				ListImprovementChoices(participantIndex, townIndex);
			}

			ListWonderChoices(participantIndex, townIndex);

			if (participantIndex == this.Owner.World.PersonParticipantIndex && (town.ConditionMarker & 0x10) == 0 && ChoiceCount > 0x1e)
			{
				if (PageState == 0)
				{
					PageState = 1;
					return true;
				}

				this.Owner.Runtime.CopyText((ushort)(FrameWord(PageCutLengthLocal) + TextBuffer.Address), "more...\n");
				ChoiceEntry(FrameWord(PageCutCountLocal), 0x63);
			}

			if (PageState == 2)
			{
				this.Owner.Words.Append("more...\n");
				AddChoice(0x62);
			}

			return false;
		}

		private void ListUnitChoices(short participantIndex, short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			for (FrameWord(ChoiceLoopLocal, 0); (short)FrameWord(ChoiceLoopLocal) < 0x1c; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				sbyte kind = (sbyte)FrameWord(ChoiceLoopLocal);

				if (kind == 0x19 && (this.Owner.World.MarvelTownIndex[17] == -1 || PlayerKnowsTechnology(participantIndex, 0x1b) == 0))
				{
					continue;
				}

				if (PlayerKnowsTechnology(participantIndex, (short)Tables.UnitKindWord(kind, 0x20)) == 0 ||
					PlayerKnowsTechnology(participantIndex, (short)Tables.UnitKindWord(kind, 0xc)) != 0)
				{
					continue;
				}

				if ((town.ConditionMarker & 2) == 0 && Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
				{
					continue;
				}

				this.Owner.Words.Append((ushort)(UnitKindTable.Origin + (kind * UnitKindTable.Stride)));
				ComposeProductionOption(townIndex, Tables.UnitKindWord(kind, UnitKindTable.Price));
				this.Owner.Words.Append(", ADM:");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)Tables.UnitKindWord(kind, UnitKindTable.GuardPower), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)Tables.UnitKindWord(kind, UnitKindTable.ShiftTally), 10));
				this.Owner.Words.Append(")\n ");

				AddChoice((ushort)kind);
			}
		}

		private void ListImprovementChoices(short participantIndex, short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			for (FrameWord(ChoiceLoopLocal, 1); (short)FrameWord(ChoiceLoopLocal) <= 0x18; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				short improvementIndex = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIndex, this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(ImprovementRecord(improvementIndex) + 0x1c))) == 0)
				{
					continue;
				}

				FrameWord(BuiltLowLocal, town.StructureMarkersLow);
				FrameWord(BuiltHighLocal, town.StructureMarkersHigh);
				uint built = (uint)((FrameWord(BuiltHighLocal) << 16) | FrameWord(BuiltLowLocal));

				if ((built & (1u << (byte)(improvementIndex - 1))) != 0)
				{
					continue;
				}

				if (improvementIndex == 7 && (this.pickFrame.FrameByte(BuiltLowLocal) & 1) != 0)
				{
					continue;
				}

				if (improvementIndex == 0xf && town.Has(CityStructure.ManufacturingPlant))
				{
					continue;
				}

				if (improvementIndex == 0xa && !town.Has(CityStructure.Marketplace) &&
					PlayerKnowsTechnology(participantIndex, (short)Tables.BuildingWord(5, BuildingTable.Technology)) != 0)
				{
					continue;
				}

				if (improvementIndex == 0xc && !town.Has(CityStructure.Library) &&
					PlayerKnowsTechnology(participantIndex, (short)Tables.BuildingWord(6, BuildingTable.Technology)) != 0)
				{
					continue;
				}

				if (improvementIndex == 0x14 && (town.ConditionMarker & 8) == 0)
				{
					continue;
				}

				if (improvementIndex == 0x13 && !town.Has(CityStructure.Factory))
				{
					continue;
				}

				if (improvementIndex is 0x13 or 0x14 or 0x15 && (FrameWord(SharedWallLocal) != 0 || (town.StructureMarkersHigh & 0x1c) != 0))
				{
					continue;
				}

				if (improvementIndex > 0x15 &&
					(this.Owner.World.MarvelTownIndex[19] == -1 || (this.Owner.World.StarshipMarkers & (1 << participantIndex)) != 0))
				{
					continue;
				}

				this.Owner.Words.Append(ImprovementRecord(improvementIndex));
				ComposeProductionOption(townIndex, this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(ImprovementRecord(improvementIndex) + 0x18)));
				this.Owner.Words.Append(")\n ");

				AddChoice((ushort)-improvementIndex);
			}
		}

		private void ListWonderChoices(short participantIndex, short townIndex)
		{
			FrameWord(ChoiceLoopLocal, 0x19);

			do
			{
				short improvementIndex = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIndex, this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(ImprovementRecord(improvementIndex) + 0x1c))) != 0 &&
					this.Owner.World.MarvelTownIndex[improvementIndex - 0x18] == -1)
				{
					ushort obsolete = this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((improvementIndex << 1) + 0x10ce));

					if (obsolete != 0x7f && ((byte)this.Owner.World.ScienceLeadingFoundVia[this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((improvementIndex << 1) + 0x10ce))] & 7) != 0)
					{
						this.Owner.Words.Append("*");
					}

					this.Owner.Words.Append(ImprovementRecord(improvementIndex));
					ComposeProductionOption(townIndex, this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(ImprovementRecord(improvementIndex) + 0x18)));
					this.Owner.Words.Append(")\n ");

					AddChoice((ushort)-improvementIndex);
				}

				FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) <= 0x2d);
		}

		private ushort? AssessCityNeeds(short participantIndex, short townIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			City town = this.Owner.World.Towns[townIndex];

			FrameWord(ContinentLocal, this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y));
			FrameWord(ApproachLocal, (ushort)player.Landmasses[FrameWord(ContinentLocal)].Approach);
			FrameWord(DefensiveLocal, (ushort)((short)FrameWord(ApproachLocal) is 1 or 2 ? 1 : 0));

			if (Turn.LateGameNudge != 0 && (short)FrameWord(ApproachLocal) == 5)
			{
				FrameWord(DefensiveLocal, 1);
			}

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

				if (other.ConditionMarker != 0xff && other.ParticipantIndex == participantIndex &&
					this.Owner.MapOperations.ObtainContinentIdentifier(other.Place.X, other.Place.Y) == FrameWord(ContinentLocal))
				{
					if (other.PresentMakingIndex >= 0 && (short)FrameWord(TownLoopLocal) != townIndex)
					{
						int making = TableWord(MakingElsewhereTable, other.PresentMakingIndex);
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
				short troopIndex = (short)FrameWord(ChoiceLoopLocal);

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0xff)
				{
					player.TroopTally++;

					if (this.Owner.MapOperations.ObtainContinentIdentifier(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) == FrameWord(ContinentLocal))
					{
						int units = TableWord(UnitsOnContinentTable, this.troops.TroopKind(participantIndex, troopIndex));
						FrameWord(units, (ushort)(FrameWord(units) + 1));
					}
				}

				FrameWord(ChoiceLoopLocal, (ushort)(troopIndex + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) < 0x80);

			if (player.TroopTally == 0 && participantIndex != this.Owner.World.PersonParticipantIndex && player.RegimeKind <= 1)
			{
				return 1;
			}

			if (participantIndex == 0)
			{
				return (ushort)((this.Processor.PullByte(0x3772, 0x520) & 7) != 0 ? 7 : 3);
			}

			return null;
		}

		private void AssessCityDefence(short participantIndex, short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			FrameWord(DefenceNeedLocal, 0);
			FrameWord(ChoiceLoopLocal, this.Owner.MapOperations.ObtainUnitAtTile(town.Place.X, town.Place.Y));

			if (FrameWord(ChoiceLoopLocal) == 0xffff)
			{
				FrameWord(DefenceNeedLocal, 2);
				return;
			}

			short defenders = (short)this.Owner.UnitOperations.CountUnitsWithFlagInStack(participantIndex, (short)FrameWord(ChoiceLoopLocal), 2);

			if ((short)((town.TrueExtent / 4) + FrameWord(DefensiveLocal)) > defenders)
			{
				FrameWord(DefenceNeedLocal, 1);
			}
		}

		private void ScoreProductionChoices(short participantIndex, short townIndex)
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
					if (choice == 0x17 || !ScoreUnitChoice(participantIndex, townIndex, choice))
					{
						continue;
					}
				}
				else
				{
					ScoreImprovementChoice(participantIndex, townIndex, choice);
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

		private void ScoreImprovementChoice(short participantIndex, short townIndex, short choice)
		{
			City town = this.Owner.World.Towns[townIndex];
			short food = (short)CityTurn.Shields;
			short trade = (short)CityTurn.Trade;

			Score = 0x3e7;
			FrameWord(DefensiveImprovementLocal, 0);

			switch (27 - choice)
			{
				case 0:
					if ((byte)town.CommerceTownIndices[2] == 0xff)
					{
						Score = (short)-(CentralProcessor.SignedQuotient(trade, 2) - 10);

						if ((byte)town.CommerceTownIndices[0] != 0xff)
						{
							Score += 2;
						}

						if ((byte)town.CommerceTownIndices[1] != 0xff)
						{
							Score += 2;
						}

						if (Score < 3)
						{
							Score = 3;
						}
					}

					if (participantIndex == this.Owner.World.PersonParticipantIndex && (town.ConditionMarker & 0x10) != 0)
					{
						Score = 0x3e7;
					}

					break;

				case 1:
					ScoreDiplomatChoice(participantIndex);
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
					{
						Score = -4;
					}

					break;

				case 32:
					Score = (short)-(CentralProcessor.SignedQuotient(trade, 2) - 10);

					if ((town.ConditionMarker & 1) != 0)
					{
						Score = -3;
					}

					break;

				case 33:
				case 37:
					Score = (short)-(CentralProcessor.SignedQuotient(trade, 3) - 10);
					break;

				case 34:
					Score = (short)-(((short)CityTurn.Corruption << 1) - 0xc);
					break;

				case 35:
					Score = (short)-((town.TrueExtent / 2) - 10);

					if (town.Has(CityStructure.Palace))
					{
						Score -= 4;
					}

					FrameWord(DefensiveImprovementLocal, 1);
					break;

				case 36:
					{
						short room = CentralProcessor.SignedQuotient((short)FrameWord(HappyRoomLocal), 2);
						short size = town.TrueExtent;

						if ((short)(10 - room) <= size)
						{
							Score = (short)-(size + room - 0xe);
						}

						break;
					}

				case 38:
					if ((town.ConditionMarker & 1) != 0)
					{
						Score = -2;
					}

					break;

				case 39:
					Score = (short)-(CentralProcessor.SignedQuotient(trade, 4) - 10);
					break;

				case 41:
					if ((town.ConditionMarker & 1) != 0)
					{
						Score = -1;
					}

					break;

				case 42:
				case 47:
					Score = (short)-(CentralProcessor.SignedQuotient(food, 2) - 0xd);
					break;

				case 43:
				case 45:
				case 48:
					Score = (short)-(CentralProcessor.SignedQuotient(food, 8) - 0xc);
					break;

				case 46:
					Score = (short)-(CentralProcessor.SignedQuotient(food, 5) - 0xc);
					break;

				case 49:
					{
						byte person = (byte)this.Owner.World.PersonParticipantIndex;

						if ((((1 << participantIndex) + (1 << person)) & this.Owner.World.StarshipMarkers) == 0)
						{
							Score = (short)((this.Owner.World.StarshipMarkers & (1 << (byte)(person + 8))) != 0 ? 2 : 4);
						}

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

				short character = (short)(Tables.LeaderWord(this.Owner.World.Participants[participantIndex].ChiefVisuals, LeaderTable.Leaning) + 3);
				Score = CentralProcessor.SignedQuotient((short)(3 * Score), character);
			}

			if (town.Has(CityStructure.Barracks))
			{
				Score += CentralProcessor.SignedQuotient(Score, 4);
			}
		}

		private void ScoreDiplomatChoice(short participantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;

			if (person <= 0 || (this.Owner.World.Participants[participantIndex].Relations[person] & 2) != 0 ||
				this.Owner.World.Participants[participantIndex].TroopsInsideMaking[27] > 0)
			{
				return;
			}

			FrameWord(TownLoopLocal, (ushort)this.Owner.Runtime.Dice.DrawInteger(5));

			if (((1 << participantIndex) & Turn.HumanContinentAttackers) == 0)
			{
				return;
			}

			ushort own = this.Owner.World.Participants[participantIndex].FoundScienceMarkers[FrameWord(TownLoopLocal)];

			if ((ushort)(this.Owner.World.Participants[person].FoundScienceMarkers[FrameWord(TownLoopLocal)] | own) == own)
			{
				return;
			}

			Score = (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[participantIndex].FoundScienceTally -
				this.Owner.World.Participants[person].FoundScienceTally + 10, 5, 10);
		}

		private bool ScoreUnitChoice(short participantIndex, short townIndex, short choice)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			City town = this.Owner.World.Towns[townIndex];
			sbyte kind = (sbyte)choice;

			if (choice == 0 && (player.RegimeKind <= 1 ? 1 : 2) >= (short)FrameWord(HappyRoomLocal) &&
				(player.LiveTroops[0] != 0 || (short)FrameWord(MakingElsewhereTable) > 1))
			{
				return false;
			}

			if (choice == 1 && player.RegimeKind > 1)
			{
				return false;
			}

			if (choice == 0x19 && (player.LiveTroops[25] >= 4 ||
				(short)this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((participantIndex * 0x38) + 0xd33a)) >= 2))
			{
				return false;
			}

			if (choice == 0xe)
			{
				short human = CentralProcessor.SignedQuotient((short)this.Processor.PullWord(this.Processor.SegmentData.Word,
					(ushort)((this.Owner.World.PersonParticipantIndex * 0x38) + 0xb3dc)), 2);

				if ((short)(player.LiveTroops[choice] + player.TroopsInsideMaking[choice]) >= human ||
					(player.Relations[this.Owner.World.PersonParticipantIndex] & 2) != 0)
				{
					return false;
				}
			}

			if (Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				Score = (short)((FrameWord(TableWord(MakingElsewhereTable, choice)) << 1) +
					FrameWord(TableWord(UnitsOnContinentTable, choice)));
			}
			else
			{
				short assault = player.Landmasses[FrameWord(ContinentLocal)].Assault;

				if (assault < 8)
				{
					return false;
				}

				short units = (short)((player.TroopsInsideMaking[choice] << 1) + player.LiveTroops[choice]);
				Score = CentralProcessor.SignedQuotient((short)(player.Landmasses[FrameWord(ContinentLocal)].Assault * units), (short)(player.TroopTally + 1));
			}

			if (FrameWord(DefenceNeedLocal) != 0)
			{
				if (Tables.UnitKindWord(kind, UnitKindTable.TroopGroup) != UnitKindTable.DefenceTroops)
				{
					return false;
				}

				Score = CentralProcessor.SignedQuotient(2, (short)FrameWord(DefenceNeedLocal));
			}

			ushort role = Tables.UnitKindWord(kind, UnitKindTable.TroopGroup);

			if (role != UnitKindTable.DefenceTroops && role != 0)
			{
				Score <<= 1;

				if (FrameWord(ApproachLocal) != role)
				{
					Score <<= 1;
				}
			}

			if (Tables.UnitKindWord(kind, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				if (FrameWord(ApproachLocal) != 5)
				{
					return false;
				}

				Score = CentralProcessor.SignedQuotient((short)(3 * Score), 2);
			}

			short weight = choice == 0
				? (short)((town.Has(CityStructure.Granary) ? 4 : 6) -
					Tables.LeaderWord(player.ChiefVisuals, LeaderTable.Leaning))
				: (short)0x10;

			short price = (short)(weight * (short)(Tables.UnitKindWord(kind, UnitKindTable.Price) + 2) * Score);
			short strength = (short)((short)(Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) +
				Tables.UnitKindWord(kind, UnitKindTable.GuardPower)) *
				(short)(Tables.UnitKindWord(kind, UnitKindTable.ShiftTally) + 1));
			Score = CentralProcessor.SignedQuotient(price, strength);

			if (Tables.UnitKindWord(kind, UnitKindTable.TroopGroup) == 3)
			{
				Score = (short)((FrameWord(DefensiveLocal) != 0 ? 4 : 2) * Score);
			}

			if (Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				Score <<= 1;
			}

			if (player.TroopTally > 120)
			{
				Score <<= 2;
			}

			if ((FrameWord(DefensiveLocal) == 0 && (short)CityTurn.Shields <= (short)CityTurn.SpecialSlots) ||
				Turn.PlayerAtWar == 0)
			{
				Score <<= 1;
			}

			return true;
		}

		private ushort? DecideProduction(short participantIndex, short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			this.Owner.World.Participants[participantIndex].TroopTally = (short)FrameWord(UnitTallyLocal);

			if (!this.Owner.World.IsHuman(participantIndex))
			{
				if ((byte)town.PresentMakingIndex == 0xff)
				{
					return 0xffff;
				}

				return ChoiceEntry((short)FrameWord(BestChoiceLocal));
			}

			if ((town.ConditionMarker & 0x10) != 0)
			{
				if (FrameWord(BestImprovementLocal) != 0xffff && (short)FrameWord(BestImprovementScoreLocal) <= 0x1f4)
				{
					return ChoiceEntry((short)FrameWord(BestImprovementLocal));
				}

				return 0x63;
			}

			if ((this.Owner.World.SessionPreferenceMarkers & 1) != 0 && AdvisorsShown == 0)
			{
				ushort screen = Screen.Box;
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(screen + 0x10), 2);

				if (FrameWord(BestUnitLocal) != 0xffff)
				{
					ShowProductionAdvice((short)FrameWord(BestUnitLocal), "Military Advisor:\n", 0x2284, 8);
				}

				if (FrameWord(BestImprovementLocal) != 0xffff)
				{
					ShowProductionAdvice((short)FrameWord(BestImprovementLocal), "Domestic Advisor:\n", 0x22a3, 164);
				}

				AdvisorsShown = 1;
				screen = Screen.Box;
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(screen + 0x10), 1);

				return null;
			}

			if (ChoiceCount > 0x14 || PageState != 0)
			{
				ushort screen = Screen.Box;
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(screen + 0x10), 2);
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
			this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(display + 0x10), 1);

			if (Menu.HelpTopic == 0)
			{
				return (ushort)Score;
			}

			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			Turn.CivilopediaWithoutPictures = 1;

			if (Score >= 0)
			{
				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)Score, 2);
			}
			else
			{
				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)(-Score), 1);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			MapView.RedrawRequest = 1;

			return null;
		}

		private void ShowProductionAdvice(short entryIndex, string advisor, ushort documentAddress, int horizontalLocation)
		{
			Score = (short)ChoiceEntry(entryIndex);
			ushort name = Score >= 0 ? (ushort)(0x112a + (0x22 * Score)) : ImprovementRecord(-Score);

			string choiceName = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, name));
			this.Owner.Words.Set(advisor);
			this.Owner.BlockText.LoadTextFromFile(documentAddress, "*" + choiceName);
			this.Owner.Codes.Set(3, choiceName);
			this.Owner.BlockText.ReflowText();
			this.Owner.BlockText.ArrangeTextRow(0x32);
			Screen.DialogNeedsRedraw = 1;
			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, horizontalLocation, 160, 1);
		}
	}
}
