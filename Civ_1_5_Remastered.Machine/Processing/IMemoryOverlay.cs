namespace Civ_1_5_Remastered.Machine.Processing
{
	public interface IMemoryOverlay
	{
		int Length { get; }

		byte Read(int place);

		void Write(int place, byte value);
	}
}
