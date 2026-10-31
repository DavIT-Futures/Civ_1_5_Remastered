using System;

namespace Civilization1Remaster.Acoustics
{
	public partial class Opl2Synth
	{
		private class Channel
		{
			public int FldNumeral { get; set; }
			public int Chunk { get; set; }
			public int Response { get; set; }
			public bool NoteOn { get; set; }
			public bool Summing { get; set; }
		}
	}
}
