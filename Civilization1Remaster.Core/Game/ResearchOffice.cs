
namespace Civilization1Remaster.Core.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1ade.
	/// </summary>
	public partial class ResearchOffice : GameComponent, IResearchOffice
	{
		// The frame of PickCityProduction (distances below bp)
		private const int HappyRoomLocal = 0x2;

		private const int AdvisorsShownLocal = 0x4;
		private const int SharedWallLocal = 0x6;
		private const int DefenceNeedLocal = 0x8;
		private const int ChoiceCountLocal = 0xa;
		private const int BestUnitLocal = 0xc;
		private const int UnitsOnContinentTable = 0x44;
		private const int ChoiceLocal = 0x46;
		private const int BestImprovementLocal = 0x48;
		private const int TownLoopLocal = 0x4a;
		private const int BestScoreLocal = 0x4c;
		private const int ChoiceTable = 0xde;
		private const int ChoiceLoopLocal = 0xe0;
		private const int MakingElsewhereTable = 0x11a;
		private const int UnfamiliarLoopLocal = 0x11c;
		private const int BestUnitScoreLocal = 0x11e;
		private const int BestImprovementScoreLocal = 0x130;
		private const int DefensiveImprovementLocal = 0x132;
		private const int ScoreLocal = 0x134;
		private const int PageCutCountLocal = 0x136;
		private const int PageCutLengthLocal = 0x138;
		private const int PageStateLocal = 0x13a;
		private const int DefensiveLocal = 0x13c;
		private const int ContinentLocal = 0x13e;
		private const int UnitTallyLocal = 0x140;
		private const int ApproachLocal = 0x142;
		private const int BestChoiceLocal = 0x144;
		private const int WallContinentLocal = 0x146;
		private const int BuiltHighLocal = 0x148;
		private const int BuiltLowLocal = 0x14a;

		private readonly UnitMemory troops;

		/// <summary>The words of the frame of PickCityProduction (B3: in C#, new for every call)</summary>
		private FrameWords pickFrame = null!;

		public ResearchOffice(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>0: one page, 1: the menu is cut after the units ("more..." leads to the rest), 2: the rest only</summary>
		private ushort PageState { get => FrameWord(PageStateLocal); set => FrameWord(PageStateLocal, value); }

		private ushort AdvisorsShown { get => FrameWord(AdvisorsShownLocal); set => FrameWord(AdvisorsShownLocal, value); }

		/// <summary>The next free entry of the list of choices (entry 0 is the production of the city)</summary>
		private short ChoiceCount { get => (short)FrameWord(ChoiceCountLocal); set => FrameWord(ChoiceCountLocal, (ushort)value); }

		private short Score { get => (short)FrameWord(ScoreLocal); set => FrameWord(ScoreLocal, (ushort)value); }

		/// <inheritdoc/>
		public void HandleAllCities(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"HandleAllCities({participantIdx})");

			Turn.DemocracyPressure = 0;
			Turn.AnarchyPressure = 0;

			// 1: a city of size above 5 whose turn returned 0, 2: a city in disorder
			int unrest = 0;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff || this.Owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				while (true)
				{
					if (this.Owner.CityOps.HandleCity(townIdx, 0) == 0 && this.Owner.World.Towns[townIdx].TrueExtent > 5)
					{
						unrest |= 1;
					}

					if ((this.Owner.World.Towns[townIdx].ConditionMarker & 1) != 0)
					{
						unrest |= 2;
					}

					if (CityTurn.Again == 0)
						break;

					this.Owner.CityOps.HandleCity(townIdx, 1);

					this.Owner.TurnHooks.RepaintMainScreen();

					bool again = CityTurn.Again == 2;

					CityTurn.Again = 0;

					if (!again)
						break;
				}
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx || (this.Owner.World.DiagMarkers & 4) == 0)
			{
				return;
			}

			Player participant = this.Owner.World.Participants[participantIdx];
			short rates = (short)(participant.ResearchLevyRatio + participant.LevyRatio);
			short luxuries = (short)(10 - rates);

			if ((unrest & 2) != 0 && rates > 6)
			{
				luxuries++;
			}

			if ((this.Owner.World.RoundTally & 3) == 0 && unrest == 0 && (short)(participant.LevyRatio + participant.ResearchLevyRatio) < 8)
			{
				luxuries--;
			}

			luxuries = (short)this.Owner.Helpers.ClampValue(luxuries, 0, 4);

			participant.ResearchLevyRatio = (short)(Tables.LeaderWord(participant.ChiefVisuals, LeaderTable.Leaning) +
				((10 - luxuries) / 2));

			if (participant.Gold > (short)(this.Owner.World.RoundTally + 0x64))
			{
				participant.ResearchLevyRatio++;
			}

			if (PlayerKnowsTechnology(participantIdx, 0x3f) != 0)
			{
				participant.ResearchLevyRatio = 0;
			}

			participant.LevyRatio = (short)(10 - participant.ResearchLevyRatio - luxuries);
		}

		/// <inheritdoc/>
		public void FinalizeProduction(short townIdx, ushort horizLoc, ushort vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"FinalizeProduction({townIdx}, {horizLoc}, {vertLoc})");

			City town = this.Owner.World.Towns[townIdx];

			this.Owner.UnitOps.AttachReplayContent(1, 0xff, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			this.Owner.World.Participants[town.ParticipantIdx].StopMaking(town.PresentMakingIdx);

			this.Owner.World.Participants[town.ParticipantIdx].TownTally--;
			town.ConditionMarker = 0xff;

			this.Owner.MapOps.AssignTileImprovements(0, horizLoc, vertLoc);

			if ((this.Owner.World.ChartSight[horizLoc, vertLoc] & (1 << this.Owner.World.PersonParticipantIdx)) != 0)
			{
				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			}

			for (int otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				for (int routeIdx = 0; routeIdx < 3; routeIdx++)
				{
					if (this.Owner.World.Towns[otherIdx].CommerceTownIndices[routeIdx] == townIdx)
					{
						this.Owner.World.Towns[otherIdx].CommerceTownIndices[routeIdx] = -1;
					}
				}
			}

			for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
			{
				if (this.Owner.World.MarvelTownIdx[wonderIdx] == townIdx)
				{
					this.Owner.World.MarvelTownIdx[wonderIdx] = 128;
				}
			}

			UnitMemory troops = new UnitMemory(this.Processor);

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				short ownerIdx = town.ParticipantIdx;

				if (troops.Exists(ownerIdx, troopIdx) && troops.PullSigned(ownerIdx, troopIdx, UnitMemory.BaseTown) == townIdx)
				{
					this.Owner.UnitOps.DestroyUnit(ownerIdx, troopIdx);
				}
			}

			ushort stack = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			if (stack != 0xffff)
			{
				this.Owner.UnitOps.VisitEachUnitInStack(town.ParticipantIdx, (short)stack, 0x1643);
			}

			for (int spotIdx = 0; spotIdx < 0x2d; spotIdx++)
			{
				int spotHoriz = this.Owner.Routing.TunePosition((short)Tables.DirectionHoriz(spotIdx) + horizLoc);
				short spotVert = (short)(Tables.DirectionVert(spotIdx) + vertLoc);

				this.Owner.Helpers.FindNearestCity(spotHoriz, spotVert);

				if (spotIdx <= 0x14 || (short)Search.FoundDistance > 3)
				{
					ushort holder = this.Owner.VideoCard.ObtainDot(2, spotHoriz + 0x50, spotVert + 0x32);
					this.Owner.Services.AssignDot(2, spotHoriz + 0x50, spotVert, holder);
				}
			}
		}

		/// <inheritdoc/>
		public void DrawAdvisorScenery()
		{
			using var _ = this.Processor.Journal.Scope("RenderAdvisorScenery()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 0, 0x140, 0xc8,
				Screen.BackScreenBox,
				0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public void RecoverAdvisorScenery()
		{
			using var _ = this.Processor.Journal.Scope("RecoverAdvisorScenery()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public void RenewCityScreen(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RenewCityScreen({townIdx})");

			DrawAdvisorScenery();

			MapView.RedrawRequest = 0x0;

			this.Owner.CityOps.HandleCity(townIdx, 1);

			if (MapView.RedrawRequest != 0)
			{
				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				RecoverAdvisorScenery();
			}
		}

		/// <summary>The distance of a word of the frame table at bp - table + 2 * index (the unit tables of 28 words)</summary>
		private static int TableWord(int table, int index) => table - (index << 1);

		/// <summary>The record of an improvement or wonder (0x1e bytes from 0xb9a, the name first)</summary>
		private static ushort ImprovementRecord(int improvementIdx) => (ushort)(0xb9a + improvementIdx * 0x1e);

		private ushort FrameWord(int distance)
		{
			return this.pickFrame.Frame(distance);
		}

		private void FrameWord(int distance, ushort amount)
		{
			this.pickFrame.SetFrame(distance, amount);
		}
	}
}
