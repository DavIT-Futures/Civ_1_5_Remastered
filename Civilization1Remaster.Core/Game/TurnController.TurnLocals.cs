using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	partial class TurnController
	{
		/// <summary>
		/// The locals of HandlePlayerTurn, shared by the methods it calls as in the original. In the original they were the
		/// words of the stack frame (BP - offset); the offset of each one is in its comment, the comments of the methods still
		/// name the locals by these offsets. Every local is a word; byte reads take its low byte, byte writes keep its high byte.
		/// B3 (2026-10-26): a C# object made for every turn, every value 0 at the start like the zeroed frame of the remaster
		/// (the "stays on the stack" remarks below tell what the classic code left there for later functions).
		/// </summary>
		private sealed class TurnLocals
		{
			private ushort dot;
			private ushort markedTileMode;
			private ushort humanTookKey;
			private ushort laterPass;
			private ushort aITries;
			private ushort clicked;
			private ushort cursorHoriz;
			private ushort city;
			private ushort cursorVert;
			private ushort cargo;
			private ushort otherUnit;
			private ushort targetHoriz;
			private ushort allMoved;
			private ushort humanMarkers;
			private ushort carrierHoriz;
			private ushort direction;
			private ushort targetVert;
			private ushort ordered;
			private ushort carrierVert;
			private ushort unitHoriz;
			private ushort pastWaiting;
			private ushort unitVert;
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

			/// <summary>0x2: the colour of a dot of the map view (stays on the stack: ShowTravellersReport reads it)</summary>
			public ushort Dot
			{
				get => this.dot;
				set => this.dot = value;
			}

			/// <summary>0x4: 1 while the marked tile moves instead of a unit</summary>
			public ushort MarkedTileMode { get => this.markedTileMode; set => this.markedTileMode = value; }

			/// <summary>0x6: a unit of the human took a key in this turn</summary>
			public ushort HumanTookKey { get => this.humanTookKey; set => this.humanTookKey = value; }

			/// <summary>0x8: not the first pass over the units</summary>
			public ushort LaterPass { get => this.laterPass; set => this.laterPass = value; }

			/// <summary>0xa: the orders asked from ShiftAIUnit for the unit</summary>
			public ushort AITries { get => this.aITries; set => this.aITries = value; }

			/// <summary>0xc: a click is waiting</summary>
			public ushort Clicked { get => this.clicked; set => this.clicked = value; }

			/// <summary>0xe: the column of the marked tile or of the click (-1 none)</summary>
			public ushort CursorHoriz { get => this.cursorHoriz; set => this.cursorHoriz = value; }

			/// <summary>0x10: a city</summary>
			public ushort City { get => this.city; set => this.city = value; }

			/// <summary>0x12: the row of the marked tile or of the click</summary>
			public ushort CursorVert
			{
				get => this.cursorVert;
				set => this.cursorVert = value;
			}

			/// <summary>0x14: the cargo unit taken along</summary>
			public ushort Cargo { get => this.cargo; set => this.cargo = value; }

			/// <summary>0x18: another unit (at the tile, the best guardian), or a flag</summary>
			public ushort OtherUnit { get => this.otherUnit; set => this.otherUnit = value; }

			/// <summary>0x1c: the column of the target tile</summary>
			public ushort TargetHoriz { get => this.targetHoriz; set => this.targetHoriz = value; }

			/// <summary>0x1e: 1 unless a unit is left with moves in this pass</summary>
			public ushort AllMoved { get => this.allMoved; set => this.allMoved = value; }

			/// <summary>0x20: the players played by humans at the start of the turn</summary>
			public ushort HumanMarkers { get => this.humanMarkers; set => this.humanMarkers = value; }

			/// <summary>0x22: the column of the carrier before the move</summary>
			public ushort CarrierHoriz { get => this.carrierHoriz; set => this.carrierHoriz = value; }

			/// <summary>0x24: the direction of the move</summary>
			public ushort Direction
			{
				get => this.direction;
				set => this.direction = value;
			}

			/// <summary>0x26: the row of the target tile</summary>
			public ushort TargetVert { get => this.targetVert; set => this.targetVert = value; }

			/// <summary>0x28: the unit got an order</summary>
			public ushort Ordered { get => this.ordered; set => this.ordered = value; }

			/// <summary>0x2a: the row of the carrier before the move</summary>
			public ushort CarrierVert { get => this.carrierVert; set => this.carrierVert = value; }

			/// <summary>0x2c: the column of the unit</summary>
			public ushort UnitHoriz { get => this.unitHoriz; set => this.unitHoriz = value; }

			/// <summary>0x2e: the pass went past the waiting unit 0x46</summary>
			public ushort PastWaiting { get => this.pastWaiting; set => this.pastWaiting = value; }

			/// <summary>0x30: the row of the unit</summary>
			public ushort UnitVert { get => this.unitVert; set => this.unitVert = value; }

			/// <summary>0x32: the unit (0x80 none)</summary>
			public ushort Troop { get => this.troop; set => this.troop = value; }

			/// <summary>0x34: the terrain the unit stands on</summary>
			public ushort StartTerrain { get => this.startTerrain; set => this.startTerrain = value; }

			/// <summary>0x36: the mouse cursor was changed</summary>
			public ushort MouseCursorSet
			{
				get => this.mouseCursorSet;
				set => this.mouseCursorSet = value;
			}

			/// <summary>0x38: something was found</summary>
			public ushort Found { get => this.found; set => this.found = value; }

			/// <summary>0x3a: a result used for many things (a city, a battle, a dialog answer, a rate)</summary>
			public ushort Result { get => this.result; set => this.result = value; }

			/// <summary>0x3c: a counter or a city</summary>
			public ushort Count { get => this.count; set => this.count = value; }

			/// <summary>0x3e: the terrain of the target tile</summary>
			public ushort TargetTerrain
			{
				get => this.targetTerrain;
				set => this.targetTerrain = value;
			}

			/// <summary>0x40: the improvements of the target tile (low byte)</summary>
			public ushort TargetImprovements { get => this.targetImprovements; set => this.targetImprovements = value; }

			/// <summary>0x44: the order (a key)</summary>
			public ushort Order { get => this.order; set => this.order = value; }

			/// <summary>0x46: the unit that waits for the next pass</summary>
			public ushort WaitingTroop { get => this.waitingTroop; set => this.waitingTroop = value; }

			/// <summary>0x48: the owner of the target tile</summary>
			public ushort TileOwner { get => this.tileOwner; set => this.tileOwner = value; }

			/// <summary>0x4a: the cargo units counted</summary>
			public ushort CargoTally { get => this.cargoTally; set => this.cargoTally = value; }

			/// <summary>0x4c: the next cargo unit</summary>
			public ushort NextCargo { get => this.nextCargo; set => this.nextCargo = value; }

			/// <summary>0x4e: the row, status or kind of the unit</summary>
			public ushort UnitDetail { get => this.unitDetail; set => this.unitDetail = value; }

			/// <summary>0x52: the kind of the carrier (low byte)</summary>
			public ushort CarrierKind { get => this.carrierKind; set => this.carrierKind = value; }
		}
	}
}
