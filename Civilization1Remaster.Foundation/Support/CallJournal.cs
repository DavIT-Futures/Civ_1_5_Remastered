using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Civilization1Remaster.Foundation.Support
{
	public readonly struct JournalScope : IDisposable
	{
		private readonly CallJournal? journal;
		private readonly string name;

		internal JournalScope(CallJournal journal, string name)
		{
			this.journal = journal;
			this.name = name;
		}

		public void Dispose()
		{
			this.journal?.CloseScope(this.name);
		}
	}

	[InterpolatedStringHandler]
	public ref struct ScopeText
	{
		private readonly bool enabled;
		private DefaultInterpolatedStringHandler inner;

		public ScopeText(int literalLength, int formattedCount, CallJournal journal, out bool enabled)
		{
			this.enabled = enabled = journal.NeedsText;
			this.inner = enabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
		}

		public void AppendLiteral(string value) => this.inner.AppendLiteral(value);

		public void AppendFormatted<T>(T value) => this.inner.AppendFormatted(value);

		public void AppendFormatted<T>(T value, string? format) => this.inner.AppendFormatted(value, format);

		public void AppendFormatted<T>(T value, int alignment) => this.inner.AppendFormatted(value, alignment);

		public void AppendFormatted<T>(T value, int alignment, string? format) => this.inner.AppendFormatted(value, alignment, format);

		public void AppendFormatted(string? value) => this.inner.AppendFormatted(value);

		internal string ToStringAndClear() => this.enabled ? this.inner.ToStringAndClear() : string.Empty;
	}

	public class CallJournal
	{
		private readonly TextWriter? journal;
		private readonly Stack<ushort> pile = new Stack<ushort>();

		private int journalIndentTier = 0;
		private IStackProbe? probe;

		public CallJournal(string route)
			: this(On ? new StreamWriter(route) : null)
		{
		}

		public CallJournal(TextWriter? journal)
		{
			this.journal = journal;
		}

		public static bool On { get; } = Environment.GetEnvironmentVariable("CIVILIZATION1REMASTER_LOG")?.Trim() == "1";

		public IStackProbe? Probe
		{
			get { return this.probe; }
			set { this.probe = value; }
		}

		public bool NeedsText => this.journal != null;

		public static string ScopeName(string words)
		{
			int bracket = words.IndexOf('(');

			return bracket < 0 ? words : words.Substring(0, bracket);
		}

		public JournalScope Scope(string words)
		{
			OpenScope(words);

			return new JournalScope(this, this.journal != null ? ScopeName(words) : string.Empty);
		}

		public JournalScope Scope([InterpolatedStringHandlerArgument("")] ref ScopeText words)
		{
			return Scope(words.ToStringAndClear());
		}

		public void EmitLine(string words)
		{
			if (this.journal != null)
			{
				EmitTabs(this.journalIndentTier);
				this.journal.WriteLine(words);
				this.journal.Flush();
			}
		}

		public void Emit(string words)
		{
			if (this.journal != null)
			{
				this.journal.Write(words);
				this.journal.Flush();
			}
		}

		internal void CloseScope(string words)
		{
			if (this.journal != null)
			{
				if (this.pile.Count != this.journalIndentTier)
				{
					throw new Exception("Unbalanced EnterBlock or ExitBlock");
				}

				this.journalIndentTier = Math.Max(0, this.journalIndentTier - 1);
				EmitTabs(this.journalIndentTier);
				this.journal.Write("}");

				ushort pile = this.pile.Pop();
				if (this.probe != null &&
					!words.Equals("_setargv", StringComparison.InvariantCultureIgnoreCase) &&
					!words.Equals("start", StringComparison.InvariantCultureIgnoreCase) &&
					this.probe.StackPointer != pile)
				{
					this.journal.Write($", Error: Stack leak detected in function '{words}', " +
						$"stack position should be 0x{pile:x4}, but is 0x{this.probe.StackPointer:x4}");
				}

				this.journal.WriteLine();

				this.journal.Flush();
			}
		}

		private void OpenScope(string words)
		{
			if (this.journal != null)
			{
				if (this.pile.Count != this.journalIndentTier)
				{
					throw new Exception("Unbalanced EnterBlock or ExitBlock");
				}

				if (this.probe != null)
				{
					this.pile.Push(this.probe.StackPointer);
				}

				EmitTabs(this.journalIndentTier);
				this.journal.WriteLine(words);

				EmitTabs(this.journalIndentTier);
				this.journal.WriteLine("{");
				this.journal.Flush();
				this.journalIndentTier++;
			}
		}

		private void EmitTabs(int tier)
		{
			this.journal?.Write($"{new string('\t', tier)}");
		}
	}
}
