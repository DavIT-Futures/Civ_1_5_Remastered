namespace Civ_1_5_Remastered.Core.Game.Towns
{
	public class SettlementAndScoring : GameComponent, ISettlementAndScoring
	{
		public SettlementAndScoring(IGameEngine owner)
			: base(owner)
		{
		}

		public void ClearCitySquare(int horizontalLocation, int verticalLocation)
		{
			ushort landKind = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
			short freshLand = (short)Tables.TerrainImprovementWord(landKind, TerrainImprovementTable.Irrigation);

			if (freshLand >= 0)
			{
				this.Owner.Services.AssignDot(2, horizontalLocation, verticalLocation, (ushort)freshLand);
				this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= 1;
				this.Owner.MapOperations.RemoveTileImprovements(6, horizontalLocation, verticalLocation);
			}
		}

		public ushort EstablishCity(short participantIndex, ushort unused, int horizontalLocation, int verticalLocation, ushort townExtent)
		{
			using var _ = this.Processor.Journal.Scope($"EstablishCity({participantIndex}, {unused}, {horizontalLocation}, {verticalLocation}, {townExtent})");

			this.Owner.Computer.DisableStrategicLocation(participantIndex, 0, horizontalLocation, verticalLocation, 4);

			ushort townIndex;

			for (townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff)
				{
					break;
				}
			}

			ushort result;

			if (townIndex == 0x80)
			{
				if (participantIndex == this.Owner.World.PersonParticipantIndex)
				{
					Screen.DialogStyle = 0x4;
					this.Owner.Words.Clear();

					this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityMaximum);

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 64, 80);
				}

				result = 0xffff;
			}
			else
			{
				result = FoundCity(participantIndex, (short)townIndex, horizontalLocation, verticalLocation, townExtent);
			}

			return result;
		}

		public void HandleCityDisasters(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"HandleCityDisasters({townIndex})");

			City town = this.Owner.World.Towns[townIndex];

			ushort terrains = 0;
			ushort neighbourIndex;

			for (neighbourIndex = 0; neighbourIndex <= 8; neighbourIndex++)
			{
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(
					town.Place.X + (short)Tables.DirectionHorizontal(neighbourIndex),
					town.Place.Y + (short)Tables.DirectionVertical(neighbourIndex));

				terrains |= (ushort)(1 << (byte)terrain);
			}

			EmptyMessage()
				.City(townIndex);

			this.Owner.Codes.Set(3, this.Owner.Words.ToString());

			this.Owner.Words.Clear();

			switch (this.Owner.Runtime.Dice.DrawInteger(10))
			{
				case 0:
					if ((terrains & 0x10) != 0 && (town.StructureMarkersLow & 0xfffe) != 0)
					{
						ushort buildingIndex = DestroyRandomBuilding(town);

						Message("Earthquake in ")
							.City(townIndex)
							.Text("!\n")
							.Text(GameTables.BuildingText(buildingIndex))
							.Text(" destroyed.\n");
					}

					break;

				case 1:
					if (this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, 10) == 0 && !town.Has(CityStructure.Aqueduct) &&
						this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, (short)Tables.BuildingWord(9, BuildingTable.Technology)) != 0)
					{
						this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityHintOne);

						if (town.TrueExtent != 0)
						{
							town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
						}
					}

					break;

				case 2:
					if ((terrains & 0x800) != 0)
					{
						if (!town.Has(CityStructure.CityWalls) && this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, (short)Tables.BuildingWord(8, BuildingTable.Technology)) != 0)
						{
							this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityHintTwo);

							if (town.TrueExtent > 1)
							{
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
							}
						}
					}

					break;

				case 3:
					if ((terrains & 0x20) != 0)
					{
						if (!town.Has(CityStructure.Temple) && this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, (short)Tables.BuildingWord(4, BuildingTable.Technology)) != 0)
						{
							this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityHintThree);

							if (town.TrueExtent > 1)
							{
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);
							}
						}
					}

					break;

				case 4:
					if (town.TrueExtent != 0 && !town.Has(CityStructure.Granary) &&
						this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, (short)Tables.BuildingWord(3, BuildingTable.Technology)) != 0)
					{
						this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityHintFour);

						town.GrainTally = 0;
						town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);

						this.Owner.Words.Append("Citizens demand GRANARY.\n");
					}

					break;

				case 5:
					if ((town.StructureMarkersLow & 0xfffe) != 0 && !town.Has(CityStructure.Aqueduct) &&
						this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, (short)Tables.BuildingWord(9, BuildingTable.Technology)) != 0)
					{
						ushort buildingIndex = DestroyRandomBuilding(town);

						this.Owner.Codes.Set(0, GameTables.BuildingText(buildingIndex));

						this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityHintFive);
					}

					break;

				case 6:
					if ((terrains & 0x400) != 0)
					{
						if (!town.Has(CityStructure.Barracks))
						{
							this.Owner.BlockText.ShowTextDialog(GameTexts.TopicCityHintSix);

							town.GrainTally = 0;
							town.MaterialsTally = 0;
						}
					}

					break;

				default:
					HandleCityRevolt(townIndex);
					break;
			}

			ReportCityDisaster(townIndex);
		}

		public ushort ShowFinalScore(short participantIndex, bool marker)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayFinalScore({participantIndex}, {marker})");

			if (marker)
			{
				ushort screenBufferPointer = Screen.Box;
				DrawingBox.DisplayIndex = 1;
				this.Owner.Briefings.ShowReportPreamble(GameTexts.TextCivilizationScore, 3);
			}

			this.Owner.World.Participants[0].Tally = 0;

			if (marker && (this.Owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(GameTexts.TextScoringCompleted, 160, 100, Hues.White);
				ushort screenBufferPointer = Screen.Box;
				DrawingBox.DisplayIndex = 0;

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					0, 0, 0x140, 0xc8,
					screenBufferPointer,
					0, 0);

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();
				this.Owner.Startup.ShowMouseCursor();
				this.Owner.TurnHooks.RepaintMainScreen();

				return 0;
			}

			int citizensVertical = 0x1a;
			int wondersVertical = 0x1a;
			Scoring.IconHorizontal = 0x3e7;
			Scoring.IconVertical = 0x1a;

			short savedLevyRatio = this.Owner.World.Participants[participantIndex].LevyRatio;
			this.Owner.World.Participants[participantIndex].LevyRatio = (short)(6 - this.Owner.World.Participants[participantIndex].ResearchLevyRatio);

			int totalCitizenScore = 0;
			for (short townIndex = 0; townIndex < 128; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff)
				{
					continue;
				}

				if (this.Owner.World.Towns[townIndex].ParticipantIndex != participantIndex)
				{
					continue;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				int happyCount = CityTurn.Happy;
				int unhappyCount = CityTurn.Unhappy;
				int specialistCount = CityTurn.Specialists;
				int contentCount = this.Owner.World.Towns[townIndex].TrueExtent - happyCount - unhappyCount - specialistCount;

				if (marker)
				{
					for (int index = 0; index < happyCount; index++)
					{
						DrawScoreIcon((ushort)(index & 1));
					}

					for (int index = 0; index < contentCount; index++)
					{
						DrawScoreIcon((ushort)((index & 1) + 2));
					}

					for (int index = 0; index < unhappyCount; index++)
					{
						DrawScoreIcon((ushort)((index & 1) + 4));
					}

					for (int index = 0; index < specialistCount; index++)
					{
						DrawScoreIcon((ushort)(this.Owner.CityOperations.ObtainCitizenType((ushort)index) + 5));
					}
				}

				totalCitizenScore += ScoreRules.ComputeCitizenScore(happyCount, contentCount, specialistCount);
			}

			this.Owner.World.Participants[participantIndex].LevyRatio = savedLevyRatio;
			Scoring.CitizenScore = (ushort)totalCitizenScore;

			ushort civilizationNamePointer = Tables.CivilizationName(participantIndex);
			Message(civilizationNamePointer)
				.Text(" Citizens (")
				.Number((short)totalCitizenScore)
				.Text(")");

			if (marker)
			{
				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, (short)citizensVertical, Hues.White);
			}

			wondersVertical = Scoring.IconVertical + 0x10;
			int wonderTitleVertical = wondersVertical;
			int wonderColumnHorizontal = 0x3e7;
			int totalWonderScore = 0;

			for (short wonderIndex = 1; wonderIndex <= 21; wonderIndex++)
			{
				short wonderCityIndex = this.Owner.World.MarvelTownIndex[wonderIndex];
				if (wonderCityIndex != -1 && wonderCityIndex != 0x80)
				{
					if (this.Owner.World.Towns[wonderCityIndex].ParticipantIndex == participantIndex)
					{
						wonderColumnHorizontal += 0x98;
						if (wonderColumnHorizontal > 0x12c)
						{
							wondersVertical += (ushort)this.Owner.Helpers.ClampValue(Scoring.VerticalShift + 2, 8, 99);
							wonderColumnHorizontal = 0x10;
						}

						if (marker && wondersVertical < 0xb4)
						{
							this.Owner.CityOperations.DrawResourceIcon((short)(wonderIndex + 0x18), (short)wonderColumnHorizontal, (short)(wondersVertical - 2));
							this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTables.WonderText(wonderIndex), (short)(wonderColumnHorizontal + 20), (short)wondersVertical, Hues.LightCyan);
						}

						totalWonderScore += 20;
					}
				}
			}

			Scoring.WonderScore = (ushort)totalWonderScore;
			Message(civilizationNamePointer)
				.Text(" Achievements (")
				.Number((short)totalWonderScore)
				.Text(")");

			if (marker)
			{
				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, (short)wonderTitleVertical, Hues.White);
			}

			this.Owner.World.Participants[0].Tally = (short)(totalWonderScore + totalCitizenScore);
			wondersVertical += 10;

			short humanPlayer = this.Owner.World.PersonParticipantIndex;
			int starshipInhabitants = this.Owner.World.Participants[humanPlayer].StarshipInhabitants;
			int victoryRatio = this.Owner.World.Participants[humanPlayer].StarshipVictoryRatio;

			if (starshipInhabitants > 0 && victoryRatio > 0)
			{
				int spaceshipBonus = ScoreRules.ComputeSpaceshipScore(victoryRatio, starshipInhabitants);

				Message("SpaceShip: (")
					.Number((short)spaceshipBonus);

				if (this.Owner.World.Participants[humanPlayer].StarshipArrivalSeason == this.Owner.World.Season)
				{
					this.Owner.World.Participants[0].Tally += (short)spaceshipBonus;
				}
				else
				{
					this.Owner.Words.Append(" possible");
				}

				this.Owner.Words.Append(")");

				if (marker && wondersVertical < 0xb8)
				{
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, (short)wondersVertical, Hues.White);
				}

				wondersVertical += 10;
			}

			this.Owner.Words.Clear();

			if (this.Owner.World.DirtyCellTally > 0)
			{
				int penalty = ScoreRules.ComputePollutionPenalty(this.Owner.World.DirtyCellTally);
				Message("Pollution: (")
					.Number((short)(-penalty))
					.Text(")   ");
				this.Owner.World.Participants[0].Tally -= (short)penalty;
			}

			if (this.Owner.World.Season > 0 && this.Owner.World.TruceRoundTally > 0)
			{
				int peaceBonus = ScoreRules.ComputePeaceBonus(this.Owner.World.TruceRoundTally);
				this.Owner.Words.Append("Peace: (+");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)peaceBonus, 10));
				this.Owner.Words.Append(") ");
				this.Owner.World.Participants[0].Tally += (short)peaceBonus;
			}

			if (this.Owner.World.Participants[humanPlayer].ComingScienceTally > 0)
			{
				int futureTechnologyBonus = ScoreRules.ComputeFutureTechnologyScore(this.Owner.World.Participants[humanPlayer].ComingScienceTally);
				this.Owner.Words.Append("Future Tech: (+");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)futureTechnologyBonus, 10));
				this.Owner.Words.Append(")");
				this.Owner.World.Participants[0].Tally += (short)futureTechnologyBonus;
			}

			if (this.Owner.Words[0] != 0 && marker && wondersVertical < 0xb8)
			{
				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, (short)wondersVertical, Hues.White);
				wondersVertical += 10;
			}

			int conquestBonus = -1;
			if (Turn.ConquestEnd != 0)
			{
				conquestBonus = ScoreRules.ComputeConquestBonus(this.Owner.World.ComputerRivalTally, this.Owner.World.RoundTally);
			}

			if (this.Owner.World.Participants[0].Tally < 0)
			{
				this.Owner.World.Participants[0].Tally = 0;
			}

			if (conquestBonus != -1 && conquestBonus > this.Owner.World.Participants[0].Tally)
			{
				Message("Bonus Score: ")
					.Number((short)conquestBonus);
				this.Owner.World.Participants[0].Tally = (short)conquestBonus;
			}
			else
			{
				Message("Total Score: ")
					.Number(this.Owner.World.Participants[0].Tally);
			}

			if (marker && wondersVertical < 0xba)
			{
				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, (short)wondersVertical, Hues.White);
			}

			Scoring.HallOfFameScore = (ushort)this.Owner.World.Participants[0].Tally;

			if (marker)
			{
				ushort screenBufferPointer = Screen.Box;
				this.Owner.Services.FloodBox(screenBufferPointer, 35, 192, 250, 7, Hues.DarkGray);

				if (this.Owner.World.Participants[0].Tally > 0)
				{
					this.Owner.Services.FloodBox(screenBufferPointer, 35, 192, (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[0].Tally / 4, 0, 250), 7, Hues.LightCyan);
				}

				DrawingBox.DisplayIndex = 0;

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					0, 0, 0x140, 0xc8,
					screenBufferPointer,
					0, 0);

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();
				this.Owner.Startup.ShowMouseCursor();
				this.Owner.TurnHooks.RepaintMainScreen();
			}

			return (ushort)this.Owner.World.Participants[0].Tally;
		}

		public void DrawScoreIcon(ushort icon)
		{
			using var _ = this.Processor.Journal.Scope($"RenderScoreIcon({icon})");

			short iconLeft = (short)(Scoring.IconHorizontal + 8);
			Scoring.IconHorizontal = (ushort)iconLeft;

			if (iconLeft > 0x124)
			{
				short iconTop = (short)(Scoring.IconVertical + (ushort)Scoring.VerticalShift);
				Scoring.IconVertical = (ushort)iconTop;
				Scoring.IconHorizontal = (ushort)(((iconTop >> 1) & 0x4) + 0x10);
			}

			if ((short)Scoring.IconVertical <= 0xa0)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box,
					(short)Scoring.IconHorizontal,
					(short)Scoring.IconVertical,
					CitizenPicture(icon));
			}
		}

		private ushort FoundCity(short participantIndex, short townIndex, int horizontalLocation, int verticalLocation, ushort townExtent)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			City town = this.Owner.World.Towns[townIndex];

			ushort labelIndex = ChooseCityLabel(participantIndex);

			town.LabelIndex = (byte)labelIndex;

			this.Owner.World.TownPlaces[labelIndex] = new Point(horizontalLocation, verticalLocation);

			if (this.Owner.World.IsHuman(participantIndex))
			{
				if (this.Owner.CityNaming.ShowCityLabelPrompt(townIndex) == 0)
				{
					return 0xffff;
				}

				char[] label = this.Owner.World.TownLabels[town.LabelIndex].ToCharArray();
				label[12] = '\0';
				this.Owner.World.TownLabels[town.LabelIndex] = new string(label);
			}

			TrimCityLabel(town.LabelIndex);

			this.Owner.UnitOperations.AttachReplayContent(1, (byte)participantIndex, town.LabelIndex, (byte)horizontalLocation, (byte)verticalLocation);

			this.Owner.MapOperations.RemoveTileImprovements(0x24, horizontalLocation, verticalLocation);

			ClearCitySquare(horizontalLocation, verticalLocation);

			this.Owner.MapOperations.AssignTileImprovements(9, horizontalLocation, verticalLocation);

			if ((short)Tables.TerrainImprovementWord(this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation), TerrainImprovementTable.Irrigation) < -1)
			{
				this.Owner.MapOperations.AssignTileImprovements(2, horizontalLocation, verticalLocation);
			}

			town.ConditionMarker = 0;
			town.Place = new Point(horizontalLocation, town.Place.Y);
			town.Place = new Point(town.Place.X, verticalLocation);
			town.TrueExtent = (sbyte)townExtent;
			town.ShownExtent = 0;
			town.LaborerMarkersLow = 0;
			town.LaborerMarkersHigh = 0;
			town.RootCommerce = 0;
			town.GrainTally = 0;
			town.MaterialsTally = 0;
			town.StructureMarkersLow = 0;
			town.StructureMarkersHigh = 0;
			town.PresentMakingIndex = 1;

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x22) != 0)
			{
				town.PresentMakingIndex = 4;
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x40) != 0)
			{
				town.PresentMakingIndex = 5;
			}

			town.Unfamiliar[0] = -1;
			town.Unfamiliar[1] = -1;

			bool first = true;

			for (ushort otherIndex = 0; otherIndex < 0x80; otherIndex++)
			{
				if (this.Owner.World.Towns[otherIndex].ParticipantIndex == participantIndex && otherIndex != (ushort)townIndex)
				{
					first = false;
				}
			}

			if (first)
			{
				town.StructureMarkersLow = 1;
				town.StructureMarkersHigh = 0;
			}

			town.RootCommerce = 0;
			town.ParticipantIndex = participantIndex;

			for (int routeIndex = 0; routeIndex < 3; routeIndex++)
			{
				town.CommerceTownIndices[routeIndex] = -1;
			}

			town.SpecialistMarkers = 0;

			MarkCityNeighbourhood(participantIndex, town, horizontalLocation, verticalLocation);

			if (first && participantIndex == person)
			{
				this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstProduct);
			}

			MarkCityPowerArea(participantIndex, town, horizontalLocation, verticalLocation);

			if (participantIndex == person)
			{
				this.Owner.Services.PerformCityFoundedSound();

				if ((this.Owner.World.SessionPreferenceMarkers & 8) != 0)
				{
					if (this.Owner.Startup.AskForDisk(1, 0) != 0)
					{
						this.Owner.Diplomats.ExamineCity(townIndex);
					}
				}

				this.Owner.Advisors.RenewCityScreen(townIndex);

				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				town.PresentMakingIndex = (sbyte)this.Owner.Advisors.PickCityProduction(participantIndex, townIndex);

				if (this.Owner.CityOperations.PlayerEnjoysWonderEffect(person, 0x13) != 0)
				{
					this.Owner.World.ChartSight[town.Place.X, town.Place.Y] |= (sbyte)(byte)(1 << (byte)person);

					town.ShownExtent = 1;

					this.Owner.MapOperations.KeepKnownTileImprovements(town.Place.X, town.Place.Y);
				}
			}

			return (ushort)townIndex;
		}

		private ushort ChooseCityLabel(short participantIndex)
		{
			ushort labelIndex = (ushort)(this.Owner.World.Participants[participantIndex].ChiefVisuals == 16 ? 0 : this.Owner.World.Participants[participantIndex].ChiefVisuals - 1);

			if ((short)labelIndex >= 7)
			{
				labelIndex--;
			}

			labelIndex <<= 4;

			ushort first = labelIndex;
			bool found;

			do
			{
				if (this.Owner.World.TownPlaces[labelIndex].X == -1)
				{
					break;
				}

				found = false;

				ushort end = (ushort)(first + 0x10);
				labelIndex++;

				if (end == labelIndex)
				{
					labelIndex = 0xe0;
				}

				if (labelIndex == 0x100)
				{
					labelIndex = 0;

					if (first == 0x3e7)
					{
						found = true;
					}

					first = 0x3e7;
				}
			}
			while (!found);

			return labelIndex;
		}

		private void TrimCityLabel(byte labelIndex)
		{
			ushort letterIndex = 0xb;

			while (letterIndex > 0 && this.Owner.World.TownLabels[labelIndex][letterIndex] == ' ')
			{
				char[] label = this.Owner.World.TownLabels[labelIndex].ToCharArray();
				label[letterIndex] = '\0';
				this.Owner.World.TownLabels[labelIndex] = new string(label);
				letterIndex--;
			}
		}

		private void MarkCityNeighbourhood(short participantIndex, City town, int horizontalLocation, int verticalLocation)
		{
			ushort neighbourIndex;

			for (neighbourIndex = 1; neighbourIndex <= 8; neighbourIndex++)
			{
				int tileHorizontal = (short)Tables.DirectionHorizontal(neighbourIndex) + horizontalLocation;
				int tileVertical = (short)Tables.DirectionVertical(neighbourIndex) + verticalLocation;

				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(tileHorizontal, tileVertical);

				if (terrain == 0xa &&
					((short)this.Owner.MapOperations.ObtainLandMassSize(tileHorizontal, tileVertical) > 0x100 || participantIndex == this.Owner.World.PersonParticipantIndex))
				{
					town.ConditionMarker |= 2;
				}

				if (terrain == 0xb || terrain == 5)
				{
					town.ConditionMarker |= 8;
				}
			}
		}

		private void MarkCityPowerArea(short participantIndex, City town, int horizontalLocation, int verticalLocation)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			ushort tileIndex;

			for (tileIndex = 0; tileIndex < 0x2d; tileIndex++)
			{
				short tileHorizontal = (short)(ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(tileIndex) + horizontalLocation);
				short tileVertical = (short)(Tables.DirectionVertical(tileIndex) + (ushort)verticalLocation);

				if (tileIndex <= 0x14)
				{
					this.Owner.Services.AssignDot(2, tileHorizontal + 0x50, tileVertical, (ushort)participantIndex);
				}
				else
				{
					ushort colour = this.Owner.VideoCard.ObtainDot(2, tileHorizontal + 0x50, tileVertical);

					if ((short)colour >= 8)
					{
						this.Owner.Services.AssignDot(2, tileHorizontal + 0x50, tileVertical, 8);
					}
				}

				if (participantIndex == person || tileIndex > 0x18)
				{
					continue;
				}

				if ((this.Owner.MapOperations.ObtainTileImprovements(tileHorizontal, tileVertical) & 1) == 0 ||
					this.Owner.MapOperations.ObtainTileOwnerBits(tileHorizontal, tileVertical) != (ushort)person)
				{
					continue;
				}

				town.ShownExtent = 1;

				this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);
			}
		}

		private ushort DestroyRandomBuilding(City town)
		{
			ushort buildingIndex;

			do
			{
				buildingIndex = (ushort)this.Owner.Runtime.Dice.DrawInteger(24);
			}
			while (buildingIndex == 0 || !town.HasStructureSignExtended(buildingIndex));

			ushort keep = (ushort)~(ushort)(1 << (byte)buildingIndex);

			town.StructureMarkersLow &= keep;
			town.StructureMarkersHigh &= (keep & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return buildingIndex;
		}

		private void HandleCityRevolt(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			this.Owner.CityOperations.HandleCity(townIndex, -1);

			this.Owner.Words.Clear();

			if ((short)CityTurn.Happy >= (short)CityTurn.Unhappy)
			{
				return;
			}

			switch (this.Owner.Runtime.Dice.DrawInteger(3))
			{
				case 0:
					this.Owner.Words.Set(0x4fcb);
					break;

				case 1:
					this.Owner.Words.Set(0x4fd7);
					break;

				case 2:
					this.Owner.Words.Set(0x4fe6);
					break;
			}

			this.Owner.Conquest.ObtainCityLabel(townIndex);

			this.Owner.Words.Append("!\nCitizens demand ");

			if (!town.Has(CityStructure.Temple))
			{
				this.Owner.Words.Append("TEMPLE\n");
			}
			else if (!town.Has(CityStructure.Courthouse))
			{
				this.Owner.Words.Append(0x500d);
			}
			else if (!town.Has(CityStructure.Marketplace))
			{
				this.Owner.Words.Append(0x5019);
			}
			else
			{
				this.Owner.Words.Append(this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, 0x1d) != 0 && !town.Has(CityStructure.Cathedral) ? (ushort)0x5026 : (ushort)0x5031);
			}

			if (this.Owner.World.IsHuman(town.ParticipantIndex))
			{
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				this.Owner.Words.Clear();
			}

			town.GrainTally = 0;
			town.MaterialsTally = 0;

			short highest = 4;
			short rivalIndex = -1;

			if ((short)CityTurn.Happy > (short)CityTurn.Unhappy ||
				town.Has(CityStructure.Palace) || this.Owner.World.Participants[town.ParticipantIndex].TownTally <= 3)
			{
				return;
			}

			ushort otherIndex;

			for (otherIndex = 0; otherIndex < 0x80 && otherIndex != (ushort)townIndex && this.Owner.World.Towns[otherIndex].ConditionMarker != 0xff; otherIndex++)
			{
				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(town.Place.X, this.Owner.World.Towns[otherIndex].Place.X, town.Place.Y, this.Owner.World.Towns[otherIndex].Place.Y);

				this.Owner.CityOperations.HandleCity((short)otherIndex, -1);

				short prosperity = CentralProcessor.SignedQuotient((short)((ushort)(CityTurn.Happy - CityTurn.Unhappy) << 5), distance);

				if (prosperity > highest)
				{
					highest = prosperity;
					rivalIndex = (short)otherIndex;
				}
			}

			if (rivalIndex == -1)
			{
				return;
			}

			if (this.Owner.World.Towns[rivalIndex].ParticipantIndex == town.ParticipantIndex)
			{
				return;
			}

			if ((this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.Owner.World.PersonParticipantIndex)) != 0)
			{
				Message("Residents of ")
					.City(townIndex)
					.Text(" admire\nthe prosperity of ")
					.City(rivalIndex)
					.Text(".\n")
					.Dialog(80, 80);
			}

			this.Owner.Diplomats.ComputeInciteCost(townIndex, this.Owner.World.Towns[rivalIndex].ParticipantIndex);

			this.Owner.Words.Clear();
		}

		private void ReportCityDisaster(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			if (this.Owner.Words[0] == 0)
			{
				return;
			}

			if (this.Owner.World.IsHuman(town.ParticipantIndex))
			{
				Screen.DialogStyle = 0x4;

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				return;
			}

			if ((this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.Owner.World.PersonParticipantIndex)) == 0)
			{
				return;
			}

			ushort lineEnd = 0;

			while (this.Processor.PullGlobalByte((ushort)(lineEnd++ + TextBuffer.Address)) != 0xa)
			{
			}

			this.Processor.EmitGlobalByte((ushort)(lineEnd + TextBuffer.Address), 0);
			Screen.DialogStyle = 0x2;

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 64);

			this.Owner.MapOperations.FocusMapView(this.Owner.World.PersonParticipantIndex, town.Place.X - 10, town.Place.Y - 6);
		}
	}
}
