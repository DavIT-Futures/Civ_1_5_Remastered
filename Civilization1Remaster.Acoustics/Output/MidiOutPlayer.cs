using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Civilization1Remaster.Acoustics.Output
{
	public class MidiOutPlayer : IDisposable
	{
		private const uint SynthesizerRouter = 0xffffffff;

		private readonly SoundDriverHost handler;
		private readonly IntPtr pointerGadget;
		private readonly Thread worker;

		private volatile bool busy = true;
		private bool shut = false;

		private MidiOutPlayer(SoundDriverHost handler, IntPtr gadget)
		{
			this.handler = handler;
			this.pointerGadget = gadget;
			this.worker = new Thread(Operate) { IsBackground = true, Name = "Music", Priority = ThreadPriority.AboveNormal };
		}

		public static MidiOutPlayer? Launch(SoundDriverHost handler)
		{
			if (!OperatingSystem.IsWindows())
				return null;

			if (OpenMidiDevice(out IntPtr gadget, SynthesizerRouter, IntPtr.Zero, IntPtr.Zero, 0) != 0 &&
				OpenMidiDevice(out gadget, 0, IntPtr.Zero, IntPtr.Zero, 0) != 0)
				return null;

			MidiOutPlayer participant = new MidiOutPlayer(handler, gadget);
			handler.Synthesizer.Emitted = participant.Dispatch;
			handler.Synthesizer.Restore();
			handler.Prepare();

			AppDomain.CurrentDomain.ProcessExit += (caller, arguments) => participant.Dispose();

			participant.worker.Start();
			return participant;
		}

		public void Dispose()
		{
			this.busy = false;

			if (this.worker.IsAlive && Thread.CurrentThread != this.worker)
				this.worker.Join(1000);

			Shut();
		}

		[DllImport("winmm.dll", EntryPoint = "midiOutOpen")]
		private static extern int OpenMidiDevice(out IntPtr handle, uint gadgetIndex, IntPtr hook, IntPtr copy, uint markers);

		[DllImport("winmm.dll", EntryPoint = "midiOutShortMsg")]
		private static extern int SendShortMessage(IntPtr handle, uint note);

		[DllImport("winmm.dll", EntryPoint = "midiOutReset")]
		private static extern int ResetMidiDevice(IntPtr handle);

		[DllImport("winmm.dll", EntryPoint = "midiOutClose")]
		private static extern int CloseMidiDevice(IntPtr handle);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint RequestTimerResolution(uint interval);

		[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
		private static extern uint ReleaseTimerResolution(uint interval);

		private void Dispatch(byte condition, byte firstContent, byte secondContent)
		{
			if (!this.shut)
				SendShortMessage(this.pointerGadget, (uint)(condition | (firstContent << 8) | (secondContent << 16)));
		}

		private void Operate()
		{
			RequestTimerResolution(1);

			try
			{
				Stopwatch timer = Stopwatch.StartNew();
				long beats = 0;

				while (this.busy)
				{
					long pending = beats * Stopwatch.Frequency / SoundDriverHost.BeatRatio;
					long presentAlternate = timer.ElapsedTicks;

					if (presentAlternate < pending)
					{
						Thread.Sleep(1);
						continue;
					}

					this.handler.Beat();
					beats++;

					if (presentAlternate - pending > Stopwatch.Frequency / 4)
						beats = presentAlternate * SoundDriverHost.BeatRatio / Stopwatch.Frequency;
				}
			}
			catch (Exception extra)
			{
				Console.WriteLine($"Music stopped: {extra.Message}");
			}
			finally
			{
				ReleaseTimerResolution(1);
				Shut();
			}
		}

		private void Shut()
		{
			lock (this.worker)
			{
				if (!this.shut)
				{
					this.shut = true;
					ResetMidiDevice(this.pointerGadget);
					CloseMidiDevice(this.pointerGadget);
				}
			}
		}
	}
}
