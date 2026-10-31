namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The text cursor and the code texts of the text blocks (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0x68cc: the cursor in the text field of PullTextInput</summary>
			public ushort Cursor { get => this.cursor.Value; set => this.cursor.Value = value; }

			/// <summary>0x30b8: the address of the first code text of the text blocks</summary>
			public ushort CodeText1 { get => this.codeText1.Value; set => this.codeText1.Value = value; }

			/// <summary>0x30ba: the address of the second code text of the text blocks</summary>
			public ushort CodeText2 { get => this.codeText2.Value; set => this.codeText2.Value = value; }

			/// <summary>0x30bc: the address of the third code text of the text blocks</summary>
			public ushort CodeText3 { get => this.codeText3.Value; set => this.codeText3.Value = value; }

			/// <summary>0x30be: the address of the fourth code text of the text blocks</summary>
			public ushort CodeText4 { get => this.codeText4.Value; set => this.codeText4.Value = value; }

			/// <summary>0x30c0: the address of the fifth code text of the text blocks</summary>
			public ushort CodeText5 { get => this.codeText5.Value; set => this.codeText5.Value = value; }

			/// <summary>0x68d0: the column of the text cursor</summary>
			public ushort CursorColumn { get => this.cursorColumn.Value; set => this.cursorColumn.Value = value; }

			/// <summary>0x68ce: the width of the character at the text cursor</summary>
			public ushort CursorWidth { get => this.cursorWidth.Value; set => this.cursorWidth.Value = value; }

			/// <summary>0x6b8c: whether text coordinate is doubled (e.g. 640-pixel screen modes)</summary>
			public ushort DoubleWidthText { get => this.doubleWidthText.Value; set => this.doubleWidthText.Value = value; }
		}
	}
}
