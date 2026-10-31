using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The move of a unit: the target tile, zones of control, cities, fights, the cost of the move</summary>
	partial class TurnController
	{
		/// <summary>The free railroad steps of the units not moved by hand in the turn (RideForFree)</summary>
		private (int Round, Dictionary<(short, short), int> Steps) freeRides = (-1, new Dictionary<(short, short), int>());

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

		/// <inheritdoc/>
		public bool RideForFree(short participantIdx, short troopIdx, short horizLoc, short vertLoc)
		{
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, RailroadRules.RailroadTechnology) != 0;

			if (!RailroadRules.HasRailroad(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc), knowsRailroad, classic: false))
				return false;

			bool byHand = participantIdx == this.Owner.World.PersonParticipantIdx &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff;

			if (this.freeRides.Round != this.Owner.World.RoundTally)
				this.freeRides = (this.Owner.World.RoundTally, new Dictionary<(short, short), int>());

			int ridden = this.freeRides.Steps.GetValueOrDefault((participantIdx, troopIdx));

			if (!RailroadRules.IsFreeStep(byHand, ridden, classic: false))
				return false;

			if (!byHand)
				this.freeRides.Steps[(participantIdx, troopIdx)] = ridden + 1;

			return true;
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
				this.Owner.MapOps.FocusMapView(participantIdx,
					(short)this.turn.CursorHoriz - 7,
					(short)this.turn.CursorVert - 6);
			}
			else
			{
				this.Owner.MapOps.FocusMapView(participantIdx,
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
			if (((sbyte)this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (byte)participantIdx)) != 0)
			{
				this.Owner.MapOps.RepaintMapTile(
					(short)this.turn.CursorHoriz,
					(short)this.turn.CursorVert);
			}


			this.turn.CursorHoriz = (ushort)this.Owner.Routing.TunePosition(
				(short)DirectionHorizAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.CursorHoriz);

			this.turn.CursorVert = (ushort)this.Owner.Helpers.ClampValue(
				(short)DirectionVertAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.CursorVert,
				0, 49);

			if (CheckPositionOnScreen((short)this.turn.CursorHoriz, (short)this.turn.CursorVert) == 0)
			{
				this.Owner.MapOps.FocusMapView(participantIdx,
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
			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kind == UnitKindTable.Diplomat)
				return false;

			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;
			this.turn.TargetImprovements = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			if (((byte)this.turn.TargetImprovements & 1) != 0 || ((byte)this.turn.TargetImprovements & 6) == 0 || kind == 3 || kind == 7)
				return false;

			if (this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc)].ParticipantIdx != this.Owner.World.PersonParticipantIdx)
				return false;

			this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);

			if (((sbyte)this.Owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] & (1 << (this.Owner.World.PersonParticipantIdx & 0xff))) != 0)
			{
				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		/// <summary>
		/// The tile the unit 0x32 moves to in the direction 0x24 (0x1c / 0x26, off the poles): the terrain where it stands (0x34)
		/// and there (0x3e), the improvements there (0x40), the unit there (0x18); true for a land unit. The locals of
		/// HandlePlayerTurn are used as in the original, BX / DI as it leaves them.
		/// </summary>
		private bool FindMoveTarget(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			this.turn.TargetHoriz = (ushort)this.Owner.Routing.TunePosition(
				(short)DirectionHorizAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.UnitHoriz);

			this.turn.TargetVert = (ushort)this.Owner.Helpers.ClampValue(
				(short)DirectionVertAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.UnitVert,
				0, 49);

			this.turn.StartTerrain = this.Owner.MapOps.ObtainTerrainType((short)this.turn.UnitHoriz, (short)this.turn.UnitVert);
			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			this.turn.TargetImprovements = this.Owner.MapOps.ObtainTileImprovements((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			this.turn.OtherUnit = this.Owner.MapOps.ObtainUnitAtTile((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			this.indexDest = (ushort)(participantIdx * UnitMemory.ParticipantStride);

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
				this.Owner.UnitOps.MayOccupyTile(participantIdx, (short)this.turn.UnitHoriz, (short)this.turn.UnitVert) == 0 ||
				this.Owner.UnitOps.MayOccupyTile(participantIdx, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert) == 0)
			{
				return false;
			}

			short troopIdx = (short)this.turn.Troop;

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || this.turn.StartTerrain == MapRules.OceanTerrain)
				return false;

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
				this.Owner.Services.PerformSound(0x25, 0);

			if ((this.Owner.World.SessionPrefMarkers & 1) != 0)
				ShowHelpForHuman(GameTexts.TopicZoc);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			return true;
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

			if (this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat)
				return MoveStep.GoesOn;

			this.turn.City = this.Owner.Helpers.FindCityAtPosition((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			if (this.turn.City == 0xffff)
				return MoveStep.NoCity;

			Game.State.City town = this.Owner.World.Towns[this.turn.City];

			if (town.ParticipantIdx != participantIdx && this.turn.StartTerrain == MapRules.OceanTerrain)
				return MoveStep.Done;


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				this.indexDest = (ushort)town.ParticipantIdx;

				if (participantIdx != town.ParticipantIdx)
				{
					short person = this.Owner.World.PersonParticipantIdx;

					if (person == town.ParticipantIdx)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));

						this.Owner.UnitOps.PanMapToPosition(person,
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

						this.Owner.TextPainter.WaitForTime(30);

						this.Owner.UnitOps.ChoreographUnitCombat(participantIdx,
							(short)this.turn.Troop,
							this.turn.Direction);
					}

					this.Owner.Diplomats.DiplomatArrivesAtCity((short)this.turn.City,
						participantIdx,
						(short)this.turn.Troop);

					return MoveStep.Done;
				}
			}


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x1b)
				return MoveStep.GoesOn;

			// The caravan: its distance from its home city (0x3a)
			sbyte home = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown);
			this.indexDest = (ushort)(home * 0x1c);
			byte homeIdx = this.troops.Pull(participantIdx, troopIdx, UnitMemory.BaseTown);

			this.turn.Result = (ushort)this.Owner.Helpers.ObtainDistance(
				(short)this.turn.TargetHoriz,
				this.Owner.World.Towns[homeIdx].Place.X,
				(short)this.turn.TargetVert,
				this.Owner.World.Towns[homeIdx].Place.Y);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx >= -24 && (short)this.turn.Result < 10)
			{
				// Near its home city, building no wonder: on the same continent it just moves in
				ushort homeContinent = this.Owner.MapOps.ObtainContinentID(
					this.Owner.World.Towns[homeIdx].Place.X,
					this.Owner.World.Towns[homeIdx].Place.Y);
				this.indexDest = homeContinent;

				if (this.Owner.MapOps.ObtainContinentID((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) == homeContinent)
					return MoveStep.GoesOn;
			}

			this.Owner.Words.Set("Will you?\n Keep moving\n Establish trade route\n");

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx < -24)
			{
				this.Owner.Words.Append(" Help build WONDER.\n");

				if ((short)this.turn.Result < 10)
					DisabledLines = 0x2;
			}


			if (town.ParticipantIdx == participantIdx)
			{
				this.turn.Result = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
			else
			{
				this.turn.Result = 0x1;
			}

			if (this.turn.Result == 1 &&
				this.Owner.Conquest.FoundTradeRoute(participantIdx, (short)this.turn.Troop, (short)this.turn.City) != 0)
			{
				return MoveStep.Done;
			}

			if (this.turn.Result != 2)
				return MoveStep.GoesOn;

			// The caravan helps to build the wonder: 10 times its price
			short materials = (short)(10 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
			this.Owner.World.Towns[this.turn.City].MaterialsTally += materials;

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);

			return MoveStep.Done;
		}

		/// <summary>
		/// A diplomat next to a foreign unit where no city is bribes it (BuyUnit) and forgets its destination. The locals of
		/// HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BribeUnitOnTheWay(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat || this.turn.OtherUnit == 0xffff ||
				participantIdx == (short)UnitAtTilePlayer)
			{
				return;
			}

			this.Owner.Diplomats.BuyUnit((short)UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				participantIdx);

			// BuyUnit keeps SI
			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
		}

		/// <summary>
		/// The tile of the move: a land unit to the sea boards an own ship with room (sentried, 3 moves – written through SI as
		/// CountSpecialUnitsInStack leaves it, as the original does) or stays; a foreign unit there: no attack from a ship by a
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
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// A land unit boards an own ship with room
					if (participantIdx != (short)UnitAtTilePlayer || this.turn.OtherUnit == 0xffff ||
						(short)this.Owner.UnitOps.CountSpecialUnitsInStack(participantIdx, (short)this.turn.OtherUnit) <= 0)
					{
						return MoveCheck.Done;
					}

					// Written through the SI that CountSpecialUnitsInStack leaves: the unit 0 of the player, like in the original
					ushort boarded = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					ushort status = (ushort)(boarded + UnitMemory.Origin + UnitMemory.Status);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) | 1));
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) & 0xf3));
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(boarded + UnitMemory.Origin + UnitMemory.LeftoverSteps), 3);

					return MoveCheck.Moves;
				}
			}

			if (this.turn.OtherUnit == 0xffff || participantIdx == (short)UnitAtTilePlayer)
				return MoveCheck.Moves;

			if (this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// (a land unit on a ship does not attack)
					ShowHelpForHuman(GameTexts.TopicAmphib);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

					return MoveCheck.Done;
				}
			}


			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Submarine)
				return MoveCheck.Done;

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower) == 0)
					return MoveCheck.Done;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
				return MoveCheck.Done;

			// A plane outside a city is attacked only by a fighter
			short guardian = (short)this.turn.OtherUnit;
			this.indexDest = (ushort)(guardian * UnitMemory.TroopStride);

			if (this.troops.KindField(owner, guardian, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xe && ((byte)this.turn.TargetImprovements & 1) == 0)
				{
					ShowHelpForHuman(GameTexts.TopicFighter);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

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

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 3)
				return true;

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Words.Set("Attack at\n");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((sbyte)this.Troops.PullAt(UnitMemory.Offset(participantIdx, troopIdx), UnitMemory.LeftoverSteps), 10));
				this.Owner.Words.Append("/3 strength?\n Cancel\n Attack\n");
				if (this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 16) == 1)
					return true;

				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(UnitMemory.Offset(participantIdx, troopIdx) + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
				return false;
			}


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
			short person = this.Owner.World.PersonParticipantIdx;
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			if (UnitAtTilePlayer == (ushort)person)
				this.Owner.UnitOps.PanMapToPosition(person, horizLoc, vertLoc);

			this.turn.OtherUnit = this.Owner.UnitOps.FindBestGuardianInStack((short)UnitAtTilePlayer,
				(short)this.turn.OtherUnit);

			if (participantIdx == person || UnitAtTilePlayer == (ushort)person)
				CountLosses = 0x1;

			this.turn.Result = this.Owner.Combat.SettleBattle(participantIdx,
				(short)this.turn.Troop,
				(short)UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				1);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			CountLosses = 0x0;

			if (this.turn.Result == 0xffff)
				return false;


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				return true;
			}

			// 3 moves less (no fewer than 0), a bomber has none left
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			if (((byte)this.turn.TargetImprovements & 1) == 0)
			{
				if (((byte)this.turn.TargetImprovements & 0x20) == 0)
				{
						}

				return false;
			}

			// The attacked city
			this.turn.City = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);
			Game.State.City town = this.Owner.World.Towns[this.turn.City];

			if (town.ParticipantIdx != person)
				town.ConditionMarker |= 0x10;


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff && !town.Has(CityStructure.CityWalls) &&
				this.turn.StartTerrain != MapRules.OceanTerrain &&
				(this.Owner.World.HardnessTier != 0 || UnitAtTilePlayer != (ushort)person))
			{
				town.TrueExtent--;
			}


			if (town.TrueExtent == 0)
			{
				// The city is destroyed
				this.Owner.Advisors.FinalizeProduction(
					(short)this.turn.City,
					this.turn.TargetHoriz,
					this.turn.TargetVert);

				this.Owner.NewGameWizard.DestroyCivilization((short)UnitAtTilePlayer, participantIdx);

				if (participantIdx == person)
				{
					this.turn.OtherUnit = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

					if (this.turn.OtherUnit != 0xffff)
					{
						short owner = (short)UnitAtTilePlayer;
						short left = (short)this.turn.OtherUnit;
						this.troops.Assign(owner, left, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(owner, left, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));
					}
				}
			}

			if (participantIdx == person || UnitAtTilePlayer == (ushort)person || MapRevealed != 0)
			{
				// The human sees the new size
				town.ShownExtent = (sbyte)town.TrueExtent;

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if (this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) == 0xffff && UnitAtTilePlayer != (ushort)person)
			{
				this.Owner.Bot.RefreshCityProduction((short)UnitAtTilePlayer, this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc));
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
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
					return false;
				}
			}


			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits && ((byte)this.turn.TargetImprovements & 1) != 0 &&
				this.Owner.MapOps.ObtainTileOwnerBits((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != (ushort)participantIdx)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x19)
				{
					ShowHelpForHuman(GameTexts.TopicOccupy);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					return false;
				}

				this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);

				this.Owner.Combat.ExplodeNuclearWeapon(participantIdx,
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert);

				return false;
			}


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
				return false;


			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				short horizLoc = (short)this.turn.UnitHoriz;
				short vertLoc = (short)this.turn.UnitVert;

				if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & (byte)this.turn.TargetImprovements & 8) != 0 &&
					this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0)
				{
					// A road on both tiles: 1, a railroad where it stands nothing (for the human without GoTo)
					byte cost = 1;

					if (this.Owner.TurnHooks.ClassicRailroads)
					{
						if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x10) != 0 && participantIdx == this.Owner.World.PersonParticipantIdx)
						{
							this.indexDest = (ushort)(participantIdx * UnitMemory.ParticipantStride);

							if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
								cost = 0;
						}
					}
					else if (RideForFree(participantIdx, troopIdx, horizLoc, vertLoc))
					{
						cost = 0;
					}

					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - cost));
				}
				else
				{
					// The terrain: with fewer moves than full a die may stop the unit
					short terrainCost = (short)(3 * (sbyte)TerrainField(this.turn.TargetTerrain, TerrainTable.MoveCost));

					if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 3)
					{
						int roll = this.Owner.Libc.Dice.DrawInt(terrainCost);

						if (roll > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps))
						{
							this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
							return false;
						}
					}

					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - (byte)terrainCost));
				}
			}
			else
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));
			}


			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		/// <summary>
		/// The moves of a computer unit: one that would turn back (the opposite of its last heading) in a stack of up to 2 units
		/// forgets its heading and destination and stops (written through SI as CountUnitsByDomainInStack leaves it, as the
		/// original does); else the direction 0x24 becomes its heading. A computer land unit (or a plane with fuel) does not join
		/// an own stack outside a city of 4 units or more (2 or more without moves). False: no move (L3365). The locals of
		/// HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool ComputerMayMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.Owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHeading) != 0xff &&
					(ushort)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHeading) ^ 4) == this.turn.Direction &&
					(short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, (short)this.turn.Troop, 2) <= 2)
				{
					ushort targetBase = (ushort)(UnitMemory.Origin + (UnitMemory.ParticipantStride * participantIdx));
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(targetBase + UnitMemory.TravelHeading), 0xff);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(targetBase + UnitMemory.TravelHoriz), 0xff);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(targetBase + UnitMemory.LeftoverSteps), 0);

					return false;
				}

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, (byte)this.turn.Direction);
			}

			if (this.turn.OtherUnit == 0xffff || participantIdx != (short)UnitAtTilePlayer || participantIdx == person ||
				((byte)this.turn.TargetImprovements & 1) != 0)
			{
				return true;
			}

			ushort group = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);
			this.indexDest = group;

			if (group != UnitKindTable.LandUnits &&
				(group != UnitKindTable.AirUnits || this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) == 0))
			{
				return true;
			}

			this.turn.Result = this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, (short)this.turn.OtherUnit, 2);
			short threshold = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0 ? 4 : 2);

			return threshold > (short)this.turn.Result;
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
			short person = this.Owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			this.turn.Dot = this.Owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);

			if (this.turn.Dot == 0 || (short)this.turn.Dot >= 8)
				return true;

			short owner = (short)this.turn.Dot;

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.turn.StartTerrain != MapRules.OceanTerrain &&
				MayTreatyStop(participantIdx, troopIdx, unit, owner, person))
			{
				if (participantIdx == person)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

					if (this.Owner.Combat.AffirmBreakTreaty((short)this.turn.Dot) == 0xffff)
						return false;

					this.Owner.Politics.ClearDiplomacyFlags(participantIdx, (short)this.turn.Dot, 2);
				}
				else if (this.turn.Dot == (ushort)person)
				{
					if ((this.Owner.World.Participants[this.turn.Dot].Relations[participantIdx] & 2) != 0)
					{
									this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

						return false;
					}
				}
			}

			if (participantIdx != (short)this.turn.Dot && (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0xe) != 0)
			{
				byte mask = (byte)(1 << ((byte)this.turn.Dot & 0x1f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | mask));
			}

			return true;
		}

		/// <summary>
		/// The conditions of EnterTerritory after the terrains: a land unit (not a diplomat or caravan) onto an improved tile of
		/// another player's territory, between the human and another player, not onto a tile of its own player
		/// </summary>
		private bool MayTreatyStop(short participantIdx, short troopIdx, ushort unit, short owner, short person)
		{
			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || ((byte)this.turn.TargetImprovements & 0xf) == 0 ||
				owner == participantIdx || (participantIdx != person && owner != person))
			{
				return false;
			}


			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.Owner.MapOps.ObtainTileOwner((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != (ushort)participantIdx;
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
			short person = this.Owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			this.turn.TileOwner = this.Owner.MapOps.ObtainTileOwnerBits((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			if (participantIdx == 0 && this.turn.TargetTerrain != MapRules.OceanTerrain)
			{
				if (((sbyte)this.Owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] & personBit) != 0)
				{
					this.indexDest = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.ShownToParticipants);

					if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
						this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity((short)this.turn.UnitHoriz, (short)this.turn.UnitVert)].ParticipantIdx == person)
					{
						byte sight = (byte)this.Owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert];
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | sight));
					}
				}
			}

			if (((byte)this.turn.TargetImprovements & 1) != 0 && this.turn.TileOwner != (ushort)participantIdx &&
				((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & personBit) != 0)
			{
				// Into a foreign city the human sees: with an embassy
				bool embassy = (this.Owner.World.Participants[person].Relations[participantIdx] & 0x40) != 0;

				if (!embassy)
				{
					embassy = (this.Owner.World.Participants[person].Relations[this.turn.TileOwner] & 0x40) != 0;
				}

				if (embassy)
				{
							this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
						(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | personBit));
				}
			}

			if (this.turn.TargetTerrain == MapRules.OceanTerrain &&
				(this.Owner.MapOps.ObtainTileImprovements((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) & 2) != 0)
			{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (byte)(1 << ((byte)this.turn.TileOwner & 0x1f))));
			}

			// The move on the screen

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 && participantIdx != person &&
				(MapRevealed == 0 || participantIdx == 0))
			{
				return;
			}

			if ((this.Owner.World.SessionPrefMarkers & 0x20) != 0 || participantIdx == person)
			{
				this.Owner.UnitOps.PanMapToPosition(person,
					(short)this.turn.TargetHoriz,
					(short)this.turn.TargetVert);

				this.Owner.UnitOps.ChoreographUnitCombat(participantIdx,
					(short)this.turn.Troop,
					this.turn.Direction);
			}
			else
			{
				this.Owner.MapOps.DrawMapTile((short)this.turn.UnitHoriz, (short)this.turn.UnitVert);
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
			short person = this.Owner.World.PersonParticipantIdx;

			this.turn.Cargo = (ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.FollowingInStack);

			this.Owner.MapOps.RemoveUnitFromTile(participantIdx,
				(short)this.turn.Troop,
				(short)this.turn.UnitHoriz,
				(short)this.turn.UnitVert);

			this.turn.CarrierHoriz = this.turn.UnitHoriz;
			this.turn.CarrierVert = this.turn.UnitVert;

			this.Owner.MapOps.PlaceUnitOnTile(participantIdx,
				(short)this.turn.Troop,
				(short)this.turn.TargetHoriz,
				(short)this.turn.TargetVert);

			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.Horiz), (byte)this.turn.TargetHoriz);
			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.Vert), (byte)this.turn.TargetVert);
			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.ShownToParticipants), 0);

			if (participantIdx != person)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x30));
			}

			if (this.turn.Dot != 0 && (short)this.turn.Dot < 8 && this.turn.Dot != (ushort)participantIdx)
			{
				this.Owner.Services.AssignDot(2, (short)this.turn.TargetHoriz + 80,
					(short)this.turn.TargetVert,
					0);
			}


			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == this.turn.TargetHoriz &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) == this.turn.TargetVert)
			{
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
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}

			this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if ((UnitKindWord((sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.CarrierKind != UnitKindTable.Carrier) || this.turn.Cargo == 0xffff)
			{
				return;
			}

			this.turn.CargoTally = 0;
			this.turn.Count = 0;

			while (true)
			{
				short member = (short)this.turn.Cargo;
				this.indexDest = (ushort)(member * UnitMemory.TroopStride + participantIdx * UnitMemory.ParticipantStride);
				this.turn.NextCargo = (ushort)this.troops.PullSigned(participantIdx, member, UnitMemory.FollowingInStack);

				// A transport takes land units, a carrier (no room) planes
				ushort cargoGroup = (ushort)(this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ? 1 : 0);

				if (cargoGroup == this.troops.KindField(participantIdx, member, UnitKindTable.LandGroup))
				{
					byte status = this.troops.Pull(participantIdx, member, UnitMemory.Status);
					bool taken = (status & 1) != 0 || this.turn.StartTerrain == MapRules.OceanTerrain;

					if (!taken && participantIdx != this.Owner.World.PersonParticipantIdx && (status & 0xc) == 0)
					{
						taken = this.troops.Pull(participantIdx, member, UnitMemory.FollowingInStack) != 0xff;
					}

					if (taken)
						CarryUnit(participantIdx, troopIdx, member, player);
				}

				this.turn.Cargo = this.turn.NextCargo;
				this.turn.CargoTally = (ushort)(this.turn.CargoTally + 1);

				if ((short)this.turn.CargoTally >= 0x14 || this.turn.Cargo == 0xffff)
					return;

					this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

				if ((short)UnitKindWord((sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) <= (short)this.turn.Count &&
					(byte)this.turn.CarrierKind != UnitKindTable.Carrier && participantIdx != 0)
				{
					return;
				}
			}
		}

		/// <summary>One unit of the cargo moves along with the ship (see TakeCargoAlong)</summary>
		private void CarryUnit(short participantIdx, short troopIdx, short member, ushort player)
		{
			this.Owner.MapOps.RemoveUnitFromTile(participantIdx,
				(short)this.turn.Cargo,
				(short)this.turn.CarrierHoriz,
				(short)this.turn.CarrierVert);

			this.Owner.MapOps.PlaceUnitOnTile(participantIdx,
				(short)this.turn.Cargo,
				(short)this.turn.TargetHoriz,
				(short)this.turn.TargetVert);

			this.troops.Assign(participantIdx, member, UnitMemory.Horiz, (byte)this.turn.TargetHoriz);
			this.troops.Assign(participantIdx, member, UnitMemory.Vert, (byte)this.turn.TargetVert);
			this.troops.Assign(participantIdx, member, UnitMemory.ShownToParticipants, 0);
			this.troops.Assign(participantIdx, member, UnitMemory.TravelHeading, 0xff);


			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) != 0)
			{
				this.troops.Assign(participantIdx, member, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, member, UnitMemory.Status) | 1));
			}

			// A plane on a carrier: a round of fuel less
			ushort roundsAway = this.troops.KindField(participantIdx, member, UnitKindTable.RoundsAway);
			this.indexDest = roundsAway;

			if (roundsAway != 0)
			{
				this.troops.Assign(participantIdx, member, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
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
			short person = this.Owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			if (((byte)this.turn.TargetImprovements & 1) != 0 && this.turn.TileOwner != (ushort)participantIdx)
			{
				// A foreign city is taken
				byte mask = (byte)(1 << ((byte)this.turn.TileOwner & 0x1f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | mask));

				this.turn.City = this.Owner.Helpers.FindCityAtPosition((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

				this.Owner.Conquest.SeizeCity(participantIdx, (short)this.turn.City, 0);

				this.Owner.MapOps.TagTileOwned((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			}

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			this.Owner.UnitOps.ShiftUnit(
				(short)this.turn.TargetHoriz,
				(short)this.turn.TargetVert,
				participantIdx,
				(short)this.turn.Troop,
				1);

			if (this.Owner.MapOps.ContainsHut(this.turn.TargetTerrain, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != 0)
			{
				this.Owner.UnitOps.VisitHut(participantIdx, (short)this.turn.Troop);

				this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] |= 1;
			}

			if (this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					this.Owner.UnitOps.VisitEachUnitInStack(participantIdx, (short)this.turn.Troop, 0x1560);
			}

			if (participantIdx != 0 || this.turn.TargetTerrain == MapRules.OceanTerrain)
				return;

			// A barbarian the human sees near his city

			if (((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & personBit) == 0)
				return;

			this.indexDest = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.ShownToParticipants);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
				this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity((short)this.turn.TargetHoriz, (short)this.turn.TargetVert)].ParticipantIdx == person)
			{
				byte sight = (byte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert];
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | sight));
			}
		}

		/// <summary>
		/// The end of the move: a move the human sees (or his own, or of a computer player with everything shown) shows the
		/// unit on the map (with the view on it for the human or the option 0x20, a short wait for a computer move unless
		/// 0xb278); 0xa = 0; a plane in a city lands (no orders, no moves, no destination), a plane on a carrier gets its fuel
		/// back (written through SI as CountUnitsOfTypeInStack leaves it, as the original does). The locals of HandlePlayerTurn
		/// are used as in the original.
		/// </summary>
		private void FinishMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.Owner.World.PersonParticipantIdx;


			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << (person & 0xff))) != 0 || participantIdx == person ||
				(MapRevealed != 0 && participantIdx != 0))
			{
				if (participantIdx == person || (this.Owner.World.SessionPrefMarkers & 0x20) != 0)
				{
					this.Owner.UnitOps.PanMapToPosition(person, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
					this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, (short)this.turn.Troop);

					if (participantIdx != person && WorldMapView != 1)
						this.Owner.TextPainter.WaitForTime(30);
				}
				else
				{
					if (this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, (short)this.turn.Troop) != 0)
						this.Owner.TextPainter.WaitForTime(10);
				}
			}

			this.turn.AITries = 0;

			if (((byte)this.turn.TargetImprovements & 1) != 0)
			{
				// A plane lands in a city

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
				}
			}

			this.indexDest = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) == 0 ||
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				return;
			}

			// A plane on a carrier
			if (this.Owner.UnitOps.CountUnitsOfTypeInStack(participantIdx, (short)this.turn.Troop, 23) == 0)
				return;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));
		}
	}
}
