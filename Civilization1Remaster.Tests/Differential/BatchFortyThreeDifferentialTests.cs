using System;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The capture of a city, a diplomat in a city and the step of a unit towards a place against their original code</summary>
	public class BatchFortyThreeDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void SeizeCityMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].TrueExtent > 0).ToArray();

				if (towns.Length == 0)
					return null;

				short townIdx = towns[chance.Next(towns.Length)];
				short ownerIdx = engine.World.Towns[townIdx].ParticipantIdx;
				short participantIdx = (short)(chance.Next(4) == 0 ? person : chance.Next(0, 8));

				if (participantIdx == ownerIdx)
					return null;

				ushort marker = (ushort)(chance.Next(4) == 0 ? 1 : 0);
				bool screen = chance.Next(3) == 0;
				bool known = chance.Next(2) == 0;
				byte condition = (byte)chance.Next(0, 0x80);
				int spies = Calls(pair.Rewritten.Engine, pair, "DisplayDialog");

				return new Scenario($"SeizeCity({participantIdx}, {townIdx}, {marker}), owner {ownerIdx}, city screen {screen}, known {known}, condition 0x{condition:x2}",
					game =>
					{
						game.World.SessionPrefMarkers = (short)(screen ? game.World.SessionPrefMarkers | 8 : game.World.SessionPrefMarkers & ~8);
						game.World.Towns[townIdx].ConditionMarker = condition;
						ushort[] relations = game.World.Participants[person].Relations;
						relations[participantIdx] = (ushort)((relations[participantIdx] & ~0x40) | (known ? 0x40 : 0));
					},
					game => new TradeAndConquestReference(game).SeizeCity(participantIdx, townIdx, marker),
					game => game.Conquest.SeizeCity(participantIdx, townIdx, marker),
					Register.Kept,
					game => game.World.Towns[townIdx].TrueExtent == 0 ? "destroyed" :
						participantIdx == person ? "human takes" : ownerIdx == person ? "human loses" :
						Calls(game, pair, "DisplayDialog") > spies ? "spies" : "unseen");
			}, output, ["human takes", "spies", "unseen"], [0x0d, 0x1b, 0x20]);
		}

		[Fact]
		public void DiplomatArrivesAtCityMatchesOriginalCode()
		{
			string[] events = { "EarnTechnology", "ComputeInciteCost", "DisplayIntelligenceReport", "RenewCityScreen", "LaunchAudience" };

			Check(3, 160, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(3) != 0 ? person : chance.Next(1, 8));
				(short, short)? unit = RandomUnit(engine, chance, participantIdx);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].ParticipantIdx != participantIdx).ToArray();

				if (unit == null || towns.Length == 0)
					return null;

				short troopIdx = unit.Value.Item2;
				short townIdx = towns[chance.Next(towns.Length)];
				short gold = (short)(chance.Next(2) == 0 ? chance.Next(0, 3000) : 30000);
				bool robbed = chance.Next(3) == 0;
				bool disorder = chance.Next(2) == 0;
				ushort mask = (ushort)chance.Next(2);
				int[] before = events.Select(name => Calls(pair.Rewritten.Engine, pair, name)).ToArray();

				return new Scenario($"DiplomatArrivesAtCity({townIdx}, {participantIdx}, {troopIdx}), gold {gold}, robbed {robbed}",
					game =>
					{
						game.World.Participants[participantIdx].Gold = gold;
						game.World.Towns[townIdx].ConditionMarker = (byte)((game.World.Towns[townIdx].ConditionMarker & ~0x21) | (robbed ? 0x20 : 0) | (disorder ? 1 : 0));
						game.Processor.EmitGlobalWord(0xb276, mask);
					},
					game => new DiplomatActionsReference(game).DiplomatArrivesAtCity(townIdx, participantIdx, troopIdx),
					game => game.Diplomats.DiplomatArrivesAtCity(townIdx, participantIdx, troopIdx),
					Register.Kept,
					game =>
					{
						int happened = Enumerable.Range(0, events.Length).FirstOrDefault(idx => Calls(game, pair, events[idx]) > before[idx], -1);
						string text = game.Processor.PullText(CPU.AsLinearAddress(game.Processor.SegData.Value16, 0xba06));

						return happened >= 0 ? events[happened] : text.Contains("destroyed") || text.Contains("sabotaged") ? "sabotage" : text.Contains("Dissidents") ? "incite refused" : "other";
					});
			}, output, ["EarnTechnology", "ComputeInciteCost", "DisplayIntelligenceReport", "RenewCityScreen", "sabotage", "other"],
				[0x0d, 0x5000, 0x0d, 0x5000, 0x5000, 0x0d, 0x5000, 0x5000, 0x5000, 0x0d, 0x5000, 0x5000, 0x5000, 0x5000, 0x0d, 0x5000, 0x0d, 0x1b]);
		}

		[Fact]
		public void ShiftTowardMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				(short, short)? unit = RandomUnit(engine, chance);

				if (unit == null)
					return null;

				(short participantIdx, short troopIdx) = unit.Value;
				ushort troop = (ushort)(0x81d4 + (0x600 * participantIdx) + (0xc * troopIdx));
				int unitHoriz = engine.Processor.PullGlobalByte((ushort)(troop + 1)), unitVert = engine.Processor.PullGlobalByte((ushort)(troop + 2));
				bool again = chance.Next(4) == 0;
				ushort placeHoriz = again ? engine.Processor.PullGlobalWord(0x6796) : (ushort)Math.Clamp(unitHoriz + chance.Next(-7, 8), 0, 79);
				ushort placeVert = again ? engine.Processor.PullGlobalWord(0x6798) : (ushort)Math.Clamp(unitVert + chance.Next(-7, 8), 2, 47);
				ushort limit = (ushort)(chance.Next(3) == 0 ? chance.Next(0, 20) : 0x3e7);

				return new Scenario($"ShiftToward({participantIdx}, {troopIdx}, {limit}), unit ({unitHoriz}, {unitVert}), place ({placeHoriz}, {placeVert}), again {again}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x6590, placeHoriz);
						game.Processor.EmitGlobalWord(0x6592, placeVert);
					},
					game => new PathfindingReference(game).ShiftToward(participantIdx, troopIdx, limit),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Routing.ShiftToward(participantIdx, troopIdx, limit)),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 == 0xffff ? "no step" : "step");
			}, output, ["no step", "step"]);
		}
	}
}
