namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The texts of the data segment of CIV.EXE that the code passes to the text calls (the topics of the text files with "*",
	/// the names of the picture and text files, the texts drawn on the screens), named by their texts (as loaded; the
	/// same text at two addresses gets a number)
	/// </summary>
	public static class GameTexts
	{
		/// <summary>"*LOMEM"</summary>
		public static readonly GameText TopicLomem = new(0x0270, "*LOMEM");

		/// <summary>"custom.pic"</summary>
		public static readonly GameText FileCustomPic = new(0x1b3d, "custom.pic");

		/// <summary>"sp257.pic"</summary>
		public static readonly GameText FileSp257Pic = new(0x1c0c, "sp257.pic");

		/// <summary>"diffs.pic"</summary>
		public static readonly GameText FileDiffsPic = new(0x1c16, "diffs.pic");

		/// <summary>"*HELP1"</summary>
		public static readonly GameText TopicHelp1 = new(0x1e60, "*HELP1");

		/// <summary>"*HELP2"</summary>
		public static readonly GameText TopicHelp2 = new(0x1e67, "*HELP2");

		/// <summary>"*FIRSTMOVE"</summary>
		public static readonly GameText TopicFirstmove = new(0x1e6e, "*FIRSTMOVE");

		/// <summary>"cback.pic"</summary>
		public static readonly GameText FileCbackPic = new(0x1e79, "cback.pic");

		/// <summary>"*PILLAGE"</summary>
		public static readonly GameText TopicPillage = new(0x1e83, "*PILLAGE");

		/// <summary>"*SETTLERS"</summary>
		public static readonly GameText TopicSettlers = new(0x1e8c, "*SETTLERS");

		/// <summary>"*SETTLERS"</summary>
		public static readonly GameText TopicSettlersB = new(0x1e96, "*SETTLERS");

		/// <summary>"*SETTLERS"</summary>
		public static readonly GameText TopicSettlersC = new(0x1ea0, "*SETTLERS");

		/// <summary>"*NOIRR"</summary>
		public static readonly GameText TopicNoirr = new(0x1eaa, "*NOIRR");

		/// <summary>"*NOWATER"</summary>
		public static readonly GameText TopicNowater = new(0x1eb1, "*NOWATER");

		/// <summary>"*SETTLERS"</summary>
		public static readonly GameText TopicSettlersD = new(0x1eba, "*SETTLERS");

		/// <summary>"*NOMINE"</summary>
		public static readonly GameText TopicNomine = new(0x1ec4, "*NOMINE");

		/// <summary>"*SETTLERS"</summary>
		public static readonly GameText TopicSettlersE = new(0x1ecc, "*SETTLERS");

		/// <summary>"*ADDCITY"</summary>
		public static readonly GameText TopicAddcity = new(0x1ed6, "*ADDCITY");

		/// <summary>"*ZOC"</summary>
		public static readonly GameText TopicZoc = new(0x1edf, "*ZOC");

		/// <summary>"*AMPHIB"</summary>
		public static readonly GameText TopicAmphib = new(0x1f28, "*AMPHIB");

		/// <summary>"*FIGHTER"</summary>
		public static readonly GameText TopicFighter = new(0x1f30, "*FIGHTER");

		/// <summary>"*OCCUPY"</summary>
		public static readonly GameText TopicOccupy = new(0x1f62, "*OCCUPY");

		/// <summary>"*REV"</summary>
		public static readonly GameText TopicRev = new(0x1f6a, "*REV");

		/// <summary>"Leaders randomized. / "</summary>
		public static readonly GameText TextLeadersRandomized = new(0x2003, "Leaders randomized.\n");

		/// <summary>"*HELP1"</summary>
		public static readonly GameText TopicHelp1B = new(0x2029, "*HELP1");

		/// <summary>"*HELP2"</summary>
		public static readonly GameText TopicHelp2B = new(0x2030, "*HELP2");

		/// <summary>"*FUEL"</summary>
		public static readonly GameText TopicFuel = new(0x2070, "*FUEL");

		/// <summary>"*TRIREME"</summary>
		public static readonly GameText TopicTrireme = new(0x2076, "*TRIREME");

		/// <summary>"End of Turn"</summary>
		public static readonly GameText TextEndOfTurn = new(0x207f, "End of Turn");

		/// <summary>"Press Enter"</summary>
		public static readonly GameText TextPressEnter = new(0x208b, "Press Enter");

		/// <summary>"to continue"</summary>
		public static readonly GameText TextToContinue = new(0x2097, "to continue");

		/// <summary>"Veteran"</summary>
		public static readonly GameText TextVeteran = new(0x20a3, "Veteran");

		/// <summary>"+"</summary>
		public static readonly GameText TextPlus = new(0x20f1, "+");

		/// <summary>"End of Turn"</summary>
		public static readonly GameText TextEndOfTurnB = new(0x20f6, "End of Turn");

		/// <summary>"End of Turn"</summary>
		public static readonly GameText TextEndOfTurnC = new(0x2102, "End of Turn");

		/// <summary>"*FIRSTUNIT1"</summary>
		public static readonly GameText TopicFirstunit1 = new(0x210e, "*FIRSTUNIT1");

		/// <summary>"*FIRSTUNIT2"</summary>
		public static readonly GameText TopicFirstunit2 = new(0x211a, "*FIRSTUNIT2");

		/// <summary>"*UMAX"</summary>
		public static readonly GameText TopicUmax = new(0x2126, "*UMAX");

		/// <summary>"iconpg1.pal"</summary>
		public static readonly GameText FileIconpg1Pal = new(0x2311, "iconpg1.pal");

		/// <summary>"*FIRSTCIV"</summary>
		public static readonly GameText TopicFirstciv = new(0x231d, "*FIRSTCIV");

		/// <summary>"*GAPE"</summary>
		public static readonly GameText TopicGape = new(0x240e, "*GAPE");

		/// <summary>"*MONARCHY"</summary>
		public static readonly GameText TopicMonarchy = new(0x2414, "*MONARCHY");

		/// <summary>"INFO"</summary>
		public static readonly GameText TextInfo = new(0x25ea, "INFO");

		/// <summary>"HAPPY"</summary>
		public static readonly GameText TextHappy = new(0x25ef, "HAPPY");

		/// <summary>"MAP"</summary>
		public static readonly GameText TextMap = new(0x25f5, "MAP");

		/// <summary>"VIEW"</summary>
		public static readonly GameText TextView = new(0x25f9, "VIEW");

		/// <summary>"WONDERS"</summary>
		public static readonly GameText TextWonders = new(0x25fe, "WONDERS");

		/// <summary>"MORE"</summary>
		public static readonly GameText TextMore = new(0x262a, "MORE");

		/// <summary>"BUY"</summary>
		public static readonly GameText TextBuy = new(0x263c, "BUY");

		/// <summary>"Food Storage"</summary>
		public static readonly GameText TextFoodStorage = new(0x2640, "Food Storage");

		/// <summary>"City Resources"</summary>
		public static readonly GameText TextCityResources = new(0x264d, "City Resources");

		/// <summary>"EXIT"</summary>
		public static readonly GameText TextExit = new(0x2662, "EXIT");

		/// <summary>"RENAME"</summary>
		public static readonly GameText TextRename = new(0x2667, "RENAME");

		/// <summary>"*DISORDER"</summary>
		public static readonly GameText TopicDisorder = new(0x2708, "*DISORDER");

		/// <summary>"*LEADER"</summary>
		public static readonly GameText TopicLeader = new(0x2ae7, "*LEADER");

		/// <summary>"F"</summary>
		public static readonly GameText TextF = new(0x2ba0, "F");

		/// <summary>"G"</summary>
		public static readonly GameText TextG = new(0x2ba2, "G");

		/// <summary>"CityAt Error / "</summary>
		public static readonly GameText TextCityAtError = new(0x2bc6, "CityAt Error\n");

		/// <summary>"(HELP AVAILABLE)"</summary>
		public static readonly GameText TextHelpAvailable = new(0x2fbd, "(HELP AVAILABLE)");

		/// <summary>"king.txt"</summary>
		public static readonly GameText FileKingTxt = new(0x30a5, "king.txt");

		/// <summary>"logo.pic"</summary>
		public static readonly GameText FileLogoPic = new(0x328a, "logo.pic");

		/// <summary>"birth0.pic"</summary>
		public static readonly GameText FileBirth0Pic = new(0x3293, "birth0.pic");

		/// <summary>"birth1.pic"</summary>
		public static readonly GameText FileBirth1Pic = new(0x329e, "birth1.pic");

		/// <summary>"logo.pic"</summary>
		public static readonly GameText FileLogoPicB = new(0x32a9, "logo.pic");

		/// <summary>"planet2.pic"</summary>
		public static readonly GameText FilePlanet2Pic = new(0x32d8, "planet2.pic");

		/// <summary>"planet1.pic"</summary>
		public static readonly GameText FilePlanet1Pic = new(0x32e4, "planet1.pic");

		/// <summary>"king.txt"</summary>
		public static readonly GameText FileKingTxtB = new(0x3301, "king.txt");

		/// <summary>"spacest.pic"</summary>
		public static readonly GameText FileSpacestPic = new(0x330a, "spacest.pic");

		/// <summary>"spacest.pic"</summary>
		public static readonly GameText FileSpacestPicB = new(0x3316, "spacest.pic");

		/// <summary>"sprites.pic"</summary>
		public static readonly GameText FileSpritesPic = new(0x3322, "sprites.pic");

		/// <summary>"arch.pic"</summary>
		public static readonly GameText FileArchPic = new(0x332e, "arch.pic");

		/// <summary>"*ARCH"</summary>
		public static readonly GameText TopicArch = new(0x3342, "*ARCH");

		/// <summary>"king.txt"</summary>
		public static readonly GameText FileKingTxtC = new(0x3348, "king.txt");

		/// <summary>"*INIT"</summary>
		public static readonly GameText TopicInit = new(0x35c3, "*INIT");

		/// <summary>"sp299.pic"</summary>
		public static readonly GameText FileSp299Pic = new(0x36ff, "sp299.pic");

		/// <summary>"ter257.pic"</summary>
		public static readonly GameText FileTer257Pic = new(0x3709, "ter257.pic");

		/// <summary>"sprites.pic"</summary>
		public static readonly GameText FileSpritesPicB = new(0x3714, "sprites.pic");

		/// <summary>"sp257.pic"</summary>
		public static readonly GameText FileSp257PicB = new(0x3720, "sp257.pic");

		/// <summary>"sp256.pal"</summary>
		public static readonly GameText FileSp256Pal = new(0x372a, "sp256.pal");

		/// <summary>"New Cabinet:"</summary>
		public static readonly GameText TextNewCabinet = new(0x375a, "New Cabinet:");

		/// <summary>"*EXCHANGE"</summary>
		public static readonly GameText TopicExchange = new(0x3776, "*EXCHANGE");

		/// <summary>"*CANCEL"</summary>
		public static readonly GameText TopicCancel = new(0x378f, "*CANCEL");

		/// <summary>"*TRIBUTE"</summary>
		public static readonly GameText TopicTribute = new(0x3797, "*TRIBUTE");

		/// <summary>"*TAKECIV"</summary>
		public static readonly GameText TopicTakeciv = new(0x37a0, "*TAKECIV");

		/// <summary>"*PEACE"</summary>
		public static readonly GameText TopicPeace = new(0x37cd, "*PEACE");

		/// <summary>"*GIVECIV"</summary>
		public static readonly GameText TopicGiveciv = new(0x3805, "*GIVECIV");

		/// <summary>"*GIVECASH"</summary>
		public static readonly GameText TopicGivecash = new(0x380e, "*GIVECASH");

		/// <summary>"*GROVEL"</summary>
		public static readonly GameText TopicGrovel = new(0x3818, "*GROVEL");

		/// <summary>"*MOBILIZE"</summary>
		public static readonly GameText TopicMobilize = new(0x3820, "*MOBILIZE");

		/// <summary>"*ATTITUDE"</summary>
		public static readonly GameText TopicAttitude = new(0x382a, "*ATTITUDE");

		/// <summary>"*THREAT"</summary>
		public static readonly GameText TopicThreat = new(0x388f, "*THREAT");

		/// <summary>"*GIVEMORE"</summary>
		public static readonly GameText TopicGivemore = new(0x3897, "*GIVEMORE");

		/// <summary>"*PROVOKE"</summary>
		public static readonly GameText TopicProvoke = new(0x38a1, "*PROVOKE");

		/// <summary>"*NOCONTACT"</summary>
		public static readonly GameText TopicNocontact = new(0x38e9, "*NOCONTACT");

		/// <summary>"*ATWAR"</summary>
		public static readonly GameText TopicAtwar = new(0x38f4, "*ATWAR");

		/// <summary>"*MERCENARY"</summary>
		public static readonly GameText TopicMercenary = new(0x38fb, "*MERCENARY");

		/// <summary>"*UNFORTUNATE"</summary>
		public static readonly GameText TopicUnfortunate = new(0x391e, "*UNFORTUNATE");

		/// <summary>"*SCHISM"</summary>
		public static readonly GameText TopicSchism = new(0x392b, "*SCHISM");

		/// <summary>"*TREATY"</summary>
		public static readonly GameText TopicTreaty = new(0x3a8c, "*TREATY");

		/// <summary>"map.pic"</summary>
		public static readonly GameText FileMapPic = new(0x3b20, "map.pic");

		/// <summary>"BUILDING NEW WORLD..."</summary>
		public static readonly GameText TextBuildingNewWorld = new(0x3b28, "BUILDING NEW WORLD...");

		/// <summary>"ENCYCLOPAEDIA of CIVILIZATION"</summary>
		public static readonly GameText TextEncyclopaediaOfCivilization = new(0x3c90, "ENCYCLOPAEDIA of CIVILIZATION");

		/// <summary>"ENCYCLOPAEDIA of CIVILIZATION"</summary>
		public static readonly GameText TextEncyclopaediaOfCivilizationB = new(0x3cae, "ENCYCLOPAEDIA of CIVILIZATION");

		/// <summary>"EXIT"</summary>
		public static readonly GameText TextExitB = new(0x3ccc, "EXIT");

		/// <summary>"MORE"</summary>
		public static readonly GameText TextMoreB = new(0x3cd1, "MORE");

		/// <summary>"Allows: "</summary>
		public static readonly GameText TextAllows = new(0x3dad, "Allows: ");

		/// <summary>"Allows: "</summary>
		public static readonly GameText TextAllowsB = new(0x3de5, "Allows: ");

		/// <summary>"nothing"</summary>
		public static readonly GameText TextNothing = new(0x3f20, "nothing");

		/// <summary>"*  -1 if government is Despotism/Anarchy."</summary>
		public static readonly GameText Topic1IfGovernmentIsDespotismAnarchy = new(0x3f28, "*  -1 if government is Despotism/Anarchy.");

		/// <summary>"%  +1 if government is Republic/Democracy."</summary>
		public static readonly GameText Text1IfGovernmentIsRepublicDemocracy = new(0x3f52, "%  +1 if government is Republic/Democracy.");

		/// <summary>"See Replay? /  No replay /  Quick Replay /  Complete Replay /  Write Replay to Disk / "</summary>
		public static readonly GameText TextSeeReplayNoReplayQuickReplayCompleteRepl = new(0x3fb0, "See Replay?\n No replay\n Quick Replay\n Complete Replay\n Write Replay to Disk\n");

		/// <summary>"slam1.pic"</summary>
		public static readonly GameText FileSlam1Pic = new(0x4112, "slam1.pic");

		/// <summary>"slam1.pal"</summary>
		public static readonly GameText FileSlam1Pal = new(0x412a, "slam1.pal");

		/// <summary>"slam1.pic"</summary>
		public static readonly GameText FileSlam1PicB = new(0x4134, "slam1.pic");

		/// <summary>"CIVILIZATION POWERGraph"</summary>
		public static readonly GameText TextCivilizationPOWERGraph = new(0x43d8, "CIVILIZATION POWERGraph");

		/// <summary>"Approval Rating:"</summary>
		public static readonly GameText TextApprovalRating = new(0x4544, "Approval Rating:");

		/// <summary>"Population:"</summary>
		public static readonly GameText TextPopulation = new(0x4559, "Population:");

		/// <summary>"GNP:"</summary>
		public static readonly GameText TextGnp = new(0x456a, "GNP:");

		/// <summary>"Mfg. Goods:"</summary>
		public static readonly GameText TextMfgGoods = new(0x457d, "Mfg. Goods:");

		/// <summary>"Land Area:"</summary>
		public static readonly GameText TextLandArea = new(0x4597, "Land Area:");

		/// <summary>"Literacy:"</summary>
		public static readonly GameText TextLiteracy = new(0x45b5, "Literacy:");

		/// <summary>"Disease:"</summary>
		public static readonly GameText TextDisease = new(0x45c3, "Disease:");

		/// <summary>"Pollution:"</summary>
		public static readonly GameText TextPollution = new(0x45d0, "Pollution:");

		/// <summary>"Life Expectancy:"</summary>
		public static readonly GameText TextLifeExpectancy = new(0x45ee, "Life Expectancy:");

		/// <summary>"Family Size:"</summary>
		public static readonly GameText TextFamilySize = new(0x460b, "Family Size:");

		/// <summary>"Military Service:"</summary>
		public static readonly GameText TextMilitaryService = new(0x4625, "Military Service:");

		/// <summary>"Annual Income:"</summary>
		public static readonly GameText TextAnnualIncome = new(0x4643, "Annual Income:");

		/// <summary>"Productivity:"</summary>
		public static readonly GameText TextProductivity = new(0x4661, "Productivity:");

		/// <summary>"SCIENCE REPORT"</summary>
		public static readonly GameText TextScienceReport = new(0x4762, "SCIENCE REPORT");

		/// <summary>"MILITARY STATUS"</summary>
		public static readonly GameText TextMilitaryStatus = new(0x477e, "MILITARY STATUS");

		/// <summary>"MILITARY LOSSES"</summary>
		public static readonly GameText TextMilitaryLosses = new(0x47ad, "MILITARY LOSSES");

		/// <summary>"TRADE REPORT"</summary>
		public static readonly GameText TextTradeReport = new(0x47bd, "TRADE REPORT");

		/// <summary>"City Trade"</summary>
		public static readonly GameText TextCityTrade = new(0x47ca, "City Trade");

		/// <summary>"TRADE REPORT"</summary>
		public static readonly GameText TextTradeReportB = new(0x47d5, "TRADE REPORT");

		/// <summary>"Maintenance Costs"</summary>
		public static readonly GameText TextMaintenanceCosts = new(0x481f, "Maintenance Costs");

		/// <summary>"INTELLIGENCE REPORT"</summary>
		public static readonly GameText TextIntelligenceReport = new(0x4847, "INTELLIGENCE REPORT");

		/// <summary>"No embassy established."</summary>
		public static readonly GameText TextNoEmbassyEstablished = new(0x485b, "No embassy established.");

		/// <summary>"INTELLIGENCE REPORT"</summary>
		public static readonly GameText TextIntelligenceReportB = new(0x48a5, "INTELLIGENCE REPORT");

		/// <summary>"Foreign Affairs:"</summary>
		public static readonly GameText TextForeignAffairs = new(0x4956, "Foreign Affairs:");

		/// <summary>"Technologies:"</summary>
		public static readonly GameText TextTechnologies = new(0x4987, "Technologies:");

		/// <summary>"ATTITUDE SURVEY"</summary>
		public static readonly GameText TextAttitudeSurvey = new(0x4995, "ATTITUDE SURVEY");

		/// <summary>"ATTITUDE SURVEY"</summary>
		public static readonly GameText TextAttitudeSurveyB = new(0x49a5, "ATTITUDE SURVEY");

		/// <summary>"CITY STATUS"</summary>
		public static readonly GameText TextCityStatus = new(0x49e4, "CITY STATUS");

		/// <summary>"CITY STATUS"</summary>
		public static readonly GameText TextCityStatusB = new(0x49f0, "CITY STATUS");

		/// <summary>"hill.pic"</summary>
		public static readonly GameText FileHillPic = new(0x4e98, "hill.pic");

		/// <summary>"hill.pic"</summary>
		public static readonly GameText FileHillPicB = new(0x4ea1, "hill.pic");

		/// <summary>"hill.pal"</summary>
		public static readonly GameText FileHillPal = new(0x4eaa, "hill.pal");

		/// <summary>"pop.pic"</summary>
		public static readonly GameText FilePopPic = new(0x4ebc, "pop.pic");

		/// <summary>"riot.pic"</summary>
		public static readonly GameText FileRiotPic = new(0x4f2f, "riot.pic");

		/// <summary>"riot2.pic"</summary>
		public static readonly GameText FileRiot2Pic = new(0x4f38, "riot2.pic");

		/// <summary>"*FIRSTPRODUCT"</summary>
		public static readonly GameText TopicFirstproduct = new(0x4f66, "*FIRSTPRODUCT");

		/// <summary>"*PMAX"</summary>
		public static readonly GameText TopicPmax = new(0x4f74, "*PMAX");

		/// <summary>"*D1"</summary>
		public static readonly GameText TopicD1 = new(0x4f99, "*D1");

		/// <summary>"*D2"</summary>
		public static readonly GameText TopicD2 = new(0x4f9d, "*D2");

		/// <summary>"*D3"</summary>
		public static readonly GameText TopicD3 = new(0x4fa1, "*D3");

		/// <summary>"*D4"</summary>
		public static readonly GameText TopicD4 = new(0x4fa5, "*D4");

		/// <summary>"*D5"</summary>
		public static readonly GameText TopicD5 = new(0x4fc3, "*D5");

		/// <summary>"*D6"</summary>
		public static readonly GameText TopicD6 = new(0x4fc7, "*D6");

		/// <summary>"CIVILIZATION SCORE"</summary>
		public static readonly GameText TextCivilizationScore = new(0x506b, "CIVILIZATION SCORE");

		/// <summary>"SCORING COMPLETED"</summary>
		public static readonly GameText TextScoringCompleted = new(0x507e, "SCORING COMPLETED");

		/// <summary>"No new technology found. / "</summary>
		public static readonly GameText TextNoNewTechnologyFound = new(0x5231, "No new technology found.\n");

		/// <summary>"nuke1.pic"</summary>
		public static readonly GameText FileNuke1Pic = new(0x5318, "nuke1.pic");

		/// <summary>"settlers.pic"</summary>
		public static readonly GameText FileSettlersPic = new(0x536e, "settlers.pic");

		/// <summary>"hill.pic"</summary>
		public static readonly GameText FileHillPicC = new(0x537b, "hill.pic");

		/// <summary>"hill.pic"</summary>
		public static readonly GameText FileHillPicD = new(0x5384, "hill.pic");

		/// <summary>"City Name..."</summary>
		public static readonly GameText TextCityName = new(0x539e, "City Name...");

		/// <summary>"Your Name..."</summary>
		public static readonly GameText TextYourName = new(0x53ab, "Your Name...");

		/// <summary>"Name of your Tribe..."</summary>
		public static readonly GameText TextNameOfYourTribe = new(0x53b8, "Name of your Tribe...");

		/// <summary>"Unknown city."</summary>
		public static readonly GameText TextUnknownCity = new(0x53f3, "Unknown city.");
	}
}
