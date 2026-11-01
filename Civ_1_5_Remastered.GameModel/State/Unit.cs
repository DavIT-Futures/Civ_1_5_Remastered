using System.Drawing;
using System.IO;

namespace Civ_1_5_Remastered.GameModel.State
{
	public class Unit
	{
		public short Condition { get; set; } = 0;
		public Point Place { get; set; } = new Point(-1, -1);
		public short KindIndex { get; set; } = 0;
		public short LeftoverSteps { get; set; } = 0;
		public short UnusualSteps { get; set; } = 0;
		public Point TravelTowardPlace { get; set; } = new Point(-1, -1);
		public short TravelTowardFollowingHeading { get; set; } = 0;
		public ushort ShownViaParticipant { get; set; } = 0;
		public short FollowingTroopIndex { get; set; } = 0;
		public short BaseTownIndex { get; set; } = 0;

		public static Unit ReadFrom(Stream flow)
		{
			Unit troop = new Unit();

			troop.Condition = flow.PullByteOrZero();
			troop.Place = new Point((sbyte)flow.PullByteOrZero(), troop.Place.Y);
			troop.Place = new Point(troop.Place.X, (sbyte)flow.PullByteOrZero());
			troop.KindIndex = (sbyte)flow.PullByteOrZero();
			troop.LeftoverSteps = (sbyte)flow.PullByteOrZero();
			troop.UnusualSteps = (sbyte)flow.PullByteOrZero();
			troop.TravelTowardPlace = new Point((sbyte)flow.PullByteOrZero(), troop.TravelTowardPlace.Y);
			troop.TravelTowardPlace = new Point(troop.TravelTowardPlace.X, (sbyte)flow.PullByteOrZero());
			troop.TravelTowardFollowingHeading = (sbyte)flow.PullByteOrZero();
			troop.ShownViaParticipant = flow.PullByteOrZero();
			troop.FollowingTroopIndex = (sbyte)flow.PullByteOrZero();
			troop.BaseTownIndex = (sbyte)flow.PullByteOrZero();

			return troop;
		}

		public void WriteTo(Stream flow)
		{
			flow.WriteByte((byte)((sbyte)this.Condition));
			flow.WriteByte((byte)((sbyte)this.Place.X));
			flow.WriteByte((byte)((sbyte)this.Place.Y));
			flow.WriteByte((byte)((sbyte)this.KindIndex));
			flow.WriteByte((byte)((sbyte)this.LeftoverSteps));
			flow.WriteByte((byte)((sbyte)this.UnusualSteps));
			flow.WriteByte((byte)((sbyte)this.TravelTowardPlace.X));
			flow.WriteByte((byte)((sbyte)this.TravelTowardPlace.Y));
			flow.WriteByte((byte)((sbyte)this.TravelTowardFollowingHeading));
			flow.WriteByte((byte)((sbyte)this.ShownViaParticipant));
			flow.WriteByte((byte)((sbyte)this.FollowingTroopIndex));
			flow.WriteByte((byte)((sbyte)this.BaseTownIndex));
		}
	}
}
