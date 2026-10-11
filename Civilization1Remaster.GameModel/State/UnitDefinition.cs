using System;

namespace Civilization1Remaster.Game.State
{
	public class UnitDefinition
	{
		// Total size: 34 bytes
		public string Label; // (12 bytes)
		public short AbortScience;
		public short LandGroup;
		public short ShiftTally;
		public short RoundsAway;
		public short AssaultPower;
		public short GuardPower;
		public short Price;
		public short VisionSpan;
		public short FerryRoom;
		public short TroopGroup;
		public short ScienceNeeded;

		public UnitDefinition(string label, short abortScience, short landGroup,
			short shiftTally, short roundsAway, short assaultPower, short guardPower,
			short price, short visionSpan, short ferryRoom, short troopGroup,
			short scienceNeeded)
		{
			this.Label = label;
			this.AbortScience = abortScience;
			this.LandGroup = landGroup;
			this.ShiftTally = shiftTally;
			this.RoundsAway = roundsAway;
			this.AssaultPower = assaultPower;
			this.GuardPower = guardPower;
			this.Price = price;
			this.VisionSpan = visionSpan;
			this.FerryRoom = ferryRoom;
			this.TroopGroup = troopGroup;
			this.ScienceNeeded = scienceNeeded;
		}
	}
}
