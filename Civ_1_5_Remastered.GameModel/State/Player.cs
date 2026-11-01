namespace Civ_1_5_Remastered.GameModel.State
{
	public class Player
	{
		public Player()
		{
			for (int index = 0; index < this.Relations.Length; index++)
			{
				this.Relations[index] = 0;
			}

			for (int index = 0; index < this.Landmasses.Length; index++)
			{
				this.Landmasses[index] = new PlayerContinent();
			}

			for (int index = 0; index < this.FoundScienceMarkers.Length; index++)
			{
				this.FoundScienceMarkers[index] = 0;
			}

			for (int index = 0; index < this.ScienceObtainedSince.Length; index++)
			{
				this.ScienceObtainedSince[index] = 0;
			}

			for (int index = 0; index < this.LiveTroops.Length; index++)
			{
				this.LiveTroops[index] = 0;
			}

			for (int index = 0; index < this.TroopsRuined.Length; index++)
			{
				this.TroopsRuined[index] = 0;
			}

			for (int index = 0; index < this.Troops.Length; index++)
			{
				this.Troops[index] = new Unit();
			}

			for (int index = 0; index < this.TroopsInsideMaking.Length; index++)
			{
				this.TroopsInsideMaking[index] = 0;
			}

			for (int index = 0; index < this.MissingTroops.Length; index++)
			{
				this.MissingTroops[index] = 0;
			}

			for (int index = 0; index < this.TacticalSites.Length; index++)
			{
				this.TacticalSites[index] = new StrategicLocation();
			}

			for (int index = 0; index < this.StarshipContent.Length; index++)
			{
				this.StarshipContent[index] = 0;
			}
		}

		public string Label { get; set; } = "";
		public short ChiefVisuals { get; set; } = 0;
		public short Gold { get; set; } = 0;
		public short RegimeKind { get; set; } = 0;
		public short LevyRatio { get; set; } = 0;
		public short MartialMight { get; set; } = 0;
		public short TouchParticipantTimeLeft { get; set; } = 0;
		public short HorizontalBegin { get; set; } = 0;
		public short TownTally { get; set; } = 0;
		public short GrandTownExtent { get; set; } = 0;
		public short SoilTally { get; set; } = 0;
		public ushort[] Relations { get; set; } = new ushort[8];
		public PlayerContinent[] Landmasses { get; set; } = new PlayerContinent[16];
		public short Standing { get; set; } = 0;
		public short RunningSagaStanding { get; set; } = 0;
		public short Tally { get; set; } = 0;

		public short ResearchLevyRatio { get; set; } = 0;

		public short InquiryHeadway { get; set; } = 0;
		public short FoundScienceTally { get; set; } = 0;
		public short PresentInquiryIndex { get; set; } = 0;
		public short ComingScienceTally { get; set; } = 0;
		public ushort[] FoundScienceMarkers { get; set; } = new ushort[5];
		public short[] ScienceObtainedSince { get; set; } = new short[72];

		public short TroopTally { get; set; } = 0;

		public short ColonistTally { get; set; } = 0;
		public short[] LiveTroops { get; set; } = new short[28];
		public short[] TroopsRuined { get; set; } = new short[8];
		public Unit[] Troops { get; set; } = new Unit[129];
		public short[] TroopsInsideMaking { get; set; } = new short[28];
		public short[] MissingTroops { get; set; } = new short[28];

		public StrategicLocation[] TacticalSites { get; set; } = new StrategicLocation[16];

		public sbyte[] StarshipContent { get; set; } = new sbyte[180];

		public short StarshipInhabitants { get; set; } = 0;
		public short StarshipArrivalSeason { get; set; } = 0;
		public short StarshipLiftoffSeason { get; set; } = 0;
		public short StarshipVictoryRatio { get; set; } = 0;

		public void StopMaking(sbyte kind)
		{
			if (kind >= 0)
				this.TroopsInsideMaking[kind]--;
		}

		public void StartMaking(sbyte kind)
		{
			if (kind >= 0)
				this.TroopsInsideMaking[kind]++;
		}
	}
}
