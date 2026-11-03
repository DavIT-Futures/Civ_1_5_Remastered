using System.IO;
using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Machine.Processing;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.GameModel
{
	public class SpecialProjectsTests
	{
		[Fact]
		public void PrintTechnologyIndices()
		{
			if (GameFiles.Folder == null)
			{
				return;
			}

			GameEngine engine = new GameEngine(new SilentGameHost());
			engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
			engine.PrepareMachine();

			for (int technologyIndex = 0; technologyIndex < 72; technologyIndex++)
			{
				string name = engine.Processor.PullText(CentralProcessor.AsLinearAddress(engine.Processor.SegmentData.Word, GameTables.TechnologyText(technologyIndex)));
				if (name == "Banking")
				{
					Assert.Equal("53:Banking", $"{technologyIndex}:{name}");
				}

				if (name == "University")
				{
					Assert.Equal("13:University", $"{technologyIndex}:{name}");
				}

				if (name == "Engineering")
				{
					Assert.Equal("12:Engineering", $"{technologyIndex}:{name}");
				}
			}
		}

		[Fact]
		public void ConstantsHaveExpectedValues()
		{
			Assert.Equal(-100, SpecialProjects.Wealth);
			Assert.Equal(-101, SpecialProjects.Research);
			Assert.Equal(-102, SpecialProjects.Food);
			Assert.Equal(unchecked((ushort)(-100)), SpecialProjects.WealthChoice);
			Assert.Equal(unchecked((ushort)(-101)), SpecialProjects.ResearchChoice);
			Assert.Equal(unchecked((ushort)(-102)), SpecialProjects.FoodChoice);
			Assert.Equal(53, SpecialProjects.BankingTechnology);
			Assert.Equal(13, SpecialProjects.UniversityTechnology);
			Assert.Equal(12, SpecialProjects.EngineeringTechnology);
		}

		[Theory]
		[InlineData(SpecialProjects.Wealth, true)]
		[InlineData(SpecialProjects.Research, true)]
		[InlineData(SpecialProjects.Food, true)]
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
		[InlineData(102, true)]
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
				PresentMakingIndex = SpecialProjects.Wealth,
				MaterialsTally = 42,
			};

			using MemoryStream stream = new MemoryStream();
			city.WriteTo(stream);
			stream.Position = 0;

			City deserialized = City.ReadFrom(stream);
			Assert.Equal(SpecialProjects.Wealth, deserialized.PresentMakingIndex);
			Assert.Equal(42, deserialized.MaterialsTally);

			city.PresentMakingIndex = SpecialProjects.Research;
			using MemoryStream streamResearch = new MemoryStream();
			city.WriteTo(streamResearch);
			streamResearch.Position = 0;

			City deserializedResearch = City.ReadFrom(streamResearch);
			Assert.Equal(SpecialProjects.Research, deserializedResearch.PresentMakingIndex);

			city.PresentMakingIndex = SpecialProjects.Food;
			using MemoryStream streamFood = new MemoryStream();
			city.WriteTo(streamFood);
			streamFood.Position = 0;

			City deserializedFood = City.ReadFrom(streamFood);
			Assert.Equal(SpecialProjects.Food, deserializedFood.PresentMakingIndex);
		}

		[Fact]
		public void ChoiceCastingRecoversSpecialProjectSignedByte()
		{
			ushort wealthChoice = SpecialProjects.WealthChoice;
			sbyte recoveredWealth = unchecked((sbyte)(byte)wealthChoice);
			Assert.Equal(SpecialProjects.Wealth, recoveredWealth);

			ushort researchChoice = SpecialProjects.ResearchChoice;
			sbyte recoveredResearch = unchecked((sbyte)(byte)researchChoice);
			Assert.Equal(SpecialProjects.Research, recoveredResearch);

			ushort foodChoice = SpecialProjects.FoodChoice;
			sbyte recoveredFood = unchecked((sbyte)(byte)foodChoice);
			Assert.Equal(SpecialProjects.Food, recoveredFood);
		}
	}
}
