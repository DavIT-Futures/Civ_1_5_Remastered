using System;
using System.Drawing;

namespace Civilization1Remaster
{
	[Flags]
	public enum PointerButtons : ushort
	{
		None = 0,
		Left = 1,
		Right = 2,
		Middle = 4
	}
}
