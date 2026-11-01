using System.Runtime.CompilerServices;

namespace Civ_1_5_Remastered.Foundation.Support
{
	[InterpolatedStringHandler]
	public ref struct ScopeText
	{
		private readonly bool enabled;
		private DefaultInterpolatedStringHandler inner;

		public ScopeText(int literalLength, int formattedCount, CallJournal journal, out bool enabled)
		{
			this.enabled = enabled = journal.NeedsText;
			this.inner = enabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
		}

		public void AppendLiteral(string value) => this.inner.AppendLiteral(value);

		public void AppendFormatted<T>(T value) => this.inner.AppendFormatted(value);

		public void AppendFormatted<T>(T value, string? format) => this.inner.AppendFormatted(value, format);

		public void AppendFormatted<T>(T value, int alignment) => this.inner.AppendFormatted(value, alignment);

		public void AppendFormatted<T>(T value, int alignment, string? format) => this.inner.AppendFormatted(value, alignment, format);

		public void AppendFormatted(string? value) => this.inner.AppendFormatted(value);

		internal string ToStringAndClear() => this.enabled ? this.inner.ToStringAndClear() : string.Empty;
	}
}
