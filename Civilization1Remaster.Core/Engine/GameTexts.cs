namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The texts of the data segment of CIV.EXE that the code passes by address (the topics of the text files with "*",
	/// the names of the picture and text files, the texts drawn on the screens), named by their texts (as loaded; the
	/// same text at two addresses gets a number)
	/// </summary>
	public static class GameTexts
	{
		/// <summary>"*LOMEM"</summary>
		public const ushort TopicLomem = 0x0270;

		/// <summary>"custom.pic"</summary>
		public const ushort FileCustomPic = 0x1b3d;

		/// <summary>"sp257.pic"</summary>
		public const ushort FileSp257Pic = 0x1c0c;

		/// <summary>"diffs.pic"</summary>
		public const ushort FileDiffsPic = 0x1c16;

		/// <summary>"*HELP1"</summary>
		public const ushort TopicHelp1 = 0x1e60;

		/// <summary>"*HELP2"</summary>
		public const ushort TopicHelp2 = 0x1e67;

		/// <summary>"*FIRSTMOVE"</summary>
		public const ushort TopicFirstmove = 0x1e6e;

		/// <summary>"cback.pic"</summary>
		public const ushort FileCbackPic = 0x1e79;

		/// <summary>"*PILLAGE"</summary>
		public const ushort TopicPillage = 0x1e83;

		/// <summary>"*SETTLERS"</summary>
		public const ushort TopicSettlers = 0x1e8c;

		/// <summary>"*SETTLERS"</summary>
		public const ushort TopicSettlersB = 0x1e96;

		/// <summary>"*SETTLERS"</summary>
		public const ushort TopicSettlersC = 0x1ea0;

		/// <summary>"*NOIRR"</summary>
		public const ushort TopicNoirr = 0x1eaa;

		/// <summary>"*NOWATER"</summary>
		public const ushort TopicNowater = 0x1eb1;

		/// <summary>"*SETTLERS"</summary>
		public const ushort TopicSettlersD = 0x1eba;

		/// <summary>"*NOMINE"</summary>
		public const ushort TopicNomine = 0x1ec4;

		/// <summary>"*SETTLERS"</summary>
		public const ushort TopicSettlersE = 0x1ecc;

		/// <summary>"*ADDCITY"</summary>
		public const ushort TopicAddcity = 0x1ed6;

		/// <summary>"*ZOC"</summary>
		public const ushort TopicZoc = 0x1edf;

		/// <summary>"*AMPHIB"</summary>
		public const ushort TopicAmphib = 0x1f28;

		/// <summary>"*FIGHTER"</summary>
		public const ushort TopicFighter = 0x1f30;

		/// <summary>"*OCCUPY"</summary>
		public const ushort TopicOccupy = 0x1f62;

		/// <summary>"*REV"</summary>
		public const ushort TopicRev = 0x1f6a;

		/// <summary>"Leaders randomized. / "</summary>
		public const ushort TextLeadersRandomized = 0x2003;

		/// <summary>"*HELP1"</summary>
		public const ushort TopicHelp1B = 0x2029;

		/// <summary>"*HELP2"</summary>
		public const ushort TopicHelp2B = 0x2030;

		/// <summary>"*FUEL"</summary>
		public const ushort TopicFuel = 0x2070;

		/// <summary>"*TRIREME"</summary>
		public const ushort TopicTrireme = 0x2076;

		/// <summary>"End of Turn"</summary>
		public const ushort TextEndOfTurn = 0x207f;

		/// <summary>"Press Enter"</summary>
		public const ushort TextPressEnter = 0x208b;

		/// <summary>"to continue"</summary>
		public const ushort TextToContinue = 0x2097;

		/// <summary>"Veteran"</summary>
		public const ushort TextVeteran = 0x20a3;

		/// <summary>"+"</summary>
		public const ushort TextPlus = 0x20f1;

		/// <summary>"End of Turn"</summary>
		public const ushort TextEndOfTurnB = 0x20f6;

		/// <summary>"End of Turn"</summary>
		public const ushort TextEndOfTurnC = 0x2102;

		/// <summary>"*FIRSTUNIT1"</summary>
		public const ushort TopicFirstunit1 = 0x210e;

		/// <summary>"*FIRSTUNIT2"</summary>
		public const ushort TopicFirstunit2 = 0x211a;

		/// <summary>"*UMAX"</summary>
		public const ushort TopicUmax = 0x2126;

		/// <summary>"iconpg1.pal"</summary>
		public const ushort FileIconpg1Pal = 0x2311;

		/// <summary>"*FIRSTCIV"</summary>
		public const ushort TopicFirstciv = 0x231d;

		/// <summary>"*GAPE"</summary>
		public const ushort TopicGape = 0x240e;

		/// <summary>"*MONARCHY"</summary>
		public const ushort TopicMonarchy = 0x2414;

		/// <summary>"INFO"</summary>
		public const ushort TextInfo = 0x25ea;

		/// <summary>"HAPPY"</summary>
		public const ushort TextHappy = 0x25ef;

		/// <summary>"MAP"</summary>
		public const ushort TextMap = 0x25f5;

		/// <summary>"VIEW"</summary>
		public const ushort TextView = 0x25f9;

		/// <summary>"WONDERS"</summary>
		public const ushort TextWonders = 0x25fe;

		/// <summary>"MORE"</summary>
		public const ushort TextMore = 0x262a;

		/// <summary>"BUY"</summary>
		public const ushort TextBuy = 0x263c;

		/// <summary>"Food Storage"</summary>
		public const ushort TextFoodStorage = 0x2640;

		/// <summary>"City Resources"</summary>
		public const ushort TextCityResources = 0x264d;

		/// <summary>"EXIT"</summary>
		public const ushort TextExit = 0x2662;

		/// <summary>"RENAME"</summary>
		public const ushort TextRename = 0x2667;

		/// <summary>"*DISORDER"</summary>
		public const ushort TopicDisorder = 0x2708;

		/// <summary>"*LEADER"</summary>
		public const ushort TopicLeader = 0x2ae7;

		/// <summary>"F"</summary>
		public const ushort TextF = 0x2ba0;

		/// <summary>"G"</summary>
		public const ushort TextG = 0x2ba2;

		/// <summary>"CityAt Error / "</summary>
		public const ushort TextCityAtError = 0x2bc6;

		/// <summary>"(HELP AVAILABLE)"</summary>
		public const ushort TextHelpAvailable = 0x2fbd;

		/// <summary>"king.txt"</summary>
		public const ushort FileKingTxt = 0x30a5;

		/// <summary>"logo.pic"</summary>
		public const ushort FileLogoPic = 0x328a;

		/// <summary>"birth0.pic"</summary>
		public const ushort FileBirth0Pic = 0x3293;

		/// <summary>"birth1.pic"</summary>
		public const ushort FileBirth1Pic = 0x329e;

		/// <summary>"logo.pic"</summary>
		public const ushort FileLogoPicB = 0x32a9;

		/// <summary>"planet2.pic"</summary>
		public const ushort FilePlanet2Pic = 0x32d8;

		/// <summary>"planet1.pic"</summary>
		public const ushort FilePlanet1Pic = 0x32e4;

		/// <summary>"king.txt"</summary>
		public const ushort FileKingTxtB = 0x3301;

		/// <summary>"spacest.pic"</summary>
		public const ushort FileSpacestPic = 0x330a;

		/// <summary>"spacest.pic"</summary>
		public const ushort FileSpacestPicB = 0x3316;

		/// <summary>"sprites.pic"</summary>
		public const ushort FileSpritesPic = 0x3322;

		/// <summary>"arch.pic"</summary>
		public const ushort FileArchPic = 0x332e;

		/// <summary>"*ARCH"</summary>
		public const ushort TopicArch = 0x3342;

		/// <summary>"king.txt"</summary>
		public const ushort FileKingTxtC = 0x3348;

		/// <summary>"*INIT"</summary>
		public const ushort TopicInit = 0x35c3;

		/// <summary>"sp299.pic"</summary>
		public const ushort FileSp299Pic = 0x36ff;

		/// <summary>"ter257.pic"</summary>
		public const ushort FileTer257Pic = 0x3709;

		/// <summary>"sprites.pic"</summary>
		public const ushort FileSpritesPicB = 0x3714;

		/// <summary>"sp257.pic"</summary>
		public const ushort FileSp257PicB = 0x3720;

		/// <summary>"sp256.pal"</summary>
		public const ushort FileSp256Pal = 0x372a;

		/// <summary>"New Cabinet:"</summary>
		public const ushort TextNewCabinet = 0x375a;

		/// <summary>"*EXCHANGE"</summary>
		public const ushort TopicExchange = 0x3776;

		/// <summary>"*CANCEL"</summary>
		public const ushort TopicCancel = 0x378f;

		/// <summary>"*TRIBUTE"</summary>
		public const ushort TopicTribute = 0x3797;

		/// <summary>"*TAKECIV"</summary>
		public const ushort TopicTakeciv = 0x37a0;

		/// <summary>"*PEACE"</summary>
		public const ushort TopicPeace = 0x37cd;

		/// <summary>"*GIVECIV"</summary>
		public const ushort TopicGiveciv = 0x3805;

		/// <summary>"*GIVECASH"</summary>
		public const ushort TopicGivecash = 0x380e;

		/// <summary>"*GROVEL"</summary>
		public const ushort TopicGrovel = 0x3818;

		/// <summary>"*MOBILIZE"</summary>
		public const ushort TopicMobilize = 0x3820;

		/// <summary>"*ATTITUDE"</summary>
		public const ushort TopicAttitude = 0x382a;

		/// <summary>"*THREAT"</summary>
		public const ushort TopicThreat = 0x388f;

		/// <summary>"*GIVEMORE"</summary>
		public const ushort TopicGivemore = 0x3897;

		/// <summary>"*PROVOKE"</summary>
		public const ushort TopicProvoke = 0x38a1;

		/// <summary>"*NOCONTACT"</summary>
		public const ushort TopicNocontact = 0x38e9;

		/// <summary>"*ATWAR"</summary>
		public const ushort TopicAtwar = 0x38f4;

		/// <summary>"*MERCENARY"</summary>
		public const ushort TopicMercenary = 0x38fb;

		/// <summary>"*UNFORTUNATE"</summary>
		public const ushort TopicUnfortunate = 0x391e;

		/// <summary>"*SCHISM"</summary>
		public const ushort TopicSchism = 0x392b;

		/// <summary>"*TREATY"</summary>
		public const ushort TopicTreaty = 0x3a8c;

		/// <summary>"map.pic"</summary>
		public const ushort FileMapPic = 0x3b20;

		/// <summary>"BUILDING NEW WORLD..."</summary>
		public const ushort TextBuildingNewWorld = 0x3b28;

		/// <summary>"ENCYCLOPAEDIA of CIVILIZATION"</summary>
		public const ushort TextEncyclopaediaOfCivilization = 0x3c90;

		/// <summary>"ENCYCLOPAEDIA of CIVILIZATION"</summary>
		public const ushort TextEncyclopaediaOfCivilizationB = 0x3cae;

		/// <summary>"EXIT"</summary>
		public const ushort TextExitB = 0x3ccc;

		/// <summary>"MORE"</summary>
		public const ushort TextMoreB = 0x3cd1;

		/// <summary>"Allows: "</summary>
		public const ushort TextAllows = 0x3dad;

		/// <summary>"Allows: "</summary>
		public const ushort TextAllowsB = 0x3de5;

		/// <summary>"nothing"</summary>
		public const ushort TextNothing = 0x3f20;

		/// <summary>"*  -1 if government is Despotism/Anarchy."</summary>
		public const ushort Topic1IfGovernmentIsDespotismAnarchy = 0x3f28;

		/// <summary>"%  +1 if government is Republic/Democracy."</summary>
		public const ushort Text1IfGovernmentIsRepublicDemocracy = 0x3f52;

		/// <summary>"See Replay? /  No replay /  Quick Replay /  Complete Replay /  Write Replay to Disk / "</summary>
		public const ushort TextSeeReplayNoReplayQuickReplayCompleteRepl = 0x3fb0;

		/// <summary>"slam1.pic"</summary>
		public const ushort FileSlam1Pic = 0x4112;

		/// <summary>"slam1.pal"</summary>
		public const ushort FileSlam1Pal = 0x412a;

		/// <summary>"slam1.pic"</summary>
		public const ushort FileSlam1PicB = 0x4134;

		/// <summary>"CIVILIZATION POWERGraph"</summary>
		public const ushort TextCivilizationPOWERGraph = 0x43d8;

		/// <summary>"Approval Rating:"</summary>
		public const ushort TextApprovalRating = 0x4544;

		/// <summary>"Population:"</summary>
		public const ushort TextPopulation = 0x4559;

		/// <summary>"GNP:"</summary>
		public const ushort TextGnp = 0x456a;

		/// <summary>"Mfg. Goods:"</summary>
		public const ushort TextMfgGoods = 0x457d;

		/// <summary>"Land Area:"</summary>
		public const ushort TextLandArea = 0x4597;

		/// <summary>"Literacy:"</summary>
		public const ushort TextLiteracy = 0x45b5;

		/// <summary>"Disease:"</summary>
		public const ushort TextDisease = 0x45c3;

		/// <summary>"Pollution:"</summary>
		public const ushort TextPollution = 0x45d0;

		/// <summary>"Life Expectancy:"</summary>
		public const ushort TextLifeExpectancy = 0x45ee;

		/// <summary>"Family Size:"</summary>
		public const ushort TextFamilySize = 0x460b;

		/// <summary>"Military Service:"</summary>
		public const ushort TextMilitaryService = 0x4625;

		/// <summary>"Annual Income:"</summary>
		public const ushort TextAnnualIncome = 0x4643;

		/// <summary>"Productivity:"</summary>
		public const ushort TextProductivity = 0x4661;

		/// <summary>"SCIENCE REPORT"</summary>
		public const ushort TextScienceReport = 0x4762;

		/// <summary>"MILITARY STATUS"</summary>
		public const ushort TextMilitaryStatus = 0x477e;

		/// <summary>"MILITARY LOSSES"</summary>
		public const ushort TextMilitaryLosses = 0x47ad;

		/// <summary>"TRADE REPORT"</summary>
		public const ushort TextTradeReport = 0x47bd;

		/// <summary>"City Trade"</summary>
		public const ushort TextCityTrade = 0x47ca;

		/// <summary>"TRADE REPORT"</summary>
		public const ushort TextTradeReportB = 0x47d5;

		/// <summary>"Maintenance Costs"</summary>
		public const ushort TextMaintenanceCosts = 0x481f;

		/// <summary>"INTELLIGENCE REPORT"</summary>
		public const ushort TextIntelligenceReport = 0x4847;

		/// <summary>"No embassy established."</summary>
		public const ushort TextNoEmbassyEstablished = 0x485b;

		/// <summary>"INTELLIGENCE REPORT"</summary>
		public const ushort TextIntelligenceReportB = 0x48a5;

		/// <summary>"Foreign Affairs:"</summary>
		public const ushort TextForeignAffairs = 0x4956;

		/// <summary>"Technologies:"</summary>
		public const ushort TextTechnologies = 0x4987;

		/// <summary>"ATTITUDE SURVEY"</summary>
		public const ushort TextAttitudeSurvey = 0x4995;

		/// <summary>"ATTITUDE SURVEY"</summary>
		public const ushort TextAttitudeSurveyB = 0x49a5;

		/// <summary>"CITY STATUS"</summary>
		public const ushort TextCityStatus = 0x49e4;

		/// <summary>"CITY STATUS"</summary>
		public const ushort TextCityStatusB = 0x49f0;

		/// <summary>"hill.pic"</summary>
		public const ushort FileHillPic = 0x4e98;

		/// <summary>"hill.pic"</summary>
		public const ushort FileHillPicB = 0x4ea1;

		/// <summary>"hill.pal"</summary>
		public const ushort FileHillPal = 0x4eaa;

		/// <summary>"pop.pic"</summary>
		public const ushort FilePopPic = 0x4ebc;

		/// <summary>"riot.pic"</summary>
		public const ushort FileRiotPic = 0x4f2f;

		/// <summary>"riot2.pic"</summary>
		public const ushort FileRiot2Pic = 0x4f38;

		/// <summary>"*FIRSTPRODUCT"</summary>
		public const ushort TopicFirstproduct = 0x4f66;

		/// <summary>"*PMAX"</summary>
		public const ushort TopicPmax = 0x4f74;

		/// <summary>"*D1"</summary>
		public const ushort TopicD1 = 0x4f99;

		/// <summary>"*D2"</summary>
		public const ushort TopicD2 = 0x4f9d;

		/// <summary>"*D3"</summary>
		public const ushort TopicD3 = 0x4fa1;

		/// <summary>"*D4"</summary>
		public const ushort TopicD4 = 0x4fa5;

		/// <summary>"*D5"</summary>
		public const ushort TopicD5 = 0x4fc3;

		/// <summary>"*D6"</summary>
		public const ushort TopicD6 = 0x4fc7;

		/// <summary>"CIVILIZATION SCORE"</summary>
		public const ushort TextCivilizationScore = 0x506b;

		/// <summary>"SCORING COMPLETED"</summary>
		public const ushort TextScoringCompleted = 0x507e;

		/// <summary>"No new technology found. / "</summary>
		public const ushort TextNoNewTechnologyFound = 0x5231;

		/// <summary>"nuke1.pic"</summary>
		public const ushort FileNuke1Pic = 0x5318;

		/// <summary>"settlers.pic"</summary>
		public const ushort FileSettlersPic = 0x536e;

		/// <summary>"hill.pic"</summary>
		public const ushort FileHillPicC = 0x537b;

		/// <summary>"hill.pic"</summary>
		public const ushort FileHillPicD = 0x5384;

		/// <summary>"City Name..."</summary>
		public const ushort TextCityName = 0x539e;

		/// <summary>"Your Name..."</summary>
		public const ushort TextYourName = 0x53ab;

		/// <summary>"Name of your Tribe..."</summary>
		public const ushort TextNameOfYourTribe = 0x53b8;

		/// <summary>"Unknown city."</summary>
		public const ushort TextUnknownCity = 0x53f3;
	}
}
