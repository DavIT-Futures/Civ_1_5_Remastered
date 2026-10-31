using System;
using System.IO;

namespace Civilization1Remaster.Emulation
{
	/// <summary>
	/// Helpers for the file names that the DOS program passes to the emulated services.
	/// </summary>
	public static class DosPaths
	{
		/// <summary>
		/// Cuts the drive letter and the folders off a DOS path, only the file name stays.
		/// </summary>
		public static string ObtainDOSFileLabel(string route)
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

		/// <summary>
		/// Looks for a folder with the given name next to the executable and in every folder above it.
		/// </summary>
		/// <returns>The full path with a closing separator, or null when there is no such folder.</returns>
		public static string LocateGameDirectory(string directoryLabel)
		{
			DirectoryInfo folder = new DirectoryInfo(AppContext.BaseDirectory);

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
