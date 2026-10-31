using System;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>
	/// Translates the MIDI stream of the Roland MT-32 sound driver (RSOUND.CVL) into General MIDI,
	/// so it can be played by any General MIDI synthesizer, like the one built into Windows.
	///
	/// The MT-32 has its own instrument numbers and the driver uploads its own patches and a few sound effect
	/// instruments with system exclusive messages. The translator keeps these patches and translates every
	/// program change to the General MIDI instrument that sounds most like the MT-32 one.
	/// It also applies the patch key shift and bender range and reverses the panning, which is reversed on the MT-32.
	/// </summary>
	public class Mt32ToGeneralMidi
	{
		private const int DrumsTrack = 9;
		private const int TweakExtent = 8;
		private const int VoiceExtent = 0x100;

		// General MIDI instrument (0 - 127) for each of the 128 MT-32 preset instruments (timbre group A and B)
		private static readonly byte[] BuiltinToGmMap =
		{
			// Pianos, organs, harpsichords, clavinets, celestas
			0, 1, 0, 4, 5, 4, 5, 3, 16, 17, 18, 16, 19, 19, 19, 21,
			6, 6, 6, 7, 7, 7, 8, 8,
			// Synth brass and bass
			62, 63, 62, 63, 38, 39, 38, 39,
			// Synth effects: Fantasy, Harmo Pan, Chorale, Glasses, Soundtrack, Atmosphere, Warm Bell, Funny Vox,
			// Echo Bell, Ice Rain, Oboe 2001, Echo Pan, Doctor Solo, Schooldaze, Bellsinger, Square Wave
			88, 89, 52, 98, 97, 99, 14, 54, 102, 96, 53, 102, 81, 100, 14, 80,
			// Strings, harps, guitars, sitar
			48, 48, 49, 45, 40, 40, 42, 42, 43, 46, 46, 24, 25, 26, 27, 104,
			// Basses
			32, 32, 33, 34, 36, 37, 35, 35,
			// Flutes, recorder, pan pipes, saxophones, clarinets, oboe, english horn, bassoon, harmonica
			73, 73, 72, 72, 74, 75, 64, 65, 66, 67, 71, 71, 68, 69, 70, 22,
			// Trumpets, trombones, french horns, tuba, brass sections
			56, 56, 57, 57, 60, 60, 58, 61, 61,
			// Vibraphones, mallets, bells, xylophone, marimba, koto, sho, shakuhachi, whistles, bottle, breath pipe
			11, 11, 98, 14, 9, 14, 13, 12, 107, 107, 77, 78, 78, 76, 76,
			// Percussion and effects: Timpani, Melodic Tom, Deep Snare, Elec Perc 1 and 2, Taiko, Taiko Rim, Cymbal,
			// Castanets, Triangle, Orchestra Hit, Telephone, Bird Tweet, One Note Jam, Water Bells, Jungle Tune
			47, 117, 127, 118, 118, 116, 115, 119, 115, 112, 55, 124, 123, 0, 14, 117
		};

		private readonly byte[] tweakStorage = new byte[128 * TweakExtent];
		private readonly string[] voiceLabels = new string[64];
		private readonly int[] laneNoteShift = new int[16];
		private readonly int[] laneBendSpan = new int[16];

		// Note that was sent for each received note, so the note off matches even when the key shift changes
		private readonly byte[,] dispatchedTones = new byte[16, 128];

		private readonly byte[] note = new byte[3];
		private readonly byte[] systemExtra = new byte[1024];

		private int systemExtraSpan = -1;
		private int state = 0;
		private int noteSpan = 0;

		/// <param name="output">Receives the General MIDI short messages: status, first data byte, second data byte</param>
		public Mt32ToGeneralMidi(Action<byte, byte, byte> emitted)
		{
			this.Emitted = emitted;

			// MT-32 default patches: patch N plays preset instrument N, without key shift, bender range 12
			for (int idx = 0; idx < 128; idx++)
			{
				int displacement = idx * TweakExtent;
				this.tweakStorage[displacement] = (byte)(idx / 64);
				this.tweakStorage[displacement + 1] = (byte)(idx % 64);
				this.tweakStorage[displacement + 2] = 24;
				this.tweakStorage[displacement + 3] = 50;
				this.tweakStorage[displacement + 4] = 12;
			}

			for (int idx = 0; idx < this.voiceLabels.Length; idx++)
				this.voiceLabels[idx] = "";

			for (int lane = 0; lane < 16; lane++)
			{
				this.laneNoteShift[lane] = 0;
				this.laneBendSpan[lane] = -1;

				for (int pitch = 0; pitch < 128; pitch++)
					this.dispatchedTones[lane, pitch] = (byte)pitch;
			}
		}

		public Action<byte, byte, byte> Emitted { get; set; }

		/// <summary>
		/// Prepares the General MIDI synthesizer to sound like a freshly initialized MT-32
		/// </summary>
		public void Restore()
		{
			for (int lane = 0; lane < 16; lane++)
			{
				Dispatch(0xb0 | lane, 121, 0);
				Dispatch(0xb0 | lane, 123, 0);
				this.laneBendSpan[lane] = -1;

				if (lane != DrumsTrack)
					AssignBenderSpan(lane, 12);
			}
		}

		/// <summary>
		/// Receives one byte that the driver writes to the MIDI data port
		/// </summary>
		public void Emit(byte amount)
		{
			if (amount >= 0xf8)
			{
				// Real time messages can come anywhere and are not needed
				return;
			}

			if (amount == 0xf0)
			{
				this.systemExtraSpan = 0;
				return;
			}

			if (this.systemExtraSpan >= 0)
			{
				if (amount == 0xf7)
				{
					HandleSystemExclusive(this.systemExtra, this.systemExtraSpan);
					this.systemExtraSpan = -1;
				}
				else if (amount < 0x80)
				{
					if (this.systemExtraSpan < this.systemExtra.Length)
						this.systemExtra[this.systemExtraSpan++] = amount;
				}
				else
				{
					// Unterminated system exclusive message, the new status starts a new message
					this.systemExtraSpan = -1;
					Emit(amount);
				}

				return;
			}

			if (amount >= 0x80)
			{
				this.state = amount < 0xf0 ? amount : 0;
				this.noteSpan = 0;
				return;
			}

			if (this.state == 0)
				return;

			// Data byte, with running status
			this.note[this.noteSpan++] = amount;
			int sort = this.state & 0xf0;
			int wanted = sort == 0xc0 || sort == 0xd0 ? 1 : 2;

			if (this.noteSpan == wanted)
			{
				this.noteSpan = 0;
				ComposeMessage(this.state, this.note[0], this.note[1]);
			}
		}

		/// <summary>
		/// The General MIDI instrument used for an MT-32 patch
		/// </summary>
		public int LookupGmPatch(int tweak)
		{
			int displacement = (tweak & 0x7f) * TweakExtent;
			int bundle = this.tweakStorage[displacement];
			int numeral = this.tweakStorage[displacement + 1] & 0x3f;

			switch (bundle)
			{
				case 0:
					return BuiltinToGmMap[numeral];
				case 1:
					return BuiltinToGmMap[64 + numeral];
				case 2:
					return StorageTimbreToGeneralMidi(this.voiceLabels[numeral]);
				default:
					// Rhythm timbres, not used by the driver for melodic parts
					return 47;
			}
		}

		/// <summary>
		/// Instruments that the driver uploads are matched by name
		/// </summary>
		private static int StorageTimbreToGeneralMidi(string label)
		{
			label = label.ToLowerInvariant();

			if (label.Contains("explosion") || label.Contains("gun") || label.Contains("cannon"))
				return 127;
			if (label.Contains("sword") || label.Contains("swish"))
				return 119;
			if (label.Contains("bongo") || label.Contains("conga") || label.Contains("drum"))
				return 116;
			if (label.Contains("gourd") || label.Contains("block"))
				return 115;

			return 0;
		}

		private void ComposeMessage(int condition, byte content1, byte content2)
		{
			int lane = condition & 0x0f;

			switch (condition & 0xf0)
			{
				case 0x80:
					Dispatch(condition, this.dispatchedTones[lane, content1], content2);
					break;

				case 0x90:
					if (content2 == 0)
					{
						Dispatch(condition, this.dispatchedTones[lane, content1], 0);
					}
					else
					{
						int pitch = lane == DrumsTrack ? content1 : Math.Clamp(content1 + this.laneNoteShift[lane], 0, 127);
						this.dispatchedTones[lane, content1] = (byte)pitch;
						Dispatch(condition, pitch, content2);
					}
					break;

				case 0xb0:
					// MT-32 panning goes from right to left, General MIDI from left to right
					if (content1 == 10)
						content2 = (byte)(127 - content2);

					Dispatch(condition, content1, content2);
					break;

				case 0xc0:
					// The rhythm part has no instruments on the MT-32, on General MIDI it would change the drum kit
					if (lane != DrumsTrack)
						SelectInstrument(lane, content1);
					break;

				default:
					Dispatch(condition, content1, content2);
					break;
			}
		}

		private void SelectInstrument(int lane, int tweak)
		{
			int displacement = tweak * TweakExtent;
			this.laneNoteShift[lane] = this.tweakStorage[displacement + 2] - 24;
			AssignBenderSpan(lane, Math.Clamp((int)this.tweakStorage[displacement + 4], 0, 24));
			Dispatch(0xc0 | lane, LookupGmPatch(tweak), 0);
		}

		private void AssignBenderSpan(int lane, int spanAlt)
		{
			if (this.laneBendSpan[lane] == spanAlt)
				return;

			this.laneBendSpan[lane] = spanAlt;

			// Registered parameter 0 is the pitch bend sensitivity
			Dispatch(0xb0 | lane, 101, 0);
			Dispatch(0xb0 | lane, 100, 0);
			Dispatch(0xb0 | lane, 6, spanAlt);
			Dispatch(0xb0 | lane, 38, 0);
			Dispatch(0xb0 | lane, 101, 127);
			Dispatch(0xb0 | lane, 100, 127);
		}

		private void HandleSystemExclusive(byte[] content, int span)
		{
			// Roland (0x41), device 0x10, MT-32 (0x16), data set (0x12), 3 address bytes, data, checksum
			if (span < 8 || content[0] != 0x41 || content[2] != 0x16 || content[3] != 0x12)
				return;

			int location = (content[4] << 14) | (content[5] << 7) | content[6];
			int contentBegin = 7;
			int contentSpan = span - 8;

			for (int idx = 0; idx < contentSpan; idx++)
				EmitStorage(location + idx, content[contentBegin + idx]);
		}

		private void EmitStorage(int location, byte amount)
		{
			int zone = location >> 14;
			int displacement = location & 0x3fff;

			switch (zone)
			{
				case 0x05:
					// Patch memory
					if (displacement < this.tweakStorage.Length)
						this.tweakStorage[displacement] = amount;
					break;

				case 0x08:
					// Timbre memory, the first 10 bytes of each timbre are its name
					int voice = displacement / VoiceExtent;
					int place = displacement % VoiceExtent;

					if (voice < this.voiceLabels.Length && place < 10)
					{
						char[] label = this.voiceLabels[voice].PadRight(10).ToCharArray();
						label[place] = amount >= 0x20 ? (char)amount : ' ';
						this.voiceLabels[voice] = new string(label).TrimEnd();
					}
					break;
			}
		}

		private void Dispatch(int condition, int content1, int content2)
		{
			this.Emitted((byte)condition, (byte)content1, (byte)content2);
		}
	}
}
