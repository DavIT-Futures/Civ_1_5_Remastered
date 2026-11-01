using System;
using System.Collections.Generic;

namespace Civilization1Remaster.Core.Game
{
	public partial class WorldEvents
	{
		public void MendFortifiedUnits()
		{
			UnitMemory troops = new UnitMemory(this.Processor);

			for (int participantIdx = 0; participantIdx < UnitHealth.ParticipantTally; participantIdx++)
			{
				List<Point> ownTowns = new List<Point>();

				foreach (City town in this.Owner.World.Towns)
				{
					if (town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx)
						ownTowns.Add(town.Place);
				}

				for (int troopIdx = 0; troopIdx < UnitHealth.TroopTally; troopIdx++)
				{
					int troopKind = troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if (troopKind == 0xff || this.Owner.World.Vitals.ObtainLostHitPoints(participantIdx, troopIdx, troopKind) == 0)
						continue;

					int horizLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
					int vertLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
					int status = troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
					int landGroup = troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);
					bool inOwnTown = ownTowns.Contains(new Point(horizLoc, vertLoc));
					bool resting;
					bool nextToLand = false;
					int townDistance = int.MaxValue;

					if (landGroup == UnitKindTable.SeaUnits)
					{
						int fullSteps = 3 * troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally);

						resting = (status & (int)UnitStatus.Sentry) != 0 ||
							troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= fullSteps;

						if (!inOwnTown)
						{
							townDistance = HealingRules.ComputeDistanceToNearestFriendlyCity(horizLoc, vertLoc, ownTowns);

							if (townDistance <= HealingRules.MaxShipCoastDistanceToCity)
							{
								nextToLand = HealingRules.IsAdjacentToLand(horizLoc, vertLoc,
									(tileHoriz, tileVert) => this.Owner.MapOps.Chart.Terrain(tileHoriz, tileVert) == MapRules.OceanTerrain);
							}
						}
					}
					else
					{
						resting = (status & (int)UnitStatus.Fortified) != 0;
					}

					int amount = HealingRules.CalculateHealAmount(landGroup, inOwnTown, resting, nextToLand, townDistance);

					this.Owner.World.Vitals.MendAmount(participantIdx, troopIdx, troopKind, amount);
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
					break;
			}
		}

		private bool PlayTurnOfPlayers(out short startGold)
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Helpers.LaunchHueCycling();

			short personIdx = this.Owner.World.PersonParticipantIdx;

			startGold = this.Owner.World.Participants[personIdx].Gold;

			Turn.UnitLimitTold = 0;
			Turn.RiotShown = 0;
			this.Owner.World.HighestScienceTally = 0;
			Turn.LiveRealmCount = 0;

			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				Turn.LiveRealmCount = (ushort)(Turn.LiveRealmCount + 1);

				short sciences = this.Owner.World.Participants[participantIdx].FoundScienceTally;

