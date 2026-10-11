using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using Civilization1Remaster.Containers;

namespace Civilization1Remaster
{
	public partial class MainForm
	{
		/// <summary>
		/// A copy of a game screen in 32 bit colors, taken while the game is stopped by the lock
		/// </summary>
		private class ScreenFrame
		{
			public int Token;
			public int Col;
			public int Line;
			public int Breadth;
			public int Altitude;
			public int[] Dots = Array.Empty<int>();
			public bool Altered;
		}
	}
}
