namespace Civilization1Remaster.Core.Game
{
	public partial class TurnController : GameComponent, ITurnController
	{
		private readonly UnitMemory troops;

		private TurnLocals turn = new TurnLocals();

		private ushort indexDest;

		public TurnController(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public void HandlePlayerTurn(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"HandlePlayerTurn({participantIdx})");

			TurnLocals outer = this.turn;
			this.turn = new TurnLocals();
			ushort outerDest = this.indexDest;

			PrepareTurn(participantIdx);

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
					if (!VisitUnit(participantIdx))
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

				if (Turn.GameEndRequest != 0 || !this.Owner.World.IsHuman(participantIdx) ||
					(this.turn.HumanTookKey != 0 && (this.Owner.World.SessionPrefMarkers & 4) == 0))
				{
					break;
				}

				this.turn.Troop = UnitMemory.TroopTally;
				this.turn.AllMoved = 0;
				this.turn.LaterPass = 1;

				if (!LookAtUnit(participantIdx))
				{
					break;
				}

				this.turn.Troop = (ushort)(this.turn.Troop + 1);
				newPass = false;
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Startup.HideMouseCursor();
				this.Owner.TurnHooks.FillBoxWithShadow(0, 97, 80, 103);
				this.Owner.Startup.ShowMouseCursor();
			}

			this.indexDest = outerDest;
			this.turn = outer;
		}

		private bool VisitUnit(short participantIdx)
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

			if (!StartUnitMove(participantIdx))
			{
				return true;
			}

			if (!FocusOnUnit(participantIdx))
			{
				return true;
			}

