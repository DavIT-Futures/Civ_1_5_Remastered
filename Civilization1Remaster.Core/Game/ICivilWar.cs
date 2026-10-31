using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of CivilWar that the other parts of the game use</summary>
	public interface ICivilWar
	{
		/// <summary>
		/// A civil war splits the player: in the first free slot (no units, no cities) a new civilization rises (none or
		/// CreateCivilization fails: 0), with a new leader, half the treasury and might, the same research, government and
		/// known technologies count, at war with the player (relation 9; the technologies are copied onto the player himself
		/// and the second relation goes to the player's own row too, like in the original). It gets the continents beyond the
		/// capital's when they make between half and all of the rest, else the cities farthest from the capital until it has
		/// enough; the player's units follow their cities, units in the new civilization's cities or homed there change sides
		/// (with their stacks). Both get a capital (the human player's map moves home). Returns 1.
		/// </summary>
		ushort DivideCivilization(short participantIdx);

		/// <summary>
		/// The city goes to the player: its owner, the map layer of the owners and the total size of the cities of the player
		/// </summary>
		void AssignCityOwner(short townIdx, short participantIdx);

		/// <summary>
		/// The most central city of the player: the one with the smallest sum of distances to all its cities (a signed word sum,
		/// the first one of equal sums; sums of 0x7fff and more never count)
		/// </summary>
		/// <returns>City index, 0xffff when none (also in AX)</returns>
		ushort ComputeDistanceToCapital(short participantIdx);
	}
}
