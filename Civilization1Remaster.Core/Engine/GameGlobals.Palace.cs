namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The palace screen (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class PalaceVariables : VariableGroup
		{
			private readonly GlobalWord partWidth = new GlobalWord(0x4b2e);
			private readonly GlobalWord firstPicture = new GlobalWord(0x6828);
			private readonly GlobalWord leftBushes = new GlobalWord(0xd930);
			private readonly GlobalWord rightBushes = new GlobalWord(0xd934);
			private readonly GlobalWord wingShift = new GlobalWord(0x4b32);
			private readonly GlobalWord layout = new GlobalWord(0xb22a);
			private readonly GlobalWord leftWingWidth = new GlobalWord(0x4b2c);
			private readonly GlobalWord rightWingWidth = new GlobalWord(0x4b34);
			private readonly GlobalWord styleShift = new GlobalWord(0xd922);
			private readonly GlobalWord gardenBackground = new GlobalWord(0xd932);

			/// <summary>0x4b2e: the width of a part of the palace</summary>
			public ushort PartWidth { get => this.partWidth.Value; set => this.partWidth.Value = value; }

			/// <summary>0x6828: the first picture of the palace parts (0xffff unset)</summary>
			public ushort FirstPicture { get => this.firstPicture.Value; set => this.firstPicture.Value = value; }

			/// <summary>0xd930: the bushes left of the palace</summary>
			public ushort LeftBushes { get => this.leftBushes.Value; set => this.leftBushes.Value = value; }

			/// <summary>0xd934: the bushes right of the palace</summary>
			public ushort RightBushes { get => this.rightBushes.Value; set => this.rightBushes.Value = value; }

			/// <summary>0x4b32: the shift of a palace wing from its end</summary>
			public ushort WingShift { get => this.wingShift.Value; set => this.wingShift.Value = value; }

			/// <summary>0xb22a: a choice of the palace layout: 1 moves the parts one column to the right</summary>
			public ushort Layout { get => this.layout.Value; set => this.layout.Value = value; }

			/// <summary>0x4b2c: width of the palace left wing</summary>
			public ushort LeftWingWidth { get => this.leftWingWidth.Value; set => this.leftWingWidth.Value = value; }

			/// <summary>0x4b34: width of the palace right wing</summary>
			public ushort RightWingWidth { get => this.rightWingWidth.Value; set => this.rightWingWidth.Value = value; }

			/// <summary>0xd922: palace style horizontal shift</summary>
			public ushort StyleShift { get => this.styleShift.Value; set => this.styleShift.Value = value; }

			/// <summary>0xd932: palace garden background index</summary>
			public ushort GardenBackground { get => this.gardenBackground.Value; set => this.gardenBackground.Value = value; }
		}
	}
}
