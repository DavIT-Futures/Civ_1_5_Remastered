using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;

namespace Civilization1Remaster.Imaging.Typefaces
{
	public class CivFonts: BDictionary<int, CivFont>
	{
		public CivFonts() : base()
		{ }

		public static CivFonts IngestFromOldStructure(byte[] typefaceLayout)
		{
			CivFonts typefaces = new CivFonts();

			int total = FontStructPullWord(typefaceLayout, 0);

			for (int idx = 0; idx < total; idx++)
			{
				CivFont typeface = new CivFont();
				int typefaceAddr = FontStructPullWord(typefaceLayout, (idx + 1) * 2);

				char leadingLetterCipher = (char)typefaceLayout[typefaceAddr - 8];
				char finalLetterCipher = (char)typefaceLayout[typefaceAddr - 7];
				int octetsEachLetter = typefaceLayout[typefaceAddr - 6];
				int letterBreadth = typefaceLayout[typefaceAddr - 5];
				int letterAltitude = typefaceLayout[typefaceAddr - 4];
				int letterBreadthGap = typefaceLayout[typefaceAddr - 3];
				int strokeGap = typefaceLayout[typefaceAddr - 2];

				int letterCipherSpan = (int)(finalLetterCipher - leadingLetterCipher);
				int typefaceBreadthLookupAddr = 0;
				if (letterBreadth == 0)
				{
					typefaceBreadthLookupAddr = (ushort)(typefaceAddr - 9 - letterCipherSpan);
				}
				int letterLookupLineBreadth = (ushort)((letterCipherSpan + 1) << (octetsEachLetter - 1));

				typeface.LetterBreadthGap = letterBreadthGap;
				typeface.StrokeGap = strokeGap;

				for (int jdx = 0; jdx < letterCipherSpan; jdx++)
				{
					CivFontCharacter typefaceLetter = new CivFontCharacter();
					char chr = (char)(leadingLetterCipher + jdx);

					if (typefaceBreadthLookupAddr != 0)
					{
						typefaceLetter.Breadth = typefaceLayout[typefaceBreadthLookupAddr + jdx];
					}
					else
					{
						typefaceLetter.Breadth = letterBreadth;
					}
					typefaceLetter.Altitude = letterAltitude;

					// parse bitmap
					int letterAddr = typefaceAddr + (octetsEachLetter * jdx);
					int[][] image = new int[typefaceLetter.Altitude][];

					for (int kdx = 0; kdx < typefaceLetter.Altitude; kdx++)
					{
						int binaryTally = 0;
						int quantity = typefaceLayout[letterAddr];
						int octetTotal = 0;
						image[kdx] = new int[typefaceLetter.Breadth];

						for (int lng = 0; lng < typefaceLetter.Breadth; lng++)
						{
							if (binaryTally >= 8)
							{
								binaryTally = 0;
								octetTotal++;
								quantity = typefaceLayout[letterAddr + octetTotal];
							}
							image[kdx][lng] = 0;
							if ((quantity & 0x80) != 0)
							{
								image[kdx][lng] = 1;
							}
							binaryTally++;
							quantity <<= 1;
						}
						letterAddr += letterLookupLineBreadth;
					}
					typefaceLetter.Picture = image;

					typeface.Letters.Add(chr, typefaceLetter);
				}

				typefaces.Add(idx + 1, typeface);
			}

			return typefaces;
		}

		/// <summary>
		/// Deserializes a CivFonts object.
		/// </summary>
		/// <param name="path">A path to the CivFonts xml</param>
		/// <returns>A deserialized CivFonts object</returns>
		public static CivFonts Unpack(string route, bool packed)
		{
			return Unpack(route + (packed ? ".gz" : ""));
		}

		/// <summary>
		/// Deserializes a GeneOntology object.
		/// Assumes file iz gzipped, if the filename ends with .gz
		/// </summary>
		/// <param name="path">A path to CivFonts xml</param>
		/// <returns>A deserialized CivFonts object</returns>
		public static CivFonts Unpack(string route)
		{
			Stream source;
			if (route.EndsWith(".gz"))
			{
				source = new GZipStream(new BufferedStream(new FileStream(route, FileMode.Open, FileAccess.Read, FileShare.Read), 65536), CompressionMode.Decompress);
			}
			else
			{
				source = new BufferedStream(new FileStream(route, FileMode.Open, FileAccess.Read, FileShare.Read), 65536);
			}

			CivFonts entity = Unpack(source);

			source.Close();

			return entity;
		}

		/// <summary>
		/// Deserializes a CivFonts object.
		/// </summary>
		/// <param name="reader">A stream to read the object from.</param>
		/// <returns>A deserialized CivFonts object.</returns>
		public static CivFonts Unpack(Stream source)
		{
			XmlSerializer xml = new XmlSerializer(typeof(CivFonts));
			object? thing = xml.Deserialize(source);
			if (thing == null)
				throw new Exception("Can't deserialize object");

			CivFonts freshThing = (CivFonts)thing;

			return freshThing;
		}

		/// <summary>
		/// Serializes a CivFonts object.
		/// </summary>
		/// <param name="path">A path to serialize CivFonts to</param>
		public void Pack(string route, bool packed)
		{
			Pack(route + (packed ? ".gz" : ""));
		}

		/// <summary>
		/// Serializes a CivFonts object.
		/// Assumes file iz gzipped, if the filename ends with .gz
		/// </summary>
		/// <param name="filePath">A path to a object xml file</param>
		public void Pack(string documentRoute)
		{
			StreamWriter sink;
			if (documentRoute.EndsWith(".gz"))
			{
				sink = new StreamWriter(new GZipStream(new FileStream(documentRoute, FileMode.Create, FileAccess.Write, FileShare.Read), CompressionMode.Compress));
			}
			else
			{
				sink = new StreamWriter(new FileStream(documentRoute, FileMode.Create, FileAccess.Write, FileShare.Read));
			}

			Pack(sink);

			sink.Flush();
			sink.Close();
		}

		/// <summary>
		/// Serializes a CivFonts object.
		/// </summary>
		/// <param name="writer">A stream to serialize the object to.</param>
		public void Pack(StreamWriter sink)
		{
			XmlSerializer xml = new XmlSerializer(typeof(CivFonts));
			xml.Serialize(sink, this);
		}

		private static ushort FontStructPullWord(byte[] typefaceStaging, int place)
		{
			return (ushort)((ushort)typefaceStaging[place] | ((ushort)typefaceStaging[place + 1] << 8));
		}
	}
}
