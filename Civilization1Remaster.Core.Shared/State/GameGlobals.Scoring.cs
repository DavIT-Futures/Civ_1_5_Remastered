namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class ScoringVariables : VariableGroup
		{
			private readonly GlobalWord iconHorizontal = new GlobalWord(0xb25c);
			private readonly GlobalWord iconVertical = new GlobalWord(0xb2bc);
			private readonly GlobalWord hallOfFameScore = new GlobalWord(0xb77e);
			private readonly GlobalWord wonderScore = new GlobalWord(0xd764);
			private readonly GlobalWord citizenScore = new GlobalWord(0xd766);
			private readonly GlobalWord verticalShift = new GlobalWord(0x5116);
			private readonly GlobalWord schismShare = new GlobalWord(0xdc6a);
			private readonly GlobalWord fallenLeadersCount = new GlobalWord(0xb1d4);

			public ushort IconHorizontal { get => this.iconHorizontal.Value; set => this.iconHorizontal.Value = value; }

			public ushort IconVertical { get => this.iconVertical.Value; set => this.iconVertical.Value = value; }

			public ushort HallOfFameScore { get => this.hallOfFameScore.Value; set => this.hallOfFameScore.Value = value; }

			public ushort WonderScore { get => this.wonderScore.Value; set => this.wonderScore.Value = value; }

			public ushort CitizenScore { get => this.citizenScore.Value; set => this.citizenScore.Value = value; }

			public short VerticalShift { get => (short)this.verticalShift.Value; set => this.verticalShift.Value = (ushort)value; }

			public ushort SchismShare { get => this.schismShare.Value; set => this.schismShare.Value = value; }

			public ushort FallenLeadersCount { get => this.fallenLeadersCount.Value; set => this.fallenLeadersCount.Value = value; }
		}
	}
}
