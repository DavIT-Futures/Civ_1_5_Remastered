using System;

namespace Civ_1_5_Remastered.Core.UI.CityScreens
{
	public class CityNamingScreen : GameComponent, ICityNamingScreen
	{
		public CityNamingScreen(IGameEngine owner)
			: base(owner)
		{
		}

		public ushort ShowCityLabelPrompt(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCityLabelPrompt({townIndex})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			ushort answer;

			do
			{
				this.Owner.Services.FloodBox(Screen.Box, 80, 80, 160, 32, Hues.White);

				this.Owner.Dialogs.DrawBox(80, 80, 160, 32, Hues.LightCyan);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextCityName, 88, 82, Hues.Black);

				EmptyMessage()
					.City(townIndex);

				answer = PullTextInput(0x58, 0x60, 0xc);
			}
			while (this.Owner.Words.Length == 0);

			if (answer != 0)
			{
				uint labelIndex = this.Owner.World.Towns[townIndex].LabelIndex;
				char[] label = this.Owner.World.TownLabels[labelIndex].ToCharArray();

				for (int index = 0; index < 0xd; index++)
				{
					label[index] = (char)this.Owner.Words[index];
				}

				this.Owner.World.TownLabels[labelIndex] = new string(label);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			return answer;
		}

		public void ShowFoundCityMessage()
		{
			using var _ = this.Processor.Journal.Scope("DisplayFoundCityMessage()");

			short playerIndex = this.Owner.World.PersonParticipantIndex;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 158, 88, 160, 32, Hues.White);

			this.Owner.Dialogs.DrawBox(158, 88, 160, 32, Hues.LightCyan);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextYourName, 166, 90, Hues.Black);

			this.Owner.Words.Set(Tables.LeaderName(playerIndex));

			PullTextInput(0xa6, 0x68, 0xd);

			this.Owner.Names.Set(Tables.LeaderName(playerIndex), this.Owner.Words.ToString());

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowCityFoundedMessage()
		{
			using var _ = this.Processor.Journal.Scope("DisplayCityFoundedMessage()");

			short playerIndex = this.Owner.World.PersonParticipantIndex;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 158, 88, 160, 32, Hues.White);

