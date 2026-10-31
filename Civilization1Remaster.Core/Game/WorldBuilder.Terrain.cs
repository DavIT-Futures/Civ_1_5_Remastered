using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The terrain of the new world: elevation, clouds, climate, moisture, age, special terrain and global warming</summary>
	public partial class WorldBuilder
	{
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
				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0xa0, 0x64, Screen.Box, 0xa0, 0);

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
					this.Owner.VideoCard.RenderImage(Screen.Box, 0xa0, 0, 0xa0, 0x64, Screen.Box, 0, 0);
				}

				ShowStoryIntro();

				attemptTally++;

				if (attemptTally >= 0x100 ||
					(short)(((NewWorld.LandMass + NewWorld.Climate) << 1) + 6) <= riverTally)
					break;
			}

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x32, 0x50, 0x32, Screen.Box, 0, 0x96);

			this.Owner.Services.FloodBox(Screen.Box, 0, 50, 80, 50, Hues.Black);
		}

		/// <inheritdoc/>
		public void ProduceCloudLayer()
		{
			using var _ = this.Processor.Journal.Scope("ProduceCloudLayer()");

			this.Owner.Services.FloodBox(Screen.Box, 160, 0, 80, 50, Hues.Black);

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

						Story.LightenedPixels = (ushort)(Story.LightenedPixels + 1);
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

			FrameWords outerFrame = this.terrainFrame;
			this.terrainFrame = new FrameWords(this.Processor, 0xd4);

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

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.PictureScreenBox, 0, 0);

			LabelLandmasses();

			ReckonContinentSizes(1);

			ShowStoryIntro();

			PlaceSpecialTerrain();

			this.Owner.VideoCard.RenderLine(Screen.Box, 0, 0, 79, 0, Hues.White);
			this.Owner.VideoCard.RenderLine(Screen.Box, 0, 49, 79, 49, Hues.White);

			ushort dotIdx;

			for (dotIdx = 0; dotIdx < 0x14; dotIdx++)
			{
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 0, 7);
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 1, 7);
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 48, 7);
				this.Owner.Services.AssignDot(this.Owner.Libc.Dice.DrawInt(80), 49, 7);
			}

			this.terrainFrame.SetFrame(0x24, dotIdx);

			this.terrainFrame = outerFrame;
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

				weights[terrainIdx] = (ushort)((3 * (sbyte)Tables.TerrainField(terrainIdx, TerrainTable.Food)) +
					(sbyte)Tables.TerrainField(terrainIdx, TerrainTable.Trade));

				if (kind != 2 && kind != 11)
				{
					weights[terrainIdx] += (ushort)((sbyte)Tables.TerrainField(terrainIdx, TerrainTable.Shields) << 1);
				}

				short mining = (short)Tables.TerrainImprovementWord(kind, TerrainImprovementTable.Mining);

				if (mining < 0)
				{
					weights[terrainIdx] += (ushort)(0xffff - mining);
				}
				else
				{
					short irrigation = (short)Tables.TerrainImprovementWord(kind, TerrainImprovementTable.Irrigation);

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
		public ushort ObtainTerrainTypeFromScreen(short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTerrainTypeFromScreen({horizLoc}, {vertLoc})");

			ushort displayIdx = DrawingBox.DisplayIdx;
			ushort colour = this.Owner.VideoCard.ObtainDot(displayIdx, horizLoc, vertLoc);
			ushort terrain = this.Processor.PullGlobalWord((ushort)(MapPlanes.TerrainOfColour + (colour << 1)));


			return terrain;
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
						short band = (short)(absLat + (1 - (short)NewWorld.Temperature));

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
						short cap = (short)(this.Owner.Libc.AbsoluteValue((short)(0xc - latitudeDist)) + (NewWorld.Climate << 2));
						if (cap > moisture)
						{
							moisture++;
						}
					}
					else if (moisture > 0)
					{
						ushort drop = (ushort)this.Owner.Libc.Dice.DrawInt((ushort)(7 - (NewWorld.Climate << 1)));
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
						short cap = (short)((latitudeDist / 2) + (short)NewWorld.Climate);
						if (cap > moisture)
						{
							moisture++;
						}
					}
					else if (moisture > 0)
					{
						ushort drop = (ushort)this.Owner.Libc.Dice.DrawInt((ushort)(7 - (NewWorld.Climate << 1)));
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

			while ((short)(((short)NewWorld.WorldAge * 0x320) + 0x320) > step)
			{
				if ((step & 1) == 0)
				{
					horizLoc = (short)this.Owner.Libc.Dice.DrawInt(80);

					vertLoc = (short)this.Owner.Libc.Dice.DrawInt(50);
				}
				else
				{
					ushort heading = (ushort)(this.Owner.Libc.Dice.DrawInt(8) + 1);

					horizLoc = (short)(horizLoc + (short)Tables.DirectionHoriz(heading));

					vertLoc = (short)(vertLoc + (short)Tables.DirectionVert(heading));
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
	}
}
