using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Civ_1_5_Remastered.Core.Engine.Session
{
	public static class GameFolderCheck
	{
		public const string FolderLabel = "CIVILIZATION";

		public static string[] RequiredFiles { get; } = new string[]
		{
			"ADSCREEN.PIC", "ARCH.PIC", "BACK0A.PIC", "BACK0M.PIC", "BACK1A.PIC", "BACK1M.PIC",
			"BACK2A.PIC", "BACK2M.PIC", "BACK3A.PIC", "BIRTH0.PIC", "BIRTH1.PIC", "BIRTH2.PIC",
			"BIRTH3.PIC", "BIRTH4.PIC", "BIRTH5.PIC", "BIRTH6.PIC", "BIRTH7.PIC", "BIRTH8.PIC",
			"CASTLE0.PIC", "CASTLE1.PIC", "CASTLE2.PIC", "CASTLE3.PIC", "CASTLE4.PIC", "CBACK.PIC",
			"CBACKS1.PIC", "CBACKS2.PIC", "CBACKS3.PIC", "CBRUSH0.PIC", "CBRUSH1.PIC", "CBRUSH2.PIC",
			"CBRUSH3.PIC", "CBRUSH4.PIC", "CBRUSH5.PIC", "CITYPIX1.PIC", "CITYPIX2.PIC", "CITYPIX3.PIC",
			"CUSTOM.PIC", "DIFFS.PIC", "DISCOVR1.PIC", "DISCOVR2.PIC", "DOCKER.PIC", "GOVT0A.PIC",
			"GOVT0M.PIC", "GOVT1A.PIC", "GOVT1M.PIC", "GOVT2A.PIC", "GOVT2M.PIC", "GOVT3A.PIC",
			"HILL.PIC", "ICONPG1.PIC", "ICONPG2.PIC", "ICONPG3.PIC", "ICONPG4.PIC", "ICONPG5.PIC",
			"ICONPG6.PIC", "ICONPG7.PIC", "ICONPG8.PIC", "ICONPGA.PIC", "ICONPGB.PIC", "ICONPGC.PIC",
			"ICONPGD.PIC", "ICONPGE.PIC", "ICONPGT1.PIC", "ICONPGT2.PIC", "INVADER2.PIC", "INVADER3.PIC",
			"INVADERS.PIC", "KING00.PIC", "KING01.PIC", "KING02.PIC", "KING03.PIC", "KING04.PIC",
			"KING05.PIC", "KING06.PIC", "KING07.PIC", "KING08.PIC", "KING09.PIC", "KING10.PIC",
			"KING11.PIC", "KING12.PIC", "KING13.PIC", "KINK00.PIC", "KINK03.PIC", "LOGO.PIC",
			"LOVE1.PIC", "LOVE2.PIC", "MAP.PIC", "NUKE1.PIC", "PLANET1.PIC", "PLANET2.PIC",
			"POP.PIC", "RIOT.PIC", "RIOT2.PIC", "SAD.PIC", "SETTLERS.PIC", "SLAG2.PIC",
			"SLAM1.PIC", "SLAM2.PIC", "SP257.PIC", "SP299.PIC", "SPACEST.PIC", "SPRITES.PIC",
			"TER257.PIC", "TORCH.PIC", "WONDERS.PIC", "WONDERS2.PIC", "BACK0A.PAL", "BACK0M.PAL",
			"BACK1A.PAL", "BACK1M.PAL", "BACK2A.PAL", "BACK2M.PAL", "BACK3A.PAL", "BIRTH0.PAL",
			"BIRTH1.PAL", "BIRTH2.PAL", "BIRTH3.PAL", "BIRTH4.PAL", "BIRTH5.PAL", "BIRTH6.PAL",
			"BIRTH7.PAL", "BIRTH8.PAL", "DISCOVR1.PAL", "DISCOVR2.PAL", "HILL.PAL", "ICONPG1.PAL",
			"ICONPGA.PAL", "KING00.PAL", "KING01.PAL", "KING02.PAL", "KING03.PAL", "KING04.PAL",
			"KING05.PAL", "KING06.PAL", "KING07.PAL", "KING08.PAL", "KING09.PAL", "KING10.PAL",
			"KING11.PAL", "KING12.PAL", "KING13.PAL", "SLAM1.PAL", "SP256.PAL", "SP257.PAL",
			"BLURB0.TXT", "BLURB1.TXT", "BLURB2.TXT", "BLURB3.TXT", "BLURB4.TXT", "CREDITS.TXT",
			"ERROR.TXT", "HELP.TXT", "INTRO.TXT", "INTRO3.TXT", "KING.TXT", "PRODUCE.TXT",
			"STORY.TXT", "CIV.EXE", "FONTS.CV"
		};

		public static bool Verify(out string message)
		{
			string? folder = OperatingSystemPaths.LocateGameDirectory(FolderLabel);
			List<string> candidates = new List<string>();

			if (folder != null)
			{
				candidates.Add(folder);
			}
			else
			{
				if (OperatingSystem.IsWindows())
				{
					candidates.Add(@"C:\Dos\Civ1\");
				}

				candidates.Add(AppContext.BaseDirectory);
			}

			List<string> missing = new List<string>();
			foreach (string candidate in candidates)
			{
				missing = FindMissing(candidate);
				if (missing.Count == 0)
				{
					message = Summarize(candidate);
					return true;
				}

				if (missing.Count < RequiredFiles.Length)
				{
					folder = candidate;
					break;
				}
			}

			StringBuilder text = new StringBuilder();
			string target = ExpectedLocation();

			if (folder == null || missing.Count == RequiredFiles.Length)
			{
				text.AppendLine("The files of the original Civilization 1 were not found.");
				text.AppendLine();
				text.AppendLine("Copy the installed DOS game into a folder named " + FolderLabel + " here:");
			}
			else
			{
				text.AppendLine("The folder " + folder + " does not contain the whole game.");
				text.AppendLine();
				text.AppendLine("Missing " + missing.Count + " of " + RequiredFiles.Length + " files, for example:");
				text.AppendLine("    " + string.Join(", ", missing.Take(8)) + (missing.Count > 8 ? ", ..." : string.Empty));
				text.AppendLine();
				text.AppendLine("Copy the whole installed DOS game into the folder " + FolderLabel + " here:");
				target = folder;
			}

			text.AppendLine();
			text.AppendLine("    " + target);
			text.AppendLine();
			text.AppendLine("The program also finds that folder in any folder above the program's own folder.");
			text.AppendLine();
			text.AppendLine("Copy the content of the game folder (the one with CIV.EXE), not the folder inside another folder.");
			text.AppendLine("The folder should hold about 170 files, plus your saved games:");
			text.AppendLine("  - CIV.EXE and the other programs (*.EXE)");
			text.AppendLine("  - about 107 pictures (*.PIC) and about 38 palettes (*.PAL)");
			text.AppendLine("  - the texts BLURB0-4, CREDITS, ERROR, HELP, INTRO, INTRO3, KING, PRODUCE, STORY (*.TXT)");
			text.AppendLine("  - the sound drivers ASOUND, ISOUND, NSOUND, RSOUND, TSOUND (*.CVL)");
			text.AppendLine("  - FONTS.CV, FAME.DTA, CHKLIST.CPS, READ.ME");
			text.AppendLine("  - your saved games, if you have them (*.SVE, *.MAP, *.HP)");
			text.AppendLine();
			text.AppendLine("File names must be in uppercase on systems that care about letter case.");
			message = text.ToString();
			return false;
		}

		private static string Summarize(string folder)
		{
			string[] files = Directory.GetFiles(folder);
			int Count(string extension) => files.Count(file => string.Equals(Path.GetExtension(file), extension, StringComparison.OrdinalIgnoreCase));
			bool Has(string name) => File.Exists(Path.Combine(folder, name));

			string[] drivers = new string[] { "ASOUND.CVL", "ISOUND.CVL", "NSOUND.CVL", "RSOUND.CVL", "TSOUND.CVL" };
			string[] driversFound = drivers.Where(Has).ToArray();

			StringBuilder text = new StringBuilder();
			text.AppendLine("The files of the original Civilization 1 were found. Everything is OK.");
			text.AppendLine();
			text.AppendLine("Folder:");
			text.AppendLine("    " + folder);
			text.AppendLine();
			text.AppendLine("Required files:  " + RequiredFiles.Length + " of " + RequiredFiles.Length + " present");
			text.AppendLine("Pictures (*.PIC):  " + Count(".PIC") + "     Palettes (*.PAL):  " + Count(".PAL") + "     Texts (*.TXT):  " + Count(".TXT"));
			text.AppendLine("Programs (*.EXE):  " + Count(".EXE") + "  (CIV.EXE is there)");
			text.AppendLine("Sound drivers (*.CVL):  " + driversFound.Length + " of " + drivers.Length +
				(driversFound.Length == drivers.Length ? string.Empty : "  (missing: " + string.Join(", ", drivers.Except(driversFound)) + ", their music will not play)"));
			text.AppendLine("Fonts, Hall of Fame:  FONTS.CV present, FAME.DTA " + (Has("FAME.DTA") ? "present" : "missing, it is created when needed"));
			text.AppendLine("Saved games:  " + Count(".SVE") + " (*.SVE, with " + Count(".MAP") + " maps)");
			text.AppendLine();
			text.AppendLine("Files in the folder:  " + files.Length);
			text.AppendLine();
			text.AppendLine("The game starts when you press OK.");
			return text.ToString();
		}

		private static List<string> FindMissing(string folder)
		{
			List<string> missing = new List<string>();

			if (!Directory.Exists(folder))
			{
				missing.AddRange(RequiredFiles);
				return missing;
			}

			foreach (string name in RequiredFiles)
			{
				if (!File.Exists(Path.Combine(folder, name)))
				{
					missing.Add(name);
				}
			}

			return missing;
		}

		private static string ExpectedLocation()
		{
			DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory);

			while (folder != null)
			{
				if (folder.GetFiles("*.sln").Length > 0 || folder.GetFiles("*.slnx").Length > 0)
				{
					return Path.Combine(folder.FullName, FolderLabel) + Path.DirectorySeparatorChar;
				}

				folder = folder.Parent;
			}

			return Path.Combine(AppContext.BaseDirectory, FolderLabel) + Path.DirectorySeparatorChar;
		}
	}
}
