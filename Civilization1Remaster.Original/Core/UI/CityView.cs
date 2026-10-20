using Civilization1Original.Emulation;
using System;
using Civilization1Original.Game.State;

namespace Civilization1Original.UI
{
	public class CityView : GameModule
	{
		public CityView(GameEngine owner)
			: base(owner)
		{
		}
		
		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="param2"></param>
		public void DisplayCityScreen(short townIdx, short arg2)
		{
			this.processor.Journal.OpenScope($"DisplayCityScreen({townIdx}, {arg2})");

			// function body
			this.processor.EnterFrame(0x186);

			City settlement = this.owner.World.Towns[townIdx];

			int scienceTally = this.owner.World.Participants[settlement.ParticipantIdx].FoundScienceTally >> 1;
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

			this.processor.EmitLocalByte(0x80, 0x0);

			if (this.owner.Libc.TextLength(0xba06) < 120)
			{
				this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x80), 0xba06);
			}

			// RNG based on city name
			EmptyMessage()
				.City(townIdx);
			string settlementTitle = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, 0xba06));
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
					int mergedPlace = this.owner.Helpers.MergeCoordinates((short)(horizLoc1 - 9), (short)(vertLoc1 - 10));

					townArrangement[horizLoc1, vertLoc1] = Math.Min(Math.Max(((scienceTally - mergedPlace) / 3) - dice.DrawInt(2), scienceTally / 6),
						Math.Min((settlement.TrueExtent / 4) + 6, 9));

					if ((mergedPlace * mergedPlace) > ((int)settlement.TrueExtent << 3))
					{
						horizLoc1 = 9;
						vertLoc1 = 11;
					}
				}

				vertLoc2 = dice.DrawInt(8) + 1;

				horizLoc1 = Math.Min(Math.Max(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((vertLoc2 << 1) + 0x1882)) + horizLoc1, 0), 18);
				vertLoc1 = Math.Min(Math.Max(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((vertLoc2 << 1) + 0x18e4)) + (ushort)vertLoc1, 0), 11);
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

			if (townIdx == this.owner.World.MarvelTownIdx[7])
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
				if (this.owner.World.MarvelTownIdx[idx] == townIdx && ((1 << idx) & 0x808a) == 0)
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

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(1, 1);

			RenderCityBuildings((ushort)arg2, settlement.ParticipantIdx, townIdx);

			int idx3 = 0;

			do
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4e98, 0);

				for (int idx = 1; idx < 8; idx++)
				{
					int marvelIdx = idx;

					if (idx == 2)
					{
						marvelIdx = 15;
					}

					if (this.owner.World.MarvelTownIdx[marvelIdx] == townIdx)
					{
						if ((marvelIdx + 24) != arg2)
						{
							if (((1 << marvelIdx) & 0x808a) != 0)
							{
								this.owner.Services.RenderPictureToScreen(
									this.processor.PullGlobalWord(0x19d4),
									(((marvelIdx == 3) ? 80 : 0) +
										((this.processor.PullInt16(0x3767, (ushort)((marvelIdx << 3) + 0x0)) < 10) ? -1 : 2) +
										this.processor.PullInt16(0x3767, (ushort)((marvelIdx << 3) + 0x0))),
									((marvelIdx <= 7) ? 0 : (this.processor.PullInt16(0x3767, (ushort)((marvelIdx << 3) + 0x2)) - 6)),
									this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((marvelIdx << 1) + 0x68a0)));
							}
						}
					}
				}

				if (settlement.Has(CityStructure.PowerPlant) && arg2 != 19)
				{
					for (int idx = (townIdx == this.owner.World.MarvelTownIdx[7]) ? 69 : 0; idx < 310; idx += 45)
					{
						this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
							idx, 2, this.processor.PullGlobalWord(0x81c4));
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
									leadScratch = ((this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x18e4)) <= 0) ? 0 : -1);
								}
								else
								{
									leadScratch = (this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x18e4)) >= 0) ? 0 : 1;
								}

								int horizScratch = leadScratch + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x1882)) + horizLoc1;
								int vertScratch = this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x18e4)) + vertLoc1;

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
									if (arg2 + 14 != quantity && arg2 + 40 != quantity)
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
										leadScratch = (this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x18e4)) <= 0) ? 0 : -1;
									}
									else
									{
										leadScratch = (this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x18e4)) >= 0) ? 0 : 1;
									}

									int horizScratch = leadScratch + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x1882)) + horizLoc1;
									int vertScratch = this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((idx << 1) + 0x18e4)) + vertLoc1;

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
				
				if (settlement.Has(CityStructure.CityWalls) && arg2 != 8)
				{
					for (int idx = 0; idx < 320; idx += 43)
					{
						if (idx == 172)
						{
							idx += 19;
						}

						this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
							idx, 108, this.processor.PullGlobalWord(0x81ce));
					}

					this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4),
						142, 108, this.processor.PullGlobalWord(0x81ae));
				}

				if (settlement.Has(CityStructure.MassTransit) && arg2 != 13)
				{
					for (int idx = 0; idx < 310; idx += 46)
					{
						this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
							idx, 115, this.processor.PullGlobalWord(0x81cc));
					}

					this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4),
						0, 115, this.processor.PullGlobalWord(0x81b8));
				}

				this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
				this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x6);

				if (this.owner.Libc.TextLength((ushort)(this.processor.RegFrame.Value16 - 0x80)) == 0)
				{
					EmptyMessage()
						.City(townIdx);

					this.owner.TextPainter.RenderCenteredTextWithShadowToScreen0(0xba06, 160, 2, 15);

					this.processor.EmitGlobalByte(0xba06, 0x0);

					this.owner.TurnHooks.ObtainCurrentYearAsText();

					this.owner.TextPainter.RenderCenteredTextWithShadowToScreen0(0xba06, 160, 15, 15);
				}
				else
				{
					this.processor.EmitGlobalWord(0xdb38, 0x1);

					this.owner.Dialogs.DisplayMenu((ushort)(this.processor.RegFrame.Value16 - 0x80), 80, 8, 1);
				}

				this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
				this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

				if (idx3 != 0)
				{
					this.owner.Services.PerformSound(0x2c, 0);

					this.owner.VideoCard.RenderScreenToMainScreen(1, 10);
				}
				else
				{
					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

					if (arg2 != -3)
					{
						this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4ea1, 1);
					}

					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19d4),
						0, 0, 320, 200,
						this.processor.PullGlobalWord(0xaa),
						0, 0);

					if (arg2 == -3 && this.processor.PullGlobalWord(0xd762) != 0)
					{
						this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4eaa, 0xbdee);

						this.owner.Services.ImportColorsFromBuffer(15, 0xbdee);
					}

					if (this.processor.PullGlobalWord(0xd762) != 0)
					{
						this.owner.Services.AssignHueCycleSpan(4, 15, 64, 79);

						this.owner.Services.ActivateHueCycle(4);
					}
				}

				if (arg2 == -1 && idx3 == 0)
				{
					this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x817a), 0x4eb3);

					RenderCityScenery(townIdx, 0x18, 0x8c, 0x100);
				}

				if (arg2 <= -1 || this.owner.Libc.KeyAvailable() != 0)
					break;

				arg2 = -1;
				idx3 = 1;

				this.owner.TextPainter.AwaitTime(60);
			}
			while (this.owner.Libc.KeyAvailable() == 0);

			if (arg2 != -1 || idx3 != 0)
			{
				this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x817a), 0x4eb8);
			}

			if (arg2 == -2)
			{
				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.Services.DeactivateHueCycle(4);
				}
			}
			else
			{
				if (this.owner.Libc.KeyAvailable() != 0)
				{
					this.owner.TurnLogic.AwaitForKey();
				}
				else
				{
					this.owner.Conquest.AwaitForKeyPressOrMouseClick();
				}

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.Services.DeactivateHueCycle(4);
				}

				this.owner.Startup.QueryForDisk(0, 1);

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

					this.owner.TurnHooks.DisplayCivilizationScenery();
				}

				this.owner.Helpers.LaunchHueCycling();
			}

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCityScreen");
		}

		/// <summary>
		/// One picture of the city view (table at 0x6880) into the back screen (0x19d4), in the column and row of the view
		/// </summary>
		public void RenderCityPicture(ushort arg1, ushort arg2, ushort pictureIdx)
		{
			this.processor.Journal.OpenScope($"RenderCityPicture({arg1}, {arg2}, {pictureIdx})");

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4),
				((arg2 & 1) * 8) + (arg1 * 16) - 5,
				(arg2 * 8) + 50,
				this.processor.PullGlobalWord((ushort)((pictureIdx << 1) + 0x6880)));

			this.processor.Journal.CloseScope("RenderCityPicture");
		}
		
		/// <summary>
		/// One picture of the city view (table at 0x817a by picture and kind) into the back screen (0x19d4), in the column and
		/// row of the view
		/// </summary>
		public void RenderCityPictureRegion(ushort arg1, ushort arg2, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"RenderCityPictureRegion({arg1}, {arg2}, {arg3}, {arg4})");

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4),
				((arg2 & 1) << 3) + (arg1 << 4),
				(arg2 << 3) + 26,
				this.processor.PullGlobalWord((ushort)((arg3 << 1) + (arg4 << 3) + 0x817a)));

			this.processor.Journal.CloseScope("RenderCityPictureRegion");
		}

		/// <summary>
		/// One picture of a row of the city screen into the back screen (0x19d4): pictures below 0x30 from the table at 0x81a2,
		/// the others from the table at 0x68a0, moved up by their height (segment 0x3767)
		/// </summary>
		public void RenderCityResourceBar(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"RenderCityResourceBar({arg1}, {arg2}, {arg3})");

			if ((short)arg3 < 0x30)
			{
				int horizLoc = this.owner.Helpers.VerifyValueSpan(((arg2 & 1) * 8) + (arg1 * 16) - 9, 0, 999);

				this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
					(short)horizLoc,
					(short)((short)(arg2 << 3) + 16),
					this.processor.PullGlobalWord((ushort)((arg3 << 1) + 0x81a2)));
			}
			else
			{
				this.processor.SegExtra.Value16 = 0x3767;
				arg3 -= 0x30;
				arg2 = (ushort)((short)(arg2 << 3) - this.processor.PullInt16(this.processor.SegExtra.Value16, (ushort)((arg3 << 3) + 6)) + 76);

				int horizLoc = this.owner.Helpers.VerifyValueSpan(((arg2 & 1) * 8) + (arg1 * 16) - 9, 0, 999);

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0x19d4),
					(short)horizLoc,
					(short)arg2 - 10,
					this.processor.PullGlobalWord((ushort)((arg3 << 1) + 0x68a0)));
			}

			this.processor.Journal.CloseScope("RenderCityResourceBar");
		}

		/// <summary>
		/// The citizens of the city in a row from (arg2, arg3), at most arg4 wide: the 9 pictures 0x23 apart of 0x4ebc (row 0x34
		/// without technology 0x23, else row 1) are cut into the stack frame; then the happy (0x70e2), content, unhappy (0x70e4)
		/// people (clamped, so that with the specialists 0xe8b8 they fit the size) with two alternating pictures each and a gap of
		/// 8 after a non-empty group (12 after the unhappy), then the specialists (picture 5 + their type)
		/// </summary>
		public void RenderCityScenery(short townIdx, ushort arg2, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"RenderCityScenery({townIdx}, {arg2}, {arg3}, {arg4})");

			// The pictures are in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveSource();

			ushort pictures = (ushort)(this.processor.RegFrame.Value16 - 0x16);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4ebc, 0);

			for (int pictureIdx = 0; pictureIdx < 9; pictureIdx++)
			{
				ushort vertLoc = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x23) == 0 ? (ushort)0x34 : (ushort)1;

				this.owner.VideoCard.ScreenToPicture(1, (ushort)((0x23 * pictureIdx) + 1), vertLoc, 0x22, 0x32);

				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(pictures + (pictureIdx << 1)), this.processor.RegAcc.Value16);
			}

			sbyte size = this.owner.World.Towns[townIdx].TrueExtent;
			short spacing = (short)(12 * size) > (short)arg4 ? CPU.SignedQuotient((short)arg4, size) : (short)12;

			short happy = (short)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e2), 0, this.owner.World.Towns[townIdx].TrueExtent);
			short unhappy = (short)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e4), 0, this.owner.World.Towns[townIdx].TrueExtent);

			while ((short)(happy + unhappy) > (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Towns[townIdx].TrueExtent - this.processor.PullGlobalSignedWord(0xe8b8), 0, 99))
			{
				happy = (short)this.owner.Helpers.VerifyValueSpan((short)(happy - 1), 0, this.owner.World.Towns[townIdx].TrueExtent);
				unhappy = (short)this.owner.Helpers.VerifyValueSpan((short)(unhappy - 1), 0, this.owner.World.Towns[townIdx].TrueExtent);
			}

			short citizenIdx;

			for (citizenIdx = 0; citizenIdx < happy; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, citizenIdx & 1, ref arg2, arg3, spacing);
			}

			if (happy != 0)
				arg2 += 8;

			for (citizenIdx = 0; (short)(this.owner.World.Towns[townIdx].TrueExtent - happy - unhappy - this.processor.PullGlobalWord(0xe8b8)) > citizenIdx; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, 2 + (citizenIdx & 1), ref arg2, arg3, spacing);
			}

			if (citizenIdx != 0)
				arg2 += 8;

			for (citizenIdx = 0; citizenIdx < unhappy; citizenIdx++)
			{
				RenderSceneryCitizen(pictures, 4 + (citizenIdx & 1), ref arg2, arg3, spacing);
			}

			arg2 += 12;

			for (citizenIdx = 0; citizenIdx < (short)this.processor.PullGlobalWord(0xe8b8); citizenIdx++)
			{
				RenderSceneryCitizen(pictures, 5 + this.owner.CityOps.ObtainCitizenType((ushort)citizenIdx), ref arg2, arg3, spacing);
			}

			this.processor.EmitLocalWord(0x2, (ushort)happy);
			this.processor.EmitLocalWord(0x4, (ushort)unhappy);
			this.processor.EmitLocalWord(0x18, (ushort)citizenIdx);
			this.processor.EmitLocalWord(0x1a, (ushort)spacing);
			this.processor.EmitLocalByte(0x1c, (byte)size);

			this.owner.Helpers.ReleaseResource(this.processor.PullUInt16(this.processor.SegStack.Value16, pictures), 0x4ec4);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCityScenery");
		}

		/// <summary>One citizen of the city scenery: picture pictureIdx of the frame at (horizLoc, vertLoc), then the next place</summary>
		private void RenderSceneryCitizen(ushort pictures, int pictureIdx, ref ushort horizLoc, ushort vertLoc, short spacing)
		{
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)horizLoc,
				(short)vertLoc,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(pictures + (pictureIdx << 1))));

			horizLoc += (ushort)spacing;
		}

		/// <summary>
		/// Cuts the pictures of the city view out of screen 1: from 0x4ec8 the 5 x 4 land pieces (0x817a) and the 16 wall pieces
		/// (0x6880, the second set with technology 0x3a), from 0x4ed5 (technology 0x25) or 0x4ee2 the buildings 0 - 22 the city has
		/// (StructureMarkers bit index + 1), arg1 - 1 and 21 - 22 (0x81a2), and the city's wonders (0x68a0): from 0x4eef those
		/// outside mask 0x808a, from 0x4efb those in it
		/// </summary>
		public void RenderCityBuildings(ushort arg1, short participantIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"RenderCityBuildings({arg1}, {participantIdx}, {townIdx})");

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4ec8, 0);

			for (int pieceIdx = 0; pieceIdx <= 4; pieceIdx++)
			{
				ushort horizLoc = (ushort)(pieceIdx << 6);
				ushort pictures = (ushort)((pieceIdx << 3) + 0x817a);

				this.processor.EmitGlobalWord(pictures, this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 1), 1, 0x1f, 0x1f));
				this.processor.EmitGlobalWord((ushort)(pictures + 4), this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 1), 0x21, 0x1f, 0x1f));
				this.processor.EmitGlobalWord((ushort)(pictures + 2), this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x21), 1, 0x1f, 0x1f));
				this.processor.EmitGlobalWord((ushort)(pictures + 6), this.owner.VideoCard.ScreenToPicture(1, (ushort)(horizLoc + 0x21), 0x21, 0x1f, 0x1f));
			}

			for (int wallIdx = 0; wallIdx < 0x10; wallIdx++)
			{
				ushort top = (ushort)(((wallIdx >> 3) << 3) + (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x3a) == 0 ? 0x41 : 0x51));

				this.processor.EmitGlobalWord((ushort)((wallIdx << 1) + 0x6880),
					this.owner.VideoCard.ScreenToPicture(1, (ushort)((wallIdx & 7) * 0x18), top, 0x18, 0x8));
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, (ushort)(this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x25) != 0 ? 0x4ed5 : 0x4ee2), 0);

			City town = this.owner.World.Towns[townIdx];

			for (int structureIdx = 0; structureIdx < 0x17; structureIdx++)
			{
				uint mask = 1u << (structureIdx + 1);
				bool built = ((mask & town.StructureMarkers0) | ((mask >> 16) & town.StructureMarkers1)) != 0;

				if (built || (ushort)(arg1 - 1) == structureIdx || structureIdx > 0x14)
				{
					this.processor.EmitGlobalWord((ushort)((structureIdx << 1) + 0x81a2),
						this.owner.VideoCard.ScreenToPicture(1, (ushort)(((structureIdx >> 2) * 0x32) + 1), (ushort)(((structureIdx & 3) * 0x32) + 1), 0x31, 0x31));
				}
			}

			CutWonderPictures(townIdx, false, 0x4eef);
			CutWonderPictures(townIdx, true, 0x4efb);

			this.processor.Journal.CloseScope("RenderCityBuildings");
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
				if (this.owner.World.MarvelTownIdx[wonderIdx] != townIdx || (((1 << wonderIdx) & 0x808a) != 0) != inMask)
					continue;

				if (!loaded)
				{
					this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, docnameAddr, 0);
				}

				loaded = true;

				SeizeCityScenery((ushort)wonderIdx);

				this.processor.EmitGlobalWord((ushort)((wonderIdx << 1) + 0x68a0), this.processor.RegAcc.Value16);
			}
		}

		/// <summary>
		/// Cuts a picture of the city view out of screen 1. The rectangles (segment 0x3767, records of 4 words: left, top, right,
		/// bottom) are turned into left, top, width, height once, the first time (when the right of rectangle 1 is above 300).
		/// </summary>
		public void SeizeCityScenery(ushort arg1)
		{
			this.processor.Journal.OpenScope($"SeizeCityScenery({arg1})");

			this.processor.SegExtra.Value16 = CitySceneryBoxes;

			if ((short)this.processor.PullUInt16(CitySceneryBoxes, 0xc) > 0x12c)
			{
				for (int boxIdx = 1; boxIdx <= 0x15; boxIdx++)
				{
					ushort box = (ushort)(boxIdx << 3);

					this.processor.EmitUInt16(CitySceneryBoxes, (ushort)(box + 4),
						(ushort)(this.processor.PullUInt16(CitySceneryBoxes, (ushort)(box + 4)) - this.processor.PullUInt16(CitySceneryBoxes, box) + 1));
					this.processor.EmitUInt16(CitySceneryBoxes, (ushort)(box + 6),
						(ushort)(this.processor.PullUInt16(CitySceneryBoxes, (ushort)(box + 6)) - this.processor.PullUInt16(CitySceneryBoxes, (ushort)(box + 2)) + 1));
				}
			}

			ushort picked = (ushort)(arg1 << 3);

			this.owner.VideoCard.ScreenToPicture(1,
				this.processor.PullUInt16(CitySceneryBoxes, picked),
				this.processor.PullUInt16(CitySceneryBoxes, (ushort)(picked + 2)),
				this.processor.PullUInt16(CitySceneryBoxes, (ushort)(picked + 4)),
				this.processor.PullUInt16(CitySceneryBoxes, (ushort)(picked + 6)));

			this.processor.Journal.CloseScope("SeizeCityScenery");
		}

		/// <summary>The segment of the rectangles of the city view pictures</summary>
		private const ushort CitySceneryBoxes = 0x3767;

		/// <summary>
		/// The parade of the player's people over the bottom of the screen until a key or a click (frame by frame, 10 timer
		/// ticks each): with technology 0x22 the 10 pictures of 0x4f08 (with 0x3a) or 0x4f15, else 0x4f22; 8 figures 0x30
		/// apart moving right, all with the same picture. The pictures are cut into the stack frame, like in the original.
		/// </summary>
		public void RenderCitySceneryForPlayer(short participantIdx)
		{
			this.processor.Journal.OpenScope($"RenderCitySceneryForPlayer({participantIdx})");

			// The pictures are in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x28);
			this.processor.PreserveSource();

			ushort pictures = (ushort)(this.processor.RegFrame.Value16 - 0x20);

			this.owner.Helpers.HaltHueCycling();

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(3, 3, 3));
			}

			this.owner.Startup.ConcealMouseCursor();

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22) != 0)
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x3a) != 0 ? (ushort)0x4f08 : (ushort)0x4f15, 0);

				CutParadePictures(pictures, 0, 0x3d, 2, 0x3c);
			}
			else
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f22, 0);

				CutParadePictures(pictures, 1, 0x42, 1, 0x41);
			}

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0x64, 0x140, 0x64, this.processor.PullGlobalWord(0x19d4), 0, 0);

			ushort frameIdx = 0;
			short offset;
			short figureHoriz = 0;

			for (offset = 0; offset < 0x294; offset += 3)
			{
				this.owner.Services.RestoreTimer();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0x64, this.processor.PullGlobalWord(0x19d4), 0, 0x64);

				for (short figureIdx = 7; figureIdx >= 0; figureIdx--)
				{
					figureHoriz = (short)(offset - (0x30 * figureIdx));

					if (figureHoriz >= 0x140 || figureHoriz <= -0x30)
						continue;

					this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4), figureHoriz, 132,
						this.processor.PullGlobalWord((ushort)(pictures + (((short)frameIdx % 10) << 1))));
				}

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0x64, 0x140, 0x64, this.processor.PullGlobalWord(0xaa), 0, 0x64);

				frameIdx++;

				if (ParadeInterrupted())
					break;
			}

			this.processor.EmitLocalWord(0x22, (ushort)offset);
			this.processor.EmitLocalWord(0x24, 0xffff);
			this.processor.EmitLocalWord(0x26, (ushort)figureHoriz);
			this.processor.EmitLocalWord(0x28, frameIdx);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(pictures), 0);

			this.owner.Helpers.LaunchHueCycling();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitySceneryForPlayer");
		}

		/// <summary>
		/// The 10 pictures of the parade cut from screen 1 (4 a row, 0x4f apart) into the frame
		/// </summary>
		private void CutParadePictures(ushort pictures, ushort left, ushort rowHeight, ushort top, ushort altitude)
		{
			for (int pictureIdx = 0; pictureIdx < 0xa; pictureIdx++)
			{
				this.owner.VideoCard.ScreenToPicture(1, (ushort)(((pictureIdx % 4) * 0x4f) + left), (ushort)(((pictureIdx >> 2) * rowHeight) + top), 0x4e, altitude);

				this.processor.EmitGlobalWord((ushort)(pictures + (pictureIdx << 1)), this.processor.RegAcc.Value16);
			}
		}

		/// <summary>Waits for 10 timer ticks (since RestoreTimer), then a key or a click ends the parade</summary>
		private bool ParadeInterrupted()
		{
			while ((short)this.processor.PullGlobalWord(0x5c) < 0xa)
			{
			}

			this.owner.Startup.ObtainMouseState();

			this.owner.Libc.KeyAvailable();

			return this.processor.RegAcc.Value16 != 0 || this.owner.GlobDb3a != 0;
		}
		
		/// <summary>
		/// ?
		/// </summary>
		public void RiotAnimation()
		{
			this.processor.Journal.OpenScope("RiotAnimation()");

			// function body
			ushort[] pictures = new ushort[10];

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 13) != 0)
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f2f, 0);

				for (int idx = 0; idx < 10; idx++)
				{
					pictures[idx] = this.owner.VideoCard.ScreenToPicture(1, (ushort)(((idx % 4) * 79) + 1), (ushort)(((idx >> 2) * 64) + 1), 78, 63);
				}
			}
			else
			{
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f38, 0);

				for (int idx = 0; idx < 10; idx++)
				{
					pictures[idx] = this.owner.VideoCard.ScreenToPicture(1, (ushort)(((idx % 4) * 75) + 1), (ushort)(((idx >> 2) * 66) + 1), 74, 65);
				}
			}

			// Draw City View to screen 0
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 100, 320, 100,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			for (int idx = -48, jdx = 0; idx < 420; idx += 3, jdx++)
			{
				this.owner.Services.RestoreTimer();

				// Draw a lower portion of City View to screen 0
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, 320, 100,
					this.processor.PullGlobalWord(0x19d4),
					0, 100);

				for (int kdx = 0; kdx < 4; kdx++)
				{
					int horizSpot = this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(0x4f42 + (kdx << 1))) + idx;

					if (horizSpot > -48 && horizSpot < 320)
					{
						this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
							horizSpot, this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((kdx << 1) + 0x4f4a)) + 132,
							pictures[(kdx * 3 + jdx) % 10]);
					}				
				}

				// Draw a lower portion of City View to screen 0
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 100, 320, 100,
					this.processor.PullGlobalWord(0xaa),
					0, 100);

				while (this.processor.PullGlobalWord(0x5c) < 10) ;

				this.owner.Startup.ObtainMouseState();

				if (this.owner.Libc.KeyAvailable() != 0 || this.owner.GlobDb3a != 0)
					break;
			}

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			for (int idx = 0; idx < 10; idx++)
			{
				this.owner.Helpers.ReleaseResource(pictures[idx], 0);
			}

			// Far return
			this.processor.Journal.CloseScope("RiotAnimation");
		}

		/// <summary>
		/// The parade of the human player's people (the 10 pictures of 0x4f5c with technology 0x23, else 0x4f52): 4 figures
		/// (columns 0x4f42, rows 0x4f4a) moving left, each with its own picture, until a key or a click.
		/// </summary>
		public void RenderCitySceneryForHuman()
		{
			this.processor.Journal.OpenScope("RenderCitySceneryForHuman()");

			// The pictures are in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x28);
			this.processor.PreserveSource();

			ushort pictures = (ushort)(this.processor.RegFrame.Value16 - 0x20);

			this.owner.Helpers.HaltHueCycling();

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0,
				this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x23) != 0 ? (ushort)0x4f5c : (ushort)0x4f52, 0);

			CutParadePictures(pictures, 1, 0x42, 1, 0x41);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0x64, 0x140, 0x64, this.processor.PullGlobalWord(0x19d4), 0, 0);

			ushort frameIdx = 0;
			short offset;
			short figureHoriz = 0;

			for (offset = 0x1a4; offset > -0x30; offset -= 3)
			{
				this.owner.Services.RestoreTimer();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0x64, this.processor.PullGlobalWord(0x19d4), 0, 0x64);

				for (short figureIdx = 0; figureIdx < 4; figureIdx++)
				{
					figureHoriz = (short)(((short)(3 * this.processor.PullGlobalWord((ushort)((figureIdx << 1) + 0x4f42))) / 2) + offset);

					if (figureHoriz >= 0x140 || figureHoriz <= -0x30)
						continue;

					this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4), figureHoriz,
						this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((figureIdx << 1) + 0x4f4a)) + 132,
						this.processor.PullGlobalWord((ushort)(pictures + (((short)((3 * figureIdx) + frameIdx) % 10) << 1))));
				}

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0x64, 0x140, 0x64, this.processor.PullGlobalWord(0xaa), 0, 0x64);

				frameIdx++;

				if (ParadeInterrupted())
					break;
			}

			this.processor.EmitLocalWord(0x22, (ushort)offset);
			this.processor.EmitLocalWord(0x24, 0x4);
			this.processor.EmitLocalWord(0x26, (ushort)figureHoriz);
			this.processor.EmitLocalWord(0x28, frameIdx);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(pictures), 0);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitySceneryForHuman");
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
	}
}
