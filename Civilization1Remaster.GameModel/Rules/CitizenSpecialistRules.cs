using System.Drawing;

namespace Civilization1Remaster.GameModel.Rules
{
	public static class CitizenSpecialistRules
	{
		public enum SpecialistType : ushort
		{
			Entertainer = 0,
			Taxman = 1,
			Scientist = 2
		}

		public static Point[] CityRadiusOffsets { get; } = new Point[21]
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

		public static SpecialistType GetSpecialistType(ushort laborerMarkers2, int slotIndex)
		{
			if (slotIndex >= 8)
			{
				return SpecialistType.Taxman;
			}

			int shift = slotIndex * 2;
			int typeBits = (laborerMarkers2 >> shift) & 0x3;
			return (SpecialistType)typeBits;
		}

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
