namespace Civilization1Remaster.Machine
{
	public partial class CPU
	{
		public byte PullGlobalByte(ushort address)
		{
			return PullUInt8(this.segData.Value16, address);
		}

		public ushort PullGlobalWord(ushort address)
		{
			return PullUInt16(this.segData.Value16, address);
		}

		public void EmitGlobalByte(ushort address, byte amount)
		{
			EmitUInt8(this.segData.Value16, address, amount);
		}

		public void EmitGlobalWord(ushort address, ushort amount)
		{
			EmitUInt16(this.segData.Value16, address, amount);
		}
	}
}
