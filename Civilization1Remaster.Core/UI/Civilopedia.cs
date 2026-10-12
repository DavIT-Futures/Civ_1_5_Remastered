using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	public class Civilopedia
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public Civilopedia(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// The Civilopedia, at the part chosen in its menu: shows pages of the index until one answers 0xfffe (leave);
		/// 0xffff turns to the next page (78 entries further) when there is one (0x6808), else back to the first
		/// </summary>
		public void DisplayCivilopedia(ushort part)
		{
			this.processor.Journal.OpenScope($"DisplayCivilopedia({part})");

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			ushort firstEntry = 0;
			ushort answer;

			do
			{
				answer = DisplayCivilopediaIndex(firstEntry, part);

				if (answer == 0xffff)
				{
					firstEntry = this.processor.PullGlobalWord(0x6808) != 0 ? (ushort)(firstEntry + 0x4e) : (ushort)0;
				}
			}
			while (answer != 0xfffe);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Helpers.LaunchHueCycling();

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayCivilopedia");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <returns></returns>
		public ushort DisplayCivilopediaIndex(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayCivilopediaIndex({arg1}, {arg2})");

			// function body
			this.processor.EnterFrame(0x5c);
			this.processor.PreserveIndexRegisters();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 14);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x4c), "a");

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 60, 2, 200, 9, 15);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3c90, 161, 4, 0);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3cae, 160, 3, 10);

			this.owner.TextPainter.RenderTextToScreen0(0x3ccc, 286, 4, 12);
			
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L011f;

			this.owner.TextPainter.RenderTextToScreen0(0x3cd1, 8, 4, 12);

		L011f:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 2, 14, 316, 184, 15);

			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsGreater) goto L014a;
			this.processor.RegAcc.Value16 = 0x64;
			goto L014d;

		L014a:
			this.processor.RegAcc.Value16 = 0x96;

		L014d:
			this.processor.EmitLocalWord(0x1a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4e, 0xa);
			this.processor.EmitLocalWord(0x50, 0x10);
			this.processor.EmitLocalWord(0x58, 0x0);

		L015f:
			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x32), "zzz");

			this.processor.EmitLocalWord(0x5a, 0xffff);
			this.processor.EmitLocalWord(0x34, 0xffff);
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0185;
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L01d9;

		L0185:
			this.processor.EmitLocalWord(0x52, 0x0);

		L018a:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x52));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x4da);
			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x4c));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L01d0;

			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01d0;
			this.processor.CopyLocalWord(0x52, 0x5a);
			this.processor.EmitLocalWord(0x34, 0x0);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x32), this.processor.RegSource.Value16);

		L01d0:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0x43);
			if (this.processor.FlagSet.IsLess) goto L018a;

		L01d9:
			this.processor.CompareWord(arg2, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L01e5;
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0239;

		L01e5:
			this.processor.EmitLocalWord(0x52, 0x1);

		L01ea:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x52));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0xb9a);
			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x4c));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L0230;

			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0230;
			this.processor.CopyLocalWord(0x52, 0x5a);
			this.processor.EmitLocalWord(0x34, 0x1);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x32), this.processor.RegSource.Value16);

		L0230:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0x2d);
			if (this.processor.FlagSet.IsLessOrEqual) goto L01ea;

		L0239:
			this.processor.CompareWord(arg2, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0245;
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0299;

		L0245:
			this.processor.EmitLocalWord(0x52, 0x0);

		L024a:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x52));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x112a);
			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x4c));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L0290;

			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0290;
			this.processor.CopyLocalWord(0x52, 0x5a);
			this.processor.EmitLocalWord(0x34, 0x2);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x32), this.processor.RegSource.Value16);

		L0290:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L024a;

		L0299:
			this.processor.CompareWord(arg2, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L02a5;
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L02f9;

		L02a5:
			this.processor.EmitLocalWord(0x52, 0x0);

		L02aa:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x52));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x282);
			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x4c));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L02f0;

			this.owner.Libc.CompareTextNoCase(this.processor.RegSource.Value16, (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02f0;
			this.processor.CopyLocalWord(0x52, 0x5a);
			this.processor.EmitLocalWord(0x34, 0x3);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x32), this.processor.RegSource.Value16);

		L02f0:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0xc);
			if (this.processor.FlagSet.IsLess) goto L02aa;

		L02f9:
			this.processor.CompareWord(arg2, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0305;
			this.processor.CompareWord(arg2, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0359;

		L0305:
			this.processor.EmitLocalWord(0x52, 0x0);

		L030a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x52);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x3c64);
			this.owner.Libc.CompareTextNoCase(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), (ushort)(this.processor.RegFrame.Value16 - 0x4c));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLessOrEqual) goto L0350;

			this.owner.Libc.CompareTextNoCase(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0350;
			this.processor.CopyLocalWord(0x52, 0x5a);
			this.processor.EmitLocalWord(0x34, 0x4);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x32), this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16));

		L0350:
			this.processor.EmitLocalWord(0x52, this.processor.IncrementWord(this.processor.PullLocalWord(0x52)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x52), 0x15);
			if (this.processor.FlagSet.IsLess) goto L030a;

		L0359:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0362;
			goto L03f1;

		L0362:
			this.processor.RegAcc.Value16 = arg1;
			this.processor.CompareWord(this.processor.PullLocalWord(0x58), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L03d4;

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x18), (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.owner.BlockText.ShrinkTextToWidth(
				(ushort)(this.processor.RegFrame.Value16 - 0x18),
				this.processor.PullLocalWord(0x1a));

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(this.processor.RegFrame.Value16 - 0x18),
				this.processor.PullLocalSignedWord(0x4e),
				this.processor.PullLocalSignedWord(0x50),
				0);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, arg1);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x5a);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1c80), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x34);
			this.processor.SegExtra.Value16 = 0x3772; // segment
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x36fc), this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x50, this.processor.SumWord(this.processor.PullLocalWord(0x50), 0x7));
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0xc0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L03d4;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.EmitLocalWord(0x4e, this.processor.SumWord(this.processor.PullLocalWord(0x4e), this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0x50, 0x10);

		L03d4:
			this.processor.EmitLocalWord(0x58, this.processor.IncrementWord(this.processor.PullLocalWord(0x58)));

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x4c), (ushort)(this.processor.RegFrame.Value16 - 0x32));

			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x12c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L03f1;
			goto L015f;

		L03f1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x12c);
			if (this.processor.FlagSet.IsLess) goto L03fd;
			this.processor.RegAcc.Value16 = 0x1;
			goto L03ff;

		L03fd:
			this.processor.RegAcc.Value16 = 0;

		L03ff:
			this.processor.EmitGlobalWord(0x6808, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L040c;
			goto L0492;

		L040c:
			this.owner.Startup.DisplayMouseCursor();

		L0411:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0426;

			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0411;

		L0426:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.owner.GlobDb3e, 0x10);
			if (this.processor.FlagSet.IsLess) goto L044d;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x7;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			goto L0452;

		L044d:
			this.processor.EmitLocalWord(0x50, 0xffff);

		L0452:
			this.processor.RegAcc.Value16 = 0x1a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4e));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			this.processor.EmitLocalWord(0x56, this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0474;
			this.processor.EmitLocalWord(0x50, 0xffff);
			this.processor.EmitLocalWord(0x4e, 0x2);

		L0474:
			this.owner.Libc.KeyAvailable();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0480;
			goto L05b7;

		L0480:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x50, 0xffff);
			this.processor.EmitLocalWord(0x4e, 0x2);
			goto L05b7;

		L0492:
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);

		L049a:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x1a));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x8);
			
			this.processor.ScaleIndex(0x7, this.processor.PullLocalWord(0x50));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0xf);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x1a);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x5c, this.processor.RegAcc.Value16);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				(short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16,
				this.processor.PullLocalSignedWord(0x5c), 8, 15, 10);

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0x54, this.processor.RegAcc.Value16);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				(short)this.processor.RegSource.Value16, (short)this.processor.RegDest.Value16,
				this.processor.PullLocalSignedWord(0x5c), 8, 10, 15);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x54);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsEqual) goto L0513;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b00);
			if (this.processor.FlagSet.IsEqual) goto L057f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d00);
			if (this.processor.FlagSet.IsEqual) goto L0538;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsEqual) goto L0553;
			goto L052c;

		L0513:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L051e;
			this.processor.DecrementLocalWord(0x50);
			goto L052c;

		L051e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L052c;
			this.processor.DecrementLocalWord(0x4e);
			this.processor.EmitLocalWord(0x50, 0x19);

		L052c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x6d);
			if (this.processor.FlagSet.IsNotEqual) goto L058a;
			this.processor.RegAcc.Value16 = 0xffff;
			goto L0624;

		L0538:
			this.processor.RegAcc.Value16 = 0x1a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4e));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x1a);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, arg1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L052c;

		L054e:
			this.processor.EmitLocalWord(0x4e, this.processor.IncrementWord(this.processor.PullLocalWord(0x4e)));
			goto L052c;

		L0553:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, arg1);
			this.processor.ScaleIndex(0x1a, this.processor.PullLocalWord(0x4e));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.PullLocalWord(0x50));
			this.processor.RegDest.Value16 = this.processor.IncrementWord(this.processor.RegDest.Value16);
			this.processor.CompareWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L052c;
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x19);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0578;
			this.processor.CompareWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0578;
			this.processor.IncrementLocalWord(0x50);
			goto L052c;

		L0578:
			this.processor.EmitLocalWord(0x50, 0x0);
			goto L054e;

		L057f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L052c;
			this.processor.DecrementLocalWord(0x4e);
			goto L052c;

		L058a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L0596;
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x78);
			if (this.processor.FlagSet.IsNotEqual) goto L059c;

		L0596:
			this.processor.RegAcc.Value16 = 0xfffe;
			goto L0624;

		L059c:
			this.processor.RegAcc.Value16 = 0x1a;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4e));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			this.processor.EmitLocalWord(0x56, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0xd);
			if (this.processor.FlagSet.IsEqual) goto L05b7;
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L05b7;
			goto L049a;

		L05b7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsLess) goto L05cc;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x58);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, arg1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x56));
			if (this.processor.FlagSet.IsGreater) goto L05cc;
			this.processor.RegAcc.Value16 = 0;
			goto L0624;

		L05cc:
			this.processor.CompareWord(this.processor.PullLocalWord(0x50), 0x0);
			if (this.processor.FlagSet.IsLess) goto L060b;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xe17c), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L05eb;

			this.owner.Startup.QueryForDisk(3, 1);

		L05eb:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.SegExtra.Value16 = 0x3772; // segment
			DisplayCivilopediaRecord(this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1c80)),
				this.processor.PullUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegSource.Value16 + 0x36fc)));
			
			goto L061c;

		L060b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0616;
			this.processor.RegAcc.Value16 = 0xfffe;
			goto L0619;

		L0616:
			this.processor.RegAcc.Value16 = 0xffff;

		L0619:
			this.processor.EmitLocalWord(0x56, this.processor.RegAcc.Value16);

		L061c:
			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x56);

		L0624:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilopediaIndex");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="technologyID"></param>
		/// <param name="param2"></param>
		public void DisplayCivilopediaRecord(ushort scienceIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayCivilopediaRecord({scienceIdx}, {arg2})");

			// function body
			this.processor.EnterFrame(0x34);
			this.processor.PreserveIndexRegisters();
			this.processor.RegAcc.LowByte = (byte)arg2;
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x30);
			this.processor.EmitGlobalByte(0x3fa9, this.processor.RegAcc.LowByte);

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			this.processor.EmitLocalWord(0x6, 0xffff);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xe17c), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L066d;

			this.owner.Startup.QueryForDisk(3, 0);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L066d;
			this.processor.EmitGlobalWord(0x3c62, 0x1);

		L066d:
			this.processor.EmitLocalWord(0x2, 0xf);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L069e;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3c62), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0687;
			this.processor.EmitLocalWord(0x2, 0xff);
			goto L069e;

		L0687:
			this.processor.EmitLocalWord(0x2, 0xfd);

			this.owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(0x3d, 0x3d, 0x3d));

		L069e:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200,
				this.processor.PullLocalWord(0x2));

			this.owner.UnitOps.RenderBusyIndicator();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x7);
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.EmitLocalWord(0x32, 0xcc);
			this.processor.CompareWord(arg2, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L06dc;
			this.processor.EmitLocalWord(0x32, 0xe0);

		L06dc:
			this.processor.CompareWord(arg2, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L06f5;
			this.processor.CompareWord(arg2, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L06ee;
			this.processor.CompareWord(scienceIdx, 0x18);
			if (this.processor.FlagSet.IsGreater) goto L06f5;

		L06ee:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3c62), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L06fa;

		L06f5:
			this.processor.EmitLocalWord(0x32, 0xa0);

		L06fa:
			this.processor.RegAcc.Value16 = arg2;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0724;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0709;
			goto L08ea;

		L0709:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L0711;
			goto L09d8;

		L0711:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0719;
			goto L0aac;

		L0719:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L0721;
			goto L0b8f;

		L0721:
			goto L07e2;

		L0724:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3c62), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L072e;
			goto L07b5;

		L072e:
			ObtainFirstPrerequisite(scienceIdx);
			
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "iconpg1.pic");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x34);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x9;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalByte(0xba0c, this.processor.SumByte(this.processor.PullGlobalByte(0xba0c), this.processor.RegAcc.LowByte));

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xba06, 0);

			this.owner.Helpers.PullAndAssignColors(0x3ce6);

			RenderIcon(
				(ushort)(((this.processor.PullLocalWord(0x34) % 3) * 111) + 1),
				(ushort)((((this.processor.PullLocalWord(0x34) % 9) / 3) * 69) + 1),
				110, 68, 62, 42);

		L07b5:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, scienceIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3cf2, this.processor.PullLocalSignedWord(0x32), 36, 7);

		L07e2:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x6);

			this.owner.Libc.UppercaseText(0xba06);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, this.processor.PullLocalSignedWord(0x32), 20, 0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x7);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x2c), "*");

			this.owner.Libc.AppendText((ushort)(this.processor.RegFrame.Value16 - 0x2c), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.EmitLocalWord(0xc, 0x4c);
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0846;
			goto L08e4;

		L0846:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x40);
			if (this.processor.FlagSet.IsEqual) goto L089c;

			this.owner.BlockText.ImportTextFromFile(0x3fa4, (ushort)(this.processor.RegFrame.Value16 - 0x2c));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L089c;

			this.owner.BlockText.RenderTextChunk(37, 12, 76, 1);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 8, 76, 304, 116,
				this.processor.PullLocalWord(0x2));

		L089c:
			this.owner.Libc.AppendText((ushort)(this.processor.RegFrame.Value16 - 0x2c), "2");

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.CompareWord(arg2, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L08e4;

			this.owner.BlockText.ImportTextFromFile(0x3fa4, (ushort)(this.processor.RegFrame.Value16 - 0x2c));

			this.owner.BlockText.RenderTextChunk(36, 12, this.processor.PullLocalSignedWord(0xc), 1);

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

		L08e4:
			this.processor.RegAcc.Value16 = arg2;
			goto L1636;

		L08ea:
			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0904;
			this.processor.EmitGlobalWord(0x3c62, 0x1);

		L0904:
			this.processor.CompareWord(scienceIdx, 0x1);
			if (this.processor.FlagSet.IsGreater) goto L090d;
			goto L09a5;

		L090d:
			this.processor.CompareWord(scienceIdx, 0x15);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0916;
			goto L09a5;

		L0916:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3c62), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0920;
			goto L09a5;

		L0920:
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3d07, 0);

			this.owner.Helpers.PullAndAssignColors(0x3d14);

			this.processor.RegSource.Value16 = scienceIdx;
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, 0x2);
			
			this.owner.VideoCard.ScreenToPicture(1,
				(ushort)(((this.processor.RegSource.Value16 >> 2) * 0x32) + 1),
				(ushort)(((this.processor.RegSource.Value16 & 3) * 0x32) + 1),
				0x31, 0x31);

			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				37, 17,
				this.processor.PullLocalWord(0xa));

			this.owner.Helpers.ReleaseResource(this.processor.PullLocalWord(0xa), 0);

		L09a5:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, scienceIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareWord(scienceIdx, 0x18);
			if (this.processor.FlagSet.IsGreater)
			{
				this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d2e, this.processor.PullLocalSignedWord(0x32), 36, 7);
			}
			else
			{
				this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d1d, this.processor.PullLocalSignedWord(0x32), 36, 7);
			}

			goto L07e2;

		L09d8:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3c62), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L09e2;
			goto L0a62;

		L09e2:
			ObtainSecondPrerequisite(scienceIdx);
			
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "iconpga.pic");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x34);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalByte(0xba0c, this.processor.SumByte(this.processor.PullGlobalByte(0xba0c), this.processor.RegAcc.LowByte));

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xba06, 0);

			this.owner.Helpers.PullAndAssignColors(0x3d4e);

			RenderIcon(
				(ushort)(((this.processor.PullLocalWord(0x34) & 1) * 160) + 1),
				(ushort)(((this.processor.PullLocalWord(0x34) % 6) >> 1) * 61),
				158, 61, 88, 42);

		L0a62:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, scienceIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d5a, this.processor.PullLocalSignedWord(0x32), 36, 7);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				216, 48,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((scienceIdx << 1) + 0xd58e)));
			goto L07e2;

		L0aac:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3c62), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0ab6;
			goto L0b68;

		L0ab6:
			ObtainTechnologyCategory(scienceIdx);
			
			this.processor.EmitLocalWord(0x34, this.processor.RegAcc.Value16);

			this.owner.Libc.CopyText(0xba06, "iconpgt1.pic");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x34);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalByte(0xba0d, this.processor.SumByte(this.processor.PullGlobalByte(0xba0d), this.processor.RegAcc.LowByte));

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xba06, 0);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0b25;

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x3d75, 0xc5be);

			this.owner.VideoCard.AssignHuesFromHueStruct(0xc5be);
			
			this.owner.VideoCard.AssignHue(0xfd, VGABitmap.Hue18ToHue(0x3d, 0x3d, 0x3d));

		L0b25:
			RenderIcon(
				(ushort)(((this.processor.PullLocalSignedWord(0x34) % 3) * 107) + 1),
				(ushort)((((this.processor.PullLocalSignedWord(0x34) % 6) / 3) * 87) + 5),
				106, 77, 76, 46);

		L0b68:
			this.processor.EmitGlobalByte(0xba06, 0);

			this.processor.ScaleIndex(0x13, scienceIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x282);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d7f, this.processor.PullLocalSignedWord(0x32), 36, 7);

			goto L07e2;

		L0b8f:
			this.processor.RegBase.Value16 = scienceIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x3c64)));

			this.owner.TextPainter.RenderCenteredTextToScreen0(0x3d8c, this.processor.PullLocalSignedWord(0x32), 36, 7);

			goto L07e2;

		L0bb5:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToSource(0x16, scienceIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x4ee);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0bf3;

			this.owner.Libc.CopyText(0xba06, "Requires ");

			this.processor.RegAcc.LowByte = 0x16;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L0bf3:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.ScaleAccumulatorToSource(scienceIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x4ef);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0c28;

			this.owner.Libc.AppendText(0xba06, " and ");

			this.processor.RegAcc.LowByte = 0x16;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L0c28:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 32, this.processor.PullLocalSignedWord(0xc), 1);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x10));

			this.owner.TextPainter.RenderTextToScreen0(0x3dad, 32, this.processor.PullLocalSignedWord(0xc), 1);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));
			this.processor.CopyLocalWord(0xc, 0x30);
			this.processor.EmitLocalWord(0x2e, 0x0);

		L0c69:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2e));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, scienceIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L0ce5;

			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0cca;

			this.owner.Libc.AppendText(0xba06, " (with ");

			this.processor.RegAcc.LowByte = 0x16;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ")");

		L0cca:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 40, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

		L0ce5:
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2e));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ef));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, scienceIdx);
			if (this.processor.FlagSet.IsNotEqual) goto L0d61;

			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0d46;

			this.owner.Libc.AppendText(0xba06, " (with ");

			this.processor.RegAcc.LowByte = 0x16;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x4ee)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ")");

		L0d46:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 40, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

		L0d61:
			this.processor.EmitLocalWord(0x2e, this.processor.IncrementWord(this.processor.PullLocalWord(0x2e)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x48);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0d6d;
			goto L0c69;

		L0d6d:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x4));
			this.processor.EmitLocalWord(0x2e, 0x0);

		L0d76:
			this.processor.RegAcc.Value16 = 0x22;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2e));
			this.processor.RegAcc.Value16 = scienceIdx;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x114a)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0de8;

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				40,
				this.processor.PullLocalSignedWord(0xc) - 4,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((this.processor.PullLocalWord(0x2e) +
					(this.owner.World.PersonParticipantIdx << 5)) + 0x40) << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " unit");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 60, this.processor.PullLocalSignedWord(0xc), 12);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0xc));

		L0de8:
			this.processor.EmitLocalWord(0x2e, this.processor.IncrementWord(this.processor.PullLocalWord(0x2e)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x1c);
			if (this.processor.FlagSet.IsLess) goto L0d76;
			this.processor.EmitLocalWord(0x2e, 0x1);
			goto L0e26;

		L0df8:
			this.owner.Libc.AppendText(0xba06, " Wonder");

		L0dfb:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 60, this.processor.PullLocalSignedWord(0xc), 2);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0xc));

		L0e23:
			this.processor.EmitLocalWord(0x2e, this.processor.IncrementWord(this.processor.PullLocalWord(0x2e)));

		L0e26:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x2d);
			if (this.processor.FlagSet.IsGreater) goto L0e6f;
			this.processor.ScaleIndexToSource(0x1e, this.processor.PullLocalWord(0x2e));
			this.processor.RegAcc.Value16 = scienceIdx;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb6)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0e23;

			this.owner.CityOps.RenderResourceIcon(this.processor.PullLocalSignedWord(0x2e),
				40, (short)(this.processor.PullLocalSignedWord(0xc) - 2));

			this.processor.RegAcc.Value16 = this.processor.RegSource.Value16;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L0df8;

			this.owner.Libc.AppendText(0xba06, " improvement");

			goto L0dfb;

		L0e6f:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x30);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc));
			if (this.processor.FlagSet.IsNotEqual) goto L0e94;

			this.owner.TextPainter.RenderTextToScreen0(0x3de5, 
				32, 
				this.processor.PullLocalSignedWord(0xc) - 12,
				this.processor.PullLocalWord(0x2));

		L0e94:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ScienceObtainedSince[scienceIdx];
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xfffe);
			if (this.processor.FlagSet.IsEqual) goto L0f01;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0f2b;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0f32;

			this.owner.Libc.CopyText(0xba06, "(Taken from ");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ScienceObtainedSince[scienceIdx];
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ")");
			goto L0f11;

		L0f01:
			this.processor.RegAcc.Value16 = 0x3dee;

		L0f04:
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

		L0f11:
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, this.processor.PullLocalSignedWord(0x32), 48, 7);

			goto L1655;

		L0f2b:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			goto L0f11;

		L0f32:
			this.processor.RegAcc.Value16 = 0x3e03;
			goto L0f04;

		L0f37:
			this.owner.Libc.CopyText(0xba06, "Requires ");

			this.processor.ScaleIndexToSource(0x1e, scienceIdx);
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb6)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 12, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Cost: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(0xa * (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb2))), 10));

			this.owner.Libc.AppendText(0xba06, " shields.");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 12, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Maintenance: $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbb4)), 10));

		L1005:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 12, this.processor.PullLocalSignedWord(0xc), 12);

		L1019:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));
			goto L1655;

		L102d:
			this.owner.Libc.CopyText(0xba06, "Requires ");

			this.processor.ScaleIndexToBase(0x22, scienceIdx);
			this.processor.RegAcc.Value16 = 0x16;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x114a)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.processor.CompareWord(scienceIdx, 0x19);
			if (this.processor.FlagSet.IsNotEqual) goto L1072;

			this.owner.Libc.AppendText(0xba06, " and Nuclear Fission");

		L1072:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 100, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Cost: ");

			this.processor.ScaleIndexToSource(0x22, scienceIdx);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(0xa * (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1142))), 10));

			this.owner.Libc.AppendText(0xba06, " resources.");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 100, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Attack Strength: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113e)), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 100, this.processor.PullLocalSignedWord(0xc), 12);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Defense Strength: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1140)), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 100, this.processor.PullLocalSignedWord(0xc), 12);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Movement Rate: ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x113a)), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 100, this.processor.PullLocalSignedWord(0xc), 0);

			goto L1019;

		L11ca:
			this.processor.RegAcc.Value16 = scienceIdx;
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			goto L1552;

		L11d3:
			this.processor.RegAcc.Value16 = scienceIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L11ed;
			this.processor.ScaleIndexToBase(0x13, scienceIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x290)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L11ed;
			goto L154e;

		L11ed:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));

			this.owner.TextPainter.RenderTextToScreen0(
				(ushort)(this.processor.RegSource.Value16 + 0x282), 12, this.processor.PullLocalSignedWord(0xc), 1);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x290)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L122b;
			this.processor.ScaleIndexToBase(0xc, scienceIdx);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)), 0xffff);
			if (this.processor.FlagSet.IsLess) goto L122b;
			goto L131e;

		L122b:
			this.owner.Libc.CopyText(0xba06, "Food: ");

			this.processor.ScaleIndexToSource(0x13, this.processor.PullLocalWord(0x4));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x290);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 10));

			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x3);
			if (this.processor.FlagSet.IsLess) goto L127d;

			this.owner.Libc.AppendText(0xba06, "*");

		L127d:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(scienceIdx);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x44a);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0xffff);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L12f3;

			this.owner.Libc.AppendText(0xba06, " (");

			this.processor.ScaleIndex(0x13, this.processor.PullLocalWord(0x4));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x290);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16)) -
					(short)this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16) - 1), 10));

			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16), 0x2);
			if (this.processor.FlagSet.IsLess) goto L12e3;

			this.owner.Libc.AppendText(0xba06, "*");

		L12e3:
			this.owner.Libc.AppendText(0xba06, " with Irrigation)");

		L12f3:
			this.owner.Libc.AppendText(0xba06, " units.");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

		L131e:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x291)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L133f;
			this.processor.ScaleIndexToBase(0xc, scienceIdx);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e)), 0xffff);
			if (this.processor.FlagSet.IsLess) goto L133f;
			goto L1433;

		L133f:
			this.owner.Libc.CopyText(0xba06, "Production: ");

			this.processor.CompareWord(scienceIdx, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L135b;
			this.processor.CompareWord(scienceIdx, 0xb);
			if (this.processor.FlagSet.IsNotEqual) goto L136b;

		L135b:
			this.owner.Libc.AppendText(0xba06, "0/");

		L136b:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x291);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 10));

			this.processor.ScaleIndex(0xc, scienceIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x44e);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16), 0xffff);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1408;

			this.owner.Libc.AppendText(0xba06, " (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16)) -
						(short)this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16) - 1), 10));

			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16));
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsLess) goto L13f8;

			this.owner.Libc.AppendText(0xba06, "*");

		L13f8:
			this.owner.Libc.AppendText(0xba06, " with Mining)");

		L1408:
			this.owner.Libc.AppendText(0xba06, " units.");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

		L1433:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x292)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L144b;
			this.processor.CompareWord(scienceIdx, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L144b;
			goto L1529;

		L144b:
			this.owner.Libc.CopyText(0xba06, "Trade: ");

			this.processor.ScaleIndexToSource(0x13, this.processor.PullLocalWord(0x4));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x292);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 10));

			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L149d;

			this.owner.Libc.AppendText(0xba06, "%");

		L149d:
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x292)), 0x3);
			if (this.processor.FlagSet.IsLess) goto L14bc;

			this.owner.Libc.AppendText(0xba06, "*");

		L14bc:
			this.processor.CompareWord(scienceIdx, 0x2);
			if (this.processor.FlagSet.IsGreater) goto L150e;

			this.owner.Libc.AppendText(0xba06, " (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16,
					(ushort)(0x13 * (short)this.processor.PullLocalWord(0x4) + 0x292))) + 1), 10));

			this.owner.Libc.AppendText(0xba06, "% with Roads)");

		L150e:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 16, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

		L1529:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x7);
			if (this.processor.FlagSet.IsNotEqual) goto L154a;

			this.owner.TextPainter.RenderTextToScreen0(0x3f20, 16, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

		L154a:
			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x4));

		L154e:
			this.processor.EmitLocalWord(0x4, this.processor.SumWord(this.processor.PullLocalWord(0x4), 0xc));

		L1552:
			this.processor.RegAcc.Value16 = scienceIdx;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			if (this.processor.FlagSet.IsLess) goto L1560;
			goto L11d3;

		L1560:
			this.owner.TextPainter.RenderTextToScreen0(0x3f28, 16, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.TextPainter.RenderTextToScreen0(0x3f52, 16, this.processor.PullLocalSignedWord(0xc), 9);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0xc));

			this.owner.Libc.CopyText(0xba06, "Movement cost: ");

			this.processor.ScaleIndexToSource(0x13, scienceIdx);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x28e)), 10));

			this.owner.Libc.AppendText(0xba06, " MP");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 12, this.processor.PullLocalSignedWord(0xc), 12);

			this.processor.EmitLocalWord(0xc, this.processor.SumWord(this.processor.PullLocalWord(0xc), 0x8));

			this.owner.Libc.CopyText(0xba06, "Defense bonus: +");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((0x32 * (short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x28f)))) - 0x64),
					10));

			this.owner.Libc.AppendText(0xba06, "%");
			goto L1005;

		L1636:
			this.processor.RegAcc.Value16 = this.processor.BitOrWord(this.processor.RegAcc.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L163d;
			goto L0bb5;

		L163d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1645;
			goto L0f37;

		L1645:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L164d;
			goto L102d;

		L164d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L1655;
			goto L11ca;

		L1655:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.ConcealMouseCursor();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L168b;
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L168b;

			this.owner.Startup.QueryForDisk(0, 1);

		L168b:
			this.processor.EmitGlobalWord(0x3c62, 0x0);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L16b9;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 8, 8, 304, 184,
				this.processor.PullLocalWord(0x2));

			this.owner.Helpers.LaunchHueCycling();

		L16b9:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilopediaRecord");
		}

		/// <summary>
		/// Index of the technology in the list of the technologies that lead to others first (72 bytes at 0x371e:0000)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		public ushort ObtainFirstPrerequisite(ushort scienceIdx)
		{
			this.processor.Journal.OpenScope($"ObtainFirstPrerequisite({scienceIdx})");

			ushort found = LocateInScienceList(0x0, 0x48, scienceIdx);

			this.processor.Journal.CloseScope("ObtainFirstPrerequisite");

			return found;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		/// <param name="param4"></param>
		/// <param name="param5"></param>
		/// <param name="param6"></param>
		public void RenderIcon(ushort arg1, ushort arg2, ushort arg3, ushort arg4, ushort arg5, ushort arg6)
		{
			this.processor.Journal.OpenScope($"RenderIcon({arg1}, {arg2}, {arg3}, {arg4}, {arg5}, {arg6})");

			// function body
			this.processor.EnterFrame(0x4);
			this.processor.PreserveSource();
			this.processor.RegAcc.Value16 = arg1;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, arg3);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x13f);
			if (this.processor.FlagSet.IsLessOrEqual) goto L171a;
			this.processor.EmitLocalWord(0x2, 0x13f);
			this.processor.RegAcc.Value16 = 0x13f;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, arg1);
			arg3 = this.processor.RegAcc.Value16;

		L171a:
			this.processor.RegAcc.Value16 = arg2;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, arg4);
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc7);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1736;
			this.processor.EmitLocalWord(0x4, 0xc7);
			this.processor.RegAcc.Value16 = 0xc7;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, arg2);
			arg4 = this.processor.RegAcc.Value16;

		L1736:
			this.processor.RegAcc.Value16 = arg3;
			this.processor.HalveAccumulator();
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(arg5, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1753;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, arg5);
			arg1 += this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = arg5;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			arg3 = this.processor.RegAcc.Value16;

		L1753:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				arg1,
				arg2,
				arg3,
				arg4,
				this.processor.PullGlobalWord(0xaa),
				(ushort)(arg5 - (arg3 >> 1)),
				(ushort)(arg6 - (arg4 >> 1)));

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderIcon");
		}

		/// <summary>
		/// Index of the technology in the second list of the Civilopedia (28 bytes at 0x371e:0044)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		public ushort ObtainSecondPrerequisite(ushort scienceIdx)
		{
			this.processor.Journal.OpenScope($"ObtainSecondPrerequisite({scienceIdx})");

			ushort found = LocateInScienceList(0x44, 0x1c, scienceIdx);

			this.processor.Journal.CloseScope("ObtainSecondPrerequisite");

			return found;
		}

		/// <summary>
		/// Index of the technology in the list of the technology categories of the Civilopedia (12 bytes at 0x371e:0061)
		/// </summary>
		/// <returns>Index, or 0xffff; also in AX</returns>
		public ushort ObtainTechnologyCategory(ushort scienceIdx)
		{
			this.processor.Journal.OpenScope($"ObtainTechnologyCategory({scienceIdx})");

			ushort found = LocateInScienceList(0x61, 0xc, scienceIdx);

			this.processor.Journal.CloseScope("ObtainTechnologyCategory");

			return found;
		}

		/// <summary>
		/// The first index of the technology in a list of signed bytes in the segment 0x371e. Like the original, ES is left
		/// at the segment and BX at the last index looked at.
		/// </summary>
		/// <returns>Index, or 0xffff when the technology is not in the list; also in AX</returns>
		private ushort LocateInScienceList(ushort origin, int tally, ushort scienceIdx)
		{
			ushort found = 0xffff;

			for (int idx = 0; idx < tally; idx++)
			{
				this.processor.RegBase.Value16 = (ushort)idx;
				this.processor.SegExtra.Value16 = 0x371e;

				if ((ushort)(sbyte)this.processor.PullUInt8(this.processor.SegExtra.Value16, (ushort)(origin + idx)) == scienceIdx)
				{
					found = (ushort)idx;
					break;
				}
			}

			this.processor.RegAcc.Value16 = found;

			return found;
		}
	}
}
