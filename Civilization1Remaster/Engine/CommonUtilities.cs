using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2dc4.
	/// </summary>
	public class CommonUtilities
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public CommonUtilities(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void SeedRandomizer()
		{
			// function body
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.CurrentTime(0) & 0x7fff);

			this.owner.World.ChanceRoot = this.processor.RegAcc.Value16;

			this.owner.Libc.SeedRandom(this.processor.RegAcc.Value16);
		}

		/// <summary>
		/// Check and adjusts value to given range
		/// </summary>
		/// <param name="value"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public int VerifyValueSpan(int amount, int floorAmount, int peakAmount)
		{
			// function body
			if (floorAmount > peakAmount)
				throw new Exception("Minimum value is greater than maximum value");

			int backAmountInt = Math.Min(Math.Max(amount, floorAmount), peakAmount);
			this.processor.RegAcc.Value16 = (ushort)((short)backAmountInt);

			return backAmountInt;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort LocateCityAtPosition(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateCityAtPosition({horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x2);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L00cb;

		L00c8:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L00cb:
			if (this.processor.PullLocalWord(0x2) >= 128)
				goto L00fa;

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x2));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker == 0xff ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.X != horizLoc ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].Place.Y != vertLoc)
				goto L00c8;

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			goto L00fd;

		L00fa:
			this.processor.RegAcc.Value16 = 0xffff;

		L00fd:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LocateCityAtPosition");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort LocateNearestCity(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateNearestCity({horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x4, 0x3e7);
			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.EmitLocalWord(0x8, 0x0);

		L0118:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x8));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x8)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L015f;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x8));

			ObtainDistance(
				horizLoc, 
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.X,
				vertLoc, 
				this.owner.World.Towns[this.processor.PullLocalWord(0x8)].Place.Y);

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L015f;
			this.processor.CopyLocalWord(0x2, 0x4);
			this.processor.CopyLocalWord(0x8, 0x6);

		L015f:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0118;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitGlobalWord(0x6c9a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LocateNearestCity");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort LocateNearestUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"LocateNearestUnit({participantIdx}, {troopIdx}, {horizLoc}, {vertLoc})");

			// function body
			this.processor.EnterFrame(0x8);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x4, 0x3e7);
			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L01d2;

		L018f:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x8));

			ObtainDistance(
				horizLoc, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				vertLoc, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01cf;
			this.processor.CopyLocalWord(0x2, 0x4);
			this.processor.CopyLocalWord(0x8, 0x6);

		L01cf:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L01d2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01fa;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x8));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L01cf;
			this.processor.RegAcc.Value16 = (ushort)troopIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L018f;
			goto L01cf;

		L01fa:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitGlobalWord(0x6c9a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LocateNearestUnit");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Combines the X and Y positions
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public int MergeCoordinates(int horizLoc, int vertLoc)
		{
			// function body			
			horizLoc = Math.Abs(horizLoc);
			vertLoc = Math.Abs(vertLoc);

			if (horizLoc > vertLoc)
			{

				vertLoc = (short)((vertLoc / 2) + horizLoc);
				this.processor.RegAcc.Value16 = (ushort)((short)vertLoc);

				return vertLoc;
			}
			else
			{
				horizLoc = (short)((horizLoc / 2) + vertLoc);
				this.processor.RegAcc.Value16 = (ushort)((short)horizLoc);

				return horizLoc;
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ObtainDistanceFromOffsets(int horizLoc, int vertLoc)
		{
			this.owner.Journal.OpenScope($"ObtainDistanceFromOffsets({horizLoc}, {vertLoc})");

			// function body
			horizLoc = Math.Abs(horizLoc);

			if (horizLoc > 40)
			{
				horizLoc = 80 - horizLoc;
			}

			vertLoc = Math.Abs(vertLoc);

			if (horizLoc > vertLoc)
			{
				this.processor.RegAcc.Value16 = (ushort)((short)horizLoc * 2 + vertLoc);
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)((short)vertLoc * 2 + horizLoc);
			}

			this.owner.Journal.CloseScope("ObtainDistanceFromOffsets");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="xPos1"></param>
		/// <param name="yPos"></param>
		/// <param name="yPos1"></param>
		/// <returns></returns>
		public int ObtainDistance(int horizLoc, int horizSpot1, int vertLoc, int vertSpot1)
		{
			// function body
			int horizLocScratch = Math.Abs(horizLoc - horizSpot1);

			if (horizLocScratch > 40)
			{
				horizLocScratch = 0x50 - horizLocScratch;
			}
		
			int vertPointposScratch = Math.Abs(vertLoc - vertSpot1);
			
			return MergeCoordinates(horizLocScratch, vertPointposScratch);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <returns></returns>
		public ushort ObtainPlayerPopulation(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainPlayerPopulation({participantIdx})");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.PreserveSource();
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L02fe;

		L02e0:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x4);

		L02e9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x2)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02e0;

		L02fb:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L02fe:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0325;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x2));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ParticipantIdx != participantIdx ||
				this.owner.World.Towns[this.processor.PullLocalWord(0x2)].ConditionMarker == 0xff)
				goto L02fb;

			this.processor.EmitLocalWord(0x4, 0x1);
			goto L02e9;

		L0325:
			PopulationToText(this.processor.PullLocalSignedWord(0x6));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ObtainPlayerPopulation");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Converts population to string
		/// </summary>
		/// <param name="population"></param>
		public void PopulationToText(int inhabitants)
		{
			// function body
			if (inhabitants >= 100)
			{
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(inhabitants / 100, 10));

				this.owner.Libc.AppendText(0xba06, ",");

				if ((inhabitants % 100) < 10)
				{
					this.owner.Libc.AppendText(0xba06, "0");
				}
			}

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(inhabitants % 100, 10));

			this.owner.Libc.AppendText(0xba06, "0,000");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			// function body
			if (this.processor.PullGlobalSignedWord(0xd762) != 0)
			{
				int vertLocScratch = vertLoc;
				int altitudeScratch = altitude;

				while (altitudeScratch > 0)
				{
					int tileAltitude = Math.Min(altitudeScratch, 16);
					int horizLocScratch = horizLoc;
					int breadthScratch = breadth;

					while (breadthScratch > 0)
					{
						int tileBreadth = Math.Min(breadthScratch, 32);

						this.owner.VideoCard.RenderImage(
							this.processor.PullGlobalWord(0x19e8),
							288, 120, tileBreadth, tileAltitude,
							this.processor.PullGlobalWord(0xaa),
							horizLocScratch, vertLocScratch);

						horizLocScratch += tileBreadth;
						breadthScratch -= tileBreadth;
					}

					vertLocScratch += tileAltitude;
					altitudeScratch -= tileAltitude;
				}
			}
			else
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
					horizLoc, vertLoc, breadth, altitude, 7);
			}		
		}

		/// <summary>
		/// Reads the palette from file and applies to screeen 0
		/// </summary>
		/// <param name="filenamePtr"></param>
		public void PullAndAssignColors(ushort docnameAddr)
		{
			this.processor.Journal.OpenScope($"PullAndAssignColors(0x{docnameAddr:x4})");

			// function body
			this.processor.EnterFrame(0x2);
			this.processor.PreserveSource();
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L04d8;

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docnameAddr, 0xbdee);

			this.processor.EmitLocalWord(0x2, 0x0);

		L049f:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.RegBase.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6b34));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbdf4), this.processor.RegAcc.LowByte);
			this.processor.IncrementLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x30);
			if (this.processor.FlagSet.IsLess) goto L049f;
			
			this.owner.VideoCard.AssignHuesFromHueStruct(0xbdee);
			
			this.owner.VideoCard.AssignHue(0x2d, VGABitmap.Hue18ToHue(0x33, 0x27, 0x19));

		L04d8:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("PullAndAssignColors");
		}

		/// <summary>
		/// Free resource, show Memory error dialog if error happens
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="stringPtr"></param>
		public void ReleaseResource(ushort arg1, ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"ReleaseResource({arg1}, 0x{charsAddr:x4})");

			// function body
			this.processor.EnterFrame();
			
			this.owner.Libc.DosReleaseMemory(arg1);

			if (this.processor.RegAcc.Value16 != 0)
			{
				this.owner.Libc.CopyText(0xba06, "MEM.ERR:");

				if (charsAddr != 0)
				{
					this.owner.Libc.AppendText(0xba06, charsAddr);
				}

				this.owner.Libc.AppendText(0xba06, "\n");

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("ReleaseResource");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void SetupHueCycling()
		{
			this.processor.Journal.OpenScope("SetupHueCycling()");

			// function body
			this.owner.Services.AssignHueCycleSpan(1, 15, 96, 103);

			this.owner.Services.AssignHueCycleSpan(2, 15, 104, 111);

			this.owner.Services.AssignHueCycleSpan(3, 15, 112, 127);

			// Far return
			this.processor.Journal.CloseScope("SetupHueCycling");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void LaunchHueCycling()
		{
			// function body
			if (this.processor.PullGlobalWord(0x2fec) == 0 && this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.ActivateHueCycle(1);

				this.owner.Services.ActivateHueCycle(2);

				this.owner.Services.ActivateHueCycle(3);
			}
		
			this.processor.EmitGlobalWord(0x2fec, 0x1);
		}

		/// <summary>
		/// ?
		/// </summary>
		public void HaltHueCycling()
		{
			this.processor.Journal.OpenScope("HaltHueCycling()");

			// function body
			if (this.processor.PullGlobalWord(0x2fec) != 0 &&
				this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.DeactivateHueCycle(1);

				this.owner.Services.DeactivateHueCycle(2);

				this.owner.Services.DeactivateHueCycle(3);
			}

			this.processor.EmitGlobalWord(0x2fec, 0x0);

			// Far return
			this.processor.Journal.CloseScope("HaltHueCycling");
		}
	}
}
