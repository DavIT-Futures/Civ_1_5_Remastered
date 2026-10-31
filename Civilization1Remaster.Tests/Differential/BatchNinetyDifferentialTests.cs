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

public sealed class BatchNinetyDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnStartMatchesOriginalCode()
	{
		// The start of the turn of a player and of the move of each unit: the help of some turns, the moves of the units
		// (ships with the wonders 4 / 8, none with 0xdf60), kinds from 0x1c, fortifying units, home cities
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckTurns(bay, ways);

		foreach (string way in new[] { "help", "fortified", "home city", "kept in city", "militia", "ships", "no moves", "human", "computer" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort KindField(GameEngine game, byte kind, int field)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + field));
	}

	private void CheckTurns(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			var players = Enumerable.Range(1, 7).Where(player => Enumerable.Range(0, 128).Any(troop => UnitByte(engine, player, troop, UnitMemory.Kind) != 0xff)).ToList();
			short participantIdx = (short)(chance.Next(3) == 0 && players.Contains(person) ? person : players[chance.Next(players.Count)]);
			var units = Enumerable.Range(0, 128).Where(troop => UnitByte(engine, participantIdx, troop, UnitMemory.Kind) != 0xff).ToList();
			short round = (short)new[] { 20, 40, 60, 80, chance.Next(1, 200) }[chance.Next(5)];
			bool still = chance.Next(6) == 0;
			// Units that were fortifying (some in cities, which may become their home cities), one of a kind from 0x1c
			int[] fortifying = units.Where(_ => chance.Next(3) == 0).ToArray();
			int strange = chance.Next(3) == 0 && units.Count > 0 ? units[chance.Next(units.Count)] : -1;
			byte strangeKind = (byte)chance.Next(0x1c, 0x80);
			// The wonders 4 (Lighthouse) and 8 (Magellan's Expedition) in a city of the player
			var towns = Enumerable.Range(0, 128).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].ParticipantIdx == participantIdx).ToList();
			short wonderTown = (short)(towns.Count > 0 && chance.Next(2) == 0 ? towns[chance.Next(towns.Count)] : -1);
			int wonder = chance.Next(2) == 0 ? 4 : 8;
			sbyte[] extents = Enumerable.Range(0, 128).Select(_ => (sbyte)chance.Next(1, 6)).ToArray();
			byte[] homes = units.Select(_ => (byte)(chance.Next(2) == 0 ? 0xff : 0)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			var ships = units.Where(troop => KindField(engine, UnitByte(engine, participantIdx, troop, UnitMemory.Kind), UnitKindTable.LandGroup) == UnitKindTable.SeaUnits).ToList();

			return new Scenario(
				$"HandlePlayerTurn({participantIdx}), round {round}, still {still}, fortifying {fortifying.Length}, strange {strange}, wonder {wonder} in {wonderTown}",
				game =>
				{
					game.World.RoundTally = round;
					game.Processor.EmitUInt16(0x3b01, 0xdf60, (ushort)(still ? 1 : 0));

					foreach (int troop in fortifying)
					{
						ushort unit = UnitMemory.Address(participantIdx, troop);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), (byte)((game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Status)) & 0xf6) | 4));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), 3);

						// (a city: no home or the city itself)
						int homeIdx = units.IndexOf(troop);

						if (homes[homeIdx] == 0xff)
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), 0xff);
						else if ((game.MapOps.Chart.Improvements(game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)), game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert))) & 1) != 0)
						{
							ushort townIdx = game.Helpers.LocateCityAtPosition(game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)), game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)));
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), (byte)townIdx);
						}
					}

					if (strange >= 0)
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, strange) + UnitMemory.Kind), strangeKind);

					for (int townIdx = 0; townIdx < 128; townIdx++)
					{
						if (game.World.Towns[townIdx].ConditionMarker != 0xff)
							game.World.Towns[townIdx].TrueExtent = extents[townIdx];
					}

					if (wonderTown >= 0)
						game.World.MarvelTownIdx[wonder] = wonderTown;
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(participantIdx),
				game => game.TurnLogic.HandlePlayerTurn(participantIdx),
				Register.Kept,
				// What the turn met, from the calls and the units after it
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [participantIdx == person ? "human" : "computer"];

					if (Delta("DisplayHelpPopup") > 0 && round is 20 or 40 or 60 or 80)
						met.Add("help");

					if (fortifying.Any(troop => UnitByte(game, participantIdx, troop, UnitMemory.Kind) != 0xff && (UnitByte(game, participantIdx, troop, UnitMemory.Status) & 0xc) == 8))
						met.Add("fortified");

					if (participantIdx != person && fortifying.Any(troop => homes[units.IndexOf(troop)] == 0xff && UnitByte(game, participantIdx, troop, UnitMemory.BaseTown) != 0xff))
						met.Add("home city");

					if (Delta("KeepUnitsInCityList") > 0)
						met.Add("kept in city");

					if (strange >= 0 && UnitByte(game, participantIdx, strange, UnitMemory.Kind) == 1)
						met.Add("militia");

					if (wonderTown >= 0 && ships.Count > 0 && !still)
						met.Add("ships");

					if (still)
						met.Add("no moves");

					foreach (string way in met)
						ways.Add(way);

					return string.Join("+", met);
				});
		}, output, [], null);
	}
}
