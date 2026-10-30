using System;
using System.Collections.Generic;
using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.UI;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1238.
	/// </summary>
	public class TurnEvents : GameModule
	{
		private const ushort BankruptcyWarningDialogAddress = 0x1c2a;
		private const ushort RetirementContinuePromptAddress = 0x1cf0;
		private const ushort EndGameTitlePictureAddress = 0x1cf8;
		private const ushort EndGameTitlePaletteAddress = 0x1d05;

		private const ushort TravellersReportGunpowderAddress = 0x1d26;
		private const ushort TravellersReportIronWorkingAddress = 0x1d3f;
		private const ushort TravellersReportAncientAddress = 0x1d53;

		private const ushort MenuBarGameLabelAddress = 0x1d6f;
		private const ushort MenuBarOrdersLabelAddress = 0x1d75;
		private const ushort MenuBarAdvisorsLabelAddress = 0x1d7d;
		private const ushort MenuBarWorldLabelAddress = 0x1d87;
		private const ushort MenuBarCivilopediaLabelAddress = 0x1d8e;

		private const ushort EraBcStringAddress = 0x1df0;
		private const ushort EraAdStringAddress = 0x1df4;

		private const ushort AdvisorPaletteResourceAddress = 0x1e56;
		private const ushort PalaceSectionSpritesTableAddress = 0xd21c;
		private const ushort ReplayHistoryScoresSegment = 0x3772;

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
		/// The news of a technology as the translated code wrote it: "... discover X from the Barbarians!" for every research
		/// of a computer player (TechnologyNewsRules.NamesSource). The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicTechnologyNews { get; set; }

		/// <summary>
		/// The railroads as in the original (RailroadRules): only the human player's units moved by hand ride for free, and
		/// a city has no railroad. The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicRailroads { get; set; }

		/// <summary>
		/// The city of a wonder read also for a wonder nobody has built (-1) or whose city was destroyed (128), as the
		/// translated code did it: IndexOutOfRangeException (WonderCityRules.ReadsCity). The tests turn it on (HeadlessGame),
		/// like ClassicHealing.
		/// </summary>
		public bool ClassicWonderCities { get; set; }

		/// <summary>
		/// The schism (DivideCivilization) reads the owner of the home city also for a unit without one (0xff), as the
		/// translated code did it: IndexOutOfRangeException (HomeCityRules.HasHome). The tests turn it on (HeadlessGame),
		/// like ClassicHealing.
		/// </summary>
		public bool ClassicSchismHomes { get; set; }

		/// <summary>
		/// The schism (DivideCivilization) as the translated code did it: the technologies copied onto the player himself
		/// (the new civilization gets none), the second flag of the war in the player's relation instead of the new
		/// civilization's, a unit in a city of another owner given to the player in a raw byte of the data segment as a unit of kind -1, the farthest cities given until the share is reached even when no farther city is left (the last
		/// one again, or city 0 of anybody, forever when it is empty), and IndexOutOfRangeException (Towns[0xffff]) when a
		/// side has no city for a capital. The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicSchism { get; set; }

		/// <summary>
		/// A starving city without a settler to lose is lost whatever its size, as the translated code did it (FeedCity
		/// tested the flags of an earlier comparison instead of the size); off it loses a citizen, and itself at size 1.
		/// The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicStarvation { get; set; }

		/// <summary>
		/// A computer unit weighs the strength of an own stack divided by 0x600 * player, as the translated code did it
		/// (StackStrengthRules.Divisor: the divisor kept in SI was lost in the second tally). The tests turn it on
		/// (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicStackStrength { get; set; }

		/// <summary>
		/// The military report takes the player of a chosen row from the stack frame also for a row of a player without
		/// contact, which never wrote it (garbage, IndexOutOfRangeException from 8 up), as the translated code did it. The
		/// tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicMilitaryReportRows { get; set; }

		/// <summary>
		/// The tenths of the census (children of a family) and of the spaceship (mass, flight time) written as the whole part
		/// again ("4.4" for 45), as the translated code did it (TenthsRules.Tenth). The tests turn it on (HeadlessGame), like
		/// ClassicHealing.
		/// </summary>
		public bool ClassicTenths { get; set; }

		/// <summary>
		/// The pictures of the spaceship parts read from part &lt;&lt; 11 instead of 0x682a + 2 * part, as the translated code
		/// did it (SpaceshipPictureRules.PictureAddress), so the parts are not drawn. The tests turn it on (HeadlessGame), like
		/// ClassicHealing.
		/// </summary>
		public bool ClassicSpaceshipPictures { get; set; }

		/// <summary>
		/// The random disasters after turn 50 with the loop counter taken from the drawn city, as the translated code did it:
		/// one city a turn (two only when city 0 is drawn) instead of TurnEndRules.DisasterDraws. The tests turn it on
		/// (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicDisasterDraws { get; set; }

		/// <summary>
		/// The high byte of the population in the replay entry 11 always 0, as the translated code wrote it
		/// (TurnEndRules.ReplayPopulationHigh). The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicReplayPopulation { get; set; }

		/// <summary>
		/// A threatening emissary names the civilization at a pointer past the table of names, as the translated code read it
		/// (TurnEndRules.EmissaryEnemyName). The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicEmissaryThreat { get; set; }

		/// <summary>
		/// The ruined units of player 6 cleared after each civilization made at the start of a game instead of that
		/// civilization's, as the translated code did it (TurnEndRules.RuinedUnitsCleared). The tests turn it on
		/// (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicFreshGameRuins { get; set; }

		/// <summary>
		/// A spaceship part without a free place of its kind put past the table of places (row 0xff, a structure into
		/// place 0x63), as the translated code did it; off a full kind is marked complete with "No further ... required."
		/// (SpaceshipSlotRules). The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicSpaceshipSlots { get; set; }

		/// <summary>
		/// A continent or sea of a new map labelled past the 63 ranked ones (or a label above 127, negative as a byte) takes
		/// the number of its label from past the table of numbers, as the translated code did it: the locals of
		/// ProduceTerrain, the frames of its callers above it or the stack under it; off it gets 15, the number of the
		/// unranked ones (GameInitAndIntro.RankLandOrSea). The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicLabelNumbers { get; set; }

		/// <summary>
		/// The demographics screen uses 16-bit signed integer arithmetic as in the original DOS CIV.EXE (which overflows
		/// and produces negative values for approval rating, productivity, and disease in large empires); off it uses
		/// 32-bit math clamped to valid ranges (CensusRules). The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicCensus { get; set; }

		/// <summary>
		/// In diplomacy, demanding gold under peace gives the human player the button answer of the dialog (0 or 1)
		/// instead of the gold amount, as in the original CIV.EXE translation slip (DemandGoldUnderPeace). Off: the human
		/// player receives the actual gold amount demanded. The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicTributeDemand { get; set; }

		/// <summary>
		/// In palace improvement style selection, thickened/inner wall segments (look == 1) in the original CIV.EXE DOS
		/// used dirty uninitialized registers for the second half of the segment, always drawing the Medieval tile
		/// (offset 6 + 2) and using an incorrect height offset (SelectPalaceArchitectureStyle). Off: the second half is
		/// drawn with the correct style tile and height for the selected option. The tests turn it on (HeadlessGame), like ClassicHealing.
		/// </summary>
		public bool ClassicPalaceStyleGlitch { get; set; }

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
					int troopKind = this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if (troopKind != 0xff && (this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Fortified) != 0)
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


			if (PlayTurnOfPlayers(out short startGold))
			{
				FinishTurn(startGold);
			}


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

			UnitLimitTold = 0;
			RiotShown = 0;
			this.owner.World.HighestScienceTally = 0;
			LiveRealmCount = 0;

			// The living players and the most technologies
			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				LiveRealmCount = (ushort)(LiveRealmCount + 1);

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

				MapViewHoriz = (ushort)(this.Troops.PullAt(troop, UnitMemory.Horiz) - 7);
				MapViewVert = (ushort)(this.Troops.PullAt(troop, UnitMemory.Vert) - 6);

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
				if (participantIdx < this.owner.World.PersonParticipantIdx && MidTurnSave != 0)
					continue;

				PlayTurnOfPlayer(participantIdx);

				if (GameEndRequest != 0 && ConquestEnd == 0)
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

			if (MidTurnSave == 0)
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

			MidTurnSave = 0x0;

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
		/// Disasters for random cities of size 5 or more (not the last city of their owner): two drawn cities
		/// (TurnEndRules.DisasterDraws); with ClassicDisasterDraws the loop counter is the city number + 1, like in the
		/// translated code, so it ends after the first city unless that is city 0
		/// </summary>
		private void StrikeRandomCities()
		{
			if (!this.ClassicDisasterDraws)
			{
				for (int draw = 0; draw < TurnEndRules.DisasterDraws; draw++)
				{
					short townIdx = (short)this.owner.Libc.Dice.DrawInt(128);

					StrikeCity(townIdx);
				}

				return;
			}

			short counter = 0;

			while (counter < 2)
			{
				counter = (short)this.owner.Libc.Dice.DrawInt(128);

				StrikeCity(counter);

				counter++;
			}
		}

		/// <summary>A disaster for the city when it is of size 5 or more and not the last city of its owner</summary>
		private void StrikeCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if (town.ConditionMarker != 0xff && this.owner.World.Participants[town.ParticipantIdx].TownTally > 1 && town.TrueExtent >= 5)
			{
				this.owner.Founding.HandleCityDisasters(townIdx);
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

			this.owner.Words.Clear();

			this.owner.BlockText.DisplayTextDialog(BankruptcyWarningDialogAddress);

			DialogStyle = 0x4;

			DisplayDialog(TextBuffer.Address, 80, 80);
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
		/// status of the human player (11: cities, population; with ClassicReplayPopulation the high byte is always 0, see
		/// TurnEndRules.ReplayPopulationHigh) and the ranking (12: the players by their standing, two places a byte; the bytes are made
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
				TurnEndRules.ReplayPopulationHigh(population, this.ClassicReplayPopulation), (byte)population);

			byte[] ranking = new byte[4];

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				short standing = this.owner.World.Participants[participantIdx].Standing;
				int pairIdx = standing / 2;
				if (pairIdx >= 0 && pairIdx < 4)
				{
					byte nibble = (byte)((standing & 1) == 0 ? participantIdx : (participantIdx << 4));
					ranking[pairIdx] |= nibble;
				}
			}

			this.owner.UnitOps.AttachReplayContent(12, ranking[3], ranking[2], ranking[1], ranking[0]);
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

			if (LoadedGameMark == 0xfffd)
			{
				LoadedGameMark = 0xfffe;
				return;
			}

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Tally -= (short)(10 * this.owner.World.DirtyCellTally);

			if (LoadedGameMark == 0xfffe)
			{
				LoadedGameMark = 0x0;
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
			ArrivedSpaceship = 0xffff;

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.owner.World.StarshipMarkers & (1 << participantIdx)) == 0)
					continue;

				if (this.owner.World.Participants[participantIdx].StarshipArrivalSeason == this.owner.World.Season)
				{
					ArrivedSpaceship = (ushort)participantIdx;
				}
			}

			if ((short)(0x820 - (0x14 * this.owner.World.HardnessTier)) == this.owner.World.Season)
			{
				Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

				Message(GovernmentTitle(person.RegimeKind))
					.Text(" ")
					.Text(LeaderName(this.owner.World.PersonParticipantIdx))
					.Text("\nplans retirement\nin 20 years.\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			return ArrivedSpaceship != 0xffff || ConquestEnd != 0 ||
				(short)(0x834 - (0x14 * this.owner.World.HardnessTier)) == this.owner.World.Season;
		}

		/// <summary>
		/// The end of the game: conquest, the spaceship arrival, the end of the reign (each with its news), the end sequence
		/// (not after a launched spaceship of the human player... bit 0x100), and the question to go on (not after conquest)
		/// </summary>
		private void EndGame()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Words.Set("*** GAME OVER ***\n");

			if (ConquestEnd != 0)
			{
				this.owner.Words.Set("Your civilization\nhas conquered\nthe entire planet!\n");

				ArrivedSpaceship = 0xffff;

				this.owner.Paper.DisplayNewspaper(-1);

				this.owner.Replayer.DisplayConquerorScreen();
			}

			if (ArrivedSpaceship != 0xffff)
			{
				this.owner.Prologue.DisplayCivilizationScreen((short)ArrivedSpaceship);

				RepaintMainScreen();

				Message(CivilizationName(ArrivedSpaceship))
					.Text(" spaceship\narrives at Alpha Centauri.\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			if ((short)(0x834 - (0x14 * this.owner.World.HardnessTier)) == this.owner.World.Season)
			{
				Message(LeaderName(this.owner.World.PersonParticipantIdx))
					.Text(" dynasty\nends after glorious\n6000 year reign!\n");

				ushort leader = (ushort)(0x3a * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);

				this.owner.Services.PerformSound(LeaderWordAt(leader, LeaderTable.Tune), 3);

				this.owner.Paper.DisplayNewspaper(-1);

				this.owner.Services.PerformSound(1, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			if ((this.owner.World.StarshipMarkers & 0x100) == 0)
			{
				DisplayEndGameSequence();
			}

			RepaintMainScreen();

			GameEndFlags |= 0x1;

			if (ConquestEnd == 0)
			{
				this.owner.Words.Clear();

				this.owner.BlockText.DisplayTextDialog(RetirementContinuePromptAddress);

				// The first line goes on with the game, the second one ends it
				GameEndRequest = (ushort)(1 - DisplayDialog(TextBuffer.Address, 80, 80));
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

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, EndGameTitlePictureAddress, 0);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, EndGameTitlePaletteAddress, 1);

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


			if ((ushort)((ushort)(5 - (ushort)this.owner.World.HardnessTier) * 0x1e) <= this.owner.World.RoundTally)
			{
				if ((((byte)(this.owner.World.RoundTally & 0xff) + 1) & 7) == 0 && (this.owner.World.ScienceLeadingFoundVia[58] & 7) == 0)
				{
					LandBarbariansFromSea();
				}

				RaidHumanCity();
			}

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

				vertLoc = (short)(this.owner.Libc.Dice.DrawInt(44) + 3);
			}
			while (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != 0xa ||
				this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff ||
				(short)this.owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 0x10);

			short units = (short)this.owner.Helpers.VerifyValueSpan((this.owner.World.RoundTally / 150) + 1, 1, 3);

			this.owner.UnitOps.ConstructUnit(0, units < 3 ? (short)0x11 : (short)0x12, horizLoc, vertLoc);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.owner.World.ScienceLeadingFoundVia[56] & 7) != 0 ? (short)7 : (short)3;

				if (unitIdx == units)
					kind = 0x1a;


				ushort troopIdx = this.owner.UnitOps.ConstructUnit(0, kind, horizLoc, vertLoc);

				// The barbarian units start on sentry (player 0); troopIdx may be past the table (0xffff, no free place)
				int marker = (ushort)(UnitMemory.TroopStride * troopIdx);
				this.Troops.AssignAt(marker, UnitMemory.Status, (byte)(this.Troops.PullAt(marker, UnitMemory.Status) | 1));
			}

		}

		/// <summary>The barbarians landing near a city of the human player (see DisplayTravellersReport)</summary>
		private void RaidHumanCity()
		{
			ushort townIdx = (ushort)this.owner.Libc.Dice.DrawInt(128);

			if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff ||
				this.owner.World.Towns[townIdx].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				return;

			ushort plunder = this.owner.Conquest.ComputeCityPlunder((short)townIdx);

			if ((short)this.owner.Libc.Dice.DrawInt(100) >= (short)plunder)
				return;

			ushort tries = 0;
			short horizLoc, vertLoc;

			while (true)
			{
				int shift = this.owner.Libc.Dice.DrawInt(13);
				horizLoc = (short)((short)this.owner.World.Towns[townIdx].Place.X + shift - 6);

				shift = this.owner.Libc.Dice.DrawInt(13);
				vertLoc = (short)((short)this.owner.World.Towns[townIdx].Place.Y + shift - 6);

				townIdx = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
	
				short distance = (short)(ushort)this.owner.Helpers.ObtainDistance(horizLoc, this.owner.World.Towns[townIdx].Place.X, vertLoc, this.owner.World.Towns[townIdx].Place.Y);

				tries++;

				if ((short)tries >= 0x63)
					return;

				if (distance < 6)
					continue;

				if (this.owner.UnitOps.VerifyTileOccupation(0, horizLoc, vertLoc) != 0)
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

				this.owner.Words.Set("Travellers report:\n");

				if ((this.owner.World.ScienceLeadingFoundVia[35] & 7) != 0)
				{
					this.owner.Words.Append(TravellersReportGunpowderAddress);
				}
				else if ((this.owner.World.ScienceLeadingFoundVia[34] & 7) != 0)
				{
					this.owner.Words.Append(TravellersReportIronWorkingAddress);
				}
				else
				{
					this.owner.Words.Append(TravellersReportAncientAddress);
				}

				this.owner.Conquest.ObtainCityLabel((short)townIdx);

				this.owner.Words.Append("!\n");

				DialogStyle = 0x2;

				DisplayDialog(TextBuffer.Address, 100, 32);
			}

			short units = (short)this.owner.Helpers.VerifyValueSpan(this.owner.Libc.Dice.DrawInt(this.owner.World.Towns[townIdx].TrueExtent / 2), 2, 99);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.owner.World.ScienceLeadingFoundVia[35] & 7) != 0 ? ((unitIdx & 4) != 0 ? (short)9 : (short)4) :
					((unitIdx & 4) != 0 ? (short)0xa : (short)6);

				if (unitIdx == units)
					kind = 0x1a;


				ushort sight = (ushort)((ushort)(0xc * this.owner.UnitOps.ConstructUnit(0, kind, horizLoc, vertLoc)) + 0x81dd);
				this.processor.EmitGlobalByte(sight, (byte)(this.processor.PullGlobalByte(sight) | (byte)this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc]));

				if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				{
					this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
				}
			}

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

			// The powers of the frame of the original (B3: in C#): when no power is above -1, the ranking takes the player of the
			// rank before (at first 0, the local 0x1a of the original read before it is written)

			ushort[] powers = new ushort[8];
			short strongest = 0;
			ushort treaties = 0;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				Player participant = this.owner.World.Participants[participantIdx];
				ushort power = (ushort)(DivideByPowerOfTwo(participant.Gold, 5) + (participant.GrandTownExtent << 3) + participant.FoundScienceTally);

				for (int kind = 0; kind < 0x1c; kind++)
				{
					power += (ushort)(UnitKindWord((sbyte)kind, UnitKindTable.Price) * participant.LiveTroops[kind]);
				}

				if (((1 << participantIdx) & this.owner.World.LiveRealms) == 0)
				{
					power = 0;
				}

				powers[participantIdx] = power;

				if (participantIdx != this.owner.World.PersonParticipantIdx && strongest < (short)power)
				{
					strongest = (short)power;
				}

				if (this.owner.World.RoundTally < 600)
				{
					// The power graph is in the segment 0x3772 (ES is left there, like in the original)
					this.processor.SegExtra.Value16 = ReplayHistoryScoresSegment;
					this.processor.EmitUInt8(ReplayHistoryScoresSegment, GraphPlace(participantIdx), (byte)this.owner.Helpers.VerifyValueSpan((short)power / 8, 0, 255));
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

			ushort best = 0;

			for (short rank = 1; rank < 8; rank++)
			{
				strongest = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short power = (short)powers[participantIdx];

					if (strongest < power)
					{
						best = (ushort)participantIdx;
						strongest = power;
					}
				}

				this.owner.World.Participants[best].Standing = (short)(8 - rank);
				powers[best] = 0xffff;
			}

			LateGameNudge = 0x0;

			Player human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

			if (human.Standing == 7 && human.TownTally > 4 && human.LiveTroops[25] == 0 && this.owner.World.RoundTally > 200)
			{
				LateGameNudge = 0x1;

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

			this.owner.TextPainter.RenderTextToScreen0(MenuBarGameLabelAddress, 8, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(MenuBarOrdersLabelAddress, 64, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(MenuBarAdvisorsLabelAddress, 128, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(MenuBarWorldLabelAddress, 192, 1, 15);

			this.owner.TextPainter.RenderTextToScreen0(MenuBarCivilopediaLabelAddress, 240, 1, 15);

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
					short storage = (short)this.owner.VideoCard.ObtainFreeStorage();

					EmptyMessage()
						.Number(storage);
					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 2, 59, 0);
				}

				this.owner.Words.Clear();
				short population = (short)this.owner.Helpers.ObtainPlayerPopulation(this.owner.World.PersonParticipantIdx);
				this.owner.Words.Append("#");

				if (population != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 2, 73, 0);
				}

				this.owner.Words.Clear();
				ObtainCurrentYearAsText();
				this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 2, 81, 0);

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
				this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 2, 89, 0);

				if (RepaintingMainScreen == 0)
				{
					AnnouncePopulationMark(population);
				}
			}


			this.processor.Journal.CloseScope("RenderStatusBar");
		}

		/// <summary>
		/// The population mark of RenderStatusBar: when the population passes the next mark 0x1c24 (in tens below 100, then in
		/// hundreds with 9 added), the mark moves and, unless 0x3484 is below -1, a dialog tells "...exceeds N00,000 citizens"
		/// </summary>
		private void AnnouncePopulationMark(short population)
		{
			short previousMark = (short)PopulationMark;

			Message("The population of the\nfertile ")
				.Text(CivilizationName(this.owner.World.PersonParticipantIdx))
				.Text(" empire now\nexceeds ");

			if (population < 100)
			{
				if ((short)(PopulationMark * 10) < population)
				{
					short mark = (short)(population / 10);

					this.owner.Words.Append(this.owner.Libc.IntegerToText(mark, 10));

					PopulationMark = (ushort)mark;
				}
			}
			else if ((short)((PopulationMark - 9) * 100) < population)
			{
				short hundreds = (short)(population / 100);

				this.owner.Words.Append(this.owner.Libc.IntegerToText(hundreds, 10));
				this.owner.Words.Append(",0");

				PopulationMark = (ushort)(hundreds + 9);
			}

			if (previousMark < (short)PopulationMark && (short)LoadedGameMark >= -1)
			{
				this.owner.Words.Append("00,000 citizens.\n");

				DialogStyle = 4;

				DisplayDialog(TextBuffer.Address, 100, 80);
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

			// The levels and the styles of the sections are in the words of the frame of the original (B3: in C#): the left
			// neighbour of section 0 is the frame word 0x34 (never written, 0), the right neighbour of section 8 the style of
			// section 0 (0 for an AI player without it)

			this.owner.VideoCard.RenderLine(ScreenBox, horizLoc - 5, vertLoc + 13, horizLoc + 68, vertLoc + 13, 2);

			if (this.owner.World.Participants[participantIdx].TownTally != 0)
			{
				FrameWords frame = new FrameWords(this.processor, 0x36);
				SectionTable levels = new SectionTable(frame, 0x32);
				SectionTable styles = new SectionTable(frame, 0x20);

				short parts = (short)this.owner.Helpers.VerifyValueSpan(castleTier + 3, 3, 36);
				short highest = (short)(parts / 5);

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					SetSectionWord(levels, sectionIdx, sectionIdx is >= 3 and <= 5 ? (ushort)0 : (ushort)0xffff);
				}

				ushort seed = (ushort)(participantIdx * this.owner.World.ChanceRoot);
				short built = 0;

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
					{
						SetSectionWord(levels, sectionIdx, this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + Palace.PalaceSectionLevelsAddress)));
						SetSectionWord(styles, sectionIdx, this.processor.PullGlobalWord((ushort)((sectionIdx << 1) + Palace.PalaceSectionStylesAddress)));
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
					}
				}

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

					int height = this.owner.Helpers.VerifyValueSpan(sectionLevel - 1, 0, 3);

					this.owner.Services.RenderPictureToScreen(ScreenBox, (7 * sectionIdx) + horizLoc, vertLoc - 1,
						this.processor.PullGlobalWord((ushort)((0x28 * SectionWord(styles, sectionIdx)) + ((((short)height * 5) + shape) << 1) + PalaceSectionSpritesTableAddress)));
				}
			}

			// Far return
			this.processor.Journal.CloseScope("RenderPalace");
		}

		/// <summary>A table of the sections in the frame words of RenderPalace: section 0 at the given distance below BP</summary>
		private readonly record struct SectionTable(FrameWords Frame, int Distance);

		/// <summary>A word of a table of the sections (the index may be -1 or 9, like in the original)</summary>
		private static ushort SectionWord(SectionTable table, int sectionIdx)
		{
			return table.Frame.Frame(table.Distance - (sectionIdx << 1));
		}

		private static void SetSectionWord(SectionTable table, int sectionIdx, ushort amount)
		{
			table.Frame.SetFrame(table.Distance - (sectionIdx << 1), amount);
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

			this.processor.Journal.CloseScope("ObtainPalaceScoreForLevel");

			return score;
		}

		/// <summary>
		/// Gets current year as a String, appends buffer at TextBuffer.Address
		/// </summary>
		public void ObtainCurrentYearAsText()
		{
			this.owner.Words.Append(this.owner.Libc.IntegerToText(Math.Abs(this.owner.World.Season), 10));

			this.owner.Words.Append((ushort)((this.owner.World.Season >= 0) ? EraAdStringAddress : EraBcStringAddress));
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


			short pull = (short)(((ushort)this.owner.World.DirtyCellTally << 1) - ((ushort)this.owner.World.WorldHeatingTally << 2) - (ushort)this.owner.World.SmogImpactTier);

			this.owner.World.SmogImpactTier = (short)this.owner.Helpers.VerifyValueSpan((short)(Math.Sign(pull) + this.owner.World.SmogImpactTier), 0, 99);

			if (this.owner.World.SmogImpactTier == 12 && this.owner.World.DirtyCellTally > 6)
			{
				this.owner.Words.Set("Scientists alarmed by\nrising temperatures.\nGlobal Warming feared!\n");

				this.owner.Paper.DisplayNewspaper(-1);
			}

			if (this.owner.World.SmogImpactTier > 16)
			{
				this.owner.WorldSetup.EnactGlobalWarming(this.owner.World.WorldHeatingTally);

				this.owner.World.WorldHeatingTally++;
				this.owner.World.SmogImpactTier = 0;
			}

			for (short attempt = 0; attempt <= this.owner.World.HardnessTier; attempt++)
			{
				if (TryRivalWonder(attempt))
					break;
			}

			// Far return
			this.processor.Journal.CloseScope("HandleEndOfTurnEvents");
		}

		/// <summary>One try for a wonder built by a rival (see HandleEndOfTurnEvents), true when one was built</summary>
		private bool TryRivalWonder(short attempt)
		{
			short person = this.owner.World.PersonParticipantIdx;
			ushort wonderIdx = (ushort)(this.owner.Libc.Dice.DrawInt(21) + 1);

			if (this.owner.World.HardnessTier == 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(person, (short)BuildingWord(wonderIdx, BuildingTable.Technology)) == 0)
				return false;

			if (this.owner.World.HardnessTier < 2)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (HumanCityBuilds(townIdx, wonderIdx))
						wonderIdx = 0x11;
				}

					}

			ushort cityIdx = (ushort)this.owner.Libc.Dice.DrawInt(128);

			if (this.owner.World.Towns[cityIdx].ConditionMarker == 0xff)
				return false;

			ushort rivalIdx = (ushort)this.owner.World.Towns[cityIdx].ParticipantIdx;

			if ((this.owner.World.Participants[rivalIdx].Relations[0] & 4) != 0)
				return false;

			if (wonderIdx == 0x11 && this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) == 0)
				return false;

			if (attempt == 0 && this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) != 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(person, 0x2e) == 0 && this.owner.World.MarvelTownIdx[17] == -1)
			{
				wonderIdx = 0x11;
				}

			if (wonderIdx == 0x11 && this.owner.Advisors.PlayerKnowsTechnology(person, 0x2e) != 0 &&
				this.owner.World.Participants[rivalIdx].MartialMight > this.owner.World.Participants[person].MartialMight)
				return false;

			if (rivalIdx == 0xffff || rivalIdx == (ushort)person)
				return false;

			if (this.owner.World.MarvelTownIdx[wonderIdx] != -1)
				return false;

			if (this.owner.World.Towns[cityIdx].TrueExtent <= CPU.SignedQuotient((short)BuildingWord(24 + wonderIdx, BuildingTable.Price), 10))
				return false;

			if (this.owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, (short)BuildingWord(24 + wonderIdx, BuildingTable.Technology)) == 0)
				return false;

			this.owner.World.MarvelTownIdx[wonderIdx] = (short)cityIdx;
			this.owner.World.Towns[cityIdx].MaterialsTally = 0;

			this.owner.UnitOps.AttachReplayContent(10, (byte)rivalIdx, (byte)wonderIdx);

			EmptyMessage()
				.City((short)cityIdx)
				.Text(" (")
				.Text(CivilizationName(rivalIdx))
				.Text(")\nbuilds ")
				.Text(BuildingRecordText(24 + wonderIdx))
				.Text(".\n");

			this.owner.World.Participants[rivalIdx].Gold -= (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[rivalIdx].Gold / 3, 0,
				(10 * (short)BuildingWord(24 + wonderIdx, BuildingTable.Price)) - this.owner.World.Towns[cityIdx].MaterialsTally);

			if (WorldMapView != 1)
			{
				this.owner.Services.PerformSound(LeaderWord(this.owner.World.Participants[rivalIdx].ChiefVisuals, LeaderTable.ShortTune), 0);

				DialogStyle = (this.owner.World.Participants[person].Relations[rivalIdx] & 0x40) == 0 ? (ushort)2 : (ushort)5;

				DisplayDialog(TextBuffer.Address, 100, 80);

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

			RepaintingMainScreen = 0x1;

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

			RepaintingMainScreen = 0x0;

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

			if (VgaMode != 0)
			{
				this.owner.Helpers.HaltHueCycling();

				short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
				char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
				char techChar = regime != 3 && this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0 ? 'm' : 'a';
				string docname = $"back{regimeChar}{techChar}.pal";

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docname, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, AdvisorPaletteResourceAddress, 0xc5be);

				this.owner.Libc.CopyBytes(0xc744, 0xc35c, 0x180);

				this.owner.Services.ImportColorsFromBuffer(5, 0xc5be);

				this.owner.Helpers.LaunchHueCycling();
			}

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
