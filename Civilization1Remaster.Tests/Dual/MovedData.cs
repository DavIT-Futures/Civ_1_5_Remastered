using System;
using System.Collections.Generic;
using System.Linq;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// The data the current game no longer keeps in the emulated memory (the steps B of PLAN-PO-MIGRACJI.md). The frozen game
/// still has it at its address, the current one in a field: both are compared by meaning under the key "moved.{Key}", and
/// the address is left out of the hashes of the data segment on both sides. Every step that moves data adds its entry here.
/// </summary>
public static class MovedData
{
	/// <summary>A moved piece of data: its key, its address and length in the data segment of the frozen game, how the current game gives it (the bytes in hex, low first)</summary>
	public sealed record Entry(string Key, ushort Address, int Length, Func<GameEngine, string> Current);

	/// <summary>Nothing is moved yet</summary>
	public static readonly Entry[] Entries = [];

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
			values[$"moved.{entry.Key}"] = entry.Current(engine);
	}
}
