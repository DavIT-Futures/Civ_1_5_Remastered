using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Civilization1Remaster.Tests.GoldenMaster;
using FrozenGame = Civilization1Original.Tests.HeadlessGame;
using FrozenSnapshot = Civilization1Original.Tests.GameSnapshot;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// The frozen game (tag migration-end, Civilization1Remaster.Original) and the current one, loaded from the same autosave
/// with the same random seed and played the same way. They are compared by the state of the game (StateOf): the fields of
/// GameState and of the engine, the data segment without the moved data, the map, the random generator, the stack
/// pointer, the screens and the moved data by meaning (MovedData). The calls and the registers are not compared: they may
/// change while the emulation goes away.
/// </summary>
public sealed class DualGame : IDisposable
{
	public DualGame(string gameFolder, int[]? script = null)
	{
		string saves = Path.Combine(GoldenMasterTests.SourceFolder(), "Saves");

		this.Frozen = new FrozenGame(gameFolder, saves, script);
		this.Current = new HeadlessGame(gameFolder, saves, script);
	}

	/// <summary>The game frozen at the end of the migration: the reference</summary>
	public FrozenGame Frozen { get; }

	/// <summary>The current game</summary>
	public HeadlessGame Current { get; }

	public void LoadAutosave(int bay, uint randomSeed)
	{
		this.Frozen.LoadAutosave(bay, randomSeed);
		this.Current.LoadAutosave(bay, randomSeed);
	}

	public void PlayTurn()
	{
		this.Frozen.PlayTurn();
		this.Current.PlayTurn();
	}

	/// <summary>The differences of the two games, null when they are the same</summary>
	public string? Compare(string label)
	{
		SortedDictionary<string, string> expected = StateOf(this.Frozen);
		SortedDictionary<string, string> actual = StateOf(this.Current);

		List<string> lines = expected.Keys.Union(actual.Keys).OrderBy(key => key, StringComparer.Ordinal)
			.Where(key => !expected.TryGetValue(key, out string? left) || !actual.TryGetValue(key, out string? right) || left != right)
			.Select(key => $"  {key}: {(expected.TryGetValue(key, out string? left) ? left : "(none)")} -> " +
				$"{(actual.TryGetValue(key, out string? right) ? right : "(none)")}")
			.ToList();

		if (lines.Count == 0)
			return null;

		StringBuilder text = new StringBuilder();
		text.AppendLine($"{label}: the current game differs from the frozen one ({lines.Count} values, frozen -> current):");

		foreach (string line in lines.Take(40))
			text.AppendLine(line);

		return text.ToString();
	}

	/// <summary>The state of the frozen game</summary>
	public static SortedDictionary<string, string> StateOf(FrozenGame game)
	{
		SortedDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.Ordinal);

		foreach (KeyValuePair<string, string> value in game.Capture().Values.Where(value => FrozenSnapshot.IsBehaviour(value.Key)))
			values[value.Key] = value.Value;

		foreach (var display in game.Engine.VideoCard.Displays)
			values[$"screen.{display.Token}"] = FrozenSnapshot.Fnv(display.Datum.PictureStorage).ToString("x8");

		MovedData.AddFrozen(game.Engine, values);

		return values;
	}

	/// <summary>The state of the current game</summary>
	public static SortedDictionary<string, string> StateOf(HeadlessGame game)
	{
		SortedDictionary<string, string> values = new SortedDictionary<string, string>(StringComparer.Ordinal);

		foreach (KeyValuePair<string, string> value in game.Capture().Values.Where(value => GameSnapshot.IsBehaviour(value.Key)))
			values[value.Key] = value.Value;

		foreach (var display in game.Engine.VideoCard.Displays)
			values[$"screen.{display.Token}"] = GameSnapshot.Fnv(display.Datum.PictureStorage).ToString("x8");

		MovedData.AddCurrent(game.Engine, values);

		return values;
	}

	public void Dispose()
	{
		this.Frozen.Dispose();
		this.Current.Dispose();
	}
}
