using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Civ_1_5_Remastered.Acoustics.Output
{
	public class GeneralMidiPlayer : IDisposable
	{
		private const string SequencerAlias = "CivGeneralMidi";

		private readonly Func<string> folderSupplier;
		private readonly object synchronization = new object();

		private bool opened = false;
		private string? currentSong = null;

		public GeneralMidiPlayer(Func<string> folderSupplier)
		{
			this.folderSupplier = folderSupplier;
			AppDomain.CurrentDomain.ProcessExit += (sender, eventArguments) => Dispose();
		}

		public static string? SongFileName(ushort soundIdentifier) => soundIdentifier switch
		{
			3 => "opening_raw.mid",
			4 => "evolution_raw.mid",
			5 or 19 => "americans_raw.mid",
			6 or 20 => "aztecs_raw.mid",
			7 or 21 => "egyptians_raw.mid",
			8 or 22 => "zulus_raw.mid",
			9 or 23 => "french_raw.mid",
			10 or 24 => "romans_raw.mid",
			11 or 25 => "russians_raw.mid",
			12 or 26 => "greeks_raw.mid",
			13 or 27 => "english_raw.mid",
			14 or 28 => "babylonians_raw.mid",
			15 or 29 => "chinese_raw.mid",
			16 or 30 => "mongols_raw.mid",
			17 or 31 => "indian_raw.mid",
			18 or 32 => "german_raw.mid",
			33 => "poland_raw.mid",
			34 => "victory_raw.mid",
			35 => "defeat_raw.mid",
			36 => "japan_raw.mid",
			_ => null,
		};

		public void PerformSound(ushort soundIdentifier)
		{
			if (soundIdentifier <= 1)
			{
				Halt();
				return;
			}

			string? documentName = SongFileName(soundIdentifier);
			if (documentName == null)
			{
				return;
			}

			string? fullLocation = ResolveSongPath(documentName);
			if (fullLocation == null || !File.Exists(fullLocation))
			{
				return;
			}

			PlayFile(fullLocation);
		}

		public void PlayFile(string fullLocation)
		{
			if (!OperatingSystem.IsWindows())
			{
				return;
			}

			lock (this.synchronization)
			{
				CloseSequencer();

				string openInstruction = $"open \"{fullLocation}\" type sequencer alias {SequencerAlias}";
				int openOutcome = SendMediaControlString(openInstruction, null, 0, nint.Zero);
				if (openOutcome == 0)
				{
					this.opened = true;
					this.currentSong = fullLocation;
					string playInstruction = $"play {SequencerAlias} from 0";
					SendMediaControlString(playInstruction, null, 0, nint.Zero);
				}
			}
		}

		public void Halt()
		{
			if (!OperatingSystem.IsWindows())
			{
				return;
			}

			lock (this.synchronization)
			{
				CloseSequencer();
			}
		}

		public void Dispose()
		{
			Halt();
		}

		[DllImport("winmm.dll", EntryPoint = "mciSendStringW", CharSet = CharSet.Unicode)]
		private static extern int SendMediaControlString(string command, StringBuilder? returnString, int returnLength, nint callback);

		private void CloseSequencer()
		{
			if (this.opened)
			{
				SendMediaControlString($"stop {SequencerAlias}", null, 0, nint.Zero);
				SendMediaControlString($"close {SequencerAlias}", null, 0, nint.Zero);
				this.opened = false;
				this.currentSong = null;
			}
		}

		private string? ResolveSongPath(string documentName)
		{
			string gameFolder = this.folderSupplier();

			string[] candidateLocations = new[]
			{
				Path.Combine(gameFolder, "MIDI", documentName),
				Path.Combine(gameFolder, documentName),
				Path.Combine(AppContext.BaseDirectory, "Assets", "Midi", documentName),
				Path.Combine(AppContext.BaseDirectory, documentName),
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
	}
}
