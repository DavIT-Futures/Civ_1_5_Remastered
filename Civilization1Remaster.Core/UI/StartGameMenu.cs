using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class StartGameMenu : GameModule
	{
		private const ushort SoundDriverOptionAddress = 0x1a30;
		private const ushort PalaceSectionLevelsAddress = Palace.PalaceSectionLevelsAddress;
		private const ushort PalaceSectionStylesAddress = Palace.PalaceSectionStylesAddress;
		private const ushort ParticipantCountersAddress = 0xd20c;
		private const ushort LeaderEarthStartPositionTableAddress = 0x35da;
		private const ushort VgaPaletteBufferAddress = 0x6b34;
		private const ushort ImportedPaletteBufferAddress = 0xbdf4;
		private const ushort VgaUnitSpritesTableAddress = 0xb886;
		private const ushort AdvisorTitleTextTableAddress = 0x2fac;

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
			this.Processor.Journal.OpenScope("ConfigureFreshGame()");

			// The frame like the original (the functions called see the stack at the same depth)

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.Owner.Startup.DisplayMouseCursor();

			DrawingBox.TypefaceIdx = 0x7;

			ChooseDifficulty();

			ChooseCompetition();

			WipeWorld();

			ChooseTribe();

			StartCivilizations();

			WelcomeHumanPlayer();


			this.Processor.Journal.CloseScope("ConfigureFreshGame");
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
			this.Processor.Journal.OpenScope($"ConstructCivilization({participantIdx})");

			// The frame like the original (the functions called see the stack at the same depth)

			ushort result = 0;
			if (participantIdx != this.Owner.World.PersonParticipantIdx || this.Owner.World.RoundTally == 0)
			{
				result = RemakeCivilization(participantIdx);
			}

			this.Processor.Journal.CloseScope("ConstructCivilization");

			return result;
		}

		/// <summary>
		/// When the player (not 0, not the human player) has no city left: tells that the civilization was destroyed by
		/// destroyerIdx (dialog style 0x2f9e: 3 when by the human player, else 5 with relations bit 0x40 of the human player
		/// towards it, else 2), takes away its spaceship, units and everything it saw, notes it in the replay and makes a new
		/// civilization in its place
		/// </summary>
		public void EliminateCivilization(short participantIdx, short participantIdx1)
		{
			this.Processor.Journal.OpenScope($"EliminateCivilization({participantIdx}, {participantIdx1})");

			if (participantIdx != 0 && participantIdx != this.Owner.World.PersonParticipantIdx && !OwnsCity(participantIdx))
			{
				Message(CivilizationName(participantIdx))
					.Text("\ncivilization\ndestroyed\nby ")
					.Text(CivilizationPlural(participantIdx1))
					.Text("!\n");

				short humanIdx = this.Owner.World.PersonParticipantIdx;
				DialogStyle = (ushort)((this.Owner.World.Participants[humanIdx].Relations[participantIdx] & 0x40) != 0 ? 5 : 2);

				if (participantIdx1 == humanIdx)
				{
					DialogStyle = 3;
				}

				this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.Owner.Conquest.EliminateSpaceship(participantIdx);

				this.Owner.UnitOps.AttachReplayContent(13, (byte)participantIdx, (byte)participantIdx1);

				UnitMemory troops = new UnitMemory(this.Processor);

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (troops.Exists(participantIdx, troopIdx))
					{
						this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
				}

				sbyte unseen = (sbyte)~(1 << participantIdx);

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					for (int vertLoc = 0; vertLoc < 50; vertLoc++)
					{
						this.Owner.World.ChartSight[horizLoc, vertLoc] &= unseen;
					}
				}

				ConstructCivilization(participantIdx);
			}

			this.Processor.Journal.CloseScope("EliminateCivilization");
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
			this.Processor.Journal.OpenScope("CharacterizeStartLocation()");


			short[] points = new short[8];

			ushort participantIdx;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				this.Owner.MapOps.ObtainContinentID((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));
			}

			ushort scattered = 0;
			short best = 0;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
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

				points[participantIdx] = start;
			}

			short chosen = -1;

			for (participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				ushort troop = (ushort)(0x600 * participantIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				this.Owner.Words.Set("Skills: ");

				if ((short)(best - points[participantIdx]) > 3)
				{
					points[participantIdx] += 3;

					this.Owner.UnitOps.ConstructUnit((short)participantIdx, 0, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
						(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

					this.Owner.Words.Append("Settlers+");
				}

				for (ushort given = 0; (short)(best - points[participantIdx]) > (short)given; given++)
				{
					if (given != 0)
						this.Owner.Words.Append("+");

					this.Owner.Words.Append((ushort)((ushort)(this.Owner.Advisors.UncoverTechnology((short)participantIdx, 0) * 0x16) + 0x4da));
				}

				if (points[participantIdx] != best)
					continue;

				this.Owner.Words.Append("NONE");

				if (chosen == -1 || (scattered & (1 << (byte)participantIdx)) != 0)
				{
					chosen = (short)participantIdx;
				}
			}

			this.Processor.Journal.CloseScope("CharacterizeStartLocation");

			return (ushort)chosen;
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
			this.Processor.Journal.OpenScope("DisplayTribeSelection()");

			this.Owner.Startup.QueryForDisk(7, 1);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSp299Pic, 0);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0xa0, 0x32, 0xa0, 0x96,
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
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSp257PicB, 0);

				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileSp256Pal, 0xbdee);

				for (int idx = 0; idx < 0x30; idx++)
				{
					this.Processor.EmitGlobalByte((ushort)(VgaPaletteBufferAddress + idx), this.Processor.PullGlobalByte((ushort)(ImportedPaletteBufferAddress + idx)));
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

			PointerPicture = PointerIcon;

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

			this.Processor.Journal.CloseScope("DisplayTribeSelection");
		}

		/// <summary>
		/// Loads the government picture of the human player, "govtNa.pic": N is the government / 2 (3 for government 3), "m"
		/// instead of "a" with technology 20; the leader part of it goes into the picture 0x19e8. The file name is made in a
		/// buffer in the stack frame, like in the original.
		/// </summary>
		public void ImportGovernmentGraphics()
		{
			this.Processor.Journal.OpenScope("ImportGovernmentGraphics()");

			this.Owner.Startup.QueryForDisk(2, 1);

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;
			char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char techChar = regime != 3 && this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x14) != 0 ? 'm' : 'a';
			string docname = $"govt{regimeChar}{techChar}.pic";

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docname, 0);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0xa0, 0x3c,
				PictureScreenBox, 0xa0, 0x8c);

			this.Owner.Startup.QueryForDisk(0, 1);

			this.Processor.Journal.CloseScope("ImportGovernmentGraphics");
		}

		/// <summary>
		/// After a revolution of the human player (not to government 0): the newspaper "... government changed to ...!", then
		/// the advisor screen with the title 0x375a and the 4 advisors (pictures from 0x19e8, names at 0x2fac without their
		/// last character, every second one lower) until a key or click
		/// </summary>
		public void DisplayRevolutionScreen()
		{
			this.Processor.Journal.OpenScope("DisplayRevolutionScreen()");

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;

			if (regime != 0)
			{
				this.Owner.Advisors.RenderAdvisorScenery();

				Message(CivilizationName(this.Owner.World.PersonParticipantIdx))
					.Text(" government\nchanged to ");
				this.Owner.Words.Append(GovernmentName(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind));
				this.Owner.Words.Append("!\n");

				this.Owner.Paper.DisplayNewspaper(-2);

				this.Owner.Startup.ConcealMouseCursor();

				this.Owner.Services.FloodBox(ScreenBox, 0, 100, 320, 100, Hues.White);

				DrawingBox.TypefaceIdx = 0x6;

				this.Owner.TextPainter.RenderCenteredTextToScreen0(GameTexts.TextNewCabinet, 160, 102, Hues.Black);

				DrawingBox.TypefaceIdx = 0x2;

				for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
				{
					this.Owner.VideoCard.RenderImage(PictureScreenBox, (ushort)((0x28 * advisorIdx) + 0xa0), 0x8c, 0x28, 0x3c,
						ScreenBox, (ushort)((0x50 * advisorIdx) + 0x14), 0x76);

					this.Owner.Words.Set(this.Processor.PullGlobalWord((ushort)((advisorIdx << 1) + AdvisorTitleTextTableAddress)));
					this.Owner.Words[this.Owner.Words.Length - 1] = 0x0;

					this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), (80 * advisorIdx) + 40, (advisorIdx & 1) != 0 ? 186 : 180, Hues.Black);
				}

				DrawingBox.TypefaceIdx = 0x1;

				this.Owner.Startup.DisplayMouseCursor();

				this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.Owner.Advisors.RecoverAdvisorScenery();
			}

			this.Processor.Journal.CloseScope("DisplayRevolutionScreen");
		}

		/// <summary>
		/// The player has no spaceship: no arrival, every place of the spaceship layout (15 rows of 12, segment 0x3772) empty
		/// </summary>
		public void WipeSpaceshipContent(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"WipeSpaceshipContent({participantIdx})");

			this.Owner.World.Participants[participantIdx].StarshipArrivalSeason = -1;

			this.Processor.SegExtra.Value16 = 0x3772;

			for (int row = 0; row < 0xf; row++)
			{
				for (int column = 0; column < 0xc; column++)
				{
					this.Processor.EmitUInt8(this.Processor.SegExtra.Value16,
						(ushort)((0xb4 * participantIdx) + (0xc * row) + column + 0x16e0), 0xff);
				}
			}

			this.Processor.Journal.CloseScope("WipeSpaceshipContent");
		}

		/// <summary>
		/// The difficulty menu (a click on a portrait of the left picture chooses too), none = 0; the portrait of the
		/// difficulty on the left
		/// </summary>
		private void ChooseDifficulty()
		{
			this.Owner.Words.Set("Difficulty Level...\n Chieftain (easiest)\n Warlord\n Prince\n King\n Emperor (toughest)\n");

			this.Owner.World.HardnessTier = (short)this.Owner.Dialogs.DisplayMenu(TextBuffer.Address, 160, 35, 1);

			if (MousePresent != 0 && MenuMouseUsed != 0)
			{
				this.Owner.Startup.ObtainMouseState();

				if ((short)this.Owner.GlobDb3c < 0x87)
				{
					this.Owner.World.HardnessTier = (short)this.Owner.Helpers.VerifyValueSpan(((short)this.Owner.GlobDb3e - 12) / 35, 0, 4);
				}
			}

			if (this.Owner.World.HardnessTier == -1)
			{
				this.Owner.World.HardnessTier = 0;
			}

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 150, 200, Hues.Black);

			RenderDifficultyPortrait(0x14, 0x64);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				ScreenBox, 0x96, 0);

			this.Owner.Startup.DisplayMouseCursor();
		}

		/// <summary>
		/// The portrait of the difficulty (left column for even levels, right for odd ones) at (horizLoc, vertLoc)
		/// </summary>
		private void RenderDifficultyPortrait(ushort horizLoc, ushort vertLoc)
		{
			ushort column = (ushort)((this.Owner.World.HardnessTier & 1) != 0 ? 0x50 : 0x15);

			this.Owner.VideoCard.RenderImage(BackScreenBox, column,
				(ushort)((0x23 * this.Owner.World.HardnessTier) + 6), 0x35, 0x2f, ScreenBox, horizLoc, vertLoc);
		}

		/// <summary>
		/// The number of civilizations (7 - 3, asked again for none): the rivals (2 - 6), the living players and as many
		/// portraits stacked
		/// </summary>
		private void ChooseCompetition()
		{
			this.Owner.Words.Set("Level of Competition...\n ");

			for (short tally = 7; tally > 2; tally--)
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(tally, 10));

				this.Owner.Words.Append(" Civilizations\n ");
			}

			do
			{
				this.Owner.World.BotRivalTally = (short)this.Owner.Dialogs.DisplayMenu(TextBuffer.Address, 160, 35, 1);
			}
			while ((ushort)this.Owner.World.BotRivalTally == 0xffff);

			this.Owner.World.BotRivalTally = (short)this.Owner.Helpers.VerifyValueSpan(6 - this.Owner.World.BotRivalTally, 2, 6);
			this.Owner.World.LiveRealms = (short)(0xff >> (byte)(6 - (byte)this.Owner.World.BotRivalTally));

			this.Owner.Startup.ConcealMouseCursor();

			for (short rivalIdx = this.Owner.World.BotRivalTally; rivalIdx >= 0; rivalIdx--)
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
			this.Owner.World.Season = -4000;
			this.Owner.World.DiagMarkers = 0xf;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				this.Owner.World.Towns[townIdx].LabelIdx = 0xff;
				this.Owner.World.Towns[townIdx].ConditionMarker = 0xff;
			}

			for (int idx = 0; idx < 256; idx++)
			{
				this.Owner.World.TownPlaces[idx].X = -1;
			}

			for (int scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				this.Owner.World.ScienceLeadingFoundVia[scienceIdx] = 0;
			}

			for (int marvelIdx = 0; marvelIdx <= 0x15; marvelIdx++)
			{
				this.Owner.World.MarvelTownIdx[marvelIdx] = -1;
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.Owner.World.Participants[participantIdx].ChiefVisuals = -1;

				WipeSpaceshipContent(participantIdx);
			}

			for (int horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					this.Owner.World.ChartSight[horizLoc, vertLoc] = 0;
				}
			}

			this.Owner.World.SessionPrefMarkers = 0xfa;

			if (this.Processor.PullGlobalByte(SoundDriverOptionAddress) == 0x4e)
			{
				this.Owner.World.SessionPrefMarkers &= 0x7fef;
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
			this.Owner.Words.Set("Pick your tribe...\n ");

			_ = this.Owner.Words.Length;

			for (int leader = 1; leader < this.Owner.World.BotRivalTally + 2; leader++)
			{
				this.Owner.Words.Append(LeaderText(leader, LeaderTable.Civilization));

				this.Owner.Words.Append("\n ");
			}

			for (int leader = 1; leader < this.Owner.World.BotRivalTally + 2; leader++)
			{
				this.Owner.Words.Append((ushort)((0x3a * leader) + 0x16d2));

				this.Owner.Words.Append("\n ");
			}

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0x96, 0, 0xa0, 0xc8,
				ScreenBox, 0x96, 0);

			this.Owner.Startup.DisplayMouseCursor();

			// The line: 0 - rivals the first leaders of slots 1 - rivals + 1, then the second leaders (slot + 8)
			this.Owner.World.PersonParticipantIdx = (short)this.Owner.Dialogs.DisplayMenu(TextBuffer.Address, 160, 35, 1);

			if (this.Owner.World.PersonParticipantIdx <= this.Owner.World.BotRivalTally)
			{
				this.Owner.World.PersonParticipantIdx++;
			}
			else
			{
				this.Owner.World.PersonParticipantIdx = (short)(this.Owner.World.PersonParticipantIdx - this.Owner.World.BotRivalTally + 8);
			}

			if (this.Owner.World.HardnessTier == 0)
			{
				this.Owner.World.SessionPrefMarkers |= 1;
			}

			// The slot with the leader bit, made 0 - 7 only below (like the original)
			this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx & 7].PresentInquiryIdx = -1;

			this.Owner.World.FollowingCollectionRound += (short)this.Owner.Libc.Dice.DrawInt(50);

			for (int idx = 0; idx < 0xc; idx++)
			{
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (idx << 1)), 0xffff);
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionStylesAddress + (idx << 1)), 0x0);
			}

			for (int idx = 3; idx <= 5; idx++)
			{
				this.Processor.EmitGlobalWord((ushort)(0xd92a + (idx << 1)), 0);
				this.Processor.EmitGlobalWord((ushort)(PalaceSectionLevelsAddress + (idx << 1)), 0);
			}

			bool randomTribe = this.Owner.World.PersonParticipantIdx == 0;
			bool secondLeader = false;

			if (randomTribe)
			{
				this.Owner.World.PersonParticipantIdx = (short)(this.Owner.Libc.Dice.DrawInt(this.Owner.World.BotRivalTally) + 1);
			}
			else
			{
				secondLeader = this.Owner.World.PersonParticipantIdx > 7;
				this.Owner.World.PersonParticipantIdx &= 7;
			}

			short personIdx = this.Owner.World.PersonParticipantIdx;
			ushort names = (ushort)(personIdx << 1);

			this.Processor.EmitGlobalByte(CivilizationNameAt(names), 0x0);

			if (randomTribe)
			{
				this.Owner.CityNaming.DisplayCityFoundedMessage();

				this.Owner.World.Participants[personIdx].ChiefVisuals = personIdx;

				this.Owner.Names.Set(LeaderNameAt(names), LeaderText(personIdx, LeaderTable.Name));
			}
			else
			{
				Player person = this.Owner.World.Participants[personIdx];

				person.ChiefVisuals = (short)(secondLeader ? personIdx + 8 : personIdx);

				ushort leader = (ushort)(0x3a * person.ChiefVisuals);

				this.Owner.Names.Set(LeaderNameAt(names), LeaderTextAt(leader, LeaderTable.Name));

				this.Owner.Names.Set(CivilizationNameAt(names), LeaderTextAt(leader, LeaderTable.Civilization));

				this.Owner.Words.Set(LeaderTextAt(leader, LeaderTable.CivilizationPlural));

				if (this.Owner.Words[0] == 0)
				{
					Message(CivilizationNameAt(names))
						.Text("s");
				}

				this.Owner.Names.Set(CivilizationPluralAt(names), this.Owner.Words.ToString());
			}

			this.Owner.TextPainter.RenderCenteredTextToScreen0(CivilizationPluralAt(names), 46, 92, Hues.White);
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

				// The ruined units of the new civilization cleared (the translated code cleared player 6, the counter of an
				// earlier loop: TurnEndRules.RuinedUnitsCleared)
				int cleared = TurnEndRules.RuinedUnitsCleared(participantIdx, this.Owner.TurnHooks.ClassicFreshGameRuins);

				for (int kind = 0; kind < 8; kind++)
				{
					this.Owner.World.Participants[cleared].TroopsRuined[kind] = 0;
				}
			}

			CharacterizeStartLocation();

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

			this.Owner.World.ParticipantMarkers = (short)(1 << this.Owner.World.PersonParticipantIdx);
			person.LevyRatio = 5;
			person.ResearchLevyRatio = 5;

			if (this.Owner.World.HardnessTier == 0)
			{
				person.Gold = 50;
			}

			this.Owner.World.ParticipantIdentMarkers = 0;

			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if (this.Owner.World.Participants[participantIdx].ChiefVisuals >= 8)
				{
					this.Owner.World.ParticipantIdentMarkers |= (short)(1 << participantIdx);
				}
			}
		}

		/// <summary>
		/// The welcome: the found city message, the text 0x35c3 with the names and the technologies the human player starts
		/// with (two a line, then "and Roads."), the leader's music, the difficulty portrait; waits for a key or a click
		/// </summary>
		private void WelcomeHumanPlayer()
		{
			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 150, 0, 170, 200, Hues.Black);

			this.Owner.CityNaming.DisplayFoundCityMessage();

			this.Owner.Startup.DisplayMouseCursor();

			DrawingBox.TypefaceIdx = 0x1;

			ushort names = (ushort)(this.Owner.World.PersonParticipantIdx << 1);

			this.Owner.Codes.Set(3, LeaderNameAt(names));

			this.Owner.Codes.Set(0, CivilizationPluralAt(names));

			this.Owner.Words.Clear();

			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicInit);

			_ = this.Owner.Words.Length;

			int shown = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, scienceIdx) == 0)
					continue;

				this.Owner.Words.Append(TechnologyText(scienceIdx));

				this.Owner.Words.Append(", ");

				shown++;

				if (shown == 2)
				{
					this.Owner.Words.Append("\n");
				}
			}

			this.Owner.Words.Append("and Roads.\n");

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals);

			this.Owner.Services.PerformSound(LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.White);

			this.Owner.UnitOps.RenderBusyIndicator();

			RenderDifficultyPortrait(0x86, 0x14);

			this.Owner.BlockText.RenderTextChunk(99, 88, 81, 0);

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.Conquest.AwaitForKeyPressOrMouseClick();
		}

		/// <summary>The work of ConstructCivilization; 0 when the civilization does not come</summary>
		private ushort RemakeCivilization(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];

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
					foreignTowns[continentIdx] += (ushort)this.Owner.World.Participants[otherIdx].Landmasses[continentIdx].TownTally;
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

				this.Troops.AssignAt(troop, UnitMemory.Kind, 0xff);
				this.Troops.AssignAt(troop, UnitMemory.Status, 0x0);
			}

			this.Processor.EmitGlobalWord((ushort)(ParticipantCountersAddress + (participantIdx << 1)), 0);
			player.Tally = 0;

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Services.FloodBox(PictureScreenBox, 240, 0, 80, 50, Hues.Black);
			}

			this.Owner.Bot.WipeStrategicLocations(participantIdx);

			for (int otherIdx = 0; otherIdx < 8; otherIdx++)
			{
				player.Relations[otherIdx] = 0;
				this.Owner.World.Participants[otherIdx].Relations[participantIdx] = 0;
			}

			for (int idx = 0; idx < 5; idx++)
			{
				player.FoundScienceMarkers[idx] = 0;
			}

			if (this.Owner.World.RoundTally != 0)
			{
				ShareKnownTechnologies(participantIdx);
			}

			if (participantIdx != 0 && (this.Owner.World.LiveRealms & (1 << participantIdx)) != 0)
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
			short personIdx = this.Owner.World.PersonParticipantIdx;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0)
					continue;

				int finder = (byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7;

				if (finder == 0 || finder == (ushort)personIdx)
					continue;

				if (this.Owner.Libc.Dice.DrawInt(2) == 0)
				{
					this.Owner.Advisors.EarnTechnology(participantIdx, scienceIdx, participantIdx);
				}
			}
		}

		/// <summary>
		/// The leader, the start place, the names and the settler of a living player (not 0); false when a new civilization
		/// in a running game does not come (then the player is dead)
		/// </summary>
		private bool SettleCivilization(short participantIdx, ushort[] foreignTowns)
		{
			Player player = this.Owner.World.Participants[participantIdx];

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				if (this.Owner.World.RoundTally == 0)
				{
					ChooseFreeLeader(participantIdx);
				}
				else
				{
					player.ChiefVisuals ^= 8;
				}
			}

			(short horizLoc, short vertLoc, ushort attemptTally) = ChooseStartPlace(participantIdx, foreignTowns);

			if (this.Owner.World.RoundTally == 0 && EarthGame != 0)
			{
				// EARTH: the start place of the leader
				ushort place = (ushort)(player.ChiefVisuals << 1);

				horizLoc = (sbyte)this.Processor.PullGlobalByte((ushort)(place + LeaderEarthStartPositionTableAddress));
				vertLoc = (sbyte)this.Processor.PullGlobalByte((ushort)(place + LeaderEarthStartPositionTableAddress + 1));
				attemptTally = 0;
			}

			if (this.Owner.World.RoundTally != 0)
			{
				// A new civilization must have the other leader of its slot than the one there before (identity bit)
				bool identity = (this.Owner.World.ParticipantIdentMarkers & (1 << participantIdx)) != 0;

				if (player.ChiefVisuals >= 8 && identity)
				{
					attemptTally = 0x7d0;
				}

				if (player.ChiefVisuals < 8 && !identity)
				{
					attemptTally = 0x7d0;
				}
			}

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				player.ResearchLevyRatio = (short)(LeaderWord(player.ChiefVisuals, LeaderTable.Leaning) + 3);
				player.LevyRatio = (short)(9 - player.ResearchLevyRatio);
			}

			ushort latestRound = (ushort)(0x15e - (0x32 * this.Owner.World.HardnessTier));

			if ((latestRound < this.Owner.World.RoundTally || attemptTally >= 0x7d0) &&
				participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				// It does not come: the other leader back, the player dead
				player.ChiefVisuals ^= 8;
				this.Owner.World.LiveRealms &= (short)~(1 << participantIdx);

				if ((ushort)((1 << this.Owner.World.PersonParticipantIdx) | 1) == (ushort)this.Owner.World.LiveRealms)
				{
					GameEndRequest = 1;
					ConquestEnd = 1;
				}

				return false;
			}

			NameCivilization(participantIdx);

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);

			this.Owner.UnitOps.ConstructUnit(participantIdx, 0, horizLoc, vertLoc);

			player.HorizBegin = horizLoc;

			// The barbarian units near the start place are removed
			for (short troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)(0xc * troopIdx);

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff)
					continue;

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
					vertLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

				if (distance <= 8)
				{
					this.Owner.UnitOps.EliminateUnit(0, troopIdx);
				}
			}

			return true;
		}

		/// <summary>
		/// At the start of a game: a random leader (0 - 15) of the player's colour (leader & 7) that no other player has
		/// </summary>
		private void ChooseFreeLeader(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			bool free;

			do
			{
				player.ChiefVisuals = (short)this.Owner.Libc.Dice.DrawInt(16);

				free = (player.ChiefVisuals & 7) == participantIdx;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if (otherIdx != participantIdx && this.Owner.World.Participants[otherIdx].ChiefVisuals == player.ChiefVisuals)
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
				short horizLoc = (short)(this.Owner.Libc.Dice.DrawInt(64) + 8);
				short vertLoc = (short)(this.Owner.Libc.Dice.DrawInt(34) + 8);

				ushort townIdx = this.Owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
				short nearest = townIdx == 0xffff ? (short)0x1e : (short)this.Owner.Helpers.ObtainDistance(horizLoc,
					this.Owner.World.Towns[townIdx].Place.X, vertLoc, this.Owner.World.Towns[townIdx].Place.Y);

				if (this.Owner.World.RoundTally == 0)
				{
					for (int otherIdx = 1; otherIdx < participantIdx; otherIdx++)
					{
						ushort troop = (ushort)(0x600 * otherIdx);

						short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
							vertLoc, (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert));

						if (distance < nearest)
						{
							nearest = distance;
						}
					}
				}

				short landValue = (short)this.Owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);

				attemptTally++;

				if ((short)attemptTally > 0x7d0 &&
					(participantIdx != this.Owner.World.PersonParticipantIdx || this.Owner.World.RoundTally != 0))
				{
					return (horizLoc, vertLoc, attemptTally);
				}

				if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
					continue;

				if ((short)(0xc - ((short)attemptTally / 32)) > landValue)
					continue;

				short relief = (short)((short)attemptTally / 64);

				if (nearest < (short)(0xa - relief))
					continue;

				ushort continentIdx = this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
				short siteDemand = (short)(0x20 - (this.Owner.World.RoundTally / 16) - relief);

				if (this.Owner.World.Landmasses[continentIdx].ConstructionSpotTally < siteDemand)
					continue;

				if (this.Owner.World.Season > 0 && foreignTowns[this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc)] != 0)
					continue;

				if (this.Owner.MapOps.ContainsHut(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc), horizLoc, vertLoc) != 0)
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
			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[participantIdx].ChiefVisuals);
			ushort names = (ushort)(participantIdx << 1);

			this.Owner.Names.Set(LeaderNameAt(names), LeaderTextAt(leader, LeaderTable.Name));

			if (participantIdx == this.Owner.World.PersonParticipantIdx &&
				this.Processor.PullGlobalByte(CivilizationNameAt(names)) != 0)
				return;

			this.Owner.Names.Set(CivilizationNameAt(names), LeaderTextAt(leader, LeaderTable.Civilization));

			this.Owner.Words.Set(LeaderTextAt(leader, LeaderTable.CivilizationPlural));

			if (this.Owner.Words[0] == 0)
			{
				Message(CivilizationNameAt(names))
					.Text("s");
			}

			this.Owner.Names.Set(CivilizationPluralAt(names), this.Owner.Words.ToString());
		}

		/// <summary>True when one of the 128 cities belongs to the player</summary>
		private bool OwnsCity(short participantIdx)
		{
			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker != 0xff && this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					return true;
			}

			return false;
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

				if (this.Troops.PullAt(other, UnitMemory.Kind) == 0xff)
					continue;

				ushort otherContinent = this.Owner.MapOps.ObtainContinentID((sbyte)this.Troops.PullAt(other, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(other, UnitMemory.Vert));

				if (this.Owner.MapOps.ObtainContinentID((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert)) != otherContinent)
					continue;

				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(other, UnitMemory.Horiz),
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert), (sbyte)this.Troops.PullAt(other, UnitMemory.Vert));

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

			this.Owner.Words.Set("Nearest civilization: ");

			if (nearest < 10)
			{
				this.Owner.Words.Append("close");
			}
			else if (nearest < 0x14)
			{
				this.Owner.Words.Append("moderate");
				start++;
			}
			else if (nearest != 0x3e7)
			{
				this.Owner.Words.Append("far away");
				start += 2;
			}
			else
			{
				this.Owner.Words.Append("isolated");
				start += 4;
			}

			ushort ocean = 0, grassland = 0, river = 0;
			ushort neighbourIdx;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz) + (short)DirectionHoriz(neighbourIdx),
					(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert) + (short)DirectionVert(neighbourIdx));


				if (terrain == 0xa)
					ocean++;

				if (terrain == 2)
					grassland++;

				if (terrain == 0xb)
					river++;
			}


			this.Owner.Words.Set(ocean != 0 ? (ushort)0x3653 : (ushort)0x365c);

			if ((short)river > 1)
			{
				this.Owner.Words.Append("river valley on a ");
				start += 2;
			}
			else if ((short)grassland >= 3)
			{
				this.Owner.Words.Append("grasslands on a ");
				start++;
			}
			else
			{
				this.Owner.Words.Append("plains on a ");
			}

			switch (CPU.SignedQuotient((short)this.Owner.MapOps.ObtainLandMassSize((sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz), (sbyte)this.Troops.PullAt(troop, UnitMemory.Vert)), 0x32))
			{
				case 0:
					this.Owner.Words.Append(0x3695);
					break;

				case 1:
					this.Owner.Words.Append(0x36a3);
					break;

				case 2:
					this.Owner.Words.Append(0x36b1);
					start++;
					break;

				case 3:
					this.Owner.Words.Append(0x36c2);
					start++;
					break;

				default:
					this.Owner.Words.Append("large continent.");
					start += 2;
					break;
			}


			return start;
		}

		/// <summary>
		/// Cuts a picture out of the screen into the table at tableAddr, entry idx
		/// </summary>
		private void StorePicture(ushort tableAddr, int idx, ushort displayIdx, ushort horizLoc, ushort vertLoc, ushort breadth, ushort altitude)
		{
			ushort picture = this.Owner.VideoCard.ScreenToPicture(displayIdx, horizLoc, vertLoc, breadth, altitude);

			this.Processor.EmitGlobalWord((ushort)(tableAddr + (idx << 1)), picture);
		}

		/// <summary>
		/// The VGA unit sprites (sheet 0x3709, 10 rows of 16 at 0xb886, 0x20 bytes a row), the quarter tiles (8 of 4 at 0xd294)
		/// and 4 tiles (0xd2d4)
		/// </summary>
		private void CutVgaUnitSprites()
		{
			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileTer257Pic, 0);

			for (int row = 0; row < 0xa; row++)
			{
				for (int column = 0; column < 0x10; column++)
				{
					ushort entry = (ushort)(VgaUnitSpritesTableAddress + (row << 5) + (column << 1));

					// Rows 6 - 9 (and all rows with 0x1a3e) have one sprite, repeated in every column
					if ((row >= 6 || LowMemoryVga != 0) && column != 0)
					{
						this.Processor.EmitGlobalWord(entry, this.Processor.PullGlobalWord((ushort)(entry - 2)));
					}
					else
					{
						this.Processor.EmitGlobalWord(entry,
							this.Owner.VideoCard.ScreenToPicture(1, (ushort)(column << 4), (ushort)(row << 4), 0x10, 0x10));
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
			this.Owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, GameTexts.FileSpritesPicB, 1);

			this.Owner.VideoCard.RenderImage(SpriteScreenBox, 0, 0, 0x140, 0xc8,
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
			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0xa0, 0x140, 0x20,
				BackScreenBox, 0, 0x80);

			ushort colours = (ushort)(playerIdx << 1);
			ushort primaryColour = PlayerColourAt(colours);
			ushort secondaryColour = PlayerSecondaryColourAt(colours);

			if (primaryColour == 0xf)
			{
				this.Owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, Hues.White, Hues.LightCyan);
			}

			if (primaryColour == 0x7)
			{
				this.Owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, Hues.LightGray, Hues.Cyan);
			}

			this.Owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, Hues.LightGreen,
				(byte)primaryColour);

			this.Owner.VideoCard.SubstituteHue(BackScreenBox, 0, 128, 320, 32, Hues.Green,
				(byte)secondaryColour);

			for (int idx = 0; idx < 0x1c; idx++)
			{
				StorePicture(0xd54e, (playerIdx << 5) + idx, 1, (ushort)(((idx % 0x14) << 4) + 1), (ushort)(((idx / 0x14) << 4) + 0x81), 15, 15);
			}
		}
	}
}
