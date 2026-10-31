using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1403.
	/// </summary>
	public partial class PlayerTurn : GameModule
	{
		private readonly UnitMemory troops;

		public PlayerTurn(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>The locals of the running HandlePlayerTurn</summary>
		private TurnLocals turn = new TurnLocals();

		/// <summary>
		/// DI of the original HandlePlayerTurn, which PillageTile passes as the player of the strategic location: whatever the
		/// turn left there last (the unit's column in FocusOnUnit, the player's units address, the city owner of a diplomat,
		/// a caravan's home and its continent, a guardian, the land group, the unit kind, the cargo, ...). HandlePlayerTurn
		/// gives it back as it found it (not after an exception), 0 at the start like the register; the other functions of
		/// the game keep DI for their callers.
		/// </summary>
		private ushort indexDest;

		/// <summary>The unit of the running HandlePlayerTurn (the local 0x32), for the tests that click next to it</summary>
		public ushort ActiveTroop => this.turn.Troop;

		/// <summary>
		/// The turn of a player: passes over his units (another pass while some unit waited or was off the view), each unit
		/// gets its orders (from ShiftAIUnit, its standing orders or the keys and clicks of the human) and carries them out
		/// or moves; the human gets a last look without a unit at the end of the turn. The locals (TurnLocals) are shared by
		/// the methods it calls, as in the original; the frame (0x52) stays so that the called functions run at the same
		/// depth of the stack.
		/// </summary>
		public void HandlePlayerTurn(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePlayerTurn({participantIdx})");

			TurnLocals outer = this.turn;
			this.turn = new TurnLocals();
			ushort outerDest = this.indexDest;

			// The player, the help of some turns, the moves of the units
			PrepareTurn(participantIdx);

			bool newPass = true;

			while (true)
			{
				if (newPass)
				{
					// A pass over the units (0x1e stays 1 unless a unit is left with moves)
					this.turn.AllMoved = 1;
					this.turn.Troop = 0;
				}

				bool endOfTurn = false;

				while ((short)this.turn.Troop < UnitMemory.TroopTally)
				{
					if (!VisitUnit(participantIdx))
					{
						endOfTurn = true;
						break;
					}

					this.turn.Troop = (ushort)(this.turn.Troop + 1);
				}

				if (endOfTurn)
					break;

				// After all units: another pass while units still move, else the look at the end of the turn of the human
				// (no unit of his took a key, or the option 4), else the end
				newPass = true;

				if (this.turn.AllMoved == 0 && GameEndRequest == 0)
					continue;

				if (GameEndRequest != 0 || !this.owner.World.IsHuman(participantIdx) ||
					(this.turn.HumanTookKey != 0 && (this.owner.World.SessionPrefMarkers & 4) == 0))
				{
					break;
				}

				this.turn.Troop = UnitMemory.TroopTally;
				this.turn.AllMoved = 0;
				this.turn.LaterPass = 1;

				if (!LookAtUnit(participantIdx))
					break;

				this.turn.Troop = (ushort)(this.turn.Troop + 1);
				newPass = false;
			}

			// The end of the turn: the human's "end of turn" line goes away
			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Startup.ConcealMouseCursor();
				this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);
				this.owner.Startup.DisplayMouseCursor();
			}

			this.indexDest = outerDest;
			this.turn = outer;
			this.processor.Journal.CloseScope("HandlePlayerTurn");
		}

		/// <summary>Where the look at a unit goes on (LookAtUnit)</summary>
		private enum LookStep
		{
			/// <summary>The order 0x44 is carried out (L0d0a)</summary>
			Order,

			/// <summary>The order of a unit, or the key without a unit (L0d00)</summary>
			OrderOrKey,

			/// <summary>The direction of a move from the order (L1b5e)</summary>
			Direction,

			/// <summary>The marked tile or the unit moves in the direction 0x24 (L1c10)</summary>
			Move,

			/// <summary>The key that is neither an order nor a move (L3365)</summary>
			Key,

			/// <summary>The unit goes on or its move ends (L3c40)</summary>
			NextLook,

			/// <summary>The end of the turn (L3ea1)</summary>
			EndOfTurn,
		}

		/// <summary>
		/// The unit 0x32 in a pass: the screen repainted when asked (0x1ae0); the quit leaves the units; the unit 0x46 sets
		/// 0x2e; skipped units and units of the human off the view go to the next one, else the look at the unit. False at
		/// the end of the turn.
		/// </summary>
		private bool VisitUnit(short participantIdx)
		{
			if (ScreenRepaintRequest != 0)
				this.owner.TurnHooks.RepaintMainScreen();

			if (GameEndRequest != 0)
			{
				// The quit leaves the units
				this.turn.Troop = UnitMemory.TroopTally;
				return true;
			}

			if (this.turn.Troop == this.turn.WaitingTroop)
				this.turn.PastWaiting = 1;

			// Empty slots, sentried and fortified units and units without moves are skipped; fortifying ones are fortified now
			if (!StartUnitMove(participantIdx))
				return true;

			// A unit of the human player without orders off the map view waits for the second pass, else the view moves to it
			if (!FocusOnUnit(participantIdx))
				return true;

			return LookAtUnit(participantIdx);
		}

		/// <summary>
		/// The look at the unit 0x32 (or at no unit, 0x80): the keyboard emptied for the human, then orders until the unit
		/// has no moves left (each order: the order, the move in its direction or the key that is neither). False at the end
		/// of the turn.
		/// </summary>
		private bool LookAtUnit(short participantIdx)
		{
			if (this.owner.World.IsHuman(participantIdx))
				AwaitForKey();

			this.turn.AITries = 0;

			if (this.turn.MouseCursorSet != 0)
				this.owner.Services.AssignMouseCursorImage(0, 0, PointerPicture);

			this.turn.Clicked = 0;
			this.turn.MouseCursorSet = 0;
			this.turn.Ordered = 0;

			while (true)
			{
				LookStep step = ObtainOrder(participantIdx);

				while (step != LookStep.NextLook && step != LookStep.EndOfTurn)
				{
					switch (step)
					{
						case LookStep.OrderOrKey:
							step = (short)this.turn.Troop < UnitMemory.TroopTally ? LookStep.Order : LookStep.Direction;
							break;

						case LookStep.Order:
							// The order 0x44 of the unit (a plane flying home moves at once)
							step = CarryOutOrder(participantIdx) ? LookStep.Move : LookStep.Direction;
							break;

						case LookStep.Direction:
							step = ChooseDirection(participantIdx);
							break;

						case LookStep.Move:
							step = MoveTheUnit(participantIdx);
							break;

						default:
							// The keys of the reports, the menus, the rates and the view
							step = CarryOutKey(participantIdx) switch
							{
								KeyStep.MenuOrder => LookStep.Order,
								KeyStep.EndOfTurn => LookStep.EndOfTurn,
								_ => LookStep.NextLook,
							};
							break;
					}
				}

				if (step == LookStep.EndOfTurn)
					return false;

				// A unit with moves left goes on; else its move ends: a plane burns fuel, a trireme may be lost at sea
				if (!ContinueOrFinishUnit(participantIdx))
					return true;
			}
		}

		/// <summary>
		/// The order of the unit: a computer unit gets it from ShiftAIUnit, a unit with orders goes on with them, else the
		/// human chooses with a key or a click
		/// </summary>
		private LookStep ObtainOrder(short participantIdx)
		{
			if (!this.owner.World.IsHuman(participantIdx))
			{
				ObtainAIOrder(participantIdx);
				return LookStep.Order;
			}

			if (ObtainStandingOrder(participantIdx))
				return LookStep.OrderOrKey;

			return TakeHumanOrder(participantIdx) switch
			{
				HumanOrder.MenuOrder => LookStep.Order,
				HumanOrder.MenuWithoutUnit => LookStep.Key,
				HumanOrder.NextLook => LookStep.NextLook,
				HumanOrder.EndOfTurn => LookStep.EndOfTurn,
				_ => LookStep.OrderOrKey,
			};
		}

		/// <summary>
		/// The direction of a move from the order 0x44 (1 - 8 from GoTo and the computer, the arrows, the keypad) into 0x24;
		/// without a unit only the marked tile moves; 'c' centers the view; other keys go on as keys
		/// </summary>
		private LookStep ChooseDirection(short participantIdx)
		{
			if ((short)this.turn.Troop >= UnitMemory.TroopTally && this.turn.MarkedTileMode == 0)
				return LookStep.Key;

			if (this.turn.Order == 'c')
			{
				CenterView(participantIdx);
				return LookStep.Key;
			}

			if (KeyDirection(this.turn.Order) == 0)
				return LookStep.Key;

			this.turn.Direction = KeyDirection(this.turn.Order);
			return LookStep.Move;
		}

		/// <summary>
		/// The move in the direction 0x24: the marked tile moves, or a barbarian pillages a tile of the human on its way, or
		/// the unit moves (the checks of the target tile, an attack, the cost, the territory, the move and the arrival).
		/// After a won or lost attack the next look comes at once, else the key of the order.
		/// </summary>
		private LookStep MoveTheUnit(short participantIdx)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				MoveMarkedTile(participantIdx);
				return LookStep.Key;
			}

			if (BarbarianPillages(participantIdx))
				return LookStep.Key;

			// The tile of the move; a land unit may be stopped by a zone of control, a diplomat or caravan acts at a city
			if (LocateMoveTarget(participantIdx))
			{
				if (StoppedByZoneOfControl(participantIdx))
					return LookStep.Key;

				switch (EnterCityAsDiplomatOrCaravan(participantIdx))
				{
					case MoveStep.Done:
						return LookStep.Key;

					case MoveStep.NoCity:
						BribeUnitOnTheWay(participantIdx);
						break;
				}
			}

			// The tile of the move: boarding a ship, the cases without an attack, an attack (the unit may be lost)
			switch (CheckMoveTile(participantIdx))
			{
				case MoveCheck.Done:
					return LookStep.Key;

				case MoveCheck.Attacks:
					if (!ConfirmAttack(participantIdx))
						return LookStep.Key;

					return FightAt(participantIdx) ? LookStep.NextLook : LookStep.Key;
			}

			// The cost of the move (a ship on land, a plane or ship into a foreign city, no moves: no move); a computer unit
			// does not turn back nor crowd an own stack; a treaty may stop the move into a territory
			if (!PayForMove(participantIdx) || !ComputerMayMove(participantIdx) || !EnterTerritory(participantIdx))
				return LookStep.Key;

			// Who sees the move, the move, landing and the cargo of ships, the arrival, the unit shown and a plane landing
			ShowMove(participantIdx);
			MoveUnit(participantIdx);
			TakeCargoAlong(participantIdx);
			ArriveAtTile(participantIdx);
			FinishMove(participantIdx);

			return LookStep.Key;
		}

		/// <summary>
		/// The start of the turn of the player: 0x6b90 the player, 0x20 the players who were played by humans, 0xe -1, 0x4 0;
		/// for the human player only he is played by a human now, with the help about the turns 20 / 60 and 40 / 80. Every unit
		/// gets its moves (3 per tile of its type, ships 3 more with Magellan's Expedition or the Lighthouse), unless 0xdf60 is 1.
		/// Then 0x6, 0x8, 0x28, 0x2e are 0 and 0x46 is 0x7f. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void PrepareTurn(short participantIdx)
		{
			TurnPlayer = (ushort)participantIdx;
			this.turn.HumanMarkers = (ushort)this.owner.World.ParticipantMarkers;
			this.turn.CursorHoriz = 0xffff;
			this.turn.MarkedTileMode = 0;

			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				this.owner.World.ParticipantMarkers = (short)(1 << (person & 0xff));

				if (this.owner.World.RoundTally == 20 || this.owner.World.RoundTally == 60)
					this.owner.HelpSystem.DisplayHelpPopup(GameTexts.TopicHelp1);

				if (this.owner.World.RoundTally == 40 || this.owner.World.RoundTally == 80)
					this.owner.HelpSystem.DisplayHelpPopup(GameTexts.TopicHelp2);
			}

			for (this.turn.Troop = 0; (short)this.turn.Troop < UnitMemory.TroopTally;
				this.turn.Troop = (ushort)(this.turn.Troop + 1))
			{
				short troopIdx = (short)this.turn.Troop;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff || MidTurnSave == 1)
					continue;

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps,
					(byte)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)));

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
					(this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 4) != 0 || this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 8) != 0))
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps,
						(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) + 3));
				}
			}

			this.turn.HumanTookKey = 0;
			this.turn.LaterPass = 0;
			this.turn.Ordered = 0;
			this.turn.PastWaiting = 0;
			this.turn.WaitingTroop = 0x7f;
		}

		/// <summary>
		/// The start of the move of the unit 0x32: none (false) for an empty slot, a sentried or fortified unit (status bits 0, 3)
		/// or one without moves. A unit of a kind from 0x1c becomes a militia; in the first pass (0x8 = 0) the unit is shifted in
		/// place (ShiftUnit). A unit that was fortifying (bit 2) is fortified now and has no moves: a computer unit in a city of
		/// size 3 or more makes it its home city, and in its home city without a foreign unit around it is kept in the list of
		/// the defenders of the city (false). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool StartUnitMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			// (SI as the original leaves it: the player's units, or the unit when it goes on or was fortifying)
			ushort player = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			ushort unit = (ushort)(player + troopIdx * UnitMemory.TroopStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
				return false;

			if (this.troops.TroopKind(participantIdx, troopIdx) >= 0x1c)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 1);

			if (this.turn.LaterPass == 0)
			{
				this.owner.UnitOps.ShiftUnit(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), participantIdx, troopIdx, 0);
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 9) != 0 ||
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
			{
				return false;
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 4) == 0)
				return true;

			// Fortifying: fortified
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfb) | 8));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, troopIdx);

			sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			if (participantIdx != this.owner.World.PersonParticipantIdx && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.turn.Result = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);

				if ((sbyte)this.owner.World.Towns[this.turn.Result].TrueExtent >= 3)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)this.turn.Result);
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.turn.Result = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);

				if ((ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == this.turn.Result &&
					this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx, horizLoc, vertLoc) == 0)
				{
					this.owner.UnitOps.KeepUnitsInCityList((short)this.turn.Result);
				}
			}

			return false;
		}

		/// <summary>
		/// A unit of the human player without orders (status bits 1, 6, 7) that is not on the map view (0xd4cc / 0xd75e, 15 x 11
		/// tiles) waits for the second pass (false, 0x1e = 0) while 0x2e is 0; else the view moves to it (after a short wait
		/// unless 0xb278 is 1), 0x46 remembers the unit before it and 0x2e is 0 again (the unit panel in the first turn).
		/// The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool FocusOnUnit(short participantIdx)
		{
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				return true;

			short troopIdx = (short)this.turn.Troop;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0)
				return true;

			// On the map view (the unsigned comparisons of the original)
			ushort horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.indexDest = horizLoc;
			ushort viewHoriz = MapViewHoriz;

			if (viewHoriz < horizLoc)
			{
				this.turn.UnitDetail = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
				ushort viewVert = MapViewVert;

				if (this.turn.UnitDetail > viewVert && (ushort)(viewHoriz + 0xe) >= horizLoc &&
					(ushort)(viewVert + 0xa) > this.turn.UnitDetail)
				{
					return true;
				}
			}

			if (this.turn.PastWaiting == 0)
			{
				this.turn.AllMoved = 0;
				return false;
			}

			if (WorldMapView != 1)
				this.owner.TextPainter.AwaitTime(30);

			this.owner.MapOps.FocusMapView(participantIdx,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 7,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - 6);

			this.turn.WaitingTroop = (ushort)((this.turn.Troop - 1) & 0x7f);
			this.turn.PastWaiting = 0;

			if (this.owner.World.RoundTally == 0)
				RenderUnitInfo(participantIdx, this.turn.Troop);

			return true;
		}

		/// <summary>
		/// The order of a computer unit (0x44) is the answer of ShiftAIUnit: with one the unit forgets its destination; after
		/// more than 4 tries (0xa) it is 0x20 and the unit forgets its heading and destination. The locals of HandlePlayerTurn
		/// are used as in the original, SI as the original leaves it.
		/// </summary>
		private void ObtainAIOrder(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.turn.Order = this.owner.Bot.ShiftAIUnit(participantIdx, troopIdx);

			if (this.turn.Order != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			}

			this.turn.AITries = (ushort)(this.turn.AITries + 1);

			if ((short)this.turn.AITries > 4)
			{
				this.turn.Order = 0x20;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			}
		}

		/// <summary>
		/// A unit of a player played by a human with orders (status bits 1, 6, 7) or a destination goes on with them (true, the
		/// key 0x44): 'r' for a destination, 'i' / 'm' for settlers / others with bit 6, 'm' with bit 7 ('f' with bits 6 and 7),
		/// 'p' with bits 7 and 1, 'h' for a plane. False: the human chooses. The locals of HandlePlayerTurn are used as in the
		/// original, SI as the original leaves it.
		/// </summary>
		private bool ObtainStandingOrder(short participantIdx)
		{
			if ((short)this.turn.Troop >= UnitMemory.TroopTally)
				return false;

			short troopIdx = (short)this.turn.Troop;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) == 0 &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
			{
				return false;
			}

			this.turn.Order = 0x72;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x40) != 0)
				this.turn.Order = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ? 0x6d : 0x69);

			this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status));

			if (((byte)this.turn.UnitDetail & 0x80) != 0)
			{
				this.turn.Order = 0x6d;

				if (((byte)this.turn.UnitDetail & 0x40) != 0)
					this.turn.Order = 0x66;

				if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 2) != 0)
					this.turn.Order = 0x70;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				this.turn.Order = 0x68;

			return true;
		}
	}
}
