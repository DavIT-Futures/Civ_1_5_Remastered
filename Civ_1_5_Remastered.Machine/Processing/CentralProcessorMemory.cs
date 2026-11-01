using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.Machine.Processing
{
	public class CentralProcessorMemory
	{
		private static readonly uint FloorUnusedLocation = 0x0;
		private static readonly uint PeakUnusedLocation = 0x9ffff;
		private static readonly uint StorageExtent = 0xa0000;

		private readonly byte[] store = new byte[StorageExtent];
		private readonly List<CentralProcessorMemoryRegion> storageZones = new List<CentralProcessorMemoryRegion>();
		private readonly CentralProcessorMemoryRegion graphicsUnitZone = new CentralProcessorMemoryRegion((ushort)0xa000, 0, 0x10000);
		private readonly CentralProcessor processor;
		private readonly Dictionary<uint, IMemoryOverlay> overlayStarts = new Dictionary<uint, IMemoryOverlay>();

		private readonly List<(uint Begin, IMemoryOverlay Overlay)> overlays = new List<(uint Begin, IMemoryOverlay Overlay)>();

		private short[]? overlayMap;

		public CentralProcessorMemory(CentralProcessor processor)
		{
			this.processor = processor;
			this.storageZones.Add(new CentralProcessorMemoryRegion(0, PeakUnusedLocation, CentralProcessorMemoryFlags.ReadWrite |
				CentralProcessorMemoryFlags.ReadWarning | CentralProcessorMemoryFlags.WriteWarning));
		}

		public CentralProcessor Owner
		{
			get { return this.processor; }
		}

		public CentralProcessorMemoryRegion UnusedStorage
		{
			get { return this.storageZones[0]; }
		}

		public List<CentralProcessorMemoryRegion> StorageZones
		{
			get
			{
				return this.storageZones;
			}
		}

		public void AttachOverlay(uint begin, IMemoryOverlay overlay)
		{
			this.overlayMap ??= new short[this.store.Length];

			for (int place = 0; place < overlay.Length; place++)
			{
				if (this.overlayMap[begin + place] != 0)
				{
					throw new InvalidOperationException($"The overlay at 0x{begin:x5} overlaps another one at 0x{begin + place:x5}");
				}

				overlay.Write(place, this.store[begin + place]);
				this.store[begin + place] = 0;
				this.overlayMap[begin + place] = (short)(this.overlays.Count + 1);
			}

			this.overlays.Add((begin, overlay));
			this.overlayStarts[begin] = overlay;
		}

		public void AttachOverlay(uint begin, byte[] content) => AttachOverlay(begin, new ArrayOverlay(content));

		public IMemoryOverlay? OverlayOf(uint begin) => this.overlayStarts.TryGetValue(begin, out IMemoryOverlay? overlay) ? overlay : null;

		public byte[]? OverlayAt(uint begin) => (OverlayOf(begin) as ArrayOverlay)?.Content;

		public void ReadChunk(uint location, byte[] target, int targetOffset, int span)
		{
			for (int index = 0; index < span; index++)
			{
				target[targetOffset + index] = Peek((uint)(location + index));
			}
		}

		public bool VerifyFlag(uint location, uint extent, CentralProcessorMemoryFlags marker)
		{
			uint tallyUnsigned = 0;

			for (int index = 0; index < extent; index++)
			{
				bool located = false;
				for (int innerIndex = 0; innerIndex < this.storageZones.Count; innerIndex++)
				{
					if (this.storageZones[innerIndex].VerifyLimits(location, 1))
					{
						located = true;
						if ((this.storageZones[innerIndex].PermissionMarkers & marker) == marker)
						{
							tallyUnsigned++;
							break;
						}
					}
				}

				if (!located)
				{
					tallyUnsigned++;
				}
			}

			if (tallyUnsigned == extent)
			{
				return true;
			}

			return false;
		}

		public sbyte PullSignedByte(ushort section, ushort displacement)
		{
			return (sbyte)this.PullByte(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public sbyte PullSignedByte(uint location)
		{
			return (sbyte)this.PullByte(location);
		}

		public byte PullByte(ushort section, ushort displacement)
		{
			return this.PullByte(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public byte PullByte(uint location)
		{
			if (!this.VerifyFlag(location, 1, CentralProcessorMemoryFlags.Read) || this.VerifyFlag(location, 1, CentralProcessorMemoryFlags.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to read byte from protected area at 0x{location:x8}");
				return 0;
			}

			for (int index = 0; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, 1))
				{
					if ((this.storageZones[index].PermissionMarkers & CentralProcessorMemoryFlags.ReadWarning) == CentralProcessorMemoryFlags.ReadWarning)
					{
						this.processor.Journal.EmitLine($"// Warning: Read byte at 0x{location:x8}");
					}

					return Peek(location);
				}
			}

			if (this.graphicsUnitZone.VerifyLimits(location, 1))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to read byte in VGA memory at address 0x{location:x8}");
				return 0;
			}

			this.processor.Journal.EmitLine($"// Error: Attempt to read byte at undefined address 0x{location:x8}");
			return 0;
		}

		public short PullSignedWord(ushort section, ushort displacement)
		{
			return (short)this.PullWord(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public short PullSignedWord(uint location)
		{
			return (short)this.PullWord(location);
		}

		public ushort PullWord(ushort section, ushort displacement)
		{
			return this.PullWord(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public ushort PullWord(uint location)
		{
			if (!this.VerifyFlag(location, 2, CentralProcessorMemoryFlags.Read) || this.VerifyFlag(location, 2, CentralProcessorMemoryFlags.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to read word from protected area at 0x{location:x8}");
				return 0;
			}

			for (int index = 0; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, 2))
				{
					if ((this.storageZones[index].PermissionMarkers & CentralProcessorMemoryFlags.ReadWarning) == CentralProcessorMemoryFlags.ReadWarning)
					{
						this.processor.Journal.EmitLine($"// Warning: Read word at 0x{location:x8}");
					}

					uint site = location;

					return (ushort)((ushort)Peek(site) |
						(ushort)((ushort)Peek(site + 1) << 8));
				}
			}

			if (this.graphicsUnitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to read word in VGA memory at address 0x{location:x8}");
				return 0;
			}

			this.processor.Journal.EmitLine($"// Error: Attempt to read word at undefined address 0x{location:x8}");
			return 0;
		}

		public int PullSignedDoubleWord(ushort section, ushort displacement)
		{
			return (int)this.PullDoubleWord(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public int PullSignedDoubleWord(uint location)
		{
			return (int)this.PullDoubleWord(location);
		}

		public uint PullDoubleWord(ushort section, ushort displacement)
		{
			return this.PullDoubleWord(CentralProcessor.AsLinearAddress(section, displacement));
		}

		public uint PullDoubleWord(uint location)
		{
			if (!this.VerifyFlag(location, 4, CentralProcessorMemoryFlags.Read) || this.VerifyFlag(location, 4, CentralProcessorMemoryFlags.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to read dword from protected area at 0x{location:x8}");
				return 0;
			}

			for (int index = 0; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, 4))
				{
					if ((this.storageZones[index].PermissionMarkers & CentralProcessorMemoryFlags.ReadWarning) == CentralProcessorMemoryFlags.ReadWarning)
					{
						this.processor.Journal.EmitLine($"// Warning: Read dword at 0x{location:x8}");
					}

					uint site = location;

					return (uint)((uint)Peek(site) |
						(uint)((uint)Peek(site + 1) << 8) |
						(uint)((uint)Peek(site + 2) << 16) |
						(uint)((uint)Peek(site + 3) << 24));
				}
			}

			if (this.graphicsUnitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to read dword in VGA memory at address 0x{location:x8}");
				return 0;
			}

			this.processor.Journal.EmitLine($"Error: Attempt to read dword at undefined address 0x{location:x8}");
			return 0;
		}

		public void EmitSignedByte(ushort section, ushort displacement, sbyte amount)
		{
			this.EmitByte(CentralProcessor.AsLinearAddress(section, displacement), (byte)amount);
		}

		public void EmitSignedByte(uint location, sbyte amount)
		{
			this.EmitByte(location, (byte)amount);
		}

		public void EmitSignedByte(ushort section, ushort displacement, int amount)
		{
			if (amount < sbyte.MinValue || amount > sbyte.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for Int8");
			}

			this.EmitByte(CentralProcessor.AsLinearAddress(section, displacement), (byte)((sbyte)amount));
		}

		public void EmitSignedByte(uint location, int amount)
		{
			if (amount < sbyte.MinValue || amount > sbyte.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for Int8");
			}

			this.EmitByte(location, (byte)((sbyte)amount));
		}

		public void EmitByte(ushort section, ushort displacement, int amount)
		{
			if (amount < 0 || amount > byte.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for UInt8");
			}

			this.EmitByte(CentralProcessor.AsLinearAddress(section, displacement), (byte)amount);
		}

		public void EmitByte(uint location, int amount)
		{
			if (amount < 0 || amount > byte.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for UInt8");
			}

			this.EmitByte(location, (byte)amount);
		}

		public void EmitByte(ushort section, ushort displacement, byte amount)
		{
			this.EmitByte(CentralProcessor.AsLinearAddress(section, displacement), amount);
		}

		public void EmitByte(uint location, byte amount)
		{
			if (!this.VerifyFlag(location, 1, CentralProcessorMemoryFlags.Write) || this.VerifyFlag(location, 1, CentralProcessorMemoryFlags.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to write to protected area at 0x{location:x8}");
				return;
			}

			bool located = false;
			for (int index = 0; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, 1))
				{
					if ((this.storageZones[index].PermissionMarkers & CentralProcessorMemoryFlags.WriteWarning) == CentralProcessorMemoryFlags.WriteWarning)
					{
						this.processor.Journal.EmitLine($"// Warning: Write byte at 0x{location:x8}");
					}

					Poke(location, amount);
					located = true;
					break;
				}
			}

			if (this.graphicsUnitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write byte to VGA memory at address 0x{location:x8}");
				located = true;
			}

			if (!located)
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write byte at undefined address 0x{location:x8}");
			}
		}

		public void EmitSignedWord(ushort section, ushort displacement, short amount)
		{
			this.EmitWord(CentralProcessor.AsLinearAddress(section, displacement), (ushort)amount);
		}

		public void EmitSignedWord(uint location, short amount)
		{
			this.EmitWord(location, (ushort)amount);
		}

		public void EmitSignedWord(ushort section, ushort displacement, int amount)
		{
			if (amount < short.MinValue || amount > short.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for Int16");
			}

			this.EmitWord(CentralProcessor.AsLinearAddress(section, displacement), (ushort)((short)amount));
		}

		public void EmitSignedWord(uint location, int amount)
		{
			if (amount < short.MinValue || amount > short.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for Int16");
			}

			this.EmitWord(location, (ushort)((short)amount));
		}

		public void EmitWord(ushort section, ushort displacement, int amount)
		{
			if (amount < 0 || amount > ushort.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for UInt16");
			}

			this.EmitWord(CentralProcessor.AsLinearAddress(section, displacement), (ushort)amount);
		}

		public void EmitWord(uint location, int amount)
		{
			if (amount < 0 || amount > ushort.MaxValue)
			{
				throw new Exception($"Value {amount} out of range for UInt16");
			}

			this.EmitWord(location, (ushort)amount);
		}

		public void EmitWord(ushort section, ushort displacement, ushort amount)
		{
			this.EmitWord(CentralProcessor.AsLinearAddress(section, displacement), amount);
		}

		public void EmitWord(uint location, ushort amount)
		{
			if (!this.VerifyFlag(location, 2, CentralProcessorMemoryFlags.Write) || this.VerifyFlag(location, 2, CentralProcessorMemoryFlags.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to write to protected area at 0x{location:x8}");
				return;
			}

			bool located = false;
			for (int index = 0; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, 2))
				{
					if ((this.storageZones[index].PermissionMarkers & CentralProcessorMemoryFlags.WriteWarning) == CentralProcessorMemoryFlags.WriteWarning)
					{
						this.processor.Journal.EmitLine($"// Warning: Write word at 0x{location:x8}");
					}

					uint site = location;

					Poke(site, (byte)(amount & 0xff));
					Poke(site + 1, (byte)((amount & 0xff00) >> 8));
					located = true;
					break;
				}
			}

			if (this.graphicsUnitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write word to VGA memory at address 0x{location:x8}");
				located = true;
			}

			if (!located)
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write word at undefined address 0x{location:x8}");
			}
		}

		public void EmitSignedDoubleWord(ushort section, ushort displacement, int amount)
		{
			this.EmitDoubleWord(CentralProcessor.AsLinearAddress(section, displacement), (uint)amount);
		}

		public void EmitSignedDoubleWord(uint location, int amount)
		{
			this.EmitDoubleWord(location, (uint)amount);
		}

		public void EmitDoubleWord(ushort section, ushort displacement, uint amount)
		{
			this.EmitDoubleWord(CentralProcessor.AsLinearAddress(section, displacement), amount);
		}

		public void EmitDoubleWord(uint location, uint amount)
		{
			if (!this.VerifyFlag(location, 4, CentralProcessorMemoryFlags.Write) || this.VerifyFlag(location, 4, CentralProcessorMemoryFlags.AccessNotAllowed))
			{
				this.processor.Journal.EmitLine($"// Attempt to write to protected area at 0x{location:x8}");
				return;
			}

			bool located = false;
			for (int index = 0; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, 4))
				{
					if ((this.storageZones[index].PermissionMarkers & CentralProcessorMemoryFlags.WriteWarning) == CentralProcessorMemoryFlags.WriteWarning)
					{
						this.processor.Journal.EmitLine($"// Warning: Write dword at 0x{location:x8}");
					}

					uint site = location;

					Poke(site, (byte)(amount & 0xff));
					Poke(site + 1, (byte)((amount & 0xff00) >> 8));
					Poke(site + 2, (byte)((amount & 0xff0000) >> 16));
					Poke(site + 3, (byte)((amount & 0xff000000) >> 24));
					located = true;
					break;
				}
			}

			if (this.graphicsUnitZone.VerifyLimits(location))
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write dword to VGA memory at address 0x{location:x8}");
				located = true;
			}

			if (!located)
			{
				this.processor.Journal.EmitLine($"// Error: Attempt to write dword at undefined address 0x{location:x8}");
			}
		}

		public void EmitChunk(ushort section, ushort displacement, byte[] originContent, int location, int span)
		{
			EmitChunk(CentralProcessor.AsLinearAddress(section, displacement), originContent, location, span);
		}

		public void EmitChunk(uint location, byte[] originContent, int sourceOffset, int span)
		{
			if (TouchesOverlay(location, span))
			{
				for (int index = 0; index < span; index++)
				{
					Poke((uint)(location + index), originContent[sourceOffset + index]);
				}
			}
			else
			{
				Array.Copy(originContent, sourceOffset, this.store, location, span);
			}
		}

		public bool ReserveStorageChunk(uint location, uint extent)
		{
			return ReserveStorageChunk(location, extent, CentralProcessorMemoryFlags.ReadWrite | CentralProcessorMemoryFlags.WriteWarning);
		}

		public bool ReserveStorageChunk(uint location, uint extent, CentralProcessorMemoryFlags markers)
		{
			for (int index = 1; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].VerifyLimits(location, extent))
				{
					return false;
				}
			}

			this.storageZones.Add(new CentralProcessorMemoryRegion(location, extent, markers));

			TuneFreeStorageSpan();

			return true;
		}

		public bool ReserveStorageChunk(ushort sectionTally, out ushort beginSection)
		{
			uint magnitude = CentralProcessor.AsLinearAddress(sectionTally, 0);

			if (this.storageZones[0].Extent < magnitude)
			{
				beginSection = (ushort)((this.storageZones[0].Extent >> 4) & 0xffff);
				return false;
			}

			beginSection = (ushort)((this.storageZones[0].Origin >> 4) & 0xffff);
			this.storageZones.Add(new CentralProcessorMemoryRegion(this.storageZones[0].Origin, magnitude, CentralProcessorMemoryFlags.ReadWrite | CentralProcessorMemoryFlags.WriteWarning));

			TuneFreeStorageSpan();

			return true;
		}

		public bool RescaleStorageChunk(ushort chunkSection, ushort sectionTally)
		{
			return RescaleStorageChunk(CentralProcessor.AsLinearAddress(chunkSection, 0),
				CentralProcessor.AsLinearAddress(sectionTally, 0));
		}

		public bool RescaleStorageChunk(uint location, uint extent)
		{
			for (int index = 1; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].Origin == location)
				{
					for (int innerIndex = 1; innerIndex < this.storageZones.Count; innerIndex++)
					{
						if (innerIndex != index && this.storageZones[innerIndex].VerifyOverlap(location, extent))
						{
							return false;
						}
					}

					this.storageZones[index].Extent = extent;

					TuneFreeStorageSpan();

					return true;
				}
			}

			return false;
		}

		public bool ReleaseStorageChunk(ushort section)
		{
			uint spot = CentralProcessor.AsLinearAddress(section, 0);

			for (int index = 1; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].Origin == spot)
				{
					this.storageZones.RemoveAt(index);
					return true;
				}
			}

			return false;
		}

		private byte Peek(uint site)
		{
			int owner = this.overlayMap == null ? 0 : this.overlayMap[site];

			if (owner == 0)
			{
				return this.store[site];
			}

			(uint start, IMemoryOverlay overlay) = this.overlays[owner - 1];
			return overlay.Read((int)(site - start));
		}

		private void Poke(uint site, byte value)
		{
			int owner = this.overlayMap == null ? 0 : this.overlayMap[site];

			if (owner == 0)
			{
				this.store[site] = value;
				return;
			}

			(uint start, IMemoryOverlay overlay) = this.overlays[owner - 1];
			overlay.Write((int)(site - start), value);
		}

		private bool TouchesOverlay(uint location, int span)
		{
			if (this.overlayMap == null)
			{
				return false;
			}

			for (int index = 0; index < span; index++)
			{
				if (this.overlayMap[location + index] != 0)
				{
					return true;
				}
			}

			return false;
		}

		private void TuneFreeStorageSpan()
		{
			CentralProcessorMemoryRegion unused = this.storageZones[0];
			unused.Origin = FloorUnusedLocation;

			for (int index = 1; index < this.storageZones.Count; index++)
			{
				if (this.storageZones[index].Finish + 1 >= unused.Origin)
				{
					unused.Origin = this.storageZones[index].Finish + 1;
					unused.Finish = PeakUnusedLocation;
				}
			}
		}
	}
}
