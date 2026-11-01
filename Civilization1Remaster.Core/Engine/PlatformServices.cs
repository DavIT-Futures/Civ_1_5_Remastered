using System;
using System.IO;

namespace Civilization1Remaster.Core.Engine
{
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
		private const ushort TimerCounterAddress = 0x54;
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

		private const ushort HueCycleSegment = HueCycles.Segment;

		public PlatformServices(GameEngine owner)
			: base(owner)
		{
		}

		private ushort ActiveBox => this.Processor.PullWord(this.Processor.SegmentData.Word, ActiveRectanglePointerAddress);

		public void LaunchTimer()
		{
			using var _ = this.Processor.Journal.Scope("LaunchTimer()");

			byte running = this.Processor.PullGlobalByte(TimerRunningFlagAddress);
			this.Processor.EmitGlobalByte(TimerRunningFlagAddress, 0x1);
			this.Processor.RegisterAccumulator.LowByte = running;

			if (running == 0)
			{
				this.Processor.EmitGlobalWord(TimerDivisorAddress, 0x1);
				this.Processor.EmitGlobalWord(TimerCounterAddress, 0x1);
				this.Processor.EmitGlobalWord(TimerTicksLowAddress, 0x0);
				this.Processor.EmitGlobalWord(TimerTicksHighAddress, 0x0);

				PrepareTimer();

				this.Processor.AllowClock = true;
			}
		}

		public void HaltTimer()
		{
			using var _ = this.Processor.Journal.Scope("HaltTimer()");

			this.Processor.RegisterAccumulator.LowByte = this.Processor.PullGlobalByte(TimerRunningFlagAddress);
			this.Processor.EmitGlobalByte(TimerRunningFlagAddress, 0x0);
		}

