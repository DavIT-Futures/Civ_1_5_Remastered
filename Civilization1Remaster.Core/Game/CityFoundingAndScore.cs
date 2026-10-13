using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

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
		/// A random event in the city (one in ten each): earthquake near hills (a building), plague without aqueduct (0x100;
		/// technology 10 protects), flood near a river (0x800 terrain, without city walls 0x80), fire near mountains (0x20,
		/// without temple 8), famine (without granary 4), pirates/vandals (a building, without aqueduct), volcano near
		/// mountains (0x400, without barracks 2), and three in ten a revolt when the unhappy outnumber the happy: a demand,
		/// food and production lost, and a city of another player with more prosperity (happy - unhappy * 32 / distance) may
		/// buy it. A human player sees the message (the first line for a city he only sees). SI and DI get the values of the
		/// original before the calls (HandleCity reads below the stack).
		/// </summary>
		public void HandleCityDisasters(short townIdx)
		{
			this.processor.Journal.OpenScope($"HandleCityDisasters({townIdx})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x10);
			this.processor.PreserveIndexRegisters();

			City town = this.owner.World.Towns[townIdx];
			ushort townOffset = (ushort)(0x1c * townIdx);

			// The terrains of the city's tile and its 8 neighbours
			ushort terrains = 0;
			ushort neighbourIdx;

			this.processor.RegDest.Value16 = townOffset;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(neighbourIdx << 1);

				ushort terrain = this.owner.MapOps.ObtainTerrainType(
					town.Place.X + (short)this.processor.PullGlobalWord((ushort)((neighbourIdx << 1) + 0x1882)),
					town.Place.Y + (short)this.processor.PullGlobalWord((ushort)((neighbourIdx << 1) + 0x18e4)));

				terrains |= (ushort)(1 << (byte)terrain);
			}

			this.processor.EmitLocalWord(0xa, neighbourIdx);
			this.processor.EmitLocalWord(0xc, terrains);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);

			switch (this.owner.Libc.Dice.DrawInt(10))
			{
				case 0:
					if ((terrains & 0x10) != 0 && (town.StructureMarkers0 & 0xfffe) != 0)
					{
						ushort buildingIdx = DestroyRandomBuilding(town);

						this.owner.Libc.CopyText(0xba06, "Earthquake in ");

						this.owner.Conquest.ObtainCityLabel(townIdx);

						this.owner.Libc.AppendText(0xba06, "!\n");

						this.owner.Libc.AppendText(0xba06, (ushort)((ushort)(0x1e * buildingIdx) + 0xbb8));

						this.owner.Libc.AppendText(0xba06, " destroyed.\n");
					}
					break;

				case 1:
					this.processor.RegSource.Value16 = townOffset;

					if (this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 10) == 0 && (town.StructureMarkers0 & 0x100) == 0 &&
						this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)this.processor.PullGlobalWord(0xcc4)) != 0)
					{
						this.owner.BlockText.DisplayTextDialog(0x4f99);

						if (town.TrueExtent != 0)
							town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
					}
					break;

				case 2:
					if ((terrains & 0x800) != 0)
					{
						this.processor.RegSource.Value16 = townOffset;

						if ((town.StructureMarkers0 & 0x80) == 0 && this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)this.processor.PullGlobalWord(0xca6)) != 0)
						{
							this.owner.BlockText.DisplayTextDialog(0x4f9d);

							if (town.TrueExtent > 1)
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
						}
					}
					break;

				case 3:
					if ((terrains & 0x20) != 0)
					{
						this.processor.RegSource.Value16 = townOffset;

						if ((town.StructureMarkers0 & 0x8) == 0 && this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)this.processor.PullGlobalWord(0xc2e)) != 0)
						{
							this.owner.BlockText.DisplayTextDialog(0x4fa1);

							if (town.TrueExtent > 1)
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);
						}
					}
					break;

				case 4:
					this.processor.RegSource.Value16 = townOffset;

					if (town.TrueExtent != 0 && (town.StructureMarkers0 & 0x4) == 0 &&
						this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)this.processor.PullGlobalWord(0xc10)) != 0)
					{
						this.owner.BlockText.DisplayTextDialog(0x4fa5);

						town.GrainTally = 0;
						town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);

						this.owner.Libc.AppendText(0xba06, "Citizens demand GRANARY.\n");
					}
					break;

				case 5:
					this.processor.RegSource.Value16 = townOffset;

					if ((town.StructureMarkers0 & 0xfffe) != 0 && (town.StructureMarkers0 & 0x100) == 0 &&
						this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)this.processor.PullGlobalWord(0xcc4)) != 0)
					{
						ushort buildingIdx = DestroyRandomBuilding(town);

						this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), (ushort)((ushort)(0x1e * buildingIdx) + 0xbb8));

						this.owner.BlockText.DisplayTextDialog(0x4fc3);
					}
					break;

				case 6:
					if ((terrains & 0x400) != 0)
					{
						this.processor.RegSource.Value16 = townOffset;

						if ((town.StructureMarkers0 & 0x2) == 0)
						{
							this.owner.BlockText.DisplayTextDialog(0x4fc7);

							town.GrainTally = 0;
							town.MaterialsTally = 0;
						}
					}
					break;

				default:
					HandleCityRevolt(townIdx);
					break;
			}

			ReportCityDisaster(townIdx);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleCityDisasters");
		}

		/// <summary>
		/// A random building of the city (1 - 23, read like the original: a 16-bit mask sign extended) is destroyed; its number
		/// (frame 0x10)
		/// </summary>
		private ushort DestroyRandomBuilding(City town)
		{
			ushort buildingIdx;

			do
			{
				buildingIdx = (ushort)this.owner.Libc.Dice.DrawInt(24);
				this.processor.EmitLocalWord(0x10, buildingIdx);
			}
			while (buildingIdx == 0 || !CityHasStructure(town, buildingIdx));

			ushort keep = (ushort)~(ushort)(1 << (byte)buildingIdx);

			town.StructureMarkers0 &= keep;
			town.StructureMarkers1 &= (keep & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return buildingIdx;
		}

		/// <summary>Bit b of the structures read like the original: a 16-bit mask sign extended to 32 bits</summary>
		private static bool CityHasStructure(City town, ushort buildingIdx)
		{
			ushort mask = (ushort)(1 << (byte)buildingIdx);
			ushort high = (mask & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return ((town.StructureMarkers0 & mask) | (town.StructureMarkers1 & high)) != 0;
		}

		/// <summary>
		/// The revolt (three times in ten): when the unhappy outnumber the happy the citizens demand a building (a human player
		/// sees it), food and production are lost and, unless it is a capital of a civilization with more than 3 cities, the
		/// city of another player with the most prosperity (from the first cities up to the first destroyed one or this city)
		/// may buy it (ComputeInciteCost)
		/// </summary>
		private void HandleCityRevolt(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			ushort townOffset = (ushort)(0x1c * townIdx);

			this.owner.CityOps.HandleCity(townIdx, -1);

			this.processor.EmitGlobalByte(0xba06, 0);

			if ((short)this.processor.PullGlobalWord(0x70e2) >= (short)this.processor.PullGlobalWord(0x70e4))
				return;

			switch (this.owner.Libc.Dice.DrawInt(3))
			{
				case 0:
					this.owner.Libc.CopyText(0xba06, 0x4fcb);
					break;

				case 1:
					this.owner.Libc.CopyText(0xba06, 0x4fd7);
					break;

				case 2:
					this.owner.Libc.CopyText(0xba06, 0x4fe6);
					break;
			}

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\nCitizens demand ");

			if ((town.StructureMarkers0 & 0x8) == 0)
			{
				this.owner.Libc.AppendText(0xba06, "TEMPLE\n");
			}
			else if ((town.StructureMarkers0 & 0x40) == 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x500d);
			}
			else if ((town.StructureMarkers0 & 0x10) == 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x5019);
			}
			else
			{
				this.processor.RegSource.Value16 = townOffset;

				this.owner.Libc.AppendText(0xba06, this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x1d) != 0 &&
					(town.StructureMarkers0 & 0x400) == 0 ? (ushort)0x5026 : (ushort)0x5031);
			}

			this.processor.RegSource.Value16 = townOffset;

			if (((1 << (byte)town.ParticipantIdx) & (ushort)this.owner.World.ParticipantMarkers) != 0)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				this.owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				this.processor.EmitGlobalByte(0xba06, 0);
			}

			this.processor.RegSource.Value16 = townOffset;

			town.GrainTally = 0;
			town.MaterialsTally = 0;

			short highest = 4;
			short rivalIdx = -1;

			this.processor.EmitLocalWord(0x6, (ushort)highest);
			this.processor.EmitLocalWord(0xe, 0xffff);

			if ((short)this.processor.PullGlobalWord(0x70e2) > (short)this.processor.PullGlobalWord(0x70e4) ||
				(town.StructureMarkers0 & 1) != 0 || this.owner.World.Participants[town.ParticipantIdx].TownTally <= 3)
				return;

			ushort otherIdx;

			for (otherIdx = 0; otherIdx < 0x80 && otherIdx != (ushort)townIdx && this.owner.World.Towns[otherIdx].ConditionMarker != 0xff; otherIdx++)
			{
				this.processor.EmitLocalWord(0x2, otherIdx);
				this.processor.RegSource.Value16 = townOffset;
				this.processor.RegDest.Value16 = (ushort)(0x1c * otherIdx);

				this.owner.Helpers.ObtainDistance(town.Place.X, this.owner.World.Towns[otherIdx].Place.X, town.Place.Y, this.owner.World.Towns[otherIdx].Place.Y);

				short distance = (short)this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0x4, (ushort)distance);

				this.owner.CityOps.HandleCity((short)otherIdx, -1);

				short prosperity = CPU.SignedQuotient((short)((ushort)(this.processor.PullGlobalWord(0x70e2) - this.processor.PullGlobalWord(0x70e4)) << 5), distance);
				this.processor.EmitLocalWord(0x8, (ushort)prosperity);

				if (prosperity > highest)
				{
					highest = prosperity;
					rivalIdx = (short)otherIdx;

					this.processor.EmitLocalWord(0x6, (ushort)highest);
					this.processor.EmitLocalWord(0xe, (ushort)rivalIdx);
				}
			}

			this.processor.EmitLocalWord(0x2, otherIdx);

			if (rivalIdx == -1)
				return;

			this.processor.RegSource.Value16 = townOffset;

			if (this.owner.World.Towns[rivalIdx].ParticipantIdx == town.ParticipantIdx)
				return;

			if ((this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.owner.World.PersonParticipantIdx)) != 0)
			{
				this.owner.Libc.CopyText(0xba06, "Residents of ");

				this.owner.Conquest.ObtainCityLabel(townIdx);

				this.owner.Libc.AppendText(0xba06, " admire\nthe prosperity of ");

				this.owner.Conquest.ObtainCityLabel(rivalIdx);

				this.owner.Libc.AppendText(0xba06, ".\n");

				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
			}

			this.owner.Diplomats.ComputeInciteCost(townIdx, this.owner.World.Towns[rivalIdx].ParticipantIdx);

			this.processor.EmitGlobalByte(0xba06, 0);
		}

		/// <summary>
		/// The message left in 0xba06: a human player's city shows it; a city the human player only sees shows its first line
		/// and the map moves there
		/// </summary>
		private void ReportCityDisaster(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if (this.processor.PullGlobalByte(0xba06) == 0)
				return;

			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			if (((1 << (byte)town.ParticipantIdx) & (ushort)this.owner.World.ParticipantMarkers) != 0)
			{
				this.processor.EmitGlobalWord(0x2f9e, 0x4);

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				this.owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				return;
			}

			if ((this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.owner.World.PersonParticipantIdx)) == 0)
				return;

			ushort lineEnd = 0;

			while (this.processor.PullGlobalByte((ushort)(lineEnd++ + 0xba06)) != 0xa)
			{
			}

			this.processor.EmitLocalWord(0x8, lineEnd);
			this.processor.EmitGlobalByte((ushort)(lineEnd + 0xba06), 0);
			this.processor.EmitGlobalWord(0x2f9e, 0x2);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 64);

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, town.Place.X - 10, town.Place.Y - 6);
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

			// The original keeps SI for its caller and returns the score in AX (TurnEvents compares them for the palace)
			ushort savedSource = this.processor.RegSource.Value16;

			if (marker)
			{
				ushort screenBufferPtr = this.processor.PullGlobalWord(0xaa);
				this.processor.EmitUInt16(this.processor.SegData.Value16, screenBufferPtr, 1);
				this.owner.Briefings.DisplayReportPreamble(0x506b, 3);
			}

			this.owner.World.Participants[0].Tally = 0;

			if (marker && (this.owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.owner.TextPainter.RenderCenteredTextToScreen0(0x507e, 160, 100, 15);
				ushort screenBufferPtr = this.processor.PullGlobalWord(0xaa);
				this.processor.EmitUInt16(this.processor.SegData.Value16, screenBufferPtr, 0);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, 0x140, 0xc8,
					screenBufferPtr,
					0, 0);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();
				this.owner.Startup.DisplayMouseCursor();
				this.owner.TurnHooks.RepaintMainScreen();

				this.processor.RegSource.Value16 = savedSource;
				this.processor.RegAcc.Value16 = 0;
				this.processor.Journal.CloseScope("DisplayFinalScore");
				return 0;
			}

			int citizensY = 0x1a;
			int wondersY = 0x1a;
			this.processor.EmitGlobalWord(0xb25c, 0x3e7);
			this.processor.EmitGlobalWord(0xb2bc, 0x1a);

			short savedLevyRatio = this.owner.World.Participants[participantIdx].LevyRatio;
			this.owner.World.Participants[participantIdx].LevyRatio = (short)(6 - this.owner.World.Participants[participantIdx].ResearchLevyRatio);

			int totalCitizenScore = 0;
			for (short townIdx = 0; townIdx < 128; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff)
					continue;

				if (this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				this.owner.CityOps.HandleCity(townIdx, -1);

				int happyCount = this.processor.PullGlobalWord(0x70e2);
				int unhappyCount = this.processor.PullGlobalWord(0x70e4);
				int specialistCount = this.processor.PullGlobalWord(0xe8b8);
				int contentCount = this.owner.World.Towns[townIdx].TrueExtent - happyCount - unhappyCount - specialistCount;

				if (marker)
				{
					for (int i = 0; i < happyCount; i++)
					{
						RenderScoreIcon((ushort)(i & 1));
					}
					for (int i = 0; i < contentCount; i++)
					{
						RenderScoreIcon((ushort)((i & 1) + 2));
					}
					for (int i = 0; i < unhappyCount; i++)
					{
						RenderScoreIcon((ushort)((i & 1) + 4));
					}
					for (int i = 0; i < specialistCount; i++)
					{
						this.owner.CityOps.ObtainCitizenType((ushort)i);
						RenderScoreIcon((ushort)(this.processor.RegAcc.Value16 + 5));
					}
				}

				totalCitizenScore += ScoreRules.ComputeCitizenScore(happyCount, contentCount, specialistCount);
			}

			this.owner.World.Participants[participantIdx].LevyRatio = savedLevyRatio;
			this.processor.EmitGlobalWord(0xd766, (ushort)totalCitizenScore);

			ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
			this.owner.Libc.CopyText(0xba06, civNamePtr);
			this.owner.Libc.AppendText(0xba06, " Citizens (");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)totalCitizenScore, 10));
			this.owner.Libc.AppendText(0xba06, ")");

			if (marker)
			{
				this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, (short)citizensY, 15);
			}

			wondersY = this.processor.PullGlobalWord(0xb2bc) + 0x10;
			int wonderTitleY = wondersY;
			int wonderColX = 0x3e7;
			int totalWonderScore = 0;

			for (short wonderIdx = 1; wonderIdx <= 21; wonderIdx++)
			{
				short wonderCityIdx = this.owner.World.MarvelTownIdx[wonderIdx];
				if (wonderCityIdx != -1 && wonderCityIdx != 0x80)
				{
					if (this.owner.World.Towns[wonderCityIdx].ParticipantIdx == participantIdx)
					{
						wonderColX += 0x98;
						if (wonderColX > 0x12c)
						{
							this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x5116) + 2, 8, 99);
							wondersY += this.processor.RegAcc.Value16;
							wonderColX = 0x10;
						}

						if (marker && wondersY < 0xb4)
						{
							this.owner.CityOps.RenderResourceIcon((short)(wonderIdx + 0x18), (short)wonderColX, (short)(wondersY - 2));
							this.owner.TextPainter.RenderTextToScreen0((ushort)(wonderIdx * 0x1e + 0xe6a), (short)(wonderColX + 20), (short)wondersY, 11);
						}

						totalWonderScore += 20;
					}
				}
			}

			this.processor.EmitGlobalWord(0xd764, (ushort)totalWonderScore);
			this.owner.Libc.CopyText(0xba06, civNamePtr);
			this.owner.Libc.AppendText(0xba06, " Achievements (");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)totalWonderScore, 10));
			this.owner.Libc.AppendText(0xba06, ")");

			if (marker)
			{
				this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, (short)wonderTitleY, 15);
			}

			this.owner.World.Participants[0].Tally = (short)(totalWonderScore + totalCitizenScore);
			wondersY += 10;

			short humanPlayer = this.owner.World.PersonParticipantIdx;
			int starshipInhabitants = this.owner.World.Participants[humanPlayer].StarshipInhabitants;
			int victoryRatio = this.owner.World.Participants[humanPlayer].StarshipVictoryRatio;

			if (starshipInhabitants > 0 && victoryRatio > 0)
			{
				int spaceshipBonus = ScoreRules.ComputeSpaceshipScore(victoryRatio, starshipInhabitants);

				this.owner.Libc.CopyText(0xba06, "SpaceShip: (");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)spaceshipBonus, 10));

				if (this.owner.World.Participants[humanPlayer].StarshipArrivalSeason == this.owner.World.Season)
				{
					this.owner.World.Participants[0].Tally += (short)spaceshipBonus;
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, " possible");
				}

				this.owner.Libc.AppendText(0xba06, ")");

				if (marker && wondersY < 0xb8)
				{
					this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, (short)wondersY, 15);
				}

				wondersY += 10;
			}

			this.processor.EmitGlobalByte(0xba06, 0);

			if (this.owner.World.DirtyCellTally > 0)
			{
				int penalty = ScoreRules.ComputePollutionPenalty(this.owner.World.DirtyCellTally);
				this.owner.Libc.CopyText(0xba06, "Pollution: (");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)(-penalty), 10));
				this.owner.Libc.AppendText(0xba06, ")   ");
				this.owner.World.Participants[0].Tally -= (short)penalty;
			}

			if (this.owner.World.Season > 0 && this.owner.World.TruceRoundTally > 0)
			{
				int peaceBonus = ScoreRules.ComputePeaceBonus(this.owner.World.TruceRoundTally);
				this.owner.Libc.AppendText(0xba06, "Peace: (+");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)peaceBonus, 10));
				this.owner.Libc.AppendText(0xba06, ") ");
				this.owner.World.Participants[0].Tally += (short)peaceBonus;
			}

			if (this.owner.World.Participants[humanPlayer].ComingScienceTally > 0)
			{
				int futureTechBonus = ScoreRules.ComputeFutureTechScore(this.owner.World.Participants[humanPlayer].ComingScienceTally);
				this.owner.Libc.AppendText(0xba06, "Future Tech: (+");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)futureTechBonus, 10));
				this.owner.Libc.AppendText(0xba06, ")");
				this.owner.World.Participants[0].Tally += (short)futureTechBonus;
			}

			if (this.processor.PullGlobalByte(0xba06) != 0 && marker && wondersY < 0xb8)
			{
				this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, (short)wondersY, 15);
				wondersY += 10;
			}

			int conquestBonus = -1;
			if (this.processor.PullGlobalWord(0xb884) != 0)
			{
				conquestBonus = ScoreRules.ComputeConquestBonus(this.owner.World.BotRivalTally, this.owner.World.RoundTally);
			}

			if (this.owner.World.Participants[0].Tally < 0)
			{
				this.owner.World.Participants[0].Tally = 0;
			}

			if (conquestBonus != -1 && conquestBonus > this.owner.World.Participants[0].Tally)
			{
				this.owner.Libc.CopyText(0xba06, "Bonus Score: ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)conquestBonus, 10));
				this.owner.World.Participants[0].Tally = (short)conquestBonus;
			}
			else
			{
				this.owner.Libc.CopyText(0xba06, "Total Score: ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[0].Tally, 10));
			}

			if (marker && wondersY < 0xba)
			{
				this.owner.TextPainter.RenderTextWithShadow(0xba06, 16, (short)wondersY, 15);
			}

			this.processor.EmitGlobalWord(0xb77e, (ushort)this.owner.World.Participants[0].Tally);

			if (marker)
			{
				ushort screenBufferPtr = this.processor.PullGlobalWord(0xaa);
				this.owner.Services.FloodBox(screenBufferPtr, 35, 192, 250, 7, 8);

				if (this.owner.World.Participants[0].Tally > 0)
				{
					this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[0].Tally / 4, 0, 250);
					this.owner.Services.FloodBox(screenBufferPtr, 35, 192, (short)this.processor.RegAcc.Value16, 7, 11);
				}

				this.processor.EmitUInt16(this.processor.SegData.Value16, screenBufferPtr, 0);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, 0x140, 0xc8,
					screenBufferPtr,
					0, 0);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();
				this.owner.Startup.DisplayMouseCursor();
				this.owner.TurnHooks.RepaintMainScreen();
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[0].Tally;
			this.processor.Journal.CloseScope("DisplayFinalScore");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The next icon of the score screen: 8 pixels right of the last one (0xb25c); after x 292 a new row (0xb2bc grows by
		/// 0x5116) starting at x 16 or 20. Nothing is drawn below row 160.
		/// </summary>
		/// <param name="arg1">Picture of the icon, from the table at 0x6e96</param>
		public void RenderScoreIcon(ushort arg1)
		{
			this.processor.Journal.OpenScope($"RenderScoreIcon({arg1})");

			short iconLeft = (short)(this.processor.PullGlobalWord(0xb25c) + 8);
			this.processor.EmitGlobalWord(0xb25c, (ushort)iconLeft);

			if (iconLeft > 0x124)
			{
				short iconTop = (short)(this.processor.PullGlobalWord(0xb2bc) + this.processor.PullGlobalWord(0x5116));
				this.processor.EmitGlobalWord(0xb2bc, (ushort)iconTop);
				this.processor.EmitGlobalWord(0xb25c, (ushort)(((iconTop >> 1) & 0x4) + 0x10));
			}

			if (this.processor.PullGlobalSignedWord(0xb2bc) <= 0xa0)
			{
				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
					this.processor.PullGlobalSignedWord(0xb25c),
					this.processor.PullGlobalSignedWord(0xb2bc),
					this.processor.PullGlobalWord((ushort)((arg1 << 1) + 0x6e96)));
			}

			this.processor.Journal.CloseScope("RenderScoreIcon");
		}
	}
}
