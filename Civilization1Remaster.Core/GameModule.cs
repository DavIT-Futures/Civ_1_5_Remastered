using Civilization1Remaster.Emulation;

namespace Civilization1Remaster
{
	/// <summary>A part of the game (a segment of the original CIV.EXE): the game it belongs to and its processor</summary>
	public abstract class GameModule
	{
		protected readonly GameEngine owner;
		protected readonly CPU processor;

		protected GameModule(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// a * b as the original multiplies (IMUL): DX:AX the product, AX (its low word) given back. The registers stay set
		/// because the code after it may still read them.
		/// </summary>
		protected ushort MultiplyWords(ushort multiplicand, ushort multiplier)
		{
			this.processor.RegAcc.Value16 = multiplicand;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, multiplier);
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// a / b as the original divides (CWD, IDIV): AX the quotient (given back), DX the remainder; a division by zero or
		/// an overflow throws as the processor does
		/// </summary>
		protected ushort DivideWords(ushort dividend, ushort divisor)
		{
			this.processor.RegAcc.Value16 = dividend;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, divisor);
			return this.processor.RegAcc.Value16;
		}

		/// <summary>A message in the buffer 0xba06 that starts with the text</summary>
		protected GameMessage Message(string text) => GameMessage.Start(this.owner, text);

		/// <summary>A message in the buffer 0xba06 that starts with the text at the address</summary>
		protected GameMessage Message(ushort textAddr) => GameMessage.Start(this.owner, textAddr);

		/// <summary>A message in the buffer 0xba06 that starts empty</summary>
		protected GameMessage EmptyMessage() => GameMessage.Empty(this.owner);

		#region Global variables of the data segment, read where the original reads them

		/// <summary>0xaa: the address of the box (a screen and its rectangle) the drawing goes to</summary>
		protected ushort ScreenBox => this.processor.PullGlobalWord(0xaa);

		/// <summary>0x19d4: the box of the back screen, which keeps what the screen showed before a dialog or the map</summary>
		protected ushort BackScreenBox => this.processor.PullGlobalWord(0x19d4);

		/// <summary>0x19e8: the box of the picture screen (the world map from column 0xf0, the pieces of the borders)</summary>
		protected ushort PictureScreenBox => this.processor.PullGlobalWord(0x19e8);

		/// <summary>0x6540: the tiles around the city a worker may take (0x15)</summary>
		protected ushort CityTiles
		{
			get => this.processor.PullGlobalWord(0x6540);
			set => this.processor.EmitGlobalWord(0x6540, value);
		}

		/// <summary>0x6548: the owner of the city in its turn</summary>
		protected ushort CityOwner
		{
			get => this.processor.PullGlobalWord(0x6548);
			set => this.processor.EmitGlobalWord(0x6548, value);
		}

		/// <summary>0x6c9a: the distance to what the last search for the nearest city or unit found</summary>
		protected ushort FoundDistance
		{
			get => this.processor.PullGlobalWord(0x6c9a);
			set => this.processor.EmitGlobalWord(0x6c9a, value);
		}

		/// <summary>0x70da: the food of the city in its turn</summary>
		protected ushort CityFood
		{
			get => this.processor.PullGlobalWord(0x70da);
			set => this.processor.EmitGlobalWord(0x70da, value);
		}

		/// <summary>0x70dc: the shields of the city in its turn</summary>
		protected ushort CityShields
		{
			get => this.processor.PullGlobalWord(0x70dc);
			set => this.processor.EmitGlobalWord(0x70dc, value);
		}

		/// <summary>0x70de: the trade of the city in its turn</summary>
		protected ushort CityTrade
		{
			get => this.processor.PullGlobalWord(0x70de);
			set => this.processor.EmitGlobalWord(0x70de, value);
		}

		/// <summary>0x70e0: the luxuries of the city in its turn</summary>
		protected ushort CityLuxuries
		{
			get => this.processor.PullGlobalWord(0x70e0);
			set => this.processor.EmitGlobalWord(0x70e0, value);
		}

		/// <summary>0x70e2: the happy citizens of the city in its turn</summary>
		protected ushort CityHappy
		{
			get => this.processor.PullGlobalWord(0x70e2);
			set => this.processor.EmitGlobalWord(0x70e2, value);
		}

