namespace Civilization1Remaster.Core.Game
{
	partial class TurnController
	{
		public void RepaintTileIfVisible(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintTileIfVisible({horizLoc}, {vertLoc})");

			if ((this.Owner.World.ChartSight[horizLoc, vertLoc] & (ushort)(1 << this.Owner.World.PersonParticipantIdx)) != 0 ||
				MapView.Revealed != 0)
			{
				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		public void RepaintUnitIfVisible(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintUnitIfVisible({participantIdx}, {troopIdx})");

			short human = this.Owner.World.PersonParticipantIdx;

			if (participantIdx == human ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << human)) != 0)
			{
				this.Owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}
		}

		public ushort ObtainTileMoveCost(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTileMoveCost({horizLoc}, {vertLoc})");

			ushort cost = 0;

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x7) == 0)
			{
				int terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				if ((short)Tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Mining) <= -3)
				{
					cost = 2;
				}
				else if (Tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Irrigation) == 0xfffe)
				{
					if (CheckCoastalTile(horizLoc, vertLoc) != 0)
						cost = 1;
				}
			}

			return cost;
		}

		public ushort CheckCoastalTile(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyCoastalTile({horizLoc}, {vertLoc})");

			IMapPlanes chart = this.Owner.MapOps.Chart;
			ushort coastal = 0;

			if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xb)
			{
				coastal = 1;
			}
			else
			{
				for (int heading = 1; heading <= 8 && coastal == 0; heading += 2)
				{
					short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
					short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

					ushort terrain = this.Owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);
					ushort improvements = this.Owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert);

					if ((improvements & 0x1) == 0 && (terrain == 0xb || terrain == 0xa || (improvements & 0x2) != 0))
					{
						coastal = 1;
					}
				}
			}

			return coastal;
		}

		public void DrawUnitInfo(short participantIdx, ushort troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitInfo({participantIdx}, {troopIdx})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.TurnHooks.FillBoxWithShadow(0, 97, 80, 103);

			if (troopIdx == 0x80)
			{
				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextEndOfTurn, 4, 124, Hues.Black);

				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextPressEnter, 4, 136, Hues.Black);

				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextToContinue, 4, 144, Hues.Black);
			}
			else
			{
				DrawUnitPanel(participantIdx, troopIdx);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort CheckPositionOnScreen(int horizLoc, int vertLoc)
		{
			using var _ = this.Owner.Journal.Scope($"VerifyPositionOnScreen({horizLoc}, {vertLoc})");
			ushort onScreen;
			if (horizLoc < 16 && (short)MapView.Horiz >= 65)
			{
				horizLoc += 80;
			}

			if (horizLoc < (short)MapView.Horiz ||
				horizLoc >= (short)MapView.Horiz + 15 ||
				vertLoc < (short)MapView.Vert ||
				vertLoc >= (short)MapView.Vert + 12)
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

			while (this.Owner.Libc.KeyAvailable() != 0)
			{
				this.Owner.Dialogs.ReadNavigationKey();
			}

			do
			{
				this.Owner.Startup.ReadMouseState();
			}
			while (this.Owner.GlobDb3a != 0);
		}

		public ushort FindBestGuardian(short participantIdx, ushort troopIdx)
		{
			using var _ = this.Processor.Journal.Scope("LocateBestGuardian()");

			ushort best = troopIdx;
			short current = this.troops.Following(participantIdx, troopIdx);

			if (current != -1)
			{
				short smallestStep = 999;

				for (int stepIdx = 0; stepIdx < 0x10; stepIdx++)
				{
					if (this.troops.KindField(participantIdx, current, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					{
						short step = (short)(current > (short)troopIdx ? current - troopIdx : current - troopIdx + 0x80);

						if (step < smallestStep)
						{
							smallestStep = step;
							best = (ushort)current;
						}
					}

					current = this.troops.Following(participantIdx, current);

					if (current == (short)troopIdx)
						break;
				}
			}

			return best;
		}

		public void ShowHelpForHuman(GameText topic)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayHelpForHuman(0x{topic.Address:x4})");

			if ((short)Turn.Player == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.HelpSystem.ShowHelpPopupForHuman(topic);
			}
		}

		private bool ContinueOrFinishUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (troopIdx < UnitMemory.TroopTally)
			{
				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0 && this.turn.Ordered == 0 &&
					Turn.GameEndRequest == 0)
				{
					return true;
				}

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
					return false;

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0)
				{
					this.turn.AllMoved = 0;
				}
				else
				{
					BurnPlaneFuel(participantIdx, troopIdx);
					MayLoseTrireme(participantIdx, troopIdx);
				}
			}

			if (this.Owner.World.IsHuman(participantIdx))
				DrawUnitInfo(participantIdx, this.turn.Troop);

			return false;
		}

		private void BurnPlaneFuel(short participantIdx, short troopIdx)
		{
			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) == 0)
				return;

			ushort tileImprovements = this.Owner.MapOps.ObtainTileImprovements(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((tileImprovements & 1) != 0 ||
				this.Owner.UnitOps.CountUnitsOfTypeInStack(participantIdx, (short)this.turn.Troop, 23) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));
			}

			byte rounds = (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) - 1);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, rounds);

			if ((sbyte)rounds < 0)
			{
				this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);
				ShowHelpForHuman(GameTexts.TopicFuel);
			}
		}

		private void MayLoseTrireme(short participantIdx, short troopIdx)
		{
			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10 || participantIdx != this.Owner.World.PersonParticipantIdx)
				return;

			if (this.Owner.Libc.Dice.DrawInt(2) == 0)
				return;

			this.turn.Found = 0;

			for (ushort direction = 1; direction <= 8; direction++)
			{
				this.turn.Count = direction;
				this.indexDest = UnitMemory.Offset(participantIdx, troopIdx);

				if (this.Owner.MapOps.ObtainTerrainType(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) + (short)Tables.DirectionHorizAt((ushort)(direction << 1)),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) + (short)Tables.DirectionVertAt((ushort)(direction << 1))) != MapRules.OceanTerrain)
				{
					this.turn.Found = 1;
					break;
				}

				if (direction == 8)
					this.turn.Count = 9;
			}

			if (this.turn.Found != 0)
				return;

			this.Owner.UnitOps.DestroyUnit(participantIdx, (short)this.turn.Troop);
			ShowHelpForHuman(GameTexts.TopicTrireme);
		}

		private void DrawUnitPanel(short participantIdx, ushort troop)
		{
			short troopIdx = (short)troop;
			short vertLoc = 0x63;

			this.Owner.Words.Set(Tables.CivilizationName(participantIdx));

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, 99, Hues.Black);

			vertLoc += 8;

			this.Owner.Words.Set(GameTables.UnitText(this.troops.TroopKind(participantIdx, troopIdx)));

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x20) != 0)
			{
				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextVeteran, 8, vertLoc, Hues.Black);

				vertLoc += 8;
			}

			sbyte steps = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps);
			short moves = (short)this.Owner.Helpers.ClampValue(steps / 3, 0, 99);

			Message("Moves: ")
				.Number(moves);

			short thirds = (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) % 3);

			if (thirds != 0)
			{
				this.Owner.Words.Append(".");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(thirds, 10));
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
			{
				this.Owner.Words.Append("(");

				moves += (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) *
					(short)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(moves, 10));

				this.Owner.Words.Append(")");
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			EmptyMessage()
				.City(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			Message("(")
				.Text(GameTables.TerrainText(this.Owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))))
				.Text(")");

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			byte improvements = (byte)this.Owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			foreach ((byte bit, byte otherBit, ushort text, ushort otherText) in new[] { ((byte)0x10, (byte)0x8, (ushort)0x20bd, (ushort)0x20c8),
				((byte)0x2, (byte)0x4, (ushort)0x20cf, (ushort)0x20dc), ((byte)0x40, (byte)0x0, (ushort)0x20e5, (ushort)0x0) })
			{
				ushort shown = (improvements & bit) != 0 ? text : (improvements & otherBit) != 0 ? otherText : (ushort)0;

				if (shown != 0)
				{
					this.Owner.TextPainter.DrawTextToScreen0(shown, 4, vertLoc, Hues.Black);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short followingIdx = this.troops.Following(participantIdx, troopIdx);
			short column = 8;

			if ((this.Owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) & 1) != 0)
			{
				ushort townIdx = this.Owner.Helpers.FindCityAtPosition(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				for (int unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
				{
					byte unfamiliar = (byte)this.Owner.World.Towns[townIdx].Unfamiliar[unfamiliarIdx];

					if (unfamiliar == 0xff)
						continue;

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, (byte)(unfamiliar & 0x3f));
					this.troops.Assign(participantIdx, 127, UnitMemory.Status, 0x8);
					this.troops.Assign(participantIdx, 127, UnitMemory.TravelHoriz, 0xff);

					this.Owner.MapOps.DrawUnitSprite(participantIdx, 127, column, vertLoc);

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, 0xff);

					column += 0x10;
				}
			}

			while (followingIdx != -1)
			{
				if (followingIdx == troopIdx || vertLoc >= 0xb8)
					break;

				this.Owner.MapOps.DrawUnitSprite(participantIdx, followingIdx, column, vertLoc);

				column += 0x10;

				if (column > 0x40)
				{
					column = 8;
					vertLoc += 0x10;
				}

				followingIdx = this.troops.Following(participantIdx, followingIdx);
			}

			if (followingIdx != -1 && followingIdx != troopIdx)
			{
				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextPlus, 74, 192, Hues.Black);
			}
		}
	}
}
