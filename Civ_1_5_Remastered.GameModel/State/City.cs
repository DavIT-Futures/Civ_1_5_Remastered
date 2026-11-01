using System.Drawing;
using System.IO;

namespace Civ_1_5_Remastered.GameModel.State
{
	public class City
	{
		public City()
		{
			for (int index = 0; index < this.CommerceTownIndices.Length; index++)
			{
				this.CommerceTownIndices[index] = 0;
			}

			for (int index = 0; index < this.Unfamiliar.Length; index++)
			{
				this.Unfamiliar[index] = 0;
			}
		}

		public ushort StructureMarkersLow { get; set; } = 0;
		public ushort StructureMarkersHigh { get; set; } = 0;
		public Point Place { get; set; } = new Point(0, 0);
		public byte ConditionMarker { get; set; } = 0;
		public sbyte TrueExtent { get; set; } = 0;
		public sbyte ShownExtent { get; set; } = 0;
		public sbyte PresentMakingIndex { get; set; } = 0;
		public sbyte RootCommerce { get; set; } = 0;
		public short ParticipantIndex { get; set; } = 0;
		public short GrainTally { get; set; } = 0;
		public short MaterialsTally { get; set; } = 0;
		public ushort LaborerMarkersLow { get; set; } = 0;
		public ushort LaborerMarkersHigh { get; set; } = 0;
		public ushort SpecialistMarkers { get; set; } = 0;
		public byte LabelIndex { get; set; } = 0;
		public sbyte[] CommerceTownIndices { get; set; } = new sbyte[3];
		public sbyte[] Unfamiliar { get; set; } = new sbyte[2];

		public uint Structures
		{
			get
			{
				return ((uint)this.StructureMarkersHigh << 16) | this.StructureMarkersLow;
			}

			set
			{
				this.StructureMarkersLow = (ushort)value;
				this.StructureMarkersHigh = (ushort)(value >> 16);
			}
		}

		public static City ReadFrom(Stream flow)
		{
			City town = new City();

			town.StructureMarkersLow = flow.PullWordOrZero();
			town.StructureMarkersHigh = flow.PullWordOrZero();
			town.Place = new Point(flow.PullByteOrZero(), town.Place.Y);
			town.Place = new Point(town.Place.X, flow.PullByteOrZero());
			town.ConditionMarker = flow.PullByteOrZero();
			town.TrueExtent = (sbyte)flow.PullByteOrZero();
			town.ShownExtent = (sbyte)flow.PullByteOrZero();
			town.PresentMakingIndex = (sbyte)flow.PullByteOrZero();
			town.RootCommerce = (sbyte)flow.PullByteOrZero();
			town.ParticipantIndex = (sbyte)flow.PullByteOrZero();
			town.GrainTally = flow.PullSignedWordOrZero();
			town.MaterialsTally = flow.PullSignedWordOrZero();
			town.LaborerMarkersLow = flow.PullWordOrZero();
			town.LaborerMarkersHigh = flow.PullWordOrZero();
			town.SpecialistMarkers = flow.PullWordOrZero();
			town.LabelIndex = flow.PullByteOrZero();

			for (int index = 0; index < town.CommerceTownIndices.Length; index++)
			{
				town.CommerceTownIndices[index] = (sbyte)flow.PullByteOrZero();
			}

			for (int index = 0; index < town.Unfamiliar.Length; index++)
			{
				town.Unfamiliar[index] = (sbyte)flow.PullByteOrZero();
			}

			return town;
		}

		public bool Has(CityStructure structure) => (this.Structures & (1u << (int)structure)) != 0;

		public void Add(CityStructure structure) => this.Structures |= 1u << (int)structure;

		public void Remove(CityStructure structure) => this.Structures &= ~(1u << (int)structure);

		public bool HasStructureSignExtended(ushort structureIndex)
		{
			ushort mask = (ushort)(1 << (byte)structureIndex);
			ushort high = (mask & 0x8000) != 0 ? (ushort)0xffff : (ushort)0;

			return ((this.StructureMarkersLow & mask) | (this.StructureMarkersHigh & high)) != 0;
		}

		public void WriteTo(Stream flow)
		{
			flow.EmitWord(this.StructureMarkersLow);
			flow.EmitWord(this.StructureMarkersHigh);
			flow.WriteByte((byte)this.Place.X);
			flow.WriteByte((byte)this.Place.Y);
			flow.WriteByte(this.ConditionMarker);
			flow.WriteByte((byte)this.TrueExtent);
			flow.WriteByte((byte)this.ShownExtent);
			flow.WriteByte((byte)this.PresentMakingIndex);
			flow.WriteByte((byte)this.RootCommerce);
			flow.WriteByte((byte)((short)this.ParticipantIndex));
			flow.EmitSignedWord(this.GrainTally);
			flow.EmitSignedWord(this.MaterialsTally);
			flow.EmitWord(this.LaborerMarkersLow);
			flow.EmitWord(this.LaborerMarkersHigh);
			flow.EmitWord(this.SpecialistMarkers);
			flow.WriteByte(this.LabelIndex);

			for (int index = 0; index < this.CommerceTownIndices.Length; index++)
			{
				flow.WriteByte((byte)this.CommerceTownIndices[index]);
			}

			for (int index = 0; index < this.Unfamiliar.Length; index++)
			{
				flow.WriteByte((byte)this.Unfamiliar[index]);
			}
		}
	}
}
