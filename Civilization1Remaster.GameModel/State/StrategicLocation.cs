using System;
using System.Drawing;

namespace Civilization1Remaster.Game.State
{
	public class StrategicLocation
	{
		public sbyte Live { get; set; } = 0;
		public byte Strategy { get; set; } = 0;
		public Point Place { get; set; } = new Point(0, 0);
	}
}
