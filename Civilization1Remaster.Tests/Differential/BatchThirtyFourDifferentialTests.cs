using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The small palace, the centring of the map, the meeting of two players and the overview of a civilization against their original code</summary>
	public class BatchThirtyFourDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void RenderPalaceMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				short human = pair.Original.Engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(4) == 0 ? human : chance.Next(1, 8));
				short castleTier = (short)chance.Next(-5, 45);
				short horizLoc = (short)chance.Next(5, 240), vertLoc = (short)chance.Next(10, 180);
				short towns = (short)(chance.Next(6) == 0 ? 0 : chance.Next(1, 20));
				ushort root = (ushort)chance.Next(0x10000);
				ushort[] levels = Enumerable.Range(0, 9).Select(_ => chance.Next(4) == 0 ? (ushort)0xffff : (ushort)chance.Next(0, 6)).ToArray();
				ushort[] styles = Enumerable.Range(0, 9).Select(_ => (ushort)chance.Next(0, 3)).ToArray();

				return new Scenario($"RenderPalace({participantIdx}, {castleTier}, {horizLoc}, {vertLoc}), cities {towns}, root {root}",
					game =>
					{
						game.World.Participants[participantIdx].TownTally = towns;
						game.World.ChanceRoot = root;

						for (int idx = 0; idx < 9; idx++)
						{
							game.Processor.EmitGlobalWord((ushort)(0xd91e + (idx << 1)), levels[idx]);
							game.Processor.EmitGlobalWord((ushort)(0xb222 + (idx << 1)), styles[idx]);
						}
					},
					game => new WorldEventsReference(game).RenderPalace(participantIdx, castleTier, horizLoc, vertLoc),
					game => game.TurnHooks.RenderPalace(participantIdx, castleTier, horizLoc, vertLoc),
					Register.Kept,
					game => towns == 0 ? "no cities" : participantIdx == human ? "human" : castleTier > 20 ? "AI, big" : "AI");
			}, output, ["no cities", "human", "AI", "AI, big"]);
		}

		[Fact]
		public void FocusMapViewMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				short human = pair.Original.Engine.World.PersonParticipantIdx;
				short participantIdx = chance.Next(4) == 0 ? (short)chance.Next(8) : human;
				int horizLoc = chance.Next(0, 80), vertLoc = chance.Next(0, 50);
				ushort all = (ushort)(chance.Next(4) == 0 ? 1 : 0);
				bool chart = chance.Next(3) == 0;

				return new Scenario($"FocusMapView({participantIdx}, {horizLoc}, {vertLoc}), all shown {all}, technology 8 {chart}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xd806, all);
						ushort[] science = game.World.Participants[participantIdx].FoundScienceMarkers;
						science[0] = (ushort)((science[0] & ~0x100) | (chart ? 0x100 : 0));
					},
					game => new MapServiceReference(game).FocusMapView(participantIdx, horizLoc, vertLoc),
					game => game.MapOps.FocusMapView(participantIdx, horizLoc, vertLoc),
					Register.Kept,
					game => all != 0 ? "all shown" : horizLoc < 0x20 || horizLoc > 0x20 ? (chart ? "wrapped, from the top" : "wrapped") : "not wrapped");
			}, output, ["all shown", "wrapped", "wrapped, from the top"]);
		}

		[Fact]
		public void BargainPeaceTreatyMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short human = engine.World.PersonParticipantIdx;
				int kind = chance.Next(10);
				short participantIdx1 = (short)(kind == 0 ? 0 : kind is 1 or 2 ? human : chance.Next(1, 8));
				short participantIdx2 = (short)(kind == 3 ? human : chance.Next(1, 8));

				if (participantIdx1 == participantIdx2 || (kind > 3 && (participantIdx1 == human || participantIdx2 == human)))
					return null;

				// Vendetta (8) seldom, so that most meetings come to a decision
				ushort relation1 = (ushort)(chance.Next(0, 0x20) & (chance.Next(5) == 0 ? 0x1f : 0x17));
				ushort relation2 = (ushort)(chance.Next(0, 0x20) & (chance.Next(5) == 0 ? 0x1f : 0x17));

				// With the human player mostly in contact already, so that the turn of the last meeting decides
				if (kind is >= 1 and <= 3 && chance.Next(4) != 0)
					relation1 |= 1;
				ushort known1 = (ushort)(chance.Next(3) == 0 ? 0x40 : 0), known2 = (ushort)(chance.Next(3) == 0 ? 0x40 : 0);
				// One meeting in four comes to a decision (player + player + turn divisible by 4)
				short rounds = (short)(chance.Next(25, 100) * 4 - participantIdx1 - participantIdx2 + (chance.Next(3) == 0 ? chance.Next(1, 4) : 0));
				// The last meeting with the human player around 16 turns ago
				short touched = (short)(rounds - (chance.Next(2) == 0 ? chance.Next(14, 19) : chance.Next(0, 30)));
				short diagnostics = (short)chance.Next(2);
				short[] regimes = { (short)chance.Next(0, 6), (short)chance.Next(0, 6) };
				int[] calls = { Calls(pair.Rewritten.Engine, pair, "LaunchAudience"), Calls(pair.Rewritten.Engine, pair, "SwapTechnology"),
					Calls(pair.Rewritten.Engine, pair, "WipeDiplomacyFlags") };

				return new Scenario($"BargainPeaceTreaty({participantIdx1}, {participantIdx2}), relations 0x{relation1:x2} 0x{relation2:x2}, " +
					$"known 0x{known1:x2} 0x{known2:x2}, turn {rounds}, touched {touched}, diagnostics {diagnostics}, governments {regimes[0]} {regimes[1]}",
					game =>
					{
						if (participantIdx1 == 0)
							return;

						Player participant1 = game.World.Participants[participantIdx1], participant2 = game.World.Participants[participantIdx2];
						participant1.Relations[participantIdx2] = relation1;
						participant2.Relations[participantIdx1] = relation2;
						game.World.Participants[human].Relations[participantIdx1] = (ushort)((game.World.Participants[human].Relations[participantIdx1] & ~0x40) | known1);
						game.World.Participants[human].Relations[participantIdx2] = (ushort)((game.World.Participants[human].Relations[participantIdx2] & ~0x40) | known2);
						game.World.RoundTally = rounds;
						participant1.TouchParticipantTimeleft = touched;
						participant2.TouchParticipantTimeleft = touched;
						participant1.RegimeKind = regimes[0];
						participant2.RegimeKind = regimes[1];
						game.World.DiagMarkers = (short)((game.World.DiagMarkers & ~1) | diagnostics);
					},
					game => new ForeignAffairsReference(game).BargainPeaceTreaty(participantIdx1, participantIdx2, 40, 20),
					game => game.Politics.BargainPeaceTreaty(participantIdx1, participantIdx2, 40, 20),
					Register.Kept,
					game => participantIdx1 == 0 ? "barbarians" : Calls(game, pair, "LaunchAudience") > calls[0] ? "audience" :
						participantIdx1 == human || participantIdx2 == human ? "human, no audience" :
						Calls(game, pair, "SwapTechnology") > calls[1] ? "peace" : Calls(game, pair, "WipeDiplomacyFlags") > calls[2] ? "war" : "nothing");
			}, output, ["barbarians", "audience", "human, no audience", "peace", "war", "nothing"], [0x0d, 0x0d, 0x1b]);
		}

		[Fact]
		public void DisplayCivilizationOverviewMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				ushort[] relations = Enumerable.Range(0, 8).Select(_ => (ushort)chance.Next(0, 0x10)).ToArray();
				short[] continents = Enumerable.Range(0, 32).Select(_ => (short)(chance.Next(3) == 0 ? chance.Next(0, 8) : 0)).ToArray();
				ushort[] science = Enumerable.Range(0, 5).Select(_ => (ushort)(chance.Next(3) == 0 ? 0xffff : chance.Next(3) == 0 ? chance.Next(0x10000) : chance.Next(0x10) << chance.Next(12))).ToArray();
				short[] founders = Enumerable.Range(0, 72).Select(_ => (short)chance.Next(0, 16)).ToArray();
				int knownTechnologies = science.Sum(word => Convert.ToString(word, 2).Count(bit => bit == '1'));

				return new Scenario($"DisplayCivilizationOverview({participantIdx}), {knownTechnologies} technologies",
					game =>
					{
						Player participant = game.World.Participants[participantIdx];
						relations.CopyTo(participant.Relations, 0);
						science.CopyTo(participant.FoundScienceMarkers, 0);
						founders.CopyTo(game.World.ScienceLeadingFoundVia, 0);

						for (int idx = 0; idx < 16; idx++)
						{
							participant.Landmasses[idx].Assault = continents[idx * 2];
							participant.Landmasses[idx].Approach = continents[(idx * 2) + 1];
						}
					},
					game => new RivalOverviewReference(game).DisplayCivilizationOverview(participantIdx),
					game => game.RealmInfo.DisplayCivilizationOverview(participantIdx),
					Register.Kept,
					game => knownTechnologies > 50 ? "screen full" : "all technologies");
			}, output, ["screen full", "all technologies"], [0x0d]);
		}
	}
}
