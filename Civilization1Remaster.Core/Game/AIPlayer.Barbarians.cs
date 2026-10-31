using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.UI;

namespace Civilization1Remaster.Game
{
	/// <summary>The barbarian units</summary>
	partial class AIPlayer
	{
		/// <summary>
		/// The move of a barbarian unit for ShiftAIUnit: a unit in the rows 0 - 1 or from 48 on is taken off; a ship (group 5)
		/// carries its troops to land, the others attack, take or wander (ManageBarbarianSeaTransport,
		/// ManageBarbarianFieldUnit). Returns the order.
		/// </summary>
		public ushort ManageBarbarianUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"ManageBarbarianUnit({participantIdx}, {troopIdx})");

			ushort dataSeg = this.processor.SegData.Value16;
			ushort unitAddr = (ushort)((ushort)participantIdx * 0x600 + (ushort)troopIdx * 0xc);
			byte unitYByte = this.Troops.PullAt(unitAddr, UnitMemory.Vert);
			if (unitYByte < 0x2 || unitYByte >= 0x30)
			{
				// ShiftAIUnit takes the result as the order: no order (ClassicBarbarianOrder: what EliminateUnit leaves in AX,
				// 0xffff when it repaints the tile and no unit is left there, else the value computed before it)
				if (!this.owner.TurnHooks.ClassicBarbarianOrder)
				{
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					this.processor.Journal.CloseScope("ManageBarbarianUnit");
					return 0xffff;
				}

				this.processor.RegAcc.Value16 = (ushort)(((0x600 * (ushort)participantIdx) & 0xff00) | unitYByte);
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
				this.processor.Journal.CloseScope("ManageBarbarianUnit");
				return this.processor.RegAcc.Value16;
			}

