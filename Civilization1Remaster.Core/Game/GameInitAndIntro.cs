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
		/// ?
		/// </summary>
		public void ProduceElevation()
		{
			this.processor.Journal.OpenScope($"ProduceElevation()");

			// function body
			this.processor.EnterFrame(0x18);
			this.processor.PreserveSource();
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

		L066b:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0xa0, 0x64,
				this.processor.PullGlobalWord(0xaa),
				0xa0, 0);

			this.processor.EmitLocalWord(0x16, 0x0);

		L0693:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(80));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(50));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsNotEqual) goto L0693;
			this.processor.CopyLocalWord(0x6, 0x8);
			this.processor.CopyLocalWord(0xa, 0xe);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(4));

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

		L06e6:
			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				9);

			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.EmitLocalWord(0xc, 0x1);

		L0706:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x6),
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0xa));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0731;
			this.processor.EmitLocalWord(0x4, 0x1);

		L0731:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x2));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0706;
			this.processor.CopyLocalWord(0x18, 0x10);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x16);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x18));
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10);
			this.processor.RegAcc.LowByte = this.processor.BitXorByte(this.processor.RegAcc.LowByte, 0x4);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x18));
			if (this.processor.FlagSet.IsLessOrEqual) goto L0784;

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa) + 0x32,
				8);

		L0784:
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x18) << 1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1884));
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e6));
			this.processor.EmitLocalWord(0xa, this.processor.SumWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16));

			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07c7;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd);
			if (this.processor.FlagSet.IsEqual) goto L07c7;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L07c7;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9);
			if (this.processor.FlagSet.IsEqual) goto L07c7;
			goto L06e6;

		L07c7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07d3;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L07d9;

		L07d3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L07fe;

		L07d9:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0xa0, 0, 0xa0, 0x64,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			goto L0853;

		L07fe:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));
			this.processor.EmitLocalWord(0xc, 0x1);

		L0806:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe));
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.VideoCard.ObtainDot(1,
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L084a;

			this.owner.Services.AssignDot(1, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0xa),
				11);

		L084a:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0806;

		L0853:
			DisplayStoryIntro();

			this.processor.IncrementLocalWord(0x14);
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x100);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0875;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7ef6);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x7efa));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x12));
			if (this.processor.FlagSet.IsLessOrEqual) goto L0875;
			goto L066b;

		L0875:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0x32, 0x50, 0x32,
				this.processor.PullGlobalWord(0xaa),
				0, 0x96);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 50, 80, 50, 0);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
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
		/// ?
		/// </summary>
		public void ProduceTerrain()
		{
			this.processor.Journal.OpenScope("ProduceTerrain()");

			// function body
			this.processor.EnterFrame(0xd4);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitLocalWord(0x24, 0x0);

		L0a41:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), 0x0);
			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xc);
			if (this.processor.FlagSet.IsLess) goto L0a41;
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L0de2;

		L0a5c:
			this.processor.EmitLocalWord(0xa8, this.processor.IncrementWord(this.processor.PullLocalWord(0xa8)));

		L0a60:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa8), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a7f;

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x24),
				this.processor.PullLocalWord(0xa8)] = 0;
			goto L0a5c;

		L0a7f:
			this.processor.EmitLocalWord(0x24, this.processor.IncrementWord(this.processor.PullLocalWord(0x24)));

		L0a82:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a90;
			this.processor.EmitLocalWord(0xa8, 0x0);
			goto L0a60;

		L0a90:
			this.processor.EmitLocalWord(0xae, 0x0);
			this.processor.EmitLocalWord(0xa8, 0x0);
			goto L0c46;

		L0a9f:
			this.processor.RegAcc.Value16 = 0;

		L0aa1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0ab0;
			goto L0bfc;

		L0ab0:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa8), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L0aba;
			goto L0b9a;

		L0aba:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ac5;
			this.processor.RegAcc.Value16 = 0xffff;
			goto L0ac7;

		L0ac5:
			this.processor.RegAcc.Value16 = 0;

		L0ac7:
			this.processor.EmitLocalWord(0xac, this.processor.RegAcc.Value16);
			goto L0b8b;

		L0ace:
			this.processor.RegAcc.Value16 = 0;

		L0ad0:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xac));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0ad9;
			goto L0b9a;

		L0ad9:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xac));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa8);
			if (this.processor.RegBase.Value16 > 0)
			{
				this.processor.RegBase.Value16--;
			}
			else
			{
				this.processor.RegAcc.Value16--;
				this.processor.RegBase.Value16 = 49;
			}
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xd0, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0b00;
			goto L0b87;

		L0b00:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0b7c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0b7c;
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L0b6c;

		L0b12:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));

		L0b15:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b69;
			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2),
				this.processor.PullLocalWord(0x4)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x22));
			if (this.processor.FlagSet.IsNotEqual) goto L0b45;
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xd0);
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x2),
				this.processor.PullLocalWord(0x4)] = (sbyte)this.processor.RegAcc.LowByte;

		L0b45:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.LowByte = 0x7;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0xdcfe);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xd0);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe),
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe)), this.processor.RegAcc.Value16));
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16, 0x0);
			goto L0b12;

		L0b69:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L0b6c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0b7c;
			this.processor.EmitLocalWord(0x2, 0x0);
			goto L0b15;

		L0b7c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd0);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

			DisplayStoryIntro();

		L0b87:
			this.processor.EmitLocalWord(0xac, this.processor.IncrementWord(this.processor.PullLocalWord(0xac)));

		L0b8b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x4f);
			if (this.processor.FlagSet.IsLess) goto L0b94;
			goto L0ace;

		L0b94:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0ad0;

		L0b9a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x22), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0bcb;
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0bc4;
			this.processor.EmitLocalWord(0xae, 0x0);

		L0bac:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.LowByte = 0x7;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.IncrementLocalWord(0xae);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xae);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0bac;

		L0bc4:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xae);
			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);

		L0bcb:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x22);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

			this.owner.World.ChartSight[this.processor.PullLocalWord(0x24),
				this.processor.PullLocalWord(0xa8)] = (sbyte)this.processor.RegCount.LowByte;

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.LowByte = 0x7;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x22);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe))));
			this.processor.EmitLocalWord(0x6, 0x1);
			goto L0c06;

		L0bfc:
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0x22, 0xffff);

		L0c06:
			this.processor.EmitLocalWord(0x24, this.processor.IncrementWord(this.processor.PullLocalWord(0x24)));

		L0c09:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c3e;

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0xa8));
			
			this.processor.EmitLocalWord(0xaa, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0c2e;
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20))));

		L0c2e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xaa), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0c38;
			goto L0a9f;

		L0c38:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0aa1;

		L0c3e:
			DisplayStoryIntro();

			this.processor.IncrementLocalWord(0xa8);

		L0c46:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa8), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0c5e;
			this.processor.EmitLocalWord(0x22, 0xffff);
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0x24, 0x0);
			goto L0c09;

		L0c5e:
			this.processor.EmitLocalWord(0x24, 0x0);

		L0c63:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xce), 0x0);
			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x10);
			if (this.processor.FlagSet.IsLess) goto L0c63;
			this.processor.EmitLocalWord(0xa4, 0x0);
			this.processor.EmitLocalWord(0x24, 0x1);
			goto L0cc9;

		L0c84:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xac);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xd0));
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xac);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0xa4), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xd0));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xce), this.processor.RegAcc.Value16);
			this.processor.DecrementLocalWord(0xac);

		L0ca6:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xac), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0c84;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa4), this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xce), this.processor.RegAcc.Value16);

		L0cc6:
			this.processor.EmitLocalWord(0x24, this.processor.IncrementWord(this.processor.PullLocalWord(0x24)));

		L0cc9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x40);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d17;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa4), 0xf);
			this.processor.EmitLocalWord(0xa8, 0x1);
			goto L0ce6;

		L0ce2:
			this.processor.EmitLocalWord(0xa8, this.processor.IncrementWord(this.processor.PullLocalWord(0xa8)));

		L0ce6:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa8), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0cc6;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.LowByte = 0x7;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe));
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xa8);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0xce));
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0ce2;
			this.processor.EmitLocalWord(0xac, 0xf);
			goto L0ca6;

		L0d17:
			this.processor.EmitLocalWord(0xa4, 0x0);
			this.processor.EmitLocalWord(0x24, 0x0);
			goto L0d74;

		L0d24:
			this.processor.EmitLocalWord(0xa8, this.processor.IncrementWord(this.processor.PullLocalWord(0xa8)));

		L0d28:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa8), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d71;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x24),
				this.processor.PullLocalWord(0xa8)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa4));
			this.processor.EmitLocalWord(0xa6, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0d24;

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0xa8) + 0x32,
				this.processor.RegAcc.Value16);

			goto L0d24;

		L0d71:
			this.processor.EmitLocalWord(0x24, this.processor.IncrementWord(this.processor.PullLocalWord(0x24)));

		L0d74:
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d82;
			this.processor.EmitLocalWord(0xa8, 0x0);
			goto L0d28;

		L0d82:
			this.processor.EmitLocalWord(0x24, 0x1);

		L0d87:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, 0xce);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.LowByte = 0x7;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xdcfe));
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xf);
			if (this.processor.FlagSet.IsLess) goto L0d87;
			this.processor.EmitLocalWord(0x24, 0x1);

		L0db1:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xce));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegCount.LowByte = 0x7;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xdcfe), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0xf);
			if (this.processor.FlagSet.IsLess) goto L0db1;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xdcfe), 0x0);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xdd1c), 0x1);
			this.processor.IncrementLocalWord(0x8);

		L0de2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0df0;
			this.processor.EmitLocalWord(0x24, 0x0);
			goto L0a82;

		L0df0:
			DisplayStoryIntro();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19e8),
				0, 0);

			LabelLandmasses();

			ReckonContinentSizes(1);

			DisplayStoryIntro();

			SituateSpecialTerrain();

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 0, 79, 0, 15);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 49, 79, 49, 15);

			this.processor.EmitLocalWord(0x24, 0x0);

		L0e75:
			this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 0, 7);

			this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 1, 7);

			this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 48, 7);

			this.owner.Services.AssignDot(this.owner.Libc.Dice.DrawInt(80), 49, 7);

			this.processor.IncrementLocalWord(0x24);
			this.processor.CompareWord(this.processor.PullLocalWord(0x24), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f04;
			goto L0e75;

		L0f04:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ProduceTerrain");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void SituateSpecialTerrain()
		{
			this.processor.Journal.OpenScope("SituateSpecialTerrain()");

			// function body
			this.processor.EnterFrame(0x42);
			this.processor.PreserveIndexRegisters();
			
			this.processor.EmitLocalWord(0x40, 0x0);

		L0f17:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			
			this.owner.World.Landmasses[this.processor.PullLocalWord(0x40)].ConstructionSpotTally = 0;

			this.processor.IncrementLocalWord(0x40);

			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x10);
			if (this.processor.FlagSet.IsLess) goto L0f17;

			this.processor.EmitLocalWord(0x40, 0x0);
			goto L0f54;

		L0f32:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a));
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f51;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);

		L0f4e:
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x38), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x38)), this.processor.RegAcc.Value16));

		L0f51:
			this.processor.EmitLocalWord(0x40, this.processor.IncrementWord(this.processor.PullLocalWord(0x40)));

		L0f54:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x18);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0fb8;
			this.processor.ScaleIndexToSource(0x13, this.processor.PullLocalWord(0x40));
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegFrame.Value16);
			this.processor.RegDest.Value16 = this.processor.SubtractWord(this.processor.RegDest.Value16, 0x38);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x292));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x3;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x290)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0xc;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x2, this.processor.RegData.Value16);
			this.processor.CompareWord(this.processor.RegData.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0f9c;
			this.processor.CompareWord(this.processor.RegData.Value16, 0xb);
			if (this.processor.FlagSet.IsEqual) goto L0f9c;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x291));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16), this.processor.RegAcc.Value16));

		L0f9c:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e));
			this.processor.RegSource.Value16 = this.processor.BitOrWord(this.processor.RegSource.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f32;
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			goto L0f4e;

		L0fb8:
			this.processor.EmitLocalWord(0x3c, 0x2);
			goto L1174;

		L0fc0:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x50);

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x3c) + 80,
				this.processor.PullLocalSignedWord(0x3e), 0);

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x3c) + 80,
				this.processor.PullLocalSignedWord(0x3e) + 0x32, 0);

		L0ff4:
			this.processor.EmitLocalWord(0x3e, this.processor.IncrementWord(this.processor.PullLocalWord(0x3e)));

		L0ff7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0x30);
			if (this.processor.FlagSet.IsLess) goto L1000;
			goto L1171;

		L1000:
			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x3c),
				this.processor.PullLocalSignedWord(0x3e));
			
			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xb);
			if (this.processor.FlagSet.IsEqual) goto L101f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L101f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0fc0;

		L101f:
			this.processor.EmitLocalWord(0x6, 0x0);
			this.processor.EmitLocalWord(0x40, 0x0);

		L1029:
			this.processor.EmitLocalWord(0x4, 0x0);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x40);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.TownDeltas[this.processor.PullLocalWord(0x40)].X +
				this.processor.PullLocalSignedWord(0x3c)));
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.TownDeltas[this.processor.PullLocalWord(0x40)].Y +
				this.processor.PullLocalSignedWord(0x3e)));
			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegAcc.Value16);
			
			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L105f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L1078;

		L105f:
			this.processor.RegAcc.Value16 = 0x7;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x8));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xb, this.processor.PullLocalWord(0x3a));
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.RegCount.LowByte, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L1078;
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0x2));

		L1078:
			this.owner.MapOps.ContainsSpecialResource(
				this.processor.PullLocalSignedWord(0x8),
				this.processor.PullLocalSignedWord(0x3a));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1091;
			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0xc));

		L1091:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x42);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x38));
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x9);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L10a8;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));

		L10a8:
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L10b4;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));

		L10b4:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x6, this.processor.SumWord(this.processor.PullLocalWord(0x6), this.processor.RegAcc.Value16));
			this.processor.IncrementLocalWord(0x40);
			this.processor.CompareWord(this.processor.PullLocalWord(0x40), 0x14);
			if (this.processor.FlagSet.IsGreater) goto L10c6;
			goto L1029;

		L10c6:
			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x3c),
				this.processor.PullLocalSignedWord(0x3e));
			
			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L10f4;
			this.processor.ScaleIndex(0x7, this.processor.PullLocalWord(0x3c));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xb, this.processor.PullLocalWord(0x3e));
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, this.processor.RegAcc.LowByte);
			this.processor.TestBitsByte(this.processor.RegCount.LowByte, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L10f4;
			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L10f4:
			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullLocalSignedWord(0x6) - 120) / 8,
				1, 15);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x50);

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x3c) + 80,
				this.processor.PullLocalSignedWord(0x3e), this.processor.RegAcc.Value16);

			this.owner.Services.AssignDot(this.processor.PullLocalSignedWord(0x3c) + 80,
				this.processor.PullLocalSignedWord(0x3e) + 0x32,
				this.processor.PullLocalWord(0x4));

			ObtainContinentIDFromScreen(
				this.processor.PullLocalSignedWord(0x3c),
				this.processor.PullLocalSignedWord(0x3e));

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.World.Landmasses[this.processor.RegAcc.Value16].ConstructionSpotTally++;

			goto L0ff4;

		L1171:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L1174:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x4e);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1182;
			this.processor.EmitLocalWord(0x3e, 0x2);
			goto L0ff7;

		L1182:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("SituateSpecialTerrain");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void LabelLandmasses()
		{
			this.processor.Journal.OpenScope("LabelLandmasses()");

			// function body
			this.processor.EnterFrame(0x26);
			this.processor.PreserveIndexRegisters();

			this.owner.Libc.FillBytes(0xdb44, 0, 0x104);

			this.processor.EmitLocalWord(0x14, 0x0);
			goto L142c;

		L11ab:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0xc));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L11d4;

			this.owner.MapOps.ObtainContinentID(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x20, this.processor.RegSource.Value16);
			goto L125a;

		L11d4:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegSource.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1262;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegSource.Value16);

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x8, 0x20);
			this.processor.EmitLocalWord(0x24, this.processor.RegSource.Value16);

		L1200:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L1209;
			goto L128e;

		L1209:
			this.processor.EmitLocalWord(0x1a, this.processor.IncrementWord(this.processor.PullLocalWord(0x1a)));

		L120c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1a), 0xd);
			if (this.processor.FlagSet.IsLess) goto L1215;
			goto L1429;

		L1215:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x18, 0xffff);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegAcc.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1243;
			goto L11ab;

		L1243:
			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x8),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x8, 0x20);

		L125a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			goto L1200;

		L1262:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			ObtainTerrainTypeFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1200;

			this.owner.MapOps.ObtainContinentID(
				(short)this.processor.RegSource.Value16,
				(short)this.processor.RegDest.Value16);

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x20, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x24, this.processor.RegDest.Value16);
			goto L1200;

		L128e:
			this.processor.EmitLocalWord(0xa, 0x1);
			goto L12fb;

		L1295:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x4));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L12be;

			this.owner.MapOps.ObtainContinentID(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x4));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x16, this.processor.RegSource.Value16);
			goto L1354;

		L12be:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegSource.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L12d5;
			goto L135c;

		L12d5:
			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegSource.Value16);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x2, 0x16);
			this.processor.EmitLocalWord(0x1c, this.processor.RegSource.Value16);

		L12ed:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L12f8;
			goto L138b;

		L12f8:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L12fb:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1304;
			goto L1209;

		L1304:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe, 0xffff);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegAcc.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L133d;
			goto L1295;

		L133d:
			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x2, 0x16);

		L1354:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			goto L12ed;

		L135c:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			ObtainTerrainTypeFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1375;
			goto L12ed;

		L1375:
			this.owner.MapOps.ObtainContinentID(
				(short)this.processor.RegSource.Value16,
				(short)this.processor.RegDest.Value16);

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x16, this.processor.RegSource.Value16);
			this.processor.EmitLocalWord(0x1c, this.processor.RegDest.Value16);
			goto L12ed;

		L138b:
			this.owner.Routing.ComputeRouteCost(
				this.processor.PullLocalSignedWord(0x20),
				this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0x16),
				this.processor.PullLocalSignedWord(0x1c),
				0, 20);

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L13b1;
			goto L12f8;

		L13b1:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x14);
			if (this.processor.FlagSet.IsLess) goto L13b9;
			goto L12f8;

		L13b9:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x14));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xa);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xdb44),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xdb44)), this.processor.RegAcc.LowByte));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L13f2;
			goto L12f8;

		L13f2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x14);
			if (this.processor.FlagSet.IsLess) goto L13fb;
			goto L12f8;

		L13fb:
			this.processor.RegAcc.Value16 = this.processor.BitOrWord(this.processor.RegAcc.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1402;
			goto L12f8;

		L1402:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd);
			if (this.processor.FlagSet.IsLess) goto L140a;
			goto L12f8;

		L140a:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x2));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xa);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x3);
			this.processor.RegCount.LowByte = this.processor.BitAndByte(this.processor.RegCount.LowByte, 0x7);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xdb44),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegDest.Value16 + 0xdb44)), this.processor.RegAcc.LowByte));
			goto L12f8;

		L1429:
			this.processor.EmitLocalWord(0x14, this.processor.IncrementWord(this.processor.PullLocalWord(0x14)));

		L142c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L143a;
			this.processor.EmitLocalWord(0x1a, 0x0);
			goto L120c;

		L143a:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("LabelLandmasses");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="flag"></param>
		public void ReckonContinentSizes(ushort marker)
		{
			this.processor.Journal.OpenScope($"ReckonContinentSizes({marker})");

			// function body
			this.processor.EnterFrame(0x18);
			this.processor.PreserveIndexRegisters();

			this.owner.Libc.FillBytes(0x7f38, 0, 0x104);

			this.processor.EmitLocalWord(0x14, 0x0);
			goto L172a;

		L1463:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0xc));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L147d;

			ObtainContinentIDFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0xc));

			goto L14f3;

		L147d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegSource.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1494;

			ObtainContinentIDFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegSource.Value16);

			goto L14f3;

		L1494:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			ObtainTerrainTypeFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L14ad;

			ObtainContinentIDFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);

			goto L14f3;

		L14ad:
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L14ff;

		L14b3:
			this.processor.EmitLocalWord(0x18, this.processor.IncrementWord(this.processor.PullLocalWord(0x18)));

		L14b6:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xc);
			if (this.processor.FlagSet.IsLess) goto L14bf;
			goto L171d;

		L14bf:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x14);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x16, 0xffff);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				(short)this.processor.RegAcc.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L14ed;
			goto L1463;

		L14ed:
			ObtainContinentIDFromScreen(
				this.processor.PullLocalSignedWord(0x8),
				this.processor.PullLocalSignedWord(0xc));

		L14f3:			
			this.processor.EmitLocalWord(0x16, this.processor.RegAcc.Value16);
			goto L14ad;

		L14ff:
			this.processor.EmitLocalWord(0xa, 0x1);
			goto L155b;

		L1506:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x4));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1520;

			ObtainContinentIDFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x4));

			goto L159e;

		L1520:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegSource.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1537;

			ObtainContinentIDFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegSource.Value16);

			goto L159e;

		L1537:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			ObtainTerrainTypeFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1550;

			ObtainContinentIDFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);

			goto L159e;

		L1550:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L15aa;

		L1558:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L155b:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1564;
			goto L14b3;

		L1564:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x8));
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegAcc.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L1598;
			goto L1506;

		L1598:
			ObtainContinentIDFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4));

		L159e:			
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			goto L1550;

		L15aa:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xc, 0x4);
			this.processor.EmitLocalWord(0x10, 0x1);
			this.processor.EmitLocalWord(0x12, 0x0);
			goto L15c5;

		L15c2:
			this.processor.EmitLocalWord(0x12, this.processor.IncrementWord(this.processor.PullLocalWord(0x12)));

		L15c5:
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x4);
			if (this.processor.FlagSet.IsLessOrEqual) goto L15ce;
			goto L1690;

		L15ce:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.EmitLocalWord(0x2, this.processor.SumWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0x6, 0x4);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L160a;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x2),
				this.processor.PullLocalSignedWord(0x4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L160d;

		L160a:
			this.processor.EmitLocalWord(0x6, this.processor.DecrementWord(this.processor.PullLocalWord(0x6)));

		L160d:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x4));
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1631;

			this.owner.MapOps.VerifyMapLimits(
				(short)this.processor.RegSource.Value16,
				this.processor.PullLocalSignedWord(0x4));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1634;

		L1631:
			this.processor.EmitLocalWord(0x6, this.processor.DecrementWord(this.processor.PullLocalWord(0x6)));

		L1634:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);

			ObtainTerrainTypeFromScreen(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegSource.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L1658;

			this.owner.MapOps.VerifyMapLimits(
				this.processor.PullLocalSignedWord(0x2),
				(short)this.processor.RegSource.Value16);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L165b;

		L1658:
			this.processor.EmitLocalWord(0x6, this.processor.DecrementWord(this.processor.PullLocalWord(0x6)));

		L165b:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);

			ObtainTerrainTypeFromScreen((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L167f;

			this.owner.MapOps.VerifyMapLimits((short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L1682;

		L167f:
			this.processor.EmitLocalWord(0x6, this.processor.DecrementWord(this.processor.PullLocalWord(0x6)));

		L1682:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x2);
			if (this.processor.FlagSet.IsLess) goto L168b;
			goto L15c2;

		L168b:
			this.processor.EmitLocalWord(0x10, 0x0);

		L1690:
			this.processor.CompareWord(this.processor.PullLocalWord(0x10), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L16a8;
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xb);
			if (this.processor.FlagSet.IsEqual) goto L169f;
			goto L1558;

		L169f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L16a8;
			goto L1558;

		L16a8:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x18));
			this.processor.CompareWord(this.processor.RegDest.Value16, 0xc);
			if (this.processor.FlagSet.IsLess) goto L16bc;
			goto L1558;

		L16bc:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x14));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0x18));
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xa);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f38), this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f38)), this.processor.RegAcc.LowByte));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x14));
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegDest.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L16e8;
			goto L1558;

		L16e8:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x14);
			if (this.processor.FlagSet.IsLess) goto L16f0;
			goto L1558;

		L16f0:
			this.processor.RegDest.Value16 = this.processor.BitOrWord(this.processor.RegDest.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L16f7;
			goto L1558;

		L16f7:
			this.processor.CompareWord(this.processor.RegDest.Value16, 0xc);
			if (this.processor.FlagSet.IsLessOrEqual) goto L16ff;
			goto L1558;

		L16ff:
			this.processor.RegAcc.Value16 = 0xd;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegDest.Value16);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xa);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x3);
			this.processor.RegCount.LowByte = this.processor.BitAndByte(this.processor.RegCount.LowByte, 0x7);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f38), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f38)), this.processor.RegAcc.LowByte));
			goto L1558;

		L171d:
			if (marker != 0)
			{
				DisplayStoryIntro();
			}
		
			this.processor.IncrementLocalWord(0x14);

		L172a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0x14);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1738;
			this.processor.EmitLocalWord(0x18, 0x0);
			goto L14b6;

		L1738:
			this.processor.EmitLocalWord(0x18, 0x0);

		L173d:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x18);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f38), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7f38)), 0xe0));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x802f), 
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x802f)), 0xe));
			this.processor.IncrementLocalWord(0x18);
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xc);
			if (this.processor.FlagSet.IsLess) goto L173d;
			this.processor.EmitGlobalByte(0x7f38, this.processor.BitAndByte(this.processor.PullGlobalByte(0x7f38), 0x7f));
			this.processor.EmitGlobalByte(0x7f43, this.processor.BitAndByte(this.processor.PullGlobalByte(0x7f43), 0xdf));
			this.processor.EmitGlobalByte(0x802f, this.processor.BitAndByte(this.processor.PullGlobalByte(0x802f), 0xfd));
			this.processor.EmitGlobalByte(0x803a, this.processor.BitAndByte(this.processor.PullGlobalByte(0x803a), 0xf7));
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ReckonContinentSizes");
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
		/// ?
		/// </summary>
		public void DisplayStoryIntro()
		{
			this.processor.Journal.OpenScope("DisplayStoryIntro()");

			// function body
			this.processor.EnterFrame(0x1a);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b62), 0x2);
			if (this.processor.FlagSet.IsLess) goto L185a;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L185a;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b62), 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1820;
			this.processor.EmitGlobalWord(0x3b62, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b62)));
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b62), 0x5);
			if (this.processor.FlagSet.IsNotEqual) goto L184f;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 40, 160, 240, 8, 0);

			goto L184f;

		L1820:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3c;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0x6800, this.processor.RegAcc.Value16);
			this.processor.TestBitsByte(this.processor.PullGlobalByte(0x6800), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L183a;
			this.processor.RegAcc.Value16 = 0xf;
			goto L183d;

		L183a:
			this.processor.RegAcc.Value16 = 0x3;

		L183d:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3b28, 160, 160, this.processor.RegAcc.Value16);

		L184f:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			goto L1bdd;

		L185a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b62), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L18a1;

			this.owner.Libc.OpenStream("story.txt", "rt");

			this.processor.EmitGlobalWord(0x6804, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x3b62, 0x1);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0x6806, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6800, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "");

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x7);
			this.processor.EmitGlobalWord(0x6802, 0x0);

		L18a1:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6802), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L18b0;

			this.owner.Services.ObtainSongPosition();

			this.processor.EmitGlobalWord(0x6800, this.processor.RegAcc.Value16);

		L18b0:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6806);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6800), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L18c4;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			goto L1bd8;

		L18c4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b62), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L18f3;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6802), 0x28);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L18f3;

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 160, 3);

			this.owner.TextPainter.AwaitTime(5);

		L18f3:
			this.owner.Libc.ScanStream((short)this.processor.PullGlobalWord(0x6804), "%[^\n]\n", 0xba06);

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1912;
			goto L1b6a;

		L1912:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L1925;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x67fc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1925;
			goto L1b6a;

		L1925:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b64), 0x1);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1952;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0xa0, 0x140, 8,
				this.processor.PullGlobalWord(0xaa),
				0, 0xa0);

		L1952:
			this.owner.TextPainter.AwaitTime(10);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 160, 3);

			this.owner.TextPainter.AwaitTime(5);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 160, 11);

			this.processor.EmitGlobalWord(0x6806, 0x0);

			this.owner.Libc.TextLength(0xba06);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L19db;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6802), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L19cf;

			this.owner.Services.RestoreTimer();

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.PerformSound(4, 0);

		L19cf:
			this.processor.EmitGlobalWord(0x6806, this.processor.SumWord(this.processor.PullGlobalWord(0x6806), 0x2));
			this.processor.EmitGlobalWord(0x6802, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6802)));
			goto L1b29;

		L19db:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x10), "birth0.pic");

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x3b64);
			this.processor.EmitGlobalWord(0x3b64, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b64)));
			this.processor.EmitLocalByte(0xb, this.processor.SumByte(this.processor.PullLocalByte(0xb), this.processor.RegAcc.LowByte));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1a19;
			this.processor.EmitLocalWord(0x16, 0x1);
			goto L1a11;

		L1a03:
			this.owner.Services.DeactivateHueCycle(this.processor.PullLocalWord(0x16));

			this.processor.IncrementLocalWord(0x16);

		L1a11:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x3b68);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1a03;

		L1a19:
			this.owner.ImageToolkit.ImportPictureOrColors(2, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1a41;

			this.owner.Services.FloodColors(8, 0, 0, 0);

		L1a41:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1a6b;
			goto L1b29;

		L1a6b:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x9), "pal");

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, (ushort)(this.processor.RegFrame.Value16 - 0x10), 0xc5be);

			this.owner.Services.ImportColorsFromBuffer(8, 0xc5be);

			this.processor.EmitGlobalWord(0x3b68, 0x0);
			goto L1af8;

		L1aa3:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x3b66);
			this.processor.EmitGlobalWord(0x3b66, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b66)));
			this.processor.SegExtra.Value16 = 0x3710; // segment
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x3b66);
			this.processor.EmitGlobalWord(0x3b66, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b66)));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0));
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x3b66);
			this.processor.EmitGlobalWord(0x3b66, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b66)));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x12c;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);

			this.processor.EmitGlobalWord(0x3b68, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b68)));

			this.owner.Services.AssignHueCycleSpan(
				this.processor.PullGlobalWord(0x3b68),
				this.processor.RegAcc.Value16,
				this.processor.PullLocalWord(0x12),
				this.processor.PullLocalWord(0x14));

		L1af8:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x3b66);
			this.processor.SegExtra.Value16 = 0x3710; // segment
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1aa3;
			this.processor.EmitGlobalWord(0x3b66, this.processor.IncrementWord(this.processor.PullGlobalWord(0x3b66)));
			this.processor.EmitLocalWord(0x16, 0x1);
			goto L1b21;

		L1b13:
			this.owner.Services.ActivateHueCycle(this.processor.PullLocalWord(0x16));

			this.processor.IncrementLocalWord(0x16);

		L1b21:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x3b68);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1b13;

		L1b29:
			this.owner.Libc.TextLength(0xba06);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x13);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1b3e;
			this.processor.EmitGlobalWord(0x6806, this.processor.IncrementWord(this.processor.PullGlobalWord(0x6806)));

		L1b3e:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(0x3710, (ushort)((this.processor.PullGlobalWord(0x6802) << 1) + 0x83)),
				(45 * this.processor.PullGlobalSignedWord(0x6806)) + this.processor.PullGlobalSignedWord(0x6800),
				32767);

			this.processor.EmitGlobalWord(0x6806, this.processor.RegAcc.Value16);
			goto L1bbe;

		L1b6a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x18), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1b7c;

			this.owner.TextPainter.AwaitTime(180);

		L1b7c:
			this.owner.Libc.CloseStream((short)this.processor.PullGlobalWord(0x6804));

			this.owner.Services.PerformSound(1, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1bb8;
			this.processor.EmitLocalWord(0x16, 0x1);
			goto L1bb0;

		L1ba2:
			this.owner.Services.DeactivateHueCycle(this.processor.PullLocalWord(0x16));

			this.processor.IncrementLocalWord(0x16);

		L1bb0:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x3b68);
			this.processor.CompareWord(this.processor.PullLocalWord(0x16), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1ba2;

		L1bb8:
			this.processor.EmitGlobalWord(0x3b62, 0x2);

		L1bbe:
			this.owner.Services.DisplayScreen(0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3b62), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L1bdd;

		L1bd8:
			this.processor.RegAcc.Value16 = 0x1;
			goto L1bdf;

		L1bdd:
			this.processor.RegAcc.Value16 = 0;

		L1bdf:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("DisplayStoryIntro");
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
