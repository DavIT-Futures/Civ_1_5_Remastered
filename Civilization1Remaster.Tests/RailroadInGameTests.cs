using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The remaster's railroads in a loaded game (PlayerTurn.RideForFree, without ClassicRailroads): a step out of a city
	/// is free once the player knows Railroad; the human player's unit moved by hand rides for ever, a unit with GoTo or of
	/// a computer player 30 times a turn
	/// </summary>
	public class RailroadInGameTests
	{
		[Fact]
		public void TrainsRideThroughCities()
		{
			using HeadlessGame game = Load(out GameEngine engine);
			short person = engine.World.PersonParticipantIdx;
			City town = engine.World.Towns.First(t => t.ConditionMarker != 0xff && t.ParticipantIdx == person);
			short troop = FirstUnit(engine, person);
			UnitMemory troops = new UnitMemory(engine.Processor);
			troops.Assign(person, troop, UnitMemory.TravelHoriz, 0xff);

			KnowRailroad(engine, person, false);
			Assert.False(engine.TurnLogic.RideForFree(person, troop, (short)town.Place.X, (short)town.Place.Y));

			KnowRailroad(engine, person, true);

			// By hand: free every time
			for (int step = 0; step < 40; step++)
				Assert.True(engine.TurnLogic.RideForFree(person, troop, (short)town.Place.X, (short)town.Place.Y));
		}

		[Fact]
		public void GoToAndComputerPlayersRideForFreeThirtyTimesATurn()
		{
			using HeadlessGame game = Load(out GameEngine engine);
			short person = engine.World.PersonParticipantIdx;
			UnitMemory troops = new UnitMemory(engine.Processor);

			// The human player's unit with GoTo
			City own = engine.World.Towns.First(t => t.ConditionMarker != 0xff && t.ParticipantIdx == person);
			short troop = FirstUnit(engine, person);
			troops.Assign(person, troop, UnitMemory.TravelHoriz, 10);
			KnowRailroad(engine, person, true);

			for (int step = 0; step < RailroadRules.MaxFreeStepsPerTurn; step++)
				Assert.True(engine.TurnLogic.RideForFree(person, troop, (short)own.Place.X, (short)own.Place.Y));

			Assert.False(engine.TurnLogic.RideForFree(person, troop, (short)own.Place.X, (short)own.Place.Y));

			// A computer player's unit: counted on its own
			City foreign = engine.World.Towns.First(t => t.ConditionMarker != 0xff && t.ParticipantIdx != person && t.ParticipantIdx != 0);
			short player = (short)foreign.ParticipantIdx;
			short computerTroop = FirstUnit(engine, player);
			KnowRailroad(engine, player, true);

			for (int step = 0; step < RailroadRules.MaxFreeStepsPerTurn; step++)
				Assert.True(engine.TurnLogic.RideForFree(player, computerTroop, (short)foreign.Place.X, (short)foreign.Place.Y));

			Assert.False(engine.TurnLogic.RideForFree(player, computerTroop, (short)foreign.Place.X, (short)foreign.Place.Y));

			// The next turn the count starts again
			engine.World.RoundTally++;
			Assert.True(engine.TurnLogic.RideForFree(player, computerTroop, (short)foreign.Place.X, (short)foreign.Place.Y));
		}

		[Fact]
		public void TurnsWithRailroadsEverywhereEnd()
		{
			// Every player knows Railroad and the land around the cities has railroads: the units of the computer players
			// ride for free (at most 30 times a turn), the turns still end
			using HeadlessGame game = Load(out GameEngine engine);
			IMapLayers chart = engine.MapOps.Chart;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				KnowRailroad(engine, participantIdx, true);

			foreach (City town in engine.World.Towns.Where(t => t.ConditionMarker != 0xff))
			{
				for (int horiz = town.Place.X - 3; horiz <= town.Place.X + 3; horiz++)
				{
					for (int vert = System.Math.Max(1, town.Place.Y - 3); vert <= System.Math.Min(48, town.Place.Y + 3); vert++)
					{
						int at = (horiz + 80) % 80;

						if (chart.Terrain(at, vert) == MapRules.OceanTerrain)
							continue;

						engine.VideoCard.AssignDot(2, at, vert + 100, (byte)(chart.LowImprovements(at, vert) | RailroadRules.RoadBit), 0);
						engine.VideoCard.AssignDot(2, at, vert + 150, (byte)(chart.HighImprovements(at, vert) | (RailroadRules.RailroadBit >> 4)), 0);
					}
				}
			}

			for (int turn = 0; turn < 3; turn++)
			{
				game.Deadline = System.Environment.TickCount64 + 120_000;
				game.PlayTurn();
			}

			game.Deadline = 0;

			// The computer players did ride for free (the counts of the last turn)
			var rides = ((int Round, System.Collections.Generic.Dictionary<(short, short), int> Steps))typeof(PlayerTurn)
				.GetField("freeRides", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.GetValue(engine.TurnLogic)!;
			Assert.NotEmpty(rides.Steps);
		}

		private static HeadlessGame Load(out GameEngine engine)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
			engine = game.Engine;
			engine.TurnHooks.ClassicRailroads = false;
			return game;
		}

		private static short FirstUnit(GameEngine engine, short participantIdx)
		{
			UnitMemory troops = new UnitMemory(engine.Processor);
			return (short)Enumerable.Range(0, UnitMemory.TroopTally).First(troop => troops.Pull(participantIdx, troop, UnitMemory.Kind) != 0xff);
		}

		private static void KnowRailroad(GameEngine engine, short participantIdx, bool known)
		{
			ushort bit = (ushort)(1 << (RailroadRules.RailroadTechnology & 0xf));
			ushort[] markers = engine.World.Participants[participantIdx].FoundScienceMarkers;
			markers[RailroadRules.RailroadTechnology >> 4] = (ushort)(known ? markers[RailroadRules.RailroadTechnology >> 4] | bit :
				markers[RailroadRules.RailroadTechnology >> 4] & ~bit);
		}
	}
}
