namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		private enum ProductionStep
		{
			UnitBuilt,

			NotEnough,

			Improvement,

			SettlersWait,

			CityLost,
		}

		private ProductionStep ProduceInCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			ushort surplus = (ushort)(CityTurn.Food - ((ushort)((short)this.round.SettlerFood * (short)CityTurn.Settlers)) - (ushort)(town.TrueExtent * 2));
			town.GrainTally += (short)surplus;

			this.round.Value = (ushort)(CityTurn.Shields - CityTurn.UnitUpkeep);

			if ((town.ConditionMarker & 1) != 0)
			{
				this.round.Value = 0;
			}

			town.MaterialsTally += (short)this.round.Value;

			if (town.PresentMakingIdx < 0)
			{
				return ProductionStep.Improvement;
			}

			short cost = (short)((short)Tables.UnitKindWord(town.PresentMakingIdx, UnitKindTable.Price) *
				(short)this.round.CostFactor);

			if (cost > town.MaterialsTally)
			{
				return ProductionStep.NotEnough;
			}

			if (town.PresentMakingIdx == 0 && town.TrueExtent == 1 && this.Owner.World.HardnessTier == 0)
			{
				return ProductionStep.SettlersWait;
			}

			town.MaterialsTally -= cost;
			this.round.NewUnit = 0xffff;

			if (owner == this.Owner.World.PersonParticipantIdx || town.PresentMakingIdx != 0x1a)
			{
				this.round.NewUnit = this.Owner.UnitOps.CreateUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y);
			}

			if ((this.Owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] & 8) == 0)
			{
				this.Owner.UnitOps.AttachReplayContent(6, (byte)CityTurn.Owner, (byte)town.PresentMakingIdx);
				this.Owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] |= 8;
			}

			if (town.Has(CityStructure.Barracks) && this.round.NewUnit != 0xffff)
			{
				short troopIdx = (short)this.round.NewUnit;
				this.troops.Assign(owner, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(owner, troopIdx, UnitMemory.Status) | 0x20));
			}

			if (this.round.NewUnit == 0xffff)
			{
				return ProductionStep.UnitBuilt;
			}

			if (town.PresentMakingIdx != 0)
			{
				return ProductionStep.UnitBuilt;
			}

			if (town.TrueExtent <= 1 && this.Owner.World.Participants[owner].TownTally <= 1)
			{
				return ProductionStep.UnitBuilt;
			}

			sbyte size = town.TrueExtent;
			town.TrueExtent--;

			if (size != 1)
			{
				return ProductionStep.UnitBuilt;
			}

			this.Owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.round.NewUnit = this.Owner.UnitOps.CreateUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y);
			this.Owner.NewGameWizard.DestroyCivilization((short)CityTurn.Owner, 0);
			this.Owner.Startup.ShowMouseCursor();

			return ProductionStep.CityLost;
		}

		private void FinishUnitOfCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIdx;

			if (owner == person || this.round.NewUnit == 0xffff)
			{
				town.MaterialsTally = 0;

				if (this.round.NewUnit == 0xffff || (town.PresentMakingIdx != 0 && town.PresentMakingIdx < 0x1a))
				{
					return;
				}

				CityTurn.Again = 1;
				EmptyMessage()
					.City(townIdx)
					.Text(" builds ");
				this.Owner.Words.Append((ushort)((town.PresentMakingIdx * UnitKindTable.Stride) + UnitKindTable.Origin));
				this.Owner.Words.Append(".\n");
				Screen.DialogStyle = 3;
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);
				return;
			}

			if (town.PresentMakingIdx == 0x1b)
			{
				this.round.Found = 0xffff;
				this.round.Best = 0xffff;

				for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					this.round.OtherCity = otherIdx;
					City other = this.Owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == owner || other.ConditionMarker == 0xff || other.ParticipantIdx == person ||
						other.RootCommerce <= (short)this.round.Best)
					{
						continue;
					}

					this.round.Best = (ushort)other.RootCommerce;
					this.round.Found = this.round.OtherCity;
				}

				this.round.OtherCity = 0x80;
				this.Owner.Conquest.FoundTradeRoute(owner, (short)this.round.NewUnit, (short)this.round.Found);
			}

			if (town.PresentMakingIdx == 0x1a)
			{
				SendDiplomat(townIdx);
			}

			if (town.PresentMakingIdx == 0x19)
			{
				if (this.Owner.World.Participants[owner].LiveTroops[25] == 1)
				{
					this.Owner.World.Participants[person].TouchParticipantTimeleft = -1;
				}
			}

			this.Owner.Bot.PickNewCityProduction(townIdx);
		}

		private void SendDiplomat(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			this.round.Found = 0xffff;
			this.round.Best = 0x7fff;
			this.round.Selected = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				City other = this.Owner.World.Towns[otherIdx];

				if (other.ParticipantIdx != this.Owner.World.PersonParticipantIdx || other.ConditionMarker == 0xff)
				{
					continue;
				}

				this.round.Value = this.Owner.Helpers.FindNearestUnit(owner, (short)this.round.NewUnit, other.Place.X, other.Place.Y);

				Search.FoundDistance = (ushort)this.Owner.Libc.AbsoluteValue((short)((ushort)(Search.FoundDistance - 3)));

				if ((other.ConditionMarker & 0x20) != 0)
				{
					Search.FoundDistance = 3;
				}

				short best = (short)this.round.Best;
				short distance = (short)Search.FoundDistance;

				if (best < distance)
				{
					continue;
				}

				if (best == distance)
				{
					this.round.Selected = (ushort)(this.round.Selected + 1);

					if (this.Owner.Libc.Dice.DrawInt(this.round.Selected) != 0)
					{
						continue;
					}
				}

				short troopIdx = (short)this.round.Value;

				if (this.Owner.MapOps.ObtainTerrainType(this.troops.PullSigned(owner, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain)
				{
					continue;
				}

				this.round.Continent = this.Owner.MapOps.ObtainContinentID(this.troops.PullSigned(owner, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, troopIdx, UnitMemory.Vert));
				if (this.Owner.MapOps.ObtainContinentID(other.Place.X, other.Place.Y) != this.round.Continent)
				{
					continue;
				}

				if (this.round.Best == Search.FoundDistance)
				{
					this.round.Selected = 1;
				}
				else
				{
					this.round.Best = Search.FoundDistance;
				}

				this.round.Found = this.round.Value;
				this.round.Target = this.round.OtherCity;
			}

			this.round.OtherCity = 0x80;

			if ((short)this.round.Best <= 3)
			{
				if ((this.Owner.World.Participants[owner].Relations[this.Owner.World.PersonParticipantIdx] & 2) == 0)
				{
					short fromIdx = (short)this.round.Found;
					this.round.NewUnit = this.Owner.UnitOps.CreateUnit(owner, 0x1a,
						this.troops.PullSigned(owner, fromIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, fromIdx, UnitMemory.Vert));

					if (this.round.NewUnit != 0xffff)
					{
						short troopIdx = (short)this.round.NewUnit;
						City target = this.Owner.World.Towns[this.round.Target];
						this.troops.Assign(owner, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
						this.troops.Assign(owner, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);
					}

					return;
				}
			}

			ushort unitRecord = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			town.MaterialsTally += (short)((short)this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(unitRecord + UnitKindTable.Origin + UnitKindTable.Price)) *
				(short)this.round.CostFactor);
		}

		private bool CompleteImprovement(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			if ((short)((short)Tables.BuildingWordAt((ushort)(-town.PresentMakingIdx * 0x1e), BuildingTable.Price) *
				(short)this.round.CostFactor) > town.MaterialsTally)
			{
				return false;
			}

			this.round.Value = (ushort)-town.PresentMakingIdx;
			short built = (short)this.round.Value;

			if (built > 0x18)
			{
				if (this.Owner.World.MarvelTownIdx[built - 24] != -1)
				{
					this.round.Value = 0xffff;
				}
				else
				{
					this.Owner.World.MarvelTownIdx[built - 24] = townIdx;
					this.Owner.UnitOps.AttachReplayContent(10, (byte)CityTurn.Owner, (byte)(built - 0x18));
				}
			}
			else if (TestImprovement(townIdx, built))
			{
				this.round.Value = 0xffff;
			}

			if (this.round.Value == 0xffff)
			{
				return false;
			}

			if (built == 0x29)
			{
				Message("Sensors report a\nNUCLEAR WEAPONS test\nnear ")
					.City(townIdx)
					.Text("!\n");
				this.Owner.Services.PerformSound(0x24, 0);
				this.Owner.Paper.ShowNewspaper(-1);
				this.Owner.Services.PerformSound(1, 0);
			}

			if (built < 0x16)
			{
				uint mask = 1u << ((byte)(built - 1) & 0xff);
				town.StructureMarkers0 |= (ushort)mask;
				town.StructureMarkers1 |= (ushort)(mask >> 16);
			}

			town.MaterialsTally -= (short)((short)Tables.BuildingWord(-town.PresentMakingIdx, BuildingTable.Price) *
				(short)this.round.CostFactor);

			bool humanPlayed = this.Owner.World.IsHuman(owner);

			if (!humanPlayed && built <= 0x18)
			{
				if (built == 1)
				{
					town.PresentMakingIdx = -2;
				}

				town.PresentMakingIdx = (sbyte)(byte)this.Owner.Advisors.PickCityProduction(owner, townIdx);

				StartMakingUnit(town, owner);

				return true;
			}

			if (humanPlayed)
			{
				CityTurn.Again = 1;
			}
			else
			{
				this.Owner.World.ChartSight[town.Place.X, town.Place.Y] = -1;
			}

			EmptyMessage()
				.City(townIdx)
				.Text(" builds\n");
			this.Owner.Words.Append(GameTables.BuildingRecordText(built));
			this.Owner.Words.Append(".\n");

			bool screen = false;

			if (owner == this.Owner.World.PersonParticipantIdx && (this.Owner.World.SessionPrefMarkers & 8) != 0 && (built <= 0x15 || built > 0x18))
			{
				if ((town.ConditionMarker & 0x10) == 0 && built != 1)
				{
					screen = this.Owner.Startup.AskForDisk(1, 0) != 0;
				}
			}

			if (screen)
			{
				this.Owner.CityScene.ShowCityScreen(townIdx, (short)this.round.Value);
			}
			else
			{
				this.Owner.Paper.ShowNewspaper(townIdx);
			}

			town.MaterialsTally = 0;
			return true;
		}

		private bool TestImprovement(short townIdx, short improvement)
		{
			City town = this.Owner.World.Towns[townIdx];
			uint mask = 1u << ((byte)(improvement - 1) & 0xff);

			return ((mask & town.StructureMarkers0) | ((mask >> 16) & town.StructureMarkers1)) != 0;
		}

		private void BuildSpaceshipPart(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIdx;
			short part = (short)this.round.Value;

			if (part < 0x16 || part > 0x18 || (this.Owner.World.StarshipMarkers & (1 << (owner & 0xff))) != 0)
			{
				return;
			}

			if (owner == person)
			{
				this.Owner.RocketScreen.AttachSpaceshipPart(owner, (ushort)(part - 22));
				return;
			}

			this.round.Best = this.Owner.RocketScreen.ComputeSpaceshipSuccess(owner, (short)((ushort)(part - 0x16)));

			if (this.round.Best != 0 && part < 0x18)
			{
				ushort refund = (ushort)((short)Tables.BuildingWordAt((ushort)(part * 0x1e), BuildingTable.Price) * (short)this.round.CostFactor);
				town.MaterialsTally += (short)refund;

				if (town.PresentMakingIdx <= -0x16 && town.PresentMakingIdx >= -0x18)
				{
					town.PresentMakingIdx = (sbyte)(byte)((byte)-part - 1);
				}

				this.round.Best = (ushort)(this.round.Best - 1);
			}

			bool launch;
			short success = (short)this.round.Best;

			if ((this.Owner.World.StarshipMarkers & (1 << (owner & 0xff))) != 0 || this.Owner.World.BotStarshipVictoryRatio < 40 || success != 0)
			{
				launch = success != 0;
			}
			else if (this.Owner.Advisors.PlayerKnowsTechnology(person, 0x28) == 0)
			{
				launch = false;
			}
			else if (this.Owner.World.BotStarshipVictoryRatio > 75)
			{
				launch = true;
			}
			else
			{
				Player ahead = this.Owner.World.Participants[person];
				Player computer = this.Owner.World.Participants[owner];

				if (ahead.Standing > computer.Standing)
				{
					launch = true;
				}
				else
				{
					launch = false;
					bool decided = false;

					if ((this.Owner.World.StarshipMarkers & (1 << (person & 0xff))) != 0)
					{
						short computerArrives = (short)(ushort)(computer.StarshipArrivalSeason - this.Owner.World.Season);
						short humanArrives = (short)(ushort)(ahead.StarshipArrivalSeason - this.Owner.World.Season);

						if (computerArrives <= humanArrives)
						{
							launch = true;
							decided = true;
						}
					}

					if (!decided)
					{
						launch = ((ushort)this.Owner.World.StarshipMarkers & (ushort)(1 << (byte)(((person & 0xff) + 8) & 0xff))) != 0 &&
							this.Owner.World.Participants[this.Owner.World.LiveRealms].Gold > 1000;
					}
				}
			}

			if (launch)
			{
				this.Owner.RocketScreen.FireSpaceship(owner);
			}
		}

		private void ApplyImprovement(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIdx;
			short built = (short)this.round.Value;

			if (built == 1)
			{
				for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					this.round.OtherCity = otherIdx;
					City other = this.Owner.World.Towns[otherIdx];

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
					told = (this.Owner.World.Participants[person].Relations[owner] & 0x40) != 0;
				}

				if (told)
				{
					this.Owner.Words.Clear();

					if (owner != person)
					{
						Message("Diplomats report:\n")
							.Style(1);
					}

					this.Owner.Words.Append(Tables.CivilizationNameAt((ushort)(owner << 1)));
					this.Owner.Words.Append(" capital\nmoved to ");
					this.Owner.Conquest.ObtainCityLabel(townIdx);
					this.Owner.Words.Append(".\n");
					Screen.DialogStyle = 5;
					this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

					this.Owner.World.Participants[owner].HorizBegin = (short)town.Place.X;
					town.StructureMarkers0 |= 1;
					town.StructureMarkers1 |= 0;
				}
			}

			if (built == 0x26)
			{
				this.Owner.Startup.ShowMouseCursorFully();
				this.Owner.Advisors.UncoverTechnology(owner, 0);
				this.Owner.Advisors.UncoverTechnology(owner, 0);
				this.Owner.Startup.HideMouseCursorToSavedLevel();
			}

			if (built != 0x2b || owner != person)
			{
				return;
			}

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				City other = this.Owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || other.ParticipantIdx == owner)
				{
					continue;
				}

				other.ShownExtent = other.TrueExtent;

				this.Owner.World.ChartSight[other.Place.X, other.Place.Y] |= (sbyte)(byte)(1 << (owner & 0x1f));
				this.Owner.MapOps.KeepKnownTileImprovements(other.Place.X, other.Place.Y);
			}

			this.round.OtherCity = 0x80;
		}

		private void AdviseHumanProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			if (owner != this.Owner.World.PersonParticipantIdx)
			{
				return;
			}

			if ((town.ConditionMarker & 0x10) == 0)
			{
				return;
			}

			if (town.MaterialsTally != 0 && (town.PresentMakingIdx >= 0 || this.round.Value != 0xffff))
			{
				return;
			}

			this.round.Value = this.Owner.Advisors.PickCityProduction(owner, townIdx);
			DrawingBox.TypefaceIdx = 2;

			if (this.round.Value == 0x63)
			{
				town.ConditionMarker &= 0xef;
				return;
			}

			CityTurn.Again = 0;

			StopMakingUnit(town, owner);
			town.PresentMakingIdx = (sbyte)(byte)this.round.Value;
			StartMakingUnit(town, owner);
		}

		private void BuyForComputerCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];

			if ((town.ConditionMarker & 0x10) != 0)
			{
				this.Owner.Bot.PickNewCityProduction(townIdx);
			}

			this.round.Amount = 0;

			ushort continent = this.Owner.MapOps.ObtainContinentID((short)this.round.Horiz, (short)this.round.Vert);
			this.round.Approach = continent >= this.Owner.World.Participants.Length ? (ushort)0 : (ushort)this.Owner.World.Participants[continent].Landmasses[owner].Approach;
			ushort approach = this.round.Approach;

			if ((approach == 1 || approach == 2 || approach == 5) && this.round.Value != 0)
			{
				if (town.PresentMakingIdx >= 0)
				{
					if (Tables.UnitKindWord(town.PresentMakingIdx, UnitKindTable.TroopGroup) == approach)
					{
						this.round.Amount = (ushort)(player.Gold / 64);
					}
				}
			}

			if (((ushort)this.Owner.World.StarshipMarkers & (ushort)(1 << (byte)(((this.Owner.World.PersonParticipantIdx & 0xff) + 8) & 0xff))) != 0)
			{
				if (-town.PresentMakingIdx >= 0x16 && -town.PresentMakingIdx <= 0x18)
				{
					this.round.Amount = (ushort)(player.Gold / 128);
				}
			}

			if ((town.ConditionMarker & 1) != 0 && town.PresentMakingIdx < 0 && town.MaterialsTally != 0)
			{
				this.round.Amount = (ushort)this.Owner.Helpers.ClampValue(
					((short)Tables.BuildingWord(-town.PresentMakingIdx, BuildingTable.Price) * (short)this.round.CostFactor) -
						town.MaterialsTally,
					0, player.Gold / 8);
			}

			bool unowned = (ushort)(this.Owner.MapOps.ObtainTileOwner((short)this.round.Horiz, (short)this.round.Vert) + 1) == 0;

			if ((unowned || (town.ConditionMarker & 0x10) != 0) && town.PresentMakingIdx >= 0 && town.MaterialsTally != 0)
			{
				BuyShortfall(townIdx, player.Gold / 3);
			}

			if (town.PresentMakingIdx == -1 && town.MaterialsTally != 0)
			{
				BuyShortfall(townIdx, player.Gold / 3);
			}

			if (town.ConditionMarker == 0x19)
			{
				if (player.LiveTroops[25] == 0)
				{
					if (town.MaterialsTally != 0)
					{
						BuyShortfall(townIdx, player.Gold / 4);
					}
				}
			}

			if (player.Gold > 2000)
			{
				this.round.Amount = (ushort)(this.round.Amount + ((ushort)(player.Gold / 512)));
			}

			town.MaterialsTally += (short)this.round.Amount;
			player.Gold -= (short)((ushort)(this.round.Amount << 1));
			town.ConditionMarker &= 0xef;
		}

		private void BuyShortfall(short townIdx, int limit)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.Amount = (ushort)this.Owner.Helpers.ClampValue(
				((short)Tables.UnitKindWord(town.PresentMakingIdx, UnitKindTable.Price) * (short)this.round.CostFactor) -
					town.MaterialsTally,
				0, limit);
		}
	}
}
