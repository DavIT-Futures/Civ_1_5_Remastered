namespace Civilization1Remaster.Core.Engine
{
	public sealed class GlobalWord : IMemoryOverlay
	{
		public GlobalWord(ushort address)
		{
			this.Address = address;
		}

		public ushort Address { get; }

		public ushort Value { get; set; }

		public int Length => 2;

		public byte Read(int place) => place == 0 ? (byte)this.Value : (byte)(this.Value >> 8);

		public void Write(int place, byte value) =>
			this.Value = place == 0 ? (ushort)((this.Value & 0xff00) | value) : (ushort)((this.Value & 0xff) | (value << 8));
	}
}
