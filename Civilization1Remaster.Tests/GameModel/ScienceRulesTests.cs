using Civilization1Remaster.GameModel.Rules;

namespace Civilization1Remaster.Tests.GameModel
{
	public class ScienceRulesTests
	{
		[Theory]
		[InlineData(0x22, true)]
		[InlineData(0x25, true)]
		[InlineData(0x23, false)]
		public void BarracksObsolescence(int technology, bool expected) => Assert.Equal(expected, ScienceRules.DoesTechnologyObsoleteBarracks(technology));

		[Fact]
		public void PlayerKnowsTechnologyReadsBitPerTechnology()
		{
			ushort[] markers = new ushort[5];
			markers[1] = 1 << 1;

			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 1, 17));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 16));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 0, 17));
			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 0, -1));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 71));
			Assert.False(ScienceRules.PlayerKnowsTechnology(new ushort[1], 1, 20));
		}

		[Fact]
		public void TechnologyInformation() => Assert.Equal(13, ScienceRules.CalculateTechnologyInformation(3, 2, 5));
	}
}
