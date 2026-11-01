namespace Civilization1Remaster.Core.UI.Help
{
	public class HelpPopups : GameComponent, IHelpPopups
	{
		private static readonly GameText HelpFileNameAddress = new(0x34b2, "help.txt");
		private static readonly GameText HelpFileNameSecondAddress = new(0x34de, "help.txt");
		private static readonly GameText HelpFileNameThirdAddress = new(0x3501, "error.txt");
		private static readonly GameText HelpCitySiteTopicAddress = new(0x34bb, "*BUILDCITY");
		private static readonly GameText HelpIrrigateTopicAddress = new(0x34c6, "*MINING");
		private static readonly GameText HelpIrrigateSecondTopicAddress = new(0x34ce, "*IRRIGATE");
		private static readonly GameText HelpMineTopicAddress = new(0x34d8, "*ROAD");
		private static readonly GameText HelpAdviceHeaderAddress = new(0x34e7, "--- Civilization Note ---");
		private static readonly GameText HelpAdviceHeaderSecondAddress = new(0x350b, "--- Civilization Note ---");

		public HelpPopups(IGameEngine owner)
			: base(owner)
		{
		}

		public void ShowHelpTopic(ushort charactersAddress)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayHelpTopic(0x{charactersAddress:x4})");

			ushort upper = this.Owner.Runtime.UppercaseText(charactersAddress);
			string topic = "*" + this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, upper));

			this.Owner.Words.Clear();

			if (this.Owner.BlockText.LoadTextFromFile(HelpFileNameAddress, topic) != 0)
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(
					Screen.Box,
					0, 0, 0x140, 0xc8,
					Screen.BackScreenBox,
					0, 0);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Green);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(topic, 160, 192, Hues.Black);
				this.Owner.BlockText.DrawTextChunk(32, 32, 32, 15);
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					0, 0, 0x140, 0xc8,
					Screen.Box,
					0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}

			Menu.HelpTopic = 0x0;
		}

		public void ShowTileInformation(short participantIndex, ushort troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayTileInfo({participantIndex}, {troopIndex})");

			if ((short)troopIndex < 0x80)
			{
				UnitMemory troops = new UnitMemory(this.Processor);
				sbyte horizontalLocation = troops.PullSigned(participantIndex, (short)troopIndex, UnitMemory.Horizontal);
				sbyte verticalLocation = troops.PullSigned(participantIndex, (short)troopIndex, UnitMemory.Vertical);
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

				bool cityPlace = (short)((sbyte)Tables.TerrainField(terrain, TerrainTable.Food) +
					(sbyte)Tables.TerrainField(terrain, TerrainTable.Shields)) >= 2;

				short worth = (short)this.Owner.VideoCard.ObtainDot(2, horizontalLocation + 80, verticalLocation);

				if (worth < 0xa)
				{
					cityPlace = false;
				}

				IMapPlanes chart = this.Owner.MapOperations.Chart;

				for (int heading = 1; heading <= 8; heading++)
				{
					if ((short)this.Owner.VideoCard.ObtainDot(2, horizontalLocation + chart.DirectionHorizontal(heading) + 80, verticalLocation + chart.DirectionVertical(heading)) > worth)
					{
						cityPlace = false;
					}
				}

				if (cityPlace)
				{
					this.Owner.MapOperations.FocusMapView(this.Owner.World.PersonParticipantIndex, horizontalLocation - 7, verticalLocation - 6);

					ShowHelpPopup(HelpCitySiteTopicAddress);
				}
				else
				{
					ShowImprovementHints(participantIndex, horizontalLocation, verticalLocation, terrain);
				}
			}
		}

		public void ShowHelpPopup(GameText topic, bool always = false)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayHelpPopup(0x{topic.Address:x4})");

			if (always || ((this.Owner.World.SessionPreferenceMarkers & 0x1) != 0 && this.Owner.World.Season < 0))
			{
				this.Owner.Words.Clear();

				ushort lineTally = this.Owner.BlockText.LoadTextFromFile(HelpFileNameSecondAddress, topic);

				this.Owner.BlockText.ReflowText();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
					Screen.BackScreenBox, 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.Owner.Services.FloodBox(Screen.Box, 56, 16, 208, height, Hues.Green);

				this.Owner.Dialogs.DrawBox(56, 16, 208, height, Hues.LightGreen);

				DrawingBox.TypefaceIndex = 0x2;

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(HelpAdviceHeaderAddress, 160, 19, Hues.Black);

				this.Owner.BlockText.DrawTextChunk(32, 64, 25, 15);

				DrawingBox.TypefaceIndex = 0x1;

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
					Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		public void ShowHelpPopupForHuman(GameText topic)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayHelpPopupForHuman(0x{topic.Address:x4})");

			if (Turn.Player == (ushort)this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.Words.Clear();

				ushort lineTally = this.Owner.BlockText.LoadTextFromFile(HelpFileNameThirdAddress, topic);

				this.Owner.BlockText.ReflowText();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
					Screen.BackScreenBox, 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.Owner.Services.FloodBox(Screen.Box, 56, 16, 208, height, Hues.Red);

				this.Owner.Dialogs.DrawBox(56, 16, 208, height, Hues.LightRed);

				DrawingBox.TypefaceIndex = 0x2;

				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(HelpAdviceHeaderSecondAddress, 160, 19, Hues.Black);

				this.Owner.BlockText.DrawTextChunk(32, 64, 25, 15);

				DrawingBox.TypefaceIndex = 0x1;

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
					Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		private void ShowImprovementHints(short participantIndex, int horizontalLocation, int verticalLocation, ushort terrain)
		{
			ushort townIndex = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);

			if (townIndex == 0xffff || (ushort)this.Owner.World.Towns[townIndex].ParticipantIndex != (ushort)participantIndex ||
				Search.FoundDistance != 1)
			{
				return;
			}

			byte improvements = (byte)this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

			EmptyMessage()
				.City((short)townIndex);
			this.Owner.Codes.Set(0, this.Owner.Words.ToString());

			if (terrain == 4 && (improvements & 6) == 0)
			{
				ShowHelpPopup(HelpIrrigateTopicAddress);
			}

			if (terrain == 1 && this.Owner.TurnLogic.CheckCoastalTile(horizontalLocation, verticalLocation) != 0 && (improvements & 6) == 0)
			{
				ShowHelpPopup(HelpIrrigateSecondTopicAddress);
				return;
			}

			if (Tables.TerrainField(terrain, TerrainTable.MoveCost) == 1 && (improvements & 8) == 0 && terrain != 0xb &&
				this.Owner.World.Participants[participantIndex].LiveTroops[0] < 2)
			{
				ShowHelpPopup(HelpMineTopicAddress);
			}
		}
	}
}
