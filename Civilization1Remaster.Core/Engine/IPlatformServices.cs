namespace Civilization1Remaster.Core.Engine
{
	public interface IPlatformServices
	{
		void LaunchTimer();

		void HaltTimer();

		void ClockInterrupt();

		void PrepareTimer();

		ushort RestoreTimer();

		void OnClockBeat();

		void AssignHueCycleSpan(ushort cycleNumber, ushort delay, ushort firstHue, ushort lastHue);

		void ActivateHueCycle(ushort cycleNumber);

		void DeactivateHueCycle(ushort cycleNumber);

		void RefreshHueCycling();

		void LoadColorsFromBuffer(ushort steps, ushort swatchAddr);

		void FillColors(ushort steps, byte red, byte green, byte blue);

		void FadeColors(ushort steps);

		void PullCurrentColors();

		ushort GaugeColorsPerformance();

		void ColorsFadeStep();

		void ColorsFadeStepInterrupt();

		void AssignColors();

		ushort PathPresent(ushort documentLabelAddr);

		void RecoverScreenUnderCursor();

		void DrawCursorToScreen(short horizLoc, short vertLoc, ushort picture);

		void ShowScreen(ushort displayIdx);

		void DrawPictureToScreenAlternate(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr);

		void DrawPictureToScreen(ushort boxAddr, int horizLoc, int vertLoc, ushort pictureAddr);

		void PrepareSoundDriver();

		void PerformSound(ushort audio, ushort argument);

		void PerformCityFoundedSound();

		ushort HaltSound();

		ushort AdvanceSong();

		void RefreshSoundEffects();

		ushort ReadSongPosition();

		void FloodBox(ushort boxAddr, int horizLoc, int vertLoc, int breadth, int altitude, ushort style);

		void AssignDot(int horizLoc, int vertLoc, ushort style);

		void AssignDot(ushort displayIdx, int horizLoc, int vertLoc, ushort style);

		ushort PrepareMouse();

		void DeactivateMouse();

		void AssignMouseCursorImage(ushort hotHoriz, ushort hotVert, ushort picture);

		void AssignMousePosition(ushort horizLoc, ushort vertLoc);

		ushort TakeMouseClick();

		void DrawMouseCursor();

		void BlankMouseCursor();

		void RenewScreen();

		void PointerEvent();
	}
}
