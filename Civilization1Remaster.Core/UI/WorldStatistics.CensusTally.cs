using System;

namespace Civilization1Remaster.Core.UI
{
	partial class WorldStatistics
	{
		/// <summary>The sums of the demographics per player 0 - 7</summary>
		private sealed class CensusTally
		{
			/// <summary>City sizes (+1, so never 0)</summary>
			public short[] Size { get; } = { 1, 1, 1, 1, 1, 1, 1, 1 };

			/// <summary>Land tiles owned</summary>
			public short[] Land { get; } = new short[8];

			/// <summary>Taxes * 2 + 0xe17a of the cities</summary>
			public short[] Money { get; } = new short[8];

			/// <summary>Sizes of the cities with the improvements 0x20 and 0x800 (each counts)</summary>
			public short[] Learning { get; } = new short[8];

			/// <summary>Sizes of the cities with the improvements 0x4 and 0x100; later the life expectancy</summary>
			public short[] Health { get; } = new short[8];

			/// <summary>Luxury (0x70da) - 2 * size</summary>
			public short[] Unrest { get; } = new short[8];

			/// <summary>Size + happy (0x70e2) - unhappy (0x70e4)</summary>
			public short[] Approval { get; } = new short[8];

			/// <summary>Luxury + shields + trade (0x70da, 0x70dc, 0x70de)</summary>
			public short[] Output { get; } = new short[8];

			/// <summary>Shields (0x70dc)</summary>
			public short[] Shields { get; } = new short[8];

			/// <summary>Pollution: shields / 0x6c98 - 20 + size * 0xb882 / 4, when above 0</summary>
			public short[] Pollution { get; } = new short[8];
		}
	}
}
