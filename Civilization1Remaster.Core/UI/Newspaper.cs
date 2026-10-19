using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 21.
	/// </summary>
	public class Newspaper : GameModule
	{
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

			// The topic of the story is in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x10);
			this.processor.PreserveSource();

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

				// The search leaves 0x1c * 127 in SI, and DisplayMenu leaves SI in DI, which goes back to the caller
				this.processor.RegSource.Value16 = 0x1c * 0x7f;
			}

			this.owner.Startup.ConcealMouseCursor();

			if (shown)
			{
				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);
			}

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 100, 15);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x4);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 1, 1, 318, 1, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 8, 3, 304, 20, 15);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0xe), 0xf);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 35, 319, 35, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 1, 1, 1, 35, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 318, 1, 318, 35, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 97, 319, 97, 0);

			if (shown)
			{
				for (short personIdx = 0; personIdx < 0x10; personIdx++)
				{
					this.owner.Services.RenderPictureToScreenAlternate(this.processor.PullGlobalWord(0xaa), 20 * personIdx,
						100 - this.owner.Libc.Dice.DrawInt(2), this.processor.PullGlobalWord(0xdf0c));
				}
			}

			this.owner.BlockText.RenderTextChunk(40, 16, 40, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x5);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			switch (this.owner.Libc.Dice.DrawInt(4))
			{
				case 0:
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, 0x5118);
					break;

				case 1:
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, 0x5120);
					break;

				case 2:
					this.owner.Libc.CopyText(0xba06, "The ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, 0x512c);
					break;

				case 3:
					this.owner.Libc.CopyText(0xba06, "The ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, 0x5138);
					break;
			}

			this.owner.TextPainter.ObtainTextWidth(0xba06);

			short width = (short)this.processor.RegAcc.Value16;

			if (width > 0x12c)
			{
				this.processor.EmitGlobalByte(0xba06, 0x0);

				this.owner.Conquest.ObtainCityLabel(townIdx);

				this.owner.Libc.AppendText(0xba06, " News");

				this.owner.TextPainter.ObtainTextWidth(0xba06);

				width = (short)this.processor.RegAcc.Value16;
			}

			if (width < 0xc8)
			{
				this.owner.TextPainter.RenderText(0x5147, 8, 11, 0);

				this.owner.TextPainter.RenderText(0x514b, 268, 11, 0);
			}

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 11, 0);

			this.owner.Libc.CopyText(0xba06, this.owner.Libc.Dice.DrawInt(2) != 0 ? (ushort)0x514f : (ushort)0x5156);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x3);

			this.owner.TextPainter.RenderText(0xba06, 6, 3, 0);

			this.owner.TextPainter.RenderText(0xba06, 272, 3, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06, "10 cents");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 272, 28, 0);

			this.owner.Libc.CopyText(0xba06, "January 1, ");

			this.owner.TurnHooks.ObtainCurrentYearAsText();

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, 28, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x2);

			ushort topic = (ushort)(this.processor.RegFrame.Value16 - 0xc);

			this.owner.Libc.CopyText(topic, "*NEWSA");

			this.processor.EmitLocalByte(0x7, (byte)(this.processor.PullLocalByte(0x7) + this.owner.Libc.Dice.DrawInt(14)));
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.ImportTextFromFile(0x5178, topic);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 3, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

			if (shown)
			{
				this.owner.Dialogs.DisplayMenu(0x5181, 80, 128, 1);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitLocalWord(0x4, (ushort)givenIdx);
			this.processor.EmitLocalWord(0xe, (ushort)width);

			if (givenIdx < 0)
			{
				this.processor.EmitLocalWord(0x2, 0x80);
			}

			if (shown)
			{
				this.processor.EmitLocalWord(0x10, 0x10);
			}

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayNewspaper");
		}
	}
}
