namespace Civilization1Remaster.Core.Game
{
	public partial class WorldEvents
	{
		public void HandleGameEnd()
		{
			using var _ = this.Processor.Journal.Scope("HandleGameEnd()");

			if (PlayTurnOfPlayers(out short startGold))
			{
				FinishTurn(startGold);
			}
		}

		public void ShowEndGameSequence()
		{
			using var _ = this.Processor.Journal.Scope("DisplayEndGameSequence()");

			this.Owner.Atlas.ShowCivilizationGraph();

			if (Turn.GameEndRequest != 2)
			{
				this.Owner.Replayer.ShowReplay();
			}

			this.Owner.Startup.HideMouseCursor();

			if ((this.Owner.World.StarshipMarkers & 0x100) == 0)
			{
				this.Owner.Legends.ShowGameResultsScreen((ushort)((this.Owner.World.CastleTier >> 1) - 1),
					this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIndex, true));

				this.Owner.Legends.LoadHallOfFame();

				this.Owner.Legends.AttachHallOfFameRecord();

				this.Owner.Legends.SaveHallOfFame();
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		private bool ChooseGameEnd()
		{
			Spaceship.ArrivedSpaceship = 0xffff;

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if ((this.Owner.World.StarshipMarkers & (1 << participantIndex)) == 0)
				{
					continue;
				}

				if (this.Owner.World.Participants[participantIndex].StarshipArrivalSeason == this.Owner.World.Season)
				{
					Spaceship.ArrivedSpaceship = (ushort)participantIndex;
				}
			}

			if ((short)(0x820 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season)
			{
				Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];

				Message(Tables.GovernmentTitle(person.RegimeKind))
					.Text(" ")
					.Text(Tables.LeaderName(this.Owner.World.PersonParticipantIndex))
					.Text("\nplans retirement\nin 20 years.\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			return Spaceship.ArrivedSpaceship != 0xffff || Turn.ConquestEnd != 0 ||
				(short)(0x834 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season;
		}

		private void EndGame()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Words.Set("*** GAME OVER ***\n");

			if (Turn.ConquestEnd != 0)
			{
				this.Owner.Words.Set("Your civilization\nhas conquered\nthe entire planet!\n");

				Spaceship.ArrivedSpaceship = 0xffff;

				this.Owner.Paper.ShowNewspaper(-1);

				this.Owner.Replayer.ShowConquerorScreen();
			}

			if (Spaceship.ArrivedSpaceship != 0xffff)
			{
				this.Owner.Prologue.ShowCivilizationScreen((short)Spaceship.ArrivedSpaceship);

				RepaintMainScreen();

				Message(Tables.CivilizationName(Spaceship.ArrivedSpaceship))
					.Text(" spaceship\narrives at Alpha Centauri.\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			if ((short)(0x834 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season)
			{
				Message(Tables.LeaderName(this.Owner.World.PersonParticipantIndex))
					.Text(" dynasty\nends after glorious\n6000 year reign!\n");

				ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ChiefVisuals);

				this.Owner.Services.PerformSound(Tables.LeaderWordAt(leader, LeaderTable.Tune), 3);

				this.Owner.Paper.ShowNewspaper(-1);

				this.Owner.Services.PerformSound(1, 0);
			}

			this.Owner.Startup.ShowMouseCursor();

			if ((this.Owner.World.StarshipMarkers & 0x100) == 0)
			{
				ShowEndGameSequence();
			}

			RepaintMainScreen();

			if (Turn.ConquestEnd == 0)
			{
				this.Owner.Words.Clear();

				this.Owner.BlockText.ShowTextDialog(RetirementContinuePromptAddress);

				Turn.GameEndRequest = (ushort)(1 - ShowDialog(TextBuffer.Address, 80, 80));
			}
			else
			{
				Turn.GameEndRequest = 0x1;
			}

			if (Turn.GameEndRequest != 0)
			{
				ShowTitleScreen();
			}
		}
	}
}
