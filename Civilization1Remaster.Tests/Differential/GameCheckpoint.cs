using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Tests.GoldenMaster;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The state of a headless game to go back to (in milliseconds; loading the autosave again takes over a second): the
	/// game state (deep copy), the whole memory of the machine, the registers and the pictures of the screens (the map is in
	/// them). Scenarios that change the state then start from the same game instead of from what the scenarios before
	/// them left behind. Engine fields outside these are not restored.
	/// </summary>
	public sealed class GameCheckpoint
	{
		private readonly object world;
		private readonly byte[] memory;
		private readonly byte[][] overlays;
		private readonly ushort[] registers;
		private readonly Dictionary<int, byte[]> screens;

		public GameCheckpoint(HeadlessGame game)
		{
			GameEngine engine = game.Engine;
			CPU processor = engine.Processor;

			this.world = Clone(engine.World);
			this.memory = (byte[])processor.Storage.StoragePayload.Clone();
			// The data kept in C# (B6: the message buffer, the units, the tables, the globals), not in the memory block
			this.overlays = processor.Storage.Overlays
				.Select(range => Enumerable.Range(0, range.Overlay.Length).Select(place => range.Overlay.Read(place)).ToArray()).ToArray();
			this.registers = Registers(processor).Select(register => register.Value16).ToArray();
			this.screens = engine.VideoCard.Displays.ToDictionary(display => display.Token, display => (byte[])display.Datum.PictureStorage.Clone());
		}

		/// <summary>Puts the game back to the checkpoint</summary>
		public void Restore(HeadlessGame game)
		{
			GameEngine engine = game.Engine;
			CPU processor = engine.Processor;

			CopyInto(this.world, engine.World);
			Buffer.BlockCopy(this.memory, 0, processor.Storage.StoragePayload, 0, this.memory.Length);
			for (int idx = 0; idx < this.overlays.Length; idx++)
			{
				for (int place = 0; place < this.overlays[idx].Length; place++)
					processor.Storage.Overlays[idx].Overlay.Write(place, this.overlays[idx][place]);
			}

			CPURegister[] live = Registers(processor);

			for (int idx = 0; idx < live.Length; idx++)
				live[idx].Value16 = this.registers[idx];

			foreach (var display in engine.VideoCard.Displays)
			{
				if (this.screens.TryGetValue(display.Token, out byte[]? picture))
					Buffer.BlockCopy(picture, 0, display.Datum.PictureStorage, 0, picture.Length);
			}

			lock (engine.VideoCard.Tokens)
				engine.VideoCard.Tokens.Clear();
		}

		private static CPURegister[] Registers(CPU processor) =>
		[
			processor.RegAcc, processor.RegBase, processor.RegCount, processor.RegData, processor.RegSource, processor.RegDest,
			processor.RegFrame, processor.RegStack,
		];

		private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, FieldInfo[]> FieldCache = new();
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, bool> PlainCache = new();

		private static FieldInfo[] Fields(Type type) => FieldCache.GetOrAdd(type, key =>
		{
			List<FieldInfo> fields = [];

			for (Type? level = key; level != null && level != typeof(object); level = level.BaseType)
				fields.AddRange(level.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

			return fields.ToArray();
		});

		private static bool IsPlain(Type type) => PlainCache.GetOrAdd(type, key => key.IsPrimitive || key.IsEnum || key == typeof(string) ||
			key == typeof(decimal) || (key.IsValueType && Fields(key).All(field => IsPlain(field.FieldType))));

		/// <summary>A deep copy (value types and strings as they are, arrays and objects copied)</summary>
		private static object? Clone(object? source)
		{
			if (source == null || IsPlain(source.GetType()))
				return source;

			if (source is Array array)
			{
				Array copy = (Array)array.Clone();

				if (!IsPlain(array.GetType().GetElementType()!))
				{
					foreach (int[] index in Indices(array))
						copy.SetValue(Clone(array.GetValue(index)), index);
				}

				return copy;
			}

			object target = RuntimeHelpers.GetUninitializedObject(source.GetType());

			foreach (FieldInfo field in Fields(source.GetType()))
				field.SetValue(target, Clone(field.GetValue(source)));

			return target;
		}

		/// <summary>Copies the state into the live objects, keeping their identity (others may hold them)</summary>
		private static void CopyInto(object source, object target)
		{
			foreach (FieldInfo field in Fields(source.GetType()))
			{
				object? from = field.GetValue(source);
				object? to = field.GetValue(target);

				if (from == null || to == null || IsPlain(field.FieldType) || from.GetType() != to.GetType())
				{
					field.SetValue(target, Clone(from));
				}
				else if (from is Array fromArray && to is Array toArray && SameShape(fromArray, toArray))
				{
					if (IsPlain(fromArray.GetType().GetElementType()!))
						Array.Copy(fromArray, toArray, fromArray.Length);
					else
					{
						foreach (int[] index in Indices(fromArray))
						{
							object? element = fromArray.GetValue(index);
							object? live = toArray.GetValue(index);

							if (element == null || live == null || element.GetType() != live.GetType() || IsPlain(element.GetType()))
								toArray.SetValue(Clone(element), index);
							else
								CopyInto(element, live);
						}
					}
				}
				else if (from is Array)
				{
					field.SetValue(target, Clone(from));
				}
				else
				{
					CopyInto(from, to);
				}
			}
		}

		private static bool SameShape(Array left, Array right) =>
			left.Rank == right.Rank && Enumerable.Range(0, left.Rank).All(dimension => left.GetLength(dimension) == right.GetLength(dimension));

		private static IEnumerable<int[]> Indices(Array array)
		{
			if (array.Length == 0)
				yield break;

			int[] index = new int[array.Rank];

			while (true)
			{
				yield return (int[])index.Clone();

				int dimension = array.Rank - 1;

				while (dimension >= 0 && ++index[dimension] >= array.GetLength(dimension))
				{
					index[dimension] = 0;
					dimension--;
				}

				if (dimension < 0)
					yield break;
			}
		}
	}
}
