using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// ShiftUnit against its original code: units of every player come onto random tiles, half of them next to (1 or 2 tiles)
	/// a foreign unit, with and without repainting. Units meet, cities give back kept units, peace is offered.
	/// </summary>
	public abstract class ShiftUnitDifferentialTests(int bay, ITestOutputHelper output)
	{
		[Fact]
		public void MatchesOriginalCode()
		{
			Check(bay, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				(int horizLoc, int vertLoc) = RandomTile(engine, chance);

				if (chance.Next(2) == 0 && RandomUnit(engine, chance, where: (p, t) => true) is (short otherIdx, short otherTroopIdx) &&
					otherIdx != participantIdx)
				{
					UnitMemory troops = new UnitMemory(engine.Processor);
					int heading = chance.Next(1, 25);
					horizLoc = (troops.Pull(otherIdx, otherTroopIdx, UnitMemory.Horiz) + engine.MapOps.Chart.DirectionHoriz(heading) + 80) % 80;
					vertLoc = Math.Clamp(troops.Pull(otherIdx, otherTroopIdx, UnitMemory.Vert) + engine.MapOps.Chart.DirectionVert(heading), 1, 48);
				}

				ushort marker = (ushort)chance.Next(2);
				int meetings = Calls(pair.Rewritten.Engine, pair, "VisitEachUnitInStack");
				int treaties = Calls(pair.Rewritten.Engine, pair, "BargainPeaceTreaty");
				int keptUnits = Calls(pair.Rewritten.Engine, pair, "ConstructUnitsFromCityList");
				int strategies = Calls(pair.Rewritten.Engine, pair, "SendUnitToStrategicLocation");

				return new Scenario($"ShiftUnit({horizLoc}, {vertLoc}, {participantIdx}, {troopIdx}, {marker})",
					game => { },
					game => new UnitManagementReference(game).ShiftUnit(horizLoc, vertLoc, participantIdx, troopIdx, marker),
					game => game.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, troopIdx, marker),
					Register.Kept,
					game => Calls(game, pair, "BargainPeaceTreaty") > treaties ? "peace offer" :
						Calls(game, pair, "ConstructUnitsFromCityList") > keptUnits ? "foreign city" :
						Calls(game, pair, "VisitEachUnitInStack") > meetings ? "unit met" :
						Calls(game, pair, "SendUnitToStrategicLocation") > strategies ? "strategic location" : "alone");
			}, output, ["peace offer", "foreign city", "unit met", "alone"]);
		}
	}

	public class ShiftUnitDifferentialAuto03(ITestOutputHelper output) : ShiftUnitDifferentialTests(3, output);
	public class ShiftUnitDifferentialAuto10(ITestOutputHelper output) : ShiftUnitDifferentialTests(10, output);

	/// <summary>
	/// DisplayBattleScreen against its original code: stacks and single units, the cursor keys, Enter, Space and Escape,
	/// and the mouse mode (0x1a3c)
	/// </summary>
	public abstract class DisplayBattleScreenDifferentialTests(int bay, ITestOutputHelper output)
	{
		private static readonly int[] Keys = { 0x5000, 0x5000, 0x4800, 0x0d, 0x5000, 0x20, 0x4800, 0x4800, 0x1b, 0x0d, 0x5000, 0x1b };

		[Fact]
		public void MatchesOriginalCode()
		{
			Check(bay, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				bool stacked = chance.Next(4) != 0;

				if (RandomUnit(engine, chance, where: (p, t) => !stacked || troops.Following(p, t) != -1) is not (short participantIdx, short troopIdx))
					return null;

				ushort mouse = (ushort)(chance.Next(5) == 0 ? 1 : 0);

				return new Scenario($"DisplayBattleScreen({participantIdx}, {troopIdx}), mouse {mouse}",
					game => game.Processor.EmitGlobalWord(0x1a3c, mouse),
					game => new UnitManagementReference(game).DisplayBattleScreen(participantIdx, troopIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.UnitOps.DisplayBattleScreen(participantIdx, troopIdx)),
					Register.Acc | Register.Kept,
					game => mouse != 0 ? "mouse" : troops.Following(participantIdx, troopIdx) != -1 ? "stack" : "single unit");
			}, output, ["mouse", "stack", "single unit"], Keys);
		}
	}

	public class DisplayBattleScreenDifferentialAuto03(ITestOutputHelper output) : DisplayBattleScreenDifferentialTests(3, output);
	public class DisplayBattleScreenDifferentialAuto10(ITestOutputHelper output) : DisplayBattleScreenDifferentialTests(10, output);

	/// <summary>
	/// The way home of planes (LocateNearestFriendlyCity) and the stack steps EliminateSeaUnit and RestoreUnitOrders against
	/// their original code, for units of every kind
	/// </summary>
	public class UnitStepDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void LocateNearestFriendlyCityMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				// Without an own city or carrier the original goes to whatever is left on the stack, which differs between the games
				bool home = engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx) ||
					Enumerable.Range(0, UnitMemory.TroopTally).Any(idx => troops.Pull(participantIdx, idx, UnitMemory.Kind) == UnitKindTable.Carrier);

				if (!home)
					return null;

				return new Scenario($"LocateNearestFriendlyCity({participantIdx}, {troopIdx})",
					game => { },
					game => new UnitManagementReference(game).LocateNearestFriendlyCity(participantIdx, troopIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.UnitOps.LocateNearestFriendlyCity(participantIdx, troopIdx)),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 == 0 ? "no step" : "step");
			}, output, ["no step", "step"]);
		}

		[Fact]
		public void StackStepsMatchOriginalCode()
		{
			Check(10, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				// Planes are rare in the saves: a third of the units becomes a fighter (unit type 14)
				bool plane = chance.Next(3) == 0;
				byte status = (byte)chance.Next(256);
				byte unusual = (byte)chance.Next(256);

				if (chance.Next(2) == 0)
				{
					return new Scenario($"RestoreUnitOrders({participantIdx}, {troopIdx}), status 0x{status:x2}, 0x{unusual:x2}",
						game =>
						{
							UnitMemory gameTroops = new UnitMemory(game.Processor);
							gameTroops.Assign(participantIdx, troopIdx, UnitMemory.Status, status);
							gameTroops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, unusual);
						},
						game => new UnitManagementReference(game).RestoreUnitOrders(participantIdx, troopIdx),
						game => game.UnitOps.RestoreUnitOrders(participantIdx, troopIdx),
						Register.Kept, game => "orders");
				}

				return new Scenario($"EliminateSeaUnit({participantIdx}, {troopIdx}), fighter {plane}",
					game =>
					{
						if (plane)
							new UnitMemory(game.Processor).Assign(participantIdx, troopIdx, UnitMemory.Kind, 14);
					},
					game => new UnitManagementReference(game).EliminateSeaUnit(participantIdx, troopIdx),
					game => game.UnitOps.EliminateSeaUnit(participantIdx, troopIdx),
					Register.Kept, game => plane ? "plane" : "other unit");
			}, output, ["orders", "plane", "other unit"]);
		}
	}
}
