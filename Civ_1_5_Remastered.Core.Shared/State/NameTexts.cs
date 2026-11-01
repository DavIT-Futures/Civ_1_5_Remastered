namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed class NameTexts
	{
		public const ushort Address = 0x0111;

		public const int Size = 0x156;

		private readonly IGameEngine game;
		private readonly byte[] store = new byte[Size];

		private bool attached;

		public NameTexts(IGameEngine game)
		{
			this.game = game;
		}

		public ushort Set(ushort nameAddress, string text)
		{
			for (int index = 0; index <= text.Length; index++)
			{
				Poke(nameAddress - Address + index, index < text.Length ? (byte)text[index] : (byte)0);
			}

			return nameAddress;
		}

		public ushort Set(ushort nameAddress, ushort textAddress) =>
			Set(nameAddress, this.game.Processor.PullText(CentralProcessor.AsLinearAddress(this.game.Processor.SegmentData.Word, textAddress)));

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
