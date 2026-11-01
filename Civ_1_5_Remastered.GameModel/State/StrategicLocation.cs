using System.Drawing;

namespace Civ_1_5_Remastered.GameModel.State
{
	public class StrategicLocation
	{
		public sbyte Live { get; set; } = 0;
		public byte Strategy { get; set; } = 0;
		public Point Place { get; set; } = new Point(0, 0);
	}
}
