using System.Collections.Generic;
using System.Drawing;
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
	/// HealingRules in a loaded game (TurnEvents.MendFortifiedUnits without ClassicHealing): units placed in free slots of
	/// a player with a coastal city, with lost hit points, then one healing step
	/// </summary>
	public class HealingInGameTests
	{
		[Fact]
		public void UnitsHealByHealingRules()
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(2, GoldenMasterTests.RandomSeed);

			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicHealing = false;

			IMapLayers chart = engine.MapOps.Chart;
			UnitMemory troops = new UnitMemory(engine.Processor);
			bool IsOcean(int horizLoc, int vertLoc) => chart.Terrain(horizLoc, vertLoc) == MapRules.OceanTerrain;

			// A player with a city next to the ocean
			(int participantIdx, Point town, Point coast) = FindCoastalTown(engine, IsOcean);
			List<Point> ownTowns = engine.World.Towns.Where(t => t.ConditionMarker != 0xff && t.ParticipantIdx == participantIdx)
				.Select(t => t.Place).ToList();

			Point farOcean = Enumerable.Range(0, TradeRules.WorldHeight)
				.SelectMany(vertLoc => Enumerable.Range(0, TradeRules.WorldWidth).Select(horizLoc => new Point(horizLoc, vertLoc)))
				.First(tile => IsOcean(tile.X, tile.Y) && HealingRules.ComputeDistanceToNearestFriendlyCity(tile.X, tile.Y, ownTowns) > 3);

			int ship = KindOfGroup(engine, UnitKindTable.SeaUnits);
			int land = KindOfGroup(engine, UnitKindTable.LandUnits);
			int plane = KindOfGroup(engine, UnitKindTable.AirUnits);

			(string Name, int Kind, Point Place, UnitStatusEnum Status, bool Moved, int Healed)[] cases =
			[
				("ship in city, moved", ship, town, UnitStatusEnum.None, true, 2),
				("ship at the coast, sentry, moved", ship, coast, UnitStatusEnum.Sentry, true, 1),
				("ship at the coast, not moved", ship, coast, UnitStatusEnum.None, false, 1),
				("ship at the coast, moved", ship, coast, UnitStatusEnum.None, true, 0),
				("ship far from cities, sentry", ship, farOcean, UnitStatusEnum.Sentry, false, 0),
				("land unit in city", land, town, UnitStatusEnum.None, true, 2),
				("land unit fortified outside", land, coast, UnitStatusEnum.Fortified, false, 1),
				("land unit outside", land, coast, UnitStatusEnum.None, false, 0),
				("plane in city", plane, town, UnitStatusEnum.None, true, 2),
				("plane outside", plane, coast, UnitStatusEnum.Sentry, false, 0),
			];

			int[] slots = Enumerable.Range(0, UnitHealth.TroopTally)
				.Where(troopIdx => troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff).Take(cases.Length).ToArray();
			Assert.Equal(cases.Length, slots.Length);

			for (int idx = 0; idx < cases.Length; idx++)
			{
				var unit = cases[idx];
				int troopIdx = slots[idx];
				byte fullSteps = (byte)(3 * engine.Processor.PullGlobalWord((ushort)(UnitKindTable.Origin + UnitKindTable.Stride * unit.Kind + UnitKindTable.ShiftTally)));

				troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)unit.Kind);
				troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)unit.Status);
				troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)unit.Place.X);
				troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)unit.Place.Y);
				troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, unit.Moved ? (byte)0 : fullSteps);
				engine.World.Vitals.Assign(participantIdx, troopIdx, unit.Kind, 3, false);
			}

			engine.TurnHooks.MendFortifiedUnits();

			for (int idx = 0; idx < cases.Length; idx++)
			{
				var unit = cases[idx];
				int lost = engine.World.Vitals.ObtainLostHitPoints(participantIdx, slots[idx], unit.Kind);

				Assert.True(3 - lost == unit.Healed, $"{unit.Name}: healed {3 - lost}, expected {unit.Healed}");
			}
		}

		private static (int ParticipantIdx, Point Town, Point Coast) FindCoastalTown(GameEngine engine, System.Func<int, int, bool> isOcean)
		{
			foreach (City town in engine.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIdx < 0 || town.ParticipantIdx >= UnitHealth.ParticipantTally)
					continue;

				for (int vertShift = -1; vertShift <= 1; vertShift++)
				{
					for (int horizShift = -1; horizShift <= 1; horizShift++)
					{
						int horizLoc = town.Place.X + horizShift, vertLoc = town.Place.Y + vertShift;

						if (vertLoc > 0 && vertLoc < TradeRules.WorldHeight - 1 && horizLoc > 0 && horizLoc < TradeRules.WorldWidth - 1 &&
							isOcean(horizLoc, vertLoc))
							return (town.ParticipantIdx, town.Place, new Point(horizLoc, vertLoc));
					}
				}
			}

			Assert.Fail("No coastal city in the saved game");
			return default;
		}

		private static int KindOfGroup(GameEngine engine, int landGroup)
		{
			return Enumerable.Range(0, 28).First(kind =>
				engine.Processor.PullGlobalWord((ushort)(UnitKindTable.Origin + UnitKindTable.Stride * kind + UnitKindTable.LandGroup)) == landGroup);
		}
	}
}
