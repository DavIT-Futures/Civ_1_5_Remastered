namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		public sealed class SearchVariables : VariableGroup
		{
			private readonly GlobalWord foundDistance = new GlobalWord(0x6c9a);
			private readonly GlobalWord goalHoriz = new GlobalWord(0x6590);
			private readonly GlobalWord goalVert = new GlobalWord(0x6592);
			private readonly GlobalWord foundPlayer = new GlobalWord(0xd7f0);
			private readonly GlobalWord gotoCost = new GlobalWord(0x6794);
			private readonly GlobalWord routePlaceHoriz = new GlobalWord(0x6796);
			private readonly GlobalWord routePlaceVert = new GlobalWord(0x6798);

			public ushort FoundDistance { get => this.foundDistance.Value; set => this.foundDistance.Value = value; }

			public ushort GoalHoriz { get => this.goalHoriz.Value; set => this.goalHoriz.Value = value; }

			public ushort GoalVert { get => this.goalVert.Value; set => this.goalVert.Value = value; }

			public ushort FoundPlayer { get => this.foundPlayer.Value; set => this.foundPlayer.Value = value; }

			public ushort GotoCost { get => this.gotoCost.Value; set => this.gotoCost.Value = value; }

			public ushort RoutePlaceHoriz { get => this.routePlaceHoriz.Value; set => this.routePlaceHoriz.Value = value; }

			public ushort RoutePlaceVert { get => this.routePlaceVert.Value; set => this.routePlaceVert.Value = value; }
		}
	}
}
