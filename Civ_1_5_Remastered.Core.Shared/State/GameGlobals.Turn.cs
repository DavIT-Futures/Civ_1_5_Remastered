namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
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

			public ushort ContentAdvances { get => this.contentAdvances.Value; set => this.contentAdvances.Value = value; }

			public ushort Player { get => this.player.Value; set => this.player.Value = value; }

			public ushort TechnologyLevel { get => this.technologyLevel.Value; set => this.technologyLevel.Value = value; }

			public ushort GameEndRequest { get => this.gameEndRequest.Value; set => this.gameEndRequest.Value = value; }

			public ushort ChosenOrder { get => this.chosenOrder.Value; set => this.chosenOrder.Value = value; }

			public ushort PollutionFactor { get => this.pollutionFactor.Value; set => this.pollutionFactor.Value = value; }

			public ushort ConquestEnd { get => this.conquestEnd.Value; set => this.conquestEnd.Value = value; }

			public ushort PopulationMark { get => this.populationMark.Value; set => this.populationMark.Value = value; }

			public ushort CountLosses { get => this.countLosses.Value; set => this.countLosses.Value = value; }

			public ushort HumanContinentAttackers { get => this.humanContinentAttackers.Value; set => this.humanContinentAttackers.Value = value; }

			public ushort UnitLimitTold { get => this.unitLimitTold.Value; set => this.unitLimitTold.Value = value; }

			public ushort CivilopediaNextPage { get => this.civilopediaNextPage.Value; set => this.civilopediaNextPage.Value = value; }

			public ushort PlayerAtWar { get => this.playerAtWar.Value; set => this.playerAtWar.Value = value; }

			public ushort NoConquestAnimation { get => this.noConquestAnimation.Value; set => this.noConquestAnimation.Value = value; }

			public ushort CivilopediaWithoutPictures { get => this.civilopediaWithoutPictures.Value; set => this.civilopediaWithoutPictures.Value = value; }

			public ushort AnarchyPressure { get => this.anarchyPressure.Value; set => this.anarchyPressure.Value = value; }

			public ushort DemocracyPressure { get => this.democracyPressure.Value; set => this.democracyPressure.Value = value; }

			public ushort QuietUnitSetup { get => this.quietUnitSetup.Value; set => this.quietUnitSetup.Value = value; }

			public ushort RiotShown { get => this.riotShown.Value; set => this.riotShown.Value = value; }

			public ushort LateGameNudge { get => this.lateGameNudge.Value; set => this.lateGameNudge.Value = value; }

			public ushort LiveRealmCount { get => this.liveRealmCount.Value; set => this.liveRealmCount.Value = value; }

			public ushort UnitSupportCount { get => this.unitSupportCount.Value; set => this.unitSupportCount.Value = value; }

			public ushort SavedGovernmentTitleCommunism { get => this.savedGovernmentTitleCommunism.Value; set => this.savedGovernmentTitleCommunism.Value = value; }

			public ushort SavedGovernmentTitleMonarchy { get => this.savedGovernmentTitleMonarchy.Value; set => this.savedGovernmentTitleMonarchy.Value = value; }
		}
	}
}
