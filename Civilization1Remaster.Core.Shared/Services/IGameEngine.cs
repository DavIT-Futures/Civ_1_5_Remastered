namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IGameEngine
	{
		CentralProcessor Processor { get; }

		GameState World { get; }

		CallJournal Journal { get; }

		CallJournal InterruptJournal { get; }

		CallJournal VideoJournal { get; }

		CallJournal PrologueJournal { get; }

		IGameLauncher Startup { get; }

		IPlatformServices Services { get; }

		IWorldEvents TurnHooks { get; }

		IDialogPainter Dialogs { get; }

		ITurnController TurnLogic { get; }

		IGameHelpers Helpers { get; }

		ITextPainter TextPainter { get; }

		IPictureLoader ImageToolkit { get; }

		ITextBlockPainter BlockText { get; }

		IMapService MapOperations { get; }

		IUnitRoster UnitOperations { get; }

		IRoutePlanner Routing { get; }

		ICityCapture Conquest { get; }

		IComputerPlayer Computer { get; }

		IResearchOffice Advisors { get; }

		ICities CityOperations { get; }

		IBattles Combat { get; }

		IForeignAffairs Politics { get; }

		IMenuBar MenuSystem { get; }

		IIntroSequence Prologue { get; }

		IRulerAudience Audience { get; }

		IWorldBuilder WorldSetup { get; }

		IHelpPopups HelpSystem { get; }

		ISaveGameStore SaveLoad { get; }

		IFameRecords Legends { get; }

		INewGameSetup NewGameWizard { get; }

		ICityNamingScreen CityNaming { get; }

		IAdvisorScreens Briefings { get; }

		IEncyclopedia Encyclopedia { get; }

		IGazette Paper { get; }

		ICityPanorama CityScene { get; }

		ISpaceshipBay RocketScreen { get; }

		IDiplomatMissions Diplomats { get; }

		IReplayViewer Replayer { get; }

		IRivalOverview RealmInformation { get; }

		IWorldStatistics Atlas { get; }

		ISettlementAndScoring Founding { get; }

		IPalaceBuilder Throne { get; }

		IUnitPicker ArmyPicker { get; }

		ICivilWar Secession { get; }

		IRuntimeLibrary Runtime { get; }

		TimerState Timer { get; }

		PointerState Pointer { get; }

		HueCycles HueCycles { get; }

		GameTables Tables { get; }

		GameGlobals Globals { get; }

		TextBuffer Words { get; }

		CodeTexts Codes { get; }

		NameTexts Names { get; }

		VideoGraphicsDriver VideoCard { get; }

		IAudioOutput Audio { get; }

		Point[] TownDeltas { get; set; }

		ushort CreditsStream { get; set; }

		ushort MenuPointerState { get; set; }

		ushort MenuPointerHorizontal { get; set; }

		ushort MenuPointerVertical { get; set; }
	}
}
