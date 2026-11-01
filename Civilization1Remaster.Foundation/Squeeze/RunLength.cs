using System;
using System.IO;

namespace Civilization1Remaster.Foundation.Squeeze
{
	public static class RunLength
	{
		public static void Pack(Stream emitted, Stream incoming, int floorTally)
		{
			int priorAmount = -1;
			int total = 0;
			int characterAlternate;
			floorTally = Math.Max(floorTally, 2);

			while ((characterAlternate = incoming.ReadByte()) >= 0)
			{
				if (characterAlternate < 0)
				{
					break;
				}

				if (characterAlternate == priorAmount && total < 255)
				{
					total++;
				}
				else
				{
					if (total > floorTally)
					{
						emitted.WriteByte((byte)priorAmount);
						if (priorAmount == 0x90)
						{
							emitted.WriteByte(0);
						}

						emitted.WriteByte(0x90);
						emitted.WriteByte((byte)total);

						priorAmount = characterAlternate;
						total = 1;
					}
					else
					{
						for (int index = 0; index < total; index++)
						{
							emitted.WriteByte((byte)priorAmount);
							if (priorAmount == 0x90)
							{
								emitted.WriteByte(0);
							}
						}

						priorAmount = characterAlternate;
						total = 1;
					}
				}
			}

			if (total > floorTally)
			{
				emitted.WriteByte((byte)priorAmount);
				if (priorAmount == 0x90)
				{
					emitted.WriteByte(0);
				}

				emitted.WriteByte(0x90);
				emitted.WriteByte((byte)total);
			}
			else
			{
				for (int index = 0; index < total; index++)
				{
					emitted.WriteByte((byte)priorAmount);
					if (priorAmount == 0x90)
					{
						emitted.WriteByte(0);
					}
				}
			}
		}

		public static void Unpack(Stream emitted, Stream incoming)
		{
			byte formerQuantity = 0;
			int characterAlternate;

			while ((characterAlternate = incoming.ReadByte()) >= 0)
			{
				if (characterAlternate == 0x90)
				{
					int spanValue = incoming.ReadByte();
					if (spanValue < 0)
					{
						break;
					}

					if (spanValue == 0)
					{
						formerQuantity = (byte)characterAlternate;
						emitted.WriteByte(formerQuantity);
					}
					else
					{
						for (int index = 1; index < spanValue; index++)
						{
							emitted.WriteByte(formerQuantity);
						}
					}
				}
				else
				{
					formerQuantity = (byte)characterAlternate;
					emitted.WriteByte(formerQuantity);
				}
			}
		}
	}
}
