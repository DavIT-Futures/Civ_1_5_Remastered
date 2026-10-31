
using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The boxes of the city screen</summary>
	partial class Cities
	{
		/// <summary>
		/// The city screen, the list of improvements (0x106 the row, 0xe4 its line, 0x40 the wonders, 0xb4 the improvements
		/// listed): first the wonders of the city (when the list starts at 0, 0xda), then the improvements from 0xda on; a full
		/// list (16 rows) stops at the improvement it reached (0xfe) and allows the next page (0xca bit 2, bit 1 cleared)
		/// </summary>
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
				// The wonders of the city
				for (ushort wonder = 0; wonder < 0x15; wonder++)
				{
					this.round.Item = wonder;

					if (this.Owner.World.MarvelTownIdx[wonder + 1] != townIdx)
						continue;

					this.round.Value = (ushort)(wonder + 0x18);
					DrawImprovementName(this.round.Value, 63, true);
					this.round.WonderRows = this.Processor.IncrementWord(this.round.WonderRows);
				}

				this.round.Item = 0x15;
			}

			// The improvements from 0xda on
			this.round.ListEnd = 0;
			this.round.Item = this.round.ListStart;

			for (; (short)this.round.Item < 0x18; this.round.Item = (ushort)(this.round.Item + 1))
			{
				ushort improvement = this.round.Item;
				uint mask = 1u << (improvement & 0xff);
				if ((ushort)(((mask >> 16) & town.StructureMarkers1) | (ushort)(mask & town.StructureMarkers0)) == 0)
					continue;

				if ((short)this.round.Selected >= 0x10)
				{
					// The list is full: the next page
					this.round.ListFlags = (ushort)((this.round.ListFlags | 2) & 0xfffe);
					this.round.ListEnd = this.round.Item;
					break;
				}

				this.round.ImprovementCount = (ushort)(this.round.ImprovementCount + 1);
				this.Owner.Services.DrawPictureToScreen(ScreenBox, 309, (short)this.round.Vert + 1,
					SpritePicture(0xb));
				DrawImprovementName(improvement, 56, false);
			}
		}

		/// <summary>A row of the improvement list: the name (upper case, within the width), the icon (left or right by the row)</summary>
		private void DrawImprovementName(ushort improvement, ushort width, bool shrinkFirst)
		{
			this.Owner.Words.Set(BuildingText(improvement));

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

		/// <summary>
		/// The frame of the improvement list (the "more" box when a next page exists), and 0xde the price of the production
		/// (a unit or an improvement)
		/// </summary>
		private void FinishImprovementList(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.VideoCard.RenderLine(ScreenBox, 231, 0, 250, 0, Hues.Black);
			this.Owner.VideoCard.RenderLine(ScreenBox, 231, 1, 250, 1, Hues.Blue);

			if (((byte)this.round.ListFlags & 2) != 0)
				DrawLabelBox(287, 88, 315, 96, GameTexts.TextMore, 9);

			this.Owner.VideoCard.SubstituteHue(ScreenBox, 309, 2, 8, 96, Hues.Yellow, Hues.LightRed);

			if (town.PresentMakingIdx >= 0)
			{
				this.round.ProductionPrice = this.Processor.PullUInt16(this.Processor.SegData.Value16,
					(ushort)((ushort)(town.PresentMakingIdx * UnitKindTable.Stride) + UnitKindTable.Origin + UnitKindTable.Price));
			}
			else
			{
				this.round.ProductionPrice = BuildingWordAt((ushort)(-town.PresentMakingIdx * 0x1e), BuildingTable.Price);
			}
		}

		/// <summary>
		/// The production box of the city screen: 0x44 the shields per row (by the price and the shields, 1 - 99), 0xe8 the
		/// step, 0xf6 / 0xfc its width and height; the Change and Buy buttons of a human city (Change shows the advisor
		/// flag 0x10), the unit or the name of the improvement in production.
		/// </summary>
		private void DrawProductionBox(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.round.ShieldsPerRow = (ushort)this.Owner.Helpers.ClampValue((((short)this.round.ProductionPrice - 1) / 10) + 1, ((town.MaterialsTally - 1) / 100) + 1, 99);

			short box = (short)this.round.CostFactor;

			if (box == 0)
				throw new Exception("Division by zero");

			this.round.Value = (ushort)(80 / box);
			short perRow = (short)this.round.ShieldsPerRow;
			short width = (short)(box * (short)this.round.Value);
			this.round.DrawHoriz = (ushort)((short)((perRow << 3) - 8) / perRow + width);
			this.round.DrawVert = (ushort)((((short)this.round.ProductionPrice - 1) / perRow << 3) + 8);

			this.Owner.Services.FloodBox(ScreenBox, 230, 99, (short)this.round.DrawHoriz + 3, (short)this.round.DrawVert + 19, Hues.Blue);

			if (owner == this.Owner.World.PersonParticipantIdx)
			{
				DrawLabelBox(231, 106, 263, 114, (ushort)((town.ConditionMarker & 0x10) != 0 ? 0x262f : 0x2635), 9);
				DrawLabelBox(294, 106, 311, 114, GameTexts.TextBuy, 9);
			}

			if (town.PresentMakingIdx >= 0)
			{
				this.Owner.Services.DrawPictureToScreen(ScreenBox, 264, 100,
					SpritePicture(((byte)town.PresentMakingIdx + (owner << 5) + 0x40)));
			}
			else
			{
				this.Owner.Words.Set(BuildingRecordText(-town.PresentMakingIdx));
				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 0x56);
				this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 274, 100, Hues.White);
			}
		}

		/// <summary>The shields of the production box (0x44 a row, 0xe8 the step)</summary>
		private void DrawShieldsInBox(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			FillBoxWithMotif(231, 116, (short)this.round.DrawHoriz + 1, (short)this.round.DrawVert + 1);

			ushort shield = 0;

			for (; town.MaterialsTally > (short)shield; shield++)
			{
				this.round.Index = shield;
				this.Owner.Services.DrawPictureToScreen(ScreenBox,
					((((short)this.round.Index % ((short)this.round.CostFactor * (short)this.round.ShieldsPerRow)) *
						(short)this.round.Value) / (short)this.round.ShieldsPerRow) + 232,
					(((short)this.round.Index / ((short)this.round.CostFactor * (short)this.round.ShieldsPerRow)) * 8) + 117,
					SpritePicture(0x9));
			}

			this.round.Index = shield;
		}

		/// <summary>
		/// The food box of the city screen: 0xe8 the step (80 / size, 1 - 8), 0xfc the row height (80 / 0x4a), the line of
		/// a granary, the food stored (at most what the city needs to grow).
		/// </summary>
		private void DrawFoodBox(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (town.TrueExtent != 0)
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(80 / town.TrueExtent, 1, 8);
			else
				this.round.Value = 1;

			short box = (short)this.round.CostFactor;

			if (box == 0)
				throw new Exception("Division by zero");

			this.round.DrawVert = (ushort)(80 / box);

			this.Owner.Services.FloodBox(ScreenBox, 2, 106, 91,
				((short)this.round.CostFactor * (short)this.round.DrawVert) + 12, Hues.Blue);
			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextFoodStorage, 8, 108, Hues.White);
			FillBoxWithMotif(3, 115, (town.TrueExtent * (short)this.round.Value) + 9,
				((short)this.round.CostFactor * (short)this.round.DrawVert) + 2);

			// The granary
			if (town.Has(CityStructure.Granary))
			{
				this.Owner.VideoCard.RenderLine(ScreenBox, 5, 155,
					(town.TrueExtent * (short)this.round.Value) + 9, 155, Hues.Blue);
			}

			// The food stored
			for (ushort grain = 0; ; grain++)
			{
				this.round.Index = grain;
				if ((short)this.Owner.Helpers.ClampValue(town.GrainTally, 0, (town.TrueExtent + 1) * (short)this.round.CostFactor) <= (short)grain)
					break;

				short perRow = (short)(town.TrueExtent + 1);
				this.Owner.Services.DrawPictureToScreen(ScreenBox,
					(((short)this.round.Index % perRow) * (short)this.round.Value) + 4,
					(((short)this.round.Index / perRow) * (short)this.round.DrawVert) + 116,
					SpritePicture(0x8));
			}
		}

		/// <summary>
		/// The resources box of the city screen: three rows (food, shields, trade: the words at 0x70da) of icons, the step
		/// (0xe8) by how many fit; a gap marks where the food the citizens and settlers eat ends, where the support of units
		/// ends (0xd2f6) and where the corruption starts (0x70de less 0xd2e0). 0xd8 the column, 0xfc the first line.
		/// </summary>
		private void DrawResourceRows(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			DrawFrame(2, 23, 124, 65);
			this.round.DrawVert = 0x19;
			this.Owner.Services.FloodBox(ScreenBox, 2, 23, 122, 9, Hues.Blue);
			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextCityResources, 8, (short)this.round.DrawVert, Hues.White);
			this.round.DrawVert = (ushort)(this.round.DrawVert + 8);

			for (ushort row = 0; row < 3; row++)
			{
				this.round.Item = row;
				this.round.Horiz = 4;
				ushort amount = (ushort)((row << 1) + 0x70da);

				int step = row == 0
					? this.Owner.Helpers.ClampValue(116 / ((short)this.Owner.Helpers.ClampValue(this.Processor.PullInt16(this.Processor.SegData.Value16, amount),
						(town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CitySettlers), 999) + 1), 1, 8)
					: this.Owner.Helpers.ClampValue(116 / (this.Processor.PullInt16(this.Processor.SegData.Value16, amount) + 1), 1, 8);

				this.round.Value = (ushort)step;
				ushort icon = 0;

				for (; (short)this.Processor.PullUInt16(this.Processor.SegData.Value16, amount) > (short)icon; icon++)
				{
					this.round.Index = icon;

					// The gaps: the food eaten, the support paid, the corruption
					if (row == 0 && (ushort)(town.TrueExtent * 2 + (short)this.round.SettlerFood * (short)CitySettlers) == icon)
						this.round.Horiz = (ushort)(this.round.Horiz + 4);

					if (row == 1 && CityUnitUpkeep != 0 && icon == CityUnitUpkeep)
						this.round.Horiz = (ushort)(this.round.Horiz + 4);

					if (row == 2 && (ushort)(CityTrade - CityCorruption) == icon)
						this.round.Horiz = (ushort)(this.round.Horiz + 2);

					this.Owner.Services.DrawPictureToScreen(ScreenBox, (short)this.round.Horiz,
						((short)this.round.Item * 8) + (short)this.round.DrawVert,
						SpritePicture((row + 8)));

					this.round.Horiz = (ushort)(this.round.Horiz + this.round.Value);
				}

				this.round.Index = icon;
			}

			this.round.Item = 3;
		}

		/// <summary>
		/// The row of luxuries, taxes and science on the city screen (0xe8 the step by how many fit, 1 - 16; a gap after the
		/// luxuries and after the taxes; 0xf6 where the taxes start)
		/// </summary>
		private void DrawTradeRow()
		{
			this.round.Horiz = 8;
			this.round.Value = (ushort)this.Owner.Helpers.ClampValue(224 / ((short)CityLuxuries + (short)CityScience +
				(short)CityTaxes + (short)CityCorruption + 2), 1, 16);

			DrawTradeIcons(0x70e0, 0xe);

			if (CityLuxuries != 0)
				this.round.Horiz = (ushort)(this.round.Horiz + 8);

			this.round.DrawHoriz = this.round.Horiz;

			DrawTradeIcons(0xe17a, 0xb);

			if (CityTaxes != 0)
				this.round.Horiz = (ushort)(this.round.Horiz + 8);

			DrawTradeIcons(0x70e6, 0xc);
		}

		/// <summary>The icons of one part of the trade (as many as the word says), half the column 0xd8 apart by 0xe8</summary>
		private void DrawTradeIcons(ushort amount, ushort icon)
		{
			ushort drawn = 0;

			for (; (short)drawn < (short)this.Processor.PullGlobalWord(amount); drawn++)
			{
				this.round.Index = drawn;
				this.Owner.Services.DrawPictureToScreen(ScreenBox, (short)this.round.Horiz / 2,
					(short)this.round.DrawVert + 24, SpritePicture(icon));
				this.round.Horiz = (ushort)(this.round.Horiz + this.round.Value);
			}

			this.round.Index = drawn;
		}

		/// <summary>
		/// The shortfalls of the resources box: the food the citizens and settlers eat beyond the food gathered (0x70da) and the
		/// shields of the support beyond the production (0x70dc, 0xd2f6) drawn past the row and shaded; the corruption (0xd2e0)
		/// shaded in the trade row. 0xe8 the step, 0xea the icon, 0xfc the first line.
		/// </summary>
		private void DrawShortfalls(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			int Eaten() => (town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CitySettlers);

			// The food missing
			if ((short)Eaten() > (short)CityFood)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / (Eaten() + 1), 1, 8);
				ushort icon = CityFood;

				for (; (short)Eaten() > (short)icon; icon++)
				{
					this.round.Index = icon;
					this.Owner.Services.DrawPictureToScreen(ScreenBox,
						((short)this.round.Index * (short)this.round.Value) + 8,
						(short)this.round.DrawVert,
						SpritePicture(0x8));
				}

				this.round.Index = icon;
				this.Owner.VideoCard.SubstituteHue(ScreenBox,
					((short)this.round.Value * (short)CityFood) + 8,
					(short)this.round.DrawVert,
					((Eaten() - (short)CityFood) * (short)this.round.Value) + 4,
					8, Hues.White, Hues.Black);
			}

			// The shields of the support missing
			if ((short)CityShields < (short)CityUnitUpkeep)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / ((short)CityShields + 1), 1, 8);
				ushort icon = CityShields;

				for (; (short)icon < (short)CityUnitUpkeep; icon++)
				{
					this.round.Index = icon;
					this.Owner.Services.DrawPictureToScreen(ScreenBox,
						((short)this.round.Index * (short)this.round.Value) + 8,
						(short)this.round.DrawVert + 8,
						SpritePicture(0x9));
				}

				this.round.Index = icon;
				this.Owner.VideoCard.SubstituteHue(ScreenBox,
					((short)CityShields * (short)this.round.Value) + 8,
					(short)this.round.DrawVert + 8,
					((short)CityUnitUpkeep - (short)CityShields) * (short)this.round.Value,
					8, Hues.White, Hues.Black);
			}

			// The corruption
			if (CityCorruption != 0)
			{
				this.round.Value = (ushort)this.Owner.Helpers.ClampValue(116 / ((short)CityTrade + 1), 1, 8);
				this.Owner.VideoCard.SubstituteHue(ScreenBox,
					(((short)CityTrade - (short)CityCorruption) * (short)this.round.Value) + 6,
					(short)this.round.DrawVert + 16,
					((short)this.round.Value * (short)CityCorruption) + 2,
					8, Hues.White, Hues.Black);
			}
		}

		/// <summary>
		/// The citizens along the top of the city screen (0xf4 what DrawCitizenIcons gives back, 0xe8 the width by the size) and
		/// the trade routes (0xbc the route, 0x4c the city at its end): the name and the trade of each, a half of it with a city
		/// of another owner (an eighth of the trade of both, a sixteenth with an own city), written on the first tab (0x2496 0),
		/// a dot on the mini map (tab 2).
		/// </summary>
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
					continue;

				EmptyMessage()
					.City((short)partner)
					.Text(":+");
				City other = this.Owner.World.Towns[partner];
				int shift = (ushort)other.ParticipantIdx != CityOwner ? 3 : 4;
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)(((short)town.RootCommerce + (short)other.RootCommerce + 4) >> shift), 10));
				this.Owner.Words.Append("} ");

				if (CityScreenTab == 2)
					DrawMiniMapDot((ushort)((short)other.Place.X), (ushort)((short)other.Place.Y), 10);

				if (CityScreenTab == 0)
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 98, ((short)this.round.Item * 6) + 179, Hues.LightGreen);
			}

			this.round.Item = 3;
		}

		/// <summary>
		/// The city itself on the mini map (tab 2) and the pollution on the first tab (0x2496 0): 0xe0 how much (the shields over
		/// the divisor 0x6c98 less 20, a quarter of the size by 0xb882 more), 0xfc the step, 0xea the icon (0xb2ba).
		/// </summary>
		private void DrawCityDotAndPollution(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (CityScreenTab == 2)
			{
				City shown = this.Owner.World.Towns[ShownCity];
				DrawMiniMapDot((ushort)((short)shown.Place.X), (ushort)((short)shown.Place.Y), 15);
			}

			if (CityScreenTab != 0)
				return;

			short pollution = CountPollution(town);

			this.round.DrawVert = (ushort)this.Owner.Helpers.ClampValue(100 / (short)this.Owner.Helpers.ClampValue(pollution, 1, 99), 1, 8);

			ushort icon = 0;

			for (; (short)icon < (short)this.round.Pollution; icon++)
			{
				this.round.Index = icon;
				this.Owner.Services.DrawPictureToScreen(ScreenBox,
					((short)this.round.Index * (short)this.round.DrawVert) + 98,
					(this.round.Index & 1) + 161,
					PollutionIconPicture);
			}

			this.round.Index = icon;
		}

		/// <summary>
		/// The buttons Exit and Buy of the city screen of the human, and whether its Change button blinks (0xc0): the city makes
		/// an improvement it has or a part of the spaceship that was launched, a wonder that is built, or a unit when the owner
		/// has 127 of them.
		/// </summary>
		private void DrawExitAndBuyButtons(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			DrawLabelBox(284, 190, 316, 198, GameTexts.TextExit, 12);
			DrawLabelBox(231, 190, 272, 198, GameTexts.TextRename, 9);
			this.round.ChangeBlinks = 0;

			sbyte making = town.PresentMakingIdx;

			// An improvement the city has (-1 - -21)
			if (making < 0 && making >= -21)
			{
				if (town.Has((CityStructure)(-making - 1)))
					this.round.ChangeBlinks = 1;
			}

			// A part of the spaceship (-22 - -24) when the spaceship is on its way
			if (making < -21 && making >= -24 && ((ushort)(1 << (owner & 0xff)) & (ushort)this.Owner.World.StarshipMarkers) != 0)
				this.round.ChangeBlinks = 1;

			// A wonder that is built
			if (making < -24 && this.Owner.World.MarvelTownIdx[Math.Abs(making) - 24] != -1)
				this.round.ChangeBlinks = 1;

			// A unit when there are 127 of them
			if (making >= 0 && this.Owner.World.Participants[owner].TroopTally >= 127)
				this.round.ChangeBlinks = 1;
		}
	}
}
