using System;

namespace Civilization1Remaster.Core.Engine
{
	public readonly record struct GameText(ushort Address, string? Text) : IFormattable
	{
		public static implicit operator ushort(GameText text) => text.Address;

		public static implicit operator GameText(ushort address) => new(address, null);

		public string Read(CPU processor) =>
			this.Text ?? processor.PullText(CPU.AsLinearAddress(processor.SegData.Value16, this.Address));

		public string ToString(string? format, IFormatProvider? formatProvider) => this.Address.ToString(format, formatProvider);

		public override string ToString() => this.Address.ToString();
	}
}
