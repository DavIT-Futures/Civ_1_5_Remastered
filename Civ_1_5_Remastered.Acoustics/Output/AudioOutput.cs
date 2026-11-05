using System;

namespace Civ_1_5_Remastered.Acoustics.Output
{
	public class AudioOutput : IAudioOutput
	{
		private const int SliceRatio = 44100;

		private readonly Func<string> gameFolder;

		private string? driverPreference;
		private ushort stagingPlace = 0;
		private bool handlerFetched = false;
		private SoundDriverHost? handler;
		private IDisposable? participant;
		private GeneralMidiPlayer? midiSequencer;

		public AudioOutput(Func<string> gameFolder)
		{
			this.gameFolder = gameFolder;
		}

		public string? DriverPreference
		{
			get
			{
				return this.driverPreference;
			}

			set
			{
				if (!string.Equals(this.driverPreference, value, StringComparison.OrdinalIgnoreCase))
				{
					this.driverPreference = value;
					SwitchDriver();
				}
			}
		}

		private GeneralMidiPlayer MidiSequencer => this.midiSequencer ??= new GeneralMidiPlayer(this.gameFolder);

		private SoundDriverHost? Handler
		{
			get
			{
				if (!this.handlerFetched)
				{
					this.handlerFetched = true;

					foreach (SoundDriverHost candidateHandler in SoundDriverHost.ConstructAll(this.gameFolder(), SliceRatio, this.DriverPreference))
					{
						try
						{
							if (candidateHandler.HasSynthesizer)
							{
								this.participant = MidiOutPlayer.Launch(candidateHandler);
							}
							else
							{
								candidateHandler.Prepare();
								this.participant = WaveOutPlayer.Launch(candidateHandler);
							}
						}
						catch (Exception extra)
						{
							Console.WriteLine($"Sound driver {candidateHandler.Label} could not be started: {extra.Message}");
							this.participant = null;
						}

						if (this.participant != null)
						{
							this.handler = candidateHandler;
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
			SoundDriverHost? activeHandler = this.Handler;
			return activeHandler != null ? activeHandler.CurrentOffset() : stagingPlace;
		}

		public void PerformSound(ushort audio, ushort argument)
		{
			if (audio == 33)
			{
				if (this.participant is MidiOutPlayer midiOut)
				{
					midiOut.PauseDevice();
				}

				this.MidiSequencer.PerformSound(audio);
			}
			else
			{
				if (this.midiSequencer != null && this.midiSequencer.IsPlaying)
				{
					this.midiSequencer.Halt();
					if (this.participant is MidiOutPlayer midiOut)
					{
						midiOut.ResumeDevice();
					}
				}
				else if (audio <= 1)
				{
					this.midiSequencer?.Halt();
					if (this.participant is MidiOutPlayer midiOut)
					{
						midiOut.ResumeDevice();
					}
				}

				this.Handler?.Perform(audio, argument);
			}

			stagingPlace = 0x0;
		}

		public void PerformChopping()
		{
			this.Handler?.PerformChopping();
		}

		public ushort Halt()
		{
			this.midiSequencer?.Halt();
			if (this.participant is MidiOutPlayer midiOut)
			{
				midiOut.ResumeDevice();
			}

			this.handler?.Halt();

			return 0x0;
		}

		private void SwitchDriver()
		{
			try
			{
				Halt();
			}
			catch
			{
			}

			if (this.participant != null)
			{
				try
				{
					this.participant.Dispose();
				}
				catch
				{
				}

				this.participant = null;
			}

			this.handler = null;
			this.handlerFetched = false;

			if (this.midiSequencer != null)
			{
				try
				{
					this.midiSequencer.Dispose();
				}
				catch
				{
				}

				this.midiSequencer = null;
			}
		}
	}
}
