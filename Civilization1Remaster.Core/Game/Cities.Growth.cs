namespace Civilization1Remaster.Core.Game
{
	/// <summary>The food, growth and workers of a city</summary>
	partial class Cities
	{
		/// <summary>
		/// The food of the city in its turn: the flag 0x80 cleared; with the food storage below zero a settler of the city
		/// starves (the human is told), else the city loses a citizen and the storage, at size 1 the city is lost
		/// with the storage at (size + 1) x 0x4a the city grows (a granary keeps 5 x size + 5), above 10
		/// without an aqueduct it stays (with the bit 4 of DiagMarkers; the human is told). False when the city is lost.
		/// The locals of HandleCity are used as in the original.
		/// </summary>
		private bool FeedCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			town.ConditionMarker &= 0x7f;

			if (town.GrainTally < 0)
			{
				// Starvation: a settler of the city, or the city
				this.round.Value = 0xffff;
				ushort troop = 0;

				for (; troop < UnitMemory.TroopTally; troop++)
				{
					this.round.Index = troop;

					if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0 && this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) == townIdx)
					{
						this.round.Value = troop;
						break;
					}
				}

				if (troop == UnitMemory.TroopTally)
					this.round.Index = troop;

				if (owner == this.Owner.World.PersonParticipantIdx)
				{
					Message("Food storage exhausted\nin ")
						.City(townIdx)
						.Text((ushort)(this.round.Value != 0xffff ? 0x2510 : 0x2522));
					this.Owner.Services.PerformSound(0x24, 0);
					this.Owner.Paper.ShowNewspaper(townIdx);
					this.Owner.Services.PerformSound(1, 0);
				}

				if (this.round.Value != 0xffff)
				{
					this.Owner.UnitOps.DestroyUnit(owner, (short)this.round.Value);
				}
				else
				{
					// The city loses a citizen, and itself at size 1
					town.TrueExtent--;

					if (town.TrueExtent == 0)
						return LoseStarvingCity(town, townIdx);
				}

				town.GrainTally = 0;
				this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			}

			return GrowCity(town, townIdx);
		}

		/// <summary>The starving city is lost (see FeedCity); false</summary>
		private bool LoseStarvingCity(City town, short townIdx)
		{
			this.Owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.Owner.NewGameWizard.DestroyCivilization((short)CityTurn.Owner, 0);
			this.Owner.Startup.ShowMouseCursor();
			return false;
		}

		/// <summary>The growth of the city (see FeedCity); true</summary>
		private bool GrowCity(City town, short townIdx)
		{
			// Growth

			if ((short)((town.TrueExtent + 1) * (short)this.round.CostFactor) > town.GrainTally)
				return true;

			town.TrueExtent++;

			// A granary keeps half of the food
			town.GrainTally = (short)(town.Has(CityStructure.Granary) ? (short)(5 * town.TrueExtent) + 5 : 0);

			if (town.TrueExtent > 10 && !town.Has(CityStructure.Aqueduct) && (this.Owner.World.DiagMarkers & 4) != 0)
			{
				// No growth above 10 without an aqueduct
				town.TrueExtent--;

				if (CityTurn.Owner == (ushort)this.Owner.World.PersonParticipantIdx)
				{
					Screen.DialogStyle = 4;
					EmptyMessage()
						.City(townIdx)
						.Text(" requires an AQUEDUCT\nfor further growth.\n")
						.Dialog(100, 80);
				}
			}

			this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			return true;
		}

		/// <summary>
		/// The units of the city: 0x50 the size + 1; 0xdeb8 the special slots of the city in use (Unfamiliar) and the units
		/// it supports (but diplomats and caravans), 0xd2f6 those above the size (and every one for a government above
		/// monarchy with the bit 2 of DiagMarkers), 0x6546 the attackers away from the city (planes always), 0xe3c6 the
		/// settlers.
		/// </summary>
		private void CountCityUnits(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			this.round.Workers = (ushort)(town.TrueExtent + 1);
			CityTurn.AttackersAway = 0;
			CityTurn.SpecialSlots = 0;
			CityTurn.UnitUpkeep = 0;
			CityTurn.Settlers = 0;

			// The special slots of the city
			CountSpecialSlots(townIdx);

			// The units the city supports
			for (ushort troop = 0; troop < UnitMemory.TroopTally; troop++)
			{
				this.round.Index = troop;

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) != townIdx)
					continue;

				if (this.troops.PullSigned(owner, troop, UnitMemory.Kind) < 0x1a)
				{
					CityTurn.SpecialSlots = (ushort)(CityTurn.SpecialSlots + 1);

					if (town.TrueExtent < (short)CityTurn.SpecialSlots ||
						(this.Owner.World.Participants[owner].RegimeKind > 1 && (this.Owner.World.DiagMarkers & 2) != 0))
					{
						CityTurn.UnitUpkeep = (ushort)(CityTurn.UnitUpkeep + 1);
					}

					// An attacker away from the city (a plane always)
					if (this.troops.KindField(owner, troop, UnitKindTable.AssaultPower) != 0 &&
						(this.troops.KindField(owner, troop, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troop, UnitMemory.Horiz) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troop, UnitMemory.Vert) != (byte)(sbyte)town.Place.Y))
					{
						CityTurn.AttackersAway = (ushort)(CityTurn.AttackersAway + 1);
					}

				}

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0)
					CityTurn.Settlers = (ushort)(CityTurn.Settlers + 1);
			}

			this.round.Index = UnitMemory.TroopTally;
		}

		/// <summary>
		/// The tiles the city cannot work (the array at BP-0x3e, one word per tile around): those the owner has not seen
		/// (not for the barbarians), those of another player (marked also in the array at BP-0x8e; 0x70e8 = 1 when that is
		/// the human) and those worked by a neighbouring city; their workers are taken off. Then the 4 words at 0x70da
		/// cleared. The locals of HandleCity are used as in the original.
		/// </summary>
		private void MarkUnavailableTiles(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			CityTurn.HumanTileNear = 0;
			ushort tile = 0;

			for (; (short)tile < (short)CityTurn.Tiles; tile++)
			{
				this.round.Index = tile;
				int unavailable = 0x3e - (ushort)(tile << 1);
				this.round.SetFrame(unavailable, 0);

				short horizLoc = (short)(town.Place.X + this.Owner.TownDeltas[tile].X);
				short vertLoc = (short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
				this.round.TileHoriz = (ushort)horizLoc;
				this.round.TileVert = (ushort)vertLoc;

				short owner = (short)CityTurn.Owner;

				// A tile the owner has not seen (the barbarians see all)
				if (owner != 0 && (this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
					this.round.SetFrame(unavailable, 1);

				this.round.Value = this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				ushort shared = (ushort)(short)(10 * this.Owner.TownDeltas[tile].X + this.Owner.TownDeltas[tile].Y * 2);

				if (this.round.Value != 0xffff && this.round.Value != CityTurn.Owner)
				{
					// A tile of another player
					this.round.SetFrame(0x8e - shared, 1);
					this.round.SetFrame(unavailable, 1);

					if (this.round.Value == (ushort)this.Owner.World.PersonParticipantIdx)
						CityTurn.HumanTileNear = 1;
				}

				// A tile worked by a neighbouring city

				if (this.round.Frame(0x8e - shared) != 0)
					this.round.SetFrame(unavailable, 1);

				if (this.round.Frame(unavailable) == 0)
					continue;

				// Its worker is taken off
				uint mask = ~(1u << (tile & 0xff));
				town.LaborerMarkers0 &= (ushort)mask;
				town.LaborerMarkers1 &= (ushort)(mask >> 16);
			}

			this.round.Index = tile;

			for (ushort slot = 0; slot < 4; slot++)
			{
				this.round.Index = slot;
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)((slot << 1) + 0x70da), 0);
			}

			this.round.Index = 4;
		}

		/// <summary>
		/// The workers the city keeps: a computer city every fourth turn (without the flag 1 and with no tile of the human
		/// around, 0x70e8) drops them all and picks anew (true). Else 0x50 = size + 1 less the workers: not too many, they
		/// are kept (drawn, marked in the array at BP-0x3e and in 0xee / 0xec); too many, all go (the human is told in the
		/// turn). True when workers are still to be picked (0x50 neither the specialists 0xe8b8 nor 0). The locals of
		/// HandleCity are used as in the original.
		/// </summary>
		private bool KeepWorkers(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			this.round.WorkersHigh = 0;
			this.round.WorkersLow = 0;

			if (!this.Owner.World.IsHuman(owner) && ((townIdx + this.Owner.World.RoundTally) & 3) == 0)
			{
				if ((town.ConditionMarker & 1) == 0 && CityTurn.HumanTileNear == 0)
					return true;
			}

			// 0x50: the size + 1 less the workers
			ushort tile = 0;

			for (; (short)tile < (short)CityTurn.Tiles; tile++)
			{
				this.round.Index = tile;

				if (TestWorker(townIdx, tile))
					this.round.Workers = (ushort)(this.round.Workers - 1);
			}

			this.round.Index = tile;

			if ((short)this.round.Workers >= 0)
			{
				// Not too many: the workers are kept
				this.round.Workers = (ushort)(town.TrueExtent + 1);

				for (tile = 0; (short)tile < (short)CityTurn.Tiles; tile++)
				{
					this.round.Index = tile;

					if (this.round.Workers == 0 || !TestWorker(townIdx, tile))
						continue;

					this.round.SetFrame(0x3e - ((ushort)(tile << 1)), 1);

					DrawCityTile(townIdx, tile, marker);

					this.round.Workers = (ushort)(this.round.Workers - 1);
					uint mask = 1u << (tile & 0xff);
					this.round.WorkersLow = (ushort)(this.round.WorkersLow | (ushort)mask);
					this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | (ushort)(mask >> 16));
				}

				this.round.Index = tile;
			}
			else
			{
				// Too many: all go
				if (marker == 0 && owner == this.Owner.World.PersonParticipantIdx)
				{
					Message("Population decrease\nin ")
						.City(townIdx)
						.Text(".\n")
						.Style(4)
						.Dialog(100, 80);
					CityTurn.Again = 1;
				}

				this.round.Workers = (ushort)(town.TrueExtent + 1);
			}

			return this.round.Workers != CityTurn.Specialists && this.round.Workers != 0;
		}

		/// <summary>Whether the tile is worked by the city</summary>
		private bool TestWorker(short townIdx, ushort tile)
		{
			City town = this.Owner.World.Towns[townIdx];
			uint mask = 1u << (tile & 0xff);

			return ((mask & town.LaborerMarkers0) | ((mask >> 16) & town.LaborerMarkers1)) != 0;
		}

		/// <summary>
		/// The new workers of the city (0x50 the workers still to place): the city tile first (once, 0x16); while the food
		/// (0x70da + half the workers left) does not feed the citizens (2 per size) and the settlers (0x48 each), or the
		/// city is below size 3, the tile with the most food (then the most shields x 2 + trade; none without shields for
		/// the last worker of a city without shields, 0x70dc) is taken while more than the specialists 0xe8b8 are to place
		/// and the last search found one (0x108). Then (a computer city with the flag 1 keeps one more specialist) the
		/// tiles with the best weighted yield: food x 16 / the food left over, shields x (3 x size) / the shields left over
		/// (0x70dc less 0xd2f6), trade x (2 x size) / the trade (0x70de), each divisor kept within 1 - 99. The workers 0xee
		/// / 0xec and the specialists (0x50 in bits 26 - 31) are stored, 0xae = -0x50. The locals of HandleCity are used as
		/// in the original.
		/// </summary>
		private void PickWorkers(short townIdx, short marker, bool pick)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (pick)
			{
				if ((short)this.round.Workers > 0 && this.round.CityTileTaken == 0)
				{
					// The city tile
					this.round.CityTileTaken = 1;
					DrawCityTile(townIdx, 20, marker);
					this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | 0x10);
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}

				// Food first
				while (true)
				{
					int neededFood = town.TrueExtent * 2 + ((short)this.round.SettlerFood * (short)CityTurn.Settlers);
					int availableFood = (short)this.round.Workers / 2 + CityTurn.Food;
					bool hungry = neededFood > availableFood || town.TrueExtent < 3;

					if (!hungry || (short)this.round.Workers <= (short)CityTurn.Specialists ||
						this.round.Found == 0xffff)
					{
						break;
					}

					this.round.Found = 0xffff;
					this.round.BestValue = 0;
					this.round.BestFood = 0;
					ushort tile = 0;

					for (; (short)tile < (short)CityTurn.Tiles; tile++)
					{
						this.round.Index = tile;

						if (!FindFreeTile(townIdx, tile))
							continue;

						short horizLoc = (short)this.round.TileHoriz;
						short vertLoc = (short)this.round.TileVert;

						this.round.Amount = MayCityWorkTile(horizLoc, vertLoc, 0);
						this.round.TileValue = (ushort)(MayCityWorkTile(horizLoc, vertLoc, 1) << 1);

						if (this.round.Workers == 1 && CityTurn.Shields == 0 && this.round.TileValue == 0)
							continue;

						this.round.TileValue = (ushort)(this.round.TileValue + MayCityWorkTile(horizLoc, vertLoc, 2));

						short food = (short)this.round.Amount;
						short best = (short)this.round.BestFood;

						if (food > best || (food == best && (short)this.round.TileValue > (short)this.round.BestValue))
						{
							this.round.BestFood = (ushort)food;
							this.round.BestValue = this.round.TileValue;
							this.round.Found = this.round.Index;
						}
					}

					this.round.Index = tile;

					if (this.round.Found == 0xffff)
						break;

					TakeWorker(townIdx, marker);
				}

				// A computer city with the flag 1 keeps one more specialist
				if (CityTurn.Owner != (ushort)this.Owner.World.PersonParticipantIdx)
				{
					if ((town.ConditionMarker & 1) != 0)
						CityTurn.Specialists = (ushort)(CityTurn.Specialists + 1);
				}

				// The best weighted yield
				while ((short)this.round.Workers > (short)CityTurn.Specialists)
				{
					this.round.Found = 0xffff;
					this.round.BestWeighted = 0;
					this.round.BestValue = 0;
					this.round.BestFood = 0;
					ushort tile = 0;

					for (; (short)tile < (short)CityTurn.Tiles; tile++)
					{
						this.round.Index = tile;

						if (!FindFreeTile(townIdx, tile))
							continue;

						short horizLoc = (short)this.round.TileHoriz;
						short vertLoc = (short)this.round.TileVert;

						// Food
						short divisor = (short)this.Owner.Helpers.ClampValue(
							(short)CityTurn.Food - (town.TrueExtent * 2) - ((short)this.round.SettlerFood * (short)CityTurn.Settlers), 1, 99);
						short weight = (short)(16 / divisor);

						short yield = (short)MayCityWorkTile(horizLoc, vertLoc, 0);
						this.round.TileFood = (ushort)yield;
						this.round.TileValue = (ushort)(yield * weight);

						// Shields
						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 1);
						this.round.TileShields = (ushort)yield;

						divisor = (short)this.Owner.Helpers.ClampValue((short)CityTurn.Shields - (short)CityTurn.UnitUpkeep, 1, 99);
						this.round.TileValue = (ushort)(this.round.TileValue + (short)(3 * town.TrueExtent) / divisor * yield);

						// Trade
						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 2);
						this.round.TileTrade = (ushort)yield;

						divisor = (short)this.Owner.Helpers.ClampValue((short)CityTurn.Trade, 1, 99);
						int product = (short)(town.TrueExtent * 2) / divisor * yield;
						this.round.TileValue = (ushort)(this.round.TileValue + (ushort)product);

						if ((short)this.round.TileValue > (short)this.round.BestValue)
						{
							this.round.BestValue = this.round.TileValue;
							this.round.Found = this.round.Index;
							this.round.BestWeighted = (ushort)(((this.round.TileFood + this.round.TileShields) << 1) + this.round.TileTrade);
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
			town.LaborerMarkers0 = (ushort)workers;
			town.LaborerMarkers1 = (ushort)(workers >> 16);
			this.round.WorkersGained = (ushort)-(short)this.round.Workers;
		}

		/// <summary>A tile around the city still free for a worker (the array at BP-0x3e); 0xc6 / 0xd2 its place</summary>
		private bool FindFreeTile(short townIdx, ushort tile)
		{
			City town = this.Owner.World.Towns[townIdx];


			if (this.round.Frame(0x3e - (tile << 1)) != 0)
				return false;

			this.round.TileHoriz = (ushort)(short)(town.Place.X + this.Owner.TownDeltas[tile].X);
			this.round.TileVert = (ushort)(short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
			return true;
		}

		/// <summary>The tile 0x108 gets a worker: marked, drawn, in 0xee / 0xec; 0x50 one less</summary>
		private void TakeWorker(short townIdx, short marker)
		{
			ushort tile = this.round.Found;

			this.round.SetFrame(0x3e - ((ushort)(tile << 1)), 1);

			DrawCityTile(townIdx, tile, marker);

			uint mask = 1u << (tile & 0xff);
			this.round.WorkersLow = (ushort)(this.round.WorkersLow | (ushort)mask);
			this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | (ushort)(mask >> 16));
			this.round.Workers = (ushort)(this.round.Workers - 1);
		}

		/// <summary>
		/// After the workers: 0x8 the specialists; the first 0x8 citizen icons without a type become type 3, the rest of
		/// the 8 type 0. The factories: 0xf8 the factory bonus (2 the factory, 4 the manufacturing plant), 0xac the power
		/// (2 the power plant, hydro plant, nuclear plant or the wonder 15 on the same continent), 0x6c98 the pollution
		/// factor (1; 2 for the clean plants, 3 the recycling center); the shields 0x70dc (kept in 0x10a, the trade in 0xa)
		/// grow by a quarter of the factory bonus and of the power within it. The locals of HandleCity are used as in the
		/// original.
		/// </summary>
		private void PrepareCitizensAndFactories(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			this.round.Specialists = this.round.Workers;

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
			this.round.FactoryPower = 0;
			this.round.FactoryBonus = 0;
			Turn.PollutionFactor = 1;

			if (town.Has(CityStructure.Factory))
				this.round.FactoryBonus = 2;

			if (town.Has(CityStructure.ManufacturingPlant))
				this.round.FactoryBonus = 4;

			if (town.Has(CityStructure.PowerPlant))
				this.round.FactoryPower = 2;

			if (town.Has(CityStructure.HydroPlant))
			{
				Turn.PollutionFactor = 2;
				this.round.FactoryPower = 2;
			}

			if (town.Has(CityStructure.NuclearPlant))
			{
				Turn.PollutionFactor = 2;
				this.round.FactoryPower = 2;
			}

			if (PlayerEnjoysWonderEffect((short)CityTurn.Owner, 15) != 0)
			{
				// The wonder powers the cities on its continent
				short marvelIdx = this.Owner.World.MarvelTownIdx[15];

				this.round.Continent = this.Owner.MapOps.ObtainContinentID(this.Owner.World.Towns[marvelIdx].Place.X, this.Owner.World.Towns[marvelIdx].Place.Y);

				if (this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) == this.round.Continent)
				{
					Turn.PollutionFactor = 2;
					this.round.FactoryPower = 2;
				}
			}

			if (town.Has(CityStructure.RecyclingCenter))
				Turn.PollutionFactor = 3;

			this.round.FactoryPower = (ushort)this.Owner.Helpers.ClampValue((short)this.round.FactoryPower, 0, (short)this.round.FactoryBonus);
			this.round.ShieldsKept = CityTurn.Shields;
			this.round.TradeKept = CityTurn.Trade;

			// The shields grow by a quarter of the power and of the factory bonus
			short shields = (short)CityTurn.Shields;
			short power = (short)((short)((short)this.round.FactoryPower * shields) / 4);
			short factory = (short)((short)this.round.FactoryBonus * shields);
			CityTurn.Shields = (ushort)(CityTurn.Shields + ((ushort)(factory / 4 + power)));

		}
	}
}
