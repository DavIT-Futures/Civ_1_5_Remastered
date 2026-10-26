using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
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

		public FrameWords(CPU processor, int size)
		{
			this.processor = processor;
			this.size = size;
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

		private bool Within(int distance) => distance > 0 && distance <= this.size && (distance & 1) == 0;
	}
}
