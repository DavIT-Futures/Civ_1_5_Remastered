namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The score and the Hall of Fame (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0xb25c: the x of the last icon of the score screen</summary>
			public ushort IconHoriz { get => this.iconHoriz.Value; set => this.iconHoriz.Value = value; }

			/// <summary>0xb2bc: the y of the row of icons of the score screen</summary>
			public ushort IconVert { get => this.iconVert.Value; set => this.iconVert.Value = value; }

			/// <summary>0xb77e: the score of the game for the Hall of Fame</summary>
			public ushort HallOfFameScore { get => this.hallOfFameScore.Value; set => this.hallOfFameScore.Value = value; }

			/// <summary>0xd764: the score of the wonders</summary>
			public ushort WonderScore { get => this.wonderScore.Value; set => this.wonderScore.Value = value; }

			/// <summary>0xd766: the score of the citizens</summary>
			public ushort CitizenScore { get => this.citizenScore.Value; set => this.citizenScore.Value = value; }

			/// <summary>0x5116: vertical shift for wonders/score icons</summary>
			public short VerticalShift { get => (short)this.verticalShift.Value; set => this.verticalShift.Value = (ushort)value; }

			/// <summary>0xdc6a: schism share parameter / peace turns in Hall of Fame</summary>
			public ushort SchismShareDc6a { get => this.schismShareDc6a.Value; set => this.schismShareDc6a.Value = value; }

			/// <summary>0xb1d4: count of fallen leaders placed on the conquest screen</summary>
			public ushort FallenLeadersCount { get => this.fallenLeadersCount.Value; set => this.fallenLeadersCount.Value = value; }
		}
	}
}
