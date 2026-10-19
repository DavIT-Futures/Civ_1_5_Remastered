using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 13.
	/// </summary>
	public class CivilizationInfo : GameModule
	{
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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 1);

			this.owner.TextPainter.RenderTextToScreen0(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)), 128, 4, 15);

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), 16, 4,
				this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

			Message("Government: ")
				.Text(this.processor.PullGlobalWord((ushort)((participant.RegimeKind << 1) + 0x1966)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, 24, 15);

			this.owner.Libc.CopyText(0xba06, "Population: ");

			this.owner.Helpers.ObtainPlayerPopulation(participantIdx);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, 32, 15);

			Message("Treasury: $")
				.Number(participant.Gold)
				.Text(", Taxes/Science: ")
				.Number(participant.LevyRatio)
				.Text("/")
				.Number(participant.ResearchLevyRatio);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, 40, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x46be, 16, 50, 15);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			foreach (short tally in new[] { participant.TroopTally, participant.TownTally, participant.ColonistTally, participant.SoilTally })
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(tally, 10));

				this.owner.Libc.AppendText(0xba06, "/");
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 192, 48, 15);

			short vertLoc = 4;

			for (short continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent = participant.Landmasses[continentIdx];

				if (continent.Assault == 0)
					continue;

				this.owner.Libc.CopyText(0xba06, "Settle");

				if (continent.Approach == 2)
				{
					this.owner.Libc.CopyText(0xba06, "Defend");
				}

				if (continent.Approach == 1)
				{
					this.owner.Libc.CopyText(0xba06, "Attack");
				}

				if (continent.Approach == 5)
				{
					this.owner.Libc.CopyText(0xba06, "Transport");
				}

				this.owner.Libc.AppendText(0xba06, "/");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(continentIdx, 10));

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 256, vertLoc, 7);

				vertLoc += 8;
			}

			vertLoc = 0x18;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == participantIdx)
					continue;

				ushort relation = participant.Relations[otherIdx];

				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((otherIdx << 1) + 0x1982)));

				if ((relation & 0xb) == 1)
				{
					this.owner.Libc.AppendText(0xba06, ": Contact");
				}

				if ((relation & 2) != 0)
				{
					this.owner.Libc.AppendText(0xba06, (relation & 4) != 0 ? ": Allied" : ": Peace");
				}

				if ((participant.Relations[otherIdx] & 8) != 0)
				{
					this.owner.Libc.AppendText(0xba06, ": Vendetta");
				}

				if ((participant.Relations[otherIdx] & 0xb) == 0)
					continue;

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 192, vertLoc, 7);

				vertLoc += 8;
			}

			vertLoc = 0x38;

			this.owner.TextPainter.RenderTextToScreen0(0x4729, 16, 56, 15);

			vertLoc += 8;

			short shown = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
					continue;

				this.owner.Libc.CopyText(0xba06, (ushort)((0x16 * scienceIdx) + 0x4da));

				ushort hue = (ushort)(((byte)this.owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) == participantIdx ? 0xf : 0x7);

				this.owner.TextPainter.RenderTextToScreen0(0xba06, ((shown % 3) * 100) + 8, vertLoc, hue);

				shown++;

				if (shown % 3 == 0)
				{
					vertLoc += 8;
				}

				if (vertLoc > 0xc0)
					break;
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

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
				EmptyMessage()
					.Number(this.owner.World.Participants[participantIdx].Standing);

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
						Message("")
							.Number(participant.LiveTroops[kind])
							.Text("/")
							.Number(participant.TroopsInsideMaking[kind]);
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

					EmptyMessage()
						.Number(this.owner.World.Participants[rowIdx].TroopsRuined[columnIdx])
						.Text("/")
						.Number(this.owner.World.Participants[columnIdx].TroopsRuined[rowIdx]);

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
