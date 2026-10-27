using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 20.
	/// </summary>
	public class CityFoundingAndScore : GameModule
	{
		public CityFoundingAndScore(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Changes the terrain under a new city to what irrigation makes of it, when irrigation changes the terrain type.
		/// The irrigation result of a terrain type (12 bytes per type, at 0x44a) is -1 when it can't be irrigated,
		/// -2 when it is irrigated without change, other values are the new terrain (as the settlers code uses it).
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		public void WipeCitySquare(int horizLoc, int vertLoc)
		{
			ushort landKind = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			short freshLand = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(landKind * 12 + 0x44a));

			if (freshLand >= 0)
			{
				// The same steps as when settlers finish changing the terrain
				this.owner.Services.AssignDot(2, horizLoc, vertLoc, (ushort)freshLand);
				this.owner.World.ChartSight[horizLoc, vertLoc] |= 1;
				this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
			}
		}

		/// <summary>
		/// Founds a city of the player at the place in the first free city slot (none: -1, the human player is told). Its name
		/// is the next free one of the leader's 16 names (then of the shared names 0xe0 - 0xff, then any); the human player may
		/// rename it (cancelled: -1, the name stays taken). The tile loses its improvements and gets city and road (and
		/// irrigation where the terrain allows); production settlers (militia 4 with technology 0x22, 5 with 0x40); the first
		/// city gets the palace; coastal (2, ocean of more than 256 tiles or for the human player) and river/hills (8) flags;
		/// its 21 tiles marked in the owner's colour on the power map (screen 2) and the outer ones made at most colour 8; a
		/// human neighbour learns of an AI city next to his city or with wonder 0x13. Returns the city. SI gets the values of
		/// the original before the calls (dialogs leave it in DI).
		/// </summary>
		public ushort EstablishCity(short participantIdx, ushort arg2, int horizLoc, int vertLoc, ushort townExtent)
		{
			this.processor.Journal.OpenScope($"EstablishCity({participantIdx}, {arg2}, {horizLoc}, {vertLoc}, {townExtent})");

			// The final values of the locals stay in the stack frame, like in the original

			this.owner.Bot.DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 4);

			ushort townIdx;

			for (townIdx = 0; townIdx < 0x80; townIdx++)
			{

				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff)
					break;
			}

			ushort result;

			if (townIdx == 0x80)
			{

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					DialogStyle = 0x4;
					this.owner.Words.Clear();

					this.owner.BlockText.DisplayTextDialog(0x4f74);

					this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 64, 80);
				}

				result = 0xffff;
			}
			else
			{
				result = FoundCity(participantIdx, (short)townIdx, horizLoc, vertLoc, townExtent);
			}

			this.processor.RegAcc.Value16 = result;

			// Far return
			this.processor.Journal.CloseScope("EstablishCity");

			return result;
		}

		/// <summary>The city in the free slot (see EstablishCity); -1 when the human player cancels its name</summary>
		private ushort FoundCity(short participantIdx, short townIdx, int horizLoc, int vertLoc, ushort townExtent)
		{
			short person = this.owner.World.PersonParticipantIdx;
			City town = this.owner.World.Towns[townIdx];

			ushort labelIdx = ChooseCityLabel(participantIdx);

			town.LabelIdx = (byte)labelIdx;

			this.owner.World.TownPlaces[labelIdx] = new Point(horizLoc, vertLoc);

			if (this.owner.World.IsHuman(participantIdx))
			{
				if (this.owner.CityNaming.DisplayCityLabelPrompt(townIdx) == 0)
					return 0xffff;

				char[] label = this.owner.World.TownLabels[town.LabelIdx].ToCharArray();
				label[12] = '\0';
				this.owner.World.TownLabels[town.LabelIdx] = new string(label);
			}

			TrimCityLabel(town.LabelIdx);

			this.owner.UnitOps.AttachReplayContent(1, (byte)participantIdx, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			this.owner.MapOps.DiscardTileImprovements(0x24, horizLoc, vertLoc);

			// Remaster: the land under a new city is cleared. Forest, jungle and swamp change the same way
			// as when settlers irrigate them (forest to plains, jungle and swamp to grassland).
			WipeCitySquare(horizLoc, vertLoc);

			this.owner.MapOps.AssignTileImprovements(9, horizLoc, vertLoc);

			if ((short)this.processor.PullGlobalWord((ushort)((ushort)(this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) * 0xc) + 0x44a)) < -1)
			{
				this.owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);
			}

			ushort townOffset = (ushort)(0x1c * townIdx);
			this.processor.RegSource.Value16 = townOffset;

			town.ConditionMarker = 0;
			town.Place.X = horizLoc;
			town.Place.Y = vertLoc;
			town.TrueExtent = (sbyte)townExtent;
			town.ShownExtent = 0;
			town.LaborerMarkers0 = 0;
			town.LaborerMarkers1 = 0;
			town.RootCommerce = 0;
			town.GrainTally = 0;
			town.MaterialsTally = 0;
			town.StructureMarkers0 = 0;
			town.StructureMarkers1 = 0;
			town.PresentMakingIdx = 1;

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x22) != 0)
				town.PresentMakingIdx = 4;

			if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x40) != 0)
				town.PresentMakingIdx = 5;

			this.processor.RegSource.Value16 = townOffset;

			town.Unfamiliar[0] = -1;
			town.Unfamiliar[1] = -1;

			bool first = true;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				if (this.owner.World.Towns[otherIdx].ParticipantIdx == participantIdx && otherIdx != (ushort)townIdx)
					first = false;
			}


			if (first)
			{
				town.StructureMarkers0 = 1;
				town.StructureMarkers1 = 0;
			}

			this.processor.RegSource.Value16 = townOffset;

			town.RootCommerce = 0;
			town.ParticipantIdx = participantIdx;

			for (int routeIdx = 0; routeIdx < 3; routeIdx++)
				town.CommerceTownIndices[routeIdx] = -1;

			town.LaborerMarkers2 = 0;

			MarkCityNeighbourhood(participantIdx, town, horizLoc, vertLoc);

			if (first && participantIdx == person)
			{
				this.owner.HelpSystem.DisplayHelpPopup(0x4f66);
			}

			MarkCityPowerArea(participantIdx, town, horizLoc, vertLoc);

			if (participantIdx == person)
			{
				// Sound of chopping wood while the new city is built
				this.owner.Services.PerformCityFoundedSound();

				if ((this.owner.World.SessionPrefMarkers & 8) != 0)
				{
					if (this.owner.Startup.QueryForDisk(1, 0) != 0)
						this.owner.Diplomats.ExamineCity(townIdx);
				}

				this.owner.Advisors.RenewCityScreen(townIdx);

				this.owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				this.processor.RegSource.Value16 = townOffset;

				town.PresentMakingIdx = (sbyte)this.owner.Advisors.PickCityProduction(participantIdx, townIdx);

				if (this.owner.CityOps.PlayerEnjoysWonderEffect(person, 0x13) != 0)
				{
					this.owner.World.ChartSight[town.Place.X, town.Place.Y] |= (sbyte)(byte)(1 << (byte)person);

					town.ShownExtent = 1;

					this.owner.MapOps.KeepKnownTileImprovements(town.Place.X, town.Place.Y);
				}
			}

			return (ushort)townIdx;
		}

		/// <summary>
		/// The next free name of the leader's 16 (16 * (ChiefVisuals - 1), one less from the 8th leader on), then the shared
		/// names 0xe0 - 0xff, then from 0 on; all taken: name 0
		/// </summary>
		private ushort ChooseCityLabel(short participantIdx)
		{
			ushort labelIdx = (ushort)(this.owner.World.Participants[participantIdx].ChiefVisuals - 1);

			if ((short)labelIdx >= 7)
				labelIdx--;

			labelIdx <<= 4;

			ushort first = labelIdx;
			bool found;

			do
			{
				found = true;

				if (this.owner.World.TownPlaces[labelIdx].X == -1)
					break;

				found = false;

				ushort end = (ushort)(first + 0x10);
				labelIdx++;

				if (end == labelIdx)
					labelIdx = 0xe0;

				if (labelIdx == 0x100)
				{
					labelIdx = 0;

					if (first == 0x3e7)
						found = true;

					first = 0x3e7;
				}
			}
			while (!found);


			return labelIdx;
		}

		/// <summary>The trailing spaces of the name (from its 12th letter back) become ends of text</summary>
		private void TrimCityLabel(byte labelIdx)
		{
			ushort letterIdx = 0xb;

			while (letterIdx > 0 && this.owner.World.TownLabels[labelIdx][letterIdx] == ' ')
			{
				char[] label = this.owner.World.TownLabels[labelIdx].ToCharArray();
				label[letterIdx] = '\0';
				this.owner.World.TownLabels[labelIdx] = new string(label);
				letterIdx--;
			}

		}

		/// <summary>The flags of the city's neighbours: 2 coastal (big ocean, or any for the human player), 8 river or hills</summary>
		private void MarkCityNeighbourhood(short participantIdx, City town, int horizLoc, int vertLoc)
		{
			ushort neighbourIdx;

			for (neighbourIdx = 1; neighbourIdx <= 8; neighbourIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(neighbourIdx << 1);

				int tileHoriz = (short)DirectionHoriz(neighbourIdx) + horizLoc;
				int tileVert = (short)DirectionVert(neighbourIdx) + vertLoc;

				ushort terrain = this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);

				if (terrain == 0xa &&
					((short)this.owner.MapOps.ObtainLandMassSize(tileHoriz, tileVert) > 0x100 || participantIdx == this.owner.World.PersonParticipantIdx))
				{
					town.ConditionMarker |= 2;
				}

				if (terrain == 0xb || terrain == 5)
				{
					town.ConditionMarker |= 8;
				}
			}

		}

		/// <summary>
		/// The power map (screen 2, 0x50 to the right): the 21 tiles of the city in the owner's colour, the next ones at most
		/// colour 8; an AI city next to a city of the human player becomes known to him
		/// </summary>
		private void MarkCityPowerArea(short participantIdx, City town, int horizLoc, int vertLoc)
		{
			short person = this.owner.World.PersonParticipantIdx;
			ushort tileIdx;

			for (tileIdx = 0; tileIdx < 0x2d; tileIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(tileIdx << 1);

				short tileHoriz = (short)(ushort)this.owner.Routing.TunePosition((short)DirectionHoriz(tileIdx) + horizLoc);
				short tileVert = (short)(DirectionVert(tileIdx) + (ushort)vertLoc);


				if (tileIdx <= 0x14)
				{
					this.owner.Services.AssignDot(2, tileHoriz + 0x50, tileVert, (ushort)participantIdx);
				}
				else
				{
					ushort colour = this.owner.VideoCard.ObtainDot(2, tileHoriz + 0x50, tileVert);

					if ((short)colour >= 8)
						this.owner.Services.AssignDot(2, tileHoriz + 0x50, tileVert, 8);
				}

				if (participantIdx == person || tileIdx > 0x18)
					continue;

				if ((this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert) & 1) == 0 ||
					this.owner.MapOps.ObtainTileOwnerBits(tileHoriz, tileVert) != (ushort)person)
					continue;

				town.ShownExtent = 1;

				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			}

		}

		/// <summary>
		/// A random event in the city (one in ten each): earthquake near hills (a building), plague without aqueduct (0x100;
		/// technology 10 protects), flood near a river (0x800 terrain, without city walls 0x80), fire near mountains (0x20,
		/// without temple 8), famine (without granary 4), pirates/vandals (a building, without aqueduct), volcano near
		/// mountains (0x400, without barracks 2), and three in ten a revolt when the unhappy outnumber the happy: a demand,
		/// food and production lost, and a city of another player with more prosperity (happy - unhappy * 32 / distance) may
		/// buy it. A human player sees the message (the first line for a city he only sees). SI and DI get the values of the
		/// original before the calls (HandleCity reads below the stack).
		/// </summary>
		public void HandleCityDisasters(short townIdx)
		{
			this.processor.Journal.OpenScope($"HandleCityDisasters({townIdx})");

			// The final values of the locals stay in the stack frame, like in the original

			City town = this.owner.World.Towns[townIdx];
			ushort townOffset = (ushort)(0x1c * townIdx);

			// The terrains of the city's tile and its 8 neighbours
			ushort terrains = 0;
			ushort neighbourIdx;

			this.processor.RegDest.Value16 = townOffset;

			for (neighbourIdx = 0; neighbourIdx <= 8; neighbourIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(neighbourIdx << 1);

				ushort terrain = this.owner.MapOps.ObtainTerrainType(
					town.Place.X + (short)DirectionHoriz(neighbourIdx),
					town.Place.Y + (short)DirectionVert(neighbourIdx));

				terrains |= (ushort)(1 << (byte)terrain);
			}


			EmptyMessage()
				.City(townIdx);

			this.owner.Libc.CopyText(CodeText4, TextBuffer.Address);

			this.owner.Words.Clear();

			switch (this.owner.Libc.Dice.DrawInt(10))
			{
				case 0:
					if ((terrains & 0x10) != 0 && (town.StructureMarkers0 & 0xfffe) != 0)
					{
						ushort buildingIdx = DestroyRandomBuilding(town);

						Message("Earthquake in ")
							.City(townIdx)
							.Text("!\n")
							.Text(BuildingText(buildingIdx))
							.Text(" destroyed.\n");
					}
					break;

				case 1:
					this.processor.RegSource.Value16 = townOffset;

					if (this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 10) == 0 && !town.Has(CityStructure.Aqueduct) &&
						this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(9, BuildingTable.Technology)) != 0)
					{
						this.owner.BlockText.DisplayTextDialog(0x4f99);

						if (town.TrueExtent != 0)
							town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
					}
					break;

				case 2:
					if ((terrains & 0x800) != 0)
					{
						this.processor.RegSource.Value16 = townOffset;

						if (!town.Has(CityStructure.CityWalls) && this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(8, BuildingTable.Technology)) != 0)
						{
							this.owner.BlockText.DisplayTextDialog(0x4f9d);

							if (town.TrueExtent > 1)
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputePlaguePopulationLoss(town.TrueExtent);
						}
					}
					break;

				case 3:
					if ((terrains & 0x20) != 0)
					{
						this.processor.RegSource.Value16 = townOffset;

						if (!town.Has(CityStructure.Temple) && this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(4, BuildingTable.Technology)) != 0)
						{
							this.owner.BlockText.DisplayTextDialog(0x4fa1);

							if (town.TrueExtent > 1)
								town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);
						}
					}
					break;

				case 4:
					this.processor.RegSource.Value16 = townOffset;

					if (town.TrueExtent != 0 && !town.Has(CityStructure.Granary) &&
						this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(3, BuildingTable.Technology)) != 0)
					{
						this.owner.BlockText.DisplayTextDialog(0x4fa5);

						town.GrainTally = 0;
						town.TrueExtent -= (sbyte)CityDisasterRules.ComputeFaminePopulationLoss(town.TrueExtent);

						this.owner.Words.Append("Citizens demand GRANARY.\n");
					}
					break;

				case 5:
					this.processor.RegSource.Value16 = townOffset;

					if ((town.StructureMarkers0 & 0xfffe) != 0 && !town.Has(CityStructure.Aqueduct) &&
						this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, (short)BuildingWord(9, BuildingTable.Technology)) != 0)
					{
						ushort buildingIdx = DestroyRandomBuilding(town);

						this.owner.Libc.CopyText(CodeText1, BuildingText(buildingIdx));

						this.owner.BlockText.DisplayTextDialog(0x4fc3);
					}
					break;

				case 6:
					if ((terrains & 0x400) != 0)
					{
						this.processor.RegSource.Value16 = townOffset;

						if (!town.Has(CityStructure.Barracks))
						{
							this.owner.BlockText.DisplayTextDialog(0x4fc7);

							town.GrainTally = 0;
							town.MaterialsTally = 0;
						}
					}
					break;

				default:
					HandleCityRevolt(townIdx);
					break;
			}

			ReportCityDisaster(townIdx);

			// Far return
			this.processor.Journal.CloseScope("HandleCityDisasters");
		}

		/// <summary>
		/// A random building of the city (1 - 23, read like the original: a 16-bit mask sign extended) is destroyed; its number
		/// (frame 0x10)
		/// </summary>
		private ushort DestroyRandomBuilding(City town)
		{
			ushort buildingIdx;

			do
			{
				buildingIdx = (ushort)this.owner.Libc.Dice.DrawInt(24);
			}
			while (buildingIdx == 0 || !town.HasStructureSignExtended(buildingIdx));

			ushort keep = (ushort)~(ushort)(1 << (byte)buildingIdx);

			town.StructureMarkers0 &= keep;
			town.StructureMarkers1 &= (keep & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return buildingIdx;
		}

		/// <summary>
		/// The revolt (three times in ten): when the unhappy outnumber the happy the citizens demand a building (a human player
		/// sees it), food and production are lost and, unless it is a capital of a civilization with more than 3 cities, the
		/// city of another player with the most prosperity (from the first cities up to the first destroyed one or this city)
		/// may buy it (ComputeInciteCost)
		/// </summary>
		private void HandleCityRevolt(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			ushort townOffset = (ushort)(0x1c * townIdx);

			this.owner.CityOps.HandleCity(townIdx, -1);

			this.owner.Words.Clear();

			if ((short)CityHappy >= (short)CityUnhappy)
				return;

			switch (this.owner.Libc.Dice.DrawInt(3))
			{
				case 0:
					this.owner.Words.Set(0x4fcb);
					break;

				case 1:
					this.owner.Words.Set(0x4fd7);
					break;

				case 2:
					this.owner.Words.Set(0x4fe6);
					break;
			}

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Words.Append("!\nCitizens demand ");

			if (!town.Has(CityStructure.Temple))
			{
				this.owner.Words.Append("TEMPLE\n");
			}
			else if (!town.Has(CityStructure.Courthouse))
			{
				this.owner.Words.Append(0x500d);
			}
			else if (!town.Has(CityStructure.Marketplace))
			{
				this.owner.Words.Append(0x5019);
			}
			else
			{
				this.processor.RegSource.Value16 = townOffset;

				this.owner.Words.Append(this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x1d) != 0 &&
					!town.Has(CityStructure.Cathedral) ? (ushort)0x5026 : (ushort)0x5031);
			}

			this.processor.RegSource.Value16 = townOffset;

			if (this.owner.World.IsHuman(town.ParticipantIdx))
			{
				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				this.owner.Words.Clear();
			}

			this.processor.RegSource.Value16 = townOffset;

			town.GrainTally = 0;
			town.MaterialsTally = 0;

			short highest = 4;
			short rivalIdx = -1;


			if ((short)CityHappy > (short)CityUnhappy ||
				town.Has(CityStructure.Palace) || this.owner.World.Participants[town.ParticipantIdx].TownTally <= 3)
				return;

			ushort otherIdx;

			for (otherIdx = 0; otherIdx < 0x80 && otherIdx != (ushort)townIdx && this.owner.World.Towns[otherIdx].ConditionMarker != 0xff; otherIdx++)
			{
				this.processor.RegSource.Value16 = townOffset;
				this.processor.RegDest.Value16 = (ushort)(0x1c * otherIdx);

				short distance = (short)(ushort)this.owner.Helpers.ObtainDistance(town.Place.X, this.owner.World.Towns[otherIdx].Place.X, town.Place.Y, this.owner.World.Towns[otherIdx].Place.Y);

				this.owner.CityOps.HandleCity((short)otherIdx, -1);

				short prosperity = CPU.SignedQuotient((short)((ushort)(CityHappy - CityUnhappy) << 5), distance);

				if (prosperity > highest)
				{
					highest = prosperity;
					rivalIdx = (short)otherIdx;

				}
			}


			if (rivalIdx == -1)
				return;

			this.processor.RegSource.Value16 = townOffset;

			if (this.owner.World.Towns[rivalIdx].ParticipantIdx == town.ParticipantIdx)
				return;

			if ((this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.owner.World.PersonParticipantIdx)) != 0)
			{
				Message("Residents of ")
					.City(townIdx)
					.Text(" admire\nthe prosperity of ")
					.City(rivalIdx)
					.Text(".\n")
					.Dialog(80, 80);
			}

			this.owner.Diplomats.ComputeInciteCost(townIdx, this.owner.World.Towns[rivalIdx].ParticipantIdx);

			this.owner.Words.Clear();
		}

		/// <summary>
		/// The message left in TextBuffer.Address: a human player's city shows it; a city the human player only sees shows its first line
		/// and the map moves there
		/// </summary>
		private void ReportCityDisaster(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			if (this.owner.Words[0] == 0)
				return;

			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			if (this.owner.World.IsHuman(town.ParticipantIdx))
			{
				DialogStyle = 0x4;

				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.owner.TurnLogic.RepaintTileIfVisible(town.Place.X, town.Place.Y);

				return;
			}

			if ((this.owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << (byte)this.owner.World.PersonParticipantIdx)) == 0)
				return;

			ushort lineEnd = 0;

			while (this.processor.PullGlobalByte((ushort)(lineEnd++ + TextBuffer.Address)) != 0xa)
			{
			}

			this.processor.EmitGlobalByte((ushort)(lineEnd + TextBuffer.Address), 0);
			DialogStyle = 0x2;

			this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 64);

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, town.Place.X - 10, town.Place.Y - 6);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="flag"></param>
		/// <returns></returns>
		public ushort DisplayFinalScore(short participantIdx, bool marker)
		{
			this.processor.Journal.OpenScope($"DisplayFinalScore({participantIdx}, {marker})");

			// The original keeps SI for its caller and returns the score in AX (TurnEvents compares them for the palace)
			ushort savedSource = this.processor.RegSource.Value16;

			if (marker)
			{
				ushort screenBufferPtr = ScreenBox;
				DrawingBox.DisplayIdx = 1;
				this.owner.Briefings.DisplayReportPreamble(0x506b, 3);
			}

			this.owner.World.Participants[0].Tally = 0;

			if (marker && (this.owner.World.StarshipMarkers & 0x100) != 0)
			{
				this.owner.TextPainter.RenderCenteredTextToScreen0(0x507e, 160, 100, 15);
				ushort screenBufferPtr = ScreenBox;
				DrawingBox.DisplayIdx = 0;

				this.owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 0x140, 0xc8,
					screenBufferPtr,
					0, 0);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();
				this.owner.Startup.DisplayMouseCursor();
				this.owner.TurnHooks.RepaintMainScreen();

				this.processor.RegSource.Value16 = savedSource;
				this.processor.RegAcc.Value16 = 0;
				this.processor.Journal.CloseScope("DisplayFinalScore");
				return 0;
			}

			int citizensY = 0x1a;
			int wondersY = 0x1a;
			ScoreIconHoriz = 0x3e7;
			ScoreIconVert = 0x1a;

			short savedLevyRatio = this.owner.World.Participants[participantIdx].LevyRatio;
			this.owner.World.Participants[participantIdx].LevyRatio = (short)(6 - this.owner.World.Participants[participantIdx].ResearchLevyRatio);

			int totalCitizenScore = 0;
			for (short townIdx = 0; townIdx < 128; townIdx++)
			{
				if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff)
					continue;

				if (this.owner.World.Towns[townIdx].ParticipantIdx != participantIdx)
					continue;

				this.owner.CityOps.HandleCity(townIdx, -1);

				int happyCount = CityHappy;
				int unhappyCount = CityUnhappy;
				int specialistCount = CitySpecialists;
				int contentCount = this.owner.World.Towns[townIdx].TrueExtent - happyCount - unhappyCount - specialistCount;

				if (marker)
				{
					for (int i = 0; i < happyCount; i++)
					{
						RenderScoreIcon((ushort)(i & 1));
					}
					for (int i = 0; i < contentCount; i++)
					{
						RenderScoreIcon((ushort)((i & 1) + 2));
					}
					for (int i = 0; i < unhappyCount; i++)
					{
						RenderScoreIcon((ushort)((i & 1) + 4));
					}
					for (int i = 0; i < specialistCount; i++)
					{
						RenderScoreIcon((ushort)(this.owner.CityOps.ObtainCitizenType((ushort)i) + 5));
					}
				}

				totalCitizenScore += ScoreRules.ComputeCitizenScore(happyCount, contentCount, specialistCount);
			}

			this.owner.World.Participants[participantIdx].LevyRatio = savedLevyRatio;
			CitizenScore = (ushort)totalCitizenScore;

			ushort civNamePtr = CivilizationName(participantIdx);
			Message(civNamePtr)
				.Text(" Citizens (")
				.Number((short)totalCitizenScore)
				.Text(")");

			if (marker)
			{
				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, 16, (short)citizensY, 15);
			}

			wondersY = ScoreIconVert + 0x10;
			int wonderTitleY = wondersY;
			int wonderColX = 0x3e7;
			int totalWonderScore = 0;

			for (short wonderIdx = 1; wonderIdx <= 21; wonderIdx++)
			{
				short wonderCityIdx = this.owner.World.MarvelTownIdx[wonderIdx];
				if (wonderCityIdx != -1 && wonderCityIdx != 0x80)
				{
					if (this.owner.World.Towns[wonderCityIdx].ParticipantIdx == participantIdx)
					{
						wonderColX += 0x98;
						if (wonderColX > 0x12c)
						{
							wondersY += (ushort)this.owner.Helpers.VerifyValueSpan(ScoreVerticalShift + 2, 8, 99);
							wonderColX = 0x10;
						}

						if (marker && wondersY < 0xb4)
						{
							this.owner.CityOps.RenderResourceIcon((short)(wonderIdx + 0x18), (short)wonderColX, (short)(wondersY - 2));
							this.owner.TextPainter.RenderTextToScreen0(WonderText(wonderIdx), (short)(wonderColX + 20), (short)wondersY, 11);
						}

						totalWonderScore += 20;
					}
				}
			}

			WonderScore = (ushort)totalWonderScore;
			Message(civNamePtr)
				.Text(" Achievements (")
				.Number((short)totalWonderScore)
				.Text(")");

			if (marker)
			{
				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, 16, (short)wonderTitleY, 15);
			}

			this.owner.World.Participants[0].Tally = (short)(totalWonderScore + totalCitizenScore);
			wondersY += 10;

			short humanPlayer = this.owner.World.PersonParticipantIdx;
			int starshipInhabitants = this.owner.World.Participants[humanPlayer].StarshipInhabitants;
			int victoryRatio = this.owner.World.Participants[humanPlayer].StarshipVictoryRatio;

			if (starshipInhabitants > 0 && victoryRatio > 0)
			{
				int spaceshipBonus = ScoreRules.ComputeSpaceshipScore(victoryRatio, starshipInhabitants);

				Message("SpaceShip: (")
					.Number((short)spaceshipBonus);

				if (this.owner.World.Participants[humanPlayer].StarshipArrivalSeason == this.owner.World.Season)
				{
					this.owner.World.Participants[0].Tally += (short)spaceshipBonus;
				}
				else
				{
					this.owner.Words.Append(" possible");
				}

				this.owner.Words.Append(")");

				if (marker && wondersY < 0xb8)
				{
					this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, 16, (short)wondersY, 15);
				}

				wondersY += 10;
			}

			this.owner.Words.Clear();

			if (this.owner.World.DirtyCellTally > 0)
			{
				int penalty = ScoreRules.ComputePollutionPenalty(this.owner.World.DirtyCellTally);
				Message("Pollution: (")
					.Number((short)(-penalty))
					.Text(")   ");
				this.owner.World.Participants[0].Tally -= (short)penalty;
			}

			if (this.owner.World.Season > 0 && this.owner.World.TruceRoundTally > 0)
			{
				int peaceBonus = ScoreRules.ComputePeaceBonus(this.owner.World.TruceRoundTally);
				this.owner.Words.Append("Peace: (+");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)peaceBonus, 10));
				this.owner.Words.Append(") ");
				this.owner.World.Participants[0].Tally += (short)peaceBonus;
			}

			if (this.owner.World.Participants[humanPlayer].ComingScienceTally > 0)
			{
				int futureTechBonus = ScoreRules.ComputeFutureTechScore(this.owner.World.Participants[humanPlayer].ComingScienceTally);
				this.owner.Words.Append("Future Tech: (+");
				this.owner.Words.Append(this.owner.Libc.IntegerToText((short)futureTechBonus, 10));
				this.owner.Words.Append(")");
				this.owner.World.Participants[0].Tally += (short)futureTechBonus;
			}

			if (this.owner.Words[0] != 0 && marker && wondersY < 0xb8)
			{
				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, 16, (short)wondersY, 15);
				wondersY += 10;
			}

			int conquestBonus = -1;
			if (ConquestEnd != 0)
			{
				conquestBonus = ScoreRules.ComputeConquestBonus(this.owner.World.BotRivalTally, this.owner.World.RoundTally);
			}

			if (this.owner.World.Participants[0].Tally < 0)
			{
				this.owner.World.Participants[0].Tally = 0;
			}

			if (conquestBonus != -1 && conquestBonus > this.owner.World.Participants[0].Tally)
			{
				Message("Bonus Score: ")
					.Number((short)conquestBonus);
				this.owner.World.Participants[0].Tally = (short)conquestBonus;
			}
			else
			{
				Message("Total Score: ")
					.Number(this.owner.World.Participants[0].Tally);
			}

			if (marker && wondersY < 0xba)
			{
				this.owner.TextPainter.RenderTextWithShadow(TextBuffer.Address, 16, (short)wondersY, 15);
			}

			HallOfFameScore = (ushort)this.owner.World.Participants[0].Tally;

			if (marker)
			{
				ushort screenBufferPtr = ScreenBox;
				this.owner.Services.FloodBox(screenBufferPtr, 35, 192, 250, 7, 8);

				if (this.owner.World.Participants[0].Tally > 0)
				{
					this.owner.Services.FloodBox(screenBufferPtr, 35, 192, (short)this.owner.Helpers.VerifyValueSpan(this.owner.World.Participants[0].Tally / 4, 0, 250), 7, 11);
				}

				DrawingBox.DisplayIdx = 0;

				this.owner.VideoCard.RenderImage(
					BackScreenBox,
					0, 0, 0x140, 0xc8,
					screenBufferPtr,
					0, 0);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();
				this.owner.Startup.DisplayMouseCursor();
				this.owner.TurnHooks.RepaintMainScreen();
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[0].Tally;
			this.processor.Journal.CloseScope("DisplayFinalScore");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The next icon of the score screen: 8 pixels right of the last one (0xb25c); after x 292 a new row (0xb2bc grows by
		/// 0x5116) starting at x 16 or 20. Nothing is drawn below row 160.
		/// </summary>
		/// <param name="arg1">Picture of the icon, from the table at 0x6e96</param>
		public void RenderScoreIcon(ushort arg1)
		{
			this.processor.Journal.OpenScope($"RenderScoreIcon({arg1})");

			short iconLeft = (short)(ScoreIconHoriz + 8);
			ScoreIconHoriz = (ushort)iconLeft;

			if (iconLeft > 0x124)
			{
				short iconTop = (short)(ScoreIconVert + (ushort)ScoreVerticalShift);
				ScoreIconVert = (ushort)iconTop;
				ScoreIconHoriz = (ushort)(((iconTop >> 1) & 0x4) + 0x10);
			}

			if ((short)ScoreIconVert <= 0xa0)
			{
				this.owner.Services.RenderPictureToScreen(ScreenBox,
					(short)ScoreIconHoriz,
					(short)ScoreIconVert,
					this.processor.PullGlobalWord((ushort)((arg1 << 1) + 0x6e96)));
			}

			this.processor.Journal.CloseScope("RenderScoreIcon");
		}
	}
}
