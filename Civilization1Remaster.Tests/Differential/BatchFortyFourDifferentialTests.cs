using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The step of a unit towards its goal and the end of a research against their original code</summary>
	public class BatchFortyFourDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void CarryOutGotoMatchesOriginalCode()
		{
			Check(3, 150, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				(short, short)? unit = RandomUnit(engine, chance, chance.Next(3) == 0 ? person : -1);

				if (unit == null)
					return null;

				(short participantIdx, short troopIdx) = unit.Value;
				ushort troop = (ushort)(0x81d4 + (0x600 * participantIdx) + (0xc * troopIdx));
				int unitHoriz = engine.Processor.PullGlobalByte((ushort)(troop + 1)), unitVert = engine.Processor.PullGlobalByte((ushort)(troop + 2));
				int reach = chance.Next(3) switch { 0 => 1, 1 => 7, _ => 40 };
				int goalHoriz = Math.Clamp(unitHoriz + chance.Next(-reach, reach + 1), 0, 79), goalVert = Math.Clamp(unitVert + chance.Next(-reach, reach + 1), 2, 47);
				byte last = (byte)(chance.Next(3) == 0 ? 0xff : chance.Next(1, 9));
				int density = chance.Next(0, 9);
				byte[] land = Enumerable.Range(0, 0x104).Select(_ => (byte)Enumerable.Range(0, 8).Sum(bit => chance.Next(10) < density ? 1 << bit : 0)).ToArray();
				bool near = participantIdx == person && Math.Abs(goalHoriz - unitHoriz) <= 1 && Math.Abs(goalVert - unitVert) <= 1;

				return new Scenario($"CarryOutGoto({participantIdx}, {troopIdx}), unit ({unitHoriz}, {unitVert}), goal ({goalHoriz}, {goalVert}), last {last}",
					game =>
					{
						game.Processor.EmitGlobalByte((ushort)(troop + 6), (byte)goalHoriz);
						game.Processor.EmitGlobalByte((ushort)(troop + 7), (byte)goalVert);
						game.Processor.EmitGlobalByte((ushort)(troop + 8), last);

						for (int idx = 0; idx < 0x104; idx++)
							game.Processor.EmitGlobalByte((ushort)(0xdb44 + idx), land[idx]);
					},
					game => new PathfindingReference(game).CarryOutGoto(participantIdx, troopIdx),
					game => game.Routing.CarryOutGoto(participantIdx, troopIdx),
					Register.Acc | Register.Kept,
					game => near ? "near goal" : game.Processor.RegAcc.Value16 == 0xffff ? "no step" : "step");
			}, output, ["near goal", "no step", "step"]);
		}

		[Fact]
		public void UncoverTechnologyMatchesOriginalCode()
		{
			Check(3, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(2) == 0 ? person : chance.Next(0, 8));
				short source = (short)(chance.Next(2) == 0 ? participantIdx : 0);
				short round = (short)(chance.Next(5) == 0 ? 0 : chance.Next(1, 400));
				short hardness = (short)(chance.Next(2) == 0 ? 0 : chance.Next(1, 5));
				short preferences = (short)chance.Next(0, 0x10);
				short inquiry = (short)(chance.Next(3) switch { 0 => -1, 1 => 71, _ => chance.Next(0, 0x48) });
				ushort cycling = (ushort)chance.Next(2);
				ushort palette = (ushort)chance.Next(0, 8);
				int earned = Calls(pair.Rewritten.Engine, pair, "EarnTechnology");

				return new Scenario($"UncoverTechnology({participantIdx}, {source}), turn {round}, level {hardness}, preferences 0x{preferences:x}, research {inquiry}",
					game =>
					{
						game.World.RoundTally = round;
						game.World.HardnessTier = hardness;
						game.World.SessionPrefMarkers = preferences;
						game.World.Participants[person].PresentInquiryIdx = inquiry;
						game.Processor.EmitGlobalWord(0xd762, cycling);
						game.Processor.EmitGlobalWord(0xdeba, palette);
						game.Processor.EmitGlobalWord(0x2f9c, 0);
					},
					game => new ScienceAndAdvisorsReference(game).UncoverTechnology(participantIdx, source),
					game => game.Advisors.UncoverTechnology(participantIdx, source),
					Register.Acc | Register.Kept,
					game => participantIdx == 0 ? "barbarians" : participantIdx != person || round == 0 ?
						(Calls(game, pair, "EarnTechnology") > earned ? "AI learns" : "AI doesn't") :
						inquiry != -1 ? "discovery" : "choice");
			}, output, ["barbarians", "AI learns", "AI doesn't", "discovery", "choice"], [0x0d, 0x5000, 0x0d, 0x0d, 0x5000, 0x5000, 0x0d, 0x1b]);
		}
	}
}
