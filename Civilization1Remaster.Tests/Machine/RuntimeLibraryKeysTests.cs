using Civilization1Remaster.Foundation.Support;
using Civilization1Remaster.Machine.Library;
using Civilization1Remaster.Machine.Processing;

namespace Civilization1Remaster.Tests.Machine
{
	public partial class RuntimeLibraryKeysTests
	{
		[Theory]
		[InlineData(0x0000)]
		[InlineData(0x1234)]
		[InlineData(0xffff)]
		public void KeyAvailableAnswersAlsoWhenSomeKeyWaits(int leftInAccumulator)
		{
			var (library, processor, host) = Library();

			processor.RegisterAccumulator.Word = (ushort)leftInAccumulator;
			Assert.Equal(0, library.KeyAvailable());

			host.InputTokens.Enqueue(0x4800);
			processor.RegisterAccumulator.Word = (ushort)leftInAccumulator;
			Assert.Equal(-1, library.KeyAvailable());
			Assert.Equal(0xffff, processor.RegisterAccumulator.Word);
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
			Assert.Equal(0x5000, processor.RegisterAccumulator.Word);
			Assert.Equal(0, library.KeyAvailable());
		}

		private static (RuntimeLibrary Library, CentralProcessor Processor, KeyHost Host) Library()
		{
			KeyHost host = new KeyHost();
			CentralProcessor processor = new CentralProcessor(host, new CallJournal((TextWriter?)null));
			return (new RuntimeLibrary(processor, host), processor, host);
		}
	}
}