			return LookAtUnit(participantIdx);
		}

		private bool LookAtUnit(short participantIdx)
		{
			if (this.Owner.World.IsHuman(participantIdx))
			{
				WaitForKey();
			}

			this.turn.AITries = 0;

			if (this.turn.MouseCursorSet != 0)
			{
				this.Owner.Services.AssignMouseCursorImage(0, 0, Mouse.Picture);
			}

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
							step = CarryOutOrder(participantIdx) ? LookStep.Move : LookStep.Direction;
							break;

						case LookStep.Direction:
							step = ChooseDirection(participantIdx);
							break;

						case LookStep.Move:
							step = MoveTheUnit(participantIdx);
							break;

						default:
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
				{
					return false;
				}

				if (!ContinueOrFinishUnit(participantIdx))
				{
					return true;
				}
			}
		}

		private LookStep ObtainOrder(short participantIdx)
		{
			if (!this.Owner.World.IsHuman(participantIdx))
			{
				ObtainAIOrder(participantIdx);
				return LookStep.Order;
			}

			if (ObtainStandingOrder(participantIdx))
			{
				return LookStep.OrderOrKey;
			}

			return TakeHumanOrder(participantIdx) switch
			{
				HumanOrder.MenuOrder => LookStep.Order,
				HumanOrder.MenuWithoutUnit => LookStep.Key,
				HumanOrder.NextLook => LookStep.NextLook,
				HumanOrder.EndOfTurn => LookStep.EndOfTurn,
				_ => LookStep.OrderOrKey,
			};
		}

		private LookStep ChooseDirection(short participantIdx)
		{
			if ((short)this.turn.Troop >= UnitMemory.TroopTally && this.turn.MarkedTileMode == 0)
			{
				return LookStep.Key;
			}

			if (this.turn.Order == 'c')
			{
				CenterView(participantIdx);
				return LookStep.Key;
			}

			if (KeyDirection(this.turn.Order) == 0)
			{
				return LookStep.Key;
			}

			this.turn.Direction = KeyDirection(this.turn.Order);
			return LookStep.Move;
		}

		private LookStep MoveTheUnit(short participantIdx)
		{
			if (this.turn.MarkedTileMode != 0)
			{
				MoveMarkedTile(participantIdx);
				return LookStep.Key;
			}

			if (BarbarianPillages(participantIdx))
			{
				return LookStep.Key;
			}

			if (FindMoveTarget(participantIdx))
			{
				if (StoppedByZoneOfControl(participantIdx))
				{
					return LookStep.Key;
				}

				switch (EnterCityAsDiplomatOrCaravan(participantIdx))
				{
					case MoveStep.Done:
						return LookStep.Key;

					case MoveStep.NoCity:
						BribeUnitOnTheWay(participantIdx);
						break;
				}
			}

			switch (CheckMoveTile(participantIdx))
			{
				case MoveCheck.Done:
					return LookStep.Key;

				case MoveCheck.Attacks:
					if (!ConfirmAttack(participantIdx))
					{
						return LookStep.Key;
					}

					return FightAt(participantIdx) ? LookStep.NextLook : LookStep.Key;
			}

			if (!PayForMove(participantIdx) || !ComputerMayMove(participantIdx) || !EnterTerritory(participantIdx))
			{
				return LookStep.Key;
			}

			ShowMove(participantIdx);
			MoveUnit(participantIdx);
			TakeCargoAlong(participantIdx);
			ArriveAtTile(participantIdx);
			FinishMove(participantIdx);

			return LookStep.Key;
		}

		private void PrepareTurn(short participantIdx)
		{
			Turn.Player = (ushort)participantIdx;
			this.turn.HumanMarkers = (ushort)this.Owner.World.ParticipantMarkers;
			this.turn.CursorHoriz = 0xffff;
			this.turn.MarkedTileMode = 0;

			short person = this.Owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				this.Owner.World.ParticipantMarkers = (short)(1 << (person & 0xff));

				if (this.Owner.World.RoundTally == 20 || this.Owner.World.RoundTally == 60)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelp1);
				}

				if (this.Owner.World.RoundTally == 40 || this.Owner.World.RoundTally == 80)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicHelp2);
				}
			}

			for (this.turn.Troop = 0; (short)this.turn.Troop < UnitMemory.TroopTally;
				this.turn.Troop = (ushort)(this.turn.Troop + 1))
			{
				short troopIdx = (short)this.turn.Troop;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff || Files.MidTurnSave == 1)
				{
					continue;
				}

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps,
					(byte)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)));

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
					(this.Owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 4) != 0 || this.Owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 8) != 0))
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

		private bool StartUnitMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
			{
				return false;
			}

			if (this.troops.TroopKind(participantIdx, troopIdx) >= 0x1c)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 1);
			}

			if (this.turn.LaterPass == 0)
			{
				this.Owner.UnitOps.ShiftUnit(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), participantIdx, troopIdx, 0);
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 9) != 0 ||
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
			{
				return false;
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 4) == 0)
			{
				return true;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfb) | 8));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, troopIdx);

			sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			if (participantIdx != this.Owner.World.PersonParticipantIdx && (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.turn.Result = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);

				if ((sbyte)this.Owner.World.Towns[this.turn.Result].TrueExtent >= 3)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)this.turn.Result);
				}
			}

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.turn.Result = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);

				if ((ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == this.turn.Result &&
					this.Owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx, horizLoc, vertLoc) == 0)
				{
					this.Owner.UnitOps.KeepUnitsInCityList((short)this.turn.Result);
				}
			}

			return false;
		}

		private bool FocusOnUnit(short participantIdx)
		{
			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				return true;
			}

			short troopIdx = (short)this.turn.Troop;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0)
			{
				return true;
			}

			ushort horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.indexDest = horizLoc;
			ushort viewHoriz = MapView.Horiz;

			if (viewHoriz < horizLoc)
			{
				this.turn.UnitDetail = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
				ushort viewVert = MapView.Vert;

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

			if (MapView.WorldMapView != 1)
			{
				this.Owner.TextPainter.WaitForTime(30);
			}

			this.Owner.MapOps.FocusMapView(participantIdx,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 7,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - 6);

			this.turn.WaitingTroop = (ushort)((this.turn.Troop - 1) & 0x7f);
			this.turn.PastWaiting = 0;

			if (this.Owner.World.RoundTally == 0)
			{
				DrawUnitInfo(participantIdx, this.turn.Troop);
			}

			return true;
		}

		private void ObtainAIOrder(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			this.turn.Order = this.Owner.Bot.ShiftAIUnit(participantIdx, troopIdx);

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

		private bool ObtainStandingOrder(short participantIdx)
		{
			if ((short)this.turn.Troop >= UnitMemory.TroopTally)
			{
				return false;
			}

			short troopIdx = (short)this.turn.Troop;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) == 0 &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
			{
				return false;
			}

			this.turn.Order = 0x72;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x40) != 0)
			{
				this.turn.Order = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ? 0x6d : 0x69);
			}

			this.turn.UnitDetail = (ushort)((this.turn.UnitDetail & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status));

			if (((byte)this.turn.UnitDetail & 0x80) != 0)
			{
				this.turn.Order = 0x6d;

				if (((byte)this.turn.UnitDetail & 0x40) != 0)
				{
					this.turn.Order = 0x66;
				}

				if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 2) != 0)
				{
					this.turn.Order = 0x70;
				}
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				this.turn.Order = 0x68;
			}

			return true;
		}
	}
}
