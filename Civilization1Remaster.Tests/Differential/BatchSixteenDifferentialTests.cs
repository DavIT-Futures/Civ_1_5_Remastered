using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The coast check, the unit drawn on a tile, the revolution screen and the city name search against their original code
	/// </summary>
	public class BatchSixteenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void VerifyCoastalTileMatchesOriginalCode()
		{
			Check(3, 400, (chance, pair) =>
			{
				(int horizLoc, int vertLoc) = RandomTile(pair.Original.Engine, chance);
				int terrain = pair.Original.Engine.MapOps.Chart.Terrain(horizLoc, vertLoc);

				return new Scenario($"VerifyCoastalTile({horizLoc}, {vertLoc})", game => { },
					game => new TurnControllerReference(game).CheckCoastalTile(horizLoc, vertLoc),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.TurnLogic.CheckCoastalTile(horizLoc, vertLoc)),
					Register.Acc | Register.Kept,
					game => terrain == 0xb ? "terrain 11" : game.Processor.RegAcc.Value16 != 0 ? "coastal" : "inland");
			}, output, ["terrain 11", "coastal", "inland"]);
		}

		[Fact]
		public void RepaintUnitTileMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				short participantIdx = (short)chance.Next(8);
				short[] own = Enumerable.Range(0, UnitMemory.TroopTally).Select(idx => (short)idx)
					.Where(idx => troops.Exists(participantIdx, idx)).ToArray();
				short[] ships = own.Where(idx => troops.KindField(participantIdx, idx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits).ToArray();
				short[] land = own.Where(idx => troops.KindField(participantIdx, idx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits).ToArray();

				if (own.Length == 0)
					return null;

				int kind = ships.Length > 0 && land.Length > 1 ? chance.Next(3) : 0;
				short troopIdx = kind == 0 ? own[chance.Next(own.Length)] : land[0];
				short ship = ships.Length > 0 ? ships[chance.Next(ships.Length)] : (short)0;
				short partner = kind == 1 ? ship : land.Length > 1 ? land[1] : (short)0;

				return new Scenario($"RepaintUnitTile({participantIdx}, {troopIdx}), " + (kind switch { 1 => $"with ship {ship}", 2 => $"with land unit {partner} at sea", _ => "as it is" }),
					game =>
					{
						if (kind == 0)
							return;

						UnitMemory memory = new UnitMemory(game.Processor);

						// The unit and its partner on the ship's tile (at sea), in a ring of two
						foreach (short unit in new[] { troopIdx, partner })
						{
							memory.Assign(participantIdx, unit, UnitMemory.Horiz, memory.Pull(participantIdx, ship, UnitMemory.Horiz));
							memory.Assign(participantIdx, unit, UnitMemory.Vert, memory.Pull(participantIdx, ship, UnitMemory.Vert));
						}

						memory.AssignFollowing(participantIdx, troopIdx, (byte)partner);
						memory.AssignFollowing(participantIdx, partner, (byte)troopIdx);
					},
					game => new MapServiceReference(game).RepaintUnitTile(participantIdx, troopIdx),
					game => game.MapOps.RepaintUnitTile(participantIdx, troopIdx),
					Register.Kept, game => kind switch { 1 => "ship drawn", 2 => "itself at sea", _ => "as it is" });
			}, output, ["ship drawn", "itself at sea", "as it is"]);
		}

		[Fact]
		public void DisplayRevolutionScreenMatchesOriginalCode()
		{
			Check(10, 12, (chance, pair) =>
			{
				short regime = (short)(chance.Next(3) == 0 ? 0 : chance.Next(1, 6));

				return new Scenario($"DisplayRevolutionScreen(), government {regime}",
					game => game.World.Participants[game.World.PersonParticipantIdx].RegimeKind = regime,
					game => new NewGameSetupReference(game).ShowRevolutionScreen(),
					game => game.NewGameWizard.ShowRevolutionScreen(),
					Register.Kept, game => regime == 0 ? "government 0" : "shown");
			}, output, ["government 0", "shown"]);
		}

		[Fact]
		public void DisplayLabelInputBoxMatchesOriginalCode()
		{
			// "q" (the first name of the list becomes "Qx", given to a city in some scenarios), other letters, nothing
			int[] keys = new[] { "q", "zq", "", "a", "q" }.SelectMany(name => name.Select(letter => (int)letter).Append(0x0d)).ToArray();

			Check(3, 30, (chance, pair) =>
			{
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short named = chance.Next(2) == 0 ? towns[chance.Next(towns.Length)] : (short)-1;

				return new Scenario($"DisplayLabelInputBox(0x53ce), city with the first name {named}",
					game =>
					{
						game.World.TownLabels[0] = "Qx".PadRight(game.World.TownLabels[0].Length, '\0');

						if (named >= 0)
							game.World.Towns[named].LabelIdx = 0;
					},
					game => new CityNamingScreenReference(game).ShowLabelInputBox(0x53ce),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.CityNaming.ShowLabelInputBox(0x53ce)),
					Register.Acc | Register.Kept, game => game.Processor.RegAcc.Value16 == 0xffff ? "no city" : "city found");
			}, output, ["no city", "city found"], keys);
		}
	}
}
