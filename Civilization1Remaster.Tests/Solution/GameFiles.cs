namespace Civilization1Remaster.Tests.Solution
{
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

		private static readonly Lazy<string> SolutionFolder = new Lazy<string>(() =>
		{
			for (DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
			{
				if (File.Exists(Path.Combine(folder.FullName, "Civilization1Remaster.sln")))
				{
					return folder.FullName;
				}
			}

			throw new DirectoryNotFoundException("Civilization1Remaster.sln not found above the test output");
		});

		public static string? Folder => GameFolder.Value;

		public static string Solution => SolutionFolder.Value;

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
