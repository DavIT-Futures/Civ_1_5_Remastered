namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
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

			public ushort Tiles { get => this.tiles.Value; set => this.tiles.Value = value; }

			public ushort Owner { get => this.owner.Value; set => this.owner.Value = value; }

			public ushort Food { get => this.food.Value; set => this.food.Value = value; }

			public ushort Shields { get => this.shields.Value; set => this.shields.Value = value; }

			public ushort Trade { get => this.trade.Value; set => this.trade.Value = value; }

			public ushort Luxuries { get => this.luxuries.Value; set => this.luxuries.Value = value; }

			public ushort Happy { get => this.happy.Value; set => this.happy.Value = value; }

			public ushort Unhappy { get => this.unhappy.Value; set => this.unhappy.Value = value; }

			public ushort Science { get => this.science.Value; set => this.science.Value = value; }

			public ushort HumanTileNear { get => this.humanTileNear.Value; set => this.humanTileNear.Value = value; }

			public ushort Specialists { get => this.specialists.Value; set => this.specialists.Value = value; }

			public ushort ScreenTab { get => this.screenTab.Value; set => this.screenTab.Value = value; }

			public ushort AttackersAway { get => this.attackersAway.Value; set => this.attackersAway.Value = value; }

			public ushort Corruption { get => this.corruption.Value; set => this.corruption.Value = value; }

			public ushort UnitUpkeep { get => this.unitUpkeep.Value; set => this.unitUpkeep.Value = value; }

			public ushort SpecialSlots { get => this.specialSlots.Value; set => this.specialSlots.Value = value; }

			public ushort Taxes { get => this.taxes.Value; set => this.taxes.Value = value; }

			public ushort Settlers { get => this.settlers.Value; set => this.settlers.Value = value; }

			public ushort Again { get => this.again.Value; set => this.again.Value = value; }

			public ushort ShownCity { get => this.shownCity.Value; set => this.shownCity.Value = value; }

			public ushort PalaceDistance { get => this.palaceDistance.Value; set => this.palaceDistance.Value = value; }

			public ushort ExcessUnhappy { get => this.excessUnhappy.Value; set => this.excessUnhappy.Value = value; }

			public ushort TileDotted { get => this.tileDotted.Value; set => this.tileDotted.Value = value; }
		}
	}
}
