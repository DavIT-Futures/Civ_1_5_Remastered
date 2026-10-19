namespace Civilization1Remaster.Game.State
{
	/// <summary>
	/// The improvements of a city as bits of its structure words (City.StructureMarkers0 / 1): the improvement n of the
	/// game's list (1 the palace … 21 the nuclear plant) is the bit n - 1; the spaceship parts and the wonders have none.
	/// </summary>
	public enum CityStructure
	{
		Palace = 0,
		Barracks = 1,
		Granary = 2,
		Temple = 3,
		Marketplace = 4,
		Library = 5,
		Courthouse = 6,
		CityWalls = 7,
		Aqueduct = 8,
		Bank = 9,
		Cathedral = 10,
		University = 11,
		MassTransit = 12,
		Colosseum = 13,
		Factory = 14,
		ManufacturingPlant = 15,
		SdiDefense = 16,
		RecyclingCenter = 17,
		PowerPlant = 18,
		HydroPlant = 19,
		NuclearPlant = 20,
	}
}
