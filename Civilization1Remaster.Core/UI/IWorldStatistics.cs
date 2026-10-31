using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of WorldStatistics that the other parts of the game use</summary>
	public interface IWorldStatistics
	{
		/// <summary>
		/// The world map, 4 pixels a tile, of what the human player has seen (everything with the cheat 0xd806): shifted so that
		/// his start column (HorizBegin) is in the middle and his units and cities in the middle rows (all rows when he has seen
		/// a pole); terrain as land / sea (marker 0) or as the small pictures of 0x19e8, cities he knows in their owner's colour,
		/// units he sees as a small box in their owner's colour. Waits for a key or a click unless 0xb278 is 1. The final values
		/// of the locals stay in the stack frame, like in the original.
		/// </summary>
		void DisplayWorldMap(ushort marker);

		/// <summary>
		/// The (debug) screen of the continents: for the players 1 - 7 their name and unit picture, and for every continent 1 - 14
		/// its value (0xdcfe) and, for the players with attack there, "defence/attack/cities"; waits for a key or a click and
		/// puts the screen back
		/// </summary>
		void DisplayGameFigures();

		/// <summary>
		/// The graph of the civilizations' scores from the replay history (0x3772: a byte per player for every 4 turns): year lines
		/// every 50 turns (labelled every 100), the names of the players 1 - 8 that live or were in the game (player 8 reads the
		/// next table, like in the original), and a line per player 1 - 7 scaled to the highest value (at least 50), until a key
		/// or a click
		/// </summary>
		void DisplayCivilizationGraph();

		/// <summary>
		/// The Wonders of the World: a box for every built wonder (1 - 21) in the colour of the city's player, with its icon,
		/// "The " (wonders 1 - 7) and its name, and "in"/"of" (0x440e, 0x4413) the city and civilization, or " (Destroyed)" for
		/// city 128; pages of up to 7 boxes, each after a key or a click
		/// </summary>
		void DisplayWondersOfTheWorld();

		/// <summary>
		/// A historian's ranking of the living civilizations by one of five measures (random): 0 gold, 1 might, 2 technologies,
		/// 3 the size plus the happy minus the unhappy citizens of the cities, 4 the size of the cities. The places go into
		/// RunningSagaStanding (7 - place, the barbarians +1); known civilizations (all on the easiest level) are shown with
		/// their small palace.
		/// </summary>
		void DisplayEpicChronicle();

		/// <summary>
		/// The demographics of the player: statistics of all cities (each computed by HandleCity) and land tiles per player,
		/// then 13 lines with the player's value and rank (RenderCensusLine): approval, population, GNP, manufacturing goods,
		/// land area, literacy, disease, pollution, life expectancy, family size, military service, annual income, productivity.
		/// The values shown are kept in the frame (bp - 0xa8, read by RenderCensusLine) like the original.
		/// </summary>
		void DisplayCensus(short participantIdx);

		/// <summary>
		/// A line of the demographics: the human player's rank ("1st" ...) among the living players 1 - 7 by the words at valuesAddr
		/// (one per player; with a negative row they are negated first, a lower value is better), and the best player's value
		/// "(civilization: value text)" in its colour when the human player has relations bit 0x40 towards it. The values are
		/// read from memory as they change, like in the original: the human player's own value is negated in the loop too.
		/// </summary>
		void RenderCensusLine(ushort valuesAddr, short vertLoc, ushort charsAddr);

		/// <summary>RenderCensusLine of the values of the players 1 - 7 in a C# array (made positive in it for a negative row)</summary>
		void RenderCensusLine(short[] values, short vertLoc, ushort charsAddr);
	}
}
