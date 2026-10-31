
namespace Civilization1Remaster.Core.Engine
{
	/// <summary>
	/// The tables of CIV.EXE in the data segment from 0x282 to 0x19c1 kept in C# (B6.3): terrain (0x282), technologies
	/// (0x4da), buildings and wonders (0xb9a), the technologies that make the wonders obsolete (0x10fe), unit types (0x112a,
	/// saved with the game), leaders (0x14e2), directions (0x1882 / 0x18e4), colours (0x1946), the texts of the
	/// governments, civilizations, leaders and titles (0x1966 - 0x19c1) and the few bytes between them. The accessors below read them
	/// here (GameComponent.Tables), the rest of the code through the memory (the overlay).
	/// </summary>
	public sealed class GameTables
	{
		/// <summary>The first address of the tables</summary>
		public const ushort Begin = 0x282;

		/// <summary>The address after the tables</summary>
		public const ushort End = 0x19c2;

		private readonly GameEngine game;

		public GameTables(GameEngine game)
		{
			this.game = game;
		}

		/// <summary>The bytes of the tables</summary>
		public byte[] Content { get; } = new byte[End - Begin];

		/// <summary>The word at the address: from the tables, or from the memory outside them (an index past a table)</summary>
		public ushort Word(ushort address) => address >= Begin && address + 1 < End ?
			(ushort)(this.Content[address - Begin] | (this.Content[address - Begin + 1] << 8)) :
			this.game.Processor.PullGlobalWord(address);

		/// <summary>The byte at the address (see Word)</summary>
		public byte Byte(ushort address) => address >= Begin && address < End ?
			this.Content[address - Begin] :
			this.game.Processor.PullGlobalByte(address);

		/// <summary>The address of the name of the unit kind through the offset 0x22 * kind computed before</summary>
		public static ushort UnitTextAt(int offset) => (ushort)(offset + UnitKindTable.Origin);

		/// <summary>The address of the name of the building record: 0xb9a + 0x1e * record</summary>
		public static ushort BuildingRecordText(int record) => (ushort)(BuildingTable.Stride * record + BuildingTable.Origin);

		/// <summary>The address of the name of the building record through the offset 0x1e * record computed before</summary>
		public static ushort BuildingRecordTextAt(int offset) => (ushort)(offset + BuildingTable.Origin);

		public static ushort TerrainText(int terrain) => (ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin);

		/// <summary>The address of the name of the terrain type through the offset 0x13 * terrain computed before</summary>
		public static ushort TerrainTextAt(int offset) => (ushort)(offset + TerrainTable.Origin);

		/// <summary>The address of a text field (LeaderTable) of the leader: 0x14e2 + 0x3a * leader + field</summary>
		public static ushort LeaderText(int leader, int field) => (ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field);

		/// <summary>The address of a text field of the leader through the offset 0x3a * leader computed before</summary>
		public static ushort LeaderTextAt(int offset, int field) => (ushort)(offset + LeaderTable.Origin + field);

		/// <summary>The address of the name of the technology (records of 0x16 bytes at 0x4da)</summary>
		public static ushort TechnologyText(int index) => (ushort)(0x16 * index + 0x4da);

		/// <summary>The address of the name of the building (records of 0x1e bytes from 0xb9a, the name at 0xbb8 + 0x1e * building)</summary>
		public static ushort BuildingText(int index) => (ushort)(0x1e * index + 0xbb8);

		/// <summary>The address of the name of the wonder (records of 0x1e bytes, the name at 0xe6a + 0x1e * wonder)</summary>
		public static ushort WonderText(int index) => (ushort)(0x1e * index + 0xe6a);

		/// <summary>The address of the name of the unit kind (records of 0x22 bytes at 0x112a, UnitKindTable)</summary>
		public static ushort UnitText(int index) => (ushort)(0x22 * index + 0x112a);

		/// <summary>A word field (UnitKindTable) of the unit kind: 0x112a + 0x22 * kind + field</summary>
		public ushort UnitKindWord(int kind, int field) => Word((ushort)(UnitKindTable.Stride * kind + UnitKindTable.Origin + field));

		/// <summary>A word field of the unit kind through the offset 0x22 * kind computed before</summary>
		public ushort UnitKindWordAt(int offset, int field) => Word((ushort)(offset + UnitKindTable.Origin + field));

		/// <summary>A word field (BuildingTable) of the building record: 0xb9a + 0x1e * record + field</summary>
		public ushort BuildingWord(int record, int field) => Word((ushort)(BuildingTable.Stride * record + BuildingTable.Origin + field));

		/// <summary>A word field of the building record through the offset 0x1e * record computed before</summary>
		public ushort BuildingWordAt(int offset, int field) => Word((ushort)(offset + BuildingTable.Origin + field));