			ushort mayOccupyCurrent = this.owner.UnitOps.MayOccupyTile(
				participantIdx,
				(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz),
				(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Vert));
			ushort unitX = this.Troops.PullAt(unitAddr, UnitMemory.Horiz);
			ushort unitY = this.Troops.PullAt(unitAddr, UnitMemory.Vert);
			byte unitKind = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
			ushort unitSpecOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)unitKind);

			if (UnitKindWordAt(unitSpecOffset, UnitKindTable.TroopGroup) == 0x5)
			{
				ushort seaResult = this.ManageBarbarianSeaTransport(participantIdx, troopIdx, unitAddr, unitX, unitY);
				this.processor.Journal.CloseScope("ManageBarbarianUnit");
				return seaResult;
			}

			ushort landResult = this.ManageBarbarianFieldUnit(participantIdx, troopIdx, unitAddr, unitX, unitY, mayOccupyCurrent);
			this.processor.Journal.CloseScope("ManageBarbarianUnit");
			return landResult;
		}

		private ushort ManageBarbarianSeaTransport(short participantIdx, short troopIdx, ushort unitAddr, ushort unitX, ushort unitY)
		{
			ushort dataSeg = this.processor.SegData.Value16;
			if (this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack) == 0xff)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
				return 0x20;
			}

			ushort nearestCity = this.owner.Helpers.LocateNearestCity((short)unitX, (short)unitY);
			for (ushort dir = 1; (short)dir <= 8; dir++)
			{
				ushort dirOffset = (ushort)(dir << 1);
				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					(short)DirectionHorizAt(dirOffset) + (short)unitX);
				ushort adjY = (ushort)(DirectionVertAt(dirOffset) + unitY);

				if ((short)FoundDistance <= 8 &&
					this.owner.MapOps.ObtainTerrainType((short)adjX, (short)adjY) != 0xa &&
					((byte)this.owner.MapOps.ObtainTileImprovements((short)adjX, (short)adjY) & 0x1) == 0 &&
					(ushort)(this.owner.MapOps.ObtainTileOwner((short)adjX, (short)adjY) + 1) == 0 &&
					(short)adjY > 1 &&
					(short)adjY < 0x30)
				{
					short sightMask = (short)(sbyte)(byte)this.owner.World.ChartSight[adjX, adjY];
					ushort humanBit = (ushort)(1 << (byte)(this.owner.World.PersonParticipantIdx & 0xff));
					if ((sightMask & (short)humanBit) != 0 &&
						this.owner.World.Towns[nearestCity].ParticipantIdx == this.owner.World.PersonParticipantIdx)
					{
						ushort cityContinent = this.owner.MapOps.ObtainContinentID(
							this.owner.World.Towns[nearestCity].Place.X,
							this.owner.World.Towns[nearestCity].Place.Y);
						ushort adjContinent = this.owner.MapOps.ObtainContinentID((short)adjX, (short)adjY);
						if (adjContinent == cityContinent)
						{
							byte humanBitByte = (byte)(1 << (byte)(this.owner.World.PersonParticipantIdx & 0xff));
							this.Troops.AssignAt(unitAddr, UnitMemory.ShownToParticipants, (byte)(this.Troops.PullAt(unitAddr, UnitMemory.ShownToParticipants) | humanBitByte));

							this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx, (short)adjX, (short)adjY);
							DialogStyle = 0x3;
							this.owner.Words.Set("Barbarian raiding party\nlands near ");
							nearestCity = this.owner.Helpers.LocateNearestCity((short)adjX, (short)adjY);
							this.owner.Conquest.ObtainCityLabel((short)nearestCity);
							this.owner.Words.Append("!\nCitizens are alarmed.\n");
							this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 32);
						}
					}

					byte tileSight = (byte)this.owner.World.ChartSight[adjX, adjY];
					this.Troops.AssignAt(unitAddr, UnitMemory.ShownToParticipants, (byte)(this.Troops.PullAt(unitAddr, UnitMemory.ShownToParticipants) | tileSight));
					this.Troops.AssignAt(unitAddr, UnitMemory.LeftoverSteps, 0x0);
					return 0x75;
				}

				if ((short)this.owner.MapOps.ObtainTileOwner((short)adjX, (short)adjY) > 0)
				{
					return dir;
				}
			}

			if (this.Troops.PullAt(unitAddr, UnitMemory.TravelHoriz) == 0xff)
			{
				short bestScore = 0;
				ushort bestCity = 0;
				for (ushort cityIdx = 0; cityIdx < 0x80; cityIdx++)
				{
					if (this.owner.World.Towns[cityIdx].ConditionMarker == 0xff)
					{
						continue;
					}

					ushort dist = (ushort)(this.owner.Helpers.ObtainDistance(
						(short)unitX,
						this.owner.World.Towns[cityIdx].Place.X,
						(short)unitY,
						this.owner.World.Towns[cityIdx].Place.Y) + 1);
					ushort plunder = this.owner.Conquest.ComputeCityPlunder((short)cityIdx);
					short score = CPU.SignedQuotient((short)(plunder + 0x32), (short)dist);
					if (score > bestScore)
					{
						bestScore = score;
						bestCity = cityIdx;
					}
				}

				if (bestScore == 0)
				{
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					return 0x20;
				}

				this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, (byte)(sbyte)this.owner.World.Towns[bestCity].Place.X);
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelVert, (byte)(sbyte)this.owner.World.Towns[bestCity].Place.Y);
			}

			return 0;
		}

		private ushort ManageBarbarianFieldUnit(
			short participantIdx,
			short troopIdx,
			ushort unitAddr,
			ushort unitX,
			ushort unitY,
			ushort mayOccupyCurrent)
		{
			ushort dataSeg = this.processor.SegData.Value16;
			ushort nearestCity = this.owner.Helpers.LocateNearestCity((short)unitX, (short)unitY);
			if (FoundDistance == 0)
			{
				if (this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack) == 0xff)
				{
					return 0x66;
				}

				byte curKind = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
				ushort curSpecOffset = (ushort)((short)(sbyte)0x22 * (short)(sbyte)curKind);
				if (UnitKindWordAt(curSpecOffset, UnitKindTable.TroopGroup) == 0x2)
				{
					return 0x66;
				}
			}

			ushort targetDirX = 0xfffe;
			ushort targetDirY = 0xfffe;
			if (((byte)((byte)(sbyte)troopIdx + (byte)(this.owner.World.RoundTally & 0xff)) & 0x3) == 0)
			{
				ushort cityContinent = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[nearestCity].Place.X,
					this.owner.World.Towns[nearestCity].Place.Y);
				ushort unitContinent = this.owner.MapOps.ObtainContinentID((short)unitX, (short)unitY);
				if (unitContinent != cityContinent || (short)FoundDistance > 8)
				{
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					return 0x20;
				}
			}

			if (nearestCity != 0xffff)
			{
				ushort cityContinent = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[nearestCity].Place.X,
					this.owner.World.Towns[nearestCity].Place.Y);
				ushort unitContinent = this.owner.MapOps.ObtainContinentID((short)unitX, (short)unitY);
				if (unitContinent == cityContinent)
				{
					ushort ownerMask = (ushort)(1 << (byte)this.owner.World.Towns[nearestCity].ParticipantIdx);
					if ((ownerMask & (ushort)this.owner.World.ParticipantMarkers) != 0)
					{
						targetDirX = (ushort)Math.Sign((short)((short)this.owner.World.Towns[nearestCity].Place.X - (short)unitX));
						targetDirY = (ushort)Math.Sign((short)((short)this.owner.World.Towns[nearestCity].Place.Y - (short)unitY));
					}
				}
			}

			ushort cityOwner = (ushort)this.owner.World.Towns[nearestCity].ParticipantIdx;
			if ((ushort)participantIdx == cityOwner ||
				(this.owner.World.Participants[cityOwner].TownTally < 2 && this.owner.World.Participants[cityOwner].Gold < 100))
			{
				targetDirX = 0xfffe;
				targetDirY = 0xfffe;
			}

			if (targetDirX != 0xfffe)
			{
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, (byte)(sbyte)this.owner.World.Towns[nearestCity].Place.X);
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelVert, (byte)(sbyte)this.owner.World.Towns[nearestCity].Place.Y);
				ushort gotoDir = this.owner.Routing.CarryOutGoto(participantIdx, troopIdx);
				this.Troops.AssignAt(unitAddr, UnitMemory.TravelHeading, (byte)(gotoDir & 0xff));
			}

			this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, 0xff);
			if (this.Troops.PullAt(unitAddr, UnitMemory.Kind) == 0x1a)
			{
				ushort currentTerrain = this.owner.MapOps.ObtainTerrainType(
					(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz),
					(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Vert));
				byte nextInStack = this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack);
				if (nextInStack != 0xff && currentTerrain != 0xa)
				{
					ushort nextUnitOffset = (ushort)((short)(sbyte)0xc * (short)(sbyte)nextInStack);
					ushort nextUnitAddr = (ushort)((ushort)participantIdx * 0x600 + nextUnitOffset);
					if (this.Troops.PullAt(nextUnitAddr, UnitMemory.Kind) != 0x1a)
					{
						return 0x20;
					}
				}

				short cityDist = (short)FoundDistance;
				if (currentTerrain == 0xa)
				{
					if (cityDist < 3)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						return 0x20;
					}
				}
				else
				{
					ushort nearestUnit = this.owner.Helpers.LocateNearestUnit(
						participantIdx,
						troopIdx,
						(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz),
						(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Vert));
					if ((short)FoundDistance <= 3)
					{
						ushort nearestUnitAddr = (ushort)((ushort)participantIdx * 0x600 + nearestUnit * 0xc);
						if (this.Troops.PullAt(nearestUnitAddr, UnitMemory.Kind) != 0x1a)
						{
							this.Troops.AssignAt(unitAddr, UnitMemory.TravelHoriz, this.Troops.PullAt(nearestUnitAddr, UnitMemory.Horiz));
							this.Troops.AssignAt(unitAddr, UnitMemory.TravelVert, this.Troops.PullAt(nearestUnitAddr, UnitMemory.Vert));
							return 0;
						}
					}

					short surviveDist = (short)(((ushort)this.owner.World.RoundTally & 0x7) + 4);
					if (surviveDist <= cityDist)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						return 0x20;
					}
				}
			}

			ushort bestDir = 0;
			short bestDirScore = unchecked((short)0xfc19);
			for (ushort dir = 1; (short)dir < 9; dir++)
			{
				ushort dirOffset = (ushort)(dir << 1);
				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.Horiz) +
					(short)DirectionHorizAt(dirOffset));
				ushort adjY = (ushort)(this.Troops.PullAt(unitAddr, UnitMemory.Vert) +
					DirectionVertAt(dirOffset));

				ushort adjTerrain = this.owner.MapOps.ObtainTerrainType((short)adjX, (short)adjY);
				ushort isOcean = adjTerrain == 0xa ? (ushort)1 : (ushort)0;

				byte curKind = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
				ushort curSpecOff = (ushort)((short)(sbyte)0x22 * (short)(sbyte)curKind);
				ushort isSeaDomain = UnitKindWordAt(curSpecOff, UnitKindTable.LandGroup) == 0x2 ? (ushort)1 : (ushort)0;
				if (isSeaDomain != isOcean)
				{
					continue;
				}

				if (this.owner.MapOps.VerifyMapLimits((short)adjX, (short)adjY) == 0)
				{
					continue;
				}

				ushort unitAtAdj = this.owner.MapOps.ObtainUnitAtTile((short)adjX, (short)adjY);
				short dirScore = 0;

				if (this.Troops.PullAt(unitAddr, UnitMemory.Kind) == 0x1a)
				{
					if (unitAtAdj != 0xffff)
					{
						ushort unitOwnerAtAdj = FoundPlayer;
						dirScore = (short)(dirScore + ((ushort)participantIdx == unitOwnerAtAdj ? (short)0x63 : unchecked((short)0xff9d)));
					}

					this.owner.Helpers.LocateNearestCity((short)adjX, (short)adjY);
					short randBonus = (short)this.owner.Libc.Dice.DrawInt(4);
					short defenseBonus = (short)(sbyte)TerrainField(adjTerrain, TerrainTable.MoveCost);
					short distPenalty = (short)(FoundDistance << 2);
					dirScore = (short)(dirScore + defenseBonus + randBonus + distPenalty);
				}
				else
				{
					ushort adjImprovements = this.owner.MapOps.ObtainTileImprovements((short)adjX, (short)adjY);
					if (((byte)adjImprovements & 0x1) != 0)
					{
						ushort adjOwnerBits = this.owner.MapOps.ObtainTileOwnerBits((short)adjX, (short)adjY);
						if (adjOwnerBits != (ushort)participantIdx)
						{
							if (this.owner.MapOps.ObtainTerrainType((short)unitX, (short)unitY) != 0xa)
							{
								return dir;
							}

							continue;
						}
					}

					ushort randRoll = (ushort)this.owner.Libc.Dice.DrawInt(6);
					ushort impCheck = this.owner.MapOps.ObtainTileImprovements((short)adjX, (short)adjY);
					ushort impBonus = ((byte)impCheck & 0x6) != 0 ? (ushort)0x6 : (ushort)0;
					ushort ownerBits = this.owner.MapOps.ObtainTileOwnerBits((short)adjX, (short)adjY);
					ushort ownerBonus = ownerBits == (ushort)participantIdx ? (ushort)0 : (ushort)0x4;
					dirScore = (short)(ownerBonus + impBonus + randRoll);

					if (unitAtAdj != 0xffff)
					{
						ushort adjUnitOwner = FoundPlayer;
						if ((ushort)participantIdx == adjUnitOwner)
						{
							if (this.owner.MapOps.ObtainTerrainType((short)unitX, (short)unitY) != 0xa)
							{
								continue;
							}

							dirScore = (short)(dirScore - 0x14);
						}
						else
						{
							if (this.owner.MapOps.ObtainTerrainType((short)unitX, (short)unitY) == 0xa)
							{
								continue;
							}

							ushort adjUnitAddr = (ushort)(adjUnitOwner * 0x600 + unitAtAdj * 0xc);
							if (this.Troops.PullAt(adjUnitAddr, UnitMemory.Kind) != 0x1a)
							{
								dirScore = (short)(dirScore + 0x63);
							}
						}
					}

					if (mayOccupyCurrent != 0)
					{
						byte kindNow = this.Troops.PullAt(unitAddr, UnitMemory.Kind);
						ushort specNow = (ushort)((short)(sbyte)0x22 * (short)(sbyte)kindNow);
						if (UnitKindWordAt(specNow, UnitKindTable.LandGroup) == 0 &&
							unitAtAdj == 0xffff &&
							this.owner.UnitOps.MayOccupyTile(participantIdx, (short)adjX, (short)adjY) != 0)
						{
							continue;
						}
					}

					short prevDir = (short)(sbyte)this.Troops.PullAt(unitAddr, UnitMemory.TravelHeading);
					if ((ushort)prevDir == dir)
					{
						dirScore = (short)(dirScore + 0x6);
					}

					if (DirectionHorizAt(dirOffset) == targetDirX)
					{
						dirScore = (short)(dirScore + 0x2);
					}

					if (DirectionVertAt(dirOffset) == targetDirY)
					{
						dirScore = (short)(dirScore + 0x2);
					}
				}

				if (dirScore > bestDirScore)
				{
					bestDirScore = dirScore;
					bestDir = dir;
				}
			}

			ushort curImprovements = this.owner.MapOps.ObtainTileImprovements((short)unitX, (short)unitY);
			if (((byte)curImprovements & 0x1) != 0 &&
				this.Troops.PullAt(unitAddr, UnitMemory.FollowingInStack) == 0xff &&
				bestDirScore < 0x63)
			{
				return 0x20;
			}

			this.Troops.AssignAt(unitAddr, UnitMemory.TravelHeading, (byte)(bestDir & 0xff));
			return bestDir;
		}
	}
}
