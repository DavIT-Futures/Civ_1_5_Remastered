using System.Collections.Generic;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// A global word variable of the game kept in C# (B6.4): the code reads and writes Value, and the memory reaches it
	/// over its address (CPUMemory.AttachOverlay), low byte first, like the data segment of the original.
	/// </summary>
	public sealed class GlobalWord : IMemoryOverlay
	{
		public GlobalWord(ushort address)
		{
			this.Address = address;
		}

		/// <summary>The address of the variable in the data segment</summary>
		public ushort Address { get; }

		/// <summary>The value of the variable</summary>
		public ushort Value { get; set; }

		public int Length => 2;

		public byte Read(int place) => place == 0 ? (byte)this.Value : (byte)(this.Value >> 8);

		public void Write(int place, byte value) =>
			this.Value = place == 0 ? (ushort)((this.Value & 0xff00) | value) : (ushort)((this.Value & 0xff) | (value << 8));
	}
}
