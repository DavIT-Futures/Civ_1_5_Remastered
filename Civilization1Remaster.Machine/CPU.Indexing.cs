namespace Civilization1Remaster.Machine
{
	/// <summary>
	/// The translated program reaches the element number "index" of a table, whose elements are "elementSize" bytes long,
	/// by multiplying the two numbers (the original multiplied with the IMUL instruction, which leaves the product in the
	/// accumulator and its upper half in the data register) and then using the result as an address. These methods are
	/// that multiplication and the moves of its result that follow it.
	/// </summary>
	public partial class CPU
	{
		/// <summary>The accumulator gets elementSize * index (mov ax, elementSize; imul index).</summary>
		public void ScaleIndex(ushort elementSize, ushort index)
		{
			this.regAcc.Value16 = elementSize;
			MultiplySignedWord(this.regAcc, this.regData, index);
		}

		/// <summary>The source register gets the offset elementSize * index.</summary>
		public void ScaleIndexToSource(ushort elementSize, ushort index)
		{
			ScaleIndex(elementSize, index);
			this.regSource.Value16 = this.regAcc.Value16;
		}

		/// <summary>The base register gets the offset elementSize * index.</summary>
		public void ScaleIndexToBase(ushort elementSize, ushort index)
		{
			ScaleIndex(elementSize, index);
			this.regBase.Value16 = this.regAcc.Value16;
		}

		/// <summary>Adds elementSize * index to the offset in the source register, for the element of a table inside another table.</summary>
		public void AddScaledToSource(ushort elementSize, ushort index)
		{
			ScaleIndex(elementSize, index);
			this.regSource.Value16 = SumWord(this.regSource.Value16, this.regAcc.Value16);
		}

		/// <summary>The accumulator, which already holds the element size, is multiplied by the index and the product goes to the source register.</summary>
		public void ScaleAccumulatorToSource(ushort index)
		{
			MultiplySignedWord(this.regAcc, this.regData, index);
			this.regSource.Value16 = this.regAcc.Value16;
		}

		/// <summary>The accumulator, which already holds the element size, is multiplied by the index and the product goes to the base register.</summary>
		public void ScaleAccumulatorToBase(ushort index)
		{
			MultiplySignedWord(this.regAcc, this.regData, index);
			this.regBase.Value16 = this.regAcc.Value16;
		}
	}
}
