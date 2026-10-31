using System;

namespace Civilization1Remaster.Machine
{
	public class ApplicationExitException : Exception
	{
		public ApplicationExitException() : base() { }

		public ApplicationExitException(string note) : base(note) { }

		public ApplicationExitException(string note, Exception internalFault) : base(note, internalFault) { }
	}
}
