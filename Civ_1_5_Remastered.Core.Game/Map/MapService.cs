using System;

namespace Civ_1_5_Remastered.Core.Game.Map
{
	public class MapService : GameComponent, IMapService
	{
		private const ushort CityLabelHorizontalAddress = 0x6dac;

		private const ushort CityLabelVerticalAddress = 0x6e3e;

		private const ushort CityLabelTownAddress = 0xdf20;

		private const ushort VitalityStripBackdrop = 0;

		private const ushort VitalityStripComplete = 10;
		private const ushort VitalityStripBlank = 8;
		private const int VitalityStripCellBreadth = 2;
		private const int VitalityStripCellAltitude = 1;

		private readonly MapPlanes chart;
		private readonly UnitMemory troops;

		public MapService(IGameEngine owner)
			: base(owner)
		{
			this.chart = new MapPlanes(owner);
			this.troops = new UnitMemory(owner.Processor);
		}

		public IMapPlanes Chart
		{
			get { return this.chart; }
		}

		public void FocusMapView(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"FocusMapView({participantIndex}, {horizontalLocation}, {verticalLocation})");

			this.Owner.Startup.HideMouseCursor();

			Screen.RepaintRequest = 0x0;

			this.Owner.TurnHooks.ClearStatusLine();

			this.Owner.TurnHooks.DrawStatusBar();

			ushort kept = MapView.UnitAtTilePlayer;

			MapView.Horizontal = (ushort)this.Owner.Routing.TunePosition(horizontalLocation);
			MapView.Vertical = (ushort)this.Owner.Helpers.ClampValue(verticalLocation, 0, 38);

			ushort chartSightFilter = (ushort)(1 << (byte)participantIndex);

			MapView.CityLabelCount = 0x0;

			ushort order = (ushort)this.Owner.Runtime.Dice.DrawInteger(256);

			for (int tileIndex = 0; tileIndex < 0x100; tileIndex++)
			{
				short column = (short)(order & 0xf);
				short row = (short)((short)order >> 4);

				if (row < 0xc && column < 0xf)
				{
					bool shown = MapView.Revealed != 0;

					if (!shown)
					{
						ushort tileHorizontal = (ushort)this.Owner.Routing.TunePosition(column + (short)MapView.Horizontal);
						ushort tileVertical = (ushort)(row + MapView.Vertical);

						shown = (this.Owner.World.ChartSight[tileHorizontal, tileVertical] & chartSightFilter) != 0;
					}

					if (shown)
					{
						RepaintMapTile((short)this.Owner.Routing.TunePosition(column + (short)MapView.Horizontal),
							row + (short)MapView.Vertical);
					}
					else
					{
						this.Owner.Services.FloodBox(Screen.Box, (column * 16) + 80, (row * 16) + 8, 16, 16, Hues.Black);
					}
				}

				order = (byte)((order * 5) + 1);
			}

			for (int labelIndex = 0; labelIndex < (short)MapView.CityLabelCount; labelIndex++)
			{
				ushort place = (ushort)(labelIndex << 1);

				if (this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(place + CityLabelVerticalAddress)) >= 0xb8)
				{
					continue;
				}

