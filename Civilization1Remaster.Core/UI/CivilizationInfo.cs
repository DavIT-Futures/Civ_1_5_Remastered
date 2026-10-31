using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 13.
	/// </summary>
	public class CivilizationInfo : GameModule
	{
		private const ushort CivilizationOverviewSummaryHeaderAddress = 0x46be;
		private const ushort CivilizationOverviewTechnologiesHeaderAddress = 0x4729;

		public CivilizationInfo(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The overview of a civilization (diagnostics): its name and picture, government, population, treasury and rates, the
		/// counts of units / cities / settlers / land, the strategy on each continent with an attack value, the relations with
		/// the others and its technologies, three a row (white the ones it found first), until the screen is full; after a key
		/// or a click the screen is put back.
		/// </summary>
		public void DisplayCivilizationOverview(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationOverview({participantIdx})");

			Player participant = this.owner.World.Participants[participantIdx];

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 1);

			this.owner.TextPainter.RenderTextToScreen0(CivilizationName(participantIdx), 128, 4, 15);

			this.owner.Services.RenderPictureToScreen(ScreenBox, 16, 4,
				SpritePicture(((participantIdx << 5) + 0x40)));

			Message("Government: ")
				.Text(GovernmentName(participant.RegimeKind));

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, 24, 15);

			this.owner.Words.Set("Population: ");

			this.owner.Helpers.ObtainPlayerPopulation(participantIdx);

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, 32, 15);

			Message("Treasury: $")
				.Number(participant.Gold)
				.Text(", Taxes/Science: ")
				.Number(participant.LevyRatio)
				.Text("/")
				.Number(participant.ResearchLevyRatio);

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 16, 40, 15);

			this.owner.TextPainter.RenderTextToScreen0(CivilizationOverviewSummaryHeaderAddress, 16, 50, 15);

			this.owner.Words.Clear();

			foreach (short tally in new[] { participant.TroopTally, participant.TownTally, participant.ColonistTally, participant.SoilTally })
			{
				this.owner.Words.Append(this.owner.Libc.IntegerToText(tally, 10));

				this.owner.Words.Append("/");
			}

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 192, 48, 15);

			short vertLoc = 4;

			for (short continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent = participant.Landmasses[continentIdx];

				if (continent.Assault == 0)
					continue;

				this.owner.Words.Set("Settle");

				if (continent.Approach == 2)
				{
					this.owner.Words.Set("Defend");
				}

				if (continent.Approach == 1)
				{
					this.owner.Words.Set("Attack");
				}

				if (continent.Approach == 5)
				{
					this.owner.Words.Set("Transport");
				}

				this.owner.Words.Append("/");

				this.owner.Words.Append(this.owner.Libc.IntegerToText(continentIdx, 10));

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 256, vertLoc, 7);

				vertLoc += 8;
			}

			vertLoc = 0x18;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == participantIdx)
					continue;

				ushort relation = participant.Relations[otherIdx];

				this.owner.Words.Set(CivilizationName(otherIdx));

				if ((relation & 0xb) == 1)
				{
					this.owner.Words.Append(": Contact");
				}

				if ((relation & 2) != 0)
				{
					this.owner.Words.Append((relation & 4) != 0 ? ": Allied" : ": Peace");
				}

				if ((participant.Relations[otherIdx] & 8) != 0)
				{
					this.owner.Words.Append(": Vendetta");
				}

				if ((participant.Relations[otherIdx] & 0xb) == 0)
					continue;

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 192, vertLoc, 7);

				vertLoc += 8;
			}

			vertLoc = 0x38;

			this.owner.TextPainter.RenderTextToScreen0(CivilizationOverviewTechnologiesHeaderAddress, 16, 56, 15);

			vertLoc += 8;

			short shown = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
					continue;

				this.owner.Words.Set(TechnologyText(scienceIdx));

				ushort hue = (ushort)(((byte)this.owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) == participantIdx ? 0xf : 0x7);

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), ((shown % 3) * 100) + 8, vertLoc, hue);

				shown++;

				if (shown % 3 == 0)
				{
					vertLoc += 8;
				}

				if (vertLoc > 0xc0)
					break;
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

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

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Dialogs.FloodBoxWithDoubleShadow(0, 0, 317, 197, 1);

			int rowIdx = 0;
			short vertLoc = 0x12;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				if (!AnyPlayerHasUnits(kind))
					continue;

				this.owner.VideoCard.RenderLine(ScreenBox, 36, vertLoc - 1, 319, vertLoc - 1, 9);

				this.owner.Services.RenderPictureToScreen(ScreenBox, ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					SpritePicture((kind + 0x60)));

				rowIdx++;

				this.owner.TextPainter.RenderTextToScreen0(UnitText(kind), 36, vertLoc, 15);

				vertLoc += 8;
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				EmptyMessage()
					.Number(this.owner.World.Participants[participantIdx].Standing);

				if (MapRevealed != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), (participantIdx * 30) + 84, 1, 15);
				}

				this.owner.Services.RenderPictureToScreen(ScreenBox, (30 * participantIdx) + 90, 2,
					SpritePicture(((participantIdx << 5) + 0x40)));

				if (participantIdx != this.owner.World.PersonParticipantIdx && MapRevealed == 0)
					continue;

				vertLoc = 0x12;

				for (int kind = 0; kind < 0x1c; kind++)
				{
					if (!AnyPlayerHasUnits(kind))
						continue;

					Player participant = this.owner.World.Participants[participantIdx];

					if (participant.LiveTroops[kind] != 0 || participant.TroopsInsideMaking[kind] != 0)
					{
						Message("")
							.Number(participant.LiveTroops[kind])
							.Text("/")
							.Number(participant.TroopsInsideMaking[kind]);
						this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), (participantIdx * 30) + 88, vertLoc, 15);
					}

					vertLoc += 8;
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

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

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 4);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort name = CivilizationName(participantIdx);
				ushort hue = PlayerColour(participantIdx);

				this.owner.TextPainter.RenderTextWithShadow(name, participantIdx * 40, 8, hue);

				this.owner.Services.RenderPictureToScreen(ScreenBox, (short)(0x28 * participantIdx), 16,
					SpritePicture(((participantIdx << 5) + 0x40)));

				this.owner.TextPainter.RenderTextWithShadow(name, 4, (participantIdx * 12) + 32, hue);
			}

			short personIdx = this.owner.World.PersonParticipantIdx;

			for (int rowIdx = 1; rowIdx < 8; rowIdx++)
			{
				for (int columnIdx = 1; columnIdx < 8; columnIdx++)
				{
					if (rowIdx == columnIdx)
						continue;

					if (MapRevealed == 0 && rowIdx != personIdx && columnIdx != personIdx)
						continue;

					EmptyMessage()
						.Number(this.owner.World.Participants[rowIdx].TroopsRuined[columnIdx])
						.Text("/")
						.Number(this.owner.World.Participants[columnIdx].TroopsRuined[rowIdx]);

					this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), rowIdx * 40, (columnIdx * 12) + 32,
						PlayerColour(rowIdx));
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("RenderScoreListRecord");
		}
	}
}
