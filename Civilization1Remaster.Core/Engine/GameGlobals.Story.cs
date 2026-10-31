namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The story of the intro (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0x3b62: the story text of the intro (0 not started, then telling)</summary>
			public ushort State { get => this.state.Value; set => this.state.Value = value; }

			/// <summary>0x6806: the music time of the next line of the story</summary>
			public ushort LineTime { get => this.lineTime.Value; set => this.lineTime.Value = value; }

			/// <summary>0x3b66: the next byte of the colour cycle records of the story pictures (0x3710)</summary>
			public ushort CycleRecord { get => this.cycleRecord.Value; set => this.cycleRecord.Value = value; }

			/// <summary>0x3b68: the colour cycles of the story picture</summary>
			public ushort CycleCount { get => this.cycleCount.Value; set => this.cycleCount.Value = value; }

			/// <summary>0x6804: the handle of story.txt</summary>
			public ushort File { get => this.file.Value; set => this.file.Value = value; }

			/// <summary>0x67fc: a key may end the story</summary>
			public ushort KeyAllowed { get => this.keyAllowed.Value; set => this.keyAllowed.Value = value; }

			/// <summary>0x67fe: the pixels under the clouds lightened while the world is made</summary>
			public ushort LightenedPixels { get => this.lightenedPixels.Value; set => this.lightenedPixels.Value = value; }

			/// <summary>0x6800: the time of the music in the story told while the world is made (a song position, or the seconds)</summary>
			public ushort MusicTime { get => this.musicTime.Value; set => this.musicTime.Value = value; }

			/// <summary>0x6802: the lines of the story told so far</summary>
			public ushort LinesTold { get => this.linesTold.Value; set => this.linesTold.Value = value; }

			/// <summary>0x32d6: picture handle of the reserved intro screen</summary>
			public ushort IntroReservedScreen { get => this.introReservedScreen.Value; set => this.introReservedScreen.Value = value; }
		}
	}
}
