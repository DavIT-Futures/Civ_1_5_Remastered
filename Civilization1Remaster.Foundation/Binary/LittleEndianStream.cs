using System;
using System.Buffers.Binary;
using System.IO;

namespace Civilization1Remaster.Foundation.Binary
{
	/// <summary>
	/// Reads and writes the little-endian numbers of the DOS file formats (executables, pictures, saved games).
	/// Each way of meeting the end of the stream has its own name:
	/// Pull* throws, Pull*OrZero reads zero (saved games), TryPull* reports it.
	/// A partly available number still consumes the bytes that were there.
	/// </summary>
	public static class LittleEndianStream
	{
		public const string UnexpectedEnd = "Unexpected end of stream";

		/// <summary>
		/// Reads one byte, throws at the end of the stream.
		/// </summary>
		public static byte PullUInt8(this Stream source)
		{
			int octet = source.ReadByte();

			if (octet < 0)
				throw new Exception(UnexpectedEnd);

			return (byte)octet;
		}

		/// <summary>
		/// Reads a 16 bit number, throws at the end of the stream.
		/// </summary>
		public static ushort PullUInt16(this Stream source)
		{
			Span<byte> staging = stackalloc byte[2];

			if (!Fill(source, staging))
				throw new Exception(UnexpectedEnd);

			return BinaryPrimitives.ReadUInt16LittleEndian(staging);
		}

		/// <summary>
		/// Reads a 32 bit number, throws at the end of the stream.
		/// </summary>
		public static uint PullUInt32(this Stream source)
		{
			Span<byte> staging = stackalloc byte[4];

			if (!Fill(source, staging))
				throw new Exception(UnexpectedEnd);

			return BinaryPrimitives.ReadUInt32LittleEndian(staging);
		}

		/// <summary>
		/// Reads one byte, or zero at the end of the stream.
		/// </summary>
		public static byte PullUInt8OrZero(this Stream source)
		{
			int octet = source.ReadByte();

			return octet < 0 ? (byte)0 : (byte)octet;
		}

		/// <summary>
		/// Reads a 16 bit number, or zero when the stream ends before both bytes.
		/// </summary>
		public static ushort PullUInt16OrZero(this Stream source)
		{
			return TryPullUInt16(source, out ushort amount) ? amount : (ushort)0;
		}

		/// <summary>
		/// Reads a signed 16 bit number, or zero when the stream ends before both bytes.
		/// </summary>
		public static short PullInt16OrZero(this Stream source)
		{
			return (short)PullUInt16OrZero(source);
		}

		/// <summary>
		/// Reads a 16 bit number, returns false when the stream ends before both bytes.
		/// </summary>
		public static bool TryPullUInt16(this Stream source, out ushort amount)
		{
			Span<byte> staging = stackalloc byte[2];

			if (!Fill(source, staging))
			{
				amount = 0;
				return false;
			}

			amount = BinaryPrimitives.ReadUInt16LittleEndian(staging);
			return true;
		}

		public static void EmitUInt16(this Stream sink, ushort amount)
		{
			Span<byte> staging = stackalloc byte[2];
			BinaryPrimitives.WriteUInt16LittleEndian(staging, amount);
			sink.Write(staging);
		}

		public static void EmitInt16(this Stream sink, short amount)
		{
			EmitUInt16(sink, (ushort)amount);
		}

		private static bool Fill(Stream source, Span<byte> staging)
		{
			return source.ReadAtLeast(staging, staging.Length, throwOnEndOfStream: false) == staging.Length;
		}
	}
}
