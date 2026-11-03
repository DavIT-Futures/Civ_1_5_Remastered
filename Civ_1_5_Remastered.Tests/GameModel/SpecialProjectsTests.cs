using System.IO;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class SpecialProjectsTests
	{
		[Fact]
		public void ConstantsHaveExpectedValues()
		{
			Assert.Equal(-100, SpecialProjects.Coinage);
			Assert.Equal(-101, SpecialProjects.Research);
			Assert.Equal(unchecked((ushort)(-100)), SpecialProjects.CoinageChoice);
			Assert.Equal(unchecked((ushort)(-101)), SpecialProjects.ResearchChoice);
		}

		[Theory]
		[InlineData(SpecialProjects.Coinage, true)]
		[InlineData(SpecialProjects.Research, true)]
		[InlineData(0, false)]
		[InlineData(1, false)]
		[InlineData(27, false)]
		[InlineData(-1, false)]
		[InlineData(-21, false)]
		[InlineData(-25, false)]
		[InlineData(-48, false)]
		public void IsSpecialProjectIdentifiesMakingCorrectly(sbyte making, bool expected)
		{
			Assert.Equal(expected, SpecialProjects.IsSpecialProject(making));
		}

		[Theory]
		[InlineData(100, true)]
		[InlineData(101, true)]
		[InlineData(0, false)]
		[InlineData(1, false)]
		[InlineData(10, false)]
		[InlineData(12, false)]
		public void IsSpecialProjectIndexIdentifiesIndexesCorrectly(int index, bool expected)
		{
			Assert.Equal(expected, SpecialProjects.IsSpecialProjectIndex(index));
		}

		[Fact]
		public void CitySerializationPreservesSpecialProjects()
		{
			City city = new City
			{
				PresentMakingIndex = SpecialProjects.Coinage,
				MaterialsTally = 42,
			};

			using MemoryStream stream = new MemoryStream();
			city.WriteTo(stream);
			stream.Position = 0;

			City deserialized = City.ReadFrom(stream);
			Assert.Equal(SpecialProjects.Coinage, deserialized.PresentMakingIndex);
			Assert.Equal(42, deserialized.MaterialsTally);

			city.PresentMakingIndex = SpecialProjects.Research;
			using MemoryStream streamResearch = new MemoryStream();
			city.WriteTo(streamResearch);
			streamResearch.Position = 0;

			City deserializedResearch = City.ReadFrom(streamResearch);
			Assert.Equal(SpecialProjects.Research, deserializedResearch.PresentMakingIndex);
		}

		[Fact]
		public void ChoiceCastingRecoversSpecialProjectSignedByte()
		{
			ushort coinageChoice = SpecialProjects.CoinageChoice;
			sbyte recoveredCoinage = unchecked((sbyte)(byte)coinageChoice);
			Assert.Equal(SpecialProjects.Coinage, recoveredCoinage);

			ushort researchChoice = SpecialProjects.ResearchChoice;
			sbyte recoveredResearch = unchecked((sbyte)(byte)researchChoice);
			Assert.Equal(SpecialProjects.Research, recoveredResearch);
		}
	}
}
