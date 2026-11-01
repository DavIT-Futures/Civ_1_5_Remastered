using System;

namespace Civilization1Remaster.Machine
{
	/// <summary>The operations of RuntimeLibrary that the other parts of the game use</summary>
	public interface IRuntimeLibrary
	{
		/// <summary>
		/// The clock of time(): the wall clock of the computer; a test sets a fixed moment, so that the random seed taken from
		/// it (SeedRandomizer) is the same in both games it compares
		/// </summary>
		Func<DateTime> Clock { get; set; }

		RandomMT19937 Dice { get; }

		void TerminateProgram(short cipher);

		short KeyAvailable();

		short ReadKey();

		ushort CopyBytes(ushort endpoint, ushort originAlt, ushort tally);

		ushort FillBytes(ushort endpoint, byte amount, ushort tally);

		void MoveFarData(ushort originSection, ushort originDisplacement, ushort targetSection, ushort targetDisplacement, ushort tally);

		/// <summary>
		/// Frees the memory block (a picture from 0xb000 up). True: freed (AX 0); false: no such block (AX 9, the DOS error
		/// of an invalid block)
		/// </summary>
		bool DosReleaseMemory(ushort section);

		short OpenStream(ushort docnameAddr, ushort styleAddr);

		short OpenStream(string docname, ushort styleAddr);

		short OpenStream(ushort docnameAddr, string style);

		short OpenStream(string docname, string style);

		short CloseStream(short hnd);

		ushort ReadStream(ushort addr, ushort elementExtent, ushort elementTallyAlt, short hnd);

		short ScanStream(short hnd, ushort layoutAddr, ushort globAddr);

		/// <summary>
		/// fscanf(hnd, "%[^\n]\n", line) into a C# string: the line without '\r' and its tally (1); -1 and the empty text at the
		/// end of the file or for an empty line, like the emulated fscanf
		/// </summary>
		short ScanStreamLine(short hnd, out string line);

		/// <summary>fread of one little-endian word: the word read, or the word given when the file ends before it</summary>
		ushort ReadStreamWord(short hnd, ushort word);

		/// <summary>fwrite of one little-endian word to a binary file (the text file translation of WriteStream is not needed)</summary>
		void WriteStreamWord(ushort word, short hnd);

		short ScanStream(short hnd, string layout, ushort globAddr);

		ushort WriteStream(ushort addr, ushort elementExtent, ushort elementTallyAlt, short hnd);

		short SeekStream(short hnd, int displacement, short originOther);

		int TellStream(short hnd);

		int SeekHandle(short hnd, int displacement, short originOther);

		short OpenHandle(ushort docnameAddr, ushort permission);

		short OpenHandle(ushort docnameAddr, ushort permission, ushort style);

		short OpenHandle(string docname, ushort permission, ushort style);

		short CloseHandle(short hnd);

		string ReadText(short hnd, int span);

		short ReadHandle(short hnd, ushort stagingAddr, ushort span);

		short WriteHandle(short hnd, ushort stagingAddr, ushort span);

		short DosOpenHandle(ushort docnameAddr, ushort markers, ushort hndAddr);

		ushort DosCloseHandle(short hnd);

		ushort DosReadHandle(short hnd, ushort stagingAddr, ushort span, ushort nbytesreadAddr);

		/// <summary>
		/// The BIOS disk call: 0 resets the disks, 4 checks the drive of the request is ready. True: done (AH 0); false: not
		/// ready (AH 0x80)
		/// </summary>
		bool BiosDiskCall(ushort directive, int volume);

		bool BiosDiskCall(ushort directive, ushort driveaDetailsAddr);

		void DosQueryDrive(ushort amountAddr);

		/// <summary>
		/// Join two strings together
		/// </summary>
		/// <param name="destinationPtr">The destination string ptr</param>
		/// <param name="sourcePtr">The source string ptr</param>
		/// <returns></returns>
		ushort AppendText(ushort targetAddr, ushort originAddr);

		ushort AppendText(ushort targetAddr, string originChars);

		ushort CopyText(ushort targetAddr, ushort originAddr);

		ushort CopyText(ushort targetAddr, string originAlt);

		ushort TextLength(ushort charsAddr);

		short CompareTextNoCase(ushort chars1Addr, ushort chars2Addr);

		/// <summary>stricmp of two texts</summary>
		short CompareTextNoCase(string txt1, string txt2);

		short CompareTextPrefixNoCase(ushort chars1Addr, ushort chars2Addr, ushort peakSpan);

		ushort UppercaseText(ushort charsAddr);

		int FindSubtext(ushort chars1Addr, ushort chars2Addr);

		ushort IntegerToText(int amount, ushort charsAddr, short baseAlt);

		string IntegerToText(int amount, short baseAlt);

		void CurrentTime();

		int CurrentTime(uint momentAddr);

		short AbsoluteValue(short amount);

		void SeedRandom();

		void SeedRandom(uint amount);

		short NextRandom();
	}
}
