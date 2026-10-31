using System.Collections.Generic;
using System.Linq;

namespace Civilization1Remaster.Core.Engine
{
	partial class MovedRanges
	{
		public sealed record Range(string Key, ushort Address, int Length);
	}
}
