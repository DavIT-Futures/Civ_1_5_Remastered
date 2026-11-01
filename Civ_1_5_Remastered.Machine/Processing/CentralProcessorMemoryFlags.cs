using System;

namespace Civ_1_5_Remastered.Machine.Processing
{
	[Flags]
	public enum CentralProcessorMemoryFlags
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
