using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 14.
	/// </summary>
	public class AdvisorReports
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public AdvisorReports(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="mode"></param>
		public void DisplayReportPreamble(ushort charsAddr, ushort style)
		{
			this.processor.Journal.OpenScope($"DisplayReportPreamble(0x{charsAddr:x4}, {style})");

			// function body
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			RenderReportPreamble(charsAddr, style);

			// Far return
			this.processor.Journal.CloseScope("DisplayReportPreamble");
		}

		/// <summary>
		/// The head of an advisor report on a screen of the colour: the title, then the kind of state by government / 2
		/// (0x473c, 0x4743, 0x474b; for the others the text at 0xba06 stays) " of the " civilization, then the ruler's title
		/// (0x19b2 + 2 * government), the leader's name and the year
		/// </summary>
		public void RenderReportPreamble(ushort charsAddr, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderReportPreamble(0x{charsAddr:x4}, {style})");

			Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];
			ushort personSlot = (ushort)(this.owner.World.PersonParticipantIdx << 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, style);

			this.owner.TextPainter.RenderCenteredTextToScreen0(charsAddr, 160, 2, 15);

			switch (person.RegimeKind / 2)
			{
				case 0:
					this.owner.Libc.CopyText(0xba06, 0x473c);
					break;

				case 1:
					this.owner.Libc.CopyText(0xba06, 0x4743);
					break;

				case 2:
					this.owner.Libc.CopyText(0xba06, 0x474b);
					break;
			}

			this.owner.Libc.AppendText(0xba06, " of the ");
			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(personSlot + 0x1992)));

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 10, 15);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((person.RegimeKind << 1) + 0x19b2)));
			this.owner.Libc.AppendText(0xba06, " ");
			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(personSlot + 0x19a2)));
			this.owner.Libc.AppendText(0xba06, ": ");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 18, 15);

			this.processor.Journal.CloseScope("RenderReportPreamble");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void FinishReportScreen()
		{
			this.processor.Journal.OpenScope("FinishReportScreen()");

			// function body
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			// Far return
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

			DisplayReportPreamble(0x4762, 1);

			short knownTally = this.owner.World.Participants[participantIdx].FoundScienceTally;
			short costFactor = (short)((this.owner.World.HardnessTier << 1) + this.processor.PullGlobalWord(0xd2de) + 6);

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
				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), 0x118, 0x8c, 0x28, 0x3c, this.processor.PullGlobalWord(0xaa), 0x116, 0x2);
			}

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), barLeft, 25, barBreadth, 16, 9);

			short progress = 0;

			for (short bulbIdx = 0; (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[participantIdx].InquiryHeadway, 0, cost) > bulbIdx; bulbIdx++)
			{
				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), (progress / 8) + barLeft, 33,
					this.processor.PullGlobalWord((0xc << 1) + 0xd4ce));

				progress += (short)step;
			}

			short inquiry = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx;

			if (inquiry != -1)
			{
				this.owner.Libc.CopyText(0xba06, "Researching ");
				this.owner.Libc.AppendText(0xba06, (ushort)((inquiry * 0x16) + 0x4da));

				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 161, 26, 0);
				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 26, 15);
			}

			short vertLoc = 0x2a;
			short shown = 0;

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, technologyIdx) == 0)
					continue;

				this.owner.Libc.CopyText(0xba06, (ushort)((technologyIdx * 0x16) + 0x4da));

				ushort hue = (ushort)((this.owner.World.ScienceLeadingFoundVia[technologyIdx] & 7) == participantIdx ? 0xf : 0xb);

				this.owner.TextPainter.RenderTextToScreen0(0xba06, ((shown % 3) * 100) + 8, vertLoc, hue);

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

			DisplayReportPreamble(0x477e, 1);

			int rowIdx = 0;
			int vertLoc = 0x20;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				short active = this.owner.World.Participants[participantIdx].LiveTroops[kind];
				short making = this.owner.World.Participants[participantIdx].TroopsInsideMaking[kind];

				if (active <= 0 && making <= 0)
					continue;

				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 36, vertLoc - 2, 319, vertLoc - 2, 9);

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + kind + 0x40) << 1) + 0xd4ce)));

				rowIdx++;

				ushort record = (ushort)((kind * UnitKindTable.Stride) + UnitKindTable.Origin);

				this.owner.TextPainter.RenderTextToScreen0(record, 36, vertLoc, 15);

				this.owner.Libc.CopyText(0xba06, "(");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)(record + UnitKindTable.AssaultPower)), 10));
				this.owner.Libc.AppendText(0xba06, "/");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)(record + UnitKindTable.GuardPower)), 10));
				this.owner.Libc.AppendText(0xba06, "/");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)(record + UnitKindTable.ShiftTally)), 10));
				this.owner.Libc.AppendText(0xba06, ")");

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 112, vertLoc, 11);

				if (active > 0)
				{
					this.processor.EmitGlobalByte(0xba06, 0);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(active, 10));
					this.owner.Libc.AppendText(0xba06, " active");
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 168, vertLoc, 15);
				}

				if (making > 0)
				{
					this.processor.EmitGlobalByte(0xba06, 0);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(making, 10));
					this.owner.Libc.AppendText(0xba06, " in production");
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 232, vertLoc, 11);
				}

				vertLoc += 9;
			}

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), 0xa0, 0x8c, 0x28, 0x3c, this.processor.PullGlobalWord(0xaa), 0x116, 0x2);

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

			DisplayReportPreamble(0x47ad, 4);

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

				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 36, vertLoc - 1, 319, vertLoc - 1, 8);

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + kind + 0x40) << 1) + 0xd4ce)));

				rowIdx++;

				this.owner.TextPainter.RenderTextToScreen0((ushort)((kind * 0x22) + 0x112a), 36, vertLoc, 15);

				short losses = this.owner.World.Participants[participantIdx].MissingTroops[kind];

				if (losses != 0)
				{
					this.processor.EmitGlobalByte(0xba06, 0);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(losses, 10));
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, vertLoc, 12);
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
						this.processor.EmitGlobalByte(0xba06, 0);
						this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(losses, 10));
						this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc, vertLoc,
							this.processor.PullGlobalWord((ushort)((playerIdx << 1) + 0x1946)));
					}
				}

				vertLoc += 7;
			}

			this.owner.Dialogs.RenderBox(100, 25, 18, vertLoc - 25,
				this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1946)));

			FinishReportScreen();

			this.processor.Journal.CloseScope("RenderPlayerComparisonChart");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayTradeReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayTradeReport({participantIdx})");

			// function body
			this.processor.EnterFrame(0x40);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.Value16 = (ushort)owner.World.HardnessTier;
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0x3e, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.IncrementLocalWord(0x3e);
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x25);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.IncrementLocalWord(0x3e);
			}
		
			DisplayReportPreamble(0x47bd, 2);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0xc8, 0x8c, 0x28, 0x3c,
				this.processor.PullGlobalWord(0xaa),
				0x116, 2);

			this.owner.TextPainter.RenderTextToScreen0(0x47ca, 8, 32, 15);

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, this.processor.RegAcc.Value16);

		L088a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x34), 0x0);
			this.processor.IncrementLocalWord(0x3c);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x18);
			if (this.processor.FlagSet.IsLess) goto L088a;
			this.processor.EmitLocalWord(0x36, 0x28);
			this.processor.EmitLocalWord(0x38, 0x0);
			goto L09d3;

		L08aa:
			this.owner.Libc.AppendText(0xba06, " ");

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70e0), 10));

			this.owner.Libc.AppendText(0xba06, "\\/");

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0xe17a), 10));

			this.owner.Libc.AppendText(0xba06, "$/");

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70e6), 10));

			this.owner.Libc.AppendText(0xba06, "~");

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));

		L0959:
			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0xb4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L097b;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 80, this.processor.PullLocalSignedWord(0x36), 10);

			this.processor.EmitLocalWord(0x36, this.processor.SumWord(this.processor.PullLocalWord(0x36), 0x8));

		L097b:
			this.processor.EmitLocalWord(0x3c, 0x1);

		L0980:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x3c);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 28;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x38));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[this.processor.PullLocalWord(0x38)].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[this.processor.PullLocalWord(0x38)].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsEqual) goto L09c7;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, 0x34);
			this.processor.ScaleIndexToBase(0x1e, this.processor.PullLocalWord(0x3c));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd2));
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L09c7;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3e);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), this.processor.RegAcc.Value16));

		L09c7:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x18);
			if (this.processor.FlagSet.IsLess) goto L0980;

		L09d0:
			this.processor.EmitLocalWord(0x38, this.processor.IncrementWord(this.processor.PullLocalWord(0x38)));

		L09d3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0x80);
			if (this.processor.FlagSet.IsLess) goto L09dd;
			goto L0a8d;

		L09dd:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x38));

			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x38)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L09d0;
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x38)].ParticipantIdx != participantIdx)
				goto L09d0;

			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0xb4);
			if (this.processor.FlagSet.IsLess) goto L0a16;

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderReportPreamble(0x47d5, 2);

			this.processor.EmitLocalWord(0x36, 0x28);

		L0a16:
			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x38), -1);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x38));

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 62);

			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0xb4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a63;

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				16,
				this.processor.PullLocalSignedWord(0x36),
				15);

		L0a63:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x38));
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullLocalWord(0x38)].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0a7a;
			goto L08aa;

		L0a7a:
			this.owner.Libc.AppendText(0xba06, " \x008c(Disorder)");
			goto L0959;

		L0a8d:
			this.owner.Libc.CopyText(0xba06, "Total Income: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x2), 10));

			this.owner.Libc.AppendText(0xba06, "$");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, this.processor.PullLocalSignedWord(0x36) + 4, 10);

			this.owner.CityOps.PlayerEnjoysWonderEffect(this.owner.World.PersonParticipantIdx, 0x14);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0b07;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));

		L0b07:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0b10;
			goto L0baf;

		L0b10:
			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0xb4);
			if (this.processor.FlagSet.IsLess) goto L0b1a;
			goto L0baf;

		L0b1a:
			if (this.owner.World.Season >= 0) goto L0b26;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0b36;

		L0b26:
			if (this.owner.World.Season>= 1750) goto L0b33;
			this.processor.RegAcc.Value16 = 0x2;
			goto L0b36;

		L0b33:
			this.processor.RegAcc.Value16 = 0x3;

		L0b36:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceTally);
			this.processor.RegCount.Value16 = (ushort)owner.World.HardnessTier;
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x6);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Discoveries: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x3a), 10));

			this.owner.Libc.AppendText(0xba06, " turns");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, this.processor.PullLocalSignedWord(0x36) + 12, 10);

		L0baf:
			this.owner.TextPainter.RenderTextToScreen0(0x481f, 160, 32, 15);

			this.processor.EmitLocalWord(0x36, 0x28);
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x3c, 0x0);
			goto L0ca9;

		L0bd9:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullUInt16(this.processor.SegStack.Value16, 
					(ushort)(this.processor.RegFrame.Value16 + (this.processor.PullLocalWord(0x3c) << 1) - 0x34)) /
					(short)this.processor.PullUInt16(this.processor.SegData.Value16, 
					(ushort)((0x1e * this.processor.PullLocalWord(0x3c) + 0xbd2) & 0xffff))), 10));

		L0bf6:
			this.owner.Libc.AppendText(0xba06, " ");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0x3c));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ", ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(
					(short)this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 +
						(this.processor.PullLocalWord(0x3c) << 1) - 0x34)), 10));

			this.owner.Libc.AppendText(0xba06, "$");

			this.processor.CompareWord(this.processor.PullLocalWord(0x36), 0xb4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c9b;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 160, this.processor.PullLocalSignedWord(0x36), 14);

			this.processor.EmitLocalWord(0x36, this.processor.SumWord(this.processor.PullLocalWord(0x36), 0x8));

		L0c9b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x34));
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));

		L0ca6:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L0ca9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x18);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0cda;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x34));
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ca6;
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0cc9;
			goto L0bd9;

		L0cc9:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.RegSource.Value16 / 
					(short)this.processor.PullLocalWord(0x3e)), 10));

			goto L0bf6;

		L0cda:
			this.owner.Libc.CopyText(0xba06, "Total Cost: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x2), 10));

			this.owner.Libc.AppendText(0xba06, "$");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 160, this.processor.PullLocalSignedWord(0x36) + 4, 14);

			this.processor.EmitLocalWord(0x36, this.processor.SumWord(this.processor.PullLocalWord(0x36), 0xc));

			FinishReportScreen();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTradeReport");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayMilitaryReport()
		{
			this.processor.Journal.OpenScope("DisplayMilitaryReport()");

			// function body
			this.processor.EnterFrame(0x26);
			this.processor.PreserveIndexRegisters();

			DisplayReportPreamble(0x4847, 1);

			this.processor.EmitLocalWord(0x1e, 0x20);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, 0x1);
			goto L0fee;

		L0d6f:
			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.EmitLocalWord(0x1c, 0x0);

		L0d79:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x1c));
			this.processor.CompareWord((ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x1c)].ParticipantIdx,
				this.processor.PullLocalWord(0x4));
			if (this.processor.FlagSet.IsNotEqual) goto L0d9f;
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x1c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0d9f;
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x1c)].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0d9f;
			this.processor.CopyLocalWord(0x1c, 0x6);

		L0d9f:
			this.processor.EmitLocalWord(0x1c, this.processor.IncrementWord(this.processor.PullLocalWord(0x1c)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0d79;

			this.owner.Libc.AppendText(0xba06, ": ");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)));

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				8,
				this.processor.PullLocalSignedWord(0x1e),
				15);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x4)].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1966)));

			this.owner.Libc.AppendText(0xba06, ", ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x4)].Gold, 10));

			this.owner.Libc.AppendText(0xba06, "$");

			if (this.owner.World.HardnessTier > 1) goto L0e84;

			this.owner.Libc.AppendText(0xba06, ", ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x4)].TroopTally, 10));

			this.owner.Libc.AppendText(0xba06, " Units.");

		L0e84:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 
				160,
				this.processor.PullLocalSignedWord(0x1e),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			this.processor.EmitLocalWord(0x1e, this.processor.SumWord(this.processor.PullLocalWord(0x1e), 0x8));
			this.processor.EmitLocalWord(0x1a, 0x10);
			this.processor.EmitLocalWord(0x1c, 0x1);
			goto L0f1d;

		L0eb0:
			this.owner.Libc.AppendText(0xba06, ":\x008f PEACE");

		L0eb3:
			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0x1a),
				this.processor.PullLocalSignedWord(0x1e),
				7);

			this.processor.EmitLocalWord(0x1a, this.processor.SumWord(this.processor.PullLocalWord(0x1a), 0x60));

		L0eda:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x1c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			
			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x4)].Relations[this.processor.PullLocalWord(0x1c)] & 3) != 1)
				goto L0f1a;

			this.owner.Libc.AppendText(0xba06, ":\x008c WAR");

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0x1a),
				this.processor.PullLocalSignedWord(0x1e),
				7);

			this.processor.EmitLocalWord(0x1a, this.processor.SumWord(this.processor.PullLocalWord(0x1a), 0x60));

		L0f1a:
			this.processor.EmitLocalWord(0x1c, this.processor.IncrementWord(this.processor.PullLocalWord(0x1c)));

		L0f1d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1c), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f78;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x1c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), 0xf0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0f4d;
			this.processor.EmitLocalWord(0x1a, 0x10);
			this.processor.EmitLocalWord(0x1e, this.processor.SumWord(this.processor.PullLocalWord(0x1e), 0x8));

		L0f4d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x1c);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			
			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x4)].Relations[this.processor.PullLocalWord(0x1c)] & 2) == 0)
				goto L0eda;

			if ((this.owner.World.Participants[this.processor.PullLocalWord(0x4)].Relations[this.processor.PullLocalWord(0x1c)] & 4) == 0)
				goto L0eb0;

			this.owner.Libc.AppendText(0xba06, ":\x008f ALLIED");

			goto L0eb3;

		L0f78:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x16), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0feb;

			this.owner.Libc.CopyText(0xba06, "INFO");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x2), 10));

			this.processor.ScaleIndexToSource(0x18, this.processor.PullLocalWord(0x2));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.CityOps.RenderLabelBox(281, (short)this.processor.RegSource.Value16 + 20, 318, (short)this.processor.RegSource.Value16 + 29, 0xba06,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

		L0feb:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L0fee:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0ff7;
			goto L1079;

		L0ff7:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.LiveRealms);
			if (this.processor.FlagSet.IsEqual) goto L0feb;
			this.processor.ScaleIndex(0x18, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x20);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, 0x2);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 
				4, (short)this.processor.RegSource.Value16, 316, (short)this.processor.RegSource.Value16, 9);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L104c;
			goto L0d6f;

		L104c:
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x4)] & 0x40) != 0)
				goto L0d6f;

			this.owner.TextPainter.RenderTextToScreen0(0x485b, 
				96,
				this.processor.PullLocalSignedWord(0x1e),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.processor.IncrementLocalWord(0x2);
			goto L0feb;

		L1079:
			this.owner.Startup.DisplayMouseCursor();

		L107e:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1093;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L107e;

		L1093:
			this.processor.EmitLocalWord(0x18, 0x1);

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L10b8;

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x31);
			if (this.processor.FlagSet.IsLess) goto L10b8;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x37);
			if (this.processor.FlagSet.IsGreater) goto L10b8;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x31);
			goto L10cc;

		L10b8:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x119);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1114;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x1c);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x18;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);

		L10cc:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L1114;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1114;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1114;
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x4)] & 0x40) == 0)
				goto L1114;

			DisplayIntelligenceReport(this.processor.PullLocalSignedWord(0x4));

			this.processor.EmitLocalWord(0x24, 0x1);
			this.processor.EmitLocalWord(0x18, 0x0);

		L1114:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.AssignMousePosition(270, this.owner.GlobDb3e);
			
			this.owner.Startup.DisplayMouseCursor();

			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1142;
			goto L107e;

		L1142:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1154;

			this.owner.TurnHooks.RepaintMainScreen();

			goto L1159;

		L1154:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

		L1159:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayMilitaryReport");
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

			DisplayReportPreamble(0x48a5, 1);

			short vertLoc = 0x20;

			this.owner.Libc.CopyText(0xba06, "Subject: the ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)));

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, 32, 15);

			vertLoc += 0xc;

			this.owner.Libc.CopyText(0xba06, "Leader:\x008f ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participant.RegimeKind << 1) + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x19a2)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc, 9);

			vertLoc += 8;

			// The traits of the leader: three words, each 1 or -1, each with its own text
			ushort[] traits = { 0x48d3, 0x48de, 0x48e7, 0x48f6, 0x4904, 0x490e };

			for (int traitIdx = 0; traitIdx < 6; traitIdx++)
			{
				ushort trait = this.processor.PullGlobalWord((ushort)((0x3a * participant.ChiefVisuals) + 0x1512 + ((traitIdx >> 1) << 1)));

				if (trait == ((traitIdx & 1) == 0 ? 1 : 0xffff))
				{
					this.owner.TextPainter.RenderTextToScreen0(traits[traitIdx], 24, vertLoc, 7);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short capitalIdx = -1;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff && (town.StructureMarkers0 & 1) != 0)
				{
					capitalIdx = townIdx;
				}
			}

			this.owner.Libc.CopyText(0xba06, "Capital:\x008f ");

			this.owner.Conquest.ObtainCityLabel(capitalIdx);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc, 9);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "Government:\x008f ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participant.RegimeKind << 1) + 0x1966)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc, 9);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "Treasury:\x008f ");

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(participant.Gold, 10));

			this.owner.Libc.AppendText(0xba06, "$");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc, 9);

			vertLoc += 8;

			if (this.owner.World.HardnessTier <= 1)
			{
				this.owner.Libc.CopyText(0xba06, "Military:\x008f ");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(participant.TroopTally, 10));

				this.owner.Libc.AppendText(0xba06, " Units.");

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc, 9);

				vertLoc += 0xc;
			}

			this.owner.TextPainter.RenderTextToScreen0(0x4956, 16, vertLoc, 9);

			vertLoc += 8;

			for (int otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				// With the 1 of contact one of the two applies (3 is peace)
				if ((participant.Relations[otherIdx] & 2) != 0)
				{
					this.owner.Libc.CopyText(0xba06, "At\x008f PEACE");
				}

				if ((participant.Relations[otherIdx] & 3) == 1)
				{
					this.owner.Libc.CopyText(0xba06, "At\x008c WAR");
				}

				if ((participant.Relations[otherIdx] & 1) == 0)
					continue;

				this.owner.Libc.AppendText(0xba06, "\x0087 with the ");

				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((otherIdx << 1) + 0x1992)));

				this.owner.Libc.AppendText(0xba06, ".");

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 24, vertLoc, 7);

				vertLoc += 8;
			}

			vertLoc += 4;

			this.owner.TextPainter.RenderTextToScreen0(0x4987, 16, vertLoc, 9);

			vertLoc += 8;

			for (short scienceIdx = 0x47; scienceIdx >= 0; scienceIdx--)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
					continue;

				this.owner.TextPainter.RenderTextToScreen0((ushort)((scienceIdx * 0x16) + 0x4da), 24, vertLoc, 7);

				vertLoc += 8;

				if (vertLoc >= 0xc0)
					break;
			}

			FinishReportScreen();

			// Far return
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

			this.processor.EnterFrame(0x14);
			this.processor.PreserveSource();

			DisplayReportPreamble(0x4995, 9);

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

					RenderReportPreamble(0x49a5, 9);

					vertLoc = 0x20;
				}

				this.owner.CityOps.HandleCity(townIdx, -1);

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Conquest.ObtainCityLabel(townIdx);
				this.owner.Libc.AppendText(0xba06, ":");
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc, 15);

				this.owner.CityOps.RenderCitizenIcons(townIdx, (short)((shift & 7) + 0x50), (short)(vertLoc - 3), this.processor.PullGlobalWord(0xe8b8), 124);

				short iconHoriz = 0xd4;

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 212, vertLoc - 1, 90, 10, 11);

				for (int structureIdx = 0; structureIdx < 0x18; structureIdx++)
				{
					if ((this.owner.World.Towns[townIdx].StructureMarkers0 & (1 << structureIdx) & 0x2618) == 0)
						continue;

					this.owner.CityOps.RenderResourceIcon((short)(structureIdx + 1), iconHoriz, (short)(vertLoc - 1));

					iconHoriz += 0x12;
				}

				citizens += this.owner.World.Towns[townIdx].TrueExtent;
				happy += (short)this.processor.PullGlobalWord(0x70e2);
				unhappy += (short)this.processor.PullGlobalWord(0x70e4);
				shift += 4;
				vertLoc += 0xa;
			}

			if (citizens == 0)
			{
				citizens = 1;
			}

			// The population is computed, but not written
			this.owner.Libc.CopyText(0xba06, "Population: ");
			this.owner.Helpers.ObtainPlayerPopulation(participantIdx);

			this.owner.Libc.AppendText(0xba06, " Happy:");
			AffixProductionCount(happy, citizens);
			this.owner.Libc.AppendText(0xba06, "% Content:");
			AffixProductionCount((short)(citizens - happy - unhappy), citizens);
			this.owner.Libc.AppendText(0xba06, "% Unhappy:");
			AffixProductionCount(unhappy, citizens);
			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, vertLoc + 8, 15);

			FinishReportScreen();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayAttitudeReport");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void AffixProductionCount(short arg1, short arg2)
		{
			this.processor.Journal.OpenScope($"AffixProductionCount({arg1}, {arg2})");

			// function body
			if (arg1 < 300)
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((100 * arg1) / arg2, 10));
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((10 * arg1) / (arg2 / 10), 10));
			}

			// Far return
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

			DisplayReportPreamble(0x49e4, 8);

			int vertLoc = 0x20;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff || this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				if (vertLoc >= 0xc0)
				{
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();

					RenderReportPreamble(0x49f0, 8);

					vertLoc = 0x20;
				}

				this.owner.CityOps.HandleCity(townIdx, -1);

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Conquest.ObtainCityLabel(townIdx);
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, vertLoc, 15);

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Towns[townIdx].TrueExtent, 10));
				this.owner.Libc.AppendText(0xba06, "-");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70da), 10));
				this.owner.Libc.AppendText(0xba06, "{ ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70dc), 10));
				this.owner.Libc.AppendText(0xba06, "| ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70de), 10));
				this.owner.Libc.AppendText(0xba06, "}");
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 80, vertLoc, 15);

				sbyte making = this.owner.World.Towns[townIdx].PresentMakingIdx;

				if (making < 0)
				{
					// A building: names of 0x1e bytes at 0xb9a, costs at 0xbb2
					this.owner.Libc.CopyText(0xba06, (ushort)((-making * 0x1e) + 0xb9a));
					this.owner.BlockText.ShrinkTextToWidth(0xba06, 96);
					AppendProductionCost(townIdx, (short)(10 * this.processor.PullGlobalWord((ushort)((-making * 0x1e) + 0xbb2))));
				}
				else if (making < 0x40)
				{
					ushort record = (ushort)((making * UnitKindTable.Stride) + UnitKindTable.Origin);
					this.owner.Libc.CopyText(0xba06, record);
					AppendProductionCost(townIdx, (short)(10 * this.processor.PullGlobalWord((ushort)(record + UnitKindTable.Price))));
				}

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 172, vertLoc, 15);

				vertLoc += 8;
			}

			FinishReportScreen();

			this.processor.Journal.CloseScope("DisplayCityStatusReport");
		}

		/// <summary>Appends " (shields/cost)" of the city's production to the text at 0xba06 (0x87 is the shield)</summary>
		private void AppendProductionCost(short townIdx, short cost)
		{
			this.owner.Libc.AppendText(0xba06, "\x0087 (");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Towns[townIdx].MaterialsTally, 10));
			this.owner.Libc.AppendText(0xba06, "/");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(cost, 10));
			this.owner.Libc.AppendText(0xba06, ")");
		}
	}
}
