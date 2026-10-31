using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>
	/// Plays the Roland MT-32 music through the General MIDI synthesizer of Windows (winmm midiOut,
	/// usually the Microsoft GS Wavetable Synth or a better one chosen in Windows).
	/// The driver runs on its own background thread at the original game timer rate, the notes are sent as they are played.
	/// </summary>
	public class MidiOutPlayer : IDisposable
	{
		private const uint SynthRouter = 0xffffffff;

		private readonly SoundDriverHost handler;
		private readonly IntPtr ptrGadget;
		private readonly Thread worker;

		private volatile bool busy = true;
		private bool shut = false;

		private MidiOutPlayer(SoundDriverHost handler, IntPtr gadget)
		{
			this.handler = handler;
			this.ptrGadget = gadget;
			this.worker = new Thread(Operate) { IsBackground = true, Name = "Music", Priority = ThreadPriority.AboveNormal };
		}

		/// <summary>
		/// Opens the MIDI output, initializes the driver and starts playing.
		/// Returns null when there is no MIDI output (or when not running on Windows).
		/// </summary>
		public static MidiOutPlayer Launch(SoundDriverHost handler)
		{
			if (!OperatingSystem.IsWindows())
				return null;

			if (OpenMidiDevice(out IntPtr gadget, SynthRouter, IntPtr.Zero, IntPtr.Zero, 0) != 0 &&
				OpenMidiDevice(out gadget, 0, IntPtr.Zero, IntPtr.Zero, 0) != 0)
				return null;

			MidiOutPlayer participant = new MidiOutPlayer(handler, gadget);
			handler.Synth.Emitted = participant.Dispatch;
			handler.Synth.Restore();
			handler.Prepare();

			// Notes that are still playing would keep sounding after the game is closed
			AppDomain.CurrentDomain.ProcessExit += (caller, ev) => participant.Dispose();

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
		private static extern int OpenMidiDevice(out IntPtr hnd, uint gadgetIdx, IntPtr hook, IntPtr copy, uint markers);

		[DllImport("winmm.dll", EntryPoint = "midiOutShortMsg")]
		private static extern int SendShortMessage(IntPtr hnd, uint note);

		[DllImport("winmm.dll", EntryPoint = "midiOutReset")]
		private static extern int ResetMidiDevice(IntPtr hnd);

		[DllImport("winmm.dll", EntryPoint = "midiOutClose")]
		private static extern int CloseMidiDevice(IntPtr hnd);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		private static extern uint RequestTimerResolution(uint interval);

		[DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
		private static extern uint ReleaseTimerResolution(uint interval);

		private void Dispatch(byte condition, byte content1, byte content2)
		{
			if (!this.shut)
				SendShortMessage(this.ptrGadget, (uint)(condition | (content1 << 8) | (content2 << 16)));
		}

		private void Operate()
		{
			// The driver must run 300 times per second, the default Windows timer is too coarse for that
			RequestTimerResolution(1);

			try
			{
				Stopwatch timer = Stopwatch.StartNew();
				long beats = 0;

				while (this.busy)
				{
					long pending = beats * Stopwatch.Frequency / SoundDriverHost.BeatRatio;
					long presentAlt = timer.ElapsedTicks;

					if (presentAlt < pending)
					{
						Thread.Sleep(1);
						continue;
					}

					this.handler.Beat();
					beats++;

					// After a long pause (computer sleep, debugger) continue from now instead of catching up
					if (presentAlt - pending > Stopwatch.Frequency / 4)
						beats = presentAlt * SoundDriverHost.BeatRatio / Stopwatch.Frequency;
				}
			}
			catch (Exception extra)
			{
				// Sound is not essential, the game continues without it
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
					ResetMidiDevice(this.ptrGadget);
					CloseMidiDevice(this.ptrGadget);
				}
			}
		}
	}
}
