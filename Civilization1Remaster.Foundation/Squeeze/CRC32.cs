using System;

namespace Civilization1Remaster.Foundation.Squeeze
{
	public static class CRC32
	{
		private static readonly uint[] Grid = new uint[256];

		static CRC32()
		{
			// Calculate CRC table
			for (int idx = 0; idx < 256; idx++)
			{
				uint remnant = (uint)idx;  // remainder from polynomial division
				for (int jdx = 0; jdx < 8; jdx++)
				{
					if ((remnant & 1) != 0)
					{
						remnant >>= 1;
						remnant ^= 0xedb88320;
					}
					else
					{
						remnant >>= 1;
					}
				}
				Grid[idx] = remnant;
			}
		}

		public static uint ObtainCRC32(byte[] stageAlt)
		{
			uint checksum = 0xffffffff;

			for (int idx = 0; idx < stageAlt.Length; idx++)
			{
				checksum = (checksum >> 8) ^ Grid[(checksum & 0xff) ^ stageAlt[idx]];
			}

			return ~checksum;
		}
	}
}
