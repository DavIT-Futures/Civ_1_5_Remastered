namespace Civilization1Remaster.Emulation
{
	/// <summary>
	/// Short sequences of instructions that the translated program uses over and over, with names that say what they do.
	/// </summary>
	public partial class CPU
	{
		#region Counters in local variables

		/// <summary>Adds one to a local variable (loop counters; inc word [bp - distance]).</summary>
		public void IncrementLocalWord(int distance)
		{
			EmitLocalWord(distance, IncrementWord(PullLocalWord(distance)));
		}

		/// <summary>Subtracts one from a local variable (dec word [bp - distance]).</summary>
		public void DecrementLocalWord(int distance)
		{
			EmitLocalWord(distance, DecrementWord(PullLocalWord(distance)));
		}

		/// <summary>Copies a local variable into another one through the accumulator (mov ax, [bp - source]; mov [bp - destination], ax).</summary>
		public void CopyLocalWord(int source, int destination)
		{
			this.regAcc.Value16 = PullLocalWord(source);
			EmitLocalWord(destination, this.regAcc.Value16);
		}

		#endregion

		#region Registers that a function has to give back unchanged

		/// <summary>Saves the destination and source index registers on the stack (push di; push si).</summary>
		public void PreserveIndexRegisters()
		{
			StackWord(this.regDest.Value16);
			StackWord(this.regSource.Value16);
		}

		/// <summary>Takes the registers saved by PreserveIndexRegisters back from the stack, in the reverse order (pop si; pop di).</summary>
		public void RestoreIndexRegisters()
		{
			this.regSource.Value16 = UnstackWord();
			this.regDest.Value16 = UnstackWord();
		}

		/// <summary>Saves the source index register on the stack (push si).</summary>
		public void PreserveSource()
		{
			StackWord(this.regSource.Value16);
		}

		/// <summary>Takes the source index register back from the stack (pop si).</summary>
		public void RestoreSource()
		{
			this.regSource.Value16 = UnstackWord();
		}

		#endregion

		#region Arithmetic on the accumulator

		/// <summary>Sets the flags from the accumulator without changing it (or ax, ax).</summary>
		public void TestAccumulator()
		{
			this.regAcc.Value16 = BitOrWord(this.regAcc.Value16, this.regAcc.Value16);
		}

		/// <summary>
		/// The accumulator becomes its absolute value. The data register ends up holding the sign (all ones for a negative
		/// number, zero for a positive one), which the program often uses afterwards to restore the sign (cwd; xor ax, dx; sub ax, dx).
		/// </summary>
		public void AbsoluteAccumulator()
		{
			SignExtendWord(this.regAcc, this.regData);
			this.regAcc.Value16 = BitXorWord(this.regAcc.Value16, this.regData.Value16);
			this.regAcc.Value16 = SubtractWord(this.regAcc.Value16, this.regData.Value16);
		}

		/// <summary>
		/// Divides the signed accumulator by two, rounding towards zero: one is added to a negative number before the shift
		/// (cwd; sub ax, dx; sar ax, 1).
		/// </summary>
		public void HalveAccumulator()
		{
			SignExtendWord(this.regAcc, this.regData);
			this.regAcc.Value16 = SubtractWord(this.regAcc.Value16, this.regData.Value16);
			this.regAcc.Value16 = ShiftRightSignedWord(this.regAcc.Value16, 0x1);
		}

		/// <summary>
		/// Divides the signed accumulator by two to the power "shift", rounding towards zero: takes the absolute value, shifts it
		/// right and puts the sign back. The count register and the data register are left as the instructions leave them.
		/// </summary>
		public void DivideAccumulatorByPowerOfTwo(ushort shift)
		{
			AbsoluteAccumulator();
			this.regCount.Value16 = shift;
			this.regAcc.Value16 = ShiftRightSignedWord(this.regAcc.Value16, this.regCount.LowByte);
			this.regAcc.Value16 = BitXorWord(this.regAcc.Value16, this.regData.Value16);
			this.regAcc.Value16 = SubtractWord(this.regAcc.Value16, this.regData.Value16);
		}

		#endregion
	}
}
