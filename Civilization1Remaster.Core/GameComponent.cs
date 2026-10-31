
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

		/// <summary>The fields of the box at ScreenBox (B6.5): display, rectangle, flags, hues, typeface</summary>
		protected CivRectangle DrawingBox => new CivRectangle(this.Processor, CPU.AsLinearAddress(this.Processor.SegData.Value16, Screen.Box));

		/// <summary>The address of the name of the unit kind through the offset 0x22 * kind computed before</summary>
		protected static ushort UnitTextAt(int offset) => (ushort)(offset + UnitKindTable.Origin);

		/// <summary>The address of the name of the building record: 0xb9a + 0x1e * record</summary>
		protected static ushort BuildingRecordText(int record) => (ushort)(BuildingTable.Stride * record + BuildingTable.Origin);

		/// <summary>The address of the name of the building record through the offset 0x1e * record computed before</summary>
		protected static ushort BuildingRecordTextAt(int offset) => (ushort)(offset + BuildingTable.Origin);

		protected static ushort TerrainText(int terrain) => (ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin);

		/// <summary>The address of the name of the terrain type through the offset 0x13 * terrain computed before</summary>
		protected static ushort TerrainTextAt(int offset) => (ushort)(offset + TerrainTable.Origin);

		/// <summary>The address of a text field (LeaderTable) of the leader: 0x14e2 + 0x3a * leader + field</summary>
		protected static ushort LeaderText(int leader, int field) => (ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field);

		/// <summary>The address of a text field of the leader through the offset 0x3a * leader computed before</summary>
		protected static ushort LeaderTextAt(int offset, int field) => (ushort)(offset + LeaderTable.Origin + field);

		/// <summary>The address of the name of the technology (records of 0x16 bytes at 0x4da)</summary>
		protected static ushort TechnologyText(int index) => (ushort)(0x16 * index + 0x4da);

		/// <summary>The address of the name of the building (records of 0x1e bytes from 0xb9a, the name at 0xbb8 + 0x1e * building)</summary>
		protected static ushort BuildingText(int index) => (ushort)(0x1e * index + 0xbb8);

		/// <summary>The address of the name of the wonder (records of 0x1e bytes, the name at 0xe6a + 0x1e * wonder)</summary>
		protected static ushort WonderText(int index) => (ushort)(0x1e * index + 0xe6a);

		/// <summary>The address of the name of the unit kind (records of 0x22 bytes at 0x112a, UnitKindTable)</summary>
		protected static ushort UnitText(int index) => (ushort)(0x22 * index + 0x112a);

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

		/// <summary>A word field (UnitKindTable) of the unit kind: 0x112a + 0x22 * kind + field</summary>
		protected ushort UnitKindWord(int kind, int field) => this.Owner.Tables.Word((ushort)(UnitKindTable.Stride * kind + UnitKindTable.Origin + field));

		/// <summary>A word field of the unit kind through the offset 0x22 * kind computed before</summary>
		protected ushort UnitKindWordAt(int offset, int field) => this.Owner.Tables.Word((ushort)(offset + UnitKindTable.Origin + field));

		/// <summary>A word field (BuildingTable) of the building record: 0xb9a + 0x1e * record + field</summary>
		protected ushort BuildingWord(int record, int field) => this.Owner.Tables.Word((ushort)(BuildingTable.Stride * record + BuildingTable.Origin + field));

		/// <summary>A word field of the building record through the offset 0x1e * record computed before</summary>
		protected ushort BuildingWordAt(int offset, int field) => this.Owner.Tables.Word((ushort)(offset + BuildingTable.Origin + field));

		/// <summary>A byte field (TerrainTable) of the terrain type: 0x282 + 0x13 * terrain + field</summary>
		protected byte TerrainField(int terrain, int field) => this.Owner.Tables.Byte((ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin + field));

		/// <summary>A byte field of the terrain type through the offset 0x13 * terrain computed before</summary>
		protected byte TerrainFieldAt(int offset, int field) => this.Owner.Tables.Byte((ushort)(offset + TerrainTable.Origin + field));

		/// <summary>The address of the name of the terrain type: 0x282 + 0x13 * terrain</summary>
		protected ushort TerrainImprovementWord(int terrain, int field) =>
			this.Owner.Tables.Word((ushort)(TerrainImprovementTable.Stride * terrain + TerrainImprovementTable.Origin + field));

		/// <summary>A word field (LeaderTable) of the leader: 0x14e2 + 0x3a * leader + field</summary>
		protected ushort LeaderWord(int leader, int field) => this.Owner.Tables.Word((ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field));

		/// <summary>A word field of the leader through the offset 0x3a * leader computed before</summary>
		protected ushort LeaderWordAt(int offset, int field) => this.Owner.Tables.Word((ushort)(offset + LeaderTable.Origin + field));

		/// <summary>CivilizationName through the offset 2 * index computed before</summary>
		protected ushort CivilizationNameAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x1982));

		/// <summary>CivilizationPlural through the offset 2 * index computed before</summary>
		protected ushort CivilizationPluralAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x1992));

		/// <summary>LeaderName through the offset 2 * index computed before</summary>
		protected ushort LeaderNameAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x19a2));

		/// <summary>GovernmentTitle through the offset 2 * index computed before</summary>
		protected ushort GovernmentTitleAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x19b2));

		/// <summary>PlayerColour through the offset 2 * index computed before</summary>
		protected ushort PlayerColourAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x1946));

		/// <summary>PlayerSecondaryColour through the offset 2 * index computed before</summary>
		protected ushort PlayerSecondaryColourAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x1956));

		/// <summary>DirectionHoriz through the offset 2 * index computed before</summary>
		protected ushort DirectionHorizAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x1882));

		/// <summary>DirectionVert through the offset 2 * index computed before</summary>
		protected ushort DirectionVertAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0x18e4));

		/// <summary>SpritePicture through the offset 2 * index computed before</summary>
		protected ushort SpritePictureAt(int offset) => this.Owner.Tables.Word((ushort)(offset + 0xd4ce));

		/// <summary>0x10fe + 2 * wonder: the technology that makes the wonder obsolete (WonderObsoleteTable)</summary>
		protected ushort WonderObsoleteTechnology(int wonder) => this.Owner.Tables.Word((ushort)((wonder << 1) + WonderObsoleteTable.Origin));

		/// <summary>0x1982 + 2 * index: the text of the name of the civilization of the player ("Babylonian")</summary>
		protected ushort CivilizationName(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x1982));

		/// <summary>0x1992 + 2 * index: the text of the plural name of the civilization of the player ("Babylonians")</summary>
		protected ushort CivilizationPlural(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x1992));

		/// <summary>0x19a2 + 2 * index: the text of the name of the leader of the player</summary>
		protected ushort LeaderName(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x19a2));

		/// <summary>0x1966 + 2 * index: the text of the name of the government</summary>
		protected ushort GovernmentName(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x1966));

		/// <summary>0x19b2 + 2 * index: the text of the title of the leader under the government</summary>
		protected ushort GovernmentTitle(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x19b2));

		/// <summary>0x1946 + 2 * index: the colour of the player</summary>
		protected ushort PlayerColour(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x1946));

		/// <summary>The picture of the citizen (see CitizenPicturesAddress)</summary>
		protected ushort CitizenPicture(int kind) => this.Processor.PullGlobalWord((ushort)((kind << 1) + CitizenPicturesAddress));

		/// <summary>0x1882 + 2 * index: the x step of the direction (0 the tile itself, 1 - 8 the neighbours, then the second ring)</summary>
		protected ushort DirectionHoriz(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x1882));

		/// <summary>0x18e4 + 2 * index: the y step of the direction</summary>
		protected ushort DirectionVert(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0x18e4));

		/// <summary>0xd4ce + 2 * index: the picture of the sprite (0x40 + 32 * player + unit kind: the units of the players)</summary>
		protected ushort SpritePicture(int index) => this.Owner.Tables.Word((ushort)((index << 1) + 0xd4ce));
	}
}
