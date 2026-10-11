using System;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;

namespace Civilization1Remaster
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			bool filesFound = GameFolderCheck.Verify(out string message);
			MessageBox.Show(message, filesFound ? "Civilization 1 files found" : "Civilization 1 files not found",
				MessageBoxButtons.OK, MessageBoxIcon.Information);

			if (!filesFound)
			{
				return;
			}

			Application.Run(new MainForm());
		}
	}
}
