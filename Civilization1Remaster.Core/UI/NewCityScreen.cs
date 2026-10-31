using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 23.
	/// </summary>
	public class NewCityScreen : GameModule
	{
		public NewCityScreen(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Asks for the name of the city (question 0x539e, starting from its name) until the answer is not empty; when the
		/// answer is accepted (PullTextInput not 0) its 13 characters become the city's name. The screen is kept in the back
		/// screen 0x19d4 meanwhile.
		/// </summary>
		/// <returns>The answer of PullTextInput, also in AX</returns>
		public ushort DisplayCityLabelPrompt(short townIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayCityLabelPrompt({townIdx})");

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			ushort answer;

			do
			{
				this.Owner.Services.FloodBox(ScreenBox, 80, 80, 160, 32, Hues.White);

				this.Owner.Dialogs.RenderBox(80, 80, 160, 32, Hues.LightCyan);

				this.Owner.TextPainter.RenderTextToScreen0(GameTexts.TextCityName, 88, 82, Hues.Black);

				EmptyMessage()
					.City(townIdx);

				answer = PullTextInput(0x58, 0x60, 0xc);
			}
			while (this.Owner.Words.Length == 0);

			if (answer != 0)
			{
				uint labelIdx = this.Owner.World.Towns[townIdx].LabelIdx;
				char[] label = this.Owner.World.TownLabels[labelIdx].ToCharArray();

				for (int idx = 0; idx < 0xd; idx++)
				{
					label[idx] = (char)this.Owner.Words[idx];
				}

				this.Owner.World.TownLabels[labelIdx] = new string(label);
			}

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayCityLabelPrompt");

			return answer;
		}

		/// <summary>
		/// Asks the human player for a name in a box with the question at 0x53ab: the answer starts as the text that
		/// 0x19a2 + 2 * player points to, and is written back there
		/// </summary>
		public void DisplayFoundCityMessage()
		{
			this.Processor.Journal.OpenScope("DisplayFoundCityMessage()");

			short playerIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 158, 88, 160, 32, Hues.White);

			this.Owner.Dialogs.RenderBox(158, 88, 160, 32, Hues.LightCyan);

			this.Owner.TextPainter.RenderTextToScreen0(GameTexts.TextYourName, 166, 90, Hues.Black);

			this.Owner.Words.Set(LeaderName(playerIdx));

			PullTextInput(0xa6, 0x68, 0xd);

			this.Owner.Names.Set(LeaderName(playerIdx), this.Owner.Words.ToString());

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayFoundCityMessage");
		}

		/// <summary>
		/// Asks the human player for the name of their civilization in a box with the question at 0x53b8: the plural is typed
		/// (the text that 0x1992 + 2 * player points to); without its final "s" and cut to 10 characters it is also the name
		/// (0x1982 + 2 * player)
		/// </summary>
		public void DisplayCityFoundedMessage()
		{
			this.Processor.Journal.OpenScope("DisplayCityFoundedMessage()");

			short playerIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 158, 88, 160, 32, Hues.White);

			this.Owner.Dialogs.RenderBox(158, 88, 160, 32, Hues.LightCyan);

			this.Owner.TextPainter.RenderTextToScreen0(GameTexts.TextNameOfYourTribe, 166, 90, Hues.Black);

			this.Owner.Words.Set(CivilizationPlural(playerIdx));

			PullTextInput(0xa6, 0x68, 0xb);

			this.Owner.Names.Set(CivilizationPlural(playerIdx), this.Owner.Words.ToString());

			// The last character (the one before the text when it is empty)
			int lastIndex = this.Owner.Words.Length - 1;

			if (lastIndex >= 0 && this.Owner.Words[lastIndex] == 's')
			{
				this.Owner.Words[lastIndex] = 0;
			}

			this.Owner.Names.Set(CivilizationName(playerIdx), this.Owner.Words.ToString());
			this.Processor.EmitGlobalByte((ushort)(CivilizationName(playerIdx) + 0xa), 0);

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayCityFoundedMessage");
		}

		/// <summary>
		/// Asks for the name of a city (0x53ce) and centres the map on it when the human player knows its tile (ChartSight);
		/// otherwise (or when no city is chosen) shows the message 0x53f3 until a key or click. The screen is kept in the back
		/// screen 0x19d4 meanwhile.
		/// </summary>
		public void DisplayNewCityLocation()
		{
			this.Processor.Journal.OpenScope("DisplayNewCityLocation()");

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			ushort townIdx = DisplayLabelInputBox(0x53ce);
			short personIdx = this.Owner.World.PersonParticipantIdx;

			if (townIdx != 0xffff &&
				(this.Owner.World.ChartSight[this.Owner.World.Towns[townIdx].Place.X, this.Owner.World.Towns[townIdx].Place.Y] & (1 << personIdx)) != 0)
			{
				this.Owner.MapOps.FocusMapView(personIdx, this.Owner.World.Towns[townIdx].Place.X - 7, this.Owner.World.Towns[townIdx].Place.Y - 6);
			}
			else
			{
				this.Owner.Services.FloodBox(ScreenBox, 64, 78, 224, 10, Hues.White);

				this.Owner.TextPainter.RenderTextToScreen0(GameTexts.TextUnknownCity, 82, 80, Hues.Black);

				this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayNewCityLocation");
		}

		/// <summary>
		/// Asks for a city name (the question at charsAddr) and looks for it: the first of the 256 city names that starts with
		/// the answer (any case), and the first city with that name; names without a city are skipped. Each name is copied into
		/// a buffer in the stack frame for the comparison, like in the original.
		/// </summary>
		/// <returns>City index, 0xffff when none (also in AX)</returns>
		public ushort DisplayLabelInputBox(ushort charsAddr)
		{
			this.Processor.Journal.OpenScope($"DisplayLabelInputBox(0x{charsAddr:x4})");

			this.Owner.Services.FloodBox(ScreenBox, 64, 78, 224, 24, Hues.White);

			this.Owner.Dialogs.RenderBox(64, 78, 224, 24, Hues.Black);

			this.Owner.TextPainter.RenderTextToScreen0(charsAddr, 66, 80, Hues.Black);

			this.Owner.Words.Clear();

			PullTextInput(0x42, 0x59, 0x10);

			string input = this.Owner.Words.ToString();
			int inputLen = this.Owner.Words.Length;
			ushort found = 0xffff;

			for (int labelIdx = 0; labelIdx < 0x100 && found == 0xffff; labelIdx++)
			{
				string labelText = this.Owner.World.TownLabels[labelIdx];
				string prefix = labelText.Length > inputLen ? labelText.Substring(0, inputLen) : labelText;
				if (string.Compare(input, prefix, StringComparison.CurrentCultureIgnoreCase) != 0)
					continue;

				for (int townIdx = 0; townIdx < 0x80; townIdx++)
				{
					City town = this.Owner.World.Towns[townIdx];

					if (town.ConditionMarker != 0xff && town.LabelIdx == labelIdx)
					{
						found = (ushort)townIdx;
						break;
					}
				}
			}

			this.Processor.Journal.CloseScope("DisplayLabelInputBox");

			return found;
		}

		/// <summary>
		/// Edits the text at TextBuffer.Address in a field of breadth characters (padded with spaces): the cursor (0x68cc) blinks until a
		/// key or a click; Left / Right move it, Insert and Delete shift the rest, Backspace deletes before it, a character
		/// overwrites (the first one typed clears the field). Return or a click accepts, Escape cancels; the trailing spaces
		/// are cut off.
		/// </summary>
		/// <returns>1 when accepted, 0 when cancelled, also in AX</returns>
		public ushort PullTextInput(short horizLoc, short vertLoc, ushort breadth)
		{
			this.Processor.Journal.OpenScope($"PullTextInput({horizLoc}, {vertLoc}, {breadth})");



			this.Owner.Services.FloodBox(ScreenBox, horizLoc, vertLoc, (7 * breadth), 11, Hues.White);

			this.Owner.Dialogs.RenderBox(horizLoc, vertLoc - 1, breadth * 8 + 8, 13, Hues.Black);

			DrawingBox.RearHues = 0xf;
			TextCursor = 0x0;

			bool fresh = true;

			for (ushort charIdx = this.Owner.Words.Length; (short)charIdx < (short)breadth; charIdx++)
			{
				this.Owner.Words[charIdx] = 0x20;
			}

			this.Owner.Words[breadth] = 0x0;

			RenderTextInputField(horizLoc, vertLoc, breadth);

			ushort accepted = 1;
			ushort key;

			while (true)
			{
				while (true)
				{
					if ((ushort)this.Owner.Libc.KeyAvailable() != 0 || this.Owner.GlobDb3a != 0)
						break;

					// The cursor blinks
					this.Owner.VideoCard.SubstituteHue(ScreenBox, (short)TextCursorColumn, vertLoc + 1,
						(short)TextCursorWidth, 10, Hues.White, Hues.LightCyan);

					this.Owner.TextPainter.AwaitTime(10);

					this.Owner.VideoCard.SubstituteHue(ScreenBox, (short)TextCursorColumn, vertLoc + 1,
						(short)TextCursorWidth, 10, Hues.LightCyan, Hues.White);

					if ((ushort)this.Owner.Libc.KeyAvailable() == 0)
					{
						this.Owner.TextPainter.AwaitTime(10);
					}

					this.Owner.Startup.ObtainMouseState();
				}

				if (this.Owner.GlobDb3a != 0)
				{
					key = 0xd;
				}
				else
				{
					key = (ushort)this.Owner.Libc.ReadKey();
				}

				if (key == 0)
				{
					key = (ushort)((ushort)this.Owner.Libc.ReadKey() + 0x80);
				}

				if (key == KeyCodes.Enter)
					break;

				if (key == KeyCodes.Escape)
				{
					accepted = 0;
					break;
				}

				if (key == 0xcb && TextCursor != 0)
				{
					TextCursor = (ushort)(TextCursor - 1);
				}

				if (key == 0xcd && (short)(breadth - 1) > (short)TextCursor)
				{
					TextCursor = (ushort)(TextCursor + 1);
				}

				if (key == 0xd2)
				{
					string kept = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, (ushort)(TextCursor + TextBuffer.Address)));

					this.Owner.Libc.CopyText((ushort)(TextCursor + (ushort)(TextBuffer.Address + 1)), kept);

					this.Owner.Words[TextCursor] = 0x20;
					this.Owner.Words[breadth] = 0x0;
				}

				if (key == 0xd3)
				{
					this.Owner.Libc.CopyText((ushort)(TextCursor + TextBuffer.Address), (ushort)(TextCursor + (ushort)(TextBuffer.Address + 1)));

					this.Owner.Words[TextCursor] = 0x20;
					this.Owner.Words[breadth - 1] = 0x20;
					this.Owner.Words[breadth] = 0x0;
				}

				if (key == KeyCodes.Backspace && TextCursor != 0)
				{
					this.Owner.Libc.CopyText((ushort)(TextCursor + (ushort)(TextBuffer.Address - 1)), (ushort)(TextCursor + TextBuffer.Address));

					this.Owner.Words[breadth - 1] = 0x20;
					this.Owner.Words[breadth] = 0x0;
					TextCursor = (ushort)(TextCursor - 1);
				}

				if ((short)key < 0x80 && (short)key >= 0x20)
				{
					if (fresh)
					{
						for (ushort charIdx = 0; (short)charIdx < (short)breadth; charIdx++)
						{
							this.Owner.Words[charIdx] = 0x20;
						}
					}

					ushort place = TextCursor;

					this.Owner.Words[place] = (byte)key;

					if ((short)(breadth - 1) > (short)place)
					{
						TextCursor = (ushort)(TextCursor + 1);
					}
				}

				fresh = false;

				RenderTextInputField(horizLoc, vertLoc, breadth);
			}

			short lastIdx = (short)(breadth - 1);

			while (lastIdx > 0 && this.Owner.Words[lastIdx] == 0x20)
			{
				this.Owner.Words[lastIdx] = 0x0;
				lastIdx--;
			}

			this.Owner.TurnLogic.AwaitForKey();

			this.Processor.Journal.CloseScope("PullTextInput");

			return accepted;
		}

		/// <summary>
		/// Draws the text of an input field (TextBuffer.Address) character by character on white boxes of 8 x 8, the field the width in
		/// characters; at the cursor (0x68cc) the column and the width of its character go to 0x68d0 and 0x68ce
		/// </summary>
		public void RenderTextInputField(short horizLoc, short vertLoc, ushort breadth)
		{
			this.Processor.Journal.OpenScope($"RenderTextInputField({horizLoc}, {vertLoc}, {breadth})");

			horizLoc += 2;
			vertLoc += 2;

			for (short place = 0; place < (short)breadth; place++)
			{
				this.Owner.Services.FloodBox(ScreenBox, horizLoc, vertLoc, 8, 8, Hues.White);

				if ((short)this.Owner.Words.Length <= place)
					continue;

				// The character alone: the next one is cut off for the drawing
				ushort next = (ushort)(place + 1);
				byte cut = this.Owner.Words[next];
				this.Owner.Words[next] = 0x0;

				this.Owner.TextPainter.RenderText((ushort)(TextBuffer.Address + place), horizLoc, vertLoc, 0);

				this.Owner.Words[next] = cut;

				ushort typeface = DrawingBox.TypefaceIdx;
				byte character = this.Owner.Words[place];

				if (place == (short)TextCursor)
				{
					TextCursorColumn = (ushort)horizLoc;
					TextCursorWidth = this.Owner.VideoCard.ObtainCharWidth(typeface, character);
				}

				horizLoc += (short)this.Owner.VideoCard.ObtainCharWidth(typeface, character);
			}

			this.Processor.Journal.CloseScope("RenderTextInputField");
		}
	}
}
