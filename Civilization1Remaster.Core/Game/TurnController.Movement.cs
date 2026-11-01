using System.Collections.Generic;

namespace Civilization1Remaster.Core.Game
{
	public partial class TurnController
	{
		private (int Round, Dictionary<(short, short), int> Steps) freeRides = (-1, new Dictionary<(short, short), int>());

		private enum MoveStep
		{
			GoesOn,

			NoCity,

			Done,
		}

		private enum MoveCheck
		{
			Moves,

			Done,

			Attacks,
		}

		public bool RideForFree(short participantIdx, short troopIdx, short horizLoc, short vertLoc)
		{
			bool knowsRailroad = this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, RailroadRules.RailroadTechnology) != 0;

			if (!RailroadRules.HasRailroad(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc), knowsRailroad))
			{
				return false;
			}

			bool byHand = participantIdx == this.Owner.World.PersonParticipantIdx &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff;

			if (this.freeRides.Round != this.Owner.World.RoundTally)
			{
				this.freeRides = (this.Owner.World.RoundTally, new Dictionary<(short, short), int>());
			}

			int ridden = this.freeRides.Steps.GetValueOrDefault((participantIdx, troopIdx));

			if (!RailroadRules.IsFreeStep(byHand, ridden))
			{
				return false;
			}

			if (!byHand)
			{
				this.freeRides.Steps[(participantIdx, troopIdx)] = ridden + 1;
			}

