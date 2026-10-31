using System.Collections.Generic;
using System.IO;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential;
using Civilization1Remaster.Tests.GoldenMaster;
using Civilization1Remaster.UI;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// A unit of the human pillages a road next to a city of another player (PlayerTurn.PillageTile): off (the game) the
	/// owner of the city gets the tile as a strategic location to defend (role 1, priority 4); on (ClassicPillagePlayer, the
	/// tests) the player is DI as the original leaves it (the unit's column), so the owner's table stays as it was.
	/// </summary>
	public class PillagePlayerTests
	{
		[Theory]
		[InlineData(false, 5)]
		[InlineData(true, 5)]
		[InlineData(false, 13)]
		[InlineData(true, 13)]
		public void OwnerDefendsThePillagedTile(bool classic, int bay)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"), classic: true);
			game.LoadAutosave(bay, GoldenMasterTests.RandomSeed);
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicPillagePlayer = classic;
			short person = engine.World.PersonParticipantIdx;

			byte UnitByte(int troop, int offset) => engine.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + offset));

			// A land unit of the human next to a city of another player, on a road; the other units sentried
			var town = Enumerable.Range(0, 128).Select(townIdx => engine.World.Towns[townIdx])
				.First(town => town.ConditionMarker != 0xff && town.ParticipantIdx != person && town.ParticipantIdx != 0);
			short owner = town.ParticipantIdx;
			(int Horiz, int Vert) spot = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1) }
				.Select(step => (Horiz: town.Place.X + step.Item1, Vert: town.Place.Y + step.Item2))
				.First(place => place.Vert > 1 && place.Vert < 48 && place.Horiz >= 0 && place.Horiz < 80 &&
					engine.MapOps.Chart.Terrain(place.Horiz, place.Vert) != MapRules.OceanTerrain &&
					(engine.MapOps.Chart.OwnerBits(place.Horiz, place.Vert) & MapLayers.UnitsPresent) == 0 &&
					(engine.MapOps.Chart.Improvements(place.Horiz, place.Vert) & 1) == 0);
			short troop = (short)Enumerable.Range(0, 128).First(unit => UnitByte(unit, UnitMemory.Kind) is < 0x1a and not 0xe and not 0);

			HumanTurnScript.Install(engine, [new HumanTurnScript.Input('1'), new HumanTurnScript.Input('P')]);
			engine.World.Participants[person].Relations[owner] &= unchecked((ushort)~2);

			for (int unit = 0; unit < 128; unit++)
			{
				if (UnitByte(unit, UnitMemory.Kind) == 0xff)
					continue;

				ushort status = (ushort)(UnitMemory.Address(person, unit) + UnitMemory.Status);
				byte bits = engine.Processor.Storage.PullUInt8(0x3b01, status);
				engine.Processor.EmitUInt8(0x3b01, status, (byte)(unit == troop ? bits & 0x30 : (bits & 0x30) | 1));
				engine.Processor.EmitUInt8(0x3b01, (ushort)(UnitMemory.Address(person, unit) + UnitMemory.TravelHoriz), 0xff);
			}

			ushort address = UnitMemory.Address(person, troop);
			engine.MapOps.DiscardUnitFromTile(person, troop, UnitByte(troop, UnitMemory.Horiz), UnitByte(troop, UnitMemory.Vert));
			engine.Processor.EmitUInt8(0x3b01, (ushort)(address + UnitMemory.Horiz), (byte)spot.Horiz);
			engine.Processor.EmitUInt8(0x3b01, (ushort)(address + UnitMemory.Vert), (byte)spot.Vert);
			engine.Processor.EmitUInt8(0x3b01, (ushort)(address + UnitMemory.FollowingInStack), 0xff);
			engine.MapOps.Chart.AssignOwnerBits(spot.Horiz, spot.Vert, MapLayers.UnitsPresent | person);
			engine.MapOps.AssignTileImprovements(8, spot.Horiz, spot.Vert);

			bool Listed() => Enumerable.Range(0, 0x20).Any(placeIdx =>
			{
				ushort place = (ushort)((owner << 5) + placeIdx);
				return engine.Processor.PullInt8(0x3b01, (ushort)(place + ArmySelection.StrategicLocationHorizTableAddress)) == spot.Horiz &&
					engine.Processor.PullInt8(0x3b01, (ushort)(place + ArmySelection.StrategicLocationVertTableAddress)) == spot.Vert &&
					engine.Processor.PullInt8(0x3b01, (ushort)(place + ArmySelection.StrategicLocationRoleTableAddress)) == 1;
			});

			Assert.False(Listed());

			engine.TurnLogic.HandlePlayerTurn(person);

			Assert.Equal(0, engine.MapOps.Chart.Improvements(spot.Horiz, spot.Vert) & 8);
			Assert.Equal(!classic, Listed());
		}
	}
}
