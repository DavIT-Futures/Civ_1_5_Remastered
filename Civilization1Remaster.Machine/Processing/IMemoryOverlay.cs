namespace Civilization1Remaster.Machine.Processing
{
	public interface IMemoryOverlay
	{
		int Length { get; }

		byte Read(int place);

		void Write(int place, byte value);
	}
}
