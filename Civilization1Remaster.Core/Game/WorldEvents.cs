using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1238.
	/// </summary>
	public partial class WorldEvents : GameComponent, IWorldEvents
	{
		private const ushort TravellersReportGunpowderAddress = 0x1d26;
		private const ushort TravellersReportIronWorkingAddress = 0x1d3f;
		private const ushort TravellersReportAncientAddress = 0x1d53;
		private const ushort MenuBarGameLabelAddress = 0x1d6f;
		private const ushort MenuBarOrdersLabelAddress = 0x1d75;
		private const ushort MenuBarAdvisorsLabelAddress = 0x1d7d;
		private const ushort MenuBarWorldLabelAddress = 0x1d87;
		private const ushort MenuBarCivilopediaLabelAddress = 0x1d8e;
		private const ushort EraBcStringAddress = 0x1df0;
		private const ushort EraAdStringAddress = 0x1df4;
		private const ushort PalaceSectionSpritesTableAddress = 0xd21c;
		private const ushort ReplayHistoryScoresSegment = 0x3772;

		private static readonly GameText BankruptcyWarningDialogAddress = new(0x1c2a, "*TAXES");
		private static readonly GameText RetirementContinuePromptAddress = new(0x1cf0, "*FAME");
		private static readonly GameText EndGameTitlePictureAddress = new(0x1cf8, "adscreen.pic");
		private static readonly GameText EndGameTitlePaletteAddress = new(0x1d05, "adscreen.pic");
		private static readonly GameText AdvisorPaletteResourceAddress = new(0x1e56, "sp256.pal");

		public WorldEvents(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void MendFortifiedUnits()
		{
			UnitMemory troops = new UnitMemory(this.Processor);

			for (int participantIdx = 0; participantIdx < UnitHealth.ParticipantTally; participantIdx++)
			{
				List<Point> ownTowns = new List<Point>();

				foreach (City town in this.Owner.World.Towns)
				{
					if (town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx)
						ownTowns.Add(town.Place);
				}

				for (int troopIdx = 0; troopIdx < UnitHealth.TroopTally; troopIdx++)
				{
					int troopKind = troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if (troopKind == 0xff || this.Owner.World.Vitals.ObtainLostHitPoints(participantIdx, troopIdx, troopKind) == 0)
						continue;

					int horizLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
					int vertLoc = troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
					int status = troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
					int landGroup = troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);
					bool inOwnTown = ownTowns.Contains(new Point(horizLoc, vertLoc));
					bool resting;
					bool nextToLand = false;
					int townDistance = int.MaxValue;

					if (landGroup == UnitKindTable.SeaUnits)
					{
						int fullSteps = 3 * troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally);

						resting = (status & (int)UnitStatus.Sentry) != 0 ||
							troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= fullSteps;

						if (!inOwnTown)
						{
							townDistance = HealingRules.ComputeDistanceToNearestFriendlyCity(horizLoc, vertLoc, ownTowns);

							if (townDistance <= HealingRules.MaxShipCoastDistanceToCity)
							{
								nextToLand = HealingRules.IsAdjacentToLand(horizLoc, vertLoc,
									(tileHoriz, tileVert) => this.Owner.MapOps.Chart.Terrain(tileHoriz, tileVert) == MapRules.OceanTerrain);
							}
						}
					}
					else
					{
						resting = (status & (int)UnitStatus.Fortified) != 0;
					}

					int amount = HealingRules.CalculateHealAmount(landGroup, inOwnTown, resting, nextToLand, townDistance);

					this.Owner.World.Vitals.MendAmount(participantIdx, troopIdx, troopKind, amount);
				}
			}
		}

		/// <inheritdoc/>
		public ushort ShowDialog(ushort charsAddr, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 320, 200,
				Screen.BackScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			ushort answer = this.Owner.Dialogs.ShowMenu(charsAddr, horizLoc, vertLoc, 1);

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 320, 200,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			return answer;
		}

		/// <inheritdoc/>
		public void HandleGameEnd()
		{
			using var _ = this.Processor.Journal.Scope("HandleGameEnd()");

			if (PlayTurnOfPlayers(out short startGold))
			{
				FinishTurn(startGold);
			}
		}

		/// <inheritdoc/>
		public void ShowEndGameSequence()
		{
			using var _ = this.Processor.Journal.Scope("DisplayEndGameSequence()");

			this.Owner.Atlas.ShowCivilizationGraph();

			if (Turn.GameEndRequest != 2)
			{
				this.Owner.Replayer.ShowReplay();
			}

			this.Owner.Startup.HideMouseCursor();

			if ((this.Owner.World.StarshipMarkers & 0x100) == 0)
			{
				this.Owner.Legends.ShowGameResultsScreen((ushort)((this.Owner.World.CastleTier >> 1) - 1),
					this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIdx, true));

				this.Owner.Legends.LoadHallOfFame();

				this.Owner.Legends.AttachHallOfFameRecord();

				this.Owner.Legends.SaveHallOfFame();
			}
		
			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public void ShowTitleScreen()
		{
			using var _ = this.Processor.Journal.Scope("DisplayTitleScreen()");

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.AskForDisk(1, 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, EndGameTitlePictureAddress, 0);

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, EndGameTitlePaletteAddress, 1);

			this.Owner.VideoCard.RenderImage(
				Screen.BackScreenBox,
				0, 0, 0x140, 0xc8,
				Screen.Box,
				0, 0);

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();
		}

		/// <inheritdoc/>
		public void ShowTravellersReport()
		{
			using var _ = this.Processor.Journal.Scope("DisplayTravellersReport()");

			if ((ushort)((ushort)(5 - (ushort)this.Owner.World.HardnessTier) * 0x1e) <= this.Owner.World.RoundTally)
			{
				if ((((byte)(this.Owner.World.RoundTally & 0xff) + 1) & 7) == 0 && (this.Owner.World.ScienceLeadingFoundVia[58] & 7) == 0)
				{
					LandBarbariansFromSea();
				}

				RaidHumanCity();
			}
		}

		/// <inheritdoc/>
		public void RefreshPeaceGraphContent()
		{
			using var _ = this.Processor.Journal.Scope("RefreshPeaceGraphContent()");

			// The powers of the frame of the original (B3: in C#): when no power is above -1, the ranking takes the player of the
			// rank before (at first 0, the local 0x1a of the original read before it is written)

			ushort[] powers = new ushort[8];
			short strongest = 0;
			ushort treaties = 0;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				Player participant = this.Owner.World.Participants[participantIdx];
				ushort power = (ushort)(DivideByPowerOfTwo(participant.Gold, 5) + (participant.GrandTownExtent << 3) + participant.FoundScienceTally);

				for (int kind = 0; kind < 0x1c; kind++)
				{
					power += (ushort)(UnitKindWord((sbyte)kind, UnitKindTable.Price) * participant.LiveTroops[kind]);
				}

				if (((1 << participantIdx) & this.Owner.World.LiveRealms) == 0)
				{
					power = 0;
				}

				powers[participantIdx] = power;

				if (participantIdx != this.Owner.World.PersonParticipantIdx && strongest < (short)power)
				{
					strongest = (short)power;
				}

				if (this.Owner.World.RoundTally < 600)
				{
					// The power graph is in the segment 0x3772 (ES is left there, like in the original)
					this.Processor.SegExtra.Value16 = ReplayHistoryScoresSegment;
					this.Processor.EmitUInt8(ReplayHistoryScoresSegment, GraphPlace(participantIdx), (byte)this.Owner.Helpers.ClampValue((short)power / 8, 0, 255));
				}

				treaties = 0;

				for (int otherIdx = 0; otherIdx < 8; otherIdx++)
				{
					if ((participant.Relations[otherIdx] & 3) == 1)
					{
						treaties |= (ushort)(1 << otherIdx);
					}
				}

				if (this.Owner.World.RoundTally < 600)
				{
					this.Owner.World.TrucePlotContent[GraphPlace(participantIdx)] = (byte)treaties;
				}
			}

			ushort best = 0;

			for (short rank = 1; rank < 8; rank++)
			{
				strongest = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short power = (short)powers[participantIdx];

					if (strongest < power)
					{
						best = (ushort)participantIdx;
						strongest = power;
					}
				}

				this.Owner.World.Participants[best].Standing = (short)(8 - rank);
				powers[best] = 0xffff;
			}

			Turn.LateGameNudge = 0x0;

			Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

			if (human.Standing == 7 && human.TownTally > 4 && human.LiveTroops[25] == 0 && this.Owner.World.RoundTally > 200)
			{
				Turn.LateGameNudge = 0x1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					if (participantIdx == this.Owner.World.PersonParticipantIdx || this.Owner.World.Participants[participantIdx].TownTally <= 1)
						continue;

					if ((this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] & 2) == 0)
					{
						this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 1;
					}
					else
					{
						this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 0x100;
					}
				}
			}
		}

		/// <inheritdoc/>
		public void ClearStatusLine()
		{
			using var _ = this.Processor.Journal.Scope("WipeStatusLine()");

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 8, Hues.Black);

			this.Owner.TextPainter.DrawTextToScreen0(MenuBarGameLabelAddress, 8, 1, Hues.White);

			this.Owner.TextPainter.DrawTextToScreen0(MenuBarOrdersLabelAddress, 64, 1, Hues.White);

			this.Owner.TextPainter.DrawTextToScreen0(MenuBarAdvisorsLabelAddress, 128, 1, Hues.White);

			this.Owner.TextPainter.DrawTextToScreen0(MenuBarWorldLabelAddress, 192, 1, Hues.White);

			this.Owner.TextPainter.DrawTextToScreen0(MenuBarCivilopediaLabelAddress, 240, 1, Hues.White);
		}

		/// <inheritdoc/>
		public void DrawStatusBar()
		{
			using var _ = this.Processor.Journal.Scope("RenderStatusBar()");

			if (MapView.WorldMapView != 1)
			{
				Player human = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

				FillBoxWithShadow(0, 58, 80, 39);

				this.Owner.Services.FloodBox(Screen.Box, 3, 60, 74, 11, Hues.LightCyan);

				if (this.Owner.World.TruceRoundTally > 1)
				{
					int peace = this.Owner.Helpers.ClampValue(this.Owner.World.TruceRoundTally * 2, 0, 60);

					this.Owner.Services.FloodBox(Screen.Box, 3, 60, (short)peace, 2, Hues.White);
				}

				DrawPalace(this.Owner.World.PersonParticipantIdx, this.Owner.World.CastleTier, 8, 58);

				if (MapView.Revealed != 0)
				{
					short storage = (short)this.Owner.VideoCard.ObtainFreeStorage();

					EmptyMessage()
						.Number(storage);
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 2, 59, Hues.Black);
				}

				this.Owner.Words.Clear();
				short population = (short)this.Owner.Helpers.ComputePlayerPopulation(this.Owner.World.PersonParticipantIdx);
				this.Owner.Words.Append("#");

				if (population != 0)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 2, 73, Hues.Black);
				}

				this.Owner.Words.Clear();
				FormatCurrentYear();
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 2, 81, Hues.Black);

				short cost = (short)(((this.Owner.World.HardnessTier << 1) + Turn.TechnologyLevel + 6) *
					human.FoundScienceTally * (this.Owner.World.Season < 0 ? 1 : 2));
				short progress = CPU.SignedQuotient((short)(human.InquiryHeadway << 2), (short)(cost + 1));
				int bulb = this.Owner.Helpers.ClampValue(progress, 0, 3);

				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)((bulb << 3) + 0xa0), 0x78, 8, 8,
					Screen.Box, 0x30, 0x50);

				if (this.Owner.World.SmogImpactTier != 0)
				{
					int smog = this.Owner.Helpers.ClampValue(this.Owner.World.SmogImpactTier / 4, 0, 3);

					this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)((smog << 3) + 0xa0), 0x80, 8, 8,
						Screen.Box, 0x38, 0x50);
				}

				EmptyMessage()
					.Number(human.Gold)
					.Text("$")
					.Text(" ")
					.Number((short)(-(human.LevyRatio + human.ResearchLevyRatio - 10)))
					.Text(".")
					.Number(human.LevyRatio)
					.Text(".")
					.Number(human.ResearchLevyRatio);
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 2, 89, Hues.Black);

				if (Screen.RepaintingMainScreen == 0)
				{
					AnnouncePopulationMark(population);
				}
			}
		}

		/// <inheritdoc/>
		public void DrawPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPalace({participantIdx}, {castleTier}, {horizLoc}, {vertLoc})");

			// The levels and the styles of the sections are in the words of the frame of the original (B3: in C#): the left
			// neighbour of section 0 is the frame word 0x34 (never written, 0), the right neighbour of section 8 the style of
			// section 0 (0 for an AI player without it)

			this.Owner.VideoCard.RenderLine(Screen.Box, horizLoc - 5, vertLoc + 13, horizLoc + 68, vertLoc + 13, Hues.Green);

			if (this.Owner.World.Participants[participantIdx].TownTally != 0)
			{
				FrameWords frame = new FrameWords(this.Processor, 0x36);
				SectionTable levels = new SectionTable(frame, 0x32);
				SectionTable styles = new SectionTable(frame, 0x20);

				short parts = (short)this.Owner.Helpers.ClampValue(castleTier + 3, 3, 36);
				short highest = (short)(parts / 5);

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					SetSectionWord(levels, sectionIdx, sectionIdx is >= 3 and <= 5 ? (ushort)0 : (ushort)0xffff);
				}

				ushort seed = (ushort)(participantIdx * this.Owner.World.ChanceRoot);
				short built = 0;

				if (participantIdx == this.Owner.World.PersonParticipantIdx)
				{
					for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
					{
						SetSectionWord(levels, sectionIdx, this.Processor.PullGlobalWord((ushort)((sectionIdx << 1) + PalaceBuilder.PalaceSectionLevelsAddress)));
						SetSectionWord(styles, sectionIdx, this.Processor.PullGlobalWord((ushort)((sectionIdx << 1) + PalaceBuilder.PalaceSectionStylesAddress)));
					}

					built = 9;
				}
				else
				{
					for (built = 0; built < parts; built++)
					{
						ushort attempts = 0;
						bool fits;
						short chosen;
						short level;

						while (true)
						{
							fits = true;
							seed = (ushort)((seed * 5) + 1);
							chosen = (short)((byte)seed % 9);
							level = (short)(SectionWord(levels, chosen) + 1);

							if (chosen < 4 && ((short)SectionWord(levels, chosen + 1) < level || SectionWord(levels, 7 - chosen) == 0xffff))
							{
								fits = false;
							}

							if (chosen > 4 && ((short)SectionWord(levels, chosen - 1) < level || SectionWord(levels, 9 - chosen) == 0xffff))
							{
								fits = false;
							}

							if (level > highest || level > 3)
							{
								fits = false;
							}

							if (fits || ++attempts >= 0x3e7)
								break;
						}

						SetSectionWord(levels, chosen, (ushort)level);
						SetSectionWord(styles, chosen, (ushort)(participantIdx % 3));
					}
				}

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					short sectionLevel = (short)SectionWord(levels, sectionIdx);

					if (sectionLevel <= 0)
						continue;

					// 0: left end, 1: left, 2: middle, 3: right, 4: right end
					ushort shape = 2;

					if (sectionIdx < 4)
					{
						shape = (ushort)(SectionWord(levels, sectionIdx - 1) == 0xffff || sectionIdx == 1 ? 0 : 1);
					}

					if (sectionIdx > 4)
					{
						shape = (ushort)(SectionWord(levels, sectionIdx + 1) == 0xffff || sectionIdx == 7 ? 4 : 3);
					}

					int height = this.Owner.Helpers.ClampValue(sectionLevel - 1, 0, 3);

					this.Owner.Services.DrawPictureToScreen(Screen.Box, (7 * sectionIdx) + horizLoc, vertLoc - 1,
						this.Processor.PullGlobalWord((ushort)((0x28 * SectionWord(styles, sectionIdx)) + ((((short)height * 5) + shape) << 1) + PalaceSectionSpritesTableAddress)));
				}
			}
		}

		/// <inheritdoc/>
		public ushort ObtainPalaceScoreForLevel(short castleTier)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainPalaceScoreForLevel({castleTier})");

			short sum = 0;

			for (short tier = 0; tier <= castleTier; tier++)
			{
				sum = (short)(sum + (tier * (short)(7 - this.Owner.World.HardnessTier)));
			}

			ushort score = (ushort)((sum / 2) + 1);

			return score;
		}

		/// <inheritdoc/>
		public void FormatCurrentYear()
		{
			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(Math.Abs(this.Owner.World.Season), 10));

			this.Owner.Words.Append((ushort)((this.Owner.World.Season >= 0) ? EraAdStringAddress : EraBcStringAddress));
		}

		/// <inheritdoc/>
		public void HandleEndOfTurnEvents()
		{
			using var _ = this.Processor.Journal.Scope("HandleEndOfTurnEvents()");

			short pull = (short)(((ushort)this.Owner.World.DirtyCellTally << 1) - ((ushort)this.Owner.World.WorldHeatingTally << 2) - (ushort)this.Owner.World.SmogImpactTier);

			this.Owner.World.SmogImpactTier = (short)this.Owner.Helpers.ClampValue((short)(Math.Sign(pull) + this.Owner.World.SmogImpactTier), 0, 99);

			if (this.Owner.World.SmogImpactTier == 12 && this.Owner.World.DirtyCellTally > 6)
			{
				this.Owner.Words.Set("Scientists alarmed by\nrising temperatures.\nGlobal Warming feared!\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			if (this.Owner.World.SmogImpactTier > 16)
			{
				this.Owner.WorldSetup.EnactGlobalWarming(this.Owner.World.WorldHeatingTally);

				this.Owner.World.WorldHeatingTally++;
				this.Owner.World.SmogImpactTier = 0;
			}

			for (short attempt = 0; attempt <= this.Owner.World.HardnessTier; attempt++)
			{
				if (TryRivalWonder(attempt))
					break;
			}
		}

		/// <inheritdoc/>
		public void RepaintMainScreen()
		{
			using var _ = this.Processor.Journal.Scope("RepaintMainScreen()");

			this.Owner.Helpers.HaltHueCycling();

			Screen.RepaintingMainScreen = 0x1;

			if (Screen.VgaMode != 0)
			{
				this.Owner.Services.FillColors(5, 0, 0, 0);
			}

			this.Owner.Startup.HideMouseCursor();

			this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx,
				(short)MapView.Horiz,
				(short)MapView.Vert);

			FillBoxWithShadow(0, 97, 80, 103);

			this.Owner.Startup.ShowMouseCursor();

			ShowCivilizationScenery();

			Screen.RepaintingMainScreen = 0x0;

			this.Owner.Helpers.LaunchHueCycling();
		}

		/// <inheritdoc/>
		public void FillBoxWithShadow(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			this.Owner.Helpers.FillBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.Owner.Dialogs.DrawShadowBox(horizLoc, vertLoc, breadth - 1, altitude - 1, Hues.White, Hues.DarkGray);
		}

		/// <inheritdoc/>
		public void ShowCivilizationScenery()
		{
			using var _ = this.Processor.Journal.Scope("DisplayCivilizationScenery()");

			if (Screen.VgaMode != 0)
			{
				this.Owner.Helpers.HaltHueCycling();

				short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;
				char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
				char techChar = regime != 3 && this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x14) != 0 ? 'm' : 'a';
				string docname = $"back{regimeChar}{techChar}.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, docname, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, AdvisorPaletteResourceAddress, 0xc5be);

				this.Owner.Libc.CopyBytes(0xc744, 0xc35c, 0x180);

				this.Owner.Services.LoadColorsFromBuffer(5, 0xc5be);

				this.Owner.Helpers.LaunchHueCycling();
			}
		}

		/// <inheritdoc/>
		public short ObtainPalaceLevel(short rating)
		{
			short castleTier = 0;

			do
			{
				castleTier += 5;
				rating -= castleTier;
			} while (rating > 0);

			castleTier /= 5;
			castleTier--;

			return castleTier;
		}

		/// <summary>Signed division toward zero by 2^shift of the original (-32768 gives a positive quotient)</summary>
		private static short DivideByPowerOfTwo(short amount, int shift)
		{
			short quotient = (short)((short)Math.Abs((int)amount) >> shift);

			return amount < 0 ? (short)-quotient : quotient;
		}

		/// <summary>A word of a table of the sections (the index may be -1 or 9, like in the original)</summary>
		private static ushort SectionWord(SectionTable table, int sectionIdx)
		{
			return table.Frame.Frame(table.Distance - (sectionIdx << 1));
		}

		private static void SetSectionWord(SectionTable table, int sectionIdx, ushort amount)
		{
			table.Frame.SetFrame(table.Distance - (sectionIdx << 1), amount);
		}

		/// <summary>
		/// The start of the turn and the turns of the players; false when the game ended in it (0xdc48). startGold: the gold
		/// of the human player at the start of the turn.
		/// </summary>
		private bool PlayTurnOfPlayers(out short startGold)
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Helpers.LaunchHueCycling();

			short personIdx = this.Owner.World.PersonParticipantIdx;

			startGold = this.Owner.World.Participants[personIdx].Gold;

			Turn.UnitLimitTold = 0;
			Turn.RiotShown = 0;
			this.Owner.World.HighestScienceTally = 0;
			Turn.LiveRealmCount = 0;

			// The living players and the most technologies
			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				Turn.LiveRealmCount = (ushort)(Turn.LiveRealmCount + 1);

				short sciences = this.Owner.World.Participants[participantIdx].FoundScienceTally;

				if (this.Owner.World.HighestScienceTally < sciences)
				{
					this.Owner.World.HighestScienceTally = sciences;
				}
			}

			if (this.Owner.World.RoundTally == 0)
			{
				// The first turn: the view on the settler of the human player, the tutorial on the easiest level
				ushort troop = (ushort)(0x600 * personIdx);

				MapView.Horiz = (ushort)(this.Troops.PullAt(troop, UnitMemory.Horiz) - 7);
				MapView.Vert = (ushort)(this.Troops.PullAt(troop, UnitMemory.Vert) - 6);

				RepaintMainScreen();

				if (this.Owner.World.HardnessTier == 0)
				{
					this.Owner.TurnLogic.DrawUnitInfo(this.Owner.World.PersonParticipantIdx, 0);

					this.Owner.Prologue.ShowInterfaceTutorial();

					RepaintMainScreen();
				}

				this.Owner.Services.PerformSound(1, 0);
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				this.Owner.World.Participants[participantIdx].Tally = 0;

				if ((this.Owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				// After a loaded game the turn goes on at the human player
				if (participantIdx < this.Owner.World.PersonParticipantIdx && Files.MidTurnSave != 0)
					continue;

				PlayTurnOfPlayer(participantIdx);

				if (Turn.GameEndRequest != 0 && Turn.ConquestEnd == 0)
				{
					if (Turn.GameEndRequest == 2)
					{
						ShowEndGameSequence();

						ShowTitleScreen();
					}

					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// The turn of one player (the busy light at the bottom left): its cities (not after a loaded game), at most 30000 gold,
		/// peace treaties, the AI; for the human player the status bar, the end without cities and settlers, the truce count
		/// (AD), then its own turn (unless the game ended)
		/// </summary>
		private void PlayTurnOfPlayer(short participantIdx)
		{
			Player player = this.Owner.World.Participants[participantIdx];
			bool person = participantIdx == this.Owner.World.PersonParticipantIdx;

			this.Owner.Services.FloodBox(Screen.Box, 2, 192, 6, 6, Hues.DarkGray);

			if (person)
			{
				this.Owner.Startup.ShowMouseCursor();
			}

			if (Files.MidTurnSave == 0)
			{
				this.Owner.Advisors.HandleAllCities(participantIdx);
			}

			if (player.Gold > 0x7530)
			{
				player.Gold = 0x7530;
			}

			this.Owner.Politics.HandlePeaceTreaties(participantIdx);

			this.Owner.Bot.OperateAITurn(participantIdx);

			this.Owner.Bot.ReimposeStrategicLocations(participantIdx);

			this.Owner.Services.FloodBox(Screen.Box, 2, 192, 6, 6, Hues.White);

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				DrawStatusBar();

				if (player.TownTally == 0 && player.ColonistTally == 0)
				{
					this.Owner.Startup.HideMouseCursor();

					this.Owner.Prologue.ShowTerrainTutorial();

					Turn.GameEndRequest = 0xffff;

					this.Owner.Startup.ShowMouseCursor();
				}

				if (this.Owner.World.Season > 0)
				{
					this.Owner.World.TruceRoundTally++;
				}
			}

			if (Turn.GameEndRequest == 0)
			{
				this.Owner.TurnLogic.HandlePlayerTurn(participantIdx);
			}

			Files.MidTurnSave = 0x0;

			if (person)
			{
				this.Owner.Startup.HideMouseCursor();
			}
		}

		/// <summary>The end of the turn after the players</summary>
		private void FinishTurn(short startGold)
		{
			this.Owner.Startup.ShowMouseCursor();

			ShowTravellersReport();

			HandleEndOfTurnEvents();

			if (this.Owner.World.RoundTally > 50)
			{
				StrikeRandomCities();
			}

			RefreshPeaceGraphContent();

			WarnOfBankruptcy(startGold);

			AdvanceYear();

			MendFortifiedUnits();

			if (this.Owner.World.PersonParticipantIdx != -1)
			{
				this.Owner.SaveLoad.PerformBackup();
			}

			if (this.Owner.World.RoundTally % 0x32 == 0 && this.Owner.World.PersonParticipantIdx != -1)
			{
				NoteStatusInReplay();
			}

			if (this.Owner.World.RoundTally >= this.Owner.World.FollowingCollectionRound)
			{
				this.Owner.World.FollowingCollectionRound = (short)(this.Owner.Libc.Dice.DrawInt(40) + this.Owner.World.RoundTally + 0x14);

				this.Owner.Atlas.ShowEpicChronicle();
			}

			ScoreWondersAndPollution();

			GrowPalace();

			DrawStatusBar();

			if (!ChooseGameEnd())
				return;

			EndGame();
		}

		/// <summary>
		/// Disasters for random cities of size 5 or more (not the last city of their owner): two drawn cities
		/// (TurnEndRules.DisasterDraws)
		/// </summary>
		private void StrikeRandomCities()
		{
			for (int draw = 0; draw < TurnEndRules.DisasterDraws; draw++)
			{
				short townIdx = (short)this.Owner.Libc.Dice.DrawInt(128);

				StrikeCity(townIdx);
			}
		}

		/// <summary>A disaster for the city when it is of size 5 or more and not the last city of its owner</summary>
		private void StrikeCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if (town.ConditionMarker != 0xff && this.Owner.World.Participants[town.ParticipantIdx].TownTally > 1 && town.TrueExtent >= 5)
			{
				this.Owner.Founding.HandleCityDisasters(townIdx);
			}
		}

		/// <summary>
		/// The warning (text 0x1c2a) when the human player with more than one city and less than 100 gold would be broke at
		/// this rate (gold + 10 * the change of this turn below 0) and has the warning on (ParticipantMarkers)
		/// </summary>
		private void WarnOfBankruptcy(short startGold)
		{
			if (Turn.GameEndRequest != 0)
				return;

			Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

			if (person.TownTally <= 1 || person.Gold >= 100)
				return;

			if ((short)((10 * (person.Gold - startGold)) + person.Gold) >= 0)
				return;

			if (!this.Owner.World.IsHuman(this.Owner.World.PersonParticipantIdx))
				return;

			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(BankruptcyWarningDialogAddress);

			Screen.DialogStyle = 0x4;

			ShowDialog(TextBuffer.Address, 80, 80);
		}

		/// <summary>
		/// The next turn and year: 20 years a turn until 1000 AD, 10 until 1500, 5 until 1750, 2 until 1850 (1 with a
		/// spaceship of a player other than 0), then 1; there is no year 0 (1 AD, the research doubled) and 20 AD is skipped
		/// </summary>
		private void AdvanceYear()
		{
			this.Owner.World.RoundTally++;

			short season = this.Owner.World.Season;

			this.Owner.World.Season += season < 1000 ? (short)20 : season < 1500 ? (short)10 : season < 1750 ? (short)5 :
				season < 1850 && (this.Owner.World.StarshipMarkers & 0xfe) == 0 ? (short)2 : (short)1;

			if (this.Owner.World.Season == 0)
			{
				for (int participantIdx = 0; participantIdx < 8; participantIdx++)
				{
					this.Owner.World.Participants[participantIdx].InquiryHeadway <<= 1;
				}

				this.Owner.World.Season = 1;
			}

			if (this.Owner.World.Season == 21)
			{
				this.Owner.World.Season = 20;
			}
		}

		/// <summary>
		/// Every 50 turns: the most technologies, the technology level of the time (0xd2de, AD only), and into the replay the
		/// status of the human player (11: cities, population) and the ranking (12: the players by their standing, two places a byte; the bytes are made
		/// in the frame and given in the reverse order, like in the original)
		/// </summary>
		private void NoteStatusInReplay()
		{
			this.Owner.World.HighestScienceTally = 0;

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				short sciences = this.Owner.World.Participants[participantIdx].FoundScienceTally;

				if (this.Owner.World.HighestScienceTally < sciences)
				{
					this.Owner.World.HighestScienceTally = sciences;
				}
			}

			Turn.TechnologyLevel = 0x0;

			if (this.Owner.World.Season >= 0)
			{
				Turn.TechnologyLevel = (ushort)this.Owner.Helpers.ClampValue(
					this.Owner.World.HighestScienceTally - (this.Owner.World.RoundTally / 9), 0, 6);
			}

			ushort population = this.Owner.Helpers.ComputePlayerPopulation(this.Owner.World.PersonParticipantIdx);

			this.Owner.UnitOps.AttachReplayContent(11, (byte)this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].TownTally,
				TurnEndRules.ReplayPopulationHigh(population), (byte)population);

			byte[] ranking = new byte[4];

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.LiveRealms & (1 << participantIdx)) == 0)
					continue;

				short standing = this.Owner.World.Participants[participantIdx].Standing;
				int pairIdx = standing / 2;
				if (pairIdx >= 0 && pairIdx < 4)
				{
					byte nibble = (byte)((standing & 1) == 0 ? participantIdx : (participantIdx << 4));
					ranking[pairIdx] |= nibble;
				}
			}

			this.Owner.UnitOps.AttachReplayContent(12, ranking[3], ranking[2], ranking[1], ranking[0]);
		}

		/// <summary>
		/// 25 points for every wonder in a city; the pollution costs the human player 10 points a polluted tile, unless the
		/// flag 0x3484 is -3 (then -2 for the next turn; -2 becomes 0 after counting)
		/// </summary>
		private void ScoreWondersAndPollution()
		{
			for (int marvelIdx = 1; marvelIdx <= 0x15; marvelIdx++)
			{
				ushort townIdx = (ushort)this.Owner.World.MarvelTownIdx[marvelIdx];

				if (townIdx == 0xffff || townIdx == 0x80)
					continue;

				this.Owner.World.Participants[this.Owner.World.Towns[townIdx].ParticipantIdx].Tally += 25;
			}

			if (Files.LoadedGameMark == 0xfffd)
			{
				Files.LoadedGameMark = 0xfffe;
				return;
			}

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Tally -= (short)(10 * this.Owner.World.DirtyCellTally);

			if (Files.LoadedGameMark == 0xfffe)
			{
				Files.LoadedGameMark = 0x0;
			}
		}

		/// <summary>
		/// The palace grows a level (DisplayPalace, at most 37) when the human player has a palace, the palace option (0x80) is
		/// on, the game is not over (spaceship bit 0x100) and the score reaches the next level
		/// </summary>
		private void GrowPalace()
		{
			bool palace = false;

			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ConditionMarker != 0xff && town.ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
					town.Has(CityStructure.Palace))
				{
					palace = true;
				}
			}

			if (!palace || (this.Owner.World.SessionPrefMarkers & 0x80) == 0 || (this.Owner.World.StarshipMarkers & 0x100) != 0)
				return;

			short needed = (short)ObtainPalaceScoreForLevel((short)(this.Owner.World.CastleTier + 1));

			if ((short)this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIdx, false) < needed)
				return;

			this.Owner.World.CastleTier++;

			if (this.Owner.World.CastleTier > 37)
				return;

			this.Owner.Throne.DisplayPalace();
		}

		/// <summary>
		/// The spaceship that arrived this year (0x1cf6, -1 none), the retirement news (20 years before the end year 2100 -
		/// 20 * difficulty); true when the game ends: a spaceship arrived, conquest (0xb884) or the end year
		/// </summary>
		private bool ChooseGameEnd()
		{
			Spaceship.ArrivedSpaceship = 0xffff;

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if ((this.Owner.World.StarshipMarkers & (1 << participantIdx)) == 0)
					continue;

				if (this.Owner.World.Participants[participantIdx].StarshipArrivalSeason == this.Owner.World.Season)
				{
					Spaceship.ArrivedSpaceship = (ushort)participantIdx;
				}
			}

			if ((short)(0x820 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season)
			{
				Player person = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

				Message(GovernmentTitle(person.RegimeKind))
					.Text(" ")
					.Text(LeaderName(this.Owner.World.PersonParticipantIdx))
					.Text("\nplans retirement\nin 20 years.\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			return Spaceship.ArrivedSpaceship != 0xffff || Turn.ConquestEnd != 0 ||
				(short)(0x834 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season;
		}

		/// <summary>
		/// The end of the game: conquest, the spaceship arrival, the end of the reign (each with its news), the end sequence
		/// (not after a launched spaceship of the human player... bit 0x100), and the question to go on (not after conquest)
		/// </summary>
		private void EndGame()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.Words.Set("*** GAME OVER ***\n");

			if (Turn.ConquestEnd != 0)
			{
				this.Owner.Words.Set("Your civilization\nhas conquered\nthe entire planet!\n");

				Spaceship.ArrivedSpaceship = 0xffff;

				this.Owner.Paper.ShowNewspaper(-1);

				this.Owner.Replayer.ShowConquerorScreen();
			}

			if (Spaceship.ArrivedSpaceship != 0xffff)
			{
				this.Owner.Prologue.ShowCivilizationScreen((short)Spaceship.ArrivedSpaceship);

				RepaintMainScreen();

				Message(CivilizationName(Spaceship.ArrivedSpaceship))
					.Text(" spaceship\narrives at Alpha Centauri.\n");

				this.Owner.Paper.ShowNewspaper(-1);
			}

			if ((short)(0x834 - (0x14 * this.Owner.World.HardnessTier)) == this.Owner.World.Season)
			{
				Message(LeaderName(this.Owner.World.PersonParticipantIdx))
					.Text(" dynasty\nends after glorious\n6000 year reign!\n");

				ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals);

				this.Owner.Services.PerformSound(LeaderWordAt(leader, LeaderTable.Tune), 3);

				this.Owner.Paper.ShowNewspaper(-1);

				this.Owner.Services.PerformSound(1, 0);
			}

			this.Owner.Startup.ShowMouseCursor();

			if ((this.Owner.World.StarshipMarkers & 0x100) == 0)
			{
				ShowEndGameSequence();
			}

			RepaintMainScreen();

			// (the original sets bit 0 of the byte 0x4cd9 here: the emulated memory does not let the game touch 0x4cd8 - 0x4cd9,
			// so the write never happened, and nothing reads it)

			if (Turn.ConquestEnd == 0)
			{
				this.Owner.Words.Clear();

				this.Owner.BlockText.ShowTextDialog(RetirementContinuePromptAddress);

				// The first line goes on with the game, the second one ends it
				Turn.GameEndRequest = (ushort)(1 - ShowDialog(TextBuffer.Address, 80, 80));
			}
			else
			{
				Turn.GameEndRequest = 0x1;
			}

			if (Turn.GameEndRequest != 0)
			{
				ShowTitleScreen();
			}
		}

		/// <summary>The landing ship of the barbarians with its units (see ShowTravellersReport)</summary>
		private void LandBarbariansFromSea()
		{
			short horizLoc, vertLoc;

			do
			{
				horizLoc = (short)this.Owner.Libc.Dice.DrawInt(80);

				vertLoc = (short)(this.Owner.Libc.Dice.DrawInt(44) + 3);
			}
			while (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != 0xa ||
				this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff ||
				(short)this.Owner.MapOps.ObtainLandMassSize(horizLoc, vertLoc) < 0x10);

			short units = (short)this.Owner.Helpers.ClampValue((this.Owner.World.RoundTally / 150) + 1, 1, 3);

			this.Owner.UnitOps.CreateUnit(0, units < 3 ? (short)0x11 : (short)0x12, horizLoc, vertLoc);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.Owner.World.ScienceLeadingFoundVia[56] & 7) != 0 ? (short)7 : (short)3;

				if (unitIdx == units)
					kind = 0x1a;

				ushort troopIdx = this.Owner.UnitOps.CreateUnit(0, kind, horizLoc, vertLoc);

				// The barbarian units start on sentry (player 0); troopIdx may be past the table (0xffff, no free place)
				int marker = (ushort)(UnitMemory.TroopStride * troopIdx);
				this.Troops.AssignAt(marker, UnitMemory.Status, (byte)(this.Troops.PullAt(marker, UnitMemory.Status) | 1));
			}
		}

		/// <summary>The barbarians landing near a city of the human player (see ShowTravellersReport)</summary>
		private void RaidHumanCity()
		{
			ushort townIdx = (ushort)this.Owner.Libc.Dice.DrawInt(128);

			if (this.Owner.World.Towns[townIdx].ConditionMarker == 0xff ||
				this.Owner.World.Towns[townIdx].ParticipantIdx != this.Owner.World.PersonParticipantIdx)
				return;

			ushort plunder = this.Owner.Conquest.ComputeCityPlunder((short)townIdx);

			if ((short)this.Owner.Libc.Dice.DrawInt(100) >= (short)plunder)
				return;

			ushort tries = 0;
			short horizLoc, vertLoc;

			while (true)
			{
				int shift = this.Owner.Libc.Dice.DrawInt(13);
				horizLoc = (short)((short)this.Owner.World.Towns[townIdx].Place.X + shift - 6);

				shift = this.Owner.Libc.Dice.DrawInt(13);
				vertLoc = (short)((short)this.Owner.World.Towns[townIdx].Place.Y + shift - 6);

				townIdx = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);
	
				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance(horizLoc, this.Owner.World.Towns[townIdx].Place.X, vertLoc, this.Owner.World.Towns[townIdx].Place.Y);

				tries++;

				if ((short)tries >= 0x63)
					return;

				if (distance < 6)
					continue;

				if (this.Owner.UnitOps.CheckTileOccupation(0, horizLoc, vertLoc) != 0)
					continue;

				if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa)
					continue;

				if (this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff)
					continue;

				ushort continent = this.Owner.MapOps.ObtainContinentID(this.Owner.World.Towns[townIdx].Place.X, this.Owner.World.Towns[townIdx].Place.Y);

				if (this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc) == continent)
					break;
			}

			if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
				((sbyte)this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (byte)this.Owner.World.PersonParticipantIdx)) != 0)
			{
				this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx,
					this.Owner.World.Towns[townIdx].Place.X - 8,
					this.Owner.World.Towns[townIdx].Place.Y - 6);

				this.Owner.Words.Set("Travellers report:\n");

				if ((this.Owner.World.ScienceLeadingFoundVia[35] & 7) != 0)
				{
					this.Owner.Words.Append(TravellersReportGunpowderAddress);
				}
				else if ((this.Processor.PullUInt8(this.Processor.SegExtra.Value16, 0x4f4) & 7) != 0)
				{
					this.Owner.Words.Append(TravellersReportIronWorkingAddress);
				}
				else
				{
					this.Owner.Words.Append(TravellersReportAncientAddress);
				}

				this.Owner.Conquest.ObtainCityLabel((short)townIdx);

				this.Owner.Words.Append("!\n");

				Screen.DialogStyle = 0x2;

				ShowDialog(TextBuffer.Address, 100, 32);
			}

			short units = (short)this.Owner.Helpers.ClampValue(this.Owner.Libc.Dice.DrawInt(this.Owner.World.Towns[townIdx].TrueExtent / 2), 2, 99);

			short unitIdx;

			for (unitIdx = 0; unitIdx <= units; unitIdx++)
			{
				short kind = (this.Owner.World.ScienceLeadingFoundVia[35] & 7) != 0 ? ((unitIdx & 4) != 0 ? (short)9 : (short)4) :
					((unitIdx & 4) != 0 ? (short)0xa : (short)6);

				if (unitIdx == units)
					kind = 0x1a;

				ushort sight = (ushort)((ushort)(0xc * this.Owner.UnitOps.CreateUnit(0, kind, horizLoc, vertLoc)) + 0x81dd);
				this.Processor.EmitGlobalByte(sight, (byte)(this.Processor.PullGlobalByte(sight) | (byte)this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc]));

				if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
				{
					this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
				}
			}
		}

		/// <summary>The place of the player in the power and peace graphs: 8 * (turn / 4 % 150) + player</summary>
		private ushort GraphPlace(short participantIdx)
		{
			return (ushort)(((DivideByPowerOfTwo(this.Owner.World.RoundTally, 2) % 150) << 3) + participantIdx);
		}

		/// <summary>
		/// The population mark of DrawStatusBar: when the population passes the next mark 0x1c24 (in tens below 100, then in
		/// hundreds with 9 added), the mark moves and, unless 0x3484 is below -1, a dialog tells "...exceeds N00,000 citizens"
		/// </summary>
		private void AnnouncePopulationMark(short population)
		{
			short previousMark = (short)Turn.PopulationMark;

			Message("The population of the\nfertile ")
				.Text(CivilizationName(this.Owner.World.PersonParticipantIdx))
				.Text(" empire now\nexceeds ");

			if (population < 100)
			{
				if ((short)(Turn.PopulationMark * 10) < population)
				{
					short mark = (short)(population / 10);

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(mark, 10));

					Turn.PopulationMark = (ushort)mark;
				}
			}
			else if ((short)((Turn.PopulationMark - 9) * 100) < population)
			{
				short hundreds = (short)(population / 100);

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(hundreds, 10));
				this.Owner.Words.Append(",0");

				Turn.PopulationMark = (ushort)(hundreds + 9);
			}

			if (previousMark < (short)Turn.PopulationMark && (short)Files.LoadedGameMark >= -1)
			{
				this.Owner.Words.Append("00,000 citizens.\n");

				Screen.DialogStyle = 4;

				ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		/// <summary>One try for a wonder built by a rival (see HandleEndOfTurnEvents), true when one was built</summary>
		private bool TryRivalWonder(short attempt)
		{
			short person = this.Owner.World.PersonParticipantIdx;
			ushort wonderIdx = (ushort)(this.Owner.Libc.Dice.DrawInt(21) + 1);

			if (this.Owner.World.HardnessTier == 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(person, (short)BuildingWord(wonderIdx, BuildingTable.Technology)) == 0)
				return false;

			if (this.Owner.World.HardnessTier < 2)
			{
				for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (HumanCityBuilds(townIdx, wonderIdx))
						wonderIdx = 0x11;
				}
					}

			ushort cityIdx = (ushort)this.Owner.Libc.Dice.DrawInt(128);

			if (this.Owner.World.Towns[cityIdx].ConditionMarker == 0xff)
				return false;

			ushort rivalIdx = (ushort)this.Owner.World.Towns[cityIdx].ParticipantIdx;

			if ((this.Owner.World.Participants[rivalIdx].Relations[0] & 4) != 0)
				return false;

			if (wonderIdx == 0x11 && this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) == 0)
				return false;

			if (attempt == 0 && this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, 0x2e) != 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(person, 0x2e) == 0 && this.Owner.World.MarvelTownIdx[17] == -1)
			{
				wonderIdx = 0x11;
				}

			if (wonderIdx == 0x11 && this.Owner.Advisors.PlayerKnowsTechnology(person, 0x2e) != 0 &&
				this.Owner.World.Participants[rivalIdx].MartialMight > this.Owner.World.Participants[person].MartialMight)
				return false;

			if (rivalIdx == 0xffff || rivalIdx == (ushort)person)
				return false;

			if (this.Owner.World.MarvelTownIdx[wonderIdx] != -1)
				return false;

			if (this.Owner.World.Towns[cityIdx].TrueExtent <= CPU.SignedQuotient((short)BuildingWord(24 + wonderIdx, BuildingTable.Price), 10))
				return false;

			if (this.Owner.Advisors.PlayerKnowsTechnology((short)rivalIdx, (short)BuildingWord(24 + wonderIdx, BuildingTable.Technology)) == 0)
				return false;

			this.Owner.World.MarvelTownIdx[wonderIdx] = (short)cityIdx;
			this.Owner.World.Towns[cityIdx].MaterialsTally = 0;

			this.Owner.UnitOps.AttachReplayContent(10, (byte)rivalIdx, (byte)wonderIdx);

			EmptyMessage()
				.City((short)cityIdx)
				.Text(" (")
				.Text(CivilizationName(rivalIdx))
				.Text(")\nbuilds ")
				.Text(BuildingRecordText(24 + wonderIdx))
				.Text(".\n");

			this.Owner.World.Participants[rivalIdx].Gold -= (short)this.Owner.Helpers.ClampValue(this.Owner.World.Participants[rivalIdx].Gold / 3, 0,
				(10 * (short)BuildingWord(24 + wonderIdx, BuildingTable.Price)) - this.Owner.World.Towns[cityIdx].MaterialsTally);

			if (MapView.WorldMapView != 1)
			{
				this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[rivalIdx].ChiefVisuals, LeaderTable.ShortTune), 0);

				Screen.DialogStyle = (this.Owner.World.Participants[person].Relations[rivalIdx] & 0x40) == 0 ? (ushort)2 : (ushort)5;

				ShowDialog(TextBuffer.Address, 100, 80);

				this.Owner.Encyclopedia.ShowCivilopediaRecord((ushort)(wonderIdx + 0x18), 1);

				RepaintMainScreen();

				this.Owner.Services.PerformSound(1, 0);
			}

			if (wonderIdx == 0xe)
			{
				this.Owner.Advisors.UncoverTechnology((short)rivalIdx, 0);

				this.Owner.Advisors.UncoverTechnology((short)rivalIdx, 0);
			}

			for (ushort townIdx = 0; townIdx < 0x80; townIdx++)
			{
				if (HumanCityBuilds(townIdx, wonderIdx))
					this.Owner.Advisors.RenewCityScreen((short)townIdx);
			}

			return true;
		}

		/// <summary>Whether the city is a city of the human player building the wonder</summary>
		private bool HumanCityBuilds(ushort townIdx, ushort wonderIdx)
		{
			return this.Owner.World.Towns[townIdx].ConditionMarker != 0xff &&
				this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
				this.Owner.World.Towns[townIdx].PresentMakingIdx == (short)-(wonderIdx + 0x18);
		}
	}
}
