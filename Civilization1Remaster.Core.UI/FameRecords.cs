using System;

namespace Civilization1Remaster.Core.UI
{
	public class FameRecords : GameComponent, IFameRecords
	{
		public const ushort HallOfFameSegment = 0x3772;
		public const ushort HallOfFameBaseAddress = 0x37d2;
		public const int HallOfFameRecordCount = 6;
		public const int HallOfFameRecordSize = 0x2e;
		public const int HallOfFameTotalSize = 0x114;

		private const ushort HallOfFameClearPromptStringAddress = 0x346a;
		private const ushort HallOfFameRulerTitlesTableAddress = 0x33a2;
		private const ushort HallOfFameBeforeChristEraStringAddress = 0x33f1;
		private const ushort HallOfFameAnnoDominiEraStringAddress = 0x33f6;
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

		public FameRecords(IGameEngine owner)
			: base(owner)
		{
		}

		public void ClearHallOfFame()
		{
			using var _ = this.Processor.Journal.Scope("WipeHallOfFame()");

			this.Processor.SegmentExtra.Word = HallOfFameSegment;

			for (int entryIndex = 0; entryIndex < HallOfFameRecordCount; entryIndex++)
			{
				SetFameScore(entryIndex, -1);
			}
		}

		public void LoadHallOfFame()
		{
			using var _ = this.Processor.Journal.Scope("ImportHallOfFame()");

			ClearHallOfFame();

			short handle = this.Owner.Runtime.OpenHandle("fame.dta", 0x8102, 0x80);

			this.Owner.Runtime.ReadHandle(handle, TextBuffer.Address, HallOfFameTotalSize);

			this.Owner.Runtime.MoveFarData(this.Processor.SegmentData.Word, TextBuffer.Address, HallOfFameSegment, HallOfFameBaseAddress, HallOfFameTotalSize);

			this.Owner.Runtime.CloseHandle(handle);
		}

		public void SaveHallOfFame()
		{
			using var _ = this.Processor.Journal.Scope("StoreHallOfFame()");

			short handle = this.Owner.Runtime.OpenHandle("fame.dta", 0x8302, 0x80);

			this.Owner.Runtime.MoveFarData(HallOfFameSegment, HallOfFameBaseAddress, this.Processor.SegmentData.Word, TextBuffer.Address, HallOfFameTotalSize);

			this.Owner.Runtime.WriteHandle(handle, TextBuffer.Address, HallOfFameTotalSize);

			this.Owner.Runtime.CloseHandle(handle);
		}

		public ushort ShowHallOfFame(ushort newPlace)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayHallOfFame({newPlace})");

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(HallOfFameTitleStringAddress, 160, 16, Hues.Black);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(HallOfFameSubtitleStringAddress, 160, 24, Hues.Black);

			this.Owner.VideoCard.RenderLine(Screen.Box, 80, 32, 240, 32, Hues.Yellow);

			short verticalLocation = 0x28;

			for (ushort placeIndex = 0; placeIndex < 6; placeIndex++)
			{
				if (placeIndex != newPlace && placeIndex == 5)
				{
					break;
				}

				if (newPlace == 5 && placeIndex == 4)
				{
					continue;
				}

				verticalLocation = DrawFameRecord(placeIndex, newPlace, verticalLocation);
			}

			if (newPlace != 0xffff)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(HallOfFameClearPromptStringAddress, 252, 189, Hues.Black);

				this.Owner.Dialogs.DrawBox(236, 187, 60, 10, Hues.LightCyan);
			}

			ushort key = (ushort)this.Owner.Conquest.WaitForKeyPressOrMouseClick();
			ushort result = (ushort)(newPlace != 0xffff && (key == 'c' || key == 'C') ? 1 : 0);

