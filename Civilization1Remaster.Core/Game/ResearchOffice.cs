
namespace Civilization1Remaster.Core.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1ade.
	/// </summary>
	public class ResearchOffice : GameComponent, IResearchOffice
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

			participant.ResearchLevyRatio = (short)(LeaderWord(participant.ChiefVisuals, LeaderTable.Leaning) +
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
				int spotHoriz = this.Owner.Routing.TunePosition((short)DirectionHoriz(spotIdx) + horizLoc);
				short spotVert = (short)(DirectionVert(spotIdx) + vertLoc);

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

		/// <inheritdoc/>
		public ushort PickCityProduction(short participantIdx, short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"PickCityProduction({participantIdx}, {townIdx})");

			// The words of the frame are in C# (B3, properties below; some are read before they are written: 0)
			FrameWords outerFrame = this.pickFrame;
			this.pickFrame = new FrameWords(this.Processor, 0x14a);
			PageState = 0;
			AdvisorsShown = 0;

			this.Owner.CityOps.HandleCity(townIdx, -1);

			ushort choice;

			while (true)
			{
				if (ListProductionChoices(participantIdx, townIdx))
					continue;

				ushort? early = AssessCityNeeds(participantIdx, townIdx);
				if (early != null)
				{
					choice = early.Value;
					break;
				}

				AssessCityDefence(participantIdx, townIdx);
				ScoreProductionChoices(participantIdx, townIdx);

				ushort? decided = DecideProduction(participantIdx, townIdx);
				if (decided != null)
				{
					choice = decided.Value;
					break;
				}
			}

			this.pickFrame = outerFrame;

			return choice;
		}

		/// <inheritdoc/>
		public void ComposeProductionOption(short townIdx, ushort price)
		{
			using var _ = this.Processor.Journal.Scope($"ComposeProductionOption({townIdx}, {price})");

			int perTurn = this.Owner.Helpers.ClampValue(
				(short)CityTurn.Shields - (short)CityTurn.UnitUpkeep, 1, 99);

			ushort turns = (ushort)this.Owner.Helpers.ClampValue(
				(((10 * price) - this.Owner.World.Towns[townIdx].MaterialsTally - 1) / (short)perTurn) + 1, 1, 999);

			this.Owner.Words.Append(" (");

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)turns, 10));

			this.Owner.Words.Append((ushort)(turns != 1 ? 0x22b2 : 0x22b9));
		}

		/// <inheritdoc/>
		public ushort UncoverTechnology(short participantIdx, short participantIdx1)
		{
			using var _ = this.Processor.Journal.Scope($"UncoverTechnology({participantIdx}, {participantIdx1})");


			ushort result = participantIdx == 0 ? (ushort)0xffff : UncoverTechnologyOf(participantIdx, participantIdx1);


			return result;
		}

		/// <inheritdoc/>
		public void EarnTechnology(short participantIdx, short scienceIdx, short participantIdx1)
		{
			using var _ = this.Processor.Journal.Scope($"EarnTechnology({participantIdx}, {scienceIdx}, {participantIdx1})");


			if (PlayerKnowsTechnology(participantIdx, scienceIdx) == 0)
			{
				if (scienceIdx != 0x47)
				{
					this.Owner.World.Participants[participantIdx].FoundScienceMarkers[scienceIdx >> 4] |= (ushort)(1 << (scienceIdx & 0xf));
					this.Owner.World.Participants[participantIdx].ScienceObtainedSince[scienceIdx] = participantIdx1;
				}

				if (this.Owner.World.RoundTally != 0)
				{
					this.Owner.World.Participants[participantIdx].FoundScienceTally++;

					AnnounceTechnology(participantIdx, scienceIdx, participantIdx1);

					ApplyTechnology(participantIdx, scienceIdx);
				}
			}
		}

		/// <inheritdoc/>
		public ushort PlayerKnowsTechnology(short participantIdx, short scienceIdx)
		{
			return (ushort)(ScienceRules.PlayerKnowsTechnology(
				this.Owner.World.Participants[participantIdx].FoundScienceMarkers,
				participantIdx,
				scienceIdx) ? 1 : 0);
		}

		/// <inheritdoc/>
		public ushort ObtainTechnologyInfo(short participantIdx, ushort scienceIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTechnologyInfo({participantIdx}, {scienceIdx})");

			sbyte b0 = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(scienceIdx * 2 + 0xb0a));
			sbyte b1 = (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(scienceIdx * 2 + 0xb0b));
			int leaderVisuals = this.Owner.World.Participants[participantIdx].ChiefVisuals;
			short leaderFactor = (short)LeaderWord(leaderVisuals, LeaderTable.Leaning);
			ushort result = ScienceRules.CalculateTechnologyInfo(b0, b1, leaderFactor);

			return result;
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

		private ushort ChoiceEntry(int entryIdx) => FrameWord(TableWord(ChoiceTable, entryIdx));

		private void ChoiceEntry(int entryIdx, ushort choice) => FrameWord(TableWord(ChoiceTable, entryIdx), choice);

		/// <summary>Appends a choice to the list (and to the menu text, done by the caller)</summary>
		private void AddChoice(ushort choice)
		{
			short entryIdx = ChoiceCount;
			ChoiceCount = (short)(entryIdx + 1);
			ChoiceEntry(entryIdx, choice);
		}

		/// <summary>
		/// The menu text and the list of choices. True when the list is too long for one page the first time: it starts again,
		/// cut after the units
		/// </summary>
		private bool ListProductionChoices(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			ChoiceCount = 1;
			FrameWord(ChoiceTable, (ushort)(short)town.PresentMakingIdx);

			Message("What shall we build in ")
				.City(townIdx)
				.Text("?\n ");

			if (PageState != 2)
				ListUnitChoices(participantIdx, townIdx);

			FrameWord(PageCutCountLocal, (ushort)ChoiceCount);
			FrameWord(PageCutLengthLocal, this.Owner.Words.Length);

			// The Great Wall on the continent of the city (no walls and the like then)
			FrameWord(SharedWallLocal, 0);

			if (this.Owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 15) != 0)
			{
				City wallTown = this.Owner.World.Towns[this.Owner.World.MarvelTownIdx[15]];
				FrameWord(WallContinentLocal, this.Owner.MapOps.ObtainContinentID(wallTown.Place.X, wallTown.Place.Y));

				if (this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) == FrameWord(WallContinentLocal))
					FrameWord(SharedWallLocal, 1);
			}

			if ((this.Owner.World.DiagMarkers & 4) != 0)
				ListImprovementChoices(participantIdx, townIdx);

			ListWonderChoices(participantIdx, townIdx);

			if (participantIdx == this.Owner.World.PersonParticipantIdx && (town.ConditionMarker & 0x10) == 0 && ChoiceCount > 0x1e)
			{
				if (PageState == 0)
				{
					PageState = 1;
					return true;
				}

				this.Owner.Libc.CopyText((ushort)(FrameWord(PageCutLengthLocal) + TextBuffer.Address), "more...\n");
				ChoiceEntry(FrameWord(PageCutCountLocal), 0x63);
			}

			if (PageState == 2)
			{
				this.Owner.Words.Append("more...\n");
				AddChoice(0x62);
			}

			return false;
		}

		/// <summary>
		/// The units the player knows and that aren't obsolete (ships only in a coastal city; nuclear weapons with the
		/// Manhattan Project and the technology 0x1b), with their turns, attack, defence and moves
		/// </summary>
		private void ListUnitChoices(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (FrameWord(ChoiceLoopLocal, 0); (short)FrameWord(ChoiceLoopLocal) < 0x1c; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				sbyte kind = (sbyte)FrameWord(ChoiceLoopLocal);

				if (kind == 0x19 && (this.Owner.World.MarvelTownIdx[17] == -1 || PlayerKnowsTechnology(participantIdx, 0x1b) == 0))
					continue;

				if (PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0x20)) == 0 ||
					PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0xc)) != 0)
				{
					continue;
				}

				if ((town.ConditionMarker & 2) == 0 && UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
					continue;

				this.Owner.Words.Append((ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride));
				ComposeProductionOption(townIdx, UnitKindWord(kind, UnitKindTable.Price));
				this.Owner.Words.Append(", ADM:");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.AssaultPower), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.GuardPower), 10));
				this.Owner.Words.Append("/");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.ShiftTally), 10));
				this.Owner.Words.Append(")\n ");

				AddChoice((ushort)kind);
			}
		}

		/// <summary>
		/// The improvements 1 - 24 the player knows and the city hasn't built, without the ones that don't fit: a second
		/// building of bit 0 (7), 15 with bit 15, 10 / 12 when replaced by a known technology, 20 only in a city with bit 3 of
		/// its state, 19 with bit 14, 19 - 21 not with the Great Wall on the continent or with bits 2 - 4 of the second word;
		/// the spaceship parts (22 - 24) after the Apollo Program, before the launch
		/// </summary>
		private void ListImprovementChoices(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (FrameWord(ChoiceLoopLocal, 1); (short)FrameWord(ChoiceLoopLocal) <= 0x18; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				short improvementIdx = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIdx, this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x1c))) == 0)
					continue;

				FrameWord(BuiltLowLocal, town.StructureMarkers0);
				FrameWord(BuiltHighLocal, town.StructureMarkers1);
				uint built = (uint)(FrameWord(BuiltHighLocal) << 16 | FrameWord(BuiltLowLocal));

				if ((built & (1u << (byte)(improvementIdx - 1))) != 0)
					continue;

				if (improvementIdx == 7 && (this.pickFrame.FrameByte(BuiltLowLocal) & 1) != 0)
					continue;

				if (improvementIdx == 0xf && town.Has(CityStructure.ManufacturingPlant))
					continue;

				if (improvementIdx == 0xa && !town.Has(CityStructure.Marketplace) &&
					PlayerKnowsTechnology(participantIdx, (short)BuildingWord(5, BuildingTable.Technology)) != 0)
				{
					continue;
				}

				if (improvementIdx == 0xc && !town.Has(CityStructure.Library) &&
					PlayerKnowsTechnology(participantIdx, (short)BuildingWord(6, BuildingTable.Technology)) != 0)
				{
					continue;
				}

				if (improvementIdx == 0x14 && (town.ConditionMarker & 8) == 0)
					continue;

				if (improvementIdx == 0x13 && !town.Has(CityStructure.Factory))
					continue;

				if (improvementIdx is 0x13 or 0x14 or 0x15 && (FrameWord(SharedWallLocal) != 0 || (town.StructureMarkers1 & 0x1c) != 0))
					continue;

				if (improvementIdx > 0x15 &&
					(this.Owner.World.MarvelTownIdx[19] == -1 || (this.Owner.World.StarshipMarkers & (1 << participantIdx)) != 0))
				{
					continue;
				}

				this.Owner.Words.Append(ImprovementRecord(improvementIdx));
				ComposeProductionOption(townIdx, this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x18)));
				this.Owner.Words.Append(")\n ");

				AddChoice((ushort)-improvementIdx);
			}
		}

		/// <summary>The wonders (25 - 45) the player knows that nobody has built; "*" before one that is obsolete for someone</summary>
		private void ListWonderChoices(short participantIdx, short townIdx)
		{
			FrameWord(ChoiceLoopLocal, 0x19);

			do
			{
				short improvementIdx = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIdx, this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x1c))) != 0 &&
					this.Owner.World.MarvelTownIdx[improvementIdx - 0x18] == -1)
				{
					ushort obsolete = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((improvementIdx << 1) + 0x10ce));

					if (obsolete != 0x7f && ((byte)this.Owner.World.ScienceLeadingFoundVia[this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((improvementIdx << 1) + 0x10ce))] & 7) != 0)
						this.Owner.Words.Append("*");

					this.Owner.Words.Append(ImprovementRecord(improvementIdx));
					ComposeProductionOption(townIdx, this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x18)));
					this.Owner.Words.Append(")\n ");

					AddChoice((ushort)-improvementIdx);
				}

				FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) <= 0x2d);
		}

		/// <summary>
		/// The situation of the city: its continent and the player's approach there (defensive: 1, 2, or 5 with 0x8078), the
		/// room for unhappiness, the units being built in the player's other cities there (+1) and the units of each kind
		/// on the continent (also the ones known in cities). A computer player without units under despotism or anarchy builds
		/// militia (1); the barbarians build legions (3) or, with bit 0 - 2 of 0x3772:0x520, knights (7). Null: go on.
		/// The player's units are counted again (TroopTally); it is put back later, but not after these early answers.
		/// </summary>
		private ushort? AssessCityNeeds(short participantIdx, short townIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			City town = this.Owner.World.Towns[townIdx];

			FrameWord(ContinentLocal, this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y));
			FrameWord(ApproachLocal, (ushort)player.Landmasses[FrameWord(ContinentLocal)].Approach);
			FrameWord(DefensiveLocal, (ushort)((short)FrameWord(ApproachLocal) is 1 or 2 ? 1 : 0));

			if (Turn.LateGameNudge != 0 && (short)FrameWord(ApproachLocal) == 5)
				FrameWord(DefensiveLocal, 1);

			short unhappy = (short)((player.RegimeKind <= 1 ? 1 : 2) * (short)CityTurn.Settlers);
			FrameWord(HappyRoomLocal, (ushort)((short)CityTurn.Food - unhappy - (town.TrueExtent << 1)));

			for (FrameWord(ChoiceLoopLocal, 0); (short)FrameWord(ChoiceLoopLocal) < 0x1c; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				int kind = (short)FrameWord(ChoiceLoopLocal);
				FrameWord(TableWord(MakingElsewhereTable, kind), 1);
				FrameWord(TableWord(UnitsOnContinentTable, kind), 0);
			}

			FrameWord(UnitTallyLocal, (ushort)player.TroopTally);
			player.TroopTally = 0;

			FrameWord(TownLoopLocal, 0);

			do
			{
				City other = this.Owner.World.Towns[FrameWord(TownLoopLocal)];

				if (other.ConditionMarker != 0xff && other.ParticipantIdx == participantIdx &&
					this.Owner.MapOps.ObtainContinentID(other.Place.X, other.Place.Y) == FrameWord(ContinentLocal))
				{
					if (other.PresentMakingIdx >= 0 && (short)FrameWord(TownLoopLocal) != townIdx)
					{
						int making = TableWord(MakingElsewhereTable, other.PresentMakingIdx);
						FrameWord(making, (ushort)(FrameWord(making) + 1));
					}

					for (FrameWord(UnfamiliarLoopLocal, 0); (short)FrameWord(UnfamiliarLoopLocal) < 2; FrameWord(UnfamiliarLoopLocal, (ushort)(FrameWord(UnfamiliarLoopLocal) + 1)))
					{
						byte unfamiliar = (byte)other.Unfamiliar[FrameWord(UnfamiliarLoopLocal)];
						this.pickFrame.SetFrameByte(BuiltLowLocal, unfamiliar);

						if (unfamiliar != 0xff)
						{
							player.TroopTally++;
							int units = TableWord(UnitsOnContinentTable, (sbyte)unfamiliar & 0x3f);
							FrameWord(units, (ushort)(FrameWord(units) + 1));
						}
					}
				}

				FrameWord(TownLoopLocal, (ushort)(FrameWord(TownLoopLocal) + 1));
			}
			while ((short)FrameWord(TownLoopLocal) < 0x80);

			FrameWord(ChoiceLoopLocal, 0);

			do
			{
				short troopIdx = (short)FrameWord(ChoiceLoopLocal);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff)
				{
					player.TroopTally++;

					if (this.Owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == FrameWord(ContinentLocal))
					{
						int units = TableWord(UnitsOnContinentTable, this.troops.TroopKind(participantIdx, troopIdx));
						FrameWord(units, (ushort)(FrameWord(units) + 1));
					}
				}

				FrameWord(ChoiceLoopLocal, (ushort)(troopIdx + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) < 0x80);

			if (player.TroopTally == 0 && participantIdx != this.Owner.World.PersonParticipantIdx && player.RegimeKind <= 1)
				return 1;

			if (participantIdx == 0)
				return (ushort)((this.Processor.PullUInt8(0x3772, 0x520) & 7) != 0 ? 7 : 3);

			return null;
		}

		/// <summary>
		/// The defenders the city needs (0x8): 2 without units in it, 1 when it has fewer defenders than a quarter of its
		/// size plus the defensive approach
		/// </summary>
		private void AssessCityDefence(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			FrameWord(DefenceNeedLocal, 0);
			FrameWord(ChoiceLoopLocal, this.Owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y));

			if (FrameWord(ChoiceLoopLocal) == 0xffff)
			{
				FrameWord(DefenceNeedLocal, 2);
				return;
			}

			short defenders = (short)this.Owner.UnitOps.CountUnitsWithFlagInStack(participantIdx, (short)FrameWord(ChoiceLoopLocal), 2);

			if ((short)(town.TrueExtent / 4 + FrameWord(DefensiveLocal)) > defenders)
				FrameWord(DefenceNeedLocal, 1);
		}

		/// <summary>
		/// The score of every choice (lower is better), and the best one overall (0x144), the best unit (0xc) and the best
		/// improvement or wonder (0x48)
		/// </summary>
		private void ScoreProductionChoices(short participantIdx, short townIdx)
		{
			FrameWord(BestUnitScoreLocal, 0x3e7);
			FrameWord(BestImprovementScoreLocal, 0x3e7);
			FrameWord(BestScoreLocal, 0x3e7);
			FrameWord(BestUnitLocal, 0xffff);
			FrameWord(BestImprovementLocal, 0xffff);

			for (FrameWord(ChoiceLoopLocal, 1); (short)FrameWord(ChoiceLoopLocal) < ChoiceCount; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				FrameWord(ChoiceLocal, ChoiceEntry((short)FrameWord(ChoiceLoopLocal)));
				short choice = (short)FrameWord(ChoiceLocal);

				if (choice >= 0 && choice < 0x1a)
				{
					// Carriers are never chosen
					if (choice == 0x17 || !ScoreUnitChoice(participantIdx, townIdx, choice))
						continue;
				}
				else
				{
					ScoreImprovementChoice(participantIdx, townIdx, choice);
				}

				if (Score < (short)FrameWord(BestScoreLocal))
				{
					FrameWord(BestChoiceLocal, FrameWord(ChoiceLoopLocal));
					FrameWord(BestScoreLocal, FrameWord(ScoreLocal));
				}

				if (choice < 0 || choice >= 0x1a)
				{
					if (Score < (short)FrameWord(BestImprovementScoreLocal))
					{
						FrameWord(BestImprovementScoreLocal, (ushort)Score);
						FrameWord(BestImprovementLocal, FrameWord(ChoiceLoopLocal));
					}
				}
				else if (Score < (short)FrameWord(BestUnitScoreLocal))
				{
					FrameWord(BestUnitScoreLocal, (ushort)Score);
					FrameWord(BestUnitLocal, FrameWord(ChoiceLoopLocal));
				}
			}
		}

		/// <summary>
		/// The score of an improvement, a wonder, a diplomat (26) or a caravan (27): its need in the city (most are 999, not
		/// needed), weighed by the defensive approach and the leader's character, half more with bit 1 of the buildings
		/// </summary>
		private void ScoreImprovementChoice(short participantIdx, short townIdx, short choice)
		{
			City town = this.Owner.World.Towns[townIdx];
			short food = (short)CityTurn.Shields;
			short trade = (short)CityTurn.Trade;

			Score = 0x3e7;
			FrameWord(DefensiveImprovementLocal, 0);

			switch (27 - choice)
			{
				case 0:
					// A caravan: with a free trade route, more wanted with less trade, at least 3; for the human player only
					// when the city builds by itself
					if ((byte)town.CommerceTownIndices[2] == 0xff)
					{
						Score = (short)-(CPU.SignedQuotient(trade, 2) - 10);

						if ((byte)town.CommerceTownIndices[0] != 0xff)
							Score += 2;

						if ((byte)town.CommerceTownIndices[1] != 0xff)
							Score += 2;

						if (Score < 3)
							Score = 3;
					}

					if (participantIdx == this.Owner.World.PersonParticipantIdx && (town.ConditionMarker & 0x10) != 0)
						Score = 0x3e7;

					break;

				case 1:
					// A diplomat: against the human player (no peace, no caravan in production, a player of 0x2a6e), when the
					// human player knows a technology of a random word of the technologies the player doesn't
					ScoreDiplomatChoice(participantIdx);
					break;

				case 29:
					Score = 5;
					FrameWord(DefensiveImprovementLocal, 1);
					break;

				case 30:
					Score = (short)(food < 3 ? 8 : 4);
					break;

				case 31:
					if ((town.ConditionMarker & 1) != 0)
						Score = -4;
					break;

				case 32:
					Score = (short)-(CPU.SignedQuotient(trade, 2) - 10);

					if ((town.ConditionMarker & 1) != 0)
						Score = -3;
					break;

				case 33:
				case 37:
					Score = (short)-(CPU.SignedQuotient(trade, 3) - 10);
					break;

				case 34:
					Score = (short)-(((short)CityTurn.Corruption << 1) - 0xc);
					break;

				case 35:
					Score = (short)-(town.TrueExtent / 2 - 10);

					if (town.Has(CityStructure.Palace))
						Score -= 4;

					FrameWord(DefensiveImprovementLocal, 1);
					break;

				case 36:
				{
					short room = CPU.SignedQuotient((short)FrameWord(HappyRoomLocal), 2);
					short size = town.TrueExtent;

					if ((short)(10 - room) <= size)
						Score = (short)-(size + room - 0xe);
					break;
				}

				case 38:
					if ((town.ConditionMarker & 1) != 0)
						Score = -2;
					break;

				case 39:
					Score = (short)-(CPU.SignedQuotient(trade, 4) - 10);
					break;

				case 41:
					if ((town.ConditionMarker & 1) != 0)
						Score = -1;
					break;

				case 42:
				case 47:
					Score = (short)-(CPU.SignedQuotient(food, 2) - 0xd);
					break;

				case 43:
				case 45:
				case 48:
					Score = (short)-(CPU.SignedQuotient(food, 8) - 0xc);
					break;

				case 46:
					Score = (short)-(CPU.SignedQuotient(food, 5) - 0xc);
					break;

				case 49:
				{
					// A spaceship part: not when the player or the human player has launched; 2 when the human player has
					// built a structure (bit 8 + human player), else 4
					byte person = (byte)this.Owner.World.PersonParticipantIdx;

					if ((((1 << participantIdx) + (1 << person)) & this.Owner.World.StarshipMarkers) == 0)
						Score = (short)((this.Owner.World.StarshipMarkers & (1 << (byte)(person + 8))) != 0 ? 2 : 4);
					break;
				}
			}

			if (Score >= 0x190)
			{
				Score = 0x3fff;
			}
			else
			{
				short weight = (short)(FrameWord(DefensiveLocal) == FrameWord(DefensiveImprovementLocal) ? 10 : 20);
				Score = (short)(weight * Score);

				short character = (short)(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Leaning) + 3);
				Score = CPU.SignedQuotient((short)(3 * Score), character);
			}

			if (town.Has(CityStructure.Barracks))
				Score += CPU.SignedQuotient(Score, 4);
		}

		private void ScoreDiplomatChoice(short participantIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			if (person <= 0 || (this.Owner.World.Participants[participantIdx].Relations[person] & 2) != 0 ||
				this.Owner.World.Participants[participantIdx].TroopsInsideMaking[27] > 0)
			{
				return;
			}

			FrameWord(TownLoopLocal, (ushort)this.Owner.Libc.Dice.DrawInt(5));

			if (((1 << participantIdx) & Turn.HumanContinentAttackers) == 0)
				return;

			ushort own = this.Owner.World.Participants[participantIdx].FoundScienceMarkers[FrameWord(TownLoopLocal)];

			if ((ushort)(this.Owner.World.Participants[person].FoundScienceMarkers[FrameWord(TownLoopLocal)] | own) == own)
				return;

			Score = (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[participantIdx].FoundScienceTally -
				this.Owner.World.Participants[person].FoundScienceTally + 10, 5, 10);
		}

		/// <summary>
		/// The score of a unit (false: not wanted): settlers only with room for unhappiness or no settlers yet; no militia
		/// above despotism; at most 3 nuclear weapons; fighters (0xe) only against a human player without peace who has more
		/// than twice as many; the units of the kind there and in production, weighed by the defence the city needs, the role
		/// and the approach, the cost, the strength and the moves
		/// </summary>
		private bool ScoreUnitChoice(short participantIdx, short townIdx, short choice)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			City town = this.Owner.World.Towns[townIdx];
			sbyte kind = (sbyte)choice;

			if (choice == 0 && (player.RegimeKind <= 1 ? 1 : 2) >= (short)FrameWord(HappyRoomLocal) &&
				(player.LiveTroops[0] != 0 || (short)FrameWord(MakingElsewhereTable) > 1))
			{
				return false;
			}

			if (choice == 1 && player.RegimeKind > 1)
				return false;

			if (choice == 0x19 && (player.LiveTroops[25] >= 4 ||
				(short)this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(participantIdx * 0x38 + 0xd33a)) >= 2))
			{
				return false;
			}

			if (choice == 0xe)
			{
				short human = CPU.SignedQuotient((short)this.Processor.PullUInt16(this.Processor.SegData.Value16,
					(ushort)(this.Owner.World.PersonParticipantIdx * 0x38 + 0xb3dc)), 2);

				if ((short)(player.LiveTroops[choice] + player.TroopsInsideMaking[choice]) >= human ||
					(player.Relations[this.Owner.World.PersonParticipantIdx] & 2) != 0)
				{
					return false;
				}
			}

			if (UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				Score = (short)((FrameWord(TableWord(MakingElsewhereTable, choice)) << 1) +
					FrameWord(TableWord(UnitsOnContinentTable, choice)));
			}
			else
			{
				short assault = player.Landmasses[FrameWord(ContinentLocal)].Assault;

				if (assault < 8)
					return false;

				short units = (short)((player.TroopsInsideMaking[choice] << 1) + player.LiveTroops[choice]);
				Score = CPU.SignedQuotient((short)(player.Landmasses[FrameWord(ContinentLocal)].Assault * units), (short)(player.TroopTally + 1));
			}

			if (FrameWord(DefenceNeedLocal) != 0)
			{
				if (UnitKindWord(kind, UnitKindTable.TroopGroup) != UnitKindTable.DefenceTroops)
					return false;

				Score = CPU.SignedQuotient(2, (short)FrameWord(DefenceNeedLocal));
			}

			ushort role = UnitKindWord(kind, UnitKindTable.TroopGroup);

			if (role != UnitKindTable.DefenceTroops && role != 0)
			{
				Score <<= 1;

				if (FrameWord(ApproachLocal) != role)
					Score <<= 1;
			}

			if (UnitKindWord(kind, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				if (FrameWord(ApproachLocal) != 5)
					return false;

				Score = CPU.SignedQuotient((short)(3 * Score), 2);
			}

			short weight = choice == 0
				? (short)((town.Has(CityStructure.Granary) ? 4 : 6) -
					LeaderWord(player.ChiefVisuals, LeaderTable.Leaning))
				: (short)0x10;

			short price = (short)(weight * (short)(UnitKindWord(kind, UnitKindTable.Price) + 2) * Score);
			short strength = (short)((short)(UnitKindWord(kind, UnitKindTable.AssaultPower) +
				UnitKindWord(kind, UnitKindTable.GuardPower)) *
				(short)(UnitKindWord(kind, UnitKindTable.ShiftTally) + 1));
			Score = CPU.SignedQuotient(price, strength);

			if (UnitKindWord(kind, UnitKindTable.TroopGroup) == 3)
				Score = (short)((FrameWord(DefensiveLocal) != 0 ? 4 : 2) * Score);

			if (UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				Score <<= 1;

			if (player.TroopTally > 120)
				Score <<= 2;

			// Doubled unless the player is at war (0x6ed4) and the approach is defensive or there is more food than 0xdeb8
			if ((FrameWord(DefensiveLocal) == 0 && (short)CityTurn.Shields <= (short)CityTurn.SpecialSlots) ||
				Turn.PlayerAtWar == 0)
			{
				Score <<= 1;
			}

			return true;
		}

		/// <summary>
		/// The answer, or null to show the menu again: a computer player takes the best choice (0xffff when the city builds
		/// nothing); a city of the human player that builds by itself takes the best improvement (score up to 500) or 0x63;
		/// else the advisors (once, with bit 0 of the preferences) and the menu
		/// </summary>
		private ushort? DecideProduction(short participantIdx, short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.World.Participants[participantIdx].TroopTally = (short)FrameWord(UnitTallyLocal);

			if (!this.Owner.World.IsHuman(participantIdx))
			{
				if ((byte)town.PresentMakingIdx == 0xff)
					return 0xffff;

				return ChoiceEntry((short)FrameWord(BestChoiceLocal));
			}

			if ((town.ConditionMarker & 0x10) != 0)
			{
				if (FrameWord(BestImprovementLocal) != 0xffff && (short)FrameWord(BestImprovementScoreLocal) <= 0x1f4)
					return ChoiceEntry((short)FrameWord(BestImprovementLocal));

				return 0x63;
			}

			if ((this.Owner.World.SessionPrefMarkers & 1) != 0 && AdvisorsShown == 0)
			{
				ushort screen = Screen.Box;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), 2);

				if (FrameWord(BestUnitLocal) != 0xffff)
					ShowProductionAdvice((short)FrameWord(BestUnitLocal), "Military Advisor:\n", 0x2284, 8);

				if (FrameWord(BestImprovementLocal) != 0xffff)
					ShowProductionAdvice((short)FrameWord(BestImprovementLocal), "Domestic Advisor:\n", 0x22a3, 164);

				AdvisorsShown = 1;
				screen = Screen.Box;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), 1);

				return null;
			}

			if (ChoiceCount > 0x14 || PageState != 0)
			{
				ushort screen = Screen.Box;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), 2);
			}

			Menu.Footer = 1;
			ushort picked = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 80, 8, 1);
			Score = (short)ChoiceEntry(picked + 1);

			if ((ushort)Score == 0x63)
			{
				PageState = 2;
				return null;
			}

			if ((ushort)Score == 0x62)
			{
				PageState = 1;
				return null;
			}

			ushort display = Screen.Box;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(display + 0x10), 1);

			if (Menu.HelpTopic == 0)
				return (ushort)Score;

			// The Civilopedia of the choice, then the menu again
			this.Owner.Startup.HideMouseCursor();
			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			Turn.CivilopediaWithoutPictures = 1;

			if (Score >= 0)
				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)Score, 2);
			else
				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)(-Score), 1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			this.Owner.Startup.ShowMouseCursor();
			MapView.RedrawRequest = 1;

			return null;
		}

		/// <summary>An advisor names a choice: the advice for "*name" from the text file, in a menu at the bottom</summary>
		private void ShowProductionAdvice(short entryIdx, string advisor, ushort documentAddr, int horizLoc)
		{
			Score = (short)ChoiceEntry(entryIdx);
			ushort name = Score >= 0 ? (ushort)(0x112a + 0x22 * Score) : ImprovementRecord(-Score);

			string choiceName = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, name));
			this.Owner.Words.Set(advisor);
			this.Owner.BlockText.LoadTextFromFile(documentAddr, "*" + choiceName);
			this.Owner.Codes.Set(3, choiceName);
			this.Owner.BlockText.ReflowText();
			this.Owner.BlockText.ArrangeTextRow(0x32);
			Screen.DialogNeedsRedraw = 1;
			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, horizLoc, 160, 1);
		}

		/// <summary>The choice of an AI player, or the discovery and the next choice of the human player (see UncoverTechnology)</summary>
		private ushort UncoverTechnologyOf(short participantIdx, short participantIdx1)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			Player human = this.Owner.World.Participants[person];

			short pick = -1;
			short highest = -1;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (!MayResearch(participantIdx, scienceIdx))
					continue;

				short draw = (short)this.Owner.Libc.Dice.DrawInt((ushort)(ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx) << 2));

				if (draw <= highest || (scienceIdx == human.PresentInquiryIdx && participantIdx == person))
					continue;

				highest = draw;
				pick = scienceIdx;
			}

			if (participantIdx == person && this.Owner.World.RoundTally != 0)
				return ChooseNextResearch(participantIdx, participantIdx1, pick);

			if (this.Owner.World.RoundTally != 0 && this.Owner.World.HardnessTier == 0 &&
				PlayerKnowsTechnology(person, pick) == 0 && this.Owner.Libc.Dice.DrawInt(2) != 0)
				return 0xffff;

			if (highest != -1)
			{
				EarnTechnology(participantIdx, pick, participantIdx1);

				this.Owner.Bot.RefreshCityProduction(participantIdx, 0xffff);
			}

			return (ushort)pick;
		}

		/// <summary>
		/// Whether the player can research the technology: unknown, with both prerequisites (0x4ee / 0x4ef + 0x16 * technology)
		/// known; SI points at its entry when it is unknown, like in the original
		/// </summary>
		private bool MayResearch(short participantIdx, short scienceIdx)
		{
			if (PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
				return false;

			ushort science = (ushort)(0x16 * scienceIdx);

			return PlayerKnowsTechnology(participantIdx, (sbyte)this.Processor.PullGlobalByte((ushort)(science + TechnologyTable.Prerequisite1Address))) != 0 &&
				PlayerKnowsTechnology(participantIdx, (sbyte)this.Processor.PullGlobalByte((ushort)(science + TechnologyTable.Prerequisite2Address))) != 0;
		}

		/// <summary>The human player's discovery and his choice of the next research (see UncoverTechnology)</summary>
		private ushort ChooseNextResearch(short participantIdx, short participantIdx1, short pick)
		{
			Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];
			ushort[] choiceList = new ushort[0x48];
			bool again = false;

			while (true)
			{
				if (human.PresentInquiryIdx != -1)
				{
					AnnounceDiscovery(participantIdx, participantIdx1);
				}

				if (this.Owner.World.Participants[participantIdx].FoundScienceTally <= 1)
				{
					this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicFirstciv);
				}

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

				this.Owner.Startup.ShowMouseCursor();

				if ((this.Owner.World.SessionPrefMarkers & 1) != 0 && pick != -1 && this.Owner.World.HardnessTier == 0)
				{
					RecommendResearch(pick);
				}

				ushort choices = ListResearchChoices(participantIdx, again, choiceList);

				Screen.DialogStyle = 0x6;

				if (choices == 0)
				{
					this.Owner.Words.Append(" Future Technology ");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(human.ComingScienceTally + 1), 10));

					this.Owner.Words.Append("\n");

					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

					human.PresentInquiryIdx = 71;

					return 0x48;
				}

				Menu.Footer = 0x1;

				ushort choice = this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 80, 64, 1);

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();

				if (choice == 0xffff)
					choice = 0;

				ushort scienceIdx = choiceList[choice];

				if (Menu.HelpTopic == 0)
				{
					human.PresentInquiryIdx = (short)scienceIdx;

					return scienceIdx;
				}

				this.Owner.Encyclopedia.ShowCivilopediaRecord(scienceIdx, 0);

				this.Owner.TurnHooks.RepaintMainScreen();

				// Asked again, with the same choices
				again = true;
			}
		}

		/// <summary>
		/// The human player's research is done: the discovery picture (discovr1 / discovr2 with technology 0x36, VGA, disk 1,
		/// preference 8, not for future technology) with the message and the icon of its first prerequisite, else the newspaper;
		/// then he learns it and the research is -1
		/// </summary>
		private void AnnounceDiscovery(short participantIdx, short participantIdx1)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			Player human = this.Owner.World.Participants[person];

			short first = (short)this.Owner.Encyclopedia.ObtainFirstPrerequisite((ushort)human.PresentInquiryIdx);
			bool picture = human.PresentInquiryIdx != 71 && (short)Files.FirstMissingDisk > 3 &&
				this.Owner.Startup.AskForDisk(1, 0) != 0 && (this.Owner.World.SessionPrefMarkers & 8) != 0;

			bool advanced = PlayerKnowsTechnology(person, 0x36) != 0;
			string pictureName = advanced ? "discovr2.pic" : "discovr1.pic";

			if (picture)
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, pictureName, 0);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, pictureName, 1);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			}

			Message(CivilizationName((ushort)participantIdx))
				.Text(PlayerKnowsTechnology(person, 0x14) != 0 ? (ushort)0x22cc : (ushort)0x22d8)
				.Text("\ndiscover the secret\nof ")
				.Text(TechnologyText(human.PresentInquiryIdx));

			if (human.PresentInquiryIdx == 71)
			{
				this.Owner.Words.Append(" ");

				human.ComingScienceTally++;

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(human.ComingScienceTally, 10));
			}

			this.Owner.Words.Append("!\n");

			// The original plays only a short fanfare (table at 0x1518, one to five seconds),
			// the national anthem (table at 0x151a, as in the palace and the throne room) plays until the next research is chosen
			this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			if (picture)
			{
				ShowDiscoveryPicture(pictureName, first, advanced);
			}
			else
			{
				this.Owner.Paper.ShowNewspaper(-1);
			}

			EarnTechnology(participantIdx, human.PresentInquiryIdx, participantIdx1);

			this.Owner.Services.PerformSound(1, 0);

			human.PresentInquiryIdx = -1;
		}

		/// <summary>The discovery message on the picture and the icon of the first prerequisite (iconpgN.pic, N = first / 9)</summary>
		private void ShowDiscoveryPicture(string pictureName, short first, bool advanced)
		{
			if (!advanced)
				DrawingBox.TypefaceIdx = 0x6;

			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 96, advanced ? (short)25 : (short)1, 1);

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.Words.Set("iconpg1.pic");

			this.Owner.Words[6] = (byte)(this.Owner.Words[6] + CPU.SignedQuotient(first, 9));

			this.Owner.Startup.AskForDisk(3, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, TextBuffer.Address, 0);

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.Encyclopedia.DrawIcon((ushort)(((first % 3) * 111) + 1), (ushort)((((first % 9) / 3) * 69) + 1), 110, 68, 174, advanced ? (ushort)87 : (ushort)96);

			if (Screen.VgaMode != 0)
			{
				string palName = advanced ? "discovr2.pal" : "discovr1.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, palName, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileIconpg1Pal, 0xc5be);

				this.Owner.Libc.CopyBytes(0xc5c4, 0xc1dc, 0xff);

				this.Owner.Services.LoadColorsFromBuffer(30, 0xc5be);
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Helpers.LaunchHueCycling();
		}

		/// <summary>The science advisor recommends the technology with the units and wonders it allows</summary>
		private void RecommendResearch(short pick)
		{
			Message("Science Advisor:\nI recommend we\ndevelop ")
				.Text(TechnologyText(pick))
				.Text(".\n");

			ushort length = this.Owner.Words.Length;

			ushort allowed = 0;

			this.Owner.Words.Append("So we can build:\n-");

			for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
			{
				ushort kindRecord = (ushort)(0x22 * kindIdx);

				if (UnitKindWordAt(kindRecord, UnitKindTable.Technology) != (ushort)pick)
					continue;

				this.Owner.Words.Append(UnitTextAt(kindRecord));

				this.Owner.Words.Append("\n-");

				allowed++;
			}

			for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
			{
				ushort wonderRecord = (ushort)(0x1e * wonderIdx);

				if (BuildingWordAt(wonderRecord, BuildingTable.Technology) != (ushort)pick)
					continue;

				this.Owner.Words.Append(BuildingRecordTextAt(wonderRecord));

				this.Owner.Words.Append("\n-");

				allowed++;
			}

			if (allowed == 0)
				this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0);

			DrawingBox.TypefaceIdx = 0x2;
			Screen.DialogNeedsRedraw = 1;

			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 8, (short)-((allowed << 3) - 0xa8), 1);

			DrawingBox.TypefaceIdx = 0x1;
		}

		/// <summary>
		/// The menu of the researchable technologies (their list in the stack frame at -0x3a): from the third on, on the harder
		/// levels only those (technology - known technologies) divisible by 3; again: only the same choices in the same order;
		/// the number of choices
		/// </summary>
		private ushort ListResearchChoices(short participantIdx, bool again, ushort[] choiceList)
		{
			Message("Which discovery should our\n")
				.Text(PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x14) != 0 ? (ushort)0x2388 : (ushort)0x2393)
				.Text(" be pursuing, sire?\nPick one...\n ");

			ushort choices = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (!MayResearch(participantIdx, scienceIdx))
					continue;

				if (!again)
				{
					if ((short)choices > 1 && (short)(scienceIdx - this.Owner.World.Participants[participantIdx].FoundScienceTally) % 3 == 0 &&
						this.Owner.World.HardnessTier != 0)
						continue;
				}
				else
				{
					if (choiceList[choices] != (ushort)scienceIdx)
						continue;
				}

				this.Owner.Words.Append(TechnologyText(scienceIdx));

				this.Owner.Words.Append("\n ");

				choiceList[choices] = (ushort)scienceIdx;
				choices++;
			}

			return choices;
		}

		/// <summary>The message about a new technology, or the emissary of a player without contact (see EarnTechnology)</summary>
		private void AnnounceTechnology(short participantIdx, short scienceIdx, short participantIdx1)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				if ((this.Owner.World.Participants[person].Relations[participantIdx] & 0x40) == 0)
				{
					SendTechnologyEmissary(participantIdx, scienceIdx);
					return;
				}
			}

			EmptyMessage()
				.Text(CivilizationPlural((ushort)participantIdx))
				.Text(participantIdx == participantIdx1 || participantIdx1 == 0 ? (ushort)0x23d7 : (ushort)0x23e1)
				.Text(TechnologyText(scienceIdx));

			if (TechnologyNewsRules.NamesSource(participantIdx, participantIdx1, this.Owner.TurnHooks.ClassicTechnologyNews))
			{
				this.Owner.Words.Append(" from\nthe ");

				if (participantIdx1 == TechnologyNewsRules.GreatLibrary)
				{
					this.Owner.Words.Append("Great Library");
				}
				else
				{
					this.Owner.Words.Append(CivilizationPlural((ushort)participantIdx1));
				}
			}

			this.Owner.Words.Append("!\n ");

			if (participantIdx != person)
			{
				// The wonders (0xbb6, names 0xb9a) and the units (0x114a, names 0x112a) the technology allows
				for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
				{
					ushort wonderRecord = (ushort)(0x1e * wonderIdx);

					if (BuildingWordAt(wonderRecord, BuildingTable.Technology) == (ushort)scienceIdx)
					{
						this.Owner.Words.Append(BuildingRecordTextAt(wonderRecord));

						this.Owner.Words.Append("\n ");
					}
				}

				for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
				{
					ushort kindRecord = (ushort)(0x22 * kindIdx);

					if (UnitKindWordAt(kindRecord, UnitKindTable.Technology) == (ushort)scienceIdx)
					{
						this.Owner.Words.Append(UnitTextAt(kindRecord));

						this.Owner.Words.Append("\n ");
					}
				}
			}

			if ((participantIdx != person || participantIdx1 == -2) && King.Answer == 0xffff)
			{
				Menu.AnyKey = 1;

				Screen.DialogStyle = participantIdx == person ? (ushort)6 : (ushort)0;

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		/// <summary>
		/// The first time anybody learns the technology, a player without contact who knows technology 8 sends an emissary to
		/// the human player who doesn't know it, one time in 3; when he is received the king tells about it (0x240e)
		/// </summary>
		private void SendTechnologyEmissary(short participantIdx, short scienceIdx)
		{
			if (((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) != 0 ||
				PlayerKnowsTechnology(participantIdx, 8) == 0 ||
				PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, scienceIdx) != 0 ||
				this.Owner.Libc.Dice.DrawInt(3) != 0)
				return;

			if (this.Owner.Audience.WelcomeEmissary(participantIdx, -1, 0) == 0xffff)
				return;

			King.Answer = 0;
			King.EmissaryAudience = 0;
			this.Owner.Words.Clear();

			this.Owner.Codes.Set(0, CivilizationPlural((ushort)participantIdx));

			this.Owner.Codes.Set(3, TechnologyText(scienceIdx));

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicGape);

			King.Mood = 0x2;
			King.Speaking = 0xc;

			this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);

			this.Owner.Audience.FinishAudience();
		}

		/// <summary>What a new technology does after its message (see EarnTechnology)</summary>
		private void ApplyTechnology(short participantIdx, short scienceIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				if (scienceIdx != 0x47)
				{
					this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)scienceIdx, 0);
				}

				if (scienceIdx == 0x14)
				{
					this.Owner.NewGameWizard.LoadGovernmentGraphics();

					this.Owner.TurnHooks.RepaintMainScreen();
				}

				if (King.Answer == 0xffff)
				{
					this.Owner.TurnHooks.RepaintMainScreen();

					if (scienceIdx == 0x5)
					{
						this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicMonarchy);
					}
				}
			}
			else if (this.Owner.World.HardnessTier == 0 && scienceIdx == 0x16)
			{
				// The exchange of ambassadors that followed never happens: the test of the original, ((Relations == 0) & 0x40),
				// is always false
			}

			if (ScienceRules.DoesTechObsoleteBarracks(scienceIdx))
			{
				for (int townIdx = 0; townIdx < 128; townIdx++)
				{
					if (this.Owner.World.Towns[townIdx].ConditionMarker != 0xff &&
						this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx)
					{
						this.Owner.World.Towns[townIdx].StructureMarkers0 &= 0xfffd;
					}
				}

				if (participantIdx == person && King.Answer == 0xffff)
				{
					Message("Development of ")
						.Text(TechnologyText(scienceIdx))
						.Text("\nmakes existing Barracks\nobsolete.\n")
						.Style(0x3)
						.Dialog(80, 100);
				}
			}

			if (((byte)this.Owner.World.ScienceLeadingFoundVia[scienceIdx] & 7) == 0)
			{
				this.Owner.World.ScienceLeadingFoundVia[scienceIdx] = participantIdx;

				this.Owner.UnitOps.AttachReplayContent(5, (byte)participantIdx, (byte)scienceIdx);

				// The human player's wonders (0x10fe: the technology that makes each obsolete) lose their effect
				for (ushort wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
				{
					if (WonderObsoleteTechnology(wonderIdx) != (ushort)scienceIdx ||
						!WonderCityRules.ReadsCity(this.Owner.World.MarvelTownIdx[wonderIdx], this.Owner.TurnHooks.ClassicWonderCities) ||
						this.Owner.World.Towns[this.Owner.World.MarvelTownIdx[wonderIdx]].ParticipantIdx != person)
						continue;

					Message(TechnologyText(scienceIdx))
						.Text(" cancels\nthe effect of\n")
						.Text(WonderText(wonderIdx))
						.Text(".\n");

					this.Owner.Paper.ShowNewspaper(-1);
				}
			}

			// The Great Library (wonder 5) gives a technology that two players know
			ushort libraryOwner = (ushort)this.Owner.World.MarvelTownIdx[5];

			if (libraryOwner == 0xffff || !WonderCityRules.ReadsCity(libraryOwner, this.Owner.TurnHooks.ClassicWonderCities))
				return;

			libraryOwner = (ushort)this.Owner.World.Towns[libraryOwner].ParticipantIdx;

			if (PlayerKnowsTechnology((short)libraryOwner, scienceIdx) != 0)
				return;

			if (this.Owner.CityOps.PlayerEnjoysWonderEffect((short)libraryOwner, 5) == 0)
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
	}
}
