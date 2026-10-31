namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The mouse pointer (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class MouseVariables : VariableGroup
		{
			private readonly GlobalWord present = new GlobalWord(0x1a3c);
			private readonly GlobalWord showLevel = new GlobalWord(0xdeea);
			private readonly GlobalWord picture = new GlobalWord(0xd4dc);
			private readonly GlobalWord savedShowLevel = new GlobalWord(0x652e);
			private readonly GlobalWord image = new GlobalWord(0x5876);
			private readonly GlobalWord hotHoriz = new GlobalWord(0x5878);
			private readonly GlobalWord hotVert = new GlobalWord(0x587a);
			private readonly GlobalWord icon = new GlobalWord(0x6e92);

			/// <summary>0x1a3c: a mouse is present</summary>
			public ushort Present { get => this.present.Value; set => this.present.Value = value; }

			/// <summary>0xdeea: the show level of the mouse pointer (drawn from 1)</summary>
			public ushort ShowLevel { get => this.showLevel.Value; set => this.showLevel.Value = value; }

			/// <summary>0xd4dc: the picture of the mouse pointer</summary>
			public ushort Picture { get => this.picture.Value; set => this.picture.Value = value; }

			/// <summary>0x652e: the show level of the mouse pointer kept</summary>
			public ushort SavedShowLevel { get => this.savedShowLevel.Value; set => this.savedShowLevel.Value = value; }

			/// <summary>0x5876: the picture of the mouse pointer drawn</summary>
			public ushort Image { get => this.image.Value; set => this.image.Value = value; }

			/// <summary>0x5878: the x of the hot spot of the mouse pointer</summary>
			public ushort HotHoriz { get => this.hotHoriz.Value; set => this.hotHoriz.Value = value; }

			/// <summary>0x587a: the y of the hot spot of the mouse pointer</summary>
			public ushort HotVert { get => this.hotVert.Value; set => this.hotVert.Value = value; }

			/// <summary>0x6e92: the picture of the mouse pointer (icon 0x277)</summary>
			public ushort Icon { get => this.icon.Value; set => this.icon.Value = value; }
		}
	}
}
