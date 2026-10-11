namespace Civilization1Remaster.Emulation
{
	/// <summary>
	/// Every function of the translated DOS program opens a stack frame when it starts and closes it before it returns.
	/// </summary>
	public partial class CPU
	{
		/// <summary>
		/// The start of a function: remembers the caller's frame pointer on the stack and makes the current top of the stack
		/// the new frame pointer (push bp; mov bp, sp).
		/// </summary>
		public void EnterFrame()
		{
			StackWord(this.regFrame.Value16);
			this.regFrame.Value16 = this.regStack.Value16;
		}

		/// <summary>
		/// The start of a function that has local variables: opens the frame and reserves room for the variables below it
		/// (push bp; mov bp, sp; sub sp, size). The variables are then reached with PullLocalWord, EmitLocalWord and so on.
		/// </summary>
		/// <param name="localsSize">The size of the local variables in bytes</param>
		public void EnterFrame(ushort localsSize)
		{
			EnterFrame();
			this.regStack.Value16 = SubtractWord(this.regStack.Value16, localsSize);
		}

		/// <summary>
		/// The end of a function: throws the local variables away and restores the caller's frame pointer (mov sp, bp; pop bp).
		/// </summary>
		public void LeaveFrame()
		{
			this.regStack.Value16 = this.regFrame.Value16;
			this.regFrame.Value16 = UnstackWord();
		}
	}
}
