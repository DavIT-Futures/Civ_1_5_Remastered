using Civilization1Remaster.Foundation.Containers;
using Civilization1Remaster.Foundation.Randomness;

namespace Civilization1Remaster.Tests.Foundation
{
	public class RandomMT19937Tests
	{
		[Fact]
		public void MatchesReferenceSequenceForSeed5489()
		{
			// Reference output of mt19937 for the default seed
			RandomMT19937 random = new RandomMT19937(5489);
			uint[] expected = { 3499211612u, 581869302u, 3890346734u, 3586334585u, 545404204u };

			foreach (uint value in expected)
				Assert.Equal(value, random.DrawUnsigned());
		}

		[Fact]
		public void TheTenThousandthDrawMatchesReference()
		{
			RandomMT19937 random = new RandomMT19937(5489);
			uint value = 0;

			for (int idx = 0; idx < 10000; idx++)
				value = random.DrawUnsigned();

			Assert.Equal(4123659995u, value);
		}

		[Fact]
		public void SameSeedGivesSameSequence()
		{
			RandomMT19937 first = new RandomMT19937(42);
			RandomMT19937 second = new RandomMT19937(42);

			for (int idx = 0; idx < 1000; idx++)
				Assert.Equal(first.DrawInt(), second.DrawInt());
		}

		[Fact]
		public void DrawIntIsNonNegative()
		{
			RandomMT19937 random = new RandomMT19937(1);

			for (int idx = 0; idx < 1000; idx++)
				Assert.InRange(random.DrawInt(), 0, int.MaxValue);
		}

		[Fact]
		public void DrawIntStaysBelowPeak()
		{
			RandomMT19937 random = new RandomMT19937(2);

			for (int idx = 0; idx < 1000; idx++)
				Assert.InRange(random.DrawInt(6), 0, 5);
		}

		[Fact]
		public void DrawIntStaysInFloorPeakRange()
		{
			RandomMT19937 random = new RandomMT19937(3);
			HashSet<int> seen = new HashSet<int>();

			for (int idx = 0; idx < 1000; idx++)
			{
				int value = random.DrawInt(-3, 3);
				Assert.InRange(value, -3, 2);
				seen.Add(value);
			}

			Assert.Equal(6, seen.Count);
		}

		[Fact]
		public void DrawIntRejectsFloorAbovePeak()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new RandomMT19937(4).DrawInt(5, 4));
		}

		[Fact]
		public void DrawFractionIsInUnitInterval()
		{
			RandomMT19937 random = new RandomMT19937(5);

			for (int idx = 0; idx < 1000; idx++)
			{
				double value = random.DrawFraction();
				Assert.True(value >= 0.0 && value < 1.0);
			}
		}

		[Fact]
		public void FillWithRandomUsesLowByteOfEachDraw()
		{
			RandomMT19937 reference = new RandomMT19937(6);
			byte[] staging = new byte[16];
			new RandomMT19937(6).FillWithRandom(staging);

			foreach (byte value in staging)
				Assert.Equal((byte)(reference.DrawUnsigned() & 0xff), value);
		}
	}

	public class ContainerTests
	{
		[Fact]
		public void DictionaryAddsAndFindsValues()
		{
			BDictionary<int, string> lookup = new BDictionary<int, string>();

			for (int idx = 0; idx < 500; idx++)
				lookup.Add(idx * 7, $"v{idx}");

			Assert.Equal(500, lookup.Count);

			for (int idx = 0; idx < 500; idx++)
			{
				Assert.True(lookup.IncludesKey(idx * 7));
				Assert.Equal($"v{idx}", lookup.ObtainValueByKey(idx * 7));
			}

			Assert.False(lookup.IncludesKey(3));
		}

		[Fact]
		public void DictionaryKeepsInsertionOrder()
		{
			BDictionary<int, int> lookup = new BDictionary<int, int>();
			int[] keys = { 50, 3, 99, 1, 42 };

			foreach (int key in keys)
				lookup.Add(key, key * 2);

			Assert.Equal(keys, lookup.Select(couple => couple.Token).ToArray());
			Assert.Equal(2, lookup.PositionOfKey(99));
		}

		[Fact]
		public void DictionaryAddWithExistingKeyReplacesValue()
		{
			BDictionary<string, int> lookup = new BDictionary<string, int>();
			lookup.Add("a", 1);
			lookup.Add("a", 2);

			Assert.Single(lookup);
			Assert.Equal(2, lookup.ObtainValueByKey("a"));
		}

		[Fact]
		public void DictionaryDiscardKeepsOtherKeysReachable()
		{
			BDictionary<int, int> lookup = new BDictionary<int, int>();

			for (int idx = 0; idx < 100; idx++)
				lookup.Add(idx, idx);

			for (int idx = 0; idx < 100; idx += 3)
				lookup.DiscardByKey(idx);

			for (int idx = 0; idx < 100; idx++)
			{
				Assert.Equal(idx % 3 != 0, lookup.IncludesKey(idx));

				if (idx % 3 != 0)
					Assert.Equal(idx, lookup.ObtainValueByKey(idx));
			}
		}

		[Fact]
		public void DictionaryDiscardOfMissingKeyThrows()
		{
			Assert.Throws<InvalidOperationException>(() => new BDictionary<int, int>().DiscardByKey(1));
		}

		[Fact]
		public void DictionaryClearEmptiesIt()
		{
			BDictionary<int, int> lookup = new BDictionary<int, int>();
			lookup.Add(1, 1);
			lookup.Clear();

			Assert.Empty(lookup);
			Assert.False(lookup.IncludesKey(1));
		}

		[Fact]
		public void HashSetIgnoresDuplicates()
		{
			BHashSet<int> set = new BHashSet<int>();

			for (int idx = 0; idx < 200; idx++)
				set.Add(idx % 50);

			Assert.Equal(50, set.Count);
			Assert.Equal(Enumerable.Range(0, 50).ToArray(), set.AsArray());
		}

		[Fact]
		public void HashSetRemoveKeepsOthers()
		{
			BHashSet<string> set = new BHashSet<string>(new[] { "a", "b", "c", "d" });

			Assert.True(set.Remove("b"));
			Assert.False(set.Remove("x"));
			Assert.DoesNotContain("b", set);
			Assert.Contains("c", set);
			Assert.Equal(new[] { "a", "c", "d" }, set.ToArray());
		}
	}
}
