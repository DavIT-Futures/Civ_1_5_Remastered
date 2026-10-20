using System;

namespace Civilization1Original.Game.State
{
	public class Player
	{
		public string Label = "";
		public string Citizenship = "";
		public string ChiefLabel = "";
		public short ChiefVisuals = 0;
		public short Gold = 0;
		public short RegimeKind = 0;
		public short LevyRatio = 0;
		public short MartialMight = 0;
		public short TouchParticipantTimeleft = 0;
		public short HorizBegin = 0;
		public short TownTally = 0;
		public short GrandTownExtent = 0;
		public short SoilTally = 0;

		public ushort[] Relations = new ushort[8];
		public PlayerContinent[] Landmasses = new PlayerContinent[16];
		public short Standing = 0;
		public short RunningSagaStanding = 0;
		public short Tally = 0;

		// Technology
		public short ResearchLevyRatio = 0;
		public short InquiryHeadway = 0;
		public short FoundScienceTally = 0;
		public short PresentInquiryIdx = 0;
		public short ComingScienceTally = 0;
		public ushort[] FoundScienceMarkers = new ushort[5];
		public short[] ScienceObtainedSince = new short[72];

		// Units
		public short TroopTally = 0;
		public short ColonistTally = 0;
		public short[] LiveTroops = new short[28];
		public short[] TroopsRuined = new short[8];
		public Unit[] Troops = new Unit[129];
		public short[] TroopsInsideMaking = new short[28];
		public short[] MissingTroops = new short[28];

		// Strategic locations
		public StrategicLocation[] TacticalSites = new StrategicLocation[16];

		// Spaceship
		public sbyte[] StarshipContent = new sbyte[180];
		public short StarshipInhabitants = 0;
		public short StarshipArrivalSeason = 0;
		public short StarshipLiftoffSeason = 0;
		public short StarshipVictoryRatio = 0;

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
	}
}
