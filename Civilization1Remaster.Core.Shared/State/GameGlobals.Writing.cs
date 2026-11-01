namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class WritingVariables : VariableGroup
		{
			private readonly GlobalWord cursor = new GlobalWord(0x68cc);
			private readonly GlobalWord firstCodeText = new GlobalWord(0x30b8);
			private readonly GlobalWord secondCodeText = new GlobalWord(0x30ba);
			private readonly GlobalWord thirdCodeText = new GlobalWord(0x30bc);
			private readonly GlobalWord fourthCodeText = new GlobalWord(0x30be);
			private readonly GlobalWord fifthCodeText = new GlobalWord(0x30c0);
			private readonly GlobalWord cursorColumn = new GlobalWord(0x68d0);
			private readonly GlobalWord cursorWidth = new GlobalWord(0x68ce);
			private readonly GlobalWord doubleWidthText = new GlobalWord(0x6b8c);

			public ushort Cursor { get => this.cursor.Value; set => this.cursor.Value = value; }

			public ushort FirstCodeText { get => this.firstCodeText.Value; set => this.firstCodeText.Value = value; }

			public ushort SecondCodeText { get => this.secondCodeText.Value; set => this.secondCodeText.Value = value; }

			public ushort ThirdCodeText { get => this.thirdCodeText.Value; set => this.thirdCodeText.Value = value; }

			public ushort FourthCodeText { get => this.fourthCodeText.Value; set => this.fourthCodeText.Value = value; }

			public ushort FifthCodeText { get => this.fifthCodeText.Value; set => this.fifthCodeText.Value = value; }

			public ushort CursorColumn { get => this.cursorColumn.Value; set => this.cursorColumn.Value = value; }

			public ushort CursorWidth { get => this.cursorWidth.Value; set => this.cursorWidth.Value = value; }

			public ushort DoubleWidthText { get => this.doubleWidthText.Value; set => this.doubleWidthText.Value = value; }
		}
	}
}
