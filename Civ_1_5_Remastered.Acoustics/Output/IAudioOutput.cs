namespace Civ_1_5_Remastered.Acoustics.Output
{
	public interface IAudioOutput
	{
		string? DriverPreference { get; set; }

		ushort Prepare();

		ushort PerformNextStep();

		void RefreshVibratoTremolo();

		ushort ReadSongPosition();

		void PerformSound(ushort audio, ushort argument);

		void PerformChopping();

		ushort Halt();
	}
}
