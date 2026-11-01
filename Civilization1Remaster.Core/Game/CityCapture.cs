namespace Civilization1Remaster.Core.Game
{
	public class CityCapture : GameComponent, ICityCapture
	{
		private const ushort TradeGoodsNamesAddress = 0x1972;

		public CityCapture(GameEngine owner)
			: base(owner)
		{
		}

		public void SeizeCity(short participantIdx, short townIdx, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"SeizeCity({participantIdx}, {townIdx}, {marker})");

			short person = this.Owner.World.PersonParticipantIdx;
			City town = this.Owner.World.Towns[townIdx];

			ushort formerIdx = (ushort)town.ParticipantIdx;
			ushort horizLoc = (ushort)(short)town.Place.X;
			ushort vertLoc = (ushort)(short)town.Place.Y;

			if (participantIdx == person)
				this.Owner.World.Participants[formerIdx].TouchParticipantTimeleft = -2;

			if (formerIdx != (ushort)person)
			{
				if ((town.Has(CityStructure.Palace) || (byte)town.PresentMakingIdx == 0xff) && marker == 0 && town.Has(CityStructure.Palace))
				{
					if (this.Owner.World.Participants[formerIdx].TownTally > 4 &&
						this.Owner.World.Participants[person].Standing < this.Owner.World.Participants[formerIdx].Standing)
					{
						SplitCapturedCivilization(participantIdx, town, (short)formerIdx);
					}
				}
			}

			if (town.Has(CityStructure.Palace))
			{
				DestroySpaceship((short)formerIdx);
			}

			if (formerIdx == (ushort)person)
			{
				this.Owner.World.Participants[participantIdx].TouchParticipantTimeleft = -1;
			}

			short plunder = (short)this.Owner.Helpers.ClampValue((short)ComputeCityPlunder(townIdx), 0, this.Owner.World.Participants[formerIdx].Gold);

			if (!BarbariansRazeCity(participantIdx, townIdx, horizLoc, vertLoc))
			{
				town.MaterialsTally = 0;
				town.ConditionMarker &= 0xae;
				town.StructureMarkers0 &= 0xfbb6;

				if (marker == 0)
				{
					int half = this.Owner.Libc.Dice.DrawInt(2);

					town.StructureMarkers0 &= (ushort)(0xaaaa >> (byte)half);
					town.StructureMarkers1 &= 0;
				}

				if (marker == 0 || town.TrueExtent > 1)
				{
					town.TrueExtent--;

					if (town.TrueExtent < 1)
					{
						this.Owner.Advisors.FinalizeProduction(townIdx, horizLoc, vertLoc);
					}
				}
			}

			this.Owner.World.Participants[formerIdx].Gold -= plunder;

			if (participantIdx != 0)
				this.Owner.World.Participants[participantIdx].Gold += plunder;

			if (town.TrueExtent != 0)
			{
				MarkCapturedCity(participantIdx, town, horizLoc, vertLoc);
			}

			town.ParticipantIdx = (sbyte)participantIdx;

			if (participantIdx == person || formerIdx == (ushort)person)
			{
				AnnounceCapture(participantIdx, townIdx, (short)formerIdx, horizLoc, vertLoc, plunder);
			}
			else
			{
				ReportCaptureBySpies(participantIdx, townIdx, (short)formerIdx);
			}

			ChooseTechnologyToTake(participantIdx, (short)formerIdx);

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, (short)horizLoc, (short)vertLoc);

			ushort troopIdx;

			for (troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)((ushort)(0x600 * formerIdx) + (ushort)(0xc * troopIdx));

				if (this.Troops.PullAt(troop, UnitMemory.Kind) == 0xff ||
					(sbyte)this.Troops.PullAt(troop, UnitMemory.BaseTown) != townIdx)
					continue;

				if (this.Troops.PullAt(troop, UnitMemory.Horiz) == (byte)(sbyte)town.Place.X &&
					this.Troops.PullAt(troop, UnitMemory.Vert) == (byte)(sbyte)town.Place.Y &&
					town.TrueExtent != 0)
					continue;

				this.Owner.UnitOps.DestroyUnit((short)formerIdx, (short)troopIdx);
			}

			if (participantIdx == person && this.Owner.World.Participants[person].RegimeKind < 4)
			{
				town.ConditionMarker |= 1;
			}

			if (town.TrueExtent != 0)
			{
				if (town.PresentMakingIdx >= 0)
				{
					this.Owner.World.Participants[formerIdx].TroopsInsideMaking[town.PresentMakingIdx]--;
				}

				if (participantIdx == person)
				{
					this.Owner.CityOps.HandleCity(townIdx, 1);

					this.Owner.TurnHooks.RepaintMainScreen();
				}
				else
				{
					town.PresentMakingIdx = 0;

					town.PresentMakingIdx = (sbyte)this.Owner.Advisors.PickCityProduction(participantIdx, townIdx);

					if (town.PresentMakingIdx >= 0)
					{
						this.Owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]++;
					}
				}
			}

			this.Owner.NewGameWizard.DestroyCivilization((short)formerIdx, participantIdx);
		}

		public void DestroySpaceship(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"EliminateSpaceship({participantIdx})");

			ushort built = (ushort)(1 << participantIdx);
			ushort launched = (ushort)(1 << (participantIdx + 8));

			Message(Tables.CivilizationName(participantIdx))
				.Text(" spaceship\n");

			if ((this.Owner.World.StarshipMarkers & launched) != 0)
			{
				this.Owner.Words.Append((this.Owner.World.StarshipMarkers & built) != 0 ? (ushort)0x28ca : (ushort)0x28dd);

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}

			this.Owner.World.StarshipMarkers &= (short)~built;
			this.Owner.World.StarshipMarkers &= (short)~launched;

			this.Owner.NewGameWizard.ClearSpaceshipContent(participantIdx);
		}

		public ushort ComputeCityPlunder(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ComputeCityPlunder({townIdx})");

			short size = this.Owner.World.Towns[townIdx].TrueExtent;
			Player victim = this.Owner.World.Participants[(ushort)this.Owner.World.Towns[townIdx].ParticipantIdx];
			short divisor = (short)(victim.GrandTownExtent + 1);

			short plunder = CPU.SignedQuotient(32000, size) > victim.Gold ?
				CPU.SignedQuotient((short)(victim.Gold * size), divisor) :
				(short)(CPU.SignedQuotient(victim.Gold, divisor) * size);

			return (ushort)plunder;
		}

		public void ChooseTechnologyToTake(short participantIdx1, short participantIdx2)
		{
			using var _ = this.Processor.Journal.Scope($"ChooseTechnologyToTake({participantIdx1}, {participantIdx2})");

			ushort bestTech = 0;

			if (participantIdx1 != 0 && participantIdx2 != 0)
			{
				ushort count = 0;
				ushort[] techs = new ushort[0x48];

				this.Owner.Words.Set("Select one...\n ");

				short bestScore = -1;

				for (short techId = 0x47; techId >= 0; techId--)
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx1, techId) == 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIdx2, techId) != 0)
					{
						this.Owner.Words.Append(GameTables.TechnologyText(techId));
						this.Owner.Words.Append("\n ");

						count++;
						techs[count - 1] = (ushort)techId;

						if ((short)this.Owner.Advisors.ObtainTechnologyInfo(participantIdx1, (ushort)techId) > bestScore)
						{
							bestScore = (short)this.Owner.Advisors.ObtainTechnologyInfo(participantIdx1, (ushort)techId);
							bestTech = (ushort)techId;
						}
					}
				}

				if (count != 0)
				{
					if (participantIdx1 == this.Owner.World.PersonParticipantIdx)
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

						short chosenTech = (short)techs[choice];

						this.Owner.Advisors.EarnTechnology(participantIdx1, chosenTech, participantIdx2);
					}
					else
					{
						if (participantIdx2 == this.Owner.World.PersonParticipantIdx)
						{
							this.Owner.Words.Set(Tables.CivilizationPlural(participantIdx1));
							this.Owner.Words.Append(" take\n");
							this.Owner.Words.Append(GameTables.TechnologyText(bestTech));
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

						this.Owner.Advisors.EarnTechnology(participantIdx1, (short)bestTech, participantIdx2);
						this.Owner.Bot.RefreshCityProduction(participantIdx1, 0xffff);
					}
				}
			}
		}

		public void ObtainCityLabel(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainCityLabel({townIdx})");

			if (townIdx != -1)
			{
				byte townLabelIdx = this.Owner.World.Towns[townIdx].LabelIdx;
				ushort charsDisplacement = (ushort)(TextBuffer.Address + this.Owner.Words.Length);

				for (int idx = 0; idx < 0xd; idx++)
				{
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(charsDisplacement + idx), (byte)this.Owner.World.TownLabels[townLabelIdx][idx]);
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

				if (this.Owner.GlobDb3a != 0)
					break;
			}
			while (this.Owner.Libc.KeyAvailable() == 0);

			short key = 0;
			if (this.Owner.GlobDb3a == 0 && (key = this.Owner.Libc.ReadKey()) == 0)
			{
				key = this.Owner.Libc.ReadKey();
			}

			return key;
		}

		public ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"FoundTradeRoute({participantIdx}, {troopIdx}, {townIdx})");

			UnitMemory troops = new UnitMemory(this.Processor);
			ushort homeIdx = (ushort)troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown);
			City town = this.Owner.World.Towns[townIdx];
			City home = this.Owner.World.Towns[homeIdx];

			int distance = this.Owner.Helpers.ObtainDistance(town.Place.X, home.Place.X, town.Place.Y, home.Place.Y);
			short revenue = (short)((home.RootCommerce + town.RootCommerce) * (short)(distance + 0xa));
			revenue = (short)(revenue / 0x18);

			short homeHoriz = (short)home.Place.X;

			if (this.Owner.MapOps.ObtainContinentID(homeHoriz, homeHoriz) == this.Owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y))
			{
				revenue = (short)(revenue / 2);
			}

			if (town.ParticipantIdx == participantIdx)
			{
				revenue = (short)(revenue / 2);
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x31) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x26) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			this.Owner.World.Participants[participantIdx].Gold += revenue;
			this.Owner.World.Participants[participantIdx].InquiryHeadway += revenue;

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				Message(this.Processor.PullGlobalWord((ushort)(((troopIdx & 7) << 1) + TradeGoodsNamesAddress)))
					.Text(" caravan from ");

				ObtainCityLabel((short)homeIdx);

				this.Owner.Words.Append("\narrives in ");

				ObtainCityLabel(townIdx);

				this.Owner.Words.Append("\nTrade route established\nRevenue: $");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(revenue, 10));

				this.Owner.Words.Append(".\n");

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

				this.Owner.TurnHooks.DrawStatusBar();
			}

			this.Owner.UnitOps.DestroyUnit(participantIdx, troopIdx);

			short trade = RouteTrade(townIdx, participantIdx);
			int replacedIdx = -1;

			for (int routeIdx = 0; routeIdx < 3; routeIdx++)
			{
				ushort routeTownIdx = (byte)home.CommerceTownIndices[routeIdx];

				if (routeTownIdx == (ushort)townIdx)
				{
					replacedIdx = -1;
					break;
				}

				short routeTrade = routeTownIdx == 0xff ? (short)-1 : RouteTrade((short)routeTownIdx, participantIdx);

				if (routeTrade < trade)
				{
					trade = routeTrade;
					replacedIdx = routeIdx;
				}
			}

			if (replacedIdx != -1)
			{
				home.CommerceTownIndices[replacedIdx] = (sbyte)townIdx;
			}

			return 1;
		}

		private void SplitCapturedCivilization(short participantIdx, City town, short formerIdx)
		{
			if (this.Owner.Secession.DivideCivilization(formerIdx) != 0)
				return;

			town.ParticipantIdx = (sbyte)participantIdx;

			ushort capitalIdx = this.Owner.Secession.ComputeDistanceToCapital(formerIdx);

			this.Owner.World.Towns[capitalIdx].PresentMakingIdx = -1;

			town.ParticipantIdx = (sbyte)formerIdx;
		}

		private bool BarbariansRazeCity(short participantIdx, short townIdx, ushort horizLoc, ushort vertLoc)
		{
			if (participantIdx != 0)
				return false;

			ushort troop = (ushort)(0xc * this.Owner.MapOps.ObtainUnitAtTile((short)horizLoc, (short)vertLoc));

			sbyte kind = (sbyte)this.Troops.PullAt(((ushort)(0x600 * participantIdx) + troop), UnitMemory.Kind);

			if ((short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) > 2 || Turn.NoConquestAnimation != 0)
				return false;

			this.Owner.Advisors.FinalizeProduction(townIdx, horizLoc, vertLoc);

			this.Owner.World.Towns[townIdx].TrueExtent = 0;

			return true;
		}

		private void MarkCapturedCity(short participantIdx, City town, ushort horizLoc, ushort vertLoc)
		{
			this.Owner.UnitOps.AttachReplayContent(9, (byte)participantIdx, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			this.Owner.Bot.DisableStrategicLocation(participantIdx, 0, (short)horizLoc, (short)vertLoc, 4);

			town.ConditionMarker &= 0x9b;

			ushort markIdx;

			for (markIdx = 0; markIdx < 0x14; markIdx++)
			{
				ushort markHoriz = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHoriz(markIdx) + (short)horizLoc);
				ushort markVert = (ushort)(Tables.DirectionVert(markIdx) + vertLoc);

				this.Owner.Services.AssignDot(2, (short)markHoriz + 0x50, (short)markVert, (ushort)participantIdx);
			}
		}

		private void AnnounceCapture(short participantIdx, short townIdx, short formerIdx, ushort horizLoc, ushort vertLoc, short plunder)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.MapOps.KeepKnownTileImprovements((short)horizLoc, (short)vertLoc);

			if (formerIdx == person)
			{
				this.Owner.UnitOps.PanMapToPosition(person, (short)horizLoc, (short)vertLoc);
			}

			this.Owner.MapOps.DrawMapTile((short)horizLoc, (short)vertLoc);

			Message(Tables.CivilizationPlural(participantIdx))
				.Text(" capture\n");

			ObtainCityLabel(townIdx);

			this.Owner.Words.Append(". ");

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(plunder, 10));

			this.Owner.Words.Append(" gold\npieces plundered.\n");

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.ShortTune), 0);

			if ((this.Owner.World.SessionPrefMarkers & 8) != 0)
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.CityScene.ShowCityScreen(townIdx, -2);

				this.Owner.Startup.ShowMouseCursor();

				this.Owner.Startup.AskForDisk(1, 1);

				this.Owner.CityScene.DrawCitySceneryForPlayer(participantIdx);

				this.Owner.Startup.AskForDisk(0, 1);

				this.Owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				this.Owner.Paper.ShowNewspaper(townIdx);
			}

			this.Owner.Services.PerformSound(1, 0);

			town.ShownExtent = participantIdx == person ? (sbyte)0 : town.TrueExtent;
		}

		private void ReportCaptureBySpies(short participantIdx, short townIdx, short formerIdx)
		{
			ushort[] known = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations;

			if ((known[participantIdx] & 0x40) == 0 && (known[formerIdx] & 0x40) == 0)
				return;

			Screen.DialogStyle = 0x0;

			Message("Spies report:\n")
				.Text(Tables.CivilizationPlural((ushort)participantIdx))
				.Text(this.Owner.World.Towns[townIdx].TrueExtent != 0 ? (ushort)0x2893 : (ushort)0x28a1)
				.Text(Tables.CivilizationName((ushort)formerIdx))
				.Text(" city\nof ");

			ObtainCityLabel(townIdx);

			this.Owner.Words.Append(".\n");

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
		}

		private short RouteTrade(short townIdx, short participantIdx)
		{
			short trade = this.Owner.World.Towns[townIdx].RootCommerce;

			return this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx ? trade : (short)(trade << 1);
		}
	}
}
