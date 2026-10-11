using System.Drawing;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 20.
	/// </summary>
	public class CityFoundingAndScore
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public CityFoundingAndScore(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Changes the terrain under a new city to what irrigation makes of it, when irrigation changes the terrain type.
		/// The irrigation result of a terrain type (12 bytes per type, at 0x44a) is -1 when it can't be irrigated,
		/// -2 when it is irrigated without change, other values are the new terrain (as the settlers code uses it).
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		private void WipeCitySquare(int horizLoc, int vertLoc)
		{
			ushort landKind = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			short freshLand = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(landKind * 12 + 0x44a));

			if (freshLand >= 0)
			{
				// The same steps as when settlers finish changing the terrain
				this.owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)freshLand);
				this.owner.World.ChartSight[horizLoc, vertLoc] |= 1;
				this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="param2"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="citySize"></param>
		/// <returns></returns>
		public ushort EstablishCity(short participantIdx, ushort arg2, int horizLoc, int vertLoc, ushort townExtent)
		{
			this.processor.Journal.OpenScope($"EstablishCity({participantIdx}, {arg2}, {horizLoc}, {vertLoc}, {townExtent})");

			// function body
			this.processor.EnterFrame(0x14);
			this.processor.PreserveSource();

			this.owner.Bot.DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 4);

			this.processor.EmitLocalWord(0x8, 0x0);
			goto L0029;

		L0026:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L0029:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0033;
			goto L0502;

		L0033:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x8));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0026;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].ChiefVisuals;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x7);
			if (this.processor.FlagSet.IsLess) goto L0057;
			this.processor.DecrementLocalWord(0x10);

		L0057:
			this.processor.RegCount.LowByte = 0x4;
			this.processor.EmitLocalWord(0x10, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x10), this.processor.RegCount.LowByte));
			this.processor.CopyLocalWord(0x10, 0x2);

		L0062:
			this.processor.EmitLocalWord(0xa, 0x1);

			if (this.owner.World.TownPlaces[this.processor.PullLocalWord(0x10)].X == -1)
				goto L00ab;

			this.processor.EmitLocalWord(0xa, 0x0);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.IncrementLocalWord(0x10);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10));
			if (this.processor.FlagSet.IsNotEqual) goto L008e;
			this.processor.EmitLocalWord(0x10, 0xe0);

		L008e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x100);
			if (this.processor.FlagSet.IsNotEqual) goto L00ab;
			this.processor.EmitLocalWord(0x10, 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x3e7);
			if (this.processor.FlagSet.IsNotEqual) goto L00a6;
			this.processor.EmitLocalWord(0xa, 0x1);

		L00a6:
			this.processor.EmitLocalWord(0x2, 0x3e7);

		L00ab:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0062;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x8));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x10);
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LabelIdx = this.processor.RegAcc.LowByte;

			this.owner.World.TownPlaces[this.processor.PullLocalWord(0x10)] = new Point(horizLoc, vertLoc);

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0118;
			
			this.owner.CityNaming.DisplayCityLabelPrompt(this.processor.PullLocalSignedWord(0x8));
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L00fe;

		L00f8:
			this.processor.RegAcc.Value16 = 0xffff;
			goto L053b;

		L00fe:
			byte townLabelIdx = this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LabelIdx;
			char[] townLabel = this.owner.World.TownLabels[townLabelIdx].ToCharArray();
			townLabel[12] = '\0';
			this.owner.World.TownLabels[townLabelIdx] = new string(townLabel);

		L0118:
			townLabelIdx = this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LabelIdx;
			this.processor.EmitLocalWord(0xc, 0xb);

			while (this.processor.PullLocalWord(0xc) > 0 &&
				this.owner.World.TownLabels[townLabelIdx][this.processor.PullLocalWord(0xc)] == ' ')
			{
				townLabel = this.owner.World.TownLabels[townLabelIdx].ToCharArray();
				townLabel[this.processor.PullLocalWord(0xc)] = '\0';
				this.owner.World.TownLabels[townLabelIdx] = new string(townLabel);
				this.processor.DecrementLocalWord(0xc);
			}

			this.owner.UnitOps.AttachReplayContent(1, (byte)participantIdx,
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LabelIdx,
				(byte)horizLoc, (byte)vertLoc);

			this.owner.MapOps.DiscardTileImprovements(0x24, horizLoc, vertLoc);

			// Remake: the land under a new city is cleared. Forest, jungle and swamp change the same way
			// as when settlers irrigate them (forest to plains, jungle and swamp to grassland).
			WipeCitySquare(horizLoc, vertLoc);

			this.owner.MapOps.AssignTileImprovements(9, horizLoc, vertLoc);

			this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			this.processor.RegCount.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)), 0xffff);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01c9;

			this.owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);

		L01c9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ConditionMarker = 0;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.X = horizLoc;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.Y = vertLoc;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].TrueExtent = (sbyte)townExtent;
			this.processor.RegAcc.LowByte = 0;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ShownExtent = 0;
			
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LaborerMarkers0 = this.processor.RegAcc.Value16;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LaborerMarkers1 = this.processor.RegData.Value16;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].RootCommerce = (sbyte)this.processor.RegAcc.LowByte;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].GrainTally = (short)this.processor.RegAcc.Value16;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].MaterialsTally = (short)this.processor.RegAcc.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].StructureMarkers0 = this.processor.RegAcc.Value16;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].StructureMarkers1 = this.processor.RegData.Value16;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].PresentMakingIdx = 1;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].PresentMakingIdx = 4;
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x40);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x8));
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].PresentMakingIdx = 5;
			}
		
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Unfamiliar[0] = -1;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Unfamiliar[1] = -1;
			this.processor.EmitLocalWord(0x6, 0x1);
			this.processor.EmitLocalWord(0xc, 0x0);

		L026a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xc));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0xc)].ParticipantIdx != participantIdx) goto L0289;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0289;
			this.processor.EmitLocalWord(0x6, 0x0);

		L0289:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x80);
			if (this.processor.FlagSet.IsLess) goto L026a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L02ad;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].StructureMarkers0 = 1;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].StructureMarkers1 = 0;

		L02ad:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].RootCommerce = 0;
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ParticipantIdx = participantIdx;
			this.processor.EmitLocalWord(0xc, 0x0);

		L02c6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x8));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].CommerceTownIndices[this.processor.PullLocalWord(0xc)] = -1;
			this.processor.IncrementLocalWord(0xc);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L02c6;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].LaborerMarkers2 = 0;
			this.processor.EmitLocalWord(0xc, 0x1);

		L02f2:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0349;

			this.owner.MapOps.ObtainLandMassSize(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x100);
			if (this.processor.FlagSet.IsGreater) goto L033c;

			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L0349;

		L033c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ConditionMarker |= 2;

		L0349:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xb);
			if (this.processor.FlagSet.IsEqual) goto L0355;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L0362;

		L0355:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ConditionMarker |= 8;

		L0362:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L02f2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0385;

			if (participantIdx != this.owner.World.PersonParticipantIdx) goto L0385;

			this.owner.HelpSystem.DisplayHelpPopup(0x4f66);

		L0385:
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L0411;

		L038d:
			this.owner.VideoCard.ObtainDot(2, 
				this.processor.PullLocalSignedWord(0x2) + 0x50,
				this.processor.PullLocalSignedWord(0x4));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsLess) goto L03bf;

			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x2) + 0x50,
				this.processor.PullLocalSignedWord(0x4),
				8);

		L03bf:
			if (participantIdx == this.owner.World.PersonParticipantIdx) goto L040e;

			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L040e;

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L040e;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4));

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.PersonParticipantIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L040e;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x8));
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ShownExtent = 1;
			
			this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

		L040e:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0411:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x2d);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0456;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) + horizLoc);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x14);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0442;
			goto L038d;

		L0442:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x2) + 0x50,
				this.processor.PullLocalSignedWord(0x4),
				(ushort)participantIdx);

			goto L03bf;

		L0456:
			if (participantIdx != this.owner.World.PersonParticipantIdx) goto L0499;

			// Sound of chopping wood while the new city is built
			this.owner.Services.PerformCityFoundedSound();

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L0484;

			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0484;
			
			this.owner.Diplomats.ExamineCity(this.processor.PullLocalSignedWord(0x8));

		L0484:
			this.owner.Advisors.RenewCityScreen(this.processor.PullLocalSignedWord(0x8));

			this.owner.TurnHooks.RepaintMainScreen();

			goto L04fd;

		L0499:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x8));

			this.owner.Advisors.PickCityProduction(participantIdx,
				this.processor.PullLocalSignedWord(0x8));
			
			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			this.owner.CityOps.PlayerEnjoysWonderEffect(this.owner.World.PersonParticipantIdx, 0x13);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L04fd;
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.PersonParticipantIdx;
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.ChartSight[this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.Y] |= (sbyte)this.processor.RegCount.LowByte;

			this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ShownExtent = 1;

			this.owner.MapOps.KeepKnownTileImprovements(
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.Y);

		L04fd:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			goto L053b;

		L0502:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L00f8;

			this.processor.EmitGlobalWord(0x2f9e, 0x4);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x4f74);

			this.owner.TurnHooks.DisplayDialog(0xba06, 64, 80);

			goto L00f8;

		L053b:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("EstablishCity");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		public void HandleCityDisasters(short townIdx)
		{
			this.processor.Journal.OpenScope($"HandleCityDisasters({townIdx})");

			// function body
			this.processor.EnterFrame(0x10);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0xc, 0x0);
			this.processor.EmitLocalWord(0xa, 0x0);

		L0552:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainTerrainType(
				this.owner.World.Towns[townIdx].Place.X + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)),
				this.owner.World.Towns[townIdx].Place.Y + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0xc, this.processor.BitOrWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0552;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(10));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L05c9;
			goto L0bc6;

		L05c9:
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L05d1;
				case 1:
					goto L0698;
				case 2:
					goto L0703;
				case 3:
					goto L0750;
				case 4:
					goto L07a4;
				case 5:
					goto L07fe;
				case 6:
					goto L08b1;
				case 7:
					goto L08e8;
				case 8:
					goto L08e8;
				case 9:
					goto L08e8;
			}

		L05d1:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xc), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L05da;
			goto L0bc6;

		L05da:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0xfffe);
			if (this.processor.FlagSet.IsNotEqual) goto L05ed;
			goto L0bc6;

		L05ed:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(24));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L05ed;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = this.owner.World.Towns[townIdx].StructureMarkers0;
			this.processor.RegData.Value16 = this.owner.World.Towns[townIdx].StructureMarkers1;
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x10);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, this.processor.RegData.Value16);
			this.processor.RegCount.Value16 = this.processor.BitOrWord(this.processor.RegCount.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsEqual) goto L05ed;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x10);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= this.processor.RegData.Value16;

			this.owner.Libc.CopyText(0xba06, "Earthquake in ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0x10));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " destroyed.\n");

			goto L0bc6;

		L0698:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, 10);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L06b9;

			goto L0bc6;

		L06b9:
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L06c4;
			goto L0bc6;

		L06c4:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, this.processor.PullGlobalSignedWord(0xcc4));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L06dd;

			goto L0bc6;

		L06dd:
			this.owner.BlockText.DisplayTextDialog(0x4f99);

			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L06f3;
			goto L0bc6;

		L06f3:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x4;

		L06fa:
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.owner.World.Towns[townIdx].TrueExtent -= (sbyte)this.processor.RegAcc.LowByte;
			goto L0bc6;

		L0703:
			this.processor.TestBitsWord(this.processor.PullLocalWord(0xc), 0x800);
			if (this.processor.FlagSet.IsNotEqual) goto L070d;
			goto L0bc6;

		L070d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L071f;
			goto L0bc6;

		L071f:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, this.processor.PullGlobalSignedWord(0xca6));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0738;

			goto L0bc6;

		L0738:
			this.owner.BlockText.DisplayTextDialog(0x4f9d);

			if (this.owner.World.Towns[townIdx].TrueExtent > 1)
				goto L06f3;

			goto L0bc6;

		L0750:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xc), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L0759;
			goto L0bc6;

		L0759:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L076b;
			goto L0bc6;

		L076b:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, this.processor.PullGlobalSignedWord(0xc2e));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0784;

			goto L0bc6;

		L0784:
			this.owner.BlockText.DisplayTextDialog(0x4fa1);

			if (this.owner.World.Towns[townIdx].TrueExtent > 1)
				goto L079a;

			goto L0bc6;

		L079a:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x3;
			goto L06fa;

		L07a4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07b6;
			goto L0bc6;

		L07b6:
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L07c0;
			goto L0bc6;

		L07c0:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, this.processor.PullGlobalSignedWord(0xc10));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L07d9;

			goto L0bc6;

		L07d9:
			this.owner.BlockText.DisplayTextDialog(0x4fa5);

			this.owner.World.Towns[townIdx].GrainTally = 0;
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.owner.World.Towns[townIdx].TrueExtent -= (sbyte)this.processor.RegAcc.LowByte;

			this.owner.Libc.AppendText(0xba06, "Citizens demand GRANARY.\n");

			goto L0bc6;

		L07fe:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0xfffe);
			if (this.processor.FlagSet.IsNotEqual) goto L0811;
			goto L0bc6;

		L0811:
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L081c;
			goto L0bc6;

		L081c:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, this.processor.PullGlobalSignedWord(0xcc4));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L0835;

			goto L0bc6;

		L0835:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(24));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0835;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = this.owner.World.Towns[townIdx].StructureMarkers0;
			this.processor.RegData.Value16 = this.owner.World.Towns[townIdx].StructureMarkers1;
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x10);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, this.processor.RegData.Value16);
			this.processor.RegCount.Value16 = this.processor.BitOrWord(this.processor.RegCount.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0835;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x10);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= this.processor.RegData.Value16;

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0x10));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), this.processor.RegAcc.Value16);

			this.owner.BlockText.DisplayTextDialog(0x4fc3);

			goto L0bc6;

		L08b1:
			this.processor.TestBitsWord(this.processor.PullLocalWord(0xc), 0x400);
			if (this.processor.FlagSet.IsNotEqual) goto L08bb;
			goto L0bc6;

		L08bb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L08cd;
			goto L0bc6;

		L08cd:
			this.owner.BlockText.DisplayTextDialog(0x4fc7);

			this.owner.World.Towns[townIdx].GrainTally = 0;
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			goto L0bc6;

		L08e8:
			this.owner.CityOps.HandleCity(townIdx, -1);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0908;
			goto L0bc6;

		L0908:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0924;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0963;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0968;
			goto L0934;

		L0924:
			this.processor.RegAcc.Value16 = 0x4fcb;

		L0927:
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

		L0934:
			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\nCitizens demand ");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L096d;
			
			this.owner.Libc.AppendText(0xba06, "TEMPLE\n");

			goto L09c3;

		L0963:
			this.processor.RegAcc.Value16 = 0x4fd7;
			goto L0927;

		L0968:
			this.processor.RegAcc.Value16 = 0x4fe6;
			goto L0927;

		L096d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L0981;
			
			this.owner.Libc.AppendText(0xba06, 0x500d);

			goto L09c3;

		L0981:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L0995;
			
			this.owner.Libc.AppendText(0xba06, 0x5019);

			goto L09c3;

		L0995:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, 0x1d) != 0 &&
				(this.owner.World.Towns[townIdx].StructureMarkers0 & 0x400) == 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x5026);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x5031);
			}

		L09c3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0a14;

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.owner.TurnLogic.RepaintTileIfVisible(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.processor.EmitGlobalByte(0xba06, 0x0);

		L0a14:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].GrainTally = 0;
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			this.processor.EmitLocalWord(0x6, 0x4);
			this.processor.EmitLocalWord(0xe, 0xffff);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0a3e;
			goto L0bc6;

		L0a3e:
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0a48;
			goto L0bc6;

		L0a48:
			if (this.owner.World.Participants[this.owner.World.Towns[townIdx].ParticipantIdx].TownTally > 3)
				goto L0a5b;
			goto L0bc6;

		L0a5b:
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L0acd;

		L0a62:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.Helpers.ObtainDistance(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.X,
				this.owner.World.Towns[townIdx].Place.Y,
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.Y);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x2), -1);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0aca;
			this.processor.CopyLocalWord(0x8, 0x6);
			this.processor.CopyLocalWord(0x2, 0xe);

		L0aca:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0acd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0aee;
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0aee;
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x2));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0aee;
			goto L0a62;

		L0aee:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0af7;
			goto L0bc6;

		L0af7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xe));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0xe)].ParticipantIdx !=
				this.owner.World.Towns[townIdx].ParticipantIdx) goto L0b14;
			goto L0bc6;

		L0b14:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0b92;

			this.owner.Libc.CopyText(0xba06, "Residents of ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " admire\nthe prosperity of ");

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0xe));

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

		L0b92:
			this.owner.Diplomats.ComputeInciteCost(townIdx, 
				this.owner.World.Towns[this.processor.PullLocalWord(0xe)].ParticipantIdx);
			
			this.processor.EmitGlobalByte(0xba06, 0x0);

		L0bc6:
			this.processor.CompareByte(this.processor.PullGlobalByte(0xba06), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0bd0;
			goto L0ca3;

		L0bd0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)this.owner.World.Towns[townIdx].ParticipantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0c18;
			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.owner.TurnLogic.RepaintTileIfVisible(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			goto L0ca3;

		L0c18:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ca3;
			this.processor.EmitLocalWord(0x8, 0x0);

		L0c4c:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.IncrementLocalWord(0x8);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0c4c;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);
			this.processor.EmitGlobalWord(0x2f9e, 0x2);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 64);

			this.owner.MapOps.FocusMapView(owner.World.PersonParticipantIdx,
				this.owner.World.Towns[townIdx].Place.X - 10,
				this.owner.World.Towns[townIdx].Place.Y - 6);

		L0ca3:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleCityDisasters");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		public ushort DisplayFinalScore(short participantIdx, bool marker)
		{
			this.processor.Journal.OpenScope($"DisplayFinalScore({participantIdx}, {marker})");

			// function body
			this.processor.EnterFrame(0x1a);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x2, 0x0);
			
			if (!marker) goto L0cc3;

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);

		L0cc3:
			if (!marker) goto L0cd9;

			this.owner.Briefings.DisplayReportPreamble(0x506b, 3);

		L0cd9:
			this.owner.World.Participants[0].Tally = 0;
			
			if (!marker) goto L0d3e;

			this.processor.TestBitsWord((ushort)this.owner.World.StarshipMarkers, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L0d3e;

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x507e, 160, 100, 15);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.RegAcc.Value16 = 0;
			goto L13f3;

		L0d3e:
			this.processor.EmitLocalWord(0x8, 0x1);
			this.processor.EmitLocalWord(0x12, 0x1a);
			this.processor.EmitLocalWord(0xc, 0x1a);
			this.processor.EmitLocalWord(0xe, 0x3e7);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xb25c, 0x3e7);
			this.processor.EmitGlobalWord(0xb2bc, 0x1a);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].LevyRatio = (short)(6 - this.owner.World.Participants[participantIdx].ResearchLevyRatio);
			this.processor.EmitLocalWord(0x10, 0x0);
			goto L0e3c;

		L0d85:
			RenderScoreIcon((ushort)(this.processor.PullLocalWord(0x14) & 1));
			
			this.processor.IncrementLocalWord(0x14);

		L0d96:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0d85;
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0db8;

		L0da5:
			RenderScoreIcon((ushort)((this.processor.PullLocalWord(0x14) & 1) + 2));
			
			this.processor.IncrementLocalWord(0x14);

		L0db8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xe8b8));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			if (this.processor.FlagSet.IsGreater) goto L0da5;
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0df1;

		L0ddd:
			RenderScoreIcon((ushort)((this.processor.PullLocalWord(0x14) & 1) + 4));
			
			this.processor.IncrementLocalWord(0x14);

		L0df1:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0ddd;
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0e19;

		L0e00:
			this.owner.CityOps.ObtainCitizenType(this.processor.PullLocalWord(0x14));

			RenderScoreIcon((ushort)(this.processor.RegAcc.Value16 + 5));
			
			this.processor.IncrementLocalWord(0x14);

		L0e19:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe8b8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0e00;

		L0e21:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x10));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));

		L0e39:
			this.processor.EmitLocalWord(0x10, this.processor.IncrementWord(this.processor.PullLocalWord(0x10)));

		L0e3c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e79;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x10));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0e39;
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx != participantIdx)
				goto L0e39;

			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x10), -1);

			if (!marker) goto L0e21;
			this.processor.EmitLocalWord(0x14, 0x0);
			goto L0d96;

		L0e79:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.owner.World.Participants[participantIdx].LevyRatio = (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitGlobalWord(0xd766, this.processor.RegAcc.Value16);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " Citizens (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0xd766), 10));

			this.owner.Libc.AppendText(0xba06, ")");

			if (!marker) goto L0ef9;

			this.owner.TextPainter.RenderTextWithShadow(
				0xba06,
				16,
				this.processor.PullLocalSignedWord(0xc),
				15);

		L0ef9:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xb2bc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe, 0x3e7);
			this.processor.EmitGlobalWord(0xd764, 0x0);
			this.processor.EmitLocalWord(0x10, 0x1);

		L0f15:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x10)];
			this.processor.CompareWord(this.processor.RegSource.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0f26;
			goto L0fb9;

		L0f26:
			this.processor.CompareWord(this.processor.RegSource.Value16, 0x80);
			if (this.processor.FlagSet.IsNotEqual) goto L0f2f;
			goto L0fb9;

		L0f2f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.RegSource.Value16);
			
			if (this.owner.World.Towns[this.processor.RegSource.Value16].ParticipantIdx != participantIdx) goto L0fb9;

			this.processor.EmitLocalWord(0xe, this.processor.SumWord(this.processor.PullLocalWord(0xe), 0x98));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x12c);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0f6a;

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x5116) + 2, 8, 99);

			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0xe, 0x10);

		L0f6a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			if (!marker) goto L0fb4;

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xb4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0fb4;

			this.owner.CityOps.RenderResourceIcon(this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0xe),
				(short)(this.processor.PullLocalSignedWord(0x12) - 2));
			
			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)((this.processor.PullLocalWord(0x10) * 0x1e) + 0xe6a),
				this.processor.PullLocalSignedWord(0xe) + 20,
				this.processor.PullLocalSignedWord(0x12),
				11);

		L0fb4:
			this.processor.EmitGlobalWord(0xd764, this.processor.SumWord(this.processor.PullGlobalWord(0xd764), 0x14));

		L0fb9:
			if (this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x10)] != -1)
				goto L0fca;
			this.processor.EmitLocalWord(0x8, 0x0);

		L0fca:
			this.processor.EmitLocalWord(0x10, this.processor.IncrementWord(this.processor.PullLocalWord(0x10)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x15);
			if (this.processor.FlagSet.IsGreater) goto L0fd6;
			goto L0f15;

		L0fd6:
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " Achievements (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord(0xd764), 10));

			this.owner.Libc.AppendText(0xba06, ")");

			if (!marker) goto L1049;

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, this.processor.PullLocalSignedWord(0xc), 15);

		L1049:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd764);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd766));
			this.owner.World.Participants[0].Tally = (short)this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0xa));
			this.processor.RegSource.Value16 = (ushort)this.owner.World.Participants[owner.World.PersonParticipantIdx].StarshipInhabitants;
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1068;
			goto L1118;

		L1068:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipVictoryRatio != 0) goto L1072;
			goto L1118;

		L1072:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipVictoryRatio;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegSource.Value16);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1088;
			this.processor.EmitLocalWord(0xe, 0x0);

		L1088:
			this.owner.Libc.CopyText(0xba06, "SpaceShip: (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xe), 10));
			
			if (this.owner.World.Participants[owner.World.PersonParticipantIdx].StarshipArrivalSeason != owner.World.Season) goto L10d0;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.owner.World.Participants[0].Tally += (short)this.processor.RegAcc.Value16;
			goto L10e0;

		L10d0:
			this.owner.Libc.AppendText(0xba06, " possible");

		L10e0:
			this.owner.Libc.AppendText(0xba06, ")");

			if (!marker) goto L1114;

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xb8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1114;

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, this.processor.PullLocalSignedWord(0x12), 15);

		L1114:
			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0xa));

		L1118:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			if (this.owner.World.DirtyCellTally == 0) goto L1174;

			this.owner.Libc.CopyText(0xba06, "Pollution: (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(-10 * this.owner.World.DirtyCellTally), 10));

			this.owner.Libc.AppendText(0xba06, ")   ");

			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.DirtyCellTally);
			this.owner.World.Participants[0].Tally -= (short)this.processor.RegAcc.Value16;

		L1174:
			if (owner.World.Season <= 0) goto L11e3;
			if (this.owner.World.TruceRoundTally == 0) goto L11e3;

			this.owner.Helpers.VerifyValueSpan(this.owner.World.TruceRoundTally * 3, 0, 100);

			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "Peace: (+");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x16), 10));

			this.owner.Libc.AppendText(0xba06, ") ");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x16);
			this.owner.World.Participants[0].Tally += (short)this.processor.RegAcc.Value16;

		L11e3:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally == 0) goto L123a;

			this.owner.Libc.AppendText(0xba06, "Future Tech: (+");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(5 * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally), 10));

			this.owner.Libc.AppendText(0xba06, ")");

			this.processor.RegAcc.Value16 = 0x5;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally);
			this.owner.World.Participants[0].Tally += (short)this.processor.RegAcc.Value16;

		L123a:
			this.processor.CompareByte(this.processor.PullGlobalByte(0xba06), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1269;
			
			if (!marker) goto L1269;

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xb8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1269;

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, this.processor.PullLocalSignedWord(0x12), 15);

			this.processor.EmitLocalWord(0x12, this.processor.SumWord(this.processor.PullLocalWord(0x12), 0xa));

		L1269:
			this.processor.EmitLocalWord(0x18, 0xffff);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb884), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L128e;
			this.processor.RegAcc.Value16 = 0x64;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.BotRivalTally);
			this.processor.RegCount.Value16 = 0x226;
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, (ushort)this.owner.World.RoundTally);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x190);
			this.processor.EmitLocalWord(0x18, this.processor.RegCount.Value16);

		L128e:
			if (this.owner.World.Participants[0].Tally < 0)
			{
				this.owner.World.Participants[0].Tally = 0;
			}

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[0].Tally;
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L12d6;

			this.owner.Libc.CopyText(0xba06, "Total Score: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[0].Tally, 10));
			goto L130c;

		L12d6:
			this.owner.Libc.CopyText(0xba06, "Bonus Score: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x18), 10));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.owner.World.Participants[0].Tally = (short)this.processor.RegAcc.Value16;

		L130c:
			if (!marker) goto L1330;

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xba);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1330;

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, this.processor.PullLocalSignedWord(0x12), 15);

		L1330:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0xba);
			if (this.processor.FlagSet.IsLess) goto L1348;
			this.processor.EmitLocalWord(0x2, 0x1);
			this.processor.EmitGlobalWord(0x5116, this.processor.DecrementWord(this.processor.PullGlobalWord(0x5116)));

			this.owner.Startup.DisplayMouseCursor();

			goto L0cc3;

		L1348:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[0].Tally;
			this.processor.EmitGlobalWord(0xb77e, this.processor.RegAcc.Value16);

			if (!marker)
				goto L13f0;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 35, 192, 250, 7, 8);

			if (this.owner.World.Participants[0].Tally > 0)
			{
				this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[0].Tally / 4, 0, 250);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 35, 192, (short)this.processor.RegAcc.Value16, 7, 11);
			}

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnHooks.RepaintMainScreen();

		L13f0:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[0].Tally;

		L13f3:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("DisplayFinalScore");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		public void RenderScoreIcon(ushort arg1)
		{
			this.processor.Journal.OpenScope($"RenderScoreIcon({arg1})");

			// function body
			this.processor.EnterFrame();
			this.processor.EmitGlobalWord(0xb25c, this.processor.SumWord(this.processor.PullGlobalWord(0xb25c), 0x8));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb25c), 0x124);
			if (this.processor.FlagSet.IsLessOrEqual) goto L141d;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5116);
			this.processor.EmitGlobalWord(0xb2bc, this.processor.SumWord(this.processor.PullGlobalWord(0xb2bc), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xb2bc);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightSignedWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.EmitGlobalWord(0xb25c, this.processor.RegAcc.Value16);

		L141d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb2bc), 0xa0);
			if (this.processor.FlagSet.IsGreater) goto L1442;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullGlobalSignedWord(0xb25c),
				this.processor.PullGlobalSignedWord(0xb2bc),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((arg1 << 1) + 0x6e96)));

		L1442:
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RenderScoreIcon");
		}
	}
}
