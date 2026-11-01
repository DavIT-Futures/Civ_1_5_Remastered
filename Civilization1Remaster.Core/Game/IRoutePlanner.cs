namespace Civilization1Remaster.Core.Game
{
	public interface IRoutePlanner
	{
		ushort CarryOutGoto(short participantIdx, short troopIdx);

		ushort ChartRoute(short participantIdx, short troopIdx);

		ushort FindRouteStep(int horizLoc, int vertLoc, ushort marker);

		ushort ShiftToward(short participantIdx, short troopIdx, ushort costLimit);

		ushort ComputeRouteCost(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort marker, ushort costLimit);

		int TunePosition(int horizLoc);
	}
}
