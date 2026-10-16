using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class InGameHelp
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public InGameHelp(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Shows the help text for a topic from HELP.TXT (0x34b2): prefixes the uppercase topic with '*' in the stack frame, draws the
		/// text on a blue background when found and restores the screen after a key press or mouse click
		/// </summary>
		public void DisplayHelpTopic(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpTopic(0x{charsAddr:x4})");

			// The topic buffer stays in the stack frame at bp - 0x18
			this.processor.EnterFrame(0x18);

			ushort topicAddr = (ushort)(this.processor.RegFrame.Value16 - 0x18);

			this.owner.Libc.CopyText(topicAddr, "*");
			this.owner.Libc.UppercaseText(charsAddr);
			this.owner.Libc.AppendText(topicAddr, this.processor.RegAcc.Value16);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			if (this.owner.BlockText.ImportTextFromFile(0x34b2, topicAddr) != 0)
			{
				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0xaa),
					0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0x19d4),
					0, 0);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 2);
				this.owner.TextPainter.RenderCenteredTextToScreen0(topicAddr, 160, 192, 0);
				this.owner.BlockText.RenderTextChunk(32, 32, 32, 15);
				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0xaa),
					0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.EmitGlobalWord(0x2f9c, 0x0);
			this.processor.LeaveFrame();
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

				// Terrain records of 0x13 bytes: food at 0x290, shields at 0x291, mining at 0x28e
				ushort terrainRecord = (ushort)(terrain * 0x13);
				bool cityPlace = (short)((sbyte)this.processor.PullGlobalByte((ushort)(terrainRecord + 0x290)) +
					(sbyte)this.processor.PullGlobalByte((ushort)(terrainRecord + 0x291))) >= 2;

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

					DisplayHelpPopup(0x34bb);
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
				this.processor.PullGlobalWord(0x6c9a) != 1)
				return;

			byte improvements = (byte)this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Conquest.ObtainCityLabel((short)townIdx);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), 0xba06);

			if (terrain == 4 && (improvements & 6) == 0)
			{
				DisplayHelpPopup(0x34c6);
			}

			if (terrain == 1 && this.owner.TurnLogic.VerifyCoastalTile(horizLoc, vertLoc) != 0 && (improvements & 6) == 0)
			{
				DisplayHelpPopup(0x34ce);
				return;
			}

			if (this.processor.PullGlobalByte((ushort)((terrain * 0x13) + 0x28e)) == 1 && (improvements & 8) == 0 && terrain != 0xb &&
				this.owner.World.Participants[participantIdx].LiveTroops[0] < 2)
			{
				DisplayHelpPopup(0x34d8);
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
				this.processor.EmitGlobalByte(0xba06, 0x0);

				ushort lineTally = this.owner.BlockText.ImportTextFromFile(0x34de, charsAddr);

				this.owner.BlockText.ReflowText();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0x19d4), 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 56, 16, 208, height, 2);

				this.owner.Dialogs.RenderBox(56, 16, 208, height, 10);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x2);

				this.owner.TextPainter.RenderCenteredTextToScreen0(0x34e7, 160, 19, 0);

				this.owner.BlockText.RenderTextChunk(32, 64, 25, 15);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0xaa), 0, 0);

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

			if (this.processor.PullGlobalWord(0x6b90) == (ushort)this.owner.World.PersonParticipantIdx)
			{
				this.processor.EmitGlobalByte(0xba06, 0x0);

				ushort lineTally = this.owner.BlockText.ImportTextFromFile(0x3501, charsAddr);

				this.owner.BlockText.ReflowText();

				this.owner.Startup.ConcealMouseCursor();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0x19d4), 0, 0);

				short height = (short)((6 * lineTally) + 10);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 56, 16, 208, height, 4);

				this.owner.Dialogs.RenderBox(56, 16, 208, height, 12);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x2);

				this.owner.TextPainter.RenderCenteredTextToScreen0(0x350b, 160, 19, 0);

				this.owner.BlockText.RenderTextChunk(32, 64, 25, 15);

				this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0xaa), 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("DisplayHelpPopupForHuman");
		}
	}
}
