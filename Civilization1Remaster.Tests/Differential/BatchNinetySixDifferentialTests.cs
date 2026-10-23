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

public sealed class BatchNinetySixDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnOrdersOfAllKeysMatchOriginalCode()
	{
		// 'b' (settlers join a city or found one) and the orders of all keys through one switch
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckOrders(bay, ways);

		foreach (string way in new[] { "joined", "city too big", "founded", "help" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckOrders(int bay, HashSet<string> ways)
	{
		Check(bay, 16, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			int[] keys = { 'b', 'b', 'b', 'b', 0x20, 0x0d, 'w', 's', 'f', 'r', 'i', 'm', 'p', 'h', 'u', 'P', 'x' };
			List<HumanTurnScript.Input> inputs = Enumerable.Range(0, chance.Next(4, 14))
				.Select(_ => new HumanTurnScript.Input(keys[chance.Next(keys.Length)])).ToList();
			// The sizes of the cities (some at 10 and more), a settler moved into an own city
			sbyte[] extents = Enumerable.Range(0, 128).Select(_ => (sbyte)chance.Next(7, 12)).ToArray();
			bool settle = chance.Next(2) == 0;
			int townPick = chance.Next(1000);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			return new Scenario(
				$"HandlePlayerTurn({person}), keys {string.Join(" ", inputs.Select(input => input.Key.ToString("x")))}, settle {settle}",
				game =>
				{
					HumanTurnScript.Install(game, inputs);

					for (int townIdx = 0; townIdx < 128; townIdx++)
					{
						if (game.World.Towns[townIdx].ConditionMarker != 0xff)
							game.World.Towns[townIdx].TrueExtent = extents[townIdx];
					}

					var own = Enumerable.Range(0, 128).Where(townIdx => game.World.Towns[townIdx].ConditionMarker != 0xff && game.World.Towns[townIdx].ParticipantIdx == person).ToList();
					int first = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, person, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, person, troop, UnitMemory.Status) & 9) == 0, -1);

					if (settle && own.Count > 0 && first >= 0)
					{
						// The first unit that moves becomes settlers, in an own city (or where it stands)
						ushort unit = UnitMemory.Address(person, first);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0);

						if (townPick % 2 == 0 && UnitByte(game, person, first, UnitMemory.FollowingInStack) == 0xff)
						{
							var town = game.World.Towns[own[townPick % own.Count]];
							game.MapOps.DiscardUnitFromTile(person, (short)first, UnitByte(game, person, first, UnitMemory.Horiz), UnitByte(game, person, first, UnitMemory.Vert));
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)town.Place.X);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)town.Place.Y);
							game.MapOps.Chart.AssignOwnerBits(town.Place.X, town.Place.Y, MapLayers.UnitsPresent | person);
						}
					}
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the orders did, from the cities and the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (Enumerable.Range(0, 128).Any(townIdx => game.World.Towns[townIdx].ConditionMarker != 0xff && game.World.Towns[townIdx].TrueExtent == extents[townIdx] + 1 &&
						game.World.Towns[townIdx].ParticipantIdx == person))
						met.Add("joined");

					if (inputs.Any(input => input.Key == 'b') && Delta("DisplayHelpForHuman") > 0 &&
						Enumerable.Range(0, 128).Any(townIdx => game.World.Towns[townIdx].ConditionMarker != 0xff && game.World.Towns[townIdx].ParticipantIdx == person && extents[townIdx] >= 10))
						met.Add("city too big");

					if (Delta("EstablishCity") > 0)
						met.Add("founded");

					if (Delta("DisplayHelpForHuman") > 0)
						met.Add("help");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
