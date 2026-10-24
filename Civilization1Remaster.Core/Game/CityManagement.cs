
using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1d12.
	/// </summary>
	public class CityManagement : GameModule
	{
		private readonly UnitMemory troops;
		private CityRound round;

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
			/// <summary>The value, and the word of the frame as the original writes it (other functions read it later)</summary>
			private void Keep(ref ushort local, int distance, ushort value)
			{
				local = value;
				processor.EmitLocalWord(distance, value);
			}

			private ushort content;
			/// <summary>BP-0xa8: the content citizens (written only)</summary>
			public ushort Content { get => this.content; set => Keep(ref this.content, 0xa8, value); }

			private ushort factoryPower;
			/// <summary>BP-0xac: the power plant bonus of the factories</summary>
			public ushort FactoryPower { get => this.factoryPower; set => Keep(ref this.factoryPower, 0xac, value); }

			private ushort workersGained;
			/// <summary>BP-0xae: the workers gained (-0x50 after the workers are placed)</summary>
			public ushort WorkersGained { get => this.workersGained; set => Keep(ref this.workersGained, 0xae, value); }

			private ushort target;
			/// <summary>BP-0xb0: the distance to a city; the city a diplomat goes to</summary>
			public ushort Target { get => this.target; set => Keep(ref this.target, 0xb0, value); }

			private ushort distance;
			/// <summary>BP-0xb2: the distance to the nearest city (at most 7); the distance of a unit</summary>
			public ushort Distance { get => this.distance; set => Keep(ref this.distance, 0xb2, value); }

			private ushort improvementCount;
			/// <summary>BP-0xb4: the improvements in the list</summary>
			public ushort ImprovementCount { get => this.improvementCount; set => Keep(ref this.improvementCount, 0xb4, value); }

			private ushort cursorHoriz;
			/// <summary>BP-0xb6: the column of the map cursor</summary>
			public ushort CursorHoriz { get => this.cursorHoriz; set => Keep(ref this.cursorHoriz, 0xb6, value); }

			private ushort cursorOn;
			/// <summary>BP-0xb8: the map cursor is on (P)</summary>
			public ushort CursorOn { get => this.cursorOn; set => Keep(ref this.cursorOn, 0xb8, value); }

			private ushort newUnit;
			/// <summary>BP-0xba: the unit made in the turn, -1 when none</summary>
			public ushort NewUnit { get => this.newUnit; set => Keep(ref this.newUnit, 0xba, value); }

			private ushort item;
			/// <summary>BP-0xbc: the improvement, wonder or trade route at hand</summary>
			public ushort Item { get => this.item; set => Keep(ref this.item, 0xbc, value); }

			private ushort cursorVert;
			/// <summary>BP-0xbe: the row of the map cursor</summary>
			public ushort CursorVert { get => this.cursorVert; set => Keep(ref this.cursorVert, 0xbe, value); }

			private ushort changeBlinks;
			/// <summary>BP-0xc0: the Change button blinks; its colour count</summary>
			public ushort ChangeBlinks { get => this.changeBlinks; set => Keep(ref this.changeBlinks, 0xc0, value); }

			private ushort mapViewHoriz;
			/// <summary>BP-0xc2: the map view before the city screen (0xd4cc)</summary>
			public ushort MapViewHoriz { get => this.mapViewHoriz; set => Keep(ref this.mapViewHoriz, 0xc2, value); }

			private ushort tileHoriz;
			/// <summary>BP-0xc6: the place of the tile at hand</summary>
			public ushort TileHoriz { get => this.tileHoriz; set => Keep(ref this.tileHoriz, 0xc6, value); }

			private ushort best;
			/// <summary>BP-0xc8: the best (lowest or highest) value of a search; the spaceship success</summary>
			public ushort Best { get => this.best; set => Keep(ref this.best, 0xc8, value); }

			private ushort listFlags;
			/// <summary>BP-0xca: the list of improvements: bit 0 the other page, bit 1 more to come</summary>
			public ushort ListFlags { get => this.listFlags; set => Keep(ref this.listFlags, 0xca, value); }

			private ushort amount;
			/// <summary>BP-0xcc: the food of a tile; the shields bought</summary>
			public ushort Amount { get => this.amount; set => Keep(ref this.amount, 0xcc, value); }

			private ushort bestFood;
			/// <summary>BP-0xce: the most food of a tile so far</summary>
			public ushort BestFood { get => this.bestFood; set => Keep(ref this.bestFood, 0xce, value); }

			private ushort mapViewVert;
			/// <summary>BP-0xd0: the map view before the city screen (0xd75e)</summary>
			public ushort MapViewVert { get => this.mapViewVert; set => Keep(ref this.mapViewVert, 0xd0, value); }

			private ushort tileVert;
			/// <summary>BP-0xd2: the place of the tile at hand</summary>
			public ushort TileVert { get => this.tileVert; set => Keep(ref this.tileVert, 0xd2, value); }

			private ushort tileValue;
			/// <summary>BP-0xd4: the shields x 2 and the trade of a tile</summary>
			public ushort TileValue { get => this.tileValue; set => Keep(ref this.tileValue, 0xd4, value); }

			private ushort bestValue;
			/// <summary>BP-0xd6: the best value of a tile so far</summary>
			public ushort BestValue { get => this.bestValue; set => Keep(ref this.bestValue, 0xd6, value); }

			private ushort horiz;
			/// <summary>BP-0xd8: the column of the city (or of a loop over the map)</summary>
			public ushort Horiz { get => this.horiz; set => Keep(ref this.horiz, 0xd8, value); }

			private ushort listStart;
			/// <summary>BP-0xda: the first improvement of the list page</summary>
			public ushort ListStart { get => this.listStart; set => Keep(ref this.listStart, 0xda, value); }

			private ushort productionPrice;
			/// <summary>BP-0xde: the price of the production</summary>
			public ushort ProductionPrice { get => this.productionPrice; set => Keep(ref this.productionPrice, 0xde, value); }

			private ushort pollution;
			/// <summary>BP-0xe0: the pollution of the city</summary>
			public ushort Pollution { get => this.pollution; set => Keep(ref this.pollution, 0xe0, value); }

			private ushort bestWeighted;
			/// <summary>BP-0xe2: the best weighted yield (written only)</summary>
			public ushort BestWeighted { get => this.bestWeighted; set => Keep(ref this.bestWeighted, 0xe2, value); }

			private ushort vert;
			/// <summary>BP-0xe4: the row of the city (or of a loop over the map)</summary>
			public ushort Vert { get => this.vert; set => Keep(ref this.vert, 0xe4, value); }

			private ushort unitsAway;
			/// <summary>BP-0xe6: the attack units away (unhappiness)</summary>
			public ushort UnitsAway { get => this.unitsAway; set => Keep(ref this.unitsAway, 0xe6, value); }

			private ushort value;
			/// <summary>BP-0xe8: a value at hand (many uses)</summary>
			public ushort Value { get => this.value; set => Keep(ref this.value, 0xe8, value); }

			private ushort index;
			/// <summary>BP-0xea: a tile, citizen or unit at hand (many uses)</summary>
			public ushort Index { get => this.index; set => Keep(ref this.index, 0xea, value); }

			private ushort workersHigh;
			/// <summary>BP-0xec: the worked tiles, bits 16 - 31</summary>
			public ushort WorkersHigh { get => this.workersHigh; set => Keep(ref this.workersHigh, 0xec, value); }

			private ushort workersLow;
			/// <summary>BP-0xee: the worked tiles, bits 0 - 15</summary>
			public ushort WorkersLow { get => this.workersLow; set => Keep(ref this.workersLow, 0xee, value); }

			private ushort key;
			/// <summary>BP-0xf0: the key of the city screen, -1 for a click</summary>
			public ushort Key { get => this.key; set => Keep(ref this.key, 0xf0, value); }

			private ushort measure;
			/// <summary>BP-0xf4: the width of a citizen; the cost of an advance</summary>
			public ushort Measure { get => this.measure; set => Keep(ref this.measure, 0xf4, value); }

			private ushort drawHoriz;
			/// <summary>BP-0xf6: the column where the city screen draws</summary>
			public ushort DrawHoriz { get => this.drawHoriz; set => Keep(ref this.drawHoriz, 0xf6, value); }

			private ushort factoryBonus;
			/// <summary>BP-0xf8: the factory bonus (2 the factory, 4 the manufacturing plant)</summary>
			public ushort FactoryBonus { get => this.factoryBonus; set => Keep(ref this.factoryBonus, 0xf8, value); }

			private ushort unitHoriz;
			/// <summary>BP-0xfa: the column of a unit in the city</summary>
			public ushort UnitHoriz { get => this.unitHoriz; set => Keep(ref this.unitHoriz, 0xfa, value); }

			private ushort drawVert;
			/// <summary>BP-0xfc: the row where the city screen draws</summary>
			public ushort DrawVert { get => this.drawVert; set => Keep(ref this.drawVert, 0xfc, value); }

			private ushort listEnd;
			/// <summary>BP-0xfe: the improvement the list page stopped at</summary>
			public ushort ListEnd { get => this.listEnd; set => Keep(ref this.listEnd, 0xfe, value); }

			private ushort unitVert;
			/// <summary>BP-0x100: the row of a unit in the city</summary>
			public ushort UnitVert { get => this.unitVert; set => Keep(ref this.unitVert, 0x100, value); }

			private ushort advisorPick;
			/// <summary>BP-0x102: the production the advisor picks</summary>
			public ushort AdvisorPick { get => this.advisorPick; set => Keep(ref this.advisorPick, 0x102, value); }

			private ushort approach;
			/// <summary>BP-0x104: the attitude of the continent</summary>
			public ushort Approach { get => this.approach; set => Keep(ref this.approach, 0x104, value); }

			private ushort selected;
			/// <summary>BP-0x106: the row of a list, a unit or an improvement at hand</summary>
			public ushort Selected { get => this.selected; set => Keep(ref this.selected, 0x106, value); }

			private ushort shieldsKept;
			/// <summary>BP-0x10a: the shields 0x70dc kept for the city screen</summary>
			public ushort ShieldsKept { get => this.shieldsKept; set => Keep(ref this.shieldsKept, 0x10a, value); }

			private ushort continent;
			/// <summary>BP-0x10c: a continent at hand</summary>
			public ushort Continent { get => this.continent; set => Keep(ref this.continent, 0x10c, value); }

			/// <summary>BP-0x2 (on the stack): the units looked at for martial law (at most 0x20)</summary>
			public ushort MartialLawUnits { get => processor.PullLocalWord(0x2); set => processor.EmitLocalWord(0x2, value); }
			/// <summary>BP-0x4 (on the stack): the food of the tile looked at</summary>
			public ushort TileFood { get => processor.PullLocalWord(0x4); set => processor.EmitLocalWord(0x4, value); }
			/// <summary>BP-0x6 (on the stack): the column of the next icon in the row of the citizens</summary>
			public ushort IconHoriz { get => processor.PullLocalWord(0x6); set => processor.EmitLocalWord(0x6, value); }
			/// <summary>BP-0x8 (on the stack): the specialists of the city</summary>
			public ushort Specialists { get => processor.PullLocalWord(0x8); set => processor.EmitLocalWord(0x8, value); }
			/// <summary>BP-0xa (on the stack): the trade 0x70de kept for the city screen</summary>
			public ushort TradeKept { get => processor.PullLocalWord(0xa); set => processor.EmitLocalWord(0xa, value); }
			/// <summary>BP-0xc (on the stack): the shields of the tile looked at</summary>
			public ushort TileShields { get => processor.PullLocalWord(0xc); set => processor.EmitLocalWord(0xc, value); }
			/// <summary>BP-0x16 (on the stack): the word of the city tile (20) in the array of the tiles not to work (BP-0x3e)</summary>
			public ushort CityTileTaken { get => processor.PullLocalWord(0x16); set => processor.EmitLocalWord(0x16, value); }
			/// <summary>BP-0x40 (on the stack): the wonders in the list of improvements</summary>
			public ushort WonderRows { get => processor.PullLocalWord(0x40); set => processor.EmitLocalWord(0x40, value); }
			/// <summary>BP-0x42 (on the stack): the line of the icons of the citizens</summary>
			public ushort IconVert { get => processor.PullLocalWord(0x42); set => processor.EmitLocalWord(0x42, value); }
			/// <summary>BP-0x44 (on the stack): the shields per row of the production box</summary>
			public ushort ShieldsPerRow { get => processor.PullLocalWord(0x44); set => processor.EmitLocalWord(0x44, value); }
			/// <summary>BP-0x46 (on the stack): the trade of the tile looked at</summary>
			public ushort TileTrade { get => processor.PullLocalWord(0x46); set => processor.EmitLocalWord(0x46, value); }
			/// <summary>BP-0x48 (on the stack): the food of a settler (1 under despotism or anarchy, else 2)</summary>
			public ushort SettlerFood { get => processor.PullLocalWord(0x48); set => processor.EmitLocalWord(0x48, value); }
			/// <summary>BP-0x4a (on the stack): the factor of the prices (10, for a computer player by the level)</summary>
			public ushort CostFactor { get => processor.PullLocalWord(0x4a); set => processor.EmitLocalWord(0x4a, value); }
			/// <summary>BP-0x4c (on the stack): a city looked at; the city at the end of a trade route</summary>
			public ushort OtherCity { get => processor.PullLocalWord(0x4c); set => processor.EmitLocalWord(0x4c, value); }
			/// <summary>BP-0x4e (on the stack): the units drawn in the city</summary>
			public ushort UnitsDrawn { get => processor.PullLocalWord(0x4e); set => processor.EmitLocalWord(0x4e, value); }
			/// <summary>BP-0x50 (on the stack): the workers still to place; the size + 1</summary>
			public ushort Workers { get => processor.PullLocalWord(0x50); set => processor.EmitLocalWord(0x50, value); }
			/// <summary>BP-0x8e (on the stack): the word of the city tile in the array of the tiles of neighbours (BP-0x8e)</summary>
			public ushort CityTileNeighbour { get => processor.PullLocalWord(0x8e); set => processor.EmitLocalWord(0x8e, value); }
			/// <summary>BP-0x108 (on the stack): the tile the last search found (-1 none) or a unit; PickWorkers reads it before anything writes it</summary>
			public ushort Found { get => processor.PullLocalWord(0x108); set => processor.EmitLocalWord(0x108, value); }
		}

		public CityManagement(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
			this.round = new CityRound(owner.Processor);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ObtainCityTileIndex(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainCityTileIndex({horizLoc}, {vertLoc})");

			int index = CitizenSpecialistRules.FindCityTileIndex(horizLoc, vertLoc);
			this.processor.RegAcc.Value16 = (index >= 0) ? (ushort)index : (ushort)0xffff;

			this.processor.Journal.CloseScope("ObtainCityTileIndex");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The city screen, first part: the units of the city listed, the screen cleared, the title "NAME (Pop:...)" (0xe8
		/// the population sum 1 + 2 + ... + size), the map of the city (the view 0xd4cc / 0xd75e on it): each of the 0x6540
		/// tiles around that the owner has seen (all for a computer player) is drawn, a tile of another player only repainted,
		/// a tile worked by a neighbouring city (the array at BP-0x8e) framed. The locals of HandleCity and BX, SI, DI are used
		/// as in the original.
		/// </summary>
		private void RenderCityHeaderAndMap(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.owner.UnitOps.ConstructUnitsFromCityList(townIdx);
			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);
			RenderFrame(2, 1, 208, 21);

			// The population: 1 + 2 + ... + the size
			this.round.Value = 0;
			ushort citizen = 1;

			for (; town.TrueExtent >= (short)citizen; citizen++)
			{
				this.round.Index = citizen;
				this.round.Value = (ushort)(this.round.Value + citizen);
			}

			this.round.Index = citizen;
			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 2);

			EmptyMessage()
				.City(townIdx);
			this.owner.Libc.UppercaseText(0xba06);
			this.owner.Libc.AppendText(0xba06, " (Pop:");
			this.owner.Helpers.PopulationToText((short)this.round.Value);
			this.owner.Libc.AppendText(0xba06, ")");
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 104, 2, 15);

			RenderFrame(127, 23, 208, 104);

			// The map of the city
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitGlobalWord(0xd4cc, (ushort)((short)town.Place.X - 5));
			this.processor.EmitGlobalWord(0xd75e, (ushort)((short)town.Place.Y - 3));
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			this.owner.MapOps.RenderMapTile(town.Place.X, town.Place.Y);

			ushort tile = 0;

			for (; (short)tile < (short)CityTiles; tile++)
			{
				this.round.Index = tile;
				short horizLoc = (short)(town.Place.X + this.owner.TownDeltas[tile].X);
				short vertLoc = (short)(town.Place.Y + this.owner.TownDeltas[tile].Y);
				this.round.TileHoriz = (ushort)horizLoc;
				this.round.TileVert = (ushort)vertLoc;

				short owner = (short)CityOwner;

				if (owner == this.owner.World.PersonParticipantIdx &&
					(this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
				{
					continue;
				}

				this.round.Value = this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				if (this.round.Value != 0xffff && this.round.Value != (ushort)owner)
				{
					// A tile of another player
					this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
					continue;
				}

				this.owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				this.processor.RegSource.Value16 = (ushort)(tile << 1);
				this.processor.RegDest.Value16 = (ushort)(short)(10 * this.owner.TownDeltas[tile].X + this.owner.TownDeltas[tile].Y * 2);

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e)) != 0)
				{
					// A tile worked by a neighbouring city
					this.owner.Dialogs.RenderBox(
						(horizLoc - this.processor.PullGlobalSignedWord(0xd4cc)) * 16 + 80,
						(vertLoc - this.processor.PullGlobalSignedWord(0xd75e)) * 16 + 8,
						15, 15, 12);
				}
			}

			this.round.Index = tile;
		}

		/// <summary>
		/// The food of the city in its turn: the flag 0x80 cleared; with the food storage below zero a settler of the city
		/// starves (the human is told), else the city is lost (the translated code tests the flags of the earlier comparison
		/// instead of the size, so it is always lost: kept, see the backlog); with the storage at (size + 1) x 0x4a the city
		/// grows (a granary keeps 5 x size + 5), above 10 without an aqueduct it stays (with the bit 4 of DiagMarkers; the human
		/// is told). False when the city is lost. The locals of HandleCity and BX, SI are used as in the original.
		/// </summary>
		private bool FeedCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			town.ConditionMarker &= 0x7f;

			if (town.GrainTally < 0)
			{
				// Starvation: a settler of the city, or the city
				this.round.Value = 0xffff;
				ushort troop = 0;

				for (; troop < UnitMemory.TroopTally; troop++)
				{
					this.round.Index = troop;
					this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
					this.processor.RegSource.Value16 = (ushort)(troop * UnitMemory.TroopStride);

					if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0 && this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) == townIdx)
					{
						this.round.Value = troop;
						break;
					}
				}

				if (troop == UnitMemory.TroopTally)
					this.round.Index = troop;

				if (owner == this.owner.World.PersonParticipantIdx)
				{
					Message("Food storage exhausted\nin ")
						.City(townIdx)
						.Text((ushort)(this.round.Value != 0xffff ? 0x2510 : 0x2522));
					this.owner.Services.PerformSound(0x24, 0);
					this.owner.Paper.DisplayNewspaper(townIdx);
					this.owner.Services.PerformSound(1, 0);
				}

				if (this.round.Value != 0xffff)
				{
					this.owner.UnitOps.EliminateUnit(owner, (short)this.round.Value);
				}
				else
				{
					// The city is lost (whatever its size, as the translated code does)
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
					town.TrueExtent--;
					this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
					this.owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
					this.owner.NewGameWizard.EliminateCivilization((short)CityOwner, 0);
					this.owner.Startup.DisplayMouseCursor();
					return false;
				}

				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				town.GrainTally = 0;
				this.owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			}

			// Growth
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			if ((short)((town.TrueExtent + 1) * (short)this.round.CostFactor) > town.GrainTally)
				return true;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			town.TrueExtent++;
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			// A granary keeps half of the food
			town.GrainTally = (short)(town.Has(CityStructure.Granary) ? (short)(5 * town.TrueExtent) + 5 : 0);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.TrueExtent > 10 && !town.Has(CityStructure.Aqueduct) && (this.owner.World.DiagMarkers & 4) != 0)
			{
				// No growth above 10 without an aqueduct
				town.TrueExtent--;

				if (CityOwner == (ushort)this.owner.World.PersonParticipantIdx)
				{
					DialogStyle = 4;
					EmptyMessage()
						.City(townIdx)
						.Text(" requires an AQUEDUCT\nfor further growth.\n")
						.Dialog(100, 80);
				}
			}

			this.owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			return true;
		}

		/// <summary>
		/// The units of the city: 0x50 the size + 1; 0xdeb8 the special slots of the city in use (Unfamiliar) and the units
		/// it supports (but diplomats and caravans), 0xd2f6 those above the size (and every one for a government above
		/// monarchy with the bit 2 of DiagMarkers), 0x6546 the attackers away from the city (planes always), 0xe3c6 the
		/// settlers. BX, SI are left as in the original.
		/// </summary>
		private void CountCityUnits(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.round.Workers = (ushort)(town.TrueExtent + 1);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0x6546, 0);
			this.processor.EmitGlobalWord(0xdeb8, 0);
			this.processor.EmitGlobalWord(0xd2f6, 0);
			this.processor.EmitGlobalWord(0xe3c6, 0);

			// The special slots of the city
			CountSpecialSlots(townIdx);

			// The units the city supports
			for (ushort troop = 0; troop < UnitMemory.TroopTally; troop++)
			{
				this.round.Index = troop;
				this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(troop * UnitMemory.TroopStride);

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) != townIdx)
					continue;

				if (this.troops.PullSigned(owner, troop, UnitMemory.Kind) < 0x1a)
				{
					this.processor.EmitGlobalWord(0xdeb8, (ushort)(this.processor.PullGlobalWord(0xdeb8) + 1));

					if (town.TrueExtent < (short)this.processor.PullGlobalWord(0xdeb8) ||
						(this.owner.World.Participants[owner].RegimeKind > 1 && (this.owner.World.DiagMarkers & 2) != 0))
					{
						this.processor.EmitGlobalWord(0xd2f6, (ushort)(this.processor.PullGlobalWord(0xd2f6) + 1));
					}

					// An attacker away from the city (a plane always)
					if (this.troops.KindField(owner, troop, UnitKindTable.AssaultPower) != 0 &&
						(this.troops.KindField(owner, troop, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troop, UnitMemory.Horiz) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troop, UnitMemory.Vert) != (byte)(sbyte)town.Place.Y))
					{
						this.processor.EmitGlobalWord(0x6546, (ushort)(this.processor.PullGlobalWord(0x6546) + 1));
					}

					this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
					this.processor.RegSource.Value16 = (ushort)(troop * UnitMemory.TroopStride);
				}

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0)
					this.processor.EmitGlobalWord(0xe3c6, (ushort)(this.processor.PullGlobalWord(0xe3c6) + 1));
			}

			this.round.Index = UnitMemory.TroopTally;
		}

		/// <summary>
		/// The tiles the city cannot work (the array at BP-0x3e, one word per tile around): those the owner has not seen (not
		/// for the barbarians), those of another player (marked also in the array at BP-0x8e; 0x70e8 = 1 when that is the
		/// human) and those worked by a neighbouring city; their workers are taken off. Then the 4 words at 0x70da cleared.
		/// The locals of HandleCity and BX, CX, SI, DI are used as in the original.
		/// </summary>
		private void MarkUnavailableTiles(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			HumanTileNear = 0;
			ushort tile = 0;

			for (; (short)tile < (short)CityTiles; tile++)
			{
				this.round.Index = tile;
				this.processor.RegSource.Value16 = (ushort)(tile << 1);
				ushort unavailable = (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 0);

				short horizLoc = (short)(town.Place.X + this.owner.TownDeltas[tile].X);
				short vertLoc = (short)(town.Place.Y + this.owner.TownDeltas[tile].Y);
				this.round.TileHoriz = (ushort)horizLoc;
				this.round.TileVert = (ushort)vertLoc;

				short owner = (short)CityOwner;

				// A tile the owner has not seen (the barbarians see all)
				if (owner != 0 && (this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
					this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 1);

				this.round.Value = this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				ushort shared = (ushort)(short)(10 * this.owner.TownDeltas[tile].X + this.owner.TownDeltas[tile].Y * 2);

				if (this.round.Value != 0xffff && this.round.Value != CityOwner)
				{
					// A tile of another player
					this.processor.RegDest.Value16 = shared;
					this.processor.RegAcc.Value16 = 1;
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + shared - 0x8e), 1);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 1);
					this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;

					if (this.round.Value == (ushort)this.owner.World.PersonParticipantIdx)
						HumanTileNear = 1;
				}

				// A tile worked by a neighbouring city
				this.processor.RegDest.Value16 = shared;

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + shared - 0x8e)) != 0)
					this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 1);

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, unavailable) == 0)
					continue;

				// Its worker is taken off
				uint mask = ~(1u << (tile & 0xff));
				this.processor.RegCount.Value16 = (ushort)mask;
				this.processor.RegBase.Value16 = (ushort)(mask >> 16);
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				town.LaborerMarkers0 &= this.processor.RegCount.Value16;
				town.LaborerMarkers1 &= this.processor.RegBase.Value16;
			}

			this.round.Index = tile;

			for (ushort slot = 0; slot < 4; slot++)
			{
				this.round.Index = slot;
				this.processor.RegBase.Value16 = (ushort)(slot << 1);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da), 0);
			}

			this.round.Index = 4;
		}

		/// <summary>
		/// The workers the city keeps: a computer city every fourth turn (without the flag 1 and with no tile of the human
		/// around, 0x70e8) drops them all and picks anew (true). Else 0x50 = size + 1 less the workers: not too many, they are
		/// kept (drawn, marked in the array at BP-0x3e and in 0xee / 0xec); too many, all go (the human is told in the turn).
		/// True when workers are still to be picked (0x50 neither the specialists 0xe8b8 nor 0). The locals of HandleCity and
		/// AX, BX, CX, DX, SI are used as in the original.
		/// </summary>
		private bool KeepWorkers(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.processor.RegAcc.Value16 = 0;
			this.round.WorkersHigh = 0;
			this.round.WorkersLow = 0;

			if (!this.owner.World.IsHuman(owner) && ((townIdx + this.owner.World.RoundTally) & 3) == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.ConditionMarker & 1) == 0 && HumanTileNear == 0)
					return true;
			}

			// 0x50: the size + 1 less the workers
			ushort tile = 0;

			for (; (short)tile < (short)CityTiles; tile++)
			{
				this.round.Index = tile;

				if (TestWorker(townIdx, tile))
					this.round.Workers = (ushort)(this.round.Workers - 1);
			}

			this.round.Index = tile;

			if ((short)this.round.Workers >= 0)
			{
				// Not too many: the workers are kept
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.round.Workers = (ushort)(town.TrueExtent + 1);

				for (tile = 0; (short)tile < (short)CityTiles; tile++)
				{
					this.round.Index = tile;

					if (this.round.Workers == 0 || !TestWorker(townIdx, tile))
						continue;

					this.processor.RegSource.Value16 = (ushort)(tile << 1);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 1);

					RenderCityTile(townIdx, tile, marker);

					this.round.Workers = (ushort)(this.round.Workers - 1);
					uint mask = 1u << (tile & 0xff);
					this.processor.RegAcc.Value16 = (ushort)mask;
					this.processor.RegData.Value16 = (ushort)(mask >> 16);
					this.round.WorkersLow = (ushort)(this.round.WorkersLow | this.processor.RegAcc.Value16);
					this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | this.processor.RegData.Value16);
				}

				this.round.Index = tile;
			}
			else
			{
				// Too many: all go
				if (marker == 0 && owner == this.owner.World.PersonParticipantIdx)
				{
					Message("Population decrease\nin ")
						.City(townIdx)
						.Text(".\n")
						.Style(4)
						.Dialog(100, 80);
					this.processor.EmitGlobalWord(0xb1e8, 1);
				}

				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.round.Workers = (ushort)(town.TrueExtent + 1);
			}

			return this.round.Workers != CitySpecialists && this.round.Workers != 0;
		}

		/// <summary>Whether the tile is worked by the city (CX, BX the masked words of the workers, SI the city, as in the original)</summary>
		private bool TestWorker(short townIdx, ushort tile)
		{
			City town = this.owner.World.Towns[townIdx];
			uint mask = 1u << (tile & 0xff);

			this.processor.RegCount.Value16 = (ushort)(mask & town.LaborerMarkers0);
			this.processor.RegBase.Value16 = (ushort)(((mask >> 16) & town.LaborerMarkers1) | this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			return this.processor.RegBase.Value16 != 0;
		}

		/// <summary>
		/// The new workers of the city (0x50 the workers still to place): the city tile first (once, 0x16); while the food
		/// (0x70da + half the workers left) does not feed the citizens (2 per size) and the settlers (0x48 each), or the city
		/// is below size 3, the tile with the most food (then the most shields x 2 + trade; none without shields for the last
		/// worker of a city without shields, 0x70dc) is taken while more than the specialists 0xe8b8 are to place and the
		/// last search found one (0x108). Then (a computer city with the flag 1 keeps one more specialist) the tiles with the
		/// best weighted yield: food x 16 / the food left over, shields x (3 x size) / the shields left over (0x70dc less
		/// 0xd2f6), trade x (2 x size) / the trade (0x70de), each divisor kept within 1 - 99. The workers 0xee / 0xec and the
		/// specialists (0x50 in bits 26 - 31) are stored, 0xae = -0x50. The locals of HandleCity and AX, BX, CX, DX, SI are
		/// used as in the original.
		/// </summary>
		private void PickWorkers(short townIdx, short marker, bool pick)
		{
			City town = this.owner.World.Towns[townIdx];

			if (pick)
			{
				if ((short)this.round.Workers > 0 && this.round.CityTileTaken == 0)
				{
					// The city tile
					this.round.CityTileTaken = 1;
					RenderCityTile(townIdx, 20, marker);
					this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | 0x10);
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}

				// Food first
				while (true)
				{
					this.processor.RegCount.Value16 = (ushort)((short)this.round.Workers / 2 + CityFood);
					this.processor.RegAcc.Value16 = (ushort)((short)this.round.SettlerFood * (short)this.processor.PullGlobalWord(0xe3c6));
					this.processor.RegBase.Value16 = (ushort)(town.TrueExtent * 2 + this.processor.RegAcc.Value16);

					bool hungry = (short)this.processor.RegBase.Value16 > (short)this.processor.RegCount.Value16 || town.TrueExtent < 3;

					if (!hungry || (short)this.round.Workers <= (short)CitySpecialists ||
						this.round.Found == 0xffff)
					{
						break;
					}

					this.round.Found = 0xffff;
					this.processor.RegAcc.Value16 = 0;
					this.round.BestValue = 0;
					this.round.BestFood = 0;
					ushort tile = 0;

					for (; (short)tile < (short)CityTiles; tile++)
					{
						this.round.Index = tile;

						if (!LocateFreeTile(townIdx, tile))
							continue;

						short horizLoc = (short)this.round.TileHoriz;
						short vertLoc = (short)this.round.TileVert;

						this.round.Amount = MayCityWorkTile(horizLoc, vertLoc, 0);
						this.round.TileValue = (ushort)(MayCityWorkTile(horizLoc, vertLoc, 1) << 1);
						this.processor.RegAcc.Value16 = this.round.TileValue;

						if (this.round.Workers == 1 && CityShields == 0 && this.round.TileValue == 0)
							continue;

						this.round.TileValue = (ushort)(this.round.TileValue + MayCityWorkTile(horizLoc, vertLoc, 2));

						short food = (short)this.round.Amount;
						short best = (short)this.round.BestFood;
						this.processor.RegAcc.Value16 = (ushort)best;

						if (food > best || (food == best && (short)this.round.TileValue > (short)this.round.BestValue))
						{
							this.round.BestFood = (ushort)food;
							this.processor.RegAcc.Value16 = this.round.TileValue;
							this.round.BestValue = this.processor.RegAcc.Value16;
							this.processor.RegAcc.Value16 = this.round.Index;
							this.round.Found = this.processor.RegAcc.Value16;
						}
					}

					this.round.Index = tile;

					if (this.round.Found == 0xffff)
						break;

					TakeWorker(townIdx, marker);
				}

				// A computer city with the flag 1 keeps one more specialist
				if (CityOwner != (ushort)this.owner.World.PersonParticipantIdx)
				{
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

					if ((town.ConditionMarker & 1) != 0)
						CitySpecialists = (ushort)(CitySpecialists + 1);
				}

				// The best weighted yield
				while ((short)this.round.Workers > (short)CitySpecialists)
				{
					this.round.Found = 0xffff;
					this.processor.RegAcc.Value16 = 0;
					this.round.BestWeighted = 0;
					this.round.BestValue = 0;
					this.round.BestFood = 0;
					ushort tile = 0;

					for (; (short)tile < (short)CityTiles; tile++)
					{
						this.round.Index = tile;

						if (!LocateFreeTile(townIdx, tile))
							continue;

						short horizLoc = (short)this.round.TileHoriz;
						short vertLoc = (short)this.round.TileVert;

						// Food
						short divisor = (short)this.owner.Helpers.VerifyValueSpan(
							(short)CityFood - (town.TrueExtent * 2) - ((short)this.round.SettlerFood * this.processor.PullGlobalSignedWord(0xe3c6)), 1, 99);
						short weight = (short)(16 / divisor);
						this.processor.RegCount.Value16 = (ushort)weight;
						this.processor.RegSource.Value16 = (ushort)weight;

						short yield = (short)MayCityWorkTile(horizLoc, vertLoc, 0);
						this.round.TileFood = (ushort)yield;
						this.round.TileValue = (ushort)(yield * weight);

						// Shields
						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 1);
						this.processor.RegSource.Value16 = (ushort)yield;
						this.round.TileShields = (ushort)yield;

						divisor = (short)this.owner.Helpers.VerifyValueSpan((short)CityShields - this.processor.PullGlobalSignedWord(0xd2f6), 1, 99);
						this.processor.RegCount.Value16 = (ushort)divisor;
						this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
						this.round.TileValue = (ushort)(this.round.TileValue + (short)(3 * town.TrueExtent) / divisor * yield);

						// Trade
						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 2);
						this.processor.RegSource.Value16 = (ushort)yield;
						this.round.TileTrade = (ushort)yield;

						divisor = (short)this.owner.Helpers.VerifyValueSpan((short)CityTrade, 1, 99);
						this.processor.RegCount.Value16 = (ushort)divisor;
						this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
						int product = (short)(town.TrueExtent * 2) / divisor * yield;
						this.processor.RegAcc.Value16 = (ushort)product;
						this.processor.RegData.Value16 = (ushort)(product >> 16);
						this.round.TileValue = (ushort)(this.round.TileValue + this.processor.RegAcc.Value16);
						this.processor.RegAcc.Value16 = this.round.BestValue;

						if ((short)this.round.TileValue > (short)this.round.BestValue)
						{
							this.round.BestValue = this.round.TileValue;
							this.processor.RegAcc.Value16 = this.round.Index;
							this.round.Found = this.processor.RegAcc.Value16;
							this.processor.RegAcc.Value16 = (ushort)(((this.round.TileFood + this.round.TileShields) << 1) + this.round.TileTrade);
							this.round.BestWeighted = this.processor.RegAcc.Value16;
						}
					}

					this.round.Index = tile;

					if (this.round.Found == 0xffff)
						break;

					TakeWorker(townIdx, marker);
				}
			}

			// The workers and the specialists (bits 26 - 31) stored
			uint workers = (uint)((int)(short)this.round.Workers << 0x1a) +
				(((uint)this.round.WorkersHigh << 16) | this.round.WorkersLow);
			this.processor.RegCount.Value16 = (ushort)workers;
			this.processor.RegBase.Value16 = (ushort)(workers >> 16);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			town.LaborerMarkers0 = this.processor.RegCount.Value16;
			town.LaborerMarkers1 = this.processor.RegBase.Value16;
			this.processor.RegAcc.Value16 = (ushort)-(short)this.round.Workers;
			this.round.WorkersGained = this.processor.RegAcc.Value16;
		}

		/// <summary>A tile around the city still free for a worker (the array at BP-0x3e); 0xc6 / 0xd2 its place</summary>
		private bool LocateFreeTile(short townIdx, ushort tile)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegSource.Value16 = (ushort)(tile << 1);

			if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e)) != 0)
				return false;

			this.round.TileHoriz = (ushort)(short)(town.Place.X + this.owner.TownDeltas[tile].X);
			this.round.TileVert = (ushort)(short)(town.Place.Y + this.owner.TownDeltas[tile].Y);
			return true;
		}

		/// <summary>The tile 0x108 gets a worker: marked, drawn, in 0xee / 0xec; 0x50 one less</summary>
		private void TakeWorker(short townIdx, short marker)
		{
			ushort tile = this.round.Found;

			this.processor.RegSource.Value16 = (ushort)(tile << 1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 1);

			RenderCityTile(townIdx, tile, marker);

			uint mask = 1u << (tile & 0xff);
			this.processor.RegAcc.Value16 = (ushort)mask;
			this.processor.RegData.Value16 = (ushort)(mask >> 16);
			this.round.WorkersLow = (ushort)(this.round.WorkersLow | this.processor.RegAcc.Value16);
			this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | this.processor.RegData.Value16);
			this.round.Workers = (ushort)(this.round.Workers - 1);
		}

		/// <summary>
		/// After the workers: 0x8 the specialists; the first 0x8 citizen icons without a type become type 3, the rest of the
		/// 8 type 0. The factories: 0xf8 the factory bonus (2 the factory, 4 the manufacturing plant), 0xac the power (2 the
		/// power plant, hydro plant, nuclear plant or the wonder 15 on the same continent), 0x6c98 the pollution factor (1;
		/// 2 for the clean plants, 3 the recycling center); the shields 0x70dc (kept in 0x10a, the trade in 0xa) grow by a
		/// quarter of the factory bonus and of the power within it. The locals of HandleCity and AX - DX, SI, DI are used
		/// as in the original.
		/// </summary>
		private void PrepareCitizensAndFactories(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			this.processor.RegAcc.Value16 = this.round.Workers;
			this.round.Specialists = this.processor.RegAcc.Value16;

			// The citizen icons
			ushort citizen = 0;

			for (; (short)citizen < (short)this.round.Specialists; citizen++)
			{
				this.round.Index = citizen;

				if (ObtainCitizenType(citizen) == 0)
					AssignCitizenType(citizen, 3);
			}

			for (citizen = this.round.Specialists; (short)citizen < 8; citizen++)
			{
				this.round.Index = citizen;
				AssignCitizenType(citizen, 0);
			}

			this.round.Index = citizen;

			// The factories and the power
			this.processor.RegAcc.Value16 = 0;
			this.round.FactoryPower = 0;
			this.round.FactoryBonus = 0;
			this.processor.EmitGlobalWord(0x6c98, 1);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.Has(CityStructure.Factory))
				this.round.FactoryBonus = 2;

			if (town.Has(CityStructure.ManufacturingPlant))
				this.round.FactoryBonus = 4;

			if (town.Has(CityStructure.PowerPlant))
				this.round.FactoryPower = 2;

			if (town.Has(CityStructure.HydroPlant))
			{
				this.processor.EmitGlobalWord(0x6c98, 2);
				this.round.FactoryPower = 2;
			}

			if (town.Has(CityStructure.NuclearPlant))
			{
				this.processor.EmitGlobalWord(0x6c98, 2);
				this.round.FactoryPower = 2;
			}

			if (PlayerEnjoysWonderEffect((short)CityOwner, 15) != 0)
			{
				// The wonder powers the cities on its continent
				short marvelIdx = this.owner.World.MarvelTownIdx[15];
				this.processor.RegSource.Value16 = (ushort)(marvelIdx * 0x1c);
				this.processor.RegDest.Value16 = (ushort)(townIdx * 0x1c);

				this.round.Continent = this.owner.MapOps.ObtainContinentID(this.owner.World.Towns[marvelIdx].Place.X, this.owner.World.Towns[marvelIdx].Place.Y);

				if (this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) == this.round.Continent)
				{
					this.processor.EmitGlobalWord(0x6c98, 2);
					this.round.FactoryPower = 2;
				}
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.Has(CityStructure.RecyclingCenter))
				this.processor.EmitGlobalWord(0x6c98, 3);

			this.round.FactoryPower = (ushort)this.owner.Helpers.VerifyValueSpan((short)this.round.FactoryPower, 0, (short)this.round.FactoryBonus);
			this.round.ShieldsKept = CityShields;
			this.round.TradeKept = CityTrade;

			// The shields grow by a quarter of the power and of the factory bonus
			short shields = (short)CityShields;
			short power = (short)((short)((short)this.round.FactoryPower * shields) / 4);
			short factory = (short)((short)this.round.FactoryBonus * shields);
			this.processor.RegBase.Value16 = (ushort)power;
			this.processor.RegCount.Value16 = 2;
			this.processor.RegData.Value16 = (ushort)(factory < 0 ? 0xffff : 0);
			this.processor.RegAcc.Value16 = (ushort)(factory / 4 + power);
			CityShields = (ushort)(CityShields + this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);
		}

		/// <summary>Where HandleCity goes on after the food and the shields of the turn (ProduceInCity)</summary>
		private enum ProductionStep
		{
			/// <summary>A unit was built (L174b)</summary>
			UnitBuilt,

			/// <summary>Not enough shields for the unit (L1b60)</summary>
			NotEnough,

			/// <summary>The city builds an improvement or a wonder (L1b63)</summary>
			Improvement,

			/// <summary>Settlers of a city of size 1 on the easiest level wait (L2307)</summary>
			SettlersWait,

			/// <summary>The city is gone (L6927)</summary>
			CityLost,
		}

		/// <summary>
		/// The turn of the city, first part: the food stored grows by the surplus (the citizens eat 2, the settlers 0x48),
		/// the shields by 0x70dc less the support of the units (0xd2f6; nothing with the flag 1). A unit (price x 0x4a) is built
		/// when the shields suffice: the replay notes the first one of its kind, the barracks make it veteran; settlers
		/// make the city smaller (a city of size 1 is lost, unless it is the last one of its owner). A computer player builds
		/// no diplomats (the shields go). The locals of HandleCity (0xba the new unit) and BX, SI are used as in the original.
		/// </summary>
		private ProductionStep ProduceInCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			// The food and the shields of the turn
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegCount.Value16 = (ushort)(town.TrueExtent * 2);
			this.processor.RegAcc.Value16 = (ushort)((short)this.round.SettlerFood * (short)this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegData.Value16 = (ushort)(CityFood - this.processor.RegAcc.Value16 - this.processor.RegCount.Value16);
			town.GrainTally += (short)this.processor.RegData.Value16;

			this.round.Value = (ushort)(CityShields - this.processor.PullGlobalWord(0xd2f6));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.ConditionMarker & 1) != 0)
				this.round.Value = 0;

			this.processor.RegAcc.Value16 = this.round.Value;
			town.MaterialsTally += (short)this.processor.RegAcc.Value16;

			if (town.PresentMakingIdx < 0)
				return ProductionStep.Improvement;

			// A unit
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			short cost = (short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + UnitKindTable.Origin + UnitKindTable.Price)) *
				(short)this.round.CostFactor);

			if (cost > town.MaterialsTally)
				return ProductionStep.NotEnough;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx == 0 && town.TrueExtent == 1 && this.owner.World.HardnessTier == 0)
				return ProductionStep.SettlersWait;

			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			town.MaterialsTally -= cost;
			this.round.NewUnit = 0xffff;

			if (owner == this.owner.World.PersonParticipantIdx || town.PresentMakingIdx != 0x1a)
			{
				// (a computer player builds no diplomats)
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				this.round.NewUnit = this.owner.UnitOps.ConstructUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y);
			}

			// The first unit of its kind goes into the replay
			if ((this.owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] & 8) == 0)
			{
				this.owner.UnitOps.AttachReplayContent(6, (byte)CityOwner, (byte)town.PresentMakingIdx);
				this.owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] |= 8;
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.Has(CityStructure.Barracks) && this.round.NewUnit != 0xffff)
			{
				// The barracks make it veteran
				short troopIdx = (short)this.round.NewUnit;
				this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.troops.Assign(owner, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(owner, troopIdx, UnitMemory.Status) | 0x20));
			}

			if (this.round.NewUnit == 0xffff)
				return ProductionStep.UnitBuilt;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx != 0)
				return ProductionStep.UnitBuilt;

			// Settlers make the city smaller (but the last city of size 1)
			if (town.TrueExtent <= 1 && this.owner.World.Participants[owner].TownTally <= 1)
				return ProductionStep.UnitBuilt;

			sbyte size = town.TrueExtent;
			town.TrueExtent--;

			// (the translated code tests the flags of the comparison of the size with 1: the same as the size becoming 0)
			if (size != 1)
				return ProductionStep.UnitBuilt;

			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.round.NewUnit = this.owner.UnitOps.ConstructUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y);
			this.owner.NewGameWizard.EliminateCivilization((short)CityOwner, 0);
			this.owner.Startup.DisplayMouseCursor();

			return ProductionStep.CityLost;
		}

		/// <summary>
		/// After the unit of the turn (0xba, -1 when none): a human city keeps no shields and tells of new settlers,
		/// diplomats and caravans. A computer city sends a new caravan to the foreign city with the most trade (not of the human)
		/// and a new diplomat (never built for a computer player: unreachable, kept as translated) to a city of the human;
		/// the last nuclear unit of a computer player resets the human's timer of contact; then it picks a new production.
		/// The locals of HandleCity and BX, CX, SI, DI are used as in the original.
		/// </summary>
		private void FinishUnitOfCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;

			if (owner == person || this.round.NewUnit == 0xffff)
			{
				// A human city (or no unit): no shields kept, the human is told of settlers, diplomats and caravans
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				town.MaterialsTally = 0;

				if (this.round.NewUnit == 0xffff || (town.PresentMakingIdx != 0 && town.PresentMakingIdx < 0x1a))
					return;

				this.processor.EmitGlobalWord(0xb1e8, 1);
				EmptyMessage()
					.City(townIdx)
					.Text(" builds ");
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.RegAcc.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride + UnitKindTable.Origin);
				this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);
				this.owner.Libc.AppendText(0xba06, ".\n");
				DialogStyle = 3;
				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
				return;
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx == 0x1b)
			{
				// A caravan goes to the foreign city with the most trade (not of the human)
				this.processor.RegAcc.Value16 = 0xffff;
				this.round.Found = 0xffff;
				this.round.Best = 0xffff;

				for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					this.round.OtherCity = otherIdx;
					this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
					City other = this.owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == owner || other.ConditionMarker == 0xff || other.ParticipantIdx == person ||
						other.RootCommerce <= (short)this.round.Best)
					{
						continue;
					}

					this.round.Best = (ushort)other.RootCommerce;
					this.processor.RegAcc.Value16 = this.round.OtherCity;
					this.round.Found = this.processor.RegAcc.Value16;
				}

				this.round.OtherCity = 0x80;
				this.owner.Conquest.FoundTradeRoute(owner, (short)this.round.NewUnit, (short)this.round.Found);
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx == 0x1a)
				SendDiplomat(townIdx);

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx == 0x19)
			{
				// The last nuclear unit of a computer player
				this.processor.RegBase.Value16 = (ushort)(owner * 0x38);

				if (this.owner.World.Participants[owner].LiveTroops[25] == 1)
					this.owner.World.Participants[person].TouchParticipantTimeleft = -1;
			}

			this.owner.Bot.PickNewCityProduction(townIdx);
		}

		/// <summary>
		/// A new diplomat of a computer player (unreachable: a computer player builds none) goes, from the nearest of its
		/// units, to a city of the human on the same continent (the nearest; equal ones by chance, 0x106); with none within
		/// 3 or at peace with the human the shields come back
		/// </summary>
		private void SendDiplomat(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.round.Found = 0xffff;
			this.round.Best = 0x7fff;
			this.round.Selected = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
				City other = this.owner.World.Towns[otherIdx];

				if (other.ParticipantIdx != this.owner.World.PersonParticipantIdx || other.ConditionMarker == 0xff)
					continue;

				this.processor.RegSource.Value16 = (ushort)(otherIdx * 0x1c);
				this.round.Value = this.owner.Helpers.LocateNearestUnit(owner, (short)this.round.NewUnit, other.Place.X, other.Place.Y);

				this.processor.RegAcc.Value16 = (ushort)(FoundDistance - 3);
				FoundDistance = (ushort)this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);
				this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);

				if ((other.ConditionMarker & 0x20) != 0)
					FoundDistance = 3;

				short best = (short)this.round.Best;
				short distance = (short)FoundDistance;

				if (best < distance)
					continue;

				if (best == distance)
				{
					this.round.Selected = (ushort)(this.round.Selected + 1);

					if (this.owner.Libc.Dice.DrawInt(this.round.Selected) != 0)
						continue;
				}

				// The unit must stand on land of the continent of the city
				short troopIdx = (short)this.round.Value;
				this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + owner * UnitMemory.ParticipantStride);

				if (this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(owner, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain)
					continue;

				this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + owner * UnitMemory.ParticipantStride);
				this.processor.RegDest.Value16 = (ushort)(otherIdx * 0x1c);

				this.round.Continent = this.owner.MapOps.ObtainContinentID(this.troops.PullSigned(owner, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, troopIdx, UnitMemory.Vert));
				if (this.owner.MapOps.ObtainContinentID(other.Place.X, other.Place.Y) != this.round.Continent)
					continue;

				if (this.round.Best == FoundDistance)
					this.round.Selected = 1;
				else
					this.round.Best = FoundDistance;

				this.round.Found = this.round.Value;
				this.processor.RegAcc.Value16 = this.round.OtherCity;
				this.round.Target = this.processor.RegAcc.Value16;
			}

			this.round.OtherCity = 0x80;

			if ((short)this.round.Best <= 3)
			{
				this.processor.RegSource.Value16 = (ushort)(owner << 4);
				this.processor.RegCount.LowByte = 4;
				this.processor.RegBase.Value16 = (ushort)(this.owner.World.PersonParticipantIdx << 1);

				if ((this.owner.World.Participants[owner].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				{
					// The diplomat goes from the unit 0x108 to the city 0xb0
					short fromIdx = (short)this.round.Found;
					this.processor.RegSource.Value16 = (ushort)(fromIdx * UnitMemory.TroopStride + owner * UnitMemory.ParticipantStride);
					this.round.NewUnit = this.owner.UnitOps.ConstructUnit(owner, 0x1a,
						this.troops.PullSigned(owner, fromIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, fromIdx, UnitMemory.Vert));

					if (this.round.NewUnit != 0xffff)
					{
						short troopIdx = (short)this.round.NewUnit;
						City target = this.owner.World.Towns[this.round.Target];
						this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
						this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
						this.troops.Assign(owner, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
						this.troops.Assign(owner, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);
					}

					return;
				}
			}

			// The shields come back
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			town.MaterialsTally += (short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + UnitKindTable.Origin + UnitKindTable.Price)) *
				(short)this.round.CostFactor);
		}

		/// <summary>
		/// An improvement or wonder (0xe8 = -the production) completed when the shields reach its price x 0x4a: a wonder built
		/// elsewhere or an improvement the city has already makes 0xe8 -1 (false: nothing happens). Else the nuclear test is
		/// reported (0x29), an improvement (below 0x16) is added, the shields paid; a human city and every wonder are announced
		/// (a wonder of a computer player shows its city to all; the human may see the city screen, with the option 8 and the
		/// disk) and the shields cleared, a computer city picks its next production (after the palace -2 first) and counts a
		/// unit in production. The locals of HandleCity and BX, CX, SI are used as in the original.
		/// </summary>
		private bool CompleteImprovement(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.RegBase.Value16 = (ushort)(-town.PresentMakingIdx * 0x1e);

			if ((short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2)) *
				(short)this.round.CostFactor) > town.MaterialsTally)
			{
				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.round.Value = (ushort)-town.PresentMakingIdx;
			short built = (short)this.round.Value;

			if (built > 0x18)
			{
				// A wonder: only the first city builds it
				this.processor.RegBase.Value16 = (ushort)(built << 1);

				if (this.owner.World.MarvelTownIdx[built - 24] != -1)
				{
					this.round.Value = 0xffff;
				}
				else
				{
					this.owner.World.MarvelTownIdx[built - 24] = townIdx;
					this.owner.UnitOps.AttachReplayContent(10, (byte)CityOwner, (byte)(built - 0x18));
				}
			}
			else if (TestImprovement(townIdx, built))
			{
				// The city has it already
				this.round.Value = 0xffff;
			}

			if (this.round.Value == 0xffff)
				return false;

			if (built == 0x29)
			{
				Message("Sensors report a\nNUCLEAR WEAPONS test\nnear ")
					.City(townIdx)
					.Text("!\n");
				this.owner.Services.PerformSound(0x24, 0);
				this.owner.Paper.DisplayNewspaper(-1);
				this.owner.Services.PerformSound(1, 0);
			}

			if (built < 0x16)
			{
				// The improvement
				uint mask = 1u << ((byte)(built - 1) & 0xff);
				this.processor.RegCount.Value16 = (ushort)mask;
				this.processor.RegBase.Value16 = (ushort)(mask >> 16);
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				town.StructureMarkers0 |= this.processor.RegCount.Value16;
				town.StructureMarkers1 |= this.processor.RegBase.Value16;
			}

			// The price paid
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.RegBase.Value16 = (ushort)(-town.PresentMakingIdx * 0x1e);
			town.MaterialsTally -= (short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2)) *
				(short)this.round.CostFactor);
			this.processor.RegCount.LowByte = (byte)CityOwner;

			bool humanPlayed = this.owner.World.IsHuman(owner);

			if (!humanPlayed && built <= 0x18)
			{
				// A computer city picks its next production (after the palace -2 first)
				if (built == 1)
				{
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
					town.PresentMakingIdx = -2;
				}

				this.processor.RegCount.Value16 = this.owner.Advisors.PickCityProduction(owner, townIdx);
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				town.PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

				StartMakingUnit(town, owner, true);

				return true;
			}

			// The announcement (a wonder of a computer player shows its city to all)
			if (humanPlayed)
			{
				this.processor.EmitGlobalWord(0xb1e8, 1);
			}
			else
			{
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				this.owner.World.ChartSight[town.Place.X, town.Place.Y] = -1;
			}

			EmptyMessage()
				.City(townIdx)
				.Text(" builds\n");
			this.processor.RegAcc.Value16 = (ushort)(built * 0x1e + 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);
			this.owner.Libc.AppendText(0xba06, ".\n");
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;

			bool screen = false;

			if (owner == this.owner.World.PersonParticipantIdx && (this.owner.World.SessionPrefMarkers & 8) != 0 && (built <= 0x15 || built > 0x18))
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.ConditionMarker & 0x10) == 0 && built != 1)
				{
					screen = this.owner.Startup.QueryForDisk(1, 0) != 0;
				}
			}

			if (screen)
				this.owner.CityScene.DisplayCityScreen(townIdx, (short)this.round.Value);
			else
				this.owner.Paper.DisplayNewspaper(townIdx);

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			town.MaterialsTally = 0;
			return true;
		}

		/// <summary>Whether the city has the improvement (1 - 0x18: its bit; CX, BX the masked words, SI the city, as in the original)</summary>
		private bool TestImprovement(short townIdx, short improvement)
		{
			City town = this.owner.World.Towns[townIdx];
			uint mask = 1u << ((byte)(improvement - 1) & 0xff);

			this.processor.RegCount.Value16 = (ushort)(mask & town.StructureMarkers0);
			this.processor.RegBase.Value16 = (ushort)(((mask >> 16) & town.StructureMarkers1) | this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			return this.processor.RegBase.Value16 != 0;
		}

		/// <summary>
		/// A spaceship part (0xe8 0x16 - 0x18) of a player whose spaceship has not left: the human adds it on the spaceship
		/// screen; a computer player (0xc8 = the success it computes) gets the shields back for the structures and modules it
		/// can use and keeps building them, then launches when the success says so, or (with 40 percent of the computer
		/// victories, 0x28 known by the human) when it is sure, the human is ahead, or arrives later, or a third player is rich
		/// (the gold of the player 0x... LiveRealms, as translated). The locals of HandleCity and AX, BX, CX, SI are used as in
		/// the original.
		/// </summary>
		private void BuildSpaceshipPart(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;
			short part = (short)this.round.Value;

			if (part < 0x16 || part > 0x18 || (this.owner.World.StarshipMarkers & (1 << (owner & 0xff))) != 0)
				return;

			if (owner == person)
			{
				this.owner.RocketScreen.AttachSpaceshipPart(owner, (ushort)(part - 22));
				return;
			}

			this.processor.RegAcc.Value16 = (ushort)(part - 0x16);
			this.round.Best = this.owner.RocketScreen.ComputeSpaceshipSuccess(owner, (short)this.processor.RegAcc.Value16);

			if (this.round.Best != 0 && part < 0x18)
			{
				// The shields come back, the city builds the part again
				this.processor.RegBase.Value16 = (ushort)(part * 0x1e);
				this.processor.RegCount.Value16 = (ushort)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2)) *
					(short)this.round.CostFactor);
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				town.MaterialsTally += (short)this.processor.RegCount.Value16;

				if (town.PresentMakingIdx <= -0x16 && town.PresentMakingIdx >= -0x18)
				{
					this.processor.RegAcc.Value16 = (ushort)-part;
					this.processor.RegAcc.LowByte = (byte)(this.processor.RegAcc.LowByte - 1);
					town.PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;
				}

				this.round.Best = (ushort)(this.round.Best - 1);
			}

			bool launch;
			short success = (short)this.round.Best;

			if ((this.owner.World.StarshipMarkers & (1 << (owner & 0xff))) != 0 || this.owner.World.BotStarshipVictoryRatio < 40 || success != 0)
			{
				launch = success != 0;
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(person, 0x28) == 0)
			{
				launch = false;
			}
			else if (this.owner.World.BotStarshipVictoryRatio > 75)
			{
				launch = true;
			}
			else
			{
				this.processor.RegSource.Value16 = (ushort)(owner << 1);
				Player ahead = this.owner.World.Participants[person];
				Player computer = this.owner.World.Participants[owner];

				if (ahead.Standing > computer.Standing)
				{
					launch = true;
				}
				else
				{
					launch = false;
					bool decided = false;

					if ((this.owner.World.StarshipMarkers & (1 << (person & 0xff))) != 0)
					{
						// Both spaceships fly: the computer launches when it arrives first
						this.processor.RegAcc.Value16 = (ushort)(computer.StarshipArrivalSeason - this.owner.World.Season);
						this.processor.RegCount.Value16 = (ushort)(ahead.StarshipArrivalSeason - this.owner.World.Season);

						if ((short)this.processor.RegAcc.Value16 <= (short)this.processor.RegCount.Value16)
						{
							launch = true;
							decided = true;
						}
					}

					if (!decided)
					{
						this.processor.RegAcc.Value16 = 1;
						this.processor.RegCount.LowByte = (byte)(((person & 0xff) + 8) & 0xff);

						launch = ((ushort)this.owner.World.StarshipMarkers & (ushort)(1 << this.processor.RegCount.LowByte)) != 0 &&
							this.owner.World.Participants[this.owner.World.LiveRealms].Gold > 1000;
					}
				}
			}

			if (launch)
				this.owner.RocketScreen.FireSpaceship(owner);
		}

		/// <summary>
		/// What some completed improvements do (0xe8): the palace (1) leaves every city of the owner; the human hears of the
		/// move (of a computer player only with an embassy, 0x40): then the capital is this city (HorizBegin, the palace bit
		/// set again; without the message it stays off, as translated); the wonder 0x26 gives two advances; the wonder 0x2b of
		/// the human shows every foreign city (its size, the tile seen). The locals of HandleCity and BX, CX, SI are used as in
		/// the original.
		/// </summary>
		private void ApplyImprovement(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;
			short built = (short)this.round.Value;

			if (built == 1)
			{
				// The palace leaves every city of the owner
				for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					this.round.OtherCity = otherIdx;
					this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
					City other = this.owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == owner)
					{
						other.StructureMarkers0 &= 0xfffe;
						other.StructureMarkers1 &= 0xffff;
					}
				}

				this.round.OtherCity = 0x80;

				bool told = owner == person;

				if (!told)
				{
					this.processor.RegSource.Value16 = (ushort)(person << 4);
					this.processor.RegCount.LowByte = 4;
					this.processor.RegBase.Value16 = (ushort)(owner << 1);
					told = (this.owner.World.Participants[person].Relations[owner] & 0x40) != 0;
				}

				if (told)
				{
					this.processor.EmitGlobalByte(0xba06, 0);

					if (owner != person)
					{
						Message("Diplomats report:\n")
							.Style(1);
					}

					this.processor.RegBase.Value16 = (ushort)(owner << 1);
					this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));
					this.owner.Libc.AppendText(0xba06, " capital\nmoved to ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, ".\n");
					DialogStyle = 5;
					this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

					this.owner.World.Participants[owner].HorizBegin = (short)town.Place.X;
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
					town.StructureMarkers0 |= 1;
					town.StructureMarkers1 |= 0;
				}
			}

			if (built == 0x26)
			{
				// Two advances
				this.owner.Startup.DisplayMouseCursorFully();
				this.owner.Advisors.UncoverTechnology(owner, 0);
				this.owner.Advisors.UncoverTechnology(owner, 0);
				this.owner.Startup.ConcealMouseCursorToSavedLevel();
			}

			if (built != 0x2b || owner != person)
				return;

			// Every foreign city shown to the human
			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
				City other = this.owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || other.ParticipantIdx == owner)
					continue;

				this.processor.RegSource.Value16 = (ushort)(otherIdx * 0x1c);
				other.ShownExtent = other.TrueExtent;

				this.processor.RegCount.Value16 = (ushort)(byte)(1 << (owner & 0x1f));
				this.owner.World.ChartSight[other.Place.X, other.Place.Y] |= (sbyte)this.processor.RegCount.LowByte;
				this.owner.MapOps.KeepKnownTileImprovements(other.Place.X, other.Place.Y);
			}

			this.round.OtherCity = 0x80;
		}

		/// <summary>
		/// A human city with the flag 0x10 (production chosen by the advisor) picks its production again when it has no shields,
		/// or builds an improvement and nothing was completed (0xe8 -1): 0x63 (none chosen) clears the flag, else the count of
		/// units in production follows the change. The locals of HandleCity and BX, SI are used as in the original.
		/// </summary>
		private void AdviseHumanProduction(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			if (owner != this.owner.World.PersonParticipantIdx)
				return;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.ConditionMarker & 0x10) == 0)
				return;

			if (town.MaterialsTally != 0 && (town.PresentMakingIdx >= 0 || this.round.Value != 0xffff))
				return;

			this.round.Value = this.owner.Advisors.PickCityProduction(owner, townIdx);
			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 2);

			if (this.round.Value == 0x63)
			{
				// Nothing chosen: the advisor stops
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				town.ConditionMarker &= 0xef;
				return;
			}

			this.processor.EmitGlobalWord(0xb1e8, 0);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			StopMakingUnit(town, owner, true);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			town.PresentMakingIdx = (sbyte)(byte)this.round.Value;
			StartMakingUnit(town, owner, true);
		}

		/// <summary>
		/// A computer city (one played by a human goes on with the city screen) buys shields with gold (0xcc the purchase): a
		/// city with the flag 0x10 picks a new production first; units of the role (the attitude of the continent 0x104: 1, 2
		/// or 5) for a 64th of the gold, spaceship parts while the human's spaceship flies for a 128th, an improvement of a city
		/// with the flag 1 up to an 8th, a unit where the city tile has no owner (or with the flag 0x10) up to a third, the
		/// production -1 up to a third, nuclear units (the condition 0x19, as translated) up to a quarter; above 2000 gold a
		/// 512th more. The shields grow by the purchase, the gold falls by twice it, the flag 0x10 is cleared. The locals of
		/// HandleCity and AX, BX, SI are used as in the original.
		/// </summary>
		private void BuyForComputerCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.ConditionMarker & 0x10) != 0)
				this.owner.Bot.PickNewCityProduction(townIdx);

			this.round.Amount = 0;

			// The attitude of the continent (as translated: the landmass record of the player numbered by the continent)
			if (this.owner.MapOps.ObtainContinentID((short)this.round.Horiz, (short)this.round.Vert) >= this.owner.World.Participants.Length)
				this.processor.RegAcc.Value16 = 0;
			else
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.RegAcc.Value16].Landmasses[owner].Approach;

			this.round.Approach = this.processor.RegAcc.Value16;
			ushort approach = this.round.Approach;

			if ((approach == 1 || approach == 2 || approach == 5) && this.round.Value != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if (town.PresentMakingIdx >= 0)
				{
					// A unit of the role the continent needs
					this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
					this.processor.RegAcc.Value16 = approach;

					if (this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + UnitKindTable.Origin + UnitKindTable.TroopGroup)) == approach)
						this.round.Amount = (ushort)(player.Gold / 64);
				}
			}

			// A spaceship part while the human's spaceship flies
			this.processor.RegCount.LowByte = (byte)(((this.owner.World.PersonParticipantIdx & 0xff) + 8) & 0xff);

			if (((ushort)this.owner.World.StarshipMarkers & (ushort)(1 << this.processor.RegCount.LowByte)) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if (-town.PresentMakingIdx >= 0x16 && -town.PresentMakingIdx <= 0x18)
					this.round.Amount = (ushort)(player.Gold / 128);
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			// An improvement of a city with the flag 1
			if ((town.ConditionMarker & 1) != 0 && town.PresentMakingIdx < 0 && town.MaterialsTally != 0)
			{
				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
				this.round.Amount = (ushort)this.owner.Helpers.VerifyValueSpan(
					this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((-town.PresentMakingIdx * 0x1e) + 0xbb2)) * (short)this.round.CostFactor -
						town.MaterialsTally,
					0, player.Gold / 8);
			}

			// A unit where the city tile has no owner (or with the flag 0x10)
			bool unowned = (ushort)(this.owner.MapOps.ObtainTileOwner((short)this.round.Horiz, (short)this.round.Vert) + 1) == 0;
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 + 1);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((unowned || (town.ConditionMarker & 0x10) != 0) && town.PresentMakingIdx >= 0 && town.MaterialsTally != 0)
				BuyShortfall(townIdx, player.Gold / 3);

			// The production -1
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx == -1 && town.MaterialsTally != 0)
				BuyShortfall(townIdx, player.Gold / 3);

			// Nuclear units (the condition marker compared with 0x19, as translated)
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.ConditionMarker == 0x19)
			{
				this.processor.RegBase.Value16 = (ushort)(owner * 0x38);

				if (player.LiveTroops[25] == 0)
				{
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

					if (town.MaterialsTally != 0)
						BuyShortfall(townIdx, player.Gold / 4);
				}
			}

			if (player.Gold > 2000)
			{
				this.processor.RegAcc.Value16 = (ushort)(player.Gold / 512);
				this.round.Amount = (ushort)(this.round.Amount + this.processor.RegAcc.Value16);
			}

			// The purchase
			this.processor.RegAcc.Value16 = this.round.Amount;
			town.MaterialsTally += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)(this.round.Amount << 1);
			player.Gold -= (short)this.processor.RegAcc.Value16;
			town.ConditionMarker &= 0xef;
		}

		/// <summary>0xcc: the shields the unit still needs (its price x 0x4a less the shields), within 0 and the limit</summary>
		private void BuyShortfall(short townIdx, int limit)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
			this.round.Amount = (ushort)this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((0x22 * town.PresentMakingIdx) + 0x1142)) * (short)this.round.CostFactor -
					town.MaterialsTally,
				0, limit);
		}

		/// <summary>
		/// The city screen (marker 1): the frames of the resources and the info area with its four tabs, the tab 0x2496 lit,
		/// and the mini map when that tab (2) is open
		/// </summary>
		private void RenderCityScreenFrames()
		{
			RenderFrame(2, 67, 124, 104);
			RenderFrame(95, 106, 227, 197);
			RenderLabelBox(95, 106, 128, 114, 0x25ea, 9);
			RenderLabelBox(129, 106, 160, 114, 0x25ef, 9);
			RenderLabelBox(161, 106, 193, 114, 0x25f5, 9);
			RenderLabelBox(194, 106, 226, 114, 0x25f9, 9);

			this.owner.VideoCard.SubstituteHue(ScreenBox, (33 * (short)CityScreenTab) + 96, 107, 32, 7, 9, 15);

			this.processor.RegAcc.Value16 = CityScreenTab;

			if (this.processor.RegAcc.Value16 == 2)
				RenderMiniMap();
		}

		/// <summary>
		/// The corruption and the trade routes: 0xd2e0 the corruption (trade x the distance to the palace 0x6b30 x 3 / (20 x
		/// government + 80); communism counts the distance as 10; a courthouse or the palace halves it, democracy has none),
		/// RootCommerce the trade less it; each trade route adds an eighth (a foreign city) or a sixteenth (an own city) of the
		/// trade of both cities + 4. The locals of HandleCity and AX, BX, CX are used as in the original.
		/// </summary>
		private void CountCorruptionAndTradeRoutes(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			this.processor.EmitGlobalWord(0xd2e0, 0);

			if (player.RegimeKind == 3)
				this.processor.EmitGlobalWord(0x6b30, 0xa);

			// The corruption
			this.processor.RegCount.Value16 = (ushort)(20 * player.RegimeKind + 0x50);
			this.processor.RegBase.Value16 = 3;
			short corruption = (short)((short)((short)CityTrade * (short)this.processor.PullGlobalWord(0x6b30)) * 3);
			this.processor.EmitGlobalWord(0xd2e0, (ushort)(corruption / (short)this.processor.RegCount.Value16));

			if (town.Has(CityStructure.Courthouse) || town.Has(CityStructure.Palace))
			{
				// A courthouse or the palace
				this.processor.RegCount.Value16 = 2;
				this.processor.EmitGlobalWord(0xd2e0, (ushort)((short)this.processor.PullGlobalWord(0xd2e0) / 2));
			}

			if (player.RegimeKind == 5)
				this.processor.EmitGlobalWord(0xd2e0, 0);

			this.processor.RegAcc.LowByte = (byte)((byte)CityTrade - this.processor.PullGlobalByte(0xd2e0));
			town.RootCommerce = (sbyte)this.processor.RegAcc.LowByte;

			// The trade routes
			for (ushort route = 0; route < 3; route++)
			{
				this.round.Item = route;
				this.round.OtherCity = (byte)town.CommerceTownIndices[route];

				if (this.round.OtherCity == 0xff)
					continue;

				City partner = this.owner.World.Towns[this.round.OtherCity];
				short share = (short)(partner.RootCommerce + CityTrade + 4);
				share = (short)(partner.ParticipantIdx != owner ? share / 8 : share / 16);
				this.processor.RegAcc.Value16 = (ushort)share;
				CityTrade = (ushort)(CityTrade + share);
			}

			this.round.Item = 3;
		}

		/// <summary>
		/// The trade of the city split: 0xd2e0 the corruption again (of the trade with the routes; democracy none, a courthouse
		/// halves it), 0x70e0 the luxuries, 0xe17a the taxes, 0x70e6 the science by the rates of the owner; the specialists
		/// add 2 each (taxmen, scientists, entertainers: TallySpecialists 1, 2, 3); a library (0x10) and a university (0x200)
		/// each add half of the luxuries and taxes (as translated). The locals of HandleCity and AX, BX, CX are used as in the
		/// original.
		/// </summary>
		private void SplitTrade(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			Player player = this.owner.World.Participants[(short)CityOwner];

			// The corruption of the whole trade
			this.processor.RegCount.Value16 = (ushort)(20 * player.RegimeKind + 0x50);
			short corruption = (short)((short)((short)CityTrade * (short)this.processor.PullGlobalWord(0x6b30)) * 3);
			this.processor.EmitGlobalWord(0xd2e0, (ushort)(corruption / (short)this.processor.RegCount.Value16));

			if (player.RegimeKind == 5)
				this.processor.EmitGlobalWord(0xd2e0, 0);

			if (town.Has(CityStructure.Courthouse))
			{
				this.processor.RegCount.Value16 = 2;
				this.processor.EmitGlobalWord(0xd2e0, (ushort)((short)this.processor.PullGlobalWord(0xd2e0) / 2));
			}

			// The luxuries, the taxes and the science
			short trade = (short)CityTrade;
			short corrupt = (short)this.processor.PullGlobalWord(0xd2e0);

			CityLuxuries = (ushort)this.owner.Helpers.VerifyValueSpan(((-(player.ResearchLevyRatio + player.LevyRatio - 10) * (trade - corrupt)) + 5) / 10, 0, trade);

			this.processor.EmitGlobalWord(0xe17a, (ushort)this.owner.Helpers.VerifyValueSpan(((player.LevyRatio * (trade - corrupt)) + 5) / 10, 0,
				trade - (short)CityLuxuries - corrupt));

			this.processor.RegAcc.Value16 = (ushort)(CityTrade - CityLuxuries -
				this.processor.PullGlobalWord(0xe17a) - this.processor.PullGlobalWord(0xd2e0));
			CityScience = this.processor.RegAcc.Value16;

			// The specialists
			TallySpecialists(1);
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 << 1);
			this.processor.EmitGlobalWord(0xe17a, (ushort)(this.processor.PullGlobalWord(0xe17a) + this.processor.RegAcc.Value16));

			TallySpecialists(2);
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 << 1);
			CityScience = (ushort)(CityScience + this.processor.RegAcc.Value16);

			TallySpecialists(3);
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 << 1);
			CityLuxuries = (ushort)(CityLuxuries + this.processor.RegAcc.Value16);

			// The library and the university
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			foreach (ushort improvement in new ushort[] { 0x10, 0x200 })
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.StructureMarkers0 & improvement) == 0)
					continue;

				this.processor.RegAcc.Value16 = (ushort)((short)CityLuxuries / 2);
				CityLuxuries = (ushort)(CityLuxuries + this.processor.RegAcc.Value16);
				this.processor.RegAcc.Value16 = (ushort)((short)this.processor.PullGlobalWord(0xe17a) / 2);
				this.processor.EmitGlobalWord(0xe17a, (ushort)(this.processor.PullGlobalWord(0xe17a) + this.processor.RegAcc.Value16));
			}
		}

		/// <summary>
		/// The science (0x70e6, as translated): a marketplace (0x20) and a bank (0x800) each add a half (and a third more with the
		/// wonder 0xc), the city of the wonder 10 doubles it; 0x70e2 cleared. 0x70e4 / 0xa8 the content citizens: for the
		/// human by the level, the government, the cities and the number of the city, for a computer player the size - 3;
		/// 0x6542 the citizens above the size (then 0x70e4 the size); then the citizens are reshuffled. The locals of HandleCity
		/// and AX, BX, CX, DX, SI are used as in the original.
		/// </summary>
		private void CountScienceAndContent(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];

			this.round.Value = CityScience;

			foreach (ushort improvement in new ushort[] { 0x20, 0x800 })
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.StructureMarkers0 & improvement) == 0)
					continue;

				this.processor.RegAcc.Value16 = (ushort)((short)CityScience / 2);
				this.round.Value = (ushort)(this.round.Value + this.processor.RegAcc.Value16);

				if (PlayerEnjoysWonderEffect(owner, 0xc) != 0)
				{
					this.processor.RegCount.Value16 = 3;
					this.processor.RegAcc.Value16 = (ushort)((short)CityScience / 3);
					this.round.Value = (ushort)(this.round.Value + this.processor.RegAcc.Value16);
				}
			}

			// The city of the wonder 10
			if (ObtainWonderCityID(10) == (ushort)townIdx)
			{
				this.processor.RegAcc.Value16 = this.round.Value;
				this.round.Value = (ushort)(this.round.Value + this.processor.RegAcc.Value16);
			}

			this.processor.RegAcc.Value16 = this.round.Value;
			CityScience = this.processor.RegAcc.Value16;
			CityHappy = 0;

			// The content citizens
			if (owner == this.owner.World.PersonParticipantIdx)
			{
				this.round.Value = (ushort)-(this.owner.World.HardnessTier * 2 - 0xe);
				this.processor.RegCount.Value16 = (ushort)((short)this.round.Value / 2);
				this.processor.RegAcc.Value16 = (ushort)(player.RegimeKind / 2 + 2);
				this.round.Value = (ushort)((short)this.processor.RegAcc.Value16 * (short)this.processor.RegCount.Value16);

				short step = (short)this.round.Value;

				if (step == 0)
					throw new Exception("Division by zero");

				this.processor.RegBase.Value16 = (ushort)town.TrueExtent;
				this.processor.RegSource.Value16 = (ushort)(owner << 1);
				short spread = (short)((short)(townIdx % step + player.TownTally - step) / step);
				this.processor.RegAcc.Value16 = (ushort)(spread + town.TrueExtent + this.owner.World.HardnessTier - 6);
				this.processor.RegCount.Value16 = (ushort)step;
			}
			else
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.RegAcc.Value16 = (ushort)(town.TrueExtent - 3);
			}

			CityUnhappy = this.processor.RegAcc.Value16;
			this.round.Content = this.processor.RegAcc.Value16;

			// The citizens above the size
			this.processor.EmitGlobalWord(0x6542, 0);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.TrueExtent < (short)CityUnhappy)
			{
				this.processor.RegCount.Value16 = (ushort)(CityUnhappy - town.TrueExtent);
				this.processor.EmitGlobalWord(0x6542, this.processor.RegCount.Value16);
				CityUnhappy = (ushort)town.TrueExtent;
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);
		}

		/// <summary>
		/// The luxuries: on the city screen with the tab 1 the citizens are drawn (0x42 the line), then 0x70e2 = half of the
		/// luxuries and the citizens reshuffled again, drawn once more with the luxury icon when there are any. The locals of
		/// HandleCity are used as in the original.
		/// </summary>
		private void ApplyLuxuries(short townIdx, short marker)
		{
			if (marker == 1 && CityScreenTab == 1)
			{
				this.round.IconVert = 0x74;
				RenderCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.owner.VideoCard.RenderLine(ScreenBox,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, 1);
			}

			this.processor.RegAcc.Value16 = (ushort)((short)CityLuxuries / 2);
			CityHappy = this.processor.RegAcc.Value16;

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityScreenTab == 1 && CityHappy != 0)
			{
				RenderCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.owner.Services.RenderPictureToScreen(ScreenBox, 208, (short)this.round.IconVert + 4,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xe << 1) + 0xd4ce)));
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.owner.VideoCard.RenderLine(ScreenBox,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, 1);
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)(marker < 0 ? 0xffff : 0);
		}

		/// <summary>
		/// The improvements that make citizens content (0x70e4 the unhappy ones less): a colosseum (0x2000) 3; with the advance
		/// 0x1d a cathedral (0x400) 4, or 6 with the wonder 9; a temple (8) 2 with the advance 0x1a, else 1 with 0x19, and the wonder
		/// 6 as much again (2 with 0x1a, else 1). On the city screen with the tab 1 their icons are drawn (0x6 the column, 0x42 the
		/// line), then the citizens are reshuffled and drawn again when the city has any of them. The locals of HandleCity are
		/// used as in the original.
		/// </summary>
		private void ApplyContentImprovements(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			bool tab = marker == 1 && CityScreenTab == 1;

			this.round.IconHoriz = 0xd0;
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			// The colosseum
			if (town.Has(CityStructure.Colosseum))
			{
				CityUnhappy = (ushort)(CityUnhappy - 3);

				if (tab)
					DrawContentIcon(14);
			}

			// The cathedral (the advance 0x1d; the wonder 9)
			if (this.owner.Advisors.PlayerKnowsTechnology(owner, 0x1d) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if (town.Has(CityStructure.Cathedral))
					this.processor.RegAcc.Value16 = (ushort)(PlayerEnjoysWonderEffect(owner, 9) != 0 ? 6 : 4);
				else
					this.processor.RegAcc.Value16 = 0;

				CityUnhappy = (ushort)(CityUnhappy - this.processor.RegAcc.Value16);

				if (tab)
				{
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

					if (town.Has(CityStructure.Cathedral))
						DrawContentIcon(11);
				}
			}

			// The temple (the advances 0x1a and 0x19; the wonder 6)
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.Has(CityStructure.Temple))
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(owner, 0x1a) != 0)
					CityUnhappy = (ushort)(CityUnhappy - 2);
				else if (this.owner.Advisors.PlayerKnowsTechnology(owner, 0x19) != 0)
					CityUnhappy = (ushort)(CityUnhappy - 1);

				if (PlayerEnjoysWonderEffect(owner, 6) != 0)
				{
					this.processor.RegAcc.Value16 = (ushort)(this.owner.Advisors.PlayerKnowsTechnology(owner, 0x1a) != 0 ? 2 : 1);
					CityUnhappy = (ushort)(CityUnhappy - this.processor.RegAcc.Value16);
				}

				if (tab)
					DrawContentIcon(4);
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityScreenTab == 1)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.StructureMarkers0 & 0x2408) != 0)
				{
					RenderCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
					this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
					this.owner.VideoCard.RenderLine(ScreenBox,
						100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, 1);
				}
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)(marker < 0 ? 0xffff : 0);
		}

		/// <summary>An icon of the citizens' contentment on the city screen at the column 0x6 (which moves 16 left)</summary>
		private void DrawContentIcon(short icon)
		{
			RenderResourceIcon(icon, (short)this.round.IconHoriz, (short)this.round.IconVert);
			this.round.IconHoriz = (ushort)(this.round.IconHoriz - 0x10);
		}

		/// <summary>
		/// The military and the citizens: below republic (government under 4) up to 3 attack units in the city (its stack, at
		/// most 32 steps, and the units in the special slots while some are unhappy) make as many unhappy citizens content
		/// (drawn on the city screen, 0xe6 the count); in a republic or democracy every attack unit away (0x6546) makes one
		/// citizen unhappy (two in a democracy, none in a republic with the wonder 16; drawn on the screen). Then the citizens
		/// are reshuffled and drawn again. The locals of HandleCity and BX, CX, SI are used as in the original.
		/// </summary>
		private void ApplyMilitaryHappiness(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];
			bool tab = marker == 1 && CityScreenTab == 1;

			this.round.IconHoriz = 0xd0;

			if (player.RegimeKind < 4)
			{
				// Martial law: the attack units in the city
				this.processor.RegAcc.Value16 = 0;
				this.round.MartialLawUnits = 0;
				this.round.UnitsAway = 0;

				this.round.Value = this.owner.MapOps.ObtainUnitAtTile((short)this.round.Horiz, (short)this.round.Vert);
				this.round.NewUnit = this.processor.RegAcc.Value16;

				while (this.round.NewUnit != 0xffff)
				{
					this.round.MartialLawUnits = (ushort)(this.round.MartialLawUnits + 1);

					if ((short)this.round.MartialLawUnits >= 0x20)
						break;

					short troopIdx = (short)this.round.NewUnit;
					this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(owner, troopIdx) * UnitKindTable.Stride);

					if (this.troops.KindField(owner, troopIdx, UnitKindTable.AssaultPower) != 0)
					{
						this.round.UnitsAway = (ushort)(this.round.UnitsAway + 1);

						if (tab && (short)this.round.UnitsAway <= 3)
						{
							this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.IconHoriz,
								(short)this.round.IconVert - 1,
								this.processor.PullUInt16(this.processor.SegData.Value16,
									(ushort)(((this.troops.Pull(owner, troopIdx, UnitMemory.Kind) + (owner << 5) + 0x40) << 1) + 0xd4ce)));
							this.round.IconHoriz = (ushort)(this.round.IconHoriz - 2);
						}
					}

					this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
					this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.round.NewUnit = (ushort)this.troops.PullSigned(owner, troopIdx, UnitMemory.FollowingInStack);
					this.processor.RegAcc.Value16 = this.round.Value;

					if (this.round.NewUnit == this.round.Value)
						this.round.NewUnit = 0xffff;
				}

				// The attack units in the special slots
				for (ushort slot = 0; slot < 2; slot++)
				{
					this.round.NewUnit = slot;

					if (CityUnhappy == 0)
						continue;

					this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
					this.processor.RegBase.Value16 = slot;

					if ((byte)town.Unfamiliar[slot] == 0xff)
						continue;

					this.processor.RegCount.Value16 = 0x22;
					this.processor.RegBase.Value16 = (ushort)((town.Unfamiliar[slot] & 0x3f) * 0x22);

					if (this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)) != 0)
						this.round.UnitsAway = (ushort)(this.round.UnitsAway + 1);
				}

				this.round.NewUnit = 2;

				if ((short)this.round.UnitsAway > 3)
					this.round.UnitsAway = 3;

				CityUnhappy = (ushort)(CityUnhappy - (ushort)this.owner.Helpers.VerifyValueSpan((short)this.round.UnitsAway, 0, (short)CityUnhappy));
			}
			else
			{
				// The units away make citizens unhappy (not with the wonder 16 in a republic; two in a democracy)
				this.processor.RegCount.Value16 = (ushort)(PlayerEnjoysWonderEffect(owner, 16) < 1 ? 1 : 0);
				this.round.Value = this.processor.RegCount.Value16;

				if (player.RegimeKind == 5)
					this.round.Value = (ushort)(this.round.Value + 1);

				if (this.round.Value != 0)
				{
					short unhappy = (short)((short)this.round.Value * (short)this.processor.PullGlobalWord(0x6546));
					this.processor.RegAcc.Value16 = (ushort)unhappy;
					CityUnhappy = (ushort)(CityUnhappy + unhappy);

					if (tab)
					{
						ushort drawn = 0;

						for (; unhappy > (short)drawn; drawn++)
						{
							this.round.NewUnit = drawn;
							this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.IconHoriz,
								(short)this.round.IconVert + 4, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));
							this.round.IconHoriz = (ushort)(this.round.IconHoriz - 2);
						}

						this.round.NewUnit = drawn;
					}
				}
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityScreenTab == 1)
			{
				RenderCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.owner.VideoCard.RenderLine(ScreenBox,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, 1);
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)(marker < 0 ? 0xffff : 0);
		}

		/// <summary>
		/// The wonders and the citizens: the wonders 2 and 0x15 each make one more citizen happy (0x70e2), the city of the
		/// wonder 11 has no unhappy ones, the wonder 13 makes two content in the cities on its continent; the citizens are
		/// reshuffled and drawn on the city screen (tab 1) when something changed (0xe8 the balance before). Without the bit 2
		/// of DiagMarkers 0x70e4 and 0x70e2 are cleared. The locals of HandleCity and DI, SI are used as in the original.
		/// </summary>
		private void ApplyWonderHappiness(short townIdx, short marker)
		{
			short owner = (short)CityOwner;

			this.round.Value = (ushort)(CityHappy - CityUnhappy);

			if (PlayerEnjoysWonderEffect(owner, 2) != 0)
				CityHappy = (ushort)(CityHappy + 1);

			if (PlayerEnjoysWonderEffect(owner, 0x15) != 0)
				CityHappy = (ushort)(CityHappy + 1);

			if (ObtainWonderCityID(11) == (ushort)townIdx)
				CityUnhappy = 0;

			if (PlayerEnjoysWonderEffect(owner, 0xd) != 0)
			{
				// The wonder 13 on the continent of the city
				short marvelIdx = this.owner.World.MarvelTownIdx[13];
				this.processor.RegSource.Value16 = (ushort)(marvelIdx * 0x1c);
				this.processor.RegDest.Value16 = this.owner.MapOps.ObtainContinentID(this.owner.World.Towns[marvelIdx].Place.X, this.owner.World.Towns[marvelIdx].Place.Y);
				if (this.owner.MapOps.ObtainContinentID((short)this.round.Horiz, (short)this.round.Vert) == this.processor.RegDest.Value16)
					CityUnhappy = (ushort)(CityUnhappy - 2);
			}

			ReshuffleCitizens(townIdx, this.round.Specialists);

			if (marker == 1 && CityScreenTab == 1 &&
				(ushort)(CityHappy - CityUnhappy) != this.round.Value)
			{
				RenderCitizenIcons(townIdx, 100, (short)this.round.IconVert, this.round.Specialists, 92);
				this.owner.TextPainter.RenderTextToScreen0(0x25fe, 190, (short)this.round.IconVert + 5, 15);
				this.round.IconVert = (ushort)(this.round.IconVert + 0x10);
				this.owner.VideoCard.RenderLine(ScreenBox,
					100, (short)this.round.IconVert - 2, 222, (short)this.round.IconVert - 2, 1);
			}

			if ((this.owner.World.DiagMarkers & 2) == 0)
			{
				this.processor.RegAcc.Value16 = 0;
				CityUnhappy = 0;
				CityHappy = 0;
			}
		}

		/// <summary>
		/// The start of the units of the city (again after a unit was disbanded): 0x4e the units drawn in the city, 0xdeb8
		/// the support (the special slots in use first), 0xdeb6, 0xd2f6 the support above the size; the places on the city
		/// screen (0xf6 / 0xfc the supported units, 0xfa / 0x100 the units in the city). The locals of HandleCity and BX, CX,
		/// SI are used as in the original.
		/// </summary>
		private void StartUnitSupport(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegAcc.Value16 = 0;
			this.round.UnitsDrawn = 0;
			this.processor.EmitGlobalWord(0xdeb8, 0);
			this.processor.EmitGlobalWord(0xdeb6, 0);
			this.processor.EmitGlobalWord(0xd2f6, 0);

			CountSpecialSlots(townIdx);

			this.round.DrawHoriz = 8;
			this.round.DrawVert = 0x45;
			this.round.UnitHoriz = 0x64;
			this.round.UnitVert = 0x74;
		}

		/// <summary>
		/// The unit 0xea of the owner: one supported by the city adds its support (but diplomats and caravans: above the size,
		/// or every one for a government above monarchy with the bit 2 of DiagMarkers), may be disbanded in the turn and is
		/// drawn among the supported units of the city screen; every unit in the city is drawn in its list. True when a unit
		/// was disbanded for lack of shields (all the units are counted again).
		/// </summary>
		private bool SupportUnit(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short troopIdx = (short)this.round.Index;

			this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);

			if (this.troops.Pull(owner, troopIdx, UnitMemory.Kind) != 0xff && this.troops.PullSigned(owner, troopIdx, UnitMemory.BaseTown) == townIdx)
			{
				if (this.troops.PullSigned(owner, troopIdx, UnitMemory.Kind) < 0x1a)
				{
					this.processor.EmitGlobalWord(0xdeb8, (ushort)(this.processor.PullGlobalWord(0xdeb8) + 1));

					if (town.TrueExtent < (short)this.processor.PullGlobalWord(0xdeb8) ||
						(this.owner.World.Participants[owner].RegimeKind > 1 && (this.owner.World.DiagMarkers & 2) != 0))
					{
						this.processor.EmitGlobalWord(0xd2f6, (ushort)(this.processor.PullGlobalWord(0xd2f6) + 1));
					}
				}

				if (marker == 0 && DisbandUnsupportedUnit(townIdx))
					return true;

				if (marker == 1)
					DrawSupportedUnit(townIdx);
			}

			DrawUnitInCity(marker);
			return false;
		}

		/// <summary>
		/// In the turn: without shields for the support (or for a computer player in a republic or democracy, its city with
		/// the flag 1, every eighth turn), the unit of the city farthest away (but diplomats and caravans) goes: for lack of
		/// shields it is disbanded (the human is told) and true is returned; else a land unit other than settlers is disbanded
		/// and the flag 1 cleared, unless 0x8078 and the owner knows fewer advances than the human (then 0xdb42 = -999). The
		/// locals of HandleCity (0x108 the unit, 0xc8 its distance) and DI, SI are used as in the original.
		/// </summary>
		private bool DisbandUnsupportedUnit(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;
			bool enoughShields = (short)CityShields >= (short)this.processor.PullGlobalWord(0xd2f6);

			if (enoughShields)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.ConditionMarker & 1) == 0 || ((byte)((byte)this.round.Index + (byte)(this.owner.World.RoundTally & 0xff)) & 7) != 0 ||
					owner == person || this.owner.World.Participants[owner].RegimeKind < 4)
				{
					return false;
				}
			}

			// The unit of the city farthest away
			this.processor.RegAcc.Value16 = 0xffff;
			this.round.Found = 0xffff;
			this.round.Best = 0xffff;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.Selected = otherIdx;
				short other = (short)otherIdx;
				this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(otherIdx * UnitMemory.TroopStride);

				if (this.troops.Pull(owner, other, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, other, UnitMemory.BaseTown) != townIdx ||
					this.troops.PullSigned(owner, other, UnitMemory.Kind) >= 0x1a)
				{
					continue;
				}

				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.RegDest.Value16 = (ushort)(otherIdx * UnitMemory.TroopStride + owner * UnitMemory.ParticipantStride);

				this.round.Distance = (ushort)this.owner.Helpers.ObtainDistance(town.Place.X, this.troops.PullSigned(owner, other, UnitMemory.Horiz),
					town.Place.Y, this.troops.PullSigned(owner, other, UnitMemory.Vert));
				this.processor.RegAcc.Value16 = this.round.Best;

				if ((short)this.round.Distance > (short)this.round.Best)
				{
					this.round.Best = this.round.Distance;
					this.processor.RegAcc.Value16 = this.round.Selected;
					this.round.Found = this.processor.RegAcc.Value16;
				}
			}

			this.round.Selected = 0x80;
			short farthest = (short)this.round.Found;

			if (enoughShields)
			{
				if (this.processor.PullGlobalWord(0x8078) != 0 &&
					this.owner.World.Participants[owner].FoundScienceTally < this.owner.World.Participants[person].FoundScienceTally)
				{
					this.processor.EmitGlobalWord(0xdb42, 0xfc19);
				}
				else if ((short)this.round.Best > 0)
				{
					this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
					this.processor.RegSource.Value16 = (ushort)(farthest * UnitMemory.TroopStride);

					if (this.troops.KindField(owner, farthest, UnitKindTable.LandGroup) == UnitKindTable.LandUnits && this.troops.Pull(owner, farthest, UnitMemory.Kind) != 0)
					{
						this.owner.UnitOps.EliminateUnit(owner, farthest);
						this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
						town.ConditionMarker &= 0xfe;
					}
				}

				return false;
			}

			// No shields for the support: the unit is disbanded
			if (owner == person)
			{
				EmptyMessage()
					.City(townIdx)
					.Text(" can't support\n");
				this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(farthest * UnitMemory.TroopStride);
				this.processor.RegAcc.Value16 = (ushort)(this.troops.TroopKind(owner, farthest) * UnitKindTable.Stride + UnitKindTable.Origin);
				this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);
				this.owner.Libc.AppendText(0xba06, ".\n Unit Disbanded.\n");
				DialogStyle = 3;
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.UnitOps.EliminateUnit(owner, (short)this.round.Found);
			return true;
		}

		/// <summary>
		/// A unit supported by the city on the city screen (0xf6 / 0xfc its place, 8 a row, 3 rows): the shield of its support
		/// (when some is paid), the food of settlers (two for a government above anarchy), the unhappy faces of an attack unit
		/// away in a republic or democracy (two in a democracy; none in a republic with the wonder 16), the unit and its dot on
		/// the mini map
		/// </summary>
		private void DrawSupportedUnit(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short troopIdx = (short)this.round.Index;
			Player player = this.owner.World.Participants[owner];

			this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);

			if (this.troops.PullSigned(owner, troopIdx, UnitMemory.Kind) < 0x1a)
			{
				if (this.processor.PullGlobalWord(0xd2f6) != 0)
				{
					this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.DrawHoriz + 8,
						(short)this.round.DrawVert + 12, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
				}

				if (this.troops.Pull(owner, troopIdx, UnitMemory.Kind) == 0)
				{
					// The food of settlers
					this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.DrawHoriz,
						(short)this.round.DrawVert + 12, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));

					if (player.RegimeKind >= 2)
					{
						this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.DrawHoriz + 2,
							(short)this.round.DrawVert + 12, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
					}
				}
				else
				{
					// The unhappy faces of an attack unit away
					this.processor.RegCount.Value16 = (ushort)(PlayerEnjoysWonderEffect(owner, 16) < 1 ? 1 : 0);
					this.round.Value = this.processor.RegCount.Value16;

					if (player.RegimeKind == 5)
						this.round.Value = (ushort)(this.round.Value + 1);

					if (player.RegimeKind >= 4 && this.troops.KindField(owner, troopIdx, UnitKindTable.AssaultPower) != 0 && this.round.Value != 0 &&
						(this.troops.KindField(owner, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troopIdx, UnitMemory.Horiz) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troopIdx, UnitMemory.Vert) != (byte)(sbyte)town.Place.Y))
					{
						this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.DrawHoriz,
							(short)this.round.DrawVert + 12, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

						if ((short)this.round.Value > 1)
						{
							this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.DrawHoriz + 2,
								(short)this.round.DrawVert + 12, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));
						}
					}
				}
			}

			// The unit and its dot on the mini map
			this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.DrawHoriz, (short)this.round.DrawVert,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.troops.Pull(owner, troopIdx, UnitMemory.Kind) + (owner << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + owner * UnitMemory.ParticipantStride);
			RenderMiniMapDot(this.troops.Pull(owner, troopIdx, UnitMemory.Horiz), this.troops.Pull(owner, troopIdx, UnitMemory.Vert), 7);

			this.round.DrawHoriz = (ushort)(this.round.DrawHoriz + 0x10);

			if ((short)this.round.DrawHoriz >= 0x70)
			{
				this.round.DrawHoriz = 8;
				this.round.DrawVert = (ushort)(this.round.DrawVert + 0x10);

				if ((short)this.round.DrawVert > 0x55)
					this.round.DrawVert = (ushort)(this.round.DrawVert - 0x18);
			}
		}

		/// <summary>
		/// A unit standing in the city on the city screen with the tab 0 (at most 18, 6 a row; 0xfa / 0x100 the place): the unit
		/// and the abbreviation of its home city; its number goes into the list at BP-0x74 (0x4e the count)
		/// </summary>
		private void DrawUnitInCity(short marker)
		{
			short owner = (short)CityOwner;
			short troopIdx = (short)this.round.Index;

			if ((short)this.round.UnitsDrawn >= 0x12 || CityScreenTab != 0)
				return;

			this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);

			if (this.troops.Pull(owner, troopIdx, UnitMemory.Kind) == 0xff || marker != 1 ||
				this.troops.Pull(owner, troopIdx, UnitMemory.Horiz) != this.round.Horiz ||
				this.troops.Pull(owner, troopIdx, UnitMemory.Vert) != this.round.Vert)
			{
				return;
			}

			this.owner.MapOps.RenderUnitSprite(owner, troopIdx, (short)this.round.UnitHoriz, (short)this.round.UnitVert);

			// The abbreviation of the home city (3 letters and a dot)
			this.processor.EmitGlobalByte(0xba06, 0);
			this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.owner.Conquest.ObtainCityLabel(this.troops.PullSigned(owner, troopIdx, UnitMemory.BaseTown));
			this.processor.EmitGlobalByte(0xba09, 0x2e);
			this.processor.EmitGlobalByte(0xba0a, 0);
			this.owner.TextPainter.RenderTextToScreen0(0xba06, (short)this.round.UnitHoriz, (short)this.round.UnitVert + 15, 0);

			ushort drawn = this.round.UnitsDrawn;
			this.round.UnitsDrawn = (ushort)(drawn + 1);
			this.processor.RegSource.Value16 = (ushort)(drawn << 1);
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74), this.processor.RegAcc.Value16);
			this.round.UnitHoriz = (ushort)(this.round.UnitHoriz + 0x12);

			this.processor.RegCount.Value16 = 6;

			if ((short)this.round.UnitsDrawn % 6 == 0)
			{
				this.round.UnitHoriz = 0x64;
				this.round.UnitVert = (ushort)(this.round.UnitVert + 0x10);
			}
		}

		/// <summary>
		/// In the turn: the support the shields cannot pay adds 5 each to 0xe3c2, the supported units (at most the size) add
		/// one each; 0xdb42 falls by the attack units away (0x6546) x (5 with a library, else 7, less the word 2 of the record
		/// of the owner's leader at 0x1516). AX, BX, CX are used as in the original.
		/// </summary>
		private void CountSupportCosts(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			Player player = this.owner.World.Participants[(short)CityOwner];

			if ((short)CityShields < (short)this.processor.PullGlobalWord(0xdeb8))
			{
				this.processor.RegCount.Value16 = 5;
				this.processor.RegAcc.Value16 = (ushort)((short)(this.processor.PullGlobalWord(0xdeb8) - CityShields) * 5);
				this.processor.EmitGlobalWord(0xe3c2, (ushort)(this.processor.PullGlobalWord(0xe3c2) + this.processor.RegAcc.Value16));
			}

			this.processor.EmitGlobalWord(0xe3c2, (ushort)(this.processor.PullGlobalWord(0xe3c2) + (ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0xdeb8), 0, town.TrueExtent)));

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegCount.Value16 = (ushort)(town.Has(CityStructure.Marketplace) ? 5 : 7);
			this.processor.RegBase.Value16 = (ushort)(player.ChiefVisuals * 0x3a);

			short factor = (short)(this.processor.RegCount.Value16 - this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516)));
			this.processor.RegAcc.Value16 = (ushort)(factor * (short)this.processor.PullGlobalWord(0x6546));
			this.processor.EmitGlobalWord(0xdb42, (ushort)(this.processor.PullGlobalWord(0xdb42) - this.processor.RegAcc.Value16));
		}

		/// <summary>
		/// The city screen, the list of improvements (0x106 the row, 0xe4 its line, 0x40 the wonders, 0xb4 the improvements
		/// listed): first the wonders of the city (when the list starts at 0, 0xda), then the improvements from 0xda on; a full
		/// list (16 rows) stops at the improvement it reached (0xfe) and allows the next page (0xca bit 2, bit 1 cleared)
		/// </summary>
		private void RenderImprovementList(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 2);
			RenderFrame(211, 1, 317, 97);

			this.processor.RegAcc.Value16 = 2;
			this.round.Selected = 2;
			this.round.Vert = 2;
			this.processor.RegAcc.Value16 = 0;
			this.round.WonderRows = 0;
			this.round.ImprovementCount = 0;

			if (this.round.ListStart == 0)
			{
				// The wonders of the city
				for (ushort wonder = 0; wonder < 0x15; wonder++)
				{
					this.round.Item = wonder;
					this.processor.RegAcc.Value16 = (ushort)townIdx;

					if (this.owner.World.MarvelTownIdx[wonder + 1] != townIdx)
						continue;

					this.round.Value = (ushort)(wonder + 0x18);
					RenderImprovementName(this.round.Value, 63, true);
					this.round.WonderRows = this.processor.IncrementWord(this.round.WonderRows);
				}

				this.round.Item = 0x15;
			}

			// The improvements from 0xda on
			this.round.ListEnd = 0;
			this.processor.RegAcc.Value16 = this.round.ListStart;
			this.round.Item = this.processor.RegAcc.Value16;

			for (; (short)this.round.Item < 0x18; this.round.Item = (ushort)(this.round.Item + 1))
			{
				ushort improvement = this.round.Item;
				uint mask = 1u << (improvement & 0xff);
				this.processor.RegCount.Value16 = (ushort)(mask & town.StructureMarkers0);
				this.processor.RegBase.Value16 = (ushort)(((mask >> 16) & town.StructureMarkers1) | this.processor.RegCount.Value16);
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

				if (this.processor.RegBase.Value16 == 0)
					continue;

				if ((short)this.round.Selected >= 0x10)
				{
					// The list is full: the next page
					this.round.ListFlags = (ushort)((this.round.ListFlags | 2) & 0xfffe);
					this.processor.RegAcc.Value16 = this.round.Item;
					this.round.ListEnd = this.processor.RegAcc.Value16;
					break;
				}

				this.round.ImprovementCount = (ushort)(this.round.ImprovementCount + 1);
				this.owner.Services.RenderPictureToScreen(ScreenBox, 309, (short)this.round.Vert + 1,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xb << 1) + 0xd4ce)));
				RenderImprovementName(improvement, 56, false);
			}
		}

		/// <summary>A row of the improvement list: the name (upper case, within the width), the icon (left or right by the row)</summary>
		private void RenderImprovementName(ushort improvement, ushort width, bool shrinkFirst)
		{
			this.processor.RegAcc.Value16 = (ushort)(improvement * 0x1e + 0xbb8);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			if (shrinkFirst)
			{
				this.owner.Libc.UppercaseText(0xba06);
				this.owner.BlockText.ShrinkTextToWidth(0xba06, width);
			}
			else
			{
				this.owner.BlockText.ShrinkTextToWidth(0xba06, width);
				this.owner.Libc.UppercaseText(0xba06);
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 253, (short)this.round.Vert + 2, 15);
			RenderResourceIcon((short)(improvement + 1), (short)((((byte)this.round.Selected & 1) != 0) ? 213 : 233),
				(short)((short)this.round.Vert - 2));

			this.round.Vert = (ushort)(this.round.Vert + 6);
			this.round.Selected = this.processor.IncrementWord(this.round.Selected);
		}

		/// <summary>
		/// The frame of the improvement list (the "more" box when a next page exists), and 0xde the price of the production
		/// (a unit or an improvement)
		/// </summary>
		private void FinishImprovementList(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.owner.VideoCard.RenderLine(ScreenBox, 231, 0, 250, 0, 0);
			this.owner.VideoCard.RenderLine(ScreenBox, 231, 1, 250, 1, 1);

			if (((byte)this.round.ListFlags & 2) != 0)
				RenderLabelBox(287, 88, 315, 96, 0x262a, 9);

			this.owner.VideoCard.SubstituteHue(ScreenBox, 309, 2, 8, 96, 14, 12);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx >= 0)
			{
				this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
				this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + UnitKindTable.Origin + UnitKindTable.Price));
			}
			else
			{
				this.processor.RegCount.Value16 = 0x1e;
				this.processor.RegBase.Value16 = (ushort)(-town.PresentMakingIdx * 0x1e);
				this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			}

			this.round.ProductionPrice = this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The production box of the city screen: 0x44 the shields per row (by the price and the shields, 1 - 99), 0xe8 the step,
		/// 0xf6 / 0xfc its width and height; the Change and Buy buttons of a human city (Change shows the advisor flag 0x10),
		/// the unit or the name of the improvement in production. AX, BX, CX, DX are used as in the original.
		/// </summary>
		private void RenderProductionBox(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.round.ShieldsPerRow = (ushort)this.owner.Helpers.VerifyValueSpan((((short)this.round.ProductionPrice - 1) / 10) + 1, ((town.MaterialsTally - 1) / 100) + 1, 99);

			short box = (short)this.round.CostFactor;

			if (box == 0)
				throw new Exception("Division by zero");

			this.round.Value = (ushort)(80 / box);
			short perRow = (short)this.round.ShieldsPerRow;
			short width = (short)(box * (short)this.round.Value);
			this.processor.RegCount.Value16 = (ushort)width;
			this.processor.RegBase.Value16 = (ushort)perRow;
			this.round.DrawHoriz = (ushort)((short)((perRow << 3) - 8) / perRow + width);
			this.processor.RegCount.Value16 = (ushort)perRow;
			this.round.DrawVert = (ushort)((((short)this.round.ProductionPrice - 1) / perRow << 3) + 8);
			this.processor.RegAcc.Value16 = this.round.DrawVert;

			this.owner.Services.FloodBox(ScreenBox, 230, 99, (short)this.round.DrawHoriz + 3, (short)this.round.DrawVert + 19, 1);

			if (owner == this.owner.World.PersonParticipantIdx)
			{
				RenderLabelBox(231, 106, 263, 114, (ushort)((town.ConditionMarker & 0x10) != 0 ? 0x262f : 0x2635), 9);
				RenderLabelBox(294, 106, 311, 114, 0x263c, 9);
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx >= 0)
			{
				this.owner.Services.RenderPictureToScreen(ScreenBox, 264, 100,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((((byte)town.PresentMakingIdx + (owner << 5) + 0x40) << 1) + 0xd4ce)));
			}
			else
			{
				this.processor.RegCount.Value16 = 0x1e;
				this.processor.RegAcc.Value16 = (ushort)(-town.PresentMakingIdx * 0x1e + 0xb9a);
				this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);
				this.owner.BlockText.ShrinkTextToWidth(0xba06, 0x56);
				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 274, 100, 15);
			}
		}

		/// <summary>The shields of the production box (0x44 a row, 0xe8 the step)</summary>
		private void RenderShieldsInBox(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			FloodBoxWithMotif(231, 116, (short)this.round.DrawHoriz + 1, (short)this.round.DrawVert + 1);

			ushort shield = 0;

			for (; town.MaterialsTally > (short)shield; shield++)
			{
				this.round.Index = shield;
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.owner.Services.RenderPictureToScreen(ScreenBox,
					((((short)this.round.Index % ((short)this.round.CostFactor * (short)this.round.ShieldsPerRow)) *
						(short)this.round.Value) / (short)this.round.ShieldsPerRow) + 232,
					(((short)this.round.Index / ((short)this.round.CostFactor * (short)this.round.ShieldsPerRow)) * 8) + 117,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
			}

			this.round.Index = shield;
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
		}

		/// <summary>
		/// The food box of the city screen: 0xe8 the step (80 / size, 1 - 8), 0xfc the row height (80 / 0x4a), the line of a
		/// granary, the food stored (at most what the city needs to grow). SI is used as in the original.
		/// </summary>
		private void RenderFoodBox(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if (town.TrueExtent != 0)
				this.owner.Helpers.VerifyValueSpan(80 / town.TrueExtent, 1, 8);
			else
				this.processor.RegAcc.Value16 = 1;

			this.round.Value = this.processor.RegAcc.Value16;

			short box = (short)this.round.CostFactor;

			if (box == 0)
				throw new Exception("Division by zero");

			this.processor.RegCount.Value16 = (ushort)box;
			this.round.DrawVert = (ushort)(80 / box);
			this.processor.RegAcc.Value16 = this.round.DrawVert;

			this.owner.Services.FloodBox(ScreenBox, 2, 106, 91,
				((short)this.round.CostFactor * (short)this.round.DrawVert) + 12, 1);
			this.owner.TextPainter.RenderTextToScreen0(0x2640, 8, 108, 15);
			FloodBoxWithMotif(3, 115, (town.TrueExtent * (short)this.round.Value) + 9,
				((short)this.round.CostFactor * (short)this.round.DrawVert) + 2);

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			// The granary
			if (town.Has(CityStructure.Granary))
			{
				this.owner.VideoCard.RenderLine(ScreenBox, 5, 155,
					(town.TrueExtent * (short)this.round.Value) + 9, 155, 1);
			}

			// The food stored
			for (ushort grain = 0; ; grain++)
			{
				this.round.Index = grain;
				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
				if ((short)this.owner.Helpers.VerifyValueSpan(town.GrainTally, 0, (town.TrueExtent + 1) * (short)this.round.CostFactor) <= (short)grain)
					break;

				this.processor.RegSource.Value16 = (ushort)(town.TrueExtent + 1);
				this.owner.Services.RenderPictureToScreen(ScreenBox,
					(((short)this.round.Index % (short)this.processor.RegSource.Value16) * (short)this.round.Value) + 4,
					(((short)this.round.Index / (short)this.processor.RegSource.Value16) * (short)this.round.DrawVert) + 116,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
			}
		}

		/// <summary>
		/// The resources box of the city screen: three rows (food, shields, trade: the words at 0x70da) of icons, the step
		/// (0xe8) by how many fit; a gap marks where the food the citizens and settlers eat ends, where the support of units
		/// ends (0xd2f6) and where the corruption starts (0x70de less 0xd2e0). 0xd8 the column, 0xfc the first line.
		/// </summary>
		private void RenderResourceRows(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			RenderFrame(2, 23, 124, 65);
			this.round.DrawVert = 0x19;
			this.owner.Services.FloodBox(ScreenBox, 2, 23, 122, 9, 1);
			this.owner.TextPainter.RenderTextToScreen0(0x264d, 8, (short)this.round.DrawVert, 15);
			this.round.DrawVert = (ushort)(this.round.DrawVert + 8);

			for (ushort row = 0; row < 3; row++)
			{
				this.round.Item = row;
				this.round.Horiz = 4;
				ushort amount = (ushort)((row << 1) + 0x70da);

				int step = row == 0
					? this.owner.Helpers.VerifyValueSpan(116 / ((short)this.owner.Helpers.VerifyValueSpan(this.processor.PullInt16(this.processor.SegData.Value16, amount),
						(town.TrueExtent * 2) + ((short)this.round.SettlerFood * this.processor.PullGlobalSignedWord(0xe3c6)), 999) + 1), 1, 8)
					: this.owner.Helpers.VerifyValueSpan(116 / (this.processor.PullInt16(this.processor.SegData.Value16, amount) + 1), 1, 8);

				this.round.Value = (ushort)step;
				ushort icon = 0;

				for (; (short)this.processor.PullUInt16(this.processor.SegData.Value16, amount) > (short)icon; icon++)
				{
					this.round.Index = icon;
					this.processor.RegBase.Value16 = (ushort)(row << 1);

					// The gaps: the food eaten, the support paid, the corruption
					if (row == 0 && (ushort)(town.TrueExtent * 2 + (short)this.round.SettlerFood * (short)this.processor.PullGlobalWord(0xe3c6)) == icon)
						this.round.Horiz = (ushort)(this.round.Horiz + 4);

					if (row == 1 && this.processor.PullGlobalWord(0xd2f6) != 0 && icon == this.processor.PullGlobalWord(0xd2f6))
						this.round.Horiz = (ushort)(this.round.Horiz + 4);

					if (row == 2 && (ushort)(CityTrade - this.processor.PullGlobalWord(0xd2e0)) == icon)
						this.round.Horiz = (ushort)(this.round.Horiz + 2);

					this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.Horiz,
						((short)this.round.Item * 8) + (short)this.round.DrawVert,
						this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(((row + 8) << 1) + 0xd4ce)));

					this.processor.RegAcc.Value16 = this.round.Value;
					this.round.Horiz = (ushort)(this.round.Horiz + this.processor.RegAcc.Value16);
				}

				this.round.Index = icon;
			}

			this.round.Item = 3;
		}

		/// <summary>
		/// The row of luxuries, taxes and science on the city screen (0xe8 the step by how many fit, 1 - 16; a gap after the
		/// luxuries and after the taxes; 0xf6 where the taxes start)
		/// </summary>
		private void RenderTradeRow()
		{
			this.round.Horiz = 8;
			this.round.Value = (ushort)this.owner.Helpers.VerifyValueSpan(224 / ((short)CityLuxuries + (short)CityScience +
				this.processor.PullGlobalSignedWord(0xe17a) + this.processor.PullGlobalSignedWord(0xd2e0) + 2), 1, 16);

			RenderTradeIcons(0x70e0, 0xe);

			if (CityLuxuries != 0)
				this.round.Horiz = (ushort)(this.round.Horiz + 8);

			this.processor.RegAcc.Value16 = this.round.Horiz;
			this.round.DrawHoriz = this.processor.RegAcc.Value16;

			RenderTradeIcons(0xe17a, 0xb);

			if (this.processor.PullGlobalWord(0xe17a) != 0)
				this.round.Horiz = (ushort)(this.round.Horiz + 8);

			RenderTradeIcons(0x70e6, 0xc);
		}

		/// <summary>The icons of one part of the trade (as many as the word says), half the column 0xd8 apart by 0xe8</summary>
		private void RenderTradeIcons(ushort amount, ushort icon)
		{
			ushort drawn = 0;

			for (; (short)drawn < (short)this.processor.PullGlobalWord(amount); drawn++)
			{
				this.round.Index = drawn;
				this.owner.Services.RenderPictureToScreen(ScreenBox, (short)this.round.Horiz / 2,
					(short)this.round.DrawVert + 24, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((icon << 1) + 0xd4ce)));
				this.processor.RegAcc.Value16 = this.round.Value;
				this.round.Horiz = (ushort)(this.round.Horiz + this.processor.RegAcc.Value16);
			}

			this.round.Index = drawn;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(amount);
		}

		/// <summary>
		/// The shortfalls of the resources box: the food the citizens and settlers eat beyond the food gathered (0x70da) and the
		/// shields of the support beyond the production (0x70dc, 0xd2f6) drawn past the row and shaded; the corruption (0xd2e0)
		/// shaded in the trade row. 0xe8 the step, 0xea the icon, 0xfc the first line.
		/// </summary>
		private void RenderShortfalls(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			int Eaten() => (town.TrueExtent * 2) + ((short)this.round.SettlerFood * this.processor.PullGlobalSignedWord(0xe3c6));

			// The food missing
			if ((short)Eaten() > (short)CityFood)
			{
				this.round.Value = (ushort)this.owner.Helpers.VerifyValueSpan(116 / (Eaten() + 1), 1, 8);
				ushort icon = CityFood;

				for (; (short)Eaten() > (short)icon; icon++)
				{
					this.round.Index = icon;
					this.owner.Services.RenderPictureToScreen(ScreenBox,
						((short)this.round.Index * (short)this.round.Value) + 8,
						(short)this.round.DrawVert,
						this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
				}

				this.round.Index = icon;
				this.owner.VideoCard.SubstituteHue(ScreenBox,
					((short)this.round.Value * (short)CityFood) + 8,
					(short)this.round.DrawVert,
					((Eaten() - (short)CityFood) * (short)this.round.Value) + 4,
					8, 15, 0);
			}

			// The shields of the support missing
			if ((short)CityShields < this.processor.PullGlobalSignedWord(0xd2f6))
			{
				this.round.Value = (ushort)this.owner.Helpers.VerifyValueSpan(116 / ((short)CityShields + 1), 1, 8);
				ushort icon = CityShields;

				for (; (short)icon < this.processor.PullGlobalSignedWord(0xd2f6); icon++)
				{
					this.round.Index = icon;
					this.owner.Services.RenderPictureToScreen(ScreenBox,
						((short)this.round.Index * (short)this.round.Value) + 8,
						(short)this.round.DrawVert + 8,
						this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
				}

				this.round.Index = icon;
				this.owner.VideoCard.SubstituteHue(ScreenBox,
					((short)CityShields * (short)this.round.Value) + 8,
					(short)this.round.DrawVert + 8,
					(this.processor.PullGlobalSignedWord(0xd2f6) - (short)CityShields) * (short)this.round.Value,
					8, 15, 0);
			}

			// The corruption
			if (this.processor.PullGlobalWord(0xd2e0) != 0)
			{
				this.round.Value = (ushort)this.owner.Helpers.VerifyValueSpan(116 / ((short)CityTrade + 1), 1, 8);
				this.owner.VideoCard.SubstituteHue(ScreenBox,
					(((short)CityTrade - this.processor.PullGlobalSignedWord(0xd2e0)) * (short)this.round.Value) + 6,
					(short)this.round.DrawVert + 16,
					((short)this.round.Value * this.processor.PullGlobalSignedWord(0xd2e0)) + 2,
					8, 15, 0);
			}
		}

		/// <summary>
		/// The citizens along the top of the city screen (0xf4 what RenderCitizenIcons gives back, 0xe8 the width by the size) and
		/// the trade routes (0xbc the route, 0x4c the city at its end): the name and the trade of each, a half of it with a city
		/// of another owner (an eighth of the trade of both, a sixteenth with an own city), written on the first tab (0x2496 0),
		/// a dot on the mini map (tab 2).
		/// </summary>
		private void RenderCitizensAndRoutes(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			FloodBoxWithMotif(8, 8, 200, 13);
			this.round.Measure = RenderCitizenIcons(townIdx, 8, 8, this.round.Specialists, 192);
			this.round.Value = (ushort)this.owner.Helpers.VerifyValueSpan((town.TrueExtent * 8) + 24, 0, 128);

			for (ushort route = 0; route < 3; route++)
			{
				this.round.Item = route;
				ushort partner = (byte)town.CommerceTownIndices[route];
				this.round.OtherCity = partner;

				if (partner == 0xff)
					continue;

				EmptyMessage()
					.City((short)partner)
					.Text(":+");
				City other = this.owner.World.Towns[partner];
				int shift = (ushort)other.ParticipantIdx != CityOwner ? 3 : 4;
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((short)(((short)town.RootCommerce + (short)other.RootCommerce + 4) >> shift), 10));
				this.owner.Libc.AppendText(0xba06, "} ");

				if (CityScreenTab == 2)
					RenderMiniMapDot((ushort)((short)other.Place.X), (ushort)((short)other.Place.Y), 10);

				if (CityScreenTab == 0)
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 98, ((short)this.round.Item * 6) + 179, 10);
			}

			this.round.Item = 3;
		}

		/// <summary>
		/// The city itself on the mini map (tab 2) and the pollution on the first tab (0x2496 0): 0xe0 how much (the shields over
		/// the divisor 0x6c98 less 20, a quarter of the size by 0xb882 more), 0xfc the step, 0xea the icon (0xb2ba).
		/// </summary>
		private void RenderCityDotAndPollution(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if (CityScreenTab == 2)
			{
				City shown = this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)];
				RenderMiniMapDot((ushort)((short)shown.Place.X), (ushort)((short)shown.Place.Y), 15);
			}

			if (CityScreenTab != 0)
				return;

			short pollution = CountPollution(town);

			this.round.DrawVert = (ushort)this.owner.Helpers.VerifyValueSpan(100 / (short)this.owner.Helpers.VerifyValueSpan(pollution, 1, 99), 1, 8);

			ushort icon = 0;

			for (; (short)icon < (short)this.round.Pollution; icon++)
			{
				this.round.Index = icon;
				this.owner.Services.RenderPictureToScreen(ScreenBox,
					((short)this.round.Index * (short)this.round.DrawVert) + 98,
					(this.round.Index & 1) + 161,
					this.processor.PullGlobalWord(0xb2ba));
			}

			this.round.Index = icon;
		}

		/// <summary>
		/// The buttons Exit and Buy of the city screen of the human, and whether its Change button blinks (0xc0): the city makes
		/// an improvement it has or a part of the spaceship that was launched, a wonder that is built, or a unit when the owner
		/// has 127 of them.
		/// </summary>
		private void RenderExitAndBuyButtons(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			RenderLabelBox(284, 190, 316, 198, 0x2662, 12);
			RenderLabelBox(231, 190, 272, 198, 0x2667, 9);
			this.round.ChangeBlinks = 0;

			sbyte making = town.PresentMakingIdx;

			// An improvement the city has (-1 - -21)
			if (making < 0 && making >= -21)
			{
				if (town.Has((CityStructure)(-making - 1)))
					this.round.ChangeBlinks = 1;
			}

			// A part of the spaceship (-22 - -24) when the spaceship is on its way
			if (making < -21 && making >= -24 && ((ushort)(1 << (owner & 0xff)) & (ushort)this.owner.World.StarshipMarkers) != 0)
				this.round.ChangeBlinks = 1;

			// A wonder that is built
			if (making < -24 && this.owner.World.MarvelTownIdx[Math.Abs(making) - 24] != -1)
				this.round.ChangeBlinks = 1;

			// A unit when there are 127 of them
			if (making >= 0 && this.owner.World.Participants[owner].TroopTally >= 127)
				this.round.ChangeBlinks = 1;
		}

		/// <summary>
		/// Waits until the mouse button is let go, then for a click, a key or the flag 0xb8; meanwhile a blinking Change button
		/// (0xc0, counted up) changes its colours every 10 ticks.
		/// </summary>
		private void AwaitCityScreenInput()
		{
			this.owner.Startup.DisplayMouseCursor();

			while (this.owner.GlobDb3a != 0)
				this.owner.Startup.ObtainMouseState();

			while (this.owner.GlobDb3a == 0)
			{
				if ((ushort)this.owner.Libc.KeyAvailable() != 0 || this.round.CursorOn != 0)
					break;

				this.owner.Startup.ObtainMouseState();

				if (this.round.ChangeBlinks == 0)
					continue;

				this.owner.Startup.ConcealMouseCursor();
				this.round.ChangeBlinks = (ushort)(this.round.ChangeBlinks + 1);

				if (((byte)this.round.ChangeBlinks & 1) != 0)
					this.owner.VideoCard.SubstituteHue(ScreenBox, 231, 106, 32, 8, 14, 9);
				else
					this.owner.VideoCard.SubstituteHue(ScreenBox, 231, 106, 32, 8, 9, 14);

				this.owner.Startup.DisplayMouseCursor();
				this.owner.TextPainter.AwaitTime(10);
			}
		}

		/// <summary>What the city screen does after a button</summary>
		private enum CityScreenStep
		{
			/// <summary>The button wasn't used: the buttons after it</summary>
			Others,
			/// <summary>The city screen again from the citizens (the advisor switched, another tab)</summary>
			Redraw,
			/// <summary>The city from its start (the advisor gave up with 99, a purchase, a change, the view of the city)</summary>
			Restart,
			/// <summary>No click at all: the end of the city screen</summary>
			Leave,
			/// <summary>A line of the list clicked: selling the improvement</summary>
			Sell,
		}

		/// <summary>
		/// The input of the city screen (0xf0: the key, -1 for a click, 0x70 with the flag 0xb8) and the Change button (a click
		/// on it or A): the advisor flag 0x10 switched; switched on, the advisor picks the production (0x102), the units in making
		/// of the owner are counted over again. 0x70dc and 0x70de get back the words 0x10a and 0xa of the frame.
		/// </summary>
		private CityScreenStep ReadCityScreenInput(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.round.Key = 0xffff;
			if ((ushort)this.owner.Libc.KeyAvailable() != 0)
			{
				this.round.Key = this.owner.Dialogs.ObtainNavigationKey();
				this.processor.RegAcc.Value16 = 0;
				this.owner.GlobDb3e = 0;
				this.owner.GlobDb3c = 0;
			}
			else if (this.round.CursorOn != 0)
			{
				this.round.Key = 0x70;
			}

			this.owner.Startup.ConcealMouseCursor();

			bool clicked = this.owner.GlobDb3a == 2 && (short)this.owner.GlobDb3c >= 0xe6 && (short)this.owner.GlobDb3c < 0x10e &&
				(short)this.owner.GlobDb3e >= 0x6a && (short)this.owner.GlobDb3e <= 0x74;

			if (!clicked && this.round.Key != 0x41)
				return CityScreenStep.Others;

			town.ConditionMarker ^= 0x10;

			if ((town.ConditionMarker & 0x10) != 0)
			{
				short owner = (short)CityOwner;

				this.round.AdvisorPick = this.owner.Advisors.PickCityProduction(owner, townIdx);
				this.processor.RegBase.Value16 = ScreenBox;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 2);

				if (this.round.AdvisorPick == 0x63)
					return CityScreenStep.Restart;

				this.processor.EmitGlobalWord(0xb1e8, 0);

				StopMakingUnit(town, owner, false);
				town.PresentMakingIdx = (sbyte)(byte)this.round.AdvisorPick;
				StartMakingUnit(town, owner, false);

				this.owner.Services.FloodBox(ScreenBox, 230, 99, 90, 100, 0);
			}

			RestoreShieldsAndTrade();

			return CityScreenStep.Redraw;
		}

		/// <summary>
		/// The other buttons of the city screen (0xf0 the key, -1 for a click; a click only in the row of the buttons, 0x6a -
		/// 0x74): Buy (right of 0x128 or B), Change (0xe6 - 0x10e or C), the tabs (I, H, M, V or a click at 0x60 - 0xe0, 0x2496;
		/// the view of the city (3) is shown at once). Others: none of them.
		/// </summary>
		private CityScreenStep HandleCityScreenButtons(short townIdx)
		{
			ushort key = this.round.Key;
			short horiz = (short)this.owner.GlobDb3c;
			short vert = (short)this.owner.GlobDb3e;

			if (key == 0xffff && !(this.owner.GlobDb3a == 1 && vert >= 0x6a && vert <= 0x74))
				return CityScreenStep.Others;

			if (horiz >= 0x128 || key == 0x62)
			{
				BuyCityProduction(townIdx);
				return CityScreenStep.Restart;
			}

			if ((horiz >= 0xe6 && horiz < 0x10e) || key == 0x63)
			{
				ChangeCityProduction(townIdx);
				return CityScreenStep.Restart;
			}

			ushort tab;

			if (key == 0x69)
				tab = 0;
			else if (key == 0x68)
				tab = 1;
			else if (key == 0x6d)
				tab = 2;
			else if (key == 0x76)
				tab = 3;
			else if (horiz >= 0x60 && horiz < 0xe0)
				tab = (ushort)((horiz - 0x60) >> 5);
			else
				return CityScreenStep.Others;

			CityScreenTab = tab;
			RestoreShieldsAndTrade();

			if (CityScreenTab != 3)
				return CityScreenStep.Redraw;

			// The view of the city
			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 1);
			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.CityScene.DisplayCityScreen(townIdx, -1);
			this.processor.EmitGlobalWord(0x6b64, 1);
			CityScreenTab = 0;

			return CityScreenStep.Restart;
		}

		/// <summary>
		/// Buy: 0x106 the price, 0xe8 the cost of the shields missing (0 - 999 of them): a unit their double and a twentieth
		/// of their square, an improvement their double, a wonder (or a spaceship part) four times them; twice that with no
		/// shields. A menu with the cost and the treasury (in civil disorder a unit can't be bought); Yes buys it.
		/// </summary>
		private void BuyCityProduction(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			sbyte making = town.PresentMakingIdx;

			if (making >= 0)
			{
				this.round.Selected = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((making * 0x22) + 0x1142));
				short missing = (short)this.owner.Helpers.VerifyValueSpan((10 * (short)this.round.Selected) - town.MaterialsTally, 0, 999);
				this.round.Value = (ushort)((short)(missing * missing) / 20 + (missing * 2));
			}
			else
			{
				this.round.Selected = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((-making * 0x1e) + 0xbb2));
				this.round.Value = (ushort)((ushort)this.owner.Helpers.VerifyValueSpan((10 * (short)this.round.Selected) - town.MaterialsTally, 0, 999) << 1);

				if (-making > 0x15)
					this.round.Value = (ushort)(this.round.Value << 1);
			}

			if (town.MaterialsTally == 0)
				this.round.Value = (ushort)(this.round.Value << 1);

			short cost = (short)this.round.Value;

			Message("Cost to complete\n")
				.Text((ushort)(making >= 0 ? (making * 0x22) + 0x112a : (-making * 0x1e) + 0xb9a))
				.Text(": $")
				.Number(cost)
				.Text("\nTreasury: $")
				.Number(this.owner.World.Participants[owner].Gold)
				.Text("\n");

			if (making >= 0 && (town.ConditionMarker & 1) != 0)
				this.owner.Libc.AppendText(0xba06, "CIVIL DISORDER\n");
			else if (this.owner.World.Participants[owner].Gold >= cost)
				this.owner.Libc.AppendText(0xba06, " Yes\n No\n");

			this.owner.Startup.DisplayMouseCursor();
			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 1);
			if (this.owner.Dialogs.DisplayMenu(0xba06, 100, 80, 1) == 0 && this.owner.World.Participants[owner].Gold >= (short)this.round.Value)
			{
				town.MaterialsTally = (short)(10 * (short)this.round.Selected);
				this.owner.World.Participants[owner].Gold -= (short)this.round.Value;
			}

			this.owner.Startup.ConcealMouseCursor();
		}

		/// <summary>Change: the advisor flag 0x10 off, the human picks the production, the units in making counted over again</summary>
		private void ChangeCityProduction(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			town.ConditionMarker &= 0xef;
			this.owner.Startup.DisplayMouseCursor();
			RepickCityProduction(townIdx);
			this.owner.Startup.ConcealMouseCursor();
		}

		/// <summary>The unit of the list of the units in the city (the words before the frame at -0x74)</summary>
		private ushort UnitOfCityList(ushort place) =>
			this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + (place << 1) - 0x74));

		/// <summary>
		/// A: a cursor on the units in the city (0x4e of them, six in a row; 0x106 the unit, from the first that sleeps or
		/// is fortified or busy; 0xfa / 0x100 its place) moved by the arrows; Enter or Space wakes the unit (a sleeping or
		/// fortified one gets its moves back) and clears its goal; Escape ends it (the city screen again).
		/// </summary>
		private void ActivateUnitsInCity()
		{
			short owner = (short)CityOwner;
			ushort Unit(ushort place) => UnitMemory.Address(owner, UnitOfCityList(place));

			this.round.Selected = 0;

			while ((this.processor.PullUInt8(this.processor.SegData.Value16, Unit(this.round.Selected)) & 0xcf) == 0 &&
				(short)this.round.Selected < (short)this.round.UnitsDrawn)
			{
				this.round.Selected = (ushort)(this.round.Selected + 1);
			}

			if (this.round.Selected == this.round.UnitsDrawn)
				this.round.Selected = 0;

			while (true)
			{
				short place = (short)this.round.Selected;
				this.round.UnitHoriz = (ushort)(((place % 6) * 18) + 100);
				this.round.UnitVert = (ushort)(((place / 6) << 4) + 0x74);

				do
				{
					FloodBoxWithMotif((short)this.round.UnitHoriz, (short)this.round.UnitVert, 16, 16);
					this.owner.TextPainter.AwaitTime(10);
					this.owner.MapOps.RenderUnitSprite(owner, (short)UnitOfCityList(this.round.Selected),
						(short)this.round.UnitHoriz, (short)this.round.UnitVert);
					this.owner.TextPainter.AwaitTime(10);
				}
				while ((ushort)this.owner.Libc.KeyAvailable() == 0);

				ushort key = this.owner.Dialogs.ObtainNavigationKey();
				this.round.DrawVert = key;
				ushort current = this.round.Selected;
				this.round.Index = key switch
				{
					0x4800 => (ushort)(current - 6),
					0x4b00 => (ushort)(current - 1),
					0x4d00 => (ushort)(current + 1),
					0x5000 => (ushort)(current + 6),
					_ => current,
				};

				this.round.Selected = (ushort)this.owner.Helpers.VerifyValueSpan((short)this.round.Index, 0, (short)this.round.UnitsDrawn - 1);

				if (key == 0xd || key == 0x20)
					WakeUnitOfCity(Unit(this.round.Selected));

				if (key == 0x1b)
					break;
			}

			RestoreShieldsAndTrade();
		}

		/// <summary>
		/// S (when nothing was sold this turn, the flag 0x80): the improvement 0x106 of the list (0xb4 of them, from the line
		/// 0x40) blinks; the arrows up and down move it (another key keeps 0xea as it was), Enter or Space sells it, Escape
		/// gives up. True: sell it.
		/// </summary>
		private bool PickImprovementToSell(short townIdx)
		{
			if (this.round.Key != 0x73 || (this.owner.World.Towns[townIdx].ConditionMarker & 0x80) != 0)
				return false;

			this.round.Selected = 0;

			while (true)
			{
				do
				{
					short line = (short)((((short)this.round.Selected + (short)this.round.WonderRows) * 6) + 3);
					this.owner.VideoCard.SubstituteHue(ScreenBox, 252, line, 56, 7, 9, 1);
					this.owner.TextPainter.AwaitTime(10);
					this.owner.VideoCard.SubstituteHue(ScreenBox, 252, line, 56, 7, 1, 9);
					this.owner.TextPainter.AwaitTime(10);
				}
				while ((ushort)this.owner.Libc.KeyAvailable() == 0);

				ushort key = this.owner.Dialogs.ObtainNavigationKey();
				this.round.DrawVert = key;

				if (key == 0x4800)
					this.round.Index = (ushort)(this.round.Selected - 1);
				else if (key == 0x5000)
					this.round.Index = (ushort)(this.round.Selected + 1);

				this.round.Selected = (ushort)this.owner.Helpers.VerifyValueSpan((short)this.round.Index, 0, (short)this.round.ImprovementCount - 1);

				if (key == 0xd || key == 0x20)
					return true;

				if (key == 0x1b)
					return false;
			}
		}

		/// <summary>
		/// The keys 1 - 9: the specialist 0xea (of the 0x8) becomes the next type (entertainer, taxman, scientist, again);
		/// a city smaller than five gets a message instead (and starts again), a key past the specialists starts it again.
		/// </summary>
		private CityScreenStep ToggleSpecialist(short townIdx)
		{
			short key = (short)this.round.Key;

			if (key < 0x31 || key > 0x39)
				return CityScreenStep.Others;

			this.round.Index = (ushort)(key - 0x31);

			if ((short)this.round.Index >= (short)this.round.Specialists)
				return CityScreenStep.Restart;

			if (this.owner.World.Towns[townIdx].TrueExtent < 5)
				return RefuseSpecialists();

			return CycleSpecialist();
		}

		/// <summary>
		/// P (or the flag 0xb8 of the map cursor, set here with the cursor in the middle): a box blinks on the square 0xb6 /
		/// 0xbe of the map of the city; the eight directions move it within the city's area (no more than two squares each way,
		/// not a corner) onto squares the human has seen, else it stays (0xf6 / 0xfc the old place, 0xe8 the key). True:
		/// Enter or Space picked the square (0xf6 / 0xfc); false: Escape (the flag cleared).
		/// </summary>
		private bool MoveCityMapCursor(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if (this.round.CursorOn == 0)
			{
				this.processor.RegAcc.Value16 = 0;
				this.round.CursorVert = 0;
				this.round.CursorHoriz = 0;
				this.round.CursorOn = 1;
			}

			while (true)
			{
				do
				{
					ushort horiz = (ushort)((this.round.CursorHoriz << 4) + 0xa0);
					ushort vert = (ushort)((this.round.CursorVert << 4) + 0x38);

					this.owner.VideoCard.RenderImage(ScreenBox, horiz, vert, 16, 16, BackScreenBox, 0, 0);
					this.owner.Dialogs.RenderBox((short)this.round.CursorHoriz * 16 + 160, (short)this.round.CursorVert * 16 + 56, 15, 15, 15);
					this.owner.TextPainter.AwaitTime(10);
					this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 16, 16, ScreenBox, horiz, vert);
					this.owner.TextPainter.AwaitTime(10);
					this.processor.EmitGlobalWord(0x6b64, 1);
				}
				while ((ushort)this.owner.Libc.KeyAvailable() == 0);

				this.round.DrawHoriz = this.round.CursorHoriz;
				this.processor.RegAcc.Value16 = this.round.CursorVert;
				this.round.DrawVert = this.processor.RegAcc.Value16;
				ushort key = this.owner.Dialogs.ObtainNavigationKey();
				this.round.Value = key;

				// The keys of the number pad: 7 8 9 / 4 6 / 1 2 3
				(int horiz, int vert) step = key switch
				{
					0x4700 => (-1, -1),
					0x4800 => (0, -1),
					0x4900 => (1, -1),
					0x4b00 => (-1, 0),
					0x4d00 => (1, 0),
					0x4f00 => (-1, 1),
					0x5000 => (0, 1),
					0x5100 => (1, 1),
					_ => (0, 0),
				};

				this.round.CursorHoriz = (ushort)(this.round.CursorHoriz + step.horiz);
				this.round.CursorVert = (ushort)(this.round.CursorVert + step.vert);

				short across = (short)this.round.CursorHoriz;
				short down = (short)this.round.CursorVert;
				bool allowed = Math.Abs((int)across) <= 2 && Math.Abs((int)down) <= 2 && Math.Abs((int)down) + Math.Abs((int)across) < 4 &&
					(this.owner.World.ChartSight[(ushort)((short)town.Place.X + across), (ushort)((short)town.Place.Y + down)] &
						(short)(1 << (this.owner.World.PersonParticipantIdx & 0xff))) != 0;

				if (!allowed)
				{
					this.round.CursorHoriz = this.round.DrawHoriz;
					this.processor.RegAcc.Value16 = this.round.DrawVert;
					this.round.CursorVert = this.processor.RegAcc.Value16;
				}

				if (key == 0xd || key == 0x20)
				{
					this.round.DrawHoriz = this.round.CursorHoriz;
					this.processor.RegAcc.Value16 = this.round.CursorVert;
					this.round.DrawVert = this.processor.RegAcc.Value16;
					return true;
				}

				if (key == 0x1b)
				{
					this.round.CursorOn = 0;
					return false;
				}
			}
		}

		/// <summary>
		/// M or the arrow of the list (when the list has more, 0xca bit 2): the other page of the list (0xca bit 0, 0xda the
		/// first from 0xfe); R or the name of the city: renaming it; a click into the list: 0x106 the line clicked (Sell, from
		/// the line 0x40). Leave: no click at all.
		/// </summary>
		private CityScreenStep HandleListAndRename(short townIdx)
		{
			ushort key = this.round.Key;
			short horiz = (short)this.owner.GlobDb3c;
			short vert = (short)this.owner.GlobDb3e;
			bool click = this.owner.GlobDb3a == 1;

			// M: the other page of the list
			if (key == 0x4d && ((byte)this.round.ListFlags & 2) != 0)
				return TurnListPage();

			// R or the name of the city
			if (key == 0x72 || (click && horiz >= 0xe7 && vert > 0xbe && horiz < 0x10e))
			{
				this.owner.TurnLogic.AwaitForKey();
				this.processor.RegBase.Value16 = ScreenBox;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 1);
				this.owner.CityNaming.DisplayCityLabelPrompt(townIdx);
				this.processor.EmitGlobalWord(0x6b64, 1);
				return CityScreenStep.Restart;
			}

			if (!click)
				return CityScreenStep.Leave;

			// The arrow of the list
			if (horiz >= 0x120 && vert < 0x60 && vert > 0x58 && ((byte)this.round.ListFlags & 2) != 0)
				return TurnListPage();

			// A line of the list
			if (horiz >= 0x12c && vert < 0x5e)
			{
				this.round.Selected = (ushort)((short)(vert - 2) / 6 - this.round.WonderRows);
				return CityScreenStep.Sell;
			}

			return CityScreenStep.Others;
		}

		/// <summary>The other page of the list of improvements (0xca bit 0; 0xda the first one, from 0xfe)</summary>
		private CityScreenStep TurnListPage()
		{
			this.round.ListFlags = (ushort)(this.round.ListFlags ^ 1);
			this.processor.RegAcc.Value16 = this.round.ListEnd;
			this.round.ListStart = this.processor.RegAcc.Value16;
			RestoreShieldsAndTrade();
			return CityScreenStep.Redraw;
		}

		/// <summary>
		/// Selling the improvement 0x106 of the list (counted among those the city has from 0xda, 0xbc the improvement), when
		/// nothing was sold this turn: a menu with its price (ten times its upkeep word); Yes adds the money, removes it, marks
		/// the city (0x80) and for the improvement 8 redraws the map (0x6b64). True: the menu was shown.
		/// </summary>
		private bool SellImprovement(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			for (ushort improvement = this.round.ListStart; (short)improvement < 0x18; improvement++)
			{
				this.round.Item = improvement;
				uint bit = 1u << (byte)improvement;

				if ((town.Structures & bit) == 0)
					continue;

				ushort line = this.round.Selected;
				this.round.Selected = (ushort)(line - 1);

				if (line != 0 || (town.ConditionMarker & 0x80) != 0)
					continue;

				ushort price = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1e * improvement + 0xbd0));

				Message("Do you want to sell\nyour ")
					.Text((ushort)(0x1e * improvement + 0xbb8))
					.Text(" for ")
					.Number((short)(0xa * (short)price))
					.Text("$?\n No.\n Yes.\n");
				this.processor.RegBase.Value16 = ScreenBox;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 1);
				this.owner.Startup.DisplayMouseCursor();
				if (this.owner.Dialogs.DisplayMenu(0xba06, 128, 80, 1) == 1)
				{
					this.owner.World.Participants[CityOwner].Gold += (short)(0xa * price);
					town.Structures -= bit;
					town.ConditionMarker |= 0x80;

					if (improvement + 1 == 8)
						this.processor.EmitGlobalWord(0x6b64, 1);
				}

				this.owner.Startup.ConcealMouseCursor();
				return true;
			}

			this.round.Item = (ushort)Math.Max((short)this.round.ListStart, (short)0x18);
			return false;
		}

		/// <summary>The specialist 0xea becomes the next type (entertainer, taxman, scientist, again): the city screen again</summary>
		private CityScreenStep CycleSpecialist()
		{
			this.round.Value = ObtainCitizenType(this.round.Index);
			AssignCitizenType(this.round.Index,
				(ushort)((short)this.round.Value < 3 ? this.round.Value + 1 : 1));

			RestoreShieldsAndTrade();

			return CityScreenStep.Redraw;
		}

		/// <summary>The message that a city smaller than five can't have taxmen or scientists: the city from its start</summary>
		private CityScreenStep RefuseSpecialists()
		{
			this.owner.Libc.CopyText(0xba06, "A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.owner.Dialogs.DisplayMenu(0xba06, 32, 32, 1);

			return CityScreenStep.Restart;
		}

		/// <summary>
		/// A click on the citizens at the top (left of 0xc8, above 0x14): the specialist under it (0xea, by the width 0xf4 of
		/// an icon, after the 0x50 working ones) becomes the next type; elsewhere the city starts again.
		/// </summary>
		private CityScreenStep ClickCitizen(short townIdx)
		{
			if (this.owner.World.Towns[townIdx].TrueExtent < 5)
				return RefuseSpecialists();

			short width = (short)this.round.Measure;

			if (width == 0)
				throw new Exception("Division by zero");

			int place = (short)(this.owner.GlobDb3c - 0x10) / width;

			if (place != (short)place)
				throw new Exception("Division error");

			this.round.Index = (ushort)(place - (this.owner.World.Towns[townIdx].TrueExtent - (short)this.round.Workers));

			if ((short)this.round.Index < 0 || (short)this.round.Index >= (short)this.round.Specialists)
				return CityScreenStep.Restart;

			return CycleSpecialist();
		}

		/// <summary>A unit of the city woken: a sleeping or fortified one gets its moves back, its goal is cleared</summary>
		private void WakeUnitOfCity(ushort unit)
		{
			if ((this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(unit + UnitMemory.Status)) & 9) != 0)
			{
				sbyte kind = this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(unit + UnitMemory.Kind));
				ushort moves = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((kind * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.ShiftTally));
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(unit + UnitMemory.LeftoverSteps), (byte)(3 * moves));
			}

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(unit + UnitMemory.Status),
				(byte)(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(unit + UnitMemory.Status)) & 0x30));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
		}

		/// <summary>
		/// A click on the units in the city (right of 0x64, below 0x74; six in a row of 16): the unit 0x106 under it is woken
		/// and the city screen drawn again. False: no unit there.
		/// </summary>
		private bool ClickUnitOfCity()
		{
			short row = (short)((short)(this.owner.GlobDb3e - 0x74) / 16);
			short column = (short)((short)(this.owner.GlobDb3c - 0x64) / 16);
			this.round.Selected = (ushort)(row * 6 + column);

			if ((short)this.round.Selected >= (short)this.round.UnitsDrawn)
				return false;

			short owner = (short)CityOwner;
			WakeUnitOfCity(UnitMemory.Address(owner, UnitOfCityList(this.round.Selected)));

			RestoreShieldsAndTrade();

			return true;
		}

		/// <summary>
		/// A square of the map of the city picked (0xf6 / 0xfc from the middle, 0xea its number): the middle takes all the
		/// workers off (the game puts them back); a square the owner has seen gets a worker or loses one (a free square only:
		/// the word of the frame for it is 0; 0xae the workers gained, 0x50 the working, 0xe8b8 the specialists, and the
		/// food, shields and trade of a square left are taken back). Redraw: the city screen again; Restart: the city again.
		/// </summary>
		private CityScreenStep ToggleCityTile(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];

			this.round.Index = ObtainCityTileIndex((short)this.round.DrawHoriz, (short)this.round.DrawVert);
			ushort tile = this.round.Index;

			if (tile == 0xffff)
				return CityScreenStep.Restart;

			if (tile == 20)
			{
				// A click on the city square in the middle of the map: all citizens are taken off their tiles
				// (also entertainers, taxmen and scientists) and the game's own assignment puts them back on the best tiles,
				// first enough food so the city doesn't starve, then the best mix of food, shields and trade.
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
				CitySpecialists = 0;
				return CityScreenStep.Restart;
			}

			if ((short)tile >= 0x14)
				return CityScreenStep.Restart;

			int horiz = town.Place.X + this.owner.TownDeltas[tile].X;
			int vert = town.Place.Y + this.owner.TownDeltas[tile].Y;

			// SI and DI as the original leaves them (the menus keep them on the stack)
			this.processor.RegSource.Value16 = (ushort)(tile << 1);
			this.processor.RegDest.Value16 = (ushort)(townIdx * 0x1c);

			if ((this.owner.World.ChartSight[(ushort)(short)horiz, (ushort)(short)vert] & (short)(1 << (byte)CityOwner)) == 0)
				return CityScreenStep.Restart;

			RestoreShieldsAndTrade();

			uint bit = 1u << (byte)tile;
			uint Workers() => ((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0;

			// A square without a worker must be free
			if ((Workers() & bit) == 0)
			{
				ushort place = (ushort)(short)((10 * this.owner.TownDeltas[tile].X) + (this.owner.TownDeltas[tile].Y * 2));
				this.processor.RegDest.Value16 = place;

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + place - 0x8e)) != 0)
					return CityScreenStep.Redraw;
			}

			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			uint workers = Workers() ^ bit;
			town.LaborerMarkers0 = (ushort)workers;
			town.LaborerMarkers1 = (ushort)(workers >> 16);

			if ((Workers() & bit) != 0)
			{
				// A worker more, a specialist less
				this.round.WorkersGained = (ushort)(this.round.WorkersGained + 1);
				RenderCityTile(townIdx, tile, marker);

				if (CitySpecialists != 0)
				{
					CitySpecialists = (ushort)(CitySpecialists - 1);
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}
			}
			else
			{
				// A worker less: what the square gave is taken back
				this.round.WorkersGained = (ushort)(this.round.WorkersGained - 1);

				for (ushort resource = 0; resource < 3; resource++)
				{
					this.round.Item = resource;
					this.processor.RegSource.Value16 = (ushort)(tile << 1);
					this.round.Amount = MayCityWorkTile(town.Place.X + this.owner.TownDeltas[tile].X, town.Place.Y + this.owner.TownDeltas[tile].Y, resource);
					ushort word = (ushort)((resource << 1) + 0x70da);
					this.processor.EmitUInt16(this.processor.SegData.Value16, word,
						(ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, word) - this.round.Amount));
				}

				this.round.Item = 3;
				this.processor.RegSource.Value16 = (ushort)(tile << 1);
				this.owner.MapOps.RenderMapTile(town.Place.X + this.owner.TownDeltas[tile].X, town.Place.Y + this.owner.TownDeltas[tile].Y);
				CitySpecialists = (ushort)(CitySpecialists + 1);
				this.round.Workers = (ushort)(this.round.Workers + 1);
			}

			// The specialists in the top six bits
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			uint counted = (uint)((short)this.round.Workers << 26) + (Workers() & 0x3ffffff);
			town.LaborerMarkers0 = (ushort)counted;
			town.LaborerMarkers1 = (ushort)(counted >> 16);

			return (short)this.round.WorkersGained > 0 ? CityScreenStep.Restart : CityScreenStep.Redraw;
		}

		/// <summary>
		/// The two special slots of the city (Unfamiliar, 0xff empty) counted in 0xdeb8 (0xea the slot); 0xd2f6 how many
		/// more of them there are than citizens. SI, BX and CX as the original leaves them.
		/// </summary>
		private void CountSpecialSlots(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			for (ushort slot = 0; slot < 2; slot++)
			{
				this.round.Index = slot;
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.RegBase.Value16 = slot;

				if ((byte)town.Unfamiliar[slot] != 0xff)
					this.processor.EmitGlobalWord(0xdeb8, (ushort)(this.processor.PullGlobalWord(0xdeb8) + 1));
			}

			this.round.Index = 2;
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.TrueExtent < (short)this.processor.PullGlobalWord(0xdeb8))
			{
				this.processor.RegCount.Value16 = (ushort)(this.processor.PullGlobalWord(0xdeb8) - town.TrueExtent);
				this.processor.EmitGlobalWord(0xd2f6, this.processor.RegCount.Value16);
			}
		}

		/// <summary>
		/// The shields and the trade of the city back as they were before the factories and the power (0x70dc / 0x70de from
		/// the words 0x10a / 0xa of the frame) before the city screen counts them again; AX the trade
		/// </summary>
		private void RestoreShieldsAndTrade()
		{
			CityShields = this.round.ShieldsKept;
			CityTrade = this.round.TradeKept;
			this.processor.RegAcc.Value16 = this.round.TradeKept;
		}

		/// <summary>
		/// The city stops making its unit (the owner's count of units in making); SI (and with the base flag BX) as the
		/// original leaves them, the menus keep them on the stack
		/// </summary>
		private void StopMakingUnit(City town, short owner, bool withBase)
		{
			if (town.PresentMakingIdx >= 0)
			{
				this.processor.RegSource.Value16 = (ushort)(town.PresentMakingIdx << 1);

				if (withBase)
					this.processor.RegBase.Value16 = (ushort)(owner * 0x38);
			}

			this.owner.World.Participants[owner].StopMaking(town.PresentMakingIdx);
		}

		/// <summary>The city starts making its unit (the owner's count of units in making); SI and BX as in StopMakingUnit</summary>
		private void StartMakingUnit(City town, short owner, bool withBase)
		{
			if (town.PresentMakingIdx >= 0)
			{
				this.processor.RegSource.Value16 = (ushort)(town.PresentMakingIdx << 1);

				if (withBase)
					this.processor.RegBase.Value16 = (ushort)(owner * 0x38);
			}

			this.owner.World.Participants[owner].StartMaking(town.PresentMakingIdx);
		}

		/// <summary>The advisor picks the production of the city; the units in making of the owner counted over again</summary>
		private void RepickCityProduction(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			StopMakingUnit(town, owner, false);
			this.processor.RegCount.Value16 = this.owner.Advisors.PickCityProduction(owner, townIdx);
			town.PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;
			StartMakingUnit(town, owner, false);
		}

		/// <summary>
		/// The order of the city at the end of its turn (0xe8 the happy less the unhappy, 0x70e2 - 0x70e4): fewer happy than
		/// unhappy is civil disorder (the flag 1; a human hears of it – with the riot of the city view when the option 8 is on,
		/// the first time – the computer picks another production), under democracy a disorder that goes on overthrows the
		/// government; otherwise a disorder ends ("Order restored") and the taxes 0xe17a are paid, but not in anarchy.
		/// </summary>
		private void ApplyCivilOrder(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			bool human = this.owner.World.IsHuman(owner);

			this.round.Value = (ushort)(CityHappy - CityUnhappy);

			if ((short)this.round.Value < 0)
			{
				if (human)
				{
					Message("Civil Disorder in\n")
						.City(townIdx)
						.Text("! Mayor\nflees in panic.\n");
					this.owner.Services.PerformSound(0x24, 0);

					if ((this.owner.World.SessionPrefMarkers & 8) != 0 && (town.ConditionMarker & 1) == 0 && this.processor.PullGlobalWord(0x6b92) == 0)
					{
						// The riot in the view of the city, once
						this.owner.CityScene.DisplayCityScreen(townIdx, -2);
						this.owner.CityScene.RiotAnimation();
						this.owner.Startup.QueryForDisk(0, 1);
						this.owner.TurnHooks.RepaintMainScreen();
						this.processor.EmitGlobalWord(0x6b92, 1);
					}
					else
					{
						DialogStyle = 4;
						this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
					}

					this.owner.Services.PerformSound(1, 0);

					if ((town.ConditionMarker & 1) == 0)
					{
						this.processor.EmitGlobalWord(0xb1e8, 1);

						if ((this.owner.World.SessionPrefMarkers & 1) != 0)
							this.owner.HelpSystem.DisplayHelpPopup(0x2708);
					}
				}

				if ((town.ConditionMarker & 1) == 0)
				{
					town.ConditionMarker |= 1;

					if (!human)
						RepickCityProduction(townIdx);

					this.owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
				}
				else if (this.owner.World.Participants[owner].RegimeKind == 5)
				{
					// The disorder goes on under democracy
					this.owner.Services.PerformSound(0x24, 0);
					Message("Discontented citizens of\n")
						.City(townIdx)
						.Text(" revolt:\nGovernment Collapses!\n");

					if (owner == this.owner.World.PersonParticipantIdx)
					{
						this.owner.Paper.DisplayNewspaper(townIdx);
						this.owner.World.Participants[owner].Relations[0] |= 4;
					}

					this.owner.Politics.SwitchGovernment(owner, 0);
					this.owner.Services.PerformSound(1, 0);
				}

				return;
			}

			if ((town.ConditionMarker & 1) != 0)
			{
				town.ConditionMarker &= 0xfe;

				if (human)
				{
					DialogStyle = 4;
					Message("Order restored\nin ")
						.City(townIdx)
						.Text(".\n")
						.Dialog(100, 80);
				}
				else
				{
					RepickCityProduction(townIdx);
				}

				this.owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			}

			// The taxes, but not in anarchy
			if (this.owner.World.Participants[owner].RegimeKind != 0)
				this.owner.World.Participants[owner].Gold += (short)this.processor.PullGlobalWord(0xe17a);
		}

		/// <summary>
		/// The celebration of the city ("We love the ... day", the flag 0x40): no unhappy citizens (0x70e4), bigger than 2, at
		/// least half of the citizens happy (0x70e2), not in anarchy. It starts with a message for a human (the city view with
		/// the option 8, else the newspaper); going on under republic or democracy the city grows while it has food to spare
		/// (from 10 only with an aqueduct, 0x100); it ends (without disorder, 0xe8 not below 0) with "celebration canceled".
		/// </summary>
		private void ApplyCelebration(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];
			bool human = this.owner.World.IsHuman(owner);

			bool celebrates = CityUnhappy == 0 && town.TrueExtent > 2 &&
				(short)((town.TrueExtent + 1) / 2) <= (short)CityHappy && player.RegimeKind != 0;

			if (celebrates)
			{
				if ((town.ConditionMarker & 0x40) == 0)
				{
					// The celebration starts
					if (human && player.RegimeKind != 0)
					{
						Message("'We love the ")
							.Text(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((player.RegimeKind << 1) + 0x19b2)))
							.Text("'\nday celebrated in\n")
							.City(townIdx)
							.Text("!\n");
						this.owner.Services.PerformSound(0x22, 0);

						if ((this.owner.World.SessionPrefMarkers & 8) != 0)
						{
							this.owner.CityScene.DisplayCityScreen(townIdx, -2);
							this.owner.CityScene.RenderCitySceneryForHuman();
							this.owner.Startup.QueryForDisk(0, 1);
							this.owner.TurnHooks.RepaintMainScreen();
						}
						else
						{
							this.owner.Paper.DisplayNewspaper(townIdx);
						}

						this.owner.Services.PerformSound(1, 0);
					}

					town.ConditionMarker |= 0x40;
				}
				else if (player.RegimeKind >= 4)
				{
					// Growth while it goes on under republic or democracy, with food to spare
					short eaten = (short)((town.TrueExtent * 2) + ((short)this.round.SettlerFood * this.processor.PullGlobalSignedWord(0xe3c6)));

					if (eaten < (short)CityFood && (town.TrueExtent < 10 || town.Has(CityStructure.Aqueduct)))
						town.TrueExtent++;
				}

				return;
			}

			if ((short)this.round.Value < 0 || (town.ConditionMarker & 0x40) == 0)
				return;

			// The celebration ends
			if (human && player.RegimeKind != 0)
			{
				this.owner.Services.PerformSound(0x23, 0);
				Message("'We love the ")
					.Text(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((player.RegimeKind << 1) + 0x19b2)))
					.Text("'\ncelebration canceled\nin ")
					.City(townIdx)
					.Text(".\n");
				this.owner.Paper.DisplayNewspaper(townIdx);
				this.owner.Services.PerformSound(1, 0);
			}

			town.ConditionMarker &= 0xbf;
		}

		/// <summary>
		/// The science of the city (0xe8, from 0x70e6; none in anarchy) goes to the research of its owner (InquiryHeadway):
		/// for the human a finished or empty goal first starts a new one (with the flag 8 of DiagMarkers the advance is
		/// uncovered), on the easiest level the science counts twice for an advance others found, the wonder 0x14 adds half.
		/// The cost (0xf4: 14 less the level for the computer, 6 and twice the level for the human, more 0xd2de; for the human
		/// at least 11 less its advances) by the advances found; past it the research starts again (the advance uncovered with
		/// the flag 8), not for the human while 0xb1e8 is set.
		/// </summary>
		private void ApplyResearch()
		{
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;
			Player player = this.owner.World.Participants[owner];
			Player human = this.owner.World.Participants[person];

			this.round.Value = CityScience;

			if (owner == person)
			{
				bool finished = human.PresentInquiryIdx != -1 ?
					this.owner.Advisors.PlayerKnowsTechnology(owner, human.PresentInquiryIdx) != 0 :
					player.InquiryHeadway != 0;

				if (finished)
				{
					human.PresentInquiryIdx = -1;
					UncoverTechnologyWhenFlagged(owner);
					player.InquiryHeadway = 0;
				}
			}

			if (player.RegimeKind == 0)
				this.round.Value = 0;

			player.InquiryHeadway += (short)this.round.Value;

			// On the easiest level an advance others found costs half
			if (owner == person && this.owner.World.HardnessTier == 0 && human.PresentInquiryIdx >= 0 &&
				(this.owner.World.ScienceLeadingFoundVia[human.PresentInquiryIdx] & 7) != 0)
			{
				player.InquiryHeadway += (short)this.round.Value;
			}

			if (PlayerEnjoysWonderEffect(owner, 0x14) != 0)
				player.InquiryHeadway += (short)((short)this.round.Value / 2);

			// The cost of an advance
			this.round.Measure = (ushort)(owner != person ? 14 - this.owner.World.HardnessTier : (this.owner.World.HardnessTier << 1) + 6);
			this.round.Measure = (ushort)(this.round.Measure + TechnologyLevel);

			if (owner == person && (short)(11 - player.FoundScienceTally) > (short)this.round.Measure)
				this.round.Measure = (ushort)(11 - player.FoundScienceTally);

			if (owner == person && this.processor.PullGlobalWord(0xb1e8) != 0)
				return;

			short progress = (short)(player.InquiryHeadway / (this.owner.World.Season < 0 ? 1 : 2));

			if (progress > (short)(player.FoundScienceTally * (short)this.round.Measure))
			{
				player.InquiryHeadway = 0;
				UncoverTechnologyWhenFlagged(owner);
			}
		}

		/// <summary>With the flag 8 of DiagMarkers the player uncovers an advance (the mouse shown meanwhile)</summary>
		private void UncoverTechnologyWhenFlagged(short participant)
		{
			if ((this.owner.World.DiagMarkers & 8) == 0)
				return;

			this.owner.Startup.DisplayMouseCursorFully();
			this.owner.Advisors.UncoverTechnology(participant, 0);
			this.owner.Startup.ConcealMouseCursorToSavedLevel();
		}

		/// <summary>
		/// The pollution of the city (0xe0): the shields over the divisor 0x6c98 less 20, a quarter of the size by the
		/// advances 0xb882 more (a quarter rounded towards zero; the original takes the absolute value, -32768 can't come out
		/// of the size)
		/// </summary>
		private short CountPollution(City town)
		{
			short divisor = this.processor.PullGlobalSignedWord(0x6c98);

			if (divisor == 0)
				throw new Exception("Division by zero");

			int quotient = (short)CityShields / divisor;

			if (quotient != (short)quotient)
				throw new Exception("Division error");

			short pollution = (short)((short)(quotient - 20) + ((short)(town.TrueExtent * (short)ContentAdvances) / 4));
			this.round.Pollution = (ushort)pollution;

			return pollution;
		}

		/// <summary>
		/// The end of the turn of a city: the score of the owner grows by the size and the happy less the unhappy. For the human
		/// only (false: not the human): the pollution may land on a square of the city (twice 0xe0 against a draw below 256 less
		/// half the advances by the level; 0xe8 the square, 0xc6 / 0xd2 its place: not polluted, not the sea, not the city);
		/// a city in disorder with a nuclear plant (0x10 of the second word) may melt down, one time in three, until the
		/// advance 0x42.
		/// </summary>
		private bool ApplyScoreAndPollution(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];
			short person = this.owner.World.PersonParticipantIdx;

			player.Tally += (short)(town.TrueExtent + CityHappy - CityUnhappy);

			if (owner != person)
				return false;

			// The pollution
			short pollution = CountPollution(town);
			ushort chance = (ushort)(0x100 - (short)((short)(player.FoundScienceTally * this.owner.World.HardnessTier) / 2));

			if ((short)(pollution << 1) > (short)(ushort)this.owner.Libc.Dice.DrawInt(chance))
			{
				this.round.Value = (ushort)this.owner.Libc.Dice.DrawInt(20);
				var delta = this.owner.TownDeltas[this.round.Value];
				this.round.TileHoriz = (ushort)(short)(delta.X + (short)this.round.Horiz);
				this.round.TileVert = (ushort)(short)(delta.Y + (short)this.round.Vert);
				short horiz = (short)this.round.TileHoriz;
				short vert = (short)this.round.TileVert;

				if ((this.owner.MapOps.ObtainTileImprovements(horiz, vert) & 0x40) == 0 && this.owner.MapOps.ObtainTerrainType(horiz, vert) != 0xa &&
					(this.owner.MapOps.ObtainTileImprovements(horiz, vert) & 1) == 0)
				{
					AttachSmog(horiz, vert);
					this.owner.MapOps.FocusMapView(person, horiz - 8, vert - 6);
					this.owner.MapOps.RepaintMapTile(horiz, vert);
					DialogStyle = 6;
					Message("Pollution near ")
						.City(townIdx)
						.Text(".\n")
						.Dialog(80, 64);
				}
			}

			// The nuclear plant melts down
			if ((town.ConditionMarker & 1) != 0 && town.Has(CityStructure.NuclearPlant) && this.owner.Libc.Dice.DrawInt(3) == 0 &&
				this.owner.Advisors.PlayerKnowsTechnology(owner, 0x42) == 0)
			{
				short horiz = (short)this.round.Horiz;
				short vert = (short)this.round.Vert;

				this.owner.MapOps.FocusMapView(person, horiz - 8, vert - 6);
				this.owner.Diplomats.RenderExplosion(horiz, vert);
				this.owner.Combat.EnactNuclearFallout(horiz, vert);
				town.Remove(CityStructure.NuclearPlant);
				this.owner.Services.PerformSound(0x24, 0);
				Message("Nuclear Catastrophe\nin ")
					.City(townIdx)
					.Text("!\nContamination feared!\n");
				this.owner.Paper.DisplayNewspaper(townIdx);
				this.owner.Services.PerformSound(1, 0);
			}

			return true;
		}

		/// <summary>
		/// The upkeep of the improvements of the human's city (0xbc the improvement 1 - 23, its word at 0xbd2; not in anarchy;
		/// the palace costs one more from the level 2 and from 4, and with each of the advances 0x22 and 0x25): when the money
		/// runs out the improvement is sold ("can't maintain", ten times its word at 0xbd0). SI as in the original.
		/// </summary>
		private void PayImprovementUpkeep(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];
			ushort improvement = 1;

			for (; (short)improvement < 0x18; improvement++)
			{
				this.round.Item = improvement;
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				uint bit = 1u << (byte)improvement;

				if ((town.Structures & bit) == 0 || player.RegimeKind == 0)
					continue;

				player.Gold -= (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(improvement * 0x1e + 0xbd2));

				if (improvement == 1)
				{
					if (this.owner.World.HardnessTier >= 2)
						player.Gold--;

					if (this.owner.World.HardnessTier >= 4)
						player.Gold--;

					if (this.owner.Advisors.PlayerKnowsTechnology(owner, 0x22) != 0)
						player.Gold--;

					if (this.owner.Advisors.PlayerKnowsTechnology(owner, 0x25) != 0)
						player.Gold--;
				}

				if (player.Gold >= 0)
					continue;

				// Sold for the debt
				player.Gold = 0;
				town.Structures &= ~bit;
				EmptyMessage()
					.City(townIdx)
					.Text("\ncan't maintain\n")
					.Text((ushort)(improvement * 0x1e + 0xbb8))
					.Text(".\n")
					.Style(4)
					.Dialog(100, 80);
				player.Gold += (short)(0xa * this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(improvement * 0x1e + 0xbd0)));
			}

			this.round.Item = improvement;
		}

		/// <summary>
		/// The very end of HandleCity: a city with the flag 4 and no foreign unit next to it keeps its list of units; the mouse
		/// shown again; AX the happy less the unhappy. SI as in the original.
		/// </summary>
		private void FinishHandleCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if ((town.ConditionMarker & 4) != 0)
			{
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				if (this.owner.UnitOps.ContainsAdjacentForeignUnit(town.ParticipantIdx, town.Place.X, town.Place.Y) == 0)
					this.owner.UnitOps.KeepUnitsInCityList(townIdx);
			}

			this.processor.RegBase.Value16 = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 1);
			this.owner.Startup.DisplayMouseCursor();
			this.processor.RegAcc.Value16 = (ushort)(CityHappy - CityUnhappy);
		}

		/// <summary>
		/// The start of HandleCity: the locals 0xb8, 0xda, 0xfe, 0xca and 0x2496 cleared, the mouse hidden, the 5 x 5 array
		/// of the tiles around (BP-0xa6) cleared; 0x653e the city; the materials outside 0 - 999 cleared. Over the other
		/// cities: 0xb2 the distance to the nearest one (at most 7), 0x6b30 the distance to the nearest palace of the owner
		/// (0 with an own palace, else at most 0x20), the tiles within two of the city worked by a city at most 5 away marked
		/// in the array. Then 0x6548 the owner, 0x6540 0x15, 0xc2 / 0xd0 the map view, 0x48 the waste (1 for despotism and
		/// anarchy, else 2), 0xb882 the advances that make citizens content (without the wonder 0x1000), 0x4a 10 the content
		/// citizens (for a computer player 16 - 2 x the level, 2 less on the levels above 1 for the leader of a later game),
		/// 0xe8b8 the specialists (bits 26 - 31 of the workers), for a computer player in the turn (marker 0) a city without
		/// the flag 1 has none, else all workers are taken off. The locals of HandleCity and SI, BX, CX are used as in the
		/// original.
		/// </summary>
		private void PrepareCity(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];

			CityScreenTab = 0;
			this.round.CursorOn = 0;
			this.processor.RegAcc.Value16 = 0;
			this.round.ListStart = 0;
			this.round.ListEnd = 0;
			this.round.ListFlags = 0;

			this.owner.Startup.ConcealMouseCursor();

			// The 5 x 5 array of the tiles around
			for (ushort column = 0; column < 5; column++)
			{
				this.round.Horiz = column;

				for (ushort row = 0; row < 5; row++)
				{
					this.round.Vert = row;
					this.processor.RegSource.Value16 = (ushort)(column * 0xa + row * 2);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa6), 0);
				}

				this.round.Vert = 5;
			}

			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			this.processor.EmitGlobalWord(0x653e, (ushort)townIdx);
			this.round.Distance = 7;
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitGlobalWord(0x6b30, (ushort)(town.Has(CityStructure.Palace) ? 0 : 0x20));

			if (town.MaterialsTally > 999 || town.MaterialsTally < 0)
				town.MaterialsTally = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
				City other = this.owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || otherIdx == (ushort)townIdx)
					continue;

				this.processor.RegSource.Value16 = (ushort)(otherIdx * 0x1c);

				short distance = (short)(ushort)this.owner.Helpers.ObtainDistance((short)this.round.Horiz, other.Place.X, (short)this.round.Vert, other.Place.Y);
				this.round.Target = (ushort)distance;

				if (distance < (short)this.round.Distance && distance != 0)
					this.round.Distance = (ushort)distance;

				if (other.ParticipantIdx == town.ParticipantIdx && other.Has(CityStructure.Palace) && distance < (short)this.processor.PullGlobalWord(0x6b30))
				{
					// The nearest palace of the owner
					this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
					this.processor.EmitGlobalWord(0x6b30, (ushort)distance);
				}

				if (distance > 5)
					continue;

				// The tiles near the city worked by the other city (and the other city itself, tile 20)
				for (ushort tile = 0; tile <= 20; tile++)
				{
					this.round.Index = tile;
					uint mask = 1u << tile;
					this.processor.RegCount.Value16 = (ushort)(mask & other.LaborerMarkers0);
					this.processor.RegBase.Value16 = (ushort)(((mask >> 16) & other.LaborerMarkers1) | this.processor.RegCount.Value16);
					this.processor.RegSource.Value16 = (ushort)(otherIdx * 0x1c);

					if (this.processor.RegBase.Value16 == 0 && tile != 20)
						continue;

					short horizLoc = (short)(other.Place.X + this.owner.TownDeltas[tile].X);
					short vertLoc = (short)(other.Place.Y + this.owner.TownDeltas[tile].Y);
					this.round.TileHoriz = (ushort)horizLoc;
					this.round.TileVert = (ushort)vertLoc;

					this.processor.RegAcc.Value16 = (ushort)(horizLoc - (short)this.round.Horiz);
					if ((short)(ushort)this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16) > 2)
						continue;

					this.processor.RegAcc.Value16 = (ushort)(vertLoc - (short)this.round.Vert);
					if ((short)(ushort)this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16) > 2)
						continue;

					this.processor.RegCount.Value16 = 0xa;
					this.processor.RegSource.Value16 = (ushort)((horizLoc - (short)this.round.Horiz) * 0xa +
						((vertLoc - (short)this.round.Vert) << 1));
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x8e), 1);
				}

				this.round.Index = 21;
			}

			this.round.OtherCity = 0x80;

			// The tile of the city itself
			this.round.CityTileNeighbour = 0;
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			CityOwner = (ushort)town.ParticipantIdx;
			CityTiles = 0x15;
			this.round.MapViewHoriz = this.processor.PullGlobalWord(0xd4cc);
			this.round.MapViewVert = this.processor.PullGlobalWord(0xd75e);

			short owner = (short)CityOwner;
			this.round.SettlerFood = (ushort)(this.owner.World.Participants[owner].RegimeKind <= 1 ? 1 : 2);

			// The advances that make citizens content, unless the wonder 0x1000 is in the city
			ContentAdvances = 0;

			if (!town.Has(CityStructure.MassTransit))
			{
				foreach (short advance in new short[] { 0x23, 0x3a, 0x29, 0x3c })
				{
					if (this.owner.Advisors.PlayerKnowsTechnology(owner, advance) != 0)
						ContentAdvances = (ushort)(ContentAdvances + 1);
				}
			}

			this.round.CostFactor = 0xa;
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			CitySpecialists = (ushort)((((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0) >> 0x1a);
			this.processor.RegData.Value16 = 0;
			this.processor.RegCount.LowByte = 0x1a;

			if (owner == this.owner.World.PersonParticipantIdx)
				return;

			// A computer player: the content citizens by the level
			this.round.CostFactor = (ushort)-(this.owner.World.HardnessTier * 2 - 0x10);

			if (this.owner.World.Season >= 0 &&
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing == 7 &&
				this.owner.World.HardnessTier > 1)
			{
				this.round.CostFactor = (ushort)(this.round.CostFactor - 2);
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);

			if (marker != 0)
				return;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.ConditionMarker & 1) == 0)
			{
				CitySpecialists = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
			}
		}

		/// <summary>
		/// The work of a city: marker -1 only counts (food, shields, trade, the citizens), 0 is the city's turn (growth,
		/// production, disorder, celebration, science, pollution, upkeep), 1 the city screen with its input. AX gives back
		/// the happy less the unhappy citizens. The frame of 0x10c bytes holds the locals of all the parts.
		/// </summary>
		public ushort HandleCity(short townIdx, short marker)
		{
			this.processor.Journal.OpenScope($"HandleCity({townIdx}, {marker})");

			this.processor.EnterFrame(0x10c);
			this.processor.PreserveIndexRegisters();

			// HandleCity calls itself (the advisor counts the city), each call has its own locals as its own frame
			CityRound outer = this.round;
			this.round = new CityRound(this.processor);

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (this.owner.World.Towns[townIdx].ConditionMarker != 0xff)
				RunCity(townIdx, marker);

			this.round = outer;

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("HandleCity");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// HandleCity on a city that exists: the round from the start (again after most buttons of the city screen), the
		/// economy (again from the citizens after the others), the city screen while it is open, then the end of the turn.
		/// </summary>
		private void RunCity(short townIdx, short marker)
		{
			// The neighbouring cities, the palace, the governments, the content citizens
			PrepareCity(townIdx, marker);

			bool fromStart = true;

			while (true)
			{
				if (fromStart && !StartCityRound(townIdx, marker))
					return;

				if (!CountCityEconomy(townIdx, marker))
					return;

				if (marker != 1)
					break;

				CityScreenStep step = RunCityScreen(townIdx, marker);

				if (step == CityScreenStep.Leave)
				{
					// The end of the city screen
					this.processor.EmitGlobalWord(0xd4cc, this.round.MapViewHoriz);
					this.processor.EmitGlobalWord(0xd75e, this.round.MapViewVert);
					break;
				}

				fromStart = step == CityScreenStep.Restart;
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);

			if (marker == 0)
				EndCityTurn(townIdx);

			FinishHandleCity(townIdx);
		}

		/// <summary>
		/// The start of a round of HandleCity: the place of the city (0xd8 / 0xe4), the title and the map on the city screen;
		/// in the turn the starvation and the growth (false: the city is gone); the units, the tiles it cannot work, the workers.
		/// </summary>
		private bool StartCityRound(short townIdx, short marker)
		{
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.round.Horiz = (ushort)(short)this.owner.World.Towns[townIdx].Place.X;
			this.round.Vert = (ushort)(short)this.owner.World.Towns[townIdx].Place.Y;
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)(marker < 0 ? 0xffff : 0);

			// The city screen: the title and the map of the city
			if (marker == 1)
				RenderCityHeaderAndMap(townIdx);

			// In the turn of the city: starvation and growth
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);

			if (marker == 0 && !FeedCity(townIdx))
				return false;

			// The units the city supports
			CountCityUnits(townIdx);

			// The tiles the city cannot work
			MarkUnavailableTiles(townIdx);

			// The workers the city keeps and the new ones, the specialists
			PickWorkers(townIdx, marker, KeepWorkers(townIdx, marker));

			return true;
		}

		/// <summary>
		/// The economy of the city from its citizens: the factories and the power, in the turn the production (false: the
		/// city is gone), the trade, the luxuries, taxes and science, the content and happy citizens, the units it supports
		/// (counted again after a disbanded one) and in the turn their costs.
		/// </summary>
		private bool CountCityEconomy(short townIdx, short marker)
		{
			// The citizen icons, the factories and the power
			PrepareCitizensAndFactories(townIdx, marker);

			if (marker == 0 && !ProduceInTurn(townIdx))
				return false;

			// The city screen: the frames and the tabs
			if (marker == 1)
				RenderCityScreenFrames();

			// The corruption and the trade routes
			CountCorruptionAndTradeRoutes(townIdx);

			// The trade split into luxuries, taxes and science; the science buildings; the content citizens
			SplitTrade(townIdx);
			CountScienceAndContent(townIdx);

			// The luxuries (and the citizens on the city screen)
			ApplyLuxuries(townIdx, marker);

			// The improvements that make citizens content
			ApplyContentImprovements(townIdx, marker);

			// The military and the citizens
			ApplyMilitaryHappiness(townIdx, marker);

			// The wonders and the citizens
			ApplyWonderHappiness(townIdx, marker);

			// The units of the city: the support, disbanding (then all are counted again), the units on the city screen
			bool disbanded;

			do
			{
				StartUnitSupport(townIdx);
				disbanded = false;

				for (this.round.Index = 0; (short)this.round.Index < 0x80;
					this.round.Index = (ushort)(this.round.Index + 1))
				{
					if (SupportUnit(townIdx, marker))
					{
						disbanded = true;
						break;
					}
				}
			}
			while (disbanded);

			// In the turn: the costs of the support
			if (marker == 0)
				CountSupportCosts(townIdx);

			return true;
		}

		/// <summary>
		/// The production of the turn: a unit built (the human is told, a computer city sends its caravan and picks again), an
		/// improvement or wonder completed (a spaceship part, what the improvement does), the advisor of a human city; a
		/// computer city buys shields with gold. False: the city is gone.
		/// </summary>
		private bool ProduceInTurn(short townIdx)
		{
			// The food and the shields of the turn, a unit built
			switch (ProduceInCity(townIdx))
			{
				case ProductionStep.CityLost:
					return false;

				case ProductionStep.SettlersWait:
					break;

				case ProductionStep.Improvement:
					// An improvement or a wonder completed: a spaceship part, what some improvements do
					if (CompleteImprovement(townIdx))
					{
						BuildSpaceshipPart(townIdx);
						ApplyImprovement(townIdx);
					}

					AdviseHumanProduction(townIdx);
					break;

				case ProductionStep.NotEnough:
					AdviseHumanProduction(townIdx);
					break;

				default:
					FinishUnitOfCity(townIdx);
					AdviseHumanProduction(townIdx);
					break;
			}

			// A computer city buys shields with gold
			if (!this.owner.World.IsHuman(CityOwner))
				BuyForComputerCity(townIdx);

			return true;
		}

		/// <summary>
		/// The city screen once: everything drawn, then (for the human) one input handled. Redraw: from the citizens again;
		/// Restart: the city from the start; Leave: the screen ends (a computer city's screen waits for a key or a click).
		/// </summary>
		private CityScreenStep RunCityScreen(short townIdx, short marker)
		{
			// The list of improvements
			RenderImprovementList(townIdx);
			FinishImprovementList(townIdx);

			// The production box, its shields, the food box
			RenderProductionBox(townIdx);
			RenderShieldsInBox(townIdx);
			RenderFoodBox(townIdx);

			// The resources box and the row of luxuries, taxes and science
			RenderResourceRows(townIdx);
			RenderTradeRow();

			// The shortfalls, the citizens, the trade routes, the pollution
			RenderShortfalls(townIdx);
			RenderCitizensAndRoutes(townIdx);
			RenderCityDotAndPollution(townIdx);

			if (CityOwner != (ushort)this.owner.World.PersonParticipantIdx)
			{
				this.owner.Conquest.AwaitForKeyPressOrMouseClick();
				return CityScreenStep.Leave;
			}

			// The buttons, the input, the Change button
			RenderExitAndBuyButtons(townIdx);
			AwaitCityScreenInput();
			CityScreenStep step = ReadCityScreenInput(townIdx);

			if (step != CityScreenStep.Others)
				return step;

			// The other buttons: Buy, Change, the tabs, the view of the city
			step = HandleCityScreenButtons(townIdx);

			if (step != CityScreenStep.Others)
				return step;

			// A: the units in the city
			if (this.round.Key == 0x61)
			{
				ActivateUnitsInCity();
				return CityScreenStep.Redraw;
			}

			// S: an improvement to sell, else the specialists, P (the cursor on the map), M, R and the clicks on the list
			if (!PickImprovementToSell(townIdx))
			{
				step = ToggleSpecialist(townIdx);

				if (step != CityScreenStep.Others)
					return step;

				if (this.round.Key == 0x70)
					return MoveCityMapCursor(townIdx) ? ToggleCityTile(townIdx, marker) : CityScreenStep.Restart;

				step = HandleListAndRename(townIdx);

				if (step == CityScreenStep.Others)
					return HandleCityScreenClicks(townIdx, marker);

				if (step != CityScreenStep.Sell)
					return step;
			}

			if (SellImprovement(townIdx))
				return CityScreenStep.Restart;

			return HandleCityScreenClicks(townIdx, marker);
		}

		/// <summary>The clicks on the city screen: the citizens at the top, the units in the city, the map of the city</summary>
		private CityScreenStep HandleCityScreenClicks(short townIdx, short marker)
		{
			if ((short)this.owner.GlobDb3c < 0xc8 && (short)this.owner.GlobDb3e < 0x14)
				return ClickCitizen(townIdx);

			if ((short)this.owner.GlobDb3c >= 0x64 && (short)this.owner.GlobDb3c < 0x2580 && (short)this.owner.GlobDb3e >= 0x74 && ClickUnitOfCity())
				return CityScreenStep.Redraw;

			if ((short)this.owner.GlobDb3e < 0x18 || (short)this.owner.GlobDb3e >= 0x68)
				return CityScreenStep.Leave;

			this.round.DrawHoriz = (ushort)((short)this.owner.GlobDb3c / 16 - 10);
			this.round.DrawVert = (ushort)((short)(this.owner.GlobDb3e - 0x18) / 16 - 2);

			return ToggleCityTile(townIdx, marker);
		}

		/// <summary>
		/// The end of the city's turn: civil disorder and the celebration (not for the barbarians), the science, the score and
		/// the pollution, for the human the upkeep of the improvements
		/// </summary>
		private void EndCityTurn(short townIdx)
		{
			if (CityOwner != 0)
			{
				// Civil disorder, order restored, the taxes; the celebration
				ApplyCivilOrder(townIdx);
				ApplyCelebration(townIdx);
			}

			ApplyResearch();

			// The score, the pollution, the nuclear plant; the upkeep of the improvements
			if (ApplyScoreAndPollution(townIdx))
				PayImprovementUpkeep(townIdx);
		}

		/// <summary>
		/// A tile of the city (TownDeltas[townTile] from the city): with marker 1 draws it on the city screen with the icons of
		/// its food, shields and trade (MayCityWorkTile 0 - 2; closer together the more there are, a mark when there are none),
		/// else puts the city's player on the tile in map layer 2 (0x2494 set meanwhile). Either way the amounts are added to the
		/// totals at 0x70da.
		/// </summary>
		public void RenderCityTile(short townIdx, ushort townTile, short marker)
		{
			this.processor.Journal.OpenScope($"RenderCityTile({townIdx}, {townTile}, {marker})");

			short horizLoc = (short)(this.owner.World.Towns[townIdx].Place.X + this.owner.TownDeltas[townTile].X);
			short vertLoc = (short)(this.owner.World.Towns[townIdx].Place.Y + this.owner.TownDeltas[townTile].Y);
			bool shown = marker == 1;
			short total = 0;
			short spacing = 0;

			if (shown)
			{
				this.owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				for (ushort resource = 0; resource < 3; resource++)
				{
					total += (short)MayCityWorkTile(horizLoc, vertLoc, resource);
				}

				spacing = (short)(total <= 4 ? 8 : total <= 6 ? 5 : 3);
			}
			else
			{
				this.processor.EmitGlobalWord(0x2494, 1);

				this.owner.Services.AssignDot(2, horizLoc + 0x50, vertLoc, (ushort)this.owner.World.Towns[townIdx].ParticipantIdx);
			}

			short iconHoriz = 0;
			short iconVert = 0;

			for (ushort resource = 0; resource < 3; resource++)
			{
				short amount = (short)MayCityWorkTile(horizLoc, vertLoc, resource);

				this.processor.EmitGlobalWord((ushort)((resource << 1) + 0x70da), (ushort)(this.processor.PullGlobalWord((ushort)((resource << 1) + 0x70da)) + amount));

				for (; amount > 0; amount--)
				{
					if (!shown)
						continue;

					this.owner.Services.RenderPictureToScreen(ScreenBox,
						161 + (this.owner.TownDeltas[townTile].X * 16) + iconHoriz, 57 + (this.owner.TownDeltas[townTile].Y * 16) + iconVert,
						this.processor.PullGlobalWord((ushort)(((resource + 8) << 1) + 0xd4ce)));

					if (iconHoriz < 8)
					{
						iconHoriz += spacing;
					}
					else
					{
						iconHoriz = 0;
						iconVert += 8;
					}
				}
			}

			if (shown && total == 0)
			{
				this.owner.Services.RenderPictureToScreen(ScreenBox,
					165 + (this.owner.TownDeltas[townTile].X * 16), 61 + (this.owner.TownDeltas[townTile].Y * 16),
					this.processor.PullGlobalWord((0xd << 1) + 0xd4ce));
			}

			this.processor.EmitGlobalWord(0x2494, 0);

			this.processor.Journal.CloseScope("RenderCityTile");
		}

		/// <summary>
		/// What the tile yields to the city 0x653e of player 0x6548: food (arg3 0), shields (1) or trade (2). The terrain's
		/// value (0x290, or 0x374 with the special resource) with irrigation (bit 2), mine (4) and road (8, trade on terrain 0 - 2)
		/// — the real improvements only with DiagMarkers bit 2, else irrigation (terrain 0 - 2) or mine and a road (not on terrain
		/// 1) assumed; no shields on every other tile of terrain 2 and 11; the Colossus, the railroad (bit 0x10: 1.5 times), -1 above
		/// 2 under governments 0 - 1 unless the city celebrates (0x40; 0xe3c2 -= 2 while 0x2494), +1 trade under government 2+
		/// celebrating or 4+, and pollution (bit 0x40) halves it. 0 outside the map, never negative.
		/// </summary>
		public ushort MayCityWorkTile(int horizLoc, int vertLoc, ushort arg3)
		{
			this.processor.Journal.OpenScope($"MayCityWorkTile({horizLoc}, {vertLoc}, {arg3})");

			short yield = 0;

			if (this.owner.MapOps.VerifyMapLimits(horizLoc, vertLoc) != 0)
			{
				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				ushort terrainRecord = (ushort)((terrain * 0x13) + arg3);
				ushort yieldTable = (ushort)(this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0 ? 0x374 : 0x290);

				yield = (sbyte)this.processor.PullGlobalByte((ushort)(terrainRecord + yieldTable));

				ushort improvements = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

				if ((this.owner.World.DiagMarkers & 2) == 0)
				{
					improvements = (ushort)((short)terrain > 2 ? 4 : 2);

					if (terrain != 1)
					{
						improvements |= 8;
					}
				}

				if (terrain != MapRules.OceanTerrain)
				{
					// Terrain records of 0xc words at 0x44a: irrigation and mining bonus minus 1
					if (arg3 == 0 && (improvements & 2) != 0)
					{
						yield += (short)(0xffff - this.processor.PullGlobalWord((ushort)((terrain * 0xc) + 0x44a)));
					}

					if (arg3 == 1 && (improvements & 4) != 0)
					{
						yield += (short)(0xffff - this.processor.PullGlobalWord((ushort)((terrain * 0xc) + 0x44e)));
					}

					if (arg3 == 2 && (improvements & 8) != 0 && (short)terrain <= 2)
					{
						yield++;
					}
				}

				if (arg3 == 1 && (terrain == 2 || terrain == 0xb) && ((7 * horizLoc + 11 * vertLoc) & 2) != 0)
				{
					yield = 0;
				}

				ushort townIdx = this.processor.PullGlobalWord(0x653e);

				if (yield != 0 && arg3 == 2 && ObtainWonderCityID(3) == townIdx)
				{
					yield++;
				}

				if ((improvements & 0x10) != 0)
				{
					yield += (short)(yield / 2);
				}

				if (yield > 2 && (this.owner.World.Towns[townIdx].ConditionMarker & 0x40) == 0)
				{
					if (this.owner.World.Participants[CityOwner].RegimeKind <= 1)
					{
						yield--;
					}

					if (this.processor.PullGlobalWord(0x2494) != 0)
					{
						this.processor.EmitGlobalWord(0xe3c2, (ushort)(this.processor.PullGlobalWord(0xe3c2) - 2));
					}
				}

				if (yield != 0 && arg3 == 2)
				{
					if (this.processor.PullGlobalWord(0x2494) != 0)
					{
						this.processor.EmitGlobalWord(0xdb42, (ushort)(this.processor.PullGlobalWord(0xdb42) + 1));
					}

					short needed = (short)((this.owner.World.Towns[townIdx].ConditionMarker & 0x40) != 0 ? 2 : 4);

					if (needed <= this.owner.World.Participants[CityOwner].RegimeKind)
					{
						yield++;
					}
				}

				if ((improvements & 0x40) != 0)
				{
					yield = (short)((yield + 1) / 2);
				}

				if (yield < 0)
				{
					yield = 0;
				}
			}

			this.processor.RegAcc.Value16 = (ushort)yield;
			this.processor.Journal.CloseScope("MayCityWorkTile");

			return (ushort)yield;
		}

		/// <summary>
		/// Does the player have the effect of the wonder: it stands in a city of the player and is not obsolete
		/// </summary>
		/// <returns>1 or 0</returns>
		public ushort PlayerEnjoysWonderEffect(short participantIdx, short miracleIdent)
		{
			this.processor.Journal.OpenScope($"PlayerEnjoysWonderEffect({participantIdx}, {miracleIdent})");

			bool enjoys = false;

			if (!IsWonderObsolete(miracleIdent))
			{
				ushort townIdx = (ushort)this.owner.World.MarvelTownIdx[miracleIdent];

				enjoys = townIdx != 0xffff && this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx;
			}

			this.processor.RegAcc.Value16 = (ushort)(enjoys ? 1 : 0);

			this.processor.Journal.CloseScope("PlayerEnjoysWonderEffect");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The city of the wonder, also when the wonder is not finished (-1)
		/// </summary>
		/// <returns>City index, or 0xffff when the wonder is obsolete</returns>
		public ushort ObtainWonderCityID(short miracleIdent)
		{
			this.processor.Journal.OpenScope($"ObtainWonderCityID({miracleIdent})");

			ushort townIdx = IsWonderObsolete(miracleIdent) ? (ushort)0xffff : (ushort)this.owner.World.MarvelTownIdx[miracleIdent];
			this.processor.RegAcc.Value16 = townIdx;

			this.processor.Journal.CloseScope("ObtainWonderCityID");

			return townIdx;
		}

		/// <summary>
		/// A wonder has no effect any more when any civilization knows the technology that makes it obsolete
		/// (word table at 0x10fe of the data segment)
		/// </summary>
		private bool IsWonderObsolete(short miracleIdent)
		{
			short obsoletingTechnology = this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(0x10fe + (miracleIdent << 1)));

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, obsoletingTechnology) != 0)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Pollution (improvement bit 0x40) appears on the tile, unless it is there already; the human player sees it
		/// </summary>
		public void AttachSmog(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"AttachSmog({horizLoc}, {vertLoc})");

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x40) == 0)
			{
				this.owner.MapOps.AssignTileImprovements(0x40, horizLoc, vertLoc);

				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

				this.owner.World.DirtyCellTally++;
			}

			this.processor.Journal.CloseScope("AttachSmog");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void AssignCitizenType(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"AssignCitizenType({arg1}, {arg2})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort currentMarkers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			ushort updated = CitizenSpecialistRules.SetSpecialistType(currentMarkers, arg1, (CitizenSpecialistRules.SpecialistType)arg2);
			this.owner.World.Towns[cityIdx].LaborerMarkers2 = updated;

			this.processor.Journal.CloseScope("AssignCitizenType");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort ObtainCitizenType(ushort arg1)
		{
			this.processor.Journal.OpenScope($"ObtainCitizenType({arg1})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort markers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			var specType = CitizenSpecialistRules.GetSpecialistType(markers, arg1);
			this.processor.RegAcc.Value16 = (ushort)specType;

			this.processor.Journal.CloseScope("ObtainCitizenType");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort TallySpecialists(ushort arg1)
		{
			this.processor.Journal.OpenScope($"TallySpecialists({arg1})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort markers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			int tally = CitizenSpecialistRules.CountSpecialists(markers, (CitizenSpecialistRules.SpecialistType)arg1);
			this.processor.RegAcc.Value16 = (ushort)tally;

			this.processor.Journal.CloseScope("TallySpecialists");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Keeps the specialists of the city in range: entertainers (0x70e2) and taxmen (0x70e4) are clamped to the city size,
		/// scientists (0x6542) become taxmen while there are fewer taxmen than scientists, then specialists are given up
		/// (scientists first, then entertainers, and a taxman each time) until all of them fit in the size less the workers
		/// (clamped to 0 - 99)
		/// </summary>
		public void ReshuffleCitizens(short townIdx, ushort workerTally)
		{
			this.processor.Journal.OpenScope($"ReshuffleCitizens({townIdx}, {workerTally})");

			City town = this.owner.World.Towns[townIdx];

			CityHappy =
				(ushort)this.owner.Helpers.VerifyValueSpan((short)CityHappy, 0, town.TrueExtent);

			while (this.processor.PullGlobalWord(0x6542) != 0 &&
				(short)CityUnhappy < this.processor.PullGlobalSignedWord(0x6542))
			{
				this.processor.EmitGlobalWord(0x6542, (ushort)(this.processor.PullGlobalWord(0x6542) - 1));
				CityUnhappy = (ushort)(CityUnhappy + 1);
			}

			ushort taxmen = (ushort)this.owner.Helpers.VerifyValueSpan((short)CityUnhappy, 0, town.TrueExtent);

			while (true)
			{
				CityUnhappy = taxmen;

				short room = (short)this.owner.Helpers.VerifyValueSpan(town.TrueExtent - (short)workerTally, 0, 99);

				if ((short)(CityHappy + CityUnhappy) <= room)
					break;

				if (this.processor.PullGlobalWord(0x6542) != 0)
				{
					this.processor.EmitGlobalWord(0x6542, (ushort)(this.processor.PullGlobalWord(0x6542) - 1));
				}
				else
				{
					CityHappy = (ushort)(CityHappy - 1);
					CityHappy =
						(ushort)this.owner.Helpers.VerifyValueSpan((short)CityHappy, 0, town.TrueExtent);
				}

				CityUnhappy = (ushort)(CityUnhappy - 1);
				taxmen = (ushort)this.owner.Helpers.VerifyValueSpan((short)CityUnhappy, 0, town.TrueExtent);
			}

			this.processor.Journal.CloseScope("ReshuffleCitizens");
		}

		/// <summary>
		/// The citizens of the city from (horizLoc, vertLoc): the happy (0x70e2), the content, the unhappy (0x70e4, the first
		/// 0x6542 of them marked) and the arg4 specialists (ObtainCitizenType), 7 pixels apart or narrower to fit in arg5, with
		/// gaps between the groups.
		/// </summary>
		/// <returns>The distance of the icons, also in AX</returns>
		public ushort RenderCitizenIcons(short townIdx, short horizLoc, short vertLoc, ushort arg4, ushort arg5)
		{
			this.processor.Journal.OpenScope($"RenderCitizenIcons({townIdx}, {horizLoc}, {vertLoc}, {arg4}, {arg5})");

			// The gap after the happy citizens depends on the frame word 0x2, which is never written (the leftovers of the
			// functions called before at this depth), like in the original; the final values of the locals stay in the frame
			this.processor.EnterFrame(0xa);

			sbyte size = this.owner.World.Towns[townIdx].TrueExtent;

			this.processor.EmitLocalByte(0xa, (byte)size);

			ushort step = (short)(7 * size) > (short)arg5 ? (ushort)CPU.SignedQuotient((short)arg5, size) : (ushort)7;
			short citizenIdx;

			for (citizenIdx = 0; citizenIdx < (short)CityHappy; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, this.processor.PullGlobalWord((ushort)(((citizenIdx & 1) << 1) + 0x6e96)), step);
			}

			if (this.processor.PullLocalWord(0x2) != 0)
			{
				horizLoc += 2;
			}

			for (citizenIdx = 0; (short)(this.owner.World.Towns[townIdx].TrueExtent - arg4 - CityHappy -
				CityUnhappy) > citizenIdx; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, this.processor.PullGlobalWord((ushort)(((citizenIdx & 1) << 1) + 0x6e9a)), step);
			}

			if (citizenIdx != 0)
			{
				horizLoc += 2;
			}

			for (citizenIdx = 0; citizenIdx < (short)CityUnhappy; citizenIdx++)
			{
				this.owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc,
					this.processor.PullGlobalWord((ushort)(((citizenIdx & 1) << 1) + 0x6e9e)));

				if (citizenIdx < (short)this.processor.PullGlobalWord(0x6542))
				{
					this.owner.VideoCard.SubstituteHue(ScreenBox, horizLoc, vertLoc, 12, 14, 5, 12);
				}

				horizLoc += (short)step;
			}

			horizLoc += 4;

			for (citizenIdx = 0; citizenIdx < (short)arg4; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, this.processor.PullGlobalWord((ushort)((ObtainCitizenType((ushort)citizenIdx) << 1) + 0x6ea0)), step);
			}

			this.processor.EmitLocalWord(0x6, (ushort)citizenIdx);
			this.processor.EmitLocalWord(0x8, step);

			this.processor.RegAcc.Value16 = step;
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitizenIcons");

			return step;
		}

		/// <summary>One citizen icon at the place</summary>
		/// <returns>The place of the next one</returns>
		private short RenderCitizen(short horizLoc, short vertLoc, ushort picture, ushort step)
		{
			this.owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc, picture);

			return (short)(horizLoc + step);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPosSrc"></param>
		/// <param name="xPosDst"></param>
		/// <param name="yPosDst"></param>
		public void RenderResourceIcon(short horizLocOrigin, short horizLocTo, short vertLocTo)
		{
			this.processor.Journal.OpenScope($"RenderResourceIcon({horizLocOrigin}, {horizLocTo}, {vertLocTo})");

			// function body
			if (horizLocOrigin < 22 || horizLocOrigin > 24)
			{
				horizLocOrigin--;

				if (horizLocOrigin >= 24)
				{
					horizLocOrigin -= 3;
				}

				if (horizLocOrigin >= 40)
				{
					this.owner.VideoCard.RenderImage(
						PictureScreenBox,
						((horizLocOrigin & 1) * 19) + 161, 100, 18, 10,
						ScreenBox,
						horizLocTo, vertLocTo);
				}
				else
				{
					this.owner.VideoCard.RenderImage(
						PictureScreenBox,
						((horizLocOrigin / 5) * 19) + 161,
						((horizLocOrigin % 5) * 10) + 50,
						18, 10,
						ScreenBox,
						horizLocTo, vertLocTo);
				}
			}

			// Far return
			this.processor.Journal.CloseScope("RenderResourceIcon");
		}

		/// <summary>
		/// A box from one corner to the other: the background pattern (FloodBoxWithMotif) and the frame (RenderBox, style 1)
		/// </summary>
		public void RenderFrame(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1)
		{
			this.processor.Journal.OpenScope($"RenderFrame({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1})");

			int breadth = horizSpot1 - horizLoc;
			int altitude = vertSpot1 - vertLoc;

			FloodBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.owner.Dialogs.RenderBox(horizLoc, vertLoc, breadth, altitude, 1);

			this.processor.Journal.CloseScope("RenderFrame");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			this.processor.Journal.OpenScope($"FloodBoxWithMotif({horizLoc}, {vertLoc}, {breadth}, {altitude})");

			// function body
			if (VgaMode == 0)
			{
				this.owner.Services.FloodBox(ScreenBox, horizLoc, vertLoc, breadth, altitude, 9);
			}
			else
			{
				int boxVertLoc = vertLoc;
				int boxAltitude = altitude;

				while (boxAltitude > 0)
				{
					int tileAltitude = Math.Min(boxAltitude, 16);
					int boxHorizLoc = horizLoc;
					int boxBreadth = breadth;

					while (boxBreadth > 0)
					{
						int tileBreadth = Math.Min(boxBreadth, 16);

						this.owner.VideoCard.RenderImage(PictureScreenBox,
							208, 100, tileBreadth, tileAltitude,
							ScreenBox,
							boxHorizLoc, boxVertLoc);

						boxHorizLoc += tileBreadth;
						boxBreadth -= tileBreadth;
					}

					boxVertLoc += tileAltitude;
					boxAltitude -= tileAltitude;
				}
			}
		
			// Far return
			this.processor.Journal.CloseScope("FloodBoxWithMotif");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="xPos1"></param>
		/// <param name="yPos1"></param>
		/// <param name="stringPtr"></param>
		/// <param name="mode"></param>
		public void RenderLabelBox(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort charsAddr, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderLabelBox({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, 0x{charsAddr:x4}, {style})");

			// function body
			this.owner.Services.FloodBox(ScreenBox,
				horizLoc, vertLoc, horizSpot1 - horizLoc, vertSpot1 - vertLoc, style);
		
			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizSpot1, vertLoc, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizLoc, vertSpot1, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc + 1, vertSpot1, horizSpot1, vertSpot1, (ushort)((style < 8) ? 8 : (style - 8)));

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizSpot1, vertLoc, horizSpot1, vertSpot1, (ushort)((style < 0x8) ? 8 : (style - 8)));

			this.owner.TextPainter.RenderCenteredTextToScreen0(charsAddr,
				((horizLoc + horizSpot1) / 2) + 1, ((vertLoc + vertSpot1) / 2) - 2, (ushort)(style ^ 8));

			// Far return
			this.processor.Journal.CloseScope("RenderLabelBox");
		}

		/// <summary>
		/// Draws the small map of the city screen: the 80 x 50 tiles around the human player's view (columns from HorizBegin - 40),
		/// every seen tile as a 2 x 2 dot (ocean in colour 1, land in colour 2), two tiles in every 3 pixels: the four tiles of
		/// a 2 x 2 block are shifted by the offsets at 0x280c (columns) and 0x2810 (rows)
		/// </summary>
		public void RenderMiniMap()
		{
			this.processor.Journal.OpenScope("RenderMiniMap()");

			this.owner.Services.FloodBox(ScreenBox, 101, 117, 120, 75, 0);

			short humanIdx = this.owner.World.PersonParticipantIdx;

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				for (int column = 0; column < 80; column++)
				{
					int horizLoc = this.owner.Routing.TunePosition(this.owner.World.Participants[humanIdx].HorizBegin + column - 40);

					if ((this.owner.World.ChartSight[(ushort)horizLoc, vertLoc] & (1 << humanIdx)) == 0)
						continue;

					ushort quarter = (ushort)(((vertLoc & 1) << 1) + (column & 1));
					int dotHoriz = (column / 2) * 3 + (sbyte)this.processor.PullGlobalByte((ushort)(0x280c + quarter)) + 0x65;
					int dotVert = (vertLoc / 2) * 3 + (sbyte)this.processor.PullGlobalByte((ushort)(0x2810 + quarter)) + 0x76;
					ushort hue = (ushort)(this.owner.MapOps.ObtainTerrainType((short)horizLoc, vertLoc) == MapRules.OceanTerrain ? 1 : 2);

					this.owner.Services.FloodBox(ScreenBox, dotHoriz, dotVert, 2, 2, hue);
				}
			}

			this.owner.Dialogs.RenderBox(100, 117, 121, 75, 9);

			this.processor.Journal.CloseScope("RenderMiniMap");
		}

		/// <summary>
		/// A dot of 2 x 2 pixels on the small world map (only when 0x2496 = 2), the world centred on the start column of the
		/// human player
		/// </summary>
		public void RenderMiniMapDot(int horizLoc, int vertLoc, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderMiniMapDot({horizLoc}, {vertLoc}, {style})");

			if (CityScreenTab == 2)
			{
				horizLoc = this.owner.Routing.TunePosition(
					horizLoc - this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin + 40);

				this.owner.Services.FloodBox(ScreenBox,
					((horizLoc * 3) / 2) + 101, ((vertLoc * 3) / 2) + 118, 2, 2, style);
			}

			this.processor.Journal.CloseScope("RenderMiniMapDot");
		}
	}
}
