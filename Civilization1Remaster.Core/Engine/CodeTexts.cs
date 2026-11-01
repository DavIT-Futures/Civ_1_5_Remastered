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

			for (int index = 0; index <= text.Length; index++)
			{
				Poke(place + index, index < text.Length ? (byte)text[index] : (byte)0);
			}

			return (ushort)(Address + place);
		}

		public ushort Set(int code, ushort textAddress) =>
			Set(code, this.game.Processor.PullText(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, textAddress)));

		public void Attach()
		{
			this.game.Processor.Storage.AttachOverlay(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, Address), this.store);
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
