using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The keys of the human turn that are not unit orders: menus, view, taxes, revolution</summary>
	partial class PlayerTurn
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
			bool everythingShown = MapRevealed != 0;


			switch (key)
			{
				case KeyCodes.F1:
					if (everythingShown)
						this.Owner.ArmyPicker.DisplayWhichArmy();
					else
						this.Owner.Briefings.DisplayCityStatusReport(person);
					break;

				case KeyCodes.F2:
					this.Owner.Briefings.RenderUnitCountsChart(person);
					break;

				case KeyCodes.F3:
					if (everythingShown)
					{
						this.Owner.Startup.ConcealMouseCursor();
						this.Owner.Services.DisplayScreen(2);
						this.Owner.Libc.ReadKey();
						this.Owner.Services.DisplayScreen(0);
						this.Owner.Startup.DisplayMouseCursor();
					}
					else
					{
						this.Owner.Briefings.DisplayMilitaryReport();
					}
					break;

				case KeyCodes.F4:
					this.Owner.Briefings.DisplayAttitudeReport(person);
					break;

				case KeyCodes.F5:
					this.Owner.Briefings.DisplayTradeReport(person);
					break;

				case KeyCodes.F6:
					this.Owner.Briefings.DisplayScienceReport(person);
					break;

				case KeyCodes.F7:
					if (everythingShown)
					{
						for (short civ = 1; civ < 8; civ++)
						{
							this.turn.Count = (ushort)civ;
							this.Owner.RealmInfo.DisplayCivilizationOverview(civ);
						}

						this.turn.Count = 8;
					}
					else
					{
						this.Owner.Atlas.DisplayWondersOfTheWorld();
					}
					break;

				case KeyCodes.F8:
					if (everythingShown)
					{
						this.Owner.Atlas.DisplayCivilizationGraph();
						this.Owner.Replayer.DisplayReplay();
					}
					else
					{
						this.Owner.Legends.DisplayTopCities();
					}
					break;

				case KeyCodes.F9:
					if (everythingShown)
						this.Owner.Atlas.DisplayGameFigures();
					else
						this.Owner.Founding.DisplayFinalScore(person, true);
					break;

				case KeyCodes.F10:
					this.Owner.Atlas.DisplayWorldMap(1);
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
					MapRevealed = (ushort)~MapRevealed;
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
					this.Owner.CityNaming.DisplayNewCityLocation();
					break;

				case 'S':
					if (this.Owner.World.RoundTally != 0)
						this.Owner.SaveLoad.DisplayStoreGameDialog(0xffff);
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
					UnitsHidden = 1;
					this.Owner.MapOps.FocusMapView(participantIdx, (short)MapViewHoriz, (short)MapViewVert);
					this.Owner.Conquest.AwaitForKeyPressOrMouseClick();
					UnitsHidden = 0;
					this.Owner.MapOps.FocusMapView(participantIdx, (short)MapViewHoriz, (short)MapViewVert);
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
					this.Owner.TurnHooks.DisplayDialog(GameTexts.TextLeadersRandomized, 100, 80);
					break;

				case KeyCodes.AltV:
					this.Owner.World.SessionPrefMarkers ^= 0x10;
					Message("Sounds ")
						.Text((ushort)((this.Owner.World.SessionPrefMarkers & 0x10) != 0 ? 0x2020 : 0x2024))
						.Dialog(100, 80);
					break;

				case KeyCodes.AltH: // Alt + H (the remaster shows the help always; ClassicHelpKey: only with the help option, before 0)
					this.Owner.HelpSystem.DisplayHelpPopup(GameTexts.TopicHelp1B, !this.Owner.TurnHooks.ClassicHelpKey);
					this.Owner.HelpSystem.DisplayHelpPopup(GameTexts.TopicHelp2B, !this.Owner.TurnHooks.ClassicHelpKey);
					break;

				case KeyCodes.AltQ:
					ConfirmQuit();
					break;

				case KeyCodes.Escape:
					if (MousePresent == 0 && this.turn.MarkedTileMode != 0)
					{
						this.turn.MarkedTileMode = 0;
						ForgetMarkedTileOnScreen(participantIdx);
					}
					break;

				case KeyCodes.Tab:
					if (MousePresent == 0)
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
			ushort horiz = (ushort)(DirectionHorizAt((ushort)(this.turn.Direction << 1)) << 2);
			MapViewHoriz = (ushort)(MapViewHoriz + horiz);

			ushort vert = (ushort)(DirectionVertAt((ushort)(this.turn.Direction << 1)) << 2);
			MapViewVert = (ushort)(MapViewVert + vert);

			this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx,
				(short)MapViewHoriz,
				(short)MapViewVert);
		}

		/// <summary>
		/// The revolution: after the dialog 0x1f6a, answer 1 makes the government anarchy, with the newspaper and the new
		/// government graphics (SI = 2 * the player, as in the original)
		/// </summary>
		private void StartRevolution(short participantIdx)
		{
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicRev);
			if (this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 64, 80) != 1)
				return;

			this.Owner.World.Participants[participantIdx].RegimeKind = 0;

			Message("The ")
				.Text(CivilizationPluralAt((ushort)(participantIdx << 1)))
				.Text(" are\nrevolting! Citizens\ndemand new govt.\n");
			this.Owner.Paper.DisplayNewspaper(-1);
			this.Owner.NewGameWizard.ImportGovernmentGraphics();
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
			DefaultLine = (ushort)participant.LevyRatio;

			this.turn.Result = this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

			if (this.turn.Result == 0xffff)
				return;

			participant.ResearchLevyRatio += (short)(participant.LevyRatio - (short)this.turn.Result);
			participant.LevyRatio = (short)this.turn.Result;

			this.Owner.TurnHooks.RenderStatusBar();
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
			DefaultLine = (ushort)-(participant.LevyRatio + participant.ResearchLevyRatio - 10);

			this.turn.Result = this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

			if (this.turn.Result == 0xffff)
				return;

			participant.ResearchLevyRatio = (short)-(participant.LevyRatio + (short)this.turn.Result - 10);

			this.Owner.TurnHooks.RenderStatusBar();
		}

		/// <summary>
		/// The quit: Load Game from the GAME menu (0xdc48) leaves at once, else the dialog; Keep Playing clears 0xdc48, Yes
		/// sets it to 1 (when it was 0); leaving ends the moves of the units (0x32 = 0x80)
		/// </summary>
		private void ConfirmQuit()
		{
			if (GameEndRequest != GameLoadAndSave.FetchSessionAsk)
			{
				ushort quitChoice = Message("Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n")
					.Dialog(100, 80);

				if (quitChoice != 1)
				{
					GameEndRequest = 0;
					return;
				}

				if (GameEndRequest == 0)
					GameEndRequest = 1;
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

					if (VerifyPositionOnScreen(
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
					this.turn.CursorHoriz = (ushort)(MapViewHoriz + 7);
					this.turn.CursorVert = (ushort)(MapViewVert + 6);
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

			if (ChosenOrder == 0xffff)
				return KeyStep.NextLook;

			this.turn.Order = ChosenOrder;
			return KeyStep.MenuOrder;
		}
	}
}
