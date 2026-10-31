using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class HallOfFame : GameModule, IHallOfFame
	{
		public const ushort HallOfFameSegment = 0x3772;
		public const ushort HallOfFameBaseAddress = 0x37d2;
		public const int HallOfFameRecordCount = 6;
		public const int HallOfFameRecordSize = 0x2e;
		public const int HallOfFameTotalSize = 0x114;

		private const ushort HallOfFameClearPromptStringAddress = 0x346a;
		private const ushort HallOfFameRulerTitlesTableAddress = 0x33a2;
		private const ushort HallOfFameBcEraStringAddress = 0x33f1;
		private const ushort HallOfFameAdEraStringAddress = 0x33f6;
		private const ushort HallOfFameRetiresEndingStringAddress = 0x3471;
		private const ushort HallOfFameExceedsEvenEndingStringAddress = 0x3482;
		private const ushort RulerRankTitlesSegment = 0x36d4;
		private const int RulerRankEntryStride = 0x19;
		private const ushort OffsetScore = 0x0;
		private const ushort OffsetDifficulty = 0x2;
		private const ushort OffsetSchismShare = 0x4;
		private const ushort OffsetYear = 0x6;
		private const ushort OffsetWonderScore = 0x8;
		private const ushort OffsetCitizenScore = 0xa;
		private const ushort OffsetPopulation = 0xc;
		private const ushort OffsetLeaderName = 0xe;
		private const ushort OffsetCivilizationPlural = 0x1e;

		private static readonly GameText HallOfFameTitleStringAddress = new(0x33be, "CIVILIZATION");
		private static readonly GameText HallOfFameSubtitleStringAddress = new(0x33cb, "HALL OF FAME");
		private static readonly GameText TopCitiesTitleStringAddress = new(0x3486, "The Top Five Cities in the World");
		private static readonly ushort[] DifficultyNameAddresses = [0x3412, 0x341e, 0x3429, 0x3433, 0x343b];

		public HallOfFame(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void WipeHallOfFame()
		{
			this.Processor.Journal.OpenScope("WipeHallOfFame()");

			this.Processor.SegExtra.Value16 = HallOfFameSegment;

			for (int entryIdx = 0; entryIdx < HallOfFameRecordCount; entryIdx++)
			{
				SetFameScore(entryIdx, -1);
			}

			this.Processor.Journal.CloseScope("WipeHallOfFame");
		}

		/// <inheritdoc/>
		public void ImportHallOfFame()
		{
			this.Processor.Journal.OpenScope("ImportHallOfFame()");

			WipeHallOfFame();

			short handle = this.Owner.Libc.OpenHandle("fame.dta", 0x8102, 0x80);

			this.Owner.Libc.ReadHandle(handle, TextBuffer.Address, HallOfFameTotalSize);

			this.Owner.Libc.MoveFarData(this.Processor.SegData.Value16, TextBuffer.Address, HallOfFameSegment, HallOfFameBaseAddress, HallOfFameTotalSize);

			this.Owner.Libc.CloseHandle(handle);

			this.Processor.Journal.CloseScope("ImportHallOfFame");
		}

		/// <inheritdoc/>
		public void StoreHallOfFame()
		{
			this.Processor.Journal.OpenScope("StoreHallOfFame()");

			short handle = this.Owner.Libc.OpenHandle("fame.dta", 0x8302, 0x80);

			this.Owner.Libc.MoveFarData(HallOfFameSegment, HallOfFameBaseAddress, this.Processor.SegData.Value16, TextBuffer.Address, HallOfFameTotalSize);

			this.Owner.Libc.WriteHandle(handle, TextBuffer.Address, HallOfFameTotalSize);

			this.Owner.Libc.CloseHandle(handle);

			this.Processor.Journal.CloseScope("StoreHallOfFame");
		}

		/// <inheritdoc/>
		public ushort DisplayHallOfFame(ushort newPlace)
		{
			this.Processor.Journal.OpenScope($"DisplayHallOfFame({newPlace})");

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.White);

			this.Owner.TextPainter.RenderCenteredTextToScreen0(HallOfFameTitleStringAddress, 160, 16, Hues.Black);

			this.Owner.TextPainter.RenderCenteredTextToScreen0(HallOfFameSubtitleStringAddress, 160, 24, Hues.Black);

			this.Owner.VideoCard.RenderLine(ScreenBox, 80, 32, 240, 32, Hues.Yellow);

			short vertLoc = 0x28;

			for (ushort placeIdx = 0; placeIdx < 6; placeIdx++)
			{
				if (placeIdx != newPlace && placeIdx == 5)
					break;

				if (newPlace == 5 && placeIdx == 4)
					continue;

				vertLoc = RenderFameRecord(placeIdx, newPlace, vertLoc);
			}

			if (newPlace != 0xffff)
			{
				this.Owner.TextPainter.RenderTextToScreen0(HallOfFameClearPromptStringAddress, 252, 189, Hues.Black);

				this.Owner.Dialogs.RenderBox(236, 187, 60, 10, Hues.LightCyan);
			}

			ushort key = (ushort)this.Owner.Conquest.AwaitForKeyPressOrMouseClick();
			ushort result = (ushort)(newPlace != 0xffff && (key == 'c' || key == 'C') ? 1 : 0);

			this.Processor.Journal.CloseScope("DisplayHallOfFame");

			return result;
		}

		/// <inheritdoc/>
		public ushort AttachHallOfFameRecord()
		{
			this.Processor.Journal.OpenScope("AttachHallOfFameRecord()");

			ushort place = 0xffff;

			for (int slotIdx = 0; slotIdx < 6; slotIdx++)
			{
				ushort record = HallOfFameRecordAddress(slotIdx);
				short rating = (short)((this.Processor.PullUInt16(HallOfFameSegment, (ushort)(record + OffsetDifficulty)) + 2) * this.Processor.PullUInt16(HallOfFameSegment, (ushort)(record + OffsetScore)));
				short gameRating = (short)((this.Owner.World.HardnessTier + 2) * HallOfFameScore);

				if (rating >= gameRating && slotIdx != 5)
					continue;

				for (int laterIdx = 5; laterIdx > slotIdx; laterIdx--)
				{
					this.Owner.Libc.MoveFarData(HallOfFameSegment, HallOfFameRecordAddress(laterIdx - 1), HallOfFameSegment, HallOfFameRecordAddress(laterIdx), HallOfFameRecordSize);
				}

				short humanIdx = this.Owner.World.PersonParticipantIdx;

				this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetScore), HallOfFameScore);
				this.Owner.Libc.MoveFarData(this.Processor.SegData.Value16, LeaderName(humanIdx),
					HallOfFameSegment, (ushort)(record + OffsetLeaderName), 0xe);
				this.Owner.Libc.MoveFarData(this.Processor.SegData.Value16, CivilizationPlural(humanIdx),
					HallOfFameSegment, (ushort)(record + OffsetCivilizationPlural), 0x10);
				this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetDifficulty), (ushort)this.Owner.World.HardnessTier);
				this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetSchismShare), SchismShareDc6a);
				this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetWonderScore), WonderScore);
				this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetCitizenScore), CitizenScore);
				this.Processor.EmitInt16(HallOfFameSegment, (ushort)(record + OffsetYear), this.Owner.World.Season);

				ushort population = this.Owner.Helpers.ObtainPlayerPopulation(humanIdx);

				this.Processor.SegExtra.Value16 = HallOfFameSegment;
				this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(record + OffsetPopulation), population);

				if (DisplayHallOfFame((ushort)slotIdx) == 0)
				{
					place = (ushort)slotIdx;
					break;
				}

				WipeHallOfFame();

				slotIdx = -1;
			}

			this.Processor.Journal.CloseScope("AttachHallOfFameRecord");

			return place;
		}

		/// <inheritdoc/>
		public void DisplayGameResultsScreen(ushort rankGiven, ushort score)
		{
			this.Processor.Journal.OpenScope($"DisplayGameResultsScreen({rankGiven}, {score})");

			short rank = (short)rankGiven;
			short percentage = 0;

			if (score != 0)
			{
				percentage = CPU.SignedQuotient((short)(score * (this.Owner.World.HardnessTier + 1)), 0x32);
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

				this.Owner.Startup.ConcealMouseCursor();

				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

				this.Owner.Services.PerformSound(0x22, 0);

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.White);

				this.Owner.UnitOps.RenderBusyIndicator();

				this.Owner.VideoCard.RenderImage(PictureScreenBox, 0xc8, 0x8c, 0x28, 0x3c, ScreenBox, 10, 10);

				this.Owner.VideoCard.RenderImage(PictureScreenBox, 0xf0, 0x8c, 0x28, 0x3c, ScreenBox, 0x10e, 10);

				DrawingBox.TypefaceIdx = 7;

				this.Owner.Helpers.FloodBoxWithMotif(80, 4, 160, 27);

				this.Owner.Dialogs.RenderShadowBox(80, 4, 159, 26, Hues.LightCyan, Hues.DarkGray);

				ushort ending;

				if (score == 0)
				{
					PullRankTitle(20 * RulerRankEntryStride);
					this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 6, Hues.White);

					PullRankTitle(21 * RulerRankEntryStride);
					this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 14, Hues.White);

					ending = HallOfFameRetiresEndingStringAddress;
				}
				else
				{
					PullRankTitle(22 * RulerRankEntryStride);
					this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 6, Hues.White);

					PullRankTitle(23 * RulerRankEntryStride);
					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(percentage, 10));
					this.Owner.Words.Append("% exceeds even");
					this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 14, Hues.White);

					ending = HallOfFameExceedsEvenEndingStringAddress;
				}

				PullRankTitle((ushort)((0x13 - rank) * RulerRankEntryStride));
				this.Owner.Words.Append(ending);
				this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 22, Hues.White);

				for (short level = 0; level <= rank; level++)
				{
					PullRankTitle((ushort)((0x13 - level) * 0x19));
					this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, -((level * 8) - 184), (ushort)(level == rank ? 0 : 7));
				}

				this.Owner.VideoCard.SubstituteHue(ScreenBox, 80, 183 - rank * 8, 160, 8, Hues.White, Hues.Yellow);

				this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.Owner.Services.PerformSound(1, 0);

				DrawingBox.TypefaceIdx = 1;

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				this.Owner.Startup.DisplayMouseCursor();
			}

			this.Processor.Journal.CloseScope("DisplayGameResultsScreen");
		}

		/// <inheritdoc/>
		public void DisplayTopCities()
		{
			this.Processor.Journal.OpenScope("DisplayTopCities()");

			short[] scores = new short[5];
			short[] towns = new short[5];

			for (int placeIdx = 0; placeIdx < 5; placeIdx++)
			{
				towns[placeIdx] = -1;
				scores[placeIdx] = -1;
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff)
					continue;

				this.Owner.CityOps.HandleCity(townIdx, -1);

				short score = (short)(this.Owner.World.Towns[townIdx].TrueExtent + CityHappy - CityUnhappy);

				for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (this.Owner.World.MarvelTownIdx[wonderIdx] == townIdx)
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

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Cyan);

			this.Owner.TextPainter.RenderTextWithShadow(TopCitiesTitleStringAddress, 80, 12, Hues.White);

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
					.Text(CivilizationName(this.Owner.World.Towns[townIdx].ParticipantIdx))
					.Text(")");

				this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc + 3,
					PlayerColour(this.Owner.World.Towns[townIdx].ParticipantIdx));

				this.Owner.CityOps.HandleCity(townIdx, -1);

				this.Owner.CityOps.RenderCitizenIcons(townIdx, 48, (short)(vertLoc + 10), CitySpecialists, 160);

				short iconHoriz = (short)(0x30 + this.Owner.Helpers.VerifyValueSpan(this.Owner.World.Towns[townIdx].TrueExtent * 8, 0, 160) + 8);

				for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (this.Owner.World.MarvelTownIdx[wonderIdx] != townIdx)
						continue;

					if (iconHoriz < 0x125)
					{
						this.Owner.CityOps.RenderResourceIcon((short)(wonderIdx + 24), iconHoriz, (short)(vertLoc + 12));
					}

					iconHoriz += 0x13;
				}

				this.Owner.Dialogs.RenderBox(8, vertLoc, 303, 25,
					PlayerColour(this.Owner.World.Towns[townIdx].ParticipantIdx));

				vertLoc += 0x20;
			}

			this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayTopCities");
		}

		private static ushort HallOfFameRecordAddress(int entryIdx)
		{
			return (ushort)(HallOfFameBaseAddress + (entryIdx * HallOfFameRecordSize));
		}

		private short FameScore(int entryIdx)
		{
			return this.Processor.PullInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetScore));
		}

		private void SetFameScore(int entryIdx, short score)
		{
			this.Processor.EmitUInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetScore), (ushort)score);
		}

		private ushort FameDifficulty(int entryIdx)
		{
			return this.Processor.PullUInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetDifficulty));
		}

		private short FameYear(int entryIdx)
		{
			return (short)this.Processor.PullUInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetYear));
		}

		private short FamePopulation(int entryIdx)
		{
			return this.Processor.PullInt16(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIdx) + OffsetPopulation));
		}

		/// <summary>One place of the Hall of Fame at the row, " ---" when empty</summary>
		/// <returns>The row of the next place</returns>
		private short RenderFameRecord(ushort placeIdx, ushort newPlace, short vertLoc)
		{
			const short horizLoc = 8;
			ushort record = HallOfFameRecordAddress(placeIdx);

			// The records are in the segment 0x3772: ES is set to it where the original sets it, the functions called may change it
			this.Processor.SegExtra.Value16 = HallOfFameSegment;

			short score = FameScore(placeIdx);

			if (score <= 0)
			{
				this.Owner.Words.Set(" ---");

				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), horizLoc, vertLoc, Hues.Black);

				return (short)(vertLoc + 0x1a);
			}

			EmptyMessage()
				.Number((short)(placeIdx + 1))
				.Text(". ");

			this.Owner.Libc.MoveFarData(HallOfFameSegment, (ushort)(record + OffsetLeaderName), this.Processor.SegData.Value16, (ushort)(TextBuffer.Address + this.Owner.Words.Length), 16);

			this.Owner.Words.Append(", ");

			this.Processor.SegExtra.Value16 = HallOfFameSegment;
			this.Owner.Words.Append(this.Processor.PullGlobalWord((ushort)((FameDifficulty(placeIdx) << 1) + HallOfFameRulerTitlesTableAddress)));

			this.Owner.Words.Append(" of the ");

			this.Owner.Libc.MoveFarData(HallOfFameSegment, (ushort)(record + OffsetCivilizationPlural), this.Processor.SegData.Value16, (ushort)(TextBuffer.Address + this.Owner.Words.Length), 16);

			ushort length = this.Owner.Words.Length;

			this.Owner.Words.Append(" to ");

			this.Processor.SegExtra.Value16 = HallOfFameSegment;
			short year = FameYear(placeIdx);

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(Math.Abs(year), 10));

			this.Owner.Words.Append(year < 0 ? HallOfFameBcEraStringAddress : HallOfFameAdEraStringAddress);

			// Too wide: only up to the civilization
			if ((short)((ushort)this.Owner.TextPainter.ObtainTextWidth(this.Owner.Words.ToString()) + horizLoc) >= 0x140)
			{
				this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0x0);
			}

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), horizLoc, vertLoc, Hues.Black);

			vertLoc += 8;

			this.Owner.Words.Set("Population: ");

			this.Processor.SegExtra.Value16 = HallOfFameSegment;
			this.Owner.Helpers.PopulationToText(FamePopulation(placeIdx));

			this.Owner.Words.Append(", Score: ");

			this.Processor.SegExtra.Value16 = HallOfFameSegment;
			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(FameScore(placeIdx), 10));

			this.Processor.SegExtra.Value16 = HallOfFameSegment;
			ushort hardness = FameDifficulty(placeIdx);

			if (hardness <= 4)
			{
				this.Owner.Words.Append(DifficultyNameAddresses[hardness]);
			}

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), horizLoc + 0xc, vertLoc, Hues.LightGray);

			vertLoc += 8;

			this.Owner.Words.Set("    --- CIVILIZATION RATING: ");

			// The score is read through ES as the functions called since left it, like in the original
			this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)((((short)FameDifficulty(placeIdx) + 1) *
				(short)this.Processor.PullUInt16(this.Processor.SegExtra.Value16, (ushort)(record + OffsetScore))) / 0x32), 10));

			this.Owner.Words.Append("% ---");

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), horizLoc + 0xc, vertLoc, Hues.LightRed);

			vertLoc += 10;

			if (placeIdx == newPlace)
			{
				this.Owner.VideoCard.SubstituteHue(ScreenBox, horizLoc - 4, vertLoc - 28, 296, 26, Hues.White, Hues.Yellow);
			}

			return vertLoc;
		}

		/// <summary>Copies the text (0x20 bytes) at 0x36d4:offset, the ranking titles, to TextBuffer.Address</summary>
		private void PullRankTitle(ushort offset)
		{
			this.Owner.Libc.MoveFarData(RulerRankTitlesSegment, offset, this.Processor.SegData.Value16, TextBuffer.Address, 0x20);
		}
	}
}
