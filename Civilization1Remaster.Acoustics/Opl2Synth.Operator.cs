namespace Civilization1Remaster.Acoustics
{
	public partial class Opl2Synth
	{
		private class Operator
		{
			public bool Wobble { get; set; }
			public bool Quiver { get; set; }
			public bool HoldMode { get; set; }
			public bool PitchScaleRate { get; set; }
			public int Scaler { get; set; }
			public int PitchScaleLevel { get; set; }
			public int OverallLevel { get; set; }
			public int OnsetRate { get; set; }
			public int FallRate { get; set; }
			public int HoldLevel { get; set; }
			public int LetGoRate { get; set; }
			public int Shape { get; set; }
			public double FinalEmitted1 { get; set; }
			public double FinalEmitted2 { get; set; }

			public double Stage { get; set; }
			public EnvelopeStage Phase { get; set; } = EnvelopeStage.Off;

			// Attenuation in dB (0 = loudest, 96 = silent)
			public double Shaper { get; set; } = 96.0;

			public bool NoteOn { get; set; }
		}
	}
}
