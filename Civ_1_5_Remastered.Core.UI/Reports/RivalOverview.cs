namespace Civ_1_5_Remastered.Core.UI.Reports
{
	public class RivalOverview : GameComponent, IRivalOverview
	{
		private static readonly GameText CivilizationOverviewSummaryHeaderAddress = new(0x46be, "/Units/Cities/Settlers/Land");
		private static readonly GameText CivilizationOverviewTechnologiesHeaderAddress = new(0x4729, "Civilization:");

		public RivalOverview(IGameEngine owner)
			: base(owner)
		{
		}

		public void ShowCivilizationOverview(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilizationOverview({participantIndex})");

			Player participant = this.Owner.World.Participants[participantIndex];

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Blue);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(Tables.CivilizationName(participantIndex), 128, 4, Hues.White);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, 16, 4,
				Tables.SpritePicture((participantIndex << 5) + 0x40));

			Message("Government: ")
				.Text(Tables.GovernmentName(participant.RegimeKind));

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, 24, Hues.White);

			this.Owner.Words.Set("Population: ");

			this.Owner.Helpers.ComputePlayerPopulation(participantIndex);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, 32, Hues.White);

			Message("Treasury: $")
				.Number(participant.Gold)
				.Text(", Taxes/Science: ")
				.Number(participant.LevyRatio)
				.Text("/")
				.Number(participant.ResearchLevyRatio);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 16, 40, Hues.White);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(CivilizationOverviewSummaryHeaderAddress, 16, 50, Hues.White);

			this.Owner.Words.Clear();

			foreach (short tally in new[] { participant.TroopTally, participant.TownTally, participant.ColonistTally, participant.SoilTally })
			{
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(tally, 10));

				this.Owner.Words.Append("/");
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 192, 48, Hues.White);

			short verticalLocation = 4;

			for (short continentIndex = 1; continentIndex < 0xf; continentIndex++)
			{
				PlayerContinent continent = participant.Landmasses[continentIndex];

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

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(continentIndex, 10));

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 256, verticalLocation, Hues.LightGray);

				verticalLocation += 8;
			}

			verticalLocation = 0x18;

			for (short otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex == participantIndex)
				{
					continue;
				}

				ushort relation = participant.Relations[otherIndex];

				this.Owner.Words.Set(Tables.CivilizationName(otherIndex));

				if ((relation & 0xb) == 1)
				{
					this.Owner.Words.Append(": Contact");
				}

				if ((relation & 2) != 0)
				{
					this.Owner.Words.Append((relation & 4) != 0 ? ": Allied" : ": Peace");
				}

				if ((participant.Relations[otherIndex] & 8) != 0)
				{
					this.Owner.Words.Append(": Vendetta");
				}

				if ((participant.Relations[otherIndex] & 0xb) == 0)
				{
					continue;
				}

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 192, verticalLocation, Hues.LightGray);

				verticalLocation += 8;
			}

			verticalLocation = 0x38;

			this.Owner.TextPainter.DrawTextToPrimaryScreen(CivilizationOverviewTechnologiesHeaderAddress, 16, 56, Hues.White);

			verticalLocation += 8;

			short shown = 0;

			for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) == 0)
				{
					continue;
				}

				this.Owner.Words.Set(GameTables.TechnologyText(scienceIndex));

				ushort hue = (ushort)(((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIndex] & 7) == participantIndex ? 0xf : 0x7);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), ((shown % 3) * 100) + 8, verticalLocation, hue);

				shown++;

				if (shown % 3 == 0)
				{
					verticalLocation += 8;
				}

				if (verticalLocation > 0xc0)
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

			int rowIndex = 0;
			short verticalLocation = 0x12;

			for (int kind = 0; kind < 0x1c; kind++)
			{
				if (!AnyPlayerHasUnits(kind))
				{
					continue;
				}

				this.Owner.VideoCard.RenderLine(Screen.Box, 36, verticalLocation - 1, 319, verticalLocation - 1, Hues.LightBlue);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, ((rowIndex & 1) << 4) + 2, verticalLocation - 4,
					Tables.SpritePicture(kind + 0x60));

				rowIndex++;

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTables.UnitText(kind), 36, verticalLocation, Hues.White);

				verticalLocation += 8;
			}

			for (short participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				EmptyMessage()
					.Number(this.Owner.World.Participants[participantIndex].Standing);

				if (MapView.Revealed != 0)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), (participantIndex * 30) + 84, 1, Hues.White);
				}

				this.Owner.Services.DrawPictureToScreen(Screen.Box, (30 * participantIndex) + 90, 2,
					Tables.SpritePicture((participantIndex << 5) + 0x40));

				if (participantIndex != this.Owner.World.PersonParticipantIndex && MapView.Revealed == 0)
				{
					continue;
				}

				verticalLocation = 0x12;

				for (int kind = 0; kind < 0x1c; kind++)
				{
					if (!AnyPlayerHasUnits(kind))
					{
						continue;
					}

					Player participant = this.Owner.World.Participants[participantIndex];

					if (participant.LiveTroops[kind] != 0 || participant.TroopsInsideMaking[kind] != 0)
					{
						Message(string.Empty)
							.Number(participant.LiveTroops[kind])
							.Text("/")
							.Number(participant.TroopsInsideMaking[kind]);
						this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), (participantIndex * 30) + 88, verticalLocation, Hues.White);
					}

					verticalLocation += 8;
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

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				ushort name = Tables.CivilizationName(participantIndex);
				ushort hue = Tables.PlayerColour(participantIndex);

				this.Owner.TextPainter.DrawTextWithShadow(name, participantIndex * 40, 8, hue);

				this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)(0x28 * participantIndex), 16,
					Tables.SpritePicture((participantIndex << 5) + 0x40));

				this.Owner.TextPainter.DrawTextWithShadow(name, 4, (participantIndex * 12) + 32, hue);
			}

			short personIndex = this.Owner.World.PersonParticipantIndex;

			for (int rowIndex = 1; rowIndex < 8; rowIndex++)
			{
				for (int columnIndex = 1; columnIndex < 8; columnIndex++)
				{
					if (rowIndex == columnIndex)
					{
						continue;
					}

					if (MapView.Revealed == 0 && rowIndex != personIndex && columnIndex != personIndex)
					{
						continue;
					}

					EmptyMessage()
						.Number(this.Owner.World.Participants[rowIndex].TroopsRuined[columnIndex])
						.Text("/")
						.Number(this.Owner.World.Participants[columnIndex].TroopsRuined[rowIndex]);

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), rowIndex * 40, (columnIndex * 12) + 32,
						Tables.PlayerColour(rowIndex));
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		private bool AnyPlayerHasUnits(int kind)
		{
			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (this.Owner.World.Participants[participantIndex].LiveTroops[kind] != 0 ||
					this.Owner.World.Participants[participantIndex].TroopsInsideMaking[kind] != 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}
