using System.IO;

namespace Civilization1Remaster.Machine
{
	public class FileStreamItem
	{
		private readonly FileStreamType kind = FileStreamType.Binary;
		private readonly FileStream flow;

		private bool strokeFinish = false;
		private char finalFinishLetter = '\r';
		private short putbackChar = -1;

		public FileStreamItem(FileStream flow)
		{
			this.flow = flow;
		}

		public FileStreamItem(FileStream flow, FileStreamType sort)
		{
			this.flow = flow;
			this.kind = sort;
		}

		public FileStreamType Kind
		{
			get { return this.kind; }
		}

		public FileStream Flow
		{
			get { return this.flow; }
		}

		public short PutbackChar
		{
			get
			{
				short scratch = this.putbackChar;
				this.putbackChar = -1;
				return scratch;
			}

			set
			{
				this.putbackChar = value;
			}
		}

		public short PullChar()
		{
			if (this.kind == FileStreamType.Binary)
			{
				if (this.putbackChar != -1)
				{
					return this.PutbackChar;
				}

				return (short)this.flow.ReadByte();
			}

			while (true)
			{
				int chidx;

				if (this.putbackChar != -1)
				{
					chidx = this.PutbackChar;
				}
				else
				{
					chidx = this.flow.ReadByte();
				}

				if (chidx == -1)
				{
					return -1;
				}

				char chr = (char)chidx;
				if (chr == '\r' || chr == '\n')
				{
					if (strokeFinish)
					{
						if (finalFinishLetter == chr)
						{
							return (short)chr;
						}
						else
						{
							strokeFinish = false;
							continue;
						}
					}
					else
					{
						strokeFinish = true;
						finalFinishLetter = chr;
						return (short)'\n';
					}
				}

				return (short)chr;
			}
		}
	}
}
