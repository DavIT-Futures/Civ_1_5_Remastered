namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class SearchVariables : VariableGroup
		{
			private readonly GlobalWord foundDistance = new GlobalWord(0x6c9a);
			private readonly GlobalWord goalHorizontal = new GlobalWord(0x6590);
			private readonly GlobalWord goalVertical = new GlobalWord(0x6592);
			private readonly GlobalWord foundPlayer = new GlobalWord(0xd7f0);
			private readonly GlobalWord gotoCost = new GlobalWord(0x6794);
			private readonly GlobalWord routePlaceHorizontal = new GlobalWord(0x6796);
			private readonly GlobalWord routePlaceVertical = new GlobalWord(0x6798);

			public ushort FoundDistance { get => this.foundDistance.Value; set => this.foundDistance.Value = value; }

			public ushort GoalHorizontal { get => this.goalHorizontal.Value; set => this.goalHorizontal.Value = value; }

			public ushort GoalVertical { get => this.goalVertical.Value; set => this.goalVertical.Value = value; }

			public ushort FoundPlayer { get => this.foundPlayer.Value; set => this.foundPlayer.Value = value; }

			public ushort GotoCost { get => this.gotoCost.Value; set => this.gotoCost.Value = value; }

			public ushort RoutePlaceHorizontal { get => this.routePlaceHorizontal.Value; set => this.routePlaceHorizontal.Value = value; }

			public ushort RoutePlaceVertical { get => this.routePlaceVertical.Value; set => this.routePlaceVertical.Value = value; }
		}
	}
}
