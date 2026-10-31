using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventySevenDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitMatchesOriginalCode()
	{
		// What the moves of the units did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 3, 8, 12 })
			CheckShiftAIUnit(bay, ways);

		foreach (string way in new[] { "barbarians", "out of fuel", "fighter waits", "fighter to bomber", "fighter to city", "fighter without city", "moved" })
			Assert.True(ways.Contains(way), $"No unit did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckShiftAIUnit(int bay, HashSet<string> ways)
	{
		int step = 0;

		Check(bay, 30, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// 0: a barbarian unit, 1: a bomber (fuel 0 - 2), 2 - 4: a fighter (2: few moves, 3: a bomber of the human player
			// near, 4: far, with or without an own city in reach), 5+: a unit as it is
			int mode = step++ % 8;
			short participantIdx = mode == 0 ? (short)0 : (short)chance.Next(1, 8);

			if (participantIdx == person)
				participantIdx = (short)(person % 7 + 1);

			List<short> units = Enumerable.Range(0, 128).Where(idx => UnitByte(engine, participantIdx, idx, UnitMemory.Kind) != 0xff).Select(idx => (short)idx).ToList();
			short troopIdx = units.Count == 0 ? (short)0 : units[chance.Next(units.Count)];
			byte fuel = (byte)chance.Next(3);
			byte moves = (byte)(mode == 2 ? chance.Next(1, 4) : chance.Next(12, 30));
			int bomberDistance = mode == 3 ? chance.Next(2, 5) : chance.Next(10, 30);
			bool noCity = chance.Next(3) == 0;
			byte horiz = (byte)chance.Next(10, 70);
			byte vert = (byte)chance.Next(5, 45);

			if (units.Count == 0 && mode > 4)
				return null;

			// Mode 4 with an own city: the fighter starts in it (the city is in reach, the bomber is not)
			var home = engine.World.Towns.FirstOrDefault(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx);

			if (mode == 4 && !noCity && home != null)
			{
				horiz = (byte)home.Place.X;
				vert = (byte)home.Place.Y;
				bomberDistance = 30;
			}

			byte travelBefore = UnitByte(engine, participantIdx, troopIdx, UnitMemory.TravelHoriz);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}), mode {mode}, fuel {fuel}, moves {moves}, bomber {bomberDistance}, no city {noCity}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					ushort unit = UnitMemory.Address(participantIdx, troopIdx);

					void Set(int offset, byte amount) => game.Processor.EmitUInt8(0x3b01, (ushort)(unit + offset), amount);

					if (mode is >= 1 and <= 4)
					{
						Set(UnitMemory.Kind, (byte)(mode == 1 ? 0xf : 0xe));
						Set(UnitMemory.Horiz, horiz);
						Set(UnitMemory.Vert, vert);
						Set(UnitMemory.Status, 0);
						Set(UnitMemory.UnusualSteps, fuel);
						Set(UnitMemory.LeftoverSteps, moves);
						Set(UnitMemory.TravelHoriz, 0xff);
						Set(UnitMemory.TravelVert, 0xff);
						Set(UnitMemory.FollowingInStack, 0xff);
					}

					if (mode is 3 or 4)
					{
						// A bomber of the human player the player has seen
						ushort bomber = UnitMemory.Address(person, 127);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Kind), 0xf);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Horiz), (byte)((horiz + bomberDistance) % 80));
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.Vert), vert);
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.ShownToParticipants), (byte)(1 << participantIdx));
						game.Processor.EmitUInt8(0x3b01, (ushort)(bomber + UnitMemory.FollowingInStack), 0xff);

						if (noCity)
						{
							foreach (var town in game.World.Towns.Where(town => town.ParticipantIdx == participantIdx && town.ConditionMarker != 0xff))
								town.ParticipantIdx = (sbyte)person;
						}
					}
				},
				game => new ComputerPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ShiftAIUnit(participantIdx, troopIdx)),
				Register.Kept | Register.Acc,
				// What the move did, from the answer and the destination of the unit
				game =>
				{
					ushort answer = game.Processor.RegAcc.Value16;
					byte travel = UnitByte(game, participantIdx, troopIdx, UnitMemory.TravelHoriz);
					string way;

					if (participantIdx == 0)
						way = "barbarians";
					else if (mode == 1 && answer == 0x68)
						way = "out of fuel";
					else if (mode == 2 && answer == 0x68)
						way = "fighter waits";
					else if (mode is 3 or 4 && travel == (byte)((horiz + bomberDistance) % 80))
						way = "fighter to bomber";
					else if (mode is 3 or 4 && answer == 0x20 && travel == 0xff)
						way = "fighter without city";
					else if (mode is 3 or 4 && travel != 0xff && travel != travelBefore)
						way = "fighter to city";
					else
						way = "moved";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
