using Civilization1Remaster.Foundation.Randomness;

namespace Civilization1Remaster.Tests.Foundation
{
	public class RandomGeneratorTests
	{
		[Theory]
		[InlineData(1u, new uint[] { 2442144158u, 3238099751u, 3819917871u, 2104621829u, 2021136066u })]
		[InlineData(0u, new uint[] { 3809008728u, 1133695204u, 53579671u, 2891528803u, 139681546u })]
		public void MatchesTheReferenceSequence(uint seed, uint[] expected)
		{
			RandomGenerator random = new RandomGenerator(seed);

			Assert.Equal(expected, expected.Select(_ => random.DrawUnsigned()));
		}

		[Fact]
		public void SameSeedGivesSameSequence()
		{
			RandomGenerator first = new RandomGenerator(42);
			RandomGenerator second = new RandomGenerator(42);

			for (int index = 0; index < 1000; index++)
			{
				Assert.Equal(first.DrawUnsigned(), second.DrawUnsigned());
			}
		}

		[Fact]
		public void DrawIntegerStaysBelowPeak()
		{
			RandomGenerator random = new RandomGenerator(2);

			for (int index = 0; index < 1000; index++)
			{
				Assert.InRange(random.DrawInteger(6), 0, 5);
			}

			Assert.Equal(0, random.DrawInteger(0));
			Assert.InRange(random.DrawInteger(-6), -5, 0);
		}

		[Fact]
		public void DrawIntegerSpreadsEvenly()
		{
			RandomGenerator random = new RandomGenerator(7);
			int[] tally = new int[6];

			for (int index = 0; index < 60000; index++)
			{
				tally[random.DrawInteger(6)]++;
			}

			Assert.All(tally, count => Assert.InRange(count, 9500, 10500));
		}
	}
}
