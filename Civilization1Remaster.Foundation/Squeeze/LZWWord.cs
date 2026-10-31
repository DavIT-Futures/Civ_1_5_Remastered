using System;
using System.Collections.Generic;

namespace Civilization1Remaster.Foundation.Squeeze
{
	/// <summary>
	/// A byte sequence used as a key of the LZW dictionary.
	/// </summary>
	public sealed class LZWWord : IEquatable<LZWWord>
	{
		private readonly byte[] elements;
		private readonly int digestCipher;

		public LZWWord()
			: this(Array.Empty<byte>())
		{ }

		public LZWWord(byte amount)
			: this(new byte[] { amount })
		{ }

		public LZWWord(List<byte> bag)
			: this(bag.ToArray())
		{ }

		private LZWWord(byte[] elements)
		{
			this.elements = elements;
			this.digestCipher = (int)CRC32.ObtainCRC32(elements);
		}

		public static LZWWord Blank { get; } = new LZWWord();

		public bool Equals(LZWWord? alternate)
		{
			return alternate is not null && this.elements.AsSpan().SequenceEqual(alternate.elements);
		}

		public override bool Equals(object? thing)
		{
			return thing is LZWWord word && this.Equals(word);
		}

		public override int GetHashCode()
		{
			return this.digestCipher;
		}
	}
}
