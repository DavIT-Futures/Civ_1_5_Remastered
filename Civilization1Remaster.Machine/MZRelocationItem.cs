using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Civilization1Remaster.Emulation
{
	public struct MZRelocationItem
	{
		private readonly ushort seg;
		private readonly ushort delta;

		public MZRelocationItem(ushort section, ushort displacement)
		{
			this.seg = section;
			this.delta = displacement;
		}

		public ushort Section
		{
			get { return this.seg; }
		}

		public ushort Displacement
		{
			get { return this.delta; }
		}

		public override string ToString()
		{
			return $"0x{this.seg:x4}:0x{this.delta:x4}";
		}
	}
}
