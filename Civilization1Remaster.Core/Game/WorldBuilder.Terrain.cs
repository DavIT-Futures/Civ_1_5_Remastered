namespace Civilization1Remaster.Core.Game
{
	public partial class WorldBuilder
	{
		public void BlendTerrainTile(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"BlendTerrainTile({horizontalLocation}, {verticalLocation})");

			if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation - 1, verticalLocation - 1) != 1 &&
				this.Owner.VideoCard.ObtainDot(1, horizontalLocation - 1, verticalLocation + 1) != 1 &&
				this.Owner.VideoCard.ObtainDot(1, horizontalLocation + 1, verticalLocation - 1) != 1 &&
				this.Owner.VideoCard.ObtainDot(1, horizontalLocation + 1, verticalLocation + 1) != 1)
			{
				this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 1);
			}
		}

		public void ProduceElevation()
		{
			using var _ = this.Processor.Journal.Scope($"ProduceElevation()");

			IMapPlanes chart = this.Owner.MapOperations.Chart;
			short riverTally = 0;
			short attemptTally = 0;

			while (true)
			{
				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0xa0, 0x64, Screen.Box, 0xa0, 0);

				short horizontalLocation;
				short verticalLocation;

				do
				{
					horizontalLocation = (short)this.Owner.Runtime.Dice.DrawInteger(80);
					verticalLocation = (short)this.Owner.Runtime.Dice.DrawInteger(50);
				}
				while (this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation) != 0xc);

				short springHorizontal = horizontalLocation;
				short springVertical = verticalLocation;
				short heading = (short)(this.Owner.Runtime.Dice.DrawInteger(4) << 1);
				short length = 0;
				bool nearOcean;
				ushort reached;

				do
				{
					this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 9);

					nearOcean = false;

					for (int side = 1; side <= 8; side += 2)
					{
						if (this.Owner.VideoCard.ObtainDot(1, chart.DirectionHorizontal(side) + horizontalLocation, chart.DirectionVertical(side) + verticalLocation) == 1)
						{
							nearOcean = true;
						}
					}

					short previousHeading = heading;
					heading = (short)(((((short)this.Owner.Runtime.Dice.DrawInteger(2) - (length & 1)) << 1) + heading) & 7);

					if ((previousHeading ^ 4) > heading)
					{
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation + 0x32, 8);
					}

					horizontalLocation += (short)chart.DirectionHorizontal(heading + 1);
					verticalLocation += (short)chart.DirectionVertical(heading + 1);

					reached = this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation);

					length++;
				}
				while (!nearOcean && reached != 0xd && reached != 1 && reached != 9);

				if ((nearOcean || reached == 9) && length >= 5)
				{
					riverTally++;

					for (int spotIndex = 1; spotIndex <= 0x15; spotIndex++)
					{
						horizontalLocation = (short)(chart.DirectionHorizontal(spotIndex) + springHorizontal);
						verticalLocation = (short)(chart.DirectionVertical(spotIndex) + springVertical);

						if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation) == 2)
						{
							this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 11);
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
				{
					break;
				}
			}

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x32, 0x50, 0x32, Screen.Box, 0, 0x96);

			this.Owner.Services.FloodBox(Screen.Box, 0, 50, 80, 50, Hues.Black);
		}

		public void ProduceCloudLayer()
		{
			using var _ = this.Processor.Journal.Scope("ProduceCloudLayer()");

			this.Owner.Services.FloodBox(Screen.Box, 160, 0, 80, 50, Hues.Black);

			short cloudHorizontal = (short)(this.Owner.Runtime.Dice.DrawInteger(72) + 4);
			short cloudVertical = (short)(this.Owner.Runtime.Dice.DrawInteger(34) + 8);

			ProduceCloud(cloudHorizontal, cloudVertical);

			for (int horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
			{
				for (int verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
				{
					if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation + 0xa0, verticalLocation) != 0)
					{
						ushort hue = this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation);

						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, (ushort)(hue + 1));

						Story.LightenedPixels = (ushort)(Story.LightenedPixels + 1);
					}
				}
			}
		}

		public void ProduceCloud(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ProduceCloud({horizontalLocation}, {verticalLocation})");

			int vaporExtent = this.Owner.Runtime.Dice.DrawInteger(64) + 1;

			do
			{
				this.Owner.Services.AssignDot(1, horizontalLocation + 160, verticalLocation, 15);
				this.Owner.Services.AssignDot(1, horizontalLocation + 161, verticalLocation, 15);
				this.Owner.Services.AssignDot(1, horizontalLocation + 160, verticalLocation + 1, 15);

				switch (this.Owner.Runtime.Dice.DrawInteger(4))
				{
					case 0:
						horizontalLocation += 0;
						verticalLocation += -1;
						break;

					case 1:
						horizontalLocation += 1;
						verticalLocation += 0;
						break;

					case 2:
						horizontalLocation += 0;
						verticalLocation += 1;
						break;

					case 3:
						horizontalLocation += -1;
						verticalLocation += 0;
						break;
				}

				vaporExtent--;
			}
			while (vaporExtent > 0 && horizontalLocation > 2 && horizontalLocation < 77 && verticalLocation > 2 && verticalLocation < 46);
		}

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

			ushort dotIndex;

			for (dotIndex = 0; dotIndex < 0x14; dotIndex++)
			{
				this.Owner.Services.AssignDot(this.Owner.Runtime.Dice.DrawInteger(80), 0, 7);
				this.Owner.Services.AssignDot(this.Owner.Runtime.Dice.DrawInteger(80), 1, 7);
				this.Owner.Services.AssignDot(this.Owner.Runtime.Dice.DrawInteger(80), 48, 7);
				this.Owner.Services.AssignDot(this.Owner.Runtime.Dice.DrawInteger(80), 49, 7);
			}

			this.terrainFrame.SetFrame(0x24, dotIndex);

			this.terrainFrame = outerFrame;
		}

		public void PlaceSpecialTerrain()
		{
			using var _ = this.Processor.Journal.Scope("SituateSpecialTerrain()");

			for (int continentIndex = 0; continentIndex < 0x10; continentIndex++)
			{
				this.Owner.World.Landmasses[continentIndex].ConstructionSpotTally = 0;
			}

			ushort[] weights = new ushort[0x18];

			for (int terrainIndex = 0; terrainIndex < 0x18; terrainIndex++)
			{
				int kind = terrainIndex % 12;

				weights[terrainIndex] = (ushort)((3 * (sbyte)Tables.TerrainField(terrainIndex, TerrainTable.Food)) +
					(sbyte)Tables.TerrainField(terrainIndex, TerrainTable.Trade));

				if (kind != 2 && kind != 11)
				{
					weights[terrainIndex] += (ushort)((sbyte)Tables.TerrainField(terrainIndex, TerrainTable.Shields) << 1);
				}

				short mining = (short)Tables.TerrainImprovementWord(kind, TerrainImprovementTable.Mining);

				if (mining < 0)
				{
					weights[terrainIndex] += (ushort)(0xffff - mining);
				}
				else
				{
					short irrigation = (short)Tables.TerrainImprovementWord(kind, TerrainImprovementTable.Irrigation);

					if (irrigation < 0)
					{
						weights[terrainIndex] += (ushort)((0xffff - irrigation) << 1);
					}
				}
			}

			for (short horizontalLocation = 2; horizontalLocation < 0x4e; horizontalLocation++)
			{
				for (short verticalLocation = 2; verticalLocation < 0x30; verticalLocation++)
				{
					ushort terrain = ObtainTerrainTypeFromScreen(horizontalLocation, verticalLocation);

					if (terrain != 0xb && terrain != 2 && terrain != 1)
					{
						this.Owner.Services.AssignDot(horizontalLocation + 80, verticalLocation, 0);
						this.Owner.Services.AssignDot(horizontalLocation + 80, verticalLocation + 0x32, 0);
						continue;
					}

					short total = 0;

					for (int deltaIndex = 0; deltaIndex <= 0x14; deltaIndex++)
					{
						short value = 0;
						short tileHorizontal = (short)(this.Owner.TownDeltas[deltaIndex].X + horizontalLocation);
						short tileVertical = (short)(this.Owner.TownDeltas[deltaIndex].Y + verticalLocation);

						ushort tile = ObtainTerrainTypeFromScreen(tileHorizontal, tileVertical);

						if ((tile == 2 || tile == 0xb) && !OffRiverPattern(tileHorizontal, tileVertical))
						{
							value += 2;
						}

						if (this.Owner.MapOperations.ContainsSpecialResource(tileHorizontal, tileVertical) != 0)
						{
							tile += 0xc;
						}

						value += (short)weights[tile];

						if (deltaIndex < 9)
						{
							value += value;
						}

						if (deltaIndex == 0)
						{
							value += value;
						}

						total += value;
					}

					if (ObtainTerrainTypeFromScreen(horizontalLocation, verticalLocation) != 1 && OffRiverPattern(horizontalLocation, verticalLocation))
					{
						total -= 0x10;
					}

					short worth = (short)(((short)this.Owner.Helpers.ClampValue((total - 120) / 8, 1, 15) / 2) + 8);

					this.Owner.Services.AssignDot(horizontalLocation + 80, verticalLocation, (ushort)worth);
					this.Owner.Services.AssignDot(horizontalLocation + 80, verticalLocation + 0x32, (ushort)worth);

					this.Owner.World.Landmasses[ObtainContinentIdentifierFromScreen(horizontalLocation, verticalLocation)].ConstructionSpotTally++;
				}
			}
		}

		public ushort ObtainTerrainTypeFromScreen(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainTerrainTypeFromScreen({horizontalLocation}, {verticalLocation})");

			ushort displayIndex = DrawingBox.DisplayIndex;
			ushort colour = this.Owner.VideoCard.ObtainDot(displayIndex, horizontalLocation, verticalLocation);
			ushort terrain = this.Processor.PullGlobalWord((ushort)(MapPlanes.TerrainOfColour + (colour << 1)));

			return terrain;
		}

		public void EnactGlobalWarming(short worldHeatingTally)
		{
			using var _ = this.Processor.Journal.Scope($"EnactGlobalWarming({worldHeatingTally})");

			this.Owner.Words.Set("Global temperature\nrises! Icecaps melt.\nSevere Drought.\n");

			this.Owner.Paper.ShowNewspaper(-1);

			IMapPlanes chart = this.Owner.MapOperations.Chart;

			for (int horizontalLocation = 0; horizontalLocation < 80; horizontalLocation++)
			{
				for (int verticalLocation = 0; verticalLocation < 50; verticalLocation++)
				{
					ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

					if ((short)terrain > 3)
					{
						continue;
					}

					short oceanTally = 0;

					for (int heading = 1; heading <= 8; heading++)
					{
						if (this.Owner.MapOperations.ObtainTerrainType(chart.DirectionHorizontal(heading) + horizontalLocation, chart.DirectionVertical(heading) + verticalLocation) == MapRules.OceanTerrain)
						{
							oceanTally++;
						}
					}

					if (7 - worldHeatingTally <= oceanTally)
					{
						this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= 1;
						this.Owner.Services.AssignDot(2, horizontalLocation, verticalLocation, (ushort)(terrain != 3 ? 3 : 11));
						this.Owner.MapOperations.RemoveTileImprovements(6, horizontalLocation, verticalLocation);
					}
					else if ((((11 * horizontalLocation) + (13 * verticalLocation)) & 7) == (ushort)worldHeatingTally)
					{
						this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= 1;
						this.Owner.Services.AssignDot(2, horizontalLocation, verticalLocation, (ushort)((short)terrain <= 1 ? 14 : 6));
					}
					else
					{
						continue;
					}

					if ((this.Owner.World.ChartSight[horizontalLocation, verticalLocation] & (1 << this.Owner.World.PersonParticipantIndex)) != 0)
					{
						this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
					}
				}
			}
		}

		private static bool OffRiverPattern(short horizontalLocation, short verticalLocation)
		{
			return (((7 * horizontalLocation) + (0xb * verticalLocation)) & 2) != 0;
		}

		private void SmoothDiagonalCoastlines()
		{
			short horizontalLocation = 1;

			while (horizontalLocation < 0x4f)
			{
				short verticalLocation = 1;

				while (verticalLocation < 0x31)
				{
					byte mask = 0;

					if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation) != 0)
					{
						mask |= 0x1;
					}

					if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation + 1, verticalLocation) != 0)
					{
						mask |= 0x2;
					}

					if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation + 1) != 0)
					{
						mask |= 0x4;
					}

					if (this.Owner.VideoCard.ObtainDot(1, horizontalLocation + 1, verticalLocation + 1) != 0)
					{
						mask |= 0x8;
					}

					if (mask == 0x6 || mask == 0x9)
					{
						this.Owner.Services.AssignDot(1, horizontalLocation + 1, verticalLocation, 1);
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation + 1, 1);
						this.Owner.Services.AssignDot(1, horizontalLocation + 1, verticalLocation + 1, 1);

						if (horizontalLocation != 0)
						{
							horizontalLocation--;
						}

						if (verticalLocation != 0)
						{
							verticalLocation--;
						}
					}

					verticalLocation++;
				}

				this.ShowStoryIntro();

				horizontalLocation++;
			}
		}

		private void AssignLatitudeAndElevationTerrain()
		{
			short horizontalLocation;

			for (horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
			{
				short verticalLocation;

				for (verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
				{
					ushort dot = this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation);

					if (dot == 0)
					{
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 1);
					}
					else if (dot == 1)
					{
						short rawLatitude = (short)-((short)(this.Owner.Runtime.Dice.DrawInteger(8) + verticalLocation - 0x1d));
						ushort absoluteLatitude = (ushort)this.Owner.Runtime.AbsoluteValue(rawLatitude);
						short band = (short)(absoluteLatitude + (1 - (short)NewWorld.Temperature));

						ushort tier = (ushort)(CentralProcessor.SignedQuotient(band, 6) + 1);
						if (tier <= 7)
						{
							ushort colour = (tier / 2) switch
							{
								0 => 14,
								1 => 6,
								2 => 7,
								_ => 15,
							};
							this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, colour);
						}
					}
					else if (dot == 2)
					{
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 12);
					}
					else
					{
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 13);
					}
				}

				this.ShowStoryIntro();
			}
		}

		private void ApplyMoisturePasses()
		{
			short verticalLocation;

			for (verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
			{
				short latitudeDistance = (short)this.Owner.Runtime.AbsoluteValue((short)(0x19 - verticalLocation));

				short moisture = 0;

				short horizontalLocation;

				for (horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
				{
					ushort dot = this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation);

					if (dot == 1)
					{
						short cap = (short)(this.Owner.Runtime.AbsoluteValue((short)(0xc - latitudeDistance)) + (NewWorld.Climate << 2));
						if (cap > moisture)
						{
							moisture++;
						}
					}
					else if (moisture > 0)
					{
						ushort drop = (ushort)this.Owner.Runtime.Dice.DrawInteger((ushort)(7 - (NewWorld.Climate << 1)));
						moisture = (short)(moisture - drop);

						switch (dot)
						{
							case 0x6:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 10);
								break;
							case 0x7:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 15);
								break;
							case 0xc:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 2);
								break;
							case 0xd:
								moisture -= 3;
								break;
							case 0xe:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 6);
								break;
						}
					}
				}

				moisture = 0;

				for (horizontalLocation = 0x4f; horizontalLocation >= 0; horizontalLocation--)
				{
					ushort dot = this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation);

					if (dot == 1)
					{
						short cap = (short)((latitudeDistance / 2) + (short)NewWorld.Climate);
						if (cap > moisture)
						{
							moisture++;
						}
					}
					else if (moisture > 0)
					{
						ushort drop = (ushort)this.Owner.Runtime.Dice.DrawInteger((ushort)(7 - (NewWorld.Climate << 1)));
						moisture = (short)(moisture - drop);

						switch (dot)
						{
							case 0x3:
							case 0xc:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 2);
								break;
							case 0x6:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 10);
								break;
							case 0xa:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, latitudeDistance < 0xa ? (ushort)11 : (ushort)3);
								moisture = -2;
								break;
							case 0xd:
								moisture -= 3;
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 2);
								break;
							case 0xe:
								this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 6);
								break;
						}
					}
				}

				this.ShowStoryIntro();
			}
		}

		private void AgeTerrain()
		{
			short horizontalLocation = -1;
			short verticalLocation = 0x32;
			short step = 0;

			while ((short)(((short)NewWorld.WorldAge * 0x320) + 0x320) > step)
			{
				if ((step & 1) == 0)
				{
					horizontalLocation = (short)this.Owner.Runtime.Dice.DrawInteger(80);

					verticalLocation = (short)this.Owner.Runtime.Dice.DrawInteger(50);
				}
				else
				{
					ushort heading = (ushort)(this.Owner.Runtime.Dice.DrawInteger(8) + 1);

					horizontalLocation = (short)(horizontalLocation + (short)Tables.DirectionHorizontal(heading));

					verticalLocation = (short)(verticalLocation + (short)Tables.DirectionVertical(heading));
				}

				ushort dot = this.Owner.VideoCard.ObtainDot(1, horizontalLocation, verticalLocation);

				switch (dot)
				{
					case 2:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 11);
						break;
					case 3:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 10);
						break;
					case 6:
					case 7:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 12);
						break;
					case 10:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 2);
						break;
					case 11:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 3);
						break;
					case 12:
					case 15:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 13);
						break;
					case 13:
						this.BlendTerrainTile(horizontalLocation, verticalLocation);
						break;
					case 14:
						this.Owner.Services.AssignDot(1, horizontalLocation, verticalLocation, 6);
						break;
				}

				step++;
			}
		}
	}
}
