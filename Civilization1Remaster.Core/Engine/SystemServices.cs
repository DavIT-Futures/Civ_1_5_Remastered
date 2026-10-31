using System;
using System.Drawing;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1000.
	/// </summary>
	public class SystemServices : GameModule
	{
		private const ushort TimerTicksLowAddress = 0x42;
		private const ushort TimerTicksHighAddress = 0x44;
		private const ushort TimerTargetBeatsAddress = 0x46;
		private const ushort TimerBeatsAddress = 0x48;
		private const ushort TimerClockTicksAddress = 0x4a;
		private const ushort TimerDivisorAddress = 0x4c;
		private const ushort TimerRateAddress = 0x4e;
		private const ushort TimerBeatLengthAddress = 0x50;
		private const ushort TimerVoiceStateAddress = 0x52;
		private const ushort TimerCounter54Address = 0x54;
		private const ushort TimerVoiceDurationAddress = 0x56;
		private const ushort TimerActiveFlagAddress = 0x58;
		private const ushort TimerChannelCounterAddress = 0x59;
		private const ushort TimerRunningFlagAddress = 0x5a;
		private const ushort TimerCallbackCountAddress = 0x5c;
		private const ushort TimerSkipCounterAddress = 0x5e;

		private const ushort HueMixerActiveAddress = 0x60;
		private const ushort HueMixerColorAddress = 0x62;
		private const ushort HueMixerCountAddress = 0x64;
		private const ushort HueMixerSpeedAddress = 0x66;
		private const ushort HueMixerBeatsAddress = 0x68;
		private const ushort HueMixerStepAddress = 0x6a;
		private const ushort HueMixerStepRateAddress = 0x6c;

		private const ushort ActiveRectanglePointerAddress = 0xaa;

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

			byte running = this.processor.PullGlobalByte(TimerRunningFlagAddress);
			this.processor.EmitGlobalByte(TimerRunningFlagAddress, 0x1);
			this.processor.RegAcc.LowByte = running;

			if (running == 0)
			{
				this.processor.EmitGlobalWord(TimerDivisorAddress, 0x1);
				this.processor.EmitGlobalWord(TimerCounter54Address, 0x1);
				this.processor.EmitGlobalWord(TimerTicksLowAddress, 0x0);
				this.processor.EmitGlobalWord(TimerTicksHighAddress, 0x0);

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

			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(TimerRunningFlagAddress);
			this.processor.EmitGlobalByte(TimerRunningFlagAddress, 0x0);

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
			(ushort frame, ushort acc, ushort basis, ushort count, ushort data, ushort dataSection) =
				(this.processor.RegFrame.Value16, this.processor.RegAcc.Value16, this.processor.RegBase.Value16,
				this.processor.RegCount.Value16, this.processor.RegData.Value16, this.processor.SegData.Value16);

			ushort oldSP = this.processor.RegStack.Value16;
			ushort oldSS = this.processor.SegStack.Value16;
			this.processor.SegStack.Value16 = 0x1000;
			this.processor.RegStack.Value16 = 0x1a1;
			this.processor.SegData.Value16 = 0x3b01;

			uint oldTime = this.processor.PullUInt32(this.processor.SegData.Value16, TimerTicksLowAddress);
			this.processor.EmitUInt32(this.processor.SegData.Value16, TimerTicksLowAddress, oldTime + this.processor.PullGlobalWord(TimerBeatsAddress));

			ushort counter54 = (ushort)(this.processor.PullGlobalWord(TimerCounter54Address) - 1);
			this.processor.EmitGlobalWord(TimerCounter54Address, counter54);
			if (counter54 == 0)
			{
				this.processor.EmitGlobalWord(TimerCounter54Address, this.processor.PullGlobalWord(TimerDivisorAddress));

				byte counter59 = (byte)(this.processor.PullGlobalByte(TimerChannelCounterAddress) - 1);
				this.processor.EmitGlobalByte(TimerChannelCounterAddress, counter59);
				if (counter59 == 0)
				{
					this.processor.EmitGlobalByte(TimerChannelCounterAddress, 0x14);

					if (this.processor.PullGlobalByte(TimerActiveFlagAddress) != 0)
					{
						this.processor.EmitGlobalByte(TimerActiveFlagAddress, 0);
					}
					else
					{
						// The new tempo (0x46) is read again when it is taken: the accesses to memory are counted by the timer of the tests
						if (this.processor.PullGlobalWord(TimerTargetBeatsAddress) != this.processor.PullGlobalWord(TimerBeatsAddress))
						{
							this.processor.EmitGlobalWord(TimerBeatsAddress, this.processor.PullGlobalWord(TimerTargetBeatsAddress));
							this.processor.EmitGlobalWord(TimerClockTicksAddress, (ushort)(this.processor.PullGlobalWord(TimerClockTicksAddress) + 1));
							this.processor.EmitGlobalWord(TimerVoiceDurationAddress, 0x20);
						}
					}
				}

				OnClockBeat();
			}

			if (this.processor.PullGlobalWord(TimerDivisorAddress) != 1)
			{
				// sound driver call
				RefreshSoundEffects();
			}

			if (this.processor.PullGlobalWord(TimerTicksHighAddress) != 0)
			{
				this.processor.EmitGlobalWord(TimerTicksHighAddress, (ushort)(this.processor.PullGlobalWord(TimerTicksHighAddress) - 1));
			}

			this.processor.SegStack.Value16 = oldSS;
			this.processor.RegStack.Value16 = oldSP;

			(this.processor.RegFrame.Value16, this.processor.RegAcc.Value16, this.processor.RegBase.Value16,
				this.processor.RegCount.Value16, this.processor.RegData.Value16, this.processor.SegData.Value16) = (frame, acc, basis, count, data, dataSection);
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

			this.processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			this.processor.EmitGlobalByte(TimerActiveFlagAddress, 0x0);
			this.processor.EmitGlobalWord(TimerVoiceStateAddress, 0x0);
			this.processor.EmitUInt32(this.processor.SegData.Value16, TimerTicksLowAddress, this.processor.PullUInt32(this.processor.SegData.Value16, TimerTicksLowAddress) + measured);
			this.processor.EmitGlobalWord(TimerBeatLengthAddress, beatLength);
			this.processor.EmitGlobalWord(TimerRateAddress, rate);

			if (this.processor.PullGlobalWord(TimerDivisorAddress) != 0x1)
			{
				this.processor.EmitGlobalWord(TimerDivisorAddress, rate);
			}

			ushort divisor = this.processor.PullGlobalWord(TimerDivisorAddress);
			ushort beats = (ushort)(beatLength / divisor);

			this.processor.EmitGlobalWord(TimerBeatsAddress, beats);
			this.processor.EmitGlobalWord(TimerTargetBeatsAddress, beats);

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

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(TimerCallbackCountAddress);
			this.processor.EmitGlobalWord(TimerCallbackCountAddress, 0);

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

			this.processor.EmitGlobalWord(TimerCallbackCountAddress, (ushort)(this.processor.PullGlobalWord(TimerCallbackCountAddress) + 1));

			AssignColors();

			RefreshHueCycling();

			if (this.processor.PullGlobalWord(TimerRateAddress) == 4)
			{
				sbyte skip = (sbyte)this.processor.PullGlobalByte(TimerSkipCounterAddress);

				this.processor.EmitGlobalByte(TimerSkipCounterAddress, (byte)(skip - 1));

				if (skip - 1 <= 0)
				{
					this.processor.EmitGlobalByte(TimerSkipCounterAddress, 0x7);

					this.processor.Journal.CloseScope("OnClockBeat");
					return;
				}
			}

			short speed = (short)AdvanceSong();

			if (speed < 0)
			{
				this.processor.EmitGlobalWord(TimerDivisorAddress, 0x1);
				this.processor.EmitGlobalWord(TimerTargetBeatsAddress, this.processor.PullGlobalWord(TimerBeatLengthAddress));
				this.processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			}
			else if (speed > 0)
			{
				ushort rate = this.processor.PullGlobalWord(TimerRateAddress);

				this.processor.EmitGlobalWord(TimerDivisorAddress, rate);

				ushort beatLength = this.processor.PullGlobalWord(TimerBeatLengthAddress);

				if (rate == 0)
					throw new Exception("Division by zero");

				this.processor.RegBase.Value16 = rate;
				this.processor.RegData.Value16 = (ushort)(beatLength % rate);
				this.processor.RegAcc.Value16 = (ushort)(beatLength / rate);

				this.processor.EmitGlobalWord(TimerTargetBeatsAddress, this.processor.RegAcc.Value16);
				this.processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			}

			this.processor.Journal.CloseScope("OnClockBeat");
		}

		/// <summary>
		/// Sets the colour cycle 1 - 8 (a record of the segment 0x35cf, see ActivateHueCycle) to the colours first - last (at
		/// most 16), with the delay in beats; the current colours are kept twice in the record, so that a cycle is one copy.
		/// Also writes 0xffff to 0x35cf:0. The registers as the original leaves them.
		/// </summary>
		public void AssignHueCycleSpan(ushort cycleNumber, ushort delay, ushort firstHue, ushort lastHue)
		{
			this.processor.Journal.OpenScope($"AssignHueCycleSpan({cycleNumber}, {delay}, {firstHue}, {lastHue})");

			this.owner.HueCycles.SetWord(0x0, 0xffff);
			this.processor.RegAcc.Value16 = 0xffff;

			ushort cycleIdx = (ushort)(cycleNumber - 1);
			this.processor.RegBase.Value16 = cycleIdx;

			if (cycleIdx < 8)
			{
				ushort cycle = HueCycleRecord(cycleIdx);
				ushort span = (ushort)(lastHue - firstHue);

				this.processor.RegAcc.Value16 = firstHue;
				this.processor.RegData.Value16 = span;

				if (lastHue > firstHue)
				{
					ushort colours = (ushort)Math.Min(span + 1, 0x10);
					byte first = (byte)firstHue;

					this.owner.HueCycles.SetWord((ushort)(cycle + 4), (ushort)(colours * 3));
					this.owner.HueCycles.SetByte((ushort)(cycle + 6), first);

					this.processor.DisableInterrupts();

					if (!this.owner.VideoCard.Displays.IncludesKey(0))
						throw new Exception($"The screen {0} is not allocated");

					lock (this.owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
						ushort stagingLoc = (ushort)(cycle + 0x9);

						for (int idx = 0; idx < colours; idx++)
						{
							Color hue = VGABitmap.HueToHue18(display.ObtainHue(idx + first));

							this.owner.HueCycles.SetByte(stagingLoc++, hue.R);
							this.owner.HueCycles.SetByte(stagingLoc++, hue.G);
							this.owner.HueCycles.SetByte(stagingLoc++, hue.B);
						}
					}

					this.processor.EnableInterrupts();

					// The colours again after them, but the last one (rep movsb with ES = the cycle segment, straight in the memory
					// without clock steps)
					ushort hues = (ushort)(cycle + 0x9);
					ushort copyLength = (ushort)(this.owner.HueCycles.Word((ushort)(cycle + 4)) - 3);

					for (int idx = 0; idx < copyLength; idx++)
					{
						this.owner.HueCycles.SetByte((ushort)(hues + (colours * 3) + idx),
							this.owner.HueCycles.Byte((ushort)(hues + idx)));
					}

					this.owner.HueCycles.SetByte((ushort)(cycle + 0x7), (byte)delay);
					this.owner.HueCycles.SetByte((ushort)(cycle + 0x8), 0x0);
					this.owner.HueCycles.SetWord((ushort)(cycle + 0x2), 0x0);

					this.processor.SegExtra.Value16 = HueCycleSegment;
					this.processor.RegAcc.Value16 = delay;
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
		public void ActivateHueCycle(ushort cycleNumber)
		{
			this.processor.Journal.OpenScope($"ActivateHueCycle({cycleNumber})");

			// Like the original: AX = |cycleNumber| - 1, DX = the sign of cycleNumber
			bool backwards = (short)cycleNumber < 0;
			ushort cycleIdx = (ushort)(Math.Abs((int)(short)cycleNumber) - 1);
			this.processor.RegData.Value16 = (ushort)(backwards ? 0xffff : 0);

			if (cycleIdx < 8)
			{
				ushort cycle = HueCycleRecord(cycleIdx);

				this.owner.HueCycles.SetWord(cycle, (ushort)(backwards ? -3 : 3));
				this.owner.HueCycles.SetByte((ushort)(cycle + 8), this.owner.HueCycles.Byte((ushort)(cycle + 7)));
			}

			this.processor.Journal.CloseScope("ActivateHueCycle");
		}

		/// <summary>The segment of the colour cycles (a table of 8 record addresses at 2)</summary>
		private const ushort HueCycleSegment = HueCycles.Segment;

		private ushort HueCycleRecord(ushort cycleIdx)
		{
			ushort cycle = this.owner.HueCycles.Word((ushort)((cycleIdx << 1) + 2));
			this.processor.RegBase.Value16 = cycle;

			return cycle;
		}

		/// <summary>
		/// Stops the colour cycle 1 - 8
		/// </summary>
		public void DeactivateHueCycle(ushort cycleNumber)
		{
			this.processor.Journal.OpenScope($"DeactivateHueCycle({cycleNumber})");

			ushort cycleIdx = (ushort)(cycleNumber - 1);
			this.processor.RegBase.Value16 = cycleIdx;

			if (cycleIdx < 8)
			{
				this.owner.HueCycles.SetByte((ushort)(HueCycleRecord(cycleIdx) + 8), 0);
			}

			this.processor.Journal.CloseScope("DeactivateHueCycle");
		}

		/// <summary>
		/// One step of every active colour cycle whose delay has run out (the cycle records of the segment 0x35cf)
		/// </summary>
		public void RefreshHueCycling()
		{
			this.processor.Journal.OpenScope("RefreshHueCycling()");

			ushort savedData = this.processor.SegData.Value16;
			this.processor.SegData.Value16 = 0x35cf;
			ushort cycleOffset = 2;

			while (true)
			{
				ushort cycle = this.owner.HueCycles.Word(cycleOffset);
				this.processor.RegBase.Value16 = cycle;
				cycleOffset = (ushort)(cycleOffset + 2);

				if (cycle == 0xffff)
				{
					break;
				}

				// Inactive cycle (timer 0), or not yet time for the next step: the timer is read again to count it down, as in the original
				if (this.owner.HueCycles.Byte((ushort)(cycle + 8)) == 0)
				{
					continue;
				}

				byte timer = (byte)(this.owner.HueCycles.Byte((ushort)(cycle + 8)) - 1);
				this.owner.HueCycles.SetByte((ushort)(cycle + 8), timer);
				if (timer != 0)
				{
					continue;
				}

				byte initialDelay = this.owner.HueCycles.Byte((ushort)(cycle + 7));
				this.processor.RegAcc.LowByte = initialDelay;
				this.owner.HueCycles.SetByte((ushort)(cycle + 8), initialDelay);

				ushort count = this.owner.HueCycles.Word((ushort)(cycle + 4));
				this.processor.RegCount.Value16 = count;

				// One step back (forward for a backward cycle), wrapped around the colours of the cycle
				short position = (short)this.owner.HueCycles.Word((ushort)(cycle + 2));
				short step = (short)this.owner.HueCycles.Word(cycle);
				short source = (short)(position - step);

				if (position < step)
				{
					source += (short)count;
				}

				if (source >= (short)count)
				{
					source = 0;
				}

				this.owner.HueCycles.SetWord((ushort)(cycle + 2), (ushort)source);

				ushort paletteOffset = (ushort)(cycle + 9);
				this.processor.RegAcc.Value16 = paletteOffset;

				byte startHue = this.owner.HueCycles.Byte((ushort)(cycle + 6));
				this.processor.RegAcc.LowByte = startHue;

				if (this.owner.VideoCard.Displays.IncludesKey(0))
				{
					lock (this.owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.owner.VideoCard.Displays.ObtainValueByKey(0);
						ushort stagingLoc = (ushort)(source + paletteOffset);
						int total = count / 3;

						for (int idx = 0; idx < total; idx++)
						{
							byte crimson = this.owner.HueCycles.Byte(stagingLoc++);
							byte emerald = this.owner.HueCycles.Byte(stagingLoc++);
							byte azure = this.owner.HueCycles.Byte(stagingLoc++);

							display.AssignHue(idx + startHue, VGABitmap.Hue18ToHue(crimson, emerald, azure));
						}
					}
				}
				else
				{
					throw new Exception($"The screen 0 is not allocated");
				}
			}

			this.processor.SegData.Value16 = savedData;

			this.processor.Journal.CloseScope("RefreshHueCycling");
		}

		/// <summary>
		/// Fades the colours to the palette of a picture (the 0x300 colour bytes after the 6 byte header at swatchAddr) in
		/// steps * 6 timer beats and waits until the fade is done
		/// </summary>
		public void ImportColorsFromBuffer(ushort steps, ushort swatchAddr)
		{
			this.processor.Journal.OpenScope($"ImportColorsFromBuffer({steps}, 0x{swatchAddr:x4})");

			// The target palette goes to TextBuffer.Address (rep movsb with ES = DS, which the original leaves in ES). Copied straight in the
			// memory like rep movsb, without clock steps: with a colour cycle on, an earlier timer beat would change the
			// colours that PullCurrentColors takes
			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;

			for (int idx = 0; idx < 0x300; idx++)
			{
				this.processor.Storage.EmitUInt8(this.processor.SegData.Value16, (ushort)(TextBuffer.Address + idx),
					this.processor.Storage.PullUInt8(this.processor.SegData.Value16, (ushort)(swatchAddr + 6 + idx)));
			}

			FadeToTargetColors(steps);

			this.processor.Journal.CloseScope("ImportColorsFromBuffer");
		}

		/// <summary>
		/// Fades all 256 colours to one colour (red, green, blue: 0 - 63) in steps * 6 timer beats and waits until the fade
		/// is done
		/// </summary>
		public void FloodColors(ushort steps, byte red, byte green, byte blue)
		{
			this.processor.Journal.OpenScope($"FloodColors({steps}, {red}, {green}, {blue})");

			// The original leaves DS in ES
			this.processor.SegExtra.Value16 = this.processor.SegData.Value16;

			for (int idx = 0; idx < 0x100; idx++)
			{
				ushort colour = (ushort)(TextBuffer.Address + (3 * idx));

				this.processor.EmitGlobalByte(colour, red);
				this.processor.EmitGlobalByte((ushort)(colour + 1), green);
				this.processor.EmitGlobalByte((ushort)(colour + 2), blue);
			}

			FadeToTargetColors(steps);

			this.processor.Journal.CloseScope("FloodColors");
		}

		/// <summary>
		/// The common part of ImportColorsFromBuffer and FloodColors: the current colours to 0xbd06, the fade to the colours
		/// at TextBuffer.Address, then waiting until the timer interrupt has finished it (0x68 = 0)
		/// </summary>
		private void FadeToTargetColors(ushort steps)
		{
			PullCurrentColors();

			FadeColors(steps);

			while (this.processor.PullGlobalWord(HueMixerBeatsAddress) != 0)
			{
				this.processor.Idle();
			}
		}

		/// <summary>
		/// Starts a fade from the colours at 0xbd06 to the colours at TextBuffer.Address in steps * 6 timer beats (0x68): the colours per
		/// beat (0x64, from the measured speed 0x66 / 14, at most 0x100), the first colour (0x62), the mix step (0x6c) and the
		/// mix (0x6a); the first step is done at once
		/// </summary>
		public void FadeColors(ushort steps)
		{
			this.processor.Journal.OpenScope($"FadeColors({steps})");

			ushort beats = (ushort)(steps * 6);
			ushort speed = this.processor.PullGlobalWord(HueMixerSpeedAddress);

			if (speed == 0)
			{
				speed = GaugeColorsPerformance();
				this.processor.EmitGlobalWord(HueMixerSpeedAddress, speed);
			}

			ushort colours = (ushort)(speed / 0xe);

			if (colours > 0xff)
				colours = 0x100;

			this.processor.EmitGlobalWord(HueMixerCountAddress, colours);

			// Written, read back and negated like the original: the same number of clock steps before the fade starts (0x68)
			this.processor.EmitGlobalWord(HueMixerColorAddress, colours);
			this.processor.EmitGlobalWord(HueMixerColorAddress, (ushort)-this.processor.PullGlobalWord(HueMixerColorAddress));

			// 0x10000 / (colours * beats / 0x100 - 1), a 32 / 16 bit division like the original (and its errors)
			ushort divisor = (ushort)(((uint)colours * beats >> 8) - 1);

			if (divisor == 0)
				throw new Exception("Division by zero");

			if (divisor == 1)
				throw new Exception("Division error");

			ushort mixStep = (ushort)(0x10000 / divisor);

			this.processor.EmitGlobalWord(HueMixerStepRateAddress, mixStep);
			this.processor.EmitGlobalWord(HueMixerStepAddress, mixStep);

			ColorsFadeStep();

			this.processor.EmitGlobalWord(HueMixerBeatsAddress, beats);

			this.processor.Journal.CloseScope("FadeColors");
		}

		/// <summary>Copies the 256 colours of screen 0 to 0xbd06 (red, green, blue of 6 bits), the start of a fade</summary>
		public void PullCurrentColors()
		{
			this.processor.Journal.OpenScope("PullCurrentColors()");

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
		/// Measures how fast the colours can be set (the first 10 colours from 0xbd06); returns the speed, always the highest
		/// (65530)
		/// </summary>
		public ushort GaugeColorsPerformance()
		{
			this.processor.Journal.OpenScope("GaugeColorsPerformance()");

			this.processor.EmitGlobalWord(HueMixerActiveAddress, 0xffff);

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
		/// colours at 0xbd06 (from) and TextBuffer.Address (to), (from * ~mix + to * mix) / 0x10000 rounded, with the mix 0x6a; after the last colour
		/// the mix grows by 0x6c (at most 0xffff). The memory accesses are those of the original (the beats of the test timer
		/// depend on them; the colours are written straight into memory like stosb), and AX, BX, CX, DX are left as it leaves
		/// them; SI, DI, BP and ES are kept.
		/// </summary>
		private void MixFadeColours()
		{
			this.processor.DisableInterrupts();

			ushort previous = this.processor.PullGlobalWord(HueMixerColorAddress);
			ushort count = this.processor.PullGlobalWord(HueMixerCountAddress);
			ushort first = (ushort)((previous + count) & 0xff);

			this.processor.EmitGlobalWord(HueMixerColorAddress, first);

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
					uint product = (uint)from * (ushort)~this.processor.PullGlobalWord(HueMixerStepAddress);

					mixed = product;
					acc = (ushort)product;
					data = (ushort)(product >> 16);
				}

				byte to = this.processor.PullGlobalByte((ushort)(source + TextBuffer.Address));

				acc = to;

				if (to != 0)
				{
					uint product = (uint)to * this.processor.PullGlobalWord(HueMixerStepAddress);

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
					acc = this.processor.PullGlobalWord(HueMixerStepRateAddress);

					uint mix = (uint)this.processor.PullGlobalWord(HueMixerStepAddress) + acc;

					this.processor.EmitGlobalWord(HueMixerStepAddress, (ushort)mix);

					if (mix > 0xffff)
					{
						this.processor.EmitGlobalWord(HueMixerStepAddress, 0xffff);
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
		/// A step of a fade: while beats are left (HueMixerBeats), the colours from 0xc006 go to screen 0 from the first colour
		/// of the step on, then the next step is mixed
		/// </summary>
		public void AssignColors()
		{
			this.processor.Journal.OpenScope("AssignColors()");

			if (this.processor.PullGlobalWord(HueMixerBeatsAddress) != 0)
			{
				this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(HueMixerColorAddress);
				this.processor.RegBase.Value16 = this.processor.PullGlobalWord(HueMixerCountAddress);
				
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

				this.processor.EmitGlobalWord(HueMixerBeatsAddress, this.processor.DecrementWord(this.processor.PullGlobalWord(HueMixerBeatsAddress)));

				ColorsFadeStepInterrupt();
			}
			
			this.processor.Journal.CloseScope("AssignColors");
		}

		/// <summary>Whether the file named at the address is in the folder of the game: 0, else 0xffff</summary>
		public ushort PathPresent(ushort documentLabelAddr)
		{
			string documentLabel = CRuntime.ObtainDOSFileLabel(this.owner.Processor.PullText(CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, documentLabelAddr)).ToUpper());

			this.processor.Journal.OpenScope($"PathPresent('{documentLabel}')");

			if (File.Exists($"{this.owner.Processor.StandardFolder}{documentLabel}"))
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0xffff;
			}

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
		public void RenderCursorToScreen(short horizLoc, short vertLoc, ushort picture)
		{
			this.processor.Journal.OpenScope($"RenderCursorToScreen({horizLoc}, {vertLoc}, {picture})");

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
			if (this.owner.Pointer.PointerShown != 0)
				this.owner.Pointer.RefreshLatch = 0x0;
		}

		/// <summary>Draws the picture into the box at (horizLoc, vertLoc)</summary>
		public void RenderPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			this.processor.Journal.OpenScope($"RenderPictureToScreen(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, 0x{pictureAddr:x4})");

			this.owner.VideoCard.RenderPictureToScreen(boxAddr, horizLoc, vertLoc, pictureAddr);

			this.processor.Journal.CloseScope("RenderPictureToScreen");
		}

		/// <summary>Prepares the sound driver</summary>
		public void PrepareSoundDriver()
		{
			this.owner.Audio.Prepare();
		}

		/// <summary>
		/// Sound function, possibly: Play Music
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void PerformSound(ushort audio, ushort argument)
		{
			if ((this.owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.owner.Audio.PerformSound(audio, argument);
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
			this.owner.Audio.RefreshVibratoTremolo();
		}

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		public ushort ObtainSongPosition()
		{
			// Test seam: the fixed test clock counts the polls of the music (nothing in the game)
			this.processor.PollMusic();

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

			CivRectangle box = new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, boxAddr));

			if (breadth > 0 && altitude > 0)
			{
				Rectangle box1 = new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, breadth, altitude);
				this.owner.VideoCard.FloodBox(box.DisplayIdx, box1, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

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
			CivRectangle box = new CivRectangle(this.owner.Processor, CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, this.owner.Processor.PullUInt16(this.owner.Processor.SegData.Value16, ActiveRectanglePointerAddress)));

			this.processor.Journal.OpenScope($"AssignDot({box.DisplayIdx}, {horizLoc}, {vertLoc}, 0x{style:x4})");

			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				this.owner.VideoCard.AssignDot(box.DisplayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

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
			CivRectangle box = new CivRectangle(this.owner.Processor, CPU.AsLinearAddress(this.owner.Processor.SegData.Value16, this.owner.Processor.PullUInt16(this.owner.Processor.SegData.Value16, ActiveRectanglePointerAddress)));

			this.processor.Journal.OpenScope($"AssignDot({displayIdx}, {horizLoc}, {vertLoc}, 0x{style:x4})");

			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				this.owner.VideoCard.AssignDot(displayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			this.processor.Journal.CloseScope("AssignDot");
		}

		/// <summary>
		/// Resets and inits the mouse subsystem
		/// </summary>
		/// <returns>true if mouse is detected</returns>
		public ushort PrepareMouse()
		{
			this.processor.Journal.OpenScope("PrepareMouse()");

			// assume our mouse is initialized

			this.processor.RegCount.LowByte = 0;
			this.processor.RegAcc.Value16 = (ushort)(this.owner.VideoCard.DisplayPointerSite.X >> this.processor.RegCount.LowByte);
			this.owner.Pointer.ColumnShift = this.processor.RegCount.LowByte;
			this.owner.Pointer.Horiz = this.processor.RegAcc.Value16;
			this.owner.Pointer.Vert = (ushort)this.owner.VideoCard.DisplayPointerSite.Y;
			this.owner.Pointer.Buttons = this.owner.VideoCard.DisplayPointerClicks;
			this.processor.RegAcc.Value16 = 0xffff;

			this.owner.Pointer.DriverActive = this.processor.RegAcc.LowByte;

			this.processor.Journal.CloseScope("PrepareMouse");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Turns the mouse driver off (the flag 0x587d cleared; when it was on, AX -1 and BX 2, as the original leaves them)
		/// </summary>
		public void DeactivateMouse()
		{
			this.processor.Journal.OpenScope("DeactivateMouse()");

			// The flag 0x587d of the mouse cleared; when it was on, AX -1 and BX 2 (as the original leaves them)
			byte active = this.owner.Pointer.DriverActive;
			this.processor.Scratch.LowByte = active;
			this.owner.Pointer.DriverActive = 0;
			this.processor.RegAcc.LowByte = this.processor.BitOrByte(active, active);

			if (active != 0)
			{
				this.processor.RegAcc.Value16 = 0xffff;
				this.processor.RegBase.Value16 = 2;
			}

			this.processor.Journal.CloseScope("DeactivateMouse");
		}

		/// <summary>The picture of the mouse pointer and its hot spot</summary>
		public void AssignMouseCursorImage(ushort hotHoriz, ushort hotVert, ushort picture)
		{
			this.processor.Journal.OpenScope($"AssignMouseCursorImage({hotHoriz}, {hotVert}, {picture})");

			PointerHotHoriz = hotHoriz;
			PointerHotVert = hotVert;
			PointerImage = picture;

			this.processor.Journal.CloseScope("AssignMouseCursorImage");
		}

		/// <summary>
		/// Moves the mouse pointer to the position (0x586e, 0x5870)
		/// </summary>
		public void AssignMousePosition(ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"AssignMousePosition({horizLoc}, {vertLoc})");

			this.owner.Pointer.Horiz = horizLoc;
			this.owner.Pointer.Vert = vertLoc;

			this.processor.Journal.CloseScope("AssignMousePosition");
		}

		/// <summary>The clicks since the last call (cleared); the interrupts are served first</summary>
		public ushort ObtainAndWipeMouseClick()
		{
			// The interrupts come before the clicks are read (see GameStartup.ObtainMouseState)
			this.processor.PumpMessages();

			ushort clicks = this.owner.Pointer.Clicks;
			this.owner.Pointer.Clicks = 0;

			return clicks;
		}

		/// <summary>
		/// Draws the mouse pointer (picture 0x5876, hot spot 0x5878, 0x587a) at the mouse position (0x586e, 0x5870), when there
		/// is a picture and the pointer is not drawn yet (0x5403)
		/// </summary>
		public void RenderMouseCursor()
		{
			this.processor.Journal.OpenScope("RenderMouseCursor()");

			if (PointerImage != 0 && this.owner.Pointer.PointerShown == 0)
			{
				RenderCursorToScreen((short)((short)this.owner.Pointer.Horiz - (short)PointerHotHoriz),
					(short)((short)this.owner.Pointer.Vert - (short)PointerHotVert),
					PointerImage);

				this.owner.Pointer.PointerShown = 0x1;
			}

			this.processor.Journal.CloseScope("RenderMouseCursor");
		}

		/// <summary>
		/// Puts back the screen under the mouse pointer, when the pointer has a picture (0x5876) and is drawn (0x5403)
		/// </summary>
		public void BlankMouseCursor()
		{
			this.processor.Journal.OpenScope("BlankMouseCursor()");

			if (PointerImage != 0 && this.owner.Pointer.PointerShown != 0)
			{
				this.owner.Pointer.PointerShown = 0x0;

				RecoverScreenUnderCursor();
			}

			this.processor.Journal.CloseScope("BlankMouseCursor");
		}

		/// <summary>
		/// Copies the screen buffer to the window (from the mouse interrupt; the registers kept outside the emulated stack)
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

			byte shift = this.owner.Pointer.ColumnShift;
			this.processor.RegCount.LowByte = shift;
			this.processor.RegFrame.Value16 >>= shift;
			this.processor.RegCount.Value16 = this.processor.RegFrame.Value16;

			ushort buttons = this.processor.RegBase.Value16;
			this.owner.Pointer.Clicks = (ushort)(this.owner.Pointer.Clicks | buttons);
			this.owner.Pointer.Buttons = buttons;
			this.owner.Pointer.Horiz = this.processor.RegCount.Value16;
			this.owner.Pointer.Vert = this.processor.RegData.Value16;

			ushort condition = this.processor.RegAcc.Value16;
			this.processor.RegFrame.Value16 = condition;

			if ((condition & 1) != 0 && this.owner.Pointer.PointerShown != 0)
			{
				this.processor.RegAcc.LowByte = 2;
				byte prev = this.owner.Pointer.RefreshLatch;
				this.owner.Pointer.RefreshLatch = 2;
				this.processor.Scratch.LowByte = prev;
				this.processor.RegAcc.LowByte = prev;

				if (prev == 0)
				{
					RenewScreen();
					this.owner.Pointer.RefreshLatch = 0;
				}
			}
		}
	}
}
