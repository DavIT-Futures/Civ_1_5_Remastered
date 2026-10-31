namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The screens, their boxes and the video mode (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0xaa: the address of the box (a screen and its rectangle) the drawing goes to</summary>
			public ushort Box => this.box.Value;

			/// <summary>0x19d4: the box of the back screen, which keeps what the screen showed before a dialog or the map</summary>
			public ushort BackScreenBox => this.backScreenBox.Value;

			/// <summary>0x19e8: the box of the picture screen (the world map from column 0xf0, the pieces of the borders)</summary>
			public ushort PictureScreenBox => this.pictureScreenBox.Value;

			/// <summary>0x2f9e: the style of the next dialog (its frame and the advisor portrait)</summary>
			public ushort DialogStyle { get => this.dialogStyle.Value; set => this.dialogStyle.Value = value; }

			/// <summary>0xd762: not 0 on VGA (the colours and palettes of the pictures)</summary>
			public ushort VgaMode { get => this.vgaMode.Value; set => this.vgaMode.Value = value; }

			/// <summary>0x6dfc: one of the screens could not be reserved</summary>
			public ushort ReserveFailed { get => this.reserveFailed.Value; set => this.reserveFailed.Value = value; }

			/// <summary>0x1ae0: the screen is to be repainted</summary>
			public ushort RepaintRequest { get => this.repaintRequest.Value; set => this.repaintRequest.Value = value; }

			/// <summary>0xb20e: the EGA driver is used</summary>
			public ushort EgaMode { get => this.egaMode.Value; set => this.egaMode.Value = value; }

			/// <summary>0x19fc: the address of the box of the sprites</summary>
			public ushort SpriteScreenBox { get => this.spriteScreenBox.Value; set => this.spriteScreenBox.Value = value; }

			/// <summary>0x1a3e: little memory with VGA: one sprite for every row</summary>
			public ushort LowMemoryVga { get => this.lowMemoryVga.Value; set => this.lowMemoryVga.Value = value; }

			/// <summary>0x1c28: while RepaintMainScreen draws (1): the population mark is not announced</summary>
			public ushort RepaintingMainScreen { get => this.repaintingMainScreen.Value; set => this.repaintingMainScreen.Value = value; }

			/// <summary>0x2f98: after the tribe selection (1): boxes of style 7 get the motif</summary>
			public ushort BoxMotif { get => this.boxMotif.Value; set => this.boxMotif.Value = value; }

			/// <summary>0x2fec: the colour cycles 1 - 3 of the map are running (LaunchHueCycling / HaltHueCycling)</summary>
			public ushort HueCyclingOn { get => this.hueCyclingOn.Value; set => this.hueCyclingOn.Value = value; }

			/// <summary>0xdb38: whether a dialog or menu background needs to be redrawn (1 = needs redraw)</summary>
			public ushort DialogNeedsRedraw { get => this.dialogNeedsRedraw.Value; set => this.dialogNeedsRedraw.Value = value; }
		}
	}
}
