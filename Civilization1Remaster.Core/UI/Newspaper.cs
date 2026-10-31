using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 21.
	/// </summary>
	public class Newspaper : GameModule
	{
		private const ushort NewspaperNameSuffixTimesAddress = 0x5118;
		private const ushort NewspaperNameSuffixGazetteAddress = 0x5120;
		private const ushort NewspaperNameSuffixTribuneAddress = 0x512c;
		private const ushort NewspaperNameSuffixChronicleAddress = 0x5138;
		private const ushort NewspaperLeftPriceLabelAddress = 0x5147;
		private const ushort NewspaperRightPriceLabelAddress = 0x514b;
		private const ushort NewspaperMotto1Address = 0x514f;
		private const ushort NewspaperMotto2Address = 0x5156;
		private const ushort NewspaperMenuPromptAddress = 0x5181;

		private static readonly GameText NewspaperTextFileNameAddress = new(0x5178, "king.txt");

		public Newspaper(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The newspaper of the city (a negative city: the human player's palace city, for -1 else its first city): the
		/// title (one of four headlines with the city's name, " News" when too wide, ornaments when narrow), "10 cents", the
		/// date and a random story *NEWSA - *NEWSN of the text file 0x5178. Unless townIdx is -2 (drawn on the current screen,
		/// without waiting): a crowd of people under it, the screen kept and put back after a key.
		/// </summary>
		public void DisplayNewspaper(short townIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayNewspaper({townIdx})");


			short givenIdx = townIdx;
			bool shown = townIdx != -2;

			if (townIdx < 0)
			{
				for (short cityIdx = 0; cityIdx < 0x80; cityIdx++)
				{
					City town = this.Owner.World.Towns[cityIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != this.Owner.World.PersonParticipantIdx)
						continue;

					if (town.Has(CityStructure.Palace) || townIdx == -1)
					{
						townIdx = cityIdx;
					}
				}
			}

			this.Owner.Startup.ConcealMouseCursor();

			if (shown)
			{
				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			}

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 100, Hues.White);

			DrawingBox.TypefaceIdx = 0x4;

			this.Owner.VideoCard.RenderLine(ScreenBox, 1, 1, 318, 1, Hues.Black);

			this.Owner.Services.FloodBox(ScreenBox, 8, 3, 304, 20, Hues.White);

			DrawingBox.RearHues = 0xf;

			this.Owner.VideoCard.RenderLine(ScreenBox, 0, 35, 319, 35, Hues.Black);

			this.Owner.VideoCard.RenderLine(ScreenBox, 1, 1, 1, 35, Hues.Black);

			this.Owner.VideoCard.RenderLine(ScreenBox, 318, 1, 318, 35, Hues.Black);

			this.Owner.VideoCard.RenderLine(ScreenBox, 0, 97, 319, 97, Hues.Black);

			if (shown)
			{
				for (short personIdx = 0; personIdx < 0x10; personIdx++)
				{
					this.Owner.Services.RenderPictureToScreenAlternate(ScreenBox, 20 * personIdx,
						100 - this.Owner.Libc.Dice.DrawInt(2), NewspaperCrowdPicture);
				}
			}

			this.Owner.BlockText.RenderTextChunk(40, 16, 40, 0);

			DrawingBox.TypefaceIdx = 0x5;
			this.Owner.Words.Clear();

			switch (this.Owner.Libc.Dice.DrawInt(4))
			{
				case 0:
					this.Owner.Conquest.ObtainCityLabel(townIdx);
					this.Owner.Words.Append(NewspaperNameSuffixTimesAddress);
					break;

				case 1:
					this.Owner.Conquest.ObtainCityLabel(townIdx);
					this.Owner.Words.Append(NewspaperNameSuffixGazetteAddress);
					break;

				case 2:
					Message("The ")
						.City(townIdx)
						.Text(NewspaperNameSuffixTribuneAddress);
					break;

				case 3:
					Message("The ")
						.City(townIdx)
						.Text(NewspaperNameSuffixChronicleAddress);
					break;
			}

			short width = (short)(ushort)this.Owner.TextPainter.ObtainTextWidth(this.Owner.Words.ToString());

			if (width > 0x12c)
			{
				EmptyMessage()
					.City(townIdx)
					.Text(" News");

				width = (short)(ushort)this.Owner.TextPainter.ObtainTextWidth(this.Owner.Words.ToString());
			}

			if (width < 0xc8)
			{
				this.Owner.TextPainter.RenderText(NewspaperLeftPriceLabelAddress, 8, 11, 0);

				this.Owner.TextPainter.RenderText(NewspaperRightPriceLabelAddress, 268, 11, 0);
			}

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 11, Hues.Black);

			this.Owner.Words.Set(this.Owner.Libc.Dice.DrawInt(2) != 0 ? NewspaperMotto1Address : NewspaperMotto2Address);

			DrawingBox.TypefaceIdx = 0x3;

			this.Owner.TextPainter.RenderText(this.Owner.Words.ToString(), 6, 3, 0);

			this.Owner.TextPainter.RenderText(this.Owner.Words.ToString(), 272, 3, 0);

			DrawingBox.TypefaceIdx = 0x1;
			EmptyMessage()
				.Text("10 cents");

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 272, 28, Hues.Black);

			this.Owner.Words.Set("January 1, ");

			this.Owner.TurnHooks.ObtainCurrentYearAsText();

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 8, 28, Hues.Black);

			DrawingBox.TypefaceIdx = 0x2;

			short storyIdx = (short)this.Owner.Libc.Dice.DrawInt(14);
			char letter = (char)('A' + storyIdx);
			string topic = $"*NEWS{letter}";
			this.Owner.Words.Clear();

			this.Owner.BlockText.ImportTextFromFile(NewspaperTextFileNameAddress, topic);

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 3, Hues.Black);

			DrawingBox.TypefaceIdx = 0x1;

			if (shown)
			{
				this.Owner.Dialogs.DisplayMenu(NewspaperMenuPromptAddress, 80, 128, 1);

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayNewspaper");
		}
	}
}
