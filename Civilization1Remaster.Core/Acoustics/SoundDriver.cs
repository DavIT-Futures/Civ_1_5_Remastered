using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Acoustics;

namespace Civilization1Remaster.Acoustics
{
	public class SoundDriver : GameModule
	{
		// The original sound driver from the game folder (RSOUND.CVL, ASOUND.CVL or ISOUND.CVL) and the audio or MIDI output.
		// When the driver or the audio output is missing, the functions below keep the old silent behaviour.
		private const int SliceRatio = 44100;

		private ushort stagingPlace = 0;
		private bool handlerFetched = false;
		private SoundDriverHost? handler = null;
		private IDisposable? participant = null;

		public SoundDriver(GameEngine owner)
			: base(owner)
		{
		}

		private SoundDriverHost? Handler
		{
			get
			{
				if (!this.handlerFetched)
				{
					this.handlerFetched = true;

					// The best driver that has an output is used: Roland MT-32 music needs a MIDI output
					foreach (SoundDriverHost handler in SoundDriverHost.ConstructAll(this.Processor.StandardFolder, SliceRatio))
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

		/// <summary>
		/// Initializes the sound driver
		/// </summary>
		/// <returns></returns>
		public ushort Prepare()
		{
			// The driver is initialized when it is loaded
			_ = this.Handler;
			stagingPlace = 0x0;
			return 0x0;
		}

		/// <summary>
		/// Plays the next step of the song, the game calls it from its timer.
		/// The loaded driver is stepped by the audio thread instead, so the tempo is exact.
		/// It returns 0, so the game doesn't change its timer speed.
		/// </summary>
		/// <returns></returns>
		public ushort PerformNextStep()
		{
			stagingPlace++;
			return 0x0;
		}

		/// <summary>
		/// Updates the vibrato and tremolo, the audio thread does it for the loaded driver
		/// </summary>
		public void RefreshVibratoTremolo()
		{
		}

		/// <summary>
		/// Returns the song position, the intro uses it to synchronize pictures with the music
		/// </summary>
		/// <returns></returns>
		public ushort ObtainSongPosition()
		{
			SoundDriverHost? handler = this.Handler;
			return handler != null ? handler.CurrentOffset() : stagingPlace;
		}

		/// <summary>
		/// Starts a song or a sound effect
		/// </summary>
		/// <param name="param1">Song or sound effect number</param>
		/// <param name="param2"></param>
		public void PerformSound(ushort audio, ushort argument)
		{
			this.Handler?.Perform(audio, argument);
			stagingPlace = 0x0;
		}

		/// <summary>
		/// Plays the sound of chopping wood (a new city is founded), together with the current song
		/// </summary>
		public void PerformChopping()
		{
			this.Handler?.PerformChopping();
		}

		/// <summary>
		/// Stops the sound, the game calls it when it exits
		/// </summary>
		/// <returns></returns>
		public ushort Halt()
		{
			this.handler?.Halt();
			return 0x0;
		}
	}
}