		/// <summary>0x70e4: the unhappy citizens of the city in its turn</summary>
		protected ushort CityUnhappy
		{
			get => this.processor.PullGlobalWord(0x70e4);
			set => this.processor.EmitGlobalWord(0x70e4, value);
		}

		/// <summary>0x70e6: the science of the city in its turn (by the rates of the owner)</summary>
		protected ushort CityScience
		{
			get => this.processor.PullGlobalWord(0x70e6);
			set => this.processor.EmitGlobalWord(0x70e6, value);
		}

		/// <summary>0x70e8: 1 when a tile around the city belongs to the human player</summary>
		protected ushort HumanTileNear
		{
			get => this.processor.PullGlobalWord(0x70e8);
			set => this.processor.EmitGlobalWord(0x70e8, value);
		}

		/// <summary>0xb882: the advances that make citizens content (none with the wonder 0x1000 in the city)</summary>
		protected ushort ContentAdvances
		{
			get => this.processor.PullGlobalWord(0xb882);
			set => this.processor.EmitGlobalWord(0xb882, value);
		}

		/// <summary>0xe8b8: the specialists of the city (bits 26 - 31 of the workers)</summary>
		protected ushort CitySpecialists
		{
			get => this.processor.PullGlobalWord(0xe8b8);
			set => this.processor.EmitGlobalWord(0xe8b8, value);
		}
		/// <summary>0x2496: the tab lit on the city screen (0 - 3)</summary>
		protected ushort CityScreenTab
		{
			get => this.processor.PullGlobalWord(0x2496);
			set => this.processor.EmitGlobalWord(0x2496, value);
		}

		/// <summary>0x2f9e: the style of the next dialog (its frame and the advisor portrait)</summary>
		protected ushort DialogStyle
		{
			get => this.processor.PullGlobalWord(0x2f9e);
			set => this.processor.EmitGlobalWord(0x2f9e, value);
		}

		/// <summary>0x6590: the column where the unit heads (see CarryOutGoto)</summary>
		protected ushort GoalHoriz
		{
			get => this.processor.PullGlobalWord(0x6590);
			set => this.processor.EmitGlobalWord(0x6590, value);
		}

		/// <summary>0x6592: the row where the unit heads (see CarryOutGoto)</summary>
		protected ushort GoalVert
		{
			get => this.processor.PullGlobalWord(0x6592);
			set => this.processor.EmitGlobalWord(0x6592, value);
		}

		/// <summary>0x67f4: the step of the king's animation (0 standing still)</summary>
		protected ushort KingStep
		{
			get => this.processor.PullGlobalWord(0x67f4);
			set => this.processor.EmitGlobalWord(0x67f4, value);
		}

		/// <summary>0x68cc: the cursor in the text field of PullTextInput</summary>
		protected ushort TextCursor
		{
			get => this.processor.PullGlobalWord(0x68cc);
			set => this.processor.EmitGlobalWord(0x68cc, value);
		}

		/// <summary>0x6b90: the player whose turn it is</summary>
		protected ushort TurnPlayer
		{
			get => this.processor.PullGlobalWord(0x6b90);
			set => this.processor.EmitGlobalWord(0x6b90, value);
		}

		/// <summary>0xb276: the lines of the menu that cannot be chosen (a bit per line)</summary>
		protected ushort DisabledLines
		{
			get => this.processor.PullGlobalWord(0xb276);
			set => this.processor.EmitGlobalWord(0xb276, value);
		}

		/// <summary>0xb276: its low byte alone (the high byte stays)</summary>
		protected byte DisabledLinesLow { set => this.processor.EmitGlobalByte(0xb276, value); }

		/// <summary>0xd2de: the technology level of the time (AD only)</summary>
		protected ushort TechnologyLevel
		{
			get => this.processor.PullGlobalWord(0xd2de);
			set => this.processor.EmitGlobalWord(0xd2de, value);
		}

		/// <summary>0xd762: not 0 on VGA (the colours and palettes of the pictures)</summary>
		protected ushort VgaMode
		{
			get => this.processor.PullGlobalWord(0xd762);
			set => this.processor.EmitGlobalWord(0xd762, value);
		}

		/// <summary>0xd76c: the king speaks (counts down)</summary>
		protected ushort KingSpeaking
		{
			get => this.processor.PullGlobalWord(0xd76c);
			set => this.processor.EmitGlobalWord(0xd76c, value);
		}

