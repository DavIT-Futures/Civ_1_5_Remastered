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

public sealed class BatchSeventyEightDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitNuclearWeaponMatchesOriginalCode()
	{
		// What the nuclear weapons did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 6, 10, 14 })
			CheckNuclearWeapon(bay, ways);

		foreach (string way in new[] { "placed", "no target", "too few weapons" })
			Assert.True(ways.Contains(way), $"No nuclear weapon did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckNuclearWeapon(int bay, HashSet<string> ways)
	{
		Check(bay, 16, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			short[] players = Enumerable.Range(1, 7)
				.Where(idx => idx != person && engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == idx))
				.Select(idx => (short)idx).ToArray();
			short participantIdx = players[chance.Next(players.Length)];
			List<short> units = Enumerable.Range(0, 128).Where(idx => UnitByte(engine, participantIdx, idx, UnitMemory.Kind) != 0xff).Select(idx => (short)idx).ToList();

			if (units.Count == 0)
				return null;

			short troopIdx = units[chance.Next(units.Count)];
			short weapons = (short)chance.Next(1, 4);
			// The relations to the others: war with bit 0x80 (a target), warned (8), peace, own strength against theirs
			ushort[] relations = Enumerable.Range(0, 8).Select(_ => (ushort)new[] { 0x81, 0x81 | 8, 0x82, 1, 0x80 }[chance.Next(5)]).ToArray();
			short might = (short)chance.Next(0, 300);
			short[] others = Enumerable.Range(0, 8).Select(_ => (short)chance.Next(0, 300)).ToArray();
			short[] theirWeapons = Enumerable.Range(0, 8).Select(_ => (short)(chance.Next(3) == 0 ? 1 : 0)).ToArray();
			int growth = chance.Next(0, 8);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) nuclear, weapons {weapons}, might {might}, growth {growth}",
				game =>
				{
					var player = game.World.Participants[participantIdx];
					ushort unit = UnitMemory.Address(participantIdx, troopIdx);

					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0x19);
					game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
					player.LiveTroops[25] = weapons;
					player.MartialMight = might;

					for (int otherIdx = 0; otherIdx < 8; otherIdx++)
					{
						if (otherIdx == participantIdx)
							continue;

						player.Relations[otherIdx] = relations[otherIdx];
						game.World.Participants[otherIdx].MartialMight = others[otherIdx];
						game.World.Participants[otherIdx].LiveTroops[25] = theirWeapons[otherIdx];
					}

					// The cities grow: more of them are bigger than 4
					foreach (var town in game.World.Towns.Where(town => town.ConditionMarker != 0xff))
						town.TrueExtent = (sbyte)Math.Min(20, town.TrueExtent + growth);
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Bot.ShiftAIUnit(participantIdx, troopIdx),
				Register.Kept | Register.Acc,
				// What the weapon did: placed next to a target (the answer is the direction ^ 4), no target, or a target but too few weapons
				game =>
				{
					ushort answer = game.Processor.RegAcc.Value16;
					string way = answer != 0x20 ? "placed"
						: (short)game.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x28)) >= 0xa ? "too few weapons"
						: "no target";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
