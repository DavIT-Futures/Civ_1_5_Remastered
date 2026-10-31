namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The texts of the data segment of CIV.EXE that the code passes to the text calls (the topics of the text files with "*",
	/// the names of the picture and text files, the texts drawn on the screens), named by their texts (as loaded; the
	/// same text at two addresses gets a number)
	/// </summary>
	public static class GameTexts
	{
		/// <summary>"*LOMEM"</summary>
		public static GameText TopicLomem { get; } = new(0x0270, "*LOMEM");

		/// <summary>"custom.pic"</summary>
		public static GameText FileCustomPic { get; } = new(0x1b3d, "custom.pic");

		/// <summary>"sp257.pic"</summary>
		public static GameText FileSp257Pic { get; } = new(0x1c0c, "sp257.pic");

		/// <summary>"diffs.pic"</summary>
		public static GameText FileDiffsPic { get; } = new(0x1c16, "diffs.pic");

		/// <summary>"*HELP1"</summary>
		public static GameText TopicHelp1 { get; } = new(0x1e60, "*HELP1");

		/// <summary>"*HELP2"</summary>
		public static GameText TopicHelp2 { get; } = new(0x1e67, "*HELP2");

		/// <summary>"*FIRSTMOVE"</summary>
		public static GameText TopicFirstmove { get; } = new(0x1e6e, "*FIRSTMOVE");

		/// <summary>"cback.pic"</summary>
		public static GameText FileCbackPic { get; } = new(0x1e79, "cback.pic");

		/// <summary>"*PILLAGE"</summary>
		public static GameText TopicPillage { get; } = new(0x1e83, "*PILLAGE");

		/// <summary>"*SETTLERS"</summary>
		public static GameText TopicSettlers { get; } = new(0x1e8c, "*SETTLERS");

		/// <summary>"*SETTLERS"</summary>
		public static GameText TopicSettlersB { get; } = new(0x1e96, "*SETTLERS");

		/// <summary>"*SETTLERS"</summary>
		public static GameText TopicSettlersC { get; } = new(0x1ea0, "*SETTLERS");

		/// <summary>"*NOIRR"</summary>
		public static GameText TopicNoirr { get; } = new(0x1eaa, "*NOIRR");

		/// <summary>"*NOWATER"</summary>
		public static GameText TopicNowater { get; } = new(0x1eb1, "*NOWATER");

		/// <summary>"*SETTLERS"</summary>
		public static GameText TopicSettlersD { get; } = new(0x1eba, "*SETTLERS");

		/// <summary>"*NOMINE"</summary>
		public static GameText TopicNomine { get; } = new(0x1ec4, "*NOMINE");

		/// <summary>"*SETTLERS"</summary>
		public static GameText TopicSettlersE { get; } = new(0x1ecc, "*SETTLERS");

		/// <summary>"*ADDCITY"</summary>
		public static GameText TopicAddcity { get; } = new(0x1ed6, "*ADDCITY");

		/// <summary>"*ZOC"</summary>
		public static GameText TopicZoc { get; } = new(0x1edf, "*ZOC");

		/// <summary>"*AMPHIB"</summary>
		public static GameText TopicAmphib { get; } = new(0x1f28, "*AMPHIB");

		/// <summary>"*FIGHTER"</summary>
		public static GameText TopicFighter { get; } = new(0x1f30, "*FIGHTER");

		/// <summary>"*OCCUPY"</summary>
		public static GameText TopicOccupy { get; } = new(0x1f62, "*OCCUPY");

		/// <summary>"*REV"</summary>
		public static GameText TopicRev { get; } = new(0x1f6a, "*REV");

		/// <summary>"Leaders randomized. / "</summary>
		public static GameText TextLeadersRandomized { get; } = new(0x2003, "Leaders randomized.\n");

		/// <summary>"*HELP1"</summary>
		public static GameText TopicHelp1B { get; } = new(0x2029, "*HELP1");

		/// <summary>"*HELP2"</summary>
		public static GameText TopicHelp2B { get; } = new(0x2030, "*HELP2");

		/// <summary>"*FUEL"</summary>
		public static GameText TopicFuel { get; } = new(0x2070, "*FUEL");

		/// <summary>"*TRIREME"</summary>
		public static GameText TopicTrireme { get; } = new(0x2076, "*TRIREME");

		/// <summary>"End of Turn"</summary>
		public static GameText TextEndOfTurn { get; } = new(0x207f, "End of Turn");

		/// <summary>"Press Enter"</summary>
		public static GameText TextPressEnter { get; } = new(0x208b, "Press Enter");

		/// <summary>"to continue"</summary>
		public static GameText TextToContinue { get; } = new(0x2097, "to continue");

		/// <summary>"Veteran"</summary>
		public static GameText TextVeteran { get; } = new(0x20a3, "Veteran");

		/// <summary>"+"</summary>
		public static GameText TextPlus { get; } = new(0x20f1, "+");

		/// <summary>"End of Turn"</summary>
		public static GameText TextEndOfTurnB { get; } = new(0x20f6, "End of Turn");

		/// <summary>"End of Turn"</summary>
		public static GameText TextEndOfTurnC { get; } = new(0x2102, "End of Turn");

		/// <summary>"*FIRSTUNIT1"</summary>
		public static GameText TopicFirstunit1 { get; } = new(0x210e, "*FIRSTUNIT1");

		/// <summary>"*FIRSTUNIT2"</summary>
		public static GameText TopicFirstunit2 { get; } = new(0x211a, "*FIRSTUNIT2");

		/// <summary>"*UMAX"</summary>
		public static GameText TopicUmax { get; } = new(0x2126, "*UMAX");

		/// <summary>"iconpg1.pal"</summary>
		public static GameText FileIconpg1Pal { get; } = new(0x2311, "iconpg1.pal");

		/// <summary>"*FIRSTCIV"</summary>
		public static GameText TopicFirstciv { get; } = new(0x231d, "*FIRSTCIV");

		/// <summary>"*GAPE"</summary>
		public static GameText TopicGape { get; } = new(0x240e, "*GAPE");

		/// <summary>"*MONARCHY"</summary>
		public static GameText TopicMonarchy { get; } = new(0x2414, "*MONARCHY");

		/// <summary>"INFO"</summary>
		public static GameText TextInfo { get; } = new(0x25ea, "INFO");

		/// <summary>"HAPPY"</summary>
		public static GameText TextHappy { get; } = new(0x25ef, "HAPPY");

		/// <summary>"MAP"</summary>
		public static GameText TextMap { get; } = new(0x25f5, "MAP");

		/// <summary>"VIEW"</summary>
		public static GameText TextView { get; } = new(0x25f9, "VIEW");

		/// <summary>"WONDERS"</summary>
		public static GameText TextWonders { get; } = new(0x25fe, "WONDERS");

		/// <summary>"MORE"</summary>
		public static GameText TextMore { get; } = new(0x262a, "MORE");

		/// <summary>"BUY"</summary>
		public static GameText TextBuy { get; } = new(0x263c, "BUY");

		/// <summary>"Food Storage"</summary>
		public static GameText TextFoodStorage { get; } = new(0x2640, "Food Storage");

		/// <summary>"City Resources"</summary>
		public static GameText TextCityResources { get; } = new(0x264d, "City Resources");

		/// <summary>"EXIT"</summary>
		public static GameText TextExit { get; } = new(0x2662, "EXIT");

		/// <summary>"RENAME"</summary>
		public static GameText TextRename { get; } = new(0x2667, "RENAME");

		/// <summary>"*DISORDER"</summary>
		public static GameText TopicDisorder { get; } = new(0x2708, "*DISORDER");

		/// <summary>"*LEADER"</summary>
		public static GameText TopicLeader { get; } = new(0x2ae7, "*LEADER");

		/// <summary>"F"</summary>
		public static GameText TextF { get; } = new(0x2ba0, "F");

		/// <summary>"G"</summary>
		public static GameText TextG { get; } = new(0x2ba2, "G");

		/// <summary>"CityAt Error / "</summary>
		public static GameText TextCityAtError { get; } = new(0x2bc6, "CityAt Error\n");

		/// <summary>"(HELP AVAILABLE)"</summary>
		public static GameText TextHelpAvailable { get; } = new(0x2fbd, "(HELP AVAILABLE)");

		/// <summary>"king.txt"</summary>
		public static GameText FileKingTxt { get; } = new(0x30a5, "king.txt");

		/// <summary>"logo.pic"</summary>
		public static GameText FileLogoPic { get; } = new(0x328a, "logo.pic");

		/// <summary>"birth0.pic"</summary>
		public static GameText FileBirth0Pic { get; } = new(0x3293, "birth0.pic");

		/// <summary>"birth1.pic"</summary>
		public static GameText FileBirth1Pic { get; } = new(0x329e, "birth1.pic");

		/// <summary>"logo.pic"</summary>
		public static GameText FileLogoPicB { get; } = new(0x32a9, "logo.pic");

		/// <summary>"planet2.pic"</summary>
		public static GameText FilePlanet2Pic { get; } = new(0x32d8, "planet2.pic");

		/// <summary>"planet1.pic"</summary>
		public static GameText FilePlanet1Pic { get; } = new(0x32e4, "planet1.pic");

		/// <summary>"king.txt"</summary>
		public static GameText FileKingTxtB { get; } = new(0x3301, "king.txt");

		/// <summary>"spacest.pic"</summary>
		public static GameText FileSpacestPic { get; } = new(0x330a, "spacest.pic");

		/// <summary>"spacest.pic"</summary>
		public static GameText FileSpacestPicB { get; } = new(0x3316, "spacest.pic");

		/// <summary>"sprites.pic"</summary>
		public static GameText FileSpritesPic { get; } = new(0x3322, "sprites.pic");

		/// <summary>"arch.pic"</summary>
		public static GameText FileArchPic { get; } = new(0x332e, "arch.pic");

		/// <summary>"*ARCH"</summary>
		public static GameText TopicArch { get; } = new(0x3342, "*ARCH");

		/// <summary>"king.txt"</summary>
		public static GameText FileKingTxtC { get; } = new(0x3348, "king.txt");

		/// <summary>"*INIT"</summary>
		public static GameText TopicInit { get; } = new(0x35c3, "*INIT");

		/// <summary>"sp299.pic"</summary>
		public static GameText FileSp299Pic { get; } = new(0x36ff, "sp299.pic");

		/// <summary>"ter257.pic"</summary>
		public static GameText FileTer257Pic { get; } = new(0x3709, "ter257.pic");

		/// <summary>"sprites.pic"</summary>
		public static GameText FileSpritesPicB { get; } = new(0x3714, "sprites.pic");

		/// <summary>"sp257.pic"</summary>
		public static GameText FileSp257PicB { get; } = new(0x3720, "sp257.pic");

		/// <summary>"sp256.pal"</summary>
		public static GameText FileSp256Pal { get; } = new(0x372a, "sp256.pal");

		/// <summary>"New Cabinet:"</summary>
		public static GameText TextNewCabinet { get; } = new(0x375a, "New Cabinet:");

		/// <summary>"*EXCHANGE"</summary>
		public static GameText TopicExchange { get; } = new(0x3776, "*EXCHANGE");

		/// <summary>"*CANCEL"</summary>
		public static GameText TopicCancel { get; } = new(0x378f, "*CANCEL");

		/// <summary>"*TRIBUTE"</summary>
		public static GameText TopicTribute { get; } = new(0x3797, "*TRIBUTE");

		/// <summary>"*TAKECIV"</summary>
		public static GameText TopicTakeciv { get; } = new(0x37a0, "*TAKECIV");

		/// <summary>"*PEACE"</summary>
		public static GameText TopicPeace { get; } = new(0x37cd, "*PEACE");

		/// <summary>"*GIVECIV"</summary>
		public static GameText TopicGiveciv { get; } = new(0x3805, "*GIVECIV");

		/// <summary>"*GIVECASH"</summary>
		public static GameText TopicGivecash { get; } = new(0x380e, "*GIVECASH");

		/// <summary>"*GROVEL"</summary>
		public static GameText TopicGrovel { get; } = new(0x3818, "*GROVEL");

		/// <summary>"*MOBILIZE"</summary>
		public static GameText TopicMobilize { get; } = new(0x3820, "*MOBILIZE");

		/// <summary>"*ATTITUDE"</summary>
		public static GameText TopicAttitude { get; } = new(0x382a, "*ATTITUDE");

		/// <summary>"*THREAT"</summary>
		public static GameText TopicThreat { get; } = new(0x388f, "*THREAT");

		/// <summary>"*GIVEMORE"</summary>
		public static GameText TopicGivemore { get; } = new(0x3897, "*GIVEMORE");

		/// <summary>"*PROVOKE"</summary>
		public static GameText TopicProvoke { get; } = new(0x38a1, "*PROVOKE");

		/// <summary>"*NOCONTACT"</summary>
		public static GameText TopicNocontact { get; } = new(0x38e9, "*NOCONTACT");

		/// <summary>"*ATWAR"</summary>
		public static GameText TopicAtwar { get; } = new(0x38f4, "*ATWAR");

		/// <summary>"*MERCENARY"</summary>
		public static GameText TopicMercenary { get; } = new(0x38fb, "*MERCENARY");

		/// <summary>"*UNFORTUNATE"</summary>
		public static GameText TopicUnfortunate { get; } = new(0x391e, "*UNFORTUNATE");

		/// <summary>"*SCHISM"</summary>
		public static GameText TopicSchism { get; } = new(0x392b, "*SCHISM");

		/// <summary>"*TREATY"</summary>
		public static GameText TopicTreaty { get; } = new(0x3a8c, "*TREATY");

		/// <summary>"map.pic"</summary>
		public static GameText FileMapPic { get; } = new(0x3b20, "map.pic");

		/// <summary>"BUILDING NEW WORLD..."</summary>
		public static GameText TextBuildingNewWorld { get; } = new(0x3b28, "BUILDING NEW WORLD...");

		/// <summary>"ENCYCLOPAEDIA of CIVILIZATION"</summary>
		public static GameText TextEncyclopaediaOfCivilization { get; } = new(0x3c90, "ENCYCLOPAEDIA of CIVILIZATION");

		/// <summary>"ENCYCLOPAEDIA of CIVILIZATION"</summary>
		public static GameText TextEncyclopaediaOfCivilizationB { get; } = new(0x3cae, "ENCYCLOPAEDIA of CIVILIZATION");

		/// <summary>"EXIT"</summary>
		public static GameText TextExitB { get; } = new(0x3ccc, "EXIT");

		/// <summary>"MORE"</summary>
		public static GameText TextMoreB { get; } = new(0x3cd1, "MORE");

		/// <summary>"Allows: "</summary>
		public static GameText TextAllows { get; } = new(0x3dad, "Allows: ");

		/// <summary>"Allows: "</summary>
		public static GameText TextAllowsB { get; } = new(0x3de5, "Allows: ");

		/// <summary>"nothing"</summary>
		public static GameText TextNothing { get; } = new(0x3f20, "nothing");

		/// <summary>"*  -1 if government is Despotism/Anarchy."</summary>
		public static GameText Topic1IfGovernmentIsDespotismAnarchy { get; } = new(0x3f28, "*  -1 if government is Despotism/Anarchy.");

		/// <summary>"%  +1 if government is Republic/Democracy."</summary>
		public static GameText Text1IfGovernmentIsRepublicDemocracy { get; } = new(0x3f52, "%  +1 if government is Republic/Democracy.");

		/// <summary>"See Replay? /  No replay /  Quick Replay /  Complete Replay /  Write Replay to Disk / "</summary>
		public static GameText TextSeeReplayNoReplayQuickReplayCompleteRepl { get; } = new(0x3fb0, "See Replay?\n No replay\n Quick Replay\n Complete Replay\n Write Replay to Disk\n");

		/// <summary>"slam1.pic"</summary>
		public static GameText FileSlam1Pic { get; } = new(0x4112, "slam1.pic");

		/// <summary>"slam1.pal"</summary>
		public static GameText FileSlam1Pal { get; } = new(0x412a, "slam1.pal");

		/// <summary>"slam1.pic"</summary>
		public static GameText FileSlam1PicB { get; } = new(0x4134, "slam1.pic");

		/// <summary>"CIVILIZATION POWERGraph"</summary>
		public static GameText TextCivilizationPOWERGraph { get; } = new(0x43d8, "CIVILIZATION POWERGraph");

		/// <summary>"Approval Rating:"</summary>
		public static GameText TextApprovalRating { get; } = new(0x4544, "Approval Rating:");

		/// <summary>"Population:"</summary>
		public static GameText TextPopulation { get; } = new(0x4559, "Population:");

		/// <summary>"GNP:"</summary>
		public static GameText TextGnp { get; } = new(0x456a, "GNP:");

		/// <summary>"Mfg. Goods:"</summary>
		public static GameText TextMfgGoods { get; } = new(0x457d, "Mfg. Goods:");

		/// <summary>"Land Area:"</summary>
		public static GameText TextLandArea { get; } = new(0x4597, "Land Area:");

		/// <summary>"Literacy:"</summary>
		public static GameText TextLiteracy { get; } = new(0x45b5, "Literacy:");

		/// <summary>"Disease:"</summary>
		public static GameText TextDisease { get; } = new(0x45c3, "Disease:");

		/// <summary>"Pollution:"</summary>
		public static GameText TextPollution { get; } = new(0x45d0, "Pollution:");

		/// <summary>"Life Expectancy:"</summary>
		public static GameText TextLifeExpectancy { get; } = new(0x45ee, "Life Expectancy:");

		/// <summary>"Family Size:"</summary>
		public static GameText TextFamilySize { get; } = new(0x460b, "Family Size:");

		/// <summary>"Military Service:"</summary>
		public static GameText TextMilitaryService { get; } = new(0x4625, "Military Service:");

		/// <summary>"Annual Income:"</summary>
		public static GameText TextAnnualIncome { get; } = new(0x4643, "Annual Income:");

		/// <summary>"Productivity:"</summary>
		public static GameText TextProductivity { get; } = new(0x4661, "Productivity:");

		/// <summary>"SCIENCE REPORT"</summary>
		public static GameText TextScienceReport { get; } = new(0x4762, "SCIENCE REPORT");

		/// <summary>"MILITARY STATUS"</summary>
		public static GameText TextMilitaryStatus { get; } = new(0x477e, "MILITARY STATUS");

		/// <summary>"MILITARY LOSSES"</summary>
		public static GameText TextMilitaryLosses { get; } = new(0x47ad, "MILITARY LOSSES");

		/// <summary>"TRADE REPORT"</summary>
		public static GameText TextTradeReport { get; } = new(0x47bd, "TRADE REPORT");

		/// <summary>"City Trade"</summary>
		public static GameText TextCityTrade { get; } = new(0x47ca, "City Trade");

		/// <summary>"TRADE REPORT"</summary>
		public static GameText TextTradeReportB { get; } = new(0x47d5, "TRADE REPORT");

		/// <summary>"Maintenance Costs"</summary>
		public static GameText TextMaintenanceCosts { get; } = new(0x481f, "Maintenance Costs");

		/// <summary>"INTELLIGENCE REPORT"</summary>
		public static GameText TextIntelligenceReport { get; } = new(0x4847, "INTELLIGENCE REPORT");

		/// <summary>"No embassy established."</summary>
		public static GameText TextNoEmbassyEstablished { get; } = new(0x485b, "No embassy established.");

		/// <summary>"INTELLIGENCE REPORT"</summary>
		public static GameText TextIntelligenceReportB { get; } = new(0x48a5, "INTELLIGENCE REPORT");

		/// <summary>"Foreign Affairs:"</summary>
		public static GameText TextForeignAffairs { get; } = new(0x4956, "Foreign Affairs:");

		/// <summary>"Technologies:"</summary>
		public static GameText TextTechnologies { get; } = new(0x4987, "Technologies:");

		/// <summary>"ATTITUDE SURVEY"</summary>
		public static GameText TextAttitudeSurvey { get; } = new(0x4995, "ATTITUDE SURVEY");

		/// <summary>"ATTITUDE SURVEY"</summary>
		public static GameText TextAttitudeSurveyB { get; } = new(0x49a5, "ATTITUDE SURVEY");

		/// <summary>"CITY STATUS"</summary>
		public static GameText TextCityStatus { get; } = new(0x49e4, "CITY STATUS");

		/// <summary>"CITY STATUS"</summary>
		public static GameText TextCityStatusB { get; } = new(0x49f0, "CITY STATUS");

		/// <summary>"hill.pic"</summary>
		public static GameText FileHillPic { get; } = new(0x4e98, "hill.pic");

		/// <summary>"hill.pic"</summary>
		public static GameText FileHillPicB { get; } = new(0x4ea1, "hill.pic");

		/// <summary>"hill.pal"</summary>
		public static GameText FileHillPal { get; } = new(0x4eaa, "hill.pal");

		/// <summary>"pop.pic"</summary>
		public static GameText FilePopPic { get; } = new(0x4ebc, "pop.pic");

		/// <summary>"riot.pic"</summary>
		public static GameText FileRiotPic { get; } = new(0x4f2f, "riot.pic");

		/// <summary>"riot2.pic"</summary>
		public static GameText FileRiot2Pic { get; } = new(0x4f38, "riot2.pic");

		/// <summary>"*FIRSTPRODUCT"</summary>
		public static GameText TopicFirstproduct { get; } = new(0x4f66, "*FIRSTPRODUCT");

		/// <summary>"*PMAX"</summary>
		public static GameText TopicPmax { get; } = new(0x4f74, "*PMAX");

		/// <summary>"*D1"</summary>
		public static GameText TopicD1 { get; } = new(0x4f99, "*D1");

		/// <summary>"*D2"</summary>
		public static GameText TopicD2 { get; } = new(0x4f9d, "*D2");

		/// <summary>"*D3"</summary>
		public static GameText TopicD3 { get; } = new(0x4fa1, "*D3");

		/// <summary>"*D4"</summary>
		public static GameText TopicD4 { get; } = new(0x4fa5, "*D4");

		/// <summary>"*D5"</summary>
		public static GameText TopicD5 { get; } = new(0x4fc3, "*D5");

		/// <summary>"*D6"</summary>
		public static GameText TopicD6 { get; } = new(0x4fc7, "*D6");

		/// <summary>"CIVILIZATION SCORE"</summary>
		public static GameText TextCivilizationScore { get; } = new(0x506b, "CIVILIZATION SCORE");

		/// <summary>"SCORING COMPLETED"</summary>
		public static GameText TextScoringCompleted { get; } = new(0x507e, "SCORING COMPLETED");

		/// <summary>"No new technology found. / "</summary>
		public static GameText TextNoNewTechnologyFound { get; } = new(0x5231, "No new technology found.\n");

		/// <summary>"nuke1.pic"</summary>
		public static GameText FileNuke1Pic { get; } = new(0x5318, "nuke1.pic");

		/// <summary>"settlers.pic"</summary>
		public static GameText FileSettlersPic { get; } = new(0x536e, "settlers.pic");

		/// <summary>"hill.pic"</summary>
		public static GameText FileHillPicC { get; } = new(0x537b, "hill.pic");

		/// <summary>"hill.pic"</summary>
		public static GameText FileHillPicD { get; } = new(0x5384, "hill.pic");

		/// <summary>"City Name..."</summary>
		public static GameText TextCityName { get; } = new(0x539e, "City Name...");

		/// <summary>"Your Name..."</summary>
		public static GameText TextYourName { get; } = new(0x53ab, "Your Name...");

		/// <summary>"Name of your Tribe..."</summary>
		public static GameText TextNameOfYourTribe { get; } = new(0x53b8, "Name of your Tribe...");

		/// <summary>"Unknown city."</summary>
		public static GameText TextUnknownCity { get; } = new(0x53f3, "Unknown city.");
	}
}
