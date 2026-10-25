namespace Civilization1Remaster.Emulation
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

	/// <summary>A range kept in a byte array (the message buffer, the units, the tables of CIV.EXE)</summary>
	public sealed class ArrayOverlay : IMemoryOverlay
	{
		public ArrayOverlay(byte[] content)
		{
			this.Content = content;
		}

		public byte[] Content { get; }

		public int Length => this.Content.Length;

		public byte Read(int place) => this.Content[place];

		public void Write(int place, byte value) => this.Content[place] = value;
	}
}
