using System;
using System.Drawing;
using System.IO;

namespace Civilization1Remaster.Core.Engine
{
	/// <summary>The operations of PlatformServices that the other parts of the game use</summary>
	public interface IPlatformServices
	{
		/// <summary>
		/// Starts the timer (0x5a = 1) unless it runs already: the beat counters (0x4c, 0x54), the time (0x42) and the
		/// calibration (PrepareTimer), then the clock interrupt is allowed. AL = the old 0x5a, like the original.
		/// </summary>
		void LaunchTimer();

		/// <summary>
		/// Stops the timer (0x5a = 0); AL = the old 0x5a, like the original
		/// </summary>
		void HaltTimer();

		/// <summary>
		/// Timer function, it should fire appoximately every 20-25 ms
		/// </summary>
		void ClockInterrupt();

		/// <summary>
		/// The timer calibration. The original measures the timer 16 times (100 - 200 the first time, 200 - 200 after), so in
		/// the emulator it always adds 0xff9c to the time (0x42, 32 bit), gets the rate 0 (not 4 - 6) and falls back to the
		/// default: the beat length 0x4dae (0x50), the rate 5 (0x4e, also 0x4c unless it is 1), the beat counters
		/// 0x46 = 0x48 = 0x4dae / 0x4c; 0x58 = 0, 0x59 = 1. The registers as the original leaves them.
		/// </summary>
		void PrepareTimer();

		/// <summary>
		/// Reset timer count
		/// </summary>
		/// <returns></returns>
		ushort RestoreTimer();

		/// <summary>
		/// A beat of the timer (from ClockInterrupt): the beat counter 0x5c, the colour fade and cycles, then the music. With the
		/// rate 4 (0x4e) the music skips every 7th beat (counter 0x5e). The music step tells a new speed: below 0 the normal one
		/// (rate 1, beat length 0x50), above 0 the rate 0x4e (beat length 0x50 / rate); the beat counters 0x46 and 0x59 restart.
		/// The memory accesses are those of the original (the beats of the test timer depend on them).
		/// </summary>
		void OnClockBeat();

		/// <summary>
		/// Sets the colour cycle 1 - 8 (a record of the segment 0x35cf, see ActivateHueCycle) to the colours first - last (at
		/// most 16), with the delay in beats; the current colours are kept twice in the record, so that a cycle is one copy.
		/// Also writes 0xffff to 0x35cf:0. The registers as the original leaves them.
		/// </summary>
		void AssignHueCycleSpan(ushort cycleNumber, ushort delay, ushort firstHue, ushort lastHue);

		/// <summary>
		/// Starts the colour cycle 1 - 8 (the cycle records of the segment 0x35cf), forwards for a positive number, backwards
		/// for a negative one (step 3 or -3), from its first colour
		/// </summary>
		void ActivateHueCycle(ushort cycleNumber);

		/// <summary>
		/// Stops the colour cycle 1 - 8
		/// </summary>
		void DeactivateHueCycle(ushort cycleNumber);

		/// <summary>
		/// One step of every active colour cycle whose delay has run out (the cycle records of the segment 0x35cf)
		/// </summary>
		void RefreshHueCycling();

		/// <summary>
		/// Fades the colours to the palette of a picture (the 0x300 colour bytes after the 6 byte header at swatchAddr) in
		/// steps * 6 timer beats and waits until the fade is done
		/// </summary>
		void LoadColorsFromBuffer(ushort steps, ushort swatchAddr);

		/// <summary>
		/// Fades all 256 colours to one colour (red, green, blue: 0 - 63) in steps * 6 timer beats and waits until the fade
		/// is done
		/// </summary>
		void FillColors(ushort steps, byte red, byte green, byte blue);

		/// <summary>
		/// Starts a fade from the colours at 0xbd06 to the colours at TextBuffer.Address in steps * 6 timer beats (0x68): the colours per
		/// beat (0x64, from the measured speed 0x66 / 14, at most 0x100), the first colour (0x62), the mix step (0x6c) and the
		/// mix (0x6a); the first step is done at once
		/// </summary>
		void FadeColors(ushort steps);

		/// <summary>Copies the 256 colours of screen 0 to 0xbd06 (red, green, blue of 6 bits), the start of a fade</summary>
		void PullCurrentColors();

		/// <summary>
		/// Measures how fast the colours can be set (the first 10 colours from 0xbd06); returns the speed, always the highest
		/// (65530)
		/// </summary>
		ushort GaugeColorsPerformance();

