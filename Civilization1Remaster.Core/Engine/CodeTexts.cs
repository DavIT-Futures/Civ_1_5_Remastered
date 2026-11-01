namespace Civilization1Remaster.Core.Engine
{
	public sealed class CodeTexts
	{
		public const ushort Address = 0x3037;

		public const int Stride = 0x16;

		public const int Size = 5 * Stride;

		private readonly GameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public CodeTexts(GameEngine game)
		{
			this.game = game;
		}

		public ushort Set(int code, string text)
		{
			int place = code * Stride;

			for (int idx = 0; idx <= text.Length; idx++)
			{
				Poke(place + idx, idx < text.Length ? (byte)text[idx] : (byte)0);
			}

			return (ushort)(Address + place);
		}

		public ushort Set(int code, ushort textAddr) =>
			Set(code, this.game.Processor.PullText(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, textAddr)));

		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CPU.AsLinearAddress(this.game.Processor.SegData.Value16, Address), this.store);
			this.attached = true;
		}

		private void Poke(int index, byte value)
		{
			if (this.attached && index >= 0 && index < Size)
			{
				this.store[index] = value;
			}
			else
			{
				this.game.Processor.EmitGlobalByte((ushort)(Address + index), value);
			}
		}
	}
}
