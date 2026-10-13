using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class HallOfFame
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public HallOfFame(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Empties the 6 entries of the Hall of Fame (records of 0x2e bytes at 0x3772:37d2, the first word -1)
		/// </summary>
		public void WipeHallOfFame()
		{
			this.processor.Journal.OpenScope("WipeHallOfFame()");

			this.processor.SegExtra.Value16 = 0x3772;

			for (int entryIdx = 0; entryIdx < 6; entryIdx++)
			{
				this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)((0x2e * entryIdx) + 0x37d2), 0xffff);
			}

			this.processor.Journal.CloseScope("WipeHallOfFame");
		}

		/// <summary>
		/// Wipes the Hall of Fame and reads it from fame.dta (0x114 bytes, through the buffer 0xba06) into 0x3772:0x37d2
		/// </summary>
		public void ImportHallOfFame()
		{
			this.processor.Journal.OpenScope("ImportHallOfFame()");

			WipeHallOfFame();

			short handle = this.owner.Libc.OpenHandle("fame.dta", 0x8102, 0x80);

			this.owner.Libc.ReadHandle(handle, 0xba06, 0x114);

			this.owner.Libc.MoveFarData(this.processor.SegData.Value16, 0xba06, 0x3772, 0x37d2, 0x114);

			this.owner.Libc.CloseHandle(handle);

			this.processor.Journal.CloseScope("ImportHallOfFame");
		}

		/// <summary>
		/// Writes the Hall of Fame (0x114 bytes at 0x3772:0x37d2, through the buffer 0xba06) to fame.dta
		/// </summary>
		public void StoreHallOfFame()
		{
			this.processor.Journal.OpenScope("StoreHallOfFame()");

			short handle = this.owner.Libc.OpenHandle("fame.dta", 0x8302, 0x80);

			this.owner.Libc.MoveFarData(0x3772, 0x37d2, this.processor.SegData.Value16, 0xba06, 0x114);

			this.owner.Libc.WriteHandle(handle, 0xba06, 0x114);

			this.owner.Libc.CloseHandle(handle);

			this.processor.Journal.CloseScope("StoreHallOfFame");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort DisplayHallOfFame(ushort arg1)
		{
			this.processor.Journal.OpenScope($"DisplayHallOfFame({arg1})");

			// function body
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveIndexRegisters();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x33be, 160, 16, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x33cb, 160, 24, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 80, 32, 240, 32, 14);

			this.processor.EmitLocalWord(0x6, 0x28);
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L0160;

		L0151:
			this.processor.CompareWord(arg1, 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L01d1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L01d1;

		L015d:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L0160:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0174;
			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0151;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L0151;

		L0174:
			this.processor.CompareWord(arg1, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L01ad;

			this.owner.TextPainter.RenderTextToScreen0(0x346a, 252, 189, 0);

			this.owner.Dialogs.RenderBox(236, 187, 60, 10, 11);

		L01ad:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(arg1, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L01be;
			goto L050b;

		L01be:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x63);
			if (this.processor.FlagSet.IsEqual) goto L01cb;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x43);
			if (this.processor.FlagSet.IsEqual) goto L01cb;
			goto L050b;

		L01cb:
			this.processor.RegAcc.Value16 = 0x1;
			goto L050d;

		L01d1:
			this.processor.EmitLocalWord(0x4, 0x8);
			this.processor.ScaleIndexToBase(0x2e, this.processor.PullLocalWord(0x8));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x37d2));
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1c), this.processor.RegSource.Value16);
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0220;

			this.owner.Libc.CopyText(0xba06, " ---");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, this.processor.PullLocalSignedWord(0x6), 0);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x1a));
			goto L015d;

		L0220:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullLocalWord(0x8) + 1), 10));

			this.owner.Libc.AppendText(0xba06, ". ");

			this.processor.ScaleIndexToSource(0x2e, this.processor.PullLocalWord(0x8));

			this.owner.Libc.MoveFarData(
				0x3772,
				(ushort)(this.processor.RegSource.Value16 + 0x37e0),
				this.processor.SegData.Value16,
				(ushort)(0xba06 + this.owner.Libc.TextLength(0xba06)),
				16);

			this.owner.Libc.AppendText(0xba06, ", ");

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d4));
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x33a2)));

			this.owner.Libc.AppendText(0xba06, " of the ");

			this.owner.Libc.MoveFarData(
				0x3772,
				(ushort)(this.processor.RegSource.Value16 + 0x37f0),
				this.processor.SegData.Value16,
				(ushort)(0xba06 + this.owner.Libc.TextLength(0xba06)),
				16);

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " to ");

			this.processor.SegExtra.Value16 = 0x3772; // segment
										
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(Math.Abs((short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d8))), 10));

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d8)), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0343;
			this.processor.RegAcc.Value16 = 0x33f1;
			goto L0346;

		L0343:
			this.processor.RegAcc.Value16 = 0x33f6;

		L0346:
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.ObtainTextWidth(0xba06);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x140);
			if (this.processor.FlagSet.IsLess) goto L036f;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

		L036f:
			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6),
				0);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));

			this.owner.Libc.CopyText(0xba06, "Population: ");

			this.processor.ScaleIndexToSource(0x2e, this.processor.PullLocalWord(0x8));
			this.processor.SegExtra.Value16 = 0x3772; // segment
			
			this.owner.Helpers.PopulationToText(this.processor.PullInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37de)));

			this.owner.Libc.AppendText(0xba06, ", Score: ");

			this.processor.SegExtra.Value16 = 0x3772; // segment

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d2)), 10));

			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x37d4));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0416;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L03fc;
			goto L04f3;

		L03fc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0404;
			goto L04f9;

		L0404:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L040c;
			goto L04ff;

		L040c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0414;
			goto L0505;

		L0414:
			goto L0426;

		L0416:
			this.processor.RegAcc.Value16 = 0x3412;

		L0419:
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L0426:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xc);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, (short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x6),
				7);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));

			this.owner.Libc.CopyText(0xba06, "    --- CIVILIZATION RATING: ");

			this.processor.ScaleIndex(0x2e, this.processor.PullLocalWord(0x8));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((((short)this.processor.PullUInt16(0x3772, 
					(ushort)(this.processor.RegDest.Value16 + 0x37d4)) + 1) *
					(short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegDest.Value16 + 0x37d2))) / 0x32), 10));

			this.owner.Libc.AppendText(0xba06, "% ---");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, (short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x6),
				12);

			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x8));
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), 0x2));
			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L04c6;
			goto L015d;

		L04c6:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x4) - 4,
				this.processor.PullLocalSignedWord(0x6) - 28,
				296, 26, 15, 14);

			goto L015d;

		L04f3:
			this.processor.RegAcc.Value16 = 0x341e;
			goto L0419;

		L04f9:
			this.processor.RegAcc.Value16 = 0x3429;
			goto L0419;

		L04ff:
			this.processor.RegAcc.Value16 = 0x3433;
			goto L0419;

		L0505:
			this.processor.RegAcc.Value16 = 0x343b;
			goto L0419;

		L050b:
			this.processor.RegAcc.Value16 = 0;

		L050d:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("DisplayHallOfFame");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Puts the human player's game into the Hall of Fame (6 records of 0x2e bytes at 0x3772:0x37d2, ordered by score 0xb77e
		/// times difficulty + 2; a game not better than the first 5 takes the last place), moving the worse records down, and
		/// shows it. When DisplayHallOfFame returns non-zero the Hall of Fame is wiped and the game put in again.
		/// </summary>
		/// <returns>The place of the game (also in AX)</returns>
		public ushort AttachHallOfFameRecord()
		{
			this.processor.Journal.OpenScope("AttachHallOfFameRecord()");

			ushort place = 0xffff;

			for (int slotIdx = 0; slotIdx < 6; slotIdx++)
			{
				ushort record = (ushort)((slotIdx * 0x2e) + 0x37d2);
				short rating = (short)((this.processor.PullUInt16(0x3772, (ushort)(record + 2)) + 2) * this.processor.PullUInt16(0x3772, record));
				short gameRating = (short)((this.owner.World.HardnessTier + 2) * this.processor.PullGlobalWord(0xb77e));

				if (rating >= gameRating && slotIdx != 5)
					continue;

				for (int laterIdx = 5; laterIdx > slotIdx; laterIdx--)
				{
					this.owner.Libc.MoveFarData(0x3772, (ushort)((laterIdx * 0x2e) + 0x37a4), 0x3772, (ushort)((laterIdx * 0x2e) + 0x37d2), 0x2e);
				}

				short humanIdx = this.owner.World.PersonParticipantIdx;

				this.processor.EmitUInt16(0x3772, record, this.processor.PullGlobalWord(0xb77e));
				this.owner.Libc.MoveFarData(this.processor.SegData.Value16, this.processor.PullGlobalWord((ushort)((humanIdx << 1) + 0x19a2)),
					0x3772, (ushort)(record + 0xe), 0xe);
				this.owner.Libc.MoveFarData(this.processor.SegData.Value16, this.processor.PullGlobalWord((ushort)((humanIdx << 1) + 0x1992)),
					0x3772, (ushort)(record + 0x1e), 0x10);
				this.processor.EmitUInt16(0x3772, (ushort)(record + 2), (ushort)this.owner.World.HardnessTier);
				this.processor.EmitUInt16(0x3772, (ushort)(record + 4), this.processor.PullGlobalWord(0xdc6a));
				this.processor.EmitUInt16(0x3772, (ushort)(record + 8), this.processor.PullGlobalWord(0xd764));
				this.processor.EmitUInt16(0x3772, (ushort)(record + 0xa), this.processor.PullGlobalWord(0xd766));
				this.processor.EmitInt16(0x3772, (ushort)(record + 6), this.owner.World.Season);

				ushort population = this.owner.Helpers.ObtainPlayerPopulation(humanIdx);

				this.processor.SegExtra.Value16 = 0x3772;
				this.processor.EmitUInt16(0x3772, (ushort)(record + 0xc), population);

				if (DisplayHallOfFame((ushort)slotIdx) == 0)
				{
					place = (ushort)slotIdx;
					break;
				}

				WipeHallOfFame();

				slotIdx = -1;
			}

			this.processor.RegAcc.Value16 = place;
			this.processor.Journal.CloseScope("AttachHallOfFameRecord");

			return place;
		}

		/// <summary>
		/// The ranking of the ruler among the 20 titles of 0x36d4 (0x19 bytes each, from the last): rank arg1, or with a score
		/// percentage arg2 * (difficulty + 1) / 50 the highest k - 1 (k = 1 - 20) with k * k / 3 below it ("...% exceeds even");
		/// nothing for a negative rank. The titles up to the rank are listed upwards, the rank highlighted, until a key or a click.
		/// </summary>
		public void DisplayGameResultsScreen(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayGameResultsScreen({arg1}, {arg2})");

			short rank = (short)arg1;
			short percentage = 0;

			if (arg2 != 0)
			{
				percentage = CPU.SignedQuotient((short)(arg2 * (this.owner.World.HardnessTier + 1)), 0x32);
				rank = -1;

				for (short level = 1; level <= 0x14; level++)
				{
					if (CPU.SignedQuotient((short)(level * level), 3) < percentage)
					{
						rank = (short)(level - 1);
					}
				}
			}

			if (rank >= 0)
			{
				if (rank > 0x13)
				{
					rank = 0x13;
				}

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

				this.owner.Services.PerformSound(0x22, 0);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

				this.owner.UnitOps.RenderBusyIndicator();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), 0xc8, 0x8c, 0x28, 0x3c, this.processor.PullGlobalWord(0xaa), 10, 10);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), 0xf0, 0x8c, 0x28, 0x3c, this.processor.PullGlobalWord(0xaa), 0x10e, 10);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 7);

				this.owner.Helpers.FloodBoxWithMotif(80, 4, 160, 27);

				this.owner.Dialogs.RenderShadowBox(80, 4, 159, 26, 11, 8);

				ushort ending;

				if (arg2 == 0)
				{
					PullRankTitle(0x1f4);
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 6, 15);

					PullRankTitle(0x20d);
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 14, 15);

					ending = 0x3471;
				}
				else
				{
					PullRankTitle(0x226);
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 6, 15);

					PullRankTitle(0x23f);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(percentage, 10));
					this.owner.Libc.AppendText(0xba06, "% exceeds even");
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 14, 15);

					ending = 0x3482;
				}

				PullRankTitle((ushort)((0x13 - rank) * 0x19));
				this.owner.Libc.AppendText(0xba06, ending);
				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 22, 15);

				for (short level = 0; level <= rank; level++)
				{
					PullRankTitle((ushort)((0x13 - level) * 0x19));
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, -((level * 8) - 184), (ushort)(level == rank ? 0 : 7));
				}

				this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 80, 183 - rank * 8, 160, 8, 15, 14);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.Services.PerformSound(1, 0);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 1);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayGameResultsScreen");
		}

		/// <summary>Copies the text (0x20 bytes) at 0x36d4:offset, the ranking titles, to 0xba06</summary>
		private void PullRankTitle(ushort offset)
		{
			this.owner.Libc.MoveFarData(0x36d4, offset, this.processor.SegData.Value16, 0xba06, 0x20);
		}

		/// <summary>
		/// The 5 top cities (after HandleCity of each: size + happy - unhappy citizens, + 10 per wonder), best first, each in a box
		/// of its player's colour with its citizens and wonders, until a key or a click. The list (scores at BP - 0x20, cities at
		/// BP - 0x10) stays in the stack frame like in the original, and RenderCitizenIcons, which reads what is left below the
		/// stack, is called at the same depth.
		/// </summary>
		public void DisplayTopCities()
		{
			this.processor.Journal.OpenScope("DisplayTopCities()");

			this.processor.EnterFrame(0x20);
			this.processor.PreserveIndexRegisters();

			ushort scores = (ushort)(this.processor.RegFrame.Value16 - 0x20);
			ushort towns = (ushort)(this.processor.RegFrame.Value16 - 0x10);

			for (int placeIdx = 0; placeIdx < 5; placeIdx++)
			{
				this.processor.EmitGlobalWord((ushort)(towns + (placeIdx << 1)), 0xffff);
				this.processor.EmitGlobalWord((ushort)(scores + (placeIdx << 1)), 0xffff);
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff)
					continue;

				this.owner.CityOps.HandleCity(townIdx, -1);

				short score = (short)(this.owner.World.Towns[townIdx].TrueExtent + this.processor.PullGlobalWord(0x70e2) - this.processor.PullGlobalWord(0x70e4));

				for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (this.owner.World.MarvelTownIdx[wonderIdx] == townIdx)
					{
						score += 10;
					}
				}

				for (int placeIdx = 0; placeIdx < 5; placeIdx++)
				{
					if ((short)this.processor.PullGlobalWord((ushort)(scores + (placeIdx << 1))) >= score)
						continue;

					for (int laterIdx = 3; laterIdx >= placeIdx; laterIdx--)
					{
						this.processor.EmitGlobalWord((ushort)(scores + ((laterIdx + 1) << 1)), this.processor.PullGlobalWord((ushort)(scores + (laterIdx << 1))));
						this.processor.EmitGlobalWord((ushort)(towns + ((laterIdx + 1) << 1)), this.processor.PullGlobalWord((ushort)(towns + (laterIdx << 1))));
					}

					this.processor.EmitGlobalWord((ushort)(scores + (placeIdx << 1)), (ushort)score);
					this.processor.EmitGlobalWord((ushort)(towns + (placeIdx << 1)), (ushort)townIdx);
					break;
				}
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 3);

			this.owner.TextPainter.RenderTextWithShadow(0x3486, 80, 12, 15);

			short vertLoc = 0x20;

			for (short placeIdx = 0; placeIdx < 5; placeIdx++)
			{
				if (this.processor.PullGlobalWord((ushort)(scores + (placeIdx << 1))) == 0xffff)
					continue;

				short townIdx = (short)this.processor.PullGlobalWord((ushort)(towns + (placeIdx << 1)));

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)(placeIdx + 1), 10));
				this.owner.Libc.AppendText(0xba06, ". ");
				this.owner.Conquest.ObtainCityLabel(townIdx);
				this.owner.Libc.AppendText(0xba06, " (");
				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((this.owner.World.Towns[townIdx].ParticipantIdx << 1) + 0x1982)));
				this.owner.Libc.AppendText(0xba06, ")");

				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, vertLoc + 3,
					this.processor.PullGlobalWord((ushort)((this.owner.World.Towns[townIdx].ParticipantIdx << 1) + 0x1946)));

				this.owner.CityOps.HandleCity(townIdx, -1);

				this.owner.CityOps.RenderCitizenIcons(townIdx, 48, (short)(vertLoc + 10), this.processor.PullGlobalWord(0xe8b8), 160);

				short iconHoriz = (short)(0x30 + this.owner.Helpers.VerifyValueSpan(this.owner.World.Towns[townIdx].TrueExtent * 8, 0, 160) + 8);

				for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (this.owner.World.MarvelTownIdx[wonderIdx] != townIdx)
						continue;

					if (iconHoriz < 0x125)
					{
						this.owner.CityOps.RenderResourceIcon((short)(wonderIdx + 24), iconHoriz, (short)(vertLoc + 12));
					}

					iconHoriz += 0x13;
				}

				this.owner.Dialogs.RenderBox(8, vertLoc, 303, 25,
					this.processor.PullGlobalWord((ushort)((this.owner.World.Towns[townIdx].ParticipantIdx << 1) + 0x1946)));

				vertLoc += 0x20;
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayTopCities");
		}
	}
}
