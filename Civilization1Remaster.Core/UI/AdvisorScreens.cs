
namespace Civilization1Remaster.Core.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 14.
	/// </summary>
	public class AdvisorScreens : GameComponent, IAdvisorScreens
	{
		public AdvisorScreens(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public void DrawReportPreamble(GameText title, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"RenderReportPreamble(0x{title.Address:x4}, {style})");

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];
			ushort personSlot = (ushort)(this.Owner.World.PersonParticipantIdx << 1);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, style);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(title, 160, 2, Hues.White);

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

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 10, Hues.White);

			Message(Tables.GovernmentTitle(person.RegimeKind))
				.Text(" ")
				.Text(Tables.LeaderNameAt(personSlot))
				.Text(": ");

			this.Owner.TurnHooks.FormatCurrentYear();

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 18, Hues.White);
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public void ShowScienceReport(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayScienceReport({participantIdx})");

			ShowReportPreamble(GameTexts.TextScienceReport, 1);

			short knownTally = this.Owner.World.Participants[participantIdx].FoundScienceTally;
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

			for (short bulbIdx = 0; (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[participantIdx].InquiryHeadway, 0, cost) > bulbIdx; bulbIdx++)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, (progress / 8) + barLeft, 33,
					Tables.SpritePicture(0xc));

				progress += (short)step;
			}

			short inquiry = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].PresentInquiryIdx;

			if (inquiry != -1)
			{
				Message("Researching ")
					.Text(GameTables.TechnologyText(inquiry));

				this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 161, 26, Hues.Black);
				this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 26, Hues.White);
			}

			short vertLoc = 0x2a;
			short shown = 0;

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, technologyIdx) == 0)
					continue;

				this.Owner.Words.Set(GameTables.TechnologyText(technologyIdx));

				ushort hue = (ushort)((this.Owner.World.ScienceLeadingFoundVia[technologyIdx] & 7) == participantIdx ? 0xf : 0xb);

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), ((shown % 3) * 100) + 8, vertLoc, hue);

				shown++;

				if (shown % 3 == 0)
				{
					vertLoc += 7;
				}

				if (vertLoc > 0xc0)
					break;
			}

			FinishReportScreen();
		}

		/// <inheritdoc/>
		public void DrawUnitCountsChart(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitCountsChart({participantIdx})");

			ShowReportPreamble(GameTexts.TextMilitaryStatus, 1);

			int rowIdx = 0;
			int vertLoc = 0x20;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				short active = this.Owner.World.Participants[participantIdx].LiveTroops[kind];
				short making = this.Owner.World.Participants[participantIdx].TroopsInsideMaking[kind];

				if (active <= 0 && making <= 0)
					continue;

				this.Owner.VideoCard.RenderLine(Screen.Box, 36, vertLoc - 2, 319, vertLoc - 2, Hues.LightBlue);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					Tables.SpritePicture(((participantIdx << 5) + kind + 0x40)));

				rowIdx++;

				ushort record = (ushort)((kind * UnitKindTable.Stride) + UnitKindTable.Origin);

				this.Owner.TextPainter.DrawTextToScreen0(record, 36, vertLoc, Hues.White);

				this.Owner.Words.Set("(");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.AssaultPower)), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.GuardPower)), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.ShiftTally)), 10));
				this.Owner.Words.Append(")");

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 112, vertLoc, Hues.LightCyan);

				if (active > 0)
				{
					EmptyMessage()
						.Number(active)
						.Text(" active");
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 168, vertLoc, Hues.White);
				}

				if (making > 0)
				{
					EmptyMessage()
						.Number(making)
						.Text(" in production");
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 232, vertLoc, Hues.LightCyan);
				}

				vertLoc += 9;
			}

			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xa0, 0x8c, 0x28, 0x3c, Screen.Box, 0x116, 0x2);

			FinishReportScreen();

			DrawPlayerComparisonChart(participantIdx);
		}

		/// <inheritdoc/>
		public void DrawPlayerComparisonChart(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPlayerComparisonChart({participantIdx})");

			ShowReportPreamble(GameTexts.TextMilitaryLosses, 4);

			int rowIdx = 0;
			int vertLoc = 0x1c;

			for (int kind = 0; kind < 0x1b; kind++)
			{
				bool lost = false;

				for (int playerIdx = 0; playerIdx < 8; playerIdx++)
				{
					if (this.Owner.World.Participants[playerIdx].MissingTroops[kind] != 0)
					{
						lost = true;
					}
				}

				if (!lost)
					continue;

				this.Owner.VideoCard.RenderLine(Screen.Box, 36, vertLoc - 1, 319, vertLoc - 1, Hues.DarkGray);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					Tables.SpritePicture(((participantIdx << 5) + kind + 0x40)));

				rowIdx++;

				this.Owner.TextPainter.DrawTextToScreen0(GameTables.UnitText(kind), 36, vertLoc, Hues.White);

				short losses = this.Owner.World.Participants[participantIdx].MissingTroops[kind];

				if (losses != 0)
				{
					EmptyMessage()
						.Number(losses);
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 104, vertLoc, Hues.LightRed);
				}

				int horizLoc = 0x68;

				for (short playerIdx = 1; playerIdx < 8; playerIdx++)
				{
					if (playerIdx == participantIdx)
						continue;

					horizLoc += 0x1c;
					losses = this.Owner.World.Participants[playerIdx].MissingTroops[kind];

					if (losses != 0)
					{
						EmptyMessage()
							.Number(losses);
						this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), horizLoc, vertLoc,
							Tables.PlayerColour(playerIdx));
					}
				}

				vertLoc += 7;
			}

			this.Owner.Dialogs.DrawBox(100, 25, 18, vertLoc - 25,
				Tables.PlayerColour(this.Owner.World.PersonParticipantIdx));

			FinishReportScreen();
		}

		/// <inheritdoc/>
		public void ShowTradeReport(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayTradeReport({participantIdx})");


			ushort[] costs = new ushort[0x18];

			short palaceCost = (short)(this.Owner.World.HardnessTier / 2);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22) != 0)
			{
				palaceCost++;
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x25) != 0)
			{
				palaceCost++;
			}

			ShowReportPreamble(GameTexts.TextTradeReport, 2);

			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xc8, 0x8c, 0x28, 0x3c, Screen.Box, 0x116, 2);

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextCityTrade, 8, 32, Hues.White);

			short income = 0;
			short science = 0;

			for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
			{
				costs[structureIdx] = 0x0;
			}

			short vertLoc = 0x28;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				if (vertLoc >= 0xb4)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextTradeReportB, 2);

					vertLoc = 0x28;
				}

				this.Owner.CityOps.HandleCity(townIdx, -1);

				EmptyMessage()
					.City(townIdx);

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 62);

				if (vertLoc < 0xb4)
				{
					this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, vertLoc, Hues.White);
				}

				this.Owner.Words.Clear();

				if ((town.ConditionMarker & 1) != 0)
				{
					this.Owner.Words.Append(" \x008c(Disorder)");
				}
				else
				{
					this.Owner.Words.Append(" ");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)CityTurn.Luxuries, 10));

					this.Owner.Words.Append("\\/");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)CityTurn.Taxes, 10));

					this.Owner.Words.Append("$/");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)CityTurn.Science, 10));

					this.Owner.Words.Append("~");

					income += (short)CityTurn.Taxes;
					science += (short)CityTurn.Science;
				}

				if (vertLoc < 0xb4)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 80, vertLoc, Hues.LightGreen);

					vertLoc += 8;
				}

				for (int structureIdx = 1; structureIdx < 0x18; structureIdx++)
				{
					uint structure = 1u << structureIdx;

					if (((ushort)structure & town.StructureMarkers0) == 0 && ((ushort)(structure >> 16) & town.StructureMarkers1) == 0)
						continue;

					costs[structureIdx] = (ushort)(costs[structureIdx] + Tables.BuildingWord(structureIdx + 1, BuildingTable.Upkeep));

					if (structureIdx == 1)
					{
						costs[structureIdx] = (ushort)(costs[structureIdx] + palaceCost);
					}
				}
			}


			Message("Total Income: ")
				.Number(income)
				.Text("$");

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 8, vertLoc + 4, Hues.LightGreen);

			if (this.Owner.CityOps.PlayerEnjoysWonderEffect(this.Owner.World.PersonParticipantIdx, 0x14) != 0)
			{
				science += (short)(science / 2);
			}

			if (science != 0 && vertLoc < 0xb4)
			{
				short epoch = (short)(this.Owner.World.Season < 0 ? 1 : this.Owner.World.Season < 1750 ? 2 : 3);
				short turns = CPU.SignedQuotient((short)((short)(epoch * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].FoundScienceTally) *
					(short)((this.Owner.World.HardnessTier << 1) + 6)), science);


				Message("Discoveries: ")
					.Number(turns)
					.Text(" turns");

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 8, vertLoc + 12, Hues.LightGreen);
			}

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextMaintenanceCosts, 160, 32, Hues.White);

			vertLoc = 0x28;
			short total = 0;

			for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
			{
				short cost = (short)costs[structureIdx];

				if (cost == 0)
					continue;

				this.Owner.Words.Clear();

				// The number of the buildings: the cost by the upkeep (the palace by its own cost)
				short tally = structureIdx == 1 ? (short)(cost / palaceCost) :
					(short)(cost / (short)Tables.BuildingWord(structureIdx + 1, BuildingTable.Upkeep));

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(tally, 10));

				this.Owner.Words.Append(" ");

				this.Owner.Words.Append(GameTables.BuildingText(structureIdx));

				this.Owner.Words.Append(", ");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(cost, 10));

				this.Owner.Words.Append("$");

				if (vertLoc < 0xb4)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc, Hues.Yellow);

					vertLoc += 8;
				}

				total += cost;
			}

			Message("Total Cost: ")
				.Number(total)
				.Text("$");

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc + 4, Hues.Yellow);

			vertLoc += 0xc;


			FinishReportScreen();
		}

		/// <inheritdoc/>
		public void ShowMilitaryReport()
		{
			using var _ = this.Processor.Journal.Scope("DisplayMilitaryReport()");

			// The same list in C#, with -1 for the rows without contact
			short[] rowPlayerList = [-1, -1, -1, -1, -1, -1, -1, -1];

			ShowReportPreamble(GameTexts.TextIntelligenceReport, 1);

			ushort rowCount = 0;
			bool reportShown = false;
			ushort participantIdx;


			for (participantIdx = 1; (short)participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.LiveRealms & (1 << (byte)participantIdx)) == 0)
					continue;

				short vertLoc = (short)((0x18 * rowCount) + 0x20);

				this.Owner.VideoCard.RenderLine(Screen.Box, 4, (short)(vertLoc - 2), 316, (short)(vertLoc - 2), Hues.LightBlue);

				this.Owner.Words.Set(Tables.CivilizationPlural(participantIdx));

				if ((short)participantIdx != this.Owner.World.PersonParticipantIdx &&
					(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) == 0)
				{
					this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextNoEmbassyEstablished, 96, vertLoc, Tables.PlayerColour(participantIdx));

					rowCount++;
					continue;
				}

				DrawMilitaryReportRow(participantIdx, vertLoc);

				rowPlayerList[rowCount] = (short)participantIdx;
				rowCount++;

				if ((short)participantIdx == this.Owner.World.PersonParticipantIdx)
					continue;

				Message("INFO")
					.Number((short)rowCount);

				short boxTop = (short)(0x18 * rowCount);

				this.Owner.CityOps.DrawLabelBox(281, boxTop + 20, 318, boxTop + 29, TextBuffer.Address, Tables.PlayerColour(participantIdx));
			}

			this.Owner.Startup.ShowMouseCursor();

			ushort done;

			do
			{
				do
				{
					this.Owner.Startup.ReadMouseState();
				}
				while (this.Owner.GlobDb3a == 0 && this.Owner.Libc.KeyAvailable() == 0);

				done = 1;

				short rowIdx = -1;
				bool chosen = false;

				if (this.Owner.Libc.KeyAvailable() != 0)
				{
					ushort key = this.Owner.Dialogs.ReadNavigationKey();

					if ((short)key >= 0x31 && (short)key <= 0x37)
					{
						rowIdx = (short)(key - 0x31);
						chosen = true;
					}
				}

				if (!chosen && (short)this.Owner.GlobDb3c > 0x119)
				{
					rowIdx = CPU.SignedQuotient((short)(this.Owner.GlobDb3e - 0x1c), 0x18);
					chosen = true;
				}

				if (chosen && rowIdx >= 0 && rowIdx < (short)rowCount)
				{
					ushort rowPlayerIdx = (ushort)rowPlayerList[rowIdx];

					if ((short)rowPlayerIdx != -1 &&
						(short)rowPlayerIdx != this.Owner.World.PersonParticipantIdx &&
						(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[rowPlayerIdx] & 0x40) != 0)
					{
						ShowIntelligenceReport((short)rowPlayerIdx);

						reportShown = true;
						done = 0;
					}
				}

				participantIdx = this.Owner.GlobDb3c;

				this.Owner.TurnLogic.WaitForKey();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.Services.AssignMousePosition(270, this.Owner.GlobDb3e);

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

		/// <inheritdoc/>
		public void ShowIntelligenceReport(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayIntelligenceReport({participantIdx})");

			Player participant = this.Owner.World.Participants[participantIdx];

			ShowReportPreamble(GameTexts.TextIntelligenceReportB, 1);

			short vertLoc = 0x20;

			Message("Subject: the ")
				.Text(Tables.CivilizationPlural(participantIdx));

			this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 16, 32, Hues.White);

			vertLoc += 0xc;

			Message("Leader:\x008f ")
				.Text(Tables.GovernmentTitle(participant.RegimeKind))
				.Text(" ")
				.Text(Tables.LeaderName(participantIdx));

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			// The traits of the leader: three words, each 1 or -1, each with its own text
			GameText[] traits =
			{
				new(0x48d3, "Aggressive"), new(0x48de, "Friendly"), new(0x48e7, "Expansionistic"),
				new(0x48f6, "Perfectionist"), new(0x4904, "Civilized"), new(0x490e, "Militaristic")
			};

			for (int traitIdx = 0; traitIdx < 6; traitIdx++)
			{
				ushort trait = Tables.LeaderWord(participant.ChiefVisuals, LeaderTable.Aggression + ((traitIdx >> 1) << 1));

				if (trait == ((traitIdx & 1) == 0 ? 1 : 0xffff))
				{
					this.Owner.TextPainter.DrawTextToScreen0(traits[traitIdx], 24, vertLoc, Hues.LightGray);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short capitalIdx = -1;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff && town.Has(CityStructure.Palace))
				{
					capitalIdx = townIdx;
				}
			}

			Message("Capital:\x008f ")
				.City(capitalIdx);

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			Message("Government:\x008f ")
				.Text(Tables.GovernmentName(participant.RegimeKind));

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			Message("Treasury:\x008f ")
				.Number(participant.Gold)
				.Text("$");

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			if (this.Owner.World.HardnessTier <= 1)
			{
				Message("Military:\x008f ")
					.Number(participant.TroopTally)
					.Text(" Units.");

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

				vertLoc += 0xc;
			}

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextForeignAffairs, 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			for (int otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				// With the 1 of contact one of the two applies (3 is peace)
				if ((participant.Relations[otherIdx] & 2) != 0)
				{
					this.Owner.Words.Set("At\x008f PEACE");
				}

				if ((participant.Relations[otherIdx] & 3) == 1)
				{
					this.Owner.Words.Set("At\x008c WAR");
				}

				if ((participant.Relations[otherIdx] & 1) == 0)
					continue;

				this.Owner.Words.Append("\x0087 with the ");

				this.Owner.Words.Append(Tables.CivilizationPlural(otherIdx));

				this.Owner.Words.Append(".");

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 24, vertLoc, Hues.LightGray);

				vertLoc += 8;
			}

			vertLoc += 4;

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextTechnologies, 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			for (short scienceIdx = 0x47; scienceIdx >= 0; scienceIdx--)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
					continue;

				this.Owner.TextPainter.DrawTextToScreen0(GameTables.TechnologyText(scienceIdx), 24, vertLoc, Hues.LightGray);

				vertLoc += 8;

				if (vertLoc >= 0xc0)
					break;
			}

			FinishReportScreen();
		}

		/// <inheritdoc/>
		public void ShowAttitudeReport(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayAttitudeReport({participantIdx})");


			ShowReportPreamble(GameTexts.TextAttitudeSurvey, 9);

			short vertLoc = 0x20;
			short unhappy = 0;
			short happy = 0;
			short citizens = 0;
			short shift = 0;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff || this.Owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				if (vertLoc > 0xbe)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextAttitudeSurveyB, 9);

					vertLoc = 0x20;
				}

				this.Owner.CityOps.HandleCity(townIdx, -1);

				EmptyMessage()
					.City(townIdx)
					.Text(":");
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc, Hues.White);

				this.Owner.CityOps.DrawCitizenIcons(townIdx, (short)((shift & 7) + 0x50), (short)(vertLoc - 3), CityTurn.Specialists, 124);

				short iconHoriz = 0xd4;

				this.Owner.Services.FloodBox(Screen.Box, 212, vertLoc - 1, 90, 10, Hues.LightCyan);

				for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
				{
					if ((this.Owner.World.Towns[townIdx].StructureMarkers0 & (1 << structureIdx) & 0x2618) == 0)
						continue;

					this.Owner.CityOps.DrawResourceIcon((short)(structureIdx + 1), iconHoriz, (short)(vertLoc - 1));

					iconHoriz += 0x12;
				}

				citizens += this.Owner.World.Towns[townIdx].TrueExtent;
				happy += (short)CityTurn.Happy;
				unhappy += (short)CityTurn.Unhappy;
				shift += 4;
				vertLoc += 0xa;
			}

			if (citizens == 0)
			{
				citizens = 1;
			}

			// The population is computed, but not written
			this.Owner.Words.Set("Population: ");
			this.Owner.Helpers.ComputePlayerPopulation(participantIdx);

			this.Owner.Words.Append(" Happy:");
			AffixProductionCount(happy, citizens);
			this.Owner.Words.Append("% Content:");
			AffixProductionCount((short)(citizens - happy - unhappy), citizens);
			this.Owner.Words.Append("% Unhappy:");
			AffixProductionCount(unhappy, citizens);
			this.Owner.Words.Append("%");

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, vertLoc + 8, Hues.White);

			FinishReportScreen();
		}

		/// <inheritdoc/>
		public void AffixProductionCount(short count, short total)
		{
			using var _ = this.Processor.Journal.Scope($"AffixProductionCount({count}, {total})");

			if (count < 300)
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((100 * count) / total, 10));
			}
			else
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((10 * count) / (total / 10), 10));
			}
		}

		/// <inheritdoc/>
		public void ShowCityStatusReport(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCityStatusReport({participantIdx})");

			ShowReportPreamble(GameTexts.TextCityStatus, 8);

			int vertLoc = 0x20;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff || this.Owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				if (vertLoc >= 0xc0)
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();

					DrawReportPreamble(GameTexts.TextCityStatusB, 8);

					vertLoc = 0x20;
				}

				this.Owner.CityOps.HandleCity(townIdx, -1);

				EmptyMessage()
					.City(townIdx);
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 8, vertLoc, Hues.White);

				EmptyMessage()
					.Number(this.Owner.World.Towns[townIdx].TrueExtent)
					.Text("-")
					.Number((short)CityTurn.Food)
					.Text("{ ")
					.Number((short)CityTurn.Shields)
					.Text("| ")
					.Number((short)CityTurn.Trade)
					.Text("}");
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 80, vertLoc, Hues.White);

				sbyte making = this.Owner.World.Towns[townIdx].PresentMakingIdx;

				if (making < 0)
				{
					// A building: names of 0x1e bytes at 0xb9a, costs at 0xbb2
					this.Owner.Words.Set(GameTables.BuildingRecordText(-making));
					this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 96);
					AppendProductionCost(townIdx, (short)(10 * Tables.BuildingWord(-making, BuildingTable.Price)));
				}
				else if (making < 0x40)
				{
					ushort record = (ushort)((making * UnitKindTable.Stride) + UnitKindTable.Origin);
					this.Owner.Words.Set(record);
					AppendProductionCost(townIdx, (short)(10 * this.Processor.PullGlobalWord((ushort)(record + UnitKindTable.Price))));
				}

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 172, vertLoc, Hues.White);

				vertLoc += 8;
			}

			FinishReportScreen();
		}

		/// <summary>
		/// The details of a player in the foreign minister's report (his name is in TextBuffer.Address): title, government, treasury, units
		/// and then peace, alliance or war with each player, 0x60 apart, in rows of 8 from x 0x10
		/// </summary>
		private void DrawMilitaryReportRow(ushort participantIdx, short vertLoc)
		{
			// The last city of the player with a palace, never used
			ushort townIdx;

			for (townIdx = 0; (short)townIdx < 0x80; townIdx++)
			{
				if ((ushort)this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx &&
					this.Owner.World.Towns[townIdx].ConditionMarker != 0xff &&
					this.Owner.World.Towns[townIdx].Has(CityStructure.Palace))
				{
				}
			}


			this.Owner.Words.Append(": ");

			this.Owner.Words.Append(Tables.LeaderName(participantIdx));

			this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 8, vertLoc, Hues.White);

			Message(Tables.GovernmentName((ushort)this.Owner.World.Participants[participantIdx].RegimeKind))
				.Text(", ")
				.Number(this.Owner.World.Participants[participantIdx].Gold)
				.Text("$");

			if (this.Owner.World.HardnessTier <= 1)
			{
				this.Owner.Words.Append(", ");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(this.Owner.World.Participants[participantIdx].TroopTally, 10));

				this.Owner.Words.Append(" Units.");
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc, Tables.PlayerColour(participantIdx));

			vertLoc += 8;

			short horizLoc = 0x10;
			ushort otherIdx;

			for (otherIdx = 1; (short)otherIdx < 8; otherIdx++)
			{
				this.Owner.Words.Clear();

				this.Owner.Words.Set(Tables.CivilizationPlural(otherIdx));

				if (horizLoc > 0xf0)
				{
					horizLoc = 0x10;
					vertLoc += 8;
				}

				ushort relation = this.Owner.World.Participants[participantIdx].Relations[otherIdx];

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

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), horizLoc, vertLoc, Hues.LightGray);

				horizLoc += 0x60;
			}
		}

		/// <summary>Appends " (shields/cost)" of the city's production to the text at TextBuffer.Address (0x87 is the shield)</summary>
		private void AppendProductionCost(short townIdx, short cost)
		{
			this.Owner.Words.Append("\x0087 (");
			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(this.Owner.World.Towns[townIdx].MaterialsTally, 10));
			this.Owner.Words.Append("/");
			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(cost, 10));
			this.Owner.Words.Append(")");
		}
	}
}
