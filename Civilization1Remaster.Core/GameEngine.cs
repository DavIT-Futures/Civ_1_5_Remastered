using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Civilization1Remaster.Core
{
	public partial class GameEngine
	{
		private const ushort StartupExecutablePathAddress = 0x6156;
		private const ushort StartupDriveFlagAddress = 0x61ee;
		private const ushort StartupStackLimitAddress = 0x5890;
		private const ushort StartupStackBaseAddress = 0x588c;
		private const ushort StartupFreeMemoryAddress = 0x588a;
		private const ushort StartupDataSegmentAddress = 0x5901;
		private const ushort DosVersionAddress = 0x5903;
		private const ushort DosDriveInfoAddress = 0x591e;
		private const ushort DosArgumentBlockAddress = 0x5920;
		private const ushort DosEnvironmentBlockAddress = 0x5922;

		private readonly CPU processor;
		private readonly GameLauncher sessionBoot;
		private readonly PlatformServices machineDuties;
		private readonly WorldEvents roundHappenings;
		private readonly DialogPainter promptPainting;
		private readonly TurnController participantRound;
		private readonly GameHelpers sharedHelpers;
		private readonly TextPainter wordsPainting;
		private readonly PictureLoader visualUtils;
		private readonly TextBlockPainter wordsChunkPainter;
		private readonly MapService chartControl;
		private readonly UnitRoster troopControl;
		private readonly RoutePlanner routing;
		private readonly CityCapture commerceWithCapture;
		private readonly ComputerPlayer botParticipant;
		private readonly ResearchOffice researchWithCounselors;
		private readonly Cities townControl;
		private readonly Battles troopWarfare;
		private readonly ForeignAffairs regimeWithRelations;
		private readonly MenuBar sessionLists;
		private readonly IntroSequence primaryPrologue;
		private readonly RulerAudience greetUsingMonarch;
		private readonly WorldBuilder sessionSetupWithPrologue;
		private readonly HelpPopups insideSessionGuide;
		private readonly SaveGameStore sessionFetchWithStore;
		private readonly FameRecords chamberForGlory;
		private readonly NewGameSetup beginSessionChoices;
		private readonly CityNamingScreen freshTownDisplay;
		private readonly AdvisorScreens counselorBriefings;
		private readonly Encyclopedia encyclopedia;
		private readonly Gazette gazette;
		private readonly CityPanorama townScene;
		private readonly SpaceshipBay starshipDisplay;
		private readonly DiplomatMissions envoyDeeds;
		private readonly ReplayViewer sessionRerun;
		private readonly RivalOverview realmDetails;
		private readonly WorldStatistics globeChart;
		private readonly SettlementAndScoring townSettlingWithTally;
		private readonly PalaceBuilder castle;
		private readonly UnitPicker forceChoice;
		private readonly CivilWar realmSplit;
		private readonly RuntimeLibrary chrLibc;
		private readonly TextBuffer words;
		private readonly CodeTexts codes;
		private readonly NameTexts names;
		private readonly GameTables tables;
		private readonly GameGlobals globals = new GameGlobals();
		private readonly ScreenBoxes boxes = new ScreenBoxes();
		private readonly HueCycles hueCycles;
		private readonly TimerState timer;
		private readonly PointerState pointer = new PointerState();
		private readonly VGADriver videoHandler;
		private readonly AudioOutput audioHandler;
		private readonly CallJournal journal;
		private readonly CallJournal signalJournal;
		private readonly CallJournal videoHandlerJournal;
		private readonly CallJournal prologueJournal;
		private readonly ushort beginSection = 0x1000;
		private readonly GameState sessionCondition;
		private readonly IGameHost host;

		public GameEngine(IGameHost host)
		{
			this.host = host;
			this.journal = new CallJournal("Log.txt");
			this.signalJournal = new CallJournal("InterruptLog.txt");
			this.videoHandlerJournal = new CallJournal("VGADriverLog.txt");
			this.prologueJournal = new CallJournal("IntroLog.txt");

			this.processor = new CPU(this, this.journal);

			this.journal.Probe = this.processor;
			this.signalJournal.Probe = this.processor;
			this.videoHandlerJournal.Probe = this.processor;
			this.prologueJournal.Probe = this.processor;

			this.sessionCondition = new GameState();

			this.chrLibc = new RuntimeLibrary(this.processor, this);
			this.words = new TextBuffer(this);
			this.codes = new CodeTexts(this);
			this.names = new NameTexts(this);
			this.tables = new GameTables(this);
			this.hueCycles = new HueCycles(this.processor);
			this.timer = new TimerState(this.processor);
			this.videoHandler = new VGADriver(this.processor, this.videoHandlerJournal, host, LoadFonts());
			this.audioHandler = new AudioOutput(() => this.processor.StandardFolder);

			this.sessionBoot = new GameLauncher(this);
			this.machineDuties = new PlatformServices(this);
			this.roundHappenings = new WorldEvents(this);
			this.promptPainting = new DialogPainter(this);
			this.participantRound = new TurnController(this);
			this.sharedHelpers = new GameHelpers(this);
			this.wordsPainting = new TextPainter(this);
			this.visualUtils = new PictureLoader(this);
			this.wordsChunkPainter = new TextBlockPainter(this);
			this.chartControl = new MapService(this);
			this.troopControl = new UnitRoster(this);
			this.routing = new RoutePlanner(this);
			this.commerceWithCapture = new CityCapture(this);
			this.botParticipant = new ComputerPlayer(this);
			this.researchWithCounselors = new ResearchOffice(this);
			this.townControl = new Cities(this);
			this.troopWarfare = new Battles(this);
			this.regimeWithRelations = new ForeignAffairs(this);
			this.sessionLists = new MenuBar(this);
			this.primaryPrologue = new IntroSequence(this);
			this.greetUsingMonarch = new RulerAudience(this);
			this.sessionSetupWithPrologue = new WorldBuilder(this);
			this.insideSessionGuide = new HelpPopups(this);
			this.sessionFetchWithStore = new SaveGameStore(this);
			this.chamberForGlory = new FameRecords(this);
			this.beginSessionChoices = new NewGameSetup(this);
			this.freshTownDisplay = new CityNamingScreen(this);
			this.counselorBriefings = new AdvisorScreens(this);
			this.encyclopedia = new Encyclopedia(this);
			this.gazette = new Gazette(this);
			this.townScene = new CityPanorama(this);
			this.starshipDisplay = new SpaceshipBay(this);
			this.envoyDeeds = new DiplomatMissions(this);
			this.sessionRerun = new ReplayViewer(this);
			this.realmDetails = new RivalOverview(this);
			this.globeChart = new WorldStatistics(this);
			this.townSettlingWithTally = new SettlementAndScoring(this);
			this.castle = new PalaceBuilder(this);
			this.forceChoice = new UnitPicker(this);
			this.realmSplit = new CivilWar(this);
		}

		public CPU Processor
		{
			get { return this.processor; }
		}

		public GameState World
		{
			get { return this.sessionCondition; }
		}

		public CallJournal Journal
		{
			get { return this.journal; }
		}

		public CallJournal IrqJournal
		{
			get { return this.signalJournal; }
		}

		public CallJournal VideoJournal
		{
			get { return this.videoHandlerJournal; }
		}

		public CallJournal PrologueJournal
		{
			get { return this.prologueJournal; }
		}

		public IGameLauncher Startup
		{
			get { return this.sessionBoot; }
		}

		public IPlatformServices Services
		{
			get { return this.machineDuties; }
		}

		public IWorldEvents TurnHooks
		{
			get { return this.roundHappenings; }
		}

		public IDialogPainter Dialogs
		{
			get { return this.promptPainting; }
		}

		public ITurnController TurnLogic
		{
			get { return this.participantRound; }
		}

		public IGameHelpers Helpers
		{
			get { return this.sharedHelpers; }
		}

		public ITextPainter TextPainter
		{
			get { return this.wordsPainting; }
		}

		public IPictureLoader ImageToolkit
		{
			get { return this.visualUtils; }
		}

		public ITextBlockPainter BlockText
		{
			get { return this.wordsChunkPainter; }
		}

		public IMapService MapOps
		{
			get { return this.chartControl; }
		}

		public IUnitRoster UnitOps
		{
			get { return this.troopControl; }
		}

		public IRoutePlanner Routing
		{
			get { return this.routing; }
		}

		public ICityCapture Conquest
		{
			get { return this.commerceWithCapture; }
		}

		public IComputerPlayer Bot
		{
			get { return this.botParticipant; }
		}

		public IResearchOffice Advisors
		{
			get { return this.researchWithCounselors; }
		}

		public ICities CityOps
		{
			get { return this.townControl; }
		}

		public IBattles Combat
		{
			get { return this.troopWarfare; }
		}

		public IForeignAffairs Politics
		{
			get { return this.regimeWithRelations; }
		}

		public IMenuBar MenuSystem
		{
			get { return this.sessionLists; }
		}

		public IIntroSequence Prologue
		{
			get { return this.primaryPrologue; }
		}

		public IRulerAudience Audience
		{
			get { return this.greetUsingMonarch; }
		}

		public IWorldBuilder WorldSetup
		{
			get { return this.sessionSetupWithPrologue; }
		}

		public IHelpPopups HelpSystem
		{
			get { return this.insideSessionGuide; }
		}

		public ISaveGameStore SaveLoad
		{
			get { return this.sessionFetchWithStore; }
		}

		public IFameRecords Legends
		{
			get { return this.chamberForGlory; }
		}

		public INewGameSetup NewGameWizard
		{
			get { return this.beginSessionChoices; }
		}

		public ICityNamingScreen CityNaming
		{
			get { return this.freshTownDisplay; }
		}

		public IAdvisorScreens Briefings
		{
			get { return this.counselorBriefings; }
		}

		public IEncyclopedia Encyclopedia
		{
			get { return this.encyclopedia; }
		}

		public IGazette Paper
		{
			get { return this.gazette; }
		}

		public ICityPanorama CityScene
		{
			get { return this.townScene; }
		}

		public ISpaceshipBay RocketScreen
		{
			get { return this.starshipDisplay; }
		}

		public IDiplomatMissions Diplomats
		{
			get { return this.envoyDeeds; }
		}

		public IReplayViewer Replayer
		{
			get { return this.sessionRerun; }
		}

		public IRivalOverview RealmInfo
		{
			get { return this.realmDetails; }
		}

		public IWorldStatistics Atlas
		{
			get { return this.globeChart; }
		}

		public ISettlementAndScoring Founding
		{
			get { return this.townSettlingWithTally; }
		}

		public IPalaceBuilder Throne
		{
			get { return this.castle; }
		}

		public IUnitPicker ArmyPicker
		{
			get { return this.forceChoice; }
		}

		public ICivilWar Secession
		{
			get { return this.realmSplit; }
		}

		public IRuntimeLibrary Libc
		{
			get { return this.chrLibc; }
		}

		public TimerState Timer
		{
			get { return this.timer; }
		}

		public PointerState Pointer
		{
			get { return this.pointer; }
		}

		public HueCycles HueCycles
		{
			get { return this.hueCycles; }
		}

		public GameTables Tables
		{
			get { return this.tables; }
		}

		public GameGlobals Globals
		{
			get { return this.globals; }
		}

		public TextBuffer Words
		{
			get { return this.words; }
		}

		public CodeTexts Codes
		{
			get { return this.codes; }
		}

		public NameTexts Names
		{
			get { return this.names; }
		}

		public VGADriver VideoCard
		{
			get { return this.videoHandler; }
		}

		public IAudioOutput Audio
		{
			get { return this.audioHandler; }
		}

		public void Launch()
		{
			PrepareMachine();

			this.Startup.RunGame();

			this.Libc.TerminateProgram(0);
		}

		private Dictionary<int, CivFont> LoadFonts()
		{
			byte[]? typefaces = (byte[]?)Properties.Resources.ResourceManager.GetObject("Fonts_xml");

			if (typefaces == null)
			{
				this.host?.ShowMessage("Civilization1Remaster resource Fonts is missing.", "Resource missing");
				this.processor.Quit(-1);

				throw new Exception("Embedded resource missing");
			}

			using Stream source = new GZipStream(new MemoryStream(typefaces), CompressionMode.Decompress);

			return CivFonts.Unpack(source);
		}

		public void PrepareMachine()
		{
			ushort startingStackseg = 0x398d;
			ushort startingStackptr = 0x0800;

			this.processor.Storage.ReserveStorageChunk(0xff00, 0x100, CPUMemoryFlags.ReadWrite);
			this.processor.Storage.EmitUInt16(0xff00, 0x20cd);
			this.processor.Storage.EmitUInt16(0xff02, (ushort)(this.processor.Storage.UnusedStorage.Finish >> 4));
			this.processor.Storage.EmitUInt8(0xff81, (byte)'\r');
			this.processor.Storage.StorageZones[1].PermissionMarkers = CPUMemoryFlags.ReadWrite | CPUMemoryFlags.WriteWarning | CPUMemoryFlags.ReadWarning;

			uint programBegin = CPU.AsLinearAddress(beginSection, 0);
			uint programSpan = 0x3a0e0;

			byte[]? assets = (byte[]?)Properties.Resources.ResourceManager.GetObject("BinaryResources");

			if (assets == null)
			{
				this.host?.ShowMessage("Civilization1Remaster resource BinaryResources is missing.", "Resource missing");
				this.processor.Quit(-1);

				throw new Exception("Embedded resource missing");
			}

			this.processor.Storage.ReserveStorageChunk(programBegin, programSpan, CPUMemoryFlags.ReadWrite);
			this.processor.Storage.EmitChunk(CPU.AsLinearAddress(0x35cf, 0), assets, 0, assets.Length);
			this.processor.Storage.StorageZones[2].PermissionMarkers |= CPUMemoryFlags.WriteWarning;

			uint payloadOrigin = programBegin + CPU.AsLinearAddress(0x25cf, 0);
			uint contentFinish = programBegin + CPU.AsLinearAddress(0x2b01, 0xf0c0);

			this.processor.Storage.StorageZones[2].Finish = payloadOrigin - 1;
			this.processor.Storage.StorageZones.Add(
				new CPUMemoryRegion(payloadOrigin, (contentFinish - payloadOrigin) + 1, CPUMemoryFlags.ReadWrite));

			this.processor.SegStack.Value16 = (ushort)(startingStackseg + beginSection);
			this.processor.SegData.Value16 = (ushort)(beginSection - 0x10);
			this.processor.SegExtra.Value16 = (ushort)(beginSection - 0x10);
			this.processor.RegStack.Value16 = startingStackptr;

			ushort contentSection = 0x3b01;

			this.processor.StackWord(this.processor.SegData.Value16);

			this.processor.SegData.Value16 = 0x3b01;

			if (!string.IsNullOrEmpty(this.processor.StandardFolder) && !Directory.Exists(this.processor.StandardFolder))
			{
				this.host?.ShowMessage($"Civilization1Remaster resource files not found at '{this.processor.StandardFolder}'.\n" +
					"The Civilization1Remaster depends on Civilization resource files (*.pic, *.pal and *.txt).\nPlease adjust path to these resources.",
					"Resource path error");
				this.processor.Quit(-1);
			}

			string[] assetDocuments = GameFolderCheck.RequiredFiles;

			for (int idx = 0; idx < assetDocuments.Length; idx++)
			{
				string docfileTrail = Path.Combine(this.processor.StandardFolder, assetDocuments[idx].ToUpper());

				while (!File.Exists(docfileTrail))
				{
					HostDialogResult outcome = this.host != null
						? this.host.ShowRetryCancel($"Missing resource file {docfileTrail}. Plsease ensure that the file exists at specified path.\n" +
						   "Some File systems are case sensitive, ensure that the file name is in uppercase.", "Error")
						: HostDialogResult.Cancel;

					if (outcome == HostDialogResult.Cancel)
					{
						this.processor.Quit(-1);
						break;
					}
				}
			}

			string trail = Path.Combine(this.processor.StandardFolder, "CIV.EXE");

			this.processor.EmitGlobalByte(StartupDriveFlagAddress, (byte)'C');
			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, StartupExecutablePathAddress), trail, trail.Length);

			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;

			ushort topSection = (ushort)(this.processor.Storage.UnusedStorage.Finish >> 4);

			this.processor.DisableInterrupts();
			this.processor.SegStack.Value16 = contentSection;
			this.processor.RegStack.Value16 = (ushort)(this.processor.RegStack.Value16 + 0xe8c0);
			this.processor.EnableInterrupts();

			this.processor.RegStack.Value16 &= 0xfffe;

			this.processor.EmitUInt16(contentSection, StartupStackLimitAddress, this.processor.RegStack.Value16);
			this.processor.EmitUInt16(contentSection, StartupStackBaseAddress, this.processor.RegStack.Value16);
			this.processor.EmitUInt16(contentSection, StartupFreeMemoryAddress, (ushort)(((topSection - contentSection) << 4) - 1));

			ushort programSection = this.processor.SegExtra.Value16;
			ushort blockSize = (ushort)(topSection - programSection);

			this.processor.Storage.RescaleStorageChunk(programSection, blockSize);

			this.processor.EmitUInt16(contentSection, StartupDataSegmentAddress, this.processor.SegData.Value16);

			this.processor.SegExtra.Value16 = contentSection;
			this.processor.SegData.Value16 = contentSection;

			ForbidAccess(0x3604, 0, 0xd00);
			ForbidAccess(0x3772, 0x4b0, 0x90);
			ForbidAccess(0x3772, 0x540, 0x100);
			ForbidAccess(0x3772, 0x640, 0x100);
			ForbidAccess(0x3772, 0x740, 0xfa0);
			ForbidAccess(0x3772, 0x1f88, 0x4b0);
			ForbidAccess(0x3772, 0x253c, 0x1c0);
			ForbidAccess(0x3772, 0x26fc, 0x1000);
			ForbidAccess(0x3b01, 0x19be, 2);
			ForbidAccess(0x3b01, 0x19c0, 2);
			ForbidAccess(0x3b01, 0x1c20, 2);
			ForbidAccess(0x3b01, 0x1c26, 2);
			ForbidAccess(0x3b01, 0x40dc, 2);
			ForbidAccess(0x3b01, 0x4cd4, 2);
			ForbidAccess(0x3b01, 0x4cd6, 2);
			ForbidAccess(0x3b01, 0x4cd8, 2);
			ForbidAccess(0x3b01, 0x6b66, 0x10);
			ForbidAccess(0x3b01, 0x6b76, 0x10);
			ForbidAccess(0x3b01, 0x6b86, 2);
			ForbidAccess(0x3b01, 0x6b8a, 2);
			ForbidAccess(0x3b01, 0x6b96, 0x10);
			ForbidAccess(0x3b01, 0x6c9c, 0x10);
			ForbidAccess(0x3b01, 0x6cac, 0x100);
			ForbidAccess(0x3b01, 0x6dec, 0x10);
			ForbidAccess(0x3b01, 0x6e80, 2);
			ForbidAccess(0x3b01, 0x6e82, 0x10);
			ForbidAccess(0x3b01, 0x6e94, 2);
			ForbidAccess(0x3b01, 0x6ea8, 0x2c);
			ForbidAccess(0x3b01, 0x70ec, 0xe00);
			ForbidAccess(0x3b01, 0x7f36, 2);
			ForbidAccess(0x3b01, 0x803c, 0x10);
			ForbidAccess(0x3b01, 0x804c, 2);
			ForbidAccess(0x3b01, 0x8068, 0x10);
			ForbidAccess(0x3b01, 0x807a, 0x100);
			ForbidAccess(0x3b01, 0x81d2, 2);
			ForbidAccess(0x3b01, 0xb1d6, 0x10);
			ForbidAccess(0x3b01, 0xb1ea, 2);
			ForbidAccess(0x3b01, 0xb1ee, 0x20);
			ForbidAccess(0x3b01, 0xb210, 0x10);
			ForbidAccess(0x3b01, 0xb220, 2);
			ForbidAccess(0x3b01, 0xb23a, 2);
			ForbidAccess(0x3b01, 0xb3be, 0x1c0);
			ForbidAccess(0x3b01, 0xd2e4, 0x10);
			ForbidAccess(0x3b01, 0xd2f4, 2);
			ForbidAccess(0x3b01, 0xd2f8, 0x10);
			ForbidAccess(0x3b01, 0xd308, 0x1c0);
			ForbidAccess(0x3b01, 0xd7ee, 2);
			ForbidAccess(0x3b01, 0xd7f4, 0x10);
			ForbidAccess(0x3b01, 0xd76e, 0x80);
			ForbidAccess(0x3b01, 0xd808, 2);
			ForbidAccess(0x3b01, 0xdb36, 2);
			ForbidAccess(0x3b01, 0xdc6c, 0x80);
			ForbidAccess(0x3b01, 0xdcec, 0x10);
			ForbidAccess(0x3b01, 0xddfe, 0x10);
			ForbidAccess(0x3b01, 0xde10, 0x10);
			ForbidAccess(0x3b01, 0xde22, 0x80);
			ForbidAccess(0x3b01, 0xdeec, 0x10);
			ForbidAccess(0x3b01, 0xdefc, 0x10);
			ForbidAccess(0x3b01, 0xdf0e, 0x10);
			ForbidAccess(0x3b01, 0xdf1e, 2);
			ForbidAccess(0x3b01, 0xe16a, 0x10);
			ForbidAccess(0x3b01, 0xe3c4, 2);
			ForbidAccess(0x3b01, 0xe3c8, 0x50);
			ForbidAccess(0x3b01, 0xe418, 0x80);
			ForbidAccess(0x3b01, 0xe498, 0x80);
			ForbidAccess(0x3b01, 0xe698, 0x10);
			ForbidAccess(0x3b01, 0xe898, 0x10);
			ForbidAccess(0x3b01, 0xe8a8, 0x10);

			this.processor.EmitGlobalWord(DosVersionAddress, 0x616);

			this.processor.EmitGlobalWord(DosEnvironmentBlockAddress, 0);
			this.processor.EmitGlobalWord(DosArgumentBlockAddress, 0);
			this.processor.EmitGlobalWord(DosDriveInfoAddress, 0);

			this.processor.RegFrame.Value16 = 0x0;

			this.words.Attach();
			this.codes.Attach();
			this.names.Attach();

			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, UnitMemory.Origin),
				new byte[UnitMemory.ParticipantStride * 8]);

			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, GameTables.Begin), this.tables.Content);

			for (int boxIdx = 0; boxIdx < ScreenBoxes.Addresses.Length; boxIdx++)
				this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, ScreenBoxes.Addresses[boxIdx]), this.boxes.Content[boxIdx]);

			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(HueCycles.Segment, 0), this.hueCycles.Content);

			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, TimerState.Begin), this.timer.Content);
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, PointerState.PlaceAddress), this.pointer.Place);
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, PointerState.ShiftAddress), this.pointer.Shift);
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, PointerState.CursorAddress), this.pointer.Cursor);

			foreach (GlobalWord global in this.globals.All)
				this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, global.Address), global);
		}

		private void ForbidAccess(ushort section, ushort displacement, uint extent)
		{
			this.processor.Storage.StorageZones.Add(new CPUMemoryRegion(section, displacement, extent, CPUMemoryFlags.AccessNotAllowed));
		}
	}
}