		public void ClockInterrupt()
		{
			using var _ = this.Processor.Journal.Scope("ClockInterrupt()");

			(ushort frame, ushort accumulator, ushort basis, ushort count, ushort data, ushort dataSection) =
				(this.Processor.RegisterFrame.Word, this.Processor.RegisterAccumulator.Word, this.Processor.RegisterBase.Word,
				this.Processor.RegisterCount.Word, this.Processor.RegisterData.Word, this.Processor.SegmentData.Word);

			ushort oldStackPointer = this.Processor.RegisterStack.Word;
			ushort oldStackSegment = this.Processor.SegmentStack.Word;
			this.Processor.SegmentStack.Word = 0x1000;
			this.Processor.RegisterStack.Word = 0x1a1;
			this.Processor.SegmentData.Word = 0x3b01;

			uint oldTime = this.Processor.PullDoubleWord(this.Processor.SegmentData.Word, TimerTicksLowAddress);
			this.Processor.EmitDoubleWord(this.Processor.SegmentData.Word, TimerTicksLowAddress, oldTime + this.Processor.PullGlobalWord(TimerBeatsAddress));

			ushort timerCounter = (ushort)(this.Processor.PullGlobalWord(TimerCounterAddress) - 1);
			this.Processor.EmitGlobalWord(TimerCounterAddress, timerCounter);
			if (timerCounter == 0)
			{
				this.Processor.EmitGlobalWord(TimerCounterAddress, this.Processor.PullGlobalWord(TimerDivisorAddress));

				byte secondTimerCounter = (byte)(this.Processor.PullGlobalByte(TimerChannelCounterAddress) - 1);
				this.Processor.EmitGlobalByte(TimerChannelCounterAddress, secondTimerCounter);
				if (secondTimerCounter == 0)
				{
					this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x14);

					if (this.Processor.PullGlobalByte(TimerActiveFlagAddress) != 0)
					{
						this.Processor.EmitGlobalByte(TimerActiveFlagAddress, 0);
					}
					else
					{
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
				RefreshSoundEffects();
			}

			if (this.Processor.PullGlobalWord(TimerTicksHighAddress) != 0)
			{
				this.Processor.EmitGlobalWord(TimerTicksHighAddress, (ushort)(this.Processor.PullGlobalWord(TimerTicksHighAddress) - 1));
			}

			this.Processor.SegmentStack.Word = oldStackSegment;
			this.Processor.RegisterStack.Word = oldStackPointer;

			(this.Processor.RegisterFrame.Word, this.Processor.RegisterAccumulator.Word, this.Processor.RegisterBase.Word,
				this.Processor.RegisterCount.Word, this.Processor.RegisterData.Word, this.Processor.SegmentData.Word) = (frame, accumulator, basis, count, data, dataSection);
		}

		public void PrepareTimer()
		{
			using var _ = this.Processor.Journal.Scope("PrepareTimer()");

			const ushort measured = 0xff9c;
			const ushort beatLength = 0x4dae;
			const ushort rate = 5;

			this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			this.Processor.EmitGlobalByte(TimerActiveFlagAddress, 0x0);
			this.Processor.EmitGlobalWord(TimerVoiceStateAddress, 0x0);
			this.Processor.EmitDoubleWord(this.Processor.SegmentData.Word, TimerTicksLowAddress, this.Processor.PullDoubleWord(this.Processor.SegmentData.Word, TimerTicksLowAddress) + measured);
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

			this.Processor.RegisterAccumulator.Word = beats;
			this.Processor.RegisterData.Word = (ushort)(beatLength % divisor);
			this.Processor.RegisterBase.Word = 0xf89;
			this.Processor.RegisterCount.Word = 0x10;
		}

		public ushort RestoreTimer()
		{
			using var _ = this.Processor.Journal.Scope("RestoreTimer()");

			this.Processor.RegisterAccumulator.Word = this.Processor.PullGlobalWord(TimerCallbackCountAddress);
			this.Processor.EmitGlobalWord(TimerCallbackCountAddress, 0);

			return this.Processor.RegisterAccumulator.Word;
		}

		public void OnClockBeat()
		{
			using var _ = this.Processor.Journal.Scope("OnClockBeat()");

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
				{
					throw new Exception("Division by zero");
				}

				this.Processor.RegisterBase.Word = rate;
				this.Processor.RegisterData.Word = (ushort)(beatLength % rate);
				this.Processor.RegisterAccumulator.Word = (ushort)(beatLength / rate);

				this.Processor.EmitGlobalWord(TimerTargetBeatsAddress, this.Processor.RegisterAccumulator.Word);
				this.Processor.EmitGlobalByte(TimerChannelCounterAddress, 0x1);
			}
		}

		public void AssignHueCycleSpan(ushort cycleNumber, ushort delay, ushort firstHue, ushort lastHue)
		{
			using var _ = this.Processor.Journal.Scope($"AssignHueCycleSpan({cycleNumber}, {delay}, {firstHue}, {lastHue})");

			this.Owner.HueCycles.SetWord(0x0, 0xffff);
			this.Processor.RegisterAccumulator.Word = 0xffff;

			ushort cycleIndex = (ushort)(cycleNumber - 1);
			this.Processor.RegisterBase.Word = cycleIndex;

			if (cycleIndex < 8)
			{
				ushort cycle = HueCycleRecord(cycleIndex);
				ushort span = (ushort)(lastHue - firstHue);

				this.Processor.RegisterAccumulator.Word = firstHue;
				this.Processor.RegisterData.Word = span;

				if (lastHue > firstHue)
				{
					ushort colours = (ushort)Math.Min(span + 1, 0x10);
					byte first = (byte)firstHue;

					this.Owner.HueCycles.SetWord((ushort)(cycle + 4), (ushort)(colours * 3));
					this.Owner.HueCycles.SetByte((ushort)(cycle + 6), first);

					this.Processor.DisableInterrupts();

					if (!this.Owner.VideoCard.Displays.ContainsKey(0))
					{
						throw new Exception($"The screen {0} is not allocated");
					}

					lock (this.Owner.VideoCard.VideoLatch)
					{
						VideoGraphicsBitmap display = this.Owner.VideoCard.Displays[0];
						ushort stagingLocation = (ushort)(cycle + 0x9);

						for (int index = 0; index < colours; index++)
						{
							Color hue = VideoGraphicsBitmap.HueToDigitalLevels(display.ObtainHue(index + first));

							this.Owner.HueCycles.SetByte(stagingLocation++, hue.R);
							this.Owner.HueCycles.SetByte(stagingLocation++, hue.G);
							this.Owner.HueCycles.SetByte(stagingLocation++, hue.B);
						}
					}

					this.Processor.EnableInterrupts();

					ushort hues = (ushort)(cycle + 0x9);
					ushort copyLength = (ushort)(this.Owner.HueCycles.Word((ushort)(cycle + 4)) - 3);

					for (int index = 0; index < copyLength; index++)
					{
						this.Owner.HueCycles.SetByte((ushort)(hues + (colours * 3) + index),
							this.Owner.HueCycles.Byte((ushort)(hues + index)));
					}

					this.Owner.HueCycles.SetByte((ushort)(cycle + 0x7), (byte)delay);
					this.Owner.HueCycles.SetByte((ushort)(cycle + 0x8), 0x0);
					this.Owner.HueCycles.SetWord((ushort)(cycle + 0x2), 0x0);

					this.Processor.SegmentExtra.Word = HueCycleSegment;
					this.Processor.RegisterAccumulator.Word = delay;
					this.Processor.RegisterData.Word = colours;
					this.Processor.RegisterCount.Word = 0;
				}
			}
		}

