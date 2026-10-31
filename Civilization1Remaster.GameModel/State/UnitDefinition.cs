using System;

namespace Civilization1Remaster.Game.State
{
	public class UnitDefinition
	{
		// Total size: 34 bytes
		public string Label; // (12 bytes)

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

		public short AbortScience { get; set; }
		public short LandGroup { get; set; }
		public short ShiftTally { get; set; }
		public short RoundsAway { get; set; }
		public short AssaultPower { get; set; }
		public short GuardPower { get; set; }
		public short Price { get; set; }
		public short VisionSpan { get; set; }
		public short FerryRoom { get; set; }
		public short TroopGroup { get; set; }
		public short ScienceNeeded { get; set; }
	}
}
