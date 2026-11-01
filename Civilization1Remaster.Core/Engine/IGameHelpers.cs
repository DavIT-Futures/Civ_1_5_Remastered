using System;

namespace Civilization1Remaster.Core.Engine
{
	public interface IGameHelpers
	{
		void SeedRandomizer();

		int ClampValue(int amount, int floorAmount, int peakAmount);

		ushort FindCityAtPosition(int horizLoc, int vertLoc);

		ushort FindNearestCity(int horizLoc, int vertLoc);

		ushort FindNearestUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc);

		int MergeCoordinates(int horizLoc, int vertLoc);

		ushort ComputeDistanceFromOffsets(int horizLoc, int vertLoc);

		int ObtainDistance(int horizLoc, int horizSpot1, int vertLoc, int vertSpot1);

		ushort ComputePlayerPopulation(short participantIdx);

		void FormatPopulation(int inhabitants);

		void FillBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude);

		void PullAndAssignColors(ushort docnameAddr);

		void ReleaseResource(ushort resource, ushort charsAddr);

		void SetupHueCycling();

		void LaunchHueCycling();

		void HaltHueCycling();
	}
}
