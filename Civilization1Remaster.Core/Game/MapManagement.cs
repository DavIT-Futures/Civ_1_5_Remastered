using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2aea.
	/// </summary>
	public class MapManagement : GameModule
	{
		private readonly MapLayers chart;
		private readonly UnitMemory troops;

		public MapManagement(GameEngine owner)
			: base(owner)
		{
			this.chart = new MapLayers(owner);
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>The map data (terrain, continents, improvements, tile owners)</summary>
		public MapLayers Chart
		{
			get { return this.chart; }
		}

		/// <summary>
		/// Returns the value also in AX, like the original function: the translated callers read it from there.
		/// </summary>
		private ushort ReturnAcc(int amount)
		{
			ushort result = (ushort)amount;
			this.processor.RegAcc.Value16 = result;

			return result;
		}

		/// <summary>
		/// Centres the map view on the place (0xd4cc / 0xd75e): the 15 x 12 tiles are repainted in a random order (unseen ones
		/// black, all with 0xd806), the names of the cities on the map (0x6c96 of them) are written, then the small world map
		/// in the corner: the part around the place (from the screen 0x19e8, wrapped around, from the top with the technology 8),
		/// the dots of the known cities and the frame of the view.
		/// </summary>
		public void FocusMapView(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"FocusMapView({participantIdx}, {horizLoc}, {vertLoc})");

			this.owner.Startup.ConcealMouseCursor();

			ScreenRepaintRequest = 0x0;

			this.owner.TurnHooks.WipeStatusLine();

			this.owner.TurnHooks.RenderStatusBar();

			ushort kept = UnitAtTilePlayer;

			MapViewHoriz = (ushort)this.owner.Routing.TunePosition(horizLoc);
			MapViewVert = (ushort)this.owner.Helpers.VerifyValueSpan(vertLoc, 0, 38);

			// Another error, the code modified first parameter (playerID) to a
			// Visibility Mask and that conflicts with other code which expects playerID.
			ushort chartSightFilter = (ushort)(1 << (byte)participantIdx);

			CityLabelCount = 0x0;

			ushort order = (ushort)this.owner.Libc.Dice.DrawInt(256);

			for (int tileIdx = 0; tileIdx < 0x100; tileIdx++)
			{
				short column = (short)(order & 0xf);
				short row = (short)((short)order >> 4);

				if (row < 0xc && column < 0xf)
				{
					bool shown = MapRevealed != 0;

					if (!shown)
					{
						ushort tileHoriz = (ushort)this.owner.Routing.TunePosition(column + (short)MapViewHoriz);
						ushort tileVert = (ushort)(row + MapViewVert);

						shown = (this.owner.World.ChartSight[tileHoriz, tileVert] & chartSightFilter) != 0;
					}

					if (shown)
					{
						RepaintMapTile((short)this.owner.Routing.TunePosition(column + (short)MapViewHoriz),
							row + (short)MapViewVert);
					}
					else
					{
						this.owner.Services.FloodBox(ScreenBox, (column * 16) + 80, (row * 16) + 8, 16, 16, 0);
					}
				}

				order = (byte)((order * 5) + 1);
			}

			for (int labelIdx = 0; labelIdx < (short)CityLabelCount; labelIdx++)
			{
				ushort place = (ushort)(labelIdx << 1);

				if (this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(place + 0x6e3e)) >= 0xb8)
					continue;

				EmptyMessage()
					.City(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(place + 0xdf20)));

				this.owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, (ushort)(327 - this.processor.PullGlobalWord((ushort)(place + 0x6dac))));

				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address,
					this.owner.Helpers.VerifyValueSpan(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(place + 0x6dac)) - 8, 80, 999),
					this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(place + 0x6e3e)) + 16, 11);
			}

			// The part of the world map in the corner (80 x 50), the screen 0x19e8 has the world map at column 0xf0
			short left = (short)(horizLoc - 0x20);
			short top = (short)(vertLoc - 0x13);

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 8) != 0)
			{
				top = 0;
			}

			ushort wrappedLeft = (ushort)(left >= 0 ? left : left + 0x50);
			ushort breadth = (ushort)((short)wrappedLeft > 0 ? 0x50 - wrappedLeft : 0x50);
			ushort sourceTop = (ushort)(top >= 0 ? top : 0);
			ushort targetTop = (ushort)(sourceTop - top);
			ushort altitude = (ushort)(0x32 - targetTop - (top > 0 ? top : 0));

			MapFocusLeft = (ushort)left;
			MapFocusTop = (ushort)top;

			this.owner.Services.FloodBox(ScreenBox, 0, 8, 80, 50, 0);

			if (MapRevealed != 0)
			{
				left = (short)this.owner.Helpers.VerifyValueSpan(left, 0, 16);
				MapFocusLeft = (ushort)left;

				top = (short)this.owner.Helpers.VerifyValueSpan(top, 0, 65530);
				MapFocusTop = (ushort)top;

				this.owner.VideoCard.RenderImage(PictureScreenBox, (ushort)left, (ushort)top, 0x50, 0x32,
					ScreenBox, 0, 8);
			}
			else
			{
				this.owner.VideoCard.RenderImage(PictureScreenBox, (ushort)(wrappedLeft + 0xf0), sourceTop, breadth, altitude,
					ScreenBox, 0, (ushort)(targetTop + 8));

				if (breadth < 0x50)
				{
					this.owner.VideoCard.RenderImage(PictureScreenBox, 0xf0, sourceTop, (ushort)(0x50 - breadth), altitude,
						ScreenBox, breadth, (ushort)(targetTop + 8));
				}
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker == 0xff || (town.ShownExtent == 0 && town.ParticipantIdx != this.owner.World.PersonParticipantIdx))
					continue;

				short dotHoriz = (short)this.owner.Routing.TunePosition(town.Place.X - (short)wrappedLeft);
				short dotVert = (short)(town.Place.Y - sourceTop + targetTop);

				if (dotVert < 0 || dotVert >= 0x32)
					continue;

				this.owner.Services.AssignDot(dotHoriz, dotVert + 8, PlayerColour(town.ParticipantIdx));
			}

			this.owner.Dialogs.RenderShadowBox(0, 8, 79, 49, 15, 8);

			this.owner.Dialogs.RenderBox(horizLoc - left - 1, vertLoc - top + 7, 17, 10, 15);

			UnitAtTilePlayer = kept;

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("FocusMapView");
		}

		/// <summary>
		/// Renders the map tile at (`horizLoc`, `vertLoc`): either as a 4x4 block on the world map (`0xb278 == 1`), or as a 16x16
		/// tile in the main map viewport (`0xd4cc`, `0xd75e`) including coastlines, river mouths, terrain overlays, irrigation,
		/// pollution, roads/railroads, mines, special resources, huts, fortresses, fog-of-war edges, and cities.
		/// </summary>
		public ushort RenderMapTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderMapTile({horizLoc}, {vertLoc})");


			if (WorldMapView == 0x1)
			{
				ushort outcome = RenderWorldMapTileBlock(horizLoc, vertLoc);
				return FinishRenderMapTile(outcome);
			}

			short screenX = (short)((this.owner.Routing.TunePosition(horizLoc - (short)MapViewHoriz) << 4) + 0x50);

			short screenY = (short)(((vertLoc - (short)MapViewVert) << 4) + 0x8);

			if (screenX < 0x50 || screenX >= 0x140 || screenY < 0x8 || screenY > 0xc0)
			{
				return FinishRenderMapTile(0);
			}

			ushort terrain = ObtainTerrainType(horizLoc, vertLoc);

			ushort improvements = ObtainKnownTileImprovements(horizLoc, vertLoc);

			if (MapRevealed != 0)
			{
				improvements = ObtainTileImprovements(horizLoc, vertLoc);
			}

			this.owner.Startup.ConcealMouseCursor();

			if (terrain == 0xa)
			{
				RenderOceanTile(horizLoc, vertLoc, screenX, screenY);
			}

			if (terrain != 0xa)
			{
				if (VgaMode == 0)
				{
					this.owner.VideoCard.RenderImage(
						SpriteScreenBox,
						0, 0x50, 16, 16,
						ScreenBox,
						(ushort)screenX, (ushort)screenY);
				}
				else
				{
					this.owner.VideoCard.RenderImage(
						PictureScreenBox,
						0x100, 0x78, 16, 16,
						ScreenBox,
						(ushort)screenX, (ushort)screenY);
				}
			}

			if ((improvements & 0x2) != 0 &&
				terrain != 0xa &&
				UnitsHidden == 0 &&
				(improvements & 0x1) == 0)
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					SpritePicture(4));
			}

			if (terrain == 0xb)
			{
				RenderRiverTile(horizLoc, vertLoc, screenX, screenY);
			}
			else if (terrain != 0xa)
			{
				RenderLandTerrainOverlay(horizLoc, vertLoc, screenX, screenY, terrain);
			}

			RenderTileImprovementsAndFeatures(horizLoc, vertLoc, screenX, screenY, terrain, improvements);

			ushort ownerBits = ObtainTileOwnerBits(horizLoc, vertLoc);

			if ((improvements & 0x1) != 0 && UnitsHidden == 0)
			{
				RenderCityOnTile(horizLoc, vertLoc, screenX, screenY);
			}

			this.owner.Startup.DisplayMouseCursor();

			return FinishRenderMapTile(1);
		}

		private ushort FinishRenderMapTile(ushort result)
		{
			this.processor.RegAcc.Value16 = result;
			this.processor.Journal.CloseScope("RenderMapTile");
			return result;
		}

		private ushort RenderWorldMapTileBlock(int horizLoc, int vertLoc)
		{
			ushort imp = ObtainTileImprovements(horizLoc, vertLoc);
			if ((imp & 0x1) != 0)
			{
				ushort ownerBits = ObtainTileOwnerBits(horizLoc, vertLoc);
				this.owner.Services.FloodBox(
					ScreenBox,
					horizLoc * 4, vertLoc * 4, 4, 4,
					PlayerColour(ownerBits));
				return ownerBits;
			}

			ushort terrain = ObtainTerrainType(horizLoc, vertLoc);
			if (terrain == 0xa)
			{
				this.owner.Services.FloodBox(ScreenBox, horizLoc * 4, vertLoc * 4, 4, 4, 1);
				return 0xa;
			}

			this.owner.Services.FloodBox(ScreenBox, horizLoc * 4, vertLoc * 4, 4, 4, 2);
			return 0x2;
		}

		/// <summary>
		/// The neighbours of the square that fit (the directions 1 - 8 of the tables 0x1882 / 0x18e4, each or every second):
		/// one bit each, shifted in from the bit given (0xc the direction, 0x12 the mask as it grows, 0xc 9 at the end)
		/// </summary>
		private ushort MaskNeighbours(int horizLoc, int vertLoc, ushort mask, ushort step, ushort bit, Func<ushort, bool> fits)
		{
			for (ushort dir = 1; dir < 9; dir += step)
			{
				mask = (ushort)((short)mask >> 1);

				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					(short)DirectionHoriz(dir) + horizLoc);
				int adjY = (short)DirectionVert(dir) + vertLoc;

				if (fits(ObtainTerrainType((short)adjX, adjY)) && VerifyMapLimits(0, adjY) != 0)
				{
					mask |= bit;
				}
			}

			return mask;
		}

		private void RenderOceanTile(int horizLoc, int vertLoc, short screenX, short screenY)
		{
			ushort mask = 0;

			if (VgaMode == 0)
			{
				mask = MaskNeighbours(horizLoc, vertLoc, mask, 2, 0x8, terrainType => terrainType != 0xa);

				this.owner.VideoCard.RenderImage(
					SpriteScreenBox,
					(ushort)(mask << 4), 0x40, 16, 16,
					ScreenBox,
					(ushort)screenX, (ushort)screenY);
				return;
			}

			mask = MaskNeighbours(horizLoc, vertLoc, mask, 1, 0x80, terrainType => terrainType != 0xa);
			ushort rawMask = mask;

			ushort rotatedMask = (ushort)((((short)mask >> 6) & 0x3) + (mask << 2));

			for (ushort quad = 0; quad < 4; quad++)
			{
				ushort piece = (ushort)(((short)rotatedMask >> ((byte)quad << 1)) & 0x7);
				ushort sprite = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((quad << 1) + (piece << 3) + 0xd294));

				short qx = quad >= 2
					? (short)(screenX - ((quad & 1) << 3) + 8)
					: (short)(screenX + ((quad & 1) << 3));
				short qy = quad >= 2 ? (short)(screenY + 8) : screenY;

				this.owner.Services.RenderPictureToScreen(ScreenBox, qx, qy, sprite);
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
				this.owner.VideoCard.RenderImage(
					PictureScreenBox,
					(ushort)specialCoastX, 0x64, 16, 16,
					ScreenBox,
					(ushort)screenX, (ushort)screenY);
			}

			for (ushort dir = 1; dir < 9; dir += 2)
			{
				ushort adjX = (ushort)this.owner.Routing.TunePosition(
					(short)DirectionHoriz(dir) + horizLoc);
				int adjY = (short)DirectionVert(dir) + vertLoc;

				if (ObtainTerrainType((short)adjX, adjY) == 0xb)
				{
					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						screenX, screenY,
						this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(((dir / 2) << 1) + 0xd2d4)));
				}
			}

		}

		private void RenderRiverTile(int horizLoc, int vertLoc, short screenX, short screenY)
		{
			ushort mask = 0;

			for (ushort dir = 1; dir < 9; dir += 2)
			{
				mask = (ushort)((short)mask >> 1);

				ushort adjTerrain = ObtainTerrainType(
					(short)DirectionHoriz(dir) + horizLoc,
					(short)DirectionVert(dir) + vertLoc);

				if (adjTerrain == 0xb || adjTerrain == 0xa)
				{
					mask |= 0x8;
				}
			}


			if (VgaMode == 0 ||
				(this.owner.VideoCard.ObtainDot(2, horizLoc, vertLoc + 150) & 0x8) != 0)
			{
				mask = (ushort)(mask + 0x10);
			}

			this.owner.Services.RenderPictureToScreen(
				ScreenBox,
				screenX, screenY,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((mask << 1) + 0x6dfe)));
		}

		private void RenderLandTerrainOverlay(int horizLoc, int vertLoc, short screenX, short screenY, ushort terrain)
		{
			if (VgaMode != 0)
			{
				ushort mask = 0;

				mask = MaskNeighbours(horizLoc, vertLoc, mask, 2, 0x8, terrainType => terrainType == terrain);

				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((mask << 1) + (terrain << 5) + 0xb886)));

				if (terrain == 0x2 && (((byte)(7 * horizLoc + 11 * vertLoc)) & 0x2) == 0)
				{
					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						(short)(screenX + 4),
						(short)(screenY + 4),
						MiniSymbolPicture);
				}
			}
			else
			{
				ushort variant = terrain == 0x2
					? (ushort)((((byte)(11 * vertLoc + 7 * horizLoc)) & 0x2) >> 1)
					: (ushort)((horizLoc + vertLoc) & 0x1);

				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((terrain << 5) + (variant << 1) + 0xb886)));
			}
		}

		private void RenderTileImprovementsAndFeatures(int horizLoc, int vertLoc, short screenX, short screenY, ushort terrain, ushort improvements)
		{
			if ((improvements & 0x40) != 0)
			{
				if (VgaMode != 0)
				{
					this.owner.Services.RenderPictureToScreen(
						ScreenBox,
						screenX, screenY,
						TileFortressPicture);
				}
				else
				{
					this.owner.VideoCard.SubstituteHue(ScreenBox, screenX, screenY, 16, 16, 2, 0);
					this.owner.VideoCard.SubstituteHue(ScreenBox, screenX, screenY, 16, 16, 10, 15);
					this.owner.VideoCard.SubstituteHue(ScreenBox, screenX, screenY, 16, 16, 9, 8);
					this.owner.VideoCard.SubstituteHue(ScreenBox, screenX, screenY, 16, 16, 11, 0);
				}
			}

			if ((improvements & 0x8) != 0)
			{
				ushort isolatedDotHue = (improvements & 0x10) == 0 ? (ushort)0x6 : (ushort)0;

				for (ushort dir = 1; dir <= 8; dir++)
				{

					ushort adjImp = ObtainKnownTileImprovements(
						(short)DirectionHoriz(dir) + horizLoc,
						(short)DirectionVert(dir) + vertLoc);

					if ((adjImp & 0x8) != 0)
					{
						isolatedDotHue = 0xffff;

						ushort roadTable = ((improvements & 0x11) != 0 && (adjImp & 0x11) != 0) ? (ushort)0xb298 : (ushort)0xb278;
						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							screenX, screenY,
							this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((dir << 1) + roadTable)));
					}
				}


				if (isolatedDotHue != 0xffff)
				{
					this.owner.Services.FloodBox(
						ScreenBox,
						screenX + 7, screenY + 7, 2, 2,
						isolatedDotHue);
				}
			}

			if ((improvements & 0x4) != 0 && UnitsHidden == 0)
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					SpritePicture(5));
			}

			if (ContainsSpecialResource(horizLoc, vertLoc) != 0)
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					SpritePicture((terrain + 0x10)));
			}

			if (ContainsHut(terrain, horizLoc, vertLoc) != 0)
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					SpritePicture(0x1f));
			}

			if ((improvements & 0x20) != 0)
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX, screenY,
					SpritePicture(0x1e));
			}

			if (MapRevealed == 0)
			{
				ushort personBit = (ushort)(1 << (this.owner.World.PersonParticipantIdx & 0xff));

				for (ushort dir = 1; dir <= 8; dir += 2)
				{
					ushort adjX = (ushort)this.owner.Routing.TunePosition(
						(short)DirectionHoriz(dir) + horizLoc);
					ushort adjY = (ushort)(DirectionVert(dir) + (ushort)vertLoc);

					if (((ushort)(short)this.owner.World.ChartSight[adjX, adjY] & personBit) == 0)
					{
						this.owner.Services.RenderPictureToScreen(
							ScreenBox,
							screenX, screenY,
							this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(((dir / 2) << 1) + 0x7eec)));
					}
				}

			}
		}

		private void RenderCityOnTile(int horizLoc, int vertLoc, short screenX, short screenY)
		{
			ushort cityIdx = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);

			City town = this.owner.World.Towns[cityIdx];
			if (town.ParticipantIdx != this.owner.World.PersonParticipantIdx &&
				town.ShownExtent == 0 &&
				MapRevealed == 0)
			{
				return;
			}

			this.owner.Dialogs.RenderBox(screenX + 1, screenY + 1, 13, 13, 15);

			ushort townOffset = (ushort)(0x1c * cityIdx);

			short innerX = (short)(screenX + 2);

			ushort ownerWordOffset = (ushort)(town.ParticipantIdx << 1);
			this.owner.Dialogs.RenderBox(
				screenX + 2, screenY + 1, 12, 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(ownerWordOffset + 0x1956)));

			short innerY = (short)(screenY + 2);

			this.owner.Services.FloodBox(
				ScreenBox,
				innerX, innerY, 12, 12,
				PlayerColourAt(ownerWordOffset));

			this.owner.Services.RenderPictureToScreen(
				ScreenBox,
				(short)(screenX + 1),
				(short)(screenY + 1),
				SpritePicture(0x1c));

			this.owner.VideoCard.SubstituteHue(
				ScreenBox,
				innerX, innerY, 12, 12, 5,
				(byte)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(ownerWordOffset + 0x1956)));

			short displayedSize = (town.ParticipantIdx == this.owner.World.PersonParticipantIdx || MapRevealed != 0)
				? (short)town.TrueExtent
				: (short)town.ShownExtent;

			EmptyMessage()
				.Number(displayedSize);

			if ((town.ConditionMarker & 0x1) != 0)
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX + 5, innerY,
					UnhappyCitizenPicture);
			}
			else
			{
				this.owner.TextPainter.RenderTextToScreen0(
					TextBuffer.Address,
					(displayedSize < 10 ? 6 : 3) + screenX,
					screenY + 5,
					0);
			}

			if (ObtainUnitAtTile(horizLoc, vertLoc) != 0xffff || (byte)town.Unfamiliar[0] != 0xff)
			{
				this.owner.Dialogs.RenderBox(screenX, screenY, 15, 15, 0);
			}

			if (town.Has(CityStructure.CityWalls))
			{
				this.owner.Services.RenderPictureToScreen(
					ScreenBox,
					screenX + 1, screenY + 1,
					SpritePicture(0x1d));
			}

			ushort labelIdx = CityLabelCount;
			if ((short)labelIdx < 0x20)
			{
				ushort slot = (ushort)(labelIdx << 1);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(slot + 0x6dac), (ushort)screenX);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(slot + 0x6e3e), (ushort)screenY);
				CityLabelCount = (ushort)(labelIdx + 1);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(slot + 0xdf20), cityIdx);
			}
		}

		/// <summary>
		/// Draws the unit: on the world map (0xb278 = 1) as a dot of 3 x 3 pixels in the player's colour with a shadow (colour 6
		/// under settlers, 0 under the others), returning 0xc * troopIdx (left in AX by the address of the unit); else on the map window (16 pixels a tile,
		/// from 0xd4cc, 0xd75e) when it is in view and is not a land unit sleeping at sea (status bit 1), with the shadow of
		/// a stack under it.
		/// </summary>
		/// <returns>On the map window 1 when drawn, 0 when not (also in AX)</returns>
		public ushort RenderUnitOnWorldMap(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RenderUnitOnWorldMap({participantIdx}, {troopIdx})");

			sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (WorldMapView == 1)
			{
				this.processor.RegAcc.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);

				this.owner.Services.FloodBox(ScreenBox, (horizLoc * 4) + 1, (vertLoc * 4) + 1, 3, 3, (ushort)(kind == 0 ? 6 : 0));
				this.owner.Services.FloodBox(ScreenBox, horizLoc * 4, vertLoc * 4, 3, 3,
					PlayerColour(participantIdx));

				this.processor.Journal.CloseScope("RenderUnitOnWorldMap");

				return this.processor.RegAcc.Value16;
			}

			short viewHoriz = (short)((this.owner.Routing.TunePosition(horizLoc - (short)MapViewHoriz) << 4) + 0x50);
			short viewVert = (short)((((byte)vertLoc - MapViewVert) << 4) + 8);
			ushort drawn = 0;

			if (viewHoriz >= 0x50 && viewHoriz < 0x140 && viewVert >= 8 && viewVert <= 0xc0 &&
				(ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
				(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 1) == 0 ||
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
			{
				this.owner.Startup.ConcealMouseCursor();

				if (this.troops.Following(participantIdx, troopIdx) != -1)
				{
					this.owner.Services.RenderPictureToScreen(ScreenBox, viewHoriz + 1, viewVert + 1,
						SpritePicture(((participantIdx << 5) + kind + 0x40)));
				}

				RenderUnitSprite(participantIdx, troopIdx, viewHoriz, viewVert);

				this.owner.Startup.DisplayMouseCursor();

				drawn = 1;
			}

			this.processor.RegAcc.Value16 = drawn;
			this.processor.Journal.CloseScope("RenderUnitOnWorldMap");

			return drawn;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		// Colors of the hit point squares
		private const ushort VitalityStripBackdrop = 0;
		private const ushort VitalityStripComplete = 10;
		private const ushort VitalityStripBlank = 8;
		private const int VitalityStripCellBreadth = 2;
		private const int VitalityStripCellAltitude = 1;

		/// <summary>
		/// Draws the hit points of a unit as a row of flat rectangles at the top of its 16 x 16 map square:
		/// green for each hit point the unit has, gray for each lost hit point.
		/// </summary>
		/// <param name="xPos">Left side of the map square on the screen</param>
		/// <param name="yPos">Top side of the map square on the screen</param>
		public void RenderHealthBar(int horizLoc, int vertLoc, int completeStrikeMarks, int strikeMarks)
		{
			ushort display = ScreenBox;

			// The unit image covers the map square without its left column and top row,
			// the bar stays inside the unit image so it is erased together with the unit
			int breadth = Math.Min(completeStrikeMarks * (VitalityStripCellBreadth + 1) + 1, 15);
			int remaining = horizLoc + 1 + (15 - breadth) / 2;
			int head = vertLoc + 1;

			this.owner.Services.FloodBox(display, remaining, head, breadth, VitalityStripCellAltitude + 2, VitalityStripBackdrop);

			for (int idx = 0; idx < completeStrikeMarks; idx++)
			{
				this.owner.Services.FloodBox(display, remaining + 1 + idx * (VitalityStripCellBreadth + 1), head + 1,
					VitalityStripCellBreadth, VitalityStripCellAltitude, idx < strikeMarks ? VitalityStripComplete : VitalityStripBlank);
			}
		}

		/// <summary>
		/// Draws the unit's picture at the position with the marks of its status: the fortification (status bit 8), the
		/// mark 0x2ba0 (bit 4), the "go to" mark 0x2ba2 of the human player's units, a letter in a box for
		/// the orders of a unit that is not a plane (bits 0xc2: "R", "I"/"?" for bit 0x40, "M"/"F" for bit 0x80, "P" for bit 2
		/// with 0x80), faded colours when sleeping (bit 1), and its hit points. AX is left 0x600 * participantIdx (from the address
		/// of the unit), like in the original.
		/// </summary>
		public void RenderUnitSprite(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderUnitSprite({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);
			ushort markHue = (ushort)(participantIdx == 1 ? 9 : 15);

			this.owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc,
				SpritePicture((kind + (participantIdx << 5) + 0x40)));

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 8) != 0)
			{
				this.owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc, SpritePicture(0x1d));
			}
			else if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 4) != 0)
			{
				this.owner.TextPainter.RenderTextWithShadow(0x2ba0, horizLoc + 4, vertLoc + 7, markHue);
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx && this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				this.owner.TextPainter.RenderTextWithShadow(0x2ba2, horizLoc + 4, vertLoc + 7, markHue);
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0 &&
				UnitKindWord((sbyte)kind, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
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

				this.owner.Words.Set(" ");
				this.processor.EmitGlobalByte(TextBuffer.Address, letter);

				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, horizLoc + 4, vertLoc + 7, markHue);

				this.owner.Dialogs.RenderBox(horizLoc - 1, vertLoc - 1, 15, 15, 7);
			}

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 1) != 0)
			{
				this.owner.VideoCard.SubstituteHue(ScreenBox, horizLoc, vertLoc, 16, 16, 5, 7);

				this.owner.VideoCard.SubstituteHue(ScreenBox, horizLoc, vertLoc, 16, 16, 8, 7);
			}

			{
				// Every unit shows its hit points: green squares, and gray squares for lost hit points until it heals
				int troopKind = this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);
				int missingStrikeMarks = this.owner.World.Vitals.ObtainLostHitPoints(participantIdx, troopIdx, troopKind);

				if (troopKind != 0xff)
				{
					bool seasoned = (this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Veteran) != 0;
					int completeStrikeMarks = this.owner.World.Vitals.ObtainFullHitPoints(participantIdx, troopIdx, troopKind, seasoned);

					RenderHealthBar(horizLoc - 1, vertLoc - 1, completeStrikeMarks, completeStrikeMarks - missingStrikeMarks);
				}
			}

			this.processor.Journal.CloseScope("RenderUnitSprite");
		}

		/// <summary>
		/// Draws the tile again and, unless units are hidden (0xdcfc), the unit on it: when the human sees it (all units are
		/// shown with 0xd806, the human's own, or the unit's bit of the human), and the tile has no city
		/// </summary>
		public void RepaintMapTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintMapTile({horizLoc}, {vertLoc})");

			RenderMapTile(horizLoc, vertLoc);

			if (UnitsHidden == 0)
			{
				ushort troopIdx = ObtainUnitAtTile(horizLoc, vertLoc);

				if (troopIdx != 0xffff)
				{
					short ownerIdx = (short)UnitAtTilePlayer;
					short personIdx = this.owner.World.PersonParticipantIdx;

					bool seen = MapRevealed != 0 || ownerIdx == personIdx ||
						(this.troops.Pull(ownerIdx, troopIdx, UnitMemory.ShownToParticipants) & (1 << personIdx)) != 0;

					if (seen && (ObtainTileImprovements(horizLoc, vertLoc) & 0x1) == 0)
					{
						RepaintUnitTile((short)UnitAtTilePlayer, (short)troopIdx);
					}
				}
			}

			this.processor.Journal.CloseScope("RepaintMapTile");
		}

		/// <summary>
		/// Draws the unit shown on the tile of the unit: on terrain 10, a non-ship unit in a stack is drawn as the first ship
		/// of its stack (following the ring from the unit; itself when there is none); otherwise the best defender of the stack
		/// </summary>
		public void RepaintUnitTile(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RepaintUnitTile({participantIdx}, {troopIdx})");

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
						RenderUnitOnWorldMap(participantIdx, current);
						current = -1;
					}
				}
				while (current != -1 && current != troopIdx);

				if (current == troopIdx)
				{
					RenderUnitOnWorldMap(participantIdx, troopIdx);
				}
			}
			else
			{
				RenderUnitOnWorldMap(participantIdx, (short)this.owner.UnitOps.LocateBestGuardianInStack(participantIdx, troopIdx));
			}

			this.processor.Journal.CloseScope("RepaintUnitTile");
		}

		/// <summary>
		/// Is the position on the map
		/// </summary>
		/// <returns>1 on the map, 0 outside</returns>
		public ushort VerifyMapLimits(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyMapLimits({horizLoc}, {vertLoc})");

			ushort onMap = ReturnAcc(MapRules.IsOnMap(horizLoc, vertLoc) ? 1 : 0);

			this.processor.Journal.CloseScope("VerifyMapLimits");

			return onMap;
		}

		/// <summary>
		/// Terrain type of the tile (MapRules.OceanTerrain is the ocean)
		/// </summary>
		public ushort ObtainTerrainType(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTerrainType({horizLoc}, {vertLoc})");

			ushort terrain = ReturnAcc(this.chart.Terrain(horizLoc, vertLoc));

			this.processor.Journal.CloseScope("ObtainTerrainType");

			return terrain;
		}

		/// <summary>
		/// The player of the tile owner layer (bits 0 - 2), also when no units stand there
		/// </summary>
		public ushort ObtainTileOwnerBits(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc) & 0x7);
		}

		/// <summary>
		/// Sets the player of the tile owner layer (of a city), keeps the units bit
		/// </summary>
		public void MapAssignCityOwner(short participantIdx, int horizLoc, int vertLoc)
		{
			int bits = ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc));

			this.chart.AssignOwnerBits(horizLoc, vertLoc, (bits & MapLayers.UnitsPresent) + participantIdx);
		}

		/// <summary>
		/// Puts the unit on the tile: into the stack of the units standing there, and marks the tile as having units of the player
		/// </summary>
		public void SituateUnitOnTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"SituateUnitOnTile({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			ushort stackTroopIdx = ObtainUnitAtTile(horizLoc, vertLoc);

			if (stackTroopIdx != 0xffff)
			{
				this.owner.Combat.AttachUnitToStack(participantIdx, troopIdx, stackTroopIdx);
			}

			AssignChartDot(horizLoc + 160, vertLoc, (ushort)(participantIdx + MapLayers.UnitsPresent));

			this.processor.Journal.CloseScope("SituateUnitOnTile");
		}

		/// <summary>
		/// Writes a value of a map layer through SystemServices.AssignDot (bounds of the current screen), as the original
		/// </summary>
		private void AssignChartDot(int horizLoc, int vertLoc, ushort style)
		{
			this.owner.Services.AssignDot(2, horizLoc, vertLoc, style);
		}

		/// <summary>
		/// Takes the unit off the tile: out of its stack, or when it stood alone, the tile has no units any more
		/// </summary>
		public void DiscardUnitFromTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"DiscardUnitFromTile({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.owner.Combat.DiscardUnitFromStack(participantIdx, troopIdx);
			}
			else
			{
				AssignChartDot(horizLoc + 160, vertLoc, (byte)participantIdx);
			}

			this.processor.Journal.CloseScope("DiscardUnitFromTile");
		}

		/// <summary>
		/// The first unit standing on the tile. Sets the player of the unit into 0xd7f0 and 0xd20a.
		/// When the tile owner layer says that units are there but none is found, the layer is cleared.
		/// </summary>
		/// <returns>Unit index, or 0xffff when no unit stands on the tile</returns>
		public ushort ObtainUnitAtTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainUnitAtTile({horizLoc}, {vertLoc})");

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
					FoundPlayer = participantIdx;
					UnitAtTilePlayer = participantIdx;
				}
				else
				{
					this.owner.Services.AssignDot(2, horizLoc + 160, vertLoc, 0);
				}
			}

			ReturnAcc(found);

			this.processor.Journal.CloseScope("ObtainUnitAtTile");

			return found;
		}

		/// <summary>
		/// The player whose units stand on the tile
		/// </summary>
		/// <returns>Player, or 0xffff when no units stand on the tile</returns>
		public ushort ObtainTileOwner(int horizLoc, int vertLoc)
		{
			int bits = this.chart.OwnerBits(horizLoc, vertLoc);

			return ReturnAcc((bits & MapLayers.UnitsPresent) != 0 ? bits & 0x7 : 0xffff);
		}

		/// <summary>
		/// Marks that units stand on the tile, keeps the player
		/// </summary>
		public void TagTileOwned(int horizLoc, int vertLoc)
		{
			int bits = ReturnAcc(this.chart.OwnerBits(horizLoc, vertLoc));

			this.chart.AssignOwnerBits(horizLoc, vertLoc, (bits & 0x7) + MapLayers.UnitsPresent);
		}

		/// <summary>
		/// The road bit (8) of the tile improvements
		/// </summary>
		public ushort ContainsRoad(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Improvements(horizLoc, vertLoc) & 0x8);
		}

		/// <summary>
		/// Improvements of the tile (bit 0 city, 1 irrigation, 2 mine, 3 road, ...)
		/// </summary>
		public ushort ObtainTileImprovements(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Improvements(horizLoc, vertLoc));
		}

		/// <summary>
		/// Improvements of the tile as the human player saw them last time
		/// </summary>
		public ushort ObtainKnownTileImprovements(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainKnownTileImprovements({horizLoc}, {vertLoc})");

			ushort known = ReturnAcc(this.chart.KnownImprovements(horizLoc, vertLoc));

			this.processor.Journal.CloseScope("ObtainKnownTileImprovements");

			return known;
		}

		/// <summary>
		/// The human player sees the improvements of the tile as they are now
		/// </summary>
		public void KeepKnownTileImprovements(int horizLoc, int vertLoc)
		{
			this.chart.RevealImprovements(horizLoc, vertLoc);

			ReturnAcc(this.chart.Improvements(horizLoc, vertLoc) >> 4);
		}

		/// <summary>
		/// Adds improvements to the tile: 0 removes all, a value from 0x10 sets the high bits, a smaller one the low bits.
		/// In the turn of the human player the human sees the change at once.
		/// </summary>
		public void AssignTileImprovements(ushort improvements, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"AssignTileImprovements({improvements}, {horizLoc}, {vertLoc})");

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

			if ((short)TurnPlayer == this.owner.World.PersonParticipantIdx)
			{
				KeepKnownTileImprovements(horizLoc, vertLoc);
			}

			this.processor.Journal.CloseScope("AssignTileImprovements");
		}

		/// <summary>
		/// Removes the improvement bits from the tile
		/// </summary>
		public void DiscardTileImprovements(ushort improvements, int horizLoc, int vertLoc)
		{
			this.chart.ClearImprovements(improvements, horizLoc, vertLoc);
		}

		/// <summary>
		/// The city on the tile. Sets the player of the tile owner layer into 0xd7f0 and 0xd20a.
		/// When there is no city, an error message is shown.
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		public ushort LocateCityAtTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateCityAtTile({horizLoc}, {vertLoc})");

			ushort participantIdx = ObtainTileOwnerBits(horizLoc, vertLoc);
			ushort found = 0xffff;

			for (int townIdx = 0; townIdx < this.owner.World.Towns.Length; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.Place.X == horizLoc && town.Place.Y == vertLoc)
				{
					FoundPlayer = participantIdx;
					UnitAtTilePlayer = participantIdx;
					found = (ushort)townIdx;
					break;
				}
			}

			if (found == 0xffff)
			{
				this.owner.TurnHooks.DisplayDialog(0x2bc6, 100, 80);
			}

			ReturnAcc(found);

			this.processor.Journal.CloseScope("LocateCityAtTile");

			return found;
		}

		/// <summary>
		/// Does the tile have a special resource (MapRules.HasSpecialResource with the world seed)
		/// </summary>
		public ushort ContainsSpecialResource(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsSpecialResource({horizLoc}, {vertLoc})");

			ushort special = ReturnAcc(MapRules.HasSpecialResource(horizLoc, vertLoc, this.owner.World.ChanceRoot) ? 1 : 0);

			this.processor.Journal.CloseScope("ContainsSpecialResource");

			return special;
		}

		/// <summary>
		/// Is there a hut (tribal village) on the tile: not on the ocean, on a tile nobody has visited and without a city
		/// </summary>
		/// <param name="terrain">Terrain type of the tile</param>
		public ushort ContainsHut(ushort terrain, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsHut({terrain}, {horizLoc}, {vertLoc})");

			bool hut = terrain != MapRules.OceanTerrain &&
				(this.owner.World.ChartSight[horizLoc, vertLoc] & 1) == 0 &&
				MapRules.IsHutPlace(horizLoc, vertLoc, this.owner.World.ChanceRoot) &&
				(ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			ushort result = ReturnAcc(hut ? 1 : 0);

			this.processor.Journal.CloseScope("ContainsHut");

			return result;
		}

		/// <summary>
		/// Number of the continent or the sea of the tile
		/// </summary>
		public ushort ObtainContinentID(int horizLoc, int vertLoc)
		{
			return ReturnAcc(this.chart.Continent(horizLoc, vertLoc));
		}

		/// <summary>
		/// Size of the continent (land) or the sea (ocean) of the tile, from the word arrays at 0xdcfe and 0xdd7e
		/// </summary>
		public ushort ObtainLandMassSize(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainLandMassSize({horizLoc}, {vertLoc})");

			ushort sizes = ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ? (ushort)0xdcfe : (ushort)0xdd7e;
			int continent = ObtainContinentID(horizLoc, vertLoc);
			ushort size = ReturnAcc(this.processor.PullGlobalWord((ushort)(sizes + (continent << 1))));

			this.processor.Journal.CloseScope("ObtainLandMassSize");

			return size;
		}
	}
}
