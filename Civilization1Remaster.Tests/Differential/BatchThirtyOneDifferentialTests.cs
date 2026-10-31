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
	/// <summary>The king's picture, the investigation of a city, the trade route and the power ranking against their original code</summary>
	public class BatchThirtyOneDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ChoreographKingLikenessMatchesOriginalCode()
		{
			int made = 0;

			Check(3, 120, (chance, pair) =>
			{
				bool load = made++ == 0;
				short participantIdx = (short)chance.Next(8);
				short regime = (short)chance.Next(6);
				short standing = (short)chance.Next(9);
				ushort technology = (ushort)chance.Next(2);
				ushort king = (ushort)chance.Next(14);
				ushort skip = (ushort)(chance.Next(8) == 0 ? 1 : 0);
				ushort advisors = (ushort)(chance.Next(4) == 0 ? 0 : 1);
				ushort shown = (ushort)chance.Next(4);
				ushort face = chance.Next(4) == 0 ? (ushort)0xffff : (ushort)chance.Next(8);
				ushort mouth = chance.Next(4) == 0 ? (ushort)0xffff : (ushort)chance.Next(8);
				ushort pose = chance.Next(5) == 0 ? (ushort)0xffff : (ushort)chance.Next(3);
				bool drawn = skip == 0 && pose != 0xffff && pose != shown && advisors != 0 && standing > 0;

				return new Scenario($"ChoreographKingLikeness({face}, {mouth}, {pose}) of {participantIdx}, government {regime}, standing {standing}, " +
					$"0x67ee {technology}, king {king}, shown {shown}, advisors {advisors}, 0x67f0 {skip}",
					game =>
					{
						// Pictures for the tables of the advisors (0x804e) and the faces (0x7efe)
						if (load)
						{
							game.Libc.CopyText(0xbb80, "govt0a.pic");
							game.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xbb80, 0);

							for (int idx = 0; idx < 12; idx++)
								game.Processor.EmitGlobalWord((ushort)(0x804e + (idx << 1)),
									game.VideoCard.ScreenToPicture(1, (ushort)((0x50 * (idx & 3)) + 1), 0x65, 0x4e, 0x63));

							game.Libc.CopyText(0xbb80, "king00.pic");
							game.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0xbb80, 0);

							for (int idx = 0; idx < 16; idx++)
								game.Processor.EmitGlobalWord((ushort)(0x7efe + (idx << 1)),
									game.VideoCard.ScreenToPicture(1, (ushort)(0xb5 + ((idx & 3) * 0x10)), 0x43, 0x20, 0x10));
						}

						game.World.Participants[participantIdx].RegimeKind = regime;
						game.World.Participants[participantIdx].Standing = standing;
						game.Processor.EmitGlobalWord(0x67f2, (ushort)participantIdx);
						game.Processor.EmitGlobalWord(0x67ee, technology);
						game.Processor.EmitGlobalWord(0x67f8, king);
						game.Processor.EmitGlobalWord(0x67f0, skip);
						game.Processor.EmitGlobalWord(0x67fa, advisors);
						game.Processor.EmitGlobalWord(0x67e8, shown);
					},
					game => new MeetWithKingReference(game).ChoreographKingLikeness(face, mouth, pose),
					game => game.Audience.ChoreographKingLikeness(face, mouth, pose),
					Register.Kept,
					game => skip != 0 ? "skipped" :
						$"{(drawn ? (regime == 3 ? "advisors, monarchy" : technology != 0 ? "advisors, later" : "advisors") : "no advisors")}" +
						$"{(face == 0xffff ? ", no face" : "")}{(mouth == 0xffff ? ", no mouth" : "")}");
			}, output, ["skipped", "advisors", "advisors, monarchy", "advisors, later", "no advisors", "advisors, no face",
				"no advisors, no mouth"]);
		}

		[Fact]
		public void ExamineCityMatchesOriginalCode()
		{
			ushort? diskPath = null;

			Check(3, 12, (chance, pair) =>
			{
				diskPath ??= pair.Original.Engine.Processor.PullGlobalWord(0x1ad0);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				bool missing = chance.Next(5) == 0;
				ushort colours = (ushort)chance.Next(2);
				int screens = Calls(pair.Rewritten.Engine, pair, "DisplayCityScreen");

				return new Scenario($"ExamineCity({townIdx}), disk missing {missing}, colours {colours}",
					game =>
					{
						game.Libc.CopyText(0xbb80, "NOFILE.XYZ");
						game.Processor.EmitGlobalWord(0x1ad0, missing ? (ushort)0xbb80 : diskPath.Value);
						game.Processor.EmitGlobalWord(0xd762, colours);
					},
					game => new DiplomatActionsReference(game).ExamineCity(townIdx),
					game => game.Diplomats.ExamineCity(townIdx),
					Register.Kept,
					game => missing ? "no disk" : Calls(game, pair, "DisplayCityScreen") > screens ? "city screen" : "scenery");
			// The city screen needs no key waiting after the animation, but with the scripted keyboard the animation only
			// ends on a key, which stays in the buffer
			}, output, ["no disk", "scenery"], [0x0d, 0x1b, 0x0d, 0x0d]);
		}

		[Fact]
		public void FoundTradeRouteMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx).Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short participantIdx = (short)(chance.Next(3) == 0 ? engine.World.PersonParticipantIdx : chance.Next(1, 8));
				short homeIdx = towns[chance.Next(towns.Length)];
				(short, short)? unit = RandomUnit(engine, chance, participantIdx);

				if (unit == null)
					return null;

				short troopIdx = unit.Value.Item2;
				short townIdx = towns[chance.Next(towns.Length)];
				sbyte homeTrade = (sbyte)chance.Next(0, 40), townTrade = (sbyte)chance.Next(0, 40);
				bool owned = chance.Next(3) == 0;
				sbyte[] routes = Enumerable.Range(0, 3).Select(_ => chance.Next(3) == 0 ? (sbyte)-1 : (sbyte)towns[chance.Next(towns.Length)]).ToArray();

				if (chance.Next(5) == 0)
					routes[chance.Next(3)] = (sbyte)townIdx;

				bool[] technologies = { chance.Next(3) == 0, chance.Next(3) == 0 };
				short townOwner = owned ? participantIdx : engine.World.Towns[townIdx].ParticipantIdx;

				// The trade of the routes as the function compares them
				short Trade(int idx)
				{
					sbyte trade = idx == townIdx ? townTrade : idx == homeIdx ? homeTrade : engine.World.Towns[idx].RootCommerce;
					short owner = idx == townIdx ? townOwner : engine.World.Towns[idx].ParticipantIdx;
					return owner == participantIdx ? trade : (short)(trade << 1);
				}

				string outcome = "kept";

				if (routes.Any(route => (byte)route == townIdx))
				{
					outcome = "already a route";
				}
				else
				{
					short least = Trade(townIdx);

					foreach (sbyte route in routes)
					{
						short trade = route == -1 ? (short)-1 : Trade((byte)route);

						if (trade < least)
						{
							least = trade;
							outcome = route == -1 ? "empty slot" : "route replaced";
						}
					}
				}

				return new Scenario($"FoundTradeRoute({participantIdx}, {troopIdx}, {townIdx}), home {homeIdx}, trade {homeTrade} {townTrade}, " +
					$"owned {owned}, routes {string.Join(" ", routes)}, technologies {string.Join(" ", technologies)}",
					game =>
					{
						new UnitMemory(game.Processor).Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)homeIdx);
						game.World.Towns[homeIdx].RootCommerce = homeTrade;
						game.World.Towns[townIdx].RootCommerce = townTrade;

						if (owned)
							game.World.Towns[townIdx].ParticipantIdx = participantIdx;

						routes.CopyTo(game.World.Towns[homeIdx].CommerceTownIndices, 0);

						ushort[] science = game.World.Participants[game.World.Towns[townIdx].ParticipantIdx].FoundScienceMarkers;
						science[3] = (ushort)((science[3] & ~2) | (technologies[0] ? 2 : 0));
						science[2] = (ushort)((science[2] & ~0x40) | (technologies[1] ? 0x40 : 0));
					},
					game => new TradeAndConquestReference(game).FoundTradeRoute(participantIdx, troopIdx, townIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Conquest.FoundTradeRoute(participantIdx, troopIdx, townIdx)),
					Register.Acc | Register.Kept,
					game => $"{(participantIdx == game.World.PersonParticipantIdx ? "human" : "AI")}, {outcome}");
			}, output, ["human, route replaced", "AI, route replaced", "AI, empty slot", "AI, kept", "AI, already a route"], [0x0d]);
		}

		[Fact]
		public void RefreshPeaceGraphContentMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short human = engine.World.PersonParticipantIdx;
				short rounds = (short)(chance.Next(4) == 0 ? chance.Next(600, 700) : chance.Next(0, 600));
				short realms = (short)(chance.Next(3) == 0 ? chance.Next(0x100) : 0xff);
				short[] gold = Enumerable.Range(0, 8).Select(_ => (short)(chance.Next(5) == 0 ? chance.Next(-2000, 0) : chance.Next(0, 5000))).ToArray();
				short[] citizens = Enumerable.Range(0, 8).Select(_ => (short)chance.Next(0, 80)).ToArray();
				short[] technologies = Enumerable.Range(0, 8).Select(_ => (short)chance.Next(0, 70)).ToArray();
				short[] troops = Enumerable.Range(0, 8 * 28).Select(_ => (short)(chance.Next(3) == 0 ? chance.Next(0, 6) : 0)).ToArray();
				ushort[] relations = Enumerable.Range(0, 64).Select(_ => (ushort)chance.Next(0, 0x10)).ToArray();
				short[] cities = Enumerable.Range(0, 8).Select(_ => (short)chance.Next(0, 8)).ToArray();
				bool mighty = chance.Next(2) == 0;

				if (mighty)
				{
					gold[human] = 30000;
					troops[(human * 28) + 25] = (short)(chance.Next(4) == 0 ? 1 : 0);
				}

				return new Scenario($"RefreshPeaceGraphContent(), turn {rounds}, live 0x{realms:x2}, mighty human {mighty}",
					game =>
					{
						game.World.RoundTally = rounds;
						game.World.LiveRealms = realms;

						for (int idx = 0; idx < 8; idx++)
						{
							Player participant = game.World.Participants[idx];
							participant.Gold = gold[idx];
							participant.GrandTownExtent = citizens[idx];
							participant.FoundScienceTally = technologies[idx];
							participant.TownTally = cities[idx];
							Array.Copy(troops, idx * 28, participant.LiveTroops, 0, 28);
							Array.Copy(relations, idx * 8, participant.Relations, 0, 8);
						}
					},
					game => new TurnEventsReference(game).RefreshPeaceGraphContent(),
					game => game.TurnHooks.RefreshPeaceGraphContent(),
					Register.Kept,
					game => game.Processor.PullGlobalWord(0x8078) != 0 ? "feared" : rounds >= 600 ? "after turn 600" :
						game.World.Participants[human].Standing == 7 ? "human first" : "graph",
					[(0x3772, 0, 0x4b0)]);
			}, output, ["feared", "after turn 600", "human first", "graph"]);
		}
	}
}
