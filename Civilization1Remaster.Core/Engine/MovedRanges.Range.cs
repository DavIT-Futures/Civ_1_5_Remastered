using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.Engine
{
	partial class MovedRanges
	{
		public sealed record Range(string Key, ushort Address, int Length);
	}
}