		/// <summary>0xd7f0: the player of the unit, or the owner of the tile, found last (also in 0xd20a)</summary>
		protected ushort FoundPlayer
		{
			get => this.processor.PullGlobalWord(0xd7f0);
			set => this.processor.EmitGlobalWord(0xd7f0, value);
		}

		/// <summary>0xd806: the whole map is shown</summary>
		protected ushort MapRevealed
		{
			get => this.processor.PullGlobalWord(0xd806);
			set => this.processor.EmitGlobalWord(0xd806, value);
		}

		/// <summary>0xdc48: asks the main loop to end or to load the game</summary>
		protected ushort GameEndRequest
		{
			get => this.processor.PullGlobalWord(0xdc48);
			set => this.processor.EmitGlobalWord(0xdc48, value);
		}
		/// <summary>0x1a3c: a mouse is present</summary>
		protected ushort MousePresent
		{
			get => this.processor.PullGlobalWord(0x1a3c);
			set => this.processor.EmitGlobalWord(0x1a3c, value);
		}

		/// <summary>0x2f9a: the default line of the next menu</summary>
		protected ushort DefaultLine
		{
			get => this.processor.PullGlobalWord(0x2f9a);
			set => this.processor.EmitGlobalWord(0x2f9a, value);
		}

		/// <summary>0x2f9c: the help topic of the menu (while set, the menu comes back even after Escape)</summary>
		protected ushort MenuHelpTopic
		{
			get => this.processor.PullGlobalWord(0x2f9c);
			set => this.processor.EmitGlobalWord(0x2f9c, value);
		}

		/// <summary>0x3934: the answer of the king dialog (-1 not known yet)</summary>
		protected ushort KingAnswer
		{
			get => this.processor.PullGlobalWord(0x3934);
			set => this.processor.EmitGlobalWord(0x3934, value);
		}

		/// <summary>0x6546: the attack units of the city away from it (planes always)</summary>
		protected ushort CityAttackersAway
		{
			get => this.processor.PullGlobalWord(0x6546);
			set => this.processor.EmitGlobalWord(0x6546, value);
		}

		/// <summary>0x680a: the mode of the replay</summary>
		protected ushort ReplayMode
		{
			get => this.processor.PullGlobalWord(0x680a);
			set => this.processor.EmitGlobalWord(0x680a, value);
		}

		/// <summary>0x6810: the acting player of the replay event (-1 none)</summary>
		protected ushort ReplayActor
		{
			get => this.processor.PullGlobalWord(0x6810);
			set => this.processor.EmitGlobalWord(0x6810, value);
		}

		/// <summary>0x6b32: the choice of the main menu (0 - 3, -1 not chosen)</summary>
		protected ushort MainMenuChoice
		{
			get => this.processor.PullGlobalWord(0x6b32);
			set => this.processor.EmitGlobalWord(0x6b32, value);
		}

		/// <summary>0xd2e0: the corruption of the city in its turn</summary>
		protected ushort CityCorruption
		{
			get => this.processor.PullGlobalWord(0xd2e0);
			set => this.processor.EmitGlobalWord(0xd2e0, value);
		}

		/// <summary>0xd2f6: the shields the units of the city cost in its turn</summary>
		protected ushort CityUnitUpkeep
		{
			get => this.processor.PullGlobalWord(0xd2f6);
			set => this.processor.EmitGlobalWord(0xd2f6, value);
		}

		/// <summary>0xd4ca: the order chosen from the menu bar (its key)</summary>
		protected ushort ChosenOrder
		{
			get => this.processor.PullGlobalWord(0xd4ca);
			set => this.processor.EmitGlobalWord(0xd4ca, value);
		}

		/// <summary>0xd4cc: the column of the map view (its top left tile)</summary>
		protected ushort MapViewHoriz
		{
			get => this.processor.PullGlobalWord(0xd4cc);
			set => this.processor.EmitGlobalWord(0xd4cc, value);
		}

		/// <summary>0xd75e: the row of the map view (its top left tile)</summary>
		protected ushort MapViewVert
		{
			get => this.processor.PullGlobalWord(0xd75e);
			set => this.processor.EmitGlobalWord(0xd75e, value);
		}

