using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;
using FrozenCheckpoint = Civilization1Original.Tests.GameCheckpoint;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// HandleCity on cities picked and changed at random (the scenarios of the former BatchHundredThirteenDifferentialTests), on
/// the frozen game and on the current one: every scenario starts both from the checkpoint after the load and changes them
/// the same way (dynamic: the two games have the same members); the states, AX and the failures must match. What a
/// scenario met is read from the frozen game, whose stack still holds the locals of HandleCity.
/// </summary>
public sealed class DualCityRandomTests(ITestOutputHelper output)
{
	[Fact]
	public void HandleCityMatchesFrozenGame()
	{
		// A city only counted (-1), in its turn (0) or on the city screen (1, the human's cities, left with Escape); the
		// materials sometimes out of range; the advances that make citizens content taken away or given; the palaces of the
		// owner sometimes taken away
		HashSet<string> ways = Run(false, false);

		foreach (string way in new[] { "own palace", "palace near", "no palace near", "shared tiles", "content advances", "materials reset", "computer turn", "city lost", "grew", "held", "settler starved" })
			Assert.True(ways.Contains(way), $"No city met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	[Fact]
	public void HandleCityProductionMatchesFrozenGame()
	{
		// Cities in their turn with a unit ready (settlers, also of a city of size 1 or the last city, a diplomat of a computer
		// player, others; the barracks, the first unit of its kind, the shields exactly the price)
		HashSet<string> ways = Run(true, false);

		foreach (string way in new[] { "unit built", "city lost" })
			Assert.True(ways.Contains(way), $"No city met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	[Fact]
	public void HandleCitySpaceshipMatchesFrozenGame()
	{
		// Cities of computer players in their turn with a spaceship part ready
		HashSet<string> ways = Run(true, true);

		foreach (string way in new[] { "not launched" })
			Assert.True(ways.Contains(way), $"No city met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private HashSet<string> Run(bool production, bool spaceship)
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		HashSet<string> ways = [];
		List<string> differences = [];

		foreach (int bay in new[] { 5, 9, 13 })
			differences.AddRange(CheckCities(gameFolder!, bay, ways, production, spaceship));

		foreach (string difference in differences)
			output.WriteLine(difference);

		Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences.Take(3)));
		return ways;
	}

	private static List<string> CheckCities(string gameFolder, int bay, HashSet<string> ways, bool production, bool spaceship)
	{
		List<string> differences = [];

		using DualGame games = new DualGame(gameFolder);
		games.LoadAutosave(bay, GoldenMasterTests.RandomSeed);

		FrozenCheckpoint frozenPoint = new FrozenCheckpoint(games.Frozen);
		GameCheckpoint currentPoint = new GameCheckpoint(games.Current);
		Random chance = new Random(bay);
		int scenarioTally = production ? 70 : 40;

		for (int scenarioIdx = 0; scenarioIdx < scenarioTally; scenarioIdx++)
		{
			frozenPoint.Restore(games.Frozen);
			currentPoint.Restore(games.Current);

			GameEngine engine = games.Current.Engine;
			short person = engine.World.PersonParticipantIdx;
			var towns = Enumerable.Range(0, 128).Where(town => engine.World.Towns[town].ConditionMarker != 0xff).ToList();

			if (towns.Count == 0)
continue;

			// (sometimes a capital)
			var capitals = towns.Where(town => (engine.World.Towns[town].StructureMarkers0 & 1) != 0).ToList();
			short townIdx = (short)(capitals.Count > 0 && chance.Next(4) == 0 ? capitals[chance.Next(capitals.Count)] : towns[chance.Next(towns.Count)]);

			if (spaceship)
			{
// (a city of a computer player)
var computerTowns = towns.Where(town => engine.World.Towns[town].ParticipantIdx != person && engine.World.Towns[town].ParticipantIdx != 0).ToList();

if (computerTowns.Count == 0)
	continue;

townIdx = (short)computerTowns[chance.Next(computerTowns.Count)];
			}

			bool human = engine.World.Towns[townIdx].ParticipantIdx == person;
			short marker = (short)(human && chance.Next(4) == 0 ? 1 : chance.Next(2) - 1);

			if (production)
marker = 0;
			short materials = (short)new[] { -1, -5, 0, 1000, 1500 }[chance.Next(5)];
			bool badMaterials = chance.Next(3) == 0;
			// The advances that make citizens content: 0 kept, 1 taken away, 2 all given
			int advances = chance.Next(3);
			bool screen = marker == 1;
			bool noPalaces = !screen && chance.Next(4) == 0;
			// Another city moved next to this one (its own tile among the tiles around); the level and the leader of a later
			// game; the flag 1 of the city
			// (only counted: in the turn of the city the original may then loop without end)
			bool close = marker == -1 && chance.Next(2) == 0;
			// A city of the barbarians (only counted)
			bool barbarian = marker == -1 && !close && chance.Next(3) == 0;
			int closeIdx = chance.Next(1000);
			(int Horiz, int Vert) offset = (chance.Next(-2, 3), chance.Next(1, 3));
			short hardness = (short)(screen ? 1 : chance.Next(5));
			bool leader = !screen && chance.Next(2) == 0;
			bool flag = !screen && chance.Next(2) == 0;
			// The food: 0 kept, 1 below zero, 2 enough to grow (sometimes at size 10 or 11), with a granary, an aqueduct and the
			// bit 4 of DiagMarkers by chance
			// (3 exactly the food to grow, 4 one less)
			int food = screen ? 0 : chance.Next(5);
			sbyte size = (sbyte)(!screen && chance.Next(3) != 0 ? chance.Next(10, 12) : 0);
			bool unrest = !screen && chance.Next(2) == 0;
			// The special slots of the city, a unit of the owner made a unit of the city (of a chosen kind), the government
			// and the bit 2 of DiagMarkers (not on the city screen, which then waits without end)
			sbyte[] slots = Enumerable.Range(0, 2).Select(_ => (sbyte)(chance.Next(4) == 0 ? -1 : chance.Next(6))).ToArray();
			// (mostly in small cities, so the slots count above the size)
			bool slotsSet = marker != 1 && chance.Next(engine.World.Towns[townIdx].TrueExtent <= 2 ? 1 : 2) == 0;
			int supported = chance.Next(1000);
			byte supportedKind = marker == 1 ? (byte)0xff : (byte)new[] { 0x19, 0x1a, 0x1b, 0, 0xe, 2, -1 }[chance.Next(7)];
			short regime = (short)(marker == 1 ? -1 : chance.Next(-1, 7));
			bool diag2 = chance.Next(2) == 0;
			bool granary = !screen && chance.Next(2) == 0;
			bool aqueduct = !screen && chance.Next(2) == 0;
			bool diag = !screen && chance.Next(2) == 0;
			sbyte sizeBefore = 0;
			bool alive = true;
			Dictionary<string, int> before = new Dictionary<string, int>(games.Frozen.Calls);
			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			// The local 0x16 of HandleCity (left on the stack by earlier calls) cleared, so the city tile is taken anew; the
			// workers of the city mixed up (not on the city screen); tiles around a city of the human unseen (on the screen)
			bool freeCityTile = chance.Next(2) == 0;
			bool mixWorkers = !screen && chance.Next(3) == 0;
			uint mixed = (uint)chance.Next(1 << 20);
			bool unseen = screen && chance.Next(2) == 0;
			int unseenSeed = chance.Next(1000);
			// The factory, the manufacturing plant (0x4000, 0x8000) and the mass transit, power, hydro and nuclear plants
			// (0x2, 0x4, 0x8, 0x10 of the second word) set by chance (not on the city screen)
			bool buildings = !screen && chance.Next(2) == 0;
			int buildingBits = chance.Next(64);
			// The wonder 15 in this city or in another city of the owner (maybe on another continent)
			bool marvel = !screen && chance.Next(3) == 0;
			int marvelPick = chance.Next(1000);
			// A unit ready in the turn: the settlers (sometimes of a city of size 1, the last of its owner), a diplomat, others;
			// the barracks, the first unit of its kind for the replay
			bool produce = marker == 0 && chance.Next(4) != 0 && production;
			sbyte produced = (sbyte)new[] { 0, 0, 0x1a, 0, 0xe }[chance.Next(5)];
			bool smallCity = chance.Next(2) == 0;
			bool lastCity = chance.Next(3) == 0;
			bool barracks = chance.Next(2) == 0;
			bool firstOfKind = chance.Next(2) == 0;
			// (the shields exactly the price, or one less; the easiest level)
			int exactCost = chance.Next(3);
			bool easiest = chance.Next(2) == 0;
			sbyte smallSize = (sbyte)chance.Next(1, 3);
			// (also a caravan, or a nuclear unit, maybe the only one of its owner)
			int alternative = chance.Next(4);
			bool onlyNuclear = chance.Next(3) != 0;

			if (alternative == 1)
produced = 0x1b;
			else if (alternative == 2)
produced = 0x19;

			// An improvement or a wonder instead (maybe one the city or the world has already), the option 8 (city screen)
			bool improvement = chance.Next(2) == 0;
			short improvementIdx = (short)new[] { 1, 3, 5, 0xc, 0x15, 0x16, 0x18, 0x19, 0x1d, 0x26, 0x29, 0x2b }[chance.Next(12)];
			bool alreadyBuilt = chance.Next(3) == 0;
			bool cityScreenOption = chance.Next(2) == 0;
			int elsewhere = chance.Next(1000);
			// The spaceship (only in its own test, so the draws of the other tests stay)
			short ratio = 0, arrival = 0;
			bool knows28 = false, ownerFlying = false, personFlying = false, personAhead = false, richThird = false;

			if (spaceship)
			{
improvement = true;
improvementIdx = (short)chance.Next(0x16, 0x19);
alreadyBuilt = false;
ratio = (short)new[] { 0, 40, 60, 76, 80 }[chance.Next(5)];
knows28 = chance.Next(3) != 0;
ownerFlying = chance.Next(5) == 0;
personFlying = chance.Next(2) == 0;
personAhead = chance.Next(2) == 0;
arrival = (short)chance.Next(-3, 4);
richThird = chance.Next(2) == 0;
			}

			// (no food variants with production, which changes the size; a full store at size 10 or 11 for the aqueduct)
			if (produce)
food = 0;

			if (food == 2 && size == 0)
size = 10;

			if (close)
			{
// With the city moved next to another one nothing else changes (the original may then loop without end)
food = 0;
size = 0;
slotsSet = false;
supportedKind = 0xff;
regime = -1;
buildings = false;
marvel = false;
produce = false;
			}


			string label = $"AUTO{bay:00} " + $"HandleCity({townIdx}, {marker}), materials {(badMaterials ? materials : "kept")}, advances {advances}, no palaces {noPalaces}, close {close}, level {hardness}, leader {leader}, flag {flag}, food {food}, size {size}, granary {granary}, aqueduct {aqueduct}, diag {diag}, flag 0x80 {unrest}, slots {(slotsSet ? string.Join("/", slots) : "kept")}, unit kind {supportedKind}, government {regime}, diag 2 {diag2}, barbarian {barbarian}, free city tile {freeCityTile}, mixed workers {(mixWorkers ? mixed : 0)}, unseen {unseen}, buildings {(buildings ? buildingBits : -1)}, wonder 15 {marvel}, produce {(produce ? produced : -1)} small {(smallCity ? smallSize : 0)} last {lastCity} barracks {barracks} first {firstOfKind} exact {exactCost} easiest {easiest}, improvement {(improvement ? improvementIdx : 0)} built {alreadyBuilt} screen option {cityScreenOption}";

			void Setup(dynamic game)
			{
	HumanTurnScript.Install((object)game, [new HumanTurnScript.Input(0x1b), new HumanTurnScript.Input(0x1b)]);

	if (badMaterials)
		game.World.Towns[townIdx].MaterialsTally = materials;

	short owner = game.World.Towns[townIdx].ParticipantIdx;
	ushort[] known = game.World.Participants[owner].FoundScienceMarkers;

	foreach (int advance in new[] { 0x23, 0x29, 0x3a, 0x3c })
	{
		if (advances == 1)
			known[advance >> 4] &= (ushort)~(1 << (advance & 0xf));
		else if (advances == 2)
			known[advance >> 4] |= (ushort)(1 << (advance & 0xf));
	}

	if (close)
	{
		var others = towns.Where(other => other != townIdx).ToList();

		if (others.Count > 0)
		{
			var other = game.World.Towns[others[closeIdx % others.Count]];
			var place = game.World.Towns[townIdx].Place;
			other.Place = new System.Drawing.Point((place.X + offset.Horiz + 80) % 80, place.Y + offset.Vert);
		}
	}

	if (!screen)
	{
		game.World.HardnessTier = hardness;
		game.World.Participants[person].Standing = (short)(leader ? 7 : game.World.Participants[person].Standing == 7 ? 6 : game.World.Participants[person].Standing);
		game.World.Towns[townIdx].ConditionMarker = (byte)(flag ? game.World.Towns[townIdx].ConditionMarker | 1 : game.World.Towns[townIdx].ConditionMarker & 0xfe);
	}

	var city = game.World.Towns[townIdx];

	if (!screen)
	{
		if (size != 0)
			city.TrueExtent = size;

		if (food == 1)
			city.GrainTally = -1;
		else if (food == 2)
			city.GrainTally = 900;
		else if (food >= 3)
		{
			// The food box: 10 for the human, else 16 - 2 x the level (2 less for the leader of a later game)
			int box = city.ParticipantIdx == person ? 10 : 16 - 2 * hardness -
				(game.World.Season >= 0 && game.World.Participants[person].Standing == 7 && hardness > 1 ? 2 : 0);
			city.GrainTally = (short)((city.TrueExtent + 1) * box - (food - 3));
		}

		if (slotsSet)
		{
			city.Unfamiliar[0] = slots[0];
			city.Unfamiliar[1] = slots[1];
		}

		var ownUnits = Enumerable.Range(0, 128).Where(troop => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(city.ParticipantIdx, troop) + UnitMemory.Kind)) != 0xff).ToList();

		if (supportedKind != 0xff && ownUnits.Count > 0)
		{
			ushort unit = UnitMemory.Address(city.ParticipantIdx, ownUnits[supported % ownUnits.Count]);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), supportedKind);
			game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), (byte)townIdx);
		}

		if (regime >= 0)
			game.World.Participants[city.ParticipantIdx].RegimeKind = regime;

		game.World.DiagMarkers = (short)(diag2 ? game.World.DiagMarkers | 2 : game.World.DiagMarkers & ~2);

		city.ConditionMarker = (byte)(unrest ? city.ConditionMarker | 0x80 : city.ConditionMarker & 0x7f);

		city.StructureMarkers0 = (ushort)(granary ? city.StructureMarkers0 | 4 : city.StructureMarkers0 & ~4);
		city.StructureMarkers0 = (ushort)(aqueduct ? city.StructureMarkers0 | 0x100 : city.StructureMarkers0 & ~0x100);
		game.World.DiagMarkers = (short)(diag ? game.World.DiagMarkers | 4 : game.World.DiagMarkers & ~4);
		sizeBefore = city.TrueExtent;
		alive = true;
	}

