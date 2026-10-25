using System;
using System.Collections.Generic;
using Civilization1Original.Emulation;

namespace Civilization1Original.Tests
{
	/// <summary>
	/// Keyboard of the headless game: when the game looks for a key and none is waiting, the next key of a fixed script
	/// is typed. The keys depend only on the order of the game's keyboard calls, never on time, so a run is reproducible.
	/// </summary>
	/// <remarks>
	/// A game that waits for a key (ReadKey) always gets one. A game that only polls (KeyAvailable) gets "no key" on the
	/// first poll after a typed key and a key on the next one, so a loop that empties the keyboard buffer ends and a loop
	/// that waits for a key press continues.
	/// Every keyboard call is also an idle moment for the game, the next timer interrupt is due then (CPU.Idle).
	/// </remarks>
	public sealed class ScriptedKeyboard
	{
		/// <summary>
		/// Space skips the active unit of the human player (no orders), Enter accepts the default choice of a menu or dialog
		/// and closes a message, '1' picks the first part in the palace screen (it is ignored on the map, the moves are scan codes).
		/// Together they get through a human turn without changing anything on purpose.
		/// </summary>
		public static readonly int[] DefaultScript = { 0x20, 0x0d, 0x31 };

		private readonly CPU processor;
		private readonly Queue<int> tokens;
		private readonly object latch;
		private readonly int[] script;
		private int scriptIdx = 0;
		private bool withholdNextPoll = false;

		public ScriptedKeyboard(CPU processor, Queue<int> tokens, object latch, int[] script)
		{
			this.processor = processor;
			this.tokens = tokens;
			this.latch = latch;
			this.script = script;
		}

		/// <summary>Called just before a key is typed, while the game waits for it (the differential tests note the screen)</summary>
		public Action? KeyTyping { get; set; }

		/// <summary>Keys typed since the start</summary>
		public long KeysTyped { get; private set; }

		/// <summary>Keys typed since the last ResetTurnCounter</summary>
		public int KeysThisTurn { get; private set; }

		/// <summary>A turn that needs more keys is stuck (the script doesn't get through some screen)</summary>
		public int TurnKeyLimit { get; set; } = 20000;

		public void ResetTurnCounter()
		{
			this.KeysThisTurn = 0;
		}

		public void OnInputStarved(bool waiting)
		{
			// Time passes while the game waits for a key, not when it only looks for one (B6.0: how often the code looks is
			// a detail of the code and must not move the timer interrupts of the fixed clock)
			if (waiting)
				this.processor.Idle();

			if (!waiting && this.withholdNextPoll)
			{
				this.withholdNextPoll = false;
				return;
			}

			if (++this.KeysThisTurn > this.TurnKeyLimit)
				throw new InvalidOperationException($"The headless game is stuck, it asked for more than {this.TurnKeyLimit} keys in one turn.");

			this.KeyTyping?.Invoke();

			int key = this.script[this.scriptIdx];
			this.scriptIdx = (this.scriptIdx + 1) % this.script.Length;
			this.KeysTyped++;
			this.withholdNextPoll = true;

			lock (this.latch)
			{
				this.tokens.Enqueue(key);
			}
		}
	}
}
