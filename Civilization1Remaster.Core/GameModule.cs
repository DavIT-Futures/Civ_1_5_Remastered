using Civilization1Remaster.Emulation;
using Civilization1Remaster.Video;
using Civilization1Remaster.Game;

namespace Civilization1Remaster
{
	/// <summary>A part of the game (a segment of the original CIV.EXE): the game it belongs to and its processor</summary>
	public abstract class GameModule
	{
		protected readonly GameEngine owner;
		protected readonly CPU processor;
		private UnitMemory? unitTable;

		/// <summary>The records of the units (B6.2: in C#, UnitMemory)</summary>
		protected UnitMemory Troops => this.unitTable ??= new UnitMemory(this.processor);

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

		/// <summary>A message in the buffer TextBuffer.Address that starts with the text</summary>
		protected GameMessage Message(string text) => GameMessage.Start(this.owner, text);

		/// <summary>A message in the buffer TextBuffer.Address that starts with the text at the address</summary>
		protected GameMessage Message(ushort textAddr) => GameMessage.Start(this.owner, textAddr);

		/// <summary>A message in the buffer TextBuffer.Address that starts empty</summary>
		protected GameMessage EmptyMessage() => GameMessage.Empty(this.owner);

		#region Global variables of the data segment, read where the original reads them

		/// <summary>0xaa: the address of the box (a screen and its rectangle) the drawing goes to</summary>
		protected ushort ScreenBox => this.owner.Globals.ScreenBox.Value;

		/// <summary>The fields of the box at ScreenBox (B6.5): display, rectangle, flags, hues, typeface</summary>
		protected CivRectangle DrawingBox => new CivRectangle(this.processor, CPU.AsLinearAddress(this.processor.SegData.Value16, ScreenBox));

		/// <summary>0x19d4: the box of the back screen, which keeps what the screen showed before a dialog or the map</summary>
		protected ushort BackScreenBox => this.owner.Globals.BackScreenBox.Value;

		/// <summary>0x19e8: the box of the picture screen (the world map from column 0xf0, the pieces of the borders)</summary>
		protected ushort PictureScreenBox => this.owner.Globals.PictureScreenBox.Value;

		/// <summary>0x6540: the tiles around the city a worker may take (0x15)</summary>
		protected ushort CityTiles
		{
			get => this.owner.Globals.CityTiles.Value;
			set => this.owner.Globals.CityTiles.Value = value;
		}

		/// <summary>0x6548: the owner of the city in its turn</summary>
		protected ushort CityOwner
		{
			get => this.owner.Globals.CityOwner.Value;
			set => this.owner.Globals.CityOwner.Value = value;
		}

		/// <summary>0x6c9a: the distance to what the last search for the nearest city or unit found</summary>
		protected ushort FoundDistance
		{
			get => this.owner.Globals.FoundDistance.Value;
			set => this.owner.Globals.FoundDistance.Value = value;
		}

		/// <summary>0x70da: the food of the city in its turn</summary>
		protected ushort CityFood
		{
			get => this.owner.Globals.CityFood.Value;
			set => this.owner.Globals.CityFood.Value = value;
		}

		/// <summary>0x70dc: the shields of the city in its turn</summary>
		protected ushort CityShields
		{
			get => this.owner.Globals.CityShields.Value;
			set => this.owner.Globals.CityShields.Value = value;
		}

		/// <summary>0x70de: the trade of the city in its turn</summary>
		protected ushort CityTrade
		{
			get => this.owner.Globals.CityTrade.Value;
			set => this.owner.Globals.CityTrade.Value = value;
		}

		/// <summary>0x70e0: the luxuries of the city in its turn</summary>
		protected ushort CityLuxuries
		{
			get => this.owner.Globals.CityLuxuries.Value;
			set => this.owner.Globals.CityLuxuries.Value = value;
		}

		/// <summary>0x70e2: the happy citizens of the city in its turn</summary>
		protected ushort CityHappy
		{
			get => this.owner.Globals.CityHappy.Value;
			set => this.owner.Globals.CityHappy.Value = value;
		}

		/// <summary>0x70e4: the unhappy citizens of the city in its turn</summary>
		protected ushort CityUnhappy
		{
			get => this.owner.Globals.CityUnhappy.Value;
			set => this.owner.Globals.CityUnhappy.Value = value;
		}

		/// <summary>0x70e6: the science of the city in its turn (by the rates of the owner)</summary>
		protected ushort CityScience
		{
			get => this.owner.Globals.CityScience.Value;
			set => this.owner.Globals.CityScience.Value = value;
		}

		/// <summary>0x70e8: 1 when a tile around the city belongs to the human player</summary>
		protected ushort HumanTileNear
		{
			get => this.owner.Globals.HumanTileNear.Value;
			set => this.owner.Globals.HumanTileNear.Value = value;
		}

		/// <summary>0xb882: the advances that make citizens content (none with the wonder 0x1000 in the city)</summary>
		protected ushort ContentAdvances
		{
			get => this.owner.Globals.ContentAdvances.Value;
			set => this.owner.Globals.ContentAdvances.Value = value;
		}

		/// <summary>0xe8b8: the specialists of the city (bits 26 - 31 of the workers)</summary>
		protected ushort CitySpecialists
		{
			get => this.owner.Globals.CitySpecialists.Value;
			set => this.owner.Globals.CitySpecialists.Value = value;
		}
		/// <summary>0x2496: the tab lit on the city screen (0 - 3)</summary>
		protected ushort CityScreenTab
		{
			get => this.owner.Globals.CityScreenTab.Value;
			set => this.owner.Globals.CityScreenTab.Value = value;
		}

		/// <summary>0x2f9e: the style of the next dialog (its frame and the advisor portrait)</summary>
		protected ushort DialogStyle
		{
			get => this.owner.Globals.DialogStyle.Value;
			set => this.owner.Globals.DialogStyle.Value = value;
		}

		/// <summary>0x6590: the column where the unit heads (see CarryOutGoto)</summary>
		protected ushort GoalHoriz
		{
			get => this.owner.Globals.GoalHoriz.Value;
			set => this.owner.Globals.GoalHoriz.Value = value;
		}

		/// <summary>0x6592: the row where the unit heads (see CarryOutGoto)</summary>
		protected ushort GoalVert
		{
			get => this.owner.Globals.GoalVert.Value;
			set => this.owner.Globals.GoalVert.Value = value;
		}

		/// <summary>0x67f4: the step of the king's animation (0 standing still)</summary>
		protected ushort KingStep
		{
			get => this.owner.Globals.KingStep.Value;
			set => this.owner.Globals.KingStep.Value = value;
		}

		/// <summary>0x68cc: the cursor in the text field of PullTextInput</summary>
		protected ushort TextCursor
		{
			get => this.owner.Globals.TextCursor.Value;
			set => this.owner.Globals.TextCursor.Value = value;
		}

		/// <summary>0x6b90: the player whose turn it is</summary>
		protected ushort TurnPlayer
		{
			get => this.owner.Globals.TurnPlayer.Value;
			set => this.owner.Globals.TurnPlayer.Value = value;
		}

		/// <summary>0xb276: the lines of the menu that cannot be chosen (a bit per line)</summary>
		protected ushort DisabledLines
		{
			get => this.owner.Globals.DisabledLines.Value;
			set => this.owner.Globals.DisabledLines.Value = value;
		}

		/// <summary>0xb276: its low byte alone (the high byte stays)</summary>
		protected byte DisabledLinesLow { set => this.owner.Globals.DisabledLines.Write(0, value); }

		/// <summary>0xd2de: the technology level of the time (AD only)</summary>
		protected ushort TechnologyLevel
		{
			get => this.owner.Globals.TechnologyLevel.Value;
			set => this.owner.Globals.TechnologyLevel.Value = value;
		}

