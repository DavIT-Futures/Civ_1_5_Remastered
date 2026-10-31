
using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1d12.
	/// </summary>
	public partial class CityManagement : GameModule
	{
		private readonly UnitMemory troops;

		private CityRound round;

		public CityManagement(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
			this.round = new CityRound(owner.Processor);
		}

		/// <summary>The index (0 - 20) of the offset among the tiles of a city, 0xffff when it is none of them</summary>
		public ushort ObtainCityTileIndex(short horizLoc, short vertLoc)
		{
			this.Processor.Journal.OpenScope($"ObtainCityTileIndex({horizLoc}, {vertLoc})");

			int index = CitizenSpecialistRules.FindCityTileIndex(horizLoc, vertLoc);
			ushort result = (index >= 0) ? (ushort)index : (ushort)0xffff;

			this.Processor.Journal.CloseScope("ObtainCityTileIndex");
			return result;
		}

		/// <summary>
		/// The city screen, first part: the units of the city listed, the screen cleared, the title "NAME (Pop:...)" (0xe8
		/// the population sum 1 + 2 + ... + size), the map of the city (the view 0xd4cc / 0xd75e on it): each of the 0x6540
		/// tiles around that the owner has seen (all for a computer player) is drawn, a tile of another player only
		/// repainted, a tile worked by a neighbouring city (the array at BP-0x8e) framed. The locals of HandleCity are used
		/// as in the original.
		/// </summary>
		private void RenderCityHeaderAndMap(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			this.Owner.UnitOps.ConstructUnitsFromCityList(townIdx);
			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);
			RenderFrame(2, 1, 208, 21);

			// The population: 1 + 2 + ... + the size
			this.round.Value = 0;
			ushort citizen = 1;

			for (; town.TrueExtent >= (short)citizen; citizen++)
			{
				this.round.Index = citizen;
				this.round.Value = (ushort)(this.round.Value + citizen);
			}

			this.round.Index = citizen;
			DrawingBox.TypefaceIdx = 2;

			EmptyMessage()
				.City(townIdx);
			this.Owner.Words.Uppercase();
			this.Owner.Words.Append(" (Pop:");
			this.Owner.Helpers.PopulationToText((short)this.round.Value);
			this.Owner.Words.Append(")");
			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 104, 2, Hues.White);

			RenderFrame(127, 23, 208, 104);

			// The map of the city
			MapViewHoriz = (ushort)((short)town.Place.X - 5);
			MapViewVert = (ushort)((short)town.Place.Y - 3);

			this.Owner.MapOps.RenderMapTile(town.Place.X, town.Place.Y);

			ushort tile = 0;

			for (; (short)tile < (short)CityTiles; tile++)
			{
				this.round.Index = tile;
				short horizLoc = (short)(town.Place.X + this.Owner.TownDeltas[tile].X);
				short vertLoc = (short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
				this.round.TileHoriz = (ushort)horizLoc;
				this.round.TileVert = (ushort)vertLoc;

				short owner = (short)CityOwner;

				if (owner == this.Owner.World.PersonParticipantIdx &&
					(this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
				{
					continue;
				}

				this.round.Value = this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

				if (this.round.Value != 0xffff && this.round.Value != (ushort)owner)
				{
					// A tile of another player
					this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
					continue;
				}

				this.Owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				ushort spot = (ushort)(short)(10 * this.Owner.TownDeltas[tile].X + this.Owner.TownDeltas[tile].Y * 2);

				if (this.round.Frame(0x8e - spot) != 0)
				{
					// A tile worked by a neighbouring city
					this.Owner.Dialogs.RenderBox(
						(horizLoc - (short)MapViewHoriz) * 16 + 80,
						(vertLoc - (short)MapViewVert) * 16 + 8,
						15, 15, Hues.LightRed);
				}
			}

			this.round.Index = tile;
		}

		/// <summary>
		/// The city screen (marker 1): the frames of the resources and the info area with its four tabs, the tab 0x2496 lit,
		/// and the mini map when that tab (2) is open
		/// </summary>
		private void RenderCityScreenFrames()
		{
			RenderFrame(2, 67, 124, 104);
			RenderFrame(95, 106, 227, 197);
			RenderLabelBox(95, 106, 128, 114, GameTexts.TextInfo, 9);
			RenderLabelBox(129, 106, 160, 114, GameTexts.TextHappy, 9);
			RenderLabelBox(161, 106, 193, 114, GameTexts.TextMap, 9);
			RenderLabelBox(194, 106, 226, 114, GameTexts.TextView, 9);

			this.Owner.VideoCard.SubstituteHue(ScreenBox, (33 * (short)CityScreenTab) + 96, 107, 32, 7, Hues.LightBlue, Hues.White);

			if (CityScreenTab == 2)
				RenderMiniMap();
		}
	}
}
