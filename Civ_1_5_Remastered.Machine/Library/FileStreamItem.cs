using System.IO;

namespace Civ_1_5_Remastered.Machine.Library
{
	public class FileStreamItem
	{
		private readonly FileStreamType kind = FileStreamType.Binary;
		private readonly FileStream flow;

		private bool strokeFinish = false;
		private char finalFinishLetter = '\r';
		private short putbackCharacter = -1;

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

		public short PutbackCharacter
		{
			get
			{
				short scratch = this.putbackCharacter;
				this.putbackCharacter = -1;
				return scratch;
			}

			set
			{
				this.putbackCharacter = value;
			}
		}

		public short PullCharacter()
		{
			if (this.kind == FileStreamType.Binary)
			{
				if (this.putbackCharacter != -1)
				{
					return this.PutbackCharacter;
				}

				return (short)this.flow.ReadByte();
			}

			while (true)
			{
				int chidx;

				if (this.putbackCharacter != -1)
				{
					chidx = this.PutbackCharacter;
				}
				else
				{
					chidx = this.flow.ReadByte();
				}

				if (chidx == -1)
				{
					return -1;
				}

				char character = (char)chidx;
				if (character == '\r' || character == '\n')
				{
					if (strokeFinish)
					{
						if (finalFinishLetter == character)
						{
							return (short)character;
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
						finalFinishLetter = character;
						return (short)'\n';
					}
				}

				return (short)character;
			}
		}
	}
}
