namespace Civilization1Remaster.Core.Game.Turns
{
	public partial class TurnController
	{
		private sealed class TurnLocals
		{
			private ushort dot;
			private ushort markedTileMode;
			private ushort humanTookKey;
			private ushort laterPass;
			private ushort computerTries;
			private ushort clicked;
			private ushort cursorHorizontal;
			private ushort city;
			private ushort cursorVertical;
			private ushort cargo;
			private ushort otherUnit;
			private ushort targetHorizontal;
			private ushort allMoved;
			private ushort humanMarkers;
			private ushort carrierHorizontal;
			private ushort direction;
			private ushort targetVertical;
			private ushort ordered;
			private ushort carrierVertical;
			private ushort unitHorizontal;
			private ushort pastWaiting;
			private ushort unitVertical;
			private ushort troop;
			private ushort startTerrain;
			private ushort mouseCursorSet;
			private ushort found;
			private ushort result;
			private ushort count;
			private ushort targetTerrain;
			private ushort targetImprovements;
			private ushort order;
			private ushort waitingTroop;
			private ushort tileOwner;
			private ushort cargoTally;
			private ushort nextCargo;
			private ushort unitDetail;
			private ushort carrierKind;

			public ushort Dot
			{
				get => this.dot;
				set => this.dot = value;
			}

			public ushort MarkedTileMode { get => this.markedTileMode; set => this.markedTileMode = value; }

			public ushort HumanTookKey { get => this.humanTookKey; set => this.humanTookKey = value; }

			public ushort LaterPass { get => this.laterPass; set => this.laterPass = value; }

			public ushort ComputerTries { get => this.computerTries; set => this.computerTries = value; }

			public ushort Clicked { get => this.clicked; set => this.clicked = value; }

			public ushort CursorHorizontal { get => this.cursorHorizontal; set => this.cursorHorizontal = value; }

			public ushort City { get => this.city; set => this.city = value; }

			public ushort CursorVertical
			{
				get => this.cursorVertical;
				set => this.cursorVertical = value;
			}

			public ushort Cargo { get => this.cargo; set => this.cargo = value; }

			public ushort OtherUnit { get => this.otherUnit; set => this.otherUnit = value; }

			public ushort TargetHorizontal { get => this.targetHorizontal; set => this.targetHorizontal = value; }

			public ushort AllMoved { get => this.allMoved; set => this.allMoved = value; }

			public ushort HumanMarkers { get => this.humanMarkers; set => this.humanMarkers = value; }

			public ushort CarrierHorizontal { get => this.carrierHorizontal; set => this.carrierHorizontal = value; }

			public ushort Direction
			{
				get => this.direction;
				set => this.direction = value;
			}

			public ushort TargetVertical { get => this.targetVertical; set => this.targetVertical = value; }

			public ushort Ordered { get => this.ordered; set => this.ordered = value; }

			public ushort CarrierVertical { get => this.carrierVertical; set => this.carrierVertical = value; }

			public ushort UnitHorizontal { get => this.unitHorizontal; set => this.unitHorizontal = value; }

			public ushort PastWaiting { get => this.pastWaiting; set => this.pastWaiting = value; }

			public ushort UnitVertical { get => this.unitVertical; set => this.unitVertical = value; }

			public ushort Troop { get => this.troop; set => this.troop = value; }

			public ushort StartTerrain { get => this.startTerrain; set => this.startTerrain = value; }

			public ushort MouseCursorSet
			{
				get => this.mouseCursorSet;
				set => this.mouseCursorSet = value;
			}

			public ushort Found { get => this.found; set => this.found = value; }

			public ushort Result { get => this.result; set => this.result = value; }

			public ushort Count { get => this.count; set => this.count = value; }

			public ushort TargetTerrain
			{
				get => this.targetTerrain;
				set => this.targetTerrain = value;
			}

			public ushort TargetImprovements { get => this.targetImprovements; set => this.targetImprovements = value; }

			public ushort Order { get => this.order; set => this.order = value; }

			public ushort WaitingTroop { get => this.waitingTroop; set => this.waitingTroop = value; }

			public ushort TileOwner { get => this.tileOwner; set => this.tileOwner = value; }

			public ushort CargoTally { get => this.cargoTally; set => this.cargoTally = value; }

			public ushort NextCargo { get => this.nextCargo; set => this.nextCargo = value; }

			public ushort UnitDetail { get => this.unitDetail; set => this.unitDetail = value; }

			public ushort CarrierKind { get => this.carrierKind; set => this.carrierKind = value; }
		}
	}
}
