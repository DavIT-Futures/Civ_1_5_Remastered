using System;

namespace Civilization1Remaster.Acoustics
{
	public partial class Opl2Synth
	{
		private class Operator
		{
			public bool Wobble, Quiver, HoldMode, PitchScaleRate;
			public int Scaler, PitchScaleLevel, OverallLevel, OnsetRate, FallRate, HoldLevel, LetGoRate, Shape;

			public double Stage;
			public EnvelopeStage Phase = EnvelopeStage.Off;
			// Attenuation in dB (0 = loudest, 96 = silent)
			public double Shaper = 96.0;
			public double FinalEmitted1, FinalEmitted2;
			public bool NoteOn;
		}
	}
}
