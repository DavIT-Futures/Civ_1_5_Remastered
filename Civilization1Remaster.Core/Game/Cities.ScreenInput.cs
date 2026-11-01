using System;

namespace Civilization1Remaster.Core.Game
{
	partial class Cities
	{
		private enum CityScreenStep
		{
			Others,
			Redraw,
			Restart,
			Leave,
			Sell,
		}

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
					this.Owner.VideoCard.SubstituteHue(Screen.Box, 231, 106, 32, 8, Hues.Yellow, Hues.LightBlue);
				else
					this.Owner.VideoCard.SubstituteHue(Screen.Box, 231, 106, 32, 8, Hues.LightBlue, Hues.Yellow);

				this.Owner.Startup.ShowMouseCursor();
				this.Owner.TextPainter.WaitForTime(10);
			}
		}

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
				short owner = (short)CityTurn.Owner;

				this.round.AdvisorPick = this.Owner.Advisors.PickCityProduction(owner, townIdx);
				DrawingBox.TypefaceIdx = 2;

				if (this.round.AdvisorPick == 0x63)
					return CityScreenStep.Restart;

				CityTurn.Again = 0;

				StopMakingUnit(town, owner);
				town.PresentMakingIdx = (sbyte)(byte)this.round.AdvisorPick;
				StartMakingUnit(town, owner);

				this.Owner.Services.FloodBox(Screen.Box, 230, 99, 90, 100, Hues.Black);
			}

			RestoreShieldsAndTrade();

			return CityScreenStep.Redraw;
		}

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

			CityTurn.ScreenTab = tab;
			RestoreShieldsAndTrade();

			if (CityTurn.ScreenTab != 3)
				return CityScreenStep.Redraw;

			DrawingBox.TypefaceIdx = 1;
			this.Owner.Words.Clear();
			this.Owner.CityScene.ShowCityScreen(townIdx, -1);
			MapView.RedrawRequest = 1;
			CityTurn.ScreenTab = 0;

			return CityScreenStep.Restart;
		}

		private void BuyCityProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			sbyte making = town.PresentMakingIdx;

			if (making >= 0)
			{
				this.round.Selected = Tables.UnitKindWord(making, UnitKindTable.Price);
				short missing = (short)this.Owner.Helpers.ClampValue((10 * (short)this.round.Selected) - town.MaterialsTally, 0, 999);
				this.round.Value = (ushort)((short)(missing * missing) / 20 + (missing * 2));
			}
			else
			{
				this.round.Selected = Tables.BuildingWord(-making, BuildingTable.Price);
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

		private void ChangeCityProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			town.ConditionMarker &= 0xef;
			this.Owner.Startup.ShowMouseCursor();
			RepickCityProduction(townIdx);
			this.Owner.Startup.HideMouseCursor();
		}

		private ushort UnitOfCityList(ushort place) =>
			this.round.Frame(0x74 - (place << 1));

		private void ActivateUnitsInCity()
		{
			short owner = (short)CityTurn.Owner;
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
					this.Owner.VideoCard.SubstituteHue(Screen.Box, 252, line, 56, 7, Hues.LightBlue, Hues.Blue);
					this.Owner.TextPainter.WaitForTime(10);
					this.Owner.VideoCard.SubstituteHue(Screen.Box, 252, line, 56, 7, Hues.Blue, Hues.LightBlue);
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

					this.Owner.VideoCard.RenderImage(Screen.Box, horiz, vert, 16, 16, Screen.BackScreenBox, 0, 0);
					this.Owner.Dialogs.DrawBox((short)this.round.CursorHoriz * 16 + 160, (short)this.round.CursorVert * 16 + 56, 15, 15, Hues.White);
					this.Owner.TextPainter.WaitForTime(10);
					this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 16, 16, Screen.Box, horiz, vert);
					this.Owner.TextPainter.WaitForTime(10);
					MapView.RedrawRequest = 1;
				}
				while ((ushort)this.Owner.Libc.KeyAvailable() == 0);

				this.round.DrawHoriz = this.round.CursorHoriz;
				this.round.DrawVert = this.round.CursorVert;
				ushort key = this.Owner.Dialogs.ReadNavigationKey();
				this.round.Value = key;

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

		private CityScreenStep HandleListAndRename(short townIdx)
		{
			ushort key = this.round.Key;
			short horiz = (short)this.Owner.GlobDb3c;
			short vert = (short)this.Owner.GlobDb3e;
			bool click = this.Owner.GlobDb3a == 1;

			if (key == 'M' && ((byte)this.round.ListFlags & 2) != 0)
				return TurnListPage();

			if (key == 'r' || (click && horiz >= 0xe7 && vert > 0xbe && horiz < 0x10e))
			{
				this.Owner.TurnLogic.WaitForKey();
				DrawingBox.TypefaceIdx = 1;
				this.Owner.CityNaming.ShowCityLabelPrompt(townIdx);
				MapView.RedrawRequest = 1;
				return CityScreenStep.Restart;
			}

			if (!click)
				return CityScreenStep.Leave;

			if (horiz >= 0x120 && vert < 0x60 && vert > 0x58 && ((byte)this.round.ListFlags & 2) != 0)
				return TurnListPage();

			if (horiz >= 0x12c && vert < 0x5e)
			{
				this.round.Selected = (ushort)((short)(vert - 2) / 6 - this.round.WonderRows);
				return CityScreenStep.Sell;
			}

			return CityScreenStep.Others;
		}

		private CityScreenStep TurnListPage()
		{
			this.round.ListFlags = (ushort)(this.round.ListFlags ^ 1);
			this.round.ListStart = this.round.ListEnd;
			RestoreShieldsAndTrade();
			return CityScreenStep.Redraw;
		}

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

				ushort price = Tables.BuildingWord(improvement + 1, BuildingTable.Price);

				Message("Do you want to sell\nyour ")
					.Text(GameTables.BuildingText(improvement))
					.Text(" for ")
					.Number((short)(0xa * (short)price))
					.Text("$?\n No.\n Yes.\n");
				DrawingBox.TypefaceIdx = 1;
				this.Owner.Startup.ShowMouseCursor();
				if (this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 128, 80, 1) == 1)
				{
					this.Owner.World.Participants[CityTurn.Owner].Gold += (short)(0xa * price);
					town.Structures -= bit;
					town.ConditionMarker |= 0x80;

					if (improvement + 1 == 8)
						MapView.RedrawRequest = 1;
				}

				this.Owner.Startup.HideMouseCursor();
				return true;
			}

			this.round.Item = (ushort)Math.Max((short)this.round.ListStart, (short)0x18);
			return false;
		}

		private CityScreenStep CycleSpecialist()
		{
			this.round.Value = ObtainCitizenType(this.round.Index);
			AssignCitizenType(this.round.Index,
				(ushort)((short)this.round.Value < 3 ? this.round.Value + 1 : 1));

			RestoreShieldsAndTrade();

			return CityScreenStep.Redraw;
		}

		private CityScreenStep RefuseSpecialists()
		{
			this.Owner.Words.Set("A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 32, 32, 1);

			return CityScreenStep.Restart;
		}

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

		private bool ClickUnitOfCity()
		{
			short row = (short)((short)(this.Owner.GlobDb3e - 0x74) / 16);
			short column = (short)((short)(this.Owner.GlobDb3c - 0x64) / 16);
			this.round.Selected = (ushort)(row * 6 + column);

			if ((short)this.round.Selected >= (short)this.round.UnitsDrawn)
				return false;

			short owner = (short)CityTurn.Owner;
			WakeUnitOfCity(UnitMemory.Address(owner, UnitOfCityList(this.round.Selected)));

			RestoreShieldsAndTrade();

			return true;
		}

		private CityScreenStep ToggleCityTile(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.round.Index = ObtainCityTileIndex((short)this.round.DrawHoriz, (short)this.round.DrawVert);
			ushort tile = this.round.Index;

			if (tile == 0xffff)
				return CityScreenStep.Restart;

			if (tile == 20)
			{
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
				CityTurn.Specialists = 0;
				return CityScreenStep.Restart;
			}

			if ((short)tile >= 0x14)
				return CityScreenStep.Restart;

			int horiz = town.Place.X + this.Owner.TownDeltas[tile].X;
			int vert = town.Place.Y + this.Owner.TownDeltas[tile].Y;

			if ((this.Owner.World.ChartSight[(ushort)(short)horiz, (ushort)(short)vert] & (short)(1 << (byte)CityTurn.Owner)) == 0)
				return CityScreenStep.Restart;

			RestoreShieldsAndTrade();

			uint bit = 1u << (byte)tile;
			uint Workers() => ((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0;

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
				this.round.WorkersGained = (ushort)(this.round.WorkersGained + 1);
				DrawCityTile(townIdx, tile, marker);

				if (CityTurn.Specialists != 0)
				{
					CityTurn.Specialists = (ushort)(CityTurn.Specialists - 1);
					this.round.Workers = (ushort)(this.round.Workers - 1);
				}
			}
			else
			{
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
				CityTurn.Specialists = (ushort)(CityTurn.Specialists + 1);
				this.round.Workers = (ushort)(this.round.Workers + 1);
			}

			uint counted = (uint)((short)this.round.Workers << 26) + (Workers() & 0x3ffffff);
			town.LaborerMarkers0 = (ushort)counted;
			town.LaborerMarkers1 = (ushort)(counted >> 16);

			return (short)this.round.WorkersGained > 0 ? CityScreenStep.Restart : CityScreenStep.Redraw;
		}

		private void CountSpecialSlots(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			for (ushort slot = 0; slot < 2; slot++)
			{
				this.round.Index = slot;

				if ((byte)town.Unfamiliar[slot] != 0xff)
					CityTurn.SpecialSlots = (ushort)(CityTurn.SpecialSlots + 1);
			}

			this.round.Index = 2;

			if (town.TrueExtent < (short)CityTurn.SpecialSlots)
			{
				CityTurn.UnitUpkeep = (ushort)(CityTurn.SpecialSlots - town.TrueExtent);
			}
		}
	}
}
