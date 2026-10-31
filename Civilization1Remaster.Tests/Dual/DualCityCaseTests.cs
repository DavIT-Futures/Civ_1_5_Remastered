using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Tests.Differential;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;
using CityCase = Civilization1Remaster.Tests.Dual.HandleCityCaseTables.CityCase;
using Choice = Civilization1Remaster.Tests.Dual.HandleCityCaseTables.Choice;
using FrozenCheckpoint = Civilization1Original.Tests.GameCheckpoint;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// The tables of cases of HandleCity (HandleCityCaseTables) on the frozen game and on the current one:
/// every case starts both games from the checkpoint after the load, picks its city, changes the state the same way in
/// both (the setups are dynamic, the two games have the same members) and calls HandleCity in each. Then the states
/// (DualGame.Compare), AX (what HandleCity gives back) and the failures must match.
/// </summary>
public sealed class DualCityCaseTests(ITestOutputHelper output)
{
	/// <summary>The longest one case may run in one game (an endless loop fails the case)</summary>
	public const int RunSeconds = 120;

	public static TheoryData<string> Tables() => new TheoryData<string>(TablesByName.Keys);

	private static readonly Dictionary<string, CityCase[]> TablesByName = new Dictionary<string, CityCase[]>(StringComparer.Ordinal)
	{
		["Turn"] = HandleCityCaseTables.TurnCases,
		["Trade"] = HandleCityCaseTables.TradeCases,
		["Split"] = HandleCityCaseTables.SplitCases,
		["Happiness"] = HandleCityCaseTables.HappinessCases,
		["Support"] = HandleCityCaseTables.SupportCases,
		["List"] = HandleCityCaseTables.ListCases,
		["Box"] = HandleCityCaseTables.BoxCases,
		["Row"] = HandleCityCaseTables.RowCases,
		["Shortfall"] = HandleCityCaseTables.ShortfallCases,
		["Button"] = HandleCityCaseTables.ButtonCases,
		["Purchase"] = HandleCityCaseTables.PurchaseCases,
		["Key"] = HandleCityCaseTables.KeyCases,
		["ListClick"] = HandleCityCaseTables.ListCases2,
		["Click"] = HandleCityCaseTables.ClickCases,
		["Order"] = HandleCityCaseTables.OrderCases,
		["Celebration"] = HandleCityCaseTables.CelebrationCases,
		["Science"] = HandleCityCaseTables.ScienceCases,
		["Pollution"] = HandleCityCaseTables.PollutionCases,
		["Upkeep"] = HandleCityCaseTables.UpkeepCases,
	};

	[Theory]
	[MemberData(nameof(Tables))]
	public void HandleCityCasesMatchFrozenGame(string table)
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		List<string> differences = [];

		foreach (int bay in new[] { 5, 9 })
			differences.AddRange(Check(gameFolder!, bay, TablesByName[table]));

		foreach (string difference in differences)
			output.WriteLine(difference);

		Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences.Take(3)));
	}

	/// <summary>Runs the cases on one saved game; the differences (at most one per case)</summary>
	public static List<string> Check(string gameFolder, int bay, CityCase[] cases)
	{
		List<string> differences = [];

		using DualGame games = new DualGame(gameFolder);
		games.LoadAutosave(bay, GoldenMasterTests.RandomSeed);

		FrozenCheckpoint frozenPoint = new FrozenCheckpoint(games.Frozen);
		GameCheckpoint currentPoint = new GameCheckpoint(games.Current);

		foreach (CityCase current in cases)
		{
			frozenPoint.Restore(games.Frozen);
			currentPoint.Restore(games.Current);

			short townIdx = PickCity(games.Current.Engine, current);

			if (townIdx < 0)
				continue;

			string label = $"AUTO{bay:00} {current.Name}: HandleCity({townIdx}, {current.Marker})";

			string? frozenFailure = Run(games.Frozen.Engine, current, townIdx, deadline => games.Frozen.Deadline = deadline);
			string? currentFailure = Run(games.Current.Engine, current, townIdx, deadline => games.Current.Deadline = deadline);

			string? difference = games.Compare(label);

			if (difference == null && frozenFailure != currentFailure)
				difference = $"{label}: the frozen game {frozenFailure ?? "finished"}, the current one {currentFailure ?? "finished"}";

			ushort frozenAx = games.Frozen.Engine.Processor.RegAcc.Value16;
			ushort currentAx = games.Current.Engine.Processor.RegAcc.Value16;

			// (a city gone in its turn leaves what the last function put in AX, which nobody reads: HandleAllCities asks for
			// the result only of a city above size 5)
			bool gone = games.Current.Engine.World.Towns[townIdx].ConditionMarker == 0xff;

			if (difference == null && frozenAx != currentAx && !gone)
				difference = $"{label}: AX 0x{frozenAx:x4} in the frozen game, 0x{currentAx:x4} in the current one";

			if (difference != null)
				differences.Add(difference);

			// A setup that fails the same way in both games would hide the case
			if (frozenFailure != null && frozenFailure.StartsWith("RuntimeBinderException", StringComparison.Ordinal))
				differences.Add($"{label}: the setup fails: {frozenFailure}");
		}

		return differences;
	}

	/// <summary>The setup of the case and HandleCity in one game (dynamic: the frozen game or the current one); the failure, null when it finished</summary>
	private static string? Run(dynamic engine, CityCase current, short townIdx, Action<long> setDeadline)
	{
		ushort stack = engine.Processor.RegStack.Value16;
		ushort frame = engine.Processor.RegFrame.Value16;

		try
		{
			HumanTurnScript.Install((object)engine, [new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);
			current.Setup(engine, townIdx);
			setDeadline(Environment.TickCount64 + RunSeconds * 1000L);
			engine.CityOps.HandleCity(townIdx, current.Marker);
			return null;
		}
		catch (Exception failure)
		{
			// The exception leaves the frames of the functions it left on the emulated stack, as many as the version has (B3
			// removes frames and saved registers): the pointers go back, so the next cases start at the same depth
			engine.Processor.RegStack.Value16 = stack;
			engine.Processor.RegFrame.Value16 = frame;

			// (without the temporary game folder, which differs between the two games)
			return $"{failure.GetType().Name}: {System.Text.RegularExpressions.Regex.Replace(failure.Message, @"Civ1GoldenMaster[\\/][0-9a-f]{32}", "<game folder>")}";
		}
		finally
		{
			setDeadline(0);
		}
	}

	/// <summary>The city of the case (as HandleCityCaseTables.CheckCases picks it), -1 when none fits</summary>
	private static short PickCity(GameEngine engine, CityCase current)
	{
		short person = engine.World.PersonParticipantIdx;
		List<int> fitting = Enumerable.Range(0, 128).Where(town => engine.World.Towns[town].ConditionMarker != 0xff &&
			(engine.World.Towns[town].ParticipantIdx == person) == current.Human && engine.World.Towns[town].ParticipantIdx != 0 &&
			(current.Human || (engine.World.ParticipantMarkers & (1 << engine.World.Towns[town].ParticipantIdx)) == 0)).ToList();

		if (fitting.Count == 0)
			return -1;

		return (short)(current.Pick switch
		{
			Choice.Last => fitting[^1],
			Choice.Biggest => fitting.OrderByDescending(town => engine.World.Towns[town].TrueExtent).ThenBy(town => town).First(),
			_ => fitting[0],
		});
	}
}
