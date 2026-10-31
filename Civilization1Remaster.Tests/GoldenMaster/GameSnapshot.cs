using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Video;

namespace Civilization1Remaster.Tests.GoldenMaster
{
	/// <summary>
	/// Canonical picture of the game after a turn: a sorted map of key = value. Values that are 0, empty or false are left
	/// out, so a missing key means the default value.
	/// </summary>
	/// <remarks>
	/// Key prefixes (channels):
	/// <list type="bullet">
	/// <item>state.  every field of GameState (players, cities, units, gold, science, relations, ...), read by reflection</item>
	/// <item>engine. the global variables of GameEngine that were moved out of the emulated memory</item>
	/// <item>mem.    FNV hash of each 256 byte page of the emulated data segment (0x3b01), without the timer variables and the stack</item>
	/// <item>map.    FNV hash of each band of 10 rows of the map picture (screen 2, the terrain and its improvements)</item>
	/// <item>rng.    hash of the state of the C runtime random generator, catches a different number of random draws</item>
	/// <item>cpu.    the stack pointer between turns, the original code leaves it where it was (a leak overwrites the data segment)</item>
	/// <item>call.   how many times each game function was called in the turn (processor Journal), informational only</item>
	/// <item>input.  how many keys the scripted keyboard typed in the turn, informational only</item>
	/// </list>
	/// </remarks>
	public sealed class GameSnapshot
	{
		public const ushort DataSection = 0x3b01;

		/// <summary>
		/// Parts of the data segment that change with the timer, the computer or the call depth, not with the game: the variables
		/// of the timer interrupt (counters, sound, palette fade), the path of CIV.EXE (written by GameEngine.PrepareMachine,
		/// the tests run from a temporary folder), the mouse state and the stack (0xe8c0 - 0xffff, it starts at 0xf0c0).
		/// </summary>
		public static readonly (ushort Begin, ushort End)[] IgnoredDataRanges =
		{
			(0x0040, 0x006f),
			(0x6156, 0x61ed),
			(0x586e, 0x5874),
			(0xe8c0, 0xffff),
		};

		/// <summary>Channels whose differences fail the test. The others only describe how the game got there.</summary>
		public static readonly string[] BehaviourChannels = { "state.", "engine.", "mem.", "map.", "rng.", "cpu." };

		public SortedDictionary<string, string> Values { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);

		public static GameSnapshot Capture(GameEngine engine, IReadOnlyDictionary<string, int> calls, int keysTyped)
		{
			GameSnapshot snapshot = new GameSnapshot();

			ObjectWalker.Walk("state", engine.World, snapshot.Values);

			// The public fields of the engine and the fields behind its public auto-properties, by their names
			foreach (FieldInfo field in typeof(GameEngine).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(f => f.FieldType.IsPrimitive && (f.IsPublic || IsPublicPropertyField(typeof(GameEngine), f))))
			{
				ObjectWalker.Walk($"engine.{ObjectWalker.MemberName(field)}", field.GetValue(engine), snapshot.Values);
			}

			CaptureDataSegment(engine, snapshot.Values);
			CaptureMap(engine, snapshot.Values);
			CaptureRandom(engine.Libc.Dice, snapshot.Values);
			snapshot.Values["cpu.sp"] = $"{engine.Processor.SegStack.Value16:x4}:{engine.Processor.RegStack.Value16:x4}";

			foreach (KeyValuePair<string, int> call in calls)
			{
				snapshot.Values[$"call.{call.Key}"] = call.Value.ToString(CultureInfo.InvariantCulture);
			}

			if (keysTyped != 0)
				snapshot.Values["input.keys"] = keysTyped.ToString(CultureInfo.InvariantCulture);

			return snapshot;
		}

		public static bool IsBehaviour(string key)
		{
			return BehaviourChannels.Any(channel => key.StartsWith(channel, StringComparison.Ordinal));
		}

