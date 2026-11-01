namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class StoryVariables : VariableGroup
		{
			private readonly GlobalWord state = new GlobalWord(0x3b62);
			private readonly GlobalWord lineTime = new GlobalWord(0x6806);
			private readonly GlobalWord cycleRecord = new GlobalWord(0x3b66);
			private readonly GlobalWord cycleCount = new GlobalWord(0x3b68);
			private readonly GlobalWord file = new GlobalWord(0x6804);
			private readonly GlobalWord keyAllowed = new GlobalWord(0x67fc);
			private readonly GlobalWord lightenedPixels = new GlobalWord(0x67fe);
			private readonly GlobalWord musicTime = new GlobalWord(0x6800);
			private readonly GlobalWord linesTold = new GlobalWord(0x6802);
			private readonly GlobalWord introReservedScreen = new GlobalWord(0x32d6);

			public ushort State { get => this.state.Value; set => this.state.Value = value; }

			public ushort LineTime { get => this.lineTime.Value; set => this.lineTime.Value = value; }

			public ushort CycleRecord { get => this.cycleRecord.Value; set => this.cycleRecord.Value = value; }

			public ushort CycleCount { get => this.cycleCount.Value; set => this.cycleCount.Value = value; }

			public ushort File { get => this.file.Value; set => this.file.Value = value; }

			public ushort KeyAllowed { get => this.keyAllowed.Value; set => this.keyAllowed.Value = value; }

			public ushort LightenedPixels { get => this.lightenedPixels.Value; set => this.lightenedPixels.Value = value; }

			public ushort MusicTime { get => this.musicTime.Value; set => this.musicTime.Value = value; }

			public ushort LinesTold { get => this.linesTold.Value; set => this.linesTold.Value = value; }

			public ushort IntroReservedScreen { get => this.introReservedScreen.Value; set => this.introReservedScreen.Value = value; }
		}
	}
}
