using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// A unit entering an empty city of its own civilization does not conquer it again: the city keeps its size,
	/// its buildings and its owner, also on the map once the last unit leaves it.
	/// </summary>
	public class OwnCityTests
	{
		[Fact]
		public async Task SeizingYourOwnCityChangesNothing()
		{
			if (GameFiles.Folder == null || GameFiles.Find("*.SVE").FirstOrDefault() is not string save)
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(Path.GetFileName(save));

				if (!game.Loaded)
				{
					return;
				}

				var engine = game.Engine;

				for (short townIndex = 0; townIndex < engine.World.Towns.Length; townIndex++)
				{
					City town = engine.World.Towns[townIndex];

					if (town.ConditionMarker == 0xff)
					{
						continue;
					}

					short owner = town.ParticipantIndex;
					sbyte size = town.TrueExtent;
					ushort structures = town.StructureMarkersLow;
					int towns = engine.World.Participants[owner].TownTally;

					engine.Conquest.SeizeCity(owner, townIndex, 0);

					Assert.Equal(owner, town.ParticipantIndex);
					Assert.Equal(size, town.TrueExtent);
					Assert.Equal(structures, town.StructureMarkersLow);
					Assert.Equal(towns, engine.World.Participants[owner].TownTally);
				}
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}

		[Fact]
		public async Task AnEmptyCityKeepsItsOwnerOnTheMap()
		{
			if (GameFiles.Folder == null || GameFiles.Find("*.SVE").FirstOrDefault() is not string save)
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(Path.GetFileName(save));

				if (!game.Loaded)
				{
					return;
				}

				var engine = game.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);

				foreach (City town in engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIndex != 0))
				{
					short owner = town.ParticipantIndex;
					bool empty = Enumerable.Range(0, UnitMemory.TroopTally).All(troop => troops.Pull(owner, (short)troop, UnitMemory.Kind) == 0xff ||
						troops.PullSigned(owner, (short)troop, UnitMemory.Horizontal) != town.Place.X ||
						troops.PullSigned(owner, (short)troop, UnitMemory.Vertical) != town.Place.Y);

					if (!empty)
					{
						continue;
					}

					// The map still marks units in the city, though none is left: looking for them clears the mark. Before,
					// the city tile was left to nobody (0), and the owner's next unit to enter conquered its own city.
					engine.Services.AssignDot(2, town.Place.X + 160, town.Place.Y, (ushort)(owner + 8));

					Assert.Equal(0xffff, engine.MapOperations.ObtainUnitAtTile(town.Place.X, town.Place.Y));
					Assert.Equal((int)owner, (int)engine.VideoCard.ObtainDot(2, town.Place.X + 160, town.Place.Y));
					return;
				}
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