		public void ActivateHueCycle(ushort cycleNumber)
		{
			using var _ = this.Processor.Journal.Scope($"ActivateHueCycle({cycleNumber})");

			bool backwards = (short)cycleNumber < 0;
			ushort cycleIndex = (ushort)(Math.Abs((int)(short)cycleNumber) - 1);
			this.Processor.RegisterData.Word = (ushort)(backwards ? 0xffff : 0);

			if (cycleIndex < 8)
			{
				ushort cycle = HueCycleRecord(cycleIndex);

				this.Owner.HueCycles.SetWord(cycle, (ushort)(backwards ? -3 : 3));
				this.Owner.HueCycles.SetByte((ushort)(cycle + 8), this.Owner.HueCycles.Byte((ushort)(cycle + 7)));
			}
		}

		public void DeactivateHueCycle(ushort cycleNumber)
		{
			using var _ = this.Processor.Journal.Scope($"DeactivateHueCycle({cycleNumber})");

			ushort cycleIndex = (ushort)(cycleNumber - 1);
			this.Processor.RegisterBase.Word = cycleIndex;

			if (cycleIndex < 8)
			{
				this.Owner.HueCycles.SetByte((ushort)(HueCycleRecord(cycleIndex) + 8), 0);
			}
		}

		public void RefreshHueCycling()
		{
			using var _ = this.Processor.Journal.Scope("RefreshHueCycling()");

			ushort savedData = this.Processor.SegmentData.Word;
			this.Processor.SegmentData.Word = 0x35cf;
			ushort cycleOffset = 2;

			while (true)
			{
				ushort cycle = this.Owner.HueCycles.Word(cycleOffset);
				this.Processor.RegisterBase.Word = cycle;
				cycleOffset = (ushort)(cycleOffset + 2);

				if (cycle == 0xffff)
				{
					break;
				}

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
				this.Processor.RegisterAccumulator.LowByte = initialDelay;
				this.Owner.HueCycles.SetByte((ushort)(cycle + 8), initialDelay);

				ushort count = this.Owner.HueCycles.Word((ushort)(cycle + 4));
				this.Processor.RegisterCount.Word = count;

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
				this.Processor.RegisterAccumulator.Word = paletteOffset;

				byte startHue = this.Owner.HueCycles.Byte((ushort)(cycle + 6));
				this.Processor.RegisterAccumulator.LowByte = startHue;

				if (this.Owner.VideoCard.Displays.ContainsKey(0))
				{
					lock (this.Owner.VideoCard.VideoLatch)
					{
						VideoGraphicsBitmap display = this.Owner.VideoCard.Displays[0];
						ushort stagingLocation = (ushort)(source + paletteOffset);
						int total = count / 3;

						for (int index = 0; index < total; index++)
						{
							byte crimson = this.Owner.HueCycles.Byte(stagingLocation++);
							byte emerald = this.Owner.HueCycles.Byte(stagingLocation++);
							byte azure = this.Owner.HueCycles.Byte(stagingLocation++);

							display.AssignHue(index + startHue, VideoGraphicsBitmap.HueFromDigitalLevels(crimson, emerald, azure));
						}
					}
				}
				else
				{
					throw new Exception($"The screen 0 is not allocated");
				}
			}

			this.Processor.SegmentData.Word = savedData;
		}

