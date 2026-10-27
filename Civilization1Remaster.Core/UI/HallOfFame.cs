using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class HallOfFame : GameModule
	{
		public const ushort HallOfFameSegment = 0x3772;
		public const ushort HallOfFameBaseAddress = 0x37d2;
		public const int HallOfFameRecordCount = 6;
		public const int HallOfFameRecordSize = 0x2e;
		public const int HallOfFameTotalSize = 0x114;

		private const ushort HallOfFameTitleStringAddress = 0x33be;
		private const ushort HallOfFameSubtitleStringAddress = 0x33cb;
		private const ushort HallOfFameClearPromptStringAddress = 0x346a;
		private const ushort HallOfFameRulerTitlesTableAddress = 0x33a2;
		private const ushort HallOfFameBcEraStringAddress = 0x33f1;
		private const ushort HallOfFameAdEraStringAddress = 0x33f6;
		private const ushort HallOfFameRetiresEndingStringAddress = 0x3471;
		private const ushort HallOfFameExceedsEvenEndingStringAddress = 0x3482;
		private const ushort TopCitiesTitleStringAddress = 0x3486;

		private const ushort RulerRankTitlesSegment = 0x36d4;
		private const int RulerRankEntryStride = 0x19;

		private static readonly ushort[] DifficultyNameAddresses = [0x3412, 0x341e, 0x3429, 0x3433, 0x343b];

		private const ushort OffsetScore = 0x0;
		private const ushort OffsetDifficulty = 0x2;
		private const ushort OffsetSchismShare = 0x4;
		private const ushort OffsetYear = 0x6;
		private const ushort OffsetWonderScore = 0x8;
		private const ushort OffsetCitizenScore = 0xa;
		private const ushort OffsetPopulation = 0xc;
		private const ushort OffsetLeaderName = 0xe;
		private const ushort OffsetCivilizationPlural = 0x1e;

		public HallOfFame(GameEngine owner)
			: base(owner)
		{
		}

		private static ushort HallOfFameRecordAddress(int entryIdx)
		{
			return (ushort)(HallOfFameBaseAddress + (entryIdx * HallOfFameRecordSize));
		}

		private short FameScore(int entryIdx)
		{
			return this.processor.PullInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetScore));
		}

		private void SetFameScore(int entryIdx, short score)
		{
			this.processor.EmitUInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetScore), (ushort)score);
		}

		private ushort FameDifficulty(int entryIdx)
		{
			return this.processor.PullUInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetDifficulty));
		}

		private short FameYear(int entryIdx)
		{
			return (short)this.processor.PullUInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetYear));
		}

		private short FamePopulation(int entryIdx)
		{
			return this.processor.PullInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetPopulation));
		}

		/// <summary>
		/// Empties the 6 entries of the Hall of Fame (records of 0x2e bytes at 0x3772:37d2, the first word -1)
		/// </summary>
		public void WipeHallOfFame()
		{
			this.processor.Journal.OpenScope("WipeHallOfFame()");

			this.processor.SegExtra.Value16 = HallOfFameSegment;

			for (int entryIdx = 0; entryIdx < HallOfFameRecordCount; entryIdx++)
			{
				SetFameScore(entryIdx, -1);
			}

			this.processor.Journal.CloseScope("WipeHallOfFame");
		}

		/// <summary>
		/// Wipes the Hall of Fame and reads it from fame.dta (0x114 bytes, through the buffer TextBuffer.Address) into 0x3772:0x37d2
		/// </summary>
		public void ImportHallOfFame()
		{
			this.processor.Journal.OpenScope("ImportHallOfFame()");

			WipeHallOfFame();

			short handle = this.owner.Libc.OpenHandle("fame.dta", 0x8102, 0x80);

			this.owner.Libc.ReadHandle(handle, TextBuffer.Address, HallOfFameTotalSize);

			this.owner.Libc.MoveFarData(this.processor.SegData.Value16, TextBuffer.Address, HallOfFameSegment, HallOfFameBaseAddress, HallOfFameTotalSize);

			this.owner.Libc.CloseHandle(handle);

			this.processor.Journal.CloseScope("ImportHallOfFame");
		}

		/// <summary>
		/// Writes the Hall of Fame (0x114 bytes at 0x3772:0x37d2, through the buffer TextBuffer.Address) to fame.dta
		/// </summary>
		public void StoreHallOfFame()
		{
			this.processor.Journal.OpenScope("StoreHallOfFame()");

			short handle = this.owner.Libc.OpenHandle("fame.dta", 0x8302, 0x80);

			this.owner.Libc.MoveFarData(HallOfFameSegment, HallOfFameBaseAddress, this.processor.SegData.Value16, TextBuffer.Address, HallOfFameTotalSize);

			this.owner.Libc.WriteHandle(handle, TextBuffer.Address, HallOfFameTotalSize);

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

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);

			this.owner.TextPainter.RenderCenteredTextToScreen0(HallOfFameTitleStringAddress, 160, 16, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(HallOfFameSubtitleStringAddress, 160, 24, 0);

			this.owner.VideoCard.RenderLine(ScreenBox, 80, 32, 240, 32, 14);

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
				this.owner.TextPainter.RenderTextToScreen0(HallOfFameClearPromptStringAddress, 252, 189, 0);

				this.owner.Dialogs.RenderBox(236, 187, 60, 10, 11);
			}

			ushort key = (ushort)this.owner.Conquest.AwaitForKeyPressOrMouseClick();
			ushort result = (ushort)(arg1 != 0xffff && (key == 'c' || key == 'C') ? 1 : 0);

			// Far return
			this.processor.Journal.CloseScope("DisplayHallOfFame");

			return result;
		}

		/// <summary>One place of the Hall of Fame at the row, " ---" when empty</summary>
		/// <returns>The row of the next place</returns>
		private short RenderFameRecord(ushort placeIdx, ushort arg1, short vertLoc)
		{
			const short horizLoc = 8;
			ushort record = HallOfFameRecordAddress(placeIdx);

			// The records are in the segment 0x3772: ES is set to it where the original sets it, the functions called may change it
			this.processor.SegExtra.Value16 = HallOfFameSegment;

			short score = FameScore(placeIdx);

			if (score <= 0)
			{
				this.owner.Words.Set(" ---");

				this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, horizLoc, vertLoc, 0);

				return (short)(vertLoc + 0x1a);
			}

			EmptyMessage()
				.Number((short)(placeIdx + 1))
				.Text(". ");

			this.owner.Libc.MoveFarData(HallOfFameSegment, (ushort)(record + OffsetLeaderName), this.processor.SegData.Value16, (ushort)(TextBuffer.Address + this.owner.Words.Length), 16);

			this.owner.Words.Append(", ");

			this.processor.SegExtra.Value16 = HallOfFameSegment;
			this.owner.Words.Append(this.processor.PullGlobalWord((ushort)((FameDifficulty(placeIdx) << 1) + HallOfFameRulerTitlesTableAddress)));

			this.owner.Words.Append(" of the ");

			this.owner.Libc.MoveFarData(HallOfFameSegment, (ushort)(record + OffsetCivilizationPlural), this.processor.SegData.Value16, (ushort)(TextBuffer.Address + this.owner.Words.Length), 16);

			ushort length = this.owner.Words.Length;

			this.owner.Words.Append(" to ");

			this.processor.SegExtra.Value16 = HallOfFameSegment;
			short year = FameYear(placeIdx);

			this.owner.Words.Append(this.owner.Libc.IntegerToText(Math.Abs(year), 10));

			this.owner.Words.Append(year < 0 ? HallOfFameBcEraStringAddress : HallOfFameAdEraStringAddress);

			// Too wide: only up to the civilization
			if ((short)((ushort)this.owner.TextPainter.ObtainTextWidth(TextBuffer.Address) + horizLoc) >= 0x140)
			{
				this.processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0x0);
			}

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, horizLoc, vertLoc, 0);

			vertLoc += 8;

			this.owner.Words.Set("Population: ");

			this.processor.SegExtra.Value16 = HallOfFameSegment;
			this.owner.Helpers.PopulationToText(FamePopulation(placeIdx));

			this.owner.Words.Append(", Score: ");

			this.processor.SegExtra.Value16 = HallOfFameSegment;
			this.owner.Words.Append(this.owner.Libc.IntegerToText(FameScore(placeIdx), 10));

			this.processor.SegExtra.Value16 = HallOfFameSegment;
			ushort hardness = FameDifficulty(placeIdx);

			if (hardness <= 4)
			{
				this.owner.Words.Append(DifficultyNameAddresses[hardness]);
			}

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, horizLoc + 0xc, vertLoc, 7);

			vertLoc += 8;

			this.owner.Words.Set("    --- CIVILIZATION RATING: ");

			// The score is read through ES as the functions called since left it, like in the original
			this.owner.Words.Append(this.owner.Libc.IntegerToText((short)((((short)FameDifficulty(placeIdx) + 1) *
				(short)this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(record + OffsetScore))) / 0x32), 10));

			this.owner.Words.Append("% ---");

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, horizLoc + 0xc, vertLoc, 12);

			vertLoc += 10;

			if (placeIdx == arg1)
			{
				this.owner.VideoCard.SubstituteHue(ScreenBox, horizLoc - 4, vertLoc - 28, 296, 26, 15, 14);
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
				ushort record = HallOfFameRecordAddress(slotIdx);
				short rating = (short)((this.processor.PullUInt16(HallOfFameSegment, (ushort)(record + OffsetDifficulty)) + 2) * this.processor.PullUInt16(HallOfFameSegment, (ushort)(record + OffsetScore)));
				short gameRating = (short)((this.owner.World.HardnessTier + 2) * HallOfFameScore);

				if (rating >= gameRating && slotIdx != 5)
					continue;

				for (int laterIdx = 5; laterIdx > slotIdx; laterIdx--)
				{
					this.owner.Libc.MoveFarData(HallOfFameSegment, HallOfFameRecordAddress(laterIdx - 1), HallOfFameSegment, HallOfFameRecordAddress(laterIdx), HallOfFameRecordSize);
				}

				short humanIdx = this.owner.World.PersonParticipantIdx;

				this.processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetScore), HallOfFameScore);
				this.owner.Libc.MoveFarData(this.processor.SegData.Value16, LeaderName(humanIdx),
					HallOfFameSegment, (ushort)(record + OffsetLeaderName), 0xe);
				this.owner.Libc.MoveFarData(this.processor.SegData.Value16, CivilizationPlural(humanIdx),
					HallOfFameSegment, (ushort)(record + OffsetCivilizationPlural), 0x10);
				this.processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetDifficulty), (ushort)this.owner.World.HardnessTier);
				this.processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetSchismShare), SchismShareDc6a);
				this.processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetWonderScore), WonderScore);
				this.processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetCitizenScore), CitizenScore);
				this.processor.EmitInt16(HallOfFameSegment, (ushort)(record + OffsetYear), this.owner.World.Season);

				ushort population = this.owner.Helpers.ObtainPlayerPopulation(humanIdx);

				this.processor.SegExtra.Value16 = HallOfFameSegment;
				this.processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetPopulation), population);

				if (DisplayHallOfFame((ushort)slotIdx) == 0)
				{
					place = (ushort)slotIdx;
					break;
				}

				WipeHallOfFame();

				slotIdx = -1;
			}

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

				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

				this.owner.Services.PerformSound(0x22, 0);

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);

				this.owner.UnitOps.RenderBusyIndicator();

				this.owner.VideoCard.RenderImage(PictureScreenBox, 0xc8, 0x8c, 0x28, 0x3c, ScreenBox, 10, 10);

				this.owner.VideoCard.RenderImage(PictureScreenBox, 0xf0, 0x8c, 0x28, 0x3c, ScreenBox, 0x10e, 10);

				DrawingBox.TypefaceIdx = 7;

				this.owner.Helpers.FloodBoxWithMotif(80, 4, 160, 27);

				this.owner.Dialogs.RenderShadowBox(80, 4, 159, 26, 11, 8);

				ushort ending;

				if (arg2 == 0)
				{
					PullRankTitle(20 * RulerRankEntryStride);
					this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 6, 15);

					PullRankTitle(21 * RulerRankEntryStride);
					this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 14, 15);

					ending = HallOfFameRetiresEndingStringAddress;
				}
				else
				{
					PullRankTitle(22 * RulerRankEntryStride);
					this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 6, 15);

					PullRankTitle(23 * RulerRankEntryStride);
					this.owner.Words.Append(this.owner.Libc.IntegerToText(percentage, 10));
					this.owner.Words.Append("% exceeds even");
					this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 14, 15);

					ending = HallOfFameExceedsEvenEndingStringAddress;
				}

				PullRankTitle((ushort)((0x13 - rank) * RulerRankEntryStride));
				this.owner.Words.Append(ending);
				this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 22, 15);

				for (short level = 0; level <= rank; level++)
				{
					PullRankTitle((ushort)((0x13 - level) * 0x19));
					this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, -((level * 8) - 184), (ushort)(level == rank ? 0 : 7));
				}

				this.owner.VideoCard.SubstituteHue(ScreenBox, 80, 183 - rank * 8, 160, 8, 15, 14);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.Services.PerformSound(1, 0);

				DrawingBox.TypefaceIdx = 1;

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayGameResultsScreen");
		}

		/// <summary>Copies the text (0x20 bytes) at 0x36d4:offset, the ranking titles, to TextBuffer.Address</summary>
		private void PullRankTitle(ushort offset)
		{
			this.owner.Libc.MoveFarData(RulerRankTitlesSegment, offset, this.processor.SegData.Value16, TextBuffer.Address, 0x20);
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

			short[] scores = new short[5];
			short[] towns = new short[5];

			for (int placeIdx = 0; placeIdx < 5; placeIdx++)
			{
				towns[placeIdx] = -1;
				scores[placeIdx] = -1;
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff)
					continue;

				this.owner.CityOps.HandleCity(townIdx, -1);

				short score = (short)(this.owner.World.Towns[townIdx].TrueExtent + CityHappy - CityUnhappy);

				for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (this.owner.World.MarvelTownIdx[wonderIdx] == townIdx)
					{
						score += 10;
					}
				}

				for (int placeIdx = 0; placeIdx < 5; placeIdx++)
				{
					if (scores[placeIdx] >= score)
						continue;

					for (int laterIdx = 3; laterIdx >= placeIdx; laterIdx--)
					{
						scores[laterIdx + 1] = scores[laterIdx];
						towns[laterIdx + 1] = towns[laterIdx];
					}

					scores[placeIdx] = score;
					towns[placeIdx] = townIdx;
					break;
				}
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 3);

			this.owner.TextPainter.RenderTextWithShadow(TopCitiesTitleStringAddress, 80, 12, 15);

			short vertLoc = 0x20;

			for (short placeIdx = 0; placeIdx < 5; placeIdx++)
			{
				if (scores[placeIdx] == -1)
					continue;

				short townIdx = towns[placeIdx];

				EmptyMessage()
					.Number((short)(placeIdx + 1))
					.Text(". ")
					.City(townIdx)
					.Text(" (")
					.Text(CivilizationName(this.owner.World.Towns[townIdx].ParticipantIdx))
					.Text(")");

				this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, vertLoc + 3,
					PlayerColour(this.owner.World.Towns[townIdx].ParticipantIdx));

				this.owner.CityOps.HandleCity(townIdx, -1);

				this.owner.CityOps.RenderCitizenIcons(townIdx, 48, (short)(vertLoc + 10), CitySpecialists, 160);

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
					PlayerColour(this.owner.World.Towns[townIdx].ParticipantIdx));

				vertLoc += 0x20;
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayTopCities");
		}
	}
}
