using Civilization1Remaster.Foundation.Support;

namespace Civilization1Remaster.Tests.Foundation
{
	public class LogWrapperTests
	{
		private sealed class FakeStack : IStackProbe
		{
			public ushort StackPointer { get; set; } = 0x100;
		}

		private static (LogWrapper Journal, StringWriter Text, FakeStack Stack) Written()
		{
			StringWriter text = new StringWriter();
			FakeStack stack = new FakeStack();
			LogWrapper journal = new LogWrapper(text) { Probe = stack };
			return (journal, text, stack);
		}

		private static string[] Lines(StringWriter text) => text.ToString().Split(text.NewLine).SkipLast(1).ToArray();

		[Fact]
		public void ScopesAreWrittenAsIndentedBlocks()
		{
			var (journal, text, _) = Written();

			using (journal.Scope("Outer(1)"))
			{
				journal.EmitLine("// inside");

				using (journal.Scope("Inner()"))
				{
				}
			}

			Assert.Equal(new[] { "Outer(1)", "{", "\t// inside", "\tInner()", "\t{", "\t}", "}" }, Lines(text));
		}

		[Fact]
		public void InterpolatedScopeIsFormatted()
		{
			var (journal, text, _) = Written();
			int place = 0x2a;

			using (journal.Scope($"Find({place}, 0x{place:x4})"))
			{
			}

			Assert.Equal("Find(42, 0x002a)", Lines(text)[0]);
		}

		[Fact]
		public void ScopeClosesWhenAnExceptionLeavesIt()
		{
			var (journal, text, _) = Written();

			Action failing = () =>
			{
				using var _ = journal.Scope("Failing()");
				throw new InvalidOperationException();
			};

			Assert.Throws<InvalidOperationException>(failing);

			Assert.Equal(new[] { "Failing()", "{", "}" }, Lines(text));
		}

		[Fact]
		public void StackLeakIsReported()
		{
			var (journal, text, stack) = Written();

			using (journal.Scope("Leaking()"))
				stack.StackPointer -= 2;

			Assert.Equal("}, Error: Stack leak detected in function 'Leaking', stack position should be 0x0100, but is 0x00fe", Lines(text)[2]);
		}

		[Theory]
		[InlineData("_setargv")]
		[InlineData("start")]
		[InlineData("START")]
		public void StackLeakIsAllowedInStartupFunctions(string name)
		{
			var (journal, text, stack) = Written();

			using (journal.Scope(name + "()"))
				stack.StackPointer -= 2;

			Assert.Equal("}", Lines(text)[2]);
		}

		[Fact]
		public void UnbalancedJournalThrows()
		{
			var (journal, _, _) = Written();
			journal.Probe = null;
			journal.Scope("A()");

			Exception problem = Assert.Throws<Exception>(() => journal.Scope("B()"));
			Assert.Equal("Unbalanced EnterBlock or ExitBlock", problem.Message);
		}

		[Fact]
		public void SwitchedOffJournalDoesNotFormatTheText()
		{
			LogWrapper journal = new LogWrapper((TextWriter?)null);
			Counter counter = new Counter();

			using (journal.Scope($"Count({counter})"))
			{
			}

			Assert.Equal(0, counter.Formatted);
			Assert.False(journal.NeedsText);
		}

		[Fact]
		public void ListenerGetsTheTextAlsoWhenTheJournalIsOff()
		{
			LogWrapper journal = new LogWrapper((TextWriter?)null);
			List<string> heard = new List<string>();
			int opened = 0;
			journal.ScopeListener = heard.Add;
			journal.ScopeOpened = () => opened++;
			int place = 7;

			using (journal.Scope($"Go({place})"))
			{
				using (journal.Scope("Stop()"))
				{
				}
			}

			Assert.Equal(new[] { "Go(7)", "Stop()" }, heard);
			Assert.Equal(2, opened);
		}

		[Fact]
		public void OpenedCallbackRunsAlsoWithoutListenerOrJournal()
		{
			LogWrapper journal = new LogWrapper((TextWriter?)null);
			int opened = 0;
			journal.ScopeOpened = () => opened++;

			using (journal.Scope($"Tick({opened})"))
			{
			}

			Assert.Equal(1, opened);
		}

		[Theory]
		[InlineData("Name(1, 2)", "Name")]
		[InlineData("Name()", "Name")]
		[InlineData("'Intro start'", "'Intro start'")]
		[InlineData("", "")]
		public void ScopeName(string words, string expected) => Assert.Equal(expected, LogWrapper.ScopeName(words));

		[Fact]
		public void DefaultJournalStructDoesNothing()
		{
			new JournalScope().Dispose();
		}

		private sealed class Counter
		{
			public int Formatted { get; private set; }

			public override string ToString()
			{
				this.Formatted++;
				return "counter";
			}
		}
	}
}
