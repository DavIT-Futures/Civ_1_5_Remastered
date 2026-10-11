using System;
using System.Collections.Generic;

namespace Civilization1Remaster.Game.Rules
{
	/// <summary>
	/// Manages stacks of units on tiles.
	/// In the original CIV.EXE engine, unit stacking was maintained as a circular or null-terminated linked list
	/// via the byte offset 0x81de (FollowingTroopIdx in modern C#).
	/// </summary>
	public static class UnitStack
	{
		public const short EndOfStack = -1;
		public const int MaxStackSearchDepth = 32;

		/// <summary>
		/// Attaches a unit to an existing stack anchored at anchorUnitIdx.
		/// Original logic from CIV.EXE 0x29f3:0x0b8a / UnitCombat.AttachUnitToStack.
		/// </summary>
		/// <param name="getFollowing">Callback to retrieve FollowingTroopIdx for a given unit.</param>
		/// <param name="setFollowing">Callback to update FollowingTroopIdx for a given unit.</param>
		/// <param name="unitIdx">The unit being attached to the stack.</param>
		/// <param name="anchorUnitIdx">The anchor unit already on the tile.</param>
		public static void AttachUnit(
			Func<short, short> getFollowing,
			Action<short, short> setFollowing,
			short unitIdx,
			short anchorUnitIdx)
		{
			short existingNext = getFollowing(anchorUnitIdx);
			if (existingNext == EndOfStack)
			{
				setFollowing(anchorUnitIdx, unitIdx);
				setFollowing(unitIdx, anchorUnitIdx);
			}
			else
			{
				setFollowing(unitIdx, existingNext);
				setFollowing(anchorUnitIdx, unitIdx);
			}
		}

		/// <summary>
		/// Discards/unlinks a unit from its stack.
		/// Original logic from CIV.EXE 0x29f3:0x0be0 / UnitCombat.DiscardUnitFromStack.
		/// </summary>
		/// <param name="getFollowing">Callback to retrieve FollowingTroopIdx for a given unit.</param>
		/// <param name="setFollowing">Callback to update FollowingTroopIdx for a given unit.</param>
		/// <param name="unitIdx">The unit being removed from the stack.</param>
		public static void DiscardUnit(
			Func<short, short> getFollowing,
			Action<short, short> setFollowing,
			short unitIdx)
		{
			short nextIdx = getFollowing(unitIdx);
			if (nextIdx == EndOfStack)
			{
				return;
			}

			// If the next unit points back to this unit, the stack had exactly 2 units.
			if (getFollowing(nextIdx) == unitIdx)
			{
				setFollowing(nextIdx, EndOfStack);
			}
			else
			{
				// Otherwise, traverse the linked cycle to find the predecessor pointing to unitIdx.
				short curr = nextIdx;
				int iterations = 0;
				while (iterations < MaxStackSearchDepth && curr != EndOfStack)
				{
					short candidate = getFollowing(curr);
					iterations++;
					if (candidate == unitIdx)
					{
						break;
					}
					curr = candidate;
				}

				if (iterations < MaxStackSearchDepth && curr != EndOfStack)
				{
					setFollowing(curr, getFollowing(unitIdx));
				}
			}

			setFollowing(unitIdx, EndOfStack);
		}

		/// <summary>
		/// Enumerates all units in a stack starting at startUnitIdx (up to maxCount, avoiding infinite loops).
		/// </summary>
		public static List<short> EnumerateStack(Func<short, short> getFollowing, short startUnitIdx, int maxCount = 10)
		{
			var result = new List<short>();
			short current = startUnitIdx;

			for (int i = 0; i < maxCount; i++)
			{
				result.Add(current);
				short next = getFollowing(current);
				if (next == EndOfStack || next == startUnitIdx)
				{
					break;
				}
				current = next;
			}

			return result;
		}
	}
}
