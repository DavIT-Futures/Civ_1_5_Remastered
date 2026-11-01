using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class CensusRulesTests
	{
		[Theory]
		[InlineData(3, 6, 25)]
		[InlineData(10, 2, 100)]
		[InlineData(3, 0, 0)]
		[InlineData(-4, 2, 0)]
		[InlineData(1000, 10, 100)]
		public void ApprovalRating(short approval, short size, short expected) => Assert.Equal(expected, CensusRules.ApprovalRating(approval, size));

		[Theory]
		[InlineData(5, 2, 25)]
		[InlineData(5, 0, 0)]
		[InlineData(-5, 2, 0)]
		[InlineData(4000, 100, 400)]
		public void Productivity(short output, short size, short expected) => Assert.Equal(expected, CensusRules.Productivity(output, size));

		[Theory]
		[InlineData(2, 3, 20)]
		[InlineData(0, 0, 0)]
		[InlineData(1000, 0, 50)]
		public void DiseaseRate(short size, short health, short expected) => Assert.Equal(expected, CensusRules.DiseaseRate(size, health));

		[Theory]
		[InlineData(3, 6, 40)]
		[InlineData(3, 0, 20)]
		[InlineData(1000, 100, 420)]
		public void FamilySize(short unrest, short size, short expected) => Assert.Equal(expected, CensusRules.FamilySize(unrest, size));
	}
}
