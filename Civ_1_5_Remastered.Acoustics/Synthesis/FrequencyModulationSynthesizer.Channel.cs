namespace Civ_1_5_Remastered.Acoustics.Synthesis
{
	public partial class FrequencyModulationSynthesizer
	{
		private class Channel
		{
			public int FieldNumeral { get; set; }
			public int Chunk { get; set; }
			public int Response { get; set; }
			public bool NoteOn { get; set; }
			public bool Summing { get; set; }
		}
	}
}
