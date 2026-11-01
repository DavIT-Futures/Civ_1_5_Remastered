using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.Foundation.Squeeze
{
	public sealed class DictionaryCompressionWord : IEquatable<DictionaryCompressionWord>
	{
		private readonly byte[] elements;
		private readonly int digestCipher;

		public DictionaryCompressionWord()
			: this(Array.Empty<byte>())
		{
		}

		public DictionaryCompressionWord(byte amount)
			: this(new byte[] { amount })
		{
		}

		public DictionaryCompressionWord(List<byte> bag)
			: this(bag.ToArray())
		{
		}

		private DictionaryCompressionWord(byte[] elements)
		{
			this.elements = elements;
			this.digestCipher = (int)CyclicRedundancyCheck.ObtainChecksum(elements);
		}

		public static DictionaryCompressionWord Blank { get; } = new DictionaryCompressionWord();

		public bool Equals(DictionaryCompressionWord? other)
		{
			return other is not null && this.elements.AsSpan().SequenceEqual(other.elements);
		}

		public override bool Equals(object? obj)
		{
			return obj is DictionaryCompressionWord word && this.Equals(word);
		}

		public override int GetHashCode()
		{
			return this.digestCipher;
		}
	}
}
