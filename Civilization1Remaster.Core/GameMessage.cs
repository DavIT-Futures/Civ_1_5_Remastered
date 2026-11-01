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

		public static GameMessage Start(GameEngine game, ushort textAddress)
		{
			game.Words.Set(textAddress);
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

		public GameMessage Text(ushort textAddress)
		{
			this.game.Words.Append(textAddress);
			return this;
		}

		public GameMessage Number(int amount)
		{
			this.game.Words.Append(this.game.Runtime.IntegerToText(amount, 10));
			return this;
		}

		public GameMessage City(short townIndex)
		{
			this.game.Conquest.ObtainCityLabel(townIndex);
			return this;
		}

		public GameMessage Style(ushort style)
		{
			this.game.Globals.Screen.DialogStyle = style;
			return this;
		}

		public ushort Dialog(int horizontalLocation, int verticalLocation)
		{
			return this.game.TurnHooks.ShowDialog(Buffer, horizontalLocation, verticalLocation);
		}
	}
}
