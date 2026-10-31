using System;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The Hall of Fame entry, the emissary, a line of the demographics and a unit on the map against their original code
	/// </summary>
	public class BatchTwentyThreeDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The Hall of Fame: 6 records of 0x2e bytes at 0x3772:0x37d2</summary>
		private static readonly (ushort, ushort, int) HallOfFame = (0x3772, 0x37d2, 0x114);

		[Fact]
		public void AttachHallOfFameRecordMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				ushort[] scores = Enumerable.Range(0, 6).Select(_ => (ushort)(chance.Next(5) == 0 ? 0xffff : chance.Next(0, 3000))).OrderByDescending(score => (short)score).ToArray();
				ushort[] tiers = Enumerable.Range(0, 6).Select(_ => (ushort)chance.Next(5)).ToArray();
				ushort score = (ushort)chance.Next(0, 3000);
				short tier = (short)chance.Next(5);

				return new Scenario($"AttachHallOfFameRecord(), score {score}, difficulty {tier}, records {string.Join(" ", scores)}",
					game =>
					{
						for (int idx = 0; idx < 6; idx++)
						{
							game.Processor.EmitUInt16(0x3772, (ushort)((idx * 0x2e) + 0x37d2), scores[idx]);
							game.Processor.EmitUInt16(0x3772, (ushort)((idx * 0x2e) + 0x37d4), tiers[idx]);
						}

						game.Processor.EmitGlobalWord(0xb77e, score);
						game.World.HardnessTier = tier;
					},
					game => new HallOfFameReference(game).AttachHallOfFameRecord(),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Legends.AttachHallOfFameRecord()),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 switch { 0 => "first", 5 => "last", 0xffff => "none", _ => "between" },
					[HallOfFame]);
			}, output, ["first", "last", "between"]);
		}

		[Fact]
		public void WelcomeEmissaryMatchesOriginalCode()
		{
			// Enter takes the first line of the question (receive), the arrows choose the others
			int[] keys = { 0x0d, 0x0d, 0x5000, 0x0d, 0x0d, 0x5000, 0x5000, 0x0d, 0x0d, 0x0d, 0x0d };

			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short humanIdx = engine.World.PersonParticipantIdx;
				short participantIdx = (short)((humanIdx + chance.Next(1, 8)) % 8);
				ushort asked = (ushort)(chance.Next(5) == 0 ? 1 : 0);
				ushort answer = new ushort[] { 0, 0, 0, 0xffff, 0xfffe, 3 }[chance.Next(6)];
				bool intelligence = chance.Next(2) == 0;
				(int horizLoc, int vertLoc) = chance.Next(2) == 0 ? (-1, 0) : RandomTile(engine, chance);
				int reports = Calls(pair.Rewritten.Engine, pair, "DisplayIntelligenceReport");

				return new Scenario($"WelcomeEmissary({participantIdx}, {horizLoc}, {vertLoc}), 0xd804 {asked}, 0x3934 {answer:x4}, " +
					$"relation bit 0x40 {intelligence}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd804, asked);
						game.Processor.EmitGlobalWord(0x3934, answer);

						ref ushort relation = ref game.World.Participants[humanIdx].Relations[participantIdx];
						relation = (ushort)(intelligence ? relation | 0x40 : relation & ~0x40);
					},
					game => new MeetWithKingReference(game).WelcomeEmissary(participantIdx, horizLoc, vertLoc),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Audience.WelcomeEmissary(participantIdx, horizLoc, vertLoc)),
					Register.Acc | Register.Kept,
					game => asked != 0 ? "asked before" : answer != 0 ? (answer == 0xffff ? "refused before" : "answer known") :
						(Calls(game, pair, "DisplayIntelligenceReport") > reports ? "intelligence, " : "") +
						(game.Processor.PullGlobalWord(0x3934) == 0xffff ? "refused" : "received"));
			}, output, ["asked before", "refused before", "answer known", "refused", "received", "intelligence, received"], keys);
		}

		[Fact]
		public void RenderCensusLineMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				short humanIdx = pair.Original.Engine.World.PersonParticipantIdx;
				short[] amounts = Enumerable.Range(0, 8).Select(_ => (short)chance.Next(-500, 5000)).ToArray();
				short vertLoc = (short)(chance.Next(20, 190) * (chance.Next(2) == 0 ? -1 : 1));
				ushort realms = (ushort)(chance.Next(0x100) | (1 << humanIdx));
				ushort relations = (ushort)chance.Next(0x100);
				ushort charsAddr = new ushort[] { 0x4557, 0x4565, 0x45ce, 0x466f }[chance.Next(4)];

				return new Scenario($"RenderCensusLine(0xbb00, {vertLoc}, 0x{charsAddr:x4}), values {string.Join(" ", amounts)}, " +
					$"living 0x{realms:x2}, relations 0x40 0x{relations:x2}",
					game =>
					{
						for (int idx = 0; idx < 8; idx++)
						{
							game.Processor.EmitGlobalWord((ushort)(0xbb00 + (idx << 1)), (ushort)amounts[idx]);

							ref ushort relation = ref game.World.Participants[humanIdx].Relations[idx];
							relation = (ushort)((relations & (1 << idx)) != 0 ? relation | 0x40 : relation & ~0x40);
						}

						game.World.LiveRealms = (short)realms;
					},
					game => new WorldMapReference(game).RenderCensusLine(0xbb00, vertLoc, charsAddr),
					game => game.Atlas.RenderCensusLine(0xbb00, vertLoc, charsAddr),
					Register.Kept,
					game =>
					{
						string text = game.Processor.PullText(CPU.AsLinearAddress(game.Processor.SegData.Value16, 0xba06));

						return text.StartsWith('(') ? "best shown" : text[^2..];
					});
			}, output, ["best shown", "st", "nd", "rd", "th"]);
		}

		[Fact]
		public void RenderUnitOnWorldMapMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				UnitMemory troops = new UnitMemory(engine.Processor);
				int horizLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz), vertLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
				ushort worldMap = (ushort)(chance.Next(4) == 0 ? 1 : 0);
				bool near = chance.Next(4) != 0;
				ushort viewHoriz = (ushort)(near ? (horizLoc - chance.Next(0, 16) + 80) % 80 : chance.Next(80));
				ushort viewVert = (ushort)(near ? Math.Max(0, vertLoc - chance.Next(0, 13)) : chance.Next(40));

				// Sometimes a sleeping land unit moved to the ocean (hidden on a ship)
				bool atSea = chance.Next(4) == 0;
				(int seaHoriz, int seaVert) = (horizLoc, vertLoc);

				for (int attempt = 0; atSea && attempt < 30; attempt++)
				{
					(int tryHoriz, int tryVert) = ((horizLoc + chance.Next(-4, 5) + 80) % 80, Math.Clamp(vertLoc + chance.Next(-4, 5), 1, 48));

					if (engine.MapOps.Chart.Terrain(tryHoriz, tryVert) == MapRules.OceanTerrain)
					{
						(seaHoriz, seaVert) = (tryHoriz, tryVert);
						break;
					}
				}

				byte status = (byte)(atSea ? troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 1 : troops.Pull(participantIdx, troopIdx, UnitMemory.Status));

				return new Scenario($"RenderUnitOnWorldMap({participantIdx}, {troopIdx}), world map {worldMap}, view {viewHoriz}, {viewVert}, " +
					$"at sea {atSea} ({seaHoriz}, {seaVert})",
					game =>
					{
						game.Processor.EmitGlobalWord(0xb278, worldMap);
						game.Processor.EmitGlobalWord(0xd4cc, viewHoriz);
						game.Processor.EmitGlobalWord(0xd75e, viewVert);

						UnitMemory memory = new UnitMemory(game.Processor);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)seaHoriz);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)seaVert);
						memory.Assign(participantIdx, troopIdx, UnitMemory.Status, status);
					},
					game => new MapManagementReference(game).RenderUnitOnWorldMap(participantIdx, troopIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx)),
					Register.Acc | Register.Kept,
					game => worldMap == 1 ? "world map" : game.Processor.RegAcc.Value16 == 1 ?
						(new UnitMemory(game.Processor).Following(participantIdx, troopIdx) != -1 ? "drawn with stack" : "drawn") :
						atSea && engine.MapOps.Chart.Terrain(seaHoriz, seaVert) == MapRules.OceanTerrain ? "not drawn at sea" : "not drawn");
			}, output, ["world map", "drawn with stack", "drawn", "not drawn at sea", "not drawn"]);
		}
	}
}
