namespace Civilization1Remaster.Core.Game
{
	public partial class Cities : GameComponent, ICities
	{
		private readonly UnitMemory troops;

		private CityRound round;

		public Cities(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
			this.round = new CityRound(owner.Processor);
		}

		public ushort ObtainCityTileIndex(short horizontalLocation, short verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainCityTileIndex({horizontalLocation}, {verticalLocation})");

			int index = CitizenSpecialistRules.FindCityTileIndex(horizontalLocation, verticalLocation);
			ushort result = (index >= 0) ? (ushort)index : (ushort)0xffff;

			return result;
		}

		private void DrawCityHeaderAndMap(short townIndex)
		{
			City town = this.Owner.World.Towns[townIndex];

			this.Owner.UnitOperations.CreateUnitsFromCityList(townIndex);
			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);
			DrawFrame(2, 1, 208, 21);

			this.round.Value = 0;
			ushort citizen = 1;

			for (; town.TrueExtent >= (short)citizen; citizen++)
			{
				this.round.Index = citizen;
				this.round.Value = (ushort)(this.round.Value + citizen);
			}

			this.round.Index = citizen;
			DrawingBox.TypefaceIndex = 2;

			EmptyMessage()
				.City(townIndex);
			this.Owner.Words.Uppercase();
			this.Owner.Words.Append(" (Pop:");
			this.Owner.Helpers.FormatPopulation((short)this.round.Value);
			this.Owner.Words.Append(")");
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 104, 2, Hues.White);

			DrawFrame(127, 23, 208, 104);

			MapView.Horizontal = (ushort)((short)town.Place.X - 5);
			MapView.Vertical = (ushort)((short)town.Place.Y - 3);

			this.Owner.MapOperations.DrawMapTile(town.Place.X, town.Place.Y);

			ushort tile = 0;

			for (; (short)tile < (short)CityTurn.Tiles; tile++)
			{
				this.round.Index = tile;
				short horizontalLocation = (short)(town.Place.X + this.Owner.TownDeltas[tile].X);
				short verticalLocation = (short)(town.Place.Y + this.Owner.TownDeltas[tile].Y);
				this.round.TileHorizontal = (ushort)horizontalLocation;
				this.round.TileVertical = (ushort)verticalLocation;

				short owner = (short)CityTurn.Owner;

				if (owner == this.Owner.World.PersonParticipantIndex &&
					(this.Owner.World.ChartSight[(ushort)horizontalLocation, (ushort)verticalLocation] & (1 << (owner & 0xff))) == 0)
				{
					continue;
				}

				this.round.Value = this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation);

				if (this.round.Value != 0xffff && this.round.Value != (ushort)owner)
				{
					this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
					continue;
				}

				this.Owner.MapOperations.DrawMapTile(horizontalLocation, verticalLocation);

				ushort spot = (ushort)(short)((10 * this.Owner.TownDeltas[tile].X) + (this.Owner.TownDeltas[tile].Y * 2));

				if (this.round.Frame(0x8e - spot) != 0)
				{
					this.Owner.Dialogs.DrawBox(
						((horizontalLocation - (short)MapView.Horizontal) * 16) + 80,
						((verticalLocation - (short)MapView.Vertical) * 16) + 8,
						15, 15, Hues.LightRed);
				}
			}

			this.round.Index = tile;
		}

		private void DrawCityScreenFrames()
		{
			DrawFrame(2, 67, 124, 104);
			DrawFrame(95, 106, 227, 197);
			DrawLabelBox(95, 106, 128, 114, GameTexts.TextInformation, 9);
			DrawLabelBox(129, 106, 160, 114, GameTexts.TextHappy, 9);
			DrawLabelBox(161, 106, 193, 114, GameTexts.TextMap, 9);
			DrawLabelBox(194, 106, 226, 114, GameTexts.TextView, 9);

			this.Owner.VideoCard.SubstituteHue(Screen.Box, (33 * (short)CityTurn.ScreenTab) + 96, 107, 32, 7, Hues.LightBlue, Hues.White);

			if (CityTurn.ScreenTab == 2)
			{
				DrawMiniMap();
			}
		}
	}
}
