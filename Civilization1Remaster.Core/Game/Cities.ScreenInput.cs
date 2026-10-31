
using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The input of the city screen: buying, selling, the specialists, the tiles, the units</summary>
	partial class Cities
	{
		/// <summary>What the city screen does after a button</summary>
		private enum CityScreenStep
		{
			/// <summary>The button wasn't used: the buttons after it</summary>
			Others,
			/// <summary>The city screen again from the citizens (the advisor switched, another tab)</summary>
			Redraw,
			/// <summary>The city from its start (the advisor gave up with 99, a purchase, a change, the view of the city)</summary>
			Restart,
			/// <summary>No click at all: the end of the city screen</summary>
			Leave,
			/// <summary>A line of the list clicked: selling the improvement</summary>
			Sell,
		}

		/// <summary>
		/// Waits until the mouse button is let go, then for a click, a key or the flag 0xb8; meanwhile a blinking Change button
		/// (0xc0, counted up) changes its colours every 10 ticks.
		/// </summary>
		private void WaitForCityScreenInput()
		{
			this.Owner.Startup.ShowMouseCursor();

			while (this.Owner.GlobDb3a != 0)
				this.Owner.Startup.ReadMouseState();

			while (this.Owner.GlobDb3a == 0)
			{
				if ((ushort)this.Owner.Libc.KeyAvailable() != 0 || this.round.CursorOn != 0)
					break;

				this.Owner.Startup.ReadMouseState();

				if (this.round.ChangeBlinks == 0)
					continue;

				this.Owner.Startup.HideMouseCursor();
				this.round.ChangeBlinks = (ushort)(this.round.ChangeBlinks + 1);

				if (((byte)this.round.ChangeBlinks & 1) != 0)
					this.Owner.VideoCard.SubstituteHue(ScreenBox, 231, 106, 32, 8, Hues.Yellow, Hues.LightBlue);
				else
					this.Owner.VideoCard.SubstituteHue(ScreenBox, 231, 106, 32, 8, Hues.LightBlue, Hues.Yellow);

				this.Owner.Startup.ShowMouseCursor();
				this.Owner.TextPainter.WaitForTime(10);
			}
		}

		/// <summary>
		/// The input of the city screen (0xf0: the key, -1 for a click, 0x70 with the flag 0xb8) and the Change button (a click
		/// on it or A): the advisor flag 0x10 switched; switched on, the advisor picks the production (0x102), the units in making
		/// of the owner are counted over again. 0x70dc and 0x70de get back the words 0x10a and 0xa of the frame.
		/// </summary>
		private CityScreenStep ReadCityScreenInput(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.Key = 0xffff;
			if ((ushort)this.Owner.Libc.KeyAvailable() != 0)
			{
				this.round.Key = this.Owner.Dialogs.ReadNavigationKey();
				this.Owner.GlobDb3e = 0;
				this.Owner.GlobDb3c = 0;
			}
			else if (this.round.CursorOn != 0)
			{
				this.round.Key = 0x70;
			}

			this.Owner.Startup.HideMouseCursor();

			bool clicked = this.Owner.GlobDb3a == 2 && (short)this.Owner.GlobDb3c >= 0xe6 && (short)this.Owner.GlobDb3c < 0x10e &&
				(short)this.Owner.GlobDb3e >= 0x6a && (short)this.Owner.GlobDb3e <= 0x74;

			if (!clicked && this.round.Key != 'A')
				return CityScreenStep.Others;

			town.ConditionMarker ^= 0x10;

			if ((town.ConditionMarker & 0x10) != 0)
			{
				short owner = (short)CityOwner;

				this.round.AdvisorPick = this.Owner.Advisors.PickCityProduction(owner, townIdx);
				DrawingBox.TypefaceIdx = 2;

				if (this.round.AdvisorPick == 0x63)
					return CityScreenStep.Restart;

				CityAgain = 0;

				StopMakingUnit(town, owner);
				town.PresentMakingIdx = (sbyte)(byte)this.round.AdvisorPick;
				StartMakingUnit(town, owner);

				this.Owner.Services.FloodBox(ScreenBox, 230, 99, 90, 100, Hues.Black);
			}

			RestoreShieldsAndTrade();

			return CityScreenStep.Redraw;
		}

		/// <summary>
		/// The other buttons of the city screen (0xf0 the key, -1 for a click; a click only in the row of the buttons, 0x6a -
		/// 0x74): Buy (right of 0x128 or B), Change (0xe6 - 0x10e or C), the tabs (I, H, M, V or a click at 0x60 - 0xe0, 0x2496;
		/// the view of the city (3) is shown at once). Others: none of them.
		/// </summary>
		private CityScreenStep HandleCityScreenButtons(short townIdx)
		{
			ushort key = this.round.Key;
			short horiz = (short)this.Owner.GlobDb3c;
			short vert = (short)this.Owner.GlobDb3e;

			if (key == 0xffff && !(this.Owner.GlobDb3a == 1 && vert >= 0x6a && vert <= 0x74))
				return CityScreenStep.Others;

			if (horiz >= 0x128 || key == 'b')
			{
				BuyCityProduction(townIdx);
				return CityScreenStep.Restart;
			}

			if ((horiz >= 0xe6 && horiz < 0x10e) || key == 'c')
			{
				ChangeCityProduction(townIdx);
				return CityScreenStep.Restart;
			}

			ushort tab;

			if (key == 'i')
				tab = 0;
			else if (key == 'h')
				tab = 1;
			else if (key == 'm')
				tab = 2;
			else if (key == 'v')
				tab = 3;
			else if (horiz >= 0x60 && horiz < 0xe0)
				tab = (ushort)((horiz - 0x60) >> 5);
			else
				return CityScreenStep.Others;

			CityScreenTab = tab;
			RestoreShieldsAndTrade();

			if (CityScreenTab != 3)
				return CityScreenStep.Redraw;

			// The view of the city
			DrawingBox.TypefaceIdx = 1;
			this.Owner.Words.Clear();
			this.Owner.CityScene.ShowCityScreen(townIdx, -1);
			MapRedrawRequest = 1;
			CityScreenTab = 0;

			return CityScreenStep.Restart;
		}

		/// <summary>
		/// Buy: 0x106 the price, 0xe8 the cost of the shields missing (0 - 999 of them): a unit their double and a twentieth
		/// of their square, an improvement their double, a wonder (or a spaceship part) four times them; twice that with no
		/// shields. A menu with the cost and the treasury (in civil disorder a unit can't be bought); Yes buys it.
		/// </summary>
		private void BuyCityProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			sbyte making = town.PresentMakingIdx;

			if (making >= 0)
			{
				this.round.Selected = UnitKindWord(making, UnitKindTable.Price);
				short missing = (short)this.Owner.Helpers.ClampValue((10 * (short)this.round.Selected) - town.MaterialsTally, 0, 999);
				this.round.Value = (ushort)((short)(missing * missing) / 20 + (missing * 2));
			}
			else
			{
				this.round.Selected = BuildingWord(-making, BuildingTable.Price);
				this.round.Value = (ushort)((ushort)this.Owner.Helpers.ClampValue((10 * (short)this.round.Selected) - town.MaterialsTally, 0, 999) << 1);

				if (-making > 0x15)
					this.round.Value = (ushort)(this.round.Value << 1);
			}

			if (town.MaterialsTally == 0)
				this.round.Value = (ushort)(this.round.Value << 1);

			short cost = (short)this.round.Value;

			Message("Cost to complete\n")
				.Text((ushort)(making >= 0 ? (making * 0x22) + 0x112a : (-making * 0x1e) + 0xb9a))
				.Text(": $")
				.Number(cost)
				.Text("\nTreasury: $")
				.Number(this.Owner.World.Participants[owner].Gold)
				.Text("\n");

			if (making >= 0 && (town.ConditionMarker & 1) != 0)
				this.Owner.Words.Append("CIVIL DISORDER\n");
			else if (this.Owner.World.Participants[owner].Gold >= cost)
				this.Owner.Words.Append(" Yes\n No\n");

			this.Owner.Startup.ShowMouseCursor();
			DrawingBox.TypefaceIdx = 1;
			if (this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 100, 80, 1) == 0 && this.Owner.World.Participants[owner].Gold >= (short)this.round.Value)
			{
				town.MaterialsTally = (short)(10 * (short)this.round.Selected);
				this.Owner.World.Participants[owner].Gold -= (short)this.round.Value;
			}

			this.Owner.Startup.HideMouseCursor();
		}

		/// <summary>Change: the advisor flag 0x10 off, the human picks the production, the units in making counted over again</summary>
		private void ChangeCityProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			town.ConditionMarker &= 0xef;
			this.Owner.Startup.ShowMouseCursor();
			RepickCityProduction(townIdx);
			this.Owner.Startup.HideMouseCursor();
		}

		/// <summary>The unit of the list of the units in the city (the words before the frame at -0x74)</summary>
		private ushort UnitOfCityList(ushort place) =>
			this.round.Frame(0x74 - (place << 1));

		/// <summary>
		/// A: a cursor on the units in the city (0x4e of them, six in a row; 0x106 the unit, from the first that sleeps or
		/// is fortified or busy; 0xfa / 0x100 its place) moved by the arrows; Enter or Space wakes the unit (a sleeping or
		/// fortified one gets its moves back) and clears its goal; Escape ends it (the city screen again).
		/// </summary>
		private void ActivateUnitsInCity()
		{
			short owner = (short)CityOwner;
			ushort Unit(ushort place) => UnitMemory.Address(owner, UnitOfCityList(place));

			this.round.Selected = 0;

			while ((this.Processor.PullUInt8(this.Processor.SegData.Value16, Unit(this.round.Selected)) & 0xcf) == 0 &&
				(short)this.round.Selected < (short)this.round.UnitsDrawn)
			{
				this.round.Selected = (ushort)(this.round.Selected + 1);
			}

			if (this.round.Selected == this.round.UnitsDrawn)
				this.round.Selected = 0;

			while (true)
			{
				short place = (short)this.round.Selected;
				this.round.UnitHoriz = (ushort)(((place % 6) * 18) + 100);
				this.round.UnitVert = (ushort)(((place / 6) << 4) + 0x74);

				do
				{
					FillBoxWithMotif((short)this.round.UnitHoriz, (short)this.round.UnitVert, 16, 16);
					this.Owner.TextPainter.WaitForTime(10);
					this.Owner.MapOps.DrawUnitSprite(owner, (short)UnitOfCityList(this.round.Selected),
						(short)this.round.UnitHoriz, (short)this.round.UnitVert);
					this.Owner.TextPainter.WaitForTime(10);
				}
				while ((ushort)this.Owner.Libc.KeyAvailable() == 0);

				ushort key = this.Owner.Dialogs.ReadNavigationKey();
				this.round.DrawVert = key;
				ushort current = this.round.Selected;
				this.round.Index = key switch
				{
					0x4800 => (ushort)(current - 6),
					0x4b00 => (ushort)(current - 1),
					0x4d00 => (ushort)(current + 1),
					0x5000 => (ushort)(current + 6),
					_ => current,
				};

				this.round.Selected = (ushort)this.Owner.Helpers.ClampValue((short)this.round.Index, 0, (short)this.round.UnitsDrawn - 1);

				if (key == KeyCodes.Enter || key == KeyCodes.Space)
					WakeUnitOfCity(Unit(this.round.Selected));

				if (key == KeyCodes.Escape)
					break;
			}

			RestoreShieldsAndTrade();
		}

		/// <summary>
		/// S (when nothing was sold this turn, the flag 0x80): the improvement 0x106 of the list (0xb4 of them, from the line
		/// 0x40) blinks; the arrows up and down move it (another key keeps 0xea as it was), Enter or Space sells it, Escape
		/// gives up. True: sell it.
		/// </summary>
		private bool PickImprovementToSell(short townIdx)
		{
			if (this.round.Key != 's' || (this.Owner.World.Towns[townIdx].ConditionMarker & 0x80) != 0)
				return false;

			this.round.Selected = 0;

			while (true)
			{
				do
				{
					short line = (short)((((short)this.round.Selected + (short)this.round.WonderRows) * 6) + 3);
					this.Owner.VideoCard.SubstituteHue(ScreenBox, 252, line, 56, 7, Hues.LightBlue, Hues.Blue);
					this.Owner.TextPainter.WaitForTime(10);
					this.Owner.VideoCard.SubstituteHue(ScreenBox, 252, line, 56, 7, Hues.Blue, Hues.LightBlue);
					this.Owner.TextPainter.WaitForTime(10);
				}
				while ((ushort)this.Owner.Libc.KeyAvailable() == 0);

				ushort key = this.Owner.Dialogs.ReadNavigationKey();
				this.round.DrawVert = key;

				if (key == KeyCodes.Up)
					this.round.Index = (ushort)(this.round.Selected - 1);
				else if (key == KeyCodes.Down)
					this.round.Index = (ushort)(this.round.Selected + 1);

				this.round.Selected = (ushort)this.Owner.Helpers.ClampValue((short)this.round.Index, 0, (short)this.round.ImprovementCount - 1);

				if (key == KeyCodes.Enter || key == KeyCodes.Space)
					return true;

				if (key == KeyCodes.Escape)
					return false;
			}
		}

		/// <summary>
		/// The keys 1 - 9: the specialist 0xea (of the 0x8) becomes the next type (entertainer, taxman, scientist, again);
		/// a city smaller than five gets a message instead (and starts again), a key past the specialists starts it again.
		/// </summary>
		private CityScreenStep ToggleSpecialist(short townIdx)
		{
			short key = (short)this.round.Key;

			if (key < 0x31 || key > 0x39)
				return CityScreenStep.Others;

			this.round.Index = (ushort)(key - 0x31);

			if ((short)this.round.Index >= (short)this.round.Specialists)
				return CityScreenStep.Restart;

			if (this.Owner.World.Towns[townIdx].TrueExtent < 5)
				return RefuseSpecialists();

			return CycleSpecialist();
		}

		/// <summary>
		/// P (or the flag 0xb8 of the map cursor, set here with the cursor in the middle): a box blinks on the square 0xb6 /
		/// 0xbe of the map of the city; the eight directions move it within the city's area (no more than two squares each way,
		/// not a corner) onto squares the human has seen, else it stays (0xf6 / 0xfc the old place, 0xe8 the key). True:
		/// Enter or Space picked the square (0xf6 / 0xfc); false: Escape (the flag cleared).
		/// </summary>
		private bool MoveCityMapCursor(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (this.round.CursorOn == 0)
			{
				this.round.CursorVert = 0;
				this.round.CursorHoriz = 0;
				this.round.CursorOn = 1;
			}

			while (true)
			{
				do
				{
					ushort horiz = (ushort)((this.round.CursorHoriz << 4) + 0xa0);
					ushort vert = (ushort)((this.round.CursorVert << 4) + 0x38);

					this.Owner.VideoCard.RenderImage(ScreenBox, horiz, vert, 16, 16, BackScreenBox, 0, 0);
					this.Owner.Dialogs.DrawBox((short)this.round.CursorHoriz * 16 + 160, (short)this.round.CursorVert * 16 + 56, 15, 15, Hues.White);
					this.Owner.TextPainter.WaitForTime(10);
					this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 16, 16, ScreenBox, horiz, vert);
					this.Owner.TextPainter.WaitForTime(10);
					MapRedrawRequest = 1;
				}
				while ((ushort)this.Owner.Libc.KeyAvailable() == 0);

				this.round.DrawHoriz = this.round.CursorHoriz;
				this.round.DrawVert = this.round.CursorVert;
				ushort key = this.Owner.Dialogs.ReadNavigationKey();
				this.round.Value = key;

				// The keys of the number pad: 7 8 9 / 4 6 / 1 2 3
				(int horiz, int vert) step = key switch
				{
					0x4700 => (-1, -1),
					0x4800 => (0, -1),
					0x4900 => (1, -1),
					0x4b00 => (-1, 0),
					0x4d00 => (1, 0),
					0x4f00 => (-1, 1),
					0x5000 => (0, 1),
					0x5100 => (1, 1),
					_ => (0, 0),
				};

				this.round.CursorHoriz = (ushort)(this.round.CursorHoriz + step.horiz);
				this.round.CursorVert = (ushort)(this.round.CursorVert + step.vert);

				short across = (short)this.round.CursorHoriz;
				short down = (short)this.round.CursorVert;
				bool allowed = Math.Abs((int)across) <= 2 && Math.Abs((int)down) <= 2 && Math.Abs((int)down) + Math.Abs((int)across) < 4 &&
					(this.Owner.World.ChartSight[(ushort)((short)town.Place.X + across), (ushort)((short)town.Place.Y + down)] &
						(short)(1 << (this.Owner.World.PersonParticipantIdx & 0xff))) != 0;

				if (!allowed)
				{
					this.round.CursorHoriz = this.round.DrawHoriz;
					this.round.CursorVert = this.round.DrawVert;
				}

				if (key == KeyCodes.Enter || key == KeyCodes.Space)
				{
					this.round.DrawHoriz = this.round.CursorHoriz;
					this.round.DrawVert = this.round.CursorVert;
					return true;
				}

				if (key == KeyCodes.Escape)
				{
					this.round.CursorOn = 0;
					return false;
				}
			}
		}

		/// <summary>
		/// M or the arrow of the list (when the list has more, 0xca bit 2): the other page of the list (0xca bit 0, 0xda the
		/// first from 0xfe); R or the name of the city: renaming it; a click into the list: 0x106 the line clicked (Sell, from
		/// the line 0x40). Leave: no click at all.
		/// </summary>
		private CityScreenStep HandleListAndRename(short townIdx)
		{
			ushort key = this.round.Key;
			short horiz = (short)this.Owner.GlobDb3c;
			short vert = (short)this.Owner.GlobDb3e;
			bool click = this.Owner.GlobDb3a == 1;

			// M: the other page of the list
			if (key == 'M' && ((byte)this.round.ListFlags & 2) != 0)
				return TurnListPage();

			// R or the name of the city
			if (key == 'r' || (click && horiz >= 0xe7 && vert > 0xbe && horiz < 0x10e))
			{
				this.Owner.TurnLogic.WaitForKey();
				DrawingBox.TypefaceIdx = 1;
				this.Owner.CityNaming.ShowCityLabelPrompt(townIdx);
				MapRedrawRequest = 1;
				return CityScreenStep.Restart;
			}

			if (!click)
				return CityScreenStep.Leave;

			// The arrow of the list
			if (horiz >= 0x120 && vert < 0x60 && vert > 0x58 && ((byte)this.round.ListFlags & 2) != 0)
				return TurnListPage();

			// A line of the list
			if (horiz >= 0x12c && vert < 0x5e)
			{
				this.round.Selected = (ushort)((short)(vert - 2) / 6 - this.round.WonderRows);
				return CityScreenStep.Sell;
			}

			return CityScreenStep.Others;
		}

		/// <summary>The other page of the list of improvements (0xca bit 0; 0xda the first one, from 0xfe)</summary>
		private CityScreenStep TurnListPage()
		{
			this.round.ListFlags = (ushort)(this.round.ListFlags ^ 1);
			this.round.ListStart = this.round.ListEnd;
			RestoreShieldsAndTrade();
			return CityScreenStep.Redraw;
		}

		/// <summary>
		/// Selling the improvement 0x106 of the list (counted among those the city has from 0xda, 0xbc the improvement), when
		/// nothing was sold this turn: a menu with its price (ten times its upkeep word); Yes adds the money, removes it, marks
		/// the city (0x80) and for the improvement 8 redraws the map (0x6b64). True: the menu was shown.
		/// </summary>
		private bool SellImprovement(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (ushort improvement = this.round.ListStart; (short)improvement < 0x18; improvement++)
			{
				this.round.Item = improvement;
				uint bit = 1u << (byte)improvement;

				if ((town.Structures & bit) == 0)
					continue;

				ushort line = this.round.Selected;
				this.round.Selected = (ushort)(line - 1);

				if (line != 0 || (town.ConditionMarker & 0x80) != 0)
					continue;

				ushort price = BuildingWord(improvement + 1, BuildingTable.Price);

				Message("Do you want to sell\nyour ")
					.Text(BuildingText(improvement))
					.Text(" for ")
					.Number((short)(0xa * (short)price))
					.Text("$?\n No.\n Yes.\n");
				DrawingBox.TypefaceIdx = 1;
				this.Owner.Startup.ShowMouseCursor();
				if (this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 128, 80, 1) == 1)
				{
					this.Owner.World.Participants[CityOwner].Gold += (short)(0xa * price);
					town.Structures -= bit;
					town.ConditionMarker |= 0x80;

					if (improvement + 1 == 8)
						MapRedrawRequest = 1;
				}

				this.Owner.Startup.HideMouseCursor();
				return true;
			}

			this.round.Item = (ushort)Math.Max((short)this.round.ListStart, (short)0x18);
			return false;
		}

		/// <summary>The specialist 0xea becomes the next type (entertainer, taxman, scientist, again): the city screen again</summary>
		private CityScreenStep CycleSpecialist()
		{
			this.round.Value = ObtainCitizenType(this.round.Index);
			AssignCitizenType(this.round.Index,
				(ushort)((short)this.round.Value < 3 ? this.round.Value + 1 : 1));

			RestoreShieldsAndTrade();

			return CityScreenStep.Redraw;
		}

		/// <summary>The message that a city smaller than five can't have taxmen or scientists: the city from its start</summary>
		private CityScreenStep RefuseSpecialists()
		{
			this.Owner.Words.Set("A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 32, 32, 1);

			return CityScreenStep.Restart;
		}

		/// <summary>
		/// A click on the citizens at the top (left of 0xc8, above 0x14): the specialist under it (0xea, by the width 0xf4 of
		/// an icon, after the 0x50 working ones) becomes the next type; elsewhere the city starts again.
		/// </summary>
		private CityScreenStep ClickCitizen(short townIdx)
		{
			if (this.Owner.World.Towns[townIdx].TrueExtent < 5)
				return RefuseSpecialists();

			short width = (short)this.round.Measure;

			if (width == 0)
				throw new Exception("Division by zero");

			int place = (short)(this.Owner.GlobDb3c - 0x10) / width;

			if (place != (short)place)
				throw new Exception("Division error");

			this.round.Index = (ushort)(place - (this.Owner.World.Towns[townIdx].TrueExtent - (short)this.round.Workers));

			if ((short)this.round.Index < 0 || (short)this.round.Index >= (short)this.round.Specialists)
				return CityScreenStep.Restart;

			return CycleSpecialist();
		}

		/// <summary>A unit of the city woken: a sleeping or fortified one gets its moves back, its goal is cleared</summary>
		private void WakeUnitOfCity(ushort unit)
		{
			if ((this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Status)) & 9) != 0)
			{
				sbyte kind = this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Kind));
				ushort moves = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((kind * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.ShiftTally));
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.LeftoverSteps), (byte)(3 * moves));
			}

			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Status),
				(byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.Status)) & 0x30));
			this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(unit + UnitMemory.TravelHoriz), 0xff);
		}

		/// <summary>
		/// A click on the units in the city (right of 0x64, below 0x74; six in a row of 16): the unit 0x106 under it is woken
		/// and the city screen drawn again. False: no unit there.
		/// </summary>
		private bool ClickUnitOfCity()
		{
			short row = (short)((short)(this.Owner.GlobDb3e - 0x74) / 16);
			short column = (short)((short)(this.Owner.GlobDb3c - 0x64) / 16);
			this.round.Selected = (ushort)(row * 6 + column);

			if ((short)this.round.Selected >= (short)this.round.UnitsDrawn)
				return false;

			short owner = (short)CityOwner;
			WakeUnitOfCity(UnitMemory.Address(owner, UnitOfCityList(this.round.Selected)));

			RestoreShieldsAndTrade();

			return true;
		}

		/// <summary>
		/// A square of the map of the city picked (0xf6 / 0xfc from the middle, 0xea its number): the middle takes all the
		/// workers off (the game puts them back); a square the owner has seen gets a worker or loses one (a free square only:
		/// the word of the frame for it is 0; 0xae the workers gained, 0x50 the working, 0xe8b8 the specialists, and the
		/// food, shields and trade of a square left are taken back). Redraw: the city screen again; Restart: the city again.
		/// </summary>
		private CityScreenStep ToggleCityTile(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.Index = ObtainCityTileIndex((short)this.round.DrawHoriz, (short)this.round.DrawVert);
			ushort tile = this.round.Index;

			if (tile == 0xffff)
				return CityScreenStep.Restart;

			if (tile == 20)
			{
				// A click on the city square in the middle of the map: all citizens are taken off their tiles
				// (also entertainers, taxmen and scientists) and the game's own assignment puts them back on the best tiles,
				// first enough food so the city doesn't starve, then the best mix of food, shields and trade.
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
				CitySpecialists = 0;
				return CityScreenStep.Restart;
			}

			if ((short)tile >= 0x14)
				return CityScreenStep.Restart;

			int horiz = town.Place.X + this.Owner.TownDeltas[tile].X;
			int vert = town.Place.Y + this.Owner.TownDeltas[tile].Y;


			if ((this.Owner.World.ChartSight[(ushort)(short)horiz, (ushort)(short)vert] & (short)(1 << (byte)CityOwner)) == 0)
				return CityScreenStep.Restart;

			RestoreShieldsAndTrade();

			uint bit = 1u << (byte)tile;
			uint Workers() => ((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0;

			// A square without a worker must be free
			if ((Workers() & bit) == 0)
			{
				ushort place = (ushort)(short)((10 * this.Owner.TownDeltas[tile].X) + (this.Owner.TownDeltas[tile].Y * 2));

				if (this.round.Frame(0x8e - place) != 0)
					return CityScreenStep.Redraw;
			}

			uint workers = Workers() ^ bit;
			town.LaborerMarkers0 = (ushort)workers;
			town.LaborerMarkers1 = (ushort)(workers >> 16);

			if ((Workers() & bit) != 0)
			{
				// A worker more, a specialist less
				this.round.WorkersGained = (ushort)(this.round.WorkersGained + 1);
				DrawCityTile(townIdx, tile, marker);

				if (CitySpecialists != 0)
				{
					CitySpecialists = (ushort)(CitySpecialists - 1);
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}
			}
			else
			{
				// A worker less: what the square gave is taken back
				this.round.WorkersGained = (ushort)(this.round.WorkersGained - 1);

				for (ushort resource = 0; resource < 3; resource++)
				{
					this.round.Item = resource;
					this.round.Amount = MayCityWorkTile(town.Place.X + this.Owner.TownDeltas[tile].X, town.Place.Y + this.Owner.TownDeltas[tile].Y, resource);
					ushort word = (ushort)((resource << 1) + 0x70da);
					this.Processor.EmitUInt16(this.Processor.SegData.Value16, word,
						(ushort)(this.Processor.PullUInt16(this.Processor.SegData.Value16, word) - this.round.Amount));
				}

				this.round.Item = 3;
				this.Owner.MapOps.DrawMapTile(town.Place.X + this.Owner.TownDeltas[tile].X, town.Place.Y + this.Owner.TownDeltas[tile].Y);
				CitySpecialists = (ushort)(CitySpecialists + 1);
				this.round.Workers = (ushort)(this.round.Workers + 1);
			}

			// The specialists in the top six bits
			uint counted = (uint)((short)this.round.Workers << 26) + (Workers() & 0x3ffffff);
			town.LaborerMarkers0 = (ushort)counted;
			town.LaborerMarkers1 = (ushort)(counted >> 16);

			return (short)this.round.WorkersGained > 0 ? CityScreenStep.Restart : CityScreenStep.Redraw;
		}

		/// <summary>
		/// The two special slots of the city (Unfamiliar, 0xff empty) counted in 0xdeb8 (0xea the slot); 0xd2f6 how many
		/// more of them there are than citizens.
		/// </summary>
		private void CountSpecialSlots(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (ushort slot = 0; slot < 2; slot++)
			{
				this.round.Index = slot;

				if ((byte)town.Unfamiliar[slot] != 0xff)
					CitySpecialSlots = (ushort)(CitySpecialSlots + 1);
			}

			this.round.Index = 2;

			if (town.TrueExtent < (short)CitySpecialSlots)
			{
				CityUnitUpkeep = (ushort)(CitySpecialSlots - town.TrueExtent);
			}
		}
	}
}
