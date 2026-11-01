using System;

namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		public ushort HandleCity(short townIndex, short marker)
		{
			using var _ = this.Processor.Journal.Scope($"HandleCity({townIndex}, {marker})");

			CityRound outer = this.round;
			this.round = new CityRound(this.Processor);

			ushort result = 0;

			if (this.Owner.World.Towns[townIndex].ConditionMarker != 0xff)
			{
				result = RunCity(townIndex, marker);
			}

			this.round = outer;

			return result;
		}

		private void RestoreShieldsAndTrade()
		{
			CityTurn.Shields = this.round.ShieldsKept;
			CityTurn.Trade = this.round.TradeKept;
		}

		private void StopMakingUnit(City town, short owner)
		{
			this.Owner.World.Participants[owner].StopMaking(town.PresentMakingIndex);
		}

		private void StartMakingUnit(City town, short owner)
		{
			this.Owner.World.Participants[owner].StartMaking(town.PresentMakingIndex);
		}

		private void RepickCityProduction(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;

			StopMakingUnit(town, owner);
			town.PresentMakingIndex = (sbyte)(byte)this.Owner.Advisors.PickCityProduction(owner, townIndex);
			StartMakingUnit(town, owner);
		}

		private void ApplyCivilOrder(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			bool human = this.Owner.World.IsHuman(owner);

			this.round.Value = (ushort)(CityTurn.Happy - CityTurn.Unhappy);

			if ((short)this.round.Value < 0)
			{
				if (human)
				{
					Message("Civil Disorder in\n")
						.City(townIndex)
						.Text("! Mayor\nflees in panic.\n");
					this.Owner.Services.PerformSound(0x24, 0);

					if ((this.Owner.World.SessionPreferenceMarkers & 8) != 0 && (town.ConditionMarker & 1) == 0 && Turn.RiotShown == 0)
					{
						this.Owner.CityScene.ShowCityScreen(townIndex, -2);
						this.Owner.CityScene.RiotAnimation();
						this.Owner.Startup.AskForDisk(0, 1);
						this.Owner.TurnHooks.RepaintMainScreen();
						Turn.RiotShown = 1;
					}
					else
					{
						Screen.DialogStyle = 4;
						this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
					}

					this.Owner.Services.PerformSound(1, 0);

					if ((town.ConditionMarker & 1) == 0)
					{
						CityTurn.Again = 1;

						if ((this.Owner.World.SessionPreferenceMarkers & 1) != 0)
						{
							this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicDisorder);
						}
					}
				}

				if ((town.ConditionMarker & 1) == 0)
				{
					town.ConditionMarker |= 1;

					if (!human)
					{
						RepickCityProduction(townIndex);
					}

					this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horizontal, this.round.Vertical);
				}
				else if (this.Owner.World.Participants[owner].RegimeKind == 5)
				{
					this.Owner.Services.PerformSound(0x24, 0);
					Message("Discontented citizens of\n")
						.City(townIndex)
						.Text(" revolt:\nGovernment Collapses!\n");

					if (owner == this.Owner.World.PersonParticipantIndex)
					{
						this.Owner.Paper.ShowNewspaper(townIndex);
						this.Owner.World.Participants[owner].Relations[0] |= 4;
					}

					this.Owner.Politics.SwitchGovernment(owner, 0);
					this.Owner.Services.PerformSound(1, 0);
				}

				return;
			}

			if ((town.ConditionMarker & 1) != 0)
			{
				town.ConditionMarker &= 0xfe;

				if (human)
				{
					Screen.DialogStyle = 4;
					Message("Order restored\nin ")
						.City(townIndex)
						.Text(".\n")
						.Dialog(100, 80);
				}
				else
				{
					RepickCityProduction(townIndex);
				}

				this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horizontal, this.round.Vertical);
			}

			if (this.Owner.World.Participants[owner].RegimeKind != 0)
			{
				this.Owner.World.Participants[owner].Gold += (short)CityTurn.Taxes;
			}
		}

		private void ApplyCelebration(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			bool human = this.Owner.World.IsHuman(owner);

			bool celebrates = CityTurn.Unhappy == 0 && town.TrueExtent > 2 &&
				(short)((town.TrueExtent + 1) / 2) <= (short)CityTurn.Happy && player.RegimeKind != 0;

			if (celebrates)
			{
				if ((town.ConditionMarker & 0x40) == 0)
				{
					if (human && player.RegimeKind != 0)
					{
						Message("'We love the ")
							.Text(Tables.GovernmentTitle(player.RegimeKind))
							.Text("'\nday celebrated in\n")
							.City(townIndex)
							.Text("!\n");
						this.Owner.Services.PerformSound(0x22, 0);

						if ((this.Owner.World.SessionPreferenceMarkers & 8) != 0)
						{
							this.Owner.CityScene.ShowCityScreen(townIndex, -2);
							this.Owner.CityScene.DrawCitySceneryForHuman();
							this.Owner.Startup.AskForDisk(0, 1);
							this.Owner.TurnHooks.RepaintMainScreen();
						}
						else
						{
							this.Owner.Paper.ShowNewspaper(townIndex);
						}

						this.Owner.Services.PerformSound(1, 0);
					}

					town.ConditionMarker |= 0x40;
				}
				else if (player.RegimeKind >= 4)
				{
					short eaten = (short)((town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers));

					if (eaten < (short)CityTurn.Food && (town.TrueExtent < 10 || town.Has(CityStructure.Aqueduct)))
					{
						town.TrueExtent++;
					}
				}

				return;
			}

			if ((short)this.round.Value < 0 || (town.ConditionMarker & 0x40) == 0)
			{
				return;
			}

			if (human && player.RegimeKind != 0)
			{
				this.Owner.Services.PerformSound(0x23, 0);
				Message("'We love the ")
					.Text(Tables.GovernmentTitle(player.RegimeKind))
					.Text("'\ncelebration canceled\nin ")
					.City(townIndex)
					.Text(".\n");
				this.Owner.Paper.ShowNewspaper(townIndex);
				this.Owner.Services.PerformSound(1, 0);
			}

			town.ConditionMarker &= 0xbf;
		}

		private void ApplyResearch()
		{
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIndex;
			Player player = this.Owner.World.Participants[owner];
			Player human = this.Owner.World.Participants[person];

			this.round.Value = CityTurn.Science;

			if (owner == person)
			{
				bool finished = human.PresentInquiryIndex != -1 ? this.Owner.Advisors.PlayerKnowsTechnology(owner, human.PresentInquiryIndex) != 0 : player.InquiryHeadway != 0;

				if (finished)
				{
					human.PresentInquiryIndex = -1;
					UncoverTechnologyWhenFlagged(owner);
					player.InquiryHeadway = 0;
				}
			}

			if (player.RegimeKind == 0)
			{
				this.round.Value = 0;
			}

			player.InquiryHeadway += (short)this.round.Value;

			if (owner == person && this.Owner.World.HardnessTier == 0 && human.PresentInquiryIndex >= 0 &&
				(this.Owner.World.ScienceLeadingFoundVia[human.PresentInquiryIndex] & 7) != 0)
			{
				player.InquiryHeadway += (short)this.round.Value;
			}

			if (PlayerEnjoysWonderEffect(owner, 0x14) != 0)
			{
				player.InquiryHeadway += (short)((short)this.round.Value / 2);
			}

			this.round.Measure = (ushort)(owner != person ? 14 - this.Owner.World.HardnessTier : (this.Owner.World.HardnessTier << 1) + 6);
			this.round.Measure = (ushort)(this.round.Measure + Turn.TechnologyLevel);

			if (owner == person && (short)(11 - player.FoundScienceTally) > (short)this.round.Measure)
			{
				this.round.Measure = (ushort)(11 - player.FoundScienceTally);
			}

			if (owner == person && CityTurn.Again != 0)
			{
				return;
			}

			short progress = (short)(player.InquiryHeadway / (this.Owner.World.Season < 0 ? 1 : 2));

			if (progress > (short)(player.FoundScienceTally * (short)this.round.Measure))
			{
				player.InquiryHeadway = 0;
				UncoverTechnologyWhenFlagged(owner);
			}
		}

		private void UncoverTechnologyWhenFlagged(short participant)
		{
			if ((this.Owner.World.DiagonalMarkers & 8) == 0)
			{
				return;
			}

			this.Owner.Startup.ShowMouseCursorFully();
			this.Owner.Advisors.UncoverTechnology(participant, 0);
			this.Owner.Startup.HideMouseCursorToSavedLevel();
		}

		private short CountPollution(City town)
		{
			short divisor = (short)Turn.PollutionFactor;

			if (divisor == 0)
			{
				throw new Exception("Division by zero");
			}

			int quotient = (short)CityTurn.Shields / divisor;

			if (quotient != (short)quotient)
			{
				throw new Exception("Division error");
			}

			short pollution = (short)((short)(quotient - 20) + ((short)(town.TrueExtent * (short)Turn.ContentAdvances) / 4));
			this.round.Pollution = (ushort)pollution;

			return pollution;
		}

		private bool ApplyScoreAndPollution(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			short person = this.Owner.World.PersonParticipantIndex;

			player.Tally += (short)(town.TrueExtent + CityTurn.Happy - CityTurn.Unhappy);

			if (owner != person)
			{
				return false;
			}

			short pollution = CountPollution(town);
			ushort chance = (ushort)(0x100 - (short)((short)(player.FoundScienceTally * this.Owner.World.HardnessTier) / 2));

			if ((short)(pollution << 1) > (short)(ushort)this.Owner.Runtime.Dice.DrawInteger(chance))
			{
				this.round.Value = (ushort)this.Owner.Runtime.Dice.DrawInteger(20);
				var delta = this.Owner.TownDeltas[this.round.Value];
				this.round.TileHorizontal = (ushort)(short)(delta.X + (short)this.round.Horizontal);
				this.round.TileVertical = (ushort)(short)(delta.Y + (short)this.round.Vertical);
				short horizontal = (short)this.round.TileHorizontal;
				short vertical = (short)this.round.TileVertical;

				if ((this.Owner.MapOperations.ObtainTileImprovements(horizontal, vertical) & 0x40) == 0 && this.Owner.MapOperations.ObtainTerrainType(horizontal, vertical) != 0xa &&
					(this.Owner.MapOperations.ObtainTileImprovements(horizontal, vertical) & 1) == 0)
				{
					AttachSmog(horizontal, vertical);
					this.Owner.MapOperations.FocusMapView(person, horizontal - 8, vertical - 6);
					this.Owner.MapOperations.RepaintMapTile(horizontal, vertical);
					Screen.DialogStyle = 6;
					Message("Pollution near ")
						.City(townIndex)
						.Text(".\n")
						.Dialog(80, 64);
				}
			}

			if ((town.ConditionMarker & 1) != 0 && town.Has(CityStructure.NuclearPlant) && this.Owner.Runtime.Dice.DrawInteger(3) == 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x42) == 0)
			{
				short horizontal = (short)this.round.Horizontal;
				short vertical = (short)this.round.Vertical;

				this.Owner.MapOperations.FocusMapView(person, horizontal - 8, vertical - 6);
				this.Owner.Diplomats.DrawExplosion(horizontal, vertical);
				this.Owner.Combat.EnactNuclearFallout(horizontal, vertical);
				town.Remove(CityStructure.NuclearPlant);
				this.Owner.Services.PerformSound(0x24, 0);
				Message("Nuclear Catastrophe\nin ")
					.City(townIndex)
					.Text("!\nContamination feared!\n");
				this.Owner.Paper.ShowNewspaper(townIndex);
				this.Owner.Services.PerformSound(1, 0);
			}

			return true;
		}

		private void PayImprovementUpkeep(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			ushort improvement = 1;

			for (; (short)improvement < 0x18; improvement++)
			{
				this.round.Item = improvement;
				uint bit = 1u << (byte)improvement;

				if ((town.Structures & bit) == 0 || player.RegimeKind == 0)
				{
					continue;
				}

				player.Gold -= (short)Tables.BuildingWord(improvement + 1, BuildingTable.Upkeep);

				if (improvement == 1)
				{
					if (this.Owner.World.HardnessTier >= 2)
					{
						player.Gold--;
					}

					if (this.Owner.World.HardnessTier >= 4)
					{
						player.Gold--;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x22) != 0)
					{
						player.Gold--;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x25) != 0)
					{
						player.Gold--;
					}
				}

				if (player.Gold >= 0)
				{
					continue;
				}

				player.Gold = 0;
				town.Structures &= ~bit;
				EmptyMessage()
					.City(townIndex)
					.Text("\ncan't maintain\n")
					.Text(GameTables.BuildingText(improvement))
					.Text(".\n")
					.Style(4)
					.Dialog(100, 80);
				player.Gold += (short)(0xa * Tables.BuildingWord(improvement + 1, BuildingTable.Price));
			}

			this.round.Item = improvement;
		}

		private ushort FinishHandleCity(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			if ((town.ConditionMarker & 4) != 0)
			{
				if (this.Owner.UnitOperations.ContainsAdjacentForeignUnit(town.ParticipantIndex, town.Place.X, town.Place.Y) == 0)
				{
					this.Owner.UnitOperations.KeepUnitsInCityList(townIndex);
				}
			}

			DrawingBox.TypefaceIndex = 1;
			this.Owner.Startup.ShowMouseCursor();

			return (ushort)(CityTurn.Happy - CityTurn.Unhappy);
		}

		private void PrepareCity(short townIndex, short marker)
		{
			City town = this.Owner.World.Towns[townIndex];

			CityTurn.ScreenTab = 0;
			this.round.CursorOn = 0;
			this.round.ListStart = 0;
			this.round.ListEnd = 0;
			this.round.ListFlags = 0;

			this.Owner.Startup.HideMouseCursor();

			for (ushort column = 0; column < 5; column++)
			{
				this.round.Horizontal = column;

				for (ushort row = 0; row < 5; row++)
				{
					this.round.Vertical = row;
					this.round.SetFrame(0xa6 - ((ushort)((column * 0xa) + (row * 2))), 0);
				}

				this.round.Vertical = 5;
			}

			this.round.Horizontal = (ushort)(short)town.Place.X;
			this.round.Vertical = (ushort)(short)town.Place.Y;
			CityTurn.ShownCity = (ushort)townIndex;
			this.round.Distance = 7;
			CityTurn.PalaceDistance = (ushort)(town.Has(CityStructure.Palace) ? 0 : 0x20);

			if (town.MaterialsTally > 999 || town.MaterialsTally < 0)
			{
				town.MaterialsTally = 0;
			}

			for (ushort otherIndex = 0; otherIndex < 0x80; otherIndex++)
			{
				this.round.OtherCity = otherIndex;
				City other = this.Owner.World.Towns[otherIndex];

				if (other.ConditionMarker == 0xff || otherIndex == (ushort)townIndex)
				{
					continue;
				}

				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance((short)this.round.Horizontal, other.Place.X, (short)this.round.Vertical, other.Place.Y);
				this.round.Target = (ushort)distance;

				if (distance < (short)this.round.Distance && distance != 0)
				{
					this.round.Distance = (ushort)distance;
				}

				if (other.ParticipantIndex == town.ParticipantIndex && other.Has(CityStructure.Palace) && distance < (short)CityTurn.PalaceDistance)
				{
					CityTurn.PalaceDistance = (ushort)distance;
				}

				if (distance > 5)
				{
					continue;
				}

				for (ushort tile = 0; tile <= 20; tile++)
				{
					this.round.Index = tile;
					uint mask = 1u << tile;
					if ((ushort)(((mask >> 16) & other.LaborerMarkersHigh) | (ushort)(mask & other.LaborerMarkersLow)) == 0 && tile != 20)
					{
						continue;
					}

					short horizontalLocation = (short)(other.Place.X + this.Owner.TownDeltas[tile].X);
					short verticalLocation = (short)(other.Place.Y + this.Owner.TownDeltas[tile].Y);
					this.round.TileHorizontal = (ushort)horizontalLocation;
					this.round.TileVertical = (ushort)verticalLocation;

					if ((short)(ushort)this.Owner.Runtime.AbsoluteValue((short)((ushort)(horizontalLocation - (short)this.round.Horizontal))) > 2)
					{
						continue;
					}

					if ((short)(ushort)this.Owner.Runtime.AbsoluteValue((short)((ushort)(verticalLocation - (short)this.round.Vertical))) > 2)
					{
						continue;
					}

					int frameOffset = ((horizontalLocation - (short)this.round.Horizontal) * 0xa) +
						((verticalLocation - (short)this.round.Vertical) << 1);
					this.round.SetFrame(0x8e - frameOffset, 1);
				}

				this.round.Index = 21;
			}

			this.round.OtherCity = 0x80;

			this.round.CityTileNeighbour = 0;
			CityTurn.Owner = (ushort)town.ParticipantIndex;
			CityTurn.Tiles = 0x15;
			this.round.MapViewHorizontal = MapView.Horizontal;
			this.round.MapViewVertical = MapView.Vertical;

			short owner = (short)CityTurn.Owner;
			this.round.SettlerFood = (ushort)(this.Owner.World.Participants[owner].RegimeKind <= 1 ? 1 : 2);

			Turn.ContentAdvances = 0;

			if (!town.Has(CityStructure.MassTransit))
			{
				foreach (short advance in new short[] { 0x23, 0x3a, 0x29, 0x3c })
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, advance) != 0)
					{
						Turn.ContentAdvances = (ushort)(Turn.ContentAdvances + 1);
					}
				}
			}

			this.round.CostFactor = 0xa;
			CityTurn.Specialists = (ushort)((((uint)town.LaborerMarkersHigh << 16) | town.LaborerMarkersLow) >> 0x1a);

			if (owner == this.Owner.World.PersonParticipantIndex)
			{
				return;
			}

			this.round.CostFactor = (ushort)-((this.Owner.World.HardnessTier * 2) - 0x10);

			if (this.Owner.World.Season >= 0 &&
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Standing == 7 &&
				this.Owner.World.HardnessTier > 1)
			{
				this.round.CostFactor = (ushort)(this.round.CostFactor - 2);
			}

			if (marker != 0)
			{
				return;
			}

			if ((town.ConditionMarker & 1) == 0)
			{
				CityTurn.Specialists = 0;
			}
			else
			{
				town.LaborerMarkersLow = 0;
				town.LaborerMarkersHigh = 0;
			}
		}

		private ushort RunCity(short townIndex, short marker)
		{
			PrepareCity(townIndex, marker);

			bool fromStart = true;

			while (true)
			{
				if (fromStart && !StartCityRound(townIndex, marker))
				{
					return 0;
				}

				if (!CountCityEconomy(townIndex, marker))
				{
					return 0;
				}

				if (marker != 1)
				{
					break;
				}

				CityScreenStep step = RunCityScreen(townIndex, marker);

				if (step == CityScreenStep.Leave)
				{
					MapView.Horizontal = this.round.MapViewHorizontal;
					MapView.Vertical = this.round.MapViewVertical;
					break;
				}

				fromStart = step == CityScreenStep.Restart;
			}

			if (marker == 0)
			{
				EndCityTurn(townIndex);
			}

			return FinishHandleCity(townIndex);
		}

		private bool StartCityRound(short townIndex, short marker)
		{
			this.round.Horizontal = (ushort)(short)this.Owner.World.Towns[townIndex].Place.X;
			this.round.Vertical = (ushort)(short)this.Owner.World.Towns[townIndex].Place.Y;

			if (marker == 1)
			{
				DrawCityHeaderAndMap(townIndex);
			}

			if (marker == 0 && !FeedCity(townIndex))
			{
				return false;
			}

			CountCityUnits(townIndex);

			MarkUnavailableTiles(townIndex);

			PickWorkers(townIndex, marker, KeepWorkers(townIndex, marker));

			return true;
		}

		private bool CountCityEconomy(short townIndex, short marker)
		{
			PrepareCitizensAndFactories(townIndex);

			if (marker == 0 && !ProduceInTurn(townIndex))
			{
				return false;
			}

			if (marker == 1)
			{
				DrawCityScreenFrames();
			}

			CountCorruptionAndTradeRoutes(townIndex);

			SplitTrade(townIndex);
			CountScienceAndContent(townIndex);

			ApplyLuxuries(townIndex, marker);

			ApplyContentImprovements(townIndex, marker);

			ApplyMilitaryHappiness(townIndex, marker);

			ApplyWonderHappiness(townIndex, marker);

			bool disbanded;

			do
			{
				StartUnitSupport(townIndex);
				disbanded = false;

				for (this.round.Index = 0; (short)this.round.Index < 0x80;
					this.round.Index = (ushort)(this.round.Index + 1))
				{
					if (SupportUnit(townIndex, marker))
					{
						disbanded = true;
						break;
					}
				}
			}
			while (disbanded);

			if (marker == 0)
			{
				CountSupportCosts(townIndex);
			}

			return true;
		}

		private bool ProduceInTurn(short townIndex)
		{
			switch (ProduceInCity(townIndex))
			{
				case ProductionStep.CityLost:
					return false;

				case ProductionStep.SettlersWait:
					break;

				case ProductionStep.Improvement:
					if (CompleteImprovement(townIndex))
					{
						BuildSpaceshipPart(townIndex);
						ApplyImprovement(townIndex);
					}

					AdviseHumanProduction(townIndex);
					break;

				case ProductionStep.NotEnough:
					AdviseHumanProduction(townIndex);
					break;

				default:
					FinishUnitOfCity(townIndex);
					AdviseHumanProduction(townIndex);
					break;
			}

			if (!this.Owner.World.IsHuman(CityTurn.Owner))
			{
				BuyForComputerCity(townIndex);
			}

			return true;
		}

		private CityScreenStep RunCityScreen(short townIndex, short marker)
		{
			DrawImprovementList(townIndex);
			FinishImprovementList(townIndex);

			DrawProductionBox(townIndex);
			DrawShieldsInBox(townIndex);
			DrawFoodBox(townIndex);

			DrawResourceRows(townIndex);
			DrawTradeRow();

			DrawShortfalls(townIndex);
			DrawCitizensAndRoutes(townIndex);
			DrawCityDotAndPollution(townIndex);

			if (CityTurn.Owner != (ushort)this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();
				return CityScreenStep.Leave;
			}

			DrawExitAndBuyButtons(townIndex);
			WaitForCityScreenInput();
			CityScreenStep step = ReadCityScreenInput(townIndex);

			if (step != CityScreenStep.Others)
			{
				return step;
			}

			step = HandleCityScreenButtons(townIndex);

			if (step != CityScreenStep.Others)
			{
				return step;
			}

			if (this.round.Key == 'a')
			{
				ActivateUnitsInCity();
				return CityScreenStep.Redraw;
			}

			if (!PickImprovementToSell(townIndex))
			{
				step = ToggleSpecialist(townIndex);

				if (step != CityScreenStep.Others)
				{
					return step;
				}

				if (this.round.Key == 'p')
				{
					return MoveCityMapCursor(townIndex) ? ToggleCityTile(townIndex, marker) : CityScreenStep.Restart;
				}

				step = HandleListAndRename(townIndex);

				if (step == CityScreenStep.Others)
				{
					return HandleCityScreenClicks(townIndex, marker);
				}

				if (step != CityScreenStep.Sell)
				{
					return step;
				}
			}

			if (SellImprovement(townIndex))
			{
				return CityScreenStep.Restart;
			}

			return HandleCityScreenClicks(townIndex, marker);
		}

		private CityScreenStep HandleCityScreenClicks(short townIndex, short marker)
		{
			if ((short)this.Owner.MenuPointerHorizontal < 0xc8 && (short)this.Owner.MenuPointerVertical < 0x14)
			{
				return ClickCitizen(townIndex);
			}

			if ((short)this.Owner.MenuPointerHorizontal >= 0x64 && (short)this.Owner.MenuPointerHorizontal < 0x2580 && (short)this.Owner.MenuPointerVertical >= 0x74 && ClickUnitOfCity())
			{
				return CityScreenStep.Redraw;
			}

			if ((short)this.Owner.MenuPointerVertical < 0x18 || (short)this.Owner.MenuPointerVertical >= 0x68)
			{
				return CityScreenStep.Leave;
			}

			this.round.DrawHorizontal = (ushort)(((short)this.Owner.MenuPointerHorizontal / 16) - 10);
			this.round.DrawVertical = (ushort)(((short)(this.Owner.MenuPointerVertical - 0x18) / 16) - 2);

			return ToggleCityTile(townIndex, marker);
		}

		private void EndCityTurn(short townIndex)
		{
			if (CityTurn.Owner != 0)
			{
				ApplyCivilOrder(townIndex);
				ApplyCelebration(townIndex);
			}

			ApplyResearch();

			if (ApplyScoreAndPollution(townIndex))
			{
				PayImprovementUpkeep(townIndex);
			}
		}
	}
}