		/// <summary>A byte field (TerrainTable) of the terrain type: 0x282 + 0x13 * terrain + field</summary>
		public byte TerrainField(int terrain, int field) => Byte((ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin + field));

		/// <summary>A byte field of the terrain type through the offset 0x13 * terrain computed before</summary>
		public byte TerrainFieldAt(int offset, int field) => Byte((ushort)(offset + TerrainTable.Origin + field));

		/// <summary>The address of the name of the terrain type: 0x282 + 0x13 * terrain</summary>
		public ushort TerrainImprovementWord(int terrain, int field) =>
			Word((ushort)(TerrainImprovementTable.Stride * terrain + TerrainImprovementTable.Origin + field));

		/// <summary>A word field (LeaderTable) of the leader: 0x14e2 + 0x3a * leader + field</summary>
		public ushort LeaderWord(int leader, int field) => Word((ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field));

		/// <summary>A word field of the leader through the offset 0x3a * leader computed before</summary>
		public ushort LeaderWordAt(int offset, int field) => Word((ushort)(offset + LeaderTable.Origin + field));

		/// <summary>CivilizationName through the offset 2 * index computed before</summary>
		public ushort CivilizationNameAt(int offset) => Word((ushort)(offset + 0x1982));

		/// <summary>CivilizationPlural through the offset 2 * index computed before</summary>
		public ushort CivilizationPluralAt(int offset) => Word((ushort)(offset + 0x1992));

		/// <summary>LeaderName through the offset 2 * index computed before</summary>
		public ushort LeaderNameAt(int offset) => Word((ushort)(offset + 0x19a2));

		/// <summary>GovernmentTitle through the offset 2 * index computed before</summary>
		public ushort GovernmentTitleAt(int offset) => Word((ushort)(offset + 0x19b2));

		/// <summary>PlayerColour through the offset 2 * index computed before</summary>
		public ushort PlayerColourAt(int offset) => Word((ushort)(offset + 0x1946));

		/// <summary>PlayerSecondaryColour through the offset 2 * index computed before</summary>
		public ushort PlayerSecondaryColourAt(int offset) => Word((ushort)(offset + 0x1956));

		/// <summary>DirectionHoriz through the offset 2 * index computed before</summary>
		public ushort DirectionHorizAt(int offset) => Word((ushort)(offset + 0x1882));

		/// <summary>DirectionVert through the offset 2 * index computed before</summary>
		public ushort DirectionVertAt(int offset) => Word((ushort)(offset + 0x18e4));

		/// <summary>SpritePicture through the offset 2 * index computed before</summary>
		public ushort SpritePictureAt(int offset) => Word((ushort)(offset + 0xd4ce));

		/// <summary>0x10fe + 2 * wonder: the technology that makes the wonder obsolete (WonderObsoleteTable)</summary>
		public ushort WonderObsoleteTechnology(int wonder) => Word((ushort)((wonder << 1) + WonderObsoleteTable.Origin));

		/// <summary>0x1982 + 2 * index: the text of the name of the civilization of the player ("Babylonian")</summary>
		public ushort CivilizationName(int index) => Word((ushort)((index << 1) + 0x1982));

		/// <summary>0x1992 + 2 * index: the text of the plural name of the civilization of the player ("Babylonians")</summary>
		public ushort CivilizationPlural(int index) => Word((ushort)((index << 1) + 0x1992));

		/// <summary>0x19a2 + 2 * index: the text of the name of the leader of the player</summary>
		public ushort LeaderName(int index) => Word((ushort)((index << 1) + 0x19a2));

		/// <summary>0x1966 + 2 * index: the text of the name of the government</summary>
		public ushort GovernmentName(int index) => Word((ushort)((index << 1) + 0x1966));

		/// <summary>0x19b2 + 2 * index: the text of the title of the leader under the government</summary>
		public ushort GovernmentTitle(int index) => Word((ushort)((index << 1) + 0x19b2));

		/// <summary>0x1946 + 2 * index: the colour of the player</summary>
		public ushort PlayerColour(int index) => Word((ushort)((index << 1) + 0x1946));

		/// <summary>0x1882 + 2 * index: the x step of the direction (0 the tile itself, 1 - 8 the neighbours, then the second ring)</summary>
		public ushort DirectionHoriz(int index) => Word((ushort)((index << 1) + 0x1882));

		/// <summary>0x18e4 + 2 * index: the y step of the direction</summary>
		public ushort DirectionVert(int index) => Word((ushort)((index << 1) + 0x18e4));

		/// <summary>0xd4ce + 2 * index: the picture of the sprite (0x40 + 32 * player + unit kind: the units of the players)</summary>
		public ushort SpritePicture(int index) => Word((ushort)((index << 1) + 0xd4ce));
	}
}
