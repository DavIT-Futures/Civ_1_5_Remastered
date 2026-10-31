namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// Access to the global variables of the translated DOS program, at fixed addresses of the data segment. These methods
	/// say what the original instructions do: [address] in the data segment.
	/// </summary>
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
