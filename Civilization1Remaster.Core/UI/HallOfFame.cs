using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class HallOfFame : GameModule
	{
		public HallOfFame(GameEngine owner)
			: base(owner)
		{
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
		/// The Hall of Fame (records of 0x2e bytes at 0x3772:0x37d2): places 1 - 5 (the 6th instead of the 5th when arg1 is 5),
		/// each with the leader, title, civilization, years, population, score and rating; the place arg1 is highlighted, and
		/// unless arg1 is -1 "C" may be pressed (the hint at the bottom).
		/// </summary>
		/// <returns>1 after the key C (c), else 0, also in AX</returns>
		public ushort DisplayHallOfFame(ushort arg1)
		{
			this.processor.Journal.OpenScope($"DisplayHallOfFame({arg1})");

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x33be, 160, 16, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x33cb, 160, 24, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 80, 32, 240, 32, 14);

			short vertLoc = 0x28;

			for (ushort placeIdx = 0; placeIdx < 6; placeIdx++)
			{
				if (placeIdx != arg1 && placeIdx == 5)
					break;

				if (arg1 == 5 && placeIdx == 4)
					continue;

				vertLoc = RenderFameRecord(placeIdx, arg1, vertLoc);
			}

			if (arg1 != 0xffff)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x346a, 252, 189, 0);

				this.owner.Dialogs.RenderBox(236, 187, 60, 10, 11);
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			ushort key = this.processor.RegAcc.Value16;
			ushort result = (ushort)(arg1 != 0xffff && (key == 'c' || key == 'C') ? 1 : 0);

			this.processor.RegAcc.Value16 = result;

			// Far return
			this.processor.Journal.CloseScope("DisplayHallOfFame");

			return result;
		}

		/// <summary>One place of the Hall of Fame at the row, " ---" when empty</summary>
		/// <returns>The row of the next place</returns>
		private short RenderFameRecord(ushort placeIdx, ushort arg1, short vertLoc)
		{
			const short horizLoc = 8;
			ushort record = (ushort)(0x2e * placeIdx);

			// The records are in the segment 0x3772: ES is set to it where the original sets it, the functions called may change it
			this.processor.SegExtra.Value16 = 0x3772;

			short score = (short)this.processor.PullUInt16(0x3772, (ushort)(record + 0x37d2));

			if (score <= 0)
			{
				this.owner.Libc.CopyText(0xba06, " ---");

				this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc, vertLoc, 0);

				return (short)(vertLoc + 0x1a);
			}

			EmptyMessage()
				.Number((short)(placeIdx + 1))
				.Text(". ");

			this.owner.Libc.MoveFarData(0x3772, (ushort)(record + 0x37e0), this.processor.SegData.Value16, (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06)), 16);

			this.owner.Libc.AppendText(0xba06, ", ");

			this.processor.SegExtra.Value16 = 0x3772;
			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((this.processor.PullUInt16(0x3772, (ushort)(record + 0x37d4)) << 1) + 0x33a2)));

			this.owner.Libc.AppendText(0xba06, " of the ");

			this.owner.Libc.MoveFarData(0x3772, (ushort)(record + 0x37f0), this.processor.SegData.Value16, (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06)), 16);

			ushort length = this.owner.Libc.TextLength(0xba06);

			this.owner.Libc.AppendText(0xba06, " to ");

			this.processor.SegExtra.Value16 = 0x3772;
			short year = (short)this.processor.PullUInt16(0x3772, (ushort)(record + 0x37d8));

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(Math.Abs(year), 10));

			this.owner.Libc.AppendText(0xba06, year < 0 ? (ushort)0x33f1 : (ushort)0x33f6);

			// Too wide: only up to the civilization
			if ((short)((ushort)this.owner.TextPainter.ObtainTextWidth(0xba06) + horizLoc) >= 0x140)
			{
				this.processor.EmitGlobalByte((ushort)(length + 0xba06), 0x0);
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc, vertLoc, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "Population: ");

			this.processor.SegExtra.Value16 = 0x3772;
			this.owner.Helpers.PopulationToText(this.processor.PullInt16(0x3772, (ushort)(record + 0x37de)));

			this.owner.Libc.AppendText(0xba06, ", Score: ");

			this.processor.SegExtra.Value16 = 0x3772;
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(0x3772, (ushort)(record + 0x37d2)), 10));

			this.processor.SegExtra.Value16 = 0x3772;
			ushort hardness = this.processor.PullUInt16(0x3772, (ushort)(record + 0x37d4));

			if (hardness <= 4)
			{
				this.owner.Libc.AppendText(0xba06, new ushort[] { 0x3412, 0x341e, 0x3429, 0x3433, 0x343b }[hardness]);
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc + 0xc, vertLoc, 7);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "    --- CIVILIZATION RATING: ");

			// The score is read through ES as the functions called since left it, like in the original
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)((((short)this.processor.PullUInt16(0x3772, (ushort)(record + 0x37d4)) + 1) *
				(short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(record + 0x37d2))) / 0x32), 10));

			this.owner.Libc.AppendText(0xba06, "% ---");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc + 0xc, vertLoc, 12);

			vertLoc += 10;

			if (placeIdx == arg1)
			{
				this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), horizLoc - 4, vertLoc - 28, 296, 26, 15, 14);
			}

			return vertLoc;
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

				EmptyMessage()
					.Number((short)(placeIdx + 1))
					.Text(". ")
					.City(townIdx)
					.Text(" (")
					.Text(this.processor.PullGlobalWord((ushort)((this.owner.World.Towns[townIdx].ParticipantIdx << 1) + 0x1982)))
					.Text(")");

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