		public void LoadColorsFromBuffer(ushort steps, ushort swatchAddress)
		{
			using var _ = this.Processor.Journal.Scope($"ImportColorsFromBuffer({steps}, 0x{swatchAddress:x4})");

			this.Processor.SegmentExtra.Word = this.Processor.SegmentData.Word;

			for (int index = 0; index < 0x300; index++)
			{
				this.Processor.Storage.EmitByte(this.Processor.SegmentData.Word, (ushort)(TextBuffer.Address + index),
					this.Processor.Storage.PullByte(this.Processor.SegmentData.Word, (ushort)(swatchAddress + 6 + index)));
			}

			FadeToTargetColors(steps);
		}

		public void FillColors(ushort steps, byte red, byte green, byte blue)
		{
			using var _ = this.Processor.Journal.Scope($"FloodColors({steps}, {red}, {green}, {blue})");

			this.Processor.SegmentExtra.Word = this.Processor.SegmentData.Word;

			for (int index = 0; index < 0x100; index++)
			{
				ushort colour = (ushort)(TextBuffer.Address + (3 * index));

				this.Processor.EmitGlobalByte(colour, red);
				this.Processor.EmitGlobalByte((ushort)(colour + 1), green);
				this.Processor.EmitGlobalByte((ushort)(colour + 2), blue);
			}

			FadeToTargetColors(steps);
		}

		public void FadeColors(ushort steps)
		{
			using var _ = this.Processor.Journal.Scope($"FadeColors({steps})");

			ushort beats = (ushort)(steps * 6);
			ushort speed = this.Processor.PullGlobalWord(HueMixerSpeedAddress);

			if (speed == 0)
			{
				speed = GaugeColorsPerformance();
				this.Processor.EmitGlobalWord(HueMixerSpeedAddress, speed);
			}

			ushort colours = (ushort)(speed / 0xe);

			if (colours > 0xff)
			{
				colours = 0x100;
			}

			this.Processor.EmitGlobalWord(HueMixerCountAddress, colours);

			this.Processor.EmitGlobalWord(HueMixerColorAddress, colours);
			this.Processor.EmitGlobalWord(HueMixerColorAddress, (ushort)-this.Processor.PullGlobalWord(HueMixerColorAddress));

			ushort divisor = (ushort)((((uint)colours * beats) >> 8) - 1);

			if (divisor == 0)
			{
				throw new Exception("Division by zero");
			}

			if (divisor == 1)
			{
				throw new Exception("Division error");
			}

			ushort mixStep = (ushort)(0x10000 / divisor);

			this.Processor.EmitGlobalWord(HueMixerStepRateAddress, mixStep);
			this.Processor.EmitGlobalWord(HueMixerStepAddress, mixStep);

			ColorsFadeStep();

			this.Processor.EmitGlobalWord(HueMixerBeatsAddress, beats);
		}

