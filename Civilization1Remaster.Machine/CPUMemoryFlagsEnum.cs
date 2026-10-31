using System;

namespace Civilization1Remaster.Machine
{
	[Flags]
	public enum CPUMemoryFlagsEnum
	{
		None = 0,
		Write = 1,
		Read = 2,
		ReadWrite = 3,
		ReadWarning = 4,
		WriteWarning = 8,
		AccessNotAllowed = 16
	}
}
