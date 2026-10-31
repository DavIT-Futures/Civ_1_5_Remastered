namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// Finds the original game files (CIVILIZATION/ next to the solution) and a scratch folder inside the test output.
	/// Tests only read the game files; anything they write goes to the scratch folder.
	/// </summary>
	internal static class GameFiles
	{
		private static readonly Lazy<string?> GameFolder = new Lazy<string?>(() =>
		{
			for (DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
			{
				if (File.Exists(Path.Combine(folder.FullName, "Civilization1Remaster.sln")))
				{
					string games = Path.Combine(folder.FullName, "CIVILIZATION");
					return Directory.Exists(games) ? games : null;
				}
			}

			return null;
		});

		/// <summary>
		/// The CIVILIZATION folder, or null when the game files are not present (tests that need them then pass without checking).
		/// </summary>
		public static string? Folder => GameFolder.Value;

		public static string[] Find(string pattern) =>
			Folder == null ? Array.Empty<string>() : Directory.GetFiles(Folder, pattern).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

		/// <summary>The built-in fonts of the game (the resource of Core), unpacked</summary>
		public static MemoryStream FontsXml()
		{
			string route = Path.Combine(SolutionFolder.Value, "Civilization1Remaster.Core", "Assets", "Fonts.xml.gz");
			MemoryStream content = new MemoryStream();

			using (System.IO.Compression.GZipStream source = new System.IO.Compression.GZipStream(File.OpenRead(route), System.IO.Compression.CompressionMode.Decompress))
				source.CopyTo(content);

			content.Position = 0;
			return content;
		}

		private static readonly Lazy<string> SolutionFolder = new Lazy<string>(() =>
		{
			for (DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
			{
				if (File.Exists(Path.Combine(folder.FullName, "Civilization1Remaster.sln")))
					return folder.FullName;
			}

			throw new DirectoryNotFoundException("Civilization1Remaster.sln not found above the test output");
		});

		public static string ScratchFile(string name)
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "scratch");
			Directory.CreateDirectory(folder);
			return Path.Combine(folder, $"{Guid.NewGuid():N}-{name}");
		}
	}
}
