using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1403.
	/// </summary>
	public class PlayerTurn
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public PlayerTurn(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// The turn of a player: passes over his units (another pass while some unit waited or was off the view), each unit
		/// gets its orders (from ShiftAIUnit, its standing orders or the keys and clicks of the human) and carries them out
		/// or moves; the human gets a last look without a unit at the end of the turn. The locals (frame 0x52) are shared by
		/// the methods it calls, as in the original.
		/// </summary>
		public void HandlePlayerTurn(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePlayerTurn({participantIdx})");

			this.processor.EnterFrame(0x52);
			this.processor.PreserveIndexRegisters();

			// The player, the help of some turns, the moves of the units
			PrepareTurn(participantIdx);

			bool newPass = true;

			while (true)
			{
				if (newPass)
				{
					// A pass over the units (0x1e stays 1 unless a unit is left with moves)
					this.processor.EmitLocalWord(0x1e, 1);
					this.processor.EmitLocalWord(0x32, 0);
				}

				bool endOfTurn = false;

				while ((short)this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally)
				{
					if (!VisitUnit(participantIdx))
					{
						endOfTurn = true;
						break;
					}

					this.processor.EmitLocalWord(0x32, (ushort)(this.processor.PullLocalWord(0x32) + 1));
				}

				if (endOfTurn)
					break;

				// After all units: another pass while units still move, else the look at the end of the turn of the human
				// (no unit of his took a key, or the option 4), else the end
				newPass = true;

				if (this.processor.PullLocalWord(0x1e) == 0 && this.processor.PullGlobalWord(0xdc48) == 0)
					continue;

				if (this.processor.PullGlobalWord(0xdc48) != 0 || (this.owner.World.ParticipantMarkers & (1 << participantIdx)) == 0 ||
					(this.processor.PullLocalWord(0x6) != 0 && (this.owner.World.SessionPrefMarkers & 4) == 0))
				{
					break;
				}

				this.processor.EmitLocalWord(0x32, UnitMemory.TroopTally);
				this.processor.EmitLocalWord(0x1e, 0);
				this.processor.EmitLocalWord(0x8, 1);

				if (!LookAtUnit(participantIdx))
					break;

				this.processor.EmitLocalWord(0x32, (ushort)(this.processor.PullLocalWord(0x32) + 1));
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

			if (this.processor.PullGlobalWord(0xdc48) != 0)
			{
				// The quit leaves the units
				this.processor.EmitLocalWord(0x32, UnitMemory.TroopTally);
				return true;
			}

			if (this.processor.PullLocalWord(0x32) == this.processor.PullLocalWord(0x46))
				this.processor.EmitLocalWord(0x2e, 1);

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
			if ((this.owner.World.ParticipantMarkers & (1 << participantIdx)) != 0)
				AwaitForKey();

			this.processor.EmitLocalWord(0xa, 0);

			if (this.processor.PullLocalWord(0x36) != 0)
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0xc, 0);
			this.processor.EmitLocalWord(0x36, 0);
			this.processor.EmitLocalWord(0x28, 0);

			while (true)
			{
				LookStep step = ObtainOrder(participantIdx);

				while (step != LookStep.NextLook && step != LookStep.EndOfTurn)
				{
					switch (step)
					{
						case LookStep.OrderOrKey:
							step = (short)this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally ? LookStep.Order : LookStep.Direction;
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
			if ((this.owner.World.ParticipantMarkers & (1 << participantIdx)) == 0)
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
			if ((short)this.processor.PullLocalWord(0x32) >= UnitMemory.TroopTally && this.processor.PullLocalWord(0x4) == 0)
				return LookStep.Key;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);

			if (this.processor.PullLocalWord(0x44) == 'c')
			{
				CenterView(participantIdx);
				return LookStep.Key;
			}

			if (KeyDirection(this.processor.PullLocalWord(0x44)) == 0)
				return LookStep.Key;

			this.processor.EmitLocalWord(0x24, KeyDirection(this.processor.PullLocalWord(0x44)));
			return LookStep.Move;
		}

		/// <summary>
		/// The move in the direction 0x24: the marked tile moves, or a barbarian pillages a tile of the human on its way, or
		/// the unit moves (the checks of the target tile, an attack, the cost, the territory, the move and the arrival).
		/// After a won or lost attack the next look comes at once, else the key of the order.
		/// </summary>
		private LookStep MoveTheUnit(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x4) != 0)
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
			this.processor.EmitGlobalWord(0x6b90, (ushort)participantIdx);
			this.processor.EmitLocalWord(0x20, (ushort)this.owner.World.ParticipantMarkers);
			this.processor.EmitLocalWord(0xe, 0xffff);
			this.processor.EmitLocalWord(0x4, 0);

			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				this.owner.World.ParticipantMarkers = (short)(1 << (person & 0xff));

				if (this.owner.World.RoundTally == 20 || this.owner.World.RoundTally == 60)
					this.owner.HelpSystem.DisplayHelpPopup(0x1e60);

				if (this.owner.World.RoundTally == 40 || this.owner.World.RoundTally == 80)
					this.owner.HelpSystem.DisplayHelpPopup(0x1e67);
			}

			for (this.processor.EmitLocalWord(0x32, 0); (short)this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally;
				this.processor.EmitLocalWord(0x32, (ushort)(this.processor.PullLocalWord(0x32) + 1)))
			{
				short troopIdx = (short)this.processor.PullLocalWord(0x32);

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
			this.processor.EmitLocalWord(0x6, 0);
			this.processor.EmitLocalWord(0x8, 0);
			this.processor.EmitLocalWord(0x28, 0);
			this.processor.EmitLocalWord(0x2e, 0);
			this.processor.EmitLocalWord(0x46, 0x7f);
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			// (SI as the original leaves it: the player's units, or the unit when it goes on or was fortifying)
			ushort player = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			ushort unit = (ushort)(player + troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = player;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
				return false;

			if (this.troops.TroopKind(participantIdx, troopIdx) >= 0x1c)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 1);

			if (this.processor.PullLocalWord(0x8) == 0)
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
				this.processor.EmitLocalWord(0x3a, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));

				if ((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x3a)].TrueExtent >= 3)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.PullLocalByte(0x3a));
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.processor.EmitLocalWord(0x3a, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));

				if ((ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == this.processor.PullLocalWord(0x3a) &&
					this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx, horizLoc, vertLoc) == 0)
				{
					this.owner.UnitOps.KeepUnitsInCityList((short)this.processor.PullLocalWord(0x3a));
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

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0)
				return true;

			// On the map view (the unsigned comparisons of the original)
			ushort horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.processor.RegDest.Value16 = horizLoc;
			ushort viewHoriz = this.processor.PullGlobalWord(0xd4cc);

			if (viewHoriz < horizLoc)
			{
				this.processor.EmitLocalWord(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
				ushort viewVert = this.processor.PullGlobalWord(0xd75e);

				if (this.processor.PullLocalWord(0x4e) > viewVert && (ushort)(viewHoriz + 0xe) >= horizLoc &&
					(ushort)(viewVert + 0xa) > this.processor.PullLocalWord(0x4e))
				{
					return true;
				}
			}

			if (this.processor.PullLocalWord(0x2e) == 0)
			{
				this.processor.EmitLocalWord(0x1e, 0);
				return false;
			}

			if (this.processor.PullGlobalWord(0xb278) != 1)
				this.owner.TextPainter.AwaitTime(30);

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.owner.MapOps.FocusMapView(participantIdx,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 7,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - 6);

			this.processor.EmitLocalWord(0x46, (ushort)((this.processor.PullLocalWord(0x32) - 1) & 0x7f));
			this.processor.EmitLocalWord(0x2e, 0);

			if (this.owner.World.RoundTally == 0)
				RenderUnitInfo(participantIdx, this.processor.PullLocalWord(0x32));

			return true;
		}

		/// <summary>
		/// The order of a computer unit (0x44) is the answer of ShiftAIUnit: with one the unit forgets its destination; after
		/// more than 4 tries (0xa) it is 0x20 and the unit forgets its heading and destination. The locals of HandlePlayerTurn
		/// are used as in the original, SI as the original leaves it.
		/// </summary>
		private void ObtainAIOrder(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.EmitLocalWord(0x44, this.owner.Bot.ShiftAIUnit(participantIdx, troopIdx));

			if (this.processor.PullLocalWord(0x44) != 0)
			{
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			}

			this.processor.EmitLocalWord(0xa, (ushort)(this.processor.PullLocalWord(0xa) + 1));

			if ((short)this.processor.PullLocalWord(0xa) > 4)
			{
				this.processor.EmitLocalWord(0x44, 0x20);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
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
			if ((short)this.processor.PullLocalWord(0x32) >= UnitMemory.TroopTally)
				return false;

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) == 0 &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
			{
				return false;
			}

			this.processor.EmitLocalWord(0x44, 0x72);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x40) != 0)
				this.processor.EmitLocalWord(0x44, (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ? 0x6d : 0x69));

			this.processor.EmitLocalByte(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status));

			if ((this.processor.PullLocalByte(0x4e) & 0x80) != 0)
			{
				this.processor.EmitLocalWord(0x44, 0x6d);

				if ((this.processor.PullLocalByte(0x4e) & 0x40) != 0)
					this.processor.EmitLocalWord(0x44, 0x66);

				if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 2) != 0)
					this.processor.EmitLocalWord(0x44, 0x70);
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				this.processor.EmitLocalWord(0x44, 0x68);

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

			RenderUnitInfo(participantIdx, this.processor.PullLocalWord(0x32));

			if (this.owner.World.RoundTally == 0 && this.processor.PullLocalWord(0x8) == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);
				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8),
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

				this.owner.HelpSystem.DisplayHelpPopup(0x1e6e);

				this.processor.EmitLocalWord(0x8, 1);
			}

			if (this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally)
			{
				short troopIdx = (short)this.processor.PullLocalWord(0x32);
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0 && this.processor.PullLocalWord(0x8) == 0)
				{
					this.owner.HelpSystem.DisplayTileInfo(participantIdx, this.processor.PullLocalWord(0x32));
					this.processor.EmitLocalWord(0x8, 1);
				}
			}

			this.owner.UnitOps.ChoreographUnitMove(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12));

			if (this.processor.PullLocalWord(0x32) != 0x80)
				this.processor.EmitLocalWord(0x6, 1);

			if (this.owner.GlobDb3a == 0)
			{
				this.processor.EmitLocalWord(0x44, this.owner.Dialogs.ObtainNavigationKey());

				if (this.processor.PullLocalWord(0x44) != 0xd || this.processor.PullLocalWord(0x4) == 0)
					return HumanOrder.Order;

				// Enter on the marked tile: a click there
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
				this.processor.EmitLocalWord(0x1c, this.processor.PullLocalWord(0xe));
				this.processor.EmitLocalWord(0x26, this.processor.PullLocalWord(0x12));
				this.owner.GlobDb3c = 0x50;
				this.owner.GlobDb3e = 0x8;
				this.owner.GlobDb3a = 0x1;

				return TakeClick(participantIdx);
			}

			LocateClickedTile();

			if ((short)this.owner.GlobDb3e < 8)
			{
				// The menu bar
				this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), -1);

				AwaitForKey();

				if (this.processor.PullGlobalWord(0xd4ca) == 0xffff)
					return HumanOrder.NextLook;

				this.processor.EmitLocalWord(0x44, this.processor.PullGlobalWord(0xd4ca));

				return (short)this.processor.PullLocalWord(0x32) < 0x80 ? HumanOrder.MenuOrder : HumanOrder.MenuWithoutUnit;
			}

			if ((short)this.owner.GlobDb3c < 0x50)
			{
				if ((short)this.owner.GlobDb3e < 0x3a)
				{
					// The world map: the view moves there
					this.owner.Routing.TunePosition(
						(short)this.owner.GlobDb3c + this.processor.PullGlobalSignedWord(0x6ed6) - 7);

					this.owner.MapOps.FocusMapView(participantIdx,
						(short)this.processor.RegAcc.Value16,
						this.owner.Helpers.VerifyValueSpan((short)this.owner.GlobDb3e + this.processor.PullGlobalSignedWord(0x70ea) - 14, 0, 49));
				}
				else if ((short)this.owner.GlobDb3e < 0x48)
				{
					ShowPalace();
				}
				else if (this.processor.PullLocalWord(0x32) == 0x80)
				{
					return HumanOrder.EndOfTurn;
				}

				this.owner.GlobDb3a = 0;
			}

			if (this.owner.GlobDb3a == 2 && (short)this.processor.PullLocalWord(0x32) < 0x80)
				TakeHeldClick(participantIdx);

			if (this.owner.GlobDb3a == 2 &&
				(((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] & (1 << (person & 0xff))) != 0 ||
					this.processor.PullGlobalWord(0xd806) != 0))
			{
				// The Civilopedia of the terrain of a seen tile
				this.owner.Startup.ConcealMouseCursor();

				this.owner.MapOps.ObtainTerrainType(
					this.processor.PullLocalSignedWord(0x1c),
					this.processor.PullLocalSignedWord(0x26));

				this.owner.Encyclopedia.DisplayCivilopediaRecord(this.processor.RegAcc.Value16, 3);

				this.owner.TurnHooks.RepaintMainScreen();

				this.owner.Startup.DisplayMouseCursor();
			}

			return TakeClick(participantIdx);
		}

		/// <summary>The tile clicked on the map view: 0x1c / 0x26</summary>
		private void LocateClickedTile()
		{
			this.owner.Routing.TunePosition(
				(((short)this.owner.GlobDb3c - 80) / 16) + this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)(this.owner.GlobDb3e - 8);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 + this.processor.PullGlobalWord(0xd75e));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
		}

		/// <summary>The palace of the human player, until a key or a click</summary>
		private void ShowPalace()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Throne.ImportPalaceGraphics(0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Helpers.HaltHueCycling();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1e79, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
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

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			this.owner.Helpers.MergeCoordinates(
				(short)(this.processor.PullLocalSignedWord(0x1c) - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz)),
				(short)(this.processor.PullLocalSignedWord(0x26) - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

			// Across the date line
			if (this.owner.Libc.AbsoluteValue((short)(this.processor.PullLocalWord(0x1c) - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz))) == 0x4f &&
				this.owner.Libc.AbsoluteValue((short)(this.processor.PullLocalWord(0x26) - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))) <= 1)
			{
				this.processor.EmitLocalWord(0x18, 1);
			}

			if (this.processor.PullLocalWord(0x18) != 1)
			{
				this.owner.GlobDb3a = 2;
				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.processor.PullLocalByte(0x1c));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.processor.PullLocalByte(0x26));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.owner.GlobDb3a = 0;
			this.processor.EmitLocalWord(0x36, 0);

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));
		}

		/// <summary>
		/// A click (GlobDb3a 1) on the tile 0x1c / 0x26, see TakeHumanOrder; then 0x44 is -1 and the keys wait unless 0xc.
		/// </summary>
		private HumanOrder TakeClick(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.processor.PullLocalWord(0x1c);
			short vertLoc = (short)this.processor.PullLocalWord(0x26);

			if (this.owner.GlobDb3a == 1)
			{
				if (this.processor.PullLocalWord(0x36) != 0 && (short)this.processor.PullLocalWord(0x32) < 0x80)
				{
					// The GoTo pointer: a destination the unit may reach (any for 2)
					short troopIdx = (short)this.processor.PullLocalWord(0x32);
					this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));
					bool ocean = this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain;
					bool reachable;

					if (this.processor.PullLocalWord(0x36) == 2)
						reachable = true;
					else if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] & (1 << (person & 0xff))) == 0)
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
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.processor.PullLocalByte(0x1c));
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.processor.PullLocalByte(0x26));
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
					}

					this.processor.EmitLocalWord(0x36, 0);

					this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

					ForgetMarkedTile(participantIdx);
				}
				else if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 &&
					(this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) == (ushort)person || this.processor.PullGlobalWord(0xd806) != 0))
				{
					// A city of the human (any with everything shown): the city screen
					this.processor.EmitLocalWord(0x48, this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc));
					this.processor.EmitLocalWord(0x3c, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));

					this.owner.Advisors.RenewCityScreen(this.processor.PullLocalSignedWord(0x3c));

					this.owner.TurnHooks.RenderStatusBar();

					this.processor.EmitLocalWord(0x1e, 0);
					ForgetMarkedTile(participantIdx);
				}
				else
				{
					this.processor.EmitLocalWord(0x44, this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc));

					if (this.processor.PullGlobalWord(0xd806) != 0 && this.processor.PullLocalWord(0x44) != 0xffff)
					{
						this.owner.ArmyPicker.DisplayUnitInfo(
							this.processor.PullGlobalSignedWord(0xd7f0),
							this.processor.PullLocalSignedWord(0x44));
					}

					if (this.processor.PullLocalWord(0x44) != 0xffff && participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
					{
						// An own unit (from its stack): it is the unit now, unless sentried or fortified
						this.processor.EmitLocalWord(0x3a, this.owner.UnitOps.DisplayBattleScreen(this.processor.PullGlobalSignedWord(0xd7f0),
							this.processor.PullLocalSignedWord(0x44)));

						short chosen = (short)this.processor.PullLocalWord(0x3a);
						this.processor.RegBase.Value16 = (ushort)(chosen * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

						if ((this.troops.Pull(participantIdx, chosen, UnitMemory.Status) & 9) == 0)
						{
							this.processor.EmitLocalWord(0x32, this.processor.PullLocalWord(0x3a));
							this.processor.EmitLocalWord(0x46, (ushort)((this.processor.PullLocalWord(0x3a) - 1) & 0x7f));
							this.processor.EmitLocalWord(0x2e, 0);
							this.processor.EmitLocalWord(0x1e, 0);
							ForgetMarkedTile(participantIdx);

							return HumanOrder.NextLook;
						}
					}
					else
					{
						this.owner.MapOps.FocusMapView(participantIdx,
							this.processor.PullLocalSignedWord(0x1c) - 7,
							this.processor.PullLocalSignedWord(0x26) - 6);
					}
				}
			}

			this.processor.EmitLocalWord(0x44, 0xffff);

			if (this.processor.PullLocalWord(0xc) != 0)
				this.processor.EmitLocalWord(0xc, 0);
			else
				AwaitForKey();

			return HumanOrder.Order;
		}

		/// <summary>The marked tile (0x4, at 0xe / 0x12) is forgotten and repainted when the player sees it</summary>
		private void ForgetMarkedTile(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x4) == 0)
				return;

			this.processor.EmitLocalWord(0x4, 0);

			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), this.processor.PullLocalWord(0x12)] & (1 << (byte)participantIdx)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullLocalSignedWord(0xe),
					this.processor.PullLocalSignedWord(0x12));
			}

			this.processor.EmitLocalWord(0xe, 0xffff);
		}

		/// <summary>
		/// Before an order: a unit with a destination (or no unit, 0x32 from 128) goes on with GoTo (CarryOutGoto gives the
		/// order 0x44), else it forgets its heading; 0x2c / 0x30 are its place (0 without a unit). The locals of HandlePlayerTurn
		/// are used as in the original, SI as the original leaves it.
		/// </summary>
		private void PrepareOrder(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);

			if (this.processor.PullLocalWord(0x32) > 127 || this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				this.processor.EmitLocalWord(0x44, this.owner.Routing.CarryOutGoto(participantIdx, troopIdx));
			}
			else
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + this.processor.PullLocalWord(0x32) * UnitMemory.TroopStride);

			if (this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally)
			{
				this.processor.EmitLocalWord(0x2c, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz));
				this.processor.EmitLocalWord(0x30, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
			}
			else
			{
				this.processor.EmitLocalWord(0x2c, 0);
				this.processor.EmitLocalWord(0x30, 0);
			}
		}

		/// <summary>'D': the unit is disbanded</summary>
		private void DisbandUnit(short participantIdx)
		{
			this.troops.Assign(participantIdx, (short)this.processor.PullLocalWord(0x32), UnitMemory.LeftoverSteps, 0);

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
		}

		/// <summary>
		/// 'P': a land unit (not a diplomat, caravan or fighter) outside a city with moves left pillages the tile: irrigation and
		/// mine, else a railroad, else a road go (a human seen tile is remembered, the treaty with the owner of the nearest city
		/// may break and he gets the place as a strategic location); else the help message. The locals of HandlePlayerTurn are
		/// used as in the original.
		/// </summary>
		private void PillageTile(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);

			bool may = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			if (may)
			{
				this.processor.EmitLocalByte(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));
				may = (sbyte)this.processor.PullLocalByte(0x4e) < 0x1a && this.processor.PullLocalByte(0x4e) != 0xe;
			}

			if (!may)
			{
				DisplayHelpForHuman(0x1e83);
				return;
			}

			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc));

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
				return;

			// (no when the human keeps the treaty)
			if (this.owner.Combat.AffirmBreakTreaty(this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx) == 0xffff)
				return;

			this.processor.EmitLocalWord(0x3a, this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc));

			if ((this.processor.PullLocalByte(0x3a) & 6) != 0)
				this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
			else if ((this.processor.PullLocalByte(0x3a) & 0x10) != 0)
				this.owner.MapOps.DiscardTileImprovements(0x10, horizLoc, vertLoc);
			else
				this.owner.MapOps.DiscardTileImprovements(8, horizLoc, vertLoc);

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

			RepaintTileIfVisible(this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30));

			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx,
				this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx,
				2);

			// (the player of the strategic location is DI as the original leaves it)
			if (participantIdx != this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx)
				this.owner.Bot.SendUnitToStrategicLocation((short)this.processor.RegDest.Value16, horizLoc, vertLoc, 1, 4);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>Space: the unit does nothing this turn</summary>
		private void SkipUnit(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>'s': the unit is sentried</summary>
		private void SentryUnit(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.EmitLocalByte(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if (UnitKindTable.Field(this.processor, (sbyte)this.processor.PullLocalByte(0x4e), UnitKindTable.FerryRoom) == 0 &&
				this.processor.PullLocalByte(0x4e) != UnitKindTable.Carrier)
			{
				return;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return;

			this.owner.UnitOps.VisitEachUnitInStack(participantIdx, troopIdx, 0x14f6);

			this.processor.EmitLocalWord(0x1e, 0);
			this.processor.EmitLocalWord(0x28, 1);

			this.processor.EmitLocalWord(0x32, (ushort)(LocateBestGuardian(participantIdx, this.processor.PullLocalWord(0x32)) - 1));
			this.processor.EmitLocalWord(0x46, (ushort)(this.processor.PullLocalWord(0x32) & 0x7f));
			this.processor.EmitLocalWord(0x2e, 0);
		}

		/// <summary>
		/// 'g': GoTo on (0x36), with the square cursor on the unit (0x4 at 0xe / 0x12), or off, the cursor removed; with a
		/// mouse its pointer changes. The original game showed the square cursor (moved with the arrow keys, Enter picks the
		/// destination) only when there was no mouse, with a mouse it only changed the mouse pointer, which was easy to miss.
		/// The square cursor is now shown in both cases, so the destination can be picked with the keyboard or the mouse.
		/// </summary>
		private void ToggleGoTo(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x36) == 0)
			{
				if (this.processor.PullLocalWord(0x4) == 0)
				{
					this.processor.EmitLocalWord(0x4, 0x1);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * 0x600 + this.processor.PullLocalWord(0x32) * 0xc);
					this.processor.EmitLocalWord(0xe, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));
					this.processor.EmitLocalWord(0x12, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
				}
			}
			else if (this.processor.PullLocalWord(0x4) != 0)
			{
				// GoTo cancelled, remove the square cursor
				this.processor.EmitLocalWord(0x4, 0x0);

				if ((this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
					this.processor.PullLocalWord(0x12)] & (1 << participantIdx)) != 0)
				{
					this.owner.MapOps.RepaintMapTile(
						this.processor.PullLocalSignedWord(0xe),
						this.processor.PullLocalSignedWord(0x12));
				}

				this.processor.EmitLocalWord(0xe, 0xffff);
			}

			if (this.processor.PullGlobalWord(0x1a3c) != 0)
			{
				// GoTo mouse pointer, or the normal one when GoTo is cancelled
				this.processor.RegBase.Value16 = (ushort)((this.processor.PullLocalWord(0x36) != 0 ? 7 : 2) << 1);
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4ce)));
			}

			this.processor.EmitLocalWord(0x36, (ushort)(this.processor.PullLocalWord(0x36) ^ 1));
		}

		/// <summary>
		/// 'h': in a city (distance 0 to the nearest, 0x10) it becomes the home city of the unit; a plane elsewhere flies to the
		/// nearest own city (true: status bit 1, the move follows), without one a human plane forgets that order and a computer
		/// plane is lost. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool OrderHome(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);

			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30)));

			if (this.processor.PullGlobalWord(0x6c9a) == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.PullLocalByte(0x10));

				return false;
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
				return false;

			this.processor.EmitLocalWord(0x24, this.owner.UnitOps.LocateNearestFriendlyCity(participantIdx, this.processor.PullLocalSignedWord(0x32)));

			if (this.processor.PullLocalWord(0x24) != 0)
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

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

			return false;
		}

		/// <summary>
		/// 'p': settlers clean up pollution (status bits 1 and 7, the work counter in byte 5): after 4 turns the pollution is
		/// gone; on a tile without pollution the order is forgotten (and the view moves to the unit). Other units: the help
		/// message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void CleanPollution(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1e8c);
				return;
			}

			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30)));

			this.processor.RegSource.Value16 = unit;

			if ((this.processor.PullLocalByte(0x40) & 0x40) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x7d));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx,
					this.processor.PullLocalSignedWord(0x2c),
					this.processor.PullLocalSignedWord(0x30));

				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0x82));

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegAcc.LowByte = 0xff;

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) < 4)
				return;

			this.owner.MapOps.DiscardTileImprovements(0x40,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			owner.World.DirtyCellTally--;
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = unit;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 4));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

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

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0x13);
			short work = (short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)) + 4);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegCount.Value16 = (ushort)work;

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.owner.MapOps.AssignTileImprovements(0x20, horizLoc, vertLoc);

			this.owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, this.processor.PullLocalSignedWord(0x32), 1);

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>
		/// 'r': settlers build a road (status bit 1, the work counter in byte 5 up to 2 – 4 for a railroad – times the
		/// byte 0x28e of the terrain), a railroad on a road with Railroad (0x31) outside cities; not on a railroad, not on a
		/// river (0xb) without Bridge Building (0x13); refused: the order is forgotten (and the view moves to the unit). Other
		/// units: the help message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BuildRoad(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1e96);
				return;
			}

			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc));
			byte improvements = this.processor.PullLocalByte(0x40);
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

			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));

			if (this.processor.PullLocalWord(0x3e) == 0xb &&
				(this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13)) == 0)
			{
				return;
			}

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			short factor = (short)((this.processor.PullLocalByte(0x40) & 8) != 0 ? 4 : 2);
			short worth = TerrainWorkByte(0x28e);
			this.processor.RegBase.Value16 = (ushort)worth;
			short work = (short)(factor * worth);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegCount.Value16 = (ushort)work;

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.owner.MapOps.AssignTileImprovements(
				(ushort)(((this.processor.PullLocalByte(0x40) & 8) != 0) ? 0x10 : 8),
				horizLoc, vertLoc);

			this.owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, this.processor.PullLocalSignedWord(0x32), 1);

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>A signed byte of the terrain 0x3e in the table of terrains (0x13 bytes each); BX as the original leaves it</summary>
		private short TerrainWorkByte(ushort offset)
		{
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0x13);

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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

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

			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0xc);
			this.processor.EmitLocalWord(0x3a, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)));

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1eaa);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (this.processor.PullLocalWord(0x3a) == 0xfffe && VerifyCoastalTile(horizLoc, vertLoc) == 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1eb1);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x40, 0x44c))
				return;

			if ((short)this.processor.PullLocalWord(0x3a) >= 0)
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

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

			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0xc);
			this.processor.EmitLocalWord(0x3a, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e)));

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1ec4);
				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x80, 0x450))
				return;

			if ((short)this.processor.PullLocalWord(0x3a) >= 0)
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

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.processor.RegAcc.LowByte = 0xff;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0xc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			return (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + offset)) <=
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps);
		}

		/// <summary>The terrain of the tile becomes 0x3a (seen by the human player), irrigation and mine go</summary>
		private void ChangeTerrain(short horizLoc, short vertLoc)
		{
			this.owner.Services.AssignDot(2, horizLoc, vertLoc, this.processor.PullLocalWord(0x3a));
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30)] |= 1;
			this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
		}

		/// <summary>The work is done: the unit is shown, its status loses the bit (mask) and the counter is 0</summary>
		private void FinishTileWork(short participantIdx, short troopIdx, ushort unit, byte mask)
		{
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

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

			switch (this.processor.PullLocalWord(0x44))
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
					this.processor.EmitLocalWord(0x28, 0x1);
					this.processor.EmitLocalWord(0x2e, 0x1);
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
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
				this.processor.EmitLocalWord(0x10, this.owner.MapOps.LocateCityAtTile(horizLoc, vertLoc));

				if ((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent >= 0xa)
				{
					DisplayHelpForHuman(0x1ed6);
					return;
				}

				this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent++;
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.ScaleIndex(0x600, (ushort)participantIdx);
				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

				RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

				return;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.processor.EmitLocalWord(0x3a, this.owner.Founding.EstablishCity(participantIdx,
				this.processor.PullLocalWord(0x24),
				horizLoc, vertLoc,
				1));

			if (this.processor.PullLocalWord(0x3a) != 0xffff)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

				RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
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
			if (this.processor.PullLocalWord(0x4) != 0)
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					this.processor.PullLocalSignedWord(0xe) - 7,
					this.processor.PullLocalSignedWord(0x12) - 6);
			}
			else
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					this.processor.PullLocalSignedWord(0x2c) - 7,
					this.processor.PullLocalSignedWord(0x30) - 6);
			}
		}

		/// <summary>
		/// The marked tile (0xe / 0x12) moves one step in the direction 0x24 (repainted where it was when the player sees it,
		/// kept off the poles); off the map view the view moves. SI as the original leaves it.
		/// </summary>
		private void MoveMarkedTile(short participantIdx)
		{
			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), this.processor.PullLocalWord(0x12)] & (1 << (byte)participantIdx)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullLocalSignedWord(0xe),
					this.processor.PullLocalSignedWord(0x12));
			}

			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x24) << 1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0xe));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0x12),
				0, 49);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			if (VerifyPositionOnScreen(this.processor.PullLocalSignedWord(0xe), this.processor.PullLocalSignedWord(0x12)) == 0)
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					this.processor.PullLocalSignedWord(0xe) - 8,
					this.processor.PullLocalSignedWord(0x12) - 7);
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

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.Kind);
			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kind == UnitKindTable.Diplomat)
				return false;

			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc));

			if ((this.processor.PullLocalByte(0x40) & 1) != 0 || (this.processor.PullLocalByte(0x40) & 6) == 0 || kind == 3 || kind == 7)
				return false;

			if (this.owner.World.Towns[this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				return false;

			this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);

			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30)] & (1 << (this.owner.World.PersonParticipantIdx & 0xff))) != 0)
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x24) << 1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x2c));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0x30),
				0, 49);

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x34, this.owner.MapOps.ObtainTerrainType(this.processor.PullLocalSignedWord(0x2c), this.processor.PullLocalSignedWord(0x30)));
			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));
			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));
			this.processor.EmitLocalWord(0x18, this.owner.MapOps.ObtainUnitAtTile(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));

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
			if (this.processor.PullLocalWord(0x18) != 0xffff ||
				this.owner.UnitOps.MayOccupyTile(participantIdx, this.processor.PullLocalSignedWord(0x2c), this.processor.PullLocalSignedWord(0x30)) == 0 ||
				this.owner.UnitOps.MayOccupyTile(participantIdx, this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) == 0)
			{
				return false;
			}

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain)
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat)
				return MoveStep.GoesOn;

			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateCityAtPosition(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));

			if (this.processor.PullLocalWord(0x10) == 0xffff)
				return MoveStep.NoCity;

			Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x10)];
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx != participantIdx && this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain)
				return MoveStep.Done;

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);
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
							this.processor.PullLocalSignedWord(0x32),
							this.processor.PullLocalWord(0x24));
					}

					this.owner.Diplomats.DiplomatArrivesAtCity(this.processor.PullLocalSignedWord(0x10),
						participantIdx,
						this.processor.PullLocalSignedWord(0x32));

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

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x1c),
				this.owner.World.Towns[homeIdx].Place.X,
				this.processor.PullLocalSignedWord(0x26),
				this.owner.World.Towns[homeIdx].Place.Y);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx >= -24 && (short)this.processor.PullLocalWord(0x3a) < 10)
			{
				// Near its home city, building no wonder: on the same continent it just moves in
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(home * 0x1c);

				this.processor.RegDest.Value16 = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[homeIdx].Place.X,
					this.owner.World.Towns[homeIdx].Place.Y);

				if (this.owner.MapOps.ObtainContinentID(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) == this.processor.RegDest.Value16)
					return MoveStep.GoesOn;
			}

			this.owner.Libc.CopyText(0xba06, "Will you?\n Keep moving\n Establish trade route\n");
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx < -24)
			{
				this.owner.Libc.AppendText(0xba06, " Help build WONDER.\n");

				if ((short)this.processor.PullLocalWord(0x3a) < 10)
					this.processor.EmitGlobalWord(0xb276, 0x2);
			}

			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx == participantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			}
			else
			{
				this.processor.EmitLocalWord(0x3a, 0x1);
			}

			if (this.processor.PullLocalWord(0x3a) == 1 &&
				this.owner.Conquest.FoundTradeRoute(participantIdx, this.processor.PullLocalSignedWord(0x32), this.processor.PullLocalSignedWord(0x10)) != 0)
			{
				return MoveStep.Done;
			}

			if (this.processor.PullLocalWord(0x3a) != 2)
				return MoveStep.GoesOn;

			// The caravan helps to build the wonder: 10 times its price
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
			this.processor.RegCount.Value16 = (ushort)(10 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);
			this.owner.World.Towns[this.processor.PullLocalWord(0x10)].MaterialsTally += (short)this.processor.RegCount.Value16;

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat || this.processor.PullLocalWord(0x18) == 0xffff ||
				participantIdx == this.processor.PullGlobalSignedWord(0xd20a))
			{
				return;
			}

			this.owner.Diplomats.BuyUnit(this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18),
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short owner = this.processor.PullGlobalSignedWord(0xd20a);

			if (this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// A land unit boards an own ship with room
					if (participantIdx != this.processor.PullGlobalSignedWord(0xd20a) || this.processor.PullLocalWord(0x18) == 0xffff ||
						(short)this.owner.UnitOps.TallySpecialUnitsInStack(participantIdx, this.processor.PullLocalSignedWord(0x18)) <= 0)
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

			if (this.processor.PullLocalWord(0x18) == 0xffff || participantIdx == this.processor.PullGlobalSignedWord(0xd20a))
				return MoveCheck.Moves;

			if (this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain)
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

			if (this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Submarine)
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
			short guardian = (short)this.processor.PullLocalWord(0x18);
			this.processor.RegDest.Value16 = (ushort)(guardian * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(owner, guardian) * UnitKindTable.Stride);

			if (this.troops.KindField(owner, guardian, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				this.processor.RegSource.Value16 = unit;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xe && (this.processor.PullLocalByte(0x40) & 1) == 0)
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 3)
				return true;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Libc.CopyText(0xba06, "Attack at\n");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 10));
				this.owner.Libc.AppendText(0xba06, "/3 strength?\n Cancel\n Attack\n");
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 16);

				if (this.processor.RegAcc.Value16 == 1)
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x1c);
			short vertLoc = (short)this.processor.PullLocalWord(0x26);

			this.processor.RegAcc.Value16 = (ushort)person;

			if (this.processor.PullGlobalWord(0xd20a) == (ushort)person)
				this.owner.UnitOps.PanMapToPosition(person, horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x18, this.owner.UnitOps.LocateBestGuardianInStack(this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18)));

			this.processor.RegAcc.Value16 = (ushort)person;

			if (participantIdx == person || this.processor.PullGlobalWord(0xd20a) == (ushort)person)
				this.processor.EmitGlobalWord(0x70d8, 0x1);

			this.processor.EmitLocalWord(0x3a, this.owner.Combat.SettleBattle(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18),
				1));

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.processor.EmitGlobalWord(0x70d8, 0x0);

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
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

			if ((this.processor.PullLocalByte(0x40) & 1) == 0)
			{
				if ((this.processor.PullLocalByte(0x40) & 0x20) == 0)
				{
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				}

				return false;
			}

			// The attacked city
			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));
			Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x10)];
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx != person)
				town.ConditionMarker |= 0x10;

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff && (town.StructureMarkers0 & 0x80) == 0 &&
				this.processor.PullLocalWord(0x34) != MapRules.OceanTerrain &&
				(this.owner.World.HardnessTier != 0 || this.processor.PullGlobalWord(0xd20a) != (ushort)person))
			{
				town.TrueExtent--;
			}

			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.TrueExtent == 0)
			{
				// The city is destroyed
				this.owner.Advisors.FinalizeProduction(
					this.processor.PullLocalSignedWord(0x10),
					this.processor.PullLocalWord(0x1c),
					this.processor.PullLocalWord(0x26));

				this.owner.NewGameWizard.EliminateCivilization(this.processor.PullGlobalSignedWord(0xd20a), participantIdx);

				if (participantIdx == person)
				{
					this.processor.EmitLocalWord(0x18, this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc));

					if (this.processor.PullLocalWord(0x18) != 0xffff)
					{
						short owner = this.processor.PullGlobalSignedWord(0xd20a);
						short left = (short)this.processor.PullLocalWord(0x18);
						this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
						this.processor.RegSource.Value16 = (ushort)(left * UnitMemory.TroopStride);
						this.troops.Assign(owner, left, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(owner, left, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));
					}
				}
			}

			this.processor.RegAcc.Value16 = (ushort)person;

			if (participantIdx == person || this.processor.PullGlobalWord(0xd20a) == (ushort)person || this.processor.PullGlobalWord(0xd806) != 0)
			{
				// The human sees the new size
				this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);
				this.processor.RegAcc.LowByte = (byte)town.TrueExtent;
				town.ShownExtent = (sbyte)this.processor.RegAcc.LowByte;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if (this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) == 0xffff && this.processor.PullGlobalWord(0xd20a) != (ushort)person)
			{
				this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);

				this.owner.Bot.RefreshCityProduction(this.processor.PullGlobalSignedWord(0xd20a), this.processor.RegAcc.Value16);
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short kindOffset = (short)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain && (this.processor.PullLocalByte(0x40) & 1) == 0)
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

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits && (this.processor.PullLocalByte(0x40) & 1) != 0 &&
				this.owner.MapOps.ObtainTileOwnerBits(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) != (ushort)participantIdx)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x19)
				{
					DisplayHelpForHuman(0x1f62);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					return false;
				}

				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

				this.owner.Combat.ExplodeNuclearWeapon(participantIdx,
					this.processor.PullLocalSignedWord(0x1c),
					this.processor.PullLocalSignedWord(0x26));

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
				short horizLoc = (short)this.processor.PullLocalWord(0x2c);
				short vertLoc = (short)this.processor.PullLocalWord(0x30);

				if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & this.processor.PullLocalByte(0x40) & 8) != 0 &&
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
					this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0x13);
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				this.processor.RegSource.Value16 = unit;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHeading) != 0xff &&
					(ushort)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHeading) ^ 4) == this.processor.PullLocalWord(0x24) &&
					(short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 2) <= 2)
				{
					this.processor.RegAcc.LowByte = 0xff;
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHeading), 0xff);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.LeftoverSteps), 0);

					return false;
				}

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x24);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, this.processor.PullLocalByte(0x24));
			}

			if (this.processor.PullLocalWord(0x18) == 0xffff || participantIdx != this.processor.PullGlobalSignedWord(0xd20a) || participantIdx == person ||
				(this.processor.PullLocalByte(0x40) & 1) != 0)
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

			this.processor.EmitLocalWord(0x3a, this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x18), 2));
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegAcc.Value16 = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0 ? 4 : 2);

			return (short)this.processor.RegAcc.Value16 > (short)this.processor.PullLocalWord(0x3a);
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short person = this.owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.processor.PullLocalWord(0x1c);
			short vertLoc = (short)this.processor.PullLocalWord(0x26);

			this.owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			if (this.processor.PullLocalWord(0x2) == 0 || (short)this.processor.PullLocalWord(0x2) >= 8)
				return true;

			short owner = (short)this.processor.PullLocalWord(0x2);

			if (this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain && this.processor.PullLocalWord(0x34) != MapRules.OceanTerrain &&
				MayTreatyStop(participantIdx, troopIdx, unit, owner, person))
			{
				if (participantIdx == person)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

					if (this.owner.Combat.AffirmBreakTreaty(this.processor.PullLocalSignedWord(0x2)) == 0xffff)
						return false;

					this.owner.Politics.WipeDiplomacyFlags(participantIdx, this.processor.PullLocalSignedWord(0x2), 2);
				}
				else if (this.processor.PullLocalWord(0x2) == (ushort)person)
				{
					this.processor.RegSource.Value16 = (ushort)(owner << 4);
					this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);

					if ((this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[participantIdx] & 2) != 0)
					{
						this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

						return false;
					}
				}
			}

			if (participantIdx != (short)this.processor.PullLocalWord(0x2) && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0xe) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = (byte)(1 << (this.processor.PullLocalByte(0x2) & 0x1f));
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

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || (this.processor.PullLocalByte(0x40) & 0xf) == 0 ||
				owner == participantIdx || (participantIdx != person && owner != person))
			{
				return false;
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.owner.MapOps.ObtainTileOwner((short)this.processor.PullLocalWord(0x1c), (short)this.processor.PullLocalWord(0x26)) != (ushort)participantIdx;
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			short person = this.owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			this.processor.EmitLocalWord(0x48, this.owner.MapOps.ObtainTileOwnerBits(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));

			if (participantIdx == 0 && this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = (ushort)personBit;

				if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30)] & personBit) != 0)
				{
					this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
					this.processor.RegDest.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.ShownToParticipants);

					if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
						this.owner.World.Towns[this.owner.Helpers.LocateNearestCity(this.processor.PullLocalSignedWord(0x2c), this.processor.PullLocalSignedWord(0x30))].ParticipantIdx == person)
					{
						this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30)];
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));
					}
				}
			}

			if ((this.processor.PullLocalByte(0x40) & 1) != 0 && this.processor.PullLocalWord(0x48) != (ushort)participantIdx &&
				((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] & personBit) != 0)
			{
				// Into a foreign city the human sees: with an embassy
				this.processor.RegSource.Value16 = (ushort)(person << 4);
				this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);
				bool embassy = (this.owner.World.Participants[person].Relations[participantIdx] & 0x40) != 0;

				if (!embassy)
				{
					this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x48) << 1);
					embassy = (this.owner.World.Participants[person].Relations[this.processor.PullLocalWord(0x48)] & 0x40) != 0;
				}

				if (embassy)
				{
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
						(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | personBit));
				}
			}

			if (this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain &&
				(this.owner.MapOps.ObtainTileImprovements(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) & 2) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (byte)(1 << (this.processor.PullLocalByte(0x48) & 0x1f))));
			}

			// The move on the screen
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 && participantIdx != person &&
				(this.processor.PullGlobalWord(0xd806) == 0 || participantIdx == 0))
			{
				return;
			}

			if ((this.owner.World.SessionPrefMarkers & 0x20) != 0 || participantIdx == person)
			{
				this.owner.UnitOps.PanMapToPosition(person,
					this.processor.PullLocalSignedWord(0x1c),
					this.processor.PullLocalSignedWord(0x26));

				this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
					this.processor.PullLocalSignedWord(0x32),
					this.processor.PullLocalWord(0x24));
			}
			else
			{
				this.owner.MapOps.RenderMapTile(this.processor.PullLocalSignedWord(0x2c), this.processor.PullLocalSignedWord(0x30));
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short person = this.owner.World.PersonParticipantIdx;

			this.processor.RegSource.Value16 = unit;
			this.processor.EmitLocalWord(0x14, (ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.FollowingInStack));

			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.EmitLocalWord(0x22, this.processor.PullLocalWord(0x2c));
			this.processor.EmitLocalWord(0x2a, this.processor.PullLocalWord(0x30));

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Horiz), this.processor.PullLocalByte(0x1c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Vert), this.processor.PullLocalByte(0x26));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.ShownToParticipants), 0);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);

			if (participantIdx != person)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x30));
			}

			if (this.processor.PullLocalWord(0x2) != 0 && (short)this.processor.PullLocalWord(0x2) < 8 && this.processor.PullLocalWord(0x2) != (ushort)participantIdx)
			{
				this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x1c) + 80,
					this.processor.PullLocalSignedWord(0x26),
					0);
			}

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == this.processor.PullLocalWord(0x1c) &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) == this.processor.PullLocalWord(0x26))
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			ushort player = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain && this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = player;
			this.processor.EmitLocalByte(0x52, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if ((UnitKindTable.Field(this.processor, (sbyte)this.processor.PullLocalByte(0x52), UnitKindTable.FerryRoom) == 0 &&
				this.processor.PullLocalByte(0x52) != UnitKindTable.Carrier) || this.processor.PullLocalWord(0x14) == 0xffff)
			{
				return;
			}

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x4a, 0);
			this.processor.EmitLocalWord(0x3c, 0);

			while (true)
			{
				short member = (short)this.processor.PullLocalWord(0x14);
				this.processor.RegSource.Value16 = player;
				this.processor.RegDest.Value16 = (ushort)(member * UnitMemory.TroopStride + player);
				this.processor.EmitLocalWord(0x4c, (ushort)this.troops.PullSigned(participantIdx, member, UnitMemory.FollowingInStack));

				// A transport takes land units, a carrier (no room) planes
				ushort cargoGroup = (ushort)(this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ? 1 : 0);
				this.processor.RegCount.Value16 = cargoGroup;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, member) * UnitKindTable.Stride);

				if (cargoGroup == this.troops.KindField(participantIdx, member, UnitKindTable.LandGroup))
				{
					byte status = this.troops.Pull(participantIdx, member, UnitMemory.Status);
					bool taken = (status & 1) != 0 || this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain;

					if (!taken && participantIdx != this.owner.World.PersonParticipantIdx && (status & 0xc) == 0)
					{
						this.processor.RegBase.Value16 = (ushort)(member * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = player;
						taken = this.troops.Pull(participantIdx, member, UnitMemory.FollowingInStack) != 0xff;
					}

					if (taken)
						CarryUnit(participantIdx, troopIdx, member, player);
				}

				this.processor.EmitLocalWord(0x14, this.processor.PullLocalWord(0x4c));
				this.processor.EmitLocalWord(0x4a, (ushort)(this.processor.PullLocalWord(0x4a) + 1));

				if ((short)this.processor.PullLocalWord(0x4a) >= 0x14 || this.processor.PullLocalWord(0x14) == 0xffff)
					return;

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = player;
				this.processor.EmitLocalByte(0x52, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));
				this.processor.RegBase.Value16 = (ushort)((sbyte)this.processor.PullLocalByte(0x52) * UnitKindTable.Stride);
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3c);

				if ((short)UnitKindTable.Field(this.processor, (sbyte)this.processor.PullLocalByte(0x52), UnitKindTable.FerryRoom) <= (short)this.processor.PullLocalWord(0x3c) &&
					this.processor.PullLocalByte(0x52) != UnitKindTable.Carrier && participantIdx != 0)
				{
					return;
				}
			}
		}

		/// <summary>One unit of the cargo moves along with the ship (see TakeCargoAlong)</summary>
		private void CarryUnit(short participantIdx, short troopIdx, short member, ushort player)
		{
			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x22),
				this.processor.PullLocalSignedWord(0x2a));

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.RegSource.Value16 = (ushort)(player + member * UnitMemory.TroopStride);
			this.troops.Assign(participantIdx, member, UnitMemory.Horiz, this.processor.PullLocalByte(0x1c));
			this.troops.Assign(participantIdx, member, UnitMemory.Vert, this.processor.PullLocalByte(0x26));
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

			this.processor.EmitLocalWord(0x3c, (ushort)(this.processor.PullLocalWord(0x3c) + 1));
		}

		/// <summary>
		/// The unit arrived at 0x1c / 0x26: a foreign city there is seized (the unit shown to its player), the tile belongs to
		/// the player, the unit looks around (ShiftUnit), a hut is visited, a unit (not a ship) at sea wakes its stack (0x1560),
		/// a barbarian on land the human sees near his city is shown to who sees the tile. SI as the calls leave it. The locals
		/// of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void ArriveAtTile(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			short person = this.owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			if ((this.processor.PullLocalByte(0x40) & 1) != 0 && this.processor.PullLocalWord(0x48) != (ushort)participantIdx)
			{
				// A foreign city is taken
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = (byte)(1 << (this.processor.PullLocalByte(0x48) & 0x1f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));

				this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateCityAtPosition(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));

				this.owner.Conquest.SeizeCity(participantIdx, this.processor.PullLocalSignedWord(0x10), 0);

				this.owner.MapOps.TagTileOwned(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26));
			}

			this.owner.MapOps.MapAssignCityOwner(participantIdx, this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26));

			this.owner.UnitOps.ShiftUnit(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				1);

			if (this.owner.MapOps.ContainsHut(this.processor.PullLocalWord(0x3e), this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) != 0)
			{
				this.owner.UnitOps.VisitHut(participantIdx, this.processor.PullLocalSignedWord(0x32));

				this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] |= 1;
			}

			if (this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain)
			{
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					this.owner.UnitOps.VisitEachUnitInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 0x1560);
			}

			if (participantIdx != 0 || this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain)
				return;

			// A barbarian the human sees near his city
			this.processor.RegSource.Value16 = (ushort)personBit;

			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] & personBit) == 0)
				return;

			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.ShownToParticipants);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
				this.owner.World.Towns[this.owner.Helpers.LocateNearestCity(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26))].ParticipantIdx == person)
			{
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)];
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short person = this.owner.World.PersonParticipantIdx;

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << (person & 0xff))) != 0 || participantIdx == person ||
				(this.processor.PullGlobalWord(0xd806) != 0 && participantIdx != 0))
			{
				if (participantIdx == person || (this.owner.World.SessionPrefMarkers & 0x20) != 0)
				{
					this.owner.UnitOps.PanMapToPosition(person, this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26));
					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullLocalSignedWord(0x32));

					if (participantIdx != person && this.processor.PullGlobalWord(0xb278) != 1)
						this.owner.TextPainter.AwaitTime(30);
				}
				else
				{
					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullLocalSignedWord(0x32));

					if (this.processor.RegAcc.Value16 != 0)
						this.owner.TextPainter.AwaitTime(10);
				}
			}

			this.processor.EmitLocalWord(0xa, 0);

			if ((this.processor.PullLocalByte(0x40) & 1) != 0)
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
			if (this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 23) == 0)
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
			ushort key = this.processor.PullLocalWord(0x44);
			short person = this.owner.World.PersonParticipantIdx;
			bool everythingShown = this.processor.PullGlobalWord(0xd806) != 0;

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
							this.processor.EmitLocalWord(0x3c, (ushort)civ);
							this.owner.RealmInfo.DisplayCivilizationOverview(civ);
						}

						this.processor.EmitLocalWord(0x3c, 8);
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
					this.processor.EmitLocalWord(0x24, ShiftedKeyDirection(key));
					PanViewByDirection();
					break;

				case 0x2000: // Alt + D
					this.processor.EmitGlobalWord(0xd806, (ushort)~this.processor.PullGlobalWord(0xd806));
					this.processor.EmitLocalWord(0x24, 0);
					PanViewByDirection();
					break;

				case 0xd: // Enter
				case 0x20: // Space
					if (this.processor.PullLocalWord(0x32) == 0x80)
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
					this.owner.MapOps.FocusMapView(participantIdx, this.processor.PullGlobalSignedWord(0xd4cc), this.processor.PullGlobalSignedWord(0xd75e));
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();
					this.processor.EmitGlobalWord(0xdcfc, 0);
					this.owner.MapOps.FocusMapView(participantIdx, this.processor.PullGlobalSignedWord(0xd4cc), this.processor.PullGlobalSignedWord(0xd75e));
					break;

				case 0x1300: // Alt + R
					// Three random words of each of 16 records of 0x3a bytes, then the dialog 0x2003
					for (ushort record = 0; record < 0x10; record++)
					{
						this.processor.EmitLocalWord(0x3a, record);
						this.processor.RegAcc.Value16 = 0x3a;
						this.processor.ScaleAccumulatorToSource(record);

						for (ushort word = 0; word < 6; word += 2)
						{
							this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1512 + word),
								(ushort)(this.owner.Libc.Dice.DrawInt(3) - 1));
						}
					}

					this.processor.EmitLocalWord(0x3a, 0x10);
					this.owner.TurnHooks.DisplayDialog(0x2003, 100, 80);
					break;

				case 0x2f00: // Alt + V
					this.owner.World.SessionPrefMarkers ^= 0x10;
					this.owner.Libc.CopyText(0xba06, "Sounds ");
					this.owner.Libc.AppendText(0xba06, (ushort)((this.owner.World.SessionPrefMarkers & 0x10) != 0 ? 0x2020 : 0x2024));
					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
					break;

				case 0x2300: // Alt + H
					this.owner.HelpSystem.DisplayHelpPopup(0x2029);
					this.owner.HelpSystem.DisplayHelpPopup(0x2030);
					break;

				case 0x1000: // Alt + Q
					ConfirmQuit();
					break;

				case 0x1b: // Escape
					if (this.processor.PullGlobalWord(0x1a3c) == 0 && this.processor.PullLocalWord(0x4) != 0)
					{
						this.processor.EmitLocalWord(0x4, 0);
						ForgetMarkedTileOnScreen(participantIdx);
					}
					break;

				case 0x9: // Tab
					if (this.processor.PullGlobalWord(0x1a3c) == 0)
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
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x24) << 1);

			ushort horiz = (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) << 2);
			this.processor.EmitGlobalWord(0xd4cc, (ushort)(this.processor.PullGlobalWord(0xd4cc) + horiz));

			ushort vert = (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) << 2);
			this.processor.EmitGlobalWord(0xd75e, (ushort)(this.processor.PullGlobalWord(0xd75e) + vert));

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));
		}

		/// <summary>
		/// The revolution: after the dialog 0x1f6a, answer 1 makes the government anarchy, with the newspaper and the new
		/// government graphics (SI = 2 * the player, as in the original)
		/// </summary>
		private void StartRevolution(short participantIdx)
		{
			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.BlockText.DisplayTextDialog(0x1f6a);
			this.owner.TurnHooks.DisplayDialog(0xba06, 64, 80);

			if (this.processor.RegAcc.Value16 != 1)
				return;

			this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);
			this.owner.World.Participants[participantIdx].RegimeKind = 0;

			this.owner.Libc.CopyText(0xba06, "The ");
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));
			this.owner.Libc.AppendText(0xba06, " are\nrevolting! Citizens\ndemand new govt.\n");
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

			this.processor.EmitLocalWord(0x3a, (ushort)rate);
			this.processor.EmitGlobalWord(0x2f9a, (ushort)participant.LevyRatio);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
				return;

			participant.ResearchLevyRatio += (short)(participant.LevyRatio - (short)this.processor.PullLocalWord(0x3a));
			participant.LevyRatio = (short)this.processor.PullLocalWord(0x3a);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3a);

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

			this.processor.EmitLocalWord(0x3a, (ushort)rate);
			this.processor.EmitGlobalWord(0x2f9a, (ushort)-(participant.LevyRatio + participant.ResearchLevyRatio - 10));

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
				return;

			participant.ResearchLevyRatio = (short)-(participant.LevyRatio + (short)this.processor.PullLocalWord(0x3a) - 10);
			this.processor.RegAcc.Value16 = (ushort)participant.ResearchLevyRatio;

			this.owner.TurnHooks.RenderStatusBar();
		}

		/// <summary>
		/// The quit: Load Game from the GAME menu (0xdc48) leaves at once, else the dialog; Keep Playing clears 0xdc48, Yes
		/// sets it to 1 (when it was 0); leaving ends the moves of the units (0x32 = 0x80)
		/// </summary>
		private void ConfirmQuit()
		{
			if (this.processor.PullGlobalWord(0xdc48) != GameLoadAndSave.FetchSessionAsk)
			{
				this.owner.Libc.CopyText(0xba06, "Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n");
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				if (this.processor.RegAcc.Value16 != 1)
				{
					this.processor.EmitGlobalWord(0xdc48, 0);
					return;
				}

				if (this.processor.PullGlobalWord(0xdc48) == 0)
					this.processor.EmitGlobalWord(0xdc48, 1);
			}

			this.processor.EmitLocalWord(0x32, 0x80);
		}

		/// <summary>The marked tile (0xe, 0x12) is repainted when the player has seen it, and forgotten (0xe = -1)</summary>
		private void ForgetMarkedTileOnScreen(short participantIdx)
		{
			if ((this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), this.processor.PullLocalWord(0x12)] & (1 << (participantIdx & 0xff))) != 0)
				this.owner.MapOps.RepaintMapTile(this.processor.PullLocalSignedWord(0xe), this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0xe, 0xffff);
		}

		/// <summary>
		/// Tab: the marked tile goes away, or comes on the unit (when it is on the screen; SI the unit, as in the original)
		/// or else in the middle of the view; the tile is repainted when the player has seen it; 0x4 toggles
		/// </summary>
		private void ToggleMarkedTile(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x4) != 0)
			{
				ForgetMarkedTileOnScreen(participantIdx);
			}
			else
			{
				bool onUnit = false;

				if (this.processor.PullLocalWord(0x32) != 0x80)
				{
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + this.processor.PullLocalWord(0x32) * UnitMemory.TroopStride);
					ushort place = (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin);

					if (VerifyPositionOnScreen(
						this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Horiz)),
						this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Vert))) != 0)
					{
						this.processor.EmitLocalWord(0xe, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Horiz)));
						this.processor.EmitLocalWord(0x12, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(place + UnitMemory.Vert)));
						onUnit = true;
					}
				}

				if (!onUnit)
				{
					this.processor.EmitLocalWord(0xe, (ushort)(this.processor.PullGlobalWord(0xd4cc) + 7));
					this.processor.EmitLocalWord(0x12, (ushort)(this.processor.PullGlobalWord(0xd75e) + 6));
				}

				if ((this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), this.processor.PullLocalWord(0x12)] & (1 << (participantIdx & 0xff))) != 0)
					this.owner.MapOps.RepaintMapTile(this.processor.PullLocalSignedWord(0xe), this.processor.PullLocalSignedWord(0x12));
			}

			this.processor.EmitLocalWord(0x4, (ushort)(this.processor.PullLocalWord(0x4) ^ 1));
		}

		/// <summary>The menu of the menu bar; a chosen order (0xd4ca) becomes the order 0x44 of the unit</summary>
		private KeyStep ManageMenu(short participantIdx, short menu)
		{
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), menu);

			if (this.processor.PullGlobalWord(0xd4ca) == 0xffff)
				return KeyStep.NextLook;

			this.processor.EmitLocalWord(0x44, this.processor.PullGlobalWord(0xd4ca));
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
			short troopIdx = (short)this.processor.PullLocalWord(0x32);

			if (troopIdx < UnitMemory.TroopTally)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0 && this.processor.PullLocalWord(0x28) == 0 &&
					this.processor.PullGlobalWord(0xdc48) == 0)
				{
					return true;
				}

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
					return false;

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0)
				{
					this.processor.EmitLocalWord(0x1e, 0);
				}
				else
				{
					BurnPlaneFuel(participantIdx, troopIdx);
					MayLoseTrireme(participantIdx, troopIdx);
				}
			}

			if ((this.owner.World.ParticipantMarkers & (1 << participantIdx)) != 0)
				RenderUnitInfo(participantIdx, this.processor.PullLocalWord(0x32));

			return false;
		}

		/// <summary>
		/// A plane at the end of its move: in a city or on a carrier it gets all its rounds back, then it burns one; below
		/// zero it is lost (help 0x2070)
		/// </summary>
		private void BurnPlaneFuel(short participantIdx, short troopIdx)
		{
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) == 0)
				return;

			this.owner.MapOps.ObtainTileImprovements(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((this.processor.RegAcc.LowByte & 1) != 0 ||
				this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 23) != 0)
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
				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
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

			this.processor.EmitLocalWord(0x38, 0);

			for (ushort direction = 1; direction <= 8; direction++)
			{
				this.processor.EmitLocalWord(0x3c, direction);
				this.processor.RegSource.Value16 = (ushort)(direction << 1);
				this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

				this.owner.MapOps.ObtainTerrainType(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));

				if (this.processor.RegAcc.Value16 != MapRules.OceanTerrain)
				{
					this.processor.EmitLocalWord(0x38, 1);
					break;
				}

				if (direction == 8)
					this.processor.EmitLocalWord(0x3c, 9);
			}

			if (this.processor.PullLocalWord(0x38) != 0)
				return;

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			DisplayHelpForHuman(0x2076);
		}

		/// <summary>
		/// Repaints the tile when the human player has seen it (or everything is shown, 0xd806)
		/// </summary>
		public void RepaintTileIfVisible(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintTileIfVisible({horizLoc}, {vertLoc})");

			if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0 ||
				this.processor.PullGlobalWord(0xd806) != 0)
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
					VerifyCoastalTile(horizLoc, vertLoc);

					if (this.processor.RegAcc.Value16 != 0)
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

			this.owner.Libc.CopyText(0xba06, "Moves: ");

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(moves, 10));

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

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "(");

			this.owner.Libc.AppendText(0xba06, (ushort)((0x13 * this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))) + 0x282));

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			byte improvements = this.processor.RegAcc.LowByte;

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

			this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((this.processor.RegAcc.LowByte & 1) != 0)
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
			if (horizLoc < 16 && this.processor.PullGlobalSignedWord(0xd4cc) >= 65)
			{
				horizLoc += 80;
			}

			if (horizLoc < this.processor.PullGlobalSignedWord(0xd4cc) ||
				horizLoc >= this.processor.PullGlobalSignedWord(0xd4cc) + 15 ||
				vertLoc < this.processor.PullGlobalSignedWord(0xd75e) ||
				vertLoc >= this.processor.PullGlobalSignedWord(0xd75e) + 12)
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
			if (this.processor.PullGlobalSignedWord(0x6b90) == this.owner.World.PersonParticipantIdx)
			{
				this.owner.HelpSystem.DisplayHelpPopupForHuman(charsAddr);
			}
		
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpForHuman");
		}
	}
}
