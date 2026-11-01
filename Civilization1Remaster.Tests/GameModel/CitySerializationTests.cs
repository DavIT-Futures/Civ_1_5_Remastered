using System.Drawing;
using Civilization1Remaster.GameModel.State;

namespace Civilization1Remaster.Tests.GameModel
{
	public class CitySerializationTests
	{
		private const int RecordLength = 28;

		internal static City Sample()
		{
			City town = new City
			{
				StructureMarkers0 = 0x1234,
				StructureMarkers1 = 0x8001,
				Place = new Point(77, 48),
				ConditionMarker = 0x81,
				TrueExtent = 12,
				ShownExtent = 11,
				PresentMakingIdx = -3,
				RootCommerce = 9,
				ParticipantIdx = 6,
				GrainTally = -20,
				MaterialsTally = 300,
				LaborerMarkers0 = 0xfedc,
				LaborerMarkers1 = 0x0102,
				LaborerMarkers2 = 0xa5a5,
				LabelIdx = 0xc8,
			};
			town.CommerceTownIndices = new sbyte[] { 1, -1, 127 };
			town.Unfamiliar = new sbyte[] { -128, 5 };
			return town;
		}

		[Fact]
		public void WritesTheSaveGameLayout()
		{
			MemoryStream sink = new MemoryStream();
			Sample().WriteTo(sink);

			byte[] expected =
			{
				0x34, 0x12, 0x01, 0x80, 77, 48, 0x81, 12, 11, 0xfd, 9, 6,
				0xec, 0xff, 0x2c, 0x01, 0xdc, 0xfe, 0x02, 0x01, 0xa5, 0xa5, 0xc8,
				1, 0xff, 127, 0x80, 5
			};

			Assert.Equal(RecordLength, expected.Length);
			Assert.Equal(expected, sink.ToArray());
		}

		[Fact]
		public void RoundTrip()
		{
			MemoryStream sink = new MemoryStream();
			City original = Sample();
			original.WriteTo(sink);
			sink.Position = 0;

			City copy = City.ReadFrom(sink);

			Assert.Equal(RecordLength, sink.Position);
			AssertSame(original, copy);
		}

		[Fact]
		public void ParticipantIsReadAsSignedByte()
		{
			MemoryStream sink = new MemoryStream();
			City town = Sample();
			town.ParticipantIdx = -1;
			town.WriteTo(sink);
			sink.Position = 0;

			Assert.Equal(-1, City.ReadFrom(sink).ParticipantIdx);
		}

		[Fact]
		public void ShortStreamReadsZerosWithoutThrowing()
		{
			MemoryStream sink = new MemoryStream();
			Sample().WriteTo(sink);
			byte[] cut = sink.ToArray().AsSpan(0, 5).ToArray();

			City copy = City.ReadFrom(new MemoryStream(cut));

			Assert.Equal(0x1234, copy.StructureMarkers0);
			Assert.Equal(0x8001, copy.StructureMarkers1);
			Assert.Equal(new Point(77, 0), copy.Place);
			Assert.Equal(0, copy.GrainTally);
			Assert.Equal(0, copy.LaborerMarkers2);
			Assert.Equal(new sbyte[] { 0, 0, 0 }, copy.CommerceTownIndices);
		}

		[Fact]
		public void OddByteAtEndOfStreamIsConsumedAndReadAsZero()
		{
			City copy = City.ReadFrom(new MemoryStream(new byte[] { 0x34 }));

			Assert.Equal(0, copy.StructureMarkers0);
			Assert.Equal(0, copy.StructureMarkers1);
		}

		[Fact]
		public void StructuresCombineBothMarkerWords()
		{
			City town = new City();
			town.Structures = 0x80010002;

			Assert.Equal(0x0002, town.StructureMarkers0);
			Assert.Equal(0x8001, town.StructureMarkers1);
			Assert.True(town.Has((CityStructure)1));
			Assert.True(town.Has((CityStructure)16));
			Assert.False(town.Has((CityStructure)2));

			town.Add((CityStructure)2);
			town.Remove((CityStructure)16);
			Assert.Equal(0x80000006u, town.Structures);
		}

		[Fact]
		public void SignExtendedStructureCheckUsesHighWordOnlyForBit15()
		{
			City town = new City { StructureMarkers0 = 0, StructureMarkers1 = 1 };

			Assert.True(town.HasStructureSignExtended(15));
			Assert.False(town.HasStructureSignExtended(14));
		}

		internal static void AssertSame(City expected, City actual)
		{
			Assert.Equal(expected.StructureMarkers0, actual.StructureMarkers0);
			Assert.Equal(expected.StructureMarkers1, actual.StructureMarkers1);
			Assert.Equal(expected.Place, actual.Place);
			Assert.Equal(expected.ConditionMarker, actual.ConditionMarker);
			Assert.Equal(expected.TrueExtent, actual.TrueExtent);
			Assert.Equal(expected.ShownExtent, actual.ShownExtent);
			Assert.Equal(expected.PresentMakingIdx, actual.PresentMakingIdx);
			Assert.Equal(expected.RootCommerce, actual.RootCommerce);
			Assert.Equal(expected.ParticipantIdx, actual.ParticipantIdx);
			Assert.Equal(expected.GrainTally, actual.GrainTally);
			Assert.Equal(expected.MaterialsTally, actual.MaterialsTally);
			Assert.Equal(expected.LaborerMarkers0, actual.LaborerMarkers0);
			Assert.Equal(expected.LaborerMarkers1, actual.LaborerMarkers1);
			Assert.Equal(expected.LaborerMarkers2, actual.LaborerMarkers2);
			Assert.Equal(expected.LabelIdx, actual.LabelIdx);
			Assert.Equal(expected.CommerceTownIndices, actual.CommerceTownIndices);
			Assert.Equal(expected.Unfamiliar, actual.Unfamiliar);
		}
	}
}
