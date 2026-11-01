namespace Civ_1_5_Remastered.Core.Composition
{
	public partial class GameEngine
	{
		public Point[] TownDeltas { get; set; } = new Point[]
		{
			new Point(0, -1), new Point(1, 0), new Point(0, 1),
			new Point(-1, 0), new Point(1, -1), new Point(1, 1), new Point(-1, 1), new Point(-1, -1),
			new Point(0, -2), new Point(2, 0), new Point(0, 2), new Point(-2, 0), new Point(-1, -2),
			new Point(1, -2), new Point(2, -1), new Point(2, 1), new Point(1, 2), new Point(-1, 2),
			new Point(-2, 1), new Point(-2, -1), new Point(0, 0)
		};

		public ushort CreditsStream { get; set; } = 0;
		public ushort MenuPointerState { get; set; } = 0;
		public ushort MenuPointerHorizontal { get; set; } = 0;
		public ushort MenuPointerVertical { get; set; } = 0;
	}
}
