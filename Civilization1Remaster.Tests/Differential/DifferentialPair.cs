using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Tests.GoldenMaster;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Two headless games loaded from the same autosave with the same random seed: one runs the original code of a function
	/// (Reference), the other the rewritten one. After every call the whole state is compared: the GameSnapshot (game state,
	/// data segment, map, random generator, stack pointer, call counts), the pictures of all screens and the registers the
	/// callers depend on. While they match, both games stay identical, so the scenarios run one after the other.
	/// </summary>
	public sealed class DifferentialPair : IDisposable
	{
		public DifferentialPair(string gameFolder, int bay, uint randomSeed = GoldenMasterTests.RandomSeed, int[]? script = null)
		{
			string saves = Path.Combine(GoldenMasterTests.SourceFolder(), "Saves");

			this.Original = new HeadlessGame(gameFolder, saves, script);
			this.Rewritten = new HeadlessGame(gameFolder, saves, script);

			this.Original.LoadAutosave(bay, randomSeed);
			this.Rewritten.LoadAutosave(bay, randomSeed);

			string? difference = Compare("after loading", null, null, Register.None, null);

			if (difference != null)
				throw new InvalidOperationException(difference);
		}

		/// <summary>The game that runs the original code</summary>
		public HeadlessGame Original { get; }

		/// <summary>The game that runs the rewritten code</summary>
		public HeadlessGame Rewritten { get; }

		/// <summary>Registers that a function leaves for its caller</summary>
		[Flags]
		public enum Register
		{
			None = 0,
			Acc = 1,
			Source = 2,
			Dest = 4,
			Frame = 8,
			Stack = 0x10,
			Base = 0x20,
			Count = 0x40,
			Data = 0x80,

			/// <summary>What every function of the original keeps: SI, DI, BP, SP</summary>
			Kept = Source | Dest | Frame | Stack,
		}

		/// <summary>Changes both games in the same way (the setup of a scenario)</summary>
		public void Prepare(Action<GameEngine> setup)
		{
			setup(this.Original.Engine);
			setup(this.Rewritten.Engine);
		}

		/// <summary>
		/// Runs the original code in one game and the rewritten one in the other, and compares them
		/// </summary>
		/// <returns>The description of the differences, null when both games are the same</returns>
		public string? Run(string scenario, Action<GameEngine> original, Action<GameEngine> rewritten, Register registers,
			(ushort Segment, ushort Begin, int Length)[]? watched = null)
		{
			this.Original.Keyboard.ResetTurnCounter();
			this.Rewritten.Keyboard.ResetTurnCounter();

			this.Original.Trace = new List<string>();
			this.Rewritten.Trace = new List<string>();

			Exception? originalFailure = Invoke(original, this.Original.Engine);
			Exception? rewrittenFailure = Invoke(rewritten, this.Rewritten.Engine);

			List<string> originalTrace = this.Original.Trace;
			List<string> rewrittenTrace = this.Rewritten.Trace;
			this.Original.Trace = null;
			this.Rewritten.Trace = null;

			return Compare(scenario, originalFailure, rewrittenFailure, registers, watched) ??
				CompareTraces(scenario, originalTrace, rewrittenTrace);
		}

		/// <summary>
		/// The called game functions with their arguments, in their order, must be the same: it also catches different
		/// arguments of a drawing call when the picture is empty
		/// </summary>
		private static string? CompareTraces(string scenario, List<string> original, List<string> rewritten)
		{
			int length = Math.Min(original.Count, rewritten.Count);

			for (int idx = 0; idx < length; idx++)
			{
				if (original[idx] != rewritten[idx] && WithoutStackAddresses(original[idx]) != WithoutStackAddresses(rewritten[idx]))
					return $"{scenario}: call {idx} differs from the original: {original[idx]} -> {rewritten[idx]}";
			}

			return original.Count == rewritten.Count ? null :
				$"{scenario}: {original.Count} calls in the original, {rewritten.Count} in the rewritten code, the first missing: " +
				(original.Count > rewritten.Count ? original[length] : rewritten[length]);
		}

		/// <summary>
		/// Addresses in the stack (0xe8c0 - 0xf0bf, the stack starts at 0xf0c0) passed as arguments, like a text buffer of a
		/// caller, move when a rewritten function has no stack frame any more; their content is compared with the memory
		/// </summary>
		private static string WithoutStackAddresses(string call)
		{
			return System.Text.RegularExpressions.Regex.Replace(call, "0x[0-9a-f]{4}", match =>
			{
				int amount = Convert.ToInt32(match.Value, 16);

				return amount >= 0xe8c0 && amount < 0xf0c0 ? "<stack>" : match.Value;
			});
		}

		private static Exception? Invoke(Action<GameEngine> action, GameEngine engine)
		{
			try
			{
				action(engine);
				return null;
			}
			catch (Exception failure)
			{
				return failure;
			}
		}

		private string? Compare(string scenario, Exception? originalFailure, Exception? rewrittenFailure, Register registers,
			(ushort Segment, ushort Begin, int Length)[]? watched)
		{
			SortedDictionary<string, string> expected = Describe(this.Original, originalFailure, registers, watched);
			SortedDictionary<string, string> actual = Describe(this.Rewritten, rewrittenFailure, registers, watched);

			List<string> lines = expected.Keys.Union(actual.Keys).OrderBy(key => key, StringComparer.Ordinal)
				.Where(key => !expected.TryGetValue(key, out string? left) || !actual.TryGetValue(key, out string? right) || left != right)
				.Select(key => $"  {key}: {(expected.TryGetValue(key, out string? left) ? left : "(none)")} -> " +
					$"{(actual.TryGetValue(key, out string? right) ? right : "(none)")}")
				.ToList();

			if (lines.Count == 0)
				return null;

			StringBuilder text = new StringBuilder();
			text.AppendLine($"{scenario}: the rewritten code differs from the original ({lines.Count} values, original -> rewritten):");

			foreach (string line in lines.Take(40))
				text.AppendLine(line);

			return text.ToString();
		}

		private static SortedDictionary<string, string> Describe(HeadlessGame game, Exception? failure, Register registers,
			(ushort Segment, ushort Begin, int Length)[]? watched)
		{
			GameSnapshot snapshot = game.Capture();
			SortedDictionary<string, string> values = new SortedDictionary<string, string>(snapshot.Values, StringComparer.Ordinal);

			values.Remove("input.keys");

			foreach (var display in game.Engine.VideoCard.Displays)
			{
				values[$"screen.{display.Token}"] = GameSnapshot.Fnv(display.Datum.PictureStorage).ToString("x8");
			}

			CPU processor = game.Engine.Processor;

			// Memory outside the data segment that the GameSnapshot doesn't see (tables in other segments)
			foreach ((ushort segment, ushort begin, int length) in watched ?? [])
			{
				for (int idx = 0; idx < length; idx++)
					values[$"watch.{segment:x4}:{begin + idx:x4}"] = processor.PullUInt8(segment, (ushort)(begin + idx)).ToString("x2");
			}

			AddRegister(values, registers, Register.Acc, "ax", processor.RegAcc.Value16);
			AddRegister(values, registers, Register.Base, "bx", processor.RegBase.Value16);
			AddRegister(values, registers, Register.Count, "cx", processor.RegCount.Value16);
			AddRegister(values, registers, Register.Data, "dx", processor.RegData.Value16);
			AddRegister(values, registers, Register.Source, "si", processor.RegSource.Value16);
			AddRegister(values, registers, Register.Dest, "di", processor.RegDest.Value16);
			AddRegister(values, registers, Register.Frame, "bp", processor.RegFrame.Value16);
			AddRegister(values, registers, Register.Stack, "sp", processor.RegStack.Value16);

			if (failure != null)
				values["failure"] = $"{failure.GetType().Name}: {failure.Message}";

			return values;
		}

		private static void AddRegister(IDictionary<string, string> values, Register wanted, Register register, string name, ushort content)
		{
			if ((wanted & register) != 0)
				values[$"reg.{name}"] = content.ToString("x4");
		}

		public void Dispose()
		{
			this.Original.Dispose();
			this.Rewritten.Dispose();
		}
	}
}
