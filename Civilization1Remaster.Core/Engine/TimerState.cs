using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The variables of the timer and sound driver at 0x40 - 0x6f of the data segment, kept in C# (B6.7, part 1): the timer
	/// interrupt (SystemServices.ClockInterrupt) and the waits of the game work on them. The game code reads the beat counter
	/// through the CPU (Beats): under the fixed clock of the tests every read of these variables is a poll of the timer
	/// (CPU.PollTimer), and the interrupts come after so many polls, so the reads stay where and as many as they were.
	/// </summary>
	public sealed class TimerState
	{
		/// <summary>The first address of the block</summary>
		public const ushort Begin = 0x40;

		/// <summary>The size of the block in bytes (to 0x6f)</summary>
		public const int Size = 0x30;

		/// <summary>The address of the beat counter</summary>
		public const ushort BeatsAddress = 0x5c;

		private readonly CPU processor;

		public TimerState(CPU processor)
		{
			this.processor = processor;
		}

		/// <summary>The bytes of the block</summary>
		public byte[] Content { get; } = new byte[Size];

		/// <summary>
		/// The beats counted by the timer since the last reset (0x5c, OnClockBeat adds one), what the waits of the game compare;
		/// a read is a poll of the timer (see the class)
		/// </summary>
		public ushort Beats => this.processor.PullGlobalWord(BeatsAddress);
	}
}
