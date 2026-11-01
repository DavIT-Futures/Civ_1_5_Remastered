using System;

namespace Civ_1_5_Remastered.Core.UI.CityScreens
{
	public class CityPanorama : GameComponent, ICityPanorama
	{
		private const ushort ParadeFigureColumnsAddress = 0x4f42;

		private const ushort WonderPicturesTableAddress = 0x68a0;
		private const ushort CityWallPicturesTableAddress = 0x6880;
		private const ushort CityStructurePicturesTableAddress = 0x81a2;
		private const ushort CitySceneryPiecesTableAddress = 0x817a;
		private const ushort CityWondersPictureAddress = 0x4eef;
		private const ushort CityWondersInMaskPictureAddress = 0x4efb;

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

		public CityPanorama(IGameEngine owner)
			: base(owner)
		{
		}

		private ushort CitySceneryHandle => this.Processor.PullGlobalWord(CitySceneryPiecesTableAddress);

		public void ShowCityScreen(short townIndex, short improvement)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCityScreen({townIndex}, {improvement})");

			City settlement = this.Owner.World.Towns[townIndex];

			int scienceTally = this.Owner.World.Participants[settlement.ParticipantIndex].FoundScienceTally >> 1;
			int[,] townArrangement = new int[19, 12];
			int firstHorizontalLocation, firstVerticalLocation, secondHorizontalLocation, secondVerticalLocation, featureRow;
			int quantity;

			for (int index = 0; index < 19; index++)
			{
				for (int innerIndex = 0; innerIndex < 12; innerIndex++)
				{
					townArrangement[index, innerIndex] = -1;
				}
			}

			string savedText = string.Empty;

			if (this.Owner.Words.Length < 120)
			{
				savedText = this.Owner.Words.ToString();
			}

