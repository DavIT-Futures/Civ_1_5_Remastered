using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Civilization1Remaster.Game.State
{
	public class Continent
	{
		public short ConstructionSpotTally { get; set; } = 0;
		public short Extent { get; set; } = 0;
		public short SeaExtent { get; set; } = 0;
	}
}
