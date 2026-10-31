using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Remaster.Machine
{
	public class MZExecutable
	{
		// relocation data
		private readonly List<MZRelocationItem> rebases = new List<MZRelocationItem>();

		// overlays
		private readonly List<MZExecutable> layers = new List<MZExecutable>();

		// 0x00 - Signature, word (0x5A4D (ASCII for 'M' and 'Z'))
		private ushort stamp = 0;

		// 0x0A - Minimum allocation, word (The number of paragraphs required by the program, excluding the PSP and program image. If no free block is big enough, the loading stops.)
		private ushort lowestReservation = 0;

		// 0x0C - Maximum allocation, word (The number of paragraphs requested by the program. If no free block is big enough, the biggest one possible is allocated.)
		private ushort highestReservation = 0;

		// 0x0E - Initial SS, word (Relocatable segment address for SS.)
		private ushort startingStackseg = 0;

		// 0x10 - Initial SP, word (Initial value for SP.)
		private ushort startingStackptr = 0;

		// 0x14 - Initial IP, word (Initial value for IP.)
		private ushort startingInstrptr = 0;

		// 0x16 - Initial CS, word (Relocatable segment address for CS.)
		private ushort startingCodeseg = 0;

		// 0x1A - Overlay, word (Value used for overlay management. If zero, this is the main executable.)
		private ushort layerOrdinal = 0;

		// 0x1C - Overlay information, word (Files sometimes contain extra information for the main's program overlay management.)
		// always 1 in thiscase
		private ushort layerIdx = 0;

		// actual code or data
		private byte[] content = new byte[0];

		public MZExecutable(string route)
			: this(new MemoryStream(File.ReadAllBytes(route)))
		{ }

		public MZExecutable(Stream flow)
		{
			int documentPlace = 0;
			documentPlace += PullPreamble(flow, documentPlace, this);

			// load overlays
			while (documentPlace < flow.Length)
			{
				MZExecutable layer = new MZExecutable();
				flow.Seek(documentPlace, SeekOrigin.Begin);
				documentPlace += PullPreamble(flow, documentPlace, layer);

				this.layers.Add(layer);
			}
		}

		protected MZExecutable()
		{ }

		public ushort Stamp
		{
			get { return this.stamp; }
		}

		public ushort LowestReservation
		{
			get { return this.lowestReservation; }
			set { this.lowestReservation = value; }
		}

		public ushort HighestReservation
		{
			get { return this.highestReservation; }
			set { this.highestReservation = value; }
		}

		public ushort StartingStackseg
		{
			get { return this.startingStackseg; }
			set { this.startingStackseg = value; }
		}

		public ushort StartingStackptr
		{
			get { return this.startingStackptr; }
			set { this.startingStackptr = value; }
		}

		public ushort StartingInstrptr
		{
			get { return this.startingInstrptr; }
			set { this.startingInstrptr = value; }
		}

		public ushort StartingCodeseg
		{
			get { return this.startingCodeseg; }
			set { this.startingCodeseg = value; }
		}

		public ushort LayerOrdinal
		{
			get { return this.layerOrdinal; }
			set { this.layerOrdinal = value; }
		}

		public ushort LayerIdx
		{
			get { return this.layerIdx; }
			set { this.layerIdx = value; }
		}

		public byte[] Content
		{
			get { return this.content; }
			set { this.content = value; }
		}

		public List<MZRelocationItem> Rebases
		{
			get { return this.rebases; }
		}

		public List<MZExecutable> Layers
		{
			get { return this.layers; }
		}

		public void EmitToFile(string route)
		{
			using FileStream sink = new FileStream(route, FileMode.Create);
			this.EmitToFile(sink);
		}

		public void EmitToFile(Stream flow)
		{
			MemoryStream sink = new MemoryStream();
			sink.EmitUInt16(this.stamp);
			uint spanUint = (uint)this.content.Length;
			uint headingMagnitude = (uint)(0x1e + this.rebases.Count * 4);
			CPU.SnapToSegment(ref headingMagnitude);
			spanUint += headingMagnitude;
			uint sheetsUint = spanUint / 512;
			uint additionalOctets = spanUint - (sheetsUint * 512);
			if (additionalOctets > 0)
				sheetsUint++;
			sink.EmitUInt16((ushort)additionalOctets);
			sink.EmitUInt16((ushort)sheetsUint);
			sink.EmitUInt16((ushort)this.rebases.Count);
			headingMagnitude >>= 4;
			sink.EmitUInt16((ushort)headingMagnitude);
			sink.EmitUInt16(this.lowestReservation);
			sink.EmitUInt16(this.highestReservation);
			sink.EmitUInt16(this.startingStackseg);
			sink.EmitUInt16(this.startingStackptr);
			uint digest = 0;
			sink.EmitUInt16((ushort)digest);
			sink.EmitUInt16(this.startingInstrptr);
			sink.EmitUInt16(this.startingCodeseg);
			sink.EmitUInt16(0x1e);
			sink.EmitUInt16(this.layerOrdinal);
			sink.EmitUInt16(this.layerIdx);

			// write relocations
			for (int idx = 0; idx < this.rebases.Count; idx++)
			{
				MZRelocationItem rebase = this.rebases[idx];
				sink.EmitUInt16(rebase.Displacement);
				sink.EmitUInt16(rebase.Section);
			}

			// append to 16 byte boundary
			uint attach = (uint)((headingMagnitude << 4) - (0x1e + this.rebases.Count * 4));
			for (int idx = 0; idx < attach; idx++)
			{
				sink.WriteByte(0);
			}

			// write data
			sink.Write(this.content, 0, this.content.Length);

			// append to full 512 byte page
			attach = 512 - additionalOctets;
			for (int idx = 0; idx < attach; idx++)
			{
				sink.WriteByte(0);
			}
			sink.Flush();

			// calculate checksum
			byte[] staging = sink.ToArray();
			sink.Seek(0, SeekOrigin.Begin);
			for (int idx = 0; idx < staging.Length; idx += 2)
			{
				digest += sink.PullUInt16();
			}
			digest &= 0xffff;
			digest = 0x10000 - digest;
			sink.Seek(0x12, SeekOrigin.Begin);
			sink.EmitUInt16((ushort)digest);

			sink.Seek(staging.Length, SeekOrigin.Begin);

			// write overlays
			for (int idx = 0; idx < this.layers.Count; idx++)
			{
				this.layers[idx].EmitToFile(sink);
			}

			sink.Flush();

			staging = sink.ToArray();
			flow.Write(staging, 0, staging.Length);

			sink.Dispose();
		}

		public void EnactRelocations(ushort section)
		{
			for (int idx = 0; idx < this.rebases.Count; idx++)
			{
				MZRelocationItem rebase = this.rebases[idx];
				uint spot = CPU.AsLinearAddress((ushort)rebase.Section, (ushort)rebase.Displacement);
				Rebase(this.content.AsSpan((int)spot, 2), section);
			}

			for (int idx = 0; idx < this.layers.Count; idx++)
			{
				MZExecutable layer = this.layers[idx];

				for (int jdx = 0; jdx < layer.Rebases.Count; jdx++)
				{
					MZRelocationItem rebase = layer.Rebases[jdx];
					Rebase(layer.content.AsSpan(rebase.Displacement, 2), section);
				}
			}
		}

		private static void Rebase(Span<byte> word, ushort section)
		{
			BinaryPrimitives.WriteUInt16LittleEndian(word, (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(word) + section));
		}

		private static int PullPreamble(Stream flow, int place, MZExecutable program)
		{
			// load header
			program.stamp = flow.PullUInt16();
			if (program.stamp != 0x5a4d)
			{
				throw new Exception("Not an MS-DOS executable file");
			}

			// 0x02 - Extra bytes, word (Number of bytes in the last page.)
			int additionalOctetsInt = flow.PullUInt16();
			// 0x04 - Pages, word (Number of whole/partial pages.)
			int sheets = flow.PullUInt16();
			// 0x06 - Relocation items, word (Number of entries in the relocation table.)
			int rebaseElements = flow.PullUInt16();
			// 0x08 - Header size, word (The number of paragraphs taken up by the header. It can be any value, as the loader just uses it to find where the actual executable data starts. It may be larger than what the "standard" fields take up, and you may use it if you want to include your own header metadata, or put the relocation table there, or use it for any other purpose.)
			int preambleExtentInt = flow.PullUInt16();
			program.lowestReservation = flow.PullUInt16();
			program.highestReservation = flow.PullUInt16();
			program.startingStackseg = flow.PullUInt16();
			program.startingStackptr = flow.PullUInt16();
			// 0x12 - Checksum, word (When added to the sum of all other words in the file, the result should be zero.)
			int sum = flow.PullUInt16();
			program.startingInstrptr = flow.PullUInt16();
			program.startingCodeseg = flow.PullUInt16();
			// 0x18 - Relocation table, word (The (absolute) offset to the relocation table.)
			int rebaseLookupDisplacement = flow.PullUInt16();
			program.layerOrdinal = flow.PullUInt16();
			program.layerIdx = flow.PullUInt16();

			// read relocations
			if (rebaseElements > 0)
			{
				flow.Seek(place + rebaseLookupDisplacement, SeekOrigin.Begin);
				for (int idx = 0; idx < rebaseElements; idx++)
				{
					ushort delta = flow.PullUInt16();
					ushort seg = flow.PullUInt16();
					program.rebases.Add(new MZRelocationItem(seg, delta));
				}

				flow.Seek(place + preambleExtentInt << 4, SeekOrigin.Begin);
			}

			// read data
			int contentExtent = (additionalOctetsInt > 0) ? (((sheets - 1) << 9) + additionalOctetsInt) - (preambleExtentInt << 4) : (sheets << 9) - (preambleExtentInt << 4);
			flow.Seek(place + (preambleExtentInt << 4), SeekOrigin.Begin);
			byte[] staging = new byte[contentExtent];
			flow.ReadExactly(staging, 0, contentExtent);
			program.content = staging;

			return (sheets << 9);
		}
	}
}
