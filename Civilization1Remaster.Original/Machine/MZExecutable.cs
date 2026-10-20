using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Civilization1Original.Emulation
{
	public class MZExecutable
	{
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
		// relocation data
		private readonly List<MZRelocationItem> rebases = new List<MZRelocationItem>();
		// overlays
		private readonly List<MZExecutable> layers = new List<MZExecutable>();

		protected MZExecutable()
		{ }

		public MZExecutable(string route)
			: this(new FileStream(route, FileMode.Open, FileAccess.Read, FileShare.Read))
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

		private static int PullPreamble(Stream flow, int place, MZExecutable program)
		{
			// load header
			program.stamp = PullUInt16(flow);
			if (program.stamp != 0x5a4d)
			{
				throw new Exception("Not an MS-DOS executable file");
			}

			// 0x02 - Extra bytes, word (Number of bytes in the last page.)
			int additionalOctetsInt = PullUInt16(flow);
			// 0x04 - Pages, word (Number of whole/partial pages.)
			int sheets = PullUInt16(flow);
			// 0x06 - Relocation items, word (Number of entries in the relocation table.)
			int rebaseElements = PullUInt16(flow);
			// 0x08 - Header size, word (The number of paragraphs taken up by the header. It can be any value, as the loader just uses it to find where the actual executable data starts. It may be larger than what the "standard" fields take up, and you may use it if you want to include your own header metadata, or put the relocation table there, or use it for any other purpose.)
			int preambleExtentInt = PullUInt16(flow);
			program.lowestReservation = PullUInt16(flow);
			program.highestReservation = PullUInt16(flow);
			program.startingStackseg = PullUInt16(flow);
			program.startingStackptr = PullUInt16(flow);
			// 0x12 - Checksum, word (When added to the sum of all other words in the file, the result should be zero.)
			int sum = PullUInt16(flow);
			program.startingInstrptr = PullUInt16(flow);
			program.startingCodeseg = PullUInt16(flow);
			// 0x18 - Relocation table, word (The (absolute) offset to the relocation table.)
			int rebaseLookupDisplacement = PullUInt16(flow);
			program.layerOrdinal = PullUInt16(flow);
			program.layerIdx = PullUInt16(flow);

			// read relocations
			if (rebaseElements > 0)
			{
				flow.Seek(place + rebaseLookupDisplacement, SeekOrigin.Begin);
				for (int idx = 0; idx < rebaseElements; idx++)
				{
					ushort delta = PullUInt16(flow);
					ushort seg = PullUInt16(flow);
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

		public void EmitToFile(string route)
		{
			FileStream sink = new FileStream(route, FileMode.Create);
			this.EmitToFile(sink);
			sink.Close();
		}

		public void EmitToFile(Stream flow)
		{
			MemoryStream sink = new MemoryStream();
			EmitUInt16(sink, this.stamp);
			uint spanUint = (uint)this.content.Length;
			uint headingMagnitude = (uint)(0x1e + this.rebases.Count * 4);
			CPU.SnapToSegment(ref headingMagnitude);
			spanUint += headingMagnitude;
			uint sheetsUint = spanUint / 512;
			uint additionalOctets = spanUint - (sheetsUint * 512);
			if (additionalOctets > 0)
				sheetsUint++;
			EmitUInt16(sink, additionalOctets);
			EmitUInt16(sink, sheetsUint);
			EmitUInt16(sink, (uint)this.rebases.Count);
			headingMagnitude >>= 4;
			EmitUInt16(sink, headingMagnitude);
			EmitUInt16(sink, this.lowestReservation);
			EmitUInt16(sink, this.highestReservation);
			EmitUInt16(sink, this.startingStackseg);
			EmitUInt16(sink, this.startingStackptr);
			uint digest = 0;
			EmitUInt16(sink, digest);
			EmitUInt16(sink, this.startingInstrptr);
			EmitUInt16(sink, this.startingCodeseg);
			EmitUInt16(sink, 0x1e);
			EmitUInt16(sink, this.layerOrdinal);
			EmitUInt16(sink, this.layerIdx);

			// write relocations
			for (int idx = 0; idx < this.rebases.Count; idx++)
			{
				MZRelocationItem rebase = this.rebases[idx];
				EmitUInt16(sink, rebase.Displacement);
				EmitUInt16(sink, rebase.Section);
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
				digest += PullUInt16(sink);
			}
			digest &= 0xffff;
			digest = 0x10000 - digest;
			sink.Seek(0x12, SeekOrigin.Begin);
			EmitUInt16(sink, digest);

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
				ushort term1 = (ushort)((ushort)this.content[spot] | (ushort)((ushort)this.content[spot + 1] << 8));
				term1 += section;
				this.content[spot] = (byte)(term1 & 0xff);
				this.content[spot + 1] = (byte)((term1 & 0xff00) >> 8);
			}

			for (int idx = 0; idx < this.layers.Count; idx++)
			{
				MZExecutable layer = this.layers[idx];

				for (int jdx = 0; jdx < layer.Rebases.Count; jdx++)
				{
					MZRelocationItem rebase = layer.Rebases[jdx];
					uint spot = rebase.Displacement;
					ushort term1 = (ushort)((ushort)layer.content[spot] | (ushort)((ushort)layer.content[spot + 1] << 8));
					term1 += section;
					layer.content[spot] = (byte)(term1 & 0xff);
					layer.content[spot + 1] = (byte)((term1 & 0xff00) >> 8);
				}
			}
		}

		#region Helper functions
		public static byte PullByte(Stream flow)
		{
			int rhs0 = flow.ReadByte();

			if (rhs0 < 0)
			{
				throw new Exception("Unexpected end of stream");
			}

			return (byte)(rhs0 & 0xff);
		}

		public static ushort PullUInt16(Stream flow)
		{
			int rhs0 = flow.ReadByte();
			int rhs1 = flow.ReadByte();

			if (rhs0 < 0 || rhs1 < 0)
			{
				throw new Exception("Unexpected end of stream");
			}

			return (ushort)((rhs0 & 0xff) | ((rhs1 & 0xff) << 8));
		}

		public static uint PullUInt32(Stream flow)
		{
			int rhs0 = flow.ReadByte();
			int rhs1 = flow.ReadByte();
			int rhs2 = flow.ReadByte();
			int rhs3 = flow.ReadByte();

			if (rhs0 < 0 || rhs1 < 0 || rhs2 < 0 || rhs3 < 0)
			{
				throw new Exception("Unexpected end of stream");
			}

			return (uint)((uint)((uint)rhs0 & 0xff) | (uint)(((uint)rhs1 & 0xff) << 8) |
				(uint)(((uint)rhs2 & 0xff) << 16) | (uint)(((uint)rhs3 & 0xff) << 24));
		}

		public static byte[] PullChunk(Stream flow, int extent)
		{
			byte[] scratchBytes = new byte[extent];

			if (flow.Read(scratchBytes, 0, extent) != extent)
			{
				throw new Exception("Unexpected end of stream");
			}

			return scratchBytes;
		}

		public static string? PullText(Stream flow)
		{
			int spanInt = PullByte(flow);
			byte[] scratchBytes = new byte[spanInt];

			if (spanInt == 0)
				return null;

			if (flow.Read(scratchBytes, 0, spanInt) != spanInt)
			{
				throw new Exception("Unexpected end of stream");
			}

			return Encoding.ASCII.GetString(scratchBytes);
		}

		public void EmitUInt16(Stream flow, uint amount)
		{
			flow.WriteByte((byte)(amount & 0xff));
			flow.WriteByte((byte)((amount & 0xff00) >> 8));
		}
		#endregion

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
	}
}