		/// <summary>0xdeb8: the special slots of the city in use (Unfamiliar)</summary>
		protected ushort CitySpecialSlots
		{
			get => this.processor.PullGlobalWord(0xdeb8);
			set => this.processor.EmitGlobalWord(0xdeb8, value);
		}

		/// <summary>0xdeea: the show level of the mouse pointer (drawn from 1)</summary>
		protected ushort PointerShowLevel
		{
			get => this.processor.PullGlobalWord(0xdeea);
			set => this.processor.EmitGlobalWord(0xdeea, value);
		}

		/// <summary>0xe17a: the taxes of the city in its turn</summary>
		protected ushort CityTaxes
		{
			get => this.processor.PullGlobalWord(0xe17a);
			set => this.processor.EmitGlobalWord(0xe17a, value);
		}

		/// <summary>0xe3c6: the settlers of the city in its turn</summary>
		protected ushort CitySettlers
		{
			get => this.processor.PullGlobalWord(0xe3c6);
			set => this.processor.EmitGlobalWord(0xe3c6, value);
		}
		/// <summary>0x3b62: the story text of the intro (0 not started, then telling)</summary>
		protected ushort StoryState
		{
			get => this.processor.PullGlobalWord(0x3b62);
			set => this.processor.EmitGlobalWord(0x3b62, value);
		}

		/// <summary>0x4384: the letter of the drive of the saved games</summary>
		protected ushort SaveDrive
		{
			get => this.processor.PullGlobalWord(0x4384);
			set => this.processor.EmitGlobalWord(0x4384, value);
		}

		/// <summary>0x6806: the music time of the next line of the story</summary>
		protected ushort StoryLineTime
		{
			get => this.processor.PullGlobalWord(0x6806);
			set => this.processor.EmitGlobalWord(0x6806, value);
		}

		/// <summary>0x6b64: the main screen and the map are drawn again after the city screen</summary>
		protected ushort MapRedrawRequest
		{
			get => this.processor.PullGlobalWord(0x6b64);
			set => this.processor.EmitGlobalWord(0x6b64, value);
		}

		/// <summary>0x6c98: the divisor of the pollution of the cities</summary>
		protected ushort PollutionFactor
		{
			get => this.processor.PullGlobalWord(0x6c98);
			set => this.processor.EmitGlobalWord(0x6c98, value);
		}

		/// <summary>0xb1e8: a city is handled once more with its screen (2 again)</summary>
		protected ushort CityAgain
		{
			get => this.processor.PullGlobalWord(0xb1e8);
			set => this.processor.EmitGlobalWord(0xb1e8, value);
		}

		/// <summary>0xb278: 1 when the map is drawn as the world map (a tile in 4 x 4 pixels)</summary>
		protected ushort WorldMapView => this.processor.PullGlobalWord(0xb278);

		/// <summary>0xd20a: the player of the unit found last at a tile (also in 0xd7f0)</summary>
		protected ushort UnitAtTilePlayer
		{
			get => this.processor.PullGlobalWord(0xd20a);
			set => this.processor.EmitGlobalWord(0xd20a, value);
		}
		/// <summary>0x1cf6: the player whose spaceship arrived this year (-1 none)</summary>
		protected ushort ArrivedSpaceship
		{
			get => this.processor.PullGlobalWord(0x1cf6);
			set => this.processor.EmitGlobalWord(0x1cf6, value);
		}

		/// <summary>0x653a: the unit kind TallyUnitsOfTypeInStack counts</summary>
		protected ushort StackKind
		{
			get => this.processor.PullGlobalWord(0x653a);
			set => this.processor.EmitGlobalWord(0x653a, value);
		}

		/// <summary>0x653c: the units of the kind in the stack</summary>
		protected ushort StackTally
		{
			get => this.processor.PullGlobalWord(0x653c);
			set => this.processor.EmitGlobalWord(0x653c, value);
		}

		/// <summary>0x653e: the city of the city screen</summary>
		protected ushort ShownCity
		{
			get => this.processor.PullGlobalWord(0x653e);
			set => this.processor.EmitGlobalWord(0x653e, value);
		}

		/// <summary>0x654a: the result of the menu of the menu bar</summary>
		protected ushort MenuResult
		{
			get => this.processor.PullGlobalWord(0x654a);
			set => this.processor.EmitGlobalWord(0x654a, value);
		}

