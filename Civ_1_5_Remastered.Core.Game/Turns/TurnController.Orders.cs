namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController
	{
		private HumanOrder TakeHumanOrder(short participantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;

			DrawUnitInformation(participantIndex, this.turn.Troop);

			if (this.Owner.World.RoundTally == 0 && this.turn.LaterPass == 0)
			{
				this.Owner.Codes.Set(0, Tables.CivilizationPlural(participantIndex));

				this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstMove);

				this.turn.LaterPass = 1;
			}

			if (this.turn.Troop < UnitMemory.TroopTally)
			{
				short troopIndex = (short)this.turn.Troop;

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0 && this.turn.LaterPass == 0)
				{
					this.Owner.HelpSystem.ShowTileInformation(participantIndex, this.turn.Troop);
					this.turn.LaterPass = 1;
				}
			}

			this.Owner.UnitOperations.ChoreographUnitMove(participantIndex,
				(short)this.turn.Troop,
				this.turn.CursorHorizontal,
				this.turn.CursorVertical);

			if (this.turn.Troop != 0x80)
			{
				this.turn.HumanTookKey = 1;
			}

			if (this.Owner.MenuPointerState == 0)
			{
				this.turn.Order = this.Owner.Dialogs.ReadNavigationKey();

				if (this.turn.Order != KeyCodes.Enter || this.turn.MarkedTileMode == 0)
				{
					return HumanOrder.Order;
				}

				this.turn.TargetHorizontal = this.turn.CursorHorizontal;
				this.turn.TargetVertical = this.turn.CursorVertical;
				this.Owner.MenuPointerHorizontal = 0x50;
				this.Owner.MenuPointerVertical = 0x8;
				this.Owner.MenuPointerState = 0x1;

				return TakeClick(participantIndex);
			}

			FindClickedTile();

			if ((short)this.Owner.MenuPointerVertical < 8)
			{
				this.Owner.MenuSystem.ManageMenuBar(participantIndex, (short)this.turn.Troop, -1);

				WaitForKey();

				if (Turn.ChosenOrder == 0xffff)
				{
					return HumanOrder.NextLook;
				}

				this.turn.Order = Turn.ChosenOrder;

				return (short)this.turn.Troop < 0x80 ? HumanOrder.MenuOrder : HumanOrder.MenuWithoutUnit;
			}

			if ((short)this.Owner.MenuPointerHorizontal < 0x50)
			{
				if ((short)this.Owner.MenuPointerVertical < 0x3a)
				{
					this.Owner.MapOperations.FocusMapView(participantIndex,
						(short)(ushort)this.Owner.Routing.TunePosition(
						(short)this.Owner.MenuPointerHorizontal + (short)MapView.FocusLeft - 7),
						this.Owner.Helpers.ClampValue((short)this.Owner.MenuPointerVertical + (short)MapView.FocusTop - 14, 0, 49));
				}
				else if ((short)this.Owner.MenuPointerVertical < 0x48)
				{
					ShowPalace();
				}
				else if (this.turn.Troop == 0x80)
				{
					return HumanOrder.EndOfTurn;
				}

				this.Owner.MenuPointerState = 0;
			}

			if (this.Owner.MenuPointerState == 2 && (short)this.turn.Troop < 0x80)
			{
				TakeHeldClick(participantIndex);
			}

			if (this.Owner.MenuPointerState == 2 &&
				(((sbyte)this.Owner.World.ChartSight[this.turn.TargetHorizontal, this.turn.TargetVertical] & (1 << (person & 0xff))) != 0 ||
					MapView.Revealed != 0))
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.Encyclopedia.ShowCivilopediaRecord(this.Owner.MapOperations.ObtainTerrainType(
					(short)this.turn.TargetHorizontal,
					(short)this.turn.TargetVertical), 3);

				this.Owner.TurnHooks.RepaintMainScreen();

				this.Owner.Startup.ShowMouseCursor();
			}

			return TakeClick(participantIndex);
		}

		private void FindClickedTile()
		{
			this.turn.TargetHorizontal = (ushort)this.Owner.Routing.TunePosition(
				(((short)this.Owner.MenuPointerHorizontal - 80) / 16) + (short)MapView.Horizontal);
			this.turn.TargetVertical = (ushort)(((short)(ushort)(this.Owner.MenuPointerVertical - 8) / 16) + MapView.Vertical);
		}

		private void ShowPalace()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Throne.LoadPalaceGraphics(0);

			this.Processor.EmitWord(this.Processor.SegmentData.Word, Screen.Box, 0x0);

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileCityBackgroundPicture, 1);

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.TurnHooks.RepaintMainScreen();

			this.Owner.Helpers.LaunchHueCycling();
		}

		private void TakeHeldClick(short participantIndex)
		{
			do
			{
				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.MenuPointerState != 0);

			FindClickedTile();

			short troopIndex = (short)this.turn.Troop;

			this.turn.OtherUnit = (ushort)this.Owner.Helpers.MergeCoordinates(
				(short)((short)this.turn.TargetHorizontal - this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal)),
				(short)((short)this.turn.TargetVertical - this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)));

			if (this.Owner.Runtime.AbsoluteValue((short)(this.turn.TargetHorizontal - this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal))) == 0x4f &&
				this.Owner.Runtime.AbsoluteValue((short)(this.turn.TargetVertical - this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical))) <= 1)
			{
				this.turn.OtherUnit = 1;
			}

			if (this.turn.OtherUnit != 1)
			{
				this.Owner.MenuPointerState = 2;
				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)this.turn.TargetHorizontal);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)this.turn.TargetVertical);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			this.Owner.MenuPointerState = 0;
			this.turn.MouseCursorSet = 0;

			this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);
		}

		private HumanOrder TakeClick(short participantIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			short horizontalLocation = (short)this.turn.TargetHorizontal;
			short verticalLocation = (short)this.turn.TargetVertical;

			if (this.Owner.MenuPointerState == 1)
			{
				if (this.turn.MouseCursorSet != 0 && (short)this.turn.Troop < 0x80)
				{
					short troopIndex = (short)this.turn.Troop;
					this.turn.TargetTerrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
					bool ocean = this.turn.TargetTerrain == MapRules.OceanTerrain;
					bool reachable;

					if (this.turn.MouseCursorSet == 2)
					{
						reachable = true;
					}
					else if (((sbyte)this.Owner.World.ChartSight[this.turn.TargetHorizontal, this.turn.TargetVertical] & (1 << (person & 0xff))) == 0)
					{
						reachable = false;
					}
					else
					{
						ushort group = this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup);

						reachable = (group == UnitKindTable.SeaUnits && ocean) ||
							(group == UnitKindTable.LandUnits && !ocean) ||
							(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0 ||
							this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;
					}

					if (reachable)
					{
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)this.turn.TargetHorizontal);
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)this.turn.TargetVertical);
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
					}

					this.turn.MouseCursorSet = 0;

					this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);

					ForgetMarkedTile(participantIndex);
				}
				else if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0 &&
					(this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation) == (ushort)person || MapView.Revealed != 0))
				{
					this.turn.TileOwner = this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation);
					this.turn.Count = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);

					this.Owner.Advisors.RenewCityScreen((short)this.turn.Count);

					this.Owner.TurnHooks.DrawStatusBar();

					this.turn.AllMoved = 0;
					ForgetMarkedTile(participantIndex);
				}
				else
				{
					this.turn.Order = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

					if (MapView.Revealed != 0 && this.turn.Order != 0xffff)
					{
						this.Owner.ArmyPicker.ShowUnitInformation(
							(short)Search.FoundPlayer,
							(short)this.turn.Order);
					}

					if (this.turn.Order != 0xffff && participantIndex == (short)Search.FoundPlayer)
					{
						this.turn.Result = this.Owner.UnitOperations.ShowBattleScreen((short)Search.FoundPlayer,
							(short)this.turn.Order);

						short chosen = (short)this.turn.Result;

						if ((this.troops.Pull(participantIndex, chosen, UnitMemory.Status) & 9) == 0)
						{
							this.turn.Troop = this.turn.Result;
							this.turn.WaitingTroop = (ushort)((this.turn.Result - 1) & 0x7f);
							this.turn.PastWaiting = 0;
							this.turn.AllMoved = 0;
							ForgetMarkedTile(participantIndex);

							return HumanOrder.NextLook;
						}
					}
					else
					{
						this.Owner.MapOperations.FocusMapView(participantIndex,
							(short)this.turn.TargetHorizontal - 7,
							(short)this.turn.TargetVertical - 6);
					}
				}
			}

			this.turn.Order = 0xffff;

			if (this.turn.Clicked != 0)
			{
				this.turn.Clicked = 0;
			}
			else
			{
				WaitForKey();
			}

			return HumanOrder.Order;
		}

		private void ForgetMarkedTile(short participantIndex)
		{
			if (this.turn.MarkedTileMode == 0)
			{
				return;
			}

			this.turn.MarkedTileMode = 0;

			if (((sbyte)this.Owner.World.ChartSight[this.turn.CursorHorizontal, this.turn.CursorVertical] & (1 << (byte)participantIndex)) != 0)
			{
				this.Owner.MapOperations.RepaintMapTile(
					(short)this.turn.CursorHorizontal,
					(short)this.turn.CursorVertical);
			}

			this.turn.CursorHorizontal = 0xffff;
		}

		private void PrepareOrder(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.turn.Troop > 127 || this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				this.turn.Order = this.Owner.Routing.CarryOutGoto(participantIndex, troopIndex);
			}
			else
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			}

			if (this.turn.Troop < UnitMemory.TroopTally)
			{
				this.turn.UnitHorizontal = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
				this.turn.UnitVertical = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);
			}
			else
			{
				this.turn.UnitHorizontal = 0;
				this.turn.UnitVertical = 0;
			}
		}

		private void DisbandUnit(short participantIndex)
		{
			this.troops.Assign(participantIndex, (short)this.turn.Troop, UnitMemory.LeftoverSteps, 0);

			this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);
		}

		private void PillageTile(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short horizontalLocation = (short)this.turn.UnitHorizontal;
			short verticalLocation = (short)this.turn.UnitVertical;

			bool may = (this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) == 0;

			if (may)
			{
				this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind));
				may = (sbyte)(byte)this.turn.UnitDetail < 0x1a && (byte)this.turn.UnitDetail != 0xe;
			}

			if (!may)
			{
				ShowHelpForHuman(GameTexts.TopicPillage);
				return;
			}

			this.turn.City = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) == 0)
			{
				return;
			}

			if (this.Owner.Combat.AffirmBreakTreaty(this.Owner.World.Towns[this.turn.City].ParticipantIndex) == 0xffff)
			{
				return;
			}

			this.turn.Result = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

			if (((byte)this.turn.Result & 6) != 0)
			{
				this.Owner.MapOperations.RemoveTileImprovements(6, horizontalLocation, verticalLocation);
			}
			else if (((byte)this.turn.Result & 0x10) != 0)
			{
				this.Owner.MapOperations.RemoveTileImprovements(0x10, horizontalLocation, verticalLocation);
			}
			else
			{
				this.Owner.MapOperations.RemoveTileImprovements(8, horizontalLocation, verticalLocation);
			}

			if (this.Owner.World.Towns[this.turn.City].ParticipantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);
			}

			RepaintTileIfVisible(this.turn.UnitHorizontal, this.turn.UnitVertical);

			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIndex,
				this.Owner.World.Towns[this.turn.City].ParticipantIndex,
				2);

			short cityOwner = this.Owner.World.Towns[this.turn.City].ParticipantIndex;

			if (participantIndex != cityOwner)
			{
				this.Owner.Computer.SendUnitToStrategicLocation(cityOwner,
					horizontalLocation, verticalLocation, 1, 4);
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
		}

		private void SkipUnit(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
		}

		private void SentryUnit(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 1));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIndex, troopIndex);
		}

		private void UpgradeUnit(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short successor = this.Owner.UnitOperations.UpgradeKind(participantIndex, troopIndex);

			if (successor == UnitUpgradeRules.NoSuccessor)
			{
				return;
			}

			short cost = this.Owner.UnitOperations.UpgradeCost(participantIndex, troopIndex);
			short gold = this.Owner.World.Participants[participantIndex].Gold;

			Message("Upgrade to ")
				.Text(GameTables.UnitText(successor))
				.Text(": $")
				.Number(cost)
				.Text("\nTreasury: $")
				.Number(gold)
				.Text("\n");

			if (gold >= cost)
			{
				this.Owner.Words.Append(" Yes\n No\n");
			}

			this.Owner.Startup.ShowMouseCursor();
			DrawingBox.TypefaceIndex = 1;

			bool chosen = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 100, 80, 1) == 0 && gold >= cost;

			this.Owner.Startup.HideMouseCursor();

			if (chosen && this.Owner.UnitOperations.UpgradeUnit(participantIndex, troopIndex))
			{
				RepaintUnitIfVisible(participantIndex, troopIndex);
			}
		}

		private void UnloadUnit(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind));

			if (Tables.UnitKindWord((sbyte)(byte)this.turn.UnitDetail, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.UnitDetail != UnitKindTable.Carrier)
			{
				return;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.FollowingInStack) == 0xff)
			{
				return;
			}

			this.Owner.UnitOperations.VisitEachUnitInStack(participantIndex, troopIndex, 0x14f6);

			this.turn.AllMoved = 0;
			this.turn.Ordered = 1;

			this.turn.Troop = (ushort)(FindBestGuardian(participantIndex, this.turn.Troop) - 1);
			this.turn.WaitingTroop = (ushort)(this.turn.Troop & 0x7f);
			this.turn.PastWaiting = 0;
		}

		private void ToggleGoTo(short participantIndex)
		{
			if (this.turn.MouseCursorSet == 0)
			{
				if (this.turn.MarkedTileMode == 0)
				{
					this.turn.MarkedTileMode = 0x1;
					this.turn.CursorHorizontal = this.Troops.PullAt((ushort)((participantIndex * 0x600) + (this.turn.Troop * 0xc)), UnitMemory.Horizontal);
					this.turn.CursorVertical = this.Troops.PullAt((ushort)((participantIndex * 0x600) + (this.turn.Troop * 0xc)), UnitMemory.Vertical);
				}
			}
			else if (this.turn.MarkedTileMode != 0)
			{
				this.turn.MarkedTileMode = 0x0;

				if ((this.Owner.World.ChartSight[this.turn.CursorHorizontal,
					this.turn.CursorVertical] & (1 << participantIndex)) != 0)
				{
					this.Owner.MapOperations.RepaintMapTile(
						(short)this.turn.CursorHorizontal,
						(short)this.turn.CursorVertical);
				}

				this.turn.CursorHorizontal = 0xffff;
			}

			if (Mouse.Present != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, Tables.SpritePictureAt((ushort)((this.turn.MouseCursorSet != 0 ? 7 : 2) << 1)));
			}

			this.turn.MouseCursorSet = (ushort)(this.turn.MouseCursorSet ^ 1);
		}

		private bool OrderHome(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			this.turn.City = this.Owner.Helpers.FindNearestCity(
				(short)this.turn.UnitHorizontal,
				(short)this.turn.UnitVertical);

			if (Search.FoundDistance == 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.BaseTown, (byte)this.turn.City);

				return false;
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				return false;
			}

			this.turn.Direction = this.Owner.UnitOperations.FindNearestFriendlyCity(participantIndex, (short)this.turn.Troop);

			if (this.turn.Direction != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 2));
				return true;
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xfd));

				return false;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);

			return false;
		}

		private void CleanPollution(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicSettlers);
				return;
			}

			this.turn.TargetImprovements = this.Owner.MapOperations.ObtainTileImprovements(
				(short)this.turn.UnitHorizontal,
				(short)this.turn.UnitVertical);

			if (((byte)this.turn.TargetImprovements & 0x40) == 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x7d));
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);

				this.Owner.UnitOperations.PanMapToPosition(participantIndex,
					(short)this.turn.UnitHorizontal,
					(short)this.turn.UnitVertical);

				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 0x82));

			RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) + 1));

			if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps) < 4)
			{
				return;
			}

			this.Owner.MapOperations.RemoveTileImprovements(0x40,
				(short)this.turn.UnitHorizontal,
				(short)this.turn.UnitVertical);

			this.Owner.MapOperations.KeepKnownTileImprovements(
				(short)this.turn.UnitHorizontal,
				(short)this.turn.UnitVertical);

			this.Owner.World.DirtyCellTally--;
			RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x7d));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
		}

		private void FortifyOrBuildFortress(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				return;
			}

			short horizontalLocation = (short)this.turn.UnitHorizontal;
			short verticalLocation = (short)this.turn.UnitVertical;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 4));
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

				RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

				if (participantIndex != this.Owner.World.PersonParticipantIndex && (this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
				{
					ushort homeCity = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.BaseTown, (byte)homeCity);
				}

				return;
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0x21) != 0 ||
				this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x2d) == 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x3f));
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);

				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);

				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 0xc0));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

			short work = (short)((sbyte)Tables.TerrainField(this.turn.TargetTerrain, TerrainTable.MoveCost) + 4);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) + 1));

			if (work > this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps))
			{
				return;
			}

			this.Owner.MapOperations.AssignTileImprovements(0x20, horizontalLocation, verticalLocation);

			this.Owner.UnitOperations.ShiftUnit(horizontalLocation, verticalLocation, participantIndex, (short)this.turn.Troop, 1);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x3f));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
		}

		private (short TroopIndex, ushort Unit, short HorizontalLocation, short VerticalLocation) StartTerrainWork(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIndex, troopIndex);
			short horizontalLocation = (short)this.turn.UnitHorizontal;
			short verticalLocation = (short)this.turn.UnitVertical;

			return (troopIndex, unit, horizontalLocation, verticalLocation);
		}

		private void BuildRoad(short participantIndex)
		{
			(short troopIndex, _, short horizontalLocation, short verticalLocation) = StartTerrainWork(participantIndex);

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicSettlersSecond);
				return;
			}

			this.turn.TargetImprovements = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);
			byte improvements = (byte)this.turn.TargetImprovements;
			bool refused;

			if ((improvements & 8) != 0)
			{
				refused = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x31) == 0 ||
					(improvements & 1) != 0 || (improvements & 0x10) != 0;
			}
			else
			{
				refused = (improvements & 0x10) != 0;
			}

			if (refused)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xfd));
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);

				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);

				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

			if (this.turn.TargetTerrain == 0xb &&
				this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x13) == 0)
			{
				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | 2));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

			short factor = (short)(((byte)this.turn.TargetImprovements & 8) != 0 ? 4 : 2);
			short worth = TerrainWorkByte(0x28e);
			short work = (short)(factor * worth);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) + 1));

			if (work > this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps))
			{
				return;
			}

			this.Owner.MapOperations.AssignTileImprovements(
				(ushort)((((byte)this.turn.TargetImprovements & 8) != 0) ? 0x10 : 8),
				horizontalLocation, verticalLocation);

			this.Owner.UnitOperations.ShiftUnit(horizontalLocation, verticalLocation, participantIndex, (short)this.turn.Troop, 1);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
		}

		private short TerrainWorkByte(ushort offset)
		{
			return (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)((this.turn.TargetTerrain * 0x13) + offset));
		}

		private void IrrigateTile(short participantIndex)
		{
			(short troopIndex, ushort unit, short horizontalLocation, short verticalLocation) = StartTerrainWork(participantIndex);

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0 || (this.Owner.World.DiagonalMarkers & 2) == 0 ||
				(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 2) != 0)
			{
				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
				{
					ShowHelpForHuman(GameTexts.TopicSettlersThird);
				}
				else
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
				}

				StopWork(participantIndex, troopIndex, 0xbf);
				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
			this.turn.Result = Tables.TerrainImprovementWord(this.turn.TargetTerrain, TerrainImprovementTable.Irrigation);

			if (this.turn.Result == 0xffff)
			{
				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);
				ShowHelpForHuman(GameTexts.TopicNoIrrigation);
				StopWork(participantIndex, troopIndex, 0xbf);
				return;
			}

			if (this.turn.Result == 0xfffe && CheckCoastalTile(horizontalLocation, verticalLocation) == 0)
			{
				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);
				ShowHelpForHuman(GameTexts.TopicNoWater);
				StopWork(participantIndex, troopIndex, 0xbf);
				return;
			}

			if (!DoTileWork(participantIndex, troopIndex, 0x40, 0x44c))
			{
				return;
			}

			if ((short)this.turn.Result >= 0)
			{
				ChangeTerrain(horizontalLocation, verticalLocation);
			}
			else
			{
				this.Owner.MapOperations.RemoveTileImprovements(4, horizontalLocation, verticalLocation);
				this.Owner.MapOperations.AssignTileImprovements(2, horizontalLocation, verticalLocation);
			}

			FinishTileWork(participantIndex, troopIndex, 0xbf);
		}

		private void MineTile(short participantIndex)
		{
			(short troopIndex, ushort unit, short horizontalLocation, short verticalLocation) = StartTerrainWork(participantIndex);

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0 ||
				(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 4) != 0)
			{
				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
				{
					ShowHelpForHuman(GameTexts.TopicSettlersFourth);
				}
				else
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
				}

				StopWork(participantIndex, troopIndex, 0x7f);
				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
			this.turn.Result = Tables.TerrainImprovementWord(this.turn.TargetTerrain, TerrainImprovementTable.Mining);

			if (this.turn.Result == 0xffff)
			{
				this.Owner.UnitOperations.PanMapToPosition(participantIndex, horizontalLocation, verticalLocation);
				ShowHelpForHuman(GameTexts.TopicNomine);
				StopWork(participantIndex, troopIndex, 0x7f);
				return;
			}

			if (!DoTileWork(participantIndex, troopIndex, 0x80, 0x450))
			{
				return;
			}

			if ((short)this.turn.Result >= 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) + 1));

				if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps) <= 5)
				{
					return;
				}

				ChangeTerrain(horizontalLocation, verticalLocation);
			}
			else
			{
				this.Owner.MapOperations.RemoveTileImprovements(2, horizontalLocation, verticalLocation);
				this.Owner.MapOperations.AssignTileImprovements(4, horizontalLocation, verticalLocation);
			}

			FinishTileWork(participantIndex, troopIndex, 0x7f);
		}

		private void StopWork(short participantIndex, short troopIndex, byte mask)
		{
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & mask));
		}

		private bool DoTileWork(short participantIndex, short troopIndex, byte bit, ushort offset)
		{
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) | bit));

			RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			ushort terrainOffset = (ushort)(this.turn.TargetTerrain * 0xc);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) + 1));

			return (short)this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(terrainOffset + offset)) <=
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps);
		}

		private void ChangeTerrain(short horizontalLocation, short verticalLocation)
		{
			this.Owner.Services.AssignDot(2, horizontalLocation, verticalLocation, this.turn.Result);
			this.Owner.World.ChartSight[this.turn.UnitHorizontal, this.turn.UnitVertical] |= 1;
			this.Owner.MapOperations.RemoveTileImprovements(6, horizontalLocation, verticalLocation);
		}

		private void FinishTileWork(short participantIndex, short troopIndex, byte mask)
		{
			RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & mask));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, 0);
		}

		private bool CarryOutOrder(short participantIndex)
		{
			PrepareOrder(participantIndex);

			switch (this.turn.Order)
			{
				case KeyCodes.Space:
					SkipUnit(participantIndex);
					break;

				case 'D':
					DisbandUnit(participantIndex);
					break;

				case 'P':
					PillageTile(participantIndex);
					break;

				case 'b':
					BuildCity(participantIndex);
					break;

				case 'e':
					UpgradeUnit(participantIndex);
					break;

				case 'f':
					FortifyOrBuildFortress(participantIndex);
					break;

				case 'g':
					ToggleGoTo(participantIndex);
					break;

				case 'h':
					return OrderHome(participantIndex);

				case 'i':
					IrrigateTile(participantIndex);
					break;

				case 'm':
					MineTile(participantIndex);
					break;

				case 'p':
					CleanPollution(participantIndex);
					break;

				case 'r':
					BuildRoad(participantIndex);
					break;

				case 's':
					SentryUnit(participantIndex);
					break;

				case 'u':
					UnloadUnit(participantIndex);
					break;

				case 'w':
					this.turn.Ordered = 0x1;
					this.turn.PastWaiting = 0x1;
					break;
			}

			return false;
		}

		private void BuildCity(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			short horizontalLocation = (short)this.turn.UnitHorizontal;
			short verticalLocation = (short)this.turn.UnitVertical;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicSettlersFifth);
				return;
			}

			if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain || verticalLocation < 2 || verticalLocation >= 0x30)
			{
				return;
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				this.turn.City = this.Owner.MapOperations.FindCityAtTile(horizontalLocation, verticalLocation);

				if ((sbyte)this.Owner.World.Towns[this.turn.City].TrueExtent >= 0xa)
				{
					ShowHelpForHuman(GameTexts.TopicAddCity);
					return;
				}

				this.Owner.World.Towns[this.turn.City].TrueExtent++;
				this.indexDestination = (ushort)(participantIndex * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

				this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);

				RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);

				return;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			this.turn.Result = this.Owner.Founding.EstablishCity(participantIndex,
				this.turn.Direction,
				horizontalLocation, verticalLocation,
				1);

			if (this.turn.Result != 0xffff)
			{
				this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);

				RepaintUnitIfVisible(participantIndex, (short)this.turn.Troop);
			}
		}
	}
}
