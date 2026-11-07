namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController
	{
		private static ushort ShiftedKeyDirection(ushort key)
		{
			switch (key)
			{
				case KeyCodes.ShiftUp: return 1;
				case KeyCodes.ShiftPageUp: return 2;
				case KeyCodes.ShiftRight: return 3;
				case KeyCodes.ShiftPageDown: return 4;
				case KeyCodes.ShiftDown: return 5;
				case KeyCodes.ShiftEnd: return 6;
				case KeyCodes.ShiftLeft: return 7;
				default: return 8;
			}
		}

		private KeyStep CarryOutKey(short participantIndex)
		{
			ushort key = this.turn.Order;
			short person = this.Owner.World.PersonParticipantIndex;
			bool everythingShown = MapView.Revealed != 0;

			switch (key)
			{
				case KeyCodes.FunctionKeyOne:
					if (everythingShown)
					{
						this.Owner.ArmyPicker.ShowWhichArmy();
					}
					else
					{
						this.Owner.Briefings.ShowCityStatusReport(person);
					}

					break;

				case KeyCodes.FunctionKeyTwo:
					this.Owner.Briefings.DrawUnitCountsChart(person);
					break;

				case KeyCodes.FunctionKeyThree:
					if (everythingShown)
					{
						this.Owner.Startup.HideMouseCursor();
						this.Owner.Services.ShowScreen(2);
						this.Owner.Runtime.ReadKey();
						this.Owner.Services.ShowScreen(0);
						this.Owner.Startup.ShowMouseCursor();
					}
					else
					{
						this.Owner.Briefings.ShowMilitaryReport();
					}

					break;

				case KeyCodes.FunctionKeyFour:
					this.Owner.Briefings.ShowAttitudeReport(person);
					break;

				case KeyCodes.FunctionKeyFive:
					this.Owner.Briefings.ShowTradeReport(person);
					break;

				case KeyCodes.FunctionKeySix:
					this.Owner.Briefings.ShowScienceReport(person);
					break;

				case KeyCodes.FunctionKeySeven:
					if (everythingShown)
					{
						for (short civilization = 1; civilization < 8; civilization++)
						{
							this.turn.Count = (ushort)civilization;
							this.Owner.RealmInformation.ShowCivilizationOverview(civilization);
						}

						this.turn.Count = 8;
					}
					else
					{
						this.Owner.Atlas.ShowWondersOfTheWorld();
					}

					break;

				case KeyCodes.FunctionKeyEight:
					if (everythingShown)
					{
						this.Owner.Atlas.ShowCivilizationGraph();
						this.Owner.Replayer.ShowReplay();
					}
					else
					{
						this.Owner.Legends.ShowTopCities();
					}

					break;

				case KeyCodes.FunctionKeyNine:
					if (everythingShown)
					{
						this.Owner.Atlas.ShowGameFigures();
					}
					else
					{
						this.Owner.Founding.ShowFinalScore(person, true);
					}

					break;

				case KeyCodes.FunctionKeyTen:
					this.Owner.Atlas.ShowWorldMap(1);
					break;

				case KeyCodes.FunctionKeyTwelve:
					this.Owner.Briefings.ShowWarReport(person);
					break;

				case KeyCodes.ShiftUp:
				case KeyCodes.ShiftPageUp:
				case KeyCodes.ShiftRight:
				case KeyCodes.ShiftPageDown:
				case KeyCodes.ShiftDown:
				case KeyCodes.ShiftEnd:
				case KeyCodes.ShiftLeft:
				case KeyCodes.ShiftHome:
					this.turn.Direction = ShiftedKeyDirection(key);
					PanViewByDirection();
					break;

				case KeyCodes.AltD:
					MapView.Revealed = (ushort)~MapView.Revealed;
					this.turn.Direction = 0;
					PanViewByDirection();
					break;

				case KeyCodes.Enter:
				case KeyCodes.Space:
					if (this.turn.Troop == 0x80)
					{
						return KeyStep.EndOfTurn;
					}

					break;

				case '/':
				case '?':
					this.Owner.CityNaming.ShowNewCityLocation();
					break;

				case 'S':
					if (this.Owner.World.RoundTally != 0)
					{
						this.Owner.SaveLoad.ShowStoreGameDialog(0xffff);
					}

					break;

				case 0xfffe:
					StartRevolution(participantIndex);
					break;

				case KeyCodes.AltM:
					this.Owner.Services.PrepareMouse();
					break;

				case '=':
				case '+':
					SelectTaxRate(participantIndex);
					break;

				case '-':
				case '_':
					SelectLuxuriesRate(participantIndex);
					break;

				case 't':
					MapView.UnitsHidden = 1;
					this.Owner.MapOperations.FocusMapView(participantIndex, (short)MapView.Horizontal, (short)MapView.Vertical);
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();
					MapView.UnitsHidden = 0;
					this.Owner.MapOperations.FocusMapView(participantIndex, (short)MapView.Horizontal, (short)MapView.Vertical);
					break;

				case KeyCodes.AltR:
					for (ushort record = 0; record < 0x10; record++)
					{
						this.turn.Result = record;

						for (ushort word = 0; word < 6; word += 2)
						{
							this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)((0x3a * record) + 0x1512 + word),
								(ushort)(this.Owner.Runtime.Dice.DrawInteger(3) - 1));
						}
					}

					this.turn.Result = 0x10;
					this.Owner.TurnHooks.ShowDialog(GameTexts.TextLeadersRandomized, 100, 80);
					break;

				case KeyCodes.AltV:
					this.Owner.World.SessionPreferenceMarkers ^= 0x10;
					Message("Sounds ")
						.Text((ushort)((this.Owner.World.SessionPreferenceMarkers & 0x10) != 0 ? 0x2020 : 0x2024))
						.Dialog(100, 80);
					break;

				case KeyCodes.AltH:
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelpOneSecond, true);
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelpTwoSecond, true);
					this.Owner.HelpSystem.ShowNotePopup("--- Remastered Keys ---",
						"F12  War Advisor: 1-6 details,\n" +
						"     T talks to a ruler (embassy)\n" +
						"a    Automate Settlers (5 modes,\n" +
						"     all cities or one city)\n" +
						"e    Upgrade unit (costs gold)\n" +
						"Q    City screen: production queue\n" +
						"     (also the QUE button)\n" +
						"F11  Full screen on/off\n" +
						"GoTo now follows railroads and\n" +
						"     the fastest route.");
					this.Owner.HelpSystem.ShowNotePopup("--- New Civilopedia Entries ---",
						"Civilopedia, Miscellaneous:\n" +
						"DIPLOMACY  attitude, tribute,\n" +
						"     vendetta, reparations, peace\n" +
						"WAR  sneak attacks, stacks,\n" +
						"     computer wars, advice\n" +
						"AUTOMATION  production queue,\n" +
						"     automated Settlers, jungle\n" +
						"     and swamp, GoTo routes");
					break;

				case KeyCodes.AltQ:
					ConfirmQuit();
					break;

				case KeyCodes.Escape:
					if (Mouse.Present == 0 && this.turn.MarkedTileMode != 0)
					{
						this.turn.MarkedTileMode = 0;
						ForgetMarkedTileOnScreen(participantIndex);
					}

					break;

				case KeyCodes.Tab:
					if (Mouse.Present == 0)
					{
						ToggleMarkedTile(participantIndex);
					}

					break;

				case KeyCodes.AltG:
					return ManageMenu(participantIndex, 0);

				case KeyCodes.AltO:
					return ManageMenu(participantIndex, 1);

				case KeyCodes.AltA:
					return ManageMenu(participantIndex, 2);

				case KeyCodes.AltW:
					return ManageMenu(participantIndex, 3);

				case KeyCodes.AltC:
					return ManageMenu(participantIndex, 4);
			}

			return KeyStep.NextLook;
		}

		private void PanViewByDirection()
		{
			ushort horizontal = (ushort)(Tables.DirectionHorizontalAt((ushort)(this.turn.Direction << 1)) << 2);
			MapView.Horizontal = (ushort)(MapView.Horizontal + horizontal);

			ushort vertical = (ushort)(Tables.DirectionVerticalAt((ushort)(this.turn.Direction << 1)) << 2);
			MapView.Vertical = (ushort)(MapView.Vertical + vertical);

			this.Owner.MapOperations.FocusMapView(this.Owner.World.PersonParticipantIndex,
				(short)MapView.Horizontal,
				(short)MapView.Vertical);
		}

		private void StartRevolution(short participantIndex)
		{
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicRevolution);
			if (this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 64, 80) != 1)
			{
				return;
			}

			this.Owner.World.Participants[participantIndex].RegimeKind = 0;

			Message("The ")
				.Text(Tables.CivilizationPluralAt((ushort)(participantIndex << 1)))
				.Text(" are\nrevolting! Citizens\ndemand new govt.\n");
			this.Owner.Paper.ShowNewspaper(-1);
			this.Owner.NewGameWizard.LoadGovernmentGraphics();
			this.Owner.TurnHooks.RepaintMainScreen();
		}

		private void SelectTaxRate(short participantIndex)
		{
			ref var participant = ref this.Owner.World.Participants[participantIndex];

			this.Owner.Words.Set("Select new Tax rate:\n ");

			short rate = 0;

			for (; (short)(participant.LevyRatio + participant.ResearchLevyRatio) >= rate; rate++)
			{
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(10 * rate), 10));
				this.Owner.Words.Append("% Tax, (");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)((participant.LevyRatio + participant.ResearchLevyRatio - rate) * 10), 10));
				this.Owner.Words.Append("% Science)\n ");
			}

			this.turn.Result = (ushort)rate;
			Menu.DefaultLine = (ushort)participant.LevyRatio;

			this.turn.Result = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

			if (this.turn.Result == 0xffff)
			{
				return;
			}

			participant.ResearchLevyRatio += (short)(participant.LevyRatio - (short)this.turn.Result);
			participant.LevyRatio = (short)this.turn.Result;

			this.Owner.TurnHooks.DrawStatusBar();
		}

		private void SelectLuxuriesRate(short participantIndex)
		{
			ref var participant = ref this.Owner.World.Participants[participantIndex];

			this.Owner.Words.Set("Select new Luxuries rate:\n ");

			short rate = 0;

			for (; (short)(10 - participant.LevyRatio) >= rate; rate++)
			{
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(10 * rate), 10));
				this.Owner.Words.Append("% Luxuries, (");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(-(rate + participant.LevyRatio - 10) * 10), 10));
				this.Owner.Words.Append("% Science)\n ");
			}

			this.turn.Result = (ushort)rate;
			Menu.DefaultLine = (ushort)-(participant.LevyRatio + participant.ResearchLevyRatio - 10);

			this.turn.Result = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

			if (this.turn.Result == 0xffff)
			{
				return;
			}

			participant.ResearchLevyRatio = (short)-(participant.LevyRatio + (short)this.turn.Result - 10);

			this.Owner.TurnHooks.DrawStatusBar();
		}

		private void ConfirmQuit()
		{
			if (Turn.GameEndRequest != ISaveGameStore.FetchSessionAsk)
			{
				ushort quitChoice = Message("Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n")
					.Dialog(100, 80);

				if (quitChoice != 1)
				{
					Turn.GameEndRequest = 0;
					return;
				}

				if (Turn.GameEndRequest == 0)
				{
					Turn.GameEndRequest = 1;
				}
			}

			this.turn.Troop = 0x80;
		}

		private void ForgetMarkedTileOnScreen(short participantIndex)
		{
			if ((this.Owner.World.ChartSight[this.turn.CursorHorizontal, this.turn.CursorVertical] & (1 << (participantIndex & 0xff))) != 0)
			{
				this.Owner.MapOperations.RepaintMapTile((short)this.turn.CursorHorizontal, (short)this.turn.CursorVertical);
			}

			this.turn.CursorHorizontal = 0xffff;
		}

		private void ToggleMarkedTile(short participantIndex)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				ForgetMarkedTileOnScreen(participantIndex);
			}
			else
			{
				bool onUnit = false;

				if (this.turn.Troop != 0x80)
				{
					ushort place = (ushort)(((ushort)((participantIndex * UnitMemory.ParticipantStride) + (this.turn.Troop * UnitMemory.TroopStride))) + UnitMemory.Origin);

					if (CheckPositionOnScreen(
						this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + UnitMemory.Horizontal)),
						this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + UnitMemory.Vertical))) != 0)
					{
						this.turn.CursorHorizontal = this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(place + UnitMemory.Horizontal));
						this.turn.CursorVertical = this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(place + UnitMemory.Vertical));
						onUnit = true;
					}
				}

				if (!onUnit)
				{
					this.turn.CursorHorizontal = (ushort)(MapView.Horizontal + 7);
					this.turn.CursorVertical = (ushort)(MapView.Vertical + 6);
				}

				if ((this.Owner.World.ChartSight[this.turn.CursorHorizontal, this.turn.CursorVertical] & (1 << (participantIndex & 0xff))) != 0)
				{
					this.Owner.MapOperations.RepaintMapTile((short)this.turn.CursorHorizontal, (short)this.turn.CursorVertical);
				}
			}

			this.turn.MarkedTileMode = (ushort)(this.turn.MarkedTileMode ^ 1);
		}

		private KeyStep ManageMenu(short participantIndex, short menu)
		{
			this.Owner.MenuSystem.ManageMenuBar(participantIndex, (short)this.turn.Troop, menu);

			if (Turn.ChosenOrder == 0xffff)
			{
				return KeyStep.NextLook;
			}

			this.turn.Order = Turn.ChosenOrder;
			return KeyStep.MenuOrder;
		}
	}
}
