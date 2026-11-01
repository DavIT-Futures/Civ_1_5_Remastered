using System;

namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		private void DrawImprovementList(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			DrawingBox.TypefaceIdx = 2;
			DrawFrame(211, 1, 317, 97);

			this.round.Selected = 2;
			this.round.Vert = 2;
			this.round.WonderRows = 0;
			this.round.ImprovementCount = 0;

			if (this.round.ListStart == 0)
			{
				for (ushort wonder = 0; wonder < 0x15; wonder++)
				{
					this.round.Item = wonder;

					if (this.Owner.World.MarvelTownIdx[wonder + 1] != townIdx)
					{
						continue;
					}

					this.round.Value = (ushort)(wonder + 0x18);
					DrawImprovementName(this.round.Value, 63, true);
					this.round.WonderRows = this.Processor.IncrementWord(this.round.WonderRows);
				}

				this.round.Item = 0x15;
			}

			this.round.ListEnd = 0;
			this.round.Item = this.round.ListStart;

			for (; (short)this.round.Item < 0x18; this.round.Item = (ushort)(this.round.Item + 1))
			{
				ushort improvement = this.round.Item;
				uint mask = 1u << (improvement & 0xff);
				if ((ushort)(((mask >> 16) & town.StructureMarkers1) | (ushort)(mask & town.StructureMarkers0)) == 0)
				{
					continue;
				}

				if ((short)this.round.Selected >= 0x10)
				{
					this.round.ListFlags = (ushort)((this.round.ListFlags | 2) & 0xfffe);
					this.round.ListEnd = this.round.Item;
					break;
				}

				this.round.ImprovementCount = (ushort)(this.round.ImprovementCount + 1);
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 309, (short)this.round.Vert + 1,
					Tables.SpritePicture(0xb));
				DrawImprovementName(improvement, 56, false);
			}
		}

		private void DrawImprovementName(ushort improvement, ushort width, bool shrinkFirst)
		{
			this.Owner.Words.Set(GameTables.BuildingText(improvement));

			if (shrinkFirst)
			{
				this.Owner.Words.Uppercase();
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, width);
			}
			else
			{
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, width);
				this.Owner.Words.Uppercase();
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 253, (short)this.round.Vert + 2, Hues.White);
			DrawResourceIcon((short)(improvement + 1), (short)((((byte)this.round.Selected & 1) != 0) ? 213 : 233),
				(short)((short)this.round.Vert - 2));

			this.round.Vert = (ushort)(this.round.Vert + 6);
			this.round.Selected = this.Processor.IncrementWord(this.round.Selected);
		}

		private void FinishImprovementList(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.VideoCard.RenderLine(Screen.Box, 231, 0, 250, 0, Hues.Black);
			this.Owner.VideoCard.RenderLine(Screen.Box, 231, 1, 250, 1, Hues.Blue);

			if (((byte)this.round.ListFlags & 2) != 0)
			{
				DrawLabelBox(287, 88, 315, 96, GameTexts.TextMore, 9);
			}

			this.Owner.VideoCard.SubstituteHue(Screen.Box, 309, 2, 8, 96, Hues.Yellow, Hues.LightRed);

			if (town.PresentMakingIdx >= 0)
			{
				this.round.ProductionPrice = this.Processor.PullUInt16(this.Processor.SegData.Value16,
					(ushort)((ushort)(town.PresentMakingIdx * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.Price));
			}
			else
			{
				this.round.ProductionPrice = Tables.BuildingWordAt((ushort)(-town.PresentMakingIdx * 0x1e), BuildingTable.Price);
			}
		}

		private void DrawProductionBox(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			this.round.ShieldsPerRow = (ushort)this.Owner.Helpers.ClampValue((((short)this.round.ProductionPrice - 1) / 10) + 1, ((town.MaterialsTally - 1) / 100) + 1, 99);

			short box = (short)this.round.CostFactor;

			if (box == 0)
			{
				throw new Exception("Division by zero");
			}

			this.round.Value = (ushort)(80 / box);
			short perRow = (short)this.round.ShieldsPerRow;
			short width = (short)(box * (short)this.round.Value);
			this.round.DrawHoriz = (ushort)(((short)((perRow << 3) - 8) / perRow) + width);
			this.round.DrawVert = (ushort)(((((short)this.round.ProductionPrice - 1) / perRow) << 3) + 8);

			this.Owner.Services.FloodBox(Screen.Box, 230, 99, (short)this.round.DrawHoriz + 3, (short)this.round.DrawVert + 19, Hues.Blue);

			if (owner == this.Owner.World.PersonParticipantIdx)
			{
				DrawLabelBox(231, 106, 263, 114, (ushort)((town.ConditionMarker & 0x10) != 0 ? 0x262f : 0x2635), 9);
				DrawLabelBox(294, 106, 311, 114, GameTexts.TextBuy, 9);
			}

			if (town.PresentMakingIdx >= 0)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 264, 100,
					Tables.SpritePicture((byte)town.PresentMakingIdx + (owner << 5) + 0x40));
			}
			else
			{
				this.Owner.Words.Set(GameTables.BuildingRecordText(-town.PresentMakingIdx));
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 0x56);
				this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 274, 100, Hues.White);
			}
		}

		private void DrawShieldsInBox(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			FillBoxWithMotif(231, 116, (short)this.round.DrawHoriz + 1, (short)this.round.DrawVert + 1);

			ushort shield = 0;

			for (; town.MaterialsTally > (short)shield; shield++)
			{
				this.round.Index = shield;
				this.Owner.Services.DrawPictureToScreen(Screen.Box,
					((((short)this.round.Index % ((short)this.round.CostFactor * (short)this.round.ShieldsPerRow)) *
						(short)this.round.Value) / (short)this.round.ShieldsPerRow) + 232,
					(((short)this.round.Index / ((short)this.round.CostFactor * (short)this.round.ShieldsPerRow)) * 8) + 117,
					Tables.SpritePicture(0x9));
			}

			this.round.Index = shield;
		}

		private void DrawFoodBox(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (town.TrueExtent != 0)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(80 / town.TrueExtent, 1, 8);
			}
			else
			{
				this.round.Value = 1;
			}

			short box = (short)this.round.CostFactor;

			if (box == 0)
			{
				throw new Exception("Division by zero");
			}

			this.round.DrawVert = (ushort)(80 / box);

			this.Owner.Services.FloodBox(Screen.Box, 2, 106, 91,
				((short)this.round.CostFactor * (short)this.round.DrawVert) + 12, Hues.Blue);
			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextFoodStorage, 8, 108, Hues.White);
			FillBoxWithMotif(3, 115, (town.TrueExtent * (short)this.round.Value) + 9,
				((short)this.round.CostFactor * (short)this.round.DrawVert) + 2);

			if (town.Has(CityStructure.Granary))
			{
				this.Owner.VideoCard.RenderLine(Screen.Box, 5, 155,
					(town.TrueExtent * (short)this.round.Value) + 9, 155, Hues.Blue);
			}

			for (ushort grain = 0; ; grain++)
			{
				this.round.Index = grain;
				if ((short)this.Owner.Helpers.ClampValue(town.GrainTally, 0, (town.TrueExtent + 1) * (short)this.round.CostFactor) <= (short)grain)
				{
					break;
				}

				short perRow = (short)(town.TrueExtent + 1);
				this.Owner.Services.DrawPictureToScreen(Screen.Box,
					(((short)this.round.Index % perRow) * (short)this.round.Value) + 4,
					(((short)this.round.Index / perRow) * (short)this.round.DrawVert) + 116,
					Tables.SpritePicture(0x8));
			}
		}

		private void DrawResourceRows(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			DrawFrame(2, 23, 124, 65);
			this.round.DrawVert = 0x19;
			this.Owner.Services.FloodBox(Screen.Box, 2, 23, 122, 9, Hues.Blue);
			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextCityResources, 8, (short)this.round.DrawVert, Hues.White);
			this.round.DrawVert = (ushort)(this.round.DrawVert + 8);

			for (ushort row = 0; row < 3; row++)
			{
				this.round.Item = row;
				this.round.Horiz = 4;
				ushort amount = (ushort)((row << 1) + 0x70da);

				int step = row == 0
					? this.Owner.Helpers.ClampValue(116 / ((short)this.Owner.Helpers.ClampValue(this.Processor.PullInt16(this.Processor.SegData.Value16, amount),
						(town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers), 999) + 1), 1, 8)
					: this.Owner.Helpers.ClampValue(116 / (this.Processor.PullInt16(this.Processor.SegData.Value16, amount) + 1), 1, 8);

				this.round.Value = (ushort)step;
				ushort icon = 0;

				for (; (short)this.Processor.PullUInt16(this.Processor.SegData.Value16, amount) > (short)icon; icon++)
				{
					this.round.Index = icon;

					if (row == 0 && (ushort)((town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers)) == icon)
					{
						this.round.Horiz = (ushort)(this.round.Horiz + 4);
					}

					if (row == 1 && CityTurn.UnitUpkeep != 0 && icon == CityTurn.UnitUpkeep)
					{
						this.round.Horiz = (ushort)(this.round.Horiz + 4);
					}

					if (row == 2 && (ushort)(CityTurn.Trade - CityTurn.Corruption) == icon)
					{
						this.round.Horiz = (ushort)(this.round.Horiz + 2);
					}

					this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.Horiz,
						((short)this.round.Item * 8) + (short)this.round.DrawVert,
						Tables.SpritePicture(row + 8));

					this.round.Horiz = (ushort)(this.round.Horiz + this.round.Value);
				}

				this.round.Index = icon;
			}

			this.round.Item = 3;
		}

		private void DrawTradeRow()
		{
			this.round.Horiz = 8;
			this.round.Value = (ushort)this.Owner.Helpers.ClampValue(224 / ((short)CityTurn.Luxuries + (short)CityTurn.Science +
				(short)CityTurn.Taxes + (short)CityTurn.Corruption + 2), 1, 16);

			DrawTradeIcons(0x70e0, 0xe);

			if (CityTurn.Luxuries != 0)
			{
				this.round.Horiz = (ushort)(this.round.Horiz + 8);
			}

			this.round.DrawHoriz = this.round.Horiz;

			DrawTradeIcons(0xe17a, 0xb);

			if (CityTurn.Taxes != 0)
			{
				this.round.Horiz = (ushort)(this.round.Horiz + 8);
			}

			DrawTradeIcons(0x70e6, 0xc);
		}

		private void DrawTradeIcons(ushort amount, ushort icon)
		{
			ushort drawn = 0;

			for (; (short)drawn < (short)this.Processor.PullGlobalWord(amount); drawn++)
			{
				this.round.Index = drawn;
				this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.Horiz / 2,
					(short)this.round.DrawVert + 24, Tables.SpritePicture(icon));
				this.round.Horiz = (ushort)(this.round.Horiz + this.round.Value);
			}

			this.round.Index = drawn;
		}

		private void DrawShortfalls(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			int Eaten() => (town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers);

			if ((short)Eaten() > (short)CityTurn.Food)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / (Eaten() + 1), 1, 8);
				ushort icon = CityTurn.Food;

				for (; (short)Eaten() > (short)icon; icon++)
				{
					this.round.Index = icon;
					this.Owner.Services.DrawPictureToScreen(Screen.Box,
						((short)this.round.Index * (short)this.round.Value) + 8,
						(short)this.round.DrawVert,
						Tables.SpritePicture(0x8));
				}

				this.round.Index = icon;
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					((short)this.round.Value * (short)CityTurn.Food) + 8,
					(short)this.round.DrawVert,
					((Eaten() - (short)CityTurn.Food) * (short)this.round.Value) + 4,
					8, Hues.White, Hues.Black);
			}

			if ((short)CityTurn.Shields < (short)CityTurn.UnitUpkeep)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / ((short)CityTurn.Shields + 1), 1, 8);
				ushort icon = CityTurn.Shields;

				for (; (short)icon < (short)CityTurn.UnitUpkeep; icon++)
				{
					this.round.Index = icon;
					this.Owner.Services.DrawPictureToScreen(Screen.Box,
						((short)this.round.Index * (short)this.round.Value) + 8,
						(short)this.round.DrawVert + 8,
						Tables.SpritePicture(0x9));
				}

				this.round.Index = icon;
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					((short)CityTurn.Shields * (short)this.round.Value) + 8,
					(short)this.round.DrawVert + 8,
					((short)CityTurn.UnitUpkeep - (short)CityTurn.Shields) * (short)this.round.Value,
					8, Hues.White, Hues.Black);
			}

			if (CityTurn.Corruption != 0)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / ((short)CityTurn.Trade + 1), 1, 8);
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					(((short)CityTurn.Trade - (short)CityTurn.Corruption) * (short)this.round.Value) + 6,
					(short)this.round.DrawVert + 16,
					((short)this.round.Value * (short)CityTurn.Corruption) + 2,
					8, Hues.White, Hues.Black);
			}
		}

		private void DrawCitizensAndRoutes(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			FillBoxWithMotif(8, 8, 200, 13);
			this.round.Measure = DrawCitizenIcons(townIdx, 8, 8, this.round.Specialists, 192);
			this.round.Value = (ushort)this.Owner.Helpers.ClampValue((town.TrueExtent * 8) + 24, 0, 128);

			for (ushort route = 0; route < 3; route++)
			{
				this.round.Item = route;
				ushort partner = (byte)town.CommerceTownIndices[route];
				this.round.OtherCity = partner;

				if (partner == 0xff)
				{
					continue;
				}

				EmptyMessage()
					.City((short)partner)
					.Text(":+");
				City other = this.Owner.World.Towns[partner];
				int shift = (ushort)other.ParticipantIdx != CityTurn.Owner ? 3 : 4;
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(((short)town.RootCommerce + (short)other.RootCommerce + 4) >> shift), 10));
				this.Owner.Words.Append("} ");

				if (CityTurn.ScreenTab == 2)
				{
					DrawMiniMapDot((ushort)((short)other.Place.X), (ushort)((short)other.Place.Y), 10);
				}

				if (CityTurn.ScreenTab == 0)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 98, ((short)this.round.Item * 6) + 179, Hues.LightGreen);
				}
			}

			this.round.Item = 3;
		}

		private void DrawCityDotAndPollution(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (CityTurn.ScreenTab == 2)
			{
				City shown = this.Owner.World.Towns[CityTurn.ShownCity];
				DrawMiniMapDot((ushort)((short)shown.Place.X), (ushort)((short)shown.Place.Y), 15);
			}

			if (CityTurn.ScreenTab != 0)
			{
				return;
			}

			short pollution = CountPollution(town);

			this.round.DrawVert = (ushort)this.Owner.Helpers.ClampValue(100 / (short)this.Owner.Helpers.ClampValue(pollution, 1, 99), 1, 8);

			ushort icon = 0;

			for (; (short)icon < (short)this.round.Pollution; icon++)
			{
				this.round.Index = icon;
				this.Owner.Services.DrawPictureToScreen(Screen.Box,
					((short)this.round.Index * (short)this.round.DrawVert) + 98,
					(this.round.Index & 1) + 161,
					Icons.PollutionIconPicture);
			}

			this.round.Index = icon;
		}

		private void DrawExitAndBuyButtons(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			DrawLabelBox(284, 190, 316, 198, GameTexts.TextExit, 12);
			DrawLabelBox(231, 190, 272, 198, GameTexts.TextRename, 9);
			this.round.ChangeBlinks = 0;

			sbyte making = town.PresentMakingIdx;

			if (making < 0 && making >= -21)
			{
				if (town.Has((CityStructure)(-making - 1)))
				{
					this.round.ChangeBlinks = 1;
				}
			}

			if (making < -21 && making >= -24 && ((ushort)(1 << (owner & 0xff)) & (ushort)this.Owner.World.StarshipMarkers) != 0)
			{
				this.round.ChangeBlinks = 1;
			}

			if (making < -24 && this.Owner.World.MarvelTownIdx[Math.Abs(making) - 24] != -1)
			{
				this.round.ChangeBlinks = 1;
			}

			if (making >= 0 && this.Owner.World.Participants[owner].TroopTally >= 127)
			{
				this.round.ChangeBlinks = 1;
			}
		}
	}
}