		/// <summary>0xd762: not 0 on VGA (the colours and palettes of the pictures)</summary>
		protected ushort VgaMode
		{
			get => this.owner.Globals.VgaMode.Value;
			set => this.owner.Globals.VgaMode.Value = value;
		}

		/// <summary>0xd76c: the king speaks (counts down)</summary>
		protected ushort KingSpeaking
		{
			get => this.owner.Globals.KingSpeaking.Value;
			set => this.owner.Globals.KingSpeaking.Value = value;
		}

		/// <summary>0xd7f0: the player of the unit, or the owner of the tile, found last (also in 0xd20a)</summary>
		protected ushort FoundPlayer
		{
			get => this.owner.Globals.FoundPlayer.Value;
			set => this.owner.Globals.FoundPlayer.Value = value;
		}

		/// <summary>0xd806: the whole map is shown</summary>
		protected ushort MapRevealed
		{
			get => this.owner.Globals.MapRevealed.Value;
			set => this.owner.Globals.MapRevealed.Value = value;
		}

		/// <summary>0xdc48: asks the main loop to end or to load the game</summary>
		protected ushort GameEndRequest
		{
			get => this.owner.Globals.GameEndRequest.Value;
			set => this.owner.Globals.GameEndRequest.Value = value;
		}
		/// <summary>0x1a3c: a mouse is present</summary>
		protected ushort MousePresent
		{
			get => this.owner.Globals.MousePresent.Value;
			set => this.owner.Globals.MousePresent.Value = value;
		}

		/// <summary>0x2f9a: the default line of the next menu</summary>
		protected ushort DefaultLine
		{
			get => this.owner.Globals.DefaultLine.Value;
			set => this.owner.Globals.DefaultLine.Value = value;
		}

		/// <summary>0x2f9c: the help topic of the menu (while set, the menu comes back even after Escape)</summary>
		protected ushort MenuHelpTopic
		{
			get => this.owner.Globals.MenuHelpTopic.Value;
			set => this.owner.Globals.MenuHelpTopic.Value = value;
		}

		/// <summary>0x3934: the answer of the king dialog (-1 not known yet)</summary>
		protected ushort KingAnswer
		{
			get => this.owner.Globals.KingAnswer.Value;
			set => this.owner.Globals.KingAnswer.Value = value;
		}

		/// <summary>0x6546: the attack units of the city away from it (planes always)</summary>
		protected ushort CityAttackersAway
		{
			get => this.owner.Globals.CityAttackersAway.Value;
			set => this.owner.Globals.CityAttackersAway.Value = value;
		}

		/// <summary>0x680a: the mode of the replay</summary>
		protected ushort ReplayMode
		{
			get => this.owner.Globals.ReplayMode.Value;
			set => this.owner.Globals.ReplayMode.Value = value;
		}

		/// <summary>0x6810: the acting player of the replay event (-1 none)</summary>
		protected ushort ReplayActor
		{
			get => this.owner.Globals.ReplayActor.Value;
			set => this.owner.Globals.ReplayActor.Value = value;
		}

		/// <summary>0x6b32: the choice of the main menu (0 - 3, -1 not chosen)</summary>
		protected ushort MainMenuChoice
		{
			get => this.owner.Globals.MainMenuChoice.Value;
			set => this.owner.Globals.MainMenuChoice.Value = value;
		}

		/// <summary>0xd2e0: the corruption of the city in its turn</summary>
		protected ushort CityCorruption
		{
			get => this.owner.Globals.CityCorruption.Value;
			set => this.owner.Globals.CityCorruption.Value = value;
		}

		/// <summary>0xd2f6: the shields the units of the city cost in its turn</summary>
		protected ushort CityUnitUpkeep
		{
			get => this.owner.Globals.CityUnitUpkeep.Value;
			set => this.owner.Globals.CityUnitUpkeep.Value = value;
		}

		/// <summary>0xd4ca: the order chosen from the menu bar (its key)</summary>
		protected ushort ChosenOrder
		{
			get => this.owner.Globals.ChosenOrder.Value;
			set => this.owner.Globals.ChosenOrder.Value = value;
		}

		/// <summary>0xd4cc: the column of the map view (its top left tile)</summary>
		protected ushort MapViewHoriz
		{
			get => this.owner.Globals.MapViewHoriz.Value;
			set => this.owner.Globals.MapViewHoriz.Value = value;
		}

		/// <summary>0xd75e: the row of the map view (its top left tile)</summary>
		protected ushort MapViewVert
		{
			get => this.owner.Globals.MapViewVert.Value;
			set => this.owner.Globals.MapViewVert.Value = value;
		}

		/// <summary>0xdeb8: the special slots of the city in use (Unfamiliar)</summary>
		protected ushort CitySpecialSlots
		{
			get => this.owner.Globals.CitySpecialSlots.Value;
			set => this.owner.Globals.CitySpecialSlots.Value = value;
		}

		/// <summary>0xdeea: the show level of the mouse pointer (drawn from 1)</summary>
		protected ushort PointerShowLevel
		{
			get => this.owner.Globals.PointerShowLevel.Value;
			set => this.owner.Globals.PointerShowLevel.Value = value;
		}

		/// <summary>0xe17a: the taxes of the city in its turn</summary>
		protected ushort CityTaxes
		{
			get => this.owner.Globals.CityTaxes.Value;
			set => this.owner.Globals.CityTaxes.Value = value;
		}

		/// <summary>0xe3c6: the settlers of the city in its turn</summary>
		protected ushort CitySettlers
		{
			get => this.owner.Globals.CitySettlers.Value;
			set => this.owner.Globals.CitySettlers.Value = value;
		}
		/// <summary>0x3b62: the story text of the intro (0 not started, then telling)</summary>
		protected ushort StoryState
		{
			get => this.owner.Globals.StoryState.Value;
			set => this.owner.Globals.StoryState.Value = value;
		}

		/// <summary>0x4384: the letter of the drive of the saved games</summary>
		protected ushort SaveDrive
		{
			get => this.owner.Globals.SaveDrive.Value;
			set => this.owner.Globals.SaveDrive.Value = value;
		}

		/// <summary>0x6806: the music time of the next line of the story</summary>
		protected ushort StoryLineTime
		{
			get => this.owner.Globals.StoryLineTime.Value;
			set => this.owner.Globals.StoryLineTime.Value = value;
		}

		/// <summary>0x6b64: the main screen and the map are drawn again after the city screen</summary>
		protected ushort MapRedrawRequest
		{
			get => this.owner.Globals.MapRedrawRequest.Value;
			set => this.owner.Globals.MapRedrawRequest.Value = value;
		}

		/// <summary>0x6c98: the divisor of the pollution of the cities</summary>
		protected ushort PollutionFactor
		{
			get => this.owner.Globals.PollutionFactor.Value;
			set => this.owner.Globals.PollutionFactor.Value = value;
		}

		/// <summary>0xb1e8: a city is handled once more with its screen (2 again)</summary>
		protected ushort CityAgain
		{
			get => this.owner.Globals.CityAgain.Value;
			set => this.owner.Globals.CityAgain.Value = value;
		}

		/// <summary>0xb278: 1 when the map is drawn as the world map (a tile in 4 x 4 pixels)</summary>
		protected ushort WorldMapView => this.owner.Globals.WorldMapView.Value;

		/// <summary>0xd20a: the player of the unit found last at a tile (also in 0xd7f0)</summary>
		protected ushort UnitAtTilePlayer
		{
			get => this.owner.Globals.UnitAtTilePlayer.Value;
			set => this.owner.Globals.UnitAtTilePlayer.Value = value;
		}
		/// <summary>0x1cf6: the player whose spaceship arrived this year (-1 none)</summary>
		protected ushort ArrivedSpaceship
		{
			get => this.owner.Globals.ArrivedSpaceship.Value;
			set => this.owner.Globals.ArrivedSpaceship.Value = value;
		}

