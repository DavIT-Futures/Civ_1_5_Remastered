using System;
using System.Collections.Generic;
using Civilization1Remaster.Game.Rules;
using Xunit;

namespace Civilization1Remaster.Tests
{
	public class ScienceRulesTests
	{
		[Fact]
		public void ComputeTechnologyCost_HumanChieftainEarlyGame()
		{
			// Difficulty 0 (Chieftain), 1 tech discovered, BC year.
			// baseFactor = 6 + 0 = 6, but (11 - 1) = 10 > 6 => baseFactor = 10.
			// Cost = 10 * 1 * 1 = 10 beakers.
			int cost = ScienceRules.ComputeTechnologyCost(isHuman: true, difficultyTier: 0, foundScienceTally: 1, yearBC: true);
			Assert.Equal(10, cost);
		}

		[Fact]
		public void ComputeTechnologyCost_HumanEmperorMidGame()
		{
			// Difficulty 4 (Emperor), 10 techs discovered, AD year.
			// baseFactor = 6 + (4 * 2) = 14.
			// (11 - 10) = 1 < 14 => baseFactor = 14.
			// AD year multiplier = 2.
			// Cost = 14 * 10 * 2 = 280 beakers.
			int cost = ScienceRules.ComputeTechnologyCost(isHuman: true, difficultyTier: 4, foundScienceTally: 10, yearBC: false);
			Assert.Equal(280, cost);
		}

		[Fact]
		public void ComputeTechnologyCost_AiChieftainVersusEmperor()
		{
			// On Emperor (tier 4), AI gets advantage: baseFactor = 14 - 4 = 10.
			// On Chieftain (tier 0), AI is handicapped: baseFactor = 14 - 0 = 14.
			int aiChieftain = ScienceRules.ComputeTechnologyCost(isHuman: false, difficultyTier: 0, foundScienceTally: 5, yearBC: true);
			int aiEmperor = ScienceRules.ComputeTechnologyCost(isHuman: false, difficultyTier: 4, foundScienceTally: 5, yearBC: true);

			Assert.Equal(70, aiChieftain);  // 14 * 5 * 1 = 70
			Assert.Equal(50, aiEmperor);    // 10 * 5 * 1 = 50
			Assert.True(aiEmperor < aiChieftain, "AI tech costs should be cheaper on higher difficulty levels.");
		}

		[Fact]
		public void CanResearch_RequiresBothPrerequisites()
		{
			var playerKnown = new HashSet<int> { 1, 2 };

			// Tech requires 1 and 2: satisfied
			Assert.True(ScienceRules.CanResearch(1, 2, playerKnown.Contains));

			// Tech requires 1 and 3: missing 3
			Assert.False(ScienceRules.CanResearch(1, 3, playerKnown.Contains));

			// Tech has only one prerequisite (prereq2 is -1)
			Assert.True(ScienceRules.CanResearch(1, -1, playerKnown.Contains));
			Assert.False(ScienceRules.CanResearch(3, -1, playerKnown.Contains));

			// Tech has no prerequisites (-1, -1)
			Assert.True(ScienceRules.CanResearch(-1, -1, playerKnown.Contains));
		}

		[Theory]
		[InlineData(100, 100, true)]
		[InlineData(105, 100, true)]
		[InlineData(99, 100, false)]
		public void IsResearchComplete_ChecksThreshold(int headway, int cost, bool expected)
		{
			Assert.Equal(expected, ScienceRules.IsResearchComplete(headway, cost));
		}

		[Theory]
		[InlineData(0x22, true)]  // Gunpowder obsoletes barracks
		[InlineData(0x25, true)]  // Combustion obsoletes barracks
		[InlineData(0x00, false)] // Alphabet
		[InlineData(0x14, false)] // Philosophy
		[InlineData(0x47, false)] // Future Tech
		public void DoesTechObsoleteBarracks_VerifiesGunpowderAndCombustion(int techIdx, bool expected)
		{
			Assert.Equal(expected, ScienceRules.DoesTechObsoleteBarracks(techIdx));
		}

		[Fact]
		public void PlayerKnowsTechnology_ValidatesBitmasksAndBoundaries()
		{
			ushort[] markers = new ushort[5];
			// Set tech 0 (bit 0 of word 0) and tech 17 (bit 1 of word 1)
			markers[0] = 0x0001;
			markers[1] = 0x0002;

			// Barbarians (participantIdx 0) cannot know techs via this method
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 0, 0));

			// Player 1 knows tech 0 and 17
			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 1, 0));
			Assert.True(ScienceRules.PlayerKnowsTechnology(markers, 1, 17));

			// Player 1 does not know tech 1 or 16
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 1));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 16));

			// Negative tech or out-of-bounds tech (>= 71)
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, -1));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 71));
			Assert.False(ScienceRules.PlayerKnowsTechnology(markers, 1, 100));
		}

		[Fact]
		public void CalculateTechnologyInfo_CalculatesLeaderTechPreference()
		{
			// formula: (ushort)(leaderFactor * b1 + b0)
			sbyte b0 = 10;
			sbyte b1 = 3;
			short leaderFactor = 5;

			ushort result = ScienceRules.CalculateTechnologyInfo(b0, b1, leaderFactor);
			Assert.Equal((ushort)25, result); // 5 * 3 + 10 = 25
		}
	}
}

