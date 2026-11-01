namespace Civilization1Remaster.Core.Engine
{
	public sealed class FrameWords
	{
		private readonly CPU processor;
		private readonly int size;
		private readonly ushort[] words;
		private readonly int stackFrom;
		private readonly int stackTo;

		public FrameWords(CPU processor, int size, int stackFrom = 0, int stackTo = 0)
		{
			this.processor = processor;
			this.size = size;
			this.stackFrom = stackFrom;
			this.stackTo = stackTo;
			this.words = new ushort[(size / 2) + 1];
		}

		public ushort Frame(int distance)
		{
			distance = (short)(ushort)distance;

			return Within(distance) ? this.words[distance / 2] : this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance));
		}

		public void SetFrame(int distance, ushort value)
		{
			distance = (short)(ushort)distance;

			if (Within(distance))
			{
				this.words[distance / 2] = value;
			}
			else
			{
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance), value);
			}
		}

		public byte FrameByte(int distance)
		{
			distance = (short)(ushort)distance;
			int word = (distance + 1) & ~1;

			if (!Within(word))
			{
				return this.processor.PullUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance));
			}

			return (distance & 1) == 0 ? (byte)this.words[word / 2] : (byte)(this.words[word / 2] >> 8);
		}

		public void SetFrameByte(int distance, byte value)
		{
			distance = (short)(ushort)distance;
			int word = (distance + 1) & ~1;

			if (!Within(word))
			{
				this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance), value);
				return;
			}

			ushort old = this.words[word / 2];
			this.words[word / 2] = (distance & 1) == 0 ? (ushort)((old & 0xff00) | value) : (ushort)((old & 0xff) | (value << 8));
		}

		private bool Within(int distance) => distance > 0 && distance <= this.size && (distance & 1) == 0 &&
			(distance < this.stackFrom || distance > this.stackTo);
	}
}