		/// <summary>0x653a: the unit kind TallyUnitsOfTypeInStack counts</summary>
		protected ushort StackKind
		{
			get => this.owner.Globals.StackKind.Value;
			set => this.owner.Globals.StackKind.Value = value;
		}

		/// <summary>0x653c: the units of the kind in the stack</summary>
		protected ushort StackTally
		{
			get => this.owner.Globals.StackTally.Value;
			set => this.owner.Globals.StackTally.Value = value;
		}

		/// <summary>0x653e: the city of the city screen</summary>
		protected ushort ShownCity
		{
			get => this.owner.Globals.ShownCity.Value;
			set => this.owner.Globals.ShownCity.Value = value;
		}

		/// <summary>0x654a: the result of the menu of the menu bar</summary>
		protected ushort MenuResult
		{
			get => this.owner.Globals.MenuResult.Value;
			set => this.owner.Globals.MenuResult.Value = value;
		}

		/// <summary>0x6794: the cost of the route to the unit</summary>
		protected ushort GotoCost
		{
			get => this.owner.Globals.GotoCost.Value;
			set => this.owner.Globals.GotoCost.Value = value;
		}

		/// <summary>0x6b30: the distance of the city to the palace of its owner</summary>
		protected ushort PalaceDistance
		{
			get => this.owner.Globals.PalaceDistance.Value;
			set => this.owner.Globals.PalaceDistance.Value = value;
		}

		/// <summary>0xb884: the game ends by conquest</summary>
		protected ushort ConquestEnd
		{
			get => this.owner.Globals.ConquestEnd.Value;
			set => this.owner.Globals.ConquestEnd.Value = value;
		}

		/// <summary>0xd804: the audience with the emissary of the player took place this turn</summary>
		protected ushort EmissaryAudience
		{
			get => this.owner.Globals.EmissaryAudience.Value;
			set => this.owner.Globals.EmissaryAudience.Value = value;
		}

		/// <summary>0xdcfc: the units on the map are not drawn</summary>
		protected ushort UnitsHidden
		{
			get => this.owner.Globals.UnitsHidden.Value;
			set => this.owner.Globals.UnitsHidden.Value = value;
		}

		/// <summary>0xdeba: the first disk of the game that is missing</summary>
		protected ushort FirstMissingDisk
		{
			get => this.owner.Globals.FirstMissingDisk.Value;
			set => this.owner.Globals.FirstMissingDisk.Value = value;
		}

		/// <summary>0x3936: the mood of the king in the audience, which picks his next gesture (0, 1, 2; 0xffff: no audience)</summary>
		protected ushort KingMood
		{
			get => this.owner.Globals.KingMood.Value;
			set => this.owner.Globals.KingMood.Value = value;
		}

		/// <summary>0x30b8: the address of the first code text of the text blocks</summary>
		protected ushort CodeText1
		{
			get => this.owner.Globals.CodeText1.Value;
			set => this.owner.Globals.CodeText1.Value = value;
		}

		/// <summary>0x30ba: the address of the second code text of the text blocks</summary>
		protected ushort CodeText2
		{
			get => this.owner.Globals.CodeText2.Value;
			set => this.owner.Globals.CodeText2.Value = value;
		}

		/// <summary>0x30bc: the address of the third code text of the text blocks</summary>
		protected ushort CodeText3
		{
			get => this.owner.Globals.CodeText3.Value;
			set => this.owner.Globals.CodeText3.Value = value;
		}

		/// <summary>0x30be: the address of the fourth code text of the text blocks</summary>
		protected ushort CodeText4
		{
			get => this.owner.Globals.CodeText4.Value;
			set => this.owner.Globals.CodeText4.Value = value;
		}

		/// <summary>0x2fa4: the height of a line of the menu</summary>
		protected ushort MenuLineHeight
		{
			get => this.owner.Globals.MenuLineHeight.Value;
			set => this.owner.Globals.MenuLineHeight.Value = value;
		}

		/// <summary>0xde0e: the right edge of the menu</summary>
		protected ushort MenuRightEdge
		{
			get => this.owner.Globals.MenuRightEdge.Value;
			set => this.owner.Globals.MenuRightEdge.Value = value;
		}

		/// <summary>0x658e: the lines of the menu</summary>
		protected ushort MenuLineCount
		{
			get => this.owner.Globals.MenuLineCount.Value;
			set => this.owner.Globals.MenuLineCount.Value = value;
		}

		/// <summary>0x4b2e: the width of a part of the palace</summary>
		protected ushort PalacePartWidth
		{
			get => this.owner.Globals.PalacePartWidth.Value;
			set => this.owner.Globals.PalacePartWidth.Value = value;
		}

		/// <summary>0x67f8: the picture of the king in the audience (kingNN.pic)</summary>
		protected ushort KingPicture
		{
			get => this.owner.Globals.KingPicture.Value;
			set => this.owner.Globals.KingPicture.Value = value;
		}

		/// <summary>0x6532: the total TallyUnitsByDomainInStack counts</summary>
		protected ushort StackDomainTally
		{
			get => this.owner.Globals.StackDomainTally.Value;
			set => this.owner.Globals.StackDomainTally.Value = value;
		}

		/// <summary>0x1c24: the next population mark of the status bar (tens below 100, then hundreds + 9)</summary>
		protected ushort PopulationMark
		{
			get => this.owner.Globals.PopulationMark.Value;
			set => this.owner.Globals.PopulationMark.Value = value;
		}

		/// <summary>0xe168: the save slot used last</summary>
		protected ushort LastSaveSlot
		{
			get => this.owner.Globals.LastSaveSlot.Value;
			set => this.owner.Globals.LastSaveSlot.Value = value;
		}

		/// <summary>0xdf60: the game continues in the middle of the turn (2 an autosave, 1 a manual save)</summary>
		protected ushort MidTurnSave
		{
			get => this.owner.Globals.MidTurnSave.Value;
			set => this.owner.Globals.MidTurnSave.Value = value;
		}

		/// <summary>0xdeae: the colonists of the spaceship</summary>
		protected ushort SpaceshipColonists
		{
			get => this.owner.Globals.SpaceshipColonists.Value;
			set => this.owner.Globals.SpaceshipColonists.Value = value;
		}

		/// <summary>0xd76a: the game is played on the map of the Earth</summary>
		protected ushort EarthGame
		{
			get => this.owner.Globals.EarthGame.Value;
			set => this.owner.Globals.EarthGame.Value = value;
		}

		/// <summary>0x7efa: the climate of the new world</summary>
		protected ushort Climate
		{
			get => this.owner.Globals.Climate.Value;
			set => this.owner.Globals.Climate.Value = value;
		}

		/// <summary>0x67f6: the frames of the pause of the speaking king</summary>
		protected ushort KingPause
		{
			get => this.owner.Globals.KingPause.Value;
			set => this.owner.Globals.KingPause.Value = value;
		}

		/// <summary>0x6812: the distance of the nearest entry of the replay city table (999 none)</summary>
		protected ushort ReplayCityDistance
		{
			get => this.owner.Globals.ReplayCityDistance.Value;
			set => this.owner.Globals.ReplayCityDistance.Value = value;
		}

		/// <summary>0xb25c: the x of the last icon of the score screen</summary>
		protected ushort ScoreIconHoriz
		{
			get => this.owner.Globals.ScoreIconHoriz.Value;
			set => this.owner.Globals.ScoreIconHoriz.Value = value;
		}

