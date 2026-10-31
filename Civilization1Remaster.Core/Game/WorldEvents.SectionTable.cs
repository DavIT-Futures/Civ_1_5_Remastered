using System;
using System.Collections.Generic;
using System.Drawing;

namespace Civilization1Remaster.Core.Game
{
	partial class WorldEvents
	{
		/// <summary>A table of the sections in the frame words of DrawPalace: section 0 at the given distance below BP</summary>
		private readonly record struct SectionTable(FrameWords Frame, int Distance);
	}
}
