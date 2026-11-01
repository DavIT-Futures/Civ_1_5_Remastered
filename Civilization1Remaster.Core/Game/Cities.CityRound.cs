namespace Civilization1Remaster.Core.Game
{
	partial class Cities
	{
		private sealed class CityRound(CPU processor)
		{
			private readonly FrameWords frame = new FrameWords(processor, 0x10c);

			private ushort content;
			private ushort factoryPower;
			private ushort workersGained;
			private ushort target;
			private ushort distance;
			private ushort improvementCount;
			private ushort cursorHoriz;
			private ushort cursorOn;
			private ushort newUnit;
			private ushort item;
			private ushort cursorVert;
			private ushort changeBlinks;
			private ushort mapViewHoriz;
			private ushort tileHoriz;
			private ushort best;
			private ushort listFlags;
			private ushort amount;
			private ushort bestFood;
			private ushort mapViewVert;
			private ushort tileVert;
			private ushort tileValue;
			private ushort bestValue;
			private ushort horiz;
			private ushort listStart;
			private ushort productionPrice;
			private ushort pollution;
			private ushort bestWeighted;
			private ushort vert;
			private ushort unitsAway;
			private ushort value;
			private ushort index;
			private ushort workersHigh;
			private ushort workersLow;
			private ushort key;
			private ushort measure;
			private ushort drawHoriz;
			private ushort factoryBonus;
			private ushort unitHoriz;
			private ushort drawVert;
			private ushort listEnd;
			private ushort unitVert;
			private ushort advisorPick;
			private ushort approach;
			private ushort selected;
			private ushort shieldsKept;
			private ushort continent;

			public ushort Content { get => this.content; set => this.content = value; }

			public ushort FactoryPower { get => this.factoryPower; set => this.factoryPower = value; }

			public ushort WorkersGained { get => this.workersGained; set => this.workersGained = value; }

			public ushort Target { get => this.target; set => this.target = value; }

			public ushort Distance { get => this.distance; set => this.distance = value; }

			public ushort ImprovementCount { get => this.improvementCount; set => this.improvementCount = value; }

			public ushort CursorHoriz { get => this.cursorHoriz; set => this.cursorHoriz = value; }

			public ushort CursorOn { get => this.cursorOn; set => this.cursorOn = value; }

			public ushort NewUnit { get => this.newUnit; set => this.newUnit = value; }

			public ushort Item { get => this.item; set => this.item = value; }

			public ushort CursorVert { get => this.cursorVert; set => this.cursorVert = value; }

			public ushort ChangeBlinks { get => this.changeBlinks; set => this.changeBlinks = value; }

			public ushort MapViewHoriz { get => this.mapViewHoriz; set => this.mapViewHoriz = value; }

			public ushort TileHoriz { get => this.tileHoriz; set => this.tileHoriz = value; }

			public ushort Best { get => this.best; set => this.best = value; }

			public ushort ListFlags { get => this.listFlags; set => this.listFlags = value; }

			public ushort Amount { get => this.amount; set => this.amount = value; }

			public ushort BestFood { get => this.bestFood; set => this.bestFood = value; }

			public ushort MapViewVert { get => this.mapViewVert; set => this.mapViewVert = value; }

			public ushort TileVert { get => this.tileVert; set => this.tileVert = value; }

			public ushort TileValue { get => this.tileValue; set => this.tileValue = value; }

			public ushort BestValue { get => this.bestValue; set => this.bestValue = value; }

			public ushort Horiz { get => this.horiz; set => this.horiz = value; }

			public ushort ListStart { get => this.listStart; set => this.listStart = value; }

			public ushort ProductionPrice { get => this.productionPrice; set => this.productionPrice = value; }

			public ushort Pollution { get => this.pollution; set => this.pollution = value; }

			public ushort BestWeighted { get => this.bestWeighted; set => this.bestWeighted = value; }

			public ushort Vert { get => this.vert; set => this.vert = value; }

			public ushort UnitsAway { get => this.unitsAway; set => this.unitsAway = value; }

			public ushort Value { get => this.value; set => this.value = value; }

			public ushort Index { get => this.index; set => this.index = value; }

			public ushort WorkersHigh { get => this.workersHigh; set => this.workersHigh = value; }

			public ushort WorkersLow { get => this.workersLow; set => this.workersLow = value; }

			public ushort Key { get => this.key; set => this.key = value; }

			public ushort Measure { get => this.measure; set => this.measure = value; }

			public ushort DrawHoriz { get => this.drawHoriz; set => this.drawHoriz = value; }

			public ushort FactoryBonus { get => this.factoryBonus; set => this.factoryBonus = value; }

			public ushort UnitHoriz { get => this.unitHoriz; set => this.unitHoriz = value; }

			public ushort DrawVert { get => this.drawVert; set => this.drawVert = value; }

			public ushort ListEnd { get => this.listEnd; set => this.listEnd = value; }

			public ushort UnitVert { get => this.unitVert; set => this.unitVert = value; }

			public ushort AdvisorPick { get => this.advisorPick; set => this.advisorPick = value; }

			public ushort Approach { get => this.approach; set => this.approach = value; }

			public ushort Selected { get => this.selected; set => this.selected = value; }

			public ushort ShieldsKept { get => this.shieldsKept; set => this.shieldsKept = value; }

			public ushort Continent { get => this.continent; set => this.continent = value; }

			public ushort MartialLawUnits { get => Frame(0x2); set => SetFrame(0x2, value); }

			public ushort TileFood { get => Frame(0x4); set => SetFrame(0x4, value); }

			public ushort IconHoriz { get => Frame(0x6); set => SetFrame(0x6, value); }

			public ushort Specialists { get => Frame(0x8); set => SetFrame(0x8, value); }

			public ushort TradeKept { get => Frame(0xa); set => SetFrame(0xa, value); }

			public ushort TileShields { get => Frame(0xc); set => SetFrame(0xc, value); }

			public ushort CityTileTaken { get => Frame(0x16); set => SetFrame(0x16, value); }

			public ushort WonderRows { get => Frame(0x40); set => SetFrame(0x40, value); }

			public ushort IconVert { get => Frame(0x42); set => SetFrame(0x42, value); }

			public ushort ShieldsPerRow { get => Frame(0x44); set => SetFrame(0x44, value); }

			public ushort TileTrade { get => Frame(0x46); set => SetFrame(0x46, value); }

			public ushort SettlerFood { get => Frame(0x48); set => SetFrame(0x48, value); }

			public ushort CostFactor { get => Frame(0x4a); set => SetFrame(0x4a, value); }

			public ushort OtherCity { get => Frame(0x4c); set => SetFrame(0x4c, value); }

			public ushort UnitsDrawn { get => Frame(0x4e); set => SetFrame(0x4e, value); }

			public ushort Workers { get => Frame(0x50); set => SetFrame(0x50, value); }

			public ushort CityTileNeighbour { get => Frame(0x8e); set => SetFrame(0x8e, value); }

			public ushort Found { get => Frame(0x108); set => SetFrame(0x108, value); }

			public ushort Frame(int distance) => this.frame.Frame(distance);

			public void SetFrame(int distance, ushort value) => this.frame.SetFrame(distance, value);
		}
	}
}
