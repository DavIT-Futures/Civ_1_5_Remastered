namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		public sealed class ScoringVariables : VariableGroup
		{
			private readonly GlobalWord iconHoriz = new GlobalWord(0xb25c);
			private readonly GlobalWord iconVert = new GlobalWord(0xb2bc);
			private readonly GlobalWord hallOfFameScore = new GlobalWord(0xb77e);
			private readonly GlobalWord wonderScore = new GlobalWord(0xd764);
			private readonly GlobalWord citizenScore = new GlobalWord(0xd766);
			private readonly GlobalWord verticalShift = new GlobalWord(0x5116);
			private readonly GlobalWord schismShareDc6a = new GlobalWord(0xdc6a);
			private readonly GlobalWord fallenLeadersCount = new GlobalWord(0xb1d4);

			public ushort IconHoriz { get => this.iconHoriz.Value; set => this.iconHoriz.Value = value; }

			public ushort IconVert { get => this.iconVert.Value; set => this.iconVert.Value = value; }

			public ushort HallOfFameScore { get => this.hallOfFameScore.Value; set => this.hallOfFameScore.Value = value; }

			public ushort WonderScore { get => this.wonderScore.Value; set => this.wonderScore.Value = value; }

			public ushort CitizenScore { get => this.citizenScore.Value; set => this.citizenScore.Value = value; }

			public short VerticalShift { get => (short)this.verticalShift.Value; set => this.verticalShift.Value = (ushort)value; }

			public ushort SchismShareDc6a { get => this.schismShareDc6a.Value; set => this.schismShareDc6a.Value = value; }

			public ushort FallenLeadersCount { get => this.fallenLeadersCount.Value; set => this.fallenLeadersCount.Value = value; }
		}
	}
}
