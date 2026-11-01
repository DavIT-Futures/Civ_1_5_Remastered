namespace Civilization1Remaster.Core.Engine
{
	public sealed class GameTables
	{
		public const ushort Begin = 0x282;

		public const ushort End = 0x19c2;

		private readonly GameEngine game;

		public GameTables(GameEngine game)
		{
			this.game = game;
		}

		public byte[] Content { get; } = new byte[End - Begin];

		public ushort Word(ushort address) => address >= Begin && address + 1 < End ?
			(ushort)(this.Content[address - Begin] | (this.Content[address - Begin + 1] << 8)) :
			this.game.Processor.PullGlobalWord(address);

		public byte Byte(ushort address) => address >= Begin && address < End ?
			this.Content[address - Begin] :
			this.game.Processor.PullGlobalByte(address);

		public static ushort UnitTextAt(int offset) => (ushort)(offset + UnitKindTable.Origin);

		public static ushort BuildingRecordText(int record) => (ushort)(BuildingTable.Stride * record + BuildingTable.Origin);

		public static ushort BuildingRecordTextAt(int offset) => (ushort)(offset + BuildingTable.Origin);

		public static ushort TerrainText(int terrain) => (ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin);

		public static ushort TerrainTextAt(int offset) => (ushort)(offset + TerrainTable.Origin);

		public static ushort LeaderText(int leader, int field) => (ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field);

		public static ushort LeaderTextAt(int offset, int field) => (ushort)(offset + LeaderTable.Origin + field);

		public static ushort TechnologyText(int index) => (ushort)(0x16 * index + 0x4da);

		public static ushort BuildingText(int index) => (ushort)(0x1e * index + 0xbb8);

		public static ushort WonderText(int index) => (ushort)(0x1e * index + 0xe6a);

		public static ushort UnitText(int index) => (ushort)(0x22 * index + 0x112a);

		public ushort UnitKindWord(int kind, int field) => Word((ushort)(UnitKindTable.Stride * kind + UnitKindTable.Origin + field));

		public ushort UnitKindWordAt(int offset, int field) => Word((ushort)(offset + UnitKindTable.Origin + field));

		public ushort BuildingWord(int record, int field) => Word((ushort)(BuildingTable.Stride * record + BuildingTable.Origin + field));

		public ushort BuildingWordAt(int offset, int field) => Word((ushort)(offset + BuildingTable.Origin + field));

		public byte TerrainField(int terrain, int field) => Byte((ushort)(TerrainTable.Stride * terrain + TerrainTable.Origin + field));

		public byte TerrainFieldAt(int offset, int field) => Byte((ushort)(offset + TerrainTable.Origin + field));

		public ushort TerrainImprovementWord(int terrain, int field) =>
			Word((ushort)(TerrainImprovementTable.Stride * terrain + TerrainImprovementTable.Origin + field));

		public ushort LeaderWord(int leader, int field) => Word((ushort)(LeaderTable.Stride * leader + LeaderTable.Origin + field));

		public ushort LeaderWordAt(int offset, int field) => Word((ushort)(offset + LeaderTable.Origin + field));

		public ushort CivilizationNameAt(int offset) => Word((ushort)(offset + 0x1982));

		public ushort CivilizationPluralAt(int offset) => Word((ushort)(offset + 0x1992));

		public ushort LeaderNameAt(int offset) => Word((ushort)(offset + 0x19a2));

		public ushort GovernmentTitleAt(int offset) => Word((ushort)(offset + 0x19b2));

		public ushort PlayerColourAt(int offset) => Word((ushort)(offset + 0x1946));

		public ushort PlayerSecondaryColourAt(int offset) => Word((ushort)(offset + 0x1956));

		public ushort DirectionHorizAt(int offset) => Word((ushort)(offset + 0x1882));

		public ushort DirectionVertAt(int offset) => Word((ushort)(offset + 0x18e4));

		public ushort SpritePictureAt(int offset) => Word((ushort)(offset + 0xd4ce));

		public ushort WonderObsoleteTechnology(int wonder) => Word((ushort)((wonder << 1) + WonderObsoleteTable.Origin));

		public ushort CivilizationName(int index) => Word((ushort)((index << 1) + 0x1982));

		public ushort CivilizationPlural(int index) => Word((ushort)((index << 1) + 0x1992));

		public ushort LeaderName(int index) => Word((ushort)((index << 1) + 0x19a2));

		public ushort GovernmentName(int index) => Word((ushort)((index << 1) + 0x1966));

		public ushort GovernmentTitle(int index) => Word((ushort)((index << 1) + 0x19b2));

		public ushort PlayerColour(int index) => Word((ushort)((index << 1) + 0x1946));

		public ushort DirectionHoriz(int index) => Word((ushort)((index << 1) + 0x1882));

		public ushort DirectionVert(int index) => Word((ushort)((index << 1) + 0x18e4));

		public ushort SpritePicture(int index) => Word((ushort)((index << 1) + 0xd4ce));
	}
}
