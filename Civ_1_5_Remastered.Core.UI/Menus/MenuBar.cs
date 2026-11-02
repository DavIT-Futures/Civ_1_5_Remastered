using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.UI.Menus
{
	public class MenuBar : GameComponent, IMenuBar
	{
		public MenuBar(IGameEngine owner)
			: base(owner)
		{
		}

		public void ManageMenuBar(short participantIndex, short troopIndex, short marker)
		{
			using var _ = this.Processor.Journal.Scope($"ManageMenuBar({participantIndex}, {troopIndex}, {marker})");

			Turn.ChosenOrder = 0xffff;
			Menu.Result = 0x0;

			if (marker == -1)
			{
				marker = (short)this.Owner.Helpers.ClampValue((short)this.Owner.MenuPointerHorizontal / 60, 0, 4);
			}

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 320, 200,
				Screen.BackScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			switch (marker)
			{
				case 0:
					ShowGameMenu();
					break;

				case 1:
					ShowOrdersMenu(participantIndex, troopIndex);
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

			if (Menu.Result == 1)
			{
				this.Owner.TurnHooks.RepaintMainScreen();
			}

			if (Menu.Result == 0)
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
					Screen.Box, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowGameMenu()
		{
			using var _ = this.Processor.Journal.Scope("DisplayGameMenu()");

			if (this.Owner.World.RoundTally == 0)
			{
				Menu.DisabledLines = 0x10;
			}

			this.Owner.Words.Set(" Tax Rate\n Luxuries Rate\n FindCity\n Options\n Save Game\n Load Game\n REVOLUTION!\n \n Retire\n QUIT to DOS\n");

			if ((this.Owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.Owner.Words.Append(" View Replay\n");
			}

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 16, 8, 0);

			this.Owner.TurnLogic.WaitForKey();

			switch (choice)
			{
				case 0:
					Turn.ChosenOrder = 0x3d;
					break;

				case 1:
					Turn.ChosenOrder = 0x2d;
					break;

				case 2:
					Turn.ChosenOrder = 0x3f;
					break;

				case 3:
					ChooseOptions();
					break;

				case 4:
					Turn.ChosenOrder = 0x53;
					break;

				case 5:
					this.Owner.SaveLoad.FetchBay = this.Owner.SaveLoad.ChooseGameToImport();

					if (this.Owner.SaveLoad.FetchBay != 0xffff)
					{
						Turn.GameEndRequest = ISaveGameStore.FetchSessionAsk;
						Turn.ChosenOrder = 0x1000;
					}

					break;

				case 6:
					Turn.ChosenOrder = 0xfffe;
					break;

				case 8:
					Turn.GameEndRequest = 0x2;
					Turn.ChosenOrder = 0x1000;
					break;

				case 9:
					Turn.GameEndRequest = 0x1;
					Turn.ChosenOrder = 0x1000;
					break;

				case 10:
					this.Owner.Replayer.ShowReplay();
					break;
			}
		}

		public void ShowOrdersMenu(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayOrdersMenu({participantIndex}, {troopIndex})");

			if (troopIndex != -1 && troopIndex != 128)
			{
				ChooseOrder(participantIndex, troopIndex);
			}
		}

		public void ShowAdvisorMenu()
		{
			using var _ = this.Processor.Journal.Scope("DisplayAdvisorMenu()");

			Message(" City Status (F1)\n Military Advisor (F2)\n Intelligence Advisor (F3)\n Attitude Advisor (F4)\n")
				.Text(" Trade Advisor (F5)\n Science Advisor (F6)\n");

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 112, 8, 0);

			RestoreMenuScreen();

			short personIndex = this.Owner.World.PersonParticipantIndex;

			switch (choice)
			{
				case 0:
					this.Owner.Briefings.ShowCityStatusReport(personIndex);
					break;

				case 1:
					if (MapView.Revealed != 0)
					{
						this.Owner.RealmInformation.ShowCivilizationScoreList();
					}
					else
					{
						this.Owner.Briefings.DrawUnitCountsChart(personIndex);
					}

					break;

				case 2:
					this.Owner.Briefings.ShowMilitaryReport();
					break;

				case 3:
					this.Owner.Briefings.ShowAttitudeReport(personIndex);
					break;

				case 4:
					this.Owner.Briefings.ShowTradeReport(personIndex);
					break;

				case 5:
					this.Owner.Briefings.ShowScienceReport(personIndex);
					break;
			}
		}

		public void ShowWorldMenu()
		{
			using var _ = this.Processor.Journal.Scope("DisplayWorldMenu()");

			if ((this.Owner.World.StarshipMarkers & 0xfe00) == 0)
			{
				Menu.DisabledLines = 0x20;
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
					this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIndex, true);
					break;

				case 3:
					this.Owner.Atlas.ShowWorldMap(1);
					break;

				case 4:
					this.Owner.Atlas.ShowCensus(this.Owner.World.PersonParticipantIndex);
					break;

				case 5:
					this.Owner.RocketScreen.ChooseNationToView();
					break;
			}
		}

		public void ShowCivilopediaMenu()
		{
			using var _ = this.Processor.Journal.Scope("DisplayCivilopediaMenu()");

			Message(" Complete\n Civilization Advances\n City Improvements\n Military Units\n Terrain Types\n")
				.Text(" Miscellaneous\n");

			ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 182, 8, 0);

			if (choice != 0xffff)
			{
				this.Owner.Encyclopedia.ShowCivilopedia((ushort)(choice - 1));

				Menu.Result = 0xffff;
			}
			else
			{
				Menu.Result = 0x1;
			}
		}

		private void ChooseOptions()
		{
			this.Owner.Words.Set("Options:\n Instant Advice\n AutoSave\n End of Turn\n Animations\n Sound\n Enemy Moves\n Civilopedia Text\n Palace\n");

			ushort choice;

			do
			{
				Menu.Options = (ushort)this.Owner.World.SessionPreferenceMarkers;

				choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 24, 16, 0);

				if (choice != 0xffff)
				{
					if (Menu.HelpTopic == 0)
					{
						this.Owner.World.SessionPreferenceMarkers ^= (short)(1 << (byte)choice);
					}

					Menu.DefaultLine = choice;
				}
			}
			while (choice != 0xffff || Menu.HelpTopic != 0);
		}

		private void ChooseOrder(short participantIndex, short troopIndex)
		{
			UnitMemory troops = new UnitMemory(this.Processor);
			List<byte> keys = new List<byte>();

			this.Owner.Words.Clear();

			short horizontalLocation = troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
			short verticalLocation = troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);

			byte improvements = (byte)this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

			ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

			this.Owner.Words.Append(" No Orders \x008fspace\n");
			keys.Add(0x20);

			Menu.DisabledLines = 0x0;

			if (troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0)
			{
				this.Owner.Words.Append((improvements & 1) != 0 ? (ushort)0x2cbc : (ushort)0x2ccd);
				keys.Add(0x62);

				if ((improvements & 0x8) == 0)
				{
					this.Owner.Words.Append(" Build Road \x008fr\n");
					keys.Add(0x72);
				}
				else if ((improvements & 0x10) == 0 && this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x31) != 0)
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

						if (this.Owner.TurnLogic.CheckCoastalTile(horizontalLocation, verticalLocation) == 0)
						{
							Menu.DisabledLines = (ushort)(Menu.DisabledLines | (1 << (byte)keys.Count));
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

			if (troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0)
			{
				this.Owner.Words.Append(" Build Fortress \x008ff\n");

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x2d) == 0)
				{
					Menu.DisabledLines = (ushort)(Menu.DisabledLines | (1 << (byte)keys.Count));
				}
			}
			else if (troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == 0)
			{
				this.Owner.Words.Append(" Fortify \x008ff\n");
			}

			if (troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == 0)
			{
				keys.Add(0x66);
			}

			this.Owner.Words.Append(" Wait \x008fw\n Sentry \x008fs\n GoTo\n");
			keys.Add(0x77);
			keys.Add(0x73);
			keys.Add(0x67);

			if ((improvements & 0x1e) != 0)
			{
				sbyte kind = troops.TroopKind(participantIndex, troopIndex);

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

			if ((troops.KindField(participantIndex, troopIndex, UnitKindTable.TroopGroup) == 5 || troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0x17) &&
				troops.Pull(participantIndex, troopIndex, UnitMemory.FollowingInStack) != 0xff)
			{
				this.Owner.Words.Append(" Unload \x008fu\n");
				keys.Add(0x75);
			}

			short successor = this.Owner.UnitOperations.UpgradeKind(participantIndex, troopIndex);

			if (successor != UnitUpgradeRules.NoSuccessor)
			{
				short cost = this.Owner.UnitOperations.UpgradeCost(participantIndex, troopIndex);

				if (this.Owner.World.Participants[participantIndex].Gold < cost)
				{
					Menu.DisabledLines |= (ushort)(1 << keys.Count);
				}

				this.Owner.Words.Append(" Upgrade to ");
				this.Owner.Words.Append(GameTables.UnitText(successor));
				this.Owner.Words.Append($" (${cost}) \x008fe\n");
				keys.Add(0x65);
			}

			this.Owner.Words.Append(" \n Disband Unit \x008fD\n");
			keys.Add(0x0);
			keys.Add(0x44);

			short choice = (short)this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 72, 8, 0);

			Turn.ChosenOrder = choice >= 0 && choice < keys.Count ? (ushort)(short)(sbyte)keys[choice] : (ushort)0xffff;
		}

		private void AppendChangeOfTerrain(ushort work)
		{
			this.Owner.Words.Append(" Change to ");

			this.Owner.Words.Append(GameTables.TerrainText(this.Processor.PullGlobalWord((ushort)((this.Processor.PullGlobalWord(work) << 1) + IMapPlanes.TerrainOfColour))));
		}

		private void RestoreMenuScreen()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			Menu.Result = 0xffff;
		}
	}
}
