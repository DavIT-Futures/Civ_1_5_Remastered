using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// Whole turns of all players from every autosave, in the frozen game and in the current one: after the load and after
/// every turn both must be in the same state. Like the golden master, but against the frozen game instead of a recorded
/// baseline, so it keeps working when data leaves the emulated memory.
/// </summary>
/// <remarks>Every autosave has its own small class, so xUnit runs them in parallel.</remarks>
public abstract class DualTurnTests(int bay, ITestOutputHelper output)
{
	/// <summary>Turns played from every autosave</summary>
	public const int TurnTally = 5;

	[Fact]
	public void PlayedTurnsMatchFrozenGame()
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		using DualGame games = new DualGame(gameFolder!);
		games.LoadAutosave(bay, GoldenMasterTests.RandomSeed);
		string? difference = games.Compare($"AUTO{bay:00} after loading");

		for (int turnIdx = 1; difference == null && turnIdx <= TurnTally; turnIdx++)
		{
			games.PlayTurn();
			difference = games.Compare($"AUTO{bay:00} turn {turnIdx}");

			if (games.Frozen.GameEndFlag != 0)
				break;
		}

		if (difference != null)
			output.WriteLine(difference);

		Assert.True(difference == null, difference);
	}
}

public class DualTurnsAuto00(ITestOutputHelper output) : DualTurnTests(0, output);
public class DualTurnsAuto01(ITestOutputHelper output) : DualTurnTests(1, output);
public class DualTurnsAuto02(ITestOutputHelper output) : DualTurnTests(2, output);
public class DualTurnsAuto03(ITestOutputHelper output) : DualTurnTests(3, output);
public class DualTurnsAuto04(ITestOutputHelper output) : DualTurnTests(4, output);
public class DualTurnsAuto05(ITestOutputHelper output) : DualTurnTests(5, output);
public class DualTurnsAuto06(ITestOutputHelper output) : DualTurnTests(6, output);
public class DualTurnsAuto07(ITestOutputHelper output) : DualTurnTests(7, output);
public class DualTurnsAuto08(ITestOutputHelper output) : DualTurnTests(8, output);
public class DualTurnsAuto09(ITestOutputHelper output) : DualTurnTests(9, output);
public class DualTurnsAuto10(ITestOutputHelper output) : DualTurnTests(10, output);
