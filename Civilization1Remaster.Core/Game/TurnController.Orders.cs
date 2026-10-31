
namespace Civilization1Remaster.Core.Game
{
	/// <summary>The orders of a unit: from the human (keys, clicks) or the AI, and carrying them out</summary>
	partial class TurnController
	{
		/// <summary>Where HandlePlayerTurn goes on after the human chose (TakeHumanOrder)</summary>
		private enum HumanOrder
		{
			/// <summary>The order 0x44 of the unit, or none (L0d00)</summary>
			Order,

			/// <summary>A menu chose the order 0x44 of the unit (L0d0a)</summary>
			MenuOrder,

			/// <summary>A menu chose without a unit (L3365)</summary>
			MenuWithoutUnit,

			/// <summary>The menu bar chose nothing, or a click picked another unit (L3c40)</summary>
			NextLook,

			/// <summary>The palace area clicked without a unit: the end of the turn (L3ea1)</summary>
			EndOfTurn,
		}

		/// <summary>
		/// The human chooses what the unit 0x32 does: its panel (with the help of the first turn and for settlers), the
		/// blinking unit, then a key (0x44; Enter with a marked tile 0x4 acts as a click on it) or a click: the menu bar, the
		/// world map (moves the view), the palace, a click held (2) on the map is a destination (a neighbouring tile, also across
		/// the date line) or, on a seen tile, the Civilopedia of its terrain; a click (1) with a GoTo pointer (0x36) picks a
		/// destination the unit may reach, on a city of the human the city screen, on an own unit that unit (with its panel when
		/// everything is shown, 0xd806), else the view moves there. At the end 0x44 is -1 and the keys wait unless 0xc.
		/// The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private HumanOrder TakeHumanOrder(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			DrawUnitInfo(participantIdx, this.turn.Troop);

