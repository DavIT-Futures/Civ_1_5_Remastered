using System;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>The operations of AudioOutput that the other parts of the game use</summary>
	public interface IAudioOutput
	{
		/// <summary>
		/// Initializes the sound driver
		/// </summary>
		/// <returns></returns>
		ushort Prepare();

		/// <summary>
		/// Plays the next step of the song, the game calls it from its timer.
		/// The loaded driver is stepped by the audio thread instead, so the tempo is exact.
		/// It returns 0, so the game doesn't change its timer speed.
		/// </summary>
		/// <returns></returns>
		ushort PerformNextStep();

		/// <summary>
		/// Updates the vibrato and tremolo, the audio thread does it for the loaded driver
		/// </summary>
		void RefreshVibratoTremolo();

		/// <summary>
		/// Returns the song position, the intro uses it to synchronize pictures with the music
		/// </summary>
		/// <returns></returns>
		ushort ReadSongPosition();

		/// <summary>
		/// Starts a song or a sound effect
		/// </summary>
		/// <param name="param1">Song or sound effect number</param>
		/// <param name="param2"></param>
		void PerformSound(ushort audio, ushort argument);

		/// <summary>
		/// Plays the sound of chopping wood (a new city is founded), together with the current song
		/// </summary>
		void PerformChopping();

		/// <summary>
		/// Stops the sound, the game calls it when it exits
		/// </summary>
		/// <returns></returns>
		ushort Halt();
	}
}
