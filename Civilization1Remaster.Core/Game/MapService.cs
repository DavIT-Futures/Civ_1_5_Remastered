using System;

namespace Civilization1Remaster.Core.Game
{
	public class MapService : GameComponent, IMapService
	{
		private const ushort CityLabelHorizAddress = 0x6dac;

		private const ushort CityLabelVertAddress = 0x6e3e;

		private const ushort CityLabelTownAddress = 0xdf20;

		private const ushort VitalityStripBackdrop = 0;

		private const ushort VitalityStripComplete = 10;
		private const ushort VitalityStripBlank = 8;
		private const int VitalityStripCellBreadth = 2;
		private const int VitalityStripCellAltitude = 1;

		private readonly MapPlanes chart;
		private readonly UnitMemory troops;

		public MapService(GameEngine owner)
			: base(owner)
		{
			this.chart = new MapPlanes(owner);
			this.troops = new UnitMemory(owner.Processor);
		}

		public IMapPlanes Chart
		{
			get { return this.chart; }
		}

		public void FocusMapView(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"FocusMapView({participantIdx}, {horizLoc}, {vertLoc})");

			this.Owner.Startup.HideMouseCursor();

			Screen.RepaintRequest = 0x0;

			this.Owner.TurnHooks.ClearStatusLine();

			this.Owner.TurnHooks.DrawStatusBar();

			ushort kept = MapView.UnitAtTilePlayer;

			MapView.Horiz = (ushort)this.Owner.Routing.TunePosition(horizLoc);
			MapView.Vert = (ushort)this.Owner.Helpers.ClampValue(vertLoc, 0, 38);

			ushort chartSightFilter = (ushort)(1 << (byte)participantIdx);

			MapView.CityLabelCount = 0x0;

			ushort order = (ushort)this.Owner.Libc.Dice.DrawInt(256);

			for (int tileIdx = 0; tileIdx < 0x100; tileIdx++)
			{
				short column = (short)(order & 0xf);
				short row = (short)((short)order >> 4);

				if (row < 0xc && column < 0xf)
				{
					bool shown = MapView.Revealed != 0;

					if (!shown)
					{
						ushort tileHoriz = (ushort)this.Owner.Routing.TunePosition(column + (short)MapView.Horiz);
						ushort tileVert = (ushort)(row + MapView.Vert);

						shown = (this.Owner.World.ChartSight[tileHoriz, tileVert] & chartSightFilter) != 0;
					}

					if (shown)
					{
						RepaintMapTile((short)this.Owner.Routing.TunePosition(column + (short)MapView.Horiz),
							row + (short)MapView.Vert);
					}
					else
					{
						this.Owner.Services.FloodBox(Screen.Box, (column * 16) + 80, (row * 16) + 8, 16, 16, Hues.Black);
					}
				}

				order = (byte)((order * 5) + 1);
			}

			for (int labelIdx = 0; labelIdx < (short)MapView.CityLabelCount; labelIdx++)
			{
				ushort place = (ushort)(labelIdx << 1);

				if (this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(place + CityLabelVertAddress)) >= 0xb8)
				{
					continue;
				}

