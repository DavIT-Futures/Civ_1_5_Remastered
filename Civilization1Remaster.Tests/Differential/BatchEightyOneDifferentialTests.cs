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

public sealed class BatchEightyOneDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void ShiftAIUnitStacksMatchOriginalCode()
	{
		// What the looks at the stacks did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13, 14 })
			CheckStacks(bay, ways);

		foreach (string way in new[] { "fortified released", "stack counted", "alone" })
			Assert.True(ways.Contains(way), $"No stack did '{way}' (did: {string.Join(", ", ways)})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private static ushort RoleOf(GameEngine game, byte kind)
	{
		return game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.TroopGroup));
	}

	private void CheckStacks(int bay, HashSet<string> ways)
	{
		Check(bay, 24, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// An attacker (role 3) or a transport (role 5) of a computer player, in a stack when possible, as the save has it
			var candidates = Enumerable.Range(1, 7).Where(idx => idx != person)
				.SelectMany(player => Enumerable.Range(0, 128).Select(troop => (Player: (short)player, Troop: (short)troop)))
				.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.Kind) is byte kind && kind != 0xff && RoleOf(engine, kind) is 3 or 5)
				.ToList();

			if (candidates.Count == 0)
				return null;

			var stacked = candidates.Where(unit => UnitByte(engine, unit.Player, unit.Troop, UnitMemory.FollowingInStack) != 0xff).ToList();
			var (participantIdx, troopIdx) = stacked.Count > 0 && chance.Next(4) != 0 ? stacked[chance.Next(stacked.Count)] : candidates[chance.Next(candidates.Count)];

			// The other members of its stack, fortified at random
			List<short> members = [];
			for (short member = (sbyte)UnitByte(engine, participantIdx, troopIdx, UnitMemory.FollowingInStack); member >= 0 && member != troopIdx && members.Count < 128;
				member = (sbyte)UnitByte(engine, participantIdx, member, UnitMemory.FollowingInStack))
			{
				members.Add(member);
			}

			bool[] fortified = members.Select(_ => chance.Next(2) == 0).ToArray();
			bool attack = chance.Next(2) == 0;
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// ShiftAIUnit reads its local 0xc before setting it: in the game ShiftUnit leaves a terrain type there
			ushort terrainLeft = (ushort)(troopIdx % 12);

			return new Scenario(
				$"ShiftAIUnit({participantIdx}, {troopIdx}) with {members.Count} more, attack {attack}",
				game =>
				{
					game.Processor.EmitUInt16(0x3b01, (ushort)(frame - 0xc), terrainLeft);

					for (int memberIdx = 0; memberIdx < members.Count; memberIdx++)
					{
						ushort status = (ushort)(UnitMemory.Address(participantIdx, members[memberIdx]) + UnitMemory.Status);
						byte current = game.Processor.PullUInt8(0x3b01, status);
						game.Processor.EmitUInt8(0x3b01, status, (byte)(fortified[memberIdx] ? current | 8 | (memberIdx & 1) : current & ~8));
					}

					ushort continent = game.MapOps.ObtainContinentID(game.Processor.PullInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Horiz)),
						game.Processor.PullInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Vert)));
					game.World.Participants[participantIdx].Landmasses[continent].Approach = (short)(attack ? 5 : 0);
				},
				game => new AIPlayerReference(game).ShiftAIUnit(participantIdx, troopIdx),
				game => game.Bot.ShiftAIUnit(participantIdx, troopIdx),
				Register.Kept | Register.Acc,
				// What the look at the stack did: a fortified member released (bit 3 cleared), or the members only counted
				game =>
				{
					string way = members.Count == 0 ? "alone"
						: members.Where((member, memberIdx) => fortified[memberIdx] && (UnitByte(game, participantIdx, member, UnitMemory.Status) & 8) == 0).Any() ? "fortified released"
						: "stack counted";

					ways.Add(way);
					return way;
				},
				[(0x3b01, (ushort)(frame - 0x5e), 0x5e)]);
		}, output, [], null);
	}
}