			this.Owner.Dialogs.DrawBox(158, 88, 160, 32, Hues.LightCyan);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextNameOfYourTribe, 166, 90, Hues.Black);

			this.Owner.Words.Set(Tables.CivilizationPlural(playerIndex));

			PullTextInput(0xa6, 0x68, 0xb);

			this.Owner.Names.Set(Tables.CivilizationPlural(playerIndex), this.Owner.Words.ToString());

			int lastIndex = this.Owner.Words.Length - 1;

			if (lastIndex >= 0 && this.Owner.Words[lastIndex] == 's')
			{
				this.Owner.Words[lastIndex] = 0;
			}

			this.Owner.Names.Set(Tables.CivilizationName(playerIndex), this.Owner.Words.ToString());
			this.Processor.EmitGlobalByte((ushort)(Tables.CivilizationName(playerIndex) + 0xa), 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowNewCityLocation()
		{
			using var _ = this.Processor.Journal.Scope("DisplayNewCityLocation()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			ushort townIndex = ShowLabelInputBox(0x53ce);
			short personIndex = this.Owner.World.PersonParticipantIndex;

			if (townIndex != 0xffff &&
				(this.Owner.World.ChartSight[this.Owner.World.Towns[townIndex].Place.X, this.Owner.World.Towns[townIndex].Place.Y] & (1 << personIndex)) != 0)
			{
				this.Owner.MapOperations.FocusMapView(personIndex, this.Owner.World.Towns[townIndex].Place.X - Viewport.MiddleColumn, this.Owner.World.Towns[townIndex].Place.Y - Viewport.MiddleRow);
			}
			else
			{
				this.Owner.Services.FloodBox(Screen.Box, 64, 78, 224, 10, Hues.White);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextUnknownCity, 82, 80, Hues.Black);

				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
					Screen.Box, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public ushort ShowLabelInputBox(ushort charactersAddress)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayLabelInputBox(0x{charactersAddress:x4})");

			this.Owner.Services.FloodBox(Screen.Box, 64, 78, 224, 24, Hues.White);

			this.Owner.Dialogs.DrawBox(64, 78, 224, 24, Hues.Black);

			this.Owner.TextPainter.DrawTextToPrimaryScreen(charactersAddress, 66, 80, Hues.Black);

			this.Owner.Words.Clear();

			PullTextInput(0x42, 0x59, 0x10);

			string input = this.Owner.Words.ToString();
			int inputLength = this.Owner.Words.Length;
			ushort found = 0xffff;

			for (int labelIndex = 0; labelIndex < 0x100 && found == 0xffff; labelIndex++)
			{
				string labelText = this.Owner.World.TownLabels[labelIndex];
				string prefix = labelText.Length > inputLength ? labelText.Substring(0, inputLength) : labelText;
				if (string.Compare(input, prefix, StringComparison.CurrentCultureIgnoreCase) != 0)
				{
					continue;
				}

				for (int townIndex = 0; townIndex < 0x80; townIndex++)
				{
					City town = this.Owner.World.Towns[townIndex];

					if (town.ConditionMarker != 0xff && town.LabelIndex == labelIndex)
					{
						found = (ushort)townIndex;
						break;
					}
				}
			}

			return found;
		}

		public ushort PullTextInput(short horizontalLocation, short verticalLocation, ushort breadth)
		{
			using var _ = this.Processor.Journal.Scope($"PullTextInput({horizontalLocation}, {verticalLocation}, {breadth})");

			this.Owner.Services.FloodBox(Screen.Box, horizontalLocation, verticalLocation, 7 * breadth, 11, Hues.White);

			this.Owner.Dialogs.DrawBox(horizontalLocation, verticalLocation - 1, (breadth * 8) + 8, 13, Hues.Black);

			DrawingBox.RearHues = 0xf;
			Writing.Cursor = 0x0;

			bool fresh = true;

			for (ushort characterIndex = this.Owner.Words.Length; (short)characterIndex < (short)breadth; characterIndex++)
			{
				this.Owner.Words[characterIndex] = 0x20;
			}

			this.Owner.Words[breadth] = 0x0;

			DrawTextInputField(horizontalLocation, verticalLocation, breadth);

			ushort accepted = 1;
			ushort key;

			while (true)
			{
				while (true)
				{
					if ((ushort)this.Owner.Runtime.KeyAvailable() != 0 || this.Owner.MenuPointerState != 0)
					{
						break;
					}

					this.Owner.VideoCard.SubstituteHue(Screen.Box, (short)Writing.CursorColumn, verticalLocation + 1,
						(short)Writing.CursorWidth, 10, Hues.White, Hues.LightCyan);

					this.Owner.TextPainter.WaitForTime(10);

					this.Owner.VideoCard.SubstituteHue(Screen.Box, (short)Writing.CursorColumn, verticalLocation + 1,
						(short)Writing.CursorWidth, 10, Hues.LightCyan, Hues.White);

					if ((ushort)this.Owner.Runtime.KeyAvailable() == 0)
					{
						this.Owner.TextPainter.WaitForTime(10);
					}

					this.Owner.Startup.ReadMouseState();
				}

				if (this.Owner.MenuPointerState != 0)
				{
					key = 0xd;
				}
				else
				{
					key = (ushort)this.Owner.Runtime.ReadKey();
				}

				if (key == 0)
				{
					key = (ushort)((ushort)this.Owner.Runtime.ReadKey() + 0x80);
				}

				if (key == KeyCodes.Enter)
				{
					break;
				}

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
					string kept = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, (ushort)(Writing.Cursor + TextBuffer.Address)));

					this.Owner.Runtime.CopyText((ushort)(Writing.Cursor + (ushort)(TextBuffer.Address + 1)), kept);

					this.Owner.Words[Writing.Cursor] = 0x20;
					this.Owner.Words[breadth] = 0x0;
				}

				if (key == 0xd3)
				{
					this.Owner.Runtime.CopyText((ushort)(Writing.Cursor + TextBuffer.Address), (ushort)(Writing.Cursor + (ushort)(TextBuffer.Address + 1)));

					this.Owner.Words[Writing.Cursor] = 0x20;
					this.Owner.Words[breadth - 1] = 0x20;
					this.Owner.Words[breadth] = 0x0;
				}

				if (key == KeyCodes.Backspace && Writing.Cursor != 0)
				{
					this.Owner.Runtime.CopyText((ushort)(Writing.Cursor + (ushort)(TextBuffer.Address - 1)), (ushort)(Writing.Cursor + TextBuffer.Address));

					this.Owner.Words[breadth - 1] = 0x20;
					this.Owner.Words[breadth] = 0x0;
					Writing.Cursor = (ushort)(Writing.Cursor - 1);
				}

				if ((short)key < 0x80 && (short)key >= 0x20)
				{
					if (fresh)
					{
						for (ushort characterIndex = 0; (short)characterIndex < (short)breadth; characterIndex++)
						{
							this.Owner.Words[characterIndex] = 0x20;
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

				DrawTextInputField(horizontalLocation, verticalLocation, breadth);
			}

			short lastIndex = (short)(breadth - 1);

			while (lastIndex > 0 && this.Owner.Words[lastIndex] == 0x20)
			{
				this.Owner.Words[lastIndex] = 0x0;
				lastIndex--;
			}

			this.Owner.TurnLogic.WaitForKey();

			return accepted;
		}

		public void DrawTextInputField(short horizontalLocation, short verticalLocation, ushort breadth)
		{
			using var _ = this.Processor.Journal.Scope($"RenderTextInputField({horizontalLocation}, {verticalLocation}, {breadth})");

			horizontalLocation += 2;
			verticalLocation += 2;

			for (short place = 0; place < (short)breadth; place++)
			{
				this.Owner.Services.FloodBox(Screen.Box, horizontalLocation, verticalLocation, 8, 8, Hues.White);

				if ((short)this.Owner.Words.Length <= place)
				{
					continue;
				}

				ushort next = (ushort)(place + 1);
				byte cut = this.Owner.Words[next];
				this.Owner.Words[next] = 0x0;

				this.Owner.TextPainter.RenderText((ushort)(TextBuffer.Address + place), horizontalLocation, verticalLocation, 0);

				this.Owner.Words[next] = cut;

				ushort typeface = DrawingBox.TypefaceIndex;
				byte character = this.Owner.Words[place];

				if (place == (short)Writing.Cursor)
				{
					Writing.CursorColumn = (ushort)horizontalLocation;
					Writing.CursorWidth = this.Owner.VideoCard.ObtainCharacterWidth(typeface, character);
				}

				horizontalLocation += (short)this.Owner.VideoCard.ObtainCharacterWidth(typeface, character);
			}
		}
	}
}
