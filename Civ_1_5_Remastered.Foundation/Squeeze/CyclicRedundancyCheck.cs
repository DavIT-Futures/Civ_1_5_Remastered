namespace Civ_1_5_Remastered.Foundation.Squeeze
{
	public static class CyclicRedundancyCheck
	{
		private static readonly uint[] Grid = new uint[256];

		static CyclicRedundancyCheck()
		{
			for (int index = 0; index < 256; index++)
			{
				uint remnant = (uint)index;
				for (int innerIndex = 0; innerIndex < 8; innerIndex++)
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

				Grid[index] = remnant;
			}
		}

		public static uint ObtainChecksum(byte[] stageAlternate)
		{
			uint checksum = 0xffffffff;

			for (int index = 0; index < stageAlternate.Length; index++)
			{
				checksum = (checksum >> 8) ^ Grid[(checksum & 0xff) ^ stageAlternate[index]];
			}

			return ~checksum;
		}
	}
}
