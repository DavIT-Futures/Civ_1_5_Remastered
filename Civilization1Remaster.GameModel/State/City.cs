using System.Drawing;
using System.IO;

namespace Civilization1Remaster.GameModel.State
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

			town.StructureMarkers0 = flow.PullUInt16OrZero();
			town.StructureMarkers1 = flow.PullUInt16OrZero();
			town.Place = new Point(flow.PullUInt8OrZero(), town.Place.Y);
			town.Place = new Point(town.Place.X, flow.PullUInt8OrZero());
			town.ConditionMarker = flow.PullUInt8OrZero();
			town.TrueExtent = (sbyte)flow.PullUInt8OrZero();
			town.ShownExtent = (sbyte)flow.PullUInt8OrZero();
			town.PresentMakingIdx = (sbyte)flow.PullUInt8OrZero();
			town.RootCommerce = (sbyte)flow.PullUInt8OrZero();
			town.ParticipantIdx = (sbyte)flow.PullUInt8OrZero();
			town.GrainTally = flow.PullInt16OrZero();
			town.MaterialsTally = flow.PullInt16OrZero();
			town.LaborerMarkers0 = flow.PullUInt16OrZero();
			town.LaborerMarkers1 = flow.PullUInt16OrZero();
			town.LaborerMarkers2 = flow.PullUInt16OrZero();
			town.LabelIdx = flow.PullUInt8OrZero();

			for (int idx = 0; idx < town.CommerceTownIndices.Length; idx++)
				town.CommerceTownIndices[idx] = (sbyte)flow.PullUInt8OrZero();

			for (int idx = 0; idx < town.Unfamiliar.Length; idx++)
				town.Unfamiliar[idx] = (sbyte)flow.PullUInt8OrZero();

			return town;
		}

		public bool Has(CityStructure structure) => (this.Structures & (1u << (int)structure)) != 0;

		public void Add(CityStructure structure) => this.Structures |= 1u << (int)structure;

		public void Remove(CityStructure structure) => this.Structures &= ~(1u << (int)structure);

		public bool HasStructureSignExtended(ushort structureIdx)
		{
			ushort mask = (ushort)(1 << (byte)structureIdx);
			ushort high = (mask & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return ((this.StructureMarkers0 & mask) | (this.StructureMarkers1 & high)) != 0;
		}

		public void WriteTo(Stream flow)
		{
			flow.EmitUInt16(this.StructureMarkers0);
			flow.EmitUInt16(this.StructureMarkers1);
			flow.WriteByte((byte)this.Place.X);
			flow.WriteByte((byte)this.Place.Y);
			flow.WriteByte(this.ConditionMarker);
			flow.WriteByte((byte)this.TrueExtent);
			flow.WriteByte((byte)this.ShownExtent);
			flow.WriteByte((byte)this.PresentMakingIdx);
			flow.WriteByte((byte)this.RootCommerce);
			flow.WriteByte((byte)((short)this.ParticipantIdx));
			flow.EmitInt16(this.GrainTally);
			flow.EmitInt16(this.MaterialsTally);
			flow.EmitUInt16(this.LaborerMarkers0);
			flow.EmitUInt16(this.LaborerMarkers1);
			flow.EmitUInt16(this.LaborerMarkers2);
			flow.WriteByte(this.LabelIdx);

			for (int idx = 0; idx < this.CommerceTownIndices.Length; idx++)
				flow.WriteByte((byte)this.CommerceTownIndices[idx]);

			for (int idx = 0; idx < this.Unfamiliar.Length; idx++)
				flow.WriteByte((byte)this.Unfamiliar[idx]);
		}
	}
}
