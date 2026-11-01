namespace Civilization1Remaster.Core.Game
{
	public interface IBattles
	{
		ushort SettleBattle(short participantIdx1, short troopIdx1, short participantIdx2, short troopIdx2, ushort marker);

		void AttachUnitToStack(short participantIdx, short troopIdx, ushort stackTroopIdx);

		void RemoveUnitFromStack(short participantIdx, short troopIdx);

		ushort AffirmBreakTreaty(short participantIdx);

		void ExplodeNuclearWeapon(short participantIdx, short horizLoc, short vertLoc);

		void EnactNuclearFallout(short horizLoc, short vertLoc);
	}
}
