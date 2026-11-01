namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		public sealed class WritingVariables : VariableGroup
		{
			private readonly GlobalWord cursor = new GlobalWord(0x68cc);
			private readonly GlobalWord codeText1 = new GlobalWord(0x30b8);
			private readonly GlobalWord codeText2 = new GlobalWord(0x30ba);
			private readonly GlobalWord codeText3 = new GlobalWord(0x30bc);
			private readonly GlobalWord codeText4 = new GlobalWord(0x30be);
			private readonly GlobalWord codeText5 = new GlobalWord(0x30c0);
			private readonly GlobalWord cursorColumn = new GlobalWord(0x68d0);
			private readonly GlobalWord cursorWidth = new GlobalWord(0x68ce);
			private readonly GlobalWord doubleWidthText = new GlobalWord(0x6b8c);

			public ushort Cursor { get => this.cursor.Value; set => this.cursor.Value = value; }

			public ushort CodeText1 { get => this.codeText1.Value; set => this.codeText1.Value = value; }

			public ushort CodeText2 { get => this.codeText2.Value; set => this.codeText2.Value = value; }

			public ushort CodeText3 { get => this.codeText3.Value; set => this.codeText3.Value = value; }

			public ushort CodeText4 { get => this.codeText4.Value; set => this.codeText4.Value = value; }

			public ushort CodeText5 { get => this.codeText5.Value; set => this.codeText5.Value = value; }

			public ushort CursorColumn { get => this.cursorColumn.Value; set => this.cursorColumn.Value = value; }

			public ushort CursorWidth { get => this.cursorWidth.Value; set => this.cursorWidth.Value = value; }

			public ushort DoubleWidthText { get => this.doubleWidthText.Value; set => this.doubleWidthText.Value = value; }
		}
	}
}
