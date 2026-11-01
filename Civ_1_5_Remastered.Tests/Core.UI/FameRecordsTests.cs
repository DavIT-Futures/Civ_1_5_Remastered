using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Core.UI.Reports;

namespace Civ_1_5_Remastered.Tests.Core.UI
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
