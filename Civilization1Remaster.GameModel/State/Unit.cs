using System;
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

			troop.Condition = StreamIo.PullUInt8(flow);
			troop.Place = new Point((sbyte)StreamIo.PullUInt8(flow), troop.Place.Y);
			troop.Place = new Point(troop.Place.X, (sbyte)StreamIo.PullUInt8(flow));
			troop.KindIdx = (sbyte)StreamIo.PullUInt8(flow);
			troop.LeftoverSteps = (sbyte)StreamIo.PullUInt8(flow);
			troop.UnusualSteps = (sbyte)StreamIo.PullUInt8(flow);
			troop.TravelTowardPlace = new Point((sbyte)StreamIo.PullUInt8(flow), troop.TravelTowardPlace.Y);
			troop.TravelTowardPlace = new Point(troop.TravelTowardPlace.X, (sbyte)StreamIo.PullUInt8(flow));
			troop.TravelTowardFollowingHeading = (sbyte)StreamIo.PullUInt8(flow);
			troop.ShownViaParticipant = StreamIo.PullUInt8(flow);
			troop.FollowingTroopIdx = (sbyte)StreamIo.PullUInt8(flow);
			troop.BaseTownIdx = (sbyte)StreamIo.PullUInt8(flow);

			return troop;
		}

		public void WipeStatus(short condition)
		{
			this.Condition &= condition;
		}

		public bool InspectStatus(short condition)
		{
			return (this.Condition & condition) != 0;
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
