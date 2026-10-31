using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Civilization1Remaster.Foundation.Squeeze
{
	/// <summary>
	/// Run-length compression algorithm as implemented in original CIV
	/// </summary>
	public static class RLE
	{
		public static void Pack(Stream emitted, Stream incoming, int floorTally)
		{
			int priorAmount = -1;
			int total = 0;
			int chrAlt;
			floorTally = Math.Max(floorTally, 2);

			while ((chrAlt = incoming.ReadByte()) >= 0)
			{
				if (chrAlt < 0)
					break;

				if (chrAlt == priorAmount && total < 255)
				{
					total++;
				}
				else
				{
					if (total > floorTally)
					{
						emitted.WriteByte((byte)priorAmount);
						if (priorAmount == 0x90)
							emitted.WriteByte(0);
						emitted.WriteByte(0x90);
						emitted.WriteByte((byte)total);

						priorAmount = chrAlt;
						total = 1;
					}
					else
					{
						for (int idx = 0; idx < total; idx++)
						{
							emitted.WriteByte((byte)priorAmount);
							if (priorAmount == 0x90)
								emitted.WriteByte(0);
						}
						priorAmount = chrAlt;
						total = 1;
					}
				}
			}
			if (total > floorTally)
			{
				emitted.WriteByte((byte)priorAmount);
				if (priorAmount == 0x90)
					emitted.WriteByte(0);
				emitted.WriteByte(0x90);
				emitted.WriteByte((byte)total);
			}
			else
			{
				for (int idx = 0; idx < total; idx++)
				{
					emitted.WriteByte((byte)priorAmount);
					if (priorAmount == 0x90)
						emitted.WriteByte(0);
				}
			}
		}

		public static void Unpack(Stream emitted, Stream incoming)
		{
			byte formerQuantity = 0;
			int chrAlt;

			while ((chrAlt = incoming.ReadByte()) >= 0)
			{
				if (chrAlt == 0x90)
				{
					int spanInt = incoming.ReadByte();
					if (spanInt < 0)
						break;

					if (spanInt == 0)
					{
						formerQuantity = (byte)chrAlt;
						emitted.WriteByte(formerQuantity);
					}
					else
					{
						for (int idx = 1; idx < spanInt; idx++)
						{
							emitted.WriteByte(formerQuantity);
						}
					}
				}
				else
				{
					formerQuantity = (byte)chrAlt;
					emitted.WriteByte(formerQuantity);
				}
			}
		}
	}
}
