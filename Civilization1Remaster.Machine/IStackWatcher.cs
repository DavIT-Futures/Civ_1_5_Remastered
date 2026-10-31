namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// Test seam: sees what the game does with its stack segment. The translated functions read some of their locals before
	/// they write them, so they get what an earlier function left at the same depth of the stack; the tests use this to
	/// check that the rewritten code leaves the same words there (Tests/Dual/LeftoverWatcher.cs).
	/// </summary>
	public interface IStackWatcher
	{
		/// <summary>A function opened its frame: the frame pointer and the size of its locals below it</summary>
		void OpenFrame(ushort framePointer, ushort localsSize);

		/// <summary>The function of this frame pointer closes its frame</summary>
		void CloseFrame(ushort framePointer);

		/// <summary>A read of the stack segment through the CPU</summary>
		void Read(ushort displacement, int size);

		/// <summary>A write of the stack segment through the CPU, a push included</summary>
		void Write(ushort displacement, int size);
	}
}
