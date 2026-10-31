namespace Civilization1Remaster.Foundation.Support
{
	/// <summary>
	/// Lets the journal read the stack registers of the emulated processor without knowing the processor itself.
	/// </summary>
	public interface IStackProbe
	{
		ushort StackPointer { get; }
	}
}
