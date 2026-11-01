namespace Civ_1_5_Remastered.Machine.Processing
{
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
