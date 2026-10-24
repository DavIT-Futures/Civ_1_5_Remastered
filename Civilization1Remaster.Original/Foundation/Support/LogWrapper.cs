using System;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Original
{
	/// <summary>
	/// Log of the called game functions, for debugging. Every line is written to the disk immediately, which makes
	/// the game much slower, so the logs are written only when the CIVILIZATION1REMASTER_LOG environment variable is set to 1.
	/// </summary>
	public class LogWrapper
	{
		private readonly StreamWriter? journal;
		private int journalIndentTier = 0;
		private IStackProbe? probe = null;
		private readonly Stack<ushort> pile = new Stack<ushort>();

		public LogWrapper(string route)
		{
			if (On)
			{
				this.journal = new StreamWriter(route);
			}
		}

		public static bool On { get; } = Environment.GetEnvironmentVariable("CIVILIZATION1REMASTER_LOG")?.Trim() == "1";

		/// <summary>
		/// Test seam: receives the text of every opened scope (the called game function), also when the log file is off
		/// </summary>
		public Action<string>? ScopeListener { get; set; }

		/// <summary>Test seam: called at the start of every scope, before ScopeListener (the fixed test clock counts them)</summary>
		public Action? ScopeOpened { get; set; }

		public IStackProbe? Probe
		{
			get { return this.probe; }
			set { this.probe = value; }
		}

		public void OpenScope(string words)
		{
			OpenScope(words, false);
		}

		public void OpenScope(string words, bool displayLatches)
		{
			this.ScopeOpened?.Invoke();
			this.ScopeListener?.Invoke(words);

			if (this.journal != null)
			{
				if (this.pile.Count != this.journalIndentTier)
					throw new Exception("Unbalanced EnterBlock or ExitBlock");

				if (this.probe != null)
					this.pile.Push(this.probe.StackPointer);

				EmitTabs(this.journalIndentTier);
				this.journal.Write($"{words}");

				if (this.probe != null && displayLatches)
					this.journal.Write($" // Stack: 0x{this.probe.StackSegment:x4}:0x{this.probe.StackPointer:x4}, DS 0x{this.probe.DataSegment:x4}, BP 0x{this.probe.FramePointer:x4}");

				this.journal.WriteLine();

				EmitTabs(this.journalIndentTier);
				this.journal.WriteLine("{");
				this.journal.Flush();
				this.journalIndentTier++;
			}
		}

		public void CloseScope(string words)
		{
			if (this.journal != null)
			{
				if (this.pile.Count != this.journalIndentTier)
					throw new Exception("Unbalanced EnterBlock or ExitBlock");

				this.journalIndentTier = Math.Max(0, this.journalIndentTier - 1);
				EmitTabs(this.journalIndentTier);
				this.journal.Write("}");

				ushort pile = this.pile.Pop();
				// _setargv pushes permanent data on the stack!
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

		private void EmitTabs(int tier)
		{
			this.journal?.Write($"{new string('\t', tier)}");
		}
	}
}
