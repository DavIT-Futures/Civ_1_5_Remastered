using System;
using System.Collections.Generic;

namespace Civilization1Remaster.Foundation.Squeeze
{
	public sealed class LZWWord : IEquatable<LZWWord>
	{
		private readonly byte[] elements;
		private readonly int digestCipher;

		public LZWWord()
			: this(Array.Empty<byte>())
		{
		}

		public LZWWord(byte amount)
			: this(new byte[] { amount })
		{
		}

		public LZWWord(List<byte> bag)
			: this(bag.ToArray())
		{
		}

		private LZWWord(byte[] elements)
		{
			this.elements = elements;
			this.digestCipher = (int)CRC32.ObtainCRC32(elements);
		}

		public static LZWWord Blank { get; } = new LZWWord();

		public bool Equals(LZWWord? other)
		{
			return other is not null && this.elements.AsSpan().SequenceEqual(other.elements);
		}

		public override bool Equals(object? obj)
		{
			return obj is LZWWord word && this.Equals(word);
		}

		public override int GetHashCode()
		{
			return this.digestCipher;
		}
	}
}
