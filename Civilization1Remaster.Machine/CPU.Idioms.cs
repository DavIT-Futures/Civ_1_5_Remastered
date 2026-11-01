using System;

namespace Civilization1Remaster.Machine
{
	public partial class CPU
	{
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
