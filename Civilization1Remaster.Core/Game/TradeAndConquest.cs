using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2459.
	/// </summary>
	public class TradeAndConquest
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public TradeAndConquest(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Conquers or incites revolt in a city.
		/// </summary>
		/// <param name="participantIdx">New owner</param>
		/// <param name="townIdx">City index</param>
		/// <param name="marker">0 if military conquest, 1 if bought/incited revolt</param>
		public void SeizeCity(short participantIdx, short townIdx, ushort marker)
		{
			this.processor.Journal.OpenScope($"SeizeCity({participantIdx}, {townIdx}, {marker})");

			var town = this.owner.World.Towns[townIdx];
			short originalOwner = town.ParticipantIdx;
			short cityX = (short)town.Place.X;
			short cityY = (short)town.Place.Y;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.World.Participants[originalOwner].TouchParticipantTimeleft = -2;
			}

			// Civil war / schism check if capital captured
			if (originalOwner != this.owner.World.PersonParticipantIdx)
			{
				bool hasPalace = (town.StructureMarkers0 & 1) != 0;

				if (CityConquestRules.ShouldTriggerSchism(hasPalace, marker != 0,
					this.owner.World.Participants[originalOwner].TownTally,
					this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing,
					this.owner.World.Participants[originalOwner].Standing))
				{
					ushort schismResult = this.owner.Secession.DivideCivilization(originalOwner);
					if (schismResult == 0)
					{
						town.ParticipantIdx = (sbyte)participantIdx;
						ushort capitalTownIdx = this.owner.Secession.ObtainDistanceToCapital(originalOwner);
						if (capitalTownIdx != 0xffff && capitalTownIdx < 128)
						{
							this.owner.World.Towns[capitalTownIdx].PresentMakingIdx = -1;
						}
						town.ParticipantIdx = (sbyte)originalOwner;
					}
				}
			}

			// If palace present, destroy victim spaceship
			if ((town.StructureMarkers0 & 1) != 0)
			{
				EliminateSpaceship(originalOwner);
			}

			if (originalOwner == this.owner.World.PersonParticipantIdx)
			{
				this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = -1;
			}

			int plunderAmount = ComputeCityPlunder(townIdx);
			plunderAmount = CityConquestRules.ClampPlunder(plunderAmount, (int)this.owner.World.Participants[originalOwner].Gold);

			// Barbarian capture of player city with weak unit check
			bool isBarbarian = (participantIdx == 0);
			if (isBarbarian)
			{
				short attackerUnitIdx = (short)this.owner.MapOps.ObtainUnitAtTile(cityX, cityY);
				if (attackerUnitIdx >= 0 && attackerUnitIdx < this.owner.World.Participants[0].Troops.Length)
				{
					int unitKind = this.owner.World.Participants[0].Troops[attackerUnitIdx].KindIdx;
					if (unitKind >= 0 && unitKind < this.owner.World.TroopSpecs.Length)
					{
						int assaultPower = this.owner.World.TroopSpecs[unitKind].AssaultPower;
						if (CityConquestRules.ShouldBarbarianRaidRazeCity(assaultPower, this.processor.PullGlobalWord(0x28bc)))
						{
							this.owner.Advisors.FinalizeProduction(townIdx, (ushort)cityX, (ushort)cityY);
							town.TrueExtent = 0;
							goto ApplyGoldTransfer;
						}
					}
				}
			}

			// Building destruction & population reduction
			town.MaterialsTally = 0;
			town.ConditionMarker &= 0xae;
			int diceVal = this.owner.Libc.Dice.DrawInt(2);
			town.StructureMarkers0 = CityConquestRules.ComputeCapturedBuildingFlags(town.StructureMarkers0, diceVal, marker != 0);
			if (marker == 0)
			{
				town.StructureMarkers1 = 0;
			}

			int newSize = CityConquestRules.ComputeCapturedCitySize(town.TrueExtent, marker != 0);
			if (newSize != town.TrueExtent)
			{
				town.TrueExtent = (sbyte)newSize;
				if (CityConquestRules.IsCityDestroyed(town.TrueExtent))
				{
					this.owner.Advisors.FinalizeProduction(townIdx, (ushort)cityX, (ushort)cityY);
				}
			}

		ApplyGoldTransfer:
			this.owner.World.Participants[originalOwner].Gold -= (short)plunderAmount;
			if (participantIdx != 0)
			{
				this.owner.World.Participants[participantIdx].Gold += (short)plunderAmount;
			}

			if (town.TrueExtent != 0)
			{
				// Record replay event 9 (City Captured)
				this.owner.UnitOps.AttachReplayContent(9, (byte)participantIdx, town.LabelIdx, (byte)cityX, (byte)cityY);

				this.owner.Bot.DisableStrategicLocation(participantIdx, 0, cityX, cityY, 4);
				town.ConditionMarker &= 0x9b;

				// Assign city catchment dots (radius of 20 tiles)
				for (short dIdx = 0; dIdx < 20; dIdx++)
				{
					short tunedX = (short)this.owner.Routing.TunePosition(this.owner.TownDeltas[dIdx].X + cityX);
					short tunedY = (short)(this.owner.TownDeltas[dIdx].Y + cityY);
					this.owner.Services.AssignDot(2, tunedX + 0x50, tunedY, (ushort)participantIdx);
				}
			}

			town.ParticipantIdx = (sbyte)participantIdx;

			// UI feedback & city screen
			if (participantIdx == this.owner.World.PersonParticipantIdx || originalOwner == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.KeepKnownTileImprovements(cityX, cityY);
				if (originalOwner == this.owner.World.PersonParticipantIdx)
				{
					this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx, cityX, cityY);
				}
				this.owner.MapOps.RenderMapTile(cityX, cityY);

				ushort captorNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
				this.owner.Libc.CopyText(0xba06, captorNamePtr);
				this.owner.Libc.AppendText(0xba06, " capture\n");
				ObtainCityLabel(townIdx);
				this.owner.Libc.AppendText(0xba06, ". ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)plunderAmount, 10));
				this.owner.Libc.AppendText(0xba06, " gold\npieces plundered.\n");

				ushort leaderSoundOffset = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.owner.World.Participants[participantIdx].ChiefVisuals * 0x3a + 0x1518));
				this.owner.Services.PerformSound(leaderSoundOffset, 0);

				if ((this.owner.World.SessionPrefMarkers & 8) != 0)
				{
					this.owner.Startup.ConcealMouseCursor();
					this.owner.CityScene.DisplayCityScreen(townIdx, -2);
					this.owner.Startup.DisplayMouseCursor();
					this.owner.Startup.QueryForDisk(1, 1);
					this.owner.CityScene.RenderCitySceneryForPlayer(participantIdx);
					this.owner.Startup.QueryForDisk(0, 1);
					this.owner.TurnHooks.RepaintMainScreen();
				}
				else
				{
					this.owner.Paper.DisplayNewspaper(townIdx);
				}

				this.owner.Services.PerformSound(1, 0);
				town.ShownExtent = (participantIdx == this.owner.World.PersonParticipantIdx) ? (sbyte)0 : town.TrueExtent;
			}
			else
			{
				// Foreign capture: notify if player has contact/embassy with either civ
				var human = this.owner.World.Participants[this.owner.World.PersonParticipantIdx];
				if ((human.Relations[participantIdx] & 0x40) != 0 || (human.Relations[originalOwner] & 0x40) != 0)
				{
					this.processor.EmitGlobalWord(0x2f9e, 0x0);
					this.owner.Libc.CopyText(0xba06, "Spies report:\n");
					ushort captorNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
					this.owner.Libc.AppendText(0xba06, captorNamePtr);
					this.owner.Libc.AppendText(0xba06, (ushort)(town.TrueExtent != 0 ? 0x2893 : 0x28a1));
					ushort victimNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((originalOwner << 1) + 0x1982));
					this.owner.Libc.AppendText(0xba06, victimNamePtr);
					this.owner.Libc.AppendText(0xba06, " city\nof ");
					ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, ".\n");
					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				}
			}

			// Steal technology
			ChooseTechnologyToTake(participantIdx, originalOwner);

			// Map assign city owner
			this.owner.MapOps.MapAssignCityOwner(participantIdx, cityX, cityY);

			// Eliminate garrison units of original owner that belonged to this city and were outside or in destroyed city
			for (short troopIdx = 0; troopIdx < 128; troopIdx++)
			{
				var troop = this.owner.World.Participants[originalOwner].Troops[troopIdx];
				if (troop.KindIdx != -1)
				{
					if (CityConquestRules.ShouldEliminateGarrison(troop.BaseTownIdx, townIdx, troop.Place.X, troop.Place.Y, cityX, cityY, town.TrueExtent))
					{
						this.owner.UnitOps.EliminateUnit(originalOwner, troopIdx);
					}
				}
			}

			// Disorder flag if captured by human under Republic/Democracy
			if (participantIdx == this.owner.World.PersonParticipantIdx &&
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind < 4)
			{
				town.ConditionMarker |= 1;
			}

			// Production adjustment for new owner
			if (town.TrueExtent != 0)
			{
				if (town.PresentMakingIdx >= 0)
				{
					this.owner.World.Participants[originalOwner].TroopsInsideMaking[town.PresentMakingIdx]--;
				}

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					this.owner.CityOps.HandleCity(townIdx, 1);
					this.owner.TurnHooks.RepaintMainScreen();
				}
				else
				{
					town.PresentMakingIdx = 0;
					ushort pick = this.owner.Advisors.PickCityProduction(participantIdx, townIdx);
					town.PresentMakingIdx = (sbyte)(byte)pick;
					if (town.PresentMakingIdx >= 0)
					{
						this.owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]++;
					}
				}
			}

			// Eliminate civilization if victim lost last city
			this.owner.NewGameWizard.EliminateCivilization(originalOwner, participantIdx);

			this.processor.Journal.CloseScope("SeizeCity");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void EliminateSpaceship(short participantIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSpaceship({participantIdx})");

			ushort civNameOffset = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
			this.owner.Libc.CopyText(0xba06, civNameOffset);
			this.owner.Libc.AppendText(0xba06, " spaceship\n");

			bool hasSpaceship = (this.owner.World.StarshipMarkers & (1 << (participantIdx + 8))) != 0;
			if (hasSpaceship)
			{
				bool isLaunched = (this.owner.World.StarshipMarkers & (1 << participantIdx)) != 0;
				if (isLaunched)
				{
					this.owner.Libc.AppendText(0xba06, 0x28ca);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, 0x28dd);
				}

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.World.StarshipMarkers &= (short)~(1 << participantIdx);
			this.owner.World.StarshipMarkers &= (short)~(1 << (participantIdx + 8));

			this.owner.NewGameWizard.WipeSpaceshipContent(participantIdx);

			this.processor.Journal.CloseScope("EliminateSpaceship");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <returns></returns>
		public ushort ComputeCityPlunder(short townIdx)
		{
			this.processor.Journal.OpenScope($"ComputeCityPlunder({townIdx})");

			int participantIdx = this.owner.World.Towns[townIdx].ParticipantIdx;
			int citySize = this.owner.World.Towns[townIdx].TrueExtent;
			int victimGold = this.owner.World.Participants[participantIdx].Gold;
			int grandTownExtent = this.owner.World.Participants[participantIdx].GrandTownExtent;

			int plunder = TradeRules.ComputeCityPlunder(victimGold, citySize, grandTownExtent);

			this.processor.Journal.CloseScope("ComputeCityPlunder");
			return (ushort)plunder;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		public void ChooseTechnologyToTake(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"ChooseTechnologyToTake({participantIdx1}, {participantIdx2})");

			if (participantIdx1 == 0 || participantIdx2 == 0)
			{
				this.processor.Journal.CloseScope("ChooseTechnologyToTake");
				return;
			}

			this.owner.Libc.CopyText(0xba06, "Select one...\n ");

			List<short> eligibleTechs = new List<short>();
			short bestTechForAi = -1;
			int highestAiTechScore = -1;

			for (short techId = 71; techId >= 0; techId--)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx1, techId) != 0)
					continue;

				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx2, techId) == 0)
					continue;

				this.owner.Libc.AppendText(0xba06, (ushort)(techId * 0x16 + 0x4da));
				this.owner.Libc.AppendText(0xba06, "\n ");

				eligibleTechs.Add(techId);

				int techScore = this.owner.Advisors.ObtainTechnologyInfo(participantIdx1, (ushort)techId);
				if (techScore > highestAiTechScore)
				{
					highestAiTechScore = techScore;
					bestTechForAi = techId;
				}
			}

			if (eligibleTechs.Count == 0)
			{
				this.processor.Journal.CloseScope("ChooseTechnologyToTake");
				return;
			}

			if (participantIdx1 == this.owner.World.PersonParticipantIdx)
			{
				int selectedOption = -1;
				while (selectedOption < 0 || selectedOption >= eligibleTechs.Count)
				{
					if (this.processor.PullGlobalWord(0x3934) == 0xffff)
					{
						this.owner.TurnHooks.DisplayDialog(0xba06, 80, 32);
					}
					else
					{
						this.processor.StackWord(0);
						this.processor.StackWord(0x07fe);
						this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
						this.processor.UnstackDoubleWord();
					}

					selectedOption = (short)this.processor.RegAcc.Value16;
				}

				short chosenTech = eligibleTechs[selectedOption];
				this.owner.Advisors.EarnTechnology(participantIdx1, chosenTech, participantIdx2);
			}
			else
			{
				if (participantIdx2 == this.owner.World.PersonParticipantIdx)
				{
					ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1992));
					this.owner.Libc.CopyText(0xba06, civNamePtr);
					this.owner.Libc.AppendText(0xba06, " take\n");
					this.owner.Libc.AppendText(0xba06, (ushort)(bestTechForAi * 0x16 + 0x4da));
					this.owner.Libc.AppendText(0xba06, ".\n");

					if (this.processor.PullGlobalWord(0x3934) == 0xffff)
					{
						this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
					}
					else
					{
						this.processor.StackWord(0);
						this.processor.StackWord(0x089e);
						this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
						this.processor.UnstackDoubleWord();
					}
				}

				this.owner.Advisors.EarnTechnology(participantIdx1, bestTechForAi, participantIdx2);
				this.owner.Bot.RefreshCityProduction(participantIdx1, 0xffff);
			}

			this.processor.Journal.CloseScope("ChooseTechnologyToTake");
		}

		/// <summary>
		/// Appends buffer at 0xba06 with city name
		/// </summary>
		/// <param name="cityID"></param>
		public void ObtainCityLabel(short townIdx)
		{
			this.processor.Journal.OpenScope($"ObtainCityLabel({townIdx})");

			if (townIdx != -1)
			{
				byte townLabelIdx = this.owner.World.Towns[townIdx].LabelIdx;
				ushort charsDisplacement = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06));

				for (int idx = 0; idx < 0xd; idx++)
				{
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(charsDisplacement + idx), (byte)this.owner.World.TownLabels[townLabelIdx][idx]);
				}
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, "NONE");
			}

			this.processor.Journal.CloseScope("ObtainCityLabel");
		}

		/// <summary>
		/// Waits for Key press or Mouse click
		/// </summary>
		public void AwaitForKeyPressOrMouseClick()
		{
			this.processor.Journal.OpenScope("AwaitForKeyPressOrMouseClick()");

			this.owner.TurnLogic.AwaitForKey();

			while (true)
			{
				this.owner.Startup.ObtainMouseState();
				if (this.owner.GlobDb3a != 0)
				{
					break;
				}

				this.owner.Libc.KeyAvailable();
				if (this.processor.RegAcc.Value16 != 0)
				{
					if (this.owner.GlobDb3a == 0)
					{
						this.owner.Libc.ReadKey();
						if (this.processor.RegAcc.Value16 == 0)
						{
							this.owner.Libc.ReadKey();
						}
					}
					break;
				}
			}

			this.processor.Journal.CloseScope("AwaitForKeyPressOrMouseClick");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="cityID"></param>
		/// <returns></returns>
		public ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"FoundTradeRoute({participantIdx}, {troopIdx}, {townIdx})");

			short sourceTownIdx = this.owner.World.Participants[participantIdx].Troops[troopIdx].BaseTownIdx;

			int distance = TradeRules.ComputeDistance(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y,
				this.owner.World.Towns[sourceTownIdx].Place.X,
				this.owner.World.Towns[sourceTownIdx].Place.Y);

			int sourceContinent = this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[sourceTownIdx].Place.X,
				this.owner.World.Towns[sourceTownIdx].Place.Y);

			int destContinent = this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			int sourceCommerce = this.owner.World.Towns[sourceTownIdx].RootCommerce;
			int destCommerce = this.owner.World.Towns[townIdx].RootCommerce;
			bool sameContinent = (sourceContinent == destContinent);
			bool sameCiv = (this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx);
			bool destHasRailroad = (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, 0x31) != 0);
			bool destHasFlight = (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.Towns[townIdx].ParticipantIdx, 0x26) != 0);

			int revenue = TradeRules.ComputeOneTimeTradeBonus(
				distance,
				sourceCommerce,
				destCommerce,
				sameContinent,
				sameCiv,
				destHasRailroad,
				destHasFlight);

			this.owner.World.Participants[participantIdx].Gold += (short)revenue;
			this.owner.World.Participants[participantIdx].InquiryHeadway += (short)revenue;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				ushort resourceLabelPtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(((troopIdx & 7) << 1) + 0x1972));
				this.owner.Libc.CopyText(0xba06, resourceLabelPtr);
				this.owner.Libc.AppendText(0xba06, " caravan from ");
				ObtainCityLabel(sourceTownIdx);
				this.owner.Libc.AppendText(0xba06, "\narrives in ");
				ObtainCityLabel(townIdx);
				this.owner.Libc.AppendText(0xba06, "\nTrade route established\nRevenue: $");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)revenue, 10));
				this.owner.Libc.AppendText(0xba06, ".\n");

				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
				this.owner.TurnHooks.RenderStatusBar();
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			int newRouteValue = (sbyte)this.owner.World.Towns[townIdx].RootCommerce;
			if (this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
			{
				newRouteValue *= 2;
			}

			TradeRules.TryAssignTradeRouteSlot(
				this.owner.World.Towns[sourceTownIdx].CommerceTownIndices,
				townIdx,
				newRouteValue,
				existingCityId =>
				{
					int val = (sbyte)this.owner.World.Towns[existingCityId].RootCommerce;
					if (this.owner.World.Towns[existingCityId].ParticipantIdx != participantIdx)
					{
						val *= 2;
					}
					return val;
				});

			this.processor.Journal.CloseScope("FoundTradeRoute");
			return 1;
		}
	}
}
