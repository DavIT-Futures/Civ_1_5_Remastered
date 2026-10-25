using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The ranges of the data segment that live in C# (B6): the message buffer (B6.1), the units (B6.2), the tables of
	/// CIV.EXE (B6.3), the named global variables (B6.4), the boxes of the screens (B6.5) and the timer and mouse (B6.7). GameEngine.PrepareMachine
	/// attaches them over the memory, the snapshots of the tests write them back at their addresses (MovedData).
	/// </summary>
	public static class MovedRanges
	{
		public sealed record Range(string Key, ushort Address, int Length);

		public static IReadOnlyList<Range> All { get; } =
		[
			new Range("TextBuffer", TextBuffer.Address, TextBuffer.Size),
			new Range("Units", UnitMemory.Origin, UnitMemory.ParticipantStride * 8),
			new Range("Tables", GameTables.Begin, GameTables.End - GameTables.Begin),
			new Range("Timer", TimerState.Begin, TimerState.Size),
			new Range("PointerPlace", PointerState.PlaceAddress, 8),
			new Range("PointerShift", PointerState.ShiftAddress, 2),
			new Range("PointerFlags", PointerState.CursorAddress, 2),
			.. ScreenBoxes.Addresses.Select((address, boxIdx) => new Range($"Box{boxIdx}", address, ScreenBoxes.Size)),
			.. GameGlobals.Layout.Select(global => new Range(global.Name, global.Address, 2)),
		];
	}
}
