namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
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

			public ushort Present { get => this.present.Value; set => this.present.Value = value; }

			public ushort ShowLevel { get => this.showLevel.Value; set => this.showLevel.Value = value; }

			public ushort Picture { get => this.picture.Value; set => this.picture.Value = value; }

			public ushort SavedShowLevel { get => this.savedShowLevel.Value; set => this.savedShowLevel.Value = value; }

			public ushort Image { get => this.image.Value; set => this.image.Value = value; }

			public ushort HotHoriz { get => this.hotHoriz.Value; set => this.hotHoriz.Value = value; }

			public ushort HotVert { get => this.hotVert.Value; set => this.hotVert.Value = value; }

			public ushort Icon { get => this.icon.Value; set => this.icon.Value = value; }
		}
	}
}
