namespace Civilization1Original.Game.State
{
	/// <summary>
	/// Reads and writes the little-endian numbers of the saved game format.
	/// </summary>
	public static class StreamIo
	{
		/// <summary>
		/// ?
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static byte PullUInt8(Stream source)
		{
			int octet0 = source.ReadByte();

			if (octet0 >= 0)
			{
				return (byte)octet0;
			}

			return 0;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static ushort PullUInt16(Stream source)
		{
			int octet0 = source.ReadByte();
			int octet1 = source.ReadByte();

			if (octet0 >= 0 && octet1 >= 0)
			{
				return (ushort)(octet0 | (octet1 << 8));
			}

			return 0;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		public static short PullInt16(Stream source)
		{
			int octet0 = source.ReadByte();
			int octet1 = source.ReadByte();

			if (octet0 >= 0 && octet1 >= 0)
			{
				return (short)(octet0 | (octet1 << 8));
			}

			return 0;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void EmitUInt16(Stream sink, ushort amount)
		{
			sink.WriteByte((byte)(amount & 0xff));
			sink.WriteByte((byte)((amount & 0xff00) >> 8));
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public static void EmitInt16(Stream sink, short amount)
		{
			sink.WriteByte((byte)((ushort)amount & 0xff));
			sink.WriteByte((byte)(((ushort)amount & 0xff00) >> 8));
		}
	}
}
