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
			this.Processor.Journal.OpenScope($"DiplomatArrivesAtCity({townIdx}, {participantIdx}, {troopIdx})");


			short person = this.Owner.World.PersonParticipantIdx;
			City town = this.Owner.World.Towns[townIdx];
			short ownerIdx = town.ParticipantIdx;
			ushort troop = (ushort)((ushort)(0xc * troopIdx) + (ushort)(0x600 * participantIdx));

			this.Troops.AssignAt(troop, UnitMemory.TravelHoriz, 0xff);

			Message(CivilizationName((ushort)participantIdx))
				.Text(" diplomat arrives\nin ")
				.City(townIdx)
				.Text("...\n Establish Embassy\n Investigate City\n Steal Technology\n Industrial Sabotage\n Incite a Revolt\n Meet with King\n");

			ushort choice;

			if (participantIdx == person)
			{
				if ((this.Owner.World.Participants[person].Relations[ownerIdx] & 0x40) != 0 || ownerIdx == 0)
					DisabledLines = 0x1;

				if ((town.ConditionMarker & 0x20) != 0)
					DisabledLinesLow = (byte)((byte)DisabledLines | 4);

				if (town.Has(CityStructure.Palace))
					DisabledLinesLow = (byte)((byte)DisabledLines | 0x10);

				if (ownerIdx == 0)
					DisabledLinesLow = (byte)((byte)DisabledLines | 0x20);

				choice = this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);
			}
			else
			{
				choice = 2;

				if (ownerIdx == person)
				{
					if ((this.Owner.World.Participants[person].Relations[participantIdx] & 2) != 0)
					{
						this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
						FinishDiplomatArrival();
						return;
					}
				}
			}

			switch (choice)
			{
				case 0:
					this.Owner.World.Participants[person].Relations[ownerIdx] |= 0x40;

					this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

					this.Owner.Briefings.DisplayIntelligenceReport(ownerIdx);
					break;

				case 1:
					this.Owner.Advisors.RenewCityScreen(townIdx);

					this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
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
						this.Owner.Audience.LaunchAudience(ownerIdx, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horiz),
							(sbyte)this.Troops.PullAt(troop, UnitMemory.Vert), 1);
					}

					this.Troops.AssignAt(troop, UnitMemory.LeftoverSteps, 0);
					break;
			}

			FinishDiplomatArrival();
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
			this.Processor.Journal.OpenScope($"BuyUnit({participantIdx}, {troopIdx}, {participantIdx1})");


			short humanIdx = this.Owner.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(this.Processor);

			if ((short)this.Owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 2) <= 1)
			{
				short distance = (short)this.Owner.Politics.LocateNearestCityOfPlayer(participantIdx,
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
				sbyte kind = troops.TroopKind(participantIdx, troopIdx);
				short price = (short)(CPU.SignedQuotient((short)(this.Owner.World.Participants[participantIdx].Gold + 0x2ee), (short)(distance + 2)) *
					UnitKindWord(kind, UnitKindTable.Price));

				if (kind != 0)
				{
					price = CPU.SignedQuotient(price, 2);
				}

				if (participantIdx1 == humanIdx || this.Owner.World.Participants[participantIdx1].Gold / 2 >= price)
				{
					OfferBribe(participantIdx, troopIdx, participantIdx1, price);
				}
			}


			this.Processor.Journal.CloseScope("BuyUnit");
		}

		/// <summary>
		/// The nuclear explosion on the tile: 28 pictures cut from the explosion picture (0x5318) into the stack frame, the
		/// sound, then 35 steps of the animation (the last 7 repeat the pictures 21 - 27), each put on the screen for a while and
		/// taken away again from the back screen.
		/// </summary>
		public void RenderExplosion(short horizLoc, short vertLoc)
		{
			this.Processor.Journal.OpenScope($"RenderExplosion({horizLoc}, {vertLoc})");

			if (this.Owner.Startup.QueryForDisk(1, 0) != 0)
			{
				// The pictures of the frame of the original (B3: in C#)
				ushort[] pictures = new ushort[0x1c];

				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileNuke1Pic, 1);

				for (int pictureIdx = 0; pictureIdx < 0x1c; pictureIdx++)
				{
					pictures[pictureIdx] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIdx % 7) * 0x2d) + 1), (ushort)(((pictureIdx / 7) * 0x2d) + 1), 0x2c, 0x2c);
				}

				horizLoc = (short)((this.Owner.Routing.TunePosition(horizLoc - (short)MapViewHoriz) << 4) + 0x42);
				vertLoc = (short)(((vertLoc - MapViewVert) << 4) - 6);

				horizLoc = (short)this.Owner.Helpers.VerifyValueSpan(horizLoc, 80, 276);
				vertLoc = (short)this.Owner.Helpers.VerifyValueSpan(vertLoc, 8, 0x9c);

				this.Owner.Services.PerformSound(0x2a, 0);

				this.Owner.VideoCard.RenderImage(ScreenBox, horizLoc, vertLoc, 0x2c, 0x2c,
					BackScreenBox, horizLoc, vertLoc);

				for (int stepIdx = 0; stepIdx < 0x23; stepIdx++)
				{
					int pictureIdx = stepIdx < 0x1c ? stepIdx : stepIdx - 7;

					this.Owner.Services.RenderPictureToScreenAlternate(ScreenBox, horizLoc, vertLoc,
						pictures[pictureIdx]);

					this.Owner.TextPainter.AwaitTime(5);

					this.Owner.VideoCard.RenderImage(BackScreenBox, horizLoc, vertLoc, 0x2c, 0x2c,
						ScreenBox, horizLoc, vertLoc);
				}

				this.Owner.Helpers.ReleaseResource(pictures[0], 0);

				this.Owner.Startup.QueryForDisk(0, 1);

				this.Owner.TurnHooks.DisplayCivilizationScenery();
			}

			this.Processor.Journal.CloseScope("RenderExplosion");
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
			this.Processor.Journal.OpenScope($"ComputeInciteCost({townIdx}, {participantIdx})");


			City town = this.Owner.World.Towns[townIdx];
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
					City other = this.Owner.World.Towns[otherIdx];

					if (otherIdx == (ushort)townIdx || other.ConditionMarker == 0xff || other.ParticipantIdx == ownerIdx)
						break;

					int distance = this.Owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y);

					this.Owner.CityOps.HandleCity((short)otherIdx, -1);

					short surplus = CPU.SignedQuotient((short)((CityHappy - CityUnhappy) << 5), (short)distance);

					if (surplus > bestSurplus)
					{
						bestSurplus = surplus;
						bestIdx = otherIdx;
					}
				}

				if (bestSurplus != 0)
				{
					participantIdx = this.Owner.World.Towns[bestIdx].ParticipantIdx;
				}
			}

			if (participantIdx != -1)
			{
				Revolt(townIdx, participantIdx, ownerIdx, horizLoc, vertLoc);
			}

			this.Processor.Journal.CloseScope("ComputeInciteCost");
		}

		/// <summary>
		/// A diplomat establishes an embassy... in fact investigates the city: the title "... founded: year." over a picture with
		/// four walking figures (cut from 0x536e) moving to the right until a key or a click (frame by
		/// frame, 10 timer ticks each), then the city screen (unless a key is still waiting).
		/// </summary>
		public void ExamineCity(short townIdx)
		{
			this.Processor.Journal.OpenScope($"ExamineCity({townIdx})");

			if (this.Owner.Startup.QueryForDisk(1, 0) != 0)
			{
				// The pictures of the frame of the original (B3: in C#)
				ushort[] figures = new ushort[4];

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.Startup.ConcealMouseCursor();

				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSettlersPic, 0);

				for (int figureIdx = 0; figureIdx < 4; figureIdx++)
				{
					figures[figureIdx] = this.Owner.VideoCard.ScreenToPicture(1, 1, (ushort)((figureIdx << 4) + 1), 0x2f, 0xf);
				}

				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileHillPicC, 0);

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileHillPicD, 1);

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				EmptyMessage()
					.City(townIdx)
					.Text(" founded: ");

				this.Owner.TurnHooks.ObtainCurrentYearAsText();

				this.Owner.Words.Append(".");

				DrawingBox.TypefaceIdx = 0x6;

				this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 4, Hues.Black);

				DrawingBox.TypefaceIdx = 0x1;

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x78, 0x140, 0x28, BackScreenBox, 0, 0);

				this.Owner.TurnLogic.AwaitForKey();

				short horizLoc = 0x50;

				for (; horizLoc < 0x78; horizLoc++)
				{
					this.Owner.Services.RestoreTimer();

					this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0x28, BackScreenBox, 0, 0x78);

					for (short figureIdx = 0; figureIdx < 4; figureIdx++)
					{
						this.Owner.Services.RenderPictureToScreen(BackScreenBox, (13 * figureIdx) + horizLoc,
							((10 * figureIdx) & 0xf) + 120, figures[(figureIdx + horizLoc) & 3]);
					}

					this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x78, 0x140, 0x28, ScreenBox, 0, 0x78);

					while ((short)this.Owner.Timer.Beats < 0xa)
					{
					}

					this.Owner.Startup.ObtainMouseState();

					if ((ushort)this.Owner.Libc.KeyAvailable() != 0 || this.Owner.GlobDb3a != 0)
						break;
				}

				this.Owner.Helpers.ReleaseResource(figures[0], 0x539a);

				this.Owner.Startup.QueryForDisk(0, 1);

				if (VgaMode != 0)
				{
					this.Owner.Services.FloodColors(10, 0, 0, 0);
				}

				this.Owner.Words.Clear();

				if ((ushort)this.Owner.Libc.KeyAvailable() == 0)
				{
					this.Owner.CityScene.DisplayCityScreen(townIdx, -3);
				}
				else
				{
					this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

					this.Owner.TurnHooks.DisplayCivilizationScenery();
				}

				this.Owner.Startup.DisplayMouseCursor();

				this.Owner.TurnLogic.AwaitForKey();
			}

			this.Processor.Journal.CloseScope("ExamineCity");
		}

		private void FinishDiplomatArrival()
		{
			this.Processor.Journal.CloseScope("DiplomatArrivesAtCity");
		}

		/// <summary>
		/// The diplomat steals a technology the city's owner knows and the player doesn't (from a random one of the 72), once
		/// a city (0x20); the human player breaks his treaty first (unless he declines) and hears when there is none; the AI
		/// sabotages instead
		/// </summary>
		private void StealTechnology(short townIdx, short participantIdx, short troopIdx, short ownerIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			City town = this.Owner.World.Towns[townIdx];

			if (participantIdx == person)
			{
				if ((town.ConditionMarker & 0x20) != 0)
					return;

				if (this.Owner.Combat.AffirmBreakTreaty(town.ParticipantIdx) == 0xffff)
					return;

				this.Owner.Politics.WipeDiplomacyFlags(person, town.ParticipantIdx, 2);
			}
			else if ((town.ConditionMarker & 0x20) != 0)
			{
				SabotageCity(townIdx, participantIdx, troopIdx);
				return;
			}

			this.Owner.Words.Clear();

			ushort first = (ushort)this.Owner.Libc.Dice.DrawInt(72);

			for (ushort attempt = 0; attempt < 0x48; attempt++)
			{
				short scienceIdx = (short)((short)(first + attempt) % 0x48);

				if (participantIdx == 0 ||
					this.Owner.Advisors.PlayerKnowsTechnology(ownerIdx, scienceIdx) == 0 ||
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					continue;

				this.Owner.Advisors.EarnTechnology(participantIdx, scienceIdx, ownerIdx);

				Message(CivilizationPlural((ushort)participantIdx))
					.Text(" steal\n")
					.Text(TechnologyText(scienceIdx))
					.Text(".\n")
					.Style(0x0)
					.Dialog(80, 80);

				town.ConditionMarker |= 0x20;
				break;
			}

			if (this.Owner.Words[0] != 0)
			{
				this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
			}
			else if (participantIdx == person)
			{
				this.Owner.TurnHooks.DisplayDialog(GameTexts.TextNoNewTechnologyFound, 80, 80);
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
			City town = this.Owner.World.Towns[townIdx];
			ushort tries = 0;
			ushort buildingIdx = 0;
			bool destroyed = false;

			if (this.Owner.Libc.Dice.DrawInt(2) != 0 || town.MaterialsTally == 0)
			{
				do
				{
					buildingIdx = (ushort)this.Owner.Libc.Dice.DrawInt(24);
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
				town.MaterialsTally = 0;

				Message(town.PresentMakingIdx >= 0 ? UnitText(town.PresentMakingIdx) : BuildingRecordText(-town.PresentMakingIdx))
					.Text(" production sabotaged\n");
			}

			this.Owner.Words.Append("in ");

			this.Owner.Conquest.ObtainCityLabel(townIdx);

			this.Owner.Words.Append(".\n");

			DialogStyle = 0x0;

			this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);

			this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
		}

		/// <summary>
		/// The dissidents revolt for (owner's gold + 1000) / (distance to the owner's capital + 3) * size, halved in disorder;
		/// the player may pay it, or twice it to subvert a city of a player at peace; inciting breaks the peace
		/// </summary>
		private void InciteRevolt(short townIdx, short participantIdx, short troopIdx, short ownerIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			ushort distance = this.Owner.Politics.LocateNearestCityOfPlayer(ownerIdx, town.Place.X, town.Place.Y);

			if (distance == 0)
				return;

			short cost = (short)(CPU.SignedQuotient((short)(ushort)((ushort)this.Owner.World.Participants[ownerIdx].Gold + 0x3e8), (short)(distance + 3)) * town.TrueExtent);

			if ((town.ConditionMarker & 1) != 0)
				cost = CPU.SignedQuotient(cost, 2);

			Message("Dissidents in ")
				.City(townIdx)
				.Text("\nwill revolt for $")
				.Number(cost)
				.Text(".\n");

			if (cost <= this.Owner.World.Participants[participantIdx].Gold)
			{
				this.Owner.Words.Append(" Forget it.\n Incite revolt\n");

				if ((short)(cost << 1) <= this.Owner.World.Participants[participantIdx].Gold)
				{
					if ((this.Owner.World.Participants[participantIdx].Relations[ownerIdx] & 2) != 0)
					{
						this.Owner.Words.Append(" Subvert city ($x2)\n");
					}
				}
			}

			DialogStyle = 0x0;

			short choice = (short)this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

			if (choice < 1)
				return;

			if (choice == 1)
			{
				if ((this.Owner.World.Participants[ownerIdx].Relations[participantIdx] & 2) != 0)
				{
					this.Owner.Politics.WipeDiplomacyFlags(ownerIdx, participantIdx, 2);

					this.Owner.World.Participants[ownerIdx].Relations[participantIdx] |= 8;
				}
			}

			this.Owner.World.Participants[participantIdx].Gold -= (short)(cost * choice);

			ComputeInciteCost(townIdx, participantIdx);

			this.Owner.MapOps.RepaintMapTile(town.Place.X, town.Place.Y);

			this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			this.Owner.TurnHooks.RenderStatusBar();
		}

		/// <summary>The offer of BuyUnit and, when it is taken, the unit changing sides</summary>
		private void OfferBribe(short participantIdx, short troopIdx, short participantIdx1, short price)
		{
			short humanIdx = this.Owner.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(this.Processor);
			ushort kindName = (ushort)((troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride) + UnitKindTable.Origin);

			Message(CivilizationName(participantIdx))
				.Text(" ")
				.Text(kindName)
				.Text("\nwill desert for ")
				.Number(price)
				.Text("$.\nTreasury: ")
				.Number(this.Owner.World.Participants[participantIdx1].Gold)
				.Text("$\n");

			if (price <= this.Owner.World.Participants[participantIdx1].Gold)
			{
				this.Owner.Words.Append(" Forget it.\n Pay\n");
			}

			if (participantIdx1 == humanIdx)
			{
				if (this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80) != 1)
					return;
			}

			if (participantIdx == humanIdx)
			{
				this.Owner.MapOps.FocusMapView(participantIdx, troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 8,
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

			this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			this.Owner.World.Participants[participantIdx].LiveTroops[kind]--;

			// The record keeps its position after EliminateUnit
			ushort bribedIdx = this.Owner.UnitOps.ConstructUnit(participantIdx1, kind,
				troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if (bribedIdx != 0xffff)
			{
				this.Owner.World.Participants[participantIdx1].LiveTroops[kind]++;

				troops.Assign(participantIdx1, bribedIdx, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIdx1, bribedIdx, UnitMemory.ShownToParticipants) | (1 << participantIdx)));
			}

			this.Owner.World.Participants[participantIdx1].Gold -= price;

			if (participantIdx == humanIdx)
			{
				this.Owner.MapOps.RepaintMapTile(troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				this.Owner.TurnHooks.RenderStatusBar();

				this.Owner.TextPainter.AwaitTime(30);
			}
		}

		/// <summary>The city goes over to the player (see ComputeInciteCost)</summary>
		private void Revolt(short townIdx, short participantIdx, short ownerIdx, short horizLoc, short vertLoc)
		{
			City town = this.Owner.World.Towns[townIdx];
			sbyte sight = (sbyte)(byte)(1 << (byte)participantIdx);

			// ES is left at the segment 0x3772, like in the original
			this.Processor.SegExtra.Value16 = 0x3772;

			this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] |= sight;

			for (int deltaIdx = 0; deltaIdx < 0x14; deltaIdx++)
			{
				this.Owner.World.ChartSight[(ushort)(short)(this.Owner.TownDeltas[deltaIdx].X + horizLoc),
					(ushort)(short)(this.Owner.TownDeltas[deltaIdx].Y + vertLoc)] |= sight;
			}

			short humanIdx = this.Owner.World.PersonParticipantIdx;
			ushort[] humanRelations = this.Owner.World.Participants[humanIdx].Relations;

			if (ownerIdx == humanIdx || participantIdx == humanIdx || (humanRelations[ownerIdx] & 0x40) != 0 || (humanRelations[participantIdx] & 0x40) != 0)
			{
				if ((this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)humanIdx)) != 0)
				{
					this.Owner.MapOps.FocusMapView(humanIdx, town.Place.X - 8, town.Place.Y - 5);
				}

				Message(CivilizationPlural(ownerIdx))
					.Text(participantIdx == 0 ? (ushort)0x5322 : (ushort)0x533c)
					.City(townIdx);

				if (participantIdx == 0)
				{
					this.Owner.Words.Append(0x536c);
				}
				else
				{
					this.Owner.Words.Append(".\n");

					this.Owner.Words.Append(CivilizationName(participantIdx));

					this.Owner.Words.Append(0x5355);
				}

				this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 32);
			}

			UnitMemory troops = new UnitMemory(this.Processor);

			for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
			{
				int distance = this.Owner.Helpers.ObtainDistance(horizLoc, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					vertLoc, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				if (troops.Pull(ownerIdx, troopIdx, UnitMemory.Kind) == 0xff || (short)distance > 1)
					continue;

				if (distance != 0)
				{
					// Not the units in another city
					if ((this.Owner.MapOps.ObtainTileImprovements(troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
						troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert)) & 1) != 0)
						continue;
				}

				sbyte kind = troops.TroopKind(ownerIdx, troopIdx);

				this.Owner.UnitOps.EliminateUnit(ownerIdx, troopIdx);

				this.Owner.MapOps.MapAssignCityOwner(participantIdx, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				this.Owner.MapOps.TagTileOwned(troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz), troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				ushort newIdx = this.Owner.UnitOps.ConstructUnit(participantIdx, kind, troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Horiz),
					troops.PullSigned(ownerIdx, troopIdx, UnitMemory.Vert));

				if (newIdx == 0xffff)
					continue;

				troops.Assign(participantIdx, newIdx, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIdx, newIdx, UnitMemory.ShownToParticipants) | (byte)(1 << (byte)ownerIdx)));
				troops.Assign(participantIdx, newIdx, UnitMemory.BaseTown, (byte)townIdx);
			}

			short preferences = this.Owner.World.SessionPrefMarkers;

			NoConquestAnimation = 0x1;

			if ((short)FirstMissingDisk <= 1)
			{
				this.Owner.World.SessionPrefMarkers &= 0x7ff7;
			}

			this.Owner.Conquest.SeizeCity(participantIdx, townIdx, 1);

			NoConquestAnimation = 0x0;
			this.Owner.World.SessionPrefMarkers = preferences;

			this.Owner.MapOps.MapAssignCityOwner(participantIdx, town.Place.X, town.Place.Y);
		}
	}
}
