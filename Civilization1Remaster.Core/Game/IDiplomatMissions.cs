namespace Civilization1Remaster.Core.Game
{
	public interface IDiplomatMissions
	{
		void DiplomatArrivesAtCity(short townIndex, short participantIndex, short troopIndex);

		void BuyUnit(short participantIndex, short troopIndex, short firstParticipantIndex);

		void DrawExplosion(short horizontalLocation, short verticalLocation);

		void ComputeInciteCost(short townIndex, short participantIndex);

		void ExamineCity(short townIndex);
	}
}
