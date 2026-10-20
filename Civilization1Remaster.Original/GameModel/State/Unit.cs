using System;
using System.Drawing;
using System.IO;

namespace Civilization1Original.Game.State
{
	public class Unit
	{
		public short Condition = 0;
		public Point Place = new Point(-1, -1);
		public short KindIdx = 0;
		public short LeftoverSteps = 0;
		public short UnusualSteps = 0;
		public Point TravelTowardPlace = new Point(-1, -1);
		public short TravelTowardFollowingHeading = 0;
		public ushort ShownViaParticipant = 0;
		public short FollowingTroopIdx = 0;
		public short BaseTownIdx = 0;

		public void WipeStatus(short condition)
		{
			this.Condition &= condition;
		}

		public bool InspectStatus(short condition)
		{
			return (this.Condition & condition) != 0;
		}

		public static Unit ReadFrom(Stream flow)
		{
			Unit troop = new Unit();

			troop.Condition = StreamIo.PullUInt8(flow);
			troop.Place.X = (sbyte)StreamIo.PullUInt8(flow);
			troop.Place.Y = (sbyte)StreamIo.PullUInt8(flow);
			troop.KindIdx = (sbyte)StreamIo.PullUInt8(flow);
			troop.LeftoverSteps = (sbyte)StreamIo.PullUInt8(flow);
			troop.UnusualSteps = (sbyte)StreamIo.PullUInt8(flow);
			troop.TravelTowardPlace.X = (sbyte)StreamIo.PullUInt8(flow);
			troop.TravelTowardPlace.Y = (sbyte)StreamIo.PullUInt8(flow);
			troop.TravelTowardFollowingHeading = (sbyte)StreamIo.PullUInt8(flow);
			troop.ShownViaParticipant = StreamIo.PullUInt8(flow);
			troop.FollowingTroopIdx = (sbyte)StreamIo.PullUInt8(flow);
			troop.BaseTownIdx = (sbyte)StreamIo.PullUInt8(flow);

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
