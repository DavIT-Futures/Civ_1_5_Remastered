namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The menus and the menu bar (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0xb276: the lines of the menu that cannot be chosen (a bit per line)</summary>
			public ushort DisabledLines { get => this.disabledLines.Value; set => this.disabledLines.Value = value; }

			/// <summary>0xb276: its low byte alone (the high byte stays)</summary>
			public byte DisabledLinesLow { set => this.disabledLines.Write(0, value); }

			/// <summary>0x2f9a: the default line of the next menu</summary>
			public ushort DefaultLine { get => this.defaultLine.Value; set => this.defaultLine.Value = value; }

			/// <summary>0x2f9c: the help topic of the menu (while set, the menu comes back even after Escape)</summary>
			public ushort HelpTopic { get => this.helpTopic.Value; set => this.helpTopic.Value = value; }

			/// <summary>0x6b32: the choice of the main menu (0 - 3, -1 not chosen)</summary>
			public ushort MainMenuChoice { get => this.mainMenuChoice.Value; set => this.mainMenuChoice.Value = value; }

			/// <summary>0x654a: the result of the menu of the menu bar</summary>
			public ushort Result { get => this.result.Value; set => this.result.Value = value; }

			/// <summary>0x2fa4: the height of a line of the menu</summary>
			public ushort LineHeight { get => this.lineHeight.Value; set => this.lineHeight.Value = value; }

			/// <summary>0xde0e: the right edge of the menu</summary>
			public ushort RightEdge { get => this.rightEdge.Value; set => this.rightEdge.Value = value; }

			/// <summary>0x658e: the lines of the menu</summary>
			public ushort LineCount { get => this.lineCount.Value; set => this.lineCount.Value = value; }

			/// <summary>0xd4c8: the width of the widest line of the menu</summary>
			public ushort Width { get => this.width.Value; set => this.width.Value = value; }

			/// <summary>0xb1ec: the line of the first choice of the menu (0xffff none)</summary>
			public ushort FirstChoice { get => this.firstChoice.Value; set => this.firstChoice.Value = value; }

			/// <summary>0xd208: the choices of the menu</summary>
			public ushort ChoiceCount { get => this.choiceCount.Value; set => this.choiceCount.Value = value; }

			/// <summary>0xd2e2: the first pass of the menu, which measures it</summary>
			public ushort Measuring { get => this.measuring.Value; set => this.measuring.Value = value; }

			/// <summary>0x654c: the colour of the flashing choice of the menu</summary>
			public ushort FlashHue { get => this.flashHue.Value; set => this.flashHue.Value = value; }

			/// <summary>0xd7f2: the bits of the options the menu shows as chosen</summary>
			public ushort Options { get => this.options.Value; set => this.options.Value = value; }

			/// <summary>0xd206: the menu has no choice, any key closes it ("Press key to continue")</summary>
			public ushort AnyKey { get => this.anyKey.Value; set => this.anyKey.Value = value; }

			/// <summary>0x2fa0: the menu box is 2 pixels taller with the text 0x2fbd at its bottom right</summary>
			public ushort Footer { get => this.footer.Value; set => this.footer.Value = value; }

			/// <summary>0x2fa2: the mouse was used in the menu (1)</summary>
			public ushort MouseUsed { get => this.mouseUsed.Value; set => this.mouseUsed.Value = value; }

			/// <summary>0x8066: whether the 'More' box on the menu is displayed</summary>
			public ushort MoreBox { get => this.moreBox.Value; set => this.moreBox.Value = value; }

			/// <summary>0x654e: the base of the table of line offsets in a menu</summary>
			public ushort LineOffsets { get => this.lineOffsets.Value; set => this.lineOffsets.Value = value; }
		}
	}
}