			return true;
		}

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

		private void MoveMarkedTile(short participantIdx)
		{
			if (((sbyte)this.Owner.World.ChartSight[this.turn.CursorHoriz, this.turn.CursorVert] & (1 << (byte)participantIdx)) != 0)
			{
				this.Owner.MapOps.RepaintMapTile(
					(short)this.turn.CursorHoriz,
					(short)this.turn.CursorVert);
			}

			this.turn.CursorHoriz = (ushort)this.Owner.Routing.TunePosition(
				(short)Tables.DirectionHorizAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.CursorHoriz);

			this.turn.CursorVert = (ushort)this.Owner.Helpers.ClampValue(
				(short)Tables.DirectionVertAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.CursorVert,
				0, 49);

			if (CheckPositionOnScreen((short)this.turn.CursorHoriz, (short)this.turn.CursorVert) == 0)
			{
				this.Owner.MapOps.FocusMapView(participantIdx,
					(short)this.turn.CursorHoriz - 8,
					(short)this.turn.CursorVert - 7);
			}
		}

		private bool BarbarianPillages(short participantIdx)
		{
			if (participantIdx != 0)
			{
				return false;
			}

			short troopIdx = (short)this.turn.Troop;
			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kind == UnitKindTable.Diplomat)
			{
				return false;
			}

			short horizLoc = (short)this.turn.UnitHoriz;
			short vertLoc = (short)this.turn.UnitVert;
			this.turn.TargetImprovements = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			if (((byte)this.turn.TargetImprovements & 1) != 0 || ((byte)this.turn.TargetImprovements & 6) == 0 || kind == 3 || kind == 7)
			{
				return false;
			}

			if (this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc)].ParticipantIdx != this.Owner.World.PersonParticipantIdx)
			{
				return false;
			}

			this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);

			if (((sbyte)this.Owner.World.ChartSight[this.turn.UnitHoriz, this.turn.UnitVert] & (1 << (this.Owner.World.PersonParticipantIdx & 0xff))) != 0)
			{
				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		private bool FindMoveTarget(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			this.turn.TargetHoriz = (ushort)this.Owner.Routing.TunePosition(
				(short)Tables.DirectionHorizAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.UnitHoriz);

			this.turn.TargetVert = (ushort)this.Owner.Helpers.ClampValue(
				(short)Tables.DirectionVertAt((ushort)(this.turn.Direction << 1)) +
					(short)this.turn.UnitVert,
				0, 49);

			this.turn.StartTerrain = this.Owner.MapOps.ObtainTerrainType((short)this.turn.UnitHoriz, (short)this.turn.UnitVert);
			this.turn.TargetTerrain = this.Owner.MapOps.ObtainTerrainType((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			this.turn.TargetImprovements = this.Owner.MapOps.ObtainTileImprovements((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
			this.turn.OtherUnit = this.Owner.MapOps.ObtainUnitAtTile((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			this.indexDest = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;
		}

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
			{
				return false;
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Services.PerformSound(0x25, 0);
			}

			if ((this.Owner.World.SessionPrefMarkers & 1) != 0)
			{
				ShowHelpForHuman(GameTexts.TopicZoc);
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			return true;
		}

		private MoveStep EnterCityAsDiplomatOrCaravan(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat)
			{
				return MoveStep.GoesOn;
			}

			this.turn.City = this.Owner.Helpers.FindCityAtPosition((short)this.turn.TargetHoriz, (short)this.turn.TargetVert);

			if (this.turn.City == 0xffff)
			{
				return MoveStep.NoCity;
			}

			GameModel.State.City town = this.Owner.World.Towns[this.turn.City];

			if (town.ParticipantIdx != participantIdx && this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				return MoveStep.Done;
			}

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
			{
				return MoveStep.GoesOn;
			}

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
				ushort homeContinent = this.Owner.MapOps.ObtainContinentID(
					this.Owner.World.Towns[homeIdx].Place.X,
					this.Owner.World.Towns[homeIdx].Place.Y);
				this.indexDest = homeContinent;

				if (this.Owner.MapOps.ObtainContinentID((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) == homeContinent)
				{
					return MoveStep.GoesOn;
				}
			}

			this.Owner.Words.Set("Will you?\n Keep moving\n Establish trade route\n");

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx < -24)
			{
				this.Owner.Words.Append(" Help build WONDER.\n");

				if ((short)this.turn.Result < 10)
				{
					Menu.DisabledLines = 0x2;
				}
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
			{
				return MoveStep.GoesOn;
			}

			short materials = (short)(10 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
			this.Owner.World.Towns[this.turn.City].MaterialsTally += materials;

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);

			return MoveStep.Done;
		}

		private void BribeUnitOnTheWay(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat || this.turn.OtherUnit == 0xffff ||
				participantIdx == (short)MapView.UnitAtTilePlayer)
			{
				return;
			}

			this.Owner.Diplomats.BuyUnit((short)MapView.UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				participantIdx);

			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
		}

		private MoveCheck CheckMoveTile(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short owner = (short)MapView.UnitAtTilePlayer;

			if (this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					if (participantIdx != (short)MapView.UnitAtTilePlayer || this.turn.OtherUnit == 0xffff ||
						(short)this.Owner.UnitOps.CountSpecialUnitsInStack(participantIdx, (short)this.turn.OtherUnit) <= 0)
					{
						return MoveCheck.Done;
					}

					ushort boarded = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					ushort status = (ushort)(boarded + UnitMemory.Origin + UnitMemory.Status);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) | 1));
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) & 0xf3));
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(boarded + UnitMemory.Origin + UnitMemory.LeftoverSteps), 3);

					return MoveCheck.Moves;
				}
			}

			if (this.turn.OtherUnit == 0xffff || participantIdx == (short)MapView.UnitAtTilePlayer)
			{
				return MoveCheck.Moves;
			}

			if (this.turn.StartTerrain == MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					ShowHelpForHuman(GameTexts.TopicAmphib);
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

					return MoveCheck.Done;
				}
			}

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Submarine)
			{
				return MoveCheck.Done;
			}

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower) == 0)
				{
					return MoveCheck.Done;
				}
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				return MoveCheck.Done;
			}

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

		private bool ConfirmAttack(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 3)
			{
				return true;
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Words.Set("Attack at\n");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((sbyte)this.Troops.PullAt(UnitMemory.Offset(participantIdx, troopIdx), UnitMemory.LeftoverSteps), 10));
				this.Owner.Words.Append("/3 strength?\n Cancel\n Attack\n");
				if (this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 16) == 1)
				{
					return true;
				}

				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(UnitMemory.Offset(participantIdx, troopIdx) + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
				return false;
			}

			return this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 2;
		}

		private bool FightAt(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			short troopIdx = (short)this.turn.Troop;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			if (MapView.UnitAtTilePlayer == (ushort)person)
			{
				this.Owner.UnitOps.PanMapToPosition(person, horizLoc, vertLoc);
			}

			this.turn.OtherUnit = this.Owner.UnitOps.FindBestGuardianInStack((short)MapView.UnitAtTilePlayer,
				(short)this.turn.OtherUnit);

			if (participantIdx == person || MapView.UnitAtTilePlayer == (ushort)person)
			{
				Turn.CountLosses = 0x1;
			}

			this.turn.Result = this.Owner.Combat.SettleBattle(participantIdx,
				(short)this.turn.Troop,
				(short)MapView.UnitAtTilePlayer,
				(short)this.turn.OtherUnit,
				1);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			Turn.CountLosses = 0x0;

			if (this.turn.Result == 0xffff)
			{
				return false;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				return true;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}

			if (((byte)this.turn.TargetImprovements & 1) == 0)
			{
				if (((byte)this.turn.TargetImprovements & 0x20) == 0)
				{
						}

				return false;
			}

			this.turn.City = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);
			GameModel.State.City town = this.Owner.World.Towns[this.turn.City];

			if (town.ParticipantIdx != person)
			{
				town.ConditionMarker |= 0x10;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff && !town.Has(CityStructure.CityWalls) &&
				this.turn.StartTerrain != MapRules.OceanTerrain &&
				(this.Owner.World.HardnessTier != 0 || MapView.UnitAtTilePlayer != (ushort)person))
			{
				town.TrueExtent--;
			}

			if (town.TrueExtent == 0)
			{
				this.Owner.Advisors.FinalizeProduction(
					(short)this.turn.City,
					this.turn.TargetHoriz,
					this.turn.TargetVert);

				this.Owner.NewGameWizard.DestroyCivilization((short)MapView.UnitAtTilePlayer, participantIdx);

				if (participantIdx == person)
				{
					this.turn.OtherUnit = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

					if (this.turn.OtherUnit != 0xffff)
					{
						short owner = (short)MapView.UnitAtTilePlayer;
						short left = (short)this.turn.OtherUnit;
						this.troops.Assign(owner, left, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(owner, left, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));
					}
				}
			}

			if (participantIdx == person || MapView.UnitAtTilePlayer == (ushort)person || MapView.Revealed != 0)
			{
				town.ShownExtent = (sbyte)town.TrueExtent;

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if (this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) == 0xffff && MapView.UnitAtTilePlayer != (ushort)person)
			{
				this.Owner.Bot.RefreshCityProduction((short)MapView.UnitAtTilePlayer, this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc));
			}

			return false;
		}

		private bool PayForMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);

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
			{
				return false;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				short horizLoc = (short)this.turn.UnitHoriz;
				short vertLoc = (short)this.turn.UnitVert;

				if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & (byte)this.turn.TargetImprovements & 8) != 0 &&
					this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0)
				{
					byte cost = 1;

					if (RideForFree(participantIdx, troopIdx, horizLoc, vertLoc))
					{
						cost = 0;
					}

					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - cost));
				}
				else
				{
					short terrainCost = (short)(3 * (sbyte)Tables.TerrainField(this.turn.TargetTerrain, TerrainTable.MoveCost));

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
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			}

			return true;
		}

		private bool ComputerMayMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
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

			if (this.turn.OtherUnit == 0xffff || participantIdx != (short)MapView.UnitAtTilePlayer || participantIdx == person ||
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

		private bool EnterTerritory(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);
			short person = this.Owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.turn.TargetHoriz;
			short vertLoc = (short)this.turn.TargetVert;

			this.turn.Dot = this.Owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);

			if (this.turn.Dot == 0 || (short)this.turn.Dot >= 8)
			{
				return true;
			}

			short owner = (short)this.turn.Dot;

			if (this.turn.TargetTerrain != MapRules.OceanTerrain && this.turn.StartTerrain != MapRules.OceanTerrain &&
				MayTreatyStop(participantIdx, troopIdx, owner, person))
			{
				if (participantIdx == person)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

					if (this.Owner.Combat.AffirmBreakTreaty((short)this.turn.Dot) == 0xffff)
					{
						return false;
					}

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

		private bool MayTreatyStop(short participantIdx, short troopIdx, short owner, short person)
		{
			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || ((byte)this.turn.TargetImprovements & 0xf) == 0 ||
				owner == participantIdx || (participantIdx != person && owner != person))
			{
				return false;
			}

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.Owner.MapOps.ObtainTileOwner((short)this.turn.TargetHoriz, (short)this.turn.TargetVert) != (ushort)participantIdx;
		}

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
					this.indexDest = (ushort)((troopIdx * UnitMemory.TroopStride) + UnitMemory.Origin + UnitMemory.ShownToParticipants);

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

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 && participantIdx != person &&
				(MapView.Revealed == 0 || participantIdx == 0))
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
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x30));
				}
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
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				}
			}
		}

		private void TakeCargoAlong(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			ushort player = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.turn.StartTerrain == MapRules.OceanTerrain && this.turn.TargetTerrain != MapRules.OceanTerrain)
			{
				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				}
			}

			this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if ((Tables.UnitKindWord((sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) == 0 &&
				(byte)this.turn.CarrierKind != UnitKindTable.Carrier) || this.turn.Cargo == 0xffff)
			{
				return;
			}

			this.turn.CargoTally = 0;
			this.turn.Count = 0;

			while (true)
			{
				short member = (short)this.turn.Cargo;
				this.indexDest = (ushort)((member * UnitMemory.TroopStride) + (participantIdx * UnitMemory.ParticipantStride));
				this.turn.NextCargo = (ushort)this.troops.PullSigned(participantIdx, member, UnitMemory.FollowingInStack);

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
					{
						CarryUnit(participantIdx, troopIdx, member);
					}
				}

				this.turn.Cargo = this.turn.NextCargo;
				this.turn.CargoTally = (ushort)(this.turn.CargoTally + 1);

				if ((short)this.turn.CargoTally >= 0x14 || this.turn.Cargo == 0xffff)
				{
					return;
				}

				this.turn.CarrierKind = (ushort)((this.turn.CarrierKind & 0xff00) | this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

				if ((short)Tables.UnitKindWord((sbyte)(byte)this.turn.CarrierKind, UnitKindTable.FerryRoom) <= (short)this.turn.Count &&
					(byte)this.turn.CarrierKind != UnitKindTable.Carrier && participantIdx != 0)
				{
					return;
				}
			}
		}

		private void CarryUnit(short participantIdx, short troopIdx, short member)
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

			ushort roundsAway = this.troops.KindField(participantIdx, member, UnitKindTable.RoundsAway);
			this.indexDest = roundsAway;

			if (roundsAway != 0)
			{
				this.troops.Assign(participantIdx, member, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
			}

			this.turn.Count = (ushort)(this.turn.Count + 1);
		}

		private void ArriveAtTile(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			short person = this.Owner.World.PersonParticipantIdx;
			int personBit = 1 << (person & 0xff);

			if (((byte)this.turn.TargetImprovements & 1) != 0 && this.turn.TileOwner != (ushort)participantIdx)
			{
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
				{
					this.Owner.UnitOps.VisitEachUnitInStack(participantIdx, (short)this.turn.Troop, 0x1560);
				}
			}

			if (participantIdx != 0 || this.turn.TargetTerrain == MapRules.OceanTerrain)
			{
				return;
			}

			if (((sbyte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert] & personBit) == 0)
			{
				return;
			}

			this.indexDest = (ushort)((troopIdx * UnitMemory.TroopStride) + UnitMemory.Origin + UnitMemory.ShownToParticipants);

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & personBit) == 0 &&
				this.Owner.World.Towns[this.Owner.Helpers.FindNearestCity((short)this.turn.TargetHoriz, (short)this.turn.TargetVert)].ParticipantIdx == person)
			{
				byte sight = (byte)this.Owner.World.ChartSight[this.turn.TargetHoriz, this.turn.TargetVert];
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | sight));
			}
		}

		private void FinishMove(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;
			short person = this.Owner.World.PersonParticipantIdx;

			if ((this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << (person & 0xff))) != 0 || participantIdx == person ||
				(MapView.Revealed != 0 && participantIdx != 0))
			{
				if (participantIdx == person || (this.Owner.World.SessionPrefMarkers & 0x20) != 0)
				{
					this.Owner.UnitOps.PanMapToPosition(person, (short)this.turn.TargetHoriz, (short)this.turn.TargetVert);
					this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, (short)this.turn.Troop);

					if (participantIdx != person && MapView.WorldMapView != 1)
					{
						this.Owner.TextPainter.WaitForTime(30);
					}
				}
				else
				{
					if (this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, (short)this.turn.Troop) != 0)
					{
						this.Owner.TextPainter.WaitForTime(10);
					}
				}
			}

			this.turn.AITries = 0;

			if (((byte)this.turn.TargetImprovements & 1) != 0)
			{
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

			if (this.Owner.UnitOps.CountUnitsOfTypeInStack(participantIdx, (short)this.turn.Troop, 23) == 0)
			{
				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));
		}
	}
}
