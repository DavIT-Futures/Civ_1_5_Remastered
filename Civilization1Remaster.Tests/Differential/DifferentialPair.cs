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
	/// data segment, map, random generator, stack pointer, call counts), the pictures of all screens and the result
	/// registers (AX, DX). While they match, both games stay identical, so the scenarios run one after the other.
	/// Since phase 0, point 4 (2026-10-25) the other registers (SI, DI, BP, SP, BX, CX) and the stack below SP are no longer
	/// compared, whatever a test asks for: they are what B3 removes (frames, saved registers), the state is what matters.
	/// </summary>
	public sealed class DifferentialPair : IDisposable
	{
		public DifferentialPair(string gameFolder, int bay, uint randomSeed = GoldenMasterTests.RandomSeed, int[]? script = null)
		{
			this.gameFolder = gameFolder;
			this.bay = bay;
			this.randomSeed = randomSeed;
			this.script = script;

			Load();
		}

		private readonly string gameFolder;
		private readonly int bay;
		private readonly uint randomSeed;
		private readonly int[]? script;

		/// <summary>The game that runs the original code</summary>
		public HeadlessGame Original { get; private set; } = null!;

		/// <summary>The game that runs the rewritten code</summary>
		public HeadlessGame Rewritten { get; private set; } = null!;

		private GameCheckpoint? checkpoint;

		/// <summary>
		/// Both games back to the state after loading (the first call takes the checkpoint): a scenario that changes the state
		/// then starts from the saved game, not from what the scenarios before it left (their changes add up to states the
		/// original may loop on)
		/// </summary>
		public void Restore()
		{
			if (this.checkpoint == null)
			{
				this.checkpoint = new GameCheckpoint(this.Original);
				return;
			}

			this.checkpoint.Restore(this.Original);
			this.checkpoint.Restore(this.Rewritten);

			string? difference = Compare("after the checkpoint", null, null, Register.None, null);

			if (difference != null)
				throw new InvalidOperationException(difference);
		}

		private void Load()
		{
			string saves = Path.Combine(GoldenMasterTests.SourceFolder(), "Saves");

			this.Original = new HeadlessGame(this.gameFolder, saves, this.script, classic: true);
			this.Rewritten = new HeadlessGame(this.gameFolder, saves, this.script, classic: true);

			this.Original.LoadAutosave(this.bay, this.randomSeed);
			this.Rewritten.LoadAutosave(this.bay, this.randomSeed);

			string? difference = Compare("after loading", null, null, Register.None, null);

			if (difference != null)
				throw new InvalidOperationException(difference);
		}

		/// <summary>The registers compared: the results of a function (AX, and DX for 32-bit results)</summary>
		public const Register Compared = Register.Acc | Register.Data;

		/// <summary>The first stack address of the data segment (the stack: 0xe8c0 - 0xffff, see GameSnapshot)</summary>
		private const int StackBegin = 0xe8c0;

		/// <summary>Registers that a function leaves for its caller (only those in Compared are compared)</summary>
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

		/// <summary>
		/// The longest a run of one scenario may take in one game (a scenario takes well under a second; making a world or the
		/// opening tens of seconds while the whole suite runs in parallel): an endless loop then fails the test with the
		/// scenario and the last call instead of hanging for hours
		/// </summary>
		public int RunSeconds { get; set; } = 120;

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

			this.Original.Deadline = Environment.TickCount64 + RunSeconds * 1000L;
			Exception? originalFailure = Invoke(original, this.Original.Engine);
			this.Original.Deadline = 0;

			this.Rewritten.Deadline = Environment.TickCount64 + RunSeconds * 1000L;
			Exception? rewrittenFailure = Invoke(rewritten, this.Rewritten.Engine);
			this.Rewritten.Deadline = 0;

			if (originalFailure is ScenarioHangException || rewrittenFailure is ScenarioHangException)
			{
				this.Original.Trace = null;
				this.Rewritten.Trace = null;

				return $"{scenario}: hangs (more than {RunSeconds} s) – original: {originalFailure?.Message ?? "finished"}; " +
					$"rewritten: {rewrittenFailure?.Message ?? "finished"}";
			}

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
		/// Addresses in the stack (0xe8c0 - 0xf0bf, the stack starts at 0xf0c0; hex or decimal) passed as arguments, like a text buffer of a
		/// caller, move when a rewritten function has no stack frame any more; their content is compared with the memory
		/// </summary>
		private static string WithoutStackAddresses(string call)
		{
			// In hex (0x....) or, in some scopes, in decimal
			return System.Text.RegularExpressions.Regex.Replace(call, @"0x[0-9a-f]{4}|\b\d{5}\b", match =>
			{
				int amount = match.Value.StartsWith("0x") ? Convert.ToInt32(match.Value, 16) : int.Parse(match.Value);

				return amount >= 0xe8c0 && amount < 0xf0c0 ? "<stack>" : match.Value;
			});
		}

		/// <summary>
		/// Runs the scenario. An exception leaves the frames of the functions it left on the emulated stack, as many as the
		/// version has (B3 removes frames): the stack pointer and the frame pointer go back to where they were, like the C#
		/// stack unwinds, so the next scenarios of the game start at the same depth in both versions.
		/// </summary>
		private static Exception? Invoke(Action<GameEngine> action, GameEngine engine)
		{
			ushort stack = engine.Processor.RegStack.Value16;
			ushort frame = engine.Processor.RegFrame.Value16;

			try
			{
				action(engine);
				return null;
			}
			catch (Exception failure)
			{
				engine.Processor.RegStack.Value16 = stack;
				engine.Processor.RegFrame.Value16 = frame;
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
			values.Remove("cpu.sp");

			foreach (var display in game.Engine.VideoCard.Displays)
			{
				values[$"screen.{display.Token}"] = GameSnapshot.Fnv(display.Datum.PictureStorage).ToString("x8");
			}

			CPU processor = game.Engine.Processor;

			// Memory outside the data segment that the GameSnapshot doesn't see (tables in other segments); not the stack
			// (frames and what lies below them, which B3 removes)
			foreach ((ushort segment, ushort begin, int length) in watched ?? [])
			{
				for (int idx = 0; idx < length; idx++)
				{
					if (segment == GameSnapshot.DataSection && begin + idx >= StackBegin)
						continue;

					values[$"watch.{segment:x4}:{begin + idx:x4}"] = processor.PullUInt8(segment, (ushort)(begin + idx)).ToString("x2");
				}
			}

			registers &= Compared;

			AddRegister(values, registers, Register.Acc, "ax", processor.RegAcc.Value16);
			AddRegister(values, registers, Register.Base, "bx", processor.RegBase.Value16);
			AddRegister(values, registers, Register.Count, "cx", processor.RegCount.Value16);
			AddRegister(values, registers, Register.Data, "dx", processor.RegData.Value16);
			AddRegister(values, registers, Register.Source, "si", processor.RegSource.Value16);
			AddRegister(values, registers, Register.Dest, "di", processor.RegDest.Value16);
			AddRegister(values, registers, Register.Frame, "bp", processor.RegFrame.Value16);
			AddRegister(values, registers, Register.Stack, "sp", processor.RegStack.Value16);

			if (failure != null)
				values["failure"] = $"{failure.GetType().Name}: {failure.Message.Replace(game.Folder, "<game folder>", StringComparison.Ordinal)}";

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
