using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class StartGameMenu : GameModule
	{
		public StartGameMenu(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The choices of a new game and its start: the difficulty (also by a mouse click on a portrait), the number of
		/// civilizations (3 - 7, the living players 0xff >> (6 - rivals)), the tribe (the first leaders of the playing slots,
		/// then the second ones; the human player gets that slot and leader, or a random slot for none), the world wiped
		/// (cities, wonders, leaders, spaceships, what the players saw), every civilization made (ConstructCivilization), the
		/// human player's rates 5 / 5 (and 50 gold on the easiest level), and the welcome text with the technologies the human
		/// player starts with.
		/// </summary>
		public void ConfigureFreshGame()
		{
			this.processor.Journal.OpenScope("ConfigureFreshGame()");

			// The frame like the original (the functions called see the stack at the same depth)
			this.processor.EnterFrame(0x12);
			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 0x7);

			ChooseDifficulty();

			ChooseCompetition();

			WipeWorld();

			ChooseTribe();

			StartCivilizations();

			WelcomeHumanPlayer();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ConfigureFreshGame");
		}

		/// <summary>
		/// The difficulty menu (a click on a portrait of the left picture chooses too), none = 0; the portrait of the
		/// difficulty on the left
		/// </summary>
		private void ChooseDifficulty()
		{
			this.owner.Words.Set("Difficulty Level...\n Chieftain (easiest)\n Warlord\n Prince\n King\n Emperor (toughest)\n");

			this.owner.World.HardnessTier = (short)this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 160, 35, 1);

			if (MousePresent != 0 && this.processor.PullGlobalWord(0x2fa2) != 0)
			{
				this.owner.Startup.ObtainMouseState();

				if ((short)this.owner.GlobDb3c < 0x87)
				{
					this.owner.World.HardnessTier = (short)this.owner.Helpers.VerifyValueSpan(((short)this.owner.GlobDb3e - 12) / 35, 0, 4);
				}
			}

			if (this.owner.World.HardnessTier == -1)
			{
				this.owner.World.HardnessTier = 0;
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 150, 200, 0);

			RenderDifficultyPortrait(0x14, 0x64);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				ScreenBox, 0x96, 0);

			this.owner.Startup.DisplayMouseCursor();
		}

		/// <summary>
		/// The portrait of the difficulty (left column for even levels, right for odd ones) at (horizLoc, vertLoc)
		/// </summary>
		private void RenderDifficultyPortrait(ushort horizLoc, ushort vertLoc)
		{
			ushort column = (ushort)((this.owner.World.HardnessTier & 1) != 0 ? 0x50 : 0x15);

			this.owner.VideoCard.RenderImage(BackScreenBox, column,
				(ushort)((0x23 * this.owner.World.HardnessTier) + 6), 0x35, 0x2f, ScreenBox, horizLoc, vertLoc);
		}

		/// <summary>
		/// The number of civilizations (7 - 3, asked again for none): the rivals (2 - 6), the living players and as many
		/// portraits stacked
		/// </summary>
		private void ChooseCompetition()
		{
			this.owner.Words.Set("Level of Competition...\n ");

			for (short tally = 7; tally > 2; tally--)
			{
				this.owner.Words.Append(this.owner.Libc.IntegerToText(tally, 10));

				this.owner.Words.Append(" Civilizations\n ");
			}

			do
			{
				this.owner.World.BotRivalTally = (short)this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 160, 35, 1);
			}
			while ((ushort)this.owner.World.BotRivalTally == 0xffff);

			this.owner.World.BotRivalTally = (short)this.owner.Helpers.VerifyValueSpan(6 - this.owner.World.BotRivalTally, 2, 6);
			this.owner.World.LiveRealms = (short)(0xff >> (byte)(6 - (byte)this.owner.World.BotRivalTally));

			this.owner.Startup.ConcealMouseCursor();

			for (short rivalIdx = this.owner.World.BotRivalTally; rivalIdx >= 0; rivalIdx--)
			{
				RenderDifficultyPortrait((ushort)((rivalIdx << 1) + 0x14), (ushort)((3 * rivalIdx) + 0x64));
			}
		}

		/// <summary>
		/// The world of a new game: year -4000, no cities, wonders, leaders, spaceships, first finders of technologies and
		/// nothing seen; the game options (no sound driver: without the sound options)
		/// </summary>
		private void WipeWorld()
		{
			this.owner.World.Season = -4000;
			this.owner.World.DiagMarkers = 0xf;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				this.owner.World.Towns[townIdx].LabelIdx = 0xff;
				this.owner.World.Towns[townIdx].ConditionMarker = 0xff;
			}

			for (int idx = 0; idx < 256; idx++)
			{
				this.owner.World.TownPlaces[idx].X = -1;
			}

			for (int scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				this.owner.World.ScienceLeadingFoundVia[scienceIdx] = 0;
			}

			for (int marvelIdx = 0; marvelIdx <= 0x15; marvelIdx++)
			{
				this.owner.World.MarvelTownIdx[marvelIdx] = -1;
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.owner.World.Participants[participantIdx].ChiefVisuals = -1;

				WipeSpaceshipContent(participantIdx);
			}

			for (int horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					this.owner.World.ChartSight[horizLoc, vertLoc] = 0;
				}
			}

			this.owner.World.SessionPrefMarkers = 0xfa;

			if (this.processor.PullGlobalByte(0x1a30) == 0x4e)
			{
				this.owner.World.SessionPrefMarkers &= 0x7fef;
			}
		}

		/// <summary>
		/// The tribe menu: the first leaders (records 1 - rivals + 1), then the second ones (records 9 - rivals + 9). The human
		/// player gets the slot of the line and its first or second leader; for no answer a random slot 1 - rivals with its
		/// own names (DisplayCityFoundedMessage). The names: leader 0x19a2, civilization 0x1982 and plural 0x1992 (the name +
		/// "s" without a plural), the plural written on the screen.
		/// </summary>
		private void ChooseTribe()
		{
			this.owner.Words.Set("Pick your tribe...\n ");

			_ = this.owner.Words.Length;

			for (int leader = 1; leader < this.owner.World.BotRivalTally + 2; leader++)
			{
				this.owner.Words.Append(LeaderText(leader, LeaderTable.Civilization));

				this.owner.Words.Append("\n ");
			}

			for (int leader = 1; leader < this.owner.World.BotRivalTally + 2; leader++)
			{
				this.owner.Words.Append((ushort)((0x3a * leader) + 0x16d2));

				this.owner.Words.Append("\n ");
			}

			this.owner.VideoCard.RenderImage(BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				ScreenBox, 0x96, 0);

			this.owner.Startup.DisplayMouseCursor();

			// The line: 0 - rivals the first leaders of slots 1 - rivals + 1, then the second leaders (slot + 8)
			this.owner.World.PersonParticipantIdx = (short)this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 160, 35, 1);

			if (this.owner.World.PersonParticipantIdx <= this.owner.World.BotRivalTally)
			{
				this.owner.World.PersonParticipantIdx++;
			}
			else
			{
				this.owner.World.PersonParticipantIdx = (short)(this.owner.World.PersonParticipantIdx - this.owner.World.BotRivalTally + 8);
			}

			if (this.owner.World.HardnessTier == 0)
			{
				this.owner.World.SessionPrefMarkers |= 1;
			}

			// The slot with the leader bit, made 0 - 7 only below (like the original)
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx & 7].PresentInquiryIdx = -1;

			this.owner.World.FollowingCollectionRound += (short)this.owner.Libc.Dice.DrawInt(50);

			for (int idx = 0; idx < 0xc; idx++)
			{
				this.processor.EmitGlobalWord((ushort)(0xd91e + (idx << 1)), 0xffff);
				this.processor.EmitGlobalWord((ushort)(0xb222 + (idx << 1)), 0x0);
			}

			for (int idx = 3; idx <= 5; idx++)
			{
				this.processor.EmitGlobalWord((ushort)(0xd92a + (idx << 1)), 0);
				this.processor.EmitGlobalWord((ushort)(0xd91e + (idx << 1)), 0);
			}

			bool randomTribe = this.owner.World.PersonParticipantIdx == 0;
			bool secondLeader = false;

			if (randomTribe)
			{
				this.owner.World.PersonParticipantIdx = (short)(this.owner.Libc.Dice.DrawInt(this.owner.World.BotRivalTally) + 1);
			}
			else
			{
				secondLeader = this.owner.World.PersonParticipantIdx > 7;
				this.owner.World.PersonParticipantIdx &= 7;
			}

			short personIdx = this.owner.World.PersonParticipantIdx;
			ushort names = (ushort)(personIdx << 1);

			this.processor.EmitGlobalByte(CivilizationNameAt(names), 0x0);

			if (randomTribe)
			{
				this.owner.CityNaming.DisplayCityFoundedMessage();

				this.owner.World.Participants[personIdx].ChiefVisuals = personIdx;

				this.owner.Libc.CopyText(LeaderNameAt(names), LeaderText(personIdx, LeaderTable.Name));
			}
			else
			{
				Player person = this.owner.World.Participants[personIdx];

				person.ChiefVisuals = (short)(secondLeader ? personIdx + 8 : personIdx);

				ushort leader = (ushort)(0x3a * person.ChiefVisuals);

				this.owner.Libc.CopyText(LeaderNameAt(names), LeaderTextAt(leader, LeaderTable.Name));

				this.owner.Libc.CopyText(CivilizationNameAt(names), LeaderTextAt(leader, LeaderTable.Civilization));

				this.owner.Words.Set(LeaderTextAt(leader, LeaderTable.CivilizationPlural));

				if (this.owner.Words[0] == 0)
				{
					Message(CivilizationNameAt(names))
						.Text("s");
				}

				this.owner.Libc.CopyText(CivilizationPluralAt(names), TextBuffer.Address);
			}

			this.owner.TextPainter.RenderCenteredTextToScreen0(CivilizationPluralAt(names), 46, 92, 15);
		}

		/// <summary>
		/// Every civilization made (ConstructCivilization), the start places described, the human player's rates and gold,
		/// the identity bits (the second leaders)
		/// </summary>
		private void StartCivilizations()
		{
			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				ConstructCivilization(participantIdx);

				// The original clears the ruined units of player 6 here: its index is the counter of an earlier loop (6)
				for (int kind = 0; kind < 8; kind++)
				{
					this.owner.World.Participants[6].TroopsRuined[kind] = 0;
				}
			}

			CharacterizeStartLocation();

			Player person = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

			this.owner.World.ParticipantMarkers = (short)(1 << this.owner.World.PersonParticipantIdx);
			person.LevyRatio = 5;
			person.ResearchLevyRatio = 5;

			if (this.owner.World.HardnessTier == 0)
			{
				person.Gold = 50;
			}

			this.owner.World.ParticipantIdentMarkers = 0;

			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if (this.owner.World.Participants[participantIdx].ChiefVisuals >= 8)
				{
					this.owner.World.ParticipantIdentMarkers |= (short)(1 << participantIdx);
				}
			}
		}

		/// <summary>
		/// The welcome: the found city message, the text 0x35c3 with the names and the technologies the human player starts
		/// with (two a line, then "and Roads."), the leader's music, the difficulty portrait; waits for a key or a click
		/// </summary>
		private void WelcomeHumanPlayer()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 150, 0, 170, 200, 0);

			this.owner.CityNaming.DisplayFoundCityMessage();

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(ScreenBox + 0x10), 0x1);

			ushort names = (ushort)(this.owner.World.PersonParticipantIdx << 1);

			this.owner.Libc.CopyText(CodeText4, LeaderNameAt(names));

			this.owner.Libc.CopyText(CodeText1, CivilizationPluralAt(names));

			this.owner.Words.Clear();

			this.owner.BlockText.DisplayTextDialog(0x35c3);

			_ = this.owner.Words.Length;

			int shown = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, scienceIdx) == 0)
					continue;

				this.owner.Words.Append(TechnologyText(scienceIdx));

				this.owner.Words.Append(", ");

				shown++;

				if (shown == 2)
				{
					this.owner.Words.Append("\n");
				}
			}

			this.owner.Words.Append("and Roads.\n");

			ushort leader = (ushort)(0x3a * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);

			this.owner.Services.PerformSound(LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 15);

			this.owner.UnitOps.RenderBusyIndicator();

			RenderDifficultyPortrait(0x86, 0x14);

			this.owner.BlockText.RenderTextChunk(99, 88, 81, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();
		}

		/// <summary>
		/// Makes the civilization of the player anew (at the start of a game, for a new civilization in a free slot, or after a
		/// civil war; for the human player only at the start): everything of the player is wiped (units, relations, cities,
		/// money; government 1, rates 4 / 4; in a running game it gets the technologies the human player knows and that were
		/// first found by a third player, each with chance 1/2). A living player other than 0 then gets a leader (a free one
		/// of its colour at the start, else the other leader of its colour), a start place and its names, a settler there, the
		/// barbarian units within 8 tiles removed. The start place is random on land: not too close to cities and earlier
		/// players, on good land (screen 2 value), on a continent with room for cities and without cities of others after the
		/// start of the game, not on a hut; the demands get lower with the attempts (0x34), after 2000 attempts it takes the
		/// last place. EARTH (0xd76a) uses the start place of the leader at the start. A new civilization in a running game
		/// with no place, or too late for the difficulty, does not come: the player is dead (0, and the game ends 0xdc48 when
		/// only the human player is left).
		/// </summary>
		/// <returns>1 when made (also when it was not made anew), 0 when it does not come; also in AX</returns>
		public ushort ConstructCivilization(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ConstructCivilization({participantIdx})");

			// The frame like the original (the functions called see the stack at the same depth)
			this.processor.EnterFrame(0x34);
			this.processor.PreserveIndexRegisters();

			if (participantIdx != this.owner.World.PersonParticipantIdx || this.owner.World.RoundTally == 0)
			{
				ushort result = RemakeCivilization(participantIdx);

				this.processor.RegAcc.Value16 = result;
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ConstructCivilization");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>The work of ConstructCivilization; 0 when the civilization does not come</summary>
		private ushort RemakeCivilization(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];

			player.StarshipInhabitants = 0;
			player.RunningSagaStanding = 0;
			player.FoundScienceTally = 0;
			player.InquiryHeadway = 0;
			player.Gold = 0;
			player.RegimeKind = 1;
			player.LevyRatio = 4;
			player.ResearchLevyRatio = 4;

			// The cities of the other players (1 - 7) on each continent
			ushort[] foreignTowns = new ushort[16];

			for (int continentIdx = 0; continentIdx < 16; continentIdx++)
			{
				player.Landmasses[continentIdx].Approach = 0;

				for (int otherIdx = 1; otherIdx < 8; otherIdx++)
				{
					foreignTowns[continentIdx] += (ushort)this.owner.World.Participants[otherIdx].Landmasses[continentIdx].TownTally;
				}
			}

			player.TownTally = 0;
			player.TroopTally = 0;
			player.SoilTally = 1;
			player.ColonistTally = 1;
			player.TouchParticipantTimeleft = -1;

			for (int idx = 0; idx < player.ScienceObtainedSince.Length; idx++)
			{
				player.ScienceObtainedSince[idx] = -1;
			}

			for (int troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)((0x600 * participantIdx) + (0xc * troopIdx));

				this.processor.EmitGlobalByte((ushort)(troop + 0x81d7), 0xff);
				this.processor.EmitGlobalByte((ushort)(troop + 0x81d4), 0x0);
			}

			this.processor.EmitGlobalWord((ushort)(0xd20c + (participantIdx << 1)), 0);
			player.Tally = 0;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Services.FloodBox(PictureScreenBox, 240, 0, 80, 50, 0);
			}

			this.owner.Bot.WipeStrategicLocations(participantIdx);

			for (int otherIdx = 0; otherIdx < 8; otherIdx++)
			{
				player.Relations[otherIdx] = 0;
				this.owner.World.Participants[otherIdx].Relations[participantIdx] = 0;
			}

			for (int idx = 0; idx < 5; idx++)
			{
				player.FoundScienceMarkers[idx] = 0;
			}

			if (this.owner.World.RoundTally != 0)
			{
				ShareKnownTechnologies(participantIdx);
			}

			if (participantIdx != 0 && (this.owner.World.LiveRealms & (1 << participantIdx)) != 0)
			{
				if (!SettleCivilization(participantIdx, foreignTowns))
					return 0;

				player.FoundScienceTally = 1;
			}

			for (int kind = 0; kind < 28; kind++)
			{
				player.MissingTroops[kind] = 0;
				player.LiveTroops[kind] = 0;
				player.TroopsInsideMaking[kind] = 0;
			}

			return 1;
		}

		/// <summary>
		/// In a running game the new civilization gets, each with chance 1/2, the technologies (0 - 0x47) that the human
		/// player knows and that were first found by a player other than the human player
		/// </summary>
		private void ShareKnownTechnologies(short participantIdx)
		{
			short personIdx = this.owner.World.PersonParticipantIdx;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0)
					continue;

				int finder = (byte)this.owner.World.ScienceLeadingFoundVia[scienceIdx] & 7;

				if (finder == 0 || finder == (ushort)personIdx)
					continue;

				if (this.owner.Libc.Dice.DrawInt(2) == 0)
				{
					this.owner.Advisors.EarnTechnology(participantIdx, scienceIdx, participantIdx);
				}
			}
		}

		/// <summary>
		/// The leader, the start place, the names and the settler of a living player (not 0); false when a new civilization
		/// in a running game does not come (then the player is dead)
		/// </summary>
		private bool SettleCivilization(short participantIdx, ushort[] foreignTowns)
		{
			Player player = this.owner.World.Participants[participantIdx];

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				if (this.owner.World.RoundTally == 0)
				{
					ChooseFreeLeader(participantIdx);
				}
				else
				{
					player.ChiefVisuals ^= 8;
				}
			}

			(short horizLoc, short vertLoc, ushort attemptTally) = ChooseStartPlace(participantIdx, foreignTowns);

			if (this.owner.World.RoundTally == 0 && EarthGame != 0)
			{
				// EARTH: the start place of the leader
				ushort place = (ushort)(player.ChiefVisuals << 1);

				horizLoc = (sbyte)this.processor.PullGlobalByte((ushort)(place + 0x35da));
				vertLoc = (sbyte)this.processor.PullGlobalByte((ushort)(place + 0x35db));
				attemptTally = 0;
			}

			if (this.owner.World.RoundTally != 0)
			{
				// A new civilization must have the other leader of its slot than the one there before (identity bit)
				bool identity = (this.owner.World.ParticipantIdentMarkers & (1 << participantIdx)) != 0;

				if (player.ChiefVisuals >= 8 && identity)
				{
					attemptTally = 0x7d0;
				}

				if (player.ChiefVisuals < 8 && !identity)
				{
					attemptTally = 0x7d0;
				}
			}

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				player.ResearchLevyRatio = (short)(LeaderWord(player.ChiefVisuals, LeaderTable.Leaning) + 3);
				player.LevyRatio = (short)(9 - player.ResearchLevyRatio);
			}

			ushort latestRound = (ushort)(0x15e - (0x32 * this.owner.World.HardnessTier));

			if ((latestRound < this.owner.World.RoundTally || attemptTally >= 0x7d0) &&
				participantIdx != this.owner.World.PersonParticipantIdx)
			{
				// It does not come: the other leader back, the player dead
				player.ChiefVisuals ^= 8;
				this.owner.World.LiveRealms &= (short)~(1 << participantIdx);

				if ((ushort)((1 << this.owner.World.PersonParticipantIdx) | 1) == (ushort)this.owner.World.LiveRealms)
				{
					GameEndRequest = 1;
					ConquestEnd = 1;
				}

				return false;
			}

			NameCivilization(participantIdx);

			this.owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);

			this.owner.UnitOps.ConstructUnit(participantIdx, 0, horizLoc, vertLoc);

			player.HorizBegin = horizLoc;

			// The barbarian units near the start place are removed
			for (short troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)(0xc * troopIdx);

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)),
					vertLoc, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

				if (distance <= 8)
				{
					this.owner.UnitOps.EliminateUnit(0, troopIdx);
				}
			}

			return true;
		}

		/// <summary>
		/// At the start of a game: a random leader (0 - 15) of the player's colour (leader & 7) that no other player has
		/// </summary>
		private void ChooseFreeLeader(short participantIdx)
		{
			Player player = this.owner.World.Participants[participantIdx];
			bool free;

			do
			{
				player.ChiefVisuals = (short)this.owner.Libc.Dice.DrawInt(16);

				free = (player.ChiefVisuals & 7) == participantIdx;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if (otherIdx != participantIdx && this.owner.World.Participants[otherIdx].ChiefVisuals == player.ChiefVisuals)
					{
						free = false;
					}
				}
			}
			while (!free);
		}

		/// <summary>
		/// A random start place (x 8 - 71, y 8 - 41) on land, tried again while: the screen 2 value of the place is below
		/// 12 - attempts / 32; it is nearer than 10 - attempts / 64 to a city (30 without cities) or, at the start of the game,
		/// to the first unit of an earlier player; its continent has fewer city sites than 32 - round / 16 - attempts / 64;
		/// after the start of the game other players have cities on the continent; there is a hut. After 2000 attempts the
		/// last place is taken (for the human player at the start of the game it goes on trying).
		/// </summary>
		private (short HorizLoc, short VertLoc, ushort AttemptTally) ChooseStartPlace(short participantIdx, ushort[] foreignTowns)
		{
			ushort attemptTally = 0;

			while (true)
			{
				short horizLoc = (short)(this.owner.Libc.Dice.DrawInt(64) + 8);
				short vertLoc = (short)(this.owner.Libc.Dice.DrawInt(34) + 8);

				ushort townIdx = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
				short nearest = townIdx == 0xffff ? (short)0x1e : (short)this.owner.Helpers.ObtainDistance(horizLoc,
					this.owner.World.Towns[townIdx].Place.X, vertLoc, this.owner.World.Towns[townIdx].Place.Y);

				if (this.owner.World.RoundTally == 0)
				{
					for (int otherIdx = 1; otherIdx < participantIdx; otherIdx++)
					{
						ushort troop = (ushort)(0x600 * otherIdx);

						short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)),
							vertLoc, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

						if (distance < nearest)
						{
							nearest = distance;
						}
					}
				}

				short landValue = (short)this.owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);

				attemptTally++;

				if ((short)attemptTally > 0x7d0 &&
					(participantIdx != this.owner.World.PersonParticipantIdx || this.owner.World.RoundTally != 0))
				{
					return (horizLoc, vertLoc, attemptTally);
				}

				if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if ((short)(0xc - ((short)attemptTally / 32)) > landValue)
					continue;

				short relief = (short)((short)attemptTally / 64);

				if (nearest < (short)(0xa - relief))
					continue;

				ushort continentIdx = this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
				short siteDemand = (short)(0x20 - (this.owner.World.RoundTally / 16) - relief);

				if (this.owner.World.Landmasses[continentIdx].ConstructionSpotTally < siteDemand)
					continue;

				if (this.owner.World.Season > 0 && foreignTowns[this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc)] != 0)
					continue;

				if (this.owner.MapOps.ContainsHut(this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc), horizLoc, vertLoc) != 0)
					continue;

				return (horizLoc, vertLoc, attemptTally);
			}
		}

		/// <summary>
		/// The leader name (0x19a2) and, unless the human player has named it, the civilization name (0x1982) and its plural
		/// (0x1992; the name + "s" when the leader has none) of the player's leader record (0x14e2 + 0x3a * leader)
		/// </summary>
		private void NameCivilization(short participantIdx)
		{
			ushort leader = (ushort)(0x3a * this.owner.World.Participants[participantIdx].ChiefVisuals);
			ushort names = (ushort)(participantIdx << 1);

			this.owner.Libc.CopyText(LeaderNameAt(names), LeaderTextAt(leader, LeaderTable.Name));

			if (participantIdx == this.owner.World.PersonParticipantIdx &&
				this.processor.PullGlobalByte(CivilizationNameAt(names)) != 0)
				return;

			this.owner.Libc.CopyText(CivilizationNameAt(names), LeaderTextAt(leader, LeaderTable.Civilization));

			this.owner.Words.Set(LeaderTextAt(leader, LeaderTable.CivilizationPlural));

			if (this.owner.Words[0] == 0)
			{
				Message(CivilizationNameAt(names))
					.Text("s");
			}

			this.owner.Libc.CopyText(CivilizationPluralAt(names), TextBuffer.Address);
		}

		/// <summary>
		/// When the player (not 0, not the human player) has no city left: tells that the civilization was destroyed by
		/// destroyerIdx (dialog style 0x2f9e: 3 when by the human player, else 5 with relations bit 0x40 of the human player
		/// towards it, else 2), takes away its spaceship, units and everything it saw, notes it in the replay and makes a new
		/// civilization in its place
		/// </summary>
		public void EliminateCivilization(short participantIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"EliminateCivilization({participantIdx}, {participantIdx1})");

			if (participantIdx != 0 && participantIdx != this.owner.World.PersonParticipantIdx && !OwnsCity(participantIdx))
			{
				Message(CivilizationName(participantIdx))
					.Text("\ncivilization\ndestroyed\nby ")
					.Text(CivilizationPlural(participantIdx1))
					.Text("!\n");

				short humanIdx = this.owner.World.PersonParticipantIdx;
				DialogStyle = (ushort)((this.owner.World.Participants[humanIdx].Relations[participantIdx] & 0x40) != 0 ? 5 : 2);

				if (participantIdx1 == humanIdx)
				{
					DialogStyle = 3;
				}

				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.owner.Conquest.EliminateSpaceship(participantIdx);

				this.owner.UnitOps.AttachReplayContent(13, (byte)participantIdx, (byte)participantIdx1);

				UnitMemory troops = new UnitMemory(this.processor);

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (troops.Exists(participantIdx, troopIdx))
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
				}

				sbyte unseen = (sbyte)~(1 << participantIdx);

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					for (int vertLoc = 0; vertLoc < 50; vertLoc++)
					{
						this.owner.World.ChartSight[horizLoc, vertLoc] &= unseen;
					}
				}

				ConstructCivilization(participantIdx);
			}

			this.processor.Journal.CloseScope("EliminateCivilization");
		}

		/// <summary>True when one of the 128 cities belongs to the player</summary>
		private bool OwnsCity(short participantIdx)
		{
			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker != 0xff && this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Rates the start of every player 1 - 7 by his first unit (settlers): the nearest civilization on the same continent
		/// (close under 10, moderate under 20 +1, far away +2, isolated +4), river valley (more than one river tile +2) or
		/// grasslands (3 grassland tiles +1), and the continent size (/ 50: 2 and 3 +1, more +2). The players behind the best
		/// get compensation: more than 3 points behind another settlers (+3), then a technology per point (UncoverTechnology).
		/// Returns the first player with the best start, or the last of them not too close to another civilization. The
		/// descriptions in TextBuffer.Address are never shown. The tables of the continents and the points are in the stack frame and SI / DI
		/// get the values of the original before the calls, like in the original.
		/// </summary>
		public ushort CharacterizeStartLocation()
		{
			this.processor.Journal.OpenScope("CharacterizeStartLocation()");

			this.processor.EnterFrame(0x5e);
			this.processor.PreserveIndexRegisters();

			ushort frame = this.processor.RegFrame.Value16;
			short[] points = new short[8];

			ushort participantIdx;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.processor.RegDest.Value16 = (ushort)((participantIdx << 1) + frame - 0x5a);

				this.owner.MapOps.ObtainContinentID((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));
			}

			ushort scattered = 0;
			short best = 0;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				short nearest = NearestCivilization(participantIdx, troop);

				if (nearest >= 10 && nearest != 0x3e7)
				{
					scattered |= (ushort)(1 << (byte)participantIdx);
				}

				short start = RateStartLocation(participantIdx, troop, nearest);

				if (start > best)
				{
					best = start;
				}

				this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);
				points[participantIdx] = start;
			}

			short chosen = -1;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff)
					continue;

				this.owner.Words.Set("Skills: ");

				this.processor.RegDest.Value16 = (ushort)((participantIdx << 1) + frame - 0x1c);

				if ((short)(best - points[participantIdx]) > 3)
				{
					points[participantIdx] += 3;

					this.owner.UnitOps.ConstructUnit((short)participantIdx, 0, (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)),
						(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)));

					this.owner.Words.Append("Settlers+");
				}

				for (ushort given = 0; (short)(best - points[participantIdx]) > (short)given; given++)
				{
					this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

					if (given != 0)
						this.owner.Words.Append("+");

					this.owner.Words.Append((ushort)((ushort)(this.owner.Advisors.UncoverTechnology((short)participantIdx, 0) * 0x16) + 0x4da));
				}

				this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

				if (points[participantIdx] != best)
					continue;

				this.owner.Words.Append("NONE");

				if (chosen == -1 || (scattered & (1 << (byte)participantIdx)) != 0)
				{
					chosen = (short)participantIdx;
				}
			}

			this.processor.RegAcc.Value16 = (ushort)chosen;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("CharacterizeStartLocation");

			return (ushort)chosen;
		}

		/// <summary>
		/// The distance from the player's first unit to the nearest first unit of another player on the same continent, 0x3e7
		/// for none (the locals 0x5e, 0x8, 0xa, 0x48 as in the original)
		/// </summary>
		private short NearestCivilization(ushort participantIdx, ushort troop)
		{
			short nearest = 0x3e7;
			ushort otherIdx;


			for (otherIdx = 1; otherIdx < 8; otherIdx++)
			{

				if (otherIdx == participantIdx)
					continue;

				ushort other = (ushort)(0x600 * otherIdx);
				this.processor.RegSource.Value16 = other;

				if (this.processor.PullGlobalByte((ushort)(other + 0x81d7)) == 0xff)
					continue;

				this.processor.RegDest.Value16 = troop;

				ushort otherContinent = this.owner.MapOps.ObtainContinentID((sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d6)));

				if (this.owner.MapOps.ObtainContinentID((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6))) != otherContinent)
					continue;

				short distance = (short)(ushort)this.owner.Helpers.ObtainDistance(
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d5)),
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)), (sbyte)this.processor.PullGlobalByte((ushort)(other + 0x81d6)));

				if (distance < nearest)
				{
					nearest = distance;
				}
			}


			return nearest;
		}

		/// <summary>The points of the player's start (see CharacterizeStartLocation); the description goes into TextBuffer.Address</summary>
		private short RateStartLocation(ushort participantIdx, ushort troop, short nearest)
		{
			short start = 0;

			this.owner.Words.Set("Nearest civilization: ");

			if (nearest < 10)
			{
				this.owner.Words.Append("close");
			}
			else if (nearest < 0x14)
			{
				this.owner.Words.Append("moderate");
				start++;
			}
			else if (nearest != 0x3e7)
			{
				this.owner.Words.Append("far away");
				start += 2;
			}
			else
			{
				this.owner.Words.Append("isolated");
				start += 4;
			}

			ushort ocean = 0, grassland = 0, river = 0;
			ushort neighbourIdx;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(neighbourIdx << 1);
				this.processor.RegDest.Value16 = troop;

				ushort terrain = this.owner.MapOps.ObtainTerrainType(
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)) + (short)DirectionHoriz(neighbourIdx),
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6)) + (short)DirectionVert(neighbourIdx));


				if (terrain == 0xa)
					ocean++;

				if (terrain == 2)
					grassland++;

				if (terrain == 0xb)
					river++;
			}


			this.owner.Words.Set(ocean != 0 ? (ushort)0x3653 : (ushort)0x365c);

			if ((short)river > 1)
			{
				this.owner.Words.Append("river valley on a ");
				start += 2;
			}
			else if ((short)grassland >= 3)
			{
				this.owner.Words.Append("grasslands on a ");
				start++;
			}
			else
			{
				this.owner.Words.Append("plains on a ");
			}

			this.processor.RegSource.Value16 = troop;

			switch (CPU.SignedQuotient((short)this.owner.MapOps.ObtainLandMassSize((sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d5)), (sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81d6))), 0x32))
			{
				case 0:
					this.owner.Words.Append(0x3695);
					break;

				case 1:
					this.owner.Words.Append(0x36a3);
					break;

				case 2:
					this.owner.Words.Append(0x36b1);
					start++;
					break;

				case 3:
					this.owner.Words.Append(0x36c2);
					start++;
					break;

				default:
					this.owner.Words.Append("large continent.");
					start += 2;
					break;
			}


			return start;
		}

		/// <summary>
		/// Despite the name: cuts the sprites of the game out of the sprite sheets into picture tables (the order of the
		/// ScreenToPicture calls gives the picture numbers, like in the original). The three tribe portraits (0xdf62); with VGA
		/// colours (0xd762) the unit sprites of 0x3709 (10 rows of 16, 0xb886; rows 6 - 9, and all rows with 0x1a3e, only
		/// column 0 cut and copied to the others), the quarter tiles (0xd294) and 4 tiles (0xd2d4), else the EGA unit sprites
		/// of 0x3714 (12 in two rows); with VGA its palette part (0x30 bytes to 0x6b34). Then the terrain and map pieces of
		/// sheet 0x36ff: tiles, rivers, specials, city walls, numbers, cursors and the 8 coloured rows of city sprites (0xd54e),
		/// coloured per player with the colours 0x1946 / 0x1956.
		/// </summary>
		public void DisplayTribeSelection()
		{
			this.processor.Journal.OpenScope("DisplayTribeSelection()");

			this.owner.Startup.QueryForDisk(7, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x36ff, 0);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
				PictureScreenBox, 0xa0, 0x32);

			for (int idx = 0; idx < 3; idx++)
			{
				StorePicture(0xdf62, idx, 1, (ushort)((0x2a * idx) + 0xa0), 0x8e, 0x29, 0x3a);
			}

			if (VgaMode != 0)
			{
				CutVgaUnitSprites();
			}
			else
			{
				CutEgaUnitSprites();
			}

			if (VgaMode != 0)
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3720, 0);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x372a, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.processor.EmitGlobalByte((ushort)(0x6b34 + idx), this.processor.PullGlobalByte((ushort)(0xbdf4 + idx)));
				}
			}

			for (int idx = 0; idx < 8; idx++)
			{
				StorePicture(0xb27a, idx, 1, (ushort)(idx << 4), 0x30, 0x10, 0x10);
				StorePicture(0xb29a, idx, 1, (ushort)((idx << 4) + 0x80), 0x60, 0x10, 0x10);
			}

			StorePicture(0xb2ba, 0, 1, 0x30, 0x20, 0x10, 0x10);

			for (int idx = 1; idx < 0x10; idx++)
			{
				StorePicture(0x6dfe, idx, 1, (ushort)(idx << 4), 0x40, 0x10, 0x10);
				StorePicture(0x6e1e, idx, 1, (ushort)(idx << 4), 0x50, 0x10, 0x10);
			}

			for (int idx = 0; idx < 0x14; idx++)
			{
				ushort horizLoc = (ushort)((7 * idx) + 0xa0);

				StorePicture(0xd21c, idx, 1, horizLoc, 0, 7, 15);
				StorePicture(0xd244, idx, 1, horizLoc, 0x10, 7, 15);
				StorePicture(0xd26c, idx, 1, horizLoc, 0x20, 7, 15);
			}

			for (int idx = 0; idx < 4; idx++)
			{
				StorePicture(0x7eec, idx, 1, (ushort)((idx << 4) + 0x50), 0x80, 0x10, 0x10);
			}

			for (int idx = 0; idx < 7; idx++)
			{
				StorePicture(0xd4ce, idx, 1, (ushort)(idx << 4), 0x20, 0x10, 0x10);
			}

			PointerPicture = this.processor.PullGlobalWord(0x6e92);

			for (int idx = 0; idx < 8; idx++)
			{
				StorePicture(0xd4de, idx, 1, (ushort)(((idx & 3) << 3) + 0x81), (ushort)(((idx & 4) << 1) + 0x21), 7, 7);
			}

			for (int idx = 0; idx < 9; idx++)
			{
				StorePicture(0x6e96, idx, 1, (ushort)(idx << 3), 0x80, 8, 0x10);
			}

			for (int idx = 0; idx < 0x10; idx++)
			{
				StorePicture(0xd4ee, idx, 1, (ushort)((idx << 4) + 1), 0x71, 15, 15);
			}

			for (int idx = 0; idx < 8; idx++)
			{
				StorePicture(0xd50e, idx, 1, (ushort)((idx << 4) + 1), 0x61, 15, 15);
			}

			StorePicture(0xb880, 0, 1, 0x99, 0x29, 7, 7);
			StorePicture(0xdf0c, 0, 1, 0xb0, 0x80, 0x14, 9);

			for (int playerIdx = 0; playerIdx < 8; playerIdx++)
			{
				CutPlayerCitySprites(playerIdx);
			}

			this.processor.Journal.CloseScope("DisplayTribeSelection");
		}

		/// <summary>
		/// Cuts a picture out of the screen into the table at tableAddr, entry idx
		/// </summary>
		private void StorePicture(ushort tableAddr, int idx, ushort displayIdx, ushort horizLoc, ushort vertLoc, ushort breadth, ushort altitude)
		{
			ushort picture = this.owner.VideoCard.ScreenToPicture(displayIdx, horizLoc, vertLoc, breadth, altitude);

			this.processor.EmitGlobalWord((ushort)(tableAddr + (idx << 1)), picture);
		}

		/// <summary>
		/// The VGA unit sprites (sheet 0x3709, 10 rows of 16 at 0xb886, 0x20 bytes a row), the quarter tiles (8 of 4 at 0xd294)
		/// and 4 tiles (0xd2d4)
		/// </summary>
		private void CutVgaUnitSprites()
		{
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3709, 0);

			for (int row = 0; row < 0xa; row++)
			{
				for (int column = 0; column < 0x10; column++)
				{
					ushort entry = (ushort)(0xb886 + (row << 5) + (column << 1));

					// Rows 6 - 9 (and all rows with 0x1a3e) have one sprite, repeated in every column
					if ((row >= 6 || LowMemoryVga != 0) && column != 0)
					{
						this.processor.EmitGlobalWord(entry, this.processor.PullGlobalWord((ushort)(entry - 2)));
					}
					else
					{
						this.processor.EmitGlobalWord(entry,
							this.owner.VideoCard.ScreenToPicture(1, (ushort)(column << 4), (ushort)(row << 4), 0x10, 0x10));
					}
				}
			}

			for (int tile = 0; tile < 8; tile++)
			{
				for (int quarter = 0; quarter < 4; quarter++)
				{
					// The upper quarters at row 0xb0, the lower ones at 0xb8 (swapped left and right)
					ushort horizLoc = quarter < 2
						? (ushort)(((quarter & 1) << 3) + (tile << 4))
						: (ushort)(((tile << 4) - ((quarter & 1) << 3)) + 8);
					ushort vertLoc = (ushort)(quarter < 2 ? 0xb0 : 0xb8);

					StorePicture((ushort)(0xd294 + (tile << 3)), quarter, 1, horizLoc, vertLoc, 8, 8);
				}
			}

			for (int idx = 0; idx < 4; idx++)
			{
				StorePicture(0xd2d4, idx, 1, (ushort)((idx << 4) + 0x80), 0xb0, 0x10, 0x10);
			}
		}

		/// <summary>
		/// The EGA unit sprites: sheet 0x3714 on screen 3 (copied to the back screen), 12 sprites in two rows (0xb886 and 0xb888,
		/// 0x20 bytes apart)
		/// </summary>
		private void CutEgaUnitSprites()
		{
			this.owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, 0x3714, 1);

			this.owner.VideoCard.RenderImage(SpriteScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			for (int idx = 0; idx < 0xc; idx++)
			{
				StorePicture(0xb886, idx << 4, 3, (ushort)(idx << 4), 0, 0x10, 0x10);
				StorePicture(0xb888, idx << 4, 3, (ushort)(idx << 4), 0x10, 0x10, 0x10);
			}
		}

		/// <summary>
		/// The city sprites of a player (0x1c of them at 0xd54e + 0x40 * player): the city row of the sheet copied to row 0x80 of
		/// the back screen, white (15) to 11 and grey (7) to 3 for those player colours, 10 and 2 to the colours of the player
		/// (0x1946, 0x1956)
		/// </summary>
		private void CutPlayerCitySprites(int playerIdx)
		{
			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0xa0, 0x140, 0x20,
				BackScreenBox, 0, 0x80);

			ushort colours = (ushort)(playerIdx << 1);

			if (this.processor.PullGlobalWord((ushort)(0x1946 + colours)) == 0xf)
			{
				this.owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, 15, 11);
			}

			if (this.processor.PullGlobalWord((ushort)(0x1946 + colours)) == 0x7)
			{
				this.owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, 7, 3);
			}

			this.owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, 10,
				(byte)this.processor.PullGlobalWord((ushort)(0x1946 + colours)));

			this.owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, 2,
				(byte)this.processor.PullGlobalWord((ushort)(0x1956 + colours)));

			for (int idx = 0; idx < 0x1c; idx++)
			{
				StorePicture(0xd54e, (playerIdx << 5) + idx, 1, (ushort)(((idx % 0x14) << 4) + 1), (ushort)(((idx / 0x14) << 4) + 0x81), 15, 15);
			}
		}

		/// <summary>
		/// Loads the government picture of the human player, "govtNa.pic": N is the government / 2 (3 for government 3), "m"
		/// instead of "a" with technology 20; the leader part of it goes into the picture 0x19e8. The file name is made in a
		/// buffer in the stack frame, like in the original.
		/// </summary>
		public void ImportGovernmentGraphics()
		{
			this.processor.Journal.OpenScope("ImportGovernmentGraphics()");

			this.processor.EnterFrame(0x12);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(2, 1);

			ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			this.owner.Libc.CopyText(docname, "govt0a.pic");

			ushort regime = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.EmitGlobalByte((ushort)(docname + 4), (byte)(((short)regime / 2) + 0x30));

			if (regime == 3)
			{
				this.processor.EmitGlobalByte((ushort)(docname + 4), 0x33);
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0)
			{
				this.processor.EmitGlobalByte((ushort)(docname + 5), 0x6d);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docname, 0);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0xa0, 0x3c,
				PictureScreenBox, 0xa0, 0x8c);

			this.owner.Startup.QueryForDisk(0, 1);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ImportGovernmentGraphics");
		}

		/// <summary>
		/// After a revolution of the human player (not to government 0): the newspaper "... government changed to ...!", then
		/// the advisor screen with the title 0x375a and the 4 advisors (pictures from 0x19e8, names at 0x2fac without their
		/// last character, every second one lower) until a key or click
		/// </summary>
		public void DisplayRevolutionScreen()
		{
			this.processor.Journal.OpenScope("DisplayRevolutionScreen()");

			short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;

			if (regime != 0)
			{
				this.owner.Advisors.RenderAdvisorScenery();

				Message(CivilizationName(this.owner.World.PersonParticipantIdx))
					.Text(" government\nchanged to ");
				this.owner.Libc.AppendText(TextBuffer.Address,
					GovernmentName(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind));
				this.owner.Words.Append("!\n");

				this.owner.Paper.DisplayNewspaper(-2);

				this.owner.Startup.ConcealMouseCursor();

				this.owner.Services.FloodBox(ScreenBox, 0, 100, 320, 100, 15);

				this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 0x6);

				this.owner.TextPainter.RenderCenteredTextToScreen0(0x375a, 160, 102, 0);

				this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 0x2);

				for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
				{
					this.owner.VideoCard.RenderImage(PictureScreenBox, (ushort)((0x28 * advisorIdx) + 0xa0), 0x8c, 0x28, 0x3c,
						ScreenBox, (ushort)((0x50 * advisorIdx) + 0x14), 0x76);

					this.owner.Words.Set(this.processor.PullGlobalWord((ushort)((advisorIdx << 1) + 0x2fac)));
					this.owner.Words[this.owner.Words.Length - 1] = 0x0;

					this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, (80 * advisorIdx) + 40, (advisorIdx & 1) != 0 ? 186 : 180, 0);
				}

				this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 0x1);

				this.owner.Startup.DisplayMouseCursor();

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.Advisors.RecoverAdvisorScenery();
			}

			this.processor.Journal.CloseScope("DisplayRevolutionScreen");
		}

		/// <summary>
		/// The player has no spaceship: no arrival, every place of the spaceship layout (15 rows of 12, segment 0x3772) empty
		/// </summary>
		public void WipeSpaceshipContent(short participantIdx)
		{
			this.processor.Journal.OpenScope($"WipeSpaceshipContent({participantIdx})");

			this.owner.World.Participants[participantIdx].StarshipArrivalSeason = -1;

			this.processor.SegExtra.Value16 = 0x3772;

			for (int row = 0; row < 0xf; row++)
			{
				for (int column = 0; column < 0xc; column++)
				{
					this.processor.EmitUInt8(this.processor.SegExtra.Value16,
						(ushort)((0xb4 * participantIdx) + (0xc * row) + column + 0x16e0), 0xff);
				}
			}

			this.processor.Journal.CloseScope("WipeSpaceshipContent");
		}
	}
}
