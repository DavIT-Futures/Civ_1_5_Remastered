using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of MeetWithKing that the other parts of the game use</summary>
	public interface IMeetWithKing
	{
		/// <summary>
		/// An audience of the human player with the leader of a computer player (occasion 0: the player asked for it, the
		/// computer player came otherwise, 0xffff from a diplomat). The leader weighs the strength on the continents, nuclear
		/// weapons, wonders and enemies, then may exchange technologies, ask for an alliance against a common enemy, demand
		/// tribute or a technology, threaten, offer peace or a cease-fire, or ask the human player to declare war on someone.
		/// The locals stay in the frame (functions called later from the same depth may read what is left there).
		/// </summary>
		void LaunchAudience(short participantIdx, int horizLoc, int vertLoc, ushort occasion);

		/// <summary>
		/// The end of an audience: the pictures of the leader released, the main screen repainted, no audience any more
		/// </summary>
		void FinishAudience();

		/// <summary>
		/// The audience with the player's emissary, once a turn (0xd804): unless the answer 0x3934 is known, shows the map at
		/// the position (horizLoc -1: not moved) and asks whether to receive the emissary (with "Check Intelligence" when the human player
		/// has relations bit 0x40 towards the player, which shows the report and asks again); 0x3934 is minus the choice. Unless
		/// refused (-1), shows the king and his greeting in the player's font (0x36fa).
		/// </summary>
		/// <returns>0x3934, also in AX</returns>
		ushort WelcomeEmissary(short participantIdx, int horizLoc, int vertLoc);

		/// <summary>
		/// The greeting of the king at TextBuffer.Address: the opening (0x39b1 with relations bit 8 towards the human player, else 0x39e8),
		/// one of 4 random titles with the government (0x19b2) and the leader's name (0x19a2), "of the" civilization (0x1992),
		/// and the nuclear threat when the player has nuclear units (type 25)
		/// </summary>
		void ObtainKingSalutation(short participantIdx);

		/// <summary>
		/// The message of the leader of the player to the human player (text 0x3a8c, with the names of both civilizations from
		/// the table at 0x1982 put into the text parameters 0x30be and 0x30b8)
		/// </summary>
		void DisplayKingMessage(short participantIdx);

		/// <summary>
		/// After an audience: the player and the human have met (diplomacy flag 2), the player waits 16 turns before the next
		/// contact. At the easiest level the units of the player whose nearest city is one of the human's, at most 2 away, are
		/// removed.
		/// </summary>
		void LogAudience(short participantIdx);

		/// <summary>
		/// One frame of the king's animation. While he speaks (0xd76c counts down) and stands still (step 0x67f4 = 0), the mouth
		/// moves: eyes 5 or -1, mouth 0, 9 or (every 4th frame) 8, then a pause of 4 frames (0x67f6). Otherwise the step goes on
		/// along table 0x3ab8 (positive: next step, -1, -2, -3: back to step 0, 6, 14); at the end of a gesture (0) the mood
		/// 0x3936 picks the next one (0: step 3, the eyes colour 0x3a9a; 1: step 0; 2: step 12, the colours 0x3ab4 / 0x3ab7)
		/// and 0x7ef4 says which pose he holds (0, 1 at step 14, 2 at step 6); without a pause one time in 12 a still pose
		/// starts a short gesture. The eyes and the mouth come from the tables 0x3a94 and 0x3aa6.
		/// </summary>
		void ChoreographKingSpeaking();

		/// <summary>
		/// Opens the audience with the player's king (0x67f2): without the pictures (disk 2 missing and no preference bit 8) only
		/// a line "&lt;title&gt;: &lt;government&gt; of the &lt;civilization&gt;." over a motif (0x67f0 = 1). Otherwise the advisors of
		/// govtNa.pic (N the government / 2, 3 for government 3, "m" with technology 0x14, 0x67ee) into 0x804e, the king of
		/// kingNN.pic (NN = 0x3938 + ChiefVisuals, 0x67f8; kink on EGA for kings 0 and 3) into 0xb1e6, the throne room
		/// (ImportKingScenery), the 14 faces and mouths into 0x7efe / 0x7f00 and, when disk 2 answers, the 12 small advisors
		/// (0x67fa = 1). The file name is in the stack frame, like in the original.
		/// </summary>
		void DisplayKingScreen(short participantIdx);

		/// <summary>
		/// Unless 0x67f0 is set: shows the throne room of the player, the background "backNa.pic" (N is the government / 2, 3 for
		/// government 3, "m" instead of "a" when 0x67ee is set) with (Standing + 1) / 2 figures (columns at 0x3afe, pictures at
		/// 0x804e) and the king (0xb1e6), with the colours of "kingNN.pal" (NN is 0x67f8) and of the background. The file names are
		/// made in buffers in the stack frame, like in the original.
		/// </summary>
		void ImportKingScenery(short participantIdx);

		/// <summary>
		/// Ends the meeting music and frees the picture of the king (0x7efe), when one is loaded (0x67ea)
		/// </summary>
		void ReleaseKingResources();

		/// <summary>
		/// The king's picture (unless 0x67f0 is set): with the advisors' pose advisorPose (not -1, not the one shown, 0x67e8, and with
		/// 0x67fa) the advisors, one for every two ranks of the standing, by the style of the government (6 for the monarchy,
		/// +3 with 0x67ee); then the face (eyes) and the mouth (mouth) of the king, placed by the table of the king (0x67f8) in the
		/// segment 0x36fa.
		/// </summary>
		void ChoreographKingLikeness(ushort eyes, ushort mouth, ushort advisorPose);

		/// <summary>
		/// A dialog over the bottom part of the king screen (rows 128 - 199, kept in the back screen 0x19d4 and put back);
		/// column 20 is moved to 36
		/// </summary>
		/// <returns>The answer of DisplayMenu, also in AX</returns>
		ushort DisplayKingDialog(ushort charsAddr, ushort horizLoc, ushort vertLoc);
	}
}
