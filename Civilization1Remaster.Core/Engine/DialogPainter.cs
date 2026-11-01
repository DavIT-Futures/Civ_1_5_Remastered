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

		public ushort ShowMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort marker) =>
			ShowMenu(new MenuText(this.Owner, charsAddr), charsAddr, horizLoc, vertLoc, marker);

		public ushort ShowMenu(string text, int horizLoc, int vertLoc, ushort marker) =>
			ShowMenu(new MenuText(text), TextBuffer.StackTextAddress, horizLoc, vertLoc, marker);

		public void RunMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort shownChoice) =>
			RunMenu(new MenuText(this.Owner, charsAddr), charsAddr, horizLoc, vertLoc, shownChoice);

		public void FillBoxWithDoubleShadow(int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			if (style == 7 && Screen.BoxMotif != 0)
			{
				this.Owner.Helpers.FillBoxWithMotif(horizLoc + 1, vertLoc + 1, breadth, altitude);
			}
			else
			{
				this.Owner.Services.FloodBox(Screen.Box,
					horizLoc + 1, vertLoc + 1, breadth, altitude, style);
			}

			DrawingBox.RearHues = style;

			DrawShadowBox(horizLoc + 1, vertLoc + 1, breadth, altitude, 15, 8);

			DrawBox(horizLoc, vertLoc, breadth + 2, altitude + 2, 0);
		}

		public void DrawBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc, vertLoc, horizLoc + breadth, vertLoc, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc, vertLoc + altitude, horizLoc + breadth, vertLoc + altitude, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc + breadth, vertLoc, horizLoc + breadth, vertLoc + altitude, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc, vertLoc, horizLoc, vertLoc + altitude, style);
		}

		public void DrawShadowBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style, ushort style1)
		{
			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc, vertLoc, horizLoc + breadth, vertLoc, style1);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc, vertLoc + altitude, horizLoc + breadth, vertLoc + altitude, style);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc + breadth, vertLoc, horizLoc + breadth, vertLoc + altitude, style1);

			this.Owner.VideoCard.RenderLine(Screen.Box,
				horizLoc, vertLoc + 1, horizLoc, vertLoc + altitude, style);
		}

		public ushort ReadNavigationKey()
		{
			using var scope = this.Processor.Journal.Scope("ObtainNavigationKey()");

			ushort key = (ushort)this.Owner.Libc.ReadKey();

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

		private ushort ShowMenu(MenuText text, ushort charsAddr, int horizLoc, int vertLoc, ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayMenu(0x{charsAddr:x4}, {horizLoc}, {vertLoc}, {marker})");

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

			ushort fontId = DrawingBox.TypefaceIdx;
			ushort lineHeight = this.Owner.VideoCard.ObtainTextHeight(fontId);

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
			ushort shadowHue = (ushort)(vertLoc == 139 ? 8 : 0x16);

			if ((vertLoc & 1) != 0 && vertLoc != 139)
			{
				backHue = this.Owner.VideoCard.ObtainDot(0, horizLoc, vertLoc);
				shadowHue = 0xffff;
			}

			Menu.FlashHue = (ushort)(backHue == 15 ? 3 : 15);

			if (Screen.VgaMode == 0)
			{
				shadowHue = 0xffff;
			}

			ushort highlightHeight = Menu.LineHeight;
			ushort mouseUsed = 0;
			ushort done = 0;

			this.Owner.Startup.HideMouseCursor();

			RunMenu(text, charsAddr, horizLoc, vertLoc, Menu.DefaultLine);

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
					this.Owner.GlobDb3a = 0;

					this.Owner.Startup.ReadMouseState();

					if (this.Owner.GlobDb3a != 0 || mouseUsed != 0)
					{
						Menu.MouseUsed = 1;
						mouseUsed = 1;

						if (this.Owner.GlobDb3a == 2)
						{
							Menu.HelpTopic = 1;
						}

						ushort rawLine = (ushort)((short)this.Owner.GlobDb3e - (short)vertLoc - 4);
						currentChoice = (ushort)(DivideWords(rawLine, Menu.LineHeight) - Menu.FirstChoice);

						if (horizLoc > (short)this.Owner.GlobDb3c || (short)this.Owner.GlobDb3c > (short)Menu.RightEdge)
						{
							currentChoice = 0xffff;
						}

						if (((1 << (currentChoice & 0x1f)) & Menu.DisabledLines) != 0)
						{
							currentChoice = previousChoice;
						}
						else if (this.Owner.GlobDb3a == 0)
						{
							done = 1;
						}
					}
					else if (this.Owner.Libc.KeyAvailable() != 0)
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
							this.Owner.World.SessionPrefMarkers ^= 0x10;

							if ((this.Owner.World.SessionPrefMarkers & 0x10) == 0)
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
							ushort searchIdx = (ushort)(currentChoice + 1);

							while ((short)searchIdx < 0x20)
							{
								byte hotkey = this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(searchIdx + MenuItemHotkeysTableAddress));

								if (hotkey != 0xff)
								{
									if ((hotkey & 0x1f) == (key & 0x1f))
									{
										currentChoice = searchIdx;
										break;
									}
								}

								searchIdx++;
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

						SwapMenuOptionHue(horizLoc, vertLoc, previousChoice, highlightHeight, backHue, shadowHue, false);
						SwapMenuOptionHue(horizLoc, vertLoc, currentChoice, highlightHeight, backHue, shadowHue, true);

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

					if (Screen.VgaMode == 0)
					{
						byte flashHue = (byte)(Menu.HelpTopic != 0 ? 11 : Menu.FlashHue);

						this.Owner.VideoCard.SubstituteHue(Screen.Box,
							horizLoc + 3,
							(((short)currentChoice + (short)Menu.FirstChoice) *
								(short)Menu.LineHeight) + vertLoc + 4,
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

		private void SwapMenuOptionHue(int horizLoc, int vertLoc, ushort choice, ushort highlightHeight, ushort backHue, ushort shadowHue, bool highlight)
		{
			if (choice == 0xffff)
			{
				return;
			}

			int rowTop = (((short)choice + (short)Menu.FirstChoice) *
				(short)Menu.LineHeight) + vertLoc + 4;
			int width = (short)Menu.Width + 5;

			this.Owner.VideoCard.SubstituteHue(Screen.Box,
				horizLoc + 3, rowTop, width, (short)highlightHeight,
				highlight ? (byte)backHue : (byte)11,
				highlight ? (byte)11 : (byte)backHue);

			if (shadowHue != 0xffff)
			{
				this.Owner.VideoCard.SubstituteHue(Screen.Box,
					horizLoc + 3, rowTop, width, (short)highlightHeight,
					highlight ? (byte)shadowHue : (byte)3,
					highlight ? (byte)3 : (byte)shadowHue);
			}
		}

		private void RunMenu(MenuText text, ushort charsAddr, int horizLoc, int vertLoc, ushort shownChoice)
		{
			using var _ = this.Processor.Journal.Scope($"OperateMenu(0x{charsAddr:x4}, {horizLoc}, {vertLoc}, {shownChoice})");

			Menu.LineOffsets = 0;

			if (Menu.Measuring == 1)
			{
				MeasureAndDrawMenuFrame(text, horizLoc, vertLoc);
			}

			DrawMenuLines(text, horizLoc, vertLoc, shownChoice);
		}

		private void MeasureAndDrawMenuFrame(MenuText text, int horizLoc, int vertLoc)
		{
			ushort fontId = DrawingBox.TypefaceIdx;
			ushort lineHeight = this.Owner.VideoCard.ObtainTextHeight(fontId);

			if (lineHeight == 9)
			{
				lineHeight--;
			}

			if (King.Mood != 0xffff)
			{
				lineHeight = 8;
			}

			Menu.LineHeight = lineHeight;

			for (ushort idx = 0; idx < 0x20; idx++)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(idx + 0xdc4a), 0xff);
			}

			ushort choiceCount = 0;
			ushort lineWidth = 0;
			ushort charIdx = 0;
			Menu.LineCount = 0;
			Menu.Width = 0;

			while (text.Length > charIdx)
			{
				byte ch = text[charIdx];

				if (ch == 0xa)
				{
					if ((short)lineWidth > (short)Menu.Width)
					{
						Menu.Width = lineWidth;
					}

					lineWidth = 0;

					ushort lineCount = (ushort)(Menu.LineCount + 1);
					Menu.LineCount = lineCount;
					this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)((lineCount << 1) + 0x654e), (ushort)(charIdx + 1));
				}
				else
				{
					if (lineWidth == 0)
					{
						if (ch == 0x20 || ch == 0x5f)
						{
							if ((short)choiceCount < 0x20)
							{
								byte nextCh = text[charIdx + 1];
								this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(choiceCount + 0xdc4a), nextCh);
							}

							if (Menu.FirstChoice == 0xffff)
							{
								Menu.FirstChoice = Menu.LineCount;
							}

							choiceCount++;
						}
					}

					ushort currentFont = DrawingBox.TypefaceIdx;
					lineWidth = (ushort)(lineWidth + this.Owner.VideoCard.ObtainCharWidth(currentFont, ch));
				}

				charIdx++;
			}

			short clampedLines = (short)this.Owner.Helpers.ClampValue(
				(short)Menu.LineCount,
				0, (192 - vertLoc) / (short)Menu.LineHeight);
			Menu.LineCount = (ushort)clampedLines;

			ushort rightEdge = (ushort)(horizLoc + Menu.Width + 8);
			Menu.RightEdge = rightEdge;

			ushort bottomEdge = (ushort)(MultiplyWords((ushort)clampedLines, Menu.LineHeight) + vertLoc + 6);

			if (Menu.Footer != 0)
			{
				bottomEdge += 2;
			}

			ushort totalLen = text.Length;

			if (text[totalLen - 1] != 0xa)
			{
				choiceCount--;
			}

			Menu.ChoiceCount = choiceCount;

			if ((vertLoc & 1) == 0)
			{
				ushort advisor = Screen.DialogStyle;

				if (advisor == 0xffff)
				{
					FillBoxWithDoubleShadow(horizLoc, vertLoc,
						(short)Menu.RightEdge - horizLoc, (short)bottomEdge - vertLoc, 7);
				}
				else
				{
					string title = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16,
						this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((advisor << 1) + AdvisorTitlesTableAddress))));

					if ((short)advisor <= 2)
					{
						title += " report:";
					}

					ushort titleWidth = (ushort)this.Owner.TextPainter.MeasureTextWidth(title);

					ushort minRight = (ushort)(horizLoc + titleWidth + 8);

					if ((short)Menu.RightEdge < (short)minRight)
					{
						Menu.RightEdge = minRight;
					}

					ushort boxSpan = (ushort)((short)bottomEdge - vertLoc);
					short boxHeight = (short)this.Owner.Helpers.ClampValue((short)boxSpan + 8, 61, 999);

					FillBoxWithDoubleShadow(horizLoc - 42, vertLoc - 8,
						((short)Menu.RightEdge - horizLoc) + 42, boxHeight, 7);

					short portraitExtra = (short)(boxSpan - 0x34);

					if ((short)advisor <= 2)
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box,
							horizLoc - 40, vertLoc - 5,
							this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((advisor << 1) + AdvisorPortraitsTableAddress)));
					}
					else if (portraitExtra > 0)
					{
						this.Owner.VideoCard.RenderImage(
							Screen.PictureScreenBox,
							(40 * (short)advisor) + 40, 140, 40, 60,
							Screen.Box,
							horizLoc - 40,
							(portraitExtra - 1 + vertLoc) - 6);
					}
					else
					{
						this.Owner.VideoCard.RenderImage(
							Screen.PictureScreenBox,
							(40 * advisor) + 40, 140, 40, 60,
							Screen.Box,
							horizLoc - 40, vertLoc - 6);
					}

					this.Owner.TextPainter.DrawTextToScreen0(title, horizLoc + 5, vertLoc - 4, Hues.White);
					this.Owner.TextPainter.MeasureTextWidth(title);
					this.Owner.VideoCard.RenderLine(Screen.Box,
						horizLoc + 5, vertLoc + 3, horizLoc + 5, vertLoc + 3, Hues.LightCyan);
				}
			}

			if (Menu.Footer != 0)
			{
				ushort savedFont = DrawingBox.TypefaceIdx;
				DrawingBox.TypefaceIdx = 2;

				this.Owner.TextPainter.DrawTextToScreen0(GameTexts.TextHelpAvailable,
					(short)Menu.RightEdge - 74,
					(short)bottomEdge - 4,
					Hues.LightGreen);

				DrawingBox.TypefaceIdx = savedFont;
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

		private void DrawMenuLines(MenuText text, int horizLoc, int vertLoc, ushort shownChoice)
		{
			byte firstCh = text[0];
			short optionIdx = (firstCh == 0x20 || firstCh == 0x5f) ? (short)0 : (short)-1;

			DrawingBox.ForeHues = Menu.FlashHue;

			ushort lineIdx = 0;

			while ((short)lineIdx < (short)Menu.LineCount)
			{
				bool drawLine = true;

				if (Menu.Measuring == 0)
				{
					short diff = (short)this.Owner.Libc.AbsoluteValue((short)(shownChoice - (ushort)optionIdx));

					if (diff >= 2)
					{
						drawLine = false;
					}
				}

				if (drawLine)
				{
					ushort lineSlot = (ushort)(lineIdx << 1);
					ushort nextLineStart = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress + 2));
					text[nextLineStart - 1] = 0;

					bool checkedOption = optionIdx >= 0 &&
						((1 << (optionIdx & 0x1f)) & Menu.Options) != 0;

					if (checkedOption)
					{
						ushort lineStart = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress));
						text[lineStart] = 0x5e;

						ushort hue = (ushort)(((1 << (optionIdx & 0x1f)) & Menu.DisabledLines) != 0 ? 3 : 0);

						this.Owner.TextPainter.DrawTextToScreen0(text.From(lineStart),
							horizLoc + 5,
							((short)lineIdx * (short)Menu.LineHeight) + vertLoc + 5,
							hue);

						text[this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress))] = 0x20;
					}
					else
					{
						if (optionIdx < 0 && (short)Menu.LineHeight > 9)
						{
							this.Owner.TextPainter.DrawTextToScreen0(
								text.From(this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress))),
								horizLoc + 5,
								((short)lineIdx * (short)Menu.LineHeight) + vertLoc + 6,
								Hues.Black);
						}

						ushort hue;

						if (optionIdx < 0)
						{
							hue = Menu.FlashHue;
						}
						else
						{
							hue = (ushort)(((1 << (optionIdx & 0x1f)) & Menu.DisabledLines) != 0 ? 3 : 0);
						}

						this.Owner.TextPainter.DrawTextToScreen0(
							text.From(this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress))),
							horizLoc + 5,
							((short)lineIdx * (short)Menu.LineHeight) + vertLoc + 5,
							hue);
					}

					ushort endOffset = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress + 2));
					text[endOffset - 1] = 0xa;
				}

				ushort nextOffset = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)((lineIdx << 1) + MenuLineOffsetsTableAddress + 2));
				byte nextLineChar = text[nextOffset];

				if (nextLineChar == 0x20 || nextLineChar == 0x5f)
				{
					optionIdx++;
				}

				lineIdx++;
			}
		}
	}
}
