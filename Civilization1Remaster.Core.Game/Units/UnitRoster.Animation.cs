namespace Civilization1Remaster.Core.Game.Units
{
	public partial class UnitRoster
	{
		public void ChoreographUnitMove(short participantIndex, short troopIndex, ushort cursorHorizontal, ushort cursorVertical)
		{
			using var _ = this.Processor.Journal.Scope($"ChoreographUnitMove({participantIndex}, {troopIndex}, {cursorHorizontal}, {cursorVertical})");

			short frameHorizontal = -1;
			short frameVertical = 0;

			if (cursorHorizontal != 0xffff)
			{
				frameHorizontal = (short)((this.Owner.Routing.TunePosition((short)cursorHorizontal - (short)MapView.Horizontal) << 4) + 0x50);
				frameVertical = (short)(((cursorVertical - MapView.Vertical) << 4) + 0x8);

				if (frameHorizontal < 0x50 || frameHorizontal >= 0x140 || frameVertical < 0x8 || frameVertical > 0xc0)
				{
					frameHorizontal = -1;
				}
			}

			short worldMapTop = (short)MapView.FocusTop;
			short dotHorizontal = (short)this.Owner.Routing.TunePosition(
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal) - (short)MapView.FocusLeft);
			int unitVertical = troopIndex < 127 ? this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) : 0xff;
			short dotVertical = (short)(unitVertical - worldMapTop);
			bool dotVisible = dotVertical >= 0 && dotVertical < 50;

			do
			{
				this.Owner.Startup.HideMouseCursor();

				if (troopIndex >= UnitMemory.TroopTally)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextEndOfTurnSecond, 4, 124, Hues.LightGray);
				}
				else
				{
					this.Owner.MapOperations.DrawMapTile(
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

					if (dotVisible)
					{
						this.Owner.Services.AssignDot(dotHorizontal, dotVertical + 8, 15);
					}
				}

				if (frameHorizontal != -1)
				{
					this.Owner.Dialogs.DrawBox(frameHorizontal, frameVertical, 15, 15, Hues.White);
				}

				WaitForBlink();

				this.Owner.Startup.HideMouseCursor();

				if (troopIndex >= UnitMemory.TroopTally)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextEndOfTurnThird, 4, 124, Hues.Black);
				}
				else
				{
					this.Owner.MapOperations.DrawUnitOnWorldMap(participantIndex, troopIndex);

					if (dotVisible)
					{
						ushort dotColour = this.Owner.MapOperations.ObtainTerrainType(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal), this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) == MapRules.OceanTerrain ? (ushort)1 : (ushort)2;

						this.Owner.Services.AssignDot(dotHorizontal, dotVertical + 8, dotColour);
					}
				}

				if (frameHorizontal != -1)
				{
					this.Owner.Dialogs.DrawBox(frameHorizontal, frameVertical, 15, 15, Hues.Black);
				}

				WaitForBlink();

				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.Runtime.KeyAvailable() == 0 && this.Owner.MenuPointerState == 0);
		}

		public void ChoreographUnitCombat(short participantIndex, short troopIndex, ushort direction)
		{
			using var _ = this.Processor.Journal.Scope($"ChoreographUnitCombat({participantIndex}, {troopIndex}, {direction})");

			int horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			int verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

			if (MapView.WorldMapView == 1)
			{
				this.Owner.MapOperations.DrawMapTile(horizontalLocation, verticalLocation);
			}
			else
			{
				this.Owner.Startup.HideMouseCursor();

				if (this.Owner.MapOperations.DrawMapTile(horizontalLocation, verticalLocation) != 0 &&
					this.Owner.TurnLogic.CheckPositionOnScreen(horizontalLocation, verticalLocation) != 0 &&
					this.Owner.TurnLogic.CheckPositionOnScreen(this.Owner.MapOperations.Chart.DirectionHorizontal(direction) + horizontalLocation,
						this.Owner.MapOperations.Chart.DirectionVertical(direction) + verticalLocation) != 0)
				{
					AnimateAttack(participantIndex, troopIndex, direction, horizontalLocation, verticalLocation);
				}

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		public void DrawBusyIndicator()
		{
			using var _ = this.Processor.Journal.Scope("RenderBusyIndicator()");

			int styleLeft = (this.Owner.Runtime.Dice.DrawInteger(2) << 5) + 0xc0;

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

			this.Owner.TextPainter.WaitForTime((short)(this.Owner.Runtime.KeyAvailable() != 0 ? 1 : 10));
		}

		private void AnimateAttack(short participantIndex, short troopIndex, int heading, int horizontalLocation, int verticalLocation)
		{
			sbyte followingIndex = this.troops.Following(participantIndex, troopIndex);

			if (followingIndex != -1 &&
				(this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) != MapRules.OceanTerrain ||
				this.troops.KindField(participantIndex, followingIndex, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
			{
				this.Owner.MapOperations.DrawUnitOnWorldMap(participantIndex, followingIndex);
			}

			ushort screen = Screen.Box;
			ushort backScreen = Screen.BackScreenBox;

			this.Owner.VideoCard.RenderImage(screen, 0x50, 0, 0xf0, 0xc8, backScreen, 0x50, 0);

			short tileLeft = (short)((this.Owner.Routing.TunePosition(horizontalLocation - (short)MapView.Horizontal) << 4) + 0x50);
			short tileTop = (short)(((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) - MapView.Vertical) << 4) + 8);

			int stepHorizontal = this.Owner.MapOperations.Chart.DirectionHorizontal(heading);
			int stepVertical = this.Owner.MapOperations.Chart.DirectionVertical(heading);

			for (int frame = 0; frame <= 16; frame++)
			{
				ushort picture = this.Processor.PullGlobalWord(
					(ushort)(0xd4ce + ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) + (participantIndex << 5) + 0x40) << 1)));

				this.Owner.Services.DrawPictureToScreen(screen,
					(stepHorizontal * frame) + tileLeft + 1, (stepVertical * frame) + tileTop + 1, picture);

				this.Owner.TextPainter.WaitForTime(1);

				ushort frameLeft = (ushort)((stepHorizontal * frame) + tileLeft);
				ushort frameTop = (ushort)((stepVertical * frame) + tileTop);

				this.Owner.VideoCard.RenderImage(backScreen, frameLeft, frameTop, 0x10, 0x10, screen, frameLeft, frameTop);
			}
		}

		private void DrawBorderPiece(int pieceLeft, int pieceTop, int horizontalLocation, int verticalLocation)
		{
			this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, pieceLeft, pieceTop, 8, 8,
				Screen.Box, horizontalLocation, verticalLocation);
		}
	}
}
