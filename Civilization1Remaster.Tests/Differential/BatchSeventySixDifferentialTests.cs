using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventySixDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void LaunchAudienceMatchesOriginalCode()
	{
		// What the audiences did, over the scenarios of all the saves
		HashSet<string> ways = [];

		foreach (int bay in new[] { 4, 9, 13 })
			CheckLaunchAudience(bay, ways);

		foreach (string way in new[] { "exchange", "earned", "logged", "wiped", "dialog", "government", "advice", "finished", "tribute", "gold" })
			Assert.True(ways.Contains(way), $"No audience did '{way}' (did: {string.Join(", ", ways)})");
	}

	private void CheckLaunchAudience(int bay, HashSet<string> ways)
	{
		int step = 0;

		Check(bay, 24, (chance, pair) =>
		{
			// 0: anything, 1: at war with a leader in danger on its continent (it offers peace, a technology, gold, everything),
			// 2: at peace with a calm leader who knows a technology the human player lacks (an exchange)
			int mode = step++ % 3;
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			short[] computers = Enumerable.Range(1, 7)
				.Where(idx => idx != person && engine.World.Towns.Any(town => town.ConditionMarker != 0xff && town.ParticipantIdx == idx))
				.Select(idx => (short)idx).ToArray();
			short participantIdx = computers[chance.Next(computers.Length)];
			ushort arg4 = chance.Next(3) switch { 0 => 0, 1 => 1, _ => 0xffff };

			// The relations both ways (peace 2, war 1, alliance 4, warned 8, contact 0x20, nuclear 0x80), the strength
			ushort relation = (ushort)(new[] { 1, 2, 2 | 8, 1 | 0x20, 2 | 4, 0x80 | 1, 0 }[chance.Next(7)]);
			ushort back = (ushort)(new[] { 1, 2, 0 }[chance.Next(3)]);
			short leaderMight = (short)chance.Next(0, 300);
			short personMight = (short)chance.Next(0, 300);
			short leaderNukes = (short)(chance.Next(4) == 0 ? chance.Next(1, 3) : 0);
			short personNukes = (short)(chance.Next(4) == 0 ? chance.Next(1, 3) : 0);
			short leaderGold = (short)chance.Next(0, 2000);
			short personGold = (short)chance.Next(0, 2000);
			short standing = (short)chance.Next(8);
			short leaderStanding = (short)chance.Next(8);
			short round = (short)chance.Next(400);
			short hardness = (short)chance.Next(5);
			bool diplomacy = chance.Next(4) != 0;
			bool empress = chance.Next(5) == 0;
			short regime = (short)chance.Next(1, 6);
			short personRegime = (short)chance.Next(1, 6);
			short touch = (short)(chance.Next(3) == 0 ? -2 : chance.Next(400));
			int[] continents = Enumerable.Range(0, 15 * 6).Select(_ => chance.Next(4) == 0 ? chance.Next(0, 40) : 0).ToArray();
			int[] answers = Enumerable.Range(0, 40).Select(_ => mode == 2 ? 1 : chance.Next(3)).ToArray();

			if (mode != 0)
			{
				relation = (ushort)(mode == 1 ? 1 : 2);
				back = relation;
				leaderMight = 10;
				personMight = 200;
				leaderNukes = 0;
				personNukes = 0;
				leaderGold = 1000;
				personRegime = (short)chance.Next(1, 4);
				diplomacy = true;
				Array.Clear(continents);

				if (mode == 2 && chance.Next(2) == 0)
				{
					// At peace, but in danger on its continent: a friendly leader doesn't negotiate (when it asked for the audience)
					arg4 = 0;
					continents[6] = 20;
					continents[7] = 1;
					continents[11] = 30;
				}

				if (mode == 1)
				{
					// A big leader the human player isn't at peace with: the advisor warns after the audience
					arg4 = 1;
					standing = 0;
					leaderStanding = 7;

					// Continent 1: two cities of the leader, a weak attack, a strong human defence
					continents[6] = 20;
					continents[7] = 1;
					continents[11] = 30;
				}
			}

			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			int Delta(GameEngine game, string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);

			return new Scenario(
				$"LaunchAudience({participantIdx}, arg4 {arg4:x}), relation {relation:x}/{back:x}, might {leaderMight}/{personMight}, nukes {leaderNukes}/{personNukes}",
				game =>
				{
					Player leader = game.World.Participants[participantIdx];
					Player human = game.World.Participants[person];

					leader.Relations[person] = relation;
					human.Relations[participantIdx] = back;
					leader.MartialMight = leaderMight;
					human.MartialMight = personMight;
					leader.LiveTroops[25] = leaderNukes;
					human.LiveTroops[25] = personNukes;
					leader.Gold = leaderGold;
					human.Gold = personGold;
					human.Standing = standing;
					leader.Standing = leaderStanding;
					leader.RegimeKind = regime;
					human.RegimeKind = personRegime;
					leader.TouchParticipantTimeleft = touch;
					game.World.RoundTally = round;
					game.World.HardnessTier = hardness;
					game.World.DiagMarkers = (short)(diplomacy ? game.World.DiagMarkers | 1 : game.World.DiagMarkers & ~1);

					if (empress)
						leader.ChiefVisuals = 14;

					if (mode == 1)
						leader.TownTally = 5;

					// Mode 2: only the two players are left (the choice "attack someone" is disabled)
					if (mode == 2)
						game.World.LiveRealms = (short)((1 << participantIdx) | (1 << person));

					for (int continentIdx = 1; continentIdx < 15; continentIdx++)
					{
						int at = continentIdx * 6;
						leader.Landmasses[continentIdx].TownTally = (short)(continents[at] / 10);
						leader.Landmasses[continentIdx].Assault = (short)continents[at + 1];
						leader.Landmasses[continentIdx].Guard = (short)continents[at + 2];
						human.Landmasses[continentIdx].TownTally = (short)(continents[at + 3] / 10);
						human.Landmasses[continentIdx].Assault = (short)continents[at + 4];
						human.Landmasses[continentIdx].Guard = (short)continents[at + 5];
					}

					if (mode == 2)
					{
						// The human player knows more technologies, but not the last one the leader knows
						ushort[] own = leader.FoundScienceMarkers;
						ushort[] theirs = human.FoundScienceMarkers;
						int lacking = Enumerable.Range(0, 0x48).LastOrDefault(idx => (own[idx >> 4] & (1 << (idx & 0xf))) != 0, -1);

						for (int wordIdx = 0; wordIdx < theirs.Length; wordIdx++)
							theirs[wordIdx] |= (ushort)(own[wordIdx] | 0x0f0f);

						if (lacking >= 0)
							theirs[lacking >> 4] = (ushort)(theirs[lacking >> 4] & ~(1 << (lacking & 0xf)));
					}

					// Without a mouse the dialogs read the keys: the answers 0 - 3 (that many times Down, then Enter)
					game.Processor.EmitGlobalWord(0x1a3c, 0);

					lock (game.VideoCard.Tokens)
						game.VideoCard.Tokens.Clear();

					Queue<int> pending = new Queue<int>(answers.SelectMany(answer => Enumerable.Repeat(0x5000, answer).Append(0x0d)));
					bool withhold = false;
					int typed = 0;

					game.Processor.InputStarved = waiting =>
					{
						game.Processor.Idle();

						if (!waiting && withhold)
						{
							withhold = false;
							return;
						}

						if (++typed > 400)
							throw new InvalidOperationException("The game asked for more than 400 keys");

						withhold = true;

						lock (game.VideoCard.Tokens)
							game.VideoCard.Tokens.Enqueue(pending.Count > 0 ? pending.Dequeue() : 0x0d);
					};
				},
				game => new MeetWithKingReference(game).LaunchAudience(participantIdx, 10, 10, arg4),
				game => game.Audience.LaunchAudience(participantIdx, 10, 10, arg4),
				Register.Kept,
				// What the audience did, from its calls and the gold
				game =>
				{
					List<string> did = [];

					if (Delta(game, "ChooseTechnologyToTake") > 0)
						did.Add("exchange");
					if (Delta(game, "EarnTechnology") > 0)
						did.Add("earned");
					if (Delta(game, "LogAudience") > 0)
						did.Add("logged");
					if (Delta(game, "WipeDiplomacyFlags") > 0)
						did.Add("wiped");
					if (Delta(game, "DisplayDialog") > 0)
						did.Add("dialog");
					if (Delta(game, "SwitchGovernment") > 0)
						did.Add("government");
					if (Delta(game, "DisplayHelpPopup") > 0)
						did.Add("advice");
					if (Delta(game, "FinishAudience") > 0)
						did.Add("finished");
					if (game.World.Participants[participantIdx].Gold > leaderGold)
						did.Add("tribute");
					if (game.World.Participants[person].Gold > personGold)
						did.Add("gold");

					foreach (string way in did)
						ways.Add(way);

					return did.Count == 0 ? "nothing" : "audience";
				});
		}, output, [], null);
	}
}
