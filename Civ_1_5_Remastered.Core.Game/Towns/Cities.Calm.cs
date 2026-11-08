namespace Civ_1_5_Remastered.Core.Game.Towns
{
	public partial class Cities
	{
		private const int SpecialistShift = 26;
		private const uint TileBits = 0xfffff;

		public bool CalmCity(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			for (int step = 0; step <= town.TrueExtent; step++)
			{
				if ((short)HandleCity(townIndex, -1) >= 0)
				{
					return true;
				}

				uint workers = ((uint)town.LaborerMarkersHigh << 16) | town.LaborerMarkersLow;
				int specialists = (int)(workers >> SpecialistShift);
				int weakest = -1;
				int weakestValue = int.MaxValue;

				for (int tile = 0; tile < 20; tile++)
				{
					if ((workers & TileBits & (1u << tile)) == 0)
					{
						continue;
					}

					int horizontalLocation = town.Place.X + this.Owner.TownDeltas[tile].X;
					int verticalLocation = town.Place.Y + this.Owner.TownDeltas[tile].Y;
					int value = (2 * MayCityWorkTile(horizontalLocation, verticalLocation, 0)) +
						MayCityWorkTile(horizontalLocation, verticalLocation, 1) +
						MayCityWorkTile(horizontalLocation, verticalLocation, 2);

					if (value < weakestValue)
					{
						weakestValue = value;
						weakest = tile;
					}
				}

				if (weakest < 0)
				{
					return false;
				}

				workers = (workers & 0x3ffffff & ~(1u << weakest)) | ((uint)(specialists + 1) << SpecialistShift);
				town.LaborerMarkersLow = (ushort)workers;
				town.LaborerMarkersHigh = (ushort)(workers >> 16);
				town.SpecialistMarkers = CitizenSpecialistRules.SetSpecialistType(town.SpecialistMarkers, specialists, CitizenSpecialistRules.SpecialistType.Entertainer);
			}

			return (short)HandleCity(townIndex, -1) >= 0;
		}
	}
}
