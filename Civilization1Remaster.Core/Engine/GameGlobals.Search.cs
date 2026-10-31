namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The searches for the nearest city or unit and the routes (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class SearchVariables : VariableGroup
		{
			private readonly GlobalWord foundDistance = new GlobalWord(0x6c9a);
			private readonly GlobalWord goalHoriz = new GlobalWord(0x6590);
			private readonly GlobalWord goalVert = new GlobalWord(0x6592);
			private readonly GlobalWord foundPlayer = new GlobalWord(0xd7f0);
			private readonly GlobalWord gotoCost = new GlobalWord(0x6794);
			private readonly GlobalWord routePlaceHoriz = new GlobalWord(0x6796);
			private readonly GlobalWord routePlaceVert = new GlobalWord(0x6798);

			/// <summary>0x6c9a: the distance to what the last search for the nearest city or unit found</summary>
			public ushort FoundDistance { get => this.foundDistance.Value; set => this.foundDistance.Value = value; }

			/// <summary>0x6590: the column where the unit heads (see CarryOutGoto)</summary>
			public ushort GoalHoriz { get => this.goalHoriz.Value; set => this.goalHoriz.Value = value; }

			/// <summary>0x6592: the row where the unit heads (see CarryOutGoto)</summary>
			public ushort GoalVert { get => this.goalVert.Value; set => this.goalVert.Value = value; }

			/// <summary>0xd7f0: the player of the unit, or the owner of the tile, found last (also in 0xd20a)</summary>
			public ushort FoundPlayer { get => this.foundPlayer.Value; set => this.foundPlayer.Value = value; }

			/// <summary>0x6794: the cost of the route to the unit</summary>
			public ushort GotoCost { get => this.gotoCost.Value; set => this.gotoCost.Value = value; }

			/// <summary>0x6796: the x of the place of the route</summary>
			public ushort RoutePlaceHoriz { get => this.routePlaceHoriz.Value; set => this.routePlaceHoriz.Value = value; }

			/// <summary>0x6798: the y of the place of the route</summary>
			public ushort RoutePlaceVert { get => this.routePlaceVert.Value; set => this.routePlaceVert.Value = value; }
		}
	}
}
