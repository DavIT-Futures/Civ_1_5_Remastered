using Civilization1Remaster.Core.Composition;
using Civilization1Remaster.Core.UI.Reports;

namespace Civilization1Remaster.Tests.Core.UI
{
	public class FameRecordsTests
	{
		[Fact]
		public void ClearedHallOfFameHasNoScores()
		{
			GameEngine engine = new GameEngine(new SilentGameHost());

			engine.Legends.ClearHallOfFame();

			for (int entryIndex = 0; entryIndex < FameRecords.HallOfFameRecordCount; entryIndex++)
			{
				ushort record = (ushort)(FameRecords.HallOfFameBaseAddress + (entryIndex * FameRecords.HallOfFameRecordSize));
				Assert.Equal(-1, engine.Processor.PullSignedWord(FameRecords.HallOfFameSegment, record));
			}
		}
	}
}
