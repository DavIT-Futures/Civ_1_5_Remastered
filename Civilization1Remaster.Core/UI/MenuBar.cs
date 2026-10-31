using System.Collections.Generic;

namespace Civilization1Remaster.Core.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2c84.
	/// </summary>
	public class MenuBar : GameComponent, IMenuBar
	{
		public MenuBar(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void ManageMenuBar(short participantIdx, short troopIdx, short marker)
		{
			this.Processor.Journal.OpenScope($"ManageMenuBar({participantIdx}, {troopIdx}, {marker})");

			ChosenOrder = 0xffff;
			MenuResult = 0x0;

			if (marker == -1)
			{
				marker = (short)this.Owner.Helpers.ClampValue((short)this.Owner.GlobDb3c / 60, 0, 4);
			}

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 320, 200,
				BackScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			switch (marker)
			{
				case 0:
					ShowGameMenu();
					break;

				case 1:
					ShowOrdersMenu(participantIdx, troopIdx);
					break;

				case 2:
					ShowAdvisorMenu();
					break;

				case 3:
					ShowWorldMenu();
					break;

				case 4:
					ShowCivilopediaMenu();
					break;
			}

			this.Owner.Startup.HideMouseCursor();

			if (MenuResult == 1)
			{
				this.Owner.TurnHooks.RepaintMainScreen();
			}

			if (MenuResult == 0)
			{
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("ManageMenuBar");
		}

		/// <inheritdoc/>
		public void ShowGameMenu()
		{
			this.Processor.Journal.OpenScope("DisplayGameMenu()");

			if (this.Owner.World.RoundTally == 0)
			{
				DisabledLines = 0x10;
			}

			this.Owner.Words.Set(" Tax Rate\n Luxuries Rate\n FindCity\n Options\n Save Game\n Load Game\n REVOLUTION!\n \n Retire\n QUIT to DOS\n");

			if ((this.Owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.Owner.Words.Append(" View Replay\n");
			}

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 16, 8, 0);

			this.Owner.TurnLogic.WaitForKey();

			// Load Game (5) is new, the items after it moved down by one
			switch (choice)
			{
				case 0:
					ChosenOrder = 0x3d;
					break;

				case 1:
					ChosenOrder = 0x2d;
					break;

				case 2:
					ChosenOrder = 0x3f;
					break;

				case 3:
					ChooseOptions();
					break;

				case 4:
					ChosenOrder = 0x53;
					break;

				case 5:
					// The saved game is chosen now and loaded by the main loop after the turn is left,
					// the same way Quit leaves the turn
					this.Owner.SaveLoad.FetchBay = this.Owner.SaveLoad.ChooseGameToImport();

					if (this.Owner.SaveLoad.FetchBay != 0xffff)
					{
						GameEndRequest = SaveGameStore.FetchSessionAsk;
						ChosenOrder = 0x1000;
					}
					break;

				case 6:
					ChosenOrder = 0xfffe;
					break;

				case 8:
					GameEndRequest = 0x2;
					ChosenOrder = 0x1000;
					break;

				case 9:
					GameEndRequest = 0x1;
					ChosenOrder = 0x1000;
					break;

				case 10:
					this.Owner.Replayer.ShowReplay();
					break;
			}

			this.Processor.Journal.CloseScope("DisplayGameMenu");
		}

		/// <inheritdoc/>
		public void ShowOrdersMenu(short participantIdx, short troopIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayOrdersMenu({participantIdx}, {troopIdx})");

			if (troopIdx != -1 && troopIdx != 128)
			{
				ChooseOrder(participantIdx, troopIdx);
			}

			this.Processor.Journal.CloseScope("DisplayOrdersMenu");
		}

		/// <inheritdoc/>
		public void ShowAdvisorMenu()
		{
			this.Processor.Journal.OpenScope("DisplayAdvisorMenu()");

			Message(" City Status (F1)\n Military Advisor (F2)\n Intelligence Advisor (F3)\n Attitude Advisor (F4)\n")
				.Text(" Trade Advisor (F5)\n Science Advisor (F6)\n");

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 112, 8, 0);

			RestoreMenuScreen();

			short personIdx = this.Owner.World.PersonParticipantIdx;

			switch (choice)
			{
				case 0:
					this.Owner.Briefings.ShowCityStatusReport(personIdx);
					break;

				case 1:
					if (MapRevealed != 0)
					{
						this.Owner.RealmInfo.ShowCivilizationScoreList();
					}
					else
					{
						this.Owner.Briefings.DrawUnitCountsChart(personIdx);
					}

					break;

				case 2:
					this.Owner.Briefings.ShowMilitaryReport();
					break;

				case 3:
					this.Owner.Briefings.ShowAttitudeReport(personIdx);
					break;

				case 4:
					this.Owner.Briefings.ShowTradeReport(personIdx);
					break;

				case 5:
					this.Owner.Briefings.ShowScienceReport(personIdx);
					break;
			}

			this.Processor.Journal.CloseScope("DisplayAdvisorMenu");
		}

		/// <inheritdoc/>
		public void ShowWorldMenu()
		{
			this.Processor.Journal.OpenScope("DisplayWorldMenu()");

			if ((this.Owner.World.StarshipMarkers & 0xfe00) == 0)
			{
				DisabledLines = 0x20;
			}

			Message(" Wonders of the World (F7)\n Top 5 Cities (F8)\n Civilization Score (F9)\n")
				.Text(" World Map (F10)\n Demographics\n SpaceShips\n");

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 144, 8, 0);

			RestoreMenuScreen();

			switch (choice)
			{
				case 0:
					this.Owner.Atlas.ShowWondersOfTheWorld();
					break;

				case 1:
					this.Owner.Legends.ShowTopCities();
					break;

				case 2:
					this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIdx, true);
					break;

				case 3:
					this.Owner.Atlas.ShowWorldMap(1);
					break;

				case 4:
					this.Owner.Atlas.ShowCensus(this.Owner.World.PersonParticipantIdx);
					break;

				case 5:
					this.Owner.RocketScreen.ChooseNationToView();
					break;
			}

			this.Processor.Journal.CloseScope("DisplayWorldMenu");
		}

		/// <inheritdoc/>
		public void ShowCivilopediaMenu()
		{
			this.Processor.Journal.OpenScope("DisplayCivilopediaMenu()");

			Message(" Complete\n Civilization Advances\n City Improvements\n Military Units\n Terrain Types\n")
				.Text(" Miscellaneous\n");

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 182, 8, 0);

			if (choice != 0xffff)
			{
				this.Owner.Encyclopedia.ShowCivilopedia((ushort)(choice - 1));

				MenuResult = 0xffff;
			}
			else
			{
				MenuResult = 0x1;
			}

			this.Processor.Journal.CloseScope("DisplayCivilopediaMenu");
		}

		/// <summary>
		/// The Options menu: each chosen option is toggled (0xd7f2 shows the options) and the menu stays on it, until Escape
		/// (while 0x2f9c is set, the menu comes back even after Escape and toggles nothing)
		/// </summary>
		private void ChooseOptions()
		{
			this.Owner.Words.Set("Options:\n Instant Advice\n AutoSave\n End of Turn\n Animations\n Sound\n Enemy Moves\n Civilopedia Text\n Palace\n");

			ushort choice;

			do
			{
				MenuOptions = (ushort)this.Owner.World.SessionPrefMarkers;

				choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 24, 16, 0);

				if (choice != 0xffff)
				{
					if (MenuHelpTopic == 0)
					{
						this.Owner.World.SessionPrefMarkers ^= (short)(1 << (byte)choice);
					}

					DefaultLine = choice;
				}
			}
			while (choice != 0xffff || MenuHelpTopic != 0);
		}

		private void ChooseOrder(short participantIdx, short troopIdx)
		{
			UnitMemory troops = new UnitMemory(this.Processor);
			List<byte> keys = new List<byte>();

			this.Owner.Words.Clear();

			short horizLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			short vertLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);

			byte improvements = (byte)this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.Owner.Words.Append(" No Orders \x008fspace\n");
			keys.Add(0x20);

			DisabledLines = 0x0;

			if (troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0)
			{
				// Settlers
				this.Owner.Words.Append((improvements & 1) != 0 ? (ushort)0x2cbc : (ushort)0x2ccd);
				keys.Add(0x62);

				if ((improvements & 0x8) == 0)
				{
					this.Owner.Words.Append(" Build Road \x008fr\n");
					keys.Add(0x72);
				}
				else if ((improvements & 0x10) == 0 && this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31) != 0)
				{
					this.Owner.Words.Append(" Build RailRoad \x008fr\n");
					keys.Add(0x72);
				}

				if ((improvements & 0x2) == 0)
				{
					ushort irrigation = (ushort)((0xc * terrain) + 0x44a);

					if (this.Processor.PullGlobalWord(irrigation) == 0xfffe)
					{
						this.Owner.Words.Append(" Build Irrigation");

						if (this.Owner.TurnLogic.CheckCoastalTile(horizLoc, vertLoc) == 0)
						{
							DisabledLines = (ushort)(DisabledLines | (1 << (byte)keys.Count));
						}
					}
					else if ((short)this.Processor.PullGlobalWord(irrigation) >= 0)
					{
						AppendChangeOfTerrain(irrigation);
					}

					if (this.Processor.PullGlobalWord(irrigation) != 0xffff)
					{
						keys.Add(0x69);
						this.Owner.Words.Append(" \x008fi\n");
					}
				}

				if ((improvements & 0x4) == 0)
				{
					ushort mining = (ushort)((0xc * terrain) + 0x44e);

					if ((short)this.Processor.PullGlobalWord(mining) <= -2)
					{
						this.Owner.Words.Append(0x2d28);
					}
					else if ((short)this.Processor.PullGlobalWord(mining) >= 0)
					{
						AppendChangeOfTerrain(mining);
					}

					if (this.Processor.PullGlobalWord(mining) != 0xffff)
					{
						keys.Add(0x6d);
						this.Owner.Words.Append(" \x008fm\n");
					}
				}

				if ((improvements & 0x40) != 0)
				{
					this.Owner.Words.Append(" Clean up Pollution \x008fp\n");
					keys.Add(0x70);
				}
			}

			if (troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0)
			{
				this.Owner.Words.Append(" Build Fortress \x008ff\n");

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d) == 0)
				{
					DisabledLines = (ushort)(DisabledLines | (1 << (byte)keys.Count));
				}
			}
			else if (troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == 0)
			{
				this.Owner.Words.Append(" Fortify \x008ff\n");
			}

			if (troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == 0)
			{
				keys.Add(0x66);
			}

			this.Owner.Words.Append(" Wait \x008fw\n Sentry \x008fs\n GoTo\n");
			keys.Add(0x77);
			keys.Add(0x73);
			keys.Add(0x67);

			if ((improvements & 0x1e) != 0)
			{
				sbyte kind = troops.TroopKind(participantIdx, troopIdx);

				if (kind < 0x1a && kind != 0xe)
				{
					this.Owner.Words.Append(" Pillage \x008fP\n");
					keys.Add(0x50);
				}
			}

			if ((improvements & 0x1) != 0)
			{
				this.Owner.Words.Append(" Home City \x008fh\n");
				keys.Add(0x68);
			}

			if ((troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == 5 || troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0x17) &&
				troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) != 0xff)
			{
				this.Owner.Words.Append(" Unload \x008fu\n");
				keys.Add(0x75);
			}

			this.Owner.Words.Append(" \n Disband Unit \x008fD\n");
			keys.Add(0x0);
			keys.Add(0x44);

			short choice = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 72, 8, 0);

			ChosenOrder = choice >= 0 && choice < keys.Count ? (ushort)(short)(sbyte)keys[choice] : (ushort)0xffff;
		}

		/// <summary>" Change to " and the name of the terrain the work makes (the word at the address)</summary>
		private void AppendChangeOfTerrain(ushort work)
		{
			this.Owner.Words.Append(" Change to ");

			this.Owner.Words.Append(TerrainText(this.Processor.PullGlobalWord((ushort)((this.Processor.PullGlobalWord(work) << 1) + MapPlanes.TerrainOfColour))));
		}

		/// <summary>After a menu of the menu bar: the screen put back from the back screen 0x19d4, 0x654a set to 0xffff</summary>
		private void RestoreMenuScreen()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			MenuResult = 0xffff;
		}
	}
}
