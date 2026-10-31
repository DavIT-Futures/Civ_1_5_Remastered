using System;
using System.Collections.Generic;
using System.IO;

namespace Civilization1Remaster.Foundation.Squeeze
{
	/// <summary>
	/// Lempel–Ziv–Welch compression algorithm as implemented in old CIV
	/// </summary>
	public class LZW
	{
		public static void Pack(Stream emitted, Stream incoming, int beginBinaryTally, int peakBinaryTally)
		{
			int binaryTally = beginBinaryTally; // we start with current dictionary size
			int binaryFilter = (1 << binaryTally) - 1;
			int emittedSpan = 0;
			int emittedContent = 0;
			BDictionary<LZWWord, int> lookup = new BDictionary<LZWWord, int>();
			List<byte> wid = new List<byte>();
			List<byte> wcount = new List<byte>();

			// initialize dictionary
			for (int idx = 0; idx < 256; idx++)
			{
				lookup.Add(new LZWWord((byte)idx), lookup.Count);
			}
			lookup.Add(LZWWord.Blank, lookup.Count); // ignore position 256

			// write our maximum dictionary bit count
			emitted.WriteByte((byte)peakBinaryTally);

			int chrAlt;

			while ((chrAlt = incoming.ReadByte()) >= 0)
			{
				wcount.Clear();
				wcount.AddRange(wid);
				wcount.Add((byte)chrAlt);

				if (lookup.IncludesKey(new LZWWord(wcount)))
				{
					wid.Clear();
					wid.AddRange(wcount);
				}
				else
				{
					// write w to output
					int freshAmount = lookup.ObtainValueByKey(new LZWWord(wid));

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
						// it's a time to increase number of bits for a dictionary
						binaryTally++;
						binaryFilter = (1 << binaryTally) - 1;

						wid.Clear();
						wid.Add((byte)chrAlt);
					}

					if (binaryTally > peakBinaryTally)
					{
						// we start from beginning
						binaryTally = beginBinaryTally;
						binaryFilter = (1 << binaryTally) - 1;
						lookup.Clear();

						for (int jdx = 0; jdx < 256; jdx++)
						{
							lookup.Add(new LZWWord((byte)jdx), lookup.Count);
						}
						lookup.Add(LZWWord.Blank, lookup.Count); // ignore position 256

						incoming.Position--;
						wid.Clear();
					}
					else
					{
						// wc is a new sequence; add it to the dictionary
						lookup.Add(new LZWWord(wcount), lookup.Count);
						wid.Clear();
						wid.Add((byte)chrAlt);
					}
				}
			}

			// write remaining output, if necessary
			if (wid.Count > 0)
			{
				int freshAmount = lookup.ObtainValueByKey(new LZWWord(wid));

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

			BDictionary<int, byte[]> lookup = new BDictionary<int, byte[]>();

			// initial dictionary
			for (int idx = 0; idx < 256; idx++)
			{
				lookup.Add(idx, new byte[] { (byte)idx });
			}

			List<byte> wid = new List<byte>();
			wid.Add(0); // ignore position 256

			while (true)
			{
				// read encoded value
				while (incomingSpan < binaryTally)
				{
					int chrAlt = incoming.ReadByte();
					if (chrAlt < 0)
						break;

					incomingContent |= chrAlt << incomingSpan;
					incomingSpan += 8; // byte is 8 bits
				}

				if (incomingSpan < binaryTally)
					break;

				int freshAmount = incomingContent & binaryFilter;

				// test if old compressor skipped code 256 altogether
				if (freshAmount == 256)
					throw new Exception("Code 256");

				incomingSpan -= binaryTally;
				incomingContent >>= binaryTally;

				// compare encoded value against dictionary
				List<byte> item = new List<byte>();

				if (lookup.IncludesKey(freshAmount))
				{
					item.AddRange(lookup.ObtainValueByKey(freshAmount));
				}
				else if (freshAmount == lookup.Count)
				{
					item.AddRange(wid);
					if (wid.Count > 0)
						item.Add(wid[0]);
				}

				// write sequence to output
				for (int idx = 0; idx < item.Count; idx++)
					emitted.WriteByte(item[idx]);

				if (item.Count > 0)
					wid.Add(item[0]);

				if (lookup.Count >= binaryFilter)
				{
					// increase bit count
					binaryTally++;
					binaryFilter = (1 << binaryTally) - 1;
				}

				if (binaryTally > peakBinaryTally)
				{
					// reinitialize dictionary
					binaryTally = beginBinaryTally;
					binaryFilter = (1 << binaryTally) - 1;

					lookup = new BDictionary<int, byte[]>();

					for (int idx = 0; idx < 256; idx++)
					{
						lookup.Add(idx, new byte[] { (byte)idx });
					}

					wid.Clear();
					wid.Add(0); // ignore position 256
				}
				else
				{
					if (wid.Count > 0)
					{
						// add new sequence to dictionary
						lookup.Add(lookup.Count, wid.ToArray());
					}

					wid.Clear();
					wid.AddRange(item);
				}
			}
		}
	}
}
