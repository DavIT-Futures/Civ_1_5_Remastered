using System;
using System.IO;

namespace Civilization1Remaster.Foundation.Support
{
	public static class CrashLogger
	{
		private static readonly object Latch = new object();

		public static void Log(Exception ex, string context = "Application")
		{
			lock (Latch)
			{
				string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
				string report = $"================================================================================\n" +
								$"CRASH REPORT [{timestamp}] - Context: {context}\n" +
								$"================================================================================\n" +
								$"Exception: {ex.GetType().FullName}\n" +
								$"Message: {ex.Message}\n" +
								$"Stack trace:\n{ex.StackTrace}\n";

				if (ex.InnerException != null)
				{
					report += $"\nInner Exception: {ex.InnerException.GetType().FullName}\n" +
							  $"Message: {ex.InnerException.Message}\n" +
							  $"Stack trace:\n{ex.InnerException.StackTrace}\n";
				}

				report += "================================================================================\n\n";

				Console.Error.WriteLine(report);

				TryWriteFile("crash.log", report);
				TryWriteFile("game.log", $"[{timestamp}] [CRASH] {ex.GetType().Name}: {ex.Message}\n");
			}
		}

		private static void TryWriteFile(string filename, string content)
		{
			try
			{
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string primaryPath = Path.Combine(baseDir, filename);
				File.AppendAllText(primaryPath, content);

				string currentDir = Environment.CurrentDirectory;
				if (!string.Equals(baseDir, currentDir, StringComparison.OrdinalIgnoreCase))
				{
					string fallbackPath = Path.Combine(currentDir, filename);
					File.AppendAllText(fallbackPath, content);
				}
			}
			catch
			{
			}
		}
	}
}
