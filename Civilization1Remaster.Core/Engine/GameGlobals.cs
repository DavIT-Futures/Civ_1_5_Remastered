using System.Collections.Generic;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The global variables of the game named in GameModule (B4) as C# fields (B6.4); GameModule reads and writes them
	/// through its properties of the same names, the rest of the code through the memory (the overlays).
	/// </summary>
	public sealed class GameGlobals
	{
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
		];

		public GlobalWord ScreenBox { get; } = new GlobalWord(0xaa);
		public GlobalWord BackScreenBox { get; } = new GlobalWord(0x19d4);
		public GlobalWord PictureScreenBox { get; } = new GlobalWord(0x19e8);
		public GlobalWord CityTiles { get; } = new GlobalWord(0x6540);
		public GlobalWord CityOwner { get; } = new GlobalWord(0x6548);
		public GlobalWord FoundDistance { get; } = new GlobalWord(0x6c9a);
		public GlobalWord CityFood { get; } = new GlobalWord(0x70da);
		public GlobalWord CityShields { get; } = new GlobalWord(0x70dc);
		public GlobalWord CityTrade { get; } = new GlobalWord(0x70de);
		public GlobalWord CityLuxuries { get; } = new GlobalWord(0x70e0);
		public GlobalWord CityHappy { get; } = new GlobalWord(0x70e2);
		public GlobalWord CityUnhappy { get; } = new GlobalWord(0x70e4);
		public GlobalWord CityScience { get; } = new GlobalWord(0x70e6);
		public GlobalWord HumanTileNear { get; } = new GlobalWord(0x70e8);
		public GlobalWord ContentAdvances { get; } = new GlobalWord(0xb882);
		public GlobalWord CitySpecialists { get; } = new GlobalWord(0xe8b8);
		public GlobalWord CityScreenTab { get; } = new GlobalWord(0x2496);
		public GlobalWord DialogStyle { get; } = new GlobalWord(0x2f9e);
		public GlobalWord GoalHoriz { get; } = new GlobalWord(0x6590);
		public GlobalWord GoalVert { get; } = new GlobalWord(0x6592);
		public GlobalWord KingStep { get; } = new GlobalWord(0x67f4);
		public GlobalWord TextCursor { get; } = new GlobalWord(0x68cc);
		public GlobalWord TurnPlayer { get; } = new GlobalWord(0x6b90);
		public GlobalWord DisabledLines { get; } = new GlobalWord(0xb276);
		public GlobalWord TechnologyLevel { get; } = new GlobalWord(0xd2de);
		public GlobalWord VgaMode { get; } = new GlobalWord(0xd762);
		public GlobalWord KingSpeaking { get; } = new GlobalWord(0xd76c);
		public GlobalWord FoundPlayer { get; } = new GlobalWord(0xd7f0);
		public GlobalWord MapRevealed { get; } = new GlobalWord(0xd806);
		public GlobalWord GameEndRequest { get; } = new GlobalWord(0xdc48);
		public GlobalWord MousePresent { get; } = new GlobalWord(0x1a3c);
		public GlobalWord DefaultLine { get; } = new GlobalWord(0x2f9a);
		public GlobalWord MenuHelpTopic { get; } = new GlobalWord(0x2f9c);
		public GlobalWord KingAnswer { get; } = new GlobalWord(0x3934);
		public GlobalWord CityAttackersAway { get; } = new GlobalWord(0x6546);
		public GlobalWord ReplayMode { get; } = new GlobalWord(0x680a);
		public GlobalWord ReplayActor { get; } = new GlobalWord(0x6810);
		public GlobalWord MainMenuChoice { get; } = new GlobalWord(0x6b32);
		public GlobalWord CityCorruption { get; } = new GlobalWord(0xd2e0);
		public GlobalWord CityUnitUpkeep { get; } = new GlobalWord(0xd2f6);
		public GlobalWord ChosenOrder { get; } = new GlobalWord(0xd4ca);
		public GlobalWord MapViewHoriz { get; } = new GlobalWord(0xd4cc);
		public GlobalWord MapViewVert { get; } = new GlobalWord(0xd75e);
		public GlobalWord CitySpecialSlots { get; } = new GlobalWord(0xdeb8);
		public GlobalWord PointerShowLevel { get; } = new GlobalWord(0xdeea);
		public GlobalWord CityTaxes { get; } = new GlobalWord(0xe17a);
		public GlobalWord CitySettlers { get; } = new GlobalWord(0xe3c6);
		public GlobalWord StoryState { get; } = new GlobalWord(0x3b62);
		public GlobalWord SaveDrive { get; } = new GlobalWord(0x4384);
		public GlobalWord StoryLineTime { get; } = new GlobalWord(0x6806);
		public GlobalWord MapRedrawRequest { get; } = new GlobalWord(0x6b64);
		public GlobalWord PollutionFactor { get; } = new GlobalWord(0x6c98);
		public GlobalWord CityAgain { get; } = new GlobalWord(0xb1e8);
		public GlobalWord WorldMapView { get; } = new GlobalWord(0xb278);
		public GlobalWord UnitAtTilePlayer { get; } = new GlobalWord(0xd20a);
		public GlobalWord ArrivedSpaceship { get; } = new GlobalWord(0x1cf6);
		public GlobalWord StackKind { get; } = new GlobalWord(0x653a);
		public GlobalWord StackTally { get; } = new GlobalWord(0x653c);
		public GlobalWord ShownCity { get; } = new GlobalWord(0x653e);
		public GlobalWord MenuResult { get; } = new GlobalWord(0x654a);
		public GlobalWord GotoCost { get; } = new GlobalWord(0x6794);
		public GlobalWord PalaceDistance { get; } = new GlobalWord(0x6b30);
		public GlobalWord ConquestEnd { get; } = new GlobalWord(0xb884);
		public GlobalWord EmissaryAudience { get; } = new GlobalWord(0xd804);
		public GlobalWord UnitsHidden { get; } = new GlobalWord(0xdcfc);
		public GlobalWord FirstMissingDisk { get; } = new GlobalWord(0xdeba);
		public GlobalWord KingMood { get; } = new GlobalWord(0x3936);
		public GlobalWord CodeText1 { get; } = new GlobalWord(0x30b8);
		public GlobalWord CodeText2 { get; } = new GlobalWord(0x30ba);
		public GlobalWord CodeText3 { get; } = new GlobalWord(0x30bc);
		public GlobalWord CodeText4 { get; } = new GlobalWord(0x30be);
		public GlobalWord MenuLineHeight { get; } = new GlobalWord(0x2fa4);
		public GlobalWord MenuRightEdge { get; } = new GlobalWord(0xde0e);
		public GlobalWord MenuLineCount { get; } = new GlobalWord(0x658e);
		public GlobalWord PalacePartWidth { get; } = new GlobalWord(0x4b2e);
		public GlobalWord KingPicture { get; } = new GlobalWord(0x67f8);
		public GlobalWord StackDomainTally { get; } = new GlobalWord(0x6532);
		public GlobalWord PopulationMark { get; } = new GlobalWord(0x1c24);
		public GlobalWord LastSaveSlot { get; } = new GlobalWord(0xe168);
		public GlobalWord MidTurnSave { get; } = new GlobalWord(0xdf60);
		public GlobalWord SpaceshipColonists { get; } = new GlobalWord(0xdeae);
		public GlobalWord EarthGame { get; } = new GlobalWord(0xd76a);
		public GlobalWord Climate { get; } = new GlobalWord(0x7efa);
		public GlobalWord KingPause { get; } = new GlobalWord(0x67f6);
		public GlobalWord ReplayCityDistance { get; } = new GlobalWord(0x6812);
		public GlobalWord ScoreIconHoriz { get; } = new GlobalWord(0xb25c);
		public GlobalWord ScoreIconVert { get; } = new GlobalWord(0xb2bc);
		public GlobalWord BirthPicture { get; } = new GlobalWord(0x3b64);
		public GlobalWord MenuWidth { get; } = new GlobalWord(0xd4c8);
		public GlobalWord MenuFirstChoice { get; } = new GlobalWord(0xb1ec);
		public GlobalWord MenuChoiceCount { get; } = new GlobalWord(0xd208);
		public GlobalWord MenuMeasuring { get; } = new GlobalWord(0xd2e2);
		public GlobalWord MenuFlashHue { get; } = new GlobalWord(0x654c);
		public GlobalWord MenuOptions { get; } = new GlobalWord(0xd7f2);
		public GlobalWord PalaceFirstPicture { get; } = new GlobalWord(0x6828);
		public GlobalWord PalaceLeftBushes { get; } = new GlobalWord(0xd930);
		public GlobalWord StoryCycleRecord { get; } = new GlobalWord(0x3b66);
		public GlobalWord StoryCycleCount { get; } = new GlobalWord(0x3b68);
		public GlobalWord CountLosses { get; } = new GlobalWord(0x70d8);
		public GlobalWord StackLeader { get; } = new GlobalWord(0x6530);
		public GlobalWord KingPose { get; } = new GlobalWord(0x7ef4);
		public GlobalWord AudiencePlayer { get; } = new GlobalWord(0x67f2);
		public GlobalWord AudienceWithoutPictures { get; } = new GlobalWord(0x67f0);
		public GlobalWord ReplayFile { get; } = new GlobalWord(0x680c);
		public GlobalWord CityLabelCount { get; } = new GlobalWord(0x6c96);
		public GlobalWord HumanContinentAttackers { get; } = new GlobalWord(0x2a6e);
		public GlobalWord DiskInUse { get; } = new GlobalWord(0xe17c);
		public GlobalWord HallOfFameScore { get; } = new GlobalWord(0xb77e);
		public GlobalWord PointerPicture { get; } = new GlobalWord(0xd4dc);
		public GlobalWord UnitLimitTold { get; } = new GlobalWord(0xd760);
		public GlobalWord StoryFile { get; } = new GlobalWord(0x6804);
		public GlobalWord StoryKeyAllowed { get; } = new GlobalWord(0x67fc);
		public GlobalWord LandMass { get; } = new GlobalWord(0x7ef6);
		public GlobalWord Temperature { get; } = new GlobalWord(0x7ef8);
		public GlobalWord WorldAge { get; } = new GlobalWord(0x7efc);
		public GlobalWord ScreenReserveFailed { get; } = new GlobalWord(0x6dfc);
		public GlobalWord ScreenRepaintRequest { get; } = new GlobalWord(0x1ae0);
		public GlobalWord CodeText5 { get; } = new GlobalWord(0x30c0);
		public GlobalWord EgaMode { get; } = new GlobalWord(0xb20e);
		public GlobalWord SavedScreenStyle { get; } = new GlobalWord(0x67ec);
		public GlobalWord KingPictureLoaded { get; } = new GlobalWord(0x67ea);
		public GlobalWord AdvisorPoseShown { get; } = new GlobalWord(0x67e8);
		public GlobalWord KingScenePicture { get; } = new GlobalWord(0xb1e6);
		public GlobalWord KingFacesPicture { get; } = new GlobalWord(0x7efe);
		public GlobalWord SpaceshipPropulsion { get; } = new GlobalWord(0xdea8);
		public GlobalWord SpaceshipFuel { get; } = new GlobalWord(0xdeaa);
		public GlobalWord SpaceshipSupport { get; } = new GlobalWord(0xdeb0);
		public GlobalWord SpaceshipEnergy { get; } = new GlobalWord(0xdeb2);
		public GlobalWord SpaceshipComponentsComplete { get; } = new GlobalWord(0x687c);
		public GlobalWord SpaceshipModulesComplete { get; } = new GlobalWord(0x687e);
		public GlobalWord SpaceshipPicture { get; } = new GlobalWord(0x682a);
		public GlobalWord FileError { get; } = new GlobalWord(0x681c);
		public GlobalWord SavedHumanPlayer { get; } = new GlobalWord(0x6822);
		public GlobalWord SavedDifficulty { get; } = new GlobalWord(0x6824);
		public GlobalWord SavedYear { get; } = new GlobalWord(0x6826);
		public GlobalWord ReplayAtGameEnd { get; } = new GlobalWord(0x680e);
		public GlobalWord CivilopediaNextPage { get; } = new GlobalWord(0x6808);
		public GlobalWord SavedPointerShowLevel { get; } = new GlobalWord(0x652e);
		public GlobalWord TextCursorColumn { get; } = new GlobalWord(0x68d0);
		public GlobalWord TextCursorWidth { get; } = new GlobalWord(0x68ce);
		public GlobalWord StackDomainMode { get; } = new GlobalWord(0x6536);
		public GlobalWord StackIndexLimit { get; } = new GlobalWord(0x6538);
		public GlobalWord RoutePlaceHoriz { get; } = new GlobalWord(0x6796);
		public GlobalWord RoutePlaceVert { get; } = new GlobalWord(0x6798);
		public GlobalWord PlayerAtWar { get; } = new GlobalWord(0x6ed4);
		public GlobalWord NoConquestAnimation { get; } = new GlobalWord(0x28bc);
		public GlobalWord PalaceRightBushes { get; } = new GlobalWord(0xd934);
		public GlobalWord WonderScore { get; } = new GlobalWord(0xd764);
		public GlobalWord CitizenScore { get; } = new GlobalWord(0xd766);
		public GlobalWord MenuAnyKey { get; } = new GlobalWord(0xd206);
		public GlobalWord MenuFooter { get; } = new GlobalWord(0x2fa0);
		public GlobalWord LoadingGame { get; } = new GlobalWord(0x681a);
		public GlobalWord LoadFile { get; } = new GlobalWord(0x681e);
		public GlobalWord CivilopediaWithoutPictures { get; } = new GlobalWord(0x3c62);
		public GlobalWord CityExcessUnhappy { get; } = new GlobalWord(0x6542);
		public GlobalWord AnarchyPressure { get; } = new GlobalWord(0xe3c2);
		public GlobalWord DemocracyPressure { get; } = new GlobalWord(0xdb42);
		public GlobalWord QuietUnitSetup { get; } = new GlobalWord(0x20f4);
		public GlobalWord MapFocusLeft { get; } = new GlobalWord(0x6ed6);
		public GlobalWord MapFocusTop { get; } = new GlobalWord(0x70ea);
		public GlobalWord SpriteScreenBox { get; } = new GlobalWord(0x19fc);
		public GlobalWord LightenedPixels { get; } = new GlobalWord(0x67fe);
		public GlobalWord LowMemoryVga { get; } = new GlobalWord(0x1a3e);
		public GlobalWord PointerImage { get; } = new GlobalWord(0x5876);
		public GlobalWord PointerHotHoriz { get; } = new GlobalWord(0x5878);
		public GlobalWord PointerHotVert { get; } = new GlobalWord(0x587a);
		public GlobalWord AudienceWelcomed { get; } = new GlobalWord(0x6b8e);
		public GlobalWord KingModernPictures { get; } = new GlobalWord(0x67ee);
		public GlobalWord GovernmentPicture { get; } = new GlobalWord(0x804e);
		public GlobalWord RepaintingMainScreen { get; } = new GlobalWord(0x1c28);
		public GlobalWord CityTileDotted { get; } = new GlobalWord(0x2494);
		public GlobalWord BoxMotif { get; } = new GlobalWord(0x2f98);
		public GlobalWord MenuMouseUsed { get; } = new GlobalWord(0x2fa2);
		public GlobalWord HueCyclingOn { get; } = new GlobalWord(0x2fec);
		public GlobalWord LoadedGameMark { get; } = new GlobalWord(0x3484);
		public GlobalWord PalaceWingShift { get; } = new GlobalWord(0x4b32);
		public GlobalWord StackLeaderScore { get; } = new GlobalWord(0x6534);
		public GlobalWord KingScreenKept { get; } = new GlobalWord(0x67fa);
		public GlobalWord StoryMusicTime { get; } = new GlobalWord(0x6800);
		public GlobalWord StoryLinesTold { get; } = new GlobalWord(0x6802);
		public GlobalWord RiotShown { get; } = new GlobalWord(0x6b92);
		public GlobalWord PointerIcon { get; } = new GlobalWord(0x6e92);
		public GlobalWord LateGameNudge { get; } = new GlobalWord(0x8078);
		public GlobalWord PalaceLayout { get; } = new GlobalWord(0xb22a);
		public GlobalWord LiveRealmCount { get; } = new GlobalWord(0xde20);
		public GlobalWord DialogNeedsRedraw { get; } = new GlobalWord(0xdb38);
		public GlobalWord MenuMoreBox { get; } = new GlobalWord(0x8066);
		public GlobalWord MenuLineOffsets { get; } = new GlobalWord(0x654e);
		public GlobalWord ScoreVerticalShift { get; } = new GlobalWord(0x5116);
		public GlobalWord SchismShareDc6a { get; } = new GlobalWord(0xdc6a);
		public GlobalWord DoubleWidthText { get; } = new GlobalWord(0x6b8c);
		public GlobalWord NewspaperCrowdPicture { get; } = new GlobalWord(0xdf0c);
		public GlobalWord AudienceInitialPrompt { get; } = new GlobalWord(0xd2dc);
		public GlobalWord UnitSupportCount { get; } = new GlobalWord(0xdeb6);
		public GlobalWord ImportPlayerIndex { get; } = new GlobalWord(0x6820);
		public GlobalWord TileFortressPicture { get; } = new GlobalWord(0xd4da);
		public GlobalWord MiniSymbolPicture { get; } = new GlobalWord(0xb880);
		public GlobalWord UnhappyCitizenPicture { get; } = new GlobalWord(0x6e9e);
		public GlobalWord PollutionIconPicture { get; } = new GlobalWord(0xb2ba);
		public GlobalWord SpaceshipHabitatPicture { get; } = new GlobalWord(0x6834);
		public GlobalWord IntroReservedScreen { get; } = new GlobalWord(0x32d6);
		public GlobalWord PalaceLeftWingWidth { get; } = new GlobalWord(0x4b2c);
		public GlobalWord PalaceRightWingWidth { get; } = new GlobalWord(0x4b34);
		public GlobalWord PalaceStyleShift { get; } = new GlobalWord(0xd922);
		public GlobalWord PalaceGardenBackground { get; } = new GlobalWord(0xd932);
		public GlobalWord SavedGovernmentTitleCommunism { get; } = new GlobalWord(0x33a8);
		public GlobalWord SavedGovernmentTitleMonarchy { get; } = new GlobalWord(0x33aa);
		public GlobalWord SpaceshipStarFieldFrame { get; } = new GlobalWord(0x687a);
		public GlobalWord FallenLeadersCount { get; } = new GlobalWord(0xb1d4);

		/// <summary>All the variables, in the order of Layout</summary>
		public IEnumerable<GlobalWord> All => [ScreenBox, BackScreenBox, PictureScreenBox, CityTiles, CityOwner, FoundDistance, CityFood, CityShields, CityTrade, CityLuxuries, CityHappy, CityUnhappy, CityScience, HumanTileNear, ContentAdvances, CitySpecialists, CityScreenTab, DialogStyle, GoalHoriz, GoalVert, KingStep, TextCursor, TurnPlayer, DisabledLines, TechnologyLevel, VgaMode, KingSpeaking, FoundPlayer, MapRevealed, GameEndRequest, MousePresent, DefaultLine, MenuHelpTopic, KingAnswer, CityAttackersAway, ReplayMode, ReplayActor, MainMenuChoice, CityCorruption, CityUnitUpkeep, ChosenOrder, MapViewHoriz, MapViewVert, CitySpecialSlots, PointerShowLevel, CityTaxes, CitySettlers, StoryState, SaveDrive, StoryLineTime, MapRedrawRequest, PollutionFactor, CityAgain, WorldMapView, UnitAtTilePlayer, ArrivedSpaceship, StackKind, StackTally, ShownCity, MenuResult, GotoCost, PalaceDistance, ConquestEnd, EmissaryAudience, UnitsHidden, FirstMissingDisk, KingMood, CodeText1, CodeText2, CodeText3, CodeText4, MenuLineHeight, MenuRightEdge, MenuLineCount, PalacePartWidth, KingPicture, StackDomainTally, PopulationMark, LastSaveSlot, MidTurnSave, SpaceshipColonists, EarthGame, Climate, KingPause, ReplayCityDistance, ScoreIconHoriz, ScoreIconVert, BirthPicture, MenuWidth, MenuFirstChoice, MenuChoiceCount, MenuMeasuring, MenuFlashHue, MenuOptions, PalaceFirstPicture, PalaceLeftBushes, StoryCycleRecord, StoryCycleCount, CountLosses, StackLeader, KingPose, AudiencePlayer, AudienceWithoutPictures, ReplayFile, CityLabelCount, HumanContinentAttackers, DiskInUse, HallOfFameScore, PointerPicture, UnitLimitTold, StoryFile, StoryKeyAllowed, LandMass, Temperature, WorldAge, ScreenReserveFailed, ScreenRepaintRequest, CodeText5, EgaMode, SavedScreenStyle, KingPictureLoaded, AdvisorPoseShown, KingScenePicture, KingFacesPicture, SpaceshipPropulsion, SpaceshipFuel, SpaceshipSupport, SpaceshipEnergy, SpaceshipComponentsComplete, SpaceshipModulesComplete, SpaceshipPicture, FileError, SavedHumanPlayer, SavedDifficulty, SavedYear, ReplayAtGameEnd, CivilopediaNextPage, SavedPointerShowLevel, TextCursorColumn, TextCursorWidth, StackDomainMode, StackIndexLimit, RoutePlaceHoriz, RoutePlaceVert, PlayerAtWar, NoConquestAnimation, PalaceRightBushes, WonderScore, CitizenScore, MenuAnyKey, MenuFooter, LoadingGame, LoadFile, CivilopediaWithoutPictures, CityExcessUnhappy, AnarchyPressure, DemocracyPressure, QuietUnitSetup, MapFocusLeft, MapFocusTop, SpriteScreenBox, LightenedPixels, LowMemoryVga, PointerImage, PointerHotHoriz, PointerHotVert, AudienceWelcomed, KingModernPictures, GovernmentPicture, RepaintingMainScreen, CityTileDotted, BoxMotif, MenuMouseUsed, HueCyclingOn, LoadedGameMark, PalaceWingShift, StackLeaderScore, KingScreenKept, StoryMusicTime, StoryLinesTold, RiotShown, PointerIcon, LateGameNudge, PalaceLayout, LiveRealmCount, DialogNeedsRedraw, MenuMoreBox, MenuLineOffsets, ScoreVerticalShift, SchismShareDc6a, DoubleWidthText, NewspaperCrowdPicture, AudienceInitialPrompt, UnitSupportCount, ImportPlayerIndex, TileFortressPicture, MiniSymbolPicture, UnhappyCitizenPicture, PollutionIconPicture, SpaceshipHabitatPicture, IntroReservedScreen, PalaceLeftWingWidth, PalaceRightWingWidth, PalaceStyleShift, PalaceGardenBackground, SavedGovernmentTitleCommunism, SavedGovernmentTitleMonarchy, SpaceshipStarFieldFrame, FallenLeadersCount];
	}
}
