using System;
using System.Runtime.InteropServices;

namespace Civ_1_5_Remastered.Acoustics.Output
{
	public partial class WaveOutPlayer
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct WaveHeader
		{
			public IntPtr Content;
			public uint StagingSpan;
			public uint OctetsCaptured;
			public IntPtr Person;
			public uint FlagSet;
			public uint Repeats;
			public IntPtr Following;
			public IntPtr Held;
		}
	}
}
