using System;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster
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

		#region Segment definitions
		private readonly GameStartup sessionBoot;
		private readonly SystemServices machineDuties;
		private readonly TurnEvents roundHappenings;
		private readonly DialogDrawing promptPainting;
		private readonly PlayerTurn participantRound;
		private readonly CommonUtilities sharedHelpers;
		private readonly TextDrawing wordsPainting;
		private readonly ImageTools visualUtils;
		private readonly TextBlockRenderer wordsChunkPainter;
		private readonly MapManagement chartControl;
		private readonly UnitManagement troopControl;
		private readonly Pathfinding routing;
		private readonly TradeAndConquest commerceWithCapture;
		private readonly AIPlayer botParticipant;
		private readonly ScienceAndAdvisors researchWithCounselors;
		private readonly CityManagement townControl;
		private readonly UnitCombat troopWarfare;
		private readonly GovernmentAndDiplomacy regimeWithRelations;
		private readonly GameMenus sessionLists;
		private readonly MainIntro primaryPrologue;
		private readonly MeetWithKing greetUsingMonarch;
		private readonly GameInitAndIntro sessionSetupWithPrologue;
		private readonly InGameHelp insideSessionGuide;
		private readonly GameLoadAndSave sessionFetchWithStore;
		private readonly HallOfFame chamberForGlory;
		private readonly StartGameMenu beginSessionChoices;
		private readonly NewCityScreen freshTownDisplay;
		private readonly AdvisorReports counselorBriefings;
		private readonly Civilopedia encyclopedia;
		private readonly Newspaper gazette;
		private readonly CityView townScene;
		private readonly SpaceshipScreen starshipDisplay;
		private readonly DiplomatActions envoyDeeds;
		private readonly GameReplay sessionRerun;
		private readonly CivilizationInfo realmDetails;
		private readonly WorldMap globeChart;
		private readonly CityFoundingAndScore townSettlingWithTally;
		private readonly Palace castle;
		private readonly ArmySelection forceChoice;
		private readonly CivilizationSchism realmSplit;
		private readonly CRuntime chrLibc;
		private readonly TextBuffer words;
		private readonly GameTables tables;
		private readonly GameGlobals globals = new GameGlobals();
		private readonly ScreenBoxes boxes = new ScreenBoxes();
		private readonly HueCycles hueCycles;
		private readonly TimerState timer;
		private readonly PointerState pointer = new PointerState();
		private readonly VGADriver videoHandler;
		private readonly SoundDriver audioHandler;
		#endregion

		private readonly LogWrapper journal;
		private readonly LogWrapper signalJournal;
		private readonly LogWrapper videoHandlerJournal;
		private readonly LogWrapper prologueJournal;

		private readonly ushort beginSection = 0x1000;

		private readonly GameState sessionCondition;
		private readonly IGameHost? host;

		public GameEngine(IGameHost? host)
		{
			this.host = host;
			this.journal = new LogWrapper("Log.txt");
			this.signalJournal = new LogWrapper("InterruptLog.txt");
			this.videoHandlerJournal = new LogWrapper("VGADriverLog.txt");
			this.prologueJournal = new LogWrapper("IntroLog.txt");

			this.processor = new CPU(this, this.journal);

			this.journal.Probe = this.processor;
			this.signalJournal.Probe = this.processor;
			this.videoHandlerJournal.Probe = this.processor;
			this.prologueJournal.Probe = this.processor;

			this.sessionCondition = new GameState();

			#region Initialize Segments
			this.chrLibc = new CRuntime(this);
			this.words = new TextBuffer(this);
			this.tables = new GameTables(this);
			this.hueCycles = new HueCycles(this.processor);
			this.timer = new TimerState(this.processor);
			this.videoHandler = new VGADriver(this, host);
			this.audioHandler = new SoundDriver(this);

			this.sessionBoot = new GameStartup(this);
			this.machineDuties = new SystemServices(this);
			this.roundHappenings = new TurnEvents(this);
			this.promptPainting = new DialogDrawing(this);
			this.participantRound = new PlayerTurn(this);
			this.sharedHelpers = new CommonUtilities(this);
			this.wordsPainting = new TextDrawing(this);
			this.visualUtils = new ImageTools(this);
			this.wordsChunkPainter = new TextBlockRenderer(this);
			this.chartControl = new MapManagement(this);
			this.troopControl = new UnitManagement(this);
			this.routing = new Pathfinding(this);
			this.commerceWithCapture = new TradeAndConquest(this);
			this.botParticipant = new AIPlayer(this);
			this.researchWithCounselors = new ScienceAndAdvisors(this);
			this.townControl = new CityManagement(this);
			this.troopWarfare = new UnitCombat(this);
			this.regimeWithRelations = new GovernmentAndDiplomacy(this);
			this.sessionLists = new GameMenus(this);
			this.primaryPrologue = new MainIntro(this);
			this.greetUsingMonarch = new MeetWithKing(this);
			this.sessionSetupWithPrologue = new GameInitAndIntro(this);
			this.insideSessionGuide = new InGameHelp(this);
			this.sessionFetchWithStore = new GameLoadAndSave(this);
			this.chamberForGlory = new HallOfFame(this);
			this.beginSessionChoices = new StartGameMenu(this);
			this.freshTownDisplay = new NewCityScreen(this);
			this.counselorBriefings = new AdvisorReports(this);
			this.encyclopedia = new Civilopedia(this);
			this.gazette = new Newspaper(this);
			this.townScene = new CityView(this);
			this.starshipDisplay = new SpaceshipScreen(this);
			this.envoyDeeds = new DiplomatActions(this);
			this.sessionRerun = new GameReplay(this);
			this.realmDetails = new CivilizationInfo(this);
			this.globeChart = new WorldMap(this);
			this.townSettlingWithTally = new CityFoundingAndScore(this);
			this.castle = new Palace(this);
			this.forceChoice = new ArmySelection(this);
			this.realmSplit = new CivilizationSchism(this);
			#endregion
		}

		/// <summary>Marks a part of the program's memory as untouchable, so that a stray access to it is caught while the game runs.</summary>
		private void ForbidAccess(ushort section, ushort displacement, uint extent)
		{
			this.processor.Storage.StorageZones.Add(new CPUMemoryRegion(section, displacement, extent, CPUMemoryFlagsEnum.AccessNotAllowed));
		}

		public void Launch()
		{
			PrepareMachine();

			// Call our 'short Main()' function
			this.Startup.RunGame();

			this.Libc.TerminateProgram((short)this.processor.RegAcc.Value16);
		}

		/// <summary>
		/// Sets up the memory, the registers and the data of the emulated program, everything before its main function runs.
		/// A host without a window (the tests) calls this and then the game functions it needs.
		/// </summary>
		public void PrepareMachine()
		{
			ushort startingStackseg = 0x398d; // oEXE.InitialSS;
			ushort startingStackptr = 0x0800; // oEXE.InitialSP;

			this.processor.Storage.ReserveStorageChunk(0xff00, 0x100, CPUMemoryFlagsEnum.ReadWrite);
			this.processor.Storage.EmitUInt16(0xff00, 0x20cd);
			this.processor.Storage.EmitUInt16(0xff02, (ushort)(this.processor.Storage.UnusedStorage.Finish >> 4));
			this.processor.Storage.EmitUInt8(0xff81, (byte)'\r');
			this.processor.Storage.StorageZones[1].PermissionMarkers = CPUMemoryFlagsEnum.ReadWrite | CPUMemoryFlagsEnum.WriteWarning | CPUMemoryFlagsEnum.ReadWarning;

			uint programBegin = CPU.AsLinearAddress(beginSection, 0);
			uint programSpan = 0x3a0e0;

			byte[]? assets = (byte[]?)Properties.Resources.ResourceManager.GetObject("BinaryResources");

			if (assets == null)
			{
				this.host?.ShowMessage("Civilization1Remaster resource BinaryResources is missing.", "Resource missing");
				this.processor.Quit(-1);

				throw new Exception("Embedded resource missing"); // Fool compiler as Exit already throws an exception
			}

			this.processor.Storage.ReserveStorageChunk(programBegin, programSpan, CPUMemoryFlagsEnum.ReadWrite);
			this.processor.Storage.EmitChunk(CPU.AsLinearAddress(0x35cf, 0), assets, 0, assets.Length);
			this.processor.Storage.StorageZones[2].PermissionMarkers |= CPUMemoryFlagsEnum.WriteWarning;

			// Define data and stack region
			uint payloadOrigin = programBegin + CPU.AsLinearAddress(0x25cf, 0);
			uint contentFinish = programBegin + CPU.AsLinearAddress(0x2b01, 0xf0c0);

			this.processor.Storage.StorageZones[2].Finish = payloadOrigin - 1;
			this.processor.Storage.StorageZones.Add(
				new CPUMemoryRegion(payloadOrigin, (contentFinish - payloadOrigin) + 1, CPUMemoryFlagsEnum.ReadWrite));

			// Initialize CPU
			this.processor.SegStack.Value16 = (ushort)(startingStackseg + beginSection);
			this.processor.SegData.Value16 = (ushort)(beginSection - 0x10);
			this.processor.SegExtra.Value16 = (ushort)(beginSection - 0x10);
			this.processor.RegStack.Value16 = startingStackptr;

			// Overlay segment, set by F0_3045_2b44

			ushort contentSection = 0x3b01;

			// function body
			this.processor.StackWord(this.processor.SegData.Value16);

			this.processor.SegData.Value16 = 0x3b01;

			// Check for Default directory and individual Resource files
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

			// Not important, but just for case it's still needed, to be removed later
			string trail = Path.Combine(this.processor.StandardFolder, "CIV.EXE");

			this.processor.EmitGlobalByte(StartupDriveFlagAddress, (byte)'C');
			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, StartupExecutablePathAddress), trail, trail.Length);

			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;

			// The start-up code of the program: the stack goes to the end of the data segment (SS:SP = 0x3b01:0xf0c0), the
			// stack limits (0x5890, 0x588c) and the size of the free memory above the data segment in bytes - 1 (0x588a) are
			// noted, the memory block of the program is resized up to the top of memory (DOS: CF / AX = 0 or 8, BX), the
			// program segment (0x5901), then DS = ES = SS. The registers stay as the start-up code leaves them.
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

			this.processor.RegSource.Value16 = topSection;
			this.processor.RegBase.Value16 = blockSize;

			bool resized = this.processor.Storage.RescaleStorageChunk(programSection, blockSize);

			this.processor.RegAcc.Value16 = (ushort)(resized ? 0 : 8);

			if (!resized)
			{
				this.processor.RegBase.Value16 = 0;
			}

			this.processor.EmitUInt16(contentSection, StartupDataSegmentAddress, this.processor.SegData.Value16);

			this.processor.SegExtra.Value16 = contentSection;
			this.processor.SegData.Value16 = contentSection;

			// The original clears the data and stack segment from 0x652e on except the block 0x70ec - 0x7eeb, but its
			// condition is never true, so nothing is cleared

			#region Protect already mapped data objects

			#region Strings
			// Skip checking of string as it slows down VCPU too much

			#endregion

			#region Objects
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
			#endregion

			#endregion

			// DOS version
			this.processor.EmitGlobalWord(DosVersionAddress, 0x616);

			// Environment block is not used
			// Argument block in not used
			this.processor.EmitGlobalWord(DosEnvironmentBlockAddress, 0);
			this.processor.EmitGlobalWord(DosArgumentBlockAddress, 0);
			this.processor.EmitGlobalWord(DosDriveInfoAddress, 0);

			// Get special devices information
			// Defaults are OK, no need to modify bytes at 0x590a - 0x590e

			this.processor.RegFrame.Value16 = 0x0;

			// B6.1: the message buffer lives in C# from now on
			this.words.Attach();

			// B6.2: the units too (UnitMemory reads and writes the array, the rest of the code through the memory)
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, UnitMemory.Origin),
				new byte[UnitMemory.ParticipantStride * 8]);

			// B6.3: the tables of CIV.EXE, B6.4: the named global variables
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, GameTables.Begin), this.tables.Content);

			// B6.5: the boxes of the screens
			for (int boxIdx = 0; boxIdx < ScreenBoxes.Addresses.Length; boxIdx++)
				this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, ScreenBoxes.Addresses[boxIdx]), this.boxes.Content[boxIdx]);

			// B6.5: the colour cycles of the driver
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(HueCycles.Segment, 0), this.hueCycles.Content);

			// B6.7, part 1: the variables of the timer and the state of the mouse
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, TimerState.Begin), this.timer.Content);
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, PointerState.PlaceAddress), this.pointer.Place);
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, PointerState.ShiftAddress), this.pointer.Shift);
			this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, PointerState.CursorAddress), this.pointer.Cursor);

			foreach (GlobalWord global in this.globals.All)
				this.processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.processor.SegData.Value16, global.Address), global);
		}

		public CPU Processor
		{
			get { return this.processor; }
		}

		public GameState World
		{
			get { return this.sessionCondition; }
		}

		#region Logs
		public LogWrapper Journal
		{
			get { return this.journal; }
		}

		public LogWrapper IrqJournal
		{
			get { return this.signalJournal; }
		}

		public LogWrapper VideoJournal
		{
			get { return this.videoHandlerJournal; }
		}

		public LogWrapper PrologueJournal
		{
			get { return this.prologueJournal; }
		}
		#endregion

		#region Public Segment getters
		public GameStartup Startup
		{
			get { return this.sessionBoot; }
		}

		public SystemServices Services
		{
			get { return this.machineDuties; }
		}

		public TurnEvents TurnHooks
		{
			get { return this.roundHappenings; }
		}

		public DialogDrawing Dialogs
		{
			get { return this.promptPainting; }
		}

		public PlayerTurn TurnLogic
		{
			get { return this.participantRound; }
		}

		public CommonUtilities Helpers
		{
			get { return this.sharedHelpers; }
		}

		public TextDrawing TextPainter
		{
			get { return this.wordsPainting; }
		}

		public ImageTools ImageToolkit
		{
			get { return this.visualUtils; }
		}

		public TextBlockRenderer BlockText
		{
			get { return this.wordsChunkPainter; }
		}

		public MapManagement MapOps
		{
			get { return this.chartControl; }
		}

		public UnitManagement UnitOps
		{
			get { return this.troopControl; }
		}

		public Pathfinding Routing
		{
			get { return this.routing; }
		}

		public TradeAndConquest Conquest
		{
			get { return this.commerceWithCapture; }
		}

		public AIPlayer Bot
		{
			get { return this.botParticipant; }
		}

		public ScienceAndAdvisors Advisors
		{
			get { return this.researchWithCounselors; }
		}

		public CityManagement CityOps
		{
			get { return this.townControl; }
		}

		public UnitCombat Combat
		{
			get { return this.troopWarfare; }
		}

		public GovernmentAndDiplomacy Politics
		{
			get { return this.regimeWithRelations; }
		}

		public GameMenus MenuSystem
		{
			get { return this.sessionLists; }
		}

		public MainIntro Prologue
		{
			get { return this.primaryPrologue; }
		}

		public MeetWithKing Audience
		{
			get { return this.greetUsingMonarch; }
		}

		public GameInitAndIntro WorldSetup
		{
			get { return this.sessionSetupWithPrologue; }
		}

		public InGameHelp HelpSystem
		{
			get { return this.insideSessionGuide; }
		}

		public GameLoadAndSave SaveLoad
		{
			get { return this.sessionFetchWithStore; }
		}

		public HallOfFame Legends
		{
			get { return this.chamberForGlory; }
		}

		public StartGameMenu NewGameWizard
		{
			get { return this.beginSessionChoices; }
		}

		public NewCityScreen CityNaming
		{
			get { return this.freshTownDisplay; }
		}

		public AdvisorReports Briefings
		{
			get { return this.counselorBriefings; }
		}

		public Civilopedia Encyclopedia
		{
			get { return this.encyclopedia; }
		}

		public Newspaper Paper
		{
			get { return this.gazette; }
		}

		public CityView CityScene
		{
			get { return this.townScene; }
		}

		public SpaceshipScreen RocketScreen
		{
			get { return this.starshipDisplay; }
		}

		public DiplomatActions Diplomats
		{
			get { return this.envoyDeeds; }
		}

		public GameReplay Replayer
		{
			get { return this.sessionRerun; }
		}

		public CivilizationInfo RealmInfo
		{
			get { return this.realmDetails; }
		}

		public WorldMap Atlas
		{
			get { return this.globeChart; }
		}

		public CityFoundingAndScore Founding
		{
			get { return this.townSettlingWithTally; }
		}

		public Palace Throne
		{
			get { return this.castle; }
		}

		public ArmySelection ArmyPicker
		{
			get { return this.forceChoice; }
		}

		public CivilizationSchism Secession
		{
			get { return this.realmSplit; }
		}

		public CRuntime Libc
		{
			get { return this.chrLibc; }
		}

		/// <summary>The records of the units of all players at 0x81d4, kept in C# (B6.2): a copy</summary>
		public byte[] UnitRecords => (byte[])(this.processor.Storage.OverlayAt(
			CPU.AsLinearAddress(this.processor.SegData.Value16, UnitMemory.Origin)) ?? []).Clone();

		/// <summary>The variables of the timer and sound driver at 0x40 - 0x6f (B6.7)</summary>
		public TimerState Timer
		{
			get { return this.timer; }
		}

		/// <summary>The state of the mouse driver (B6.7)</summary>
		public PointerState Pointer
		{
			get { return this.pointer; }
		}

		/// <summary>The colour cycles of the driver at 0x35cf (B6.5)</summary>
		public HueCycles HueCycles
		{
			get { return this.hueCycles; }
		}

		/// <summary>The boxes of the screens (B6.5)</summary>
		public ScreenBoxes Boxes
		{
			get { return this.boxes; }
		}

		/// <summary>The tables of CIV.EXE 0x282 - 0x19c1 (B6.3)</summary>
		public GameTables Tables
		{
			get { return this.tables; }
		}

		/// <summary>The named global variables (B6.4)</summary>
		public GameGlobals Globals
		{
			get { return this.globals; }
		}

		/// <summary>The message buffer at 0xba06 (B6.1)</summary>
		public TextBuffer Words
		{
			get { return this.words; }
		}

		public VGADriver VideoCard
		{
			get { return this.videoHandler; }
		}

		public SoundDriver Audio
		{
			get { return this.audioHandler; }
		}
		#endregion
	}
}
