using System.Drawing;
using Civilization1Remaster.GameModel.State;

namespace Civilization1Remaster.Tests.GameModel
{
	public class UnitSerializationTests
	{
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
			Assert.Equal(original.KindIndex, copy.KindIndex);
			Assert.Equal(original.LeftoverSteps, copy.LeftoverSteps);
			Assert.Equal(original.UnusualSteps, copy.UnusualSteps);
			Assert.Equal(original.TravelTowardPlace, copy.TravelTowardPlace);
			Assert.Equal(original.TravelTowardFollowingHeading, copy.TravelTowardFollowingHeading);
			Assert.Equal(original.ShownViaParticipant, copy.ShownViaParticipant);
			Assert.Equal(original.FollowingTroopIndex, copy.FollowingTroopIndex);
			Assert.Equal(original.BaseTownIndex, copy.BaseTownIndex);
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
			Assert.Equal(0, copy.FollowingTroopIndex);
		}

		internal static Unit Sample() => new Unit
		{
			Condition = 0x7f,
			Place = new Point(79, 49),
			KindIndex = 26,
			LeftoverSteps = 3,
			UnusualSteps = -2,
			TravelTowardPlace = new Point(-1, -1),
			TravelTowardFollowingHeading = 4,
			ShownViaParticipant = 0x81,
			FollowingTroopIndex = -1,
			BaseTownIndex = 127,
		};
	}
}
