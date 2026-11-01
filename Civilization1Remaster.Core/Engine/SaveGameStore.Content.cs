using System;
using System.IO;

namespace Civilization1Remaster.Core.Engine
{
	public partial class SaveGameStore
	{
		public bool LoadGameContent(string route)
		{
			route = route.ToUpper();

			using var _ = this.Processor.Journal.Scope($"ImportGameContent('{route}')");

			bool victory = false;
			string docname = Path.GetFileNameWithoutExtension(RuntimeLibrary.ObtainDOSFileLabel(route));

			try
			{
				byte[] scratchAlt;
				VGABitmap? chart = VGABitmap.ViaFile(Path.Combine(this.Processor.StandardFolder, $"{docname}.MAP"), out scratchAlt);

				if (chart == null)
				{
					throw new Exception($"Can't read Map file '{docname}.MAP'");
				}

				if (!this.Owner.VideoCard.Displays.ContainsKey(2))
				{
					throw new InvalidOperationException("This collection doesn't contain key '2'");
				}

				this.Owner.VideoCard.Displays[2] = chart;

				FileStream source = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{docname}.SVE"), FileMode.Open);
				this.Owner.World.RoundTally = source.PullInt16OrZero();
				this.Owner.World.PersonParticipantIdx = source.PullInt16OrZero();
				this.Owner.World.ParticipantMarkers = source.PullInt16OrZero();
				this.Owner.World.ChanceRoot = source.PullUInt16OrZero();
				this.Owner.World.Season = source.PullInt16OrZero();
				this.Owner.World.HardnessTier = source.PullInt16OrZero();
				this.Owner.World.LiveRealms = source.PullInt16OrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].PresentInquiryIdx = source.PullInt16OrZero();
				PullContent(source, Tables.LeaderNameAt(0), 0x70);
				PullContent(source, Tables.CivilizationPluralAt(0), 0x60);
				PullContent(source, Tables.CivilizationNameAt(0), 0x58);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].Gold = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].InquiryHeadway = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].LiveTroops.Length; jdx++)
					{
						this.Owner.World.Participants[idx].LiveTroops[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].TroopsInsideMaking.Length; jdx++)
					{
						this.Owner.World.Participants[idx].TroopsInsideMaking[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].FoundScienceTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 5; jdx++)
					{
						this.Owner.World.Participants[idx].FoundScienceMarkers[jdx] = source.PullUInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].RegimeKind = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].Approach = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						this.Owner.World.Participants[idx].Relations[jdx] = source.PullUInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].TownTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].TroopTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].SoilTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].ColonistTally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].GrandTownExtent = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].MartialMight = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].Standing = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].LevyRatio = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].Tally = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].TouchParticipantTimeleft = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].HorizBegin = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].ChiefVisuals = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].Assault = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].Guard = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						this.Owner.World.Participants[idx].Landmasses[jdx].TownTally = source.PullInt16OrZero();
					}
				}

				PullContent(source, ExploredMapSegment, ExploredMapSize);

				for (int idx = 0; idx < 16; idx++)
				{
					this.Owner.World.Landmasses[idx].ConstructionSpotTally = source.PullInt16OrZero();
				}

				PullContent(source, CivilizationHistorySegment, CivilizationHistoryBaseAddress, CivilizationHistorySize);

				for (int idx = 0; idx < this.Owner.World.TrucePlotContent.Length; idx++)
				{
					this.Owner.World.TrucePlotContent[idx] = source.PullUInt8OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Towns.Length; idx++)
				{
					this.Owner.World.Towns[idx] = City.ReadFrom(source);
				}

				PullContent(source, CityNamesStatusAddress, CityNamesStatusSize);

				byte[] troops = new byte[UnitsBufferSize];
				source.ReadExactly(troops, 0, UnitsBufferSize);
				MemoryStream troopSource = new MemoryStream(troops);

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 128; jdx++)
					{
						this.Owner.World.Participants[idx].Troops[jdx] = Unit.ReadFrom(troopSource);
					}
				}

				troopSource.Position = 0;
				PullContent(troopSource, UnitMemory.Origin, UnitsBufferSize);

				troopSource.Close();

				for (int idx = 0; idx < this.Owner.World.ChartSight.GetLength(0); idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.ChartSight.GetLength(1); jdx++)
					{
						this.Owner.World.ChartSight[idx, jdx] = (sbyte)source.PullUInt8OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Live = (sbyte)source.PullUInt8OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Strategy = source.PullUInt8OrZero();
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Place = new Point((sbyte)source.PullUInt8OrZero(), this.Owner.World.Participants[idx].TacticalSites[jdx].Place.Y);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						this.Owner.World.Participants[idx].TacticalSites[jdx].Place = new Point(this.Owner.World.Participants[idx].TacticalSites[jdx].Place.X, (sbyte)source.PullUInt8OrZero());
					}
				}

				for (int idx = 0; idx < this.Owner.World.ScienceLeadingFoundVia.Length; idx++)
				{
					this.Owner.World.ScienceLeadingFoundVia[idx] = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						this.Owner.World.Participants[idx].TroopsRuined[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.TownLabels.Length; idx++)
				{
					char[] townLabel = new char[13];

					for (int jdx = 0; jdx < 13; jdx++)
					{
						townLabel[jdx] = (char)source.PullUInt8OrZero();
					}

					this.Owner.World.TownLabels[idx] = new string(townLabel);
				}

				this.Owner.World.RerunContentSpan = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.RerunContent.Length; idx++)
				{
					this.Owner.World.RerunContent[idx] = source.PullUInt8OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.MarvelTownIdx.Length; idx++)
				{
					this.Owner.World.MarvelTownIdx[idx] = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].MissingTroops.Length; jdx++)
					{
						this.Owner.World.Participants[idx].MissingTroops[jdx] = source.PullInt16OrZero();
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].ScienceObtainedSince.Length; jdx++)
					{
						this.Owner.World.Participants[idx].ScienceObtainedSince[jdx] = (sbyte)source.PullUInt8OrZero();
					}
				}

				this.Owner.World.DirtyCellTally = source.PullInt16OrZero();
				this.Owner.World.SmogImpactTier = source.PullInt16OrZero();
				this.Owner.World.WorldHeatingTally = source.PullInt16OrZero();
				this.Owner.World.SessionPrefMarkers = source.PullInt16OrZero();
				PullContent(source, ReplayTurnsAddress, ReplayTurnsSize);
				this.Owner.World.HighestScienceTally = source.PullInt16OrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ComingScienceTally = source.PullInt16OrZero();
				this.Owner.World.DiagMarkers = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].ResearchLevyRatio = source.PullInt16OrZero();
				}

				this.Owner.World.FollowingCollectionRound = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].RunningSagaStanding = source.PullInt16OrZero();
				}

				PullContent(source, SpaceshipGridSegment, SpaceshipGridBaseAddress, SpaceshipGridTotalSize);
				this.Owner.World.StarshipMarkers = source.PullInt16OrZero();
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].StarshipVictoryRatio = source.PullInt16OrZero();
				this.Owner.World.BotStarshipVictoryRatio = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].StarshipArrivalSeason = source.PullInt16OrZero();
				}

				PullContent(source, PalaceBuilder.PalaceSectionLevelsAddress, PalaceSectionRecordSize);
				PullContent(source, PalaceBuilder.PalaceSectionStylesAddress, PalaceSectionRecordSize);

				for (int idx = 0; idx < this.Owner.World.TownPlaces.Length; idx++)
				{
					this.Owner.World.TownPlaces[idx].X = (sbyte)source.PullUInt8OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.TownPlaces.Length; idx++)
				{
					this.Owner.World.TownPlaces[idx].Y = (sbyte)source.PullUInt8OrZero();
				}

				this.Owner.World.CastleTier = source.PullInt16OrZero();
				this.Owner.World.TruceRoundTally = source.PullInt16OrZero();
				this.Owner.World.BotRivalTally = source.PullInt16OrZero();

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].StarshipInhabitants = source.PullInt16OrZero();
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					this.Owner.World.Participants[idx].StarshipLiftoffSeason = source.PullInt16OrZero();
				}

				this.Owner.World.ParticipantIdentMarkers = source.PullInt16OrZero();

				source.Close();

				string vitalityRoute = Path.Combine(this.Processor.StandardFolder, $"{docname}.HP");
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
			string docname = Path.GetFileNameWithoutExtension(RuntimeLibrary.ObtainDOSFileLabel(route));

			try
			{
				this.Owner.VideoCard.Displays[2].Store(Path.Combine(this.Processor.StandardFolder, $"{docname}.MAP"), false);

				FileStream sink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{docname}.SVE"), FileMode.Create);
				sink.EmitInt16(this.Owner.World.RoundTally);
				sink.EmitInt16(this.Owner.World.PersonParticipantIdx);
				sink.EmitInt16(this.Owner.World.ParticipantMarkers);
				sink.EmitUInt16(this.Owner.World.ChanceRoot);
				sink.EmitInt16(this.Owner.World.Season);
				sink.EmitInt16(this.Owner.World.HardnessTier);
				sink.EmitInt16(this.Owner.World.LiveRealms);
				sink.EmitInt16(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].PresentInquiryIdx);
				EmitContent(sink, Tables.LeaderNameAt(0), 0x70);
				EmitContent(sink, Tables.CivilizationPluralAt(0), 0x60);
				EmitContent(sink, Tables.CivilizationNameAt(0), 0x58);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].Gold);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].InquiryHeadway);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].LiveTroops.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].LiveTroops[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].TroopsInsideMaking.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].TroopsInsideMaking[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].FoundScienceTally);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 5; jdx++)
					{
						sink.EmitUInt16(this.Owner.World.Participants[idx].FoundScienceMarkers[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].RegimeKind);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].Landmasses.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].Approach);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						sink.EmitUInt16(this.Owner.World.Participants[idx].Relations[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].TownTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].TroopTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].SoilTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].ColonistTally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].GrandTownExtent);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].MartialMight);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].Standing);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].LevyRatio);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].Tally);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].TouchParticipantTimeleft);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].HorizBegin);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].ChiefVisuals);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].Assault);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].Guard);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].Landmasses[jdx].TownTally);
					}
				}

				EmitContent(sink, ExploredMapSegment, ExploredMapSize);

				for (int idx = 0; idx < 16; idx++)
				{
					sink.EmitInt16(this.Owner.World.Landmasses[idx].ConstructionSpotTally);
				}

				EmitContent(sink, CivilizationHistorySegment, CivilizationHistoryBaseAddress, CivilizationHistorySize);

				for (int idx = 0; idx < this.Owner.World.TrucePlotContent.Length; idx++)
				{
					sink.WriteByte(this.Owner.World.TrucePlotContent[idx]);
				}

				for (int idx = 0; idx < this.Owner.World.Towns.Length; idx++)
				{
					this.Owner.World.Towns[idx].WriteTo(sink);
				}

				EmitContent(sink, CityNamesStatusAddress, CityNamesStatusSize);

				EmitContent(sink, UnitMemory.Origin, UnitsBufferSize);

				for (int idx = 0; idx < this.Owner.World.ChartSight.GetLength(0); idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.ChartSight.GetLength(1); jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.ChartSight[idx, jdx]);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[idx].TacticalSites[jdx].Live);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte(this.Owner.World.Participants[idx].TacticalSites[jdx].Strategy);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[idx].TacticalSites[jdx].Place.X);
					}
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 16; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.Participants[idx].TacticalSites[jdx].Place.Y);
					}
				}

				for (int idx = 0; idx < this.Owner.World.ScienceLeadingFoundVia.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.ScienceLeadingFoundVia[idx]);
				}

				for (int idx = 0; idx < 8; idx++)
				{
					for (int jdx = 0; jdx < 8; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].TroopsRuined[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.TownLabels.Length; idx++)
				{
					for (int jdx = 0; jdx < 13; jdx++)
					{
						sink.WriteByte((byte)this.Owner.World.TownLabels[idx][jdx]);
					}
				}

				sink.EmitInt16(this.Owner.World.RerunContentSpan);
				for (int idx = 0; idx < this.Owner.World.RerunContent.Length; idx++)
				{
					sink.WriteByte(this.Owner.World.RerunContent[idx]);
				}

				for (int idx = 0; idx < this.Owner.World.MarvelTownIdx.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.MarvelTownIdx[idx]);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].MissingTroops.Length; jdx++)
					{
						sink.EmitInt16(this.Owner.World.Participants[idx].MissingTroops[jdx]);
					}
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					for (int jdx = 0; jdx < this.Owner.World.Participants[idx].ScienceObtainedSince.Length; jdx++)
					{
						sink.WriteByte((byte)((sbyte)this.Owner.World.Participants[idx].ScienceObtainedSince[jdx]));
					}
				}

				sink.EmitInt16(this.Owner.World.DirtyCellTally);
				sink.EmitInt16(this.Owner.World.SmogImpactTier);
				sink.EmitInt16(this.Owner.World.WorldHeatingTally);
				sink.EmitInt16(this.Owner.World.SessionPrefMarkers);
				EmitContent(sink, ReplayTurnsAddress, ReplayTurnsSize);
				sink.EmitInt16(this.Owner.World.HighestScienceTally);
				sink.EmitInt16(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ComingScienceTally);
				sink.EmitInt16(this.Owner.World.DiagMarkers);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].ResearchLevyRatio);
				}

				sink.EmitInt16(this.Owner.World.FollowingCollectionRound);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].RunningSagaStanding);
				}

				EmitContent(sink, SpaceshipGridSegment, SpaceshipGridBaseAddress, SpaceshipGridTotalSize);
				sink.EmitInt16(this.Owner.World.StarshipMarkers);
				sink.EmitInt16(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].StarshipVictoryRatio);
				sink.EmitInt16(this.Owner.World.BotStarshipVictoryRatio);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].StarshipArrivalSeason);
				}

				EmitContent(sink, PalaceBuilder.PalaceSectionLevelsAddress, PalaceSectionRecordSize);
				EmitContent(sink, PalaceBuilder.PalaceSectionStylesAddress, PalaceSectionRecordSize);

				for (int idx = 0; idx < 256; idx++)
				{
					sink.WriteByte((byte)((sbyte)this.Owner.World.TownPlaces[idx].X));
				}

				for (int idx = 0; idx < 256; idx++)
				{
					sink.WriteByte((byte)((sbyte)this.Owner.World.TownPlaces[idx].Y));
				}

				sink.EmitInt16(this.Owner.World.CastleTier);
				sink.EmitInt16(this.Owner.World.TruceRoundTally);
				sink.EmitInt16(this.Owner.World.BotRivalTally);

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].StarshipInhabitants);
				}

				for (int idx = 0; idx < this.Owner.World.Participants.Length; idx++)
				{
					sink.EmitInt16(this.Owner.World.Participants[idx].StarshipLiftoffSeason);
				}

				sink.EmitInt16(this.Owner.World.ParticipantIdentMarkers);

				sink.Close();

				using (FileStream vitalitySink = new FileStream(Path.Combine(this.Processor.StandardFolder, $"{docname}.HP"), FileMode.Create))
				{
					this.Owner.World.Vitals.Store(vitalitySink);
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

		private void PullContent(Stream source, ushort stagingAddr, ushort span)
		{
			ReadIntoMemory(source, CPU.AsLinearAddress(this.Processor.SegData.Value16, stagingAddr), span);
		}

		private void PullContent(Stream source, ushort stagingSect, ushort stagingAddr, ushort span)
		{
			ReadIntoMemory(source, CPU.AsLinearAddress(stagingSect, stagingAddr), span);
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

		private void EmitContent(Stream sink, ushort stagingAddr, ushort span)
		{
			WriteFromMemory(sink, CPU.AsLinearAddress(this.Processor.SegData.Value16, stagingAddr), span);
		}

		private void EmitContent(Stream sink, ushort stagingSect, ushort stagingAddr, ushort span)
		{
			WriteFromMemory(sink, CPU.AsLinearAddress(stagingSect, stagingAddr), span);
		}
	}
}
