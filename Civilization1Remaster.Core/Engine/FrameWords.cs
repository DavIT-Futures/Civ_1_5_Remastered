
namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The words of a stack frame of the original kept in C# (B3, 2026-10-26): the locals and the arrays of a function that
	/// its helpers share, with the layout and the overlaps of the original (an array read at index -1 gets the local below
	/// it). A word is named by its distance below BP (2 - the size of the frame, 16-bit as in the original: an array index
	/// may be negative); 0 at the start like the zeroed frame of the remaster. A word out of the frame still goes to the
	/// stack.
	/// </summary>
	public sealed class FrameWords
	{
		private readonly CPU processor;
		private readonly int size;
		private readonly ushort[] words;
		private readonly int stackFrom;
		private readonly int stackTo;

		/// <summary>The words at the distances stackFrom - stackTo stay on the stack (a buffer passed by its address)</summary>
		public FrameWords(CPU processor, int size, int stackFrom = 0, int stackTo = 0)
		{
			this.processor = processor;
			this.size = size;
			this.stackFrom = stackFrom;
			this.stackTo = stackTo;
			this.words = new ushort[(size / 2) + 1];
		}

		/// <summary>The word at BP - distance</summary>
		public ushort Frame(int distance)
		{
			distance = (short)(ushort)distance;

			return Within(distance) ? this.words[distance / 2] :
				this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance));
		}

		/// <summary>Writes the word at BP - distance</summary>
		public void SetFrame(int distance, ushort value)
		{
			distance = (short)(ushort)distance;

			if (Within(distance))
				this.words[distance / 2] = value;
			else
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance), value);
		}

		/// <summary>The byte at BP - distance (the low byte of the word at an even distance, the high byte of the word below an odd one)</summary>
		public byte FrameByte(int distance)
		{
			distance = (short)(ushort)distance;
			int word = (distance + 1) & ~1;

			if (!Within(word))
				return this.processor.PullUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 - distance));

			return (distance & 1) == 0 ? (byte)this.words[word / 2] : (byte)(this.words[word / 2] >> 8);
		}

		/// <summary>Writes the byte at BP - distance (see FrameByte)</summary>
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
