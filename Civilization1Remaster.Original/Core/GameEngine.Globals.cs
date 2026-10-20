using System;
using System.Collections.Generic;
using System.Text;

namespace Civilization1Original
{
	partial class GameEngine
	{
		#region Global variables
		public Point[] TownDeltas = new Point[] { 
			new Point(0, -1), new Point(1, 0), new Point(0, 1), 
			new Point(-1, 0), new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1), 
			new Point(0, -2), new Point(2, 0), new Point(0, 2), new Point(-2, 0), new Point(-1, -2), 
			new Point(1, -2), new Point(2, -1), new Point(2, 1), new Point(1, 2), new Point(-1, 2), 
			new Point(-2, 1), new Point(-2, -1), new Point(0, 0) };

		public ushort GlobD768 = 0;
		public ushort GlobDb3a = 0;
		public ushort GlobDb3c = 0;
		public ushort GlobDb3e = 0;
		#endregion
	}
}
