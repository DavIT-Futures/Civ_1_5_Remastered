using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Civilization1Remaster.Tests.GoldenMaster
{
	/// <summary>
	/// Baseline file of one golden master run: the snapshot after loading (turn 0) and after every played turn.
	/// The first snapshot is written whole, the next ones only with the keys that changed ("-key" when a key went back
	/// to the default value), so the files stay small and a diff of two baselines is readable.
	/// </summary>
	public sealed class GoldenMasterBaseline
	{
		private const string TurnMarker = "turn ";

		public GoldenMasterBaseline(string header, IReadOnlyList<GameSnapshot> turns)
		{
			this.Header = header;
			this.Turns = turns;
		}

		/// <summary>The comment lines at the start of the file, without '#'</summary>
		public string Header { get; }

		public IReadOnlyList<GameSnapshot> Turns { get; }

		public static GoldenMasterBaseline Read(string route)
		{
			StringBuilder header = new StringBuilder();
			List<GameSnapshot> turns = new List<GameSnapshot>();
			GameSnapshot? present = null;

			foreach (string line in File.ReadLines(route))
			{
				if (line.Length == 0)
					continue;

				if (line.StartsWith('#'))
				{
					header.AppendLine(line.Substring(1).Trim());
				}
				else if (line.StartsWith(TurnMarker, StringComparison.Ordinal))
				{
					GameSnapshot following = new GameSnapshot();

					if (present != null)
					{
						foreach (KeyValuePair<string, string> item in present.Values)
							following.Values[item.Key] = item.Value;
					}

					// call. and input. describe one turn only, they don't carry over
					foreach (string key in following.Values.Keys.Where(k => !GameSnapshot.IsBehaviour(k)).ToList())
						following.Values.Remove(key);

					turns.Add(following);
					present = following;
				}
				else if (present == null)
				{
					throw new InvalidDataException($"{route}: value before the first turn");
				}
				else if (line.StartsWith('-'))
				{
					present.Values.Remove(line.Substring(1));
				}
				else
				{
					int split = line.IndexOf('=');

					if (split < 0)
						throw new InvalidDataException($"{route}: line without '=': {line}");

					present.Values[line.Substring(0, split)] = line.Substring(split + 1);
				}
			}

			return new GoldenMasterBaseline(header.ToString().TrimEnd(), turns);
		}

		public void Write(string route)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(route)!);

			using StreamWriter sink = new StreamWriter(route, false, new UTF8Encoding(false)) { NewLine = "\n" };

			foreach (string line in this.Header.Split('\n'))
				sink.WriteLine($"# {line.TrimEnd('\r')}");

			IReadOnlyDictionary<string, string> previous = new Dictionary<string, string>();

			for (int turnIdx = 0; turnIdx < this.Turns.Count; turnIdx++)
			{
				SortedDictionary<string, string> present = this.Turns[turnIdx].Values;

				sink.WriteLine($"{TurnMarker}{turnIdx}");

				foreach (string key in previous.Keys.Where(k => GameSnapshot.IsBehaviour(k) && !present.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal))
					sink.WriteLine($"-{key}");

				foreach (KeyValuePair<string, string> item in present)
				{
					bool carried = GameSnapshot.IsBehaviour(item.Key);

					if (!carried || !previous.TryGetValue(item.Key, out string? before) || before != item.Value)
						sink.WriteLine($"{item.Key}={item.Value}");
				}

				previous = present;
			}
		}
	}
}