		private static void CaptureDataSegment(GameEngine engine, IDictionary<string, string> values)
		{
			// The data segment as the original game has it: the data moved out of the emulated memory into C# is written back
			// at its address (MovedData.Project), so the hashes stay those of the baselines and of the frozen game
			byte[] storage = new byte[0x10000];
			Array.Copy(engine.Processor.Storage.StoragePayload, CPU.AsLinearAddress(DataSection, 0), storage, 0, storage.Length);
			Civilization1Remaster.Tests.Dual.MovedData.Project(engine, storage);

			uint origin = 0;
			byte[] page = new byte[0x100];

			for (int pageIdx = 0; pageIdx < 0x100; pageIdx++)
			{
				int pageBegin = pageIdx << 8;
				bool anyContent = false;

				for (int idx = 0; idx < page.Length; idx++)
				{
					int displacement = pageBegin + idx;
					page[idx] = IsIgnored(displacement) ? (byte)0 : storage[origin + displacement];
					anyContent |= page[idx] != 0;
				}

				if (anyContent)
					values[$"mem.{pageBegin:x4}"] = Fnv(page).ToString("x8");
			}
		}

		private static bool IsIgnored(int displacement)
		{
			foreach ((ushort begin, ushort end) in IgnoredDataRanges)
			{
				if (displacement >= begin && displacement <= end)
					return true;
			}

			return false;
		}

		private static void CaptureMap(GameEngine engine, IDictionary<string, string> values)
		{
			if (!engine.VideoCard.Displays.IncludesKey(2))
				return;

			VGABitmap chart = engine.VideoCard.Displays.ObtainValueByKey(2);
			byte[] pixels = chart.PictureStorage;
			int band = chart.Pitch * 10;

			for (int begin = 0, bandIdx = 0; begin < pixels.Length; begin += band, bandIdx++)
			{
				values[$"map.rows{bandIdx * 10:000}"] = Fnv(pixels.AsSpan(begin, Math.Min(band, pixels.Length - begin))).ToString("x8");
			}
		}

		private static void CaptureRandom(object dice, IDictionary<string, string> values)
		{
			StringBuilder text = new StringBuilder();

			foreach (FieldInfo field in dice.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				.OrderBy(f => f.Name, StringComparer.Ordinal))
			{
				object? value = field.GetValue(dice);

				if (value is IEnumerable sequence && value is not string)
				{
					foreach (object? item in sequence)
						text.Append(item).Append(',');
				}
				else
				{
					text.Append(value);
				}

				text.Append(';');
			}

			values["rng.state"] = Fnv(Encoding.ASCII.GetBytes(text.ToString())).ToString("x8");
		}

		public static uint Fnv(ReadOnlySpan<byte> content)
		{
			uint hash = 2166136261;

			foreach (byte item in content)
			{
				hash ^= item;
				hash *= 16777619;
			}

			return hash;
		}

		/// <summary>
		/// Turns an object graph into key = value lines. Our own classes are walked field by field (also the private fields,
		/// like the hit points in UnitHealth), arrays item by item. Arrays of numbers are written in chunks of 16 items
		/// ("key[0..15]=1,0,2,..."), so the baselines stay small. A class with GetLength(int) and an [x, y] indexer
		/// (ChartSightGrid) is written like a two dimensional array, so it matches the sbyte[,] it replaced.
		/// </summary>
		/// <summary>The field is the one behind a public auto-property of the type</summary>
		private static bool IsPublicPropertyField(Type kind, FieldInfo field)
		{
			string name = ObjectWalker.MemberName(field);
			return name != field.Name && kind.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) != null;
		}

		private static class ObjectWalker
		{
			/// <summary>
			/// The name of the field in the snapshot: the name of the property for the field behind an auto-property
			/// ("&lt;Gold&gt;k__BackingField" is "Gold"), so a public field turned into a property keeps its key
			/// </summary>
			public static string MemberName(FieldInfo field)
			{
				string name = field.Name;
				return name.StartsWith('<') && name.EndsWith(">k__BackingField", StringComparison.Ordinal) ? name[1..name.IndexOf('>')] : name;
			}

			private const int ChunkSpan = 16;

