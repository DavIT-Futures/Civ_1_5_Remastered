using System;
using System.Drawing;

namespace Civilization1Remaster.Game.State
{
	public class GameState
	{
		// Game common data
		public short RoundTally = 0;

		/// <summary>
		/// Hit points lost in battles and elite units, saved in a separate file next to the saved game
		/// </summary>
		public UnitHealth Vitals = new UnitHealth();
		public short Season = 0;
		public ushort ChanceRoot = 0;
		public short HardnessTier = 0;
		public short PersonParticipantIdx = 0;
		public short ParticipantMarkers = 0;
		public short ParticipantIdentMarkers = 0;
		public short LiveRealms = 0;

		public short TruceRoundTally = 0;
		public short BotRivalTally = 0;
		public short FollowingCollectionRound = 80;

		// Game settings
		public short SessionPrefMarkers = 2;
		public short DiagMarkers = 0;

		// Players
		public Player[] Participants = new Player[8];

		// Palace
		public short CastleTier = 0;
		public short[] CastleContent1 = new short[13];
		public short[] CastleContent2 = new short[13];

		// Continents
		public short[] EachLandmassExtentWithEachSeaExtent = new short[128];
		public Continent[] Landmasses = new Continent[16];

		// Graphs
		public byte[] TallyPlotContent = new byte[1200];
		public byte[] TrucePlotContent = new byte[1200];

		// Cities
		public City[] Towns = new City[128];
		public string[] TownLabels = new string[] {
			"Rome        \0", "Caesarea    \0", "Carthage    \0", "Nicopolis   \0", "Byzantium   \0",
			"Brundisium  \0", "Syracuse    \0", "Antioch     \0", "Palmyra     \0", "Cyrene      \0",
			"Gordion     \0", "Tyrus       \0", "Jerusalem   \0", "Seleucia    \0", "Ravenna     \0",
			"Artaxata    \0", "Babylon     \0", "Sumer       \0", "Uruk        \0", "Nineveh     \0",
			"Ashur       \0", "Ellipi      \0", "Akkad       \0", "Eridu       \0", "Kish        \0",
			"Nippur      \0", "Shuruppak   \0", "Zariqum     \0", "Izibia      \0", "Nimrud      \0",
			"Arbela      \0", "Zamua       \0", "Berlin      \0", "Leipzig     \0", "Hamburg     \0",
			"Bremen      \0", "Frankfurt   \0", "Bonn        \0", "Nuremberg   \0", "Cologne     \0",
			"Hannover    \0", "Munich      \0", "Stuttgart   \0", "Heidelburg  \0", "Salzburg    \0",
			"Konigsberg  \0", "Dortmund    \0", "Brandenburg \0", "Thebes      \0", "Memphis     \0",
			"Oryx        \0", "Heliopolis  \0", "Gaza        \0", "Alexandria  \0", "Byblos      \0",
			"Cairo       \0", "Coptos      \0", "Edfu        \0", "Pithom      \0", "Busiris     \0",
			"Athribis    \0", "Mendes      \0", "Tanis       \0", "Abydos      \0", "Washington  \0",
			"New York    \0", "Boston      \0", "Philadelphia\0", "Atlanta     \0", "Chicago     \0",
			"Buffalo     \0", "St.Louis    \0", "Detroit     \0", "New Orleans \0", "Baltimore   \0",
			"Denver      \0", "Cincinnati  \0", "Dallas      \0", "Los Angeles \0", "Las Vegas   \0",
			"Athens      \0", "Sparta      \0", "Corinth     \0", "Delphi      \0", "Eretria     \0",
			"Pharsalos   \0", "Argos       \0", "Mycenae     \0", "Herakleia   \0", "Antioch     \0",
			"Ephesos     \0", "Rhodes      \0", "Knossos     \0", "Troy        \0", "Pergamon    \0",
			"Miletos     \0", "Delhi       \0", "Bombay      \0", "Madras      \0", "Bangalore   \0",
			"Calcutta    \0", "Lahore      \0", "Karachi     \0", "Kolhapur    \0", "Jaipur      \0",
			"Hyderabad   \0", "Bengal      \0", "Chittagong  \0", "Punjab      \0", "Dacca       \0",
			"Indus       \0", "Ganges      \0", "Moscow      \0", "Leningrad   \0", "Kiev        \0",
			"Minsk       \0", "Smolensk    \0", "Odessa      \0", "Sevastopol  \0", "Tblisi      \0",
			"Sverdlovsk  \0", "Yakutsk     \0", "Vladivostok \0", "Novograd    \0", "Krasnoyarsk \0",
			"Riga        \0", "Rostov      \0", "Astrakhan   \0", "Zimbabwe    \0", "Ulundi      \0",
			"Bapedi      \0", "Hlobane     \0", "Isandhlwana \0", "Intombe     \0", "Mpondo      \0",
			"Ngome       \0", "Swazi       \0", "Tugela      \0", "Umtata      \0", "Umfolozi    \0",
			"Ibabanago   \0", "Isipezi     \0", "Amatikulu   \0", "Zunguin     \0", "Paris       \0",
			"Orleans     \0", "Lyons       \0", "Tours       \0", "Chartres    \0", "Bordeaux    \0",
			"Rouen       \0", "Avignon     \0", "Marseilles  \0", "Grenoble    \0", "Dijon       \0",
			"Amiens      \0", "Cherbourg   \0", "Poitiers    \0", "Toulouse    \0", "Bayonne     \0",
			"Tenochtitlan\0", "Chiauhtia   \0", "Chapultepec \0", "Coatepec    \0", "Ayotzinco   \0",
			"Itzapalapa  \0", "Iztapam     \0", "Mitxcoac    \0", "Tacubaya    \0", "Tecamac     \0",
			"Tepezinco   \0", "Ticoman     \0", "Tlaxcala    \0", "Xaltocan    \0", "Xicalango   \0",
			"Zumpanco    \0", "Peking      \0", "Shanghai    \0", "Canton      \0", "Nanking     \0",
			"Tsingtao    \0", "Hangchow    \0", "Tientsin    \0", "Tatung      \0", "Macao       \0",
			"Anyang      \0", "Shantung    \0", "Chinan      \0", "Kaifeng     \0", "Ningpo      \0",
			"Paoting     \0", "Yangchow    \0", "London      \0", "Coventry    \0", "Birmingham  \0",
			"Dover       \0", "Nottingham  \0", "York        \0", "Liverpool   \0", "Brighton    \0",
			"Oxford      \0", "Reading     \0", "Exeter      \0", "Cambridge   \0", "Hastings    \0",
			"Canterbury  \0", "Banbury     \0", "Newcastle   \0", "Samarkand   \0", "Bokhara     \0",
			"Nishapur    \0", "Karakorum   \0", "Kashgar     \0", "Tabriz      \0", "Aleppo      \0",
			"Kabul       \0", "Ormuz       \0", "Basra       \0", "Khanbalyk   \0", "Khorasan    \0",
			"Shangtu     \0", "Kazan       \0", "Quinsay     \0", "Kerman      \0", "Mecca       \0",
			"Naples      \0", "Sidon       \0", "Tyre        \0", "Tarsus      \0", "Issus       \0",
			"Cunaxa      \0", "Cremona     \0", "Cannae      \0", "Capua       \0", "Turin       \0",
			"Genoa       \0", "Utica       \0", "Crete       \0", "Damascus    \0", "Verona      \0",
			"Salamis     \0", "Lisbon      \0", "Hamburg     \0", "Prague      \0", "Salzburg    \0",
			"Bergen      \0", "Venice      \0", "Milan       \0", "Ghent       \0", "Pisa        \0",
			"Cordoba     \0", "Seville     \0", "Dublin      \0", "Toronto     \0", "Melbourne   \0",
			"Sydney      \0" };
		public Point[] TownPlaces = new Point[256];

