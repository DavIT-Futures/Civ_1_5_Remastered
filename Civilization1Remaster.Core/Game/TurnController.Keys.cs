namespace Civilization1Remaster.Core.Game
{
	partial class TurnController
	{
		private enum KeyStep
		{
			NextLook,

			MenuOrder,

			EndOfTurn,
		}

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

				case 0xfffe:
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

				case KeyCodes.AltH:
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

		private void ForgetMarkedTileOnScreen(short participantIdx)
		{
			if ((this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (participantIdx & 0xff))) != 0)
				this.Owner.MapOps.RepaintMapTile((short)this.turn.CursorHoriz, (short)this.turn.CursorVert);

			this.turn.CursorHoriz = 0xffff;
		}

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
