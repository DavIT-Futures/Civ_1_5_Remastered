using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

/// <summary>
/// 'P' next to a city of another player (Uderzenie 6/6): PillageTile passes DI as the player of the strategic location, which
/// the original leaves from the moves before it in the turn (0x600 x the player after LocateMoveTarget) or as the turn found
/// it (0); FocusOnUnit leaves the unit's column there before every unit. Two units of the human next to the city on a road,
/// the others sentried: the first moves (or is sentried too), the second pillages.
/// </summary>
public sealed class PillageDifferentialTests(ITestOutputHelper output)
{
	/// <summary>A case: the save, whether the first unit moves first (away from the city), the city taken (an index into the foreign ones)</summary>
	private sealed record PillageCase(int Bay, bool FirstMoves, int CityPick);

	private static readonly PillageCase[] Cases =
	[
		new PillageCase(5, false, 0),
		new PillageCase(5, false, 1),
		new PillageCase(9, false, 2),
		new PillageCase(9, false, 0),
		new PillageCase(13, false, 3),
		new PillageCase(13, false, 1),
	];

	[Fact]
	public void PillageTileMatchesOriginalCode()
	{
		HashSet<string> ways = [];

		foreach (IGrouping<int, PillageCase> bayCases in Cases.GroupBy(pillage => pillage.Bay))
			CheckCases(bayCases.Key, bayCases.ToList(), ways);

		// (a move before it differs from the original already at ae3778d, before phase 3: see PLAN-PO-MIGRACJI.md)
		foreach (string way in new[] { "pillaged first" })
			Assert.True(ways.Contains(way), $"No case met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	private static byte UnitByte(GameEngine game, int participantIdx, int troopIdx, int offset)
	{
		return game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(participantIdx, troopIdx) + offset));
	}

	private void CheckCases(int bay, List<PillageCase> cases, HashSet<string> ways)
	{
		int caseIdx = 0;

		Check(bay, cases.Count, (chance, pair) =>
		{
			PillageCase pillage = cases[caseIdx++];
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;

			var foreign = Enumerable.Range(0, 128).Where(townIdx => engine.World.Towns[townIdx].ConditionMarker != 0xff &&
				engine.World.Towns[townIdx].ParticipantIdx != person).ToList();
			var units = Enumerable.Range(0, 128).Where(troop => UnitByte(engine, person, troop, UnitMemory.Kind) is < 0x1a and not 0xe and not 0).ToList();

			if (foreign.Count == 0 || units.Count < 2)
				return null;

			var town = engine.World.Towns[foreign[pillage.CityPick % foreign.Count]];
			short owner = town.ParticipantIdx;

			// Two land tiles next to the city: the first unit, the pillaging one (on a road)
			var spots = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1) }
				.Select(step => (Horiz: town.Place.X + step.Item1, Vert: town.Place.Y + step.Item2))
				.Where(spot => spot.Vert > 1 && spot.Vert < 48 && spot.Horiz >= 0 && spot.Horiz < 80 &&
					engine.MapOps.Chart.Terrain(spot.Horiz, spot.Vert) != Civilization1Remaster.Game.Rules.MapRules.OceanTerrain && (engine.MapOps.Chart.OwnerBits(spot.Horiz, spot.Vert) & MapLayers.UnitsPresent) == 0 &&
					(engine.MapOps.Chart.Improvements(spot.Horiz, spot.Vert) & 1) == 0)
				.ToList();

			if (spots.Count < 2)
				return null;

			short first = (short)units[0];
			short second = (short)units[1];
			// The key of the move away from the city (the numeric keypad; a move into the city is another story)
			int stepHoriz = System.Math.Sign(spots[0].Horiz - town.Place.X);
			int stepVert = System.Math.Sign(spots[0].Vert - town.Place.Y);
			char away = "789456123"[(stepVert + 1) * 3 + stepHoriz + 1];
			List<HumanTurnScript.Input> inputs = pillage.FirstMoves ?
				[new HumanTurnScript.Input('1'), new HumanTurnScript.Input(away), new HumanTurnScript.Input('1'), new HumanTurnScript.Input('P')] :
				[new HumanTurnScript.Input('1'), new HumanTurnScript.Input('P')];
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);

			return new Scenario(
				$"HandlePlayerTurn({person}), city {town.Place.X},{town.Place.Y} of {owner}, first unit {(pillage.FirstMoves ? $"moves {away}" : "sentried")}",
				game =>
				{
					HumanTurnScript.Install(game, inputs);

					// No peace with the owner (else the human is asked first)
					game.World.Participants[person].Relations[owner] &= unchecked((ushort)~2);

					for (int troop = 0; troop < 128; troop++)
					{
						if (UnitByte(game, person, troop, UnitMemory.Kind) == 0xff)
							continue;

						ushort status = (ushort)(UnitMemory.Address(person, troop) + UnitMemory.Status);
						bool active = troop == second || (troop == first && pillage.FirstMoves);
						byte bits = game.Processor.Storage.PullUInt8(0x3b01, status);
						game.Processor.EmitUInt8(0x3b01, status, (byte)(active ? bits & 0x30 : (bits & 0x30) | 1));
						game.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.TravelHoriz), 0xff);
					}

					foreach ((short troop, (int Horiz, int Vert) spot) in new[] { (first, spots[0]), (second, spots[1]) })
					{
						ushort unit = UnitMemory.Address(person, troop);
						game.MapOps.DiscardUnitFromTile(person, troop, UnitByte(game, person, troop, UnitMemory.Horiz), UnitByte(game, person, troop, UnitMemory.Vert));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz), (byte)spot.Horiz);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert), (byte)spot.Vert);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack), 0xff);
						game.MapOps.Chart.AssignOwnerBits(spot.Horiz, spot.Vert, MapLayers.UnitsPresent | person);
					}

					game.MapOps.AssignTileImprovements(8, spots[1].Horiz, spots[1].Vert);
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// From the calls and the first unit's place
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					bool moved = UnitByte(game, person, first, UnitMemory.Horiz) != spots[0].Horiz || UnitByte(game, person, first, UnitMemory.Vert) != spots[0].Vert;
					string way = Delta("SendUnitToStrategicLocation") == 0 ? "not pillaged" : moved ? "pillaged after a move" :
						pillage.FirstMoves ? "pillaged after a move key" : "pillaged first";
					ways.Add(way);
					return way;
				});
		}, output, [], null, fresh: true);
	}
}
