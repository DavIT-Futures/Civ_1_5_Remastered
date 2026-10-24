using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class TechnologyNewsRulesTests
	{
		private const int Babylonians = 3;
		private const int Romans = 1;

		[Fact]
		public void OwnResearchNamesNoSource()
		{
			// UncoverTechnology and the scrolls of a hut give the source 0, the start of the game the player himself
			Assert.False(TechnologyNewsRules.NamesSource(Babylonians, 0, classic: false));
			Assert.False(TechnologyNewsRules.NamesSource(Babylonians, Babylonians, classic: false));
		}

		[Fact]
		public void TechnologyFromElsewhereNamesTheSource()
		{
			Assert.True(TechnologyNewsRules.NamesSource(Babylonians, Romans, classic: false));
			Assert.True(TechnologyNewsRules.NamesSource(Babylonians, TechnologyNewsRules.GreatLibrary, classic: false));
		}

		[Fact]
		public void ClassicNewsNamesTheBarbariansForResearch()
		{
			// The translated code: "Babylonians discover Mass Transit from the Barbarians!"
			Assert.True(TechnologyNewsRules.NamesSource(Babylonians, 0, classic: true));
			Assert.False(TechnologyNewsRules.NamesSource(Babylonians, Babylonians, classic: true));
			Assert.True(TechnologyNewsRules.NamesSource(Babylonians, Romans, classic: true));
		}
	}
}
