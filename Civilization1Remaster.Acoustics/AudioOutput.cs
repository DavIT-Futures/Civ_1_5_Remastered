using System;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>The music and the sound effects of the game, played by its original sound driver</summary>
	public class AudioOutput : IAudioOutput
	{
		// The original sound driver from the game folder (RSOUND.CVL, ASOUND.CVL or ISOUND.CVL) and the audio or MIDI output.
		// When the driver or the audio output is missing, the functions below keep the old silent behaviour.
		private const int SliceRatio = 44100;

		private readonly Func<string> gameFolder;

		private ushort stagingPlace = 0;
		private bool handlerFetched = false;
		private SoundDriverHost? handler;
		private IDisposable? participant;

		/// <summary>The sound player of the game in the folder (read when the first sound plays)</summary>
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

					// The best driver that has an output is used: Roland MT-32 music needs a MIDI output
					foreach (SoundDriverHost handler in SoundDriverHost.ConstructAll(this.gameFolder(), SliceRatio))
					{
						try
						{
							if (handler.HasSynth)
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

		/// <inheritdoc/>
		public ushort Prepare()
		{
			// The driver is initialized when it is loaded
			_ = this.Handler;
			stagingPlace = 0x0;
			return 0x0;
		}

		/// <inheritdoc/>
		public ushort PerformNextStep()
		{
			stagingPlace++;
			return 0x0;
		}

		/// <inheritdoc/>
		public void RefreshVibratoTremolo()
		{
		}

		/// <inheritdoc/>
		public ushort ReadSongPosition()
		{
			SoundDriverHost? handler = this.Handler;
			return handler != null ? handler.CurrentOffset() : stagingPlace;
		}

		/// <inheritdoc/>
		public void PerformSound(ushort audio, ushort argument)
		{
			this.Handler?.Perform(audio, argument);
			stagingPlace = 0x0;
		}

		/// <inheritdoc/>
		public void PerformChopping()
		{
			this.Handler?.PerformChopping();
		}

		/// <inheritdoc/>
		public ushort Halt()
		{
			this.handler?.Halt();
			return 0x0;
		}
	}
}
