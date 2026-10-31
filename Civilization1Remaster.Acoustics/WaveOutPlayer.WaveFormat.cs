using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Civilization1Remaster.Acoustics
{
	public partial class WaveOutPlayer
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct WaveFormat
		{
			public ushort LayoutLabel { get; set; }
			public ushort Lanes { get; set; }
			public uint SlicesEachOther { get; set; }
			public uint MeanOctetsEachOther { get; set; }
			public ushort ChunkSnap { get; set; }
			public ushort DigitsEachSlice { get; set; }
			public ushort AdditionalExtent { get; set; }
		}
	}
}
