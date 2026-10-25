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

	/// <summary>The data moved so far: every range of MovedRanges (Core), as the current game gives it through the memory</summary>
	public static readonly Entry[] Entries = MovedRanges.All
		.Select(range => new Entry(range.Key, range.Address, range.Length, engine => Read(engine, range.Address, range.Length)))
		.ToArray();

	/// <summary>The bytes of the current game at the address, also of the ranges kept in C#</summary>
	private static byte[] Read(GameEngine engine, ushort address, int length)
	{
		byte[] bytes = new byte[length];
		engine.Processor.Storage.ReadChunk(Civilization1Remaster.Emulation.CPU.AsLinearAddress(engine.Processor.SegData.Value16, address), bytes, 0, length);
		return bytes;
	}

	/// <summary>Whether the byte of the data segment belongs to moved data</summary>
	public static bool IsMoved(int displacement) => Entries.Any(entry => displacement >= entry.Address && displacement < entry.Address + entry.Length);

	/// <summary>The bytes as the frozen game has them in memory</summary>
	public static void AddFrozen(Civilization1Original.GameEngine engine, IDictionary<string, string> values)
	{
		foreach (Entry entry in Entries)
		{
			// The bytes as they are (the CPU reads give 0 for the bytes the game may not touch, like 0x19be - 0x19c1)
			uint origin = Civilization1Original.Emulation.CPU.AsLinearAddress(engine.Processor.SegData.Value16, entry.Address);
			byte[] bytes = engine.Processor.Storage.StoragePayload.AsSpan((int)origin, entry.Length).ToArray();
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
