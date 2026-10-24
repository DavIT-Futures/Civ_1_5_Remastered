using System;
using System.Collections.Generic;
using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1238.
	/// </summary>
	public class TurnEvents : GameModule
	{
		public TurnEvents(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The healing before HealingRules: only fortified units, 1 hit point a turn. The golden master and differential tests
		/// turn it on (HeadlessGame), so they still compare with baselines recorded from the original code.
		/// </summary>
		public bool ClassicHealing { get; set; }

		/// <summary>
		/// Units heal lost hit points every turn (HealingRules): 2 in an own city; outside cities 1 for a fortified land unit
		/// and for a resting ship (sentry or not moved this turn) next to land, at most 3 tiles from an own city.
		/// Runs at the end of the game turn, so the moves left (reset at the start of each player's turn) show whether the
		/// unit moved. Reads the memory without journal entries or registers, like the rest of the hit points (host only).
		/// </summary>
		public void MendFortifiedUnits()
		{
			if (this.ClassicHealing)
			{
				MendOnlyFortifiedUnits();
				return;
			}

			UnitMemory troops = new UnitMemory(this.processor);

			for (int participantIdx = 0; participantIdx < UnitHealth.ParticipantTally; participantIdx++)
			{
				List<Point> ownTowns = new List<Point>();

				foreach (City town in this.owner.World.Towns)
				{
					if (town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx)
						ownTowns.Add(town.Place);
				}

				for (int troopIdx = 0; troopIdx < UnitHealth.TroopTally; troopIdx++)
				{
					int troopKind = troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if (troopKind == 0xff || this.owner.World.Vitals.ObtainLostHitPoints(participantIdx, troopIdx, troopKind) == 0)
						continue;

					int horizLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
					int vertLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
					int status = troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
					int landGroup = troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);
					bool inOwnTown = ownTowns.Contains(new Point(horizLoc, vertLoc));
					bool resting;
					bool nextToLand = false;
					int townDistance = int.MaxValue;

					if (landGroup == UnitKindTable.SeaUnits)
					{
						int fullSteps = 3 * troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally);

						resting = (status & (int)UnitStatusEnum.Sentry) != 0 ||
							troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= fullSteps;

						if (!inOwnTown)
						{
							townDistance = HealingRules.ComputeDistanceToNearestFriendlyCity(horizLoc, vertLoc, ownTowns);

							if (townDistance <= HealingRules.MaxShipCoastDistanceToCity)
							{
								nextToLand = HealingRules.IsAdjacentToLand(horizLoc, vertLoc,
									(tileHoriz, tileVert) => this.owner.MapOps.Chart.Terrain(tileHoriz, tileVert) == MapRules.OceanTerrain);
							}
						}
					}
					else
					{
						resting = (status & (int)UnitStatusEnum.Fortified) != 0;
					}

					int amount = HealingRules.CalculateHealAmount(landGroup, inOwnTown, resting, nextToLand, townDistance);

					this.owner.World.Vitals.MendAmount(participantIdx, troopIdx, troopKind, amount);
				}
			}
		}

		/// <summary>
		/// The healing before HealingRules (the golden master baselines have it): fortified units heal one lost hit point every
		/// turn, ships and planes never heal. Kept for the tests, see <see cref="ClassicHealing"/>.
		/// </summary>
		private void MendOnlyFortifiedUnits()
		{
			for (int participantIdx = 0; participantIdx < UnitHealth.ParticipantTally; participantIdx++)
			{
				for (int troopIdx = 0; troopIdx < UnitHealth.TroopTally; troopIdx++)
				{
					ushort troop = (ushort)(0x81d4 + 0x600 * participantIdx + 0xc * troopIdx);
					int troopKind = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(troop + 3));

					if (troopKind != 0xff && (this.processor.PullUInt8(this.processor.SegData.Value16, troop) & (int)UnitStatusEnum.Fortified) != 0)
					{
						this.owner.World.Vitals.MendOne(participantIdx, troopIdx, troopKind);
					}
				}
			}
		}

		/// <summary>
		/// Shows the text as a dialog (DisplayMenu) and puts back the screen as it was before (kept in the back screen 0x19d4);
		/// the answer of DisplayMenu given back (and left in AX)
		/// </summary>
		public ushort DisplayDialog(ushort charsAddr, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"DisplayDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 320, 200,
				BackScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			ushort answer = this.owner.Dialogs.DisplayMenu(charsAddr, horizLoc, vertLoc, 1);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 320, 200,
				ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = answer;

			this.processor.Journal.CloseScope("DisplayDialog");

			return answer;
		}

		/// <summary>
		/// One turn of the game: every living player in order (the cities, peace treaties, the AI, the human player's turn;
		/// after a loaded game 0xdf60 the players before the human player are skipped), the end of turn events, the year, the
		/// autosave, every 50 turns the status and the ranking into the replay, the chronicle, the scores of the wonders and
		/// the pollution, the palace, and the end of the game (a spaceship arrived, conquest, the year of retirement).
		/// The frame like the original: HandleCity (through HandleAllCities) reads the stack below it, and the ranking of the
		/// replay is made in the frame (bp - 0x14).
		/// </summary>
		public void HandleGameEnd()
		{
			this.processor.Journal.OpenScope("HandleGameEnd()");

			this.processor.EnterFrame(0x1a);
			this.processor.PreserveIndexRegisters();

			if (PlayTurnOfPlayers(out short startGold))
			{
				FinishTurn(startGold);
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("HandleGameEnd");
		}

		/// <summary>
		/// The start of the turn and the turns of the players; false when the game ended in it (0xdc48). startGold: the gold
		/// of the human player at the start of the turn.
		/// </summary>
		private bool PlayTurnOfPlayers(out short startGold)
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Helpers.LaunchHueCycling();

			short personIdx = this.owner.World.PersonParticipantIdx;

			startGold = this.owner.World.Participants[personIdx].Gold;

			this.processor.EmitGlobalWord(0xd760, 0);
			this.processor.EmitGlobalWord(0x6b92, 0);
			this.owner.World.HighestScienceTally = 0;
			this.processor.EmitGlobalWord(0xde20, 0);

			// The living players and the most technologies
			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				this.processor.EmitGlobalWord(0xde20, (ushort)(this.processor.PullGlobalWord(0xde20) + 1));

				short sciences = this.owner.World.Participants[participantIdx].FoundScienceTally;

				if (this.owner.World.HighestScienceTally < sciences)
				{
					this.owner.World.HighestScienceTally = sciences;
				}
			}

			if (this.owner.World.RoundTally == 0)
			{
				// The first turn: the view on the settler of the human player, the tutorial on the easiest level
				ushort troop = (ushort)(0x600 * personIdx);

				MapViewHoriz = (ushort)(this.processor.PullGlobalByte((ushort)(troop + 0x81d5)) - 7);
				MapViewVert = (ushort)(this.processor.PullGlobalByte((ushort)(troop + 0x81d6)) - 6);

				RepaintMainScreen();

				if (this.owner.World.HardnessTier == 0)
				{
					this.owner.TurnLogic.RenderUnitInfo(this.owner.World.PersonParticipantIdx, 0);

					this.owner.Prologue.DisplayInterfaceTutorial();

					RepaintMainScreen();
				}

				this.owner.Services.PerformSound(1, 0);
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.owner.World.Participants[participantIdx].Tally = 0;

				if ((this.owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				// After a loaded game the turn goes on at the human player
				if (participantIdx < this.owner.World.PersonParticipantIdx && this.processor.PullGlobalWord(0xdf60) != 0)
					continue;

				PlayTurnOfPlayer(participantIdx);

				if (GameEndRequest != 0 && this.processor.PullGlobalWord(0xb884) == 0)
				{
					if (GameEndRequest == 2)
					{
						DisplayEndGameSequence();

						DisplayTitleScreen();
					}

					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// The turn of one player (the busy light at the bottom left): its cities (not after a loaded game), at most 30000 gold,
		/// peace treaties, the AI; for the human player the status bar, the end without cities and settlers, the truce count
		/// (AD), then its own turn (unless the game ended)
		/// </summary>
		private void PlayTurnOfPlayer(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];
			bool person = participantIdx == this.owner.World.PersonParticipantIdx;

			this.owner.Services.FloodBox(ScreenBox, 2, 192, 6, 6, 8);

			if (person)
			{
				this.owner.Startup.DisplayMouseCursor();
			}

			if (this.processor.PullGlobalWord(0xdf60) == 0)
			{
				this.owner.Advisors.HandleAllCities(participantIdx);
			}

			if (player.Gold > 0x7530)
			{
				player.Gold = 0x7530;
			}

			this.owner.Politics.HandlePeaceTreaties(participantIdx);

			this.owner.Bot.OperateAITurn(participantIdx);

			this.owner.Bot.ReimposeStrategicLocations(participantIdx);

			this.owner.Services.FloodBox(ScreenBox, 2, 192, 6, 6, 15);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				RenderStatusBar();

				if (player.TownTally == 0 && player.ColonistTally == 0)
				{
					this.owner.Startup.ConcealMouseCursor();

					this.owner.Prologue.DisplayTerrainTutorial();

					GameEndRequest = 0xffff;

					this.owner.Startup.DisplayMouseCursor();
				}

				if (this.owner.World.Season > 0)
				{
					this.owner.World.TruceRoundTally++;
				}
			}

			if (GameEndRequest == 0)
			{
				this.owner.TurnLogic.HandlePlayerTurn(participantIdx);
			}

			this.processor.EmitGlobalWord(0xdf60, 0x0);

			if (person)
			{
				this.owner.Startup.ConcealMouseCursor();
			}
		}

		/// <summary>The end of the turn after the players</summary>
		private void FinishTurn(short startGold)
		{
			this.owner.Startup.DisplayMouseCursor();

			DisplayTravellersReport();

			HandleEndOfTurnEvents();

			if (this.owner.World.RoundTally > 50)
			{
				StrikeRandomCities();
			}

			RefreshPeaceGraphContent();

			WarnOfBankruptcy(startGold);

			AdvanceYear();

			MendFortifiedUnits();

			if (this.owner.World.PersonParticipantIdx != -1)
			{
				this.owner.SaveLoad.PerformBackup();
			}

			if (this.owner.World.RoundTally % 0x32 == 0 && this.owner.World.PersonParticipantIdx != -1)
			{
				NoteStatusInReplay();
			}

			if (this.owner.World.RoundTally >= this.owner.World.FollowingCollectionRound)
			{
				this.owner.World.FollowingCollectionRound = (short)(this.owner.Libc.Dice.DrawInt(40) + this.owner.World.RoundTally + 0x14);

				this.owner.Atlas.DisplayEpicChronicle();
			}

			ScoreWondersAndPollution();

			GrowPalace();

			RenderStatusBar();

			if (!ChooseGameEnd())
				return;

			EndGame();
		}

		/// <summary>
		/// Disasters for random cities of size 5 or more (not the last city of their owner); the loop counter is the city
		/// number + 1, like in the original, so it ends after the first city unless that is city 0
		/// </summary>
		private void StrikeRandomCities()
		{
			short counter = 0;

			while (counter < 2)
			{
				counter = (short)this.owner.Libc.Dice.DrawInt(128);

				City town = this.owner.World.Towns[counter];

				if (town.ConditionMarker != 0xff && this.owner.World.Participants[town.ParticipantIdx].TownTally > 1 && town.TrueExtent >= 5)
				{
					this.owner.Founding.HandleCityDisasters(counter);
				}

				counter++;
			}
		}

		/// <summary>
		/// The warning (text 0x1c2a) when the human player with more than one city and less than 100 gold would be broke at
		/// this rate (gold + 10 * the change of this turn below 0) and has the warning on (ParticipantMarkers)
		/// </summary>
		private void WarnOfBankruptcy(short startGold)
		{
			if (GameEndRequest != 0)
				return;

			Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

			if (person.TownTally <= 1 || person.Gold >= 100)
				return;

			if ((short)((10 * (person.Gold - startGold)) + person.Gold) >= 0)
				return;

			if (!this.owner.World.IsHuman(this.owner.World.PersonParticipantIdx))
				return;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x1c2a);

			DialogStyle = 0x4;

			DisplayDialog(0xba06, 80, 80);
		}

		/// <summary>
		/// The next turn and year: 20 years a turn until 1000 AD, 10 until 1500, 5 until 1750, 2 until 1850 (1 with a
		/// spaceship of a player other than 0), then 1; there is no year 0 (1 AD, the research doubled) and 20 AD is skipped
		/// </summary>
		private void AdvanceYear()
		{
			this.owner.World.RoundTally++;

			short season = this.owner.World.Season;

			this.owner.World.Season += season < 1000 ? (short)20 : season < 1500 ? (short)10 : season < 1750 ? (short)5 :
				season < 1850 && (this.owner.World.StarshipMarkers & 0xfe) == 0 ? (short)2 : (short)1;

			if (this.owner.World.Season == 0)
			{
				for (int participantIdx = 0; participantIdx < 8; participantIdx++)
				{
					this.owner.World.Participants[participantIdx].InquiryHeadway <<= 1;
				}

				this.owner.World.Season = 1;
			}

			if (this.owner.World.Season == 21)
			{
				this.owner.World.Season = 20;
			}
		}

		/// <summary>
		/// Every 50 turns: the most technologies, the technology level of the time (0xd2de, AD only), and into the replay the
		/// status of the human player (11: cities, population: the original takes a byte of the population as its high part,
		/// so that part is always 0) and the ranking (12: the players by their standing, two places a byte; the bytes are made
		/// in the frame and given in the reverse order, like in the original)
		/// </summary>
		private void NoteStatusInReplay()
		{
			this.owner.World.HighestScienceTally = 0;

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				short sciences = this.owner.World.Participants[participantIdx].FoundScienceTally;

				if (this.owner.World.HighestScienceTally < sciences)
				{
					this.owner.World.HighestScienceTally = sciences;
				}
			}

			TechnologyLevel = 0x0;

			if (this.owner.World.Season >= 0)
			{
				TechnologyLevel = (ushort)this.owner.Helpers.VerifyValueSpan(
					this.owner.World.HighestScienceTally - (this.owner.World.RoundTally / 9), 0, 6);
			}

			ushort population = this.owner.Helpers.ObtainPlayerPopulation(this.owner.World.PersonParticipantIdx);

			this.owner.UnitOps.AttachReplayContent(11, (byte)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TownTally,
				(byte)0, (byte)population);

			// The ranking in the frame (bp - 0x14, four words); a standing past 7 writes past them, like in the original
			for (int placeIdx = 0; placeIdx < 4; placeIdx++)
			{
				this.processor.EmitUInt16(this.processor.SegStack.Value16, RankingSlot(placeIdx << 1), 0x0);
			}

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				short standing = this.owner.World.Participants[participantIdx].Standing;
				ushort slot = RankingSlot((short)(standing / 2) << 1);
				ushort place = (ushort)((standing & 1) == 0 ? participantIdx : participantIdx << 4);

				this.processor.EmitUInt16(this.processor.SegStack.Value16, slot,
					(ushort)(this.processor.PullUInt16(this.processor.SegStack.Value16, slot) | place));
			}

			this.owner.UnitOps.AttachReplayContent(12, this.processor.PullLocalByte(0xe), this.processor.PullLocalByte(0x10),
				this.processor.PullLocalByte(0x12), this.processor.PullLocalByte(0x14));
		}

		/// <summary>The address of the ranking word at the byte offset in the frame (bp - 0x14)</summary>
		private ushort RankingSlot(int offset)
		{
			return (ushort)(this.processor.RegFrame.Value16 + offset - 0x14);
		}

		/// <summary>
		/// 25 points for every wonder in a city; the pollution costs the human player 10 points a polluted tile, unless the
		/// flag 0x3484 is -3 (then -2 for the next turn; -2 becomes 0 after counting)
		/// </summary>
		private void ScoreWondersAndPollution()
		{
			for (int marvelIdx = 1; marvelIdx <= 0x15; marvelIdx++)
			{
				ushort townIdx = (ushort)this.owner.World.MarvelTownIdx[marvelIdx];

				if (townIdx == 0xffff || townIdx == 0x80)
					continue;

				this.owner.World.Participants[this.owner.World.Towns[townIdx].ParticipantIdx].Tally += 25;
			}

			if (this.processor.PullGlobalWord(0x3484) == 0xfffd)
			{
				this.processor.EmitGlobalWord(0x3484, 0xfffe);
				return;
			}

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Tally -= (short)(10 * this.owner.World.DirtyCellTally);

			if (this.processor.PullGlobalWord(0x3484) == 0xfffe)
			{
				this.processor.EmitGlobalWord(0x3484, 0x0);
			}
		}

		/// <summary>
		/// The palace grows a level (DisplayPalace, at most 37) when the human player has a palace, the palace option (0x80) is
		/// on, the game is not over (spaceship bit 0x100) and the score reaches the next level
		/// </summary>
		private void GrowPalace()
		{
			bool palace = false;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.ParticipantIdx == this.owner.World.PersonParticipantIdx &&
					town.Has(CityStructure.Palace))
				{
					palace = true;
				}
			}

			if (!palace || (this.owner.World.SessionPrefMarkers & 0x80) == 0 || (this.owner.World.StarshipMarkers & 0x100) != 0)
				return;

			short needed = (short)ObtainPalaceScoreForLevel((short)(this.owner.World.CastleTier + 1));

			if ((short)this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, false) < needed)
				return;

			this.owner.World.CastleTier++;

			if (this.owner.World.CastleTier > 37)
				return;

			this.owner.Throne.DisplayPalace();
		}

		/// <summary>
		/// The spaceship that arrived this year (0x1cf6, -1 none), the retirement news (20 years before the end year 2100 -
		/// 20 * difficulty); true when the game ends: a spaceship arrived, conquest (0xb884) or the end year
		/// </summary>
		private bool ChooseGameEnd()
		{
			this.processor.EmitGlobalWord(0x1cf6, 0xffff);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.StarshipMarkers & (1 << participantIdx)) == 0)
					continue;

				if (this.owner.World.Participants[participantIdx].StarshipArrivalSeason == this.owner.World.Season)
				{
					this.processor.EmitGlobalWord(0x1cf6, (ushort)participantIdx);
				}
			}

			if ((short)(0x820 - (0x14 * this.owner.World.HardnessTier)) == this.owner.World.Season)
			{
				Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

				Message(this.processor.PullGlobalWord((ushort)((person.RegimeKind << 1) + 0x19b2)))
					.Text(" ")
					.Text(this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x19a2)))
					.Text("\nplans retirement\nin 20 years.\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			return this.processor.PullGlobalWord(0x1cf6) != 0xffff || this.processor.PullGlobalWord(0xb884) != 0 ||
				(short)(0x834 - (0x14 * this.owner.World.HardnessTier)) == this.owner.World.Season;
		}

		/// <summary>
		/// The end of the game: conquest, the spaceship arrival, the end of the reign (each with its news), the end sequence
		/// (not after a launched spaceship of the human player... bit 0x100), and the question to go on (not after conquest)
		/// </summary>
		private void EndGame()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Libc.CopyText(0xba06, "*** GAME OVER ***\n");

			if (this.processor.PullGlobalWord(0xb884) != 0)
			{
				this.owner.Libc.CopyText(0xba06, "Your civilization\nhas conquered\nthe entire planet!\n");

				this.processor.EmitGlobalWord(0x1cf6, 0xffff);

				this.owner.Paper.DisplayNewspaper(-1);

				this.owner.Replayer.DisplayConquerorScreen();
			}

			if (this.processor.PullGlobalWord(0x1cf6) != 0xffff)
			{
				this.owner.Prologue.DisplayCivilizationScreen((short)this.processor.PullGlobalWord(0x1cf6));

				RepaintMainScreen();

				Message(this.processor.PullGlobalWord((ushort)((this.processor.PullGlobalWord(0x1cf6) << 1) + 0x1982)))
					.Text(" spaceship\narrives at Alpha Centauri.\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			if ((short)(0x834 - (0x14 * this.owner.World.HardnessTier)) == this.owner.World.Season)
			{
				Message(this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x19a2)))
					.Text(" dynasty\nends after glorious\n6000 year reign!\n");

				ushort leader = (ushort)(0x3a * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);

				this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)(leader + 0x151a)), 3);

				this.owner.Paper.DisplayNewspaper(-1);

				this.owner.Services.PerformSound(1, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			if ((this.owner.World.StarshipMarkers & 0x100) == 0)
			{
				DisplayEndGameSequence();
			}

			RepaintMainScreen();

			this.processor.EmitGlobalByte(0x4cd9, (byte)(this.processor.PullGlobalByte(0x4cd9) | 0x1));

			if (this.processor.PullGlobalWord(0xb884) == 0)
			{
				this.processor.EmitGlobalByte(0xba06, 0x0);

				this.owner.BlockText.DisplayTextDialog(0x1cf0);

				// The first line goes on with the game, the second one ends it
				GameEndRequest = (ushort)(1 - DisplayDialog(0xba06, 80, 80));
			}
			else
			{
				GameEndRequest = 0x1;
			}

			if (GameEndRequest != 0)
			{
				DisplayTitleScreen();
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayEndGameSequence()
		{
			this.processor.Journal.OpenScope("DisplayEndGameSequence()");

			// function body
			this.owner.Atlas.DisplayCivilizationGraph();

			if (GameEndRequest != 2)
			{
				this.owner.Replayer.DisplayReplay();
			}

			this.owner.Startup.ConcealMouseCursor();

			if ((this.owner.World.StarshipMarkers & 0x100) == 0)
			{
				this.owner.Legends.DisplayGameResultsScreen((ushort)((this.owner.World.CastleTier >> 1) - 1),
					this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, true));

				this.owner.Legends.ImportHallOfFame();

				this.owner.Legends.AttachHallOfFameRecord();

				this.owner.Legends.StoreHallOfFame();
			}
		
			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("DisplayEndGameSequence");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayTitleScreen()
		{
			this.processor.Journal.OpenScope("DisplayTitleScreen()");

			// function body
			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(1, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1cf8, 0);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1d05, 1);

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			// Far return
			this.processor.Journal.CloseScope("DisplayTitleScreen");
		}

		/// <summary>
		/// Barbarians, from turn 30 * (5 - level): every 8th turn, while nobody knows technology 58, a landing ship (0x11, from
		/// turn 300 0x12) on an ocean tile nobody owns of a sea of at least 16 tiles with 1 - 3 units (3, with technology 56: 7)
		/// and a leader (0x1a), all on board (flag 1). Then a random city of the human player is attacked with a chance of its
		/// plunder %: up to 0x63 tries for a free land tile nobody owns, at least 6 from its nearest city and on its continent
		/// (the nearest city becomes the target; the occupation test asks for the player in the never written local 0x2 of the
		/// frame, like in the original); a human city the player sees gets the travellers' report, then size / 2 (2 - 99) units
		/// (4 / 9 or 6 / 10 with technology 35) and a leader land there, seen like the tile. The final values of the locals
		/// stay in the stack frame, like in the original.
		/// </summary>
		public void DisplayTravellersReport()
		{
			this.processor.Journal.OpenScope("DisplayTravellersReport()");

			this.processor.EnterFrame(0x1c);
			this.processor.PreserveIndexRegisters();

			if ((ushort)((ushort)(5 - (ushort)this.owner.World.HardnessTier) * 0x1e) <= this.owner.World.RoundTally)
			{
				if ((((byte)(this.owner.World.RoundTally & 0xff) + 1) & 7) == 0 && (this.owner.World.ScienceLeadingFoundVia[58] & 7) == 0)
				{
					LandBarbariansFromSea();
				}

				RaidHumanCity();
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayTravellersReport");
		}

		/// <summary>The landing ship of the barbarians with its units (see DisplayTravellersReport)</summary>
		private void LandBarbariansFromSea()
		{
			short horizLoc, vertLoc;

			do
			{
				horizLoc = (short)this.owner.Libc.Dice.DrawInt(80);
				this.processor.EmitLocalWord(0xe, (ushort)horizLoc);

				vertLoc = (short)(this.owner.Libc.Dice.DrawInt(44) + 3);
				this.processor.EmitLocalWord(0x12, (ushort)vertLoc);
			}
			while (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != 0xa ||
				this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff ||
				(short)this.owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 0x10);

			short units = (short)this.owner.Helpers.VerifyValueSpan((this.owner.World.RoundTally / 150) + 1, 1, 3);
			this.processor.EmitLocalWord(0x16, (ushort)units);

			this.owner.UnitOps.ConstructUnit(0, units < 3 ? (short)0x11 : (short)0x12, horizLoc, vertLoc);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.owner.World.ScienceLeadingFoundVia[56] & 7) != 0 ? (short)7 : (short)3;

				if (unitIdx == units)
					kind = 0x1a;

				this.processor.EmitLocalWord(0x4, (ushort)kind);

				ushort troopIdx = this.owner.UnitOps.ConstructUnit(0, kind, horizLoc, vertLoc);
				this.processor.EmitLocalWord(0x1c, troopIdx);

				ushort marker = (ushort)((ushort)(0xc * troopIdx) + 0x81d4);
				this.processor.EmitGlobalByte(marker, (byte)(this.processor.PullGlobalByte(marker) | 1));
			}

			this.processor.EmitLocalWord(0x14, (ushort)unitIdx);
		}

		/// <summary>The barbarians landing near a city of the human player (see DisplayTravellersReport)</summary>
		private void RaidHumanCity()
		{
			ushort townIdx = (ushort)this.owner.Libc.Dice.DrawInt(128);
			this.processor.EmitLocalWord(0x14, townIdx);

			if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff ||
				this.owner.World.Towns[townIdx].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				return;

			ushort plunder = this.owner.Conquest.ComputeCityPlunder((short)townIdx);
			this.processor.EmitLocalWord(0x16, plunder);

			if ((short)this.owner.Libc.Dice.DrawInt(100) >= (short)plunder)
				return;

			ushort tries = 0;
			short horizLoc, vertLoc;

			while (true)
			{
				int shift = this.owner.Libc.Dice.DrawInt(13);
				horizLoc = (short)((short)this.owner.World.Towns[townIdx].Place.X + shift - 6);
				this.processor.EmitLocalWord(0xe, (ushort)horizLoc);

				shift = this.owner.Libc.Dice.DrawInt(13);
				vertLoc = (short)((short)this.owner.World.Towns[townIdx].Place.Y + shift - 6);
				this.processor.EmitLocalWord(0x12, (ushort)vertLoc);

				townIdx = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
				this.processor.EmitLocalWord(0x14, townIdx);

				short distance = (short)(ushort)this.owner.Helpers.ObtainDistance(horizLoc, this.owner.World.Towns[townIdx].Place.X, vertLoc, this.owner.World.Towns[townIdx].Place.Y);
				this.processor.EmitLocalWord(0xc, (ushort)distance);

				tries++;
				this.processor.EmitLocalWord(0x1c, tries);

				if ((short)tries >= 0x63)
					return;

				if (distance < 6)
					continue;

				if (this.owner.UnitOps.VerifyTileOccupation(this.processor.PullLocalSignedWord(0x2), horizLoc, vertLoc) != 0)
					continue;

				if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa)
					continue;

				if (this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff)
					continue;

				ushort continent = this.owner.MapOps.ObtainContinentID(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

				if (this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc) == continent)
					break;
			}

			if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx &&
				((sbyte)this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (byte)this.owner.World.PersonParticipantIdx)) != 0)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
					this.owner.World.Towns[townIdx].Place.X - 8,
					this.owner.World.Towns[townIdx].Place.Y - 6);

				this.owner.Libc.CopyText(0xba06, "Travellers report:\n");

				if ((this.owner.World.ScienceLeadingFoundVia[35] & 7) != 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x1d26);
				}
				else if ((this.processor.PullUInt8(this.processor.SegExtra.Value16, 0x4f4) & 7) != 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x1d3f);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, 0x1d53);
				}

				this.owner.Conquest.ObtainCityLabel((short)townIdx);

				this.owner.Libc.AppendText(0xba06, "!\n");

				DialogStyle = 0x2;

				DisplayDialog(0xba06, 100, 32);
			}

			short units = (short)this.owner.Helpers.VerifyValueSpan(this.owner.Libc.Dice.DrawInt(this.owner.World.Towns[townIdx].TrueExtent / 2), 2, 99);
			this.processor.EmitLocalWord(0x16, (ushort)units);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.owner.World.ScienceLeadingFoundVia[35] & 7) != 0 ? ((unitIdx & 4) != 0 ? (short)9 : (short)4) :
					((unitIdx & 4) != 0 ? (short)0xa : (short)6);

				if (unitIdx == units)
					kind = 0x1a;

				this.processor.EmitLocalWord(0x4, (ushort)kind);

				ushort sight = (ushort)((ushort)(0xc * this.owner.UnitOps.ConstructUnit(0, kind, horizLoc, vertLoc)) + 0x81dd);
				this.processor.EmitGlobalByte(sight, (byte)(this.processor.PullGlobalByte(sight) | (byte)this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc]));

				if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				{
					this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
				}
			}

			this.processor.EmitLocalWord(0x18, (ushort)unitIdx);
		}

		/// <summary>
		/// The power of every player 1 - 7 (gold / 32 + 8 * citizens + technologies + the price of all units, 0 for a destroyed
		/// civilization): before turn 600 it goes (/ 8, at most 255) into the power graph (segment 0x3772) and the players' peace
		/// treaties into the peace graph (TrucePlotContent), both at 8 * (turn / 4 % 150) + player. The players are ranked by
		/// power (Standing 7 - 1). A human player first in power, with more than 4 cities, no spaceship part (unit 25) and after
		/// turn 200 is feared (0x8078): the others with more than one city make peace, or with a treaty get 0x100.
		/// </summary>
		public void RefreshPeaceGraphContent()
		{
			this.processor.Journal.OpenScope("RefreshPeaceGraphContent()");

			// The powers are in the stack frame, like in the original, and the final values of the locals stay there: when no
			// power is above -1, the ranking takes the player of the rank before (or the one left in the frame)
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveIndexRegisters();

			ushort powers = (ushort)(this.processor.RegFrame.Value16 - 0x16);
			short strongest = 0;
			ushort treaties = 0;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				Player participant = this.owner.World.Participants[participantIdx];
				ushort power = (ushort)(DivideByPowerOfTwo(participant.Gold, 5) + (participant.GrandTownExtent << 3) + participant.FoundScienceTally);

				for (int kind = 0; kind < 0x1c; kind++)
				{
					power += (ushort)(UnitKindTable.Field(this.processor, (sbyte)kind, UnitKindTable.Price) * participant.LiveTroops[kind]);
				}

				if (((1 << participantIdx) & this.owner.World.LiveRealms) == 0)
				{
					power = 0;
				}

				this.processor.EmitGlobalWord((ushort)(powers + (participantIdx << 1)), power);

				if (participantIdx != this.owner.World.PersonParticipantIdx && strongest < (short)power)
				{
					strongest = (short)power;
				}

				if (this.owner.World.RoundTally < 600)
				{
					// The power graph is in the segment 0x3772 (ES is left there, like in the original)
					this.processor.SegExtra.Value16 = 0x3772;
					this.processor.EmitUInt8(0x3772, GraphPlace(participantIdx), (byte)this.owner.Helpers.VerifyValueSpan((short)power / 8, 0, 255));
				}

				treaties = 0;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if ((participant.Relations[otherIdx] & 3) == 1)
					{
						treaties |= (ushort)(1 << otherIdx);
					}
				}

				if (this.owner.World.RoundTally < 600)
				{
					this.owner.World.TrucePlotContent[GraphPlace(participantIdx)] = (byte)treaties;
				}
			}

			ushort best = this.processor.PullLocalWord(0x1a);

			for (short rank = 1; rank < 8; rank++)
			{
				strongest = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short power = (short)this.processor.PullGlobalWord((ushort)(powers + (participantIdx << 1)));

					if (strongest < power)
					{
						best = (ushort)participantIdx;
						strongest = power;
					}
				}

				this.owner.World.Participants[best].Standing = (short)(8 - rank);
				this.processor.EmitGlobalWord((ushort)(powers + (best << 1)), 0xffff);
			}

			this.processor.EmitLocalWord(0x2, 0x8);
			this.processor.EmitLocalWord(0x4, (ushort)strongest);
			this.processor.EmitLocalWord(0x6, 0x8);
			this.processor.EmitLocalWord(0x18, treaties);
			this.processor.EmitLocalWord(0x1a, best);

			this.processor.EmitGlobalWord(0x8078, 0x0);

			Player human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

			if (human.Standing == 7 && human.TownTally > 4 && human.LiveTroops[25] == 0 && this.owner.World.RoundTally > 200)
			{
				this.processor.EmitGlobalWord(0x8078, 0x1);

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					if (participantIdx == this.owner.World.PersonParticipantIdx || this.owner.World.Participants[participantIdx].TownTally <= 1)
						continue;

					if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
					{
						this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 1;
					}
					else
					{
						this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 0x100;
					}
				}
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RefreshPeaceGraphContent");
		}

		/// <summary>The place of the player in the power and peace graphs: 8 * (turn / 4 % 150) + player</summary>
		private ushort GraphPlace(short participantIdx)
		{
			return (ushort)(((DivideByPowerOfTwo(this.owner.World.RoundTally, 2) % 150) << 3) + participantIdx);
		}

		/// <summary>Signed division toward zero by 2^shift of the original (-32768 gives a positive quotient)</summary>
		private static short DivideByPowerOfTwo(short amount, int shift)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> shift);

			return amount < 0 ? (short)-quotient : quotient;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void WipeStatusLine()
		{
			this.processor.Journal.OpenScope("WipeStatusLine()");

			// function body
			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 8, 0);

			this.owner.TextPainter.RenderTextToScreen0(0x1d6f, 8, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d75, 64, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d7d, 128, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d87, 192, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(0x1d8e, 240, 1, 15);

			// Far return
			this.processor.Journal.CloseScope("WipeStatusLine");
		}

		/// <summary>
		/// The status panel (not on the world map, 0xb278 = 1): peace bar, palace, free memory (with 0xd806), population, year,
		/// research bulb (progress * 4 / (cost + 1), 0 - 3), pollution sun, gold and the tax / luxury / science rates; then, while
		/// 0x1c28 is 0, tells when the population passes the next mark (0x1c24: tens below 100, then hundreds + 9). The stack
		/// frame, the saved SI, the return address pushed for ObtainFreeStorage and SI = the new mark before DisplayDialog stay
		/// like in the original (DisplayDialog leaves a value made from SI in DI).
		/// </summary>
		public void RenderStatusBar()
		{
			this.processor.Journal.OpenScope("RenderStatusBar()");

			this.processor.EnterFrame(0x8);
			this.processor.PreserveSource();

			if (WorldMapView != 1)
			{
				Player human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

				FloodBoxWithShadow(0, 58, 80, 39);

				this.owner.Services.FloodBox(ScreenBox, 3, 60, 74, 11, 11);

				if (this.owner.World.TruceRoundTally > 1)
				{
					int peace = this.owner.Helpers.VerifyValueSpan(this.owner.World.TruceRoundTally * 2, 0, 60);

					this.owner.Services.FloodBox(ScreenBox, 3, 60, (short)peace, 2, 15);
				}

				RenderPalace(this.owner.World.PersonParticipantIdx, this.owner.World.CastleTier, 8, 58);

				if (MapRevealed != 0)
				{
					this.processor.StackWord(0); // stack management - push return segment, ignored
					this.processor.StackWord(0x111e); // stack management - push return offset
					this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

					short storage = (short)this.owner.VideoCard.ObtainFreeStorage();

					EmptyMessage()
						.Number(storage);
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 59, 0);
				}

				this.processor.EmitGlobalByte(0xba06, 0);
				short population = (short)this.owner.Helpers.ObtainPlayerPopulation(this.owner.World.PersonParticipantIdx);
				this.owner.Libc.AppendText(0xba06, "#");

				if (population != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 73, 0);
				}

				this.processor.EmitGlobalByte(0xba06, 0);
				ObtainCurrentYearAsText();
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 81, 0);

				short cost = (short)(((this.owner.World.HardnessTier << 1) + TechnologyLevel + 6) *
					human.FoundScienceTally * (this.owner.World.Season < 0 ? 1 : 2));
				short progress = CPU.SignedQuotient((short)(human.InquiryHeadway << 2), (short)(cost + 1));
				int bulb = this.owner.Helpers.VerifyValueSpan(progress, 0, 3);

				this.owner.VideoCard.RenderImage(PictureScreenBox, (ushort)((bulb << 3) + 0xa0), 0x78, 8, 8,
					ScreenBox, 0x30, 0x50);

				if (this.owner.World.SmogImpactTier != 0)
				{
					int smog = this.owner.Helpers.VerifyValueSpan(this.owner.World.SmogImpactTier / 4, 0, 3);

					this.owner.VideoCard.RenderImage(PictureScreenBox, (ushort)((smog << 3) + 0xa0), 0x80, 8, 8,
						ScreenBox, 0x38, 0x50);
				}

				EmptyMessage()
					.Number(human.Gold)
					.Text("$")
					.Text(" ")
					.Number((short)(-(human.LevyRatio + human.ResearchLevyRatio - 10)))
					.Text(".")
					.Number(human.LevyRatio)
					.Text(".")
					.Number(human.ResearchLevyRatio);
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, 89, 0);

				if (this.processor.PullGlobalWord(0x1c28) == 0)
				{
					AnnouncePopulationMark(population);
				}
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("RenderStatusBar");
		}

		/// <summary>
		/// The population mark of RenderStatusBar: when the population passes the next mark 0x1c24 (in tens below 100, then in
		/// hundreds with 9 added), the mark moves and, unless 0x3484 is below -1, a dialog tells "...exceeds N00,000 citizens"
		/// </summary>
		private void AnnouncePopulationMark(short population)
		{
			short previousMark = (short)this.processor.PullGlobalWord(0x1c24);

			Message("The population of the\nfertile ")
				.Text(this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1982)))
				.Text(" empire now\nexceeds ");

			if (population < 100)
			{
				if ((short)(this.processor.PullGlobalWord(0x1c24) * 10) < population)
				{
					short mark = (short)(population / 10);
					this.processor.RegSource.Value16 = (ushort)mark;

					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(mark, 10));

					this.processor.EmitGlobalWord(0x1c24, (ushort)mark);
				}
			}
			else if ((short)((this.processor.PullGlobalWord(0x1c24) - 9) * 100) < population)
			{
				short hundreds = (short)(population / 100);
				this.processor.RegSource.Value16 = (ushort)hundreds;

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(hundreds, 10));
				this.owner.Libc.AppendText(0xba06, ",0");

				this.processor.EmitGlobalWord(0x1c24, (ushort)(hundreds + 9));
			}

			if (previousMark < (short)this.processor.PullGlobalWord(0x1c24) && (short)this.processor.PullGlobalWord(0x3484) >= -1)
			{
				this.owner.Libc.AppendText(0xba06, "00,000 citizens.\n");

				DialogStyle = 4;

				DisplayDialog(0xba06, 100, 80);
			}
		}

		/// <summary>
		/// The small palace of a player with cities, on a line at (horizLoc, vertLoc): the human player's from its palace
		/// (levels 0xd91e, styles 0xb222), an AI player's built from castleTier + 3 parts (3 - 36) by a generator seeded with
		/// player * ChanceRoot (levels up to 3 and parts / 5, a section only next to a higher one, its style player % 3). Each built
		/// section is drawn from the pictures at 0xd21c by its style, level and place (ends and middle).
		/// </summary>
		public void RenderPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderPalace({participantIdx}, {castleTier}, {horizLoc}, {vertLoc})");

			// The levels and the styles of the sections are in the stack frame, like in the original: the left neighbour of
			// section 0 is the frame word 0x34 (never written), the right neighbour of section 8 the style of section 0; the
			// final values of the locals stay there
			this.processor.EnterFrame(0x36);
			this.processor.PreserveIndexRegisters();

			this.owner.VideoCard.RenderLine(ScreenBox, horizLoc - 5, vertLoc + 13, horizLoc + 68, vertLoc + 13, 2);

			if (this.owner.World.Participants[participantIdx].TownTally != 0)
			{
				ushort levels = (ushort)(this.processor.RegFrame.Value16 - 0x32);
				ushort styles = (ushort)(this.processor.RegFrame.Value16 - 0x20);

				short parts = (short)this.owner.Helpers.VerifyValueSpan(castleTier + 3, 3, 36);
				short highest = (short)(parts / 5);

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					SetSectionWord(levels, sectionIdx, sectionIdx is >= 3 and <= 5 ? (ushort)0 : (ushort)0xffff);
				}

				ushort seed = (ushort)(participantIdx * this.owner.World.ChanceRoot);
				short built = 0;
				(ushort attempts, ushort fits, ushort level, ushort chosen) last = (0, 0, 0, 0);
				bool generated = false;

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
					{
						SetSectionWord(levels, sectionIdx, this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xd91e)));
						SetSectionWord(styles, sectionIdx, this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + 0xb222)));
					}

					built = 9;
				}
				else
				{
					for (built = 0; built < parts; built++)
					{
						ushort attempts = 0;
						bool fits;
						short chosen;
						short level;

						while (true)
						{
							fits = true;
							seed = (ushort)((seed * 5) + 1);
							chosen = (short)((byte)seed % 9);
							level = (short)(SectionWord(levels, chosen) + 1);

							if (chosen < 4 && ((short)SectionWord(levels, chosen + 1) < level || SectionWord(levels, 7 - chosen) == 0xffff))
							{
								fits = false;
							}

							if (chosen > 4 && ((short)SectionWord(levels, chosen - 1) < level || SectionWord(levels, 9 - chosen) == 0xffff))
							{
								fits = false;
							}

							if (level > highest || level > 3)
							{
								fits = false;
							}

							if (fits || ++attempts >= 0x3e7)
								break;
						}

						SetSectionWord(levels, chosen, (ushort)level);
						SetSectionWord(styles, chosen, (ushort)(participantIdx % 3));
						last = (attempts, (ushort)(fits ? 1 : 0), (ushort)level, (ushort)chosen);
						generated = true;
					}
				}

				// The frame word 0xc: the last level generated, then the shape of each drawn section
				ushort? lastShape = generated ? last.level : null;

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					short sectionLevel = (short)SectionWord(levels, sectionIdx);

					if (sectionLevel <= 0)
						continue;

					// 0: left end, 1: left, 2: middle, 3: right, 4: right end
					ushort shape = 2;

					if (sectionIdx < 4)
					{
						shape = (ushort)(SectionWord(levels, sectionIdx - 1) == 0xffff || sectionIdx == 1 ? 0 : 1);
					}

					if (sectionIdx > 4)
					{
						shape = (ushort)(SectionWord(levels, sectionIdx + 1) == 0xffff || sectionIdx == 7 ? 4 : 3);
					}

					lastShape = shape;

					int height = this.owner.Helpers.VerifyValueSpan(sectionLevel - 1, 0, 3);

					this.owner.Services.RenderPictureToScreen(ScreenBox, (7 * sectionIdx) + horizLoc, vertLoc - 1,
						this.processor.PullGlobalWord((ushort)((0x28 * SectionWord(styles, sectionIdx)) + ((((short)height * 5) + shape) << 1) + 0xd21c)));
				}

				this.processor.EmitLocalWord(0x36, (ushort)parts);
				this.processor.EmitLocalWord(0x6, (ushort)highest);
				this.processor.EmitLocalWord(0x4, seed);
				this.processor.EmitLocalWord(0x8, 0x9);

				if (lastShape != null)
				{
					this.processor.EmitLocalWord(0xc, lastShape.Value);
				}

				if (generated)
				{
					this.processor.EmitLocalWord(0x2, last.attempts);
					this.processor.EmitLocalWord(0xa, last.fits);
					this.processor.EmitLocalWord(0xe, last.chosen);
				}
			}

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderPalace");
		}

		/// <summary>A word of a table of the sections in the stack frame (the index may be -1 or 9, like in the original)</summary>
		private ushort SectionWord(ushort table, int sectionIdx)
		{
			return this.processor.PullGlobalWord((ushort)(table + (sectionIdx << 1)));
		}

		private void SetSectionWord(ushort table, int sectionIdx, ushort amount)
		{
			this.processor.EmitGlobalWord((ushort)(table + (sectionIdx << 1)), amount);
		}

		/// <summary>
		/// Score needed for the palace level: the sum of level * (7 - difficulty) for the levels 0 up to the given one, halved,
		/// plus 1
		/// </summary>
		/// <returns>Score, also in AX</returns>
		public ushort ObtainPalaceScoreForLevel(short castleTier)
		{
			this.processor.Journal.OpenScope($"ObtainPalaceScoreForLevel({castleTier})");

			short sum = 0;

			for (short tier = 0; tier <= castleTier; tier++)
			{
				sum = (short)(sum + (tier * (short)(7 - this.owner.World.HardnessTier)));
			}

			ushort score = (ushort)((sum / 2) + 1);

			this.processor.RegAcc.Value16 = score;

			this.processor.Journal.CloseScope("ObtainPalaceScoreForLevel");

			return score;
		}

		/// <summary>
		/// Gets current year as a String, appends buffer at 0xba06
		/// </summary>
		public void ObtainCurrentYearAsText()
		{
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(Math.Abs(this.owner.World.Season), 10));

			this.owner.Libc.AppendText(0xba06, (ushort)((this.owner.World.Season >= 0) ? 0x1df4 : 0x1df0));
		}

		/// <summary>
		/// The end of a turn: the pollution level SmogImpactTier moves one step towards 2 * DirtyCellTally - 4 * WorldHeatingTally
		/// (0 - 99; the newspaper warns at 12 with more than 6 polluted tiles, above 16 global warming comes). Then up to level + 1
		/// tries for a wonder 1 - 21 built by a rival: on the easiest level only a wonder the human player could build, on the
		/// two easiest the one a human city builds becomes 0x11 (the Manhattan Project with technology 0x2e, also in the first try
		/// when the rival has 0x2e and the human player not, unless built), not stronger rivals than him for 0x11 once he has
		/// 0x2e; a city of size above cost / 10 of a player at peace with the barbarians and with the wonder's technology builds
		/// it with up to a third of the treasury (announced unless 0xb278 is 1), the human cities building it get a new
		/// production. The final values of the locals stay in the stack frame, like in the original.
		/// </summary>
		public void HandleEndOfTurnEvents()
		{
			this.processor.Journal.OpenScope("HandleEndOfTurnEvents()");

			this.processor.EnterFrame(0xe);
			this.processor.PreserveIndexRegisters();

			short pull = (short)(((ushort)this.owner.World.DirtyCellTally << 1) - ((ushort)this.owner.World.WorldHeatingTally << 2) - (ushort)this.owner.World.SmogImpactTier);

			this.owner.World.SmogImpactTier = (short)this.owner.Helpers.VerifyValueSpan((short)(Math.Sign(pull) + this.owner.World.SmogImpactTier), 0, 99);

			if (this.processor.RegAcc.Value16 == 0xc && this.owner.World.DirtyCellTally > 6)
			{
				this.owner.Libc.CopyText(0xba06, "Scientists alarmed by\nrising temperatures.\nGlobal Warming feared!\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			if (this.owner.World.SmogImpactTier > 16)
			{
				this.owner.WorldSetup.EnactGlobalWarming(this.owner.World.WorldHeatingTally);

				this.owner.World.WorldHeatingTally++;
				this.owner.World.SmogImpactTier = 0;
			}

			short attempt;

			for (attempt = 0; attempt <= this.owner.World.HardnessTier; attempt++)
			{
				this.processor.EmitLocalWord(0xa, (ushort)attempt);

				if (TryRivalWonder(attempt))
					break;
			}

			this.processor.EmitLocalWord(0xa, (ushort)attempt);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleEndOfTurnEvents");
		}

		/// <summary>One try for a wonder built by a rival (see HandleEndOfTurnEvents), true when one was built</summary>
		private bool TryRivalWonder(short attempt)
		{
			short person = this.owner.World.PersonParticipantIdx;
			ushort wonderIdx = (ushort)(this.owner.Libc.Dice.DrawInt(21) + 1);
			this.processor.EmitLocalWord(0x8, wonderIdx);

			if (this.owner.World.HardnessTier == 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(person, (short)this.processor.PullGlobalWord((ushort)((ushort)(0x1e * wonderIdx) + 0xbb6))) == 0)
				return false;

			if (this.owner.World.HardnessTier < 2)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (HumanCityBuilds(townIdx, wonderIdx))
						wonderIdx = 0x11;
				}

				this.processor.EmitLocalWord(0x2, 0x80);
				this.processor.EmitLocalWord(0x8, wonderIdx);
			}

			ushort cityIdx = (ushort)this.owner.Libc.Dice.DrawInt(128);
			this.processor.EmitLocalWord(0x2, cityIdx);

			if (this.owner.World.Towns[cityIdx].ConditionMarker == 0xff)
				return false;

			ushort rivalIdx = (ushort)this.owner.World.Towns[cityIdx].ParticipantIdx;
			this.processor.EmitLocalWord(0xc, rivalIdx);

			if ((this.owner.World.Participants[rivalIdx].Relations[0] & 4) != 0)
				return false;

			if (wonderIdx == 0x11 && this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) == 0)
				return false;

			if (attempt == 0 && this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) != 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(person, 0x2e) == 0 && this.owner.World.MarvelTownIdx[17] == -1)
			{
				wonderIdx = 0x11;
				this.processor.EmitLocalWord(0x8, wonderIdx);
			}

			if (wonderIdx == 0x11 && this.owner.Advisors.PlayerKnowsTechnology(person, 0x2e) != 0 &&
				this.owner.World.Participants[rivalIdx].MartialMight > this.owner.World.Participants[person].MartialMight)
				return false;

			if (rivalIdx == 0xffff || rivalIdx == (ushort)person)
				return false;

			if (this.owner.World.MarvelTownIdx[wonderIdx] != -1)
				return false;

			ushort wonder = (ushort)(0x1e * wonderIdx);
			this.processor.EmitLocalWord(0xe, (ushort)(0x1c * cityIdx));

			if (this.owner.World.Towns[cityIdx].TrueExtent <= CPU.SignedQuotient((short)this.processor.PullGlobalWord((ushort)(wonder + 0xe82)), 10))
				return false;

			if (this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, (short)this.processor.PullGlobalWord((ushort)(wonder + 0xe86))) == 0)
				return false;

			this.owner.World.MarvelTownIdx[wonderIdx] = (short)cityIdx;
			this.owner.World.Towns[cityIdx].MaterialsTally = 0;

			this.owner.UnitOps.AttachReplayContent(10, (byte)rivalIdx, (byte)wonderIdx);

			EmptyMessage()
				.City((short)cityIdx)
				.Text(" (")
				.Text(this.processor.PullGlobalWord((ushort)((rivalIdx << 1) + 0x1982)))
				.Text(")\nbuilds ")
				.Text((ushort)(wonder + 0xe6a))
				.Text(".\n");

			this.owner.World.Participants[rivalIdx].Gold -= (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[rivalIdx].Gold / 3, 0,
				(10 * (short)this.processor.PullGlobalWord((ushort)(wonder + 0xe82))) - this.owner.World.Towns[cityIdx].MaterialsTally);

			if (WorldMapView != 1)
			{
				this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)((ushort)(0x3a * this.owner.World.Participants[rivalIdx].ChiefVisuals) + 0x1518)), 0);

				DialogStyle = (this.owner.World.Participants[person].Relations[rivalIdx] & 0x40) == 0 ? (ushort)2 : (ushort)5;

				DisplayDialog(0xba06, 100, 80);

				this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)(wonderIdx + 0x18), 1);

				RepaintMainScreen();

				this.owner.Services.PerformSound(1, 0);
			}

			if (wonderIdx == 0xe)
			{
				this.owner.Advisors.UncoverTechnology((short)rivalIdx, 0);

				this.owner.Advisors.UncoverTechnology((short)rivalIdx, 0);
			}

			for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (HumanCityBuilds(townIdx, wonderIdx))
					this.owner.Advisors.RenewCityScreen((short)townIdx);
			}

			this.processor.EmitLocalWord(0x2, 0x80);

			return true;
		}

		/// <summary>Whether the city is a city of the human player building the wonder</summary>
		private bool HumanCityBuilds(ushort townIdx, ushort wonderIdx)
		{
			return this.owner.World.Towns[townIdx].ConditionMarker != 0xff &&
				this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx &&
				this.owner.World.Towns[townIdx].PresentMakingIdx == (short)-(wonderIdx + 0x18);
		}

		/// <summary>
		/// Draws the main screen again: the map view, the box of the left column and the civilization panels, with the colour
		/// cycling stopped meanwhile (0x1c28 = 1 while drawing)
		/// </summary>
		public void RepaintMainScreen()
		{
			this.processor.Journal.OpenScope("RepaintMainScreen()");

			this.owner.Helpers.HaltHueCycling();

			this.processor.EmitGlobalWord(0x1c28, 0x1);

			if (VgaMode != 0)
			{
				this.owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				(short)MapViewHoriz,
				(short)MapViewVert);

			FloodBoxWithShadow(0, 97, 80, 103);

			this.owner.Startup.DisplayMouseCursor();

			DisplayCivilizationScenery();

			this.processor.EmitGlobalWord(0x1c28, 0x0);

			this.owner.Helpers.LaunchHueCycling();

			// Far return
			this.processor.Journal.CloseScope("RepaintMainScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithShadow(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			// function body
			this.owner.Helpers.FloodBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.owner.Dialogs.RenderShadowBox(horizLoc, vertLoc, breadth - 1, altitude - 1, 15, 8);
		}

		/// <summary>
		/// When 0xd762 is set: loads the palette of the human player's government, "backNa.pal" (N is the government / 2, 3 for
		/// government 3, "m" instead of "a" with technology 20), and the palette 0x1e56, and puts colours 5 on. The file name is
		/// made in a buffer in the stack frame, like in the original.
		/// </summary>
		public void DisplayCivilizationScenery()
		{
			this.processor.Journal.OpenScope("DisplayCivilizationScenery()");

			this.processor.EnterFrame(0x10);
			this.processor.PreserveSource();

			if (VgaMode != 0)
			{
				this.owner.Helpers.HaltHueCycling();

				ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
				this.owner.Libc.CopyText(docname, "back0a.pal");

				short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
				this.processor.EmitGlobalByte((ushort)(docname + 4), (byte)((regime / 2) + 0x30));

				if (regime == 3)
				{
					this.processor.EmitGlobalByte((ushort)(docname + 4), 0x33);
				}
				else if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0)
				{
					this.processor.EmitGlobalByte((ushort)(docname + 5), 0x6d);
				}

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docname, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1e56, 0xc5be);

				this.owner.Libc.CopyBytes(0xc744, 0xc35c, 0x180);

				this.owner.Services.ImportColorsFromBuffer(5, 0xc5be);

				this.owner.Helpers.LaunchHueCycling();
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayCivilizationScenery");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="score"></param>
		public short ObtainPalaceLevel(short rating)
		{
			// function body
			short castleTier = 0;

			do
			{
				castleTier += 5;
				rating -= castleTier;
			} while (rating > 0);

			castleTier /= 5;
			castleTier--;

			return castleTier;
		}
	}
}
