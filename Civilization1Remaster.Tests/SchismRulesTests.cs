using System.Collections.Generic;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class SchismRulesTests
	{
		[Fact]
		public void FindAvailableRebelSlot_FindsFirstEmptySlot()
		{
			// Slots: 0=Barbarians, 1=Active, 2=Active, 3=Dead (troops=0, cities=0), 4=Active
			var troops = new int[] { 10, 5, 8, 0, 12, 0, 0, 0 };
			var cities = new int[] { 0, 3, 4, 0, 6, 0, 0, 0 };

			int slot = SchismRules.FindAvailableRebelSlot(troops, cities);
			Assert.Equal(3, slot);
		}

		[Fact]
		public void FindAvailableRebelSlot_ReturnsMinusOneWhenAllOccupied()
		{
			var troops = new int[] { 10, 5, 5, 5, 5, 5, 5, 5 };
			var cities = new int[] { 0, 2, 2, 2, 2, 2, 2, 2 };

			int slot = SchismRules.FindAvailableRebelSlot(troops, cities);
			Assert.Equal(-1, slot);
		}

		[Fact]
		public void ComputeAssetSplit_HalvesGoldAndMight()
		{
			// 501 gold -> 250 rebel, 251 parent
			// 100 might -> 50 rebel, 50 parent
			var (parentGold, rebelGold, parentMight, rebelMight) = SchismRules.ComputeAssetSplit(501, 100);

			Assert.Equal(251, parentGold);
			Assert.Equal(250, rebelGold);
			Assert.Equal(50, parentMight);
			Assert.Equal(50, rebelMight);
		}

		[Fact]
		public void DetermineDefectingCities_FarthestCitiesDefectFirst()
		{
			var candidates = new List<(int CityId, int DistanceToCapital, int CitySize)>
			{
				(CityId: 1, DistanceToCapital: 5, CitySize: 4),
				(CityId: 2, DistanceToCapital: 25, CitySize: 3),
				(CityId: 3, DistanceToCapital: 15, CitySize: 5),
				(CityId: 4, DistanceToCapital: 2, CitySize: 6)
			};

			// Target rebel pop: 7.
			// Farthest is City 2 (dist 25, size 3), then City 3 (dist 15, size 5). Total size = 8 >= 7.
			var defecting = SchismRules.DetermineDefectingCities(candidates, targetRebelPopulation: 7);

			Assert.Equal(2, defecting.Count);
			Assert.Equal(2, defecting[0]);
			Assert.Equal(3, defecting[1]);
		}

		[Fact]
		public void FindGeometricCenterCity_FindsCityWithMinSumOfDistances()
		{
			var towns = new City[128];
			for (int i = 0; i < towns.Length; i++)
			{
				towns[i] = new City { ConditionMarker = 0xff, ParticipantIdx = -1 };
			}

			// Town 0 at (0, 0)
			towns[0] = new City { ConditionMarker = 0, ParticipantIdx = 1, Place = new System.Drawing.Point(0, 0) };
			// Town 1 at (10, 0) - Center!
			towns[1] = new City { ConditionMarker = 0, ParticipantIdx = 1, Place = new System.Drawing.Point(10, 0) };
			// Town 2 at (20, 0)
			towns[2] = new City { ConditionMarker = 0, ParticipantIdx = 1, Place = new System.Drawing.Point(20, 0) };

			// Distance from Town 1: to 0 is 10, to 2 is 10 -> sum = 20.
			// Distance from Town 0: to 1 is 10, to 2 is 20 -> sum = 30.
			// Distance from Town 2: to 0 is 20, to 1 is 10 -> sum = 30.
			int center = SchismRules.FindGeometricCenterCity(towns, 1, (x1, x2, y1, y2) => System.Math.Abs(x1 - x2) + System.Math.Abs(y1 - y2));

			Assert.Equal(1, center);
		}
	}
}
