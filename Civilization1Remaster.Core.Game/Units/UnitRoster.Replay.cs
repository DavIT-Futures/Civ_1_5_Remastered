namespace Civilization1Remaster.Core.Game.Units
{
	public partial class UnitRoster
	{
		public void AttachReplayContent(ushort rerunIndex, byte firstContent)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIndex}, {firstContent})");
			WriteReplayEntry(rerunIndex, firstContent);
		}

		public void AttachReplayContent(ushort rerunIndex, byte firstContent, byte secondContent)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIndex}, {firstContent}, {secondContent})");
			WriteReplayEntry(rerunIndex, firstContent, secondContent);
		}

		public void AttachReplayContent(ushort rerunIndex, byte firstContent, byte secondContent, byte thirdContent)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIndex}, {firstContent}, {secondContent}, {thirdContent})");
			WriteReplayEntry(rerunIndex, firstContent, secondContent, thirdContent);
		}

		public void AttachReplayContent(ushort rerunIndex, byte firstContent, byte secondContent, byte thirdContent, byte fourthContent)
		{
			using var _ = this.Processor.Journal.Scope($"AttachReplayContent({rerunIndex}, {firstContent}, {secondContent}, {thirdContent}, {fourthContent})");
			WriteReplayEntry(rerunIndex, firstContent, secondContent, thirdContent, fourthContent);
		}

		private void WriteReplayEntry(ushort rerunIndex, params byte[] content)
		{
			if (this.Owner.World.RerunContentSpan + 2 + content.Length >= 0x1000)
			{
				return;
			}

			ushort interim = (ushort)((rerunIndex << 12) | ((ushort)this.Owner.World.RoundTally & 0xfff));
			this.Owner.World.RerunContent[this.Owner.World.RerunContentSpan++] = (byte)((interim & 0xff00) >> 8);
			this.Owner.World.RerunContent[this.Owner.World.RerunContentSpan++] = (byte)(interim & 0xff);

			foreach (byte entry in content)
			{
				this.Owner.World.RerunContent[this.Owner.World.RerunContentSpan++] = entry;
			}
		}
	}
}
