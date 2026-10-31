namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The counts of the units in a stack (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class StackVariables : VariableGroup
		{
			private readonly GlobalWord kind = new GlobalWord(0x653a);
			private readonly GlobalWord tally = new GlobalWord(0x653c);
			private readonly GlobalWord domainTally = new GlobalWord(0x6532);
			private readonly GlobalWord leader = new GlobalWord(0x6530);
			private readonly GlobalWord domainMode = new GlobalWord(0x6536);
			private readonly GlobalWord indexLimit = new GlobalWord(0x6538);
			private readonly GlobalWord leaderScore = new GlobalWord(0x6534);

			/// <summary>0x653a: the unit kind CountUnitsOfTypeInStack counts</summary>
			public ushort Kind { get => this.kind.Value; set => this.kind.Value = value; }

			/// <summary>0x653c: the units of the kind in the stack</summary>
			public ushort Tally { get => this.tally.Value; set => this.tally.Value = value; }

			/// <summary>0x6532: the total CountUnitsByDomainInStack counts</summary>
			public ushort DomainTally { get => this.domainTally.Value; set => this.domainTally.Value = value; }

			/// <summary>0x6530: the unit that leads the stack</summary>
			public ushort Leader { get => this.leader.Value; set => this.leader.Value = value; }

			/// <summary>0x6536: what CountUnitsByDomainInStack counts</summary>
			public ushort DomainMode { get => this.domainMode.Value; set => this.domainMode.Value = value; }

			/// <summary>0x6538: the units below this index count as defensive units</summary>
			public ushort IndexLimit { get => this.indexLimit.Value; set => this.indexLimit.Value = value; }

			/// <summary>0x6534: the best defence score in the stack so far (its leader)</summary>
			public ushort LeaderScore { get => this.leaderScore.Value; set => this.leaderScore.Value = value; }
		}
	}
}
