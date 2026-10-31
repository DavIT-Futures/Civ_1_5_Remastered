namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The city in its turn and the city screen (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class CityTurnVariables : VariableGroup
		{
			private readonly GlobalWord tiles = new GlobalWord(0x6540);
			private readonly GlobalWord owner = new GlobalWord(0x6548);
			private readonly GlobalWord food = new GlobalWord(0x70da);
			private readonly GlobalWord shields = new GlobalWord(0x70dc);
			private readonly GlobalWord trade = new GlobalWord(0x70de);
			private readonly GlobalWord luxuries = new GlobalWord(0x70e0);
			private readonly GlobalWord happy = new GlobalWord(0x70e2);
			private readonly GlobalWord unhappy = new GlobalWord(0x70e4);
			private readonly GlobalWord science = new GlobalWord(0x70e6);
			private readonly GlobalWord humanTileNear = new GlobalWord(0x70e8);
			private readonly GlobalWord specialists = new GlobalWord(0xe8b8);
			private readonly GlobalWord screenTab = new GlobalWord(0x2496);
			private readonly GlobalWord attackersAway = new GlobalWord(0x6546);
			private readonly GlobalWord corruption = new GlobalWord(0xd2e0);
			private readonly GlobalWord unitUpkeep = new GlobalWord(0xd2f6);
			private readonly GlobalWord specialSlots = new GlobalWord(0xdeb8);
			private readonly GlobalWord taxes = new GlobalWord(0xe17a);
			private readonly GlobalWord settlers = new GlobalWord(0xe3c6);
			private readonly GlobalWord again = new GlobalWord(0xb1e8);
			private readonly GlobalWord shownCity = new GlobalWord(0x653e);
			private readonly GlobalWord palaceDistance = new GlobalWord(0x6b30);
			private readonly GlobalWord excessUnhappy = new GlobalWord(0x6542);
			private readonly GlobalWord tileDotted = new GlobalWord(0x2494);

			/// <summary>0x6540: the tiles around the city a worker may take (0x15)</summary>
			public ushort Tiles { get => this.tiles.Value; set => this.tiles.Value = value; }

			/// <summary>0x6548: the owner of the city in its turn</summary>
			public ushort Owner { get => this.owner.Value; set => this.owner.Value = value; }

			/// <summary>0x70da: the food of the city in its turn</summary>
			public ushort Food { get => this.food.Value; set => this.food.Value = value; }

			/// <summary>0x70dc: the shields of the city in its turn</summary>
			public ushort Shields { get => this.shields.Value; set => this.shields.Value = value; }

			/// <summary>0x70de: the trade of the city in its turn</summary>
			public ushort Trade { get => this.trade.Value; set => this.trade.Value = value; }

			/// <summary>0x70e0: the luxuries of the city in its turn</summary>
			public ushort Luxuries { get => this.luxuries.Value; set => this.luxuries.Value = value; }

			/// <summary>0x70e2: the happy citizens of the city in its turn</summary>
			public ushort Happy { get => this.happy.Value; set => this.happy.Value = value; }

			/// <summary>0x70e4: the unhappy citizens of the city in its turn</summary>
			public ushort Unhappy { get => this.unhappy.Value; set => this.unhappy.Value = value; }

			/// <summary>0x70e6: the science of the city in its turn (by the rates of the owner)</summary>
			public ushort Science { get => this.science.Value; set => this.science.Value = value; }

			/// <summary>0x70e8: 1 when a tile around the city belongs to the human player</summary>
			public ushort HumanTileNear { get => this.humanTileNear.Value; set => this.humanTileNear.Value = value; }

			/// <summary>0xe8b8: the specialists of the city (bits 26 - 31 of the workers)</summary>
			public ushort Specialists { get => this.specialists.Value; set => this.specialists.Value = value; }

			/// <summary>0x2496: the tab lit on the city screen (0 - 3)</summary>
			public ushort ScreenTab { get => this.screenTab.Value; set => this.screenTab.Value = value; }

			/// <summary>0x6546: the attack units of the city away from it (planes always)</summary>
			public ushort AttackersAway { get => this.attackersAway.Value; set => this.attackersAway.Value = value; }

			/// <summary>0xd2e0: the corruption of the city in its turn</summary>
			public ushort Corruption { get => this.corruption.Value; set => this.corruption.Value = value; }

			/// <summary>0xd2f6: the shields the units of the city cost in its turn</summary>
			public ushort UnitUpkeep { get => this.unitUpkeep.Value; set => this.unitUpkeep.Value = value; }

			/// <summary>0xdeb8: the special slots of the city in use (Unfamiliar)</summary>
			public ushort SpecialSlots { get => this.specialSlots.Value; set => this.specialSlots.Value = value; }

			/// <summary>0xe17a: the taxes of the city in its turn</summary>
			public ushort Taxes { get => this.taxes.Value; set => this.taxes.Value = value; }

			/// <summary>0xe3c6: the settlers of the city in its turn</summary>
			public ushort Settlers { get => this.settlers.Value; set => this.settlers.Value = value; }

			/// <summary>0xb1e8: a city is handled once more with its screen (2 again)</summary>
			public ushort Again { get => this.again.Value; set => this.again.Value = value; }

			/// <summary>0x653e: the city of the city screen</summary>
			public ushort ShownCity { get => this.shownCity.Value; set => this.shownCity.Value = value; }

			/// <summary>0x6b30: the distance of the city to the palace of its owner</summary>
			public ushort PalaceDistance { get => this.palaceDistance.Value; set => this.palaceDistance.Value = value; }

			/// <summary>0x6542: the unhappy citizens above the size of the city</summary>
			public ushort ExcessUnhappy { get => this.excessUnhappy.Value; set => this.excessUnhappy.Value = value; }

			/// <summary>0x2494: a tile of the city was marked with a dot (taken by another city) while the tiles were counted</summary>
			public ushort TileDotted { get => this.tileDotted.Value; set => this.tileDotted.Value = value; }
		}
	}
}