		/// <summary>0xb2bc: the y of the row of icons of the score screen</summary>
		protected ushort ScoreIconVert
		{
			get => this.owner.Globals.ScoreIconVert.Value;
			set => this.owner.Globals.ScoreIconVert.Value = value;
		}

		/// <summary>0x3b64: the next picture of the story (birthN.pic)</summary>
		protected ushort BirthPicture
		{
			get => this.owner.Globals.BirthPicture.Value;
			set => this.owner.Globals.BirthPicture.Value = value;
		}

		/// <summary>0xd4c8: the width of the widest line of the menu</summary>
		protected ushort MenuWidth
		{
			get => this.owner.Globals.MenuWidth.Value;
			set => this.owner.Globals.MenuWidth.Value = value;
		}

		/// <summary>0xb1ec: the line of the first choice of the menu (0xffff none)</summary>
		protected ushort MenuFirstChoice
		{
			get => this.owner.Globals.MenuFirstChoice.Value;
			set => this.owner.Globals.MenuFirstChoice.Value = value;
		}

		/// <summary>0xd208: the choices of the menu</summary>
		protected ushort MenuChoiceCount
		{
			get => this.owner.Globals.MenuChoiceCount.Value;
			set => this.owner.Globals.MenuChoiceCount.Value = value;
		}

		/// <summary>0xd2e2: the first pass of the menu, which measures it</summary>
		protected ushort MenuMeasuring
		{
			get => this.owner.Globals.MenuMeasuring.Value;
			set => this.owner.Globals.MenuMeasuring.Value = value;
		}

		/// <summary>0x654c: the colour of the flashing choice of the menu</summary>
		protected ushort MenuFlashHue
		{
			get => this.owner.Globals.MenuFlashHue.Value;
			set => this.owner.Globals.MenuFlashHue.Value = value;
		}

		/// <summary>0xd7f2: the bits of the options the menu shows as chosen</summary>
		protected ushort MenuOptions
		{
			get => this.owner.Globals.MenuOptions.Value;
			set => this.owner.Globals.MenuOptions.Value = value;
		}

		/// <summary>0x6828: the first picture of the palace parts (0xffff unset)</summary>
		protected ushort PalaceFirstPicture
		{
			get => this.owner.Globals.PalaceFirstPicture.Value;
			set => this.owner.Globals.PalaceFirstPicture.Value = value;
		}

		/// <summary>0xd930: the bushes left of the palace</summary>
		protected ushort PalaceLeftBushes
		{
			get => this.owner.Globals.PalaceLeftBushes.Value;
			set => this.owner.Globals.PalaceLeftBushes.Value = value;
		}

		/// <summary>0x3b66: the next byte of the colour cycle records of the story pictures (0x3710)</summary>
		protected ushort StoryCycleRecord
		{
			get => this.owner.Globals.StoryCycleRecord.Value;
			set => this.owner.Globals.StoryCycleRecord.Value = value;
		}

		/// <summary>0x3b68: the colour cycles of the story picture</summary>
		protected ushort StoryCycleCount
		{
			get => this.owner.Globals.StoryCycleCount.Value;
			set => this.owner.Globals.StoryCycleCount.Value = value;
		}

		/// <summary>0x70d8: the units eliminated are counted as losses</summary>
		protected ushort CountLosses
		{
			get => this.owner.Globals.CountLosses.Value;
			set => this.owner.Globals.CountLosses.Value = value;
		}

		/// <summary>0x6530: the unit that leads the stack</summary>
		protected ushort StackLeader
		{
			get => this.owner.Globals.StackLeader.Value;
			set => this.owner.Globals.StackLeader.Value = value;
		}

		/// <summary>0x7ef4: the pose the king holds (0, 1, 2)</summary>
		protected ushort KingPose
		{
			get => this.owner.Globals.KingPose.Value;
			set => this.owner.Globals.KingPose.Value = value;
		}

		/// <summary>0x67f2: the player whose king gives the audience</summary>
		protected ushort AudiencePlayer
		{
			get => this.owner.Globals.AudiencePlayer.Value;
			set => this.owner.Globals.AudiencePlayer.Value = value;
		}

		/// <summary>0x67f0: the audience shows a line over a motif instead of the throne room</summary>
		protected ushort AudienceWithoutPictures
		{
			get => this.owner.Globals.AudienceWithoutPictures.Value;
			set => this.owner.Globals.AudienceWithoutPictures.Value = value;
		}

		/// <summary>0x680c: the handle of the replay file</summary>
		protected ushort ReplayFile
		{
			get => this.owner.Globals.ReplayFile.Value;
			set => this.owner.Globals.ReplayFile.Value = value;
		}

		/// <summary>0x6c96: the names of the cities written on the map</summary>
		protected ushort CityLabelCount
		{
			get => this.owner.Globals.CityLabelCount.Value;
			set => this.owner.Globals.CityLabelCount.Value = value;
		}

		/// <summary>0x2a6e: the bits of the players that attack a continent of the human player</summary>
		protected ushort HumanContinentAttackers
		{
			get => this.owner.Globals.HumanContinentAttackers.Value;
			set => this.owner.Globals.HumanContinentAttackers.Value = value;
		}

		/// <summary>0xe17c: the disk of the game in use</summary>
		protected ushort DiskInUse
		{
			get => this.owner.Globals.DiskInUse.Value;
			set => this.owner.Globals.DiskInUse.Value = value;
		}

		/// <summary>0xb77e: the score of the game for the Hall of Fame</summary>
		protected ushort HallOfFameScore
		{
			get => this.owner.Globals.HallOfFameScore.Value;
			set => this.owner.Globals.HallOfFameScore.Value = value;
		}

		/// <summary>0xd4dc: the picture of the mouse pointer</summary>
		protected ushort PointerPicture
		{
			get => this.owner.Globals.PointerPicture.Value;
			set => this.owner.Globals.PointerPicture.Value = value;
		}

		/// <summary>0xd760: the human player was told there is no room for more units</summary>
		protected ushort UnitLimitTold
		{
			get => this.owner.Globals.UnitLimitTold.Value;
			set => this.owner.Globals.UnitLimitTold.Value = value;
		}

		/// <summary>0x6804: the handle of story.txt</summary>
		protected ushort StoryFile
		{
			get => this.owner.Globals.StoryFile.Value;
			set => this.owner.Globals.StoryFile.Value = value;
		}

		/// <summary>0x67fc: a key may end the story</summary>
		protected ushort StoryKeyAllowed
		{
			get => this.owner.Globals.StoryKeyAllowed.Value;
			set => this.owner.Globals.StoryKeyAllowed.Value = value;
		}

		/// <summary>0x7ef6: the land mass of the new world</summary>
		protected ushort LandMass
		{
			get => this.owner.Globals.LandMass.Value;
			set => this.owner.Globals.LandMass.Value = value;
		}

		/// <summary>0x7ef8: the temperature of the new world</summary>
		protected ushort Temperature
		{
			get => this.owner.Globals.Temperature.Value;
			set => this.owner.Globals.Temperature.Value = value;
		}

		/// <summary>0x7efc: the age of the new world</summary>
		protected ushort WorldAge
		{
			get => this.owner.Globals.WorldAge.Value;
			set => this.owner.Globals.WorldAge.Value = value;
		}

		/// <summary>0x6dfc: one of the screens could not be reserved</summary>
		protected ushort ScreenReserveFailed
		{
			get => this.owner.Globals.ScreenReserveFailed.Value;
			set => this.owner.Globals.ScreenReserveFailed.Value = value;
		}

		/// <summary>0x1ae0: the screen is to be repainted</summary>
		protected ushort ScreenRepaintRequest
		{
			get => this.owner.Globals.ScreenRepaintRequest.Value;
			set => this.owner.Globals.ScreenRepaintRequest.Value = value;
		}

