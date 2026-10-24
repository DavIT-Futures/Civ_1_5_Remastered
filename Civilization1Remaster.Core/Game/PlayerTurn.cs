using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1403.
	/// </summary>
	public class PlayerTurn : GameModule
	{
		private readonly UnitMemory troops;

		public PlayerTurn(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// The locals of HandlePlayerTurn, shared by the methods it calls as in the original. In the original they were the
		/// words of the stack frame (BP - offset); the offset of each one is in its comment, the comments of the methods still
		/// name the locals by these offsets. Every local is a word; byte reads take its low byte, byte writes keep its high byte.
		/// </summary>
		private sealed class TurnLocals
		{
			private readonly CPU? processor;
			public TurnLocals(CPU? processor = null) { this.processor = processor; }

			/// <summary>The value, and the word of the frame as the original writes it (later functions read it)</summary>
			private void Keep(ref ushort local, int distance, ushort value)
			{
				local = value;
				this.processor?.EmitLocalWord(distance, value);
			}

			private ushort dot;
			/// <summary>0x2: the colour of a dot of the map view (stays on the stack: DisplayTravellersReport reads it)</summary>
			public ushort Dot
			{
				get => this.dot;
				set { this.dot = value; this.processor?.EmitLocalWord(0x2, value); }
			}
			private ushort markedTileMode;
			/// <summary>0x4: 1 while the marked tile moves instead of a unit</summary>
			public ushort MarkedTileMode { get => this.markedTileMode; set => Keep(ref this.markedTileMode, 0x4, value); }
			private ushort humanTookKey;
			/// <summary>0x6: a unit of the human took a key in this turn</summary>
			public ushort HumanTookKey { get => this.humanTookKey; set => Keep(ref this.humanTookKey, 0x6, value); }
			private ushort laterPass;
			/// <summary>0x8: not the first pass over the units</summary>
			public ushort LaterPass { get => this.laterPass; set => Keep(ref this.laterPass, 0x8, value); }
			private ushort aITries;
			/// <summary>0xa: the orders asked from ShiftAIUnit for the unit</summary>
			public ushort AITries { get => this.aITries; set => Keep(ref this.aITries, 0xa, value); }
			private ushort clicked;
			/// <summary>0xc: a click is waiting</summary>
			public ushort Clicked { get => this.clicked; set => Keep(ref this.clicked, 0xc, value); }
			private ushort cursorHoriz;
			/// <summary>0xe: the column of the marked tile or of the click (-1 none)</summary>
			public ushort CursorHoriz { get => this.cursorHoriz; set => Keep(ref this.cursorHoriz, 0xe, value); }
			private ushort city;
			/// <summary>0x10: a city</summary>
			public ushort City { get => this.city; set => Keep(ref this.city, 0x10, value); }

			private ushort cursorVert;
			/// <summary>0x12: the row of the marked tile or of the click</summary>
			public ushort CursorVert
			{
				get => this.cursorVert;
				set { this.cursorVert = value; this.processor?.EmitLocalWord(0x12, value); }
			}

			private ushort cargo;
			/// <summary>0x14: the cargo unit taken along</summary>
			public ushort Cargo { get => this.cargo; set => Keep(ref this.cargo, 0x14, value); }
			private ushort otherUnit;
			/// <summary>0x18: another unit (at the tile, the best guardian), or a flag</summary>
			public ushort OtherUnit { get => this.otherUnit; set => Keep(ref this.otherUnit, 0x18, value); }
			private ushort targetHoriz;
			/// <summary>0x1c: the column of the target tile</summary>
			public ushort TargetHoriz { get => this.targetHoriz; set => Keep(ref this.targetHoriz, 0x1c, value); }
			private ushort allMoved;
			/// <summary>0x1e: 1 unless a unit is left with moves in this pass</summary>
			public ushort AllMoved { get => this.allMoved; set => Keep(ref this.allMoved, 0x1e, value); }
			private ushort humanMarkers;
			/// <summary>0x20: the players played by humans at the start of the turn</summary>
			public ushort HumanMarkers { get => this.humanMarkers; set => Keep(ref this.humanMarkers, 0x20, value); }
			private ushort carrierHoriz;
			/// <summary>0x22: the column of the carrier before the move</summary>
			public ushort CarrierHoriz { get => this.carrierHoriz; set => Keep(ref this.carrierHoriz, 0x22, value); }

			private ushort direction;
			/// <summary>0x24: the direction of the move</summary>
			public ushort Direction
			{
				get => this.direction;
				set { this.direction = value; this.processor?.EmitLocalWord(0x24, value); }
			}

			private ushort targetVert;
			/// <summary>0x26: the row of the target tile</summary>
			public ushort TargetVert { get => this.targetVert; set => Keep(ref this.targetVert, 0x26, value); }
			private ushort ordered;
			/// <summary>0x28: the unit got an order</summary>
			public ushort Ordered { get => this.ordered; set => Keep(ref this.ordered, 0x28, value); }
			private ushort carrierVert;
			/// <summary>0x2a: the row of the carrier before the move</summary>
			public ushort CarrierVert { get => this.carrierVert; set => Keep(ref this.carrierVert, 0x2a, value); }
			private ushort unitHoriz;
			/// <summary>0x2c: the column of the unit</summary>
			public ushort UnitHoriz { get => this.unitHoriz; set => Keep(ref this.unitHoriz, 0x2c, value); }
			private ushort pastWaiting;
			/// <summary>0x2e: the pass went past the waiting unit 0x46</summary>
			public ushort PastWaiting { get => this.pastWaiting; set => Keep(ref this.pastWaiting, 0x2e, value); }
			private ushort unitVert;
			/// <summary>0x30: the row of the unit</summary>
			public ushort UnitVert { get => this.unitVert; set => Keep(ref this.unitVert, 0x30, value); }
			private ushort troop;
			/// <summary>0x32: the unit (0x80 none)</summary>
			public ushort Troop { get => this.troop; set => Keep(ref this.troop, 0x32, value); }
			private ushort startTerrain;
			/// <summary>0x34: the terrain the unit stands on</summary>
			public ushort StartTerrain { get => this.startTerrain; set => Keep(ref this.startTerrain, 0x34, value); }

			private ushort mouseCursorSet;
			/// <summary>0x36: the mouse cursor was changed</summary>
			public ushort MouseCursorSet
			{
				get => this.mouseCursorSet;
				set { this.mouseCursorSet = value; this.processor?.EmitLocalWord(0x36, value); }
			}

			private ushort found;
			/// <summary>0x38: something was found</summary>
			public ushort Found { get => this.found; set => Keep(ref this.found, 0x38, value); }
			private ushort result;
			/// <summary>0x3a: a result used for many things (a city, a battle, a dialog answer, a rate)</summary>
			public ushort Result { get => this.result; set => Keep(ref this.result, 0x3a, value); }
			private ushort count;
			/// <summary>0x3c: a counter or a city</summary>
			public ushort Count { get => this.count; set => Keep(ref this.count, 0x3c, value); }

			private ushort targetTerrain;
			/// <summary>0x3e: the terrain of the target tile</summary>
			public ushort TargetTerrain
			{
				get => this.targetTerrain;
				set { this.targetTerrain = value; this.processor?.EmitLocalWord(0x3e, value); }
			}

			private ushort targetImprovements;
			/// <summary>0x40: the improvements of the target tile (low byte)</summary>
			public ushort TargetImprovements { get => this.targetImprovements; set => Keep(ref this.targetImprovements, 0x40, value); }
			private ushort order;
			/// <summary>0x44: the order (a key)</summary>
			public ushort Order { get => this.order; set => Keep(ref this.order, 0x44, value); }
			private ushort waitingTroop;
			/// <summary>0x46: the unit that waits for the next pass</summary>
			public ushort WaitingTroop { get => this.waitingTroop; set => Keep(ref this.waitingTroop, 0x46, value); }
			private ushort tileOwner;
			/// <summary>0x48: the owner of the target tile</summary>
			public ushort TileOwner { get => this.tileOwner; set => Keep(ref this.tileOwner, 0x48, value); }
			private ushort cargoTally;
			/// <summary>0x4a: the cargo units counted</summary>
			public ushort CargoTally { get => this.cargoTally; set => Keep(ref this.cargoTally, 0x4a, value); }
			private ushort nextCargo;
			/// <summary>0x4c: the next cargo unit</summary>
			public ushort NextCargo { get => this.nextCargo; set => Keep(ref this.nextCargo, 0x4c, value); }
			private ushort unitDetail;
			/// <summary>0x4e: the row, status or kind of the unit</summary>
			public ushort UnitDetail { get => this.unitDetail; set => Keep(ref this.unitDetail, 0x4e, value); }
			private ushort carrierKind;
			/// <summary>0x52: the kind of the carrier (low byte)</summary>
			public ushort CarrierKind { get => this.carrierKind; set => Keep(ref this.carrierKind, 0x52, value); }

			/// <summary>
			/// The locals as the frame of the original starts them: the words left on the stack. Some are read before being
			/// written (0x12 and 0x24 logged by the calls of a unit that has not moved, 0x3e for a fortress of settlers).
			/// </summary>
			public static TurnLocals FromFrame(CPU processor) => new TurnLocals(processor)
			{
				dot = processor.PullLocalWord(0x2), markedTileMode = processor.PullLocalWord(0x4),
				humanTookKey = processor.PullLocalWord(0x6), laterPass = processor.PullLocalWord(0x8),
				aITries = processor.PullLocalWord(0xa), clicked = processor.PullLocalWord(0xc),
				cursorHoriz = processor.PullLocalWord(0xe), city = processor.PullLocalWord(0x10),
				cursorVert = processor.PullLocalWord(0x12), cargo = processor.PullLocalWord(0x14),
				otherUnit = processor.PullLocalWord(0x18), targetHoriz = processor.PullLocalWord(0x1c),
				allMoved = processor.PullLocalWord(0x1e), humanMarkers = processor.PullLocalWord(0x20),
				carrierHoriz = processor.PullLocalWord(0x22), direction = processor.PullLocalWord(0x24),
				targetVert = processor.PullLocalWord(0x26), ordered = processor.PullLocalWord(0x28),
				carrierVert = processor.PullLocalWord(0x2a), unitHoriz = processor.PullLocalWord(0x2c),
				pastWaiting = processor.PullLocalWord(0x2e), unitVert = processor.PullLocalWord(0x30),
				troop = processor.PullLocalWord(0x32), startTerrain = processor.PullLocalWord(0x34),
				mouseCursorSet = processor.PullLocalWord(0x36), found = processor.PullLocalWord(0x38),
				result = processor.PullLocalWord(0x3a), count = processor.PullLocalWord(0x3c),
				targetTerrain = processor.PullLocalWord(0x3e), targetImprovements = processor.PullLocalWord(0x40),
				order = processor.PullLocalWord(0x44), waitingTroop = processor.PullLocalWord(0x46),
				tileOwner = processor.PullLocalWord(0x48), cargoTally = processor.PullLocalWord(0x4a),
				nextCargo = processor.PullLocalWord(0x4c), unitDetail = processor.PullLocalWord(0x4e),
				carrierKind = processor.PullLocalWord(0x52),
			};
		}

		/// <summary>The locals of the running HandlePlayerTurn</summary>
		private TurnLocals turn = new TurnLocals();

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

			this.processor.EnterFrame(0x52);
			TurnLocals outer = this.turn;
			this.turn = TurnLocals.FromFrame(this.processor);
			this.processor.PreserveIndexRegisters();

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

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
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
			if (this.processor.PullGlobalWord(0x1ae0) != 0)
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
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

			this.processor.RegAcc.Value16 = 0;
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

			this.processor.RegAcc.Value16 = this.turn.Order;

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
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			TurnPlayer = (ushort)participantIdx;
			this.turn.HumanMarkers = (ushort)this.owner.World.ParticipantMarkers;
			this.turn.CursorHoriz = 0xffff;
			this.turn.MarkedTileMode = 0;

			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				this.owner.World.ParticipantMarkers = (short)(1 << (person & 0xff));

				if (this.owner.World.RoundTally == 20 || this.owner.World.RoundTally == 60)
					this.owner.HelpSystem.DisplayHelpPopup(0x1e60);

				if (this.owner.World.RoundTally == 40 || this.owner.World.RoundTally == 80)
					this.owner.HelpSystem.DisplayHelpPopup(0x1e67);
			}

			for (this.turn.Troop = 0; (short)this.turn.Troop < UnitMemory.TroopTally;
				this.turn.Troop = (ushort)(this.turn.Troop + 1))
			{
				short troopIdx = (short)this.turn.Troop;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff || this.processor.PullGlobalWord(0xdf60) == 1)
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

			this.processor.RegAcc.Value16 = 0;
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
			this.processor.RegSource.Value16 = player;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
				return false;

			if (this.troops.TroopKind(participantIdx, troopIdx) >= 0x1c)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 1);

			if (this.turn.LaterPass == 0)
			{
				// (ShiftUnit keeps SI on the stack, where ShiftAIUnit later finds it)
				this.processor.RegSource.Value16 = unit;
				this.owner.UnitOps.ShiftUnit(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), participantIdx, troopIdx, 0);
			}

			this.processor.RegSource.Value16 = player;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 9) != 0 ||
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
			{
				return false;
			}

			this.processor.RegSource.Value16 = unit;

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
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0)
				return true;

			// On the map view (the unsigned comparisons of the original)
			ushort horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.processor.RegDest.Value16 = horizLoc;
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

			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
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
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			}

			this.turn.AITries = (ushort)(this.turn.AITries + 1);

			if ((short)this.turn.AITries > 4)
			{
				this.turn.Order = 0x20;
				this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
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
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

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

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				this.turn.Order = 0x68;

			return true;
		}

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
			short person = this.owner.World.PersonParticipantIdx;

			RenderUnitInfo(participantIdx, this.turn.Troop);

			if (this.owner.World.RoundTally == 0 && this.turn.LaterPass == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);
				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8),
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

				this.owner.HelpSystem.DisplayHelpPopup(0x1e6e);

				this.turn.LaterPass = 1;
			}

			if (this.turn.Troop < UnitMemory.TroopTally)
			{
				short troopIdx = (short)this.turn.Troop;
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0 && this.turn.LaterPass == 0)
				{
					this.owner.HelpSystem.DisplayTileInfo(participantIdx, this.turn.Troop);
					this.turn.LaterPass = 1;
				}
			}

			this.owner.UnitOps.ChoreographUnitMove(participantIdx,
				(short)this.turn.Troop,
				this.turn.CursorHoriz,
				this.turn.CursorVert);

			if (this.turn.Troop != 0x80)
				this.turn.HumanTookKey = 1;

			if (this.owner.GlobDb3a == 0)
			{
				this.turn.Order = this.owner.Dialogs.ObtainNavigationKey();

				if (this.turn.Order != 0xd || this.turn.MarkedTileMode == 0)
					return HumanOrder.Order;

				// Enter on the marked tile: a click there
				this.processor.RegAcc.Value16 = this.turn.CursorHoriz;
				this.turn.TargetHoriz = this.turn.CursorHoriz;
				this.turn.TargetVert = this.turn.CursorVert;
				this.owner.GlobDb3c = 0x50;
				this.owner.GlobDb3e = 0x8;
				this.owner.GlobDb3a = 0x1;

				return TakeClick(participantIdx);
			}

			LocateClickedTile();

			if ((short)this.owner.GlobDb3e < 8)
			{
				// The menu bar
				this.owner.MenuSystem.ManageMenuBar(participantIdx, (short)this.turn.Troop, -1);

				AwaitForKey();

				if (ChosenOrder == 0xffff)
					return HumanOrder.NextLook;

				this.turn.Order = ChosenOrder;

				return (short)this.turn.Troop < 0x80 ? HumanOrder.MenuOrder : HumanOrder.MenuWithoutUnit;
			}

			if ((short)this.owner.GlobDb3c < 0x50)
			{
				if ((short)this.owner.GlobDb3e < 0x3a)
				{
					// The world map: the view moves there
					this.owner.MapOps.FocusMapView(participantIdx,
						(short)(ushort)this.owner.Routing.TunePosition(
						(short)this.owner.GlobDb3c + this.processor.PullGlobalSignedWord(0x6ed6) - 7),
						this.owner.Helpers.VerifyValueSpan((short)this.owner.GlobDb3e + this.processor.PullGlobalSignedWord(0x70ea) - 14, 0, 49));
				}
				else if ((short)this.owner.GlobDb3e < 0x48)
				{
					ShowPalace();
				}
				else if (this.turn.Troop == 0x80)
				{
					return HumanOrder.EndOfTurn;
				}

				this.owner.GlobDb3a = 0;
			}

			if (this.owner.GlobDb3a == 2 && (short)this.turn.Troop < 0x80)
				TakeHeldClick(participantIdx);

			if (this.owner.GlobDb3a == 2 &&
				(((sbyte)this.owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & (1 << (person & 0xff))) != 0 ||
					MapRevealed != 0))
			{
				// The Civilopedia of the terrain of a seen tile
				this.owner.Startup.ConcealMouseCursor();

				this.owner.Encyclopedia.DisplayCivilopediaRecord(this.owner.MapOps.ObtainTerrainType(
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert), 3);

				this.owner.TurnHooks.RepaintMainScreen();

				this.owner.Startup.DisplayMouseCursor();
			}

			return TakeClick(participantIdx);
		}

		/// <summary>The tile clicked on the map view: 0x1c / 0x26</summary>
		private void LocateClickedTile()
		{
			this.turn.TargetHoriz = (ushort)this.owner.Routing.TunePosition(
				(((short)this.owner.GlobDb3c - 80) / 16) + (short)MapViewHoriz);
			this.processor.RegAcc.Value16 = (ushort)(this.owner.GlobDb3e - 8);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 + MapViewVert);
			this.turn.TargetVert = this.processor.RegAcc.Value16;
		}

		/// <summary>The palace of the human player, until a key or a click</summary>
		private void ShowPalace()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Throne.ImportPalaceGraphics(0);

			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Helpers.HaltHueCycling();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1e79, 1);

			this.owner.VideoCard.RenderImage(
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Helpers.LaunchHueCycling();
		}

		/// <summary>
		/// A click held on the map (GlobDb3a 2): when the button is let go on a tile next to the unit 0x32 (also across the date
		/// line, 0x18) it is the destination of the unit (GlobDb3a 0, the normal pointer), else the click stays held (2).
		/// </summary>
		private void TakeHeldClick(short participantIdx)
		{
			do
			{
				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.GlobDb3a != 0);

			LocateClickedTile();

			short troopIdx = (short)this.turn.Troop;
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			this.turn.OtherUnit = (ushort)this.owner.Helpers.MergeCoordinates(
				(short)((short)this.turn.TargetHoriz - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz)),
				(short)((short)this.turn.TargetVert - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)));

			// Across the date line
			if (this.owner.Libc.AbsoluteValue((short)(this.turn.TargetHoriz - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz))) == 0x4f &&
				this.owner.Libc.AbsoluteValue((short)(this.turn.TargetVert - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))) <= 1)
			{
				this.turn.OtherUnit = 1;
			}

			if (this.turn.OtherUnit != 1)
			{
				this.owner.GlobDb3a = 2;
				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)this.turn.TargetHoriz);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)this.turn.TargetVert);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.owner.GlobDb3a = 0;
			this.turn.MouseCursorSet = 0;

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));
		}

		/// <summary>
		/// A click (GlobDb3a 1) on the tile 0x1c / 0x26, see TakeHumanOrder; then 0x44 is -1 and the keys wait unless 0xc.
		/// </summary>
		private HumanOrder TakeClick(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			if (this.owner.GlobDb3a == 1)
			{
				if (this.turn.MouseCursorSet != 0 && (short)this.turn.Troop < 0x80)
				{
					// The GoTo pointer: a destination the unit may reach (any for 2)
					short troopIdx = (short)this.turn.Troop;
					this.turn.TargetTerrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
					bool ocean = this.turn.TargetTerrain == MapRules.OceanTerrain;
					bool reachable;

					if (this.turn.MouseCursorSet == 2)
						reachable = true;
					else if (((sbyte)this.owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & (1 << (person & 0xff))) == 0)
						reachable = false;
					else
					{
						ushort group = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);

						// A ship on the sea, a land unit on land, any unit into a city, a plane anywhere
						reachable = (group == UnitKindTable.SeaUnits && ocean) ||
							(group == UnitKindTable.LandUnits && !ocean) ||
							(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 ||
							this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;
					}

					if (reachable)
					{
						this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)this.turn.TargetHoriz);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)this.turn.TargetVert);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
					}

					this.turn.MouseCursorSet = 0;

					this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

					ForgetMarkedTile(participantIdx);
				}
				else if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 &&
					(this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) == (ushort)person || MapRevealed != 0))
				{
					// A city of the human (any with everything shown): the city screen
					this.turn.TileOwner = this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc);
					this.turn.Count = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);

					this.owner.Advisors.RenewCityScreen((short)this.turn.Count);

					this.owner.TurnHooks.RenderStatusBar();

					this.turn.AllMoved = 0;
					ForgetMarkedTile(participantIdx);
				}
				else
				{
					this.turn.Order = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

					if (MapRevealed != 0 && this.turn.Order != 0xffff)
					{
						this.owner.ArmyPicker.DisplayUnitInfo(
							(short)FoundPlayer,
							(short)this.turn.Order);
					}

					if (this.turn.Order != 0xffff && participantIdx == (short)FoundPlayer)
					{
						// An own unit (from its stack): it is the unit now, unless sentried or fortified
						this.turn.Result = this.owner.UnitOps.DisplayBattleScreen((short)FoundPlayer,
							(short)this.turn.Order);

						short chosen = (short)this.turn.Result;
						this.processor.RegBase.Value16 = (ushort)(chosen * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

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
						this.owner.MapOps.FocusMapView(participantIdx,
							(short)this.turn.TargetHoriz - 7,
							(short)this.turn.TargetVert - 6);
					}
				}
			}

			this.turn.Order = 0xffff;

			if (this.turn.Clicked != 0)
				this.turn.Clicked = 0;
			else
				AwaitForKey();

			return HumanOrder.Order;
		}

		/// <summary>The marked tile (0x4, at 0xe / 0x12) is forgotten and repainted when the player sees it</summary>
		private void ForgetMarkedTile(short participantIdx)
		{
			if (this.turn.MarkedTileMode == 0)
				return;

			this.turn.MarkedTileMode = 0;

			if (((sbyte)this.owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (byte)participantIdx)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
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
				this.turn.Order = this.owner.Routing.CarryOutGoto(participantIdx, troopIdx);
			}
			else
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + this.turn.Troop * UnitMemory.TroopStride);

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

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);
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

			bool may = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			if (may)
			{
				this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));
				may = (sbyte)(byte)this.turn.UnitDetail < 0x1a && (byte)this.turn.UnitDetail != 0xe;
			}

			if (!may)
			{
				DisplayHelpForHuman(0x1e83);
				return;
			}

			this.turn.City = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
				return;

			// (no when the human keeps the treaty)
			if (this.owner.Combat.AffirmBreakTreaty(this.owner.World.Towns[this.turn.City].ParticipantIdx) == 0xffff)
				return;

			this.turn.Result = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			if (((byte)this.turn.Result & 6) != 0)
				this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
			else if (((byte)this.turn.Result & 0x10) != 0)
				this.owner.MapOps.DiscardTileImprovements(0x10, horizLoc, vertLoc);
			else
				this.owner.MapOps.DiscardTileImprovements(8, horizLoc, vertLoc);

			if (this.owner.World.Towns[this.turn.City].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

			RepaintTileIfVisible(this.turn.UnitHoriz, this.turn.UnitVert);

			this.processor.RegSource.Value16 = (ushort)(this.turn.City * 0x1c);

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx,
				this.owner.World.Towns[this.turn.City].ParticipantIdx,
				2);

			// (the player of the strategic location is DI as the original leaves it)
			if (participantIdx != this.owner.World.Towns[this.turn.City].ParticipantIdx)
				this.owner.Bot.SendUnitToStrategicLocation((short)this.processor.RegDest.Value16, horizLoc, vertLoc, 1, 4);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>Space: the unit does nothing this turn</summary>
		private void SkipUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>'s': the unit is sentried</summary>
		private void SentryUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);
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

			if (UnitKindTable.Field(this.processor, (sbyte)(byte)this.turn.UnitDetail, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.UnitDetail != UnitKindTable.Carrier)
			{
				return;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return;

			this.owner.UnitOps.VisitEachUnitInStack(participantIdx, troopIdx, 0x14f6);

			this.turn.AllMoved = 0;
			this.turn.Ordered = 1;

			this.turn.Troop = (ushort)(LocateBestGuardian(participantIdx, this.turn.Troop) - 1);
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
					this.processor.RegSource.Value16 = (ushort)(participantIdx * 0x600 + this.turn.Troop * 0xc);
					this.turn.CursorHoriz = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
					this.turn.CursorVert = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
				}
			}
			else if (this.turn.MarkedTileMode != 0)
			{
				// GoTo cancelled, remove the square cursor
				this.turn.MarkedTileMode = 0x0;

				if ((this.owner.World.ChartSight[this.turn.CursorHoriz,
					this.turn.CursorVert] & (1 << participantIdx)) != 0)
				{
					this.owner.MapOps.RepaintMapTile(
						(short)this.turn.CursorHoriz,
						(short)this.turn.CursorVert);
				}

				this.turn.CursorHoriz = 0xffff;
			}

			if (MousePresent != 0)
			{
				// GoTo mouse pointer, or the normal one when GoTo is cancelled
				this.processor.RegBase.Value16 = (ushort)((this.turn.MouseCursorSet != 0 ? 7 : 2) << 1);
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4ce)));
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

			this.turn.City = this.owner.Helpers.LocateNearestCity(
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			if (FoundDistance == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)this.turn.City);

				return false;
			}

			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
				return false;

			this.turn.Direction = this.owner.UnitOps.LocateNearestFriendlyCity(participantIdx, (short)this.turn.Troop);

			if (this.turn.Direction != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
				return true;
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));

				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);

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
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1e8c);
				return;
			}

			this.turn.TargetImprovements = this.owner.MapOps.ObtainTileImprovements(
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			this.processor.RegSource.Value16 = unit;

			if (((byte)this.turn.TargetImprovements & 0x40) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x7d));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx,
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
			this.processor.RegAcc.LowByte = 0xff;

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) < 4)
				return;

			this.owner.MapOps.DiscardTileImprovements(0x40,
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			this.owner.MapOps.KeepKnownTileImprovements(
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			owner.World.DirtyCellTally--;
			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.processor.RegSource.Value16 = unit;
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
			this.processor.RegSource.Value16 = unit;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 4));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

				if (participantIdx != this.owner.World.PersonParticipantIdx && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				{
					this.processor.RegCount.Value16 = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.RegCount.LowByte);
				}

				return;
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x21) != 0 ||
				(this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d)) == 0)
			{
				this.processor.RegSource.Value16 = unit;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				return;
			}

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0xc0));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.processor.RegBase.Value16 = (ushort)(this.turn.TargetTerrain * 0x13);
			short work = (short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)) + 4);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegCount.Value16 = (ushort)work;

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.owner.MapOps.AssignTileImprovements(0x20, horizLoc, vertLoc);

			this.owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, (short)this.turn.Troop, 1);

			this.processor.RegSource.Value16 = unit;
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
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

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
				DisplayHelpForHuman(0x1e96);
				return;
			}

			this.turn.TargetImprovements = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);
			byte improvements = (byte)this.turn.TargetImprovements;
			bool refused;

			if ((improvements & 8) != 0)
			{
				// A railroad on the road: with Railroad, not in a city
				refused = (this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31)) == 0 ||
					(improvements & 1) != 0 || (improvements & 0x10) != 0;
			}
			else
			{
				refused = (improvements & 0x10) != 0;
			}

			if (refused)
			{
				this.processor.RegSource.Value16 = unit;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				return;
			}

			this.turn.TargetTerrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (this.turn.TargetTerrain == 0xb &&
				(this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13)) == 0)
			{
				return;
			}

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			short factor = (short)(((byte)this.turn.TargetImprovements & 8) != 0 ? 4 : 2);
			short worth = TerrainWorkByte(0x28e);
			this.processor.RegBase.Value16 = (ushort)worth;
			short work = (short)(factor * worth);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegCount.Value16 = (ushort)work;

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.owner.MapOps.AssignTileImprovements(
				(ushort)((((byte)this.turn.TargetImprovements & 8) != 0) ? 0x10 : 8),
				horizLoc, vertLoc);

			this.owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, (short)this.turn.Troop, 1);

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>A signed byte of the terrain 0x3e in the table of terrains (0x13 bytes each); BX as the original leaves it</summary>
		private short TerrainWorkByte(ushort offset)
		{
			this.processor.RegBase.Value16 = (ushort)(this.turn.TargetTerrain * 0x13);

			return (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + offset));
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

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 || (this.owner.World.DiagMarkers & 2) == 0 ||
				(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 2) != 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
					DisplayHelpForHuman(0x1ea0);
				else
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			this.turn.TargetTerrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.processor.RegBase.Value16 = (ushort)(this.turn.TargetTerrain * 0xc);
			this.turn.Result = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a));

			if (this.turn.Result == 0xffff)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1eaa);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (this.turn.Result == 0xfffe && VerifyCoastalTile(horizLoc, vertLoc) == 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1eb1);
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
				this.owner.MapOps.DiscardTileImprovements(4, horizLoc, vertLoc);
				this.owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);
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
				(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 4) != 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
					DisplayHelpForHuman(0x1eba);
				else
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			this.turn.TargetTerrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.processor.RegBase.Value16 = (ushort)(this.turn.TargetTerrain * 0xc);
			this.turn.Result = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e));

			if (this.turn.Result == 0xffff)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1ec4);
				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x80, 0x450))
				return;

			if ((short)this.turn.Result >= 0)
			{
				// (a second step of the counter, the change only over 5)
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) <= 5)
					return;

				ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.owner.MapOps.DiscardTileImprovements(2, horizLoc, vertLoc);
				this.owner.MapOps.AssignTileImprovements(4, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, unit, 0x7f);
		}

		/// <summary>The work of the settlers stops: their status loses the bit (mask), BX / SI as the original leaves them</summary>
		private void StopWork(short participantIdx, short troopIdx, byte mask)
		{
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
		}

		/// <summary>
		/// A turn of work of the settlers (the status bit, no moves, no destination, the counter one up): true when the counter
		/// reaches the word of the terrain 0x3e at the offset (12 bytes a terrain). The locals of HandlePlayerTurn are used as
		/// in the original.
		/// </summary>
		private bool DoTileWork(short participantIdx, short troopIdx, ushort unit, byte bit, ushort offset)
		{
			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | bit));

			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.processor.RegAcc.LowByte = 0xff;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.processor.RegBase.Value16 = (ushort)(this.turn.TargetTerrain * 0xc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			return (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + offset)) <=
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps);
		}

		/// <summary>The terrain of the tile becomes 0x3a (seen by the human player), irrigation and mine go</summary>
		private void ChangeTerrain(short horizLoc, short vertLoc)
		{
			this.owner.Services.AssignDot(2, horizLoc, vertLoc, this.turn.Result);
			this.owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] |= 1;
			this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
		}

		/// <summary>The work is done: the unit is shown, its status loses the bit (mask) and the counter is 0</summary>
		private void FinishTileWork(short participantIdx, short troopIdx, ushort unit, byte mask)
		{
			RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

			this.processor.RegSource.Value16 = unit;
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
				case 0x20:
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
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1ecc);
				return;
			}

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain || vertLoc < 2 || vertLoc >= 0x30)
				return;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				// The settlers join the city
				this.turn.City = this.owner.MapOps.LocateCityAtTile(horizLoc, vertLoc);

				if ((sbyte)this.owner.World.Towns[this.turn.City].TrueExtent >= 0xa)
				{
					DisplayHelpForHuman(0x1ed6);
					return;
				}

				this.owner.World.Towns[this.turn.City].TrueExtent++;
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.ScaleIndex(0x600, (ushort)participantIdx);
				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);

				RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);

				return;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.turn.Result = this.owner.Founding.EstablishCity(participantIdx,
				this.turn.Direction,
				horizLoc, vertLoc,
				1);

			if (this.turn.Result != 0xffff)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);

				RepaintUnitIfVisible(participantIdx, (short)this.turn.Troop);
			}
		}

		/// <summary>
		/// The direction (1 - 8) of a move order: the numbers 1 - 8 (GoTo and the computer), Home / Up / PgUp / Right / PgDn /
		/// Down / End / Left; 0 for other keys (the signed comparisons of the original: no direction from 0x8000 on)
		/// </summary>
		private static ushort KeyDirection(ushort key)
		{
			return (short)key switch
			{
				>= 1 and <= 8 => key,
				0x4700 => 8,
				0x4800 => 1,
				0x4900 => 2,
				0x4d00 => 3,
				0x5100 => 4,
				0x5000 => 5,
				0x4f00 => 6,
				0x4b00 => 7,
				_ => 0,
			};
		}

		/// <summary>'c': the view centers on the marked tile (0x4, at 0xe / 0x12) or on the unit (0x2c / 0x30)</summary>
		private void CenterView(short participantIdx)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					(short)this.turn.CursorHoriz - 7,
					(short)this.turn.CursorVert - 6);
			}
			else
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					(short)this.turn.UnitHoriz - 7,
					(short)this.turn.UnitVert - 6);
			}
		}

		/// <summary>
		/// The marked tile (0xe / 0x12) moves one step in the direction 0x24 (repainted where it was when the player sees it,
		/// kept off the poles); off the map view the view moves. SI as the original leaves it.
		/// </summary>
		private void MoveMarkedTile(short participantIdx)
		{
			if (((sbyte)this.owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (byte)participantIdx)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					(short)this.turn.CursorHoriz,
					(short)this.turn.CursorVert);
			}

			this.processor.RegSource.Value16 = (ushort)(this.turn.Direction << 1);

			this.turn.CursorHoriz = (ushort)this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					(short)this.turn.CursorHoriz);

			this.turn.CursorVert = (ushort)this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					(short)this.turn.CursorVert,
				0, 49);

			if (VerifyPositionOnScreen((short)this.turn.CursorHoriz, (short)this.turn.CursorVert) == 0)
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					(short)this.turn.CursorHoriz - 8,
					(short)this.turn.CursorVert - 7);
			}
		}

		/// <summary>
		/// A barbarian land unit (not a diplomat, legion or knights) leaving an irrigated or mined tile outside a city nearest
		/// to a city of the human pillages it and has no moves left (true). The locals of HandlePlayerTurn are used as in the
		/// original.
		/// </summary>
		private bool BarbarianPillages(short participantIdx)
		{
			if (participantIdx != 0)
				return false;

			short troopIdx = (short)this.turn.Troop;
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.Kind);
			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kind == UnitKindTable.Diplomat)
				return false;

			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;
			this.turn.TargetImprovements = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			if (((byte)this.turn.TargetImprovements & 1) != 0 || ((byte)this.turn.TargetImprovements & 6) == 0 || kind == 3 || kind == 7)
				return false;

			if (this.owner.World.Towns[this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				return false;

			this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);

			if (((sbyte)this.owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] & (1 << (this.owner.World.PersonParticipantIdx & 0xff))) != 0)
			{
				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = 0;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		/// <summary>
		/// The tile the unit 0x32 moves to in the direction 0x24 (0x1c / 0x26, off the poles): the terrain where it stands (0x34)
		/// and there (0x3e), the improvements there (0x40), the unit there (0x18); true for a land unit. The locals of
		/// HandlePlayerTurn are used as in the original, BX / DI as it leaves them.
		/// </summary>
		private bool LocateMoveTarget(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.processor.RegSource.Value16 = (ushort)(this.turn.Direction << 1);

			this.turn.TargetHoriz = (ushort)this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					(short)this.turn.UnitHoriz);

			this.turn.TargetVert = (ushort)this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					(short)this.turn.UnitVert,
				0, 49);

			this.turn.StartTerrain = this.owner.MapOps.ObtainTerrainType((short)this.turn.UnitHoriz, (short)this.turn.UnitVert);
			this.turn.TargetTerrain = this.owner.MapOps.ObtainTerrainType((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			this.turn.TargetImprovements = this.owner.MapOps.ObtainTileImprovements((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			this.turn.OtherUnit = this.owner.MapOps.ObtainUnitAtTile((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;
		}

		/// <summary>
		/// Zone of control: a land unit (not a diplomat or caravan) on land goes from a tile next to a foreign unit to another
		/// free tile next to one: it stops (true; a beep for the human, the help message with that option, the destination
		/// forgotten). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool StoppedByZoneOfControl(short participantIdx)
		{
			if (this.turn.OtherUnit != 0xffff ||
				this.owner.UnitOps.MayOccupyTile(participantIdx, (short)this.turn.UnitHoriz, (short)this.turn.UnitVert) == 0 ||
				this.owner.UnitOps.MayOccupyTile(participantIdx, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert) == 0)
			{
				return false;
			}

			short troopIdx = (short)this.turn.Troop;
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || this.turn.StartTerrain == MapRules.OceanTerrain)
				return false;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				this.owner.Services.PerformSound(0x25, 0);

			if ((this.owner.World.SessionPrefMarkers & 1) != 0)
				DisplayHelpForHuman(0x1edf);

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			return true;
		}

		/// <summary>Where the move of the unit goes on after a step of HandlePlayerTurn</summary>
		private enum MoveStep
		{
			/// <summary>The move goes on (L219c)</summary>
			GoesOn,

			/// <summary>A diplomat or caravan where no city is (L2157)</summary>
			NoCity,

			/// <summary>The order is done (L3365)</summary>
			Done,
		}

		/// <summary>
		/// A diplomat or caravan moves to the tile 0x1c / 0x26: no city there (NoCity); not from the sea into a foreign city
		/// (Done). A diplomat in a foreign city acts there (Done; into a city of the human it is shown, with the view on it and
		/// its move). A caravan asks (in an own city: help to build a wonder; a trade route) unless the city is own, builds no
		/// wonder, is nearer than 10 to its home city and on the same continent; a trade route or the help to build ends the
		/// order (Done), else it moves on. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private MoveStep EnterCityAsDiplomatOrCaravan(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat)
				return MoveStep.GoesOn;

			this.turn.City = this.owner.Helpers.LocateCityAtPosition((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			if (this.turn.City == 0xffff)
				return MoveStep.NoCity;

			Game.State.City town = this.owner.World.Towns[this.turn.City];
			this.processor.RegBase.Value16 = (ushort)(this.turn.City * 0x1c);

			if (town.ParticipantIdx != participantIdx && this.turn.StartTerrain == MapRules.OceanTerrain)
				return MoveStep.Done;

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				this.processor.RegBase.Value16 = (ushort)(this.turn.City * 0x1c);
				this.processor.RegDest.Value16 = (ushort)town.ParticipantIdx;

				if (participantIdx != town.ParticipantIdx)
				{
					short person = this.owner.World.PersonParticipantIdx;

					if (person == town.ParticipantIdx)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));

						this.owner.UnitOps.PanMapToPosition(person,
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

						this.owner.TextPainter.AwaitTime(30);

						this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
							(short)this.turn.Troop,
							this.turn.Direction);
					}

					this.owner.Diplomats.DiplomatArrivesAtCity((short)this.turn.City,
						participantIdx,
						(short)this.turn.Troop);

					return MoveStep.Done;
				}
			}

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x1b)
				return MoveStep.GoesOn;

			// The caravan: its distance from its home city (0x3a)
			sbyte home = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown);
			this.processor.RegDest.Value16 = (ushort)(home * 0x1c);
			byte homeIdx = this.troops.Pull(participantIdx, troopIdx, UnitMemory.BaseTown);

			this.turn.Result = (ushort)this.owner.Helpers.ObtainDistance(
				(short)this.turn.TargetHoriz,
				this.owner.World.Towns[homeIdx].Place.X,
				(short)this.turn.TargetVert,
				this.owner.World.Towns[homeIdx].Place.Y);
			this.processor.RegSource.Value16 = (ushort)(this.turn.City * 0x1c);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx >= -24 && (short)this.turn.Result < 10)
			{
				// Near its home city, building no wonder: on the same continent it just moves in
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(home * 0x1c);

				this.processor.RegDest.Value16 = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[homeIdx].Place.X,
					this.owner.World.Towns[homeIdx].Place.Y);

				if (this.owner.MapOps.ObtainContinentID((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) == this.processor.RegDest.Value16)
					return MoveStep.GoesOn;
			}

			this.owner.Libc.CopyText(0xba06, "Will you?\n Keep moving\n Establish trade route\n");
			this.processor.RegSource.Value16 = (ushort)(this.turn.City * 0x1c);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx < -24)
			{
				this.owner.Libc.AppendText(0xba06, " Help build WONDER.\n");

				if ((short)this.turn.Result < 10)
					DisabledLines = 0x2;
			}

			this.processor.RegBase.Value16 = (ushort)(this.turn.City * 0x1c);

			if (town.ParticipantIdx == participantIdx)
			{
				this.turn.Result = this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
			else
			{
				this.turn.Result = 0x1;
			}

			if (this.turn.Result == 1 &&
				this.owner.Conquest.FoundTradeRoute(participantIdx, (short)this.turn.Troop, (short)this.turn.City) != 0)
			{
				return MoveStep.Done;
			}

			if (this.turn.Result != 2)
				return MoveStep.GoesOn;

			// The caravan helps to build the wonder: 10 times its price
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
			this.processor.RegCount.Value16 = (ushort)(10 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
			this.processor.RegBase.Value16 = (ushort)(this.turn.City * 0x1c);
			this.owner.World.Towns[this.turn.City].MaterialsTally += (short)this.processor.RegCount.Value16;

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);

			return MoveStep.Done;
		}

		/// <summary>Where the move goes on after CheckMoveTile</summary>
		private enum MoveCheck
		{
			/// <summary>The unit moves (L267b)</summary>
			Moves,

			/// <summary>No move, the order is done (L3365)</summary>
			Done,

			/// <summary>A foreign unit there is attacked (L2329)</summary>
			Attacks,
		}

		/// <summary>
		/// A diplomat next to a foreign unit where no city is bribes it (BuyUnit) and forgets its destination (written through
		/// SI as the original leaves it). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BribeUnitOnTheWay(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat || this.turn.OtherUnit == 0xffff ||
				participantIdx == (short)UnitAtTilePlayer)
			{
				return;
			}

			this.owner.Diplomats.BuyUnit((short)UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				participantIdx);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
		}

		/// <summary>
		/// The tile of the move: a land unit to the sea boards an own ship with room (sentried, 3 moves – written through SI as
		/// TallySpecialUnitsInStack leaves it, as the original does) or stays; a foreign unit there: no attack from a ship by a
		/// land unit (help), by a submarine on land, by a computer unit without attack, by a diplomat, on a plane outside a
		/// city except by a fighter (help); else it is attacked. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private MoveCheck CheckMoveTile(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short owner = (short)UnitAtTilePlayer;

			if (this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// A land unit boards an own ship with room
					if (participantIdx != (short)UnitAtTilePlayer || this.turn.OtherUnit == 0xffff ||
						(short)this.owner.UnitOps.TallySpecialUnitsInStack(participantIdx, (short)this.turn.OtherUnit) <= 0)
					{
						return MoveCheck.Done;
					}

					ushort status = (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Status);
					this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) | 1));
					this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) & 0xf3));
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.LeftoverSteps), 3);

					return MoveCheck.Moves;
				}
			}

			if (this.turn.OtherUnit == 0xffff || participantIdx == (short)UnitAtTilePlayer)
				return MoveCheck.Moves;

			if (this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// (a land unit on a ship does not attack)
					DisplayHelpForHuman(0x1f28);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

					return MoveCheck.Done;
				}
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Submarine)
				return MoveCheck.Done;

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower) == 0)
					return MoveCheck.Done;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
				return MoveCheck.Done;

			// A plane outside a city is attacked only by a fighter
			short guardian = (short)this.turn.OtherUnit;
			this.processor.RegDest.Value16 = (ushort)(guardian * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(owner, guardian) * UnitKindTable.Stride);

			if (this.troops.KindField(owner, guardian, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				this.processor.RegSource.Value16 = unit;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xe && ((byte)this.turn.TargetImprovements & 1) == 0)
				{
					DisplayHelpForHuman(0x1f30);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

					return MoveCheck.Done;
				}
			}

			return MoveCheck.Attacks;
		}

		/// <summary>
		/// An attack with fewer than 3 moves left: the human confirms it ("Attack at x/3 strength?"; cancelled, the destination
		/// is forgotten), a computer unit attacks only with 2 moves. False: no attack. The locals of HandlePlayerTurn are used
		/// as in the original (the unit through SI as it leaves it).
		/// </summary>
		private bool ConfirmAttack(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.processor.RegSource.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 3)
				return true;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Libc.CopyText(0xba06, "Attack at\n");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 10));
				this.owner.Libc.AppendText(0xba06, "/3 strength?\n Cancel\n Attack\n");
				if (this.owner.TurnHooks.DisplayDialog(0xba06, 100, 16) == 1)
					return true;

				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			return this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 2;
		}

		/// <summary>
		/// The attack on the best defender of the stack 0x18 of the player 0xd20a (the view moves to a unit of the human; 0x70d8
		/// while the human is in the battle): the attacker loses 3 moves (a bomber all). An attacked city (0x10) is noticed by
		/// its player (bit 4, not the human), shrinks (no walls, not from the sea, at the easiest level not a human city); a city
		/// of size 0 is destroyed (its player may be eliminated; the units there are shown to the human); the human sees the new
		/// size; a city tile without units of a computer player makes it rethink the production on the continent. True: the
		/// attacker is lost (L3c40). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool FightAt(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			this.processor.RegAcc.Value16 = (ushort)person;

			if (UnitAtTilePlayer == (ushort)person)
				this.owner.UnitOps.PanMapToPosition(person, horizLoc, vertLoc);

			this.turn.OtherUnit = this.owner.UnitOps.LocateBestGuardianInStack((short)UnitAtTilePlayer,
				(short)this.turn.OtherUnit);

			this.processor.RegAcc.Value16 = (ushort)person;

			if (participantIdx == person || UnitAtTilePlayer == (ushort)person)
				this.processor.EmitGlobalWord(0x70d8, 0x1);

			this.turn.Result = this.owner.Combat.SettleBattle(participantIdx,
				(short)this.turn.Troop,
				(short)UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				1);

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.processor.EmitGlobalWord(0x70d8, 0x0);

			if (this.turn.Result == 0xffff)
				return false;

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				return true;
			}

			// 3 moves less (no fewer than 0), a bomber has none left
			this.processor.RegSource.Value16 = (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			if (((byte)this.turn.TargetImprovements & 1) == 0)
			{
				if (((byte)this.turn.TargetImprovements & 0x20) == 0)
				{
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				}

				return false;
			}

			// The attacked city
			this.turn.City = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);
			Game.State.City town = this.owner.World.Towns[this.turn.City];
			this.processor.RegSource.Value16 = (ushort)(this.turn.City * 0x1c);

			if (town.ParticipantIdx != person)
				town.ConditionMarker |= 0x10;

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff && !town.Has(CityStructure.CityWalls) &&
				this.turn.StartTerrain != MapRules.OceanTerrain &&
				(this.owner.World.HardnessTier != 0 || UnitAtTilePlayer != (ushort)person))
			{
				town.TrueExtent--;
			}

			this.processor.RegBase.Value16 = (ushort)(this.turn.City * 0x1c);

			if (town.TrueExtent == 0)
			{
				// The city is destroyed
				this.owner.Advisors.FinalizeProduction(
					(short)this.turn.City,
					this.turn.TargetHoriz,
					this.turn.TargetVert);

				this.owner.NewGameWizard.EliminateCivilization((short)UnitAtTilePlayer, participantIdx);

				if (participantIdx == person)
				{
					this.turn.OtherUnit = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

					if (this.turn.OtherUnit != 0xffff)
					{
						short owner = (short)UnitAtTilePlayer;
						short left = (short)this.turn.OtherUnit;
						this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
						this.processor.RegSource.Value16 = (ushort)(left * UnitMemory.TroopStride);
						this.troops.Assign(owner, left, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(owner, left, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));
					}
				}
			}

			this.processor.RegAcc.Value16 = (ushort)person;

			if (participantIdx == person || UnitAtTilePlayer == (ushort)person || MapRevealed != 0)
			{
				// The human sees the new size
				this.processor.RegSource.Value16 = (ushort)(this.turn.City * 0x1c);
				this.processor.RegAcc.LowByte = (byte)town.TrueExtent;
				town.ShownExtent = (sbyte)this.processor.RegAcc.LowByte;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if (this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) == 0xffff && UnitAtTilePlayer != (ushort)person)
			{
				this.owner.Bot.RefreshCityProduction((short)UnitAtTilePlayer, this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc));
			}

			return false;
		}

		/// <summary>
		/// The cost of the move to 0x1c / 0x26: a ship does not go on land outside a city (the destination is forgotten); a
		/// plane or ship into a foreign city: a nuclear weapon explodes there, others are refused (help); without moves no move.
		/// A land unit pays nothing on a railroad (the human without GoTo), 1 on a road on both tiles, else 3 times the byte
		/// 0x28e of the terrain 0x3e – with fewer moves than full a die decides whether it may enter at all (no moves left);
		/// other units pay 3. False: no move (L3365). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool PayForMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short kindOffset = (short)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && ((byte)this.turn.TargetImprovements & 1) == 0)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)kindOffset;

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
					return false;
				}
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)kindOffset;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits && ((byte)this.turn.TargetImprovements & 1) != 0 &&
				this.owner.MapOps.ObtainTileOwnerBits((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != (ushort)participantIdx)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x19)
				{
					DisplayHelpForHuman(0x1f62);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					return false;
				}

				this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);

				this.owner.Combat.ExplodeNuclearWeapon(participantIdx,
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert);

				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
				return false;

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)kindOffset;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				short horizLoc = (short)this.turn.UnitHoriz;
				short vertLoc = (short)this.turn.UnitVert;

				if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & (byte)this.turn.TargetImprovements & 8) != 0 &&
					this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0)
				{
					// A road on both tiles: 1, a railroad where it stands nothing (for the human without GoTo)
					byte cost = 1;

					if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x10) != 0 && participantIdx == this.owner.World.PersonParticipantIdx)
					{
						this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
						this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

						if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
							cost = 0;
					}

					this.processor.RegAcc.LowByte = cost;
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - cost));
				}
				else
				{
					// The terrain: with fewer moves than full a die may stop the unit
					this.processor.RegSource.Value16 = (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps);
					this.processor.RegBase.Value16 = (ushort)(this.turn.TargetTerrain * 0x13);
					short terrainCost = (short)(3 * (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)));

					if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 3)
					{
						this.processor.RegAcc.Value16 = (ushort)terrainCost;
						this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));
						this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

						if ((short)this.processor.RegCount.Value16 > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps))
						{
							this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
							return false;
						}
					}

					this.processor.RegCount.Value16 = (ushort)terrainCost;
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - this.processor.RegCount.LowByte));
				}
			}
			else
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));
			}

			this.processor.RegSource.Value16 = (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		/// <summary>
		/// The moves of a computer unit: one that would turn back (the opposite of its last heading) in a stack of up to 2 units
		/// forgets its heading and destination and stops (written through SI as TallyUnitsByDomainInStack leaves it, as the
		/// original does); else the direction 0x24 becomes its heading. A computer land unit (or a plane with fuel) does not join
		/// an own stack outside a city of 4 units or more (2 or more without moves). False: no move (L3365). The locals of
		/// HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool ComputerMayMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				this.processor.RegSource.Value16 = unit;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHeading) != 0xff &&
					(ushort)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHeading) ^ 4) == this.turn.Direction &&
					(short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)this.turn.Troop, 2) <= 2)
				{
					this.processor.RegAcc.LowByte = 0xff;
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHeading), 0xff);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.LeftoverSteps), 0);

					return false;
				}

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = (byte)this.turn.Direction;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, (byte)this.turn.Direction);
			}

			if (this.turn.OtherUnit == 0xffff || participantIdx != (short)UnitAtTilePlayer || participantIdx == person ||
				((byte)this.turn.TargetImprovements & 1) != 0)
			{
				return true;
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
			this.processor.RegDest.Value16 = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);

			if (this.processor.RegDest.Value16 != UnitKindTable.LandUnits &&
				(this.processor.RegDest.Value16 != UnitKindTable.AirUnits || this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) == 0))
			{
				return true;
			}

			this.turn.Result = this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, (short)this.turn.OtherUnit, 2);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegAcc.Value16 = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0 ? 4 : 2);

			return (short)this.processor.RegAcc.Value16 > (short)this.turn.Result;
		}

		/// <summary>
		/// The territory of the tile of the move (0x2: the player on the map layer 2 at x + 80): a land unit (not a diplomat
		/// or caravan) going on land onto an improved tile of another player's territory, between the human and another player,
		/// where no own unit is: the human may break the treaty (the destination forgotten; kept: no move), a computer unit at
		/// peace with the human stops (no moves). On an improved tile of another territory the unit is shown to that player.
		/// False: no move (L3365). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool EnterTerritory(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			this.turn.Dot = this.owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);

			if (this.turn.Dot == 0 || (short)this.turn.Dot >= 8)
				return true;

			short owner = (short)this.turn.Dot;

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.turn.StartTerrain != MapRules.OceanTerrain &&
				MayTreatyStop(participantIdx, troopIdx, unit, owner, person))
			{
				if (participantIdx == person)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

					if (this.owner.Combat.AffirmBreakTreaty((short)this.turn.Dot) == 0xffff)
						return false;

					this.owner.Politics.WipeDiplomacyFlags(participantIdx, (short)this.turn.Dot, 2);
				}
				else if (this.turn.Dot == (ushort)person)
				{
					this.processor.RegSource.Value16 = (ushort)(owner << 4);
					this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);

					if ((this.owner.World.Participants[this.turn.Dot].Relations[participantIdx] & 2) != 0)
					{
						this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

						return false;
					}
				}
			}

			if (participantIdx != (short)this.turn.Dot && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0xe) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = (byte)(1 << ((byte)this.turn.Dot & 0x1f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));
			}

			return true;
		}

		/// <summary>
		/// The conditions of EnterTerritory after the terrains: a land unit (not a diplomat or caravan) onto an improved tile of
		/// another player's territory, between the human and another player, not onto a tile of its own player
		/// </summary>
		private bool MayTreatyStop(short participantIdx, short troopIdx, ushort unit, short owner, short person)
		{
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || ((byte)this.turn.TargetImprovements & 0xf) == 0 ||
				owner == participantIdx || (participantIdx != person && owner != person))
			{
				return false;
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.owner.MapOps.ObtainTileOwner((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != (ushort)participantIdx;
		}

		/// <summary>
		/// Who sees the move (0x48: the player of the tile): a barbarian on land the human sees, not shown to him yet, near a
		/// city of the human is shown to who sees its tile; a unit going into a foreign city the human sees is shown to him with
		/// an embassy with its player or the city's; on a sea tile with bit 1 it is shown to the player of the tile. A move
		/// shown to the human (or of the human, or of any computer player with everything shown) is animated with the view on it
		/// (the option 0x20, or the human's own), else its tile is drawn again. The locals of HandlePlayerTurn are used as in the
		/// original.
		/// </summary>
		private void ShowMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			short person = this.owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			this.turn.TileOwner = this.owner.MapOps.ObtainTileOwnerBits((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			if (participantIdx == 0 && this.turn.TargetTerrain != MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = (ushort)personBit;

				if (((sbyte)this.owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] & personBit) != 0)
				{
					this.processor.ScaleIndex(0xc, this.turn.Troop);
					this.processor.RegDest.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.ShownToParticipants);

					if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
						this.owner.World.Towns[this.owner.Helpers.LocateNearestCity((short)this.turn.UnitHoriz, (short)this.turn.UnitVert)].ParticipantIdx == person)
					{
						this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert];
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));
					}
				}
			}

			if (((byte)this.turn.TargetImprovements & 1) != 0 && this.turn.TileOwner != (ushort)participantIdx &&
				((sbyte)this.owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & personBit) != 0)
			{
				// Into a foreign city the human sees: with an embassy
				this.processor.RegSource.Value16 = (ushort)(person << 4);
				this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);
				bool embassy = (this.owner.World.Participants[person].Relations[participantIdx] & 0x40) != 0;

				if (!embassy)
				{
					this.processor.RegBase.Value16 = (ushort)(this.turn.TileOwner << 1);
					embassy = (this.owner.World.Participants[person].Relations[this.turn.TileOwner] & 0x40) != 0;
				}

				if (embassy)
				{
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
						(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | personBit));
				}
			}

			if (this.turn.TargetTerrain == MapRules.OceanTerrain &&
				(this.owner.MapOps.ObtainTileImprovements((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) & 2) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (byte)(1 << ((byte)this.turn.TileOwner & 0x1f))));
			}

			// The move on the screen
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 && participantIdx != person &&
				(MapRevealed == 0 || participantIdx == 0))
			{
				return;
			}

			if ((this.owner.World.SessionPrefMarkers & 0x20) != 0 || participantIdx == person)
			{
				this.owner.UnitOps.PanMapToPosition(person,
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert);

				this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
					(short)this.turn.Troop,
					this.turn.Direction);
			}
			else
			{
				this.owner.MapOps.RenderMapTile((short)this.turn.UnitHoriz, (short)this.turn.UnitVert);
			}
		}

		/// <summary>
		/// The unit moves to 0x1c / 0x26: 0x14 is the next unit of its old stack, 0x22 / 0x2a its old place; it is seen by
		/// nobody now (written through SI as the calls leave it, as the original does); a computer unit (not a plane) loses its
		/// orders (status bits 4 and 5 stay); the tile leaves another player's territory; at its destination it forgets it and
		/// its heading (a computer unit has no moves left). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void MoveUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.owner.World.PersonParticipantIdx;

			this.processor.RegSource.Value16 = unit;
			this.turn.Cargo = (ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.FollowingInStack);

			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				(short)this.turn.Troop,
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			this.turn.CarrierHoriz = this.turn.UnitHoriz;
			this.turn.CarrierVert = this.turn.UnitVert;

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				(short)this.turn.Troop,
				(short)this.turn.TargetHoriz,
				(short)this.turn.TargetVert);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Horiz), (byte)this.turn.TargetHoriz);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Vert), (byte)this.turn.TargetVert);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.ShownToParticipants), 0);
			this.processor.RegAcc.LowByte = (byte)this.turn.TargetVert;

			if (participantIdx != person)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x30));
			}

			if (this.turn.Dot != 0 && (short)this.turn.Dot < 8 && this.turn.Dot != (ushort)participantIdx)
			{
				this.owner.Services.AssignDot(2, (short)this.turn.TargetHoriz + 80,
					(short)this.turn.TargetVert,
					0);
			}

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == this.turn.TargetHoriz &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) == this.turn.TargetVert)
			{
				this.processor.RegAcc.LowByte = 0xff;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

				if (participantIdx != person)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}
		}

		/// <summary>
		/// After the move: a unit (not a plane) coming from the sea onto land has no moves left; a ship that carries units (or a
		/// carrier) takes its cargo along (0x14 the next unit of its old stack, 0x4a counts up to 20, 0x3c the units taken): land
		/// units (planes for a carrier) that are sentried, or come from the sea, or (computer) without orders and in a stack move
		/// with it, sentried on a transport, the planes with one round of fuel less; until the room is full (a carrier and the
		/// barbarians take all). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void TakeCargoAlong(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			ushort player = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.turn.StartTerrain == MapRules.OceanTerrain && this.turn.TargetTerrain != MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = player;
			this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if ((UnitKindTable.Field(this.processor, (sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.CarrierKind != UnitKindTable.Carrier) || this.turn.Cargo == 0xffff)
			{
				return;
			}

			this.processor.RegAcc.Value16 = 0;
			this.turn.CargoTally = 0;
			this.turn.Count = 0;

			while (true)
			{
				short member = (short)this.turn.Cargo;
				this.processor.RegSource.Value16 = player;
				this.processor.RegDest.Value16 = (ushort)(member * UnitMemory.TroopStride + player);
				this.turn.NextCargo = (ushort)this.troops.PullSigned(participantIdx, member, UnitMemory.FollowingInStack);

				// A transport takes land units, a carrier (no room) planes
				ushort cargoGroup = (ushort)(this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ? 1 : 0);
				this.processor.RegCount.Value16 = cargoGroup;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, member) * UnitKindTable.Stride);

				if (cargoGroup == this.troops.KindField(participantIdx, member, UnitKindTable.LandGroup))
				{
					byte status = this.troops.Pull(participantIdx, member, UnitMemory.Status);
					bool taken = (status & 1) != 0 || this.turn.StartTerrain == MapRules.OceanTerrain;

					if (!taken && participantIdx != this.owner.World.PersonParticipantIdx && (status & 0xc) == 0)
					{
						this.processor.RegBase.Value16 = (ushort)(member * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = player;
						taken = this.troops.Pull(participantIdx, member, UnitMemory.FollowingInStack) != 0xff;
					}

					if (taken)
						CarryUnit(participantIdx, troopIdx, member, player);
				}

				this.turn.Cargo = this.turn.NextCargo;
				this.turn.CargoTally = (ushort)(this.turn.CargoTally + 1);

				if ((short)this.turn.CargoTally >= 0x14 || this.turn.Cargo == 0xffff)
					return;

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = player;
				this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));
				this.processor.RegBase.Value16 = (ushort)((sbyte)(byte)this.turn.CarrierKind * UnitKindTable.Stride);
				this.processor.RegAcc.Value16 = this.turn.Count;

				if ((short)UnitKindTable.Field(this.processor, (sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) <= (short)this.turn.Count &&
					(byte)this.turn.CarrierKind != UnitKindTable.Carrier && participantIdx != 0)
				{
					return;
				}
			}
		}

		/// <summary>One unit of the cargo moves along with the ship (see TakeCargoAlong)</summary>
		private void CarryUnit(short participantIdx, short troopIdx, short member, ushort player)
		{
			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				(short)this.turn.Cargo,
				(short)this.turn.CarrierHoriz,
				(short)this.turn.CarrierVert);

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				(short)this.turn.Cargo,
				(short)this.turn.TargetHoriz,
				(short)this.turn.TargetVert);

			this.processor.RegSource.Value16 = (ushort)(player + member * UnitMemory.TroopStride);
			this.troops.Assign(participantIdx, member, UnitMemory.Horiz, (byte)this.turn.TargetHoriz);
			this.troops.Assign(participantIdx, member, UnitMemory.Vert, (byte)this.turn.TargetVert);
			this.troops.Assign(participantIdx, member, UnitMemory.ShownToParticipants, 0);
			this.troops.Assign(participantIdx, member, UnitMemory.TravelHeading, 0xff);

			this.processor.RegSource.Value16 = player;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(member * UnitMemory.TroopStride);
				this.troops.Assign(participantIdx, member, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, member, UnitMemory.Status) | 1));
			}

			// A plane on a carrier: a round of fuel less
			this.processor.RegSource.Value16 = (ushort)(player + member * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, member) * UnitKindTable.Stride);
			this.processor.RegDest.Value16 = this.troops.KindField(participantIdx, member, UnitKindTable.RoundsAway);

			if (this.processor.RegDest.Value16 != 0)
			{
				this.processor.RegAcc.Value16 = this.processor.RegDest.Value16;
				this.processor.RegAcc.LowByte = (byte)(this.processor.RegAcc.LowByte - 1);
				this.troops.Assign(participantIdx, member, UnitMemory.UnusualSteps, this.processor.RegAcc.LowByte);
			}

			this.turn.Count = (ushort)(this.turn.Count + 1);
		}

		/// <summary>
		/// The unit arrived at 0x1c / 0x26: a foreign city there is seized (the unit shown to its player), the tile belongs to
		/// the player, the unit looks around (ShiftUnit), a hut is visited, a unit (not a ship) at sea wakes its stack (0x1560),
		/// a barbarian on land the human sees near his city is shown to who sees the tile. SI as the calls leave it. The locals
		/// of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void ArriveAtTile(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			short person = this.owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			if (((byte)this.turn.TargetImprovements & 1) != 0 && this.turn.TileOwner != (ushort)participantIdx)
			{
				// A foreign city is taken
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = (byte)(1 << ((byte)this.turn.TileOwner & 0x1f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));

				this.turn.City = this.owner.Helpers.LocateCityAtPosition((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

				this.owner.Conquest.SeizeCity(participantIdx, (short)this.turn.City, 0);

				this.owner.MapOps.TagTileOwned((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			}

			this.owner.MapOps.MapAssignCityOwner(participantIdx, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			this.owner.UnitOps.ShiftUnit(
				(short)this.turn.TargetHoriz,
				(short)this.turn.TargetVert,
				participantIdx,
				(short)this.turn.Troop,
				1);

			if (this.owner.MapOps.ContainsHut(this.turn.TargetTerrain, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != 0)
			{
				this.owner.UnitOps.VisitHut(participantIdx, (short)this.turn.Troop);

				this.owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] |= 1;
			}

			if (this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					this.owner.UnitOps.VisitEachUnitInStack(participantIdx, (short)this.turn.Troop, 0x1560);
			}

			if (participantIdx != 0 || this.turn.TargetTerrain == MapRules.OceanTerrain)
				return;

			// A barbarian the human sees near his city
			this.processor.RegSource.Value16 = (ushort)personBit;

			if (((sbyte)this.owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & personBit) == 0)
				return;

			this.processor.ScaleIndex(0xc, this.turn.Troop);
			this.processor.RegDest.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.ShownToParticipants);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
				this.owner.World.Towns[this.owner.Helpers.LocateNearestCity((short)this.turn.TargetHoriz, (short)this.turn.TargetVert)].ParticipantIdx == person)
			{
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert];
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));
			}
		}

		/// <summary>
		/// The end of the move: a move the human sees (or his own, or of a computer player with everything shown) shows the
		/// unit on the map (with the view on it for the human or the option 0x20, a short wait for a computer move unless
		/// 0xb278); 0xa = 0; a plane in a city lands (no orders, no moves, no destination), a plane on a carrier gets its fuel
		/// back (written through SI as TallyUnitsOfTypeInStack leaves it, as the original does). The locals of HandlePlayerTurn
		/// are used as in the original.
		/// </summary>
		private void FinishMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.owner.World.PersonParticipantIdx;

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << (person & 0xff))) != 0 || participantIdx == person ||
				(MapRevealed != 0 && participantIdx != 0))
			{
				if (participantIdx == person || (this.owner.World.SessionPrefMarkers & 0x20) != 0)
				{
					this.owner.UnitOps.PanMapToPosition(person, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, (short)this.turn.Troop);

					if (participantIdx != person && WorldMapView != 1)
						this.owner.TextPainter.AwaitTime(30);
				}
				else
				{
					if (this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, (short)this.turn.Troop) != 0)
						this.owner.TextPainter.AwaitTime(10);
				}
			}

			this.turn.AITries = 0;

			if (((byte)this.turn.TargetImprovements & 1) != 0)
			{
				// A plane lands in a city
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
				}
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegDest.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) == 0 ||
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				return;
			}

			// A plane on a carrier (the unit through SI as the tally leaves it)
			if (this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, (short)this.turn.Troop, 23) == 0)
				return;

			ushort status = (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Status);
			this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) & 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.LeftoverSteps), 0);
			sbyte kind = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Kind));
			this.processor.RegBase.Value16 = (ushort)(kind * UnitKindTable.Stride);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + UnitKindTable.Origin + UnitKindTable.RoundsAway));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.UnusualSteps), this.processor.RegAcc.LowByte);
		}

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
			short person = this.owner.World.PersonParticipantIdx;
			bool everythingShown = MapRevealed != 0;

			this.processor.RegAcc.Value16 = key;

			switch (key)
			{
				case 0x3b00: // F1
					if (everythingShown)
						this.owner.ArmyPicker.DisplayWhichArmy();
					else
						this.owner.Briefings.DisplayCityStatusReport(person);
					break;

				case 0x3c00: // F2
					this.owner.Briefings.RenderUnitCountsChart(person);
					break;

				case 0x3d00: // F3
					if (everythingShown)
					{
						this.owner.Startup.ConcealMouseCursor();
						this.owner.Services.DisplayScreen(2);
						this.owner.Libc.ReadKey();
						this.owner.Services.DisplayScreen(0);
						this.owner.Startup.DisplayMouseCursor();
					}
					else
					{
						this.owner.Briefings.DisplayMilitaryReport();
					}
					break;

				case 0x3e00: // F4
					this.owner.Briefings.DisplayAttitudeReport(person);
					break;

				case 0x3f00: // F5
					this.owner.Briefings.DisplayTradeReport(person);
					break;

				case 0x4000: // F6
					this.owner.Briefings.DisplayScienceReport(person);
					break;

				case 0x4100: // F7
					if (everythingShown)
					{
						for (short civ = 1; civ < 8; civ++)
						{
							this.turn.Count = (ushort)civ;
							this.owner.RealmInfo.DisplayCivilizationOverview(civ);
						}

						this.turn.Count = 8;
					}
					else
					{
						this.owner.Atlas.DisplayWondersOfTheWorld();
					}
					break;

				case 0x4200: // F8
					if (everythingShown)
					{
						this.owner.Atlas.DisplayCivilizationGraph();
						this.owner.Replayer.DisplayReplay();
					}
					else
					{
						this.owner.Legends.DisplayTopCities();
					}
					break;

				case 0x4300: // F9
					if (everythingShown)
						this.owner.Atlas.DisplayGameFigures();
					else
						this.owner.Founding.DisplayFinalScore(person, true);
					break;

				case 0x4400: // F10
					this.owner.Atlas.DisplayWorldMap(1);
					break;

				case 0x4838: // Shift + Up
				case 0x4939: // Shift + PageUp
				case 0x4d36: // Shift + Right
				case 0x5133: // Shift + PageDown
				case 0x5032: // Shift + Down
				case 0x4f31: // Shift + End
				case 0x4b34: // Shift + Left
				case 0x4737: // Shift + Home
					this.turn.Direction = ShiftedKeyDirection(key);
					PanViewByDirection();
					break;

				case 0x2000: // Alt + D
					MapRevealed = (ushort)~MapRevealed;
					this.turn.Direction = 0;
					PanViewByDirection();
					break;

				case 0xd: // Enter
				case 0x20: // Space
					if (this.turn.Troop == 0x80)
						return KeyStep.EndOfTurn;
					break;

				case 0x2f: // '/'
				case 0x3f: // '?'
					this.owner.CityNaming.DisplayNewCityLocation();
					break;

				case 0x53: // 'S'
					if (this.owner.World.RoundTally != 0)
						this.owner.SaveLoad.DisplayStoreGameDialog(0xffff);
					break;

				case 0xfffe: // The revolution
					StartRevolution(participantIdx);
					break;

				case 0x3200: // Alt + M
					this.owner.Services.PrepareMouse();
					break;

				case 0x3d: // '='
				case 0x2b: // '+'
					SelectTaxRate(participantIdx);
					break;

				case 0x2d: // '-'
				case 0x5f: // '_'
					SelectLuxuriesRate(participantIdx);
					break;

				case 0x74: // 't'
					this.processor.EmitGlobalWord(0xdcfc, 1);
					this.owner.MapOps.FocusMapView(participantIdx, (short)MapViewHoriz, (short)MapViewVert);
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();
					this.processor.EmitGlobalWord(0xdcfc, 0);
					this.owner.MapOps.FocusMapView(participantIdx, (short)MapViewHoriz, (short)MapViewVert);
					break;

				case 0x1300: // Alt + R
					// Three random words of each of 16 records of 0x3a bytes, then the dialog 0x2003
					for (ushort record = 0; record < 0x10; record++)
					{
						this.turn.Result = record;
						this.processor.RegAcc.Value16 = 0x3a;
						this.processor.ScaleAccumulatorToSource(record);

						for (ushort word = 0; word < 6; word += 2)
						{
							this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1512 + word),
								(ushort)(this.owner.Libc.Dice.DrawInt(3) - 1));
						}
					}

					this.turn.Result = 0x10;
					this.owner.TurnHooks.DisplayDialog(0x2003, 100, 80);
					break;

				case 0x2f00: // Alt + V
					this.owner.World.SessionPrefMarkers ^= 0x10;
					Message("Sounds ")
						.Text((ushort)((this.owner.World.SessionPrefMarkers & 0x10) != 0 ? 0x2020 : 0x2024))
						.Dialog(100, 80);
					break;

				case 0x2300: // Alt + H
					this.owner.HelpSystem.DisplayHelpPopup(0x2029);
					this.owner.HelpSystem.DisplayHelpPopup(0x2030);
					break;

				case 0x1000: // Alt + Q
					ConfirmQuit();
					break;

				case 0x1b: // Escape
					if (MousePresent == 0 && this.turn.MarkedTileMode != 0)
					{
						this.turn.MarkedTileMode = 0;
						ForgetMarkedTileOnScreen(participantIdx);
					}
					break;

				case 0x9: // Tab
					if (MousePresent == 0)
						ToggleMarkedTile(participantIdx);
					break;

				case 0x2200: // Alt + G
					return ManageMenu(participantIdx, 0);

				case 0x1800: // Alt + O
					return ManageMenu(participantIdx, 1);

				case 0x1e00: // Alt + A
					return ManageMenu(participantIdx, 2);

				case 0x1100: // Alt + W
					return ManageMenu(participantIdx, 3);

				case 0x2e00: // Alt + C
					return ManageMenu(participantIdx, 4);
			}

			return KeyStep.NextLook;
		}

		/// <summary>The direction 1 - 8 of Shift and an arrow (the keypad digits in the low byte)</summary>
		private static ushort ShiftedKeyDirection(ushort key)
		{
			switch (key)
			{
				case 0x4838: return 1;
				case 0x4939: return 2;
				case 0x4d36: return 3;
				case 0x5133: return 4;
				case 0x5032: return 5;
				case 0x4f31: return 6;
				case 0x4b34: return 7;
				default: return 8;
			}
		}

		/// <summary>The view moves four tiles in the direction 0x24 (SI = 2 * the direction, as in the original)</summary>
		private void PanViewByDirection()
		{
			this.processor.RegSource.Value16 = (ushort)(this.turn.Direction << 1);

			ushort horiz = (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) << 2);
			MapViewHoriz = (ushort)(MapViewHoriz + horiz);

			ushort vert = (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) << 2);
			MapViewVert = (ushort)(MapViewVert + vert);

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				(short)MapViewHoriz,
				(short)MapViewVert);
		}

		/// <summary>
		/// The revolution: after the dialog 0x1f6a, answer 1 makes the government anarchy, with the newspaper and the new
		/// government graphics (SI = 2 * the player, as in the original)
		/// </summary>
		private void StartRevolution(short participantIdx)
		{
			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.BlockText.DisplayTextDialog(0x1f6a);
			if (this.owner.TurnHooks.DisplayDialog(0xba06, 64, 80) != 1)
				return;

			this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);
			this.owner.World.Participants[participantIdx].RegimeKind = 0;

			Message("The ")
				.Text(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)))
				.Text(" are\nrevolting! Citizens\ndemand new govt.\n");
			this.owner.Paper.DisplayNewspaper(-1);
			this.owner.NewGameWizard.ImportGovernmentGraphics();
			this.owner.TurnHooks.RepaintMainScreen();
		}

		/// <summary>
		/// The tax rate: a dialog of every split of tax and science (the luxuries stay); 0x3a the choice, -1 keeps the rates
		/// </summary>
		private void SelectTaxRate(short participantIdx)
		{
			ref var participant = ref this.owner.World.Participants[participantIdx];

			this.owner.Libc.CopyText(0xba06, "Select new Tax rate:\n ");

			short rate = 0;

			for (; (short)(participant.LevyRatio + participant.ResearchLevyRatio) >= rate; rate++)
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)(10 * rate), 10));
				this.owner.Libc.AppendText(0xba06, "% Tax, (");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((short)((participant.LevyRatio + participant.ResearchLevyRatio - rate) * 10), 10));
				this.owner.Libc.AppendText(0xba06, "% Science)\n ");
			}

			this.turn.Result = (ushort)rate;
			DefaultLine = (ushort)participant.LevyRatio;

			this.turn.Result = this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			if (this.turn.Result == 0xffff)
				return;

			participant.ResearchLevyRatio += (short)(participant.LevyRatio - (short)this.turn.Result);
			participant.LevyRatio = (short)this.turn.Result;
			this.processor.RegAcc.Value16 = this.turn.Result;

			this.owner.TurnHooks.RenderStatusBar();
		}

		/// <summary>
		/// The luxuries rate: a dialog of every split of luxuries and science (the tax stays); 0x3a the choice, -1 keeps the
		/// rates
		/// </summary>
		private void SelectLuxuriesRate(short participantIdx)
		{
			ref var participant = ref this.owner.World.Participants[participantIdx];

			this.owner.Libc.CopyText(0xba06, "Select new Luxuries rate:\n ");

			short rate = 0;

			for (; (short)(10 - participant.LevyRatio) >= rate; rate++)
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)(10 * rate), 10));
				this.owner.Libc.AppendText(0xba06, "% Luxuries, (");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((short)(-(rate + participant.LevyRatio - 10) * 10), 10));
				this.owner.Libc.AppendText(0xba06, "% Science)\n ");
			}

			this.turn.Result = (ushort)rate;
			DefaultLine = (ushort)-(participant.LevyRatio + participant.ResearchLevyRatio - 10);

			this.turn.Result = this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			if (this.turn.Result == 0xffff)
				return;

			participant.ResearchLevyRatio = (short)-(participant.LevyRatio + (short)this.turn.Result - 10);
			this.processor.RegAcc.Value16 = (ushort)participant.ResearchLevyRatio;

			this.owner.TurnHooks.RenderStatusBar();
		}

		/// <summary>
		/// The quit: Load Game from the GAME menu (0xdc48) leaves at once, else the dialog; Keep Playing clears 0xdc48, Yes
		/// sets it to 1 (when it was 0); leaving ends the moves of the units (0x32 = 0x80)
		/// </summary>
		private void ConfirmQuit()
		{
			if (GameEndRequest != GameLoadAndSave.FetchSessionAsk)
			{
				Message("Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n")
					.Dialog(100, 80);

				if (this.processor.RegAcc.Value16 != 1)
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
			if ((this.owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (participantIdx & 0xff))) != 0)
				this.owner.MapOps.RepaintMapTile((short)this.turn.CursorHoriz, (short)this.turn.CursorVert);

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
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + this.turn.Troop * UnitMemory.TroopStride);
					ushort place = (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin);

					if (VerifyPositionOnScreen(
						this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Horiz)),
						this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Vert))) != 0)
					{
						this.turn.CursorHoriz = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Horiz));
						this.turn.CursorVert = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Vert));
						onUnit = true;
					}
				}

				if (!onUnit)
				{
					this.turn.CursorHoriz = (ushort)(MapViewHoriz + 7);
					this.turn.CursorVert = (ushort)(MapViewVert + 6);
				}

				if ((this.owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (participantIdx & 0xff))) != 0)
					this.owner.MapOps.RepaintMapTile((short)this.turn.CursorHoriz, (short)this.turn.CursorVert);
			}

			this.turn.MarkedTileMode = (ushort)(this.turn.MarkedTileMode ^ 1);
		}

		/// <summary>The menu of the menu bar; a chosen order (0xd4ca) becomes the order 0x44 of the unit</summary>
		private KeyStep ManageMenu(short participantIdx, short menu)
		{
			this.owner.MenuSystem.ManageMenuBar(participantIdx, (short)this.turn.Troop, menu);

			if (ChosenOrder == 0xffff)
				return KeyStep.NextLook;

			this.turn.Order = ChosenOrder;
			return KeyStep.MenuOrder;
		}

		/// <summary>
		/// After an order or a move: a unit with moves left goes on (true; unless 0x28 or the quit 0xdc48). Else its move
		/// ends (0x1e = 0 while it still had moves): a plane gets its fuel back in a city or on a carrier, burns one round and
		/// is lost without fuel; a trireme of the human may be lost (one in two) on a tile without land around. The panel of
		/// the unit is shown again for a human player (not for an emptied slot). The locals of HandlePlayerTurn and BX, SI, DI
		/// are used as in the original.
		/// </summary>
		private bool ContinueOrFinishUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (troopIdx < UnitMemory.TroopTally)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0 && this.turn.Ordered == 0 &&
					GameEndRequest == 0)
				{
					return true;
				}

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
					return false;

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0)
				{
					this.turn.AllMoved = 0;
				}
				else
				{
					BurnPlaneFuel(participantIdx, troopIdx);
					MayLoseTrireme(participantIdx, troopIdx);
				}
			}

			if (this.owner.World.IsHuman(participantIdx))
				RenderUnitInfo(participantIdx, this.turn.Troop);

			return false;
		}

		/// <summary>
		/// A plane at the end of its move: in a city or on a carrier it gets all its rounds back, then it burns one; below
		/// zero it is lost (help 0x2070)
		/// </summary>
		private void BurnPlaneFuel(short participantIdx, short troopIdx)
		{
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) == 0)
				return;

			this.owner.MapOps.ObtainTileImprovements(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((this.processor.RegAcc.LowByte & 1) != 0 ||
				this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, (short)this.turn.Troop, 23) != 0)
			{
				// In a city or on a carrier
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			byte rounds = (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) - 1);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, rounds);

			if ((sbyte)rounds < 0)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);
				DisplayHelpForHuman(0x2070);
			}
		}

		/// <summary>
		/// A trireme of the human at the end of its move is lost (help 0x2076) one time in two when no tile around is land
		/// (0x3c the last direction looked at, 0x38 land found; SI and DI as in the original)
		/// </summary>
		private void MayLoseTrireme(short participantIdx, short troopIdx)
		{
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10 || participantIdx != this.owner.World.PersonParticipantIdx)
				return;

			if (this.owner.Libc.Dice.DrawInt(2) == 0)
				return;

			this.turn.Found = 0;

			for (ushort direction = 1; direction <= 8; direction++)
			{
				this.turn.Count = direction;
				this.processor.RegSource.Value16 = (ushort)(direction << 1);
				this.processor.RegDest.Value16 = UnitMemory.Offset(participantIdx, troopIdx);

				if (this.owner.MapOps.ObtainTerrainType(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4))) != MapRules.OceanTerrain)
				{
					this.turn.Found = 1;
					break;
				}

				if (direction == 8)
					this.turn.Count = 9;
			}

			if (this.turn.Found != 0)
				return;

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);
			DisplayHelpForHuman(0x2076);
		}

		/// <summary>
		/// Repaints the tile when the human player has seen it (or everything is shown, 0xd806)
		/// </summary>
		public void RepaintTileIfVisible(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintTileIfVisible({horizLoc}, {vertLoc})");

			if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0 ||
				MapRevealed != 0)
			{
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.processor.Journal.CloseScope("RepaintTileIfVisible");
		}

		/// <summary>
		/// Repaints the tile of the unit when the human player sees it (own unit, or the unit is shown to the human)
		/// </summary>
		public void RepaintUnitIfVisible(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RepaintUnitIfVisible({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			short human = this.owner.World.PersonParticipantIdx;

			if (participantIdx == human ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << human)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("RepaintUnitIfVisible");
		}

		/// <summary>
		/// 0 on a tile with a city, irrigation or a mine (improvement bits 0 - 2). Otherwise from the terrain table at 0x44a
		/// (12 bytes a terrain): 2 when the word at +4 is -3 or less, 1 when the word at +0 is -2 and the tile is at the coast
		/// (VerifyCoastalTile), else 0.
		/// </summary>
		public ushort ObtainTileMoveCost(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTileMoveCost({horizLoc}, {vertLoc})");

			ushort cost = 0;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x7) == 0)
			{
				int terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				ushort terrainRecord = (ushort)(0x44a + 0xc * terrain);

				if ((short)this.processor.PullGlobalWord((ushort)(terrainRecord + 4)) <= -3)
				{
					cost = 2;
				}
				else if (this.processor.PullGlobalWord(terrainRecord) == 0xfffe)
				{
					if (VerifyCoastalTile(horizLoc, vertLoc) != 0)
						cost = 1;
				}
			}

			this.processor.RegAcc.Value16 = cost;

			this.processor.Journal.CloseScope("ObtainTileMoveCost");

			return cost;
		}

		/// <summary>
		/// Whether the tile is on the coast: it is terrain 11 itself, or one of the 4 tiles next to it in a straight line
		/// (headings 1, 3, 5, 7; the column is not wrapped) is terrain 10 or 11 or has improvement bit 1 - but tiles with a city
		/// do not count
		/// </summary>
		/// <returns>1 on the coast, else 0 (also in AX)</returns>
		public ushort VerifyCoastalTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyCoastalTile({horizLoc}, {vertLoc})");

			MapLayers chart = this.owner.MapOps.Chart;
			ushort coastal = 0;

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xb)
			{
				coastal = 1;
			}
			else
			{
				for (int heading = 1; heading <= 8 && coastal == 0; heading += 2)
				{
					short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
					short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

					ushort terrain = this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);
					ushort improvements = this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert);

					if ((improvements & 0x1) == 0 && (terrain == 0xb || terrain == 0xa || (improvements & 0x2) != 0))
					{
						coastal = 1;
					}
				}
			}

			this.processor.RegAcc.Value16 = coastal;

			this.processor.Journal.CloseScope("VerifyCoastalTile");

			return coastal;
		}

		/// <summary>
		/// The panel of the active unit (left, under the world map): the civilization, the type (Veteran), the moves (with the
		/// fuel of aircraft), the home city, the terrain and its improvements, then the units of the city on the tile (the two
		/// supported at 0x1c * city ... Unfamiliar, drawn through the unit 127) and the stack (up to row 0xb8, "more" after).
		/// Unit 0x80: no unit.
		/// </summary>
		public void RenderUnitInfo(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"RenderUnitInfo({participantIdx}, {arg2})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);

			if (arg2 == 0x80)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x207f, 4, 124, 0);

				this.owner.TextPainter.RenderTextToScreen0(0x208b, 4, 136, 0);

				this.owner.TextPainter.RenderTextToScreen0(0x2097, 4, 144, 0);
			}
			else
			{
				RenderUnitPanel(participantIdx, arg2);
			}

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("RenderUnitInfo");
		}

		private void RenderUnitPanel(short participantIdx, ushort arg2)
		{
			short troopIdx = (short)arg2;
			short vertLoc = 0x63;

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, 99, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, (ushort)((0x22 * this.troops.TroopKind(participantIdx, troopIdx)) + 0x112a));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x20) != 0)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x20a3, 8, vertLoc, 0);

				vertLoc += 8;
			}

			sbyte steps = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps);
			short moves = (short)this.owner.Helpers.VerifyValueSpan(steps / 3, 0, 99);

			Message("Moves: ")
				.Number(moves);

			short thirds = (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) % 3);

			if (thirds != 0)
			{
				this.owner.Libc.AppendText(0xba06, ".");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(thirds, 10));
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
			{
				// Aircraft: the moves left with the fuel of the next turns
				this.owner.Libc.AppendText(0xba06, "(");

				moves += (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) *
					(short)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(moves, 10));

				this.owner.Libc.AppendText(0xba06, ")");
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			EmptyMessage()
				.City(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			Message("(")
				.Text((ushort)((0x13 * this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))) + 0x282))
				.Text(")");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			byte improvements = (byte)this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			// Railroad or road, irrigation or mines, pollution
			foreach ((byte bit, byte otherBit, ushort text, ushort otherText) in new[] { ((byte)0x10, (byte)0x8, (ushort)0x20bd, (ushort)0x20c8),
				((byte)0x2, (byte)0x4, (ushort)0x20cf, (ushort)0x20dc), ((byte)0x40, (byte)0x0, (ushort)0x20e5, (ushort)0x0) })
			{
				ushort shown = (improvements & bit) != 0 ? text : (improvements & otherBit) != 0 ? otherText : (ushort)0;

				if (shown != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(shown, 4, vertLoc, 0);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short followingIdx = this.troops.Following(participantIdx, troopIdx);
			short column = 8;

			if ((this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				// The units the city supports elsewhere are drawn through the unit 127 of the player
				for (int unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
				{
					byte unfamiliar = (byte)this.owner.World.Towns[townIdx].Unfamiliar[unfamiliarIdx];

					if (unfamiliar == 0xff)
						continue;

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, (byte)(unfamiliar & 0x3f));
					this.troops.Assign(participantIdx, 127, UnitMemory.Status, 0x8);
					this.troops.Assign(participantIdx, 127, UnitMemory.TravelHoriz, 0xff);

					this.owner.MapOps.RenderUnitSprite(participantIdx, 127, column, vertLoc);

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, 0xff);

					column += 0x10;
				}
			}

			while (followingIdx != -1)
			{
				if (followingIdx == troopIdx || vertLoc >= 0xb8)
					break;

				this.owner.MapOps.RenderUnitSprite(participantIdx, followingIdx, column, vertLoc);

				column += 0x10;

				if (column > 0x40)
				{
					column = 8;
					vertLoc += 0x10;
				}

				followingIdx = this.troops.Following(participantIdx, followingIdx);
			}

			if (followingIdx != -1 && followingIdx != troopIdx)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x20f1, 74, 192, 0);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort VerifyPositionOnScreen(int horizLoc, int vertLoc)
		{
			this.owner.Journal.OpenScope($"VerifyPositionOnScreen({horizLoc}, {vertLoc})");

			// function body
			if (horizLoc < 16 && (short)MapViewHoriz >= 65)
			{
				horizLoc += 80;
			}

			if (horizLoc < (short)MapViewHoriz ||
				horizLoc >= (short)MapViewHoriz + 15 ||
				vertLoc < (short)MapViewVert ||
				vertLoc >= (short)MapViewVert + 12)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 1;
			}

			this.owner.Journal.CloseScope("VerifyPositionOnScreen");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Throws away the keys waiting and waits until the mouse buttons are released
		/// </summary>
		public void AwaitForKey()
		{
			this.processor.Journal.OpenScope("AwaitForKey()");

			while (this.owner.Libc.KeyAvailable() != 0)
			{
				this.owner.Dialogs.ObtainNavigationKey();
			}

			do
			{
				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.GlobDb3a != 0);

			this.processor.Journal.CloseScope("AwaitForKey");
		}

		/// <summary>
		/// The next unit to activate in the stack of the unit: following the ring (at most 16 steps), the non-ship unit with the
		/// smallest index after the unit, counting round past 127
		/// </summary>
		/// <returns>That unit, the unit itself when it stands alone or nothing comes (also in AX)</returns>
		public ushort LocateBestGuardian(short participantIdx, ushort troopIdx)
		{
			this.processor.Journal.OpenScope("LocateBestGuardian()");

			ushort best = troopIdx;
			short current = this.troops.Following(participantIdx, troopIdx);

			if (current != -1)
			{
				short smallestStep = 999;

				for (int stepIdx = 0; stepIdx < 0x10; stepIdx++)
				{
					if (this.troops.KindField(participantIdx, current, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					{
						short step = (short)(current > (short)troopIdx ? current - troopIdx : current - troopIdx + 0x80);

						if (step < smallestStep)
						{
							smallestStep = step;
							best = (ushort)current;
						}
					}

					current = this.troops.Following(participantIdx, current);

					if (current == (short)troopIdx)
						break;
				}
			}

			this.processor.RegAcc.Value16 = best;

			this.processor.Journal.CloseScope("LocateBestGuardian");

			return best;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayHelpForHuman(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpForHuman(0x{charsAddr:x4})");

			// function body
			if ((short)TurnPlayer == this.owner.World.PersonParticipantIdx)
			{
				this.owner.HelpSystem.DisplayHelpPopupForHuman(charsAddr);
			}
		
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpForHuman");
		}
	}
}
