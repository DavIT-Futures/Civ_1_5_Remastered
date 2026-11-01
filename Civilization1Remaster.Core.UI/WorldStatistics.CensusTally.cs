namespace Civilization1Remaster.Core.UI
{
	public partial class WorldStatistics
	{
		private sealed class CensusTally
		{
			public short[] Size { get; } = { 1, 1, 1, 1, 1, 1, 1, 1 };

			public short[] Land { get; } = new short[8];

			public short[] Money { get; } = new short[8];

			public short[] Learning { get; } = new short[8];

			public short[] Health { get; } = new short[8];

			public short[] Unrest { get; } = new short[8];

			public short[] Approval { get; } = new short[8];

			public short[] Output { get; } = new short[8];

			public short[] Shields { get; } = new short[8];

			public short[] Pollution { get; } = new short[8];
		}
	}
}