		/// <summary>0x30c0: the address of the fifth code text of the text blocks</summary>
		protected ushort CodeText5
		{
			get => this.owner.Globals.CodeText5.Value;
			set => this.owner.Globals.CodeText5.Value = value;
		}

		/// <summary>0xb20e: the EGA driver is used</summary>
		protected ushort EgaMode
		{
			get => this.owner.Globals.EgaMode.Value;
			set => this.owner.Globals.EgaMode.Value = value;
		}

		/// <summary>0x67ec: the style of screen 0 kept during the audience</summary>
		protected ushort SavedScreenStyle
		{
			get => this.owner.Globals.SavedScreenStyle.Value;
			set => this.owner.Globals.SavedScreenStyle.Value = value;
		}

		/// <summary>0x67ea: the picture of the king is loaded</summary>
		protected ushort KingPictureLoaded
		{
			get => this.owner.Globals.KingPictureLoaded.Value;
			set => this.owner.Globals.KingPictureLoaded.Value = value;
		}

		/// <summary>0x67e8: the pose of the advisors shown (0xffff none)</summary>
		protected ushort AdvisorPoseShown
		{
			get => this.owner.Globals.AdvisorPoseShown.Value;
			set => this.owner.Globals.AdvisorPoseShown.Value = value;
		}

		/// <summary>0xb1e6: the picture of the king in the throne room</summary>
		protected ushort KingScenePicture
		{
			get => this.owner.Globals.KingScenePicture.Value;
			set => this.owner.Globals.KingScenePicture.Value = value;
		}

		/// <summary>0x7efe: the picture of the faces and mouths of the king</summary>
		protected ushort KingFacesPicture
		{
			get => this.owner.Globals.KingFacesPicture.Value;
			set => this.owner.Globals.KingFacesPicture.Value = value;
		}

		/// <summary>0xdea8: the propulsion components of the spaceship</summary>
		protected ushort SpaceshipPropulsion
		{
			get => this.owner.Globals.SpaceshipPropulsion.Value;
			set => this.owner.Globals.SpaceshipPropulsion.Value = value;
		}

		/// <summary>0xdeaa: the fuel components of the spaceship</summary>
		protected ushort SpaceshipFuel
		{
			get => this.owner.Globals.SpaceshipFuel.Value;
			set => this.owner.Globals.SpaceshipFuel.Value = value;
		}

		/// <summary>0xdeb0: the support modules of the spaceship</summary>
		protected ushort SpaceshipSupport
		{
			get => this.owner.Globals.SpaceshipSupport.Value;
			set => this.owner.Globals.SpaceshipSupport.Value = value;
		}

		/// <summary>0xdeb2: the energy modules of the spaceship</summary>
		protected ushort SpaceshipEnergy
		{
			get => this.owner.Globals.SpaceshipEnergy.Value;
			set => this.owner.Globals.SpaceshipEnergy.Value = value;
		}

		/// <summary>0x687c: all components of the spaceship are there</summary>
		protected ushort SpaceshipComponentsComplete
		{
			get => this.owner.Globals.SpaceshipComponentsComplete.Value;
			set => this.owner.Globals.SpaceshipComponentsComplete.Value = value;
		}

		/// <summary>0x687e: all modules of the spaceship are there</summary>
		protected ushort SpaceshipModulesComplete
		{
			get => this.owner.Globals.SpaceshipModulesComplete.Value;
			set => this.owner.Globals.SpaceshipModulesComplete.Value = value;
		}

		/// <summary>0x682a: the picture of the spaceship parts</summary>
		protected ushort SpaceshipPicture
		{
			get => this.owner.Globals.SpaceshipPicture.Value;
			set => this.owner.Globals.SpaceshipPicture.Value = value;
		}

		/// <summary>0x681c: the error of the last file operation</summary>
		protected ushort FileError
		{
			get => this.owner.Globals.FileError.Value;
			set => this.owner.Globals.FileError.Value = value;
		}

		/// <summary>0x6822: the human player of the saved game</summary>
		protected ushort SavedHumanPlayer
		{
			get => this.owner.Globals.SavedHumanPlayer.Value;
			set => this.owner.Globals.SavedHumanPlayer.Value = value;
		}

		/// <summary>0x6824: the difficulty of the saved game</summary>
		protected ushort SavedDifficulty
		{
			get => this.owner.Globals.SavedDifficulty.Value;
			set => this.owner.Globals.SavedDifficulty.Value = value;
		}

		/// <summary>0x6826: the year of the saved game</summary>
		protected ushort SavedYear
		{
			get => this.owner.Globals.SavedYear.Value;
			set => this.owner.Globals.SavedYear.Value = value;
		}

		/// <summary>0x680e: the replay is shown at the end of the game</summary>
		protected ushort ReplayAtGameEnd
		{
			get => this.owner.Globals.ReplayAtGameEnd.Value;
			set => this.owner.Globals.ReplayAtGameEnd.Value = value;
		}

		/// <summary>0x6808: the Civilopedia list has a next page</summary>
		protected ushort CivilopediaNextPage
		{
			get => this.owner.Globals.CivilopediaNextPage.Value;
			set => this.owner.Globals.CivilopediaNextPage.Value = value;
		}

		/// <summary>0x652e: the show level of the mouse pointer kept</summary>
		protected ushort SavedPointerShowLevel
		{
			get => this.owner.Globals.SavedPointerShowLevel.Value;
			set => this.owner.Globals.SavedPointerShowLevel.Value = value;
		}

		/// <summary>0x68d0: the column of the text cursor</summary>
		protected ushort TextCursorColumn
		{
			get => this.owner.Globals.TextCursorColumn.Value;
			set => this.owner.Globals.TextCursorColumn.Value = value;
		}

		/// <summary>0x68ce: the width of the character at the text cursor</summary>
		protected ushort TextCursorWidth
		{
			get => this.owner.Globals.TextCursorWidth.Value;
			set => this.owner.Globals.TextCursorWidth.Value = value;
		}

		/// <summary>0x6536: what TallyUnitsByDomainInStack counts</summary>
		protected ushort StackDomainMode
		{
			get => this.owner.Globals.StackDomainMode.Value;
			set => this.owner.Globals.StackDomainMode.Value = value;
		}

		/// <summary>0x6538: the units below this index count as defensive units</summary>
		protected ushort StackIndexLimit
		{
			get => this.owner.Globals.StackIndexLimit.Value;
			set => this.owner.Globals.StackIndexLimit.Value = value;
		}

		/// <summary>0x6796: the x of the place of the route</summary>
		protected ushort RoutePlaceHoriz
		{
			get => this.owner.Globals.RoutePlaceHoriz.Value;
			set => this.owner.Globals.RoutePlaceHoriz.Value = value;
		}

		/// <summary>0x6798: the y of the place of the route</summary>
		protected ushort RoutePlaceVert
		{
			get => this.owner.Globals.RoutePlaceVert.Value;
			set => this.owner.Globals.RoutePlaceVert.Value = value;
		}

		/// <summary>0x6ed4: the player is at war with someone</summary>
		protected ushort PlayerAtWar
		{
			get => this.owner.Globals.PlayerAtWar.Value;
			set => this.owner.Globals.PlayerAtWar.Value = value;
		}

		/// <summary>0x28bc: the city is seized without the animations</summary>
		protected ushort NoConquestAnimation
		{
			get => this.owner.Globals.NoConquestAnimation.Value;
			set => this.owner.Globals.NoConquestAnimation.Value = value;
		}

		/// <summary>0xd934: the bushes right of the palace</summary>
		protected ushort PalaceRightBushes
		{
			get => this.owner.Globals.PalaceRightBushes.Value;
			set => this.owner.Globals.PalaceRightBushes.Value = value;
		}

