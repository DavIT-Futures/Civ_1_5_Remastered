using System;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Remaster.Foundation.Squeeze
{
	public class LZW
	{
		public static void Pack(Stream emitted, Stream incoming, int beginBinaryTally, int peakBinaryTally)
		{
			int binaryTally = beginBinaryTally;
			int binaryFilter = (1 << binaryTally) - 1;
			int emittedSpan = 0;
			int emittedContent = 0;
			Dictionary<LZWWord, int> lookup = new Dictionary<LZWWord, int>();
			List<byte> wid = new List<byte>();
			List<byte> wcount = new List<byte>();

			for (int idx = 0; idx < 256; idx++)
			{
				lookup.Add(new LZWWord((byte)idx), lookup.Count);
			}
			lookup.Add(LZWWord.Blank, lookup.Count);

			emitted.WriteByte((byte)peakBinaryTally);

			int chrAlt;

			while ((chrAlt = incoming.ReadByte()) >= 0)
			{
				wcount.Clear();
				wcount.AddRange(wid);
				wcount.Add((byte)chrAlt);

				if (lookup.ContainsKey(new LZWWord(wcount)))
				{
					wid.Clear();
					wid.AddRange(wcount);
				}
				else
				{
					int freshAmount = lookup[new LZWWord(wid)];

					emittedContent |= freshAmount << emittedSpan;
					emittedSpan += binaryTally;

					while (emittedSpan >= 8)
					{
						emitted.WriteByte((byte)(emittedContent & 0xff));

						emittedContent >>= 8;
						emittedSpan -= 8;
					}

					if (lookup.Count > binaryFilter)
					{
						binaryTally++;
						binaryFilter = (1 << binaryTally) - 1;

						wid.Clear();
						wid.Add((byte)chrAlt);
					}

					if (binaryTally > peakBinaryTally)
					{
						binaryTally = beginBinaryTally;
						binaryFilter = (1 << binaryTally) - 1;
						lookup.Clear();

						for (int jdx = 0; jdx < 256; jdx++)
						{
							lookup.Add(new LZWWord((byte)jdx), lookup.Count);
						}
						lookup.Add(LZWWord.Blank, lookup.Count);

						incoming.Position--;
						wid.Clear();
					}
					else
					{
						lookup.Add(new LZWWord(wcount), lookup.Count);
						wid.Clear();
						wid.Add((byte)chrAlt);
					}
				}
			}

			if (wid.Count > 0)
			{
				int freshAmount = lookup[new LZWWord(wid)];

				emittedContent |= freshAmount << emittedSpan;
				emittedSpan += binaryTally;

				while (emittedSpan >= 8)
				{
					emitted.WriteByte((byte)(emittedContent & 0xff));

					emittedContent >>= 8;
					emittedSpan -= 8;
				}

				if (emittedSpan > 0)
				{
					emitted.WriteByte((byte)(emittedContent & 0xff));
				}
			}
		}

		public static void Unpack(Stream emitted, Stream incoming, int beginBinaryTally, int peakBinaryTally)
		{
			int incomingContent = 0;
			int incomingSpan = 0;
			int binaryTally = beginBinaryTally;
			int binaryFilter = (1 << binaryTally) - 1;

			Dictionary<int, byte[]> lookup = new Dictionary<int, byte[]>();

			for (int idx = 0; idx < 256; idx++)
			{
				lookup.Add(idx, new byte[] { (byte)idx });
			}

			List<byte> wid = new List<byte>();
			wid.Add(0);

			while (true)
			{
				while (incomingSpan < binaryTally)
				{
					int chrAlt = incoming.ReadByte();
					if (chrAlt < 0)
						break;

					incomingContent |= chrAlt << incomingSpan;
					incomingSpan += 8;
				}

				if (incomingSpan < binaryTally)
					break;

				int freshAmount = incomingContent & binaryFilter;

				if (freshAmount == 256)
					throw new Exception("Code 256");

				incomingSpan -= binaryTally;
				incomingContent >>= binaryTally;

				List<byte> item = new List<byte>();

				if (lookup.ContainsKey(freshAmount))
				{
					item.AddRange(lookup[freshAmount]);
				}
				else if (freshAmount == lookup.Count)
				{
					item.AddRange(wid);
					if (wid.Count > 0)
						item.Add(wid[0]);
				}

				for (int idx = 0; idx < item.Count; idx++)
					emitted.WriteByte(item[idx]);

				if (item.Count > 0)
					wid.Add(item[0]);

				if (lookup.Count >= binaryFilter)
				{
					binaryTally++;
					binaryFilter = (1 << binaryTally) - 1;
				}

				if (binaryTally > peakBinaryTally)
				{
					binaryTally = beginBinaryTally;
					binaryFilter = (1 << binaryTally) - 1;

					lookup = new Dictionary<int, byte[]>();

					for (int idx = 0; idx < 256; idx++)
					{
						lookup.Add(idx, new byte[] { (byte)idx });
					}

					wid.Clear();
					wid.Add(0);
				}
				else
				{
					if (wid.Count > 0)
					{
						lookup.Add(lookup.Count, wid.ToArray());
					}

					wid.Clear();
					wid.AddRange(item);
				}
			}
		}
	}
}
