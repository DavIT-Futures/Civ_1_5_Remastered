using System;

namespace Civilization1Remaster.Core.Game
{
	partial class Cities
	{
		public ushort HandleCity(short townIdx, short marker)
		{
			using var _ = this.Processor.Journal.Scope($"HandleCity({townIdx}, {marker})");

			CityRound outer = this.round;
			this.round = new CityRound(this.Processor);

			ushort result = 0;

			if (this.Owner.World.Towns[townIdx].ConditionMarker != 0xff)
				result = RunCity(townIdx, marker);

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
			this.Owner.World.Participants[owner].StopMaking(town.PresentMakingIdx);
		}

		private void StartMakingUnit(City town, short owner)
		{
			this.Owner.World.Participants[owner].StartMaking(town.PresentMakingIdx);
		}

		private void RepickCityProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			StopMakingUnit(town, owner);
			town.PresentMakingIdx = (sbyte)(byte)this.Owner.Advisors.PickCityProduction(owner, townIdx);
			StartMakingUnit(town, owner);
		}

		private void ApplyCivilOrder(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			bool human = this.Owner.World.IsHuman(owner);

			this.round.Value = (ushort)(CityTurn.Happy - CityTurn.Unhappy);

			if ((short)this.round.Value < 0)
			{
				if (human)
				{
					Message("Civil Disorder in\n")
						.City(townIdx)
						.Text("! Mayor\nflees in panic.\n");
					this.Owner.Services.PerformSound(0x24, 0);

					if ((this.Owner.World.SessionPrefMarkers & 8) != 0 && (town.ConditionMarker & 1) == 0 && Turn.RiotShown == 0)
					{
						this.Owner.CityScene.ShowCityScreen(townIdx, -2);
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

						if ((this.Owner.World.SessionPrefMarkers & 1) != 0)
							this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicDisorder);
					}
				}

				if ((town.ConditionMarker & 1) == 0)
				{
					town.ConditionMarker |= 1;

					if (!human)
						RepickCityProduction(townIdx);

					this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
				}
				else if (this.Owner.World.Participants[owner].RegimeKind == 5)
				{
					this.Owner.Services.PerformSound(0x24, 0);
					Message("Discontented citizens of\n")
						.City(townIdx)
						.Text(" revolt:\nGovernment Collapses!\n");

					if (owner == this.Owner.World.PersonParticipantIdx)
					{
						this.Owner.Paper.ShowNewspaper(townIdx);
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
						.City(townIdx)
						.Text(".\n")
						.Dialog(100, 80);
				}
				else
				{
					RepickCityProduction(townIdx);
				}

				this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			}

			if (this.Owner.World.Participants[owner].RegimeKind != 0)
				this.Owner.World.Participants[owner].Gold += (short)CityTurn.Taxes;
		}

		private void ApplyCelebration(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
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
							.City(townIdx)
							.Text("!\n");
						this.Owner.Services.PerformSound(0x22, 0);

						if ((this.Owner.World.SessionPrefMarkers & 8) != 0)
						{
							this.Owner.CityScene.ShowCityScreen(townIdx, -2);
							this.Owner.CityScene.DrawCitySceneryForHuman();
							this.Owner.Startup.AskForDisk(0, 1);
							this.Owner.TurnHooks.RepaintMainScreen();
						}
						else
						{
							this.Owner.Paper.ShowNewspaper(townIdx);
						}

						this.Owner.Services.PerformSound(1, 0);
					}

