namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class StackVariables : VariableGroup
		{
			private readonly GlobalWord kind = new GlobalWord(0x653a);
			private readonly GlobalWord tally = new GlobalWord(0x653c);
			private readonly GlobalWord domainTally = new GlobalWord(0x6532);
			private readonly GlobalWord leader = new GlobalWord(0x6530);
			private readonly GlobalWord domainMode = new GlobalWord(0x6536);
			private readonly GlobalWord indexLimit = new GlobalWord(0x6538);
			private readonly GlobalWord leaderScore = new GlobalWord(0x6534);

			public ushort Kind { get => this.kind.Value; set => this.kind.Value = value; }

			public ushort Tally { get => this.tally.Value; set => this.tally.Value = value; }

			public ushort DomainTally { get => this.domainTally.Value; set => this.domainTally.Value = value; }

			public ushort Leader { get => this.leader.Value; set => this.leader.Value = value; }

			public ushort DomainMode { get => this.domainMode.Value; set => this.domainMode.Value = value; }

			public ushort IndexLimit { get => this.indexLimit.Value; set => this.indexLimit.Value = value; }

			public ushort LeaderScore { get => this.leaderScore.Value; set => this.leaderScore.Value = value; }
		}
	}
}
