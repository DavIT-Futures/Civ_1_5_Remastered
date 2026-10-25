using System;
using System.Collections.Generic;

namespace Civilization1Remaster.Emulation
{
	public class CPUMemory
	{
		private static readonly uint FloorUnusedLocation = 0x0;
		private static readonly uint PeakUnusedLocation = 0x9ffff;
		private static readonly uint StorageExtent = 0xa0000;

		private readonly byte[] store = new byte[StorageExtent];
		private readonly List<CPUMemoryRegion> storageZones = new List<CPUMemoryRegion>();

		private readonly CPUMemoryRegion gfxunitZone = new CPUMemoryRegion((ushort)0xa000, 0, 0x10000);
		private readonly CPU processor;

		// B6: ranges of the memory kept in C# arrays of their owners (B6.1 the message buffer, B6.2 the units)
		private readonly List<(uint Begin, byte[] Content)> overlays = new List<(uint Begin, byte[] Content)>();
		// The bounds of all of them: the other accesses skip the search
		private uint overlaysLow = uint.MaxValue;
		private uint overlaysHigh = 0;

		/// <summary>
		/// The bytes from the linear address on live in the array from now on (B6.1: the store of the message buffer moves
		/// into C#): every access of the memory to the range reads and writes the array. The current bytes of the memory are
		/// copied into the array first, the memory under it is cleared.
		/// </summary>
		public void AttachOverlay(uint begin, byte[] content)
		{
			Array.Copy(this.store, begin, content, 0, content.Length);
			Array.Clear(this.store, (int)begin, content.Length);
			this.overlays.Add((begin, content));
			this.overlaysLow = Math.Min(this.overlaysLow, begin);
			this.overlaysHigh = Math.Max(this.overlaysHigh, begin + (uint)content.Length);
		}

		/// <summary>The ranges kept in C# arrays (the tests save and restore them with the memory)</summary>
		public IReadOnlyList<(uint Begin, byte[] Content)> Overlays => this.overlays;

		/// <summary>The array of the range that starts at the linear address, null when the range is still in the memory</summary>
		public byte[]? OverlayAt(uint begin)
		{
			foreach ((uint start, byte[] content) in this.overlays)
			{
				if (start == begin)
					return content;
			}

			return null;
		}

		private byte Peek(uint site)
		{
			if (site < this.overlaysLow || site >= this.overlaysHigh)
				return this.store[site];

			foreach ((uint start, byte[] content) in this.overlays)
			{
				uint place = site - start;

				if (place < (uint)content.Length)
					return content[place];
			}

			return this.store[site];
		}

		private void Poke(uint site, byte value)
		{
			if (site < this.overlaysLow || site >= this.overlaysHigh)
			{
				this.store[site] = value;
				return;
			}

			foreach ((uint start, byte[] content) in this.overlays)
			{
				uint place = site - start;

				if (place < (uint)content.Length)
				{
					content[place] = value;
					return;
				}
			}

			this.store[site] = value;
		}

		/// <summary>Copies bytes of the memory, also of the ranges kept in C# arrays (a file of the game is written from them)</summary>
		public void ReadChunk(uint location, byte[] target, int loc, int span)
		{
			for (int idx = 0; idx < span; idx++)
				target[loc + idx] = Peek((uint)(location + idx));
		}

		public CPUMemory(CPU processor)
		{
			this.processor = processor;
			this.storageZones.Add(new CPUMemoryRegion(0, PeakUnusedLocation, CPUMemoryFlagsEnum.ReadWrite | 
				CPUMemoryFlagsEnum.ReadWarning | CPUMemoryFlagsEnum.WriteWarning));
		}

		public CPU Owner
		{
			get { return this.processor; }
		}

		public byte[] StoragePayload
		{
			get { return this.store; }
		}

		public CPUMemoryRegion UnusedStorage
		{
			get { return this.storageZones[0]; }
		}

		public List<CPUMemoryRegion> StorageZones
		{
			get
			{
				return this.storageZones;
			}
		}

