using System.Drawing;
using Civilization1Remaster.GameModel.Rules;
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
			// The save game reader is lenient: missing bytes read as zero
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

	public class UnitSerializationTests
	{
		internal static Unit Sample() => new Unit
		{
			Condition = 0x7f,
			Place = new Point(79, 49),
			KindIdx = 26,
			LeftoverSteps = 3,
			UnusualSteps = -2,
			TravelTowardPlace = new Point(-1, -1),
			TravelTowardFollowingHeading = 4,
			ShownViaParticipant = 0x81,
			FollowingTroopIdx = -1,
			BaseTownIdx = 127,
		};

		[Fact]
		public void WritesTwelveBytes()
		{
			MemoryStream sink = new MemoryStream();
			Sample().WriteTo(sink);

			Assert.Equal(new byte[] { 0x7f, 79, 49, 26, 3, 0xfe, 0xff, 0xff, 4, 0x81, 0xff, 127 }, sink.ToArray());
		}

		[Fact]
		public void RoundTrip()
		{
			MemoryStream sink = new MemoryStream();
			Unit original = Sample();
			original.WriteTo(sink);
			sink.Position = 0;

			Unit copy = Unit.ReadFrom(sink);

			Assert.Equal(original.Condition, copy.Condition);
			Assert.Equal(original.Place, copy.Place);
			Assert.Equal(original.KindIdx, copy.KindIdx);
			Assert.Equal(original.LeftoverSteps, copy.LeftoverSteps);
			Assert.Equal(original.UnusualSteps, copy.UnusualSteps);
			Assert.Equal(original.TravelTowardPlace, copy.TravelTowardPlace);
			Assert.Equal(original.TravelTowardFollowingHeading, copy.TravelTowardFollowingHeading);
			Assert.Equal(original.ShownViaParticipant, copy.ShownViaParticipant);
			Assert.Equal(original.FollowingTroopIdx, copy.FollowingTroopIdx);
			Assert.Equal(original.BaseTownIdx, copy.BaseTownIdx);
		}

		[Fact]
		public void ConditionIsReadUnsigned()
		{
			MemoryStream sink = new MemoryStream();
			Unit troop = Sample();
			troop.Condition = 0xc0;
			troop.WriteTo(sink);
			sink.Position = 0;

			Assert.Equal(0xc0, Unit.ReadFrom(sink).Condition);
		}

		[Fact]
		public void EmptyStreamGivesZeroUnit()
		{
			Unit copy = Unit.ReadFrom(new MemoryStream());

			Assert.Equal(0, copy.Condition);
			Assert.Equal(new Point(0, 0), copy.Place);
			Assert.Equal(0, copy.FollowingTroopIdx);
		}

		[Fact]
		public void StatusBits()
		{
			Unit troop = new Unit { Condition = 0x0f };

			Assert.True(troop.InspectStatus(0x04));
			Assert.False(troop.InspectStatus(0x10));
			troop.WipeStatus(0x0c);
			Assert.Equal(0x0c, troop.Condition);
		}
	}

	public class UnitHealthTests
	{
		[Fact]
		public void FreshUnitsHaveFullHealth()
		{
			UnitHealth health = new UnitHealth();

			Assert.Equal(0, health.ObtainLostHitPoints(1, 2, 3));
			Assert.Equal(3, health.ObtainHitPoints(1, 2, 3, false));
			Assert.Equal(4, health.ObtainHitPoints(1, 2, 3, true));
			Assert.False(health.VerifyElite(1, 2, 3));
		}

		[Fact]
		public void DamageBelongsToTheUnitKind()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(1, 2, 3, 2, false);

			Assert.Equal(2, health.ObtainLostHitPoints(1, 2, 3));
			Assert.Equal(1, health.ObtainHitPoints(1, 2, 3, false));
			// a different unit now in the same slot is not damaged
			Assert.Equal(0, health.ObtainLostHitPoints(1, 2, 4));
		}

		[Fact]
		public void HitPointsNeverDropBelowOne()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(0, 0, 1, 99, false);

			Assert.Equal(CombatResolver.PeakStrikeMarks - 1, health.ObtainLostHitPoints(0, 0, 1));
			Assert.Equal(1, health.ObtainHitPoints(0, 0, 1, false));
		}

		[Fact]
		public void AceGetsFiveHitPoints()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(2, 5, 7, 1, true);

			Assert.True(health.VerifyElite(2, 5, 7));
			Assert.Equal(5, health.ObtainFullHitPoints(2, 5, 7, false));
			Assert.Equal(4, health.ObtainHitPoints(2, 5, 7, false));
		}

		[Fact]
		public void Mending()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(1, 1, 1, 3, false);

			Assert.True(health.MendOne(1, 1, 1));
			Assert.Equal(2, health.ObtainLostHitPoints(1, 1, 1));
			Assert.Equal(2, health.MendAmount(1, 1, 1, 5));
			Assert.False(health.MendOne(1, 1, 1));
			Assert.Equal(0, health.MendAmount(1, 1, 1, 1));
			Assert.False(health.MendOne(1, 1, 2));
		}

		[Fact]
		public void InvalidSlotsAreIgnored()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(8, 0, 1, 2, true);
			health.Assign(0, 128, 1, 2, true);
			health.Wipe(-1, 0);

			Assert.Equal(0, health.ObtainLostHitPoints(8, 0, 1));
		}

		[Fact]
		public void StoreAndImportRoundTrip()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(0, 0, 1, 1, false);
			health.Assign(7, 127, 25, 4, true);
			health.Assign(3, 64, 0, 2, true);

			MemoryStream sink = new MemoryStream();
			health.Store(sink);
			Assert.Equal(5 + 8 * 128 * 3, sink.Length);
			Assert.Equal("OCHP"u8.ToArray(), sink.ToArray().AsSpan(0, 4).ToArray());

			sink.Position = 0;
			UnitHealth copy = new UnitHealth();
			Assert.True(copy.Import(sink));

			Assert.Equal(1, copy.ObtainLostHitPoints(0, 0, 1));
			Assert.Equal(4, copy.ObtainLostHitPoints(7, 127, 25));
			Assert.True(copy.VerifyElite(7, 127, 25));
			Assert.Equal(2, copy.ObtainLostHitPoints(3, 64, 0));
			Assert.Equal(0, copy.ObtainLostHitPoints(3, 65, 0));
		}

		[Fact]
		public void ImportRejectsBadData()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(1, 1, 1, 1, false);
			MemoryStream sink = new MemoryStream();
			health.Store(sink);
			byte[] good = sink.ToArray();

			Assert.False(new UnitHealth().Import(new MemoryStream(good.AsSpan(0, good.Length - 1).ToArray())));

			byte[] badSignature = (byte[])good.Clone();
			badSignature[0] = (byte)'X';
			Assert.False(new UnitHealth().Import(new MemoryStream(badSignature)));

			byte[] badEdition = (byte[])good.Clone();
			badEdition[4] = 2;
			Assert.False(new UnitHealth().Import(new MemoryStream(badEdition)));
		}

		[Fact]
		public void ImportClampsDamage()
		{
			UnitHealth health = new UnitHealth();
			health.Assign(0, 0, 1, 1, false);
			MemoryStream sink = new MemoryStream();
			health.Store(sink);
			byte[] content = sink.ToArray();
			content[5 + 1] = 200;

			UnitHealth copy = new UnitHealth();
			Assert.True(copy.Import(new MemoryStream(content)));
			Assert.Equal(CombatResolver.PeakStrikeMarks - 1, copy.ObtainLostHitPoints(0, 0, 1));
		}
	}
}