	if (barbarian)
		game.World.Towns[townIdx].ParticipantIdx = 0;

	if (buildings)
	{
		var built = game.World.Towns[townIdx];
		built.StructureMarkers0 = (ushort)((built.StructureMarkers0 & 0x3fff) | ((buildingBits & 3) << 14));
		built.StructureMarkers1 = (ushort)((built.StructureMarkers1 & ~0x1e) | ((buildingBits >> 1) & 0x1e));
	}

	if (marvel)
	{
		var ownTowns = towns.Where(other => game.World.Towns[other].ParticipantIdx == game.World.Towns[townIdx].ParticipantIdx).ToList();
		game.World.MarvelTownIdx[15] = (short)(marvelPick % 2 == 0 ? townIdx : ownTowns[marvelPick % ownTowns.Count]);
	}

	if (produce)
	{
		var making = game.World.Towns[townIdx];
		making.PresentMakingIdx = produced;
		making.MaterialsTally = 990;

		if (exactCost > 0)
		{
			// The price x the box of the turn (10 for the human, else 16 - 2 x the level, 2 less for the leader)
			int level = easiest ? 0 : Math.Max(1, (int)game.World.HardnessTier);
			int box = making.ParticipantIdx == person ? 10 : 16 - 2 * level -
				(game.World.Season >= 0 && game.World.Participants[person].Standing == 7 && level > 1 ? 2 : 0);
			int price = game.Processor.Storage.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + produced * UnitKindTable.Stride + UnitKindTable.Price));
			making.MaterialsTally = (short)(price * box - (exactCost - 1));
		}

		// (the easiest level, or one above it)
		game.World.HardnessTier = (short)(easiest ? 0 : Math.Max((short)1, game.World.HardnessTier));
		making.StructureMarkers0 = (ushort)(barracks ? making.StructureMarkers0 | 2 : making.StructureMarkers0 & ~2);

		if (smallCity)
		{
			// (no food, so it does not grow first)
			making.TrueExtent = smallSize;
			making.GrainTally = 0;
		}

		if (lastCity)
			game.World.Participants[making.ParticipantIdx].TownTally = 1;

		if (firstOfKind)
			game.World.ScienceLeadingFoundVia[produced] &= ~8;

		if (improvement)
		{
			making.PresentMakingIdx = (sbyte)-improvementIdx;

			if (exactCost > 0)
			{
				int level = easiest ? 0 : Math.Max(1, (int)game.World.HardnessTier);
				int box = making.ParticipantIdx == person ? 10 : 16 - 2 * level -
					(game.World.Season >= 0 && game.World.Participants[person].Standing == 7 && level > 1 ? 2 : 0);
				int price = game.Processor.Storage.PullUInt16(0x3b01, (ushort)(0xbb2 + improvementIdx * 0x1e));
				making.MaterialsTally = (short)(price * box - (exactCost - 1));
			}

			if (alreadyBuilt && improvementIdx > 0x18)
				game.World.MarvelTownIdx[improvementIdx - 24] = (short)towns[elsewhere % towns.Count];
			else if (alreadyBuilt)
			{
				uint bit = 1u << (improvementIdx - 1);
				making.StructureMarkers0 |= (ushort)bit;
				making.StructureMarkers1 |= (ushort)(bit >> 16);
			}
			else if (improvementIdx > 0x18)
				game.World.MarvelTownIdx[improvementIdx - 24] = -1;

			game.World.SessionPrefMarkers = (short)(cityScreenOption ? game.World.SessionPrefMarkers | 8 : game.World.SessionPrefMarkers & ~8);

			if (spaceship)
			{
				var computer = game.World.Participants[making.ParticipantIdx];
				var human = game.World.Participants[person];
				game.World.BotStarshipVictoryRatio = ratio;
				ushort[] science = human.FoundScienceMarkers;
				science[0x28 >> 4] = (ushort)(knows28 ? science[0x28 >> 4] | (1 << (0x28 & 0xf)) : science[0x28 >> 4] & ~(1 << (0x28 & 0xf)));
				int flags = game.World.StarshipMarkers & ~((1 << making.ParticipantIdx) | (1 << person) | (1 << (person + 8)));
				flags |= (ownerFlying ? 1 << making.ParticipantIdx : 0) | (personFlying ? (1 << person) | (1 << (person + 8)) : 0);
				game.World.StarshipMarkers = (short)flags;
				human.Standing = (short)(personAhead ? computer.Standing + 1 : computer.Standing);
				human.StarshipArrivalSeason = (short)(computer.StarshipArrivalSeason + arrival);

				if (game.World.LiveRealms >= 0 && game.World.LiveRealms < 8)
					game.World.Participants[game.World.LiveRealms].Gold = (short)(richThird ? 2000 : 100);
			}
		}

		if (produced == 0x19 && !improvement)
		{
			// (the count grows with the new unit; the timer of the human set, so its reset shows)
			game.World.Participants[making.ParticipantIdx].LiveTroops[25] = (short)(onlyNuclear ? 0 : 3);
			game.World.Participants[person].TouchParticipantTimeleft = 7;
		}
	}

	if (freeCityTile)
		game.Processor.Storage.EmitUInt16(game.Processor.SegStack.Value16, (ushort)(frame - 0x16), 0);

	if (mixWorkers)
	{
		var mixedCity = game.World.Towns[townIdx];
		uint workers = ((uint)mixedCity.LaborerMarkers1 << 16) | mixedCity.LaborerMarkers0;
		workers = (workers & 0xfc000000) | mixed;
		mixedCity.LaborerMarkers0 = (ushort)workers;
		mixedCity.LaborerMarkers1 = (ushort)(workers >> 16);
	}

	if (unseen)
	{
		var unseenCity = game.World.Towns[townIdx];
		Random pickUnseen = new Random(unseenSeed);

		for (int tile = 0; tile < 20; tile++)
		{
			if (pickUnseen.Next(3) == 0)
			{
				int horizLoc = (unseenCity.Place.X + game.TownDeltas[tile].X + 80) % 80;
				int vertLoc = unseenCity.Place.Y + game.TownDeltas[tile].Y;
				game.World.ChartSight[horizLoc, vertLoc] &= (sbyte)~(1 << person);
			}
		}
	}

	// The owner without any palace
	foreach (dynamic town in ((IEnumerable<dynamic>)game.World.Towns).Where(town => noPalaces && town.ConditionMarker != 0xff && town.ParticipantIdx == owner))
		town.StructureMarkers0 &= 0xfffe;
			}

			string? frozenFailure = Run(games.Frozen.Engine, Setup, townIdx, marker, deadline => games.Frozen.Deadline = deadline);
			string? currentFailure = Run(games.Current.Engine, Setup, townIdx, marker, deadline => games.Current.Deadline = deadline);

			string? difference = games.Compare(label);

			if (difference == null && frozenFailure != currentFailure)
				difference = $"{label}: the frozen game {frozenFailure ?? "finished"}, the current one {currentFailure ?? "finished"}";

			if (difference == null && games.Frozen.Engine.Processor.RegAcc.Value16 != games.Current.Engine.Processor.RegAcc.Value16)
				difference = $"{label}: AX 0x{games.Frozen.Engine.Processor.RegAcc.Value16:x4} in the frozen game, 0x{games.Current.Engine.Processor.RegAcc.Value16:x4} in the current one";

			if (frozenFailure != null && frozenFailure.StartsWith("RuntimeBinderException", StringComparison.Ordinal))
				difference = $"{label}: the setup fails: {frozenFailure}";

			if (difference != null)
				differences.Add(difference);

			// What the scenario met, from the frozen game
			{
				dynamic game = games.Frozen.Engine;
	List<string> met = [];
	ushort palace = game.Processor.PullGlobalWord(0x6b30);
	met.Add(palace == 0 ? "own palace" : palace < 0x20 ? "palace near" : "no palace near");

	if (Enumerable.Range(0, 25).Any(at => at != 12 && game.Processor.Storage.PullUInt16(game.Processor.SegStack.Value16, (ushort)(frame - 0xa6 + 2 * at)) == 1))
		met.Add("shared tiles");

	if (game.Processor.PullGlobalWord(0xb882) != 0)
		met.Add("content advances");

	if (badMaterials && (materials < 0 || materials > 999) && game.World.Towns[townIdx].MaterialsTally < 999)
		met.Add("materials reset");

	if (!human && marker == 0)
		met.Add("computer turn");

	int Delta(string name) => games.Frozen.Calls.GetValueOrDefault(name) - before.GetValueOrDefault(name);

	if (Delta("ConstructUnit") > 0)
		met.Add("unit built");

	if (spaceship)
		met.Add(Delta("FireSpaceship") > 0 ? "launched" : "not launched");

	if (Delta("FinalizeProduction") > 0)
		met.Add("city lost");
	else if (marker == 0 && game.World.Towns[townIdx].TrueExtent > sizeBefore)
		met.Add("grew");
	else if (marker == 0 && food == 2 && game.World.Towns[townIdx].TrueExtent == sizeBefore)
		met.Add("held");

	if (food == 1 && marker == 0 && Delta("EliminateUnit") > 0)
		met.Add("settler starved");

	foreach (string way in met)
		ways.Add(way);

	
			}
		}

		return differences;
	}

	/// <summary>The setup and HandleCity in one game; the failure, null when it finished</summary>
	private static string? Run(dynamic engine, Action<dynamic> setup, short townIdx, short marker, Action<long> setDeadline)
	{
		ushort stack = engine.Processor.RegStack.Value16;
		ushort frame = engine.Processor.RegFrame.Value16;

		try
		{
			setup(engine);
			setDeadline(Environment.TickCount64 + DualCityCaseTests.RunSeconds * 1000L);
			engine.CityOps.HandleCity(townIdx, marker);
			return null;
		}
		catch (Exception failure)
		{
			// The exception leaves the frames of the functions it left on the emulated stack, as many as the version has (B3
			// removes frames and saved registers): the pointers go back, so the next cases start at the same depth
			engine.Processor.RegStack.Value16 = stack;
			engine.Processor.RegFrame.Value16 = frame;

			// (without the temporary game folder, which differs between the two games)
			return $"{failure.GetType().Name}: {System.Text.RegularExpressions.Regex.Replace(failure.Message, @"Civ1GoldenMaster[\\/][0-9a-f]{32}", "<game folder>")}";
		}
		finally
		{
			setDeadline(0);
		}
	}
}
