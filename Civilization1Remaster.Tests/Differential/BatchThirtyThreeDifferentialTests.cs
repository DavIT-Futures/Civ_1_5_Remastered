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
	/// <summary>The change of government, the newspaper, the text input and the intelligence report against their original code</summary>
	public class BatchThirtyThreeDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The replay (segment 0x3725) up to the data segment</summary>
		private static readonly (ushort, ushort, int)[] Replay = [(0x3725, 0, 0x3dc0)];

		[Fact]
		public void SwitchGovernmentMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short human = engine.World.PersonParticipantIdx;
				bool asHuman = chance.Next(3) == 0;
				short participantIdx = asHuman ? human : (short)chance.Next(1, 8);

				if (participantIdx == human && !asHuman)
					return null;

				short regimeKind = (short)(chance.Next(5) == 0 ? 0 : chance.Next(1, 6));
				short former = (short)(chance.Next(4) == 0 ? regimeKind : chance.Next(0, 6));
				ushort overthrown = (ushort)(chance.Next(2) == 0 ? 4 : 0);
				ushort known = (ushort)(chance.Next(4) == 0 ? 0 : 0x40);
				ushort line = (ushort)chance.Next(0, 5);
				bool[] technologies = Enumerable.Range(0, 4).Select(_ => chance.Next(2) == 0).ToArray();
				short wonder = (short)(chance.Next(5) == 0 ? -1 : chance.Next(128));

				bool announced = !asHuman && (former != regimeKind || overthrown != 0) && known != 0;
				string branch = asHuman ? (regimeKind == 0 ? "human, anarchy" : "human, chosen") :
					!announced ? "AI, not announced" : overthrown != 0 ? "AI, changed" : "AI, overthrown";

				return new Scenario($"SwitchGovernment({participantIdx}, {regimeKind}), former {former}, relation 0x{overthrown | known:x2}, line {line}, " +
					$"technologies {string.Join(" ", technologies)}, wonder city {wonder}",
					game =>
					{
						Player participant = game.World.Participants[participantIdx];
						game.World.ParticipantMarkers = (short)(1 << human);
						participant.RegimeKind = former;
						participant.Relations[0] = (ushort)((participant.Relations[0] & ~4) | overthrown);
						ushort[] relations = game.World.Participants[human].Relations;
						relations[participantIdx] = (ushort)((relations[participantIdx] & ~0x40) | known);
						ushort[] science = participant.FoundScienceMarkers;
						science[0] = (ushort)((science[0] & ~0x30) | (technologies[0] ? 0x10 : 0) | (technologies[1] ? 0x20 : 0));
						science[2] = (ushort)((science[2] & ~0x1800) | (technologies[2] ? 0x800 : 0) | (technologies[3] ? 0x1000 : 0));
						game.World.MarvelTownIdx[1] = wonder;
						game.Processor.EmitGlobalWord(0x2f9a, line);
					},
					game => new GovernmentAndDiplomacyReference(game).SwitchGovernment(participantIdx, regimeKind),
					game => game.Politics.SwitchGovernment(participantIdx, regimeKind),
					Register.Kept, game => branch, Replay);
			}, output, ["human, anarchy", "human, chosen", "AI, not announced", "AI, changed", "AI, overthrown"], [0x0d, 0x1b, 0x0d, 0x0d]);
		}

		[Fact]
		public void DisplayNewspaperMatchesOriginalCode()
		{
			Check(3, 24, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].ParticipantIdx == engine.World.PersonParticipantIdx).ToArray();
				short townIdx = chance.Next(3) switch { 0 => -1, 1 => -2, _ => towns[chance.Next(towns.Length)] };
				// The palace in a random city of the human player, or (not for -2: no city then) in none
				short palaceIdx = townIdx != -2 && chance.Next(3) == 0 ? (short)-1 : towns[chance.Next(towns.Length)];

				return new Scenario($"DisplayNewspaper({townIdx}), palace {palaceIdx}",
					game =>
					{
						foreach (short idx in towns)
							game.World.Towns[idx].StructureMarkers0 = (ushort)((game.World.Towns[idx].StructureMarkers0 & ~1) | (idx == palaceIdx ? 1 : 0));
					},
					game => new NewspaperReference(game).DisplayNewspaper(townIdx),
					game => game.Paper.DisplayNewspaper(townIdx),
					Register.Kept,
					game => townIdx == -1 ? (palaceIdx == -1 ? "first city" : "palace city") : townIdx == -2 ? "palace city, not shown" : "given city");
			}, output, ["first city", "palace city", "palace city, not shown", "given city"]);
		}

		/// <summary>Letters, Left / Insert / Delete / Right (0 and the scan code), Backspace, Return and Escape</summary>
		private static readonly int[] EditKeys = { 0x61, 0x62, 0, 0x4b, 0, 0x52, 0x63, 0x0d, 0x0d, 0x64, 0, 0x53, 0x08, 0, 0x4d, 0x65, 0x0d,
			0x1b, 0x0d, 0x08, 0x08, 0x66, 0, 0x4b, 0, 0x4b, 0x67, 0x0d, 0x0d };

		[Fact]
		public void PullTextInputMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				ushort breadth = (ushort)chance.Next(3, 20);
				string text = new string(Enumerable.Range(0, chance.Next(0, 24)).Select(_ => (char)(chance.Next(4) == 0 ? ' ' : 'A' + chance.Next(26))).ToArray());
				short horizLoc = (short)chance.Next(0, 160), vertLoc = (short)chance.Next(10, 150);

				return new Scenario($"PullTextInput({horizLoc}, {vertLoc}, {breadth}), text \"{text}\"",
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Libc.CopyText(0xba06, text)),
					game => new NewCityScreenReference(game).PullTextInput(horizLoc, vertLoc, breadth),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.CityNaming.PullTextInput(horizLoc, vertLoc, breadth)),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 != 0 ? "accepted" : "cancelled");
			}, output, ["accepted", "cancelled"], EditKeys);
		}

		[Fact]
		public void DisplayIntelligenceReportMatchesOriginalCode()
		{
			Check(3, 30, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)chance.Next(1, 8);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short capitalIdx = towns[chance.Next(towns.Length)];
				short hardness = (short)chance.Next(5);
				short leader = (short)chance.Next(14);
				ushort[] relations = Enumerable.Range(0, 8).Select(_ => (ushort)chance.Next(0, 8)).ToArray();
				ushort[] science = Enumerable.Range(0, 5).Select(_ => (ushort)(chance.Next(3) == 0 ? chance.Next(0x10000) : chance.Next(0x10) << chance.Next(12))).ToArray();
				int knownTechnologies = science.Sum(word => Convert.ToString(word, 2).Count(bit => bit == '1'));

				return new Scenario($"DisplayIntelligenceReport({participantIdx}), capital {capitalIdx}, level {hardness}, leader {leader}, " +
					$"{knownTechnologies} technologies",
					game =>
					{
						Player participant = game.World.Participants[participantIdx];
						City capital = game.World.Towns[capitalIdx];
						capital.ParticipantIdx = participantIdx;
						capital.StructureMarkers0 |= 1;
						game.World.HardnessTier = hardness;
						participant.ChiefVisuals = leader;
						relations.CopyTo(participant.Relations, 0);
						science.CopyTo(participant.FoundScienceMarkers, 0);
					},
					game => new AdvisorReportsReference(game).DisplayIntelligenceReport(participantIdx),
					game => game.Briefings.DisplayIntelligenceReport(participantIdx),
					Register.Kept,
					game => $"{(hardness <= 1 ? "military" : "no military")}{(knownTechnologies > 12 ? ", screen full" : "")}");
			}, output, ["military", "no military", "military, screen full", "no military, screen full"], [0x0d]);
		}
	}
}