			EmptyMessage()
				.City(townIndex);
			string settlementTitle = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, TextBuffer.Address));
			RandomGenerator dice = new RandomGenerator(StableTextHash(settlementTitle));

			firstHorizontalLocation = 9;
			firstVerticalLocation = 11;

			for (int index = 0, innerIndex = 0; (settlement.TrueExtent << 3) > index; index++)
			{
				if (townArrangement[firstHorizontalLocation, firstVerticalLocation] != -1)
				{
					innerIndex++;

					if (innerIndex < 999)
					{
						index--;
					}
				}
				else
				{
					int mergedPlace = this.Owner.Helpers.MergeCoordinates((short)(firstHorizontalLocation - 9), (short)(firstVerticalLocation - 10));

					townArrangement[firstHorizontalLocation, firstVerticalLocation] = Math.Min(Math.Max(((scienceTally - mergedPlace) / 3) - dice.DrawInteger(2), scienceTally / 6),
						Math.Min((settlement.TrueExtent / 4) + 6, 9));

					if ((mergedPlace * mergedPlace) > ((int)settlement.TrueExtent << 3))
					{
						firstHorizontalLocation = 9;
						firstVerticalLocation = 11;
					}
				}

				secondVerticalLocation = dice.DrawInteger(8) + 1;

				firstHorizontalLocation = Math.Min(Math.Max((short)Tables.DirectionHorizontal(secondVerticalLocation) + firstHorizontalLocation, 0), 18);
				firstVerticalLocation = Math.Min(Math.Max((short)Tables.DirectionVertical(secondVerticalLocation) + (ushort)firstVerticalLocation, 0), 11);
			}

			secondVerticalLocation = 4 + dice.DrawInteger(2);

			for (int index = 2; index < 17; index++)
			{
				townArrangement[index, secondVerticalLocation] = -2;
			}

			townArrangement[18, secondVerticalLocation] = -1;
			townArrangement[0, secondVerticalLocation] = -1;

			secondVerticalLocation = 8 + dice.DrawInteger(2);
			featureRow = secondVerticalLocation;

			for (int index = 2; index < 17; index++)
			{
				townArrangement[index, secondVerticalLocation] = -2;
			}

			townArrangement[18, secondVerticalLocation] = -1;
			townArrangement[0, secondVerticalLocation] = -1;

			secondHorizontalLocation = 9 + dice.DrawInteger(4);

			for (int index = 1; index < 12; index++)
			{
				townArrangement[secondHorizontalLocation - ((index + 1) >> 1), index] = -2;
			}

			secondHorizontalLocation = 14 + dice.DrawInteger(4);

			for (int index = 1; index < 12; index++)
			{
				townArrangement[secondHorizontalLocation - ((index + 1) >> 1), index] = -2;
			}

			if (townIndex == this.Owner.World.MarvelTownIndex[7])
			{
				townArrangement[0, 0] = 15;
				townArrangement[0, 1] = 15;
				townArrangement[0, 2] = 15;
				townArrangement[1, 0] = 15;
				townArrangement[1, 1] = 15;
				townArrangement[2, 0] = 15;
			}

			for (int index = 1; index <= 21; index++)
			{
				if (this.Owner.World.MarvelTownIndex[index] == townIndex && ((1 << index) & 0x808a) == 0)
				{
					int innerIndex = 0;

					for (; innerIndex < 500; innerIndex++)
					{
						firstHorizontalLocation = dice.DrawInteger(15) + 1;
						firstVerticalLocation = dice.DrawInteger(10) + 1;

						int bits = 0;

						if (townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] >= 0 || townArrangement[firstHorizontalLocation, firstVerticalLocation] >= 0 ||
							townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] >= 0 || townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] >= 0 ||
							townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] >= 0 || townArrangement[firstHorizontalLocation + 2, firstVerticalLocation] >= 0 ||
							innerIndex > 400)
						{
							bits |= 1;
						}

						if (townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] >= 15 || townArrangement[firstHorizontalLocation, firstVerticalLocation] >= 15 ||
							townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] >= 15 || townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] >= 15 ||
							townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] >= 15 || townArrangement[firstHorizontalLocation + 2, firstVerticalLocation] >= 15 ||
							townArrangement[firstHorizontalLocation + 2, firstVerticalLocation + 1] >= 15 ||
							townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] == -2 || townArrangement[firstHorizontalLocation, firstVerticalLocation] == -2 ||
							townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] == -2 || townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] == -2 ||
							townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] == -2)
						{
							bits |= 2;
						}

						if (bits == 1)
						{
							break;
						}
					}

					if (innerIndex < 500)
					{
						townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] = 15;
						townArrangement[firstHorizontalLocation, firstVerticalLocation] = index + 64;
						townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] = 15;
						townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] = 15;
						townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] = 15;
						townArrangement[firstHorizontalLocation + 1, firstVerticalLocation - 1] = 15;
						townArrangement[firstHorizontalLocation + 2, firstVerticalLocation] = 15;
						townArrangement[firstHorizontalLocation + 2, firstVerticalLocation - 1] = 15;
						townArrangement[firstHorizontalLocation + 2, firstVerticalLocation + 1] = 15;
						townArrangement[firstHorizontalLocation + 3, firstVerticalLocation - 1] = 15;

						if (firstVerticalLocation < 7 && (townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 2] & 7) == 0)
						{
							townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 2] = -1;
						}
					}
				}
			}

			for (int index = 23; index > 0; index--)
			{
				while (index > 0 && (((1 << index) & settlement.Structures) == 0 ||
					index == 7 || index == 8 || index == 12 || index == 18 || index == 19))
				{
					index--;
				}

				if (index < 1)
				{
					break;
				}

				int innerIndex = 0;

				for (; innerIndex < 500; innerIndex++)
				{
					firstHorizontalLocation = dice.DrawInteger(16) + 1;
					firstVerticalLocation = dice.DrawInteger(10) + 1;

					int bits = 0;

					if (townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] >= 0 || townArrangement[firstHorizontalLocation, firstVerticalLocation] >= 0 ||
						townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] >= 0 || townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] >= 0 ||
						townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] >= 0 ||
						innerIndex > 350)
					{
						bits |= 1;
					}

					if (townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] >= 15 || townArrangement[firstHorizontalLocation, firstVerticalLocation] >= 15 ||
						townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] >= 15 || townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] >= 15 ||
						townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] >= 15 ||
						townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] == -2 || townArrangement[firstHorizontalLocation, firstVerticalLocation] == -2 ||
						townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] == -2 || townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] == -2 ||
						townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] == -2)
					{
						bits |= 2;
					}

					if (bits == 1)
					{
						break;
					}
				}

				if (innerIndex < 500)
				{
					townArrangement[firstHorizontalLocation - 1, firstVerticalLocation + 1] = 15;
					townArrangement[firstHorizontalLocation, firstVerticalLocation] = index + 15;
					townArrangement[firstHorizontalLocation, firstVerticalLocation + 1] = 15;
					townArrangement[firstHorizontalLocation + 1, firstVerticalLocation] = 15;
					townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 1] = 15;

					if (firstVerticalLocation < 7)
					{
						if ((townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 2] & 7) == 0)
						{
							townArrangement[firstHorizontalLocation + 1, firstVerticalLocation + 2] = -1;
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
				townArrangement[0, featureRow - 1] = 23;
				townArrangement[0, featureRow] = 15;
				townArrangement[1, featureRow] = 15;
				townArrangement[1, featureRow + 1] = 15;
				townArrangement[2, featureRow] = 15;
				townArrangement[2, featureRow + 1] = 15;
			}

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.AskForDisk(1, 1);

			DrawCityBuildings((ushort)improvement, settlement.ParticipantIndex, townIndex);

			int improvementSeen = 0;

			do
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileHillPicture, 0);

				for (int index = 1; index < 8; index++)
				{
					int marvelIndex = index;

					if (index == 2)
					{
						marvelIndex = 15;
					}

					if (this.Owner.World.MarvelTownIndex[marvelIndex] == townIndex)
					{
						if ((marvelIndex + 24) != improvement)
						{
							if (((1 << marvelIndex) & 0x808a) != 0)
							{
								this.Owner.Services.DrawPictureToScreen(
									Screen.BackScreenBox,
									((marvelIndex == 3) ? 80 : 0) +
										(((short)SceneryBoxField(marvelIndex, SceneryBoxLeft) < 10) ? -1 : 2) +
										(short)SceneryBoxField(marvelIndex, SceneryBoxLeft),
									(marvelIndex <= 7) ? 0 : ((short)SceneryBoxField(marvelIndex, SceneryBoxTop) - 6),
									WonderPicture(marvelIndex));
							}
						}
					}
				}

				if (settlement.Has(CityStructure.PowerPlant) && improvement != 19)
				{
					for (int index = (townIndex == this.Owner.World.MarvelTownIndex[7]) ? 69 : 0; index < 310; index += 45)
					{
						this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox,
							index, 2, CityStructurePicture(17));
					}
				}

				for (firstVerticalLocation = 0; firstVerticalLocation < 12; firstVerticalLocation++)
				{
					for (firstHorizontalLocation = 0; firstHorizontalLocation < 19; firstHorizontalLocation++)
					{
						quantity = townArrangement[firstHorizontalLocation, firstVerticalLocation];

						if (quantity == -2)
						{
							int heading = 0;

							for (int index = 1; index < 9; index++)
							{
								int leadScratch;

								if ((firstVerticalLocation & 0x1) == 0)
								{
									leadScratch = ((short)Tables.DirectionVertical(index) <= 0) ? 0 : -1;
								}
								else
								{
									leadScratch = ((short)Tables.DirectionVertical(index) >= 0) ? 0 : 1;
								}

								int horizontalScratch = leadScratch + (short)Tables.DirectionHorizontal(index) + firstHorizontalLocation;
								int verticalScratch = (short)Tables.DirectionVertical(index) + firstVerticalLocation;

								if (verticalScratch < 12 && townArrangement[horizontalScratch, verticalScratch] >= 0)
								{
									heading = 1;
								}
							}

							if (heading == 0)
							{
								townArrangement[firstHorizontalLocation, firstVerticalLocation] = -1;
							}
						}
					}
				}

				for (firstVerticalLocation = 0; firstVerticalLocation < 12; firstVerticalLocation++)
				{
					for (firstHorizontalLocation = 0; firstHorizontalLocation < 19; firstHorizontalLocation++)
					{
						quantity = townArrangement[firstHorizontalLocation, firstVerticalLocation];

						if (quantity == -1 && firstHorizontalLocation > 2 && townArrangement[firstHorizontalLocation - 1, firstVerticalLocation] != -1 && ((firstHorizontalLocation + firstVerticalLocation) & 1) != 0)
						{
							DrawCityPicture((ushort)firstHorizontalLocation, (ushort)firstVerticalLocation, 0);
						}

						if (quantity != -1 && quantity != 15)
						{
							if (quantity != -2)
							{
								if (quantity >= 16)
								{
									if (improvement + 14 != quantity && improvement + 40 != quantity)
									{
										DrawCityResourceBar((ushort)firstHorizontalLocation, (ushort)firstVerticalLocation, (ushort)(quantity - 16));
									}
								}
								else if (quantity >= 2)
								{
									DrawCityPictureRegion((ushort)firstHorizontalLocation, (ushort)firstVerticalLocation, (ushort)(((firstHorizontalLocation + firstVerticalLocation) & 1) + ((quantity & 1) << 1)), (ushort)(quantity >> 1));
								}
								else
								{
									DrawCityPictureRegion((ushort)firstHorizontalLocation, (ushort)firstVerticalLocation, (ushort)(((quantity & 1) << 1) + (townIndex & 1)), (ushort)(quantity >> 1));
								}
							}
							else
							{
								int tag = 0;

								for (int index = 1; index < 9; index++)
								{
									int leadScratch;
									if ((firstVerticalLocation & 1) == 0)
									{
										leadScratch = ((short)Tables.DirectionVertical(index) <= 0) ? 0 : -1;
									}
									else
									{
										leadScratch = ((short)Tables.DirectionVertical(index) >= 0) ? 0 : 1;
									}

									int horizontalScratch = leadScratch + (short)Tables.DirectionHorizontal(index) + firstHorizontalLocation;
									int verticalScratch = (short)Tables.DirectionVertical(index) + firstVerticalLocation;

									if ((index & 1) != 0)
									{
										tag >>= 1;

										if (verticalScratch < 12 && townArrangement[horizontalScratch, verticalScratch] == -2)
										{
											tag |= 8;
										}
									}
								}

								DrawCityPicture((ushort)firstHorizontalLocation, (ushort)firstVerticalLocation, (ushort)tag);
							}
						}
					}
				}

				if (settlement.Has(CityStructure.CityWalls) && improvement != 8)
				{
					for (int index = 0; index < 320; index += 43)
					{
						if (index == 172)
						{
							index += 19;
						}

						this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox,
							index, 108, CityStructurePicture(22));
					}

					this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
						142, 108, CityStructurePicture(6));
				}

				if (settlement.Has(CityStructure.MassTransit) && improvement != 13)
				{
					for (int index = 0; index < 310; index += 46)
					{
						this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox,
							index, 115, CityStructurePicture(21));
					}

					this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
						0, 115, CityStructurePicture(11));
				}

				this.Processor.EmitWord(this.Processor.SegmentData.Word, Screen.Box, 0x1);
				DrawingBox.TypefaceIndex = 0x6;

				if (savedText.Length == 0)
				{
					EmptyMessage()
						.City(townIndex);

					this.Owner.TextPainter.DrawCenteredTextWithShadowToPrimaryScreen(this.Owner.Words.ToString(), 160, 2, Hues.White);

					this.Owner.Words.Clear();

					this.Owner.TurnHooks.FormatCurrentYear();

					this.Owner.TextPainter.DrawCenteredTextWithShadowToPrimaryScreen(this.Owner.Words.ToString(), 160, 15, Hues.White);
				}
				else
				{
					Screen.DialogNeedsRedraw = 1;

					this.Owner.Dialogs.ShowMenu(savedText, 80, 8, 1);
				}

				this.Processor.EmitWord(this.Processor.SegmentData.Word, Screen.Box, 0x0);
				DrawingBox.TypefaceIndex = 0x1;

				if (improvementSeen != 0)
				{
					this.Owner.Services.PerformSound(0x2c, 0);

					this.Owner.VideoCard.DrawScreenToMainScreen(1);
				}
				else
				{
					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

					if (improvement != -3)
					{
						this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileHillPictureSecond, 1);
					}

					this.Owner.VideoCard.RenderImage(
						Screen.BackScreenBox,
						0, 0, 320, 200,
						Screen.Box,
						0, 0);

					if (improvement == -3 && Screen.VideoGraphicsMode != 0)
					{
						this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileHillPalette, 0xbdee);

						this.Owner.Services.LoadColorsFromBuffer(15, 0xbdee);
					}

					if (Screen.VideoGraphicsMode != 0)
					{
						this.Owner.Services.AssignHueCycleSpan(4, 15, 64, 79);

						this.Owner.Services.ActivateHueCycle(4);
					}
				}

				if (improvement == -1 && improvementSeen == 0)
				{
					this.Owner.Helpers.ReleaseResource(CitySceneryHandle, 0x4eb3);

					DrawCityScenery(townIndex, 0x18, 0x8c, 0x100);
				}

				if (improvement <= -1 || this.Owner.Runtime.KeyAvailable() != 0)
				{
					break;
				}

				improvement = -1;
				improvementSeen = 1;

				this.Owner.TextPainter.WaitForTime(60);
			}
			while (this.Owner.Runtime.KeyAvailable() == 0);

			if (improvement != -1 || improvementSeen != 0)
			{
				this.Owner.Helpers.ReleaseResource(CitySceneryHandle, 0x4eb8);
			}

			if (improvement == -2)
			{
				if (Screen.VideoGraphicsMode != 0)
				{
					this.Owner.Services.DeactivateHueCycle(4);
				}
			}
			else
			{
				if (this.Owner.Runtime.KeyAvailable() != 0)
				{
					this.Owner.TurnLogic.WaitForKey();
				}
				else
				{
					this.Owner.Conquest.WaitForKeyPressOrMouseClick();
				}

				if (Screen.VideoGraphicsMode != 0)
				{
					this.Owner.Services.DeactivateHueCycle(4);
				}

				this.Owner.Startup.AskForDisk(0, 1);

				if (Screen.VideoGraphicsMode != 0)
				{
					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

					this.Owner.TurnHooks.ShowCivilizationScenery();
				}

				this.Owner.Helpers.LaunchHueCycling();
			}
		}

		public void DrawCityPicture(ushort column, ushort row, ushort pictureIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCityPicture({column}, {row}, {pictureIndex})");

			this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
				((row & 1) * 8) + (column * 16) - 5,
				(row * 8) + 50,
				CityWallPicture(pictureIndex));
		}

		public void DrawCityPictureRegion(ushort column, ushort row, ushort picture, ushort kind)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCityPictureRegion({column}, {row}, {picture}, {kind})");

			this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
				((row & 1) << 3) + (column << 4),
				(row << 3) + 26,
				CitySceneryPiecePicture(picture, kind));
		}

		public void DrawCityResourceBar(ushort column, ushort row, ushort picture)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCityResourceBar({column}, {row}, {picture})");

			if ((short)picture < 0x30)
			{
				int horizontalLocation = this.Owner.Helpers.ClampValue(((row & 1) * 8) + (column * 16) - 9, 0, 999);

				this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox,
					(short)horizontalLocation,
					(short)((short)(row << 3) + 16),
					CityStructurePicture(picture));
			}
			else
			{
				this.Processor.SegmentExtra.Word = 0x3767;
				picture -= 0x30;
				row = (ushort)((short)(row << 3) - this.Processor.PullSignedWord(this.Processor.SegmentExtra.Word, (ushort)((picture << 3) + 6)) + 76);

				int horizontalLocation = this.Owner.Helpers.ClampValue(((row & 1) * 8) + (column * 16) - 9, 0, 999);

				this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
					(short)horizontalLocation,
					(short)row - 10,
					WonderPicture(picture));
			}
		}

		public void DrawCityScenery(short townIndex, ushort rowHorizontal, ushort rowVertical, ushort breadth)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCityScenery({townIndex}, {rowHorizontal}, {rowVertical}, {breadth})");

			ushort[] pictures = new ushort[9];

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FilePopPicture, 0);

			for (int pictureIndex = 0; pictureIndex < 9; pictureIndex++)
			{
				ushort verticalLocation = this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, 0x23) == 0 ? (ushort)0x34 : (ushort)1;

				pictures[pictureIndex] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x23 * pictureIndex) + 1), verticalLocation, 0x22, 0x32);
			}

			sbyte size = this.Owner.World.Towns[townIndex].TrueExtent;
			short spacing = (short)(12 * size) > (short)breadth ? CentralProcessor.SignedQuotient((short)breadth, size) : (short)12;

			short happy = (short)this.Owner.Helpers.ClampValue((short)CityTurn.Happy, 0, this.Owner.World.Towns[townIndex].TrueExtent);
			short unhappy = (short)this.Owner.Helpers.ClampValue((short)CityTurn.Unhappy, 0, this.Owner.World.Towns[townIndex].TrueExtent);

			while ((short)(happy + unhappy) > (short)this.Owner.Helpers.ClampValue(this.Owner.World.Towns[townIndex].TrueExtent - (short)CityTurn.Specialists, 0, 99))
			{
				happy = (short)this.Owner.Helpers.ClampValue((short)(happy - 1), 0, this.Owner.World.Towns[townIndex].TrueExtent);
				unhappy = (short)this.Owner.Helpers.ClampValue((short)(unhappy - 1), 0, this.Owner.World.Towns[townIndex].TrueExtent);
			}

			short citizenIndex;

			for (citizenIndex = 0; citizenIndex < happy; citizenIndex++)
			{
				DrawSceneryCitizen(pictures, citizenIndex & 1, ref rowHorizontal, rowVertical, spacing);
			}

			if (happy != 0)
			{
				rowHorizontal += 8;
			}

			for (citizenIndex = 0; (short)(this.Owner.World.Towns[townIndex].TrueExtent - happy - unhappy - CityTurn.Specialists) > citizenIndex; citizenIndex++)
			{
				DrawSceneryCitizen(pictures, 2 + (citizenIndex & 1), ref rowHorizontal, rowVertical, spacing);
			}

			if (citizenIndex != 0)
			{
				rowHorizontal += 8;
			}

			for (citizenIndex = 0; citizenIndex < unhappy; citizenIndex++)
			{
				DrawSceneryCitizen(pictures, 4 + (citizenIndex & 1), ref rowHorizontal, rowVertical, spacing);
			}

			rowHorizontal += 12;

			for (citizenIndex = 0; citizenIndex < (short)CityTurn.Specialists; citizenIndex++)
			{
				DrawSceneryCitizen(pictures, 5 + this.Owner.CityOperations.ObtainCitizenType((ushort)citizenIndex), ref rowHorizontal, rowVertical, spacing);
			}

			this.Owner.Helpers.ReleaseResource(pictures[0], 0x4ec4);
		}

		public void DrawCityBuildings(ushort improvement, short participantIndex, short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCityBuildings({improvement}, {participantIndex}, {townIndex})");

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, CitySceneryPiecesPictureAddress, 0);

			for (int pieceIndex = 0; pieceIndex <= 4; pieceIndex++)
			{
				ushort horizontalLocation = (ushort)(pieceIndex << 6);
				ushort pictures = (ushort)((pieceIndex << 3) + CitySceneryPiecesTableAddress);

				this.Processor.EmitGlobalWord(pictures, this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 1), 1, 0x1f, 0x1f));
				this.Processor.EmitGlobalWord((ushort)(pictures + 4), this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 1), 0x21, 0x1f, 0x1f));
				this.Processor.EmitGlobalWord((ushort)(pictures + 2), this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 0x21), 1, 0x1f, 0x1f));
				this.Processor.EmitGlobalWord((ushort)(pictures + 6), this.Owner.VideoCard.ScreenToPicture(1, (ushort)(horizontalLocation + 0x21), 0x21, 0x1f, 0x1f));
			}

			for (int wallIndex = 0; wallIndex < 0x10; wallIndex++)
			{
				ushort top = (ushort)(((wallIndex >> 3) << 3) + (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x3a) == 0 ? 0x41 : 0x51));

				SetCityWallPicture(wallIndex,
					this.Owner.VideoCard.ScreenToPicture(1, (ushort)((wallIndex & 7) * 0x18), top, 0x18, 0x8));
			}

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, (ushort)(this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x25) != 0 ? CityBuildingsModernPictureAddress : CityBuildingsAncientPictureAddress), 0);

			City town = this.Owner.World.Towns[townIndex];

			for (int structureIndex = 0; structureIndex < 0x17; structureIndex++)
			{
				uint mask = 1u << (structureIndex + 1);
				bool built = ((mask & town.StructureMarkersLow) | ((mask >> 16) & town.StructureMarkersHigh)) != 0;

				if (built || (ushort)(improvement - 1) == structureIndex || structureIndex > 0x14)
				{
					SetCityStructurePicture(structureIndex,
						this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((structureIndex >> 2) * 0x32) + 1), (ushort)(((structureIndex & 3) * 0x32) + 1), 0x31, 0x31));
				}
			}

			CutWonderPictures(townIndex, false, CityWondersPictureAddress);
			CutWonderPictures(townIndex, true, CityWondersInMaskPictureAddress);
		}

		public ushort SeizeCityScenery(ushort picture)
		{
			using var _ = this.Processor.Journal.Scope($"SeizeCityScenery({picture})");

			this.Processor.SegmentExtra.Word = CitySceneryBoxes;

			if ((short)SceneryBoxField(1, SceneryBoxRight) > 0x12c)
			{
				for (int boxIndex = 1; boxIndex <= 0x15; boxIndex++)
				{
					SetSceneryBoxField(boxIndex, SceneryBoxWidth,
						(ushort)(SceneryBoxField(boxIndex, SceneryBoxRight) - SceneryBoxField(boxIndex, SceneryBoxLeft) + 1));
					SetSceneryBoxField(boxIndex, SceneryBoxHeight,
						(ushort)(SceneryBoxField(boxIndex, SceneryBoxBottom) - SceneryBoxField(boxIndex, SceneryBoxTop) + 1));
				}
			}

			ushort handle = this.Owner.VideoCard.ScreenToPicture(1,
				SceneryBoxField(picture, SceneryBoxLeft),
				SceneryBoxField(picture, SceneryBoxTop),
				SceneryBoxField(picture, SceneryBoxWidth),
				SceneryBoxField(picture, SceneryBoxHeight));

			return handle;
		}

		public void DrawCitySceneryForPlayer(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCitySceneryForPlayer({participantIndex})");

			ushort[] pictures = new ushort[10];

			this.Owner.Helpers.HaltHueCycling();

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.VideoCard.AssignHue(0xfd, VideoGraphicsBitmap.HueFromDigitalLevels(3, 3, 3));
			}

			this.Owner.Startup.HideMouseCursor();

			if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x22) != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x3a) != 0 ? ParadeAutomobilePictureAddress : ParadeTrainPictureAddress, 0);

				CutParadePictures(pictures, 0, 0x3d, 2, 0x3c);
			}
			else
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, ParadeWalkPictureAddress, 0);

				CutParadePictures(pictures, 1, 0x42, 1, 0x41);
			}

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x64, 0x140, 0x64, Screen.BackScreenBox, 0, 0);

			ushort frameIndex = 0;
			short offset;
			short figureHorizontal = 0;

			for (offset = 0; offset < 0x294; offset += 3)
			{
				this.Owner.Services.RestoreTimer();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0x64, Screen.BackScreenBox, 0, 0x64);

				for (short figureIndex = 7; figureIndex >= 0; figureIndex--)
				{
					figureHorizontal = (short)(offset - (0x30 * figureIndex));

					if (figureHorizontal >= 0x140 || figureHorizontal <= -0x30)
					{
						continue;
					}

					this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox, figureHorizontal, 132,
						pictures[(short)frameIndex % 10]);
				}

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x64, 0x140, 0x64, Screen.Box, 0, 0x64);

				frameIndex++;

				if (ParadeInterrupted())
				{
					break;
				}
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Helpers.ReleaseResource(pictures[0], 0);

			this.Owner.Helpers.LaunchHueCycling();
		}

		public void RiotAnimation()
		{
			using var _ = this.Processor.Journal.Scope("RiotAnimation()");

			ushort[] pictures = new ushort[10];

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.HideMouseCursor();

			if (this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, 13) != 0)
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileRiotPicture, 0);

				for (int index = 0; index < 10; index++)
				{
					pictures[index] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((index % 4) * 79) + 1), (ushort)(((index >> 2) * 64) + 1), 78, 63);
				}
			}
			else
			{
				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileRiotSecondPicture, 0);

				for (int index = 0; index < 10; index++)
				{
					pictures[index] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((index % 4) * 75) + 1), (ushort)(((index >> 2) * 66) + 1), 74, 65);
				}
			}

			this.Owner.VideoCard.RenderImage(
				Screen.Box,
				0, 100, 320, 100,
				Screen.BackScreenBox,
				0, 0);

			for (int index = -48, innerIndex = 0; index < 420; index += 3, innerIndex++)
			{
				this.Owner.Services.RestoreTimer();

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					0, 0, 320, 100,
					Screen.BackScreenBox,
					0, 100);

				for (int thirdIndex = 0; thirdIndex < 4; thirdIndex++)
				{
					int horizontalSpot = this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(ParadeFigureColumnsAddress + (thirdIndex << 1))) + index;

					if (horizontalSpot > -48 && horizontalSpot < 320)
					{
						this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox,
							horizontalSpot, this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)((thirdIndex << 1) + 0x4f4a)) + 132,
							pictures[((thirdIndex * 3) + innerIndex) % 10]);
					}
				}

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					0, 100, 320, 100,
					Screen.Box,
					0, 100);

				while (this.Owner.Timer.Beats < 10)
				{
				}

				this.Owner.Startup.ReadMouseState();

				if (this.Owner.Runtime.KeyAvailable() != 0 || this.Owner.MenuPointerState != 0)
				{
					break;
				}
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnLogic.WaitForKey();

			for (int index = 0; index < 10; index++)
			{
				this.Owner.Helpers.ReleaseResource(pictures[index], 0);
			}
		}

		public void DrawCitySceneryForHuman()
		{
			using var _ = this.Processor.Journal.Scope("RenderCitySceneryForHuman()");

			ushort[] pictures = new ushort[10];

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0,
				this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, 0x23) != 0 ? (ushort)0x4f5c : (ushort)0x4f52, 0);

			CutParadePictures(pictures, 1, 0x42, 1, 0x41);

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x64, 0x140, 0x64, Screen.BackScreenBox, 0, 0);

			ushort frameIndex = 0;
			short offset;
			short figureHorizontal = 0;

			for (offset = 0x1a4; offset > -0x30; offset -= 3)
			{
				this.Owner.Services.RestoreTimer();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0x64, Screen.BackScreenBox, 0, 0x64);

				for (short figureIndex = 0; figureIndex < 4; figureIndex++)
				{
					figureHorizontal = (short)(((short)(3 * this.Processor.PullGlobalWord((ushort)((figureIndex << 1) + ParadeFigureColumnsAddress))) / 2) + offset);

					if (figureHorizontal >= 0x140 || figureHorizontal <= -0x30)
					{
						continue;
					}

					this.Owner.Services.DrawPictureToScreenAlternate(Screen.BackScreenBox, figureHorizontal,
						this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)((figureIndex << 1) + 0x4f4a)) + 132,
						pictures[(short)((3 * figureIndex) + frameIndex) % 10]);
				}

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x64, 0x140, 0x64, Screen.Box, 0, 0x64);

				frameIndex++;

				if (ParadeInterrupted())
				{
					break;
				}
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Helpers.ReleaseResource(pictures[0], 0);
		}

		private static uint StableTextHash(string words)
		{
			uint hash = 2166136261;

			for (int index = 0; index < words.Length; index++)
			{
				hash ^= words[index];
				hash *= 16777619;
			}

			return hash;
		}

		private ushort CityStructurePicture(int structureIndex) => this.Processor.PullGlobalWord((ushort)((structureIndex << 1) + CityStructurePicturesTableAddress));

		private void SetCityStructurePicture(int structureIndex, ushort handle) => this.Processor.EmitGlobalWord((ushort)((structureIndex << 1) + CityStructurePicturesTableAddress), handle);

		private ushort CitySceneryPiecePicture(int horizontal, int vertical) => this.Processor.PullGlobalWord((ushort)((horizontal << 1) + (vertical << 3) + CitySceneryPiecesTableAddress));

		private ushort CityWallPicture(int pictureIndex) => this.Processor.PullGlobalWord((ushort)((pictureIndex << 1) + CityWallPicturesTableAddress));

		private void SetCityWallPicture(int wallIndex, ushort handle) => this.Processor.EmitGlobalWord((ushort)((wallIndex << 1) + CityWallPicturesTableAddress), handle);

		private ushort WonderPicture(int marvelIndex) => this.Processor.PullGlobalWord((ushort)((marvelIndex << 1) + WonderPicturesTableAddress));

		private void SetWonderPicture(int wonderIndex, ushort handle) => this.Processor.EmitGlobalWord((ushort)((wonderIndex << 1) + WonderPicturesTableAddress), handle);

		private void DrawSceneryCitizen(ushort[] pictures, int pictureIndex, ref ushort horizontalLocation, ushort verticalLocation, short spacing)
		{
			this.Owner.Services.DrawPictureToScreen(
				Screen.Box,
				(short)horizontalLocation,
				(short)verticalLocation,
				pictures[pictureIndex]);

			horizontalLocation += (ushort)spacing;
		}

		private void CutWonderPictures(short townIndex, bool inMask, ushort documentNameAddress)
		{
			bool loaded = false;

			for (int wonderIndex = 1; wonderIndex <= 0x15; wonderIndex++)
			{
				if (this.Owner.World.MarvelTownIndex[wonderIndex] != townIndex || (((1 << wonderIndex) & 0x808a) != 0) != inMask)
				{
					continue;
				}

				if (!loaded)
				{
					this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, documentNameAddress, 0);
				}

				loaded = true;

				ushort picture = SeizeCityScenery((ushort)wonderIndex);

				SetWonderPicture(wonderIndex, picture);
			}
		}

		private ushort SceneryBoxField(int boxIndex, int fieldOffset) =>
			this.Processor.PullWord(CitySceneryBoxes, (ushort)((boxIndex * SceneryBoxStride) + fieldOffset));

		private void SetSceneryBoxField(int boxIndex, int fieldOffset, ushort value) =>
			this.Processor.EmitWord(CitySceneryBoxes, (ushort)((boxIndex * SceneryBoxStride) + fieldOffset), value);

		private void CutParadePictures(ushort[] pictures, ushort left, ushort rowHeight, ushort top, ushort altitude)
		{
			for (int pictureIndex = 0; pictureIndex < 0xa; pictureIndex++)
			{
				pictures[pictureIndex] = this.Owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIndex % 4) * 0x4f) + left), (ushort)(((pictureIndex >> 2) * rowHeight) + top), 0x4e, altitude);
			}
		}

		private bool ParadeInterrupted()
		{
			while ((short)this.Owner.Timer.Beats < 0xa)
			{
			}

			this.Owner.Startup.ReadMouseState();

			return (ushort)this.Owner.Runtime.KeyAvailable() != 0 || this.Owner.MenuPointerState != 0;
		}
	}
}
