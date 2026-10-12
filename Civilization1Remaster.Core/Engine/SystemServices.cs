using System;
using System.Drawing;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1000.
	/// </summary>
	public class SystemServices
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public SystemServices(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void LaunchTimer()
		{
			this.processor.Journal.OpenScope("LaunchTimer()");

			// function body
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.Scratch.LowByte = this.processor.PullGlobalByte(0x5a);
			this.processor.EmitGlobalByte(0x5a, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.Scratch.LowByte;
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L004f;
			this.processor.EmitGlobalWord(0x4c, 0x1);
			this.processor.EmitGlobalWord(0x54, 0x1);
			this.processor.EmitGlobalWord(0x42, 0x0);
			this.processor.EmitGlobalWord(0x44, 0x0);

			PrepareTimer();
			
			this.processor.AllowClock = true;

		L004f:
			// Far return
			this.processor.Journal.CloseScope("LaunchTimer");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void HaltTimer()
		{
			this.processor.Journal.OpenScope("HaltTimer()");

			// function body
			this.processor.RegAcc.LowByte = 0x0;
			this.processor.Scratch.LowByte = this.processor.PullGlobalByte(0x5a);
			this.processor.EmitGlobalByte(0x5a, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.Scratch.LowByte;
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsEqual) goto L0075;

		L0075:
			// Far return
			this.processor.Journal.CloseScope("HaltTimer");
		}

		/// <summary>
		/// Timer function, it should fire appoximately every 20-25 ms
		/// </summary>
		public void ClockInterrupt()
		{
			this.processor.Journal.OpenScope("ClockInterrupt()");

			// The interrupted registers are kept outside the emulated stack (the original pushes them onto the stack of the game):
			// what an interrupt leaves below SP would depend on when it comes, and the game reads uninitialized locals there
			(ushort frame, ushort acc, ushort basis, ushort count, ushort data, ushort source, ushort dest, ushort dataSection) =
				(this.processor.RegFrame.Value16, this.processor.RegAcc.Value16, this.processor.RegBase.Value16,
				this.processor.RegCount.Value16, this.processor.RegData.Value16, this.processor.RegSource.Value16,
				this.processor.RegDest.Value16, this.processor.SegData.Value16);

			this.processor.RegAcc.Value16 = this.processor.RegStack.Value16;
			this.processor.RegBase.Value16 = this.processor.SegStack.Value16;
			this.processor.SegStack.Value16 = 0x1000;
			this.processor.RegStack.Value16 = 0x1a1;
			this.processor.StackWord(this.processor.RegBase.Value16);
			this.processor.StackWord(this.processor.RegAcc.Value16);
			this.processor.SegData.Value16 = 0x3b01;

			this.processor.EmitUInt32(this.processor.SegData.Value16, 0x42, (uint)(this.processor.PullUInt32(this.processor.SegData.Value16, 0x42) +
				this.processor.PullGlobalWord(0x48)));

			this.processor.EmitGlobalWord(0x54, this.processor.DecrementWord(this.processor.PullGlobalWord(0x54)));
			if (this.processor.FlagSet.IsEqual)
			{
				this.processor.EmitGlobalWord(0x54, this.processor.PullGlobalWord(0x4c));

				this.processor.EmitGlobalByte(0x59, this.processor.DecrementByte(this.processor.PullGlobalByte(0x59)));
				if (this.processor.FlagSet.IsEqual)
				{
					this.processor.EmitGlobalByte(0x59, 0x14);

					if (this.processor.PullGlobalByte(0x58) != 0)
					{
						this.processor.EmitGlobalByte(0x58, 0x0);
					}
					else
					{
						if (this.processor.PullGlobalWord(0x46) != this.processor.PullGlobalWord(0x48))
						{
							this.processor.EmitGlobalWord(0x48, this.processor.PullGlobalWord(0x46));
							this.processor.EmitGlobalWord(0x4a, this.processor.IncrementWord(this.processor.PullGlobalWord(0x4a)));
							this.processor.EmitGlobalWord(0x56, 0x20);
						}
					}
				}

				OnClockBeat();
			}

			if (this.processor.PullGlobalWord(0x4c) != 0x1)
			{
				// sound driver call
				RefreshSoundEffects();
			}

			if (this.processor.PullGlobalWord(0x44) != 0x0)
			{
				this.processor.EmitGlobalWord(0x44, this.processor.DecrementWord(this.processor.PullGlobalWord(0x44)));
			}

			this.processor.RegAcc.Value16 = this.processor.UnstackWord();
			this.processor.SegStack.Value16 = this.processor.UnstackWord();
			this.processor.RegStack.Value16 = this.processor.RegAcc.Value16;

			(this.processor.RegFrame.Value16, this.processor.RegAcc.Value16, this.processor.RegBase.Value16,
				this.processor.RegCount.Value16, this.processor.RegData.Value16, this.processor.RegSource.Value16,
				this.processor.RegDest.Value16, this.processor.SegData.Value16) = (frame, acc, basis, count, data, source, dest, dataSection);
			// IRET - Pop flags and Far return

			this.processor.Journal.CloseScope("ClockInterrupt");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void PrepareTimer()
		{
			this.processor.Journal.OpenScope("PrepareTimer()");

			// function body
			this.processor.StackFlags();
			this.processor.EmitGlobalByte(0x59, 0x1);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitGlobalByte(0x58, 0x1);
			this.processor.EmitGlobalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x52, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 100;
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = 0x10;

		L0292:
			this.processor.StackWord(this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = 200;
			this.processor.RegBase.Value16 = this.processor.UnstackWord();
			this.processor.RegBase.Value16 = this.processor.SubtractWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x50, this.processor.SumWord(this.processor.PullGlobalWord(0x50), this.processor.RegBase.Value16));
			this.processor.EmitUInt16(this.processor.SegData.Value16, 0x52, this.processor.SumCarryWord(this.processor.PullGlobalWord(0x52), 0x0));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			if (this.processor.RepeatLoop(this.processor.RegCount)) goto L0292;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x50);
			this.processor.RegData.Value16 = this.processor.PullGlobalWord(0x52);
			this.processor.EmitGlobalWord(0x42, this.processor.SumWord(this.processor.PullGlobalWord(0x42), this.processor.RegAcc.Value16));
			this.processor.EmitUInt16(this.processor.SegData.Value16, 0x44, this.processor.SumCarryWord(this.processor.PullGlobalWord(0x44), this.processor.RegData.Value16));
			this.processor.RegCount.Value16 = 0x10;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0x50, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftRightWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.ShiftRightWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.ShiftRightWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.ShiftRightWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegBase.Value16 = 0xf89;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsBelow) goto L02df;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x6);
			if (this.processor.FlagSet.IsAbove) goto L02df;
			goto L02ed;

		L02df:
			this.processor.EmitGlobalByte(0x58, 0x0);
			this.processor.EmitGlobalWord(0x50, 0x4dae);
			this.processor.RegAcc.Value16 = 0x5;

		L02ed:
			this.processor.EmitGlobalWord(0x4e, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x4c), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L02fa;
			this.processor.EmitGlobalWord(0x4c, this.processor.RegAcc.Value16);

		L02fa:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x50);
			this.processor.RegData.Value16 = 0x0;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x4c));
			this.processor.EmitGlobalWord(0x48, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.UnstackFlags();
			// Near return
			this.processor.Journal.CloseScope("PrepareTimer");
		}

		/// <summary>
		/// Reset timer count
		/// </summary>
		/// <returns></returns>
		public ushort RestoreTimer()
		{
			this.processor.Journal.OpenScope("RestoreTimer()");

			// function body
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5c);
			this.processor.EmitGlobalWord(0x5c, 0);

			// Far return
			this.processor.Journal.CloseScope("RestoreTimer");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void OnClockBeat()
		{
			this.processor.Journal.OpenScope("OnClockBeat()");

			// function body
			this.processor.EmitGlobalWord(0x5c, this.processor.IncrementWord(this.processor.PullGlobalWord(0x5c)));

			AssignColors();

			RefreshHueCycling();

			this.processor.CompareWord(this.processor.PullGlobalWord(0x4e), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L036c;
			this.processor.EmitGlobalByte(0x5e, this.processor.SubtractByte(this.processor.PullGlobalByte(0x5e), 0x1));
			if (this.processor.FlagSet.IsGreater) goto L036c;
			this.processor.EmitGlobalByte(0x5e, 0x7);

			goto ExitFn;

		L036c:
			// sound driver call
			AdvanceSong();

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsSigned) goto L0378;
			if (this.processor.FlagSet.IsNotEqual) goto L037d;

			goto ExitFn;

		L0378:
			this.processor.EmitGlobalWord(0x4c, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x50);
			this.processor.EmitGlobalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0x59, 0x1);

			goto ExitFn;

		L037d:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x4e);
			this.processor.EmitGlobalWord(0x4c, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x50);
			this.processor.RegData.Value16 = 0x0;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.EmitGlobalWord(0x46, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0x59, 0x1);

		ExitFn:
			this.processor.Journal.CloseScope("OnClockBeat");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		/// <param name="param4"></param>
		/// <exception cref="Exception"></exception>
		public void AssignHueCycleSpan(ushort arg1, ushort arg2, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"AssignHueCycleSpan({arg1}, {arg2}, {arg3}, {arg4})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.SegData.Value16);

			this.processor.SegData.Value16 = 0x35cf;
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitGlobalWord(0x0, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = arg1;
			this.processor.RegBase.Value16 = this.processor.DecrementWord(this.processor.RegBase.Value16);
			this.processor.CompareWord(this.processor.RegBase.Value16, 0x8);
			if (this.processor.FlagSet.IsAboveOrEqual) goto L03f5;

			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2));

			this.processor.RegAcc.Value16 = arg3;
			this.processor.RegData.Value16 = arg4;
			this.processor.RegData.Value16 = this.processor.SubtractWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L03f5;

			this.processor.RegData.Value16 = this.processor.IncrementWord(this.processor.RegData.Value16);

			if (this.processor.RegData.Value16 > 0x10)
			{
				this.processor.RegData.Value16 = 0x10;
			}

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x4), (ushort)(this.processor.RegData.Value16 * 3));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6), this.processor.RegAcc.LowByte);

			this.processor.DisableInterrupts();
			if (this.owner.VideoCard.Displays.IncludesKey(0))
			{
				lock (this.owner.VideoCard.VideoLatch)
				{
					VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
					uint stagingLoc = CPU.AsLinearAddress(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x9));

					for (int idx = 0; idx < this.processor.RegData.Value16; idx++)
					{
						Color hue = VGABitmap.HueToHue18(display.ObtainHue(idx + this.processor.RegAcc.LowByte));

						this.processor.Storage.EmitUInt8(stagingLoc++, hue.R);
						this.processor.Storage.EmitUInt8(stagingLoc++, hue.G);
						this.processor.Storage.EmitUInt8(stagingLoc++, hue.B);
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}
			this.processor.EnableInterrupts();

			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;
			this.processor.RegDest.Value16 = (ushort)(this.processor.RegBase.Value16 + 0x9 + (this.processor.RegData.Value16 * 3));

			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x4));
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, 0x3);
			// LEA
			this.processor.RegSource.Value16 = (ushort)(this.processor.RegBase.Value16 + 0x9);
			this.processor.RepeatCopyStringByte(this.processor.SegData, this.processor.RegSource, this.processor.SegExtra, this.processor.RegDest, this.processor.RegCount);
			this.processor.RegAcc.Value16 = arg2;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x8), 0x0);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2), 0x0);

		L03f5:
			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("AssignHueCycleSpan");
		}

		/// <summary>
		/// Starts the colour cycle 1 - 8 (the cycle records of the segment 0x35cf), forwards for a positive number, backwards
		/// for a negative one (step 3 or -3), from its first colour
		/// </summary>
		public void ActivateHueCycle(ushort arg1)
		{
			this.processor.Journal.OpenScope($"ActivateHueCycle({arg1})");

			// Like the original: AX = |arg1| - 1, DX = the sign of arg1
			bool backwards = (short)arg1 < 0;
			ushort cycleIdx = (ushort)(Math.Abs((int)(short)arg1) - 1);
			this.processor.RegData.Value16 = (ushort)(backwards ? 0xffff : 0);

			if (cycleIdx < 8)
			{
				ushort cycle = HueCycleRecord(cycleIdx);

				this.processor.EmitUInt16(HueCycleSegment, cycle, (ushort)(backwards ? -3 : 3));
				this.processor.EmitUInt8(HueCycleSegment, (ushort)(cycle + 8), this.processor.PullUInt8(HueCycleSegment, (ushort)(cycle + 7)));
			}

			this.processor.Journal.CloseScope("ActivateHueCycle");
		}

		/// <summary>The segment of the colour cycles (a table of 8 record addresses at 2)</summary>
		private const ushort HueCycleSegment = 0x35cf;

		private ushort HueCycleRecord(ushort cycleIdx)
		{
			ushort cycle = this.processor.PullUInt16(HueCycleSegment, (ushort)((cycleIdx << 1) + 2));
			this.processor.RegBase.Value16 = cycle;

			return cycle;
		}

		/// <summary>
		/// Stops the colour cycle 1 - 8
		/// </summary>
		public void DeactivateHueCycle(ushort arg1)
		{
			this.processor.Journal.OpenScope($"DeactivateHueCycle({arg1})");

			ushort cycleIdx = (ushort)(arg1 - 1);
			this.processor.RegBase.Value16 = cycleIdx;

			if (cycleIdx < 8)
			{
				this.processor.EmitUInt8(HueCycleSegment, (ushort)(HueCycleRecord(cycleIdx) + 8), 0);
			}

			this.processor.Journal.CloseScope("DeactivateHueCycle");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <exception cref="Exception"></exception>
		public void RefreshHueCycling()
		{
			this.processor.Journal.OpenScope("RefreshHueCycling()");

			// function body
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.SegData.Value16 = 0x35cf;
			// LEA
			this.processor.RegDest.Value16 = 0x2;

		L0456:
			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x2);
			this.processor.CompareWord(this.processor.RegBase.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L04a5;
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x8)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0456;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x8), this.processor.DecrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x8))));
			if (this.processor.FlagSet.IsNotEqual) goto L0456;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x8), this.processor.RegAcc.LowByte);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x4));
			this.processor.RegSource.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2));
			this.processor.RegSource.Value16 = this.processor.SubtractWord(this.processor.RegSource.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L047d;
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegCount.Value16);

		L047d:
			this.processor.CompareWord(this.processor.RegSource.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLess) goto L0484;
			this.processor.RegSource.Value16 = 0x0;

		L0484:
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2), this.processor.RegSource.Value16);
			// LEA
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegBase.Value16 + 0x9);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x6));

			if (this.owner.VideoCard.Displays.IncludesKey(0))
			{
				lock (this.owner.VideoCard.VideoLatch)
				{
					VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
					uint stagingLoc = CPU.AsLinearAddress(this.processor.SegData.Value16, this.processor.RegSource.Value16);
					int total = this.processor.RegCount.Value16 / 3;

					for (int idx = 0; idx < total; idx++)
					{
						byte crimson = this.processor.Storage.PullUInt8(stagingLoc++);
						byte emerald = this.processor.Storage.PullUInt8(stagingLoc++);
						byte azure = this.processor.Storage.PullUInt8(stagingLoc++);

						display.AssignHue(idx + this.processor.RegAcc.LowByte, VGABitmap.Hue18ToHue(crimson, emerald, azure));
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}
			goto L0456;

		L04a5:
			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			// Far return
			this.processor.Journal.CloseScope("RefreshHueCycling");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="palettePtr"></param>
		public void ImportColorsFromBuffer(ushort arg1, ushort swatchAddr)
		{
			this.processor.Journal.OpenScope($"ImportColorsFromBuffer({arg1}, 0x{swatchAddr:x4})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);

			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.SegExtra.Value16 = this.processor.UnstackWord();
			this.processor.RegCount.Value16 = 0x300;
			this.processor.RegSource.Value16 = swatchAddr;
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x6);
			// LEA
			this.processor.RegDest.Value16 = 0xba06;
			this.processor.RepeatCopyStringByte(this.processor.SegData, this.processor.RegSource, this.processor.SegExtra, this.processor.RegDest, this.processor.RegCount);

			PullCurrentColors();

			FadeColors(arg1);

		L04c9:
			// Waits until the timer interrupt has finished the fade
			this.processor.CompareWord(this.processor.PullGlobalWord(0x68), 0x0);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.Idle();
				goto L04c9;
			}

			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("ImportColorsFromBuffer");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		/// <param name="param4"></param>
		public void FloodColors(ushort arg1, byte arg2, byte arg3, byte arg4)
		{
			this.processor.Journal.OpenScope($"FloodColors({arg1}, {arg2}, {arg3}, {arg4})");

			// function body
			this.processor.EnterFrame();
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);

			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.SegExtra.Value16 = this.processor.UnstackWord();

			// LEA
			this.processor.RegDest.Value16 = 0xba06;
			this.processor.RegCount.Value16 = 0x100;

		L04eb:
			this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, arg2);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1), arg3);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x2), arg4);

			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x3);
			if (this.processor.RepeatLoop(this.processor.RegCount)) goto L04eb;

			PullCurrentColors();

			FadeColors(arg1);

		L0501:
			// Waits until the timer interrupt has finished the fade
			this.processor.CompareWord(this.processor.PullGlobalWord(0x68), 0x0);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.Idle();
				goto L0501;
			}

			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("FloodColors");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		public void FadeColors(ushort arg1)
		{
			this.processor.Journal.OpenScope($"FadeColors({arg1})");

			// function body
			arg1 *= 6;
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x66);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L051f;

			GaugeColorsPerformance();

			this.processor.EmitGlobalWord(0x66, this.processor.RegAcc.Value16);

		L051f:
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.Value16 = 0xe;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.HighByte = this.processor.BitOrByte(this.processor.RegAcc.HighByte, this.processor.RegAcc.HighByte);
			if (this.processor.FlagSet.IsEqual) goto L052d;
			this.processor.RegAcc.Value16 = 0x100;

		L052d:
			this.processor.EmitGlobalWord(0x64, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x62, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x62, this.processor.NegateWord(this.processor.PullGlobalWord(0x62)));
			this.processor.MultiplyWord(this.processor.RegData, this.processor.RegAcc, arg1);
			this.processor.RegCount.HighByte = this.processor.RegData.LowByte;
			this.processor.RegCount.LowByte = this.processor.RegAcc.HighByte;
			this.processor.RegCount.Value16 = this.processor.DecrementWord(this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.DivideWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0x6c, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6a, this.processor.RegAcc.Value16);

			ColorsFadeStep();

			this.processor.EmitGlobalWord(0x68, arg1);

			// Near return
			this.processor.Journal.CloseScope("FadeColors");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <exception cref="Exception"></exception>
		public void PullCurrentColors()
		{
			this.processor.Journal.OpenScope("PullCurrentColors()");

			// function body
			this.processor.DisableInterrupts();
			if (this.owner.VideoCard.Displays.IncludesKey(0))
			{
				lock (this.owner.VideoCard.VideoLatch)
				{
					VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
					uint stagingLoc = CPU.AsLinearAddress(this.processor.SegData.Value16, 0xbd06);

					for (int idx = 0; idx < 0x100; idx++)
					{
						Color hue = VGABitmap.HueToHue18(display.ObtainHue(idx));

						this.processor.Storage.EmitUInt8(stagingLoc++, hue.R);
						this.processor.Storage.EmitUInt8(stagingLoc++, hue.G);
						this.processor.Storage.EmitUInt8(stagingLoc++, hue.B);
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}
			this.processor.EnableInterrupts();

			// Near return
			this.processor.Journal.CloseScope("PullCurrentColors");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public ushort GaugeColorsPerformance()
		{
			this.processor.Journal.OpenScope("GaugeColorsPerformance()");

			// function body
			this.processor.EmitGlobalWord(0x60, 0xffff);

			this.processor.DisableInterrupts();
			if (this.owner.VideoCard.Displays.IncludesKey(0))
			{
				lock (this.owner.VideoCard.VideoLatch)
				{
					VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
					uint stagingLoc = CPU.AsLinearAddress(this.processor.SegData.Value16, 0xbd06);

					for (int idx = 0; idx < 10; idx++)
					{
						byte crimson = this.processor.Storage.PullUInt8(stagingLoc++);
						byte emerald = this.processor.Storage.PullUInt8(stagingLoc++);
						byte azure = this.processor.Storage.PullUInt8(stagingLoc++);

						display.AssignHue(idx, VGABitmap.Hue18ToHue(crimson, emerald, azure));
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}
			this.processor.EnableInterrupts();

			this.processor.RegAcc.Value16 = 6553 * 10; // maximum performance

			// Near return
			this.processor.Journal.CloseScope("GaugeColorsPerformance");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ColorsFadeStep()
		{
			this.processor.Journal.OpenScope("ColorsFadeStep()");

			// function body
			this.processor.DisableInterrupts();
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.RegFrame.Value16);
			this.processor.StackWord(this.processor.SegExtra.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x62);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x64);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.HighByte = 0x0;
			this.processor.EmitGlobalWord(0x62, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			// LEA
			this.processor.RegDest.Value16 = 0xc006;

			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.SegExtra.Value16 = this.processor.UnstackWord();

			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);

		L05db:
			this.processor.RegBase.Value16 = 0x0;
			this.processor.RegFrame.Value16 = 0x0;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbd06));
			this.processor.RegAcc.HighByte = 0x0;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L05f7;
			this.processor.RegData.Value16 = this.processor.PullGlobalWord(0x6a);
			this.processor.RegData.Value16 = this.processor.InvertWord(this.processor.RegData.Value16);
			this.processor.MultiplyWord(this.processor.RegData, this.processor.RegAcc, this.processor.RegData.Value16);
			this.processor.RegFrame.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;

		L05f7:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xba06));
			this.processor.RegAcc.HighByte = 0x0;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0609;
			this.processor.MultiplyWord(this.processor.RegData, this.processor.RegAcc, this.processor.PullGlobalWord(0x6a));
			this.processor.RegFrame.Value16 = this.processor.SumWord(this.processor.RegFrame.Value16, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.SumCarryWord(this.processor.RegBase.Value16, this.processor.RegData.Value16);

		L0609:
			this.processor.RegFrame.Value16 = this.processor.ShiftLeftWord(this.processor.RegFrame.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumCarryWord(this.processor.RegBase.Value16, 0x0);
			this.processor.RegAcc.LowByte = this.processor.RegBase.LowByte;
			this.processor.StoreStringByte();
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.CompareWord(this.processor.RegSource.Value16, 0x300);
			if (this.processor.FlagSet.IsBelow) goto L062a;
			this.processor.RegSource.Value16 = 0x0;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c);
			this.processor.EmitGlobalWord(0x6a, this.processor.SumWord(this.processor.PullGlobalWord(0x6a), this.processor.RegAcc.Value16));
			if (this.processor.FlagSet.IsAboveOrEqual) goto L062a;
			this.processor.EmitGlobalWord(0x6a, 0xffff);

		L062a:
			if (this.processor.RepeatLoop(this.processor.RegCount)) goto L05db;

			this.processor.SegExtra.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			this.processor.EnableInterrupts();

			// Near return
			this.processor.Journal.CloseScope("ColorsFadeStep");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ColorsFadeStepInterrupt()
		{
			this.processor.Journal.OpenScope("ColorsFadeStepInterrupt()");

			// function body
			this.processor.DisableInterrupts();
			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.RegFrame.Value16);
			this.processor.StackWord(this.processor.SegExtra.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x62);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x64);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegAcc.HighByte = 0x0;
			this.processor.EmitGlobalWord(0x62, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			// LEA
			this.processor.RegDest.Value16 = 0xc006;

			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.SegExtra.Value16 = this.processor.UnstackWord();

			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);

		L05db:
			this.processor.RegBase.Value16 = 0x0;
			this.processor.RegFrame.Value16 = 0x0;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xbd06));
			this.processor.RegAcc.HighByte = 0x0;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L05f7;
			this.processor.RegData.Value16 = this.processor.PullGlobalWord(0x6a);
			this.processor.RegData.Value16 = this.processor.InvertWord(this.processor.RegData.Value16);
			this.processor.MultiplyWord(this.processor.RegData, this.processor.RegAcc, this.processor.RegData.Value16);
			this.processor.RegFrame.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;

		L05f7:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xba06));
			this.processor.RegAcc.HighByte = 0x0;
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0609;
			this.processor.MultiplyWord(this.processor.RegData, this.processor.RegAcc, this.processor.PullGlobalWord(0x6a));
			this.processor.RegFrame.Value16 = this.processor.SumWord(this.processor.RegFrame.Value16, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.SumCarryWord(this.processor.RegBase.Value16, this.processor.RegData.Value16);

		L0609:
			this.processor.RegFrame.Value16 = this.processor.ShiftLeftWord(this.processor.RegFrame.Value16, 0x1);
			this.processor.RegBase.Value16 = this.processor.SumCarryWord(this.processor.RegBase.Value16, 0x0);
			this.processor.RegAcc.LowByte = this.processor.RegBase.LowByte;
			this.processor.StoreStringByte();
			this.processor.RegSource.Value16 = this.processor.IncrementWord(this.processor.RegSource.Value16);
			this.processor.CompareWord(this.processor.RegSource.Value16, 0x300);
			if (this.processor.FlagSet.IsBelow) goto L062a;
			this.processor.RegSource.Value16 = 0x0;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c);
			this.processor.EmitGlobalWord(0x6a, this.processor.SumWord(this.processor.PullGlobalWord(0x6a), this.processor.RegAcc.Value16));
			if (this.processor.FlagSet.IsAboveOrEqual) goto L062a;
			this.processor.EmitGlobalWord(0x6a, 0xffff);

		L062a:
			if (this.processor.RepeatLoop(this.processor.RegCount)) goto L05db;

			this.processor.SegExtra.Value16 = this.processor.UnstackWord();
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();
			this.processor.EnableInterrupts();

			// Near return
			this.processor.Journal.CloseScope("ColorsFadeStepInterrupt");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <exception cref="Exception"></exception>
		public void AssignColors()
		{
			this.processor.Journal.OpenScope("AssignColors()");

			// function body
			if (this.processor.PullGlobalWord(0x68) != 0)
			{
				this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x62);
				this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x64);
				
				if (this.owner.VideoCard.Displays.IncludesKey(0))
				{
					lock (this.owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
						uint stagingLoc = CPU.AsLinearAddress(this.processor.SegData.Value16, 0xc006);

						for (int idx = 0; idx < this.processor.RegBase.Value16; idx++)
						{
							byte crimson = this.processor.Storage.PullUInt8(stagingLoc++);
							byte emerald = this.processor.Storage.PullUInt8(stagingLoc++);
							byte azure = this.processor.Storage.PullUInt8(stagingLoc++);

							display.AssignHue(idx + this.processor.RegAcc.LowByte, VGABitmap.Hue18ToHue(crimson, emerald, azure));
						}
					}
				}
				else
				{
					throw new Exception($"The screen {0} is not allocated");
				}

				this.processor.EmitGlobalWord(0x68, this.processor.DecrementWord(this.processor.PullGlobalWord(0x68)));

				ColorsFadeStepInterrupt();
			}
			
			// Far return
			this.processor.Journal.CloseScope("AssignColors");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="fileNamePtr"></param>
		/// <returns></returns>
		public ushort PathPresent(ushort documentLabelAddr)
		{
			string documentLabel = CRuntime.ObtainDOSFileLabel(this.owner.Processor.PullText(CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, documentLabelAddr)).ToUpper());

			this.processor.Journal.OpenScope($"PathPresent('{documentLabel}')");

			// function body
			if (File.Exists($"{this.owner.Processor.StandardFolder}{documentLabel}"))
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}

			// Far return
			this.processor.Journal.CloseScope("PathPresent");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RecoverScreenUnderCursor()
		{
			this.processor.Journal.OpenScope("RecoverScreenUnderCursor()");

			// function body
			this.owner.VideoCard.RenderBufferToScreen();

			if (this.processor.PullGlobalByte(0x5403) != 0)
			{
				this.processor.EmitGlobalByte(0x5402, 0x1);

			L075e:
				this.processor.EmitGlobalByte(0x5402, this.processor.DecrementByte(this.processor.PullGlobalByte(0x5402)));
				if (this.processor.FlagSet.IsEqual) goto L0769;

				this.processor.StackWord(this.processor.RegAcc.Value16);
				this.processor.StackWord(this.processor.RegData.Value16);
				this.processor.StackWord(this.processor.SegExtra.Value16);

				this.processor.SegExtra.Value16 = 0x1000;
				this.owner.VideoCard.RenderBufferToScreen();

				this.processor.SegExtra.Value16 = this.processor.UnstackWord();
				this.processor.RegData.Value16 = this.processor.UnstackWord();
				this.processor.RegAcc.Value16 = this.processor.UnstackWord();
				goto L075e;

			L0769:
				this.processor.RegAcc.Value16 |= 0;
			}

			this.processor.Journal.CloseScope("RecoverScreenUnderCursor");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		public void RenderCursorToScreen(short arg1, short arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"RenderCursorToScreen({arg1}, {arg2}, {arg3})");

			// function body

			if (this.processor.PullGlobalByte(0x5403) != 0)
			{
				this.processor.EmitGlobalByte(0x5402, 0x1);

			L075e:
				this.processor.EmitGlobalByte(0x5402, this.processor.DecrementByte(this.processor.PullGlobalByte(0x5402)));
				if (this.processor.FlagSet.IsEqual) goto L0769;

				this.processor.StackWord(this.processor.RegAcc.Value16);
				this.processor.StackWord(this.processor.RegData.Value16);
				this.processor.StackWord(this.processor.SegExtra.Value16);

				this.processor.SegExtra.Value16 = 0x1000;
				this.owner.VideoCard.RenderBufferToScreen();

				this.processor.SegExtra.Value16 = this.processor.UnstackWord();
				this.processor.RegData.Value16 = this.processor.UnstackWord();
				this.processor.RegAcc.Value16 = this.processor.UnstackWord();
				goto L075e;

			L0769:
				this.processor.RegAcc.Value16 |= 0;
			}

			this.processor.Journal.CloseScope("RenderCursorToScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="screenID"></param>
		public void DisplayScreen(ushort displayIdx)
		{
			this.processor.Journal.OpenScope($"DisplayScreen({displayIdx})");

			// function body
			this.owner.VideoCard.DuplicateScreenToMainScreen(displayIdx);

			if (this.processor.PullGlobalByte(0x5403) != 0)
			{
				this.processor.EmitGlobalByte(0x5402, 0x1);

			L075e:
				this.processor.EmitGlobalByte(0x5402, this.processor.DecrementByte(this.processor.PullGlobalByte(0x5402)));
				if (this.processor.FlagSet.IsEqual) goto L0769;

				this.processor.StackWord(this.processor.RegAcc.Value16);
				this.processor.StackWord(this.processor.RegData.Value16);
				this.processor.StackWord(this.processor.SegExtra.Value16);
				
				this.processor.SegExtra.Value16 = 0x1000;
				this.owner.VideoCard.RenderBufferToScreen();

				this.processor.SegExtra.Value16 = this.processor.UnstackWord();
				this.processor.RegData.Value16 = this.processor.UnstackWord();
				this.processor.RegAcc.Value16 = this.processor.UnstackWord();
				goto L075e;

			L0769:
				this.processor.RegAcc.Value16 |= 0;
			}

			this.processor.Journal.CloseScope("DisplayScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="rectPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="bitmapPtr"></param>
		public void RenderPictureToScreenAlternate(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			this.processor.Journal.OpenScope($"RenderPictureToScreenAlternate(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, 0x{pictureAddr:x4})");

			// function body
			this.owner.VideoCard.RenderPictureToScreen(boxAddr, horizLoc, vertLoc, pictureAddr);

			if (this.processor.PullGlobalByte(0x5403) != 0)
			{
				this.processor.EmitGlobalByte(0x5402, 0x1);

			L075e:
				this.processor.EmitGlobalByte(0x5402, this.processor.DecrementByte(this.processor.PullGlobalByte(0x5402)));
				if (this.processor.FlagSet.IsEqual) goto L0769;

				this.processor.StackWord(this.processor.RegAcc.Value16);
				this.processor.StackWord(this.processor.RegData.Value16);
				this.processor.StackWord(this.processor.SegExtra.Value16);
				
				this.processor.SegExtra.Value16 = 0x1000;
				this.owner.VideoCard.RenderBufferToScreen();

				this.processor.SegExtra.Value16 = this.processor.UnstackWord();
				this.processor.RegData.Value16 = this.processor.UnstackWord();
				this.processor.RegAcc.Value16 = this.processor.UnstackWord();
				goto L075e;

			L0769:
				this.processor.RegAcc.Value16 |= 0;
			}

			this.processor.Journal.CloseScope("RenderPictureToScreenAlternate");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="rectPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="bitmapPtr"></param>
		public void RenderPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			this.processor.Journal.OpenScope($"RenderPictureToScreen(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, 0x{pictureAddr:x4})");

			// function body
			this.owner.VideoCard.RenderPictureToScreen(boxAddr, horizLoc, vertLoc, pictureAddr);

			this.processor.Journal.CloseScope("RenderPictureToScreen");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void PrepareSoundDriver()
		{
			this.owner.Audio.Prepare();
		}

		/// <summary>
		/// Sound function, possibly: Play Music
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void PerformSound(ushort arg1, ushort arg2)
		{
			if ((this.owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.owner.Audio.PerformSound(arg1, arg2);
			}
		}

		/// <summary>
		/// Plays the sound of chopping wood when a city is founded, if the sound is on
		/// </summary>
		public void PerformCityFoundedSound()
		{
			if ((this.owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.owner.Audio.PerformChopping();
			}
		}

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		public ushort HaltSound()
		{
			this.processor.RegAcc.Value16 =  this.owner.Audio.Halt();

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		public ushort AdvanceSong()
		{
			this.processor.RegAcc.Value16 =  this.owner.Audio.PerformNextStep();

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Sound function, possibly advance sound buffer
		/// </summary>
		public void RefreshSoundEffects()
		{
			// function body
			this.owner.Audio.RefreshVibratoTremolo();
		}

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		public ushort ObtainSongPosition()
		{
			this.processor.RegAcc.Value16 = this.owner.Audio.ObtainSongPosition();

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Fills the rectangle with color
		/// </summary>
		/// <param name="rectPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		public void FloodBox(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			this.processor.Journal.OpenScope($"FloodBox(0x{boxAddr:4}, {horizLoc}, {vertLoc}, {breadth}, {altitude}, 0x{style:4})");

			// function body
			CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));

			if (breadth > 0 && altitude > 0)
			{
				Rectangle box1 = new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, breadth, altitude);
				this.owner.VideoCard.FloodBox(box.DisplayIdx, box1, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			// Far return
			this.processor.Journal.CloseScope("FloodBox");
		}

		/// <summary>
		/// Sets a pixel on screen set by structure at 0xaa
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void AssignDot(int horizLoc, int vertLoc, ushort style)
		{
			CivRectangle box = new CivRectangle(this.owner.Processor, CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, this.owner.Processor.PullUInt16(this.owner.Processor.SegData.Value16, 0xaa)));

			this.processor.Journal.OpenScope($"AssignDot({box.DisplayIdx}, {horizLoc}, {vertLoc}, 0x{style:x4})");

			// function body
			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				this.owner.VideoCard.AssignDot(box.DisplayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			// Far return
			this.processor.Journal.CloseScope("AssignDot");
		}

		/// <summary>
		/// Sets a pixel on selected screen
		/// </summary>
		/// <param name="screenID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		public void AssignDot(ushort displayIdx, int horizLoc, int vertLoc, ushort style)
		{
			CivRectangle box = new CivRectangle(this.owner.Processor, CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, this.owner.Processor.PullUInt16(this.owner.Processor.SegData.Value16, 0xaa)));

			this.processor.Journal.OpenScope($"AssignDot({displayIdx}, {horizLoc}, {vertLoc}, 0x{style:x4})");

			// function body
			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				this.owner.VideoCard.AssignDot(displayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			// Far return
			this.processor.Journal.CloseScope("AssignDot");
		}

		/// <summary>
		/// Resets and inits the mouse subsystem
		/// </summary>
		/// <returns>true if mouse is detected</returns>
		public ushort PrepareMouse()
		{
			this.processor.Journal.OpenScope("PrepareMouse()");

			// function body
			// assume our mouse is initialized

			this.processor.RegCount.LowByte = 0;
			this.processor.RegAcc.Value16 = (ushort)(this.owner.VideoCard.DisplayPointerSite.X >> this.processor.RegCount.LowByte);
			this.processor.EmitGlobalByte(0x587c, this.processor.RegCount.LowByte);
			this.processor.EmitGlobalWord(0x586e, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x5870, (ushort)this.owner.VideoCard.DisplayPointerSite.Y);
			this.processor.EmitGlobalWord(0x5872, this.owner.VideoCard.DisplayPointerClicks);
			this.processor.RegAcc.Value16 = 0xffff;

			this.processor.EmitGlobalByte(0x587d, this.processor.RegAcc.LowByte);

			// Far return
			this.processor.Journal.CloseScope("PrepareMouse");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DeactivateMouse()
		{
			this.processor.Journal.OpenScope("DeactivateMouse()");

			// function body
			this.processor.RegAcc.LowByte = 0x0;
			this.processor.Scratch.LowByte = this.processor.PullGlobalByte(0x587d);
			this.processor.EmitGlobalByte(0x587d, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.Scratch.LowByte;
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsEqual) goto L1696;

			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.RegBase.Value16 = 2;

		L1696:
			// Far return
			this.processor.Journal.CloseScope("DeactivateMouse");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		/// <param name="param3"></param>
		public void AssignMouseCursorImage(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"AssignMouseCursorImage({arg1}, {arg2}, {arg3})");

			// function body
			this.processor.EmitGlobalWord(0x5878, arg1);
			this.processor.EmitGlobalWord(0x587a, arg2);
			this.processor.EmitGlobalWord(0x5876, arg3);

			// Far return
			this.processor.Journal.CloseScope("AssignMouseCursorImage");
		}

		/// <summary>
		/// Moves the mouse pointer to the position (0x586e, 0x5870)
		/// </summary>
		public void AssignMousePosition(ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"AssignMousePosition({horizLoc}, {vertLoc})");

			this.processor.EmitGlobalWord(0x586e, horizLoc);
			this.processor.EmitGlobalWord(0x5870, vertLoc);

			this.processor.Journal.CloseScope("AssignMousePosition");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <returns></returns>
		public ushort ObtainAndWipeMouseClick()
		{
			// function body
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5874);
			this.processor.EmitGlobalWord(0x5874, 0);

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Draws the mouse pointer (picture 0x5876, hot spot 0x5878, 0x587a) at the mouse position (0x586e, 0x5870), when there
		/// is a picture and the pointer is not drawn yet (0x5403)
		/// </summary>
		public void RenderMouseCursor()
		{
			this.processor.Journal.OpenScope("RenderMouseCursor()");

			if (this.processor.PullGlobalWord(0x5876) != 0 && this.processor.PullGlobalByte(0x5403) == 0)
			{
				RenderCursorToScreen((short)(this.processor.PullGlobalSignedWord(0x586e) - this.processor.PullGlobalSignedWord(0x5878)),
					(short)(this.processor.PullGlobalSignedWord(0x5870) - this.processor.PullGlobalSignedWord(0x587a)),
					this.processor.PullGlobalWord(0x5876));

				this.processor.EmitGlobalByte(0x5403, 0x1);
			}

			this.processor.Journal.CloseScope("RenderMouseCursor");
		}

		/// <summary>
		/// Puts back the screen under the mouse pointer, when the pointer has a picture (0x5876) and is drawn (0x5403)
		/// </summary>
		public void BlankMouseCursor()
		{
			this.processor.Journal.OpenScope("BlankMouseCursor()");

			if (this.processor.PullGlobalWord(0x5876) != 0 && this.processor.PullGlobalByte(0x5403) != 0)
			{
				this.processor.EmitGlobalByte(0x5403, 0x0);

				RecoverScreenUnderCursor();
			}

			this.processor.Journal.CloseScope("BlankMouseCursor");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void RenewScreen()
		{
			this.processor.Journal.OpenScope("RenewScreen()");

			// Called from the mouse interrupt: the registers are kept outside the emulated stack, like in ClockInterrupt
			(ushort acc, ushort data, ushort extraSection) =
				(this.processor.RegAcc.Value16, this.processor.RegData.Value16, this.processor.SegExtra.Value16);

			this.processor.SegExtra.Value16 = 0x1000;
			this.owner.VideoCard.RenderBufferToScreen();

			(this.processor.RegAcc.Value16, this.processor.RegData.Value16, this.processor.SegExtra.Value16) = (acc, data, extraSection);

			this.processor.Journal.CloseScope("RenewScreen");
		}

		/// <summary>
		/// Handles mouse events
		/// </summary>
		public void PointerEvent()
		{
			// function body
			this.processor.SegData.Value16 = 0x3b01;
			this.processor.RegFrame.Value16 = this.processor.RegCount.Value16;

			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x587c);
			this.processor.RegFrame.Value16 = this.processor.ShiftRightWord(this.processor.RegFrame.Value16, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegFrame.Value16;
			this.processor.EmitGlobalWord(0x5874, this.processor.BitOrWord(this.processor.PullGlobalWord(0x5874), this.processor.RegBase.Value16));
			this.processor.EmitGlobalWord(0x5872, this.processor.RegBase.Value16);
			this.processor.EmitGlobalWord(0x586e, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0x5870, this.processor.RegData.Value16);

			this.processor.RegFrame.Value16 = this.processor.RegAcc.Value16;
			this.processor.TestBitsWord(this.processor.RegFrame.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1829;
			this.processor.CompareByte(this.processor.PullGlobalByte(0x5403), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1829;
			this.processor.RegAcc.LowByte = 0x2;
			this.processor.Scratch.LowByte = this.processor.PullGlobalByte(0x5402);
			this.processor.EmitGlobalByte(0x5402, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.Scratch.LowByte;
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(this.processor.RegAcc.LowByte, this.processor.RegAcc.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L1829;

			RenewScreen();

			this.processor.EmitGlobalByte(0x5402, 0x0);

		L1829:
			// Far return
			this.processor.RegAcc.LowByte = this.processor.RegAcc.LowByte;
		}
	}
}
