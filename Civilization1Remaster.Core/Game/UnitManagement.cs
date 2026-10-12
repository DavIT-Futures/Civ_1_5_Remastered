using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1866.
	/// </summary>
	public class UnitManagement
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public UnitManagement(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// Puts the units kept in a city of the human player (KeepUnitsInCityList) back on the map, fortified
		/// </summary>
		public void ConstructUnitsFromCityList(short townIdx)
		{
			this.processor.Journal.OpenScope($"ConstructUnitsFromCityList({townIdx})");

			City town = this.owner.World.Towns[townIdx];

			if (town.ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				// The original keeps SI and DI for its caller
				ushort savedSource = this.processor.RegSource.Value16;
				ushort savedDest = this.processor.RegDest.Value16;

				this.processor.EmitGlobalWord(0x20f4, 0x1);

				for (int keptIdx = 0; keptIdx < 2; keptIdx++)
				{
					sbyte kept = town.Unfamiliar[keptIdx];

					if (kept == -1)
						continue;

					ushort troopIdx = ConstructUnit(town.ParticipantIdx, (short)(kept & 0x3f), town.Place.X, town.Place.Y);

					if (troopIdx == 0xffff)
						continue;

					byte status = (byte)(this.troops.Pull(town.ParticipantIdx, troopIdx, UnitMemory.Status) | (int)UnitStatusEnum.Fortified);

					if ((kept & KeptVeteran) != 0)
						status |= (byte)UnitStatusEnum.Veteran;

					this.troops.Assign(town.ParticipantIdx, troopIdx, UnitMemory.Status, status);
					town.Unfamiliar[keptIdx] = -1;
				}

				this.processor.EmitGlobalWord(0x20f4, 0x0);
				town.ConditionMarker |= KeptUnitsCondition;

				this.processor.RegSource.Value16 = savedSource;
				this.processor.RegDest.Value16 = savedDest;
			}

			this.processor.Journal.CloseScope("ConstructUnitsFromCityList");
		}

		/// <summary>Bit of a kept unit type (City.Unfamiliar): the unit was a veteran</summary>
		private const sbyte KeptVeteran = 0x40;

		/// <summary>Bit of City.ConditionMarker changed when the kept units are put back or taken away</summary>
		private const byte KeptUnitsCondition = 4;

		/// <summary>
		/// A city of the human player keeps up to two of its fortified home units standing in it: they are taken off the map and
		/// their types (with the veteran bit) are stored in City.Unfamiliar. The first such unit stays on the map.
		/// Losses are not counted meanwhile (0x70d8).
		/// </summary>
		public void KeepUnitsInCityList(short townIdx)
		{
			this.processor.Journal.OpenScope($"KeepUnitsInCityList({townIdx})");

			City town = this.owner.World.Towns[townIdx];
			short participantIdx = town.ParticipantIdx;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				// The original keeps SI and DI for its caller
				ushort savedSource = this.processor.RegSource.Value16;
				ushort savedDest = this.processor.RegDest.Value16;

				ushort countLosses = this.processor.PullGlobalWord(0x70d8);
				this.processor.EmitGlobalWord(0x70d8, 0);
				this.processor.EmitGlobalWord(0x20f4, 0x1);

				bool firstSeen = false;

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!IsFortifiedHomeUnitIn(town, townIdx, participantIdx, troopIdx))
						continue;

					if (!firstSeen)
					{
						firstSeen = true;
						continue;
					}

					int keptIdx = town.Unfamiliar[0] == -1 ? 0 : 1;

					if (keptIdx == 1 && town.Unfamiliar[1] != -1)
						break;

					town.Unfamiliar[keptIdx] = (sbyte)this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Veteran) != 0)
						town.Unfamiliar[keptIdx] |= KeptVeteran;

					EliminateUnit(participantIdx, troopIdx);
				}

				this.processor.EmitGlobalWord(0x20f4, 0x0);
				this.processor.EmitGlobalWord(0x70d8, countLosses);
				town.ConditionMarker &= unchecked((byte)~KeptUnitsCondition);

				this.processor.RegSource.Value16 = savedSource;
				this.processor.RegDest.Value16 = savedDest;
			}

			this.processor.Journal.CloseScope("KeepUnitsInCityList");
		}

		/// <summary>A fortified unit of the city standing in it</summary>
		private bool IsFortifiedHomeUnitIn(City town, short townIdx, short participantIdx, short troopIdx)
		{
			return this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) == (byte)(sbyte)town.Place.X &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) == (byte)(sbyte)town.Place.Y &&
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == townIdx &&
				(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Fortified) != 0;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="flag"></param>
		public void ShiftUnit(int horizLoc, int vertLoc, short participantIdx, short troopIdx, ushort marker)
		{
			this.processor.Journal.OpenScope($"ShiftUnit({horizLoc}, {vertLoc}, {participantIdx}, {troopIdx}, {marker})");

			// function body
			this.processor.EnterFrame(0x16);
			this.processor.PreserveIndexRegisters();

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L02af;

			this.processor.EmitLocalWord(0xa, 0x0);

		L0205:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x9);
			if (this.processor.FlagSet.IsLess) goto L0266;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1144)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L02a3;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0266;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L02a3;

		L0266:
			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L02a3;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
				this.processor.PullLocalWord(0x12)] |= (sbyte)this.processor.RegCount.LowByte;

			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

		L02a3:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x19);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02af;
			goto L0205;

		L02af:
			this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, 0x1);
			goto L0381;

		L02c8:
			this.processor.RegAcc.Value16 = 0x1;

		L02cb:
			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullGlobalSignedWord(0xd7f0),
				(short)horizLoc, (short)vertLoc, 2, (short)this.processor.RegAcc.Value16);

		L02e2:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0329;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0329;

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L0329;

			this.owner.Bot.SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				3, 2);

		L0329:
			if (marker == 0)
				goto L037e;
			
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L033e;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L037e;

		L033e:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual)
			{
				this.processor.RegAcc.Value16 = 0x1;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0x2;
			}
		
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x10) + 0xf0,
				this.processor.PullLocalSignedWord(0x12),
				this.processor.RegAcc.Value16);

		L037e:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L0381:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x9);
			if (this.processor.FlagSet.IsLess) goto L038a;
			goto L06cc;

		L038a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L037e;

			if (participantIdx != 0)
			{
				this.processor.RegAcc.LowByte = 0x1;
				this.processor.RegCount.LowByte = (byte)participantIdx;
				this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
				this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
					this.processor.PullLocalWord(0x12)] |= (sbyte)this.processor.RegCount.LowByte;
			}

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L03f9;
			goto L04bd;

		L03f9:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			if ((short)this.processor.RegAcc.Value16 == participantIdx)
				goto L04bd;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			ConstructUnitsFromCityList((short)this.processor.RegAcc.Value16);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x8));
				this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x8)].TrueExtent;
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ShownExtent = (sbyte)this.processor.RegAcc.LowByte;
			}

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L04bd;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x14);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullLocalWord(0x14)] & 2) != 0)
				goto L04bd;

			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				1, 4);

			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				2, 2);

		L04bd:
			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L04d6;
			goto L0329;

		L04d6:
			if (participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
				goto L0329;

			this.processor.TestBitsByte(this.processor.PullLocalByte(0xe), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0503;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0xd7f0);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L0503:
			VisitEachUnitInStack(this.processor.PullGlobalSignedWord(0xd7f0), this.processor.PullLocalSignedWord(0x2), 0x14a2);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0532;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0548;

		L0532:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L0548:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0580;
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0580;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0595;

		L0580:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L0595:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L05b7;
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L05b7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05d9;
			goto L02e2;

		L05d9:
			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L05ef;
			goto L02e2;

		L05ef:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0605;
			goto L02e2;

		L0605:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0628;
			goto L02e2;

		L0628:
			this.owner.Politics.BargainPeaceTreaty(participantIdx, this.processor.PullGlobalSignedWord(0xd7f0), horizLoc, vertLoc);
			
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L06a3;

			this.processor.EmitLocalWord(0x6, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L068b;
			this.processor.ScaleIndex(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L068b;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L0686;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L068b;

		L0686:
			this.processor.EmitLocalWord(0x6, 0x3);

		L068b:
			this.owner.Bot.SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				1, this.processor.PullLocalSignedWord(0x6));

		L06a3:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsNotEqual) goto L06bd;
			goto L02e2;

		L06bd:
			if (participantIdx == 0)
				goto L02c8;

			this.processor.RegAcc.Value16 = 0x2;
			goto L02cb;

		L06cc:
			this.processor.EmitLocalWord(0xa, 0x9);
			goto L08bc;

		L06d4:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x10) + 0xf0,
				this.processor.PullLocalSignedWord(0x12),
				2);

		L06ea:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L06f3;
			goto L0857;

		L06f3:			
			if (participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
				goto L0857;

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)), 0x16);
			if (this.processor.FlagSet.IsNotEqual) goto L0718;
			goto L0857;

		L0718:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0xd7f0));
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x22, this.processor.PullLocalWord(0x4));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1144)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0743;
			goto L07f0;

		L0743:
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0760;

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0760;
			goto L07f0;

		L0760:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfe));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xe), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L079d;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0xd7f0);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L079d:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L07b3;
			goto L0857;

		L07b3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07bd;
			goto L0857;

		L07bd:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L0857;

			this.owner.Bot.SendUnitToStrategicLocation(this.processor.PullGlobalSignedWord(0xd7f0),
				(short)horizLoc, (short)vertLoc, 3, 2);

			goto L0857;

		L07f0:
			if (this.processor.PullGlobalSignedWord(0xd7f0) != this.owner.World.PersonParticipantIdx)
				goto L0857;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0857;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[horizLoc, vertLoc];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0857;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0xd7f0);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

		L0857:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L08b9;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			if ((short)this.processor.RegAcc.Value16 != this.owner.World.PersonParticipantIdx)
				goto L08b9;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[horizLoc, vertLoc];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L08b9;

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

		L08b9:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L08bc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x19);
			if (this.processor.FlagSet.IsLess) goto L08c5;
			goto L0a42;

		L08c5:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L08b9;

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1144)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L093b;
			goto L06ea;

		L093b:
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0958;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0958;
			goto L06ea;

		L0958:
			if (participantIdx != 0)
			{
				this.processor.RegAcc.LowByte = 0x1;
				this.processor.RegCount.LowByte = (byte)participantIdx;
				this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
				this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

				this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
					this.processor.PullLocalWord(0x12)] |= (sbyte)this.processor.RegCount.LowByte;
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L09f9;

			if (participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
				goto L09f9;

			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x2));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0xd7f0));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x16);
			if (this.processor.FlagSet.IsEqual) goto L09f9;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x16), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L09b2;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L09b2:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L09f9;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd7f0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L09f9;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xd7f0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.processor.PullGlobalWord(0xd7f0)] & 2) != 0)
				goto L09f9;

			this.owner.Bot.SendUnitToStrategicLocation(participantIdx,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12),
				3, 2);

		L09f9:
			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0a02;
			goto L06ea;

		L0a02:
			if (participantIdx != this.owner.World.PersonParticipantIdx &&
				this.processor.PullGlobalSignedWord(0xd806) == 0)
				goto L06ea;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0a3c;
			goto L06d4;

		L0a3c:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x10) + 0xf0,
				this.processor.PullLocalSignedWord(0x12),
				1);

			goto L06ea;

		L0a42:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L0ad0;

			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ad0;

			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

			this.processor.EmitLocalWord(0xa, 0x9);

		L0a66:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0ac7;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x10),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ac7;

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0x10),
				this.processor.PullLocalSignedWord(0x12));

		L0ac7:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x31);
			if (this.processor.FlagSet.IsLess) goto L0a66;

		L0ad0:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ShiftUnit");
		}

		/// <summary>
		/// Blinks the active unit on the map and its dot on the world map, and the cursor frame on a map tile, until a key is
		/// pressed or the mouse is clicked. A unit index from 128 blinks a text instead (no unit).
		/// </summary>
		/// <param name="cursorHoriz">Map position of the cursor frame, 0xffff for none</param>
		/// <param name="cursorVert">Map position of the cursor frame</param>
		public void ChoreographUnitMove(short participantIdx, short troopIdx, ushort cursorHoriz, ushort cursorVert)
		{
			this.processor.Journal.OpenScope($"ChoreographUnitMove({participantIdx}, {troopIdx}, {cursorHoriz}, {cursorVert})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;

			// Screen position of the cursor frame, -1 when there is none or it is outside the map view
			short frameHoriz = -1;
			short frameVert = 0;

			if (cursorHoriz != 0xffff)
			{
				frameHoriz = (short)((this.owner.Routing.TunePosition((short)cursorHoriz - this.processor.PullGlobalSignedWord(0xd4cc)) << 4) + 0x50);
				frameVert = (short)(((cursorVert - this.processor.PullGlobalWord(0xd75e)) << 4) + 0x8);

				if (frameHoriz < 0x50 || frameHoriz >= 0x140 || frameVert < 0x8 || frameVert > 0xc0)
					frameHoriz = -1;
			}

			// Position of the unit on the world map (top left corner of the screen)
			short worldMapTop = this.processor.PullGlobalSignedWord(0x70ea);
			short dotHoriz = (short)this.owner.Routing.TunePosition(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - this.processor.PullGlobalSignedWord(0x6ed6));
			int unitVert = troopIdx < 127 ? this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) : 0xff;
			short dotVert = (short)(unitVert - worldMapTop);
			bool dotVisible = dotVert >= 0 && dotVert < 50;

			do
			{
				// Hidden phase
				this.owner.Startup.ConcealMouseCursor();

				if (troopIdx >= UnitMemory.TroopTally)
				{
					this.owner.TextPainter.RenderTextToScreen0(0x20f6, 4, 124, 7);
				}
				else
				{
					this.owner.MapOps.RenderMapTile(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (dotVisible)
					{
						this.owner.Services.AssignDot(dotHoriz, dotVert + 8, 15);
					}
				}

				if (frameHoriz != -1)
				{
					this.owner.Dialogs.RenderBox(frameHoriz, frameVert, 15, 15, 15);
				}

				AwaitBlink();

				// Shown phase
				this.owner.Startup.ConcealMouseCursor();

				if (troopIdx >= UnitMemory.TroopTally)
				{
					this.owner.TextPainter.RenderTextToScreen0(0x2102, 4, 124, 0);
				}
				else
				{
					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx);

					if (dotVisible)
					{
						ushort dotColour = this.owner.MapOps.ObtainTerrainType(
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain ? (ushort)1 : (ushort)2;

						this.owner.Services.AssignDot(dotHoriz, dotVert + 8, dotColour);
					}
				}

				if (frameHoriz != -1)
				{
					this.owner.Dialogs.RenderBox(frameHoriz, frameVert, 15, 15, 0);
				}

				AwaitBlink();

				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.Libc.KeyAvailable() == 0 && this.owner.GlobDb3a == 0);

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("ChoreographUnitMove");
		}

		/// <summary>
		/// Shows the mouse pointer and waits 10 timer ticks, 1 when a key is waiting
		/// </summary>
		private void AwaitBlink()
		{
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TextPainter.AwaitTime((short)(this.owner.Libc.KeyAvailable() != 0 ? 1 : 10));
		}

		/// <summary>
		/// Creates a unit of the type on the tile in the first free record (0 - 126). Its home city is the nearest city when that
		/// city belongs to the player. When there is no free record, the human player is told once.
		/// </summary>
		/// <param name="kind">Unit type</param>
		/// <returns>Index of the new unit, or 0xffff</returns>
		public ushort ConstructUnit(short participantIdx, short kind, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ConstructUnit({participantIdx}, {kind}, {horizLoc}, {vertLoc})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			short troopIdx = -1;

			for (short idx = 0; idx < UnitMemory.TroopTally - 1; idx++)
			{
				if (this.troops.Pull(participantIdx, idx, UnitMemory.Kind) == 0xff)
				{
					troopIdx = idx;
					break;
				}
			}

			ushort result;

			if (troopIdx == -1)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx && this.processor.PullGlobalWord(0xd760) == 0)
				{
					this.processor.EmitGlobalWord(0x2f9e, 0x3);
					this.processor.EmitGlobalByte(0xba06, 0x0);

					this.owner.BlockText.DisplayTextDialog(0x2126);

					this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);

					this.processor.EmitGlobalWord(0xd760, 0x1);
				}

				result = 0xffff;
			}
			else
			{
				SetUpUnit(participantIdx, troopIdx, kind, horizLoc, vertLoc);

				result = (ushort)troopIdx;
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegAcc.Value16 = result;

			this.processor.Journal.CloseScope("ConstructUnit");

			return result;
		}

		/// <summary>
		/// Fills the free record of the new unit, puts it on the map and lets it see around
		/// </summary>
		private void SetUpUnit(short participantIdx, short troopIdx, short kind, int horizLoc, int vertLoc)
		{
			byte participantBit = (byte)(1 << participantIdx);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.FollowingInStack, 0xff);

			this.owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);
			this.owner.MapOps.SituateUnitOnTile(participantIdx, troopIdx, horizLoc, vertLoc);

			this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)participantBit;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)kind);

			// A new unit is fully healthy
			this.owner.World.Vitals.Wipe(participantIdx, troopIdx);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants, participantBit);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps,
				(byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));

			this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.RegAcc.LowByte);

			byte baseTown = this.troops.Pull(participantIdx, troopIdx, UnitMemory.BaseTown);
			ushort baseOwner = baseTown < 128 ? (ushort)this.owner.World.Towns[baseTown].ParticipantIdx : (ushort)0xffff;

			if (baseOwner != (ushort)participantIdx)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, 0xff);
			}

			// Planes start with one turn of fuel used
			byte fuel = this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps);

			if (fuel != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(fuel - 1));
			}

			this.owner.World.Participants[participantIdx].LiveTroops[kind]++;

			if (this.processor.PullGlobalWord(0x20f4) == 0)
			{
				if (this.processor.PullGlobalWord(0xd806) != 0 || participantIdx == this.owner.World.PersonParticipantIdx ||
					(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0)
				{
					this.owner.MapOps.RepaintMapTile(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
				}

				ShiftUnit(horizLoc, vertLoc, participantIdx, troopIdx, 1);
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx && this.owner.World.RoundTally != 0 &&
				this.processor.PullGlobalWord(0x20f4) == 0)
			{
				if (this.owner.World.Participants[participantIdx].TroopTally == 0)
				{
					this.owner.HelpSystem.DisplayHelpPopup(0x210e);
				}

				if (this.owner.World.Participants[participantIdx].TroopTally == 1)
				{
					this.owner.HelpSystem.DisplayHelpPopup(0x211a);
				}
			}
		}

		/// <summary>
		/// Removes the unit. A transport sinking on the ocean takes the land units of its stack along, a carrier its planes.
		/// The tile is repainted when the human player saw the unit.
		/// </summary>
		public void EliminateUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateUnit({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			if (this.processor.PullGlobalWord(0x70d8) != 0)
			{
				// Losses are counted (the kind as an unsigned word: an empty record, -1, is not counted)
				ushort lostKind = (ushort)this.troops.TroopKind(participantIdx, troopIdx);

				if (lostKind < this.owner.World.Participants[participantIdx].MissingTroops.Length)
				{
					this.owner.World.Participants[participantIdx].MissingTroops[lostKind]++;
				}
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) != 0 &&
				this.troops.Following(participantIdx, troopIdx) != -1 &&
				IsOnOcean(participantIdx, troopIdx))
			{
				VisitEachUnitInStack(participantIdx, troopIdx, 0x1610);
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Carrier &&
				this.troops.Following(participantIdx, troopIdx) != -1 &&
				IsOnOcean(participantIdx, troopIdx))
			{
				VisitEachUnitInStack(participantIdx, troopIdx, 0x1676);
			}

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (kind >= 0 && kind < this.owner.World.Participants[participantIdx].LiveTroops.Length)
				this.owner.World.Participants[participantIdx].LiveTroops[kind]--;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, horizLoc, vertLoc);

			if (this.processor.PullGlobalWord(0x20f4) == 0 && this.processor.PullGlobalWord(0x3936) == 0xffff &&
				(this.processor.PullGlobalWord(0xd806) != 0 || participantIdx == this.owner.World.PersonParticipantIdx ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0))
			{
				this.owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;

			this.processor.Journal.CloseScope("EliminateUnit");
		}

		/// <summary>Does the unit stand on an ocean tile</summary>
		private bool IsOnOcean(short participantIdx, short troopIdx)
		{
			return this.owner.MapOps.ObtainTerrainType(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain;
		}

		/// <summary>
		/// The best defender of the tile of the unit: the stack of the first unit of the player standing there is searched with
		/// LocateBestGuardianStep. A unit alone is its own defender.
		/// </summary>
		/// <returns>Unit index</returns>
		public ushort LocateStackLeader(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateStackLeader({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;
			ushort leader = (ushort)troopIdx;

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.processor.EmitGlobalWord(0x6530, (ushort)troopIdx);
				this.processor.EmitGlobalWord(0x6534, 0);

				for (short firstIdx = 0; firstIdx < UnitMemory.TroopTally; firstIdx++)
				{
					if (this.troops.Pull(participantIdx, firstIdx, UnitMemory.Kind) != 0xff &&
						this.troops.Pull(participantIdx, firstIdx, UnitMemory.Horiz) == this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) &&
						this.troops.Pull(participantIdx, firstIdx, UnitMemory.Vert) == this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))
					{
						VisitEachUnitInStack(participantIdx, firstIdx, 0x1169);
						break;
					}
				}

				leader = this.processor.PullGlobalWord(0x6530);
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = leader;

			this.processor.Journal.CloseScope("LocateStackLeader");

			return leader;
		}

		/// <summary>
		/// The best defender in the stack of the unit (LocateBestGuardianStep). A unit alone is its own defender.
		/// </summary>
		/// <returns>Unit index</returns>
		public ushort LocateBestGuardianInStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateBestGuardianInStack({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort guardian = (ushort)troopIdx;

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.processor.EmitGlobalWord(0x6530, (ushort)troopIdx);
				this.processor.EmitGlobalWord(0x6534, 0xffff);

				VisitEachUnitInStack(participantIdx, troopIdx, 0x1169);

				guardian = this.processor.PullGlobalWord(0x6530);
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegAcc.Value16 = guardian;

			this.processor.Journal.CloseScope("LocateBestGuardianInStack");

			return guardian;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void LocateBestGuardianStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateBestGuardianStep({participantIdx}, {troopIdx})");

			// Extract unit parameters
			int unitKind = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d7 + participantIdx * 0x600 + troopIdx * 0xc));
			int horiz = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d5 + participantIdx * 0x600 + troopIdx * 0xc));
			int vert = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d6 + participantIdx * 0x600 + troopIdx * 0xc));
			int terrainType = this.owner.MapOps.ObtainTerrainType(horiz, vert);

			int landGroup = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1138 + unitKind * 0x22));
			int baseGuardPower = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1140 + unitKind * 0x22));
			int terrainDefenseFactor = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x28f + terrainType * 0x13));

			byte condition = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d4 + participantIdx * 0x600 + troopIdx * 0xc));
			bool isFortified = (condition & 0x8) != 0;
			bool isVeteran = (condition & 0x20) != 0;

			int score = CombatRules.ComputeGuardianScore(
				unitKind, landGroup, baseGuardPower, terrainType, terrainDefenseFactor, isFortified, isVeteran);

			if (score >= 0)
			{
				int bestScore = (short)this.processor.PullGlobalWord(0x6534);
				if (score > bestScore)
				{
					this.processor.EmitGlobalWord(0x6534, (ushort)score);
					this.processor.EmitGlobalWord(0x6530, (ushort)troopIdx);
				}
			}

			this.processor.Journal.CloseScope("LocateBestGuardianStep");
		}

		/// <summary>
		/// Sum over the stack of the unit, the mode chooses what (TallyUnitsByDomainStep: 0 price, 1 defence, 2 units,
		/// 3 attack, 4 defensive units before the unit). Leaves SI = 0x600 * player like the translated original,
		/// AIPlayer depends on it (see MIGRATION-BACKLOG.md).
		/// </summary>
		public ushort TallyUnitsByDomainInStack(short participantIdx, short troopIdx, ushort mode)
		{
			this.processor.Journal.OpenScope($"TallyUnitsByDomainInStack({participantIdx}, {troopIdx}, {mode})");

			this.processor.EmitGlobalWord(0x6532, 0x0);
			this.processor.EmitGlobalWord(0x6536, mode);
			this.processor.EmitGlobalWord(0x6538, (ushort)troopIdx);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1280);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6532);

			this.processor.Journal.CloseScope("TallyUnitsByDomainInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallyUnitsByDomainInStack, adds to the total in 0x6532 what the mode in 0x6536 asks for:
		/// 0 the price, 1 the defence, 2 one per unit, 3 the attack, 4 one per defensive unit with an index below 0x6538.
		/// </summary>
		public void TallyUnitsByDomainStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsByDomainStep({participantIdx}, {troopIdx})");

			switch (this.processor.PullGlobalWord(0x6536))
			{
				case 0:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
					break;

				case 1:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.GuardPower));
					break;

				case 2:
					AddToGlobal(0x6532, 1);
					break;

				case 3:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower));
					break;

				case 4:
					if (troopIdx < (short)this.processor.PullGlobalWord(0x6538) &&
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops)
					{
						AddToGlobal(0x6532, 1);
					}
					break;
			}

			this.processor.Journal.CloseScope("TallyUnitsByDomainStep");
		}

		/// <summary>Adds to a word of the data segment (with the 16 bit overflow of the original)</summary>
		private void AddToGlobal(ushort address, int amount)
		{
			this.processor.EmitGlobalWord(address, (ushort)(this.processor.PullGlobalWord(address) + amount));
		}

		/// <summary>
		/// Number of the units of the type in the stack of the unit. Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		public ushort TallyUnitsOfTypeInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsOfTypeInStack({participantIdx}, {troopIdx}, {arg3})");

			this.processor.EmitGlobalWord(0x653a, arg3);
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x135a);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			this.processor.Journal.CloseScope("TallyUnitsOfTypeInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallyUnitsOfTypeInStack: counts in 0x653c the units of the type in 0x653a
		/// </summary>
		public void TallyUnitsOfTypeStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsOfTypeStep({participantIdx}, {troopIdx})");

			if ((ushort)this.troops.TroopKind(participantIdx, troopIdx) == this.processor.PullGlobalWord(0x653a))
			{
				AddToGlobal(0x653c, 1);
			}

			this.processor.Journal.CloseScope("TallyUnitsOfTypeStep");
		}

		/// <summary>
		/// Number of the units with the TroopGroup in the stack of the unit. Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		public ushort TallyUnitsWithFlagInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsWithFlagInStack({participantIdx}, {troopIdx}, {arg3})");

			this.processor.EmitGlobalWord(0x653a, arg3);
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13a9);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			this.processor.Journal.CloseScope("TallyUnitsWithFlagInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallyUnitsWithFlagInStack: counts in 0x653c the units whose TroopGroup is the one in 0x653a
		/// </summary>
		public void TallyUnitsWithFlagStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsWithFlagStep({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == this.processor.PullGlobalWord(0x653a))
			{
				AddToGlobal(0x653c, 1);
			}

			this.processor.Journal.CloseScope("TallyUnitsWithFlagStep");
		}

		/// <summary>
		/// The free room on the transports of the stack of the unit (TallySpecialUnitsStep).
		/// Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		public ushort TallySpecialUnitsInStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallySpecialUnitsInStack({participantIdx}, {troopIdx})");

			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13f8);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			this.processor.Journal.CloseScope("TallySpecialUnitsInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallySpecialUnitsInStack, the free room on the transports in 0x653c: a land unit takes one place,
		/// a transport adds its room (FerryRoom)
		/// </summary>
		public void TallySpecialUnitsStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallySpecialUnitsStep({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				AddToGlobal(0x653c, -1);
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				AddToGlobal(0x653c, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom));
			}

			this.processor.Journal.CloseScope("TallySpecialUnitsStep");
		}

		/// <summary>
		/// Calls a step function for every unit of the stack of the unit, at most for 10 units. The step function is given by
		/// its address in the original code.
		/// </summary>
		public void VisitEachUnitInStack(short participantIdx, short troopIdx, int funcAddr)
		{
			this.processor.Journal.OpenScope($"VisitEachUnitInStack({participantIdx}, {troopIdx}, 0x{funcAddr:x4})");

			Action<short, short> step = StackStep(funcAddr);

			ForEachUnitInStack(participantIdx, troopIdx, (stepParticipantIdx, stepTroopIdx) =>
			{
				// The translated original left BX = 0xc * unit and SI = 0x600 * player before every step, so SI = 0x600 * player
				// after the call. A caller depends on it: AIPlayer divides by SI after TallyUnitsByDomainInStack (L2947),
				// where the DOS code had kept its own SI. Kept until the end of the migration, see MIGRATION-BACKLOG.md.
				this.processor.RegBase.Value16 = (ushort)(UnitMemory.TroopStride * stepTroopIdx);
				this.processor.RegSource.Value16 = (ushort)(UnitMemory.ParticipantStride * stepParticipantIdx);

				step(stepParticipantIdx, stepTroopIdx);
			});

			this.processor.Journal.CloseScope("VisitEachUnitInStack");
		}

		/// <summary>
		/// Calls the action for every unit of the stack of the unit, starting with it, at most for 10 units.
		/// The following unit is read before the action, so the action may remove the unit from the stack.
		/// </summary>
		public void ForEachUnitInStack(short participantIdx, short troopIdx, Action<short, short> visit)
		{
			short present = troopIdx;

			for (int idx = 0; idx < 10; idx++)
			{
				short following = this.troops.Following(participantIdx, present);

				visit(participantIdx, present);

				present = following;

				if (present == -1 || present == troopIdx)
					break;
			}
		}

		/// <summary>The step functions of VisitEachUnitInStack by their address in the original code</summary>
		private Action<short, short> StackStep(int funcAddr)
		{
			return funcAddr switch
			{
				0xf10 => EliminateUnit,
				0x1169 => LocateBestGuardianStep,
				0x1280 => TallyUnitsByDomainStep,
				0x135a => TallyUnitsOfTypeStep,
				0x13a9 => TallyUnitsWithFlagStep,
				0x13f8 => TallySpecialUnitsStep,
				0x14a2 => VerifyUnitTerrainFlag,
				0x14f6 => WipeUnitOrders,
				0x1560 => RestoreUnitOrders,
				0x1593 => RestoreUnitStatus,
				0x1610 => EliminateLandUnit,
				0x1643 => EliminateNonLandUnit,
				0x1676 => EliminateSeaUnit,
				_ => throw new Exception("Unknown function address"),
			};
		}

		/// <summary>
		/// Stack step: wakes the unit (clears Sentry), except a land unit carried on the ocean
		/// </summary>
		public void VerifyUnitTerrainFlag(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"VerifyUnitTerrainFlag({participantIdx}, {troopIdx})");

			bool carried = IsOnOcean(participantIdx, troopIdx) &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;

			if (!carried)
			{
				ClearStatus(participantIdx, troopIdx, UnitStatusEnum.Sentry);
			}

			this.processor.Journal.CloseScope("VerifyUnitTerrainFlag");
		}

		private void ClearStatus(short participantIdx, short troopIdx, UnitStatusEnum flags)
		{
			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & ~(int)flags));
		}

		/// <summary>The full moves of the unit type of the unit, in thirds</summary>
		private byte FullSteps(short participantIdx, short troopIdx)
		{
			return (byte)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));
		}

		/// <summary>
		/// Stack step: a sleeping (Sentry) unit wakes up with full moves (and full fuel for a plane); every unit loses its
		/// "go to" order
		/// </summary>
		public void WipeUnitOrders(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"WipeUnitOrders({participantIdx}, {troopIdx})");

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Sentry) != 0)
			{
				ClearStatus(participantIdx, troopIdx, UnitStatusEnum.Sentry);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, FullSteps(participantIdx, troopIdx));

				ushort roundsAway = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway);

				if (roundsAway != 0)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
				}
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			this.processor.Journal.CloseScope("WipeUnitOrders");
		}

		/// <summary>
		/// Stack step: RestoreUnitStatus for the ships of the stack
		/// </summary>
		public void RestoreUnitOrders(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RestoreUnitOrders({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
			{
				RestoreUnitStatus(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("RestoreUnitOrders");
		}

		/// <summary>
		/// Gives the unit back to the player without orders: settler work stops, a sleeping, fortified or working unit gets
		/// its full moves, only the Veteran (and 0x10) status stays, the "go to" order is removed
		/// </summary>
		public void RestoreUnitStatus(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RestoreUnitStatus({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);

			if ((status & (int)UnitStatusEnum.SettlerBuildMask) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
			}

			if ((status & (int)(UnitStatusEnum.SettlerBuildMask | UnitStatusEnum.Fortified | UnitStatusEnum.Sentry)) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, FullSteps(participantIdx, troopIdx));
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)(UnitStatusEnum.Veteran | UnitStatusEnum.Unknown1)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx);
			}

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("RestoreUnitStatus");
		}

		/// <summary>
		/// Stack step: removes the unit when it is a land unit (cargo of a sinking transport)
		/// </summary>
		public void EliminateLandUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateLandUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				EliminateUnit(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("EliminateLandUnit");
		}

		/// <summary>
		/// Stack step: removes the unit when it is a plane or a ship
		/// </summary>
		public void EliminateNonLandUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateNonLandUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				EliminateUnit(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("EliminateNonLandUnit");
		}

		/// <summary>
		/// Stack step: removes the unit when it is a plane (LandGroup 1, despite the name: the planes of a sinking carrier)
		/// </summary>
		public void EliminateSeaUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSeaUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				EliminateUnit(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("EliminateSeaUnit");
		}

		/// <summary>
		/// ??? To be checked - Too many parameters calling CenterMapView
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void PanMapToPosition(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"PanMapToPosition({participantIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x2);

			this.processor.EmitLocalWord(0x2, 0x0);

			if (horizLoc < 16 && this.processor.PullGlobalSignedWord(0xd4cc) > 64)
			{
				horizLoc += 80;
			}

			if (horizLoc < this.processor.PullGlobalSignedWord(0xd4cc) + 2 ||
				horizLoc > this.processor.PullGlobalSignedWord(0xd4cc) + 13)
			{
				this.processor.EmitLocalWord(0x2, 0x1);
			}

			if (vertLoc < this.processor.PullGlobalSignedWord(0xd75e) + 2 ||
				vertLoc > this.processor.PullGlobalSignedWord(0xd75e) + 9)
			{
				this.processor.EmitLocalWord(0x2, 0x1);
			}
		
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1721;

			this.owner.Routing.TunePosition(horizLoc - 8);

			this.owner.MapOps.FocusMapView(participantIdx, (short)this.processor.RegAcc.Value16, vertLoc - 6);

		L1721:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("PanMapToPosition");
		}

		/// <summary>
		/// Is the move onto the tile blocked by the zone of control: never into a city, otherwise VerifyTileOccupation
		/// </summary>
		/// <returns>1 when a foreign unit stands next to the tile</returns>
		public ushort MayOccupyTile(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"MayOccupyTile({participantIdx}, {horizLoc}, {vertLoc})");

			ushort blocked = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 ? (ushort)0 :
				VerifyTileOccupation(participantIdx, horizLoc, vertLoc);

			this.processor.RegAcc.Value16 = blocked;

			this.processor.Journal.CloseScope("MayOccupyTile");

			return blocked;
		}

		/// <summary>
		/// Does a unit of another player stand on one of the 8 tiles around, on the same kind of tile (land or ocean).
		/// In the turn of the human player that unit, and its city, become visible to the human.
		/// </summary>
		/// <returns>1 when a foreign unit stands next to the tile, 0 otherwise</returns>
		public ushort VerifyTileOccupation(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyTileOccupation({participantIdx}, {horizLoc}, {vertLoc})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			bool onOcean = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
			ushort found = 0;

			for (int heading = 1; heading < 9; heading++)
			{
				int nearHoriz = this.owner.Routing.TunePosition(this.owner.MapOps.Chart.DirectionHoriz(heading) + horizLoc);
				int nearVert = (short)(this.owner.MapOps.Chart.DirectionVert(heading) + vertLoc);

				bool nearOcean = this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain;

				if (nearOcean != onOcean)
					continue;

				ushort nearParticipantIdx = this.owner.MapOps.ObtainTileOwner(nearHoriz, nearVert);

				if (nearParticipantIdx == 0xffff || nearParticipantIdx == (ushort)participantIdx)
					continue;

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					RevealToHuman((short)nearParticipantIdx, nearHoriz, nearVert);
				}

				found = 1;
				break;
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = found;

			this.processor.Journal.CloseScope("VerifyTileOccupation");

			return found;
		}

		/// <summary>
		/// The human player sees the foreign unit on the tile, and the city when there is one
		/// </summary>
		private void RevealToHuman(short participantIdx, int horizLoc, int vertLoc)
		{
			short human = this.owner.World.PersonParticipantIdx;
			byte humanBit = (byte)(1 << human);
			ushort troopIdx = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			// The record of 0xffff (no unit) wraps around in the data segment, like in the original
			int shownTo = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants);

			if ((shownTo & (ushort)(1 << human)) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | humanBit));
				this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)humanBit;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);
				this.owner.World.Towns[townIdx].ShownExtent = this.owner.World.Towns[townIdx].TrueExtent;

				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)humanBit;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ContainsAdjacentForeignUnit(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsAdjacentForeignUnit({participantIdx}, {horizLoc}, {vertLoc})");

			// Check the 8 neighboring tiles for any foreign unit (Zone of Control)
			bool found = MovementRules.HasAdjacentForeignUnit(horizLoc, vertLoc, participantIdx, (x, y) =>
			{
				this.owner.MapOps.ObtainTileOwner(x, y);
				return (int)(short)this.processor.RegAcc.Value16;
			});

			ushort result = (ushort)(found ? 1 : 0);
			this.processor.RegAcc.Value16 = result;

			// Far return
			this.processor.Journal.CloseScope("ContainsAdjacentForeignUnit");

			return result;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void VisitHut(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"VisitHut({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveIndexRegisters();

			if (participantIdx == 0)
				goto L1d4f;

			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);

			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1964;
			goto L1d4f;

		L1964:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Helpers.LocateNearestCity(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0xc));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.Helpers.ObtainDistance(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				// !!! Added if city doesn't exist at that location
				(this.processor.PullLocalSignedWord(0xc) >= 0 && this.processor.PullLocalSignedWord(0xc) < 128) ?
					this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.X : -1,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				// !!! Added if city doesn't exist at that location
				(this.processor.PullLocalSignedWord(0xc) >= 0 && this.processor.PullLocalSignedWord(0xc) < 128) ?
					this.owner.World.Towns[this.processor.PullLocalWord(0xc)].Place.Y : -1);

			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L19e7;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L19cc;
			goto L1a7f;

		L19cc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L19d4;
			goto L1a8e;

		L19d4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L19dc;
			goto L1b88;

		L19dc:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L19e4;
			goto L1b9a;

		L19e4:
			goto L1d4f;

		L19e7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.VideoCard.ObtainDot(2,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) + 80,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1a20;
			goto L1b9a;

		L1a20:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1a8e;

			this.owner.Libc.CopyText(0xba06, "You have discovered\nan advanced tribe.\n");

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L1a51;

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L1a51:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			
			this.owner.Founding.EstablishCity(participantIdx,
				0,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)),
				1);

			goto L1d30;

		L1a7f:
			if (this.owner.World.RoundTally == 0)
				goto L1a8e;

			if (this.owner.World.Season <= 1000)
				goto L1ac7;

			L1a8e:
			this.owner.Libc.CopyText(0xba06, "You have discovered\nvaluable metal deposits\nworth 50$\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.World.Participants[participantIdx].Gold += 50;
			goto L1d4f;

		L1ac7:
			this.owner.Libc.CopyText(0xba06, "You have discovered\nscrolls of ancient wisdom.\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		
			this.processor.EmitLocalWord(0x2, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(72));

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

		L1b07:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x12));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L1b63;
			
			this.processor.ScaleIndexToSource(0x16, this.processor.PullLocalWord(0x12));
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L1b63;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L1b63;
			
			this.owner.Advisors.EarnTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x12),
				0);
			
			this.processor.EmitLocalWord(0x12, 0xffff);
			goto L1b72;

		L1b63:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x48;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x12, this.processor.RegData.Value16);

		L1b72:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsLess) goto L1b7f;
			goto L1d4f;

		L1b7f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1b07;
			goto L1d4f;

		L1b88:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x4);
			if (this.processor.FlagSet.IsLess) goto L1b9a;
			
			if (this.owner.World.Participants[participantIdx].TownTally != 0) goto L1bff;

		L1b9a:
			this.owner.Libc.CopyText(0xba06, "You have discovered\na friendly tribe of\nskilled mercenaries.\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		
			goto L1d28;

		L1bff:
			this.owner.Libc.CopyText(0xba06, "You have unleashed\na horde of barbarians!\n");

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.processor.EmitLocalWord(0x10, 0x1);
			goto L1c91;

		L1c32:
			this.processor.RegAcc.Value16 = 0x6;

		L1c35:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			ConstructUnit(
				0,
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));
			
			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81dd), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullLocalSignedWord(0x6),
					this.processor.PullLocalSignedWord(0xa));
			}

		L1c71:
			this.owner.Helpers.VerifyValueSpan(
				4 - this.owner.World.Participants[participantIdx].TownTally,	1, 4);

			this.processor.EmitLocalWord(0x10, this.processor.SumWord(this.processor.PullLocalWord(0x10), this.processor.RegAcc.Value16));

		L1c91:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1c9a;
			goto L1d4f;

		L1c9a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c71;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1c71;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1d00;
			goto L1c71;

		L1d00:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.RegCount.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L1d1f;
			goto L1c32;

		L1d1f:
			this.processor.RegAcc.Value16 = 0x3;
			goto L1c35;

		L1d28:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			ConstructUnit(
				participantIdx,
				(short)((this.owner.Libc.Dice.DrawInt(2) != 0) ? 3 : 6),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

		L1d30:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
			}

		L1d4f:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("VisitHut");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="param3"></param>
		public void ChoreographUnitCombat(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ChoreographUnitCombat({participantIdx}, {troopIdx}, {arg3})");

			// function body
			this.processor.EnterFrame(0xc);
			this.processor.PreserveIndexRegisters();
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1d8b;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.MapOps.RenderMapTile(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			goto L1f63;

		L1d8b:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.ScaleIndex(0xc, (ushort)troopIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);

			this.owner.MapOps.RenderMapTile(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1dbd;
			goto L1f5e;

		L1dbd:
			this.owner.TurnLogic.VerifyPositionOnScreen(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1dd8;
			goto L1f5e;

		L1dd8:
			this.processor.RegAcc.Value16 = arg3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;

			this.owner.TurnLogic.VerifyPositionOnScreen(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1882)) + this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x18e4)) + this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1e09;
			goto L1f5e;

		L1e09:
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1e61;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1e40;
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1e61;

		L1e40:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);

			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)));

		L1e61:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0x50, 0, 0xf0, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0x50, 0);

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);

			this.owner.Routing.TunePosition(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)) -
					this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x50);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd75e));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x0);

		L1ecb:
			this.processor.RegSource.Value16 = (ushort)(arg3 << 1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) *
						this.processor.PullLocalSignedWord(0x8)) +
					this.processor.PullLocalSignedWord(0x2) + 1,
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) *
						this.processor.PullLocalSignedWord(0x8)) +
					this.processor.PullLocalSignedWord(0x4) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16,
							(ushort)(((0x600 * participantIdx) + (0xc * troopIdx)) + 0x81d7)) + (participantIdx << 5) + 0x40) << 1) + 0xd4ce)));

			this.owner.TextPainter.AwaitTime(1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x2));

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				this.processor.RegDest.Value16,
				this.processor.PullLocalWord(0xc),
				0x10, 0x10,
				this.processor.PullGlobalWord(0xaa),
				this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x10);
			if (this.processor.FlagSet.IsGreater) goto L1f5e;
			goto L1ecb;

		L1f5e:
			this.owner.Startup.DisplayMouseCursor();

		L1f63:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChoreographUnitCombat");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void DisplayBattleScreen(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayBattleScreen({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x28);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			LocateStackLeader(participantIdx, troopIdx);

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x28, 0x0);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, 0x0);

		L1f9a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x1a), this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x1e));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0xc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1fd3;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1fd3;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1f9a;

		L1fd3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x60);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

		L1fe2:
			this.owner.Dialogs.FloodBoxWithDoubleShadow(100, this.processor.PullLocalSignedWord(0x22),
				120, (this.processor.PullLocalWord(0x28) << 4) + 5, 3);

			this.processor.EmitLocalWord(0x28, 0x0);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x5);
			this.processor.EmitLocalWord(0x20, this.processor.RegAcc.Value16);

		L2018:
			this.owner.MapOps.RenderUnitSprite(participantIdx,
				this.processor.PullLocalSignedWord(0x1e),
				106, this.processor.PullLocalSignedWord(0x20));

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x1e));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L206a;

			this.owner.Libc.AppendText(0xba06, " (V)");

		L206a:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 128, this.processor.PullLocalSignedWord(0x20), 15);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x1e));

			this.owner.Conquest.ObtainCityLabel(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81df)));
			
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 128, this.processor.PullLocalSignedWord(0x20) + 8, 14);

			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x20, this.processor.SumWord(this.processor.PullLocalWord(0x20), 0x10));
			this.processor.IncrementLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0xc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L20e4;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L20e4;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L20e4;
			goto L2018;

		L20e4:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2110;

		L20f5:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2104;
			goto L21b7;

		L2104:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L20f5;
			goto L21b7;

		L2110:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				105,
				(this.processor.PullLocalSignedWord(0x2) * 16) +
					this.processor.PullLocalSignedWord(0x22) + 4,
				112, 17, 3, 11);

			this.processor.CopyLocalWord(0x2, 0x1c);

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsEqual) goto L2158;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsEqual) goto L21a0;
			goto L2161;

		L2158:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2161;
			this.processor.DecrementLocalWord(0x2);

		L2161:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				105,
				(this.processor.PullLocalSignedWord(0x1c) * 16) +
					this.processor.PullLocalSignedWord(0x22) + 4,
				112, 17, 11, 3);

			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xd);
			if (this.processor.FlagSet.IsEqual) goto L219b;
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L21ae;

		L219b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			goto L21ef;

		L21a0:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			if (this.processor.FlagSet.IsLessOrEqual) goto L2161;
			this.processor.IncrementLocalWord(0x2);
			goto L2161;

		L21ae:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L21b7;
			goto L2110;

		L21b7:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L21c6;
			goto L2256;

		L21c6:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x64);
			if (this.processor.FlagSet.IsGreater) goto L21d0;
			goto L2256;

		L21d0:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xdc);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2256;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x5);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);

		L21ef:
			this.processor.EmitLocalWord(0x1e, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L2256;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2256;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x1e);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x1a));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x26));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xc2);
			if (this.processor.FlagSet.IsEqual) goto L2233;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L2233:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x26));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2256;
			goto L1fe2;

		L2256:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x26);
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayBattleScreen");
		}

		/// <summary>
		/// The direction (1 - 8) of the step of the unit towards the nearest own city or carrier (a plane flying home). Tiles with
		/// foreign units and foreign cities are avoided, 0 when no step gets nearer.
		/// </summary>
		/// <returns>Direction, also in AX</returns>
		public ushort LocateNearestFriendlyCity(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateNearestFriendlyCity({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			// The frame of the original: without an own city or carrier the target stays what was left in the stack there
			this.processor.EnterFrame(0x16);
			int targetHoriz = this.processor.PullLocalSignedWord(0x14);
			int targetVert = this.processor.PullLocalSignedWord(0x16);

			int unitHoriz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			int unitVert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			int nearest = 999;

			foreach (City town in this.owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				int distance = WrappedChebyshev(unitHoriz - town.Place.X, unitVert - town.Place.Y);

				if (distance < nearest)
				{
					nearest = distance;
					targetHoriz = town.Place.X;
					targetVert = town.Place.Y;
				}
			}

			for (short carrierIdx = 0; carrierIdx < UnitMemory.TroopTally; carrierIdx++)
			{
				if (this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Kind) != UnitKindTable.Carrier)
					continue;

				int carrierHoriz = this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Horiz);
				int carrierVert = this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Vert);
				int distance = WrappedChebyshev(unitHoriz - carrierHoriz, unitVert - carrierVert);

				if (distance < nearest)
				{
					nearest = distance;
					targetHoriz = carrierHoriz;
					targetVert = carrierVert;
				}
			}

			int best = this.owner.Helpers.ObtainDistanceFromOffsets(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - targetHoriz,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - targetVert);
			ushort heading = 0;

			for (int direction = 1; direction <= 8; direction++)
			{
				// Like the original, the step doesn't wrap around the map
				int stepHoriz = (short)(unitHoriz + this.owner.MapOps.Chart.DirectionHoriz(direction));
				int stepVert = (short)(unitVert + this.owner.MapOps.Chart.DirectionVert(direction));

				ushort stepOwner = this.owner.MapOps.ObtainTileOwner(stepHoriz, stepVert);

				if (stepOwner != 0xffff && stepOwner != (ushort)participantIdx)
					continue;

				if ((this.owner.MapOps.ObtainTileImprovements(stepHoriz, stepVert) & 1) != 0 &&
					this.owner.MapOps.ObtainTileOwnerBits(stepHoriz, stepVert) != (ushort)participantIdx)
					continue;

				int distance = this.owner.Helpers.ObtainDistanceFromOffsets(stepHoriz - targetHoriz, stepVert - targetVert);

				if (distance < best)
				{
					best = distance;
					heading = (ushort)direction;
				}
			}

			this.processor.LeaveFrame();

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = heading;

			this.processor.Journal.CloseScope("LocateNearestFriendlyCity");

			return heading;
		}

		/// <summary>The larger of the two steps, the horizontal one wrapping over 40</summary>
		private static int WrappedChebyshev(int horizStep, int vertStep)
		{
			horizStep = Math.Abs(horizStep);

			if (horizStep > 40)
				horizStep = 80 - horizStep;

			return Math.Max(horizStep, Math.Abs(vertStep));
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1})");

			// function body
			if (this.owner.World.RerunContentSpan + 3 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2})");

			// function body
			if (this.owner.World.RerunContentSpan + 4 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content2;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		/// <param name="data3"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3})");

			// function body
			if (this.owner.World.RerunContentSpan + 5 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content2;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content3;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="replayID"></param>
		/// <param name="data1"></param>
		/// <param name="data2"></param>
		/// <param name="data3"></param>
		/// <param name="data4"></param>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3, byte content4)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3}, {content4})");

			// function body
			if (this.owner.World.RerunContentSpan + 6 < 0x1000)
			{
				ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)((interim & 0xff00) >> 8);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = (byte)(interim & 0xff);
				this.owner.World.RerunContentSpan++;

				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content1;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content2;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content3;
				this.owner.World.RerunContentSpan++;
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan] = content4;
				this.owner.World.RerunContentSpan++;
			}

			// Far return
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RenderBusyIndicator()
		{
			this.processor.Journal.OpenScope("RenderBusyIndicator()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xc0);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x78);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				this.processor.PullLocalWord(0x4),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 8),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0x138, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x18),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0x138, 0xc0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x10),
				0x78, 8, 8,
				this.processor.PullGlobalWord(0xaa),
				0, 0xc0);

			this.processor.EmitLocalWord(0x2, 0x1);

		L26cc:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x18),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				0, (ushort)(this.processor.PullLocalWord(0x2) << 3));

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 8),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				0x138, (ushort)(this.processor.PullLocalWord(0x2) << 3));

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x18);
			if (this.processor.FlagSet.IsLess) goto L26cc;
			this.processor.EmitLocalWord(0x2, 0x1);

		L272a:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				this.processor.PullLocalWord(0x4),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(this.processor.PullLocalWord(0x2) << 3), 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				(ushort)(this.processor.PullLocalWord(0x4) + 0x10),
				(ushort)(this.processor.PullLocalWord(0x6) + 8),
				8, 8,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(this.processor.PullLocalWord(0x2) << 3), 0xc0);

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x27);
			if (this.processor.FlagSet.IsLess) goto L272a;
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderBusyIndicator");
		}
	}
}
