using System;
using System.Collections.Generic;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Core.UI.Reports
{
	public class AdvisorScreens : GameComponent, IAdvisorScreens
	{
		private readonly short[] talkedThisRound = [-1, -1, -1, -1, -1, -1, -1, -1];

		public AdvisorScreens(IGameEngine owner)
			: base(owner)
		{
		}

		public void ShowReportPreamble(GameText title, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayReportPreamble(0x{title.Address:x4}, {style})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 0, 0x140, 0xc8,
				Screen.BackScreenBox,
				0, 0);

			DrawReportPreamble(title, style);
		}

		public void DrawReportPreamble(GameText title, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"RenderReportPreamble(0x{title.Address:x4}, {style})");

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];
			ushort personSlot = (ushort)(this.Owner.World.PersonParticipantIndex << 1);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, style);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(title, 160, 2, Hues.White);

			switch (person.RegimeKind / 2)
			{
				case 0:
					this.Owner.Words.Set(0x473c);
					break;

				case 1:
					this.Owner.Words.Set(0x4743);
					break;

				case 2:
					this.Owner.Words.Set(0x474b);
					break;
			}

			this.Owner.Words.Append(" of the ");
			this.Owner.Words.Append(Tables.CivilizationPluralAt(personSlot));

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 10, Hues.White);

			Message(Tables.GovernmentTitle(person.RegimeKind))
				.Text(" ")
				.Text(Tables.LeaderNameAt(personSlot))
				.Text(": ");

			this.Owner.TurnHooks.FormatCurrentYear();

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 18, Hues.White);
		}

		public void FinishReportScreen()
		{
			using var _ = this.Processor.Journal.Scope("FinishReportScreen()");

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowScienceReport(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayScienceReport({participantIndex})");

			ShowReportPreamble(GameTexts.TextScienceReport, 1);

			short totalScience = 0;
			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];
				if (town.ConditionMarker != 0xff && town.ParticipantIndex == participantIndex && (town.ConditionMarker & 1) == 0)
				{
					this.Owner.CityOperations.HandleCity(townIndex, -1);
					totalScience += (short)CityTurn.Science;
				}
			}

			if (this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 0x14) != 0)
			{
				totalScience += (short)(totalScience / 2);
			}

			short knownTally = this.Owner.World.Participants[participantIndex].FoundScienceTally;
			short costFactor = (short)((this.Owner.World.HardnessTier << 1) + Turn.TechnologyLevel + 6);

			if (costFactor < (short)(0xb - knownTally))
			{
				costFactor = (short)(0xb - knownTally);
			}

			short cost = (short)(knownTally * (this.Owner.World.Season >= 0 ? 2 : 1) * costFactor);
			int step = this.Owner.Helpers.ClampValue(2432 / (cost + 1), 1, 64);
			int barBreadth = this.Owner.Helpers.ClampValue(((step * cost) / 8) + 8, 8, 304);
			short barLeft = (short)(160 - (barBreadth / 2));

			if (barLeft > 0x28)
			{
				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0x118, 0x8c, 0x28, 0x3c, Screen.Box, 0x116, 0x2);
			}

			this.Owner.Services.FloodBox(Screen.Box, barLeft, 25, barBreadth, 16, Hues.LightBlue);

			short progress = 0;

			for (short bulbIndex = 0; (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[participantIndex].InquiryHeadway, 0, cost) > bulbIndex; bulbIndex++)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, (progress / 8) + barLeft, 33,
					Tables.SpritePicture(0xc));

				progress += (short)step;
			}

			short headway = this.Owner.World.Participants[participantIndex].InquiryHeadway;
			short inquiry = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].PresentInquiryIndex;

			if (inquiry != -1)
			{
				int needed = Math.Max(0, cost - headway);
				string pace = totalScience > 0 ? $"{needed} left (+{totalScience}/t -> {(needed + totalScience - 1) / totalScience}t)" : $"{headway}/{cost} bulbs";

				Message("Researching ")
					.Text(GameTables.TechnologyText(inquiry))
					.Text($" ({pace})");

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 161, 26, Hues.Black);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 26, Hues.White);
			}
			else
			{
				Message("No Research")
					.Text(totalScience > 0 ? $" (+{totalScience} bulbs/turn)" : string.Empty);

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 161, 26, Hues.Black);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 26, Hues.White);
			}

			short verticalLocation = 0x2a;
			short shown = 0;

			for (short technologyIndex = 0; technologyIndex < 0x48; technologyIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, technologyIndex) == 0)
				{
					continue;
				}

				this.Owner.Words.Set(GameTables.TechnologyText(technologyIndex));

				ushort hue = (ushort)((this.Owner.World.ScienceLeadingFoundVia[technologyIndex] & 7) == participantIndex ? 0xf : 0xb);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), ((shown % 3) * 100) + 8, verticalLocation, hue);

				shown++;

				if (shown % 3 == 0)
				{
					verticalLocation += 7;
				}

				if (verticalLocation > 0xc0)
				{
					break;
				}
			}

			FinishReportScreen();
		}

		public void DrawUnitCountsChart(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitCountsChart({participantIndex})");

			ShowReportPreamble(GameTexts.TextMilitaryStatus, 1);

			int rowIndex = 0;
			int verticalLocation = 0x20;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				short active = this.Owner.World.Participants[participantIndex].LiveTroops[kind];
				short making = this.Owner.World.Participants[participantIndex].TroopsInsideMaking[kind];

				if (active <= 0 && making <= 0)
				{
					continue;
				}

				this.Owner.VideoCard.RenderLine(Screen.Box, 36, verticalLocation - 2, 319, verticalLocation - 2, Hues.LightBlue);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, ((rowIndex & 1) << 4) + 2, verticalLocation - 4,
					Tables.SpritePicture((participantIndex << 5) + kind + 0x40));

				rowIndex++;

				ushort record = (ushort)((kind * UnitKindTable.Stride) + UnitKindTable.Origin);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(record, 36, verticalLocation, Hues.White);

				this.Owner.Words.Set("(");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.AssaultPower)), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.GuardPower)), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.ShiftTally)), 10));
				this.Owner.Words.Append(")");

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 112, verticalLocation, Hues.LightCyan);

				if (active > 0)
				{
					EmptyMessage()
						.Number(active)
						.Text(" active");
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 168, verticalLocation, Hues.White);
				}

				if (making > 0)
				{
					EmptyMessage()
						.Number(making)
						.Text(" in production");
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 232, verticalLocation, Hues.LightCyan);
				}

				verticalLocation += 9;
			}

			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xa0, 0x8c, 0x28, 0x3c, Screen.Box, 0x116, 0x2);

			FinishReportScreen();

			DrawPlayerComparisonChart(participantIndex);
		}

		public void DrawPlayerComparisonChart(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPlayerComparisonChart({participantIndex})");

			ShowReportPreamble(GameTexts.TextMilitaryLosses, 4);

			int rowIndex = 0;
			int verticalLocation = 0x1c;

			for (int kind = 0; kind < 0x1b; kind++)
			{
				bool lost = false;

				for (int playerIndex = 0; playerIndex < 8; playerIndex++)
				{
					if (this.Owner.World.Participants[playerIndex].MissingTroops[kind] != 0)
					{
						lost = true;
					}
				}

				if (!lost)
				{
					continue;
				}

				this.Owner.VideoCard.RenderLine(Screen.Box, 36, verticalLocation - 1, 319, verticalLocation - 1, Hues.DarkGray);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, ((rowIndex & 1) << 4) + 2, verticalLocation - 4,
					Tables.SpritePicture((participantIndex << 5) + kind + 0x40));

				rowIndex++;

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTables.UnitText(kind), 36, verticalLocation, Hues.White);

				short losses = this.Owner.World.Participants[participantIndex].MissingTroops[kind];

				if (losses != 0)
				{
					EmptyMessage()
						.Number(losses);
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 104, verticalLocation, Hues.LightRed);
				}

				int horizontalLocation = 0x68;

				for (short playerIndex = 1; playerIndex < 8; playerIndex++)
				{
					if (playerIndex == participantIndex)
					{
						continue;
					}

					horizontalLocation += 0x1c;
					losses = this.Owner.World.Participants[playerIndex].MissingTroops[kind];

					if (losses != 0)
					{
						EmptyMessage()
							.Number(losses);
						this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation, verticalLocation,
							Tables.PlayerColour(playerIndex));
					}
				}

				verticalLocation += 7;
			}

			this.Owner.Dialogs.DrawBox(100, 25, 18, verticalLocation - 25,
				Tables.PlayerColour(this.Owner.World.PersonParticipantIndex));

			FinishReportScreen();
		}

		public void ShowTradeReport(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayTradeReport({participantIndex})");

			ushort[] costs = new ushort[0x18];

			short palaceCost = (short)(this.Owner.World.HardnessTier / 2);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x22) != 0)
			{
				palaceCost++;
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x25) != 0)
			{
				palaceCost++;
			}

			ShowReportPreamble(GameTexts.TextTradeReport, 2);

			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xc8, 0x8c, 0x28, 0x3c, Screen.Box, 0x116, 2);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextCityTrade, 8, 32, Hues.White);

			short totalLuxury = 0;
			short income = 0;
			short science = 0;

			for (int structureIndex = 0; structureIndex < 0x18; structureIndex++)
			{
				costs[structureIndex] = 0x0;
			}

			short verticalLocation = 0x28;

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
				{
					continue;
				}

				if (verticalLocation >= 0xa8)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextTradeReportSecond, 2);

					verticalLocation = 0x28;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				EmptyMessage()
					.City(townIndex);

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 62);

				if (verticalLocation < 0xb4)
				{
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, verticalLocation, Hues.White);
				}

				this.Owner.Words.Clear();

				if ((town.ConditionMarker & 1) != 0)
				{
					this.Owner.Words.Append(" \x008c(Disorder)");
				}
				else
				{
					this.Owner.Words.Append(" ");

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)CityTurn.Luxuries, 10));

					this.Owner.Words.Append("\\/");

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)CityTurn.Taxes, 10));

					this.Owner.Words.Append("$/");

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)CityTurn.Science, 10));

					this.Owner.Words.Append("~");

					totalLuxury += (short)CityTurn.Luxuries;
					income += (short)CityTurn.Taxes;
					science += (short)CityTurn.Science;
				}

				if (verticalLocation < 0xb4)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 80, verticalLocation, Hues.LightGreen);

					verticalLocation += 8;
				}

				for (int structureIndex = 1; structureIndex < 0x18; structureIndex++)
				{
					uint structure = 1u << structureIndex;

					if (((ushort)structure & town.StructureMarkersLow) == 0 && ((ushort)(structure >> 16) & town.StructureMarkersHigh) == 0)
					{
						continue;
					}

					costs[structureIndex] = (ushort)(costs[structureIndex] + Tables.BuildingWord(structureIndex + 1, BuildingTable.Upkeep));

					if (structureIndex == 1)
					{
						costs[structureIndex] = (ushort)(costs[structureIndex] + palaceCost);
					}
				}
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextMaintenanceCosts, 160, 32, Hues.White);

			short rightLocation = 0x28;
			short total = 0;

			for (int structureIndex = 0; structureIndex < 0x18; structureIndex++)
			{
				short cost = (short)costs[structureIndex];

				if (cost == 0)
				{
					continue;
				}

				this.Owner.Words.Clear();

				short tally = structureIndex == 1 ? (short)(cost / palaceCost) : (short)(cost / (short)Tables.BuildingWord(structureIndex + 1, BuildingTable.Upkeep));

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(tally, 10));

				this.Owner.Words.Append(" ");

				this.Owner.Words.Append(GameTables.BuildingText(structureIndex));

				this.Owner.Words.Append(", ");

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(cost, 10));

				this.Owner.Words.Append("$");

				if (rightLocation < 0xb4)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 160, rightLocation, Hues.Yellow);

					rightLocation += 8;
				}

				total += cost;
			}

			Message("Total Cost: ")
				.Number(total)
				.Text("$");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 160, rightLocation + 4, Hues.Yellow);

			if (this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 0x14) != 0)
			{
				science += (short)(science / 2);
			}

			short summaryY = (short)Math.Min((int)verticalLocation + 4, 160);

			Message("Total Luxury: ")
				.Number(totalLuxury)
				.Text("\\");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, summaryY, Hues.LightGreen);

			Message("Total Income: ")
				.Number(income)
				.Text("$");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, summaryY + 8, Hues.LightGreen);

			short headway = this.Owner.World.Participants[participantIndex].InquiryHeadway;
			short knownTally = this.Owner.World.Participants[participantIndex].FoundScienceTally;
			short costFactor = (short)((this.Owner.World.HardnessTier << 1) + Turn.TechnologyLevel + 6);
			if (costFactor < (short)(0xb - knownTally))
			{
				costFactor = (short)(0xb - knownTally);
			}

			short technologyCost = (short)(knownTally * (this.Owner.World.Season >= 0 ? 2 : 1) * costFactor);
			int needed = Math.Max(0, technologyCost - headway);
			short inquiry = this.Owner.World.Participants[participantIndex].PresentInquiryIndex;

			string turnsDescription;
			if (science <= 0)
			{
				turnsDescription = "no research";
			}
			else if (inquiry == -1)
			{
				turnsDescription = "no project";
			}
			else
			{
				int turnsLeft = TradeRules.ComputeResearchTurns(needed, science);
				turnsDescription = turnsLeft == 1 ? "1 turn left" : $"{turnsLeft} turns left";
			}

			if (science > 0)
			{
				Message("Total Science: +")
					.Number(science)
					.Text($"~/t ({turnsDescription})");
			}
			else
			{
				Message("Total Science: 0~/t (no research)");
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, summaryY + 16, Hues.LightGreen);

			short netCash = (short)TradeRules.ComputeNetIncome(income, total);
			string netSign = netCash >= 0 ? "+" : string.Empty;

			Message("Total Net Income: ")
				.Text($"{netSign}{netCash}$");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, summaryY + 24, netCash >= 0 ? Hues.LightGreen : Hues.LightRed);

			FinishReportScreen();
		}

		public void ShowMilitaryReport()
		{
			using var _ = this.Processor.Journal.Scope("DisplayMilitaryReport()");

			short[] rowPlayerList = [-1, -1, -1, -1, -1, -1, -1, -1];

			ShowReportPreamble(GameTexts.TextIntelligenceReport, 1);

			ushort rowCount = 0;
			bool reportShown = false;
			ushort participantIndex;

			for (participantIndex = 1; (short)participantIndex < 8; participantIndex++)
			{
				if ((this.Owner.World.LiveRealms & (1 << (byte)participantIndex)) == 0)
				{
					continue;
				}

				short verticalLocation = (short)((0x18 * rowCount) + 0x20);

				this.Owner.VideoCard.RenderLine(Screen.Box, 4, (short)(verticalLocation - 2), 316, (short)(verticalLocation - 2), Hues.LightBlue);

				this.Owner.Words.Set(Tables.CivilizationPlural(participantIndex));

				if ((short)participantIndex != this.Owner.World.PersonParticipantIndex &&
					(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Relations[participantIndex] & 0x40) == 0)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextNoEmbassyEstablished, 96, verticalLocation, Tables.PlayerColour(participantIndex));

					rowCount++;
					continue;
				}

				DrawMilitaryReportRow(participantIndex, verticalLocation);

				rowPlayerList[rowCount] = (short)participantIndex;
				rowCount++;

				if ((short)participantIndex == this.Owner.World.PersonParticipantIndex)
				{
					continue;
				}

				Message("INFO")
					.Number((short)rowCount);

				short boxTop = (short)(0x18 * rowCount);

				this.Owner.CityOperations.DrawLabelBox(281, boxTop + 20, 318, boxTop + 29, TextBuffer.Address, Tables.PlayerColour(participantIndex));
			}

			this.Owner.Startup.ShowMouseCursor();

			ushort done;

			do
			{
				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.MenuPointerState == 0 && this.Owner.Runtime.KeyAvailable() == 0);

				done = 1;

				short rowIndex = -1;
				bool chosen = false;

				if (this.Owner.Runtime.KeyAvailable() != 0)
				{
					ushort key = this.Owner.Dialogs.ReadNavigationKey();

					if ((short)key >= 0x31 && (short)key <= 0x37)
					{
						rowIndex = (short)(key - 0x31);
						chosen = true;
					}
				}

				if (!chosen && (short)this.Owner.MenuPointerHorizontal > 0x119)
				{
					rowIndex = CentralProcessor.SignedQuotient((short)(this.Owner.MenuPointerVertical - 0x1c), 0x18);
					chosen = true;
				}

				if (chosen && rowIndex >= 0 && rowIndex < (short)rowCount)
				{
					ushort rowPlayerIndex = (ushort)rowPlayerList[rowIndex];

					if ((short)rowPlayerIndex != -1 &&
						(short)rowPlayerIndex != this.Owner.World.PersonParticipantIndex &&
						(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Relations[rowPlayerIndex] & 0x40) != 0)
					{
						ShowIntelligenceReport((short)rowPlayerIndex);

						reportShown = true;
						done = 0;
					}
				}

				participantIndex = this.Owner.MenuPointerHorizontal;

				this.Owner.TurnLogic.WaitForKey();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.Services.AssignMousePosition(270, this.Owner.MenuPointerVertical);

				this.Owner.Startup.ShowMouseCursor();
			}
			while (done == 0);

			this.Owner.Startup.HideMouseCursor();

			if (reportShown)
			{
				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowIntelligenceReport(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayIntelligenceReport({participantIndex})");

			Player participant = this.Owner.World.Participants[participantIndex];

			ShowReportPreamble(GameTexts.TextIntelligenceReportSecond, 1);

			short verticalLocation = 0x20;

			Message("Subject: the ")
				.Text(Tables.CivilizationPlural(participantIndex));

			this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, 32, Hues.White);

			verticalLocation += 0xc;

			Message("Leader:\x008f ")
				.Text(Tables.GovernmentTitle(participant.RegimeKind))
				.Text(" ")
				.Text(Tables.LeaderName(participantIndex));

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation, Hues.LightBlue);

			verticalLocation += 8;

			GameText[] traits =
			{
				new(0x48d3, "Aggressive"), new(0x48de, "Friendly"), new(0x48e7, "Expansionistic"),
				new(0x48f6, "Perfectionist"), new(0x4904, "Civilized"), new(0x490e, "Militaristic")
			};

			for (int traitIndex = 0; traitIndex < 6; traitIndex++)
			{
				ushort trait = Tables.LeaderWord(participant.ChiefVisuals, LeaderTable.Aggression + ((traitIndex >> 1) << 1));

				if (trait == ((traitIndex & 1) == 0 ? 1 : 0xffff))
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(traits[traitIndex], 24, verticalLocation, Hues.LightGray);

					verticalLocation += 8;
				}
			}

			verticalLocation += 4;

			short capitalIndex = -1;

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ParticipantIndex == participantIndex && town.ConditionMarker != 0xff && town.Has(CityStructure.Palace))
				{
					capitalIndex = townIndex;
				}
			}

			Message("Capital:\x008f ")
				.City(capitalIndex);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation, Hues.LightBlue);

			verticalLocation += 8;

			Message("Government:\x008f ")
				.Text(Tables.GovernmentName(participant.RegimeKind));

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation, Hues.LightBlue);

			verticalLocation += 8;

			Message("Treasury:\x008f ")
				.Number(participant.Gold)
				.Text("$");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation, Hues.LightBlue);

			verticalLocation += 8;

			if (this.Owner.World.HardnessTier <= 1)
			{
				Message("Military:\x008f ")
					.Number(participant.TroopTally)
					.Text(" Units.");

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation, Hues.LightBlue);

				verticalLocation += 0xc;
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextForeignAffairs, 16, verticalLocation, Hues.LightBlue);

			verticalLocation += 8;

			for (int otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if ((participant.Relations[otherIndex] & 2) != 0)
				{
					this.Owner.Words.Set("At\x008f PEACE");
				}

				if ((participant.Relations[otherIndex] & 3) == 1)
				{
					this.Owner.Words.Set("At\x008c WAR");
				}

				if ((participant.Relations[otherIndex] & 1) == 0)
				{
					continue;
				}

				this.Owner.Words.Append("\x0087 with the ");

				this.Owner.Words.Append(Tables.CivilizationPlural(otherIndex));

				this.Owner.Words.Append(".");

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 24, verticalLocation, Hues.LightGray);

				verticalLocation += 8;
			}

			verticalLocation += 4;

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextTechnologies, 16, verticalLocation, Hues.LightBlue);

			verticalLocation += 8;

			for (short scienceIndex = 0x47; scienceIndex >= 0; scienceIndex--)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) == 0)
				{
					continue;
				}

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTables.TechnologyText(scienceIndex), 24, verticalLocation, Hues.LightGray);

				verticalLocation += 8;

				if (verticalLocation >= 0xc0)
				{
					break;
				}
			}

			FinishReportScreen();
		}

		public void ShowAttitudeReport(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayAttitudeReport({participantIndex})");

			ShowReportPreamble(GameTexts.TextAttitudeSurvey, 9);

			short verticalLocation = 0x20;
			short unhappy = 0;
			short happy = 0;
			short citizens = 0;
			short shift = 0;

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff || this.Owner.World.Towns[townIndex].ParticipantIndex != participantIndex)
				{
					continue;
				}

				if (verticalLocation > 0xbe)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextAttitudeSurveySecond, 9);

					verticalLocation = 0x20;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				EmptyMessage()
					.City(townIndex)
					.Text(":");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation, Hues.White);

				this.Owner.CityOperations.DrawCitizenIcons(townIndex, (short)((shift & 7) + 0x50), (short)(verticalLocation - 3), CityTurn.Specialists, 124);

				short iconHorizontal = 0xd4;

				this.Owner.Services.FloodBox(Screen.Box, 212, verticalLocation - 1, 90, 10, Hues.LightCyan);

				for (int structureIndex = 0; structureIndex < 0x18; structureIndex++)
				{
					if ((this.Owner.World.Towns[townIndex].StructureMarkersLow & (1 << structureIndex) & 0x2618) == 0)
					{
						continue;
					}

					this.Owner.CityOperations.DrawResourceIcon((short)(structureIndex + 1), iconHorizontal, (short)(verticalLocation - 1));

					iconHorizontal += 0x12;
				}

				citizens += this.Owner.World.Towns[townIndex].TrueExtent;
				happy += (short)CityTurn.Happy;
				unhappy += (short)CityTurn.Unhappy;
				shift += 4;
				verticalLocation += 0xa;
			}

			if (citizens == 0)
			{
				citizens = 1;
			}

			this.Owner.Words.Set("Population: ");
			this.Owner.Helpers.ComputePlayerPopulation(participantIndex);

			this.Owner.Words.Append(" Happy:");
			AffixProductionCount(happy, citizens);
			this.Owner.Words.Append("% Content:");
			AffixProductionCount((short)(citizens - happy - unhappy), citizens);
			this.Owner.Words.Append("% Unhappy:");
			AffixProductionCount(unhappy, citizens);
			this.Owner.Words.Append("%");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, verticalLocation + 8, Hues.White);

			FinishReportScreen();
		}

		public void ShowWarReport(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayWarReport({participantIndex})");

			const short top = 0x1e;
			const short rowHeight = 26;
			GameText title = new GameText(0, "WAR ADVISOR");
			List<short> rivals = new List<short>();
			Player person = this.Owner.World.Participants[participantIndex];

			for (short otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex != participantIndex && (this.Owner.World.LiveRealms & (1 << otherIndex)) != 0 &&
					(person.Relations[otherIndex] & DiplomacyRules.ContactRelation) != 0)
				{
					rivals.Add(otherIndex);
				}
			}

			ShowReportPreamble(title, Hues.Red);

			while (true)
			{
				DrawWarSummary(participantIndex, rivals, top, rowHeight);

				this.Owner.Startup.ShowMouseCursor();

				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.MenuPointerState == 0 && this.Owner.Runtime.KeyAvailable() == 0);

				short rowIndex = -1;

				if (this.Owner.Runtime.KeyAvailable() != 0)
				{
					ushort key = this.Owner.Dialogs.ReadNavigationKey();

					if (key >= 0x31 && key <= 0x37)
					{
						rowIndex = (short)(key - 0x31);
					}
				}
				else if (this.Owner.MenuPointerState == 1 && (short)this.Owner.MenuPointerVertical >= top)
				{
					rowIndex = (short)(((short)this.Owner.MenuPointerVertical - top) / rowHeight);
				}

				this.Owner.TurnLogic.WaitForKey();
				this.Owner.Startup.HideMouseCursor();

				if (rowIndex < 0 || rowIndex >= rivals.Count)
				{
					break;
				}

				DrawReportPreamble(title, Hues.Red);
				DrawWarDetails(participantIndex, rivals[rowIndex]);

				ushort detailKey = WaitForDetailKey();

				if (detailKey == 'c' || detailKey == 'C')
				{
					DrawReportPreamble(title, Hues.Red);
					DrawWarConditions(participantIndex, rivals[rowIndex]);
					this.Owner.Startup.ShowMouseCursor();
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();
					this.Owner.Startup.HideMouseCursor();
				}

				if ((detailKey == 't' || detailKey == 'T') && CanTalk(participantIndex, rivals[rowIndex]))
				{
					this.talkedThisRound[rivals[rowIndex]] = this.Owner.World.RoundTally;
					System.Drawing.Point seat = RulerSeat(rivals[rowIndex]);
					this.Owner.Audience.LaunchAudience(rivals[rowIndex], seat.X, seat.Y, 1);
					this.Owner.TurnHooks.RepaintMainScreen();
					this.Owner.Startup.ShowMouseCursor();
					return;
				}

				DrawReportPreamble(title, Hues.Red);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
		}

		public void AffixProductionCount(short count, short total)
		{
			using var _ = this.Processor.Journal.Scope($"AffixProductionCount({count}, {total})");

			if (count < 300)
			{
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((100 * count) / total, 10));
			}
			else
			{
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((10 * count) / (total / 10), 10));
			}
		}

		public void ShowCityStatusReport(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCityStatusReport({participantIndex})");

			void DrawCityStatusHeaders()
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen("City", 8, 26, Hues.Yellow);
				this.Owner.TextPainter.DrawTextToPrimaryScreen("Pop", 80, 26, Hues.Yellow);
				this.Owner.TextPainter.DrawTextToPrimaryScreen("Food", 98, 26, Hues.Yellow);
				this.Owner.TextPainter.DrawTextToPrimaryScreen("Prod", 120, 26, Hues.Yellow);
				this.Owner.TextPainter.DrawTextToPrimaryScreen("Trade", 142, 26, Hues.Yellow);
				this.Owner.TextPainter.DrawTextToPrimaryScreen("Production (Turns)", 172, 26, Hues.Yellow);
			}

			ShowReportPreamble(GameTexts.TextCityStatus, 8);

			DrawCityStatusHeaders();

			int verticalLocation = 34;
			short cityCount = 0;
			int totalPopulation = 0;
			int totalFood = 0;
			int totalShields = 0;
			int totalTrade = 0;

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff || this.Owner.World.Towns[townIndex].ParticipantIndex != participantIndex)
				{
					continue;
				}

				if (verticalLocation >= 0xb0)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextCityStatusSecond, 8);
					DrawCityStatusHeaders();

					verticalLocation = 34;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				cityCount++;
				totalPopulation += this.Owner.World.Towns[townIndex].TrueExtent;
				totalFood += (short)CityTurn.Food;
				totalShields += (short)CityTurn.Shields;
				totalTrade += (short)CityTurn.Trade;

				EmptyMessage()
					.City(townIndex);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, verticalLocation, Hues.White);

				EmptyMessage()
					.Number(this.Owner.World.Towns[townIndex].TrueExtent)
					.Text("-")
					.Number((short)CityTurn.Food)
					.Text("{ ")
					.Number((short)CityTurn.Shields)
					.Text("| ")
					.Number((short)CityTurn.Trade)
					.Text("}");
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 80, verticalLocation, Hues.White);

				sbyte making = this.Owner.World.Towns[townIndex].PresentMakingIndex;

				if (making == SpecialProjects.Wealth)
				{
					this.Owner.Words.Set("Wealth");
					this.Owner.Words.Append(" (50%->$)");
				}
				else if (making == SpecialProjects.Research)
				{
					this.Owner.Words.Set("Research");
					this.Owner.Words.Append(" (50%->Sci)");
				}
				else if (making == SpecialProjects.Food)
				{
					this.Owner.Words.Set("Food");
					this.Owner.Words.Append(" (50%->Food)");
				}
				else if (making < 0)
				{
					this.Owner.Words.Set(GameTables.BuildingRecordText(-making));
					this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 96);
					AppendProductionCost(townIndex, (short)(10 * Tables.BuildingWord(-making, BuildingTable.Price)));
				}
				else if (making < 0x40)
				{
					ushort record = (ushort)((making * UnitKindTable.Stride) + UnitKindTable.Origin);
					this.Owner.Words.Set(record);
					AppendProductionCost(townIndex, (short)(10 * this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.Price))));
				}

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 172, verticalLocation, Hues.White);

				verticalLocation += 8;
			}

			if (cityCount > 0)
			{
				if (verticalLocation > 0xaf)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextCityStatusSecond, 8);
					DrawCityStatusHeaders();

					verticalLocation = 34;
				}

				this.Owner.VideoCard.RenderLine(Screen.Box, 8, (short)(verticalLocation + 2), 312, (short)(verticalLocation + 2), Hues.LightBlue);

				string firstLine = $"Empire: {cityCount} Cities, {totalPopulation} Population";
				string secondLine = $"Totals: {totalFood} Food, {totalShields} Production, {totalTrade} Trade";

				this.Owner.TextPainter.DrawTextToPrimaryScreen(firstLine, 8, verticalLocation + 5, Hues.Yellow);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(secondLine, 8, verticalLocation + 14, Hues.LightGreen);
			}

			FinishReportScreen();
		}

		private static string Advice(DiplomacyForecast forecast, bool vendetta)
		{
			if (vendetta)
			{
				return "pay reparations (Diplomat: Meet King).";
			}

			if (forecast.Hostile)
			{
				return "pay, grow the army or pull back.";
			}

			if (forecast.PlansBetrayal)
			{
				return "outgrow their army to keep peace.";
			}

			if (forecast.OffersPeace)
			{
				return "meet them now to sign peace.";
			}

			return "keep the peace, trust will grow.";
		}

		private static ushort MoodHue(DiplomacyForecast forecast, bool vendetta) =>
			vendetta || forecast.Hostile ? Hues.LightRed : forecast.Attitude < 0 ? Hues.Yellow : forecast.Attitude >= 2 ? Hues.LightGreen : Hues.White;

		private static string Signed(int value) => value > 0 ? "+" + value : value.ToString(System.Globalization.CultureInfo.InvariantCulture);

		private System.Drawing.Point RulerSeat(short otherIndex)
		{
			System.Drawing.Point seat = new System.Drawing.Point(-1, -1);

			foreach (City town in this.Owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIndex != otherIndex)
				{
					continue;
				}

				if (seat.X < 0 || town.Has(CityStructure.Palace))
				{
					seat = town.Place;
				}
			}

			return seat.X < 0 ? new System.Drawing.Point(0, 0) : seat;
		}

		private bool CanTalk(short participantIndex, short otherIndex) =>
			(this.Owner.World.Participants[participantIndex].Relations[otherIndex] & DiplomacyRules.EmbassyRelation) != 0 &&
			this.talkedThisRound[otherIndex] != this.Owner.World.RoundTally;

		private ushort WaitForDetailKey()
		{
			this.Owner.Startup.ShowMouseCursor();

			do
			{
				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.MenuPointerState == 0 && this.Owner.Runtime.KeyAvailable() == 0);

			ushort key = 0;

			if (this.Owner.Runtime.KeyAvailable() != 0)
			{
				key = this.Owner.Dialogs.ReadNavigationKey();
			}

			this.Owner.TurnLogic.WaitForKey();
			this.Owner.Startup.HideMouseCursor();

			return key;
		}

		/// <summary>Every condition the ruler weighs about us, with current values, good ones green and bad ones red.</summary>
		private void DrawWarConditions(short participantIndex, short otherIndex)
		{
			short line = 0x1e;

			this.Owner.Words.Set(Tables.CivilizationPlural(otherIndex));
			this.Owner.Words.Append(": how they decide about us");
			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, line, Tables.PlayerColour(otherIndex));
			line += 10;

			foreach ((string text, int effect) in DiplomacyConditions.Lines(ForecastAudience(participantIndex, otherIndex)))
			{
				ushort hue = effect > 0 ? Hues.LightGreen : effect < 0 ? Hues.LightRed : Hues.White;
				this.Owner.TextPainter.DrawTextToPrimaryScreen(text, 8, line, hue);
				line += 9;
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen("Green: works for us. Red: against us.", 8, 191, Hues.Yellow);
		}

		private void DrawWarSummary(short participantIndex, List<short> rivals, short top, short rowHeight)
		{
			Player[] participants = this.Owner.World.Participants;
			Player person = participants[participantIndex];
			short verticalLocation = top;

			for (int row = 0; row < rivals.Count; row++)
			{
				short otherIndex = rivals[row];
				Player other = participants[otherIndex];
				ushort theirs = other.Relations[participantIndex];
				bool vendetta = (theirs & DiplomacyRules.VendettaRelation) != 0;
				DiplomacyForecast forecast = ForecastAudience(participantIndex, otherIndex);

				this.Owner.Words.Set(this.Owner.Runtime.IntegerToText((short)(row + 1), 10));
				this.Owner.Words.Append(". ");
				this.Owner.Words.Append(Tables.CivilizationPlural(otherIndex));
				AppendTreatyStatus(participantIndex, otherIndex);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, verticalLocation, Tables.PlayerColour(otherIndex));

				this.Owner.Words.Set(DiplomacyRules.AttitudeName(forecast.Attitude, vendetta));
				this.Owner.Words.Append(forecast.Hostile ? ", HOSTILE" : string.Empty);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 212, verticalLocation, MoodHue(forecast, vendetta));

				this.Owner.Words.Set("   Army ");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(other.MartialMight, 10));
				this.Owner.Words.Append(" vs ours ");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(person.MartialMight, 10));

				if (vendetta)
				{
					this.Owner.Words.Append("  VENDETTA ");
					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)DiplomacyRules.GrudgeTurns(theirs), 10));
					this.Owner.Words.Append("t, ");
					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)Reparations(theirs), 10));
					this.Owner.Words.Append("g");
				}

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, verticalLocation + 8, Hues.White);

				this.Owner.TextPainter.DrawTextToPrimaryScreen("   " + SummaryOutlook(forecast, participantIndex, otherIndex), 8, verticalLocation + 16, Hues.LightGray);

				verticalLocation += rowHeight;
			}

			if (rivals.Count == 0)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen("We have not met any civilization yet.", 8, verticalLocation, Hues.White);
			}
			else if (rivals.Count <= 6)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen("1-6/click: details, C: conditions, T: talk.", 8, 191, Hues.Yellow);
			}
		}

		private void DrawWarDetails(short participantIndex, short otherIndex)
		{
			Player[] participants = this.Owner.World.Participants;
			Player person = participants[participantIndex];
			Player other = participants[otherIndex];
			ushort theirs = other.Relations[participantIndex];
			bool vendetta = (theirs & DiplomacyRules.VendettaRelation) != 0;
			bool embassy = (person.Relations[otherIndex] & DiplomacyRules.EmbassyRelation) != 0;
			DiplomacyForecast forecast = ForecastAudience(participantIndex, otherIndex);
			short line = 0x1e;

			void Draw(string text, ushort hue)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(text, 8, line, hue);
				line += 8;
			}

			this.Owner.Words.Set(Tables.CivilizationPlural(otherIndex));
			this.Owner.Words.Append(", ruled by ");
			this.Owner.Words.Append(Tables.LeaderName(otherIndex));
			Draw(this.Owner.Words.ToString(), Tables.PlayerColour(otherIndex));

			this.Owner.Words.Set("Status:");
			AppendTreatyStatus(participantIndex, otherIndex);

			if (forecast.AtPeace && forecast.PeaceScore < 2)
			{
				this.Owner.Words.Append(forecast.PeaceScore == 0 ? " (trust at 32)" : " (more trust at 60)");
			}

			Draw(this.Owner.Words.ToString(), Hues.White);

			Draw("Mood: " + DiplomacyRules.AttitudeName(forecast.Attitude, vendetta) + " (score " + Signed(forecast.Attitude) + ")", MoodHue(forecast, vendetta));
			Draw("  Character " + Signed(forecast.CharacterScore) + "  Vendetta " + Signed(forecast.VendettaScore) +
				"  Rival wars " + Signed(forecast.RivalWarsScore), Hues.LightGray);
			Draw("  Army balance " + Signed(forecast.MightScore) + "  Long peace " + Signed(forecast.PeaceScore), Hues.LightGray);

			if (forecast.Hostile)
			{
				Draw("HOSTILE: will demand " + forecast.Tribute + " gold tribute.", Hues.Yellow);
			}
			else if (forecast.PlansBetrayal)
			{
				Draw("Hostile, but hides it behind the treaty.", Hues.Yellow);
			}
			else
			{
				Draw("Not hostile towards us.", Hues.LightGreen);
			}

			int reasons = 0;

			foreach (string reason in forecast.HostileReasons)
			{
				if (reasons++ < 3)
				{
					Draw("  - " + reason, Hues.LightRed);
				}
			}

			foreach (string reason in forecast.CalmingReasons)
			{
				if (reasons++ < 3)
				{
					Draw("  + " + reason, Hues.LightGreen);
				}
			}

			if (forecast.Fear > 0)
			{
				Draw("Fear of our troops: " + forecast.Fear, Hues.White);
			}

			Draw(vendetta
				? "Vendetta: " + DiplomacyRules.GrudgeTurns(theirs) + " turns left, reparations " + Reparations(theirs) + " gold."
				: "No vendetta against us.", vendetta ? Hues.Yellow : Hues.White);

			int wait = Math.Max(0, other.TouchParticipantTimeLeft + 16 - this.Owner.World.RoundTally);
			Draw("Next unit meeting: " + (wait == 0 ? "now" : "in " + wait + " turns") + " (Diplomat: now).", Hues.White);

			Draw("Next audience will likely bring:", Hues.White);
			int items = 0;

			foreach (string outlook in DetailedOutlook(forecast, vendetta, theirs))
			{
				if (items++ < 3)
				{
					Draw("  " + outlook, Hues.LightCyan);
				}
			}

			if (!embassy)
			{
				Draw("War plans: unknown, we need an embassy.", Hues.LightGray);
			}
			else
			{
				string plans = "War plans: none known.";

				if ((theirs & DiplomacyRules.SneakAttackRelation) != 0)
				{
					plans = "War plans: SNEAK ATTACK PREPARED!";
				}
				else if ((theirs & DiplomacyRules.BreakPeacePlanRelation) != 0)
				{
					plans = "War plans: they plan to break the peace!";
				}

				Draw(plans, Hues.LightGray);
			}

			Draw("Advice: " + Advice(forecast, vendetta), Hues.Yellow);

			if (embassy)
			{
				Draw(CanTalk(participantIndex, otherIndex) ? "C: all conditions.  T: talk to ruler." : "C: all conditions.  Talked this turn.", Hues.LightGreen);
			}
			else
			{
				Draw("C: all conditions explained.", Hues.LightGreen);
			}
		}

		private DiplomacyForecast ForecastAudience(short participantIndex, short otherIndex)
		{
			short holderTechnologies = 0;
			short personTechnologies = 0;
			bool newTechnology = false;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				bool ours = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0;
				bool theirs = this.Owner.Advisors.PlayerKnowsTechnology(otherIndex, scienceIndex) != 0;

				personTechnologies += (short)(ours ? 1 : 0);
				holderTechnologies += (short)(theirs ? 1 : 0);
				newTechnology |= theirs && !ours;
			}

			bool wonderShield = this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 7) != 0 ||
				this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 0x12) != 0;

			return DiplomacyForecast.Compute(
				this.Owner.World.Participants,
				otherIndex,
				participantIndex,
				(short)Tables.LeaderWord(this.Owner.World.Participants[otherIndex].ChiefVisuals, LeaderTable.Aggression),
				this.Owner.World.HardnessTier,
				this.Owner.World.RoundTally,
				wonderShield,
				holderTechnologies,
				personTechnologies,
				newTechnology);
		}

		private void AppendTreatyStatus(short participantIndex, short otherIndex)
		{
			if ((this.Owner.World.Participants[participantIndex].Relations[otherIndex] & DiplomacyRules.PeaceRelation) == 0)
			{
				this.Owner.Words.Append(": WAR");
				return;
			}

			int peaceUnits = DiplomacyRules.PeaceUnits(this.Owner.World.Participants, participantIndex, otherIndex);

			this.Owner.Words.Append(": PEACE ");
			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(peaceUnits * DiplomacyRules.TurnsPerUnit), 10));
			this.Owner.Words.Append(peaceUnits >= DiplomacyRules.MaximumUnits ? "+ turns" : " turns");
		}

		private string SummaryOutlook(DiplomacyForecast forecast, short participantIndex, short otherIndex)
		{
			ushort theirs = this.Owner.World.Participants[otherIndex].Relations[participantIndex];
			bool embassy = (this.Owner.World.Participants[participantIndex].Relations[otherIndex] & DiplomacyRules.EmbassyRelation) != 0;

			if (embassy && (theirs & DiplomacyRules.SneakAttackRelation) != 0)
			{
				return "SNEAK ATTACK PREPARED!";
			}

			if (embassy && (theirs & DiplomacyRules.BreakPeacePlanRelation) != 0)
			{
				return "Plans to break the peace!";
			}

			if (forecast.PlansBetrayal)
			{
				return "Smiles, but may turn on us.";
			}

			if (forecast.Hostile)
			{
				return "Wants " + forecast.Tribute + " gold tribute or war.";
			}

			if (forecast.PeaceGold > 0)
			{
				return "Afraid: would pay " + forecast.PeaceGold + " gold for peace.";
			}

			if (forecast.OffersPeace)
			{
				return "Ready to sign peace.";
			}

			return forecast.Attitude >= 2 ? "A loyal friend." : "Content with the peace.";
		}

		private IEnumerable<string> DetailedOutlook(DiplomacyForecast forecast, bool vendetta, ushort theirs)
		{
			if (vendetta)
			{
				yield return "Demand of " + Reparations(theirs) + " gold reparations.";
			}

			if (forecast.Hostile)
			{
				yield return "Tribute demand: " + forecast.Tribute + " gold. Refusal = war.";
			}

			if (forecast.OffersPeace)
			{
				yield return "An offer of peace.";
			}

			if (forecast.PeaceGold > 0)
			{
				yield return "If we refuse peace: " + forecast.PeaceGold + " gold for us.";
			}

			if (forecast.UnderPeaceGold > 0)
			{
				yield return "We may demand " + forecast.UnderPeaceGold + " gold tribute.";
			}

			if (forecast.OffersExchange)
			{
				yield return "An exchange of advances.";
			}

			if (forecast.PlansBetrayal)
			{
				yield return "A secret plan to break the peace!";
			}

			if (!forecast.Hostile && !forecast.OffersPeace && forecast.UnderPeaceGold == 0 && !forecast.OffersExchange && !vendetta && !forecast.PlansBetrayal)
			{
				yield return "Friendly words, nothing more.";
			}
		}

		private int Reparations(ushort relation) =>
			DiplomacyRules.ComputeReparations(DiplomacyRules.GrudgeUnits(relation), this.Owner.World.HardnessTier);

		private void DrawMilitaryReportRow(ushort participantIndex, short verticalLocation)
		{
			ushort townIndex;

			for (townIndex = 0; (short)townIndex < 0x80; townIndex++)
			{
				if ((ushort)this.Owner.World.Towns[townIndex].ParticipantIndex == participantIndex &&
					this.Owner.World.Towns[townIndex].ConditionMarker != 0xff &&
					this.Owner.World.Towns[townIndex].Has(CityStructure.Palace))
				{
				}
			}

			this.Owner.Words.Append(": ");

			this.Owner.Words.Append(Tables.LeaderName(participantIndex));

			this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 8, verticalLocation, Hues.White);

			Message(Tables.GovernmentName((ushort)this.Owner.World.Participants[participantIndex].RegimeKind))
				.Text(", ")
				.Number(this.Owner.World.Participants[participantIndex].Gold)
				.Text("$");

			if (this.Owner.World.HardnessTier <= 1)
			{
				this.Owner.Words.Append(", ");

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(this.Owner.World.Participants[participantIndex].TroopTally, 10));

				this.Owner.Words.Append(" Units.");
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 160, verticalLocation, Tables.PlayerColour(participantIndex));

			verticalLocation += 8;

			short horizontalLocation = 0x10;
			ushort otherIndex;

			for (otherIndex = 1; (short)otherIndex < 8; otherIndex++)
			{
				this.Owner.Words.Clear();

				this.Owner.Words.Set(Tables.CivilizationPlural(otherIndex));

				if (horizontalLocation > 0xf0)
				{
					horizontalLocation = 0x10;
					verticalLocation += 8;
				}

				ushort relation = this.Owner.World.Participants[participantIndex].Relations[otherIndex];

				if ((relation & 2) != 0)
				{
					this.Owner.Words.Append((relation & 4) != 0 ? ":\x008f ALLIED" : ":\x008f PEACE");
				}
				else if ((relation & 3) != 1)
				{
					continue;
				}
				else
				{
					this.Owner.Words.Append(":\x008c WAR");
				}

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation, verticalLocation, Hues.LightGray);

				horizontalLocation += 0x60;
			}
		}

		private void AppendProductionCost(short townIndex, short cost)
		{
			this.Owner.Words.Append("\x0087 (");
			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(this.Owner.World.Towns[townIndex].MaterialsTally, 10));
			this.Owner.Words.Append("/");
			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(cost, 10));
			this.Owner.Words.Append(")");
		}
	}
}
