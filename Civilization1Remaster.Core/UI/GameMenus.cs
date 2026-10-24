using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2c84.
	/// </summary>
	public class GameMenus : GameModule
	{
		public GameMenus(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Opens a menu of the menu bar (marker 0 - 4: Game, Orders, Advisors, World, Civilopedia; -1: the one under the mouse,
		/// column / 60), with the screen kept in the back screen 0x19d4. Afterwards, by the menu's result in 0x654a: 1 draws the
		/// map screen again, 0 puts the screen back.
		/// </summary>
		public void ManageMenuBar(short participantIdx, short troopIdx, short marker)
		{
			this.processor.Journal.OpenScope($"ManageMenuBar({participantIdx}, {troopIdx}, {marker})");

			this.processor.EmitGlobalWord(0xd4ca, 0xffff);
			this.processor.EmitGlobalWord(0x654a, 0x0);

			if (marker == -1)
			{
				marker = (short)this.owner.Helpers.VerifyValueSpan((short)this.owner.GlobDb3c / 60, 0, 4);
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 320, 200,
				BackScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			switch (marker)
			{
				case 0:
					DisplayGameMenu();
					break;

				case 1:
					DisplayOrdersMenu(participantIdx, troopIdx);
					break;

				case 2:
					DisplayAdvisorMenu();
					break;

				case 3:
					DisplayWorldMenu();
					break;

				case 4:
					DisplayCivilopediaMenu();
					break;
			}

			this.owner.Startup.ConcealMouseCursor();

			if (this.processor.PullGlobalWord(0x654a) == 1)
			{
				this.owner.TurnHooks.RepaintMainScreen();
			}

			if (this.processor.PullGlobalWord(0x654a) == 0)
			{
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("ManageMenuBar");
		}

		/// <summary>
		/// The Game menu: Tax Rate, Luxuries Rate, FindCity, Save Game, REVOLUTION!, Retire and QUIT to DOS are passed to the
		/// turn as commands (0xd4ca, the last two with 0xdc48), Load Game chooses the saved game to load after the turn, Options
		/// toggles the options until Escape, View Replay (after the end of the game, 0x100 of the spaceships) shows the replay.
		/// </summary>
		public void DisplayGameMenu()
		{
			this.processor.Journal.OpenScope("DisplayGameMenu()");

			if (owner.World.RoundTally == 0)
			{
				DisabledLines = 0x10;
			}

			this.owner.Libc.CopyText(0xba06, " Tax Rate\n Luxuries Rate\n FindCity\n Options\n Save Game\n Load Game\n REVOLUTION!\n \n Retire\n QUIT to DOS\n");

			if ((this.owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.owner.Libc.AppendText(0xba06, " View Replay\n");
			}

			ushort choice = this.owner.Dialogs.DisplayMenu(0xba06, 16, 8, 0);

			this.owner.TurnLogic.AwaitForKey();

			// Load Game (5) is new, the items after it moved down by one
			switch (choice)
			{
				case 0:
					this.processor.EmitGlobalWord(0xd4ca, 0x3d);
					break;

				case 1:
					this.processor.EmitGlobalWord(0xd4ca, 0x2d);
					break;

				case 2:
					this.processor.EmitGlobalWord(0xd4ca, 0x3f);
					break;

				case 3:
					ChooseOptions();
					break;

				case 4:
					this.processor.EmitGlobalWord(0xd4ca, 0x53);
					break;

				case 5:
					// The saved game is chosen now and loaded by the main loop after the turn is left,
					// the same way Quit leaves the turn
					this.owner.SaveLoad.FetchBay = this.owner.SaveLoad.ChooseGameToImport();

					if (this.owner.SaveLoad.FetchBay != 0xffff)
					{
						GameEndRequest = GameLoadAndSave.FetchSessionAsk;
						this.processor.EmitGlobalWord(0xd4ca, 0x1000);
					}
					break;

				case 6:
					this.processor.EmitGlobalWord(0xd4ca, 0xfffe);
					break;

				case 8:
					GameEndRequest = 0x2;
					this.processor.EmitGlobalWord(0xd4ca, 0x1000);
					break;

				case 9:
					GameEndRequest = 0x1;
					this.processor.EmitGlobalWord(0xd4ca, 0x1000);
					break;

				case 10:
					this.owner.Replayer.DisplayReplay();
					break;
			}

			// Far return
			this.processor.Journal.CloseScope("DisplayGameMenu");
		}

		/// <summary>
		/// The Options menu: each chosen option is toggled (0xd7f2 shows the options) and the menu stays on it, until Escape
		/// (while 0x2f9c is set, the menu comes back even after Escape and toggles nothing)
		/// </summary>
		private void ChooseOptions()
		{
			this.owner.Libc.CopyText(0xba06, "Options:\n Instant Advice\n AutoSave\n End of Turn\n Animations\n Sound\n Enemy Moves\n Civilopedia Text\n Palace\n");

			ushort choice;

			do
			{
				this.processor.EmitGlobalWord(0xd7f2, (ushort)this.owner.World.SessionPrefMarkers);

				choice = this.owner.Dialogs.DisplayMenu(0xba06, 24, 16, 0);

				if (choice != 0xffff)
				{
					if (this.processor.PullGlobalWord(0x2f9c) == 0)
					{
						this.owner.World.SessionPrefMarkers ^= (short)(1 << (byte)choice);
					}

					this.processor.EmitGlobalWord(0x2f9a, choice);
				}
			}
			while (choice != 0xffff || this.processor.PullGlobalWord(0x2f9c) != 0);
		}

		/// <summary>
		/// The Orders menu of the unit (not for -1 and 128): No Orders, for settlers the city, road / railroad, irrigation or
		/// the change of terrain (0x44a), mines or the change (0x44e), pollution, fortress (needs technology 0x2d); Fortify for
		/// land units; Wait, Sentry, GoTo; Pillage on an improved tile; Home City in a city; Unload for transports (role 5 or
		/// type 0x17) with units; Disband. Lines that cannot be chosen are set in 0xb276. The key of the chosen line goes to 0xd4ca
		/// (-1 when none).
		/// </summary>
		public void DisplayOrdersMenu(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayOrdersMenu({participantIdx}, {troopIdx})");

			// DisplayMenu leaves SI in DI: both are given back to the caller as they were
			ushort source = this.processor.RegSource.Value16;
			ushort dest = this.processor.RegDest.Value16;

			if (troopIdx != -1 && troopIdx != 128)
			{
				ChooseOrder(participantIdx, troopIdx);
			}

			this.processor.RegSource.Value16 = source;
			this.processor.RegDest.Value16 = dest;

			// Far return
			this.processor.Journal.CloseScope("DisplayOrdersMenu");
		}

		private void ChooseOrder(short participantIdx, short troopIdx)
		{
			UnitMemory troops = new UnitMemory(this.processor);
			List<byte> keys = new List<byte>();

			this.processor.EmitGlobalByte(0xba06, 0x0);

			short horizLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			short vertLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);

			byte improvements = (byte)this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.owner.Libc.AppendText(0xba06, " No Orders \x008fspace\n");
			keys.Add(0x20);

			DisabledLines = 0x0;

			if (troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0)
			{
				// Settlers
				this.owner.Libc.AppendText(0xba06, (improvements & 1) != 0 ? (ushort)0x2cbc : (ushort)0x2ccd);
				keys.Add(0x62);

				if ((improvements & 0x8) == 0)
				{
					this.owner.Libc.AppendText(0xba06, " Build Road \x008fr\n");
					keys.Add(0x72);
				}
				else if ((improvements & 0x10) == 0 && this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31) != 0)
				{
					this.owner.Libc.AppendText(0xba06, " Build RailRoad \x008fr\n");
					keys.Add(0x72);
				}

				if ((improvements & 0x2) == 0)
				{
					ushort irrigation = (ushort)((0xc * terrain) + 0x44a);

					if (this.processor.PullGlobalWord(irrigation) == 0xfffe)
					{
						this.owner.Libc.AppendText(0xba06, " Build Irrigation");

						if (this.owner.TurnLogic.VerifyCoastalTile(horizLoc, vertLoc) == 0)
						{
							DisabledLines = (ushort)(DisabledLines | (1 << (byte)keys.Count));
						}
					}
					else if ((short)this.processor.PullGlobalWord(irrigation) >= 0)
					{
						AppendChangeOfTerrain(irrigation);
					}

					if (this.processor.PullGlobalWord(irrigation) != 0xffff)
					{
						keys.Add(0x69);
						this.owner.Libc.AppendText(0xba06, " \x008fi\n");
					}
				}

				if ((improvements & 0x4) == 0)
				{
					ushort mining = (ushort)((0xc * terrain) + 0x44e);

					if ((short)this.processor.PullGlobalWord(mining) <= -2)
					{
						this.owner.Libc.AppendText(0xba06, 0x2d28);
					}
					else if ((short)this.processor.PullGlobalWord(mining) >= 0)
					{
						AppendChangeOfTerrain(mining);
					}

					if (this.processor.PullGlobalWord(mining) != 0xffff)
					{
						keys.Add(0x6d);
						this.owner.Libc.AppendText(0xba06, " \x008fm\n");
					}
				}

				if ((improvements & 0x40) != 0)
				{
					this.owner.Libc.AppendText(0xba06, " Clean up Pollution \x008fp\n");
					keys.Add(0x70);
				}
			}

			if (troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0)
			{
				this.owner.Libc.AppendText(0xba06, " Build Fortress \x008ff\n");

				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d) == 0)
				{
					DisabledLines = (ushort)(DisabledLines | (1 << (byte)keys.Count));
				}
			}
			else if (troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == 0)
			{
				this.owner.Libc.AppendText(0xba06, " Fortify \x008ff\n");
			}

			if (troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == 0)
			{
				keys.Add(0x66);
			}

			this.owner.Libc.AppendText(0xba06, " Wait \x008fw\n Sentry \x008fs\n GoTo\n");
			keys.Add(0x77);
			keys.Add(0x73);
			keys.Add(0x67);

			if ((improvements & 0x1e) != 0)
			{
				sbyte kind = troops.TroopKind(participantIdx, troopIdx);

				if (kind < 0x1a && kind != 0xe)
				{
					this.owner.Libc.AppendText(0xba06, " Pillage \x008fP\n");
					keys.Add(0x50);
				}
			}

			if ((improvements & 0x1) != 0)
			{
				this.owner.Libc.AppendText(0xba06, " Home City \x008fh\n");
				keys.Add(0x68);
			}

			if ((troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == 5 || troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0x17) &&
				troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) != 0xff)
			{
				this.owner.Libc.AppendText(0xba06, " Unload \x008fu\n");
				keys.Add(0x75);
			}

			this.owner.Libc.AppendText(0xba06, " \n Disband Unit \x008fD\n");
			keys.Add(0x0);
			keys.Add(0x44);

			short choice = (short)this.owner.Dialogs.DisplayMenu(0xba06, 72, 8, 0);

			this.processor.EmitGlobalWord(0xd4ca, choice >= 0 && choice < keys.Count ? (ushort)(short)(sbyte)keys[choice] : (ushort)0xffff);
		}

		/// <summary>" Change to " and the name of the terrain the work makes (the word at the address)</summary>
		private void AppendChangeOfTerrain(ushort work)
		{
			this.owner.Libc.AppendText(0xba06, " Change to ");

			this.owner.Libc.AppendText(0xba06,
				(ushort)((0x13 * this.processor.PullGlobalWord((ushort)((this.processor.PullGlobalWord(work) << 1) + 0x2ba6))) + 0x282));
		}

		/// <summary>
		/// The Advisors menu: City Status, Military (the score list when all units are shown, 0xd806, else the unit counts),
		/// Intelligence, Attitude, Trade and Science; the screen is put back from 0x19d4 before the chosen report
		/// </summary>
		public void DisplayAdvisorMenu()
		{
			this.processor.Journal.OpenScope("DisplayAdvisorMenu()");

			Message(" City Status (F1)\n Military Advisor (F2)\n Intelligence Advisor (F3)\n Attitude Advisor (F4)\n")
				.Text(" Trade Advisor (F5)\n Science Advisor (F6)\n");

			ushort choice = this.owner.Dialogs.DisplayMenu(0xba06, 112, 8, 0);

			RestoreMenuScreen();

			short personIdx = this.owner.World.PersonParticipantIdx;

			switch (choice)
			{
				case 0:
					this.owner.Briefings.DisplayCityStatusReport(personIdx);
					break;

				case 1:
					if (MapRevealed != 0)
					{
						this.owner.RealmInfo.DisplayCivilizationScoreList();
					}
					else
					{
						this.owner.Briefings.RenderUnitCountsChart(personIdx);
					}

					break;

				case 2:
					this.owner.Briefings.DisplayMilitaryReport();
					break;

				case 3:
					this.owner.Briefings.DisplayAttitudeReport(personIdx);
					break;

				case 4:
					this.owner.Briefings.DisplayTradeReport(personIdx);
					break;

				case 5:
					this.owner.Briefings.DisplayScienceReport(personIdx);
					break;
			}

			this.processor.Journal.CloseScope("DisplayAdvisorMenu");
		}

		/// <summary>After a menu of the menu bar: the screen put back from the back screen 0x19d4, 0x654a set to 0xffff</summary>
		private void RestoreMenuScreen()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitGlobalWord(0x654a, 0xffff);
		}

		/// <summary>
		/// The World menu: Wonders, Top 5 Cities, Civilization Score, World Map, Demographics and SpaceShips (disabled, 0xb276,
		/// when nobody has a spaceship); the screen is put back from 0x19d4 before the chosen report
		/// </summary>
		public void DisplayWorldMenu()
		{
			this.processor.Journal.OpenScope("DisplayWorldMenu()");

			if ((this.owner.World.StarshipMarkers & 0xfe00) == 0)
			{
				DisabledLines = 0x20;
			}

			Message(" Wonders of the World (F7)\n Top 5 Cities (F8)\n Civilization Score (F9)\n")
				.Text(" World Map (F10)\n Demographics\n SpaceShips\n");

			ushort choice = this.owner.Dialogs.DisplayMenu(0xba06, 144, 8, 0);

			RestoreMenuScreen();

			switch (choice)
			{
				case 0:
					this.owner.Atlas.DisplayWondersOfTheWorld();
					break;

				case 1:
					this.owner.Legends.DisplayTopCities();
					break;

				case 2:
					this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, true);
					break;

				case 3:
					this.owner.Atlas.DisplayWorldMap(1);
					break;

				case 4:
					this.owner.Atlas.DisplayCensus(this.owner.World.PersonParticipantIdx);
					break;

				case 5:
					this.owner.RocketScreen.ChooseNationToView();
					break;
			}

			this.processor.Journal.CloseScope("DisplayWorldMenu");
		}

		/// <summary>
		/// The Civilopedia menu: the chosen part is shown (0x654a = -1 afterwards), Escape sets 0x654a to 1
		/// </summary>
		public void DisplayCivilopediaMenu()
		{
			this.processor.Journal.OpenScope("DisplayCivilopediaMenu()");

			Message(" Complete\n Civilization Advances\n City Improvements\n Military Units\n Terrain Types\n")
				.Text(" Miscellaneous\n");

			ushort choice = this.owner.Dialogs.DisplayMenu(0xba06, 182, 8, 0);

			if (choice != 0xffff)
			{
				this.owner.Encyclopedia.DisplayCivilopedia((ushort)(choice - 1));

				this.processor.EmitGlobalWord(0x654a, 0xffff);
			}
			else
			{
				this.processor.EmitGlobalWord(0x654a, 0x1);
			}

			this.processor.Journal.CloseScope("DisplayCivilopediaMenu");
		}
	}
}
