namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController : GameComponent, ITurnController
	{
		private readonly UnitMemory troops;

		private TurnLocals turn = new TurnLocals();

		private ushort indexDestination;

		public TurnController(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public void HandlePlayerTurn(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"HandlePlayerTurn({participantIndex})");

			TurnLocals outer = this.turn;
			this.turn = new TurnLocals();
			ushort outerDestination = this.indexDestination;

			PrepareTurn(participantIndex);

			do
			{
				MoveUnits(participantIndex);
			}
			while (Turn.GameEndRequest == 0 && participantIndex == this.Owner.World.PersonParticipantIndex &&
				this.Owner.World.IsHuman(participantIndex) && WarnOfDisorder(participantIndex));

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Startup.HideMouseCursor();
				this.Owner.TurnHooks.FillBoxWithShadow(0, 97, 80, Viewport.Bottom - 97);
				this.Owner.Startup.ShowMouseCursor();
			}

			this.indexDestination = outerDestination;
			this.turn = outer;
		}

		private void MoveUnits(short participantIndex)
		{
			bool newPass = true;

			while (true)
			{
				if (newPass)
				{
					this.turn.AllMoved = 1;
					this.turn.Troop = 0;
				}

				bool endOfTurn = false;

				while ((short)this.turn.Troop < UnitMemory.TroopTally)
				{
					if (!VisitUnit(participantIndex))
					{
						endOfTurn = true;
						break;
					}

					this.turn.Troop = (ushort)(this.turn.Troop + 1);
				}

				if (endOfTurn)
				{
					break;
				}

				newPass = true;

				if (this.turn.AllMoved == 0 && Turn.GameEndRequest == 0)
				{
					continue;
				}

				if (Turn.GameEndRequest != 0 || !this.Owner.World.IsHuman(participantIndex) ||
					(this.turn.HumanTookKey != 0 && (this.Owner.World.SessionPreferenceMarkers & 4) == 0))
				{
					break;
				}

				this.turn.Troop = UnitMemory.TroopTally;
				this.turn.AllMoved = 0;
				this.turn.LaterPass = 1;

				if (!LookAtUnit(participantIndex))
				{
					break;
				}

				this.turn.Troop = (ushort)(this.turn.Troop + 1);
				newPass = false;
			}
		}

		private bool VisitUnit(short participantIndex)
		{
			if (Screen.RepaintRequest != 0)
			{
				this.Owner.TurnHooks.RepaintMainScreen();
			}

			if (Turn.GameEndRequest != 0)
			{
				this.turn.Troop = UnitMemory.TroopTally;
				return true;
			}

			if (this.turn.Troop == this.turn.WaitingTroop)
			{
				this.turn.PastWaiting = 1;
			}

			if (!StartUnitMove(participantIndex))
			{
				return true;
			}

			if (!FocusOnUnit(participantIndex))
			{
				return true;
			}

			return LookAtUnit(participantIndex);
		}

		private bool LookAtUnit(short participantIndex)
		{
			if (this.Owner.World.IsHuman(participantIndex))
			{
				WaitForKey();
			}

			this.turn.ComputerTries = 0;

			if (this.turn.MouseCursorSet != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);
			}

			this.turn.Clicked = 0;
			this.turn.MouseCursorSet = 0;
			this.turn.Ordered = 0;

			while (true)
			{
				LookStep step = ObtainOrder(participantIndex);

				while (step != LookStep.NextLook && step != LookStep.EndOfTurn)
				{
					switch (step)
					{
						case LookStep.OrderOrKey:
							step = (short)this.turn.Troop < UnitMemory.TroopTally ? LookStep.Order : LookStep.Direction;
							break;

						case LookStep.Order:
							step = CarryOutOrder(participantIndex) ? LookStep.Move : LookStep.Direction;
							break;

						case LookStep.Direction:
							step = ChooseDirection(participantIndex);
							break;

						case LookStep.Move:
							step = MoveTheUnit(participantIndex);
							break;

						default:
							step = CarryOutKey(participantIndex) switch
							{
								KeyStep.MenuOrder => LookStep.Order,
								KeyStep.EndOfTurn => LookStep.EndOfTurn,
								_ => LookStep.NextLook,
							};
							break;
					}
				}

				if (step == LookStep.EndOfTurn)
				{
					return false;
				}

				if (!ContinueOrFinishUnit(participantIndex))
				{
					return true;
				}
			}
		}

		private LookStep ObtainOrder(short participantIndex)
		{
			if (!this.Owner.World.IsHuman(participantIndex))
			{
				ObtainComputerOrder(participantIndex);
				return LookStep.Order;
			}

			if (ObtainStandingOrder(participantIndex))
			{
				return LookStep.OrderOrKey;
			}

			return TakeHumanOrder(participantIndex) switch
			{
				HumanOrder.MenuOrder => LookStep.Order,
				HumanOrder.MenuWithoutUnit => LookStep.Key,
				HumanOrder.NextLook => LookStep.NextLook,
				HumanOrder.EndOfTurn => LookStep.EndOfTurn,
				_ => LookStep.OrderOrKey,
			};
		}

		private LookStep ChooseDirection(short participantIndex)
		{
			if ((short)this.turn.Troop >= UnitMemory.TroopTally && this.turn.MarkedTileMode == 0)
			{
				return LookStep.Key;
			}

			if (this.turn.Order == 'c')
			{
				CenterView(participantIndex);
				return LookStep.Key;
			}

			if (KeyDirection(this.turn.Order) == 0)
			{
				return LookStep.Key;
			}

			this.turn.Direction = KeyDirection(this.turn.Order);
			return LookStep.Move;
		}

		private LookStep MoveTheUnit(short participantIndex)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				MoveMarkedTile(participantIndex);
				return LookStep.Key;
			}

			if (BarbarianPillages(participantIndex))
			{
				return LookStep.Key;
			}

			bool landUnit = FindMoveTarget(participantIndex);

			if (StoppedAtTheEdge(participantIndex))
			{
				return LookStep.Key;
			}

			if (landUnit)
			{
				if (StoppedByZoneOfControl(participantIndex))
				{
					return LookStep.Key;
				}

				switch (EnterCityAsDiplomatOrCaravan(participantIndex))
				{
					case MoveStep.Done:
						return LookStep.Key;

					case MoveStep.NoCity:
						BribeUnitOnTheWay(participantIndex);
						break;
				}
			}

			switch (CheckMoveTile(participantIndex))
			{
				case MoveCheck.Done:
					return LookStep.Key;

				case MoveCheck.Attacks:
					if (!ConfirmAttack(participantIndex))
					{
						return LookStep.Key;
					}

					return FightAt(participantIndex) ? LookStep.NextLook : LookStep.Key;
			}

			if (!PayForMove(participantIndex) || !ComputerMayMove(participantIndex) || !EnterTerritory(participantIndex))
			{
				return LookStep.Key;
			}

			ShowMove(participantIndex);
			MoveUnit(participantIndex);
			TakeCargoAlong(participantIndex);
			ArriveAtTile(participantIndex);
			FinishMove(participantIndex);

			return LookStep.Key;
		}

		private void PrepareTurn(short participantIndex)
		{
			Turn.Player = (ushort)participantIndex;
			this.turn.HumanMarkers = (ushort)this.Owner.World.ParticipantMarkers;
			this.turn.CursorHorizontal = 0xffff;
			this.turn.MarkedTileMode = 0;

			short person = this.Owner.World.PersonParticipantIndex;

			if (participantIndex == person)
			{
				this.Owner.World.ParticipantMarkers = (short)(1 << (person & 0xff));

				if (this.Owner.World.RoundTally == 20 || this.Owner.World.RoundTally == 60)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelpOne);
				}

				if (this.Owner.World.RoundTally == 40 || this.Owner.World.RoundTally == 80)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelpTwo);
				}
			}

			for (this.turn.Troop = 0; (short)this.turn.Troop < UnitMemory.TroopTally;
				this.turn.Troop = (ushort)(this.turn.Troop + 1))
			{
				short troopIndex = (short)this.turn.Troop;

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xff || Files.MidTurnSave == 1)
				{
					continue;
				}

				this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps,
					(byte)(3 * this.troops.KindField(participantIndex, troopIndex, UnitKindTable.ShiftTally)));

				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
				{
					bool lighthouse = this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 4) != 0;
					bool magellan = this.Owner.CityOperations.PlayerEnjoysWonderEffect(participantIndex, 8) != 0;

					this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps,
						(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) + (3 * MovementRules.SeaWonderMoves(lighthouse, magellan))));
				}
			}

			this.turn.HumanTookKey = 0;
			this.turn.LaterPass = 0;
			this.turn.Ordered = 0;
			this.turn.PastWaiting = 0;
			this.turn.WaitingTroop = 0x7f;
		}

		private bool StartUnitMove(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xff)
			{
				return false;
			}

			if (this.troops.TroopKind(participantIndex, troopIndex) >= 0x1c)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, 1);
			}

			if (this.turn.LaterPass == 0)
			{
				this.Owner.UnitOperations.ShiftUnit(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical), participantIndex, troopIndex, 0);
			}

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 9) != 0 ||
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) == 0)
			{
				return false;
			}

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 4) == 0)
			{
				return true;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Status, (byte)((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xfb) | 8));
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIndex, troopIndex);

			sbyte horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			sbyte verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

			if (participantIndex != this.Owner.World.PersonParticipantIndex && (this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				this.turn.Result = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);

				if ((sbyte)this.Owner.World.Towns[this.turn.Result].TrueExtent >= 3)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.BaseTown, (byte)this.turn.Result);
				}
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				this.turn.Result = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);

				if ((ushort)this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.BaseTown) == this.turn.Result &&
					this.Owner.UnitOperations.ContainsAdjacentForeignUnit(participantIndex, horizontalLocation, verticalLocation) == 0)
				{
					this.Owner.UnitOperations.KeepUnitsInCityList((short)this.turn.Result);
				}
			}

			return false;
		}

		private bool FocusOnUnit(short participantIndex)
		{
			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				return true;
			}

			short troopIndex = (short)this.turn.Troop;

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xc2) != 0 || IsAutomatedSettler(participantIndex, troopIndex))
			{
				return true;
			}

			ushort horizontalLocation = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
			this.indexDestination = horizontalLocation;
			ushort viewHorizontal = MapView.Horizontal;

			if (viewHorizontal < horizontalLocation)
			{
				this.turn.UnitDetail = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);
				ushort viewVertical = MapView.Vertical;

				if (this.turn.UnitDetail > viewVertical && (ushort)(viewHorizontal + 0xe) >= horizontalLocation &&
					(ushort)(viewVertical + 0xa) > this.turn.UnitDetail)
				{
					return true;
				}
			}

			if (this.turn.PastWaiting == 0)
			{
				this.turn.AllMoved = 0;
				return false;
			}

			if (MapView.WorldMapView != 1)
			{
				this.Owner.TextPainter.WaitForTime(30);
			}

			this.Owner.MapOperations.FocusMapView(participantIndex,
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal) - 7,
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical) - 6);

			this.turn.WaitingTroop = (ushort)((this.turn.Troop - 1) & 0x7f);
			this.turn.PastWaiting = 0;

			if (this.Owner.World.RoundTally == 0)
			{
				DrawUnitInformation(participantIndex, this.turn.Troop);
			}

			return true;
		}

		private void ObtainComputerOrder(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;
			this.turn.Order = this.Owner.Computer.ShiftComputerUnit(participantIndex, troopIndex);

			if (this.turn.Order != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			}

			this.turn.ComputerTries = (ushort)(this.turn.ComputerTries + 1);

			if ((short)this.turn.ComputerTries > 4)
			{
				this.turn.Order = 0x20;
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, 0xff);
			}
		}

		private bool ObtainStandingOrder(short participantIndex)
		{
			if ((short)this.turn.Troop >= UnitMemory.TroopTally)
			{
				return false;
			}

			short troopIndex = (short)this.turn.Troop;

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xc2) == 0 &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) == 0xff)
			{
				return IsAutomatedSettler(participantIndex, troopIndex) && DirectAutomatedSettler(participantIndex, troopIndex);
			}

			this.turn.Order = 0x72;

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x40) != 0)
			{
				this.turn.Order = (ushort)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0 ? 0x6d : 0x69);
			}

			this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status));

			if (((byte)this.turn.UnitDetail & 0x80) != 0)
			{
				this.turn.Order = 0x6d;

				if (((byte)this.turn.UnitDetail & 0x40) != 0)
				{
					this.turn.Order = 0x66;
				}

				if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 2) != 0)
				{
					this.turn.Order = 0x70;
				}
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				this.turn.Order = 0x68;
			}

			return true;
		}
	}
}
