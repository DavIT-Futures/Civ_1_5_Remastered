using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class InGameHelp : GameModule
	{
		private const ushort HelpFileNameAddress = 0x34b2;
		private const ushort HelpFileName2Address = 0x34de;
		private const ushort HelpFileName3Address = 0x3501;

		private const ushort HelpCitySiteTopicAddress = 0x34bb;
		private const ushort HelpIrrigateTopicAddress = 0x34c6;
		private const ushort HelpIrrigate2TopicAddress = 0x34ce;
		private const ushort HelpMineTopicAddress = 0x34d8;

		private const ushort HelpAdviceHeaderAddress = 0x34e7;
		private const ushort HelpAdviceHeader2Address = 0x350b;

		public InGameHelp(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Shows the help text for a topic from HELP.TXT (0x34b2): prefixes the uppercase topic with '*' in the stack frame, draws the
		/// text on a blue background when found and restores the screen after a key press or mouse click
		/// </summary>
		public void DisplayHelpTopic(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpTopic(0x{charsAddr:x4})");

			ushort topicAddr = (ushort)(this.processor.RegStack.Value16 - 0x300);

			this.owner.Libc.CopyText(topicAddr, "*");
			this.owner.Libc.AppendText(topicAddr, this.owner.Libc.UppercaseText(charsAddr));

			this.owner.Words.Clear();

			if (this.owner.BlockText.ImportTextFromFile(HelpFileNameAddress, topicAddr) != 0)
			{
				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(
					ScreenBox,
					0, 0, 0x140, 0xc8,
					BackScreenBox,
					0, 0);

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 2);
				this.owner.TextPainter.RenderCenteredTextToScreen0(topicAddr, 160, 192, 0);
				this.owner.BlockText.RenderTextChunk(32, 32, 32, 15);
				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 0x140, 0xc8,
					ScreenBox,
					0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			MenuHelpTopic = 0x0;
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpTopic");
		}

		/// <summary>
		/// Hints for the unit's tile: a good place for a city (food + shields of the terrain at least 2, map layer 2 value at
		/// least 10 and not lower than around it) shows 0x34bb; else, on a tile next to (distance 1, 0x6c9a) a city of the
		/// player: irrigation for terrain 4 (0x34c6) or a coastal terrain 1 (0x34ce) without irrigation or mine, or a mine
		/// (0x34d8) where the terrain allows it (0x28e) and the player has less than 2 settlers
		/// </summary>
		public void DisplayTileInfo(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayTileInfo({participantIdx}, {arg2})");

			if ((short)arg2 < 0x80)
			{
				UnitMemory troops = new UnitMemory(this.processor);
				sbyte horizLoc = troops.PullSigned(participantIdx, (short)arg2, UnitMemory.Horiz);
				sbyte vertLoc = troops.PullSigned(participantIdx, (short)arg2, UnitMemory.Vert);
				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				bool cityPlace = (short)((sbyte)TerrainField(terrain, TerrainTable.Food) +
					(sbyte)TerrainField(terrain, TerrainTable.Shields)) >= 2;

				short worth = (short)this.owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);

				if (worth < 0xa)
				{
					cityPlace = false;
				}

				MapLayers chart = this.owner.MapOps.Chart;

				for (int heading = 1; heading <= 8; heading++)
				{
					if ((short)this.owner.VideoCard.ObtainDot(2, horizLoc + chart.DirectionHoriz(heading) + 80, vertLoc + chart.DirectionVert(heading)) > worth)
					{
						cityPlace = false;
					}
				}

				if (cityPlace)
				{
					this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, horizLoc - 7, vertLoc - 6);

					DisplayHelpPopup(HelpCitySiteTopicAddress);
				}
				else
				{
					DisplayImprovementHints(participantIdx, horizLoc, vertLoc, terrain);
				}
			}

			this.processor.Journal.CloseScope("DisplayTileInfo");
		}

		/// <summary>The hints of DisplayTileInfo for a tile next to a city of the player (the city's name in code text 0x30b8)</summary>
		private void DisplayImprovementHints(short participantIdx, int horizLoc, int vertLoc, ushort terrain)
		{
			ushort townIdx = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);

			if (townIdx == 0xffff || (ushort)this.owner.World.Towns[townIdx].ParticipantIdx != (ushort)participantIdx ||
				FoundDistance != 1)
				return;

			byte improvements = (byte)this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			EmptyMessage()
				.City((short)townIdx);
			this.owner.Libc.CopyText(CodeText1, this.owner.Words.ToString());

			if (terrain == 4 && (improvements & 6) == 0)
			{
				DisplayHelpPopup(HelpIrrigateTopicAddress);
			}

			if (terrain == 1 && this.owner.TurnLogic.VerifyCoastalTile(horizLoc, vertLoc) != 0 && (improvements & 6) == 0)
			{
				DisplayHelpPopup(HelpIrrigate2TopicAddress);
				return;
			}

			if (TerrainField(terrain, TerrainTable.MoveCost) == 1 && (improvements & 8) == 0 && terrain != 0xb &&
				this.owner.World.Participants[participantIdx].LiveTroops[0] < 2)
			{
				DisplayHelpPopup(HelpMineTopicAddress);
			}
		}

		/// <summary>
		/// With the help option on (SessionPrefMarkers bit 0) and only before year 0 (Season below 0): a help window with the
		/// text from the help file (0x34de) for the topic, its codes replaced (ReflowText), 6 pixels per line, until a key or
		/// click. The screen is kept in the back screen 0x19d4 meanwhile.
		/// </summary>
		public void DisplayHelpPopup(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpPopup(0x{charsAddr:x4})");

			if ((this.owner.World.SessionPrefMarkers & 0x1) != 0 && this.owner.World.Season < 0)
			{
				this.owner.Words.Clear();

				ushort lineTally = this.owner.BlockText.ImportTextFromFile(HelpFileName2Address, charsAddr);

				this.owner.BlockText.ReflowText();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
					BackScreenBox, 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.owner.Services.FloodBox(ScreenBox, 56, 16, 208, height, 2);

				this.owner.Dialogs.RenderBox(56, 16, 208, height, 10);

				DrawingBox.TypefaceIdx = 0x2;

				this.owner.TextPainter.RenderCenteredTextToScreen0(HelpAdviceHeaderAddress, 160, 19, 0);

				this.owner.BlockText.RenderTextChunk(32, 64, 25, 15);

				DrawingBox.TypefaceIdx = 0x1;

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayHelpPopup");
		}

		/// <summary>
		/// For the human player only (0x6b90): a help window with the text from the help file (0x3501) for the topic, its codes
		/// replaced (ReflowText), 6 pixels per line, until a key or click. The screen is kept in the back screen 0x19d4 meanwhile.
		/// </summary>
		public void DisplayHelpPopupForHuman(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpPopupForHuman(0x{charsAddr:x4})");

			if (TurnPlayer == (ushort)this.owner.World.PersonParticipantIdx)
			{
				this.owner.Words.Clear();

				ushort lineTally = this.owner.BlockText.ImportTextFromFile(HelpFileName3Address, charsAddr);

				this.owner.BlockText.ReflowText();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
					BackScreenBox, 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.owner.Services.FloodBox(ScreenBox, 56, 16, 208, height, 4);

				this.owner.Dialogs.RenderBox(56, 16, 208, height, 12);

				DrawingBox.TypefaceIdx = 0x2;

				this.owner.TextPainter.RenderCenteredTextToScreen0(HelpAdviceHeader2Address, 160, 19, 0);

				this.owner.BlockText.RenderTextChunk(32, 64, 25, 15);

				DrawingBox.TypefaceIdx = 0x1;

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayHelpPopupForHuman");
		}
	}
}
