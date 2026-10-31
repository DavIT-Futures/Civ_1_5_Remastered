using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchHundredFiveDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnMoveVisibilityMatchesOriginalCode()
	{
		// Who sees the moves of the computer players and the barbarians: everything shown (0xd806), the option to show
		// the moves (0x20), embassies of the human, what the human has seen of the map
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckVisibility(bay, ways);

		foreach (string way in new[] { "animated", "redrawn", "computer", "barbarian" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckVisibility(int bay, HashSet<string> ways)
	{
		Check(bay, 16, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			var players = Enumerable.Range(0, 8).Where(player => player != person &&
				Enumerable.Range(0, 128).Any(troop => UnitByte(engine, player, troop, UnitMemory.Kind) != 0xff)).ToList();
			short participantIdx = (short)players[chance.Next(players.Count)];
			bool everything = chance.Next(2) == 0;
			bool option = chance.Next(2) == 0;
			bool[] embassies = Enumerable.Range(0, 8).Select(_ => chance.Next(2) == 0).ToArray();
			bool seen = chance.Next(2) == 0;
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			return new Scenario(
				$"HandlePlayerTurn({participantIdx}), everything {everything}, option {option}, seen {seen}",
				game =>
				{
					HumanTurnScript.Install(game, []);
					game.Processor.EmitUInt16(0x3b01, 0xd806, (ushort)(everything ? 1 : 0));
					game.World.SessionPrefMarkers = (short)((game.World.SessionPrefMarkers & ~0x30) | (option ? 0x20 : 0x10));
					ushort[] relations = game.World.Participants[person].Relations;

					for (int other = 0; other < 8; other++)
						relations[other] = (ushort)(embassies[other] ? relations[other] | 0x40 : relations[other] & ~0x40);

					if (seen)
					{
						// The human has seen the whole map
						for (int horizLoc = 0; horizLoc < 80; horizLoc++)
						{
							for (int vertLoc = 0; vertLoc < 50; vertLoc++)
								game.World.ChartSight[horizLoc, vertLoc] |= (sbyte)(1 << person);
						}
					}
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(participantIdx),
				game => game.TurnLogic.HandlePlayerTurn(participantIdx),
				Register.Kept,
				// How the moves were shown, from the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [participantIdx == 0 ? "barbarian" : "computer"];

					if (Delta("ChoreographUnitCombat") > 0)
						met.Add("animated");

					if (Delta("RenderMapTile") > 0)
						met.Add("redrawn");

					foreach (string way in met)
						ways.Add(way);

					return string.Join("+", met);
				});
		}, output, [], null);
	}
}
