namespace Civilization1Remaster.Core.Engine
{
	public partial class DialogPainter : GameComponent, IDialogPainter
	{
		private const ushort MenuItemHotkeysTableAddress = 0xdc4a;
		private const ushort AdvisorTitlesTableAddress = 0x2fa6;
		private const ushort AdvisorPortraitsTableAddress = 0xdf62;
		private const ushort MenuLineOffsetsTableAddress = 0x654e;

		public DialogPainter(GameEngine owner)
			: base(owner)
		{
		}

		public bool ChoicesIndentTokens { get; set; } = false;

		public bool ChoicesIndentTokenDown { get; set; } = false;

		public ushort ShowMenu(ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort marker) =>
			ShowMenu(new MenuText(this.Owner, charactersAddress), charactersAddress, horizontalLocation, verticalLocation, marker);

		public ushort ShowMenu(string text, int horizontalLocation, int verticalLocation, ushort marker) =>
			ShowMenu(new MenuText(text), TextBuffer.StackTextAddress, horizontalLocation, verticalLocation, marker);

		public void RunMenu(ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort shownChoice) =>
			RunMenu(new MenuText(this.Owner, charactersAddress), charactersAddress, horizontalLocation, verticalLocation, shownChoice);

		public void FillBoxWithDoubleShadow(int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style)
		{
			if (style == 7 && Screen.BoxMotif != 0)
			{
				this.Owner.Helpers.FillBoxWithMotif(horizontalLocation + 1, verticalLocation + 1, breadth, altitude);
			}
			else
			{
				this.Owner.Services.FloodBox(Screen.Box,
					horizontalLocation + 1, verticalLocation + 1, breadth, altitude, style);
			}

			DrawingBox.RearHues = style;

			DrawShadowBox(horizontalLocation + 1, verticalLocation + 1, breadth, altitude, 15, 8);

			DrawBox(horizontalLocation, verticalLocation, breadth + 2, altitude + 2, 0);
		}

		public void DrawBox(int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style)
		{
			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation, horizontalLocation + breadth, verticalLocation, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation + altitude, horizontalLocation + breadth, verticalLocation + altitude, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation + breadth, verticalLocation, horizontalLocation + breadth, verticalLocation + altitude, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation, horizontalLocation, verticalLocation + altitude, style);
		}

