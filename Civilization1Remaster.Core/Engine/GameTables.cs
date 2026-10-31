using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// The tables of CIV.EXE in the data segment from 0x282 to 0x19c1 kept in C# (B6.3): terrain (0x282), technologies
	/// (0x4da), buildings and wonders (0xb9a), the technologies that make the wonders obsolete (0x10fe), unit types (0x112a,
	/// saved with the game), leaders (0x14e2), directions (0x1882 / 0x18e4), colours (0x1946), the texts of the
	/// governments, civilizations, leaders and titles (0x1966 - 0x19c1) and the few bytes between them. The accessors of
	/// GameComponent (B5) read them here, the rest of the code through the memory (the overlay).
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
	}
}
