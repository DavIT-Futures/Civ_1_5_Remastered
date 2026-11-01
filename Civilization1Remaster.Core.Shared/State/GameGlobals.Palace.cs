namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
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

			public ushort PartWidth { get => this.partWidth.Value; set => this.partWidth.Value = value; }

			public ushort FirstPicture { get => this.firstPicture.Value; set => this.firstPicture.Value = value; }

			public ushort LeftBushes { get => this.leftBushes.Value; set => this.leftBushes.Value = value; }

			public ushort RightBushes { get => this.rightBushes.Value; set => this.rightBushes.Value = value; }

			public ushort WingShift { get => this.wingShift.Value; set => this.wingShift.Value = value; }

			public ushort Layout { get => this.layout.Value; set => this.layout.Value = value; }

			public ushort LeftWingWidth { get => this.leftWingWidth.Value; set => this.leftWingWidth.Value = value; }

			public ushort RightWingWidth { get => this.rightWingWidth.Value; set => this.rightWingWidth.Value = value; }

			public ushort StyleShift { get => this.styleShift.Value; set => this.styleShift.Value = value; }

			public ushort GardenBackground { get => this.gardenBackground.Value; set => this.gardenBackground.Value = value; }
		}
	}
}
