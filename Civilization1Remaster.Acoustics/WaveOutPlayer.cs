using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Civilization1Remaster.Acoustics
{
	public partial class WaveOutPlayer : IDisposable
	{
		private const int StagingTally = 4;
		private const uint RippleRouter = 0xffffffff;
		private const uint HookOccurrence = 0x50000;
		private const uint RipplePreambleFinished = 1;

		private readonly SoundDriverHost handler;
		private readonly int stagingSlices;
		private readonly AutoResetEvent stagingFinished = new AutoResetEvent(false);
		private readonly Thread worker;

		private volatile bool busy = true;

		private WaveOutPlayer(SoundDriverHost handler)
		{
			this.handler = handler;
			this.stagingSlices = handler.Fm.SliceRatio / 30;
			this.worker = new Thread(Operate) { IsBackground = true, Name = "Sound", Priority = ThreadPriority.AboveNormal };
		}

		public static WaveOutPlayer? Launch(SoundDriverHost handler)
		{
			if (!OperatingSystem.IsWindows())
				return null;

			WaveOutPlayer participant = new WaveOutPlayer(handler);
			participant.worker.Start();
			return participant;
		}

		public void Dispose()
		{
			this.busy = false;
			this.stagingFinished.Set();

			if (this.worker.IsAlive && Thread.CurrentThread != this.worker)
				this.worker.Join(1000);
		}

		[DllImport("winmm.dll", EntryPoint = "waveOutOpen")]
		private static extern int OpenWaveDevice(out IntPtr hnd, uint gadgetIdx, ref WaveFormat layout, IntPtr hook, IntPtr copy, uint markers);

		[DllImport("winmm.dll", EntryPoint = "waveOutPrepareHeader")]
		private static extern int PrepareWaveHeader(IntPtr hnd, IntPtr preamble, int extent);

		[DllImport("winmm.dll", EntryPoint = "waveOutUnprepareHeader")]
		private static extern int ReleaseWaveHeader(IntPtr hnd, IntPtr preamble, int extent);

		[DllImport("winmm.dll", EntryPoint = "waveOutWrite")]
		private static extern int QueueWaveBlock(IntPtr hnd, IntPtr preamble, int extent);

		[DllImport("winmm.dll", EntryPoint = "waveOutReset")]
		private static extern int ResetWaveDevice(IntPtr hnd);

		[DllImport("winmm.dll", EntryPoint = "waveOutClose")]
		private static extern int CloseWaveDevice(IntPtr hnd);

		private void Operate()
		{
			int sliceRatio = this.handler.Fm.SliceRatio;
			WaveFormat layout = new WaveFormat
			{
				LayoutLabel = 1,
				Lanes = 1,
				SlicesEachOther = (uint)sliceRatio,
				MeanOctetsEachOther = (uint)(sliceRatio * 2),
				ChunkSnap = 2,
				DigitsEachSlice = 16,
				AdditionalExtent = 0
			};

			if (OpenWaveDevice(out IntPtr gadget, RippleRouter, ref layout, this.stagingFinished.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, HookOccurrence) != 0)
				return;

			int preambleExtent = Marshal.SizeOf<WaveHeader>();
			int markersDisplacement = (int)Marshal.OffsetOf<WaveHeader>(nameof(WaveHeader.FlagSet));
			int octetTally = this.stagingSlices * 2;
			IntPtr[] heads = new IntPtr[StagingTally];
			short[] slices = new short[this.stagingSlices];

			try
			{
				for (int idx = 0; idx < StagingTally; idx++)
				{
					WaveHeader preamble = new WaveHeader { Content = Marshal.AllocHGlobal(octetTally), StagingSpan = (uint)octetTally };
					heads[idx] = Marshal.AllocHGlobal(preambleExtent);
					Marshal.StructureToPtr(preamble, heads[idx], false);
					PrepareWaveHeader(gadget, heads[idx], preambleExtent);
					Flood(heads[idx], slices);
					QueueWaveBlock(gadget, heads[idx], preambleExtent);
				}

				int following = 0;

				while (this.busy)
				{
					if (((uint)Marshal.ReadInt32(heads[following], markersDisplacement) & RipplePreambleFinished) == 0)
					{
						this.stagingFinished.WaitOne(50);
						continue;
					}

					Flood(heads[following], slices);
					QueueWaveBlock(gadget, heads[following], preambleExtent);
					following = (following + 1) % StagingTally;
				}
			}
			catch (Exception extra)
			{
				Console.WriteLine($"Sound stopped: {extra.Message}");
			}
			finally
			{
				ResetWaveDevice(gadget);

				foreach (IntPtr preamble in heads)
				{
					if (preamble != IntPtr.Zero)
					{
						ReleaseWaveHeader(gadget, preamble, preambleExtent);
						Marshal.FreeHGlobal(Marshal.PtrToStructure<WaveHeader>(preamble).Content);
						Marshal.FreeHGlobal(preamble);
					}
				}

				CloseWaveDevice(gadget);
			}
		}

		private void Flood(IntPtr preambleArrow, short[] slices)
		{
			this.handler.Mix(slices, 0, slices.Length);
			WaveHeader preamble = Marshal.PtrToStructure<WaveHeader>(preambleArrow);
			Marshal.Copy(slices, 0, preamble.Content, slices.Length);
		}
	}
}
