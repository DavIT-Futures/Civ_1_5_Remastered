namespace Civ_1_5_Remastered.Core.Game.Towns
{
	public class CityCapture : GameComponent, ICityCapture
	{
		private const ushort TradeGoodsNamesAddress = 0x1972;

		public CityCapture(IGameEngine owner)
			: base(owner)
		{
		}

		public void SeizeCity(short participantIndex, short townIndex, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"SeizeCity({participantIndex}, {townIndex}, {marker})");

			if (townIndex < 0 || townIndex >= this.Owner.World.Towns.Length)
			{
				return;
			}

			short person = this.Owner.World.PersonParticipantIndex;
			City town = this.Owner.World.Towns[townIndex];

			if (town == null || town.ConditionMarker == 0xff || town.ParticipantIndex == participantIndex)
			{
				return;
			}

			ushort formerIndex = (ushort)town.ParticipantIndex;
			ushort horizontalLocation = (ushort)(short)town.Place.X;
			ushort verticalLocation = (ushort)(short)town.Place.Y;

			if (participantIndex == person)
			{
				this.Owner.World.Participants[formerIndex].TouchParticipantTimeLeft = -2;
			}

			if (formerIndex != (ushort)person)
			{
				if ((town.Has(CityStructure.Palace) || (byte)town.PresentMakingIndex == 0xff) && marker == 0 && town.Has(CityStructure.Palace))
				{
					if (this.Owner.World.Participants[formerIndex].TownTally > 4 &&
						this.Owner.World.Participants[person].Standing < this.Owner.World.Participants[formerIndex].Standing)
					{
						SplitCapturedCivilization(participantIndex, town, (short)formerIndex);
					}
				}
			}

			if (town.Has(CityStructure.Palace))
			{
				DestroySpaceship((short)formerIndex);
			}

			if (formerIndex == (ushort)person)
			{
				this.Owner.World.Participants[participantIndex].TouchParticipantTimeLeft = -1;
			}

			short plunder = (short)this.Owner.Helpers.ClampValue((short)ComputeCityPlunder(townIndex), 0, this.Owner.World.Participants[formerIndex].Gold);

			if (!BarbariansRazeCity(participantIndex, townIndex, horizontalLocation, verticalLocation))
			{
				town.MaterialsTally = 0;
				town.ConditionMarker &= 0xae;
				town.StructureMarkersLow &= 0xfbb6;

				if (marker == 0)
				{
					int half = this.Owner.Runtime.Dice.DrawInteger(2);

					town.StructureMarkersLow &= (ushort)(0xaaaa >> (byte)half);
					town.StructureMarkersHigh &= 0;
				}

				if (marker == 0 || town.TrueExtent > 1)
				{
					town.TrueExtent--;

					if (town.TrueExtent < 1)
					{
						this.Owner.Advisors.FinalizeProduction(townIndex, horizontalLocation, verticalLocation);
					}
				}
			}

			this.Owner.World.Participants[formerIndex].Gold -= plunder;

			if (participantIndex != 0)
			{
				this.Owner.World.Participants[participantIndex].Gold += plunder;
			}

			if (town.TrueExtent != 0)
			{
				MarkCapturedCity(participantIndex, town, horizontalLocation, verticalLocation);
			}

			town.ParticipantIndex = (sbyte)participantIndex;

			if (participantIndex == person || formerIndex == (ushort)person)
			{
				AnnounceCapture(participantIndex, townIndex, (short)formerIndex, horizontalLocation, verticalLocation, plunder);
			}
			else
			{
				ReportCaptureBySpies(participantIndex, townIndex, (short)formerIndex);
			}

			ChooseTechnologyToTake(participantIndex, (short)formerIndex);

			this.Owner.MapOperations.MapAssignCityOwner(participantIndex, (short)horizontalLocation, (short)verticalLocation);

			ushort troopIndex;

			for (troopIndex = 0; troopIndex < 0x80; troopIndex++)
			{
				ushort troop = (ushort)((ushort)(0x600 * formerIndex) + (ushort)(0xc * troopIndex));

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff ||
					(sbyte)this.Troops.PullAt(troop, UnitMemory.BaseTown) != townIndex)
				{
					continue;
				}

				if (this.Troops.PullAt(troop, UnitMemory.Horizontal) == (byte)(sbyte)town.Place.X &&
					this.Troops.PullAt(troop, UnitMemory.Vertical) == (byte)(sbyte)town.Place.Y &&
					town.TrueExtent != 0)
				{
					continue;
				}

				this.Owner.UnitOperations.DestroyUnit((short)formerIndex, (short)troopIndex);
			}

			if (participantIndex == person && this.Owner.World.Participants[person].RegimeKind < 4)
			{
				town.ConditionMarker |= 1;
			}

			if (town.TrueExtent != 0)
			{
				if (town.PresentMakingIndex >= 0)
				{
					this.Owner.World.Participants[formerIndex].TroopsInsideMaking[town.PresentMakingIndex]--;
				}

				if (participantIndex == person)
				{
					this.Owner.CityOperations.HandleCity(townIndex, 1);

					this.Owner.TurnHooks.RepaintMainScreen();
				}
				else
				{
					town.PresentMakingIndex = 0;

					town.PresentMakingIndex = (sbyte)this.Owner.Advisors.PickCityProduction(participantIndex, townIndex);

					if (town.PresentMakingIndex >= 0)
					{
						this.Owner.World.Participants[participantIndex].TroopsInsideMaking[town.PresentMakingIndex]++;
					}
				}
			}

			this.Owner.NewGameWizard.DestroyCivilization((short)formerIndex, participantIndex);
		}

		public void DestroySpaceship(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateSpaceship({participantIndex})");

			ushort built = (ushort)(1 << participantIndex);
			ushort launched = (ushort)(1 << (participantIndex + 8));

			Message(Tables.CivilizationName(participantIndex))
				.Text(" spaceship\n");

			if ((this.Owner.World.StarshipMarkers & launched) != 0)
			{
				this.Owner.Words.Append((this.Owner.World.StarshipMarkers & built) != 0 ? (ushort)0x28ca : (ushort)0x28dd);

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}

			this.Owner.World.StarshipMarkers &= (short)~built;
			this.Owner.World.StarshipMarkers &= (short)~launched;

			this.Owner.NewGameWizard.ClearSpaceshipContent(participantIndex);
		}

		public ushort ComputeCityPlunder(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ComputeCityPlunder({townIndex})");

			short size = this.Owner.World.Towns[townIndex].TrueExtent;
			Player victim = this.Owner.World.Participants[(ushort)this.Owner.World.Towns[townIndex].ParticipantIndex];
			short divisor = (short)(victim.GrandTownExtent + 1);

			short plunder = CentralProcessor.SignedQuotient(32000, size) > victim.Gold ? CentralProcessor.SignedQuotient((short)(victim.Gold * size), divisor) : (short)(CentralProcessor.SignedQuotient(victim.Gold, divisor) * size);

			return (ushort)plunder;
		}

		public void ChooseTechnologyToTake(short firstParticipantIndex, short secondParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ChooseTechnologyToTake({firstParticipantIndex}, {secondParticipantIndex})");

			ushort bestTechnology = 0;

			if (firstParticipantIndex != 0 && secondParticipantIndex != 0)
			{
				ushort count = 0;
				ushort[] technologies = new ushort[0x48];

				this.Owner.Words.Set("Select one...\n ");

				short bestScore = -1;

				for (short technologyIdentifier = 0x47; technologyIdentifier >= 0; technologyIdentifier--)
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(firstParticipantIndex, technologyIdentifier) == 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(secondParticipantIndex, technologyIdentifier) != 0)
					{
						this.Owner.Words.Append(GameTables.TechnologyText(technologyIdentifier));
						this.Owner.Words.Append("\n ");

						count++;
						technologies[count - 1] = (ushort)technologyIdentifier;

						if ((short)this.Owner.Advisors.ObtainTechnologyInformation(firstParticipantIndex, (ushort)technologyIdentifier) > bestScore)
						{
							bestScore = (short)this.Owner.Advisors.ObtainTechnologyInformation(firstParticipantIndex, (ushort)technologyIdentifier);
							bestTechnology = (ushort)technologyIdentifier;
						}
					}
				}

				if (count != 0)
				{
					if (firstParticipantIndex == this.Owner.World.PersonParticipantIndex)
					{
						ushort choice;

						do
						{
							if (King.Answer == 0xffff)
							{
								choice = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 32);
							}
							else
							{
								choice = this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
							}
						}
						while (choice == 0xffff);

						short chosenTechnology = (short)technologies[choice];

						this.Owner.Advisors.EarnTechnology(firstParticipantIndex, chosenTechnology, secondParticipantIndex);
					}
					else
					{
						if (secondParticipantIndex == this.Owner.World.PersonParticipantIndex)
						{
							this.Owner.Words.Set(Tables.CivilizationPlural(firstParticipantIndex));
							this.Owner.Words.Append(" take\n");
							this.Owner.Words.Append(GameTables.TechnologyText(bestTechnology));
							this.Owner.Words.Append(".\n");

							if (King.Answer == 0xffff)
							{
								this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);
							}
							else
							{
								this.Owner.Audience.ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
							}
						}

						this.Owner.Advisors.EarnTechnology(firstParticipantIndex, (short)bestTechnology, secondParticipantIndex);
						this.Owner.Computer.RefreshCityProduction(firstParticipantIndex, 0xffff);
					}
				}
			}
		}

		public void ObtainCityLabel(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainCityLabel({townIndex})");

			if (townIndex != -1)
			{
				byte townLabelIndex = this.Owner.World.Towns[townIndex].LabelIndex;
				ushort charactersDisplacement = (ushort)(TextBuffer.Address + this.Owner.Words.Length);

				for (int index = 0; index < 0xd; index++)
				{
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(charactersDisplacement + index), (byte)this.Owner.World.TownLabels[townLabelIndex][index]);
				}
			}
			else
			{
				this.Owner.Words.Append("NONE");
			}
		}

		public short WaitForKeyPressOrMouseClick()
		{
			using var _ = this.Processor.Journal.Scope("AwaitForKeyPressOrMouseClick()");

			this.Owner.TurnLogic.WaitForKey();

			do
			{
				this.Owner.Startup.ReadMouseState();

				if (this.Owner.MenuPointerState != 0)
				{
					break;
				}
			}
			while (this.Owner.Runtime.KeyAvailable() == 0);

			short key = 0;
			if (this.Owner.MenuPointerState == 0 && (key = this.Owner.Runtime.ReadKey()) == 0)
			{
				key = this.Owner.Runtime.ReadKey();
			}

			return key;
		}

		public ushort FoundTradeRoute(short participantIndex, short troopIndex, short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"FoundTradeRoute({participantIndex}, {troopIndex}, {townIndex})");

			UnitMemory troops = new UnitMemory(this.Processor);
			ushort homeIndex = (ushort)troops.PullSigned(participantIndex, troopIndex, UnitMemory.BaseTown);
			City town = this.Owner.World.Towns[townIndex];
			City home = this.Owner.World.Towns[homeIndex];

			int distance = this.Owner.Helpers.ObtainDistance(town.Place.X, home.Place.X, town.Place.Y, home.Place.Y);
			short revenue = (short)((home.RootCommerce + town.RootCommerce) * (short)(distance + 0xa));
			revenue = (short)(revenue / 0x18);

			short homeHorizontal = (short)home.Place.X;

			if (this.Owner.MapOperations.ObtainContinentIdentifier(homeHorizontal, homeHorizontal) == this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y))
			{
				revenue = (short)(revenue / 2);
			}

			if (town.ParticipantIndex == participantIndex)
			{
				revenue = (short)(revenue / 2);
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, 0x31) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIndex, 0x26) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			this.Owner.World.Participants[participantIndex].Gold += revenue;
			this.Owner.World.Participants[participantIndex].InquiryHeadway += revenue;

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				Message(this.Processor.PullGlobalWord((ushort)(((troopIndex & 7) << 1) + TradeGoodsNamesAddress)))
					.Text(" caravan from ");

				ObtainCityLabel((short)homeIndex);

				this.Owner.Words.Append("\narrives in ");

				ObtainCityLabel(townIndex);

				this.Owner.Words.Append("\nTrade route established\nRevenue: $");

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(revenue, 10));

				this.Owner.Words.Append(".\n");

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

				this.Owner.TurnHooks.DrawStatusBar();
			}

			this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);

			short trade = RouteTrade(townIndex, participantIndex);
			int replacedIndex = -1;

			for (int routeIndex = 0; routeIndex < 3; routeIndex++)
			{
				ushort routeTownIndex = (byte)home.CommerceTownIndices[routeIndex];

				if (routeTownIndex == (ushort)townIndex)
				{
					replacedIndex = -1;
					break;
				}

				short routeTrade = routeTownIndex == 0xff ? (short)-1 : RouteTrade((short)routeTownIndex, participantIndex);

				if (routeTrade < trade)
				{
					trade = routeTrade;
					replacedIndex = routeIndex;
				}
			}

			if (replacedIndex != -1)
			{
				home.CommerceTownIndices[replacedIndex] = (sbyte)townIndex;
			}

			return 1;
		}

		private void SplitCapturedCivilization(short participantIndex, City town, short formerIndex)
		{
			if (this.Owner.Secession.DivideCivilization(formerIndex) != 0)
			{
				return;
			}

			town.ParticipantIndex = (sbyte)participantIndex;

			ushort capitalIndex = this.Owner.Secession.ComputeDistanceToCapital(formerIndex);

			this.Owner.World.Towns[capitalIndex].PresentMakingIndex = -1;

			town.ParticipantIndex = (sbyte)formerIndex;
		}

		private bool BarbariansRazeCity(short participantIndex, short townIndex, ushort horizontalLocation, ushort verticalLocation)
		{
			if (participantIndex != 0)
			{
				return false;
			}

			ushort troop = (ushort)(0xc * this.Owner.MapOperations.ObtainUnitAtTile((short)horizontalLocation, (short)verticalLocation));

			sbyte kind = (sbyte)this.Troops.PullAt((ushort)(0x600 * participantIndex) + troop, UnitMemory.Kind);

			if ((short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) > 2 || Turn.NoConquestAnimation != 0)
			{
				return false;
			}

			this.Owner.Advisors.FinalizeProduction(townIndex, horizontalLocation, verticalLocation);

			this.Owner.World.Towns[townIndex].TrueExtent = 0;

			return true;
		}

		private void MarkCapturedCity(short participantIndex, City town, ushort horizontalLocation, ushort verticalLocation)
		{
			this.Owner.UnitOperations.AttachReplayContent(9, (byte)participantIndex, town.LabelIndex, (byte)horizontalLocation, (byte)verticalLocation);

			this.Owner.Computer.DisableStrategicLocation(participantIndex, 0, (short)horizontalLocation, (short)verticalLocation, 4);

			town.ConditionMarker &= 0x9b;

			ushort markIndex;

			for (markIndex = 0; markIndex < 0x14; markIndex++)
			{
				ushort markHorizontal = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(markIndex) + (short)horizontalLocation);
				ushort markVertical = (ushort)(Tables.DirectionVertical(markIndex) + verticalLocation);

				this.Owner.Services.AssignDot(2, (short)markHorizontal + 0x50, (short)markVertical, (ushort)participantIndex);
			}
		}

		private void AnnounceCapture(short participantIndex, short townIndex, short formerIndex, ushort horizontalLocation, ushort verticalLocation, short plunder)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			City town = this.Owner.World.Towns[townIndex];

			this.Owner.MapOperations.KeepKnownTileImprovements((short)horizontalLocation, (short)verticalLocation);

			if (formerIndex == person)
			{
				this.Owner.UnitOperations.PanMapToPosition(person, (short)horizontalLocation, (short)verticalLocation);
			}

			this.Owner.MapOperations.DrawMapTile((short)horizontalLocation, (short)verticalLocation);

			Message(Tables.CivilizationPlural(participantIndex))
				.Text(" capture\n");

			ObtainCityLabel(townIndex);

			this.Owner.Words.Append(". ");

			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(plunder, 10));

			this.Owner.Words.Append(" gold\npieces plundered.\n");

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIndex].ChiefVisuals, LeaderTable.ShortTune), 0);

			if ((this.Owner.World.SessionPreferenceMarkers & 8) != 0)
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.CityScene.ShowCityScreen(townIndex, -2);

				this.Owner.Startup.ShowMouseCursor();

				this.Owner.Startup.AskForDisk(1, 1);

				this.Owner.CityScene.DrawCitySceneryForPlayer(participantIndex);

				this.Owner.Startup.AskForDisk(0, 1);

				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				this.Owner.Paper.ShowNewspaper(townIndex);
			}

			this.Owner.Services.PerformSound(1, 0);

			town.ShownExtent = participantIndex == person ? (sbyte)0 : town.TrueExtent;
		}

		private void ReportCaptureBySpies(short participantIndex, short townIndex, short formerIndex)
		{
			ushort[] known = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Relations;

			if ((known[participantIndex] & 0x40) == 0 && (known[formerIndex] & 0x40) == 0)
			{
				return;
			}

			Screen.DialogStyle = 0x0;

			Message("Spies report:\n")
				.Text(Tables.CivilizationPlural((ushort)participantIndex))
				.Text(this.Owner.World.Towns[townIndex].TrueExtent != 0 ? (ushort)0x2893 : (ushort)0x28a1)
				.Text(Tables.CivilizationName((ushort)formerIndex))
				.Text(" city\nof ");

			ObtainCityLabel(townIndex);

			this.Owner.Words.Append(".\n");

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
		}

		private short RouteTrade(short townIndex, short participantIndex)
		{
			short trade = this.Owner.World.Towns[townIndex].RootCommerce;

			return this.Owner.World.Towns[townIndex].ParticipantIndex == participantIndex ? trade : (short)(trade << 1);
		}
	}
}
