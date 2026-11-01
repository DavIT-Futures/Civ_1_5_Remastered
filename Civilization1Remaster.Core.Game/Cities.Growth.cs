namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		private bool FeedCity(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;

			town.ConditionMarker &= 0x7f;

			if (town.GrainTally < 0)
			{
				this.round.Value = 0xffff;
				ushort troop = 0;

				for (; troop < UnitMemory.TroopTally; troop++)
				{
					this.round.Index = troop;

					if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0 && this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) == townIndex)
					{
						this.round.Value = troop;
						break;
					}
				}

				if (troop == UnitMemory.TroopTally)
				{
					this.round.Index = troop;
				}

				if (owner == this.Owner.World.PersonParticipantIndex)
				{
					Message("Food storage exhausted\nin ")
						.City(townIndex)
						.Text((ushort)(this.round.Value != 0xffff ? 0x2510 : 0x2522));
					this.Owner.Services.PerformSound(0x24, 0);
					this.Owner.Paper.ShowNewspaper(townIndex);
					this.Owner.Services.PerformSound(1, 0);
				}

				if (this.round.Value != 0xffff)
				{
					this.Owner.UnitOperations.DestroyUnit(owner, (short)this.round.Value);
				}
				else
				{
					town.TrueExtent--;

					if (town.TrueExtent == 0)
					{
						return LoseStarvingCity(town, townIndex);
					}
				}

				town.GrainTally = 0;
				this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horizontal, this.round.Vertical);
			}

			return GrowCity(town, townIndex);
		}

		private bool LoseStarvingCity(City town, short townIndex)
		{
			this.Owner.Advisors.FinalizeProduction(townIndex, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.Owner.NewGameWizard.DestroyCivilization((short)CityTurn.Owner, 0);
			this.Owner.Startup.ShowMouseCursor();
			return false;
		}

		private bool GrowCity(City town, short townIndex)
		{
			if ((short)((town.TrueExtent + 1) * (short)this.round.CostFactor) > town.GrainTally)
			{
				return true;
			}

			town.TrueExtent++;

			town.GrainTally = (short)(town.Has(CityStructure.Granary) ? (short)(5 * town.TrueExtent) + 5 : 0);

			if (town.TrueExtent > 10 && !town.Has(CityStructure.Aqueduct) && (this.Owner.World.DiagonalMarkers & 4) != 0)
			{
				town.TrueExtent--;

				if (CityTurn.Owner == (ushort)this.Owner.World.PersonParticipantIndex)
				{
					Screen.DialogStyle = 4;
					EmptyMessage()
						.City(townIndex)
						.Text(" requires an AQUEDUCT\nfor further growth.\n")
						.Dialog(100, 80);
				}
			}

			this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horizontal, this.round.Vertical);
			return true;
		}

		private void CountCityUnits(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;

			this.round.Workers = (ushort)(town.TrueExtent + 1);
			CityTurn.AttackersAway = 0;
			CityTurn.SpecialSlots = 0;
			CityTurn.UnitUpkeep = 0;
			CityTurn.Settlers = 0;

			CountSpecialSlots(townIndex);

			for (ushort troop = 0; troop < UnitMemory.TroopTally; troop++)
			{
				this.round.Index = troop;

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) != townIndex)
				{
					continue;
				}

				if (this.troops.PullSigned(owner, troop, UnitMemory.Kind) < 0x1a)
				{
					CityTurn.SpecialSlots = (ushort)(CityTurn.SpecialSlots + 1);

					if (town.TrueExtent < (short)CityTurn.SpecialSlots ||
						(this.Owner.World.Participants[owner].RegimeKind > 1 && (this.Owner.World.DiagonalMarkers & 2) != 0))
					{
						CityTurn.UnitUpkeep = (ushort)(CityTurn.UnitUpkeep + 1);
					}

					if (this.troops.KindField(owner, troop, UnitKindTable.AssaultPower) != 0 &&
						(this.troops.KindField(owner, troop, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troop, UnitMemory.Horizontal) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troop, UnitMemory.Vertical) != (byte)(sbyte)town.Place.Y))
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

		private void MarkUnavailableTiles(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			CityTurn.HumanTileNear = 0;
			ushort tile = 0;

			for (; (short)tile < (short)CityTurn.Tiles; tile++)
			{
				this.round.Index = tile;
				int unavailable = 0x3e - (ushort)(tile << 1);
				this.round.SetFrame(unavailable, 0);

				short horizontalLocation = (short)(town.Place.X + this.Owner.TownDeltas[tile].X);
				short verticalLocation = (short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
				this.round.TileHorizontal = (ushort)horizontalLocation;
				this.round.TileVertical = (ushort)verticalLocation;

				short owner = (short)CityTurn.Owner;

				if (owner != 0 && (this.Owner.World.ChartSight[(ushort)horizontalLocation, (ushort)verticalLocation] & (1 << (owner & 0xff))) == 0)
				{
					this.round.SetFrame(unavailable, 1);
				}

				this.round.Value = this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation);

				ushort shared = (ushort)(short)((10 * this.Owner.TownDeltas[tile].X) + (this.Owner.TownDeltas[tile].Y * 2));

				if (this.round.Value != 0xffff && this.round.Value != CityTurn.Owner)
				{
					this.round.SetFrame(0x8e - shared, 1);
					this.round.SetFrame(unavailable, 1);

					if (this.round.Value == (ushort)this.Owner.World.PersonParticipantIndex)
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
				town.LaborerMarkersLow &= (ushort)mask;
				town.LaborerMarkersHigh &= (ushort)(mask >> 16);
			}

			this.round.Index = tile;

			for (ushort slot = 0; slot < 4; slot++)
			{
				this.round.Index = slot;
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)((slot << 1) + 0x70da), 0);
			}

			this.round.Index = 4;
		}

		private bool KeepWorkers(short townIndex, short marker)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;

			this.round.WorkersHigh = 0;
			this.round.WorkersLow = 0;

			if (!this.Owner.World.IsHuman(owner) && ((townIndex + this.Owner.World.RoundTally) & 3) == 0)
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

				if (TestWorker(townIndex, tile))
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

					if (this.round.Workers == 0 || !TestWorker(townIndex, tile))
					{
						continue;
					}

					this.round.SetFrame(0x3e - ((ushort)(tile << 1)), 1);

					DrawCityTile(townIndex, tile, marker);

					this.round.Workers = (ushort)(this.round.Workers - 1);
					uint mask = 1u << (tile & 0xff);
					this.round.WorkersLow = (ushort)(this.round.WorkersLow | (ushort)mask);
					this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | (ushort)(mask >> 16));
				}

				this.round.Index = tile;
			}
			else
			{
				if (marker == 0 && owner == this.Owner.World.PersonParticipantIndex)
				{
					Message("Population decrease\nin ")
						.City(townIndex)
						.Text(".\n")
						.Style(4)
						.Dialog(100, 80);
					CityTurn.Again = 1;
				}

				this.round.Workers = (ushort)(town.TrueExtent + 1);
			}

			return this.round.Workers != CityTurn.Specialists && this.round.Workers != 0;
		}

		private bool TestWorker(short townIndex, ushort tile)
		{
			City town = this.Owner.World.Towns[townIndex];
			uint mask = 1u << (tile & 0xff);

			return ((mask & town.LaborerMarkersLow) | ((mask >> 16) & town.LaborerMarkersHigh)) != 0;
		}

		private void PickWorkers(short townIndex, short marker, bool pick)
		{
			City town = this.Owner.World.Towns[townIndex];

			if (pick)
			{
				if ((short)this.round.Workers > 0 && this.round.CityTileTaken == 0)
				{
					this.round.CityTileTaken = 1;
					DrawCityTile(townIndex, 20, marker);
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

						if (!FindFreeTile(townIndex, tile))
						{
							continue;
						}

						short horizontalLocation = (short)this.round.TileHorizontal;
						short verticalLocation = (short)this.round.TileVertical;

						this.round.Amount = MayCityWorkTile(horizontalLocation, verticalLocation, 0);
						this.round.TileValue = (ushort)(MayCityWorkTile(horizontalLocation, verticalLocation, 1) << 1);

						if (this.round.Workers == 1 && CityTurn.Shields == 0 && this.round.TileValue == 0)
						{
							continue;
						}

						this.round.TileValue = (ushort)(this.round.TileValue + MayCityWorkTile(horizontalLocation, verticalLocation, 2));

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

					TakeWorker(townIndex, marker);
				}

				if (CityTurn.Owner != (ushort)this.Owner.World.PersonParticipantIndex)
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

						if (!FindFreeTile(townIndex, tile))
						{
							continue;
						}

						short horizontalLocation = (short)this.round.TileHorizontal;
						short verticalLocation = (short)this.round.TileVertical;

						short divisor = (short)this.Owner.Helpers.ClampValue(
							(short)CityTurn.Food - (town.TrueExtent * 2) - ((short)this.round.SettlerFood * (short)CityTurn.Settlers), 1, 99);
						short weight = (short)(16 / divisor);

						short yield = (short)MayCityWorkTile(horizontalLocation, verticalLocation, 0);
						this.round.TileFood = (ushort)yield;
						this.round.TileValue = (ushort)(yield * weight);

						yield = (short)MayCityWorkTile(horizontalLocation, verticalLocation, 1);
						this.round.TileShields = (ushort)yield;

						divisor = (short)this.Owner.Helpers.ClampValue((short)CityTurn.Shields - (short)CityTurn.UnitUpkeep, 1, 99);
						this.round.TileValue = (ushort)(this.round.TileValue + ((short)(3 * town.TrueExtent) / divisor * yield));

						yield = (short)MayCityWorkTile(horizontalLocation, verticalLocation, 2);
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

					TakeWorker(townIndex, marker);
				}
			}

			uint workers = (uint)((int)(short)this.round.Workers << 0x1a) +
				(((uint)this.round.WorkersHigh << 16) | this.round.WorkersLow);
			town.LaborerMarkersLow = (ushort)workers;
			town.LaborerMarkersHigh = (ushort)(workers >> 16);
			this.round.WorkersGained = (ushort)-(short)this.round.Workers;
		}

		private bool FindFreeTile(short townIndex, ushort tile)
		{
			City town = this.Owner.World.Towns[townIndex];

			if (this.round.Frame(0x3e - (tile << 1)) != 0)
			{
				return false;
			}

			this.round.TileHorizontal = (ushort)(short)(town.Place.X + this.Owner.TownDeltas[tile].X);
			this.round.TileVertical = (ushort)(short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
			return true;
		}

		private void TakeWorker(short townIndex, short marker)
		{
			ushort tile = this.round.Found;

			this.round.SetFrame(0x3e - ((ushort)(tile << 1)), 1);

			DrawCityTile(townIndex, tile, marker);

			uint mask = 1u << (tile & 0xff);
			this.round.WorkersLow = (ushort)(this.round.WorkersLow | (ushort)mask);
			this.round.WorkersHigh = (ushort)(this.round.WorkersHigh | (ushort)(mask >> 16));
			this.round.Workers = (ushort)(this.round.Workers - 1);
		}

		private void PrepareCitizensAndFactories(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			this.round.Horizontal = (ushort)(short)town.Place.X;
			this.round.Vertical = (ushort)(short)town.Place.Y;
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
				short marvelIndex = this.Owner.World.MarvelTownIndex[15];

				this.round.Continent = this.Owner.MapOperations.ObtainContinentIdentifier(this.Owner.World.Towns[marvelIndex].Place.X, this.Owner.World.Towns[marvelIndex].Place.Y);

				if (this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y) == this.round.Continent)
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
