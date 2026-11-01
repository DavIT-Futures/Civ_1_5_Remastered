using System;

namespace Civilization1Remaster.Acoustics
{
	public class AudioOutput : IAudioOutput
	{
		private const int SliceRatio = 44100;

		private readonly Func<string> gameFolder;

		private ushort stagingPlace = 0;
		private bool handlerFetched = false;
		private SoundDriverHost? handler;
		private IDisposable? participant;

		public AudioOutput(Func<string> gameFolder)
		{
			this.gameFolder = gameFolder;
		}

		private SoundDriverHost? Handler
		{
			get
			{
				if (!this.handlerFetched)
				{
					this.handlerFetched = true;

					foreach (SoundDriverHost handler in SoundDriverHost.ConstructAll(this.gameFolder(), SliceRatio))
					{
						try
						{
							if (handler.HasSynthesizer)
							{
								this.participant = MidiOutPlayer.Launch(handler);
							}
							else
							{
								handler.Prepare();
								this.participant = WaveOutPlayer.Launch(handler);
							}
						}
						catch (Exception extra)
						{
							Console.WriteLine($"Sound driver {handler.Label} could not be started: {extra.Message}");
							this.participant = null;
						}

						if (this.participant != null)
						{
							this.handler = handler;
							break;
						}
					}
				}

				return this.handler;
			}
		}

		public ushort Prepare()
		{
			_ = this.Handler;
			stagingPlace = 0x0;
			return 0x0;
		}

		public ushort PerformNextStep()
		{
			stagingPlace++;
			return 0x0;
		}

		public void RefreshVibratoTremolo()
		{
		}

		public ushort ReadSongPosition()
		{
			SoundDriverHost? handler = this.Handler;
			return handler != null ? handler.CurrentOffset() : stagingPlace;
		}

		public void PerformSound(ushort audio, ushort argument)
		{
			this.Handler?.Perform(audio, argument);
			stagingPlace = 0x0;
		}

		public void PerformChopping()
		{
			this.Handler?.PerformChopping();
		}

		public ushort Halt()
		{
			this.handler?.Halt();
			return 0x0;
		}
	}
}
