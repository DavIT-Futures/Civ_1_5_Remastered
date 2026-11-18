namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	public sealed class GameTables
	{
		public const ushort Begin = 0x282;

		public const ushort End = 0x19c2;

		private readonly IGameEngine game;

		public GameTables(IGameEngine game)
		{
			this.game = game;
		}

		public byte[] Content { get; } = new byte[End - Begin];

		public static ushort UnitTextAt(int offset) => (ushort)(offset + UnitKindTable.Origin);

		public static ushort BuildingRecordText(int record) => (ushort)((BuildingTable.Stride * record) + BuildingTable.Origin);

		public static ushort BuildingRecordTextAt(int offset) => (ushort)(offset + BuildingTable.Origin);

		public static ushort TerrainText(int terrain) => (ushort)((TerrainTable.Stride * terrain) + TerrainTable.Origin);

		public static ushort TerrainTextAt(int offset) => (ushort)(offset + TerrainTable.Origin);

		public static ushort LeaderText(int leader, int field) => (ushort)((LeaderTable.Stride * leader) + LeaderTable.Origin + field);

		public static ushort LeaderTextAt(int offset, int field) => (ushort)(offset + LeaderTable.Origin + field);

		public static ushort TechnologyText(int index) => (ushort)((0x16 * index) + 0x4da);

		public static ushort BuildingText(int index) => (ushort)((0x1e * index) + 0xbb8);

		public static ushort WonderText(int index) => (ushort)((0x1e * index) + 0xe6a);

		public static ushort UnitText(int index) => (ushort)((0x22 * index) + 0x112a);

		public ushort Word(ushort address) => address >= Begin && address + 1 < End ? (ushort)(this.Content[address - Begin] | (this.Content[address - Begin + 1] << 8)) : this.game.Processor.PullGlobalWord(address);

		public byte Byte(ushort address) => address >= Begin && address < End ? this.Content[address - Begin] : this.game.Processor.PullGlobalByte(address);

		public ushort UnitKindWord(int kind, int field) =>
			ExtraUnitKinds.Contains(kind) ? ExtraUnitKinds.Word(kind, field) : Word((ushort)((UnitKindTable.Stride * kind) + UnitKindTable.Origin + field));

		/// <summary>A word of the unit record at <paramref name="offset"/> (the kind times the record size).</summary>
		public ushort UnitKindWordAt(int offset, int field) =>
			offset % UnitKindTable.Stride == 0 && ExtraUnitKinds.Contains(offset / UnitKindTable.Stride)
				? ExtraUnitKinds.Word(offset / UnitKindTable.Stride, field)
				: Word((ushort)(offset + UnitKindTable.Origin + field));

		/// <summary>The name of a unit kind, extra kinds included.</summary>
		public string UnitName(int kind)
		{
			if (ExtraUnitKinds.Contains(kind))
			{
				return ExtraUnitKinds.Name(kind);
			}

			System.Text.StringBuilder name = new System.Text.StringBuilder();

			for (int place = 0; place < ExtraUnitKinds.NameLength; place++)
			{
				byte letter = Byte((ushort)((UnitKindTable.Stride * kind) + UnitKindTable.Origin + place));

				if (letter == 0)
				{
					break;
				}

				name.Append((char)letter);
			}

			return name.ToString();
		}

		/// <summary>The price of a unit kind in shields (the table counts in tens, extra kinds may not).</summary>
		public int UnitShieldPrice(int kind) =>
			ExtraUnitKinds.Contains(kind) ? ExtraUnitKinds.ShieldPrice(kind) : 10 * (short)UnitKindWord(kind, UnitKindTable.Price);

		/// <summary>The cost of a unit kind for a city, <paramref name="costFactor"/> shields per ten shields of price.</summary>
		public int UnitCost(int kind, int costFactor) => UnitShieldPrice(kind) * costFactor / 10;

		public ushort BuildingWord(int record, int field) =>
			ExtraBuildings.Contains(record) ? ExtraBuildings.Word(record, field) : Word((ushort)((BuildingTable.Stride * record) + BuildingTable.Origin + field));

		/// <summary>A word of the improvement record at <paramref name="offset"/> (the number times the record size).</summary>
		public ushort BuildingWordAt(int offset, int field) =>
			offset % BuildingTable.Stride == 0 && ExtraBuildings.Contains(offset / BuildingTable.Stride)
				? ExtraBuildings.Word(offset / BuildingTable.Stride, field)
				: Word((ushort)(offset + BuildingTable.Origin + field));

		/// <summary>The name of a city improvement or wonder, extra improvements included.</summary>
		public string BuildingName(int building)
		{
			if (ExtraBuildings.Contains(building))
			{
				return ExtraBuildings.Name(building);
			}

			System.Text.StringBuilder name = new System.Text.StringBuilder();

			for (int place = 0; place < BuildingTable.Price; place++)
			{
				byte letter = Byte((ushort)((BuildingTable.Stride * building) + BuildingTable.Origin + place));

				if (letter == 0)
				{
					break;
				}

				name.Append((char)letter);
			}

			return name.ToString();
		}

		public byte TerrainField(int terrain, int field) => Byte((ushort)((TerrainTable.Stride * terrain) + TerrainTable.Origin + field));

		public byte TerrainFieldAt(int offset, int field) => Byte((ushort)(offset + TerrainTable.Origin + field));

		public ushort TerrainImprovementWord(int terrain, int field) =>
			Word((ushort)((TerrainImprovementTable.Stride * terrain) + TerrainImprovementTable.Origin + field));

		public ushort LeaderWord(int leader, int field)
		{
			if (leader == 16 || leader / LeaderTable.Stride == 16)
			{
				return (ushort)(field == LeaderTable.Tune || field == LeaderTable.ShortTune ? 100 : field == LeaderTable.Aggression ? 0 : 1);
			}

			if (leader == 17 || leader / LeaderTable.Stride == 17)
			{
				return (ushort)(field == LeaderTable.Tune || field == LeaderTable.ShortTune ? 101 : field == LeaderTable.Aggression ? 0 : 1);
			}

			return Word((ushort)((LeaderTable.Stride * leader) + LeaderTable.Origin + field));
		}

		public ushort LeaderWordAt(int offset, int field)
		{
			if (offset == 16 || offset / LeaderTable.Stride == 16)
			{
				return (ushort)(field == LeaderTable.Tune || field == LeaderTable.ShortTune ? 100 : field == LeaderTable.Aggression ? 0 : 1);
			}

			if (offset == 17 || offset / LeaderTable.Stride == 17)
			{
				return (ushort)(field == LeaderTable.Tune || field == LeaderTable.ShortTune ? 101 : field == LeaderTable.Aggression ? 0 : 1);
			}

			return Word((ushort)(offset + LeaderTable.Origin + field));
		}

		public ushort CivilizationNameAt(int offset) => Word((ushort)(offset + 0x1982));

		public ushort CivilizationPluralAt(int offset) => Word((ushort)(offset + 0x1992));

		public ushort LeaderNameAt(int offset) => Word((ushort)(offset + 0x19a2));

		public ushort GovernmentTitleAt(int offset) => Word((ushort)(offset + 0x19b2));

		public ushort PlayerColourAt(int offset) => Word((ushort)(offset + 0x1946));

		public ushort PlayerSecondaryColourAt(int offset) => Word((ushort)(offset + 0x1956));

		public ushort DirectionHorizontalAt(int offset) => Word((ushort)(offset + 0x1882));

		public ushort DirectionVerticalAt(int offset) => Word((ushort)(offset + 0x18e4));

		public ushort SpritePictureAt(int offset) => Word((ushort)(offset + 0xd4ce));

		public ushort CivilizationName(int index) => Word((ushort)((index << 1) + 0x1982));

		public ushort CivilizationPlural(int index) => Word((ushort)((index << 1) + 0x1992));

		public ushort LeaderName(int index) => Word((ushort)((index << 1) + 0x19a2));

		public ushort GovernmentName(int index) => Word((ushort)((index << 1) + 0x1966));

		public ushort GovernmentTitle(int index) => Word((ushort)((index << 1) + 0x19b2));

		public ushort PlayerColour(int index) => Word((ushort)((index << 1) + 0x1946));

		public ushort DirectionHorizontal(int index) => Word((ushort)((index << 1) + 0x1882));

		public ushort DirectionVertical(int index) => Word((ushort)((index << 1) + 0x18e4));

		public ushort SpritePicture(int index) => Word((ushort)((index << 1) + 0xd4ce));
	}
}
