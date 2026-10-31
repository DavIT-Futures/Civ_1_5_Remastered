using System;
using System.Drawing;
using System.IO;

namespace Civilization1Remaster.Game.State
{
	public class City
	{
		public City()
		{
			for (int idx = 0; idx < this.CommerceTownIndices.Length; idx++)
				this.CommerceTownIndices[idx] = 0;

			for (int idx = 0; idx < this.Unfamiliar.Length; idx++)
				this.Unfamiliar[idx] = 0;
		}

		public ushort StructureMarkers0 { get; set; } = 0;
		public ushort StructureMarkers1 { get; set; } = 0;
		public Point Place { get; set; } = new Point(0, 0);
		public byte ConditionMarker { get; set; } = 0;
		public sbyte TrueExtent { get; set; } = 0;
		public sbyte ShownExtent { get; set; } = 0;
		public sbyte PresentMakingIdx { get; set; } = 0;
		public sbyte RootCommerce { get; set; } = 0;
		public short ParticipantIdx { get; set; } = 0;
		public short GrainTally { get; set; } = 0;
		public short MaterialsTally { get; set; } = 0;
		public ushort LaborerMarkers0 { get; set; } = 0;
		public ushort LaborerMarkers1 { get; set; } = 0;
		public ushort LaborerMarkers2 { get; set; } = 0;
		public byte LabelIdx { get; set; } = 0;
		public sbyte[] CommerceTownIndices { get; set; } = new sbyte[3];
		public sbyte[] Unfamiliar { get; set; } = new sbyte[2];

		/// <summary>Both structure words as one 32-bit mask (bit n: CityStructure n)</summary>
		public uint Structures
		{
			get => ((uint)this.StructureMarkers1 << 16) | this.StructureMarkers0;
			set
			{
				this.StructureMarkers0 = (ushort)value;
				this.StructureMarkers1 = (ushort)(value >> 16);
			}
		}

		public static City ReadFrom(Stream flow)
		{
			City town = new City();

			town.StructureMarkers0 = StreamIo.PullUInt16(flow);
			town.StructureMarkers1 = StreamIo.PullUInt16(flow);
			town.Place = new Point(StreamIo.PullUInt8(flow), town.Place.Y);
			town.Place = new Point(town.Place.X, StreamIo.PullUInt8(flow));
			town.ConditionMarker = StreamIo.PullUInt8(flow);
			town.TrueExtent = (sbyte)StreamIo.PullUInt8(flow);
			town.ShownExtent = (sbyte)StreamIo.PullUInt8(flow);
			town.PresentMakingIdx = (sbyte)StreamIo.PullUInt8(flow);
			town.RootCommerce = (sbyte)StreamIo.PullUInt8(flow);
			town.ParticipantIdx = (sbyte)StreamIo.PullUInt8(flow);
			town.GrainTally = StreamIo.PullInt16(flow);
			town.MaterialsTally = StreamIo.PullInt16(flow);
			town.LaborerMarkers0 = StreamIo.PullUInt16(flow);
			town.LaborerMarkers1 = StreamIo.PullUInt16(flow);
			town.LaborerMarkers2 = StreamIo.PullUInt16(flow);
			town.LabelIdx = StreamIo.PullUInt8(flow);

			for (int idx = 0; idx < town.CommerceTownIndices.Length; idx++)
				town.CommerceTownIndices[idx] = (sbyte)StreamIo.PullUInt8(flow);

			for (int idx = 0; idx < town.Unfamiliar.Length; idx++)
				town.Unfamiliar[idx] = (sbyte)StreamIo.PullUInt8(flow);

			return town;
		}

		/// <summary>Whether the city has the improvement</summary>
		public bool Has(CityStructure structure) => (this.Structures & (1u << (int)structure)) != 0;

		/// <summary>The improvement built</summary>
		public void Add(CityStructure structure) => this.Structures |= 1u << (int)structure;

		/// <summary>The improvement gone</summary>
		public void Remove(CityStructure structure) => this.Structures &= ~(1u << (int)structure);

		/// <summary>
		/// The bit of a structure read the way the original reads it: a 16-bit mask sign extended to 32 bits, so the bit 15
		/// also counts any of 16 - 31 and the bits 16 - 23 never count
		/// </summary>
		public bool HasStructureSignExtended(ushort structureIdx)
		{
			ushort mask = (ushort)(1 << (byte)structureIdx);
			ushort high = (mask & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return ((this.StructureMarkers0 & mask) | (this.StructureMarkers1 & high)) != 0;
		}

		public void WriteTo(Stream flow)
		{
			StreamIo.EmitUInt16(flow, this.StructureMarkers0);
			StreamIo.EmitUInt16(flow, this.StructureMarkers1);
			flow.WriteByte((byte)this.Place.X);
			flow.WriteByte((byte)this.Place.Y);
			flow.WriteByte(this.ConditionMarker);
			flow.WriteByte((byte)this.TrueExtent);
			flow.WriteByte((byte)this.ShownExtent);
			flow.WriteByte((byte)this.PresentMakingIdx);
			flow.WriteByte((byte)this.RootCommerce);
			flow.WriteByte((byte)((short)this.ParticipantIdx));
			StreamIo.EmitInt16(flow, this.GrainTally);
			StreamIo.EmitInt16(flow, this.MaterialsTally);
			StreamIo.EmitUInt16(flow, this.LaborerMarkers0);
			StreamIo.EmitUInt16(flow, this.LaborerMarkers1);
			StreamIo.EmitUInt16(flow, this.LaborerMarkers2);
			flow.WriteByte(this.LabelIdx);

			for (int idx = 0; idx < this.CommerceTownIndices.Length; idx++)
				flow.WriteByte((byte)this.CommerceTownIndices[idx]);

			for (int idx = 0; idx < this.Unfamiliar.Length; idx++)
				flow.WriteByte((byte)this.Unfamiliar[idx]);
		}
	}
}
