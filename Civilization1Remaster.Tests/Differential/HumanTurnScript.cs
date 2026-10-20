using System;
using System.Collections.Generic;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.Tests.Differential;

/// <summary>
/// The keyboard (and mouse) of the human player for a differential test of a whole turn: the inputs of the scenario first,
/// then Space, Enter, '1' and Escape, which end any turn (without Escape a GoTo pointer or a picked unit waits for keys
/// without end). A click comes while the game only polls the keyboard; the mouse is there (0x1a3c) from the click to the
/// next key, as screens like the city screen wait for the mouse otherwise. Before each unit the game empties the keyboard
/// (AwaitForKey): the input that comes then is lost, so a key meant for a unit needs one before it (porcja 99: '1').
/// </summary>
internal static class HumanTurnScript
{
	/// <summary>A key, or a click (button 1, or 2 held) at a place of the screen</summary>
	public sealed record Input(int Key, int Button = 0, int Horiz = 0, int Vert = 0);

	private static readonly int[] Fallback = { 0x20, 0x0d, 0x31, 0x1b };

	/// <summary>Installs the keyboard on the game (call it from the Prepare of the scenario: both games get the same)</summary>
	/// <remarks>The game is dynamic: the current game or the frozen one (Civilization1Original), which has the same members</remarks>
	public static void Install(object engine, IEnumerable<Input> inputs)
	{
		dynamic game = engine;
		Queue<Input> pending = new Queue<Input>(inputs);
		int fallbackIdx = 0;
		int typed = 0;
		bool withhold = false;

		game.Processor.EmitUInt16(0x3b01, 0x1a3c, 0);

		game.Processor.InputStarved = (Action<bool>)(waiting =>
		{
			game.Processor.Idle();

			if (!waiting && withhold)
			{
				withhold = false;
				return;
			}

			if (++typed > 3000)
				throw new InvalidOperationException("The turn asked for more than 3000 keys");

			if (!waiting && pending.Count > 0 && pending.Peek().Key == 0)
			{
				Input click = pending.Dequeue();
				game.Processor.Storage.EmitUInt16(0x3b01, 0x586e, (ushort)Math.Clamp(click.Horiz, 0, 319));
				game.Processor.Storage.EmitUInt16(0x3b01, 0x5870, (ushort)Math.Clamp(click.Vert, 0, 199));
				game.Processor.Storage.EmitUInt16(0x3b01, 0x5872, 0);
				game.Processor.Storage.EmitUInt16(0x3b01, 0x5874, (ushort)click.Button);
				game.Processor.Storage.EmitUInt16(0x3b01, 0x1a3c, 1);
				withhold = true;
				return;
			}

			int key;

			if (pending.Count > 0 && pending.Peek().Key != 0)
				key = pending.Dequeue().Key;
			else
			{
				key = Fallback[fallbackIdx];
				fallbackIdx = (fallbackIdx + 1) % Fallback.Length;
			}

			withhold = true;
			game.Processor.Storage.EmitUInt16(0x3b01, 0x1a3c, 0);

			lock (game.VideoCard.Tokens)
				game.VideoCard.Tokens.Enqueue(key);
		});
	}
}
