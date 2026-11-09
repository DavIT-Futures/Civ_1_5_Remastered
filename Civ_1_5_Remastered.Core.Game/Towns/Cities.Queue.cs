namespace Civ_1_5_Remastered.Core.Game.Towns
{
	public partial class Cities
	{
		/// <summary>Display name of a production item (unit kind, -improvement or special project).</summary>
		public string ProductionName(sbyte making)
		{
			if (making >= 0)
			{
				return this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, GameTables.UnitText(making)));
			}

			return making switch
			{
				SpecialProjects.Wealth => "Wealth",
				SpecialProjects.Research => "Research",
				SpecialProjects.Food => "Food",
				_ => this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, GameTables.BuildingRecordText(-making))),
			};
		}

		/// <summary>Shows and edits the production queue of a human city.</summary>
		public void ManageProductionQueue(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			RemasterExtras extras = this.Owner.World.Extras;
			short owner = town.ParticipantIndex;

			// The menus are drawn over the city screen without saving what lies below: put it back before each one.
			byte[] cityScreen = this.Owner.VideoCard.CaptureClassicScreen();

			while (true)
			{
				this.Owner.VideoCard.RestoreClassicScreen(cityScreen);

				int count = extras.QueueCount(townIndex, town);
				string text = "Production queue (built after\nthe current item):\n";

				for (int position = 0; position < count; position++)
				{
					text += " Remove " + (position + 1) + ". " + ProductionName(extras.QueueItem(townIndex, town, position)) + "\n";
				}

				text += " Add an item...\n Clear the queue\n Done\n";

				Menu.DisabledLines = 0;

				if (count >= RemasterExtras.QueueLength)
				{
					Menu.DisabledLines |= (ushort)(1 << count);
				}

				if (count == 0)
				{
					Menu.DisabledLines |= (ushort)(1 << (count + 1));
				}

				this.Owner.Startup.ShowMouseCursor();
				DrawingBox.TypefaceIndex = 1;
				ushort choice = this.Owner.Dialogs.ShowMenu(text, 96, 40, 1);
				this.Owner.Startup.HideMouseCursor();

				if (choice == 0xffff || choice == count + 2)
				{
					this.Owner.VideoCard.RestoreClassicScreen(cityScreen);
					return;
				}

				if (choice < count)
				{
					extras.RemoveAt(townIndex, town, choice);
				}
				else if (choice == count)
				{
					sbyte making = town.PresentMakingIndex;
					byte marker = town.ConditionMarker;

					town.ConditionMarker &= 0xef;
					this.Owner.Startup.ShowMouseCursor();
					ushort picked = this.Owner.Advisors.PickCityProduction(owner, townIndex);
					this.Owner.Startup.HideMouseCursor();
					town.ConditionMarker = marker;
					town.PresentMakingIndex = making;

					if (!this.Owner.Advisors.LastPickCancelled && picked != 0x63 && picked != 0x62)
					{
						extras.Enqueue(townIndex, town, (sbyte)(byte)picked);
					}
				}
				else
				{
					extras.ClearQueue(townIndex);
				}
			}
		}

		/// <summary>
		/// Starts the next buildable item of the queue after the city finished something.
		/// </summary>
		/// <returns><c>true</c> when production switched to a queued item.</returns>
		private bool AdvanceProductionQueue(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			RemasterExtras extras = this.Owner.World.Extras;
			short owner = town.ParticipantIndex;

			while (extras.QueueCount(townIndex, town) > 0)
			{
				sbyte next = extras.Dequeue(townIndex, town);

				if (!CanBuildQueued(townIndex, next))
				{
					continue;
				}

				StopMakingUnit(town, owner);
				town.PresentMakingIndex = next;
				StartMakingUnit(town, owner);

				return true;
			}

			return false;
		}

		private bool CanBuildQueued(short townIndex, sbyte making)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = town.ParticipantIndex;

			if (SpecialProjects.IsSpecialProject(making))
			{
				return true;
			}

			if (making >= 0)
			{
				return making < 0x1c &&
					this.Owner.Advisors.PlayerKnowsTechnology(owner, (short)Tables.UnitKindWord(making, UnitKindTable.Technology)) != 0 &&
					((town.ConditionMarker & 2) != 0 || Tables.UnitKindWord(making, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits);
			}

			short improvement = (short)-making;

			if (improvement < 1 || this.Owner.Advisors.PlayerKnowsTechnology(owner, (short)Tables.BuildingWord(improvement, BuildingTable.Technology)) == 0)
			{
				return false;
			}

			if (improvement > 0x18)
			{
				return this.Owner.World.MarvelTownIndex[improvement - 24] == -1;
			}

			return improvement > 0x15 || !TestImprovement(townIndex, improvement);
		}
	}
}