		/// <summary>0x6794: the cost of the route to the unit</summary>
		protected ushort GotoCost
		{
			get => this.processor.PullGlobalWord(0x6794);
			set => this.processor.EmitGlobalWord(0x6794, value);
		}

		/// <summary>0x6b30: the distance of the city to the palace of its owner</summary>
		protected ushort PalaceDistance
		{
			get => this.processor.PullGlobalWord(0x6b30);
			set => this.processor.EmitGlobalWord(0x6b30, value);
		}

		/// <summary>0xb884: the game ends by conquest</summary>
		protected ushort ConquestEnd
		{
			get => this.processor.PullGlobalWord(0xb884);
			set => this.processor.EmitGlobalWord(0xb884, value);
		}

		/// <summary>0xd804: the audience with the emissary of the player took place this turn</summary>
		protected ushort EmissaryAudience
		{
			get => this.processor.PullGlobalWord(0xd804);
			set => this.processor.EmitGlobalWord(0xd804, value);
		}

		/// <summary>0xdcfc: the units on the map are not drawn</summary>
		protected ushort UnitsHidden
		{
			get => this.processor.PullGlobalWord(0xdcfc);
			set => this.processor.EmitGlobalWord(0xdcfc, value);
		}

		/// <summary>0xdeba: the first disk of the game that is missing</summary>
		protected ushort FirstMissingDisk
		{
			get => this.processor.PullGlobalWord(0xdeba);
			set => this.processor.EmitGlobalWord(0xdeba, value);
		}

		/// <summary>0x3936: the mood of the king in the audience, which picks his next gesture (0, 1, 2; 0xffff: no audience)</summary>
		protected ushort KingMood
		{
			get => this.processor.PullGlobalWord(0x3936);
			set => this.processor.EmitGlobalWord(0x3936, value);
		}

		/// <summary>0x30b8: the address of the first code text of the text blocks</summary>
		protected ushort CodeText1
		{
			get => this.processor.PullGlobalWord(0x30b8);
			set => this.processor.EmitGlobalWord(0x30b8, value);
		}

		/// <summary>0x30ba: the address of the second code text of the text blocks</summary>
		protected ushort CodeText2
		{
			get => this.processor.PullGlobalWord(0x30ba);
			set => this.processor.EmitGlobalWord(0x30ba, value);
		}

		/// <summary>0x30bc: the address of the third code text of the text blocks</summary>
		protected ushort CodeText3
		{
			get => this.processor.PullGlobalWord(0x30bc);
			set => this.processor.EmitGlobalWord(0x30bc, value);
		}

		/// <summary>0x30be: the address of the fourth code text of the text blocks</summary>
		protected ushort CodeText4
		{
			get => this.processor.PullGlobalWord(0x30be);
			set => this.processor.EmitGlobalWord(0x30be, value);
		}

		/// <summary>0x2fa4: the height of a line of the menu</summary>
		protected ushort MenuLineHeight
		{
			get => this.processor.PullGlobalWord(0x2fa4);
			set => this.processor.EmitGlobalWord(0x2fa4, value);
		}

		/// <summary>0xde0e: the right edge of the menu</summary>
		protected ushort MenuRightEdge
		{
			get => this.processor.PullGlobalWord(0xde0e);
			set => this.processor.EmitGlobalWord(0xde0e, value);
		}

		/// <summary>0x658e: the lines of the menu</summary>
		protected ushort MenuLineCount
		{
			get => this.processor.PullGlobalWord(0x658e);
			set => this.processor.EmitGlobalWord(0x658e, value);
		}

		/// <summary>0x4b2e: the width of a part of the palace</summary>
		protected ushort PalacePartWidth
		{
			get => this.processor.PullGlobalWord(0x4b2e);
			set => this.processor.EmitGlobalWord(0x4b2e, value);
		}

		/// <summary>0x67f8: the picture of the king in the audience (kingNN.pic)</summary>
		protected ushort KingPicture
		{
			get => this.processor.PullGlobalWord(0x67f8);
			set => this.processor.EmitGlobalWord(0x67f8, value);
		}

		/// <summary>0x6532: the total TallyUnitsByDomainInStack counts</summary>
		protected ushort StackDomainTally
		{
			get => this.processor.PullGlobalWord(0x6532);
			set => this.processor.EmitGlobalWord(0x6532, value);
		}

