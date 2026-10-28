using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class CensusRulesTests
	{
		[Fact]
		public void ApprovalRating_SmallEmpire_MatchesClassic()
		{
			short approval = 300;
			short size = 300;

			short classic = CensusRules.ApprovalRating(approval, size, classic: true);
			short modern = CensusRules.ApprovalRating(approval, size, classic: false);

			Assert.Equal(50, classic);
			Assert.Equal(50, modern);
		}

		[Fact]
		public void ApprovalRating_LargeEmpire_OverflowsInClassic_StaysPositiveInModern()
		{
			// Approval > 655 overflows 16-bit signed integer (50 * 800 = 40,000 -> -25,536)
			short approval = 800;
			short size = 500;

			short classic = CensusRules.ApprovalRating(approval, size, classic: true);
			short modern = CensusRules.ApprovalRating(approval, size, classic: false);

			// In 16-bit DOS CIV.EXE, this overflowed into a negative percentage
			Assert.True(classic < 0, $"Expected classic to overflow to negative, got {classic}");
			// In modern Remaster, it is correctly calculated: 50 * 800 / 500 = 80%
			Assert.Equal(80, modern);
		}

		[Fact]
		public void Productivity_SmallEmpire_MatchesClassic()
		{
			short output = 1500;
			short size = 150;

			short classic = CensusRules.Productivity(output, size, classic: true);
			short modern = CensusRules.Productivity(output, size, classic: false);

			Assert.Equal(100, classic);
			Assert.Equal(100, modern);
		}

		[Fact]
		public void Productivity_LargeEmpire_OverflowsInClassic_StaysPositiveInModern()
		{
			// Output > 3276 overflows 16-bit signed integer (10 * 4000 = 40,000 -> -25,536)
			short output = 4000;
			short size = 350;

			short classic = CensusRules.Productivity(output, size, classic: true);
			short modern = CensusRules.Productivity(output, size, classic: false);

			// In 16-bit DOS CIV.EXE, this overflowed into a negative productivity
			Assert.True(classic < 0, $"Expected classic to overflow to negative, got {classic}");
			// In modern Remaster: 10 * 4000 / 350 = 114
			Assert.Equal(114, modern);
		}

		[Fact]
		public void DiseaseRate_LargeEmpire_OverflowsInClassic_StaysValidInModern()
		{
			// Size > 655 overflows 16-bit signed integer (50 * 700 = 35,000 -> -30,536)
			short size = 700;
			short health = 300;

			short classic = CensusRules.DiseaseRate(size, health, classic: true);
			short modern = CensusRules.DiseaseRate(size, health, classic: false);

			Assert.True(classic < 0, $"Expected classic to overflow to negative, got {classic}");
			Assert.Equal(35, modern); // 50 * 700 / 1000 = 35%
		}
	}
}
