
namespace Civilization1Remaster.Core
{
	/// <summary>A part of the game (a segment of the original CIV.EXE): the game it belongs to and its processor</summary>
	public abstract class GameComponent
	{
		/// <summary>The pictures of the citizens (0x6e96, a word each): 0 - 1 happy, 2 - 3 content, 4 - 5 unhappy, then the others</summary>
		protected const ushort CitizenPicturesAddress = 0x6e96;

		protected GameEngine Owner { get; }
		protected CPU Processor { get; }

		private UnitMemory? unitTable;

		protected GameComponent(GameEngine owner)
		{
			this.Owner = owner;
			this.Processor = owner.Processor;
		}

		/// <summary>The records of the units (B6.2: in C#, UnitMemory)</summary>
		protected UnitMemory Troops => this.unitTable ??= new UnitMemory(this.Processor);

		/// <summary>The city in its turn and the city screen (the global variables of GameGlobals)</summary>
		protected GameGlobals.CityTurnVariables CityTurn => this.Owner.Globals.CityTurn;

		/// <summary>The menus and the menu bar (the global variables of GameGlobals)</summary>
		protected GameGlobals.MenuVariables Menu => this.Owner.Globals.Menu;

		/// <summary>The audiences with the kings and the advisors (the global variables of GameGlobals)</summary>
		protected GameGlobals.KingVariables King => this.Owner.Globals.King;

		/// <summary>The palace screen (the global variables of GameGlobals)</summary>
		protected GameGlobals.PalaceVariables Palace => this.Owner.Globals.Palace;

		/// <summary>The story of the intro (the global variables of GameGlobals)</summary>
		protected GameGlobals.StoryVariables Story => this.Owner.Globals.Story;

		/// <summary>The choices of the new world (the global variables of GameGlobals)</summary>
		protected GameGlobals.NewWorldVariables NewWorld => this.Owner.Globals.NewWorld;

		/// <summary>The spaceship (the global variables of GameGlobals)</summary>
		protected GameGlobals.SpaceshipVariables Spaceship => this.Owner.Globals.Spaceship;

		/// <summary>The replay (the global variables of GameGlobals)</summary>
		protected GameGlobals.ReplayVariables Replay => this.Owner.Globals.Replay;

		/// <summary>The mouse pointer (the global variables of GameGlobals)</summary>
		protected GameGlobals.MouseVariables Mouse => this.Owner.Globals.Mouse;

		/// <summary>The counts of the units in a stack (the global variables of GameGlobals)</summary>
		protected GameGlobals.StackVariables Stack => this.Owner.Globals.Stack;

		/// <summary>The saved games and the disks (the global variables of GameGlobals)</summary>
		protected GameGlobals.FileVariables Files => this.Owner.Globals.Files;

		/// <summary>The map view (the global variables of GameGlobals)</summary>
		protected GameGlobals.MapViewVariables MapView => this.Owner.Globals.MapView;

		/// <summary>The screens, their boxes and the video mode (the global variables of GameGlobals)</summary>
		protected GameGlobals.ScreenVariables Screen => this.Owner.Globals.Screen;

		/// <summary>The text cursor and the code texts of the text blocks (the global variables of GameGlobals)</summary>
		protected GameGlobals.WritingVariables Writing => this.Owner.Globals.Writing;

		/// <summary>The score and the Hall of Fame (the global variables of GameGlobals)</summary>
		protected GameGlobals.ScoringVariables Scoring => this.Owner.Globals.Scoring;

		/// <summary>The searches for the nearest city or unit and the routes (the global variables of GameGlobals)</summary>
		protected GameGlobals.SearchVariables Search => this.Owner.Globals.Search;

		/// <summary>The pictures loaded for the icons (the global variables of GameGlobals)</summary>
		protected GameGlobals.IconVariables Icons => this.Owner.Globals.Icons;

		/// <summary>The game and the turn (the global variables of GameGlobals)</summary>
		protected GameGlobals.TurnVariables Turn => this.Owner.Globals.Turn;

		/// <summary>The tables of CIV.EXE (names, records, colours, directions, sprites)</summary>
		protected GameTables Tables => this.Owner.Tables;

		/// <summary>The fields of the box at ScreenBox (B6.5): display, rectangle, flags, hues, typeface</summary>
		protected CivRectangle DrawingBox => new CivRectangle(this.Processor, CPU.AsLinearAddress(this.Processor.SegData.Value16, Screen.Box));

		/// <summary>
		/// a * b as the original multiplies (IMUL): DX:AX the product, AX (its low word) given back. The registers stay set
		/// because the code after it may still read them.
		/// </summary>
		protected ushort MultiplyWords(ushort multiplicand, ushort multiplier)
		{
			this.Processor.RegAcc.Value16 = multiplicand;
			this.Processor.MultiplySignedWord(this.Processor.RegAcc, this.Processor.RegData, multiplier);
			return this.Processor.RegAcc.Value16;
		}

		/// <summary>
		/// a / b as the original divides (CWD, IDIV): AX the quotient (given back), DX the remainder; a division by zero or
		/// an overflow throws as the processor does
		/// </summary>
		protected ushort DivideWords(ushort dividend, ushort divisor)
		{
			this.Processor.RegAcc.Value16 = dividend;
			this.Processor.SignExtendWord(this.Processor.RegAcc, this.Processor.RegData);
			this.Processor.DivideSignedWord(this.Processor.RegAcc, this.Processor.RegData, divisor);
			return this.Processor.RegAcc.Value16;
		}

		/// <summary>A message in the buffer TextBuffer.Address that starts with the text</summary>
		protected GameMessage Message(string text) => GameMessage.Start(this.Owner, text);

		/// <summary>A message in the buffer TextBuffer.Address that starts with the text at the address</summary>
		protected GameMessage Message(ushort textAddr) => GameMessage.Start(this.Owner, textAddr);

		/// <summary>A message in the buffer TextBuffer.Address that starts empty</summary>
		protected GameMessage EmptyMessage() => GameMessage.Empty(this.Owner);

		/// <summary>The picture of the citizen (see CitizenPicturesAddress)</summary>
		protected ushort CitizenPicture(int kind) => this.Processor.PullGlobalWord((ushort)((kind << 1) + CitizenPicturesAddress));

	}
}
