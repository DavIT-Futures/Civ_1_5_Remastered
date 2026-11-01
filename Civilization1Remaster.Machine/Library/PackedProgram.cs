using System;
using System.IO;
using System.Text;

namespace Civilization1Remaster.Machine.Library
{
	public static class PackedProgram
	{
		private const int PackedSignature = 0x4252;
		private const int SegmentGroups = 16;
		private const byte FillCommand = 0xb0;
		private const byte CopyCommand = 0xb2;
		private const byte LastCommand = 0x01;
		private const byte Padding = 0xff;

		private static readonly byte[] CorruptMessage = Encoding.ASCII.GetBytes("Packed file is corrupt");

		public static byte[] Unpack(byte[] executable, ushort loadSegment)
		{
			int headerSize = Word(executable, 0x08) * 16;
			int packedSize = Word(executable, 0x16) * 16;
			int packHeader = headerSize + packedSize;

			if (Word(executable, packHeader + 14) != PackedSignature)
			{
				throw new InvalidDataException("The program is not packed with EXEPACK");
			}

			byte[] image = new byte[Word(executable, packHeader + 12) * 16];
			Array.Copy(executable, headerSize, image, 0, packedSize);

			int source = packedSize;
			int target = image.Length;

			while (image[source - 1] == Padding)
			{
				source--;
			}

			while (true)
			{
				byte command = image[--source];
				source -= 2;
				int length = Word(image, source);

				switch (command & ~LastCommand)
				{
					case FillCommand:
						byte fill = image[--source];

						for (int count = 0; count < length; count++)
						{
							image[--target] = fill;
						}

						break;

					case CopyCommand:
						for (int count = 0; count < length; count++)
						{
							image[--target] = image[--source];
						}

						break;

					default:
						throw new InvalidDataException($"Unknown EXEPACK command 0x{command:x2}");
				}

				if ((command & LastCommand) != 0)
				{
					break;
				}
			}

			int relocations = executable.AsSpan(packHeader).IndexOf(CorruptMessage) + packHeader + CorruptMessage.Length;

			for (int group = 0; group < SegmentGroups; group++)
			{
				int count = Word(executable, relocations);
				relocations += 2;

				for (int index = 0; index < count; index++)
				{
					int at = (group * 0x10000) + Word(executable, relocations);
					relocations += 2;

					int segment = Word(image, at) + loadSegment;
					image[at] = (byte)segment;
					image[at + 1] = (byte)(segment >> 8);
				}
			}

			return image;
		}

		private static int Word(byte[] content, int at) => content[at] | (content[at + 1] << 8);
	}
}
