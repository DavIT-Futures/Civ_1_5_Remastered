using System;

namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		private const ushort MiniMapColumnShifts = 0x280c;

		private const ushort MiniMapRowShifts = 0x2810;

		public void DrawCityTile(short townIndex, ushort townTile, short marker)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCityTile({townIndex}, {townTile}, {marker})");

			short horizontalLocation = (short)(this.Owner.World.Towns[townIndex].Place.X + this.Owner.TownDeltas[townTile].X);
			short verticalLocation = (short)(this.Owner.World.Towns[townIndex].Place.Y + this.Owner.TownDeltas[townTile].Y);
			bool shown = marker == 1;
			short total = 0;
			short spacing = 0;

			if (shown)
			{
				this.Owner.MapOperations.DrawMapTile(horizontalLocation, verticalLocation);

				for (ushort resource = 0; resource < 3; resource++)
				{
					total += (short)MayCityWorkTile(horizontalLocation, verticalLocation, resource);
				}

				spacing = (short)(total <= 4 ? 8 : total <= 6 ? 5 : 3);
			}
			else
			{
				CityTurn.TileDotted = 1;

				this.Owner.Services.AssignDot(2, horizontalLocation + 0x50, verticalLocation, (ushort)this.Owner.World.Towns[townIndex].ParticipantIndex);
			}

			short iconHorizontal = 0;
			short iconVertical = 0;

			for (ushort resource = 0; resource < 3; resource++)
			{
				short amount = (short)MayCityWorkTile(horizontalLocation, verticalLocation, resource);

				this.Processor.EmitGlobalWord((ushort)((resource << 1) + 0x70da), (ushort)(this.Processor.PullGlobalWord((ushort)((resource << 1) + 0x70da)) + amount));

				for (; amount > 0; amount--)
				{
					if (!shown)
					{
						continue;
					}

					this.Owner.Services.DrawPictureToScreen(Screen.Box,
						161 + (this.Owner.TownDeltas[townTile].X * 16) + iconHorizontal, 57 + (this.Owner.TownDeltas[townTile].Y * 16) + iconVertical,
						Tables.SpritePicture(resource + 8));

					if (iconHorizontal < 8)
					{
						iconHorizontal += spacing;
					}
					else
					{
						iconHorizontal = 0;
						iconVertical += 8;
					}
				}
			}

			if (shown && total == 0)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box,
					165 + (this.Owner.TownDeltas[townTile].X * 16), 61 + (this.Owner.TownDeltas[townTile].Y * 16),
					Tables.SpritePicture(0xd));
			}

			CityTurn.TileDotted = 0;
		}

		public ushort MayCityWorkTile(int horizontalLocation, int verticalLocation, ushort resource)
		{
			using var _ = this.Processor.Journal.Scope($"MayCityWorkTile({horizontalLocation}, {verticalLocation}, {resource})");

			short yield = 0;

			if (this.Owner.MapOperations.CheckMapLimits(horizontalLocation, verticalLocation) != 0)
			{
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
				int kind = this.Owner.MapOperations.ContainsSpecialResource(horizontalLocation, verticalLocation) != 0 ? terrain + 12 : terrain;

				yield = (sbyte)Tables.TerrainField(kind, TerrainTable.Food + resource);

				ushort improvements = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

				if ((this.Owner.World.DiagonalMarkers & 2) == 0)
				{
					improvements = (ushort)((short)terrain > 2 ? 4 : 2);

					if (terrain != 1)
					{
						improvements |= 8;
					}
				}

				if (terrain != MapRules.OceanTerrain)
				{
					if (resource == 0 && (improvements & 2) != 0)
					{
						yield += (short)(0xffff - Tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Irrigation));
					}

					if (resource == 1 && (improvements & 4) != 0)
					{
						yield += (short)(0xffff - Tables.TerrainImprovementWord(terrain, TerrainImprovementTable.Mining));
					}

					if (resource == 2 && (improvements & 8) != 0 && (short)terrain <= 2)
					{
						yield++;
					}
				}

				if (resource == 1 && (terrain == 2 || terrain == 0xb) && (((7 * horizontalLocation) + (11 * verticalLocation)) & 2) != 0)
				{
					yield = 0;
				}

				ushort townIndex = CityTurn.ShownCity;

				if (yield != 0 && resource == 2 && ObtainWonderCityIdentifier(3) == townIndex)
				{
					yield++;
				}

				if ((improvements & 0x10) != 0)
				{
					yield += (short)(yield / 2);
				}

				if (yield > 2 && (this.Owner.World.Towns[townIndex].ConditionMarker & 0x40) == 0)
				{
					if (this.Owner.World.Participants[CityTurn.Owner].RegimeKind <= 1)
					{
						yield--;
					}

					if (CityTurn.TileDotted != 0)
					{
						Turn.AnarchyPressure = (ushort)(Turn.AnarchyPressure - 2);
					}
				}

				if (yield != 0 && resource == 2)
				{
					if (CityTurn.TileDotted != 0)
					{
						Turn.DemocracyPressure = (ushort)(Turn.DemocracyPressure + 1);
					}

					short needed = (short)((this.Owner.World.Towns[townIndex].ConditionMarker & 0x40) != 0 ? 2 : 4);

					if (needed <= this.Owner.World.Participants[CityTurn.Owner].RegimeKind)
					{
						yield++;
					}
				}

				if ((improvements & 0x40) != 0)
				{
					yield = (short)((yield + 1) / 2);
				}

				if (yield < 0)
				{
					yield = 0;
				}
			}

			return (ushort)yield;
		}

		public ushort PlayerEnjoysWonderEffect(short participantIndex, short miracleIdentifier)
		{
			using var _ = this.Processor.Journal.Scope($"PlayerEnjoysWonderEffect({participantIndex}, {miracleIdentifier})");

			bool enjoys = false;

			if (!IsWonderObsolete(miracleIdentifier))
			{
				ushort townIndex = (ushort)this.Owner.World.MarvelTownIndex[miracleIdentifier];

				enjoys = townIndex != 0xffff && WonderCityRules.ReadsCity((short)townIndex) &&
					this.Owner.World.Towns[townIndex].ParticipantIndex == participantIndex;
			}

			ushort result = (ushort)(enjoys ? 1 : 0);

			return result;
		}

		public ushort ObtainWonderCityIdentifier(short miracleIdentifier)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainWonderCityID({miracleIdentifier})");

			ushort townIndex = IsWonderObsolete(miracleIdentifier) ? (ushort)0xffff : (ushort)this.Owner.World.MarvelTownIndex[miracleIdentifier];

			return townIndex;
		}

		public void AttachSmog(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"AttachSmog({horizontalLocation}, {verticalLocation})");

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 0x40) == 0)
			{
				this.Owner.MapOperations.AssignTileImprovements(0x40, horizontalLocation, verticalLocation);

				this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);

				this.Owner.World.DirtyCellTally++;
			}
		}

		public void AssignCitizenType(ushort citizen, ushort specialist)
		{
			using var _ = this.Processor.Journal.Scope($"AssignCitizenType({citizen}, {specialist})");

			short cityIndex = (short)CityTurn.ShownCity;
			ushort currentMarkers = this.Owner.World.Towns[cityIndex].SpecialistMarkers;
			ushort updated = CitizenSpecialistRules.SetSpecialistType(currentMarkers, citizen, (CitizenSpecialistRules.SpecialistType)specialist);
			this.Owner.World.Towns[cityIndex].SpecialistMarkers = updated;
		}

		public ushort ObtainCitizenType(ushort citizen)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainCitizenType({citizen})");

			short cityIndex = (short)CityTurn.ShownCity;
			ushort markers = this.Owner.World.Towns[cityIndex].SpecialistMarkers;
			var specificationType = CitizenSpecialistRules.GetSpecialistType(markers, citizen);
			ushort result = (ushort)specificationType;

			return result;
		}

		public ushort TallySpecialists(ushort specialist)
		{
			using var _ = this.Processor.Journal.Scope($"TallySpecialists({specialist})");

			short cityIndex = (short)CityTurn.ShownCity;
			ushort markers = this.Owner.World.Towns[cityIndex].SpecialistMarkers;
			int tally = CitizenSpecialistRules.CountSpecialists(markers, (CitizenSpecialistRules.SpecialistType)specialist);
			return (ushort)tally;
		}

		public void ReshuffleCitizens(short townIndex, ushort workerTally)
		{
			using var _ = this.Processor.Journal.Scope($"ReshuffleCitizens({townIndex}, {workerTally})");

			City town = this.Owner.World.Towns[townIndex];

			CityTurn.Happy =
				(ushort)this.Owner.Helpers.ClampValue((short)CityTurn.Happy, 0, town.TrueExtent);

			while (CityTurn.ExcessUnhappy != 0 &&
				(short)CityTurn.Unhappy < (short)CityTurn.ExcessUnhappy)
			{
				CityTurn.ExcessUnhappy = (ushort)(CityTurn.ExcessUnhappy - 1);
				CityTurn.Unhappy = (ushort)(CityTurn.Unhappy + 1);
			}

			ushort taxmen = (ushort)this.Owner.Helpers.ClampValue((short)CityTurn.Unhappy, 0, town.TrueExtent);

			while (true)
			{
				CityTurn.Unhappy = taxmen;

				short room = (short)this.Owner.Helpers.ClampValue(town.TrueExtent - (short)workerTally, 0, 99);

				if ((short)(CityTurn.Happy + CityTurn.Unhappy) <= room)
				{
					break;
				}

				if (CityTurn.ExcessUnhappy != 0)
				{
					CityTurn.ExcessUnhappy = (ushort)(CityTurn.ExcessUnhappy - 1);
				}
				else
				{
					CityTurn.Happy = (ushort)(CityTurn.Happy - 1);
					CityTurn.Happy =
						(ushort)this.Owner.Helpers.ClampValue((short)CityTurn.Happy, 0, town.TrueExtent);
				}

				CityTurn.Unhappy = (ushort)(CityTurn.Unhappy - 1);
				taxmen = (ushort)this.Owner.Helpers.ClampValue((short)CityTurn.Unhappy, 0, town.TrueExtent);
			}
		}

		public ushort DrawCitizenIcons(short townIndex, short horizontalLocation, short verticalLocation, ushort specialists, ushort breadth)
		{
			using var _ = this.Processor.Journal.Scope($"RenderCitizenIcons({townIndex}, {horizontalLocation}, {verticalLocation}, {specialists}, {breadth})");

			sbyte size = this.Owner.World.Towns[townIndex].TrueExtent;

			ushort step = (short)(7 * size) > (short)breadth ? (ushort)CentralProcessor.SignedQuotient((short)breadth, size) : (ushort)7;
			short citizenIndex;

			for (citizenIndex = 0; citizenIndex < (short)CityTurn.Happy; citizenIndex++)
			{
				horizontalLocation = DrawCitizen(horizontalLocation, verticalLocation, CitizenPicture(citizenIndex & 1), step);
			}

			for (citizenIndex = 0; (short)(this.Owner.World.Towns[townIndex].TrueExtent - specialists - CityTurn.Happy -
				CityTurn.Unhappy) > citizenIndex; citizenIndex++)
			{
				horizontalLocation = DrawCitizen(horizontalLocation, verticalLocation, CitizenPicture(2 + (citizenIndex & 1)), step);
			}

			if (citizenIndex != 0)
			{
				horizontalLocation += 2;
			}

			for (citizenIndex = 0; citizenIndex < (short)CityTurn.Unhappy; citizenIndex++)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, horizontalLocation, verticalLocation,
					CitizenPicture(4 + (citizenIndex & 1)));

				if (citizenIndex < (short)CityTurn.ExcessUnhappy)
				{
					this.Owner.VideoCard.SubstituteHue(Screen.Box, horizontalLocation, verticalLocation, 12, 14, Hues.Magenta, Hues.LightRed);
				}

				horizontalLocation += (short)step;
			}

			horizontalLocation += 4;

			for (citizenIndex = 0; citizenIndex < (short)specialists; citizenIndex++)
			{
				horizontalLocation = DrawCitizen(horizontalLocation, verticalLocation, this.Processor.PullGlobalWord((ushort)((ObtainCitizenType((ushort)citizenIndex) << 1) + 0x6ea0)), step);
			}

			return step;
		}

		public void DrawResourceIcon(short horizontalLocationOrigin, short horizontalLocationTo, short verticalLocationTo)
		{
			using var _ = this.Processor.Journal.Scope($"RenderResourceIcon({horizontalLocationOrigin}, {horizontalLocationTo}, {verticalLocationTo})");

			if (horizontalLocationOrigin < 22 || horizontalLocationOrigin > 24)
			{
				horizontalLocationOrigin--;

				if (horizontalLocationOrigin >= 24)
				{
					horizontalLocationOrigin -= 3;
				}

				if (horizontalLocationOrigin >= 40)
				{
					this.Owner.VideoCard.RenderImage(
						Screen.PictureScreenBox,
						((horizontalLocationOrigin & 1) * 19) + 161, 100, 18, 10,
						Screen.Box,
						horizontalLocationTo, verticalLocationTo);
				}
				else
				{
					this.Owner.VideoCard.RenderImage(
						Screen.PictureScreenBox,
						((horizontalLocationOrigin / 5) * 19) + 161,
						((horizontalLocationOrigin % 5) * 10) + 50,
						18, 10,
						Screen.Box,
						horizontalLocationTo, verticalLocationTo);
				}
			}
		}

		public void DrawFrame(int horizontalLocation, int verticalLocation, int firstHorizontalSpot, int firstVerticalSpot)
		{
			using var _ = this.Processor.Journal.Scope($"RenderFrame({horizontalLocation}, {verticalLocation}, {firstHorizontalSpot}, {firstVerticalSpot})");

			int breadth = firstHorizontalSpot - horizontalLocation;
			int altitude = firstVerticalSpot - verticalLocation;

			FillBoxWithMotif(horizontalLocation, verticalLocation, breadth, altitude);

			this.Owner.Dialogs.DrawBox(horizontalLocation, verticalLocation, breadth, altitude, Hues.Blue);
		}

		public void FillBoxWithMotif(int horizontalLocation, int verticalLocation, int breadth, int altitude)
		{
			using var _ = this.Processor.Journal.Scope($"FloodBoxWithMotif({horizontalLocation}, {verticalLocation}, {breadth}, {altitude})");

			if (Screen.VideoGraphicsMode == 0)
			{
				this.Owner.Services.FloodBox(Screen.Box, horizontalLocation, verticalLocation, breadth, altitude, Hues.LightBlue);
			}
			else
			{
				int boxVerticalLocation = verticalLocation;
				int boxAltitude = altitude;

				while (boxAltitude > 0)
				{
					int tileAltitude = Math.Min(boxAltitude, 16);
					int boxHorizontalLocation = horizontalLocation;
					int boxBreadth = breadth;

					while (boxBreadth > 0)
					{
						int tileBreadth = Math.Min(boxBreadth, 16);

						this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox,
							208, 100, tileBreadth, tileAltitude,
							Screen.Box,
							boxHorizontalLocation, boxVerticalLocation);

						boxHorizontalLocation += tileBreadth;
						boxBreadth -= tileBreadth;
					}

					boxVerticalLocation += tileAltitude;
					boxAltitude -= tileAltitude;
				}
			}
		}

		public void DrawLabelBox(int horizontalLocation, int verticalLocation, int firstHorizontalSpot, int firstVerticalSpot, GameText label, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"RenderLabelBox({horizontalLocation}, {verticalLocation}, {firstHorizontalSpot}, {firstVerticalSpot}, 0x{label.Address:x4}, {style})");

			this.Owner.Services.FloodBox(Screen.Box,
				horizontalLocation, verticalLocation, firstHorizontalSpot - horizontalLocation, firstVerticalSpot - verticalLocation, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation, firstHorizontalSpot, verticalLocation, (ushort)((style < 8) ? (style + 8) : 7));

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation, horizontalLocation, firstVerticalSpot, (ushort)((style < 8) ? (style + 8) : 7));

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation + 1, firstVerticalSpot, firstHorizontalSpot, firstVerticalSpot, (ushort)((style < 8) ? 8 : (style - 8)));

			this.Owner.VideoCard.RenderLine(Screen.Box,
				firstHorizontalSpot, verticalLocation, firstHorizontalSpot, firstVerticalSpot, (ushort)((style < 0x8) ? 8 : (style - 8)));

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(label,
				((horizontalLocation + firstHorizontalSpot) / 2) + 1, ((verticalLocation + firstVerticalSpot) / 2) - 2, (ushort)(style ^ 8));
		}

		public void DrawMiniMap()
		{
			using var _ = this.Processor.Journal.Scope("RenderMiniMap()");

			this.Owner.Services.FloodBox(Screen.Box, 101, 117, 120, 75, Hues.Black);

			short humanIndex = this.Owner.World.PersonParticipantIndex;

			for (int verticalLocation = 0; verticalLocation < 50; verticalLocation++)
			{
				for (int column = 0; column < 80; column++)
				{
					int horizontalLocation = this.Owner.Routing.TunePosition(this.Owner.World.Participants[humanIndex].HorizontalBegin + column - 40);

					if ((this.Owner.World.ChartSight[(ushort)horizontalLocation, verticalLocation] & (1 << humanIndex)) == 0)
					{
						continue;
					}

					ushort quarter = (ushort)(((verticalLocation & 1) << 1) + (column & 1));
					int dotHorizontal = ((column / 2) * 3) + (sbyte)this.Processor.PullGlobalByte((ushort)(MiniMapColumnShifts + quarter)) + 0x65;
					int dotVertical = ((verticalLocation / 2) * 3) + (sbyte)this.Processor.PullGlobalByte((ushort)(MiniMapRowShifts + quarter)) + 0x76;
					ushort hue = (ushort)(this.Owner.MapOperations.ObtainTerrainType((short)horizontalLocation, verticalLocation) == MapRules.OceanTerrain ? 1 : 2);

					this.Owner.Services.FloodBox(Screen.Box, dotHorizontal, dotVertical, 2, 2, hue);
				}
			}

			this.Owner.Dialogs.DrawBox(100, 117, 121, 75, Hues.LightBlue);
		}

		public void DrawMiniMapDot(int horizontalLocation, int verticalLocation, ushort style)
		{
			using var _ = this.Processor.Journal.Scope($"RenderMiniMapDot({horizontalLocation}, {verticalLocation}, {style})");

			if (CityTurn.ScreenTab == 2)
			{
				horizontalLocation = this.Owner.Routing.TunePosition(
					horizontalLocation - this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].HorizontalBegin + 40);

				this.Owner.Services.FloodBox(Screen.Box,
					((horizontalLocation * 3) / 2) + 101, ((verticalLocation * 3) / 2) + 118, 2, 2, style);
			}
		}

		private bool IsWonderObsolete(short miracleIdentifier)
		{
			short obsoletingTechnology = this.Processor.PullSignedWord(this.Processor.SegmentData.Word, (ushort)(0x10fe + (miracleIdentifier << 1)));

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, obsoletingTechnology) != 0)
				{
					return true;
				}
			}

			return false;
		}

		private short DrawCitizen(short horizontalLocation, short verticalLocation, ushort picture, ushort step)
		{
			this.Owner.Services.DrawPictureToScreen(Screen.Box, horizontalLocation, verticalLocation, picture);

			return (short)(horizontalLocation + step);
		}
	}
}
