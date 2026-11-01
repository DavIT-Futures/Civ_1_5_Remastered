namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		public sealed class MenuVariables : VariableGroup
		{
			private readonly GlobalWord disabledLines = new GlobalWord(0xb276);
			private readonly GlobalWord defaultLine = new GlobalWord(0x2f9a);
			private readonly GlobalWord helpTopic = new GlobalWord(0x2f9c);
			private readonly GlobalWord mainMenuChoice = new GlobalWord(0x6b32);
			private readonly GlobalWord result = new GlobalWord(0x654a);
			private readonly GlobalWord lineHeight = new GlobalWord(0x2fa4);
			private readonly GlobalWord rightEdge = new GlobalWord(0xde0e);
			private readonly GlobalWord lineCount = new GlobalWord(0x658e);
			private readonly GlobalWord width = new GlobalWord(0xd4c8);
			private readonly GlobalWord firstChoice = new GlobalWord(0xb1ec);
			private readonly GlobalWord choiceCount = new GlobalWord(0xd208);
			private readonly GlobalWord measuring = new GlobalWord(0xd2e2);
			private readonly GlobalWord flashHue = new GlobalWord(0x654c);
			private readonly GlobalWord options = new GlobalWord(0xd7f2);
			private readonly GlobalWord anyKey = new GlobalWord(0xd206);
			private readonly GlobalWord footer = new GlobalWord(0x2fa0);
			private readonly GlobalWord mouseUsed = new GlobalWord(0x2fa2);
			private readonly GlobalWord moreBox = new GlobalWord(0x8066);
			private readonly GlobalWord lineOffsets = new GlobalWord(0x654e);

			public ushort DisabledLines { get => this.disabledLines.Value; set => this.disabledLines.Value = value; }

			public byte DisabledLinesLow { set => this.disabledLines.Write(0, value); }

			public ushort DefaultLine { get => this.defaultLine.Value; set => this.defaultLine.Value = value; }

			public ushort HelpTopic { get => this.helpTopic.Value; set => this.helpTopic.Value = value; }

			public ushort MainMenuChoice { get => this.mainMenuChoice.Value; set => this.mainMenuChoice.Value = value; }

			public ushort Result { get => this.result.Value; set => this.result.Value = value; }

			public ushort LineHeight { get => this.lineHeight.Value; set => this.lineHeight.Value = value; }

			public ushort RightEdge { get => this.rightEdge.Value; set => this.rightEdge.Value = value; }

			public ushort LineCount { get => this.lineCount.Value; set => this.lineCount.Value = value; }

			public ushort Width { get => this.width.Value; set => this.width.Value = value; }

			public ushort FirstChoice { get => this.firstChoice.Value; set => this.firstChoice.Value = value; }

			public ushort ChoiceCount { get => this.choiceCount.Value; set => this.choiceCount.Value = value; }

			public ushort Measuring { get => this.measuring.Value; set => this.measuring.Value = value; }

			public ushort FlashHue { get => this.flashHue.Value; set => this.flashHue.Value = value; }

			public ushort Options { get => this.options.Value; set => this.options.Value = value; }

			public ushort AnyKey { get => this.anyKey.Value; set => this.anyKey.Value = value; }

			public ushort Footer { get => this.footer.Value; set => this.footer.Value = value; }

			public ushort MouseUsed { get => this.mouseUsed.Value; set => this.mouseUsed.Value = value; }

			public ushort MoreBox { get => this.moreBox.Value; set => this.moreBox.Value = value; }

			public ushort LineOffsets { get => this.lineOffsets.Value; set => this.lineOffsets.Value = value; }
		}
	}
}
