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
		[StructLayout(LayoutKind.Sequential)]
		private struct BitmapInfoHeader
		{
			public int Extent;
			public int Breadth;
			public int Altitude;
			public short Aircraft;
			public short BinaryTally;
			public int Packing;
			public int ExtentVisual;
			public int HorizDotsEachMetre;
			public int VertDotsEachMetre;
			public int HueTaken;
			public int HueVital;
		}
	}
}