					town.ConditionMarker |= 0x40;
				}
				else if (player.RegimeKind >= 4)
				{
					short eaten = (short)((town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers));

					if (eaten < (short)CityTurn.Food && (town.TrueExtent < 10 || town.Has(CityStructure.Aqueduct)))
						town.TrueExtent++;
				}

				return;
			}

			if ((short)this.round.Value < 0 || (town.ConditionMarker & 0x40) == 0)
				return;

			if (human && player.RegimeKind != 0)
			{
				this.Owner.Services.PerformSound(0x23, 0);
				Message("'We love the ")
					.Text(Tables.GovernmentTitle(player.RegimeKind))
					.Text("'\ncelebration canceled\nin ")
					.City(townIdx)
					.Text(".\n");
				this.Owner.Paper.ShowNewspaper(townIdx);
				this.Owner.Services.PerformSound(1, 0);
			}

			town.ConditionMarker &= 0xbf;
		}

		private void ApplyResearch()
		{
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIdx;
			Player player = this.Owner.World.Participants[owner];
			Player human = this.Owner.World.Participants[person];

			this.round.Value = CityTurn.Science;

			if (owner == person)
			{
				bool finished = human.PresentInquiryIdx != -1 ?
					this.Owner.Advisors.PlayerKnowsTechnology(owner, human.PresentInquiryIdx) != 0 :
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

			if (owner == person && this.Owner.World.HardnessTier == 0 && human.PresentInquiryIdx >= 0 &&
				(this.Owner.World.ScienceLeadingFoundVia[human.PresentInquiryIdx] & 7) != 0)
			{
				player.InquiryHeadway += (short)this.round.Value;
			}

			if (PlayerEnjoysWonderEffect(owner, 0x14) != 0)
				player.InquiryHeadway += (short)((short)this.round.Value / 2);

			this.round.Measure = (ushort)(owner != person ? 14 - this.Owner.World.HardnessTier : (this.Owner.World.HardnessTier << 1) + 6);
			this.round.Measure = (ushort)(this.round.Measure + Turn.TechnologyLevel);

			if (owner == person && (short)(11 - player.FoundScienceTally) > (short)this.round.Measure)
				this.round.Measure = (ushort)(11 - player.FoundScienceTally);

			if (owner == person && CityTurn.Again != 0)
				return;

			short progress = (short)(player.InquiryHeadway / (this.Owner.World.Season < 0 ? 1 : 2));

			if (progress > (short)(player.FoundScienceTally * (short)this.round.Measure))
			{
				player.InquiryHeadway = 0;
				UncoverTechnologyWhenFlagged(owner);
			}
		}

		private void UncoverTechnologyWhenFlagged(short participant)
		{
			if ((this.Owner.World.DiagMarkers & 8) == 0)
				return;

			this.Owner.Startup.ShowMouseCursorFully();
			this.Owner.Advisors.UncoverTechnology(participant, 0);
			this.Owner.Startup.HideMouseCursorToSavedLevel();
		}

		private short CountPollution(City town)
		{
			short divisor = (short)Turn.PollutionFactor;

			if (divisor == 0)
				throw new Exception("Division by zero");

			int quotient = (short)CityTurn.Shields / divisor;

			if (quotient != (short)quotient)
				throw new Exception("Division error");

			short pollution = (short)((short)(quotient - 20) + ((short)(town.TrueExtent * (short)Turn.ContentAdvances) / 4));
			this.round.Pollution = (ushort)pollution;

			return pollution;
		}

		private bool ApplyScoreAndPollution(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			short person = this.Owner.World.PersonParticipantIdx;

			player.Tally += (short)(town.TrueExtent + CityTurn.Happy - CityTurn.Unhappy);

			if (owner != person)
				return false;

			short pollution = CountPollution(town);
			ushort chance = (ushort)(0x100 - (short)((short)(player.FoundScienceTally * this.Owner.World.HardnessTier) / 2));

			if ((short)(pollution << 1) > (short)(ushort)this.Owner.Libc.Dice.DrawInt(chance))
			{
				this.round.Value = (ushort)this.Owner.Libc.Dice.DrawInt(20);
				var delta = this.Owner.TownDeltas[this.round.Value];
				this.round.TileHoriz = (ushort)(short)(delta.X + (short)this.round.Horiz);
				this.round.TileVert = (ushort)(short)(delta.Y + (short)this.round.Vert);
				short horiz = (short)this.round.TileHoriz;
				short vert = (short)this.round.TileVert;

				if ((this.Owner.MapOps.ObtainTileImprovements(horiz, vert) & 0x40) == 0 && this.Owner.MapOps.ObtainTerrainType(horiz, vert) != 0xa &&
					(this.Owner.MapOps.ObtainTileImprovements(horiz, vert) & 1) == 0)
				{
					AttachSmog(horiz, vert);
					this.Owner.MapOps.FocusMapView(person, horiz - 8, vert - 6);
					this.Owner.MapOps.RepaintMapTile(horiz, vert);
					Screen.DialogStyle = 6;
					Message("Pollution near ")
						.City(townIdx)
						.Text(".\n")
						.Dialog(80, 64);
				}
			}

			if ((town.ConditionMarker & 1) != 0 && town.Has(CityStructure.NuclearPlant) && this.Owner.Libc.Dice.DrawInt(3) == 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x42) == 0)
			{
				short horiz = (short)this.round.Horiz;
				short vert = (short)this.round.Vert;

				this.Owner.MapOps.FocusMapView(person, horiz - 8, vert - 6);
				this.Owner.Diplomats.DrawExplosion(horiz, vert);
				this.Owner.Combat.EnactNuclearFallout(horiz, vert);
				town.Remove(CityStructure.NuclearPlant);
				this.Owner.Services.PerformSound(0x24, 0);
				Message("Nuclear Catastrophe\nin ")
					.City(townIdx)
					.Text("!\nContamination feared!\n");
				this.Owner.Paper.ShowNewspaper(townIdx);
				this.Owner.Services.PerformSound(1, 0);
			}

			return true;
		}

		private void PayImprovementUpkeep(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			ushort improvement = 1;

			for (; (short)improvement < 0x18; improvement++)
			{
				this.round.Item = improvement;
				uint bit = 1u << (byte)improvement;

				if ((town.Structures & bit) == 0 || player.RegimeKind == 0)
					continue;

				player.Gold -= (short)Tables.BuildingWord(improvement + 1, BuildingTable.Upkeep);

				if (improvement == 1)
				{
					if (this.Owner.World.HardnessTier >= 2)
						player.Gold--;

					if (this.Owner.World.HardnessTier >= 4)
						player.Gold--;

					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x22) != 0)
						player.Gold--;

					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x25) != 0)
						player.Gold--;
				}

				if (player.Gold >= 0)
					continue;

				player.Gold = 0;
				town.Structures &= ~bit;
				EmptyMessage()
					.City(townIdx)
					.Text("\ncan't maintain\n")
					.Text(GameTables.BuildingText(improvement))
					.Text(".\n")
					.Style(4)
					.Dialog(100, 80);
				player.Gold += (short)(0xa * Tables.BuildingWord(improvement + 1, BuildingTable.Price));
			}

			this.round.Item = improvement;
		}

		private ushort FinishHandleCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if ((town.ConditionMarker & 4) != 0)
			{
				if (this.Owner.UnitOps.ContainsAdjacentForeignUnit(town.ParticipantIdx, town.Place.X, town.Place.Y) == 0)
					this.Owner.UnitOps.KeepUnitsInCityList(townIdx);
			}

			DrawingBox.TypefaceIdx = 1;
			this.Owner.Startup.ShowMouseCursor();

			return (ushort)(CityTurn.Happy - CityTurn.Unhappy);
		}

		private void PrepareCity(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];

			CityTurn.ScreenTab = 0;
			this.round.CursorOn = 0;
			this.round.ListStart = 0;
			this.round.ListEnd = 0;
			this.round.ListFlags = 0;

			this.Owner.Startup.HideMouseCursor();

			for (ushort column = 0; column < 5; column++)
			{
				this.round.Horiz = column;

				for (ushort row = 0; row < 5; row++)
				{
					this.round.Vert = row;
					this.round.SetFrame(0xa6 - ((ushort)(column * 0xa + row * 2)), 0);
				}

				this.round.Vert = 5;
			}

			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			CityTurn.ShownCity = (ushort)townIdx;
			this.round.Distance = 7;
			CityTurn.PalaceDistance = (ushort)(town.Has(CityStructure.Palace) ? 0 : 0x20);

			if (town.MaterialsTally > 999 || town.MaterialsTally < 0)
				town.MaterialsTally = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				City other = this.Owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || otherIdx == (ushort)townIdx)
					continue;

				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance((short)this.round.Horiz, other.Place.X, (short)this.round.Vert, other.Place.Y);
				this.round.Target = (ushort)distance;

				if (distance < (short)this.round.Distance && distance != 0)
					this.round.Distance = (ushort)distance;

				if (other.ParticipantIdx == town.ParticipantIdx && other.Has(CityStructure.Palace) && distance < (short)CityTurn.PalaceDistance)
				{
					CityTurn.PalaceDistance = (ushort)distance;
				}

				if (distance > 5)
					continue;

				for (ushort tile = 0; tile <= 20; tile++)
				{
					this.round.Index = tile;
					uint mask = 1u << tile;
					if ((ushort)(((mask >> 16) & other.LaborerMarkers1) | (ushort)(mask & other.LaborerMarkers0)) == 0 && tile != 20)
						continue;

					short horizLoc = (short)(other.Place.X + this.Owner.TownDeltas[tile].X);
					short vertLoc = (short)(other.Place.Y + this.Owner.TownDeltas[tile].Y);
					this.round.TileHoriz = (ushort)horizLoc;
					this.round.TileVert = (ushort)vertLoc;

					if ((short)(ushort)this.Owner.Libc.AbsoluteValue((short)((ushort)(horizLoc - (short)this.round.Horiz))) > 2)
						continue;

					if ((short)(ushort)this.Owner.Libc.AbsoluteValue((short)((ushort)(vertLoc - (short)this.round.Vert))) > 2)
						continue;

					int frameOffset = (horizLoc - (short)this.round.Horiz) * 0xa +
						((vertLoc - (short)this.round.Vert) << 1);
					this.round.SetFrame(0x8e - frameOffset, 1);
				}

				this.round.Index = 21;
			}

			this.round.OtherCity = 0x80;

			this.round.CityTileNeighbour = 0;
			CityTurn.Owner = (ushort)town.ParticipantIdx;
			CityTurn.Tiles = 0x15;
			this.round.MapViewHoriz = MapView.Horiz;
			this.round.MapViewVert = MapView.Vert;

			short owner = (short)CityTurn.Owner;
			this.round.SettlerFood = (ushort)(this.Owner.World.Participants[owner].RegimeKind <= 1 ? 1 : 2);

			Turn.ContentAdvances = 0;

			if (!town.Has(CityStructure.MassTransit))
			{
				foreach (short advance in new short[] { 0x23, 0x3a, 0x29, 0x3c })
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, advance) != 0)
						Turn.ContentAdvances = (ushort)(Turn.ContentAdvances + 1);
				}
			}

			this.round.CostFactor = 0xa;
			CityTurn.Specialists = (ushort)((((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0) >> 0x1a);

			if (owner == this.Owner.World.PersonParticipantIdx)
				return;

			this.round.CostFactor = (ushort)-(this.Owner.World.HardnessTier * 2 - 0x10);

			if (this.Owner.World.Season >= 0 &&
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Standing == 7 &&
				this.Owner.World.HardnessTier > 1)
			{
				this.round.CostFactor = (ushort)(this.round.CostFactor - 2);
			}

			if (marker != 0)
				return;

			if ((town.ConditionMarker & 1) == 0)
			{
				CityTurn.Specialists = 0;
			}
			else
			{
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
			}
		}

		private ushort RunCity(short townIdx, short marker)
		{
			PrepareCity(townIdx, marker);

			bool fromStart = true;

			while (true)
			{
				if (fromStart && !StartCityRound(townIdx, marker))
					return 0;

				if (!CountCityEconomy(townIdx, marker))
					return 0;

				if (marker != 1)
					break;

				CityScreenStep step = RunCityScreen(townIdx, marker);

				if (step == CityScreenStep.Leave)
				{
					MapView.Horiz = this.round.MapViewHoriz;
					MapView.Vert = this.round.MapViewVert;
					break;
				}

				fromStart = step == CityScreenStep.Restart;
			}

			if (marker == 0)
				EndCityTurn(townIdx);

			return FinishHandleCity(townIdx);
		}

		private bool StartCityRound(short townIdx, short marker)
		{
			this.round.Horiz = (ushort)(short)this.Owner.World.Towns[townIdx].Place.X;
			this.round.Vert = (ushort)(short)this.Owner.World.Towns[townIdx].Place.Y;

			if (marker == 1)
				DrawCityHeaderAndMap(townIdx);

			if (marker == 0 && !FeedCity(townIdx))
				return false;

			CountCityUnits(townIdx);

			MarkUnavailableTiles(townIdx);

			PickWorkers(townIdx, marker, KeepWorkers(townIdx, marker));

			return true;
		}

		private bool CountCityEconomy(short townIdx, short marker)
		{
			PrepareCitizensAndFactories(townIdx);

			if (marker == 0 && !ProduceInTurn(townIdx))
				return false;

			if (marker == 1)
				DrawCityScreenFrames();

			CountCorruptionAndTradeRoutes(townIdx);

			SplitTrade(townIdx);
			CountScienceAndContent(townIdx);

			ApplyLuxuries(townIdx, marker);

			ApplyContentImprovements(townIdx, marker);

			ApplyMilitaryHappiness(townIdx, marker);

			ApplyWonderHappiness(townIdx, marker);

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

			if (marker == 0)
				CountSupportCosts(townIdx);

			return true;
		}

		private bool ProduceInTurn(short townIdx)
		{
			switch (ProduceInCity(townIdx))
			{
				case ProductionStep.CityLost:
					return false;

				case ProductionStep.SettlersWait:
					break;

				case ProductionStep.Improvement:
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

			if (!this.Owner.World.IsHuman(CityTurn.Owner))
				BuyForComputerCity(townIdx);

			return true;
		}

		private CityScreenStep RunCityScreen(short townIdx, short marker)
		{
			DrawImprovementList(townIdx);
			FinishImprovementList(townIdx);

			DrawProductionBox(townIdx);
			DrawShieldsInBox(townIdx);
			DrawFoodBox(townIdx);

			DrawResourceRows(townIdx);
			DrawTradeRow();

			DrawShortfalls(townIdx);
			DrawCitizensAndRoutes(townIdx);
			DrawCityDotAndPollution(townIdx);

			if (CityTurn.Owner != (ushort)this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();
				return CityScreenStep.Leave;
			}

			DrawExitAndBuyButtons(townIdx);
			WaitForCityScreenInput();
			CityScreenStep step = ReadCityScreenInput(townIdx);

			if (step != CityScreenStep.Others)
				return step;

			step = HandleCityScreenButtons(townIdx);

			if (step != CityScreenStep.Others)
				return step;

			if (this.round.Key == 'a')
			{
				ActivateUnitsInCity();
				return CityScreenStep.Redraw;
			}

			if (!PickImprovementToSell(townIdx))
			{
				step = ToggleSpecialist(townIdx);

				if (step != CityScreenStep.Others)
					return step;

				if (this.round.Key == 'p')
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

		private CityScreenStep HandleCityScreenClicks(short townIdx, short marker)
		{
			if ((short)this.Owner.GlobDb3c < 0xc8 && (short)this.Owner.GlobDb3e < 0x14)
				return ClickCitizen(townIdx);

			if ((short)this.Owner.GlobDb3c >= 0x64 && (short)this.Owner.GlobDb3c < 0x2580 && (short)this.Owner.GlobDb3e >= 0x74 && ClickUnitOfCity())
				return CityScreenStep.Redraw;

			if ((short)this.Owner.GlobDb3e < 0x18 || (short)this.Owner.GlobDb3e >= 0x68)
				return CityScreenStep.Leave;

			this.round.DrawHoriz = (ushort)((short)this.Owner.GlobDb3c / 16 - 10);
			this.round.DrawVert = (ushort)((short)(this.Owner.GlobDb3e - 0x18) / 16 - 2);

			return ToggleCityTile(townIdx, marker);
		}

		private void EndCityTurn(short townIdx)
		{
			if (CityTurn.Owner != 0)
			{
				ApplyCivilOrder(townIdx);
				ApplyCelebration(townIdx);
			}

			ApplyResearch();

			if (ApplyScoreAndPollution(townIdx))
				PayImprovementUpkeep(townIdx);
		}
	}
}
