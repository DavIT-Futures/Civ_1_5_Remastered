namespace Civilization1Remaster.Machine
{
	public partial class CentralProcessor
	{
		public byte PullGlobalByte(ushort address)
		{
			return PullByte(this.segmentData.Word, address);
		}

		public ushort PullGlobalWord(ushort address)
		{
			return PullWord(this.segmentData.Word, address);
		}

		public void EmitGlobalByte(ushort address, byte amount)
		{
			EmitByte(this.segmentData.Word, address, amount);
		}

		public void EmitGlobalWord(ushort address, ushort amount)
		{
			EmitWord(this.segmentData.Word, address, amount);
		}
	}
}