		/// <summary>
		/// The first step of a colour fade (FadeColors); see MixFadeColours
		/// </summary>
		void ColorsFadeStep();

		/// <summary>
		/// A step of a colour fade from the timer (AssignColors); see MixFadeColours
		/// </summary>
		void ColorsFadeStepInterrupt();

		/// <summary>
		/// A step of a fade: while beats are left (HueMixerBeats), the colours from 0xc006 go to screen 0 from the first colour
		/// of the step on, then the next step is mixed
		/// </summary>
		void AssignColors();

		/// <summary>Whether the file named at the address is in the folder of the game: 0, else 0xffff</summary>
		ushort PathPresent(ushort documentLabelAddr);

		/// <summary>
		/// Copies the screen buffer to the screen (over the mouse pointer) and clears the pointer latch (0x5402) when the
		/// pointer is drawn
		/// </summary>
		void RecoverScreenUnderCursor();

		/// <summary>
		/// Only clears the pointer latch (0x5402) when the pointer is drawn: the pointer itself is drawn by the host, the
		/// position and the picture are not used
		/// </summary>
		void DrawCursorToScreen(short horizLoc, short vertLoc, ushort picture);

		/// <summary>
		/// Copies the screen buffer to the main screen and clears the pointer latch (0x5402) when the pointer is drawn
		/// </summary>
		/// <param name="displayIdx">The screen buffer</param>
		void ShowScreen(ushort displayIdx);

		/// <summary>
		/// Draws a picture on the screen and clears the pointer latch (0x5402) when the pointer is drawn
		/// </summary>
		void DrawPictureToScreenAlternate(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr);

		/// <summary>Draws the picture into the box at (horizLoc, vertLoc)</summary>
		void DrawPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr);

		/// <summary>Prepares the sound driver</summary>
		void PrepareSoundDriver();

		/// <summary>
		/// Sound function, possibly: Play Music
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		void PerformSound(ushort audio, ushort argument);

		/// <summary>
		/// Plays the sound of chopping wood when a city is founded, if the sound is on
		/// </summary>
		void PerformCityFoundedSound();

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		ushort HaltSound();

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		ushort AdvanceSong();

		/// <summary>
		/// Sound function, possibly advance sound buffer
		/// </summary>
		void RefreshSoundEffects();

		/// <summary>
		/// Sound function
		/// </summary>
		/// <returns></returns>
		ushort ReadSongPosition();

		/// <summary>
		/// Fills the rectangle with color
		/// </summary>
		/// <param name="rectPtr"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="mode"></param>
		void FloodBox(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, ushort style);

		/// <summary>
		/// Sets a pixel on screen set by structure at 0xaa
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		void AssignDot(int horizLoc, int vertLoc, ushort style);

		/// <summary>
		/// Sets a pixel on selected screen
		/// </summary>
		/// <param name="screenID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="mode"></param>
		void AssignDot(ushort displayIdx, int horizLoc, int vertLoc, ushort style);

		/// <summary>
		/// Resets and inits the mouse subsystem
		/// </summary>
		/// <returns>true if mouse is detected</returns>
		ushort PrepareMouse();

		/// <summary>
		/// Turns the mouse driver off (the flag 0x587d cleared; when it was on, AX -1 and BX 2, as the original leaves them)
		/// </summary>
		void DeactivateMouse();

		/// <summary>The picture of the mouse pointer and its hot spot</summary>
		void AssignMouseCursorImage(ushort hotHoriz, ushort hotVert, ushort picture);

		/// <summary>
		/// Moves the mouse pointer to the position (0x586e, 0x5870)
		/// </summary>
		void AssignMousePosition(ushort horizLoc, ushort vertLoc);

		/// <summary>The clicks since the last call (cleared); the interrupts are served first</summary>
		ushort TakeMouseClick();

		/// <summary>
		/// Draws the mouse pointer (picture 0x5876, hot spot 0x5878, 0x587a) at the mouse position (0x586e, 0x5870), when there
		/// is a picture and the pointer is not drawn yet (0x5403)
		/// </summary>
		void DrawMouseCursor();

		/// <summary>
		/// Puts back the screen under the mouse pointer, when the pointer has a picture (0x5876) and is drawn (0x5403)
		/// </summary>
		void BlankMouseCursor();

		/// <summary>
		/// Copies the screen buffer to the window (from the mouse interrupt; the registers kept outside the emulated stack)
		/// </summary>
		void RenewScreen();

		/// <summary>
		/// Handles mouse events
		/// </summary>
		void PointerEvent();
	}
}
