namespace Civ_1_5_Remastered.Core.UI.Reports
{
	public class Gazette : GameComponent, IGazette
	{
		private const ushort NewspaperNameSuffixTimesAddress = 0x5118;
		private const ushort NewspaperNameSuffixGazetteAddress = 0x5120;
		private const ushort NewspaperNameSuffixTribuneAddress = 0x512c;
		private const ushort NewspaperNameSuffixChronicleAddress = 0x5138;
		private const ushort NewspaperLeftPriceLabelAddress = 0x5147;
		private const ushort NewspaperRightPriceLabelAddress = 0x514b;
		private const ushort NewspaperMottoFirstAddress = 0x514f;
		private const ushort NewspaperMottoSecondAddress = 0x5156;
		private const ushort NewspaperMenuPromptAddress = 0x5181;

		private static readonly GameText NewspaperTextFileNameAddress = new(0x5178, "king.txt");

		public Gazette(IGameEngine owner)
			: base(owner)
		{
		}

		public void ShowNewspaper(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayNewspaper({townIndex})");

			short givenIndex = townIndex;
			bool shown = townIndex != -2;

			if (townIndex < 0)
			{
				for (short cityIndex = 0; cityIndex < 0x80; cityIndex++)
				{
					City town = this.Owner.World.Towns[cityIndex];

					if (town.ConditionMarker == 0xff || town.ParticipantIndex != this.Owner.World.PersonParticipantIndex)
					{
						continue;
					}

					if (town.Has(CityStructure.Palace) || townIndex == -1)
					{
						townIndex = cityIndex;
					}
				}
			}

			this.Owner.Startup.HideMouseCursor();

			if (shown)
			{
				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 100, Hues.White);

			DrawingBox.TypefaceIndex = 0x4;

			this.Owner.VideoCard.RenderLine(Screen.Box, 1, 1, 318, 1, Hues.Black);

			this.Owner.Services.FloodBox(Screen.Box, 8, 3, 304, 20, Hues.White);

			DrawingBox.RearHues = 0xf;

			this.Owner.VideoCard.RenderLine(Screen.Box, 0, 35, 319, 35, Hues.Black);

			this.Owner.VideoCard.RenderLine(Screen.Box, 1, 1, 1, 35, Hues.Black);

			this.Owner.VideoCard.RenderLine(Screen.Box, 318, 1, 318, 35, Hues.Black);

			this.Owner.VideoCard.RenderLine(Screen.Box, 0, 97, 319, 97, Hues.Black);

			if (shown)
			{
				for (short personIndex = 0; personIndex < 0x10; personIndex++)
				{
					this.Owner.Services.DrawPictureToScreenAlternate(Screen.Box, 20 * personIndex,
						100 - this.Owner.Runtime.Dice.DrawInteger(2), Icons.NewspaperCrowdPicture);
				}
			}

			this.Owner.BlockText.DrawTextChunk(40, 16, 40, 0);

			DrawingBox.TypefaceIndex = 0x5;
			this.Owner.Words.Clear();

			switch (this.Owner.Runtime.Dice.DrawInteger(4))
			{
				case 0:
					this.Owner.Conquest.ObtainCityLabel(townIndex);
					this.Owner.Words.Append(NewspaperNameSuffixTimesAddress);
					break;

				case 1:
					this.Owner.Conquest.ObtainCityLabel(townIndex);
					this.Owner.Words.Append(NewspaperNameSuffixGazetteAddress);
					break;

				case 2:
					Message("The ")
						.City(townIndex)
						.Text(NewspaperNameSuffixTribuneAddress);
					break;

				case 3:
					Message("The ")
						.City(townIndex)
						.Text(NewspaperNameSuffixChronicleAddress);
					break;
			}

			short width = (short)(ushort)this.Owner.TextPainter.MeasureTextWidth(this.Owner.Words.ToString());

			if (width > 0x12c)
			{
				EmptyMessage()
					.City(townIndex)
					.Text(" News");

				width = (short)(ushort)this.Owner.TextPainter.MeasureTextWidth(this.Owner.Words.ToString());
			}

			if (width < 0xc8)
			{
				this.Owner.TextPainter.RenderText(NewspaperLeftPriceLabelAddress, 8, 11, 0);

				this.Owner.TextPainter.RenderText(NewspaperRightPriceLabelAddress, 268, 11, 0);
			}

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 11, Hues.Black);

			this.Owner.Words.Set(this.Owner.Runtime.Dice.DrawInteger(2) != 0 ? NewspaperMottoFirstAddress : NewspaperMottoSecondAddress);

			DrawingBox.TypefaceIndex = 0x3;

			this.Owner.TextPainter.RenderText(this.Owner.Words.ToString(), 6, 3, 0);

			this.Owner.TextPainter.RenderText(this.Owner.Words.ToString(), 272, 3, 0);

			DrawingBox.TypefaceIndex = 0x1;
			EmptyMessage()
				.Text("10 cents");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 272, 28, Hues.Black);

			this.Owner.Words.Set("January 1, ");

			this.Owner.TurnHooks.FormatCurrentYear();

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, 28, Hues.Black);

			DrawingBox.TypefaceIndex = 0x2;

			short storyIndex = (short)this.Owner.Runtime.Dice.DrawInteger(14);
			char letter = (char)('A' + storyIndex);
			string topic = $"*NEWS{letter}";
			this.Owner.Words.Clear();

			this.Owner.BlockText.LoadTextFromFile(NewspaperTextFileNameAddress, topic);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 3, Hues.Black);

			DrawingBox.TypefaceIndex = 0x1;

			if (shown)
			{
				this.Owner.Dialogs.ShowMenu(NewspaperMenuPromptAddress, 80, 128, 1);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();
		}
	}
}
