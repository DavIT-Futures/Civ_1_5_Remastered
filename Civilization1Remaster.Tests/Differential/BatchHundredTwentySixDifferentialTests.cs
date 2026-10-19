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

	private static Action<GameEngine, short> Diag(bool on) => (game, town) =>
		game.World.DiagMarkers = (short)(on ? game.World.DiagMarkers | 2 : game.World.DiagMarkers & ~2);

	private static Action<GameEngine, short> Knows(int advance, bool known) => (game, town) =>
	{
		ushort[] science = game.World.Participants[game.World.Towns[town].ParticipantIdx].FoundScienceMarkers;
		science[advance >> 4] = (ushort)(known ? science[advance >> 4] | (1 << (advance & 0xf)) : science[advance >> 4] & ~(1 << (advance & 0xf)));
	};

	private static Action<GameEngine, short> Wonder(int wonder, bool here) => (game, town) =>
		game.World.MarvelTownIdx[wonder] = (short)(here ? town : -1);

	private static Action<GameEngine, short> Size(sbyte size) => (game, town) => game.World.Towns[town].TrueExtent = size;

	private static readonly Action<GameEngine, short> TabOne = (game, town) => HumanTurnScript.Install(game,
		[new HumanTurnScript.Input(0, 1, 144, 110), new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);

	/// <summary>The first unit of the owner made an attack unit of the city, away from it</summary>
	private static readonly Action<GameEngine, short> UnitAway = (game, town) =>
	{
		var city = game.World.Towns[town];
		int troop = Enumerable.Range(0, 128).First(idx => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(city.ParticipantIdx, idx) + UnitMemory.Kind)) != 0xff);
		ushort unit = UnitMemory.Address(city.ParticipantIdx, troop);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 2);
		game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), (byte)town);

		if (game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)) == (byte)city.Place.X)
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)((city.Place.X + 10) % 80));
	};

	private static readonly CityCase[] HappinessCases =
	{
		// ApplyContentImprovements
		new("colosseum", false, -1, Then(Diag(true), Size(12), Buildings(0x2000, 0x408)), Choice.Biggest),
		new("cathedral, 0x1d known", false, -1, Then(Diag(true), Size(12), Buildings(0x400, 0x2008), Knows(0x1d, true), Wonder(9, false)), Choice.Biggest),
		new("cathedral, 0x1d known, the wonder 9", false, -1, Then(Diag(true), Size(12), Buildings(0x400, 0x2008), Knows(0x1d, true), Wonder(9, true)), Choice.Biggest),
		new("cathedral, 0x1d unknown", false, -1, Then(Diag(true), Size(12), Buildings(0x400, 0x2008), Knows(0x1d, false)), Choice.Biggest),
		new("temple, 0x1a known", false, -1, Then(Diag(true), Size(12), Buildings(8, 0x2400), Knows(0x1a, true), Wonder(6, false)), Choice.Biggest),
		new("temple, 0x19 known", false, -1, Then(Diag(true), Size(12), Buildings(8, 0x2400), Knows(0x1a, false), Knows(0x19, true), Wonder(6, false)), Choice.Biggest),
		new("temple, neither", false, -1, Then(Diag(true), Size(12), Buildings(8, 0x2400), Knows(0x1a, false), Knows(0x19, false), Wonder(6, false)), Choice.Biggest),
		new("temple and the wonder 6, 0x1a known", false, -1, Then(Diag(true), Size(12), Buildings(8, 0x2400), Knows(0x1a, true), Wonder(6, true)), Choice.Biggest),
		new("temple and the wonder 6, 0x1a unknown", false, -1, Then(Diag(true), Size(12), Buildings(8, 0x2400), Knows(0x1a, false), Wonder(6, true)), Choice.Biggest),
		new("city screen, tab 1, the temple alone", true, 1, Then(Diag(true), Buildings(8, 0x2400), Knows(0x1a, true), TabOne), Choice.Biggest),
		new("city screen, tab 1, all three", true, 1, Then(Diag(true), Buildings(0x2408, 0), Knows(0x1d, true), Knows(0x1a, true), TabOne), Choice.Biggest),

		// ApplyMilitaryHappiness
		new("despotism, units in the city", false, -1, Then(Diag(true), Size(12), Level(1, 1)), Choice.Biggest),
		new("communism, units in the slots", false, -1, Then(Diag(true), Size(12), Level(1, 3), (game, town) =>
		{
			game.World.Towns[town].Unfamiliar[0] = 2;
			game.World.Towns[town].Unfamiliar[1] = 0xe;
		}), Choice.Biggest),
		new("republic, units away", false, -1, Then(Diag(true), Size(12), Level(1, 4), Wonder(16, false), UnitAway), Choice.Biggest),
		new("republic, units away, the wonder 16", false, -1, Then(Diag(true), Size(12), Level(1, 4), Wonder(16, true), UnitAway), Choice.Biggest),
		new("democracy, units away", false, -1, Then(Diag(true), Size(12), Level(1, 5), Wonder(16, false), UnitAway), Choice.Biggest),
		new("communism, units in the slots, last city", false, -1, Then(Diag(true), Size(12), Level(1, 3), (game, town) =>
		{
			game.World.Towns[town].Unfamiliar[0] = 2;
			game.World.Towns[town].Unfamiliar[1] = 0xe;
		}), Choice.Last),
		new("human, despotism, city screen tab 1", true, 1, Then(Diag(true), Level(1, 1), TabOne), Choice.Biggest),
		new("human, democracy, city screen tab 1", true, 1, Then(Diag(true), Level(1, 5), Wonder(16, false), TabOne), Choice.Biggest),

		// ApplyWonderHappiness
		new("the wonders 2 and 0x15", false, -1, Then(Diag(true), Wonder(2, true), Wonder(0x15, true)), Choice.Biggest),
		new("the city of the wonder 11", false, -1, Then(Diag(true), Size(12), Wonder(11, true)), Choice.Biggest),
		new("the wonder 13 on its continent", false, -1, Then(Diag(true), Size(12), Wonder(13, true)), Choice.Biggest),
		new("the wonder 13 elsewhere", false, -1, Then(Diag(true), Size(12), (game, town) =>
			game.World.MarvelTownIdx[13] = (short)OtherCity(game, town, true)), Choice.Biggest),
		new("without the bit 2", false, -1, Then(Diag(false), Size(12)), Choice.Biggest),
		new("city screen, tab 1, the wonders", true, 1, Then(Diag(true), Wonder(2, true), Wonder(11, true), TabOne), Choice.Biggest),
	};

	[Fact]
	public void HandleCityHappinessCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, HappinessCases, ways);
	}

	/// <summary>The first units of the owner made units of the city (the kind; settlers every third one when asked)</summary>
	private static Action<GameEngine, short> Supported(int count, byte kind, bool settlers) => (game, town) =>
	{
		var city = game.World.Towns[town];
		var units = Enumerable.Range(0, 128).Where(idx => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(city.ParticipantIdx, idx) + UnitMemory.Kind)) != 0xff).Take(count).ToList();

		for (int idx = 0; idx < units.Count; idx++)
		{
			ushort unit = UnitMemory.Address(city.ParticipantIdx, units[idx]);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), (byte)(settlers && idx % 3 == 0 ? 0 : kind));
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), (byte)town);
		}
	};

	private static readonly CityCase[] SupportCases =
	{
		new("computer, small city, many units", false, 0, Then(Size(1), Supported(12, 2, false))),
		new("human, small city, many units", true, 0, Then(Size(1), Supported(12, 2, false))),
		new("computer, republic, flag 1, units", false, 0, Then(Level(1, 4), (game, town) => game.World.Towns[town].ConditionMarker |= 1,
			Supported(10, 2, true)), Choice.Biggest),
		new("computer, democracy, flag 1, behind the human (0x8078)", false, 0, Then(Level(1, 5), (game, town) =>
		{
			game.World.Towns[town].ConditionMarker |= 1;
			game.Processor.EmitUInt16(0x3b01, 0x8078, 1);
			game.World.Participants[game.World.Towns[town].ParticipantIdx].FoundScienceTally = 0;
		}, Supported(10, 2, false)), Choice.Biggest),
		new("computer, monarchy, bit 2, units", false, 0, Then(Level(1, 2), Diag(true), Supported(8, 2, true)), Choice.Biggest),
		new("city screen, units and settlers", true, 1, Then(Level(1, 2), Supported(8, 2, true)), Choice.Biggest),
		new("city screen, democracy, units away", true, 1, Then(Level(1, 5), Wonder(16, false), Supported(8, 2, false), UnitAway), Choice.Biggest),
		new("city screen, despotism, settlers", true, 1, Then(Level(1, 1), Supported(6, 0, false)), Choice.Biggest),
		new("computer, republic, flag 1, settlers only", false, 0, Then(Level(1, 4), (game, town) => game.World.Towns[town].ConditionMarker |= 1,
			Supported(10, 0, false)), Choice.Biggest),
		new("city screen, many units in the city", true, 1, (game, town) =>
		{
			// Eight units of the human moved into the city
			var city = game.World.Towns[town];
			var units = Enumerable.Range(0, 128).Where(idx => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(city.ParticipantIdx, idx) + UnitMemory.Kind)) != 0xff).Take(8);

			foreach (int troop in units)
			{
				ushort unit = UnitMemory.Address(city.ParticipantIdx, troop);
				game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)city.Place.X);
				game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)city.Place.Y);
			}
		}, Choice.Biggest),
		new("city screen, the supported units tab", true, 1, Then(Supported(8, 2, true), (game, town) => HumanTurnScript.Install(game,
			[new HumanTurnScript.Input(0, 1, 110, 110), new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)])), Choice.Biggest),
	};

	[Fact]
	public void HandleCitySupportCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, SupportCases, ways);
	}

	private static readonly CityCase[] ListCases =
	{
		// CountSupportCosts
		new("turn, units above the shields, a library", false, 0, Then(Size(1), Buildings(0x10, 0), Supported(12, 2, false), UnitAway)),
		new("turn, units away, no library", false, 0, Then(Level(1, 4), Buildings(0, 0x10), Supported(6, 2, false), UnitAway), Choice.Biggest),

		// RenderImprovementList, FinishImprovementList
		new("city screen, wonders and a few improvements", true, 1, Then(Wonder(2, true), Wonder(11, true), Wonder(0x15, true),
			(game, town) => game.World.Towns[town].StructureMarkers0 = 0x0019), Choice.Biggest),
		new("city screen, every improvement (a second page)", true, 1, Then(Wonder(5, true), (game, town) =>
		{
			game.World.Towns[town].StructureMarkers0 = 0xfffe;
			game.World.Towns[town].StructureMarkers1 = 0x00ff;
		}), Choice.Biggest),
		new("city screen, an improvement in production", true, 1, (game, town) => game.World.Towns[town].PresentMakingIdx = -5, Choice.Biggest),
		new("city screen, a unit in production", true, 1, (game, town) => game.World.Towns[town].PresentMakingIdx = 3, Choice.Biggest),
		new("city screen, settlers in production", true, 1, (game, town) => game.World.Towns[town].PresentMakingIdx = 0, Choice.Biggest),
	};

	[Fact]
	public void HandleCityListCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, ListCases, ways);
	}

	private static Action<GameEngine, short> Stock(short shields, short food, sbyte making) => (game, town) =>
	{
		var city = game.World.Towns[town];
		city.MaterialsTally = shields;
		city.GrainTally = food;
		city.PresentMakingIdx = making;
	};

	private static readonly CityCase[] BoxCases =
	{
		new("city screen, no shields, no food, a unit", true, 1, Stock(0, 0, 3), Choice.Biggest),
		new("city screen, some shields, a dear unit", true, 1, Stock(37, 15, 0xe), Choice.Biggest),
		new("city screen, many shields, an improvement", true, 1, Stock(450, 60, -7), Choice.Biggest),
		new("city screen, a wonder in production, 990 shields", true, 1, Then(Stock(990, 5, -0x1d), Wonder(0x1d - 24, false)), Choice.Biggest),
		new("city screen, the advisor flag", true, 1, Then(Stock(12, 5, 2), (game, town) => game.World.Towns[town].ConditionMarker |= 0x10), Choice.Biggest),
		new("city screen, a granary, small city", true, 1, Then(Stock(5, 8, 2), Buildings(4, 0), Size(1)), Choice.First),
		new("city screen, a big city, full store", true, 1, Then(Stock(5, 300, 2), Buildings(0, 4), Size(20)), Choice.Biggest),
		new("city screen, size 9, food above the box", true, 1, Then(Stock(5, 999, 2), Size(9)), Choice.Last),
	};

	[Fact]
	public void HandleCityBoxCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, BoxCases, ways);
	}

	private static readonly CityCase[] RowCases =
	{
		new("city screen, settlers and units supported", true, 1, Then(Level(1, 2), Supported(10, 2, true)), Choice.Biggest),
		new("city screen, despotism far from the palace", true, 1, Then(Level(1, 1), Buildings(0, 0x41)), Choice.Last),
		new("city screen, luxuries only", true, 1, Rates(0, 0), Choice.Biggest),
		new("city screen, taxes only", true, 1, Rates(10, 0), Choice.Biggest),
		new("city screen, science only", true, 1, Rates(0, 10), Choice.Biggest),
		new("city screen, a small city", true, 1, Then(Size(1), Rates(4, 4)), Choice.First),
	};

	[Fact]
	public void HandleCityRowCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, RowCases, ways);
	}

	private static readonly Action<GameEngine, short> MiniMapTab = (game, town) => HumanTurnScript.Install(game,
		[new HumanTurnScript.Input(0, 1, 177, 110), new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);

	private static readonly CityCase[] ShortfallCases =
	{
		new("city screen, settlers eat more than the food", true, 1, Then(Size(1), Supported(8, 0, false)), Choice.First),
		new("city screen, republic, settlers eat more than the food", true, 1, Then(Level(1, 4), Size(2), Supported(6, 0, false)), Choice.Last),
		new("city screen, support above the production", true, 1, Then(Level(1, 4), Size(1), Supported(12, 2, false)), Choice.First),
		new("city screen, corruption far from the palace", true, 1, Then(Level(1, 1), Buildings(0, 0x41)), Choice.Biggest),
		new("city screen, trade routes", true, 1, Routes(true, true), Choice.Biggest),
		new("city screen, trade routes on the mini map", true, 1, Then(Routes(true, true), MiniMapTab), Choice.Biggest),
		new("city screen, a big city with factories", true, 1, Then(Size(25), Buildings(0xc000, 0x1000)), Choice.Biggest),
		new("city screen, a middle city with a factory", true, 1, Then(Size(12), Buildings(0x4000, 0x1000)), Choice.Last),
		new("city screen, pollution of the advances", true, 1, Then(Size(20), Buildings(0xc000, 0x1000), Knows(0x23, true), Knows(0x3a, true),
			Knows(0x29, true), Knows(0x3c, true)), Choice.Biggest),
	};

	[Fact]
	public void HandleCityShortfallCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, ShortfallCases, ways);
	}

	private static Action<GameEngine, short> Keys(params HumanTurnScript.Input[] inputs) => (game, town) => HumanTurnScript.Install(game,
		[.. inputs, new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);

	private static Action<GameEngine, short> Advisor(bool on) => (game, town) => game.World.Towns[town].ConditionMarker =
		(byte)(on ? game.World.Towns[town].ConditionMarker | 0x10 : game.World.Towns[town].ConditionMarker & ~0x10);

	private static HumanTurnScript.Input Held(int horiz, int vert) => new HumanTurnScript.Input(0, 2, horiz, vert);

	private static readonly CityCase[] ButtonCases =
	{
		new("city screen, an improvement it has (blinks)", true, 1, Then(Stock(5, 5, -3), Buildings(4, 0)), Choice.Biggest),
		new("city screen, an improvement it lacks", true, 1, Then(Stock(5, 5, -3), Buildings(0, 4)), Choice.First),
		new("city screen, the improvement 21 it has", true, 1, Then(Stock(5, 5, -21), (game, town) => game.World.Towns[town].StructureMarkers1 |= 0x10), Choice.Last),
		new("city screen, a spaceship part, launched", true, 1, Then(Stock(5, 5, -22),
			(game, town) => game.World.StarshipMarkers |= (short)(1 << game.World.Towns[town].ParticipantIdx)), Choice.Biggest),
		new("city screen, a spaceship part, not launched", true, 1, Then(Stock(5, 5, -24),
			(game, town) => game.World.StarshipMarkers &= (short)~(1 << game.World.Towns[town].ParticipantIdx)), Choice.Biggest),
		new("city screen, a wonder built", true, 1, Then(Stock(5, 5, -0x1d), Wonder(0x1d - 24, true)), Choice.Biggest),
		new("city screen, a wonder not built", true, 1, Then(Stock(5, 5, -0x19), Wonder(1, false)), Choice.Last),
		new("city screen, a unit with 127 units", true, 1, Then(Stock(5, 5, 2),
			(game, town) => game.World.Participants[game.World.Towns[town].ParticipantIdx].TroopTally = 127), Choice.Biggest),
		new("city screen, a unit with 126 units", true, 1, Then(Stock(5, 5, 2),
			(game, town) => game.World.Participants[game.World.Towns[town].ParticipantIdx].TroopTally = 126), Choice.Biggest),
		new("city screen, A switches the advisor on, a unit", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(new HumanTurnScript.Input(0x41))), Choice.Biggest),
		new("city screen, A switches the advisor on, an improvement", true, 1, Then(Stock(5, 5, -3), Advisor(false), Keys(new HumanTurnScript.Input(0x41))), Choice.Last),
		new("city screen, A switches the advisor on, settlers", true, 1, Then(Stock(5, 5, 0), Advisor(false), Keys(new HumanTurnScript.Input(0x41))), Choice.First),
		new("city screen, the last spaceship part, launched", true, 1, Then(Stock(5, 5, -24),
			(game, town) => game.World.StarshipMarkers |= (short)(1 << game.World.Towns[town].ParticipantIdx)), Choice.Last),
		new("city screen, A switches the advisor off", true, 1, Then(Stock(5, 5, 2), Advisor(true), Keys(new HumanTurnScript.Input(0x41))), Choice.Biggest),
		new("city screen, Change held at its corner", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(Held(230, 106))), Choice.Biggest),
		new("city screen, Change held at its bottom", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(Held(269, 116))), Choice.First),
		new("city screen, held right of Change", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(Held(270, 110))), Choice.Biggest),
		new("city screen, held below Change", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(Held(250, 117))), Choice.Biggest),
		new("city screen, held above Change", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(Held(250, 105))), Choice.Biggest),
		new("city screen, held left of Change", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(Held(229, 110))), Choice.Biggest),
		new("city screen, Change clicked", true, 1, Then(Stock(5, 5, 2), Advisor(false), Keys(new HumanTurnScript.Input(0, 1, 250, 110))), Choice.Biggest),
		new("computer city screen", false, 1, Stock(5, 5, 2), Choice.Biggest),
	};

	[Fact]
	public void HandleCityButtonCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, ButtonCases, ways);
	}

	private static Action<GameEngine, short> Gold(short gold) => (game, town) =>
		game.World.Participants[game.World.Towns[town].ParticipantIdx].Gold = gold;

	private static HumanTurnScript.Input Key(int key) => new HumanTurnScript.Input(key);

	private static HumanTurnScript.Input Click(int horiz, int vert) => new HumanTurnScript.Input(0, 1, horiz, vert);

	private static readonly CityCase[] PurchaseCases =
	{
		new("B, a unit, bought", true, 1, Then(Stock(5, 5, 2), Gold(30000), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, a unit, too little gold", true, 1, Then(Stock(5, 5, 2), Gold(3), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, a unit, refused", true, 1, Then(Stock(5, 5, 2), Gold(30000), Keys(Key(0x62), Key(0x1b))), Choice.Last),
		new("B, a dear unit without shields", true, 1, Then(Stock(0, 5, 0xe), Gold(30000), Keys(Key(0x62), Key(0x0d))), Choice.First),
		new("B, a unit with all its shields", true, 1, Then(Stock(400, 5, 2), Gold(100), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, a unit in civil disorder", true, 1, Then(Stock(5, 5, 2), Gold(30000), (game, town) => game.World.Towns[town].ConditionMarker |= 1,
			Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, an improvement, bought", true, 1, Then(Stock(7, 5, -3), Buildings(0, 4), Gold(30000), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, an improvement without shields", true, 1, Then(Stock(0, 5, -4), Gold(30000), Keys(Key(0x62), Key(0x0d))), Choice.Last),
		new("B, the improvement 21", true, 1, Then(Stock(9, 5, -21), Gold(30000), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, a spaceship part", true, 1, Then(Stock(9, 5, -22), Gold(30000), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("B, a wonder, in civil disorder", true, 1, Then(Stock(30, 5, -0x1d), Wonder(0x1d - 24, false), Gold(30000),
			(game, town) => game.World.Towns[town].ConditionMarker |= 1, Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("Buy clicked at its left", true, 1, Then(Stock(5, 5, 2), Gold(30000), Keys(Click(296, 106), Key(0x0d))), Choice.Biggest),
		new("clicked left of Buy", true, 1, Then(Stock(5, 5, 2), Gold(30000), Keys(Click(295, 116))), Choice.Biggest),
		new("clicked below Buy", true, 1, Then(Stock(5, 5, 2), Gold(30000), Keys(Click(300, 117))), Choice.Biggest),
		new("C, the production changed", true, 1, Then(Stock(5, 5, 2), Keys(Key(0x63), Key(0x0d))), Choice.Biggest),
		new("C, an improvement changed", true, 1, Then(Stock(5, 5, -3), Advisor(true), Keys(Key(0x63), Key(0x0d))), Choice.Last),
		new("Change clicked at its right", true, 1, Then(Stock(5, 5, 0), Keys(Click(269, 110), Key(0x0d))), Choice.First),
		new("clicked right of Change", true, 1, Then(Stock(5, 5, 2), Keys(Click(270, 110))), Choice.Biggest),
		new("I, H, M, V", true, 1, Keys(Key(0x69), Key(0x68), Key(0x6d), Key(0x76)), Choice.Biggest),
		new("V alone", true, 1, Keys(Key(0x76)), Choice.Last),
		new("the tabs clicked", true, 1, Keys(Click(96, 110), Click(127, 110), Click(128, 110), Click(160, 110), Click(223, 110)), Choice.Biggest),
		new("clicked just right of the tabs", true, 1, Keys(Click(224, 110)), Choice.Last),
		new("Buy clicked at its bottom", true, 1, Then(Stock(5, 5, 2), Gold(30000), Keys(Click(300, 116), Key(0x0d))), Choice.Last),
		new("B, an improvement, the gold just enough", true, 1, Then(Stock(10, 5, -3), Buildings(0, 4), (game, town) =>
			game.World.Participants[game.World.Towns[town].ParticipantIdx].Gold =
				(short)((10 * game.Processor.Storage.PullUInt16(0x3b01, 3 * 0x1e + 0xbb2) - 10) * 2), Keys(Key(0x62), Key(0x0d))), Choice.Biggest),
		new("clicked left and right of the tabs", true, 1, Keys(Click(95, 110), Click(224, 110), Key(0x6d)), Choice.Biggest),
	};

	[Fact]
	public void HandleCityPurchaseCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, PurchaseCases, ways);
	}

	/// <summary>The first units of the owner put into the city with these states and no moves left (the others there awake)</summary>
	private static Action<GameEngine, short> Garrison(params byte[] states) => (game, town) =>
	{
		var city = game.World.Towns[town];

		for (int idx = 0; idx < 128; idx++)
		{
			ushort other = UnitMemory.Address(city.ParticipantIdx, idx);

			if (game.Processor.Storage.PullUInt8(0x3b01, (ushort)(other + UnitMemory.Horiz)) == (byte)city.Place.X &&
				game.Processor.Storage.PullUInt8(0x3b01, (ushort)(other + UnitMemory.Vert)) == (byte)city.Place.Y)
				game.Processor.EmitUInt8(0x3b01, (ushort)(other + UnitMemory.Status), 0);
		}

		var units = Enumerable.Range(0, 128).Where(idx => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(city.ParticipantIdx, idx) + UnitMemory.Kind)) != 0xff).Take(states.Length).ToList();

		for (int idx = 0; idx < units.Count; idx++)
		{
			ushort unit = UnitMemory.Address(city.ParticipantIdx, units[idx]);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)city.Place.X);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)city.Place.Y);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), states[idx]);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.LeftoverSteps), 0);
		}
	};

	/// <summary>The number of specialists (the top six bits of the workers)</summary>
	private static Action<GameEngine, short> Specialists(int count) => (game, town) =>
		game.World.Towns[town].LaborerMarkers1 = (ushort)((game.World.Towns[town].LaborerMarkers1 & 0x3ff) | (count << 10));

	private static readonly CityCase[] KeyCases =
	{
		new("A, nothing sleeps", true, 1, Then(Garrison(0, 0, 0), Keys(Key(0x61))), Choice.Biggest),
		new("A, a sentry woken", true, 1, Then(Garrison(0, 1, 0), Keys(Key(0x61), Key(0x0d))), Choice.Biggest),
		new("A, fortified ones moved and woken", true, 1, Then(Garrison(8, 8, 0, 0, 0, 0, 0, 0, 1),
			Keys(Key(0x61), Key(0x4d00), Key(0x20), Key(0x5000), Key(0x0d), Key(0x4800), Key(0x4b00), Key(0x0d), Key(0x41), Key(0x0d))), Choice.Last),
		new("A, a busy unit", true, 1, Then(Garrison(0, 0x32, 0x40), Keys(Key(0x61), Key(0x4d00), Key(0x0d), Key(0x4d00), Key(0x0d))), Choice.First),
		new("A, the last ones", true, 1, Then(Garrison(0, 0, 0, 0, 0, 0, 0, 8), Keys(Key(0x61), Key(0x5000), Key(0x4d00), Key(0x0d))), Choice.Biggest),
		new("A, a sentry with flags woken", true, 1, Then(Garrison(0x31, 0), Keys(Key(0x61), Key(0x0d))), Choice.Biggest),
		new("A, the sixth one woken", true, 1, Then(Garrison(0, 0, 0, 0, 0, 1, 0, 0), Keys(Key(0x61), Key(0x20))), Choice.Biggest),
		new("A, to the sixth place", true, 1, Then(Garrison(0, 0, 0, 0, 0, 0, 0, 0),
			Keys(Key(0x61), Key(0x4d00), Key(0x4d00), Key(0x4d00), Key(0x4d00), Key(0x4d00), Key(0x0d))), Choice.Last),
		new("1, 2, a small city with specialists", true, 1, Then(Size(4), Specialists(2), Keys(Key(0x31), Key(0x0d), Key(0x32), Key(0x0d))), Choice.First),
		new("1, 1, a city of five with specialists", true, 1, Then(Size(5), Specialists(2), Keys(Key(0x31), Key(0x31), Key(0x31), Key(0x32))), Choice.Biggest),
		new("S, given up", true, 1, Keys(Key(0x73), Key(0x1b)), Choice.Biggest),
		new("S, moved, sold", true, 1, Keys(Key(0x73), Key(0x5000), Key(0x5000), Key(0x4800), Key(0x0d), Key(0x0d)), Choice.Biggest),
		new("S, Space, the last one", true, 1, Keys(Key(0x73), Key(0x5000), Key(0x5000), Key(0x5000), Key(0x5000), Key(0x5000), Key(0x5000),
			Key(0x5000), Key(0x5000), Key(0x41), Key(0x20), Key(0x0d)), Choice.Last),
		new("S, sold this turn already", true, 1, Then((game, town) => game.World.Towns[town].ConditionMarker |= 0x80, Keys(Key(0x73))), Choice.Biggest),
		new("1 - 4, a big city", true, 1, Then(Size(12), Keys(Key(0x31), Key(0x31), Key(0x31), Key(0x32), Key(0x34))), Choice.Biggest),
		new("1, a small city", true, 1, Then(Size(4), Keys(Key(0x31), Key(0x0d))), Choice.First),
		new("9, past the specialists", true, 1, Keys(Key(0x39)), Choice.Biggest),
	};

	[Fact]
	public void HandleCityKeyCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, KeyCases, ways);
	}

	/// <summary>The square right of the city unseen by the human</summary>
	private static readonly Action<GameEngine, short> UnseenEast = (game, town) =>
	{
		var city = game.World.Towns[town];
		game.World.ChartSight[city.Place.X + 1, city.Place.Y] = (sbyte)(game.World.ChartSight[city.Place.X + 1, city.Place.Y] & ~(1 << game.World.PersonParticipantIdx));
	};

	/// <summary>Every improvement but the palace and the wonders of the first word</summary>
	private static readonly Action<GameEngine, short> AllBuildings = (game, town) =>
	{
		game.World.Towns[town].StructureMarkers0 = 0xfffe;
		game.World.Towns[town].StructureMarkers1 |= 0x7f;
	};

	private static readonly CityCase[] ListCases2 =
	{
		new("P, moved, picked", true, 1, Keys(Key(0x70), Key(0x4800), Key(0x4d00), Key(0x0d)), Choice.Biggest),
		new("P, all ways, given up", true, 1, Keys(Key(0x70), Key(0x4700), Key(0x4900), Key(0x4f00), Key(0x5100), Key(0x4b00), Key(0x5000),
			Key(0x5000), Key(0x5000), Key(0x5000), Key(0x1b)), Choice.Biggest),
		new("P, to the corner, Space", true, 1, Keys(Key(0x70), Key(0x5100), Key(0x5100), Key(0x5100), Key(0x41), Key(0x20)), Choice.Last),
		new("P, up and left to the edge", true, 1, Keys(Key(0x70), Key(0x4800), Key(0x4800), Key(0x4800), Key(0x4b00), Key(0x4b00), Key(0x4b00), Key(0x1b)), Choice.First),
		new("P, an unseen square", true, 1, Then(UnseenEast, Keys(Key(0x70), Key(0x4d00), Key(0x0d))), Choice.Biggest),
		new("M, a long list", true, 1, Then(Buildings(0x7ffe, 0), Keys(Key(0x4d), Key(0x4d))), Choice.Biggest),
		new("M, a short list", true, 1, Then(Buildings(0, 0xfffe), Keys(Key(0x4d))), Choice.Biggest),
		new("the arrow of a long list", true, 1, Then(Buildings(0x7ffe, 0), Keys(Click(288, 89), Click(300, 95))), Choice.Biggest),
		new("around the arrow of a long list", true, 1, Then(Buildings(0x7ffe, 0), Keys(Click(287, 92), Click(300, 88), Click(300, 96))), Choice.Last),
		new("R, renamed", true, 1, Keys(Key(0x72), Key(0x20), Key(0x41), Key(0x0d)), Choice.Biggest),
		new("the name clicked", true, 1, Keys(Click(231, 191), Key(0x20), Key(0x42), Key(0x0d), Click(269, 199), Key(0x20), Key(0x0d)), Choice.Biggest),
		new("around the name", true, 1, Keys(Click(230, 191), Click(250, 190), Click(270, 195)), Choice.Biggest),
		new("a line of the list, sold", true, 1, Then(Gold(100), Keys(Click(300, 8), Key(0x5000), Key(0x0d))), Choice.Biggest),
		new("a line of the list, kept", true, 1, Keys(Click(310, 14), Key(0x0d)), Choice.Last),
		new("left of the list, its bottom", true, 1, Keys(Click(299, 20), Click(310, 93), Key(0x0d), Click(310, 94)), Choice.Biggest),
		new("the walls sold", true, 1, Then(Buildings(0x80, 0xff7f), Keys(Key(0x73), Key(0x0d), Key(0x5000), Key(0x0d))), Choice.Biggest),
		new("a line, sold this turn already", true, 1, Then((game, town) => game.World.Towns[town].ConditionMarker |= 0x80, Keys(Click(310, 8))), Choice.Biggest),
		new("P, up right, picked", true, 1, Keys(Key(0x70), Key(0x4900), Key(0x0d)), Choice.Biggest),
		new("P, three right, picked", true, 1, Keys(Key(0x70), Key(0x4d00), Key(0x4d00), Key(0x4d00), Key(0x0d)), Choice.Biggest),
		new("the arrow of a full list", true, 1, Then(AllBuildings, Keys(Click(288, 89), Click(300, 95))), Choice.Biggest),
		new("just above the arrow of a full list", true, 1, Then(AllBuildings, Keys(Click(300, 88))), Choice.Biggest),
		new("the bottom line of a full list, sold", true, 1, Then(AllBuildings, Keys(Click(310, 94), Key(0x5000), Key(0x0d))), Choice.Biggest),
		new("a line between two, sold", true, 1, Then(Gold(100), Keys(Click(310, 13), Key(0x5000), Key(0x0d))), Choice.Biggest),
		new("just above the name, then a key", true, 1, Keys(Click(250, 190), Key(0x20), Key(0x43), Key(0x0d)), Choice.Biggest),
		new("a line, sold this turn already, Yes", true, 1, Then((game, town) => game.World.Towns[town].ConditionMarker |= 0x80,
			Keys(Click(310, 8), Key(0x5000), Key(0x0d))), Choice.Biggest),
		new("a line of the second page", true, 1, Then(Buildings(0x7ffe, 0), Keys(Key(0x4d), Click(310, 8), Key(0x5000), Key(0x0d))), Choice.Biggest),
	};

	[Fact]
	public void HandleCityListClickCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, ListCases2, ways);
	}

	private static readonly CityCase[] ClickCases =
	{
		new("the specialists clicked", true, 1, Then(Size(12), Specialists(3), Keys(Click(79, 10), Click(87, 19), Click(94, 5), Click(99, 0), Click(80, 10))), Choice.Biggest),
		new("just past the specialists", true, 1, Then(Size(12), Specialists(3), Keys(Click(100, 10), Click(86, 10))), Choice.Biggest),
		new("just below the specialists", true, 1, Then(Size(12), Specialists(3), Keys(Click(90, 20))), Choice.Biggest),
		new("the last row of a unit", true, 1, Then(Garrison(1, 8, 0), Keys(Click(105, 131))), Choice.Biggest),
		new("just below the map", true, 1, Keys(Click(160, 104)), Choice.Biggest),
		new("just inside the bottom of the map", true, 1, Keys(Click(160, 103)), Choice.Last),
		new("the workers clicked", true, 1, Then(Size(10), Specialists(2), Keys(Click(16, 10), Click(40, 10))), Choice.Biggest),
		new("around the citizens", true, 1, Then(Size(10), Specialists(2), Keys(Click(200, 10), Click(190, 20))), Choice.Biggest),
		new("a citizen of a small city", true, 1, Then(Size(4), Specialists(1), Keys(Click(60, 10), Key(0x0d))), Choice.First),
		new("a unit in the city clicked", true, 1, Then(Garrison(1, 8, 0), Keys(Click(100, 116))), Choice.Biggest),
		new("the second row of units", true, 1, Then(Garrison(0, 0, 0, 0, 0, 0, 1, 8), Keys(Click(115, 135), Click(131, 140))), Choice.Biggest),
		new("past the units", true, 1, Then(Garrison(1, 8), Keys(Click(150, 150), Click(99, 120))), Choice.Biggest),
		new("the middle of the map", true, 1, Keys(Click(160, 56)), Choice.Biggest),
		new("squares of the map", true, 1, Keys(Click(176, 56), Click(176, 72), Click(144, 40), Click(128, 24), Click(207, 103)), Choice.Biggest),
		new("squares of the map, a small city", true, 1, Then(Size(2), Keys(Click(176, 56), Click(144, 72), Click(160, 88), Click(192, 40))), Choice.First),
		new("an unseen square", true, 1, Then(UnseenEast, Keys(Click(176, 56))), Choice.Biggest),
		new("above and below the map", true, 1, Keys(Click(160, 23), Click(160, 104)), Choice.Biggest),
		new("squares with specialists", true, 1, Then(Size(8), Specialists(4), Keys(Click(176, 56), Click(144, 56), Click(160, 40))), Choice.Last),
	};

	[Fact]
	public void HandleCityClickCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, ClickCases, ways);
	}

	private static readonly Action<GameEngine, short> Unhappy = Then(Size(14), Buildings(0, 0x2408), Level(4, 1), Rates(10, 0));

	private static Action<GameEngine, short> Disorder(bool on) => (game, town) => game.World.Towns[town].ConditionMarker =
		(byte)(on ? game.World.Towns[town].ConditionMarker | 1 : game.World.Towns[town].ConditionMarker & ~1);

	private static Action<GameEngine, short> Options(short set, short clear) => (game, town) =>
		game.World.SessionPrefMarkers = (short)((game.World.SessionPrefMarkers & ~clear) | set);

	private static readonly CityCase[] OrderCases =
	{
		new("human, disorder breaks out", true, 0, Then(Unhappy, Disorder(false), Options(0, 9)), Choice.Biggest),
		new("human, disorder breaks out, the help", true, 0, Then(Unhappy, Disorder(false), Options(1, 8)), Choice.Last),
		new("human, disorder breaks out, the riot", true, 0, Then(Unhappy, Disorder(false), Options(8, 1), (game, town) => game.Processor.EmitUInt16(0x3b01, 0x6b92, 0)), Choice.Biggest),
		new("human, disorder breaks out, the riot seen", true, 0, Then(Unhappy, Disorder(false), Options(8, 1), (game, town) => game.Processor.EmitUInt16(0x3b01, 0x6b92, 1)), Choice.First),
		new("human, disorder goes on", true, 0, Then(Unhappy, Disorder(true), Options(8, 1)), Choice.Biggest),
		new("human, disorder goes on under democracy", true, 0, Then(Unhappy, Level(4, 5), Rates(10, 0), Disorder(true)), Choice.Biggest),
		new("computer, disorder breaks out", false, 0, Then(Unhappy, Disorder(false)), Choice.Biggest),
		new("computer, disorder goes on under democracy", false, 0, Then(Unhappy, Level(4, 5), Rates(10, 0), Disorder(true)), Choice.Biggest),
		new("human, order restored", true, 0, Then(Size(1), Disorder(true)), Choice.First),
		new("computer, order restored", false, 0, Then(Size(1), Disorder(true)), Choice.First),
		new("human, anarchy, no taxes", true, 0, Then(Size(2), Level(1, 0), Disorder(false)), Choice.First),
		new("computer, monarchy, taxes", false, 0, Then(Size(3), Level(1, 2), Disorder(false)), Choice.Last),
	};

	[Fact]
	public void HandleCityOrderCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, OrderCases, ways);
	}

	private static Action<GameEngine, short> Celebrating(bool on) => (game, town) => game.World.Towns[town].ConditionMarker =
		(byte)(on ? game.World.Towns[town].ConditionMarker | 0x40 : game.World.Towns[town].ConditionMarker & ~0x40);

	private static Action<GameEngine, short> Happy(sbyte size, short regime) => Then(Size(size), Level(1, regime), Rates(0, 0), Buildings(0x2408, 0), Disorder(false));

	private static readonly CityCase[] CelebrationCases =
	{
		new("human, the celebration starts", true, 0, Then(Happy(4, 2), Celebrating(false), Options(0, 8)), Choice.Biggest),
		new("human, the celebration starts, the city view", true, 0, Then(Happy(4, 2), Celebrating(false), Options(8, 0)), Choice.Last),
		new("computer, the celebration starts", false, 0, Then(Happy(4, 2), Celebrating(false)), Choice.Biggest),
		new("human, republic, the celebration goes on", true, 0, Then(Happy(5, 4), Celebrating(true)), Choice.Biggest),
		new("computer, democracy, the celebration goes on, size 12", false, 0, Then(Happy(12, 5), Celebrating(true), Buildings(0, 0x100)), Choice.Biggest),
		new("human, democracy, size 12 with an aqueduct", true, 0, Then(Happy(12, 5), Celebrating(true), Buildings(0x100, 0)), Choice.First),
		new("human, democracy, size 9", true, 0, Then(Happy(9, 5), Celebrating(true), Buildings(0, 0x100)), Choice.Last),
		new("human, monarchy, the celebration goes on", true, 0, Then(Happy(6, 2), Celebrating(true)), Choice.Biggest),
		new("human, the celebration ends", true, 0, Then(Size(8), Level(1, 2), Rates(10, 0), Buildings(0x2408, 0), Celebrating(true), Disorder(false)), Choice.Biggest),
		new("computer, the celebration ends", false, 0, Then(Size(8), Level(1, 2), Rates(10, 0), Buildings(0x2408, 0), Celebrating(true), Disorder(false)), Choice.Biggest),
		new("human, the celebration ends in disorder", true, 0, Then(Unhappy, Celebrating(true), Disorder(false)), Choice.Biggest),
		new("human, communism, the celebration goes on", true, 0, Then(Happy(6, 3), Celebrating(true)), Choice.Biggest),
		new("human, democracy, size 10 without an aqueduct", true, 0, Then(Happy(10, 5), Celebrating(true), Buildings(0, 0x100)), Choice.Biggest),
		new("human, anarchy, celebrating", true, 0, Then(Happy(6, 0), Celebrating(true)), Choice.Biggest),
		new("human, size 2, happy", true, 0, Then(Happy(2, 2), Celebrating(true)), Choice.First),
		new("human, size 3, happy", true, 0, Then(Happy(3, 2), Celebrating(false)), Choice.First),
	};

	[Fact]
	public void HandleCityCelebrationCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, CelebrationCases, ways);
	}

	/// <summary>The research of the owner of the city (the goal of the human player when the owner is human)</summary>
	private static Action<GameEngine, short> Research(short goal, short headway, short found) => (game, town) =>
	{
		var player = game.World.Participants[game.World.Towns[town].ParticipantIdx];
		player.PresentInquiryIdx = goal;
		player.InquiryHeadway = headway;
		player.FoundScienceTally = found;
	};

	private static Action<GameEngine, short> Word(ushort address, ushort value) => (game, town) => game.Processor.EmitUInt16(0x3b01, address, value);

	private static Action<GameEngine, short> DiagEight(bool on) => (game, town) =>
		game.World.DiagMarkers = (short)(on ? game.World.DiagMarkers | 8 : game.World.DiagMarkers & ~8);

	/// <summary>No science from the city; the research of the human set to the cost of an advance times two, more the extra</summary>
	private static Action<GameEngine, short> AtCost(short hardness, short found, short extra) => (game, town) =>
	{
		var player = game.World.Participants[game.World.Towns[town].ParticipantIdx];
		bool human = game.World.Towns[town].ParticipantIdx == game.World.PersonParticipantIdx;
		game.World.HardnessTier = hardness;
		game.World.Season = 100;
		short cost = (short)((human ? hardness * 2 + 6 : 14 - hardness) + (short)game.Processor.Storage.PullUInt16(0x3b01, 0xd2de));

		if (human)
			cost = Math.Max(cost, (short)(11 - found));

		player.FoundScienceTally = found;
		player.InquiryHeadway = (short)(2 * found * cost + extra);
	};

	private static readonly CityCase[] ScienceCases =
	{
		new("human, no goal, research 1", true, 0, Then(Research(-1, 1, 20), DiagEight(false)), Choice.Last),
		new("human, the easiest level, found by others (4)", true, 0, Then(Level(0, 2), Rates(0, 10), Knows(0x30, false), Research(0x30, 10, 20),
			(game, town) => game.World.ScienceLeadingFoundVia[0x30] = 4), Choice.Biggest),
		new("human, level 1, found by others", true, 0, Then(Level(1, 2), Rates(0, 10), Knows(0x30, false), Research(0x30, 10, 20),
			(game, town) => game.World.ScienceLeadingFoundVia[0x30] = 4), Choice.Biggest),
		new("human, the goal 0 not found", true, 0, Then(Knows(0, false), Research(0, 40, 20), DiagEight(false)), Choice.Biggest),
		new("human, research at the cost", true, 0, Then(Rates(10, 0), Knows(0x30, false), Research(0x30, 0, 0), AtCost(2, 20, 0), DiagEight(false)), Choice.Biggest),
		new("human, research just past the cost", true, 0, Then(Rates(10, 0), Knows(0x30, false), Research(0x30, 0, 0), AtCost(2, 20, 2), DiagEight(false)), Choice.Biggest),
		new("human, one advance, just past the least cost", true, 0, Then(Rates(10, 0), Knows(0x30, false), Research(0x30, 0, 0), AtCost(0, 1, 2), DiagEight(false)), Choice.Last),
		new("computer, research past the cost, 0xb1e8", false, 0, Then(Research(0x30, 30000, 20), Word(0xb1e8, 1), DiagEight(false)), Choice.Biggest),
		new("human, the goal found already", true, 0, Then(Knows(3, true), Research(3, 40, 20), DiagEight(false)), Choice.Biggest),
		new("human, the goal found already, uncovered", true, 0, Then(Knows(3, true), Research(3, 40, 20), DiagEight(true)), Choice.Biggest),
		new("human, no goal, research done", true, 0, Then(Research(-1, 40, 20), DiagEight(false)), Choice.Last),
		new("human, no goal, nothing done", true, 0, Then(Research(-1, 0, 20), DiagEight(false)), Choice.Last),
		new("human, a goal, a little research", true, 0, Then(Knows(0x30, false), Research(0x30, 10, 20)), Choice.Biggest),
		new("human, a goal, research past the cost", true, 0, Then(Knows(0x30, false), Research(0x30, 30000, 20), DiagEight(false)), Choice.Biggest),
		new("human, research past the cost, uncovered", true, 0, Then(Knows(0x30, false), Research(0x30, 30000, 20), DiagEight(true)), Choice.First),
		new("human, research past the cost, 0xb1e8", true, 0, Then(Knows(0x30, false), Research(0x30, 30000, 20), Word(0xb1e8, 1)), Choice.Biggest),
		new("computer, a little research", false, 0, Research(0x30, 10, 20), Choice.Biggest),
		new("computer, research past the cost", false, 0, Then(Research(0x30, 30000, 20), DiagEight(false)), Choice.Biggest),
		new("human, anarchy", true, 0, Then(Level(1, 0), Knows(0x30, false), Research(0x30, 10, 20)), Choice.Biggest),
		new("human, the easiest level, an advance found by others", true, 0, Then(Level(0, 2), Knows(0x30, false), Research(0x30, 10, 20),
			(game, town) => game.World.ScienceLeadingFoundVia[0x30] = 3), Choice.Biggest),
		new("human, the easiest level, a new advance", true, 0, Then(Level(0, 2), Knows(0x30, false), Research(0x30, 10, 20),
			(game, town) => game.World.ScienceLeadingFoundVia[0x30] = 8), Choice.Last),
		new("human, the wonder 20", true, 0, Then(Wonder(0x14, true), Knows(0x30, false), Research(0x30, 10, 20)), Choice.Biggest),
		new("human, before Christ, few advances, at the cost", true, 0, Then(Knows(0x30, false), Research(0x30, 150, 2),
			(game, town) => game.World.Season = -500, Level(4, 2)), Choice.Biggest),
		new("computer, before Christ, few advances", false, 0, Then(Research(0x30, 100, 2), (game, town) => game.World.Season = -500), Choice.Biggest),
	};

	[Fact]
	public void HandleCityScienceCasesMatchOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9 })
			CheckCases(bay, ScienceCases, ways);
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
