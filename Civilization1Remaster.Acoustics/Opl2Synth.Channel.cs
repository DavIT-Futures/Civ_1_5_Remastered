using System;

namespace Civilization1Remaster.Acoustics
{
	public partial class Opl2Synth
	{
		private class Channel
		{
			public int FldNumeral, Chunk, Response;
			public bool NoteOn, Summing;
		}
	}
}
