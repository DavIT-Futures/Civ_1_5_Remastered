using System;
using System.Drawing;
using System.IO;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1000.
	/// </summary>
	public class PlatformServices : GameComponent, IPlatformServices
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

		/// <summary>The segment of the colour cycles (a table of 8 record addresses at 2)</summary>
		private const ushort HueCycleSegment = HueCycles.Segment;

		public PlatformServices(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void LaunchTimer()
		{
			this.Processor.Journal.OpenScope("LaunchTimer()");

			byte running = this.Processor.PullGlobalByte(TimerRunningFlagAddress);
			this.Processor.EmitGlobalByte(TimerRunningFlagAddress, 0x1);
			this.Processor.RegAcc.LowByte = running;

			if (running == 0)
			{
				this.Processor.EmitGlobalWord(TimerDivisorAddress, 0x1);
				this.Processor.EmitGlobalWord(TimerCounter54Address, 0x1);
				this.Processor.EmitGlobalWord(TimerTicksLowAddress, 0x0);
				this.Processor.EmitGlobalWord(TimerTicksHighAddress, 0x0);

				PrepareTimer();

				this.Processor.AllowClock = true;
			}

			this.Processor.Journal.CloseScope("LaunchTimer");
		}

		/// <inheritdoc/>
		public void HaltTimer()
		{
			this.Processor.Journal.OpenScope("HaltTimer()");

			this.Processor.RegAcc.LowByte = this.Processor.PullGlobalByte(TimerRunningFlagAddress);
			this.Processor.EmitGlobalByte(TimerRunningFlagAddress, 0x0);

			this.Processor.Journal.CloseScope("HaltTimer");
		}

		/// <inheritdoc/>
		public void ClockInterrupt()
		{
			this.Processor.Journal.OpenScope("ClockInterrupt()");

			// The interrupted registers are kept outside the emulated stack (the original pushes them onto the stack of the game):
			// what an interrupt leaves below SP would depend on when it comes, and the game reads uninitialized locals there
			(ushort frame, ushort acc, ushort basis, ushort count, ushort data, ushort dataSection) =
				(this.Processor.RegFrame.Value16, this.Processor.RegAcc.Value16, this.Processor.RegBase.Value16,
				this.Processor.RegCount.Value16, this.Processor.RegData.Value16, this.Processor.SegData.Value16);

			ushort oldSP = this.Processor.RegStack.Value16;
			ushort oldSS = this.Processor.SegStack.Value16;
			this.Processor.SegStack.Value16 = 0x1000;
			this.Processor.RegStack.Value16 = 0x1a1;
			this.Processor.SegData.Value16 = 0x3b01;

			uint oldTime = this.Processor.PullUInt32(this.Processor.SegData.Value16, TimerTicksLowAddress);
			this.Processor.EmitUInt32(this.Processor.SegData.Value16, TimerTicksLowAddress, oldTime + this.Processor.PullGlobalWord(TimerBeatsAddress));

			ushort counter54 = (ushort)(this.Processor.PullGlobalWord(TimerCounter54Address) - 1);
			this.Processor.EmitGlobalWord(TimerCounter54Address, counter54);
			if (counter54 == 0)
			{
				this.Processor.EmitGlobalWord(TimerCounter54Address, this.Processor.PullGlobalWord(TimerDivisorAddress));

				byte counter59 = (byte)(this.Processor.PullGlobalByte(TimerChannelCounterAddress) - 1);
				this.Processor.EmitGlobalByte(TimerChannelCounterAddress, counter59);
				if (counter59 == 0)
				{
					this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x14);

					if (this.Processor.PullGlobalByte(TimerActiveFlagAddress) != 0)
					{
						this.Processor.EmitGlobalByte(TimerActiveFlagAddress, 0);
					}
					else
					{
						// The new tempo (0x46) is read again when it is taken: the accesses to memory are counted by the timer of the tests
						if (this.Processor.PullGlobalWord(TimerTargetBeatsAddress) != this.Processor.PullGlobalWord(TimerBeatsAddress))
						{
							this.Processor.EmitGlobalWord(TimerBeatsAddress, this.Processor.PullGlobalWord(TimerTargetBeatsAddress));
							this.Processor.EmitGlobalWord(TimerClockTicksAddress, (ushort)(this.Processor.PullGlobalWord(TimerClockTicksAddress) + 1));
							this.Processor.EmitGlobalWord(TimerVoiceDurationAddress, 0x20);
						}
					}
				}

				OnClockBeat();
			}

			if (this.Processor.PullGlobalWord(TimerDivisorAddress) != 1)
			{
				// sound driver call
				RefreshSoundEffects();
			}

			if (this.Processor.PullGlobalWord(TimerTicksHighAddress) != 0)
			{
				this.Processor.EmitGlobalWord(TimerTicksHighAddress, (ushort)(this.Processor.PullGlobalWord(TimerTicksHighAddress) - 1));
			}

			this.Processor.SegStack.Value16 = oldSS;
			this.Processor.RegStack.Value16 = oldSP;

			(this.Processor.RegFrame.Value16, this.Processor.RegAcc.Value16, this.Processor.RegBase.Value16,
				this.Processor.RegCount.Value16, this.Processor.RegData.Value16, this.Processor.SegData.Value16) = (frame, acc, basis, count, data, dataSection);
			// IRET - Pop flags and Far return

			this.Processor.Journal.CloseScope("ClockInterrupt");
		}

		/// <inheritdoc/>
		public void PrepareTimer()
		{
			this.Processor.Journal.OpenScope("PrepareTimer()");

			const ushort measured = 0xff9c;
			const ushort beatLength = 0x4dae;
			const ushort rate = 5;

			this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			this.Processor.EmitGlobalByte(TimerActiveFlagAddress, 0x0);
			this.Processor.EmitGlobalWord(TimerVoiceStateAddress, 0x0);
			this.Processor.EmitUInt32(this.Processor.SegData.Value16, TimerTicksLowAddress, this.Processor.PullUInt32(this.Processor.SegData.Value16, TimerTicksLowAddress) + measured);
			this.Processor.EmitGlobalWord(TimerBeatLengthAddress, beatLength);
			this.Processor.EmitGlobalWord(TimerRateAddress, rate);

			if (this.Processor.PullGlobalWord(TimerDivisorAddress) != 0x1)
			{
				this.Processor.EmitGlobalWord(TimerDivisorAddress, rate);
			}

			ushort divisor = this.Processor.PullGlobalWord(TimerDivisorAddress);
			ushort beats = (ushort)(beatLength / divisor);

			this.Processor.EmitGlobalWord(TimerBeatsAddress, beats);
			this.Processor.EmitGlobalWord(TimerTargetBeatsAddress, beats);

			this.Processor.RegAcc.Value16 = beats;
			this.Processor.RegData.Value16 = (ushort)(beatLength % divisor);
			this.Processor.RegBase.Value16 = 0xf89;
			this.Processor.RegCount.Value16 = 0x10;

			this.Processor.Journal.CloseScope("PrepareTimer");
		}

		/// <inheritdoc/>
		public ushort RestoreTimer()
		{
			this.Processor.Journal.OpenScope("RestoreTimer()");

			this.Processor.RegAcc.Value16 = this.Processor.PullGlobalWord(TimerCallbackCountAddress);
			this.Processor.EmitGlobalWord(TimerCallbackCountAddress, 0);

			this.Processor.Journal.CloseScope("RestoreTimer");

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public void OnClockBeat()
		{
			this.Processor.Journal.OpenScope("OnClockBeat()");

			this.Processor.EmitGlobalWord(TimerCallbackCountAddress, (ushort)(this.Processor.PullGlobalWord(TimerCallbackCountAddress) + 1));

			AssignColors();

			RefreshHueCycling();

			if (this.Processor.PullGlobalWord(TimerRateAddress) == 4)
			{
				sbyte skip = (sbyte)this.Processor.PullGlobalByte(TimerSkipCounterAddress);

				this.Processor.EmitGlobalByte(TimerSkipCounterAddress, (byte)(skip - 1));

				if (skip - 1 <= 0)
				{
					this.Processor.EmitGlobalByte(TimerSkipCounterAddress, 0x7);

					this.Processor.Journal.CloseScope("OnClockBeat");
					return;
				}
			}

			short speed = (short)AdvanceSong();

			if (speed < 0)
			{
				this.Processor.EmitGlobalWord(TimerDivisorAddress, 0x1);
				this.Processor.EmitGlobalWord(TimerTargetBeatsAddress, this.Processor.PullGlobalWord(TimerBeatLengthAddress));
				this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			}
			else if (speed > 0)
			{
				ushort rate = this.Processor.PullGlobalWord(TimerRateAddress);

				this.Processor.EmitGlobalWord(TimerDivisorAddress, rate);

				ushort beatLength = this.Processor.PullGlobalWord(TimerBeatLengthAddress);

				if (rate == 0)
					throw new Exception("Division by zero");

				this.Processor.RegBase.Value16 = rate;
				this.Processor.RegData.Value16 = (ushort)(beatLength % rate);
				this.Processor.RegAcc.Value16 = (ushort)(beatLength / rate);

				this.Processor.EmitGlobalWord(TimerTargetBeatsAddress, this.Processor.RegAcc.Value16);
				this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			}

			this.Processor.Journal.CloseScope("OnClockBeat");
		}

		/// <inheritdoc/>
		public void AssignHueCycleSpan(ushort cycleNumber, ushort delay, ushort firstHue, ushort lastHue)
		{
			this.Processor.Journal.OpenScope($"AssignHueCycleSpan({cycleNumber}, {delay}, {firstHue}, {lastHue})");

			this.Owner.HueCycles.SetWord(0x0, 0xffff);
			this.Processor.RegAcc.Value16 = 0xffff;

			ushort cycleIdx = (ushort)(cycleNumber - 1);
			this.Processor.RegBase.Value16 = cycleIdx;

			if (cycleIdx < 8)
			{
				ushort cycle = HueCycleRecord(cycleIdx);
				ushort span = (ushort)(lastHue - firstHue);

				this.Processor.RegAcc.Value16 = firstHue;
				this.Processor.RegData.Value16 = span;

				if (lastHue > firstHue)
				{
					ushort colours = (ushort)Math.Min(span + 1, 0x10);
					byte first = (byte)firstHue;

					this.Owner.HueCycles.SetWord((ushort)(cycle + 4), (ushort)(colours * 3));
					this.Owner.HueCycles.SetByte((ushort)(cycle + 6), first);

					this.Processor.DisableInterrupts();

					if (!this.Owner.VideoCard.Displays.IncludesKey(0))
						throw new Exception($"The screen {0} is not allocated");

					lock (this.Owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.Owner.VideoCard.Displays.ObtainValueByKey(0);
						ushort stagingLoc = (ushort)(cycle + 0x9);

						for (int idx = 0; idx < colours; idx++)
						{
							Color hue = VGABitmap.HueToHue18(display.ObtainHue(idx + first));

							this.Owner.HueCycles.SetByte(stagingLoc++, hue.R);
							this.Owner.HueCycles.SetByte(stagingLoc++, hue.G);
							this.Owner.HueCycles.SetByte(stagingLoc++, hue.B);
						}
					}

					this.Processor.EnableInterrupts();

					// The colours again after them, but the last one (rep movsb with ES = the cycle segment, straight in the memory
					// without clock steps)
					ushort hues = (ushort)(cycle + 0x9);
					ushort copyLength = (ushort)(this.Owner.HueCycles.Word((ushort)(cycle + 4)) - 3);

					for (int idx = 0; idx < copyLength; idx++)
					{
						this.Owner.HueCycles.SetByte((ushort)(hues + (colours * 3) + idx),
							this.Owner.HueCycles.Byte((ushort)(hues + idx)));
					}

					this.Owner.HueCycles.SetByte((ushort)(cycle + 0x7), (byte)delay);
					this.Owner.HueCycles.SetByte((ushort)(cycle + 0x8), 0x0);
					this.Owner.HueCycles.SetWord((ushort)(cycle + 0x2), 0x0);

					this.Processor.SegExtra.Value16 = HueCycleSegment;
					this.Processor.RegAcc.Value16 = delay;
					this.Processor.RegData.Value16 = colours;
					this.Processor.RegCount.Value16 = 0;
				}
			}

			this.Processor.Journal.CloseScope("AssignHueCycleSpan");
		}

		/// <inheritdoc/>
		public void ActivateHueCycle(ushort cycleNumber)
		{
			this.Processor.Journal.OpenScope($"ActivateHueCycle({cycleNumber})");

			// Like the original: AX = |cycleNumber| - 1, DX = the sign of cycleNumber
			bool backwards = (short)cycleNumber < 0;
			ushort cycleIdx = (ushort)(Math.Abs((int)(short)cycleNumber) - 1);
			this.Processor.RegData.Value16 = (ushort)(backwards ? 0xffff : 0);

			if (cycleIdx < 8)
			{
				ushort cycle = HueCycleRecord(cycleIdx);

				this.Owner.HueCycles.SetWord(cycle, (ushort)(backwards ? -3 : 3));
				this.Owner.HueCycles.SetByte((ushort)(cycle + 8), this.Owner.HueCycles.Byte((ushort)(cycle + 7)));
			}

			this.Processor.Journal.CloseScope("ActivateHueCycle");
		}

		/// <inheritdoc/>
		public void DeactivateHueCycle(ushort cycleNumber)
		{
			this.Processor.Journal.OpenScope($"DeactivateHueCycle({cycleNumber})");

			ushort cycleIdx = (ushort)(cycleNumber - 1);
			this.Processor.RegBase.Value16 = cycleIdx;

			if (cycleIdx < 8)
			{
				this.Owner.HueCycles.SetByte((ushort)(HueCycleRecord(cycleIdx) + 8), 0);
			}

			this.Processor.Journal.CloseScope("DeactivateHueCycle");
		}

		/// <inheritdoc/>
		public void RefreshHueCycling()
		{
			this.Processor.Journal.OpenScope("RefreshHueCycling()");

			ushort savedData = this.Processor.SegData.Value16;
			this.Processor.SegData.Value16 = 0x35cf;
			ushort cycleOffset = 2;

			while (true)
			{
				ushort cycle = this.Owner.HueCycles.Word(cycleOffset);
				this.Processor.RegBase.Value16 = cycle;
				cycleOffset = (ushort)(cycleOffset + 2);

				if (cycle == 0xffff)
				{
					break;
				}

				// Inactive cycle (timer 0), or not yet time for the next step: the timer is read again to count it down, as in the original
				if (this.Owner.HueCycles.Byte((ushort)(cycle + 8)) == 0)
				{
					continue;
				}

				byte timer = (byte)(this.Owner.HueCycles.Byte((ushort)(cycle + 8)) - 1);
				this.Owner.HueCycles.SetByte((ushort)(cycle + 8), timer);
				if (timer != 0)
				{
					continue;
				}

				byte initialDelay = this.Owner.HueCycles.Byte((ushort)(cycle + 7));
				this.Processor.RegAcc.LowByte = initialDelay;
				this.Owner.HueCycles.SetByte((ushort)(cycle + 8), initialDelay);

				ushort count = this.Owner.HueCycles.Word((ushort)(cycle + 4));
				this.Processor.RegCount.Value16 = count;

				// One step back (forward for a backward cycle), wrapped around the colours of the cycle
				short position = (short)this.Owner.HueCycles.Word((ushort)(cycle + 2));
				short step = (short)this.Owner.HueCycles.Word(cycle);
				short source = (short)(position - step);

				if (position < step)
				{
					source += (short)count;
				}

				if (source >= (short)count)
				{
					source = 0;
				}

				this.Owner.HueCycles.SetWord((ushort)(cycle + 2), (ushort)source);

				ushort paletteOffset = (ushort)(cycle + 9);
				this.Processor.RegAcc.Value16 = paletteOffset;

				byte startHue = this.Owner.HueCycles.Byte((ushort)(cycle + 6));
				this.Processor.RegAcc.LowByte = startHue;

				if (this.Owner.VideoCard.Displays.IncludesKey(0))
				{
					lock (this.Owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.Owner.VideoCard.Displays.ObtainValueByKey(0);
						ushort stagingLoc = (ushort)(source + paletteOffset);
						int total = count / 3;

						for (int idx = 0; idx < total; idx++)
						{
							byte crimson = this.Owner.HueCycles.Byte(stagingLoc++);
							byte emerald = this.Owner.HueCycles.Byte(stagingLoc++);
							byte azure = this.Owner.HueCycles.Byte(stagingLoc++);

							display.AssignHue(idx + startHue, VGABitmap.Hue18ToHue(crimson, emerald, azure));
						}
					}
				}
				else
				{
					throw new Exception($"The screen 0 is not allocated");
				}
			}

			this.Processor.SegData.Value16 = savedData;

			this.Processor.Journal.CloseScope("RefreshHueCycling");
		}

		/// <inheritdoc/>
		public void LoadColorsFromBuffer(ushort steps, ushort swatchAddr)
		{
			this.Processor.Journal.OpenScope($"ImportColorsFromBuffer({steps}, 0x{swatchAddr:x4})");

			// The target palette goes to TextBuffer.Address (rep movsb with ES = DS, which the original leaves in ES). Copied straight in the
			// memory like rep movsb, without clock steps: with a colour cycle on, an earlier timer beat would change the
			// colours that PullCurrentColors takes
			this.Processor.SegExtra.Value16 = this.Processor.SegData.Value16;

			for (int idx = 0; idx < 0x300; idx++)
			{
				this.Processor.Storage.EmitUInt8(this.Processor.SegData.Value16, (ushort)(TextBuffer.Address + idx),
					this.Processor.Storage.PullUInt8(this.Processor.SegData.Value16, (ushort)(swatchAddr + 6 + idx)));
			}

			FadeToTargetColors(steps);

			this.Processor.Journal.CloseScope("ImportColorsFromBuffer");
		}

		/// <inheritdoc/>
		public void FillColors(ushort steps, byte red, byte green, byte blue)
		{
			this.Processor.Journal.OpenScope($"FloodColors({steps}, {red}, {green}, {blue})");

			// The original leaves DS in ES
			this.Processor.SegExtra.Value16 = this.Processor.SegData.Value16;

			for (int idx = 0; idx < 0x100; idx++)
			{
				ushort colour = (ushort)(TextBuffer.Address + (3 * idx));

				this.Processor.EmitGlobalByte(colour, red);
				this.Processor.EmitGlobalByte((ushort)(colour + 1), green);
				this.Processor.EmitGlobalByte((ushort)(colour + 2), blue);
			}

			FadeToTargetColors(steps);

			this.Processor.Journal.CloseScope("FloodColors");
		}

		/// <inheritdoc/>
		public void FadeColors(ushort steps)
		{
			this.Processor.Journal.OpenScope($"FadeColors({steps})");

			ushort beats = (ushort)(steps * 6);
			ushort speed = this.Processor.PullGlobalWord(HueMixerSpeedAddress);

			if (speed == 0)
			{
				speed = GaugeColorsPerformance();
				this.Processor.EmitGlobalWord(HueMixerSpeedAddress, speed);
			}

			ushort colours = (ushort)(speed / 0xe);

			if (colours > 0xff)
				colours = 0x100;

			this.Processor.EmitGlobalWord(HueMixerCountAddress, colours);

			// Written, read back and negated like the original: the same number of clock steps before the fade starts (0x68)
			this.Processor.EmitGlobalWord(HueMixerColorAddress, colours);
			this.Processor.EmitGlobalWord(HueMixerColorAddress, (ushort)-this.Processor.PullGlobalWord(HueMixerColorAddress));

			// 0x10000 / (colours * beats / 0x100 - 1), a 32 / 16 bit division like the original (and its errors)
			ushort divisor = (ushort)(((uint)colours * beats >> 8) - 1);

			if (divisor == 0)
				throw new Exception("Division by zero");

			if (divisor == 1)
				throw new Exception("Division error");

			ushort mixStep = (ushort)(0x10000 / divisor);

			this.Processor.EmitGlobalWord(HueMixerStepRateAddress, mixStep);
			this.Processor.EmitGlobalWord(HueMixerStepAddress, mixStep);

			ColorsFadeStep();

			this.Processor.EmitGlobalWord(HueMixerBeatsAddress, beats);

			this.Processor.Journal.CloseScope("FadeColors");
		}

		/// <inheritdoc/>
		public void PullCurrentColors()
		{
			this.Processor.Journal.OpenScope("PullCurrentColors()");

			this.Processor.DisableInterrupts();
			if (this.Owner.VideoCard.Displays.IncludesKey(0))
			{
				lock (this.Owner.VideoCard.VideoLatch)
				{
					VGABitmap display = this.Owner.VideoCard.Displays.ObtainValueByKey(0);
					uint stagingLoc = CPU.AsLinearAddress(this.Processor.SegData.Value16, 0xbd06);

					for (int idx = 0; idx < 0x100; idx++)
					{
						Color hue = VGABitmap.HueToHue18(display.ObtainHue(idx));

						this.Processor.Storage.EmitUInt8(stagingLoc++, hue.R);
						this.Processor.Storage.EmitUInt8(stagingLoc++, hue.G);
						this.Processor.Storage.EmitUInt8(stagingLoc++, hue.B);
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}
			this.Processor.EnableInterrupts();

			// Near return
			this.Processor.Journal.CloseScope("PullCurrentColors");
		}

		/// <inheritdoc/>
		public ushort GaugeColorsPerformance()
		{
			this.Processor.Journal.OpenScope("GaugeColorsPerformance()");

			this.Processor.EmitGlobalWord(HueMixerActiveAddress, 0xffff);

			this.Processor.DisableInterrupts();
			if (this.Owner.VideoCard.Displays.IncludesKey(0))
			{
				lock (this.Owner.VideoCard.VideoLatch)
				{
					VGABitmap display = this.Owner.VideoCard.Displays.ObtainValueByKey(0);
					uint stagingLoc = CPU.AsLinearAddress(this.Processor.SegData.Value16, 0xbd06);

					for (int idx = 0; idx < 10; idx++)
					{
						byte crimson = this.Processor.Storage.PullUInt8(stagingLoc++);
						byte emerald = this.Processor.Storage.PullUInt8(stagingLoc++);
						byte azure = this.Processor.Storage.PullUInt8(stagingLoc++);

						display.AssignHue(idx, VGABitmap.Hue18ToHue(crimson, emerald, azure));
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}
			this.Processor.EnableInterrupts();

			this.Processor.RegAcc.Value16 = 6553 * 10; // maximum performance

			// Near return
			this.Processor.Journal.CloseScope("GaugeColorsPerformance");

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public void ColorsFadeStep()
		{
			this.Processor.Journal.OpenScope("ColorsFadeStep()");

			MixFadeColours();

			this.Processor.Journal.CloseScope("ColorsFadeStep");
		}

		/// <inheritdoc/>
		public void ColorsFadeStepInterrupt()
		{
			this.Processor.Journal.OpenScope("ColorsFadeStepInterrupt()");

			MixFadeColours();

			this.Processor.Journal.CloseScope("ColorsFadeStepInterrupt");
		}

		/// <inheritdoc/>
		public void AssignColors()
		{
			this.Processor.Journal.OpenScope("AssignColors()");

			if (this.Processor.PullGlobalWord(HueMixerBeatsAddress) != 0)
			{
				this.Processor.RegAcc.Value16 = this.Processor.PullGlobalWord(HueMixerColorAddress);
				this.Processor.RegBase.Value16 = this.Processor.PullGlobalWord(HueMixerCountAddress);
				
				if (this.Owner.VideoCard.Displays.IncludesKey(0))
				{
					lock (this.Owner.VideoCard.VideoLatch)
					{
						VGABitmap display = this.Owner.VideoCard.Displays.ObtainValueByKey(0);
						uint stagingLoc = CPU.AsLinearAddress(this.Processor.SegData.Value16, 0xc006);

						for (int idx = 0; idx < this.Processor.RegBase.Value16; idx++)
						{
							byte crimson = this.Processor.Storage.PullUInt8(stagingLoc++);
							byte emerald = this.Processor.Storage.PullUInt8(stagingLoc++);
							byte azure = this.Processor.Storage.PullUInt8(stagingLoc++);

							display.AssignHue(idx + this.Processor.RegAcc.LowByte, VGABitmap.Hue18ToHue(crimson, emerald, azure));
						}
					}
				}
				else
				{
					throw new Exception($"The screen {0} is not allocated");
				}

				this.Processor.EmitGlobalWord(HueMixerBeatsAddress, this.Processor.DecrementWord(this.Processor.PullGlobalWord(HueMixerBeatsAddress)));

				ColorsFadeStepInterrupt();
			}
			
			this.Processor.Journal.CloseScope("AssignColors");
		}

		/// <inheritdoc/>
		public ushort PathPresent(ushort documentLabelAddr)
		{
			string documentLabel = RuntimeLibrary.ObtainDOSFileLabel(this.Owner.Processor.PullText(CPU.AsLinearAddress(this.Owner.Processor.SegData.Value16, documentLabelAddr)).ToUpper());

			this.Processor.Journal.OpenScope($"PathPresent('{documentLabel}')");

			if (File.Exists($"{this.Owner.Processor.StandardFolder}{documentLabel}"))
			{
				this.Processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.Processor.RegAcc.Value16 = 0xffff;
			}

			this.Processor.Journal.CloseScope("PathPresent");

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public void RecoverScreenUnderCursor()
		{
			this.Processor.Journal.OpenScope("RecoverScreenUnderCursor()");

			this.Owner.VideoCard.DrawBufferToScreen();
			ClearPointerLatch();

			this.Processor.Journal.CloseScope("RecoverScreenUnderCursor");
		}

		/// <inheritdoc/>
		public void DrawCursorToScreen(short horizLoc, short vertLoc, ushort picture)
		{
			this.Processor.Journal.OpenScope($"RenderCursorToScreen({horizLoc}, {vertLoc}, {picture})");

			ClearPointerLatch();

			this.Processor.Journal.CloseScope("RenderCursorToScreen");
		}

		/// <inheritdoc/>
		public void ShowScreen(ushort displayIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayScreen({displayIdx})");

			this.Owner.VideoCard.DuplicateScreenToMainScreen(displayIdx);
			ClearPointerLatch();

			this.Processor.Journal.CloseScope("DisplayScreen");
		}

		/// <inheritdoc/>
		public void DrawPictureToScreenAlternate(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			this.Processor.Journal.OpenScope($"RenderPictureToScreenAlternate(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, 0x{pictureAddr:x4})");

			this.Owner.VideoCard.DrawPictureToScreen(boxAddr, horizLoc, vertLoc, pictureAddr);
			ClearPointerLatch();

			this.Processor.Journal.CloseScope("RenderPictureToScreenAlternate");
		}

		/// <inheritdoc/>
		public void DrawPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr)
		{
			this.Processor.Journal.OpenScope($"RenderPictureToScreen(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, 0x{pictureAddr:x4})");

			this.Owner.VideoCard.DrawPictureToScreen(boxAddr, horizLoc, vertLoc, pictureAddr);

			this.Processor.Journal.CloseScope("RenderPictureToScreen");
		}

		/// <inheritdoc/>
		public void PrepareSoundDriver()
		{
			this.Owner.Audio.Prepare();
		}

		/// <inheritdoc/>
		public void PerformSound(ushort audio, ushort argument)
		{
			if ((this.Owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.Owner.Audio.PerformSound(audio, argument);
			}
		}

		/// <inheritdoc/>
		public void PerformCityFoundedSound()
		{
			if ((this.Owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.Owner.Audio.PerformChopping();
			}
		}

		/// <inheritdoc/>
		public ushort HaltSound()
		{
			this.Processor.RegAcc.Value16 =  this.Owner.Audio.Halt();

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public ushort AdvanceSong()
		{
			this.Processor.RegAcc.Value16 =  this.Owner.Audio.PerformNextStep();

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public void RefreshSoundEffects()
		{
			this.Owner.Audio.RefreshVibratoTremolo();
		}

		/// <inheritdoc/>
		public ushort ReadSongPosition()
		{
			// Test seam: the fixed test clock counts the polls of the music (nothing in the game)
			this.Processor.PollMusic();

			this.Processor.RegAcc.Value16 = this.Owner.Audio.ReadSongPosition();

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public void FloodBox(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			this.Processor.Journal.OpenScope($"FloodBox(0x{boxAddr:x4}, {horizLoc}, {vertLoc}, {breadth}, {altitude}, 0x{style:x4})");

			CivRectangle box = new CivRectangle(this.Processor, CPU.AsLinearAddress(this.Processor.SegData.Value16, boxAddr));

			if (breadth > 0 && altitude > 0)
			{
				Rectangle box1 = new Rectangle(box.Horiz + horizLoc, box.Vert + vertLoc, breadth, altitude);
				this.Owner.VideoCard.FloodBox(box.DisplayIdx, box1, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			this.Processor.Journal.CloseScope("FloodBox");
		}

		/// <inheritdoc/>
		public void AssignDot(int horizLoc, int vertLoc, ushort style)
		{
			CivRectangle box = new CivRectangle(this.Owner.Processor, CPU.AsLinearAddress(this.Owner.Processor.SegData.Value16, this.Owner.Processor.PullUInt16(this.Owner.Processor.SegData.Value16, ActiveRectanglePointerAddress)));

			this.Processor.Journal.OpenScope($"AssignDot({box.DisplayIdx}, {horizLoc}, {vertLoc}, 0x{style:x4})");

			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				this.Owner.VideoCard.AssignDot(box.DisplayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			this.Processor.Journal.CloseScope("AssignDot");
		}

		/// <inheritdoc/>
		public void AssignDot(ushort displayIdx, int horizLoc, int vertLoc, ushort style)
		{
			CivRectangle box = new CivRectangle(this.Owner.Processor, CPU.AsLinearAddress(this.Owner.Processor.SegData.Value16, this.Owner.Processor.PullUInt16(this.Owner.Processor.SegData.Value16, ActiveRectanglePointerAddress)));

			this.Processor.Journal.OpenScope($"AssignDot({displayIdx}, {horizLoc}, {vertLoc}, 0x{style:x4})");

			if (horizLoc >= 0 && horizLoc <= box.Breadth && vertLoc >= 0 && vertLoc <= box.Altitude)
			{
				this.Owner.VideoCard.AssignDot(displayIdx, box.Horiz + horizLoc, box.Vert + vertLoc, (byte)(style & 0xff), (byte)((style & 0xff00) >> 8));
			}

			this.Processor.Journal.CloseScope("AssignDot");
		}

		/// <inheritdoc/>
		public ushort PrepareMouse()
		{
			this.Processor.Journal.OpenScope("PrepareMouse()");

			// assume our mouse is initialized

			this.Processor.RegCount.LowByte = 0;
			this.Processor.RegAcc.Value16 = (ushort)(this.Owner.VideoCard.DisplayPointerSite.X >> this.Processor.RegCount.LowByte);
			this.Owner.Pointer.ColumnShift = this.Processor.RegCount.LowByte;
			this.Owner.Pointer.Horiz = this.Processor.RegAcc.Value16;
			this.Owner.Pointer.Vert = (ushort)this.Owner.VideoCard.DisplayPointerSite.Y;
			this.Owner.Pointer.Buttons = this.Owner.VideoCard.DisplayPointerClicks;
			this.Processor.RegAcc.Value16 = 0xffff;

			this.Owner.Pointer.DriverActive = this.Processor.RegAcc.LowByte;

			this.Processor.Journal.CloseScope("PrepareMouse");

			return this.Processor.RegAcc.Value16;
		}

		/// <inheritdoc/>
		public void DeactivateMouse()
		{
			this.Processor.Journal.OpenScope("DeactivateMouse()");

			// The flag 0x587d of the mouse cleared; when it was on, AX -1 and BX 2 (as the original leaves them)
			byte active = this.Owner.Pointer.DriverActive;
			this.Processor.Scratch.LowByte = active;
			this.Owner.Pointer.DriverActive = 0;
			this.Processor.RegAcc.LowByte = this.Processor.BitOrByte(active, active);

			if (active != 0)
			{
				this.Processor.RegAcc.Value16 = 0xffff;
				this.Processor.RegBase.Value16 = 2;
			}

			this.Processor.Journal.CloseScope("DeactivateMouse");
		}

		/// <inheritdoc/>
		public void AssignMouseCursorImage(ushort hotHoriz, ushort hotVert, ushort picture)
		{
			this.Processor.Journal.OpenScope($"AssignMouseCursorImage({hotHoriz}, {hotVert}, {picture})");

			PointerHotHoriz = hotHoriz;
			PointerHotVert = hotVert;
			PointerImage = picture;

			this.Processor.Journal.CloseScope("AssignMouseCursorImage");
		}

		/// <inheritdoc/>
		public void AssignMousePosition(ushort horizLoc, ushort vertLoc)
		{
			this.Processor.Journal.OpenScope($"AssignMousePosition({horizLoc}, {vertLoc})");

			this.Owner.Pointer.Horiz = horizLoc;
			this.Owner.Pointer.Vert = vertLoc;

			this.Processor.Journal.CloseScope("AssignMousePosition");
		}

		/// <inheritdoc/>
		public ushort TakeMouseClick()
		{
			// The interrupts come before the clicks are read (see GameLauncher.ReadMouseState)
			this.Processor.PumpMessages();

			ushort clicks = this.Owner.Pointer.Clicks;
			this.Owner.Pointer.Clicks = 0;

			return clicks;
		}

		/// <inheritdoc/>
		public void DrawMouseCursor()
		{
			this.Processor.Journal.OpenScope("RenderMouseCursor()");

			if (PointerImage != 0 && this.Owner.Pointer.PointerShown == 0)
			{
				DrawCursorToScreen((short)((short)this.Owner.Pointer.Horiz - (short)PointerHotHoriz),
					(short)((short)this.Owner.Pointer.Vert - (short)PointerHotVert),
					PointerImage);

				this.Owner.Pointer.PointerShown = 0x1;
			}

			this.Processor.Journal.CloseScope("RenderMouseCursor");
		}

		/// <inheritdoc/>
		public void BlankMouseCursor()
		{
			this.Processor.Journal.OpenScope("BlankMouseCursor()");

			if (PointerImage != 0 && this.Owner.Pointer.PointerShown != 0)
			{
				this.Owner.Pointer.PointerShown = 0x0;

				RecoverScreenUnderCursor();
			}

			this.Processor.Journal.CloseScope("BlankMouseCursor");
		}

		/// <inheritdoc/>
		public void RenewScreen()
		{
			this.Processor.Journal.OpenScope("RenewScreen()");

			// Called from the mouse interrupt: the registers are kept outside the emulated stack, like in ClockInterrupt
			(ushort acc, ushort data, ushort extraSection) =
				(this.Processor.RegAcc.Value16, this.Processor.RegData.Value16, this.Processor.SegExtra.Value16);

			this.Processor.SegExtra.Value16 = 0x1000;
			this.Owner.VideoCard.DrawBufferToScreen();

			(this.Processor.RegAcc.Value16, this.Processor.RegData.Value16, this.Processor.SegExtra.Value16) = (acc, data, extraSection);

			this.Processor.Journal.CloseScope("RenewScreen");
		}

		/// <inheritdoc/>
		public void PointerEvent()
		{
			this.Processor.SegData.Value16 = 0x3b01;
			this.Processor.RegFrame.Value16 = this.Processor.RegCount.Value16;

			byte shift = this.Owner.Pointer.ColumnShift;
			this.Processor.RegCount.LowByte = shift;
			this.Processor.RegFrame.Value16 >>= shift;
			this.Processor.RegCount.Value16 = this.Processor.RegFrame.Value16;

			ushort buttons = this.Processor.RegBase.Value16;
			this.Owner.Pointer.Clicks = (ushort)(this.Owner.Pointer.Clicks | buttons);
			this.Owner.Pointer.Buttons = buttons;
			this.Owner.Pointer.Horiz = this.Processor.RegCount.Value16;
			this.Owner.Pointer.Vert = this.Processor.RegData.Value16;

			ushort condition = this.Processor.RegAcc.Value16;
			this.Processor.RegFrame.Value16 = condition;

			if ((condition & 1) != 0 && this.Owner.Pointer.PointerShown != 0)
			{
				this.Processor.RegAcc.LowByte = 2;
				byte prev = this.Owner.Pointer.RefreshLatch;
				this.Owner.Pointer.RefreshLatch = 2;
				this.Processor.Scratch.LowByte = prev;
				this.Processor.RegAcc.LowByte = prev;

				if (prev == 0)
				{
					RenewScreen();
					this.Owner.Pointer.RefreshLatch = 0;
				}
			}
		}

		private ushort HueCycleRecord(ushort cycleIdx)
		{
			ushort cycle = this.Owner.HueCycles.Word((ushort)((cycleIdx << 1) + 2));
			this.Processor.RegBase.Value16 = cycle;

			return cycle;
		}

		/// <summary>
		/// The common part of LoadColorsFromBuffer and FillColors: the current colours to 0xbd06, the fade to the colours
		/// at TextBuffer.Address, then waiting until the timer interrupt has finished it (0x68 = 0)
		/// </summary>
		private void FadeToTargetColors(ushort steps)
		{
			PullCurrentColors();

			FadeColors(steps);

			while (this.Processor.PullGlobalWord(HueMixerBeatsAddress) != 0)
			{
				this.Processor.Idle();
			}
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
			this.Processor.DisableInterrupts();

			ushort previous = this.Processor.PullGlobalWord(HueMixerColorAddress);
			ushort count = this.Processor.PullGlobalWord(HueMixerCountAddress);
			ushort first = (ushort)((previous + count) & 0xff);

			this.Processor.EmitGlobalWord(HueMixerColorAddress, first);

			ushort source = (ushort)(3 * first);
			ushort target = 0xc006;
			ushort bytes = (ushort)(3 * count);
			ushort acc = first;
			ushort data = this.Processor.RegData.Value16;
			ushort sum = 0;

			do
			{
				uint mixed = 0;

				byte from = this.Processor.PullGlobalByte((ushort)(source + 0xbd06));

				if (from != 0)
				{
					uint product = (uint)from * (ushort)~this.Processor.PullGlobalWord(HueMixerStepAddress);

					mixed = product;
					acc = (ushort)product;
					data = (ushort)(product >> 16);
				}

				byte to = this.Processor.PullGlobalByte((ushort)(source + TextBuffer.Address));

				acc = to;

				if (to != 0)
				{
					uint product = (uint)to * this.Processor.PullGlobalWord(HueMixerStepAddress);

					mixed += product;
					acc = (ushort)product;
					data = (ushort)(product >> 16);
				}

				// The high word, rounded with the top bit of the low word
				sum = (ushort)((mixed >> 16) + ((mixed >> 15) & 1));
				acc = (ushort)((acc & 0xff00) | (sum & 0xff));

				this.Processor.Storage.EmitUInt8(this.Processor.SegData.Value16, target++, (byte)sum);

				source++;

				if (source >= 0x300)
				{
					source = 0;
					acc = this.Processor.PullGlobalWord(HueMixerStepRateAddress);

					uint mix = (uint)this.Processor.PullGlobalWord(HueMixerStepAddress) + acc;

					this.Processor.EmitGlobalWord(HueMixerStepAddress, (ushort)mix);

					if (mix > 0xffff)
					{
						this.Processor.EmitGlobalWord(HueMixerStepAddress, 0xffff);
					}
				}

				bytes--;
			}
			while (bytes != 0);

			this.Processor.RegAcc.Value16 = acc;
			this.Processor.RegBase.Value16 = sum;
			this.Processor.RegCount.Value16 = 0;
			this.Processor.RegData.Value16 = data;

			this.Processor.EnableInterrupts();
		}

		/// <summary>
		/// When the mouse pointer is drawn (0x5403), clears the pointer latch (0x5402). The original loop sets the latch to 1 and
		/// counts it down to 0 at once, so its screen refresh never runs.
		/// </summary>
		private void ClearPointerLatch()
		{
			if (this.Owner.Pointer.PointerShown != 0)
				this.Owner.Pointer.RefreshLatch = 0x0;
		}
	}
}