		public void PullCurrentColors()
		{
			using var _ = this.Processor.Journal.Scope("PullCurrentColors()");

			this.Processor.DisableInterrupts();
			if (this.Owner.VideoCard.Displays.ContainsKey(0))
			{
				lock (this.Owner.VideoCard.VideoLatch)
				{
					VideoGraphicsBitmap display = this.Owner.VideoCard.Displays[0];
					uint stagingLocation = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, 0xbd06);

					for (int index = 0; index < 0x100; index++)
					{
						Color hue = VideoGraphicsBitmap.HueToDigitalLevels(display.ObtainHue(index));

						this.Processor.Storage.EmitByte(stagingLocation++, hue.R);
						this.Processor.Storage.EmitByte(stagingLocation++, hue.G);
						this.Processor.Storage.EmitByte(stagingLocation++, hue.B);
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}

			this.Processor.EnableInterrupts();
		}

		public ushort GaugeColorsPerformance()
		{
			using var _ = this.Processor.Journal.Scope("GaugeColorsPerformance()");

			this.Processor.EmitGlobalWord(HueMixerActiveAddress, 0xffff);

			this.Processor.DisableInterrupts();
			if (this.Owner.VideoCard.Displays.ContainsKey(0))
			{
				lock (this.Owner.VideoCard.VideoLatch)
				{
					VideoGraphicsBitmap display = this.Owner.VideoCard.Displays[0];
					uint stagingLocation = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, 0xbd06);

					for (int index = 0; index < 10; index++)
					{
						byte crimson = this.Processor.Storage.PullByte(stagingLocation++);
						byte emerald = this.Processor.Storage.PullByte(stagingLocation++);
						byte azure = this.Processor.Storage.PullByte(stagingLocation++);

						display.AssignHue(index, VideoGraphicsBitmap.HueFromDigitalLevels(crimson, emerald, azure));
					}
				}
			}
			else
			{
				throw new Exception($"The screen {0} is not allocated");
			}

			this.Processor.EnableInterrupts();

			this.Processor.RegisterAccumulator.Word = 6553 * 10;

			return this.Processor.RegisterAccumulator.Word;
		}

		public void ColorsFadeStep()
		{
			using var _ = this.Processor.Journal.Scope("ColorsFadeStep()");

			MixFadeColours();
		}

		public void ColorsFadeStepInterrupt()
		{
			using var _ = this.Processor.Journal.Scope("ColorsFadeStepInterrupt()");

			MixFadeColours();
		}

		public void AssignColors()
		{
			using var _ = this.Processor.Journal.Scope("AssignColors()");

			if (this.Processor.PullGlobalWord(HueMixerBeatsAddress) != 0)
			{
				this.Processor.RegisterAccumulator.Word = this.Processor.PullGlobalWord(HueMixerColorAddress);
				this.Processor.RegisterBase.Word = this.Processor.PullGlobalWord(HueMixerCountAddress);

				if (this.Owner.VideoCard.Displays.ContainsKey(0))
				{
					lock (this.Owner.VideoCard.VideoLatch)
					{
						VideoGraphicsBitmap display = this.Owner.VideoCard.Displays[0];
						uint stagingLocation = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, 0xc006);

						for (int index = 0; index < this.Processor.RegisterBase.Word; index++)
						{
							byte crimson = this.Processor.Storage.PullByte(stagingLocation++);
							byte emerald = this.Processor.Storage.PullByte(stagingLocation++);
							byte azure = this.Processor.Storage.PullByte(stagingLocation++);

							display.AssignHue(index + this.Processor.RegisterAccumulator.LowByte, VideoGraphicsBitmap.HueFromDigitalLevels(crimson, emerald, azure));
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
		}

		public ushort PathPresent(ushort documentLabelAddress)
		{
			string documentLabel = RuntimeLibrary.ObtainOperatingSystemFileLabel(this.Owner.Processor.PullText(CentralProcessor.AsLinearAddress(this.Owner.Processor.SegmentData.Word, documentLabelAddress)).ToUpper());

			using var _ = this.Processor.Journal.Scope($"PathPresent('{documentLabel}')");

			if (File.Exists($"{this.Owner.Processor.StandardFolder}{documentLabel}"))
			{
				this.Processor.RegisterAccumulator.Word = 0;
			}
			else
			{
				this.Processor.RegisterAccumulator.Word = 0xffff;
			}

			return this.Processor.RegisterAccumulator.Word;
		}

		public void RecoverScreenUnderCursor()
		{
			using var _ = this.Processor.Journal.Scope("RecoverScreenUnderCursor()");

			this.Owner.VideoCard.DrawBufferToScreen();
			ClearPointerLatch();
		}

		public void DrawCursorToScreen(short horizontalLocation, short verticalLocation, ushort picture)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCursorToScreen({horizontalLocation}, {verticalLocation}, {picture})");

			ClearPointerLatch();
		}

		public void ShowScreen(ushort displayIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayScreen({displayIndex})");

			this.Owner.VideoCard.DuplicateScreenToMainScreen(displayIndex);
			ClearPointerLatch();
		}

		public void DrawPictureToScreenAlternate(ushort boxAddress, int horizontalLocation, int verticalLocation, ushort pictureAddress)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPictureToScreenAlternate(0x{boxAddress:x4}, {horizontalLocation}, {verticalLocation}, 0x{pictureAddress:x4})");

			this.Owner.VideoCard.DrawPictureToScreen(boxAddress, horizontalLocation, verticalLocation, pictureAddress);
			ClearPointerLatch();
		}

		public void DrawPictureToScreen(ushort boxAddress, int horizontalLocation, int verticalLocation, ushort pictureAddress)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPictureToScreen(0x{boxAddress:x4}, {horizontalLocation}, {verticalLocation}, 0x{pictureAddress:x4})");

			this.Owner.VideoCard.DrawPictureToScreen(boxAddress, horizontalLocation, verticalLocation, pictureAddress);
		}

