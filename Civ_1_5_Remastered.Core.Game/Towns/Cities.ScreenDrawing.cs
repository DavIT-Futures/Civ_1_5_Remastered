using System;

namespace Civ_1_5_Remastered.Core.Game.Towns
{
	public partial class Cities
	{
		private void DrawImprovementList(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			DrawingBox.TypefaceIndex = 2;
			DrawFrame(211, 1, 317, 97);

			this.round.Selected = 2;
			this.round.Vertical = 2;
			this.round.WonderRows = 0;
			this.round.ImprovementCount = 0;

			if (this.round.ListStart == 0)
			{
				for (ushort wonder = 0; wonder < 0x15; wonder++)
				{
					this.round.Item = wonder;

					if (this.Owner.World.MarvelTownIndex[wonder + 1] != townIndex)
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
				if ((ushort)(((mask >> 16) & town.StructureMarkersHigh) | (ushort)(mask & town.StructureMarkersLow)) == 0)
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
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 309, (short)this.round.Vertical + 1,
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

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 253, (short)this.round.Vertical + 2, Hues.White);
			DrawResourceIcon((short)(improvement + 1), (short)((((byte)this.round.Selected & 1) != 0) ? 213 : 233),
				(short)((short)this.round.Vertical - 2));

			this.round.Vertical = (ushort)(this.round.Vertical + 6);
			this.round.Selected = this.Processor.IncrementWord(this.round.Selected);
		}

		private void FinishImprovementList(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			this.Owner.VideoCard.RenderLine(Screen.Box, 231, 0, 250, 0, Hues.Black);
			this.Owner.VideoCard.RenderLine(Screen.Box, 231, 1, 250, 1, Hues.Blue);

			if (((byte)this.round.ListFlags & 2) != 0)
			{
				DrawLabelBox(287, 88, 315, 96, GameTexts.TextMore, 9);
			}

			this.Owner.VideoCard.SubstituteHue(Screen.Box, 309, 2, 8, 96, Hues.Yellow, Hues.LightRed);

			if (town.PresentMakingIndex >= 0)
			{
				this.round.ProductionPrice = this.Processor.PullWord(this.Processor.SegmentData.Word,
					(ushort)((ushort)(town.PresentMakingIndex * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.Price));
			}
			else if (SpecialProjects.IsSpecialProject(town.PresentMakingIndex))
			{
				this.round.ProductionPrice = 1;
			}
			else
			{
				this.round.ProductionPrice = Tables.BuildingWordAt((ushort)(-town.PresentMakingIndex * 0x1e), BuildingTable.Price);
			}
		}

		private void DrawProductionBox(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
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
			this.round.DrawHorizontal = (ushort)(((short)((perRow << 3) - 8) / perRow) + width);
			this.round.DrawVertical = (ushort)(((((short)this.round.ProductionPrice - 1) / perRow) << 3) + 8);

			this.Owner.Services.FloodBox(Screen.Box, 230, 99, (short)this.round.DrawHorizontal + 3, (short)this.round.DrawVertical + 19, Hues.Blue);

			if (owner == this.Owner.World.PersonParticipantIndex)
			{
				DrawLabelBox(231, 106, 263, 114, (ushort)((town.ConditionMarker & 0x10) != 0 ? 0x262f : 0x2635), 9);
				DrawLabelBox(294, 106, 311, 114, GameTexts.TextBuy, 9);
			}

			if (town.PresentMakingIndex >= 0)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 264, 100,
					Tables.SpritePicture((byte)town.PresentMakingIndex + (owner << 5) + 0x40));
			}
			else if (town.PresentMakingIndex == SpecialProjects.Wealth)
			{
				this.Owner.Words.Set("Wealth");
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 0x56);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 274, 100, Hues.White);
			}
			else if (town.PresentMakingIndex == SpecialProjects.Research)
			{
				this.Owner.Words.Set("Research");
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 0x56);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 274, 100, Hues.White);
			}
			else if (town.PresentMakingIndex == SpecialProjects.Food)
			{
				this.Owner.Words.Set("Food");
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 0x56);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 274, 100, Hues.White);
			}
			else
			{
				this.Owner.Words.Set(GameTables.BuildingRecordText(-town.PresentMakingIndex));
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 0x56);
				this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 274, 100, Hues.White);
			}
		}

		private void DrawShieldsInBox(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			FillBoxWithMotif(231, 116, (short)this.round.DrawHorizontal + 1, (short)this.round.DrawVertical + 1);

			if (SpecialProjects.IsSpecialProject(town.PresentMakingIndex))
			{
				return;
			}

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

		private void DrawFoodBox(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

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

			this.round.DrawVertical = (ushort)(80 / box);

			this.Owner.Services.FloodBox(Screen.Box, 2, 106, 91,
				((short)this.round.CostFactor * (short)this.round.DrawVertical) + 12, Hues.Blue);
			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextFoodStorage, 8, 108, Hues.White);
			FillBoxWithMotif(3, 115, (town.TrueExtent * (short)this.round.Value) + 9,
				((short)this.round.CostFactor * (short)this.round.DrawVertical) + 2);

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
					(((short)this.round.Index / perRow) * (short)this.round.DrawVertical) + 116,
					Tables.SpritePicture(0x8));
			}
		}

		private void DrawResourceRows(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			DrawFrame(2, 23, 124, 65);
			this.round.DrawVertical = 0x19;
			this.Owner.Services.FloodBox(Screen.Box, 2, 23, 122, 9, Hues.Blue);
			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextCityResources, 8, (short)this.round.DrawVertical, Hues.White);
			this.round.DrawVertical = (ushort)(this.round.DrawVertical + 8);

			for (ushort row = 0; row < 3; row++)
			{
				this.round.Item = row;
				this.round.Horizontal = 4;
				ushort amount = (ushort)((row << 1) + 0x70da);

				int step = row == 0
					? this.Owner.Helpers.ClampValue(116 / ((short)this.Owner.Helpers.ClampValue(this.Processor.PullSignedWord(this.Processor.SegmentData.Word, amount),
						(town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers), 999) + 1), 1, 8)
					: this.Owner.Helpers.ClampValue(116 / (this.Processor.PullSignedWord(this.Processor.SegmentData.Word, amount) + 1), 1, 8);

				this.round.Value = (ushort)step;
				ushort icon = 0;

				for (; (short)this.Processor.PullWord(this.Processor.SegmentData.Word, amount) > (short)icon; icon++)
				{
					this.round.Index = icon;

					if (row == 0 && (ushort)((town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers)) == icon)
					{
						this.round.Horizontal = (ushort)(this.round.Horizontal + 4);
					}

					if (row == 1 && CityTurn.UnitUpkeep != 0 && icon == CityTurn.UnitUpkeep)
					{
						this.round.Horizontal = (ushort)(this.round.Horizontal + 4);
					}

					if (row == 2 && (ushort)(CityTurn.Trade - CityTurn.Corruption) == icon)
					{
						this.round.Horizontal = (ushort)(this.round.Horizontal + 2);
					}

					this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.Horizontal,
						((short)this.round.Item * 8) + (short)this.round.DrawVertical,
						Tables.SpritePicture(row + 8));

					this.round.Horizontal = (ushort)(this.round.Horizontal + this.round.Value);
				}

				this.round.Index = icon;
			}

			this.round.Item = 3;
		}

		private void DrawTradeRow()
		{
			this.round.Horizontal = 8;
			this.round.Value = (ushort)this.Owner.Helpers.ClampValue(224 / ((short)CityTurn.Luxuries + (short)CityTurn.Science +
				(short)CityTurn.Taxes + (short)CityTurn.Corruption + 2), 1, 16);

			DrawTradeIcons(0x70e0, 0xe);

			if (CityTurn.Luxuries != 0)
			{
				this.round.Horizontal = (ushort)(this.round.Horizontal + 8);
			}

			this.round.DrawHorizontal = this.round.Horizontal;

			DrawTradeIcons(0xe17a, 0xb);

			if (CityTurn.Taxes != 0)
			{
				this.round.Horizontal = (ushort)(this.round.Horizontal + 8);
			}

			DrawTradeIcons(0x70e6, 0xc);
		}

		private void DrawTradeIcons(ushort amount, ushort icon)
		{
			ushort drawn = 0;

			for (; (short)drawn < (short)this.Processor.PullGlobalWord(amount); drawn++)
			{
				this.round.Index = drawn;
				this.Owner.Services.DrawPictureToScreen(Screen.Box, (short)this.round.Horizontal / 2,
					(short)this.round.DrawVertical + 24, Tables.SpritePicture(icon));
				this.round.Horizontal = (ushort)(this.round.Horizontal + this.round.Value);
			}

			this.round.Index = drawn;
		}

		private void DrawResourceBadge(string text, int horizontalLocation, int verticalLocation, ushort textColor)
		{
			int width = this.Owner.TextPainter.MeasureTextWidth(text);
			int leftPosition = horizontalLocation - (width / 2);
			if (leftPosition < 4)
			{
				leftPosition = 4;
				horizontalLocation = leftPosition + (width / 2);
			}

			if (leftPosition + width + 2 > 124)
			{
				leftPosition = 124 - width - 2;
				horizontalLocation = leftPosition + (width / 2);
			}

			this.Owner.Services.FloodBox(Screen.Box, leftPosition - 1, verticalLocation, width + 2, 7, Hues.Black);
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(text, horizontalLocation, verticalLocation, textColor);
		}

		private void DrawResourceQuantities(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			int foodTotal = (short)CityTurn.Food;
			int occupiedFood = (town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers);
			int freeFood = foodTotal - occupiedFood;
			string foodLabel = $"{occupiedFood}/{freeFood}";

			int highestFood = System.Math.Max(foodTotal, occupiedFood);
			int foodStep = this.Owner.Helpers.ClampValue(116 / (highestFood + 1), 1, 8);
			int foodGap = foodTotal > occupiedFood ? 4 : 0;
			int foodEndOfIcons = 4 + (highestFood * foodStep) + foodGap;
			int foodCenterHorizontal = highestFood > 0 ? (4 + foodEndOfIcons) / 2 : 24;
			DrawResourceBadge(foodLabel, foodCenterHorizontal, 33, Hues.White);

			int shieldTotal = (short)CityTurn.Shields;
			int occupiedShields = (short)CityTurn.UnitUpkeep;
			int freeShields = shieldTotal - occupiedShields;
			string shieldLabel = $"{occupiedShields}/{freeShields}";

			int highestShields = System.Math.Max(shieldTotal, occupiedShields);
			int shieldStep = this.Owner.Helpers.ClampValue(116 / (highestShields + 1), 1, 8);
			int shieldGap = (occupiedShields != 0 && shieldTotal > occupiedShields) ? 4 : 0;
			int shieldEndOfIcons = 4 + (highestShields * shieldStep) + shieldGap;
			int shieldCenterHorizontal = highestShields > 0 ? (4 + shieldEndOfIcons) / 2 : 24;
			DrawResourceBadge(shieldLabel, shieldCenterHorizontal, 41, Hues.White);

			int tradeAmount = CityTurn.Trade;
			int tradeStep = this.Owner.Helpers.ClampValue(116 / (tradeAmount + 1), 1, 8);
			int tradeGap = tradeAmount > (CityTurn.Trade - CityTurn.Corruption) ? 2 : 0;
			int tradeEndOfIcons = 4 + (tradeAmount * tradeStep) + tradeGap;
			int tradeCenterHorizontal = tradeAmount > 0 ? (4 + tradeEndOfIcons) / 2 : 24;
			DrawResourceBadge(tradeAmount.ToString(), tradeCenterHorizontal, 49, Hues.White);

			int luxuryAmount = CityTurn.Luxuries;
			int taxesAmount = CityTurn.Taxes;
			int scienceAmount = CityTurn.Science;
			int corruptionAmount = CityTurn.Corruption;
			int totalTradeIcons = luxuryAmount + taxesAmount + scienceAmount + corruptionAmount;
			int stepTrade = this.Owner.Helpers.ClampValue(224 / (totalTradeIcons + 2), 1, 16);

			int tradeHorizontal = 8;

			if (luxuryAmount > 0)
			{
				int luxuryStartHorizontal = tradeHorizontal / 2;
				tradeHorizontal += luxuryAmount * stepTrade;
				int luxuryEndHorizontal = tradeHorizontal / 2;
				int luxuryCenterHorizontal = (luxuryStartHorizontal + luxuryEndHorizontal) / 2;
				DrawResourceBadge(luxuryAmount.ToString(), luxuryCenterHorizontal, 57, Hues.LightCyan);
				tradeHorizontal += 8;
			}

			if (taxesAmount > 0)
			{
				int taxesStartHorizontal = tradeHorizontal / 2;
				tradeHorizontal += taxesAmount * stepTrade;
				int taxesEndHorizontal = tradeHorizontal / 2;
				int taxesCenterHorizontal = (taxesStartHorizontal + taxesEndHorizontal) / 2;
				DrawResourceBadge(taxesAmount.ToString(), taxesCenterHorizontal, 57, Hues.Yellow);
				tradeHorizontal += 8;
			}

			if (scienceAmount > 0)
			{
				int scienceStartHorizontal = tradeHorizontal / 2;
				tradeHorizontal += scienceAmount * stepTrade;
				int scienceEndHorizontal = tradeHorizontal / 2;
				int scienceCenterHorizontal = (scienceStartHorizontal + scienceEndHorizontal) / 2;
				DrawResourceBadge(scienceAmount.ToString(), scienceCenterHorizontal, 57, Hues.LightCyan);
			}
		}

		private void DrawShortfalls(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

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
						(short)this.round.DrawVertical,
						Tables.SpritePicture(0x8));
				}

				this.round.Index = icon;
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					((short)this.round.Value * (short)CityTurn.Food) + 8,
					(short)this.round.DrawVertical,
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
						(short)this.round.DrawVertical + 8,
						Tables.SpritePicture(0x9));
				}

				this.round.Index = icon;
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					((short)CityTurn.Shields * (short)this.round.Value) + 8,
					(short)this.round.DrawVertical + 8,
					((short)CityTurn.UnitUpkeep - (short)CityTurn.Shields) * (short)this.round.Value,
					8, Hues.White, Hues.Black);
			}

			if (CityTurn.Corruption != 0)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / ((short)CityTurn.Trade + 1), 1, 8);
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					(((short)CityTurn.Trade - (short)CityTurn.Corruption) * (short)this.round.Value) + 6,
					(short)this.round.DrawVertical + 16,
					((short)this.round.Value * (short)CityTurn.Corruption) + 2,
					8, Hues.White, Hues.Black);
			}
		}

		private void DrawCitizensAndRoutes(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			FillBoxWithMotif(8, 8, 200, 13);
			this.round.Measure = DrawCitizenIcons(townIndex, 8, 8, this.round.Specialists, 192);
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
				int shift = (ushort)other.ParticipantIndex != CityTurn.Owner ? 3 : 4;
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)(((short)town.RootCommerce + (short)other.RootCommerce + 4) >> shift), 10));
				this.Owner.Words.Append("} ");

				if (CityTurn.ScreenTab == 2)
				{
					DrawMiniMapDot((ushort)((short)other.Place.X), (ushort)((short)other.Place.Y), 10);
				}

				if (CityTurn.ScreenTab == 0)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 98, ((short)this.round.Item * 6) + 179, Hues.LightGreen);
				}
			}

			this.round.Item = 3;
		}

		private void DrawCityDotAndPollution(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

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

			this.round.DrawVertical = (ushort)this.Owner.Helpers.ClampValue(100 / (short)this.Owner.Helpers.ClampValue(pollution, 1, 99), 1, 8);

			ushort icon = 0;

			for (; (short)icon < (short)this.round.Pollution; icon++)
			{
				this.round.Index = icon;
				this.Owner.Services.DrawPictureToScreen(Screen.Box,
					((short)this.round.Index * (short)this.round.DrawVertical) + 98,
					(this.round.Index & 1) + 161,
					Icons.PollutionIconPicture);
			}

			this.round.Index = icon;
		}

		private void DrawExitAndBuyButtons(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];
			short owner = (short)CityTurn.Owner;

			DrawLabelBox(284, 190, 316, 198, GameTexts.TextExit, 12);
			DrawLabelBox(231, 190, 272, 198, GameTexts.TextRename, 9);
			this.round.ChangeBlinks = 0;

			sbyte making = town.PresentMakingIndex;

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

			if (making < -24 && making >= -48 && this.Owner.World.MarvelTownIndex[Math.Abs(making) - 24] != -1)
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
