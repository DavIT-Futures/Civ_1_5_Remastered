using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class HelpPopups : GameComponent, IHelpPopups
	{
		private static readonly GameText HelpFileNameAddress = new(0x34b2, "help.txt");
		private static readonly GameText HelpFileName2Address = new(0x34de, "help.txt");
		private static readonly GameText HelpFileName3Address = new(0x3501, "error.txt");
		private static readonly GameText HelpCitySiteTopicAddress = new(0x34bb, "*BUILDCITY");
		private static readonly GameText HelpIrrigateTopicAddress = new(0x34c6, "*MINING");
		private static readonly GameText HelpIrrigate2TopicAddress = new(0x34ce, "*IRRIGATE");
		private static readonly GameText HelpMineTopicAddress = new(0x34d8, "*ROAD");
		private static readonly GameText HelpAdviceHeaderAddress = new(0x34e7, "--- Civilization Note ---");
		private static readonly GameText HelpAdviceHeader2Address = new(0x350b, "--- Civilization Note ---");

		public HelpPopups(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void ShowHelpTopic(ushort charsAddr)
		{
			this.Processor.Journal.OpenScope($"DisplayHelpTopic(0x{charsAddr:x4})");

			// "*" and the topic in upper case (also in place, like the original)
			ushort upper = this.Owner.Libc.UppercaseText(charsAddr);
			string topic = "*" + this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, upper));

			this.Owner.Words.Clear();

			if (this.Owner.BlockText.LoadTextFromFile(HelpFileNameAddress, topic) != 0)
			{
				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(
					ScreenBox,
					0, 0, 0x140, 0xc8,
					BackScreenBox,
					0, 0);

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Green);
				this.Owner.TextPainter.DrawCenteredTextToScreen0(topic, 160, 192, Hues.Black);
				this.Owner.BlockText.DrawTextChunk(32, 32, 32, 15);
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 0x140, 0xc8,
					ScreenBox,
					0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}

			MenuHelpTopic = 0x0;
			this.Processor.Journal.CloseScope("DisplayHelpTopic");
		}

		/// <inheritdoc/>
		public void ShowTileInfo(short participantIdx, ushort troopIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayTileInfo({participantIdx}, {troopIdx})");

			if ((short)troopIdx < 0x80)
			{
				UnitMemory troops = new UnitMemory(this.Processor);
				sbyte horizLoc = troops.PullSigned(participantIdx, (short)troopIdx, UnitMemory.Horiz);
				sbyte vertLoc = troops.PullSigned(participantIdx, (short)troopIdx, UnitMemory.Vert);
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				bool cityPlace = (short)((sbyte)TerrainField(terrain, TerrainTable.Food) +
					(sbyte)TerrainField(terrain, TerrainTable.Shields)) >= 2;

				short worth = (short)this.Owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);

				if (worth < 0xa)
				{
					cityPlace = false;
				}

				IMapPlanes chart = this.Owner.MapOps.Chart;

				for (int heading = 1; heading <= 8; heading++)
				{
					if ((short)this.Owner.VideoCard.ObtainDot(2, horizLoc + chart.DirectionHoriz(heading) + 80, vertLoc + chart.DirectionVert(heading)) > worth)
					{
						cityPlace = false;
					}
				}

				if (cityPlace)
				{
					this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx, horizLoc - 7, vertLoc - 6);

					ShowHelpPopup(HelpCitySiteTopicAddress);
				}
				else
				{
					ShowImprovementHints(participantIdx, horizLoc, vertLoc, terrain);
				}
			}

			this.Processor.Journal.CloseScope("DisplayTileInfo");
		}

		/// <inheritdoc/>
		public void ShowHelpPopup(GameText topic, bool always = false)
		{
			this.Processor.Journal.OpenScope($"DisplayHelpPopup(0x{topic.Address:x4})");

			if (always || ((this.Owner.World.SessionPrefMarkers & 0x1) != 0 && this.Owner.World.Season < 0))
			{
				this.Owner.Words.Clear();

				ushort lineTally = this.Owner.BlockText.LoadTextFromFile(HelpFileName2Address, topic);

				this.Owner.BlockText.ReflowText();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
					BackScreenBox, 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.Owner.Services.FloodBox(ScreenBox, 56, 16, 208, height, Hues.Green);

				this.Owner.Dialogs.DrawBox(56, 16, 208, height, Hues.LightGreen);

				DrawingBox.TypefaceIdx = 0x2;

				this.Owner.TextPainter.DrawCenteredTextToScreen0(HelpAdviceHeaderAddress, 160, 19, Hues.Black);

				this.Owner.BlockText.DrawTextChunk(32, 64, 25, 15);

				DrawingBox.TypefaceIdx = 0x1;

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}

			this.Processor.Journal.CloseScope("DisplayHelpPopup");
		}

		/// <inheritdoc/>
		public void ShowHelpPopupForHuman(GameText topic)
		{
			this.Processor.Journal.OpenScope($"DisplayHelpPopupForHuman(0x{topic.Address:x4})");

			if (TurnPlayer == (ushort)this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Words.Clear();

				ushort lineTally = this.Owner.BlockText.LoadTextFromFile(HelpFileName3Address, topic);

				this.Owner.BlockText.ReflowText();

				this.Owner.Startup.HideMouseCursor();

				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
					BackScreenBox, 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.Owner.Services.FloodBox(ScreenBox, 56, 16, 208, height, Hues.Red);

				this.Owner.Dialogs.DrawBox(56, 16, 208, height, Hues.LightRed);

				DrawingBox.TypefaceIdx = 0x2;

				this.Owner.TextPainter.DrawCenteredTextToScreen0(HelpAdviceHeader2Address, 160, 19, Hues.Black);

				this.Owner.BlockText.DrawTextChunk(32, 64, 25, 15);

				DrawingBox.TypefaceIdx = 0x1;

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}

			this.Processor.Journal.CloseScope("DisplayHelpPopupForHuman");
		}

		/// <summary>The hints of ShowTileInfo for a tile next to a city of the player (the city's name in code text 0x30b8)</summary>
		private void ShowImprovementHints(short participantIdx, int horizLoc, int vertLoc, ushort terrain)
		{
			ushort townIdx = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);

			if (townIdx == 0xffff || (ushort)this.Owner.World.Towns[townIdx].ParticipantIdx != (ushort)participantIdx ||
				FoundDistance != 1)
				return;

			byte improvements = (byte)this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			EmptyMessage()
				.City((short)townIdx);
			this.Owner.Codes.Set(0, this.Owner.Words.ToString());

			if (terrain == 4 && (improvements & 6) == 0)
			{
				ShowHelpPopup(HelpIrrigateTopicAddress);
			}

			if (terrain == 1 && this.Owner.TurnLogic.CheckCoastalTile(horizLoc, vertLoc) != 0 && (improvements & 6) == 0)
			{
				ShowHelpPopup(HelpIrrigate2TopicAddress);
				return;
			}

			if (TerrainField(terrain, TerrainTable.MoveCost) == 1 && (improvements & 8) == 0 && terrain != 0xb &&
				this.Owner.World.Participants[participantIdx].LiveTroops[0] < 2)
			{
				ShowHelpPopup(HelpMineTopicAddress);
			}
		}
	}
}
