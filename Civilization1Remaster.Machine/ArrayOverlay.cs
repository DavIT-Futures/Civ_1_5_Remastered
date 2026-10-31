namespace Civilization1Remaster.Emulation
{
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
