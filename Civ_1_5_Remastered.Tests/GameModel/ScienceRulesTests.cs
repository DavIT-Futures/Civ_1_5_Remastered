using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class ScienceRulesTests
	{
		private const short Writing = 0x16;
		private const short Philosophy = 0x1c;
		private const short Religion = 0x1d;

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
		public void ReligionMayBeResearchedWithPhilosophyAndWriting()
		{
			ushort[] markers = Known(Philosophy, Writing);

			Assert.True(ScienceRules.MayResearch(markers, 1, Religion, Philosophy, Writing));
			Assert.False(ScienceRules.MayResearch(Known(Philosophy), 1, Religion, Philosophy, Writing));
			Assert.False(ScienceRules.MayResearch(Known(Philosophy, Writing, Religion), 1, Religion, Philosophy, Writing));
		}

		[Fact]
		public void TechnologyWithoutPrerequisitesMayBeResearched() =>
			Assert.True(ScienceRules.MayResearch(new ushort[5], 1, 0, -1, -1));

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(5)]
		public void ResearchChoicesListEveryAvailableTechnology(int extraKnown)
		{
			// Twelve independent technologies without prerequisites plus Religion: all of them must be offered no matter
			// how many there are or how many discoveries were made (the original game hid every third one).
			Dictionary<short, (sbyte, sbyte)> table = new Dictionary<short, (sbyte, sbyte)>();

			for (short index = 0; index < 12; index++)
			{
				table[index] = (-1, -1);
			}

			table[Religion] = ((sbyte)Philosophy, (sbyte)Writing);
			table[Philosophy] = (-1, -1);
			table[Writing] = (-1, -1);

			List<short> known = new List<short> { Philosophy, Writing };

			for (short index = 0; index < extraKnown; index++)
			{
				known.Add(index);
			}

			ushort[] markers = Known(known.ToArray());

			List<short> choices = ScienceRules.ResearchChoices(markers, 1, index => table.TryGetValue(index, out (sbyte, sbyte) pair) ? pair : ((sbyte)127, (sbyte)127));

			List<short> expected = Enumerable.Range(extraKnown, 12 - extraKnown).Select(index => (short)index).ToList();
			expected.Add(Religion);

			Assert.Equal(expected, choices);
		}

		[Theory]
		[InlineData(5, 64)]
		[InlineData(13, 56)]
		[InlineData(30, 0)]
		public void ResearchMenuMovesUpWhenTheListIsLong(int choices, int top) => Assert.Equal(top, ScienceRules.ResearchMenuTop(choices));

		[Fact]
		public void TechnologyInformation() => Assert.Equal(13, ScienceRules.CalculateTechnologyInformation(3, 2, 5));

		private static ushort[] Known(params short[] technologies)
		{
			ushort[] markers = new ushort[5];

			foreach (short technology in technologies)
			{
				markers[technology >> 4] |= (ushort)(1 << (technology & 0xf));
			}

			return markers;
		}
	}
}
