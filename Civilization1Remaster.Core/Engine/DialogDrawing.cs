using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2d05.
	/// </summary>
	public partial class DialogDrawing : GameModule
	{
		private const ushort MenuItemHotkeysTableAddress = 0xdc4a;
		private const ushort AdvisorTitlesTableAddress = 0x2fa6;
		private const ushort AdvisorPortraitsTableAddress = 0xdf62;
		private const ushort MenuLineOffsetsTableAddress = 0x654e;

		public DialogDrawing(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// When set, Tab, Left and Right close the menu (ShowMenu) like Escape and set MenuTabKeyPressed,
		/// so a menu with tabs can switch between them
		/// </summary>
		public bool ChoicesIndentTokens { get; set; } = false;

		/// <summary>
		/// Set when the last menu was closed with Tab, Left or Right (see MenuTabKeys)
		/// </summary>
		public bool ChoicesIndentTokenDown { get; set; } = false;

		/// <summary>
		/// Displays a menu or dialog box from the text at charsAddr at (horizLoc, vertLoc), handles keyboard and mouse navigation,
		/// and returns the chosen option index (or 0xffff when cancelled)
		/// </summary>
		public ushort DisplayMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort marker) =>
			DisplayMenu(new MenuText(this.owner, charsAddr), charsAddr, horizLoc, vertLoc, marker);

		/// <summary>DisplayMenu of a text in a C# string (the original kept it on the stack, where its changes stay unseen)</summary>
		public ushort DisplayMenu(string text, int horizLoc, int vertLoc, ushort marker) =>
			DisplayMenu(new MenuText(text), TextBuffer.StackTextAddress, horizLoc, vertLoc, marker);

		private ushort DisplayMenu(MenuText text, ushort charsAddr, int horizLoc, int vertLoc, ushort marker)
		{
			this.processor.Journal.OpenScope($"DisplayMenu(0x{charsAddr:x4}, {horizLoc}, {vertLoc}, {marker})");

			if (marker != 0)
			{
				this.owner.TurnLogic.AwaitForKey();
			}

			ushort previousChoice = 0xffff;
			ushort currentChoice = 0;
			MenuMouseUsed = 0;
			MenuHelpTopic = 0;

			if (DefaultLine != 0xffff)
			{
				currentChoice = DefaultLine;
			}

			MenuFirstChoice = 0xffff;
			MenuMeasuring = 0x1;

			ushort fontId = DrawingBox.TypefaceIdx;
			ushort lineHeight = this.owner.VideoCard.ObtainTextHeight(fontId);

			if (lineHeight == 9)
			{
				lineHeight--;
			}

			if (KingMood != 0xffff)
			{
				lineHeight = 8;
			}

			MenuLineHeight = lineHeight;

			ushort backHue = 7;
			ushort shadowHue = (ushort)(vertLoc == 139 ? 8 : 0x16);

			if ((vertLoc & 1) != 0 && vertLoc != 139)
			{
				backHue = this.owner.VideoCard.ObtainDot(0, horizLoc, vertLoc);
				shadowHue = 0xffff;
			}

			MenuFlashHue = (ushort)(backHue == 15 ? 3 : 15);

			if (VgaMode == 0)
			{
				shadowHue = 0xffff;
			}

			ushort highlightHeight = MenuLineHeight;
			ushort mouseUsed = 0;
			ushort done = 0;

			this.owner.Startup.ConcealMouseCursor();

			OperateMenu(text, charsAddr, horizLoc, vertLoc, DefaultLine);

			this.owner.Startup.DisplayMouseCursor();

			if (DialogNeedsRedraw != 0)
			{
				DialogNeedsRedraw = 0;
				currentChoice = 0xffff;
			}
			else
			{
				while (done == 0)
				{
					if (KingMood != 0xffff)
					{
						this.owner.Audience.ChoreographKingSpeaking();
					}

					MenuMeasuring = 0;
					this.owner.GlobDb3a = 0;

					this.owner.Startup.ObtainMouseState();

					if (this.owner.GlobDb3a != 0 || mouseUsed != 0)
					{
						MenuMouseUsed = 1;
						mouseUsed = 1;

						if (this.owner.GlobDb3a == 2)
						{
							MenuHelpTopic = 1;
						}

						ushort rawLine = (ushort)((short)this.owner.GlobDb3e - (short)vertLoc - 4);
						currentChoice = (ushort)(DivideWords(rawLine, MenuLineHeight) - MenuFirstChoice);

						if (horizLoc > (short)this.owner.GlobDb3c || (short)this.owner.GlobDb3c > (short)MenuRightEdge)
						{
							currentChoice = 0xffff;
						}

						if (((1 << (currentChoice & 0x1f)) & DisabledLines) != 0)
						{
							currentChoice = previousChoice;
						}
						else if (this.owner.GlobDb3a == 0)
						{
							done = 1;
						}
					}
					else if (this.owner.Libc.KeyAvailable() != 0)
					{
						ushort key = ObtainNavigationKey();

						if (this.ChoicesIndentTokens && (key == KeyCodes.Tab || key == KeyCodes.Left || key == KeyCodes.Right))
						{
							this.ChoicesIndentTokenDown = true;
							currentChoice = 0xffff;
							done = 1;
						}
						else if (key == KeyCodes.Space || key == KeyCodes.Enter)
						{
							if (((1 << (currentChoice & 0x1f)) & DisabledLines) == 0)
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
							MenuHelpTopic = 1;
							done = 1;
						}
						else if (key == KeyCodes.AltV)
						{
							this.owner.World.SessionPrefMarkers ^= 0x10;

							if ((this.owner.World.SessionPrefMarkers & 0x10) == 0)
							{
								this.owner.Services.PerformSound(1, 0);
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
							if ((short)currentChoice < (short)(MenuChoiceCount - 1))
							{
								currentChoice++;
							}
						}
						else
						{
							ushort searchIdx = (ushort)(currentChoice + 1);

							while ((short)searchIdx < 0x20)
							{
								byte hotkey = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(searchIdx + MenuItemHotkeysTableAddress));

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
						(short)currentChoice >= (short)MenuChoiceCount ||
						MenuMoreBox != 0 ||
						MenuAnyKey != 0)
					{
						currentChoice = 0xffff;
					}

					if (currentChoice != previousChoice)
					{
						this.owner.Startup.ConcealMouseCursor();

						SwapMenuOptionHue(horizLoc, vertLoc, previousChoice, highlightHeight, backHue, shadowHue, false);
						SwapMenuOptionHue(horizLoc, vertLoc, currentChoice, highlightHeight, backHue, shadowHue, true);

						previousChoice = currentChoice;

						this.owner.Startup.DisplayMouseCursor();
					}
				}

				if (currentChoice == 0xffff)
				{
					MenuHelpTopic = 0;
				}
				else
				{
					this.owner.Startup.ConcealMouseCursor();

					if (VgaMode == 0)
					{
						byte flashHue = (byte)(MenuHelpTopic != 0 ? 11 : MenuFlashHue);

						this.owner.VideoCard.SubstituteHue(ScreenBox,
							horizLoc + 3,
							(((short)currentChoice + (short)MenuFirstChoice) *
								(short)MenuLineHeight) + vertLoc + 4,
							(short)MenuWidth + 5,
							(short)highlightHeight,
							Hues.LightCyan,
							flashHue);
					}

					this.owner.TextPainter.AwaitTime(20);
					this.owner.Startup.DisplayMouseCursor();
				}

				DialogStyle = 0xffff;
				DefaultLine = 0xffff;
				MenuFooter = 0;
				MenuAnyKey = 0;
				DisabledLines = 0;
				MenuOptions = 0;
			}

			this.processor.Journal.CloseScope("DisplayMenu");

			return currentChoice;
		}

		private void SwapMenuOptionHue(int horizLoc, int vertLoc, ushort choice, ushort highlightHeight, ushort backHue, ushort shadowHue, bool highlight)
		{
			if (choice == 0xffff)
			{
				return;
			}

			int rowTop = (((short)choice + (short)MenuFirstChoice) *
				(short)MenuLineHeight) + vertLoc + 4;
			int width = (short)MenuWidth + 5;

			this.owner.VideoCard.SubstituteHue(ScreenBox,
				horizLoc + 3, rowTop, width, (short)highlightHeight,
				highlight ? (byte)backHue : (byte)11,
				highlight ? (byte)11 : (byte)backHue);

			if (shadowHue != 0xffff)
			{
				this.owner.VideoCard.SubstituteHue(ScreenBox,
					horizLoc + 3, rowTop, width, (short)highlightHeight,
					highlight ? (byte)shadowHue : (byte)3,
					highlight ? (byte)3 : (byte)shadowHue);
			}
		}

		/// <summary>
		/// Layouts and renders the menu or dialog box for charsAddr at (horizLoc, vertLoc): on the first pass (0xd2e2 == 1) measures
		/// the lines, records their offsets (0x654e) and hotkeys (0xdc4a), and draws the frame and advisor portrait (0x2f9e); then
		/// renders the text lines
		/// </summary>
		public void OperateMenu(ushort charsAddr, int horizLoc, int vertLoc, ushort shownChoice) =>
			OperateMenu(new MenuText(this.owner, charsAddr), charsAddr, horizLoc, vertLoc, shownChoice);

		private void OperateMenu(MenuText text, ushort charsAddr, int horizLoc, int vertLoc, ushort shownChoice)
		{
			this.processor.Journal.OpenScope($"OperateMenu(0x{charsAddr:x4}, {horizLoc}, {vertLoc}, {shownChoice})");

			MenuLineOffsets = 0;

			if (MenuMeasuring == 1)
			{
				MeasureAndDrawMenuFrame(text, horizLoc, vertLoc);
			}

			RenderMenuLines(text, horizLoc, vertLoc, shownChoice);

			this.processor.Journal.CloseScope("OperateMenu");
		}

		private void MeasureAndDrawMenuFrame(MenuText text, int horizLoc, int vertLoc)
		{
			ushort fontId = DrawingBox.TypefaceIdx;
			ushort lineHeight = this.owner.VideoCard.ObtainTextHeight(fontId);

			if (lineHeight == 9)
			{
				lineHeight--;
			}

			if (KingMood != 0xffff)
			{
				lineHeight = 8;
			}

			MenuLineHeight = lineHeight;

			for (ushort idx = 0; idx < 0x20; idx++)
			{
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(idx + 0xdc4a), 0xff);
			}

			ushort choiceCount = 0;
			ushort lineWidth = 0;
			ushort charIdx = 0;
			MenuLineCount = 0;
			MenuWidth = 0;

			while (text.Length > charIdx)
			{
				byte ch = text[charIdx];

				if (ch == 0xa)
				{
					if ((short)lineWidth > (short)MenuWidth)
					{
						MenuWidth = lineWidth;
					}

					lineWidth = 0;

					ushort lineCount = (ushort)(MenuLineCount + 1);
					MenuLineCount = lineCount;
					this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)((lineCount << 1) + 0x654e), (ushort)(charIdx + 1));
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
								this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(choiceCount + 0xdc4a), nextCh);
							}

							if (MenuFirstChoice == 0xffff)
							{
								MenuFirstChoice = MenuLineCount;
							}

							choiceCount++;
						}
					}

					ushort currentFont = DrawingBox.TypefaceIdx;
					lineWidth = (ushort)(lineWidth + this.owner.VideoCard.ObtainCharWidth(currentFont, ch));
				}

				charIdx++;
			}

			short clampedLines = (short)this.owner.Helpers.VerifyValueSpan(
				(short)MenuLineCount,
				0, (192 - vertLoc) / (short)MenuLineHeight);
			MenuLineCount = (ushort)clampedLines;

			ushort rightEdge = (ushort)(horizLoc + MenuWidth + 8);
			MenuRightEdge = rightEdge;

			ushort bottomEdge = (ushort)(MultiplyWords((ushort)clampedLines, MenuLineHeight) + vertLoc + 6);

			if (MenuFooter != 0)
			{
				bottomEdge += 2;
			}

			ushort totalLen = text.Length;

			if (text[totalLen - 1] != 0xa)
			{
				choiceCount--;
			}

			MenuChoiceCount = choiceCount;

			if ((vertLoc & 1) == 0)
			{
				ushort advisor = DialogStyle;

				if (advisor == 0xffff)
				{
					FloodBoxWithDoubleShadow(horizLoc, vertLoc,
						(short)MenuRightEdge - horizLoc, (short)bottomEdge - vertLoc, 7);
				}
				else
				{
					// The title of the advisor (the original builds it in its frame)
					string title = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16,
						this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((advisor << 1) + AdvisorTitlesTableAddress))));

					if ((short)advisor <= 2)
					{
						title += " report:";
					}

					ushort titleWidth = (ushort)this.owner.TextPainter.ObtainTextWidth(title);

					ushort minRight = (ushort)(horizLoc + titleWidth + 8);

					if ((short)MenuRightEdge < (short)minRight)
					{
						MenuRightEdge = minRight;
					}

					ushort boxSpan = (ushort)((short)bottomEdge - vertLoc);
					short boxHeight = (short)this.owner.Helpers.VerifyValueSpan((short)boxSpan + 8, 61, 999);

					FloodBoxWithDoubleShadow(horizLoc - 42, vertLoc - 8,
						((short)MenuRightEdge - horizLoc) + 42, boxHeight, 7);

					short portraitExtra = (short)(boxSpan - 0x34);

					if ((short)advisor <= 2)
					{
						this.owner.Services.RenderPictureToScreen(ScreenBox,
							horizLoc - 40, vertLoc - 5,
							this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((advisor << 1) + AdvisorPortraitsTableAddress)));
					}
					else if (portraitExtra > 0)
					{
						this.owner.VideoCard.RenderImage(
							PictureScreenBox,
							(40 * (short)advisor) + 40, 140, 40, 60,
							ScreenBox,
							horizLoc - 40,
							(portraitExtra - 1 + vertLoc) - 6);
					}
					else
					{
						this.owner.VideoCard.RenderImage(
							PictureScreenBox,
							(40 * advisor) + 40, 140, 40, 60,
							ScreenBox,
							horizLoc - 40, vertLoc - 6);
					}

					this.owner.TextPainter.RenderTextToScreen0(title, horizLoc + 5, vertLoc - 4, Hues.White);
					this.owner.TextPainter.ObtainTextWidth(title);
					this.owner.VideoCard.RenderLine(ScreenBox,
						horizLoc + 5, vertLoc + 3, horizLoc + 5, vertLoc + 3, Hues.LightCyan);
				}
			}

			if (MenuFooter != 0)
			{
				ushort savedFont = DrawingBox.TypefaceIdx;
				DrawingBox.TypefaceIdx = 2;

				this.owner.TextPainter.RenderTextToScreen0(GameTexts.TextHelpAvailable,
					(short)MenuRightEdge - 74,
					(short)bottomEdge - 4,
					Hues.LightGreen);

				DrawingBox.TypefaceIdx = savedFont;
			}

			if (MenuMoreBox != 0)
			{
				this.owner.TextPainter.RenderText(0x2fce,
					(ushort)(MenuRightEdge - 0x11),
					(ushort)(bottomEdge - 8),
					11);

				RenderBox(
					(short)MenuRightEdge - 20,
					(short)bottomEdge - 10,
					20, 10, 11);
			}
		}

		private void RenderMenuLines(MenuText text, int horizLoc, int vertLoc, ushort shownChoice)
		{
			byte firstCh = text[0];
			short optionIdx = (firstCh == 0x20 || firstCh == 0x5f) ? (short)0 : (short)-1;

			DrawingBox.ForeHues = MenuFlashHue;

			ushort lineIdx = 0;

			while ((short)lineIdx < (short)MenuLineCount)
			{
				bool drawLine = true;

				if (MenuMeasuring == 0)
				{
					short diff = (short)this.owner.Libc.AbsoluteValue((short)(shownChoice - (ushort)optionIdx));

					if (diff >= 2)
					{
						drawLine = false;
					}
				}

				if (drawLine)
				{
					ushort lineSlot = (ushort)(lineIdx << 1);
					ushort nextLineStart = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress + 2));
					text[nextLineStart - 1] = 0;

					bool checkedOption = optionIdx >= 0 &&
						((1 << (optionIdx & 0x1f)) & MenuOptions) != 0;

					if (checkedOption)
					{
						ushort lineStart = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress));
						text[lineStart] = 0x5e;

						ushort hue = (ushort)(((1 << (optionIdx & 0x1f)) & DisabledLines) != 0 ? 3 : 0);

						this.owner.TextPainter.RenderTextToScreen0(text.From(lineStart),
							horizLoc + 5,
							((short)lineIdx * (short)MenuLineHeight) + vertLoc + 5,
							hue);

						text[this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress))] = 0x20;
					}
					else
					{
						if (optionIdx < 0 && (short)MenuLineHeight > 9)
						{
							this.owner.TextPainter.RenderTextToScreen0(
								text.From(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress))),
								horizLoc + 5,
								((short)lineIdx * (short)MenuLineHeight) + vertLoc + 6,
								Hues.Black);
						}

						ushort hue;

						if (optionIdx < 0)
						{
							hue = MenuFlashHue;
						}
						else
						{
							hue = (ushort)(((1 << (optionIdx & 0x1f)) & DisabledLines) != 0 ? 3 : 0);
						}

						this.owner.TextPainter.RenderTextToScreen0(
							text.From(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress))),
							horizLoc + 5,
							((short)lineIdx * (short)MenuLineHeight) + vertLoc + 5,
							hue);
					}

					ushort endOffset = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(lineSlot + MenuLineOffsetsTableAddress + 2));
					text[endOffset - 1] = 0xa;
				}

				ushort nextOffset = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((lineIdx << 1) + MenuLineOffsetsTableAddress + 2));
				byte nextLineChar = text[nextOffset];

				if (nextLineChar == 0x20 || nextLineChar == 0x5f)
				{
					optionIdx++;
				}

				lineIdx++;
			}
		}

		/// <summary>
		/// Fills the box moved by (1, 1) with the colour (the motif for 7 with BoxMotif), draws its shadow (15 / 8) and a frame
		/// of colour 0 around it
		/// </summary>
		public void FloodBoxWithDoubleShadow(int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			if (style == 7 && BoxMotif != 0)
			{
				this.owner.Helpers.FloodBoxWithMotif(horizLoc + 1, vertLoc + 1, breadth, altitude);
			}
			else
			{
				this.owner.Services.FloodBox(ScreenBox,
					horizLoc + 1, vertLoc + 1, breadth, altitude, style);
			}
		
			DrawingBox.RearHues = style;

			RenderShadowBox(horizLoc + 1, vertLoc + 1, breadth, altitude, 15, 8);

			RenderBox(horizLoc, vertLoc, breadth + 2, altitude + 2, 0);
		}

		/// <summary>
		/// Draws a rectangle
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		public void RenderBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style)
		{
			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizLoc + breadth, vertLoc, style);

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc + altitude, horizLoc + breadth, vertLoc + altitude, style);

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc + breadth, vertLoc, horizLoc + breadth, vertLoc + altitude, style);

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizLoc, vertLoc + altitude, style);
		}

		/// <summary>
		/// Draws a shaddow rectangle
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		/// <param name="mode1"></param>
		public void RenderShadowBox(int horizLoc, int vertLoc, int breadth, int altitude, ushort style, ushort style1)
		{
			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc, horizLoc + breadth, vertLoc, style1);

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc + altitude, horizLoc + breadth, vertLoc + altitude, style);

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc + breadth, vertLoc, horizLoc + breadth, vertLoc + altitude, style1);

			this.owner.VideoCard.RenderLine(ScreenBox,
				horizLoc, vertLoc + 1, horizLoc, vertLoc + altitude, style);
		}

		/// <summary>
		/// Reads a key and gives the navigation keys one code each (the other codes of the arrows, Home and the shifted keys of
		/// the keypad map to the usual ones); any other key with a character loses its scan code (high byte)
		/// </summary>
		public ushort ObtainNavigationKey()
		{
			this.processor.Journal.OpenScope("ObtainNavigationKey()");

			ushort key = (ushort)this.owner.Libc.ReadKey();

			key = key switch
			{
				0x4800 or 0x487e => 0x4800, // Up
				0x4900 => 0x4900, // Page Up
				0x4d00 or 0xf400 => 0x4d00, // Right
				0x5100 => 0x5100, // Page Down
				0x5000 or 0x5060 => 0x5000, // Down
				0x4f00 => 0x4f00, // End
				0x4b00 or 0x4b7c => 0x4b00, // Left
				0x4700 or 0x475c => 0x4700, // Home
				0x4838 => 0x4838, // Shift + Up
				0x4939 => 0x4939, // Shift + Page Up
				0x4d36 or 0x4d46 => 0x4d36, // Shift + Right
				0x5133 => 0x5133, // Shift + Page Down
				0x5032 => 0x5032, // Shift + Down
				0x4f31 => 0x4f31, // Shift + End
				0x4b34 or 0x4b43 => 0x4b34, // Shift + Left
				0x4737 => 0x4737, // Shift + Home
				_ => (key & 0xff) != 0 ? (ushort)(key & 0xff) : key
			};

			this.processor.Journal.CloseScope("ObtainNavigationKey");

			return key;
		}
	}
}
