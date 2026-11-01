namespace Civilization1Remaster.Tests
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

		public static MemoryStream FontsMarkup()
		{
			string route = Path.Combine(SolutionFolder.Value, "Civilization1Remaster.Core", "Assets", "Fonts.xml.gz");
			MemoryStream content = new MemoryStream();

			using (System.IO.Compression.GZipStream source = new System.IO.Compression.GZipStream(File.OpenRead(route), System.IO.Compression.CompressionMode.Decompress))
			{
				source.CopyTo(content);
			}

			content.Position = 0;
			return content;
		}

		public static string ScratchFile(string name)
		{
			string folder = Path.Combine(AppContext.BaseDirectory, "scratch");
			Directory.CreateDirectory(folder);
			return Path.Combine(folder, $"{Guid.NewGuid():N}-{name}");
		}
	}
}
