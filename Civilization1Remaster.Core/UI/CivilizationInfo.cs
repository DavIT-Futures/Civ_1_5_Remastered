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
		/// The table of units: a row for every unit type some player 1 - 7 has or builds, a column for every player 0 - 7 (its
		/// picture, and its standing with 0xd806); the human player's column (all columns with 0xd806) shows "active/in
		/// production" of each row. After a key or a click, the table of units destroyed.
		/// </summary>
		public void DisplayCivilizationScoreList()
		{
			this.processor.Journal.OpenScope("DisplayCivilizationScoreList()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Dialogs.FloodBoxWithDoubleShadow(0, 0, 317, 197, 1);

			int rowIdx = 0;
			short vertLoc = 0x12;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				if (!AnyPlayerHasUnits(kind))
					continue;

				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 36, vertLoc - 1, 319, vertLoc - 1, 9);

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					this.processor.PullGlobalWord((ushort)(((kind + 0x60) << 1) + 0xd4ce)));

				rowIdx++;

				this.owner.TextPainter.RenderTextToScreen0((ushort)((kind * 0x22) + 0x112a), 36, vertLoc, 15);

				vertLoc += 8;
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].Standing, 10));

				if (this.processor.PullGlobalWord(0xd806) != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(0xba06, (participantIdx * 30) + 84, 1, 15);
				}

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), (30 * participantIdx) + 90, 2,
					this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

				if (participantIdx != this.owner.World.PersonParticipantIdx && this.processor.PullGlobalWord(0xd806) == 0)
					continue;

				vertLoc = 0x12;

				for (int kind = 0; kind < 0x1c; kind++)
				{
					if (!AnyPlayerHasUnits(kind))
						continue;

					Player participant = this.owner.World.Participants[participantIdx];

					if (participant.LiveTroops[kind] != 0 || participant.TroopsInsideMaking[kind] != 0)
					{
						this.owner.Libc.CopyText(0xba06, "");
						this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(participant.LiveTroops[kind], 10));
						this.owner.Libc.AppendText(0xba06, "/");
						this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(participant.TroopsInsideMaking[kind], 10));
						this.owner.TextPainter.RenderTextToScreen0(0xba06, (participantIdx * 30) + 88, vertLoc, 15);
					}

					vertLoc += 8;
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			RenderScoreListRecord();

			this.processor.Journal.CloseScope("DisplayCivilizationScoreList");
		}

		/// <summary>One of the players 1 - 7 has or builds units of the type</summary>
		private bool AnyPlayerHasUnits(int kind)
		{
			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (this.owner.World.Participants[participantIdx].LiveTroops[kind] != 0 ||
					this.owner.World.Participants[participantIdx].TroopsInsideMaking[kind] != 0)
					return true;
			}

			return false;
		}

		/// <summary>
		/// The table of units destroyed between the players 1 - 7 (TroopsRuined of the row's player against the column's / the
		/// other way round), each player in its colour (0x1946) with its picture (0xd4ce); only the human's row and column
		/// unless all units are shown (0xd806). Until a key or click, with the screen kept in the back screen 0x19d4.
		/// </summary>
		public void RenderScoreListRecord()
		{
			this.processor.Journal.OpenScope("RenderScoreListRecord()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 4);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort name = this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982));
				ushort hue = this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946));

				this.owner.TextPainter.RenderTextWithShadow(name, participantIdx * 40, 8, hue);

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), (short)(0x28 * participantIdx), 16,
					this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

				this.owner.TextPainter.RenderTextWithShadow(name, 4, (participantIdx * 12) + 32, hue);
			}

			short personIdx = this.owner.World.PersonParticipantIdx;

			for (int rowIdx = 1; rowIdx < 8; rowIdx++)
			{
				for (int columnIdx = 1; columnIdx < 8; columnIdx++)
				{
					if (rowIdx == columnIdx)
						continue;

					if (this.processor.PullGlobalWord(0xd806) == 0 && rowIdx != personIdx && columnIdx != personIdx)
						continue;

					this.processor.EmitGlobalByte(0xba06, 0x0);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[rowIdx].TroopsRuined[columnIdx], 10));
					this.owner.Libc.AppendText(0xba06, "/");
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[columnIdx].TroopsRuined[rowIdx], 10));

					this.owner.TextPainter.RenderTextToScreen0(0xba06, rowIdx * 40, (columnIdx * 12) + 32,
						this.processor.PullGlobalWord((ushort)((rowIdx << 1) + 0x1946)));
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("RenderScoreListRecord");
		}
	}
}