				EmptyMessage()
					.City(this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(place + CityLabelTownAddress)));

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, (ushort)(327 - this.Processor.PullGlobalWord((ushort)(place + CityLabelHorizAddress))));

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(),
					this.Owner.Helpers.ClampValue(this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(place + CityLabelHorizAddress)) - 8, 80, 999),
					this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(place + CityLabelVertAddress)) + 16, Hues.LightCyan);
			}

			short left = (short)(horizLoc - 0x20);
			short top = (short)(vertLoc - 0x13);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 8) != 0)
			{
				top = 0;
			}

			ushort wrappedLeft = (ushort)(left >= 0 ? left : left + 0x50);
			ushort breadth = (ushort)((short)wrappedLeft > 0 ? 0x50 - wrappedLeft : 0x50);
			ushort sourceTop = (ushort)(top >= 0 ? top : 0);
			ushort targetTop = (ushort)(sourceTop - top);
			ushort altitude = (ushort)(0x32 - targetTop - (top > 0 ? top : 0));

			MapView.FocusLeft = (ushort)left;
			MapView.FocusTop = (ushort)top;

			this.Owner.Services.FloodBox(Screen.Box, 0, 8, 80, 50, Hues.Black);

			if (MapView.Revealed != 0)
			{
				left = (short)this.Owner.Helpers.ClampValue(left, 0, 16);
				MapView.FocusLeft = (ushort)left;

				top = (short)this.Owner.Helpers.ClampValue(top, 0, 65530);
				MapView.FocusTop = (ushort)top;

				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)left, (ushort)top, 0x50, 0x32,
					Screen.Box, 0, 8);
			}
			else
			{
				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)(wrappedLeft + 0xf0), sourceTop, breadth, altitude,
					Screen.Box, 0, (ushort)(targetTop + 8));

				if (breadth < 0x50)
				{
					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, 0xf0, sourceTop, (ushort)(0x50 - breadth), altitude,
						Screen.Box, breadth, (ushort)(targetTop + 8));
				}
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || (town.ShownExtent == 0 && town.ParticipantIdx != this.Owner.World.PersonParticipantIdx))
				{
					continue;
				}

				short dotHoriz = (short)this.Owner.Routing.TunePosition(town.Place.X - (short)wrappedLeft);
				short dotVert = (short)(town.Place.Y - sourceTop + targetTop);

				if (dotVert < 0 || dotVert >= 0x32)
				{
					continue;
				}

				this.Owner.Services.AssignDot(dotHoriz, dotVert + 8, Tables.PlayerColour(town.ParticipantIdx));
			}

			this.Owner.Dialogs.DrawShadowBox(0, 8, 79, 49, Hues.White, Hues.DarkGray);

			this.Owner.Dialogs.DrawBox(horizLoc - left - 1, vertLoc - top + 7, 17, 10, Hues.White);

			MapView.UnitAtTilePlayer = kept;

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort DrawMapTile(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RenderMapTile({horizLoc}, {vertLoc})");

			if (MapView.WorldMapView == 0x1)
			{
				ushort outcome = DrawWorldMapTileBlock(horizLoc, vertLoc);
				return FinishRenderMapTile(outcome);
			}

			short screenX = (short)((this.Owner.Routing.TunePosition(horizLoc - (short)MapView.Horiz) << 4) + 0x50);

			short screenY = (short)(((vertLoc - (short)MapView.Vert) << 4) + 0x8);

			if (screenX < 0x50 || screenX >= 0x140 || screenY < 0x8 || screenY > 0xc0)
			{
				return FinishRenderMapTile(0);
			}

			ushort terrain = ObtainTerrainType(horizLoc, vertLoc);

			ushort improvements = ObtainKnownTileImprovements(horizLoc, vertLoc);

			if (MapView.Revealed != 0)
			{
				improvements = ObtainTileImprovements(horizLoc, vertLoc);
			}

			this.Owner.Startup.HideMouseCursor();

			if (terrain == 0xa)
			{
				DrawOceanTile(horizLoc, vertLoc, screenX, screenY);
			}

			if (terrain != 0xa)
			{
				if (Screen.VgaMode == 0)
				{
					this.Owner.VideoCard.RenderImage(
						Screen.SpriteScreenBox,
						0, 0x50, 16, 16,
						Screen.Box,
						(ushort)screenX, (ushort)screenY);
				}
				else
				{
					this.Owner.VideoCard.RenderImage(
						Screen.PictureScreenBox,
						0x100, 0x78, 16, 16,
						Screen.Box,
						(ushort)screenX, (ushort)screenY);
				}
			}

			if ((improvements & 0x2) != 0 &&
				terrain != 0xa &&
				MapView.UnitsHidden == 0 &&
				(improvements & 0x1) == 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					Tables.SpritePicture(4));
			}

			if (terrain == 0xb)
			{
				DrawRiverTile(horizLoc, vertLoc, screenX, screenY);
			}
			else if (terrain != 0xa)
			{
				DrawLandTerrainOverlay(horizLoc, vertLoc, screenX, screenY, terrain);
			}

			DrawTileImprovementsAndFeatures(horizLoc, vertLoc, screenX, screenY, terrain, improvements);

			ushort ownerBits = ObtainTileOwnerBits(horizLoc, vertLoc);

			if ((improvements & 0x1) != 0 && MapView.UnitsHidden == 0)
			{
				DrawCityOnTile(horizLoc, vertLoc, screenX, screenY);
			}

			this.Owner.Startup.ShowMouseCursor();

			return FinishRenderMapTile(1);
		}

		public ushort DrawUnitOnWorldMap(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitOnWorldMap({participantIdx}, {troopIdx})");

			sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (MapView.WorldMapView == 1)
			{
				this.Processor.RegAcc.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);

				this.Owner.Services.FloodBox(Screen.Box, (horizLoc * 4) + 1, (vertLoc * 4) + 1, 3, 3, (ushort)(kind == 0 ? 6 : 0));
				this.Owner.Services.FloodBox(Screen.Box, horizLoc * 4, vertLoc * 4, 3, 3,
					Tables.PlayerColour(participantIdx));

				return this.Processor.RegAcc.Value16;
			}

			short viewHoriz = (short)((this.Owner.Routing.TunePosition(horizLoc - (short)MapView.Horiz) << 4) + 0x50);
			short viewVert = (short)((((byte)vertLoc - MapView.Vert) << 4) + 8);
			ushort drawn = 0;

			if (viewHoriz >= 0x50 && viewHoriz < 0x140 && viewVert >= 8 && viewVert <= 0xc0 &&
				(ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
				(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 1) == 0 ||
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
			{
				this.Owner.Startup.HideMouseCursor();

				if (this.troops.Following(participantIdx, troopIdx) != -1)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, viewHoriz + 1, viewVert + 1,
						Tables.SpritePicture((participantIdx << 5) + kind + 0x40));
				}

				DrawUnitSprite(participantIdx, troopIdx, viewHoriz, viewVert);

				this.Owner.Startup.ShowMouseCursor();

				drawn = 1;
			}

			this.Processor.RegAcc.Value16 = drawn;

			return drawn;
		}

		public void DrawHealthBar(int horizLoc, int vertLoc, int completeStrikeMarks, int strikeMarks)
		{
			ushort display = Screen.Box;

			int breadth = Math.Min((completeStrikeMarks * (VitalityStripCellBreadth + 1)) + 1, 15);
			int remaining = horizLoc + 1 + ((15 - breadth) / 2);
			int head = vertLoc + 1;

			this.Owner.Services.FloodBox(display, remaining, head, breadth, VitalityStripCellAltitude + 2, VitalityStripBackdrop);

			for (int idx = 0; idx < completeStrikeMarks; idx++)
			{
				this.Owner.Services.FloodBox(display, remaining + 1 + (idx * (VitalityStripCellBreadth + 1)), head + 1,
					VitalityStripCellBreadth, VitalityStripCellAltitude, idx < strikeMarks ? VitalityStripComplete : VitalityStripBlank);
			}
		}

		public void DrawUnitSprite(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitSprite({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);
			ushort markHue = (ushort)(participantIdx == 1 ? 9 : 15);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, horizLoc, vertLoc,
				Tables.SpritePicture(kind + (participantIdx << 5) + 0x40));

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 8) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, horizLoc, vertLoc, Tables.SpritePicture(0x1d));
			}
			else if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 4) != 0)
			{
				this.Owner.TextPainter.DrawTextWithShadow(GameTexts.TextF, horizLoc + 4, vertLoc + 7, markHue);
			}

			if (participantIdx == this.Owner.World.PersonParticipantIdx && this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				this.Owner.TextPainter.DrawTextWithShadow(GameTexts.TextG, horizLoc + 4, vertLoc + 7, markHue);
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0 &&
				Tables.UnitKindWord((sbyte)kind, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
				byte letter = 0x52;

				if ((status & 0x40) != 0)
				{
					letter = (byte)(kind == 0 ? 0x49 : 0x3f);
				}

				if ((status & 0x80) != 0)
				{
					letter = (byte)((status & 0x40) != 0 ? 0x46 : 0x4d);

					if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 2) != 0)
					{
						letter = 0x50;
					}
				}

				this.Owner.Words.Set(" ");
				this.Processor.EmitGlobalByte(TextBuffer.Address, letter);

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), horizLoc + 4, vertLoc + 7, markHue);

				this.Owner.Dialogs.DrawBox(horizLoc - 1, vertLoc - 1, 15, 15, Hues.LightGray);
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 1) != 0)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizLoc, vertLoc, 16, 16, Hues.Magenta, Hues.LightGray);

				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizLoc, vertLoc, 16, 16, Hues.DarkGray, Hues.LightGray);
			}

			{
				int troopKind = this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);
				int missingStrikeMarks = this.Owner.World.Vitals.ObtainLostHitPoints(participantIdx, troopIdx, troopKind);

				if (troopKind != 0xff)
				{
					bool seasoned = (this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0;
					int completeStrikeMarks = this.Owner.World.Vitals.ObtainFullHitPoints(participantIdx, troopIdx, troopKind, seasoned);

					DrawHealthBar(horizLoc - 1, vertLoc - 1, completeStrikeMarks, completeStrikeMarks - missingStrikeMarks);
				}
			}
		}

		public void RepaintMapTile(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintMapTile({horizLoc}, {vertLoc})");

			DrawMapTile(horizLoc, vertLoc);

			if (MapView.UnitsHidden == 0)
			{
				ushort troopIdx = ObtainUnitAtTile(horizLoc, vertLoc);

				if (troopIdx != 0xffff)
				{
					short ownerIdx = (short)MapView.UnitAtTilePlayer;
					short personIdx = this.Owner.World.PersonParticipantIdx;

					bool seen = MapView.Revealed != 0 || ownerIdx == personIdx ||
						(this.troops.Pull(ownerIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << personIdx)) != 0;

					if (seen && (ObtainTileImprovements(horizLoc, vertLoc) & 0x1) == 0)
					{
						RepaintUnitTile((short)MapView.UnitAtTilePlayer, (short)troopIdx);
					}
				}
			}
		}

		public void RepaintUnitTile(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintUnitTile({participantIdx}, {troopIdx})");

			ushort terrain = ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if (terrain == 0xa && this.troops.Following(participantIdx, troopIdx) != -1 &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
			{
				short current = troopIdx;

				do
				{
					current = this.troops.Following(participantIdx, current);

					if (this.troops.KindField(participantIdx, current, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
					{
						DrawUnitOnWorldMap(participantIdx, current);
						current = -1;
					}
				}
				while (current != -1 && current != troopIdx);

				if (current == troopIdx)
				{
					DrawUnitOnWorldMap(participantIdx, troopIdx);
				}
			}
			else
			{
				DrawUnitOnWorldMap(participantIdx, (short)this.Owner.UnitOps.FindBestGuardianInStack(participantIdx, troopIdx));
			}
		}

		public ushort CheckMapLimits(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyMapLimits({horizLoc}, {vertLoc})");

			ushort onMap = ReturnAcc(MapRules.IsOnMap(horizLoc, vertLoc) ? 1 : 0);

			return onMap;
		}

		public ushort ObtainTerrainType(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTerrainType({horizLoc}, {vertLoc})");

			ushort terrain = ReturnAcc(this.chart.Terrain(horizLoc, vertLoc));

			return terrain;
		}

		public ushort ObtainTileOwnerBits(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc) & 0x7);
		}

		public void MapAssignCityOwner(short participantIdx, int horizLoc, int vertLoc)
		{
			int bits = ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc));

			this.chart.AssignOwnerBits(horizLoc, vertLoc, (bits & MapPlanes.UnitsPresent) + participantIdx);
		}

		public void PlaceUnitOnTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"SituateUnitOnTile({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			ushort stackTroopIdx = ObtainUnitAtTile(horizLoc, vertLoc);

			if (stackTroopIdx != 0xffff)
			{
				this.Owner.Combat.AttachUnitToStack(participantIdx, troopIdx, stackTroopIdx);
			}

			AssignChartDot(horizLoc + 160, vertLoc, (ushort)(participantIdx + MapPlanes.UnitsPresent));
		}

		public void RemoveUnitFromTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"DiscardUnitFromTile({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.Owner.Combat.RemoveUnitFromStack(participantIdx, troopIdx);
			}
			else
			{
				AssignChartDot(horizLoc + 160, vertLoc, (byte)participantIdx);
			}
		}

		public ushort ObtainUnitAtTile(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainUnitAtTile({horizLoc}, {vertLoc})");

			ushort found = 0xffff;
			ushort participantIdx = ObtainTileOwner(horizLoc, vertLoc);

			if (participantIdx != 0xffff)
			{
				for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (this.troops.Exists(participantIdx, troopIdx) && this.troops.StandsAt(participantIdx, troopIdx, horizLoc, vertLoc))
					{
						found = (ushort)troopIdx;
						break;
					}
				}

				if (found != 0xffff)
				{
					Search.FoundPlayer = participantIdx;
					MapView.UnitAtTilePlayer = participantIdx;
				}
				else
				{
					this.Owner.Services.AssignDot(2, horizLoc + 160, vertLoc, 0);
				}
			}

			ReturnAcc(found);

			return found;
		}

		public ushort ObtainTileOwner(int horizLoc, int vertLoc)
		{
			int bits = this.chart.OwnerBits(horizLoc, vertLoc);

			return ReturnAcc((bits & MapPlanes.UnitsPresent) != 0 ? bits & 0x7 : 0xffff);
		}

		public void TagTileOwned(int horizLoc, int vertLoc)
		{
			int bits = ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc));

			this.chart.AssignOwnerBits(horizLoc, vertLoc, (bits & 0x7) + MapPlanes.UnitsPresent);
		}

		public ushort ContainsRoad(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Improvements(horizLoc, vertLoc) & 0x8);
		}

		public ushort ObtainTileImprovements(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Improvements(horizLoc, vertLoc));
		}

		public ushort ObtainKnownTileImprovements(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainKnownTileImprovements({horizLoc}, {vertLoc})");

			ushort known = ReturnAcc(this.chart.KnownImprovements(horizLoc, vertLoc));

			return known;
		}

		public void KeepKnownTileImprovements(int horizLoc, int vertLoc)
		{
			this.chart.RevealImprovements(horizLoc, vertLoc);

			ReturnAcc(this.chart.Improvements(horizLoc, vertLoc) >> 4);
		}

		public void AssignTileImprovements(ushort improvements, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"AssignTileImprovements({improvements}, {horizLoc}, {vertLoc})");

			if (improvements == 0)
			{
				AssignChartDot(horizLoc, vertLoc + 100, 0);
				AssignChartDot(horizLoc, vertLoc + 150, 0);
			}
			else if (improvements >= 0x10)
			{
				int high = ReturnAcc(this.chart.HighImprovements(horizLoc, vertLoc));
				AssignChartDot(horizLoc, vertLoc + 150, (ushort)((improvements >> 4) | high));
			}
			else
			{
				int low = ReturnAcc(this.chart.LowImprovements(horizLoc, vertLoc));
				AssignChartDot(horizLoc, vertLoc + 100, (ushort)(low | improvements));
			}

			if ((short)Turn.Player == this.Owner.World.PersonParticipantIdx)
			{
				KeepKnownTileImprovements(horizLoc, vertLoc);
			}
		}

		public void RemoveTileImprovements(ushort improvements, int horizLoc, int vertLoc)
		{
			this.chart.ClearImprovements(improvements, horizLoc, vertLoc);
		}

		public ushort FindCityAtTile(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"LocateCityAtTile({horizLoc}, {vertLoc})");

			ushort participantIdx = ObtainTileOwnerBits(horizLoc, vertLoc);
			ushort found = 0xffff;

			for (int townIdx = 0; townIdx < this.Owner.World.Towns.Length; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.Place.X == horizLoc && town.Place.Y == vertLoc)
				{
					Search.FoundPlayer = participantIdx;
					MapView.UnitAtTilePlayer = participantIdx;
					found = (ushort)townIdx;
					break;
				}
			}

			if (found == 0xffff)
			{
				this.Owner.TurnHooks.ShowDialog(GameTexts.TextCityAtError, 100, 80);
			}

			ReturnAcc(found);

			return found;
		}

		public ushort ContainsSpecialResource(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ContainsSpecialResource({horizLoc}, {vertLoc})");

			ushort special = ReturnAcc(MapRules.HasSpecialResource(horizLoc, vertLoc, this.Owner.World.ChanceRoot) ? 1 : 0);

			return special;
		}

		public ushort ContainsHut(ushort terrain, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ContainsHut({terrain}, {horizLoc}, {vertLoc})");

			bool hut = terrain != MapRules.OceanTerrain &&
				(this.Owner.World.ChartSight[horizLoc, vertLoc] & 1) == 0 &&
				MapRules.IsHutPlace(horizLoc, vertLoc, this.Owner.World.ChanceRoot) &&
				(ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			ushort result = ReturnAcc(hut ? 1 : 0);

			return result;
		}

		public ushort ObtainContinentID(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Continent(horizLoc, vertLoc));
		}

		public ushort ObtainLandMassSize(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainLandMassSize({horizLoc}, {vertLoc})");

			ushort sizes = ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ? (ushort)0xdcfe : (ushort)0xdd7e;
			int continent = ObtainContinentID(horizLoc, vertLoc);
			ushort size = ReturnAcc(this.Processor.PullGlobalWord((ushort)(sizes + (continent << 1))));

			return size;
		}

		private ushort ReturnAcc(int amount)
		{
			ushort result = (ushort)amount;
			this.Processor.RegAcc.Value16 = result;

			return result;
		}

		private ushort FinishRenderMapTile(ushort result)
		{
			this.Processor.RegAcc.Value16 = result;
			return result;
		}

		private ushort DrawWorldMapTileBlock(int horizLoc, int vertLoc)
		{
			ushort imp = ObtainTileImprovements(horizLoc, vertLoc);
			if ((imp & 0x1) != 0)
			{
				ushort ownerBits = ObtainTileOwnerBits(horizLoc, vertLoc);
				this.Owner.Services.FloodBox(
					Screen.Box,
					horizLoc * 4, vertLoc * 4, 4, 4,
					Tables.PlayerColour(ownerBits));
				return ownerBits;
			}

			ushort terrain = ObtainTerrainType(horizLoc, vertLoc);
			if (terrain == 0xa)
			{
				this.Owner.Services.FloodBox(Screen.Box, horizLoc * 4, vertLoc * 4, 4, 4, Hues.Blue);
				return 0xa;
			}

			this.Owner.Services.FloodBox(Screen.Box, horizLoc * 4, vertLoc * 4, 4, 4, Hues.Green);
			return 0x2;
		}

		private ushort MaskNeighbours(int horizLoc, int vertLoc, ushort mask, ushort step, ushort bit, Func<ushort, bool> fits)
		{
			for (ushort dir = 1; dir < 9; dir += step)
			{
				mask = (ushort)((short)mask >> 1);

				ushort adjX = (ushort)this.Owner.Routing.TunePosition(
					(short)Tables.DirectionHoriz(dir) + horizLoc);
				int adjY = (short)Tables.DirectionVert(dir) + vertLoc;

				if (fits(ObtainTerrainType((short)adjX, adjY)) && CheckMapLimits(0, adjY) != 0)
				{
					mask |= bit;
				}
			}

			return mask;
		}

		private void DrawOceanTile(int horizLoc, int vertLoc, short screenX, short screenY)
		{
			ushort mask = 0;

			if (Screen.VgaMode == 0)
			{
				mask = MaskNeighbours(horizLoc, vertLoc, mask, 2, 0x8, terrainType => terrainType != 0xa);

				this.Owner.VideoCard.RenderImage(
					Screen.SpriteScreenBox,
					(ushort)(mask << 4), 0x40, 16, 16,
					Screen.Box,
					(ushort)screenX, (ushort)screenY);
				return;
			}

			mask = MaskNeighbours(horizLoc, vertLoc, mask, 1, 0x80, terrainType => terrainType != 0xa);
			ushort rawMask = mask;

			ushort rotatedMask = (ushort)((((short)mask >> 6) & 0x3) + (mask << 2));

			for (ushort quad = 0; quad < 4; quad++)
			{
				ushort piece = (ushort)(((short)rotatedMask >> ((byte)quad << 1)) & 0x7);
				ushort sprite = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((quad << 1) + (piece << 3) + 0xd294));

				short qx = quad >= 2
					? (short)(screenX - ((quad & 1) << 3) + 8)
					: (short)(screenX + ((quad & 1) << 3));
				short qy = quad >= 2 ? (short)(screenY + 8) : screenY;

				this.Owner.Services.DrawPictureToScreen(Screen.Box, qx, qy, sprite);
			}

			int specialCoastX = rawMask switch
			{
				0x1c => 0xe0,
				0xc1 => 0xf0,
				0x07 => 0x100,
				0x70 => 0x110,
				0x8f => 0x120,
				0xf8 => 0x130,
				_ => -1,
			};

			if (specialCoastX >= 0)
			{
				this.Owner.VideoCard.RenderImage(
					Screen.PictureScreenBox,
					(ushort)specialCoastX, 0x64, 16, 16,
					Screen.Box,
					(ushort)screenX, (ushort)screenY);
			}

			for (ushort dir = 1; dir < 9; dir += 2)
			{
				ushort adjX = (ushort)this.Owner.Routing.TunePosition(
					(short)Tables.DirectionHoriz(dir) + horizLoc);
				int adjY = (short)Tables.DirectionVert(dir) + vertLoc;

				if (ObtainTerrainType((short)adjX, adjY) == 0xb)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						screenX, screenY,
						this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(((dir / 2) << 1) + 0xd2d4)));
				}
			}
		}

		private void DrawRiverTile(int horizLoc, int vertLoc, short screenX, short screenY)
		{
			ushort mask = 0;

			for (ushort dir = 1; dir < 9; dir += 2)
			{
				mask = (ushort)((short)mask >> 1);

				ushort adjTerrain = ObtainTerrainType(
					(short)Tables.DirectionHoriz(dir) + horizLoc,
					(short)Tables.DirectionVert(dir) + vertLoc);

				if (adjTerrain == 0xb || adjTerrain == 0xa)
				{
					mask |= 0x8;
				}
			}

			if (Screen.VgaMode == 0 ||
				(this.Owner.VideoCard.ObtainDot(2, horizLoc, vertLoc + 150) & 0x8) != 0)
			{
				mask = (ushort)(mask + 0x10);
			}

			this.Owner.Services.DrawPictureToScreen(
				Screen.Box,
				screenX, screenY,
				this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((mask << 1) + 0x6dfe)));
		}

		private void DrawLandTerrainOverlay(int horizLoc, int vertLoc, short screenX, short screenY, ushort terrain)
		{
			if (Screen.VgaMode != 0)
			{
				ushort mask = 0;

				mask = MaskNeighbours(horizLoc, vertLoc, mask, 2, 0x8, terrainType => terrainType == terrain);

				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((mask << 1) + (terrain << 5) + 0xb886)));

				if (terrain == 0x2 && (((byte)((7 * horizLoc) + (11 * vertLoc))) & 0x2) == 0)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						(short)(screenX + 4),
						(short)(screenY + 4),
						Icons.MiniSymbolPicture);
				}
			}
			else
			{
				ushort variant = terrain == 0x2
					? (ushort)((((byte)((11 * vertLoc) + (7 * horizLoc))) & 0x2) >> 1)
					: (ushort)((horizLoc + vertLoc) & 0x1);

				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((terrain << 5) + (variant << 1) + 0xb886)));
			}
		}

		private void DrawTileImprovementsAndFeatures(int horizLoc, int vertLoc, short screenX, short screenY, ushort terrain, ushort improvements)
		{
			if ((improvements & 0x40) != 0)
			{
				if (Screen.VgaMode != 0)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						screenX, screenY,
						Icons.TileFortressPicture);
				}
				else
				{
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenX, screenY, 16, 16, Hues.Green, Hues.Black);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenX, screenY, 16, 16, Hues.LightGreen, Hues.White);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenX, screenY, 16, 16, Hues.LightBlue, Hues.DarkGray);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenX, screenY, 16, 16, Hues.LightCyan, Hues.Black);
				}
			}

			if ((improvements & 0x8) != 0)
			{
				ushort isolatedDotHue = (improvements & 0x10) == 0 ? (ushort)0x6 : (ushort)0;

				for (ushort dir = 1; dir <= 8; dir++)
				{
					ushort adjImp = ObtainKnownTileImprovements(
						(short)Tables.DirectionHoriz(dir) + horizLoc,
						(short)Tables.DirectionVert(dir) + vertLoc);

					if ((adjImp & 0x8) != 0)
					{
						isolatedDotHue = 0xffff;

						ushort roadTable = ((improvements & 0x11) != 0 && (adjImp & 0x11) != 0) ? (ushort)0xb298 : (ushort)0xb278;
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							screenX, screenY,
							this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((dir << 1) + roadTable)));
					}
				}

				if (isolatedDotHue != 0xffff)
				{
					this.Owner.Services.FloodBox(
						Screen.Box,
						screenX + 7, screenY + 7, 2, 2,
						isolatedDotHue);
				}
			}

			if ((improvements & 0x4) != 0 && MapView.UnitsHidden == 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					Tables.SpritePicture(5));
			}

			if (ContainsSpecialResource(horizLoc, vertLoc) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					Tables.SpritePicture(terrain + 0x10));
			}

			if (ContainsHut(terrain, horizLoc, vertLoc) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					Tables.SpritePicture(0x1f));
			}

			if ((improvements & 0x20) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX, screenY,
					Tables.SpritePicture(0x1e));
			}

			if (MapView.Revealed == 0)
			{
				ushort personBit = (ushort)(1 << (this.Owner.World.PersonParticipantIdx & 0xff));

				for (ushort dir = 1; dir <= 8; dir += 2)
				{
					ushort adjX = (ushort)this.Owner.Routing.TunePosition(
						(short)Tables.DirectionHoriz(dir) + horizLoc);
					ushort adjY = (ushort)(Tables.DirectionVert(dir) + (ushort)vertLoc);

					if (((ushort)(short)this.Owner.World.ChartSight[adjX, adjY] & personBit) == 0)
					{
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							screenX, screenY,
							this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(((dir / 2) << 1) + 0x7eec)));
					}
				}
			}
		}

		private void DrawCityOnTile(int horizLoc, int vertLoc, short screenX, short screenY)
		{
			ushort cityIdx = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);

			City town = this.Owner.World.Towns[cityIdx];
			if (town.ParticipantIdx != this.Owner.World.PersonParticipantIdx &&
				town.ShownExtent == 0 &&
				MapView.Revealed == 0)
			{
				return;
			}

			this.Owner.Dialogs.DrawBox(screenX + 1, screenY + 1, 13, 13, Hues.White);

			short innerX = (short)(screenX + 2);

			ushort ownerWordOffset = (ushort)(town.ParticipantIdx << 1);
			this.Owner.Dialogs.DrawBox(
				screenX + 2, screenY + 1, 12, 12,
				this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(ownerWordOffset + 0x1956)));

			short innerY = (short)(screenY + 2);

			this.Owner.Services.FloodBox(
				Screen.Box,
				innerX, innerY, 12, 12,
				Tables.PlayerColourAt(ownerWordOffset));

			this.Owner.Services.DrawPictureToScreen(
				Screen.Box,
				(short)(screenX + 1),
				(short)(screenY + 1),
				Tables.SpritePicture(0x1c));

			this.Owner.VideoCard.SubstituteHue(
				Screen.Box,
				innerX, innerY, 12, 12, Hues.Magenta,
				(byte)this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(ownerWordOffset + 0x1956)));

			short displayedSize = (town.ParticipantIdx == this.Owner.World.PersonParticipantIdx || MapView.Revealed != 0)
				? (short)town.TrueExtent
				: (short)town.ShownExtent;

			EmptyMessage()
				.Number(displayedSize);

			if ((town.ConditionMarker & 0x1) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX + 5, innerY,
					Icons.UnhappyCitizenPicture);
			}
			else
			{
				this.Owner.TextPainter.DrawTextToScreen0(
					this.Owner.Words.ToString(),
					(displayedSize < 10 ? 6 : 3) + screenX,
					screenY + 5,
					Hues.Black);
			}

			if (ObtainUnitAtTile(horizLoc, vertLoc) != 0xffff || (byte)town.Unfamiliar[0] != 0xff)
			{
				this.Owner.Dialogs.DrawBox(screenX, screenY, 15, 15, Hues.Black);
			}

			if (town.Has(CityStructure.CityWalls))
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenX + 1, screenY + 1,
					Tables.SpritePicture(0x1d));
			}

			ushort labelIdx = MapView.CityLabelCount;
			if ((short)labelIdx < 0x20)
			{
				ushort slot = (ushort)(labelIdx << 1);
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(slot + CityLabelHorizAddress), (ushort)screenX);
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(slot + CityLabelVertAddress), (ushort)screenY);
				MapView.CityLabelCount = (ushort)(labelIdx + 1);
				this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(slot + CityLabelTownAddress), cityIdx);
			}
		}

		private void AssignChartDot(int horizLoc, int vertLoc, ushort style)
		{
			this.Owner.Services.AssignDot(2, horizLoc, vertLoc, style);
		}
	}
}
