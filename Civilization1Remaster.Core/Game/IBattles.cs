namespace Civilization1Remaster.Core.Game
{
	public interface IBattles
	{
		ushort SettleBattle(short firstParticipantIndex, short firstTroopIndex, short secondParticipantIndex, short secondTroopIndex, ushort marker);

		void AttachUnitToStack(short participantIndex, short troopIndex, ushort stackTroopIndex);

		void RemoveUnitFromStack(short participantIndex, short troopIndex);

		ushort AffirmBreakTreaty(short participantIndex);

		void ExplodeNuclearWeapon(short participantIndex, short horizontalLocation, short verticalLocation);

		void EnactNuclearFallout(short horizontalLocation, short verticalLocation);
	}
}
