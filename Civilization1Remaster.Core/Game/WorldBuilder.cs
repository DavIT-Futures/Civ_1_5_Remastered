using System;

namespace Civilization1Remaster.Core.Game
{
	public class WorldBuilder : GameComponent, IWorldBuilder
	{
		private const ushort SeaBlocksAddress = 0x7f38;
		private const ushort SeaBlocksAlternateAddress = 0x802f;
		private const ushort StorySegment = 0x3710;
		private const ushort StoryTimingTableOffset = 0x83;

		/// <summary>The words of the frame of ProduceTerrain, shared by its helpers (B3: in C#, new for every call)</summary>
		private FrameWords terrainFrame = null!;

		/// <summary>The words of the frame of ReckonContinentSizes, shared by its helpers (B3: in C#, new for every call)</summary>

		public WorldBuilder(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void OpeningSequence()
		{
			using var _ = this.Processor.Journal.Scope("OpeningSequence()");

			this.Owner.Startup.AskForDisk(7, 1);

			ushort screenZero = ScreenBox;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, screenZero, 0x1);
			StoryKeyAllowed = 0x0;

			if (EarthGame != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileMapPic, 1);
			}
			else
			{
				this.ShowStoryIntro();

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

				do
				{
					this.ProduceCloudLayer();
					this.ShowStoryIntro();
				}
				while ((short)(((LandMass << 3) * 0x28) + 0x280) > (short)LightenedPixels);

				this.Owner.Dialogs.DrawBox(0, 0, 79, 49, Hues.Black);
				this.Owner.Dialogs.DrawBox(1, 1, 77, 47, Hues.Black);

				SmoothDiagonalCoastlines();
				AssignLatitudeAndElevationTerrain();
				ApplyMoisturePasses();
				AgeTerrain();

				this.ProduceElevation();
			}

			this.Owner.Services.FloodBox(ScreenBox, 160, 0, 160, 200, Hues.Black);

			this.ProduceTerrain();

			StoryKeyAllowed = 0x1;

			// The story again while it waits (ShowStoryIntro gives back 1)
			while (this.ShowStoryIntro() != 0)
			{
			}

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.VideoCard.RenderImage(
				ScreenBox,
				0, 0, 0x140, 0xc8,
				PictureScreenBox,
				0, 0);
		}

		/// <inheritdoc/>
		public void BlendTerrainTile(short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"BlendTerrainTile({horizLoc}, {vertLoc})");

