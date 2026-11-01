using System;
using System.IO;

namespace Civilization1Remaster.Machine
{
	public static class OperatingSystemPaths
	{
		public static string ObtainOperatingSystemFileLabel(string route)
		{
			string trail = route;

			if (trail.IndexOf(':') == 1)
			{
				trail = trail.Substring(2);
			}

			if (trail.IndexOf('\\') > 0)
			{
				trail = trail.Substring(trail.LastIndexOf('\\') + 1);
			}

			return trail;
		}

		public static string? LocateGameDirectory(string directoryLabel)
		{
			DirectoryInfo? folder = new DirectoryInfo(AppContext.BaseDirectory);

			while (folder != null)
			{
				string trail = Path.Combine(folder.FullName, directoryLabel);

				if (Directory.Exists(trail))
				{
					return trail + Path.DirectorySeparatorChar;
				}

				folder = folder.Parent;
			}

			return null;
		}
	}
}
