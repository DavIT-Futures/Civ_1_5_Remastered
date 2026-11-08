using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class WorldEvents
	{
		public void MendFortifiedUnits()
		{
			UnitMemory troops = new UnitMemory(this.Processor);

			for (int participantIndex = 0; participantIndex < UnitHealth.ParticipantTally; participantIndex++)
			{
				List<Point> ownTowns = new List<Point>();

				foreach (City town in this.Owner.World.Towns)
				{
					if (town.ConditionMarker != 0xff && town.ParticipantIndex == participantIndex)
					{
						ownTowns.Add(town.Place);
					}
				}

				for (int troopIndex = 0; troopIndex < UnitHealth.TroopTally; troopIndex++)
				{
					int troopKind = troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

					if (troopKind == 0xff || this.Owner.World.Vitals.ObtainLostHitPoints(participantIndex, troopIndex, troopKind) == 0)
					{
						continue;
					}

					int horizontalLocation = troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
					int verticalLocation = troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);
					int status = troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
					int landGroup = troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup);
					bool inOwnTown = ownTowns.Contains(new Point(horizontalLocation, verticalLocation));
					bool resting;
					bool nextToLand = false;
					int townDistance = int.MaxValue;

					if (landGroup == UnitKindTable.SeaUnits)
					{
						int fullSteps = 3 * troops.KindField(participantIndex, troopIndex, UnitKindTable.ShiftTally);

						resting = (status & (int)UnitStatus.Sentry) != 0 ||
							troops.Pull(participantIndex, troopIndex, UnitMemory.LeftoverSteps) >= fullSteps;

						if (!inOwnTown)
						{
							townDistance = HealingRules.ComputeDistanceToNearestFriendlyCity(horizontalLocation, verticalLocation, ownTowns);

							if (townDistance <= HealingRules.MaximumShipCoastDistanceToCity)
							{
								nextToLand = HealingRules.IsAdjacentToLand(horizontalLocation, verticalLocation,
									(tileHorizontal, tileVertical) => this.Owner.MapOperations.Chart.Terrain(tileHorizontal, tileVertical) == MapRules.OceanTerrain);
							}
						}
					}
					else
					{
						resting = (status & (int)UnitStatus.Fortified) != 0;
					}

					int amount = HealingRules.CalculateHealAmount(landGroup, inOwnTown, resting, nextToLand, townDistance);

					this.Owner.World.Vitals.MendAmount(participantIndex, troopIndex, troopKind, amount);
				}
			}
		}

		public void HandleEndOfTurnEvents()
		{
			using var _ = this.Processor.Journal.Scope("HandleEndOfTurnEvents()");

			short pull = (short)(((ushort)this.Owner.World.DirtyCellTally << 1) - ((ushort)this.Owner.World.WorldHeatingTally << 2) - (ushort)this.Owner.World.SmogImpactTier);

			this.Owner.World.SmogImpactTier = (short)this.Owner.Helpers.ClampValue((short)(Math.Sign(pull) + this.Owner.World.SmogImpactTier), 0, 99);

			if (this.Owner.World.SmogImpactTier == 12 && this.Owner.World.DirtyCellTally > 6)
			{
				this.Owner.Words.Set("Scientists alarmed by\nrising temperatures.\nGlobal Warming feared!\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			if (this.Owner.World.SmogImpactTier > 16)
			{
				this.Owner.WorldSetup.EnactGlobalWarming(this.Owner.World.WorldHeatingTally);

				this.Owner.World.WorldHeatingTally++;
				this.Owner.World.SmogImpactTier = 0;
			}

			for (short attempt = 0; attempt <= this.Owner.World.HardnessTier; attempt++)
			{
				if (TryRivalWonder(attempt))
				{
					break;
				}
			}
		}

		private bool PlayTurnOfPlayers(out short startGold)
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Helpers.LaunchHueCycling();

			short personIndex = this.Owner.World.PersonParticipantIndex;

			startGold = this.Owner.World.Participants[personIndex].Gold;

			Turn.UnitLimitTold = 0;
			Turn.RiotShown = 0;
			this.Owner.World.HighestScienceTally = 0;
			Turn.LiveRealmCount = 0;

			for (int participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				if ((this.Owner.World.LiveRealms & (1 << participantIndex)) == 0)
				{
					continue;
				}

				Turn.LiveRealmCount = (ushort)(Turn.LiveRealmCount + 1);

				short sciences = this.Owner.World.Participants[participantIndex].FoundScienceTally;

				if (this.Owner.World.HighestScienceTally < sciences)
				{
					this.Owner.World.HighestScienceTally = sciences;
				}
			}

			if (this.Owner.World.RoundTally == 0)
			{
				ushort troop = (ushort)(0x600 * personIndex);

				MapView.Horizontal = (ushort)(this.Troops.PullAt(troop, UnitMemory.Horizontal) - 7);
				MapView.Vertical = (ushort)(this.Troops.PullAt(troop, UnitMemory.Vertical) - 6);

				RepaintMainScreen();

				if (this.Owner.World.HardnessTier == 0)
				{
					this.Owner.TurnLogic.DrawUnitInformation(this.Owner.World.PersonParticipantIndex, 0);

					this.Owner.Prologue.ShowInterfaceTutorial();

					RepaintMainScreen();
				}

				this.Owner.Services.PerformSound(1, 0);
			}

			for (short participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				this.Owner.World.Participants[participantIndex].Tally = 0;

				if ((this.Owner.World.LiveRealms & (1 << participantIndex)) == 0)
				{
					continue;
				}

				if (participantIndex < this.Owner.World.PersonParticipantIndex && Files.MidTurnSave != 0)
				{
					continue;
				}

				PlayTurnOfPlayer(participantIndex);

				if (Turn.GameEndRequest != 0 && Turn.ConquestEnd == 0)
				{
					if (Turn.GameEndRequest == 2)
					{
						ShowEndGameSequence();

						ShowTitleScreen();
					}

					return false;
				}
			}

			return true;
		}

		private void PlayTurnOfPlayer(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];
			bool person = participantIndex == this.Owner.World.PersonParticipantIndex;

			this.Owner.Services.FloodBox(Screen.Box, 2, 192, 6, 6, Hues.DarkGray);

			if (person)
			{
				this.Owner.Startup.ShowMouseCursor();
			}

			if (Files.MidTurnSave == 0)
			{
				this.Owner.Advisors.HandleAllCities(participantIndex);
			}

			if (player.Gold > 0x7530)
			{
				player.Gold = 0x7530;
			}

			this.Owner.Politics.HandlePeaceTreaties(participantIndex);

			this.Owner.Computer.OperateComputerTurn(participantIndex);

			this.Owner.Computer.ReimposeStrategicLocations(participantIndex);

			this.Owner.Services.FloodBox(Screen.Box, 2, 192, 6, 6, Hues.White);

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				DrawStatusBar();

				if (player.TownTally == 0 && player.ColonistTally == 0)
				{
					this.Owner.Startup.HideMouseCursor();

					this.Owner.Prologue.ShowTerrainTutorial();

					Turn.GameEndRequest = 0xffff;

					this.Owner.Startup.ShowMouseCursor();
				}

				if (this.Owner.World.Season > 0)
				{
					this.Owner.World.TruceRoundTally++;
				}
			}

			if (Turn.GameEndRequest == 0)
			{
				if (person)
				{
					WakeThreatenedSentries(participantIndex);
				}

				this.Owner.TurnLogic.HandlePlayerTurn(participantIndex);
			}

			Files.MidTurnSave = 0x0;

			if (person)
			{
				this.Owner.Startup.HideMouseCursor();
			}
		}

		private void FinishTurn(short startGold)
		{
			this.Owner.Startup.ShowMouseCursor();

			ShowTravellersReport();

			HandleEndOfTurnEvents();

			if (this.Owner.World.RoundTally > 50)
			{
				StrikeRandomCities();
			}

			RefreshPeaceGraphContent();

			WarnOfBankruptcy(startGold);

			AdvanceYear();

			MendFortifiedUnits();

			if (this.Owner.World.PersonParticipantIndex != -1)
			{
				this.Owner.SaveLoad.PerformBackup();
			}

			if (this.Owner.World.RoundTally % 0x32 == 0 && this.Owner.World.PersonParticipantIndex != -1)
			{
				NoteStatusInReplay();
			}

			if (this.Owner.World.RoundTally >= this.Owner.World.FollowingCollectionRound)
			{
				this.Owner.World.FollowingCollectionRound = (short)(this.Owner.Runtime.Dice.DrawInteger(40) + this.Owner.World.RoundTally + 0x14);

				this.Owner.Atlas.ShowEpicChronicle();
			}

			ScoreWondersAndPollution();

			GrowPalace();

			DrawStatusBar();

			if (!ChooseGameEnd())
			{
				return;
			}

			EndGame();
		}

		private void StrikeRandomCities()
		{
			for (int draw = 0; draw < TurnEndRules.DisasterDraws; draw++)
			{
				short townIndex = (short)this.Owner.Runtime.Dice.DrawInteger(128);

				StrikeCity(townIndex);
			}
		}

		private void StrikeCity(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			if (town.ConditionMarker != 0xff && this.Owner.World.Participants[town.ParticipantIndex].TownTally > 1 && town.TrueExtent >= 5)
			{
				this.Owner.Founding.HandleCityDisasters(townIndex);
			}
		}

		private void WarnOfBankruptcy(short startGold)
		{
			if (Turn.GameEndRequest != 0)
			{
				return;
			}

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];

			if (person.TownTally <= 1 || person.Gold >= 100)
			{
				return;
			}

			if ((short)((10 * (person.Gold - startGold)) + person.Gold) >= 0)
			{
				return;
			}

			if (!this.Owner.World.IsHuman(this.Owner.World.PersonParticipantIndex))
			{
				return;
			}

			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(BankruptcyWarningDialogAddress);

			Screen.DialogStyle = 0x4;

			ShowDialog(TextBuffer.Address, 80, 80);
		}

		private void AdvanceYear()
		{
			this.Owner.World.RoundTally++;

			short season = this.Owner.World.Season;

			this.Owner.World.Season += season < 1000 ? (short)20 : season < 1500 ? (short)10 : season < 1750 ? (short)5 : season < 1850 && (this.Owner.World.StarshipMarkers & 0xfe) == 0 ? (short)2 : (short)1;

			if (this.Owner.World.Season == 0)
			{
				for (int participantIndex = 0; participantIndex < 8; participantIndex++)
				{
					this.Owner.World.Participants[participantIndex].InquiryHeadway <<= 1;
				}

				this.Owner.World.Season = 1;
			}

			if (this.Owner.World.Season == 21)
			{
				this.Owner.World.Season = 20;
			}
		}

		private void ScoreWondersAndPollution()
		{
			for (int marvelIndex = 1; marvelIndex <= 0x15; marvelIndex++)
			{
				ushort townIndex = (ushort)this.Owner.World.MarvelTownIndex[marvelIndex];

				if (townIndex == 0xffff || townIndex == 0x80)
				{
					continue;
				}

				this.Owner.World.Participants[this.Owner.World.Towns[townIndex].ParticipantIndex].Tally += 25;
			}

			if (Files.LoadedGameMark == 0xfffd)
			{
				Files.LoadedGameMark = 0xfffe;
				return;
			}

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Tally -= (short)(10 * this.Owner.World.DirtyCellTally);

			if (Files.LoadedGameMark == 0xfffe)
			{
				Files.LoadedGameMark = 0x0;
			}
		}

		private void LandBarbariansFromSea()
		{
			short horizontalLocation, verticalLocation;

			do
			{
				horizontalLocation = (short)this.Owner.Runtime.Dice.DrawInteger(80);

				verticalLocation = (short)(this.Owner.Runtime.Dice.DrawInteger(44) + 3);
			}
			while (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) != 0xa ||
				this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation) != 0xffff ||
				(short)this.Owner.MapOperations.ObtainLandMassSize(horizontalLocation, verticalLocation) < 0x10);

			short units = (short)this.Owner.Helpers.ClampValue((this.Owner.World.RoundTally / 150) + 1, 1, 3);

			this.Owner.UnitOperations.CreateUnit(0, units < 3 ? (short)0x11 : (short)0x12, horizontalLocation, verticalLocation);

			short unitIndex;

			for (unitIndex = 0; unitIndex <= units; unitIndex++)
			{
				short kind = (this.Owner.World.ScienceLeadingFoundVia[56] & 7) != 0 ? (short)7 : (short)3;

				if (unitIndex == units)
				{
					kind = 0x1a;
				}

				ushort troopIndex = this.Owner.UnitOperations.CreateUnit(0, kind, horizontalLocation, verticalLocation);

				int marker = (ushort)(UnitMemory.TroopStride * troopIndex);
				this.Troops.AssignAt(marker, UnitMemory.Status, (byte)(this.Troops.PullAt(marker, UnitMemory.Status) | 1));
			}
		}

		private void RaidHumanCity()
		{
			ushort townIndex = (ushort)this.Owner.Runtime.Dice.DrawInteger(128);

			if (this.Owner.World.Towns[townIndex].ConditionMarker == 0xff ||
				this.Owner.World.Towns[townIndex].ParticipantIndex != this.Owner.World.PersonParticipantIndex)
			{
				return;
			}

			ushort plunder = this.Owner.Conquest.ComputeCityPlunder((short)townIndex);

			if ((short)this.Owner.Runtime.Dice.DrawInteger(100) >= (short)plunder)
			{
				return;
			}

			ushort tries = 0;
			short horizontalLocation, verticalLocation;

			while (true)
			{
				int shift = this.Owner.Runtime.Dice.DrawInteger(13);
				horizontalLocation = (short)((short)this.Owner.World.Towns[townIndex].Place.X + shift - 6);

				shift = this.Owner.Runtime.Dice.DrawInteger(13);
				verticalLocation = (short)((short)this.Owner.World.Towns[townIndex].Place.Y + shift - 6);

				townIndex = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);

				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(horizontalLocation, this.Owner.World.Towns[townIndex].Place.X, verticalLocation, this.Owner.World.Towns[townIndex].Place.Y);

				tries++;

				if ((short)tries >= 0x63)
				{
					return;
				}

				if (distance < 6)
				{
					continue;
				}

				if (this.Owner.UnitOperations.CheckTileOccupation(0, horizontalLocation, verticalLocation) != 0)
				{
					continue;
				}

				if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == 0xa)
				{
					continue;
				}

				if (this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation) != 0xffff)
				{
					continue;
				}

				ushort continent = this.Owner.MapOperations.ObtainContinentIdentifier(this.Owner.World.Towns[townIndex].Place.X, this.Owner.World.Towns[townIndex].Place.Y);

				if (this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation) == continent)
				{
					break;
				}
			}

			if (this.Owner.World.Towns[townIndex].ParticipantIndex == this.Owner.World.PersonParticipantIndex &&
				((sbyte)this.Owner.World.ChartSight[(ushort)horizontalLocation, (ushort)verticalLocation] & (1 << (byte)this.Owner.World.PersonParticipantIndex)) != 0)
			{
				this.Owner.MapOperations.FocusMapView(this.Owner.World.PersonParticipantIndex,
					this.Owner.World.Towns[townIndex].Place.X - 8,
					this.Owner.World.Towns[townIndex].Place.Y - 6);

				this.Owner.Words.Set("Travellers report:\n");

				if ((this.Owner.World.ScienceLeadingFoundVia[35] & 7) != 0)
				{
					this.Owner.Words.Append(TravellersReportGunpowderAddress);
				}
				else if ((this.Processor.PullByte(this.Processor.SegmentExtra.Word, 0x4f4) & 7) != 0)
				{
					this.Owner.Words.Append(TravellersReportIronWorkingAddress);
				}
				else
				{
					this.Owner.Words.Append(TravellersReportAncientAddress);
				}

				this.Owner.Conquest.ObtainCityLabel((short)townIndex);

				this.Owner.Words.Append("!\n");

				Screen.DialogStyle = 0x2;

				ShowDialog(TextBuffer.Address, 100, 32);
			}

			short units = (short)this.Owner.Helpers.ClampValue(this.Owner.Runtime.Dice.DrawInteger(this.Owner.World.Towns[townIndex].TrueExtent / 2), 2, 99);

			short unitIndex;

			for (unitIndex = 0; unitIndex <= units; unitIndex++)
			{
				short kind = (this.Owner.World.ScienceLeadingFoundVia[35] & 7) != 0 ? ((unitIndex & 4) != 0 ? (short)9 : (short)4) : ((unitIndex & 4) != 0 ? (short)0xa : (short)6);

				if (unitIndex == units)
				{
					kind = 0x1a;
				}

				ushort sight = (ushort)((ushort)(0xc * this.Owner.UnitOperations.CreateUnit(0, kind, horizontalLocation, verticalLocation)) + 0x81dd);
				this.Processor.EmitGlobalByte(sight, (byte)(this.Processor.PullGlobalByte(sight) | (byte)this.Owner.World.ChartSight[(ushort)horizontalLocation, (ushort)verticalLocation]));

				if (this.Owner.World.Towns[townIndex].ParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
				}
			}
		}

		private void AnnouncePopulationMark(short population)
		{
			short previousMark = (short)Turn.PopulationMark;

			Message("The population of the\nfertile ")
				.Text(Tables.CivilizationName(this.Owner.World.PersonParticipantIndex))
				.Text(" empire now\nexceeds ");

			if (population < 100)
			{
				if ((short)(Turn.PopulationMark * 10) < population)
				{
					short mark = (short)(population / 10);

					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(mark, 10));

					Turn.PopulationMark = (ushort)mark;
				}
			}
			else if ((short)((Turn.PopulationMark - 9) * 100) < population)
			{
				short hundreds = (short)(population / 100);

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(hundreds, 10));
				this.Owner.Words.Append(",0");

				Turn.PopulationMark = (ushort)(hundreds + 9);
			}

			if (previousMark < (short)Turn.PopulationMark && (short)Files.LoadedGameMark >= -1)
			{
				this.Owner.Words.Append("00,000 citizens.\n");

				Screen.DialogStyle = 4;

				ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		private bool TryRivalWonder(short attempt)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			ushort wonderIndex = (ushort)(this.Owner.Runtime.Dice.DrawInteger(21) + 1);

			if (this.Owner.World.HardnessTier == 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(person, (short)Tables.BuildingWord(wonderIndex, BuildingTable.Technology)) == 0)
			{
				return false;
			}

			if (this.Owner.World.HardnessTier < 2)
			{
				for (ushort townIndex = 0; townIndex < 0x80; townIndex++)
				{
					if (HumanCityBuilds(townIndex, wonderIndex))
					{
						wonderIndex = 0x11;
					}
				}
			}

			ushort cityIndex = (ushort)this.Owner.Runtime.Dice.DrawInteger(128);

			if (this.Owner.World.Towns[cityIndex].ConditionMarker == 0xff)
			{
				return false;
			}

			ushort rivalIndex = (ushort)this.Owner.World.Towns[cityIndex].ParticipantIndex;

			if ((this.Owner.World.Participants[rivalIndex].Relations[0] & 4) != 0)
			{
				return false;
			}

			if (wonderIndex == 0x11 && this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIndex, 0x2e) == 0)
			{
				return false;
			}

			if (attempt == 0 && this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIndex, 0x2e) != 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(person, 0x2e) == 0 && this.Owner.World.MarvelTownIndex[17] == -1)
			{
				wonderIndex = 0x11;
			}

			if (wonderIndex == 0x11 && this.Owner.Advisors.PlayerKnowsTechnology(person, 0x2e) != 0 &&
				this.Owner.World.Participants[rivalIndex].MartialMight > this.Owner.World.Participants[person].MartialMight)
			{
				return false;
			}

			if (rivalIndex == 0xffff || rivalIndex == (ushort)person)
			{
				return false;
			}

			if (this.Owner.World.MarvelTownIndex[wonderIndex] != -1)
			{
				return false;
			}

			if (this.Owner.World.Towns[cityIndex].TrueExtent <= CentralProcessor.SignedQuotient((short)Tables.BuildingWord(24 + wonderIndex, BuildingTable.Price), 10))
			{
				return false;
			}

			if (this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIndex, (short)Tables.BuildingWord(24 + wonderIndex, BuildingTable.Technology)) == 0)
			{
				return false;
			}

			this.Owner.World.MarvelTownIndex[wonderIndex] = (short)cityIndex;
			this.Owner.World.Towns[cityIndex].MaterialsTally = 0;

			this.Owner.UnitOperations.AttachReplayContent(10, (byte)rivalIndex, (byte)wonderIndex);

			EmptyMessage()
				.City((short)cityIndex)
				.Text(" (")
				.Text(Tables.CivilizationName(rivalIndex))
				.Text(")\nbuilds ")
				.Text(GameTables.BuildingRecordText(24 + wonderIndex))
				.Text(".\n");

			this.Owner.World.Participants[rivalIndex].Gold -= (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[rivalIndex].Gold / 3, 0,
				(10 * (short)Tables.BuildingWord(24 + wonderIndex, BuildingTable.Price)) - this.Owner.World.Towns[cityIndex].MaterialsTally);

			if (MapView.WorldMapView != 1)
			{
				this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[rivalIndex].ChiefVisuals, LeaderTable.ShortTune), 0);

				Screen.DialogStyle = (this.Owner.World.Participants[person].Relations[rivalIndex] & 0x40) == 0 ? (ushort)2 : (ushort)5;

				ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)(wonderIndex + 0x18), 1);

				RepaintMainScreen();

				this.Owner.Services.PerformSound(1, 0);
			}

			if (wonderIndex == 0xe)
			{
				this.Owner.Advisors.UncoverTechnology((short)rivalIndex, 0);

				this.Owner.Advisors.UncoverTechnology((short)rivalIndex, 0);
			}

			for (ushort townIndex = 0; townIndex < 0x80; townIndex++)
			{
				if (HumanCityBuilds(townIndex, wonderIndex))
				{
					this.Owner.Advisors.RenewCityScreen((short)townIndex);
				}
			}

			return true;
		}

		private bool HumanCityBuilds(ushort townIndex, ushort wonderIndex)
		{
			return this.Owner.World.Towns[townIndex].ConditionMarker != 0xff &&
				this.Owner.World.Towns[townIndex].ParticipantIndex == this.Owner.World.PersonParticipantIndex &&
				this.Owner.World.Towns[townIndex].PresentMakingIndex == (short)-(wonderIndex + 0x18);
		}
	}
}
