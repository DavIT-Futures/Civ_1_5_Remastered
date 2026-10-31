using System;

namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// Short sequences of instructions that the translated program uses over and over, with names that say what they do.
	/// </summary>
	public partial class CPU
	{
		/// <summary>
		/// The quotient of a signed division of words (cwd; idiv), rounded towards zero, with the exceptions of DivideSignedWord:
		/// a division by zero and a quotient that is not a word (-32768 / -1)
		/// </summary>
		public static short SignedQuotient(short dividend, short divisor)
		{
			if (divisor == 0)
				throw new Exception("Division by zero");

			int quotient = dividend / divisor;

			if (quotient != (short)quotient)
				throw new Exception("Division error");

			return (short)quotient;
		}

		/// <summary>Saves the destination and source index registers on the stack (push di; push si).</summary>
		public void PreserveIndexRegisters()
		{
			StackWord(this.regDest.Value16);
			StackWord(this.regSource.Value16);
		}

		/// <summary>Takes the registers saved by PreserveIndexRegisters back from the stack, in the reverse order (pop si; pop di).</summary>
		public void RestoreIndexRegisters()
		{
			this.regSource.Value16 = UnstackWord();
			this.regDest.Value16 = UnstackWord();
		}

	}
}
