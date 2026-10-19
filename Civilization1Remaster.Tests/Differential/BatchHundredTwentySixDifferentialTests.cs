using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

/// <summary>
/// HandleCity from tables of cases (no random draws): every case starts from the saved game (fresh checkpoint), picks its city
/// by a rule (the first one that fits) and changes the state the same way, so a new case never changes the others. New
/// coverage of HandleCity goes here, the random tests of BatchHundredThirteen stay as they are.
/// </summary>
public sealed class BatchHundredTwentySixDifferentialTests(ITestOutputHelper output)
{
	/// <summary>Which city of the owner a case takes</summary>
	public enum Choice
	{
		First,
		Last,
		Biggest,
	}

	/// <summary>A case: its name, which city (the owner: human or computer; the first, last or biggest), the marker, the changes</summary>
	public sealed record CityCase(string Name, bool Human, short Marker, Action<GameEngine, short> Setup, Choice Pick = Choice.First);

	/// <summary>The shields of a unit kind (its price x the box of the turn of the city)</summary>
	public static int UnitPrice(GameEngine game, int kind) =>
		game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.Price));

	/// <summary>A unit kind that costs at least the price (the first one), so that it is not finished in the turn</summary>
	public static sbyte DearUnit(GameEngine game) => (sbyte)Enumerable.Range(1, 0x1a).First(kind => UnitPrice(game, kind) >= 6);

	private static readonly CityCase[] TurnCases =
	{
		// AdviseHumanProduction
		new("human advisor, no shields (flag 1)", true, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker |= 0x11;
			city.MaterialsTally = 0;
			city.PresentMakingIdx = DearUnit(game);
		}),
		new("human advisor, shields for a unit", true, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker |= 0x10;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = DearUnit(game);
		}),
		new("human advisor, improvement it has (paid)", true, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker |= 0x10;
			city.MaterialsTally = 990;
			city.PresentMakingIdx = -3;
			city.StructureMarkers0 |= 1 << 2;
		}),
		new("human advisor, improvement it lacks", true, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker |= 0x10;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = -3;
			city.StructureMarkers0 &= unchecked((ushort)~(1 << 2));
		}),
		new("human without advisor", true, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker &= 0xef;
			city.MaterialsTally = 0;
		}),

		// BuyForComputerCity
		new("computer with flag 0x10, a unit", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker |= 0x10;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = DearUnit(game);
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer with flag 1, an improvement", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker = (byte)((city.ConditionMarker | 1) & 0xef);
			city.MaterialsTally = 5;
			city.PresentMakingIdx = -3;
			city.StructureMarkers0 &= unchecked((ushort)~(1 << 2));
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer with flag 1, an improvement, no shields", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker = (byte)((city.ConditionMarker | 1) & 0xef);
			city.MaterialsTally = 0;
			city.PresentMakingIdx = -3;
			city.StructureMarkers0 &= unchecked((ushort)~(1 << 2));
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer, production -1", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker &= 0xee;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = -1;
			city.StructureMarkers0 &= 0xfffe;
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer, condition 0x19, no nuclear unit", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker = 0x19;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = DearUnit(game);
			game.World.Participants[city.ParticipantIdx].LiveTroops[25] = 0;
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer, condition 0x19, a nuclear unit", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker = 0x19;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = DearUnit(game);
			game.World.Participants[city.ParticipantIdx].LiveTroops[25] = 1;
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer, rich", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker &= 0xee;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = DearUnit(game);
			game.World.Participants[city.ParticipantIdx].Gold = 3000;
		}),
		new("computer, spaceship part while the human's flies", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			short person = game.World.PersonParticipantIdx;
			city.ConditionMarker &= 0xee;
			city.MaterialsTally = 5;
			city.PresentMakingIdx = -0x17;
			game.World.StarshipMarkers |= (short)(1 << (person + 8));
			game.World.StarshipMarkers &= (short)~(1 << city.ParticipantIdx);
			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer, unit of the role of its continent", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker &= 0xee;
			city.MaterialsTally = 5;
			sbyte kind = DearUnit(game);
			city.PresentMakingIdx = kind;
			ushort role = game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.TroopGroup));
			ushort continent = game.MapOps.ObtainContinentID(city.Place.X, city.Place.Y);

			if (continent < 8)
				game.World.Participants[continent].Landmasses[city.ParticipantIdx].Approach = (short)(role == 1 || role == 2 || role == 5 ? role : 1);

			game.World.Participants[city.ParticipantIdx].Gold = 900;
		}),
		new("computer, no purchase", false, 0, (game, town) =>
		{
			var city = game.World.Towns[town];
			city.ConditionMarker &= 0xee;
			city.MaterialsTally = 0;
			city.PresentMakingIdx = DearUnit(game);
			game.World.Participants[city.ParticipantIdx].Gold = 100;
		}),
	};

	/// <summary>A city of the owner other than the given one (for trade routes), the first that fits</summary>
	private static int OtherCity(GameEngine game, short town, bool sameOwner) => Enumerable.Range(0, 128).First(other =>
		other != town && game.World.Towns[other].ConditionMarker != 0xff &&
		(game.World.Towns[other].ParticipantIdx == game.World.Towns[town].ParticipantIdx) == sameOwner);

	private static Action<GameEngine, short> Government(short regime, bool courthouse, bool palace) => (game, town) =>
	{
		var city = game.World.Towns[town];
		game.World.Participants[city.ParticipantIdx].RegimeKind = regime;
		city.StructureMarkers0 = (ushort)(courthouse ? city.StructureMarkers0 | 0x40 : city.StructureMarkers0 & ~0x40);
		city.StructureMarkers0 = (ushort)(palace ? city.StructureMarkers0 | 1 : city.StructureMarkers0 & ~1);
	};

	private static Action<GameEngine, short> Routes(bool foreign, bool own) => (game, town) =>
	{
		var city = game.World.Towns[town];
		city.CommerceTownIndices[0] = (sbyte)(foreign ? OtherCity(game, town, false) : -1);
		city.CommerceTownIndices[1] = (sbyte)(own ? OtherCity(game, town, true) : -1);
		city.CommerceTownIndices[2] = -1;
	};

	private static readonly CityCase[] TradeCases =
	{
		// RenderCityScreenFrames (the city screen keeps 0x2496 as set by HandleCity: 0 at its start)
		new("city screen", true, 1, (game, town) => { }),
		new("city screen, the mini map tab clicked", true, 1, (game, town) => HumanTurnScript.Install(game,
			[new HumanTurnScript.Input(0, 1, 177, 110), new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)])),

		// CountCorruptionAndTradeRoutes
		new("despotism", false, -1, Government(0, false, false)),
		new("communism", false, -1, Government(3, false, false)),
		new("democracy", false, -1, Government(5, false, false)),
		new("monarchy with a courthouse", false, -1, Government(2, true, false)),
		new("monarchy with the palace", false, -1, Government(2, false, true)),
		new("communism with the palace (distance 10)", false, -1, Government(3, false, true)),
		new("republic, routes to a foreign and an own city", false, -1, Routes(true, true)),
		new("human, a route to a foreign city", true, -1, Routes(true, false)),
		new("human, a route to an own city", true, -1, Routes(false, true)),
	};

	[Fact]
	public void HandleCityTradeCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, TradeCases, ways);
	}

	private static Action<GameEngine, short> Rates(short levy, short research) => (game, town) =>
	{
		var player = game.World.Participants[game.World.Towns[town].ParticipantIdx];
		player.LevyRatio = levy;
		player.ResearchLevyRatio = research;
	};

	private static Action<GameEngine, short> Buildings(ushort set, ushort clear) => (game, town) =>
	{
		var city = game.World.Towns[town];
		city.StructureMarkers0 = (ushort)((city.StructureMarkers0 & ~clear) | set);
	};

	private static Action<GameEngine, short> Then(params Action<GameEngine, short>[] steps) => (game, town) =>
	{
		foreach (var step in steps)
			step(game, town);
	};

	private static Action<GameEngine, short> Level(short hardness, short regime) => (game, town) =>
	{
		game.World.HardnessTier = hardness;
		game.World.Participants[game.World.Towns[town].ParticipantIdx].RegimeKind = regime;
	};

	private static readonly CityCase[] SplitCases =
	{
		// SplitTrade
		new("taxes 3, science 4 (luxuries 3)", false, -1, Rates(3, 4)),
		new("taxes 10", false, -1, Rates(10, 0)),
		new("science 10", true, -1, Rates(0, 10)),
		new("democracy, a courthouse", false, -1, Then(Level(1, 5), Buildings(0x40, 0))),
		new("library", false, -1, Then(Rates(3, 3), Buildings(0x10, 0x200))),
		new("library and university", false, -1, Then(Rates(3, 3), Buildings(0x210, 0))),
		new("biggest city, library", false, -1, Then(Rates(3, 3), Buildings(0x10, 0x200)), Choice.Biggest),
		new("biggest city, library and university", false, -1, Then(Rates(3, 3), Buildings(0x210, 0)), Choice.Biggest),
		new("scientists", false, -1, (game, town) =>
		{
			// No workers listed, all specialists scientists (2 bits a slot)
			var city = game.World.Towns[town];
			city.LaborerMarkers0 = 0;
			city.LaborerMarkers1 = (ushort)(Math.Min((int)city.TrueExtent, 8) << 10);
			city.LaborerMarkers2 = 0xaaaa;
		}, Choice.Biggest),

		// CountScienceAndContent
		new("marketplace", false, -1, Buildings(0x20, 0x800)),
		new("marketplace and bank", false, -1, Buildings(0x820, 0)),
		new("marketplace and the wonder 0xc", false, -1, Then(Buildings(0x20, 0x800),
			(game, town) => game.World.MarvelTownIdx[0xc] = town)),
		new("bank and the wonder 0xc", false, -1, Then(Buildings(0x800, 0x20),
			(game, town) => game.World.MarvelTownIdx[0xc] = town)),
		new("the city of the wonder 10", false, -1, (game, town) => game.World.MarvelTownIdx[10] = town),
		new("human, level 0, despotism", true, -1, Level(0, 1)),
		new("human, level 4, republic", true, -1, Level(4, 4)),
		new("human, level 2, democracy", true, -1, Level(2, 5)),
		new("human, last city, level 1, monarchy", true, -1, Level(1, 2), Choice.Last),
		new("human, small city, many content", true, -1, Then(Level(0, 6), (game, town) => game.World.Towns[town].TrueExtent = 1)),

		// ApplyLuxuries (the tab 1 of the city screen clicked)
		new("city screen, tab 1, luxuries", true, 1, Then(Rates(2, 2), (game, town) => HumanTurnScript.Install(game,
			[new HumanTurnScript.Input(0, 1, 144, 110), new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]))),
		new("city screen, tab 1, no luxuries", true, 1, Then(Rates(5, 5), (game, town) => HumanTurnScript.Install(game,
			[new HumanTurnScript.Input(0, 1, 144, 110), new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]))),
	};

	[Fact]
	public void HandleCitySplitCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, SplitCases, ways);
	}

	[Fact]
	public void HandleCityTurnCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, TurnCases, ways);

		foreach (string way in new[] { "advisor picked", "advisor idle", "bought", "no purchase" })
			Assert.True(ways.Contains(way), $"No case met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	/// <summary>Runs the cases on one saved game, each from the checkpoint; the first city of the owner that fits</summary>
	public void CheckCases(int bay, CityCase[] cases, HashSet<string> ways)
	{
		int caseIdx = 0;

		Check(bay, cases.Length, (chance, pair) =>
		{
			CityCase current = cases[caseIdx++];
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			var fitting = Enumerable.Range(0, 128).Where(town => engine.World.Towns[town].ConditionMarker != 0xff &&
				(engine.World.Towns[town].ParticipantIdx == person) == current.Human && engine.World.Towns[town].ParticipantIdx != 0 &&
				(current.Human || (engine.World.ParticipantMarkers & (1 << engine.World.Towns[town].ParticipantIdx)) == 0)).ToList();
			int found = fitting.Count == 0 ? -1 : current.Pick switch
			{
				Choice.Last => fitting[^1],
				Choice.Biggest => fitting.OrderByDescending(town => engine.World.Towns[town].TrueExtent).ThenBy(town => town).First(),
				_ => fitting[0],
			};

			if (found < 0)
				return null;

			short townIdx = (short)found;
			short owner = engine.World.Towns[townIdx].ParticipantIdx;
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			short gold = 0;

			return new Scenario(
				$"{current.Name}: HandleCity({townIdx}, {current.Marker})",
				game =>
				{
					HumanTurnScript.Install(game, [new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);
					current.Setup(game, townIdx);
					gold = game.World.Participants[owner].Gold;
				},
				game => new CityManagementReference(game).HandleCity(townIdx, current.Marker),
				game => game.CityOps.HandleCity(townIdx, current.Marker),
				Register.Kept,
				// What the turn did, from the calls and the gold
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (current.Human)
						met.Add(Delta("PickCityProduction") > 0 ? "advisor picked" : "advisor idle");
					else
						met.Add(game.World.Participants[owner].Gold < gold ? "bought" : "no purchase");

					foreach (string way in met)
						ways.Add(way);

					return string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x180), 0x180)]);
		}, output, [], null, fresh: true);
	}
}
