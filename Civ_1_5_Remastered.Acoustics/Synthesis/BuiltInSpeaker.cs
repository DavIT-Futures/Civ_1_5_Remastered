using System;

namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public class BuiltInSpeaker
	{
		private const double ClockTimer = 1193182.0;

		private readonly object latch = new object();
		private readonly int sliceRatio;

		private int splitter = 0x10000;
		private bool lowerOctetFollowing = true;
		private int hold;
		private byte speakerGate;
		private double stage;
		private double tier;

		public BuiltInSpeaker(int sliceRatio)
		{
			this.sliceRatio = sliceRatio;
		}

		public byte SpeakerGate
		{
			get
			{
				lock (this.latch)
				{
					return this.speakerGate;
				}
			}
		}

		public void EmitPort(ushort gate, byte amount)
		{
			lock (this.latch)
			{
				switch (gate)
				{
					case 0x42:
						if (this.lowerOctetFollowing)
						{
							this.hold = amount;
							this.lowerOctetFollowing = false;
						}
						else
						{
							this.splitter = this.hold | (amount << 8);
							if (this.splitter == 0)
							{
								this.splitter = 0x10000;
							}

							this.lowerOctetFollowing = true;
						}

						break;
					case 0x43:
						if ((amount & 0xc0) == 0x80)
						{
							this.lowerOctetFollowing = true;
						}

						break;
					case 0x61:
						this.speakerGate = amount;
						break;
				}
			}
		}

		public void Produce(short[] staging, int displacement, int tally)
		{
			lock (this.latch)
			{
				double hertz = ClockTimer / this.splitter;
				bool clockDoor = (this.speakerGate & 1) != 0;
				bool buzzerEnabled = (this.speakerGate & 2) != 0;

				for (int index = 0; index < tally; index++)
				{
					double aim;

					if (clockDoor && buzzerEnabled && hertz < this.sliceRatio / 2)
					{
						this.stage += hertz / this.sliceRatio;
						this.stage -= Math.Floor(this.stage);
						aim = this.stage < 0.5 ? 1.0 : -1.0;
					}
					else
					{
						aim = 0.0;
					}

					this.tier += (aim - this.tier) * 0.35;
					int slice = staging[displacement + index] + (int)(this.tier * 6000.0);
					staging[displacement + index] = (short)Math.Clamp(slice, short.MinValue, short.MaxValue);
				}
			}
		}
	}
}
