using System;

namespace Civilization1Remaster.Core.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 23.
	/// </summary>
	public class CityNamingScreen : GameComponent, ICityNamingScreen
	{
		public CityNamingScreen(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public ushort ShowCityLabelPrompt(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCityLabelPrompt({townIdx})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			ushort answer;

			do
			{
				this.Owner.Services.FloodBox(Screen.Box, 80, 80, 160, 32, Hues.White);

				this.Owner.Dialogs.DrawBox(80, 80, 160, 32, Hues.LightCyan);

				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextCityName, 88, 82, Hues.Black);

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

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			return answer;
		}

		/// <inheritdoc/>
		public void ShowFoundCityMessage()
		{
			using var _ = this.Processor.Journal.Scope("DisplayFoundCityMessage()");

			short playerIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 158, 88, 160, 32, Hues.White);

			this.Owner.Dialogs.DrawBox(158, 88, 160, 32, Hues.LightCyan);

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextYourName, 166, 90, Hues.Black);

			this.Owner.Words.Set(Tables.LeaderName(playerIdx));

			PullTextInput(0xa6, 0x68, 0xd);

			this.Owner.Names.Set(Tables.LeaderName(playerIdx), this.Owner.Words.ToString());

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public void ShowCityFoundedMessage()
		{
			using var _ = this.Processor.Journal.Scope("DisplayCityFoundedMessage()");

			short playerIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 158, 88, 160, 32, Hues.White);

			this.Owner.Dialogs.DrawBox(158, 88, 160, 32, Hues.LightCyan);

			this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextNameOfYourTribe, 166, 90, Hues.Black);

			this.Owner.Words.Set(Tables.CivilizationPlural(playerIdx));

			PullTextInput(0xa6, 0x68, 0xb);

			this.Owner.Names.Set(Tables.CivilizationPlural(playerIdx), this.Owner.Words.ToString());

			// The last character (the one before the text when it is empty)
			int lastIndex = this.Owner.Words.Length - 1;

			if (lastIndex >= 0 && this.Owner.Words[lastIndex] == 's')
			{
				this.Owner.Words[lastIndex] = 0;
			}

			this.Owner.Names.Set(Tables.CivilizationName(playerIdx), this.Owner.Words.ToString());
			this.Processor.EmitGlobalByte((ushort)(Tables.CivilizationName(playerIdx) + 0xa), 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public void ShowNewCityLocation()
		{
			using var _ = this.Processor.Journal.Scope("DisplayNewCityLocation()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			ushort townIdx = ShowLabelInputBox(0x53ce);
			short personIdx = this.Owner.World.PersonParticipantIdx;

			if (townIdx != 0xffff &&
				(this.Owner.World.ChartSight[this.Owner.World.Towns[townIdx].Place.X, this.Owner.World.Towns[townIdx].Place.Y] & (1 << personIdx)) != 0)
			{
				this.Owner.MapOps.FocusMapView(personIdx, this.Owner.World.Towns[townIdx].Place.X - 7, this.Owner.World.Towns[townIdx].Place.Y - 6);
			}
			else
			{
				this.Owner.Services.FloodBox(Screen.Box, 64, 78, 224, 10, Hues.White);

				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextUnknownCity, 82, 80, Hues.Black);

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
					Screen.Box, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		/// <inheritdoc/>
		public ushort ShowLabelInputBox(ushort charsAddr)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayLabelInputBox(0x{charsAddr:x4})");

			this.Owner.Services.FloodBox(Screen.Box, 64, 78, 224, 24, Hues.White);

			this.Owner.Dialogs.DrawBox(64, 78, 224, 24, Hues.Black);

			this.Owner.TextPainter.DrawTextToScreen0(charsAddr, 66, 80, Hues.Black);

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

			return found;
		}

		/// <inheritdoc/>
		public ushort PullTextInput(short horizLoc, short vertLoc, ushort breadth)
		{
			using var _ = this.Processor.Journal.Scope($"PullTextInput({horizLoc}, {vertLoc}, {breadth})");



			this.Owner.Services.FloodBox(Screen.Box, horizLoc, vertLoc, (7 * breadth), 11, Hues.White);

			this.Owner.Dialogs.DrawBox(horizLoc, vertLoc - 1, breadth * 8 + 8, 13, Hues.Black);

			DrawingBox.RearHues = 0xf;
			Writing.Cursor = 0x0;

			bool fresh = true;

			for (ushort charIdx = this.Owner.Words.Length; (short)charIdx < (short)breadth; charIdx++)
			{
				this.Owner.Words[charIdx] = 0x20;
			}

			this.Owner.Words[breadth] = 0x0;

			DrawTextInputField(horizLoc, vertLoc, breadth);

			ushort accepted = 1;
			ushort key;

			while (true)
			{
				while (true)
				{
					if ((ushort)this.Owner.Libc.KeyAvailable() != 0 || this.Owner.GlobDb3a != 0)
						break;

					// The cursor blinks
					this.Owner.VideoCard.SubstituteHue(Screen.Box, (short)Writing.CursorColumn, vertLoc + 1,
						(short)Writing.CursorWidth, 10, Hues.White, Hues.LightCyan);

					this.Owner.TextPainter.WaitForTime(10);

					this.Owner.VideoCard.SubstituteHue(Screen.Box, (short)Writing.CursorColumn, vertLoc + 1,
						(short)Writing.CursorWidth, 10, Hues.LightCyan, Hues.White);

					if ((ushort)this.Owner.Libc.KeyAvailable() == 0)
					{
						this.Owner.TextPainter.WaitForTime(10);
					}

					this.Owner.Startup.ReadMouseState();
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

				if (key == 0xcb && Writing.Cursor != 0)
				{
					Writing.Cursor = (ushort)(Writing.Cursor - 1);
				}

				if (key == 0xcd && (short)(breadth - 1) > (short)Writing.Cursor)
				{
					Writing.Cursor = (ushort)(Writing.Cursor + 1);
				}

				if (key == 0xd2)
				{
					string kept = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, (ushort)(Writing.Cursor + TextBuffer.Address)));

					this.Owner.Libc.CopyText((ushort)(Writing.Cursor + (ushort)(TextBuffer.Address + 1)), kept);

					this.Owner.Words[Writing.Cursor] = 0x20;
					this.Owner.Words[breadth] = 0x0;
				}

				if (key == 0xd3)
				{
					this.Owner.Libc.CopyText((ushort)(Writing.Cursor + TextBuffer.Address), (ushort)(Writing.Cursor + (ushort)(TextBuffer.Address + 1)));

					this.Owner.Words[Writing.Cursor] = 0x20;
					this.Owner.Words[breadth - 1] = 0x20;
					this.Owner.Words[breadth] = 0x0;
				}

				if (key == KeyCodes.Backspace && Writing.Cursor != 0)
				{
					this.Owner.Libc.CopyText((ushort)(Writing.Cursor + (ushort)(TextBuffer.Address - 1)), (ushort)(Writing.Cursor + TextBuffer.Address));

					this.Owner.Words[breadth - 1] = 0x20;
					this.Owner.Words[breadth] = 0x0;
					Writing.Cursor = (ushort)(Writing.Cursor - 1);
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

					ushort place = Writing.Cursor;

					this.Owner.Words[place] = (byte)key;

					if ((short)(breadth - 1) > (short)place)
					{
						Writing.Cursor = (ushort)(Writing.Cursor + 1);
					}
				}

				fresh = false;

				DrawTextInputField(horizLoc, vertLoc, breadth);
			}

			short lastIdx = (short)(breadth - 1);

			while (lastIdx > 0 && this.Owner.Words[lastIdx] == 0x20)
			{
				this.Owner.Words[lastIdx] = 0x0;
				lastIdx--;
			}

			this.Owner.TurnLogic.WaitForKey();

			return accepted;
		}

		/// <inheritdoc/>
		public void DrawTextInputField(short horizLoc, short vertLoc, ushort breadth)
		{
			using var _ = this.Processor.Journal.Scope($"RenderTextInputField({horizLoc}, {vertLoc}, {breadth})");

			horizLoc += 2;
			vertLoc += 2;

			for (short place = 0; place < (short)breadth; place++)
			{
				this.Owner.Services.FloodBox(Screen.Box, horizLoc, vertLoc, 8, 8, Hues.White);

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

				if (place == (short)Writing.Cursor)
				{
					Writing.CursorColumn = (ushort)horizLoc;
					Writing.CursorWidth = this.Owner.VideoCard.ObtainCharWidth(typeface, character);
				}

				horizLoc += (short)this.Owner.VideoCard.ObtainCharWidth(typeface, character);
			}
		}
	}
}
