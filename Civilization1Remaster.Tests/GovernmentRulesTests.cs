using System.Collections.Generic;
using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class GovernmentRulesTests
	{
		[Fact]
		public void CanAdoptGovernment_Despotism_AlwaysAvailable()
		{
			bool canAdopt = GovernmentRules.CanAdoptGovernment(
				GovernmentRules.RegimeDespotism,
				tech => false,
				hasPyramids: false);

			Assert.True(canAdopt);
		}

		[Fact]
		public void CanAdoptGovernment_WithPyramids_AllRegimesAvailable()
		{
			for (short r = 1; r <= 5; r++)
			{
				bool canAdopt = GovernmentRules.CanAdoptGovernment(
					r,
					tech => false,
					hasPyramids: true);

				Assert.True(canAdopt, $"Regime {r} should be available with Pyramids");
			}
		}

		[Fact]
		public void CanAdoptGovernment_WithoutTech_ReturnsFalse()
		{
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeMonarchy, tech => false, false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeCommunism, tech => false, false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeRepublic, tech => false, false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeDemocracy, tech => false, false));
		}

		[Fact]
		public void CanAdoptGovernment_WithSpecificTech_ReturnsTrue()
		{
			var knownTechs = new HashSet<short> { 5, 44 }; // Monarchy (5), Republic (44)

			Assert.True(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeMonarchy, tech => knownTechs.Contains(tech), false));
			Assert.True(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeRepublic, tech => knownTechs.Contains(tech), false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeCommunism, tech => knownTechs.Contains(tech), false));
			Assert.False(GovernmentRules.CanAdoptGovernment(GovernmentRules.RegimeDemocracy, tech => knownTechs.Contains(tech), false));
		}

		[Theory]
		[InlineData(GovernmentRules.RegimeAnarchy, 4)]
		[InlineData(GovernmentRules.RegimeDespotism, 4)]    // 4 - 0 = 4
		[InlineData(GovernmentRules.RegimeMonarchy, 3)]     // 4 - 1 = 3
		[InlineData(GovernmentRules.RegimeCommunism, 3)]    // 4 - 1 = 3
		[InlineData(GovernmentRules.RegimeRepublic, 2)]     // 4 - 2 = 2
		[InlineData(GovernmentRules.RegimeDemocracy, 2)]    // 4 - 2 = 2
		public void ComputeDefaultLevyRatio_ReturnsAccurateRatio(short regime, short expectedRatio)
		{
			Assert.Equal(expectedRatio, GovernmentRules.ComputeDefaultLevyRatio(regime));
		}

		[Fact]
		public void EvaluatePeaceTreaty_WithoutDiagBit0_AlwaysAgrees()
		{
			bool agrees = GovernmentRules.EvaluatePeaceTreaty(
				diagMarkerBit0: false,
				humanPlayerIdx: 1,
				p1: 2,
				p2: 3,
				p1HumanRelation: 0,
				p2HumanRelation: 0,
				p1Might: 100,
				p2Might: 50,
				humanMight: 500,
				p1Nukes: 0,
				p2Nukes: 10,
				p1ChiefVisualDisplacement: 0,
				other => 0,
				lm => (1, 10, 10),
				lm => (10, 10));

			Assert.True(agrees);
		}

		[Fact]
		public void EvaluatePeaceTreaty_P1FewerNukes_RefusesTreaty()
		{
			bool agrees = GovernmentRules.EvaluatePeaceTreaty(
				diagMarkerBit0: true,
				humanPlayerIdx: 1,
				p1: 2,
				p2: 3,
				p1HumanRelation: 0,
				p2HumanRelation: 0,
				p1Might: 100,
				p2Might: 50,
				humanMight: 50,
				p1Nukes: 1,
				p2Nukes: 2,
				p1ChiefVisualDisplacement: 0,
				other => 0,
				lm => (1, 10, 10),
				lm => (10, 10));

			Assert.False(agrees);
		}
	}
}
