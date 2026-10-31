using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Civilization1Remaster.Machine
{
	public partial class CPU
	{
		public class DosFcb
		{
			private readonly bool enlarged;
			private readonly uint pnt;
			private readonly CPUMemory storage;
			private readonly byte[] held = new byte[4];  // 4

			// struct sFCB
			private byte disk;         // Drive number 0=default, 1=A, etc.

			private string docname;        // 8 Space padded name
			private string suffix;         // 3 Space padded extension
			private ushort nowChunk;       // Current Block
			private ushort recordExtent;        // Logical record size
			private uint docsize;      // File Size
			private ushort day;
			private ushort moment;

			// Reserved Block should be 8 bytes
			private byte filetableRecords;

			private byte divideTraits;
			private byte additionalDetails;
			private byte documentHnd;

													// end
			private byte nowRecord;           // Current record in current block

			private uint diceAlt;          // Current relative record number

			// -7   byte	if FF this is an extended FCB 0
			// -6  5bytes	reserved  0
			// -1   byte	file attribute if extended FCB 0
			// 00   byte	drive number (0 for default drive, 1=A:, 2=B:, ...)
			// 01  8bytes	filename, left justified with trailing blanks
			// 09  3bytes	filename extension, left justified w/blanks
			// 0C   word	current block number relative to beginning of the file, starting with zero
			// 0E   word	logical record size in bytes
			// 10   dword	file size in bytes
			// 14   word	date the file was created or last updated
			// |F|E|D|C|B|A|9|8|7|6|5|4|3|2|1|0| 15,14 (Intel reverse order)
			// | | | | | | | | | | | `--------- day 1-31
			// | | | | | | | `---------------- month 1-12
			// `----------------------------- year + 1980

			// 16   word	time of last write
			// |F|E|D|C|B|A|9|8|7|6|5|4|3|2|1|0| 17,16 (Intel reverse order)
			// | | | | | | | | | | | `---------- secs in 2 second increments
			// | | | | | `--------------------- minutes (0-59)
			// `------------------------------ hours (0-23)

			// 18  8bytes	see below for version specific information  Ø
			// 1A   dword	address of device header if character device  Ø
			// 20   byte	current relative record number within current BLOCK
			// 21   dword	relative record number relative to the beginning of
			// 		the file, starting with zero; high bit omitted if
			// 		record length is 64 bytes

			public DosFcb(CPUMemory ram, ushort sect, ushort away) :
				this(ram, sect, away, true)
			{ }

			public DosFcb(CPUMemory ram, ushort sect, ushort away, bool permitEnlarged)
			{
				this.storage = ram;
				this.pnt = CPU.AsLinearAddress(sect, away);
				this.enlarged = false;
				this.docname = "";
				this.suffix = "";

				PullFCBFromStorage();

				if (permitEnlarged)
				{
					if (this.disk == 0xff)
					{
						this.pnt += 7;
						this.enlarged = true;
						PullFCBFromStorage();
					}
				}
			}

			//public void Create(bool _extended);
			//public void SetSizeDateTime(uint _size, ushort _date, ushort _time);
			//public void GetSizeDateTime(ref uint _size, ref ushort _date, ref ushort _time);
			public void AssignLabel(byte disk, ref string namepart, ref string suffix)
			{
				this.disk = disk;
				storage.EmitUInt8(pnt, this.disk);
				this.docname = namepart;
				EmitText(pnt + 1, 8, this.docname);
				this.suffix = suffix;
				EmitText(pnt + 9, 3, this.suffix);
			}

			public string ObtainLabel()
			{
				StringBuilder label = new StringBuilder();
				label.Append(this.docname);
				label.Append('.');
				label.Append(this.suffix);

				return label.ToString();
			}

			public void AcquireFile(byte filehnd)
			{
				this.disk = (byte)(ObtainDrive() + 1);
				storage.EmitUInt8(pnt, this.disk);
				this.documentHnd = filehnd;
				storage.EmitUInt8(pnt + 0x1b, this.documentHnd);
				this.nowChunk = 0;
				storage.EmitUInt16(pnt + 0xc, this.nowChunk);
				this.recordExtent = 128;
				storage.EmitUInt16(pnt + 0xe, this.recordExtent);
				//	this.rndm = 0; // breaks Jewels of darkness. 
				FileInfo details = new FileInfo(this.ObtainLabel());
				this.docsize = (uint)(details.Length & 0xffffffffu);
				storage.EmitUInt16(pnt + 0x10, (ushort)(this.docsize & 0xffff));
				storage.EmitUInt16(pnt + 0x12, (ushort)((this.docsize & 0xffff0000) >> 16));
				this.day = AsDosDate(details.LastWriteTime);
				storage.EmitUInt16(pnt + 0x14, this.day);
				this.moment = AsDosTime(details.LastWriteTime);
				storage.EmitUInt16(pnt + 0x16, this.moment);
			}

			public void ReleaseFile(out byte filehnd)
			{
				filehnd = this.documentHnd;
				this.documentHnd = 0xff;
				storage.EmitUInt8(pnt + 0x1b, this.documentHnd);

			}

			public void ObtainRecord(out ushort nowChunk, out byte nowRecord)
			{
				nowChunk = this.nowChunk;
				nowRecord = this.nowRecord;
			}

			public void AssignRecord(ushort nowChunk, byte nowRecord)
			{
				this.nowChunk = nowChunk;
				storage.EmitUInt16(pnt + 0xc, this.nowChunk);
				this.nowRecord = nowRecord;
				storage.EmitUInt8(pnt + 0x20, this.nowRecord);
			}

			public void ObtainSeqContent(out byte filehnd, out ushort recordExtent)
			{
				filehnd = this.documentHnd;
				recordExtent = this.recordExtent;
			}

			public void ObtainRandom(out uint chance)
			{
				chance = this.diceAlt;
			}

			public void AssignRandom(uint chance)
			{
				this.diceAlt = chance;
				storage.EmitUInt16(pnt + 0x21, (ushort)(this.diceAlt & 0xffff));
				storage.EmitUInt16(pnt + 0x23, (ushort)((this.diceAlt & 0xffff0000) >> 16));
			}

			public byte ObtainDrive()
			{
				if (this.disk == 0)
				{
					byte drive = 2;
					string? top = Path.GetPathRoot(Assembly.GetExecutingAssembly().Location);

					if (top != null && top.Length == 3 && top[1] == ':' && top[2] == Path.PathSeparator)
					{
						drive = (byte)((byte)char.ToUpper(top[0]) - (byte)'A');
					}

					return drive;
				}
				else
				{
					return (byte)(this.disk - 1);
				}
			}

			public bool IsExtendedBlock()
			{
				return this.enlarged;
			}

			public void ObtainAttr(ref byte trait)
			{
				if (this.enlarged)
				{
					trait = storage.PullUInt8(pnt - 1);
				}
			}

			public void AssignAttr(byte trait)
			{
				if (this.enlarged)
				{
					storage.EmitUInt8(pnt - 1, trait);
				}
			}

			public bool IsConsistent()
			{
				//Very simple check for Oubliette
				if (this.docname[0] == 0 && this.documentHnd == 0)
					return false;

				return true;
			}

			private void PullFCBFromStorage()
			{
				this.disk = storage.PullUInt8(pnt);
				this.docname = PullText(pnt + 1, 8);
				this.suffix = PullText(pnt + 9, 3);
				this.nowChunk = storage.PullUInt16(pnt + 0xc);
				this.recordExtent = storage.PullUInt16(pnt + 0xe);
				this.docsize = (uint)storage.PullUInt16(pnt + 0x10) |
					((uint)storage.PullUInt16(pnt + 0x12) << 16);
				this.day = storage.PullUInt16(pnt + 0x14);
				this.moment = storage.PullUInt16(pnt + 0x16);
				this.filetableRecords = storage.PullUInt8(pnt + 0x18);
				this.divideTraits = storage.PullUInt8(pnt + 0x19);
				this.additionalDetails = storage.PullUInt8(pnt + 0x1a);
				this.documentHnd = storage.PullUInt8(pnt + 0x1b);
				this.held[0] = storage.PullUInt8(pnt + 0x1c);
				this.held[1] = storage.PullUInt8(pnt + 0x1d);
				this.held[2] = storage.PullUInt8(pnt + 0x1e);
				this.held[3] = storage.PullUInt8(pnt + 0x1f);
				this.nowRecord = storage.PullUInt8(pnt + 0x20);
				this.diceAlt = (uint)storage.PullUInt16(pnt + 0x21) |
						((uint)storage.PullUInt16(pnt + 0x23) << 16);
			}

			private string PullText(uint location, int extent)
			{
				StringBuilder words = new StringBuilder();

				for (int idx = 0; idx < extent; idx++)
				{
					byte chr = storage.PullUInt8((uint)(location + idx));
					if (chr == 0x20 || chr == 0)
						break;
					words.Append((char)chr);
				}

				return words.ToString();
			}

			private void EmitText(uint location, int extent, string words)
			{
				for (int idx = 0; idx < extent; idx++)
				{
					byte chr;
					if (idx >= words.Length)
						chr = 0x20;
					else
						chr = (byte)words[idx];
					storage.EmitUInt8((uint)(location + idx), chr);
				}
			}

			// 16   word	time of last write
			// |F|E|D|C|B|A|9|8|7|6|5|4|3|2|1|0| 17,16 (Intel reverse order)
			// | | | | | | | | | | | `---------- secs in 2 second increments
			// | | | | | `--------------------- minutes (0-59)
			// `------------------------------ hours (0-23)
			private ushort AsDosTime(DateTime moment)
			{
				ushort amountAlt = 0;
				amountAlt |= (ushort)((moment.Second / 2) & 0x1f);
				amountAlt |= (ushort)(((moment.Minute) & 0x3f) << 5);
				amountAlt |= (ushort)(((moment.Hour) & 0x1f) << 11);

				return amountAlt;
			}

			// |F|E|D|C|B|A|9|8|7|6|5|4|3|2|1|0| 15,14 (Intel reverse order)
			// | | | | | | | | | | | `--------- day 1-31
			// | | | | | | | `---------------- month 1-12
			// `----------------------------- year + 1980
			private ushort AsDosDate(DateTime moment)
			{
				ushort amountAlt = 0;
				amountAlt |= (ushort)((moment.Day) & 0x1f);
				amountAlt |= (ushort)(((moment.Minute) & 0xf) << 5);
				amountAlt |= (ushort)(((moment.Year - 1980) & 0x7f) << 9);

				return amountAlt;
			}
		}
	}
}
