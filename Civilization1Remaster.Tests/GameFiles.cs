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

		public static string ScratchFile(string name)
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "scratch");
			Directory.CreateDirectory(folder);
			return Path.Combine(folder, $"{Guid.NewGuid():N}-{name}");
		}
	}
}
