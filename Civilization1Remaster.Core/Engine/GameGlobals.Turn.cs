namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The game and the turn (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class TurnVariables : VariableGroup
		{
			private readonly GlobalWord contentAdvances = new GlobalWord(0xb882);
			private readonly GlobalWord player = new GlobalWord(0x6b90);
			private readonly GlobalWord technologyLevel = new GlobalWord(0xd2de);
			private readonly GlobalWord gameEndRequest = new GlobalWord(0xdc48);
			private readonly GlobalWord chosenOrder = new GlobalWord(0xd4ca);
			private readonly GlobalWord pollutionFactor = new GlobalWord(0x6c98);
			private readonly GlobalWord conquestEnd = new GlobalWord(0xb884);
			private readonly GlobalWord populationMark = new GlobalWord(0x1c24);
			private readonly GlobalWord countLosses = new GlobalWord(0x70d8);
			private readonly GlobalWord humanContinentAttackers = new GlobalWord(0x2a6e);
			private readonly GlobalWord unitLimitTold = new GlobalWord(0xd760);
			private readonly GlobalWord civilopediaNextPage = new GlobalWord(0x6808);
			private readonly GlobalWord playerAtWar = new GlobalWord(0x6ed4);
			private readonly GlobalWord noConquestAnimation = new GlobalWord(0x28bc);
			private readonly GlobalWord civilopediaWithoutPictures = new GlobalWord(0x3c62);
			private readonly GlobalWord anarchyPressure = new GlobalWord(0xe3c2);
			private readonly GlobalWord democracyPressure = new GlobalWord(0xdb42);
			private readonly GlobalWord quietUnitSetup = new GlobalWord(0x20f4);
			private readonly GlobalWord riotShown = new GlobalWord(0x6b92);
			private readonly GlobalWord lateGameNudge = new GlobalWord(0x8078);
			private readonly GlobalWord liveRealmCount = new GlobalWord(0xde20);
			private readonly GlobalWord unitSupportCount = new GlobalWord(0xdeb6);
			private readonly GlobalWord savedGovernmentTitleCommunism = new GlobalWord(0x33a8);
			private readonly GlobalWord savedGovernmentTitleMonarchy = new GlobalWord(0x33aa);

			/// <summary>0xb882: the advances that make citizens content (none with the wonder 0x1000 in the city)</summary>
			public ushort ContentAdvances { get => this.contentAdvances.Value; set => this.contentAdvances.Value = value; }

			/// <summary>0x6b90: the player whose turn it is</summary>
			public ushort Player { get => this.player.Value; set => this.player.Value = value; }

			/// <summary>0xd2de: the technology level of the time (AD only)</summary>
			public ushort TechnologyLevel { get => this.technologyLevel.Value; set => this.technologyLevel.Value = value; }

			/// <summary>0xdc48: asks the main loop to end or to load the game</summary>
			public ushort GameEndRequest { get => this.gameEndRequest.Value; set => this.gameEndRequest.Value = value; }

			/// <summary>0xd4ca: the order chosen from the menu bar (its key)</summary>
			public ushort ChosenOrder { get => this.chosenOrder.Value; set => this.chosenOrder.Value = value; }

			/// <summary>0x6c98: the divisor of the pollution of the cities</summary>
			public ushort PollutionFactor { get => this.pollutionFactor.Value; set => this.pollutionFactor.Value = value; }

			/// <summary>0xb884: the game ends by conquest</summary>
			public ushort ConquestEnd { get => this.conquestEnd.Value; set => this.conquestEnd.Value = value; }

			/// <summary>0x1c24: the next population mark of the status bar (tens below 100, then hundreds + 9)</summary>
			public ushort PopulationMark { get => this.populationMark.Value; set => this.populationMark.Value = value; }

			/// <summary>0x70d8: the units eliminated are counted as losses</summary>
			public ushort CountLosses { get => this.countLosses.Value; set => this.countLosses.Value = value; }

			/// <summary>0x2a6e: the bits of the players that attack a continent of the human player</summary>
			public ushort HumanContinentAttackers { get => this.humanContinentAttackers.Value; set => this.humanContinentAttackers.Value = value; }

			/// <summary>0xd760: the human player was told there is no room for more units</summary>
			public ushort UnitLimitTold { get => this.unitLimitTold.Value; set => this.unitLimitTold.Value = value; }

			/// <summary>0x6808: the Civilopedia list has a next page</summary>
			public ushort CivilopediaNextPage { get => this.civilopediaNextPage.Value; set => this.civilopediaNextPage.Value = value; }

			/// <summary>0x6ed4: the player is at war with someone</summary>
			public ushort PlayerAtWar { get => this.playerAtWar.Value; set => this.playerAtWar.Value = value; }

			/// <summary>0x28bc: the city is seized without the animations</summary>
			public ushort NoConquestAnimation { get => this.noConquestAnimation.Value; set => this.noConquestAnimation.Value = value; }

			/// <summary>0x3c62: the Civilopedia is shown without its pictures (disk missing)</summary>
			public ushort CivilopediaWithoutPictures { get => this.civilopediaWithoutPictures.Value; set => this.civilopediaWithoutPictures.Value = value; }

			/// <summary>0xe3c2: the unpaid support of the cities; a computer player above 0 falls into anarchy</summary>
			public ushort AnarchyPressure { get => this.anarchyPressure.Value; set => this.anarchyPressure.Value = value; }

			/// <summary>0xdb42: a computer player at 0 or more chooses democracy</summary>
			public ushort DemocracyPressure { get => this.democracyPressure.Value; set => this.democracyPressure.Value = value; }

			/// <summary>0x20f4: new units are set up without repainting and moving them</summary>
			public ushort QuietUnitSetup { get => this.quietUnitSetup.Value; set => this.quietUnitSetup.Value = value; }

			/// <summary>0x6b92: the riot in the view of the city was shown this turn (1)</summary>
			public ushort RiotShown { get => this.riotShown.Value; set => this.riotShown.Value = value; }

			/// <summary>0x8078: the human player is far behind late in the game (standing 7, more than 4 cities, no spaceship part, past turn 200)</summary>
			public ushort LateGameNudge { get => this.lateGameNudge.Value; set => this.lateGameNudge.Value = value; }

			/// <summary>0xde20: the living players, counted at the start of the turn</summary>
			public ushort LiveRealmCount { get => this.liveRealmCount.Value; set => this.liveRealmCount.Value = value; }

			/// <summary>0xdeb6: unit support count</summary>
			public ushort UnitSupportCount { get => this.unitSupportCount.Value; set => this.unitSupportCount.Value = value; }

			/// <summary>0x33a8: saved government title for communism</summary>
			public ushort SavedGovernmentTitleCommunism { get => this.savedGovernmentTitleCommunism.Value; set => this.savedGovernmentTitleCommunism.Value = value; }

			/// <summary>0x33aa: saved government title for monarchy</summary>
			public ushort SavedGovernmentTitleMonarchy { get => this.savedGovernmentTitleMonarchy.Value; set => this.savedGovernmentTitleMonarchy.Value = value; }
		}
	}
}
