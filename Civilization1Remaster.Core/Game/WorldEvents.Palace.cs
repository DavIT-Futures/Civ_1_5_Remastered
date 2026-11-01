namespace Civilization1Remaster.Core.Game
{
	public partial class WorldEvents
	{
		public void DrawPalace(short participantIdx, short castleTier, short horizLoc, short vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RenderPalace({participantIdx}, {castleTier}, {horizLoc}, {vertLoc})");

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
				if (participantIdx == this.Owner.World.PersonParticipantIdx)
				{
					for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
					{
						SetSectionWord(levels, sectionIdx, this.Processor.PullGlobalWord((ushort)((sectionIdx << 1) + PalaceBuilder.PalaceSectionLevelsAddress)));
						SetSectionWord(styles, sectionIdx, this.Processor.PullGlobalWord((ushort)((sectionIdx << 1) + PalaceBuilder.PalaceSectionStylesAddress)));
					}
				}
				else
				{
					short built;
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
							{
								break;
							}
						}

						SetSectionWord(levels, chosen, (ushort)level);
						SetSectionWord(styles, chosen, (ushort)(participantIdx % 3));
					}
				}

				for (int sectionIdx = 0; sectionIdx < 9; sectionIdx++)
				{
					short sectionLevel = (short)SectionWord(levels, sectionIdx);

					if (sectionLevel <= 0)
					{
						continue;
					}

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

		public short ObtainPalaceLevel(short rating)
		{
			short castleTier = 0;

			do
			{
				castleTier += 5;
				rating -= castleTier;
			}
			while (rating > 0);

			castleTier /= 5;
			castleTier--;

			return castleTier;
		}

		private static ushort SectionWord(SectionTable table, int sectionIdx)
		{
			return table.Frame.Frame(table.Distance - (sectionIdx << 1));
		}

		private static void SetSectionWord(SectionTable table, int sectionIdx, ushort amount)
		{
			table.Frame.SetFrame(table.Distance - (sectionIdx << 1), amount);
		}

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
			{
				return;
			}

			short needed = (short)ObtainPalaceScoreForLevel((short)(this.Owner.World.CastleTier + 1));

			if ((short)this.Owner.Founding.ShowFinalScore(this.Owner.World.PersonParticipantIdx, false) < needed)
			{
				return;
			}

			this.Owner.World.CastleTier++;

			if (this.Owner.World.CastleTier > 37)
			{
				return;
			}

			this.Owner.Throne.DisplayPalace();
		}
	}
}
