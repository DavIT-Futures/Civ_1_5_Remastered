
namespace Civilization1Remaster.Core.Game
{
	/// <summary>The keys of the human turn that are not unit orders: menus, view, taxes, revolution</summary>
	partial class TurnController
	{
		/// <summary>Where HandlePlayerTurn goes on after a key (CarryOutKey)</summary>
		private enum KeyStep
		{
			/// <summary>The next look at the unit (L3c40)</summary>
			NextLook,

			/// <summary>The menu bar chose the order 0x44 of the unit (L0d0a)</summary>
			MenuOrder,

			/// <summary>Enter or Space without a unit: the end of the turn (L3ea1)</summary>
			EndOfTurn,
		}

		/// <summary>The direction 1 - 8 of Shift and an arrow (the keypad digits in the low byte)</summary>
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

		/// <summary>
		/// The key 0x44 that is neither an order nor a move: the reports (F1 - F10, other ones with everything shown, 0xd806),
		/// the view moved by Shift and an arrow (Alt+D toggles 0xd806), Enter and Space (without a unit the end of the turn),
		/// the tax and luxuries rates, the revolution (0xfffe), the save, the sound, the quit, Escape and Tab (the marked tile)
		/// and the menus of the menu bar (Alt and a letter; a chosen order goes to L0d0a). The locals of HandlePlayerTurn and
		/// SI are used as in the original.
		/// </summary>
		private KeyStep CarryOutKey(short participantIdx)
		{
			ushort key = this.turn.Order;
			short person = this.Owner.World.PersonParticipantIdx;
			bool everythingShown = MapView.Revealed != 0;


			switch (key)
			{
				case KeyCodes.F1:
					if (everythingShown)
						this.Owner.ArmyPicker.ShowWhichArmy();
					else
						this.Owner.Briefings.ShowCityStatusReport(person);
					break;

				case KeyCodes.F2:
					this.Owner.Briefings.DrawUnitCountsChart(person);
					break;

				case KeyCodes.F3:
					if (everythingShown)
					{
						this.Owner.Startup.HideMouseCursor();
						this.Owner.Services.ShowScreen(2);
						this.Owner.Libc.ReadKey();
						this.Owner.Services.ShowScreen(0);
						this.Owner.Startup.ShowMouseCursor();
					}
					else
					{
						this.Owner.Briefings.ShowMilitaryReport();
					}
					break;

				case KeyCodes.F4:
					this.Owner.Briefings.ShowAttitudeReport(person);
					break;

				case KeyCodes.F5:
					this.Owner.Briefings.ShowTradeReport(person);
					break;

				case KeyCodes.F6:
					this.Owner.Briefings.ShowScienceReport(person);
					break;

				case KeyCodes.F7:
					if (everythingShown)
					{
						for (short civ = 1; civ < 8; civ++)
						{
							this.turn.Count = (ushort)civ;
							this.Owner.RealmInfo.ShowCivilizationOverview(civ);
						}

						this.turn.Count = 8;
					}
					else
					{
						this.Owner.Atlas.ShowWondersOfTheWorld();
					}
					break;

				case KeyCodes.F8:
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

				case KeyCodes.F9:
					if (everythingShown)
						this.Owner.Atlas.ShowGameFigures();
					else
						this.Owner.Founding.ShowFinalScore(person, true);
					break;

				case KeyCodes.F10:
					this.Owner.Atlas.ShowWorldMap(1);
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
						return KeyStep.EndOfTurn;
					break;

				case '/':
				case '?':
					this.Owner.CityNaming.ShowNewCityLocation();
					break;

				case 'S':
					if (this.Owner.World.RoundTally != 0)
						this.Owner.SaveLoad.ShowStoreGameDialog(0xffff);
					break;

				case 0xfffe: // The revolution
					StartRevolution(participantIdx);
					break;

				case KeyCodes.AltM:
					this.Owner.Services.PrepareMouse();
					break;

				case '=':
				case '+':
					SelectTaxRate(participantIdx);
					break;

				case '-':
				case '_':
					SelectLuxuriesRate(participantIdx);
					break;

				case 't':
					MapView.UnitsHidden = 1;
					this.Owner.MapOps.FocusMapView(participantIdx, (short)MapView.Horiz, (short)MapView.Vert);
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();
					MapView.UnitsHidden = 0;
					this.Owner.MapOps.FocusMapView(participantIdx, (short)MapView.Horiz, (short)MapView.Vert);
					break;

				case KeyCodes.AltR:
					// Three random words of each of 16 records of 0x3a bytes, then the dialog 0x2003
					for (ushort record = 0; record < 0x10; record++)
					{
						this.turn.Result = record;

						for (ushort word = 0; word < 6; word += 2)
						{
							this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)((0x3a * record) + 0x1512 + word),
								(ushort)(this.Owner.Libc.Dice.DrawInt(3) - 1));
						}
					}

