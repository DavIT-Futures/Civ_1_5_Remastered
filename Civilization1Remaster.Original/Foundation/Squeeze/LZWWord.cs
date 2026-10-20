using System;
using System.Collections.Generic;
using System.Text;

namespace Civilization1Original.Squeeze
{
	public class LZWWord : IEquatable<LZWWord>, IEquatable<List<byte>>,
		IComparable<LZWWord>, IComparable<List<byte>>
	{
		protected byte[] Elements = new byte[] { };
		protected int DigestCipher = 0;
		private static readonly LZWWord BlankWord = new LZWWord();

		public LZWWord()
		{ }

		public LZWWord(byte amount)
		{
			this.Elements = new byte[] { amount };
			this.DigestCipher = (int)CRC32.ObtainCRC32(this.Elements);
		}

		public LZWWord(byte[] bag)
		{
			this.Elements = new byte[bag.Length];
			Array.Copy(bag, this.Elements, bag.Length);
			this.DigestCipher = (int)CRC32.ObtainCRC32(this.Elements);
		}

		public LZWWord(List<byte> bag)
		{
			this.Elements = bag.ToArray();
			this.DigestCipher = (int)CRC32.ObtainCRC32(this.Elements);
		}

		public LZWWord(IEnumerable<byte> bag)
		{
			this.Elements = (new List<byte>(bag)).ToArray();
			this.DigestCipher = (int)CRC32.ObtainCRC32(this.Elements);
		}

		public static LZWWord Blank
		{
			get { return BlankWord; }
		}

		public byte this[int ordinal]
		{
			get { return this.Elements[ordinal]; }
		}

		public int Span
		{
			get { return this.Elements.Length; }
		}

		public static LZWWord operator +(LZWWord lhs, LZWWord rhs)
		{
			List<byte> outcome = new List<byte>(lhs.Elements);
			outcome.AddRange(rhs.Elements);

			return new LZWWord(outcome);
		}

		public static LZWWord operator +(LZWWord lhs, byte rhs)
		{
			List<byte> outcome = new List<byte>(lhs.Elements);
			outcome.Add(rhs);

			return new LZWWord(outcome);
		}

		public static bool operator ==(LZWWord? lhs, LZWWord? rhs)
		{
			if (lhs is null && rhs is null)
				return true;

			if (lhs is null || rhs is null)
				return false;

			return lhs.Equals(rhs);
		}

		public static bool operator !=(LZWWord? lhs, LZWWord? rhs)
		{
			if (lhs is null && rhs is null)
				return false;

			if (lhs is null || rhs is null)
				return true;

			return !lhs.Equals(rhs);
		}

		public int CompareTo(LZWWord? alternate)
		{
			if (alternate is null)
				return 1;

			if (this.Elements.Length != alternate.Elements.Length)
				return this.Elements.Length.CompareTo(alternate.Elements.Length);

			for (int idx = 0; idx < this.Elements.Length; idx++)
			{
				if (this.Elements[idx] != alternate.Elements[idx])
					return this.Elements[idx].CompareTo(alternate.Elements[idx]);
			}

			return 0;
		}

		public int CompareTo(List<byte>? alternate)
		{
			if (alternate is null)
				return 1;

			if (this.Elements.Length != alternate.Count)
				return this.Elements.Length.CompareTo(alternate.Count);

			for (int idx = 0; idx < this.Elements.Length; idx++)
			{
				if (this.Elements[idx] != alternate[idx])
					return this.Elements[idx].CompareTo(alternate[idx]);
			}

			return 0;
		}

		public bool Equals(LZWWord? alternate)
		{
			return this.CompareTo(alternate) == 0;
		}

		public bool Equals(List<byte>? alternate)
		{
			return this.CompareTo(alternate) == 0;
		}

		public override bool Equals(object? thing)
		{
			if (thing is null)
			{
				return false;
			}
			else if (thing is LZWWord)
			{
				return this.CompareTo((LZWWord)thing) == 0;
			}
			else if (thing is List<byte>)
			{
				return this.CompareTo((List<byte>)thing) == 0;
			}

			return base.Equals(thing);
		}

		public override int GetHashCode()
		{
			return DigestCipher;
		}
	}
}
