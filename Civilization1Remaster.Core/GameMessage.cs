namespace Civilization1Remaster.Core
{
	public readonly struct GameMessage
	{
		public const ushort Buffer = TextBuffer.Address;

		private readonly GameEngine game;

		private GameMessage(GameEngine game)
		{
			this.game = game;
		}

		public static GameMessage Start(GameEngine game, string text)
		{
			game.Words.Set(text);
			return new GameMessage(game);
		}

		public static GameMessage Start(GameEngine game, ushort textAddr)
		{
			game.Words.Set(textAddr);
			return new GameMessage(game);
		}

		public static GameMessage Empty(GameEngine game)
		{
			game.Processor.EmitGlobalByte(Buffer, 0);
			return new GameMessage(game);
		}

		public GameMessage Text(string text)
		{
			this.game.Words.Append(text);
			return this;
		}

		public GameMessage Text(ushort textAddr)
		{
			this.game.Words.Append(textAddr);
			return this;
		}

		public GameMessage Number(int amount)
		{
			this.game.Words.Append(this.game.Libc.IntegerToText(amount, 10));
			return this;
		}

		public GameMessage City(short townIdx)
		{
			this.game.Conquest.ObtainCityLabel(townIdx);
			return this;
		}

		public GameMessage Style(ushort style)
		{
			this.game.Globals.Screen.DialogStyle = style;
			return this;
		}

		public ushort Dialog(int horizLoc, int vertLoc)
		{
			return this.game.TurnHooks.ShowDialog(Buffer, horizLoc, vertLoc);
		}
	}
}
