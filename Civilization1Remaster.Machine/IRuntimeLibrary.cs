using System;

namespace Civilization1Remaster.Machine
{
	public interface IRuntimeLibrary
	{
		Func<DateTime> Clock { get; set; }

		RandomMT19937 Dice { get; }

		void TerminateProgram(short cipher);

		short KeyAvailable();

		short ReadKey();

		ushort CopyBytes(ushort endpoint, ushort originAlt, ushort tally);

		ushort FillBytes(ushort endpoint, byte amount, ushort tally);

		void MoveFarData(ushort originSection, ushort originDisplacement, ushort targetSection, ushort targetDisplacement, ushort tally);

		bool DosReleaseMemory(ushort section);

		short OpenStream(ushort docnameAddr, ushort styleAddr);

		short OpenStream(string docname, ushort styleAddr);

		short OpenStream(ushort docnameAddr, string style);

		short OpenStream(string docname, string style);

		short CloseStream(short hnd);

		ushort ReadStream(ushort addr, ushort elementExtent, ushort elementTallyAlt, short hnd);

		short ScanStream(short hnd, ushort layoutAddr, ushort globAddr);

		short ScanStreamLine(short hnd, out string line);

		ushort ReadStreamWord(short hnd, ushort word);

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

		bool BiosDiskCall(ushort directive, int volume);

		bool BiosDiskCall(ushort directive, ushort driveaDetailsAddr);

		void DosQueryDrive(ushort amountAddr);

		ushort AppendText(ushort targetAddr, ushort originAddr);

		ushort AppendText(ushort targetAddr, string originChars);

		ushort CopyText(ushort targetAddr, ushort originAddr);

		ushort CopyText(ushort targetAddr, string originAlt);

		ushort TextLength(ushort charsAddr);

		short CompareTextNoCase(ushort chars1Addr, ushort chars2Addr);

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
