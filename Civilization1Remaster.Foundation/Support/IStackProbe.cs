namespace Civilization1Remaster
{
	/// <summary>
	/// Lets the journal read the stack registers of the emulated processor without knowing the processor itself.
	/// </summary>
	public interface IStackProbe
	{
		ushort StackPointer { get; }
		ushort StackSegment { get; }
		ushort DataSegment { get; }
		ushort FramePointer { get; }
	}
}
