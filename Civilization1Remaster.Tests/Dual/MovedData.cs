using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Engine;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// The data the current game no longer keeps in the emulated memory (the steps B of PLAN-PO-MIGRACJI.md). The frozen game
/// still has it at its address, the current one in a field. Both are compared by meaning under the key "moved.{Key}", and
/// the snapshot of the current game writes the data back at its address before it hashes the data segment (Project, B6.0),
/// so the golden master baselines and the frozen game keep matching byte for byte. Every step that moves data adds its
/// entry here.
/// </summary>
public static class MovedData
{
	/// <summary>A moved piece of data: its key, its address and length in the data segment of the frozen game, how the current game gives it (the bytes in the layout of the original, low first)</summary>
	public sealed record Entry(string Key, ushort Address, int Length, Func<GameEngine, byte[]> Current);

	/// <summary>The data moved so far</summary>
	public static readonly Entry[] Entries =
	[
		// B6.1: the message buffer (TextBuffer, 0x300 bytes)
		new Entry("TextBuffer", TextBuffer.Address, TextBuffer.Size, engine => engine.Words.Bytes),
	];

	/// <summary>Whether the byte of the data segment belongs to moved data</summary>
	public static bool IsMoved(int displacement) => Entries.Any(entry => displacement >= entry.Address && displacement < entry.Address + entry.Length);

	/// <summary>The bytes as the frozen game has them in memory</summary>
	public static void AddFrozen(Civilization1Original.GameEngine engine, IDictionary<string, string> values)
	{
		foreach (Entry entry in Entries)
		{
			byte[] bytes = Enumerable.Range(0, entry.Length).Select(idx => engine.Processor.PullGlobalByte((ushort)(entry.Address + idx))).ToArray();
			values[$"moved.{entry.Key}"] = Convert.ToHexString(bytes);
		}
	}

	/// <summary>The same data as the current game gives it</summary>
	public static void AddCurrent(GameEngine engine, IDictionary<string, string> values)
	{
		foreach (Entry entry in Entries)
			values[$"moved.{entry.Key}"] = Convert.ToHexString(entry.Current(engine));
	}

	/// <summary>Writes the moved data of the current game back at its address of a copy of the data segment</summary>
	public static void Project(GameEngine engine, byte[] segment) => Project(engine, segment, Entries);

	public static void Project(GameEngine engine, byte[] segment, IEnumerable<Entry> entries)
	{
		foreach (Entry entry in entries)
		{
			byte[] bytes = entry.Current(engine);

			if (bytes.Length != entry.Length)
				throw new InvalidOperationException($"Moved data {entry.Key} gives {bytes.Length} bytes instead of {entry.Length}");

			Array.Copy(bytes, 0, segment, entry.Address, bytes.Length);
		}
	}
}
