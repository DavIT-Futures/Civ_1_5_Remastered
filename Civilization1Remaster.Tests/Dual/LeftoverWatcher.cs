using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// The probe of the leftover reads: a function that reads a word of its frame before it writes it gets what an earlier
/// function left at that depth of the stack. Every such read is counted with the last writer of the word ("HandleCity
/// 0x108" left there by "HandleCity bp-0x108" of the previous city, "OperateAITurn bp-0x12", a push, or nobody) and the
/// value it got. Removing a write from a frame (B2) is safe only when every reader still gets the same values.
///
/// Off unless the variable CIV_LEFTOVERS names the report file; HeadlessGame then watches every game it makes and the
/// report is written when the test process ends. Usage: run the same tests before and after a change and compare the
/// two reports with Tests/Dual/compare_leftovers.py (it lists the readers whose writers changed).
/// The frames are named by the game function that opened them (its journal scope: inlining does not hide it), the
/// helpers sharing a frame by its owner, so the names survive the rewriting of the helpers.
/// </summary>
public sealed class LeftoverWatcher : IStackWatcher
{
	private static readonly string? ReportRoute = Environment.GetEnvironmentVariable("CIV_LEFTOVERS");
	private static readonly ConcurrentDictionary<string, long> Tally = new ConcurrentDictionary<string, long>();

	private readonly CPU processor;
	private readonly List<Frame> frames = new List<Frame>();
	private readonly string?[] writers = new string?[0x10000];
	private readonly Dictionary<(string, int), string> labels = new Dictionary<(string, int), string>();

	static LeftoverWatcher()
	{
		if (ReportRoute != null)
		{
			AppDomain.CurrentDomain.ProcessExit += (_, _) =>
				File.WriteAllLines(ReportRoute, Tally.OrderBy(entry => entry.Key, StringComparer.Ordinal)
					.Select(entry => $"{entry.Key}\t{entry.Value}"));
		}
	}

	private LeftoverWatcher(CPU processor)
	{
		this.processor = processor;
	}

	/// <summary>
	/// Watches the game when the report is asked for. The frames are named by the game function opened last in the
	/// journal (each opens its scope before its frame), call it after the other listeners of the journal are set.
	/// </summary>
	public static void Attach(CPU processor, LogWrapper journal)
	{
		if (ReportRoute == null)
			return;

		LeftoverWatcher watcher = new LeftoverWatcher(processor);
		Action<string>? listener = journal.ScopeListener;

		journal.ScopeListener = scope =>
		{
			listener?.Invoke(scope);
			int open = scope.IndexOf('(');
			watcher.scope = open < 0 ? scope : scope[..open];
		};

		processor.StackWatcher = watcher;
	}

	/// <summary>The game function opened last</summary>
	private string scope = "?";

	private sealed class Frame(ushort pointer, ushort size, string owner)
	{
		public ushort Pointer { get; } = pointer;
		public ushort Size { get; } = size;
		public string Owner { get; } = owner;
		public bool[] Written { get; } = new bool[size + 1];

		/// <summary>The distance below the frame pointer, 0 outside the locals</summary>
		public int Distance(int displacement)
		{
			int distance = this.Pointer - displacement;
			return distance > 0 && distance <= this.Size ? distance : 0;
		}
	}

	public void OpenFrame(ushort framePointer, ushort localsSize)
	{
		// Frames left behind by a call that failed (an exception) lie at the same depth or deeper
		this.frames.RemoveAll(frame => frame.Pointer <= framePointer);
		this.frames.Add(new Frame(framePointer, localsSize, this.scope));

		// The coverage: which frames the tests opened at all
		Tally.AddOrUpdate("#frame " + this.scope, 1, (_, tally) => tally + 1);
	}

	public void CloseFrame(ushort framePointer)
	{
		this.frames.RemoveAll(frame => frame.Pointer <= framePointer);
	}

	public void Write(ushort displacement, int size)
	{
		for (int offset = 0; offset < size; offset++)
		{
			ushort at = (ushort)(displacement + offset);
			(Frame? frame, int distance) = Locate(at);

			if (frame != null)
			{
				frame.Written[distance] = true;
				this.writers[at] = Label(frame.Owner, distance);
			}
			else
			{
				this.writers[at] = Label(this.frames.Count > 0 ? this.frames[^1].Owner : "-", 0);
			}
		}
	}

	public void Read(ushort displacement, int size)
	{
		(Frame? frame, int distance) = Locate(displacement);

		if (frame == null)
			return;

		bool unwritten = false;

		for (int offset = 0; offset < size && distance - offset > 0; offset++)
			unwritten |= !frame.Written[distance - offset];

		if (!unwritten)
			return;

		string writer = this.writers[displacement] ?? "nobody";
		uint value = 0;

		for (int offset = size - 1; offset >= 0; offset--)
			value = (value << 8) | this.processor.Storage.PullUInt8(this.processor.SegStack.Value16, (ushort)(displacement + offset));

		Tally.AddOrUpdate($"{frame.Owner} 0x{distance:x}\t{writer}\t{value:x}", 1, (_, tally) => tally + 1);
	}

	private (Frame?, int) Locate(ushort displacement)
	{
		for (int idx = this.frames.Count - 1; idx >= 0; idx--)
		{
			int distance = this.frames[idx].Distance(displacement);

			if (distance != 0)
				return (this.frames[idx], distance);
		}

		return (null, 0);
	}

	/// <summary>"Owner bp-0x12" for a local of a frame, "Owner push" for a push or a word below the frames</summary>
	private string Label(string owner, int distance)
	{
		if (!this.labels.TryGetValue((owner, distance), out string? label))
		{
			label = distance != 0 ? $"{owner} bp-0x{distance:x}" : $"{owner} push";
			this.labels[(owner, distance)] = label;
		}

		return label;
	}
}
