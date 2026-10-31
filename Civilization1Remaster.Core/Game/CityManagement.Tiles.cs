
using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The tiles, wonders and citizens of the cities; the frames, labels and the small map of the city screen</summary>
	partial class CityManagement
	{
		/// <summary>
		/// Draws the small map of the city screen: the 80 x 50 tiles around the human player's view (columns from HorizBegin - 40),
		/// every seen tile as a 2 x 2 dot (ocean in colour 1, land in colour 2), two tiles in every 3 pixels: the four tiles of
		/// a 2 x 2 block are shifted by the offsets of MiniMapColumnShifts and MiniMapRowShifts
		/// </summary>
		/// <summary>The column shifts of the four tiles of a 2 x 2 block of the small map (bytes 0, 2, 0, 1)</summary>
		private const ushort MiniMapColumnShifts = 0x280c;

		/// <summary>The row shifts of the four tiles of a 2 x 2 block of the small map (bytes -1, 0, 1, 2)</summary>
		private const ushort MiniMapRowShifts = 0x2810;

		/// <summary>
		/// A tile of the city (TownDeltas[townTile] from the city): with marker 1 draws it on the city screen with the icons of
		/// its food, shields and trade (MayCityWorkTile 0 - 2; closer together the more there are, a mark when there are none),
		/// else puts the city's player on the tile in map layer 2 (0x2494 set meanwhile). Either way the amounts are added to the
		/// totals at 0x70da.
		/// </summary>
		public void RenderCityTile(short townIdx, ushort townTile, short marker)
		{
			this.Processor.Journal.OpenScope($"RenderCityTile({townIdx}, {townTile}, {marker})");

			short horizLoc = (short)(this.Owner.World.Towns[townIdx].Place.X + this.Owner.TownDeltas[townTile].X);
			short vertLoc = (short)(this.Owner.World.Towns[townIdx].Place.Y + this.Owner.TownDeltas[townTile].Y);
			bool shown = marker == 1;
			short total = 0;
			short spacing = 0;

			if (shown)
			{
				this.Owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				for (ushort resource = 0; resource < 3; resource++)
				{
					total += (short)MayCityWorkTile(horizLoc, vertLoc, resource);
				}

				spacing = (short)(total <= 4 ? 8 : total <= 6 ? 5 : 3);
			}
			else
			{
				CityTileDotted = 1;

				this.Owner.Services.AssignDot(2, horizLoc + 0x50, vertLoc, (ushort)this.Owner.World.Towns[townIdx].ParticipantIdx);
			}

			short iconHoriz = 0;
			short iconVert = 0;

			for (ushort resource = 0; resource < 3; resource++)
			{
				short amount = (short)MayCityWorkTile(horizLoc, vertLoc, resource);

				this.Processor.EmitGlobalWord((ushort)((resource << 1) + 0x70da), (ushort)(this.Processor.PullGlobalWord((ushort)((resource << 1) + 0x70da)) + amount));

				for (; amount > 0; amount--)
				{
					if (!shown)
						continue;

					this.Owner.Services.RenderPictureToScreen(ScreenBox,
						161 + (this.Owner.TownDeltas[townTile].X * 16) + iconHoriz, 57 + (this.Owner.TownDeltas[townTile].Y * 16) + iconVert,
						SpritePicture((resource + 8)));

					if (iconHoriz < 8)
					{
						iconHoriz += spacing;
					}
					else
					{
						iconHoriz = 0;
						iconVert += 8;
					}
				}
			}

			if (shown && total == 0)
			{
				this.Owner.Services.RenderPictureToScreen(ScreenBox,
					165 + (this.Owner.TownDeltas[townTile].X * 16), 61 + (this.Owner.TownDeltas[townTile].Y * 16),
					SpritePicture(0xd));
			}

			CityTileDotted = 0;

			this.Processor.Journal.CloseScope("RenderCityTile");
		}

		/// <summary>
		/// What the tile yields to the city 0x653e of player 0x6548: food (resource 0), shields (1) or trade (2). The terrain's
		/// value (of the terrain, or of the terrain with the special resource) with irrigation (bit 2), mine (4) and road (8, trade on terrain 0 - 2)
		/// — the real improvements only with DiagMarkers bit 2, else irrigation (terrain 0 - 2) or mine and a road (not on terrain
		/// 1) assumed; no shields on every other tile of terrain 2 and 11; the Colossus, the railroad (bit 0x10: 1.5 times), -1 above
		/// 2 under governments 0 - 1 unless the city celebrates (0x40; 0xe3c2 -= 2 while 0x2494), +1 trade under government 2+
		/// celebrating or 4+, and pollution (bit 0x40) halves it. 0 outside the map, never negative.
		/// </summary>
		public ushort MayCityWorkTile(int horizLoc, int vertLoc, ushort resource)
		{
			this.Processor.Journal.OpenScope($"MayCityWorkTile({horizLoc}, {vertLoc}, {resource})");

			short yield = 0;

			if (this.Owner.MapOps.VerifyMapLimits(horizLoc, vertLoc) != 0)
			{
				ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				// The terrains with a special resource are the records 12 - 23
				int kind = this.Owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0 ? terrain + 12 : terrain;

				yield = (sbyte)TerrainField(kind, TerrainTable.Food + resource);

				ushort improvements = this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

				if ((this.Owner.World.DiagMarkers & 2) == 0)
				{
					improvements = (ushort)((short)terrain > 2 ? 4 : 2);

					if (terrain != 1)
					{
						improvements |= 8;
					}
				}

				if (terrain != MapRules.OceanTerrain)
				{
					// The bonus of irrigation and of a mine: -1 - the value of the terrain improvement table
					if (resource == 0 && (improvements & 2) != 0)
					{
						yield += (short)(0xffff - TerrainImprovementWord(terrain, TerrainImprovementTable.Irrigation));
					}

					if (resource == 1 && (improvements & 4) != 0)
					{
						yield += (short)(0xffff - TerrainImprovementWord(terrain, TerrainImprovementTable.Mining));
					}

					if (resource == 2 && (improvements & 8) != 0 && (short)terrain <= 2)
					{
						yield++;
					}
				}

				if (resource == 1 && (terrain == 2 || terrain == 0xb) && ((7 * horizLoc + 11 * vertLoc) & 2) != 0)
				{
					yield = 0;
				}

				ushort townIdx = ShownCity;

				if (yield != 0 && resource == 2 && ObtainWonderCityID(3) == townIdx)
				{
					yield++;
				}

				if ((improvements & 0x10) != 0)
				{
					yield += (short)(yield / 2);
				}

				if (yield > 2 && (this.Owner.World.Towns[townIdx].ConditionMarker & 0x40) == 0)
				{
					if (this.Owner.World.Participants[CityOwner].RegimeKind <= 1)
					{
						yield--;
					}

					if (CityTileDotted != 0)
					{
						AnarchyPressure = (ushort)(AnarchyPressure - 2);
					}
				}

				if (yield != 0 && resource == 2)
				{
					if (CityTileDotted != 0)
					{
						DemocracyPressure = (ushort)(DemocracyPressure + 1);
					}

					short needed = (short)((this.Owner.World.Towns[townIdx].ConditionMarker & 0x40) != 0 ? 2 : 4);

					if (needed <= this.Owner.World.Participants[CityOwner].RegimeKind)
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

			this.Processor.Journal.CloseScope("MayCityWorkTile");

			return (ushort)yield;
		}

		/// <summary>
		/// Does the player have the effect of the wonder: it stands in a city of the player and is not obsolete
		/// </summary>
		/// <returns>1 or 0</returns>
		public ushort PlayerEnjoysWonderEffect(short participantIdx, short miracleIdent)
		{
			this.Processor.Journal.OpenScope($"PlayerEnjoysWonderEffect({participantIdx}, {miracleIdent})");

			bool enjoys = false;

			if (!IsWonderObsolete(miracleIdent))
			{
				ushort townIdx = (ushort)this.Owner.World.MarvelTownIdx[miracleIdent];

				enjoys = townIdx != 0xffff && WonderCityRules.ReadsCity((short)townIdx, this.Owner.TurnHooks.ClassicWonderCities) &&
					this.Owner.World.Towns[townIdx].ParticipantIdx == participantIdx;
			}

			ushort result = (ushort)(enjoys ? 1 : 0);

			this.Processor.Journal.CloseScope("PlayerEnjoysWonderEffect");

			return result;
		}

		/// <summary>
		/// The city of the wonder, also when the wonder is not finished (-1)
		/// </summary>
		/// <returns>City index, or 0xffff when the wonder is obsolete</returns>
		public ushort ObtainWonderCityID(short miracleIdent)
		{
			this.Processor.Journal.OpenScope($"ObtainWonderCityID({miracleIdent})");

			ushort townIdx = IsWonderObsolete(miracleIdent) ? (ushort)0xffff : (ushort)this.Owner.World.MarvelTownIdx[miracleIdent];

			this.Processor.Journal.CloseScope("ObtainWonderCityID");

			return townIdx;
		}

		/// <summary>
		/// Pollution (improvement bit 0x40) appears on the tile, unless it is there already; the human player sees it
		/// </summary>
		public void AttachSmog(int horizLoc, int vertLoc)
		{
			this.Processor.Journal.OpenScope($"AttachSmog({horizLoc}, {vertLoc})");

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x40) == 0)
			{
				this.Owner.MapOps.AssignTileImprovements(0x40, horizLoc, vertLoc);

				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

				this.Owner.World.DirtyCellTally++;
			}

			this.Processor.Journal.CloseScope("AttachSmog");
		}

		/// <summary>The kind of specialist of the citizen of the shown city (LaborerMarkers2)</summary>
		public void AssignCitizenType(ushort citizen, ushort specialist)
		{
			this.Processor.Journal.OpenScope($"AssignCitizenType({citizen}, {specialist})");

			short cityIdx = (short)ShownCity;
			ushort currentMarkers = this.Owner.World.Towns[cityIdx].LaborerMarkers2;
			ushort updated = CitizenSpecialistRules.SetSpecialistType(currentMarkers, citizen, (CitizenSpecialistRules.SpecialistType)specialist);
			this.Owner.World.Towns[cityIdx].LaborerMarkers2 = updated;

			this.Processor.Journal.CloseScope("AssignCitizenType");
		}

		/// <summary>The kind of specialist of the citizen of the shown city</summary>
		public ushort ObtainCitizenType(ushort citizen)
		{
			this.Processor.Journal.OpenScope($"ObtainCitizenType({citizen})");

			short cityIdx = (short)ShownCity;
			ushort markers = this.Owner.World.Towns[cityIdx].LaborerMarkers2;
			var specType = CitizenSpecialistRules.GetSpecialistType(markers, citizen);
			ushort result = (ushort)specType;

			this.Processor.Journal.CloseScope("ObtainCitizenType");
			return result;
		}

		/// <summary>The number of the specialists of the kind in the shown city</summary>
		public ushort TallySpecialists(ushort specialist)
		{
			this.Processor.Journal.OpenScope($"TallySpecialists({specialist})");

			short cityIdx = (short)ShownCity;
			ushort markers = this.Owner.World.Towns[cityIdx].LaborerMarkers2;
			int tally = CitizenSpecialistRules.CountSpecialists(markers, (CitizenSpecialistRules.SpecialistType)specialist);
			this.Processor.Journal.CloseScope("TallySpecialists");
			return (ushort)tally;
		}

		/// <summary>
		/// Keeps the specialists of the city in range: entertainers (0x70e2) and taxmen (0x70e4) are clamped to the city size,
		/// scientists (0x6542) become taxmen while there are fewer taxmen than scientists, then specialists are given up
		/// (scientists first, then entertainers, and a taxman each time) until all of them fit in the size less the workers
		/// (clamped to 0 - 99)
		/// </summary>
		public void ReshuffleCitizens(short townIdx, ushort workerTally)
		{
			this.Processor.Journal.OpenScope($"ReshuffleCitizens({townIdx}, {workerTally})");

			City town = this.Owner.World.Towns[townIdx];

			CityHappy =
				(ushort)this.Owner.Helpers.VerifyValueSpan((short)CityHappy, 0, town.TrueExtent);

			while (CityExcessUnhappy != 0 &&
				(short)CityUnhappy < (short)CityExcessUnhappy)
			{
				CityExcessUnhappy = (ushort)(CityExcessUnhappy - 1);
				CityUnhappy = (ushort)(CityUnhappy + 1);
			}

			ushort taxmen = (ushort)this.Owner.Helpers.VerifyValueSpan((short)CityUnhappy, 0, town.TrueExtent);

			while (true)
			{
				CityUnhappy = taxmen;

				short room = (short)this.Owner.Helpers.VerifyValueSpan(town.TrueExtent - (short)workerTally, 0, 99);

				if ((short)(CityHappy + CityUnhappy) <= room)
					break;

				if (CityExcessUnhappy != 0)
				{
					CityExcessUnhappy = (ushort)(CityExcessUnhappy - 1);
				}
				else
				{
					CityHappy = (ushort)(CityHappy - 1);
					CityHappy =
						(ushort)this.Owner.Helpers.VerifyValueSpan((short)CityHappy, 0, town.TrueExtent);
				}

				CityUnhappy = (ushort)(CityUnhappy - 1);
				taxmen = (ushort)this.Owner.Helpers.VerifyValueSpan((short)CityUnhappy, 0, town.TrueExtent);
			}

			this.Processor.Journal.CloseScope("ReshuffleCitizens");
		}

		/// <summary>
		/// The citizens of the city from (horizLoc, vertLoc): the happy (0x70e2), the content, the unhappy (0x70e4, the first
		/// 0x6542 of them marked) and the specialists specialists (ObtainCitizenType), 7 pixels apart or narrower to fit in breadth, with
		/// gaps between the groups.
		/// </summary>
		/// <returns>The distance of the icons</returns>
		public ushort RenderCitizenIcons(short townIdx, short horizLoc, short vertLoc, ushort specialists, ushort breadth)
		{
			this.Processor.Journal.OpenScope($"RenderCitizenIcons({townIdx}, {horizLoc}, {vertLoc}, {specialists}, {breadth})");

			// No gap after the happy citizens: the original made it on the frame word 0x2, which is never written (0)

			sbyte size = this.Owner.World.Towns[townIdx].TrueExtent;

			ushort step = (short)(7 * size) > (short)breadth ? (ushort)CPU.SignedQuotient((short)breadth, size) : (ushort)7;
			short citizenIdx;

			for (citizenIdx = 0; citizenIdx < (short)CityHappy; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, CitizenPicture(citizenIdx & 1), step);
			}


			for (citizenIdx = 0; (short)(this.Owner.World.Towns[townIdx].TrueExtent - specialists - CityHappy -
				CityUnhappy) > citizenIdx; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, CitizenPicture(2 + (citizenIdx & 1)), step);
			}

			if (citizenIdx != 0)
			{
				horizLoc += 2;
			}

			for (citizenIdx = 0; citizenIdx < (short)CityUnhappy; citizenIdx++)
			{
				this.Owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc,
					CitizenPicture(4 + (citizenIdx & 1)));

				if (citizenIdx < (short)CityExcessUnhappy)
				{
					this.Owner.VideoCard.SubstituteHue(ScreenBox, horizLoc, vertLoc, 12, 14, Hues.Magenta, Hues.LightRed);
				}

				horizLoc += (short)step;
			}

			horizLoc += 4;

			for (citizenIdx = 0; citizenIdx < (short)specialists; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, this.Processor.PullGlobalWord((ushort)((ObtainCitizenType((ushort)citizenIdx) << 1) + 0x6ea0)), step);
			}

			this.Processor.Journal.CloseScope("RenderCitizenIcons");

			return step;
		}

		/// <summary>
		/// Draws an icon of the city screen (from the picture screen, the place by its number) at (horizLocTo, vertLocTo)
		/// </summary>
		public void RenderResourceIcon(short horizLocOrigin, short horizLocTo, short vertLocTo)
		{
			this.Processor.Journal.OpenScope($"RenderResourceIcon({horizLocOrigin}, {horizLocTo}, {vertLocTo})");

			if (horizLocOrigin < 22 || horizLocOrigin > 24)
			{
				horizLocOrigin--;

				if (horizLocOrigin >= 24)
				{
					horizLocOrigin -= 3;
				}

				if (horizLocOrigin >= 40)
				{
					this.Owner.VideoCard.RenderImage(
						PictureScreenBox,
						((horizLocOrigin & 1) * 19) + 161, 100, 18, 10,
						ScreenBox,
						horizLocTo, vertLocTo);
				}
				else
				{
					this.Owner.VideoCard.RenderImage(
						PictureScreenBox,
						((horizLocOrigin / 5) * 19) + 161,
						((horizLocOrigin % 5) * 10) + 50,
						18, 10,
						ScreenBox,
						horizLocTo, vertLocTo);
				}
			}

			this.Processor.Journal.CloseScope("RenderResourceIcon");
		}

		/// <summary>
		/// A box from one corner to the other: the background pattern (FloodBoxWithMotif) and the frame (RenderBox, style 1)
		/// </summary>
		public void RenderFrame(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1)
		{
			this.Processor.Journal.OpenScope($"RenderFrame({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1})");

			int breadth = horizSpot1 - horizLoc;
			int altitude = vertSpot1 - vertLoc;

			FloodBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.Owner.Dialogs.RenderBox(horizLoc, vertLoc, breadth, altitude, Hues.Blue);

			this.Processor.Journal.CloseScope("RenderFrame");
		}

		/// <summary>
		/// Fills the box with the motif of the city screen: on VGA tiles of 16 x 16 from (208, 100) of the picture screen, on
		/// EGA colour 9
		/// </summary>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			this.Processor.Journal.OpenScope($"FloodBoxWithMotif({horizLoc}, {vertLoc}, {breadth}, {altitude})");

			if (VgaMode == 0)
			{
				this.Owner.Services.FloodBox(ScreenBox, horizLoc, vertLoc, breadth, altitude, Hues.LightBlue);
			}
			else
			{
				int boxVertLoc = vertLoc;
				int boxAltitude = altitude;

				while (boxAltitude > 0)
				{
					int tileAltitude = Math.Min(boxAltitude, 16);
					int boxHorizLoc = horizLoc;
					int boxBreadth = breadth;

					while (boxBreadth > 0)
					{
						int tileBreadth = Math.Min(boxBreadth, 16);

						this.Owner.VideoCard.RenderImage(PictureScreenBox,
							208, 100, tileBreadth, tileAltitude,
							ScreenBox,
							boxHorizLoc, boxVertLoc);

						boxHorizLoc += tileBreadth;
						boxBreadth -= tileBreadth;
					}

					boxVertLoc += tileAltitude;
					boxAltitude -= tileAltitude;
				}
			}
		
			this.Processor.Journal.CloseScope("FloodBoxWithMotif");
		}

		/// <summary>
		/// A raised box from (horizLoc, vertLoc) to (horizSpot1, vertSpot1) in the colour, with the text at the address
		/// centered in it
		/// </summary>
		public void RenderLabelBox(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, GameText label, ushort style)
		{
			this.Processor.Journal.OpenScope($"RenderLabelBox({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, 0x{label.Address:x4}, {style})");

			this.Owner.Services.FloodBox(ScreenBox,
				horizLoc, vertLoc, horizSpot1 - horizLoc, vertSpot1 - vertLoc, style);
		
			this.Owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizSpot1, vertLoc, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.Owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizLoc, vertSpot1, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.Owner.VideoCard.RenderLine(ScreenBox,
				horizLoc + 1, vertSpot1, horizSpot1, vertSpot1, (ushort)((style < 8) ? 8 : (style - 8)));

			this.Owner.VideoCard.RenderLine(ScreenBox,
				horizSpot1, vertLoc, horizSpot1, vertSpot1, (ushort)((style < 0x8) ? 8 : (style - 8)));

			this.Owner.TextPainter.RenderCenteredTextToScreen0(label,
				((horizLoc + horizSpot1) / 2) + 1, ((vertLoc + vertSpot1) / 2) - 2, (ushort)(style ^ 8));

			this.Processor.Journal.CloseScope("RenderLabelBox");
		}

		public void RenderMiniMap()
		{
			this.Processor.Journal.OpenScope("RenderMiniMap()");

			this.Owner.Services.FloodBox(ScreenBox, 101, 117, 120, 75, Hues.Black);

			short humanIdx = this.Owner.World.PersonParticipantIdx;

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				for (int column = 0; column < 80; column++)
				{
					int horizLoc = this.Owner.Routing.TunePosition(this.Owner.World.Participants[humanIdx].HorizBegin + column - 40);

					if ((this.Owner.World.ChartSight[(ushort)horizLoc, vertLoc] & (1 << humanIdx)) == 0)
						continue;

					ushort quarter = (ushort)(((vertLoc & 1) << 1) + (column & 1));
					int dotHoriz = (column / 2) * 3 + (sbyte)this.Processor.PullGlobalByte((ushort)(MiniMapColumnShifts + quarter)) + 0x65;
					int dotVert = (vertLoc / 2) * 3 + (sbyte)this.Processor.PullGlobalByte((ushort)(MiniMapRowShifts + quarter)) + 0x76;
					ushort hue = (ushort)(this.Owner.MapOps.ObtainTerrainType((short)horizLoc, vertLoc) == MapRules.OceanTerrain ? 1 : 2);

					this.Owner.Services.FloodBox(ScreenBox, dotHoriz, dotVert, 2, 2, hue);
				}
			}

			this.Owner.Dialogs.RenderBox(100, 117, 121, 75, Hues.LightBlue);

			this.Processor.Journal.CloseScope("RenderMiniMap");
		}

		/// <summary>
		/// A dot of 2 x 2 pixels on the small world map (only when 0x2496 = 2), the world centred on the start column of the
		/// human player
		/// </summary>
		public void RenderMiniMapDot(int horizLoc, int vertLoc, ushort style)
		{
			this.Processor.Journal.OpenScope($"RenderMiniMapDot({horizLoc}, {vertLoc}, {style})");

			if (CityScreenTab == 2)
			{
				horizLoc = this.Owner.Routing.TunePosition(
					horizLoc - this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].HorizBegin + 40);

				this.Owner.Services.FloodBox(ScreenBox,
					((horizLoc * 3) / 2) + 101, ((vertLoc * 3) / 2) + 118, 2, 2, style);
			}

			this.Processor.Journal.CloseScope("RenderMiniMapDot");
		}

		/// <summary>
		/// A wonder has no effect any more when any civilization knows the technology that makes it obsolete
		/// (word table at 0x10fe of the data segment)
		/// </summary>
		private bool IsWonderObsolete(short miracleIdent)
		{
			short obsoletingTechnology = this.Processor.PullInt16(this.Processor.SegData.Value16, (ushort)(0x10fe + (miracleIdent << 1)));

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, obsoletingTechnology) != 0)
					return true;
			}

			return false;
		}

		/// <summary>One citizen icon at the place</summary>
		/// <returns>The place of the next one</returns>
		private short RenderCitizen(short horizLoc, short vertLoc, ushort picture, ushort step)
		{
			this.Owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc, picture);

			return (short)(horizLoc + step);
		}
	}
}