		public void PrepareSoundDriver()
		{
			this.Owner.Audio.Prepare();
		}

		public void PerformSound(ushort audio, ushort argument)
		{
			if ((this.Owner.World.SessionPreferenceMarkers & 0x10) != 0)
			{
				this.Owner.Audio.PerformSound(audio, argument);
			}
		}

		public void PerformCityFoundedSound()
		{
			if ((this.Owner.World.SessionPreferenceMarkers & 0x10) != 0)
			{
				this.Owner.Audio.PerformChopping();
			}
		}

		public ushort HaltSound()
		{
			this.Processor.RegisterAccumulator.Word = this.Owner.Audio.Halt();

			return this.Processor.RegisterAccumulator.Word;
		}

		public ushort AdvanceSong()
		{
			this.Processor.RegisterAccumulator.Word = this.Owner.Audio.PerformNextStep();

			return this.Processor.RegisterAccumulator.Word;
		}

		public void RefreshSoundEffects()
		{
			this.Owner.Audio.RefreshVibratoTremolo();
		}

		public ushort ReadSongPosition()
		{
			this.Processor.RegisterAccumulator.Word = this.Owner.Audio.ReadSongPosition();

			return this.Processor.RegisterAccumulator.Word;
		}

		public void FloodBox(ushort boxAddress, int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"FloodBox(0x{boxAddress:x4}, {horizontalLocation}, {verticalLocation}, {breadth}, {altitude}, 0x{style:x4})");

