using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The end of a unit's move (fuel, triremes), the tiles and the unit panel</summary>
	partial class PlayerTurn
	{
		/// <summary>
		/// Repaints the tile when the human player has seen it (or everything is shown, 0xd806)
		/// </summary>
		public void RepaintTileIfVisible(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintTileIfVisible({horizLoc}, {vertLoc})");

			if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0 ||
				MapRevealed != 0)
			{
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.processor.Journal.CloseScope("RepaintTileIfVisible");
		}

		/// <summary>
		/// Repaints the tile of the unit when the human player sees it (own unit, or the unit is shown to the human)
		/// </summary>
		public void RepaintUnitIfVisible(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RepaintUnitIfVisible({participantIdx}, {troopIdx})");

			short human = this.owner.World.PersonParticipantIdx;

			if (participantIdx == human ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << human)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}

			this.processor.Journal.CloseScope("RepaintUnitIfVisible");
		}

		/// <summary>
		/// 0 on a tile with a city, irrigation or a mine (improvement bits 0 - 2). Otherwise from the terrain table at 0x44a
		/// (12 bytes a terrain): 2 when the word at +4 is -3 or less, 1 when the word at +0 is -2 and the tile is at the coast
		/// (VerifyCoastalTile), else 0.
		/// </summary>
		public ushort ObtainTileMoveCost(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTileMoveCost({horizLoc}, {vertLoc})");

			ushort cost = 0;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x7) == 0)
			{
				int terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				if ((short)TerrainImprovementWord(terrain, TerrainImprovementTable.Mining) <= -3)
				{
					cost = 2;
				}
				else if (TerrainImprovementWord(terrain, TerrainImprovementTable.Irrigation) == 0xfffe)
				{
					if (VerifyCoastalTile(horizLoc, vertLoc) != 0)
						cost = 1;
				}
			}


			this.processor.Journal.CloseScope("ObtainTileMoveCost");

			return cost;
		}

		/// <summary>
		/// Whether the tile is on the coast: it is terrain 11 itself, or one of the 4 tiles next to it in a straight line
		/// (headings 1, 3, 5, 7; the column is not wrapped) is terrain 10 or 11 or has improvement bit 1 - but tiles with a city
		/// do not count
		/// </summary>
		/// <returns>1 on the coast, else 0 (also in AX)</returns>
		public ushort VerifyCoastalTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyCoastalTile({horizLoc}, {vertLoc})");

			MapLayers chart = this.owner.MapOps.Chart;
			ushort coastal = 0;

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xb)
			{
				coastal = 1;
			}
			else
			{
				for (int heading = 1; heading <= 8 && coastal == 0; heading += 2)
				{
					short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
					short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

					ushort terrain = this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);
					ushort improvements = this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert);

					if ((improvements & 0x1) == 0 && (terrain == 0xb || terrain == 0xa || (improvements & 0x2) != 0))
					{
						coastal = 1;
					}
				}
			}


			this.processor.Journal.CloseScope("VerifyCoastalTile");

			return coastal;
		}

		/// <summary>
		/// The panel of the active unit (left, under the world map): the civilization, the type (Veteran), the moves (with the
		/// fuel of aircraft), the home city, the terrain and its improvements, then the units of the city on the tile (the two
		/// supported at 0x1c * city ... Unfamiliar, drawn through the unit 127) and the stack (up to row 0xb8, "more" after).
		/// Unit 0x80: no unit.
		/// </summary>
		public void RenderUnitInfo(short participantIdx, ushort troopIdx)
		{
			this.processor.Journal.OpenScope($"RenderUnitInfo({participantIdx}, {troopIdx})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);

			if (troopIdx == 0x80)
			{
				this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextEndOfTurn, 4, 124, Hues.Black);

				this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextPressEnter, 4, 136, Hues.Black);

				this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextToContinue, 4, 144, Hues.Black);
			}
			else
			{
				RenderUnitPanel(participantIdx, troopIdx);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("RenderUnitInfo");
		}

		/// <summary>1 when the place is inside the map view (15 x 12 tiles, also across the edge of the map), else 0</summary>
		public ushort VerifyPositionOnScreen(int horizLoc, int vertLoc)
		{
			this.owner.Journal.OpenScope($"VerifyPositionOnScreen({horizLoc}, {vertLoc})");
			ushort onScreen;
			if (horizLoc < 16 && (short)MapViewHoriz >= 65)
			{
				horizLoc += 80;
			}

			if (horizLoc < (short)MapViewHoriz ||
				horizLoc >= (short)MapViewHoriz + 15 ||
				vertLoc < (short)MapViewVert ||
				vertLoc >= (short)MapViewVert + 12)
			{
				onScreen = 0;
			}
			else
			{
				onScreen = 1;
			}

			this.owner.Journal.CloseScope("VerifyPositionOnScreen");

			return onScreen;
		}

		/// <summary>
		/// Throws away the keys waiting and waits until the mouse buttons are released
		/// </summary>
		public void AwaitForKey()
		{
			this.processor.Journal.OpenScope("AwaitForKey()");

			while (this.owner.Libc.KeyAvailable() != 0)
			{
				this.owner.Dialogs.ObtainNavigationKey();
			}

			do
			{
				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.GlobDb3a != 0);

			this.processor.Journal.CloseScope("AwaitForKey");
		}

		/// <summary>
		/// The next unit to activate in the stack of the unit: following the ring (at most 16 steps), the non-ship unit with the
		/// smallest index after the unit, counting round past 127
		/// </summary>
		/// <returns>That unit, the unit itself when it stands alone or nothing comes (also in AX)</returns>
		public ushort LocateBestGuardian(short participantIdx, ushort troopIdx)
		{
			this.processor.Journal.OpenScope("LocateBestGuardian()");

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


			this.processor.Journal.CloseScope("LocateBestGuardian");

			return best;
		}

		/// <summary>The help popup of the topic, only in the turn of the human player</summary>
		public void DisplayHelpForHuman(GameText topic)
		{
			this.processor.Journal.OpenScope($"DisplayHelpForHuman(0x{topic.Address:x4})");

			if ((short)TurnPlayer == this.owner.World.PersonParticipantIdx)
			{
				this.owner.HelpSystem.DisplayHelpPopupForHuman(topic);
			}
		
			this.processor.Journal.CloseScope("DisplayHelpForHuman");
		}

		/// <summary>
		/// After an order or a move: a unit with moves left goes on (true; unless 0x28 or the quit 0xdc48). Else its move
		/// ends (0x1e = 0 while it still had moves): a plane gets its fuel back in a city or on a carrier, burns one round and
		/// is lost without fuel; a trireme of the human may be lost (one in two) on a tile without land around. The panel of
		/// the unit is shown again for a human player (not for an emptied slot). The locals of HandlePlayerTurn and BX, SI, DI
		/// are used as in the original.
		/// </summary>
		private bool ContinueOrFinishUnit(short participantIdx)
		{
			short troopIdx = (short)this.turn.Troop;

			if (troopIdx < UnitMemory.TroopTally)
			{
				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) > 0 && this.turn.Ordered == 0 &&
					GameEndRequest == 0)
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

			if (this.owner.World.IsHuman(participantIdx))
				RenderUnitInfo(participantIdx, this.turn.Troop);

			return false;
		}

		/// <summary>
		/// A plane at the end of its move: in a city or on a carrier it gets all its rounds back, then it burns one; below
		/// zero it is lost (help 0x2070)
		/// </summary>
		private void BurnPlaneFuel(short participantIdx, short troopIdx)
		{
			ushort unit = UnitMemory.Offset(participantIdx, troopIdx);


			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) == 0)
				return;

			ushort tileImprovements = this.owner.MapOps.ObtainTileImprovements(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((tileImprovements & 1) != 0 ||
				this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, (short)this.turn.Troop, 23) != 0)
			{
				// In a city or on a carrier
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));
			}

			byte rounds = (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) - 1);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, rounds);

			if ((sbyte)rounds < 0)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);
				DisplayHelpForHuman(GameTexts.TopicFuel);
			}
		}

		/// <summary>
		/// A trireme of the human at the end of its move is lost (help 0x2076) one time in two when no tile around is land
		/// (0x3c the last direction looked at, 0x38 land found; SI and DI as in the original)
		/// </summary>
		private void MayLoseTrireme(short participantIdx, short troopIdx)
		{
			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x10 || participantIdx != this.owner.World.PersonParticipantIdx)
				return;

			if (this.owner.Libc.Dice.DrawInt(2) == 0)
				return;

			this.turn.Found = 0;

			for (ushort direction = 1; direction <= 8; direction++)
			{
				this.turn.Count = direction;
				this.indexDest = UnitMemory.Offset(participantIdx, troopIdx);

				if (this.owner.MapOps.ObtainTerrainType(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) + (short)DirectionHorizAt((ushort)(direction << 1)),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) + (short)DirectionVertAt((ushort)(direction << 1))) != MapRules.OceanTerrain)
				{
					this.turn.Found = 1;
					break;
				}

				if (direction == 8)
					this.turn.Count = 9;
			}

			if (this.turn.Found != 0)
				return;

			this.owner.UnitOps.EliminateUnit(participantIdx, (short)this.turn.Troop);
			DisplayHelpForHuman(GameTexts.TopicTrireme);
		}

		private void RenderUnitPanel(short participantIdx, ushort troop)
		{
			short troopIdx = (short)troop;
			short vertLoc = 0x63;

			this.owner.Words.Set(CivilizationName(participantIdx));

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, 99, Hues.Black);

			vertLoc += 8;

			this.owner.Words.Set(UnitText(this.troops.TroopKind(participantIdx, troopIdx)));

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x20) != 0)
			{
				this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextVeteran, 8, vertLoc, Hues.Black);

				vertLoc += 8;
			}

			sbyte steps = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps);
			short moves = (short)this.owner.Helpers.VerifyValueSpan(steps / 3, 0, 99);

			Message("Moves: ")
				.Number(moves);

			short thirds = (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) % 3);

			if (thirds != 0)
			{
				this.owner.Words.Append(".");

				this.owner.Words.Append(this.owner.Libc.IntegerToText(thirds, 10));
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
			{
				// Aircraft: the moves left with the fuel of the next turns
				this.owner.Words.Append("(");

				moves += (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) *
					(short)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));

				this.owner.Words.Append(this.owner.Libc.IntegerToText(moves, 10));

				this.owner.Words.Append(")");
			}

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			EmptyMessage()
				.City(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			Message("(")
				.Text(TerrainText(this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))))
				.Text(")");

			this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, vertLoc, Hues.Black);

			vertLoc += 8;

			byte improvements = (byte)this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			// Railroad or road, irrigation or mines, pollution
			foreach ((byte bit, byte otherBit, ushort text, ushort otherText) in new[] { ((byte)0x10, (byte)0x8, (ushort)0x20bd, (ushort)0x20c8),
				((byte)0x2, (byte)0x4, (ushort)0x20cf, (ushort)0x20dc), ((byte)0x40, (byte)0x0, (ushort)0x20e5, (ushort)0x0) })
			{
				ushort shown = (improvements & bit) != 0 ? text : (improvements & otherBit) != 0 ? otherText : (ushort)0;

				if (shown != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(shown, 4, vertLoc, Hues.Black);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short followingIdx = this.troops.Following(participantIdx, troopIdx);
			short column = 8;

			if ((this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				// The units the city supports elsewhere are drawn through the unit 127 of the player
				for (int unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
				{
					byte unfamiliar = (byte)this.owner.World.Towns[townIdx].Unfamiliar[unfamiliarIdx];

					if (unfamiliar == 0xff)
						continue;

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, (byte)(unfamiliar & 0x3f));
					this.troops.Assign(participantIdx, 127, UnitMemory.Status, 0x8);
					this.troops.Assign(participantIdx, 127, UnitMemory.TravelHoriz, 0xff);

					this.owner.MapOps.RenderUnitSprite(participantIdx, 127, column, vertLoc);

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, 0xff);

					column += 0x10;
				}
			}

			while (followingIdx != -1)
			{
				if (followingIdx == troopIdx || vertLoc >= 0xb8)
					break;

				this.owner.MapOps.RenderUnitSprite(participantIdx, followingIdx, column, vertLoc);

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
				this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextPlus, 74, 192, Hues.Black);
			}
		}
	}
}
