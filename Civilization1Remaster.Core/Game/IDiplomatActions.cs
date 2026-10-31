using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of DiplomatActions that the other parts of the game use</summary>
	public interface IDiplomatActions
	{
		/// <summary>
		/// A diplomat of the player arrives in the city. The human player chooses (0xb276 masks: 1 a known civilization or
		/// barbarians, 4 already robbed, 0x10 a capital, 0x20 barbarians): embassy, investigate, steal technology, sabotage,
		/// incite a revolt, meet the king. An AI player steals (a diplomat at peace with the human player in his city just
		/// disappears); stealing breaks a treaty of the human player and, without a technology to take, becomes sabotage for
		/// the AI. The diplomat is used up, except for the audience. SI and DI get the values of the original before the calls.
		/// </summary>
		void DiplomatArrivesAtCity(short townIdx, short participantIdx, short troopIdx);

		/// <summary>
		/// A diplomat of participantIdx1 bribes the unit (only a unit without others in its stack): the price is (gold + 750) /
		/// (distance to the owner's capital + 2) * the unit's cost, halved for all but settlers. The AI pays when it has twice the
		/// price; the human player is asked ("Forget it." / "Pay" when the treasury covers it). The unit changes sides (seen by its
		/// former owner) and the human player, if bribed, is told. The stack frame and the saved SI and DI stay like in the
		/// original: TallyUnitsByDomainInStack changes SI.
		/// </summary>
		void BuyUnit(short participantIdx, short troopIdx, short participantIdx1);

		/// <summary>
		/// The nuclear explosion on the tile: 28 pictures cut from the explosion picture (0x5318) into the stack frame, the
		/// sound, then 35 steps of the animation (the last 7 repeat the pictures 21 - 27), each put on the screen for a while and
		/// taken away again from the back screen.
		/// </summary>
		void RenderExplosion(short horizLoc, short vertLoc);

		/// <summary>
		/// In fact the revolt of a city: it goes over to the player (for -1 to the owner of the city of another player with the
		/// highest (food surplus * 32) / distance, scanning the cities from 0 until the first city that does not qualify; when
		/// none, nothing happens). The new owner sees around the city; the human player hears of it when it is involved or knows
		/// one of them (the map shows the city when it sees it). The old owner's units in and next to the city (not in another
		/// city) change sides, then the city is seized (without the animations with one disk, 0x28bc set).
		/// </summary>
		void ComputeInciteCost(short townIdx, short participantIdx);

		/// <summary>
		/// A diplomat establishes an embassy... in fact investigates the city: the title "... founded: year." over a picture with
		/// four walking figures (cut from 0x536e) moving to the right until a key or a click (frame by
		/// frame, 10 timer ticks each), then the city screen (unless a key is still waiting).
		/// </summary>
		void ExamineCity(short townIdx);
	}
}
