namespace Civilization1Remaster.Core.Game
{
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
	}
}