		public void DrawShadowBox(int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style, ushort firstStyle)
		{
			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation, horizontalLocation + breadth, verticalLocation, firstStyle);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation + altitude, horizontalLocation + breadth, verticalLocation + altitude, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation + breadth, verticalLocation, horizontalLocation + breadth, verticalLocation + altitude, firstStyle);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizontalLocation, verticalLocation + 1, horizontalLocation, verticalLocation + altitude, style);
		}

		public ushort ReadNavigationKey()
		{
			using var scope = this.Processor.Journal.Scope("ObtainNavigationKey()");

			ushort key = (ushort)this.Owner.Runtime.ReadKey();

			key = key switch
			{
				0x4800 or 0x487e => 0x4800,
				0x4900 => 0x4900,
				0x4d00 or 0xf400 => 0x4d00,
				0x5100 => 0x5100,
				0x5000 or 0x5060 => 0x5000,
				0x4f00 => 0x4f00,
				0x4b00 or 0x4b7c => 0x4b00,
				0x4700 or 0x475c => 0x4700,
				0x4838 => 0x4838,
				0x4939 => 0x4939,
				0x4d36 or 0x4d46 => 0x4d36,
				0x5133 => 0x5133,
				0x5032 => 0x5032,
				0x4f31 => 0x4f31,
				0x4b34 or 0x4b43 => 0x4b34,
				0x4737 => 0x4737,
				_ => (key & 0xff) != 0 ? (ushort)(key & 0xff) : key
			};

			return key;
		}

		private ushort ShowMenu(MenuText text, ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayMenu(0x{charactersAddress:x4}, {horizontalLocation}, {verticalLocation}, {marker})");

			if (marker != 0)
			{
				this.Owner.TurnLogic.WaitForKey();
			}

			ushort previousChoice = 0xffff;
			ushort currentChoice = 0;
			Menu.MouseUsed = 0;
			Menu.HelpTopic = 0;

			if (Menu.DefaultLine != 0xffff)
			{
				currentChoice = Menu.DefaultLine;
			}

			Menu.FirstChoice = 0xffff;
			Menu.Measuring = 0x1;

			ushort fontIdentifier = DrawingBox.TypefaceIndex;
			ushort lineHeight = this.Owner.VideoCard.ObtainTextHeight(fontIdentifier);

			if (lineHeight == 9)
			{
				lineHeight--;
			}

			if (King.Mood != 0xffff)
			{
				lineHeight = 8;
			}

			Menu.LineHeight = lineHeight;

			ushort backHue = 7;
			ushort shadowHue = (ushort)(verticalLocation == 139 ? 8 : 0x16);

			if ((verticalLocation & 1) != 0 && verticalLocation != 139)
			{
				backHue = this.Owner.VideoCard.ObtainDot(0, horizontalLocation, verticalLocation);
				shadowHue = 0xffff;
			}

			Menu.FlashHue = (ushort)(backHue == 15 ? 3 : 15);

			if (Screen.VideoGraphicsMode == 0)
			{
				shadowHue = 0xffff;
			}

			ushort highlightHeight = Menu.LineHeight;
			ushort mouseUsed = 0;
			ushort done = 0;

			this.Owner.Startup.HideMouseCursor();

			RunMenu(text, charactersAddress, horizontalLocation, verticalLocation, Menu.DefaultLine);

			this.Owner.Startup.ShowMouseCursor();

			if (Screen.DialogNeedsRedraw != 0)
			{
				Screen.DialogNeedsRedraw = 0;
				currentChoice = 0xffff;
			}
			else
			{
				while (done == 0)
				{
					if (King.Mood != 0xffff)
					{
						this.Owner.Audience.ChoreographKingSpeaking();
					}

					Menu.Measuring = 0;
					this.Owner.MenuPointerState = 0;

					this.Owner.Startup.ReadMouseState();

					if (this.Owner.MenuPointerState != 0 || mouseUsed != 0)
					{
						Menu.MouseUsed = 1;
						mouseUsed = 1;

						if (this.Owner.MenuPointerState == 2)
						{
							Menu.HelpTopic = 1;
						}

						ushort rawLine = (ushort)((short)this.Owner.MenuPointerVertical - (short)verticalLocation - 4);
						currentChoice = (ushort)(DivideWords(rawLine, Menu.LineHeight) - Menu.FirstChoice);

						if (horizontalLocation > (short)this.Owner.MenuPointerHorizontal || (short)this.Owner.MenuPointerHorizontal > (short)Menu.RightEdge)
						{
							currentChoice = 0xffff;
						}

						if (((1 << (currentChoice & 0x1f)) & Menu.DisabledLines) != 0)
						{
							currentChoice = previousChoice;
						}
						else if (this.Owner.MenuPointerState == 0)
						{
							done = 1;
						}
					}
					else if (this.Owner.Runtime.KeyAvailable() != 0)
					{
						ushort key = ReadNavigationKey();

						if (this.ChoicesIndentTokens && (key == KeyCodes.Tab || key == KeyCodes.Left || key == KeyCodes.Right))
						{
							this.ChoicesIndentTokenDown = true;
							currentChoice = 0xffff;
							done = 1;
						}
						else if (key == KeyCodes.Space || key == KeyCodes.Enter)
						{
							if (((1 << (currentChoice & 0x1f)) & Menu.DisabledLines) == 0)
							{
								done = 1;
							}
						}
						else if (key == KeyCodes.Escape)
						{
							currentChoice = 0xffff;
							done = 1;
						}
						else if (key == KeyCodes.AltH)
						{
							Menu.HelpTopic = 1;
							done = 1;
						}
						else if (key == KeyCodes.AltV)
						{
							this.Owner.World.SessionPreferenceMarkers ^= 0x10;

							if ((this.Owner.World.SessionPreferenceMarkers & 0x10) == 0)
							{
								this.Owner.Services.PerformSound(1, 0);
							}
						}
						else if (key == KeyCodes.Up)
						{
							if ((short)currentChoice > 0)
							{
								currentChoice--;
							}
						}
						else if (key == KeyCodes.Down)
						{
							if ((short)currentChoice < (short)(Menu.ChoiceCount - 1))
							{
								currentChoice++;
							}
						}
						else
						{
							ushort searchIndex = (ushort)(currentChoice + 1);

							while ((short)searchIndex < 0x20)
							{
								byte hotkey = this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)(searchIndex + MenuItemHotkeysTableAddress));

								if (hotkey != 0xff)
								{
									if ((hotkey & 0x1f) == (key & 0x1f))
									{
										currentChoice = searchIndex;
										break;
									}
								}

								searchIndex++;
							}
						}
					}

					if ((short)currentChoice < 0 ||
						(short)currentChoice >= (short)Menu.ChoiceCount ||
						Menu.MoreBox != 0 ||
						Menu.AnyKey != 0)
					{
						currentChoice = 0xffff;
					}

					if (currentChoice != previousChoice)
					{
						this.Owner.Startup.HideMouseCursor();

						SwapMenuOptionHue(horizontalLocation, verticalLocation, previousChoice, highlightHeight, backHue, shadowHue, false);
						SwapMenuOptionHue(horizontalLocation, verticalLocation, currentChoice, highlightHeight, backHue, shadowHue, true);

						previousChoice = currentChoice;

						this.Owner.Startup.ShowMouseCursor();
					}
				}

				if (currentChoice == 0xffff)
				{
					Menu.HelpTopic = 0;
				}
				else
				{
					this.Owner.Startup.HideMouseCursor();

					if (Screen.VideoGraphicsMode == 0)
					{
						byte flashHue = (byte)(Menu.HelpTopic != 0 ? 11 : Menu.FlashHue);

						this.Owner.VideoCard.SubstituteHue(Screen.Box,
							horizontalLocation + 3,
							(((short)currentChoice + (short)Menu.FirstChoice) *
								(short)Menu.LineHeight) + verticalLocation + 4,
							(short)Menu.Width + 5,
							(short)highlightHeight,
							Hues.LightCyan,
							flashHue);
					}

					this.Owner.TextPainter.WaitForTime(20);
					this.Owner.Startup.ShowMouseCursor();
				}

				Screen.DialogStyle = 0xffff;
				Menu.DefaultLine = 0xffff;
				Menu.Footer = 0;
				Menu.AnyKey = 0;
				Menu.DisabledLines = 0;
				Menu.Options = 0;
			}

			return currentChoice;
		}

		private void SwapMenuOptionHue(int horizontalLocation, int verticalLocation, ushort choice, ushort highlightHeight, ushort backHue, ushort shadowHue, bool highlight)
		{
			if (choice == 0xffff)
			{
				return;
			}

			int rowTop = (((short)choice + (short)Menu.FirstChoice) *
				(short)Menu.LineHeight) + verticalLocation + 4;
			int width = (short)Menu.Width + 5;

			this.Owner.VideoCard.SubstituteHue(Screen.Box,
				horizontalLocation + 3, rowTop, width, (short)highlightHeight,
				highlight ? (byte)backHue : (byte)11,
				highlight ? (byte)11 : (byte)backHue);

			if (shadowHue != 0xffff)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					horizontalLocation + 3, rowTop, width, (short)highlightHeight,
					highlight ? (byte)shadowHue : (byte)3,
					highlight ? (byte)3 : (byte)shadowHue);
			}
		}

		private void RunMenu(MenuText text, ushort charactersAddress, int horizontalLocation, int verticalLocation, ushort shownChoice)
		{
			using var _ = this.Processor.Journal.Scope($"OperateMenu(0x{charactersAddress:x4}, {horizontalLocation}, {verticalLocation}, {shownChoice})");

			Menu.LineOffsets = 0;

			if (Menu.Measuring == 1)
			{
				MeasureAndDrawMenuFrame(text, horizontalLocation, verticalLocation);
			}

			DrawMenuLines(text, horizontalLocation, verticalLocation, shownChoice);
		}

		private void MeasureAndDrawMenuFrame(MenuText text, int horizontalLocation, int verticalLocation)
		{
			ushort fontIdentifier = DrawingBox.TypefaceIndex;
			ushort lineHeight = this.Owner.VideoCard.ObtainTextHeight(fontIdentifier);

			if (lineHeight == 9)
			{
				lineHeight--;
			}

			if (King.Mood != 0xffff)
			{
				lineHeight = 8;
			}

			Menu.LineHeight = lineHeight;

			for (ushort index = 0; index < 0x20; index++)
			{
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(index + 0xdc4a), 0xff);
			}

			ushort choiceCount = 0;
			ushort lineWidth = 0;
			ushort characterIndex = 0;
			Menu.LineCount = 0;
			Menu.Width = 0;

			while (text.Length > characterIndex)
			{
				byte character = text[characterIndex];

				if (character == 0xa)
				{
					if ((short)lineWidth > (short)Menu.Width)
					{
						Menu.Width = lineWidth;
					}

					lineWidth = 0;

					ushort lineCount = (ushort)(Menu.LineCount + 1);
					Menu.LineCount = lineCount;
					this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)((lineCount << 1) + 0x654e), (ushort)(characterIndex + 1));
				}
				else
				{
					if (lineWidth == 0)
					{
						if (character == 0x20 || character == 0x5f)
						{
							if ((short)choiceCount < 0x20)
							{
								byte nextCharacter = text[characterIndex + 1];
								this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(choiceCount + 0xdc4a), nextCharacter);
							}

							if (Menu.FirstChoice == 0xffff)
							{
								Menu.FirstChoice = Menu.LineCount;
							}

							choiceCount++;
						}
					}

					ushort currentFont = DrawingBox.TypefaceIndex;
					lineWidth = (ushort)(lineWidth + this.Owner.VideoCard.ObtainCharacterWidth(currentFont, character));
				}

				characterIndex++;
			}

			short clampedLines = (short)this.Owner.Helpers.ClampValue(
				(short)Menu.LineCount,
				0, (192 - verticalLocation) / (short)Menu.LineHeight);
			Menu.LineCount = (ushort)clampedLines;

			ushort rightEdge = (ushort)(horizontalLocation + Menu.Width + 8);
			Menu.RightEdge = rightEdge;

			ushort bottomEdge = (ushort)(MultiplyWords((ushort)clampedLines, Menu.LineHeight) + verticalLocation + 6);

			if (Menu.Footer != 0)
			{
				bottomEdge += 2;
			}

			ushort totalLength = text.Length;

			if (text[totalLength - 1] != 0xa)
			{
				choiceCount--;
			}

			Menu.ChoiceCount = choiceCount;

			if ((verticalLocation & 1) == 0)
			{
				ushort advisor = Screen.DialogStyle;

				if (advisor == 0xffff)
				{
					FillBoxWithDoubleShadow(horizontalLocation, verticalLocation,
						(short)Menu.RightEdge - horizontalLocation, (short)bottomEdge - verticalLocation, 7);
				}
				else
				{
					string title = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word,
						this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((advisor << 1) + AdvisorTitlesTableAddress))));

					if ((short)advisor <= 2)
					{
						title += " report:";
					}

					ushort titleWidth = (ushort)this.Owner.TextPainter.MeasureTextWidth(title);

					ushort minimumRight = (ushort)(horizontalLocation + titleWidth + 8);

					if ((short)Menu.RightEdge < (short)minimumRight)
					{
						Menu.RightEdge = minimumRight;
					}

					ushort boxSpan = (ushort)((short)bottomEdge - verticalLocation);
					short boxHeight = (short)this.Owner.Helpers.ClampValue((short)boxSpan + 8, 61, 999);

					FillBoxWithDoubleShadow(horizontalLocation - 42, verticalLocation - 8,
						((short)Menu.RightEdge - horizontalLocation) + 42, boxHeight, 7);

					short portraitExtra = (short)(boxSpan - 0x34);

					if ((short)advisor <= 2)
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box,
							horizontalLocation - 40, verticalLocation - 5,
							this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((advisor << 1) + AdvisorPortraitsTableAddress)));
					}
					else if (portraitExtra > 0)
					{
						this.Owner.VideoCard.RenderImage(
							Screen.PictureScreenBox,
							(40 * (short)advisor) + 40, 140, 40, 60,
							Screen.Box,
							horizontalLocation - 40,
							(portraitExtra - 1 + verticalLocation) - 6);
					}
					else
					{
						this.Owner.VideoCard.RenderImage(
							Screen.PictureScreenBox,
							(40 * advisor) + 40, 140, 40, 60,
							Screen.Box,
							horizontalLocation - 40, verticalLocation - 6);
					}

					this.Owner.TextPainter.DrawTextToPrimaryScreen(title, horizontalLocation + 5, verticalLocation - 4, Hues.White);
					this.Owner.TextPainter.MeasureTextWidth(title);
					this.Owner.VideoCard.RenderLine(Screen.Box,
						horizontalLocation + 5, verticalLocation + 3, horizontalLocation + 5, verticalLocation + 3, Hues.LightCyan);
				}
			}

			if (Menu.Footer != 0)
			{
				ushort savedFont = DrawingBox.TypefaceIndex;
				DrawingBox.TypefaceIndex = 2;

				this.Owner.TextPainter.DrawTextToPrimaryScreen(GameTexts.TextHelpAvailable,
					(short)Menu.RightEdge - 74,
					(short)bottomEdge - 4,
					Hues.LightGreen);

				DrawingBox.TypefaceIndex = savedFont;
			}

			if (Menu.MoreBox != 0)
			{
				this.Owner.TextPainter.RenderText(0x2fce,
					(ushort)(Menu.RightEdge - 0x11),
					(ushort)(bottomEdge - 8),
					11);

				DrawBox(
					(short)Menu.RightEdge - 20,
					(short)bottomEdge - 10,
					20, 10, 11);
			}
		}

		private void DrawMenuLines(MenuText text, int horizontalLocation, int verticalLocation, ushort shownChoice)
		{
			byte firstCharacter = text[0];
			short optionIndex = (firstCharacter == 0x20 || firstCharacter == 0x5f) ? (short)0 : (short)-1;

			DrawingBox.ForegroundHues = Menu.FlashHue;

			ushort lineIndex = 0;

			while ((short)lineIndex < (short)Menu.LineCount)
			{
				bool drawLine = true;

				if (Menu.Measuring == 0)
				{
					short difference = (short)this.Owner.Runtime.AbsoluteValue((short)(shownChoice - (ushort)optionIndex));

					if (difference >= 2)
					{
						drawLine = false;
					}
				}

				if (drawLine)
				{
					ushort lineSlot = (ushort)(lineIndex << 1);
					ushort nextLineStart = this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(lineSlot + MenuLineOffsetsTableAddress + 2));
					text[nextLineStart - 1] = 0;

					bool checkedOption = optionIndex >= 0 &&
						((1 << (optionIndex & 0x1f)) & Menu.Options) != 0;

					if (checkedOption)
					{
						ushort lineStart = this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(lineSlot + MenuLineOffsetsTableAddress));
						text[lineStart] = 0x5e;

						ushort hue = (ushort)(((1 << (optionIndex & 0x1f)) & Menu.DisabledLines) != 0 ? 3 : 0);

						this.Owner.TextPainter.DrawTextToPrimaryScreen(text.From(lineStart),
							horizontalLocation + 5,
							((short)lineIndex * (short)Menu.LineHeight) + verticalLocation + 5,
							hue);

						text[this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(lineSlot + MenuLineOffsetsTableAddress))] = 0x20;
					}
					else
					{
						if (optionIndex < 0 && (short)Menu.LineHeight > 9)
						{
							this.Owner.TextPainter.DrawTextToPrimaryScreen(
								text.From(this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(lineSlot + MenuLineOffsetsTableAddress))),
								horizontalLocation + 5,
								((short)lineIndex * (short)Menu.LineHeight) + verticalLocation + 6,
								Hues.Black);
						}

						ushort hue;

						if (optionIndex < 0)
						{
							hue = Menu.FlashHue;
						}
						else
						{
							hue = (ushort)(((1 << (optionIndex & 0x1f)) & Menu.DisabledLines) != 0 ? 3 : 0);
						}

						this.Owner.TextPainter.DrawTextToPrimaryScreen(
							text.From(this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(lineSlot + MenuLineOffsetsTableAddress))),
							horizontalLocation + 5,
							((short)lineIndex * (short)Menu.LineHeight) + verticalLocation + 5,
							hue);
					}

					ushort endOffset = this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)(lineSlot + MenuLineOffsetsTableAddress + 2));
					text[endOffset - 1] = 0xa;
				}

				ushort nextOffset = this.Processor.PullWord(this.Processor.SegmentData.Word, (ushort)((lineIndex << 1) + MenuLineOffsetsTableAddress + 2));
				byte nextLineCharacter = text[nextOffset];

				if (nextLineCharacter == 0x20 || nextLineCharacter == 0x5f)
				{
					optionIndex++;
				}

				lineIndex++;
			}
		}
	}
}