		// Wonders
		public short[] MarvelTownIdx = new short[22];

		// Technology
		public short[] ScienceLeadingFoundVia = new short[72];
		public short HighestScienceTally = 0;

		// Units
		public UnitDefinition[] TroopSpecs = new UnitDefinition[]{
			new UnitDefinition("Settlers", 127, 0, 1, 0, 0, 1, 4, 0, 0, 0, -1),
			new UnitDefinition("Militia", 34, 0, 1, 0, 1, 1, 1, 0, 0, 2, -1),
			new UnitDefinition("Phalanx", 34, 0, 1, 0, 1, 2, 2, 0, 0, 2, 17),
			new UnitDefinition("Legion", 64, 0, 1, 0, 3, 1, 2, 0, 0, 1, 18),
			new UnitDefinition("Musketeers", 64, 0, 1, 0, 2, 3, 3, 0, 0, 2, 34),
			new UnitDefinition("Riflemen", 127, 0, 1, 0, 3, 5, 3, 0, 0, 2, 64),
			new UnitDefinition("Cavalry", 64, 0, 2, 0, 2, 1, 2, 0, 0, 1, 31),
			new UnitDefinition("Knights", 58, 0, 2, 0, 4, 2, 4, 0, 0, 1, 62),
			new UnitDefinition("Catapult", 48, 0, 1, 0, 6, 1, 4, 0, 0, 1, 9),
			new UnitDefinition("Cannon", 63, 0, 1, 0, 8, 1, 4, 0, 0, 1, 48),
			new UnitDefinition("Chariot", 62, 0, 2, 0, 4, 1, 4, 0, 0, 1, 33),
			new UnitDefinition("Armor", 127, 0, 3, 0, 10, 5, 8, 0, 0, 1, 58),
			new UnitDefinition("Mech. Inf.", 127, 0, 3, 0, 6, 6, 5, 0, 0, 2, 65),
			new UnitDefinition("Artillery", 127, 0, 2, 0, 12, 2, 6, 0, 0, 1, 63),
			new UnitDefinition("Fighter", 127, 1, 10, 1, 4, 2, 6, 2, 0, 4, 38),
			new UnitDefinition("Bomber", 127, 1, 8, 2, 12, 1, 12, 2, 0, 1, 39),
			new UnitDefinition("Trireme", 8, 2, 3, 0, 1, 0, 4, 0, 2, 5, 7),
			new UnitDefinition("Sail", 14, 2, 3, 0, 1, 1, 4, 0, 3, 5, 8),
			new UnitDefinition("Frigate", 35, 2, 3, 0, 2, 2, 4, 0, 4, 5, 14),
			new UnitDefinition("Ironclad", 37, 2, 4, 0, 4, 4, 6, 0, 0, 3, 23),
			new UnitDefinition("Cruiser", 127, 2, 6, 0, 6, 6, 8, 3, 0, 3, 37),
			new UnitDefinition("Battleship", 127, 2, 4, 0, 18, 12, 16, 3, 0, 3, 52),
			new UnitDefinition("Submarine", 127, 2, 3, 0, 8, 2, 5, 3, 0, 3, 41),
			new UnitDefinition("Carrier", 127, 2, 5, 0, 1, 12, 16, 3, 0, 3, 39),
			new UnitDefinition("Transport", 127, 2, 4, 0, 0, 3, 5, 0, 8, 5, 35),
			new UnitDefinition("Nuclear", 127, 1, 16, 1, 99, 0, 16, 0, 0, 1, 46),
			new UnitDefinition("Diplomat", 127, 0, 2, 0, 0, 0, 3, 0, 0, 6, 22),
			new UnitDefinition("Caravan", 127, 0, 1, 0, 0, 1, 5, 0, 0, 6, 24)};