		/// <summary>0xd764: the score of the wonders</summary>
		protected ushort WonderScore
		{
			get => this.owner.Globals.WonderScore.Value;
			set => this.owner.Globals.WonderScore.Value = value;
		}

		/// <summary>0xd766: the score of the citizens</summary>
		protected ushort CitizenScore
		{
			get => this.owner.Globals.CitizenScore.Value;
			set => this.owner.Globals.CitizenScore.Value = value;
		}

		/// <summary>0xd206: the menu has no choice, any key closes it ("Press key to continue")</summary>
		protected ushort MenuAnyKey
		{
			get => this.owner.Globals.MenuAnyKey.Value;
			set => this.owner.Globals.MenuAnyKey.Value = value;
		}

		/// <summary>0x2fa0: the menu box is 2 pixels taller with the text 0x2fbd at its bottom right</summary>
		protected ushort MenuFooter
		{
			get => this.owner.Globals.MenuFooter.Value;
			set => this.owner.Globals.MenuFooter.Value = value;
		}

		/// <summary>0x681a: the game is loaded, not saved ("Which drive contains your saved game files?")</summary>
		protected ushort LoadingGame
		{
			get => this.owner.Globals.LoadingGame.Value;
			set => this.owner.Globals.LoadingGame.Value = value;
		}

		/// <summary>0x681e: the handle of the file of the game loaded</summary>
		protected ushort LoadFile
		{
			get => this.owner.Globals.LoadFile.Value;
			set => this.owner.Globals.LoadFile.Value = value;
		}

		/// <summary>0x3c62: the Civilopedia is shown without its pictures (disk missing)</summary>
		protected ushort CivilopediaWithoutPictures
		{
			get => this.owner.Globals.CivilopediaWithoutPictures.Value;
			set => this.owner.Globals.CivilopediaWithoutPictures.Value = value;
		}

		/// <summary>0x6542: the unhappy citizens above the size of the city</summary>
		protected ushort CityExcessUnhappy
		{
			get => this.owner.Globals.CityExcessUnhappy.Value;
			set => this.owner.Globals.CityExcessUnhappy.Value = value;
		}

		/// <summary>0xe3c2: the unpaid support of the cities; a computer player above 0 falls into anarchy</summary>
		protected ushort AnarchyPressure
		{
			get => this.owner.Globals.AnarchyPressure.Value;
			set => this.owner.Globals.AnarchyPressure.Value = value;
		}

		/// <summary>0xdb42: a computer player at 0 or more chooses democracy</summary>
		protected ushort DemocracyPressure
		{
			get => this.owner.Globals.DemocracyPressure.Value;
			set => this.owner.Globals.DemocracyPressure.Value = value;
		}

		/// <summary>0x20f4: new units are set up without repainting and moving them</summary>
		protected ushort QuietUnitSetup
		{
			get => this.owner.Globals.QuietUnitSetup.Value;
			set => this.owner.Globals.QuietUnitSetup.Value = value;
		}

		/// <summary>0x6ed6: the left offset of the map view focus</summary>
		protected ushort MapFocusLeft
		{
			get => this.owner.Globals.MapFocusLeft.Value;
			set => this.owner.Globals.MapFocusLeft.Value = value;
		}

		/// <summary>0x70ea: the top offset of the map view focus</summary>
		protected ushort MapFocusTop
		{
			get => this.owner.Globals.MapFocusTop.Value;
			set => this.owner.Globals.MapFocusTop.Value = value;
		}

		/// <summary>0x19fc: the address of the box of the sprites</summary>
		protected ushort SpriteScreenBox
		{
			get => this.owner.Globals.SpriteScreenBox.Value;
			set => this.owner.Globals.SpriteScreenBox.Value = value;
		}

		/// <summary>0x67fe: the pixels under the clouds lightened while the world is made</summary>
		protected ushort LightenedPixels
		{
			get => this.owner.Globals.LightenedPixels.Value;
			set => this.owner.Globals.LightenedPixels.Value = value;
		}

		/// <summary>0x1a3e: little memory with VGA: one sprite for every row</summary>
		protected ushort LowMemoryVga
		{
			get => this.owner.Globals.LowMemoryVga.Value;
			set => this.owner.Globals.LowMemoryVga.Value = value;
		}

		/// <summary>0x5876: the picture of the mouse pointer drawn</summary>
		protected ushort PointerImage
		{
			get => this.owner.Globals.PointerImage.Value;
			set => this.owner.Globals.PointerImage.Value = value;
		}

		/// <summary>0x5878: the x of the hot spot of the mouse pointer</summary>
		protected ushort PointerHotHoriz
		{
			get => this.owner.Globals.PointerHotHoriz.Value;
			set => this.owner.Globals.PointerHotHoriz.Value = value;
		}

		/// <summary>0x587a: the y of the hot spot of the mouse pointer</summary>
		protected ushort PointerHotVert
		{
			get => this.owner.Globals.PointerHotVert.Value;
			set => this.owner.Globals.PointerHotVert.Value = value;
		}

		/// <summary>0x6b8e: the king welcomed the friendship</summary>
		protected ushort AudienceWelcomed
		{
			get => this.owner.Globals.AudienceWelcomed.Value;
			set => this.owner.Globals.AudienceWelcomed.Value = value;
		}

		/// <summary>0x67ee: the pictures of the audience are the modern ones (technology 0x14)</summary>
		protected ushort KingModernPictures
		{
			get => this.owner.Globals.KingModernPictures.Value;
			set => this.owner.Globals.KingModernPictures.Value = value;
		}

		/// <summary>0x804e: the picture of the government in the audience (govtNa.pic)</summary>
		protected ushort GovernmentPicture
		{
			get => this.owner.Globals.GovernmentPicture.Value;
			set => this.owner.Globals.GovernmentPicture.Value = value;
		}
				/// <summary>0x1c28: while RepaintMainScreen draws (1): the population mark is not announced</summary>
		protected ushort RepaintingMainScreen
		{
			get => this.owner.Globals.RepaintingMainScreen.Value;
			set => this.owner.Globals.RepaintingMainScreen.Value = value;
		}

		/// <summary>0x2494: a tile of the city was marked with a dot (taken by another city) while the tiles were counted</summary>
		protected ushort CityTileDotted
		{
			get => this.owner.Globals.CityTileDotted.Value;
			set => this.owner.Globals.CityTileDotted.Value = value;
		}

		/// <summary>0x2f98: after the tribe selection (1): boxes of style 7 get the motif</summary>
		protected ushort BoxMotif
		{
			get => this.owner.Globals.BoxMotif.Value;
			set => this.owner.Globals.BoxMotif.Value = value;
		}

		/// <summary>0x2fa2: the mouse was used in the menu (1)</summary>
		protected ushort MenuMouseUsed
		{
			get => this.owner.Globals.MenuMouseUsed.Value;
			set => this.owner.Globals.MenuMouseUsed.Value = value;
		}

		/// <summary>0x2fec: the colour cycles 1 - 3 of the map are running (LaunchHueCycling / HaltHueCycling)</summary>
		protected ushort HueCyclingOn
		{
			get => this.owner.Globals.HueCyclingOn.Value;
			set => this.owner.Globals.HueCyclingOn.Value = value;
		}

		/// <summary>0x3484: a game was just loaded (0xfffd, then 0xfffe in the first score of the turn, then 0)</summary>
		protected ushort LoadedGameMark
		{
			get => this.owner.Globals.LoadedGameMark.Value;
			set => this.owner.Globals.LoadedGameMark.Value = value;
		}

		/// <summary>0x4b32: the shift of a palace wing from its end</summary>
		protected ushort PalaceWingShift
		{
			get => this.owner.Globals.PalaceWingShift.Value;
			set => this.owner.Globals.PalaceWingShift.Value = value;
		}