			if (this.Owner.VideoCard.ObtainDot(1, horizLoc - 1, vertLoc - 1) != 1 &&
				this.Owner.VideoCard.ObtainDot(1, horizLoc - 1, vertLoc + 1) != 1 &&
				this.Owner.VideoCard.ObtainDot(1, horizLoc + 1, vertLoc - 1) != 1 &&
				this.Owner.VideoCard.ObtainDot(1, horizLoc + 1, vertLoc + 1) != 1)
			{
				this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 1);
			}
		}

		/// <inheritdoc/>
		public void ProduceElevation()
		{
			using var _ = this.Processor.Journal.Scope($"ProduceElevation()");

			IMapPlanes chart = this.Owner.MapOps.Chart;
			short riverTally = 0;
			short attemptTally = 0;

			while (true)
			{
				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0xa0, 0x64, ScreenBox, 0xa0, 0);

				short horizLoc;
				short vertLoc;

				do
				{
					horizLoc = (short)this.Owner.Libc.Dice.DrawInt(80);
					vertLoc = (short)this.Owner.Libc.Dice.DrawInt(50);
				} while (this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc) != 0xc);

				short springHoriz = horizLoc;
				short springVert = vertLoc;
				short heading = (short)(this.Owner.Libc.Dice.DrawInt(4) << 1);
				short length = 0;
				bool nearOcean;
				ushort reached;

				do
				{
					this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 9);

					nearOcean = false;

					for (int side = 1; side <= 8; side += 2)
					{
						if (this.Owner.VideoCard.ObtainDot(1, chart.DirectionHoriz(side) + horizLoc, chart.DirectionVert(side) + vertLoc) == 1)
						{
							nearOcean = true;
						}
					}

					short previousHeading = heading;
					heading = (short)(((((short)this.Owner.Libc.Dice.DrawInt(2) - (length & 1)) << 1) + heading) & 7);

					if ((previousHeading ^ 4) > heading)
					{
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc + 0x32, 8);
					}

					// The steps of the headings 0 - 7 are the directions 1 - 8
					horizLoc += (short)chart.DirectionHoriz(heading + 1);
					vertLoc += (short)chart.DirectionVert(heading + 1);

					reached = this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

					length++;
				} while (!nearOcean && reached != 0xd && reached != 1 && reached != 9);

				if ((nearOcean || reached == 9) && length >= 5)
				{
					riverTally++;

					for (int spotIdx = 1; spotIdx <= 0x15; spotIdx++)
					{
						horizLoc = (short)(chart.DirectionHoriz(spotIdx) + springHoriz);
						vertLoc = (short)(chart.DirectionVert(spotIdx) + springVert);

						if (this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc) == 2)
						{
							this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 11);
						}
					}
				}
				else
				{
					this.Owner.VideoCard.RenderImage(ScreenBox, 0xa0, 0, 0xa0, 0x64, ScreenBox, 0, 0);
				}

				ShowStoryIntro();

				attemptTally++;

				if (attemptTally >= 0x100 ||
					(short)(((LandMass + Climate) << 1) + 6) <= riverTally)
					break;
			}

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0x32, 0x50, 0x32, ScreenBox, 0, 0x96);

			this.Owner.Services.FloodBox(ScreenBox, 0, 50, 80, 50, Hues.Black);
		}

		/// <inheritdoc/>
		public void ProduceCloudLayer()
		{
			using var _ = this.Processor.Journal.Scope("ProduceCloudLayer()");

			this.Owner.Services.FloodBox(ScreenBox, 160, 0, 80, 50, Hues.Black);

			short cloudHoriz = (short)(this.Owner.Libc.Dice.DrawInt(72) + 4);
			short cloudVert = (short)(this.Owner.Libc.Dice.DrawInt(34) + 8);

			ProduceCloud(cloudHoriz, cloudVert);

			for (int horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					if (this.Owner.VideoCard.ObtainDot(1, horizLoc + 0xa0, vertLoc) != 0)
					{
						ushort hue = this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, (ushort)(hue + 1));

						LightenedPixels = (ushort)(LightenedPixels + 1);
					}
				}
			}
		}

		/// <inheritdoc/>
		public void ProduceCloud(short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ProduceCloud({horizLoc}, {vertLoc})");

			int vaporExtent = this.Owner.Libc.Dice.DrawInt(64) + 1;

			do
			{
				this.Owner.Services.AssignDot(1, horizLoc + 160, vertLoc, 15);
				this.Owner.Services.AssignDot(1, horizLoc + 161, vertLoc, 15);
				this.Owner.Services.AssignDot(1, horizLoc + 160, vertLoc + 1, 15);

				switch (this.Owner.Libc.Dice.DrawInt(4))
				{
					case 0:
						horizLoc += 0;
						vertLoc += -1;
						break;

					case 1:
						horizLoc += 1;
						vertLoc += 0;
						break;

					case 2:
						horizLoc += 0;
						vertLoc += 1;
						break;

					case 3:
						horizLoc += -1;
						vertLoc += 0;
						break;
				}

				vaporExtent--;

			} while (vaporExtent > 0 && horizLoc > 2 && horizLoc < 77 && vertLoc > 2 && vertLoc < 46);
		}

		/// <inheritdoc/>
		public void ProduceTerrain()
		{
			using var _ = this.Processor.Journal.Scope("ProduceTerrain()");

			// ClassicLabelNumbers: the frame of the original, as the numbers of the labels past the 63 ranked ones are read past
			// their table, above BP (the frames of the callers) and under the frame (the saved SI / DI); without it they are 15
			// and the words stay in C#
			bool classicFrame = this.Owner.TurnHooks.ClassicLabelNumbers;

			if (classicFrame)
				this.Processor.EnterFrame(0xd4);

			FrameWords outerFrame = this.terrainFrame;
			this.terrainFrame = new FrameWords(this.Processor, 0xd4);

			if (classicFrame)
				this.Processor.PreserveIndexRegisters();

			const int terrainCounts = 0x20;

			for (ushort terrain = 0; terrain < 0xc; terrain++)
			{
				this.terrainFrame.SetFrame(terrainCounts - (terrain << 1), 0);
			}

			ushort pass;

			for (pass = 0; pass < 2; pass++)
			{
				this.terrainFrame.SetFrame(0x8, pass);

				LabelLandOrSea(pass, terrainCounts);

				RankLandOrSea(pass);
			}

			this.terrainFrame.SetFrame(0x8, pass);

			ShowStoryIntro();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, PictureScreenBox, 0, 0);

			LabelLandmasses();

			ReckonContinentSizes(1);

			ShowStoryIntro();

			PlaceSpecialTerrain();

			this.Owner.VideoCard.RenderLine(ScreenBox, 0, 0, 79, 0, Hues.White);
			this.Owner.VideoCard.RenderLine(ScreenBox, 0, 49, 79, 49, Hues.White);

			ushort dotIdx;

			for (dotIdx = 0; dotIdx < 0x14; dotIdx++)
			{
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 0, 7);
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 1, 7);
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 48, 7);
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 49, 7);
			}

			this.terrainFrame.SetFrame(0x24, dotIdx);

			if (classicFrame)
				this.Processor.RestoreIndexRegisters();

			this.terrainFrame = outerFrame;

			if (classicFrame)
				this.Processor.LeaveFrame();
		}

		/// <inheritdoc/>
		public void PlaceSpecialTerrain()
		{
			using var _ = this.Processor.Journal.Scope("SituateSpecialTerrain()");

			for (int continentIdx = 0; continentIdx < 0x10; continentIdx++)
			{
				this.Owner.World.Landmasses[continentIdx].ConstructionSpotTally = 0;
			}

			ushort[] weights = new ushort[0x18];

			for (int terrainIdx = 0; terrainIdx < 0x18; terrainIdx++)
			{
				int kind = terrainIdx % 12;

				weights[terrainIdx] = (ushort)((3 * (sbyte)TerrainField(terrainIdx, TerrainTable.Food)) +
					(sbyte)TerrainField(terrainIdx, TerrainTable.Trade));

				if (kind != 2 && kind != 11)
				{
					weights[terrainIdx] += (ushort)((sbyte)TerrainField(terrainIdx, TerrainTable.Shields) << 1);
				}

				short mining = (short)TerrainImprovementWord(kind, TerrainImprovementTable.Mining);

				if (mining < 0)
				{
					weights[terrainIdx] += (ushort)(0xffff - mining);
				}
				else
				{
					short irrigation = (short)TerrainImprovementWord(kind, TerrainImprovementTable.Irrigation);

					if (irrigation < 0)
					{
						weights[terrainIdx] += (ushort)((0xffff - irrigation) << 1);
					}
				}
			}

			for (short horizLoc = 2; horizLoc < 0x4e; horizLoc++)
			{
				for (short vertLoc = 2; vertLoc < 0x30; vertLoc++)
				{
					ushort terrain = ObtainTerrainTypeFromScreen(horizLoc, vertLoc);

					if (terrain != 0xb && terrain != 2 && terrain != 1)
					{
						this.Owner.Services.AssignDot(horizLoc + 80, vertLoc, 0);
						this.Owner.Services.AssignDot(horizLoc + 80, vertLoc + 0x32, 0);
						continue;
					}

					short total = 0;

					for (int deltaIdx = 0; deltaIdx <= 0x14; deltaIdx++)
					{
						short value = 0;
						short tileHoriz = (short)(this.Owner.TownDeltas[deltaIdx].X + horizLoc);
						short tileVert = (short)(this.Owner.TownDeltas[deltaIdx].Y + vertLoc);

						ushort tile = ObtainTerrainTypeFromScreen(tileHoriz, tileVert);

						if ((tile == 2 || tile == 0xb) && !OffRiverPattern(tileHoriz, tileVert))
						{
							value += 2;
						}

						if (this.Owner.MapOps.ContainsSpecialResource(tileHoriz, tileVert) != 0)
						{
							tile += 0xc;
						}

						value += (short)weights[tile];

						if (deltaIdx < 9)
						{
							value += value;
						}

						if (deltaIdx == 0)
						{
							value += value;
						}

						total += value;
					}

					if (ObtainTerrainTypeFromScreen(horizLoc, vertLoc) != 1 && OffRiverPattern(horizLoc, vertLoc))
					{
						total -= 0x10;
					}

					short worth = (short)(((short)this.Owner.Helpers.ClampValue((total - 120) / 8, 1, 15) / 2) + 8);

					this.Owner.Services.AssignDot(horizLoc + 80, vertLoc, (ushort)worth);
					this.Owner.Services.AssignDot(horizLoc + 80, vertLoc + 0x32, (ushort)worth);

					this.Owner.World.Landmasses[ObtainContinentIDFromScreen(horizLoc, vertLoc)].ConstructionSpotTally++;
				}
			}
		}

		/// <inheritdoc/>
		public void LabelLandmasses()
		{
			using var _ = this.Processor.Journal.Scope("LabelLandmasses()");

			this.Owner.Libc.FillBytes(0xdb44, 0, 0x104);

			(short, short)? land = null;
			(short, short)? neighbourLand = null;

			for (short column = 0; column < 0x14; column++)
			{
				for (short row = 0; row < 0xd; row++)
				{
					short horizLoc = (short)((column << 2) + 1), vertLoc = (short)((row << 2) + 1);

					short continent = FindBlockLand(horizLoc, vertLoc, ref land);

					if (continent == -1)
						continue;

					for (short direction = 1; direction <= 4; direction++)
					{
						short nextHoriz = (short)((DirectionHoriz(direction) << 2) + horizLoc);
						short nextVert = (short)((DirectionVert(direction) << 2) + vertLoc);

						short nextContinent = FindBlockLand(nextHoriz, nextVert, ref neighbourLand);

						if (continent != nextContinent)
							continue;

						short cost = (short)this.Owner.Routing.ComputeRouteCost(land!.Value.Item1, land.Value.Item2,
							neighbourLand!.Value.Item1, neighbourLand.Value.Item2, 0, 20);

						if (cost == -1 || cost >= 0x14)
							continue;

						LinkLandmassBlock(column, row, direction - 1);

						short nextColumn = (short)(DirectionHoriz(direction) + column);
						short nextRow = (short)(DirectionVert(direction) + row);

						if (nextColumn < 0 || nextColumn >= 0x14 || nextRow < 0 || nextRow >= 0xd)
							continue;

						LinkLandmassBlock(nextColumn, nextRow, (direction + 3) & 7);
					}
				}
			}
		}

		/// <inheritdoc/>
		public void ReckonContinentSizes(ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ReckonContinentSizes({marker})");

			this.Owner.Libc.FillBytes(SeaBlocksAddress, 0, 0x104);

			short lastNextSea = 0;

			for (short column = 0; column < 0x14; column++)
			{
				for (short row = 0; row < 0xc; row++)
				{
					LinkSeaBlock(column, row, ref lastNextSea);
				}

				if (marker != 0)
				{
					ShowStoryIntro();
				}
			}

			for (ushort row = 0; row < 0xc; row++)
			{
				this.Processor.EmitGlobalByte((ushort)(row + SeaBlocksAddress), (byte)(this.Processor.PullGlobalByte((ushort)(row + SeaBlocksAddress)) | 0xe0));
				this.Processor.EmitGlobalByte((ushort)(row + SeaBlocksAlternateAddress), (byte)(this.Processor.PullGlobalByte((ushort)(row + SeaBlocksAlternateAddress)) | 0xe));
			}

			this.Processor.EmitGlobalByte(SeaBlocksAddress, (byte)(this.Processor.PullGlobalByte(SeaBlocksAddress) & 0x7f));
			this.Processor.EmitGlobalByte((ushort)(SeaBlocksAddress + 11), (byte)(this.Processor.PullGlobalByte((ushort)(SeaBlocksAddress + 11)) & 0xdf));
			this.Processor.EmitGlobalByte(SeaBlocksAlternateAddress, (byte)(this.Processor.PullGlobalByte(SeaBlocksAlternateAddress) & 0xfd));
			this.Processor.EmitGlobalByte((ushort)(SeaBlocksAlternateAddress + 11), (byte)(this.Processor.PullGlobalByte((ushort)(SeaBlocksAlternateAddress + 11)) & 0xf7));
		}

		/// <inheritdoc/>
		public ushort ObtainTerrainTypeFromScreen(short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTerrainTypeFromScreen({horizLoc}, {vertLoc})");

			ushort displayIdx = DrawingBox.DisplayIdx;
			ushort colour = this.Owner.VideoCard.ObtainDot(displayIdx, horizLoc, vertLoc);
			ushort terrain = this.Processor.PullGlobalWord((ushort)(MapPlanes.TerrainOfColour + (colour << 1)));


			return terrain;
		}

		/// <inheritdoc/>
		public ushort ObtainContinentIDFromScreen(short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainContinentIDFromScreen({horizLoc}, {vertLoc})");

			ushort dot = this.Owner.VideoCard.ObtainDot(
				DrawingBox.DisplayIdx,
				horizLoc, vertLoc + 50);

			return dot;
		}

		/// <inheritdoc/>
		public ushort ShowStoryIntro()
		{
			using var _ = this.Processor.Journal.Scope("DisplayStoryIntro()");

			DrawingBox.DisplayIdx = 0x0;

			bool waiting = StoryStep();

			ushort result = (ushort)(waiting ? 1 : 0);

			return result;
		}

		/// <inheritdoc/>
		public void EnactGlobalWarming(short worldHeatingTally)
		{
			using var _ = this.Processor.Journal.Scope($"EnactGlobalWarming({worldHeatingTally})");

			this.Owner.Words.Set("Global temperature\nrises! Icecaps melt.\nSevere Drought.\n");

			this.Owner.Paper.ShowNewspaper(-1);

			IMapPlanes chart = this.Owner.MapOps.Chart;

			for (int horizLoc = 0; horizLoc < 80; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 50; vertLoc++)
				{
					ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

					if ((short)terrain > 3)
						continue;

					short oceanTally = 0;

					for (int heading = 1; heading <= 8; heading++)
					{
						if (this.Owner.MapOps.ObtainTerrainType(chart.DirectionHoriz(heading) + horizLoc, chart.DirectionVert(heading) + vertLoc) == MapRules.OceanTerrain)
						{
							oceanTally++;
						}
					}

					if (7 - worldHeatingTally <= oceanTally)
					{
						this.Owner.World.ChartSight[horizLoc, vertLoc] |= 1;
						this.Owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)(terrain != 3 ? 3 : 11));
						this.Owner.MapOps.RemoveTileImprovements(6, horizLoc, vertLoc);
					}
					else if ((((11 * horizLoc) + (13 * vertLoc)) & 7) == (ushort)worldHeatingTally)
					{
						this.Owner.World.ChartSight[horizLoc, vertLoc] |= 1;
						this.Owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)((short)terrain <= 1 ? 14 : 6));
					}
					else
					{
						continue;
					}

					if ((this.Owner.World.ChartSight[horizLoc, vertLoc] & (1 << this.Owner.World.PersonParticipantIdx)) != 0)
					{
						this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
					}
				}
			}
		}

		/// <summary>The tile is off the pattern of 7 * x + 11 * y (bit 1 of the low byte set)</summary>
		private static bool OffRiverPattern(short horizLoc, short vertLoc)
		{
			return (((7 * horizLoc) + (0xb * vertLoc)) & 2) != 0;
		}

		/// <summary>
		/// Fills diagonal 2x2 land checkerboards (mask 6 or 9) on screen 1 with land (colour 1), stepping back one column/row
		/// to re-check adjacent 2x2 blocks.
		/// </summary>
		private void SmoothDiagonalCoastlines()
		{
			short horizLoc = 1;

			while (horizLoc < 0x4f)
			{
				short vertLoc = 1;

				while (vertLoc < 0x31)
				{
					byte mask = 0;

					if (this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc) != 0)
					{
						mask |= 0x1;
					}

					if (this.Owner.VideoCard.ObtainDot(1, horizLoc + 1, vertLoc) != 0)
					{
						mask |= 0x2;
					}

					if (this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc + 1) != 0)
					{
						mask |= 0x4;
					}

					if (this.Owner.VideoCard.ObtainDot(1, horizLoc + 1, vertLoc + 1) != 0)
					{
						mask |= 0x8;
					}

					if (mask == 0x6 || mask == 0x9)
					{
						this.Owner.Services.AssignDot(1, horizLoc + 1, vertLoc, 1);
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc + 1, 1);
						this.Owner.Services.AssignDot(1, horizLoc + 1, vertLoc + 1, 1);

						if (horizLoc != 0)
						{
							horizLoc--;
						}

						if (vertLoc != 0)
						{
							vertLoc--;
						}
					}

					vertLoc++;
				}

				this.ShowStoryIntro();

				horizLoc++;
			}
		}

		/// <summary>
		/// Converts cloud height counts on screen 1 into ocean (1), hills (12), mountains (13), or latitude-dependent
		/// flatland (desert 14, plains 6, tundra 7, arctic 15).
		/// </summary>
		private void AssignLatitudeAndElevationTerrain()
		{
			short horizLoc;

			for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				short vertLoc;

				for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					ushort dot = this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

					if (dot == 0)
					{
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 1);
					}
					else if (dot == 1)
					{
						short rawLat = (short)-((short)(this.Owner.Libc.Dice.DrawInt(8) + vertLoc - 0x1d));
						ushort absLat = (ushort)this.Owner.Libc.AbsoluteValue(rawLat);
						short band = (short)(absLat + (1 - (short)Temperature));

						ushort tier = (ushort)(CPU.SignedQuotient(band, 6) + 1);
						if (tier <= 7)
						{
							ushort colour = (tier / 2) switch
							{
								0 => 14,
								1 => 6,
								2 => 7,
								_ => 15,
							};
							this.Owner.Services.AssignDot(1, horizLoc, vertLoc, colour);
						}
					}
					else if (dot == 2)
					{
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 12);
					}
					else
					{
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 13);
					}
				}


				this.ShowStoryIntro();
			}
		}

		/// <summary>
		/// Sweeps each row west-to-east and then east-to-west, picking up moisture over ocean (1) and depositing rainfall
		/// onto land tiles according to climate setting 0x7efa.
		/// </summary>
		private void ApplyMoisturePasses()
		{
			short vertLoc;

			for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
			{
				short latitudeDist = (short)this.Owner.Libc.AbsoluteValue((short)(0x19 - vertLoc));

				short moisture = 0;

				short horizLoc;

				for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					ushort dot = this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

					if (dot == 1)
					{
						short cap = (short)(this.Owner.Libc.AbsoluteValue((short)(0xc - latitudeDist)) + (Climate << 2));
						if (cap > moisture)
						{
							moisture++;
						}
					}
					else if (moisture > 0)
					{
						ushort drop = (ushort)this.Owner.Libc.Dice.DrawInt((ushort)(7 - (Climate << 1)));
						moisture = (short)(moisture - drop);

						switch (dot)
						{
							case 0x6:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 10);
								break;
							case 0x7:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 15);
								break;
							case 0xc:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 2);
								break;
							case 0xd:
								moisture -= 3;
								break;
							case 0xe:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 6);
								break;
						}
					}
				}


				moisture = 0;

				for (horizLoc = 0x4f; horizLoc >= 0; horizLoc--)
				{
					ushort dot = this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

					if (dot == 1)
					{
						short cap = (short)((latitudeDist / 2) + (short)Climate);
						if (cap > moisture)
						{
							moisture++;
						}
					}
					else if (moisture > 0)
					{
						ushort drop = (ushort)this.Owner.Libc.Dice.DrawInt((ushort)(7 - (Climate << 1)));
						moisture = (short)(moisture - drop);

						switch (dot)
						{
							case 0x3:
							case 0xc:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 2);
								break;
							case 0x6:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 10);
								break;
							case 0xa:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, latitudeDist < 0xa ? (ushort)11 : (ushort)3);
								moisture = -2;
								break;
							case 0xd:
								moisture -= 3;
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 2);
								break;
							case 0xe:
								this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 6);
								break;
						}
					}
				}


				this.ShowStoryIntro();
			}
		}

		/// <summary>
		/// Performs 800 * (0x7efc + 1) terrain aging steps in pairs of (random spot, random neighbour).
		/// </summary>
		private void AgeTerrain()
		{
			short horizLoc = -1;
			short vertLoc = 0x32;
			short step = 0;

			while ((short)(((short)WorldAge * 0x320) + 0x320) > step)
			{
				if ((step & 1) == 0)
				{
					horizLoc = (short)this.Owner.Libc.Dice.DrawInt(80);

					vertLoc = (short)this.Owner.Libc.Dice.DrawInt(50);
				}
				else
				{
					ushort heading = (ushort)(this.Owner.Libc.Dice.DrawInt(8) + 1);

					horizLoc = (short)(horizLoc + (short)DirectionHoriz(heading));

					vertLoc = (short)(vertLoc + (short)DirectionVert(heading));
				}

				ushort dot = this.Owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

				switch (dot)
				{
					case 2:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 11);
						break;
					case 3:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 10);
						break;
					case 6:
					case 7:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 12);
						break;
					case 10:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 2);
						break;
					case 11:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 3);
						break;
					case 12:
					case 15:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 13);
						break;
					case 13:
						this.BlendTerrainTile(horizLoc, vertLoc);
						break;
					case 14:
						this.Owner.Services.AssignDot(1, horizLoc, vertLoc, 6);
						break;
				}

				step++;
			}
		}

		/// <summary>The size of a label of the pass (0xdcfe + 0x80 * pass + 2 * label)</summary>
		private ushort LabelSizeAddress(ushort pass, int label)
		{
			return (ushort)((ushort)(label << 1) + (ushort)(pass << 7) + 0xdcfe);
		}

		/// <summary>
		/// Labels the tiles of the pass's kind (land in pass 0, ocean in pass 1) in ChartSight, row by row: a run takes the
		/// label of a touching tile of the row above (joining two labels moves the old one's tiles and size to the new one)
		/// or a new free label (counts the terrains in pass 0)
		/// </summary>
		private void LabelLandOrSea(ushort pass, int terrainCounts)
		{
			ushort horizLoc, vertLoc;

			for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
					this.Owner.World.ChartSight[horizLoc, vertLoc] = 0;

				this.terrainFrame.SetFrame(0xa8, vertLoc);
			}

			this.terrainFrame.SetFrame(0x24, horizLoc);

			ushort newLabel = 0;
			this.terrainFrame.SetFrame(0xae, newLabel);

			for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
			{
				this.terrainFrame.SetFrame(0xa8, vertLoc);

				short run = -1;
				bool inRun = false;

				this.terrainFrame.SetFrame(0x22, 0xffff);
				this.terrainFrame.SetFrame(0x6, 0);

				for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					this.terrainFrame.SetFrame(0x24, horizLoc);

					ushort terrain = ObtainTerrainTypeFromScreen((short)horizLoc, (short)vertLoc);
					this.terrainFrame.SetFrame(0xaa, terrain);

					if (pass == 0)
					{
						int count = terrainCounts - (terrain << 1);
						this.terrainFrame.SetFrame(count, (ushort)(this.terrainFrame.Frame(count) + 1));
					}

					if ((terrain != 0xa) != (pass == 0))
					{
						inRun = false;
						run = -1;

						this.terrainFrame.SetFrame(0x6, 0);
						this.terrainFrame.SetFrame(0x22, 0xffff);
						continue;
					}

					if ((short)vertLoc > 0)
						run = JoinRowAbove(pass, (short)horizLoc, (short)vertLoc, run);

					if (run == -1)
					{
						if (!inRun)
						{
							newLabel = 0;

							do
							{
								newLabel++;
							}
							while (this.Processor.PullGlobalWord(LabelSizeAddress(pass, newLabel)) != 0);

							this.terrainFrame.SetFrame(0xae, newLabel);
						}

						run = (short)newLabel;
						this.terrainFrame.SetFrame(0x22, (ushort)run);
					}

					this.Owner.World.ChartSight[horizLoc, vertLoc] = (sbyte)(byte)run;

					ushort size = LabelSizeAddress(pass, run);
					this.Processor.EmitGlobalWord(size, (ushort)(this.Processor.PullGlobalWord(size) + 1));

					inRun = true;
					this.terrainFrame.SetFrame(0x6, 1);
				}

				this.terrainFrame.SetFrame(0x24, horizLoc);

				ShowStoryIntro();
			}

			this.terrainFrame.SetFrame(0xa8, vertLoc);
		}

		/// <summary>
		/// The tiles above-left, above and above-right of the tile: a labelled one joins the run (the run's earlier label is
		/// replaced in the rows so far and its size moved, then the story goes on); the run's label
		/// </summary>
		private short JoinRowAbove(ushort pass, short horizLoc, short vertLoc, short run)
		{
			short shift;
			short last = horizLoc < 0x4f ? (short)1 : (short)0;

			for (shift = horizLoc != 0 ? (short)-1 : (short)0; last >= shift; shift++)
			{
				this.terrainFrame.SetFrame(0xac, (ushort)shift);

				short label = this.Owner.World.ChartSight[(ushort)(horizLoc + shift), (ushort)(vertLoc - 1)];
				this.terrainFrame.SetFrame(0xd0, (ushort)label);

				if (label == 0)
					continue;

				if (run != -1 && run != label)
				{
					ushort row;

					for (row = 0; (short)row <= vertLoc; row++)
					{
						this.terrainFrame.SetFrame(0x4, row);

						ushort column;

						for (column = 0; column < 0x50; column++)
						{
							if (this.Owner.World.ChartSight[column, row] == run)
								this.Owner.World.ChartSight[column, row] = (sbyte)(byte)label;

							ushort from = LabelSizeAddress(pass, run);
							ushort to = LabelSizeAddress(pass, label);

							this.Processor.EmitGlobalWord(to, (ushort)(this.Processor.PullGlobalWord(to) + this.Processor.PullGlobalWord(from)));
							this.Processor.EmitGlobalWord(from, 0);
						}

						this.terrainFrame.SetFrame(0x2, column);
					}

					this.terrainFrame.SetFrame(0x4, row);
				}

				run = label;
				this.terrainFrame.SetFrame(0x22, (ushort)run);

				ShowStoryIntro();
			}

			this.terrainFrame.SetFrame(0xac, (ushort)shift);

			return run;
		}

		/// <summary>
		/// The 15 largest labels of the pass get the numbers 1 - 15 by size (the rest 15; ranking in the frame from -0xce, the
		/// numbers from -0xa4), drawn at (x, y + 50); the sizes 1 - 14 become those of the ranks, 0 gets size 0 and 15 size 1
		/// </summary>
		private void RankLandOrSea(ushort pass)
		{
			const int ranking = 0xce;
			const int numbers = 0xa4;

			for (ushort rank = 0; rank < 0x10; rank++)
			{
				this.terrainFrame.SetFrame(ranking - ((rank << 1)), 0);
			}

			this.terrainFrame.SetFrame(0xa4, 0);

			ushort label;

			for (label = 1; label < 0x40; label++)
			{
				this.terrainFrame.SetFrame(0x24, label);
				this.terrainFrame.SetFrame(numbers - ((label << 1)), 0xf);

				ushort size = this.Processor.PullGlobalWord(LabelSizeAddress(pass, label));
				ushort rank;

				for (rank = 1; rank < 0xf; rank++)
				{
					ushort ranked = this.terrainFrame.Frame(ranking - ((rank << 1)));

					if ((short)this.Processor.PullGlobalWord(LabelSizeAddress(pass, ranked)) >= (short)size)
						continue;

					// The smaller ones move down a rank
					ushort lower;

					for (lower = 0xf; (short)lower > (short)rank; lower--)
					{
						ushort moved = this.terrainFrame.Frame(ranking - ((lower << 1) - 2));

						this.terrainFrame.SetFrame(numbers - ((moved << 1)), lower);
						this.terrainFrame.SetFrame(ranking - ((lower << 1)), moved);
					}

					this.terrainFrame.SetFrame(0xac, lower);

					this.terrainFrame.SetFrame(numbers - ((label << 1)), rank);
					this.terrainFrame.SetFrame(ranking - ((rank << 1)), label);
					break;
				}

				this.terrainFrame.SetFrame(0xa8, rank);
			}

			this.terrainFrame.SetFrame(0xa4, 0);

			ushort horizLoc, vertLoc = 0;

			// A label from 0x40 on (or negative) reads past the numbers with ClassicLabelNumbers: the locals of the frame (x at
			// 0x24, y at 0xa8, ...), the frames of the callers above them or the stack under the frame; else it gets 15
			bool classicNumbers = this.Owner.TurnHooks.ClassicLabelNumbers;

			for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				this.terrainFrame.SetFrame(0x24, horizLoc);

				for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					this.terrainFrame.SetFrame(0xa8, vertLoc);

					short tileLabel = this.Owner.World.ChartSight[horizLoc, vertLoc];

					ushort number = classicNumbers || (tileLabel >= 0 && tileLabel < 0x40) ?
						this.terrainFrame.Frame(numbers - (tileLabel << 1)) : (ushort)0xf;
					this.terrainFrame.SetFrame(0xa6, number);

					if (number != 0)
						this.Owner.Services.AssignDot((short)horizLoc, (short)vertLoc + 0x32, number);
				}
			}

			this.terrainFrame.SetFrame(0xa8, vertLoc);

			for (ushort rank = 1; rank < 0xf; rank++)
			{
				int entry = ranking - (rank << 1);

				this.terrainFrame.SetFrame(entry, this.Processor.PullGlobalWord(LabelSizeAddress(pass, this.terrainFrame.Frame(entry))));
			}

			for (ushort rank = 1; rank < 0xf; rank++)
			{
				this.Processor.EmitGlobalWord(LabelSizeAddress(pass, rank), this.terrainFrame.Frame(ranking - ((rank << 1))));
			}

			this.terrainFrame.SetFrame(0x24, 0xf);

			this.Processor.EmitGlobalWord(LabelSizeAddress(pass, 0), 0);
			this.Processor.EmitGlobalWord(LabelSizeAddress(pass, 0xf), 1);
		}

		/// <summary>
		/// The continent of the first tile of (x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1) that isn't ocean on the screen map
		/// (land gets that tile), else -1 (land unchanged)
		/// </summary>
		private short FindBlockLand(short horizLoc, short vertLoc, ref (short, short)? land)
		{
			foreach ((short tileHoriz, short tileVert) in new[] { (horizLoc, vertLoc), ((short)(horizLoc + 1), vertLoc),
				(horizLoc, (short)(vertLoc + 1)), ((short)(horizLoc + 1), (short)(vertLoc + 1)) })
			{
				if (ObtainTerrainTypeFromScreen(tileHoriz, tileVert) == 0xa)
					continue;

				land = (tileHoriz, tileVert);

				return (short)this.Owner.MapOps.ObtainContinentID(tileHoriz, tileVert);
			}

			return -1;
		}

		/// <summary>Sets bit bitIdx of the block's links (0xdb44 + 13 * column + row)</summary>
		private void LinkLandmassBlock(short column, short row, int bitIdx)
		{
			ushort links = (ushort)((0xd * column) + row + 0xdb44);

			this.Processor.EmitGlobalByte(links, (byte)(this.Processor.PullGlobalByte(links) | (1 << bitIdx)));
		}

		/// <summary>The sea links of one block (see ReckonContinentSizes)</summary>
		private void LinkSeaBlock(short column, short row, ref short lastNextSea)
		{
			short horizLoc = (short)((column << 2) + 1), vertLoc = (short)((row << 2) + 1);

			if (FindBlockSea(horizLoc, vertLoc) is not ushort sea)
				return;

			for (short direction = 1; direction <= 8; direction++)
			{
				ushort offset = (ushort)(direction << 1);
				short nextHoriz = (short)((DirectionHorizAt(offset) << 2) + horizLoc);
				short nextVert = (short)((DirectionVertAt(offset) << 2) + vertLoc);

				// A neighbour without sea leaves lastNextSea as it was
				if (FindBlockSea(nextHoriz, nextVert) is ushort nextSea)
					lastNextSea = (short)nextSea;

				if ((short)sea != lastNextSea)
					continue;

				if (!SeaPassageOpen(horizLoc, vertLoc, offset) && (row != 0xb || direction != 3))
					continue;

				short nextRow = (short)(DirectionVertAt(offset) + row);

				if (nextRow >= 0xc)
					continue;

				LinkSeaBlockBit(column, row, direction - 1);

				short nextColumn = (short)(DirectionHorizAt(offset) + column);

				if (nextColumn < 0 || nextColumn >= 0x14 || nextRow < 0 || nextRow > 0xc)
					continue;

				LinkSeaBlockBit(nextColumn, nextRow, (direction + 3) & 7);
			}
		}

		/// <summary>
		/// The sea (continent id of the screen map) of the first ocean tile of (x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1),
		/// null when there is none
		/// </summary>
		private ushort? FindBlockSea(short horizLoc, short vertLoc)
		{
			foreach ((short tileHoriz, short tileVert) in new[] { (horizLoc, vertLoc), ((short)(horizLoc + 1), vertLoc),
				(horizLoc, (short)(vertLoc + 1)), ((short)(horizLoc + 1), (short)(vertLoc + 1)) })
			{
				if (ObtainTerrainTypeFromScreen(tileHoriz, tileVert) != 0xa)
					continue;

				ushort continentId = ObtainContinentIDFromScreen(tileHoriz, tileVert);

				return continentId;
			}

			return null;
		}

		/// <summary>
		/// Whether a ship can pass from the block's corner 5 tiles in the direction: at each step at least 2 of the 4 tiles
		/// (x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1) are ocean inside the map
		/// </summary>
		private bool SeaPassageOpen(short horizLoc, short vertLoc, ushort offset)
		{
			for (short step = 0; step <= 4; step++)
			{
				horizLoc = (short)(horizLoc + DirectionHorizAt(offset));
				vertLoc = (short)(vertLoc + DirectionVertAt(offset));

				short ocean = 4;

				foreach ((short tileHoriz, short tileVert) in new[] { (horizLoc, vertLoc), ((short)(horizLoc + 1), vertLoc),
					(horizLoc, (short)(vertLoc + 1)), ((short)(horizLoc + 1), (short)(vertLoc + 1)) })
				{
					if (ObtainTerrainTypeFromScreen(tileHoriz, tileVert) != 0xa || this.Owner.MapOps.CheckMapLimits(tileHoriz, tileVert) == 0)
						ocean--;
				}

				if (ocean < 2)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>Sets bit bitIdx of the block's sea links (0x7f38 + 13 * column + row)</summary>
		private void LinkSeaBlockBit(short column, short row, int bitIdx)
		{
			ushort links = (ushort)((0xd * column) + row + SeaBlocksAddress);

			this.Processor.EmitGlobalByte(links, (byte)(this.Processor.PullGlobalByte(links) | (1 << bitIdx)));
		}

		/// <summary>
		/// The step of ShowStoryIntro, true while waiting for the music
		/// </summary>
		private bool StoryStep()
		{
			if ((short)StoryState >= 2 && this.Owner.Libc.KeyAvailable() == 0)
			{
				// The story is told: the "press a key" line
				if ((short)StoryState < 5)
				{
					StoryState = (ushort)(StoryState + 1);

					if (StoryState == 5)
					{
						this.Owner.Services.FloodBox(ScreenBox, 40, 160, 240, 8, Hues.Black);
					}
				}
				else
				{
					StoryMusicTime = (ushort)CPU.SignedQuotient((short)this.Owner.Timer.Beats, 0x3c);

					ushort colour = (ushort)(((byte)StoryMusicTime & 1) != 0 ? 0xf : 0x3);

					this.Owner.TextPainter.DrawCenteredTextToScreen0(GameTexts.TextBuildingNewWorld, 160, 160, colour);
				}

				MarkScreenChanged();

				return false;
			}

			if (StoryState == 0)
			{
				StoryFile = (ushort)this.Owner.Libc.OpenStream("story.txt", "rt");
				StoryState = 0x1;
				StoryLineTime = 0;
				StoryMusicTime = 0;

				this.Owner.Words.Set("");

				DrawingBox.TypefaceIdx = 0x7;
				StoryLinesTold = 0x0;
			}

			if (StoryLinesTold != 0)
			{
				StoryMusicTime = this.Owner.Services.ReadSongPosition();
			}

			if ((short)StoryMusicTime < (short)StoryLineTime)
			{
				MarkScreenChanged();

				return true;
			}

			if (StoryState != 2 && (short)StoryLinesTold < 0x28)
			{
				this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 160, Hues.Cyan);

				this.Owner.TextPainter.WaitForTime(5);
			}

			short lineRead = this.Owner.Libc.ScanStream((short)StoryFile, "%[^\n]\n", TextBuffer.Address);

			if (lineRead == -1 || (this.Owner.Libc.KeyAvailable() != 0 && StoryKeyAllowed != 0))
			{
				EndStory(lineRead == -1);
			}
			else
			{
				TellStoryLine();
			}

			this.Owner.Services.ShowScreen(0);

			MarkScreenChanged();

			return StoryState != 2;
		}

		/// <summary>
		/// The line just read from story.txt (TextBuffer.Address): shown under the picture, or the next picture for a short line; then the
		/// time of the next line (0x6806) from the music table 0x3710:0x83
		/// </summary>
		private void TellStoryLine()
		{
			if ((short)BirthPicture > 1)
			{
				// The line under the picture is wiped with the picture
				this.Owner.VideoCard.RenderImage(PictureScreenBox, 0, 0xa0, 0x140, 8,
					ScreenBox, 0, 0xa0);
			}

			this.Owner.TextPainter.WaitForTime(10);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 160, Hues.Cyan);

			this.Owner.TextPainter.WaitForTime(5);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 160, Hues.LightCyan);

			StoryLineTime = 0x0;

			if ((short)this.Owner.Words.Length > 3)
			{
				if (StoryLinesTold == 0)
				{
					// The first line starts the music
					this.Owner.Services.RestoreTimer();

					this.Owner.Services.PerformSound(1, 0);

					this.Owner.Services.PerformSound(4, 0);
				}

				StoryLineTime = (ushort)(StoryLineTime + 2);
				StoryLinesTold = (ushort)(StoryLinesTold + 1);
			}
			else
			{
				ShowStoryPicture();
			}

			if ((short)this.Owner.Words.Length > 0x13)
			{
				StoryLineTime = (ushort)(StoryLineTime + 1);
			}

			StoryLineTime = (ushort)this.Owner.Helpers.ClampValue(
				this.Processor.PullInt16(StorySegment, (ushort)((StoryLinesTold << 1) + StoryTimingTableOffset)),
				(45 * (short)StoryLineTime) + (short)StoryMusicTime,
				32767);
		}

		/// <summary>
		/// The next picture birthN.pic (N = 0x3b64), with VGA colours faded in with its palette birthN.pal and its colour cycles
		/// </summary>
		private void ShowStoryPicture()
		{
			byte pictureIdx = (byte)BirthPicture;
			BirthPicture = (ushort)(BirthPicture + 1);
			string pictureName = $"birth{pictureIdx}.pic";

			bool colours = VgaMode != 0;

			if (colours)
			{
				StopStoryColourCycles();
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(2, 0, 0, pictureName, 0);

			if (VgaMode != 0)
			{
				this.Owner.Services.FillColors(8, 0, 0, 0);
			}

			this.Owner.VideoCard.RenderImage(PictureScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			if (VgaMode == 0)
				return;

			string palName = $"birth{pictureIdx}.pal";

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, palName, 0xc5be);

			this.Owner.Services.LoadColorsFromBuffer(8, 0xc5be);

			// The colour cycles of the picture: first colour, last colour, speed (300 / speed beats a step), 0 at the end
			StoryCycleCount = 0x0;

			while (this.Processor.PullUInt8(StorySegment, StoryCycleRecord) != 0)
			{
				ushort first = NextStoryCycleByte();
				ushort last = NextStoryCycleByte();
				ushort speed = NextStoryCycleByte();

				if (speed == 0)
					throw new Exception("Division by zero");

				ushort delay = (ushort)(0x12c / speed);

				StoryCycleCount = (ushort)(StoryCycleCount + 1);

				this.Owner.Services.AssignHueCycleSpan(StoryCycleCount, delay, first, last);
			}

			StoryCycleRecord = (ushort)(StoryCycleRecord + 1);

			for (short cycleIdx = 1; cycleIdx <= (short)StoryCycleCount; cycleIdx++)
			{
				this.Owner.Services.ActivateHueCycle((ushort)cycleIdx);
			}
		}

		/// <summary>The next byte of the colour cycle records of the story pictures (0x3710:0x3b66)</summary>
		private ushort NextStoryCycleByte()
		{
			ushort spot = StoryCycleRecord;
			StoryCycleRecord = (ushort)(StoryCycleRecord + 1);

			return this.Processor.PullUInt8(StorySegment, spot);
		}

		/// <summary>
		/// The end of the story (the end of story.txt, then a pause of 180, or a key): closes the file, stops the music and the
		/// colour cycles, stage 2
		/// </summary>
		private void EndStory(bool endOfFile)
		{
			if (endOfFile)
			{
				this.Owner.TextPainter.WaitForTime(180);
			}

			this.Owner.Libc.CloseStream((short)StoryFile);

			this.Owner.Services.PerformSound(1, 0);

			if (VgaMode != 0)
			{
				StopStoryColourCycles();
			}

			StoryState = 0x2;
		}

		/// <summary>Stops the colour cycles 1 - 0x3b68 of the story picture</summary>
		private void StopStoryColourCycles()
		{
			for (short cycleIdx = 1; cycleIdx <= (short)StoryCycleCount; cycleIdx++)
			{
				this.Owner.Services.DeactivateHueCycle((ushort)cycleIdx);
			}
		}

		/// <summary>The drawing box back on screen 1 (its display index: [[0xaa]] = 1)</summary>
		private void MarkScreenChanged()
		{
			DrawingBox.DisplayIdx = 0x1;
		}
	}
}
