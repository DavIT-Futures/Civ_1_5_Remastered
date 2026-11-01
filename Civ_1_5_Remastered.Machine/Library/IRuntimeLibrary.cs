using System;

namespace Civ_1_5_Remastered.Machine.Library
{
	public interface IRuntimeLibrary
	{
		Func<DateTime> Clock { get; set; }

		RandomGenerator Dice { get; }

		void TerminateProgram(short cipher);

		short KeyAvailable();

		short ReadKey();

		ushort CopyBytes(ushort endpoint, ushort originAlternate, ushort tally);

		ushort FillBytes(ushort endpoint, byte amount, ushort tally);

		void MoveFarData(ushort originSection, ushort originDisplacement, ushort targetSection, ushort targetDisplacement, ushort tally);

		bool OperatingSystemReleaseMemory(ushort section);

		short OpenStream(ushort documentNameAddress, ushort styleAddress);

		short OpenStream(string documentName, ushort styleAddress);

		short OpenStream(ushort documentNameAddress, string style);

		short OpenStream(string documentName, string style);

		short CloseStream(short handle);

		ushort ReadStream(ushort address, ushort elementExtent, ushort elementTallyAlternate, short handle);

		short ScanStream(short handle, ushort layoutAddress, ushort globalAddress);

		short ScanStreamLine(short handle, out string line);

		ushort ReadStreamWord(short handle, ushort word);

		void WriteStreamWord(ushort word, short handle);

		short ScanStream(short handle, string layout, ushort globalAddress);

		ushort WriteStream(ushort address, ushort elementExtent, ushort elementTallyAlternate, short handle);

		short SeekStream(short handle, int displacement, short originOther);

		int TellStream(short handle);

		int SeekHandle(short handle, int displacement, short originOther);

		short OpenHandle(ushort documentNameAddress, ushort permission);

		short OpenHandle(ushort documentNameAddress, ushort permission, ushort style);

		short OpenHandle(string documentName, ushort permission, ushort style);

		short CloseHandle(short handle);

		string ReadText(short handle, int span);

		short ReadHandle(short handle, ushort stagingAddress, ushort span);

		short WriteHandle(short handle, ushort stagingAddress, ushort span);

		short OperatingSystemOpenHandle(ushort documentNameAddress, ushort markers, ushort handleAddress);

		ushort OperatingSystemCloseHandle(short handle);

		ushort OperatingSystemReadHandle(short handle, ushort stagingAddress, ushort span, ushort bytesReadAddress);

		bool FirmwareDiskCall(ushort directive, int volume);

		bool FirmwareDiskCall(ushort directive, ushort driveADetailsAddress);

		void OperatingSystemQueryDrive(ushort amountAddress);

		ushort AppendText(ushort targetAddress, ushort originAddress);

		ushort AppendText(ushort targetAddress, string originCharacters);

		ushort CopyText(ushort targetAddress, ushort originAddress);

		ushort CopyText(ushort targetAddress, string originAlternate);

		ushort TextLength(ushort charactersAddress);

		short CompareTextNoCase(ushort charactersFirstAddress, ushort charactersSecondAddress);

		short CompareTextNoCase(string firstText, string secondText);

		short CompareTextPrefixNoCase(ushort charactersFirstAddress, ushort charactersSecondAddress, ushort peakSpan);

		ushort UppercaseText(ushort charactersAddress);

		int FindSubtext(ushort charactersFirstAddress, ushort charactersSecondAddress);

		ushort IntegerToText(int amount, ushort charactersAddress, short baseAlternate);

		string IntegerToText(int amount, short baseAlternate);

		void CurrentTime();

		int CurrentTime(uint momentAddress);

		short AbsoluteValue(short amount);

		void SeedRandom();

		void SeedRandom(uint amount);

		short NextRandom();
	}
}
