using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 22.
	/// </summary>
	public class DiplomatActions : GameModule
	{
		public DiplomatActions(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// A diplomat of the player arrives in the city. The human player chooses (0xb276 masks: 1 a known civilization or
		/// barbarians, 4 already robbed, 0x10 a capital, 0x20 barbarians): embassy, investigate, steal technology, sabotage,
		/// incite a revolt, meet the king. An AI player steals (a diplomat at peace with the human player in his city just
		/// disappears); stealing breaks a treaty of the human player and, without a technology to take, becomes sabotage for
		/// the AI. The diplomat is used up, except for the audience. SI and DI get the values of the original before the calls.
		/// </summary>
		public void DiplomatArrivesAtCity(short townIdx, short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DiplomatArrivesAtCity({townIdx}, {participantIdx}, {troopIdx})");


			short person = this.owner.World.PersonParticipantIdx;
			City town = this.owner.World.Towns[townIdx];
			short ownerIdx = town.ParticipantIdx;
			ushort troop = (ushort)((ushort)(0xc * troopIdx) + (ushort)(0x600 * participantIdx));

			this.processor.RegSource.Value16 = (ushort)(0x600 * participantIdx);
			this.Troops.AssignAt(troop, UnitMemory.TravelHoriz, 0xff);

			Message(CivilizationName((ushort)participantIdx))
				.Text(" diplomat arrives\nin ")
				.City(townIdx)
				.Text("...\n Establish Embassy\n Investigate City\n Steal Technology\n Industrial Sabotage\n Incite a Revolt\n Meet with King\n");

			ushort choice;

			if (participantIdx == person)
			{
				this.processor.RegSource.Value16 = (ushort)(ownerIdx << 1);

				if ((this.owner.World.Participants[person].Relations[ownerIdx] & 0x40) != 0 || ownerIdx == 0)
					DisabledLines = 0x1;

				if ((town.ConditionMarker & 0x20) != 0)
					DisabledLinesLow = (byte)((byte)DisabledLines | 4);

				if (town.Has(CityStructure.Palace))
					DisabledLinesLow = (byte)((byte)DisabledLines | 0x10);

				if (ownerIdx == 0)
					DisabledLinesLow = (byte)((byte)DisabledLines | 0x20);

				choice = this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);
			}
			else
			{
				choice = 2;

				if (ownerIdx == person)
				{
					this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

					if ((this.owner.World.Participants[person].Relations[participantIdx] & 2) != 0)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						FinishDiplomatArrival();
						return;
					}
				}
			}

			switch (choice)
			{
				case 0:
					this.processor.RegSource.Value16 = (ushort)(ownerIdx << 1);

					this.owner.World.Participants[person].Relations[ownerIdx] |= 0x40;

					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

					this.owner.Briefings.DisplayIntelligenceReport(ownerIdx);
					break;

				case 1:
					this.owner.Advisors.RenewCityScreen(townIdx);

					this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					break;

				case 2:
					StealTechnology(townIdx, participantIdx, troopIdx, ownerIdx);
					break;

				case 3:
					SabotageCity(townIdx, participantIdx, troopIdx);
					break;

				case 4:
					InciteRevolt(townIdx, participantIdx, troopIdx, ownerIdx);
					break;

				case 5:
					if (ownerIdx != 0)
					{
						this.processor.RegSource.Value16 = troop;

						this.owner.Audience.LaunchAudience(ownerIdx, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
							(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert), 1);
					}

					this.processor.RegSource.Value16 = (ushort)(0x600 * participantIdx);
					this.Troops.AssignAt(troop, UnitMemory.LeftoverSteps, 0);
					break;
			}

			FinishDiplomatArrival();
		}

		private void FinishDiplomatArrival()
		{
			// Far return
			this.processor.Journal.CloseScope("DiplomatArrivesAtCity");
		}

		/// <summary>
		/// The diplomat steals a technology the city's owner knows and the player doesn't (from a random one of the 72), once
		/// a city (0x20); the human player breaks his treaty first (unless he declines) and hears when there is none; the AI
		/// sabotages instead
		/// </summary>
		private void StealTechnology(short townIdx, short participantIdx, short troopIdx, short ownerIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			City town = this.owner.World.Towns[townIdx];

			if (participantIdx == person)
			{
				if ((town.ConditionMarker & 0x20) != 0)
					return;

				if (this.owner.Combat.AffirmBreakTreaty(town.ParticipantIdx) == 0xffff)
					return;

				this.owner.Politics.WipeDiplomacyFlags(person, town.ParticipantIdx, 2);
			}
			else if ((town.ConditionMarker & 0x20) != 0)
			{
				SabotageCity(townIdx, participantIdx, troopIdx);
				return;
			}

			this.owner.Words.Clear();

			ushort first = (ushort)this.owner.Libc.Dice.DrawInt(72);

			for (ushort attempt = 0; attempt < 0x48; attempt++)
			{
				short scienceIdx = (short)((short)(first + attempt) % 0x48);

				if (participantIdx == 0 ||
					this.owner.Advisors.PlayerKnowsTechnology(ownerIdx, scienceIdx) == 0 ||
					this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					continue;

				this.owner.Advisors.EarnTechnology(participantIdx, scienceIdx, ownerIdx);

				Message(CivilizationPlural((ushort)participantIdx))
					.Text(" steal\n")
					.Text(TechnologyText(scienceIdx))
					.Text(".\n")
					.Style(0x0)
					.Dialog(80, 80);

				town.ConditionMarker |= 0x20;
				break;
			}

			if (this.owner.Words[0] != 0)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			}
			else if (participantIdx == person)
			{
				this.owner.TurnHooks.DisplayDialog(0x5231, 80, 80);
			}
			else
			{
				SabotageCity(townIdx, participantIdx, troopIdx);
			}
		}

		/// <summary>
		/// The diplomat destroys a random building of the city (up to 0x63 tries; one time in 2 with production in the city
		/// the production instead) or else the production
		/// </summary>
		private void SabotageCity(short townIdx, short participantIdx, short troopIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			ushort tries = 0;
			ushort buildingIdx = 0;
			bool destroyed = false;

			if (this.owner.Libc.Dice.DrawInt(2) != 0 || town.MaterialsTally == 0)
			{
				do
				{
					buildingIdx = (ushort)this.owner.Libc.Dice.DrawInt(24);
					tries++;

					if ((short)tries >= 0x63)
						break;
				}
				while (buildingIdx == 0 || !town.HasStructureSignExtended(buildingIdx));

				if ((short)tries < 0x63)
				{
					ushort keep = (ushort)~(ushort)(1 << (byte)buildingIdx);

					town.StructureMarkers0 &= keep;
					town.StructureMarkers1 &= (keep & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

					Message(BuildingText(buildingIdx))
						.Text(" destroyed\n");

					destroyed = true;
				}
			}

			if (!destroyed)
			{
				this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

				town.MaterialsTally = 0;

				Message(town.PresentMakingIdx >= 0 ? UnitText(town.PresentMakingIdx) : BuildingRecordText(-town.PresentMakingIdx))
					.Text(" production sabotaged\n");
			}

			this.owner.Words.Append("in ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Words.Append(".\n");

			DialogStyle = 0x0;

			this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
		}

		/// <summary>
		/// The dissidents revolt for (owner's gold + 1000) / (distance to the owner's capital + 3) * size, halved in disorder;
		/// the player may pay it, or twice it to subvert a city of a player at peace; inciting breaks the peace
		/// </summary>
		private void InciteRevolt(short townIdx, short participantIdx, short troopIdx, short ownerIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			ushort distance = this.owner.Politics.LocateNearestCityOfPlayer(ownerIdx, town.Place.X, town.Place.Y);

			if (distance == 0)
				return;

			short cost = (short)(CPU.SignedQuotient((short)(ushort)((ushort)this.owner.World.Participants[ownerIdx].Gold + 0x3e8), (short)(distance + 3)) * town.TrueExtent);

			if ((town.ConditionMarker & 1) != 0)
				cost = CPU.SignedQuotient(cost, 2);

			Message("Dissidents in ")
				.City(townIdx)
				.Text("\nwill revolt for $")
				.Number(cost)
				.Text(".\n");

			if (cost <= this.owner.World.Participants[participantIdx].Gold)
			{
				this.owner.Words.Append(" Forget it.\n Incite revolt\n");

				if ((short)(cost << 1) <= this.owner.World.Participants[participantIdx].Gold)
				{
					this.processor.RegDest.Value16 = (ushort)(participantIdx << 4);

					if ((this.owner.World.Participants[participantIdx].Relations[ownerIdx] & 2) != 0)
					{
						this.owner.Words.Append(" Subvert city ($x2)\n");
					}
				}
			}

			DialogStyle = 0x0;

			short choice = (short)this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

			if (choice < 1)
				return;

			if (choice == 1)
			{
				this.processor.RegSource.Value16 = (ushort)((ownerIdx << 4) + (participantIdx << 1));

				if ((this.owner.World.Participants[ownerIdx].Relations[participantIdx] & 2) != 0)
				{
					this.owner.Politics.WipeDiplomacyFlags(ownerIdx, participantIdx, 2);

					this.owner.World.Participants[ownerIdx].Relations[participantIdx] |= 8;
				}
			}

			this.owner.World.Participants[participantIdx].Gold -= (short)(cost * choice);

			ComputeInciteCost(townIdx, participantIdx);

			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.MapOps.RepaintMapTile(town.Place.X, town.Place.Y);

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			this.owner.TurnHooks.RenderStatusBar();
		}

		/// <summary>
		/// A diplomat of participantIdx1 bribes the unit (only a unit without others in its stack): the price is (gold + 750) /
		/// (distance to the owner's capital + 2) * the unit's cost, halved for all but settlers. The AI pays when it has twice the
		/// price; the human player is asked ("Forget it." / "Pay" when the treasury covers it). The unit changes sides (seen by its
		/// former owner) and the human player, if bribed, is told. The stack frame and the saved SI and DI stay like in the
		/// original: TallyUnitsByDomainInStack changes SI.
		/// </summary>
		public void BuyUnit(short participantIdx, short troopIdx, short participantIdx1)
		{
			this.processor.Journal.OpenScope($"BuyUnit({participantIdx}, {troopIdx}, {participantIdx1})");


			short humanIdx = this.owner.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(this.processor);

			if ((short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 2) <= 1)
			{
				short distance = (short)this.owner.Politics.LocateNearestCityOfPlayer(participantIdx,
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
				sbyte kind = troops.TroopKind(participantIdx, troopIdx);
				short price = (short)(CPU.SignedQuotient((short)(this.owner.World.Participants[participantIdx].Gold + 0x2ee), (short)(distance + 2)) *
					UnitKindWord(kind, UnitKindTable.Price));

				if (kind != 0)
				{
					price = CPU.SignedQuotient(price, 2);
				}

				if (participantIdx1 == humanIdx || this.owner.World.Participants[participantIdx1].Gold / 2 >= price)
				{
					OfferBribe(participantIdx, troopIdx, participantIdx1, price);
				}
			}


			this.processor.Journal.CloseScope("BuyUnit");
		}

		/// <summary>The offer of BuyUnit and, when it is taken, the unit changing sides</summary>
		private void OfferBribe(short participantIdx, short troopIdx, short participantIdx1, short price)
		{
			short humanIdx = this.owner.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(this.processor);
			ushort kindName = (ushort)((troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride) + UnitKindTable.Origin);

			Message(CivilizationName(participantIdx))
				.Text(" ")
				.Text(kindName)
				.Text("\nwill desert for ")
				.Number(price)
				.Text("$.\nTreasury: ")
				.Number(this.owner.World.Participants[participantIdx1].Gold)
				.Text("$\n");

			if (price <= this.owner.World.Participants[participantIdx1].Gold)
			{
				this.owner.Words.Append(" Forget it.\n Pay\n");
			}

			if (participantIdx1 == humanIdx)
			{
				if (this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80) != 1)
					return;
			}

			if (participantIdx == humanIdx)
			{
				this.owner.MapOps.FocusMapView(participantIdx, troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 8,
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - 6);

				Message(CivilizationName(participantIdx))
					.Text(" ")
					.Text(kindName)
					.Text(" unit\nbribed by ")
					.Text(CivilizationPlural(participantIdx1))
					.Text("!\n")
					.Style(0)
					.Dialog(80, 40);
			}

			short kind = troops.TroopKind(participantIdx, troopIdx);

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			this.owner.World.Participants[participantIdx].LiveTroops[kind]--;

			// The record keeps its position after EliminateUnit
			ushort bribedIdx = this.owner.UnitOps.ConstructUnit(participantIdx1, kind,
				troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if (bribedIdx != 0xffff)
			{
				this.owner.World.Participants[participantIdx1].LiveTroops[kind]++;

				troops.Assign(participantIdx1, bribedIdx, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIdx1, bribedIdx, UnitMemory.ShownToParticipants) | (1 << participantIdx)));
			}

			this.owner.World.Participants[participantIdx1].Gold -= price;

			if (participantIdx == humanIdx)
			{
				this.owner.MapOps.RepaintMapTile(troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				this.owner.TurnHooks.RenderStatusBar();

				this.owner.TextPainter.AwaitTime(30);
			}
		}

		/// <summary>
		/// The nuclear explosion on the tile: 28 pictures cut from the explosion picture (0x5318) into the stack frame, the
		/// sound, then 35 steps of the animation (the last 7 repeat the pictures 21 - 27), each put on the screen for a while and
		/// taken away again from the back screen.
		/// </summary>
		public void RenderExplosion(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderExplosion({horizLoc}, {vertLoc})");

			if (this.owner.Startup.QueryForDisk(1, 0) != 0)
			{
				// The pictures of the frame of the original (B3: in C#)
				ushort[] pictures = new ushort[0x1c];

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x5318, 1);

				for (int pictureIdx = 0; pictureIdx < 0x1c; pictureIdx++)
				{
					pictures[pictureIdx] = this.owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIdx % 7) * 0x2d) + 1), (ushort)(((pictureIdx / 7) * 0x2d) + 1), 0x2c, 0x2c);
				}

				horizLoc = (short)((this.owner.Routing.TunePosition(horizLoc - (short)MapViewHoriz) << 4) + 0x42);
				vertLoc = (short)(((vertLoc - MapViewVert) << 4) - 6);

				horizLoc = (short)this.owner.Helpers.VerifyValueSpan(horizLoc, 80, 276);
				vertLoc = (short)this.owner.Helpers.VerifyValueSpan(vertLoc, 8, 0x9c);

				this.owner.Services.PerformSound(0x2a, 0);

				this.owner.VideoCard.RenderImage(ScreenBox, horizLoc, vertLoc, 0x2c, 0x2c,
					BackScreenBox, horizLoc, vertLoc);

				for (int stepIdx = 0; stepIdx < 0x23; stepIdx++)
				{
					int pictureIdx = stepIdx < 0x1c ? stepIdx : stepIdx - 7;

					this.owner.Services.RenderPictureToScreenAlternate(ScreenBox, horizLoc, vertLoc,
						pictures[pictureIdx]);

					this.owner.TextPainter.AwaitTime(5);

					this.owner.VideoCard.RenderImage(BackScreenBox, horizLoc, vertLoc, 0x2c, 0x2c,
						ScreenBox, horizLoc, vertLoc);
				}

				this.owner.Helpers.ReleaseResource(pictures[0], 0);

				this.owner.Startup.QueryForDisk(0, 1);

				this.owner.TurnHooks.DisplayCivilizationScenery();
			}

			// Far return
			this.processor.Journal.CloseScope("RenderExplosion");
		}

		/// <summary>
		/// In fact the revolt of a city: it goes over to the player (for -1 to the owner of the city of another player with the
		/// highest (food surplus * 32) / distance, scanning the cities from 0 until the first city that does not qualify; when
		/// none, nothing happens). The new owner sees around the city; the human player hears of it when it is involved or knows
		/// one of them (the map shows the city when it sees it). The old owner's units in and next to the city (not in another
		/// city) change sides, then the city is seized (without the animations with one disk, 0x28bc set).
		/// </summary>
		public void ComputeInciteCost(short townIdx, short participantIdx)
		{
			this.processor.Journal.OpenScope($"ComputeInciteCost({townIdx}, {participantIdx})");


			City town = this.owner.World.Towns[townIdx];
			short ownerIdx = town.ParticipantIdx;
			short horizLoc = (short)town.Place.X;
			short vertLoc = (short)town.Place.Y;
			short bestSurplus = 0;
			ushort bestIdx = 0xffff;

			if (participantIdx == -1)
			{
				ushort otherIdx;

				for (otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					City other = this.owner.World.Towns[otherIdx];

					if (otherIdx == (ushort)townIdx || other.ConditionMarker == 0xff || other.ParticipantIdx == ownerIdx)
						break;

					int distance = this.owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y);

					this.owner.CityOps.HandleCity((short)otherIdx, -1);

					short surplus = CPU.SignedQuotient((short)((CityHappy - CityUnhappy) << 5), (short)distance);

					if (surplus > bestSurplus)
					{
						bestSurplus = surplus;
						bestIdx = otherIdx;
					}
				}

				if (bestSurplus != 0)
				{
					participantIdx = this.owner.World.Towns[bestIdx].ParticipantIdx;
				}
			}

			if (participantIdx != -1)
			{
				Revolt(townIdx, participantIdx, ownerIdx, horizLoc, vertLoc);
			}

			// Far return
			this.processor.Journal.CloseScope("ComputeInciteCost");
		}

		/// <summary>The city goes over to the player (see ComputeInciteCost)</summary>
		private void Revolt(short townIdx, short participantIdx, short ownerIdx, short horizLoc, short vertLoc)
		{
			City town = this.owner.World.Towns[townIdx];
			sbyte sight = (sbyte)(byte)(1 << (byte)participantIdx);

			// ES is left at the segment 0x3772, like in the original
			this.processor.SegExtra.Value16 = 0x3772;

			this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] |= sight;

			for (int deltaIdx = 0; deltaIdx < 0x14; deltaIdx++)
			{
				this.owner.World.ChartSight[(ushort)(short)(this.owner.TownDeltas[deltaIdx].X + horizLoc),
					(ushort)(short)(this.owner.TownDeltas[deltaIdx].Y + vertLoc)] |= sight;
			}

			short humanIdx = this.owner.World.PersonParticipantIdx;
			ushort[] humanRelations = this.owner.World.Participants[humanIdx].Relations;

			if (ownerIdx == humanIdx || participantIdx == humanIdx || (humanRelations[ownerIdx] & 0x40) != 0 || (humanRelations[participantIdx] & 0x40) != 0)
			{
				if ((this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)humanIdx)) != 0)
				{
					this.owner.MapOps.FocusMapView(humanIdx, town.Place.X - 8, town.Place.Y - 5);
				}

				Message(CivilizationPlural(ownerIdx))
					.Text(participantIdx == 0 ? (ushort)0x5322 : (ushort)0x533c)
					.City(townIdx);

				if (participantIdx == 0)
				{
					this.owner.Words.Append(0x536c);
				}
				else
				{
					this.owner.Words.Append(".\n");

					this.owner.Words.Append(CivilizationName(participantIdx));

					this.owner.Words.Append(0x5355);
				}

				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 32);
			}

			UnitMemory troops = new UnitMemory(this.processor);

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				int distance = this.owner.Helpers.ObtainDistance(horizLoc, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					vertLoc, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				if (troops.Pull(ownerIdx, troopIdx, UnitMemory.Kind) == 0xff || (short)distance > 1)
					continue;

				if (distance != 0)
				{
					// Not the units in another city
					if ((this.owner.MapOps.ObtainTileImprovements(troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
						troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert)) & 1) != 0)
						continue;
				}

				sbyte kind = troops.TroopKind(ownerIdx, troopIdx);

				this.owner.UnitOps.EliminateUnit(ownerIdx, troopIdx);

				this.owner.MapOps.MapAssignCityOwner(participantIdx, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				this.owner.MapOps.TagTileOwned(troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				ushort newIdx = this.owner.UnitOps.ConstructUnit(participantIdx, kind, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				if (newIdx == 0xffff)
					continue;

				troops.Assign(participantIdx, newIdx, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIdx, newIdx, UnitMemory.ShownToParticipants) | (byte)(1 << (byte)ownerIdx)));
				troops.Assign(participantIdx, newIdx, UnitMemory.BaseTown, (byte)townIdx);
			}

			short preferences = this.owner.World.SessionPrefMarkers;

			NoConquestAnimation = 0x1;

			if ((short)FirstMissingDisk <= 1)
			{
				this.owner.World.SessionPrefMarkers &= 0x7ff7;
			}

			this.owner.Conquest.SeizeCity(participantIdx, townIdx, 1);

			NoConquestAnimation = 0x0;
			this.owner.World.SessionPrefMarkers = preferences;

			this.owner.MapOps.MapAssignCityOwner(participantIdx, town.Place.X, town.Place.Y);
		}

		/// <summary>
		/// A diplomat establishes an embassy... in fact investigates the city: the title "... founded: year." over a picture with
		/// four walking figures (cut from 0x536e) moving to the right until a key or a click (frame by
		/// frame, 10 timer ticks each), then the city screen (unless a key is still waiting).
		/// </summary>
		public void ExamineCity(short townIdx)
		{
			this.processor.Journal.OpenScope($"ExamineCity({townIdx})");

			if (this.owner.Startup.QueryForDisk(1, 0) != 0)
			{
				// The pictures of the frame of the original (B3: in C#)
				ushort[] figures = new ushort[4];

				this.owner.Helpers.HaltHueCycling();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x536e, 0);

				for (int figureIdx = 0; figureIdx < 4; figureIdx++)
				{
					figures[figureIdx] = this.owner.VideoCard.ScreenToPicture(1, 1, (ushort)((figureIdx << 4) + 1), 0x2f, 0xf);
				}

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x537b, 0);

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x5384, 1);

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				EmptyMessage()
					.City(townIdx)
					.Text(" founded: ");

				this.owner.TurnHooks.ObtainCurrentYearAsText();

				this.owner.Words.Append(".");

				DrawingBox.TypefaceIdx = 0x6;

				this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 4, 0);

				DrawingBox.TypefaceIdx = 0x1;

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x78, 0x140, 0x28, BackScreenBox, 0, 0);

				this.owner.TurnLogic.AwaitForKey();

				short horizLoc = 0x50;

				for (; horizLoc < 0x78; horizLoc++)
				{
					this.owner.Services.RestoreTimer();

					this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0x28, BackScreenBox, 0, 0x78);

					for (short figureIdx = 0; figureIdx < 4; figureIdx++)
					{
						this.owner.Services.RenderPictureToScreen(BackScreenBox, (13 * figureIdx) + horizLoc,
							((10 * figureIdx) & 0xf) + 120, figures[(figureIdx + horizLoc) & 3]);
					}

					this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x78, 0x140, 0x28, ScreenBox, 0, 0x78);

					while ((short)this.owner.Timer.Beats < 0xa)
					{
					}

					this.owner.Startup.ObtainMouseState();

					if ((ushort)this.owner.Libc.KeyAvailable() != 0 || this.owner.GlobDb3a != 0)
						break;
				}

				this.owner.Helpers.ReleaseResource(figures[0], 0x539a);

				this.owner.Startup.QueryForDisk(0, 1);

				if (VgaMode != 0)
				{
					this.owner.Services.FloodColors(10, 0, 0, 0);
				}

				this.owner.Words.Clear();

				if ((ushort)this.owner.Libc.KeyAvailable() == 0)
				{
					this.owner.CityScene.DisplayCityScreen(townIdx, -3);
				}
				else
				{
					this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

					this.owner.TurnHooks.DisplayCivilizationScenery();
				}

				this.owner.Startup.DisplayMouseCursor();

				this.owner.TurnLogic.AwaitForKey();
			}

			// Far return
			this.processor.Journal.CloseScope("ExamineCity");
		}
	}
}
