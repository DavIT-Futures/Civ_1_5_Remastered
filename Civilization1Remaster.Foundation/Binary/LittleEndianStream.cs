using System;
using System.Buffers.Binary;
using System.IO;

namespace Civilization1Remaster.Foundation.Binary
{
	public static class LittleEndianStream
	{
		public static byte PullByteOrZero(this Stream source)
		{
			int octet = source.ReadByte();

			return octet < 0 ? (byte)0 : (byte)octet;
		}

		public static ushort PullWordOrZero(this Stream source)
		{
			return TryPullWord(source, out ushort amount) ? amount : (ushort)0;
		}

		public static short PullSignedWordOrZero(this Stream source)
		{
			return (short)PullWordOrZero(source);
		}

		public static bool TryPullWord(this Stream source, out ushort amount)
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

		public static void EmitWord(this Stream sink, ushort amount)
		{
			Span<byte> staging = stackalloc byte[2];
			BinaryPrimitives.WriteUInt16LittleEndian(staging, amount);
			sink.Write(staging);
		}

		public static void EmitSignedWord(this Stream sink, short amount)
		{
			EmitWord(sink, (ushort)amount);
		}

		private static bool Fill(Stream source, Span<byte> staging)
		{
			return source.ReadAtLeast(staging, staging.Length, throwOnEndOfStream: false) == staging.Length;
		}
	}
}
