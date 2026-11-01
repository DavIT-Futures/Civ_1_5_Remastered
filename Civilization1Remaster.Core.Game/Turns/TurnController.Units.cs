namespace Civilization1Remaster.Core.Game.Turns
{
	public partial class TurnController
	{
		public void RepaintTileIfVisible(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintTileIfVisible({horizontalLocation}, {verticalLocation})");

			if ((this.Owner.World.ChartSight[horizontalLocation, verticalLocation] & (ushort)(1 << this.Owner.World.PersonParticipantIndex)) != 0 ||
				MapView.Revealed != 0)
			{
				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}
		}

		public void RepaintUnitIfVisible(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintUnitIfVisible({participantIndex}, {troopIndex})");

			short human = this.Owner.World.PersonParticipantIndex;

			if (participantIndex == human ||
				(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants) & (ushort)(1 << human)) != 0)
			{
				this.Owner.MapOperations.RepaintMapTile(
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));
			}
		}

		public ushort ObtainTileMoveCost(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTileMoveCost({horizontalLocation}, {verticalLocation})");

			ushort cost = 0;

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0x7) == 0)
			{
				int terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
				if ((short)Tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Mining) <= -3)
				{
					cost = 2;
				}
				else if (Tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Irrigation) == 0xfffe)
				{
					if (CheckCoastalTile(horizontalLocation, verticalLocation) != 0)
					{
						cost = 1;
					}
				}
			}

			return cost;
		}

		public ushort CheckCoastalTile(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyCoastalTile({horizontalLocation}, {verticalLocation})");

			IMapPlanes chart = this.Owner.MapOperations.Chart;
			ushort coastal = 0;

			if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == 0xb)
			{
				coastal = 1;
			}
			else
			{
				for (int heading = 1; heading <= 8 && coastal == 0; heading += 2)
				{
					short tileHorizontal = (short)(chart.DirectionHorizontal(heading) + horizontalLocation);
					short tileVertical = (short)(chart.DirectionVertical(heading) + verticalLocation);

					ushort terrain = this.Owner.MapOperations.ObtainTerrainType(tileHorizontal, tileVertical);
					ushort improvements = this.Owner.MapOperations.ObtainTileImprovements(tileHorizontal, tileVertical);

					if ((improvements & 0x1) == 0 && (terrain == 0xb || terrain == 0xa || (improvements & 0x2) != 0))
					{
						coastal = 1;
					}
				}
			}

			return coastal;
		}

		public void DrawUnitInformation(short participantIndex, ushort troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitInfo({participantIndex}, {troopIndex})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.TurnHooks.FillBoxWithShadow(0, 97, 80, 103);

			if (troopIndex == 0x80)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextEndOfTurn, 4, 124, Hues.Black);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextPressEnter, 4, 136, Hues.Black);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextToContinue, 4, 144, Hues.Black);
			}
			else
			{
				DrawUnitPanel(participantIndex, troopIndex);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort CheckPositionOnScreen(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Owner.Journal.Scope($"VerifyPositionOnScreen({horizontalLocation}, {verticalLocation})");
			ushort onScreen;
			if (horizontalLocation < 16 && (short)MapView.Horizontal >= 65)
			{
				horizontalLocation += 80;
			}

			if (horizontalLocation < (short)MapView.Horizontal ||
				horizontalLocation >= (short)MapView.Horizontal + 15 ||
				verticalLocation < (short)MapView.Vertical ||
				verticalLocation >= (short)MapView.Vertical + 12)
			{
				onScreen = 0;
			}
			else
			{
				onScreen = 1;
			}

			return onScreen;
		}

		public void WaitForKey()
		{
			using var _ = this.Processor.Journal.Scope("AwaitForKey()");

			while (this.Owner.Runtime.KeyAvailable() != 0)
			{
				this.Owner.Dialogs.ReadNavigationKey();
			}

			do
			{
				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.MenuPointerState != 0);
		}

		public ushort FindBestGuardian(short participantIndex, ushort troopIndex)
		{
			using var _ = this.Processor.Journal.Scope("LocateBestGuardian()");

			ushort best = troopIndex;
			short current = this.troops.Following(participantIndex, troopIndex);

			if (current != -1)
			{
				short smallestStep = 999;

				for (int stepIndex = 0; stepIndex < 0x10; stepIndex++)
				{
					if (this.troops.KindField(participantIndex, current, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					{
						short step = (short)(current > (short)troopIndex ? current - troopIndex : current - troopIndex + 0x80);

						if (step < smallestStep)
						{
							smallestStep = step;
							best = (ushort)current;
						}
					}

					current = this.troops.Following(participantIndex, current);

					if (current == (short)troopIndex)
					{
						break;
					}
				}
			}

			return best;
		}

		public void ShowHelpForHuman(GameText topic)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayHelpForHuman(0x{topic.Address:x4})");

			if ((short)Turn.Player == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.HelpSystem.ShowHelpPopupForHuman(topic);
			}
		}

		private bool ContinueOrFinishUnit(short participantIndex)
		{
			short troopIndex = (short)this.turn.Troop;

			if (troopIndex < UnitMemory.TroopTally)
			{
				if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) > 0 && this.turn.Ordered == 0 &&
					Turn.GameEndRequest == 0)
				{
					return true;
				}

				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xff)
				{
					return false;
				}

				if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) > 0)
				{
					this.turn.AllMoved = 0;
				}
				else
				{
					BurnPlaneFuel(participantIndex, troopIndex);
					MayLoseTrireme(participantIndex, troopIndex);
				}
			}

			if (this.Owner.World.IsHuman(participantIndex))
			{
				DrawUnitInformation(participantIndex, this.turn.Troop);
			}

			return false;
		}

		private void BurnPlaneFuel(short participantIndex, short troopIndex)
		{
			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway) == 0)
			{
				return;
			}

			ushort tileImprovements = this.Owner.MapOperations.ObtainTileImprovements(
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

			if ((tileImprovements & 1) != 0 ||
				this.Owner.UnitOperations.CountUnitsOfTypeInStack(participantIndex, (short)this.turn.Troop, 23) != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway));
			}

			byte rounds = (byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.UnusualSteps) - 1);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.UnusualSteps, rounds);

			if ((sbyte)rounds < 0)
			{
				this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);
				ShowHelpForHuman(GameTexts.TopicFuel);
			}
		}

		private void MayLoseTrireme(short participantIndex, short troopIndex)
		{
			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0x10 || participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				return;
			}

			if (this.Owner.Runtime.Dice.DrawInteger(2) == 0)
			{
				return;
			}

			this.turn.Found = 0;

			for (ushort direction = 1; direction <= 8; direction++)
			{
				this.turn.Count = direction;
				this.indexDestination = UnitMemory.Offset(participantIndex, troopIndex);

				if (this.Owner.MapOperations.ObtainTerrainType(
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal) + (short)Tables.DirectionHorizontalAt((ushort)(direction << 1)),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical) + (short)Tables.DirectionVerticalAt((ushort)(direction << 1))) != MapRules.OceanTerrain)
				{
					this.turn.Found = 1;
					break;
				}

				if (direction == 8)
				{
					this.turn.Count = 9;
				}
			}

			if (this.turn.Found != 0)
			{
				return;
			}

			this.Owner.UnitOperations.DestroyUnit(participantIndex, (short)this.turn.Troop);
			ShowHelpForHuman(GameTexts.TopicTrireme);
		}

		private void DrawUnitPanel(short participantIndex, ushort troop)
		{
			short troopIndex = (short)troop;
			short verticalLocation = 0x63;

			this.Owner.Words.Set(Tables.CivilizationName(participantIndex));

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, 99, Hues.Black);

			verticalLocation += 8;

			this.Owner.Words.Set(GameTables.UnitText(this.troops.TroopKind(participantIndex, troopIndex)));

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, verticalLocation, Hues.Black);

			verticalLocation += 8;

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0x20) != 0)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextVeteran, 8, verticalLocation, Hues.Black);

				verticalLocation += 8;
			}

			sbyte steps = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps);
			short moves = (short)this.Owner.Helpers.ClampValue(steps / 3, 0, 99);

			Message("Moves: ")
				.Number(moves);

			short thirds = (short)(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) % 3);

			if (thirds != 0)
			{
				this.Owner.Words.Append(".");

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(thirds, 10));
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.RoundsAway) != 0)
			{
				this.Owner.Words.Append("(");

				moves += (short)(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps) *
					(short)this.troops.KindField(participantIndex, troopIndex, UnitKindTable.ShiftTally));

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(moves, 10));

				this.Owner.Words.Append(")");
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, verticalLocation, Hues.Black);

			verticalLocation += 8;

			EmptyMessage()
				.City(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.BaseTown));

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, verticalLocation, Hues.Black);

			verticalLocation += 8;

			Message("(")
				.Text(GameTables.TerrainText(this.Owner.MapOperations.ObtainTerrainType(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal), this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical))))
				.Text(")");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, verticalLocation, Hues.Black);

			verticalLocation += 8;

			byte improvements = (byte)this.Owner.MapOperations.ObtainTileImprovements(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

			foreach ((byte bit, byte otherBit, ushort text, ushort otherText) in new[]
			{
				((byte)0x10, (byte)0x8, (ushort)0x20bd, (ushort)0x20c8),
				((byte)0x2, (byte)0x4, (ushort)0x20cf, (ushort)0x20dc), ((byte)0x40, (byte)0x0, (ushort)0x20e5, (ushort)0x0)
			})
			{
				ushort shown = (improvements & bit) != 0 ? text : (improvements & otherBit) != 0 ? otherText : (ushort)0;

				if (shown != 0)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(shown, 4, verticalLocation, Hues.Black);

					verticalLocation += 8;
				}
			}

			verticalLocation += 4;

			short followingIndex = this.troops.Following(participantIndex, troopIndex);
			short column = 8;

			if ((this.Owner.MapOperations.ObtainTileImprovements(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical)) & 1) != 0)
			{
				ushort townIndex = this.Owner.Helpers.FindCityAtPosition(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

				for (int unfamiliarIndex = 0; unfamiliarIndex < 2; unfamiliarIndex++)
				{
					byte unfamiliar = (byte)this.Owner.World.Towns[townIndex].Unfamiliar[unfamiliarIndex];

					if (unfamiliar == 0xff)
					{
						continue;
					}

					this.troops.Assign(participantIndex, 127, UnitMemory.Kind, (byte)(unfamiliar & 0x3f));
					this.troops.Assign(participantIndex, 127, UnitMemory.Status, 0x8);
					this.troops.Assign(participantIndex, 127, UnitMemory.TravelHorizontal, 0xff);

					this.Owner.MapOperations.DrawUnitSprite(participantIndex, 127, column, verticalLocation);

					this.troops.Assign(participantIndex, 127, UnitMemory.Kind, 0xff);

					column += 0x10;
				}
			}

			while (followingIndex != -1)
			{
				if (followingIndex == troopIndex || verticalLocation >= 0xb8)
				{
					break;
				}

				this.Owner.MapOperations.DrawUnitSprite(participantIndex, followingIndex, column, verticalLocation);

				column += 0x10;

				if (column > 0x40)
				{
					column = 8;
					verticalLocation += 0x10;
				}

				followingIndex = this.troops.Following(participantIndex, followingIndex);
			}

			if (followingIndex != -1 && followingIndex != troopIndex)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextPlus, 74, 192, Hues.Black);
			}
		}
	}
}
