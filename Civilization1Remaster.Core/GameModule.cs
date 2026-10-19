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

		/// <summary>A message in the buffer 0xba06 that starts with the text</summary>
		protected GameMessage Message(string text) => GameMessage.Start(this.owner, text);

		/// <summary>A message in the buffer 0xba06 that starts with the text at the address</summary>
		protected GameMessage Message(ushort textAddr) => GameMessage.Start(this.owner, textAddr);

		/// <summary>A message in the buffer 0xba06 that starts empty</summary>
		protected GameMessage EmptyMessage() => GameMessage.Empty(this.owner);
	}
}
