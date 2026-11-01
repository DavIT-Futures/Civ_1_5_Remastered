using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.State
{
	public class GameState
	{
		public GameState()
		{
			for (int index = 0; index < this.Participants.Length; index++)
			{
				this.Participants[index] = new Player();
			}

			for (int index = 0; index < this.FirstCastleContent.Length; index++)
			{
				this.FirstCastleContent[index] = 0;
			}

			for (int index = 0; index < this.SecondCastleContent.Length; index++)
			{
				this.SecondCastleContent[index] = 0;
			}

			for (int index = 0; index < this.EachLandmassExtentWithEachSeaExtent.Length; index++)
			{
				this.EachLandmassExtentWithEachSeaExtent[index] = 0;
			}

			for (int index = 0; index < 16; index++)
			{
				this.Landmasses[index] = new Continent();
			}

			for (int index = 0; index < this.TallyPlotContent.Length; index++)
			{
				this.TallyPlotContent[index] = 0;
			}

			for (int index = 0; index < this.TrucePlotContent.Length; index++)
			{
				this.TrucePlotContent[index] = 0;
			}

			for (int index = 0; index < this.Towns.Length; index++)
			{
				this.Towns[index] = new City();
			}

			for (int index = 0; index < this.TownPlaces.Length; index++)
			{
				this.TownPlaces[index] = new Point(0, 0);
			}

			for (int index = 0; index < this.MarvelTownIndex.Length; index++)
			{
				this.MarvelTownIndex[index] = 0;
			}

			for (int index = 0; index < this.ScienceLeadingFoundVia.Length; index++)
			{
				this.ScienceLeadingFoundVia[index] = 0;
			}

			for (int index = 0; index < this.ChartSight.GetLength(0); index++)
			{
				for (int innerIndex = 0; innerIndex < this.ChartSight.GetLength(1); innerIndex++)
				{
					this.ChartSight[index, innerIndex] = 0;
				}
			}

			for (int index = 0; index < this.RerunContent.Length; index++)
			{
				this.RerunContent[index] = 0;
			}

			for (int index = 0; index < this.SoilRouting.Length; index++)
			{
				this.SoilRouting[index] = 0;
			}
		}

		public short RoundTally { get; set; } = 0;

		public UnitHealth Vitals { get; set; } = new UnitHealth();

		public short Season { get; set; } = 0;
		public ushort ChanceRoot { get; set; } = 0;
		public short HardnessTier { get; set; } = 0;
		public short PersonParticipantIndex { get; set; } = 0;
		public short ParticipantMarkers { get; set; } = 0;
		public short ParticipantIdentifierMarkers { get; set; } = 0;
		public short LiveRealms { get; set; } = 0;
		public short TruceRoundTally { get; set; } = 0;
		public short ComputerRivalTally { get; set; } = 0;
		public short FollowingCollectionRound { get; set; } = 80;

		public short SessionPreferenceMarkers { get; set; } = 2;

		public short DiagonalMarkers { get; set; } = 0;

		public Player[] Participants { get; set; } = new Player[8];

		public short CastleTier { get; set; } = 0;

		public short[] FirstCastleContent { get; set; } = new short[13];
		public short[] SecondCastleContent { get; set; } = new short[13];

		public short[] EachLandmassExtentWithEachSeaExtent { get; set; } = new short[128];

		public Continent[] Landmasses { get; set; } = new Continent[16];

		public byte[] TallyPlotContent { get; set; } = new byte[1200];

		public byte[] TrucePlotContent { get; set; } = new byte[1200];

		public City[] Towns { get; set; } = new City[128];

		public string[] TownLabels { get; set; } = new string[]
		{
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
			"Sydney      \0"
		};

		public Point[] TownPlaces { get; set; } = new Point[256];

		public short[] MarvelTownIndex { get; set; } = new short[22];

		public short[] ScienceLeadingFoundVia { get; set; } = new short[72];

		public short HighestScienceTally { get; set; } = 0;

		public ChartSightGrid ChartSight { get; set; } = new ChartSightGrid();

		public short DirtyCellTally { get; set; } = 0;
		public short SmogImpactTier { get; set; } = 0;
		public short WorldHeatingTally { get; set; } = 0;

		public short RerunContentSpan { get; set; } = 0;

		public byte[] RerunContent { get; set; } = new byte[4096];

		public byte[] SoilRouting { get; set; } = new byte[260];

		public short StarshipMarkers { get; set; } = 0;

		public short ComputerStarshipVictoryRatio { get; set; } = 0;

		public bool IsHuman(int participantIndex) => (this.ParticipantMarkers & (1 << (participantIndex & 0xff))) != 0;
	}
}
