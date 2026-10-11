using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 13.
	/// </summary>
	public class CivilizationInfo
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public CivilizationInfo(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayCivilizationOverview(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationOverview({participantIdx})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 1);

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)),
				128, 4, 15);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				16, 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((((participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

			this.owner.Libc.CopyText(0xba06, "Government: ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[participantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1966)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, 24, 15);

			this.owner.Libc.CopyText(0xba06, "Population: ");

			this.owner.Helpers.ObtainPlayerPopulation(participantIdx);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, 32, 15);

			this.owner.Libc.CopyText(0xba06, "Treasury: $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].Gold, 10));

			this.owner.Libc.AppendText(0xba06, ", Taxes/Science: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].LevyRatio, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].ResearchLevyRatio, 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, 40, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x46be, 16, 50, 15);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].TroopTally, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].TownTally, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].ColonistTally, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].SoilTally, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 192, 48, 15);

			this.processor.EmitLocalWord(0x2, 0x4);
			this.processor.EmitLocalWord(0x4, 0x1);

		L029f:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4)].Assault == 0)
				goto L0379;

			this.owner.Libc.CopyText(0xba06, "Settle");

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4)].Approach == 2)
			{
				this.owner.Libc.CopyText(0xba06, "Defend");
			}

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4)].Approach == 1)
			{
				this.owner.Libc.CopyText(0xba06, "Attack");
			}

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x4)].Approach == 5)
			{
				this.owner.Libc.CopyText(0xba06, "Transport");
			}
		
			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x4), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 256, this.processor.PullLocalSignedWord(0x2), 7);

			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), 0x8));

		L0379:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0385;
			goto L029f;

		L0385:
			this.processor.EmitLocalWord(0x2, 0x18);
			this.processor.EmitLocalWord(0x4, 0x1);
			goto L03f5;

		L0391:
			this.owner.Libc.AppendText(0xba06, ": Peace");

		L03a1:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x4)] & 8) != 0)
			{
				this.owner.Libc.AppendText(0xba06, ": Vendetta");
			}

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x4)] & 0xb) == 0)
				goto L03f2;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 192, this.processor.PullLocalSignedWord(0x2), 7);

			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), 0x8));

		L03f2:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L03f5:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0464;
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L03f2;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x4)] & 0xb) == 0x1)
			{
				this.owner.Libc.AppendText(0xba06, ": Contact");
			}
		
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x4)] & 2) == 0)
				goto L03a1;

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x4)] & 4) == 0)
				goto L0391;

			this.owner.Libc.AppendText(0xba06, ": Allied");

			goto L03a1;

		L0464:
			this.processor.EmitLocalWord(0x2, 0x38);
			this.processor.EmitLocalWord(0xa, 0x0);

			this.owner.TextPainter.RenderTextToScreen0(0x4729, 16, 56, 15);

			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), 0x8));
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L04d6;

		L0491:
			this.processor.RegAcc.Value16 = 0x7;

		L0494:
			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				((this.processor.PullLocalSignedWord(0xa) % 3) * 100) + 8,
				this.processor.PullLocalSignedWord(0x2),
				this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0xa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L04cc;
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), 0x8));

		L04cc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xc0);
			if (this.processor.FlagSet.IsGreater) goto L0525;

		L04d3:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L04d6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0525;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x4));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L04d3;

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ScienceLeadingFoundVia[this.processor.PullLocalWord(0x4)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)participantIdx);
			if (this.processor.FlagSet.IsEqual) goto L051f;
			goto L0491;

		L051f:
			this.processor.RegAcc.Value16 = 0xf;
			goto L0494;

		L0525:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilizationOverview");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayCivilizationScoreList()
		{
			this.processor.Journal.OpenScope("DisplayCivilizationScoreList()");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Dialogs.FloodBoxWithDoubleShadow(0, 0, 317, 197, 1);

			this.processor.EmitLocalWord(0xe, 0x0);
			this.processor.EmitLocalWord(0xa, 0x12);
			this.processor.EmitLocalWord(0xc, 0x0);

		L05a7:
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x4, 0x1);

		L05b1:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, this.processor.PullLocalWord(0x4));

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x4)].LiveTroops[this.processor.PullLocalWord(0xc)] != 0)
				goto L05cc;
			
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x4)].TroopsInsideMaking[this.processor.PullLocalWord(0xc)] == 0)
				goto L05d1;

		L05cc:
			this.processor.EmitLocalWord(0x2, 0x1);

		L05d1:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L05b1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0641;

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				36, this.processor.PullLocalSignedWord(0xa) - 1,
				319, this.processor.PullLocalSignedWord(0xa) - 1, 9);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				((this.processor.PullLocalWord(0xe) & 1) << 4) + 2,
				this.processor.PullLocalSignedWord(0xa) - 4,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0xc) + 0x60) << 1) + 0xd4ce)));

			this.processor.IncrementLocalWord(0xe);

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)((this.processor.PullLocalWord(0xc) * 0x22) + 0x112a),
				36, 
				this.processor.PullLocalSignedWord(0xa), 
				15);

			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), 0x8));

		L0641:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x1c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L064d;
			goto L05a7;

		L064d:
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L0657;

		L0654:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0657:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x5);
			if (this.processor.FlagSet.IsLess) goto L0654;
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L075e;

		L0665:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0668:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L0671;
			goto L075b;

		L0671:
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x6, 0x1);

		L067b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, this.processor.PullLocalWord(0x6));
			
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x6)].LiveTroops[this.processor.PullLocalWord(0xc)] != 0)
				goto L0696;

			if (this.owner.World.Participants[this.processor.PullLocalWord(0x6)].TroopsInsideMaking[this.processor.PullLocalWord(0xc)] == 0)
				goto L069b;

		L0696:
			this.processor.EmitLocalWord(0x2, 0x1);

		L069b:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L067b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0665;
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, this.processor.PullLocalWord(0x4));
			
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x4)].LiveTroops[this.processor.PullLocalWord(0xc)] != 0)
				goto L06c8;
			
			if (this.owner.World.Participants[this.processor.PullLocalWord(0x4)].TroopsInsideMaking[this.processor.PullLocalWord(0xc)] != 0)
				goto L06c8;

			goto L0754;

		L06c8:
			this.owner.Libc.CopyText(0xba06, "");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, this.processor.PullLocalWord(0x4));

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x4)].LiveTroops[this.processor.PullLocalWord(0xc)], 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x4)].TroopsInsideMaking[this.processor.PullLocalWord(0xc)], 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				(this.processor.PullLocalSignedWord(0x4) * 30) + 88,
				this.processor.PullLocalSignedWord(0xa),
				15);

		L0754:
			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), 0x8));
			goto L0665;

		L075b:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L075e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0767;
			goto L07f7;

		L0767:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullLocalWord(0x4)].Standing, 10));

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L07b7;

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				(this.processor.PullLocalSignedWord(0x4) * 30) + 84,
				1,
				15);

		L07b7:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(30 * this.processor.PullLocalSignedWord(0x4)) + 90, 2,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((this.processor.PullLocalWord(0x4) << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L07ea;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07ea;
			goto L075b;

		L07ea:
			this.processor.EmitLocalWord(0xa, 0x12);
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L0668;

		L07f7:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			RenderScoreListRecord();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilizationScoreList");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RenderScoreListRecord()
		{
			this.processor.Journal.OpenScope("RenderScoreListRecord()");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 4);

			this.processor.EmitLocalWord(0x2, 0x1);

		L085d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x28, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.TextPainter.RenderTextWithShadow(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)),
				this.processor.PullLocalSignedWord(0x2) * 40,
				8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)this.processor.RegDest.Value16, 16,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((this.processor.PullLocalWord(0x2) << 5) + 0x40) << 1) + 0xd4ce)));

			this.owner.TextPainter.RenderTextWithShadow(
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)),
				4,
				this.processor.PullLocalSignedWord(0x2) * 12 + 32,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L085d;
			this.processor.EmitLocalWord(0x2, 0x1);
			goto L0984;

		L08c6:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L08c9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x8);
			if (this.processor.FlagSet.IsLess) goto L08d2;
			goto L0981;

		L08d2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L08c6;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L08ee;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L08ee;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L08c6;

		L08ee:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(
					this.owner.World.Participants[this.processor.PullLocalWord(0x2)].TroopsRuined[this.processor.PullLocalWord(0x6)],
					10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x2) << 1);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(
					this.owner.World.Participants[this.processor.PullLocalWord(0x6)].TroopsRuined[this.processor.PullLocalWord(0x2)],
					10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0x2) * 40,
				(this.processor.PullLocalSignedWord(0x6) * 12) + 32,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1946)));

			goto L08c6;

		L0981:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0984:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0992;
			this.processor.EmitLocalWord(0x6, 0x1);
			goto L08c9;

		L0992:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderScoreListRecord");
		}
	}
}
