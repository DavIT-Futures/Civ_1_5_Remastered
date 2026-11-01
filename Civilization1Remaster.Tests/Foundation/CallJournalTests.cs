using Civilization1Remaster.Foundation.Support;

namespace Civilization1Remaster.Tests.Foundation
{
	public partial class CallJournalTests
	{
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
			{
				stack.StackPointer -= 2;
			}

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
			{
				stack.StackPointer -= 2;
			}

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
			CallJournal journal = new CallJournal((TextWriter?)null);
			Counter counter = new Counter();

			using (journal.Scope($"Count({counter})"))
			{
			}

			Assert.Equal(0, counter.Formatted);
			Assert.False(journal.NeedsText);
		}

		[Theory]
		[InlineData("Name(1, 2)", "Name")]
		[InlineData("Name()", "Name")]
		[InlineData("'Intro start'", "'Intro start'")]
		[InlineData("", "")]
		public void ScopeName(string words, string expected) => Assert.Equal(expected, CallJournal.ScopeName(words));

		[Fact]
		public void DefaultJournalStructDoesNothing()
		{
			default(JournalScope).Dispose();
		}

		private static (CallJournal Journal, StringWriter Text, FakeStack Stack) Written()
		{
			StringWriter text = new StringWriter();
			FakeStack stack = new FakeStack();
			CallJournal journal = new CallJournal(text) { Probe = stack };
			return (journal, text, stack);
		}

		private static string[] Lines(StringWriter text) => text.ToString().Split(text.NewLine).SkipLast(1).ToArray();
	}
}
