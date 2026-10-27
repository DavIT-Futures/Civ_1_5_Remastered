using System.Collections.Generic;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// A global word variable of the game kept in C# (B6.4): the code reads and writes Value, and the memory reaches it
	/// over its address (CPUMemory.AttachOverlay), low byte first, like the data segment of the original.
	/// </summary>
	public sealed class GlobalWord : IMemoryOverlay
	{
		public GlobalWord(ushort address)
		{
			this.Address = address;
		}

		/// <summary>The address of the variable in the data segment</summary>
		public ushort Address { get; }

		/// <summary>The value of the variable</summary>
		public ushort Value { get; set; }

		public int Length => 2;

		public byte Read(int place) => place == 0 ? (byte)this.Value : (byte)(this.Value >> 8);

		public void Write(int place, byte value) =>
			this.Value = place == 0 ? (ushort)((this.Value & 0xff00) | value) : (ushort)((this.Value & 0xff) | (value << 8));
	}

	/// <summary>
	/// The global variables of the game named in GameModule (B4) as C# fields (B6.4); GameModule reads and writes them
	/// through its properties of the same names, the rest of the code through the memory (the overlays).
	/// </summary>
	public sealed class GameGlobals
	{
		public readonly GlobalWord ScreenBox = new GlobalWord(0xaa);
		public readonly GlobalWord BackScreenBox = new GlobalWord(0x19d4);
		public readonly GlobalWord PictureScreenBox = new GlobalWord(0x19e8);
		public readonly GlobalWord CityTiles = new GlobalWord(0x6540);
		public readonly GlobalWord CityOwner = new GlobalWord(0x6548);
		public readonly GlobalWord FoundDistance = new GlobalWord(0x6c9a);
		public readonly GlobalWord CityFood = new GlobalWord(0x70da);
		public readonly GlobalWord CityShields = new GlobalWord(0x70dc);
		public readonly GlobalWord CityTrade = new GlobalWord(0x70de);
		public readonly GlobalWord CityLuxuries = new GlobalWord(0x70e0);
		public readonly GlobalWord CityHappy = new GlobalWord(0x70e2);
		public readonly GlobalWord CityUnhappy = new GlobalWord(0x70e4);
		public readonly GlobalWord CityScience = new GlobalWord(0x70e6);
		public readonly GlobalWord HumanTileNear = new GlobalWord(0x70e8);
		public readonly GlobalWord ContentAdvances = new GlobalWord(0xb882);
		public readonly GlobalWord CitySpecialists = new GlobalWord(0xe8b8);
		public readonly GlobalWord CityScreenTab = new GlobalWord(0x2496);
		public readonly GlobalWord DialogStyle = new GlobalWord(0x2f9e);
		public readonly GlobalWord GoalHoriz = new GlobalWord(0x6590);
		public readonly GlobalWord GoalVert = new GlobalWord(0x6592);
		public readonly GlobalWord KingStep = new GlobalWord(0x67f4);
		public readonly GlobalWord TextCursor = new GlobalWord(0x68cc);
		public readonly GlobalWord TurnPlayer = new GlobalWord(0x6b90);
		public readonly GlobalWord DisabledLines = new GlobalWord(0xb276);
		public readonly GlobalWord TechnologyLevel = new GlobalWord(0xd2de);
		public readonly GlobalWord VgaMode = new GlobalWord(0xd762);
		public readonly GlobalWord KingSpeaking = new GlobalWord(0xd76c);
		public readonly GlobalWord FoundPlayer = new GlobalWord(0xd7f0);
		public readonly GlobalWord MapRevealed = new GlobalWord(0xd806);
		public readonly GlobalWord GameEndRequest = new GlobalWord(0xdc48);
		public readonly GlobalWord MousePresent = new GlobalWord(0x1a3c);
		public readonly GlobalWord DefaultLine = new GlobalWord(0x2f9a);
		public readonly GlobalWord MenuHelpTopic = new GlobalWord(0x2f9c);
		public readonly GlobalWord KingAnswer = new GlobalWord(0x3934);
		public readonly GlobalWord CityAttackersAway = new GlobalWord(0x6546);
		public readonly GlobalWord ReplayMode = new GlobalWord(0x680a);
		public readonly GlobalWord ReplayActor = new GlobalWord(0x6810);
		public readonly GlobalWord MainMenuChoice = new GlobalWord(0x6b32);
		public readonly GlobalWord CityCorruption = new GlobalWord(0xd2e0);
		public readonly GlobalWord CityUnitUpkeep = new GlobalWord(0xd2f6);
		public readonly GlobalWord ChosenOrder = new GlobalWord(0xd4ca);
		public readonly GlobalWord MapViewHoriz = new GlobalWord(0xd4cc);
		public readonly GlobalWord MapViewVert = new GlobalWord(0xd75e);
		public readonly GlobalWord CitySpecialSlots = new GlobalWord(0xdeb8);
		public readonly GlobalWord PointerShowLevel = new GlobalWord(0xdeea);
		public readonly GlobalWord CityTaxes = new GlobalWord(0xe17a);
		public readonly GlobalWord CitySettlers = new GlobalWord(0xe3c6);
		public readonly GlobalWord StoryState = new GlobalWord(0x3b62);
		public readonly GlobalWord SaveDrive = new GlobalWord(0x4384);
		public readonly GlobalWord StoryLineTime = new GlobalWord(0x6806);
		public readonly GlobalWord MapRedrawRequest = new GlobalWord(0x6b64);
		public readonly GlobalWord PollutionFactor = new GlobalWord(0x6c98);
		public readonly GlobalWord CityAgain = new GlobalWord(0xb1e8);
		public readonly GlobalWord WorldMapView = new GlobalWord(0xb278);
		public readonly GlobalWord UnitAtTilePlayer = new GlobalWord(0xd20a);
		public readonly GlobalWord ArrivedSpaceship = new GlobalWord(0x1cf6);
		public readonly GlobalWord StackKind = new GlobalWord(0x653a);
		public readonly GlobalWord StackTally = new GlobalWord(0x653c);
		public readonly GlobalWord ShownCity = new GlobalWord(0x653e);
		public readonly GlobalWord MenuResult = new GlobalWord(0x654a);
		public readonly GlobalWord GotoCost = new GlobalWord(0x6794);
		public readonly GlobalWord PalaceDistance = new GlobalWord(0x6b30);
		public readonly GlobalWord ConquestEnd = new GlobalWord(0xb884);
		public readonly GlobalWord EmissaryAudience = new GlobalWord(0xd804);
		public readonly GlobalWord UnitsHidden = new GlobalWord(0xdcfc);
		public readonly GlobalWord FirstMissingDisk = new GlobalWord(0xdeba);
		public readonly GlobalWord KingMood = new GlobalWord(0x3936);
		public readonly GlobalWord CodeText1 = new GlobalWord(0x30b8);
		public readonly GlobalWord CodeText2 = new GlobalWord(0x30ba);
		public readonly GlobalWord CodeText3 = new GlobalWord(0x30bc);
		public readonly GlobalWord CodeText4 = new GlobalWord(0x30be);
		public readonly GlobalWord MenuLineHeight = new GlobalWord(0x2fa4);
		public readonly GlobalWord MenuRightEdge = new GlobalWord(0xde0e);
		public readonly GlobalWord MenuLineCount = new GlobalWord(0x658e);
		public readonly GlobalWord PalacePartWidth = new GlobalWord(0x4b2e);
		public readonly GlobalWord KingPicture = new GlobalWord(0x67f8);
		public readonly GlobalWord StackDomainTally = new GlobalWord(0x6532);
		public readonly GlobalWord PopulationMark = new GlobalWord(0x1c24);
		public readonly GlobalWord LastSaveSlot = new GlobalWord(0xe168);
		public readonly GlobalWord MidTurnSave = new GlobalWord(0xdf60);
		public readonly GlobalWord SpaceshipColonists = new GlobalWord(0xdeae);
		public readonly GlobalWord EarthGame = new GlobalWord(0xd76a);
		public readonly GlobalWord Climate = new GlobalWord(0x7efa);
		public readonly GlobalWord KingPause = new GlobalWord(0x67f6);
		public readonly GlobalWord ReplayCityDistance = new GlobalWord(0x6812);
		public readonly GlobalWord ScoreIconHoriz = new GlobalWord(0xb25c);
		public readonly GlobalWord ScoreIconVert = new GlobalWord(0xb2bc);
		public readonly GlobalWord BirthPicture = new GlobalWord(0x3b64);
		public readonly GlobalWord MenuWidth = new GlobalWord(0xd4c8);
		public readonly GlobalWord MenuFirstChoice = new GlobalWord(0xb1ec);
		public readonly GlobalWord MenuChoiceCount = new GlobalWord(0xd208);
		public readonly GlobalWord MenuMeasuring = new GlobalWord(0xd2e2);
		public readonly GlobalWord MenuFlashHue = new GlobalWord(0x654c);
		public readonly GlobalWord MenuOptions = new GlobalWord(0xd7f2);
		public readonly GlobalWord PalaceFirstPicture = new GlobalWord(0x6828);
		public readonly GlobalWord PalaceLeftBushes = new GlobalWord(0xd930);
		public readonly GlobalWord StoryCycleRecord = new GlobalWord(0x3b66);
		public readonly GlobalWord StoryCycleCount = new GlobalWord(0x3b68);
		public readonly GlobalWord CountLosses = new GlobalWord(0x70d8);
		public readonly GlobalWord StackLeader = new GlobalWord(0x6530);
		public readonly GlobalWord KingPose = new GlobalWord(0x7ef4);
		public readonly GlobalWord AudiencePlayer = new GlobalWord(0x67f2);
		public readonly GlobalWord AudienceWithoutPictures = new GlobalWord(0x67f0);
		public readonly GlobalWord ReplayFile = new GlobalWord(0x680c);
		public readonly GlobalWord CityLabelCount = new GlobalWord(0x6c96);
		public readonly GlobalWord HumanContinentAttackers = new GlobalWord(0x2a6e);
		public readonly GlobalWord DiskInUse = new GlobalWord(0xe17c);
		public readonly GlobalWord HallOfFameScore = new GlobalWord(0xb77e);
		public readonly GlobalWord PointerPicture = new GlobalWord(0xd4dc);
		public readonly GlobalWord UnitLimitTold = new GlobalWord(0xd760);
		public readonly GlobalWord StoryFile = new GlobalWord(0x6804);
		public readonly GlobalWord StoryKeyAllowed = new GlobalWord(0x67fc);
		public readonly GlobalWord LandMass = new GlobalWord(0x7ef6);
		public readonly GlobalWord Temperature = new GlobalWord(0x7ef8);
		public readonly GlobalWord WorldAge = new GlobalWord(0x7efc);
		public readonly GlobalWord ScreenReserveFailed = new GlobalWord(0x6dfc);
		public readonly GlobalWord ScreenRepaintRequest = new GlobalWord(0x1ae0);
		public readonly GlobalWord CodeText5 = new GlobalWord(0x30c0);
		public readonly GlobalWord EgaMode = new GlobalWord(0xb20e);
		public readonly GlobalWord SavedScreenStyle = new GlobalWord(0x67ec);
		public readonly GlobalWord KingPictureLoaded = new GlobalWord(0x67ea);
		public readonly GlobalWord AdvisorPoseShown = new GlobalWord(0x67e8);
		public readonly GlobalWord KingScenePicture = new GlobalWord(0xb1e6);
		public readonly GlobalWord KingFacesPicture = new GlobalWord(0x7efe);
		public readonly GlobalWord SpaceshipPropulsion = new GlobalWord(0xdea8);
		public readonly GlobalWord SpaceshipFuel = new GlobalWord(0xdeaa);
		public readonly GlobalWord SpaceshipSupport = new GlobalWord(0xdeb0);
		public readonly GlobalWord SpaceshipEnergy = new GlobalWord(0xdeb2);
		public readonly GlobalWord SpaceshipComponentsComplete = new GlobalWord(0x687c);
		public readonly GlobalWord SpaceshipModulesComplete = new GlobalWord(0x687e);
		public readonly GlobalWord SpaceshipPicture = new GlobalWord(0x682a);
		public readonly GlobalWord FileError = new GlobalWord(0x681c);
		public readonly GlobalWord SavedHumanPlayer = new GlobalWord(0x6822);
		public readonly GlobalWord SavedDifficulty = new GlobalWord(0x6824);
		public readonly GlobalWord SavedYear = new GlobalWord(0x6826);
		public readonly GlobalWord ReplayAtGameEnd = new GlobalWord(0x680e);
		public readonly GlobalWord CivilopediaNextPage = new GlobalWord(0x6808);
		public readonly GlobalWord SavedPointerShowLevel = new GlobalWord(0x652e);
		public readonly GlobalWord TextCursorColumn = new GlobalWord(0x68d0);
		public readonly GlobalWord TextCursorWidth = new GlobalWord(0x68ce);
		public readonly GlobalWord StackDomainMode = new GlobalWord(0x6536);
		public readonly GlobalWord StackIndexLimit = new GlobalWord(0x6538);
		public readonly GlobalWord RoutePlaceHoriz = new GlobalWord(0x6796);
		public readonly GlobalWord RoutePlaceVert = new GlobalWord(0x6798);
		public readonly GlobalWord PlayerAtWar = new GlobalWord(0x6ed4);
		public readonly GlobalWord NoConquestAnimation = new GlobalWord(0x28bc);
		public readonly GlobalWord PalaceRightBushes = new GlobalWord(0xd934);
		public readonly GlobalWord WonderScore = new GlobalWord(0xd764);
		public readonly GlobalWord CitizenScore = new GlobalWord(0xd766);
		public readonly GlobalWord MenuAnyKey = new GlobalWord(0xd206);
		public readonly GlobalWord MenuFooter = new GlobalWord(0x2fa0);
		public readonly GlobalWord LoadingGame = new GlobalWord(0x681a);
		public readonly GlobalWord LoadFile = new GlobalWord(0x681e);
		public readonly GlobalWord CivilopediaWithoutPictures = new GlobalWord(0x3c62);
		public readonly GlobalWord CityExcessUnhappy = new GlobalWord(0x6542);
		public readonly GlobalWord AnarchyPressure = new GlobalWord(0xe3c2);
		public readonly GlobalWord DemocracyPressure = new GlobalWord(0xdb42);
		public readonly GlobalWord QuietUnitSetup = new GlobalWord(0x20f4);
		public readonly GlobalWord MapFocusLeft = new GlobalWord(0x6ed6);
		public readonly GlobalWord MapFocusTop = new GlobalWord(0x70ea);
		public readonly GlobalWord SpriteScreenBox = new GlobalWord(0x19fc);
		public readonly GlobalWord LightenedPixels = new GlobalWord(0x67fe);
		public readonly GlobalWord LowMemoryVga = new GlobalWord(0x1a3e);
		public readonly GlobalWord PointerImage = new GlobalWord(0x5876);
		public readonly GlobalWord PointerHotHoriz = new GlobalWord(0x5878);
		public readonly GlobalWord PointerHotVert = new GlobalWord(0x587a);
		public readonly GlobalWord AudienceWelcomed = new GlobalWord(0x6b8e);
		public readonly GlobalWord KingModernPictures = new GlobalWord(0x67ee);
		public readonly GlobalWord GovernmentPicture = new GlobalWord(0x804e);
		public readonly GlobalWord RepaintingMainScreen = new GlobalWord(0x1c28);
		public readonly GlobalWord CityTileDotted = new GlobalWord(0x2494);
		public readonly GlobalWord BoxMotif = new GlobalWord(0x2f98);
		public readonly GlobalWord MenuMouseUsed = new GlobalWord(0x2fa2);
		public readonly GlobalWord HueCyclingOn = new GlobalWord(0x2fec);
		public readonly GlobalWord LoadedGameMark = new GlobalWord(0x3484);
		public readonly GlobalWord PalaceWingShift = new GlobalWord(0x4b32);
		public readonly GlobalWord StackLeaderScore = new GlobalWord(0x6534);
		public readonly GlobalWord KingScreenKept = new GlobalWord(0x67fa);
		public readonly GlobalWord StoryMusicTime = new GlobalWord(0x6800);
		public readonly GlobalWord StoryLinesTold = new GlobalWord(0x6802);
		public readonly GlobalWord RiotShown = new GlobalWord(0x6b92);
		public readonly GlobalWord PointerIcon = new GlobalWord(0x6e92);
		public readonly GlobalWord LateGameNudge = new GlobalWord(0x8078);
		public readonly GlobalWord PalaceLayout = new GlobalWord(0xb22a);
		public readonly GlobalWord LiveRealmCount = new GlobalWord(0xde20);
		public readonly GlobalWord DialogNeedsRedraw = new GlobalWord(0xdb38);
		public readonly GlobalWord MenuMoreBox = new GlobalWord(0x8066);
		public readonly GlobalWord MenuLineOffsets = new GlobalWord(0x654e);
		public readonly GlobalWord ScoreVerticalShift = new GlobalWord(0x5116);
		public readonly GlobalWord SchismShareDc6a = new GlobalWord(0xdc6a);
		public readonly GlobalWord DoubleWidthText = new GlobalWord(0x6b8c);
		public readonly GlobalWord NewspaperCrowdPicture = new GlobalWord(0xdf0c);
		public readonly GlobalWord AudienceInitialPrompt = new GlobalWord(0xd2dc);
		public readonly GlobalWord UnitSupportCount = new GlobalWord(0xdeb6);
		public readonly GlobalWord ImportPlayerIndex = new GlobalWord(0x6820);
		public readonly GlobalWord TileFortressPicture = new GlobalWord(0xd4da);
		public readonly GlobalWord MiniSymbolPicture = new GlobalWord(0xb880);
		public readonly GlobalWord UnhappyCitizenPicture = new GlobalWord(0x6e9e);
		public readonly GlobalWord PollutionIconPicture = new GlobalWord(0xb2ba);
		public readonly GlobalWord SpaceshipHabitatPicture = new GlobalWord(0x6834);
		public readonly GlobalWord IntroReservedScreen = new GlobalWord(0x32d6);
		public readonly GlobalWord PalaceLeftWingWidth = new GlobalWord(0x4b2c);
		public readonly GlobalWord PalaceRightWingWidth = new GlobalWord(0x4b34);
		public readonly GlobalWord PalaceStyleShift = new GlobalWord(0xd922);
		public readonly GlobalWord PalaceGardenBackground = new GlobalWord(0xd932);
		public readonly GlobalWord SavedGovernmentTitleCommunism = new GlobalWord(0x33a8);
		public readonly GlobalWord SavedGovernmentTitleMonarchy = new GlobalWord(0x33aa);
		public readonly GlobalWord SpaceshipStarFieldFrame = new GlobalWord(0x687a);
		public readonly GlobalWord FallenLeadersCount = new GlobalWord(0xb1d4);
		public readonly GlobalWord GameEndFlags = new GlobalWord(0x4cd9);

		/// <summary>The names and addresses of the variables (MovedRanges)</summary>
		public static readonly (string Name, ushort Address)[] Layout =
		[
			("ScreenBox", 0xaa),
			("BackScreenBox", 0x19d4),
			("PictureScreenBox", 0x19e8),
			("CityTiles", 0x6540),
			("CityOwner", 0x6548),
			("FoundDistance", 0x6c9a),
			("CityFood", 0x70da),
			("CityShields", 0x70dc),
			("CityTrade", 0x70de),
			("CityLuxuries", 0x70e0),
			("CityHappy", 0x70e2),
			("CityUnhappy", 0x70e4),
			("CityScience", 0x70e6),
			("HumanTileNear", 0x70e8),
			("ContentAdvances", 0xb882),
			("CitySpecialists", 0xe8b8),
			("CityScreenTab", 0x2496),
			("DialogStyle", 0x2f9e),
			("GoalHoriz", 0x6590),
			("GoalVert", 0x6592),
			("KingStep", 0x67f4),
			("TextCursor", 0x68cc),
			("TurnPlayer", 0x6b90),
			("DisabledLines", 0xb276),
			("TechnologyLevel", 0xd2de),
			("VgaMode", 0xd762),
			("KingSpeaking", 0xd76c),
			("FoundPlayer", 0xd7f0),
			("MapRevealed", 0xd806),
			("GameEndRequest", 0xdc48),
			("MousePresent", 0x1a3c),
			("DefaultLine", 0x2f9a),
			("MenuHelpTopic", 0x2f9c),
			("KingAnswer", 0x3934),
			("CityAttackersAway", 0x6546),
			("ReplayMode", 0x680a),
			("ReplayActor", 0x6810),
			("MainMenuChoice", 0x6b32),
			("CityCorruption", 0xd2e0),
			("CityUnitUpkeep", 0xd2f6),
			("ChosenOrder", 0xd4ca),
			("MapViewHoriz", 0xd4cc),
			("MapViewVert", 0xd75e),
			("CitySpecialSlots", 0xdeb8),
			("PointerShowLevel", 0xdeea),
			("CityTaxes", 0xe17a),
			("CitySettlers", 0xe3c6),
			("StoryState", 0x3b62),
			("SaveDrive", 0x4384),
			("StoryLineTime", 0x6806),
			("MapRedrawRequest", 0x6b64),
			("PollutionFactor", 0x6c98),
			("CityAgain", 0xb1e8),
			("WorldMapView", 0xb278),
			("UnitAtTilePlayer", 0xd20a),
			("ArrivedSpaceship", 0x1cf6),
			("StackKind", 0x653a),
			("StackTally", 0x653c),
			("ShownCity", 0x653e),
			("MenuResult", 0x654a),
			("GotoCost", 0x6794),
			("PalaceDistance", 0x6b30),
			("ConquestEnd", 0xb884),
			("EmissaryAudience", 0xd804),
			("UnitsHidden", 0xdcfc),
			("FirstMissingDisk", 0xdeba),
			("KingMood", 0x3936),
			("CodeText1", 0x30b8),
			("CodeText2", 0x30ba),
			("CodeText3", 0x30bc),
			("CodeText4", 0x30be),
			("MenuLineHeight", 0x2fa4),
			("MenuRightEdge", 0xde0e),
			("MenuLineCount", 0x658e),
			("PalacePartWidth", 0x4b2e),
			("KingPicture", 0x67f8),
			("StackDomainTally", 0x6532),
			("PopulationMark", 0x1c24),
			("LastSaveSlot", 0xe168),
			("MidTurnSave", 0xdf60),
			("SpaceshipColonists", 0xdeae),
			("EarthGame", 0xd76a),
			("Climate", 0x7efa),
			("KingPause", 0x67f6),
			("ReplayCityDistance", 0x6812),
			("ScoreIconHoriz", 0xb25c),
			("ScoreIconVert", 0xb2bc),
			("BirthPicture", 0x3b64),
			("MenuWidth", 0xd4c8),
			("MenuFirstChoice", 0xb1ec),
			("MenuChoiceCount", 0xd208),
			("MenuMeasuring", 0xd2e2),
			("MenuFlashHue", 0x654c),
			("MenuOptions", 0xd7f2),
			("PalaceFirstPicture", 0x6828),
			("PalaceLeftBushes", 0xd930),
			("StoryCycleRecord", 0x3b66),
			("StoryCycleCount", 0x3b68),
			("CountLosses", 0x70d8),
			("StackLeader", 0x6530),
			("KingPose", 0x7ef4),
			("AudiencePlayer", 0x67f2),
			("AudienceWithoutPictures", 0x67f0),
			("ReplayFile", 0x680c),
			("CityLabelCount", 0x6c96),
			("HumanContinentAttackers", 0x2a6e),
			("DiskInUse", 0xe17c),
			("HallOfFameScore", 0xb77e),
			("PointerPicture", 0xd4dc),
			("UnitLimitTold", 0xd760),
			("StoryFile", 0x6804),
			("StoryKeyAllowed", 0x67fc),
			("LandMass", 0x7ef6),
			("Temperature", 0x7ef8),
			("WorldAge", 0x7efc),
			("ScreenReserveFailed", 0x6dfc),
			("ScreenRepaintRequest", 0x1ae0),
			("CodeText5", 0x30c0),
			("EgaMode", 0xb20e),
			("SavedScreenStyle", 0x67ec),
			("KingPictureLoaded", 0x67ea),
			("AdvisorPoseShown", 0x67e8),
			("KingScenePicture", 0xb1e6),
			("KingFacesPicture", 0x7efe),
			("SpaceshipPropulsion", 0xdea8),
			("SpaceshipFuel", 0xdeaa),
			("SpaceshipSupport", 0xdeb0),
			("SpaceshipEnergy", 0xdeb2),
			("SpaceshipComponentsComplete", 0x687c),
			("SpaceshipModulesComplete", 0x687e),
			("SpaceshipPicture", 0x682a),
			("FileError", 0x681c),
			("SavedHumanPlayer", 0x6822),
			("SavedDifficulty", 0x6824),
			("SavedYear", 0x6826),
			("ReplayAtGameEnd", 0x680e),
			("CivilopediaNextPage", 0x6808),
			("SavedPointerShowLevel", 0x652e),
			("TextCursorColumn", 0x68d0),
			("TextCursorWidth", 0x68ce),
			("StackDomainMode", 0x6536),
			("StackIndexLimit", 0x6538),
			("RoutePlaceHoriz", 0x6796),
			("RoutePlaceVert", 0x6798),
			("PlayerAtWar", 0x6ed4),
			("NoConquestAnimation", 0x28bc),
			("PalaceRightBushes", 0xd934),
			("WonderScore", 0xd764),
			("CitizenScore", 0xd766),
			("MenuAnyKey", 0xd206),
			("MenuFooter", 0x2fa0),
			("LoadingGame", 0x681a),
			("LoadFile", 0x681e),
			("CivilopediaWithoutPictures", 0x3c62),
			("CityExcessUnhappy", 0x6542),
			("AnarchyPressure", 0xe3c2),
			("DemocracyPressure", 0xdb42),
			("QuietUnitSetup", 0x20f4),
			("MapFocusLeft", 0x6ed6),
			("MapFocusTop", 0x70ea),
			("SpriteScreenBox", 0x19fc),
			("LightenedPixels", 0x67fe),
			("LowMemoryVga", 0x1a3e),
			("PointerImage", 0x5876),
			("PointerHotHoriz", 0x5878),
			("PointerHotVert", 0x587a),
			("AudienceWelcomed", 0x6b8e),
			("KingModernPictures", 0x67ee),
			("GovernmentPicture", 0x804e),
			("RepaintingMainScreen", 0x1c28),
			("CityTileDotted", 0x2494),
			("BoxMotif", 0x2f98),
			("MenuMouseUsed", 0x2fa2),
			("HueCyclingOn", 0x2fec),
			("LoadedGameMark", 0x3484),
			("PalaceWingShift", 0x4b32),
			("StackLeaderScore", 0x6534),
			("KingScreenKept", 0x67fa),
			("StoryMusicTime", 0x6800),
			("StoryLinesTold", 0x6802),
			("RiotShown", 0x6b92),
			("PointerIcon", 0x6e92),
			("LateGameNudge", 0x8078),
			("PalaceLayout", 0xb22a),
			("LiveRealmCount", 0xde20),
			("DialogNeedsRedraw", 0xdb38),
			("MenuMoreBox", 0x8066),
			("MenuLineOffsets", 0x654e),
			("ScoreVerticalShift", 0x5116),
			("SchismShareDc6a", 0xdc6a),
			("DoubleWidthText", 0x6b8c),
			("NewspaperCrowdPicture", 0xdf0c),
			("AudienceInitialPrompt", 0xd2dc),
			("UnitSupportCount", 0xdeb6),
			("ImportPlayerIndex", 0x6820),
			("TileFortressPicture", 0xd4da),
			("MiniSymbolPicture", 0xb880),
			("UnhappyCitizenPicture", 0x6e9e),
			("PollutionIconPicture", 0xb2ba),
			("SpaceshipHabitatPicture", 0x6834),
			("IntroReservedScreen", 0x32d6),
			("PalaceLeftWingWidth", 0x4b2c),
			("PalaceRightWingWidth", 0x4b34),
			("PalaceStyleShift", 0xd922),
			("PalaceGardenBackground", 0xd932),
			("SavedGovernmentTitleCommunism", 0x33a8),
			("SavedGovernmentTitleMonarchy", 0x33aa),
			("SpaceshipStarFieldFrame", 0x687a),
			("FallenLeadersCount", 0xb1d4),
			("GameEndFlags", 0x4cd9),
		];

		/// <summary>All the variables, in the order of Layout</summary>
		public IEnumerable<GlobalWord> All => [ScreenBox, BackScreenBox, PictureScreenBox, CityTiles, CityOwner, FoundDistance, CityFood, CityShields, CityTrade, CityLuxuries, CityHappy, CityUnhappy, CityScience, HumanTileNear, ContentAdvances, CitySpecialists, CityScreenTab, DialogStyle, GoalHoriz, GoalVert, KingStep, TextCursor, TurnPlayer, DisabledLines, TechnologyLevel, VgaMode, KingSpeaking, FoundPlayer, MapRevealed, GameEndRequest, MousePresent, DefaultLine, MenuHelpTopic, KingAnswer, CityAttackersAway, ReplayMode, ReplayActor, MainMenuChoice, CityCorruption, CityUnitUpkeep, ChosenOrder, MapViewHoriz, MapViewVert, CitySpecialSlots, PointerShowLevel, CityTaxes, CitySettlers, StoryState, SaveDrive, StoryLineTime, MapRedrawRequest, PollutionFactor, CityAgain, WorldMapView, UnitAtTilePlayer, ArrivedSpaceship, StackKind, StackTally, ShownCity, MenuResult, GotoCost, PalaceDistance, ConquestEnd, EmissaryAudience, UnitsHidden, FirstMissingDisk, KingMood, CodeText1, CodeText2, CodeText3, CodeText4, MenuLineHeight, MenuRightEdge, MenuLineCount, PalacePartWidth, KingPicture, StackDomainTally, PopulationMark, LastSaveSlot, MidTurnSave, SpaceshipColonists, EarthGame, Climate, KingPause, ReplayCityDistance, ScoreIconHoriz, ScoreIconVert, BirthPicture, MenuWidth, MenuFirstChoice, MenuChoiceCount, MenuMeasuring, MenuFlashHue, MenuOptions, PalaceFirstPicture, PalaceLeftBushes, StoryCycleRecord, StoryCycleCount, CountLosses, StackLeader, KingPose, AudiencePlayer, AudienceWithoutPictures, ReplayFile, CityLabelCount, HumanContinentAttackers, DiskInUse, HallOfFameScore, PointerPicture, UnitLimitTold, StoryFile, StoryKeyAllowed, LandMass, Temperature, WorldAge, ScreenReserveFailed, ScreenRepaintRequest, CodeText5, EgaMode, SavedScreenStyle, KingPictureLoaded, AdvisorPoseShown, KingScenePicture, KingFacesPicture, SpaceshipPropulsion, SpaceshipFuel, SpaceshipSupport, SpaceshipEnergy, SpaceshipComponentsComplete, SpaceshipModulesComplete, SpaceshipPicture, FileError, SavedHumanPlayer, SavedDifficulty, SavedYear, ReplayAtGameEnd, CivilopediaNextPage, SavedPointerShowLevel, TextCursorColumn, TextCursorWidth, StackDomainMode, StackIndexLimit, RoutePlaceHoriz, RoutePlaceVert, PlayerAtWar, NoConquestAnimation, PalaceRightBushes, WonderScore, CitizenScore, MenuAnyKey, MenuFooter, LoadingGame, LoadFile, CivilopediaWithoutPictures, CityExcessUnhappy, AnarchyPressure, DemocracyPressure, QuietUnitSetup, MapFocusLeft, MapFocusTop, SpriteScreenBox, LightenedPixels, LowMemoryVga, PointerImage, PointerHotHoriz, PointerHotVert, AudienceWelcomed, KingModernPictures, GovernmentPicture, RepaintingMainScreen, CityTileDotted, BoxMotif, MenuMouseUsed, HueCyclingOn, LoadedGameMark, PalaceWingShift, StackLeaderScore, KingScreenKept, StoryMusicTime, StoryLinesTold, RiotShown, PointerIcon, LateGameNudge, PalaceLayout, LiveRealmCount, DialogNeedsRedraw, MenuMoreBox, MenuLineOffsets, ScoreVerticalShift, SchismShareDc6a, DoubleWidthText, NewspaperCrowdPicture, AudienceInitialPrompt, UnitSupportCount, ImportPlayerIndex, TileFortressPicture, MiniSymbolPicture, UnhappyCitizenPicture, PollutionIconPicture, SpaceshipHabitatPicture, IntroReservedScreen, PalaceLeftWingWidth, PalaceRightWingWidth, PalaceStyleShift, PalaceGardenBackground, SavedGovernmentTitleCommunism, SavedGovernmentTitleMonarchy, SpaceshipStarFieldFrame, FallenLeadersCount, GameEndFlags];
	}
}