		/// <summary>0x1c24: the next population mark of the status bar (tens below 100, then hundreds + 9)</summary>
		protected ushort PopulationMark
		{
			get => this.processor.PullGlobalWord(0x1c24);
			set => this.processor.EmitGlobalWord(0x1c24, value);
		}

		/// <summary>0xe168: the save slot used last</summary>
		protected ushort LastSaveSlot
		{
			get => this.processor.PullGlobalWord(0xe168);
			set => this.processor.EmitGlobalWord(0xe168, value);
		}

		/// <summary>0xdf60: the game continues in the middle of the turn (2 an autosave, 1 a manual save)</summary>
		protected ushort MidTurnSave
		{
			get => this.processor.PullGlobalWord(0xdf60);
			set => this.processor.EmitGlobalWord(0xdf60, value);
		}

		/// <summary>0xdeae: the colonists of the spaceship</summary>
		protected ushort SpaceshipColonists
		{
			get => this.processor.PullGlobalWord(0xdeae);
			set => this.processor.EmitGlobalWord(0xdeae, value);
		}

		/// <summary>0xd76a: the game is played on the map of the Earth</summary>
		protected ushort EarthGame
		{
			get => this.processor.PullGlobalWord(0xd76a);
			set => this.processor.EmitGlobalWord(0xd76a, value);
		}

		/// <summary>0x7efa: the climate of the new world</summary>
		protected ushort Climate
		{
			get => this.processor.PullGlobalWord(0x7efa);
			set => this.processor.EmitGlobalWord(0x7efa, value);
		}

		/// <summary>0x67f6: the frames of the pause of the speaking king</summary>
		protected ushort KingPause
		{
			get => this.processor.PullGlobalWord(0x67f6);
			set => this.processor.EmitGlobalWord(0x67f6, value);
		}

		/// <summary>0x6812: the distance of the nearest entry of the replay city table (999 none)</summary>
		protected ushort ReplayCityDistance
		{
			get => this.processor.PullGlobalWord(0x6812);
			set => this.processor.EmitGlobalWord(0x6812, value);
		}

		/// <summary>0xb25c: the x of the last icon of the score screen</summary>
		protected ushort ScoreIconHoriz
		{
			get => this.processor.PullGlobalWord(0xb25c);
			set => this.processor.EmitGlobalWord(0xb25c, value);
		}

		/// <summary>0xb2bc: the y of the row of icons of the score screen</summary>
		protected ushort ScoreIconVert
		{
			get => this.processor.PullGlobalWord(0xb2bc);
			set => this.processor.EmitGlobalWord(0xb2bc, value);
		}

		/// <summary>0x3b64: the next picture of the story (birthN.pic)</summary>
		protected ushort BirthPicture
		{
			get => this.processor.PullGlobalWord(0x3b64);
			set => this.processor.EmitGlobalWord(0x3b64, value);
		}

		/// <summary>0xd4c8: the width of the widest line of the menu</summary>
		protected ushort MenuWidth
		{
			get => this.processor.PullGlobalWord(0xd4c8);
			set => this.processor.EmitGlobalWord(0xd4c8, value);
		}

		/// <summary>0xb1ec: the line of the first choice of the menu (0xffff none)</summary>
		protected ushort MenuFirstChoice
		{
			get => this.processor.PullGlobalWord(0xb1ec);
			set => this.processor.EmitGlobalWord(0xb1ec, value);
		}

		/// <summary>0xd208: the choices of the menu</summary>
		protected ushort MenuChoiceCount
		{
			get => this.processor.PullGlobalWord(0xd208);
			set => this.processor.EmitGlobalWord(0xd208, value);
		}

		/// <summary>0xd2e2: the first pass of the menu, which measures it</summary>
		protected ushort MenuMeasuring
		{
			get => this.processor.PullGlobalWord(0xd2e2);
			set => this.processor.EmitGlobalWord(0xd2e2, value);
		}

		/// <summary>0x654c: the colour of the flashing choice of the menu</summary>
		protected ushort MenuFlashHue
		{
			get => this.processor.PullGlobalWord(0x654c);
			set => this.processor.EmitGlobalWord(0x654c, value);
		}

