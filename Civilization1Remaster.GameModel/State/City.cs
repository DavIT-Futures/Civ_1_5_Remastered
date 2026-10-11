using System;
using System.Drawing;
using System.IO;

namespace Civilization1Remaster.Game.State
{
	public class City
	{
		public ushort StructureMarkers0 = 0;
		public ushort StructureMarkers1 = 0;
		public Point Place = new Point(0, 0);
		public byte ConditionMarker = 0;
		public sbyte TrueExtent = 0;
		public sbyte ShownExtent = 0;
		public sbyte PresentMakingIdx = 0;
		public sbyte RootCommerce = 0;
		public short ParticipantIdx = 0;
		public short GrainTally = 0;
		public short MaterialsTally = 0;
		public ushort LaborerMarkers0 = 0;
		public ushort LaborerMarkers1 = 0;
		public ushort LaborerMarkers2 = 0;
		public byte LabelIdx = 0;
		public sbyte[] CommerceTownIndices = new sbyte[3];
		public sbyte[] Unfamiliar = new sbyte[2];

		public City()
		{
			for (int idx = 0; idx < this.CommerceTownIndices.Length; idx++)
				this.CommerceTownIndices[idx] = 0;

			for (int idx = 0; idx < this.Unfamiliar.Length; idx++)
				this.Unfamiliar[idx] = 0;
		}

		public static City ReadFrom(Stream flow)
		{
			City town = new City();

			town.StructureMarkers0 = StreamIo.PullUInt16(flow);
			town.StructureMarkers1 = StreamIo.PullUInt16(flow);
			town.Place.X = StreamIo.PullUInt8(flow);
			town.Place.Y = StreamIo.PullUInt8(flow);
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
