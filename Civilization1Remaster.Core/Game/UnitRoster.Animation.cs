namespace Civilization1Remaster.Core.Game
{
	public partial class UnitRoster
	{
		public void ChoreographUnitMove(short participantIdx, short troopIdx, ushort cursorHoriz, ushort cursorVert)
		{
			using var _ = this.Processor.Journal.Scope($"ChoreographUnitMove({participantIdx}, {troopIdx}, {cursorHoriz}, {cursorVert})");

			short frameHoriz = -1;
			short frameVert = 0;

			if (cursorHoriz != 0xffff)
			{
				frameHoriz = (short)((this.Owner.Routing.TunePosition((short)cursorHoriz - (short)MapView.Horiz) << 4) + 0x50);
				frameVert = (short)(((cursorVert - MapView.Vert) << 4) + 0x8);

				if (frameHoriz < 0x50 || frameHoriz >= 0x140 || frameVert < 0x8 || frameVert > 0xc0)
					frameHoriz = -1;
			}

			short worldMapTop = (short)MapView.FocusTop;
			short dotHoriz = (short)this.Owner.Routing.TunePosition(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - (short)MapView.FocusLeft);
			int unitVert = troopIdx < 127 ? this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) : 0xff;
			short dotVert = (short)(unitVert - worldMapTop);
			bool dotVisible = dotVert >= 0 && dotVert < 50;

			do
			{
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

		private void WaitForBlink()
		{
			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TextPainter.WaitForTime((short)(this.Owner.Libc.KeyAvailable() != 0 ? 1 : 10));
		}

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