			return result;
		}

		public ushort AttachHallOfFameRecord()
		{
			using var _ = this.Processor.Journal.Scope("AttachHallOfFameRecord()");

			ushort place = 0xffff;

			for (int slotIndex = 0; slotIndex < 6; slotIndex++)
			{
				ushort record = HallOfFameRecordAddress(slotIndex);
				short rating = (short)((this.Processor.PullWord(HallOfFameSegment, (ushort)(record + OffsetDifficulty)) + 2) * this.Processor.PullWord(HallOfFameSegment, (ushort)(record + OffsetScore)));
				short gameRating = (short)((this.Owner.World.HardnessTier + 2) * Scoring.HallOfFameScore);

				if (rating >= gameRating && slotIndex != 5)
				{
					continue;
				}

				for (int laterIndex = 5; laterIndex > slotIndex; laterIndex--)
				{
					this.Owner.Runtime.MoveFarData(HallOfFameSegment, HallOfFameRecordAddress(laterIndex - 1), HallOfFameSegment, HallOfFameRecordAddress(laterIndex), HallOfFameRecordSize);
				}

				short humanIndex = this.Owner.World.PersonParticipantIndex;

				this.Processor.EmitWord(HallOfFameSegment, (ushort)(record + OffsetScore), Scoring.HallOfFameScore);
				this.Owner.Runtime.MoveFarData(this.Processor.SegmentData.Word, Tables.LeaderName(humanIndex),
					HallOfFameSegment, (ushort)(record + OffsetLeaderName), 0xe);
				this.Owner.Runtime.MoveFarData(this.Processor.SegmentData.Word, Tables.CivilizationPlural(humanIndex),
					HallOfFameSegment, (ushort)(record + OffsetCivilizationPlural), 0x10);
				this.Processor.EmitWord(HallOfFameSegment, (ushort)(record + OffsetDifficulty), (ushort)this.Owner.World.HardnessTier);
				this.Processor.EmitWord(HallOfFameSegment, (ushort)(record + OffsetSchismShare), Scoring.SchismShare);
				this.Processor.EmitWord(HallOfFameSegment, (ushort)(record + OffsetWonderScore), Scoring.WonderScore);
				this.Processor.EmitWord(HallOfFameSegment, (ushort)(record + OffsetCitizenScore), Scoring.CitizenScore);
				this.Processor.EmitSignedWord(HallOfFameSegment, (ushort)(record + OffsetYear), this.Owner.World.Season);

				ushort population = this.Owner.Helpers.ComputePlayerPopulation(humanIndex);

				this.Processor.SegmentExtra.Word = HallOfFameSegment;
				this.Processor.EmitWord(HallOfFameSegment, (ushort)(record + OffsetPopulation), population);

				if (ShowHallOfFame((ushort)slotIndex) == 0)
				{
					place = (ushort)slotIndex;
					break;
				}

				ClearHallOfFame();

				slotIndex = -1;
			}

			return place;
		}

		public void ShowGameResultsScreen(ushort rankGiven, ushort score)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayGameResultsScreen({rankGiven}, {score})");

			short rank = (short)rankGiven;
			short percentage = 0;

			if (score != 0)
			{
				percentage = CentralProcessor.SignedQuotient((short)(score * (this.Owner.World.HardnessTier + 1)), 0x32);
				rank = -1;

				for (short level = 1; level <= 0x14; level++)
				{
					if (CentralProcessor.SignedQuotient((short)(level * level), 3) < percentage)
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

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

				this.Owner.Services.PerformSound(0x22, 0);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.White);

				this.Owner.UnitOperations.DrawBusyIndicator();

				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xc8, 0x8c, 0x28, 0x3c, Screen.Box, 10, 10);

				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xf0, 0x8c, 0x28, 0x3c, Screen.Box, 0x10e, 10);

				DrawingBox.TypefaceIndex = 7;

				this.Owner.Helpers.FillBoxWithMotif(80, 4, 160, 27);

				this.Owner.Dialogs.DrawShadowBox(80, 4, 159, 26, Hues.LightCyan, Hues.DarkGray);

				ushort ending;

				if (score == 0)
				{
					PullRankTitle(20 * RulerRankEntryStride);
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 6, Hues.White);

					PullRankTitle(21 * RulerRankEntryStride);
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 14, Hues.White);

					ending = HallOfFameRetiresEndingStringAddress;
				}
				else
				{
					PullRankTitle(22 * RulerRankEntryStride);
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 6, Hues.White);

					PullRankTitle(23 * RulerRankEntryStride);
					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(percentage, 10));
					this.Owner.Words.Append("% exceeds even");
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 14, Hues.White);

					ending = HallOfFameExceedsEvenEndingStringAddress;
				}

				PullRankTitle((ushort)((0x13 - rank) * RulerRankEntryStride));
				this.Owner.Words.Append(ending);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 22, Hues.White);

				for (short level = 0; level <= rank; level++)
				{
					PullRankTitle((ushort)((0x13 - level) * 0x19));
					this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, -((level * 8) - 184), (ushort)(level == rank ? 0 : 7));
				}

				this.Owner.VideoCard.SubstituteHue(Screen.Box, 80, 183 - (rank * 8), 160, 8, Hues.White, Hues.Yellow);

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.Services.PerformSound(1, 0);

				DrawingBox.TypefaceIndex = 1;

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		public void ShowTopCities()
		{
			using var _ = this.Processor.Journal.Scope("DisplayTopCities()");

			short[] scores = new short[5];
			short[] towns = new short[5];

			for (int placeIndex = 0; placeIndex < 5; placeIndex++)
			{
				towns[placeIndex] = -1;
				scores[placeIndex] = -1;
			}

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff)
				{
					continue;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				short score = (short)(this.Owner.World.Towns[townIndex].TrueExtent + CityTurn.Happy - CityTurn.Unhappy);

				for (int wonderIndex = 1; wonderIndex <= 0x15; wonderIndex++)
				{
					if (this.Owner.World.MarvelTownIndex[wonderIndex] == townIndex)
					{
						score += 10;
					}
				}

				for (int placeIndex = 0; placeIndex < 5; placeIndex++)
				{
					if (scores[placeIndex] >= score)
					{
						continue;
					}

					for (int laterIndex = 3; laterIndex >= placeIndex; laterIndex--)
					{
						scores[laterIndex + 1] = scores[laterIndex];
						towns[laterIndex + 1] = towns[laterIndex];
					}

					scores[placeIndex] = score;
					towns[placeIndex] = townIndex;
					break;
				}
			}

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Cyan);

			this.Owner.TextPainter.DrawTextWithShadow(TopCitiesTitleStringAddress, 80, 12, Hues.White);

			short verticalLocation = 0x20;

			for (short placeIndex = 0; placeIndex < 5; placeIndex++)
			{
				if (scores[placeIndex] == -1)
				{
					continue;
				}

				short townIndex = towns[placeIndex];

				EmptyMessage()
					.Number((short)(placeIndex + 1))
					.Text(". ")
					.City(townIndex)
					.Text(" (")
					.Text(Tables.CivilizationName(this.Owner.World.Towns[townIndex].ParticipantIndex))
					.Text(")");

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, verticalLocation + 3,
					Tables.PlayerColour(this.Owner.World.Towns[townIndex].ParticipantIndex));

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				this.Owner.CityOperations.DrawCitizenIcons(townIndex, 48, (short)(verticalLocation + 10), CityTurn.Specialists, 160);

				short iconHorizontal = (short)(0x30 + this.Owner.Helpers.ClampValue(this.Owner.World.Towns[townIndex].TrueExtent * 8, 0, 160) + 8);

				for (int wonderIndex = 1; wonderIndex <= 0x15; wonderIndex++)
				{
					if (this.Owner.World.MarvelTownIndex[wonderIndex] != townIndex)
					{
						continue;
					}

					if (iconHorizontal < 0x125)
					{
						this.Owner.CityOperations.DrawResourceIcon((short)(wonderIndex + 24), iconHorizontal, (short)(verticalLocation + 12));
					}

					iconHorizontal += 0x13;
				}

				this.Owner.Dialogs.DrawBox(8, verticalLocation, 303, 25,
					Tables.PlayerColour(this.Owner.World.Towns[townIndex].ParticipantIndex));

				verticalLocation += 0x20;
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		private static ushort HallOfFameRecordAddress(int entryIndex)
		{
			return (ushort)(HallOfFameBaseAddress + (entryIndex * HallOfFameRecordSize));
		}

		private short FameScore(int entryIndex)
		{
			return this.Processor.PullSignedWord(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIndex) + OffsetScore));
		}

		private void SetFameScore(int entryIndex, short score)
		{
			this.Processor.EmitWord(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIndex) + OffsetScore), (ushort)score);
		}

		private ushort FameDifficulty(int entryIndex)
		{
			return this.Processor.PullWord(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIndex) + OffsetDifficulty));
		}

		private short FameYear(int entryIndex)
		{
			return (short)this.Processor.PullWord(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIndex) + OffsetYear));
		}

		private short FamePopulation(int entryIndex)
		{
			return this.Processor.PullSignedWord(HallOfFameSegment, (ushort)(HallOfFameRecordAddress(entryIndex) + OffsetPopulation));
		}

		private short DrawFameRecord(ushort placeIndex, ushort newPlace, short verticalLocation)
		{
			const short horizontalLocation = 8;
			ushort record = HallOfFameRecordAddress(placeIndex);

			this.Processor.SegmentExtra.Word = HallOfFameSegment;

			short score = FameScore(placeIndex);

			if (score <= 0)
			{
				this.Owner.Words.Set(" ---");

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation, verticalLocation, Hues.Black);

				return (short)(verticalLocation + 0x1a);
			}

			EmptyMessage()
				.Number((short)(placeIndex + 1))
				.Text(". ");

			this.Owner.Runtime.MoveFarData(HallOfFameSegment, (ushort)(record + OffsetLeaderName), this.Processor.SegmentData.Word, (ushort)(TextBuffer.Address + this.Owner.Words.Length), 16);

			this.Owner.Words.Append(", ");

			this.Processor.SegmentExtra.Word = HallOfFameSegment;
			this.Owner.Words.Append(this.Processor.PullGlobalWord((ushort)((FameDifficulty(placeIndex) << 1) + HallOfFameRulerTitlesTableAddress)));

			this.Owner.Words.Append(" of the ");

			this.Owner.Runtime.MoveFarData(HallOfFameSegment, (ushort)(record + OffsetCivilizationPlural), this.Processor.SegmentData.Word, (ushort)(TextBuffer.Address + this.Owner.Words.Length), 16);

			ushort length = this.Owner.Words.Length;

			this.Owner.Words.Append(" to ");

			this.Processor.SegmentExtra.Word = HallOfFameSegment;
			short year = FameYear(placeIndex);

			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(Math.Abs(year), 10));

			this.Owner.Words.Append(year < 0 ? HallOfFameBeforeChristEraStringAddress : HallOfFameAnnoDominiEraStringAddress);

			if ((short)((ushort)this.Owner.TextPainter.MeasureTextWidth(this.Owner.Words.ToString()) + horizontalLocation) >= 0x140)
			{
				this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0x0);
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation, verticalLocation, Hues.Black);

			verticalLocation += 8;

			this.Owner.Words.Set("Population: ");

			this.Processor.SegmentExtra.Word = HallOfFameSegment;
			this.Owner.Helpers.FormatPopulation(FamePopulation(placeIndex));

			this.Owner.Words.Append(", Score: ");

			this.Processor.SegmentExtra.Word = HallOfFameSegment;
			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(FameScore(placeIndex), 10));

			this.Processor.SegmentExtra.Word = HallOfFameSegment;
			ushort hardness = FameDifficulty(placeIndex);

			if (hardness <= 4)
			{
				this.Owner.Words.Append(DifficultyNameAddresses[hardness]);
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation + 0xc, verticalLocation, Hues.LightGray);

			verticalLocation += 8;

			this.Owner.Words.Set("    --- CIVILIZATION RATING: ");

			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)((((short)FameDifficulty(placeIndex) + 1) *
				(short)this.Processor.PullWord(this.Processor.SegmentExtra.Word, (ushort)(record + OffsetScore))) / 0x32), 10));

			this.Owner.Words.Append("% ---");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation + 0xc, verticalLocation, Hues.LightRed);

			verticalLocation += 10;

			if (placeIndex == newPlace)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizontalLocation - 4, verticalLocation - 28, 296, 26, Hues.White, Hues.Yellow);
			}

			return verticalLocation;
		}

		private void PullRankTitle(ushort offset)
		{
			this.Owner.Runtime.MoveFarData(RulerRankTitlesSegment, offset, this.Processor.SegmentData.Word, TextBuffer.Address, 0x20);
		}
	}
}
