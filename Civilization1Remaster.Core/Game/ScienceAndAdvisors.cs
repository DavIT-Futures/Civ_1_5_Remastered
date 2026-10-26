using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1ade.
	/// </summary>
	public class ScienceAndAdvisors : GameModule
	{
		private readonly UnitMemory troops;

		public ScienceAndAdvisors(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
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

			// HandleCity is called at the depth of the original (the frame stays; B3: the locals are C# variables)
			this.processor.EnterFrame(0xa);
			this.processor.PreserveIndexRegisters();

			DemocracyPressure = 0;
			AnarchyPressure = 0;

			// 1: a city of size above 5 whose turn returned 0, 2: a city in disorder
			int unrest = 0;

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff || this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				while (true)
				{
					if (this.owner.CityOps.HandleCity(townIdx, 0) == 0 && this.owner.World.Towns[townIdx].TrueExtent > 5)
					{
						unrest |= 1;
					}

					if ((this.owner.World.Towns[townIdx].ConditionMarker & 1) != 0)
					{
						unrest |= 2;
					}

					if (CityAgain == 0)
						break;

					this.owner.CityOps.HandleCity(townIdx, 1);

					this.owner.TurnHooks.RepaintMainScreen();

					bool again = CityAgain == 2;

					CityAgain = 0;

					if (!again)
						break;
				}
			}

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

			this.processor.PreserveIndexRegisters();

			City town = this.owner.World.Towns[townIdx];

			this.owner.UnitOps.AttachReplayContent(1, 0xff, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			this.owner.World.Participants[town.ParticipantIdx].StopMaking(town.PresentMakingIdx);

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
				int spotHoriz = this.owner.Routing.TunePosition((short)DirectionHoriz(spotIdx) + horizLoc);
				short spotVert = (short)(DirectionVert(spotIdx) + vertLoc);

				this.owner.Helpers.LocateNearestCity(spotHoriz, spotVert);

				if (spotIdx <= 0x14 || (short)FoundDistance > 3)
				{
					ushort holder = this.owner.VideoCard.ObtainDot(2, spotHoriz + 0x50, spotVert + 0x32);
					this.owner.Services.AssignDot(2, spotHoriz + 0x50, spotVert, holder);
				}
			}

			this.processor.RestoreIndexRegisters();

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
				ScreenBox,
				0, 0, 0x140, 0xc8,
				BackScreenBox,
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
				BackScreenBox,
				0, 0, 0x140, 0xc8,
				ScreenBox,
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

			MapRedrawRequest = 0x0;

			this.owner.CityOps.HandleCity(townIdx, 1);

			if (MapRedrawRequest != 0)
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
		/// What a city builds next. The menu text (TextBuffer.Address, "What shall we build in ...") lists the units, improvements and
		/// wonders the player can build, with a list of the choices in the frame (bp - 0xdc, units >= 0, improvements and
		/// wonders negative, 0x63 / 0x62 for "more..."); every choice gets a score for the city (lower is better). A computer
		/// player takes the best one (1 / 3 / 7 for a player without units or the barbarians); for the human player the advisors
		/// name their best unit and improvement once, then the menu asks, with the Civilopedia on demand.
		/// </summary>
		public ushort PickCityProduction(short participantIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"PickCityProduction({participantIdx}, {townIdx})");

			// The words of the frame are in C# (B3, properties below), with what lay on the stack (some are read before they
			// are written); the key of the advice and the words around it (a long name could run over) stay on the stack, so
			// does the frame (HandleCity at the depth of the original)
			this.processor.EnterFrame(0x14a);
			FrameWords outerFrame = this.pickFrame;
			this.pickFrame = new FrameWords(this.processor, 0x14a, AdviceSpillLocal, AdviceKey).WithLeftovers();
			this.processor.PreserveIndexRegisters();

			// "*" before the name of the best choice in the frame: the key of its advice in the text file
			this.pickFrame.SetFrameByte(AdviceKey, 0x2a);
			PageState = 0;
			AdvisorsShown = 0;

			this.owner.CityOps.HandleCity(townIdx, -1);

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

			this.processor.RegAcc.Value16 = choice;
			this.processor.RestoreIndexRegisters();
			this.pickFrame = outerFrame;
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("PickCityProduction");

			return choice;
		}

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
		private const int AdviceKey = 0x12e;
		/// <summary>The lowest word a name copied to the key of the advice could reach (28 letters)</summary>
		private const int AdviceSpillLocal = 0x112;
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

		/// <summary>The words of the frame of PickCityProduction (B3: in C#, new for every call)</summary>
		private FrameWords pickFrame = null!;

		private ushort FrameWord(int distance)
		{
			return this.pickFrame.Frame(distance);
		}

		private void FrameWord(int distance, ushort amount)
		{
			this.pickFrame.SetFrame(distance, amount);
		}

		/// <summary>0: one page, 1: the menu is cut after the units ("more..." leads to the rest), 2: the rest only</summary>
		private ushort PageState { get => FrameWord(PageStateLocal); set => FrameWord(PageStateLocal, value); }

		private ushort AdvisorsShown { get => FrameWord(AdvisorsShownLocal); set => FrameWord(AdvisorsShownLocal, value); }

		/// <summary>The next free entry of the list of choices (entry 0 is the production of the city)</summary>
		private short ChoiceCount { get => (short)FrameWord(ChoiceCountLocal); set => FrameWord(ChoiceCountLocal, (ushort)value); }

		private short Score { get => (short)FrameWord(ScoreLocal); set => FrameWord(ScoreLocal, (ushort)value); }

		private ushort ChoiceEntry(int entryIdx) => FrameWord(TableWord(ChoiceTable, entryIdx));

		private void ChoiceEntry(int entryIdx, ushort choice) => FrameWord(TableWord(ChoiceTable, entryIdx), choice);

		/// <summary>Appends a choice to the list (and to the menu text, done by the caller)</summary>
		private void AddChoice(ushort choice)
		{
			short entryIdx = ChoiceCount;
			ChoiceCount = (short)(entryIdx + 1);
			ChoiceEntry(entryIdx, choice);
		}

		/// <summary>The distance of a word of the frame table at bp - table + 2 * index (the unit tables of 28 words)</summary>
		private static int TableWord(int table, int index) => table - (index << 1);

		/// <summary>
		/// The menu text and the list of choices. True when the list is too long for one page the first time: it starts again,
		/// cut after the units
		/// </summary>
		private bool ListProductionChoices(short participantIdx, short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			ChoiceCount = 1;
			FrameWord(ChoiceTable, (ushort)(short)town.PresentMakingIdx);

			Message("What shall we build in ")
				.City(townIdx)
				.Text("?\n ");

			if (PageState != 2)
				ListUnitChoices(participantIdx, townIdx);

			FrameWord(PageCutCountLocal, (ushort)ChoiceCount);
			FrameWord(PageCutLengthLocal, this.owner.Words.Length);

			// The Great Wall on the continent of the city (no walls and the like then)
			FrameWord(SharedWallLocal, 0);

			if (this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 15) != 0)
			{
				City wallTown = this.owner.World.Towns[this.owner.World.MarvelTownIdx[15]];
				FrameWord(WallContinentLocal, this.owner.MapOps.ObtainContinentID(wallTown.Place.X, wallTown.Place.Y));

				if (this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) == FrameWord(WallContinentLocal))
					FrameWord(SharedWallLocal, 1);
			}

			if ((this.owner.World.DiagMarkers & 4) != 0)
				ListImprovementChoices(participantIdx, townIdx);

			ListWonderChoices(participantIdx, townIdx);

			if (participantIdx == this.owner.World.PersonParticipantIdx && (town.ConditionMarker & 0x10) == 0 && ChoiceCount > 0x1e)
			{
				if (PageState == 0)
				{
					PageState = 1;
					return true;
				}

				this.owner.Libc.CopyText((ushort)(FrameWord(PageCutLengthLocal) + TextBuffer.Address), "more...\n");
				ChoiceEntry(FrameWord(PageCutCountLocal), 0x63);
			}

			if (PageState == 2)
			{
				this.owner.Words.Append("more...\n");
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
			City town = this.owner.World.Towns[townIdx];

			for (FrameWord(ChoiceLoopLocal, 0); (short)FrameWord(ChoiceLoopLocal) < 0x1c; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				sbyte kind = (sbyte)FrameWord(ChoiceLoopLocal);

				if (kind == 0x19 && (this.owner.World.MarvelTownIdx[17] == -1 || PlayerKnowsTechnology(participantIdx, 0x1b) == 0))
					continue;

				if (PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0x20)) == 0 ||
					PlayerKnowsTechnology(participantIdx, (short)UnitKindWord(kind, 0xc)) != 0)
				{
					continue;
				}

				if ((town.ConditionMarker & 2) == 0 && UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
					continue;

				this.owner.Words.Append((ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride));
				ComposeProductionOption(townIdx, UnitKindWord(kind, UnitKindTable.Price));
				this.owner.Words.Append(", ADM:");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.AssaultPower), 10));
				this.owner.Words.Append("/");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.GuardPower), 10));
				this.owner.Words.Append("/");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)UnitKindWord(kind, UnitKindTable.ShiftTally), 10));
				this.owner.Words.Append(")\n ");

				AddChoice((ushort)kind);
			}
		}

		/// <summary>The record of an improvement or wonder (0x1e bytes from 0xb9a, the name first)</summary>
		private static ushort ImprovementRecord(int improvementIdx) => (ushort)(0xb9a + improvementIdx * 0x1e);

		/// <summary>
		/// The improvements 1 - 24 the player knows and the city hasn't built, without the ones that don't fit: a second
		/// building of bit 0 (7), 15 with bit 15, 10 / 12 when replaced by a known technology, 20 only in a city with bit 3 of
		/// its state, 19 with bit 14, 19 - 21 not with the Great Wall on the continent or with bits 2 - 4 of the second word;
		/// the spaceship parts (22 - 24) after the Apollo Program, before the launch
		/// </summary>
		private void ListImprovementChoices(short participantIdx, short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			for (FrameWord(ChoiceLoopLocal, 1); (short)FrameWord(ChoiceLoopLocal) <= 0x18; FrameWord(ChoiceLoopLocal, (ushort)(FrameWord(ChoiceLoopLocal) + 1)))
			{
				short improvementIdx = (short)FrameWord(ChoiceLoopLocal);

				if (PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x1c))) == 0)
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
					(this.owner.World.MarvelTownIdx[19] == -1 || (this.owner.World.StarshipMarkers & (1 << participantIdx)) != 0))
				{
					continue;
				}

				this.owner.Words.Append(ImprovementRecord(improvementIdx));
				ComposeProductionOption(townIdx, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x18)));
				this.owner.Words.Append(")\n ");

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

				if (PlayerKnowsTechnology(participantIdx, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x1c))) != 0 &&
					this.owner.World.MarvelTownIdx[improvementIdx - 0x18] == -1)
				{
					ushort obsolete = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((improvementIdx << 1) + 0x10ce));

					if (obsolete != 0x7f && ((byte)this.owner.World.ScienceLeadingFoundVia[this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((improvementIdx << 1) + 0x10ce))] & 7) != 0)
						this.owner.Words.Append("*");

					this.owner.Words.Append(ImprovementRecord(improvementIdx));
					ComposeProductionOption(townIdx, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(ImprovementRecord(improvementIdx) + 0x18)));
					this.owner.Words.Append(")\n ");

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
			Player player = this.owner.World.Participants[participantIdx];
			City town = this.owner.World.Towns[townIdx];

			FrameWord(ContinentLocal, this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y));
			FrameWord(ApproachLocal, (ushort)player.Landmasses[FrameWord(ContinentLocal)].Approach);
			FrameWord(DefensiveLocal, (ushort)((short)FrameWord(ApproachLocal) is 1 or 2 ? 1 : 0));

			if (LateGameNudge != 0 && (short)FrameWord(ApproachLocal) == 5)
				FrameWord(DefensiveLocal, 1);

			short unhappy = (short)((player.RegimeKind <= 1 ? 1 : 2) * (short)CitySettlers);
			FrameWord(HappyRoomLocal, (ushort)((short)CityFood - unhappy - (town.TrueExtent << 1)));

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
				City other = this.owner.World.Towns[FrameWord(TownLoopLocal)];

				if (other.ConditionMarker != 0xff && other.ParticipantIdx == participantIdx &&
					this.owner.MapOps.ObtainContinentID(other.Place.X, other.Place.Y) == FrameWord(ContinentLocal))
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

					if (this.owner.MapOps.ObtainContinentID(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == FrameWord(ContinentLocal))
					{
						int units = TableWord(UnitsOnContinentTable, this.troops.TroopKind(participantIdx, troopIdx));
						FrameWord(units, (ushort)(FrameWord(units) + 1));
					}
				}

				FrameWord(ChoiceLoopLocal, (ushort)(troopIdx + 1));
			}
			while ((short)FrameWord(ChoiceLoopLocal) < 0x80);

			if (player.TroopTally == 0 && participantIdx != this.owner.World.PersonParticipantIdx && player.RegimeKind <= 1)
				return 1;

			if (participantIdx == 0)
				return (ushort)((this.processor.PullUInt8(0x3772, 0x520) & 7) != 0 ? 7 : 3);

			return null;
		}

		/// <summary>
		/// The defenders the city needs (0x8): 2 without units in it, 1 when it has fewer defenders than a quarter of its
		/// size plus the defensive approach
		/// </summary>
		private void AssessCityDefence(short participantIdx, short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			FrameWord(DefenceNeedLocal, 0);
			FrameWord(ChoiceLoopLocal, this.owner.MapOps.ObtainUnitAtTile(town.Place.X, town.Place.Y));

			if (FrameWord(ChoiceLoopLocal) == 0xffff)
			{
				FrameWord(DefenceNeedLocal, 2);
				return;
			}

			short defenders = (short)this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, (short)FrameWord(ChoiceLoopLocal), 2);

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
					this.processor.RegAcc.Value16 = FrameWord(ChoiceLoopLocal);
					FrameWord(BestChoiceLocal, this.processor.RegAcc.Value16);
					this.processor.RegAcc.Value16 = FrameWord(ScoreLocal);
					FrameWord(BestScoreLocal, this.processor.RegAcc.Value16);
				}

				if (choice < 0 || choice >= 0x1a)
				{
					if (Score < (short)FrameWord(BestImprovementScoreLocal))
					{
						FrameWord(BestImprovementScoreLocal, (ushort)Score);
						this.processor.RegAcc.Value16 = FrameWord(ChoiceLoopLocal);
						FrameWord(BestImprovementLocal, this.processor.RegAcc.Value16);
					}
				}
				else if (Score < (short)FrameWord(BestUnitScoreLocal))
				{
					FrameWord(BestUnitScoreLocal, (ushort)Score);
					this.processor.RegAcc.Value16 = FrameWord(ChoiceLoopLocal);
					FrameWord(BestUnitLocal, this.processor.RegAcc.Value16);
				}
			}
		}

		/// <summary>
		/// The score of an improvement, a wonder, a diplomat (26) or a caravan (27): its need in the city (most are 999, not
		/// needed), weighed by the defensive approach and the leader's character, half more with bit 1 of the buildings
		/// </summary>
		private void ScoreImprovementChoice(short participantIdx, short townIdx, short choice)
		{
			City town = this.owner.World.Towns[townIdx];
			short food = (short)CityShields;
			short trade = (short)CityTrade;

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

					if (participantIdx == this.owner.World.PersonParticipantIdx && (town.ConditionMarker & 0x10) != 0)
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
					Score = (short)-(((short)CityCorruption << 1) - 0xc);
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
					byte person = (byte)this.owner.World.PersonParticipantIdx;

					if ((((1 << participantIdx) + (1 << person)) & this.owner.World.StarshipMarkers) == 0)
						Score = (short)((this.owner.World.StarshipMarkers & (1 << (byte)(person + 8))) != 0 ? 2 : 4);
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

				short character = (short)(this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(0x1516 + 0x3a * this.owner.World.Participants[participantIdx].ChiefVisuals)) + 3);
				Score = CPU.SignedQuotient((short)(3 * Score), character);
			}

			if (town.Has(CityStructure.Barracks))
				Score += CPU.SignedQuotient(Score, 4);
		}

		private void ScoreDiplomatChoice(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;

			if (person <= 0 || (this.owner.World.Participants[participantIdx].Relations[person] & 2) != 0 ||
				this.owner.World.Participants[participantIdx].TroopsInsideMaking[27] > 0)
			{
				return;
			}

			FrameWord(TownLoopLocal, (ushort)this.owner.Libc.Dice.DrawInt(5));

			if (((1 << participantIdx) & HumanContinentAttackers) == 0)
				return;

			ushort own = this.owner.World.Participants[participantIdx].FoundScienceMarkers[FrameWord(TownLoopLocal)];

			if ((ushort)(this.owner.World.Participants[person].FoundScienceMarkers[FrameWord(TownLoopLocal)] | own) == own)
				return;

			Score = (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[participantIdx].FoundScienceTally -
				this.owner.World.Participants[person].FoundScienceTally + 10, 5, 10);
		}

		/// <summary>
		/// The score of a unit (false: not wanted): settlers only with room for unhappiness or no settlers yet; no militia
		/// above despotism; at most 3 nuclear weapons; fighters (0xe) only against a human player without peace who has more
		/// than twice as many; the units of the kind there and in production, weighed by the defence the city needs, the role
		/// and the approach, the cost, the strength and the moves
		/// </summary>
		private bool ScoreUnitChoice(short participantIdx, short townIdx, short choice)
		{
			Player player = this.owner.World.Participants[participantIdx];
			City town = this.owner.World.Towns[townIdx];
			sbyte kind = (sbyte)choice;

			if (choice == 0 && (player.RegimeKind <= 1 ? 1 : 2) >= (short)FrameWord(HappyRoomLocal) &&
				(player.LiveTroops[0] != 0 || (short)FrameWord(MakingElsewhereTable) > 1))
			{
				return false;
			}

			if (choice == 1 && player.RegimeKind > 1)
				return false;

			if (choice == 0x19 && (player.LiveTroops[25] >= 4 ||
				(short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(participantIdx * 0x38 + 0xd33a)) >= 2))
			{
				return false;
			}

			if (choice == 0xe)
			{
				short human = CPU.SignedQuotient((short)this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(this.owner.World.PersonParticipantIdx * 0x38 + 0xb3dc)), 2);

				if ((short)(player.LiveTroops[choice] + player.TroopsInsideMaking[choice]) >= human ||
					(player.Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
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
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1516 + 0x3a * player.ChiefVisuals)))
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
			if ((FrameWord(DefensiveLocal) == 0 && (short)CityShields <= (short)CitySpecialSlots) ||
				PlayerAtWar == 0)
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
			City town = this.owner.World.Towns[townIdx];

			this.owner.World.Participants[participantIdx].TroopTally = (short)FrameWord(UnitTallyLocal);

			if (!this.owner.World.IsHuman(participantIdx))
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

			if ((this.owner.World.SessionPrefMarkers & 1) != 0 && AdvisorsShown == 0)
			{
				ushort screen = ScreenBox;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen + 0x10), 2);

				if (FrameWord(BestUnitLocal) != 0xffff)
					ShowProductionAdvice((short)FrameWord(BestUnitLocal), "Military Advisor:\n", 0x2284, 8);

				if (FrameWord(BestImprovementLocal) != 0xffff)
					ShowProductionAdvice((short)FrameWord(BestImprovementLocal), "Domestic Advisor:\n", 0x22a3, 164);

				AdvisorsShown = 1;
				screen = ScreenBox;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen + 0x10), 1);

				return null;
			}

			if (ChoiceCount > 0x14 || PageState != 0)
			{
				ushort screen = ScreenBox;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen + 0x10), 2);
			}

			MenuFooter = 1;
			ushort picked = this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 80, 8, 1);
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

			ushort display = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(display + 0x10), 1);

			if (MenuHelpTopic == 0)
				return (ushort)Score;

			// The Civilopedia of the choice, then the menu again
			this.owner.Startup.ConcealMouseCursor();
			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			CivilopediaWithoutPictures = 1;

			if (Score >= 0)
				this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)Score, 2);
			else
				this.owner.Encyclopedia.DisplayCivilopediaRecord((ushort)(-Score), 1);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			this.owner.Startup.DisplayMouseCursor();
			MapRedrawRequest = 1;

			return null;
		}

		/// <summary>An advisor names a choice: the advice for "*name" from the text file, in a menu at the bottom</summary>
		private void ShowProductionAdvice(short entryIdx, string advisor, ushort documentAddr, int horizLoc)
		{
			Score = (short)ChoiceEntry(entryIdx);
			ushort name = Score >= 0 ? (ushort)(0x112a + 0x22 * Score) : ImprovementRecord(-Score);

			ushort key = (ushort)(this.processor.RegFrame.Value16 - AdviceKey);
			this.owner.Libc.CopyText((ushort)(key + 1), name);
			this.owner.Words.Set(advisor);
			this.owner.BlockText.ImportTextFromFile(documentAddr, key);
			this.owner.Libc.CopyText(CodeText4, (ushort)(key + 1));
			this.owner.BlockText.ReflowText();
			this.owner.BlockText.ArrangeTextRow(0x32);
			this.processor.EmitGlobalWord(0xdb38, 1);
			this.owner.Dialogs.DisplayMenu(TextBuffer.Address, horizLoc, 160, 1);
		}

		/// <summary>
		/// Appends " (n turns)" or " (1 turn)" to the text at TextBuffer.Address: the turns the city needs for the production of cost arg2
		/// (in tens of shields), with the shields per turn 0x70dc - 0xd2f6 (1 - 99); 1 - 999 turns
		/// </summary>
		public void ComposeProductionOption(short townIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"ComposeProductionOption({townIdx}, {arg2})");

			int perTurn = this.owner.Helpers.VerifyValueSpan(
				(short)CityShields - (short)CityUnitUpkeep, 1, 99);

			ushort turns = (ushort)this.owner.Helpers.VerifyValueSpan(
				(((10 * arg2) - this.owner.World.Towns[townIdx].MaterialsTally - 1) / (short)perTurn) + 1, 1, 999);

			this.owner.Words.Append(" (");

			this.owner.Words.Append(this.owner.Libc.IntegerToText((short)turns, 10));

			this.owner.Words.Append((ushort)(turns != 1 ? 0x22b2 : 0x22b9));

			this.processor.Journal.CloseScope("ComposeProductionOption");
		}

		/// <summary>
		/// The player's research is done (participantIdx1: where it came from, see EarnTechnology). An AI player (and the
		/// human player in turn 0) takes the researchable technology with the highest random draw of 4 * ObtainTechnologyInfo
		/// (not the human player's current research for himself); on the easiest level a technology the human player doesn't
		/// know only one time in 2; returns it (-1 for none or barbarians). The human player gets his research (the discovery
		/// picture with its icon, or the newspaper), the science advisor's recommendation (easiest level, preference 1) and
		/// chooses the next research from the menu (on harder levels from the third on only every third one; asking for the
		/// Civilopedia shows it and asks again with the same choices; none left: future technology 71); returns the choice.
		/// The list of the choices and the file name are in the stack frame, and SI gets the values of the original before the
		/// calls (DisplayMenu leaves it in DI), like in the original.
		/// </summary>
		public ushort UncoverTechnology(short participantIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"UncoverTechnology({participantIdx}, {participantIdx1})");

			this.processor.EnterFrame(0x46);
			this.processor.PreserveSource();

			ushort result = participantIdx == 0 ? (ushort)0xffff : UncoverTechnologyOf(participantIdx, participantIdx1);

			this.processor.RegAcc.Value16 = result;

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("UncoverTechnology");

			return result;
		}

		/// <summary>The choice of an AI player, or the discovery and the next choice of the human player (see UncoverTechnology)</summary>
		private ushort UncoverTechnologyOf(short participantIdx, short participantIdx1)
		{
			short person = this.owner.World.PersonParticipantIdx;
			Player human = this.owner.World.Participants[person];

			short pick = -1;
			short highest = -1;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (!MayResearch(participantIdx, scienceIdx))
					continue;

				short draw = (short)this.owner.Libc.Dice.DrawInt((ushort)(ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx) << 2));

				if (draw <= highest || (scienceIdx == human.PresentInquiryIdx && participantIdx == person))
					continue;

				highest = draw;
				pick = scienceIdx;
			}

			if (participantIdx == person && this.owner.World.RoundTally != 0)
				return ChooseNextResearch(participantIdx, participantIdx1, pick);

			if (this.owner.World.RoundTally != 0 && this.owner.World.HardnessTier == 0 &&
				PlayerKnowsTechnology(person, pick) == 0 && this.owner.Libc.Dice.DrawInt(2) != 0)
				return 0xffff;

			if (highest != -1)
			{
				EarnTechnology(participantIdx, pick, participantIdx1);

				this.owner.Bot.RefreshCityProduction(participantIdx, 0xffff);
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
			this.processor.RegSource.Value16 = science;

			return PlayerKnowsTechnology(participantIdx, (sbyte)this.processor.PullGlobalByte((ushort)(science + 0x4ee))) != 0 &&
				PlayerKnowsTechnology(participantIdx, (sbyte)this.processor.PullGlobalByte((ushort)(science + 0x4ef))) != 0;
		}

		/// <summary>The human player's discovery and his choice of the next research (see UncoverTechnology)</summary>
		private ushort ChooseNextResearch(short participantIdx, short participantIdx1, short pick)
		{
			Player human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];
			ushort[] choiceList = new ushort[0x48];
			bool again = false;

			while (true)
			{
				if (human.PresentInquiryIdx != -1)
				{
					AnnounceDiscovery(participantIdx, participantIdx1);
				}

				if (this.owner.World.Participants[participantIdx].FoundScienceTally <= 1)
				{
					this.owner.HelpSystem.DisplayHelpPopup(0x231d);
				}

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

				this.owner.Startup.DisplayMouseCursor();

				if ((this.owner.World.SessionPrefMarkers & 1) != 0 && pick != -1 && this.owner.World.HardnessTier == 0)
				{
					RecommendResearch(pick);
				}

				ushort choices = ListResearchChoices(participantIdx, again, choiceList);

				DialogStyle = 0x6;

				if (choices == 0)
				{
					this.owner.Words.Append(" Future Technology ");

					this.owner.Words.Append(this.owner.Libc.IntegerToText((short)(human.ComingScienceTally + 1), 10));

					this.owner.Words.Append("\n");

					this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

					human.PresentInquiryIdx = 71;

					return 0x48;
				}

				MenuFooter = 0x1;

				ushort choice = this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 80, 64, 1);

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				this.owner.Startup.DisplayMouseCursor();

				if (choice == 0xffff)
					choice = 0;

				this.processor.RegSource.Value16 = (ushort)(choice << 1);

				ushort scienceIdx = choiceList[choice];

				if (MenuHelpTopic == 0)
				{
					human.PresentInquiryIdx = (short)scienceIdx;

					return scienceIdx;
				}

				this.owner.Encyclopedia.DisplayCivilopediaRecord(scienceIdx, 0);

				this.owner.TurnHooks.RepaintMainScreen();

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
			short person = this.owner.World.PersonParticipantIdx;
			Player human = this.owner.World.Participants[person];
			ushort pictureName = (ushort)(this.processor.RegFrame.Value16 - 0x10);

			this.owner.Encyclopedia.ObtainFirstPrerequisite((ushort)human.PresentInquiryIdx);

			short first = (short)this.processor.RegAcc.Value16;
			bool picture = human.PresentInquiryIdx != 71 && (short)FirstMissingDisk > 3 &&
				this.owner.Startup.QueryForDisk(1, 0) != 0 && (this.owner.World.SessionPrefMarkers & 8) != 0;

			bool advanced = false;

			if (picture)
			{
				this.owner.Startup.ConcealMouseCursor();

				this.owner.Libc.CopyText(pictureName, "discovr1.pic");

				if (PlayerKnowsTechnology(person, 0x36) != 0)
				{
					this.processor.EmitGlobalByte((ushort)(pictureName + 7), 0x32);
					advanced = true;
				}

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, pictureName, 0);

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

				this.owner.Helpers.HaltHueCycling();

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, pictureName, 1);

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}

			Message(CivilizationName((ushort)participantIdx))
				.Text(PlayerKnowsTechnology(person, 0x14) != 0 ? (ushort)0x22cc : (ushort)0x22d8)
				.Text("\ndiscover the secret\nof ")
				.Text(TechnologyText(human.PresentInquiryIdx));

			if (human.PresentInquiryIdx == 71)
			{
				this.owner.Words.Append(" ");

				human.ComingScienceTally++;

				this.owner.Words.Append(this.owner.Libc.IntegerToText(human.ComingScienceTally, 10));
			}

			this.owner.Words.Append("!\n");

			// The original plays only a short fanfare (table at 0x1518, one to five seconds),
			// the national anthem (table at 0x151a, as in the palace and the throne room) plays until the next research is chosen
			this.owner.Services.PerformSound(LeaderWord(this.owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			if (picture)
			{
				ShowDiscoveryPicture(pictureName, first, advanced);
			}
			else
			{
				this.owner.Paper.DisplayNewspaper(-1);
			}

			EarnTechnology(participantIdx, human.PresentInquiryIdx, participantIdx1);

			this.owner.Services.PerformSound(1, 0);

			human.PresentInquiryIdx = -1;
		}

		/// <summary>The discovery message on the picture and the icon of the first prerequisite (iconpgN.pic, N = first / 9)</summary>
		private void ShowDiscoveryPicture(ushort pictureName, short first, bool advanced)
		{
			if (!advanced)
				DrawingBox.TypefaceIdx = 0x6;

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 96, advanced ? (short)25 : (short)1, 1);

			DrawingBox.TypefaceIdx = 0x1;

			this.owner.Words.Set("iconpg1.pic");

			this.owner.Words[6] = (byte)(this.owner.Words[6] + CPU.SignedQuotient(first, 9));

			this.owner.Startup.QueryForDisk(3, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, TextBuffer.Address, 0);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.Encyclopedia.RenderIcon((ushort)(((first % 3) * 111) + 1), (ushort)((((first % 9) / 3) * 69) + 1), 110, 68, 174, advanced ? (ushort)87 : (ushort)96);

			if (VgaMode != 0)
			{
				this.owner.Libc.CopyText((ushort)(pictureName + 8), ".pal");

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, pictureName, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x2311, 0xc5be);

				this.owner.Libc.CopyBytes(0xc5c4, 0xc1dc, 0xff);

				this.owner.Services.ImportColorsFromBuffer(30, 0xc5be);
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Helpers.LaunchHueCycling();
		}

		/// <summary>The science advisor recommends the technology with the units and wonders it allows</summary>
		private void RecommendResearch(short pick)
		{
			Message("Science Advisor:\nI recommend we\ndevelop ")
				.Text(TechnologyText(pick))
				.Text(".\n");

			ushort length = this.owner.Words.Length;

			ushort allowed = 0;

			this.owner.Words.Append("So we can build:\n-");

			for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
			{
				ushort kindRecord = (ushort)(0x22 * kindIdx);
				this.processor.RegSource.Value16 = kindRecord;

				if (UnitKindWordAt(kindRecord, UnitKindTable.Technology) != (ushort)pick)
					continue;

				this.owner.Words.Append(UnitTextAt(kindRecord));

				this.owner.Words.Append("\n-");

				allowed++;
			}

			for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
			{
				ushort wonderRecord = (ushort)(0x1e * wonderIdx);
				this.processor.RegSource.Value16 = wonderRecord;

				if (BuildingWordAt(wonderRecord, BuildingTable.Technology) != (ushort)pick)
					continue;

				this.owner.Words.Append(BuildingRecordTextAt(wonderRecord));

				this.owner.Words.Append("\n-");

				allowed++;
			}

			if (allowed == 0)
				this.processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0);

			DrawingBox.TypefaceIdx = 0x2;
			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(TextBuffer.Address, 8, (short)-((allowed << 3) - 0xa8), 1);

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
				.Text(PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0 ? (ushort)0x2388 : (ushort)0x2393)
				.Text(" be pursuing, sire?\nPick one...\n ");

			ushort choices = 0;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (!MayResearch(participantIdx, scienceIdx))
					continue;

				if (!again)
				{
					if ((short)choices > 1 && (short)(scienceIdx - this.owner.World.Participants[participantIdx].FoundScienceTally) % 3 == 0 &&
						this.owner.World.HardnessTier != 0)
						continue;
				}
				else
				{
					this.processor.RegSource.Value16 = (ushort)(choices << 1);

					if (choiceList[choices] != (ushort)scienceIdx)
						continue;
				}

				this.owner.Words.Append(TechnologyText(scienceIdx));

				this.owner.Words.Append("\n ");

				this.processor.RegSource.Value16 = (ushort)(choices << 1);
				choiceList[choices] = (ushort)scienceIdx;
				choices++;
			}

			return choices;
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

			// The frame stays for the depth of the calls (B3: the locals are C# variables)
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

			EmptyMessage()
				.Text(CivilizationPlural((ushort)participantIdx))
				.Text(participantIdx == participantIdx1 || participantIdx1 == 0 ? (ushort)0x23d7 : (ushort)0x23e1)
				.Text(TechnologyText(scienceIdx));

			if (TechnologyNewsRules.NamesSource(participantIdx, participantIdx1, this.owner.TurnHooks.ClassicTechnologyNews))
			{
				this.owner.Words.Append(" from\nthe ");

				if (participantIdx1 == TechnologyNewsRules.GreatLibrary)
				{
					this.owner.Words.Append("Great Library");
				}
				else
				{
					this.owner.Words.Append(CivilizationPlural((ushort)participantIdx1));
				}
			}

			this.owner.Words.Append("!\n ");

			if (participantIdx != person)
			{
				// The wonders (0xbb6, names 0xb9a) and the units (0x114a, names 0x112a) the technology allows
				for (ushort wonderIdx = 0; wonderIdx < 0x18; wonderIdx++)
				{
					ushort wonderRecord = (ushort)(0x1e * wonderIdx);
					this.processor.RegSource.Value16 = wonderRecord;

					if (BuildingWordAt(wonderRecord, BuildingTable.Technology) == (ushort)scienceIdx)
					{
						this.owner.Words.Append(BuildingRecordTextAt(wonderRecord));

						this.owner.Words.Append("\n ");
					}
				}

				for (ushort kindIdx = 0; kindIdx < 0x1c; kindIdx++)
				{
					ushort kindRecord = (ushort)(0x22 * kindIdx);
					this.processor.RegSource.Value16 = kindRecord;

					if (UnitKindWordAt(kindRecord, UnitKindTable.Technology) == (ushort)scienceIdx)
					{
						this.owner.Words.Append(UnitTextAt(kindRecord));

						this.owner.Words.Append("\n ");
					}
				}
			}

			if ((participantIdx != person || participantIdx1 == -2) && KingAnswer == 0xffff)
			{
				MenuAnyKey = 1;

				DialogStyle = participantIdx == person ? (ushort)6 : (ushort)0;

				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);
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

			if (this.owner.Audience.WelcomeEmissary(participantIdx, -1, 0) == 0xffff)
				return;

			KingAnswer = 0;
			EmissaryAudience = 0;
			this.owner.Words.Clear();

			this.owner.Libc.CopyText(CodeText1, CivilizationPlural((ushort)participantIdx));

			this.owner.Libc.CopyText(CodeText4, TechnologyText(scienceIdx));

			this.owner.BlockText.DisplayTextDialog(0x240e);

			KingMood = 0x2;
			KingSpeaking = 0xc;

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x200b); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
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

				if (KingAnswer == 0xffff)
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
				this.processor.RegDest.Value16 = (ushort)(((ushort)person << 4) + ((ushort)(participantIdx << 1)));
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

				if (participantIdx == person && KingAnswer == 0xffff)
				{
					Message("Development of ")
						.Text(TechnologyText(scienceIdx))
						.Text("\nmakes existing Barracks\nobsolete.\n")
						.Style(0x3)
						.Dialog(80, 100);
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
						!WonderCityRules.ReadsCity(this.owner.World.MarvelTownIdx[wonderIdx], this.owner.TurnHooks.ClassicWonderCities) ||
						this.owner.World.Towns[this.owner.World.MarvelTownIdx[wonderIdx]].ParticipantIdx != person)
						continue;

					Message(TechnologyText(scienceIdx))
						.Text(" cancels\nthe effect of\n")
						.Text(WonderText(wonderIdx))
						.Text(".\n");

					this.owner.Paper.DisplayNewspaper(-1);
				}
			}

			// The Great Library (wonder 5) gives a technology that two players know
			ushort libraryOwner = (ushort)this.owner.World.MarvelTownIdx[5];

			if (libraryOwner == 0xffff || !WonderCityRules.ReadsCity(libraryOwner, this.owner.TurnHooks.ClassicWonderCities))
				return;

			libraryOwner = (ushort)this.owner.World.Towns[libraryOwner].ParticipantIdx;

			if (PlayerKnowsTechnology((short)libraryOwner, scienceIdx) != 0)
				return;

			if (this.owner.CityOps.PlayerEnjoysWonderEffect((short)libraryOwner, 5) == 0)
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
			short leaderFactor = (short)LeaderWord(leaderVisuals, LeaderTable.Leaning);
			ushort result = ScienceRules.CalculateTechnologyInfo(b0, b1, leaderFactor);
			this.processor.RegAcc.Value16 = result;

			this.processor.Journal.CloseScope("ObtainTechnologyInfo");
			return result;
		}
	}
}
