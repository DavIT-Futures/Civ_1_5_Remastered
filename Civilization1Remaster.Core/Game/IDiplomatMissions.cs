namespace Civilization1Remaster.Core.Game
{
	public interface IDiplomatMissions
	{
		void DiplomatArrivesAtCity(short townIdx, short participantIdx, short troopIdx);

		void BuyUnit(short participantIdx, short troopIdx, short participantIdx1);

		void DrawExplosion(short horizLoc, short vertLoc);

		void ComputeInciteCost(short townIdx, short participantIdx);

		void ExamineCity(short townIdx);
	}
}
