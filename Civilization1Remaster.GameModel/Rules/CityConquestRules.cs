using System;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Pure domain rules for city conquest, plunder, and post-capture state updates.
	/// </summary>
	public static class CityConquestRules
	{
		/// <summary>
		/// Calculates the building destruction bitmask after capturing a city.
		/// In original Civ1:
		/// Base buildings bitmask is masked with 0xaaaa >> dice(2), structure markers 1 is cleared to 0.
		/// If marker != 0 (e.g. bought by diplomat revolt), no additional random building destruction occurs.
		/// In all cases, bits 0 (palace) and certain wonders are preserved/cleared appropriately:
		/// StructureMarkers0 &= 0xfbb6 (clears palace bit 0x1, etc.).
		/// </summary>
		public static ushort ComputeCapturedBuildingFlags(ushort originalMarkers0, int diceTwoOutcome, bool isPeacefulCapture)
		{
			// Basic destruction mask applied when conquered
			// In original: StructureMarkers0 &= 0xfbb6
			ushort markers = (ushort)(originalMarkers0 & 0xfbb6);

			if (!isPeacefulCapture)
			{
				// 0xaaaa shifted right by 0 or 1
				ushort destructionMask = (ushort)(0xaaaa >> diceTwoOutcome);
				markers &= destructionMask;
			}

			return markers;
		}

		/// <summary>
		/// Calculates the new population size of a captured city.
		/// If barbarous/raided or conquered (marker == 0), size decreases by 1 (minimum 0).
		/// If size was already <= 1 and marker != 0, it does not decrease.
		/// </summary>
		public static int ComputeCapturedCitySize(int currentSize, bool isPeacefulCapture)
		{
			if (isPeacefulCapture && currentSize <= 1)
			{
				return currentSize;
			}

			int newSize = currentSize - 1;
			return Math.Max(0, newSize);
		}

		/// <summary>
		/// Checks if a conquered city with size <= 0 is completely destroyed / wiped from the map.
		/// </summary>
		public static bool IsCityDestroyed(int size)
		{
			return size <= 0;
		}

		/// <summary>
		/// Clamps plunder to victim's available gold.
		/// </summary>
		public static int ClampPlunder(int plunderAmount, int victimGold)
		{
			return Math.Max(0, Math.Min(plunderAmount, victimGold));
		}

		/// <summary>
		/// Computes the gold transfer on capture.
		/// </summary>
		public static (int plunderedGold, int victimRemainingGold, int captorNewGold) ComputeGoldTransfer(
			int plunderAmount, int victimGold, int captorGold)
		{
			int plunder = ClampPlunder(plunderAmount, victimGold);
			int remainingVictimGold = victimGold - plunder;
			int newCaptorGold = captorGold + plunder;
			return (plunder, remainingVictimGold, newCaptorGold);
		}

		/// <summary>
		/// Determines whether a unit from the conquered city's garrison should be eliminated.
		/// Units stationed outside their home city are disbanded upon capture.
		/// Units stationed inside the city are disbanded if the city is wiped out (size == 0).
		/// </summary>
		public static bool ShouldEliminateGarrison(int unitBaseTownIdx, int conqueredTownIdx, int unitX, int unitY, int cityX, int cityY, int cityTrueExtent)
		{
			if (unitBaseTownIdx != conqueredTownIdx)
			{
				return false;
			}

			return unitX != cityX || unitY != cityY || cityTrueExtent <= 0;
		}

		/// <summary>
		/// Determines whether capturing a victim's city qualifies for a civil war (schism).
		/// Conditions:
		/// 1. Victim's capital (has palace) captured by military conquest (not diplomatic bribe).
		/// 2. Victim has more than 4 cities.
		/// 3. Human player's standing is strictly less than victim's standing.
		/// </summary>
		public static bool ShouldTriggerSchism(bool hasPalace, bool isPeacefulCapture, int victimTownCount, int humanStanding, int victimStanding)
		{
			return hasPalace && !isPeacefulCapture && victimTownCount > 4 && humanStanding < victimStanding;
		}

		/// <summary>
		/// Determines if a barbarian raid completely razes a city (size set to 0).
		/// Occurs when barbarians capture with a weak unit (assault power &lt;= 2) and global flag 0x28bc is 0.
		/// </summary>
		public static bool ShouldBarbarianRaidRazeCity(int assaultPower, ushort globalFlag28bc)
		{
			return assaultPower <= 2 && globalFlag28bc == 0;
		}
	}
}
