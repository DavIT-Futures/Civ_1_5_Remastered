using System;
using System.Collections.Generic;
using Civilization1Original.Game.State;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for Civilization civil wars / schisms.
	/// Translated from original assembly in CIV.EXE overlay 15 (CivilizationSchism).
	/// </summary>
	public static class SchismRules
	{
		/// <summary>
		/// Evaluates whether an available slot exists for a rebel civilization to be spawned.
		/// Original CIV.EXE 0x18ba:0x0017 (DivideCivilization).
		/// </summary>
		/// <param name="activePlayerTroopCounts">Array of active troop tallies per player index (1..7).</param>
		/// <param name="activePlayerCityCounts">Array of active city tallies per player index (1..7).</param>
		/// <returns>Free slot index (1..7) or -1 if all civ slots are currently in use.</returns>
		public static int FindAvailableRebelSlot(int[] activePlayerTroopCounts, int[] activePlayerCityCounts)
		{
			for (int slot = 1; slot < 8; slot++)
			{
				if (slot < activePlayerTroopCounts.Length && slot < activePlayerCityCounts.Length)
				{
					if (activePlayerTroopCounts[slot] == 0 && activePlayerCityCounts[slot] == 0)
					{
						return slot;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Computes the split of assets (gold and military power) between parent empire and the rebel faction.
		/// Original CIV.EXE 0x18ba:0x023f.
		/// Treasury gold and martial might are split in half.
		/// </summary>
		public static (int parentGold, int rebelGold, int parentMight, int rebelMight) ComputeAssetSplit(int currentGold, int currentMight)
		{
			int rebelGold = currentGold / 2;
			int parentGold = currentGold - rebelGold;

			int rebelMight = currentMight / 2;
			int parentMight = currentMight / 2;

			return (parentGold, rebelGold, parentMight, rebelMight);
		}

		/// <summary>
		/// Determines which cities switch allegiance to the rebel faction.
		/// Cities farther away from the capital are transferred one by one until the rebel faction
		/// accumulates a fair proportion of the empire's population.
		/// </summary>
		/// <param name="candidateCityDistancesAndSizes">List of (CityId, DistanceToCapital, CitySize).</param>
		/// <param name="targetRebelPopulation">Target population extent for the rebellion.</param>
		/// <returns>List of city IDs that defect to the rebel civilization.</returns>
		public static List<int> DetermineDefectingCities(
			List<(int CityId, int DistanceToCapital, int CitySize)> candidateCityDistancesAndSizes,
			int targetRebelPopulation)
		{
			// Sort candidates by distance descending (farthest cities defect first)
			var sorted = new List<(int CityId, int DistanceToCapital, int CitySize)>(candidateCityDistancesAndSizes);
			sorted.Sort((a, b) => b.DistanceToCapital.CompareTo(a.DistanceToCapital));

			var defecting = new List<int>();
			int accumulatedPop = 0;

			foreach (var item in sorted)
			{
				defecting.Add(item.CityId);
				accumulatedPop += item.CitySize;
				if (accumulatedPop >= targetRebelPopulation)
				{
					break;
				}
			}

			return defecting;
		}

		/// <summary>
		/// Finds the city that serves as the geometric center of a civilization (minimizes total distance to all other cities).
		/// Original CIV.EXE 0x18ba:0x08df (ObtainDistanceToCapital).
		/// </summary>
		public static int FindGeometricCenterCity(City[] towns, short participantIdx, Func<int, int, int, int, int> distanceFunc)
		{
			int bestCity = -1;
			int minTotalDistance = int.MaxValue;

			for (int i = 0; i < towns.Length; i++)
			{
				if (towns[i].ConditionMarker != 0xff && towns[i].ParticipantIdx == participantIdx)
				{
					int totalDistance = 0;
					for (int j = 0; j < towns.Length; j++)
					{
						if (towns[j].ConditionMarker != 0xff && towns[j].ParticipantIdx == participantIdx)
						{
							totalDistance += distanceFunc(towns[i].Place.X, towns[j].Place.X, towns[i].Place.Y, towns[j].Place.Y);
						}
					}

					if (totalDistance < minTotalDistance)
					{
						minTotalDistance = totalDistance;
						bestCity = i;
					}
				}
			}

			return bestCity;
		}
	}
}
