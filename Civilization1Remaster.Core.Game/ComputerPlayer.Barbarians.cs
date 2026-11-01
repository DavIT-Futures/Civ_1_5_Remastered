using System;

namespace Civilization1Remaster.Core.Game
{
	public partial class ComputerPlayer
	{
		public ushort ManageBarbarianUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ManageBarbarianUnit({participantIndex}, {troopIndex})");

			ushort dataSegment = this.Processor.SegmentData.Word;
			ushort unitAddress = (ushort)(((ushort)participantIndex * 0x600) + ((ushort)troopIndex * 0xc));
			byte unitVerticalByte = this.Troops.PullAt(unitAddress, UnitMemory.Vertical);
			if (unitVerticalByte < 0x2 || unitVerticalByte >= 0x30)
			{
				this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
				return 0xffff;
			}

			ushort mayOccupyCurrent = this.Owner.UnitOperations.MayOccupyTile(
				participantIndex,
				(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Horizontal),
				(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Vertical));
			ushort unitHorizontal = this.Troops.PullAt(unitAddress, UnitMemory.Horizontal);
			ushort unitVertical = this.Troops.PullAt(unitAddress, UnitMemory.Vertical);
			byte unitKind = this.Troops.PullAt(unitAddress, UnitMemory.Kind);
			ushort unitSpecificationOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)unitKind);

			if (Tables.UnitKindWordAt(unitSpecificationOffset, UnitKindTable.TroopGroup) == 0x5)
			{
				ushort seaResult = this.ManageBarbarianSeaTransport(participantIndex, troopIndex, unitAddress, unitHorizontal, unitVertical);
				return seaResult;
			}

			ushort landResult = this.ManageBarbarianFieldUnit(participantIndex, troopIndex, unitAddress, unitHorizontal, unitVertical, mayOccupyCurrent);
			return landResult;
		}

		private ushort ManageBarbarianSeaTransport(short participantIndex, short troopIndex, ushort unitAddress, ushort unitHorizontal, ushort unitVertical)
		{
			if (this.Troops.PullAt(unitAddress, UnitMemory.FollowingInStack) == 0xff)
			{
				this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
				return 0x20;
			}

			ushort nearestCity = this.Owner.Helpers.FindNearestCity((short)unitHorizontal, (short)unitVertical);
			for (ushort direction = 1; (short)direction <= 8; direction++)
			{
				ushort directionOffset = (ushort)(direction << 1);
				ushort adjacentHorizontal = (ushort)this.Owner.Routing.TunePosition(
					(short)Tables.DirectionHorizontalAt(directionOffset) + (short)unitHorizontal);
				ushort adjacentVertical = (ushort)(Tables.DirectionVerticalAt(directionOffset) + unitVertical);

				if ((short)Search.FoundDistance <= 8 &&
					this.Owner.MapOperations.ObtainTerrainType((short)adjacentHorizontal, (short)adjacentVertical) != 0xa &&
					((byte)this.Owner.MapOperations.ObtainTileImprovements((short)adjacentHorizontal, (short)adjacentVertical) & 0x1) == 0 &&
					(ushort)(this.Owner.MapOperations.ObtainTileOwner((short)adjacentHorizontal, (short)adjacentVertical) + 1) == 0 &&
					(short)adjacentVertical > 1 &&
					(short)adjacentVertical < 0x30)
				{
					short sightMask = (short)(sbyte)(byte)this.Owner.World.ChartSight[adjacentHorizontal, adjacentVertical];
					ushort humanBit = (ushort)(1 << (byte)(this.Owner.World.PersonParticipantIndex & 0xff));
					if ((sightMask & (short)humanBit) != 0 &&
						this.Owner.World.Towns[nearestCity].ParticipantIndex == this.Owner.World.PersonParticipantIndex)
					{
						ushort cityContinent = this.Owner.MapOperations.ObtainContinentIdentifier(
							this.Owner.World.Towns[nearestCity].Place.X,
							this.Owner.World.Towns[nearestCity].Place.Y);
						ushort adjacentContinent = this.Owner.MapOperations.ObtainContinentIdentifier((short)adjacentHorizontal, (short)adjacentVertical);
						if (adjacentContinent == cityContinent)
						{
							byte humanBitByte = (byte)(1 << (byte)(this.Owner.World.PersonParticipantIndex & 0xff));
							this.Troops.AssignAt(unitAddress, UnitMemory.ShownToParticipants, (byte)(this.Troops.PullAt(unitAddress, UnitMemory.ShownToParticipants) | humanBitByte));

							this.Owner.UnitOperations.PanMapToPosition(this.Owner.World.PersonParticipantIndex, (short)adjacentHorizontal, (short)adjacentVertical);
							Screen.DialogStyle = 0x3;
							this.Owner.Words.Set("Barbarian raiding party\nlands near ");
							nearestCity = this.Owner.Helpers.FindNearestCity((short)adjacentHorizontal, (short)adjacentVertical);
							this.Owner.Conquest.ObtainCityLabel((short)nearestCity);
							this.Owner.Words.Append("!\nCitizens are alarmed.\n");
							this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 32);
						}
					}

					byte tileSight = (byte)this.Owner.World.ChartSight[adjacentHorizontal, adjacentVertical];
					this.Troops.AssignAt(unitAddress, UnitMemory.ShownToParticipants, (byte)(this.Troops.PullAt(unitAddress, UnitMemory.ShownToParticipants) | tileSight));
					this.Troops.AssignAt(unitAddress, UnitMemory.LeftoverSteps, 0x0);
					return 0x75;
				}

				if ((short)this.Owner.MapOperations.ObtainTileOwner((short)adjacentHorizontal, (short)adjacentVertical) > 0)
				{
					return direction;
				}
			}

			if (this.Troops.PullAt(unitAddress, UnitMemory.TravelHorizontal) == 0xff)
			{
				short bestScore = 0;
				ushort bestCity = 0;
				for (ushort cityIndex = 0; cityIndex < 0x80; cityIndex++)
				{
					if (this.Owner.World.Towns[cityIndex].ConditionMarker == 0xff)
					{
						continue;
					}

					ushort distance = (ushort)(this.Owner.Helpers.ObtainDistance(
						(short)unitHorizontal,
						this.Owner.World.Towns[cityIndex].Place.X,
						(short)unitVertical,
						this.Owner.World.Towns[cityIndex].Place.Y) + 1);
					ushort plunder = this.Owner.Conquest.ComputeCityPlunder((short)cityIndex);
					short score = CentralProcessor.SignedQuotient((short)(plunder + 0x32), (short)distance);
					if (score > bestScore)
					{
						bestScore = score;
						bestCity = cityIndex;
					}
				}

				if (bestScore == 0)
				{
					this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
					return 0x20;
				}

				this.Troops.AssignAt(unitAddress, UnitMemory.TravelHorizontal, (byte)(sbyte)this.Owner.World.Towns[bestCity].Place.X);
				this.Troops.AssignAt(unitAddress, UnitMemory.TravelVertical, (byte)(sbyte)this.Owner.World.Towns[bestCity].Place.Y);
			}

			return 0;
		}

		private ushort ManageBarbarianFieldUnit(
			short participantIndex,
			short troopIndex,
			ushort unitAddress,
			ushort unitHorizontal,
			ushort unitVertical,
			ushort mayOccupyCurrent)
		{
			ushort nearestCity = this.Owner.Helpers.FindNearestCity((short)unitHorizontal, (short)unitVertical);
			if (Search.FoundDistance == 0)
			{
				if (this.Troops.PullAt(unitAddress, UnitMemory.FollowingInStack) == 0xff)
				{
					return 0x66;
				}

				byte currentKind = this.Troops.PullAt(unitAddress, UnitMemory.Kind);
				ushort currentSpecificationOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)currentKind);
				if (Tables.UnitKindWordAt(currentSpecificationOffset, UnitKindTable.TroopGroup) == 0x2)
				{
					return 0x66;
				}
			}

			ushort targetDirectionHorizontal = 0xfffe;
			ushort targetDirectionVertical = 0xfffe;
			if (((byte)((byte)(sbyte)troopIndex + (byte)(this.Owner.World.RoundTally & 0xff)) & 0x3) == 0)
			{
				ushort cityContinent = this.Owner.MapOperations.ObtainContinentIdentifier(
					this.Owner.World.Towns[nearestCity].Place.X,
					this.Owner.World.Towns[nearestCity].Place.Y);
				ushort unitContinent = this.Owner.MapOperations.ObtainContinentIdentifier((short)unitHorizontal, (short)unitVertical);
				if (unitContinent != cityContinent || (short)Search.FoundDistance > 8)
				{
					this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
					return 0x20;
				}
			}

			if (nearestCity != 0xffff)
			{
				ushort cityContinent = this.Owner.MapOperations.ObtainContinentIdentifier(
					this.Owner.World.Towns[nearestCity].Place.X,
					this.Owner.World.Towns[nearestCity].Place.Y);
				ushort unitContinent = this.Owner.MapOperations.ObtainContinentIdentifier((short)unitHorizontal, (short)unitVertical);
				if (unitContinent == cityContinent)
				{
					ushort ownerMask = (ushort)(1 << (byte)this.Owner.World.Towns[nearestCity].ParticipantIndex);
					if ((ownerMask & (ushort)this.Owner.World.ParticipantMarkers) != 0)
					{
						targetDirectionHorizontal = (ushort)Math.Sign((short)((short)this.Owner.World.Towns[nearestCity].Place.X - (short)unitHorizontal));
						targetDirectionVertical = (ushort)Math.Sign((short)((short)this.Owner.World.Towns[nearestCity].Place.Y - (short)unitVertical));
					}
				}
			}

			ushort cityOwner = (ushort)this.Owner.World.Towns[nearestCity].ParticipantIndex;
			if ((ushort)participantIndex == cityOwner ||
				(this.Owner.World.Participants[cityOwner].TownTally < 2 && this.Owner.World.Participants[cityOwner].Gold < 100))
			{
				targetDirectionHorizontal = 0xfffe;
				targetDirectionVertical = 0xfffe;
			}

			if (targetDirectionHorizontal != 0xfffe)
			{
				this.Troops.AssignAt(unitAddress, UnitMemory.TravelHorizontal, (byte)(sbyte)this.Owner.World.Towns[nearestCity].Place.X);
				this.Troops.AssignAt(unitAddress, UnitMemory.TravelVertical, (byte)(sbyte)this.Owner.World.Towns[nearestCity].Place.Y);
				ushort gotoDirection = this.Owner.Routing.CarryOutGoto(participantIndex, troopIndex);
				this.Troops.AssignAt(unitAddress, UnitMemory.TravelHeading, (byte)(gotoDirection & 0xff));
			}

			this.Troops.AssignAt(unitAddress, UnitMemory.TravelHorizontal, 0xff);
			if (this.Troops.PullAt(unitAddress, UnitMemory.Kind) == 0x1a)
			{
				ushort currentTerrain = this.Owner.MapOperations.ObtainTerrainType(
					(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Horizontal),
					(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Vertical));
				byte nextInStack = this.Troops.PullAt(unitAddress, UnitMemory.FollowingInStack);
				if (nextInStack != 0xff && currentTerrain != 0xa)
				{
					ushort nextUnitOffset = (ushort)((short)(sbyte)0xc * (short)(sbyte)nextInStack);
					ushort nextUnitAddress = (ushort)(((ushort)participantIndex * 0x600) + nextUnitOffset);
					if (this.Troops.PullAt(nextUnitAddress, UnitMemory.Kind) != 0x1a)
					{
						return 0x20;
					}
				}

				short cityDistance = (short)Search.FoundDistance;
				if (currentTerrain == 0xa)
				{
					if (cityDistance < 3)
					{
						this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
						return 0x20;
					}
				}
				else
				{
					ushort nearestUnit = this.Owner.Helpers.FindNearestUnit(
						participantIndex,
						troopIndex,
						(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Horizontal),
						(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Vertical));
					if ((short)Search.FoundDistance <= 3)
					{
						ushort nearestUnitAddress = (ushort)(((ushort)participantIndex * 0x600) + (nearestUnit * 0xc));
						if (this.Troops.PullAt(nearestUnitAddress, UnitMemory.Kind) != 0x1a)
						{
							this.Troops.AssignAt(unitAddress, UnitMemory.TravelHorizontal, this.Troops.PullAt(nearestUnitAddress, UnitMemory.Horizontal));
							this.Troops.AssignAt(unitAddress, UnitMemory.TravelVertical, this.Troops.PullAt(nearestUnitAddress, UnitMemory.Vertical));
							return 0;
						}
					}

					short surviveDistance = (short)(((ushort)this.Owner.World.RoundTally & 0x7) + 4);
					if (surviveDistance <= cityDistance)
					{
						this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
						return 0x20;
					}
				}
			}

			ushort bestDirection = 0;
			short bestDirectionScore = unchecked((short)0xfc19);
			for (ushort direction = 1; (short)direction < 9; direction++)
			{
				ushort directionOffset = (ushort)(direction << 1);
				ushort adjacentHorizontal = (ushort)this.Owner.Routing.TunePosition(
					(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.Horizontal) +
					(short)Tables.DirectionHorizontalAt(directionOffset));
				ushort adjacentVertical = (ushort)(this.Troops.PullAt(unitAddress, UnitMemory.Vertical) +
					Tables.DirectionVerticalAt(directionOffset));

				ushort adjacentTerrain = this.Owner.MapOperations.ObtainTerrainType((short)adjacentHorizontal, (short)adjacentVertical);
				ushort isOcean = adjacentTerrain == 0xa ? (ushort)1 : (ushort)0;

				byte currentKind = this.Troops.PullAt(unitAddress, UnitMemory.Kind);
				ushort currentKindOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)currentKind);
				ushort isSeaDomain = Tables.UnitKindWordAt(currentKindOffset, UnitKindTable.LandGroup) == 0x2 ? (ushort)1 : (ushort)0;
				if (isSeaDomain != isOcean)
				{
					continue;
				}

				if (this.Owner.MapOperations.CheckMapLimits((short)adjacentHorizontal, (short)adjacentVertical) == 0)
				{
					continue;
				}

				ushort unitAtAdjacent = this.Owner.MapOperations.ObtainUnitAtTile((short)adjacentHorizontal, (short)adjacentVertical);
				short directionScore = 0;

				if (this.Troops.PullAt(unitAddress, UnitMemory.Kind) == 0x1a)
				{
					if (unitAtAdjacent != 0xffff)
					{
						ushort unitOwnerAtAdjacent = Search.FoundPlayer;
						directionScore = (short)(directionScore + ((ushort)participantIndex == unitOwnerAtAdjacent ? (short)0x63 : unchecked((short)0xff9d)));
					}

					this.Owner.Helpers.FindNearestCity((short)adjacentHorizontal, (short)adjacentVertical);
					short randomBonus = (short)this.Owner.Runtime.Dice.DrawInteger(4);
					short defenseBonus = (short)(sbyte)Tables.TerrainField(adjacentTerrain, TerrainTable.MoveCost);
					short distancePenalty = (short)(Search.FoundDistance << 2);
					directionScore = (short)(directionScore + defenseBonus + randomBonus + distancePenalty);
				}
				else
				{
					ushort adjacentImprovements = this.Owner.MapOperations.ObtainTileImprovements((short)adjacentHorizontal, (short)adjacentVertical);
					if (((byte)adjacentImprovements & 0x1) != 0)
					{
						ushort adjacentOwnerBits = this.Owner.MapOperations.ObtainTileOwnerBits((short)adjacentHorizontal, (short)adjacentVertical);
						if (adjacentOwnerBits != (ushort)participantIndex)
						{
							if (this.Owner.MapOperations.ObtainTerrainType((short)unitHorizontal, (short)unitVertical) != 0xa)
							{
								return direction;
							}

							continue;
						}
					}

					ushort randomRoll = (ushort)this.Owner.Runtime.Dice.DrawInteger(6);
					ushort improvementCheck = this.Owner.MapOperations.ObtainTileImprovements((short)adjacentHorizontal, (short)adjacentVertical);
					ushort improvementBonus = ((byte)improvementCheck & 0x6) != 0 ? (ushort)0x6 : (ushort)0;
					ushort ownerBits = this.Owner.MapOperations.ObtainTileOwnerBits((short)adjacentHorizontal, (short)adjacentVertical);
					ushort ownerBonus = ownerBits == (ushort)participantIndex ? (ushort)0 : (ushort)0x4;
					directionScore = (short)(ownerBonus + improvementBonus + randomRoll);

					if (unitAtAdjacent != 0xffff)
					{
						ushort adjacentUnitOwner = Search.FoundPlayer;
						if ((ushort)participantIndex == adjacentUnitOwner)
						{
							if (this.Owner.MapOperations.ObtainTerrainType((short)unitHorizontal, (short)unitVertical) != 0xa)
							{
								continue;
							}

							directionScore = (short)(directionScore - 0x14);
						}
						else
						{
							if (this.Owner.MapOperations.ObtainTerrainType((short)unitHorizontal, (short)unitVertical) == 0xa)
							{
								continue;
							}

							ushort adjacentUnitAddress = (ushort)((adjacentUnitOwner * 0x600) + (unitAtAdjacent * 0xc));
							if (this.Troops.PullAt(adjacentUnitAddress, UnitMemory.Kind) != 0x1a)
							{
								directionScore = (short)(directionScore + 0x63);
							}
						}
					}

					if (mayOccupyCurrent != 0)
					{
						byte kindNow = this.Troops.PullAt(unitAddress, UnitMemory.Kind);
						ushort specificationNow = (ushort)((short)(sbyte)0x22 * (short)(sbyte)kindNow);
						if (Tables.UnitKindWordAt(specificationNow, UnitKindTable.LandGroup) == 0 &&
							unitAtAdjacent == 0xffff &&
							this.Owner.UnitOperations.MayOccupyTile(participantIndex, (short)adjacentHorizontal, (short)adjacentVertical) != 0)
						{
							continue;
						}
					}

					short previousDirection = (short)(sbyte)this.Troops.PullAt(unitAddress, UnitMemory.TravelHeading);
					if ((ushort)previousDirection == direction)
					{
						directionScore = (short)(directionScore + 0x6);
					}

					if (Tables.DirectionHorizontalAt(directionOffset) == targetDirectionHorizontal)
					{
						directionScore = (short)(directionScore + 0x2);
					}

					if (Tables.DirectionVerticalAt(directionOffset) == targetDirectionVertical)
					{
						directionScore = (short)(directionScore + 0x2);
					}
				}

				if (directionScore > bestDirectionScore)
				{
					bestDirectionScore = directionScore;
					bestDirection = direction;
				}
			}

			ushort currentImprovements = this.Owner.MapOperations.ObtainTileImprovements((short)unitHorizontal, (short)unitVertical);
			if (((byte)currentImprovements & 0x1) != 0 &&
				this.Troops.PullAt(unitAddress, UnitMemory.FollowingInStack) == 0xff &&
				bestDirectionScore < 0x63)
			{
				return 0x20;
			}

			this.Troops.AssignAt(unitAddress, UnitMemory.TravelHeading, (byte)(bestDirection & 0xff));
			return bestDirection;
		}
	}
}
