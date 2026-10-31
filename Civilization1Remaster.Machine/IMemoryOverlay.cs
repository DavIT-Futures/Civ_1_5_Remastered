namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// A range of the emulated memory kept in C# by its owner (B6 of PLAN-PO-MIGRACJI.md): every access of the memory to the
	/// range reads and writes the owner (CPUMemory.AttachOverlay), so the code that still uses the addresses keeps working.
	/// </summary>
	public interface IMemoryOverlay
	{
		/// <summary>The length of the range in bytes</summary>
		int Length { get; }

		/// <summary>The byte at the place in the range</summary>
		byte Read(int place);

		/// <summary>Writes the byte at the place in the range</summary>
		void Write(int place, byte value);
	}
}
