using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The yield of a tile, the status panel, the ranking screen and the bribe of a unit against their original code
	/// </summary>
	public class BatchTwentySevenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void MayCityWorkTileMatchesOriginalCode()
		{
			Check(3, 600, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				(int horizLoc, int vertLoc) = chance.Next(20) == 0 ? (chance.Next(80), chance.Next(2) == 0 ? -1 : 50) : RandomTile(engine, chance);
				ushort resource = (ushort)chance.Next(3);
				bool real = chance.Next(2) == 0;
				ushort counting = (ushort)chance.Next(2);
				bool celebrating = chance.Next(4) == 0;
				short regime = (short)chance.Next(6);

				return new Scenario($"MayCityWorkTile({horizLoc}, {vertLoc}, {resource}), city {townIdx}, real improvements {real}, " +
					$"0x2494 {counting}, celebrating {celebrating}, government {regime}",
					game =>
					{
						short ownerIdx = game.World.Towns[townIdx].ParticipantIdx;
						game.Processor.EmitGlobalWord(0x653e, (ushort)townIdx);
						game.Processor.EmitGlobalWord(0x6548, (ushort)ownerIdx);
						game.Processor.EmitGlobalWord(0x2494, counting);
						game.World.DiagMarkers = (short)(real ? game.World.DiagMarkers | 2 : game.World.DiagMarkers & ~2);
						game.World.Participants[ownerIdx].RegimeKind = regime;
						game.World.Towns[townIdx].ConditionMarker = (byte)(celebrating ? game.World.Towns[townIdx].ConditionMarker | 0x40 :
							game.World.Towns[townIdx].ConditionMarker & ~0x40);
					},
					game => new CityManagementReference(game).MayCityWorkTile(horizLoc, vertLoc, resource),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.CityOps.MayCityWorkTile(horizLoc, vertLoc, resource)),
					Register.Acc | Register.Kept,
					game => vertLoc < 0 || vertLoc >= 50 ? "outside" : real ? "real improvements" : "assumed improvements");
			}, output, ["outside", "real improvements", "assumed improvements"]);
		}

		[Fact]
		public void RenderStatusBarMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				ushort worldMap = (ushort)(chance.Next(6) == 0 ? 1 : 0);
				ushort memory = (ushort)chance.Next(2);
				short truce = (short)chance.Next(0, 40);
				short smog = (short)(chance.Next(2) == 0 ? 0 : chance.Next(1, 20));
				ushort mark = (ushort)(chance.Next(2) == 0 ? chance.Next(0, 3) : chance.Next(0, 30));
				ushort announced = (ushort)(chance.Next(5) == 0 ? 1 : 0);
				ushort silence = (ushort)chance.Next(-3, 2);
				short headway = (short)chance.Next(0, 500);

				// Sometimes the human player's cities of size 1 (a population below 100, counted in tens)
				bool small = chance.Next(3) == 0;

				return new Scenario($"RenderStatusBar(), world map {worldMap}, 0xd806 {memory}, truce {truce}, smog {smog}, mark {mark}, " +
					$"0x1c28 {announced}, 0x3484 {(short)silence}, progress {headway}, small cities {small}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xb278, worldMap);
						game.Processor.EmitGlobalWord(0xd806, memory);
						game.Processor.EmitGlobalWord(0x1c24, mark);
						game.Processor.EmitGlobalWord(0x1c28, announced);
						game.Processor.EmitGlobalWord(0x3484, silence);
						game.World.TruceRoundTally = truce;
						game.World.SmogImpactTier = smog;
						game.World.Participants[game.World.PersonParticipantIdx].InquiryHeadway = headway;

						if (small)
						{
							foreach (var town in game.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == game.World.PersonParticipantIdx))
								town.TrueExtent = 1;
						}
					},
					game => new TurnEventsReference(game).RenderStatusBar(),
					game => game.TurnHooks.RenderStatusBar(),
					Register.Kept,
					game => worldMap == 1 ? "world map" : announced != 0 ? "panel" :
						game.Processor.PullGlobalWord(0x1c24) != mark ? (small ? "small mark passed" : "mark passed") : "mark not passed");
			}, output, ["world map", "panel", "small mark passed", "mark passed", "mark not passed"]);
		}

		[Fact]
		public void DisplayGameResultsScreenMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				ushort rank = (ushort)(chance.Next(4) == 0 ? chance.Next(-3, 0) : chance.Next(0, 25));
				ushort score = (ushort)(chance.Next(2) == 0 ? 0 : chance.Next(0, 3000));
				short tier = (short)chance.Next(5);

				return new Scenario($"DisplayGameResultsScreen({(short)rank}, {score}), difficulty {tier}",
					game => game.World.HardnessTier = tier,
					game => new HallOfFameReference(game).DisplayGameResultsScreen(rank, score),
					game => game.Legends.DisplayGameResultsScreen(rank, score),
					Register.Kept,
					game => score != 0 ? "score" : (short)rank < 0 ? "nothing" : "rank");
			}, output, ["score", "nothing", "rank"]);
		}

		[Fact]
		public void BuyUnitMatchesOriginalCode()
		{
			// Enter takes "Forget it.", the arrow and Enter "Pay"
			int[] keys = { 0x0d, 0x0d, 0x5000, 0x0d, 0x0d, 0x5000, 0x0d, 0x0d };

			Check(3, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short humanIdx = engine.World.PersonParticipantIdx;

				if (RandomUnit(engine, chance) is not (short participantIdx, short troopIdx))
					return null;

				short briberIdx = (short)(chance.Next(3) == 0 ? humanIdx : chance.Next(8));
				short gold = (short)chance.Next(0, 3000);
				short briberGold = (short)chance.Next(0, 5000);
				int built = Calls(pair.Rewritten.Engine, pair, "ConstructUnit");
				int stacked = new UnitMemory(engine.Processor).Following(participantIdx, troopIdx);

				return new Scenario($"BuyUnit({participantIdx}, {troopIdx}, {briberIdx}), gold {gold}, briber's gold {briberGold}",
					game =>
					{
						game.World.Participants[participantIdx].Gold = gold;
						game.World.Participants[briberIdx].Gold = briberGold;
					},
					game => new DiplomatActionsReference(game).BuyUnit(participantIdx, troopIdx, briberIdx),
					game => game.Diplomats.BuyUnit(participantIdx, troopIdx, briberIdx),
					Register.Kept,
					game => Calls(game, pair, "ConstructUnit") > built ? (participantIdx == humanIdx ? "human bribed" : "bribed") :
						stacked != -1 ? "stack or refused" : briberIdx == humanIdx ? "refused" : "too expensive");
			}, output, ["bribed", "refused", "too expensive"], keys);
		}
	}
}
