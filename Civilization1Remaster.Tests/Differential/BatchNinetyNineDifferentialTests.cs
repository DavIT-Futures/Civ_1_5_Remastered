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

public sealed class BatchNinetyNineDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnDiplomatsAndCaravansMatchOriginalCode()
	{
		// A diplomat or caravan next to a city moves into it: a diplomat acts in a foreign city (a computer diplomat in a city
		// of the human is shown), a caravan asks (trade route, help to build a wonder) or just moves in near its home city
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckEntries(bay, ways);

		foreach (string way in new[] { "diplomat in city", "diplomat into human city", "caravan asks", "trade route", "wonder help", "moved without city" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static readonly int[] Arrows = { 0, 0x4800, 0x4900, 0x4d00, 0x5100, 0x5000, 0x4f00, 0x4b00, 0x4700 };

	private void CheckEntries(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			IMapPlanes sight = engine.MapOps.Chart;
			// The mover: the first unit of the human that moves, or (a third of the time) a unit of a computer player next to a
			// city of the human, as a diplomat or caravan; the city: own or foreign, its production (a wonder from -25 down)
			bool computer = chance.Next(3) == 0;
			byte kind = (byte)(computer || chance.Next(2) == 0 ? UnitKindTable.Diplomat : 0x1b);
			bool ownCity = !computer && chance.Next(2) == 0;
			sbyte making = (sbyte)new[] { 3, -5, -30, -27, -26, -25, -24 }[chance.Next(7)];
			bool nearHome = chance.Next(2) == 0;
			int townPick = chance.Next(1000);
			int side = chance.Next(1, 9);
			int[] answers = new[] { new[] { 0x0d }, new[] { 0x5000, 0x0d }, new[] { 0x5000, 0x5000, 0x0d }, new[] { 0x5000, 0x5000, 0x0d }, new[] { 0x31 } }[chance.Next(5)];
			short computerIdx = (short)Enumerable.Range(1, 7).Where(player => player != person &&
				Enumerable.Range(0, 128).Any(troop => UnitByte(engine, player, troop, UnitMemory.Kind) != 0xff)).DefaultIfEmpty(1).ElementAt(chance.Next(3) % Math.Max(1,
				Enumerable.Range(1, 7).Count(player => player != person && Enumerable.Range(0, 128).Any(troop => UnitByte(engine, player, troop, UnitMemory.Kind) != 0xff))));
			short participantIdx = computer ? computerIdx : person;
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			short[] materials = engine.World.Towns.Select(town => town.MaterialsTally).ToArray();
			List<HumanTurnScript.Input> inputs = [];

			return new Scenario(
				$"HandlePlayerTurn({participantIdx}), kind {kind:x}, own city {ownCity}, making {making}, near home {nearHome}, side {side}, answers {string.Join(" ", answers.Select(key => key.ToString("x")))}",
				game =>
				{
					IMapPlanes chart = game.MapOps.Chart;
					var towns = Enumerable.Range(0, 128).Where(townIdx => game.World.Towns[townIdx].ConditionMarker != 0xff &&
						(ownCity || computer ? game.World.Towns[townIdx].ParticipantIdx == person : game.World.Towns[townIdx].ParticipantIdx != person)).ToList();
					int mover = Enumerable.Range(0, 128).FirstOrDefault(troop => UnitByte(game, participantIdx, troop, UnitMemory.Kind) != 0xff &&
						(UnitByte(game, participantIdx, troop, UnitMemory.Status) & 9) == 0, -1);
					inputs.Clear();

					if (towns.Count > 0 && mover >= 0)
					{
						int townIdx = towns[townPick % towns.Count];
						var town = game.World.Towns[townIdx];
						// The mover goes to the tile on the side of the city, the arrow brings it in
						int horizLoc = (town.Place.X + chart.DirectionHoriz(side) + 80) % 80;
						int vertLoc = town.Place.Y + chart.DirectionVert(side);

						if (vertLoc > 0 && vertLoc < 49 && chart.Terrain(horizLoc, vertLoc) != MapRules.OceanTerrain && (chart.OwnerBits(horizLoc, vertLoc) & MapPlanes.UnitsPresent) == 0)
						{
							ushort unit = UnitMemory.Address(participantIdx, mover);
							game.MapOps.RemoveUnitFromTile(participantIdx, (short)mover, UnitByte(game, participantIdx, mover, UnitMemory.Horiz), UnitByte(game, participantIdx, mover, UnitMemory.Vert));
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)horizLoc);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)vertLoc);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), kind);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
							chart.AssignOwnerBits(horizLoc, vertLoc, MapPlanes.UnitsPresent | participantIdx);
							game.Processor.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, horizLoc - 7));
							game.Processor.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, vertLoc - 5));

							// Its home city: this one (near) or another of the player
							var homes = Enumerable.Range(0, 128).Where(idx => game.World.Towns[idx].ConditionMarker != 0xff && game.World.Towns[idx].ParticipantIdx == participantIdx).ToList();
							int homeIdx = nearHome && town.ParticipantIdx == participantIdx ? townIdx : homes.Count > 0 ? homes[(townPick / 7) % homes.Count] : townIdx;
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), (byte)homeIdx);
							town.PresentMakingIdx = making;

							// ('1' first: the game empties the keyboard before the unit moves)
							int direction = (side + 3) % 8 + 1;
							inputs.Add(new HumanTurnScript.Input(0x31));
							inputs.Add(new HumanTurnScript.Input(Arrows[direction]));
							inputs.AddRange(answers.Select(key => new HumanTurnScript.Input(key)));
						}
					}

					HumanTurnScript.Install(game, inputs);
				},
				game => new TurnControllerReference(game).HandlePlayerTurn(participantIdx),
				game => game.TurnLogic.HandlePlayerTurn(participantIdx),
				Register.Kept,
				// What the move met, from the calls and the cities
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (Delta("DiplomatArrivesAtCity") > 0)
						met.Add("diplomat in city");

					if (Delta("ChoreographUnitCombat") > 0 && computer)
						met.Add("diplomat into human city");

					if (kind == 0x1b && Delta("DisplayDialog") > 0)
						met.Add("caravan asks");

					if (Delta("FoundTradeRoute") > 0)
						met.Add("trade route");

					if (kind == 0x1b && Enumerable.Range(0, 128).Any(townIdx => game.World.Towns[townIdx].MaterialsTally > materials[townIdx]))
						met.Add("wonder help");

					if (Delta("LocateCityAtPosition") > 0 && inputs.Count > 0 && !computer)
						met.Add("moved without city");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