		/// <summary>0x6534: the best defence score in the stack so far (its leader)</summary>
		protected ushort StackLeaderScore
		{
			get => this.owner.Globals.StackLeaderScore.Value;
			set => this.owner.Globals.StackLeaderScore.Value = value;
		}

		/// <summary>0x67fa: the screen of the king was kept in the back screen (1)</summary>
		protected ushort KingScreenKept
		{
			get => this.owner.Globals.KingScreenKept.Value;
			set => this.owner.Globals.KingScreenKept.Value = value;
		}

		/// <summary>0x6800: the time of the music in the story told while the world is made (a song position, or the seconds)</summary>
		protected ushort StoryMusicTime
		{
			get => this.owner.Globals.StoryMusicTime.Value;
			set => this.owner.Globals.StoryMusicTime.Value = value;
		}

		/// <summary>0x6802: the lines of the story told so far</summary>
		protected ushort StoryLinesTold
		{
			get => this.owner.Globals.StoryLinesTold.Value;
			set => this.owner.Globals.StoryLinesTold.Value = value;
		}

		/// <summary>0x6b92: the riot in the view of the city was shown this turn (1)</summary>
		protected ushort RiotShown
		{
			get => this.owner.Globals.RiotShown.Value;
			set => this.owner.Globals.RiotShown.Value = value;
		}

		/// <summary>0x6e92: the picture of the mouse pointer (icon 0x277)</summary>
		protected ushort PointerIcon
		{
			get => this.owner.Globals.PointerIcon.Value;
			set => this.owner.Globals.PointerIcon.Value = value;
		}

		/// <summary>0x8078: the human player is far behind late in the game (standing 7, more than 4 cities, no spaceship part, past turn 200)</summary>
		protected ushort LateGameNudge
		{
			get => this.owner.Globals.LateGameNudge.Value;
			set => this.owner.Globals.LateGameNudge.Value = value;
		}

		/// <summary>0xb22a: a choice of the palace layout: 1 moves the parts one column to the right</summary>
		protected ushort PalaceLayout
		{
			get => this.owner.Globals.PalaceLayout.Value;
			set => this.owner.Globals.PalaceLayout.Value = value;
		}

		/// <summary>0xde20: the living players, counted at the start of the turn</summary>
		protected ushort LiveRealmCount
		{
			get => this.owner.Globals.LiveRealmCount.Value;
			set => this.owner.Globals.LiveRealmCount.Value = value;
		}

		/// <summary>0xdb38: whether a dialog or menu background needs to be redrawn (1 = needs redraw)</summary>
		protected ushort DialogNeedsRedraw
		{
			get => this.owner.Globals.DialogNeedsRedraw.Value;
			set => this.owner.Globals.DialogNeedsRedraw.Value = value;
		}

		/// <summary>0x8066: whether the 'More' box on the menu is displayed</summary>
		protected ushort MenuMoreBox
		{
			get => this.owner.Globals.MenuMoreBox.Value;
			set => this.owner.Globals.MenuMoreBox.Value = value;
		}

		/// <summary>0x654e: the base of the table of line offsets in a menu</summary>
		protected ushort MenuLineOffsets
		{
			get => this.owner.Globals.MenuLineOffsets.Value;
			set => this.owner.Globals.MenuLineOffsets.Value = value;
		}

		/// <summary>0x5116: vertical shift for wonders/score icons</summary>
		protected short ScoreVerticalShift
		{
			get => (short)this.owner.Globals.ScoreVerticalShift.Value;
			set => this.owner.Globals.ScoreVerticalShift.Value = (ushort)value;
		}

		/// <summary>0xdc6a: schism share parameter / peace turns in Hall of Fame</summary>
		protected ushort SchismShareDc6a
		{
			get => this.owner.Globals.SchismShareDc6a.Value;
			set => this.owner.Globals.SchismShareDc6a.Value = value;
		}

		/// <summary>0x6b8c: whether text coordinate is doubled (e.g. 640-pixel screen modes)</summary>
		protected ushort DoubleWidthText
		{
			get => this.owner.Globals.DoubleWidthText.Value;
			set => this.owner.Globals.DoubleWidthText.Value = value;
		}

		/// <summary>0xdf0c: picture handle of the cheering crowd in the newspaper</summary>
		protected ushort NewspaperCrowdPicture
		{
			get => this.owner.Globals.NewspaperCrowdPicture.Value;
			set => this.owner.Globals.NewspaperCrowdPicture.Value = value;
		}

		/// <summary>0xd2dc: initial prompt flag in audience with foreign king</summary>
		protected ushort AudienceInitialPrompt
		{
			get => this.owner.Globals.AudienceInitialPrompt.Value;
			set => this.owner.Globals.AudienceInitialPrompt.Value = value;
		}

		/// <summary>0xdeb6: unit support count</summary>
		protected ushort UnitSupportCount
		{
			get => this.owner.Globals.UnitSupportCount.Value;
			set => this.owner.Globals.UnitSupportCount.Value = value;
		}

		/// <summary>0x6820: loop counter of player index during game import</summary>
		protected ushort ImportPlayerIndex
		{
			get => this.owner.Globals.ImportPlayerIndex.Value;
			set => this.owner.Globals.ImportPlayerIndex.Value = value;
		}

		/// <summary>0xd4da: picture handle of the tile fortress</summary>
		protected ushort TileFortressPicture
		{
			get => this.owner.Globals.TileFortressPicture.Value;
			set => this.owner.Globals.TileFortressPicture.Value = value;
		}

		/// <summary>0xb880: picture handle of the mini peace symbol on the map</summary>
		protected ushort MiniSymbolPicture
		{
			get => this.owner.Globals.MiniSymbolPicture.Value;
			set => this.owner.Globals.MiniSymbolPicture.Value = value;
		}

		/// <summary>0x6e9e: picture handle of the unhappy citizen</summary>
		protected ushort UnhappyCitizenPicture
		{
			get => this.owner.Globals.UnhappyCitizenPicture.Value;
			set => this.owner.Globals.UnhappyCitizenPicture.Value = value;
		}

		/// <summary>0xb2ba: picture handle of the pollution skull icon</summary>
		protected ushort PollutionIconPicture
		{
			get => this.owner.Globals.PollutionIconPicture.Value;
			set => this.owner.Globals.PollutionIconPicture.Value = value;
		}

		/// <summary>0x6834: picture handle of the spaceship habitat module</summary>
		protected ushort SpaceshipHabitatPicture
		{
			get => this.owner.Globals.SpaceshipHabitatPicture.Value;
			set => this.owner.Globals.SpaceshipHabitatPicture.Value = value;
		}

		/// <summary>0x32d6: picture handle of the reserved intro screen</summary>
		protected ushort IntroReservedScreen
		{
			get => this.owner.Globals.IntroReservedScreen.Value;
			set => this.owner.Globals.IntroReservedScreen.Value = value;
		}

		/// <summary>0x4b2c: width of the palace left wing</summary>
		protected ushort PalaceLeftWingWidth
		{
			get => this.owner.Globals.PalaceLeftWingWidth.Value;
			set => this.owner.Globals.PalaceLeftWingWidth.Value = value;
		}

		/// <summary>0x4b34: width of the palace right wing</summary>
		protected ushort PalaceRightWingWidth
		{
			get => this.owner.Globals.PalaceRightWingWidth.Value;
			set => this.owner.Globals.PalaceRightWingWidth.Value = value;
		}

		/// <summary>0xd922: palace style horizontal shift</summary>
		protected ushort PalaceStyleShift
		{
			get => this.owner.Globals.PalaceStyleShift.Value;
			set => this.owner.Globals.PalaceStyleShift.Value = value;
		}

