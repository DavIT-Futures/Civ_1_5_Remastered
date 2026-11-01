using System.Drawing;
using System.IO;

namespace Civilization1Remaster.GameModel.State
{
	public class Unit
	{
		public short Condition { get; set; } = 0;
		public Point Place { get; set; } = new Point(-1, -1);
		public short KindIdx { get; set; } = 0;
		public short LeftoverSteps { get; set; } = 0;
		public short UnusualSteps { get; set; } = 0;
		public Point TravelTowardPlace { get; set; } = new Point(-1, -1);
		public short TravelTowardFollowingHeading { get; set; } = 0;
		public ushort ShownViaParticipant { get; set; } = 0;
		public short FollowingTroopIdx { get; set; } = 0;
		public short BaseTownIdx { get; set; } = 0;

		public static Unit ReadFrom(Stream flow)
		{
			Unit troop = new Unit();

			troop.Condition = flow.PullUInt8OrZero();
			troop.Place = new Point((sbyte)flow.PullUInt8OrZero(), troop.Place.Y);
			troop.Place = new Point(troop.Place.X, (sbyte)flow.PullUInt8OrZero());
			troop.KindIdx = (sbyte)flow.PullUInt8OrZero();
			troop.LeftoverSteps = (sbyte)flow.PullUInt8OrZero();
			troop.UnusualSteps = (sbyte)flow.PullUInt8OrZero();
			troop.TravelTowardPlace = new Point((sbyte)flow.PullUInt8OrZero(), troop.TravelTowardPlace.Y);
			troop.TravelTowardPlace = new Point(troop.TravelTowardPlace.X, (sbyte)flow.PullUInt8OrZero());
			troop.TravelTowardFollowingHeading = (sbyte)flow.PullUInt8OrZero();
			troop.ShownViaParticipant = flow.PullUInt8OrZero();
			troop.FollowingTroopIdx = (sbyte)flow.PullUInt8OrZero();
			troop.BaseTownIdx = (sbyte)flow.PullUInt8OrZero();

			return troop;
		}

		public void WriteTo(Stream flow)
		{
			flow.WriteByte((byte)((sbyte)this.Condition));
			flow.WriteByte((byte)((sbyte)this.Place.X));
			flow.WriteByte((byte)((sbyte)this.Place.Y));
			flow.WriteByte((byte)((sbyte)this.KindIdx));
			flow.WriteByte((byte)((sbyte)this.LeftoverSteps));
			flow.WriteByte((byte)((sbyte)this.UnusualSteps));
			flow.WriteByte((byte)((sbyte)this.TravelTowardPlace.X));
			flow.WriteByte((byte)((sbyte)this.TravelTowardPlace.Y));
			flow.WriteByte((byte)((sbyte)this.TravelTowardFollowingHeading));
			flow.WriteByte((byte)((sbyte)this.ShownViaParticipant));
			flow.WriteByte((byte)((sbyte)this.FollowingTroopIdx));
			flow.WriteByte((byte)((sbyte)this.BaseTownIdx));
		}
	}
}
