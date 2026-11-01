using System.Drawing;
using Civilization1Remaster.Foundation.Support;
using Civilization1Remaster.Machine;

namespace Civilization1Remaster.Tests.MachineAndImaging
{
	public class RuntimeLibraryKeysTests
	{
		private sealed class KeyHost : IMachineHost
		{
			public Point PointerSite => Point.Empty;
			public ushort PointerClicks => 0;
			public Queue<int> InputTokens { get; } = new Queue<int>();
			public object InputLatch { get; } = new object();
			public CallJournal IrqJournal { get; } = new CallJournal((TextWriter?)null);

			public void OnPointerEvent()
			{
			}

			public void OnClockInterrupt()
			{
			}

			public void ReadKeyBlocking()
			{
			}

			public bool ReleasePicture(ushort section) => false;
		}

		private static (RuntimeLibrary Library, CPU Processor, KeyHost Host) Library()
		{
			KeyHost host = new KeyHost();
			CPU processor = new CPU(host, new CallJournal((TextWriter?)null));
			return (new RuntimeLibrary(processor, host), processor, host);
		}

		[Theory]
		[InlineData(0x0000)]
		[InlineData(0x1234)]
		[InlineData(0xffff)]
		public void KeyAvailableAnswersAlsoWhenAKeyWaits(int leftInAccumulator)
		{
			var (library, processor, host) = Library();

			processor.RegAcc.Value16 = (ushort)leftInAccumulator;
			Assert.Equal(0, library.KeyAvailable());

			host.InputTokens.Enqueue(0x4800);
			processor.RegAcc.Value16 = (ushort)leftInAccumulator;
			Assert.Equal(-1, library.KeyAvailable());
			Assert.Equal(0xffff, processor.RegAcc.Value16);
		}

		[Fact]
		public void ReadKeyGivesTheKeysInOrderIncludingTheExtendedOnes()
		{
			var (library, processor, host) = Library();
			host.InputTokens.Enqueue(0x4800);
			host.InputTokens.Enqueue('f');
			host.InputTokens.Enqueue(0x5000);

			Assert.Equal((short)0x4800, library.ReadKey());
			Assert.Equal((short)'f', library.ReadKey());
			Assert.Equal((short)0x5000, library.ReadKey());
			Assert.Equal(0x5000, processor.RegAcc.Value16);
			Assert.Equal(0, library.KeyAvailable());
		}
	}
}
