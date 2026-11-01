using System;
using System.Runtime.InteropServices;

namespace Civ_1_5_Remastered.Acoustics.Output
{
	public partial class WaveOutPlayer
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct WaveHeader
		{
			public nint Content;
			public uint StagingSpan;
			public uint OctetsCaptured;
			public nint Person;
			public uint FlagSet;
			public uint Repeats;
			public nint Following;
			public nint Held;
		}
	}
}