		// Map
		public sbyte[,] ChartSight = new sbyte[80, 50];
		public short DirtyCellTally = 0;
		public short SmogImpactTier = 0;
		public short WorldHeatingTally = 0;

		// Replay data
		public short RerunContentSpan = 0;
		public byte[] RerunContent = new byte[4096];

		// ?
		public byte[] SoilRouting = new byte[260];

		// Spaceships
		public short StarshipMarkers = 0;
		public short BotStarshipVictoryRatio = 0;

		public GameState()
		{
			for (int idx = 0; idx < this.Participants.Length; idx++)
			{
				this.Participants[idx] = new Player();
			}

			for (int idx = 0; idx < this.CastleContent1.Length; idx++)
			{
				this.CastleContent1[idx] = 0;
			}

			for (int idx = 0; idx < this.CastleContent2.Length; idx++)
			{
				this.CastleContent2[idx] = 0;
			}

			for (int idx = 0; idx < this.EachLandmassExtentWithEachSeaExtent.Length; idx++)
			{
				this.EachLandmassExtentWithEachSeaExtent[idx] = 0;
			}

			for (int idx = 0; idx < 16; idx++)
			{
				this.Landmasses[idx] = new Continent();
			}

			for (int idx = 0; idx < this.TallyPlotContent.Length; idx++)
			{
				this.TallyPlotContent[idx] = 0;
			}

			for (int idx = 0; idx < this.TrucePlotContent.Length; idx++)
			{
				this.TrucePlotContent[idx] = 0;
			}

			for (int idx = 0; idx < this.Towns.Length; idx++)
			{
				this.Towns[idx] = new City();
			}

			for (int idx = 0; idx < this.TownPlaces.Length; idx++)
			{
				this.TownPlaces[idx] = new Point(0, 0);
			}

			for (int idx = 0; idx < this.MarvelTownIdx.Length; idx++)
			{
				this.MarvelTownIdx[idx] = 0;
			}

			for (int idx = 0; idx < this.ScienceLeadingFoundVia.Length; idx++)
			{
				this.ScienceLeadingFoundVia[idx] = 0;
			}

			for (int idx = 0; idx < this.ChartSight.GetLength(0); idx++)
			{
				for (int jdx = 0; jdx < this.ChartSight.GetLength(1); jdx++)
					this.ChartSight[idx, jdx] = 0;
			}

			for (int idx = 0; idx < this.RerunContent.Length; idx++)
			{
				this.RerunContent[idx] = 0;
			}

			for (int idx = 0; idx < this.SoilRouting.Length; idx++)
			{
				this.SoilRouting[idx] = 0;
			}
		}
	}
}
