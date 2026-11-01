namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IRoutePlanner
	{
		ushort CarryOutGoto(short participantIndex, short troopIndex);

		ushort ChartRoute(short participantIndex, short troopIndex);

		ushort FindRouteStep(int horizontalLocation, int verticalLocation, ushort marker);

		ushort ShiftToward(short participantIndex, short troopIndex, ushort costLimit);

		ushort ComputeRouteCost(int horizontalLocation, int verticalLocation, int firstHorizontalSpot, int firstVerticalSpot, ushort marker, ushort costLimit);

		int TunePosition(int horizontalLocation);
	}
}