		#region Check Flag
		public bool VerifyFlag(uint location, uint extent, CPUMemoryFlagsEnum marker)
		{
			uint tallyUint = 0;

			for (int idx = 0; idx < extent; idx++)
			{
				bool located = false;
				for (int jdx = 0; jdx < this.storageZones.Count; jdx++)
				{
					if (this.storageZones[jdx].VerifyLimits(location, 1))
					{
						located = true;
						if ((this.storageZones[jdx].PermissionMarkers & marker) == marker)
						{
							tallyUint++;
							break;
						}
					}
				}
				// if address is not found in any region, assume full rights
				if (!located)
					tallyUint++;
			}
			if (tallyUint == extent)
				return true;

			return false;
		}
		#endregion

		#region Read instructions
		#region 8 Bit
		public sbyte PullInt8(ushort section, ushort displacement)
		{
			return (sbyte)this.PullUInt8(CPU.AsLinearAddress(section, displacement));
		}

		public sbyte PullInt8(uint location)
		{
			return (sbyte)this.PullUInt8(location);
		}

		public byte PullUInt8(ushort section, ushort displacement)
		{
			return this.PullUInt8(CPU.AsLinearAddress(section, displacement));
		}

		public byte PullUInt8(uint location)
		{
			if (!this.VerifyFlag(location, 1, CPUMemoryFlagsEnum.Read) || this.VerifyFlag(location, 1, CPUMemoryFlagsEnum.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to read byte from protected area at 0x{location:x8}");
				return 0;
			}

			for (int idx = 0; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, 1))
				{
					if ((this.storageZones[idx].PermissionMarkers & CPUMemoryFlagsEnum.ReadWarning) == CPUMemoryFlagsEnum.ReadWarning)
						this.processor.Journal.EmitLine($"// Warning: Read byte at 0x{location:x8}");

					return Peek(location);
				}
			}

			if (this.gfxunitZone.VerifyLimits(location, 1))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to read byte in VGA memory at address 0x{location:x8}");
				return 0;
			}

