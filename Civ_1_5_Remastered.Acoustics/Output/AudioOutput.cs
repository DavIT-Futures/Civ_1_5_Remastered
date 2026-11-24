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
		private SoundFontMusic? soundFontMusic;

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

								if (OperatingSystem.IsWindows() && SoundDriverHost.PlaysSoundFont(this.DriverPreference))
								{
									this.soundFontMusic ??= SoundFontMusic.Open(this.gameFolder(), SliceRatio);
								}

								this.participant = WaveOutPlayer.Launch(candidateHandler, this.soundFontMusic);
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
			if (SoundDriverHost.PlaysSoundFont(this.DriverPreference) && PlaySoundFontSong(audio, argument))
			{
				// SoundFont music mode: the song plays from its MIDI file through the SoundFont bank in stereo.
				stagingPlace = 0x0;
				return;
			}

			if (audio is 100 or 101)
			{
				this.Handler?.Halt();
				if (this.participant is MidiOutPlayer midiOut)
				{
					midiOut.PauseDevice();
				}

				this.MidiSequencer.PerformSound(audio);
			}
			else if (SoundDriverHost.PlaysMidiFiles(this.DriverPreference) && GeneralMidiPlayer.SongFileName(audio) != null &&
				this.MidiSequencer.PerformSound(audio))
			{
				// General MIDI music mode: the song comes from its .mid file, the driver keeps playing the same song
				// without sound, so the song position the game waits on goes on as with the original music.
				SoundDriverHost? silentHandler = this.Handler;
				if (silentHandler != null)
				{
					silentHandler.Silenced = true;
					silentHandler.Perform(audio, argument);
				}
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

				SoundDriverHost? activeHandler = this.Handler;
				if (activeHandler != null)
				{
					activeHandler.Silenced = false;
					activeHandler.Perform(audio, argument);
				}
			}

			stagingPlace = 0x0;
		}

		public void PerformChopping()
		{
			this.Handler?.PerformChopping();
		}

		public ushort Halt()
		{
			this.soundFontMusic?.Halt();
			this.midiSequencer?.Halt();
			if (this.participant is MidiOutPlayer midiOut)
			{
				midiOut.ResumeDevice();
			}

			if (this.handler != null)
			{
				this.handler.Silenced = false;
				this.handler.Halt();
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
			this.soundFontMusic = null;

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

		private bool PlaySoundFontSong(ushort audio, ushort argument)
		{
			SoundDriverHost? activeHandler = this.Handler;
			SoundFontMusic? music = this.soundFontMusic;
			string? song = music != null ? this.MidiSequencer.LocateSong(audio) : null;

			if (music == null || song == null)
			{
				music?.Halt();
				return false;
			}

			try
			{
				music.Play(song);
			}
			catch (Exception failure)
			{
				Console.WriteLine($"Song {song} could not be played: {failure.Message}");
				music.Halt();
				return false;
			}

			if (activeHandler != null)
			{
				if (audio is 100 or 101)
				{
					// The anthems of the DLC have no song in the original driver.
					activeHandler.Silenced = false;
					activeHandler.Halt();
				}
				else
				{
					// The driver plays the same song without sound, so the song position the game waits on moves on.
					activeHandler.Silenced = true;
					activeHandler.Perform(audio, argument);
				}
			}

			return true;
		}
	}
}