			if (this.Owner.World.RoundTally == 0 && this.turn.LaterPass == 0)
			{
				this.Owner.Codes.Set(0, CivilizationPlural(participantIdx));

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
				this.turn.HumanTookKey = 1;

			if (this.Owner.GlobDb3a == 0)
			{
				this.turn.Order = this.Owner.Dialogs.ReadNavigationKey();

				if (this.turn.Order != KeyCodes.Enter || this.turn.MarkedTileMode == 0)
					return HumanOrder.Order;

				// Enter on the marked tile: a click there
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
				// The menu bar
				this.Owner.MenuSystem.ManageMenuBar(participantIdx, (short)this.turn.Troop, -1);

				WaitForKey();

				if (Turn.ChosenOrder == 0xffff)
					return HumanOrder.NextLook;

				this.turn.Order = Turn.ChosenOrder;

				return (short)this.turn.Troop < 0x80 ? HumanOrder.MenuOrder : HumanOrder.MenuWithoutUnit;
			}

			if ((short)this.Owner.GlobDb3c < 0x50)
			{
				if ((short)this.Owner.GlobDb3e < 0x3a)
				{
					// The world map: the view moves there
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
				TakeHeldClick(participantIdx);

			if (this.Owner.GlobDb3a == 2 &&
				(((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & (1 << (person & 0xff))) != 0 ||
					MapView.Revealed != 0))
			{
				// The Civilopedia of the terrain of a seen tile
				this.Owner.Startup.HideMouseCursor();

				this.Owner.Encyclopedia.ShowCivilopediaRecord(this.Owner.MapOps.ObtainTerrainType(
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert), 3);

				this.Owner.TurnHooks.RepaintMainScreen();

				this.Owner.Startup.ShowMouseCursor();
			}

			return TakeClick(participantIdx);
		}

		/// <summary>The tile clicked on the map view: 0x1c / 0x26</summary>
		private void FindClickedTile()
		{
			this.turn.TargetHoriz = (ushort)this.Owner.Routing.TunePosition(
				(((short)this.Owner.GlobDb3c - 80) / 16) + (short)MapView.Horiz);
			this.turn.TargetVert = (ushort)(((short)(ushort)(this.Owner.GlobDb3e - 8) / 16) + MapView.Vert);
		}

		/// <summary>The palace of the human player, until a key or a click</summary>
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

		/// <summary>
		/// A click held on the map (GlobDb3a 2): when the button is let go on a tile next to the unit 0x32 (also across the date
		/// line, 0x18) it is the destination of the unit (GlobDb3a 0, the normal pointer), else the click stays held (2).
		/// </summary>
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

			// Across the date line
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

		/// <summary>
		/// A click (GlobDb3a 1) on the tile 0x1c / 0x26, see TakeHumanOrder; then 0x44 is -1 and the keys wait unless 0xc.
		/// </summary>
		private HumanOrder TakeClick(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			if (this.Owner.GlobDb3a == 1)
			{
				if (this.turn.MouseCursorSet != 0 && (short)this.turn.Troop < 0x80)
				{
					// The GoTo pointer: a destination the unit may reach (any for 2)
					short troopIdx = (short)this.turn.Troop;
					this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
					bool ocean = this.turn.TargetTerrain == MapRules.OceanTerrain;
					bool reachable;

					if (this.turn.MouseCursorSet == 2)
						reachable = true;
					else if (((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & (1 << (person & 0xff))) == 0)
						reachable = false;
					else
					{
						ushort group = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);

						// A ship on the sea, a land unit on land, any unit into a city, a plane anywhere
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
					// A city of the human (any with everything shown): the city screen
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
						// An own unit (from its stack): it is the unit now, unless sentried or fortified
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
				this.turn.Clicked = 0;
			else
				WaitForKey();

			return HumanOrder.Order;
		}

		/// <summary>The marked tile (0x4, at 0xe / 0x12) is forgotten and repainted when the player sees it</summary>
		private void ForgetMarkedTile(short participantIdx)
		{
			if (this.turn.MarkedTileMode == 0)
				return;

			this.turn.MarkedTileMode = 0;

			if (((sbyte)this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (byte)participantIdx)) != 0)
			{
				this.Owner.MapOps.RepaintMapTile(
					(short)this.turn.CursorHoriz,
					(short)this.turn.CursorVert);
			}

			this.turn.CursorHoriz = 0xffff;
		}

		/// <summary>
		/// Before an order: a unit with a destination (or no unit, 0x32 from 128) goes on with GoTo (CarryOutGoto gives the
		/// order 0x44), else it forgets its heading; 0x2c / 0x30 are its place (0 without a unit). The locals of HandlePlayerTurn
		/// are used as in the original, SI as the original leaves it.
		/// </summary>
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

		/// <summary>'D': the unit is disbanded</summary>
		private void DisbandUnit(short participantIdx)
		{
			this.troops.Assign(participantIdx, (short)this.turn.Troop, UnitMemory.LeftoverSteps, 0);

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);
		}

		/// <summary>
		/// 'P': a land unit (not a diplomat, caravan or fighter) outside a city with moves left pillages the tile: irrigation and
		/// mine, else a railroad, else a road go (a human seen tile is remembered, the treaty with the owner of the nearest city
		/// may break and he gets the place as a strategic location); else the help message. The locals of HandlePlayerTurn are
		/// used as in the original.
		/// </summary>
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
				return;

			// (no when the human keeps the treaty)
			if (this.Owner.Combat.AffirmBreakTreaty(this.Owner.World.Towns[this.turn.City].ParticipantIdx) == 0xffff)
				return;

			this.turn.Result = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			if (((byte)this.turn.Result & 6) != 0)
				this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);
			else if (((byte)this.turn.Result & 0x10) != 0)
				this.Owner.MapOps.RemoveTileImprovements(0x10, horizLoc, vertLoc);
			else
				this.Owner.MapOps.RemoveTileImprovements(8, horizLoc, vertLoc);

			if (this.Owner.World.Towns[this.turn.City].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

			RepaintTileIfVisible(this.turn.UnitHoriz, this.turn.UnitVert);

			this.Owner.Politics.ClearDiplomacyFlags(this.Owner.World.PersonParticipantIdx,
				this.Owner.World.Towns[this.turn.City].ParticipantIdx,
				2);

			// The owner of the city defends the tile (ClassicPillagePlayer: DI as the original leaves it, see indexDest)
			short cityOwner = this.Owner.World.Towns[this.turn.City].ParticipantIdx;

			if (participantIdx != cityOwner)
				this.Owner.Bot.SendUnitToStrategicLocation(this.Owner.TurnHooks.ClassicPillagePlayer ? (short)this.indexDest : cityOwner,
					horizLoc, vertLoc, 1, 4);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>Space: the unit does nothing this turn</summary>
		private void SkipUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>'s': the unit is sentried</summary>
		private void SentryUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 1));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, troopIdx);
		}

		/// <summary>
		/// 'u': a ship that carries units (or a carrier) in a stack wakes the units on board; the best defender of the stack
		/// is the next unit (0x32 one before it). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void UnloadUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if (UnitKindWord((sbyte)(byte)this.turn.UnitDetail, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.UnitDetail != UnitKindTable.Carrier)
			{
				return;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return;

			this.Owner.UnitOps.VisitEachUnitInStack(participantIdx, troopIdx, 0x14f6);

			this.turn.AllMoved = 0;
			this.turn.Ordered = 1;

			this.turn.Troop = (ushort)(FindBestGuardian(participantIdx, this.turn.Troop) - 1);
			this.turn.WaitingTroop = (ushort)(this.turn.Troop & 0x7f);
			this.turn.PastWaiting = 0;
		}