			this.processor.Journal.EmitLine($"// Error: Attempt to read byte at undefined address 0x{location:x8}");
			return 0;
		}
		#endregion

		#region 16 bit
		public short PullInt16(ushort section, ushort displacement)
		{
			return (short)this.PullUInt16(CPU.AsLinearAddress(section, displacement));
		}

		public short PullInt16(uint location)
		{
			return (short)this.PullUInt16(location);
		}

		public ushort PullUInt16(ushort section, ushort displacement)
		{
			return this.PullUInt16(CPU.AsLinearAddress(section, displacement));
		}

		public ushort PullUInt16(uint location)
		{
			if (!this.VerifyFlag(location, 2, CPUMemoryFlagsEnum.Read) || this.VerifyFlag(location, 2, CPUMemoryFlagsEnum.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to read word from protected area at 0x{location:x8}");
				return 0;
			}

			for (int idx = 0; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, 2))
				{
					if ((this.storageZones[idx].PermissionMarkers & CPUMemoryFlagsEnum.ReadWarning) == CPUMemoryFlagsEnum.ReadWarning)
						this.processor.Journal.EmitLine($"// Warning: Read word at 0x{location:x8}");

					uint site = location;

					return (ushort)((ushort)Peek(site) |
						(ushort)((ushort)Peek(site + 1) << 8));
				}
			}

			if (this.gfxunitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to read word in VGA memory at address 0x{location:x8}");
				return 0;
			}

			this.processor.Journal.EmitLine($"// Error: Attempt to read word at undefined address 0x{location:x8}");
			return 0;
		}
		#endregion

		#region 32 bit
		public int PullInt32(ushort section, ushort displacement)
		{
			return (int)this.PullUInt32(CPU.AsLinearAddress(section, displacement));
		}

		public int PullInt32(uint location)
		{
			return (int)this.PullUInt32(location);
		}

		public uint PullUInt32(ushort section, ushort displacement)
		{
			return this.PullUInt32(CPU.AsLinearAddress(section, displacement));
		}

		public uint PullUInt32(uint location)
		{
			if (!this.VerifyFlag(location, 4, CPUMemoryFlagsEnum.Read) || this.VerifyFlag(location, 4, CPUMemoryFlagsEnum.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to read dword from protected area at 0x{location:x8}");
				return 0;
			}

			for (int idx = 0; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, 4))
				{
					if ((this.storageZones[idx].PermissionMarkers & CPUMemoryFlagsEnum.ReadWarning) == CPUMemoryFlagsEnum.ReadWarning)
						this.processor.Journal.EmitLine($"// Warning: Read dword at 0x{location:x8}");

					uint site = location;

					return (uint)((uint)Peek(site) |
						(uint)((uint)Peek(site + 1) << 8) |
						(uint)((uint)Peek(site + 2) << 16) |
						(uint)((uint)Peek(site + 3) << 24));
				}
			}

			if (this.gfxunitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to read dword in VGA memory at address 0x{location:x8}");
				return 0;
			}

			this.processor.Journal.EmitLine($"Error: Attempt to read dword at undefined address 0x{location:x8}");
			return 0;
		}
		#endregion
		#endregion

		#region Write instructions
		#region 8 bit
		public void EmitInt8(ushort section, ushort displacement, sbyte amount)
		{
			this.EmitUInt8(CPU.AsLinearAddress(section, displacement), (byte)amount);
		}

		public void EmitInt8(uint location, sbyte amount)
		{
			this.EmitUInt8(location, (byte)amount);
		}

		public void EmitInt8(ushort section, ushort displacement, int amount)
		{
			if (amount < SByte.MinValue || amount > SByte.MaxValue)
				throw new Exception($"Value {amount} out of range for Int8");

			this.EmitUInt8(CPU.AsLinearAddress(section, displacement), (byte)((sbyte)amount));
		}

		public void EmitInt8(uint location, int amount)
		{
			if (amount < SByte.MinValue || amount > SByte.MaxValue)
				throw new Exception($"Value {amount} out of range for Int8");

			this.EmitUInt8(location, (byte)((sbyte)amount));
		}

		public void EmitUInt8(ushort section, ushort displacement, int amount)
		{
			if (amount < 0 || amount > Byte.MaxValue)
				throw new Exception($"Value {amount} out of range for UInt8");

			this.EmitUInt8(CPU.AsLinearAddress(section, displacement), (byte)amount);
		}

		public void EmitUInt8(uint location, int amount)
		{
			if (amount < 0 || amount > Byte.MaxValue)
				throw new Exception($"Value {amount} out of range for UInt8");

			this.EmitUInt8(location, (byte)amount);
		}

		public void EmitUInt8(ushort section, ushort displacement, byte amount)
		{
			this.EmitUInt8(CPU.AsLinearAddress(section, displacement), amount);
		}

		public void EmitUInt8(uint location, byte amount)
		{
			if (!this.VerifyFlag(location, 1, CPUMemoryFlagsEnum.Write) || this.VerifyFlag(location, 1, CPUMemoryFlagsEnum.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to write to protected area at 0x{location:x8}");
				return;
			}

			bool located = false;
			for (int idx = 0; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, 1))
				{
					if ((this.storageZones[idx].PermissionMarkers & CPUMemoryFlagsEnum.WriteWarning) == CPUMemoryFlagsEnum.WriteWarning)
						this.processor.Journal.EmitLine($"// Warning: Write byte at 0x{location:x8}");

					Poke(location, amount);
					located = true;
					break;
				}
			}

			if (this.gfxunitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write byte to VGA memory at address 0x{location:x8}");
				located = true;
			}

			if (!located)
				this.processor.Journal.EmitLine($"// Error: Attempt to write byte at undefined address 0x{location:x8}");
		}
		#endregion

		#region 16 bit
		public void EmitInt16(ushort section, ushort displacement, short amount)
		{
			this.EmitUInt16(CPU.AsLinearAddress(section, displacement), (ushort)amount);
		}

		public void EmitInt16(uint location, short amount)
		{
			this.EmitUInt16(location, (ushort)amount);
		}

		public void EmitInt16(ushort section, ushort displacement, int amount)
		{
			if (amount < Int16.MinValue || amount > Int16.MaxValue)
				throw new Exception($"Value {amount} out of range for Int16");

			this.EmitUInt16(CPU.AsLinearAddress(section, displacement), (ushort)((short)amount));
		}

		public void EmitInt16(uint location, int amount)
		{
			if (amount < Int16.MinValue || amount > Int16.MaxValue)
				throw new Exception($"Value {amount} out of range for Int16");

			this.EmitUInt16(location, (ushort)((short)amount));
		}

		public void EmitUInt16(ushort section, ushort displacement, int amount)
		{
			if (amount < 0 || amount > UInt16.MaxValue)
				throw new Exception($"Value {amount} out of range for UInt16");

			this.EmitUInt16(CPU.AsLinearAddress(section, displacement), (ushort)amount);
		}

		public void EmitUInt16(uint location, int amount)
		{
			if (amount < 0 || amount > UInt16.MaxValue)
				throw new Exception($"Value {amount} out of range for UInt16");

			this.EmitUInt16(location, (ushort)amount);
		}

		public void EmitUInt16(ushort section, ushort displacement, ushort amount)
		{
			this.EmitUInt16(CPU.AsLinearAddress(section, displacement), amount);
		}

		public void EmitUInt16(uint location, ushort amount)
		{
			if (!this.VerifyFlag(location, 2, CPUMemoryFlagsEnum.Write) || this.VerifyFlag(location, 2, CPUMemoryFlagsEnum.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to write to protected area at 0x{location:x8}");
				return;
			}

			bool located = false;
			for (int idx = 0; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, 2))
				{
					if ((this.storageZones[idx].PermissionMarkers & CPUMemoryFlagsEnum.WriteWarning) == CPUMemoryFlagsEnum.WriteWarning)
						this.processor.Journal.EmitLine($"// Warning: Write word at 0x{location:x8}");

					uint site = location;

					Poke(site, (byte)(amount & 0xff));
					Poke(site + 1, (byte)((amount & 0xff00) >> 8));
					located = true;
					break;
				}
			}

			if (this.gfxunitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write word to VGA memory at address 0x{location:x8}");
				located = true;
			}

			if (!located)
				this.processor.Journal.EmitLine($"// Error: Attempt to write word at undefined address 0x{location:x8}");
		}
		#endregion

		#region 32 bit
		public void EmitInt32(ushort section, ushort displacement, int amount)
		{
			this.EmitUInt32(CPU.AsLinearAddress(section, displacement), (uint)amount);
		}

		public void EmitInt32(uint location, int amount)
		{
			this.EmitUInt32(location, (uint)amount);
		}

		public void EmitUInt32(ushort section, ushort displacement, uint amount)
		{
			this.EmitUInt32(CPU.AsLinearAddress(section, displacement), amount);
		}

		public void EmitUInt32(uint location, uint amount)
		{
			if (!this.VerifyFlag(location, 4, CPUMemoryFlagsEnum.Write) || this.VerifyFlag(location, 4, CPUMemoryFlagsEnum.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to write to protected area at 0x{location:x8}");
				return;
			}

			bool located = false;
			for (int idx = 0; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, 4))
				{
					if ((this.storageZones[idx].PermissionMarkers & CPUMemoryFlagsEnum.WriteWarning) == CPUMemoryFlagsEnum.WriteWarning)
						this.processor.Journal.EmitLine($"// Warning: Write dword at 0x{location:x8}");

					uint site = location;

					Poke(site, (byte)(amount & 0xff));
					Poke(site + 1, (byte)((amount & 0xff00) >> 8));
					Poke(site + 2, (byte)((amount & 0xff0000) >> 16));
					Poke(site + 3, (byte)((amount & 0xff000000) >> 24));
					located = true;
					break;
				}
			}
			if (this.gfxunitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write dword to VGA memory at address 0x{location:x8}");
				located = true;
			}

			if (!located)
				this.processor.Journal.EmitLine($"// Error: Attempt to write dword at undefined address 0x{location:x8}");
		}
		#endregion
		#endregion

		#region Memory block instructions
		public void EmitChunk(ushort section, ushort displacement, byte[] originContent, int loc, int span)
		{
			EmitChunk(CPU.AsLinearAddress(section, displacement), originContent, loc, span);
		}

		public void EmitChunk(uint location, byte[] originContent, int loc, int span)
		{
			if (this.overlays.Exists(range => location < range.Begin + range.Content.Length && location + span > range.Begin))
			{
				for (int idx = 0; idx < span; idx++)
					Poke((uint)(location + idx), originContent[loc + idx]);
			}
			else
			{
				Array.Copy(originContent, loc, this.store, location, span);
			}
		}
		#endregion

		#region Memory allocation
		public bool ReserveStorageChunk(uint location, uint extent)
		{
			return ReserveStorageChunk(location, extent, CPUMemoryFlagsEnum.ReadWrite | CPUMemoryFlagsEnum.WriteWarning);
		}

		public bool ReserveStorageChunk(uint location, uint extent, CPUMemoryFlagsEnum markers)
		{
			for (int idx = 1; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].VerifyLimits(location, extent))
				{
					return false;
				}
			}

			this.storageZones.Add(new CPUMemoryRegion(location, extent, markers));

			TuneFreeStorageSpan();

			return true;
		}

		public bool ReserveStorageChunk(ushort sectionTally, out ushort beginSection)
		{
			uint magnitude = CPU.AsLinearAddress(sectionTally, 0);

			// Just allocate next available block, don't search between blocks for now
			// Is there enough room for allocation?
			if (this.storageZones[0].Extent < magnitude)
			{
				beginSection = (ushort)((this.storageZones[0].Extent >> 4) & 0xffff);
				return false;
			}

			// allocate block
			beginSection = (ushort)((this.storageZones[0].Origin >> 4) & 0xffff);
			this.storageZones.Add(new CPUMemoryRegion(this.storageZones[0].Origin, magnitude, CPUMemoryFlagsEnum.ReadWrite | CPUMemoryFlagsEnum.WriteWarning));

			TuneFreeStorageSpan();

			return true;
		}

		public bool RescaleStorageChunk(ushort chunkSection, ushort sectionTally)
		{
			return RescaleStorageChunk(CPU.AsLinearAddress(chunkSection, 0),
				CPU.AsLinearAddress(sectionTally, 0));
		}

		public bool RescaleStorageChunk(uint location, uint extent)
		{
			for (int idx = 1; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].Origin == location)
				{
					// found the block
					// check for overlapping
					for (int jdx = 1; jdx < this.storageZones.Count; jdx++)
					{
						if (jdx != idx && this.storageZones[jdx].VerifyOverlap(location, extent))
							return false;
					}

					this.storageZones[idx].Extent = extent;

					TuneFreeStorageSpan();

					return true;
				}
			}

			return false;
		}

		public bool ReleaseStorageChunk(ushort section)
		{
			uint spot = CPU.AsLinearAddress(section, 0);

			for (int idx = 1; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].Origin == spot)
				{
					// found the block
					this.storageZones.RemoveAt(idx);
					return true;
				}
			}

			return false;
		}

		private void TuneFreeStorageSpan()
		{
			CPUMemoryRegion unused = this.storageZones[0];
			unused.Origin = FloorUnusedLocation;

			// adjust free memory
			for (int idx = 1; idx < this.storageZones.Count; idx++)
			{
				if (this.storageZones[idx].Finish + 1 >= unused.Origin)
				{
					unused.Origin = this.storageZones[idx].Finish + 1;
					unused.Finish = PeakUnusedLocation;
				}
			}
		}
		#endregion
	}
}
