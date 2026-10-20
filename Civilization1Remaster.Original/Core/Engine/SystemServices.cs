using System;
using System.Drawing;
using System.IO;
using Civilization1Original.Emulation;

namespace Civilization1Original.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1000.
	/// </summary>
	public class SystemServices : GameModule
	{
		public SystemServices(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Starts the timer (0x5a = 1) unless it runs already: the beat counters (0x4c, 0x54), the time (0x42) and the
		/// calibration (PrepareTimer), then the clock interrupt is allowed. AL = the old 0x5a, like the original.
		/// </summary>
		public void LaunchTimer()
		{
			this.processor.Journal.OpenScope("LaunchTimer()");

			byte running = this.processor.PullGlobalByte(0x5a);
			this.processor.EmitGlobalByte(0x5a, 0x1);
			this.processor.RegAcc.LowByte = running;

			if (running == 0)
			{
				this.processor.EmitGlobalWord(0x4c, 0x1);
				this.processor.EmitGlobalWord(0x54, 0x1);
				this.processor.EmitGlobalWord(0x42, 0x0);
				this.processor.EmitGlobalWord(0x44, 0x0);

				PrepareTimer();

				this.processor.AllowClock = true;
			}

			this.processor.Journal.CloseScope("LaunchTimer");
		}

		/// <summary>
		/// Stops the timer (0x5a = 0); AL = the old 0x5a, like the original
		/// </summary>
		public void HaltTimer()
		{
			this.processor.Journal.OpenScope("HaltTimer()");

			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x5a);
			this.processor.EmitGlobalByte(0x5a, 0x0);

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

			uint oldTime = this.processor.PullUInt32(this.processor.SegData.Value16, 0x42);
			this.processor.EmitUInt32(this.processor.SegData.Value16, 0x42, oldTime + this.processor.PullGlobalWord(0x48));

			ushort counter54 = (ushort)(this.processor.PullGlobalWord(0x54) - 1);
			this.processor.EmitGlobalWord(0x54, counter54);
			if (counter54 == 0)
			{
				this.processor.EmitGlobalWord(0x54, this.processor.PullGlobalWord(0x4c));

				byte counter59 = (byte)(this.processor.PullGlobalByte(0x59) - 1);
				this.processor.EmitGlobalByte(0x59, counter59);
				if (counter59 == 0)
				{
					this.processor.EmitGlobalByte(0x59, 0x14);

					if (this.processor.PullGlobalByte(0x58) != 0)
					{
						this.processor.EmitGlobalByte(0x58, 0);
					}
					else
					{
						// The new tempo (0x46) is read again when it is taken: the accesses to memory are counted by the timer of the tests
						if (this.processor.PullGlobalWord(0x46) != this.processor.PullGlobalWord(0x48))
						{
							this.processor.EmitGlobalWord(0x48, this.processor.PullGlobalWord(0x46));
							this.processor.EmitGlobalWord(0x4a, (ushort)(this.processor.PullGlobalWord(0x4a) + 1));
							this.processor.EmitGlobalWord(0x56, 0x20);
						}
					}
				}

				OnClockBeat();
			}

			if (this.processor.PullGlobalWord(0x4c) != 1)
			{
				// sound driver call
				RefreshSoundEffects();
			}

			if (this.processor.PullGlobalWord(0x44) != 0)
			{
				this.processor.EmitGlobalWord(0x44, (ushort)(this.processor.PullGlobalWord(0x44) - 1));
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
		/// The timer calibration. The original measures the timer 16 times (100 - 200 the first time, 200 - 200 after), so in
		/// the emulator it always adds 0xff9c to the time (0x42, 32 bit), gets the rate 0 (not 4 - 6) and falls back to the
		/// default: the beat length 0x4dae (0x50), the rate 5 (0x4e, also 0x4c unless it is 1), the beat counters
		/// 0x46 = 0x48 = 0x4dae / 0x4c; 0x58 = 0, 0x59 = 1. The registers as the original leaves them.
		/// </summary>
		public void PrepareTimer()
		{
			this.processor.Journal.OpenScope("PrepareTimer()");

			const ushort measured = 0xff9c;
			const ushort beatLength = 0x4dae;
			const ushort rate = 5;

			this.processor.EmitGlobalByte(0x59, 0x1);
			this.processor.EmitGlobalByte(0x58, 0x0);
			this.processor.EmitGlobalWord(0x52, 0x0);
			this.processor.EmitUInt32(this.processor.SegData.Value16, 0x42, this.processor.PullUInt32(this.processor.SegData.Value16, 0x42) + measured);
			this.processor.EmitGlobalWord(0x50, beatLength);
			this.processor.EmitGlobalWord(0x4e, rate);

			if (this.processor.PullGlobalWord(0x4c) != 0x1)
			{
				this.processor.EmitGlobalWord(0x4c, rate);
			}

			ushort divisor = this.processor.PullGlobalWord(0x4c);
			ushort beats = (ushort)(beatLength / divisor);

			this.processor.EmitGlobalWord(0x48, beats);
			this.processor.EmitGlobalWord(0x46, beats);

			this.processor.RegAcc.Value16 = beats;
			this.processor.RegData.Value16 = (ushort)(beatLength % divisor);
			this.processor.RegBase.Value16 = 0xf89;
			this.processor.RegCount.Value16 = 0x10;

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
		/// A beat of the timer (from ClockInterrupt): the beat counter 0x5c, the colour fade and cycles, then the music. With the
		/// rate 4 (0x4e) the music skips every 7th beat (counter 0x5e). The music step tells a new speed: below 0 the normal one
		/// (rate 1, beat length 0x50), above 0 the rate 0x4e (beat length 0x50 / rate); the beat counters 0x46 and 0x59 restart.
		/// The memory accesses are those of the original (the beats of the test timer depend on them).
		/// </summary>
		public void OnClockBeat()
		{
			this.processor.Journal.OpenScope("OnClockBeat()");

			this.processor.EmitGlobalWord(0x5c, (ushort)(this.processor.PullGlobalWord(0x5c) + 1));

			AssignColors();

			RefreshHueCycling();

			if (this.processor.PullGlobalWord(0x4e) == 4)
			{
				sbyte skip = (sbyte)this.processor.PullGlobalByte(0x5e);

				this.processor.EmitGlobalByte(0x5e, (byte)(skip - 1));

				if (skip - 1 <= 0)
				{
					this.processor.EmitGlobalByte(0x5e, 0x7);

					this.processor.Journal.CloseScope("OnClockBeat");
					return;
				}
			}

			short speed = (short)AdvanceSong();

			if (speed < 0)
			{
				this.processor.EmitGlobalWord(0x4c, 0x1);
				this.processor.EmitGlobalWord(0x46, this.processor.PullGlobalWord(0x50));
				this.processor.EmitGlobalByte(0x59, 0x1);
			}
			else if (speed > 0)
			{
				ushort rate = this.processor.PullGlobalWord(0x4e);

				this.processor.EmitGlobalWord(0x4c, rate);

				ushort beatLength = this.processor.PullGlobalWord(0x50);

				if (rate == 0)
					throw new Exception("Division by zero");

				this.processor.RegBase.Value16 = rate;
				this.processor.RegData.Value16 = (ushort)(beatLength % rate);
				this.processor.RegAcc.Value16 = (ushort)(beatLength / rate);

				this.processor.EmitGlobalWord(0x46, this.processor.RegAcc.Value16);
				this.processor.EmitGlobalByte(0x59, 0x1);
			}

			this.processor.Journal.CloseScope("OnClockBeat");
		}

		/// <summary>
		/// Sets the colour cycle 1 - 8 (a record of the segment 0x35cf, see ActivateHueCycle) to the colours first - last (at
		/// most 16), with the delay in beats; the current colours are kept twice in the record, so that a cycle is one copy.
		/// Also writes 0xffff to 0x35cf:0. The registers as the original leaves them.
		/// </summary>
		public void AssignHueCycleSpan(ushort arg1, ushort arg2, ushort arg3, ushort arg4)
		{
			this.processor.Journal.OpenScope($"AssignHueCycleSpan({arg1}, {arg2}, {arg3}, {arg4})");

			this.processor.EmitUInt16(HueCycleSegment, 0x0, 0xffff);
			this.processor.RegAcc.Value16 = 0xffff;

			ushort cycleIdx = (ushort)(arg1 - 1);
			this.processor.RegBase.Value16 = cycleIdx;

			if (cycleIdx < 8)
			{
				ushort cycle = HueCycleRecord(cycleIdx);
				ushort span = (ushort)(arg4 - arg3);

				this.processor.RegAcc.Value16 = arg3;
				this.processor.RegData.Value16 = span;

				if (arg4 > arg3)
				{
					ushort colours = (ushort)Math.Min(span + 1, 0x10);
					byte first = (byte)arg3;

					this.processor.EmitUInt16(HueCycleSegment, (ushort)(cycle + 4), (ushort)(colours * 3));
					this.processor.EmitUInt8(HueCycleSegment, (ushort)(cycle + 6), first);

					this.processor.DisableInterrupts();

					if (!this.owner.VideoCard.Displays.IncludesKey(0))
						throw new Exception($"The screen {0} is not allocated");

					lock (this.owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
						uint stagingLoc = CPU.AsLinearAddress(HueCycleSegment, (ushort)(cycle + 0x9));

						for (int idx = 0; idx < colours; idx++)
						{
							Color hue = VGABitmap.HueToHue18(display.ObtainHue(idx + first));

							this.processor.Storage.EmitUInt8(stagingLoc++, hue.R);
							this.processor.Storage.EmitUInt8(stagingLoc++, hue.G);
							this.processor.Storage.EmitUInt8(stagingLoc++, hue.B);
						}
					}

					this.processor.EnableInterrupts();

					// The colours again after them, but the last one (rep movsb with ES = the cycle segment, straight in the memory
					// without clock steps)
					ushort hues = (ushort)(cycle + 0x9);
					ushort copyLength = (ushort)(this.processor.PullUInt16(HueCycleSegment, (ushort)(cycle + 4)) - 3);

					for (int idx = 0; idx < copyLength; idx++)
					{
						this.processor.Storage.EmitUInt8(HueCycleSegment, (ushort)(hues + (colours * 3) + idx),
							this.processor.Storage.PullUInt8(HueCycleSegment, (ushort)(hues + idx)));
					}

					this.processor.EmitUInt8(HueCycleSegment, (ushort)(cycle + 0x7), (byte)arg2);
					this.processor.EmitUInt8(HueCycleSegment, (ushort)(cycle + 0x8), 0x0);
					this.processor.EmitUInt16(HueCycleSegment, (ushort)(cycle + 0x2), 0x0);

					this.processor.SegExtra.Value16 = HueCycleSegment;
					this.processor.RegAcc.Value16 = arg2;
					this.processor.RegData.Value16 = colours;
					this.processor.RegCount.Value16 = 0;
				}
			}

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

			this.processor.PreserveSource();
			this.processor.StackWord(this.processor.RegDest.Value16);
			this.processor.StackWord(this.processor.SegData.Value16);
			this.processor.SegData.Value16 = 0x35cf;
			this.processor.RegDest.Value16 = 2;

			while (true)
			{
				ushort cycle = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegDest.Value16);
				this.processor.RegBase.Value16 = cycle;
				this.processor.RegDest.Value16 = (ushort)(this.processor.RegDest.Value16 + 2);

				if (cycle == 0xffff)
				{
					break;
				}

				// Inactive cycle (timer 0), or not yet time for the next step: the timer is read again to count it down, as in the original
				if (this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(cycle + 8)) == 0)
				{
					continue;
				}

				byte timer = (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(cycle + 8)) - 1);
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(cycle + 8), timer);
				if (timer != 0)
				{
					continue;
				}

				byte initialDelay = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(cycle + 7));
				this.processor.RegAcc.LowByte = initialDelay;
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(cycle + 8), initialDelay);

				ushort count = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(cycle + 4));
				this.processor.RegCount.Value16 = count;

				// One step back (forward for a backward cycle), wrapped around the colours of the cycle
				short position = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(cycle + 2));
				short step = (short)this.processor.PullUInt16(this.processor.SegData.Value16, cycle);
				short source = (short)(position - step);

				if (position < step)
				{
					source += (short)count;
				}

				if (source >= (short)count)
				{
					source = 0;
				}

				this.processor.RegSource.Value16 = (ushort)source;
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(cycle + 2), (ushort)source);

				ushort paletteOffset = (ushort)(cycle + 9);
				this.processor.RegAcc.Value16 = paletteOffset;
				this.processor.RegSource.Value16 = (ushort)(this.processor.RegSource.Value16 + paletteOffset);

				byte startHue = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(cycle + 6));
				this.processor.RegAcc.LowByte = startHue;

				if (this.owner.VideoCard.Displays.IncludesKey(0))
				{
					lock (this.owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
						uint stagingLoc = CPU.AsLinearAddress(this.processor.SegData.Value16, this.processor.RegSource.Value16);
						int total = count / 3;

						for (int idx = 0; idx < total; idx++)
						{
							byte crimson = this.processor.Storage.PullUInt8(stagingLoc++);
							byte emerald = this.processor.Storage.PullUInt8(stagingLoc++);
							byte azure = this.processor.Storage.PullUInt8(stagingLoc++);

							display.AssignHue(idx + startHue, VGABitmap.Hue18ToHue(crimson, emerald, azure));
						}
					}
				}
				else
				{
					throw new Exception($"The screen 0 is not allocated");
				}
			}

			this.processor.SegData.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.RestoreSource();

			this.processor.Journal.CloseScope("RefreshHueCycling");
		}

		/// <summary>
		/// Fades the colours to the palette of a picture (the 0x300 colour bytes after the 6 byte header at swatchAddr) in
		/// steps * 6 timer beats and waits until the fade is done
		/// </summary>
		public void ImportColorsFromBuffer(ushort arg1, ushort swatchAddr)
		{
			this.processor.Journal.OpenScope($"ImportColorsFromBuffer({arg1}, 0x{swatchAddr:x4})");

			// The target palette goes to 0xba06 (rep movsb with ES = DS, which the original leaves in ES). Copied straight in the
			// memory like rep movsb, without clock steps: with a colour cycle on, an earlier timer beat would change the
			// colours that PullCurrentColors takes
			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;

			for (int idx = 0; idx < 0x300; idx++)
			{
				this.processor.Storage.EmitUInt8(this.processor.SegData.Value16, (ushort)(0xba06 + idx),
					this.processor.Storage.PullUInt8(this.processor.SegData.Value16, (ushort)(swatchAddr + 6 + idx)));
			}

			FadeToTargetColors(arg1);

			this.processor.Journal.CloseScope("ImportColorsFromBuffer");
		}

		/// <summary>
		/// Fades all 256 colours to one colour (red, green, blue: 0 - 63) in steps * 6 timer beats and waits until the fade
		/// is done
		/// </summary>
		public void FloodColors(ushort arg1, byte arg2, byte arg3, byte arg4)
		{
			this.processor.Journal.OpenScope($"FloodColors({arg1}, {arg2}, {arg3}, {arg4})");

			// The original leaves DS in ES
			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;

			for (int idx = 0; idx < 0x100; idx++)
			{
				ushort colour = (ushort)(0xba06 + (3 * idx));

				this.processor.EmitGlobalByte(colour, arg2);
				this.processor.EmitGlobalByte((ushort)(colour + 1), arg3);
				this.processor.EmitGlobalByte((ushort)(colour + 2), arg4);
			}

			FadeToTargetColors(arg1);

			this.processor.Journal.CloseScope("FloodColors");
		}

		/// <summary>
		/// The common part of ImportColorsFromBuffer and FloodColors: the current colours to 0xbd06, the fade to the colours
		/// at 0xba06, then waiting until the timer interrupt has finished it (0x68 = 0)
		/// </summary>
		private void FadeToTargetColors(ushort steps)
		{
			PullCurrentColors();

			FadeColors(steps);

			while (this.processor.PullGlobalWord(0x68) != 0)
			{
				this.processor.Idle();
			}
		}

		/// <summary>
		/// Starts a fade from the colours at 0xbd06 to the colours at 0xba06 in arg1 * 6 timer beats (0x68): the colours per
		/// beat (0x64, from the measured speed 0x66 / 14, at most 0x100), the first colour (0x62), the mix step (0x6c) and the
		/// mix (0x6a); the first step is done at once
		/// </summary>
		public void FadeColors(ushort arg1)
		{
			this.processor.Journal.OpenScope($"FadeColors({arg1})");

			ushort beats = (ushort)(arg1 * 6);
			ushort speed = this.processor.PullGlobalWord(0x66);

			if (speed == 0)
			{
				speed = GaugeColorsPerformance();
				this.processor.EmitGlobalWord(0x66, speed);
			}

			ushort colours = (ushort)(speed / 0xe);

			if (colours > 0xff)
				colours = 0x100;

			this.processor.EmitGlobalWord(0x64, colours);

			// Written, read back and negated like the original: the same number of clock steps before the fade starts (0x68)
			this.processor.EmitGlobalWord(0x62, colours);
			this.processor.EmitGlobalWord(0x62, (ushort)-this.processor.PullGlobalWord(0x62));

			// 0x10000 / (colours * beats / 0x100 - 1), a 32 / 16 bit division like the original (and its errors)
			ushort divisor = (ushort)(((uint)colours * beats >> 8) - 1);

			if (divisor == 0)
				throw new Exception("Division by zero");

			if (divisor == 1)
				throw new Exception("Division error");

			ushort mixStep = (ushort)(0x10000 / divisor);

			this.processor.EmitGlobalWord(0x6c, mixStep);
			this.processor.EmitGlobalWord(0x6a, mixStep);

			ColorsFadeStep();

			this.processor.EmitGlobalWord(0x68, beats);

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
		/// The first step of a colour fade (FadeColors); see MixFadeColours
		/// </summary>
		public void ColorsFadeStep()
		{
			this.processor.Journal.OpenScope("ColorsFadeStep()");

			MixFadeColours();

			this.processor.Journal.CloseScope("ColorsFadeStep");
		}

		/// <summary>
		/// A step of a colour fade from the timer (AssignColors); see MixFadeColours
		/// </summary>
		public void ColorsFadeStepInterrupt()
		{
			this.processor.Journal.OpenScope("ColorsFadeStepInterrupt()");

			MixFadeColours();

			this.processor.Journal.CloseScope("ColorsFadeStepInterrupt");
		}

		/// <summary>
		/// The next 0x64 colours of a fade (from colour 0x62, wrapping at 256) into 0xc006: each colour byte is the mix of the
		/// colours at 0xbd06 (from) and 0xba06 (to), (from * ~mix + to * mix) / 0x10000 rounded, with the mix 0x6a; after the last colour
		/// the mix grows by 0x6c (at most 0xffff). The memory accesses are those of the original (the beats of the test timer
		/// depend on them; the colours are written straight into memory like stosb), and AX, BX, CX, DX are left as it leaves
		/// them; SI, DI, BP and ES are kept.
		/// </summary>
		private void MixFadeColours()
		{
			this.processor.DisableInterrupts();

			ushort previous = this.processor.PullGlobalWord(0x62);
			ushort count = this.processor.PullGlobalWord(0x64);
			ushort first = (ushort)((previous + count) & 0xff);

			this.processor.EmitGlobalWord(0x62, first);

			ushort source = (ushort)(3 * first);
			ushort target = 0xc006;
			ushort bytes = (ushort)(3 * count);
			ushort acc = first;
			ushort data = this.processor.RegData.Value16;
			ushort sum = 0;

			do
			{
				uint mixed = 0;

				byte from = this.processor.PullGlobalByte((ushort)(source + 0xbd06));

				if (from != 0)
				{
					uint product = (uint)from * (ushort)~this.processor.PullGlobalWord(0x6a);

					mixed = product;
					acc = (ushort)product;
					data = (ushort)(product >> 16);
				}

				byte to = this.processor.PullGlobalByte((ushort)(source + 0xba06));

				acc = to;

				if (to != 0)
				{
					uint product = (uint)to * this.processor.PullGlobalWord(0x6a);

					mixed += product;
					acc = (ushort)product;
					data = (ushort)(product >> 16);
				}

				// The high word, rounded with the top bit of the low word
				sum = (ushort)((mixed >> 16) + ((mixed >> 15) & 1));
				acc = (ushort)((acc & 0xff00) | (sum & 0xff));

				this.processor.Storage.EmitUInt8(this.processor.SegData.Value16, target++, (byte)sum);

				source++;

				if (source >= 0x300)
				{
					source = 0;
					acc = this.processor.PullGlobalWord(0x6c);

					uint mix = (uint)this.processor.PullGlobalWord(0x6a) + acc;

					this.processor.EmitGlobalWord(0x6a, (ushort)mix);

					if (mix > 0xffff)
					{
						this.processor.EmitGlobalWord(0x6a, 0xffff);
					}
				}

				bytes--;
			}
			while (bytes != 0);

			this.processor.RegAcc.Value16 = acc;
			this.processor.RegBase.Value16 = sum;
			this.processor.RegCount.Value16 = 0;
			this.processor.RegData.Value16 = data;

			this.processor.EnableInterrupts();
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
		/// Copies the screen buffer to the screen (over the mouse pointer) and clears the pointer latch (0x5402) when the
		/// pointer is drawn
		/// </summary>
		public void RecoverScreenUnderCursor()
		{
			this.processor.Journal.OpenScope("RecoverScreenUnderCursor()");

			this.owner.VideoCard.RenderBufferToScreen();
			ClearPointerLatch();

			this.processor.Journal.CloseScope("RecoverScreenUnderCursor");
		}

		/// <summary>
		/// Only clears the pointer latch (0x5402) when the pointer is drawn: the pointer itself is drawn by the host, the
		/// position and the picture are not used
		/// </summary>
		public void RenderCursorToScreen(short arg1, short arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"RenderCursorToScreen({arg1}, {arg2}, {arg3})");

			ClearPointerLatch();

			this.processor.Journal.CloseScope("RenderCursorToScreen");
		}

		/// <summary>
		/// Copies the screen buffer to the main screen and clears the pointer latch (0x5402) when the pointer is drawn
		/// </summary>
		/// <param name="displayIdx">The screen buffer</param>
		public void DisplayScreen(ushort displayIdx)
		{
			this.processor.Journal.OpenScope($"DisplayScreen({displayIdx})");

			this.owner.VideoCard.DuplicateScreenToMainScreen(displayIdx);
			ClearPointerLatch();

			this.processor.Journal.CloseScope("DisplayScreen");
		}

		/// <summary>
		/// Draws a picture on the screen and clears the pointer latch (0x5402) when the pointer is drawn
		/// </summary>
		public void RenderPictureToScreenAlternate(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			this.processor.Journal.OpenScope($"RenderPictureToScreenAlternate(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, 0x{pictureAddr:x4})");

			this.owner.VideoCard.RenderPictureToScreen(boxAddr, horizLoc, vertLoc, pictureAddr);
			ClearPointerLatch();

			this.processor.Journal.CloseScope("RenderPictureToScreenAlternate");
		}

		/// <summary>
		/// When the mouse pointer is drawn (0x5403), clears the pointer latch (0x5402). The original loop sets the latch to 1 and
		/// counts it down to 0 at once, so its screen refresh never runs.
		/// </summary>
		private void ClearPointerLatch()
		{
			if (this.processor.PullGlobalByte(0x5403) != 0)
				this.processor.EmitGlobalByte(0x5402, 0x0);
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
			this.processor.Journal.OpenScope($"FloodBox(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, {breadth}, {altitude}, 0x{style:x4})");

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

			// The flag 0x587d of the mouse cleared; when it was on, AX -1 and BX 2 (as the original leaves them)
			byte active = this.processor.PullGlobalByte(0x587d);
			this.processor.Scratch.LowByte = active;
			this.processor.EmitGlobalByte(0x587d, 0);
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(active, active);

			if (active != 0)
			{
				this.processor.RegAcc.Value16 = 0xffff;
				this.processor.RegBase.Value16 = 2;
			}

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
			this.processor.SegData.Value16 = 0x3b01;
			this.processor.RegFrame.Value16 = this.processor.RegCount.Value16;

			byte shift = this.processor.PullGlobalByte(0x587c);
			this.processor.RegCount.LowByte = shift;
			this.processor.RegFrame.Value16 >>= shift;
			this.processor.RegCount.Value16 = this.processor.RegFrame.Value16;

			ushort buttons = this.processor.RegBase.Value16;
			this.processor.EmitGlobalWord(0x5874, (ushort)(this.processor.PullGlobalWord(0x5874) | buttons));
			this.processor.EmitGlobalWord(0x5872, buttons);
			this.processor.EmitGlobalWord(0x586e, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0x5870, this.processor.RegData.Value16);

			ushort condition = this.processor.RegAcc.Value16;
			this.processor.RegFrame.Value16 = condition;

			if ((condition & 1) != 0 && this.processor.PullGlobalByte(0x5403) != 0)
			{
				this.processor.RegAcc.LowByte = 2;
				byte prev = this.processor.PullGlobalByte(0x5402);
				this.processor.EmitGlobalByte(0x5402, 2);
				this.processor.Scratch.LowByte = prev;
				this.processor.RegAcc.LowByte = prev;

				if (prev == 0)
				{
					RenewScreen();
					this.processor.EmitGlobalByte(0x5402, 0);
				}
			}
		}
	}
}
