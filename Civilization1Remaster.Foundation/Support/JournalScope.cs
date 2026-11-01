using System;

namespace Civilization1Remaster.Foundation.Support
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
