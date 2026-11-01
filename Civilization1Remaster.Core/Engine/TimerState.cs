namespace Civilization1Remaster.Core.Engine
{
	public sealed class TimerState
	{
		public const ushort Begin = 0x40;

		public const int Size = 0x30;

		public const ushort BeatsAddress = 0x5c;

		private readonly CentralProcessor processor;

		public TimerState(CentralProcessor processor)
		{
			this.processor = processor;
		}

		public byte[] Content { get; } = new byte[Size];

		public ushort Beats => this.processor.PullGlobalWord(BeatsAddress);
	}
}
