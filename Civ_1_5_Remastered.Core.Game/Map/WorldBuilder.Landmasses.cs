namespace Civ_1_5_Remastered.Core.Game.Map
{
	public partial class WorldBuilder
	{
		public void LabelLandmasses()
		{
			using var _ = this.Processor.Journal.Scope("LabelLandmasses()");

			this.Owner.Runtime.FillBytes(0xdb44, 0, 0x104);

			(short, short)? land = null;
			(short, short)? neighbourLand = null;

			for (short column = 0; column < 0x14; column++)
			{
				for (short row = 0; row < 0xd; row++)
				{
					short horizontalLocation = (short)((column << 2) + 1), verticalLocation = (short)((row << 2) + 1);

					short continent = FindBlockLand(horizontalLocation, verticalLocation, ref land);

					if (continent == -1)
					{
						continue;
					}

					for (short direction = 1; direction <= 4; direction++)
					{
						short nextHorizontal = (short)((Tables.DirectionHorizontal(direction) << 2) + horizontalLocation);
						short nextVertical = (short)((Tables.DirectionVertical(direction) << 2) + verticalLocation);

						short nextContinent = FindBlockLand(nextHorizontal, nextVertical, ref neighbourLand);

						if (continent != nextContinent)
						{
							continue;
						}

						short cost = (short)this.Owner.Routing.ComputeRouteCost(land!.Value.Item1, land.Value.Item2,
							neighbourLand!.Value.Item1, neighbourLand.Value.Item2, 0, 20);

						if (cost == -1 || cost >= 0x14)
						{
							continue;
						}

						LinkLandmassBlock(column, row, direction - 1);

						short nextColumn = (short)(Tables.DirectionHorizontal(direction) + column);
						short nextRow = (short)(Tables.DirectionVertical(direction) + row);

						if (nextColumn < 0 || nextColumn >= 0x14 || nextRow < 0 || nextRow >= 0xd)
						{
							continue;
						}

						LinkLandmassBlock(nextColumn, nextRow, (direction + 3) & 7);
					}
				}
			}
		}

		public void ReckonContinentSizes(ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"ReckonContinentSizes({marker})");

			this.Owner.Runtime.FillBytes(SeaBlocksAddress, 0, 0x104);

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

		public ushort ObtainContinentIdentifierFromScreen(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainContinentIDFromScreen({horizontalLocation}, {verticalLocation})");

			ushort dot = this.Owner.VideoCard.ObtainDot(
				DrawingBox.DisplayIndex,
				horizontalLocation, verticalLocation + 50);

			return dot;
		}

		private ushort LabelSizeAddress(ushort pass, int label)
		{
			return (ushort)((ushort)(label << 1) + (ushort)(pass << 7) + 0xdcfe);
		}

		private void LabelLandOrSea(ushort pass, int terrainCounts)
		{
			ushort horizontalLocation, verticalLocation;

			for (horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
			{
				for (verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
				{
					this.Owner.World.ChartSight[horizontalLocation, verticalLocation] = 0;
				}

				this.terrainFrame.SetFrame(0xa8, verticalLocation);
			}

			this.terrainFrame.SetFrame(0x24, horizontalLocation);

			ushort newLabel = 0;
			this.terrainFrame.SetFrame(0xae, newLabel);

			for (verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
			{
				this.terrainFrame.SetFrame(0xa8, verticalLocation);

				short run = -1;
				bool inRun = false;

				this.terrainFrame.SetFrame(0x22, 0xffff);
				this.terrainFrame.SetFrame(0x6, 0);

				for (horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
				{
					this.terrainFrame.SetFrame(0x24, horizontalLocation);

					ushort terrain = ObtainTerrainTypeFromScreen((short)horizontalLocation, (short)verticalLocation);
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

					if ((short)verticalLocation > 0)
					{
						run = JoinRowAbove(pass, (short)horizontalLocation, (short)verticalLocation, run);
					}

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

					this.Owner.World.ChartSight[horizontalLocation, verticalLocation] = (sbyte)(byte)run;

					ushort size = LabelSizeAddress(pass, run);
					this.Processor.EmitGlobalWord(size, (ushort)(this.Processor.PullGlobalWord(size) + 1));

					inRun = true;
					this.terrainFrame.SetFrame(0x6, 1);
				}

				this.terrainFrame.SetFrame(0x24, horizontalLocation);

				ShowStoryIntro();
			}

			this.terrainFrame.SetFrame(0xa8, verticalLocation);
		}

		private short JoinRowAbove(ushort pass, short horizontalLocation, short verticalLocation, short run)
		{
			short shift;
			short last = horizontalLocation < 0x4f ? (short)1 : (short)0;

			for (shift = horizontalLocation != 0 ? (short)-1 : (short)0; last >= shift; shift++)
			{
				this.terrainFrame.SetFrame(0xac, (ushort)shift);

				short label = this.Owner.World.ChartSight[(ushort)(horizontalLocation + shift), (ushort)(verticalLocation - 1)];
				this.terrainFrame.SetFrame(0xd0, (ushort)label);

				if (label == 0)
				{
					continue;
				}

				if (run != -1 && run != label)
				{
					ushort row;

					for (row = 0; (short)row <= verticalLocation; row++)
					{
						this.terrainFrame.SetFrame(0x4, row);

						ushort column;

						for (column = 0; column < 0x50; column++)
						{
							if (this.Owner.World.ChartSight[column, row] == run)
							{
								this.Owner.World.ChartSight[column, row] = (sbyte)(byte)label;
							}

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

		private void RankLandOrSea(ushort pass)
		{
			const int ranking = 0xce;
			const int numbers = 0xa4;

			for (ushort rank = 0; rank < 0x10; rank++)
			{
				this.terrainFrame.SetFrame(ranking - (rank << 1), 0);
			}

			this.terrainFrame.SetFrame(0xa4, 0);

			ushort label;

			for (label = 1; label < 0x40; label++)
			{
				this.terrainFrame.SetFrame(0x24, label);
				this.terrainFrame.SetFrame(numbers - (label << 1), 0xf);

				ushort size = this.Processor.PullGlobalWord(LabelSizeAddress(pass, label));
				ushort rank;

				for (rank = 1; rank < 0xf; rank++)
				{
					ushort ranked = this.terrainFrame.Frame(ranking - (rank << 1));

					if ((short)this.Processor.PullGlobalWord(LabelSizeAddress(pass, ranked)) >= (short)size)
					{
						continue;
					}

					ushort lower;

					for (lower = 0xf; (short)lower > (short)rank; lower--)
					{
						ushort moved = this.terrainFrame.Frame(ranking - ((lower << 1) - 2));

						this.terrainFrame.SetFrame(numbers - (moved << 1), lower);
						this.terrainFrame.SetFrame(ranking - (lower << 1), moved);
					}

					this.terrainFrame.SetFrame(0xac, lower);

					this.terrainFrame.SetFrame(numbers - (label << 1), rank);
					this.terrainFrame.SetFrame(ranking - (rank << 1), label);
					break;
				}

				this.terrainFrame.SetFrame(0xa8, rank);
			}

			this.terrainFrame.SetFrame(0xa4, 0);

			ushort horizontalLocation, verticalLocation = 0;

			for (horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
			{
				this.terrainFrame.SetFrame(0x24, horizontalLocation);

				for (verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
				{
					this.terrainFrame.SetFrame(0xa8, verticalLocation);

					short tileLabel = this.Owner.World.ChartSight[horizontalLocation, verticalLocation];

					ushort number = tileLabel >= 0 && tileLabel < 0x40 ? this.terrainFrame.Frame(numbers - (tileLabel << 1)) : (ushort)0xf;
					this.terrainFrame.SetFrame(0xa6, number);

					if (number != 0)
					{
						this.Owner.Services.AssignDot((short)horizontalLocation, (short)verticalLocation + 0x32, number);
					}
				}
			}

			this.terrainFrame.SetFrame(0xa8, verticalLocation);

			for (ushort rank = 1; rank < 0xf; rank++)
			{
				int entry = ranking - (rank << 1);

				this.terrainFrame.SetFrame(entry, this.Processor.PullGlobalWord(LabelSizeAddress(pass, this.terrainFrame.Frame(entry))));
			}

			for (ushort rank = 1; rank < 0xf; rank++)
			{
				this.Processor.EmitGlobalWord(LabelSizeAddress(pass, rank), this.terrainFrame.Frame(ranking - (rank << 1)));
			}

			this.terrainFrame.SetFrame(0x24, 0xf);

			this.Processor.EmitGlobalWord(LabelSizeAddress(pass, 0), 0);
			this.Processor.EmitGlobalWord(LabelSizeAddress(pass, 0xf), 1);
		}

		private short FindBlockLand(short horizontalLocation, short verticalLocation, ref (short, short)? land)
		{
			foreach ((short tileHorizontal, short tileVertical) in new[]
			{
				(horizontalLocation, verticalLocation), ((short)(horizontalLocation + 1), verticalLocation),
				(horizontalLocation, (short)(verticalLocation + 1)), ((short)(horizontalLocation + 1), (short)(verticalLocation + 1))
			})
			{
				if (ObtainTerrainTypeFromScreen(tileHorizontal, tileVertical) == 0xa)
				{
					continue;
				}

				land = (tileHorizontal, tileVertical);

				return (short)this.Owner.MapOperations.ObtainContinentIdentifier(tileHorizontal, tileVertical);
			}

			return -1;
		}

		private void LinkLandmassBlock(short column, short row, int bitIndex)
		{
			ushort links = (ushort)((0xd * column) + row + 0xdb44);

			this.Processor.EmitGlobalByte(links, (byte)(this.Processor.PullGlobalByte(links) | (1 << bitIndex)));
		}

		private void LinkSeaBlock(short column, short row, ref short lastNextSea)
		{
			short horizontalLocation = (short)((column << 2) + 1), verticalLocation = (short)((row << 2) + 1);

			if (FindBlockSea(horizontalLocation, verticalLocation) is not ushort sea)
			{
				return;
			}

			for (short direction = 1; direction <= 8; direction++)
			{
				ushort offset = (ushort)(direction << 1);
				short nextHorizontal = (short)((Tables.DirectionHorizontalAt(offset) << 2) + horizontalLocation);
				short nextVertical = (short)((Tables.DirectionVerticalAt(offset) << 2) + verticalLocation);

				if (FindBlockSea(nextHorizontal, nextVertical) is ushort nextSea)
				{
					lastNextSea = (short)nextSea;
				}

				if ((short)sea != lastNextSea)
				{
					continue;
				}

				if (!SeaPassageOpen(horizontalLocation, verticalLocation, offset) && (row != 0xb || direction != 3))
				{
					continue;
				}

				short nextRow = (short)(Tables.DirectionVerticalAt(offset) + row);

				if (nextRow >= 0xc)
				{
					continue;
				}

				LinkSeaBlockBit(column, row, direction - 1);

				short nextColumn = (short)(Tables.DirectionHorizontalAt(offset) + column);

				if (nextColumn < 0 || nextColumn >= 0x14 || nextRow < 0 || nextRow > 0xc)
				{
					continue;
				}

				LinkSeaBlockBit(nextColumn, nextRow, (direction + 3) & 7);
			}
		}

		private ushort? FindBlockSea(short horizontalLocation, short verticalLocation)
		{
			foreach ((short tileHorizontal, short tileVertical) in new[]
			{
				(horizontalLocation, verticalLocation), ((short)(horizontalLocation + 1), verticalLocation),
				(horizontalLocation, (short)(verticalLocation + 1)), ((short)(horizontalLocation + 1), (short)(verticalLocation + 1))
			})
			{
				if (ObtainTerrainTypeFromScreen(tileHorizontal, tileVertical) != 0xa)
				{
					continue;
				}

				ushort continentIdentifier = ObtainContinentIdentifierFromScreen(tileHorizontal, tileVertical);

				return continentIdentifier;
			}

			return null;
		}

		private bool SeaPassageOpen(short horizontalLocation, short verticalLocation, ushort offset)
		{
			for (short step = 0; step <= 4; step++)
			{
				horizontalLocation = (short)(horizontalLocation + Tables.DirectionHorizontalAt(offset));
				verticalLocation = (short)(verticalLocation + Tables.DirectionVerticalAt(offset));

				short ocean = 4;

				foreach ((short tileHorizontal, short tileVertical) in new[]
				{
					(horizontalLocation, verticalLocation), ((short)(horizontalLocation + 1), verticalLocation),
					(horizontalLocation, (short)(verticalLocation + 1)), ((short)(horizontalLocation + 1), (short)(verticalLocation + 1))
				})
				{
					if (ObtainTerrainTypeFromScreen(tileHorizontal, tileVertical) != 0xa || this.Owner.MapOperations.CheckMapLimits(tileHorizontal, tileVertical) == 0)
					{
						ocean--;
					}
				}

				if (ocean < 2)
				{
					return false;
				}
			}

			return true;
		}

		private void LinkSeaBlockBit(short column, short row, int bitIndex)
		{
			ushort links = (ushort)((0xd * column) + row + SeaBlocksAddress);

			this.Processor.EmitGlobalByte(links, (byte)(this.Processor.PullGlobalByte(links) | (1 << bitIndex)));
		}
	}
}
