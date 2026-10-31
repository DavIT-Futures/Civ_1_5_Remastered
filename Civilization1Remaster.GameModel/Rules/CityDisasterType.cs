using System;

namespace Civilization1Remaster.GameModel.Rules
{
	/// <summary>
	/// Types of city disasters that can strike a city in Civilization 1.
	/// </summary>
	public enum CityDisasterType
	{
		None = -1,
		Earthquake = 0,
		Plague = 1,
		Flood = 2,
		Famine = 3,
		Fire = 4,
		Volcano = 5,
		Pirates = 6,
		CivilDisorder = 7
	}
}
