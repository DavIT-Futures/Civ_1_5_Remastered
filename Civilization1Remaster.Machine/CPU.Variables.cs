namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// Access to the variables of the translated DOS program. The program keeps its local variables on the stack, next to the
	/// frame pointer, and its global variables at fixed addresses of the data segment. These methods say what the original
	/// instructions do: [frame pointer - distance] in the stack segment, and [address] in the data segment.
	/// </summary>
	public partial class CPU
	{
		public byte PullLocalByte(int distance)
		{
			return PullUInt8(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance));
		}

		public sbyte PullLocalSignedByte(int distance)
		{
			return PullInt8(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance));
		}

		public ushort PullLocalWord(int distance)
		{
			return PullUInt16(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance));
		}

		public short PullLocalSignedWord(int distance)
		{
			return PullInt16(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance));
		}

		public void EmitLocalByte(int distance, byte amount)
		{
			EmitUInt8(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance), amount);
		}

		public void EmitLocalWord(int distance, ushort amount)
		{
			EmitUInt16(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance), amount);
		}

		public void EmitLocalSignedWord(int distance, short amount)
		{
			EmitInt16(this.segStack.Value16, (ushort)(this.regFrame.Value16 - distance), amount);
		}

		public byte PullGlobalByte(ushort address)
		{
			return PullUInt8(this.segData.Value16, address);
		}

		public sbyte PullGlobalSignedByte(ushort address)
		{
			return PullInt8(this.segData.Value16, address);
		}

		public ushort PullGlobalWord(ushort address)
		{
			return PullUInt16(this.segData.Value16, address);
		}

		public short PullGlobalSignedWord(ushort address)
		{
			return PullInt16(this.segData.Value16, address);
		}

		public void EmitGlobalByte(ushort address, byte amount)
		{
			EmitUInt8(this.segData.Value16, address, amount);
		}

		public void EmitGlobalWord(ushort address, ushort amount)
		{
			EmitUInt16(this.segData.Value16, address, amount);
		}

		public void EmitGlobalSignedWord(ushort address, short amount)
		{
			EmitInt16(this.segData.Value16, address, amount);
		}
	}
}
