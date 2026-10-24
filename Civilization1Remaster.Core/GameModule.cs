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
		#endregion
	}
}
