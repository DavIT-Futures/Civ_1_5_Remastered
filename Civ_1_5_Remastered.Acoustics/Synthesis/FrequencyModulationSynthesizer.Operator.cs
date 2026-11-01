namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public partial class FrequencyModulationSynthesizer
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
			public int ReleaseRate { get; set; }
			public int Shape { get; set; }
			public double FirstFinalEmitted { get; set; }
			public double SecondFinalEmitted { get; set; }

			public double Stage { get; set; }
			public EnvelopeStage Phase { get; set; } = EnvelopeStage.Off;

			public double Shaper { get; set; } = 96.0;

			public bool NoteOn { get; set; }
		}
	}
}
