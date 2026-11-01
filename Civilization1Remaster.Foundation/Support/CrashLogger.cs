using System;
using System.IO;

namespace Civilization1Remaster.Foundation.Support
{
	public static class CrashLogger
	{
		private static readonly object Latch = new object();

		public static void Log(Exception exception, string context = "Application")
		{
			lock (Latch)
			{
				string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
				string report = $"================================================================================\n" +
								$"CRASH REPORT [{timestamp}] - Context: {context}\n" +
								$"================================================================================\n" +
								$"Exception: {exception.GetType().FullName}\n" +
								$"Message: {exception.Message}\n" +
								$"Stack trace:\n{exception.StackTrace}\n";

				if (exception.InnerException != null)
				{
					report += $"\nInner Exception: {exception.InnerException.GetType().FullName}\n" +
							  $"Message: {exception.InnerException.Message}\n" +
							  $"Stack trace:\n{exception.InnerException.StackTrace}\n";
				}

				report += "================================================================================\n\n";

				Console.Error.WriteLine(report);

				TryWriteFile("crash.log", report);
				TryWriteFile("game.log", $"[{timestamp}] [CRASH] {exception.GetType().Name}: {exception.Message}\n");
			}
		}

		private static void TryWriteFile(string filename, string content)
		{
			try
			{
				string baseDirection = AppDomain.CurrentDomain.BaseDirectory;
				string primaryPath = Path.Combine(baseDirection, filename);
				File.AppendAllText(primaryPath, content);

				string currentDirection = Environment.CurrentDirectory;
				if (!string.Equals(baseDirection, currentDirection, StringComparison.OrdinalIgnoreCase))
				{
					string fallbackPath = Path.Combine(currentDirection, filename);
					File.AppendAllText(fallbackPath, content);
				}
			}
			catch
			{
			}
		}
	}
}
