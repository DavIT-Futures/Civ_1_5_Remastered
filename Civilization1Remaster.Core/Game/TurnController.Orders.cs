namespace Civilization1Remaster.Core.Game
{
	public partial class TurnController
	{
		private enum HumanOrder
		{
			Order,

			MenuOrder,

			MenuWithoutUnit,

			NextLook,

			EndOfTurn,
		}

		private HumanOrder TakeHumanOrder(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			DrawUnitInfo(participantIdx, this.turn.Troop);

			if (this.Owner.World.RoundTally == 0 && this.turn.LaterPass == 0)
			{
				this.Owner.Codes.Set(0, Tables.CivilizationPlural(participantIdx));

				this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstmove);

				this.turn.LaterPass = 1;
			}

			if (this.turn.Troop < UnitMemory.TroopTally)
			{
				short troopIdx = (short)this.turn.Troop;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0 && this.turn.LaterPass == 0)
				{
					this.Owner.HelpSystem.ShowTileInfo(participantIdx, this.turn.Troop);
					this.turn.LaterPass = 1;
				}
			}

			this.Owner.UnitOps.ChoreographUnitMove(participantIdx,
				(short)this.turn.Troop,
				this.turn.CursorHoriz,
				this.turn.CursorVert);

			if (this.turn.Troop != 0x80)
			{
				this.turn.HumanTookKey = 1;
			}

			if (this.Owner.GlobDb3a == 0)
			{
				this.turn.Order = this.Owner.Dialogs.ReadNavigationKey();

				if (this.turn.Order != KeyCodes.Enter || this.turn.MarkedTileMode == 0)
				{
					return HumanOrder.Order;
				}

				this.turn.TargetHoriz = this.turn.CursorHoriz;
				this.turn.TargetVert = this.turn.CursorVert;
				this.Owner.GlobDb3c = 0x50;
				this.Owner.GlobDb3e = 0x8;
				this.Owner.GlobDb3a = 0x1;

				return TakeClick(participantIdx);
			}

			FindClickedTile();

			if ((short)this.Owner.GlobDb3e < 8)
			{
				this.Owner.MenuSystem.ManageMenuBar(participantIdx, (short)this.turn.Troop, -1);

				WaitForKey();

				if (Turn.ChosenOrder == 0xffff)
				{
					return HumanOrder.NextLook;
				}

				this.turn.Order = Turn.ChosenOrder;

				return (short)this.turn.Troop < 0x80 ? HumanOrder.MenuOrder : HumanOrder.MenuWithoutUnit;
			}

			if ((short)this.Owner.GlobDb3c < 0x50)
			{
				if ((short)this.Owner.GlobDb3e < 0x3a)
				{
					this.Owner.MapOps.FocusMapView(participantIdx,
						(short)(ushort)this.Owner.Routing.TunePosition(
						(short)this.Owner.GlobDb3c + (short)MapView.FocusLeft - 7),
						this.Owner.Helpers.ClampValue((short)this.Owner.GlobDb3e + (short)MapView.FocusTop - 14, 0, 49));
				}
				else if ((short)this.Owner.GlobDb3e < 0x48)
				{
					ShowPalace();
				}
				else if (this.turn.Troop == 0x80)
				{
					return HumanOrder.EndOfTurn;
				}

				this.Owner.GlobDb3a = 0;
			}

			if (this.Owner.GlobDb3a == 2 && (short)this.turn.Troop < 0x80)
			{
				TakeHeldClick(participantIdx);
			}

			if (this.Owner.GlobDb3a == 2 &&
				(((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & (1 << (person & 0xff))) != 0 ||
					MapView.Revealed != 0))
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.Encyclopedia.ShowCivilopediaRecord(this.Owner.MapOps.ObtainTerrainType(
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert), 3);

				this.Owner.TurnHooks.RepaintMainScreen();

				this.Owner.Startup.ShowMouseCursor();
			}

			return TakeClick(participantIdx);
		}

		private void FindClickedTile()
		{
			this.turn.TargetHoriz = (ushort)this.Owner.Routing.TunePosition(
				(((short)this.Owner.GlobDb3c - 80) / 16) + (short)MapView.Horiz);
			this.turn.TargetVert = (ushort)(((short)(ushort)(this.Owner.GlobDb3e - 8) / 16) + MapView.Vert);
		}

		private void ShowPalace()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Throne.LoadPalaceGraphics(0);

			this.Processor.EmitUInt16(this.Processor.SegData.Value16, Screen.Box, 0x0);

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileCbackPic, 1);

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

		private void TakeHeldClick(short participantIdx)
		{
			do
			{
				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.GlobDb3a != 0);

			FindClickedTile();

			short troopIdx = (short)this.turn.Troop;

			this.turn.OtherUnit = (ushort)this.Owner.Helpers.MergeCoordinates(
				(short)((short)this.turn.TargetHoriz - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz)),
				(short)((short)this.turn.TargetVert - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)));

			if (this.Owner.Libc.AbsoluteValue((short)(this.turn.TargetHoriz - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz))) == 0x4f &&
				this.Owner.Libc.AbsoluteValue((short)(this.turn.TargetVert - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))) <= 1)
			{
				this.turn.OtherUnit = 1;
			}

			if (this.turn.OtherUnit != 1)
			{
				this.Owner.GlobDb3a = 2;
				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)this.turn.TargetHoriz);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)this.turn.TargetVert);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.Owner.GlobDb3a = 0;
			this.turn.MouseCursorSet = 0;

			this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);
		}

		private HumanOrder TakeClick(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			if (this.Owner.GlobDb3a == 1)
			{
				if (this.turn.MouseCursorSet != 0 && (short)this.turn.Troop < 0x80)
				{
					short troopIdx = (short)this.turn.Troop;
					this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
					bool ocean = this.turn.TargetTerrain == MapRules.OceanTerrain;
					bool reachable;

					if (this.turn.MouseCursorSet == 2)
					{
						reachable = true;
					}
					else if (((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & (1 << (person & 0xff))) == 0)
					{
						reachable = false;
					}
					else
					{
						ushort group = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);

						reachable = (group == UnitKindTable.SeaUnits && ocean) ||
							(group == UnitKindTable.LandUnits && !ocean) ||
							(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 ||
							this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;
					}

					if (reachable)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)this.turn.TargetHoriz);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)this.turn.TargetVert);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
					}

					this.turn.MouseCursorSet = 0;

					this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);

					ForgetMarkedTile(participantIdx);
				}
				else if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 &&
					(this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) == (ushort)person || MapView.Revealed != 0))
				{
					this.turn.TileOwner = this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc);
					this.turn.Count = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);

					this.Owner.Advisors.RenewCityScreen((short)this.turn.Count);

					this.Owner.TurnHooks.DrawStatusBar();

					this.turn.AllMoved = 0;
					ForgetMarkedTile(participantIdx);
				}
				else
				{
					this.turn.Order = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

					if (MapView.Revealed != 0 && this.turn.Order != 0xffff)
					{
						this.Owner.ArmyPicker.ShowUnitInfo(
							(short)Search.FoundPlayer,
							(short)this.turn.Order);
					}

					if (this.turn.Order != 0xffff && participantIdx == (short)Search.FoundPlayer)
					{
						this.turn.Result = this.Owner.UnitOps.ShowBattleScreen((short)Search.FoundPlayer,
							(short)this.turn.Order);

						short chosen = (short)this.turn.Result;

						if ((this.troops.Pull(participantIdx, chosen, UnitMemory.Status) & 9) == 0)
						{
							this.turn.Troop = this.turn.Result;
							this.turn.WaitingTroop = (ushort)((this.turn.Result - 1) & 0x7f);
							this.turn.PastWaiting = 0;
							this.turn.AllMoved = 0;
							ForgetMarkedTile(participantIdx);

							return HumanOrder.NextLook;
						}
					}
					else
					{
						this.Owner.MapOps.FocusMapView(participantIdx,
							(short)this.turn.TargetHoriz - 7,
							(short)this.turn.TargetVert - 6);
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

		private void ForgetMarkedTile(short participantIdx)
		{
			if (this.turn.MarkedTileMode == 0)
			{
				return;
			}

			this.turn.MarkedTileMode = 0;

			if (((sbyte)this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (byte)participantIdx)) != 0)
			{
				this.Owner.MapOps.RepaintMapTile(
					(short)this.turn.CursorHoriz,
					(short)this.turn.CursorVert);
			}

			this.turn.CursorHoriz = 0xffff;
		}

		private void PrepareOrder(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (this.turn.Troop > 127 || this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				this.turn.Order = this.Owner.Routing.CarryOutGoto(participantIdx, troopIdx);
			}
			else
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			}

			if (this.turn.Troop < UnitMemory.TroopTally)
			{
				this.turn.UnitHoriz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
				this.turn.UnitVert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			}
			else
			{
				this.turn.UnitHoriz = 0;
				this.turn.UnitVert = 0;
			}
		}

		private void DisbandUnit(short participantIdx)
		{
			this.troops.Assign(participantIdx, (short)this.turn.Troop, UnitMemory.LeftoverSteps, 0);

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);
		}

		private void PillageTile(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;

			bool may = (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			if (may)
			{
				this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));
				may = (sbyte)(byte)this.turn.UnitDetail < 0x1a && (byte)this.turn.UnitDetail != 0xe;
			}

			if (!may)
			{
				ShowHelpForHuman(GameTexts.TopicPillage);
				return;
			}

			this.turn.City = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
			{
				return;
			}

			if (this.Owner.Combat.AffirmBreakTreaty(this.Owner.World.Towns[this.turn.City].ParticipantIdx) == 0xffff)
			{
				return;
			}

			this.turn.Result = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			if (((byte)this.turn.Result & 6) != 0)
			{
				this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);
			}
			else if (((byte)this.turn.Result & 0x10) != 0)
			{
				this.Owner.MapOps.RemoveTileImprovements(0x10, horizLoc, vertLoc);
			}
			else
			{
				this.Owner.MapOps.RemoveTileImprovements(8, horizLoc, vertLoc);
			}

			if (this.Owner.World.Towns[this.turn.City].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			}

			RepaintTileIfVisible(this.turn.UnitHoriz, this.turn.UnitVert);

			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIdx,
				this.Owner.World.Towns[this.turn.City].ParticipantIdx,
				2);

			short cityOwner = this.Owner.World.Towns[this.turn.City].ParticipantIdx;

			if (participantIdx != cityOwner)
			{
				this.Owner.Bot.SendUnitToStrategicLocation(cityOwner,
					horizLoc, vertLoc, 1, 4);
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		private void SkipUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		private void SentryUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 1));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, troopIdx);
		}

		private void UnloadUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if (Tables.UnitKindWord((sbyte)(byte)this.turn.UnitDetail, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.UnitDetail != UnitKindTable.Carrier)
			{
				return;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
			{
				return;
			}

			this.Owner.UnitOps.VisitEachUnitInStack(participantIdx, troopIdx, 0x14f6);

			this.turn.AllMoved = 0;
			this.turn.Ordered = 1;

			this.turn.Troop = (ushort)(FindBestGuardian(participantIdx, this.turn.Troop) - 1);
			this.turn.WaitingTroop = (ushort)(this.turn.Troop & 0x7f);
			this.turn.PastWaiting = 0;
		}

		private void ToggleGoTo(short participantIdx)
		{
			if (this.turn.MouseCursorSet == 0)
			{
				if (this.turn.MarkedTileMode == 0)
				{
					this.turn.MarkedTileMode = 0x1;
					this.turn.CursorHoriz = this.Troops.PullAt((ushort)((participantIdx * 0x600) + (this.turn.Troop * 0xc)), UnitMemory.Horiz);
					this.turn.CursorVert = this.Troops.PullAt((ushort)((participantIdx * 0x600) + (this.turn.Troop * 0xc)), UnitMemory.Vert);
				}
			}
			else if (this.turn.MarkedTileMode != 0)
			{
				this.turn.MarkedTileMode = 0x0;

				if ((this.Owner.World.ChartSight[this.turn.CursorHoriz,
					this.turn.CursorVert] & (1 << participantIdx)) != 0)
				{
					this.Owner.MapOps.RepaintMapTile(
						(short)this.turn.CursorHoriz,
						(short)this.turn.CursorVert);
				}

				this.turn.CursorHoriz = 0xffff;
			}

			if (Mouse.Present != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, Tables.SpritePictureAt((ushort)((this.turn.MouseCursorSet != 0 ? 7 : 2) << 1)));
			}

			this.turn.MouseCursorSet = (ushort)(this.turn.MouseCursorSet ^ 1);
		}

		private bool OrderHome(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			this.turn.City = this.Owner.Helpers.FindNearestCity(
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			if (Search.FoundDistance == 0)
			{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)this.turn.City);

					return false;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				return false;
			}

			this.turn.Direction = this.Owner.UnitOps.FindNearestFriendlyCity(participantIdx, (short)this.turn.Troop);

			if (this.turn.Direction != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
				return true;
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));

					return false;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);

			return false;
		}

		private void CleanPollution(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicSettlers);
				return;
			}

			this.turn.TargetImprovements = this.Owner.MapOps.ObtainTileImprovements(
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			if (((byte)this.turn.TargetImprovements & 0x40) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x7d));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.Owner.UnitOps.PanMapToPosition(participantIdx,
					(short)this.turn.UnitHoriz,
					(short)this.turn.UnitVert);

				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0x82));

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) < 4)
			{
				return;
			}

			this.Owner.MapOps.RemoveTileImprovements(0x40,
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			this.Owner.MapOps.KeepKnownTileImprovements(
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			this.Owner.World.DirtyCellTally--;
			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x7d));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		private void FortifyOrBuildFortress(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				return;
			}

			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 4));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

				if (participantIdx != this.Owner.World.PersonParticipantIdx && (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				{
					ushort homeCity = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)homeCity);
				}

				return;
			}

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x21) != 0 ||
				this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0xc0));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			short work = (short)((sbyte)Tables.TerrainField(this.turn.TargetTerrain, TerrainTable.MoveCost) + 4);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
			{
				return;
			}

			this.Owner.MapOps.AssignTileImprovements(0x20, horizLoc, vertLoc);

			this.Owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, (short)this.turn.Troop, 1);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		private (short TroopIdx, ushort Unit, short HorizLoc, short VertLoc) StartTerrainWork(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;

			return (troopIdx, unit, horizLoc, vertLoc);
		}

		private void BuildRoad(short participantIdx)
		{
			(short troopIdx, _, short horizLoc, short vertLoc) = StartTerrainWork(participantIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicSettlersB);
				return;
			}

			this.turn.TargetImprovements = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);
			byte improvements = (byte)this.turn.TargetImprovements;
			bool refused;

			if ((improvements & 8) != 0)
			{
				refused = this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31) == 0 ||
					(improvements & 1) != 0 || (improvements & 0x10) != 0;
			}
			else
			{
				refused = (improvements & 0x10) != 0;
			}

			if (refused)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (this.turn.TargetTerrain == 0xb &&
				this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13) == 0)
			{
				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			short factor = (short)(((byte)this.turn.TargetImprovements & 8) != 0 ? 4 : 2);
			short worth = TerrainWorkByte(0x28e);
			short work = (short)(factor * worth);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
			{
				return;
			}

			this.Owner.MapOps.AssignTileImprovements(
				(ushort)((((byte)this.turn.TargetImprovements & 8) != 0) ? 0x10 : 8),
				horizLoc, vertLoc);

			this.Owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, (short)this.turn.Troop, 1);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		private short TerrainWorkByte(ushort offset)
		{
			return (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)((this.turn.TargetTerrain * 0x13) + offset));
		}

		private void IrrigateTile(short participantIdx)
		{
			(short troopIdx, ushort unit, short horizLoc, short vertLoc) = StartTerrainWork(participantIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 || (this.Owner.World.DiagMarkers & 2) == 0 ||
				(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 2) != 0)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
				{
					ShowHelpForHuman(GameTexts.TopicSettlersC);
				}
				else
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
				}

				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.turn.Result = Tables.TerrainImprovementWord(this.turn.TargetTerrain, TerrainImprovementTable.Irrigation);

			if (this.turn.Result == 0xffff)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				ShowHelpForHuman(GameTexts.TopicNoirr);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (this.turn.Result == 0xfffe && CheckCoastalTile(horizLoc, vertLoc) == 0)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				ShowHelpForHuman(GameTexts.TopicNowater);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, 0x40, 0x44c))
			{
				return;
			}

			if ((short)this.turn.Result >= 0)
			{
				ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.Owner.MapOps.RemoveTileImprovements(4, horizLoc, vertLoc);
				this.Owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, 0xbf);
		}

		private void MineTile(short participantIdx)
		{
			(short troopIdx, ushort unit, short horizLoc, short vertLoc) = StartTerrainWork(participantIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ||
				(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 4) != 0)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
				{
					ShowHelpForHuman(GameTexts.TopicSettlersD);
				}
				else
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
				}

				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.turn.Result = Tables.TerrainImprovementWord(this.turn.TargetTerrain, TerrainImprovementTable.Mining);

			if (this.turn.Result == 0xffff)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				ShowHelpForHuman(GameTexts.TopicNomine);
				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, 0x80, 0x450))
			{
				return;
			}

			if ((short)this.turn.Result >= 0)
			{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

					if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) <= 5)
				{
					return;
				}

					ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.Owner.MapOps.RemoveTileImprovements(2, horizLoc, vertLoc);
				this.Owner.MapOps.AssignTileImprovements(4, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, 0x7f);
		}

		private void StopWork(short participantIdx, short troopIdx, byte mask)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
		}

		private bool DoTileWork(short participantIdx, short troopIdx, byte bit, ushort offset)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | bit));

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			ushort terrainOffset = (ushort)(this.turn.TargetTerrain * 0xc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			return (short)this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(terrainOffset + offset)) <=
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps);
		}

		private void ChangeTerrain(short horizLoc, short vertLoc)
		{
			this.Owner.Services.AssignDot(2, horizLoc, vertLoc, this.turn.Result);
			this.Owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] |= 1;
			this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);
		}

		private void FinishTileWork(short participantIdx, short troopIdx, byte mask)
		{
			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		private bool CarryOutOrder(short participantIdx)
		{
			PrepareOrder(participantIdx);

			switch (this.turn.Order)
			{
				case KeyCodes.Space:
					SkipUnit(participantIdx);
					break;

				case 'D':
					DisbandUnit(participantIdx);
					break;

				case 'P':
					PillageTile(participantIdx);
					break;

				case 'b':
					BuildCity(participantIdx);
					break;

				case 'f':
					FortifyOrBuildFortress(participantIdx);
					break;

				case 'g':
					ToggleGoTo(participantIdx);
					break;

				case 'h':
					return OrderHome(participantIdx);

				case 'i':
					IrrigateTile(participantIdx);
					break;

				case 'm':
					MineTile(participantIdx);
					break;

				case 'p':
					CleanPollution(participantIdx);
					break;

				case 'r':
					BuildRoad(participantIdx);
					break;

				case 's':
					SentryUnit(participantIdx);
					break;

				case 'u':
					UnloadUnit(participantIdx);
					break;

				case 'w':
					this.turn.Ordered = 0x1;
					this.turn.PastWaiting = 0x1;
					break;
			}

			return false;
		}

		private void BuildCity(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicSettlersE);
				return;
			}

			if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain || vertLoc < 2 || vertLoc >= 0x30)
			{
				return;
			}

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.turn.City = this.Owner.MapOps.FindCityAtTile(horizLoc, vertLoc);

				if ((sbyte)this.Owner.World.Towns[this.turn.City].TrueExtent >= 0xa)
				{
					ShowHelpForHuman(GameTexts.TopicAddcity);
					return;
				}

				this.Owner.World.Towns[this.turn.City].TrueExtent++;
				this.indexDest = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);

				RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.turn.Result = this.Owner.Founding.EstablishCity(participantIdx,
				this.turn.Direction,
				horizLoc, vertLoc,
				1);

			if (this.turn.Result != 0xffff)
			{
				this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);

				RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);
			}
		}
	}
}
