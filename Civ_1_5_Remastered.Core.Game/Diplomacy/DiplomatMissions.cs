namespace Civ_1_5_Remastered.Core.Game.Diplomacy
{
	public class DiplomatMissions : GameComponent, IDiplomatMissions
	{
		public DiplomatMissions(IGameEngine owner)
			: base(owner)
		{
		}

		public void DiplomatArrivesAtCity(short townIndex, short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DiplomatArrivesAtCity({townIndex}, {participantIndex}, {troopIndex})");

			short person = this.Owner.World.PersonParticipantIndex;
			City town = this.Owner.World.Towns[townIndex];
			short ownerIndex = town.ParticipantIndex;
			ushort troop = (ushort)((ushort)(0xc * troopIndex) + (ushort)(0x600 * participantIndex));

			this.Troops.AssignAt(troop, UnitMemory.TravelHorizontal, 0xff);

			Message(Tables.CivilizationName((ushort)participantIndex))
				.Text(" diplomat arrives\nin ")
				.City(townIndex)
				.Text("...\n Establish Embassy\n Investigate City\n Steal Technology\n Industrial Sabotage\n Incite a Revolt\n Meet with King\n");

			ushort choice;

			if (participantIndex == person)
			{
				if ((this.Owner.World.Participants[person].Relations[ownerIndex] & 0x40) != 0 || ownerIndex == 0)
				{
					Menu.DisabledLines = 0x1;
				}

				if ((town.ConditionMarker & 0x20) != 0)
				{
					Menu.DisabledLinesLow = (byte)((byte)Menu.DisabledLines | 4);
				}

				if (town.Has(CityStructure.Palace))
				{
					Menu.DisabledLinesLow = (byte)((byte)Menu.DisabledLines | 0x10);
				}

				if (ownerIndex == 0)
				{
					Menu.DisabledLinesLow = (byte)((byte)Menu.DisabledLines | 0x20);
				}

				choice = this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);
			}
			else
			{
				choice = 2;

				if (ownerIndex == person)
				{
					if ((this.Owner.World.Participants[person].Relations[participantIndex] & 2) != 0)
					{
						this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
						return;
					}
				}
			}

			switch (choice)
			{
				case 0:
					this.Owner.World.Participants[person].Relations[ownerIndex] |= 0x40;

					this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);

					this.Owner.Briefings.ShowIntelligenceReport(ownerIndex);
					break;

				case 1:
					this.Owner.Advisors.RenewCityScreen(townIndex);

					this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
					break;

				case 2:
					StealTechnology(townIndex, participantIndex, troopIndex, ownerIndex);
					break;

				case 3:
					SabotageCity(townIndex, participantIndex, troopIndex);
					break;

				case 4:
					InciteRevolt(townIndex, participantIndex, troopIndex, ownerIndex);
					break;

				case 5:
					if (ownerIndex != 0)
					{
						this.Owner.Audience.LaunchAudience(ownerIndex, (sbyte)this.Troops.PullAt(troop, UnitMemory.Horizontal),
							(sbyte)this.Troops.PullAt(troop, UnitMemory.Vertical), 1);
					}

					this.Troops.AssignAt(troop, UnitMemory.LeftoverSteps, 0);
					break;
			}
		}

		public void BuyUnit(short participantIndex, short troopIndex, short firstParticipantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"BuyUnit({participantIndex}, {troopIndex}, {firstParticipantIndex})");

			short humanIndex = this.Owner.World.PersonParticipantIndex;
			UnitMemory troops = new UnitMemory(this.Processor);

			if ((short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, troopIndex, 2) <= 1)
			{
				short distance = (short)this.Owner.Politics.FindNearestCityOfPlayer(participantIndex,
					troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal), troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));
				sbyte kind = troops.TroopKind(participantIndex, troopIndex);
				short price = (short)(CentralProcessor.SignedQuotient((short)(this.Owner.World.Participants[participantIndex].Gold + 0x2ee), (short)(distance + 2)) *
					Tables.UnitKindWord(kind, UnitKindTable.Price));

				if (kind != 0)
				{
					price = CentralProcessor.SignedQuotient(price, 2);
				}

				if (firstParticipantIndex == humanIndex || this.Owner.World.Participants[firstParticipantIndex].Gold / 2 >= price)
				{
					OfferBribe(participantIndex, troopIndex, firstParticipantIndex, price);
				}
			}
		}

		public void DrawExplosion(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"RenderExplosion({horizontalLocation}, {verticalLocation})");

			if (this.Owner.Startup.AskForDisk(1, 0) != 0)
			{
				ushort[] pictures = new ushort[0x1c];

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileNukeFirstPicture, 1);

				for (int pictureIndex = 0; pictureIndex < 0x1c; pictureIndex++)
				{
					pictures[pictureIndex] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIndex % 7) * 0x2d) + 1), (ushort)(((pictureIndex / 7) * 0x2d) + 1), 0x2c, 0x2c);
				}

				horizontalLocation = (short)((this.Owner.Routing.TunePosition(horizontalLocation - (short)MapView.Horizontal) << 4) + 0x42);
				verticalLocation = (short)(((verticalLocation - MapView.Vertical) << 4) - 6);

				horizontalLocation = (short)this.Owner.Helpers.ClampValue(horizontalLocation, 80, 276);
				verticalLocation = (short)this.Owner.Helpers.ClampValue(verticalLocation, 8, 0x9c);

				this.Owner.Services.PerformSound(0x2a, 0);

				this.Owner.VideoCard.RenderImage(Screen.Box, horizontalLocation, verticalLocation, 0x2c, 0x2c,
					Screen.BackScreenBox, horizontalLocation, verticalLocation);

				for (int stepIndex = 0; stepIndex < 0x23; stepIndex++)
				{
					int pictureIndex = stepIndex < 0x1c ? stepIndex : stepIndex - 7;

					this.Owner.Services.DrawPictureToScreenAlternate(Screen.Box, horizontalLocation, verticalLocation,
						pictures[pictureIndex]);

					this.Owner.TextPainter.WaitForTime(5);

					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, horizontalLocation, verticalLocation, 0x2c, 0x2c,
						Screen.Box, horizontalLocation, verticalLocation);
				}

				this.Owner.Helpers.ReleaseResource(pictures[0], 0);

				this.Owner.Startup.AskForDisk(0, 1);

				this.Owner.TurnHooks.ShowCivilizationScenery();
			}
		}

		public void ComputeInciteCost(short townIndex, short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ComputeInciteCost({townIndex}, {participantIndex})");

			City town = this.Owner.World.Towns[townIndex];
			short ownerIndex = town.ParticipantIndex;
			short horizontalLocation = (short)town.Place.X;
			short verticalLocation = (short)town.Place.Y;
			short bestSurplus = 0;
			ushort bestIndex = 0xffff;

			if (participantIndex == -1)
			{
				ushort otherIndex;

				for (otherIndex = 0; otherIndex < 0x80; otherIndex++)
				{
					City other = this.Owner.World.Towns[otherIndex];

					if (otherIndex == (ushort)townIndex || other.ConditionMarker == 0xff || other.ParticipantIndex == ownerIndex)
					{
						break;
					}

					int distance = this.Owner.Helpers.ObtainDistance(town.Place.X, other.Place.X, town.Place.Y, other.Place.Y);

					this.Owner.CityOperations.HandleCity((short)otherIndex, -1);

					short surplus = CentralProcessor.SignedQuotient((short)((CityTurn.Happy - CityTurn.Unhappy) << 5), (short)distance);

					if (surplus > bestSurplus)
					{
						bestSurplus = surplus;
						bestIndex = otherIndex;
					}
				}

				if (bestSurplus != 0)
				{
					participantIndex = this.Owner.World.Towns[bestIndex].ParticipantIndex;
				}
			}

			if (participantIndex != -1)
			{
				Revolt(townIndex, participantIndex, ownerIndex, horizontalLocation, verticalLocation);
			}
		}

		public void ExamineCity(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ExamineCity({townIndex})");

			if (this.Owner.Startup.AskForDisk(1, 0) != 0)
			{
				ushort[] figures = new ushort[4];

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSettlersPicture, 0);

				for (int figureIndex = 0; figureIndex < 4; figureIndex++)
				{
					figures[figureIndex] = this.Owner.VideoCard.ScreenToPicture(1, 1, (ushort)((figureIndex << 4) + 1), 0x2f, 0xf);
				}

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileHillPictureThird, 0);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileHillPictureFourth, 1);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				EmptyMessage()
					.City(townIndex)
					.Text(" founded: ");

				this.Owner.TurnHooks.FormatCurrentYear();

				this.Owner.Words.Append(".");

				DrawingBox.TypefaceIndex = 0x6;

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 4, Hues.Black);

				DrawingBox.TypefaceIndex = 0x1;

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x78, 0x140, 0x28, Screen.BackScreenBox, 0, 0);

				this.Owner.TurnLogic.WaitForKey();

				short horizontalLocation = 0x50;

				for (; horizontalLocation < 0x78; horizontalLocation++)
				{
					this.Owner.Services.RestoreTimer();

					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0x28, Screen.BackScreenBox, 0, 0x78);

					for (short figureIndex = 0; figureIndex < 4; figureIndex++)
					{
						this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox, (13 * figureIndex) + horizontalLocation,
							((10 * figureIndex) & 0xf) + 120, figures[(figureIndex + horizontalLocation) & 3]);
					}

					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x78, 0x140, 0x28, Screen.Box, 0, 0x78);

					while ((short)this.Owner.Timer.Beats < 0xa)
					{
					}

					this.Owner.Startup.ReadMouseState();

					if ((ushort)this.Owner.Runtime.KeyAvailable() != 0 || this.Owner.MenuPointerState != 0)
					{
						break;
					}
				}

				this.Owner.Helpers.ReleaseResource(figures[0], 0x539a);

				this.Owner.Startup.AskForDisk(0, 1);

				if (Screen.VideoGraphicsMode != 0)
				{
					this.Owner.Services.FillColors(10, 0, 0, 0);
				}

				this.Owner.Words.Clear();

				if ((ushort)this.Owner.Runtime.KeyAvailable() == 0)
				{
					this.Owner.CityScene.ShowCityScreen(townIndex, -3);
				}
				else
				{
					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

					this.Owner.TurnHooks.ShowCivilizationScenery();
				}

				this.Owner.Startup.ShowMouseCursor();

				this.Owner.TurnLogic.WaitForKey();
			}
		}

		private void StealTechnology(short townIndex, short participantIndex, short troopIndex, short ownerIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;
			City town = this.Owner.World.Towns[townIndex];

			if (participantIndex == person)
			{
				if ((town.ConditionMarker & 0x20) != 0)
				{
					return;
				}

				if (this.Owner.Combat.AffirmBreakTreaty(town.ParticipantIndex) == 0xffff)
				{
					return;
				}

				this.Owner.Politics.ClearDiplomacyFlags(person, town.ParticipantIndex, 2);
			}
			else if ((town.ConditionMarker & 0x20) != 0)
			{
				SabotageCity(townIndex, participantIndex, troopIndex);
				return;
			}

			this.Owner.Words.Clear();

			ushort first = (ushort)this.Owner.Runtime.Dice.DrawInteger(72);

			for (ushort attempt = 0; attempt < 0x48; attempt++)
			{
				short scienceIndex = (short)((short)(first + attempt) % 0x48);

				if (participantIndex == 0 ||
					this.Owner.Advisors.PlayerKnowsTechnology(ownerIndex, scienceIndex) == 0 ||
					this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
				{
					continue;
				}

				this.Owner.Advisors.EarnTechnology(participantIndex, scienceIndex, ownerIndex);

				Message(Tables.CivilizationPlural((ushort)participantIndex))
					.Text(" steal\n")
					.Text(GameTables.TechnologyText(scienceIndex))
					.Text(".\n")
					.Style(0x0)
					.Dialog(80, 80);

				town.ConditionMarker |= 0x20;
				break;
			}

			if (this.Owner.Words[0] != 0)
			{
				this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
			}
			else if (participantIndex == person)
			{
				this.Owner.TurnHooks.ShowDialog(GameTexts.TextNoNewTechnologyFound, 80, 80);
			}
			else
			{
				SabotageCity(townIndex, participantIndex, troopIndex);
			}
		}

		private void SabotageCity(short townIndex, short participantIndex, short troopIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			ushort tries = 0;
			bool destroyed = false;

			if (this.Owner.Runtime.Dice.DrawInteger(2) != 0 || town.MaterialsTally == 0)
			{
				ushort buildingIndex;
				do
				{
					buildingIndex = (ushort)this.Owner.Runtime.Dice.DrawInteger(24);
					tries++;

					if ((short)tries >= 0x63)
					{
						break;
					}
				}
				while (buildingIndex == 0 || !town.HasStructureSignExtended(buildingIndex));

				if ((short)tries < 0x63)
				{
					ushort keep = (ushort)~(ushort)(1 << (byte)buildingIndex);

					town.StructureMarkersLow &= keep;
					town.StructureMarkersHigh &= (keep & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

					Message(GameTables.BuildingText(buildingIndex))
						.Text(" destroyed\n");

					destroyed = true;
				}
			}

			if (!destroyed)
			{
				town.MaterialsTally = 0;

				string targetName = town.PresentMakingIndex switch
				{
					SpecialProjects.Wealth => "Wealth",
					SpecialProjects.Research => "Research",
					SpecialProjects.Food => "Food",
					>= 0 => this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, GameTables.UnitText(town.PresentMakingIndex))),
					_ => this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, GameTables.BuildingRecordText(-town.PresentMakingIndex)))
				};

				Message(targetName)
					.Text(" production sabotaged\n");
			}

			this.Owner.Words.Append("in ");

			this.Owner.Conquest.ObtainCityLabel(townIndex);

			this.Owner.Words.Append(".\n");

			Screen.DialogStyle = 0x0;

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 80, 80);

			this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
		}

		private void InciteRevolt(short townIndex, short participantIndex, short troopIndex, short ownerIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			ushort distance = this.Owner.Politics.FindNearestCityOfPlayer(ownerIndex, town.Place.X, town.Place.Y);

			if (distance == 0)
			{
				return;
			}

			short cost = (short)(CentralProcessor.SignedQuotient((short)(ushort)((ushort)this.Owner.World.Participants[ownerIndex].Gold + 0x3e8), (short)(distance + 3)) * town.TrueExtent);

			if ((town.ConditionMarker & 1) != 0)
			{
				cost = CentralProcessor.SignedQuotient(cost, 2);
			}

			Message("Dissidents in ")
				.City(townIndex)
				.Text("\nwill revolt for $")
				.Number(cost)
				.Text(".\n");

			if (cost <= this.Owner.World.Participants[participantIndex].Gold)
			{
				this.Owner.Words.Append(" Forget it.\n Incite revolt\n");

				if ((short)(cost << 1) <= this.Owner.World.Participants[participantIndex].Gold)
				{
					if ((this.Owner.World.Participants[participantIndex].Relations[ownerIndex] & 2) != 0)
					{
						this.Owner.Words.Append(" Subvert city ($x2)\n");
					}
				}
			}

			Screen.DialogStyle = 0x0;

			short choice = (short)this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);

			if (choice < 1)
			{
				return;
			}

			if (choice == 1)
			{
				if ((this.Owner.World.Participants[ownerIndex].Relations[participantIndex] & 2) != 0)
				{
					this.Owner.Politics.ClearDiplomacyFlags(ownerIndex, participantIndex, 2);

					this.Owner.World.Participants[ownerIndex].Relations[participantIndex] |= 8;
				}
			}

			this.Owner.World.Participants[participantIndex].Gold -= (short)(cost * choice);

			ComputeInciteCost(townIndex, participantIndex);

			this.Owner.MapOperations.RepaintMapTile(town.Place.X, town.Place.Y);

			this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);

			this.Owner.TurnHooks.DrawStatusBar();
		}

		private void OfferBribe(short participantIndex, short troopIndex, short firstParticipantIndex, short price)
		{
			short humanIndex = this.Owner.World.PersonParticipantIndex;
			UnitMemory troops = new UnitMemory(this.Processor);
			ushort kindName = (ushort)((troops.TroopKind(participantIndex, troopIndex) * UnitKindTable.Stride) + UnitKindTable.Origin);

			Message(Tables.CivilizationName(participantIndex))
				.Text(" ")
				.Text(kindName)
				.Text("\nwill desert for ")
				.Number(price)
				.Text("$.\nTreasury: ")
				.Number(this.Owner.World.Participants[firstParticipantIndex].Gold)
				.Text("$\n");

			if (price <= this.Owner.World.Participants[firstParticipantIndex].Gold)
			{
				this.Owner.Words.Append(" Forget it.\n Pay\n");
			}

			if (firstParticipantIndex == humanIndex)
			{
				if (this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80) != 1)
				{
					return;
				}
			}

			if (participantIndex == humanIndex)
			{
				this.Owner.MapOperations.FocusMapView(participantIndex, troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal) - 8,
					troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical) - 6);

				Message(Tables.CivilizationName(participantIndex))
					.Text(" ")
					.Text(kindName)
					.Text(" unit\nbribed by ")
					.Text(Tables.CivilizationPlural(firstParticipantIndex))
					.Text("!\n")
					.Style(0)
					.Dialog(80, 40);
			}

			short kind = troops.TroopKind(participantIndex, troopIndex);

			this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);

			this.Owner.World.Participants[participantIndex].LiveTroops[kind]--;

			ushort bribedIndex = this.Owner.UnitOperations.CreateUnit(firstParticipantIndex, kind,
				troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal), troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

			if (bribedIndex != 0xffff)
			{
				this.Owner.World.Participants[firstParticipantIndex].LiveTroops[kind]++;

				troops.Assign(firstParticipantIndex, bribedIndex, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(firstParticipantIndex, bribedIndex, UnitMemory.ShownToParticipants) | (1 << participantIndex)));
			}

			this.Owner.World.Participants[firstParticipantIndex].Gold -= price;

			if (participantIndex == humanIndex)
			{
				this.Owner.MapOperations.RepaintMapTile(troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
					troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

				this.Owner.TurnHooks.DrawStatusBar();

				this.Owner.TextPainter.WaitForTime(30);
			}
		}

		private void Revolt(short townIndex, short participantIndex, short ownerIndex, short horizontalLocation, short verticalLocation)
		{
			City town = this.Owner.World.Towns[townIndex];
			sbyte sight = (sbyte)(byte)(1 << (byte)participantIndex);

			this.Processor.SegmentExtra.Word = 0x3772;

			this.Owner.World.ChartSight[(ushort)horizontalLocation, (ushort)verticalLocation] |= sight;

			for (int deltaIndex = 0; deltaIndex < 0x14; deltaIndex++)
			{
				this.Owner.World.ChartSight[(ushort)(short)(this.Owner.TownDeltas[deltaIndex].X + horizontalLocation),
					(ushort)(short)(this.Owner.TownDeltas[deltaIndex].Y + verticalLocation)] |= sight;
			}

			short humanIndex = this.Owner.World.PersonParticipantIndex;
			ushort[] humanRelations = this.Owner.World.Participants[humanIndex].Relations;

			if (ownerIndex == humanIndex || participantIndex == humanIndex || (humanRelations[ownerIndex] & 0x40) != 0 || (humanRelations[participantIndex] & 0x40) != 0)
			{
				if ((this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)humanIndex)) != 0)
				{
					this.Owner.MapOperations.FocusMapView(humanIndex, town.Place.X - 8, town.Place.Y - 5);
				}

				Message(Tables.CivilizationPlural(ownerIndex))
					.Text(participantIndex == 0 ? (ushort)0x5322 : (ushort)0x533c)
					.City(townIndex);

				if (participantIndex == 0)
				{
					this.Owner.Words.Append(0x536c);
				}
				else
				{
					this.Owner.Words.Append(".\n");

					this.Owner.Words.Append(Tables.CivilizationName(participantIndex));

					this.Owner.Words.Append(0x5355);
				}

				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 32);
			}

			UnitMemory troops = new UnitMemory(this.Processor);

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				int distance = this.Owner.Helpers.ObtainDistance(horizontalLocation, troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Horizontal),
					verticalLocation, troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Vertical));

				if (troops.Pull(ownerIndex, troopIndex, UnitMemory.Kind) == 0xff || (short)distance > 1)
				{
					continue;
				}

				if (distance != 0)
				{
					if ((this.Owner.MapOperations.ObtainTileImprovements(troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Horizontal),
						troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Vertical)) & 1) != 0)
					{
						continue;
					}
				}

				sbyte kind = troops.TroopKind(ownerIndex, troopIndex);

				this.Owner.UnitOperations.DestroyUnit(ownerIndex, troopIndex);

				this.Owner.MapOperations.MapAssignCityOwner(participantIndex, troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Horizontal),
					troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Vertical));

				this.Owner.MapOperations.TagTileOwned(troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Horizontal), troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Vertical));

				ushort newIndex = this.Owner.UnitOperations.CreateUnit(participantIndex, kind, troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Horizontal),
					troops.PullSigned(ownerIndex, troopIndex, UnitMemory.Vertical));

				if (newIndex == 0xffff)
				{
					continue;
				}

				troops.Assign(participantIndex, newIndex, UnitMemory.ShownToParticipants,
					(byte)(troops.Pull(participantIndex, newIndex, UnitMemory.ShownToParticipants) | (byte)(1 << (byte)ownerIndex)));
				troops.Assign(participantIndex, newIndex, UnitMemory.BaseTown, (byte)townIndex);
			}

			short preferences = this.Owner.World.SessionPreferenceMarkers;

			Turn.NoConquestAnimation = 0x1;

			if ((short)Files.FirstMissingDisk <= 1)
			{
				this.Owner.World.SessionPreferenceMarkers &= 0x7ff7;
			}

			this.Owner.Conquest.SeizeCity(participantIndex, townIndex, 1);

			Turn.NoConquestAnimation = 0x0;
			this.Owner.World.SessionPreferenceMarkers = preferences;

			this.Owner.MapOperations.MapAssignCityOwner(participantIndex, town.Place.X, town.Place.Y);
		}
	}
}
