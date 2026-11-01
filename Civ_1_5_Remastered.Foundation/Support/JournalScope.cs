using System;

namespace Civ_1_5_Remastered.Foundation.Support
{
	public readonly struct JournalScope : IDisposable
	{
		private readonly CallJournal? journal;
		private readonly string name;

		internal JournalScope(CallJournal journal, string name)
		{
			this.journal = journal;
			this.name = name;
		}

		public void Dispose()
		{
			this.journal?.CloseScope(this.name);
		}
	}
}
