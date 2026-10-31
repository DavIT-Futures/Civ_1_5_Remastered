using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// A text of the data segment of CIV.EXE: its address (still written in the journal, so the traces stay those of the
	/// original) and, for the texts of GameTexts, the text itself as a C# string. A bare address (an ushort, e.g. a text built
	/// in the memory) has no string: Read takes it from the memory at the moment of use, as the original did. Formats like its
	/// address ("0x{text:x4}") and converts to it, so the code that still works with addresses takes it unchanged.
	/// </summary>
	public readonly record struct GameText(ushort Address, string? Text) : IFormattable
	{
		public static implicit operator ushort(GameText text) => text.Address;

		public static implicit operator GameText(ushort address) => new(address, null);

		/// <summary>The text: the C# string, or the bytes at the address of the data segment when there is none</summary>
		public string Read(CPU processor) =>
			this.Text ?? processor.PullText(CPU.AsLinearAddress(processor.SegData.Value16, this.Address));

		public string ToString(string? format, IFormatProvider? formatProvider) => this.Address.ToString(format, formatProvider);

		public override string ToString() => this.Address.ToString();
	}
}
