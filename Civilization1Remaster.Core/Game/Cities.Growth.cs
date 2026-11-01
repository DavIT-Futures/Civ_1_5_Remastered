namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		private bool FeedCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			town.ConditionMarker &= 0x7f;

			if (town.GrainTally < 0)
			{
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
				{
					this.round.Index = troop;
				}

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
					town.TrueExtent--;

					if (town.TrueExtent == 0)
					{
						return LoseStarvingCity(town, townIdx);
					}
				}

				town.GrainTally = 0;
				this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			}

			return GrowCity(town, townIdx);
		}

		private bool LoseStarvingCity(City town, short townIdx)
		{
			this.Owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.Owner.NewGameWizard.DestroyCivilization((short)CityTurn.Owner, 0);
			this.Owner.Startup.ShowMouseCursor();
			return false;
		}

		private bool GrowCity(City town, short townIdx)
		{
			if ((short)((town.TrueExtent + 1) * (short)this.round.CostFactor) > town.GrainTally)
			{
				return true;
			}

			town.TrueExtent++;

			town.GrainTally = (short)(town.Has(CityStructure.Granary) ? (short)(5 * town.TrueExtent) + 5 : 0);

			if (town.TrueExtent > 10 && !town.Has(CityStructure.Aqueduct) && (this.Owner.World.DiagMarkers & 4) != 0)
			{
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

		private void CountCityUnits(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			this.round.Workers = (ushort)(town.TrueExtent + 1);
			CityTurn.AttackersAway = 0;
			CityTurn.SpecialSlots = 0;
			CityTurn.UnitUpkeep = 0;
			CityTurn.Settlers = 0;

			CountSpecialSlots(townIdx);

			for (ushort troop = 0; troop < UnitMemory.TroopTally; troop++)
			{
				this.round.Index = troop;

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) != townIdx)
				{
					continue;
				}

				if (this.troops.PullSigned(owner, troop, UnitMemory.Kind) < 0x1a)
				{
					CityTurn.SpecialSlots = (ushort)(CityTurn.SpecialSlots + 1);

					if (town.TrueExtent < (short)CityTurn.SpecialSlots ||
						(this.Owner.World.Participants[owner].RegimeKind > 1 && (this.Owner.World.DiagMarkers & 2) != 0))
					{
						CityTurn.UnitUpkeep = (ushort)(CityTurn.UnitUpkeep + 1);
					}

					if (this.troops.KindField(owner, troop, UnitKindTable.AssaultPower) != 0 &&
						(this.troops.KindField(owner, troop, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troop, UnitMemory.Horiz) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troop, UnitMemory.Vert) != (byte)(sbyte)town.Place.Y))
					{
						CityTurn.AttackersAway = (ushort)(CityTurn.AttackersAway + 1);
					}
				}

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0)
				{
					CityTurn.Settlers = (ushort)(CityTurn.Settlers + 1);
				}
			}

			this.round.Index = UnitMemory.TroopTally;
		}

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

				if (owner != 0 && (this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
				{
					this.round.SetFrame(unavailable, 1);
				}

				this.round.Value = this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				ushort shared = (ushort)(short)((10 * this.Owner.TownDeltas[tile].X) + (this.Owner.TownDeltas[tile].Y * 2));

				if (this.round.Value != 0xffff && this.round.Value != CityTurn.Owner)
				{
					this.round.SetFrame(0x8e - shared, 1);
					this.round.SetFrame(unavailable, 1);

					if (this.round.Value == (ushort)this.Owner.World.PersonParticipantIdx)
					{
						CityTurn.HumanTileNear = 1;
					}
				}

				if (this.round.Frame(0x8e - shared) != 0)
				{
					this.round.SetFrame(unavailable, 1);
				}

				if (this.round.Frame(unavailable) == 0)
				{
					continue;
				}

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

		private bool KeepWorkers(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			this.round.WorkersHigh = 0;
			this.round.WorkersLow = 0;

			if (!this.Owner.World.IsHuman(owner) && ((townIdx + this.Owner.World.RoundTally) & 3) == 0)
			{
				if ((town.ConditionMarker & 1) == 0 && CityTurn.HumanTileNear == 0)
				{
					return true;
				}
			}

			ushort tile = 0;

			for (; (short)tile < (short)CityTurn.Tiles; tile++)
			{
				this.round.Index = tile;

				if (TestWorker(townIdx, tile))
				{
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}
			}

			this.round.Index = tile;

			if ((short)this.round.Workers >= 0)
			{
				this.round.Workers = (ushort)(town.TrueExtent + 1);

				for (tile = 0; (short)tile < (short)CityTurn.Tiles; tile++)
				{
					this.round.Index = tile;

					if (this.round.Workers == 0 || !TestWorker(townIdx, tile))
					{
						continue;
					}

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

		private bool TestWorker(short townIdx, ushort tile)
		{
			City town = this.Owner.World.Towns[townIdx];
			uint mask = 1u << (tile & 0xff);

			return ((mask & town.LaborerMarkers0) | ((mask >> 16) & town.LaborerMarkers1)) != 0;
		}

		private void PickWorkers(short townIdx, short marker, bool pick)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (pick)
			{
				if ((short)this.round.Workers > 0 && this.round.CityTileTaken == 0)
				{
					this.round.CityTileTaken = 1;
					DrawCityTile(townIdx, 20, marker);
					this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | 0x10);
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}

				while (true)
				{
					int neededFood = (town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers);
					int availableFood = ((short)this.round.Workers / 2) + CityTurn.Food;
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
						{
							continue;
						}

						short horizLoc = (short)this.round.TileHoriz;
						short vertLoc = (short)this.round.TileVert;

						this.round.Amount = MayCityWorkTile(horizLoc, vertLoc, 0);
						this.round.TileValue = (ushort)(MayCityWorkTile(horizLoc, vertLoc, 1) << 1);

						if (this.round.Workers == 1 && CityTurn.Shields == 0 && this.round.TileValue == 0)
						{
							continue;
						}

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
					{
						break;
					}

					TakeWorker(townIdx, marker);
				}

				if (CityTurn.Owner != (ushort)this.Owner.World.PersonParticipantIdx)
				{
					if ((town.ConditionMarker & 1) != 0)
					{
						CityTurn.Specialists = (ushort)(CityTurn.Specialists + 1);
					}
				}

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
						{
							continue;
						}

						short horizLoc = (short)this.round.TileHoriz;
						short vertLoc = (short)this.round.TileVert;

						short divisor = (short)this.Owner.Helpers.ClampValue(
							(short)CityTurn.Food - (town.TrueExtent * 2) - ((short)this.round.SettlerFood * (short)CityTurn.Settlers), 1, 99);
						short weight = (short)(16 / divisor);

						short yield = (short)MayCityWorkTile(horizLoc, vertLoc, 0);
						this.round.TileFood = (ushort)yield;
						this.round.TileValue = (ushort)(yield * weight);

						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 1);
						this.round.TileShields = (ushort)yield;

						divisor = (short)this.Owner.Helpers.ClampValue((short)CityTurn.Shields - (short)CityTurn.UnitUpkeep, 1, 99);
						this.round.TileValue = (ushort)(this.round.TileValue + ((short)(3 * town.TrueExtent) / divisor * yield));

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
					{
						break;
					}

					TakeWorker(townIdx, marker);
				}
			}

			uint workers = (uint)((int)(short)this.round.Workers << 0x1a) +
				(((uint)this.round.WorkersHigh << 16) | this.round.WorkersLow);
			town.LaborerMarkers0 = (ushort)workers;
			town.LaborerMarkers1 = (ushort)(workers >> 16);
			this.round.WorkersGained = (ushort)-(short)this.round.Workers;
		}

		private bool FindFreeTile(short townIdx, ushort tile)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (this.round.Frame(0x3e - (tile << 1)) != 0)
			{
				return false;
			}

			this.round.TileHoriz = (ushort)(short)(town.Place.X + this.Owner.TownDeltas[tile].X);
			this.round.TileVert = (ushort)(short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
			return true;
		}

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

		private void PrepareCitizensAndFactories(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			this.round.Specialists = this.round.Workers;

			ushort citizen = 0;

			for (; (short)citizen < (short)this.round.Specialists; citizen++)
			{
				this.round.Index = citizen;

				if (ObtainCitizenType(citizen) == 0)
				{
					AssignCitizenType(citizen, 3);
				}
			}

			for (citizen = this.round.Specialists; (short)citizen < 8; citizen++)
			{
				this.round.Index = citizen;
				AssignCitizenType(citizen, 0);
			}

			this.round.Index = citizen;

			this.round.FactoryPower = 0;
			this.round.FactoryBonus = 0;
			Turn.PollutionFactor = 1;

			if (town.Has(CityStructure.Factory))
			{
				this.round.FactoryBonus = 2;
			}

			if (town.Has(CityStructure.ManufacturingPlant))
			{
				this.round.FactoryBonus = 4;
			}

			if (town.Has(CityStructure.PowerPlant))
			{
				this.round.FactoryPower = 2;
			}

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
				short marvelIdx = this.Owner.World.MarvelTownIdx[15];

				this.round.Continent = this.Owner.MapOps.ObtainContinentID(this.Owner.World.Towns[marvelIdx].Place.X, this.Owner.World.Towns[marvelIdx].Place.Y);

				if (this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y) == this.round.Continent)
				{
					Turn.PollutionFactor = 2;
					this.round.FactoryPower = 2;
				}
			}

			if (town.Has(CityStructure.RecyclingCenter))
			{
				Turn.PollutionFactor = 3;
			}

			this.round.FactoryPower = (ushort)this.Owner.Helpers.ClampValue((short)this.round.FactoryPower, 0, (short)this.round.FactoryBonus);
			this.round.ShieldsKept = CityTurn.Shields;
			this.round.TradeKept = CityTurn.Trade;

			short shields = (short)CityTurn.Shields;
			short power = (short)((short)((short)this.round.FactoryPower * shields) / 4);
			short factory = (short)((short)this.round.FactoryBonus * shields);
			CityTurn.Shields = (ushort)(CityTurn.Shields + ((ushort)((factory / 4) + power)));
		}
	}
}
