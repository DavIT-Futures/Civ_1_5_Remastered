using System;
using System.Collections.Generic;
using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.UI;

namespace Civilization1Remaster.Game
{
	partial class WorldEvents
	{
		/// <summary>A table of the sections in the frame words of RenderPalace: section 0 at the given distance below BP</summary>
		private readonly record struct SectionTable(FrameWords Frame, int Distance);
	}
}
