namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The value a rewritten function returns, as the original left it in AX. Phase 3 removed the AX writes of the rewritten
	/// functions; the reference copies of the original code read AX after their calls, and the differential tests compare AX
	/// after the rewritten call, so both put the returned value there (a true bool is 1).
	/// </summary>
	public static class ReturnInAx
	{
		public static ushort Of(ushort value) => value;

		public static ushort Of(short value) => (ushort)value;

		public static ushort Of(int value) => (ushort)value;

		public static ushort Of(uint value) => (ushort)value;

		public static ushort Of(byte value) => value;

		public static ushort Of(sbyte value) => (ushort)value;

		public static ushort Of(bool value) => (ushort)(value ? 1 : 0);
	}
}
