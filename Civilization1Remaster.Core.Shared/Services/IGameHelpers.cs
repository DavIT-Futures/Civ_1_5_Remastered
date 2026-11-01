namespace Civilization1Remaster.Core.Shared.Services
{
	public interface IGameHelpers
	{
		void SeedRandomizer();

		int ClampValue(int amount, int floorAmount, int peakAmount);

		ushort FindCityAtPosition(int horizontalLocation, int verticalLocation);

		ushort FindNearestCity(int horizontalLocation, int verticalLocation);

		ushort FindNearestUnit(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation);

		int MergeCoordinates(int horizontalLocation, int verticalLocation);

		ushort ComputeDistanceFromOffsets(int horizontalLocation, int verticalLocation);

		int ObtainDistance(int horizontalLocation, int firstHorizontalSpot, int verticalLocation, int firstVerticalSpot);

		ushort ComputePlayerPopulation(short participantIndex);

		void FormatPopulation(int inhabitants);

		void FillBoxWithMotif(int horizontalLocation, int verticalLocation, int breadth, int altitude);

		void PullAndAssignColors(ushort documentNameAddress);

		void ReleaseResource(ushort resource, ushort charactersAddress);

		void SetupHueCycling();

		void LaunchHueCycling();

		void HaltHueCycling();
	}
}
