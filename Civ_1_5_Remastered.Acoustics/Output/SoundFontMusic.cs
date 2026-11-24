using System;
using System.IO;
using MeltySynth;

namespace Civ_1_5_Remastered.Acoustics.Output
{
	// Plays the General MIDI songs through a SoundFont bank (GeneralUser GS) in stereo, with reverb and chorus,
	// and blends them with the sound effects the original driver makes.
	public class SoundFontMusic
	{
		public const string BankFileName = "GeneralUser-GS.sf2";

		private const float Loudness = 26000f;

		private readonly object latch = new object();
		private readonly MidiFileSequencer sequencer;

		private float[] left = Array.Empty<float>();
		private float[] right = Array.Empty<float>();
		private bool playing;

		public SoundFontMusic(string bankLocation, int sliceRatio)
		{
			SynthesizerSettings settings = new SynthesizerSettings(sliceRatio) { EnableReverbAndChorus = true };
			this.sequencer = new MidiFileSequencer(new Synthesizer(bankLocation, settings));
		}

		public bool IsPlaying
		{
			get
			{
				lock (this.latch)
				{
					return this.playing && !this.sequencer.EndOfSequence;
				}
			}
		}

		public static string? LocateBank(string gameFolder)
		{
			string[] candidateLocations = new[]
			{
				Path.Combine(gameFolder, "SoundFonts", BankFileName),
				Path.Combine(gameFolder, BankFileName),
				Path.Combine(AppContext.BaseDirectory, "Assets", "SoundFonts", BankFileName),
				Path.Combine(AppContext.BaseDirectory, BankFileName),
			};

			foreach (string candidate in candidateLocations)
			{
				if (File.Exists(candidate))
				{
					return Path.GetFullPath(candidate);
				}
			}

			return null;
		}

		public static SoundFontMusic? Open(string gameFolder, int sliceRatio)
		{
			string? bankLocation = LocateBank(string.IsNullOrEmpty(gameFolder) ? "." : gameFolder);
			if (bankLocation == null)
			{
				Console.WriteLine($"SoundFont {BankFileName} not found, the music falls back to the MIDI sequencer");
				return null;
			}

			try
			{
				return new SoundFontMusic(bankLocation, sliceRatio);
			}
			catch (Exception failure)
			{
				Console.WriteLine($"SoundFont {bankLocation} could not be loaded: {failure.Message}");
				return null;
			}
		}

		public void Play(string songLocation)
		{
			MidiFile song = new MidiFile(songLocation);

			lock (this.latch)
			{
				this.sequencer.Play(song, false);
				this.playing = true;
			}
		}

		public void Halt()
		{
			lock (this.latch)
			{
				if (this.playing)
				{
					this.sequencer.Stop();
					this.playing = false;
				}
			}
		}

		// Renders the next frames of music and mixes them with the mono effects into interleaved stereo.
		public void Blend(short[] effects, short[] stereo, int frames)
		{
			lock (this.latch)
			{
				if (this.left.Length < frames)
				{
					this.left = new float[frames];
					this.right = new float[frames];
				}

				Span<float> leftLane = this.left.AsSpan(0, frames);
				Span<float> rightLane = this.right.AsSpan(0, frames);

				if (this.playing)
				{
					this.sequencer.Render(leftLane, rightLane);
				}
				else
				{
					leftLane.Clear();
					rightLane.Clear();
				}

				for (int frame = 0; frame < frames; frame++)
				{
					stereo[frame * 2] = Clamp(effects[frame] + (this.left[frame] * Loudness));
					stereo[(frame * 2) + 1] = Clamp(effects[frame] + (this.right[frame] * Loudness));
				}
			}
		}

		private static short Clamp(float sample) => (short)Math.Clamp(sample, short.MinValue, short.MaxValue);
	}
}
