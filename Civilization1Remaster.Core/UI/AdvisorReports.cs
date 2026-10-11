using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using System;

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
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="mode"></param>
		public void RenderReportPreamble(ushort charsAddr, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderReportPreamble(0x{charsAddr:x4}, {style})");

			// function body
			this.processor.EnterFrame();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, style);

			this.owner.TextPainter.RenderCenteredTextToScreen0(charsAddr, 160, 2, 15);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.HalveAccumulator();
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0070;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0082;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0087;
			goto L008c;

		L0070:
			this.processor.RegAcc.Value16 = 0x473c;

		L0073:
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			goto L008c;

		L0082:
			this.processor.RegAcc.Value16 = 0x4743;
			goto L0073;

		L0087:
			this.processor.RegAcc.Value16 = 0x474b;
			goto L0073;

		L008c:
			this.owner.Libc.AppendText(0xba06, " of the ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 10, 15);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19a2)));

			this.owner.Libc.AppendText(0xba06, ": ");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 18, 15);

			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
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
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayScienceReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayScienceReport({participantIdx})");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveSource();

			DisplayReportPreamble(0x4762, 1);

			bool isHuman = (participantIdx == this.owner.World.PersonParticipantIdx);
			int totalCost = ScienceRules.ComputeTechnologyCost(
				isHuman,
				this.owner.World.HardnessTier,
				this.owner.World.Participants[participantIdx].FoundScienceTally,
				this.owner.World.Season < 0);

			this.processor.EmitLocalWord(0xc, (ushort)totalCost);

			this.owner.Helpers.VerifyValueSpan(
				2432 / (this.processor.PullLocalSignedWord(0xc) + 1),
				1, 64);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				((this.processor.PullLocalSignedWord(0x12) *
					this.processor.PullLocalSignedWord(0xc)) / 8) + 8,
				8, 304);

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa0);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x28);
			if (this.processor.FlagSet.IsLessOrEqual) goto L022e;
			
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0x118, 0x8c, 0x28, 0x3c,
				this.processor.PullGlobalWord(0xaa),
				0x116, 0x2);

		L022e:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4), 25,
				this.processor.PullLocalSignedWord(0x8), 16, 9);

			this.processor.EmitLocalWord(0x6, 0x0);
			goto L0281;

		L0253:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0x2) / 8) +
					this.processor.PullLocalSignedWord(0x4),
				33,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xc << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x6);

		L0281:
			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Participants[participantIdx].InquiryHeadway,
				0, this.processor.PullLocalSignedWord(0xc));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x6));
			if (this.processor.FlagSet.IsGreater) goto L0253;
			this.processor.EmitLocalWord(0xa, 0x1a);
			
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx == -1) goto L02ff;

			this.processor.RegAcc.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			string techTitle = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, this.processor.RegAcc.Value16));

			int currentProgress = this.owner.World.Participants[participantIdx].InquiryHeadway;
			int totalCost = this.processor.PullLocalSignedWord(0xc);
			string researchHeader = $"Researching {techTitle} ({currentProgress}/{totalCost})";

			this.owner.TextPainter.RenderCenteredTextToScreen0(researchHeader, 161, 26, 0);
			this.owner.TextPainter.RenderCenteredTextToScreen0(researchHeader, 160, 26, 15);

		L02ff:
			this.processor.EmitLocalWord(0xa, 0x2a);
			this.processor.EmitLocalWord(0x10, 0x0);
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L0355;

		L0310:
			this.processor.RegAcc.Value16 = 0xb;

		L0313:
			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				((this.processor.PullLocalSignedWord(0x10) % 3) * 100) + 8,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x10);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L034b;
			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), 0x7));

		L034b:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0xc0);
			if (this.processor.FlagSet.IsGreater) goto L03a4;

		L0352:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L0355:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L03a4;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0352;

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x6));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ScienceLeadingFoundVia[this.processor.PullLocalWord(0x6)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L039e;
			goto L0310;

		L039e:
			this.processor.RegAcc.Value16 = 0xf;
			goto L0313;

		L03a4:
			int requiredBeakers = this.processor.PullLocalSignedWord(0xc);
			int currentBeakers = this.owner.World.Participants[participantIdx].InquiryHeadway;

			int totalSciencePerTurn = 0;
			for (int cityIdx = 0; cityIdx < 128; cityIdx++)
			{
				if (this.owner.World.Towns[cityIdx].ConditionMarker != 0xff &&
					this.owner.World.Towns[cityIdx].ParticipantIdx == participantIdx)
				{
					this.owner.CityOps.HandleCity((short)cityIdx, -1);
					totalSciencePerTurn += (short)this.processor.PullGlobalWord(0x70e6);
				}
			}
			if (this.owner.CityOps.PlayerEnjoysWonderEffect((short)participantIdx, 0x14) != 0)
			{
				totalSciencePerTurn += totalSciencePerTurn / 2;
			}

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 8, 187, 312, 187, 8);
			string scienceSummary = (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx != -1)
				? AdvisorStatistics.FormatScienceSummary(currentBeakers, requiredBeakers, totalSciencePerTurn)
				: $"Science: +{totalSciencePerTurn}/turn (No research selected)";
			this.owner.TextPainter.RenderCenteredTextToScreen0(scienceSummary, 160, 190, 14);

			FinishReportScreen();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayScienceReport");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void RenderUnitCountsChart(short participantIdx)
		{
			this.processor.Journal.OpenScope($"RenderUnitCountsChart({participantIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();

			DisplayReportPreamble(0x477e, 1);

			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0x4, 0x20);
			this.processor.EmitLocalWord(0x2, 0x0);

		L03d3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, (ushort)participantIdx);

			if (this.owner.World.Participants[participantIdx].LiveTroops[this.processor.PullLocalWord(0x2)] > 0)
				goto L03f1;

			if (this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.processor.PullLocalWord(0x2)] > 0)
				goto L03f1;

			goto L05d7;

		L03f1:
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				36, this.processor.PullLocalSignedWord(0x4) - 2,
				319, this.processor.PullLocalSignedWord(0x4) - 2, 9);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				((this.processor.PullLocalWord(0x6) & 0x1) << 4) + 2,
				this.processor.PullLocalSignedWord(0x4) - 4,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((participantIdx << 5) +
					this.processor.PullLocalWord(0x2) + 0x40) << 1) + 0xd4ce)));

			this.processor.IncrementLocalWord(0x6);

			this.processor.ScaleIndexToSource(0x22, this.processor.PullLocalWord(0x2));

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(this.processor.RegSource.Value16 + 0x112a),
				36,
				this.processor.PullLocalSignedWord(0x4),
				15);

			this.owner.Libc.CopyText(0xba06, "(");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113e)), 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1140)), 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113a)), 10));

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 112, this.processor.PullLocalSignedWord(0x4), 11);

			this.processor.ScaleIndex(0x38, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].LiveTroops[this.processor.PullLocalWord(0x2)];
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0574;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.RegSource.Value16, 10));

			this.owner.Libc.AppendText(0xba06, " active");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 168, this.processor.PullLocalSignedWord(0x4), 15);

		L0574:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.processor.PullLocalWord(0x2)];

			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L05d3;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.RegSource.Value16, 10));

			this.owner.Libc.AppendText(0xba06, " in production");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 232, this.processor.PullLocalSignedWord(0x4), 11);

		L05d3:
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x9));

		L05d7:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x1c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05e3;
			goto L03d3;

		L05e3:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0xa0, 0x8c, 0x28, 0x3c,
				this.processor.PullGlobalWord(0xaa),
				0x116, 0x2);

			FinishReportScreen();

			RenderPlayerComparisonChart(participantIdx);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderUnitCountsChart");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void RenderPlayerComparisonChart(short participantIdx)
		{
			this.processor.Journal.OpenScope($"RenderPlayerComparisonChart({participantIdx})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.PreserveSource();

			DisplayReportPreamble(0x47ad, 4);

			this.processor.EmitLocalWord(0xc, 0x0);
			this.processor.EmitLocalWord(0x8, 0x1c);
			this.processor.EmitLocalWord(0x6, 0x0);

		L0644:
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0xa, 0x0);

		L064e:
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullLocalWord(0xa)].MissingTroops[this.processor.PullLocalWord(0x6)], 0x0);
			if (this.processor.FlagSet.IsEqual) goto L066e;
			this.processor.EmitLocalWord(0x2, 0x1);

		L066e:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x8);
			if (this.processor.FlagSet.IsLess) goto L064e;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0680;
			goto L07b7;

		L0680:
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				36, this.processor.PullLocalSignedWord(0x8) - 1,
				319, this.processor.PullLocalSignedWord(0x8) - 1, 8);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				((this.processor.PullLocalWord(0xc) & 1) << 4) + 2,
				this.processor.PullLocalSignedWord(0x8) - 4,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((participantIdx << 5) + this.processor.PullLocalWord(0x6) + 0x40) << 1) + 0xd4ce)));

			this.processor.IncrementLocalWord(0xc);

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)((this.processor.PullLocalWord(0x6) * 0x22) + 0x112a),
				36,
				this.processor.PullLocalSignedWord(0x8),
				15);

			this.processor.EmitLocalWord(0x4, 0x68);

			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[participantIdx].MissingTroops[this.processor.PullLocalWord(0x6)];
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L073f;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06, 
				this.owner.Libc.IntegerToText((short)this.processor.RegSource.Value16, 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x8), 12);

		L073f:
			this.processor.EmitLocalWord(0xa, 0x1);

		L0744:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L07aa;
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x1c));

			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0xa)].MissingTroops[this.processor.PullLocalWord(0x6)];
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L07aa;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.RegSource.Value16, 10));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(
				0xba06,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x8),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

		L07aa:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0744;
			this.processor.EmitLocalWord(0x8, this.processor.SumWord(this.processor.PullLocalWord(0x8), 0x7));

		L07b7:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x1b);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L07c3;
			goto L0644;

		L07c3:
			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Dialogs.RenderBox(100, 25,
				18, this.processor.PullLocalSignedWord(0x8) - 25,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			FinishReportScreen();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
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

			int totalBaseTrade = 0;
			int totalLuxuries = 0;
			int totalFood = 0;
			int totalProduction = 0;

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

			totalBaseTrade += (short)this.processor.PullGlobalWord(0x70de);
			totalLuxuries += (short)this.processor.PullGlobalWord(0x70e0);
			totalFood += (short)this.processor.PullGlobalWord(0x70da);
			totalProduction += (short)this.processor.PullGlobalWord(0x70dc);

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

			int tradeInfoY = this.processor.PullLocalSignedWord(0x36) + 12;
			if (tradeInfoY + 16 <= 192)
			{
				string tradeTotalsText = AdvisorStatistics.FormatTradeTotals(
					totalBaseTrade,
					(short)this.processor.PullLocalWord(0x2),
					(short)this.processor.PullLocalWord(0x4),
					totalLuxuries);
				this.owner.TextPainter.RenderTextToScreen0(tradeTotalsText, 8, tradeInfoY + 8, 14);

				string tradeYieldsText = AdvisorStatistics.FormatTradeYields(
					totalFood,
					totalProduction,
					totalBaseTrade);
				this.owner.TextPainter.RenderTextToScreen0(tradeYieldsText, 8, tradeInfoY + 16, 14);
			}

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
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayIntelligenceReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayIntelligenceReport({participantIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveSource();

			DisplayReportPreamble(0x48a5, 1);

			this.processor.EmitLocalWord(0x4, 0x20);

			this.owner.Libc.CopyText(0xba06, "Subject: the ");

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, 32, 15);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0xc));

			this.owner.Libc.CopyText(0xba06, "Leader:\x008f ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x19a2)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1512)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L124c;

			this.owner.TextPainter.RenderTextToScreen0(0x48d3, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L124c:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1512)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L127c;

			this.owner.TextPainter.RenderTextToScreen0(0x48de, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L127c:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1514)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L12ac;

			this.owner.TextPainter.RenderTextToScreen0(0x48e7, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L12ac:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1514)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L12dc;

			this.owner.TextPainter.RenderTextToScreen0(0x48f6, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L12dc:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L130c;

			this.owner.TextPainter.RenderTextToScreen0(0x4904, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L130c:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516)), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L133c;

			this.owner.TextPainter.RenderTextToScreen0(0x490e, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L133c:
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x4));
			this.processor.EmitLocalWord(0x2, 0xffff);
			this.processor.EmitLocalWord(0x6, 0x0);

		L134a:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x6));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x6)].ParticipantIdx != participantIdx) goto L1370;
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x6)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1370;
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x6)].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1370;
			this.processor.CopyLocalWord(0x6, 0x2);

		L1370:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x80);
			if (this.processor.FlagSet.IsLess) goto L134a;

			this.owner.Libc.CopyText(0xba06, "Capital:\x008f ");

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x2));
			
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

			this.owner.Libc.CopyText(0xba06, "Government:\x008f ");

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1966)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

			this.owner.Libc.CopyText(0xba06, "Treasury:\x008f ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].Gold, 10));

			this.owner.Libc.AppendText(0xba06, "$");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));
			
			if (this.owner.World.HardnessTier > 1) goto L14b5;

			this.owner.Libc.CopyText(0xba06, "Military:\x008f ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].TroopTally, 10));

			this.owner.Libc.AppendText(0xba06, " Units.");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0xc));

		L14b5:
			this.owner.TextPainter.RenderTextToScreen0(0x4956, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));
			this.processor.EmitLocalWord(0x6, 0x1);

		L14d5:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x6)] & 2) != 0)
			{
				this.owner.Libc.CopyText(0xba06, "At\x008f PEACE");
			}
		
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x6)] & 3) == 1)
			{
				this.owner.Libc.CopyText(0xba06, "At\x008c WAR");
			}
		
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x6)] & 1) == 0)
				goto L157c;

			this.owner.Libc.AppendText(0xba06, "\x0087 with the ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ".");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 24, this.processor.PullLocalSignedWord(0x4), 7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));

		L157c:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1588;
			goto L14d5;

		L1588:
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x4));

			this.owner.TextPainter.RenderTextToScreen0(0x4987, 16, this.processor.PullLocalSignedWord(0x4), 9);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));
			this.processor.EmitLocalWord(0x6, 0x47);

		L15ac:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L15e6;

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)((this.processor.PullLocalWord(0x6) * 0x16) + 0x4da),
				24,
				this.processor.PullLocalSignedWord(0x4),
				7);

			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x8));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xc0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L15eb;

		L15e6:
			this.processor.EmitLocalWord(0x6, this.processor.DecrementWord(this.processor.PullLocalWord(0x6)));
			if (this.processor.FlagSet.IsUnsigned) goto L15ac;

		L15eb:
			FinishReportScreen();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayIntelligenceReport");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayAttitudeReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayAttitudeReport({participantIdx})");

			// function body
			this.processor.EnterFrame(0x14);
			this.processor.PreserveSource();

			DisplayReportPreamble(0x4995, 9);
			
			this.processor.EmitLocalWord(0xc, 0x20);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

		L1620:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xa));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L1632;
			goto L175a;

		L1632:
			if (this.owner.World.Towns[this.processor.PullLocalWord(0xa)].ParticipantIdx != participantIdx)
				goto L175a;

			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xbe);
			if (this.processor.FlagSet.IsLessOrEqual) goto L165f;

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderReportPreamble(0x49a5, 9);

			this.processor.EmitLocalWord(0xc, 0x20);

		L165f:
			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0xa), -1);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0xa));

			this.owner.Libc.AppendText(0xba06, ":");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0xc), 15);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x50);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.owner.CityOps.RenderCitizenIcons(this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0x8),
				(short)(this.processor.PullLocalSignedWord(0xc) - 3),
				this.processor.PullGlobalWord(0xe8b8), 124);

			this.processor.EmitLocalWord(0x8, 0xd4);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				212, this.processor.PullLocalSignedWord(0xc) - 1, 90, 10, 11);

			this.processor.EmitLocalWord(0xe, 0x0);

		L16f9:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.Value16 = this.owner.World.Towns[this.processor.PullLocalWord(0xa)].StructureMarkers0;
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, 0x2618);
			if (this.processor.FlagSet.IsEqual) goto L172d;

			this.owner.CityOps.RenderResourceIcon((short)(this.processor.PullLocalSignedWord(0xe) + 1),
				this.processor.PullLocalSignedWord(0x8),
				(short)(this.processor.PullLocalSignedWord(0xc) - 1));

			this.processor.EmitLocalWord(0x8, this.processor.SumWord(this.processor.PullLocalWord(0x8), 0x12));

		L172d:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x18);
			if (this.processor.FlagSet.IsLess) goto L16f9;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xa));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0xa)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0x4));
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0xa));

		L175a:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1767;
			goto L1620;

		L1767:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1770;
			this.processor.IncrementLocalWord(0x6);

		L1770:
			this.owner.Libc.CopyText(0xba06, "Population: ");

			this.owner.Helpers.ObtainPlayerPopulation(participantIdx);

			this.owner.Libc.AppendText(0xba06, " Happy:");

			AffixProductionCount(
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6));

			this.owner.Libc.AppendText(0xba06, "% Content:");

			AffixProductionCount(
				(short)(this.processor.PullLocalSignedWord(0x6) -
					this.processor.PullLocalSignedWord(0x4) -
					this.processor.PullLocalSignedWord(0x2)),
				this.processor.PullLocalSignedWord(0x6));

			this.owner.Libc.AppendText(0xba06, "% Unhappy:");

			AffixProductionCount(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x6));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 
				16,
				this.processor.PullLocalSignedWord(0xc) + 8,
				15);

			FinishReportScreen();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
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
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayCityStatusReport(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCityStatusReport({participantIdx})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveSource();

			DisplayReportPreamble(0x49e4, 8);

			int totalCities = 0;
			int totalPopulation = 0;
			int totalFood = 0;
			int totalProduction = 0;
			int totalTrade = 0;

			this.processor.EmitLocalWord(0x6, 0x20);
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L1968;

		L1892:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 96);

			this.owner.Libc.AppendText(0xba06, "\x0087 (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Towns[this.processor.PullLocalWord(0x4)].MaterialsTally, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)0xa * (short)this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((-((short)this.owner.World.Towns[
						this.processor.PullLocalWord(0x4)].PresentMakingIdx * 
					0x1e)) + 0xbb2) & 0xffff))),
						10));

			this.owner.Libc.AppendText(0xba06, ")");

		L194a:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 172, this.processor.PullLocalSignedWord(0x6), 15);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));

		L1965:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L1968:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x80);
			if (this.processor.FlagSet.IsLess) goto L1972;
			goto L1b69;

		L1972:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1965;
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4)].ParticipantIdx != participantIdx)
				goto L1965;

			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xc0);
			if (this.processor.FlagSet.IsLess) goto L19ab;

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderReportPreamble(0x49f0, 8);
			
			this.processor.EmitLocalWord(0x6, 0x20);

		L19ab:
			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x4), -1);

			totalCities++;
			totalPopulation += this.owner.World.Towns[this.processor.PullLocalWord(0x4)].TrueExtent;
			totalFood += (short)this.processor.PullGlobalWord(0x70da);
			totalProduction += (short)this.processor.PullGlobalWord(0x70dc);
			totalTrade += (short)this.processor.PullGlobalWord(0x70de);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x4));
			
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, this.processor.PullLocalSignedWord(0x6), 15);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4));

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Towns[this.processor.PullLocalWord(0x4)].TrueExtent, 10));

			this.owner.Libc.AppendText(0xba06, "-");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70da), 10));

			this.owner.Libc.AppendText(0xba06, "{ ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70dc), 10));

			this.owner.Libc.AppendText(0xba06, "| ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0x70de), 10));

			this.owner.Libc.AppendText(0xba06, "}");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 80, this.processor.PullLocalSignedWord(0x6), 15);

			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1ad5;
			goto L1892;

		L1ad5:
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].PresentMakingIdx, 0x40);
			if (this.processor.FlagSet.IsLess) goto L1adf;
			goto L194a;

		L1adf:
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4)].PresentMakingIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\x0087 (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Towns[this.processor.PullLocalWord(0x4)].MaterialsTally, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(0xa * this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(0x22 * this.owner.World.Towns[this.processor.PullLocalWord(0x4)].PresentMakingIdx + 0x1142))), 10));

			this.owner.Libc.AppendText(0xba06, ")");

			goto L194a;

		L1b69:
			if (totalCities > 0)
			{
				int lastY = this.processor.PullLocalSignedWord(0x6);
				int sepY = Math.Min(Math.Max(lastY + 3, 182), 187);
				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 8, sepY, 312, sepY, 8);
				string summaryText = AdvisorStatistics.FormatCityStatusSummary(totalCities, totalPopulation, totalFood, totalProduction, totalTrade);
				this.owner.TextPainter.RenderTextToScreen0(summaryText, 8, sepY + 3, 14);
			}

			FinishReportScreen();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCityStatusReport");
		}
	}
}
