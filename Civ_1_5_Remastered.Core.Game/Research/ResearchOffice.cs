namespace Civ_1_5_Remastered.Core.Game.Research
{
	public partial class ResearchOffice : GameComponent, IResearchOffice
	{
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

		private FrameWords pickFrame = null!;

		public ResearchOffice(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		private ushort PageState { get => FrameWord(PageStateLocal); set => FrameWord(PageStateLocal, value); }

		private ushort AdvisorsShown { get => FrameWord(AdvisorsShownLocal); set => FrameWord(AdvisorsShownLocal, value); }

		private short ChoiceCount { get => (short)FrameWord(ChoiceCountLocal); set => FrameWord(ChoiceCountLocal, (ushort)value); }

		private short Score { get => (short)FrameWord(ScoreLocal); set => FrameWord(ScoreLocal, (ushort)value); }

		public void HandleAllCities(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"HandleAllCities({participantIndex})");

			Turn.DemocracyPressure = 0;
			Turn.AnarchyPressure = 0;

			int unrest = 0;

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff || this.Owner.World.Towns[townIndex].ParticipantIndex != participantIndex)
				{
					continue;
				}

				while (true)
				{
					if (this.Owner.CityOperations.HandleCity(townIndex, 0) == 0 && this.Owner.World.Towns[townIndex].TrueExtent > 5)
					{
						unrest |= 1;
					}

					if ((this.Owner.World.Towns[townIndex].ConditionMarker & 1) != 0)
					{
						unrest |= 2;
					}

					if (CityTurn.Again == 0)
					{
						break;
					}

					this.Owner.CityOperations.HandleCity(townIndex, 1);

					this.Owner.TurnHooks.RepaintMainScreen();

					bool again = CityTurn.Again == 2;

					CityTurn.Again = 0;

					if (!again)
					{
						break;
					}
				}
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex || (this.Owner.World.DiagonalMarkers & 4) == 0)
			{
				return;
			}

			Player participant = this.Owner.World.Participants[participantIndex];
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

			if (PlayerKnowsTechnology(participantIndex, 0x3f) != 0)
			{
				participant.ResearchLevyRatio = 0;
			}

			participant.LevyRatio = (short)(10 - participant.ResearchLevyRatio - luxuries);
		}

		public void FinalizeProduction(short townIndex, ushort horizontalLocation, ushort verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"FinalizeProduction({townIndex}, {horizontalLocation}, {verticalLocation})");

			City town = this.Owner.World.Towns[townIndex];

			this.Owner.UnitOperations.AttachReplayContent(1, 0xff, town.LabelIndex, (byte)horizontalLocation, (byte)verticalLocation);

			this.Owner.World.Participants[town.ParticipantIndex].StopMaking(town.PresentMakingIndex);

			this.Owner.World.Participants[town.ParticipantIndex].TownTally--;
			town.ConditionMarker = 0xff;

			this.Owner.MapOperations.AssignTileImprovements(0, horizontalLocation, verticalLocation);

			if ((this.Owner.World.ChartSight[horizontalLocation, verticalLocation] & (1 << this.Owner.World.PersonParticipantIndex)) != 0)
			{
				this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);
			}

			for (int otherIndex = 0; otherIndex < 0x80; otherIndex++)
			{
				for (int routeIndex = 0; routeIndex < 3; routeIndex++)
				{
					if (this.Owner.World.Towns[otherIndex].CommerceTownIndices[routeIndex] == townIndex)
					{
						this.Owner.World.Towns[otherIndex].CommerceTownIndices[routeIndex] = -1;
					}
				}
			}

			for (int wonderIndex = 1; wonderIndex <= 0x15; wonderIndex++)
			{
				if (this.Owner.World.MarvelTownIndex[wonderIndex] == townIndex)
				{
					this.Owner.World.MarvelTownIndex[wonderIndex] = 128;
				}
			}

			UnitMemory troops = new UnitMemory(this.Processor);

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				short ownerIndex = town.ParticipantIndex;

				if (troops.Exists(ownerIndex, troopIndex) && troops.PullSigned(ownerIndex, troopIndex, UnitMemory.BaseTown) == townIndex)
				{
					this.Owner.UnitOperations.DestroyUnit(ownerIndex, troopIndex);
				}
			}

			ushort stack = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

			if (stack != 0xffff)
			{
				this.Owner.UnitOperations.VisitEachUnitInStack(town.ParticipantIndex, (short)stack, 0x1643);
			}

			for (int spotIndex = 0; spotIndex < 0x2d; spotIndex++)
			{
				int spotHorizontal = this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(spotIndex) + horizontalLocation);
				short spotVertical = (short)(Tables.DirectionVertical(spotIndex) + verticalLocation);

				this.Owner.Helpers.FindNearestCity(spotHorizontal, spotVertical);

				if (spotIndex <= 0x14 || (short)Search.FoundDistance > 3)
				{
					ushort holder = this.Owner.VideoCard.ObtainDot(2, spotHorizontal + 0x50, spotVertical + 0x32);
					this.Owner.Services.AssignDot(2, spotHorizontal + 0x50, spotVertical, holder);
				}
			}
		}

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

		public void RenewCityScreen(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenewCityScreen({townIndex})");

			DrawAdvisorScenery();

			MapView.RedrawRequest = 0x0;

			this.Owner.CityOperations.HandleCity(townIndex, 1);

			if (MapView.RedrawRequest != 0)
			{
				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				RecoverAdvisorScenery();
			}
		}

		private static int TableWord(int table, int index) => table - (index << 1);

		private static ushort ImprovementRecord(int improvementIndex) => (ushort)(0xb9a + (improvementIndex * 0x1e));

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
