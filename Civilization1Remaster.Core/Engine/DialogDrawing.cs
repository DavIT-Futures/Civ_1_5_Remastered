using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2d05.
	/// </summary>
	public class DialogDrawing
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public DialogDrawing(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// When set, Tab, Left and Right close the menu (ShowMenu) like Escape and set MenuTabKeyPressed,
		/// so a menu with tabs can switch between them
		/// </summary>
		public bool ChoicesIndentTokens = false;

		/// <summary>
		/// Set when the last menu was closed with Tab, Left or Right (see MenuTabKeys)
		/// </summary>
		public bool ChoicesIndentTokenDown = false;

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		public ushort DisplayMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort marker)
		{
			this.processor.Journal.OpenScope($"DisplayMenu(0x{charsAddr:x4}, {horizLoc}, {vertLoc}, {marker})");

			// function body
			this.processor.EnterFrame(0x12);
			this.processor.PreserveSource();

			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0043;

			this.owner.TurnLogic.AwaitForKey();

		L0043:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0x2fa2, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x2f9c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L006f;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L006f;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x8066), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L006f;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd206), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L006f;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

		L006f:
			this.processor.EmitLocalWord(0x4, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9a), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0081;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x2f9a);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

		L0081:
			this.processor.EmitGlobalWord(0xb1ec, 0xffff);
			this.processor.EmitGlobalWord(0xd2e2, 0x1);

			this.owner.VideoCard.ObtainTextHeight(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)));

			this.processor.EmitGlobalWord(0x2fa4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L00a8;
			this.processor.EmitGlobalWord(0x2fa4, this.processor.DecrementWord(this.processor.PullGlobalWord(0x2fa4)));

		L00a8:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3936), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L00b5;
			this.processor.EmitGlobalWord(0x2fa4, 0x8);

		L00b5:
			this.processor.EmitLocalWord(0xc, 0x7);
			this.processor.EmitLocalWord(0xe, 0x16);

			if (vertLoc == 139)
			{
				this.processor.EmitLocalWord(0xe, 0x8);
			}

			if ((vertLoc & 1) != 0 && vertLoc != 139)
			{
				this.owner.VideoCard.ObtainDot(0, horizLoc, vertLoc);

				this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
				this.processor.EmitLocalWord(0xe, 0xffff);
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0xf);
			if (this.processor.FlagSet.IsNotEqual) goto L00fc;
			this.processor.RegAcc.Value16 = 0x3;
			goto L00ff;

		L00fc:
			this.processor.RegAcc.Value16 = 0xf;

		L00ff:
			this.processor.EmitGlobalWord(0x654c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L010e;
			this.processor.EmitLocalWord(0xe, 0xffff);

		L010e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x2fa4);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			OperateMenu(charsAddr, horizLoc, vertLoc, this.processor.PullGlobalWord(0x2f9a));

			this.owner.Startup.DisplayMouseCursor();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xdb38), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L014d;
			this.processor.EmitGlobalWord(0xdb38, 0x0);
			this.processor.RegAcc.Value16 = 0xffff;
			goto L0470;

		L014d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3936), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0159;

			this.owner.Audience.ChoreographKingSpeaking();

		L0159:
			this.processor.EmitGlobalWord(0xd2e2, 0x0);
			this.owner.GlobDb3a = 0x0;

			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0177;
			this.processor.CompareWord(this.processor.PullLocalWord(0x12), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L01db;

		L0177:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.EmitGlobalWord(0x2fa2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.owner.GlobDb3a, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L018a;
			this.processor.EmitGlobalWord(0x2f9c, this.processor.RegAcc.Value16);

		L018a:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)((short)vertLoc));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x2fa4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xb1ec));
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			if (horizLoc > (short)this.owner.GlobDb3c)
			{
				this.processor.EmitLocalWord(0x4, 0xffff);
			}
			else
			{
				if ((short)this.owner.GlobDb3c > this.processor.PullGlobalSignedWord(0xde0e))
				{
					this.processor.EmitLocalWord(0x4, 0xffff);
				}
			}
		
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xb276));
			if (this.processor.FlagSet.IsEqual) goto L01cd;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);

		L01c8:
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			goto L0217;

		L01cd:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);

		L01d2:
			if (this.processor.FlagSet.IsNotEqual) goto L0217;

		L01d4:
			this.processor.EmitLocalWord(0x8, 0x1);
			goto L0217;

		L01db:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0217;

			ObtainNavigationKey();

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			if (this.ChoicesIndentTokens && (this.processor.RegAcc.Value16 == 0x9 || this.processor.RegAcc.Value16 == 0x4b00 || this.processor.RegAcc.Value16 == 0x4d00))
			{
				this.ChoicesIndentTokenDown = true;
				goto L0392;
			}

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L01f3;
			goto L0377;

		L01f3:
			if (this.processor.FlagSet.IsLessOrEqual) goto L01f8;
			goto L03e3;

		L01f8:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L0200;
			goto L0377;

		L0200:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1b);
			if (this.processor.FlagSet.IsNotEqual) goto L0208;
			goto L0392;

		L0208:
			goto L03fd;

		L020b:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd208);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0217;
			this.processor.IncrementLocalWord(0x4);

		L0217:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsLess) goto L0233;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd208);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0233;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x8066), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0233;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd206), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0238;

		L0233:
			this.processor.EmitLocalWord(0x4, 0xffff);

		L0238:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0243;
			goto L0337;

		L0243:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L02ba;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc + 3,
				((this.processor.PullLocalSignedWord(0x2) + this.processor.PullGlobalSignedWord(0xb1ec)) *
					this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 4,
				this.processor.PullGlobalSignedWord(0xd4c8) + 5,
				this.processor.PullLocalSignedWord(0x10),
				11, (byte)this.processor.PullLocalWord(0xc));

			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L02ba;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc + 3,
				((this.processor.PullLocalSignedWord(0x2) + this.processor.PullGlobalSignedWord(0xb1ec)) *
					this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 4,
				this.processor.PullGlobalSignedWord(0xd4c8) + 5,
				this.processor.PullLocalSignedWord(0x10),
				3, (byte)this.processor.PullLocalWord(0xe));

		L02ba:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L032c;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc + 3,
				((this.processor.PullLocalSignedWord(0x4) + this.processor.PullGlobalSignedWord(0xb1ec)) *
					this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 4,
				this.processor.PullGlobalSignedWord(0xd4c8) + 5,
				this.processor.PullLocalSignedWord(0x10),
				(byte)this.processor.PullLocalWord(0xc),
				11);

			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L032c;

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc + 3,
				((this.processor.PullLocalSignedWord(0x4) + this.processor.PullGlobalSignedWord(0xb1ec)) *
					this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 4,
				this.processor.PullGlobalSignedWord(0xd4c8) + 5,
				this.processor.PullLocalSignedWord(0x10),
				(byte)this.processor.PullLocalWord(0xe),
				3);

		L032c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			this.owner.Startup.DisplayMouseCursor();

		L0337:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0340;
			goto L014d;

		L0340:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0349;
			goto L0450;

		L0349:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0358;
			goto L043d;

		L0358:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0362;
			goto L0406;

		L0362:
			this.processor.RegAcc.Value16 = 0xb;
			goto L0409;

		L0368:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L0371;
			goto L0217;

		L0371:
			this.processor.EmitLocalWord(0x4, this.processor.DecrementWord(this.processor.PullLocalWord(0x4)));
			goto L0217;

		L0377:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xb276));
			goto L01d2;

		L0386:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.EmitGlobalWord(0x2f9c, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			goto L0217;

		L0392:
			this.processor.EmitLocalWord(0x4, 0xffff);
			goto L01d4;

		L039a:
			this.owner.World.SessionPrefMarkers ^= 0x10;
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x10);
			if (this.processor.FlagSet.IsEqual) goto L03a9;
			goto L0217;

		L03a9:
			this.owner.Services.PerformSound(1, 0);

			goto L0217;

		L03b8:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L03bb:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x20);
			if (this.processor.FlagSet.IsLess) goto L03c4;
			goto L0217;

		L03c4:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdc4a)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L03b8;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdc4a));
			this.processor.RegAcc.LowByte = this.processor.BitAndByte(this.processor.RegAcc.LowByte, 0x1f);
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegCount.LowByte = this.processor.BitAndByte(this.processor.RegCount.LowByte, 0x1f);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L03b8;
			this.processor.RegAcc.Value16 = this.processor.RegBase.Value16;
			goto L01c8;

		L03e3:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2300);
			if (this.processor.FlagSet.IsEqual) goto L0386;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f00);
			if (this.processor.FlagSet.IsEqual) goto L039a;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L03f5;
			goto L0368;

		L03f5:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L03fd;
			goto L020b;

		L03fd:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			goto L03bb;

		L0406:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x654c);

		L0409:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				horizLoc + 3,
				((this.processor.PullLocalSignedWord(0x4) + this.processor.PullGlobalSignedWord(0xb1ec)) *
					this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 4,
				this.processor.PullGlobalSignedWord(0xd4c8) + 5,
				this.processor.PullLocalSignedWord(0x10),
				11,
				this.processor.RegAcc.LowByte);

		L043d:
			this.owner.TextPainter.AwaitTime(20);

			this.owner.Startup.DisplayMouseCursor();

			goto L0456;

		L0450:
			this.processor.EmitGlobalWord(0x2f9c, 0x0);

		L0456:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitGlobalWord(0x2f9e, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0x2fa0, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd206, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xb276, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd7f2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);

		L0470:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayMenu");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="param4"></param>
		public void OperateMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort arg4)
		{
			this.processor.Journal.OpenScope($"OperateMenu(0x{charsAddr:x4}, {horizLoc}, {vertLoc}, {arg4})");

			// function body
			this.processor.EnterFrame(0x5c);
			this.processor.PreserveIndexRegisters();
			this.processor.EmitGlobalWord(0x654e, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2e2), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L048d;
			goto L07df;

		L048d:
			this.owner.VideoCard.ObtainTextHeight(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)));

			this.processor.EmitGlobalWord(0x2fa4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L04a8;
			this.processor.EmitGlobalWord(0x2fa4, this.processor.DecrementWord(this.processor.PullGlobalWord(0x2fa4)));

		L04a8:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3936), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L04b5;
			this.processor.EmitGlobalWord(0x2fa4, 0x8);

		L04b5:
			this.processor.EmitLocalWord(0x56, 0x0);

		L04ba:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdc4a), 0xff);
			this.processor.IncrementLocalWord(0x56);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x20);
			if (this.processor.FlagSet.IsLess) goto L04ba;
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x658e, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd4c8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x56, this.processor.RegAcc.Value16);
			goto L053b;

		L04de:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L051c;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = charsAddr;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16));
			this.processor.EmitLocalByte(0x5c, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L04f7;
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x5f);
			if (this.processor.FlagSet.IsNotEqual) goto L051c;

		L04f7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x5a), 0x20);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L050c;
			this.processor.RegSource.Value16 = this.processor.RegBase.Value16;
			this.processor.RegBase.Value16 = charsAddr;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x1));
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xdc4a), this.processor.RegAcc.LowByte);

		L050c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb1ec), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0519;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x658e);
			this.processor.EmitGlobalWord(0xb1ec, this.processor.RegAcc.Value16);

		L0519:
			this.processor.EmitLocalWord(0x5a, this.processor.IncrementWord(this.processor.PullLocalWord(0x5a)));

		L051c:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = charsAddr;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16));

			this.owner.VideoCard.ObtainCharWidth(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10)), this.processor.RegAcc.LowByte);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), this.processor.RegAcc.Value16));

		L0538:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));

		L053b:
			this.owner.Libc.TextLength(charsAddr);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x56));
			if (this.processor.FlagSet.IsLessOrEqual) goto L057d;
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = charsAddr;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16)), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L04de;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4c8);
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0564;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.EmitGlobalWord(0xd4c8, this.processor.RegAcc.Value16);

		L0564:
			this.processor.EmitLocalWord(0x54, 0x0);
			this.processor.EmitGlobalWord(0x658e, this.processor.IncrementWord(this.processor.PullGlobalWord(0x658e)));
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x658e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x654e), this.processor.RegAcc.Value16);
			goto L0538;

		L057d:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0x658e),
				0, (192 - vertLoc) / this.processor.PullGlobalSignedWord(0x2fa4));

			this.processor.EmitGlobalWord(0x658e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)horizLoc;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd4c8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitGlobalWord(0xde0e, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x658e);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x2fa4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)vertLoc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0x52, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2fa0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05c5;
			this.processor.EmitLocalWord(0x52, this.processor.SumWord(this.processor.PullLocalWord(0x52), 0x2));

		L05c5:
			this.owner.Libc.TextLength(charsAddr);

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = charsAddr;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 - 0x1)), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L05de;
			this.processor.DecrementLocalWord(0x5a);

		L05de:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.EmitGlobalWord(0xd208, this.processor.RegAcc.Value16);

			if ((vertLoc & 1) == 0)
				goto L05ed;

			goto L075b;

		L05ed:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9e), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0613;

			FloodBoxWithDoubleShadow(horizLoc, vertLoc,
				this.processor.PullGlobalSignedWord(0xde0e) - horizLoc, this.processor.PullLocalSignedWord(0x52) - vertLoc, 7);

			goto L075b;

		L0613:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x2f9e);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x50),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2fa6)));

			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9e), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L0640;

			this.owner.Libc.AppendText((ushort)(this.processor.RegFrame.Value16 - 0x50), " report:");

		L0640:
			this.owner.TextPainter.ObtainTextWidth((ushort)(this.processor.RegFrame.Value16 - 0x50));

			this.processor.EmitLocalWord(0x58, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = (ushort)horizLoc;
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x8);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xde0e), this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0661;
			this.processor.EmitGlobalWord(0xde0e, this.processor.RegSource.Value16);

		L0661:
			this.processor.RegSource.Value16 = (ushort)((short)(this.processor.PullLocalSignedWord(0x52) - vertLoc));

			this.owner.Helpers.VerifyValueSpan((short)this.processor.RegSource.Value16 + 8, 61, 999);

			FloodBoxWithDoubleShadow(horizLoc - 42, vertLoc - 8,
				(this.processor.PullGlobalSignedWord(0xde0e) - horizLoc) + 42, (short)this.processor.RegAcc.Value16, 7);

			// LEA
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegSource.Value16 - 0x34);
			this.processor.EmitLocalWord(0x58, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9e), 0x2);
			if (this.processor.FlagSet.IsGreater) goto L06d2;
			
			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
				horizLoc - 40, vertLoc - 5,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullGlobalWord(0x2f9e) << 1) + 0xdf62)));
			goto L0715;

		L06d2:
			// ??? This is dialog image being drawn for future reference
			if (this.processor.PullLocalSignedWord(0x58) > 0)
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19e8),
					(40 * this.processor.PullGlobalSignedWord(0x2f9e)) + 40, 140, 40, 60,
					this.processor.PullGlobalWord(0xaa),
					horizLoc - 40,
					((this.processor.PullLocalSignedWord(0x58) - 1) + vertLoc) - 6);
			}
			else
			{
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19e8),
					(40 * this.processor.PullGlobalWord(0x2f9e)) + 40, 140, 40, 60,
					this.processor.PullGlobalWord(0xaa),
					horizLoc - 40, vertLoc - 6);
			}

		L0715:
			this.owner.TextPainter.RenderTextToScreen0((ushort)(this.processor.RegFrame.Value16 - 0x50),
				horizLoc + 5, vertLoc - 4, 15);

			this.owner.TextPainter.ObtainTextWidth((ushort)(this.processor.RegFrame.Value16 - 0x50));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc + 5, vertLoc + 3, horizLoc + 5, vertLoc + 3, 11);

		L075b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2fa0), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0799;
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10));
			this.processor.EmitLocalWord(0x56, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			
			this.owner.TextPainter.RenderTextToScreen0(0x2fbd,
				this.processor.PullGlobalSignedWord(0xde0e) - 74,
				this.processor.PullLocalSignedWord(0x52) - 4,
				10);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), this.processor.RegAcc.Value16);

		L0799:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x8066), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L07df;

			this.owner.TextPainter.RenderText(0x2fce, 
				(ushort)(this.processor.PullGlobalWord(0xde0e) - 0x11),
				(ushort)(this.processor.PullLocalWord(0x52) - 8),
				11);
			
			RenderBox(
				this.processor.PullGlobalSignedWord(0xde0e) - 20,
				this.processor.PullLocalSignedWord(0x52) - 10,
				20, 10, 11);

		L07df:
			this.processor.RegBase.Value16 = charsAddr;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L07ec;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16), 0x5f);
			if (this.processor.FlagSet.IsNotEqual) goto L07f0;

		L07ec:
			this.processor.RegAcc.Value16 = 0;
			goto L07f3;

		L07f0:
			this.processor.RegAcc.Value16 = 0xffff;

		L07f3:
			this.processor.EmitLocalWord(0x5a, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x654c);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xc), this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x56, 0x0);
			goto L08fd;

		L0808:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x5a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xb276));
			if (this.processor.FlagSet.IsEqual) goto L081b;
			this.processor.RegAcc.Value16 = 0x3;
			goto L081d;

		L081b:
			this.processor.RegAcc.Value16 = 0;

		L081d:
			this.owner.TextPainter.RenderTextToScreen0(this.processor.RegDest.Value16,
				horizLoc + 5,
				(this.processor.PullLocalSignedWord(0x56) * this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 5,
				this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x654e));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, charsAddr);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x20);
			goto L08ce;

		L0849:
			this.processor.CompareWord(this.processor.PullLocalWord(0x5a), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0883;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2fa4), 0x9);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0883;
			
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x654e)) +
					charsAddr),
				horizLoc + 5,
				(this.processor.PullLocalSignedWord(0x56) * this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 6,
				0);

		L0883:
			this.processor.CompareWord(this.processor.PullLocalWord(0x5a), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L088e;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x654c);
			goto L08a3;

		L088e:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x5a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xb276));
			if (this.processor.FlagSet.IsEqual) goto L08a1;
			this.processor.RegAcc.Value16 = 0x3;
			goto L08a3;

		L08a1:
			this.processor.RegAcc.Value16 = 0;

		L08a3:
			this.processor.StackWord(this.processor.RegAcc.Value16);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x654e)) +
					charsAddr),
				horizLoc + 5,
				(this.processor.PullLocalSignedWord(0x56) * this.processor.PullGlobalSignedWord(0x2fa4)) + vertLoc + 5,
				this.processor.RegAcc.Value16);

		L08ce:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6550));
			this.processor.RegBase.Value16 = charsAddr;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 - 0x1), 0xa);

		L08de:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6550));
			this.processor.RegSource.Value16 = charsAddr;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16));
			this.processor.EmitLocalByte(0x5c, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L08f7;
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x5f);
			if (this.processor.FlagSet.IsNotEqual) goto L08fa;

		L08f7:
			this.processor.EmitLocalWord(0x5a, this.processor.IncrementWord(this.processor.PullLocalWord(0x5a)));

		L08fa:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));

		L08fd:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x658e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0963;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2e2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0920;
			this.processor.RegAcc.Value16 = arg4;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x5a));
			
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L08de;

		L0920:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x6550));
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, charsAddr);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 - 0x1), 0x0);
			this.processor.CompareWord(this.processor.PullLocalWord(0x5a), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0939;
			goto L0849;

		L0939:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x5a);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd7f2));
			if (this.processor.FlagSet.IsNotEqual) goto L094a;
			goto L0849;

		L094a:
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x654e));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, charsAddr);
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, 0x5e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x5a), 0x0);
			if (this.processor.FlagSet.IsLess) goto L095d;
			goto L0808;

		L095d:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x654c);
			goto L081d;

		L0963:
			this.processor.RegAcc.Value16 = arg4;
			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("OperateMenu");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		public void FloodBoxWithDoubleShadow(int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			// function body
			if (style == 7 && this.processor.PullGlobalWord(0x2f98) != 0)
			{
				this.owner.Helpers.FloodBoxWithMotif(horizLoc + 1, vertLoc + 1, breadth, altitude);
			}
			else
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
					horizLoc + 1, vertLoc + 1, breadth, altitude, style);
			}
		
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xe), style);

			RenderShadowBox(horizLoc + 1, vertLoc + 1, breadth, altitude, 15, 8);

			RenderBox(horizLoc, vertLoc, breadth + 2, altitude + 2, 0);
		}

		/// <summary>
		/// Draws a rectangle
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		public void RenderBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			// function body
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizLoc + breadth, vertLoc, style);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc + altitude, horizLoc + breadth, vertLoc + altitude, style);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc + breadth, vertLoc, horizLoc + breadth, vertLoc + altitude, style);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizLoc, vertLoc + altitude, style);
		}

		/// <summary>
		/// Draws a shaddow rectangle
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		/// <param name="mode1"></param>
		public void RenderShadowBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style, ushort style1)
		{
			// function body
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizLoc + breadth, vertLoc, style1);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc + altitude, horizLoc + breadth, vertLoc + altitude, style);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc + breadth, vertLoc, horizLoc + breadth, vertLoc + altitude, style1);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc + 1, horizLoc, vertLoc + altitude, style);
		}

		/// <summary>
		/// Reads a key and gives the navigation keys one code each (the other codes of the arrows, Home and the shifted keys of
		/// the keypad map to the usual ones); any other key with a character loses its scan code (high byte)
		/// </summary>
		public ushort ObtainNavigationKey()
		{
			this.processor.Journal.OpenScope("ObtainNavigationKey()");

			// The key stays in the stack frame, like in the original
			this.processor.EnterFrame(0x2);

			ushort key = (ushort)this.owner.Libc.ReadKey();

			key = key switch
			{
				0x4800 or 0x487e => 0x4800, // Up
				0x4900 => 0x4900, // Page Up
				0x4d00 or 0xf400 => 0x4d00, // Right
				0x5100 => 0x5100, // Page Down
				0x5000 or 0x5060 => 0x5000, // Down
				0x4f00 => 0x4f00, // End
				0x4b00 or 0x4b7c => 0x4b00, // Left
				0x4700 or 0x475c => 0x4700, // Home
				0x4838 => 0x4838, // Shift + Up
				0x4939 => 0x4939, // Shift + Page Up
				0x4d36 or 0x4d46 => 0x4d36, // Shift + Right
				0x5133 => 0x5133, // Shift + Page Down
				0x5032 => 0x5032, // Shift + Down
				0x4f31 => 0x4f31, // Shift + End
				0x4b34 or 0x4b43 => 0x4b34, // Shift + Left
				0x4737 => 0x4737, // Shift + Home
				_ => (key & 0xff) != 0 ? (ushort)(key & 0xff) : key
			};

			this.processor.EmitLocalWord(0x2, key);
			this.processor.RegAcc.Value16 = key;

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ObtainNavigationKey");

			return key;
		}
	}
}
