namespace Civilization1Remaster.Machine
{
	public class CentralProcessorMemoryRegion
	{
		private uint begin;
		private uint magnitude;
		private CentralProcessorMemoryFlags permissionMarkers;

		public CentralProcessorMemoryRegion(ushort section, ushort displacement, uint extent)
			: this(CentralProcessor.AsLinearAddress(section, displacement), extent, CentralProcessorMemoryFlags.ReadWrite)
		{
		}

		public CentralProcessorMemoryRegion(ushort section, ushort displacement, uint extent, CentralProcessorMemoryFlags permission)
			: this(CentralProcessor.AsLinearAddress(section, displacement), extent, permission)
		{
		}

		public CentralProcessorMemoryRegion(uint begin, uint extent)
			: this(begin, extent, CentralProcessorMemoryFlags.ReadWrite)
		{
		}

		public CentralProcessorMemoryRegion(uint begin, uint extent, CentralProcessorMemoryFlags permission)
		{
			this.begin = begin;
			this.magnitude = extent;
			this.permissionMarkers = permission;
		}

		public CentralProcessorMemoryFlags PermissionMarkers
		{
			get { return permissionMarkers; }
			set { permissionMarkers = value; }
		}

		public uint Origin
		{
			get
			{
				return this.begin;
			}

			set
			{
				this.begin = value;
			}
		}

		public uint Extent
		{
			get
			{
				return this.magnitude;
			}

			set
			{
				this.magnitude = value;
			}
		}

		public uint Finish
		{
			get
			{
				return (uint)(this.begin + this.magnitude - 1);
			}

			set
			{
				this.magnitude = (value - this.begin) + 1;
			}
		}

		public bool VerifyLimits(ushort section, ushort displacement)
		{
			return this.VerifyLimits(CentralProcessor.AsLinearAddress(section, displacement), 1);
		}

		public bool VerifyLimits(ushort section, ushort displacement, uint extent)
		{
			return this.VerifyLimits(CentralProcessor.AsLinearAddress(section, displacement), extent);
		}

		public bool VerifyLimits(uint location)
		{
			return this.VerifyLimits(location, 1);
		}

		public bool VerifyLimits(uint location, uint extent)
		{
			if (location >= this.begin && ((location + extent) - 1) < (this.begin + this.magnitude))
			{
				return true;
			}

			return false;
		}

		public bool VerifyOverlap(ushort section, ushort displacement, uint extent)
		{
			return this.VerifyOverlap(CentralProcessor.AsLinearAddress(section, displacement), extent);
		}

		public bool VerifyOverlap(uint location, uint extent)
		{
			if ((location >= this.begin && location < this.begin + this.magnitude) ||
				(location <= (this.begin + this.magnitude - 1) && (location + extent - 1) >= this.begin))
			{
				return true;
			}

			return false;
		}

		public uint TranslateAddress(ushort section, ushort displacement)
		{
			return this.TranslateAddress(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public uint TranslateAddress(uint location)
		{
			return (uint)(location - this.begin);
		}

		public override string ToString()
		{
			return $"0x{this.Origin:x8} - {this.Finish:x8} ({this.Extent:x8})";
		}
	}
}
