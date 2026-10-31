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
	}
}
