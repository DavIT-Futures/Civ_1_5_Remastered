using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 14.
	/// </summary>
	public class AdvisorReports : GameModule
	{
		public AdvisorReports(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Keeps the screen in the back screen and draws the header of a report (the title at the address, in the style)
		/// </summary>
		public void DisplayReportPreamble(GameText title, ushort style)
		{
			this.processor.Journal.OpenScope($"DisplayReportPreamble(0x{title.Address:x4}, {style})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				ScreenBox,
				0, 0, 0x140, 0xc8,
				BackScreenBox,
				0, 0);

			RenderReportPreamble(title, style);

			this.processor.Journal.CloseScope("DisplayReportPreamble");
		}

		/// <summary>
		/// The head of an advisor report on a screen of the colour: the title, then the kind of state by government / 2
		/// (0x473c, 0x4743, 0x474b; for the others the text at TextBuffer.Address stays) " of the " civilization, then the ruler's title
		/// (0x19b2 + 2 * government), the leader's name and the year
		/// </summary>
		public void RenderReportPreamble(GameText title, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderReportPreamble(0x{title.Address:x4}, {style})");

			Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];
			ushort personSlot = (ushort)(this.owner.World.PersonParticipantIdx << 1);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, style);

			this.owner.TextPainter.RenderCenteredTextToScreen0(title, 160, 2, Hues.White);

			switch (person.RegimeKind / 2)
			{
				case 0:
					this.owner.Words.Set(0x473c);
					break;

				case 1:
					this.owner.Words.Set(0x4743);
					break;

				case 2:
					this.owner.Words.Set(0x474b);
					break;
			}

			this.owner.Words.Append(" of the ");
			this.owner.Words.Append(CivilizationPluralAt(personSlot));

			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 10, Hues.White);

			Message(GovernmentTitle(person.RegimeKind))
				.Text(" ")
				.Text(LeaderNameAt(personSlot))
				.Text(": ");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 18, Hues.White);

			this.processor.Journal.CloseScope("RenderReportPreamble");
		}

		/// <summary>Waits for a key or a click and puts the screen back from the back screen</summary>
		public void FinishReportScreen()
		{
			this.processor.Journal.OpenScope("FinishReportScreen()");

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("FinishReportScreen");
		}

		/// <summary>
		/// The science report: the bar of the research (cost = known technologies * (1 before year 0, else 2) * the larger of
		/// 2 * difficulty + 0xd2de + 6 and 11 - known technologies), filled with a bulb icon per step of progress, the human
		/// player's current research, and the known technologies in 3 columns (white when the player found it first, else cyan)
		/// </summary>
		public void DisplayScienceReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayScienceReport({participantIdx})");

			DisplayReportPreamble(GameTexts.TextScienceReport, 1);

			short knownTally = this.owner.World.Participants[participantIdx].FoundScienceTally;
			short costFactor = (short)((this.owner.World.HardnessTier << 1) + TechnologyLevel + 6);

			if (costFactor < (short)(0xb - knownTally))
			{
				costFactor = (short)(0xb - knownTally);
			}

			short cost = (short)(knownTally * (this.owner.World.Season >= 0 ? 2 : 1) * costFactor);
			int step = this.owner.Helpers.VerifyValueSpan(2432 / (cost + 1), 1, 64);
			int barBreadth = this.owner.Helpers.VerifyValueSpan(((step * cost) / 8) + 8, 8, 304);
			short barLeft = (short)(160 - (barBreadth / 2));

			if (barLeft > 0x28)
			{
				this.owner.VideoCard.RenderImage(PictureScreenBox, 0x118, 0x8c, 0x28, 0x3c, ScreenBox, 0x116, 0x2);
			}

			this.owner.Services.FloodBox(ScreenBox, barLeft, 25, barBreadth, 16, Hues.LightBlue);

			short progress = 0;

			for (short bulbIdx = 0; (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[participantIdx].InquiryHeadway, 0, cost) > bulbIdx; bulbIdx++)
			{
				this.owner.Services.RenderPictureToScreen(ScreenBox, (progress / 8) + barLeft, 33,
					SpritePicture(0xc));

				progress += (short)step;
			}

			short inquiry = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx;

			if (inquiry != -1)
			{
				Message("Researching ")
					.Text(TechnologyText(inquiry));

				this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 161, 26, Hues.Black);
				this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 26, Hues.White);
			}

			short vertLoc = 0x2a;
			short shown = 0;

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, technologyIdx) == 0)
					continue;

				this.owner.Words.Set(TechnologyText(technologyIdx));

				ushort hue = (ushort)((this.owner.World.ScienceLeadingFoundVia[technologyIdx] & 7) == participantIdx ? 0xf : 0xb);

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), ((shown % 3) * 100) + 8, vertLoc, hue);

				shown++;

				if (shown % 3 == 0)
				{
					vertLoc += 7;
				}

				if (vertLoc > 0xc0)
					break;
			}

			FinishReportScreen();

			this.processor.Journal.CloseScope("DisplayScienceReport");
		}

		/// <summary>
		/// The military report: a row for every unit type (0 - 27) the player has or builds, with the unit's picture, its name,
		/// "(attack/defence/moves)", the active units and the units in production; then the report of the units lost
		/// </summary>
		public void RenderUnitCountsChart(short participantIdx)
		{
			this.processor.Journal.OpenScope($"RenderUnitCountsChart({participantIdx})");

			DisplayReportPreamble(GameTexts.TextMilitaryStatus, 1);

			int rowIdx = 0;
			int vertLoc = 0x20;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				short active = this.owner.World.Participants[participantIdx].LiveTroops[kind];
				short making = this.owner.World.Participants[participantIdx].TroopsInsideMaking[kind];

				if (active <= 0 && making <= 0)
					continue;

				this.owner.VideoCard.RenderLine(ScreenBox, 36, vertLoc - 2, 319, vertLoc - 2, Hues.LightBlue);

				this.owner.Services.RenderPictureToScreen(ScreenBox, ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					SpritePicture(((participantIdx << 5) + kind + 0x40)));

				rowIdx++;

				ushort record = (ushort)((kind * UnitKindTable.Stride) + UnitKindTable.Origin);

				this.owner.TextPainter.RenderTextToScreen0(record, 36, vertLoc, Hues.White);

				this.owner.Words.Set("(");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)(record + UnitKindTable.AssaultPower)), 10));
				this.owner.Words.Append("/");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)(record + UnitKindTable.GuardPower)), 10));
				this.owner.Words.Append("/");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)(record + UnitKindTable.ShiftTally)), 10));
				this.owner.Words.Append(")");

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 112, vertLoc, Hues.LightCyan);

				if (active > 0)
				{
					EmptyMessage()
						.Number(active)
						.Text(" active");
					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 168, vertLoc, Hues.White);
				}

				if (making > 0)
				{
					EmptyMessage()
						.Number(making)
						.Text(" in production");
					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 232, vertLoc, Hues.LightCyan);
				}

				vertLoc += 9;
			}

			this.owner.VideoCard.RenderImage(PictureScreenBox, 0xa0, 0x8c, 0x28, 0x3c, ScreenBox, 0x116, 0x2);

			FinishReportScreen();

			RenderPlayerComparisonChart(participantIdx);

			this.processor.Journal.CloseScope("RenderUnitCountsChart");
		}

		/// <summary>
		/// The report of the units lost: a row for every unit type (0 - 26) that some player lost, with the unit's picture (of
		/// the player), its name, the losses of the player (colour 12) and of the players 1 - 7 in their colours, 28 pixels apart
		/// </summary>
		public void RenderPlayerComparisonChart(short participantIdx)
		{
			this.processor.Journal.OpenScope($"RenderPlayerComparisonChart({participantIdx})");

			DisplayReportPreamble(GameTexts.TextMilitaryLosses, 4);

			int rowIdx = 0;
			int vertLoc = 0x1c;

			for (int kind = 0; kind < 0x1b; kind++)
			{
				bool lost = false;

				for (int playerIdx = 0; playerIdx < 8; playerIdx++)
				{
					if (this.owner.World.Participants[playerIdx].MissingTroops[kind] != 0)
					{
						lost = true;
					}
				}

				if (!lost)
					continue;

				this.owner.VideoCard.RenderLine(ScreenBox, 36, vertLoc - 1, 319, vertLoc - 1, Hues.DarkGray);

				this.owner.Services.RenderPictureToScreen(ScreenBox, ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					SpritePicture(((participantIdx << 5) + kind + 0x40)));

				rowIdx++;

				this.owner.TextPainter.RenderTextToScreen0(UnitText(kind), 36, vertLoc, Hues.White);

				short losses = this.owner.World.Participants[participantIdx].MissingTroops[kind];

				if (losses != 0)
				{
					EmptyMessage()
						.Number(losses);
					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 104, vertLoc, Hues.LightRed);
				}

				int horizLoc = 0x68;

				for (short playerIdx = 1; playerIdx < 8; playerIdx++)
				{
					if (playerIdx == participantIdx)
						continue;

					horizLoc += 0x1c;
					losses = this.owner.World.Participants[playerIdx].MissingTroops[kind];

					if (losses != 0)
					{
						EmptyMessage()
							.Number(losses);
						this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), horizLoc, vertLoc,
							PlayerColour(playerIdx));
					}
				}

				vertLoc += 7;
			}

			this.owner.Dialogs.RenderBox(100, 25, 18, vertLoc - 25,
				PlayerColour(this.owner.World.PersonParticipantIdx));

			FinishReportScreen();

			this.processor.Journal.CloseScope("RenderPlayerComparisonChart");
		}

		/// <summary>
		/// The trade report: for every city of the player (after HandleCity) its luxuries / taxes / science, or "(Disorder)",
		/// the total income and the turns to the next discovery (of the human player, with the wonder effect 0x14 science * 1.5);
		/// on the right the upkeep of the buildings (count and cost, the palace 1 costs difficulty / 2 + 1 for each of the
		/// technologies 0x22 and 0x25 more) and the total cost. A new page after a key when the left column is full.
		/// </summary>
		public void DisplayTradeReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayTradeReport({participantIdx})");


			ushort[] costs = new ushort[0x18];

			short palaceCost = (short)(this.owner.World.HardnessTier / 2);

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22) != 0)
			{
				palaceCost++;
			}

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x25) != 0)
			{
				palaceCost++;
			}

			DisplayReportPreamble(GameTexts.TextTradeReport, 2);

			this.owner.VideoCard.RenderImage(PictureScreenBox, 0xc8, 0x8c, 0x28, 0x3c, ScreenBox, 0x116, 2);

			this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextCityTrade, 8, 32, Hues.White);

			short income = 0;
			short science = 0;

			for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
			{
				costs[structureIdx] = 0x0;
			}

			short vertLoc = 0x28;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				if (vertLoc >= 0xb4)
				{
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();

					RenderReportPreamble(GameTexts.TextTradeReportB, 2);

					vertLoc = 0x28;
				}

				this.owner.CityOps.HandleCity(townIdx, -1);

				EmptyMessage()
					.City(townIdx);

				this.owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 62);

				if (vertLoc < 0xb4)
				{
					this.owner.TextPainter.RenderTextWithShadow(this.owner.Words.ToString(), 16, vertLoc, Hues.White);
				}

				this.owner.Words.Clear();

				if ((town.ConditionMarker & 1) != 0)
				{
					this.owner.Words.Append(" \x008c(Disorder)");
				}
				else
				{
					this.owner.Words.Append(" ");

					this.owner.Words.Append(this.owner.Libc.IntegerToText((short)CityLuxuries, 10));

					this.owner.Words.Append("\\/");

					this.owner.Words.Append(this.owner.Libc.IntegerToText((short)CityTaxes, 10));

					this.owner.Words.Append("$/");

					this.owner.Words.Append(this.owner.Libc.IntegerToText((short)CityScience, 10));

					this.owner.Words.Append("~");

					income += (short)CityTaxes;
					science += (short)CityScience;
				}

				if (vertLoc < 0xb4)
				{
					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 80, vertLoc, Hues.LightGreen);

					vertLoc += 8;
				}

				for (int structureIdx = 1; structureIdx < 0x18; structureIdx++)
				{
					uint structure = 1u << structureIdx;

					if (((ushort)structure & town.StructureMarkers0) == 0 && ((ushort)(structure >> 16) & town.StructureMarkers1) == 0)
						continue;

					costs[structureIdx] = (ushort)(costs[structureIdx] + BuildingWord(structureIdx + 1, BuildingTable.Upkeep));

					if (structureIdx == 1)
					{
						costs[structureIdx] = (ushort)(costs[structureIdx] + palaceCost);
					}
				}
			}


			Message("Total Income: ")
				.Number(income)
				.Text("$");

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 8, vertLoc + 4, Hues.LightGreen);

			if (this.owner.CityOps.PlayerEnjoysWonderEffect(this.owner.World.PersonParticipantIdx, 0x14) != 0)
			{
				science += (short)(science / 2);
			}

			if (science != 0 && vertLoc < 0xb4)
			{
				short epoch = (short)(this.owner.World.Season < 0 ? 1 : this.owner.World.Season < 1750 ? 2 : 3);
				short turns = CPU.SignedQuotient((short)((short)(epoch * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceTally) *
					(short)((this.owner.World.HardnessTier << 1) + 6)), science);


				Message("Discoveries: ")
					.Number(turns)
					.Text(" turns");

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 8, vertLoc + 12, Hues.LightGreen);
			}

			this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextMaintenanceCosts, 160, 32, Hues.White);

			vertLoc = 0x28;
			short total = 0;

			for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
			{
				short cost = (short)costs[structureIdx];

				if (cost == 0)
					continue;

				this.owner.Words.Clear();

				// The number of the buildings: the cost by the upkeep (the palace by its own cost)
				short tally = structureIdx == 1 ? (short)(cost / palaceCost) :
					(short)(cost / (short)BuildingWord(structureIdx + 1, BuildingTable.Upkeep));

				this.owner.Words.Append(this.owner.Libc.IntegerToText(tally, 10));

				this.owner.Words.Append(" ");

				this.owner.Words.Append(BuildingText(structureIdx));

				this.owner.Words.Append(", ");

				this.owner.Words.Append(this.owner.Libc.IntegerToText(cost, 10));

				this.owner.Words.Append("$");

				if (vertLoc < 0xb4)
				{
					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 160, vertLoc, Hues.Yellow);

					vertLoc += 8;
				}

				total += cost;
			}

			Message("Total Cost: ")
				.Number(total)
				.Text("$");

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 160, vertLoc + 4, Hues.Yellow);

			vertLoc += 0xc;


			FinishReportScreen();

			this.processor.Journal.CloseScope("DisplayTradeReport");
		}

		/// <summary>
		/// The foreign minister's report: a row 0x18 high for every living player 1 - 7 from y 0x20. The player himself and the
		/// players he has contact with (Relations bit 0x40) get their name and title, government, treasury, units (on the
		/// easiest two levels), a colour and peace, alliance or war with each player, the others only "no contact" (0x485b).
		/// The rows from the second player get a box "INFO" + row, and a key 1 - 7 or a click right of x 0x119 shows the
		/// intelligence report of that row's player (the rows without contact read an entry of the list in the stack frame that
		/// was never written, like in the original, unless ClassicMilitaryReportRows is off: then they show nothing). Ends with a key or click elsewhere.
		/// </summary>
		public void DisplayMilitaryReport()
		{
			this.processor.Journal.OpenScope("DisplayMilitaryReport()");

			// The list of the rows' players of the frame of the original (B3: in C#; the rows without contact keep 0, the
			// classic rows read it)
			ushort[] rowPlayers = new ushort[8];

			// The same list in C#, with -1 for the rows without contact (ClassicMilitaryReportRows off)
			short[] rowPlayerList = [-1, -1, -1, -1, -1, -1, -1, -1];

			DisplayReportPreamble(GameTexts.TextIntelligenceReport, 1);

			ushort rowCount = 0;
			bool reportShown = false;
			ushort participantIdx;


			for (participantIdx = 1; (short)participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.LiveRealms & (1 << (byte)participantIdx)) == 0)
					continue;

				short vertLoc = (short)((0x18 * rowCount) + 0x20);

				this.owner.VideoCard.RenderLine(ScreenBox, 4, (short)(vertLoc - 2), 316, (short)(vertLoc - 2), Hues.LightBlue);

				this.owner.Words.Set(CivilizationPlural(participantIdx));

				if ((short)participantIdx != this.owner.World.PersonParticipantIdx &&
					(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) == 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextNoEmbassyEstablished, 96, vertLoc, PlayerColour(participantIdx));

					rowCount++;
					continue;
				}

				RenderMilitaryReportRow(participantIdx, vertLoc);

				rowPlayers[rowCount] = participantIdx;
				rowPlayerList[rowCount] = (short)participantIdx;
				rowCount++;

				if ((short)participantIdx == this.owner.World.PersonParticipantIdx)
					continue;

				Message("INFO")
					.Number((short)rowCount);

				short boxTop = (short)(0x18 * rowCount);

				this.owner.CityOps.RenderLabelBox(281, boxTop + 20, 318, boxTop + 29, TextBuffer.Address, PlayerColour(participantIdx));
			}

			this.owner.Startup.DisplayMouseCursor();

			ushort done;

			do
			{
				do
				{
					this.owner.Startup.ObtainMouseState();
				}
				while (this.owner.GlobDb3a == 0 && this.owner.Libc.KeyAvailable() == 0);

				done = 1;

				short rowIdx = -1;
				bool chosen = false;

				if (this.owner.Libc.KeyAvailable() != 0)
				{
					ushort key = this.owner.Dialogs.ObtainNavigationKey();

					if ((short)key >= 0x31 && (short)key <= 0x37)
					{
						rowIdx = (short)(key - 0x31);
						chosen = true;
					}
				}

				if (!chosen && (short)this.owner.GlobDb3c > 0x119)
				{
					rowIdx = CPU.SignedQuotient((short)(this.owner.GlobDb3e - 0x1c), 0x18);
					chosen = true;
				}

				if (chosen && rowIdx >= 0 && rowIdx < (short)rowCount)
				{
					bool classicRows = this.owner.TurnHooks.ClassicMilitaryReportRows;
					ushort rowPlayerIdx = classicRows ?
						rowPlayers[rowIdx] :
						(ushort)rowPlayerList[rowIdx];

					if ((classicRows || (short)rowPlayerIdx != -1) &&
						(short)rowPlayerIdx != this.owner.World.PersonParticipantIdx &&
						(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[rowPlayerIdx] & 0x40) != 0)
					{
						DisplayIntelligenceReport((short)rowPlayerIdx);

						reportShown = true;
						done = 0;
					}
				}

				participantIdx = this.owner.GlobDb3c;

				this.owner.TurnLogic.AwaitForKey();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.Services.AssignMousePosition(270, this.owner.GlobDb3e);

				this.owner.Startup.DisplayMouseCursor();
			}
			while (done == 0);


			this.owner.Startup.ConcealMouseCursor();

			if (reportShown)
			{
				this.owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayMilitaryReport");
		}

		/// <summary>
		/// The details of a player in the foreign minister's report (his name is in TextBuffer.Address): title, government, treasury, units
		/// and then peace, alliance or war with each player, 0x60 apart, in rows of 8 from x 0x10
		/// </summary>
		private void RenderMilitaryReportRow(ushort participantIdx, short vertLoc)
		{
			// The last city of the player with a palace, never used
			ushort capitalIdx = 0xffff;
			ushort townIdx;

			for (townIdx = 0; (short)townIdx < 0x80; townIdx++)
			{
				if ((ushort)this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx &&
					this.owner.World.Towns[townIdx].ConditionMarker != 0xff &&
					this.owner.World.Towns[townIdx].Has(CityStructure.Palace))
				{
					capitalIdx = townIdx;
				}
			}


			this.owner.Words.Append(": ");

			this.owner.Words.Append(LeaderName(participantIdx));

			this.owner.TextPainter.RenderTextWithShadow(this.owner.Words.ToString(), 8, vertLoc, Hues.White);

			Message(GovernmentName((ushort)this.owner.World.Participants[participantIdx].RegimeKind))
				.Text(", ")
				.Number(this.owner.World.Participants[participantIdx].Gold)
				.Text("$");

			if (this.owner.World.HardnessTier <= 1)
			{
				this.owner.Words.Append(", ");

				this.owner.Words.Append(this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].TroopTally, 10));

				this.owner.Words.Append(" Units.");
			}

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 160, vertLoc, PlayerColour(participantIdx));

			vertLoc += 8;

			short horizLoc = 0x10;
			ushort otherIdx;

			for (otherIdx = 1; (short)otherIdx < 8; otherIdx++)
			{
				this.owner.Words.Clear();

				this.owner.Words.Set(CivilizationPlural(otherIdx));

				if (horizLoc > 0xf0)
				{
					horizLoc = 0x10;
					vertLoc += 8;
				}

				ushort relation = this.owner.World.Participants[participantIdx].Relations[otherIdx];

				if ((relation & 2) != 0)
				{
					this.owner.Words.Append((relation & 4) != 0 ? ":\x008f ALLIED" : ":\x008f PEACE");
				}
				else if ((relation & 3) != 1)
				{
					continue;
				}
				else
				{
					this.owner.Words.Append(":\x008c WAR");
				}

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), horizLoc, vertLoc, Hues.LightGray);

				horizLoc += 0x60;
			}

		}

		/// <summary>
		/// The intelligence report on a civilization: its leader (with the traits of the leader table, 0x1512 + 0x3a * leader),
		/// capital (the last city with a palace), government, treasury, military (on the easiest two levels), its relations
		/// with the others (peace or war) and its technologies from the newest, until the screen is full.
		/// </summary>
		public void DisplayIntelligenceReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayIntelligenceReport({participantIdx})");

			Player participant = this.owner.World.Participants[participantIdx];

			DisplayReportPreamble(GameTexts.TextIntelligenceReportB, 1);

			short vertLoc = 0x20;

			Message("Subject: the ")
				.Text(CivilizationPlural(participantIdx));

			this.owner.TextPainter.RenderTextWithShadow(this.owner.Words.ToString(), 16, 32, Hues.White);

			vertLoc += 0xc;

			Message("Leader:\x008f ")
				.Text(GovernmentTitle(participant.RegimeKind))
				.Text(" ")
				.Text(LeaderName(participantIdx));

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			// The traits of the leader: three words, each 1 or -1, each with its own text
			ushort[] traits = { 0x48d3, 0x48de, 0x48e7, 0x48f6, 0x4904, 0x490e };

			for (int traitIdx = 0; traitIdx < 6; traitIdx++)
			{
				ushort trait = this.processor.PullGlobalWord((ushort)((0x3a * participant.ChiefVisuals) + 0x1512 + ((traitIdx >> 1) << 1)));

				if (trait == ((traitIdx & 1) == 0 ? 1 : 0xffff))
				{
					this.owner.TextPainter.RenderTextToScreen0(traits[traitIdx], 24, vertLoc, Hues.LightGray);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short capitalIdx = -1;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff && town.Has(CityStructure.Palace))
				{
					capitalIdx = townIdx;
				}
			}

			Message("Capital:\x008f ")
				.City(capitalIdx);

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			Message("Government:\x008f ")
				.Text(GovernmentName(participant.RegimeKind));

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			Message("Treasury:\x008f ")
				.Number(participant.Gold)
				.Text("$");

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			if (this.owner.World.HardnessTier <= 1)
			{
				Message("Military:\x008f ")
					.Number(participant.TroopTally)
					.Text(" Units.");

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc, Hues.LightBlue);

				vertLoc += 0xc;
			}

			this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextForeignAffairs, 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			for (int otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				// With the 1 of contact one of the two applies (3 is peace)
				if ((participant.Relations[otherIdx] & 2) != 0)
				{
					this.owner.Words.Set("At\x008f PEACE");
				}

				if ((participant.Relations[otherIdx] & 3) == 1)
				{
					this.owner.Words.Set("At\x008c WAR");
				}

				if ((participant.Relations[otherIdx] & 1) == 0)
					continue;

				this.owner.Words.Append("\x0087 with the ");

				this.owner.Words.Append(CivilizationPlural(otherIdx));

				this.owner.Words.Append(".");

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 24, vertLoc, Hues.LightGray);

				vertLoc += 8;
			}

			vertLoc += 4;

			this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextTechnologies, 16, vertLoc, Hues.LightBlue);

			vertLoc += 8;

			for (short scienceIdx = 0x47; scienceIdx >= 0; scienceIdx--)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
					continue;

				this.owner.TextPainter.RenderTextToScreen0(TechnologyText(scienceIdx), 24, vertLoc, Hues.LightGray);

				vertLoc += 8;

				if (vertLoc >= 0xc0)
					break;
			}

			FinishReportScreen();

			this.processor.Journal.CloseScope("DisplayIntelligenceReport");
		}

		/// <summary>
		/// The attitude report (after HandleCity of each city of the player): its name, its citizens and the icons of its
		/// buildings that make people happy (mask 0x2618), 10 pixels a city, a new page after each key or click; then the
		/// percentages of happy, content and unhappy citizens of all the cities. The stack frame stays: RenderCitizenIcons reads
		/// what is left below it, so it must be called at the same depth as in the original.
		/// </summary>
		public void DisplayAttitudeReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayAttitudeReport({participantIdx})");


			DisplayReportPreamble(GameTexts.TextAttitudeSurvey, 9);

			short vertLoc = 0x20;
			short unhappy = 0;
			short happy = 0;
			short citizens = 0;
			short shift = 0;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff || this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				if (vertLoc > 0xbe)
				{
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();

					RenderReportPreamble(GameTexts.TextAttitudeSurveyB, 9);

					vertLoc = 0x20;
				}

				this.owner.CityOps.HandleCity(townIdx, -1);

				EmptyMessage()
					.City(townIdx)
					.Text(":");
				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc, Hues.White);

				this.owner.CityOps.RenderCitizenIcons(townIdx, (short)((shift & 7) + 0x50), (short)(vertLoc - 3), CitySpecialists, 124);

				short iconHoriz = 0xd4;

				this.owner.Services.FloodBox(ScreenBox, 212, vertLoc - 1, 90, 10, Hues.LightCyan);

				for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
				{
					if ((this.owner.World.Towns[townIdx].StructureMarkers0 & (1 << structureIdx) & 0x2618) == 0)
						continue;

					this.owner.CityOps.RenderResourceIcon((short)(structureIdx + 1), iconHoriz, (short)(vertLoc - 1));

					iconHoriz += 0x12;
				}

				citizens += this.owner.World.Towns[townIdx].TrueExtent;
				happy += (short)CityHappy;
				unhappy += (short)CityUnhappy;
				shift += 4;
				vertLoc += 0xa;
			}

			if (citizens == 0)
			{
				citizens = 1;
			}

			// The population is computed, but not written
			this.owner.Words.Set("Population: ");
			this.owner.Helpers.ObtainPlayerPopulation(participantIdx);

			this.owner.Words.Append(" Happy:");
			AffixProductionCount(happy, citizens);
			this.owner.Words.Append("% Content:");
			AffixProductionCount((short)(citizens - happy - unhappy), citizens);
			this.owner.Words.Append("% Unhappy:");
			AffixProductionCount(unhappy, citizens);
			this.owner.Words.Append("%");

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, vertLoc + 8, Hues.White);

			FinishReportScreen();


			this.processor.Journal.CloseScope("DisplayAttitudeReport");
		}

		/// <summary>
		/// Appends the count as a percentage of the total (from 300 on computed with the total in tens, as the original avoids
		/// the overflow)
		/// </summary>
		public void AffixProductionCount(short count, short total)
		{
			this.processor.Journal.OpenScope($"AffixProductionCount({count}, {total})");

			if (count < 300)
			{
				this.owner.Words.Append(this.owner.Libc.IntegerToText((100 * count) / total, 10));
			}
			else
			{
				this.owner.Words.Append(this.owner.Libc.IntegerToText((10 * count) / (total / 10), 10));
			}

			this.processor.Journal.CloseScope("AffixProductionCount");
		}

		/// <summary>
		/// The city status report (after HandleCity of each city of the player): its name, "size-food{ shields| trade}" and what it
		/// builds "(shields/cost)" (a building for a negative index, a unit below 0x40; for another index the previous text is
		/// drawn there again), 8 pixels a city, a new page after each key or click
		/// </summary>
		public void DisplayCityStatusReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCityStatusReport({participantIdx})");

			DisplayReportPreamble(GameTexts.TextCityStatus, 8);

			int vertLoc = 0x20;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff || this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				if (vertLoc >= 0xc0)
				{
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();

					RenderReportPreamble(GameTexts.TextCityStatusB, 8);

					vertLoc = 0x20;
				}

				this.owner.CityOps.HandleCity(townIdx, -1);

				EmptyMessage()
					.City(townIdx);
				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 8, vertLoc, Hues.White);

				EmptyMessage()
					.Number(this.owner.World.Towns[townIdx].TrueExtent)
					.Text("-")
					.Number((short)CityFood)
					.Text("{ ")
					.Number((short)CityShields)
					.Text("| ")
					.Number((short)CityTrade)
					.Text("}");
				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 80, vertLoc, Hues.White);

				sbyte making = this.owner.World.Towns[townIdx].PresentMakingIdx;

				if (making < 0)
				{
					// A building: names of 0x1e bytes at 0xb9a, costs at 0xbb2
					this.owner.Words.Set(BuildingRecordText(-making));
					this.owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 96);
					AppendProductionCost(townIdx, (short)(10 * BuildingWord(-making, BuildingTable.Price)));
				}
				else if (making < 0x40)
				{
					ushort record = (ushort)((making * UnitKindTable.Stride) + UnitKindTable.Origin);
					this.owner.Words.Set(record);
					AppendProductionCost(townIdx, (short)(10 * this.processor.PullGlobalWord((ushort)(record + UnitKindTable.Price))));
				}

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 172, vertLoc, Hues.White);

				vertLoc += 8;
			}

			FinishReportScreen();

			this.processor.Journal.CloseScope("DisplayCityStatusReport");
		}

		/// <summary>Appends " (shields/cost)" of the city's production to the text at TextBuffer.Address (0x87 is the shield)</summary>
		private void AppendProductionCost(short townIdx, short cost)
		{
			this.owner.Words.Append("\x0087 (");
			this.owner.Words.Append(this.owner.Libc.IntegerToText(this.owner.World.Towns[townIdx].MaterialsTally, 10));
			this.owner.Words.Append("/");
			this.owner.Words.Append(this.owner.Libc.IntegerToText(cost, 10));
			this.owner.Words.Append(")");
		}
	}
}
