using System;

namespace Civ_1_5_Remastered.Acoustics.Output
{
	public class AudioOutput : IAudioOutput
	{
		private const int SliceRatio = 44100;

		private readonly Func<string> gameFolder;
		private readonly System.Diagnostics.Stopwatch rawMidiStopwatch = new();

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

		private bool IsMuted =>
			string.Equals(this.DriverPreference, "none", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(this.DriverPreference, "off", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(this.DriverPreference, "mute", StringComparison.OrdinalIgnoreCase);

		private bool IsRawMidiSelected =>
			string.Equals(this.DriverPreference, "rawmidi", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(this.DriverPreference, "mid", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(this.DriverPreference, "generalmidi", StringComparison.OrdinalIgnoreCase);

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
			if (this.IsRawMidiSelected)
			{
				_ = this.MidiSequencer;
			}
			else
			{
				_ = this.Handler;
			}

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
			if (this.IsRawMidiSelected)
			{
				if (this.rawMidiStopwatch.IsRunning)
				{
					// 175 ticks per second matches original DOS music tempo (~288 ticks per measure at ~73 BPM)
					return (ushort)(this.rawMidiStopwatch.Elapsed.TotalSeconds * 175.0);
				}

				return stagingPlace;
			}

			SoundDriverHost? activeHandler = this.Handler;
			return activeHandler != null ? activeHandler.CurrentOffset() : stagingPlace;
		}

		public void PerformSound(ushort audio, ushort argument)
		{
			if (this.IsMuted)
			{
				return;
			}

			if (this.IsRawMidiSelected)
			{
				this.rawMidiStopwatch.Restart();
				this.MidiSequencer.PerformSound(audio);
			}
			else
			{
				if (audio is 33 or 36 or 100 or 101)
				{
					this.rawMidiStopwatch.Restart();
					this.Handler?.Halt();
					this.MidiSequencer.PerformSound(audio);
				}
				else
				{
					this.rawMidiStopwatch.Reset();
					this.midiSequencer?.Halt();
					this.Handler?.Perform(audio, argument);
				}
			}

			stagingPlace = 0x0;
		}

		public void PerformChopping()
		{
			if (!this.IsRawMidiSelected)
			{
				this.Handler?.PerformChopping();
			}
		}

		public ushort Halt()
		{
			this.rawMidiStopwatch.Reset();
			this.midiSequencer?.Halt();

			if (!this.IsRawMidiSelected)
			{
				this.handler?.Halt();
			}

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
