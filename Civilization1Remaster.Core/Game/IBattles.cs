namespace Civilization1Remaster.Core.Game
{
	/// <summary>The operations of Battles that the other parts of the game use</summary>
	public interface IBattles
	{
		/// <summary>
		/// Resolves or estimates (`marker == 0`) combat between attacker (`participantIdx1`, `troopIdx1`) and defender
		/// (`participantIdx2`, `troopIdx2`), including treaty confirmation, hit-point combat rounds, map/explosion animation,
		/// diplomacy updates, nuclear detonation, stack/city aftermath, and veteran promotion.
		/// </summary>
		ushort SettleBattle(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, ushort marker);

		/// <summary>
		/// Puts the unit into the stack of another unit of the same player, right after it. The units of a stack form a ring
		/// through the FollowingInStack field; a unit alone has -1 there.
		/// </summary>
		/// <param name="stackTroopIdx">A unit of the stack</param>
		void AttachUnitToStack(short participantIdx, short troopIdx, ushort stackTroopIdx);

		/// <summary>
		/// Takes the unit out of its stack (the ring of FollowingInStack), the unit stands alone afterwards.
		/// Like the original, the unit before it is searched in at most 32 steps.
		/// </summary>
		void RemoveUnitFromStack(short participantIdx, short troopIdx);

		/// <summary>
		/// Before the human player attacks a player with whom it has a peace treaty (Relations bit 1): asks whether to break the
		/// treaty; the Senate overrules it under governments 4 and up
		/// </summary>
		/// <returns>0 without a treaty, 1 when the treaty is broken, 0xffff when the action is cancelled (also in AX)</returns>
		ushort AffirmBreakTreaty(short participantIdx);

		/// <summary>
		/// A nuclear attack by the player on the tile: a message "... use nuclear weapons!" (unless the human attacks), the
		/// explosion, then on the tile and the 8 around it (headings 8 down to 0) every unit stack is destroyed (counted in
		/// TroopsRuined of the attacker, the victim 0xd7f0 turns against it) unless the tile is a city with SDI (structure bit
		/// 0 of the second word); last the fallout. The stack frame and the saved SI stay, and SI holds 2 * heading in the
		/// loop, like in the original: a called function copies SI into DI, which the caller gets back.
		/// </summary>
		void ExplodeNuclearWeapon(short participantIdx, short horizLoc, short vertLoc);

		/// <summary>
		/// The fallout of a nuclear attack on the tile and the 8 tiles around it (direction tables 0x1882 and 0x18e4): a city
		/// there loses half of its size, any other tile gets pollution with chance 2 in 3 (unless its terrain is 10); every tile
		/// is drawn again
		/// </summary>
		void EnactNuclearFallout(short horizLoc, short vertLoc);
	}
}