		/// <summary>
		/// 'g': GoTo on (0x36), with the square cursor on the unit (0x4 at 0xe / 0x12), or off, the cursor removed; with a
		/// mouse its pointer changes. The original game showed the square cursor (moved with the arrow keys, Enter picks the
		/// destination) only when there was no mouse, with a mouse it only changed the mouse pointer, which was easy to miss.
		/// The square cursor is now shown in both cases, so the destination can be picked with the keyboard or the mouse.
		/// </summary>
		private void ToggleGoTo(short participantIdx)
		{
			if (this.turn.MouseCursorSet == 0)
			{
				if (this.turn.MarkedTileMode == 0)
				{
					this.turn.MarkedTileMode = 0x1;
					this.turn.CursorHoriz = this.Troops.PullAt(((ushort)(participantIdx * 0x600 + this.turn.Troop * 0xc)), UnitMemory.Horiz);
					this.turn.CursorVert = this.Troops.PullAt(((ushort)(participantIdx * 0x600 + this.turn.Troop * 0xc)), UnitMemory.Vert);
				}
			}
			else if (this.turn.MarkedTileMode != 0)
			{
				// GoTo cancelled, remove the square cursor
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
				// GoTo mouse pointer, or the normal one when GoTo is cancelled
				this.Owner.Services.AssignMouseCursorImage(0, 0, SpritePictureAt((ushort)((this.turn.MouseCursorSet != 0 ? 7 : 2) << 1)));
			}

			this.turn.MouseCursorSet = (ushort)(this.turn.MouseCursorSet ^ 1);
		}

		/// <summary>
		/// 'h': in a city (distance 0 to the nearest, 0x10) it becomes the home city of the unit; a plane elsewhere flies to the
		/// nearest own city (true: status bit 1, the move follows), without one a human plane forgets that order and a computer
		/// plane is lost. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
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
				return false;

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

		/// <summary>
		/// 'p': settlers clean up pollution (status bits 1 and 7, the work counter in byte 5): after 4 turns the pollution is
		/// gone; on a tile without pollution the order is forgotten (and the view moves to the unit). Other units: the help
		/// message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void CleanPollution(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);

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
				return;

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

		/// <summary>
		/// 'f': a land unit fortifies (status bit 2; a computer unit in a city makes it its home city); settlers build a
		/// fortress (bits 6 and 7, the work counter in byte 5 up to the defence of the terrain 0x3e + 4 – the terrain of the
		/// last tile clicked or looked at, as the original reads it) when they know Construction (0x2d) and the tile has no city
		/// or fortress, else the order is forgotten (and the view moves to the unit). The locals of HandlePlayerTurn are used
		/// as in the original.
		/// </summary>
		private void FortifyOrBuildFortress(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

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

			short work = (short)((sbyte)TerrainField(this.turn.TargetTerrain, TerrainTable.MoveCost) + 4);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.Owner.MapOps.AssignTileImprovements(0x20, horizLoc, vertLoc);

			this.Owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, (short)this.turn.Troop, 1);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>
		/// The unit that works the terrain (the locals 0x32 the unit, 0x2c / 0x30 its square of HandlePlayerTurn): its index,
		/// its place in the table of units and its square; BX and SI as the original sets them
		/// </summary>
		private (short TroopIdx, ushort Unit, short HorizLoc, short VertLoc) StartTerrainWork(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;

			return (troopIdx, unit, horizLoc, vertLoc);
		}

		/// <summary>
		/// 'r': settlers build a road (status bit 1, the work counter in byte 5 up to 2 – 4 for a railroad – times the
		/// byte 0x28e of the terrain), a railroad on a road with Railroad (0x31) outside cities; not on a railroad, not on a
		/// river (0xb) without Bridge Building (0x13); refused: the order is forgotten (and the view moves to the unit). Other
		/// units: the help message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BuildRoad(short participantIdx)
		{
			(short troopIdx, ushort unit, short horizLoc, short vertLoc) = StartTerrainWork(participantIdx);

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
				// A railroad on the road: with Railroad, not in a city
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
				return;

			this.Owner.MapOps.AssignTileImprovements(
				(ushort)((((byte)this.turn.TargetImprovements & 8) != 0) ? 0x10 : 8),
				horizLoc, vertLoc);

			this.Owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, (short)this.turn.Troop, 1);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>A signed byte of the terrain 0x3e in the table of terrains (0x13 bytes each)</summary>
		private short TerrainWorkByte(ushort offset)
		{
			return (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)((this.turn.TargetTerrain * 0x13) + offset));
		}