					this.turn.Result = 0x10;
					this.Owner.TurnHooks.ShowDialog(GameTexts.TextLeadersRandomized, 100, 80);
					break;

				case KeyCodes.AltV:
					this.Owner.World.SessionPrefMarkers ^= 0x10;
					Message("Sounds ")
						.Text((ushort)((this.Owner.World.SessionPrefMarkers & 0x10) != 0 ? 0x2020 : 0x2024))
						.Dialog(100, 80);
					break;

				case KeyCodes.AltH: // Alt + H (the help is shown always)
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelp1B, true);
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelp2B, true);
					break;

				case KeyCodes.AltQ:
					ConfirmQuit();
					break;

				case KeyCodes.Escape:
					if (Mouse.Present == 0 && this.turn.MarkedTileMode != 0)
					{
						this.turn.MarkedTileMode = 0;
						ForgetMarkedTileOnScreen(participantIdx);
					}
					break;

				case KeyCodes.Tab:
					if (Mouse.Present == 0)
						ToggleMarkedTile(participantIdx);
					break;

				case KeyCodes.AltG:
					return ManageMenu(participantIdx, 0);

				case KeyCodes.AltO:
					return ManageMenu(participantIdx, 1);

				case KeyCodes.AltA:
					return ManageMenu(participantIdx, 2);

				case KeyCodes.AltW:
					return ManageMenu(participantIdx, 3);

				case KeyCodes.AltC:
					return ManageMenu(participantIdx, 4);
			}

			return KeyStep.NextLook;
		}

		/// <summary>The view moves four tiles in the direction 0x24 (SI = 2 * the direction, as in the original)</summary>
		private void PanViewByDirection()
		{
			ushort horiz = (ushort)(Tables.DirectionHorizAt((ushort)(this.turn.Direction << 1)) << 2);
			MapView.Horiz = (ushort)(MapView.Horiz + horiz);

			ushort vert = (ushort)(Tables.DirectionVertAt((ushort)(this.turn.Direction << 1)) << 2);
			MapView.Vert = (ushort)(MapView.Vert + vert);

			this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx,
				(short)MapView.Horiz,
				(short)MapView.Vert);
		}

		/// <summary>
		/// The revolution: after the dialog 0x1f6a, answer 1 makes the government anarchy, with the newspaper and the new
		/// government graphics (SI = 2 * the player, as in the original)
		/// </summary>
		private void StartRevolution(short participantIdx)
		{
			this.Owner.Words.Clear();
			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicRev);
			if (this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 64, 80) != 1)
				return;

			this.Owner.World.Participants[participantIdx].RegimeKind = 0;

			Message("The ")
				.Text(Tables.CivilizationPluralAt((ushort)(participantIdx << 1)))
				.Text(" are\nrevolting! Citizens\ndemand new govt.\n");
			this.Owner.Paper.ShowNewspaper(-1);
			this.Owner.NewGameWizard.LoadGovernmentGraphics();
			this.Owner.TurnHooks.RepaintMainScreen();
		}

		/// <summary>
		/// The tax rate: a dialog of every split of tax and science (the luxuries stay); 0x3a the choice, -1 keeps the rates
		/// </summary>
		private void SelectTaxRate(short participantIdx)
		{
			ref var participant = ref this.Owner.World.Participants[participantIdx];

			this.Owner.Words.Set("Select new Tax rate:\n ");

			short rate = 0;

			for (; (short)(participant.LevyRatio + participant.ResearchLevyRatio) >= rate; rate++)
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(10 * rate), 10));
				this.Owner.Words.Append("% Tax, (");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)((participant.LevyRatio + participant.ResearchLevyRatio - rate) * 10), 10));
				this.Owner.Words.Append("% Science)\n ");
			}

			this.turn.Result = (ushort)rate;
			Menu.DefaultLine = (ushort)participant.LevyRatio;

			this.turn.Result = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

			if (this.turn.Result == 0xffff)
				return;

			participant.ResearchLevyRatio += (short)(participant.LevyRatio - (short)this.turn.Result);
			participant.LevyRatio = (short)this.turn.Result;

			this.Owner.TurnHooks.DrawStatusBar();
		}

		/// <summary>
		/// The luxuries rate: a dialog of every split of luxuries and science (the tax stays); 0x3a the choice, -1 keeps the
		/// rates
		/// </summary>
		private void SelectLuxuriesRate(short participantIdx)
		{
			ref var participant = ref this.Owner.World.Participants[participantIdx];

			this.Owner.Words.Set("Select new Luxuries rate:\n ");

			short rate = 0;

			for (; (short)(10 - participant.LevyRatio) >= rate; rate++)
			{
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(10 * rate), 10));
				this.Owner.Words.Append("% Luxuries, (");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(-(rate + participant.LevyRatio - 10) * 10), 10));
				this.Owner.Words.Append("% Science)\n ");
			}

			this.turn.Result = (ushort)rate;
			Menu.DefaultLine = (ushort)-(participant.LevyRatio + participant.ResearchLevyRatio - 10);

			this.turn.Result = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

			if (this.turn.Result == 0xffff)
				return;

			participant.ResearchLevyRatio = (short)-(participant.LevyRatio + (short)this.turn.Result - 10);

			this.Owner.TurnHooks.DrawStatusBar();
		}

		/// <summary>
		/// The quit: Load Game from the GAME menu (0xdc48) leaves at once, else the dialog; Keep Playing clears 0xdc48, Yes
		/// sets it to 1 (when it was 0); leaving ends the moves of the units (0x32 = 0x80)
		/// </summary>
		private void ConfirmQuit()
		{
			if (Turn.GameEndRequest != SaveGameStore.FetchSessionAsk)
			{
				ushort quitChoice = Message("Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n")
					.Dialog(100, 80);

				if (quitChoice != 1)
				{
					Turn.GameEndRequest = 0;
					return;
				}

				if (Turn.GameEndRequest == 0)
					Turn.GameEndRequest = 1;
			}

			this.turn.Troop = 0x80;
		}

		/// <summary>The marked tile (0xe, 0x12) is repainted when the player has seen it, and forgotten (0xe = -1)</summary>
		private void ForgetMarkedTileOnScreen(short participantIdx)
		{
			if ((this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (participantIdx & 0xff))) != 0)
				this.Owner.MapOps.RepaintMapTile((short)this.turn.CursorHoriz, (short)this.turn.CursorVert);

			this.turn.CursorHoriz = 0xffff;
		}

		/// <summary>
		/// Tab: the marked tile goes away, or comes on the unit (when it is on the screen; SI the unit, as in the original)
		/// or else in the middle of the view; the tile is repainted when the player has seen it; 0x4 toggles
		/// </summary>
		private void ToggleMarkedTile(short participantIdx)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				ForgetMarkedTileOnScreen(participantIdx);
			}
			else
			{
				bool onUnit = false;

				if (this.turn.Troop != 0x80)
				{
					ushort place = (ushort)(((ushort)(participantIdx * UnitMemory.ParticipantStride + this.turn.Troop * UnitMemory.TroopStride)) + UnitMemory.Origin);

					if (CheckPositionOnScreen(
						this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitMemory.Horiz)),
						this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + UnitMemory.Vert))) != 0)
					{
						this.turn.CursorHoriz = this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitMemory.Horiz));
						this.turn.CursorVert = this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(place + UnitMemory.Vert));
						onUnit = true;
					}
				}

				if (!onUnit)
				{
					this.turn.CursorHoriz = (ushort)(MapView.Horiz + 7);
					this.turn.CursorVert = (ushort)(MapView.Vert + 6);
				}

				if ((this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (participantIdx & 0xff))) != 0)
					this.Owner.MapOps.RepaintMapTile((short)this.turn.CursorHoriz, (short)this.turn.CursorVert);
			}

			this.turn.MarkedTileMode = (ushort)(this.turn.MarkedTileMode ^ 1);
		}

		/// <summary>The menu of the menu bar; a chosen order (0xd4ca) becomes the order 0x44 of the unit</summary>
		private KeyStep ManageMenu(short participantIdx, short menu)
		{
			this.Owner.MenuSystem.ManageMenuBar(participantIdx, (short)this.turn.Troop, menu);

			if (Turn.ChosenOrder == 0xffff)
				return KeyStep.NextLook;

			this.turn.Order = Turn.ChosenOrder;
			return KeyStep.MenuOrder;
		}
	}
}
