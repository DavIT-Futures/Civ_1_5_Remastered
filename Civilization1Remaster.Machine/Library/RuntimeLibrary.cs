using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Civilization1Remaster.Machine.Library
{
	public class RuntimeLibrary : IRuntimeLibrary
	{
		private readonly IMachineHost host;

		private readonly Dictionary<short, FileStreamItem> files = new Dictionary<short, FileStreamItem>();

		private RandomGenerator dice = new RandomGenerator();
		private short nextHandle = 0x20;

		public RuntimeLibrary(CentralProcessor processor, IMachineHost host)
		{
			this.Processor = processor;
			this.host = host;
		}

		public Func<DateTime> Clock { get; set; } = () => DateTime.Now;

		public RandomGenerator Dice
		{
			get { return this.dice; }
		}

		private CentralProcessor Processor { get; }

		public static string ObtainOperatingSystemFileLabel(string route)
		{
			return OperatingSystemPaths.ObtainOperatingSystemFileLabel(route);
		}

		public void TerminateProgram(short cipher)
		{
			this.Processor.Journal.EmitLine($"exit({cipher})");
			this.Processor.Quit(cipher);
		}

		public short KeyAvailable()
		{
			this.Processor.PumpMessages();

			this.Processor.RegisterAccumulator.Word = (ushort)((this.host.InputTokens.Count > 0) ? 0xffff : 0);

			return (short)this.Processor.RegisterAccumulator.Word;
		}

		public short ReadKey()
		{
			while (this.host.InputTokens.Count == 0)
			{
				if (this.host.InputTokens.Count > 0)
				{
					break;
				}

				Thread.Sleep(10);
				this.Processor.PumpMessages();
			}

			lock (this.host.InputLatch)
			{
				this.Processor.RegisterAccumulator.Word = (ushort)this.host.InputTokens.Dequeue();
			}

			return (short)this.Processor.RegisterAccumulator.Word;
		}

		public ushort CopyBytes(ushort endpoint, ushort originAlternate, ushort tally)
		{
			using var _ = this.Processor.Journal.Scope($"CopyBytes(0x{endpoint:x4}, 0x{originAlternate:x4}, {tally})");

			int heading = 1;
			uint target = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, endpoint);
			uint origin = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, originAlternate);
			int total = tally;

			if (target > origin && (origin + total) >= target)
			{
				origin = (uint)(origin + total - 1);
				target = (uint)(target + total - 1);
				heading = -1;
			}

			while (total > 0)
			{
				this.Processor.Storage.EmitByte(target, this.Processor.Storage.PullByte(origin));
				target = (uint)((long)target + heading);
				origin = (uint)((long)origin + heading);
				total--;
			}

			this.Processor.RegisterAccumulator.Word = endpoint;
			return endpoint;
		}

		public ushort FillBytes(ushort endpoint, byte amount, ushort tally)
		{
			using var _ = this.Processor.Journal.Scope($"FillBytes(0x{endpoint:x4}, 0x{amount:x2}, {tally})");

			uint target = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, endpoint);
			int total = tally;

			while (total > 0)
			{
				this.Processor.Storage.EmitByte(target, amount);
				target++;
				total--;
			}

			this.Processor.RegisterAccumulator.Word = endpoint;
			return endpoint;
		}

		public void MoveFarData(ushort originSection, ushort originDisplacement, ushort targetSection, ushort targetDisplacement, ushort tally)
		{
			using var _ = this.Processor.Journal.Scope($"MoveFarData(0x{originSection:x4}, 0x{originDisplacement:x4}, 0x{targetSection:x4}, 0x{targetDisplacement:x4}, {tally})");

			int heading = 1;
			uint target = CentralProcessor.AsLinearAddress(targetSection, targetDisplacement);
			uint origin = CentralProcessor.AsLinearAddress(originSection, originDisplacement);
			int total = tally;

			if (target > origin && (origin + total) >= target)
			{
				origin = (uint)(origin + total - 1);
				target = (uint)(target + total - 1);
				heading = -1;
			}

			while (total > 0)
			{
				this.Processor.Storage.EmitByte(target, this.Processor.Storage.PullByte(origin));
				target = (uint)((long)target + heading);
				origin = (uint)((long)origin + heading);
				total--;
			}
		}

		public bool OperatingSystemReleaseMemory(ushort section)
		{
			using var _ = this.Processor.Journal.Scope($"DosReleaseMemory(0x{section:x4})");

			bool released;

			if (section >= 0xb000)
			{
				if (!this.host.ReleasePicture(section))
				{
					throw new Exception($"The bitmap 0x{section:x4} is not allocated");
				}

				released = true;
			}
			else
			{
				released = this.Processor.Storage.ReleaseStorageChunk(section);
			}

			this.Processor.RegisterAccumulator.Word = (ushort)(released ? 0 : 9);

			return released;
		}

		public short OpenStream(ushort documentNameAddress, ushort styleAddress)
		{
			return OpenStream(this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress)),
				this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, styleAddress)));
		}

		public short OpenStream(string documentName, ushort styleAddress)
		{
			return OpenStream(documentName,
				this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, styleAddress)));
		}

		public short OpenStream(ushort documentNameAddress, string style)
		{
			return OpenStream(this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress)), style);
		}

		public short OpenStream(string documentName, string style)
		{
			FileMode variant = FileMode.Open;
			FileAccess rights = FileAccess.Write;
			FileStreamType kind = FileStreamType.Binary;

			style = style.Trim().ToLower();
			for (int index = 0; index < style.Length; index++)
			{
				switch (style[index])
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
						kind = FileStreamType.Text;
						break;
					case 'b':
						kind = FileStreamType.Binary;
						break;
					default:
						throw new Exception($"Unknown file mode '{style}'");
				}
			}

			string trail = Path.Combine(this.Processor.StandardFolder, ObtainOperatingSystemFileLabel(documentName.ToUpper()));

			short grip;
			try
			{
				this.files[this.nextHandle] = new FileStreamItem(new FileStream($"{trail}", variant, rights), kind);
				grip = this.nextHandle;
				this.nextHandle++;
			}
			catch
			{
				grip = -1;
			}

			this.Processor.RegisterAccumulator.Word = (ushort)grip;
			return grip;
		}

		public short CloseStream(short handle)
		{
			return this.CloseHandle(handle);
		}

		public ushort ReadStream(ushort address, ushort elementExtent, ushort elementTallyAlternate, short handle)
		{
			ushort elementTally = 0;
			uint spot = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, address);

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];
				byte[] cache = new byte[elementExtent];

				for (int index = 0; index < elementTallyAlternate; index++)
				{
					int spanValue;
					if (documentElement.Kind == FileStreamType.Binary)
					{
						short putbackCharacter = documentElement.PutbackCharacter;
						if (putbackCharacter != -1)
						{
							cache[0] = (byte)(putbackCharacter & 0xff);
							if (elementExtent - 1 > 0)
							{
								spanValue = documentElement.Flow.Read(cache, 1, elementExtent - 1);
								if (spanValue != elementExtent - 1)
								{
									break;
								}
							}
						}
						else
						{
							spanValue = documentElement.Flow.Read(cache, 0, elementExtent);
							if (spanValue != elementExtent)
							{
								break;
							}
						}

						this.Processor.Storage.EmitChunk(spot, cache, 0, elementExtent);
						spot += elementExtent;
					}
					else
					{
						spanValue = 0;
						for (int innerIndex = 0; innerIndex < elementExtent; innerIndex++)
						{
							short character = documentElement.PullCharacter();
							if (character < 0)
							{
								break;
							}

							cache[innerIndex] = (byte)character;
							spanValue++;
						}

						if (spanValue != elementExtent)
						{
							break;
						}

						this.Processor.Storage.EmitChunk(spot, cache, 0, elementExtent);
						spot += elementExtent;
					}
				}
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = elementTally;
			return elementTally;
		}

		public short ScanStream(short handle, ushort layoutAddress, ushort globalAddress)
		{
			return ScanStream(handle, this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, layoutAddress)), globalAddress);
		}

		public short ScanStreamLine(short handle, out string line)
		{
			short tallyShort = -1;
			line = string.Empty;

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];
				StringBuilder answer = new StringBuilder();
				short character;

				while ((character = documentElement.PullCharacter()) != -1 && character != (short)'\n')
				{
					if (character != (short)'\r')
					{
						answer.Append((char)character);
					}
				}

				if (character != -1 && answer.Length > 0)
				{
					line = answer.ToString();
					tallyShort = 1;
				}
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			return tallyShort;
		}

		public ushort ReadStreamWord(short handle, ushort word)
		{
			if (!this.files.ContainsKey(handle))
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
				return word;
			}

			FileStreamItem documentElement = this.files[handle];
			byte[] cache = new byte[2];

			if (documentElement.Kind == FileStreamType.Binary)
			{
				short putbackCharacter = documentElement.PutbackCharacter;

				if (putbackCharacter != -1)
				{
					cache[0] = (byte)(putbackCharacter & 0xff);

					if (documentElement.Flow.Read(cache, 1, 1) != 1)
					{
						return word;
					}
				}
				else if (documentElement.Flow.Read(cache, 0, 2) != 2)
				{
					return word;
				}
			}
			else
			{
				for (int innerIndex = 0; innerIndex < 2; innerIndex++)
				{
					short character = documentElement.PullCharacter();

					if (character < 0)
					{
						return word;
					}

					cache[innerIndex] = (byte)character;
				}
			}

			return (ushort)(cache[0] | (cache[1] << 8));
		}

		public void WriteStreamWord(ushort word, short handle)
		{
			if (!this.files.ContainsKey(handle))
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
				return;
			}

			FileStreamItem documentElement = this.files[handle];

			if (documentElement.Kind != FileStreamType.Binary)
			{
				throw new InvalidOperationException("WriteStreamWord writes binary files only");
			}

			documentElement.Flow.Write([(byte)word, (byte)(word >> 8)], 0, 2);
		}

		public short ScanStream(short handle, string layout, ushort globalAddress)
		{
			short tallyShort = -1;
			uint globalLocation = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, globalAddress);

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];
				StringBuilder answer = new StringBuilder();

				switch (layout.ToLower())
				{
					case "%[^\n]\n":
						short character;
						while ((character = documentElement.PullCharacter()) != -1 && character != (short)'\n')
						{
							if (character != (short)'\r')
							{
								answer.Append((char)character);
							}
						}

						if (character != -1 && answer.Length > 0)
						{
							this.Processor.EmitText(globalLocation, answer.ToString(), answer.Length);
							tallyShort = 1;
						}
						else
						{
							this.Processor.EmitText(globalLocation, string.Empty, 1);
							tallyShort = -1;
						}

						break;

					default:
						throw new Exception($"fscanf has undefined format '{layout}'");
				}
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = (ushort)tallyShort;
			return tallyShort;
		}

		public ushort WriteStream(ushort address, ushort elementExtent, ushort elementTallyAlternate, short handle)
		{
			ushort elementTally = 0;
			uint spot = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, address);

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];
				byte[] cache = new byte[elementExtent];

				if (documentElement.Kind == FileStreamType.Binary)
				{
					for (int index = 0; index < elementTallyAlternate; index++)
					{
						for (int innerIndex = 0; innerIndex < elementExtent; innerIndex++)
						{
							cache[innerIndex] = this.Processor.Storage.PullByte(spot);
							spot++;
						}

						documentElement.Flow.Write(cache, 0, elementExtent);
						elementTally++;
					}
				}
				else
				{
					bool line = false;

					for (int index = 0; index < elementTallyAlternate; index++)
					{
						for (int innerIndex = 0; innerIndex < elementExtent; innerIndex++)
						{
							cache[innerIndex] = this.Processor.Storage.PullByte(spot);
							if (cache[innerIndex] == (byte)'\n')
							{
								if (!line)
								{
									cache[innerIndex] = (byte)'\r';
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
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = elementTally;
			return elementTally;
		}

		public short SeekStream(short handle, int displacement, short originOther)
		{
			short backAmount = -1;

			if (this.files.ContainsKey(handle))
			{
				if (originOther >= 0 && originOther < 3)
				{
					FileStreamItem documentElement = this.files[handle];
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
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = (ushort)backAmount;
			return backAmount;
		}

		public int TellStream(short handle)
		{
			int placeValue = -1;

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];
				placeValue = (int)documentElement.Flow.Position;
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.SplitDoubleWord(this.Processor.RegisterAccumulator, this.Processor.RegisterData, (uint)placeValue);
			return placeValue;
		}

		public int SeekHandle(short handle, int displacement, short originOther)
		{
			int backAmount = -1;

			if (this.files.ContainsKey(handle))
			{
				if (originOther >= 0 && originOther < 3)
				{
					FileStreamItem documentElement = this.files[handle];
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
					backAmount = displacement;
				}
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.SplitDoubleWord(this.Processor.RegisterAccumulator, this.Processor.RegisterData, (uint)backAmount);
			return backAmount;
		}

		public short OpenHandle(ushort documentNameAddress, ushort permission)
		{
			return OpenHandle(documentNameAddress, permission, 0);
		}

		public short OpenHandle(ushort documentNameAddress, ushort permission, ushort style)
		{
			return OpenHandle(this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress)), permission, style);
		}

		public short OpenHandle(string documentName, ushort permission, ushort style)
		{
			FileMode variant = FileMode.Open;
			FileAccess rights = FileAccess.Read;
			FileStreamType kind = FileStreamType.Binary;

			if ((permission & 0x1) == 0x1)
			{
				rights = FileAccess.Write;
			}
			else if ((permission & 0x2) == 0x2)
			{
				rights = FileAccess.ReadWrite;
			}

			if ((permission & 0x8) == 0x8)
			{
				variant = FileMode.Append;
			}
			else if ((permission & 0x100) == 0x100)
			{
				variant = FileMode.OpenOrCreate;
			}
			else if ((permission & 0x200) == 0x200)
			{
				variant = FileMode.Truncate;
			}
			else if ((permission & 0x400) == 0x400)
			{
				variant = FileMode.Open;
			}

			if ((permission & 0x4000) == 0x4000)
			{
				kind = FileStreamType.Text;
			}
			else if ((permission & 0x8000) == 0x8000)
			{
				kind = FileStreamType.Binary;
			}

			string trail = Path.Combine(this.Processor.StandardFolder, ObtainOperatingSystemFileLabel(documentName.ToUpper()));

			short grip;
			try
			{
				this.files[this.nextHandle] = new FileStreamItem(new FileStream($"{trail}", variant, rights), kind);
				grip = this.nextHandle;
				this.nextHandle++;
			}
			catch
			{
				grip = -1;
			}

			this.Processor.RegisterAccumulator.Word = (ushort)grip;
			return grip;
		}

		public short CloseHandle(short handle)
		{
			short scratch = 0;

			if (this.files.ContainsKey(handle))
			{
				this.files[handle].Flow.Close();
				this.files.Remove(handle);
			}
			else
			{
				this.Processor.Journal.EmitLine($"Trying to close unknown handle {handle}");
				scratch = -1;
			}

			this.Processor.RegisterAccumulator.Word = (ushort)scratch;
			return scratch;
		}

		public string ReadText(short handle, int span)
		{
			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];
				char[] characters = new char[span];
				int count = 0;
				for (int index = 0; index < span; index++)
				{
					short character = documentElement.PullCharacter();
					if (character < 0)
					{
						break;
					}

					characters[count++] = (char)(byte)character;
					if (character == 0)
					{
						break;
					}
				}

				string text = new string(characters, 0, count);
				int zero = text.IndexOf('\0');
				return zero >= 0 ? text.Substring(0, zero) : text;
			}

			return string.Empty;
		}

		public short ReadHandle(short handle, ushort stagingAddress, ushort span)
		{
			uint location = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, stagingAddress);
			short entryTotal = -1;

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];

				for (int index = 0; index < span; index++)
				{
					short character = documentElement.PullCharacter();
					if (character >= 0)
					{
						this.Processor.Storage.EmitByte(location, (byte)character);
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
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = (ushort)entryTotal;
			return entryTotal;
		}

		public short WriteHandle(short handle, ushort stagingAddress, ushort span)
		{
			uint location = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, stagingAddress);
			short entryTotal = -1;

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];

				if (documentElement.Kind == FileStreamType.Binary)
				{
					for (int index = 0; index < span; index++)
					{
						documentElement.Flow.WriteByte(this.Processor.Storage.PullByte(location));
						location++;
						entryTotal++;
					}
				}
				else
				{
					for (int index = 0; index < span; index++)
					{
						byte character = this.Processor.Storage.PullByte(location);
						if (character == (byte)'\n')
						{
							documentElement.Flow.WriteByte((byte)'\r');
						}

						documentElement.Flow.WriteByte(character);
						location++;
						entryTotal++;
					}
				}
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = (ushort)entryTotal;
			return entryTotal;
		}

		public short OperatingSystemOpenHandle(ushort documentNameAddress, ushort markers, ushort handleAddress)
		{
			short backAmount = -1;

			string title = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, documentNameAddress));
			FileMode variant = FileMode.Open;
			FileAccess rights = FileAccess.Read;
			FileStreamType kind = FileStreamType.Binary;

			if ((markers & 0x1) == 0x1)
			{
				rights = FileAccess.Write;
			}

			if ((markers & 0x2) == 0x2)
			{
				rights = FileAccess.ReadWrite;
			}

			if ((markers & 0x8) == 0x8)
			{
				variant = FileMode.Append;
			}

			if ((markers & 0x100) == 0x100)
			{
				variant = FileMode.OpenOrCreate;
			}

			if ((markers & 0x200) == 0x200)
			{
				variant = FileMode.Truncate;
			}

			if ((markers & 0x400) == 0x400)
			{
				variant = FileMode.Open;
			}

			string trail = Path.Combine(this.Processor.StandardFolder, ObtainOperatingSystemFileLabel(title.ToUpper()));
			if (File.Exists(trail))
			{
				this.files[this.nextHandle] = new FileStreamItem(new FileStream(trail, variant, rights), kind);
				short grip = this.nextHandle;
				this.nextHandle++;
				this.Processor.Storage.EmitWord(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, handleAddress), (ushort)grip);
				backAmount = 0;
			}

			this.Processor.RegisterAccumulator.Word = (ushort)backAmount;
			return backAmount;
		}

		public ushort OperatingSystemCloseHandle(short handle)
		{
			ushort backAmountShort = (ushort)this.CloseHandle(handle);

			this.Processor.RegisterAccumulator.Word = backAmountShort;
			return backAmountShort;
		}

		public ushort OperatingSystemReadHandle(short handle, ushort stagingAddress, ushort span, ushort bytesReadAddress)
		{
			uint stagingLocation = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, stagingAddress);
			ushort backAmountShort = 1;
			ushort elementTally = 0;

			if (this.files.ContainsKey(handle))
			{
				FileStreamItem documentElement = this.files[handle];

				for (int index = 0; index < span; index++)
				{
					short character = documentElement.PullCharacter();
					if (character >= 0)
					{
						this.Processor.Storage.EmitByte(stagingLocation, (byte)character);
						stagingLocation++;
						elementTally++;
					}
					else
					{
						break;
					}
				}

				this.Processor.Storage.EmitWord(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, bytesReadAddress), elementTally);
				backAmountShort = 0;
			}
			else
			{
				this.Processor.Journal.EmitLine($"Can't find file handle {handle}");
			}

			this.Processor.RegisterAccumulator.Word = backAmountShort;
			return backAmountShort;
		}

		public bool FirmwareDiskCall(ushort directive, int volume)
		{
			using var _ = this.Processor.Journal.Scope("'_bios_disk'(Cdecl) at 0x3045:0x3062");

			DriveInfo[] diskDetails = DriveInfo.GetDrives();
			char characterDisk = (char)((volume >= 0x80) ? (volume - 0x80 + 'C') : ('A' + volume));

			switch (directive)
			{
				case 0:
					this.Processor.RegisterAccumulator.HighByte = 0;
					return true;

				case 4:
					for (int index = 0; index < diskDetails.Length; index++)
					{
						if (diskDetails[index].Name[0] == characterDisk)
						{
							if (diskDetails[index].IsReady)
							{
								this.Processor.RegisterAccumulator.HighByte = 0;
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

			this.Processor.RegisterAccumulator.HighByte = 0x80;

			return false;
		}

		public bool FirmwareDiskCall(ushort directive, ushort driveADetailsAddress)
		{
			using var _ = this.Processor.Journal.Scope("'_bios_disk'(Cdecl) at 0x3045:0x3062");

			DriveInfo[] diskDetails = DriveInfo.GetDrives();
			uint spot = CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, driveADetailsAddress);
			int volume = this.Processor.Storage.PullWord(spot);
			char characterDisk = (char)((volume >= 0x80) ? (volume - 0x80 + 'C') : ('A' + volume));

			switch (directive)
			{
				case 0:
					this.Processor.RegisterAccumulator.HighByte = 0;
					return true;

				case 4:

					for (int index = 0; index < diskDetails.Length; index++)
					{
						if (diskDetails[index].Name[0] == characterDisk)
						{
							if (diskDetails[index].IsReady)
							{
								this.Processor.RegisterAccumulator.HighByte = 0;
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

			this.Processor.RegisterAccumulator.HighByte = 0x80;

			return false;
		}

		public void OperatingSystemQueryDrive(ushort amountAddress)
		{
			this.Processor.EmitWord(this.Processor.SegmentData.Word, amountAddress, 3);
		}

		public ushort AppendText(ushort targetAddress, ushort originAddress)
		{
			string goal = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, targetAddress));
			string provider = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, originAddress));

			this.Processor.EmitText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, targetAddress), goal + provider, goal.Length + provider.Length);

			this.Processor.RegisterAccumulator.Word = targetAddress;
			return targetAddress;
		}

		public ushort AppendText(ushort targetAddress, string originCharacters)
		{
			string goal = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, targetAddress));

			this.Processor.EmitText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, targetAddress), goal + originCharacters,
				goal.Length + originCharacters.Length);

			this.Processor.RegisterAccumulator.Word = targetAddress;
			return targetAddress;
		}

		public ushort CopyText(ushort targetAddress, ushort originAddress)
		{
			string provider = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, originAddress));

			this.Processor.EmitText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, targetAddress), provider, provider.Length);

			this.Processor.RegisterAccumulator.Word = targetAddress;
			return targetAddress;
		}

		public ushort CopyText(ushort targetAddress, string originAlternate)
		{
			this.Processor.EmitText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, targetAddress), originAlternate, originAlternate.Length);

			this.Processor.RegisterAccumulator.Word = targetAddress;
			return targetAddress;
		}

		public ushort TextLength(ushort charactersAddress)
		{
			string provider = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersAddress));

			this.Processor.RegisterAccumulator.Word = (ushort)provider.Length;
			return (ushort)provider.Length;
		}

		public short CompareTextNoCase(ushort charactersFirstAddress, ushort charactersSecondAddress)
		{
			string firstText = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersFirstAddress));
			string secondText = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersSecondAddress));

			return CompareTextNoCase(firstText, secondText);
		}

		public short CompareTextNoCase(string firstText, string secondText)
		{
			short backAmount = (short)string.Compare(firstText, secondText, StringComparison.CurrentCultureIgnoreCase);

			this.Processor.RegisterAccumulator.Word = (ushort)backAmount;
			return backAmount;
		}

		public short CompareTextPrefixNoCase(ushort charactersFirstAddress, ushort charactersSecondAddress, ushort peakSpan)
		{
			string firstText = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersFirstAddress));
			string secondText = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersSecondAddress));

			if (firstText.Length > peakSpan)
			{
				firstText = firstText.Substring(0, peakSpan);
			}

			if (secondText.Length > peakSpan)
			{
				secondText = secondText.Substring(0, peakSpan);
			}

			short backAmount = (short)string.Compare(firstText, secondText, StringComparison.CurrentCultureIgnoreCase);

			this.Processor.RegisterAccumulator.Word = (ushort)backAmount;
			return backAmount;
		}

		public ushort UppercaseText(ushort charactersAddress)
		{
			string scratch = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersAddress)).ToUpper();

			this.Processor.EmitText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersAddress), scratch, scratch.Length);

			this.Processor.RegisterAccumulator.Word = charactersAddress;
			return charactersAddress;
		}

		public int FindSubtext(ushort charactersFirstAddress, ushort charactersSecondAddress)
		{
			string firstText = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersFirstAddress));
			string secondText = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersSecondAddress));

			short backAmount = (short)firstText.IndexOf(secondText);

			if (backAmount >= 0)
			{
				this.Processor.RegisterAccumulator.Word = (ushort)(charactersFirstAddress + backAmount);
			}
			else
			{
				this.Processor.RegisterAccumulator.Word = 0;
			}

			return backAmount;
		}

		public ushort IntegerToText(int amount, ushort charactersAddress, short baseAlternate)
		{
			string amountShort = Convert.ToString(amount, baseAlternate);

			this.Processor.EmitText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, charactersAddress), amountShort, amountShort.Length);

			this.Processor.RegisterAccumulator.Word = charactersAddress;
			return charactersAddress;
		}

		public string IntegerToText(int amount, short baseAlternate)
		{
			string amountShort = Convert.ToString(amount, baseAlternate);

			return amountShort;
		}

		public void CurrentTime()
		{
			this.Processor.SplitDoubleWord(this.Processor.RegisterAccumulator, this.Processor.RegisterData,
				(uint)CurrentTime(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, this.Processor.PullWord(this.Processor.SegmentStack.Word, (ushort)(this.Processor.RegisterStack.Word + 0x4)))));
		}

		public int CurrentTime(uint momentAddress)
		{
			int grandSeconds = (int)Math.Floor((this.Clock() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);

			if (momentAddress != 0)
			{
				this.Processor.Storage.EmitDoubleWord(momentAddress, (uint)grandSeconds);
			}

			return grandSeconds;
		}

		public short AbsoluteValue(short amount)
		{
			short result = Math.Abs(amount);
			this.Processor.RegisterAccumulator.Word = (ushort)result;

			return result;
		}

		public void SeedRandom()
		{
			SeedRandom(this.Processor.PullWord(this.Processor.SegmentStack.Word, (ushort)(this.Processor.RegisterStack.Word + 0x4)));
		}

		public void SeedRandom(uint amount)
		{
			this.dice = new RandomGenerator(amount);
		}

		public short NextRandom()
		{
			this.Processor.RegisterAccumulator.Word = (ushort)(this.dice.DrawUnsigned() & 0x7fff);
			return (short)this.Processor.RegisterAccumulator.Word;
		}
	}
}
