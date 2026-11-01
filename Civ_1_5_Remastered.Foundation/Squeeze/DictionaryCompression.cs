using System;
using System.Collections.Generic;
using System.IO;

namespace Civ_1_5_Remastered.Foundation.Squeeze
{
	public class DictionaryCompression
	{
		public static void Pack(Stream emitted, Stream incoming, int beginBinaryTally, int peakBinaryTally)
		{
			int binaryTally = beginBinaryTally;
			int binaryFilter = (1 << binaryTally) - 1;
			int emittedSpan = 0;
			int emittedContent = 0;
			Dictionary<DictionaryCompressionWord, int> lookup = new Dictionary<DictionaryCompressionWord, int>();
			List<byte> width = new List<byte>();
			List<byte> wordCount = new List<byte>();

			for (int index = 0; index < 256; index++)
			{
				lookup.Add(new DictionaryCompressionWord((byte)index), lookup.Count);
			}

			lookup.Add(DictionaryCompressionWord.Blank, lookup.Count);

			emitted.WriteByte((byte)peakBinaryTally);

			int characterAlternate;

			while ((characterAlternate = incoming.ReadByte()) >= 0)
			{
				wordCount.Clear();
				wordCount.AddRange(width);
				wordCount.Add((byte)characterAlternate);

				if (lookup.ContainsKey(new DictionaryCompressionWord(wordCount)))
				{
					width.Clear();
					width.AddRange(wordCount);
				}
				else
				{
					int freshAmount = lookup[new DictionaryCompressionWord(width)];

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

						width.Clear();
						width.Add((byte)characterAlternate);
					}

					if (binaryTally > peakBinaryTally)
					{
						binaryTally = beginBinaryTally;
						binaryFilter = (1 << binaryTally) - 1;
						lookup.Clear();

						for (int innerIndex = 0; innerIndex < 256; innerIndex++)
						{
							lookup.Add(new DictionaryCompressionWord((byte)innerIndex), lookup.Count);
						}

						lookup.Add(DictionaryCompressionWord.Blank, lookup.Count);

						incoming.Position--;
						width.Clear();
					}
					else
					{
						lookup.Add(new DictionaryCompressionWord(wordCount), lookup.Count);
						width.Clear();
						width.Add((byte)characterAlternate);
					}
				}
			}

			if (width.Count > 0)
			{
				int freshAmount = lookup[new DictionaryCompressionWord(width)];

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

			for (int index = 0; index < 256; index++)
			{
				lookup.Add(index, new byte[] { (byte)index });
			}

			List<byte> width = new List<byte>();
			width.Add(0);

			while (true)
			{
				while (incomingSpan < binaryTally)
				{
					int characterAlternate = incoming.ReadByte();
					if (characterAlternate < 0)
					{
						break;
					}

					incomingContent |= characterAlternate << incomingSpan;
					incomingSpan += 8;
				}

				if (incomingSpan < binaryTally)
				{
					break;
				}

				int freshAmount = incomingContent & binaryFilter;

				if (freshAmount == 256)
				{
					throw new Exception("Code 256");
				}

				incomingSpan -= binaryTally;
				incomingContent >>= binaryTally;

				List<byte> item = new List<byte>();

				if (lookup.ContainsKey(freshAmount))
				{
					item.AddRange(lookup[freshAmount]);
				}
				else if (freshAmount == lookup.Count)
				{
					item.AddRange(width);
					if (width.Count > 0)
					{
						item.Add(width[0]);
					}
				}

				for (int index = 0; index < item.Count; index++)
				{
					emitted.WriteByte(item[index]);
				}

				if (item.Count > 0)
				{
					width.Add(item[0]);
				}

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

					for (int index = 0; index < 256; index++)
					{
						lookup.Add(index, new byte[] { (byte)index });
					}

					width.Clear();
					width.Add(0);
				}
				else
				{
					if (width.Count > 0)
					{
						lookup.Add(lookup.Count, width.ToArray());
					}

					width.Clear();
					width.AddRange(item);
				}
			}
		}
	}
}