		/// <summary>0xd932: palace garden background index</summary>
		protected ushort PalaceGardenBackground
		{
			get => this.owner.Globals.PalaceGardenBackground.Value;
			set => this.owner.Globals.PalaceGardenBackground.Value = value;
		}

		/// <summary>0x33a8: saved government title for communism</summary>
		protected ushort SavedGovernmentTitleCommunism
		{
			get => this.owner.Globals.SavedGovernmentTitleCommunism.Value;
			set => this.owner.Globals.SavedGovernmentTitleCommunism.Value = value;
		}

		/// <summary>0x33aa: saved government title for monarchy</summary>
		protected ushort SavedGovernmentTitleMonarchy
		{
			get => this.owner.Globals.SavedGovernmentTitleMonarchy.Value;
			set => this.owner.Globals.SavedGovernmentTitleMonarchy.Value = value;
		}

		#endregion

		#region Tables of CIV.EXE in the data segment, read where the original reads them

		/// <summary>A word field (UnitKindTable) of the unit kind: 0x112a + 0x22 * kind + field</summary>
		protected ushort UnitKindWord(int kind, int field) => this.owner.Tables.Word((ushort)(UnitKindTable.Stride * kind + UnitKindTable.Origin + field));

		/// <summary>A word field of the unit kind through the offset 0x22 * kind computed before</summary>
		protected ushort UnitKindWordAt(int offset, int field) => this.owner.Tables.Word((ushort)(offset + UnitKindTable.Origin + field));

		/// <summary>The address of the name of the unit kind through the offset 0x22 * kind computed before</summary>
		protected static ushort UnitTextAt(int offset) => (ushort)(offset + UnitKindTable.Origin);

		/// <summary>A word field (BuildingTable) of the building record: 0xb9a + 0x1e * record + field</summary>
		protected ushort BuildingWord(int record, int field) => this.owner.Tables.Word((ushort)(BuildingTable.Stride * record + BuildingTable.Origin + field));

		/// <summary>A word field of the building record through the offset 0x1e * record computed before</summary>
		protected ushort BuildingWordAt(int offset, int field) => this.owner.Tables.Word((ushort)(offset + BuildingTable.Origin + field));

		/// <summary>The address of the name of the building record: 0xb9a + 0x1e * record</summary>
		protected static ushort BuildingRecordText(int record) => (ushort)(BuildingTable.Stride * record + BuildingTable.Origin);

		/// <summary>The address of the name of the building record through the offset 0x1e * record computed before</summary>
		protected static ushort BuildingRecordTextAt(int offset) => (ushort)(offset + BuildingTable.Origin);

		/// <summary>A byte field (TerrainTable) of the terrain type: 0x282 + 0x13 * terrain + field</summary>
		protected byte TerrainField(int terrain, int field) => this.owner.Tables.Byte((ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin + field));

		/// <summary>A byte field of the terrain type through the offset 0x13 * terrain computed before</summary>
		protected byte TerrainFieldAt(int offset, int field) => this.owner.Tables.Byte((ushort)(offset + TerrainTable.Origin + field));

		/// <summary>The address of the name of the terrain type: 0x282 + 0x13 * terrain</summary>
		protected static ushort TerrainText(int terrain) => (ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin);

		/// <summary>The address of the name of the terrain type through the offset 0x13 * terrain computed before</summary>
		protected static ushort TerrainTextAt(int offset) => (ushort)(offset + TerrainTable.Origin);

		/// <summary>The address of a text field (LeaderTable) of the leader: 0x14e2 + 0x3a * leader + field</summary>
		protected static ushort LeaderText(int leader, int field) => (ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field);

		/// <summary>The address of a text field of the leader through the offset 0x3a * leader computed before</summary>
		protected static ushort LeaderTextAt(int offset, int field) => (ushort)(offset + LeaderTable.Origin + field);

		/// <summary>A word field (LeaderTable) of the leader: 0x14e2 + 0x3a * leader + field</summary>
		protected ushort LeaderWord(int leader, int field) => this.owner.Tables.Word((ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field));

		/// <summary>A word field of the leader through the offset 0x3a * leader computed before</summary>
		protected ushort LeaderWordAt(int offset, int field) => this.owner.Tables.Word((ushort)(offset + LeaderTable.Origin + field));

		/// <summary>CivilizationName through the offset 2 * index computed before</summary>
		protected ushort CivilizationNameAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x1982));

		/// <summary>CivilizationPlural through the offset 2 * index computed before</summary>
		protected ushort CivilizationPluralAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x1992));

		/// <summary>LeaderName through the offset 2 * index computed before</summary>
		protected ushort LeaderNameAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x19a2));

		/// <summary>GovernmentName through the offset 2 * index computed before</summary>
		protected ushort GovernmentNameAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x1966));

		/// <summary>GovernmentTitle through the offset 2 * index computed before</summary>
		protected ushort GovernmentTitleAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x19b2));

		/// <summary>PlayerColour through the offset 2 * index computed before</summary>
		protected ushort PlayerColourAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x1946));

		/// <summary>DirectionHoriz through the offset 2 * index computed before</summary>
		protected ushort DirectionHorizAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x1882));

		/// <summary>DirectionVert through the offset 2 * index computed before</summary>
		protected ushort DirectionVertAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0x18e4));

		/// <summary>SpritePicture through the offset 2 * index computed before</summary>
		protected ushort SpritePictureAt(int offset) => this.owner.Tables.Word((ushort)(offset + 0xd4ce));

		/// <summary>The address of the name of the technology (records of 0x16 bytes at 0x4da)</summary>
		protected static ushort TechnologyText(int index) => (ushort)(0x16 * index + 0x4da);

		/// <summary>The address of the name of the building (records of 0x1e bytes from 0xb9a, the name at 0xbb8 + 0x1e * building)</summary>
		protected static ushort BuildingText(int index) => (ushort)(0x1e * index + 0xbb8);

		/// <summary>The address of the name of the wonder (records of 0x1e bytes, the name at 0xe6a + 0x1e * wonder)</summary>
		protected static ushort WonderText(int index) => (ushort)(0x1e * index + 0xe6a);

		/// <summary>The address of the name of the unit kind (records of 0x22 bytes at 0x112a, UnitKindTable)</summary>
		protected static ushort UnitText(int index) => (ushort)(0x22 * index + 0x112a);

		/// <summary>0x1982 + 2 * index: the text of the name of the civilization of the player ("Babylonian")</summary>
		protected ushort CivilizationName(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x1982));

		/// <summary>0x1992 + 2 * index: the text of the plural name of the civilization of the player ("Babylonians")</summary>
		protected ushort CivilizationPlural(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x1992));

		/// <summary>0x19a2 + 2 * index: the text of the name of the leader of the player</summary>
		protected ushort LeaderName(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x19a2));

		/// <summary>0x1966 + 2 * index: the text of the name of the government</summary>
		protected ushort GovernmentName(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x1966));

		/// <summary>0x19b2 + 2 * index: the text of the title of the leader under the government</summary>
		protected ushort GovernmentTitle(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x19b2));

		/// <summary>0x1946 + 2 * index: the colour of the player</summary>
		protected ushort PlayerColour(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x1946));

		/// <summary>0x1882 + 2 * index: the x step of the direction (0 the tile itself, 1 - 8 the neighbours, then the second ring)</summary>
		protected ushort DirectionHoriz(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x1882));

		/// <summary>0x18e4 + 2 * index: the y step of the direction</summary>
		protected ushort DirectionVert(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0x18e4));

		/// <summary>0xd4ce + 2 * index: the picture of the sprite (0x40 + 32 * player + unit kind: the units of the players)</summary>
		protected ushort SpritePicture(int index) => this.owner.Tables.Word((ushort)((index << 1) + 0xd4ce));
		#endregion
	}
}
