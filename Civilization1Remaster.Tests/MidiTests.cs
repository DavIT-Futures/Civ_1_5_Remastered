using Civilization1Remaster.Acoustics;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class MidiTests
	{
		private static (Mt32ToGeneralMidi, List<(int, int, int)>) MakeTranslator()
		{
			List<(int, int, int)> dispatched = new List<(int, int, int)>();
			Mt32ToGeneralMidi synth = new Mt32ToGeneralMidi((condition, content1, content2) => dispatched.Add((condition, content1, content2)));
			return (synth, dispatched);
		}

		private static void Push(Mt32ToGeneralMidi synth, params int[] octets)
		{
			foreach (int amount in octets)
				synth.Emit((byte)amount);
		}

		/// <summary>
		/// MT-32 data set message with the Roland checksum
		/// </summary>
		private static int[] SystemExclusive(int location, params int[] content)
		{
			List<int> note = new List<int> { 0xf0, 0x41, 0x10, 0x16, 0x12, (location >> 14) & 0x7f, (location >> 7) & 0x7f, location & 0x7f };
			note.AddRange(content);
			int totalAlt = note.Skip(5).Sum();
			note.Add((128 - totalAlt % 128) % 128);
			note.Add(0xf7);
			return note.ToArray();
		}

		[Fact]
		public void BuiltInVoicesGetConverted()
		{
			(Mt32ToGeneralMidi synth, List<(int, int, int)> dispatched) = MakeTranslator();

			// MT-32 instrument 48 (Str Sect 1), 88 (Trumpet 1) and 64 (Acou Bass 1)
			Push(synth, 0xc1, 48, 0xc2, 88, 0xc3, 64);

			Assert.Contains((0xc1, 48, 0), dispatched);
			Assert.Contains((0xc2, 56, 0), dispatched);
			Assert.Contains((0xc3, 32, 0), dispatched);
		}

		[Fact]
		public void CustomPatchesAndVoicesGetUsed()
		{
			(Mt32ToGeneralMidi synth, List<(int, int, int)> dispatched) = MakeTranslator();

			// Patch 0: group B instrument 24 (Trumpet 1), key shift +12, bender range 2
			Push(synth, SystemExclusive(0x05 << 14, 1, 24, 36, 50, 2, 0, 1, 0));
			// Patch 1: memory instrument 0, named Explosion1
			Push(synth, SystemExclusive((0x05 << 14) + 8, 2, 0, 24, 50, 12, 0, 1, 0));
			Push(synth, SystemExclusive(0x08 << 14, "Explosion1".Select(chrAlt => (int)chrAlt).ToArray()));

			Push(synth, 0xc1, 0, 0x91, 60, 100, 60, 0);
			Assert.Contains((0xc1, 56, 0), dispatched);
			Assert.Contains((0xb1, 6, 2), dispatched);
			// Running status, the note off (velocity 0) goes to the same transposed note
			Assert.Contains((0x91, 72, 100), dispatched);
			Assert.Contains((0x91, 72, 0), dispatched);

			Push(synth, 0xc2, 1);
			Assert.Contains((0xc2, 127, 0), dispatched);
		}

		[Fact]
		public void DrumPartAndStereoPlacement()
		{
			(Mt32ToGeneralMidi synth, List<(int, int, int)> dispatched) = MakeTranslator();

			// Program changes on the rhythm part are not sent, drum notes are not transposed
			Push(synth, 0xc9, 5, 0x99, 36, 90, 0xb1, 10, 0);

			Assert.DoesNotContain(dispatched, note => note.Item1 == 0xc9);
			Assert.Contains((0x99, 36, 90), dispatched);
			Assert.Contains((0xb1, 10, 127), dispatched);
		}

		[Fact]
		public void HostRespondsToMidiCalls()
		{
			// Reset command to port 331h, then waits for the acknowledge byte FEh on port 330h
			X86Interpreter processor = new X86Interpreter();
			byte[] visual = new byte[0x100];
			visual[0x32] = 0x40;
			byte[] cipher =
			{
				0xba, 0x31, 0x03, // mov dx, 331h
				0xb0, 0xff,       // mov al, 0ffh
				0xee,             // out dx, al
				0xec,             // in al, dx       status: bit 7 clear when the acknowledge is waiting
				0x24, 0x80,       // and al, 80h
				0x75, 0xfb,       // jnz -5
				0x4a,             // dec dx
				0xec,             // in al, dx       acknowledge
				0x88, 0xc4,       // mov ah, al
				0x42,             // inc dx
				0xec,             // in al, dx       status: nothing more to read
				0xb2, 0x30,       // mov dl, 30h
				0x50,             // push ax
				0xb0, 0x90,       // mov al, 90h     note on, channel 1
				0xee,             // out dx, al
				0xb0, 0x3c,       // mov al, 60
				0xee,             // out dx, al
				0xb0, 0x40,       // mov al, 64
				0xee,             // out dx, al
				0x58,             // pop ax
				0xcb              // retf
			};
			Array.Copy(cipher, 0, visual, 0x40, cipher.Length);

			byte[] document = new byte[0x20 + visual.Length];
			document[0] = (byte)'M';
			document[1] = (byte)'Z';
			BitConverter.GetBytes((ushort)(document.Length % 512)).CopyTo(document, 2);
			BitConverter.GetBytes((ushort)((document.Length + 511) / 512)).CopyTo(document, 4);
			BitConverter.GetBytes((ushort)2).CopyTo(document, 8);
			BitConverter.GetBytes((ushort)0x1c).CopyTo(document, 0x18);
			visual.CopyTo(document, 0x20);

			SoundDriverHost server = new SoundDriverHost(document, 44100, "RSOUND.CVL");
			List<(int, int, int)> dispatched = new List<(int, int, int)>();
			server.Synth.Emitted = (condition, content1, content2) => dispatched.Add((condition, content1, content2));

			ushort outcome = server.Prepare();

			Assert.True(server.HasSynth);
			Assert.Equal(0xfe80, outcome);
			Assert.Equal(new[] { (0x90, 60, 64) }, dispatched);
		}

		[Fact]
		public void SortedDrivers()
		{
			Assert.Equal(new[] { "RSOUND.CVL", "ASOUND.CVL", "ISOUND.CVL" }, SoundDriverHost.DriverFileNames(null));
			Assert.Equal(new[] { "ASOUND.CVL" }, SoundDriverHost.DriverFileNames("AdLib"));
			Assert.Equal(new[] { "RSOUND.CVL" }, SoundDriverHost.DriverFileNames("mt32"));
			Assert.Empty(SoundDriverHost.DriverFileNames("none"));
		}
	}
}
