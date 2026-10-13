using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	public class MainIntro
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public MainIntro(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayMainIntro()
		{
			this.processor.Journal.OpenScope("DisplayMainIntro()");

			// function body
			this.processor.EnterFrame(0x4e);
			this.processor.PreserveSource();

			this.processor.EmitGlobalWord(0xdeba, 0x1);
			this.processor.EmitLocalWord(0x48, 0x1);

		L0012:
			this.owner.Startup.QueryForDisk(this.processor.PullLocalWord(0x48), 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0035;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdeba, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x48);
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0012;

		L0035:
			this.processor.EmitLocalWord(0x4c, 0x0);

		L003a:
			this.owner.Services.PathPresent(0x324a);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0051;
			this.processor.RegAcc.Value16 = 0x1;
			goto L0053;

		L0051:
			this.processor.RegAcc.Value16 = 0;

		L0053:
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L00ee;
			goto L003a;

		L00ee:
			this.processor.CompareByte(this.processor.PullGlobalByte(0x1a22), 0x6d);
			if (this.processor.FlagSet.IsEqual) goto L00fc;
			this.processor.CompareByte(this.processor.PullGlobalByte(0x1a22), 0x4d);
			if (this.processor.FlagSet.IsNotEqual) goto L0101;

		L00fc:
			this.processor.RegAcc.Value16 = 0x1;
			goto L0103;

		L0101:
			this.processor.RegAcc.Value16 = 0;

		L0103:
			this.processor.EmitGlobalWord(0xd762, this.processor.RegAcc.Value16);
			this.processor.CompareByte(this.processor.PullGlobalByte(0x1a22), 0x65);
			if (this.processor.FlagSet.IsEqual) goto L0114;
			this.processor.CompareByte(this.processor.PullGlobalByte(0x1a22), 0x45);
			if (this.processor.FlagSet.IsNotEqual) goto L0119;

		L0114:
			this.processor.RegAcc.Value16 = 0x1;
			goto L011b;

		L0119:
			this.processor.RegAcc.Value16 = 0;

		L011b:
			this.processor.EmitGlobalWord(0xb20e, this.processor.RegAcc.Value16);

			// Change Num-Lock state			

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.processor.EmitGlobalWord(0x6dfc, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0180;
			this.processor.RegAcc.Value16 = 0x3;
			goto L0183;

		L0180:
			this.processor.RegAcc.Value16 = 0x4;

		L0183:
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x48, 0x0);
			goto L0196;

		L018d:
			this.processor.EmitGlobalWord(0x6dfc, 0x1);

		L0193:
			this.processor.EmitLocalWord(0x48, this.processor.IncrementWord(this.processor.PullLocalWord(0x48)));

		L0196:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01be;
			
			this.owner.VideoCard.ReserveScreen(this.processor.PullLocalWord(0x48));
			
			this.processor.EmitGlobalWord(0x32d6, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L018d;

			goto L0193;

		L01be:
			this.owner.Services.PrepareSoundDriver();

			this.owner.Services.LaunchTimer();

			this.owner.Helpers.SeedRandomizer();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L020a;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19fc), 0, 0, 320, 200, 0);

		L020a:
			this.owner.VideoCard.AssignHuesByIndexArray(0x19fe);
			
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x328a, 1);

			this.owner.VideoCard.ScreenToPicture(1, 0, 64, 320, 80);

			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3293, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x4c, 0x140, 0x18,
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0);

			this.owner.ImageToolkit.ImportPictureOrColors(2, 0, 0, 0x329e, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0x4c, 0x140, 0x18,
				this.processor.PullGlobalWord(0x19e8),
				0, 0xb0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x32a9, 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 12, 320, 176, 0);

			this.owner.Services.AssignHueCycleSpan(1, 14, 146, 152);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L031c;
			this.processor.EmitLocalWord(0x48, 0x1);

		L0308:
			this.owner.Services.ActivateHueCycle(this.processor.PullLocalWord(0x48));
			
			this.processor.IncrementLocalWord(0x48);

			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0308;

		L031c:
			this.owner.World.SessionPrefMarkers |= 0x10;

			this.owner.Libc.OpenStream("credits.txt", "rt");

			this.owner.GlobD768 = this.processor.RegAcc.Value16;

			this.owner.Services.RestoreTimer();

			this.owner.Services.PerformSound(3, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.EmitLocalWord(0x48, 0x0);
			goto L04d2;

		L0352:
			this.processor.RegAcc.Value16 = 0x7;

		L0355:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 184, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0376;
			this.processor.RegAcc.Value16 = 0xf8;
			goto L0379;

		L0376:
			this.processor.RegAcc.Value16 = 0xf;

		L0379:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 182, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L039a;
			this.processor.RegAcc.Value16 = 0xfa;
			goto L039d;

		L039a:
			this.processor.RegAcc.Value16 = 0xf;

		L039d:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 183, this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

		L03bb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L03d4;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L03cf;
			this.processor.RegAcc.Value16 = 0;
			goto L03d2;

		L03cf:
			this.processor.RegAcc.Value16 = 0x3;

		L03d2:
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

		L03d4:
			this.processor.RegAcc.Value16 = 0x140;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x48));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, this.processor.PullLocalWord(0x48), 0x40,
				this.processor.PullGlobalWord(0xaa),
				this.processor.RegSource.Value16, 0xc);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0, 0x140, 0x18,
				this.processor.PullGlobalWord(0xaa),
				0, 0x4c);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x58, this.processor.PullLocalWord(0x48), 0x58,
				this.processor.PullGlobalWord(0xaa),
				this.processor.RegSource.Value16, 0x64);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0476;
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Services.AssignDot(0, 0, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			this.owner.Services.DisplayScreen(this.processor.RegAcc.Value16);

		L0476:
			this.owner.Services.ObtainSongPosition();

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x15, this.processor.PullLocalWord(0x48));
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L04a0;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x48), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L04a0;
			this.processor.IncrementLocalWord(0x48);

		L04a0:
			this.owner.Services.ObtainSongPosition();

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x15, this.processor.PullLocalWord(0x48));
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsGreater) goto L04a0;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L04cf;
			this.processor.EmitLocalWord(0x48, 0x3e7);

		L04cf:
			this.processor.EmitLocalWord(0x48, this.processor.IncrementWord(this.processor.PullLocalWord(0x48)));

		L04d2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x140);
			if (this.processor.FlagSet.IsLess) goto L04dc;
			goto L0635;

		L04dc:
			this.processor.RegAcc.Value16 = 0x140;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x48));
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19d4), 0, 176, (short)this.processor.RegSource.Value16, 24, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x40, this.processor.PullLocalWord(0x48), 0x18,
				this.processor.PullGlobalWord(0x19d4),
				this.processor.RegSource.Value16, 0xb0);

			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L053c;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x5);
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L053c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x32);
			if (this.processor.FlagSet.IsNotEqual) goto L0547;
			this.processor.EmitGlobalByte(0xba06, 0x0);

		L0547:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x46);
			if (this.processor.FlagSet.IsNotEqual) goto L0558;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0558:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x78);
			if (this.processor.FlagSet.IsNotEqual) goto L0569;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0569:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xa0);
			if (this.processor.FlagSet.IsNotEqual) goto L0575;
			this.processor.EmitGlobalByte(0xba06, 0x0);

		L0575:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xaa);
			if (this.processor.FlagSet.IsNotEqual) goto L0587;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0587:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xbe);
			if (this.processor.FlagSet.IsNotEqual) goto L0599;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0599:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xd2);
			if (this.processor.FlagSet.IsNotEqual) goto L05ab;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L05ab:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xe6);
			if (this.processor.FlagSet.IsNotEqual) goto L05bd;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L05bd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xfa);
			if (this.processor.FlagSet.IsNotEqual) goto L05cf;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L05cf:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x10e);
			if (this.processor.FlagSet.IsNotEqual) goto L05e1;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L05e1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x122);
			if (this.processor.FlagSet.IsNotEqual) goto L05f3;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L05f3:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x136);
			if (this.processor.FlagSet.IsNotEqual) goto L0605;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0605:
			this.owner.Libc.TextLength(0xba06);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0618;
			goto L03bb;

		L0618:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L062f;
			goto L0352;

		L062f:
			this.processor.RegAcc.Value16 = 0xfc;
			goto L0355;

		L0635:
			this.processor.EmitLocalWord(0x48, 0x0);
			goto L083b;

		L063d:
			this.processor.RegAcc.Value16 = 0x7;

		L0640:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 184, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0661;
			this.processor.RegAcc.Value16 = 0xf8;
			goto L0664;

		L0661:
			this.processor.RegAcc.Value16 = 0xf;

		L0664:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 182, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0685;
			this.processor.RegAcc.Value16 = 0xfa;
			goto L0688;

		L0685:
			this.processor.RegAcc.Value16 = 0xf;

		L0688:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 183, this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

		L06a6:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L06bf;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L06ba;
			this.processor.RegAcc.Value16 = 0;
			goto L06bd;

		L06ba:
			this.processor.RegAcc.Value16 = 0x3;

		L06bd:
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

		L06bf:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L06ee;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				this.processor.PullLocalWord(0x48),
				0,
				(ushort)(0x140 - this.processor.PullLocalWord(0x48)),
				0x40,
				this.processor.PullGlobalWord(0xaa),
				0, 0xc);

		L06ee:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L071a;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0, this.processor.PullLocalWord(0x48), 0x40,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(0x140 - this.processor.PullLocalWord(0x48)), 0xc);

		L071a:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0, 0x140, 0x18,
				this.processor.PullGlobalWord(0xaa),
				0, 0x4c);

			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0770;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				this.processor.PullLocalWord(0x48), 0x58,
				(ushort)(0x140 - this.processor.PullLocalWord(0x48)), 0x58,
				this.processor.PullGlobalWord(0xaa),
				0, 0x64);

		L0770:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L079c;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0x58, this.processor.PullLocalWord(0x48), 0x58,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(0x140 - this.processor.PullLocalWord(0x48)), 0x64);

		L079c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07d0;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Services.AssignDot(0, 0, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			this.owner.Services.DisplayScreen(this.processor.RegAcc.Value16);

		L07d0:
			this.owner.Services.ObtainSongPosition();

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x15, this.processor.PullLocalWord(0x48));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x1a40);
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L07fd;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x48), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L07fd;
			this.processor.IncrementLocalWord(0x48);

		L07fd:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L082a;

			this.owner.Services.ObtainSongPosition();

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x15, this.processor.PullLocalWord(0x48));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x1a40);
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			if (this.processor.FlagSet.IsGreater) goto L07fd;

		L082a:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0838;
			this.processor.EmitLocalWord(0x48, 0x3e7);

		L0838:
			this.processor.EmitLocalWord(0x48, this.processor.IncrementWord(this.processor.PullLocalWord(0x48)));

		L083b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x140);
			if (this.processor.FlagSet.IsLess) goto L0845;
			goto L09d7;

		L0845:
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0870;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				this.processor.PullLocalWord(0x48), 0x40,
				(ushort)(0x140 - this.processor.PullLocalWord(0x48)), 0x18,
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0);

		L0870:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L089f;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0x40, this.processor.PullLocalWord(0x48), 0x18,
				this.processor.PullGlobalWord(0x19d4),
				(ushort)(0x140 - this.processor.PullLocalWord(0x48)), 0xb0);

		L089f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L08b0;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L08b0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x1e);
			if (this.processor.FlagSet.IsNotEqual) goto L08c1;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L08c1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x32);
			if (this.processor.FlagSet.IsNotEqual) goto L08cc;
			this.processor.EmitGlobalByte(0xba06, 0x0);

		L08cc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L08dd;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L08dd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x54);
			if (this.processor.FlagSet.IsNotEqual) goto L08ee;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L08ee:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x74);
			if (this.processor.FlagSet.IsNotEqual) goto L08f9;
			this.processor.EmitGlobalByte(0xba06, 0x0);

		L08f9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x82);
			if (this.processor.FlagSet.IsNotEqual) goto L090b;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L090b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x96);
			if (this.processor.FlagSet.IsNotEqual) goto L091d;

			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L091d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xaa);
			if (this.processor.FlagSet.IsNotEqual) goto L092f;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L092f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xbe);
			if (this.processor.FlagSet.IsNotEqual) goto L0941;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0941:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xd2);
			if (this.processor.FlagSet.IsNotEqual) goto L0953;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0953:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xe6);
			if (this.processor.FlagSet.IsNotEqual) goto L0965;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0965:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0xfa);
			if (this.processor.FlagSet.IsNotEqual) goto L0977;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0977:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x10e);
			if (this.processor.FlagSet.IsNotEqual) goto L0989;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L0989:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x122);
			if (this.processor.FlagSet.IsNotEqual) goto L099b;
			
			this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);

		L099b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x136);
			if (this.processor.FlagSet.IsNotEqual) goto L09a7;
			this.processor.EmitGlobalByte(0xba06, 0x0);

		L09a7:
			this.owner.Libc.TextLength(0xba06);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L09ba;
			goto L06a6;

		L09ba:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L09d1;
			goto L063d;

		L09d1:
			this.processor.RegAcc.Value16 = 0xfc;
			goto L0640;

		L09d7:
			this.processor.EmitGlobalWord(0x6b32, 0xfffe);

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0a0f;
			this.processor.EmitGlobalWord(0x6b32, 0xffff);

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4e);
			if (this.processor.FlagSet.IsEqual) goto L0a09;
			if (this.processor.FlagSet.IsGreater) goto L0a3d;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x43);
			if (this.processor.FlagSet.IsEqual) goto L0a35;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x45);
			if (this.processor.FlagSet.IsEqual) goto L0a2d;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4c);
			if (this.processor.FlagSet.IsEqual) goto L0a25;
			goto L0a0f;

		L0a09:
			this.processor.EmitGlobalWord(0x6b32, 0x0);

		L0a0f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b32), 0xfffe);
			if (this.processor.FlagSet.IsEqual) goto L0a19;
			goto L0b26;

		L0a19:
			this.owner.Services.ObtainSongPosition();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd00); // !!! was 0xd87
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0a53;
			goto L0a19;

		L0a25:
			this.processor.EmitGlobalWord(0x6b32, 0x1);
			goto L0a0f;

		L0a2d:
			this.processor.EmitGlobalWord(0x6b32, 0x2);
			goto L0a0f;

		L0a35:
			this.processor.EmitGlobalWord(0x6b32, 0x3);
			goto L0a0f;

		L0a3d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x63);
			if (this.processor.FlagSet.IsEqual) goto L0a35;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x65);
			if (this.processor.FlagSet.IsEqual) goto L0a2d;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6c);
			if (this.processor.FlagSet.IsEqual) goto L0a25;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6e);
			if (this.processor.FlagSet.IsEqual) goto L0a09;
			goto L0a0f;

		L0a53:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				0, 64,
				this.processor.PullLocalWord(0x4e));

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ab4;

			this.owner.Services.AssignHueCycleSpan(4, 14, 224, 239);

			this.owner.Services.ActivateHueCycle(4);

		L0ab4:
			this.processor.EmitLocalWord(0x48, 0x0);

		L0ab9:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				this.processor.PullLocalWord(0x48), 0x40, 0x4, 0x50,
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalWord(0x48), 0x40);

			this.owner.TextPainter.AwaitTime(1);

			this.processor.EmitLocalWord(0x48, this.processor.SumWord(this.processor.PullLocalWord(0x48), 0x4));
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x140);
			if (this.processor.FlagSet.IsLess) goto L0ab9;
			this.processor.EmitLocalWord(0x48, 0x0);
			goto L0b0c;

		L0afd:
			this.owner.TextPainter.AwaitTime(5);

			this.processor.IncrementLocalWord(0x48);

		L0b0c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x64);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0b1b;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0afd;

		L0b1b:
			this.owner.TurnLogic.AwaitForKey();

			this.processor.EmitGlobalWord(0x6b32, 0xffff);

		L0b26:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0b4a;
			this.processor.EmitLocalWord(0x48, 0x1);
			goto L0b37;

		L0b34:
			this.processor.EmitLocalWord(0x48, this.processor.IncrementWord(this.processor.PullLocalWord(0x48)));

		L0b37:
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), 0x4);
			if (this.processor.FlagSet.IsGreater) goto L0b52;

			this.owner.Services.DeactivateHueCycle(this.processor.PullLocalWord(0x48));

			goto L0b34;

		L0b4a:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

		L0b52:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19d4), 0, 0, 320, 200, 0);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				0, 64,
				this.processor.PullLocalWord(0x4e));

			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x14);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Services.DisplayScreen(0);
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			
			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x4e), 0);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayMainIntro");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayCivilizationScreen(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationScreen({participantIdx})");

			// function body
			this.processor.EnterFrame(0x246);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(6, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x32d8, 0);

			this.processor.EmitLocalWord(0x23e, 0x0);

		L0c0c:
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.PullLocalWord(0x23e) % 6) * 0x32) + 1),
				(ushort)(((this.processor.PullLocalWord(0x23e) / 6) * 0x32) + 1),
				0x31, 0x31);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x23e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x238), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x23e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x18);
			if (this.processor.FlagSet.IsLess) goto L0c0c;

			this.owner.Helpers.HaltHueCycling();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0c6c;
			
			this.processor.EmitLocalWord(0x246, 0xec);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 236);

			goto L0c6e;

		L0c6c:
			this.processor.EmitLocalWord(0x246, 0);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

		L0c6e:			
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x32e4, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x18, 0x140, 0xb0,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x20, 0x140, 0x18,
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0);

			this.owner.Startup.QueryForDisk(0, 1);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0d17;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19fc), 0, 0, 320, 200, 0);

		L0d17:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200,
				this.processor.PullLocalWord(0x246));

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 12, 320, 176,
				this.processor.PullLocalWord(0x246));

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, " ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19a2)));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), 0xba06);

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30ba), 
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1982)));

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].StarshipInhabitants, 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].StarshipLiftoffSeason, 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Season, 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30c0), 0xba06);

			this.owner.BlockText.ImportTextFromFile(0x3301, 
				(ushort)((participantIdx != this.owner.World.PersonParticipantIdx) ? 0x32f9 : 0x32f2));

			this.owner.BlockText.ReflowText();

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x200), 0xba06);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x6);
			this.processor.EmitLocalWord(0x242, 0xfff6);
			this.processor.EmitLocalWord(0x244, 0x5c);
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x204, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x23c, this.processor.RegAcc.Value16);

			this.owner.Libc.TextLength(0xba06);

			this.processor.EmitLocalWord(0x240, this.processor.RegAcc.Value16);

			this.owner.Services.PerformSound(3, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.EmitLocalWord(0x23e, 0x0);
			goto L10cc;

		L0ed8:
			this.processor.RegAcc.Value16 = 0x7;

		L0edb:
			this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(0xba06 + this.processor.PullLocalWord(0x23c)),
				160, 184, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f00;
			this.processor.RegAcc.Value16 = 0x3c;
			goto L0f03;

		L0f00:
			this.processor.RegAcc.Value16 = 0xf;

		L0f03:
			this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(0xba06 + this.processor.PullLocalWord(0x23c)),
				160, 183, this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x23a);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

		L0f26:
			this.processor.EmitLocalWord(0x208, this.processor.IncrementWord(this.processor.PullLocalWord(0x208)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x208), 0x17);
			if (this.processor.FlagSet.IsLess) goto L0f37;
			this.processor.EmitLocalWord(0x208, 0x0);

		L0f37:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				135, 51,
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + 
					(this.processor.PullLocalWord(0x208) << 1) - 0x238)));

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0f6e;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0f69;
			this.processor.RegAcc.Value16 = 0;
			goto L0f6c;

		L0f69:
			this.processor.RegAcc.Value16 = 0x3;

		L0f6c:
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

		L0f6e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0f95;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 12, 319, 20,
				this.processor.PullLocalWord(0x246));

		L0f95:
			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0fc4;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, this.processor.PullLocalWord(0x23e), 0x14,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(0x140 - this.processor.PullLocalWord(0x23e)), 0xc);

		L0fc4:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0, 0x140, 0x18,
				this.processor.PullGlobalWord(0xaa),
				0, 0x20);

			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1015;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 56,
				320 - this.processor.PullLocalSignedWord(0x23e), 132,
				this.processor.PullLocalWord(0x246));

		L1015:
			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1047;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0,0x2c, this.processor.PullLocalWord(0x23e), 0x84,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(0x140 - this.processor.PullLocalWord(0x23e)), 0x38);

		L1047:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x23e), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L106f;
			this.processor.IncrementLocalWord(0x242);

			this.owner.Services.AssignDot(
				this.processor.PullLocalSignedWord(0x242) - 
					this.processor.PullLocalSignedWord(0x23e),
				this.processor.PullLocalSignedWord(0x244), 15);

		L106f:
			this.processor.EmitLocalWord(0x242, this.processor.IncrementWord(this.processor.PullLocalWord(0x242)));
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L10a9;
			
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x23a, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Services.AssignDot(0, 0, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x23a);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, this.processor.RegAcc.Value16);

			this.owner.Services.DisplayScreen(this.processor.RegAcc.Value16);

		L10a9:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsLessOrEqual) goto L10be;
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x23e), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L10be;
			this.processor.IncrementLocalWord(0x23e);

		L10be:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsLess) goto L10be;
			this.processor.IncrementLocalWord(0x23e);

		L10cc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x140);
			if (this.processor.FlagSet.IsLess) goto L10d7;
			goto L119f;

		L10d7:
			this.owner.Services.RestoreTimer();

			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x140);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1107;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19d4), 0, 176,
				320 - this.processor.PullLocalSignedWord(0x23e), 24,
				this.processor.PullLocalWord(0x246));

		L1107:
			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1139;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x14, this.processor.PullLocalWord(0x23e), 0x18,
				this.processor.PullGlobalWord(0x19d4),
				(ushort)(0x140 - this.processor.PullLocalWord(0x23e)), 0xb0);

		L1139:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x23e);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x16;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1174;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x23c, this.processor.RegAcc.Value16);

		L1150:
			this.processor.EmitLocalWord(0x204, this.processor.IncrementWord(this.processor.PullLocalWord(0x204)));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06)), 0x5e);
			if (this.processor.FlagSet.IsEqual) goto L1167;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1150;

		L1167:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1174;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xba06), 0x0);

		L1174:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.PullLocalWord(0x204), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1181;
			goto L0f26;

		L1181:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16);
			this.processor.EmitLocalWord(0x23a, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1199;
			goto L0ed8;

		L1199:
			this.processor.RegAcc.Value16 = 0x38;
			goto L0edb;

		L119f:
			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0x238), 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);
			
			this.owner.Services.DisplayScreen(0);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xba07);
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x200), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x240, this.processor.SubtractWord(this.processor.PullLocalWord(0x240), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0x204, 0xffff);

			if (participantIdx == this.owner.World.PersonParticipantIdx) goto L11ed;
			goto L1350;

		L11ed:
			this.owner.Startup.QueryForDisk(6, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x330a, 0);

			this.owner.Services.PerformSound(1, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1233;

			this.owner.Services.FloodColors(5, 0, 0, 0);

		L1233:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x3316, 1);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.Services.PerformSound(0x22, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L12a1;
			goto L137e;

		L12a1:
			this.owner.Services.AssignHueCycleSpan(1, 15, 16, 31);

			this.owner.Services.AssignHueCycleSpan(2, 100, 32, 34);

			this.owner.Services.AssignHueCycleSpan(3, 42, 35, 40);

			this.owner.Services.AssignHueCycleSpan(4, 300, 41, 47);

			this.owner.Services.AssignHueCycleSpan(5, 60, 48, 53);

			this.owner.Services.AssignHueCycleSpan(6, 37, 54, 63);

			this.processor.EmitLocalWord(0x23e, 0x1);

		L1337:
			this.owner.Services.ActivateHueCycle(this.processor.PullLocalWord(0x23e));
			
			this.processor.IncrementLocalWord(0x23e);

			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x6);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1337;
			goto L137e;

		L1350:
			this.owner.Throne.ImportPalaceGraphics(1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

		L137e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x23c, this.processor.RegAcc.Value16);

		L1387:
			this.processor.EmitLocalWord(0x204, this.processor.IncrementWord(this.processor.PullLocalWord(0x204)));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x200)), 0x5e);
			if (this.processor.FlagSet.IsEqual) goto L139e;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L1387;

		L139e:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.PullLocalWord(0x204), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L13b1;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x204);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x200), 0x0);

		L13b1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.PullLocalWord(0x204), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L13be;
			goto L145f;

		L13be:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0x20,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(
				(ushort)(this.processor.RegFrame.Value16 + this.processor.PullLocalWord(0x23c) - 0x200),
				160, 5, 11);

			this.owner.TextPainter.RenderCenteredTextToScreen0(
				(ushort)(this.processor.RegFrame.Value16 + this.processor.PullLocalWord(0x23c) - 0x200),
				160, 4, 15);

			this.owner.Services.AssignDot(280, 32, 15);

			this.owner.TextPainter.AwaitTime(80);

			this.owner.Services.AssignDot(280, 32, 0);

			this.owner.TextPainter.AwaitTime(80);

		L145f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x240);
			this.processor.CompareWord(this.processor.PullLocalWord(0x204), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L146c;
			goto L137e;

		L146c:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			if (participantIdx != this.owner.World.PersonParticipantIdx) goto L149d;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L149d;
			this.processor.EmitLocalWord(0x23e, 0x1);

		L1486:
			this.owner.Services.DeactivateHueCycle(this.processor.PullLocalWord(0x23e));
			
			this.processor.IncrementLocalWord(0x23e);

			this.processor.CompareWord(this.processor.PullLocalWord(0x23e), 0x6);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1486;

		L149d:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Services.PerformSound(1, 0);
			
			this.owner.Libc.CloseStream((short)this.owner.GlobD768);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1500;

			this.owner.Startup.QueryForDisk(7, 1);

			this.owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, 0x3322, 0);

			this.owner.Startup.QueryForDisk(0, 1);

		L1500:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.TurnHooks.DisplayCivilizationScenery();

			this.owner.Helpers.SetupHueCycling();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilizationScreen");
		}

		/// <summary>
		/// The opening news of a new game: the picture 0x332e and, with the human player's leader name in capitals (code text
		/// 0x30b8) and the last technology the player knows ("Irrigation" without any, code text 0x30be), the text 0x3342 of
		/// file 0x3348 shown part after part ("^" ends a part; the last part is not shown), each in a band at row 8 (colour 14
		/// with a shadow when 0xd762 is set, else 9), waiting 200 between parts unless a part ends with "!" or a key is waiting;
		/// then the leader's sound and the screen of the civilization
		/// </summary>
		public void DisplayTerrainTutorial()
		{
			this.processor.Journal.OpenScope("DisplayTerrainTutorial()");

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(6, 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.Services.PerformSound(0x23, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x332e, 1);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			short humanIdx = this.owner.World.PersonParticipantIdx;

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((humanIdx << 1) + 0x19a2)));
			this.owner.Libc.UppercaseText(0xba06);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), 0xba06);

			this.owner.Libc.CopyText(0xba06, "Irrigation");

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(humanIdx, technologyIdx) != 0)
				{
					this.owner.Libc.CopyText(0xba06, (ushort)((technologyIdx * 0x16) + 0x4da));
				}
			}

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.BlockText.ImportTextFromFile(0x3348, 0x3342);

			this.owner.BlockText.ReflowText();

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 6);

			short end = (short)this.owner.Libc.TextLength(0xba06);
			short place = -1;

			do
			{
				ushort partAddr = (ushort)(0xba06 + place + 1);

				do
				{
					place++;
				} while (this.processor.PullGlobalByte((ushort)(0xba06 + place)) != 0x5e && place < end);

				if (place >= end)
					break;

				this.processor.EmitGlobalByte((ushort)(0xba06 + place), 0);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 6, 0x140, 0x14, this.processor.PullGlobalWord(0xaa), 0, 6);

				ushort hue = 9;

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 7, 15);
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 9, 13);

					hue = 14;
				}
				else
				{
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 9, 1);
				}

				this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 8, hue);

				if (this.processor.PullGlobalByte((ushort)(0xba05 + place)) != 0x21 && this.owner.Libc.KeyAvailable() == 0)
				{
					this.owner.TextPainter.AwaitTime(200);
				}
			} while (place < end);

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.PerformSound(this.processor.PullGlobalWord(
				(ushort)((this.owner.World.Participants[humanIdx].ChiefVisuals * 0x3a) + 0x1518)), 0);

			this.owner.TextPainter.AwaitTime(300);

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.TurnHooks.DisplayCivilizationScenery();

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 1);

			this.processor.Journal.CloseScope("DisplayTerrainTutorial");
		}

		/// <summary>
		/// The tutorial of the screen: points at the 8 tiles around the centre of the map window (0xd4cc + 7, 0xd75e + 6) with
		/// the names of their terrain (of the special resource, or "Village"), then at the menu bar, the map window and the
		/// active unit, and waits for a key or a click. SI holds 2 * direction at the pointers, like in the original: DisplayMenu
		/// leaves a value made from it in DI, which goes back to the caller.
		/// </summary>
		public void DisplayInterfaceTutorial()
		{
			this.processor.Journal.OpenScope("DisplayInterfaceTutorial()");

			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			short centreHoriz = (short)(this.processor.PullGlobalWord(0xd4cc) + 7);
			short centreVert = (short)(this.processor.PullGlobalWord(0xd75e) + 6);
			MapLayers chart = this.owner.MapOps.Chart;

			for (int heading = 1; heading <= 8; heading++)
			{
				int horizLoc = this.owner.Routing.TunePosition(chart.DirectionHoriz(heading) + centreHoriz);
				short vertLoc = (short)(chart.DirectionVert(heading) + centreVert);
				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				// Terrain names of 0x13 bytes at 0x282, names of their special resources at 0x366
				this.owner.Libc.CopyText(0xba06, (ushort)((terrain * 0x13) + 0x282));

				if (this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0)
				{
					this.owner.Libc.CopyText(0xba06, (ushort)((terrain * 0x13) + 0x366));
				}

				if (this.owner.MapOps.ContainsHut(terrain, horizLoc, vertLoc) != 0)
				{
					this.owner.Libc.CopyText(0xba06, "Village");
				}

				this.processor.RegSource.Value16 = (ushort)(heading << 1);

				RenderTutorialPointer((chart.DirectionHoriz(heading) << 4) + 200, (chart.DirectionVert(heading) << 4) + 112,
					(chart.DirectionHoriz(heading) << 6) + 170, (chart.DirectionVert(heading) * 48) + 100);
			}

			this.owner.Libc.CopyText(0xba06, "Menu Bar");

			RenderTutorialPointer(160, 6, 200, 16);

			this.owner.Libc.CopyText(0xba06, "Map Window");

			RenderTutorialPointer(48, 32, 88, 24);

			this.owner.Libc.CopyText(0xba06, "Active Unit");

			RenderTutorialPointer(40, 128, 88, 170);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.RestoreSource();

			this.processor.Journal.CloseScope("DisplayInterfaceTutorial");
		}

		/// <summary>
		/// A tutorial hint: a line (colour 15) from the pointed place to the text, then the text at 0xba06 as a dialog
		/// </summary>
		public void RenderTutorialPointer(int horizSpot1, int vertSpot1, int horizSpot2, int vertSpot2)
		{
			this.processor.Journal.OpenScope($"RenderTutorialPointer({horizSpot1}, {vertSpot1}, {horizSpot2}, {vertSpot2})");

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), horizSpot1, vertSpot1, horizSpot2, vertSpot2 + 6, 15);

			this.owner.Libc.AppendText(0xba06, "\n");

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, horizSpot2, vertSpot2, 1);

			this.processor.Journal.CloseScope("RenderTutorialPointer");
		}
	}
}
