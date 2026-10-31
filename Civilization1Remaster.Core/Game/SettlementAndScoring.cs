using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 20.
	/// </summary>
	public class SettlementAndScoring : GameComponent, ISettlementAndScoring
	{
		public SettlementAndScoring(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void WipeCitySquare(int horizLoc, int vertLoc)
		{
			ushort landKind = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			short freshLand = (short)TerrainImprovementWord(landKind, TerrainImprovementTable.Irrigation);

			if (freshLand >= 0)
			{
				// The same steps as when settlers finish changing the terrain
				this.Owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)freshLand);
				this.Owner.World.ChartSight[horizLoc, vertLoc] |= 1;
				this.Owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
			}
		}

		/// <inheritdoc/>
		public ushort EstablishCity(short participantIdx, ushort unused, int horizLoc, int vertLoc, ushort townExtent)
		{
			this.Processor.Journal.OpenScope($"EstablishCity({participantIdx}, {unused}, {horizLoc}, {vertLoc}, {townExtent})");

			// The final values of the locals stay in the stack frame, like in the original

			this.Owner.Bot.DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 4);

			ushort townIdx;

			for (townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff)
					break;
			}

			ushort result;

			if (townIdx == 0x80)
			{
				if (participantIdx == this.Owner.World.PersonParticipantIdx)
				{
					DialogStyle = 0x4;
					this.Owner.Words.Clear();

					this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicPmax);

					this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 64, 80);
				}

				result = 0xffff;
			}
			else
			{
				result = FoundCity(participantIdx, (short)townIdx, horizLoc, vertLoc, townExtent);
			}

			this.Processor.Journal.CloseScope("EstablishCity");

			return result;
		}

		/// <inheritdoc/>
		public void HandleCityDisasters(short townIdx)
		{
			this.Processor.Journal.OpenScope($"HandleCityDisasters({townIdx})");

			// The final values of the locals stay in the stack frame, like in the original

			City town = this.Owner.World.Towns[townIdx];

			// The terrains of the city's tile and its 8 neighbours
			ushort terrains = 0;
			ushort neighbourIdx;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(
					town.Place.X + (short)DirectionHoriz(neighbourIdx),
					town.Place.Y + (short)DirectionVert(neighbourIdx));

				terrains |= (ushort)(1 << (byte)terrain);
			}


			EmptyMessage()
				.City(townIdx);

			this.Owner.Codes.Set(3, this.Owner.Words.ToString());

			this.Owner.Words.Clear();

			switch (this.Owner.Libc.Dice.DrawInt(10))
			{
				case 0:
					if ((terrains & 0x10) != 0 && (town.StructureMarkers0 & 0xfffe) != 0)
					{
						ushort buildingIdx = DestroyRandomBuilding(town);

						Message("Earthquake in ")
							.City(townIdx)
							.Text("!\n")
							.Text(BuildingText(buildingIdx))
							.Text(" destroyed.\n");
					}
					break;

				case 1:
					if (this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 10) == 0 && !town.Has(CityStructure.Aqueduct) &&
						this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(9, BuildingTable.Technology)) != 0)
					{
						this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicD1);

						if (town.TrueExtent != 0)
							town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
					}
					break;

				case 2:
					if ((terrains & 0x800) != 0)
					{
						if (!town.Has(CityStructure.CityWalls) && this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(8, BuildingTable.Technology)) != 0)
						{
							this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicD2);

							if (town.TrueExtent > 1)
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
						}
					}
					break;

				case 3:
					if ((terrains & 0x20) != 0)
					{
						if (!town.Has(CityStructure.Temple) && this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(4, BuildingTable.Technology)) != 0)
						{
							this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicD3);

							if (town.TrueExtent > 1)
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);
						}
					}
					break;

				case 4:
					if (town.TrueExtent != 0 && !town.Has(CityStructure.Granary) &&
						this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(3, BuildingTable.Technology)) != 0)
					{
						this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicD4);

						town.GrainTally = 0;
						town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);

						this.Owner.Words.Append("Citizens demand GRANARY.\n");
					}
					break;

				case 5:
					if ((town.StructureMarkers0 & 0xfffe) != 0 && !town.Has(CityStructure.Aqueduct) &&
						this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(9, BuildingTable.Technology)) != 0)
					{
						ushort buildingIdx = DestroyRandomBuilding(town);

						this.Owner.Codes.Set(0, BuildingText(buildingIdx));

						this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicD5);
					}
					break;

				case 6:
					if ((terrains & 0x400) != 0)
					{
						if (!town.Has(CityStructure.Barracks))
						{
							this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicD6);

							town.GrainTally = 0;
							town.MaterialsTally = 0;
						}
					}
					break;

				default:
					HandleCityRevolt(townIdx);
					break;
			}

			ReportCityDisaster(townIdx);

			this.Processor.Journal.CloseScope("HandleCityDisasters");
		}

		/// <inheritdoc/>
		public ushort DisplayFinalScore(short participantIdx, bool marker)
		{
			this.Processor.Journal.OpenScope($"DisplayFinalScore({participantIdx}, {marker})");

			if (marker)
			{
				ushort screenBufferPtr = ScreenBox;
				DrawingBox.DisplayIdx = 1;
				this.Owner.Briefings.DisplayReportPreamble(GameTexts.TextCivilizationScore, 3);
			}

			this.Owner.World.Participants[0].Tally = 0;

			if (marker && (this.Owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.Owner.TextPainter.RenderCenteredTextToScreen0(GameTexts.TextScoringCompleted, 160, 100, Hues.White);
				ushort screenBufferPtr = ScreenBox;
				DrawingBox.DisplayIdx = 0;

				this.Owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 0x140, 0xc8,
					screenBufferPtr,
					0, 0);

				this.Owner.Conquest.AwaitForKeyPressOrMouseClick();
				this.Owner.Startup.DisplayMouseCursor();
				this.Owner.TurnHooks.RepaintMainScreen();

				this.Processor.Journal.CloseScope("DisplayFinalScore");
				return 0;
			}

			int citizensY = 0x1a;
			int wondersY = 0x1a;
			ScoreIconHoriz = 0x3e7;
			ScoreIconVert = 0x1a;

			short savedLevyRatio = this.Owner.World.Participants[participantIdx].LevyRatio;
			this.Owner.World.Participants[participantIdx].LevyRatio = (short)(6 - this.Owner.World.Participants[participantIdx].ResearchLevyRatio);

			int totalCitizenScore = 0;
			for (short townIdx = 0; townIdx < 128; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff)
					continue;

				if (this.Owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				this.Owner.CityOps.HandleCity(townIdx, -1);

				int happyCount = CityHappy;
				int unhappyCount = CityUnhappy;
				int specialistCount = CitySpecialists;
				int contentCount = this.Owner.World.Towns[townIdx].TrueExtent - happyCount - unhappyCount - specialistCount;

				if (marker)
				{
					for (int i = 0; i < happyCount; i++)
					{
						RenderScoreIcon((ushort)(i & 1));
					}
					for (int i = 0; i < contentCount; i++)
					{
						RenderScoreIcon((ushort)((i & 1) + 2));
					}
					for (int i = 0; i < unhappyCount; i++)
					{
						RenderScoreIcon((ushort)((i & 1) + 4));
					}
					for (int i = 0; i < specialistCount; i++)
					{
						RenderScoreIcon((ushort)(this.Owner.CityOps.ObtainCitizenType((ushort)i) + 5));
					}
				}

				totalCitizenScore += ScoreRules.ComputeCitizenScore(happyCount, contentCount, specialistCount);
			}

			this.Owner.World.Participants[participantIdx].LevyRatio = savedLevyRatio;
			CitizenScore = (ushort)totalCitizenScore;

			ushort civNamePtr = CivilizationName(participantIdx);
			Message(civNamePtr)
				.Text(" Citizens (")
				.Number((short)totalCitizenScore)
				.Text(")");

			if (marker)
			{
				this.Owner.TextPainter.RenderTextWithShadow(this.Owner.Words.ToString(), 16, (short)citizensY, Hues.White);
			}

			wondersY = ScoreIconVert + 0x10;
			int wonderTitleY = wondersY;
			int wonderColX = 0x3e7;
			int totalWonderScore = 0;

			for (short wonderIdx = 1; wonderIdx <= 21; wonderIdx++)
			{
				short wonderCityIdx = this.Owner.World.MarvelTownIdx[wonderIdx];
				if (wonderCityIdx != -1 && wonderCityIdx != 0x80)
				{
					if (this.Owner.World.Towns[wonderCityIdx].ParticipantIdx == participantIdx)
					{
						wonderColX += 0x98;
						if (wonderColX > 0x12c)
						{
							wondersY += (ushort)this.Owner.Helpers.VerifyValueSpan(ScoreVerticalShift + 2, 8, 99);
							wonderColX = 0x10;
						}

						if (marker && wondersY < 0xb4)
						{
							this.Owner.CityOps.RenderResourceIcon((short)(wonderIdx + 0x18), (short)wonderColX, (short)(wondersY - 2));
							this.Owner.TextPainter.RenderTextToScreen0(WonderText(wonderIdx), (short)(wonderColX + 20), (short)wondersY, Hues.LightCyan);
						}

						totalWonderScore += 20;
					}
				}
			}

			WonderScore = (ushort)totalWonderScore;
			Message(civNamePtr)
				.Text(" Achievements (")
				.Number((short)totalWonderScore)
				.Text(")");

			if (marker)
			{
				this.Owner.TextPainter.RenderTextWithShadow(this.Owner.Words.ToString(), 16, (short)wonderTitleY, Hues.White);
			}

			this.Owner.World.Participants[0].Tally = (short)(totalWonderScore + totalCitizenScore);
			wondersY += 10;

			short humanPlayer = this.Owner.World.PersonParticipantIdx;
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

				if (marker && wondersY < 0xb8)
				{
					this.Owner.TextPainter.RenderTextWithShadow(this.Owner.Words.ToString(), 16, (short)wondersY, Hues.White);
				}

				wondersY += 10;
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
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)peaceBonus, 10));
				this.Owner.Words.Append(") ");
				this.Owner.World.Participants[0].Tally += (short)peaceBonus;
			}

			if (this.Owner.World.Participants[humanPlayer].ComingScienceTally > 0)
			{
				int futureTechBonus = ScoreRules.ComputeFutureTechScore(this.Owner.World.Participants[humanPlayer].ComingScienceTally);
				this.Owner.Words.Append("Future Tech: (+");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)futureTechBonus, 10));
				this.Owner.Words.Append(")");
				this.Owner.World.Participants[0].Tally += (short)futureTechBonus;
			}

			if (this.Owner.Words[0] != 0 && marker && wondersY < 0xb8)
			{
				this.Owner.TextPainter.RenderTextWithShadow(this.Owner.Words.ToString(), 16, (short)wondersY, Hues.White);
				wondersY += 10;
			}

			int conquestBonus = -1;
			if (ConquestEnd != 0)
			{
				conquestBonus = ScoreRules.ComputeConquestBonus(this.Owner.World.BotRivalTally, this.Owner.World.RoundTally);
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

			if (marker && wondersY < 0xba)
			{
				this.Owner.TextPainter.RenderTextWithShadow(this.Owner.Words.ToString(), 16, (short)wondersY, Hues.White);
			}

			HallOfFameScore = (ushort)this.Owner.World.Participants[0].Tally;

			if (marker)
			{
				ushort screenBufferPtr = ScreenBox;
				this.Owner.Services.FloodBox(screenBufferPtr, 35, 192, 250, 7, Hues.DarkGray);

				if (this.Owner.World.Participants[0].Tally > 0)
				{
					this.Owner.Services.FloodBox(screenBufferPtr, 35, 192, (short)this.Owner.Helpers.VerifyValueSpan(this.Owner.World.Participants[0].Tally / 4, 0, 250), 7, Hues.LightCyan);
				}

				DrawingBox.DisplayIdx = 0;

				this.Owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 0x140, 0xc8,
					screenBufferPtr,
					0, 0);

				this.Owner.Conquest.AwaitForKeyPressOrMouseClick();
				this.Owner.Startup.DisplayMouseCursor();
				this.Owner.TurnHooks.RepaintMainScreen();
			}

			this.Processor.Journal.CloseScope("DisplayFinalScore");
			return (ushort)this.Owner.World.Participants[0].Tally;
		}

		/// <inheritdoc/>
		public void RenderScoreIcon(ushort icon)
		{
			this.Processor.Journal.OpenScope($"RenderScoreIcon({icon})");

			short iconLeft = (short)(ScoreIconHoriz + 8);
			ScoreIconHoriz = (ushort)iconLeft;

			if (iconLeft > 0x124)
			{
				short iconTop = (short)(ScoreIconVert + (ushort)ScoreVerticalShift);
				ScoreIconVert = (ushort)iconTop;
				ScoreIconHoriz = (ushort)(((iconTop >> 1) & 0x4) + 0x10);
			}

			if ((short)ScoreIconVert <= 0xa0)
			{
				this.Owner.Services.RenderPictureToScreen(ScreenBox,
					(short)ScoreIconHoriz,
					(short)ScoreIconVert,
					CitizenPicture(icon));
			}

			this.Processor.Journal.CloseScope("RenderScoreIcon");
		}

		/// <summary>The city in the free slot (see EstablishCity); -1 when the human player cancels its name</summary>
		private ushort FoundCity(short participantIdx, short townIdx, int horizLoc, int vertLoc, ushort townExtent)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			City town = this.Owner.World.Towns[townIdx];

			ushort labelIdx = ChooseCityLabel(participantIdx);

			town.LabelIdx = (byte)labelIdx;

			this.Owner.World.TownPlaces[labelIdx] = new Point(horizLoc, vertLoc);

			if (this.Owner.World.IsHuman(participantIdx))
			{
				if (this.Owner.CityNaming.DisplayCityLabelPrompt(townIdx) == 0)
					return 0xffff;

				char[] label = this.Owner.World.TownLabels[town.LabelIdx].ToCharArray();
				label[12] = '\0';
				this.Owner.World.TownLabels[town.LabelIdx] = new string(label);
			}

			TrimCityLabel(town.LabelIdx);

			this.Owner.UnitOps.AttachReplayContent(1, (byte)participantIdx, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			this.Owner.MapOps.DiscardTileImprovements(0x24, horizLoc, vertLoc);

			// Remaster: the land under a new city is cleared. Forest, jungle and swamp change the same way
			// as when settlers irrigate them (forest to plains, jungle and swamp to grassland).
			WipeCitySquare(horizLoc, vertLoc);

			this.Owner.MapOps.AssignTileImprovements(9, horizLoc, vertLoc);

			if ((short)TerrainImprovementWord(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc), TerrainImprovementTable.Irrigation) < -1)
			{
				this.Owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);
			}

			town.ConditionMarker = 0;
			town.Place = new Point(horizLoc, town.Place.Y);
			town.Place = new Point(town.Place.X, vertLoc);
			town.TrueExtent = (sbyte)townExtent;
			town.ShownExtent = 0;
			town.LaborerMarkers0 = 0;
			town.LaborerMarkers1 = 0;
			town.RootCommerce = 0;
			town.GrainTally = 0;
			town.MaterialsTally = 0;
			town.StructureMarkers0 = 0;
			town.StructureMarkers1 = 0;
			town.PresentMakingIdx = 1;

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22) != 0)
				town.PresentMakingIdx = 4;

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x40) != 0)
				town.PresentMakingIdx = 5;

			town.Unfamiliar[0] = -1;
			town.Unfamiliar[1] = -1;

			bool first = true;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				if (this.Owner.World.Towns[otherIdx].ParticipantIdx == participantIdx && otherIdx != (ushort)townIdx)
					first = false;
			}


			if (first)
			{
				town.StructureMarkers0 = 1;
				town.StructureMarkers1 = 0;
			}

			town.RootCommerce = 0;
			town.ParticipantIdx = participantIdx;

			for (int routeIdx = 0; routeIdx < 3; routeIdx++)
				town.CommerceTownIndices[routeIdx] = -1;

			town.LaborerMarkers2 = 0;

			MarkCityNeighbourhood(participantIdx, town, horizLoc, vertLoc);

			if (first && participantIdx == person)
			{
				this.Owner.HelpSystem.DisplayHelpPopup(GameTexts.TopicFirstproduct);
			}

			MarkCityPowerArea(participantIdx, town, horizLoc, vertLoc);

			if (participantIdx == person)
			{
				// Sound of chopping wood while the new city is built
				this.Owner.Services.PerformCityFoundedSound();

				if ((this.Owner.World.SessionPrefMarkers & 8) != 0)
				{
					if (this.Owner.Startup.QueryForDisk(1, 0) != 0)
						this.Owner.Diplomats.ExamineCity(townIdx);
				}

				this.Owner.Advisors.RenewCityScreen(townIdx);

				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				town.PresentMakingIdx = (sbyte)this.Owner.Advisors.PickCityProduction(participantIdx, townIdx);

				if (this.Owner.CityOps.PlayerEnjoysWonderEffect(person, 0x13) != 0)
				{
					this.Owner.World.ChartSight[town.Place.X, town.Place.Y] |= (sbyte)(byte)(1 << (byte)person);

					town.ShownExtent = 1;

					this.Owner.MapOps.KeepKnownTileImprovements(town.Place.X, town.Place.Y);
				}
			}

			return (ushort)townIdx;
		}

		/// <summary>
		/// The next free name of the leader's 16 (16 * (ChiefVisuals - 1), one less from the 8th leader on), then the shared
		/// names 0xe0 - 0xff, then from 0 on; all taken: name 0
		/// </summary>
		private ushort ChooseCityLabel(short participantIdx)
		{
			ushort labelIdx = (ushort)(this.Owner.World.Participants[participantIdx].ChiefVisuals - 1);

			if ((short)labelIdx >= 7)
				labelIdx--;

			labelIdx <<= 4;

			ushort first = labelIdx;
			bool found;

			do
			{
				found = true;

				if (this.Owner.World.TownPlaces[labelIdx].X == -1)
					break;

				found = false;

				ushort end = (ushort)(first + 0x10);
				labelIdx++;

				if (end == labelIdx)
					labelIdx = 0xe0;

				if (labelIdx == 0x100)
				{
					labelIdx = 0;

					if (first == 0x3e7)
						found = true;

					first = 0x3e7;
				}
			}
			while (!found);


			return labelIdx;
		}

		/// <summary>The trailing spaces of the name (from its 12th letter back) become ends of text</summary>
		private void TrimCityLabel(byte labelIdx)
		{
			ushort letterIdx = 0xb;

			while (letterIdx > 0 && this.Owner.World.TownLabels[labelIdx][letterIdx] == ' ')
			{
				char[] label = this.Owner.World.TownLabels[labelIdx].ToCharArray();
				label[letterIdx] = '\0';
				this.Owner.World.TownLabels[labelIdx] = new string(label);
				letterIdx--;
			}

		}

		/// <summary>The flags of the city's neighbours: 2 coastal (big ocean, or any for the human player), 8 river or hills</summary>
		private void MarkCityNeighbourhood(short participantIdx, City town, int horizLoc, int vertLoc)
		{
			ushort neighbourIdx;

			for (neighbourIdx = 1; neighbourIdx <= 8; neighbourIdx++)
			{
				int tileHoriz = (short)DirectionHoriz(neighbourIdx) + horizLoc;
				int tileVert = (short)DirectionVert(neighbourIdx) + vertLoc;

				ushort terrain = this.Owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);

				if (terrain == 0xa &&
					((short)this.Owner.MapOps.ObtainLandMassSize(tileHoriz, tileVert) > 0x100 || participantIdx == this.Owner.World.PersonParticipantIdx))
				{
					town.ConditionMarker |= 2;
				}

				if (terrain == 0xb || terrain == 5)
				{
					town.ConditionMarker |= 8;
				}
			}

		}

		/// <summary>
		/// The power map (screen 2, 0x50 to the right): the 21 tiles of the city in the owner's colour, the next ones at most
		/// colour 8; an AI city next to a city of the human player becomes known to him
		/// </summary>
		private void MarkCityPowerArea(short participantIdx, City town, int horizLoc, int vertLoc)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			ushort tileIdx;

			for (tileIdx = 0; tileIdx < 0x2d; tileIdx++)
			{
				short tileHoriz = (short)(ushort)this.Owner.Routing.TunePosition((short)DirectionHoriz(tileIdx) + horizLoc);
				short tileVert = (short)(DirectionVert(tileIdx) + (ushort)vertLoc);


				if (tileIdx <= 0x14)
				{
					this.Owner.Services.AssignDot(2, tileHoriz + 0x50, tileVert, (ushort)participantIdx);
				}
				else
				{
					ushort colour = this.Owner.VideoCard.ObtainDot(2, tileHoriz + 0x50, tileVert);

					if ((short)colour >= 8)
						this.Owner.Services.AssignDot(2, tileHoriz + 0x50, tileVert, 8);
				}

				if (participantIdx == person || tileIdx > 0x18)
					continue;

				if ((this.Owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert) & 1) == 0 ||
					this.Owner.MapOps.ObtainTileOwnerBits(tileHoriz, tileVert) != (ushort)person)
					continue;

				town.ShownExtent = 1;

				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			}

		}

		/// <summary>
		/// A random building of the city (1 - 23, read like the original: a 16-bit mask sign extended) is destroyed; its number
		/// (frame 0x10)
		/// </summary>
		private ushort DestroyRandomBuilding(City town)
		{
			ushort buildingIdx;

			do
			{
				buildingIdx = (ushort)this.Owner.Libc.Dice.DrawInt(24);
			}
			while (buildingIdx == 0 || !town.HasStructureSignExtended(buildingIdx));

			ushort keep = (ushort)~(ushort)(1 << (byte)buildingIdx);

			town.StructureMarkers0 &= keep;
			town.StructureMarkers1 &= (keep & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return buildingIdx;
		}

		/// <summary>
		/// The revolt (three times in ten): when the unhappy outnumber the happy the citizens demand a building (a human player
		/// sees it), food and production are lost and, unless it is a capital of a civilization with more than 3 cities, the
		/// city of another player with the most prosperity (from the first cities up to the first destroyed one or this city)
		/// may buy it (ComputeInciteCost)
		/// </summary>
		private void HandleCityRevolt(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.CityOps.HandleCity(townIdx, -1);

			this.Owner.Words.Clear();

			if ((short)CityHappy >= (short)CityUnhappy)
				return;

			switch (this.Owner.Libc.Dice.DrawInt(3))
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

			this.Owner.Conquest.ObtainCityLabel(townIdx);

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
				this.Owner.Words.Append(this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x1d) != 0 &&
					!town.Has(CityStructure.Cathedral) ? (ushort)0x5026 : (ushort)0x5031);
			}

			if (this.Owner.World.IsHuman(town.ParticipantIdx))
			{
				this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.Owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				this.Owner.Words.Clear();
			}

			town.GrainTally = 0;
			town.MaterialsTally = 0;

			short highest = 4;
			short rivalIdx = -1;


			if ((short)CityHappy > (short)CityUnhappy ||
				town.Has(CityStructure.Palace) || this.Owner.World.Participants[town.ParticipantIdx].TownTally <= 3)
				return;

			ushort otherIdx;

			for (otherIdx = 0; otherIdx < 0x80 && otherIdx != (ushort)townIdx && this.Owner.World.Towns[otherIdx].ConditionMarker != 0xff; otherIdx++)
			{
				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(town.Place.X, this.Owner.World.Towns[otherIdx].Place.X, town.Place.Y, this.Owner.World.Towns[otherIdx].Place.Y);

				this.Owner.CityOps.HandleCity((short)otherIdx, -1);

				short prosperity = CPU.SignedQuotient((short)((ushort)(CityHappy - CityUnhappy) << 5), distance);

				if (prosperity > highest)
				{
					highest = prosperity;
					rivalIdx = (short)otherIdx;

				}
			}


			if (rivalIdx == -1)
				return;

			if (this.Owner.World.Towns[rivalIdx].ParticipantIdx == town.ParticipantIdx)
				return;

			if ((this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.Owner.World.PersonParticipantIdx)) != 0)
			{
				Message("Residents of ")
					.City(townIdx)
					.Text(" admire\nthe prosperity of ")
					.City(rivalIdx)
					.Text(".\n")
					.Dialog(80, 80);
			}

			this.Owner.Diplomats.ComputeInciteCost(townIdx, this.Owner.World.Towns[rivalIdx].ParticipantIdx);

			this.Owner.Words.Clear();
		}

		/// <summary>
		/// The message left in TextBuffer.Address: a human player's city shows it; a city the human player only sees shows its first line
		/// and the map moves there
		/// </summary>
		private void ReportCityDisaster(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (this.Owner.Words[0] == 0)
				return;

			if (this.Owner.World.IsHuman(town.ParticipantIdx))
			{
				DialogStyle = 0x4;

				this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.Owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				return;
			}

			if ((this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.Owner.World.PersonParticipantIdx)) == 0)
				return;

			ushort lineEnd = 0;

			while (this.Processor.PullGlobalByte((ushort)(lineEnd++ + TextBuffer.Address)) != 0xa)
			{
			}

			this.Processor.EmitGlobalByte((ushort)(lineEnd + TextBuffer.Address), 0);
			DialogStyle = 0x2;

			this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 64);

			this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx, town.Place.X - 10, town.Place.Y - 6);
		}
	}
}
