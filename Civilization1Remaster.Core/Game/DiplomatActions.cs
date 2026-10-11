using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Pure C# domain logic and game UI integration for diplomat actions.
	/// Translated from original CIV.EXE overlay 22.
	/// </summary>
	public class DiplomatActions
	{
		private const int TroopsRoot = 0x81d4;
		private const int ParticipantTroopsExtent = 0x600;
		private const int TroopExtent = 0xc;

		private readonly GameEngine owner;
		private readonly CPU processor;

		public DiplomatActions(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Handles arrival of a diplomat unit at a target city and presents available actions.
		/// </summary>
		public void DiplomatArrivesAtCity(short townIdx, short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DiplomatArrivesAtCity({townIdx}, {participantIdx}, {troopIdx})");

			short victimIdx = this.owner.World.Towns[townIdx].ParticipantIdx;

			// Clear destination / goto of diplomat
			this.owner.World.Participants[participantIdx].Troops[troopIdx].TravelTowardPlace.X = -1;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(TroopsRoot + participantIdx * ParticipantTroopsExtent + troopIdx * TroopExtent + 6), 0xff);

			ushort civNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
			this.owner.Libc.CopyText(0xba06, civNamePtr);
			this.owner.Libc.AppendText(0xba06, " diplomat arrives\nin ");
			this.owner.Conquest.ObtainCityLabel(townIdx);
			this.owner.Libc.AppendText(0xba06, "...\n Establish Embassy\n Investigate City\n Steal Technology\n Industrial Sabotage\n Incite a Revolt\n Meet with King\n");

			int action;
			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				// AI diplomat chooses action 2 (Steal Technology)
				action = 2;
				if (victimIdx == this.owner.World.PersonParticipantIdx)
				{
					if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 2) != 0)
					{
						// Peace treaty with human: eliminate diplomat and exit
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						this.processor.Journal.CloseScope("DiplomatArrivesAtCity");
						return;
					}
				}
			}
			else
			{
				// Human diplomat: determine available actions
				ushort disabledMask = 0;
				if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[victimIdx] & 0x40) != 0 || victimIdx == 0)
				{
					disabledMask |= 1; // Establish Embassy unavailable
				}

				if ((this.owner.World.Towns[townIdx].ConditionMarker & 0x20) != 0)
				{
					disabledMask |= 4; // Steal Tech unavailable
				}

				if ((this.owner.World.Towns[townIdx].StructureMarkers0 & 1) != 0)
				{
					disabledMask |= 0x10; // Incite Revolt unavailable (capital)
				}

				if (victimIdx == 0)
				{
					disabledMask |= 0x20; // Meet with King unavailable (barbarians)
				}

				this.processor.EmitGlobalWord(0xb276, disabledMask);
				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
				action = (short)this.processor.RegAcc.Value16;
			}

			switch (action)
			{
				case 0: // Establish Embassy
				{
					this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[victimIdx] |= 0x40;
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					this.owner.Briefings.DisplayIntelligenceReport(victimIdx);
					break;
				}

				case 1: // Investigate City
				{
					this.owner.Advisors.RenewCityScreen(townIdx);
					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					break;
				}

				case 2: // Steal Technology
				{
					if (participantIdx == this.owner.World.PersonParticipantIdx)
					{
						if ((this.owner.World.Towns[townIdx].ConditionMarker & 0x20) != 0)
						{
							break;
						}

						this.owner.Combat.AffirmBreakTreaty(this.owner.World.Towns[townIdx].ParticipantIdx);
						if (this.processor.RegAcc.Value16 != 0xffff)
						{
							this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx, this.owner.World.Towns[townIdx].ParticipantIdx, 2);
						}
						else
						{
							break;
						}
					}
					else
					{
						if ((this.owner.World.Towns[townIdx].ConditionMarker & 0x20) != 0)
						{
							PerformSabotage(townIdx, participantIdx, troopIdx);
							break;
						}
					}

					int startSeed = this.owner.Libc.Dice.DrawInt(72);
					int stolenTech = DiplomatRules.FindTechnologyToSteal(
						startSeed,
						tech => victimIdx != 0 && this.owner.Advisors.PlayerKnowsTechnology(victimIdx, (short)tech) != 0,
						tech => this.owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)tech) != 0
					);

					if (stolenTech >= 0)
					{
						this.owner.Advisors.EarnTechnology(participantIdx, (short)stolenTech, victimIdx);

						ushort stealerCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1992));
						this.owner.Libc.CopyText(0xba06, stealerCivNamePtr);
						this.owner.Libc.AppendText(0xba06, " steal\n");
						this.owner.Libc.AppendText(0xba06, (ushort)(stolenTech * 0x16 + 0x4da));
						this.owner.Libc.AppendText(0xba06, ".\n");
						this.processor.EmitGlobalWord(0x2f9e, 0);
						this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

						this.owner.World.Towns[townIdx].ConditionMarker |= 0x20;
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
					else
					{
						if (participantIdx == this.owner.World.PersonParticipantIdx)
						{
							this.owner.TurnHooks.DisplayDialog(0x5231, 80, 80);
						}
						else
						{
							PerformSabotage(townIdx, participantIdx, troopIdx);
						}
					}
					break;
				}

				case 3: // Industrial Sabotage
				{
					PerformSabotage(townIdx, participantIdx, troopIdx);
					break;
				}

				case 4: // Incite a Revolt
				{
					ushort distanceToNearest = this.owner.Politics.LocateNearestCityOfPlayer(victimIdx,
						this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);
					if (distanceToNearest == 0)
					{
						break;
					}

					int victimGold = this.owner.World.Participants[victimIdx].Gold;
					int citySize = this.owner.World.Towns[townIdx].TrueExtent;
					bool inDisorder = (this.owner.World.Towns[townIdx].ConditionMarker & 1) != 0;
					int baseCost = DiplomatRules.ComputeInciteRevoltCost(victimGold, distanceToNearest, citySize, inDisorder);

					this.owner.Libc.CopyText(0xba06, "Dissidents in ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, "\nwill revolt for $");
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)baseCost, 10));
					this.owner.Libc.AppendText(0xba06, ".\n");

					int playerGold = this.owner.World.Participants[participantIdx].Gold;
					if (baseCost <= playerGold)
					{
						this.owner.Libc.AppendText(0xba06, " Forget it.\n Incite revolt\n");
						if ((baseCost * 2 <= playerGold) && ((this.owner.World.Participants[participantIdx].Relations[victimIdx] & 2) != 0))
						{
							this.owner.Libc.AppendText(0xba06, " Subvert city ($x2)\n");
						}
					}

					this.processor.EmitGlobalWord(0x2f9e, 0);
					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
					short choice = (short)this.processor.RegAcc.Value16;

					if (choice >= 1)
					{
						if (choice != 1) // Subvert city (choice == 2)
						{
							if ((this.owner.World.Participants[victimIdx].Relations[participantIdx] & 2) != 0)
							{
								this.owner.Politics.WipeDiplomacyFlags(victimIdx, participantIdx, 2);
								this.owner.World.Participants[victimIdx].Relations[participantIdx] |= 8;
							}
						}

						int finalCost = baseCost * choice;
						this.owner.World.Participants[participantIdx].Gold -= (short)finalCost;
						ComputeInciteCost(townIdx, participantIdx);
						this.owner.MapOps.RepaintMapTile(this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						this.owner.TurnHooks.RenderStatusBar();
					}
					break;
				}

				case 5: // Meet with King
				{
					if (victimIdx != 0)
					{
						this.owner.Audience.LaunchAudience(
							victimIdx,
							this.owner.World.Participants[participantIdx].Troops[troopIdx].Place.X,
							this.owner.World.Participants[participantIdx].Troops[troopIdx].Place.Y,
							1);
					}

					this.owner.World.Participants[participantIdx].Troops[troopIdx].LeftoverSteps = 0;
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(TroopsRoot + participantIdx * ParticipantTroopsExtent + troopIdx * TroopExtent + 4), 0);
					break;
				}
			}

			this.processor.Journal.CloseScope("DiplomatArrivesAtCity");
		}

		private void PerformSabotage(short townIdx, short participantIdx, short troopIdx)
		{
			uint structures = (uint)(ushort)this.owner.World.Towns[townIdx].StructureMarkers0 |
				((uint)(ushort)this.owner.World.Towns[townIdx].StructureMarkers1 << 16);

			bool rollToDirectSabotage = (this.owner.Libc.Dice.DrawInt(2) == 0);
			bool hasMaterials = (this.owner.World.Towns[townIdx].MaterialsTally != 0);

			int buildingToDestroy = DiplomatRules.PickIndustrialSabotageTarget(
				rollToDirectSabotage,
				hasMaterials,
				structures,
				() => this.owner.Libc.Dice.DrawInt(24)
			);

			if (buildingToDestroy > 0)
			{
				uint mask = ~(1u << buildingToDestroy);
				this.owner.World.Towns[townIdx].StructureMarkers0 &= (ushort)mask;
				this.owner.World.Towns[townIdx].StructureMarkers1 &= (ushort)(mask >> 16);

				ushort buildingNamePtr = (ushort)(buildingToDestroy * 0x1e + 0xbb8);
				this.owner.Libc.CopyText(0xba06, buildingNamePtr);
				this.owner.Libc.AppendText(0xba06, " destroyed\n");
			}
			else
			{
				this.owner.World.Towns[townIdx].MaterialsTally = 0;
				sbyte makingIdx = (sbyte)this.owner.World.Towns[townIdx].PresentMakingIdx;
				if (makingIdx >= 0)
				{
					ushort unitNamePtr = (ushort)(makingIdx * 0x22 + 0x112a);
					this.owner.Libc.CopyText(0xba06, unitNamePtr);
				}
				else
				{
					int impIdx = -makingIdx;
					ushort impNamePtr = (ushort)(impIdx * 0x1e + 0xb9a);
					this.owner.Libc.CopyText(0xba06, impNamePtr);
				}
				this.owner.Libc.AppendText(0xba06, " production sabotaged\n");
			}

			this.owner.Libc.AppendText(0xba06, "in ");
			this.owner.Conquest.ObtainCityLabel(townIdx);
			this.owner.Libc.AppendText(0xba06, ".\n");
			this.processor.EmitGlobalWord(0x2f9e, 0);
			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);
			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
		}

		/// <summary>
		/// Bribes an enemy unit to defect and join the player's civilization.
		/// </summary>
		public void BuyUnit(short participantIdx, short troopIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"BuyUnit({participantIdx}, {troopIdx}, {participantIdx1})");

			this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 2);
			if (this.processor.RegAcc.Value16 > 1)
			{
				this.processor.Journal.CloseScope("BuyUnit");
				return;
			}

			int unitX = this.owner.World.Participants[participantIdx].Troops[troopIdx].Place.X;
			int unitY = this.owner.World.Participants[participantIdx].Troops[troopIdx].Place.Y;

			this.owner.Politics.LocateNearestCityOfPlayer(participantIdx, (sbyte)unitX, (sbyte)unitY);
			int distance = this.processor.RegAcc.Value16;

			int unitType = this.owner.World.Participants[participantIdx].Troops[troopIdx].KindIdx;
			int unitCost = this.owner.World.TroopSpecs[unitType].Price;
			int victimGold = this.owner.World.Participants[participantIdx].Gold;

			int bribeCost = DiplomatRules.ComputeBribeCost(victimGold, distance, unitCost, unitType);

			if (participantIdx1 != this.owner.World.PersonParticipantIdx)
			{
				if ((this.owner.World.Participants[participantIdx1].Gold / 2) < bribeCost)
				{
					this.processor.Journal.CloseScope("BuyUnit");
					return;
				}
			}

			ushort victimCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
			this.owner.Libc.CopyText(0xba06, victimCivNamePtr);
			this.owner.Libc.AppendText(0xba06, " ");
			this.owner.Libc.AppendText(0xba06, (ushort)(unitType * 0x22 + 0x112a));
			this.owner.Libc.AppendText(0xba06, "\nwill desert for ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)bribeCost, 10));
			this.owner.Libc.AppendText(0xba06, "$.\nTreasury: ");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx1].Gold, 10));
			this.owner.Libc.AppendText(0xba06, "$\n");

			if (bribeCost <= this.owner.World.Participants[participantIdx1].Gold)
			{
				this.owner.Libc.AppendText(0xba06, " Forget it.\n Pay\n");
			}

			if (participantIdx1 == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				if (this.processor.RegAcc.Value16 != 1)
				{
					this.processor.Journal.CloseScope("BuyUnit");
					return;
				}
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.FocusMapView(participantIdx, unitX - 8, unitY - 6);
				this.owner.Libc.CopyText(0xba06, victimCivNamePtr);
				this.owner.Libc.AppendText(0xba06, " ");
				this.owner.Libc.AppendText(0xba06, (ushort)(unitType * 0x22 + 0x112a));
				this.owner.Libc.AppendText(0xba06, " unit\nbribed by ");
				ushort buyerCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx1 << 1) + 0x1992));
				this.owner.Libc.AppendText(0xba06, buyerCivNamePtr);
				this.owner.Libc.AppendText(0xba06, "!\n");
				this.processor.EmitGlobalWord(0x2f9e, 0x0);
				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 40);
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			this.owner.World.Participants[participantIdx].LiveTroops[unitType]--;

			this.owner.UnitOps.ConstructUnit(participantIdx1, (short)unitType, (sbyte)unitX, (sbyte)unitY);
			short newTroopIdx = (short)this.processor.RegAcc.Value16;
			if (newTroopIdx != -1)
			{
				this.owner.World.Participants[participantIdx1].LiveTroops[unitType]++;
				this.owner.World.Participants[participantIdx1].Troops[newTroopIdx].ShownViaParticipant |= (ushort)(1 << participantIdx);
			}

			this.owner.World.Participants[participantIdx1].Gold -= (short)bribeCost;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile((sbyte)unitX, (sbyte)unitY);
				this.owner.TurnHooks.RenderStatusBar();
				this.owner.TextPainter.AwaitTime(30);
			}

			this.processor.Journal.CloseScope("BuyUnit");
		}

		/// <summary>
		/// Renders animation of an explosion at the specified map coordinates.
		/// </summary>
		public void RenderExplosion(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderExplosion({horizLoc}, {vertLoc})");

			this.owner.Startup.QueryForDisk(1, 0);
			if (this.processor.RegAcc.Value16 == 0)
			{
				this.processor.Journal.CloseScope("RenderExplosion");
				return;
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x5318, 1);

			ushort[] animFrames = new ushort[28];
			for (ushort frameIdx = 0; frameIdx < 28; frameIdx++)
			{
				this.owner.VideoCard.ScreenToPicture(1,
					(ushort)(((frameIdx % 7) * 45) + 1),
					(ushort)(((frameIdx / 7) * 45) + 1),
					44, 44);
				animFrames[frameIdx] = this.processor.RegAcc.Value16;
			}

			this.owner.Routing.TunePosition(horizLoc - this.processor.PullGlobalSignedWord(0xd4cc));
			short tunedX = (short)((this.processor.RegAcc.Value16 << 4) + 0x42);
			short tunedY = (short)(((vertLoc - this.processor.PullGlobalWord(0xd75e)) << 4) - 6);

			this.owner.Helpers.VerifyValueSpan(tunedX, 80, 276);
			tunedX = (short)this.processor.RegAcc.Value16;

			this.owner.Helpers.VerifyValueSpan(tunedY, 8, 0x9c);
			tunedY = (short)this.processor.RegAcc.Value16;

			this.owner.Services.PerformSound(0x2a, 0);

			ushort screen0 = this.processor.PullGlobalWord(0xaa);
			ushort screen1 = this.processor.PullGlobalWord(0x19d4);

			this.owner.VideoCard.RenderImage(screen0, tunedX, tunedY, 44, 44, screen1, tunedX, tunedY);

			for (int step = 0; step < 35; step++)
			{
				int frameToDraw = (step >= 28) ? (step - 7) : step;
				this.owner.Services.RenderPictureToScreenAlternate(screen0, tunedX, tunedY, animFrames[frameToDraw]);
				this.owner.TextPainter.AwaitTime(5);
				this.owner.VideoCard.RenderImage(screen1, tunedX, tunedY, 44, 44, screen0, tunedX, tunedY);
			}

			this.owner.Helpers.ReleaseResource(animFrames[0], 0);
			this.owner.Startup.QueryForDisk(0, 1);
			this.owner.TurnHooks.DisplayCivilizationScenery();

			this.processor.Journal.CloseScope("RenderExplosion");
		}

		/// <summary>
		/// Executes city revolt and seizure when incite is chosen.
		/// </summary>
		public void ComputeInciteCost(short townIdx, short participantIdx)
		{
			this.processor.Journal.OpenScope($"ComputeInciteCost({townIdx}, {participantIdx})");

			short originalOwner = this.owner.World.Towns[townIdx].ParticipantIdx;
			int cityX = this.owner.World.Towns[townIdx].Place.X;
			int cityY = this.owner.World.Towns[townIdx].Place.Y;

			if (participantIdx == -1)
			{
				int bestScore = 0;
				short bestTownCandidate = -1;

				for (short otherTownIdx = 0; otherTownIdx < 128; otherTownIdx++)
				{
					if (otherTownIdx == townIdx)
						continue;

					if (this.owner.World.Towns[otherTownIdx].ConditionMarker == 0xff)
						continue;

					if (this.owner.World.Towns[otherTownIdx].ParticipantIdx == originalOwner)
						continue;

					int dist = TradeRules.ComputeDistance(
						cityX, cityY,
						this.owner.World.Towns[otherTownIdx].Place.X,
						this.owner.World.Towns[otherTownIdx].Place.Y);

					if (dist == 0)
						dist = 1;

					this.owner.CityOps.HandleCity(otherTownIdx, -1);
					int score = ((this.processor.PullGlobalWord(0x70e2) - this.processor.PullGlobalWord(0x70e4)) << 5) / dist;

					if (score > bestScore)
					{
						bestScore = score;
						bestTownCandidate = otherTownIdx;
					}
				}

				if (bestScore > 0 && bestTownCandidate != -1)
				{
					participantIdx = this.owner.World.Towns[bestTownCandidate].ParticipantIdx;
				}
			}

			if (participantIdx == -1)
			{
				this.processor.Journal.CloseScope("ComputeInciteCost");
				return;
			}

			sbyte mask = (sbyte)(1 << participantIdx);
			this.owner.World.ChartSight[cityX, cityY] |= mask;

			for (int i = 0; i < 20; i++)
			{
				int dx = this.owner.TownDeltas[i].X;
				int dy = this.owner.TownDeltas[i].Y;
				this.owner.World.ChartSight[cityX + dx, cityY + dy] |= mask;
			}

			short humanPlayer = this.owner.World.PersonParticipantIdx;
			bool showMessage = (originalOwner == humanPlayer || participantIdx == humanPlayer);
			if (!showMessage)
			{
				bool humanKnowsOriginal = (this.owner.World.Participants[humanPlayer].Relations[originalOwner] & 0x40) != 0;
				bool humanKnowsNew = (this.owner.World.Participants[humanPlayer].Relations[participantIdx] & 0x40) != 0;
				showMessage = humanKnowsOriginal || humanKnowsNew;
			}

			if (showMessage)
			{
				if ((this.owner.World.ChartSight[cityX, cityY] & (1 << humanPlayer)) != 0)
				{
					this.owner.MapOps.FocusMapView(humanPlayer, cityX - 8, cityY - 5);
				}

				ushort origCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((originalOwner << 1) + 0x1992));
				this.owner.Libc.CopyText(0xba06, origCivNamePtr);

				if (participantIdx == 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x5322);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, 0x533c);
				}

				this.owner.Conquest.ObtainCityLabel(townIdx);

				if (participantIdx == 0)
				{
					this.owner.Libc.AppendText(0xba06, 0x536c);
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, ".\n");
					ushort newCivNamePtr = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982));
					this.owner.Libc.AppendText(0xba06, newCivNamePtr);
					this.owner.Libc.AppendText(0xba06, 0x5355);
				}

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 32);
			}

			for (short uIdx = 0; uIdx < 128; uIdx++)
			{
				if (this.owner.World.Participants[originalOwner].Troops[uIdx].KindIdx == 0xff)
					continue;

				int ux = this.owner.World.Participants[originalOwner].Troops[uIdx].Place.X;
				int uy = this.owner.World.Participants[originalOwner].Troops[uIdx].Place.Y;

				int uDist = TradeRules.ComputeDistance(cityX, cityY, ux, uy);
				if (uDist > 1)
					continue;

				if (uDist != 0)
				{
					this.owner.MapOps.ObtainTileImprovements((sbyte)ux, (sbyte)uy);
					if ((this.processor.RegAcc.LowByte & 1) != 0)
						continue;
				}

				int unitType = this.owner.World.Participants[originalOwner].Troops[uIdx].KindIdx;
				this.owner.UnitOps.EliminateUnit(originalOwner, uIdx);
				this.owner.MapOps.MapAssignCityOwner(participantIdx, (sbyte)ux, (sbyte)uy);
				this.owner.MapOps.TagTileOwned((sbyte)ux, (sbyte)uy);

				this.owner.UnitOps.ConstructUnit(participantIdx, (short)unitType, (sbyte)ux, (sbyte)uy);
				short newUnitIdx = (short)this.processor.RegAcc.Value16;
				if (newUnitIdx != -1)
				{
					this.owner.World.Participants[participantIdx].Troops[newUnitIdx].ShownViaParticipant |= (ushort)(1 << originalOwner);
					this.owner.World.Participants[participantIdx].Troops[newUnitIdx].BaseTownIdx = townIdx;
				}
			}

			short savedPref = this.owner.World.SessionPrefMarkers;
			this.processor.EmitGlobalWord(0x28bc, 0x1);
			if (this.processor.PullGlobalWord(0xdeba) <= 1)
			{
				this.owner.World.SessionPrefMarkers &= 0x7ff7;
			}

			this.owner.Conquest.SeizeCity(participantIdx, townIdx, 1);

			this.processor.EmitGlobalWord(0x28bc, 0x0);
			this.owner.World.SessionPrefMarkers = savedPref;

			this.owner.MapOps.MapAssignCityOwner(participantIdx,
				this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y);

			this.processor.Journal.CloseScope("ComputeInciteCost");
		}

		/// <summary>
		/// Displays detailed investigation and founding information for a city.
		/// </summary>
		public void ExamineCity(short townIdx)
		{
			this.processor.Journal.OpenScope($"ExamineCity({townIdx})");

			this.owner.Startup.QueryForDisk(1, 0);
			if (this.processor.RegAcc.Value16 == 0)
			{
				this.processor.Journal.CloseScope("ExamineCity");
				return;
			}

			this.owner.Helpers.HaltHueCycling();
			this.owner.Startup.ConcealMouseCursor();
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x536e, 0);

			ushort[] sprites = new ushort[4];
			for (ushort i = 0; i < 4; i++)
			{
				this.owner.VideoCard.ScreenToPicture(1, 1, (ushort)((i << 4) + 1), 47, 15);
				sprites[i] = this.processor.RegAcc.Value16;
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x537b, 0);

			ushort screen0 = this.processor.PullGlobalWord(0xaa);
			ushort screen1 = this.processor.PullGlobalWord(0x19d4);

			this.owner.Services.FloodBox(screen0, 0, 0, 320, 200, 0);
			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x5384, 1);

			this.owner.VideoCard.RenderImage(screen1, 0, 0, 320, 200, screen0, 0, 0);

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Conquest.ObtainCityLabel(townIdx);
			this.owner.Libc.AppendText(0xba06, " founded: ");
			this.owner.TurnHooks.ObtainCurrentYearAsText();
			this.owner.Libc.AppendText(0xba06, ".");

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0 + 0x10), 6);
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 4, 0);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0 + 0x10), 1);

			this.owner.VideoCard.RenderImage(screen1, 0, 120, 320, 40, screen1, 0, 0);

			this.owner.TurnLogic.AwaitForKey();

			for (int a = 0x50; a < 0x78; a++)
			{
				this.owner.Services.RestoreTimer();
				this.owner.VideoCard.RenderImage(screen1, 0, 0, 320, 40, screen1, 0, 120);

				for (int c = 0; c < 4; c++)
				{
					int frameIdx = (c + a) & 3;
					this.owner.Services.RenderPictureToScreen(
						screen1,
						(13 * c) + a,
						((10 * c) & 0xf) + 120,
						sprites[frameIdx]);
				}

				this.owner.VideoCard.RenderImage(screen1, 0, 120, 320, 40, screen0, 0, 120);

				while (this.processor.PullGlobalWord(0x5c) < 10)
				{
					// wait timer ticks
				}

				this.owner.Startup.ObtainMouseState();
				this.owner.Libc.KeyAvailable();
				if (this.processor.RegAcc.Value16 != 0 || this.owner.GlobDb3a != 0)
				{
					break;
				}
			}

			this.owner.Helpers.ReleaseResource(sprites[0], 0x539a);
			this.owner.Startup.QueryForDisk(0, 1);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.FloodColors(10, 0, 0, 0);
			}

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Libc.KeyAvailable();
			if (this.processor.RegAcc.Value16 == 0)
			{
				this.owner.CityScene.DisplayCityScreen(townIdx, -3);
			}
			else
			{
				this.owner.Services.FloodBox(screen0, 0, 0, 320, 200, 0);
				this.owner.TurnHooks.DisplayCivilizationScenery();
			}

			this.owner.Startup.DisplayMouseCursor();
			this.owner.TurnLogic.AwaitForKey();

			this.processor.Journal.CloseScope("ExamineCity");
		}
	}
}
