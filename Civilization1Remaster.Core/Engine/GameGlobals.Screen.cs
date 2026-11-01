namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		public sealed class ScreenVariables : VariableGroup
		{
			private readonly GlobalWord box = new GlobalWord(0x00aa);
			private readonly GlobalWord backScreenBox = new GlobalWord(0x19d4);
			private readonly GlobalWord pictureScreenBox = new GlobalWord(0x19e8);
			private readonly GlobalWord dialogStyle = new GlobalWord(0x2f9e);
			private readonly GlobalWord vgaMode = new GlobalWord(0xd762);
			private readonly GlobalWord reserveFailed = new GlobalWord(0x6dfc);
			private readonly GlobalWord repaintRequest = new GlobalWord(0x1ae0);
			private readonly GlobalWord egaMode = new GlobalWord(0xb20e);
			private readonly GlobalWord spriteScreenBox = new GlobalWord(0x19fc);
			private readonly GlobalWord lowMemoryVga = new GlobalWord(0x1a3e);
			private readonly GlobalWord repaintingMainScreen = new GlobalWord(0x1c28);
			private readonly GlobalWord boxMotif = new GlobalWord(0x2f98);
			private readonly GlobalWord hueCyclingOn = new GlobalWord(0x2fec);
			private readonly GlobalWord dialogNeedsRedraw = new GlobalWord(0xdb38);

			public ushort Box => this.box.Value;

			public ushort BackScreenBox => this.backScreenBox.Value;

			public ushort PictureScreenBox => this.pictureScreenBox.Value;

			public ushort DialogStyle { get => this.dialogStyle.Value; set => this.dialogStyle.Value = value; }

			public ushort VgaMode { get => this.vgaMode.Value; set => this.vgaMode.Value = value; }

			public ushort ReserveFailed { get => this.reserveFailed.Value; set => this.reserveFailed.Value = value; }

			public ushort RepaintRequest { get => this.repaintRequest.Value; set => this.repaintRequest.Value = value; }

			public ushort EgaMode { get => this.egaMode.Value; set => this.egaMode.Value = value; }

			public ushort SpriteScreenBox { get => this.spriteScreenBox.Value; set => this.spriteScreenBox.Value = value; }

			public ushort LowMemoryVga { get => this.lowMemoryVga.Value; set => this.lowMemoryVga.Value = value; }

			public ushort RepaintingMainScreen { get => this.repaintingMainScreen.Value; set => this.repaintingMainScreen.Value = value; }

			public ushort BoxMotif { get => this.boxMotif.Value; set => this.boxMotif.Value = value; }

			public ushort HueCyclingOn { get => this.hueCyclingOn.Value; set => this.hueCyclingOn.Value = value; }

			public ushort DialogNeedsRedraw { get => this.dialogNeedsRedraw.Value; set => this.dialogNeedsRedraw.Value = value; }
		}
	}
}
