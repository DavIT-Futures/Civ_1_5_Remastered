namespace Civ_1_5_Remastered.Acoustics.Output
{
	public interface IAudioOutput
	{
		ushort Prepare();

		ushort PerformNextStep();

		void RefreshVibratoTremolo();

		ushort ReadSongPosition();

		void PerformSound(ushort audio, ushort argument);

		void PerformChopping();

		ushort Halt();
	}
}