			public static void Walk(string key, object? value, IDictionary<string, string> values)
			{
				if (value == null)
					return;

				Type kind = value.GetType();

				if (kind.IsPrimitive || kind.IsEnum)
				{
					string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

					if (text != "0" && text != "False")
						values[key] = text;
				}
				else if (value is string words)
				{
					if (words.Length > 0)
						values[key] = words.Replace("\0", "\\0");
				}
				else if (value is Point place)
				{
					if (place.X != 0 || place.Y != 0)
						values[key] = $"{place.X},{place.Y}";
				}
				else if (value is Array numbers && numbers.Rank <= 2 && IsNumber(kind.GetElementType()!))
				{
					if (numbers.Rank == 1)
					{
						WalkNumbers(key, "", numbers.Length, idx => numbers.GetValue(idx), values);
					}
					else
					{
						for (int idx = 0; idx < numbers.GetLength(0); idx++)
						{
							int row = idx;
							WalkNumbers(key, $"{row},", numbers.GetLength(1), jdx => numbers.GetValue(row, jdx), values);
						}
					}
				}
				else if (value is Array array)
				{
					if (array.Rank == 1)
					{
						for (int idx = 0; idx < array.Length; idx++)
							Walk($"{key}[{idx}]", array.GetValue(idx), values);
					}
					else if (array.Rank == 2)
					{
						for (int idx = 0; idx < array.GetLength(0); idx++)
						{
							for (int jdx = 0; jdx < array.GetLength(1); jdx++)
								Walk($"{key}[{idx},{jdx}]", array.GetValue(idx, jdx), values);
						}
					}
					else
					{
						throw new NotSupportedException($"{key}: arrays with {array.Rank} dimensions");
					}
				}
				else if (TryWalkGrid(key, value, kind, values))
				{
				}
				else if (kind.Namespace != null && kind.Namespace.StartsWith("Civilization1Remaster", StringComparison.Ordinal))
				{
					foreach (FieldInfo field in kind.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						.OrderBy(MemberName, StringComparer.Ordinal))
					{
						Walk($"{key}.{MemberName(field)}", field.GetValue(value), values);
					}
				}
				else
				{
					throw new NotSupportedException($"{key}: type {kind.FullName} is not part of the snapshot format");
				}
			}

			private static bool IsNumber(Type kind)
			{
				return kind.IsPrimitive || kind.IsEnum;
			}

			/// <summary>One line for every 16 items, left out when they are all 0</summary>
			private static void WalkNumbers(string key, string prefix, int extent, Func<int, object?> item, IDictionary<string, string> values)
			{
				for (int begin = 0; begin < extent; begin += ChunkSpan)
				{
					int end = Math.Min(begin + ChunkSpan, extent) - 1;
					string[] texts = new string[end - begin + 1];
					bool anyContent = false;

					for (int idx = begin; idx <= end; idx++)
					{
						object? amount = item(idx);
						string text = amount is bool flag ? (flag ? "1" : "0") : Convert.ToString(amount, CultureInfo.InvariantCulture) ?? "0";
						texts[idx - begin] = text;
						anyContent |= text != "0";
					}

					if (anyContent)
						values[$"{key}[{prefix}{begin}..{end}]"] = string.Join(",", texts);
				}
			}

			private static bool TryWalkGrid(string key, object value, Type kind, IDictionary<string, string> values)
			{
				MethodInfo? extent = kind.GetMethod("GetLength", new[] { typeof(int) });
				PropertyInfo? item = kind.GetProperties()
					.FirstOrDefault(p => p.GetIndexParameters().Length == 2 && p.GetIndexParameters().All(i => i.ParameterType == typeof(int)));

				if (extent == null || item == null)
					return false;

				int breadth = (int)extent.Invoke(value, new object[] { 0 })!;
				int altitude = (int)extent.Invoke(value, new object[] { 1 })!;

				if (!IsNumber(item.PropertyType))
					throw new NotSupportedException($"{key}: grid of {item.PropertyType.FullName}");

				for (int idx = 0; idx < breadth; idx++)
				{
					int row = idx;
					WalkNumbers(key, $"{row},", altitude, jdx => item.GetValue(value, new object[] { row, jdx }), values);
				}

				return true;
			}
		}
	}
}
