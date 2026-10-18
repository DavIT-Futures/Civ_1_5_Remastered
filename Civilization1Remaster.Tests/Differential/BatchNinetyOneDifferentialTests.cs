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

public sealed class BatchNinetyOneDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnOrdersMatchOriginalCode()
	{
		// The units of a turn: the map view moves to the units of the human player (or they wait for the second pass), the
		// computer units get their orders from ShiftAIUnit (with retries), the units with orders go on with them
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckTurns(bay, ways);

		foreach (string way in new[] { "focus", "panel", "retries", "orders", "human", "computer", "computer by human" })
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
			short participantIdx = (short)(chance.Next(2) == 0 && players.Contains(person) ? person : players[chance.Next(players.Count)]);
			var units = Enumerable.Range(0, 128).Where(troop => UnitByte(engine, participantIdx, troop, UnitMemory.Kind) != 0xff).ToList();
			short round = (short)(chance.Next(4) == 0 ? 0 : chance.Next(1, 200));
			ushort quick = (ushort)chance.Next(2);
			// A computer player played by a human now and then (the units take the keys of the script)
			bool byHuman = participantIdx != person && chance.Next(5) == 0;
			// Orders of some units (status bits 1, 6, 7) or a destination, a plane among them
			int[] ordered = units.Where(_ => chance.Next(3) == 0).ToArray();
			// (only settlers take the orders of settlers: other units would ask for keys without end)
			byte[] orders = ordered.Select(troop => UnitByte(engine, participantIdx, troop, UnitMemory.Kind) == 0
				? (byte)new[] { 0x40, 0x80, 0xc0, 0x82, 0x02, 0 }[chance.Next(6)] : (byte)0).ToArray();
			bool[] travel = ordered.Select(_ => chance.Next(3) == 0).ToArray();
			int plane = chance.Next(3) == 0 && units.Count > 0 ? units[chance.Next(units.Count)] : -1;
			// The map view
			ushort viewHoriz = (ushort)chance.Next(0, 70);
			ushort viewVert = (ushort)chance.Next(0, 40);
			byte[] steps = units.Select(_ => (byte)chance.Next(0, 4)).ToArray();
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			return new Scenario(
				$"HandlePlayerTurn({participantIdx}), round {round}, by human {byHuman}, ordered {ordered.Length}, plane {plane}, view {viewHoriz}/{viewVert}",
				game =>
				{
					game.World.RoundTally = round;
					game.Processor.EmitUInt16(0x3b01, 0xdf60, 0);
					game.Processor.EmitUInt16(0x3b01, 0xb278, quick);
					game.Processor.EmitUInt16(0x3b01, 0xd4cc, viewHoriz);
					game.Processor.EmitUInt16(0x3b01, 0xd75e, viewVert);

					if (byHuman)
						game.World.ParticipantMarkers |= (short)(1 << participantIdx);
					else if (participantIdx != person)
						game.World.ParticipantMarkers &= (short)~(1 << participantIdx);

					for (int idx = 0; idx < ordered.Length; idx++)
					{
						ushort unit = UnitMemory.Address(participantIdx, ordered[idx]);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), (byte)((game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Status)) & 0x3c) | orders[idx]));

						if (travel[idx])
						{
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), (byte)((game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)) + 2) % 80));
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert), game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)));
						}
					}

					if (plane >= 0)
					{
						ushort unit = UnitMemory.Address(participantIdx, plane);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0xf);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), 2);
						// (a destination: with the order bits a plane asks for keys without end)
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), (byte)((game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)) + 3) % 80));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert), game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)));
					}
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(participantIdx),
				game => game.TurnLogic.HandlePlayerTurn(participantIdx),
				Register.Kept,
				// What the turn met, from the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [participantIdx == person ? "human" : byHuman ? "computer by human" : "computer"];

					if (participantIdx == person && Delta("FocusMapView") > 0)
						met.Add("focus");

					if (participantIdx == person && quick == 0 && Delta("AwaitTime") > 0)
						met.Add("wait");

					if (round == 0 && Delta("RenderUnitInfo") > 0)
						met.Add("panel");

					if (participantIdx != person && !byHuman && Delta("ShiftAIUnit") > units.Count)
						met.Add("retries");

					if ((participantIdx == person || byHuman) && ordered.Length > 0 && orders.Any(order => order != 0))
						met.Add("orders");

					foreach (string way in met)
						ways.Add(way);

					return string.Join("+", met);
				},
				// (and the stack below: what ShiftUnit keeps there, ShiftAIUnit finds)
				[(0x3b01, (ushort)(frame - 0x80), 0x80)]);
		}, output, [], null);
	}
}
