using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The entries of the replay</summary>
	public partial class UnitRoster
	{
		/// <inheritdoc/>
		public void AttachReplayContent(ushort rerunIdx, byte content1)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIdx}, {content1})");
			WriteReplayEntry(rerunIdx, content1);
		}

		/// <inheritdoc/>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIdx}, {content1}, {content2})");
			WriteReplayEntry(rerunIdx, content1, content2);
		}

		/// <inheritdoc/>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3})");
			WriteReplayEntry(rerunIdx, content1, content2, content3);
		}

		/// <inheritdoc/>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3, byte content4)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3}, {content4})");
			WriteReplayEntry(rerunIdx, content1, content2, content3, content4);
		}

		/// <summary>
		/// An entry of the replay of the game: the kind and the turn (4 and 12 bits, high byte first), then its bytes; nothing
		/// when the 0x1000 bytes of the replay have no room for all of it
		/// </summary>
		private void WriteReplayEntry(ushort rerunIdx, params byte[] content)
		{
			if (this.Owner.World.RerunContentSpan + 2 + content.Length >= 0x1000)
				return;

			ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.Owner.World.RoundTally & 0xfff));
			this.Owner.World.RerunContent[this.Owner.World.RerunContentSpan++] = (byte)((interim & 0xff00) >> 8);
			this.Owner.World.RerunContent[this.Owner.World.RerunContentSpan++] = (byte)(interim & 0xff);

			foreach (byte entry in content)
				this.Owner.World.RerunContent[this.Owner.World.RerunContentSpan++] = entry;
		}
	}
}
