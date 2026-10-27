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
		private const ushort NewspaperTextFileNameAddress = 0x5178;
		private const ushort NewspaperMenuPromptAddress = 0x5181;

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
			this.processor.Journal.OpenScope($"DisplayNewspaper({townIdx})");


			short givenIdx = townIdx;
			bool shown = townIdx != -2;

			if (townIdx < 0)
			{
				for (short cityIdx = 0; cityIdx < 0x80; cityIdx++)
				{
					City town = this.owner.World.Towns[cityIdx];

					if (town.ConditionMarker == 0xff || town.ParticipantIdx != this.owner.World.PersonParticipantIdx)
						continue;

					if (town.Has(CityStructure.Palace) || townIdx == -1)
					{
						townIdx = cityIdx;
					}
				}
			}

			this.owner.Startup.ConcealMouseCursor();

			if (shown)
			{
				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 100, 15);

			DrawingBox.TypefaceIdx = 0x4;

			this.owner.VideoCard.RenderLine(ScreenBox, 1, 1, 318, 1, 0);

			this.owner.Services.FloodBox(ScreenBox, 8, 3, 304, 20, 15);

			DrawingBox.RearHues = 0xf;

			this.owner.VideoCard.RenderLine(ScreenBox, 0, 35, 319, 35, 0);

			this.owner.VideoCard.RenderLine(ScreenBox, 1, 1, 1, 35, 0);

			this.owner.VideoCard.RenderLine(ScreenBox, 318, 1, 318, 35, 0);

			this.owner.VideoCard.RenderLine(ScreenBox, 0, 97, 319, 97, 0);

			if (shown)
			{
				for (short personIdx = 0; personIdx < 0x10; personIdx++)
				{
					this.owner.Services.RenderPictureToScreenAlternate(ScreenBox, 20 * personIdx,
						100 - this.owner.Libc.Dice.DrawInt(2), NewspaperCrowdPicture);
				}
			}

			this.owner.BlockText.RenderTextChunk(40, 16, 40, 0);

			DrawingBox.TypefaceIdx = 0x5;
			this.owner.Words.Clear();

			switch (this.owner.Libc.Dice.DrawInt(4))
			{
				case 0:
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Words.Append(NewspaperNameSuffixTimesAddress);
					break;

				case 1:
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Words.Append(NewspaperNameSuffixGazetteAddress);
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

			short width = (short)(ushort)this.owner.TextPainter.ObtainTextWidth(TextBuffer.Address);

			if (width > 0x12c)
			{
				EmptyMessage()
					.City(townIdx)
					.Text(" News");

				width = (short)(ushort)this.owner.TextPainter.ObtainTextWidth(TextBuffer.Address);
			}

			if (width < 0xc8)
			{
				this.owner.TextPainter.RenderText(NewspaperLeftPriceLabelAddress, 8, 11, 0);

				this.owner.TextPainter.RenderText(NewspaperRightPriceLabelAddress, 268, 11, 0);
			}

			this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 11, 0);

			this.owner.Words.Set(this.owner.Libc.Dice.DrawInt(2) != 0 ? NewspaperMotto1Address : NewspaperMotto2Address);

			DrawingBox.TypefaceIdx = 0x3;

			this.owner.TextPainter.RenderText(TextBuffer.Address, 6, 3, 0);

			this.owner.TextPainter.RenderText(TextBuffer.Address, 272, 3, 0);

			DrawingBox.TypefaceIdx = 0x1;
			EmptyMessage()
				.Text("10 cents");

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 272, 28, 0);

			this.owner.Words.Set("January 1, ");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 8, 28, 0);

			DrawingBox.TypefaceIdx = 0x2;

			short storyIdx = (short)this.owner.Libc.Dice.DrawInt(14);
			this.processor.RegAcc.Value16 = (ushort)storyIdx;
			char letter = (char)('A' + storyIdx);
			ushort topic = (ushort)(this.processor.RegStack.Value16 - 0x300);
			this.owner.Libc.CopyText(topic, $"*NEWS{letter}");
			this.owner.Words.Clear();

			this.owner.BlockText.ImportTextFromFile(NewspaperTextFileNameAddress, topic);

			this.owner.TextPainter.RenderCenteredTextToScreen0(TextBuffer.Address, 160, 3, 0);

			DrawingBox.TypefaceIdx = 0x1;

			if (shown)
			{
				this.owner.Dialogs.DisplayMenu(NewspaperMenuPromptAddress, 80, 128, 1);

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("DisplayNewspaper");
		}
	}
}
