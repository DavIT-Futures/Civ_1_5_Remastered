using System;

namespace Civ_1_5_Remastered.Machine.Hosting
{
	public class ApplicationExitException : Exception
	{
		public ApplicationExitException()
			: base()
		{
		}

		public ApplicationExitException(string note)
			: base(note)
		{
		}

		public ApplicationExitException(string note, Exception internalFault)
			: base(note, internalFault)
		{
		}
	}
}
