using System;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>The operations of GameHelpers that the other parts of the game use</summary>
	public interface IGameHelpers
	{
		/// <summary>
		/// Seeds the random generator with the clock (15 bits of the seconds), kept as the seed of the world (ChanceRoot)
		/// </summary>
		void SeedRandomizer();

		/// <summary>
		/// Check and adjusts value to given range
		/// </summary>
		/// <param name="value"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		int VerifyValueSpan(int amount, int floorAmount, int peakAmount);

		/// <summary>
		/// The city standing on the tile
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		ushort LocateCityAtPosition(int horizLoc, int vertLoc);

		/// <summary>
		/// The nearest city of any player (MapRules.Distance, the first one of equally near cities).
		/// Its distance goes to 0x6c9a, 999 when there is no city.
		/// </summary>
		/// <returns>City index, or 0xffff</returns>
		ushort LocateNearestCity(int horizLoc, int vertLoc);

		/// <summary>
		/// The nearest other unit of the player (MapRules.Distance, the first one of equally near units).
		/// Its distance goes to 0x6c9a, 999 when there is none.
		/// </summary>
		/// <param name="troopIdx">The unit itself, it is skipped</param>
		/// <returns>Unit index, or 0xffff</returns>
		ushort LocateNearestUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// The larger of the two steps plus half of the smaller one (MapRules.LongerPlusHalfShorter)
		/// </summary>
		int MergeCoordinates(int horizLoc, int vertLoc);

		/// <summary>
		/// Twice the larger step plus the smaller one (MapRules.DoubledDistance)
		/// </summary>
		ushort ObtainDistanceFromOffsets(int horizLoc, int vertLoc);

		/// <summary>
		/// Distance of two positions (MapRules.Distance)
		/// </summary>
		int ObtainDistance(int horizLoc, int horizSpot1, int vertLoc, int vertSpot1);

		/// <summary>
		/// Population of the civilization (PopulationRules.CityPopulation of all its cities), also written as text
		/// by PopulationToText
		/// </summary>
		ushort ObtainPlayerPopulation(short participantIdx);

		/// <summary>
		/// Converts population to string
		/// </summary>
		/// <param name="population"></param>
		void PopulationToText(int inhabitants);

		/// <summary>
		/// Fills the box with the background motif: on VGA tiles of 32 x 16 from (288, 120) of the picture screen, on EGA
		/// colour 7
		/// </summary>
		void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude);

		/// <summary>
		/// On VGA (0xd762) loads the colours of the picture file into the palette record at 0xbdee, puts back the 48 colour
		/// bytes kept at 0x6b34 at its start, sets the palette and colour 45
		/// </summary>
		void PullAndAssignColors(ushort docnameAddr);

		/// <summary>
		/// Free resource, show Memory error dialog if error happens
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="stringPtr"></param>
		void ReleaseResource(ushort resource, ushort charsAddr);

		/// <summary>
		/// Sets the colour cycles 1 - 3 of the map: the colours 96 - 103, 104 - 111 and 112 - 127, 15 beats a step
		/// </summary>
		void SetupHueCycling();

		/// <summary>Starts the colour cycles 1 - 3 (on VGA, unless they run already, HueCyclingOn)</summary>
		void LaunchHueCycling();

		/// <summary>Stops the colour cycles 1 - 3 (on VGA, when they run)</summary>
		void HaltHueCycling();
	}
}