				if (this.Owner.World.HighestScienceTally < sciences)
				{
					this.Owner.World.HighestScienceTally = sciences;
				}
			}

			if (this.Owner.World.RoundTally == 0)
			{
				ushort troop = (ushort)(0x600 * personIdx);

				MapView.Horiz = (ushort)(this.Troops.PullAt(troop, UnitMemory.Horiz) - 7);
				MapView.Vert = (ushort)(this.Troops.PullAt(troop, UnitMemory.Vert) - 6);

				RepaintMainScreen();

				if (this.Owner.World.HardnessTier == 0)
				{
					this.Owner.TurnLogic.DrawUnitInfo(this.Owner.World.PersonParticipantIdx, 0);

					this.Owner.Prologue.ShowInterfaceTutorial();

					RepaintMainScreen();
				}

				this.Owner.Services.PerformSound(1, 0);
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.Owner.World.Participants[participantIdx].Tally = 0;

				if ((this.Owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				if (participantIdx < this.Owner.World.PersonParticipantIdx && Files.MidTurnSave != 0)
					continue;

				PlayTurnOfPlayer(participantIdx);

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

		private void PlayTurnOfPlayer(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			bool person = participantIdx == this.Owner.World.PersonParticipantIdx;

			this.Owner.Services.FloodBox(Screen.Box, 2, 192, 6, 6, Hues.DarkGray);

			if (person)
			{
				this.Owner.Startup.ShowMouseCursor();
			}

			if (Files.MidTurnSave == 0)
			{
				this.Owner.Advisors.HandleAllCities(participantIdx);
			}

			if (player.Gold > 0x7530)
			{
				player.Gold = 0x7530;
			}

			this.Owner.Politics.HandlePeaceTreaties(participantIdx);

			this.Owner.Bot.OperateAITurn(participantIdx);

			this.Owner.Bot.ReimposeStrategicLocations(participantIdx);

			this.Owner.Services.FloodBox(Screen.Box, 2, 192, 6, 6, Hues.White);

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
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
				this.Owner.TurnLogic.HandlePlayerTurn(participantIdx);
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

			if (this.Owner.World.PersonParticipantIdx != -1)
			{
				this.Owner.SaveLoad.PerformBackup();
			}

			if (this.Owner.World.RoundTally % 0x32 == 0 && this.Owner.World.PersonParticipantIdx != -1)
			{
				NoteStatusInReplay();
			}

			if (this.Owner.World.RoundTally >= this.Owner.World.FollowingCollectionRound)
			{
				this.Owner.World.FollowingCollectionRound = (short)(this.Owner.Libc.Dice.DrawInt(40) + this.Owner.World.RoundTally + 0x14);

				this.Owner.Atlas.ShowEpicChronicle();
			}

			ScoreWondersAndPollution();

			GrowPalace();

			DrawStatusBar();

			if (!ChooseGameEnd())
				return;

			EndGame();
		}

		private void StrikeRandomCities()
		{
			for (int draw = 0; draw < TurnEndRules.DisasterDraws; draw++)
			{
				short townIdx = (short)this.Owner.Libc.Dice.DrawInt(128);

				StrikeCity(townIdx);
			}
		}

		private void StrikeCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (town.ConditionMarker != 0xff && this.Owner.World.Participants[town.ParticipantIdx].TownTally > 1 && town.TrueExtent >= 5)
			{
				this.Owner.Founding.HandleCityDisasters(townIdx);
			}
		}

		private void WarnOfBankruptcy(short startGold)
		{
			if (Turn.GameEndRequest != 0)
				return;

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

			if (person.TownTally <= 1 || person.Gold >= 100)
				return;

			if ((short)((10 * (person.Gold - startGold)) + person.Gold) >= 0)
				return;

			if (!this.Owner.World.IsHuman(this.Owner.World.PersonParticipantIdx))
				return;

			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(BankruptcyWarningDialogAddress);

			Screen.DialogStyle = 0x4;

			ShowDialog(TextBuffer.Address, 80, 80);
		}

		private void AdvanceYear()
		{
			this.Owner.World.RoundTally++;

			short season = this.Owner.World.Season;

			this.Owner.World.Season += season < 1000 ? (short)20 : season < 1500 ? (short)10 : season < 1750 ? (short)5 :
				season < 1850 && (this.Owner.World.StarshipMarkers & 0xfe) == 0 ? (short)2 : (short)1;

			if (this.Owner.World.Season == 0)
			{
				for (int participantIdx = 0; participantIdx < 8; participantIdx++)
				{
					this.Owner.World.Participants[participantIdx].InquiryHeadway <<= 1;
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
			for (int marvelIdx = 1; marvelIdx <= 0x15; marvelIdx++)
			{
				ushort townIdx = (ushort)this.Owner.World.MarvelTownIdx[marvelIdx];

				if (townIdx == 0xffff || townIdx == 0x80)
					continue;

				this.Owner.World.Participants[this.Owner.World.Towns[townIdx].ParticipantIdx].Tally += 25;
			}

			if (Files.LoadedGameMark == 0xfffd)
			{
				Files.LoadedGameMark = 0xfffe;
				return;
			}

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Tally -= (short)(10 * this.Owner.World.DirtyCellTally);

			if (Files.LoadedGameMark == 0xfffe)
			{
				Files.LoadedGameMark = 0x0;
			}
		}

		private void LandBarbariansFromSea()
		{
			short horizLoc, vertLoc;

			do
			{
				horizLoc = (short)this.Owner.Libc.Dice.DrawInt(80);

				vertLoc = (short)(this.Owner.Libc.Dice.DrawInt(44) + 3);
			}
			while (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != 0xa ||
				this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff ||
				(short)this.Owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 0x10);

			short units = (short)this.Owner.Helpers.ClampValue((this.Owner.World.RoundTally / 150) + 1, 1, 3);

			this.Owner.UnitOps.CreateUnit(0, units < 3 ? (short)0x11 : (short)0x12, horizLoc, vertLoc);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.Owner.World.ScienceLeadingFoundVia[56] & 7) != 0 ? (short)7 : (short)3;

				if (unitIdx == units)
					kind = 0x1a;

				ushort troopIdx = this.Owner.UnitOps.CreateUnit(0, kind, horizLoc, vertLoc);

				int marker = (ushort)(UnitMemory.TroopStride * troopIdx);
				this.Troops.AssignAt(marker, UnitMemory.Status, (byte)(this.Troops.PullAt(marker, UnitMemory.Status) | 1));
			}
		}

		private void RaidHumanCity()
		{
			ushort townIdx = (ushort)this.Owner.Libc.Dice.DrawInt(128);

			if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff ||
				this.Owner.World.Towns[townIdx].ParticipantIdx != this.Owner.World.PersonParticipantIdx)
				return;

			ushort plunder = this.Owner.Conquest.ComputeCityPlunder((short)townIdx);

			if ((short)this.Owner.Libc.Dice.DrawInt(100) >= (short)plunder)
				return;

			ushort tries = 0;
			short horizLoc, vertLoc;

			while (true)
			{
				int shift = this.Owner.Libc.Dice.DrawInt(13);
				horizLoc = (short)((short)this.Owner.World.Towns[townIdx].Place.X + shift - 6);

				shift = this.Owner.Libc.Dice.DrawInt(13);
				vertLoc = (short)((short)this.Owner.World.Towns[townIdx].Place.Y + shift - 6);

				townIdx = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);
	
				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(horizLoc, this.Owner.World.Towns[townIdx].Place.X, vertLoc, this.Owner.World.Towns[townIdx].Place.Y);

				tries++;

				if ((short)tries >= 0x63)
					return;

				if (distance < 6)
					continue;

				if (this.Owner.UnitOps.CheckTileOccupation(0, horizLoc, vertLoc) != 0)
					continue;

				if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa)
					continue;

				if (this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff)
					continue;

				ushort continent = this.Owner.MapOps.ObtainContinentID(this.Owner.World.Towns[townIdx].Place.X, this.Owner.World.Towns[townIdx].Place.Y);

				if (this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc) == continent)
					break;
			}

			if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
				((sbyte)this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (byte)this.Owner.World.PersonParticipantIdx)) != 0)
			{
				this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx,
					this.Owner.World.Towns[townIdx].Place.X - 8,
					this.Owner.World.Towns[townIdx].Place.Y - 6);

				this.Owner.Words.Set("Travellers report:\n");

				if ((this.Owner.World.ScienceLeadingFoundVia[35] & 7) != 0)
				{
					this.Owner.Words.Append(TravellersReportGunpowderAddress);
				}
				else if ((this.Processor.PullUInt8(this.Processor.SegExtra.Value16, 0x4f4) & 7) != 0)
				{
					this.Owner.Words.Append(TravellersReportIronWorkingAddress);
				}
				else
				{
					this.Owner.Words.Append(TravellersReportAncientAddress);
				}

				this.Owner.Conquest.ObtainCityLabel((short)townIdx);

				this.Owner.Words.Append("!\n");

				Screen.DialogStyle = 0x2;

				ShowDialog(TextBuffer.Address, 100, 32);
			}

			short units = (short)this.Owner.Helpers.ClampValue(this.Owner.Libc.Dice.DrawInt(this.Owner.World.Towns[townIdx].TrueExtent / 2), 2, 99);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.Owner.World.ScienceLeadingFoundVia[35] & 7) != 0 ? ((unitIdx & 4) != 0 ? (short)9 : (short)4) :
					((unitIdx & 4) != 0 ? (short)0xa : (short)6);

				if (unitIdx == units)
					kind = 0x1a;

				ushort sight = (ushort)((ushort)(0xc * this.Owner.UnitOps.CreateUnit(0, kind, horizLoc, vertLoc)) + 0x81dd);
				this.Processor.EmitGlobalByte(sight, (byte)(this.Processor.PullGlobalByte(sight) | (byte)this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc]));

				if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
				{
					this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
				}
			}
		}

		private void AnnouncePopulationMark(short population)
		{
			short previousMark = (short)Turn.PopulationMark;

			Message("The population of the\nfertile ")
				.Text(Tables.CivilizationName(this.Owner.World.PersonParticipantIdx))
				.Text(" empire now\nexceeds ");

			if (population < 100)
			{
				if ((short)(Turn.PopulationMark * 10) < population)
				{
					short mark = (short)(population / 10);

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(mark, 10));

					Turn.PopulationMark = (ushort)mark;
				}
			}
			else if ((short)((Turn.PopulationMark - 9) * 100) < population)
			{
				short hundreds = (short)(population / 100);

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(hundreds, 10));
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
			short person = this.Owner.World.PersonParticipantIdx;
			ushort wonderIdx = (ushort)(this.Owner.Libc.Dice.DrawInt(21) + 1);

			if (this.Owner.World.HardnessTier == 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(person, (short)Tables.BuildingWord(wonderIdx, BuildingTable.Technology)) == 0)
				return false;

			if (this.Owner.World.HardnessTier < 2)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (HumanCityBuilds(townIdx, wonderIdx))
						wonderIdx = 0x11;
				}
					}

			ushort cityIdx = (ushort)this.Owner.Libc.Dice.DrawInt(128);

			if (this.Owner.World.Towns[cityIdx].ConditionMarker == 0xff)
				return false;

			ushort rivalIdx = (ushort)this.Owner.World.Towns[cityIdx].ParticipantIdx;

			if ((this.Owner.World.Participants[rivalIdx].Relations[0] & 4) != 0)
				return false;

			if (wonderIdx == 0x11 && this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) == 0)
				return false;

			if (attempt == 0 && this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) != 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(person, 0x2e) == 0 && this.Owner.World.MarvelTownIdx[17] == -1)
			{
				wonderIdx = 0x11;
				}

			if (wonderIdx == 0x11 && this.Owner.Advisors.PlayerKnowsTechnology(person, 0x2e) != 0 &&
				this.Owner.World.Participants[rivalIdx].MartialMight > this.Owner.World.Participants[person].MartialMight)
				return false;

			if (rivalIdx == 0xffff || rivalIdx == (ushort)person)
				return false;

			if (this.Owner.World.MarvelTownIdx[wonderIdx] != -1)
				return false;

			if (this.Owner.World.Towns[cityIdx].TrueExtent <= CPU.SignedQuotient((short)Tables.BuildingWord(24 + wonderIdx, BuildingTable.Price), 10))
				return false;

			if (this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, (short)Tables.BuildingWord(24 + wonderIdx, BuildingTable.Technology)) == 0)
				return false;

			this.Owner.World.MarvelTownIdx[wonderIdx] = (short)cityIdx;
			this.Owner.World.Towns[cityIdx].MaterialsTally = 0;

			this.Owner.UnitOps.AttachReplayContent(10, (byte)rivalIdx, (byte)wonderIdx);

			EmptyMessage()
				.City((short)cityIdx)
				.Text(" (")
				.Text(Tables.CivilizationName(rivalIdx))
				.Text(")\nbuilds ")
				.Text(GameTables.BuildingRecordText(24 + wonderIdx))
				.Text(".\n");

			this.Owner.World.Participants[rivalIdx].Gold -= (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[rivalIdx].Gold / 3, 0,
				(10 * (short)Tables.BuildingWord(24 + wonderIdx, BuildingTable.Price)) - this.Owner.World.Towns[cityIdx].MaterialsTally);

			if (MapView.WorldMapView != 1)
			{
				this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[rivalIdx].ChiefVisuals, LeaderTable.ShortTune), 0);

				Screen.DialogStyle = (this.Owner.World.Participants[person].Relations[rivalIdx] & 0x40) == 0 ? (ushort)2 : (ushort)5;

				ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)(wonderIdx + 0x18), 1);

				RepaintMainScreen();

				this.Owner.Services.PerformSound(1, 0);
			}

			if (wonderIdx == 0xe)
			{
				this.Owner.Advisors.UncoverTechnology((short)rivalIdx, 0);

				this.Owner.Advisors.UncoverTechnology((short)rivalIdx, 0);
			}

			for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (HumanCityBuilds(townIdx, wonderIdx))
					this.Owner.Advisors.RenewCityScreen((short)townIdx);
			}

			return true;
		}

		private bool HumanCityBuilds(ushort townIdx, ushort wonderIdx)
		{
			return this.Owner.World.Towns[townIdx].ConditionMarker != 0xff &&
				this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
				this.Owner.World.Towns[townIdx].PresentMakingIdx == (short)-(wonderIdx + 0x18);
		}
	}
}
