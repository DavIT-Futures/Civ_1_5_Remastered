using System;

namespace Civilization1Remaster.Acoustics
{
	public partial class Opl2Synth
	{
		private class Operator
		{
			public bool Wobble, Quiver, HoldMode, PitchScaleRate;
			public int Scaler, PitchScaleLevel, OverallLevel, OnsetRate, FallRate, HoldLevel, LetGoRate, Shape;
			public double FinalEmitted1, FinalEmitted2;

			public double Stage { get; set; }
			public EnvelopeStage Phase { get; set; } = EnvelopeStage.Off;

			// Attenuation in dB (0 = loudest, 96 = silent)
			public double Shaper { get; set; } = 96.0;

			public bool NoteOn { get; set; }
		}
	}
}
