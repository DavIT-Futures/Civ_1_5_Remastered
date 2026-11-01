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

		void LoadColorsFromBuffer(ushort steps, ushort swatchAddress);

		void FillColors(ushort steps, byte red, byte green, byte blue);

		void FadeColors(ushort steps);

		void PullCurrentColors();

		ushort GaugeColorsPerformance();

		void ColorsFadeStep();

		void ColorsFadeStepInterrupt();

		void AssignColors();

		ushort PathPresent(ushort documentLabelAddress);

		void RecoverScreenUnderCursor();

		void DrawCursorToScreen(short horizontalLocation, short verticalLocation, ushort picture);

		void ShowScreen(ushort displayIndex);

		void DrawPictureToScreenAlternate(ushort boxAddress, int horizontalLocation, int verticalLocation, ushort pictureAddress);

		void DrawPictureToScreen(ushort boxAddress, int horizontalLocation, int verticalLocation, ushort pictureAddress);

		void PrepareSoundDriver();

		void PerformSound(ushort audio, ushort argument);

		void PerformCityFoundedSound();

		ushort HaltSound();

		ushort AdvanceSong();

		void RefreshSoundEffects();

		ushort ReadSongPosition();

		void FloodBox(ushort boxAddress, int horizontalLocation, int verticalLocation, int breadth, int altitude, ushort style);

		void AssignDot(int horizontalLocation, int verticalLocation, ushort style);

		void AssignDot(ushort displayIndex, int horizontalLocation, int verticalLocation, ushort style);

		ushort PrepareMouse();

		void DeactivateMouse();

		void AssignMouseCursorImage(ushort hotHorizontal, ushort hotVertical, ushort picture);

		void AssignMousePosition(ushort horizontalLocation, ushort verticalLocation);

		ushort TakeMouseClick();

		void DrawMouseCursor();

		void BlankMouseCursor();

		void RenewScreen();

		void PointerEvent();
	}
}
