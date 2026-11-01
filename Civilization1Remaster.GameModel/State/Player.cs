namespace Civilization1Remaster.GameModel.State
{
	public class Player
	{
		public Player()
		{
			for (int idx = 0; idx < this.Relations.Length; idx++)
			{
				this.Relations[idx] = 0;
			}

			for (int idx = 0; idx < this.Landmasses.Length; idx++)
			{
				this.Landmasses[idx] = new PlayerContinent();
			}

			for (int idx = 0; idx < this.FoundScienceMarkers.Length; idx++)
			{
				this.FoundScienceMarkers[idx] = 0;
			}

			for (int idx = 0; idx < this.ScienceObtainedSince.Length; idx++)
			{
				this.ScienceObtainedSince[idx] = 0;
			}

			for (int idx = 0; idx < this.LiveTroops.Length; idx++)
			{
				this.LiveTroops[idx] = 0;
			}

			for (int idx = 0; idx < this.TroopsRuined.Length; idx++)
			{
				this.TroopsRuined[idx] = 0;
			}

			for (int idx = 0; idx < this.Troops.Length; idx++)
			{
				this.Troops[idx] = new Unit();
			}

			for (int idx = 0; idx < this.TroopsInsideMaking.Length; idx++)
			{
				this.TroopsInsideMaking[idx] = 0;
			}

			for (int idx = 0; idx < this.MissingTroops.Length; idx++)
			{
				this.MissingTroops[idx] = 0;
			}

			for (int idx = 0; idx < this.TacticalSites.Length; idx++)
			{
				this.TacticalSites[idx] = new StrategicLocation();
			}

			for (int idx = 0; idx < this.StarshipContent.Length; idx++)
			{
				this.StarshipContent[idx] = 0;
			}
		}

		public string Label { get; set; } = "";
		public short ChiefVisuals { get; set; } = 0;
		public short Gold { get; set; } = 0;
		public short RegimeKind { get; set; } = 0;
		public short LevyRatio { get; set; } = 0;
		public short MartialMight { get; set; } = 0;
		public short TouchParticipantTimeleft { get; set; } = 0;
		public short HorizBegin { get; set; } = 0;
		public short TownTally { get; set; } = 0;
		public short GrandTownExtent { get; set; } = 0;
		public short SoilTally { get; set; } = 0;
		public ushort[] Relations { get; set; } = new ushort[8];
		public PlayerContinent[] Landmasses { get; set; } = new PlayerContinent[16];
		public short Standing { get; set; } = 0;
		public short RunningSagaStanding { get; set; } = 0;
		public short Tally { get; set; } = 0;

		// Technology
		public short ResearchLevyRatio { get; set; } = 0;

		public short InquiryHeadway { get; set; } = 0;
		public short FoundScienceTally { get; set; } = 0;
		public short PresentInquiryIdx { get; set; } = 0;
		public short ComingScienceTally { get; set; } = 0;
		public ushort[] FoundScienceMarkers { get; set; } = new ushort[5];
		public short[] ScienceObtainedSince { get; set; } = new short[72];

		// Units
		public short TroopTally { get; set; } = 0;

		public short ColonistTally { get; set; } = 0;
		public short[] LiveTroops { get; set; } = new short[28];
		public short[] TroopsRuined { get; set; } = new short[8];
		public Unit[] Troops { get; set; } = new Unit[129];
		public short[] TroopsInsideMaking { get; set; } = new short[28];
		public short[] MissingTroops { get; set; } = new short[28];

		// Strategic locations
		public StrategicLocation[] TacticalSites { get; set; } = new StrategicLocation[16];

		// Spaceship
		public sbyte[] StarshipContent { get; set; } = new sbyte[180];

		public short StarshipInhabitants { get; set; } = 0;
		public short StarshipArrivalSeason { get; set; } = 0;
		public short StarshipLiftoffSeason { get; set; } = 0;
		public short StarshipVictoryRatio { get; set; } = 0;

		/// <summary>A city stops making the unit kind (an improvement, a negative kind, isn't counted)</summary>
		public void StopMaking(sbyte kind)
		{
			if (kind >= 0)
				this.TroopsInsideMaking[kind]--;
		}

		/// <summary>A city starts making the unit kind (an improvement, a negative kind, isn't counted)</summary>
		public void StartMaking(sbyte kind)
		{
			if (kind >= 0)
				this.TroopsInsideMaking[kind]++;
		}
	}
}
