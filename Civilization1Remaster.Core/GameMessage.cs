namespace Civilization1Remaster
{
	/// <summary>
	/// A text built in the message buffer at 0xba06 and shown in a dialog: the same calls as the original makes (CopyText,
	/// AppendText, ObtainCityLabel, the dialog style, DisplayDialog), in the same order, only written as one chain
	/// </summary>
	public readonly struct GameMessage
	{
		/// <summary>The message buffer of the original</summary>
		public const ushort Buffer = TextBuffer.Address;

		private readonly GameEngine game;

		private GameMessage(GameEngine game)
		{
			this.game = game;
		}

		/// <summary>The buffer starts with the text</summary>
		public static GameMessage Start(GameEngine game, string text)
		{
			game.Words.Set(text);
			return new GameMessage(game);
		}

		/// <summary>The buffer starts with the text at the address</summary>
		public static GameMessage Start(GameEngine game, ushort textAddr)
		{
			game.Words.Set(textAddr);
			return new GameMessage(game);
		}

		/// <summary>The buffer starts empty</summary>
		public static GameMessage Empty(GameEngine game)
		{
			game.Processor.EmitGlobalByte(Buffer, 0);
			return new GameMessage(game);
		}

		/// <summary>The text added</summary>
		public GameMessage Text(string text)
		{
			this.game.Words.Append(text);
			return this;
		}

		/// <summary>The text at the address added</summary>
		public GameMessage Text(ushort textAddr)
		{
			this.game.Words.Append(textAddr);
			return this;
		}

		/// <summary>The number added (in decimal)</summary>
		public GameMessage Number(int amount)
		{
			this.game.Words.Append(this.game.Libc.IntegerToText(amount, 10));
			return this;
		}

		/// <summary>The name of the city added</summary>
		public GameMessage City(short townIdx)
		{
			this.game.Conquest.ObtainCityLabel(townIdx);
			return this;
		}

		/// <summary>The style of the dialog that will show it (the word 0x2f9e)</summary>
		public GameMessage Style(ushort style)
		{
			this.game.Globals.DialogStyle.Value = style;
			return this;
		}

		/// <summary>The message shown in a dialog at the place; the line chosen</summary>
		public ushort Dialog(int horizLoc, int vertLoc)
		{
			return this.game.TurnHooks.DisplayDialog(Buffer, horizLoc, vertLoc);
		}
	}
}
