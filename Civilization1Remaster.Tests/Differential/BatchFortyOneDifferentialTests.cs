using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The king's screen, the world map, the end of a turn (pollution and the rivals' wonders) and a new technology against
	/// their original code
	/// </summary>
	public class BatchFortyOneDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayKingScreenMatchesOriginalCode()
		{
			Check(3, 16, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(1, 8);
				bool preference = chance.Next(2) == 0;
				short regime = (short)chance.Next(6);
				short leader = (short)chance.Next(14);
				ushort cycling = (ushort)chance.Next(2);
				bool technology = chance.Next(2) == 0;
				int scenery = Calls(pair.Rewritten.Engine, pair, "ImportKingScenery");

				return new Scenario($"DisplayKingScreen({participantIdx}), preference bit 8 {preference}, government {regime}, leader {leader}, " +
					$"colour cycles {cycling}, technology 0x14 {technology}",
					game =>
					{
						game.World.SessionPrefMarkers = (short)(preference ? game.World.SessionPrefMarkers | 8 : game.World.SessionPrefMarkers & ~8);
						game.World.Participants[participantIdx].RegimeKind = regime;
						game.World.Participants[participantIdx].ChiefVisuals = leader;
						game.Processor.EmitGlobalWord(0xd762, cycling);
						ushort[] science = game.World.Participants[participantIdx].FoundScienceMarkers;
						science[1] = (ushort)((science[1] & ~0x10) | (technology ? 0x10 : 0));
					},
					game =>
					{
						new RulerAudienceReference(game).ShowKingScreen(participantIdx);
						ShowKingPictures(game);
					},
					game =>
					{
						game.Audience.ShowKingScreen(participantIdx);
						ShowKingPictures(game);
					},
					Register.Kept,
					game => Calls(game, pair, "ImportKingScenery") == scenery ? "line" :
						$"pictures{(game.Processor.PullGlobalWord(0x67fa) != 0 ? ", small advisors" : "")}{(regime == 3 ? ", monarchy" : "")}");
			}, output, ["pictures, small advisors", "pictures, small advisors, monarchy"], [0x0d]);
		}

		/// <summary>
		/// The pictures cut by ShowKingScreen (outside the snapshot) drawn on the screen: the king (0xb1e6), the faces and
		/// mouths (0x7efe / 0x7f00) and the small advisors (0x804e, when 0x67fa is set)
		/// </summary>
		private static void ShowKingPictures(GameEngine game)
		{
			if (game.Processor.PullGlobalWord(0x67f0) != 0)
				return;

			ushort screen = game.Processor.PullGlobalWord(0xaa);

			game.VideoCard.DrawPictureToScreen(screen, 0, 0, game.Processor.PullGlobalWord(0xb1e6));

			for (int idx = 0; idx < 14; idx++)
			{
				game.VideoCard.DrawPictureToScreen(screen, (idx % 5) * 60, 90 + ((idx / 5) * 50), game.Processor.PullGlobalWord((ushort)(0x7efe + (idx << 2))));
				game.VideoCard.DrawPictureToScreen(screen, (idx % 5) * 60, 110 + ((idx / 5) * 50), game.Processor.PullGlobalWord((ushort)(0x7f00 + (idx << 2))));
			}

			if (game.Processor.PullGlobalWord(0x67fa) == 0)
				return;

			for (int idx = 0; idx < 12; idx++)
				game.VideoCard.DrawPictureToScreen(screen, 150 + ((idx % 3) * 30), (idx / 3) * 25, game.Processor.PullGlobalWord((ushort)(0x804e + (idx << 1))));
		}

		[Fact]
		public void DisplayWorldMapMatchesOriginalCode()
		{
			Check(3, 16, (chance, pair) =>
			{
				ushort marker = (ushort)chance.Next(2);
				ushort cheat = (ushort)(chance.Next(3) == 0 ? 1 : 0);
				ushort quiet = (ushort)chance.Next(2);
				bool pole = chance.Next(3) == 0;
				int column = chance.Next(80), poleRow = chance.Next(2);

				return new Scenario($"DisplayWorldMap({marker}), cheat {cheat}, 0xb278 {quiet}, pole seen {pole}",
					game =>
					{
						short person = game.World.PersonParticipantIdx;
						game.Processor.EmitGlobalWord(0xd806, cheat);
						game.Processor.EmitGlobalWord(0xb278, quiet);

						if (pole)
							game.World.ChartSight[column, poleRow] |= (sbyte)(1 << person);
					},
					game => new WorldStatisticsReference(game).ShowWorldMap(marker),
					game => game.Atlas.ShowWorldMap(marker),
					Register.Kept,
					game => $"{(cheat != 0 ? "everything" : "seen")}, marker {marker}");
			}, output, ["everything, marker 0", "everything, marker 1", "seen, marker 0", "seen, marker 1"], [0x0d]);
		}

		[Fact]
		public void HandleEndOfTurnEventsMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short dirty = (short)chance.Next(0, 30), heating = (short)chance.Next(0, 4), smog = (short)chance.Next(0, 20);
				short hardness = (short)chance.Next(0, 5);
				ushort quiet = (ushort)(chance.Next(3) == 0 ? 0 : 1);
				bool free = chance.Next(2) == 0;
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				sbyte[] sizes = towns.Select(_ => (sbyte)chance.Next(1, 30)).ToArray();
				sbyte[] making = towns.Select(_ => (sbyte)(chance.Next(3) == 0 ? -chance.Next(0x19, 0x2e) : chance.Next(0, 20))).ToArray();
				int built = Calls(pair.Rewritten.Engine, pair, "AttachReplayContent");
				int warmed = Calls(pair.Rewritten.Engine, pair, "EnactGlobalWarming");
				int printed = Calls(pair.Rewritten.Engine, pair, "DisplayNewspaper");

				return new Scenario($"HandleEndOfTurnEvents(), polluted {dirty}, warming {heating}, pollution {smog}, level {hardness}, " +
					$"0xb278 {quiet}, wonders free {free}",
					game =>
					{
						game.World.DirtyCellTally = dirty;
						game.World.WorldHeatingTally = heating;
						game.World.SmogImpactTier = smog;
						game.World.HardnessTier = hardness;
						game.Processor.EmitGlobalWord(0xb278, quiet);

						for (int idx = 0; idx < towns.Length; idx++)
						{
							game.World.Towns[towns[idx]].TrueExtent = sizes[idx];
							game.World.Towns[towns[idx]].PresentMakingIdx = making[idx];
						}

						if (free)
						{
							for (int wonderIdx = 1; wonderIdx < 22; wonderIdx++)
								game.World.MarvelTownIdx[wonderIdx] = -1;
						}
					},
					game => new WorldEventsReference(game).HandleEndOfTurnEvents(),
					game => game.TurnHooks.HandleEndOfTurnEvents(),
					Register.Kept,
					game => $"{(Calls(game, pair, "EnactGlobalWarming") > warmed ? "global warming" : Calls(game, pair, "DisplayNewspaper") > printed ? "warming feared" : "pollution")}, " +
						$"{(Calls(game, pair, "AttachReplayContent") > built ? "wonder" : "no wonder")}");
			}, output, ["global warming, no wonder", "pollution, wonder", "pollution, no wonder", "warming feared, no wonder"], [0x0d, 0x1b]);
		}

		[Fact]
		public void EarnTechnologyMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(3) == 0 ? person : chance.Next(1, 8));
				short scienceIdx = (short)(chance.Next(8) == 0 ? 0x47 : chance.Next(0x48));
				short source = chance.Next(4) switch { 0 => participantIdx, 1 => 0, 2 => -2, _ => (short)chance.Next(1, 8) };
				bool known = chance.Next(6) == 0;
				short round = (short)(chance.Next(8) == 0 ? 0 : chance.Next(1, 400));
				bool contact = chance.Next(2) == 0;
				bool first = chance.Next(2) == 0;
				bool emissary = chance.Next(2) == 0;
				ushort audience = (ushort)(chance.Next(3) == 0 ? 0 : 0xffff);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short library = (short)(chance.Next(2) == 0 ? -1 : towns[chance.Next(towns.Length)]);
				short[] wonders = Enumerable.Range(0, 22).Select(_ => chance.Next(2) == 0 ? (short)-1 : towns[chance.Next(towns.Length)]).ToArray();
				short hardness = (short)chance.Next(0, 5);
				int emissaries = Calls(pair.Rewritten.Engine, pair, "WelcomeEmissary");
				int learned = Calls(pair.Rewritten.Engine, pair, "EarnTechnology");

				return new Scenario($"EarnTechnology({participantIdx}, {scienceIdx}, {source}), known {known}, turn {round}, contact {contact}, " +
					$"first {first}, 0x3934 0x{audience:x4}, library {library}, level {hardness}",
					game =>
					{
						ushort[] science = game.World.Participants[participantIdx].FoundScienceMarkers;
						science[scienceIdx >> 4] = (ushort)((science[scienceIdx >> 4] & ~(1 << (scienceIdx & 0xf))) | (known ? 1 << (scienceIdx & 0xf) : 0));
						ushort[] own = game.World.Participants[person].FoundScienceMarkers;

						if (emissary && participantIdx != person && scienceIdx != 0x47)
						{
							own[scienceIdx >> 4] = (ushort)(own[scienceIdx >> 4] & ~(1 << (scienceIdx & 0xf)));
							science[0] |= 1 << 8;
						}

						game.World.RoundTally = round;
						game.World.HardnessTier = hardness;
						game.World.Participants[person].Relations[participantIdx] =
							(ushort)((game.World.Participants[person].Relations[participantIdx] & ~0x40) | (contact ? 0x40 : 0));
						game.World.ScienceLeadingFoundVia[scienceIdx] = (short)(first ? 0 : 3);
						game.Processor.EmitGlobalWord(0x3934, audience);

						for (int wonderIdx = 0; wonderIdx < 22; wonderIdx++)
							game.World.MarvelTownIdx[wonderIdx] = wonders[wonderIdx];

						game.World.MarvelTownIdx[5] = library;
					},
					game => new ResearchOfficeReference(game).EarnTechnology(participantIdx, scienceIdx, source),
					game => game.Advisors.EarnTechnology(participantIdx, scienceIdx, source),
					Register.Kept,
					game => known ? "known" : round == 0 ? "turn 0" :
						$"{(participantIdx == person ? "human" : contact ? "contact" : Calls(game, pair, "WelcomeEmissary") > emissaries ? "emissary" : "no contact")}" +
						$"{(Calls(game, pair, "EarnTechnology") > learned + 1 ? ", Great Library" : "")}");
			}, output, ["known", "turn 0", "human", "contact", "no contact", "emissary"], [0x0d, 0x1b, 0x0d]);
		}
	}
}
