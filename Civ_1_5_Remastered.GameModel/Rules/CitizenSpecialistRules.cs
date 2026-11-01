using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static partial class CitizenSpecialistRules
	{
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

		public static int FindCityTileIndex(int deltaHorizontal, int deltaVertical)
		{
			for (int index = 0; index < CityRadiusOffsets.Length; index++)
			{
				if (CityRadiusOffsets[index].X == deltaHorizontal && CityRadiusOffsets[index].Y == deltaVertical)
				{
					return index;
				}
			}

			return -1;
		}

		public static SpecialistType GetSpecialistType(ushort specialistMarkers, int slotIndex)
		{
			if (slotIndex >= 8)
			{
				return SpecialistType.Taxman;
			}

			int shift = slotIndex * 2;
			int typeBits = (specialistMarkers >> shift) & 0x3;
			return (SpecialistType)typeBits;
		}

		public static ushort SetSpecialistType(ushort specialistMarkers, int slotIndex, SpecialistType type)
		{
			if (slotIndex >= 8)
			{
				return specialistMarkers;
			}

			int shift = slotIndex * 2;
			ushort mask = (ushort)~(0x3 << shift);
			ushort valueBits = (ushort)(((ushort)type & 0x3) << shift);

			return (ushort)((specialistMarkers & mask) | valueBits);
		}

		public static int CountSpecialists(ushort specialistMarkers, SpecialistType targetType)
		{
			int count = 0;
			for (int slot = 0; slot < 8; slot++)
			{
				if (GetSpecialistType(specialistMarkers, slot) == targetType)
				{
					count++;
				}
			}

			return count;
		}
	}
}
