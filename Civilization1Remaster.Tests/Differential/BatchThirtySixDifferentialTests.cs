using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The unit panel, the Orders menu, the trade report and the strategic map against their original code</summary>
	public class BatchThirtySixDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderUnitInfoMatchesOriginalCode()
		{
			Check(3, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				(short, short)? unit = RandomUnit(engine, chance, chance.Next(3) == 0 ? -1 : engine.World.PersonParticipantIdx);

				if (unit == null)
					return null;

				(short participantIdx, short troopIdx) = unit.Value;
				ushort arg2 = chance.Next(8) == 0 ? (ushort)0x80 : (ushort)troopIdx;
				byte steps = (byte)chance.Next(0, 30);
				byte fuel = (byte)chance.Next(0, 3);
				bool veteran = chance.Next(3) == 0;
				int x = troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), y = troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
				bool city = (engine.MapOps.Chart.Improvements(x, y) & 1) != 0;

				return new Scenario($"RenderUnitInfo({participantIdx}, {arg2}), steps {steps}, fuel {fuel}, veteran {veteran}",
					game =>
					{
						UnitMemory memory = new UnitMemory(game.Processor);
						memory.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, steps);
						memory.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, fuel);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Status,
							(byte)((memory.Pull(participantIdx, troopIdx, UnitMemory.Status) & ~0x20) | (veteran ? 0x20 : 0)));
					},
					game => new PlayerTurnReference(game).RenderUnitInfo(participantIdx, arg2),
					game => game.TurnLogic.RenderUnitInfo(participantIdx, arg2),
					Register.Kept,
					game => arg2 == 0x80 ? "no unit" : city ? "in a city" : "outside");
			}, output, ["no unit", "in a city", "outside"]);
		}

		[Fact]
		public void DisplayOrdersMenuMatchesOriginalCode()
		{
			Check(3, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				(short, short)? unit = RandomUnit(engine, chance, chance.Next(4) == 0 ? -1 : engine.World.PersonParticipantIdx);

				if (unit == null)
					return null;

				(short participantIdx, short troopIdx) = unit.Value;
				short given = chance.Next(10) switch { 0 => -1, 1 => 128, _ => troopIdx };
				bool settlers = chance.Next(2) == 0;
				ushort line = (ushort)chance.Next(0, 12);
				bool railroad = chance.Next(2) == 0;

				return new Scenario($"DisplayOrdersMenu({participantIdx}, {given}), settlers {settlers}, line {line}, railroad {railroad}",
					game =>
					{
						if (settlers)
							new UnitMemory(game.Processor).Assign(participantIdx, troopIdx, UnitMemory.Kind, 0);

						ushort[] science = game.World.Participants[participantIdx].FoundScienceMarkers;
						science[3] = (ushort)((science[3] & ~2) | (railroad ? 2 : 0));
						game.Processor.EmitGlobalWord(0x2f9a, line);
						game.Processor.EmitGlobalWord(0xd4ca, 0x1234);
					},
					game => new GameMenusReference(game).DisplayOrdersMenu(participantIdx, given),
					game => game.MenuSystem.DisplayOrdersMenu(participantIdx, given),
					Register.Kept,
					game => given is -1 or 128 ? "no unit" : game.Processor.PullGlobalWord(0xd4ca) == 0xffff ? "cancelled" :
						settlers ? "settlers" : "other unit");
			}, output, ["no unit", "cancelled", "settlers", "other unit"], [0x0d, 0x0d, 0x1b]);
		}

		[Fact]
		public void DisplayTradeReportMatchesOriginalCode()
		{
			Check(3, 16, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)chance.Next(0, 8);
				int towns = engine.World.Towns.Count(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx);
				short hardness = (short)chance.Next(0, 5);
				bool[] technologies = { chance.Next(2) == 0, chance.Next(2) == 0 };
				short season = (short)(chance.Next(3) switch { 0 => -1000, 1 => 1000, _ => 1800 });

				return new Scenario($"DisplayTradeReport({participantIdx}), {towns} cities, level {hardness}, year {season}",
					game =>
					{
						game.World.HardnessTier = hardness;
						game.World.Season = season;
						ushort[] science = game.World.Participants[participantIdx].FoundScienceMarkers;
						science[2] = (ushort)((science[2] & ~0x24) | (technologies[0] ? 4 : 0) | (technologies[1] ? 0x20 : 0));
					},
					game => new AdvisorReportsReference(game).DisplayTradeReport(participantIdx),
					game => game.Briefings.DisplayTradeReport(participantIdx),
					Register.Kept,
					game => towns == 0 ? "no cities" : towns > 17 ? "two pages" : "one page");
			}, output, ["no cities", "one page", "two pages"], [0x0d]);
		}

		[Fact]
		public void DisplayArmyListMatchesOriginalCode()
		{
			Check(3, 10, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(0, 8);
				ushort all = (ushort)chance.Next(2);

				return new Scenario($"DisplayArmyList({participantIdx}), all shown {all}",
					game => game.Processor.EmitGlobalWord(0xd806, all),
					game => new ArmySelectionReference(game).DisplayArmyList(participantIdx),
					game => game.ArmyPicker.DisplayArmyList(participantIdx),
					Register.Kept,
					game => all != 0 ? "all shown" : "what it sees");
			}, output, ["all shown", "what it sees"], [0x0d]);
		}
	}
}