				EmptyMessage()
					.City(this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(place + CityLabelTownAddress)));

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, (ushort)(327 - this.Processor.PullGlobalWord((ushort)(place + CityLabelHorizontalAddress))));

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(),
					this.Owner.Helpers.ClampValue(this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(place + CityLabelHorizontalAddress)) - 8, 80, 999),
					this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(place + CityLabelVerticalAddress)) + 16, Hues.LightCyan);
			}

			short left = (short)(horizontalLocation - 0x20);
			short top = (short)(verticalLocation - 0x13);

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 8) != 0)
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

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker == 0xff || (town.ShownExtent == 0 && town.ParticipantIndex != this.Owner.World.PersonParticipantIndex))
				{
					continue;
				}

				short dotHorizontal = (short)this.Owner.Routing.TunePosition(town.Place.X - (short)wrappedLeft);
				short dotVertical = (short)(town.Place.Y - sourceTop + targetTop);

				if (dotVertical < 0 || dotVertical >= 0x32)
				{
					continue;
				}

				this.Owner.Services.AssignDot(dotHorizontal, dotVertical + 8, Tables.PlayerColour(town.ParticipantIndex));
			}

			this.Owner.Dialogs.DrawShadowBox(0, 8, 79, 49, Hues.White, Hues.DarkGray);

			this.Owner.Dialogs.DrawBox(horizontalLocation - left - 1, verticalLocation - top + 7, 17, 10, Hues.White);

			MapView.UnitAtTilePlayer = kept;

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort DrawMapTile(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"RenderMapTile({horizontalLocation}, {verticalLocation})");

			if (MapView.WorldMapView == 0x1)
			{
				ushort outcome = DrawWorldMapTileBlock(horizontalLocation, verticalLocation);
				return FinishRenderMapTile(outcome);
			}

			short screenHorizontal = (short)((this.Owner.Routing.TunePosition(horizontalLocation - (short)MapView.Horizontal) << 4) + 0x50);

			short screenVertical = (short)(((verticalLocation - (short)MapView.Vertical) << 4) + 0x8);

			if (screenHorizontal < 0x50 || screenHorizontal >= 0x140 || screenVertical < 0x8 || screenVertical > 0xc0)
			{
				return FinishRenderMapTile(0);
			}

			ushort terrain = ObtainTerrainType(horizontalLocation, verticalLocation);

			ushort improvements = ObtainKnownTileImprovements(horizontalLocation, verticalLocation);

			if (MapView.Revealed != 0)
			{
				improvements = ObtainTileImprovements(horizontalLocation, verticalLocation);
			}

			this.Owner.Startup.HideMouseCursor();

			if (terrain == 0xa)
			{
				DrawOceanTile(horizontalLocation, verticalLocation, screenHorizontal, screenVertical);
			}

			if (terrain != 0xa)
			{
				if (Screen.VideoGraphicsMode == 0)
				{
					this.Owner.VideoCard.RenderImage(
						Screen.SpriteScreenBox,
						0, 0x50, 16, 16,
						Screen.Box,
						(ushort)screenHorizontal, (ushort)screenVertical);
				}
				else
				{
					this.Owner.VideoCard.RenderImage(
						Screen.PictureScreenBox,
						0x100, 0x78, 16, 16,
						Screen.Box,
						(ushort)screenHorizontal, (ushort)screenVertical);
				}
			}

			if ((improvements & 0x2) != 0 &&
				terrain != 0xa &&
				MapView.UnitsHidden == 0 &&
				(improvements & 0x1) == 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					Tables.SpritePicture(4));
			}

			if (terrain == 0xb)
			{
				DrawRiverTile(horizontalLocation, verticalLocation, screenHorizontal, screenVertical);
			}
			else if (terrain != 0xa)
			{
				DrawLandTerrainOverlay(horizontalLocation, verticalLocation, screenHorizontal, screenVertical, terrain);
			}

			DrawTileImprovementsAndFeatures(horizontalLocation, verticalLocation, screenHorizontal, screenVertical, terrain, improvements);

			ushort ownerBits = ObtainTileOwnerBits(horizontalLocation, verticalLocation);

			if ((improvements & 0x1) != 0 && MapView.UnitsHidden == 0)
			{
				DrawCityOnTile(horizontalLocation, verticalLocation, screenHorizontal, screenVertical);
			}

			this.Owner.Startup.ShowMouseCursor();

			return FinishRenderMapTile(1);
		}

		public ushort DrawUnitOnWorldMap(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitOnWorldMap({participantIndex}, {troopIndex})");

			sbyte horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			sbyte verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);
			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			if (MapView.WorldMapView == 1)
			{
				this.Processor.RegisterAccumulator.Word = (ushort)(troopIndex * UnitMemory.TroopStride);

				this.Owner.Services.FloodBox(Screen.Box, (horizontalLocation * 4) + 1, (verticalLocation * 4) + 1, 3, 3, (ushort)(kind == 0 ? 6 : 0));
				this.Owner.Services.FloodBox(Screen.Box, horizontalLocation * 4, verticalLocation * 4, 3, 3,
					Tables.PlayerColour(participantIndex));

				return this.Processor.RegisterAccumulator.Word;
			}

			short viewHorizontal = (short)((this.Owner.Routing.TunePosition(horizontalLocation - (short)MapView.Horizontal) << 4) + 0x50);
			short viewVertical = (short)((((byte)verticalLocation - MapView.Vertical) << 4) + 8);
			ushort drawn = 0;

			if (viewHorizontal >= 0x50 && viewHorizontal < 0x140 && viewVertical >= 8 && viewVertical <= 0xc0 &&
				(ObtainTerrainType(horizontalLocation, verticalLocation) != MapRules.OceanTerrain ||
				(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 1) == 0 ||
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
			{
				this.Owner.Startup.HideMouseCursor();

				if (this.troops.Following(participantIndex, troopIndex) != -1)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, viewHorizontal + 1, viewVertical + 1,
						Tables.SpritePicture((participantIndex << 5) + kind + 0x40));
				}

				DrawUnitSprite(participantIndex, troopIndex, viewHorizontal, viewVertical);

				this.Owner.Startup.ShowMouseCursor();

				drawn = 1;
			}

			this.Processor.RegisterAccumulator.Word = drawn;

			return drawn;
		}

		public void DrawHealthBar(int horizontalLocation, int verticalLocation, int completeStrikeMarks, int strikeMarks)
		{
			ushort display = Screen.Box;

			int breadth = Math.Min((completeStrikeMarks * (VitalityStripCellBreadth + 1)) + 1, 15);
			int remaining = horizontalLocation + 1 + ((15 - breadth) / 2);
			int head = verticalLocation + 1;

			this.Owner.Services.FloodBox(display, remaining, head, breadth, VitalityStripCellAltitude + 2, VitalityStripBackdrop);

			for (int index = 0; index < completeStrikeMarks; index++)
			{
				this.Owner.Services.FloodBox(display, remaining + 1 + (index * (VitalityStripCellBreadth + 1)), head + 1,
					VitalityStripCellBreadth, VitalityStripCellAltitude, index < strikeMarks ? VitalityStripComplete : VitalityStripBlank);
			}
		}

		public void DrawUnitSprite(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"RenderUnitSprite({participantIndex}, {troopIndex}, {horizontalLocation}, {verticalLocation})");

			byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);
			ushort markHue = (ushort)(participantIndex == 1 ? 9 : 15);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, horizontalLocation, verticalLocation,
				Tables.SpritePicture(kind + (participantIndex << 5) + 0x40));

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 8) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, horizontalLocation, verticalLocation, Tables.SpritePicture(0x1d));
			}
			else if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 4) != 0)
			{
				this.Owner.TextPainter.DrawTextWithShadow(GameTexts.TextFortressLetter, horizontalLocation + 4, verticalLocation + 7, markHue);
			}

			if (participantIndex == this.Owner.World.PersonParticipantIndex && this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				this.Owner.TextPainter.DrawTextWithShadow(GameTexts.TextGotoLetter, horizontalLocation + 4, verticalLocation + 7, markHue);
			}

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 0xc2) != 0 &&
				Tables.UnitKindWord((sbyte)kind, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				byte status = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
				byte letter = 0x52;

				if ((status & 0x40) != 0)
				{
					letter = (byte)(kind == 0 ? 0x49 : 0x3f);
				}

				if ((status & 0x80) != 0)
				{
					letter = (byte)((status & 0x40) != 0 ? 0x46 : 0x4d);

					if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 2) != 0)
					{
						letter = 0x50;
					}
				}

				this.Owner.Words.Set(" ");
				this.Processor.EmitGlobalByte(TextBuffer.Address, letter);

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), horizontalLocation + 4, verticalLocation + 7, markHue);

				this.Owner.Dialogs.DrawBox(horizontalLocation - 1, verticalLocation - 1, 15, 15, Hues.LightGray);
			}

			if ((this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & 1) != 0)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizontalLocation, verticalLocation, 16, 16, Hues.Magenta, Hues.LightGray);

				this.Owner.VideoCard.SubstituteHue(Screen.Box, horizontalLocation, verticalLocation, 16, 16, Hues.DarkGray, Hues.LightGray);
			}

			{
				int troopKind = this.Troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);
				int missingStrikeMarks = this.Owner.World.Vitals.ObtainLostHitPoints(participantIndex, troopIndex, troopKind);

				if (troopKind != 0xff)
				{
					bool seasoned = (this.Troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0;
					int completeStrikeMarks = this.Owner.World.Vitals.ObtainFullHitPoints(participantIndex, troopIndex, troopKind, seasoned);

					DrawHealthBar(horizontalLocation - 1, verticalLocation - 1, completeStrikeMarks, completeStrikeMarks - missingStrikeMarks);
				}
			}
		}

		public void RepaintMapTile(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintMapTile({horizontalLocation}, {verticalLocation})");

			DrawMapTile(horizontalLocation, verticalLocation);

			if (MapView.UnitsHidden == 0)
			{
				ushort troopIndex = ObtainUnitAtTile(horizontalLocation, verticalLocation);

				if (troopIndex != 0xffff)
				{
					short ownerIndex = (short)MapView.UnitAtTilePlayer;
					short personIndex = this.Owner.World.PersonParticipantIndex;

					bool seen = MapView.Revealed != 0 || ownerIndex == personIndex ||
						(this.troops.Pull(ownerIndex, troopIndex, UnitMemory.ShownToParticipants) & (1 << personIndex)) != 0;

					if (seen && (ObtainTileImprovements(horizontalLocation, verticalLocation) & 0x1) == 0)
					{
						RepaintUnitTile((short)MapView.UnitAtTilePlayer, (short)troopIndex);
					}
				}
			}
		}

		public void RepaintUnitTile(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RepaintUnitTile({participantIndex}, {troopIndex})");

			ushort terrain = ObtainTerrainType(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

			if (terrain == 0xa && this.troops.Following(participantIndex, troopIndex) != -1 &&
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
			{
				short current = troopIndex;

				do
				{
					current = this.troops.Following(participantIndex, current);

					if (this.troops.KindField(participantIndex, current, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
					{
						DrawUnitOnWorldMap(participantIndex, current);
						current = -1;
					}
				}
				while (current != -1 && current != troopIndex);

				if (current == troopIndex)
				{
					DrawUnitOnWorldMap(participantIndex, troopIndex);
				}
			}
			else
			{
				DrawUnitOnWorldMap(participantIndex, (short)this.Owner.UnitOperations.FindBestGuardianInStack(participantIndex, troopIndex));
			}
		}

		public ushort CheckMapLimits(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"VerifyMapLimits({horizontalLocation}, {verticalLocation})");

			ushort onMap = ReturnAccumulator(MapRules.IsOnMap(horizontalLocation, verticalLocation) ? 1 : 0);

			return onMap;
		}

		public ushort ObtainTerrainType(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTerrainType({horizontalLocation}, {verticalLocation})");

			ushort terrain = ReturnAccumulator(this.chart.Terrain(horizontalLocation, verticalLocation));

			return terrain;
		}

		public ushort ObtainTileOwnerBits(int horizontalLocation, int verticalLocation)
		{
			return ReturnAccumulator(this.chart.OwnerBits(horizontalLocation, verticalLocation) & 0x7);
		}

		public void MapAssignCityOwner(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			int bits = ReturnAccumulator(this.chart.OwnerBits(horizontalLocation, verticalLocation));

			this.chart.AssignOwnerBits(horizontalLocation, verticalLocation, (bits & MapPlanes.UnitsPresent) + participantIndex);
		}

		public void PlaceUnitOnTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"SituateUnitOnTile({participantIndex}, {troopIndex}, {horizontalLocation}, {verticalLocation})");

			ushort stackTroopIndex = ObtainUnitAtTile(horizontalLocation, verticalLocation);

			if (stackTroopIndex != 0xffff)
			{
				this.Owner.Combat.AttachUnitToStack(participantIndex, troopIndex, stackTroopIndex);
			}

			AssignChartDot(horizontalLocation + 160, verticalLocation, (ushort)(participantIndex + MapPlanes.UnitsPresent));
		}

		public void RemoveUnitFromTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"DiscardUnitFromTile({participantIndex}, {troopIndex}, {horizontalLocation}, {verticalLocation})");

			if (this.troops.Following(participantIndex, troopIndex) != -1)
			{
				this.Owner.Combat.RemoveUnitFromStack(participantIndex, troopIndex);
			}
			else
			{
				ushort cityIndex = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);
				byte ownerHue = (cityIndex != 0xffff && cityIndex < this.Owner.World.Towns.Length && this.Owner.World.Towns[cityIndex].ConditionMarker != 0xff)
					? (byte)this.Owner.World.Towns[cityIndex].ParticipantIndex
					: (byte)participantIndex;
				AssignChartDot(horizontalLocation + 160, verticalLocation, ownerHue);
			}
		}

		public ushort ObtainUnitAtTile(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainUnitAtTile({horizontalLocation}, {verticalLocation})");

			ushort found = 0xffff;
			ushort participantIndex = ObtainTileOwner(horizontalLocation, verticalLocation);

			if (participantIndex != 0xffff)
			{
				for (int troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (this.troops.Exists(participantIndex, troopIndex) && this.troops.StandsAt(participantIndex, troopIndex, horizontalLocation, verticalLocation))
					{
						found = (ushort)troopIndex;
						break;
					}
				}

				if (found != 0xffff)
				{
					Search.FoundPlayer = participantIndex;
					MapView.UnitAtTilePlayer = participantIndex;
				}
				else
				{
					ushort cityIndex = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);
					byte ownerHue = (cityIndex != 0xffff && cityIndex < this.Owner.World.Towns.Length && this.Owner.World.Towns[cityIndex].ConditionMarker != 0xff)
						? (byte)this.Owner.World.Towns[cityIndex].ParticipantIndex
						: (byte)0;
					this.Owner.Services.AssignDot(2, horizontalLocation + 160, verticalLocation, ownerHue);
				}
			}

			ReturnAccumulator(found);

			return found;
		}

		public ushort ObtainTileOwner(int horizontalLocation, int verticalLocation)
		{
			int bits = this.chart.OwnerBits(horizontalLocation, verticalLocation);

			return ReturnAccumulator((bits & MapPlanes.UnitsPresent) != 0 ? bits & 0x7 : 0xffff);
		}

		public void TagTileOwned(int horizontalLocation, int verticalLocation)
		{
			int bits = ReturnAccumulator(this.chart.OwnerBits(horizontalLocation, verticalLocation));

			this.chart.AssignOwnerBits(horizontalLocation, verticalLocation, (bits & 0x7) + MapPlanes.UnitsPresent);
		}

		public ushort ContainsRoad(int horizontalLocation, int verticalLocation)
		{
			return ReturnAccumulator(this.chart.Improvements(horizontalLocation, verticalLocation) & 0x8);
		}

		public ushort ObtainTileImprovements(int horizontalLocation, int verticalLocation)
		{
			return ReturnAccumulator(this.chart.Improvements(horizontalLocation, verticalLocation));
		}

		public ushort ObtainKnownTileImprovements(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainKnownTileImprovements({horizontalLocation}, {verticalLocation})");

			ushort known = ReturnAccumulator(this.chart.KnownImprovements(horizontalLocation, verticalLocation));

			return known;
		}

		public void KeepKnownTileImprovements(int horizontalLocation, int verticalLocation)
		{
			this.chart.RevealImprovements(horizontalLocation, verticalLocation);

			ReturnAccumulator(this.chart.Improvements(horizontalLocation, verticalLocation) >> 4);
		}

		public void AssignTileImprovements(ushort improvements, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"AssignTileImprovements({improvements}, {horizontalLocation}, {verticalLocation})");

			if (improvements == 0)
			{
				AssignChartDot(horizontalLocation, verticalLocation + 100, 0);
				AssignChartDot(horizontalLocation, verticalLocation + 150, 0);
			}
			else if (improvements >= 0x10)
			{
				int high = ReturnAccumulator(this.chart.HighImprovements(horizontalLocation, verticalLocation));
				AssignChartDot(horizontalLocation, verticalLocation + 150, (ushort)((improvements >> 4) | high));
			}
			else
			{
				int low = ReturnAccumulator(this.chart.LowImprovements(horizontalLocation, verticalLocation));
				AssignChartDot(horizontalLocation, verticalLocation + 100, (ushort)(low | improvements));
			}

			if ((short)Turn.Player == this.Owner.World.PersonParticipantIndex)
			{
				KeepKnownTileImprovements(horizontalLocation, verticalLocation);
			}
		}

		public void RemoveTileImprovements(ushort improvements, int horizontalLocation, int verticalLocation)
		{
			this.chart.ClearImprovements(improvements, horizontalLocation, verticalLocation);
		}

		public ushort FindCityAtTile(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"LocateCityAtTile({horizontalLocation}, {verticalLocation})");

			ushort participantIndex = ObtainTileOwnerBits(horizontalLocation, verticalLocation);
			ushort found = 0xffff;

			for (int townIndex = 0; townIndex < this.Owner.World.Towns.Length; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ConditionMarker != 0xff && town.Place.X == horizontalLocation && town.Place.Y == verticalLocation)
				{
					Search.FoundPlayer = participantIndex;
					MapView.UnitAtTilePlayer = participantIndex;
					found = (ushort)townIndex;
					break;
				}
			}

			if (found == 0xffff)
			{
				this.Owner.TurnHooks.ShowDialog(GameTexts.TextCityAtError, 100, 80);
			}

			ReturnAccumulator(found);

			return found;
		}

		public ushort ContainsSpecialResource(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ContainsSpecialResource({horizontalLocation}, {verticalLocation})");

			ushort special = ReturnAccumulator(MapRules.HasSpecialResource(horizontalLocation, verticalLocation, this.Owner.World.ChanceRoot) ? 1 : 0);

			return special;
		}

		public ushort ContainsHut(ushort terrain, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ContainsHut({terrain}, {horizontalLocation}, {verticalLocation})");

			bool hut = terrain != MapRules.OceanTerrain &&
				(this.Owner.World.ChartSight[horizontalLocation, verticalLocation] & 1) == 0 &&
				MapRules.IsHutPlace(horizontalLocation, verticalLocation, this.Owner.World.ChanceRoot) &&
				(ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) == 0;

			ushort result = ReturnAccumulator(hut ? 1 : 0);

			return result;
		}

		public ushort ObtainContinentIdentifier(int horizontalLocation, int verticalLocation)
		{
			return ReturnAccumulator(this.chart.Continent(horizontalLocation, verticalLocation));
		}

		public ushort ObtainLandMassSize(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainLandMassSize({horizontalLocation}, {verticalLocation})");

			ushort sizes = ObtainTerrainType(horizontalLocation, verticalLocation) != MapRules.OceanTerrain ? (ushort)0xdcfe : (ushort)0xdd7e;
			int continent = ObtainContinentIdentifier(horizontalLocation, verticalLocation);
			ushort size = ReturnAccumulator(this.Processor.PullGlobalWord((ushort)(sizes + (continent << 1))));

			return size;
		}

		private ushort ReturnAccumulator(int amount)
		{
			ushort result = (ushort)amount;
			this.Processor.RegisterAccumulator.Word = result;

			return result;
		}

		private ushort FinishRenderMapTile(ushort result)
		{
			this.Processor.RegisterAccumulator.Word = result;
			return result;
		}

		private ushort DrawWorldMapTileBlock(int horizontalLocation, int verticalLocation)
		{
			ushort improvement = ObtainTileImprovements(horizontalLocation, verticalLocation);
			if ((improvement & 0x1) != 0)
			{
				ushort ownerBits = ObtainTileOwnerBits(horizontalLocation, verticalLocation);
				this.Owner.Services.FloodBox(
					Screen.Box,
					horizontalLocation * 4, verticalLocation * 4, 4, 4,
					Tables.PlayerColour(ownerBits));
				return ownerBits;
			}

			ushort terrain = ObtainTerrainType(horizontalLocation, verticalLocation);
			if (terrain == 0xa)
			{
				this.Owner.Services.FloodBox(Screen.Box, horizontalLocation * 4, verticalLocation * 4, 4, 4, Hues.Blue);
				return 0xa;
			}

			this.Owner.Services.FloodBox(Screen.Box, horizontalLocation * 4, verticalLocation * 4, 4, 4, Hues.Green);
			return 0x2;
		}

		private ushort MaskNeighbours(int horizontalLocation, int verticalLocation, ushort mask, ushort step, ushort bit, Func<ushort, bool> fits)
		{
			for (ushort direction = 1; direction < 9; direction += step)
			{
				mask = (ushort)((short)mask >> 1);

				ushort adjacentHorizontal = (ushort)this.Owner.Routing.TunePosition(
					(short)Tables.DirectionHorizontal(direction) + horizontalLocation);
				int adjacentVertical = (short)Tables.DirectionVertical(direction) + verticalLocation;

				if (fits(ObtainTerrainType((short)adjacentHorizontal, adjacentVertical)) && CheckMapLimits(0, adjacentVertical) != 0)
				{
					mask |= bit;
				}
			}

			return mask;
		}

		private void DrawOceanTile(int horizontalLocation, int verticalLocation, short screenHorizontal, short screenVertical)
		{
			ushort mask = 0;

			if (Screen.VideoGraphicsMode == 0)
			{
				mask = MaskNeighbours(horizontalLocation, verticalLocation, mask, 2, 0x8, terrainType => terrainType != 0xa);

				this.Owner.VideoCard.RenderImage(
					Screen.SpriteScreenBox,
					(ushort)(mask << 4), 0x40, 16, 16,
					Screen.Box,
					(ushort)screenHorizontal, (ushort)screenVertical);
				return;
			}

			mask = MaskNeighbours(horizontalLocation, verticalLocation, mask, 1, 0x80, terrainType => terrainType != 0xa);
			ushort rawMask = mask;

			ushort rotatedMask = (ushort)((((short)mask >> 6) & 0x3) + (mask << 2));

			for (ushort quad = 0; quad < 4; quad++)
			{
				ushort piece = (ushort)(((short)rotatedMask >> ((byte)quad << 1)) & 0x7);
				ushort sprite = this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((quad << 1) + (piece << 3) + 0xd294));

				short queryHorizontal = quad >= 2
					? (short)(screenHorizontal - ((quad & 1) << 3) + 8)
					: (short)(screenHorizontal + ((quad & 1) << 3));
				short queryVertical = quad >= 2 ? (short)(screenVertical + 8) : screenVertical;

				this.Owner.Services.DrawPictureToScreen(Screen.Box, queryHorizontal, queryVertical, sprite);
			}

			int specialCoastHorizontal = rawMask switch
			{
				0x1c => 0xe0,
				0xc1 => 0xf0,
				0x07 => 0x100,
				0x70 => 0x110,
				0x8f => 0x120,
				0xf8 => 0x130,
				_ => -1,
			};

			if (specialCoastHorizontal >= 0)
			{
				this.Owner.VideoCard.RenderImage(
					Screen.PictureScreenBox,
					(ushort)specialCoastHorizontal, 0x64, 16, 16,
					Screen.Box,
					(ushort)screenHorizontal, (ushort)screenVertical);
			}

			for (ushort direction = 1; direction < 9; direction += 2)
			{
				ushort adjacentHorizontal = (ushort)this.Owner.Routing.TunePosition(
					(short)Tables.DirectionHorizontal(direction) + horizontalLocation);
				int adjacentVertical = (short)Tables.DirectionVertical(direction) + verticalLocation;

				if (ObtainTerrainType((short)adjacentHorizontal, adjacentVertical) == 0xb)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						screenHorizontal, screenVertical,
						this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(((direction / 2) << 1) + 0xd2d4)));
				}
			}
		}

		private void DrawRiverTile(int horizontalLocation, int verticalLocation, short screenHorizontal, short screenVertical)
		{
			ushort mask = 0;

			for (ushort direction = 1; direction < 9; direction += 2)
			{
				mask = (ushort)((short)mask >> 1);

				ushort adjacentTerrain = ObtainTerrainType(
					(short)Tables.DirectionHorizontal(direction) + horizontalLocation,
					(short)Tables.DirectionVertical(direction) + verticalLocation);

				if (adjacentTerrain == 0xb || adjacentTerrain == 0xa)
				{
					mask |= 0x8;
				}
			}

			if (Screen.VideoGraphicsMode == 0 ||
				(this.Owner.VideoCard.ObtainDot(2, horizontalLocation, verticalLocation + 150) & 0x8) != 0)
			{
				mask = (ushort)(mask + 0x10);
			}

			this.Owner.Services.DrawPictureToScreen(
				Screen.Box,
				screenHorizontal, screenVertical,
				this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((mask << 1) + 0x6dfe)));
		}

		private void DrawLandTerrainOverlay(int horizontalLocation, int verticalLocation, short screenHorizontal, short screenVertical, ushort terrain)
		{
			if (Screen.VideoGraphicsMode != 0)
			{
				ushort mask = 0;

				mask = MaskNeighbours(horizontalLocation, verticalLocation, mask, 2, 0x8, terrainType => terrainType == terrain);

				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((mask << 1) + (terrain << 5) + 0xb886)));

				if (terrain == 0x2 && (((byte)((7 * horizontalLocation) + (11 * verticalLocation))) & 0x2) == 0)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						(short)(screenHorizontal + 4),
						(short)(screenVertical + 4),
						Icons.MiniSymbolPicture);
				}
			}
			else
			{
				ushort variant = terrain == 0x2
					? (ushort)((((byte)((11 * verticalLocation) + (7 * horizontalLocation))) & 0x2) >> 1)
					: (ushort)((horizontalLocation + verticalLocation) & 0x1);

				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((terrain << 5) + (variant << 1) + 0xb886)));
			}
		}

		private void DrawTileImprovementsAndFeatures(int horizontalLocation, int verticalLocation, short screenHorizontal, short screenVertical, ushort terrain, ushort improvements)
		{
			if ((improvements & 0x40) != 0)
			{
				if (Screen.VideoGraphicsMode != 0)
				{
					this.Owner.Services.DrawPictureToScreen(
						Screen.Box,
						screenHorizontal, screenVertical,
						Icons.TileFortressPicture);
				}
				else
				{
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenHorizontal, screenVertical, 16, 16, Hues.Green, Hues.Black);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenHorizontal, screenVertical, 16, 16, Hues.LightGreen, Hues.White);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenHorizontal, screenVertical, 16, 16, Hues.LightBlue, Hues.DarkGray);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, screenHorizontal, screenVertical, 16, 16, Hues.LightCyan, Hues.Black);
				}
			}

			if ((improvements & 0x8) != 0)
			{
				ushort isolatedDotHue = (improvements & 0x10) == 0 ? (ushort)0x6 : (ushort)0;

				for (ushort direction = 1; direction <= 8; direction++)
				{
					ushort adjacentImprovement = ObtainKnownTileImprovements(
						(short)Tables.DirectionHorizontal(direction) + horizontalLocation,
						(short)Tables.DirectionVertical(direction) + verticalLocation);

					if ((adjacentImprovement & 0x8) != 0)
					{
						isolatedDotHue = 0xffff;

						ushort roadTable = ((improvements & 0x11) != 0 && (adjacentImprovement & 0x11) != 0) ? (ushort)0xb298 : (ushort)0xb278;
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							screenHorizontal, screenVertical,
							this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((direction << 1) + roadTable)));
					}
				}

				if (isolatedDotHue != 0xffff)
				{
					this.Owner.Services.FloodBox(
						Screen.Box,
						screenHorizontal + 7, screenVertical + 7, 2, 2,
						isolatedDotHue);
				}
			}

			if ((improvements & 0x4) != 0 && MapView.UnitsHidden == 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					Tables.SpritePicture(5));
			}

			if (ContainsSpecialResource(horizontalLocation, verticalLocation) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					Tables.SpritePicture(terrain + 0x10));
			}

			if (ContainsHut(terrain, horizontalLocation, verticalLocation) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					Tables.SpritePicture(0x1f));
			}

			if ((improvements & 0x20) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal, screenVertical,
					Tables.SpritePicture(0x1e));
			}

			if (MapView.Revealed == 0)
			{
				ushort personBit = (ushort)(1 << (this.Owner.World.PersonParticipantIndex & 0xff));

				for (ushort direction = 1; direction <= 8; direction += 2)
				{
					ushort adjacentHorizontal = (ushort)this.Owner.Routing.TunePosition(
						(short)Tables.DirectionHorizontal(direction) + horizontalLocation);
					ushort adjacentVertical = (ushort)(Tables.DirectionVertical(direction) + (ushort)verticalLocation);

					if (((ushort)(short)this.Owner.World.ChartSight[adjacentHorizontal, adjacentVertical] & personBit) == 0)
					{
						this.Owner.Services.DrawPictureToScreen(
							Screen.Box,
							screenHorizontal, screenVertical,
							this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(((direction / 2) << 1) + 0x7eec)));
					}
				}
			}
		}

		private void DrawCityOnTile(int horizontalLocation, int verticalLocation, short screenHorizontal, short screenVertical)
		{
			ushort cityIndex = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);

			City town = this.Owner.World.Towns[cityIndex];
			if (town.ParticipantIndex != this.Owner.World.PersonParticipantIndex &&
				town.ShownExtent == 0 &&
				MapView.Revealed == 0)
			{
				return;
			}

			this.Owner.Dialogs.DrawBox(screenHorizontal + 1, screenVertical + 1, 13, 13, Hues.White);

			short innerHorizontal = (short)(screenHorizontal + 2);

			ushort ownerWordOffset = (ushort)(town.ParticipantIndex << 1);
			this.Owner.Dialogs.DrawBox(
				screenHorizontal + 2, screenVertical + 1, 12, 12,
				this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(ownerWordOffset + 0x1956)));

			short innerVertical = (short)(screenVertical + 2);

			this.Owner.Services.FloodBox(
				Screen.Box,
				innerHorizontal, innerVertical, 12, 12,
				Tables.PlayerColourAt(ownerWordOffset));

			this.Owner.Services.DrawPictureToScreen(
				Screen.Box,
				(short)(screenHorizontal + 1),
				(short)(screenVertical + 1),
				Tables.SpritePicture(0x1c));

			this.Owner.VideoCard.SubstituteHue(
				Screen.Box,
				innerHorizontal, innerVertical, 12, 12, Hues.Magenta,
				(byte)this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(ownerWordOffset + 0x1956)));

			short displayedSize = (town.ParticipantIndex == this.Owner.World.PersonParticipantIndex || MapView.Revealed != 0)
				? (short)town.TrueExtent
				: (short)town.ShownExtent;

			EmptyMessage()
				.Number(displayedSize);

			if ((town.ConditionMarker & 0x1) != 0)
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal + 5, innerVertical,
					Icons.UnhappyCitizenPicture);
			}
			else
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(
					this.Owner.Words.ToString(),
					(displayedSize < 10 ? 6 : 3) + screenHorizontal,
					screenVertical + 5,
					Hues.Black);
			}

			if (ObtainUnitAtTile(horizontalLocation, verticalLocation) != 0xffff || (byte)town.Unfamiliar[0] != 0xff)
			{
				this.Owner.Dialogs.DrawBox(screenHorizontal, screenVertical, 15, 15, Hues.Black);
			}

			if (town.Has(CityStructure.CityWalls))
			{
				this.Owner.Services.DrawPictureToScreen(
					Screen.Box,
					screenHorizontal + 1, screenVertical + 1,
					Tables.SpritePicture(0x1d));
			}

			ushort labelIndex = MapView.CityLabelCount;
			if ((short)labelIndex < 0x20)
			{
				ushort slot = (ushort)(labelIndex << 1);
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(slot + CityLabelHorizontalAddress), (ushort)screenHorizontal);
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(slot + CityLabelVerticalAddress), (ushort)screenVertical);
				MapView.CityLabelCount = (ushort)(labelIndex + 1);
				this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(slot + CityLabelTownAddress), cityIndex);
			}
		}

		private void AssignChartDot(int horizontalLocation, int verticalLocation, ushort style)
		{
			this.Owner.Services.AssignDot(2, horizontalLocation, verticalLocation, style);
		}
	}
}
