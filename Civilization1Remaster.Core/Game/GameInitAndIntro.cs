using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	public class GameInitAndIntro
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public GameInitAndIntro(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void OpeningSequence()
		{
			this.processor.Journal.OpenScope("OpeningSequence()");

			// function body
			this.processor.EnterFrame(0xe);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.QueryForDisk(7, 1);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			this.processor.EmitGlobalWord(0x67fc, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd76a), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0054;
			
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3b20, 1);

			goto L0578;

		L0054:
			DisplayStoryIntro();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

		L0073:
			ProduceCloudLayer();

			DisplayStoryIntro();

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7ef6);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = 0x28;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x280);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x67fe));
			if (this.processor.FlagSet.IsGreater) goto L0073;

			this.owner.Dialogs.RenderBox(0, 0, 79, 49, 0);

			this.owner.Dialogs.RenderBox(1, 1, 77, 47, 0);

			this.processor.EmitLocalWord(0xa, 0x1);

		L00c4:
			this.processor.EmitLocalWord(0xc, 0x1);

		L00c9:
			this.processor.EmitLocalWord(0x2, 0x0);

			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L00e8;
			
			this.processor.EmitLocalByte(0x2, this.processor.BitOrByte(this.processor.PullLocalByte(0x2), 0x1));

		L00e8:
			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa) + 1,
				this.processor.PullLocalSignedWord(0xc));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0104;
			this.processor.EmitLocalByte(0x2, this.processor.BitOrByte(this.processor.PullLocalByte(0x2), 0x2));

		L0104:
			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc) + 1);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0120;
			this.processor.EmitLocalByte(0x2, this.processor.BitOrByte(this.processor.PullLocalByte(0x2), 0x4));

		L0120:
			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa) + 1,
				this.processor.PullLocalSignedWord(0xc) + 1);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L013e;
			this.processor.EmitLocalByte(0x2, this.processor.BitOrByte(this.processor.PullLocalByte(0x2), 0x8));

		L013e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x6);
			if (this.processor.FlagSet.IsEqual) goto L014a;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L0195;

		L014a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa) + 1,
				this.processor.PullLocalSignedWord(0xc),
				1);

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc) + 1,
				1);

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa) + 1,
				this.processor.PullLocalSignedWord(0xc) + 1,
				1);

			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L018c;
			this.processor.DecrementLocalWord(0xa);

		L018c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0195;
			this.processor.DecrementLocalWord(0xc);

		L0195:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x31);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01a1;
			goto L00c9;

		L01a1:
			DisplayStoryIntro();

			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x4f);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01b1;
			goto L00c4;

		L01b1:
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L0296;

		L01b9:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				1);

		L01cc:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L01cf:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x32);
			if (this.processor.FlagSet.IsLess) goto L01d8;
			goto L028f;

		L01d8:
			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L01b9;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0207;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L01fe;
			goto L0285;

		L01fe:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				13);

			goto L01cc;

		L0207:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(8));

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x1d);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x7ef8));
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xffff);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x7);
			if (this.processor.FlagSet.IsAbove) goto L01cc;
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L024a;
				case 1:
					goto L024a;
				case 2:
					goto L0254;
				case 3:
					goto L0254;
				case 4:
					goto L025e;
				case 5:
					goto L025e;
				case 6:
					goto L0268;
				case 7:
					goto L0268;
			}

		L024a:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				14);

			goto L01cc;

		L0254:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				6);

			goto L01cc;

		L025e:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				7);

			goto L01cc;

		L0268:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				15);

			goto L01cc;

		L0285:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				12);

			goto L01cc;

		L028f:
			DisplayStoryIntro();

			this.processor.IncrementLocalWord(0xa);

		L0296:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02a4;
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L01cf;

		L02a4:
			this.processor.EmitLocalWord(0xc, 0x0);
			goto L0448;

		L02ac:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L02fa;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7efa);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));

			this.processor.EmitLocalWord(0x8, this.processor.SubtractWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L0340;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x7);
			if (this.processor.FlagSet.IsEqual) goto L0352;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsEqual) goto L0349;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd);
			if (this.processor.FlagSet.IsEqual) goto L035b;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L02fa;

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				6);

		L02fa:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L02fd:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0361;

			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L02ac;
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x7efa);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			if (this.processor.FlagSet.IsLessOrEqual) goto L02fa;
			this.processor.IncrementLocalWord(0x8);
			goto L02fa;

		L0340:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				10);

			goto L02fa;

		L0349:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				2);

			goto L02fa;

		L0352:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				15);

			goto L02fa;

		L035b:
			this.processor.EmitLocalWord(0x8, this.processor.SubtractWord(this.processor.PullLocalWord(0x8), 0x3));
			goto L02fa;

		L0361:
			this.processor.EmitLocalWord(0x8, 0x0);
			this.processor.EmitLocalWord(0xa, 0x4f);
			goto L03cc;

		L036d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L03c9;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7efa);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));

			this.processor.EmitLocalWord(0x8, this.processor.SubtractWord(this.processor.PullLocalWord(0x8), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0394;
			goto L0437;

		L0394:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6);
			if (this.processor.FlagSet.IsEqual) goto L0402;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L040b;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsNotEqual) goto L03a6;
			goto L0437;

		L03a6:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L03ae;
			goto L0433;

		L03ae:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xe);
			if (this.processor.FlagSet.IsNotEqual) goto L03c9;

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				6);

		L03c9:
			this.processor.EmitLocalWord(0xa, this.processor.DecrementWord(this.processor.PullLocalWord(0xa)));

		L03cc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x0);
			if (this.processor.FlagSet.IsLess) goto L0441;

			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L036d;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x7efa));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			if (this.processor.FlagSet.IsLessOrEqual) goto L03c9;
			this.processor.IncrementLocalWord(0x8);
			goto L03c9;

		L0402:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				10);

			goto L03c9;

		L040b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xa);
			if (this.processor.FlagSet.IsLess)
			{
				this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
					this.processor.PullLocalSignedWord(0xc),
					11);
			}
			else
			{
				this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
					this.processor.PullLocalSignedWord(0xc),
					3);
			}

			this.processor.EmitLocalWord(0x8, 0xfffe);
			goto L03c9;

		L0433:
			this.processor.EmitLocalWord(0x8, this.processor.SubtractWord(this.processor.PullLocalWord(0x8), 0x3));

		L0437:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				2);

			goto L03c9;

		L0441:
			DisplayStoryIntro();

			this.processor.IncrementLocalWord(0xc);

		L0448:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L046d;
			this.processor.RegAcc.Value16 = 0x19;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x0);
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L02fd;

		L046d:
			this.processor.EmitLocalWord(0xe, 0x0);
			goto L0537;

		L0475:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(80));

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(50));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

		L0493:
			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L04b3;
			goto L0534;

		L04b3:
			switch (this.processor.RegAcc.Value16)
			{
				case 0:
					goto L04dc;
				case 1:
					goto L04f7;
				case 2:
					goto L0534;
				case 3:
					goto L0534;
				case 4:
					goto L04e5;
				case 5:
					goto L04e5;
				case 6:
					goto L0534;
				case 7:
					goto L0534;
				case 8:
					goto L04d3;
				case 9:
					goto L0500;
				case 10:
					goto L04ee;
				case 11:
					goto L04c4;
				case 12:
					goto L04bb;
				case 13:
					goto L04ee;
			}

		L04bb:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				6);

			goto L0534;

		L04c4:
			BlendTerrainTile(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));
			
			goto L0534;

		L04d3:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				2);

			goto L0534;

		L04dc:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				11);

			goto L0534;

		L04e5:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				12);

			goto L0534;

		L04ee:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				13);

			goto L0534;

		L04f7:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				10);

			goto L0534;

		L0500:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc),
				3);

		L0534:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));

		L0537:
			this.processor.RegAcc.Value16 = 0x320;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x7efc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x320);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			if (this.processor.FlagSet.IsLessOrEqual) goto L0574;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xe), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L054f;
			goto L0475;

		L054f:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(8));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), this.processor.RegAcc.Value16));
			goto L0493;

		L0574:
			ProduceElevation();

		L0578:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 160, 0, 160, 200, 0);

			ProduceTerrain();

			this.processor.EmitGlobalWord(0x67fc, 0x1);

		L05a0:
			DisplayStoryIntro();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L05a0;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19e8),
				0, 0);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("OpeningSequence");
		}

		/// <summary>
		/// On the map being made (screen 1) the tile becomes land (colour 1) unless one of its four diagonal neighbours is land
		/// </summary>
		public void BlendTerrainTile(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"BlendTerrainTile({horizLoc}, {vertLoc})");

			if (this.owner.VideoCard.ObtainDot(1, horizLoc - 1, vertLoc - 1) == 1 ||
				this.owner.VideoCard.ObtainDot(1, horizLoc - 1, vertLoc + 1) == 1 ||
				this.owner.VideoCard.ObtainDot(1, horizLoc + 1, vertLoc - 1) == 1 ||
				this.owner.VideoCard.ObtainDot(1, horizLoc + 1, vertLoc + 1) == 1)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.owner.Services.AssignDot(1, horizLoc, vertLoc, 1);
			}

			this.processor.Journal.CloseScope("BlendTerrainTile");
		}

		/// <summary>
		/// Rivers of the new map (in fact not the elevation): up to 256 attempts, until there are more than 2 * (0x7ef6 + 0x7efa)
		/// + 6 rivers. An attempt starts at a random hill (screen 1 value 12) and flows (colour 9) in one of 4 directions, turning
		/// by 45 degrees at random, until it reaches a tile next to the ocean (1), a river (9) or a value 13 or 1; it stays when
		/// it got next to the ocean or into a river after at least 5 steps (and the grassland, 2, around its spring becomes 11),
		/// else the map is put back from the copy at column 160. The story of the intro goes on after each attempt.
		/// </summary>
		public void ProduceElevation()
		{
			this.processor.Journal.OpenScope($"ProduceElevation()");

			MapLayers chart = this.owner.MapOps.Chart;
			short riverTally = 0;
			short attemptTally = 0;

			while (true)
			{
				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0xa0, 0x64, this.processor.PullGlobalWord(0xaa), 0xa0, 0);

				short horizLoc;
				short vertLoc;

				do
				{
					horizLoc = (short)this.owner.Libc.Dice.DrawInt(80);
					vertLoc = (short)this.owner.Libc.Dice.DrawInt(50);
				} while (this.owner.VideoCard.ObtainDot(1, horizLoc, vertLoc) != 0xc);

				short springHoriz = horizLoc;
				short springVert = vertLoc;
				short heading = (short)(this.owner.Libc.Dice.DrawInt(4) << 1);
				short length = 0;
				bool nearOcean;
				ushort reached;

				do
				{
					this.owner.Services.AssignDot(1, horizLoc, vertLoc, 9);

					nearOcean = false;

					for (int side = 1; side <= 8; side += 2)
					{
						if (this.owner.VideoCard.ObtainDot(1, chart.DirectionHoriz(side) + horizLoc, chart.DirectionVert(side) + vertLoc) == 1)
						{
							nearOcean = true;
						}
					}

					short previousHeading = heading;
					heading = (short)(((((short)this.owner.Libc.Dice.DrawInt(2) - (length & 1)) << 1) + heading) & 7);

					if ((previousHeading ^ 4) > heading)
					{
						this.owner.Services.AssignDot(1, horizLoc, vertLoc + 0x32, 8);
					}

					// The steps of the headings 0 - 7 are the directions 1 - 8
					horizLoc += (short)chart.DirectionHoriz(heading + 1);
					vertLoc += (short)chart.DirectionVert(heading + 1);

					reached = this.owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

					length++;
				} while (!nearOcean && reached != 0xd && reached != 1 && reached != 9);

				if ((nearOcean || reached == 9) && length >= 5)
				{
					riverTally++;

					for (int spotIdx = 1; spotIdx <= 0x15; spotIdx++)
					{
						horizLoc = (short)(chart.DirectionHoriz(spotIdx) + springHoriz);
						vertLoc = (short)(chart.DirectionVert(spotIdx) + springVert);

						if (this.owner.VideoCard.ObtainDot(1, horizLoc, vertLoc) == 2)
						{
							this.owner.Services.AssignDot(1, horizLoc, vertLoc, 11);
						}
					}
				}
				else
				{
					this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0xa0, 0, 0xa0, 0x64, this.processor.PullGlobalWord(0xaa), 0, 0);
				}

				DisplayStoryIntro();

				attemptTally++;

				if (attemptTally >= 0x100 ||
					(short)(((this.processor.PullGlobalWord(0x7ef6) + this.processor.PullGlobalWord(0x7efa)) << 1) + 6) <= riverTally)
					break;
			}

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0x32, 0x50, 0x32, this.processor.PullGlobalWord(0xaa), 0, 0x96);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 50, 80, 50, 0);

			this.processor.Journal.CloseScope("ProduceElevation");
		}

		/// <summary>
		/// A cloud layer for the intro: one cloud at a random place (columns 4 - 75, rows 8 - 41) drawn in the area at column 160
		/// of screen 1, then every pixel of the 80 x 50 area at column 0 under a cloud pixel gets one colour lighter (0x67fe
		/// counts them)
		/// </summary>
		public void ProduceCloudLayer()
		{
			this.processor.Journal.OpenScope("ProduceCloudLayer()");

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 160, 0, 80, 50, 0);

			short cloudHoriz = (short)(this.owner.Libc.Dice.DrawInt(72) + 4);
			short cloudVert = (short)(this.owner.Libc.Dice.DrawInt(34) + 8);

			ProduceCloud(cloudHoriz, cloudVert);

			for (int horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					if (this.owner.VideoCard.ObtainDot(1, horizLoc + 0xa0, vertLoc) != 0)
					{
						ushort hue = this.owner.VideoCard.ObtainDot(1, horizLoc, vertLoc);

						this.owner.Services.AssignDot(1, horizLoc, vertLoc, (ushort)(hue + 1));

						this.processor.EmitGlobalWord(0x67fe, (ushort)(this.processor.PullGlobalWord(0x67fe) + 1));
					}
				}
			}

			this.processor.Journal.CloseScope("ProduceCloudLayer");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void ProduceCloud(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ProduceCloud({horizLoc}, {vertLoc})");

			// function body
			int vaporExtent = this.owner.Libc.Dice.DrawInt(64) + 1;

			do
			{
				this.owner.Services.AssignDot(1, horizLoc + 160, vertLoc, 15);
				this.owner.Services.AssignDot(1, horizLoc + 161, vertLoc, 15);
				this.owner.Services.AssignDot(1, horizLoc + 160, vertLoc + 1, 15);

				switch (this.owner.Libc.Dice.DrawInt(4))
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

			// Far return
			this.processor.Journal.CloseScope("ProduceCloud");
		}

		/// <summary>
		/// Numbers the continents (pass 0) and the seas (pass 1) of the screen map: the tiles of the kind are labelled row by row
		/// in ChartSight (joined with the touching labels of the row above, a label's size in 0xdcfe + 0x80 * pass + 2 * label),
		/// the 15 largest get the numbers 1 - 15 by size (drawn at y + 50, the others 15) and their sizes; label 0 has size 0
		/// and 15 size 1. Then the terrain map, the block links, the special resources and the polar rows (lines and 4 * 20
		/// random tundra dots). The terrain counts of pass 0 and the ranking are in the stack frame, like in the original; the
		/// story goes on after every row and every join.
		/// </summary>
		public void ProduceTerrain()
		{
			this.processor.Journal.OpenScope("ProduceTerrain()");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0xd4);
			this.processor.PreserveIndexRegisters();

			ushort frame = this.processor.RegFrame.Value16;
			ushort terrainCounts = (ushort)(frame - 0x20);

			for (ushort terrain = 0; terrain < 0xc; terrain++)
			{
				this.processor.RegSource.Value16 = (ushort)(terrain << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(terrainCounts + (terrain << 1)), 0);
			}

			ushort pass;

			for (pass = 0; pass < 2; pass++)
			{
				this.processor.EmitLocalWord(0x8, pass);

				LabelLandOrSea(pass, terrainCounts);

				RankLandOrSea(pass);
			}

			this.processor.EmitLocalWord(0x8, pass);

			DisplayStoryIntro();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19e8), 0, 0);

			LabelLandmasses();

			ReckonContinentSizes(1);

			DisplayStoryIntro();

			SituateSpecialTerrain();

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 0, 79, 0, 15);
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 49, 79, 49, 15);

			ushort dotIdx;

			for (dotIdx = 0; dotIdx < 0x14; dotIdx++)
			{
				this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 0, 7);
				this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 1, 7);
				this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 48, 7);
				this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 49, 7);
			}

			this.processor.EmitLocalWord(0x24, dotIdx);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ProduceTerrain");
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
		private void LabelLandOrSea(ushort pass, ushort terrainCounts)
		{
			ushort horizLoc, vertLoc;

			for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
					this.owner.World.ChartSight[horizLoc, vertLoc] = 0;

				this.processor.EmitLocalWord(0xa8, vertLoc);
			}

			this.processor.EmitLocalWord(0x24, horizLoc);

			ushort newLabel = 0;
			this.processor.EmitLocalWord(0xae, newLabel);

			for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
			{
				this.processor.EmitLocalWord(0xa8, vertLoc);

				short run = -1;
				bool inRun = false;

				this.processor.EmitLocalWord(0x22, 0xffff);
				this.processor.EmitLocalWord(0x6, 0);

				for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					this.processor.EmitLocalWord(0x24, horizLoc);

					ushort terrain = ObtainTerrainTypeFromScreen((short)horizLoc, (short)vertLoc);
					terrain = this.processor.RegAcc.Value16;
					this.processor.EmitLocalWord(0xaa, terrain);

					if (pass == 0)
					{
						ushort count = (ushort)(terrainCounts + (terrain << 1));
						this.processor.RegSource.Value16 = (ushort)(terrain << 1);
						this.processor.EmitUInt16(this.processor.SegStack.Value16, count, (ushort)(this.processor.PullUInt16(this.processor.SegStack.Value16, count) + 1));
					}

					if ((terrain != 0xa) != (pass == 0))
					{
						inRun = false;
						run = -1;

						this.processor.EmitLocalWord(0x6, 0);
						this.processor.EmitLocalWord(0x22, 0xffff);
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
								this.processor.RegSource.Value16 = (ushort)(pass << 7);
							}
							while (this.processor.PullGlobalWord(LabelSizeAddress(pass, newLabel)) != 0);

							this.processor.EmitLocalWord(0xae, newLabel);
						}

						run = (short)newLabel;
						this.processor.EmitLocalWord(0x22, (ushort)run);
					}

					this.owner.World.ChartSight[horizLoc, vertLoc] = (sbyte)(byte)run;

					this.processor.RegSource.Value16 = (ushort)(pass << 7);

					ushort size = LabelSizeAddress(pass, run);
					this.processor.EmitGlobalWord(size, (ushort)(this.processor.PullGlobalWord(size) + 1));

					inRun = true;
					this.processor.EmitLocalWord(0x6, 1);
				}

				this.processor.EmitLocalWord(0x24, horizLoc);

				DisplayStoryIntro();
			}

			this.processor.EmitLocalWord(0xa8, vertLoc);
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
				this.processor.EmitLocalWord(0xac, (ushort)shift);

				short label = this.owner.World.ChartSight[(ushort)(horizLoc + shift), (ushort)(vertLoc - 1)];
				this.processor.EmitLocalWord(0xd0, (ushort)label);

				if (label == 0)
					continue;

				if (run != -1 && run != label)
				{
					ushort row;

					for (row = 0; (short)row <= vertLoc; row++)
					{
						this.processor.EmitLocalWord(0x4, row);

						ushort column;

						for (column = 0; column < 0x50; column++)
						{
							if (this.owner.World.ChartSight[column, row] == run)
								this.owner.World.ChartSight[column, row] = (sbyte)(byte)label;

							ushort from = LabelSizeAddress(pass, run);
							ushort to = LabelSizeAddress(pass, label);

							this.processor.RegSource.Value16 = (ushort)(pass << 7);
							this.processor.RegDest.Value16 = (ushort)(from);

							this.processor.EmitGlobalWord(to, (ushort)(this.processor.PullGlobalWord(to) + this.processor.PullGlobalWord(from)));
							this.processor.EmitGlobalWord(from, 0);
						}

						this.processor.EmitLocalWord(0x2, column);
					}

					this.processor.EmitLocalWord(0x4, row);
				}

				run = label;
				this.processor.EmitLocalWord(0x22, (ushort)run);

				DisplayStoryIntro();
			}

			this.processor.EmitLocalWord(0xac, (ushort)shift);

			return run;
		}

		/// <summary>
		/// The 15 largest labels of the pass get the numbers 1 - 15 by size (the rest 15; ranking in the frame from -0xce, the
		/// numbers from -0xa4), drawn at (x, y + 50); the sizes 1 - 14 become those of the ranks, 0 gets size 0 and 15 size 1
		/// </summary>
		private void RankLandOrSea(ushort pass)
		{
			ushort frame = this.processor.RegFrame.Value16;
			ushort ranking = (ushort)(frame - 0xce);
			ushort numbers = (ushort)(frame - 0xa4);

			for (ushort rank = 0; rank < 0x10; rank++)
			{
				this.processor.RegSource.Value16 = (ushort)(rank << 1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(ranking + (rank << 1)), 0);
			}

			this.processor.EmitLocalWord(0xa4, 0);

			ushort label;

			for (label = 1; label < 0x40; label++)
			{
				this.processor.EmitLocalWord(0x24, label);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(numbers + (label << 1)), 0xf);

				ushort size = this.processor.PullGlobalWord(LabelSizeAddress(pass, label));
				ushort rank;

				for (rank = 1; rank < 0xf; rank++)
				{
					this.processor.RegSource.Value16 = (ushort)(pass << 7);

					ushort ranked = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(ranking + (rank << 1)));

					if ((short)this.processor.PullGlobalWord(LabelSizeAddress(pass, ranked)) >= (short)size)
						continue;

					// The smaller ones move down a rank
					ushort lower;

					for (lower = 0xf; (short)lower > (short)rank; lower--)
					{
						ushort moved = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(ranking + (lower << 1) - 2));

						this.processor.RegSource.Value16 = (ushort)((lower << 1) + frame);
						this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(numbers + (moved << 1)), lower);
						this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(ranking + (lower << 1)), moved);
					}

					this.processor.EmitLocalWord(0xac, lower);

					this.processor.RegSource.Value16 = (ushort)(rank << 1);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(numbers + (label << 1)), rank);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(ranking + (rank << 1)), label);
					break;
				}

				this.processor.EmitLocalWord(0xa8, rank);
			}

			this.processor.EmitLocalWord(0xa4, 0);

			ushort horizLoc, vertLoc = 0;

			// A label from 0x40 on reads past the numbers, the locals of the frame (x at 0x24, y at 0xa8, ...), like in the original
			for (horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				this.processor.EmitLocalWord(0x24, horizLoc);

				for (vertLoc = 0; vertLoc < 0x32; vertLoc++)
				{
					this.processor.EmitLocalWord(0xa8, vertLoc);

					short tileLabel = this.owner.World.ChartSight[horizLoc, vertLoc];
					this.processor.RegSource.Value16 = (ushort)(tileLabel << 1);

					ushort number = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(numbers + (tileLabel << 1)));
					this.processor.EmitLocalWord(0xa6, number);

					if (number != 0)
						this.owner.Services.AssignDot((short)horizLoc, (short)vertLoc + 0x32, number);
				}
			}

			this.processor.EmitLocalWord(0xa8, vertLoc);

			for (ushort rank = 1; rank < 0xf; rank++)
			{
				ushort entry = (ushort)(ranking + (rank << 1));

				this.processor.RegSource.Value16 = entry;
				this.processor.RegDest.Value16 = (ushort)(pass << 7);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, entry, this.processor.PullGlobalWord(LabelSizeAddress(pass, this.processor.PullUInt16(this.processor.SegStack.Value16, entry))));
			}

			for (ushort rank = 1; rank < 0xf; rank++)
			{
				this.processor.RegSource.Value16 = (ushort)(rank << 1);
				this.processor.EmitGlobalWord(LabelSizeAddress(pass, rank), this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(ranking + (rank << 1))));
			}

			this.processor.EmitLocalWord(0x24, 0xf);

			this.processor.RegSource.Value16 = (ushort)(pass << 7);
			this.processor.EmitGlobalWord(LabelSizeAddress(pass, 0), 0);
			this.processor.EmitGlobalWord(LabelSizeAddress(pass, 0xf), 1);
		}

		/// <summary>
		/// The value of every land tile as a city site (in the map screen): the weights of the 12 terrains and their specials
		/// (3 * food + trade, + 2 * shields except on 2 and 11, + the bonus of irrigation 0x44e or twice of mining 0x44a), summed
		/// over the 21 tiles of a city (the inner 8 twice, the centre four times, +2 for rivers on terrains 2 and 11), less 16 off
		/// the river pattern unless on terrain 1; the value 8 - 15 is drawn at (x + 80, y) and (x + 80, y + 50), the other tiles
		/// get 0. Every site counts for its continent (ConstructionSpotTally).
		/// </summary>
		public void SituateSpecialTerrain()
		{
			this.processor.Journal.OpenScope("SituateSpecialTerrain()");

			for (int continentIdx = 0; continentIdx < 0x10; continentIdx++)
			{
				this.owner.World.Landmasses[continentIdx].ConstructionSpotTally = 0;
			}

			ushort[] weights = new ushort[0x18];

			for (int terrainIdx = 0; terrainIdx < 0x18; terrainIdx++)
			{
				ushort terrain = (ushort)(0x13 * terrainIdx);
				int kind = terrainIdx % 12;

				weights[terrainIdx] = (ushort)((3 * (sbyte)this.processor.PullGlobalByte((ushort)(terrain + 0x290))) +
					(sbyte)this.processor.PullGlobalByte((ushort)(terrain + 0x292)));

				if (kind != 2 && kind != 11)
				{
					weights[terrainIdx] += (ushort)((sbyte)this.processor.PullGlobalByte((ushort)(terrain + 0x291)) << 1);
				}

				short irrigation = (short)this.processor.PullGlobalWord((ushort)((0xc * kind) + 0x44e));

				if (irrigation < 0)
				{
					weights[terrainIdx] += (ushort)(0xffff - irrigation);
				}
				else
				{
					short mining = (short)this.processor.PullGlobalWord((ushort)((0xc * kind) + 0x44a));

					if (mining < 0)
					{
						weights[terrainIdx] += (ushort)((0xffff - mining) << 1);
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
						this.owner.Services.AssignDot(horizLoc + 80, vertLoc, 0);
						this.owner.Services.AssignDot(horizLoc + 80, vertLoc + 0x32, 0);
						continue;
					}

					short total = 0;

					for (int deltaIdx = 0; deltaIdx <= 0x14; deltaIdx++)
					{
						short value = 0;
						short tileHoriz = (short)(this.owner.TownDeltas[deltaIdx].X + horizLoc);
						short tileVert = (short)(this.owner.TownDeltas[deltaIdx].Y + vertLoc);

						ushort tile = ObtainTerrainTypeFromScreen(tileHoriz, tileVert);

						if ((tile == 2 || tile == 0xb) && !OffRiverPattern(tileHoriz, tileVert))
						{
							value += 2;
						}

						this.owner.MapOps.ContainsSpecialResource(tileHoriz, tileVert);

						if (this.processor.RegAcc.Value16 != 0)
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

					short worth = (short)(((short)this.owner.Helpers.VerifyValueSpan((total - 120) / 8, 1, 15) / 2) + 8);

					this.owner.Services.AssignDot(horizLoc + 80, vertLoc, (ushort)worth);
					this.owner.Services.AssignDot(horizLoc + 80, vertLoc + 0x32, (ushort)worth);

					this.owner.World.Landmasses[ObtainContinentIDFromScreen(horizLoc, vertLoc)].ConstructionSpotTally++;
				}
			}

			// Far return
			this.processor.Journal.CloseScope("SituateSpecialTerrain");
		}

		/// <summary>The tile is off the pattern of 7 * x + 11 * y (bit 1 of the low byte set)</summary>
		private static bool OffRiverPattern(short horizLoc, short vertLoc)
		{
			return (((7 * horizLoc) + (0xb * vertLoc)) & 2) != 0;
		}

		/// <summary>
		/// The links between the 20 x 13 blocks of 4 x 4 tiles (bits at 0xdb44 + 13 * column + row): a block with land (the
		/// first of its tiles (1, 1), (2, 1), (1, 2), (2, 2) that isn't ocean on the screen map) gets bit direction - 1, and its
		/// neighbour in the direction 1 - 4 (0x1882 / 0x18e4) bit (direction + 3) &amp; 7, when the neighbour's land is the same
		/// continent and a route of less than 20 (ComputeRouteCost) joins them. The final values of the locals stay in the
		/// stack frame, like in the original.
		/// </summary>
		public void LabelLandmasses()
		{
			this.processor.Journal.OpenScope("LabelLandmasses()");

			this.processor.EnterFrame(0x26);
			this.processor.PreserveIndexRegisters();

			this.owner.Libc.FillBytes(0xdb44, 0, 0x104);

			// The last values of the locals (null: never set), written into the frame at the end
			(short, short)? neighbour = null, origin = null, land = null, neighbourLand = null;
			short? lastDirection = null, lastNeighbourContinent = null, lastCost = null, lastContinent = null;
			short column, row = 0;

			for (column = 0; column < 0x14; column++)
			{
				for (row = 0; row < 0xd; row++)
				{
					short horizLoc = (short)((column << 2) + 1), vertLoc = (short)((row << 2) + 1);

					origin = (horizLoc, vertLoc);

					short continent = FindBlockLand(horizLoc, vertLoc, ref land);
					lastContinent = continent;

					if (continent == -1)
						continue;

					for (short direction = 1; direction <= 4; direction++)
					{
						lastDirection = direction;

						short nextHoriz = (short)((this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) << 2) + horizLoc);
						short nextVert = (short)((this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) << 2) + vertLoc);

						neighbour = (nextHoriz, nextVert);

						short nextContinent = FindBlockLand(nextHoriz, nextVert, ref neighbourLand);
						lastNeighbourContinent = nextContinent;

						if (continent != nextContinent)
							continue;

						short cost = (short)this.owner.Routing.ComputeRouteCost(land!.Value.Item1, land.Value.Item2,
							neighbourLand!.Value.Item1, neighbourLand.Value.Item2, 0, 20);
						lastCost = cost;

						if (cost == -1 || cost >= 0x14)
							continue;

						LinkLandmassBlock(column, row, direction - 1);

						short nextColumn = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x1882)) + column);
						short nextRow = (short)(this.processor.PullGlobalWord((ushort)((direction << 1) + 0x18e4)) + row);

						neighbour = (nextColumn, nextRow);

						if (nextColumn < 0 || nextColumn >= 0x14 || nextRow < 0 || nextRow >= 0xd)
							continue;

						LinkLandmassBlock(nextColumn, nextRow, (direction + 3) & 7);
					}

					lastDirection = 5;
				}
			}

			if (neighbour is (short nextHorizLoc, short nextVertLoc))
			{
				this.processor.EmitLocalWord(0x2, (ushort)nextHorizLoc);
				this.processor.EmitLocalWord(0x4, (ushort)nextVertLoc);
			}

			if (origin is (short originHoriz, short originVert))
			{
				this.processor.EmitLocalWord(0x8, (ushort)originHoriz);
				this.processor.EmitLocalWord(0xc, (ushort)originVert);
			}

			if (lastDirection is short directionLeft)
				this.processor.EmitLocalWord(0xa, (ushort)directionLeft);

			if (lastNeighbourContinent is short neighbourContinent)
				this.processor.EmitLocalWord(0xe, (ushort)neighbourContinent);

			if (lastCost is short costLeft)
				this.processor.EmitLocalWord(0x10, (ushort)costLeft);

			this.processor.EmitLocalWord(0x14, (ushort)column);

			if (neighbourLand is (short neighbourHoriz, short neighbourVert))
			{
				this.processor.EmitLocalWord(0x16, (ushort)neighbourHoriz);
				this.processor.EmitLocalWord(0x1c, (ushort)neighbourVert);
			}

			if (lastContinent is short continentLeft)
				this.processor.EmitLocalWord(0x18, (ushort)continentLeft);

			this.processor.EmitLocalWord(0x1a, (ushort)row);

			if (land is (short landHoriz, short landVert))
			{
				this.processor.EmitLocalWord(0x20, (ushort)landHoriz);
				this.processor.EmitLocalWord(0x24, (ushort)landVert);
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LabelLandmasses");
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

				return (short)this.owner.MapOps.ObtainContinentID(tileHoriz, tileVert);
			}

			return -1;
		}

		/// <summary>Sets bit bitIdx of the block's links (0xdb44 + 13 * column + row)</summary>
		private void LinkLandmassBlock(short column, short row, int bitIdx)
		{
			ushort links = (ushort)((0xd * column) + row + 0xdb44);

			this.processor.EmitGlobalByte(links, (byte)(this.processor.PullGlobalByte(links) | (1 << bitIdx)));
		}

		/// <summary>
		/// The sea links between the 20 x 12 blocks of 4 x 4 tiles (bits at 0x7f38 + 13 * column + row): a block with sea (the
		/// first ocean tile of (1, 1), (2, 1), (1, 2), (2, 2) on the screen map) gets bit direction - 1, and its neighbour in the
		/// direction 1 - 8 (0x1882 / 0x18e4) bit (direction + 3) &amp; 7, when the neighbour's sea is the same and a passage of
		/// 5 steps from the block's corner has at least 2 of 4 ocean tiles inside the map at each step (always for direction 3
		/// in the last row). The neighbour's sea is compared from the stack frame (0xe), so a neighbour without sea compares
		/// the last one found, like in the original. With the marker the intro story goes on after each column; the edge
		/// columns are then linked around the map (0xe0, 0x0e) except at the corners.
		/// </summary>
		public void ReckonContinentSizes(ushort marker)
		{
			this.processor.Journal.OpenScope($"ReckonContinentSizes({marker})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x18);
			this.processor.PreserveIndexRegisters();

			this.owner.Libc.FillBytes(0x7f38, 0, 0x104);

			short column;

			for (column = 0; column < 0x14; column++)
			{
				this.processor.EmitLocalWord(0x14, (ushort)column);

				short row;

				for (row = 0; row < 0xc; row++)
				{
					this.processor.EmitLocalWord(0x18, (ushort)row);

					LinkSeaBlock(column, row);
				}

				this.processor.EmitLocalWord(0x18, (ushort)row);

				if (marker != 0)
				{
					DisplayStoryIntro();
				}
			}

			this.processor.EmitLocalWord(0x14, (ushort)column);

			for (ushort row = 0; row < 0xc; row++)
			{
				this.processor.EmitGlobalByte((ushort)(row + 0x7f38), (byte)(this.processor.PullGlobalByte((ushort)(row + 0x7f38)) | 0xe0));
				this.processor.EmitGlobalByte((ushort)(row + 0x802f), (byte)(this.processor.PullGlobalByte((ushort)(row + 0x802f)) | 0xe));
			}

			this.processor.EmitLocalWord(0x18, 0xc);

			this.processor.EmitGlobalByte(0x7f38, (byte)(this.processor.PullGlobalByte(0x7f38) & 0x7f));
			this.processor.EmitGlobalByte(0x7f43, (byte)(this.processor.PullGlobalByte(0x7f43) & 0xdf));
			this.processor.EmitGlobalByte(0x802f, (byte)(this.processor.PullGlobalByte(0x802f) & 0xfd));
			this.processor.EmitGlobalByte(0x803a, (byte)(this.processor.PullGlobalByte(0x803a) & 0xf7));

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ReckonContinentSizes");
		}

		/// <summary>The sea links of one block (see ReckonContinentSizes); the locals of the frame are written as in the original</summary>
		private void LinkSeaBlock(short column, short row)
		{
			short horizLoc = (short)((column << 2) + 1), vertLoc = (short)((row << 2) + 1);

			this.processor.EmitLocalWord(0x8, (ushort)horizLoc);
			this.processor.EmitLocalWord(0xc, (ushort)vertLoc);
			this.processor.EmitLocalWord(0x16, 0xffff);

			if (FindBlockSea(horizLoc, vertLoc) is ushort sea)
				this.processor.EmitLocalWord(0x16, sea);

			if (this.processor.PullLocalWord(0x16) == 0xffff)
				return;

			short direction;

			for (direction = 1; direction <= 8; direction++)
			{
				this.processor.EmitLocalWord(0xa, (ushort)direction);

				ushort offset = (ushort)(direction << 1);
				short nextHoriz = (short)((this.processor.PullGlobalWord((ushort)(offset + 0x1882)) << 2) + horizLoc);
				short nextVert = (short)((this.processor.PullGlobalWord((ushort)(offset + 0x18e4)) << 2) + vertLoc);

				this.processor.EmitLocalWord(0x2, (ushort)nextHoriz);
				this.processor.EmitLocalWord(0x4, (ushort)nextVert);

				// A neighbour without sea leaves 0xe as it was
				if (FindBlockSea(nextHoriz, nextVert) is ushort nextSea)
					this.processor.EmitLocalWord(0xe, nextSea);

				if (this.processor.PullLocalWord(0x16) != this.processor.PullLocalWord(0xe))
					continue;

				if (!SeaPassageOpen(horizLoc, vertLoc, offset) && (row != 0xb || direction != 3))
					continue;

				short nextRow = (short)(this.processor.PullGlobalWord((ushort)(offset + 0x18e4)) + row);

				if (nextRow >= 0xc)
					continue;

				LinkSeaBlockBit(column, row, direction - 1);

				short nextColumn = (short)(this.processor.PullGlobalWord((ushort)(offset + 0x1882)) + column);

				this.processor.EmitLocalWord(0x2, (ushort)nextColumn);
				this.processor.EmitLocalWord(0x4, (ushort)nextRow);

				if (nextColumn < 0 || nextColumn >= 0x14 || nextRow < 0 || nextRow > 0xc)
					continue;

				LinkSeaBlockBit(nextColumn, nextRow, (direction + 3) & 7);
			}

			this.processor.EmitLocalWord(0xa, (ushort)direction);
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

				ObtainContinentIDFromScreen(tileHoriz, tileVert);

				return this.processor.RegAcc.Value16;
			}

			return null;
		}

		/// <summary>
		/// Whether a ship can pass from the block's corner 5 tiles in the direction: at each step at least 2 of the 4 tiles
		/// (x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1) are ocean inside the map (the locals 0x2 - 0x6, 0x10, 0x12 as in the
		/// original)
		/// </summary>
		private bool SeaPassageOpen(short horizLoc, short vertLoc, ushort offset)
		{
			bool open = true;
			short step;

			this.processor.EmitLocalWord(0x2, (ushort)horizLoc);
			this.processor.EmitLocalWord(0x4, (ushort)vertLoc);

			for (step = 0; step <= 4; step++)
			{
				this.processor.EmitLocalWord(0x12, (ushort)step);

				horizLoc = (short)(horizLoc + this.processor.PullGlobalWord((ushort)(offset + 0x1882)));
				vertLoc = (short)(vertLoc + this.processor.PullGlobalWord((ushort)(offset + 0x18e4)));

				this.processor.EmitLocalWord(0x2, (ushort)horizLoc);
				this.processor.EmitLocalWord(0x4, (ushort)vertLoc);

				short ocean = 4;

				foreach ((short tileHoriz, short tileVert) in new[] { (horizLoc, vertLoc), ((short)(horizLoc + 1), vertLoc),
					(horizLoc, (short)(vertLoc + 1)), ((short)(horizLoc + 1), (short)(vertLoc + 1)) })
				{
					if (ObtainTerrainTypeFromScreen(tileHoriz, tileVert) != 0xa || this.owner.MapOps.VerifyMapLimits(tileHoriz, tileVert) == 0)
						ocean--;
				}

				this.processor.EmitLocalWord(0x6, (ushort)ocean);

				if (ocean < 2)
				{
					open = false;
					break;
				}
			}

			if (open)
				this.processor.EmitLocalWord(0x12, (ushort)step);

			this.processor.EmitLocalWord(0x10, open ? (ushort)1 : (ushort)0);

			return open;
		}

		/// <summary>Sets bit bitIdx of the block's sea links (0x7f38 + 13 * column + row)</summary>
		private void LinkSeaBlockBit(short column, short row, int bitIdx)
		{
			ushort links = (ushort)((0xd * column) + row + 0x7f38);

			this.processor.EmitGlobalByte(links, (byte)(this.processor.PullGlobalByte(links) | (1 << bitIdx)));
		}

		/// <summary>
		/// Terrain type of the pixel of the current screen (the screen of the box at 0xaa), by the terrain colour table at 0x2ba6
		/// </summary>
		/// <returns>Terrain type, also in AX</returns>
		public ushort ObtainTerrainTypeFromScreen(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTerrainTypeFromScreen({horizLoc}, {vertLoc})");

			ushort displayIdx = this.processor.PullGlobalWord(this.processor.PullGlobalWord(0xaa));
			ushort colour = this.owner.VideoCard.ObtainDot(displayIdx, horizLoc, vertLoc);
			ushort terrain = this.processor.PullGlobalWord((ushort)(0x2ba6 + (colour << 1)));

			this.processor.RegAcc.Value16 = terrain;

			this.processor.Journal.CloseScope("ObtainTerrainTypeFromScreen");

			return terrain;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ObtainContinentIDFromScreen(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainContinentIDFromScreen({horizLoc}, {vertLoc})");

			// function body
			this.owner.VideoCard.ObtainDot(
				this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.PullGlobalWord(0xaa)),
				horizLoc, vertLoc + 50);

			// Far return
			this.processor.Journal.CloseScope("ObtainContinentIDFromScreen");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// One step of the story told while the new world is made (called again and again by the map generator); the stage
		/// 0x3b62: 0 = not started (opens story.txt, handle 0x6804), 1 = telling, 2 - 4 = told (the line under the picture is
		/// wiped at 5), 5 = "press a key" (0x3b28) blinking with the clock (0x5c / 60). While telling, a step waits until the
		/// music (0x6800, song position from the second line on) reaches the time of the line (0x6806), then shows the next
		/// line of story.txt; a short line (at most 3 characters) instead shows the next picture birthN.pic (0x3b64) and,
		/// with VGA colours (0xd762), its palette and colour cycles (records of first colour, last colour and speed in
		/// 0x3710:0x3b66, cycle count 0x3b68). The end of the file or a key (when 0x67fc allows it) ends the story.
		/// The screen is marked as changed ([0xaa] = 1) and AX is 1 while waiting for the music, else 0.
		/// </summary>
		public void DisplayStoryIntro()
		{
			this.processor.Journal.OpenScope("DisplayStoryIntro()");

			// The frame keeps the picture file name (bp - 0x10) like the original
			this.processor.EnterFrame(0x1a);
			ushort fileName = (ushort)(this.processor.RegFrame.Value16 - 0x10);

			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.PullGlobalWord(0xaa), 0x0);

			bool waiting = StoryStep(fileName);

			this.processor.RegAcc.Value16 = (ushort)(waiting ? 1 : 0);

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayStoryIntro");
		}

		/// <summary>
		/// The step of DisplayStoryIntro, true while waiting for the music
		/// </summary>
		private bool StoryStep(ushort fileName)
		{
			if ((short)this.processor.PullGlobalWord(0x3b62) >= 2 && this.owner.Libc.KeyAvailable() == 0)
			{
				// The story is told: the "press a key" line
				if ((short)this.processor.PullGlobalWord(0x3b62) < 5)
				{
					this.processor.EmitGlobalWord(0x3b62, (ushort)(this.processor.PullGlobalWord(0x3b62) + 1));

					if (this.processor.PullGlobalWord(0x3b62) == 5)
					{
						this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 40, 160, 240, 8, 0);
					}
				}
				else
				{
					this.processor.EmitGlobalWord(0x6800, (ushort)CPU.SignedQuotient(this.processor.PullGlobalSignedWord(0x5c), 0x3c));

					ushort colour = (ushort)((this.processor.PullGlobalByte(0x6800) & 1) != 0 ? 0xf : 0x3);

					this.owner.TextPainter.RenderCenteredTextToScreen0(0x3b28, 160, 160, colour);
				}

				MarkScreenChanged();

				return false;
			}

			if (this.processor.PullGlobalWord(0x3b62) == 0)
			{
				this.processor.EmitGlobalWord(0x6804, (ushort)this.owner.Libc.OpenStream("story.txt", "rt"));
				this.processor.EmitGlobalWord(0x3b62, 0x1);
				this.processor.EmitGlobalWord(0x6806, 0);
				this.processor.EmitGlobalWord(0x6800, 0);

				this.owner.Libc.CopyText(0xba06, "");

				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x7);
				this.processor.EmitGlobalWord(0x6802, 0x0);
			}

			if (this.processor.PullGlobalWord(0x6802) != 0)
			{
				this.processor.EmitGlobalWord(0x6800, this.owner.Services.ObtainSongPosition());
			}

			if (this.processor.PullGlobalSignedWord(0x6800) < this.processor.PullGlobalSignedWord(0x6806))
			{
				MarkScreenChanged();

				return true;
			}

			if (this.processor.PullGlobalWord(0x3b62) != 2 && this.processor.PullGlobalSignedWord(0x6802) < 0x28)
			{
				this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 160, 3);

				this.owner.TextPainter.AwaitTime(5);
			}

			short lineRead = this.owner.Libc.ScanStream((short)this.processor.PullGlobalWord(0x6804), "%[^\n]\n", 0xba06);

			if (lineRead == -1 || (this.owner.Libc.KeyAvailable() != 0 && this.processor.PullGlobalWord(0x67fc) != 0))
			{
				EndStory(lineRead == -1);
			}
			else
			{
				TellStoryLine(fileName);
			}

			this.owner.Services.DisplayScreen(0);

			MarkScreenChanged();

			return this.processor.PullGlobalWord(0x3b62) != 2;
		}

		/// <summary>
		/// The line just read from story.txt (0xba06): shown under the picture, or the next picture for a short line; then the
		/// time of the next line (0x6806) from the music table 0x3710:0x83
		/// </summary>
		private void TellStoryLine(ushort fileName)
		{
			if (this.processor.PullGlobalSignedWord(0x3b64) > 1)
			{
				// The line under the picture is wiped with the picture
				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), 0, 0xa0, 0x140, 8,
					this.processor.PullGlobalWord(0xaa), 0, 0xa0);
			}

			this.owner.TextPainter.AwaitTime(10);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 160, 3);

			this.owner.TextPainter.AwaitTime(5);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 160, 11);

			this.processor.EmitGlobalWord(0x6806, 0x0);

			if ((short)this.owner.Libc.TextLength(0xba06) > 3)
			{
				if (this.processor.PullGlobalWord(0x6802) == 0)
				{
					// The first line starts the music
					this.owner.Services.RestoreTimer();

					this.owner.Services.PerformSound(1, 0);

					this.owner.Services.PerformSound(4, 0);
				}

				this.processor.EmitGlobalWord(0x6806, (ushort)(this.processor.PullGlobalWord(0x6806) + 2));
				this.processor.EmitGlobalWord(0x6802, (ushort)(this.processor.PullGlobalWord(0x6802) + 1));
			}
			else
			{
				ShowStoryPicture(fileName);
			}

			if ((short)this.owner.Libc.TextLength(0xba06) > 0x13)
			{
				this.processor.EmitGlobalWord(0x6806, (ushort)(this.processor.PullGlobalWord(0x6806) + 1));
			}

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(0x3710, (ushort)((this.processor.PullGlobalWord(0x6802) << 1) + 0x83)),
				(45 * this.processor.PullGlobalSignedWord(0x6806)) + this.processor.PullGlobalSignedWord(0x6800),
				32767);

			this.processor.EmitGlobalWord(0x6806, this.processor.RegAcc.Value16);
		}

		/// <summary>
		/// The next picture birthN.pic (N = 0x3b64), with VGA colours faded in with its palette birthN.pal and its colour cycles
		/// </summary>
		private void ShowStoryPicture(ushort fileName)
		{
			this.owner.Libc.CopyText(fileName, "birth0.pic");

			// The digit of the name (bp - 0xb)
			byte pictureIdx = (byte)this.processor.PullGlobalWord(0x3b64);
			this.processor.EmitGlobalWord(0x3b64, (ushort)(this.processor.PullGlobalWord(0x3b64) + 1));
			this.processor.EmitGlobalByte((ushort)(fileName + 5), (byte)(this.processor.PullGlobalByte((ushort)(fileName + 5)) + pictureIdx));

			bool colours = this.processor.PullGlobalWord(0xd762) != 0;

			if (colours)
			{
				StopStoryColourCycles();
			}

			this.owner.ImageToolkit.ImportPictureOrColors(2, 0, 0, fileName, 0);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.FloodColors(8, 0, 0, 0);
			}

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), 0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa), 0, 0);

			if (this.processor.PullGlobalWord(0xd762) == 0)
				return;

			this.owner.Libc.CopyText((ushort)(fileName + 7), "pal");

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, fileName, 0xc5be);

			this.owner.Services.ImportColorsFromBuffer(8, 0xc5be);

			// The colour cycles of the picture: first colour, last colour, speed (300 / speed beats a step), 0 at the end
			this.processor.EmitGlobalWord(0x3b68, 0x0);

			while (this.processor.PullUInt8(0x3710, this.processor.PullGlobalWord(0x3b66)) != 0)
			{
				ushort first = NextStoryCycleByte();
				ushort last = NextStoryCycleByte();
				ushort speed = NextStoryCycleByte();

				if (speed == 0)
					throw new Exception("Division by zero");

				ushort delay = (ushort)(0x12c / speed);

				this.processor.EmitGlobalWord(0x3b68, (ushort)(this.processor.PullGlobalWord(0x3b68) + 1));

				this.owner.Services.AssignHueCycleSpan(this.processor.PullGlobalWord(0x3b68), delay, first, last);
			}

			this.processor.EmitGlobalWord(0x3b66, (ushort)(this.processor.PullGlobalWord(0x3b66) + 1));

			for (short cycleIdx = 1; cycleIdx <= this.processor.PullGlobalSignedWord(0x3b68); cycleIdx++)
			{
				this.owner.Services.ActivateHueCycle((ushort)cycleIdx);
			}
		}

		/// <summary>The next byte of the colour cycle records of the story pictures (0x3710:0x3b66)</summary>
		private ushort NextStoryCycleByte()
		{
			ushort spot = this.processor.PullGlobalWord(0x3b66);
			this.processor.EmitGlobalWord(0x3b66, (ushort)(this.processor.PullGlobalWord(0x3b66) + 1));

			return this.processor.PullUInt8(0x3710, spot);
		}

		/// <summary>
		/// The end of the story (the end of story.txt, then a pause of 180, or a key): closes the file, stops the music and the
		/// colour cycles, stage 2
		/// </summary>
		private void EndStory(bool endOfFile)
		{
			if (endOfFile)
			{
				this.owner.TextPainter.AwaitTime(180);
			}

			this.owner.Libc.CloseStream((short)this.processor.PullGlobalWord(0x6804));

			this.owner.Services.PerformSound(1, 0);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				StopStoryColourCycles();
			}

			this.processor.EmitGlobalWord(0x3b62, 0x2);
		}

		/// <summary>Stops the colour cycles 1 - 0x3b68 of the story picture</summary>
		private void StopStoryColourCycles()
		{
			for (short cycleIdx = 1; cycleIdx <= this.processor.PullGlobalSignedWord(0x3b68); cycleIdx++)
			{
				this.owner.Services.DeactivateHueCycle((ushort)cycleIdx);
			}
		}

		/// <summary>Marks the screen as changed ([0xaa] = 1)</summary>
		private void MarkScreenChanged()
		{
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.PullGlobalWord(0xaa), 0x1);
		}

		/// <summary>
		/// Global warming (worldHeatingTally times so far): after the newspaper, changes the land tiles of terrain 0 - 3 that have
		/// at least 7 - worldHeatingTally ocean tiles around them (map layer 2 colour 11 for terrain 3, else 3; improvement bits 6
		/// discarded) and, of the others, those where (11 * column + 13 * row) mod 8 is worldHeatingTally
		/// (colour 14 for terrain 0 - 1, else 6). A changed tile becomes visible to player 0 and is repainted when the human
		/// player sees it.
		/// </summary>
		public void EnactGlobalWarming(short worldHeatingTally)
		{
			this.processor.Journal.OpenScope($"EnactGlobalWarming({worldHeatingTally})");

			this.owner.Libc.CopyText(0xba06, "Global temperature\nrises! Icecaps melt.\nSevere Drought.\n");

			this.owner.Paper.DisplayNewspaper(-1);

			MapLayers chart = this.owner.MapOps.Chart;

			for (int horizLoc = 0; horizLoc < 80; horizLoc++)
			{
				for (int vertLoc = 0; vertLoc < 50; vertLoc++)
				{
					ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

					if ((short)terrain > 3)
						continue;

					short oceanTally = 0;

					for (int heading = 1; heading <= 8; heading++)
					{
						if (this.owner.MapOps.ObtainTerrainType(chart.DirectionHoriz(heading) + horizLoc, chart.DirectionVert(heading) + vertLoc) == MapRules.OceanTerrain)
						{
							oceanTally++;
						}
					}

					if (7 - worldHeatingTally <= oceanTally)
					{
						this.owner.World.ChartSight[horizLoc, vertLoc] |= 1;
						this.owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)(terrain != 3 ? 3 : 11));
						this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
					}
					else if ((((11 * horizLoc) + (13 * vertLoc)) & 7) == (ushort)worldHeatingTally)
					{
						this.owner.World.ChartSight[horizLoc, vertLoc] |= 1;
						this.owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)((short)terrain <= 1 ? 14 : 6));
					}
					else
					{
						continue;
					}

					if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (1 << this.owner.World.PersonParticipantIdx)) != 0)
					{
						this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
					}
				}
			}

			this.processor.Journal.CloseScope("EnactGlobalWarming");
		}
	}
}
