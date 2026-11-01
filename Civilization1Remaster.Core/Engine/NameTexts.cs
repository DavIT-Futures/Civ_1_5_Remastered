namespace Civilization1Remaster.Core.Engine
{
	public sealed class NameTexts
	{
		public const ushort Address = 0x0111;

		public const int Size = 0x156;

		private readonly GameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public NameTexts(GameEngine game)
		{
			this.game = game;
		}

		public ushort Set(ushort nameAddr, string text)
		{
			for (int idx = 0; idx <= text.Length; idx++)
				Poke(nameAddr - Address + idx, idx < text.Length ? (byte)text[idx] : (byte)0);

			return nameAddr;
		}

		public ushort Set(ushort nameAddr, ushort textAddr) =>
			Set(nameAddr, this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, textAddr)));

		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);
			this.attached = true;
		}

		private void Poke(int index, byte value)
		{
			if (this.attached && index >= 0 && index < Size)
				this.store[index] = value;
			else
				this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
		}
	}
}
