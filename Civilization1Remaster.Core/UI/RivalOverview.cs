namespace Civilization1Remaster.Core.UI
{
	public class RivalOverview : GameComponent, IRivalOverview
	{
		private static readonly GameText CivilizationOverviewSummaryHeaderAddress = new(0x46be, "/Units/Cities/Settlers/Land");
		private static readonly GameText CivilizationOverviewTechnologiesHeaderAddress = new(0x4729, "Civilization:");

		public RivalOverview(GameEngine owner)
			: base(owner)
		{
		}

		public void ShowCivilizationOverview(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilizationOverview({participantIdx})");

			Player participant = this.Owner.World.Participants[participantIdx];

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Blue);

			this.Owner.TextPainter.DrawTextToScreen0(Tables.CivilizationName(participantIdx), 128, 4, Hues.White);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, 16, 4,
				Tables.SpritePicture((participantIdx << 5) + 0x40));

			Message("Government: ")
				.Text(Tables.GovernmentName(participant.RegimeKind));

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, 24, Hues.White);

			this.Owner.Words.Set("Population: ");

			this.Owner.Helpers.ComputePlayerPopulation(participantIdx);

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, 32, Hues.White);

			Message("Treasury: $")
				.Number(participant.Gold)
				.Text(", Taxes/Science: ")
				.Number(participant.LevyRatio)
				.Text("/")
				.Number(participant.ResearchLevyRatio);

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 16, 40, Hues.White);

			this.Owner.TextPainter.DrawTextToScreen0(CivilizationOverviewSummaryHeaderAddress, 16, 50, Hues.White);

			this.Owner.Words.Clear();

			foreach (short tally in new[] { participant.TroopTally, participant.TownTally, participant.ColonistTally, participant.SoilTally })
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(tally, 10));

				this.Owner.Words.Append("/");
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 192, 48, Hues.White);

			short vertLoc = 4;

			for (short continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent = participant.Landmasses[continentIdx];

				if (continent.Assault == 0)
				{
					continue;
				}

				this.Owner.Words.Set("Settle");

				if (continent.Approach == 2)
				{
					this.Owner.Words.Set("Defend");
				}

				if (continent.Approach == 1)
				{
					this.Owner.Words.Set("Attack");
				}

				if (continent.Approach == 5)
				{
					this.Owner.Words.Set("Transport");
				}

				this.Owner.Words.Append("/");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(continentIdx, 10));

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 256, vertLoc, Hues.LightGray);

				vertLoc += 8;
			}

			vertLoc = 0x18;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == participantIdx)
				{
					continue;
				}

				ushort relation = participant.Relations[otherIdx];

				this.Owner.Words.Set(Tables.CivilizationName(otherIdx));

				if ((relation & 0xb) == 1)
				{
					this.Owner.Words.Append(": Contact");
				}

				if ((relation & 2) != 0)
				{
					this.Owner.Words.Append((relation & 4) != 0 ? ": Allied" : ": Peace");
				}

				if ((participant.Relations[otherIdx] & 8) != 0)
				{
					this.Owner.Words.Append(": Vendetta");
				}

				if ((participant.Relations[otherIdx] & 0xb) == 0)
				{
					continue;
				}

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 192, vertLoc, Hues.LightGray);

				vertLoc += 8;
			}

			vertLoc = 0x38;

			this.Owner.TextPainter.DrawTextToScreen0(CivilizationOverviewTechnologiesHeaderAddress, 16, 56, Hues.White);

			vertLoc += 8;

			short shown = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
				{
					continue;
				}

				this.Owner.Words.Set(GameTables.TechnologyText(scienceIdx));

				ushort hue = (ushort)(((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) == participantIdx ? 0xf : 0x7);

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), ((shown % 3) * 100) + 8, vertLoc, hue);

				shown++;

				if (shown % 3 == 0)
				{
					vertLoc += 8;
				}

				if (vertLoc > 0xc0)
				{
					break;
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowCivilizationScoreList()
		{
			using var _ = this.Processor.Journal.Scope("DisplayCivilizationScoreList()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Dialogs.FillBoxWithDoubleShadow(0, 0, 317, 197, Hues.Blue);

			int rowIdx = 0;
			short vertLoc = 0x12;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				if (!AnyPlayerHasUnits(kind))
				{
					continue;
				}

				this.Owner.VideoCard.RenderLine(Screen.Box, 36, vertLoc - 1, 319, vertLoc - 1, Hues.LightBlue);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, ((rowIdx & 1) << 4) + 2, vertLoc - 4,
					Tables.SpritePicture(kind + 0x60));

				rowIdx++;

				this.Owner.TextPainter.DrawTextToScreen0(GameTables.UnitText(kind), 36, vertLoc, Hues.White);

				vertLoc += 8;
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				EmptyMessage()
					.Number(this.Owner.World.Participants[participantIdx].Standing);

				if (MapView.Revealed != 0)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), (participantIdx * 30) + 84, 1, Hues.White);
				}

				this.Owner.Services.DrawPictureToScreen(Screen.Box, (30 * participantIdx) + 90, 2,
					Tables.SpritePicture((participantIdx << 5) + 0x40));

				if (participantIdx != this.Owner.World.PersonParticipantIdx && MapView.Revealed == 0)
				{
					continue;
				}

				vertLoc = 0x12;

				for (int kind = 0; kind < 0x1c; kind++)
				{
					if (!AnyPlayerHasUnits(kind))
					{
						continue;
					}

					Player participant = this.Owner.World.Participants[participantIdx];

					if (participant.LiveTroops[kind] != 0 || participant.TroopsInsideMaking[kind] != 0)
					{
						Message(string.Empty)
							.Number(participant.LiveTroops[kind])
							.Text("/")
							.Number(participant.TroopsInsideMaking[kind]);
						this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), (participantIdx * 30) + 88, vertLoc, Hues.White);
					}

					vertLoc += 8;
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			DrawScoreListRecord();
		}

		public void DrawScoreListRecord()
		{
			using var _ = this.Processor.Journal.Scope("RenderScoreListRecord()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Red);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort name = Tables.CivilizationName(participantIdx);
				ushort hue = Tables.PlayerColour(participantIdx);

				this.Owner.TextPainter.DrawTextWithShadow(name, participantIdx * 40, 8, hue);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)(0x28 * participantIdx), 16,
					Tables.SpritePicture((participantIdx << 5) + 0x40));

				this.Owner.TextPainter.DrawTextWithShadow(name, 4, (participantIdx * 12) + 32, hue);
			}

			short personIdx = this.Owner.World.PersonParticipantIdx;

			for (int rowIdx = 1; rowIdx < 8; rowIdx++)
			{
				for (int columnIdx = 1; columnIdx < 8; columnIdx++)
				{
					if (rowIdx == columnIdx)
					{
						continue;
					}

					if (MapView.Revealed == 0 && rowIdx != personIdx && columnIdx != personIdx)
					{
						continue;
					}

					EmptyMessage()
						.Number(this.Owner.World.Participants[rowIdx].TroopsRuined[columnIdx])
						.Text("/")
						.Number(this.Owner.World.Participants[columnIdx].TroopsRuined[rowIdx]);

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), rowIdx * 40, (columnIdx * 12) + 32,
						Tables.PlayerColour(rowIdx));
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		private bool AnyPlayerHasUnits(int kind)
		{
			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (this.Owner.World.Participants[participantIdx].LiveTroops[kind] != 0 ||
					this.Owner.World.Participants[participantIdx].TroopsInsideMaking[kind] != 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}
