using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	partial class WorldMap
	{
		/// <summary>The sums of the demographics per player 0 - 7</summary>
		private sealed class CensusTally
		{
			/// <summary>City sizes (+1, so never 0)</summary>
			public readonly short[] Size = { 1, 1, 1, 1, 1, 1, 1, 1 };

			/// <summary>Land tiles owned</summary>
			public readonly short[] Land = new short[8];

			/// <summary>Taxes * 2 + 0xe17a of the cities</summary>
			public readonly short[] Money = new short[8];

			/// <summary>Sizes of the cities with the improvements 0x20 and 0x800 (each counts)</summary>
			public readonly short[] Learning = new short[8];

			/// <summary>Sizes of the cities with the improvements 0x4 and 0x100; later the life expectancy</summary>
			public readonly short[] Health = new short[8];

			/// <summary>Luxury (0x70da) - 2 * size</summary>
			public readonly short[] Unrest = new short[8];

			/// <summary>Size + happy (0x70e2) - unhappy (0x70e4)</summary>
			public readonly short[] Approval = new short[8];

			/// <summary>Luxury + shields + trade (0x70da, 0x70dc, 0x70de)</summary>
			public readonly short[] Output = new short[8];

			/// <summary>Shields (0x70dc)</summary>
			public readonly short[] Shields = new short[8];

			/// <summary>Pollution: shields / 0x6c98 - 20 + size * 0xb882 / 4, when above 0</summary>
			public readonly short[] Pollution = new short[8];
		}
	}
}