			this.Owner.VideoCard.FloodBox(boxAddress, horizontalLocation, verticalLocation, breadth, altitude, style);
		}

		public void AssignDot(int horizontalLocation, int verticalLocation, ushort style) =>
			AssignDot(new CivilizationRectangle(this.Processor, CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, ActiveBox)).DisplayIndex, horizontalLocation, verticalLocation, style);

		public void AssignDot(ushort displayIndex, int horizontalLocation, int verticalLocation, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"AssignDot({displayIndex}, {horizontalLocation}, {verticalLocation}, 0x{style:x4})");

			this.Owner.VideoCard.AssignDotInBox(ActiveBox, displayIndex, horizontalLocation, verticalLocation, style);
		}

		public ushort PrepareMouse()
		{
			using var _ = this.Processor.Journal.Scope("PrepareMouse()");

			this.Processor.RegisterCount.LowByte = 0;
			this.Processor.RegisterAccumulator.Word = (ushort)(this.Owner.VideoCard.DisplayPointerSite.X >> this.Processor.RegisterCount.LowByte);
			this.Owner.Pointer.ColumnShift = this.Processor.RegisterCount.LowByte;
			this.Owner.Pointer.Horizontal = this.Processor.RegisterAccumulator.Word;
			this.Owner.Pointer.Vertical = (ushort)this.Owner.VideoCard.DisplayPointerSite.Y;
			this.Owner.Pointer.Buttons = this.Owner.VideoCard.DisplayPointerClicks;
			this.Processor.RegisterAccumulator.Word = 0xffff;

			this.Owner.Pointer.DriverActive = this.Processor.RegisterAccumulator.LowByte;

			return this.Processor.RegisterAccumulator.Word;
		}

		public void DeactivateMouse()
		{
			using var _ = this.Processor.Journal.Scope("DeactivateMouse()");

			byte active = this.Owner.Pointer.DriverActive;
			this.Processor.Scratch.LowByte = active;
			this.Owner.Pointer.DriverActive = 0;
			this.Processor.RegisterAccumulator.LowByte = this.Processor.BitOrByte(active, active);

			if (active != 0)
			{
				this.Processor.RegisterAccumulator.Word = 0xffff;
				this.Processor.RegisterBase.Word = 2;
			}
		}

		public void AssignMouseCursorImage(ushort hotHorizontal, ushort hotVertical, ushort picture)
		{
			using var _ = this.Processor.Journal.Scope($"AssignMouseCursorImage({hotHorizontal}, {hotVertical}, {picture})");

			Mouse.HotHorizontal = hotHorizontal;
			Mouse.HotVertical = hotVertical;
			Mouse.Image = picture;
		}

		public void AssignMousePosition(ushort horizontalLocation, ushort verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"AssignMousePosition({horizontalLocation}, {verticalLocation})");

			this.Owner.Pointer.Horizontal = horizontalLocation;
			this.Owner.Pointer.Vertical = verticalLocation;
		}

		public ushort TakeMouseClick()
		{
			this.Processor.PumpMessages();

			ushort clicks = this.Owner.Pointer.Clicks;
			this.Owner.Pointer.Clicks = 0;

			return clicks;
		}

		public void DrawMouseCursor()
		{
			using var _ = this.Processor.Journal.Scope("RenderMouseCursor()");

			if (Mouse.Image != 0 && this.Owner.Pointer.PointerShown == 0)
			{
				DrawCursorToScreen((short)((short)this.Owner.Pointer.Horizontal - (short)Mouse.HotHorizontal),
					(short)((short)this.Owner.Pointer.Vertical - (short)Mouse.HotVertical),
					Mouse.Image);

				this.Owner.Pointer.PointerShown = 0x1;
			}
		}

		public void BlankMouseCursor()
		{
			using var _ = this.Processor.Journal.Scope("BlankMouseCursor()");

			if (Mouse.Image != 0 && this.Owner.Pointer.PointerShown != 0)
			{
				this.Owner.Pointer.PointerShown = 0x0;

				RecoverScreenUnderCursor();
			}
		}

		public void RenewScreen()
		{
			using var _ = this.Processor.Journal.Scope("RenewScreen()");

			(ushort accumulator, ushort data, ushort extraSection) =
				(this.Processor.RegisterAccumulator.Word, this.Processor.RegisterData.Word, this.Processor.SegmentExtra.Word);

			this.Processor.SegmentExtra.Word = 0x1000;
			this.Owner.VideoCard.DrawBufferToScreen();

			(this.Processor.RegisterAccumulator.Word, this.Processor.RegisterData.Word, this.Processor.SegmentExtra.Word) = (accumulator, data, extraSection);
		}

		public void PointerEvent()
		{
			this.Processor.SegmentData.Word = 0x3b01;
			this.Processor.RegisterFrame.Word = this.Processor.RegisterCount.Word;

			byte shift = this.Owner.Pointer.ColumnShift;
			this.Processor.RegisterCount.LowByte = shift;
			this.Processor.RegisterFrame.Word >>= shift;
			this.Processor.RegisterCount.Word = this.Processor.RegisterFrame.Word;

			ushort buttons = this.Processor.RegisterBase.Word;
			this.Owner.Pointer.Clicks = (ushort)(this.Owner.Pointer.Clicks | buttons);
			this.Owner.Pointer.Buttons = buttons;
			this.Owner.Pointer.Horizontal = this.Processor.RegisterCount.Word;
			this.Owner.Pointer.Vertical = this.Processor.RegisterData.Word;

			ushort condition = this.Processor.RegisterAccumulator.Word;
			this.Processor.RegisterFrame.Word = condition;

			if ((condition & 1) != 0 && this.Owner.Pointer.PointerShown != 0)
			{
				this.Processor.RegisterAccumulator.LowByte = 2;
				byte previous = this.Owner.Pointer.RefreshLatch;
				this.Owner.Pointer.RefreshLatch = 2;
				this.Processor.Scratch.LowByte = previous;
				this.Processor.RegisterAccumulator.LowByte = previous;

				if (previous == 0)
				{
					RenewScreen();
					this.Owner.Pointer.RefreshLatch = 0;
				}
			}
		}

		private ushort HueCycleRecord(ushort cycleIndex)
		{
			ushort cycle = this.Owner.HueCycles.Word((ushort)((cycleIndex << 1) + 2));
			this.Processor.RegisterBase.Word = cycle;

			return cycle;
		}

		private void FadeToTargetColors(ushort steps)
		{
			PullCurrentColors();

			FadeColors(steps);

			while (this.Processor.PullGlobalWord(HueMixerBeatsAddress) != 0)
			{
				this.Processor.Idle();
			}
		}

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
			ushort data = this.Processor.RegisterData.Word;
			ushort accumulator;
			ushort sum;
			do
			{
				uint mixed = 0;

				byte from = this.Processor.PullGlobalByte((ushort)(source + 0xbd06));

				if (from != 0)
				{
					uint product = (uint)from * (ushort)~this.Processor.PullGlobalWord(HueMixerStepAddress);

					mixed = product;
					data = (ushort)(product >> 16);
				}

				byte to = this.Processor.PullGlobalByte((ushort)(source + TextBuffer.Address));

				accumulator = to;

				if (to != 0)
				{
					uint product = (uint)to * this.Processor.PullGlobalWord(HueMixerStepAddress);

					mixed += product;
					accumulator = (ushort)product;
					data = (ushort)(product >> 16);
				}

				sum = (ushort)((mixed >> 16) + ((mixed >> 15) & 1));
				accumulator = (ushort)((accumulator & 0xff00) | (sum & 0xff));

				this.Processor.Storage.EmitByte(this.Processor.SegmentData.Word, target++, (byte)sum);

				source++;

				if (source >= 0x300)
				{
					source = 0;
					accumulator = this.Processor.PullGlobalWord(HueMixerStepRateAddress);

					uint mix = (uint)this.Processor.PullGlobalWord(HueMixerStepAddress) + accumulator;

					this.Processor.EmitGlobalWord(HueMixerStepAddress, (ushort)mix);

					if (mix > 0xffff)
					{
						this.Processor.EmitGlobalWord(HueMixerStepAddress, 0xffff);
					}
				}

				bytes--;
			}
			while (bytes != 0);

			this.Processor.RegisterAccumulator.Word = accumulator;
			this.Processor.RegisterBase.Word = sum;
			this.Processor.RegisterCount.Word = 0;
			this.Processor.RegisterData.Word = data;

			this.Processor.EnableInterrupts();
		}

		private void ClearPointerLatch()
		{
			if (this.Owner.Pointer.PointerShown != 0)
			{
				this.Owner.Pointer.RefreshLatch = 0x0;
			}
		}
	}
}
