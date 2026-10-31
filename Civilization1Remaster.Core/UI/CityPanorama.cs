using Civilization1Remaster.Emulation;
using System;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	public class CityPanorama : GameComponent, ICityPanorama
	{
		/// <summary>The columns of the four figures of the parade (words -96, -36, 0, -56; the rows follow at 0x4f4a)</summary>
		private const ushort ParadeFigureColumnsAddress = 0x4f42;

		private const ushort WonderPicturesTableAddress = 0x68a0;
		private const ushort CityWallPicturesTableAddress = 0x6880;
		private const ushort CityStructurePicturesTableAddress = 0x81a2;
		private const ushort CitySceneryPiecesTableAddress = 0x817a;
		private const ushort CityWondersPictureAddress = 0x4eef;
		private const ushort CityWondersInMaskPictureAddress = 0x4efb;

		/// <summary>The segment of the rectangles of the city view pictures</summary>
		private const ushort CitySceneryBoxes = 0x3767;

		private const int SceneryBoxStride = 8;
		private const int SceneryBoxLeft = 0;
		private const int SceneryBoxTop = 2;
		private const int SceneryBoxRight = 4;
		private const int SceneryBoxBottom = 6;
		private const int SceneryBoxWidth = 4;
		private const int SceneryBoxHeight = 6;

		private static readonly GameText CitySceneryPiecesPictureAddress = new(0x4ec8, "citypix1.pic");
		private static readonly GameText CityBuildingsModernPictureAddress = new(0x4ed5, "citypix3.pic");
		private static readonly GameText CityBuildingsAncientPictureAddress = new(0x4ee2, "citypix2.pic");
		private static readonly GameText ParadeAutomobilePictureAddress = new(0x4f08, "invaders.pic");
		private static readonly GameText ParadeTrainPictureAddress = new(0x4f15, "invader2.pic");
		private static readonly GameText ParadeWalkPictureAddress = new(0x4f22, "invader3.pic");

		public CityPanorama(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>Picture handle of the city scenery</summary>
		private ushort CitySceneryHandle => this.Processor.PullGlobalWord(CitySceneryPiecesTableAddress);

		/// <inheritdoc/>
		public void DisplayCityScreen(short townIdx, short improvement)
		{
			this.Processor.Journal.OpenScope($"DisplayCityScreen({townIdx}, {improvement})");

			City settlement = this.Owner.World.Towns[townIdx];

			int scienceTally = this.Owner.World.Participants[settlement.ParticipantIdx].FoundScienceTally >> 1;
			int[,] townArrangement = new int[19, 12];
			int horizLoc1, vertLoc1, horizLoc2, vertLoc2, idx8;
			int quantity;

			for (int idx = 0; idx < 19; idx++)
			{
				for (int jdx = 0; jdx < 12; jdx++)
				{
					townArrangement[idx, jdx] = -1;
				}
			}

			// The text of the buffer, kept while the scene is drawn (the original kept it under SP)
			string savedText = "";

			if (this.Owner.Words.Length < 120)
			{
				savedText = this.Owner.Words.ToString();
			}

			// RNG based on city name
			EmptyMessage()
				.City(townIdx);
			string settlementTitle = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, TextBuffer.Address));
			// string.GetHashCode is different in every run of the program, a stable hash draws the same picture every time
			RandomMT19937 dice = new RandomMT19937(StableTextHash(settlementTitle));

			horizLoc1 = 9;
			vertLoc1 = 11;

			for (int idx = 0, jdx = 0; (settlement.TrueExtent << 3) > idx; idx++)
			{
				if (townArrangement[horizLoc1, vertLoc1] != -1)
				{
					jdx++;

					if (jdx < 999)
					{
						idx--;
					}
				}
				else
				{
					int mergedPlace = this.Owner.Helpers.MergeCoordinates((short)(horizLoc1 - 9), (short)(vertLoc1 - 10));

					townArrangement[horizLoc1, vertLoc1] = Math.Min(Math.Max(((scienceTally - mergedPlace) / 3) - dice.DrawInt(2), scienceTally / 6),
						Math.Min((settlement.TrueExtent / 4) + 6, 9));

					if ((mergedPlace * mergedPlace) > ((int)settlement.TrueExtent << 3))
					{
						horizLoc1 = 9;
						vertLoc1 = 11;
					}
				}

				vertLoc2 = dice.DrawInt(8) + 1;

				horizLoc1 = Math.Min(Math.Max((short)DirectionHoriz(vertLoc2) + horizLoc1, 0), 18);
				vertLoc1 = Math.Min(Math.Max((short)DirectionVert(vertLoc2) + (ushort)vertLoc1, 0), 11);
			}

			vertLoc2 = 4 + dice.DrawInt(2);

			for (int idx = 2; idx < 17; idx++)
			{
				townArrangement[idx, vertLoc2] = -2;
			}

			townArrangement[18, vertLoc2] = -1;
			townArrangement[0, vertLoc2] = -1;

			vertLoc2 = 8 + dice.DrawInt(2);
			idx8 = vertLoc2;

			for (int idx = 2; idx < 17; idx++)
			{
				townArrangement[idx, vertLoc2] = -2;
			}

			townArrangement[18, vertLoc2] = -1;
			townArrangement[0, vertLoc2] = -1;

			horizLoc2 = 9 + dice.DrawInt(4);

			for (int idx = 1; idx < 12; idx++)
			{
				townArrangement[horizLoc2 - ((idx + 1) >> 1), idx] = -2;
			}

			horizLoc2 = 14 + dice.DrawInt(4);

			for (int idx = 1; idx < 12; idx++)
			{
				townArrangement[horizLoc2 - ((idx + 1) >> 1), idx] = -2;
			}

			if (townIdx == this.Owner.World.MarvelTownIdx[7])
			{
				townArrangement[0, 0] = 15;
				townArrangement[0, 1] = 15;
				townArrangement[0, 2] = 15;
				townArrangement[1, 0] = 15;
				townArrangement[1, 1] = 15;
				townArrangement[2, 0] = 15;
			}

			for (int idx = 1; idx <= 21; idx++)
			{
				if (this.Owner.World.MarvelTownIdx[idx] == townIdx && ((1 << idx) & 0x808a) == 0)
				{
					int jdx = 0;

					for (; jdx < 500; jdx++)
					{
						horizLoc1 = dice.DrawInt(15) + 1;
						vertLoc1 = dice.DrawInt(10) + 1;

						int bits = 0;

						if (townArrangement[horizLoc1 - 1, vertLoc1 + 1] >= 0 || townArrangement[horizLoc1, vertLoc1] >= 0 ||
							townArrangement[horizLoc1, vertLoc1 + 1] >= 0 || townArrangement[horizLoc1 + 1, vertLoc1] >= 0 ||
							townArrangement[horizLoc1 + 1, vertLoc1 + 1] >= 0 || townArrangement[horizLoc1 + 2, vertLoc1] >= 0 ||
							jdx > 400)
						{
							bits |= 1;
						}

						if (townArrangement[horizLoc1 - 1, vertLoc1 + 1] >= 15 || townArrangement[horizLoc1, vertLoc1] >= 15 ||
							townArrangement[horizLoc1, vertLoc1 + 1] >= 15 || townArrangement[horizLoc1 + 1, vertLoc1] >= 15 ||
							townArrangement[horizLoc1 + 1, vertLoc1 + 1] >= 15 || townArrangement[horizLoc1 + 2, vertLoc1] >= 15 ||
							townArrangement[horizLoc1 + 2, vertLoc1 + 1] >= 15 ||
							townArrangement[horizLoc1 - 1, vertLoc1 + 1] == -2 || townArrangement[horizLoc1, vertLoc1] == -2 ||
							townArrangement[horizLoc1, vertLoc1 + 1] == -2 || townArrangement[horizLoc1 + 1, vertLoc1] == -2 ||
							townArrangement[horizLoc1 + 1, vertLoc1 + 1] == -2)
						{
							bits |= 2;
						}

						if (bits == 1)
							break;
					}

					if (jdx < 500)
					{
						townArrangement[horizLoc1 - 1, vertLoc1 + 1] = 15;
						townArrangement[horizLoc1, vertLoc1] = idx + 64;
						townArrangement[horizLoc1, vertLoc1 + 1] = 15;
						townArrangement[horizLoc1 + 1, vertLoc1] = 15;
						townArrangement[horizLoc1 + 1, vertLoc1 + 1] = 15;
						townArrangement[horizLoc1 + 1, vertLoc1 - 1] = 15;
						townArrangement[horizLoc1 + 2, vertLoc1] = 15;
						townArrangement[horizLoc1 + 2, vertLoc1 - 1] = 15;
						townArrangement[horizLoc1 + 2, vertLoc1 + 1] = 15;
						townArrangement[horizLoc1 + 3, vertLoc1 - 1] = 15;

						if (vertLoc1 < 7 && (townArrangement[horizLoc1 + 1, vertLoc1 + 2] & 7) == 0)
						{
							townArrangement[horizLoc1 + 1, vertLoc1 + 2] = -1;
						}
					}
				}
			}

			for (int idx = 23; idx > 0; idx--)
			{
				while (idx > 0 && (((1 << idx) & settlement.Structures) == 0 ||
					idx == 7 || idx == 8 || idx == 12 || idx == 18 || idx == 19))
				{
					idx--;
				}

				if (idx < 1)
					break;

				int jdx = 0;

				for (; jdx < 500; jdx++)
				{
					horizLoc1 = dice.DrawInt(16) + 1;
					vertLoc1 = dice.DrawInt(10) + 1;

					int bits = 0;

					if (townArrangement[horizLoc1 - 1, vertLoc1 + 1] >= 0 || townArrangement[horizLoc1, vertLoc1] >= 0 ||
						townArrangement[horizLoc1, vertLoc1 + 1] >= 0 || townArrangement[horizLoc1 + 1, vertLoc1] >= 0 ||
						townArrangement[horizLoc1 + 1, vertLoc1 + 1] >= 0 ||
						jdx > 350)
					{
						bits |= 1;
					}

					if (townArrangement[horizLoc1 - 1, vertLoc1 + 1] >= 15 || townArrangement[horizLoc1, vertLoc1] >= 15 ||
						townArrangement[horizLoc1, vertLoc1 + 1] >= 15 || townArrangement[horizLoc1 + 1, vertLoc1] >= 15 ||
						townArrangement[horizLoc1 + 1, vertLoc1 + 1] >= 15 ||
						townArrangement[horizLoc1 - 1, vertLoc1 + 1] == -2 || townArrangement[horizLoc1, vertLoc1] == -2 ||
						townArrangement[horizLoc1, vertLoc1 + 1] == -2 || townArrangement[horizLoc1 + 1, vertLoc1] == -2 ||
						townArrangement[horizLoc1 + 1, vertLoc1 + 1] == -2)
					{
						bits |= 2;
					}

					if (bits == 1)
						break;
				}

				if (jdx < 500)
				{
					townArrangement[horizLoc1 - 1, vertLoc1 + 1] = 15;
					townArrangement[horizLoc1, vertLoc1] = idx + 15;
					townArrangement[horizLoc1, vertLoc1 + 1] = 15;
					townArrangement[horizLoc1 + 1, vertLoc1] = 15;
					townArrangement[horizLoc1 + 1, vertLoc1 + 1] = 15;

					if (vertLoc1 < 7)
					{
						if ((townArrangement[horizLoc1 + 1, vertLoc1 + 2] & 7) == 0)
						{
							townArrangement[horizLoc1 + 1, vertLoc1 + 2] = -1;
						}
					}
				}
			}

			if (settlement.Has(CityStructure.HydroPlant))
			{
				townArrangement[17, 7] = 34;
				townArrangement[17, 8] = 15;
				townArrangement[18, 7] = 15;
				townArrangement[18, 8] = 15;
			}

			if (settlement.Has(CityStructure.Aqueduct))
			{
				townArrangement[0, idx8 - 1] = 23;
				townArrangement[0, idx8] = 15;
				townArrangement[1, idx8] = 15;
				townArrangement[1, idx8 + 1] = 15;
				townArrangement[2, idx8] = 15;
				townArrangement[2, idx8 + 1] = 15;
			}

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.QueryForDisk(1, 1);

			RenderCityBuildings((ushort)improvement, settlement.ParticipantIdx, townIdx);

			int idx3 = 0;

			do
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileHillPic, 0);

				for (int idx = 1; idx < 8; idx++)
				{
					int marvelIdx = idx;

					if (idx == 2)
					{
						marvelIdx = 15;
					}

					if (this.Owner.World.MarvelTownIdx[marvelIdx] == townIdx)
					{
						if ((marvelIdx + 24) != improvement)
						{
							if (((1 << marvelIdx) & 0x808a) != 0)
							{
								this.Owner.Services.RenderPictureToScreen(
									BackScreenBox,
									(((marvelIdx == 3) ? 80 : 0) +
										(((short)SceneryBoxField(marvelIdx, SceneryBoxLeft) < 10) ? -1 : 2) +
										(short)SceneryBoxField(marvelIdx, SceneryBoxLeft)),
									((marvelIdx <= 7) ? 0 : ((short)SceneryBoxField(marvelIdx, SceneryBoxTop) - 6)),
									WonderPicture(marvelIdx));
							}
						}
					}
				}

				if (settlement.Has(CityStructure.PowerPlant) && improvement != 19)
				{
					for (int idx = (townIdx == this.Owner.World.MarvelTownIdx[7]) ? 69 : 0; idx < 310; idx += 45)
					{
						this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox,
							idx, 2, CityStructurePicture(17));
					}
				}

				for (vertLoc1 = 0; vertLoc1 < 12; vertLoc1++)
				{
					for (horizLoc1 = 0; horizLoc1 < 19; horizLoc1++)
					{
						quantity = townArrangement[horizLoc1, vertLoc1];

						if (quantity == -2)
						{
							int heading = 0;

							for (int idx = 1; idx < 9; idx++)
							{
								int leadScratch;

								if ((vertLoc1 & 0x1) == 0)
								{
									leadScratch = (((short)DirectionVert(idx) <= 0) ? 0 : -1);
								}
								else
								{
									leadScratch = ((short)DirectionVert(idx) >= 0) ? 0 : 1;
								}

								int horizScratch = leadScratch + (short)DirectionHoriz(idx) + horizLoc1;
								int vertScratch = (short)DirectionVert(idx) + vertLoc1;

								if (vertScratch < 12 && townArrangement[horizScratch, vertScratch] >= 0)
								{
									heading = 1;
								}
							}

							if (heading == 0)
							{
								townArrangement[horizLoc1, vertLoc1] = -1;
							}
						}
					}
				}

				for (vertLoc1 = 0; vertLoc1 < 12; vertLoc1++)
				{
					for (horizLoc1 = 0; horizLoc1 < 19; horizLoc1++)
					{
						quantity = townArrangement[horizLoc1, vertLoc1];

						if (quantity == -1 && horizLoc1 > 2 && townArrangement[horizLoc1 - 1, vertLoc1] != -1 && ((horizLoc1 + vertLoc1) & 1) != 0)
						{
							RenderCityPicture((ushort)horizLoc1, (ushort)vertLoc1, 0);
						}

						if (quantity != -1 && quantity != 15)
						{
							if (quantity != -2)
							{
								if (quantity >= 16)
								{
									if (improvement + 14 != quantity && improvement + 40 != quantity)
									{
										RenderCityResourceBar((ushort)horizLoc1, (ushort)vertLoc1, (ushort)(quantity - 16));
									}
								}
								else if (quantity >= 2)
								{
									RenderCityPictureRegion((ushort)horizLoc1, (ushort)vertLoc1, (ushort)(((horizLoc1 + vertLoc1) & 1) + ((quantity & 1) << 1)), (ushort)(quantity >> 1));
								}
								else
								{
									RenderCityPictureRegion((ushort)horizLoc1, (ushort)vertLoc1, (ushort)(((quantity & 1) << 1) + (townIdx & 1)), (ushort)(quantity >> 1));
								}
							}
							else
							{
								int tag = 0;

								for (int idx = 1; idx < 9; idx++)
								{
									int leadScratch;
									if ((vertLoc1 & 1) == 0)
									{
										leadScratch = ((short)DirectionVert(idx) <= 0) ? 0 : -1;
									}
									else
									{
										leadScratch = ((short)DirectionVert(idx) >= 0) ? 0 : 1;
									}

									int horizScratch = leadScratch + (short)DirectionHoriz(idx) + horizLoc1;
									int vertScratch = (short)DirectionVert(idx) + vertLoc1;

									if ((idx & 1) != 0)
									{
										tag >>= 1;

										if (vertScratch < 12 && townArrangement[horizScratch, vertScratch] == -2)
										{
											tag |= 8;
										}
									}
								}

								RenderCityPicture((ushort)horizLoc1, (ushort)vertLoc1, (ushort)tag);
							}
						}
					}
				}
				
				if (settlement.Has(CityStructure.CityWalls) && improvement != 8)
				{
					for (int idx = 0; idx < 320; idx += 43)
					{
						if (idx == 172)
						{
							idx += 19;
						}

						this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox,
							idx, 108, CityStructurePicture(22));
					}

					this.Owner.Services.RenderPictureToScreen(BackScreenBox,
						142, 108, CityStructurePicture(6));
				}

				if (settlement.Has(CityStructure.MassTransit) && improvement != 13)
				{
					for (int idx = 0; idx < 310; idx += 46)
					{
						this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox,
							idx, 115, CityStructurePicture(21));
					}

					this.Owner.Services.RenderPictureToScreen(BackScreenBox,
						0, 115, CityStructurePicture(11));
				}

				this.Processor.EmitUInt16(this.Processor.SegData.Value16, ScreenBox, 0x1);
				DrawingBox.TypefaceIdx = 0x6;

				if (savedText.Length == 0)
				{
					EmptyMessage()
						.City(townIdx);

					this.Owner.TextPainter.RenderCenteredTextWithShadowToScreen0(this.Owner.Words.ToString(), 160, 2, Hues.White);

					this.Owner.Words.Clear();

					this.Owner.TurnHooks.ObtainCurrentYearAsText();

					this.Owner.TextPainter.RenderCenteredTextWithShadowToScreen0(this.Owner.Words.ToString(), 160, 15, Hues.White);
				}
				else
				{
					DialogNeedsRedraw = 1;

					this.Owner.Dialogs.DisplayMenu(savedText, 80, 8, 1);
				}

				this.Processor.EmitUInt16(this.Processor.SegData.Value16, ScreenBox, 0x0);
				DrawingBox.TypefaceIdx = 0x1;

				if (idx3 != 0)
				{
					this.Owner.Services.PerformSound(0x2c, 0);

					this.Owner.VideoCard.RenderScreenToMainScreen(1, 10);
				}
				else
				{
					this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

					if (improvement != -3)
					{
						this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileHillPicB, 1);
					}

					this.Owner.VideoCard.RenderImage(
						BackScreenBox,
						0, 0, 320, 200,
						ScreenBox,
						0, 0);

					if (improvement == -3 && VgaMode != 0)
					{
						this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileHillPal, 0xbdee);

						this.Owner.Services.ImportColorsFromBuffer(15, 0xbdee);
					}

					if (VgaMode != 0)
					{
						this.Owner.Services.AssignHueCycleSpan(4, 15, 64, 79);

						this.Owner.Services.ActivateHueCycle(4);
					}
				}

				if (improvement == -1 && idx3 == 0)
				{
					this.Owner.Helpers.ReleaseResource(CitySceneryHandle, 0x4eb3);

					RenderCityScenery(townIdx, 0x18, 0x8c, 0x100);
				}

				if (improvement <= -1 || this.Owner.Libc.KeyAvailable() != 0)
					break;

				improvement = -1;
				idx3 = 1;

				this.Owner.TextPainter.AwaitTime(60);
			}
			while (this.Owner.Libc.KeyAvailable() == 0);

			if (improvement != -1 || idx3 != 0)
			{
				this.Owner.Helpers.ReleaseResource(CitySceneryHandle, 0x4eb8);
			}

			if (improvement == -2)
			{
				if (VgaMode != 0)
				{
					this.Owner.Services.DeactivateHueCycle(4);
				}
			}
			else
			{
				if (this.Owner.Libc.KeyAvailable() != 0)
				{
					this.Owner.TurnLogic.AwaitForKey();
				}
				else
				{
					this.Owner.Conquest.AwaitForKeyPressOrMouseClick();
				}

				if (VgaMode != 0)
				{
					this.Owner.Services.DeactivateHueCycle(4);
				}

				this.Owner.Startup.QueryForDisk(0, 1);

				if (VgaMode != 0)
				{
					this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

					this.Owner.TurnHooks.DisplayCivilizationScenery();
				}

				this.Owner.Helpers.LaunchHueCycling();
			}

			this.Processor.Journal.CloseScope("DisplayCityScreen");
		}

		/// <inheritdoc/>
		public void RenderCityPicture(ushort column, ushort row, ushort pictureIdx)
		{
			this.Processor.Journal.OpenScope($"RenderCityPicture({column}, {row}, {pictureIdx})");

			this.Owner.Services.RenderPictureToScreen(BackScreenBox,
				((row & 1) * 8) + (column * 16) - 5,
				(row * 8) + 50,
				CityWallPicture(pictureIdx));

			this.Processor.Journal.CloseScope("RenderCityPicture");
		}

		/// <inheritdoc/>
		public void RenderCityPictureRegion(ushort column, ushort row, ushort picture, ushort kind)
		{
			this.Processor.Journal.OpenScope($"RenderCityPictureRegion({column}, {row}, {picture}, {kind})");

			this.Owner.Services.RenderPictureToScreen(BackScreenBox,
				((row & 1) << 3) + (column << 4),
				(row << 3) + 26,
				CitySceneryPiecePicture(picture, kind));

			this.Processor.Journal.CloseScope("RenderCityPictureRegion");
		}

		/// <inheritdoc/>
		public void RenderCityResourceBar(ushort column, ushort row, ushort picture)
		{
			this.Processor.Journal.OpenScope($"RenderCityResourceBar({column}, {row}, {picture})");

			if ((short)picture < 0x30)
			{
				int horizLoc = this.Owner.Helpers.VerifyValueSpan(((row & 1) * 8) + (column * 16) - 9, 0, 999);

				this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox,
					(short)horizLoc,
					(short)((short)(row << 3) + 16),
					CityStructurePicture(picture));
			}
			else
			{
				this.Processor.SegExtra.Value16 = 0x3767;
				picture -= 0x30;
				row = (ushort)((short)(row << 3) - this.Processor.PullInt16(this.Processor.SegExtra.Value16, (ushort)((picture << 3) + 6)) + 76);

				int horizLoc = this.Owner.Helpers.VerifyValueSpan(((row & 1) * 8) + (column * 16) - 9, 0, 999);

				this.Owner.Services.RenderPictureToScreen(BackScreenBox,
					(short)horizLoc,
					(short)row - 10,
					WonderPicture(picture));
			}

			this.Processor.Journal.CloseScope("RenderCityResourceBar");
		}

		/// <inheritdoc/>
		public void RenderCityScenery(short townIdx, ushort rowHoriz, ushort rowVert, ushort breadth)
		{
			this.Processor.Journal.OpenScope($"RenderCityScenery({townIdx}, {rowHoriz}, {rowVert}, {breadth})");

			// The pictures are in the stack frame, like in the original, and the final values of the locals stay there

			ushort[] pictures = new ushort[9];

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FilePopPic, 0);

			for (int pictureIdx = 0; pictureIdx < 9; pictureIdx++)
			{
				ushort vertLoc = this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x23) == 0 ? (ushort)0x34 : (ushort)1;

				pictures[pictureIdx] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x23 * pictureIdx) + 1), vertLoc, 0x22, 0x32);
			}

			sbyte size = this.Owner.World.Towns[townIdx].TrueExtent;
			short spacing = (short)(12 * size) > (short)breadth ? CPU.SignedQuotient((short)breadth, size) : (short)12;

			short happy = (short)this.Owner.Helpers.VerifyValueSpan((short)CityHappy, 0, this.Owner.World.Towns[townIdx].TrueExtent);
			short unhappy = (short)this.Owner.Helpers.VerifyValueSpan((short)CityUnhappy, 0, this.Owner.World.Towns[townIdx].TrueExtent);

			while ((short)(happy + unhappy) > (short)this.Owner.Helpers.VerifyValueSpan(this.Owner.World.Towns[townIdx].TrueExtent - (short)CitySpecialists, 0, 99))
			{
				happy = (short)this.Owner.Helpers.VerifyValueSpan((short)(happy - 1), 0, this.Owner.World.Towns[townIdx].TrueExtent);
				unhappy = (short)this.Owner.Helpers.VerifyValueSpan((short)(unhappy - 1), 0, this.Owner.World.Towns[townIdx].TrueExtent);
			}

			short citizenIdx;

			for (citizenIdx = 0; citizenIdx < happy; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, citizenIdx & 1, ref rowHoriz, rowVert, spacing);
			}

			if (happy != 0)
				rowHoriz += 8;

			for (citizenIdx = 0; (short)(this.Owner.World.Towns[townIdx].TrueExtent - happy - unhappy - CitySpecialists) > citizenIdx; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, 2 + (citizenIdx & 1), ref rowHoriz, rowVert, spacing);
			}

			if (citizenIdx != 0)
				rowHoriz += 8;

			for (citizenIdx = 0; citizenIdx < unhappy; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, 4 + (citizenIdx & 1), ref rowHoriz, rowVert, spacing);
			}

			rowHoriz += 12;

			for (citizenIdx = 0; citizenIdx < (short)CitySpecialists; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, 5 + this.Owner.CityOps.ObtainCitizenType((ushort)citizenIdx), ref rowHoriz, rowVert, spacing);
			}

			this.Owner.Helpers.ReleaseResource(pictures[0], 0x4ec4);

			this.Processor.Journal.CloseScope("RenderCityScenery");
		}

		/// <inheritdoc/>
		public void RenderCityBuildings(ushort improvement, short participantIdx, short townIdx)
		{
			this.Processor.Journal.OpenScope($"RenderCityBuildings({improvement}, {participantIdx}, {townIdx})");

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, CitySceneryPiecesPictureAddress, 0);

			for (int pieceIdx = 0; pieceIdx <= 4; pieceIdx++)
			{
				ushort horizLoc = (ushort)(pieceIdx << 6);
				ushort pictures = (ushort)((pieceIdx << 3) + CitySceneryPiecesTableAddress);

				this.Processor.EmitGlobalWord(pictures, this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 1), 1, 0x1f, 0x1f));
				this.Processor.EmitGlobalWord((ushort)(pictures + 4), this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 1), 0x21, 0x1f, 0x1f));
				this.Processor.EmitGlobalWord((ushort)(pictures + 2), this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x21), 1, 0x1f, 0x1f));
				this.Processor.EmitGlobalWord((ushort)(pictures + 6), this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x21), 0x21, 0x1f, 0x1f));
			}

			for (int wallIdx = 0; wallIdx < 0x10; wallIdx++)
			{
				ushort top = (ushort)(((wallIdx >> 3) << 3) + (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x3a) == 0 ? 0x41 : 0x51));

				SetCityWallPicture(wallIdx,
					this.Owner.VideoCard.ScreenToPicture(1, (ushort)((wallIdx & 7) * 0x18), top, 0x18, 0x8));
			}

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x25) != 0 ? CityBuildingsModernPictureAddress : CityBuildingsAncientPictureAddress), 0);

			City town = this.Owner.World.Towns[townIdx];

			for (int structureIdx = 0; structureIdx < 0x17; structureIdx++)
			{
				uint mask = 1u << (structureIdx + 1);
				bool built = ((mask & town.StructureMarkers0) | ((mask >> 16) & town.StructureMarkers1)) != 0;

				if (built || (ushort)(improvement - 1) == structureIdx || structureIdx > 0x14)
				{
					SetCityStructurePicture(structureIdx,
						this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((structureIdx >> 2) * 0x32) + 1), (ushort)(((structureIdx & 3) * 0x32) + 1), 0x31, 0x31));
				}
			}

			CutWonderPictures(townIdx, false, CityWondersPictureAddress);
			CutWonderPictures(townIdx, true, CityWondersInMaskPictureAddress);

			this.Processor.Journal.CloseScope("RenderCityBuildings");
		}

		/// <inheritdoc/>
		public ushort SeizeCityScenery(ushort picture)
		{
			this.Processor.Journal.OpenScope($"SeizeCityScenery({picture})");

			this.Processor.SegExtra.Value16 = CitySceneryBoxes;

			if ((short)SceneryBoxField(1, SceneryBoxRight) > 0x12c)
			{
				for (int boxIdx = 1; boxIdx <= 0x15; boxIdx++)
				{
					SetSceneryBoxField(boxIdx, SceneryBoxWidth,
						(ushort)(SceneryBoxField(boxIdx, SceneryBoxRight) - SceneryBoxField(boxIdx, SceneryBoxLeft) + 1));
					SetSceneryBoxField(boxIdx, SceneryBoxHeight,
						(ushort)(SceneryBoxField(boxIdx, SceneryBoxBottom) - SceneryBoxField(boxIdx, SceneryBoxTop) + 1));
				}
			}

			ushort handle = this.Owner.VideoCard.ScreenToPicture(1,
				SceneryBoxField(picture, SceneryBoxLeft),
				SceneryBoxField(picture, SceneryBoxTop),
				SceneryBoxField(picture, SceneryBoxWidth),
				SceneryBoxField(picture, SceneryBoxHeight));

			this.Processor.Journal.CloseScope("SeizeCityScenery");

			return handle;
		}

		/// <inheritdoc/>
		public void RenderCitySceneryForPlayer(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"RenderCitySceneryForPlayer({participantIdx})");

			// The pictures are in the stack frame, like in the original, and the final values of the locals stay there

			ushort[] pictures = new ushort[10];

			this.Owner.Helpers.HaltHueCycling();

			if (VgaMode != 0)
			{
				this.Owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(3, 3, 3));
			}

			this.Owner.Startup.ConcealMouseCursor();

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22) != 0)
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x3a) != 0 ? ParadeAutomobilePictureAddress : ParadeTrainPictureAddress, 0);

				CutParadePictures(pictures, 0, 0x3d, 2, 0x3c);
			}
			else
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, ParadeWalkPictureAddress, 0);

				CutParadePictures(pictures, 1, 0x42, 1, 0x41);
			}

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0x64, 0x140, 0x64, BackScreenBox, 0, 0);

			ushort frameIdx = 0;
			short offset;
			short figureHoriz = 0;

			for (offset = 0; offset < 0x294; offset += 3)
			{
				this.Owner.Services.RestoreTimer();

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0x64, BackScreenBox, 0, 0x64);

				for (short figureIdx = 7; figureIdx >= 0; figureIdx--)
				{
					figureHoriz = (short)(offset - (0x30 * figureIdx));

					if (figureHoriz >= 0x140 || figureHoriz <= -0x30)
						continue;

					this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox, figureHoriz, 132,
						pictures[(short)frameIdx % 10]);
				}

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x64, 0x140, 0x64, ScreenBox, 0, 0x64);

				frameIdx++;

				if (ParadeInterrupted())
					break;
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.TurnLogic.AwaitForKey();

			this.Owner.Helpers.ReleaseResource(pictures[0], 0);

			this.Owner.Helpers.LaunchHueCycling();

			this.Processor.Journal.CloseScope("RenderCitySceneryForPlayer");
		}

		/// <inheritdoc/>
		public void RiotAnimation()
		{
			this.Processor.Journal.OpenScope("RiotAnimation()");

			ushort[] pictures = new ushort[10];

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.ConcealMouseCursor();

			if (this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 13) != 0)
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileRiotPic, 0);

				for (int idx = 0; idx < 10; idx++)
				{
					pictures[idx] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((idx % 4) * 79) + 1), (ushort)(((idx >> 2) * 64) + 1), 78, 63);
				}
			}
			else
			{
				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileRiot2Pic, 0);

				for (int idx = 0; idx < 10; idx++)
				{
					pictures[idx] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((idx % 4) * 75) + 1), (ushort)(((idx >> 2) * 66) + 1), 74, 65);
				}
			}

			// Draw City View to screen 0
			this.Owner.VideoCard.RenderImage(
				ScreenBox,
				0, 100, 320, 100,
				BackScreenBox,
				0, 0);

			for (int idx = -48, jdx = 0; idx < 420; idx += 3, jdx++)
			{
				this.Owner.Services.RestoreTimer();

				// Draw a lower portion of City View to screen 0
				this.Owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 320, 100,
					BackScreenBox,
					0, 100);

				for (int kdx = 0; kdx < 4; kdx++)
				{
					int horizSpot = this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(ParadeFigureColumnsAddress + (kdx << 1))) + idx;

					if (horizSpot > -48 && horizSpot < 320)
					{
						this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox,
							horizSpot, this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)((kdx << 1) + 0x4f4a)) + 132,
							pictures[(kdx * 3 + jdx) % 10]);
					}				
				}

				// Draw a lower portion of City View to screen 0
				this.Owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 100, 320, 100,
					ScreenBox,
					0, 100);

				while (this.Owner.Timer.Beats < 10) ;

				this.Owner.Startup.ObtainMouseState();

				if (this.Owner.Libc.KeyAvailable() != 0 || this.Owner.GlobDb3a != 0)
					break;
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.TurnLogic.AwaitForKey();

			for (int idx = 0; idx < 10; idx++)
			{
				this.Owner.Helpers.ReleaseResource(pictures[idx], 0);
			}

			this.Processor.Journal.CloseScope("RiotAnimation");
		}

		/// <inheritdoc/>
		public void RenderCitySceneryForHuman()
		{
			this.Processor.Journal.OpenScope("RenderCitySceneryForHuman()");

			// The pictures are in the stack frame, like in the original, and the final values of the locals stay there

			ushort[] pictures = new ushort[10];

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0,
				this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x23) != 0 ? (ushort)0x4f5c : (ushort)0x4f52, 0);

			CutParadePictures(pictures, 1, 0x42, 1, 0x41);

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0x64, 0x140, 0x64, BackScreenBox, 0, 0);

			ushort frameIdx = 0;
			short offset;
			short figureHoriz = 0;

			for (offset = 0x1a4; offset > -0x30; offset -= 3)
			{
				this.Owner.Services.RestoreTimer();

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0x64, BackScreenBox, 0, 0x64);

				for (short figureIdx = 0; figureIdx < 4; figureIdx++)
				{
					figureHoriz = (short)(((short)(3 * this.Processor.PullGlobalWord((ushort)((figureIdx << 1) + ParadeFigureColumnsAddress))) / 2) + offset);

					if (figureHoriz >= 0x140 || figureHoriz <= -0x30)
						continue;

					this.Owner.Services.RenderPictureToScreenAlternate(BackScreenBox, figureHoriz,
						this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)((figureIdx << 1) + 0x4f4a)) + 132,
						pictures[(short)((3 * figureIdx) + frameIdx) % 10]);
				}

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x64, 0x140, 0x64, ScreenBox, 0, 0x64);

				frameIdx++;

				if (ParadeInterrupted())
					break;
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.TurnLogic.AwaitForKey();

			this.Owner.Helpers.ReleaseResource(pictures[0], 0);

			this.Processor.Journal.CloseScope("RenderCitySceneryForHuman");
		}

		/// <summary>
		/// FNV-1a hash of the text, the same in every run of the program
		/// </summary>
		private static uint StableTextHash(string words)
		{
			uint hash = 2166136261;

			for (int idx = 0; idx < words.Length; idx++)
			{
				hash ^= words[idx];
				hash *= 16777619;
			}

			return hash;
		}

		/// <summary>Picture handle of a city view structure from table at 0x81a2</summary>
		private ushort CityStructurePicture(int structureIdx) => this.Processor.PullGlobalWord((ushort)((structureIdx << 1) + CityStructurePicturesTableAddress));

		private void SetCityStructurePicture(int structureIdx, ushort handle) => this.Processor.EmitGlobalWord((ushort)((structureIdx << 1) + CityStructurePicturesTableAddress), handle);

		private ushort CitySceneryPiecePicture(int x, int y) => this.Processor.PullGlobalWord((ushort)((x << 1) + (y << 3) + CitySceneryPiecesTableAddress));

		private ushort CityWallPicture(int pictureIdx) => this.Processor.PullGlobalWord((ushort)((pictureIdx << 1) + CityWallPicturesTableAddress));

		private void SetCityWallPicture(int wallIdx, ushort handle) => this.Processor.EmitGlobalWord((ushort)((wallIdx << 1) + CityWallPicturesTableAddress), handle);

		private ushort WonderPicture(int marvelIdx) => this.Processor.PullGlobalWord((ushort)((marvelIdx << 1) + WonderPicturesTableAddress));

		private void SetWonderPicture(int wonderIdx, ushort handle) => this.Processor.EmitGlobalWord((ushort)((wonderIdx << 1) + WonderPicturesTableAddress), handle);

		/// <summary>One citizen of the city scenery: picture pictureIdx of the frame at (horizLoc, vertLoc), then the next place</summary>
		private void RenderSceneryCitizen(ushort[] pictures, int pictureIdx, ref ushort horizLoc, ushort vertLoc, short spacing)
		{
			this.Owner.Services.RenderPictureToScreen(
				ScreenBox,
				(short)horizLoc,
				(short)vertLoc,
				pictures[pictureIdx]);

			horizLoc += (ushort)spacing;
		}

		/// <summary>
		/// The wonder pictures of RenderCityBuildings: for each wonder of the city inside (or outside) mask 0x808a the picture
		/// cut by SeizeCityScenery (AX) goes to 0x68a0 + 2 * wonder; the picture file is loaded before the first one
		/// </summary>
		private void CutWonderPictures(short townIdx, bool inMask, ushort docnameAddr)
		{
			bool loaded = false;

			for (int wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
			{
				if (this.Owner.World.MarvelTownIdx[wonderIdx] != townIdx || (((1 << wonderIdx) & 0x808a) != 0) != inMask)
					continue;

				if (!loaded)
				{
					this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docnameAddr, 0);
				}

				loaded = true;

				ushort pic = SeizeCityScenery((ushort)wonderIdx);

				SetWonderPicture(wonderIdx, pic);
			}
		}

		private ushort SceneryBoxField(int boxIdx, int fieldOffset) =>
			this.Processor.PullUInt16(CitySceneryBoxes, (ushort)((boxIdx * SceneryBoxStride) + fieldOffset));

		private void SetSceneryBoxField(int boxIdx, int fieldOffset, ushort value) =>
			this.Processor.EmitUInt16(CitySceneryBoxes, (ushort)((boxIdx * SceneryBoxStride) + fieldOffset), value);

		/// <summary>
		/// The 10 pictures of the parade cut from screen 1 (4 a row, 0x4f apart) into the frame
		/// </summary>
		private void CutParadePictures(ushort[] pictures, ushort left, ushort rowHeight, ushort top, ushort altitude)
		{
			for (int pictureIdx = 0; pictureIdx < 0xa; pictureIdx++)
			{
				pictures[pictureIdx] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIdx % 4) * 0x4f) + left), (ushort)(((pictureIdx >> 2) * rowHeight) + top), 0x4e, altitude);
			}
		}

		/// <summary>Waits for 10 timer ticks (since RestoreTimer), then a key or a click ends the parade</summary>
		private bool ParadeInterrupted()
		{
			while ((short)this.Owner.Timer.Beats < 0xa)
			{
			}

			this.Owner.Startup.ObtainMouseState();

			return (ushort)this.Owner.Libc.KeyAvailable() != 0 || this.Owner.GlobDb3a != 0;
		}
	}
}
