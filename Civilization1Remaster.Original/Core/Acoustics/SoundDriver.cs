using System;
using Civilization1Original.Emulation;
using Civilization1Original.Acoustics;

namespace Civilization1Original.Acoustics
{
	public class SoundDriver : GameModule
	{
		private ushort stagingPlace = 0;

		// The original sound driver from the game folder (RSOUND.CVL, ASOUND.CVL or ISOUND.CVL) and the audio or MIDI output.
		// When the driver or the audio output is missing, the functions below keep the old silent behaviour.
		private const int SliceRatio = 44100;
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
					foreach (SoundDriverHost handler in SoundDriverHost.ConstructAll(this.processor.StandardFolder, SliceRatio))
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
			// function body
			// The driver is initialized when it is loaded
			_ = this.Handler;
			stagingPlace = 0x0;
			this.processor.RegAcc.Value16 = 0x0;

			// Far return

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Plays the next step of the song, the game calls it from its timer.
		/// The loaded driver is stepped by the audio thread instead, so the tempo is exact.
		/// It returns 0, so the game doesn't change its timer speed.
		/// </summary>
		/// <returns></returns>
		public ushort PerformNextStep()
		{
			// function body
			stagingPlace++;
			this.processor.RegAcc.Value16 = 0x0;
			// Far return

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Updates the vibrato and tremolo, the audio thread does it for the loaded driver
		/// </summary>
		public void RefreshVibratoTremolo()
		{
			// function body
			// Far return
		}

		/// <summary>
		/// Returns the song position, the intro uses it to synchronize pictures with the music
		/// </summary>
		/// <returns></returns>
		public ushort ObtainSongPosition()
		{
			// function body
			SoundDriverHost? handler = this.Handler;
			this.processor.RegAcc.Value16 = handler != null ? handler.CurrentOffset() : stagingPlace;
			// Far return

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Starts a song or a sound effect
		/// </summary>
		/// <param name="param1">Song or sound effect number</param>
		/// <param name="param2"></param>
		public void PerformSound(ushort arg1, ushort arg2)
		{
			// function body
			this.Handler?.Perform(arg1, arg2);
			stagingPlace = 0x0;
			// Far return
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
			// function body
			this.handler?.Halt();
			this.processor.RegAcc.Value16 = 0x0;
			// Far return

			return this.processor.RegAcc.Value16;
		}
	}
}
