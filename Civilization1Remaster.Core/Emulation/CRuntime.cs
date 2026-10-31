using System;
using System.IO;
using System.Text;
using System.Threading;
using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Emulation
{
	public class CRuntime : GameModule
	{
		private RandomMT19937 dice = new RandomMT19937();

		/// <summary>The open files by handle (B6.6: kept here, no longer in the CPU); the first handle is 0x20, like DOS gave</summary>
		private readonly BDictionary<short, FileStreamItem> files = new BDictionary<short, FileStreamItem>();
		private short nextHandle = 0x20;

		public CRuntime(GameEngine owner)
			: base(owner)
		{
		}

		public void TerminateProgram(short cipher)
		{
			this.processor.Journal.EmitLine($"exit({cipher})");
			this.processor.Quit(cipher);
		}

		#region Keyboard operations
		public short KeyAvailable()
		{
			// Loops that wait for a key alone still get the interrupts (the timer, the mouse of the window)
			this.processor.PumpMessages();

			if (this.owner.VideoCard.Tokens.Count == 0)
				this.processor.InputStarved?.Invoke(false);

			this.processor.RegAcc.Value16 = (ushort)((this.owner.VideoCard.Tokens.Count > 0) ? 0xffff : 0);

			return (short)this.processor.RegAcc.Value16;
		}

		public short ReadKey()
		{
			while (this.owner.VideoCard.Tokens.Count == 0)
			{
				this.processor.InputStarved?.Invoke(true);
				if (this.owner.VideoCard.Tokens.Count > 0)
					break;

				Thread.Sleep(10);
				this.processor.PumpMessages();
			}

			lock (this.owner.VideoCard.VideoLatch)
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.VideoCard.Tokens.Dequeue();
			}

			return (short)this.processor.RegAcc.Value16;
		}
		#endregion

		#region Memory operations
		public ushort CopyBytes(ushort endpoint, ushort originAlt, ushort tally)
		{
			this.processor.Journal.OpenScope($"CopyBytes(0x{endpoint:x4}, 0x{originAlt:x4}, {tally})");

			// function body
			int heading = 1;
			uint target = CPU.AsLinearAddress(this.processor.SegData.Value16, endpoint);
			uint origin = CPU.AsLinearAddress(this.processor.SegData.Value16, originAlt);
			int total = tally;

			if (target > origin && (origin + total) >= target)
			{
				origin = (uint)(origin + total - 1);
				target = (uint)(target + total - 1);
				heading = -1;
			}

			while (total > 0)
			{
				this.processor.Storage.EmitUInt8(target, this.processor.Storage.PullUInt8(origin));
				target = (uint)((long)target + heading);
				origin = (uint)((long)origin + heading);
				total--;
			}

			// Far return
			this.processor.Journal.CloseScope("CopyBytes");
			this.processor.RegAcc.Value16 = endpoint; // for compatibility reasons
			return endpoint;
		}

		public ushort FillBytes(ushort endpoint, byte amount, ushort tally)
		{
			this.processor.Journal.OpenScope($"FillBytes(0x{endpoint:x4}, 0x{amount:x2}, {tally})");

			// function body
			uint target = CPU.AsLinearAddress(this.processor.SegData.Value16, endpoint);
			int total = tally;

			while (total > 0)
			{
				this.processor.Storage.EmitUInt8(target, amount);
				target++;
				total--;
			}

			this.processor.Journal.CloseScope("FillBytes");
			this.processor.RegAcc.Value16 = endpoint; // for compatibility reasons
			return endpoint;
		}

		public void MoveFarData(ushort originSection, ushort originDisplacement, ushort targetSection, ushort targetDisplacement, ushort tally)
		{
			this.processor.Journal.OpenScope($"MoveFarData(0x{originSection:x4}, 0x{originDisplacement:x4}, 0x{targetSection:x4}, 0x{targetDisplacement:x4}, {tally})");

			// function body
			int heading = 1;
			uint target = CPU.AsLinearAddress(targetSection, targetDisplacement);
			uint origin = CPU.AsLinearAddress(originSection, originDisplacement);
			int total = tally;

			if (target > origin && (origin + total) >= target)
			{
				origin = (uint)(origin + total - 1);
				target = (uint)(target + total - 1);
				heading = -1;
			}

			while (total > 0)
			{
				this.processor.Storage.EmitUInt8(target, this.processor.Storage.PullUInt8(origin));
				target = (uint)((long)target + heading);
				origin = (uint)((long)origin + heading);
				total--;
			}

			this.processor.Journal.CloseScope("MoveFarData");
		}

		/// <summary>
		/// Frees the memory block (a picture from 0xb000 up). True: freed (AX 0); false: no such block (AX 9, the DOS error
		/// of an invalid block)
		/// </summary>
		public bool DosReleaseMemory(ushort section)
		{
			this.processor.Journal.OpenScope($"DosReleaseMemory(0x{section:x4})");

			bool released;

			if (section >= 0xb000)
			{
				// this is a graphics bitmap
				if (!this.owner.VideoCard.Pictures.IncludesKey(section))
					throw new Exception($"The bitmap 0x{section:x4} is not allocated");

				this.owner.VideoCard.Pictures.DiscardByKey(section);
				released = true;
			}
			else
			{
				released = this.processor.Storage.ReleaseStorageChunk(section);
			}

			this.processor.RegAcc.Value16 = (ushort)(released ? 0 : 9);
			this.processor.Journal.CloseScope("DosReleaseMemory");

			return released;
		}
		#endregion

		#region File operations
		public static string ObtainDOSFileLabel(string route)
		{
			return DosPaths.ObtainDOSFileLabel(route);
		}

		public short OpenStream(ushort docnameAddr, ushort styleAddr)
		{
			return OpenStream(this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr)),
				this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, styleAddr)));
		}

		public short OpenStream(string docname, ushort styleAddr)
		{
			return OpenStream(docname,
				this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, styleAddr)));
		}

		public short OpenStream(ushort docnameAddr, string style)
		{
			return OpenStream(this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr)), style);
		}

		public short OpenStream(string docname, string style)
		{
			FileMode variant = FileMode.Open;
			FileAccess rights = FileAccess.Write;
			FileStreamTypeEnum kind = FileStreamTypeEnum.Binary;

			style = style.Trim().ToLower();
			for (int idx = 0; idx < style.Length; idx++)
			{
				switch (style[idx])
				{
					case 'r':
						variant = FileMode.Open;
						rights = FileAccess.Read;
						break;
					case 'w':
						variant = FileMode.Create;
						rights = FileAccess.Write;
						break;
					case 'a':
						variant = FileMode.Append;
						rights = FileAccess.Write;
						break;
					case '+':
						rights = FileAccess.ReadWrite;
						break;
					case 't':
						kind = FileStreamTypeEnum.Text;
						break;
					case 'b':
						kind = FileStreamTypeEnum.Binary;
						break;
					default:
						throw new Exception($"Unknown file mode '{style}'");
				}
			}

			short grip = -1;
			string trail = Path.Combine(this.processor.StandardFolder, ObtainDOSFileLabel(docname.ToUpper()));

			try
			{
				this.files.Add(this.nextHandle, new FileStreamItem(new FileStream($"{trail}", variant, rights), kind));
				grip = this.nextHandle;
				this.nextHandle++;
			}
			catch
			{
				grip = -1;
			}

			this.processor.RegAcc.Value16 = (ushort)grip; // preserve compatibility
			return grip;
		}

		public short CloseStream(short hnd)
		{
			return this.CloseHandle(hnd);
		}

		public ushort ReadStream(ushort addr, ushort elementExtent, ushort elementTallyAlt, short hnd)
		{
			ushort elementTally = 0;
			uint spot = CPU.AsLinearAddress(this.processor.SegData.Value16, addr);

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
				byte[] cache = new byte[elementExtent];

				for (int idx = 0; idx < elementTallyAlt; idx++)
				{
					int spanInt = 0;

					if (documentElement.Kind == FileStreamTypeEnum.Binary)
					{
						short putbackChar = documentElement.PutbackChar;
						if (putbackChar != -1)
						{
							cache[0] = (byte)(putbackChar & 0xff);
							if (elementExtent - 1 > 0)
							{
								spanInt = documentElement.Flow.Read(cache, 1, elementExtent - 1);
								if (spanInt != elementExtent - 1)
									break;
							}
						}
						else
						{
							spanInt = documentElement.Flow.Read(cache, 0, elementExtent);
							if (spanInt != elementExtent)
								break;
						}

						this.processor.Storage.EmitChunk(spot, cache, 0, elementExtent);
						spot += elementExtent;
					}
					else
					{
						spanInt = 0;
						for (int jdx = 0; jdx < elementExtent; jdx++)
						{
							short chr = documentElement.PullChar();
							if (chr < 0)
								break;
							cache[jdx] = (byte)chr;
							spanInt++;
						}

						if (spanInt != elementExtent)
							break;

						this.processor.Storage.EmitChunk(spot, cache, 0, elementExtent);
						spot += elementExtent;
					}
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = elementTally; // preserve compatibility
			return elementTally;
		}

		public short ScanStream(short hnd, ushort layoutAddr, ushort globAddr)
		{
			return ScanStream(hnd, this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, layoutAddr)), globAddr);
		}

		/// <summary>
		/// fscanf(hnd, "%[^\n]\n", line) into a C# string: the line without '\r' and its tally (1); -1 and the empty text at the
		/// end of the file or for an empty line, like the emulated fscanf
		/// </summary>
		public short ScanStreamLine(short hnd, out string line)
		{
			short tallyShort = -1;
			line = "";

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
				StringBuilder answer = new StringBuilder();
				short chr;

				while ((chr = documentElement.PullChar()) != -1 && chr != (short)'\n')
				{
					if (chr != (short)'\r')
					{
						answer.Append((char)chr);
					}
				}

				if (chr != -1 && answer.Length > 0)
				{
					line = answer.ToString();
					tallyShort = 1;
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			return tallyShort;
		}

		/// <summary>fread of one little-endian word: the word read, or the word given when the file ends before it</summary>
		public ushort ReadStreamWord(short hnd, ushort word)
		{
			if (!this.files.IncludesKey(hnd))
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
				return word;
			}

			FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
			byte[] cache = new byte[2];

			if (documentElement.Kind == FileStreamTypeEnum.Binary)
			{
				short putbackChar = documentElement.PutbackChar;

				if (putbackChar != -1)
				{
					cache[0] = (byte)(putbackChar & 0xff);

					if (documentElement.Flow.Read(cache, 1, 1) != 1)
						return word;
				}
				else if (documentElement.Flow.Read(cache, 0, 2) != 2)
				{
					return word;
				}
			}
			else
			{
				for (int jdx = 0; jdx < 2; jdx++)
				{
					short chr = documentElement.PullChar();

					if (chr < 0)
						return word;

					cache[jdx] = (byte)chr;
				}
			}

			return (ushort)(cache[0] | (cache[1] << 8));
		}

		/// <summary>fwrite of one little-endian word to a binary file (the text file translation of WriteStream is not needed)</summary>
		public void WriteStreamWord(ushort word, short hnd)
		{
			if (!this.files.IncludesKey(hnd))
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
				return;
			}

			FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);

			if (documentElement.Kind != FileStreamTypeEnum.Binary)
				throw new InvalidOperationException("WriteStreamWord writes binary files only");

			documentElement.Flow.Write([(byte)word, (byte)(word >> 8)], 0, 2);
		}

		public short ScanStream(short hnd, string layout, ushort globAddr)
		{
			short tallyShort = -1;
			uint globLocation = CPU.AsLinearAddress(this.processor.SegData.Value16, globAddr);

			if (this.files.IncludesKey(hnd))
			{
				tallyShort = 0;
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
				StringBuilder answer = new StringBuilder();

				switch (layout.ToLower())
				{
					case "%[^\n]\n":
						short chr;
						while ((chr = documentElement.PullChar()) != -1 && chr != (short)'\n')
						{
							if (chr != (short)'\r')
							{
								answer.Append((char)chr);
							}
						}
						if (chr != -1 && answer.Length > 0)
						{
							this.processor.EmitText(globLocation, answer.ToString(), answer.Length);
							tallyShort = 1;
						}
						else
						{
							this.processor.EmitText(globLocation, "", 1);
							tallyShort = -1;
						}
						break;

					default:
						throw new Exception($"fscanf has undefined format '{layout}'");
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = (ushort)tallyShort; // preserve compatibility
			return tallyShort;
		}

		public ushort WriteStream(ushort addr, ushort elementExtent, ushort elementTallyAlt, short hnd)
		{
			ushort elementTally = 0;
			uint spot = CPU.AsLinearAddress(this.processor.SegData.Value16, addr);

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
				byte[] cache = new byte[elementExtent];

				if (documentElement.Kind == FileStreamTypeEnum.Binary)
				{
					for (int idx = 0; idx < elementTallyAlt; idx++)
					{
						for (int jdx = 0; jdx < elementExtent; jdx++)
						{
							cache[jdx] = this.processor.Storage.PullUInt8(spot);
							spot++;
						}

						documentElement.Flow.Write(cache, 0, elementExtent);
						elementTally++;
					}
				}
				else
				{
					bool line = false;

					for (int idx = 0; idx < elementTallyAlt; idx++)
					{
						for (int jdx = 0; jdx < elementExtent; jdx++)
						{
							cache[jdx] = this.processor.Storage.PullUInt8(spot);
							if (cache[jdx] == (byte)'\n')
							{
								if (!line)
								{
									cache[jdx] = (byte)'\r';
									line = true;
								}
								else
								{
									spot++;
								}
							}
							else
							{
								spot++;
							}
						}

						documentElement.Flow.Write(cache, 0, elementExtent);
						elementTally++;
					}
					if (line)
					{
						documentElement.Flow.WriteByte((byte)'\n');
					}
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = elementTally; // preserve compatibility
			return elementTally;
		}

		public short SeekStream(short hnd, int displacement, short originOther)
		{
			short backAmount = -1;

			if (this.files.IncludesKey(hnd))
			{
				if (originOther >= 0 && originOther < 3)
				{
					FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
					short scratch = documentElement.PutbackChar;
					SeekOrigin anchor = SeekOrigin.Begin;

					switch (originOther)
					{
						case 0:
							anchor = SeekOrigin.Begin;
							break;
						case 1:
							anchor = SeekOrigin.Current;
							break;
						case 2:
							anchor = SeekOrigin.End;
							break;
					}

					documentElement.Flow.Seek(displacement, anchor);
					backAmount = 0;
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = (ushort)backAmount; // preserve compatibility
			return backAmount;
		}

		public int TellStream(short hnd)
		{
			int placeInt = -1;

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
				short scratch = documentElement.PutbackChar;
				placeInt = (int)documentElement.Flow.Position;
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, (uint)placeInt); // preserve compatibility
			return placeInt;
		}

		public int SeekHandle(short hnd, int displacement, short originOther)
		{
			int backAmountInt = -1;

			if (this.files.IncludesKey(hnd))
			{
				if (originOther >= 0 && originOther < 3)
				{
					FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
					short scratch = documentElement.PutbackChar;
					SeekOrigin anchor = SeekOrigin.Begin;

					switch (originOther)
					{
						case 0:
							anchor = SeekOrigin.Begin;
							break;
						case 1:
							anchor = SeekOrigin.Current;
							break;
						case 2:
							anchor = SeekOrigin.End;
							break;
					}

					documentElement.Flow.Seek(displacement, anchor);
					backAmountInt = displacement;
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, (uint)backAmountInt); // preserve compatibility
			return backAmountInt;
		}

		public short OpenHandle(ushort docnameAddr, ushort permission)
		{
			return OpenHandle(docnameAddr, permission, 0);
		}

		public short OpenHandle(ushort docnameAddr, ushort permission, ushort style)
		{
			return OpenHandle(this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr)), permission, style);
		}

		public short OpenHandle(string docname, ushort permission, ushort style)
		{
			FileMode variant = FileMode.Open;
			FileAccess rights = FileAccess.Read;
			FileStreamTypeEnum kind = FileStreamTypeEnum.Binary;

			if ((permission & 0x1) == 0x1)
			{
				// open for writing only
				rights = FileAccess.Write;
			}
			else if ((permission & 0x2) == 0x2)
			{
				// open for reading and writing
				rights = FileAccess.ReadWrite;
			}

			if ((permission & 0x8) == 0x8)
			{
				// append file
				variant = FileMode.Append;
			}
			else if ((permission & 0x100) == 0x100)
			{
				// create and open file
				variant = FileMode.OpenOrCreate;
			}
			else if ((permission & 0x200) == 0x200)
			{
				// open and truncate
				variant = FileMode.Truncate;
			}
			else if ((permission & 0x400) == 0x400)
			{
				// open only if file doesn't already exist
				variant = FileMode.Open;
			}

			if ((permission & 0x4000) == 0x4000)
			{
				// file mode is text (translated)
				kind = FileStreamTypeEnum.Text;
			}
			else if ((permission & 0x8000) == 0x8000)
			{
				// file mode is binary (untranslated)
				kind = FileStreamTypeEnum.Binary;
			}

			short grip = -1;
			string trail = Path.Combine(this.processor.StandardFolder, ObtainDOSFileLabel(docname.ToUpper()));

			try
			{
				this.files.Add(this.nextHandle, new FileStreamItem(new FileStream($"{trail}", variant, rights), kind));
				grip = this.nextHandle;
				this.nextHandle++;
			}
			catch
			{
				grip = -1;
			}

			this.processor.RegAcc.Value16 = (ushort)grip; // preserve compatibility
			return grip;
		}

		public short CloseHandle(short hnd)
		{
			short scratch = 0;

			if (this.files.IncludesKey(hnd))
			{
				this.files.ObtainValueByKey(hnd).Flow.Close();
				this.files.DiscardByKey(hnd);
			}
			else
			{
				this.processor.Journal.EmitLine($"Trying to close unknown handle {hnd}");
				scratch = -1;
			}

			this.processor.RegAcc.Value16 = (ushort)scratch; // preserve compatibility
			return scratch;
		}

		public string ReadText(short hnd, int span)
		{
			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);
				char[] chars = new char[span];
				int count = 0;
				for (int idx = 0; idx < span; idx++)
				{
					short chr = documentElement.PullChar();
					if (chr < 0)
						break;
					chars[count++] = (char)(byte)chr;
					if (chr == 0)
						break;
				}
				string text = new string(chars, 0, count);
				int zero = text.IndexOf('\0');
				return zero >= 0 ? text.Substring(0, zero) : text;
			}
			return string.Empty;
		}

		public short ReadHandle(short hnd, ushort stagingAddr, ushort span)
		{
			uint location = CPU.AsLinearAddress(this.processor.SegData.Value16, stagingAddr);
			short entryTotal = -1;

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);

				for (int idx = 0; idx < span; idx++)
				{
					short chr = documentElement.PullChar();
					if (chr >= 0)
					{
						this.processor.Storage.EmitUInt8(location, (byte)chr);
						location++;
						entryTotal++;
					}
					else
					{
						break;
					}
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = (ushort)entryTotal; // preserve compatibility
			return entryTotal;
		}

		public short WriteHandle(short hnd, ushort stagingAddr, ushort span)
		{
			uint location = CPU.AsLinearAddress(this.processor.SegData.Value16, stagingAddr);
			short entryTotal = -1;

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);

				if (documentElement.Kind == FileStreamTypeEnum.Binary)
				{
					for (int idx = 0; idx < span; idx++)
					{
						documentElement.Flow.WriteByte(this.processor.Storage.PullUInt8(location));
						location++;
						entryTotal++;
					}
				}
				else
				{
					for (int idx = 0; idx < span; idx++)
					{
						byte chr = this.processor.Storage.PullUInt8(location);
						if (chr == (byte)'\n')
						{
							documentElement.Flow.WriteByte((byte)'\r');
						}
						documentElement.Flow.WriteByte(chr);
						location++;
						entryTotal++;
					}
				}
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = (ushort)entryTotal; // preserve compatibility
			return entryTotal;
		}

		public short DosOpenHandle(ushort docnameAddr, ushort markers, ushort hndAddr)
		{
			uint hndLocation = CPU.AsLinearAddress(this.processor.SegData.Value16, hndAddr);
			short backAmount = -1;

			string title = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, docnameAddr));
			FileMode variant = FileMode.Open;
			FileAccess rights = FileAccess.Read;
			FileStreamTypeEnum kind = FileStreamTypeEnum.Binary;

			if ((markers & 0x1) == 0x1)
			{
				// open for writing only
				rights = FileAccess.Write;
			}
			if ((markers & 0x2) == 0x2)
			{
				// open for reading and writing
				rights = FileAccess.ReadWrite;
			}
			if ((markers & 0x8) == 0x8)
			{
				// append file
				variant = FileMode.Append;
			}
			if ((markers & 0x100) == 0x100)
			{
				// create and open file
				variant = FileMode.OpenOrCreate;
			}
			if ((markers & 0x200) == 0x200)
			{
				// open and truncate
				variant = FileMode.Truncate;
			}
			if ((markers & 0x400) == 0x400)
			{
				// open only if file doesn't already exist
				variant = FileMode.Open;
			}

			string trail = Path.Combine(this.processor.StandardFolder, ObtainDOSFileLabel(title.ToUpper()));
			if (File.Exists(trail))
			{
				this.files.Add(this.nextHandle, new FileStreamItem(new FileStream(trail, variant, rights), kind));
				short grip = this.nextHandle;
				this.nextHandle++;
				this.processor.Storage.EmitUInt16(CPU.AsLinearAddress(this.processor.SegData.Value16, hndAddr), (ushort)grip);
				backAmount = 0;
			}

			this.processor.RegAcc.Value16 = (ushort)backAmount; // preserve compatibility
			return backAmount;
		}

		public ushort DosCloseHandle(short hnd)
		{
			ushort backAmountShort = (ushort)this.CloseHandle(hnd);

			this.processor.RegAcc.Value16 = backAmountShort; // preserve compatibility
			return backAmountShort;
		}

		public ushort DosReadHandle(short hnd, ushort stagingAddr, ushort span, ushort nbytesreadAddr)
		{
			uint stagingLocation = CPU.AsLinearAddress(this.processor.SegData.Value16, stagingAddr);
			ushort backAmountShort = 1;
			ushort elementTally = 0;

			if (this.files.IncludesKey(hnd))
			{
				FileStreamItem documentElement = this.files.ObtainValueByKey(hnd);

				for (int idx = 0; idx < span; idx++)
				{
					short chr = documentElement.PullChar();
					if (chr >= 0)
					{
						this.processor.Storage.EmitUInt8(stagingLocation, (byte)chr);
						stagingLocation++;
						elementTally++;
					}
					else
					{
						break;
					}
				}

				this.processor.Storage.EmitUInt16(CPU.AsLinearAddress(this.processor.SegData.Value16, nbytesreadAddr), elementTally);
				backAmountShort = 0;
			}
			else
			{
				this.processor.Journal.EmitLine($"Can't find file handle {hnd}");
			}

			this.processor.RegAcc.Value16 = backAmountShort; // preserve compatibility
			return backAmountShort;
		}

		/// <summary>
		/// The BIOS disk call: 0 resets the disks, 4 checks the drive of the request is ready. True: done (AH 0); false: not
		/// ready (AH 0x80)
		/// </summary>
		public bool BiosDiskCall(ushort directive, int volume)
		{
			this.processor.Journal.OpenScope("'_bios_disk'(Cdecl) at 0x3045:0x3062");

			// function body
			DriveInfo[] diskDetails = DriveInfo.GetDrives();
			char chrDisk = (char)((volume >= 0x80) ? (volume - 0x80 + 'C') : ('A' + volume));

			switch (directive)
			{
				case 0:
					// reset disk system
					this.processor.RegAcc.HighByte = 0;
					this.processor.Journal.CloseScope("'_bios_disk'");
					return true;

				case 4:
					// Verify disk sectors
					for (int idx = 0; idx < diskDetails.Length; idx++)
					{
						if (diskDetails[idx].Name[0] == chrDisk)
						{
							if (diskDetails[idx].IsReady)
							{
								this.processor.RegAcc.HighByte = 0;
								this.processor.Journal.CloseScope("'_bios_disk'");
								return true;
							}
							else
							{
								break;
							}
						}
					}
					break;
			}

			this.processor.RegAcc.HighByte = 0x80;
			this.processor.Journal.CloseScope("'_bios_disk'");

			return false;
		}

		public bool BiosDiskCall(ushort directive, ushort driveaDetailsAddr)
		{
			this.processor.Journal.OpenScope("'_bios_disk'(Cdecl) at 0x3045:0x3062");

			// function body
			DriveInfo[] diskDetails = DriveInfo.GetDrives();
			uint spot = CPU.AsLinearAddress(this.processor.SegData.Value16, driveaDetailsAddr);
			int volume = this.processor.Storage.PullUInt16(spot);
			char chrDisk = (char)((volume >= 0x80) ? (volume - 0x80 + 'C') : ('A' + volume));

			switch (directive)
			{
				case 0:
					// reset disk system
					this.processor.RegAcc.HighByte = 0;
					this.processor.Journal.CloseScope("'_bios_disk'");
					return true;

				case 4:
					// Verify disk sectors

					for (int idx = 0; idx < diskDetails.Length; idx++)
					{
						if (diskDetails[idx].Name[0] == chrDisk)
						{
							if (diskDetails[idx].IsReady)
							{
								this.processor.RegAcc.HighByte = 0;
								this.processor.Journal.CloseScope("'_bios_disk'");
								return true;
							}
							else
							{
								break;
							}
						}
					}
					break;
			}

			this.processor.RegAcc.HighByte = 0x80;
			this.processor.Journal.CloseScope("'_bios_disk'");

			return false;
		}

		public void DosQueryDrive(ushort amountAddr)
		{
			// function body
			this.processor.EmitUInt16(this.processor.SegData.Value16, amountAddr, 3);
		}
		#endregion

		#region String operations
		/// <summary>
		/// Join two strings together
		/// </summary>
		/// <param name="destinationPtr">The destination string ptr</param>
		/// <param name="sourcePtr">The source string ptr</param>
		/// <returns></returns>
		public ushort AppendText(ushort targetAddr, ushort originAddr)
		{
			string goal = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, targetAddr));
			string provider = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, originAddr));

			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, targetAddr), goal + provider, goal.Length + provider.Length);

			this.processor.RegAcc.Value16 = targetAddr; // preserve compatibility
			return targetAddr;
		}

		public ushort AppendText(ushort targetAddr, string originChars)
		{
			string goal = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, targetAddr));

			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, targetAddr), goal + originChars, 
				goal.Length + originChars.Length);

			this.processor.RegAcc.Value16 = targetAddr; // preserve compatibility
			return targetAddr;
		}

		public ushort CopyText(ushort targetAddr, ushort originAddr)
		{
			string provider = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, originAddr));

			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, targetAddr), provider, provider.Length);

			this.processor.RegAcc.Value16 = targetAddr; // preserve compatibility
			return targetAddr;
		}

		public ushort CopyText(ushort targetAddr, string originAlt)
		{
			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, targetAddr), originAlt, originAlt.Length);

			this.processor.RegAcc.Value16 = targetAddr; // preserve compatibility
			return targetAddr;
		}

		public ushort TextLength(ushort charsAddr)
		{
			string provider = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr));

			this.processor.RegAcc.Value16 = (ushort)provider.Length; // preserve compatibility
			return (ushort)provider.Length;
		}

		public short CompareTextNoCase(ushort chars1Addr, ushort chars2Addr)
		{
			string txt1 = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, chars1Addr));
			string txt2 = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, chars2Addr));

			return CompareTextNoCase(txt1, txt2);
		}

		/// <summary>stricmp of two texts</summary>
		public short CompareTextNoCase(string txt1, string txt2)
		{
			short backAmount = (short)string.Compare(txt1, txt2, StringComparison.CurrentCultureIgnoreCase);

			this.processor.RegAcc.Value16 = (ushort)backAmount; // preserve compatibility
			return backAmount;
		}

		public short CompareTextPrefixNoCase(ushort chars1Addr, ushort chars2Addr, ushort peakSpan)
		{
			string txt1 = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, chars1Addr));
			string txt2 = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, chars2Addr));

			if (txt1.Length > peakSpan)
				txt1 = txt1.Substring(0, peakSpan);

			if (txt2.Length > peakSpan)
				txt2 = txt2.Substring(0, peakSpan);

			short backAmount = (short)string.Compare(txt1, txt2, StringComparison.CurrentCultureIgnoreCase);

			this.processor.RegAcc.Value16 = (ushort)backAmount; // preserve compatibility
			return backAmount;
		}

		public ushort UppercaseText(ushort charsAddr)
		{
			string scratch = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr)).ToUpper();

			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr), scratch, scratch.Length);

			this.processor.RegAcc.Value16 = charsAddr; // preserve compatibility
			return charsAddr;
		}

		public int FindSubtext(ushort chars1Addr, ushort chars2Addr)
		{
			string txt1 = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, chars1Addr));
			string txt2 = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, chars2Addr));

			short backAmount = (short)txt1.IndexOf(txt2);

			if (backAmount >= 0) // preserve compatibility
			{
				this.processor.RegAcc.Value16 = (ushort)(chars1Addr + backAmount);
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
			}

			return backAmount;
		}

		public ushort IntegerToText(int amount, ushort charsAddr, short baseAlt)
		{
			string amountShort = Convert.ToString(amount, baseAlt);

			this.processor.EmitText(CPU.AsLinearAddress(this.processor.SegData.Value16, charsAddr), amountShort, amountShort.Length);

			this.processor.RegAcc.Value16 = charsAddr; // preserve compatibility
			return charsAddr;
		}

		public string IntegerToText(int amount, short baseAlt)
		{
			string amountShort = Convert.ToString(amount, baseAlt);

			return amountShort;
		}
		#endregion

		#region Time operations
		public void CurrentTime()
		{
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, 
				(uint)CurrentTime(CPU.AsLinearAddress(this.processor.SegData.Value16, this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegStack.Value16 + 0x4)))));
		}

		/// <summary>
		/// The clock of time(): the wall clock of the computer; a test sets a fixed moment, so that the random seed taken from
		/// it (SeedRandomizer) is the same in both games it compares
		/// </summary>
		public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

		public int CurrentTime(uint momentAddr)
		{
			int grandSecs = (int)Math.Floor((this.Clock() - (new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds);

			if (momentAddr != 0)
				this.processor.Storage.EmitUInt32(momentAddr, (uint)grandSecs);

			return grandSecs;
		}
		#endregion

		#region Math operations
		public short AbsoluteValue(short amount)
		{
			short retv = Math.Abs(amount);
			this.processor.RegAcc.Value16 = (ushort)retv; // for compatibility

			return retv;
		}
		#endregion

		#region Random number generator operations
		public void SeedRandom()
		{
			SeedRandom(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegStack.Value16 + 0x4)));
		}

		public void SeedRandom(uint amount)
		{
			this.dice = new RandomMT19937(amount);
		}

		public short NextRandom()
		{
			this.processor.RegAcc.Value16 = (ushort)(this.dice.DrawUnsigned() & 0x7fff);
			return (short)this.processor.RegAcc.Value16;
		}

		public RandomMT19937 Dice
		{
			get { return this.dice; }
		}
		#endregion
	}
}
