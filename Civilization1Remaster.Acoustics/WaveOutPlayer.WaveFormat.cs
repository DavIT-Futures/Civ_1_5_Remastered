using System.Runtime.InteropServices;

namespace Civilization1Remaster.Acoustics
{
	public partial class WaveOutPlayer
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct WaveFormat
		{
			public ushort LayoutLabel;
			public ushort Lanes;
			public uint SlicesEachOther;
			public uint MeanOctetsEachOther;
			public ushort ChunkSnap;
			public ushort DigitsEachSlice;
			public ushort AdditionalExtent;
		}
	}
}
