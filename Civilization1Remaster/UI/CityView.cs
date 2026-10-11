using Civilization1Remaster.Emulation;
using System;

namespace Civilization1Remaster.UI
{
	public class CityView
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public CityView(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
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
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.owner.Conquest.ObtainCityLabel(townIdx);
			string settlementTitle = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, 0xba06));
			RandomMT19937 dice = new RandomMT19937((uint)settlementTitle.GetHashCode());

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
				while (idx > 0 && (((1 << idx) & this.processor.JoinDoubleWord(settlement.StructureMarkers0, settlement.StructureMarkers1)) == 0 ||
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

			if ((settlement.StructureMarkers1 & 8) != 0)
			{
				townArrangement[17, 7] = 34;
				townArrangement[17, 8] = 15;
				townArrangement[18, 7] = 15;
				townArrangement[18, 8] = 15;
			}

			if ((settlement.StructureMarkers0 & 0x100) != 0)
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

				if ((settlement.StructureMarkers1 & 0x4) != 0 && arg2 != 19)
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
				
				if ((settlement.StructureMarkers0 & 0x80) != 0 && arg2 != 8)
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

				if ((settlement.StructureMarkers0 & 0x1000) != 0 && arg2 != 13)
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
					this.processor.EmitGlobalByte(0xba06, 0x0);

					this.owner.Conquest.ObtainCityLabel(townIdx);

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
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="bitmapID"></param>
		public void RenderCityPicture(ushort arg1, ushort arg2, ushort pictureIdx)
		{
			this.processor.Journal.OpenScope($"RenderCityPicture({arg1}, {arg2}, {pictureIdx})");

			// function body
			this.processor.EnterFrame();

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				((arg2 & 1) * 8) + (arg1 * 16) - 5,
				(arg2 * 8) + 50,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((pictureIdx << 1) + 0x6880)));

			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RenderCityPicture");
		}
		
		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		/// <param name="param4"></param>
		public void RenderCityPictureRegion(ushort arg1, ushort arg2, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"RenderCityPictureRegion({arg1}, {arg2}, {arg3}, {arg4})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				((arg2 & 1) << 3) + (arg1 << 4),
				(arg2 << 3) + 26,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((arg3 << 1) + (arg4 << 3) + 0x817a)));

			this.processor.RestoreSource();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RenderCityPictureRegion");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		public void RenderCityResourceBar(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"RenderCityResourceBar({arg1}, {arg2}, {arg3})");

			// function body
			this.processor.EnterFrame();

			this.processor.CompareWord(arg3, 0x30);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L10b9;

			this.owner.Helpers.VerifyValueSpan(((arg2 & 1) * 8) + (arg1 * 16) - 9, 0, 999);

			this.owner.Services.RenderPictureToScreenAlternate(
				this.processor.PullGlobalWord(0x19d4),
				(short)this.processor.RegAcc.Value16,
				(short)((short)(arg2 << 3) + 16),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((arg3 << 1) + 0x81a2)));

			goto L111a;

		L10b9:
			this.processor.SegExtra.Value16 = 0x3767; // segment
			arg3 -= 0x30;
			arg2 = (ushort)((short)(arg2 << 3) - this.processor.PullInt16(this.processor.SegExtra.Value16, (ushort)((arg3 << 3) + 6)) + 76);

			this.owner.Helpers.VerifyValueSpan(((arg2 & 1) * 8) + (arg1 * 16) - 9, 0, 999);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				(short)this.processor.RegAcc.Value16,
				(short)arg2 - 10,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((arg3 << 1) + 0x68a0)));

		L111a:
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("RenderCityResourceBar");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		/// <param name="param4"></param>
		public void RenderCityScenery(short townIdx, ushort arg2, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"RenderCityScenery({townIdx}, {arg2}, {arg3}, {arg4})");

			// function body
			this.processor.EnterFrame(0x1c);
			this.processor.PreserveSource();

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4ebc, 0);

			this.processor.EmitLocalWord(0x18, 0x0);
			goto L1165;

		L1145:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x18);

		L1165:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x9);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L118c;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x23);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
			{
				this.owner.VideoCard.ScreenToPicture(1,
					(ushort)(((0x23 * this.processor.PullLocalWord(0x18))) + 1),
					0x34, 0x22, 0x32);
			}
			else
			{
				this.owner.VideoCard.ScreenToPicture(1,
					(ushort)(((0x23 * this.processor.PullLocalWord(0x18))) + 1),
					1, 0x22, 0x32);
			}
			goto L1145;

		L118c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.EmitLocalByte(0x1c, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0xc;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x1c));
			this.processor.CompareWord(this.processor.RegAcc.Value16, arg4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L11bb;

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = arg4;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);

			goto L11c0;

		L11bb:
			this.processor.EmitLocalWord(0x1a, 0xc);

		L11c0:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x70e2),
				0, this.owner.World.Towns[townIdx].TrueExtent);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x70e4), 
				0, this.owner.World.Towns[townIdx].TrueExtent);
			goto L1214;

		L11ef:
			this.processor.EmitLocalWord(0x2, this.processor.DecrementWord(this.processor.PullLocalWord(0x2)));

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x2),
				0, this.owner.World.Towns[townIdx].TrueExtent);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			
			this.processor.DecrementLocalWord(0x4);
			
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0x4),
				0, this.owner.World.Towns[townIdx].TrueExtent);

		L1214:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			
			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Towns[townIdx].TrueExtent - this.processor.PullGlobalSignedWord(0xe8b8),
				0, 99);

			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0x4));
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L11ef;

			this.processor.EmitLocalWord(0x18, 0x0);
			goto L1281;

		L1253:
			this.processor.RegSource.Value16 = 0;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)arg2,
				(short)arg3,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16)));

			arg2 += this.processor.PullLocalWord(0x1a);
			this.processor.IncrementLocalWord(0x18);

		L1281:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1253;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1291;

			arg2 += 8;

		L1291:
			this.processor.EmitLocalWord(0x18, 0x0);
			goto L12c7;

		L1298:
			this.processor.RegSource.Value16 = 0x2;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)arg2,
				(short)arg3,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16)));

			arg2 += this.processor.PullLocalWord(0x1a);
			this.processor.IncrementLocalWord(0x18);

		L12c7:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xe8b8));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x18));
			if (this.processor.FlagSet.IsGreater) goto L1298;
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L12ed;

			arg2 += 8;

		L12ed:
			this.processor.EmitLocalWord(0x18, 0x0);
			goto L1323;

		L12f4:
			this.processor.RegSource.Value16 = 0x4;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)arg2,
				(short)arg3,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x16)));

			arg2 += this.processor.PullLocalWord(0x1a);
			this.processor.IncrementLocalWord(0x18);

		L1323:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L12f4;

			arg2 += 12;
			this.processor.EmitLocalWord(0x18, 0x0);

			goto L1363;

		L1336:
			this.owner.CityOps.ObtainCitizenType(this.processor.PullLocalWord(0x18));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(short)arg2,
				(short)arg3,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xc)));

			arg2 += this.processor.PullLocalWord(0x1a);
			this.processor.IncrementLocalWord(0x18);

		L1363:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe8b8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1336;

			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x16), 0x4ec4);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCityScenery");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="playerID"></param>
		/// <param name="cityID"></param>
		public void RenderCityBuildings(ushort arg1, short participantIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"RenderCityBuildings({arg1}, {participantIdx}, {townIdx})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveIndexRegisters();

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4ec8, 0);

			this.processor.EmitLocalWord(0x4, 0x0);

		L13a1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x6;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegDest.Value16, 1, 0x1f, 0x1f);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x817a), this.processor.RegAcc.Value16);
			
			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegDest.Value16, 0x21, 0x1f, 0x1f);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x817e), this.processor.RegAcc.Value16);

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x21);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegDest.Value16, 1, 0x1f, 0x1f);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x817c), this.processor.RegAcc.Value16);

			this.owner.VideoCard.ScreenToPicture(1, this.processor.RegDest.Value16, 0x21, 0x1f, 0x1f);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x8180), this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x4);
			if (this.processor.FlagSet.IsGreater) goto L1429;
			goto L13a1;

		L1429:
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L1473;

		L1433:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6880), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4);

		L1473:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x10);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1499;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x3a);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
			{
				this.owner.VideoCard.ScreenToPicture(1,
					(ushort)((this.processor.PullLocalWord(0x4) & 0x7) * 0x18),
					(ushort)(((this.processor.PullLocalWord(0x4) >> 3) << 3) + 0x41),
					0x18, 0x8);
			}
			else
			{
				this.owner.VideoCard.ScreenToPicture(1,
					(ushort)((this.processor.PullLocalWord(0x4) & 0x7) * 0x18),
					(ushort)(((this.processor.PullLocalWord(0x4) >> 3) << 3) + 0x51),
					0x18, 0x8);
			}
			goto L1433;

		L1499:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x25);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L14b1;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4ed5, 0);

			this.processor.EmitLocalWord(0x4, 0x0);
			goto L14c6;

		L14b1:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4ee2, 0);

			this.processor.EmitLocalWord(0x4, 0x0);

		L14c6:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegCount.LowByte = this.processor.IncrementByte(this.processor.RegCount.LowByte);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L14fb;

			this.processor.RegAcc.Value16 = arg1;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			if (this.processor.FlagSet.IsEqual) goto L14fb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x14);
			if (this.processor.FlagSet.IsLessOrEqual) goto L153a;

		L14fb:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x4) >> 2) * 0x32) + 1),
				(ushort)(((this.processor.PullLocalWord(0x4) & 0x3) * 0x32) + 1),
				0x31, 0x31);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x81a2), this.processor.RegAcc.Value16);

		L153a:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x17);
			if (this.processor.FlagSet.IsLess) goto L14c6;
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x4, 0x1);

		L154d:
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x4)], this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1596;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, 0x808a);
			if (this.processor.FlagSet.IsNotEqual) goto L1596;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L157e;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4eef, 0);

		L157e:
			this.processor.EmitLocalWord(0x2, 0x1);

			SeizeCityScenery(this.processor.PullLocalWord(0x4));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x68a0), this.processor.RegAcc.Value16);

		L1596:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L154d;
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitLocalWord(0x4, 0x1);

		L15a9:
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0x4)], this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L15f2;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, 0x808a);
			if (this.processor.FlagSet.IsEqual) goto L15f2;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L15da;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4efb, 0);

		L15da:
			this.processor.EmitLocalWord(0x2, 0x1);

			SeizeCityScenery(this.processor.PullLocalWord(0x4));

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x68a0), this.processor.RegAcc.Value16);

		L15f2:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L15a9;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCityBuildings");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		public void SeizeCityScenery(ushort arg1)
		{
			this.processor.Journal.OpenScope($"SeizeCityScenery({arg1})");

			// function body
			this.processor.EnterFrame(0x2);
			this.processor.PreserveSource();

			this.processor.SegExtra.Value16 = 0x3767; // segment
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegExtra.Value16, 0xc), 0x12c);
			if (this.processor.FlagSet.IsLessOrEqual) goto L164f;

			this.processor.EmitLocalWord(0x2, 0x1);

		L161f:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 <<= 3;

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2)));
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6), this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L161f;

		L164f:
			this.processor.RegSource.Value16 = arg1;
			this.processor.RegSource.Value16 <<= 3;
			this.owner.VideoCard.ScreenToPicture(1,
				this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x0)),
				this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x2)),
				this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4)),
				this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6)));

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("SeizeCityScenery");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void RenderCitySceneryForPlayer(short participantIdx)
		{
			this.processor.Journal.OpenScope($"RenderCitySceneryForPlayer({participantIdx})");

			// function body
			this.processor.EnterFrame(0x28);
			this.processor.PreserveSource();

			this.owner.Helpers.HaltHueCycling();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L16a0;

			this.owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(3, 3, 3));

		L16a0:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L16c0;

			goto L1743;

		L16c0:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x3a);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L16d8;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f08, 0);

			this.processor.EmitLocalWord(0x24, 0x0);
			goto L16f2;

		L16d8:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f15, 0);

			this.processor.EmitLocalWord(0x24, 0x0);
			goto L16f2;

		L16ef:
			this.processor.EmitLocalWord(0x24, this.processor.IncrementWord(this.processor.PullLocalWord(0x24)));

		L16f2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xa);
			if (this.processor.FlagSet.IsLess) goto L16fb;
			goto L17a7;

		L16fb:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)((this.processor.PullLocalWord(0x24) % 4) * 0x4f),
				(ushort)(((this.processor.PullLocalWord(0x24) >> 2) * 0x3d) + 2),
				0x4e, 0x3c);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			goto L16ef;

		L1743:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f22, 0);

			this.processor.EmitLocalWord(0x24, 0x0);

		L1758:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x24) % 4) * 0x4f) + 1),
				(ushort)(((this.processor.PullLocalWord(0x24) >> 2) * 0x42) + 1),
				0x4e, 0x41);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xa);
			if (this.processor.FlagSet.IsLess) goto L1758;

		L17a7:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0x64, 0x140, 0x64,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitLocalWord(0x28, 0x0);
			this.processor.EmitLocalWord(0x22, 0x0);
			goto L17df;

		L17db:
			this.processor.EmitLocalWord(0x22, this.processor.SumWord(this.processor.PullLocalWord(0x22), 0x3));

		L17df:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0x294);
			if (this.processor.FlagSet.IsLess) goto L17e9;
			goto L18a2;

		L17e9:
			this.owner.Services.RestoreTimer();
			
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0x64,
				this.processor.PullGlobalWord(0x19d4),
				0, 0x64);

			this.processor.EmitLocalWord(0x24, 0x7);

		L1816:
			this.processor.RegAcc.Value16 = 0x30;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x24));
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x26, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1852;
			this.processor.CompareWord(this.processor.RegCount.Value16, 0xffd0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1852;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x28);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegData.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
				this.processor.PullLocalSignedWord(0x26),
				132,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20)));

		L1852:
			this.processor.EmitLocalWord(0x24, this.processor.DecrementWord(this.processor.PullLocalWord(0x24)));
			if (this.processor.FlagSet.IsUnsigned) goto L1816;
			
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x64, 0x140, 0x64,
				this.processor.PullGlobalWord(0xaa),
				0, 0x64);

			this.processor.IncrementLocalWord(0x28);

		L1880:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsLess) goto L1880;

			this.owner.Startup.ObtainMouseState();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L18a2;
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L18a2;
			goto L17db;

		L18a2:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x20), 0);

			this.owner.Helpers.LaunchHueCycling();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitySceneryForPlayer");
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
		/// ?
		/// </summary>
		public void RenderCitySceneryForHuman()
		{
			this.processor.Journal.OpenScope("RenderCitySceneryForHuman()");

			// function body
			this.processor.EnterFrame(0x28);
			this.processor.PreserveSource();

			this.owner.Helpers.HaltHueCycling();

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x23);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L1b0b;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f52, 0);

			this.processor.EmitLocalWord(0x24, 0x0);
			goto L1b20;

		L1b0b:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4f5c, 0);

			this.processor.EmitLocalWord(0x24, 0x0);

		L1b20:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x24) % 4) * 0x4f) + 1),
				(ushort)(((this.processor.PullLocalWord(0x24) >> 2) * 0x42) + 1),
				0x4e, 0x41);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xa);
			if (this.processor.FlagSet.IsLess) goto L1b20;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0,0x64,0x140,0x64,
				this.processor.PullGlobalWord(0x19d4),
				0,0);

			this.processor.EmitLocalWord(0x28, 0x0);
			this.processor.EmitLocalWord(0x22, 0x1a4);
			goto L1bac;

		L1ba8:
			this.processor.EmitLocalWord(0x22, this.processor.SubtractWord(this.processor.PullLocalWord(0x22), 0x3));

		L1bac:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0xffd0);
			if (this.processor.FlagSet.IsGreater) goto L1bb5;
			goto L1c89;

		L1bb5:
			this.owner.Services.RestoreTimer();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0,0,0x140,0x64,
				this.processor.PullGlobalWord(0x19d4),
				0,0x64);

			this.processor.EmitLocalWord(0x24, 0x0);

		L1be2:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x4f42)));
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1c35;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffd0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1c35;

			this.processor.ScaleIndex(0x3, this.processor.PullLocalWord(0x24));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x28));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = this.processor.RegData.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0x19d4),
				this.processor.PullLocalSignedWord(0x26),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0x24) << 1) + 0x4f4a)) + 132,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20)));

		L1c35:
			this.processor.EmitLocalWord(0x24, this.processor.IncrementWord(this.processor.PullLocalWord(0x24)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x4);
			if (this.processor.FlagSet.IsLess) goto L1be2;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x64, 0x140, 0x64,
				this.processor.PullGlobalWord(0xaa),
				0, 0x64);

			this.processor.IncrementLocalWord(0x28);

		L1c67:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsLess) goto L1c67;

			this.owner.Startup.ObtainMouseState();

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1c89;
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1c89;
			goto L1ba8;

		L1c89:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();
			
			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x20), 0);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitySceneryForHuman");
		}
	}
}
