using System;

namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public class RolandToGeneralMidi
	{
		private const int DrumsTrack = 9;
		private const int TweakExtent = 8;
		private const int VoiceExtent = 0x100;

		private static readonly byte[] BuiltinToGeneralMidiMap =
		{
			0, 1, 0, 4, 5, 4, 5, 3, 16, 17, 18, 16, 19, 19, 19, 21,
			6, 6, 6, 7, 7, 7, 8, 8,
			62, 63, 62, 63, 38, 39, 38, 39,
			88, 89, 52, 98, 97, 99, 14, 54, 102, 96, 53, 102, 81, 100, 14, 80,
			48, 48, 49, 45, 40, 40, 42, 42, 43, 46, 46, 24, 25, 26, 27, 104,
			32, 32, 33, 34, 36, 37, 35, 35,
			73, 73, 72, 72, 74, 75, 64, 65, 66, 67, 71, 71, 68, 69, 70, 22,
			56, 56, 57, 57, 60, 60, 58, 61, 61,
			11, 11, 98, 14, 9, 14, 13, 12, 107, 107, 77, 78, 78, 76, 76,
			47, 117, 127, 118, 118, 116, 115, 119, 115, 112, 55, 124, 123, 0, 14, 117
		};

		private readonly byte[] tweakStorage = new byte[128 * TweakExtent];
		private readonly string[] voiceLabels = new string[64];
		private readonly int[] laneNoteShift = new int[16];
		private readonly int[] laneBendSpan = new int[16];

		private readonly byte[,] dispatchedTones = new byte[16, 128];

		private readonly byte[] note = new byte[3];
		private readonly byte[] systemExtra = new byte[1024];

		private int systemExtraSpan = -1;
		private int state = 0;
		private int noteSpan = 0;

		public RolandToGeneralMidi(Action<byte, byte, byte> emitted)
		{
			this.Emitted = emitted;

			for (int index = 0; index < 128; index++)
			{
				int displacement = index * TweakExtent;
				this.tweakStorage[displacement] = (byte)(index / 64);
				this.tweakStorage[displacement + 1] = (byte)(index % 64);
				this.tweakStorage[displacement + 2] = 24;
				this.tweakStorage[displacement + 3] = 50;
				this.tweakStorage[displacement + 4] = 12;
			}

			for (int index = 0; index < this.voiceLabels.Length; index++)
				this.voiceLabels[index] = "";

			for (int lane = 0; lane < 16; lane++)
			{
				this.laneNoteShift[lane] = 0;
				this.laneBendSpan[lane] = -1;

				for (int pitch = 0; pitch < 128; pitch++)
					this.dispatchedTones[lane, pitch] = (byte)pitch;
			}
		}

		public Action<byte, byte, byte> Emitted { get; set; }

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

		public void Emit(byte amount)
		{
			if (amount >= 0xf8)
			{
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

			this.note[this.noteSpan++] = amount;
			int sort = this.state & 0xf0;
			int wanted = sort == 0xc0 || sort == 0xd0 ? 1 : 2;

			if (this.noteSpan == wanted)
			{
				this.noteSpan = 0;
				ComposeMessage(this.state, this.note[0], this.note[1]);
			}
		}

		public int LookupGeneralMidiPatch(int tweak)
		{
			int displacement = (tweak & 0x7f) * TweakExtent;
			int bundle = this.tweakStorage[displacement];
			int numeral = this.tweakStorage[displacement + 1] & 0x3f;

			switch (bundle)
			{
				case 0:
					return BuiltinToGeneralMidiMap[numeral];
				case 1:
					return BuiltinToGeneralMidiMap[64 + numeral];
				case 2:
					return StorageTimbreToGeneralMidi(this.voiceLabels[numeral]);
				default:
					return 47;
			}
		}

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

		private void ComposeMessage(int condition, byte firstContent, byte secondContent)
		{
			int lane = condition & 0x0f;

			switch (condition & 0xf0)
			{
				case 0x80:
					Dispatch(condition, this.dispatchedTones[lane, firstContent], secondContent);
					break;

				case 0x90:
					if (secondContent == 0)
					{
						Dispatch(condition, this.dispatchedTones[lane, firstContent], 0);
					}
					else
					{
						int pitch = lane == DrumsTrack ? firstContent : Math.Clamp(firstContent + this.laneNoteShift[lane], 0, 127);
						this.dispatchedTones[lane, firstContent] = (byte)pitch;
						Dispatch(condition, pitch, secondContent);
					}
					break;

				case 0xb0:
					if (firstContent == 10)
						secondContent = (byte)(127 - secondContent);

					Dispatch(condition, firstContent, secondContent);
					break;

				case 0xc0:
					if (lane != DrumsTrack)
						SelectInstrument(lane, firstContent);
					break;

				default:
					Dispatch(condition, firstContent, secondContent);
					break;
			}
		}

		private void SelectInstrument(int lane, int tweak)
		{
			int displacement = tweak * TweakExtent;
			this.laneNoteShift[lane] = this.tweakStorage[displacement + 2] - 24;
			AssignBenderSpan(lane, Math.Clamp((int)this.tweakStorage[displacement + 4], 0, 24));
			Dispatch(0xc0 | lane, LookupGeneralMidiPatch(tweak), 0);
		}

		private void AssignBenderSpan(int lane, int spanAlternate)
		{
			if (this.laneBendSpan[lane] == spanAlternate)
				return;

			this.laneBendSpan[lane] = spanAlternate;

			Dispatch(0xb0 | lane, 101, 0);
			Dispatch(0xb0 | lane, 100, 0);
			Dispatch(0xb0 | lane, 6, spanAlternate);
			Dispatch(0xb0 | lane, 38, 0);
			Dispatch(0xb0 | lane, 101, 127);
			Dispatch(0xb0 | lane, 100, 127);
		}

		private void HandleSystemExclusive(byte[] content, int span)
		{
			if (span < 8 || content[0] != 0x41 || content[2] != 0x16 || content[3] != 0x12)
				return;

			int location = (content[4] << 14) | (content[5] << 7) | content[6];
			int contentBegin = 7;
			int contentSpan = span - 8;

			for (int index = 0; index < contentSpan; index++)
				EmitStorage(location + index, content[contentBegin + index]);
		}

		private void EmitStorage(int location, byte amount)
		{
			int zone = location >> 14;
			int displacement = location & 0x3fff;

			switch (zone)
			{
				case 0x05:
					if (displacement < this.tweakStorage.Length)
						this.tweakStorage[displacement] = amount;
					break;

				case 0x08:
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

		private void Dispatch(int condition, int firstContent, int secondContent)
		{
			this.Emitted((byte)condition, (byte)firstContent, (byte)secondContent);
		}
	}
}
