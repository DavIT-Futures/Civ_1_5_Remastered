using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1ade.
	/// </summary>
	public class ScienceAndAdvisors
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public ScienceAndAdvisors(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// The turn of every city of the player (a city that needs it, 0xb1e8, is handled once more with the screen, 2 = again
		/// from the start); then an AI player (with the 4 of the diagnostic markers) sets its taxes: more luxuries with a city
		/// in disorder, less with no city of size above 5 that ended its turn well every fourth turn, science by its leader
		/// (0x1516 + 0x3a * leader) and its gold, none after the last technology (0x3f).
		/// </summary>
		public void HandleAllCities(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandleAllCities({participantIdx})");

			// HandleCity reads leftovers under the stack, so it is called at the depth of the original, and the final values of the
			// locals stay in the frame
			this.processor.EnterFrame(0xa);
			this.processor.PreserveIndexRegisters();

			this.processor.EmitGlobalWord(0xdb42, 0);
			this.processor.EmitGlobalWord(0xe3c2, 0);

			// 1: a city of size above 5 whose turn returned 0, 2: a city in disorder
			int unrest = 0;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff || this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				while (true)
				{
					this.owner.CityOps.HandleCity(townIdx, 0);

					if (this.processor.RegAcc.Value16 == 0 && this.owner.World.Towns[townIdx].TrueExtent > 5)
					{
						unrest |= 1;
					}

					if ((this.owner.World.Towns[townIdx].ConditionMarker & 1) != 0)
					{
						unrest |= 2;
					}

					if (this.processor.PullGlobalWord(0xb1e8) == 0)
						break;

					this.owner.CityOps.HandleCity(townIdx, 1);

					this.owner.TurnHooks.RepaintMainScreen();

					bool again = this.processor.PullGlobalWord(0xb1e8) == 2;

					this.processor.EmitGlobalWord(0xb1e8, 0);

					if (!again)
						break;
				}
			}

			this.processor.EmitLocalWord(0x2, 0x80);
			this.processor.EmitLocalWord(0x8, (ushort)unrest);

			if (participantIdx == this.owner.World.PersonParticipantIdx || (this.owner.World.DiagMarkers & 4) == 0)
			{
				this.processor.RegSource.Value16 = this.processor.UnstackWord();
				this.processor.RegDest.Value16 = this.processor.UnstackWord();
				this.processor.LeaveFrame();
				this.processor.Journal.CloseScope("HandleAllCities");
				return;
			}

			Player participant = this.owner.World.Participants[participantIdx];
			short rates = (short)(participant.ResearchLevyRatio + participant.LevyRatio);
			short luxuries = (short)(10 - rates);

			if ((unrest & 2) != 0 && rates > 6)
			{
				luxuries++;
			}

			if ((this.owner.World.RoundTally & 3) == 0 && unrest == 0 && (short)(participant.LevyRatio + participant.ResearchLevyRatio) < 8)
			{
				luxuries--;
			}

			luxuries = (short)this.owner.Helpers.VerifyValueSpan(luxuries, 0, 4);
			this.processor.EmitLocalWord(0x6, (ushort)luxuries);

			participant.ResearchLevyRatio = (short)(this.processor.PullGlobalWord((ushort)(0x1516 + (0x3a * participant.ChiefVisuals))) +
				((10 - luxuries) / 2));

			if (participant.Gold > (short)(this.owner.World.RoundTally + 0x64))
			{
				participant.ResearchLevyRatio++;
			}

			if (PlayerKnowsTechnology(participantIdx, 0x3f) != 0)
			{
				participant.ResearchLevyRatio = 0;
			}

			participant.LevyRatio = (short)(10 - participant.ResearchLevyRatio - luxuries);

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleAllCities");
		}

		/// <summary>
		/// Destroys the city at the position: notes it in the replay, takes it from its player (and the unit it was building),
		/// removes it from the map, the trade routes of the other cities and the wonders (city 128 = destroyed), eliminates the
		/// units it supported, applies 0x1643 to the stack still on the tile, and redraws the territory around it (map layer 2;
		/// the outer ring only when the nearest city, 0x6c9a, is more than 3 away). The stack frame and the saved SI and DI stay
		/// like in the original: VisitEachUnitInStack changes SI, and the unit functions run at the same stack depth.
		/// </summary>
		public void FinalizeProduction(short townIdx, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"FinalizeProduction({townIdx}, {horizLoc}, {vertLoc})");

			this.processor.EnterFrame(0xa);
			this.processor.PreserveIndexRegisters();

			City town = this.owner.World.Towns[townIdx];

			this.owner.UnitOps.AttachReplayContent(1, 0xff, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			if (town.PresentMakingIdx >= 0)
			{
				this.owner.World.Participants[town.ParticipantIdx].TroopsInsideMaking[town.PresentMakingIdx]--;
			}

			this.owner.World.Participants[town.ParticipantIdx].TownTally--;
			town.ConditionMarker = 0xff;

			this.owner.MapOps.AssignTileImprovements(0, horizLoc, vertLoc);

			if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (1 << this.owner.World.PersonParticipantIdx)) != 0)
			{
				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			}

			for (int otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				for (int routeIdx = 0; routeIdx < 3; routeIdx++)
				{
					if (this.owner.World.Towns[otherIdx].CommerceTownIndices[routeIdx] == townIdx)
					{
						this.owner.World.Towns[otherIdx].CommerceTownIndices[routeIdx] = -1;
					}
				}
			}

			for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
			{
				if (this.owner.World.MarvelTownIdx[wonderIdx] == townIdx)
				{
					this.owner.World.MarvelTownIdx[wonderIdx] = 128;
				}
			}

			UnitMemory troops = new UnitMemory(this.processor);

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				short ownerIdx = town.ParticipantIdx;

				if (troops.Exists(ownerIdx, troopIdx) && troops.PullSigned(ownerIdx, troopIdx, UnitMemory.BaseTown) == townIdx)
				{
					this.owner.UnitOps.EliminateUnit(ownerIdx, troopIdx);
				}
			}

			ushort stack = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			if (stack != 0xffff)
			{
				this.owner.UnitOps.VisitEachUnitInStack(town.ParticipantIdx, (short)stack, 0x1643);
			}

			for (int spotIdx = 0; spotIdx < 0x2d; spotIdx++)
			{
				int spotHoriz = this.owner.Routing.TunePosition((short)this.processor.PullGlobalWord((ushort)((spotIdx << 1) + 0x1882)) + horizLoc);
				short spotVert = (short)(this.processor.PullGlobalWord((ushort)((spotIdx << 1) + 0x18e4)) + vertLoc);

				this.owner.Helpers.LocateNearestCity(spotHoriz, spotVert);

				if (spotIdx <= 0x14 || (short)this.processor.PullGlobalWord(0x6c9a) > 3)
				{
					ushort holder = this.owner.VideoCard.ObtainDot(2, spotHoriz + 0x50, spotVert + 0x32);
					this.owner.Services.AssignDot(2, spotHoriz + 0x50, spotVert, holder);
				}
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("FinalizeProduction");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RenderAdvisorScenery()
		{
			this.processor.Journal.OpenScope("RenderAdvisorScenery()");

			// function body
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("RenderAdvisorScenery");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RecoverAdvisorScenery()
		{
			this.processor.Journal.OpenScope("RecoverAdvisorScenery()");

			// function body
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("RecoverAdvisorScenery");
		}

		/// <summary>
		/// Opens the city screen from an advisor: the advisor screen is kept, the city is handled with its screen (HandleCity),
		/// then the advisor screen comes back, or the main screen when the city screen asked for it (0x6b64)
		/// </summary>
		public void RenewCityScreen(short townIdx)
		{
			this.processor.Journal.OpenScope($"RenewCityScreen({townIdx})");

			RenderAdvisorScenery();

			this.processor.EmitGlobalWord(0x6b64, 0x0);

			this.owner.CityOps.HandleCity(townIdx, 1);

			if (this.processor.PullGlobalWord(0x6b64) != 0)
			{
				this.owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				RecoverAdvisorScenery();
			}

			this.processor.Journal.CloseScope("RenewCityScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="cityID"></param>
		/// <returns></returns>
		public ushort PickCityProduction(short participantIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"PickCityProduction({participantIdx}, {townIdx})");

			// function body
			this.processor.EnterFrame(0x14a);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalByte(0x12e, 0x2a);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x13a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.CityOps.HandleCity(townIdx, -1);

		L0447:
			this.processor.EmitLocalWord(0xa, 0x1);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xde, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "What shall we build in ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "?\n ");

			this.processor.CompareWord(this.processor.PullLocalWord(0x13a), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0492;
			goto L05f4;

		L0492:
			this.processor.EmitLocalWord(0xe0, 0x0);
			goto L05c5;

		L049b:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe0));

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x114a)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L04b9;

			goto L05c1;

		L04b9:
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1136)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L04ce;

			goto L05c1;

		L04ce:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L04e7;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L04e7;
			goto L05c1;

		L04e7:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe0));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			ComposeProductionOption(townIdx, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1142)));

			this.owner.Libc.AppendText(0xba06, ", ADM:");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113e)), 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1140)), 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113a)), 10));

			this.owner.Libc.AppendText(0xba06, ")\n ");

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.IncrementLocalWord(0xa);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0xde), this.processor.RegAcc.Value16);

		L05c1:
			this.processor.EmitLocalWord(0xe0, this.processor.IncrementWord(this.processor.PullLocalWord(0xe0)));

		L05c5:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x1c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L05f4;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x19);
			if (this.processor.FlagSet.IsEqual) goto L05d6;
			goto L049b;

		L05d6:
			if (this.owner.World.MarvelTownIdx[17] == -1) goto L05c1;

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, 0x1b);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L05f2;

			goto L049b;

		L05f2:
			goto L05c1;

		L05f4:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitLocalWord(0x136, this.processor.RegAcc.Value16);

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0x138, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x6, 0x0);

			this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 15);
			
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L066b;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)this.owner.World.MarvelTownIdx[15]);
			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[15]].Place.X,
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[15]].Place.Y);

			this.processor.EmitLocalWord(0x146, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x146));
			if (this.processor.FlagSet.IsNotEqual) goto L066b;
			this.processor.EmitLocalWord(0x6, 0x1);

		L066b:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0675;
			goto L082f;

		L0675:
			this.processor.EmitLocalWord(0xe0, 0x1);
			goto L06a3;

		L067d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x15);
			if (this.processor.FlagSet.IsGreater) goto L0687;
			goto L07e3;

		L0687:
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[19], 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L069f;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L069f;
			goto L07e3;

		L069f:
			this.processor.EmitLocalWord(0xe0, this.processor.IncrementWord(this.processor.PullLocalWord(0xe0)));

		L06a3:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x18);
			if (this.processor.FlagSet.IsLessOrEqual) goto L06ad;
			goto L082f;

		L06ad:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe0));
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb6)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L069f;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = this.owner.World.Towns[townIdx].StructureMarkers0;
			this.processor.RegData.Value16 = this.owner.World.Towns[townIdx].StructureMarkers1;
			this.processor.EmitLocalWord(0x14a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x148, this.processor.RegData.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe0);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14a));
			this.processor.RegData.Value16 = this.processor.BitAndWord(this.processor.RegData.Value16, this.processor.PullLocalWord(0x148));
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L069f;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L0709;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x14a), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L069f;

		L0709:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L0723;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x8000);
			if (this.processor.FlagSet.IsEqual) goto L0723;
			goto L069f;

		L0723:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L074e;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L074e;
	
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullGlobalSignedWord(0xc4c));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L074e;

			goto L069f;

		L074e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0xc);
			if (this.processor.FlagSet.IsNotEqual) goto L0779;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L0779;
	
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullGlobalSignedWord(0xc6a));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L0779;

			goto L069f;

		L0779:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x14);
			if (this.processor.FlagSet.IsNotEqual) goto L0792;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L0792;
			goto L069f;

		L0792:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x13);
			if (this.processor.FlagSet.IsNotEqual) goto L07ac;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4000);
			if (this.processor.FlagSet.IsNotEqual) goto L07ac;
			goto L069f;

		L07ac:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x13);
			if (this.processor.FlagSet.IsEqual) goto L07c4;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x14);
			if (this.processor.FlagSet.IsEqual) goto L07c4;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x15);
			if (this.processor.FlagSet.IsEqual) goto L07c4;
			goto L067d;

		L07c4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L07cd;
			goto L069f;

		L07cd:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x1c);
			if (this.processor.FlagSet.IsNotEqual) goto L07e0;
			goto L067d;

		L07e0:
			goto L069f;

		L07e3:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe0));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			ComposeProductionOption(townIdx, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb2)));

			this.owner.Libc.AppendText(0xba06, ")\n ");

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.IncrementLocalWord(0xa);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0xde), this.processor.RegAcc.Value16);
			goto L069f;

		L082f:
			this.processor.EmitLocalWord(0xe0, 0x19);

		L0835:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe0));
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb6)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L08cf;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0xe0) - 0x18], 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L08cf;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x10ce)), 0x7f);
			if (this.processor.FlagSet.IsEqual) goto L0886;

			this.processor.TestBitsByte((byte)this.owner.World.ScienceLeadingFoundVia[this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x10ce))], 0x7);
			if (this.processor.FlagSet.IsEqual) goto L0886;

			this.owner.Libc.AppendText(0xba06, "*");

		L0886:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe0));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			ComposeProductionOption(townIdx, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb2)));

			this.owner.Libc.AppendText(0xba06, ")\n ");

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.IncrementLocalWord(0xa);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0xde), this.processor.RegAcc.Value16);

		L08cf:
			this.processor.EmitLocalWord(0xe0, this.processor.IncrementWord(this.processor.PullLocalWord(0xe0)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x2d);
			if (this.processor.FlagSet.IsGreater) goto L08dd;
			goto L0835;

		L08dd:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L092a;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L092a;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x1e);
			if (this.processor.FlagSet.IsLessOrEqual) goto L092a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x13a), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L090a;

		L0901:
			this.processor.EmitLocalWord(0x13a, 0x1);
			goto L0447;

		L090a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x138);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xba06);
			this.owner.Libc.CopyText(this.processor.RegAcc.Value16, "more...\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x136);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xde), 0x63);

		L092a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x13a), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L094f;

			this.owner.Libc.AppendText(0xba06, "more...\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.IncrementLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xde), 0x62);

		L094f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.EmitLocalWord(0x13e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.RegAcc.Value16].Approach;
			this.processor.EmitLocalWord(0x142, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L098c;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0991;

		L098c:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0993;

		L0991:
			this.processor.RegAcc.Value16 = 0;

		L0993:
			this.processor.EmitLocalWord(0x13c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x8078), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L09ab;
			this.processor.CompareWord(this.processor.PullLocalWord(0x142), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L09ab;
			this.processor.EmitLocalWord(0x13c, 0x1);

		L09ab:
			if (this.owner.World.Participants[participantIdx].RegimeKind <= 1) goto L09bc;
			this.processor.RegAcc.Value16 = 0x2;
			goto L09bf;

		L09bc:
			this.processor.RegAcc.Value16 = 0x1;

		L09bf:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe0, 0x0);

		L09e3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x11a), 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x44), 0x0);
			this.processor.IncrementLocalWord(0xe0);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L09e3;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].TroopTally;
			this.processor.EmitLocalWord(0x140, this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].TroopTally = 0;
			this.processor.EmitLocalWord(0x4a, 0x0);

		L0a19:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4a));
			
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].ConditionMarker == 0xff ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].ParticipantIdx != participantIdx) goto L0aa5;

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x13e));
			if (this.processor.FlagSet.IsNotEqual) goto L0aa5;
			this.processor.CompareByte((byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L0a68;
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0a68;
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x11a), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x11a))));

		L0a68:
			this.processor.EmitLocalWord(0x11c, 0x0);

		L0a6e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4a));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x11c);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4a)].Unfamiliar[this.processor.PullLocalWord(0x11c)];
			this.processor.EmitLocalByte(0x14a, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0a9a;

			this.owner.World.Participants[participantIdx].TroopTally++;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.BitAndWord(this.processor.RegSource.Value16, 0x3f);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x44), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x44))));

		L0a9a:
			this.processor.EmitLocalWord(0x11c, this.processor.IncrementWord(this.processor.PullLocalWord(0x11c)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x11c), 0x2);
			if (this.processor.FlagSet.IsLess) goto L0a6e;

		L0aa5:
			this.processor.EmitLocalWord(0x4a, this.processor.IncrementWord(this.processor.PullLocalWord(0x4a)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0ab2;
			goto L0a19;

		L0ab2:
			this.processor.EmitLocalWord(0xe0, 0x0);

		L0ab8:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0xe0));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0aff;

			this.owner.World.Participants[participantIdx].TroopTally++;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x13e));
			if (this.processor.FlagSet.IsNotEqual) goto L0aff;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x44), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x44))));

		L0aff:
			this.processor.EmitLocalWord(0xe0, this.processor.IncrementWord(this.processor.PullLocalWord(0xe0)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0ab8;
			if (this.owner.World.Participants[participantIdx].TroopTally != 0) goto L0b2c;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0b2c;
			if (this.owner.World.Participants[participantIdx].RegimeKind > 1) goto L0b2c;
			this.processor.RegAcc.Value16 = 0x1;
			goto L14e7;

		L0b2c:
			this.processor.CompareWord((ushort)participantIdx, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0b4a;
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, 0x520), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L0b44;
			this.processor.RegAcc.Value16 = 0x3;
			goto L14e7;

		L0b44:
			this.processor.RegAcc.Value16 = 0x7;
			goto L14e7;

		L0b4a:
			this.processor.EmitLocalWord(0x8, 0x0);
			this.processor.ScaleIndexToSource(0x1c, (ushort)townIdx);

			this.owner.MapOps.ObtainUnitAtTile(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.processor.EmitLocalWord(0xe0, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0b7b;
			this.processor.EmitLocalWord(0x8, 0x2);
			goto L0baf;

		L0b7b:
			this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, this.processor.PullLocalSignedWord(0xe0), 2);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.LowByte = 0x4;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegData.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x13c));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0baf;
			this.processor.EmitLocalWord(0x8, 0x1);

		L0baf:
			this.processor.RegAcc.Value16 = 0x3e7;
			this.processor.EmitLocalWord(0x11e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x130, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe0, 0x1);
			goto L0f57;

		L0bcf:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0be1;
			goto L0ed5;

		L0be1:
			this.processor.EmitLocalWord(0x134, 0xfffc);
			goto L0ed5;

		L0bea:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2e0);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);

		L0bef:
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xc);

		L0bf2:
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);

		L0bf4:
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			goto L0ed5;

		L0bfb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0c0d;
			goto L0ed5;

		L0c0d:
			this.processor.EmitLocalWord(0x134, 0xfffe);
			goto L0ed5;

		L0c16:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0c28;
			goto L0ed5;

		L0c28:
			this.processor.EmitLocalWord(0x134, 0xffff);
			goto L0ed5;

		L0c31:
			this.processor.EmitLocalWord(0x134, 0x5);

		L0c37:
			this.processor.EmitLocalWord(0x132, 0x1);
			goto L0ed5;

		L0c40:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L0c4c;

		L0c47:
			this.processor.RegAcc.Value16 = 0x4;
			goto L0bf4;

		L0c4c:
			this.processor.RegAcc.Value16 = 0x8;
			goto L0bf4;

		L0c51:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xd);
			goto L0bf2;

		L0c5e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x5;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			goto L0bef;

		L0c69:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.DivideAccumulatorByPowerOfTwo(0x3);
			goto L0bef;

		L0c7d:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0ca0;
			goto L0ed5;

		L0ca0:
			this.processor.EmitLocalWord(0x134, 0xfffd);
			goto L0ed5;

		L0ca9:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);

		L0cb2:
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			goto L0bf2;

		L0cb8:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			goto L0cb2;

		L0ccb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.LowByte = 0x2;
			this.processor.DivideSignedByte(this.processor.RegAcc, this.processor.RegCount.LowByte);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0cf0;
			goto L0c37;

		L0cf0:
			this.processor.EmitLocalWord(0x134, this.processor.SubtractWord(this.processor.PullLocalWord(0x134), 0x4));
			goto L0c37;

		L0cf8:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.HalveAccumulator();
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0d1d;
			goto L0ed5;

		L0d1d:
			this.processor.RegAcc.Value16 = this.processor.RegDest.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xe);
			goto L0bf2;

		L0d27:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L0d43;
			goto L0ed5;

		L0d43:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L0d54;
			goto L0c47;

		L0d54:
			this.processor.RegAcc.Value16 = 0x2;
			goto L0bf4;

		L0d5a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].CommerceTownIndices[2], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L0da7;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].CommerceTownIndices[0], 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0d86;
			this.processor.EmitLocalWord(0x134, this.processor.SumWord(this.processor.PullLocalWord(0x134), 0x2));

		L0d86:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].CommerceTownIndices[1], 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0d9a;
			this.processor.EmitLocalWord(0x134, this.processor.SumWord(this.processor.PullLocalWord(0x134), 0x2));

		L0d9a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0da7;
			this.processor.EmitLocalWord(0x134, 0x3);

		L0da7:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0db2;
			goto L0ed5;

		L0db2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L0dc4;
			goto L0ed5;

		L0dc4:
			this.processor.EmitLocalWord(0x134, 0x3e7);
			goto L0ed5;

		L0dcd:
			if (this.owner.World.PersonParticipantIdx <= 0)
				goto L0ed5;

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
				goto L0ed5;

			this.processor.ScaleIndexToBase(0x38, (ushort)participantIdx);

			if (this.owner.World.Participants[participantIdx].TroopsInsideMaking[27] > 0)
				goto L0ed5;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(5));

			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x2a6e));
			if (this.processor.FlagSet.IsNotEqual) goto L0e20;
			goto L0ed5;

		L0e20:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0xa, (ushort)participantIdx);

			this.processor.RegDest.Value16 = this.owner.World.Participants[participantIdx].FoundScienceMarkers[this.processor.PullLocalWord(0x4a)];

			this.processor.RegAcc.Value16 = 0xa;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.PersonParticipantIdx);

			this.processor.RegAcc.Value16 = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceMarkers[this.processor.PullLocalWord(0x4a)];
			this.processor.RegAcc.Value16 = this.processor.BitOrWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0e47;
			goto L0ed5;

		L0e47:
			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Participants[participantIdx].FoundScienceTally -
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceTally + 10,
				5, 10);
			goto L0bf4;

		L0ed5:
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), 0x190);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f1c;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x132);
			this.processor.CompareWord(this.processor.PullLocalWord(0x13c), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0eec;
			this.processor.RegAcc.Value16 = 0x14;
			goto L0eef;

		L0eec:
			this.processor.RegAcc.Value16 = 0xa;

		L0eef:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x134));
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x3);
			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x134));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			goto L0f22;

		L0f1c:
			this.processor.EmitLocalWord(0x134, 0x3fff);

		L0f22:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0f34;
			goto L1206;

		L0f34:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x134);
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0x134, this.processor.SumWord(this.processor.PullLocalWord(0x134), this.processor.RegAcc.Value16));
			goto L1206;

		L0f4d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x17);
			if (this.processor.FlagSet.IsNotEqual) goto L0f9d;

		L0f53:
			this.processor.EmitLocalWord(0xe0, this.processor.IncrementWord(this.processor.PullLocalWord(0xe0)));

		L0f57:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe0), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L0f63;
			goto L1268;

		L0f63:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xde));
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L0f79;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1a);
			if (this.processor.FlagSet.IsLess) goto L0f4d;

		L0f79:
			this.processor.EmitLocalWord(0x134, 0x3e7);
			this.processor.EmitLocalWord(0x132, 0x0);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xffe5);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x31);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L0f95;
			goto L0ed5;

		L0f95:
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L0d5a;
				case 1:
					goto L0dcd;
				case 2:
					goto L0ed5;
				case 3:
					goto L0ed5;
				case 4:
					goto L0ed5;
				case 5:
					goto L0ed5;
				case 6:
					goto L0ed5;
				case 7:
					goto L0ed5;
				case 8:
					goto L0ed5;
				case 9:
					goto L0ed5;
				case 10:
					goto L0ed5;
				case 11:
					goto L0ed5;
				case 12:
					goto L0ed5;
				case 13:
					goto L0ed5;
				case 14:
					goto L0ed5;
				case 15:
					goto L0ed5;
				case 16:
					goto L0ed5;
				case 17:
					goto L0ed5;
				case 18:
					goto L0ed5;
				case 19:
					goto L0ed5;
				case 20:
					goto L0ed5;
				case 21:
					goto L0ed5;
				case 22:
					goto L0ed5;
				case 23:
					goto L0ed5;
				case 24:
					goto L0ed5;
				case 25:
					goto L0ed5;
				case 26:
					goto L0ed5;
				case 27:
					goto L0ed5;
				case 28:
					goto L0ed5;
				case 29:
					goto L0c31;
				case 30:
					goto L0c40;
				case 31:
					goto L0bcf;
				case 32:
					goto L0c7d;
				case 33:
					goto L0ca9;
				case 34:
					goto L0bea;
				case 35:
					goto L0ccb;
				case 36:
					goto L0cf8;
				case 37:
					goto L0ca9;
				case 38:
					goto L0bfb;
				case 39:
					goto L0cb8;
				case 40:
					goto L0ed5;
				case 41:
					goto L0c16;
				case 42:
					goto L0c51;
				case 43:
					goto L0c69;
				case 44:
					goto L0ed5;
				case 45:
					goto L0c69;
				case 46:
					goto L0c5e;
				case 47:
					goto L0c51;
				case 48:
					goto L0c69;
				case 49:
					goto L0d27;
			}

		L0f9d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0fd2;
			if (this.owner.World.Participants[participantIdx].RegimeKind <= 1) goto L0fb4;
			this.processor.RegAcc.Value16 = 0x2;
			goto L0fb7;

		L0fb4:
			this.processor.RegAcc.Value16 = 0x1;

		L0fb7:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			if (this.processor.FlagSet.IsLess) goto L0fd2;
			this.processor.ScaleIndexToBase(0x38, (ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[0] != 0x0)
				goto L0f53;

			this.processor.CompareWord(this.processor.PullLocalWord(0x11a), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L0f53;

		L0fd2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0fe7;
			if (this.owner.World.Participants[participantIdx].RegimeKind <= 1) goto L0fe7;
			goto L0f53;

		L0fe7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x19);
			if (this.processor.FlagSet.IsNotEqual) goto L1009;
			this.processor.ScaleIndexToSource(0x38, (ushort)participantIdx);
			
			if (this.owner.World.Participants[participantIdx].LiveTroops[25] >= 4)
				goto L0f53;

			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xd33a)), 0x2);
			if (this.processor.FlagSet.IsLess) goto L1009;
			goto L0f53;

		L1009:
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L1054;
			
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, (ushort)participantIdx);

			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.PersonParticipantIdx);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xb3dc));
			this.processor.HalveAccumulator();
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[participantIdx].LiveTroops[this.processor.PullLocalWord(0x46)];
			this.processor.RegCount.Value16 += (ushort)this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.processor.PullLocalWord(0x46)];
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L103d;
			goto L0f53;

		L103d:
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
				goto L0f53;

		L1054:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x46));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1075;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x11a));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x44)));
			goto L10c7;

		L1075:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x13e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x13e)].Assault < 8)
				goto L0f53;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.AddScaledToSource(0x38, (ushort)participantIdx);

			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[participantIdx].TroopsInsideMaking[this.processor.PullLocalWord(0x46)];
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 += (ushort)this.owner.World.Participants[participantIdx].LiveTroops[this.processor.PullLocalWord(0x46)];
			
			this.processor.RegDest.Value16 = (ushort)participantIdx;
			this.processor.RegData.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x13e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x13e)].Assault;
			this.processor.RegCount.Value16 = this.processor.RegData.Value16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[participantIdx].TroopTally;
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);

		L10c7:
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L10f0;
			this.processor.ScaleIndexToBase(0x22, this.processor.PullLocalWord(0x46));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L10e3;
			goto L0f53;

		L10e3:
			this.processor.RegAcc.Value16 = 0x2;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);

		L10f0:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x46));
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148));
			this.processor.CompareWord(this.processor.RegSource.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1113;
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1113;
			this.processor.EmitLocalWord(0x134, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x134), 0x1));
			this.processor.CompareWord(this.processor.PullLocalWord(0x142), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1113;
			this.processor.EmitLocalWord(0x134, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x134), 0x1));

		L1113:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x46));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L113c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x142), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L112c;
			goto L0f53;

		L112c:
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x134));
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);

		L113c:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x46));
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1179;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L115e;
			this.processor.RegAcc.Value16 = 0x4;
			goto L1161;

		L115e:
			this.processor.RegAcc.Value16 = 0x6;

		L1161:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516)));
			goto L117c;

		L1179:
			this.processor.RegAcc.Value16 = 0x10;

		L117c:
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1142));
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.IncrementWord(this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x134));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113e));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1140)));
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113a));
			this.processor.RegBase.Value16 = this.processor.IncrementWord(this.processor.RegBase.Value16);
			this.processor.RegDest.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToBase(this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.RegData.Value16 = this.processor.RegDest.Value16;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1148)), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L11c6;
			this.processor.CompareWord(this.processor.PullLocalWord(0x13c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L11bb;
			this.processor.RegAcc.Value16 = 0x4;
			goto L11be;

		L11bb:
			this.processor.RegAcc.Value16 = 0x2;

		L11be:
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x134));
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);

		L11c6:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x46));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L11d9;
			this.processor.EmitLocalWord(0x134, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x134), 0x1));

		L11d9:
			if (this.owner.World.Participants[participantIdx].TroopTally > 120)
			{
				this.processor.RegCount.LowByte = 0x2;
				this.processor.EmitLocalWord(0x134, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x134), this.processor.RegCount.LowByte));
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0x13c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L11fb;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1202;

		L11fb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6ed4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1206;

		L1202:
			this.processor.EmitLocalWord(0x134, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0x134), 0x1));

		L1206:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4c);
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L121e;
			this.processor.CopyLocalWord(0xe0, 0x144);
			this.processor.CopyLocalWord(0x134, 0x4c);

		L121e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x0);
			if (this.processor.FlagSet.IsLess) goto L122a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L1249;

		L122a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x130);
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1237;
			goto L0f53;

		L1237:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x134);
			this.processor.EmitLocalWord(0x130, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xe0, 0x48);
			goto L0f53;

		L1249:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x11e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1256;
			goto L0f53;

		L1256:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x134);
			this.processor.EmitLocalWord(0x11e, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xe0, 0xc);
			goto L0f53;

		L1268:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x140);
			this.owner.World.Participants[participantIdx].TroopTally = 
				(short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L12a5;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L1298;
			this.processor.RegAcc.Value16 = 0xffff;
			goto L14e7;

		L1298:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x144);

		L129c:
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xde));
			goto L14e7;

		L12a5:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsEqual) goto L12cd;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L12c7;
			this.processor.CompareWord(this.processor.PullLocalWord(0x130), 0x1f4);
			if (this.processor.FlagSet.IsGreater) goto L12c7;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x48);
			goto L129c;

		L12c7:
			this.processor.RegAcc.Value16 = 0x63;
			goto L14e7;

		L12cd:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L12d7;
			goto L1438;

		L12d7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L12e0;
			goto L1438;

		L12e0:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L12f2;
			goto L1388;

		L12f2:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xde));
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L130f;
			this.processor.ScaleIndex(0x22, this.processor.PullLocalWord(0x134));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			goto L131d;

		L130f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x134);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);

		L131d:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x12d), this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Military Advisor:\n");

			this.owner.BlockText.ImportTextFromFile(0x2284, (ushort)(this.processor.RegFrame.Value16 - 0x12e));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), (ushort)(this.processor.RegFrame.Value16 - 0x12d));

			this.owner.BlockText.ReflowText();

			this.owner.BlockText.ArrangeTextRow(0x32);

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, 8, 160, 1);

		L1388:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1391;
			goto L1427;

		L1391:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xde));
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L13ae;
			this.processor.ScaleIndex(0x22, this.processor.PullLocalWord(0x134));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			goto L13bc;

		L13ae:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x134);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);

		L13bc:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x12d), this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "Domestic Advisor:\n");

			this.owner.BlockText.ImportTextFromFile(0x22a3, (ushort)(this.processor.RegFrame.Value16 - 0x12e));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), (ushort)(this.processor.RegFrame.Value16 - 0x12d));

			this.owner.BlockText.ReflowText();

			this.owner.BlockText.ArrangeTextRow(0x32);

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, 164, 160, 1);

		L1427:
			this.processor.EmitLocalWord(0x4, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			goto L0447;

		L1438:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x14);
			if (this.processor.FlagSet.IsGreater) goto L1445;
			this.processor.CompareWord(this.processor.PullLocalWord(0x13a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L144e;

		L1445:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);

		L144e:
			this.processor.EmitGlobalWord(0x2fa0, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, 80, 8, 1);

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xdc));
			this.processor.EmitLocalWord(0x134, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L1482;
			this.processor.EmitLocalWord(0x13a, 0x2);
			goto L0447;

		L1482:
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), 0x62);
			if (this.processor.FlagSet.IsNotEqual) goto L148c;
			goto L0901;

		L148c:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L14e3;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitGlobalWord(0x3c62, 0x1);
			this.processor.CompareWord(this.processor.PullLocalWord(0x134), 0x0);
			if (this.processor.FlagSet.IsLess) goto L14bd;

			this.owner.Encyclopedia.DisplayCivilopediaRecord(this.processor.PullLocalWord(0x134), 2);

			goto L14c8;

		L14bd:
			this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)(-this.processor.PullLocalSignedWord(0x134)), 1);

		L14c8:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitGlobalWord(0x6b64, 0x1);
			goto L0447;

		L14e3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x134);

		L14e7:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("PickCityProduction");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Appends " (n turns)" or " (1 turn)" to the text at 0xba06: the turns the city needs for the production of cost arg2
		/// (in tens of shields), with the shields per turn 0x70dc - 0xd2f6 (1 - 99); 1 - 999 turns
		/// </summary>
		public void ComposeProductionOption(short townIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"ComposeProductionOption({townIdx}, {arg2})");

			int perTurn = this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x70dc) - this.processor.PullGlobalSignedWord(0xd2f6), 1, 99);

			ushort turns = (ushort)this.owner.Helpers.VerifyValueSpan(
				(((10 * arg2) - this.owner.World.Towns[townIdx].MaterialsTally - 1) / (short)perTurn) + 1, 1, 999);

			this.owner.Libc.AppendText(0xba06, " (");

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)turns, 10));

			this.owner.Libc.AppendText(0xba06, (ushort)(turns != 1 ? 0x22b2 : 0x22b9));

			this.processor.Journal.CloseScope("ComposeProductionOption");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="playerID1"></param>
		public ushort UncoverTechnology(short participantIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"UncoverTechnology({participantIdx}, {participantIdx1})");

			// function body
			this.processor.EnterFrame(0x46);
			this.processor.PreserveSource();

			if (participantIdx != 0)
				goto L1597;

		L1591:
			this.processor.RegAcc.Value16 = 0xffff;
			goto L1d29;

		L1597:
			this.processor.EmitLocalWord(0x16, 0x0);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x46, 0x0);

		L15aa:
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullLocalSignedWord(0x46));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L162c;

			this.processor.ScaleIndexToSource(0x16, this.processor.PullLocalWord(0x46));
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, (short)this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L162c;

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, (short)this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) 
				goto L162c;

			ObtainTechnologyInfo(participantIdx, this.processor.PullLocalWord(0x46));

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));

			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L162c;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1620;
			
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L162c;

		L1620:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.EmitLocalWord(0x3c, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x46, 0x14);

		L162c:
			this.processor.EmitLocalWord(0x46, this.processor.IncrementWord(this.processor.PullLocalWord(0x46)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1638;
			goto L15aa;

		L1638:			
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L1647;

			if (this.owner.World.RoundTally != 0) 
				goto L16a5;

		L1647:
			if (this.owner.World.RoundTally == 0) goto L167a;
			if (this.owner.World.HardnessTier != 0) goto L167a;

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, this.processor.PullLocalSignedWord(0x14));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) 
				goto L167a;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L167a;
			goto L1591;

		L167a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L169f;

			EarnTechnology(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				participantIdx1);
			
			this.owner.Bot.RefreshCityProduction(participantIdx, 0xffff);

		L169f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			goto L1d29;

		L16a5:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx != -1) goto L16af;
			goto L19d4;

		L16af:
			this.owner.Encyclopedia.ObtainFirstPrerequisite((ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx);
			
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x42, 0x0);
			
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx == 71) goto L16f1;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdeba), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L16f1;

			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L16f1;
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsEqual) goto L16f1;
			this.processor.EmitLocalWord(0x42, 0x1);

		L16f1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L16fa;
			goto L178c;

		L16fa:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "discovr1.pic");

			this.processor.EmitLocalWord(0x12, 0x0);

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x36);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) 
				goto L1730;

			this.processor.EmitLocalByte(0x9, 0x32);
			this.processor.EmitLocalWord(0x12, 0x1);

		L1730:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.Helpers.HaltHueCycling();

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

		L178c:
			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.Libc.AppendText(0xba06, 0x22cc);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x22d8);
			}

			this.owner.Libc.AppendText(0xba06, "\ndiscover the secret\nof ");

			this.processor.RegAcc.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx != 71) goto L182c;

			this.owner.Libc.AppendText(0xba06, " ");

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally++;

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally, 10));

		L182c:
			this.owner.Libc.AppendText(0xba06, "!\n");

			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals);
			
			// The original plays only a short fanfare (table at 0x1518, one to five seconds),
			// the national anthem (table at 0x151a, as in the palace and the throne room) plays until the next research is chosen
			this.owner.Services.PerformSound(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x151a)), 3);

			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L185f;
			goto L19a5;

		L185f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L186e;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x6);

		L186e:
			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L187f;
			this.processor.RegAcc.Value16 = 25;
			goto L1882;

		L187f:
			this.processor.RegAcc.Value16 = 1;

		L1882:
			this.owner.Dialogs.DisplayMenu(0xba06, 96, (short)this.processor.RegAcc.Value16, 1);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Libc.CopyText(0xba06, "iconpg1.pic");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x9;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalByte(0xba0c, this.processor.SumByte(this.processor.PullGlobalByte(0xba0c), this.processor.RegAcc.LowByte));

			this.owner.Startup.QueryForDisk(3, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xba06, 0);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.Encyclopedia.RenderIcon(
				(ushort)(((this.processor.PullLocalSignedWord(0x44) % 3) * 111) + 1),
				(ushort)((((this.processor.PullLocalSignedWord(0x44) % 9) / 3) * 69) + 1),
				110,
				68,
				174,
				(ushort)(this.processor.PullLocalWord(0x12) != 0 ? 87 : 96));

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1994;

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x8), ".pal");

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0xc1d6);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x2311, 0xc5be);

			this.owner.Libc.CopyBytes(0xc5c4, 0xc1dc, 0xff);

			this.owner.Services.ImportColorsFromBuffer(30, 0xc5be);

		L1994:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Helpers.LaunchHueCycling();

			goto L19b1;

		L19a5:
			this.owner.Paper.DisplayNewspaper(-1);

		L19b1:
			EarnTechnology(participantIdx,
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx,
				participantIdx1);
			
			this.owner.Services.PerformSound(1, 0);

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx = -1;

		L19d4:
			if (this.owner.World.Participants[participantIdx].FoundScienceTally <= 1)
			{
				this.owner.HelpSystem.DisplayHelpPopup(0x231d);
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1a05;
			goto L1b3d;

		L1a05:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1a0e;
			goto L1b3d;

		L1a0e:
			if (this.owner.World.HardnessTier == 0) goto L1a18;
			goto L1b3d;

		L1a18:
			this.owner.Libc.CopyText(0xba06, "Science Advisor:\nI recommend we\ndevelop ");

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x12, 0x0);

			this.owner.Libc.AppendText(0xba06, "So we can build:\n-");

			this.processor.EmitLocalWord(0x18, 0x0);

		L1a77:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x18));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x114a)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1aad;

			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n-");

			this.processor.IncrementLocalWord(0x12);

		L1aad:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L1a77;
			this.processor.EmitLocalWord(0x18, 0x0);

		L1abb:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x18));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb6)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1af1;

			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n-");

			this.processor.IncrementLocalWord(0x12);

		L1af1:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x18);
			if (this.processor.FlagSet.IsLess) goto L1abb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1b08;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

		L1b08:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa8);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.owner.Dialogs.DisplayMenu(0xba06, 8, (short)this.processor.RegAcc.Value16, 1);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

		L1b3d:
			this.owner.Libc.CopyText(0xba06, "Which discovery should our\n");

			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.owner.Libc.AppendText(0xba06, 0x2388);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x2393);
			}

			this.owner.Libc.AppendText(0xba06, " be pursuing, sire?\nPick one...\n ");

			this.processor.EmitLocalWord(0x12, 0x0);
			this.processor.EmitLocalWord(0x46, 0x0);

		L1b8f:
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullLocalSignedWord(0x46));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L1ba3;

			goto L1c46;

		L1ba3:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x46));
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L1bc2;

			goto L1c46;

		L1bc2:
			this.processor.RegAcc.Value16 = PlayerKnowsTechnology(participantIdx, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef)));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L1c46;

			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1bff;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1c12;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[participantIdx].FoundScienceTally;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c12;
			if (this.owner.World.HardnessTier == 0) goto L1c12;

		L1bff:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1c46;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3a)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c46;

		L1c12:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x46));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n ");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x12);
			this.processor.IncrementLocalWord(0x12);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3a), this.processor.RegAcc.Value16);

		L1c46:
			this.processor.EmitLocalWord(0x46, this.processor.IncrementWord(this.processor.PullLocalWord(0x46)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1c52;
			goto L1b8f;

		L1c52:
			this.processor.EmitGlobalWord(0x2f9e, 0x6);
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1cca;
			this.processor.EmitGlobalWord(0x2fa0, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, 80, 64, 1);

			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.CompareWord(this.processor.PullLocalWord(0x46), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1c95;
			this.processor.EmitLocalWord(0x46, 0x0);

		L1c95:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3a));
			this.processor.EmitLocalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1cc2;

			this.owner.Encyclopedia.DisplayCivilopediaRecord(this.processor.PullLocalWord(0x46), 0);
			
			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.EmitLocalWord(0x16, 0x1);
			goto L1638;

		L1cc2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx = (short)this.processor.RegAcc.Value16;
			goto L1d26;

		L1cca:
			this.owner.Libc.AppendText(0xba06, " Future Technology ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ComingScienceTally + 1), 10));

			this.owner.Libc.AppendText(0xba06, "\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx = 71;

		L1d26:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);

		L1d29:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("UncoverTechnology");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The player learns a technology (from participantIdx1: himself or 0 by research, -2 the Great Library, else a player);
		/// 0x47 (future technology) is only counted. After turn 0: the human player and the players he has contact with
		/// announce it (with the wonders and units it allows, for the others), a player without contact may send an emissary
		/// the first time anybody learns it (technology 8, one time in 3); the human player sees the Civilopedia, the new
		/// governments with 0x14, the help with 5; technologies that make barracks obsolete sell them; the first player to learn
		/// a technology cancels the effect of the human player's wonders it makes obsolete; the owner of the Great Library
		/// (wonder 5) learns it when two players know it. SI and DI get the values of the original before the calls.
		/// </summary>
		public void EarnTechnology(short participantIdx, short scienceIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"EarnTechnology({participantIdx}, {scienceIdx}, {participantIdx1})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();

			if (PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
			{
				if (scienceIdx != 0x47)
				{
					this.owner.World.Participants[participantIdx].FoundScienceMarkers[scienceIdx >> 4] |= (ushort)(1 << (scienceIdx & 0xf));
					this.owner.World.Participants[participantIdx].ScienceObtainedSince[scienceIdx] = participantIdx1;
				}

				if (this.owner.World.RoundTally != 0)
				{
					this.owner.World.Participants[participantIdx].FoundScienceTally++;

					AnnounceTechnology(participantIdx, scienceIdx, participantIdx1);

					ApplyTechnology(participantIdx, scienceIdx);
				}
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("EarnTechnology");
		}

		/// <summary>The message about a new technology, or the emissary of a player without contact (see EarnTechnology)</summary>
		private void AnnounceTechnology(short participantIdx, short scienceIdx, short participantIdx1)
		{
			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

				if ((this.owner.World.Participants[person].Relations[participantIdx] & 0x40) == 0)
				{
					SendTechnologyEmissary(participantIdx, scienceIdx);
					return;
				}
			}

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(((ushort)participantIdx << 1) + 0x1992)));

			this.owner.Libc.AppendText(0xba06, participantIdx == participantIdx1 || participantIdx1 == 0 ? (ushort)0x23d7 : (ushort)0x23e1);

			this.owner.Libc.AppendText(0xba06, (ushort)((ushort)(0x16 * scienceIdx) + 0x4da));

			// AX is 0xba06 here (the original tested it)
			if (participantIdx != participantIdx1)
			{
				this.owner.Libc.AppendText(0xba06, " from\nthe ");

				if (participantIdx1 == -2)
				{
					this.owner.Libc.AppendText(0xba06, "Great Library");
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(((ushort)participantIdx1 << 1) + 0x1992)));
				}
			}

			this.owner.Libc.AppendText(0xba06, "!\n ");

			if (participantIdx != person)
			{
				// The wonders (0xbb6, names 0xb9a) and the units (0x114a, names 0x112a) the technology allows
				for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
				{
					this.processor.RegSource.Value16 = (ushort)(0x1e * wonderIdx);

					if (this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0xbb6)) == (ushort)scienceIdx)
					{
						this.owner.Libc.AppendText(0xba06, (ushort)(this.processor.RegSource.Value16 + 0xb9a));

						this.owner.Libc.AppendText(0xba06, "\n ");
					}
				}

				for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
				{
					this.processor.RegSource.Value16 = (ushort)(0x22 * kindIdx);

					if (this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0x114a)) == (ushort)scienceIdx)
					{
						this.owner.Libc.AppendText(0xba06, (ushort)(this.processor.RegSource.Value16 + 0x112a));

						this.owner.Libc.AppendText(0xba06, "\n ");
					}
				}

				this.processor.EmitLocalWord(0x2, 0x1c);
			}

			if ((participantIdx != person || participantIdx1 == -2) && this.processor.PullGlobalWord(0x3934) == 0xffff)
			{
				this.processor.EmitGlobalWord(0xd206, 1);

				this.processor.EmitGlobalWord(0x2f9e, participantIdx == person ? (ushort)6 : (ushort)0);

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		}

		/// <summary>
		/// The first time anybody learns the technology, a player without contact who knows technology 8 sends an emissary to
		/// the human player who doesn't know it, one time in 3; when he is received the king tells about it (0x240e)
		/// </summary>
		private void SendTechnologyEmissary(short participantIdx, short scienceIdx)
		{
			if (((byte)this.owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) != 0 ||
				PlayerKnowsTechnology(participantIdx, 8) == 0 ||
				PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, scienceIdx) != 0 ||
				this.owner.Libc.Dice.DrawInt(3) != 0)
				return;

			this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0);

			if (this.processor.RegAcc.Value16 == 0xffff)
				return;

			this.processor.EmitGlobalWord(0x3934, 0);
			this.processor.EmitGlobalWord(0xd804, 0);
			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), this.processor.PullGlobalWord((ushort)(((ushort)participantIdx << 1) + 0x1992)));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), (ushort)((ushort)(0x16 * scienceIdx) + 0x4da));

			this.owner.BlockText.DisplayTextDialog(0x240e);

			this.processor.EmitGlobalWord(0x3936, 0x2);
			this.processor.EmitGlobalWord(0xd76c, 0xc);

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x200b); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

			this.owner.Audience.FinishAudience();
		}

		/// <summary>What a new technology does after its message (see EarnTechnology)</summary>
		private void ApplyTechnology(short participantIdx, short scienceIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				if (scienceIdx != 0x47)
				{
					this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)scienceIdx, 0);
				}

				if (scienceIdx == 0x14)
				{
					this.owner.NewGameWizard.ImportGovernmentGraphics();

					this.owner.TurnHooks.RepaintMainScreen();
				}

				if (this.processor.PullGlobalWord(0x3934) == 0xffff)
				{
					this.owner.TurnHooks.RepaintMainScreen();

					if (scienceIdx == 0x5)
					{
						this.owner.HelpSystem.DisplayHelpPopup(0x2414);
					}
				}
			}
			else if (this.owner.World.HardnessTier == 0 && scienceIdx == 0x16)
			{
				// The exchange of ambassadors that followed never happens: the test of the original, ((Relations == 0) & 0x40),
				// is always false
				this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);
				this.processor.RegDest.Value16 = (ushort)(((ushort)person << 4) + this.processor.RegSource.Value16);
			}

			if (ScienceRules.DoesTechObsoleteBarracks(scienceIdx))
			{
				for (int townIdx = 0; townIdx < 128; townIdx++)
				{
					if (this.owner.World.Towns[townIdx].ConditionMarker != 0xff &&
						this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					{
						this.owner.World.Towns[townIdx].StructureMarkers0 &= 0xfffd;
					}
				}

				if (participantIdx == person && this.processor.PullGlobalWord(0x3934) == 0xffff)
				{
					this.owner.Libc.CopyText(0xba06, "Development of ");

					this.owner.Libc.AppendText(0xba06, (ushort)((ushort)(0x16 * scienceIdx) + 0x4da));

					this.owner.Libc.AppendText(0xba06, "\nmakes existing Barracks\nobsolete.\n");

					this.processor.EmitGlobalWord(0x2f9e, 0x3);

					this.owner.TurnHooks.DisplayDialog(0xba06, 80, 100);
				}
			}

			if (((byte)this.owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) == 0)
			{
				this.owner.World.ScienceLeadingFoundVia[scienceIdx] = participantIdx;

				this.owner.UnitOps.AttachReplayContent(5, (byte)participantIdx, (byte)scienceIdx);

				// The human player's wonders (0x10fe: the technology that makes each obsolete) lose their effect
				for (ushort wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					this.processor.RegSource.Value16 = (ushort)(wonderIdx << 1);

					if (this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0x10fe)) != (ushort)scienceIdx ||
						this.owner.World.Towns[this.owner.World.MarvelTownIdx[wonderIdx]].ParticipantIdx != person)
						continue;

					this.owner.Libc.CopyText(0xba06, (ushort)((ushort)(0x16 * scienceIdx) + 0x4da));

					this.owner.Libc.AppendText(0xba06, " cancels\nthe effect of\n");

					this.owner.Libc.AppendText(0xba06, (ushort)((ushort)(0x1e * wonderIdx) + 0xe6a));

					this.owner.Libc.AppendText(0xba06, ".\n");

					this.owner.Paper.DisplayNewspaper(-1);
				}

				this.processor.EmitLocalWord(0x6, 0x16);
			}

			// The Great Library (wonder 5) gives a technology that two players know
			ushort libraryOwner = (ushort)this.owner.World.MarvelTownIdx[5];
			this.processor.EmitLocalWord(0x8, libraryOwner);

			if (libraryOwner == 0xffff)
				return;

			libraryOwner = (ushort)this.owner.World.Towns[libraryOwner].ParticipantIdx;
			this.processor.EmitLocalWord(0x8, libraryOwner);

			if (PlayerKnowsTechnology((short)libraryOwner, scienceIdx) != 0)
				return;

			this.owner.CityOps.PlayerEnjoysWonderEffect((short)libraryOwner, 5);

			if (this.processor.RegAcc.Value16 == 0)
				return;

			int knowing = 0;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (PlayerKnowsTechnology(otherIdx, scienceIdx) != 0)
					knowing++;
			}

			if (knowing < 2)
				return;

			EarnTechnology((short)libraryOwner, scienceIdx, -2);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="technologyID"></param>
		/// <returns></returns>
		public ushort PlayerKnowsTechnology(short participantIdx, short scienceIdx)
		{
			return (ushort)(ScienceRules.PlayerKnowsTechnology(
				this.owner.World.Participants[participantIdx].FoundScienceMarkers,
				participantIdx,
				scienceIdx) ? 1 : 0);
		}

		/// <summary>
		/// Calculates the AI leader preference value for technology research.
		/// </summary>
		/// <param name="participantIdx"></param>
		/// <param name="arg2"></param>
		public ushort ObtainTechnologyInfo(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"ObtainTechnologyInfo({participantIdx}, {arg2})");

			sbyte b0 = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(arg2 * 2 + 0xb0a));
			sbyte b1 = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(arg2 * 2 + 0xb0b));
			int leaderVisuals = this.owner.World.Participants[participantIdx].ChiefVisuals;
			short leaderFactor = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(leaderVisuals * 0x3a + 0x1516));
			ushort result = ScienceRules.CalculateTechnologyInfo(b0, b1, leaderFactor);
			this.processor.RegAcc.Value16 = result;

			this.processor.Journal.CloseScope("ObtainTechnologyInfo");
			return result;
		}
	}
}
