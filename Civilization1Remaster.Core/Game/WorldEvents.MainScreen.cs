using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The main screen: the status bar, the status line and the repainting</summary>
	public partial class WorldEvents
	{
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
		public void FormatCurrentYear()
		{
			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(Math.Abs(this.Owner.World.Season), 10));

			this.Owner.Words.Append((ushort)((this.Owner.World.Season >= 0) ? EraAdStringAddress : EraBcStringAddress));
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
	}
}
