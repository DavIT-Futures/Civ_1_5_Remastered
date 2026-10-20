using System;
using System.Drawing;

namespace Civilization1Original.Game.Rules
{
	/// <summary>
	/// Pure C# domain rules for city citizen specialization (Scientist, Taxman, Entertainer)
	/// and 21-tile city radius indexing.
	/// Translated from original assembly in CIV.EXE segment 0x1e3f (CityManagement).
	/// </summary>
	public static class CitizenSpecialistRules
	{
		/// <summary>
		/// Citizen specialist roles.
		/// </summary>
		public enum SpecialistType : ushort
		{
			Entertainer = 0,
			Taxman = 1,
			Scientist = 2
		}

		/// <summary>
		/// Standard 21 tile offsets around a city center in Civilization 1 (TownDeltas).
		/// Indices 0..19 are the 20 surrounding fat cross tiles, and Index 20 is the city center (0,0).
		/// </summary>
		public static readonly Point[] CityRadiusOffsets = new Point[21]
		{
			new Point(0, -1),
			new Point(1, 0),
			new Point(0, 1),
			new Point(-1, 0),
			new Point(1, -1),
			new Point(1, 1),
			new Point(-1, 1),
			new Point(-1, -1),
			new Point(0, -2),
			new Point(2, 0),
			new Point(0, 2),
			new Point(-2, 0),
			new Point(-1, -2),
			new Point(1, -2),
			new Point(2, -1),
			new Point(2, 1),
			new Point(1, 2),
			new Point(-1, 2),
			new Point(-2, 1),
			new Point(-2, -1),
			new Point(0, 0)
		};

		/// <summary>
		/// Finds the 0..20 index of a tile relative to a city coordinate, or -1 if outside the 21-tile cross.
		/// Original CIV.EXE 0x1e3f:0x0018 (ObtainCityTileIndex).
		/// </summary>
		public static int FindCityTileIndex(int dx, int dy)
		{
			for (int i = 0; i < CityRadiusOffsets.Length; i++)
			{
				if (CityRadiusOffsets[i].X == dx && CityRadiusOffsets[i].Y == dy)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Extracts the specialist type of the citizen at the given slot (0..7).
		/// Original CIV.EXE 0x1e3f:0x6daf (ObtainCitizenType).
		/// Stored in 2 bits per citizen inside LaborerMarkers2 (slots 0..7).
		/// </summary>
		public static SpecialistType GetSpecialistType(ushort laborerMarkers2, int slotIndex)
		{
			if (slotIndex >= 8)
			{
				return SpecialistType.Taxman; // Default fallback in original code: 0x1
			}

			int shift = slotIndex * 2;
			int typeBits = (laborerMarkers2 >> shift) & 0x3;
			return (SpecialistType)typeBits;
		}

		/// <summary>
		/// Assigns the specialist type for the citizen at the given slot (0..7).
		/// Original CIV.EXE 0x1e3f:0x6d9e (AssignCitizenType).
		/// </summary>
		public static ushort SetSpecialistType(ushort laborerMarkers2, int slotIndex, SpecialistType type)
		{
			if (slotIndex >= 8)
			{
				return laborerMarkers2;
			}

			int shift = slotIndex * 2;
			ushort mask = (ushort)~(0x3 << shift);
			ushort valueBits = (ushort)(((ushort)type & 0x3) << shift);

			return (ushort)((laborerMarkers2 & mask) | valueBits);
		}

		/// <summary>
		/// Tallies the count of specialists of a specific type (Entertainer, Taxman, Scientist) in a city.
		/// Original CIV.EXE 0x1e3f:0x6ddc (TallySpecialists).
		/// </summary>
		public static int CountSpecialists(ushort laborerMarkers2, SpecialistType targetType)
		{
			int count = 0;
			for (int slot = 0; slot < 8; slot++)
			{
				if (GetSpecialistType(laborerMarkers2, slot) == targetType)
				{
					count++;
				}
			}
			return count;
		}
	}
}
