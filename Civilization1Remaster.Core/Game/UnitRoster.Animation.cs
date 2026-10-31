using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The animations of the moves and the attacks of the units, and the border of the screen</summary>
	public partial class UnitRoster
	{
		/// <inheritdoc/>
		public void ChoreographUnitMove(short participantIdx, short troopIdx, ushort cursorHoriz, ushort cursorVert)
		{
			using var _ = this.Processor.Journal.Scope($"ChoreographUnitMove({participantIdx}, {troopIdx}, {cursorHoriz}, {cursorVert})");

			// Screen position of the cursor frame, -1 when there is none or it is outside the map view
			short frameHoriz = -1;
			short frameVert = 0;

			if (cursorHoriz != 0xffff)
			{
				frameHoriz = (short)((this.Owner.Routing.TunePosition((short)cursorHoriz - (short)MapView.Horiz) << 4) + 0x50);
				frameVert = (short)(((cursorVert - MapView.Vert) << 4) + 0x8);

				if (frameHoriz < 0x50 || frameHoriz >= 0x140 || frameVert < 0x8 || frameVert > 0xc0)
					frameHoriz = -1;
			}

			// Position of the unit on the world map (top left corner of the screen)
			short worldMapTop = (short)MapView.FocusTop;
			short dotHoriz = (short)this.Owner.Routing.TunePosition(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - (short)MapView.FocusLeft);
			int unitVert = troopIdx < 127 ? this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) : 0xff;
			short dotVert = (short)(unitVert - worldMapTop);
			bool dotVisible = dotVert >= 0 && dotVert < 50;

			do
			{
				// Hidden phase
				this.Owner.Startup.HideMouseCursor();

				if (troopIdx >= UnitMemory.TroopTally)
				{
					this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextEndOfTurnB, 4, 124, Hues.LightGray);
				}
				else
				{
					this.Owner.MapOps.DrawMapTile(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (dotVisible)
					{
						this.Owner.Services.AssignDot(dotHoriz, dotVert + 8, 15);
					}
				}

				if (frameHoriz != -1)
				{
					this.Owner.Dialogs.DrawBox(frameHoriz, frameVert, 15, 15, Hues.White);
				}

				WaitForBlink();

				// Shown phase
				this.Owner.Startup.HideMouseCursor();

				if (troopIdx >= UnitMemory.TroopTally)
				{
					this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextEndOfTurnC, 4, 124, Hues.Black);
				}
				else
				{
					this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, troopIdx);

					if (dotVisible)
					{
						ushort dotColour = this.Owner.MapOps.ObtainTerrainType(
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain ? (ushort)1 : (ushort)2;

						this.Owner.Services.AssignDot(dotHoriz, dotVert + 8, dotColour);
					}
				}

				if (frameHoriz != -1)
				{
					this.Owner.Dialogs.DrawBox(frameHoriz, frameVert, 15, 15, Hues.Black);
				}

				WaitForBlink();

				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.Libc.KeyAvailable() == 0 && this.Owner.GlobDb3a == 0);
		}

		/// <inheritdoc/>
		public void ChoreographUnitCombat(short participantIdx, short troopIdx, ushort direction)
		{
			using var _ = this.Processor.Journal.Scope($"ChoreographUnitCombat({participantIdx}, {troopIdx}, {direction})");

			int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			if (MapView.WorldMapView == 1)
			{
				this.Owner.MapOps.DrawMapTile(horizLoc, vertLoc);
			}
			else
			{
				this.Owner.Startup.HideMouseCursor();

				if (this.Owner.MapOps.DrawMapTile(horizLoc, vertLoc) != 0 &&
					this.Owner.TurnLogic.CheckPositionOnScreen(horizLoc, vertLoc) != 0 &&
					this.Owner.TurnLogic.CheckPositionOnScreen(this.Owner.MapOps.Chart.DirectionHoriz(direction) + horizLoc,
						this.Owner.MapOps.Chart.DirectionVert(direction) + vertLoc) != 0)
				{
					AnimateAttack(participantIdx, troopIdx, direction, horizLoc, vertLoc);
				}

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		/// <inheritdoc/>
		public void DrawBusyIndicator()
		{
			using var _ = this.Processor.Journal.Scope("RenderBusyIndicator()");

			int styleLeft = (this.Owner.Libc.Dice.DrawInt(2) << 5) + 0xc0;

			DrawBorderPiece(styleLeft, 0x78, 0, 0);
			DrawBorderPiece(styleLeft + 8, 0x78, 0x138, 0);
			DrawBorderPiece(styleLeft + 0x18, 0x78, 0x138, 0xc0);
			DrawBorderPiece(styleLeft + 0x10, 0x78, 0, 0xc0);

			for (int row = 1; row < 0x18; row++)
			{
				DrawBorderPiece(styleLeft + 0x18, 0x80, 0, row << 3);
				DrawBorderPiece(styleLeft + 8, 0x80, 0x138, row << 3);
			}

			for (int column = 1; column < 0x27; column++)
			{
				DrawBorderPiece(styleLeft, 0x80, column << 3, 0);
				DrawBorderPiece(styleLeft + 0x10, 0x80, column << 3, 0xc0);
			}
		}

		/// <summary>
		/// Shows the mouse pointer and waits 10 timer ticks, 1 when a key is waiting
		/// </summary>
		private void WaitForBlink()
		{
			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TextPainter.WaitForTime((short)(this.Owner.Libc.KeyAvailable() != 0 ? 1 : 10));
		}

		/// <summary>
		/// Draws the next unit of the stack on the tile (on the ocean only a ship), keeps the map view in the back screen (0x19d4)
		/// and moves the picture of the unit 16 pixels in the direction, restoring the background after every frame
		/// </summary>
		private void AnimateAttack(short participantIdx, short troopIdx, int heading, int horizLoc, int vertLoc)
		{
			sbyte followingIdx = this.troops.Following(participantIdx, troopIdx);

			if (followingIdx != -1 &&
				(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
				this.troops.KindField(participantIdx, followingIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
			{
				this.Owner.MapOps.DrawUnitOnWorldMap(participantIdx, followingIdx);
			}

			ushort screen = Screen.Box;
			ushort backScreen = Screen.BackScreenBox;

			this.Owner.VideoCard.RenderImage(screen, 0x50, 0, 0xf0, 0xc8, backScreen, 0x50, 0);

			// Top left pixel of the tile in the map view (16 x 16 pixels a tile, from 80, 8)
			short tileLeft = (short)((this.Owner.Routing.TunePosition(horizLoc - (short)MapView.Horiz) << 4) + 0x50);
			short tileTop = (short)(((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) - MapView.Vert) << 4) + 8);

			int stepHoriz = this.Owner.MapOps.Chart.DirectionHoriz(heading);
			int stepVert = this.Owner.MapOps.Chart.DirectionVert(heading);

			for (int frame = 0; frame <= 16; frame++)
			{
				ushort picture = this.Processor.PullGlobalWord(
					(ushort)(0xd4ce + ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) + (participantIdx << 5) + 0x40) << 1)));

				this.Owner.Services.DrawPictureToScreen(screen,
					(stepHoriz * frame) + tileLeft + 1, (stepVert * frame) + tileTop + 1, picture);

				this.Owner.TextPainter.WaitForTime(1);

				ushort frameLeft = (ushort)((stepHoriz * frame) + tileLeft);
				ushort frameTop = (ushort)((stepVert * frame) + tileTop);

				this.Owner.VideoCard.RenderImage(backScreen, frameLeft, frameTop, 0x10, 0x10, screen, frameLeft, frameTop);
			}
		}

		private void DrawBorderPiece(int pieceLeft, int pieceTop, int horizLoc, int vertLoc)
		{
			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, pieceLeft, pieceTop, 8, 8,
				Screen.Box, horizLoc, vertLoc);
		}
	}
}
