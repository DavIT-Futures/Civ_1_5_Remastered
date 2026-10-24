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
			this.processor.Journal.OpenScope($"DisplayCityLabelPrompt({townIdx})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			ushort answer;

			do
			{
				this.owner.Services.FloodBox(ScreenBox, 80, 80, 160, 32, 15);

				this.owner.Dialogs.RenderBox(80, 80, 160, 32, 11);

				this.owner.TextPainter.RenderTextToScreen0(0x539e, 88, 82, 0);

				EmptyMessage()
					.City(townIdx);

				answer = PullTextInput(0x58, 0x60, 0xc);
			}
			while (this.owner.Libc.TextLength(0xba06) == 0);

			if (answer != 0)
			{
				uint labelIdx = this.owner.World.Towns[townIdx].LabelIdx;
				char[] label = this.owner.World.TownLabels[labelIdx].ToCharArray();

				for (int idx = 0; idx < 0xd; idx++)
				{
					label[idx] = (char)this.processor.PullGlobalByte((ushort)(0xba06 + idx));
				}

				this.owner.World.TownLabels[labelIdx] = new string(label);
			}

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = answer;

			this.processor.Journal.CloseScope("DisplayCityLabelPrompt");

			return answer;
		}

		/// <summary>
		/// Asks the human player for a name in a box with the question at 0x53ab: the answer starts as the text that
		/// 0x19a2 + 2 * player points to, and is written back there
		/// </summary>
		public void DisplayFoundCityMessage()
		{
			this.processor.Journal.OpenScope("DisplayFoundCityMessage()");

			ushort nameSlot = (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x19a2);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 158, 88, 160, 32, 15);

			this.owner.Dialogs.RenderBox(158, 88, 160, 32, 11);

			this.owner.TextPainter.RenderTextToScreen0(0x53ab, 166, 90, 0);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord(nameSlot));

			PullTextInput(0xa6, 0x68, 0xd);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(nameSlot), 0xba06);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayFoundCityMessage");
		}

		/// <summary>
		/// Asks the human player for the name of their civilization in a box with the question at 0x53b8: the plural is typed
		/// (the text that 0x1992 + 2 * player points to); without its final "s" and cut to 10 characters it is also the name
		/// (0x1982 + 2 * player)
		/// </summary>
		public void DisplayCityFoundedMessage()
		{
			this.processor.Journal.OpenScope("DisplayCityFoundedMessage()");

			ushort nameSlot = (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1982);
			ushort pluralSlot = (ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x1992);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 158, 88, 160, 32, 15);

			this.owner.Dialogs.RenderBox(158, 88, 160, 32, 11);

			this.owner.TextPainter.RenderTextToScreen0(0x53b8, 166, 90, 0);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord(pluralSlot));

			PullTextInput(0xa6, 0x68, 0xb);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(pluralSlot), 0xba06);

			// The last character (the one before the text when it is empty)
			ushort lastAddr = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06) - 1);

			if (this.processor.PullGlobalByte(lastAddr) == 's')
			{
				this.processor.EmitGlobalByte(lastAddr, 0);
			}

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(nameSlot), 0xba06);
			this.processor.EmitGlobalByte((ushort)(this.processor.PullGlobalWord(nameSlot) + 0xa), 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayCityFoundedMessage");
		}

		/// <summary>
		/// Asks for the name of a city (0x53ce) and centres the map on it when the human player knows its tile (ChartSight);
		/// otherwise (or when no city is chosen) shows the message 0x53f3 until a key or click. The screen is kept in the back
		/// screen 0x19d4 meanwhile.
		/// </summary>
		public void DisplayNewCityLocation()
		{
			this.processor.Journal.OpenScope("DisplayNewCityLocation()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			ushort townIdx = DisplayLabelInputBox(0x53ce);
			short personIdx = this.owner.World.PersonParticipantIdx;

			if (townIdx != 0xffff &&
				(this.owner.World.ChartSight[this.owner.World.Towns[townIdx].Place.X, this.owner.World.Towns[townIdx].Place.Y] & (1 << personIdx)) != 0)
			{
				this.owner.MapOps.FocusMapView(personIdx, this.owner.World.Towns[townIdx].Place.X - 7, this.owner.World.Towns[townIdx].Place.Y - 6);
			}
			else
			{
				this.owner.Services.FloodBox(ScreenBox, 64, 78, 224, 10, 15);

				this.owner.TextPainter.RenderTextToScreen0(0x53f3, 82, 80, 0);

				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
					ScreenBox, 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayNewCityLocation");
		}

		/// <summary>
		/// Asks for a city name (the question at charsAddr) and looks for it: the first of the 256 city names that starts with
		/// the answer (any case), and the first city with that name; names without a city are skipped. Each name is copied into
		/// a buffer in the stack frame for the comparison, like in the original.
		/// </summary>
		/// <returns>City index, 0xffff when none (also in AX)</returns>
		public ushort DisplayLabelInputBox(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayLabelInputBox(0x{charsAddr:x4})");

			this.processor.EnterFrame(0x14);
			this.processor.PreserveSource();

			this.owner.Services.FloodBox(ScreenBox, 64, 78, 224, 24, 15);

			this.owner.Dialogs.RenderBox(64, 78, 224, 24, 0);

			this.owner.TextPainter.RenderTextToScreen0(charsAddr, 66, 80, 0);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			PullTextInput(0x42, 0x59, 0x10);

			ushort label = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			ushort found = 0xffff;

			for (int labelIdx = 0; labelIdx < 0x100 && found == 0xffff; labelIdx++)
			{
				for (int idx = 0; idx < 13; idx++)
				{
					this.processor.Storage.EmitUInt8(this.processor.SegStack.Value16, (ushort)(label + idx), this.owner.World.TownLabels[labelIdx][idx]);
				}

				if (this.owner.Libc.CompareTextPrefixNoCase(0xba06, label, this.owner.Libc.TextLength(0xba06)) != 0)
					continue;

				for (int townIdx = 0; townIdx < 0x80; townIdx++)
				{
					City town = this.owner.World.Towns[townIdx];

					if (town.ConditionMarker != 0xff && town.LabelIdx == labelIdx)
					{
						found = (ushort)townIdx;
						break;
					}
				}
			}

			this.processor.RegAcc.Value16 = found;

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayLabelInputBox");

			return found;
		}

		/// <summary>
		/// Edits the text at 0xba06 in a field of breadth characters (padded with spaces): the cursor (0x68cc) blinks until a
		/// key or a click; Left / Right move it, Insert and Delete shift the rest, Backspace deletes before it, a character
		/// overwrites (the first one typed clears the field). Return or a click accepts, Escape cancels; the trailing spaces
		/// are cut off.
		/// </summary>
		/// <returns>1 when accepted, 0 when cancelled, also in AX</returns>
		public ushort PullTextInput(short horizLoc, short vertLoc, ushort breadth)
		{
			this.processor.Journal.OpenScope($"PullTextInput({horizLoc}, {vertLoc}, {breadth})");

			// The text moved by Insert is kept in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x48);
			this.processor.PreserveSource();

			ushort kept = (ushort)(this.processor.RegFrame.Value16 - 0x40);

			this.owner.Services.FloodBox(ScreenBox, horizLoc, vertLoc, (7 * breadth), 11, 15);

			this.owner.Dialogs.RenderBox(horizLoc, vertLoc - 1, breadth * 8 + 8, 13, 0);

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0xe), 0xf);
			this.processor.EmitGlobalWord(0x68cc, 0x0);

			bool fresh = true;

			for (ushort charIdx = this.owner.Libc.TextLength(0xba06); (short)charIdx < (short)breadth; charIdx++)
			{
				this.processor.EmitGlobalByte((ushort)(charIdx + 0xba06), 0x20);
			}

			this.processor.EmitGlobalByte((ushort)(breadth + 0xba06), 0x0);

			RenderTextInputField(horizLoc, vertLoc, breadth);

			ushort accepted = 1;
			ushort key;

			while (true)
			{
				while (true)
				{
					if ((ushort)this.owner.Libc.KeyAvailable() != 0 || this.owner.GlobDb3a != 0)
						break;

					// The cursor blinks
					this.owner.VideoCard.SubstituteHue(ScreenBox, this.processor.PullGlobalSignedWord(0x68d0), vertLoc + 1,
						this.processor.PullGlobalSignedWord(0x68ce), 10, 15, 11);

					this.owner.TextPainter.AwaitTime(10);

					this.owner.VideoCard.SubstituteHue(ScreenBox, this.processor.PullGlobalSignedWord(0x68d0), vertLoc + 1,
						this.processor.PullGlobalSignedWord(0x68ce), 10, 11, 15);

					if ((ushort)this.owner.Libc.KeyAvailable() == 0)
					{
						this.owner.TextPainter.AwaitTime(10);
					}

					this.owner.Startup.ObtainMouseState();
				}

				if (this.owner.GlobDb3a != 0)
				{
					key = 0xd;
				}
				else
				{
					key = (ushort)this.owner.Libc.ReadKey();
				}

				if (key == 0)
				{
					key = (ushort)((ushort)this.owner.Libc.ReadKey() + 0x80);
				}

				if (key == 0xd)
					break;

				if (key == 0x1b)
				{
					accepted = 0;
					break;
				}

				if (key == 0xcb && this.processor.PullGlobalWord(0x68cc) != 0)
				{
					this.processor.EmitGlobalWord(0x68cc, (ushort)(this.processor.PullGlobalWord(0x68cc) - 1));
				}

				if (key == 0xcd && (short)(breadth - 1) > (short)this.processor.PullGlobalWord(0x68cc))
				{
					this.processor.EmitGlobalWord(0x68cc, (ushort)(this.processor.PullGlobalWord(0x68cc) + 1));
				}

				if (key == 0xd2)
				{
					this.owner.Libc.CopyText(kept, (ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06));

					this.owner.Libc.CopyText((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba07), kept);

					this.processor.EmitGlobalByte((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06), 0x20);
					this.processor.EmitGlobalByte((ushort)(breadth + 0xba06), 0x0);
				}

				if (key == 0xd3)
				{
					this.owner.Libc.CopyText((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06), (ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba07));

					this.processor.EmitGlobalByte((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06), 0x20);
					this.processor.EmitGlobalByte((ushort)(breadth + 0xba05), 0x20);
					this.processor.EmitGlobalByte((ushort)(breadth + 0xba06), 0x0);
				}

				if (key == 0x8 && this.processor.PullGlobalWord(0x68cc) != 0)
				{
					this.owner.Libc.CopyText((ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba05), (ushort)(this.processor.PullGlobalWord(0x68cc) + 0xba06));

					this.processor.EmitGlobalByte((ushort)(breadth + 0xba05), 0x20);
					this.processor.EmitGlobalByte((ushort)(breadth + 0xba06), 0x0);
					this.processor.EmitGlobalWord(0x68cc, (ushort)(this.processor.PullGlobalWord(0x68cc) - 1));
				}

				if ((short)key < 0x80 && (short)key >= 0x20)
				{
					if (fresh)
					{
						for (ushort charIdx = 0; (short)charIdx < (short)breadth; charIdx++)
						{
							this.processor.EmitGlobalByte((ushort)(charIdx + 0xba06), 0x20);
						}
					}

					ushort place = this.processor.PullGlobalWord(0x68cc);

					this.processor.EmitGlobalByte((ushort)(place + 0xba06), (byte)key);

					if ((short)(breadth - 1) > (short)place)
					{
						this.processor.EmitGlobalWord(0x68cc, (ushort)(this.processor.PullGlobalWord(0x68cc) + 1));
					}
				}

				fresh = false;

				RenderTextInputField(horizLoc, vertLoc, breadth);
			}

			short lastIdx = (short)(breadth - 1);

			while (lastIdx > 0 && this.processor.PullGlobalByte((ushort)(lastIdx + 0xba06)) == 0x20)
			{
				this.processor.EmitGlobalByte((ushort)(lastIdx + 0xba06), 0x0);
				lastIdx--;
			}

			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegAcc.Value16 = accepted;
			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			// Far return
			this.processor.Journal.CloseScope("PullTextInput");

			return accepted;
		}
		
		/// <summary>
		/// Draws the text of an input field (0xba06) character by character on white boxes of 8 x 8, the field the width in
		/// characters; at the cursor (0x68cc) the column and the width of its character go to 0x68d0 and 0x68ce
		/// </summary>
		public void RenderTextInputField(short horizLoc, short vertLoc, ushort breadth)
		{
			this.processor.Journal.OpenScope($"RenderTextInputField({horizLoc}, {vertLoc}, {breadth})");

			horizLoc += 2;
			vertLoc += 2;

			for (short place = 0; place < (short)breadth; place++)
			{
				this.owner.Services.FloodBox(ScreenBox, horizLoc, vertLoc, 8, 8, 15);

				if ((short)this.owner.Libc.TextLength(0xba06) <= place)
					continue;

				// The character alone: the next one is cut off for the drawing
				ushort next = (ushort)(place + 0xba07);
				byte cut = this.processor.PullGlobalByte(next);
				this.processor.EmitGlobalByte(next, 0x0);

				this.owner.TextPainter.RenderText((ushort)(0xba06 + place), horizLoc, vertLoc, 0);

				this.processor.EmitGlobalByte(next, cut);

				ushort typeface = this.processor.PullGlobalWord((ushort)(ScreenBox + 0x10));
				byte character = this.processor.PullGlobalByte((ushort)(place + 0xba06));

				if (place == (short)this.processor.PullGlobalWord(0x68cc))
				{
					this.processor.EmitGlobalWord(0x68d0, (ushort)horizLoc);
					this.processor.EmitGlobalWord(0x68ce, this.owner.VideoCard.ObtainCharWidth(typeface, character));
				}

				horizLoc += (short)this.owner.VideoCard.ObtainCharWidth(typeface, character);
			}

			this.processor.Journal.CloseScope("RenderTextInputField");
		}
	}
}
