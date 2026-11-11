using System;
using System.IO;

namespace Civ_1_5_Remastered.Core.Engine.Session
{
	public partial class SaveGameStore
	{
		public bool LoadGameContent(string route)
		{
			route = route.ToUpper();

			using var _ = this.Processor.Journal.Scope($"ImportGameContent('{route}')");

			bool victory = false;
			string documentName = Path.GetFileNameWithoutExtension(RuntimeLibrary.ObtainOperatingSystemFileLabel(route));

			try
			{
				byte[] scratchAlternate;
				VideoGraphicsBitmap? chart = VideoGraphicsBitmap.ViaFile(Path.Combine(this.Processor.StandardFolder, $"{documentName}.MAP"), out scratchAlternate);

				if (chart == null)
				{
					throw new Exception($"Can't read Map file '{documentName}.MAP'");
				}

				if (!this.Owner.VideoCard.Displays.ContainsKey(2))
				{
					throw new InvalidOperationException("This collection doesn't contain key '2'");
				}

				this.Owner.VideoCard.Displays[2] = chart;

				FileStream source = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{documentName}.SVE"), FileMode.Open);
				this.Owner.World.RoundTally = source.PullSignedWordOrZero();
				this.Owner.World.PersonParticipantIndex = source.PullSignedWordOrZero();
				this.Owner.World.ParticipantMarkers = source.PullSignedWordOrZero();
				this.Owner.World.ChanceRoot = source.PullWordOrZero();
				this.Owner.World.Season = source.PullSignedWordOrZero();
				this.Owner.World.HardnessTier = source.PullSignedWordOrZero();
				this.Owner.World.LiveRealms = source.PullSignedWordOrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].PresentInquiryIndex = source.PullSignedWordOrZero();
				PullContent(source, Tables.LeaderNameAt(0), 0x70);
				PullContent(source, Tables.CivilizationPluralAt(0), 0x60);
				PullContent(source, Tables.CivilizationNameAt(0), 0x58);

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].Gold = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].InquiryHeadway = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].LiveTroops.Length; innerIndex++)
					{
						this.Owner.World.Participants[index].LiveTroops[innerIndex] = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].TroopsInsideMaking.Length; innerIndex++)
					{
						this.Owner.World.Participants[index].TroopsInsideMaking[innerIndex] = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].FoundScienceTally = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 5; innerIndex++)
					{
						this.Owner.World.Participants[index].FoundScienceMarkers[innerIndex] = source.PullWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].RegimeKind = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].Landmasses.Length; innerIndex++)
					{
						this.Owner.World.Participants[index].Landmasses[innerIndex].Approach = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 8; innerIndex++)
					{
						this.Owner.World.Participants[index].Relations[innerIndex] = source.PullWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].TownTally = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].TroopTally = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].SoilTally = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].ColonistTally = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].GrandTownExtent = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].MartialMight = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].Standing = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].LevyRatio = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].Tally = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].TouchParticipantTimeLeft = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].HorizontalBegin = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].ChiefVisuals = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						this.Owner.World.Participants[index].Landmasses[innerIndex].Assault = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						this.Owner.World.Participants[index].Landmasses[innerIndex].Guard = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].Landmasses.Length; innerIndex++)
					{
						this.Owner.World.Participants[index].Landmasses[innerIndex].TownTally = source.PullSignedWordOrZero();
					}
				}

				PullContent(source, ExploredMapSegment, ExploredMapSize);

				for (int index = 0; index < 16; index++)
				{
					this.Owner.World.Landmasses[index].ConstructionSpotTally = source.PullSignedWordOrZero();
				}

				PullContent(source, CivilizationHistorySegment, CivilizationHistoryBaseAddress, CivilizationHistorySize);

				for (int index = 0; index < this.Owner.World.TrucePlotContent.Length; index++)
				{
					this.Owner.World.TrucePlotContent[index] = source.PullByteOrZero();
				}

				for (int index = 0; index < this.Owner.World.Towns.Length; index++)
				{
					this.Owner.World.Towns[index] = City.ReadFrom(source);
				}

				PullContent(source, CityNamesStatusAddress, CityNamesStatusSize);
				this.originalFrigatePrice = Civ_1_5_Remastered.Core.Shared.Tables.RemasterUnitTable.SavedPrice(
					Civ_1_5_Remastered.Core.Shared.Tables.RemasterUnitTable.Apply(this.Owner.Tables));

				byte[] troops = new byte[UnitsBufferSize];
				source.ReadExactly(troops, 0, UnitsBufferSize);
				MemoryStream troopSource = new MemoryStream(troops);

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 128; innerIndex++)
					{
						this.Owner.World.Participants[index].Troops[innerIndex] = Unit.ReadFrom(troopSource);
					}
				}

				troopSource.Position = 0;
				PullContent(troopSource, UnitMemory.Origin, UnitsBufferSize);

				troopSource.Close();

				for (int index = 0; index < this.Owner.World.ChartSight.GetLength(0); index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.ChartSight.GetLength(1); innerIndex++)
					{
						this.Owner.World.ChartSight[index, innerIndex] = (sbyte)source.PullByteOrZero();
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						this.Owner.World.Participants[index].TacticalSites[innerIndex].Live = (sbyte)source.PullByteOrZero();
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						this.Owner.World.Participants[index].TacticalSites[innerIndex].Strategy = source.PullByteOrZero();
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						this.Owner.World.Participants[index].TacticalSites[innerIndex].Place = new Point((sbyte)source.PullByteOrZero(), this.Owner.World.Participants[index].TacticalSites[innerIndex].Place.Y);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						this.Owner.World.Participants[index].TacticalSites[innerIndex].Place = new Point(this.Owner.World.Participants[index].TacticalSites[innerIndex].Place.X, (sbyte)source.PullByteOrZero());
					}
				}

				for (int index = 0; index < this.Owner.World.ScienceLeadingFoundVia.Length; index++)
				{
					this.Owner.World.ScienceLeadingFoundVia[index] = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 8; innerIndex++)
					{
						this.Owner.World.Participants[index].TroopsRuined[innerIndex] = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.TownLabels.Length; index++)
				{
					char[] townLabel = new char[13];

					for (int innerIndex = 0; innerIndex < 13; innerIndex++)
					{
						townLabel[innerIndex] = (char)source.PullByteOrZero();
					}

					this.Owner.World.TownLabels[index] = new string(townLabel);
				}

				this.Owner.World.RerunContentSpan = source.PullSignedWordOrZero();

				for (int index = 0; index < this.Owner.World.RerunContent.Length; index++)
				{
					this.Owner.World.RerunContent[index] = source.PullByteOrZero();
				}

				for (int index = 0; index < this.Owner.World.MarvelTownIndex.Length; index++)
				{
					this.Owner.World.MarvelTownIndex[index] = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].MissingTroops.Length; innerIndex++)
					{
						this.Owner.World.Participants[index].MissingTroops[innerIndex] = source.PullSignedWordOrZero();
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].ScienceObtainedSince.Length; innerIndex++)
					{
						this.Owner.World.Participants[index].ScienceObtainedSince[innerIndex] = (sbyte)source.PullByteOrZero();
					}
				}

				this.Owner.World.DirtyCellTally = source.PullSignedWordOrZero();
				this.Owner.World.SmogImpactTier = source.PullSignedWordOrZero();
				this.Owner.World.WorldHeatingTally = source.PullSignedWordOrZero();
				this.Owner.World.SessionPreferenceMarkers = source.PullSignedWordOrZero();
				PullContent(source, ReplayTurnsAddress, ReplayTurnsSize);
				this.Owner.World.HighestScienceTally = source.PullSignedWordOrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ComingScienceTally = source.PullSignedWordOrZero();
				this.Owner.World.DiagonalMarkers = source.PullSignedWordOrZero();

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].ResearchLevyRatio = source.PullSignedWordOrZero();
				}

				this.Owner.World.FollowingCollectionRound = source.PullSignedWordOrZero();

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].RunningSagaStanding = source.PullSignedWordOrZero();
				}

				PullContent(source, SpaceshipGridSegment, SpaceshipGridBaseAddress, SpaceshipGridTotalSize);
				this.Owner.World.StarshipMarkers = source.PullSignedWordOrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].StarshipVictoryRatio = source.PullSignedWordOrZero();
				this.Owner.World.ComputerStarshipVictoryRatio = source.PullSignedWordOrZero();

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].StarshipArrivalSeason = source.PullSignedWordOrZero();
				}

				PullContent(source, IPalaceBuilder.PalaceSectionLevelsAddress, PalaceSectionRecordSize);
				PullContent(source, IPalaceBuilder.PalaceSectionStylesAddress, PalaceSectionRecordSize);

				for (int index = 0; index < this.Owner.World.TownPlaces.Length; index++)
				{
					this.Owner.World.TownPlaces[index].X = (sbyte)source.PullByteOrZero();
				}

				for (int index = 0; index < this.Owner.World.TownPlaces.Length; index++)
				{
					this.Owner.World.TownPlaces[index].Y = (sbyte)source.PullByteOrZero();
				}

				this.Owner.World.CastleTier = source.PullSignedWordOrZero();
				this.Owner.World.TruceRoundTally = source.PullSignedWordOrZero();
				this.Owner.World.ComputerRivalTally = source.PullSignedWordOrZero();

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].StarshipInhabitants = source.PullSignedWordOrZero();
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					this.Owner.World.Participants[index].StarshipLiftoffSeason = source.PullSignedWordOrZero();
				}

				this.Owner.World.ParticipantIdentifierMarkers = source.PullSignedWordOrZero();

				source.Close();

				string vitalityRoute = Path.Combine(this.Processor.StandardFolder, $"{documentName}.HP");
				bool vitalityFetched = false;

				if (File.Exists(vitalityRoute))
				{
					using (FileStream vitalitySource = new FileStream(vitalityRoute, FileMode.Open, FileAccess.Read))
					{
						vitalityFetched = this.Owner.World.Vitals.Import(vitalitySource);
					}
				}

				if (!vitalityFetched)
				{
					this.Owner.World.Vitals.WipeAll();
				}

				string extrasRoute = Path.Combine(this.Processor.StandardFolder, $"{documentName}.EXT");
				bool extrasFetched = false;

				if (File.Exists(extrasRoute))
				{
					using (FileStream extrasSource = new FileStream(extrasRoute, FileMode.Open, FileAccess.Read))
					{
						extrasFetched = this.Owner.World.Extras.Import(extrasSource);
					}
				}

				if (!extrasFetched)
				{
					this.Owner.World.Extras.WipeAll();
				}

				// A game on Europe saved before the world was kept with it: recognised by its map.
				if (this.Owner.World.Extras.World == 0 && LooksLikeEurope())
				{
					this.Owner.World.Extras.World = (byte)EuropeMap.Marker;
				}

				// Every slot in the colours of the civilization playing it (Poland and Japan light red, in any slot),
				// with its units and cities cut again; nothing stays from a game played before.
				this.Owner.NewGameWizard.ApplyCivilizationColours();

				// The leader portraits of Poland (16) and Japan (17) lie past the original table of 16 leaders: a new game
				// sets them, a loaded game must as well, or the portrait index is whatever follows the table.
				this.Processor.EmitGlobalByte(0x3938 + LeaderPortraits.PolishLeader, LeaderPortraits.Casimir);
				this.Processor.EmitGlobalByte(0x3938 + LeaderPortraits.JapaneseLeader, LeaderPortraits.Tokugawa);

				victory = true;
			}
			catch (Exception extra)
			{
				Message(extra.Message)
					.Dialog(100, 80);

				victory = false;
			}

			return victory;
		}

		public bool SaveGameContent(string route)
		{
			route = route.ToUpper();

			using var _ = this.Processor.Journal.Scope($"StoreGameContent('{route}')");

			bool victory = false;
			string documentName = Path.GetFileNameWithoutExtension(RuntimeLibrary.ObtainOperatingSystemFileLabel(route));

			try
			{
				this.Owner.VideoCard.Displays[2].Store(Path.Combine(this.Processor.StandardFolder, $"{documentName}.MAP"), false);

				FileStream sink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{documentName}.SVE"), FileMode.Create);
				sink.EmitSignedWord(this.Owner.World.RoundTally);
				sink.EmitSignedWord(this.Owner.World.PersonParticipantIndex);
				sink.EmitSignedWord(this.Owner.World.ParticipantMarkers);
				sink.EmitWord(this.Owner.World.ChanceRoot);
				sink.EmitSignedWord(this.Owner.World.Season);
				sink.EmitSignedWord(this.Owner.World.HardnessTier);
				sink.EmitSignedWord(this.Owner.World.LiveRealms);
				sink.EmitSignedWord(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].PresentInquiryIndex);
				EmitContent(sink, Tables.LeaderNameAt(0), 0x70);
				EmitContent(sink, Tables.CivilizationPluralAt(0), 0x60);
				EmitContent(sink, Tables.CivilizationNameAt(0), 0x58);

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].Gold);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].InquiryHeadway);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].LiveTroops.Length; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].LiveTroops[innerIndex]);
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].TroopsInsideMaking.Length; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].TroopsInsideMaking[innerIndex]);
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].FoundScienceTally);
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 5; innerIndex++)
					{
						sink.EmitWord(this.Owner.World.Participants[index].FoundScienceMarkers[innerIndex]);
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].RegimeKind);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].Landmasses.Length; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].Landmasses[innerIndex].Approach);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 8; innerIndex++)
					{
						sink.EmitWord(this.Owner.World.Participants[index].Relations[innerIndex]);
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].TownTally);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].TroopTally);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].SoilTally);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].ColonistTally);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].GrandTownExtent);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].MartialMight);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].Standing);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].LevyRatio);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].Tally);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].TouchParticipantTimeLeft);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].HorizontalBegin);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].ChiefVisuals);
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].Landmasses[innerIndex].Assault);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].Landmasses[innerIndex].Guard);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].Landmasses[innerIndex].TownTally);
					}
				}

				EmitContent(sink, ExploredMapSegment, ExploredMapSize);

				for (int index = 0; index < 16; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Landmasses[index].ConstructionSpotTally);
				}

				EmitContent(sink, CivilizationHistorySegment, CivilizationHistoryBaseAddress, CivilizationHistorySize);

				for (int index = 0; index < this.Owner.World.TrucePlotContent.Length; index++)
				{
					sink.WriteByte(this.Owner.World.TrucePlotContent[index]);
				}

				for (int index = 0; index < this.Owner.World.Towns.Length; index++)
				{
					this.Owner.World.Towns[index].WriteTo(sink);
				}

				// The saved unit table keeps the original values (see RemasterUnitTable).
				Civ_1_5_Remastered.Core.Shared.Tables.RemasterUnitTable.SetFrigatePrice(this.Owner.Tables, this.originalFrigatePrice);
				EmitContent(sink, CityNamesStatusAddress, CityNamesStatusSize);
				Civ_1_5_Remastered.Core.Shared.Tables.RemasterUnitTable.Apply(this.Owner.Tables);

				EmitContent(sink, UnitMemory.Origin, UnitsBufferSize);

				for (int index = 0; index < this.Owner.World.ChartSight.GetLength(0); index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.ChartSight.GetLength(1); innerIndex++)
					{
						sink.WriteByte((byte)this.Owner.World.ChartSight[index, innerIndex]);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[index].TacticalSites[innerIndex].Live);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.WriteByte(this.Owner.World.Participants[index].TacticalSites[innerIndex].Strategy);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[index].TacticalSites[innerIndex].Place.X);
					}
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 16; innerIndex++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[index].TacticalSites[innerIndex].Place.Y);
					}
				}

				for (int index = 0; index < this.Owner.World.ScienceLeadingFoundVia.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.ScienceLeadingFoundVia[index]);
				}

				for (int index = 0; index < 8; index++)
				{
					for (int innerIndex = 0; innerIndex < 8; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].TroopsRuined[innerIndex]);
					}
				}

				for (int index = 0; index < this.Owner.World.TownLabels.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < 13; innerIndex++)
					{
						sink.WriteByte((byte)this.Owner.World.TownLabels[index][innerIndex]);
					}
				}

				sink.EmitSignedWord(this.Owner.World.RerunContentSpan);
				for (int index = 0; index < this.Owner.World.RerunContent.Length; index++)
				{
					sink.WriteByte(this.Owner.World.RerunContent[index]);
				}

				for (int index = 0; index < this.Owner.World.MarvelTownIndex.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.MarvelTownIndex[index]);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].MissingTroops.Length; innerIndex++)
					{
						sink.EmitSignedWord(this.Owner.World.Participants[index].MissingTroops[innerIndex]);
					}
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					for (int innerIndex = 0; innerIndex < this.Owner.World.Participants[index].ScienceObtainedSince.Length; innerIndex++)
					{
						sink.WriteByte((byte)((sbyte)this.Owner.World.Participants[index].ScienceObtainedSince[innerIndex]));
					}
				}

				sink.EmitSignedWord(this.Owner.World.DirtyCellTally);
				sink.EmitSignedWord(this.Owner.World.SmogImpactTier);
				sink.EmitSignedWord(this.Owner.World.WorldHeatingTally);
				sink.EmitSignedWord(this.Owner.World.SessionPreferenceMarkers);
				EmitContent(sink, ReplayTurnsAddress, ReplayTurnsSize);
				sink.EmitSignedWord(this.Owner.World.HighestScienceTally);
				sink.EmitSignedWord(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ComingScienceTally);
				sink.EmitSignedWord(this.Owner.World.DiagonalMarkers);

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].ResearchLevyRatio);
				}

				sink.EmitSignedWord(this.Owner.World.FollowingCollectionRound);

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].RunningSagaStanding);
				}

				EmitContent(sink, SpaceshipGridSegment, SpaceshipGridBaseAddress, SpaceshipGridTotalSize);
				sink.EmitSignedWord(this.Owner.World.StarshipMarkers);
				sink.EmitSignedWord(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].StarshipVictoryRatio);
				sink.EmitSignedWord(this.Owner.World.ComputerStarshipVictoryRatio);

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].StarshipArrivalSeason);
				}

				EmitContent(sink, IPalaceBuilder.PalaceSectionLevelsAddress, PalaceSectionRecordSize);
				EmitContent(sink, IPalaceBuilder.PalaceSectionStylesAddress, PalaceSectionRecordSize);

				for (int index = 0; index < 256; index++)
				{
					sink.WriteByte((byte)((sbyte)this.Owner.World.TownPlaces[index].X));
				}

				for (int index = 0; index < 256; index++)
				{
					sink.WriteByte((byte)((sbyte)this.Owner.World.TownPlaces[index].Y));
				}

				sink.EmitSignedWord(this.Owner.World.CastleTier);
				sink.EmitSignedWord(this.Owner.World.TruceRoundTally);
				sink.EmitSignedWord(this.Owner.World.ComputerRivalTally);

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].StarshipInhabitants);
				}

				for (int index = 0; index < this.Owner.World.Participants.Length; index++)
				{
					sink.EmitSignedWord(this.Owner.World.Participants[index].StarshipLiftoffSeason);
				}

				sink.EmitSignedWord(this.Owner.World.ParticipantIdentifierMarkers);

				sink.Close();

				using (FileStream vitalitySink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{documentName}.HP"), FileMode.Create))
				{
					this.Owner.World.Vitals.Store(vitalitySink);
				}

				using (FileStream extrasSink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{documentName}.EXT"), FileMode.Create))
				{
					this.Owner.World.Extras.Store(extrasSink);
				}

				victory = true;
			}
			catch (Exception extra)
			{
				this.Owner.Words.Set(extra.Message);
				this.Owner.Dialogs.ShowMenu(TextBuffer.Address, 4, 64, 1);

				victory = false;
			}

			return victory;
		}

		private void PullContent(Stream source, ushort stagingAddress, ushort span)
		{
			ReadIntoMemory(source, CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, stagingAddress), span);
		}

		private void PullContent(Stream source, ushort stagingSection, ushort stagingAddress, ushort span)
		{
			ReadIntoMemory(source, CentralProcessor.AsLinearAddress(stagingSection, stagingAddress), span);
		}

		private void ReadIntoMemory(Stream source, uint location, ushort span)
		{
			byte[] content = new byte[span];
			source.ReadExactly(content, 0, span);
			this.Processor.Storage.EmitChunk(location, content, 0, span);
		}

		private void WriteFromMemory(Stream sink, uint location, ushort span)
		{
			byte[] content = new byte[span];
			this.Processor.Storage.ReadChunk(location, content, 0, span);
			sink.Write(content, 0, span);
		}

		private void EmitContent(Stream sink, ushort stagingAddress, ushort span)
		{
			WriteFromMemory(sink, CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, stagingAddress), span);
		}

		private void EmitContent(Stream sink, ushort stagingSection, ushort stagingAddress, ushort span)
		{
			WriteFromMemory(sink, CentralProcessor.AsLinearAddress(stagingSection, stagingAddress), span);
		}

		/// <summary>Whether the land and sea of the loaded map are those of EUROPE.PIC (85% of the tiles alike).</summary>
		private bool LooksLikeEurope()
		{
			string route = Path.Combine(AppContext.BaseDirectory, "Assets", "Pictures", "EUROPE.PIC");

			if (!File.Exists(route))
			{
				return false;
			}

			using VideoGraphicsBitmap? map = VideoGraphicsBitmap.ViaFile(route, out _);

			if (map == null)
			{
				return false;
			}

			const byte oceanColour = 1;
			int alike = 0;

			for (int row = 0; row < 50; row++)
			{
				for (int column = 0; column < 80; column++)
				{
					bool sea = this.Owner.MapOperations.ObtainTerrainType(column, row) == 10;

					if (sea == (map.ObtainDot(column, row) == oceanColour))
					{
						alike++;
					}
				}
			}

			// Other worlds share at most about 56% of their land and sea with Europe.
			return alike >= 80 * 50 * 85 / 100;
		}
	}
}
