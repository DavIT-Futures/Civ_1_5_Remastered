using Civilization1Remaster.Acoustics;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class SoundTests
	{
		private const int SliceRatio = 44100;

		private static int NumberOfZeroCrossings(short[] slices, int begin)
		{
			int tally = 0;

			for (int idx = begin + 1; idx < slices.Length; idx++)
			{
				if ((slices[idx - 1] < 0) != (slices[idx] < 0))
					tally++;
			}

			return tally;
		}

		[Fact]
		public void EmulatorExecutesCodeAndLeavesFarCall()
		{
			X86Interpreter processor = new X86Interpreter();
			byte[] cipher =
			{
				0x55,             // push bp
				0x89, 0xe5,       // mov bp, sp
				0x8b, 0x46, 0x06, // mov ax, [bp+6]   first parameter
				0x03, 0x46, 0x08, // add ax, [bp+8]   second parameter
				0xb9, 0x03, 0x00, // mov cx, 3
				0xd1, 0xe0,       // shl ax, 1
				0xe2, 0xfc,       // loop -4
				0x5d,             // pop bp
				0xcb              // retf
			};
			Array.Copy(cipher, 0, processor.Storage, 0x20000, cipher.Length);
			processor.SegStack = 0x9000;
			processor.RegStack = 0xfff0;

			processor.InvokeFar(0x2000, 0, 5, 7);

			Assert.Equal((5 + 7) * 8, processor.RegAcc);
			Assert.Equal(0xfff0, processor.RegStack);
		}

		[Fact]
		public void CpuEmulatorForwardsPortOutput()
		{
			X86Interpreter processor = new X86Interpreter();
			List<(ushort, byte)> emits = new List<(ushort, byte)>();
			processor.GateEmit = (gate, amount) => emits.Add((gate, amount));
			byte[] cipher =
			{
				0xba, 0x88, 0x03, // mov dx, 388h
				0xb0, 0xbd,       // mov al, 0bdh
				0xee,             // out dx, al
				0x42,             // inc dx
				0xb0, 0x20,       // mov al, 20h
				0xee,             // out dx, al
				0xcb              // retf
			};
			Array.Copy(cipher, 0, processor.Storage, 0x20000, cipher.Length);
			processor.SegStack = 0x9000;
			processor.RegStack = 0xfff0;

			processor.InvokeFar(0x2000, 0);

			Assert.Equal(new[] { ((ushort)0x388, (byte)0xbd), ((ushort)0x389, (byte)0x20) }, emits);
		}

		[Fact]
		public void OplTimerGetsRecognised()
		{
			// The usual AdLib detection: reset the timers, start timer 1, check the status flags
			Opl2Synth fm = new Opl2Synth(SliceRatio);
			fm.EmitRegister(0x04, 0x60);
			fm.EmitRegister(0x04, 0x80);
			Assert.Equal(0x00, fm.PullStatus() & 0xe0);

			fm.EmitRegister(0x02, 0xff);
			fm.EmitRegister(0x04, 0x21);
			Assert.Equal(0xc0, fm.PullStatus() & 0xe0);
		}

		[Fact]
		public void OplStaysQuietWithoutNotes()
		{
			Opl2Synth fm = new Opl2Synth(SliceRatio);
			short[] slices = new short[SliceRatio / 10];

			fm.Produce(slices, 0, slices.Length);

			Assert.All(slices, slice => Assert.Equal(0, slice));
		}

		[Fact]
		public void OplSoundsNoteAtCorrectFrequency()
		{
			Opl2Synth fm = new Opl2Synth(SliceRatio);
			// Channel 0: silent modulator, loud sustained carrier, sine wave
			fm.EmitRegister(0x20, 0x21);
			fm.EmitRegister(0x23, 0x21);
			fm.EmitRegister(0x40, 0x3f);
			fm.EmitRegister(0x43, 0x00);
			fm.EmitRegister(0x60, 0xf0);
			fm.EmitRegister(0x63, 0xf0);
			fm.EmitRegister(0x80, 0x0f);
			fm.EmitRegister(0x83, 0x0f);
			fm.EmitRegister(0xc0, 0x00);
			// 440 Hz: block 4, F-number 580, key on
			fm.EmitRegister(0xa0, 580 & 0xff);
			fm.EmitRegister(0xb0, 0x20 | (4 << 2) | (580 >> 8));
			short[] slices = new short[SliceRatio];

			fm.Produce(slices, 0, slices.Length);

			// A sine wave crosses zero twice per period
			Assert.InRange(NumberOfZeroCrossings(slices, 0), 860, 900);
			Assert.True(slices.Max(slice => Math.Abs((int)slice)) > 1000);
		}

		[Fact]
		public void SpeakerSoundsTimerFrequency()
		{
			PcSpeaker buzzer = new PcSpeaker(SliceRatio);
			int denominator = 1193182 / 440;
			buzzer.EmitPort(0x43, 0xb6);
			buzzer.EmitPort(0x42, (byte)(denominator & 0xff));
			buzzer.EmitPort(0x42, (byte)(denominator >> 8));
			buzzer.EmitPort(0x61, 0x03);
			short[] slices = new short[SliceRatio];

			buzzer.Produce(slices, 0, slices.Length);

			Assert.InRange(NumberOfZeroCrossings(slices, 0), 870, 890);

			buzzer.EmitPort(0x61, 0x00);
			short[] quiet = new short[SliceRatio / 10];
			buzzer.Produce(quiet, 0, quiet.Length);

			Assert.True(quiet.Skip(100).All(slice => slice == 0));
		}

		/// <summary>
		/// A tiny driver with the same layout as the game drivers: a DOS executable with
		/// a table of 6 entry points at offset 0x32
		/// </summary>
		private static byte[] MakeProbeDriver()
		{
			byte[] visual = new byte[0x100];
			ushort[] records = { 0x40, 0x50, 0x52, 0x60, 0x66, 0x6c };

			for (int idx = 0; idx < records.Length; idx++)
			{
				visual[0x32 + idx * 2] = (byte)records[idx];
			}

			byte[][] cipher =
			{
				// 0: initialize, writes one AdLib register and returns 0
				new byte[] { 0xba, 0x88, 0x03, 0xb0, 0x01, 0xee, 0x42, 0xb0, 0x20, 0xee, 0x31, 0xc0, 0xcb },
				// 1: play, does nothing
				new byte[] { 0xcb },
				// 2: stop, returns 0x1234
				new byte[] { 0xb8, 0x34, 0x12, 0xcb },
				// 3: step, counts the calls at 0xf0
				new byte[] { 0x2e, 0xff, 0x06, 0xf0, 0x00, 0xcb },
				// 4: modulate, counts the calls at 0xf2
				new byte[] { 0x2e, 0xff, 0x06, 0xf2, 0x00, 0xcb },
				// 5: position, returns the step count
				new byte[] { 0x2e, 0xa1, 0xf0, 0x00, 0xcb }
			};

			for (int idx = 0; idx < cipher.Length; idx++)
			{
				Array.Copy(cipher[idx], 0, visual, records[idx], cipher[idx].Length);
			}

			// DOS executable header of 2 paragraphs, no relocations
			byte[] document = new byte[0x20 + visual.Length];
			document[0] = (byte)'M';
			document[1] = (byte)'Z';
			BitConverter.GetBytes((ushort)(document.Length % 512)).CopyTo(document, 2);
			BitConverter.GetBytes((ushort)((document.Length + 511) / 512)).CopyTo(document, 4);
			BitConverter.GetBytes((ushort)2).CopyTo(document, 8);
			BitConverter.GetBytes((ushort)0x1c).CopyTo(document, 0x18);
			visual.CopyTo(document, 0x20);

			return document;
		}

		[Fact]
		public void HostInvokesDriverEntries()
		{
			SoundDriverHost server = new SoundDriverHost(MakeProbeDriver(), SliceRatio, "TEST");

			Assert.Equal(0, server.Prepare());
			Assert.Equal(1, server.Fm.LatchEmitTally);
			server.Perform(3, 0);
			Assert.Equal(0x1234, server.Halt());
		}

		[Fact]
		public void HostDrivesDriverAtGameClockRate()
		{
			SoundDriverHost server = new SoundDriverHost(MakeProbeDriver(), SliceRatio, "TEST");
			server.Prepare();
			short[] staging = new short[SliceRatio / 30];

			// Two seconds of audio in buffers of an odd size
			for (int idx = 0; idx < 60; idx++)
			{
				server.Mix(staging, 0, staging.Length);
			}

			Assert.Equal(2 * SoundDriverHost.AdvanceRatio, server.CurrentOffset());
		}
	}
}
