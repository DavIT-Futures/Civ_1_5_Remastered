
using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	partial class Cities
	{
		/// <summary>
		/// The locals of HandleCity from BP-0xa8 up, shared by all the parts of the round (the names say what they hold most of
		/// the time; several are reused). Every call of HandleCity has its own (it calls itself through the advisor); no part
		/// reads one before writing it in the same call (checked with the frame filled with 0x00 / 0xff at the start and the
		/// end), so they need not start with what the stack held. Every write still goes to the frame too: later functions at
		/// the same depth read these words as uninitialized locals (0x108 of a nested HandleCity gets 0xe8 of the outer one;
		/// Tests/Dual/LeftoverWatcher.cs). The lower words stay in the frame: the list of the units in the city (BP-0x74 up) runs over them when the
		/// city has many units, the arrays at BP-0x3e (the tiles not to work) and BP-0x8e (the tiles of neighbours) lie
		/// between them, the unused words of the unit list and 0x108 (the last tile found) are read before they are written.
		/// </summary>
		private sealed class CityRound(CPU processor)
		{
			/// <summary>
			/// The words of the frame of the original below BP (index distance / 2, distance 2 - 0x10c), in C# since B3
			/// (2026-10-26): 0 at the start like the zeroed frame. The arrays of the frame and the locals read through it
			/// share them as in the original.
			/// </summary>
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

			/// <summary>BP-0xa8: the content citizens (written only)</summary>
			public ushort Content { get => this.content; set => Keep(ref this.content, 0xa8, value); }

			/// <summary>BP-0xac: the power plant bonus of the factories</summary>
			public ushort FactoryPower { get => this.factoryPower; set => Keep(ref this.factoryPower, 0xac, value); }

			/// <summary>BP-0xae: the workers gained (-0x50 after the workers are placed)</summary>
			public ushort WorkersGained { get => this.workersGained; set => Keep(ref this.workersGained, 0xae, value); }

			/// <summary>BP-0xb0: the distance to a city; the city a diplomat goes to</summary>
			public ushort Target { get => this.target; set => Keep(ref this.target, 0xb0, value); }

			/// <summary>BP-0xb2: the distance to the nearest city (at most 7); the distance of a unit</summary>
			public ushort Distance { get => this.distance; set => Keep(ref this.distance, 0xb2, value); }

			/// <summary>BP-0xb4: the improvements in the list</summary>
			public ushort ImprovementCount { get => this.improvementCount; set => Keep(ref this.improvementCount, 0xb4, value); }

			/// <summary>BP-0xb6: the column of the map cursor</summary>
			public ushort CursorHoriz { get => this.cursorHoriz; set => Keep(ref this.cursorHoriz, 0xb6, value); }

			/// <summary>BP-0xb8: the map cursor is on (P)</summary>
			public ushort CursorOn { get => this.cursorOn; set => Keep(ref this.cursorOn, 0xb8, value); }

			/// <summary>BP-0xba: the unit made in the turn, -1 when none</summary>
			public ushort NewUnit { get => this.newUnit; set => Keep(ref this.newUnit, 0xba, value); }

			/// <summary>BP-0xbc: the improvement, wonder or trade route at hand</summary>
			public ushort Item { get => this.item; set => Keep(ref this.item, 0xbc, value); }

			/// <summary>BP-0xbe: the row of the map cursor</summary>
			public ushort CursorVert { get => this.cursorVert; set => Keep(ref this.cursorVert, 0xbe, value); }

			/// <summary>BP-0xc0: the Change button blinks; its colour count</summary>
			public ushort ChangeBlinks { get => this.changeBlinks; set => Keep(ref this.changeBlinks, 0xc0, value); }

			/// <summary>BP-0xc2: the map view before the city screen (0xd4cc)</summary>
			public ushort MapViewHoriz { get => this.mapViewHoriz; set => Keep(ref this.mapViewHoriz, 0xc2, value); }

			/// <summary>BP-0xc6: the place of the tile at hand</summary>
			public ushort TileHoriz { get => this.tileHoriz; set => Keep(ref this.tileHoriz, 0xc6, value); }

			/// <summary>BP-0xc8: the best (lowest or highest) value of a search; the spaceship success</summary>
			public ushort Best { get => this.best; set => Keep(ref this.best, 0xc8, value); }

			/// <summary>BP-0xca: the list of improvements: bit 0 the other page, bit 1 more to come</summary>
			public ushort ListFlags { get => this.listFlags; set => Keep(ref this.listFlags, 0xca, value); }

			/// <summary>BP-0xcc: the food of a tile; the shields bought</summary>
			public ushort Amount { get => this.amount; set => Keep(ref this.amount, 0xcc, value); }

			/// <summary>BP-0xce: the most food of a tile so far</summary>
			public ushort BestFood { get => this.bestFood; set => Keep(ref this.bestFood, 0xce, value); }

			/// <summary>BP-0xd0: the map view before the city screen (0xd75e)</summary>
			public ushort MapViewVert { get => this.mapViewVert; set => Keep(ref this.mapViewVert, 0xd0, value); }

			/// <summary>BP-0xd2: the place of the tile at hand</summary>
			public ushort TileVert { get => this.tileVert; set => Keep(ref this.tileVert, 0xd2, value); }

			/// <summary>BP-0xd4: the shields x 2 and the trade of a tile</summary>
			public ushort TileValue { get => this.tileValue; set => Keep(ref this.tileValue, 0xd4, value); }

			/// <summary>BP-0xd6: the best value of a tile so far</summary>
			public ushort BestValue { get => this.bestValue; set => Keep(ref this.bestValue, 0xd6, value); }

			/// <summary>BP-0xd8: the column of the city (or of a loop over the map)</summary>
			public ushort Horiz { get => this.horiz; set => Keep(ref this.horiz, 0xd8, value); }

			/// <summary>BP-0xda: the first improvement of the list page</summary>
			public ushort ListStart { get => this.listStart; set => Keep(ref this.listStart, 0xda, value); }

			/// <summary>BP-0xde: the price of the production</summary>
			public ushort ProductionPrice { get => this.productionPrice; set => Keep(ref this.productionPrice, 0xde, value); }

			/// <summary>BP-0xe0: the pollution of the city</summary>
			public ushort Pollution { get => this.pollution; set => Keep(ref this.pollution, 0xe0, value); }

			/// <summary>BP-0xe2: the best weighted yield (written only)</summary>
			public ushort BestWeighted { get => this.bestWeighted; set => Keep(ref this.bestWeighted, 0xe2, value); }

			/// <summary>BP-0xe4: the row of the city (or of a loop over the map)</summary>
			public ushort Vert { get => this.vert; set => Keep(ref this.vert, 0xe4, value); }

			/// <summary>BP-0xe6: the attack units away (unhappiness)</summary>
			public ushort UnitsAway { get => this.unitsAway; set => Keep(ref this.unitsAway, 0xe6, value); }

			/// <summary>BP-0xe8: a value at hand (many uses)</summary>
			public ushort Value { get => this.value; set => Keep(ref this.value, 0xe8, value); }

			/// <summary>BP-0xea: a tile, citizen or unit at hand (many uses)</summary>
			public ushort Index { get => this.index; set => Keep(ref this.index, 0xea, value); }

			/// <summary>BP-0xec: the worked tiles, bits 16 - 31</summary>
			public ushort WorkersHigh { get => this.workersHigh; set => Keep(ref this.workersHigh, 0xec, value); }

			/// <summary>BP-0xee: the worked tiles, bits 0 - 15</summary>
			public ushort WorkersLow { get => this.workersLow; set => Keep(ref this.workersLow, 0xee, value); }

			/// <summary>BP-0xf0: the key of the city screen, -1 for a click</summary>
			public ushort Key { get => this.key; set => Keep(ref this.key, 0xf0, value); }

			/// <summary>BP-0xf4: the width of a citizen; the cost of an advance</summary>
			public ushort Measure { get => this.measure; set => Keep(ref this.measure, 0xf4, value); }

			/// <summary>BP-0xf6: the column where the city screen draws</summary>
			public ushort DrawHoriz { get => this.drawHoriz; set => Keep(ref this.drawHoriz, 0xf6, value); }

			/// <summary>BP-0xf8: the factory bonus (2 the factory, 4 the manufacturing plant)</summary>
			public ushort FactoryBonus { get => this.factoryBonus; set => Keep(ref this.factoryBonus, 0xf8, value); }

			/// <summary>BP-0xfa: the column of a unit in the city</summary>
			public ushort UnitHoriz { get => this.unitHoriz; set => Keep(ref this.unitHoriz, 0xfa, value); }

			/// <summary>BP-0xfc: the row where the city screen draws</summary>
			public ushort DrawVert { get => this.drawVert; set => Keep(ref this.drawVert, 0xfc, value); }

			/// <summary>BP-0xfe: the improvement the list page stopped at</summary>
			public ushort ListEnd { get => this.listEnd; set => Keep(ref this.listEnd, 0xfe, value); }

			/// <summary>BP-0x100: the row of a unit in the city</summary>
			public ushort UnitVert { get => this.unitVert; set => Keep(ref this.unitVert, 0x100, value); }

			/// <summary>BP-0x102: the production the advisor picks</summary>
			public ushort AdvisorPick { get => this.advisorPick; set => Keep(ref this.advisorPick, 0x102, value); }

			/// <summary>BP-0x104: the attitude of the continent</summary>
			public ushort Approach { get => this.approach; set => Keep(ref this.approach, 0x104, value); }

			/// <summary>BP-0x106: the row of a list, a unit or an improvement at hand</summary>
			public ushort Selected { get => this.selected; set => Keep(ref this.selected, 0x106, value); }

			/// <summary>BP-0x10a: the shields 0x70dc kept for the city screen</summary>
			public ushort ShieldsKept { get => this.shieldsKept; set => Keep(ref this.shieldsKept, 0x10a, value); }

			/// <summary>BP-0x10c: a continent at hand</summary>
			public ushort Continent { get => this.continent; set => Keep(ref this.continent, 0x10c, value); }

			/// <summary>BP-0x2 (on the stack): the units looked at for martial law (at most 0x20)</summary>
			public ushort MartialLawUnits { get => Frame(0x2); set => SetFrame(0x2, value); }

			/// <summary>BP-0x4 (on the stack): the food of the tile looked at</summary>
			public ushort TileFood { get => Frame(0x4); set => SetFrame(0x4, value); }

			/// <summary>BP-0x6 (on the stack): the column of the next icon in the row of the citizens</summary>
			public ushort IconHoriz { get => Frame(0x6); set => SetFrame(0x6, value); }

			/// <summary>BP-0x8 (on the stack): the specialists of the city</summary>
			public ushort Specialists { get => Frame(0x8); set => SetFrame(0x8, value); }

			/// <summary>BP-0xa (on the stack): the trade 0x70de kept for the city screen</summary>
			public ushort TradeKept { get => Frame(0xa); set => SetFrame(0xa, value); }

			/// <summary>BP-0xc (on the stack): the shields of the tile looked at</summary>
			public ushort TileShields { get => Frame(0xc); set => SetFrame(0xc, value); }

			/// <summary>BP-0x16 (on the stack): the word of the city tile (20) in the array of the tiles not to work (BP-0x3e)</summary>
			public ushort CityTileTaken { get => Frame(0x16); set => SetFrame(0x16, value); }

			/// <summary>BP-0x40 (on the stack): the wonders in the list of improvements</summary>
			public ushort WonderRows { get => Frame(0x40); set => SetFrame(0x40, value); }

			/// <summary>BP-0x42 (on the stack): the line of the icons of the citizens</summary>
			public ushort IconVert { get => Frame(0x42); set => SetFrame(0x42, value); }

			/// <summary>BP-0x44 (on the stack): the shields per row of the production box</summary>
			public ushort ShieldsPerRow { get => Frame(0x44); set => SetFrame(0x44, value); }

			/// <summary>BP-0x46 (on the stack): the trade of the tile looked at</summary>
			public ushort TileTrade { get => Frame(0x46); set => SetFrame(0x46, value); }

			/// <summary>BP-0x48 (on the stack): the food of a settler (1 under despotism or anarchy, else 2)</summary>
			public ushort SettlerFood { get => Frame(0x48); set => SetFrame(0x48, value); }

			/// <summary>BP-0x4a (on the stack): the factor of the prices (10, for a computer player by the level)</summary>
			public ushort CostFactor { get => Frame(0x4a); set => SetFrame(0x4a, value); }

			/// <summary>BP-0x4c (on the stack): a city looked at; the city at the end of a trade route</summary>
			public ushort OtherCity { get => Frame(0x4c); set => SetFrame(0x4c, value); }

			/// <summary>BP-0x4e (on the stack): the units drawn in the city</summary>
			public ushort UnitsDrawn { get => Frame(0x4e); set => SetFrame(0x4e, value); }

			/// <summary>BP-0x50 (on the stack): the workers still to place; the size + 1</summary>
			public ushort Workers { get => Frame(0x50); set => SetFrame(0x50, value); }

			/// <summary>BP-0x8e (on the stack): the word of the city tile in the array of the tiles of neighbours (BP-0x8e)</summary>
			public ushort CityTileNeighbour { get => Frame(0x8e); set => SetFrame(0x8e, value); }

			/// <summary>BP-0x108 (on the stack): the tile the last search found (-1 none) or a unit; PickWorkers reads it before anything writes it</summary>
			public ushort Found { get => Frame(0x108); set => SetFrame(0x108, value); }

			/// <summary>The word at BP - distance (FrameWords)</summary>
			public ushort Frame(int distance) => this.frame.Frame(distance);

			/// <summary>Writes the word at BP - distance (FrameWords)</summary>
			public void SetFrame(int distance, ushort value) => this.frame.SetFrame(distance, value);

			/// <summary>
			/// The value (the distance is the word of the frame where the original kept it; B3, 2026-10-26: no longer written
			/// there, the frames are zeroed and nothing reads these words: the arrays of the frame end at 0xa6)
			/// </summary>
			private static void Keep(ref ushort local, int distance, ushort value)
			{
				local = value;
			}
		}
	}
}
