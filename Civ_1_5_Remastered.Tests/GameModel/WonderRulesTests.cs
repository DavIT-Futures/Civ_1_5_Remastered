using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class WonderRulesTests
	{
		[Theory]
		[InlineData("+1 happy face in each city, until the development of INVENTION.\n", "+1 happy face in each city.\n")]
		[InlineData("Gives you any technology that two other civilizations possess, until the\ndevelopment of the UNIVERSITY.\n", "Gives you any technology that two other civilizations possess.\n")]
		[InlineData("Allows change of government without Anarchy;\nMakes available all forms of government. Both of these effects\nlast until the development of COMMUNISM.\n", "Allows change of government without Anarchy;\nMakes available all forms of government.\n")]
		[InlineData("Increases sea movement by 1 MP.\n", "Increases sea movement by 1 MP.\n")]
		public void EffectsLastForever(string effect, string expected) => Assert.Equal(expected, WonderRules.WithoutObsolescence(effect));

		[Fact]
		public void ChapelStrengthensCathedralsByHalf() => Assert.Equal(
			"Increases the effect of all your CATHEDRALS by 50%: each Cathedral makes 6 unhappy citizens content instead of 4.\n",
			WonderRules.CivilopediaEffect(WonderRules.MichelangelosChapel, "Increases the effect of CATHEDRALS, until the development of COMMUNISM.\n"));

		[Fact]
		public void OtherWondersKeepTheirEffect() => Assert.Equal(
			"+1 happy face in each city.\n",
			WonderRules.CivilopediaEffect(0x1a, "+1 happy face in each city, until the development of INVENTION.\n"));

		[Theory]
		[InlineData(0x18, false)]
		[InlineData(0x19, true)]
		[InlineData(0x2d, true)]
		public void WondersFollowTheBuildings(int improvement, bool expected) => Assert.Equal(expected, WonderRules.IsWonder(improvement));
	}
}
