namespace Civilization1Remaster.Core.Game
{
	/// <summary>The end of the game: the choice of the end, the end sequence and the end itself</summary>
	public partial class WorldEvents
	{
		/// <inheritdoc/>
		public void HandleGameEnd()
		{
			using var _ = this.Processor.Journal.Scope("HandleGameEnd()");

			if (PlayTurnOfPlayers(out short startGold))
			{
				FinishTurn(startGold);
			}
		}

		/// <inheritdoc/>
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
					this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIdx, true));

				this.Owner.Legends.LoadHallOfFame();

				this.Owner.Legends.AttachHallOfFameRecord();

				this.Owner.Legends.SaveHallOfFame();
			}
		
			this.Owner.Startup.ShowMouseCursor();
		}

		/// <summary>
		/// The spaceship that arrived this year (0x1cf6, -1 none), the retirement news (20 years before the end year 2100 -
		/// 20 * difficulty); true when the game ends: a spaceship arrived, conquest (0xb884) or the end year
		/// </summary>
		private bool ChooseGameEnd()
		{
			Spaceship.ArrivedSpaceship = 0xffff;

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.StarshipMarkers & (1 << participantIdx)) == 0)
					continue;

				if (this.Owner.World.Participants[participantIdx].StarshipArrivalSeason == this.Owner.World.Season)
				{
					Spaceship.ArrivedSpaceship = (ushort)participantIdx;
				}
			}

			if ((short)(0x820 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season)
			{
				Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

				Message(Tables.GovernmentTitle(person.RegimeKind))
					.Text(" ")
					.Text(Tables.LeaderName(this.Owner.World.PersonParticipantIdx))
					.Text("\nplans retirement\nin 20 years.\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			return Spaceship.ArrivedSpaceship != 0xffff || Turn.ConquestEnd != 0 ||
				(short)(0x834 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season;
		}

		/// <summary>
		/// The end of the game: conquest, the spaceship arrival, the end of the reign (each with its news), the end sequence
		/// (not after a launched spaceship of the human player... bit 0x100), and the question to go on (not after conquest)
		/// </summary>
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
				Message(Tables.LeaderName(this.Owner.World.PersonParticipantIdx))
					.Text(" dynasty\nends after glorious\n6000 year reign!\n");

				ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals);

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

			// (the original sets bit 0 of the byte 0x4cd9 here: the emulated memory does not let the game touch 0x4cd8 - 0x4cd9,
			// so the write never happened, and nothing reads it)

			if (Turn.ConquestEnd == 0)
			{
				this.Owner.Words.Clear();

				this.Owner.BlockText.ShowTextDialog(RetirementContinuePromptAddress);

				// The first line goes on with the game, the second one ends it
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