		/// <summary>0xd7f2: the bits of the options the menu shows as chosen</summary>
		protected ushort MenuOptions
		{
			get => this.processor.PullGlobalWord(0xd7f2);
			set => this.processor.EmitGlobalWord(0xd7f2, value);
		}

		/// <summary>0x6828: the first picture of the palace parts (0xffff unset)</summary>
		protected ushort PalaceFirstPicture
		{
			get => this.processor.PullGlobalWord(0x6828);
			set => this.processor.EmitGlobalWord(0x6828, value);
		}

		/// <summary>0xd930: the bushes left of the palace</summary>
		protected ushort PalaceLeftBushes
		{
			get => this.processor.PullGlobalWord(0xd930);
			set => this.processor.EmitGlobalWord(0xd930, value);
		}

		/// <summary>0x3b66: the next byte of the colour cycle records of the story pictures (0x3710)</summary>
		protected ushort StoryCycleRecord
		{
			get => this.processor.PullGlobalWord(0x3b66);
			set => this.processor.EmitGlobalWord(0x3b66, value);
		}

		/// <summary>0x3b68: the colour cycles of the story picture</summary>
		protected ushort StoryCycleCount
		{
			get => this.processor.PullGlobalWord(0x3b68);
			set => this.processor.EmitGlobalWord(0x3b68, value);
		}

		/// <summary>0x70d8: the units eliminated are counted as losses</summary>
		protected ushort CountLosses
		{
			get => this.processor.PullGlobalWord(0x70d8);
			set => this.processor.EmitGlobalWord(0x70d8, value);
		}

		/// <summary>0x6530: the unit that leads the stack</summary>
		protected ushort StackLeader
		{
			get => this.processor.PullGlobalWord(0x6530);
			set => this.processor.EmitGlobalWord(0x6530, value);
		}

		/// <summary>0x7ef4: the pose the king holds (0, 1, 2)</summary>
		protected ushort KingPose
		{
			get => this.processor.PullGlobalWord(0x7ef4);
			set => this.processor.EmitGlobalWord(0x7ef4, value);
		}

		/// <summary>0x67f2: the player whose king gives the audience</summary>
		protected ushort AudiencePlayer
		{
			get => this.processor.PullGlobalWord(0x67f2);
			set => this.processor.EmitGlobalWord(0x67f2, value);
		}

		/// <summary>0x67f0: the audience shows a line over a motif instead of the throne room</summary>
		protected ushort AudienceWithoutPictures
		{
			get => this.processor.PullGlobalWord(0x67f0);
			set => this.processor.EmitGlobalWord(0x67f0, value);
		}

		/// <summary>0x680c: the handle of the replay file</summary>
		protected ushort ReplayFile
		{
			get => this.processor.PullGlobalWord(0x680c);
			set => this.processor.EmitGlobalWord(0x680c, value);
		}

		/// <summary>0x6c96: the names of the cities written on the map</summary>
		protected ushort CityLabelCount
		{
			get => this.processor.PullGlobalWord(0x6c96);
			set => this.processor.EmitGlobalWord(0x6c96, value);
		}

		/// <summary>0x2a6e: the bits of the players that attack a continent of the human player</summary>
		protected ushort HumanContinentAttackers
		{
			get => this.processor.PullGlobalWord(0x2a6e);
			set => this.processor.EmitGlobalWord(0x2a6e, value);
		}

		/// <summary>0xe17c: the disk of the game in use</summary>
		protected ushort DiskInUse
		{
			get => this.processor.PullGlobalWord(0xe17c);
			set => this.processor.EmitGlobalWord(0xe17c, value);
		}

		/// <summary>0xb77e: the score of the game for the Hall of Fame</summary>
		protected ushort HallOfFameScore
		{
			get => this.processor.PullGlobalWord(0xb77e);
			set => this.processor.EmitGlobalWord(0xb77e, value);
		}

		/// <summary>0xd4dc: the picture of the mouse pointer</summary>
		protected ushort PointerPicture
		{
			get => this.processor.PullGlobalWord(0xd4dc);
			set => this.processor.EmitGlobalWord(0xd4dc, value);
		}

		/// <summary>0xd760: the human player was told there is no room for more units</summary>
		protected ushort UnitLimitTold
		{
			get => this.processor.PullGlobalWord(0xd760);
			set => this.processor.EmitGlobalWord(0xd760, value);
		}
		#endregion
	}
}
