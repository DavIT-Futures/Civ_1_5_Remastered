using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Foundation.Squeeze;

namespace Civ_1_5_Remastered.Tests.Core
{
	public class ProgramContentTests
	{
		private const int ContentSize = 47182;
		private const uint ContentChecksum = 4029859809u;

		[Fact]
		public void ContentFromTheGameProgramMatchesTheKnownImage()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			byte[] content = GameEngine.ProgramContent(File.ReadAllBytes(Path.Combine(GameFiles.Folder, "CIV.EXE")));

			Assert.Equal(ContentSize, content.Length);
			Assert.Equal(ContentChecksum, CyclicRedundancyCheck.ObtainChecksum(content));
		}
	}
}
