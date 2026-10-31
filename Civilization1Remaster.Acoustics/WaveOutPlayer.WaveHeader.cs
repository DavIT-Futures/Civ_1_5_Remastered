using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Civilization1Remaster.Acoustics
{
	public partial class WaveOutPlayer
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct WaveHeader
		{
			public IntPtr Content { get; set; }
			public uint StagingSpan { get; set; }
			public uint OctetsCaptured { get; set; }
			public IntPtr Person { get; set; }
			public uint FlagSet { get; set; }
			public uint Repeats { get; set; }
			public IntPtr Following { get; set; }
			public IntPtr Held { get; set; }
		}
	}
}
