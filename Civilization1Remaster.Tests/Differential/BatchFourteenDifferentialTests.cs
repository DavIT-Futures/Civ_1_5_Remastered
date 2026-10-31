using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The nuclear fallout, the exchange of technologies, the space background and the menu bar against their original code
	/// </summary>
	public class BatchFourteenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void EnactNuclearFalloutMatchesOriginalCode()
		{
			Check(3, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				(int horizLoc, int vertLoc) = RandomTile(engine, chance);

				// Mostly a city or a tile next to one
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();

				if (chance.Next(3) != 0)
				{
					var town = engine.World.Towns[towns[chance.Next(towns.Length)]];
					horizLoc = (town.Place.X + chance.Next(-1, 2) + 80) % 80;
					vertLoc = town.Place.Y + chance.Next(-1, 2);
				}

				int cities = Calls(pair.Rewritten.Engine, pair, "LocateCityAtPosition");
				int smog = Calls(pair.Rewritten.Engine, pair, "AttachSmog");

				return new Scenario($"EnactNuclearFallout({horizLoc}, {vertLoc})", game => { },
					game => new BattlesReference(game).EnactNuclearFallout((short)horizLoc, (short)vertLoc),
					game => game.Combat.EnactNuclearFallout((short)horizLoc, (short)vertLoc),
					Register.Kept,
					game => (Calls(game, pair, "LocateCityAtPosition") > cities ? "city hit" : "no city") +
						(Calls(game, pair, "AttachSmog") > smog ? ", pollution" : ""));
			}, output, ["city hit, pollution", "no city, pollution"]);
		}

		[Fact]
		public void SwapTechnologyMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				short first = (short)chance.Next(8), second = (short)chance.Next(8);
				int flip = chance.Next(3) == 0 ? chance.Next(0x48) : -1;
				bool exchanged = false;

				return new Scenario($"SwapTechnology({first}, {second}), technology {flip} of the first player changed",
					game =>
					{
						if (flip >= 0)
							game.World.Participants[first].FoundScienceMarkers[flip >> 4] ^= (ushort)(1 << (flip & 0xf));
					},
					game => new ForeignAffairsReference(game).SwapTechnology(first, second),
					game =>
					{
						ushort[] before = (ushort[])game.World.Participants[first].FoundScienceMarkers.Clone();
						game.Politics.SwapTechnology(first, second);
						exchanged = !before.SequenceEqual(game.World.Participants[first].FoundScienceMarkers);
					},
					// (no AX: the result nobody read was dropped in phase 3)
					Register.Kept, game => exchanged ? "exchanged" : "nothing to exchange");
			}, output, ["exchanged", "nothing to exchange"]);
		}

		/// <summary>
		/// The colour cycles (segment 0x35cf, 0x66 bytes each from 0x1c) without their tick counters, which the timer interrupt
		/// counts down: when it comes depends on the number of memory accesses, which is smaller in the rewritten code
		/// </summary>
		private static readonly (ushort, ushort, int)[] HueCyclesWithoutCounters = Enumerable.Range(0, 6)
			.Select(cycleIdx => ((ushort)0x35cf, (ushort)(cycleIdx == 0 ? 0 : 0x1e + (0x66 * (cycleIdx - 1))),
				cycleIdx == 0 ? 0x1c : cycleIdx < 5 ? 0x64 : 0x200 - (0x1e + (0x66 * 4))))
			.ToArray();

		[Fact]
		public void RenderSpaceSceneryMatchesOriginalCode()
		{
			Check(10, 20, (chance, pair) =>
			{
				ushort cycling = (ushort)chance.Next(2);

				return new Scenario($"RenderSpaceScenery(), colour cycles {cycling}",
					game => game.Processor.EmitGlobalWord(0xd762, cycling),
					game => new SpaceshipBayReference(game).DrawSpaceScenery(),
					game => game.RocketScreen.DrawSpaceScenery(),
					Register.Kept, game => cycling != 0 ? "cycles started" : "no cycles", HueCyclesWithoutCounters);
			}, output, ["cycles started", "no cycles"]);
		}

		[Fact]
		public void ManageMenuBarMatchesOriginalCode()
		{
			// Escape closes a menu, Enter takes its first line (and Escape leaves what it opened)
			int[] keys = { 0x1b, 0x1b, 0x0d, 0x1b, 0x1b, 0x1b, 0x5000, 0x0d, 0x1b, 0x1b, 0x1b, 0x1b };

			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short marker = (short)chance.Next(-1, 5);
				short column = (short)chance.Next(320);
				(short participantIdx, short troopIdx) = RandomUnit(engine, chance, engine.World.PersonParticipantIdx) ?? (engine.World.PersonParticipantIdx, 0);

				return new Scenario($"ManageMenuBar({participantIdx}, {troopIdx}, {marker}), mouse column {column}",
					game => game.GlobDb3c = (ushort)column,
					game => new MenuBarReference(game).ManageMenuBar(participantIdx, troopIdx, marker),
					game => game.MenuSystem.ManageMenuBar(participantIdx, troopIdx, marker),
					Register.Kept,
					game => (short)game.Processor.PullGlobalWord(0x654a) switch
					{
						0 => "screen put back",
						1 => "map drawn again",
						_ => "other",
					});
			}, output, ["screen put back", "map drawn again"], keys);
		}
	}
}