		/// <summary>
		/// 'i': settlers next to water (0xd7fa bit 1) irrigate a tile not irrigated yet (status bit 6, the work counter in
		/// byte 5 up to the word +2 of the terrain in the table at 0x44a, 12 bytes a terrain): the terrain changes (word +0 from
		/// 0) or the tile is irrigated (a mine goes); not possible (-1) or -2 away from the coast: the help message. Refused: the
		/// view moves to the unit, the work stops (other units: the help message). The locals of HandlePlayerTurn are used as in
		/// the original.
		/// </summary>
		private void IrrigateTile(short participantIdx)
		{
			(short troopIdx, ushort unit, short horizLoc, short vertLoc) = StartTerrainWork(participantIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 || (this.Owner.World.DiagMarkers & 2) == 0 ||
				(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 2) != 0)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

	
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
					ShowHelpForHuman(GameTexts.TopicSettlersC);
				else
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.turn.Result = TerrainImprovementWord(this.turn.TargetTerrain, TerrainImprovementTable.Irrigation);

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

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x40, 0x44c))
				return;

			if ((short)this.turn.Result >= 0)
			{
				ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.Owner.MapOps.RemoveTileImprovements(4, horizLoc, vertLoc);
				this.Owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, unit, 0xbf);
		}

		/// <summary>
		/// 'm': settlers mine a tile without a mine (status bit 7, the work counter in byte 5 up to the word +6 of the terrain
		/// in the table at 0x44a): the terrain changes (word +4 from 0, after a counter over 5) or the tile gets a mine (the
		/// irrigation goes); not possible (-1): the help message. Refused: the view moves to the unit, the work stops (other
		/// units: the help message). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void MineTile(short participantIdx)
		{
			(short troopIdx, ushort unit, short horizLoc, short vertLoc) = StartTerrainWork(participantIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ||
				(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 4) != 0)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

	
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
					ShowHelpForHuman(GameTexts.TopicSettlersD);
				else
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.turn.Result = TerrainImprovementWord(this.turn.TargetTerrain, TerrainImprovementTable.Mining);

			if (this.turn.Result == 0xffff)
			{
				this.Owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				ShowHelpForHuman(GameTexts.TopicNomine);
				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x80, 0x450))
				return;

			if ((short)this.turn.Result >= 0)
			{
				// (a second step of the counter, the change only over 5)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) <= 5)
					return;

				ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.Owner.MapOps.RemoveTileImprovements(2, horizLoc, vertLoc);
				this.Owner.MapOps.AssignTileImprovements(4, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, unit, 0x7f);
		}

		/// <summary>The work of the settlers stops: their status loses the bit (mask), BX / SI as the original leaves them</summary>
		private void StopWork(short participantIdx, short troopIdx, byte mask)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
		}

		/// <summary>
		/// A turn of work of the settlers (the status bit, no moves, no destination, the counter one up): true when the counter
		/// reaches the word of the terrain 0x3e at the offset (12 bytes a terrain). The locals of HandlePlayerTurn are used as
		/// in the original.
		/// </summary>
		private bool DoTileWork(short participantIdx, short troopIdx, ushort unit, byte bit, ushort offset)
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

		/// <summary>The terrain of the tile becomes 0x3a (seen by the human player), irrigation and mine go</summary>
		private void ChangeTerrain(short horizLoc, short vertLoc)
		{
			this.Owner.Services.AssignDot(2, horizLoc, vertLoc, this.turn.Result);
			this.Owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] |= 1;
			this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);
		}

		/// <summary>The work is done: the unit is shown, its status loses the bit (mask) and the counter is 0</summary>
		private void FinishTileWork(short participantIdx, short troopIdx, ushort unit, byte mask)
		{
			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>
		/// The order 0x44 of the unit 0x32 (a key of the human, the answer of ShiftAIUnit or a standing order), after the unit
		/// went on to its destination: true when the unit moves now (a plane flying home, L1c10). The locals of
		/// HandlePlayerTurn are used as in the original.
		/// </summary>
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
					// Wait: the unit comes again in the second pass
					this.turn.Ordered = 0x1;
					this.turn.PastWaiting = 0x1;
					break;
			}

			return false;
		}

		/// <summary>
		/// 'b': settlers on land (not in the polar rows) join a city smaller than 10 or found one (EstablishCity, with the
		/// local 0x24 as the original passes it); the settlers are gone. A city of 10 or more: the help message; other units: the
		/// help message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
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
				return;

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				// The settlers join the city
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
